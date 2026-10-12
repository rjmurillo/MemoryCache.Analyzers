# Publish failed

Use this runbook when the `publish` job in the
[`Release publish`](../../.github/workflows/release.yml) workflow fails. Open
the failed run in the **Actions** tab and find the failed step.

## Package version does not match release tag

The `Verify package version matches release tag` step fails with
`Package version '...' does not match release tag '...'`.

The version in [`version.json`](../../version.json) at the tagged commit is
not the version in the tag. For a manual run, the `expected-version` input
must also match the package version.

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
