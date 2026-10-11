# Migrate from System.Runtime.Caching.MemoryCache to IMemoryCache

[`SRC1000`](rules/SRC1000.md) reports construction of
`System.Runtime.Caching.MemoryCache` and derived types in .NET Core and later
applications. It does not report `MemoryCache.Default` or other member access,
so search for `MemoryCache.Default` when planning a migration.

Microsoft's [in-memory caching
guidance](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
states that `IMemoryCache` "is recommended over
`System.Runtime.Caching`/`MemoryCache` because it offers better integration with
ASP.NET Core." It also says to use `System.Runtime.Caching`/`MemoryCache` "as a
compatibility bridge when porting code from ASP.NET 4.x to ASP.NET Core."

This guide covers the API and behavior changes that require attention during a
migration. It does not cover distributed caching. Both cache implementations
store data in the current process, so each application instance has its own
cache.

## Add the package and register the cache

For a console app, the After sample requires the
`Microsoft.Extensions.Caching.Memory` and
`Microsoft.Extensions.DependencyInjection` packages. The latter provides
`BuildServiceProvider`. ASP.NET Core apps receive both from the shared
framework. Register the default implementation once during application
startup:

```csharp
services.AddMemoryCache();
```

Request `IMemoryCache` through constructor injection. `AddMemoryCache`
registers the cache as a singleton, so do not dispose an injected instance.

## API mapping

| `System.Runtime.Caching` | `Microsoft.Extensions.Caching.Memory` | Migration notes |
| --- | --- | --- |
| `MemoryCache.Default` | Inject the singleton `IMemoryCache` registered by `AddMemoryCache` | There is no static default cache. |
| `new MemoryCache("name")` | Register a separate service that owns a `Microsoft.Extensions.Caching.Memory.MemoryCache` | The new cache has no name. Use separate instances only when entries need different cache-wide options or lifetimes. |
| `CacheItemPolicy` | `MemoryCacheEntryOptions` | Translate each policy property explicitly. The types are not interchangeable. |
| `AbsoluteExpiration` | `AbsoluteExpiration` or `AbsoluteExpirationRelativeToNow` | A relative duration avoids calculating a timestamp at the call site. |
| `SlidingExpiration` | `SlidingExpiration` | A sliding expiration does not extend an entry beyond an absolute expiration when both are set. |
| `CacheItemPriority.Default` | `CacheItemPriority.Normal` | The modern API also provides `Low` and `High`. |
| `CacheItemPriority.NotRemovable` | `CacheItemPriority.NeverRemove` | Expiration tokens, explicit removal, and replacement can still remove the entry. |
| `ChangeMonitors` | `ExpirationTokens` or `AddExpirationToken(IChangeToken)` | Replace each monitor with an `IChangeToken`. For files, `IFileProvider.Watch` supplies a token. |
| `Add` | No direct equivalent | `TryGetValue` followed by `Set` is not atomic. Synchronize access if the insert must fail when the key already exists. |
| `AddOrGetExisting` | `GetOrCreate` or `GetOrCreateAsync` for common lookup and creation flows | The return contract differs, and the factory can run concurrently for the same missing key. Use application-level coordination when one execution is required. |
| `Get` | `Get<T>` or `TryGetValue<T>` | Prefer `TryGetValue` when a miss must be distinguished from a cached default or null value. |
| `Set` | `Set` | Both create or replace an entry. Modern `Set` returns the stored value. |
| `Remove` | `Remove` | Modern `Remove` returns `void`; capture the value with `TryGetValue` first if it is needed. |
| Cache memory limits and physical memory limits | `MemoryCacheOptions.SizeLimit` with `MemoryCacheEntryOptions.Size` | The modern limit uses application-defined units, not bytes or a percentage of physical memory. |
| `RemovedCallback` | `RegisterPostEvictionCallback` | The modern callback receives the key, value, `EvictionReason`, and optional state after eviction. |
| `UpdateCallback` | No direct equivalent | Refresh or replace the entry in application code. Avoid doing expensive refresh work in an eviction callback. |

## Before

This example uses the legacy compatibility package:

```csharp
using System.Runtime.Caching;

const string ProductCacheKey = "product:42";

ObjectCache cache = MemoryCache.Default;
CacheItemPolicy policy = new()
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(30),
    Priority = CacheItemPriority.Default,
    RemovedCallback = arguments =>
        Console.WriteLine(
            $"Removed {arguments.CacheItem.Key}: {arguments.RemovedReason}")
};

Product product = LoadProduct(42);
cache.Set(ProductCacheKey, product, policy);

Product? cachedProduct = cache.Get(ProductCacheKey) as Product;
cache.Remove(ProductCacheKey);

static Product LoadProduct(int id) => new(id, $"Product {id}");

internal sealed record Product(int Id, string Name);
```

## After

This complete example registers `IMemoryCache`, injects it into a service, and
translates the entry policy:

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddMemoryCache();
services.AddSingleton<ProductService>();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
ProductService productService =
    serviceProvider.GetRequiredService<ProductService>();

Product product = productService.GetProduct(42);
Console.WriteLine(product.Name);
productService.RemoveProduct(42);

internal sealed class ProductService
{
    private const string ProductCacheKeyPrefix = "product:";
    private static readonly TimeSpan AbsoluteExpiration =
        TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SlidingExpiration =
        TimeSpan.FromMinutes(5);

    private readonly IMemoryCache cache;

    public ProductService(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public Product GetProduct(int id)
    {
        string cacheKey = $"{ProductCacheKeyPrefix}{id}";

        return cache.GetOrCreate(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AbsoluteExpiration;
                entry.SlidingExpiration = SlidingExpiration;
                entry.Priority = CacheItemPriority.Normal;
                entry.RegisterPostEvictionCallback(
                    static (key, _, reason, _) =>
                        Console.WriteLine($"Removed {key}: {reason}"));

                return LoadProduct(id);
            })!;
    }

    public void RemoveProduct(int id)
    {
        cache.Remove($"{ProductCacheKeyPrefix}{id}");
    }

    private static Product LoadProduct(int id) =>
        new(id, $"Product {id}");
}

internal sealed record Product(int Id, string Name);
```

## Named caches

`MemoryCache.Default` and named `System.Runtime.Caching.MemoryCache` instances
do not map to names in `IMemoryCache`. The DI registration supplies one shared
cache. If the old application used names only to organize keys, keep one cache
and add stable prefixes to keys.

If cache instances require different size limits, expiration scan frequencies,
or lifetimes, create small application-specific services that each own a
`Microsoft.Extensions.Caching.Memory.MemoryCache`. Register those services with
DI and dispose their caches when the services are disposed.

## Entry options

Translate a `CacheItemPolicy` into a new `MemoryCacheEntryOptions` for each
entry.

Absolute and sliding expiration can be combined in `IMemoryCache`. Combining
them is useful when frequently accessed data must still be refreshed after a
fixed maximum lifetime. A sliding expiration by itself can keep an active entry
alive indefinitely. This differs from `System.Runtime.Caching.MemoryCache`,
which throws when one policy sets both expiration modes.

Replace `ChangeMonitor` implementations with `IChangeToken` implementations.
The token expires the entry when it signals a change. Common token sources
include configuration reload tokens, `CancellationChangeToken`, and
`IFileProvider.Watch`.

## Size limits

`System.Runtime.Caching.MemoryCache` can use a limit in megabytes, a percentage
of physical memory, and polling of memory pressure. `IMemoryCache` does not
provide the same memory-pressure behavior. Its `SizeLimit` is a logical limit
whose units are defined by the application.

When `SizeLimit` is set, every entry added to that cache must specify `Size`.
Choose one unit, such as item count or estimated bytes, and use it consistently.
An entry without a size throws this exception:

```text
Cache entry must specify a value for Size when SizeLimit is set.
```

Do not add a size limit to the shared DI cache when libraries or framework
components may add entries without sizes. Create and register a dedicated
cache instance instead.

The runtime does not automatically limit `IMemoryCache` based on system memory
pressure. Set expirations, limit externally supplied keys, and call `Compact`
on a concrete `MemoryCache` when the application has a measured reason to
remove a percentage of entries.

## Eviction callbacks

Register post-eviction callbacks on `MemoryCacheEntryOptions` or `ICacheEntry`.
Callbacks report why an entry was removed, but they run after removal and
should not be used as an atomic update mechanism. `CacheItemPolicy.UpdateCallback`
has no direct replacement.

Treat callbacks as notifications. Keep them short, handle their failures, and
move refresh work into the normal lookup path or a background service.

## Behavior differences and migration gotchas

* `GetOrCreate` and `GetOrCreateAsync` do not guarantee that only one caller
  runs the factory for a missing key. Several callers can perform the load
  concurrently. Add locking or another coordination mechanism when duplicate
  work is unsafe. On .NET 9 and later, the
  `Microsoft.Extensions.Caching.Hybrid` package provides
  [HybridCache](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid)
  with built-in stampede protection. See the
  [single-flight guidance](https://github.com/rjmurillo/memory-cache-solutions#recommended-alternatives-for-single-flight-cache-stampede-protection)
  for other target frameworks.
* A legacy `Add` reports whether insertion succeeded, and
  `AddOrGetExisting` returns the prior value or null after a successful insert.
  Modern `GetOrCreate` returns the resulting value. Review code that depends on
  the old return values.
* `System.Runtime.Caching.MemoryCache` rejects null values. `IMemoryCache` can
  store null. Use `TryGetValue` when null is a valid cached result.
* Expiration is not an exact timer. Cache activity can trigger scans, and a
  callback can run after the configured expiration time.
* A size-limited entry can be rejected instead of being stored. Code must
  always be able to reload data after a cache miss.
* `NeverRemove` affects compaction priority, but explicit removal, replacement,
  expiration, and change tokens still apply.
* Neither implementation shares entries between processes or servers. Use a
  distributed cache when instances must share cached data.
* The legacy region parameters have no replacement. The legacy
  `MemoryCache` implementation did not support regions either.
* DI owns the cache returned through `IMemoryCache`. Do not dispose it from a
  consuming service.
* Avoid cache keys created directly from unbounded external input. They can
  cause uncontrolled memory growth.

## Official references

* [ASP.NET Core in-memory caching](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
* [Caching in .NET](https://learn.microsoft.com/dotnet/core/extensions/caching)
* [`IMemoryCache` API](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory.imemorycache)
* [`MemoryCacheEntryOptions` API](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions)
* [`AddMemoryCache` API](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.memorycacheservicecollectionextensions.addmemorycache)
* [`System.Runtime.Caching.MemoryCache` API](https://learn.microsoft.com/dotnet/api/system.runtime.caching.memorycache)
* [`CacheItemPolicy` API](https://learn.microsoft.com/dotnet/api/system.runtime.caching.cacheitempolicy)
