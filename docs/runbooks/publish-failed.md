# Publish failed

Use this runbook when the `publish` job in the
[`Release publish`](../../.github/workflows/release.yml) workflow fails. Open
the failed run in the **Actions** tab and find the failed step.

## Package version does not match release tag

The `Verify package version matches release tag` step fails with
`Package version '...' does not match release tag '...'`.

First, check the two versions in the message.

### Mistyped `expected-version` in a manual run

A manual run compares the package with the `expected-version` input, not the
tag. If the package version matches the tag, only the input was wrong. Start a
new manual run on the same tag with the correct input:

```shell
gh workflow run release.yml --ref vX.Y.Z -f expected-version=X.Y.Z
```

Do not re-run the failed job. A re-run keeps the old input.

### Package version does not match the tag

The version in [`version.json`](../../version.json) at the tagged commit is
not the version in the tag.

1. Do not move or re-push the existing tag.
2. Open a pull request that sets `version.json` to the next unused version.
   Merge it after CI passes.
3. Make a new tag for that version on the merge commit. Follow
   [Release a new version](release.md) from step 3.

## NuGet login failed

The `NuGet login (OIDC to temporary API key)` step fails.

The workflow uses NuGet trusted publishing. The login works only when the
trusted publishing policy on nuget.org matches the workflow. Sign in to
nuget.org as `rjmurillo` and check that the policy has these values:

| Field | Value |
| --- | --- |
| Owner | `rjmurillo` |
| Repository | `MemoryCache.Analyzers` |
| Workflow file | `release.yml` |
| Environment | `release` |

Fix any value that differs. Then re-run the failed job.

## Push failed

The `Publish NuGet package` step fails with
`NuGet push failed for '...' after 3 attempts`.

The step already retries three times. Re-run the failed job from the run page.
This is safe: `dotnet nuget push` uses `--skip-duplicate`, so a package that
is already on NuGet is skipped, not pushed twice.

The temporary NuGet API key lasts one hour. A re-run gets a new key from the
login step.
