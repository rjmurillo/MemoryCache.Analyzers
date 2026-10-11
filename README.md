# MemoryCache.Analyzers Rules

This document provides details about the rules included in the [MemoryCache.Analyzers](https://github.com/rjmurillo/memorycache.analyzers) package.

## Purpose

MemoryCache.Analyzers is a collection of Roslyn analyzers that help developers identify and fix issues related to caching APIs in .NET applications. The analyzers focus on ensuring proper usage of caching APIs and promoting best practices for performance, maintainability, and compatibility.

## Rule Categories

Rules are organized into the following categories:

- **Compatibility**: Rules that identify code that may have compatibility issues between different .NET versions

## Rule Severity

Each rule has a default severity level:

- **Error**: Indicates code that is likely to cause a significant issue
- **Warning**: Indicates code that may cause issues but might be intentional in some cases
- **Info**: Indicates code that could be improved but isn't likely to cause issues
- **Hidden**: Informational rules that aren't shown by default

## Rules

| ID | Title | Category | Severity | Description |
|----|-------|----------|----------|-------------|
| [SRC1000](docs/rules/SRC1000.md) | Avoid System.Runtime.Caching.MemoryCache in .NET Core+ | Compatibility | Warning | System.Runtime.Caching.MemoryCache is a compatibility bridge for porting from .NET Framework. Prefer Microsoft.Extensions.Caching.Memory in .NET Core+. |

## Using the Analyzers

### Installation

Install the analyzers via NuGet:

```
dotnet add package MemoryCache.Analyzers
```

### Configuration

You can configure the severity of analyzer rules in your project using an `.editorconfig` file. For example:

```editorconfig
# Increase the severity of SRC1000 from a warning to an error
dotnet_diagnostic.SRC1000.severity = error

# Disable SRC1000
dotnet_diagnostic.SRC1000.severity = none
```

## Building and Testing

To build the analyzers:

```
dotnet build
```

To run the tests:

```
dotnet test
```

## Guides

- [Install MemoryCache.Analyzers](docs/install.md)
- [Configure MemoryCache.Analyzers](docs/configuration.md)
- [Migrate from System.Runtime.Caching.MemoryCache](docs/migrating-from-system-runtime-caching.md)
- [GetOrCreate is not atomic: cache stampede in IMemoryCache](docs/getorcreate-stampede.md)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.