---
name: add-analyzer-rule
description: Checklist for adding or changing a diagnostic rule in MemoryCache.Analyzers, including IDs, tests, release tracking, and docs. Use when you create or change an analyzer.
---

# Add or change an analyzer rule

## Before you start

Read the "Add or change a rule" section of `CONTRIBUTING.md`. Then read
`docs/rules/README.md`.

## Checklist

These steps come from the "Add or change a rule" section of `CONTRIBUTING.md`.

1. Choose the next available diagnostic ID from the category range in
   `docs/rules/README.md`. The current Compatibility range is
   `SRC1000-1099`.
2. Add or update the analyzer under `src/MemoryCache.Analyzers`.
3. Add focused xUnit tests under `tests/MemoryCache.Analyzers.Tests`. Analyzer
   tests use `Microsoft.CodeAnalysis.Testing`.
4. Record new diagnostics and diagnostic metadata changes in
   `src/MemoryCache.Analyzers/AnalyzerReleases.Unshipped.md`.
5. Add or update the matching rule page under `docs/rules`.
6. Update the rule table in `docs/rules/README.md`.

These rules come from `CLAUDE.md`:

- Write the xUnit test first. Then add or change the logic.
- New analyzers use `IOperation`, not syntax-only checks.
- A new analyzer needs a code fix, a benchmark, and a page in `docs/rules/`.
  Benchmarks use BenchmarkDotNet, which is pinned in
  `Directory.Packages.props`.
- Do not expect diagnostics for code that cannot compile.

## Rule page

These items come from the "Rule page expectations" section of
`CONTRIBUTING.md`. Name each rule page after its diagnostic ID, for example
`docs/rules/SRC1000.md`. A rule page should include:

- The diagnostic ID and title.
- The category, default severity, and code fix availability.
- The exact condition that reports the diagnostic.
- Important conditions that do not report the diagnostic.
- Complete, compilable examples of reported and accepted code.
- Suppression or configuration guidance when applicable.
- Links to authoritative references for external technical claims.

## Finish

Use the `verify-change` skill to run the required checks and collect proof
for the pull request.
