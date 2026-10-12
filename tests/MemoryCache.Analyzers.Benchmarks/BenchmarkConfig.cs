using BenchmarkDotNet.Configs;

namespace MemoryCache.Analyzers.Benchmarks;

/// <summary>
/// BenchmarkDotNet configuration for the analyzer benchmarks.
/// </summary>
/// <remarks>
/// It turns off the optimizations validator. The
/// <c>Microsoft.CodeAnalysis.Analyzer.Testing</c> package ships a non-optimized build, and the validator
/// refuses to run any benchmark that references it. The benchmarks use that package only in
/// <c>[GlobalSetup]</c> to resolve reference assemblies, so it does not affect the measured code.
/// </remarks>
public class BenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenchmarkConfig"/> class.
    /// </summary>
    public BenchmarkConfig()
    {
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
