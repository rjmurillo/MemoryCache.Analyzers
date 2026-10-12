# Changelog

## [0.1.0] - 2026-10-11

This is the first stable release of MemoryCache.Analyzers.
Changes since `0.1.0-alpha`:

- Fixed the SRC1000 help link. It now points to this repository's rule page.
- Added install, configuration, and migration guides under `docs/`.
- Added a guide on cache stampedes with `GetOrCreate`.
- Updated .NET dependencies.

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

[0.1.0]: https://github.com/rjmurillo/MemoryCache.Analyzers/releases/tag/v0.1.0
[0.1.0-alpha]: https://github.com/rjmurillo/MemoryCache.Analyzers/releases/tag/v0.1.0-alpha
