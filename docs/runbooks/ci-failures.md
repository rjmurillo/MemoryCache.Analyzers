# CI failures

Use this runbook when a build or test fails, locally or in CI.

## MSB4018 from GetBuildVersion

The build fails with `MSB4018` from the Nerdbank `GetBuildVersion` task.

The clone is shallow. Nerdbank.GitVersioning needs the git history to compute
the version. Run this, then build again:

```shell
git fetch --unshallow
```

## Console buffer size error in PowerShell

PowerShell throws `ArgumentOutOfRangeException` about the console buffer size.

This is a PSReadLine bug. Run the command from CMD instead.

## Verify snapshot test fails

A Verify snapshot test fails.

1. Compare the `*.received.*` file to the `*.verified.*` file.
2. Accept the change only if it is intended.

In CI, a failed build uploads every `*.received.*` file as the
`verify-test-results` artifact. Download it from the failed run's summary
page to compare the files. See
[`.github/workflows/main.yml`](../../.github/workflows/main.yml).
