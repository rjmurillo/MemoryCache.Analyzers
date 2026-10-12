# Configure MemoryCache.Analyzers

SRC1000 is enabled by default with warning severity. Configure or suppress it with standard Roslyn and MSBuild
settings.

## Set severity with .editorconfig

Add an `.editorconfig` file at the repository root to configure source files in that folder and its subfolders:

```ini
root = true

[*.cs]
dotnet_diagnostic.SRC1000.severity = error
```

Valid severity values include `error`, `warning`, `suggestion`, `silent`, `none`, and `default`. Use `none` to disable
SRC1000 for the files covered by that section:

```ini
[*.cs]
dotnet_diagnostic.SRC1000.severity = none
```

## Set severity with .globalconfig

A file named `.globalconfig` applies to every C# project in its folder and subfolders. It does not use file section
headers:

```ini
is_global = true

dotnet_diagnostic.SRC1000.severity = error
```

For a differently named global configuration file, add it to the project with a `GlobalAnalyzerConfigFiles` item.

## Disable SRC1000 with NoWarn

To disable SRC1000 for an entire project, append its ID to `NoWarn`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);SRC1000</NoWarn>
</PropertyGroup>
```

Keeping `$(NoWarn)` preserves warning IDs set elsewhere. Compiler options such as `NoWarn` take precedence over
severity configured in `.editorconfig` or `.globalconfig`.

## Suppress a specific use

Use `#pragma warning` when one expression is intentional:

```csharp
#pragma warning disable SRC1000
var cache = new System.Runtime.Caching.MemoryCache("legacy");
#pragma warning restore SRC1000
```

Use `SuppressMessage` when every SRC1000 diagnostic in a member is intentional:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;

internal static class LegacyCacheFactory
{
    [SuppressMessage(
        "Compatibility",
        "SRC1000",
        Justification = "This adapter is removed after the legacy cache migration.")]
    internal static MemoryCache Create()
    {
        return new MemoryCache("legacy");
    }
}
```

The first argument matches the analyzer category. Apply the attribute to the smallest member that contains the
intentional use, and include a specific justification.

## Multi-targeted projects

SRC1000 evaluates each target framework compilation separately:

- It is silent for .NET Framework targets.
- It reports uses of `System.Runtime.Caching.MemoryCache` for .NET Core and .NET 5 or later targets.

For example, a project targeting both .NET Framework 4.6.2 and .NET 8 reports SRC1000 only in the .NET 8 build:

```xml
<PropertyGroup>
  <TargetFrameworks>net462;net8.0</TargetFrameworks>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="MemoryCache.Analyzers" Version="0.1.0" PrivateAssets="all" />
  <PackageReference Include="System.Runtime.Caching" Version="8.0.0" />
</ItemGroup>
```

The `System.Runtime.Caching` package makes the API available to both target compilations. Run
`dotnet build --no-incremental` to see output for each target.

The analyzer identifies .NET Framework by the core library that defines `System.Object`. It skips a compilation when
that assembly is `mscorlib`, and it analyzes a compilation when that assembly is `System.Private.CoreLib`. The test
suite covers .NET Framework 4.6.2 and .NET 8 reference assemblies.

## References

- [Configuration files for code analysis rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files)
- [Configure code analysis rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options)
- [Suppress code analysis warnings](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/suppress-warnings)
- [C# warning options](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/errors-warnings)
