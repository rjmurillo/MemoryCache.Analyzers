# Pull request

## Summary

<!-- List every change. Link the issue: Closes #123 -->

## Verification

### Format

Run this command from the repository root:

```shell
dotnet format MemoryCache.Analyzers.sln
```

```text
<!-- paste the output here -->
```

### Build (warnings as errors)

Run this command from the repository root:

```shell
dotnet build MemoryCache.Analyzers.sln --no-restore /p:PedanticMode=true
```

```text
<!-- paste the output here -->
```

### Tests

Run this command from the repository root:

```shell
dotnet test MemoryCache.Analyzers.sln --no-build --settings ./build/targets/tests/test.runsettings
```

```text
<!-- paste the output here -->
```

### Benchmarks (if you ran them)

<!-- paste BenchmarkDotNet markdown output, or write "Not run" -->

## Checklist

- [ ] `dotnet format` changes are committed.
- [ ] Tests cover every new or changed branch.
- [ ] Rule docs in `docs/rules/` are updated if rule behavior changed.
- [ ] `AnalyzerReleases.Unshipped.md` is updated if diagnostics changed.
- [ ] Bot and reviewer feedback is addressed, with proof.
