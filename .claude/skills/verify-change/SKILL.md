---
name: verify-change
description: Run the required format, build, and test checks for MemoryCache.Analyzers and collect proof for the pull request. Use before every commit or push.
---

# Verify a change

These steps come from the "Required verification" section of `AGENTS.md`.

## Steps

Run these commands from the repository root, in this order:

```shell
dotnet format MemoryCache.Analyzers.sln
dotnet build MemoryCache.Analyzers.sln --no-restore /p:PedanticMode=true
dotnet test MemoryCache.Analyzers.sln --no-build --settings ./build/targets/tests/test.runsettings
```

## If a step fails

These items come from the "Troubleshooting" section of `CLAUDE.md`.

- Build fails with `MSB4018` from the Nerdbank `GetBuildVersion` task.
  The clone is shallow. Run `git fetch --unshallow`, then build again.
- PowerShell throws `ArgumentOutOfRangeException` about console buffer size.
  This is a PSReadLine bug. Run the command from CMD instead.
- A Verify snapshot test fails. Compare the `*.received.*` file to the
  `*.verified.*` file. Accept the change only if it is intended.

## Proof

Paste the exact console output of each command into the pull request
description. The "PR checklist" section of `CLAUDE.md` requires this proof.
