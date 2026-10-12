#Requires -Version 7.0

<#
.SYNOPSIS
Installs the built MemoryCache.Analyzers package in two sample projects and checks SRC1000.

.DESCRIPTION
The unit tests run the analyzer in memory. This script checks the real
package the way a user sees it. It finds the built .nupkg, creates two
projects in a temporary folder, installs the package, and builds them.

- Project A targets net10.0. The build must report warning SRC1000.
- Project B targets net48. The build must not report SRC1000.

The projects live outside the repository, so the repository's
Directory.Build.props does not apply. A private NUGET_PACKAGES folder makes
sure NuGet uses the local package, even when nuget.org has the same version.
The script deletes the temporary folder when it finishes.

.PARAMETER Configuration
The build configuration that produced the package. The script reads the
package from artifacts/package/<lowercase configuration>.

.EXAMPLE
dotnet build MemoryCache.Analyzers.sln
pwsh ./build/scripts/Invoke-PackageSmokeTest.ps1

.EXAMPLE
pwsh ./build/scripts/Invoke-PackageSmokeTest.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string] $Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$PackageId = 'MemoryCache.Analyzers'
$CachingVersion = '10.0.0'

function Get-SmokePackage {
    param([Parameter(Mandatory)] [string] $PackageDirectory)

    if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
        throw "Package folder '$PackageDirectory' does not exist. Build the solution first."
    }

    $packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter "$PackageId.*.nupkg" -File |
        Where-Object { $_.Name -notlike '*.symbols.*' })

    if ($packages.Count -eq 0) {
        throw "No $PackageId package found in '$PackageDirectory'. Build the solution first."
    }

    if ($packages.Count -gt 1) {
        $names = ($packages | ForEach-Object Name) -join ', '
        throw "Expected exactly one $PackageId package in '$PackageDirectory', found $($packages.Count): $names. Delete old packages and build again."
    }

    return $packages[0]
}

function Get-SmokePackageVersion {
    param([Parameter(Mandatory)] [System.IO.FileInfo] $Package)

    $stem = $Package.BaseName.Substring($PackageId.Length + 1)
    if ($stem -notmatch '(\d+\.\d+\.\d+.*)$') {
        throw "Cannot parse a version from package file name '$($Package.Name)'."
    }

    return $Matches[1]
}

function Invoke-DotNet {
    param([Parameter(Mandatory)] [string[]] $Arguments)

    $output = & dotnet @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Information $output
        throw "'dotnet $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Initialize-SmokeProject {
    param(
        [Parameter(Mandatory)] [string] $Directory,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $TargetFramework,
        [Parameter(Mandatory)] [string] $CachingReference,
        [Parameter(Mandatory)] [string] $PackageVersion
    )

    $projectDirectory = Join-Path $Directory $Name
    New-Item -ItemType Directory -Path $projectDirectory | Out-Null

    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$TargetFramework</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    $CachingReference
    <PackageReference Include="$PackageId" Version="$PackageVersion" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@

    $program = @'
using System;
using System.Runtime.Caching;

class Program
{
    static void Main()
    {
        var cache = new MemoryCache("smoke");
        Console.WriteLine(cache.Name);
    }
}
'@

    $projectPath = Join-Path $projectDirectory "$Name.csproj"
    Set-Content -LiteralPath $projectPath -Value $project
    Set-Content -LiteralPath (Join-Path $projectDirectory 'Program.cs') -Value $program
    return $projectPath
}

function Test-SmokeCase {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $ProjectPath,
        [Parameter(Mandatory)] [bool] $ExpectWarning
    )

    $output = & dotnet build $ProjectPath --disable-build-servers -tl:off -nologo 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    $pattern = if ($ExpectWarning) { 'warning SRC1000' } else { 'SRC1000' }
    $found = $output -match $pattern

    $failure = $null
    if ($exitCode -ne 0) {
        $failure = "dotnet build failed with exit code $exitCode"
    }
    elseif ($found -ne $ExpectWarning) {
        $failure = if ($ExpectWarning) { "output does not match '$pattern'" } else { "output matches '$pattern'" }
    }

    if ($null -eq $failure) {
        Write-Information "PASS: $Name"
        return $true
    }

    Write-Information $output
    Write-Information "FAIL: $Name ($failure)"
    return $false
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packageDirectory = Join-Path -Path $repoRoot -ChildPath 'artifacts' -AdditionalChildPath 'package', $Configuration.ToLowerInvariant()
$package = Get-SmokePackage -PackageDirectory $packageDirectory
$version = Get-SmokePackageVersion -Package $package
Write-Information "Package: $($package.FullName)"
Write-Information "Version: $version"

$tempRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$workDirectory = Join-Path $tempRoot ([guid]::NewGuid().ToString('N'))
$originalNuGetPackages = $env:NUGET_PACKAGES

try {
    New-Item -ItemType Directory -Path $workDirectory | Out-Null
    $env:NUGET_PACKAGES = Join-Path $workDirectory 'nuget-packages'

    $configPath = Join-Path $workDirectory 'nuget.config'
    Invoke-DotNet @('new', 'nugetconfig', '--output', $workDirectory) | Out-Null
    Invoke-DotNet @('nuget', 'add', 'source', $packageDirectory, '--name', 'local', '--configfile', $configPath) | Out-Null

    $projectA = Initialize-SmokeProject -Directory $workDirectory -Name 'SmokeNetCore' -TargetFramework 'net10.0' `
        -CachingReference "<PackageReference Include=`"System.Runtime.Caching`" Version=`"$CachingVersion`" />" `
        -PackageVersion $version
    $projectB = Initialize-SmokeProject -Directory $workDirectory -Name 'SmokeNetFx' -TargetFramework 'net48' `
        -CachingReference '<Reference Include="System.Runtime.Caching" />' `
        -PackageVersion $version

    $passA = Test-SmokeCase -Name 'Project A (net10.0) reports warning SRC1000' -ProjectPath $projectA -ExpectWarning $true
    $passB = Test-SmokeCase -Name 'Project B (net48) does not report SRC1000' -ProjectPath $projectB -ExpectWarning $false

    if (-not ($passA -and $passB)) {
        throw 'Package smoke test failed. See the FAIL lines above.'
    }

    Write-Information 'PASS: Package smoke test'
}
finally {
    $env:NUGET_PACKAGES = $originalNuGetPackages
    if (Test-Path -LiteralPath $workDirectory) {
        Remove-Item -LiteralPath $workDirectory -Recurse -Force
    }
}
