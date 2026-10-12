using System.Diagnostics;

namespace MemoryCache.Analyzers;

internal static class CompilationExtensions
{
    /// <summary>
    /// Determines whether the provided compilation is targeting the classic .NET Framework.
    /// </summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the compilation targets the .NET Framework;
    /// <see langword="false"/> if it targets .NET Core, .NET 5+, or .NET 8+;
    /// <see langword="null"/> if the target framework cannot be determined.
    /// </returns>
    public static bool? IsTargetingNetFramework(this Compilation compilation)
    {
        INamedTypeSymbol objectType = compilation.GetSpecialType(SpecialType.System_Object);
        IAssemblySymbol coreAssembly = objectType.ContainingAssembly;

        AssemblyIdentity identity = coreAssembly.Identity;

        // .NET Core/5+/8+ always use System.Private.CoreLib for System.Object
        if (SymbolEqualityComparer.Default.Equals(coreAssembly, compilation.Assembly) || string.Equals(identity.Name, "System.Private.CoreLib", StringComparison.Ordinal))
        {
            return false;
        }

        // .NET Framework uses mscorlib for System.Object
        if (string.Equals(identity.Name, "mscorlib", StringComparison.Ordinal))
        {
            // Heuristic: check for .NET Core mscorlib (should not happen, but possible in test harnesses)
            Debug.Assert(!coreAssembly.Locations.IsEmpty, "An assembly that defines System.Object always has a location.");
            string? moduleName = coreAssembly.Locations[0].MetadataModule?.Name;
            if (!string.IsNullOrEmpty(moduleName) && moduleName.Contains("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (identity.Version.Major >= 5)
            {
                return false;
            }

            return true;
        }

        // Unknown/ambiguous
        return null;
    }
}
