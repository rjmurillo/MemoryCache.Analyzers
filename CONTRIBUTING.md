# Contributing to MemoryCache.Analyzers

Thank you for contributing. By participating, you agree to follow the
[Code of Conduct](CODE_OF_CONDUCT.md).

## Prerequisites

Install the .NET SDK selected by [`global.json`](global.json). Run all commands
from the repository root.

### Git hooks

The repository uses [Husky.Net](https://alirezanet.github.io/Husky.Net/) to run
checks before you commit and push. Run these commands once after you clone:

```shell
dotnet tool restore
dotnet husky install
```

The hooks do the following:

- `pre-commit` checks the format of staged C# files. The commit fails if
  `dotnet format` would change them.
- `pre-push` builds the solution with warnings as errors, then runs the tests.
  The push fails if either step fails.

Task definitions live in [`.husky/task-runner.json`](.husky/task-runner.json).

## Build and test

Restore dependencies:

```shell
dotnet restore MemoryCache.Analyzers.sln
```

Format the solution:

```shell
dotnet format MemoryCache.Analyzers.sln
```

Build with warnings treated as errors:

```shell
dotnet build MemoryCache.Analyzers.sln --no-restore /p:PedanticMode=true
```

Run the full test suite with the repository settings:

```shell
dotnet test MemoryCache.Analyzers.sln --no-build --settings ./build/targets/tests/test.runsettings
```

## Add or change a rule

1. Choose the next available diagnostic ID from the category range in
   [`docs/rules/README.md`](docs/rules/README.md). The current Compatibility
   range is `SRC1000-1099`.
2. Add or update the analyzer under `src/MemoryCache.Analyzers`.
3. Add focused xUnit tests under `tests/MemoryCache.Analyzers.Tests`. Analyzer
   tests use `Microsoft.CodeAnalysis.Testing`.
4. Record new diagnostics and diagnostic metadata changes in
   `src/MemoryCache.Analyzers/AnalyzerReleases.Unshipped.md`.
5. Add or update the matching rule page under `docs/rules`.
6. Update the rule table in `docs/rules/README.md`.

Keep diagnostic metadata, implementation, tests, release tracking, and
documentation consistent. Do not claim that a code fix exists unless the
repository contains and tests one.

## Rule page expectations

Name each rule page after its diagnostic ID, for example
`docs/rules/SRC1000.md`. A rule page should include:

- The diagnostic ID and title.
- The category, default severity, and code fix availability.
- The exact condition that reports the diagnostic.
- Important conditions that do not report the diagnostic.
- Complete, compilable examples of reported and accepted code.
- Suppression or configuration guidance when applicable.
- Links to authoritative references for external technical claims.

Verify every behavior statement against the analyzer implementation and tests.

## Pull requests

Keep each pull request focused. Describe the change and include the commands
and results used to verify formatting, the build, and tests. Update
documentation whenever rule behavior or metadata changes.
