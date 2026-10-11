namespace MemoryCache.Analyzers.Tests.Helpers;

/// <summary>
/// Provides a centralized, thread-safe catalog of commonly used <see cref="ReferenceAssemblies"/> configurations for analyzer tests.
/// This class ensures that reference assembly resolution and NuGet package downloads are performed only once per configuration,
/// improving test performance and reliability. It is designed to work around Visual Studio Test Explorer limitations by exposing
/// primitive string keys for each configuration, allowing test cases to remain distinct and debuggable.
/// </summary>
/// <remarks>
/// Use this catalog to retrieve preconfigured <see cref="ReferenceAssemblies"/> instances for different .NET targets and caching packages
/// when writing analyzer or code fix tests. This avoids redundant package resolution and supports clear test case reporting.
/// </remarks>
public static class ReferenceAssemblyCatalog
{
    /// <summary>
    /// Gets the identifier for .NET 8.0 reference assemblies with the System.Runtime.Caching package added.
    /// Use this for tests targeting .NET 8 with System.Runtime.Caching APIs.
    /// </summary>
    public static string Net80WithSystemRuntimeCaching => nameof(Net80WithSystemRuntimeCaching);

    /// <summary>
    /// Gets the identifier for .NET Standard 2.0 reference assemblies with the System.Runtime.Caching package added.
    /// Use this for tests targeting .NET Standard 2.0 with System.Runtime.Caching APIs.
    /// </summary>
    public static string NetStandard20WithSystemRuntimeCaching => nameof(NetStandard20WithSystemRuntimeCaching);

    /// <summary>
    /// Gets the identifier for .NET Framework 4.6.2 reference assemblies with the System.Runtime.Caching package added.
    /// Use this for tests targeting .NET Framework 4.6.2 with System.Runtime.Caching APIs.
    /// </summary>
    public static string NetFramework462WithSystemRuntimeCaching => nameof(NetFramework462WithSystemRuntimeCaching);

    /// <summary>
    /// Gets the identifier for .NET 8.0 reference assemblies with the Microsoft.Extensions.Caching.Memory package added.
    /// Use this for tests targeting .NET 8 with Microsoft.Extensions.Caching.Memory APIs.
    /// </summary>
    public static string Net80WithMicrosoftExtensionsCachingMemory => nameof(Net80WithMicrosoftExtensionsCachingMemory);

    /// <summary>
    /// Gets the identifier for .NET Framework 4.6.2 reference assemblies with the Microsoft.Extensions.Caching.Memory package added.
    /// Use this for tests targeting .NET Framework 4.6.2 with Microsoft.Extensions.Caching.Memory APIs.
    /// </summary>
    public static string NetFramework462WithMicrosoftExtensionsCachingMemory => nameof(NetFramework462WithMicrosoftExtensionsCachingMemory);

    /// <summary>
    /// Gets a catalog of reference assemblies grouped by their respective identifiers.
    /// Use this dictionary to retrieve a <see cref="ReferenceAssemblies"/> instance for a given configuration key.
    /// </summary>
    /// <remarks>
    /// The catalog provides a mapping between string identifiers and corresponding
    /// <see cref="Microsoft.CodeAnalysis.Testing.ReferenceAssemblies"/> instances.
    /// It is used to configure the reference assemblies for analyzer tests.
    /// </remarks>
    /// <value>
    /// A read-only dictionary where the keys are string identifiers and the values are
    /// <see cref="Microsoft.CodeAnalysis.Testing.ReferenceAssemblies"/> instances.
    /// </value>
    public static IReadOnlyDictionary<string, ReferenceAssemblies> Catalog { get; } =
        new Dictionary<string, ReferenceAssemblies>(StringComparer.Ordinal)
        {
            {
                nameof(Net80WithSystemRuntimeCaching),
                ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("System.Runtime.Caching", "8.0.0")])
            },
            {
                nameof(NetStandard20WithSystemRuntimeCaching),
                ReferenceAssemblies.NetStandard.NetStandard20.AddPackages([new PackageIdentity("System.Runtime.Caching", "8.0.0")])
            },
            {
                nameof(NetFramework462WithSystemRuntimeCaching),
                ReferenceAssemblies.NetFramework.Net462.Default.AddPackages([new PackageIdentity("System.Runtime.Caching", "8.0.0")])
            },
            {
                nameof(Net80WithMicrosoftExtensionsCachingMemory),
                ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Microsoft.Extensions.Caching.Memory", "8.0.0")])
            },
            {
                nameof(NetFramework462WithMicrosoftExtensionsCachingMemory),
                ReferenceAssemblies.NetFramework.Net462.Default.AddPackages([new PackageIdentity("Microsoft.Extensions.Caching.Memory", "8.0.0")])
            },
        };
}
