# Guidance for AI Coding Agents

## Repository scope

MemoryCache.Analyzers is a C# Roslyn analyzer package. The analyzer project
targets `netstandard2.0`. The test project targets `net10.0`. The required .NET
SDK is selected by `global.json`.

The repository currently contains one rule, SRC1000. It reports object creation
for `System.Runtime.Caching.MemoryCache` and derived types when the compilation
does not target .NET Framework.

## Read before editing

Read these files before making changes:

- `CLAUDE.md`
- `CONTRIBUTING.md`
- `docs/rules/README.md`
- The implementation, tests, and rule page for the affected diagnostic

Treat source code and tests as the authority for behavior. Verify documentation
claims against them.

## Repository layout

- `src/MemoryCache.Analyzers/` contains analyzer code and release tracking.
- `tests/MemoryCache.Analyzers.Tests/` contains xUnit analyzer and package
  tests.
- `docs/rules/` contains the diagnostic index and rule pages.
- `build/targets/` contains shared build and test configuration.

The repository does not contain a code fix project or a benchmark project. Do
not claim that a code fix or benchmark exists. Follow
`CLAUDE.md` for requirements that apply when adding an analyzer.

## Change requirements

- Keep changes limited to the requested work.
- Add or update focused tests for analyzer behavior changes.
- Preserve concurrent execution and explicit generated code handling.
- Use Roslyn symbol equality when comparing symbols.
- Keep diagnostic metadata, tests, rule documentation, and release tracking in
  sync.
- Record pending diagnostic additions and metadata changes in
  `AnalyzerReleases.Unshipped.md`.
- Assign new Compatibility rules from the `SRC1000-1099` range documented in
  `docs/rules/README.md`.
- Give public APIs practical XML documentation.
- Use complete, compilable examples in rule documentation.

## Required verification

Run these commands from the repository root:

```shell
dotnet format MemoryCache.Analyzers.sln
dotnet build MemoryCache.Analyzers.sln --no-restore /p:PedanticMode=true
dotnet test MemoryCache.Analyzers.sln --no-build --settings ./build/targets/tests/test.runsettings
```

Report the commands and their results in the pull request description.
