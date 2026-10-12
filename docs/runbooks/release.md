# Release a new version

Use this runbook to publish a new version of `MemoryCache.Analyzers` to NuGet.
In the commands, replace `X.Y.Z` with the new version, such as `0.2.0`.

The package version comes from [`version.json`](../../version.json).
Nerdbank.GitVersioning reads it at build time. The
[`Release publish`](../../.github/workflows/release.yml) workflow builds the
package and pushes it to nuget.org.

## 1. Open a release pull request

Create a branch from `main` and make these changes:

1. Set `"version"` in [`version.json`](../../version.json) to `X.Y.Z`.
2. Add a `## [X.Y.Z] - YYYY-MM-DD` section to
   [`CHANGELOG.md`](../../CHANGELOG.md). Add a link for it at the bottom of
   the file.
3. Move released rules from
   [`AnalyzerReleases.Unshipped.md`](../../src/MemoryCache.Analyzers/AnalyzerReleases.Unshipped.md)
   to a new `## Release X.Y.Z` section in
   [`AnalyzerReleases.Shipped.md`](../../src/MemoryCache.Analyzers/AnalyzerReleases.Shipped.md).
   Leave the header comment lines in the unshipped file. Skip this step if
   the unshipped file has no rules.

Open a pull request with these changes.

## 2. Merge after CI passes

Wait for every required check to pass. Then merge the pull request into
`main`.

## 3. Tag and create the release

Pull the merge commit and tag it:

```shell
git switch main
git pull
git tag vX.Y.Z
git push origin vX.Y.Z
```

Pushing the tag starts the `Release publish` workflow. Next, create the
GitHub release with notes generated from pull request labels:

```shell
gh release create vX.Y.Z --generate-notes --verify-tag
```

`--verify-tag` stops the command if the tag is not on GitHub yet. That
prevents a release on the wrong commit. See
[Release notes](../../CONTRIBUTING.md#release-notes) for how labels group the
notes.

Pushing only the tag also publishes the package. The GitHub release adds the
notes.

## 4. Approve the publish job

1. Open the repository's **Actions** tab.
2. Open the newest `Release publish` run for `vX.Y.Z`.
3. Wait for the `build` job to pass. The `publish` job then waits for
   approval in the `release` environment.
4. Select **Review deployments**, check `release`, and select
   **Approve and deploy**. Only a required reviewer can approve.

A tag push and a published release each start a run. The runs share a
concurrency group, so they run one at a time. The second run finds the
package on NuGet and skips the push because of `--skip-duplicate`.

## 5. Check NuGet

Open <https://www.nuget.org/packages/MemoryCache.Analyzers> and look for
version `X.Y.Z`. NuGet indexing can take some time, so the new version may not
show at once.

If the publish job fails, follow [Publish failed](publish-failed.md).
