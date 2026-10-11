# Changelog

## [0.1.0-alpha] - 2025-10-02

This prerelease introduced the MemoryCache.Analyzers package.

### Added

- Added SRC1000, which reports construction of
  `System.Runtime.Caching.MemoryCache` and derived types outside .NET Framework.

### Changed

- Renamed the package from `System.Runtime.Caching.Analyzers` to
  `MemoryCache.Analyzers` to avoid a name conflict on NuGet.org.
- Updated BenchmarkDotNet to 0.15.2.
- Updated .NET dependencies.

[0.1.0-alpha]: https://github.com/rjmurillo/MemoryCache.Analyzers/releases/tag/v0.1.0-alpha
