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
- `docs/architecture.md`
- The implementation, tests, and rule page for the affected diagnostic

Treat source code and tests as the authority for behavior. Verify documentation
claims against them.

## Repository layout

- `src/MemoryCache.Analyzers/` contains analyzer code and release tracking.
- `tests/MemoryCache.Analyzers.Tests/` contains xUnit analyzer and package
  tests.
- `docs/rules/` contains the diagnostic index and rule pages.
- `build/targets/` contains shared build and test configuration.

The repository does not contain a code fix project. Do not claim that a code
fix exists. `tests/MemoryCache.Analyzers.Benchmarks/` holds BenchmarkDotNet
benchmarks for the analyzer. Run them from the repository root:

```shell
dotnet run -c Release --project tests/MemoryCache.Analyzers.Benchmarks -- --filter="*"
```

Follow `CLAUDE.md` for requirements that apply when adding an analyzer.

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

## Environment variables

Local format, build, and test need no environment variables and no `.env`
file. The project does not read a `.env` file. The variables below matter only
in CI.

| Name | Where | Required | Purpose |
| --- | --- | --- | --- |
| `CODACY_PROJECT_TOKEN` | [`main.yml`][env-main] | No | Coverage upload |
| `NUGET_API_KEY` | [`release.yml`][env-release] | Set by CI | NuGet push |
| `CI`, `GITHUB_ACTIONS` | [`CodeAnalysis.targets`][env-ca] | No | CI detection |

- `CODACY_PROJECT_TOKEN` is an optional repository secret.
  [`release.yml`][env-release] passes it to [`main.yml`][env-main].
  CI uploads coverage to Codacy when it is set, and skips the upload when not.
- `NUGET_API_KEY` is a temporary key from the NuGet OIDC login step.
  Only the `publish` job in [`release.yml`][env-release] sets it.
  Nobody sets it by hand.
- GitHub Actions sets `CI` and `GITHUB_ACTIONS` to `true`. The
  `DotNet.ReproducibleBuilds` package, added by
  [`Reproducible.props`][env-repro], then sets `ContinuousIntegrationBuild`.
  [`CodeAnalysis.targets`][env-ca] uses that to turn on `PedanticMode`, which
  treats warnings as errors. If your shell sets `CI=true`, local builds do the
  same.

Never put secrets in the repository.

[env-main]: .github/workflows/main.yml
[env-release]: .github/workflows/release.yml
[env-ca]: build/targets/codeanalysis/CodeAnalysis.targets
[env-repro]: build/targets/reproducible/Reproducible.props

## Manual QA

To try the built package in a real project, follow
[Manual QA](docs/qa.md). It installs the local package in sample projects
and shows when `SRC1000` appears.

## Skills

- [`.claude/skills/verify-change/SKILL.md`](.claude/skills/verify-change/SKILL.md):
  run the format, build, and test checks and collect proof.
- [`.claude/skills/add-analyzer-rule/SKILL.md`](.claude/skills/add-analyzer-rule/SKILL.md):
  follow the checklist for adding or changing a diagnostic rule.

## Runbooks

For releases, bad releases, and CI failures, follow the
[runbooks](docs/runbooks/README.md).
