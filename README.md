# MemoryCache.Analyzers

[![NuGet Version](https://img.shields.io/nuget/vpre/MemoryCache.Analyzers?style=flat&logo=nuget&color=blue)](https://www.nuget.org/packages/MemoryCache.Analyzers)
[![Main build](https://github.com/rjmurillo/MemoryCache.Analyzers/actions/workflows/main.yml/badge.svg)](https://github.com/rjmurillo/MemoryCache.Analyzers/actions/workflows/main.yml)

**MemoryCache.Analyzers** is a Roslyn analyzer that identifies creation of
`System.Runtime.Caching.MemoryCache` in modern .NET projects. Microsoft's
[ASP.NET Core caching guidance](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
recommends `Microsoft.Extensions.Caching.Memory` over that compatibility API.

## Example

SRC1000 reports the `MemoryCache` object creation in this C# code:

```csharp
using System;
using System.Runtime.Caching;

MemoryCache cache = new("orders"); // SRC1000
cache.Set("order:42", "ready", DateTimeOffset.UtcNow.AddMinutes(5));
```

Use `Microsoft.Extensions.Caching.Memory` instead:

```csharp
using System;
using Microsoft.Extensions.Caching.Memory;

using MemoryCache cache = new(new MemoryCacheOptions());
cache.Set("order:42", "ready", TimeSpan.FromMinutes(5));
```

## Install

Install the package in each project to analyze:

```shell
dotnet add package MemoryCache.Analyzers
```

Or add a package reference:

```xml
<PackageReference Include="MemoryCache.Analyzers"
                  Version="0.1.0"
                  PrivateAssets="all" />
```

## Supported targets

- Language: C#
- Analyzed applications: .NET Standard 2.0, .NET Core, and .NET 5 or later
- Excluded applications: .NET Framework
- Analyzer target framework: `netstandard2.0`

SRC1000 reports only object creation for
`System.Runtime.Caching.MemoryCache` or a type derived from it. Calls such as
`Get` and `Set`, other member access, and `MemoryCache.Default` do not produce
diagnostics.

## Rules

| ID | Category | Severity | Code fix | Title |
| --- | --- | --- | --- | --- |
| [SRC1000](docs/rules/SRC1000.md) | Compatibility | Warning | No | Avoid System.Runtime.Caching.MemoryCache in .NET Core+ |

See the [rule reference](docs/rules/README.md) for implementation details.
See [Guides](#guides) for installation, configuration, and migration steps.

For experimental patterns and decorators built on `IMemoryCache`, see
[memory-cache-solutions](https://github.com/rjmurillo/memory-cache-solutions).

## Configure severity

Use `.editorconfig` to change the severity of SRC1000:

```editorconfig
[*.cs]
dotnet_diagnostic.SRC1000.severity = error
```

## Guides

- [Install MemoryCache.Analyzers](docs/install.md)
- [Configure MemoryCache.Analyzers](docs/configuration.md)
- [Migrate from System.Runtime.Caching.MemoryCache](docs/migrating-from-system-runtime-caching.md)
- [GetOrCreate is not atomic: cache stampede in IMemoryCache](docs/getorcreate-stampede.md)

## Build and test

The repository uses the .NET SDK version in `global.json`.

```shell
dotnet build MemoryCache.Analyzers.sln /p:PedanticMode=true
dotnet test MemoryCache.Analyzers.sln --no-build --settings ./build/targets/tests/test.runsettings
```

## License

MemoryCache.Analyzers is licensed under the [MIT License](LICENSE).