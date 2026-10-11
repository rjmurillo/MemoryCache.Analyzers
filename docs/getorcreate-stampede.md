# GetOrCreate is not atomic: cache stampede in IMemoryCache and how to fix it

`IMemoryCache` is thread safe, but `GetOrCreate` and `GetOrCreateAsync` are not
atomic: when several callers miss the same key at once, each runs the factory.
Use HybridCache, FusionCache, or a per-key lock when duplicate work is costly or
unsafe.

In short, `IMemoryCache` is thread safe but not atomic. Concurrent `Get`, `Set`,
and `Remove` calls do not corrupt the cache. `GetOrCreate` is a check followed
by a write, and other callers can run between those two steps.

## Why GetOrCreate runs the factory more than once

`GetOrCreate` checks the cache, runs the factory on a miss, and then stores the
result. It takes no lock. This is the shape of
[`MemoryCacheExtensions.cs`](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Caching.Abstractions/src/MemoryCacheExtensions.cs)
in dotnet/runtime, with option handling removed:

```csharp
public static TItem? GetOrCreate<TItem>(
    this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
{
    if (!cache.TryGetValue(key, out object? result))
    {
        using ICacheEntry entry = cache.CreateEntry(key);
        result = factory(entry);
        entry.Value = result;
    }

    return (TItem?)result;
}
```

`GetOrCreateAsync` has the same shape, with `await factory(entry)` in place of
`factory(entry)`.

Here is what happens when two requests ask for the same missing key:

1. Caller A calls `TryGetValue`. The key is missing.
2. Caller B calls `TryGetValue`. The key is still missing.
3. Caller A runs the factory, for example a database query.
4. Caller B runs the same factory.
5. Caller A stores its value.
6. Caller B stores its value and replaces the value from caller A. The last
   write wins.

`ConcurrentDictionary.GetOrAdd` can also run its factory more than once. Its
[documentation](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd)
says the value factory may be called multiple times when several threads call
`GetOrAdd` at the same time. One difference: `GetOrAdd` keeps only one value,
while with `GetOrCreate` the last write wins.

## Is this a bug?

No. The runtime team kept this behavior on purpose. In
[dotnet/runtime#71581](https://github.com/dotnet/runtime/issues/71581), closed as
not planned, a maintainer replied: "This is the same behavior that
ConcurrentDictionary.GetOrAdd has." The earlier proposal for exclusive versions,
[dotnet/runtime#36500](https://github.com/dotnet/runtime/issues/36500), was
closed without adding the API. A maintainer said that making `GetOrCreate`
thread safe would need locks inside `MemoryCache`, which raised performance
concerns. The runtime did not change `GetOrCreate`.

## Reproduce it

This console app starts 20 callers that ask for the same key at once. It needs
the `Microsoft.Extensions.Caching.Memory` package.

```csharp
using Microsoft.Extensions.Caching.Memory;

var cache = new MemoryCache(new MemoryCacheOptions());
int calls = 0;

await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
    cache.GetOrCreateAsync("product:42", async entry =>
    {
        Interlocked.Increment(ref calls);
        await Task.Delay(100);
        return 42;
    })));

Console.WriteLine($"Factory calls: {calls}");
```

The factory count is usually greater than 1, and it is often one call per
caller. The exact number depends on timing, so do not treat it as a fixed
value.

## When it matters

Duplicate factory runs are harmless when the load is cheap and has no side
effects. They matter when:

- The load is expensive, such as a slow database query or a large HTTP call.
- The backing service has rate limits or per-call costs.
- The factory is not idempotent. For example, it writes a record, sends a
  message, or reserves a resource.
- A popular entry expires under heavy traffic, so many requests miss at the
  same moment. This is a cache stampede.

Microsoft's
[in-memory caching guidance](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
describes the same risk for a callback that repopulates an entry. It says this
"can result in several threads repopulating the cached item."

## Fix 1: HybridCache

HybridCache runs the factory once per key for concurrent callers on the same
instance. Add the `Microsoft.Extensions.Caching.Hybrid` package and register
the service:

```csharp
builder.Services.AddHybridCache();
```

Inject `HybridCache` and call `GetOrCreateAsync`:

```csharp
using Microsoft.Extensions.Caching.Hybrid;

public sealed class ProductService(HybridCache cache, IProductStore store)
{
    public async Task<Product> GetProductAsync(
        int id, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"product:{id}",
            async token => await store.LoadAsync(id, token),
            cancellationToken: ct);
}
```

`Product` and `IProductStore` stand in for your own types. The factory receives
a `CancellationToken` and returns a `ValueTask<T>`.

Know these limits before you switch:

- HybridCache shipped with .NET 9, but it does not require .NET 9. The
  [HybridCache article](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid)
  says the library "supports older .NET runtimes, down to .NET Framework 4.7.2
  and .NET Standard 2.0." It runs on .NET 8 too.
- Coordination is per instance. The
  [Caching in .NET](https://learn.microsoft.com/dotnet/core/extensions/caching)
  article says: "This coordination doesn't extend to other `HybridCache`
  instances, even if they use the same distributed cache." Each server can
  still run the factory once.
- HybridCache works without a distributed cache. With no `IDistributedCache`
  registered, it still provides in-process caching and stampede protection.
- Keys are strings, and the default maximum key length is 1024 characters.

## Fix 2: FusionCache or other libraries

[FusionCache](https://github.com/ZiggyCreatures/FusionCache) also protects
against cache stampedes. For samples of both libraries and advice on picking
one, see the
[single-flight alternatives](https://github.com/rjmurillo/memory-cache-solutions#recommended-alternatives-for-single-flight-cache-stampede-protection)
in memory-cache-solutions and its
[When to Choose Which](https://github.com/rjmurillo/memory-cache-solutions#when-to-choose-which)
section.

## Fix 3: per-key lock around IMemoryCache

If you cannot add a dependency, lock per key, not per cache. This class keeps
one `SemaphoreSlim` for each key that has a load in progress:

```csharp
using Microsoft.Extensions.Caching.Memory;

public sealed class KeyedCacheLoader(IMemoryCache cache)
{
    private readonly Dictionary<object, Gate> gates = new();

    public async Task<T> GetOrCreateAsync<T>(
        object key,
        Func<CancellationToken, Task<T>> load,
        MemoryCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out T? value))
        {
            return value!;
        }

        Gate gate = Rent(key);
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Another caller may have loaded the value while this one waited.
                if (cache.TryGetValue(key, out value))
                {
                    return value!;
                }

                value = await load(cancellationToken).ConfigureAwait(false);
                cache.Set(key, value, options);
                return value;
            }
            finally
            {
                gate.Semaphore.Release();
            }
        }
        finally
        {
            Return(key, gate);
        }
    }

    private Gate Rent(object key)
    {
        lock (gates)
        {
            if (!gates.TryGetValue(key, out Gate? gate))
            {
                gate = new Gate();
                gates.Add(key, gate);
            }

            gate.References++;
            return gate;
        }
    }

    private void Return(object key, Gate gate)
    {
        lock (gates)
        {
            if (--gate.References == 0)
            {
                gates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private sealed class Gate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int References { get; set; }
    }
}
```

How it avoids the common traps:

- **One lock per key.** Callers for different keys load in parallel. The
  `lock (gates)` statement guards only the dictionary update. It never wraps the
  load or an `await`.
- **Failures are not cached.** The value is stored only after `load` returns.
  If `load` throws, the next caller runs it again.
- **Semaphores are cleaned up.** A reference count tracks the callers that use
  each gate. The last caller removes and disposes it, so the dictionary does
  not grow with every key ever requested.
- **Check again after the wait.** A caller that waited finds the value that the
  first caller stored and does not load it again.

Register `KeyedCacheLoader` as a singleton. It only coordinates callers that
share the same instance, in the same process. It also assumes each key always
holds a `T`. A value of another type counts as a miss and is replaced.

### Why not cache a `Lazy<Task<T>>`?

A common pattern stores a `Lazy<Task<T>>` with
`cache.GetOrCreate(key, _ => new Lazy<Task<T>>(load))`. This narrows the race
window, because creating a `Lazy` is fast. It does not close it. Callers that
miss at the same moment each create their own `Lazy`, each run `Value`, and
each start the load. In our tests with threads released at the same time, the
load still ran more than once. If you use this pattern anyway, remove the entry
when the task faults. Otherwise the cache keeps the failed task, and every later
caller gets the same exception until the entry expires. `IMemoryCache` has no
remove-if-unchanged call, so that removal can also evict a newer, healthy entry
that another caller stored. The keyed loader above avoids both problems.

## What does not fix it

- **`TryGetValue` followed by `Set`.** This is the same check-then-write as
  `GetOrCreate`, so it has the same race.
- **A plain `lock` around async code.** C# does not allow `await` inside a
  `lock` statement. The compiler reports
  `error CS1996: Cannot await in the body of a lock statement`.
- **One global `SemaphoreSlim`.** It stops duplicate loads, but it also makes
  every key wait for every other key. A slow load for one key blocks all
  others.

## FAQ

### Is IMemoryCache thread safe?

Yes, for individual calls. Concurrent `Get`, `Set`, and `Remove` calls do not
corrupt the cache. `GetOrCreate` is not atomic, so the factory can run several
times for one key.

### Why is my GetOrCreateAsync factory called multiple times?

Several callers missed the key before the first one stored a value, and each
ran the factory. This is by design.

### Will Microsoft make GetOrCreate atomic?

Not as of this writing. The runtime team declined to change it in
[dotnet/runtime#71581](https://github.com/dotnet/runtime/issues/71581) and
[dotnet/runtime#36500](https://github.com/dotnet/runtime/issues/36500).
HybridCache, which came later, provides stampede protection.

### Does HybridCache need .NET 9?

No. It shipped with .NET 9, but it runs on .NET 8 and supports runtimes down to
.NET Framework 4.7.2 and .NET Standard 2.0.

### Does HybridCache stop stampedes across servers?

No. Microsoft's documentation says the coordination does not extend to other
`HybridCache` instances, even when they share a distributed cache.

### Can I just lock around GetOrCreate?

A single global lock works, but it makes every key wait for every other key.
Use a per-key lock or a library instead. A `lock` statement cannot contain an
`await`, so async code needs `SemaphoreSlim`.

### What is a cache stampede?

Many requests miss the same key at once and all recompute it. The extra load
can overwhelm the database or service behind the cache.

## See also

- [Caching in .NET](https://learn.microsoft.com/dotnet/core/extensions/caching)
- [HybridCache library in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid)
- [ASP.NET Core in-memory caching](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
- [Migrate from System.Runtime.Caching.MemoryCache to IMemoryCache](migrating-from-system-runtime-caching.md)
- [memory-cache-solutions single-flight alternatives](https://github.com/rjmurillo/memory-cache-solutions#recommended-alternatives-for-single-flight-cache-stampede-protection)
