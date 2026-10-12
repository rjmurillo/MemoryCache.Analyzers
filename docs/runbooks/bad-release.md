# Bad release

Use this runbook when a published version of `MemoryCache.Analyzers` is
broken. NuGet does not let you replace a published version, so you hide the
bad one and ship a fixed one.

## 1. Unlist the bad version

1. Sign in to nuget.org as a package owner.
2. Open <https://www.nuget.org/packages/MemoryCache.Analyzers>.
3. Select **Manage package**.
4. Open **Listing**, select the bad version, and clear the listed option.
   Save the change.

Unlisting hides the version from search and from new installs without a
version. It does not delete the version. Projects that already reference it
can still restore it.

## 2. Ship a fixed version

1. Fix the problem on `main` with a normal pull request.
2. Bump the patch version in [`version.json`](../../version.json). For
   example, change `0.2.0` to `0.2.1`.
3. Release the new version with [Release a new version](release.md).

## 3. Record the bad version

In the release pull request, note the bad version in
[`CHANGELOG.md`](../../CHANGELOG.md). Say what was wrong, that it is unlisted,
and which version replaces it.
