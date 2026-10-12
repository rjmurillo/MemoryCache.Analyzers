using BenchmarkDotNet.Running;

namespace MemoryCache.Analyzers.Benchmarks;

/// <summary>
/// Entry point for the analyzer benchmarks. Pass BenchmarkDotNet arguments,
/// such as <c>--filter "*"</c>, to pick which benchmarks run.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the benchmarks selected by <paramref name="args"/>.
    /// </summary>
    /// <param name="args">BenchmarkDotNet command line arguments.</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
