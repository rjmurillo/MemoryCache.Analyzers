# Install MemoryCache.Analyzers

The current stable version of MemoryCache.Analyzers is `0.1.0`.

## Requirements

- .NET SDK 8.0.100 or later for command line builds.
- Visual Studio 2022 version 17.8 or later for editor diagnostics.
- An SDK-style C# project.

The analyzer targets .NET Standard 2.0 and uses Roslyn 4.8. The .NET 8.0.100 SDK and Visual Studio 2022 version
17.8 include Roslyn 4.8.

## Install in one project

Run this command in the folder that contains the project file:

```shell
dotnet add package MemoryCache.Analyzers
```

To pin the current version in a project file, use:

```xml
<ItemGroup>
  <PackageReference Include="MemoryCache.Analyzers" Version="0.1.0" PrivateAssets="all" />
</ItemGroup>
```

Keep `PrivateAssets="all"` on references that you write by hand. It prevents the analyzer package from becoming a
dependency of packages produced by your project or flowing to projects that reference your project.

## Install in every project under a folder

Add the package reference to a `Directory.Build.props` file. MSBuild imports this file into projects in the same
folder and its subfolders:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="MemoryCache.Analyzers" Version="0.1.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Place the file at the repository root to cover every project, or in a narrower folder to cover only those projects.
If a project has a closer `Directory.Build.props` file, MSBuild uses the closer file and does not automatically merge
the files.

## Use central package management

With NuGet central package management, declare the version in `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MemoryCache.Analyzers" Version="0.1.0" />
  </ItemGroup>
</Project>
```

Then add a versionless reference to `Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="MemoryCache.Analyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

The `PackageReference` must not contain a version when central package management is enabled. Keep
`PrivateAssets="all"` on the `PackageReference`.

## Check that it works

For a .NET 8 project that references `System.Runtime.Caching`, add this code:

```csharp
using System.Runtime.Caching;

MemoryCache cache = new("example");
```

Force the compiler to run:

```shell
dotnet build --no-incremental
```

The build should report `warning SRC1000` on the `new MemoryCache` expression. If no warning appears, check that:

- The project restored `MemoryCache.Analyzers` version `0.1.0`.
- The project references `System.Runtime.Caching`.
- The project targets .NET Core or .NET 5 or later. SRC1000 is silent on .NET Framework.
- The package reference applies to the project and has not been hidden by a closer `Directory.Build.props` file.
- The build compiled the project. `--no-incremental` avoids an up-to-date build that has no compiler output.

## References

- [dotnet package add](https://learn.microsoft.com/dotnet/core/tools/dotnet-package-add)
- [PackageReference in project files](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files)
- [Central package management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [Roslyn version support](https://learn.microsoft.com/visualstudio/extensibility/roslyn-version-support)
- [.NET SDK, MSBuild, and Visual Studio versioning](https://learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs)
