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

Package versions are set only in `Directory.Packages.props` or
`build/targets/*/Packages.props`, and per-project overrides fail restore.

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

## Test naming

Follow these rules for tests under `tests/MemoryCache.Analyzers.Tests`:

1. Test class names end in `Tests` and mirror the type under test. For
   example, `SystemRuntimeCachingAnalyzerTests` tests
   `SystemRuntimeCachingAnalyzer`.
2. Test file names match the class name. For example,
   `SystemRuntimeCachingAnalyzerTests` lives in
   `SystemRuntimeCachingAnalyzerTests.cs`.
3. Test method names are PascalCase sentences that describe behavior. They
   contain no underscores. For example, use
   `FlagsMemoryCacheUsageInNetCoreProject`, not
   `Flags_MemoryCache_Usage`.

`TestConventionTests` checks rules 1 and 3. It fails and lists each test
class or method that breaks them.

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

## Labels

Labels help people and agents filter work by type, area, and priority.

- Every issue gets one type label: `bug`, `enhancement`, `documentation`,
  `chore`, or `dependencies`.
- Every issue gets one or more `area:` labels.
- Maintainers set one `priority:` label when they triage the issue.

The table lists the labels added for this system and their meaning.

| Label | Meaning |
| --- | --- |
| `priority: P0` | Critical. Fix now. |
| `priority: P1` | High. Fix in the next release. |
| `priority: P2` | Medium. Plan it. |
| `priority: P3` | Low. Nice to have. |
| `chore` | Maintenance with no user-facing change. |
| `dependencies` | Dependency updates. |
| `area: analyzer` | Analyzer code in `src/`. |
| `area: tests` | Tests in `tests/`. |
| `area: docs` | Docs in `docs/` and root markdown. |
| `area: build` | MSBuild files in `build/` and `Directory.*`. |
| `area: ci` | GitHub Actions in `.github/workflows`. |
| `area: release` | Versioning, packaging, and NuGet publishing. |

## Flaky tests

A flaky test passes in some runs and fails in others with no code change.

- The "Test stability" workflow runs every week. You can also start it by hand
  from the Actions tab. It builds once, runs the test suite several times, and
  lists any test that failed in some runs.
- If it reports a test, open a bug with the labels `bug` and `area: tests`.
  Then fix the cause. Do not add retries. Retries hide flaky tests.

## Pull requests

Keep each pull request focused. Describe the change and include the commands
and results used to verify formatting, the build, and tests. Update
documentation whenever rule behavior or metadata changes.

## Release notes

Release notes come from merged pull requests. GitHub groups them by label, as
set in [`.github/release.yml`](.github/release.yml). Add the
`ignore-for-release` label to leave a pull request out of the notes.

Create the release with generated notes:

```shell
gh release create vX.Y.Z --generate-notes --verify-tag
```

Copy the important lines into [`CHANGELOG.md`](CHANGELOG.md) in the same
release pull request that bumps `version.json`.
