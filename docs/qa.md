# Manual QA: try the local package in a sample project

Unit tests check the analyzer in memory. This guide checks the real package
the way a user sees it. You build the package, install it in sample
projects, and confirm when `SRC1000` appears.

No login or secret is needed. The steps work on Linux, macOS, and Windows.
Each step shows bash first. A PowerShell version follows when the commands
differ.

## Before you start

- Install the .NET SDK selected by [`global.json`](../global.json).
- Install the .NET 10 SDK or later. The sample projects live outside the
  repository, so they use the newest SDK on the machine.
- Use a full clone. In a shallow clone, the build fails with `MSB4018`.
  Run `git fetch --unshallow` to fix it.

The sample projects must live outside the repository. Inside it, the
repository's `Directory.Build.props` and analyzers would apply to them.

## Step 1: Build the package

Run this from the repository root:

```shell
REPO_ROOT="$(pwd)"
dotnet build src/MemoryCache.Analyzers/MemoryCache.Analyzers.csproj -c Release
```

PowerShell:

```powershell
$REPO_ROOT = (Get-Location).Path
dotnet build src/MemoryCache.Analyzers/MemoryCache.Analyzers.csproj -c Release
```

The build creates the package in `artifacts/package/release/`.

## Step 2: Find the package file and version

```shell
ls artifacts/package/release/*.nupkg
```

PowerShell:

```powershell
Get-ChildItem artifacts/package/release/*.nupkg
```

Ignore any `*.symbols.nupkg` file. The version is the part of the file name
after `MemoryCache.Analyzers.`. For example,
`MemoryCache.Analyzers.0.1.0.nupkg` has version `0.1.0`. If you see more
than one package from earlier builds, use the newest one.

Save the version for the next steps:

```shell
PKG_VERSION="0.1.0"
```

PowerShell:

```powershell
$PKG_VERSION = "0.1.0"
```

## Step 3: Make a work folder and an isolated NuGet cache

The local version can match a version on nuget.org. A separate NuGet cache
makes sure no cached copy from nuget.org is used.

```shell
QA_DIR="$(mktemp -d)"
export NUGET_PACKAGES="$QA_DIR/nuget-cache"
cd "$QA_DIR"
dotnet new nugetconfig
dotnet nuget add source "$REPO_ROOT/artifacts/package/release" \
  --name local --configfile nuget.config
```

PowerShell:

```powershell
$QA_DIR = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $QA_DIR | Out-Null
$env:NUGET_PACKAGES = Join-Path $QA_DIR "nuget-cache"
Set-Location $QA_DIR
dotnet new nugetconfig
dotnet nuget add source "$REPO_ROOT/artifacts/package/release" `
  --name local --configfile nuget.config
```

## Step 4: Reported case (.NET 10)

Create a console project and add the packages:

```shell
dotnet new console -n QaNetCore -f net10.0
cd QaNetCore
dotnet add package System.Runtime.Caching
dotnet add package MemoryCache.Analyzers --version "$PKG_VERSION"
```

The PowerShell commands are the same.

Confirm that NuGet used the local package. The `source` value must be the
`artifacts/package/release` folder in your clone, not nuget.org:

```shell
cat "$NUGET_PACKAGES/memorycache.analyzers/$PKG_VERSION/.nupkg.metadata"
```

PowerShell:

```powershell
$meta = "memorycache.analyzers/$PKG_VERSION/.nupkg.metadata"
Get-Content (Join-Path $env:NUGET_PACKAGES $meta)
```

If `source` points to nuget.org, the test does not check your build. Delete
`$QA_DIR` and start again from step 3.

Replace `Program.cs` with:

```csharp
using System;
using System.Runtime.Caching;

var cache = new MemoryCache("qa");
Console.WriteLine(cache.Name);
```

Build:

```shell
dotnet build
```

Expected: one `warning SRC1000` line that points to `Program.cs(4,13)`.
The output looks like this:

```text
Program.cs(4,13): warning SRC1000: Do not use
System.Runtime.Caching.MemoryCache in .NET Core or .NET 5+. Use
Microsoft.Extensions.Caching.Memory instead.
```

The real line is longer. It starts with the full path to `Program.cs` and
ends with a link to the rule page and the project path.

## Step 5: Configured case

Add a file named `.editorconfig` in the `QaNetCore` folder:

```ini
root = true

[*.cs]
dotnet_diagnostic.SRC1000.severity = none
```

Build again, and skip the incremental check:

```shell
dotnet build --no-incremental
```

Expected: no `SRC1000` line, and `0 Warning(s)`.

Use `--no-incremental` here. A plain `dotnet build` can skip the compiler
when it thinks the outputs are up to date. Then no warnings print, and the
result proves nothing.

## Step 6: Not-reported case (.NET Framework)

Go back to the work folder and create a `QaNetFx` folder:

```shell
cd "$QA_DIR"
mkdir QaNetFx
cd QaNetFx
```

PowerShell:

```powershell
Set-Location $QA_DIR
New-Item -ItemType Directory -Path QaNetFx | Out-Null
Set-Location QaNetFx
```

Create `QaNetFx.csproj`. Replace `0.1.0` with your package version:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Runtime.Caching" />
    <PackageReference Include="MemoryCache.Analyzers" Version="0.1.0"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Create `Program.cs`:

```csharp
using System;
using System.Runtime.Caching;

class Program
{
    static void Main()
    {
        var cache = new MemoryCache("qa");
        Console.WriteLine(cache.Name);
    }
}
```

Build:

```shell
dotnet build
```

Expected: no `SRC1000` line, and `0 Warning(s)`. SRC1000 does not report
code that targets .NET Framework.

On Linux and macOS, the SDK downloads the .NET Framework reference
assemblies from nuget.org, so the build works. Do not run the program
there. It needs .NET Framework or Mono.

## Step 7: Clean up

Leave the work folder, delete it, and unset the cache variable:

```shell
cd "$REPO_ROOT"
rm -rf "$QA_DIR"
unset NUGET_PACKAGES
```

PowerShell:

```powershell
Set-Location $REPO_ROOT
Remove-Item -Recurse -Force $QA_DIR
Remove-Item Env:NUGET_PACKAGES
```

## Results to expect

| Step | Project | Expected result |
| --- | --- | --- |
| 4 | `QaNetCore`, `net10.0` | `warning SRC1000` in `Program.cs` |
| 5 | `QaNetCore` with `.editorconfig` | No `SRC1000` |
| 6 | `QaNetFx`, `net48` | No `SRC1000` |

These results match the rule. See [SRC1000](rules/SRC1000.md) for when the
rule reports and how to configure it.

## Automated smoke test

A script runs the .NET 10 and .NET Framework cases for you. It needs
PowerShell 7 or later. Build the solution first, then run the script from
the repository root:

```shell
dotnet build MemoryCache.Analyzers.sln
pwsh ./build/scripts/Invoke-PackageSmokeTest.ps1
```

The script reads the package from `artifacts/package/debug/`. For a Release
build, add `-Configuration Release`. The folder must hold exactly one
package, so delete old packages first.

The script creates the sample projects in a temporary folder and uses its
own NuGet cache. It prints a `PASS` or `FAIL` line for each case and
deletes the folder when it ends. A failed case makes the script fail.

CI runs the script after the tests on Windows and Linux.
