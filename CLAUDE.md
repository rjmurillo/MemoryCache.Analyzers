# MemoryCache.Analyzers

Roslyn analyzers that flag risky use of caching APIs in .NET code.
These rules apply to every contributor, human or agent.
PRs that do not follow them will be closed.

Keep answers clear and short, at a grade 9 reading level.
Use plain English. Follow SOLID, DRY, and YAGNI.
Use official .NET patterns, not forum shortcuts.

## Targets

- The analyzer project targets `netstandard2.0`. Roslyn loads it in any host.
- The test project targets `net10.0` and uses xUnit.
- `LangVersion` is `default`. Do not pin it.
- The SDK version comes from `global.json`.
- Package versions live in `Directory.Packages.props`.

## Repository layout

- `src/`: analyzers and code fixes.
- `tests/`: unit tests and package baseline tests.
- `docs/rules/`: one page per rule, plus an index.
- `build/targets/`: shared MSBuild props and targets.

## Workflow

Run these steps in order before you push.

1. Format: `dotnet format`. Style comes from `.editorconfig`.
2. Build with warnings as errors: `dotnet build /p:PedanticMode=true`.
3. Test: `dotnet test --settings ./build/targets/tests/test.runsettings`.

Other rules:

- Read `AGENTS.md` and `CONTRIBUTING.md` when they exist, and follow them.
- Do not add tech debt or analyzer suppressions without approval and a reason.
  If you suspect an analyzer bug, open an issue with a small repro.
- All changes need 100% test coverage.
- Write the xUnit test first. Then add or change the logic.
- Keep analyzers fast and low on memory. Reuse existing patterns.
- Every public API needs XML docs that explain real use and purpose.
- New analyzer: add a code fix, a benchmark, and a page in `docs/rules/`.
  Benchmarks use BenchmarkDotNet, which is pinned in `Directory.Packages.props`.
- Changed analyzer: update its page in `docs/rules/`.
- New analyzers use `IOperation`, not syntax-only checks.
- Do not expect diagnostics for code that cannot compile.
- Ask questions when requirements are unclear.

## Troubleshooting

- Build fails with `MSB4018` from the Nerdbank `GetBuildVersion` task.
  The clone is shallow. Run `git fetch --unshallow`, then build again.
- PowerShell throws `ArgumentOutOfRangeException` about console buffer size.
  This is a PSReadLine bug. Run the command from CMD instead.
- A Verify snapshot test fails. Compare the `*.received.*` file to the
  `*.verified.*` file. Accept the change only if it is intended.

## Bot and reviewer feedback

- Treat feedback from bots (CodeRabbit, Codacy, Copilot, linters) like human review.
- Fix all format and lint issues from bots or CI before you ask for review.
- If you disagree with a bot, explain why in the PR description.
- Markdown must pass lint checks.
- After a rebase or merge, look for duplicate or conflicting changes.
  Remove them and note it in the PR description.

## Reasoning and code quality

- Logic must live in code and tests, not in narration.
- Show every step. Do not skip or simulate steps.
- Write the failure path first and make it obvious:
  - Guard clauses such as `throw new ArgumentNullException(nameof(param))`.
  - `Debug.Assert(condition)` for invariants.
  - `Assert.Throws<T>(() => ...)` in tests for error paths.
- Test every branch, including edge and failure paths.
- If you are guessing, stop. Ask for context, or fail loudly with
  `throw new NotImplementedException("Unclear requirement: ...")`.
- Do not copy and tweak analyzer patterns. Re-read the requirements first.

## Output rules for agents

- Output only complete code that compiles, with all `using` directives.
- Write tests for new or changed logic before you suggest refactors.
- Do not claim success. Show it with passing tests.
- If you cannot verify a change, stop and ask.

## PR checklist

Every PR description must include:

1. Proof that `dotnet format` ran and its changes are committed.
2. Proof of a clean build with warnings as errors.
3. Proof that all tests pass.
4. Benchmark output in markdown, if you ran benchmarks.
5. A summary of every change.

Proof means the exact console output or a screenshot.
If a bot or reviewer asked for a change, show proof that you made it.
