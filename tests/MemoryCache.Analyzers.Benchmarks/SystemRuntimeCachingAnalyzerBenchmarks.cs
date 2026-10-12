using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace MemoryCache.Analyzers.Benchmarks;

/// <summary>
/// Measures the time and memory that <see cref="SystemRuntimeCachingAnalyzer"/> uses on a generated compilation.
/// </summary>
/// <remarks>
/// The analyzer registers its operation action only when <c>System.Runtime.Caching.MemoryCache</c> resolves.
/// The baseline measures a compilation without that reference, so the analyzer does no per-operation work.
/// The second benchmark measures a compilation where every method creates a <c>MemoryCache</c>.
/// </remarks>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SystemRuntimeCachingAnalyzerBenchmarks
{
    private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers =
        ImmutableArray.Create<DiagnosticAnalyzer>(new SystemRuntimeCachingAnalyzer());

    private Compilation? _withCaching;
    private Compilation? _withoutCaching;

    /// <summary>
    /// Gets the file counts that BenchmarkDotNet runs for <see cref="FileCount"/>.
    /// </summary>
    public static IEnumerable<int> FileCounts => [10, 100];

    /// <summary>
    /// Gets or sets the number of generated source files in each compilation.
    /// Each file holds one class with ten methods.
    /// </summary>
    [ParamsSource(nameof(FileCounts))]
    public int FileCount { get; set; }

    private static int MethodsPerFile => 10;

    /// <summary>
    /// Builds both compilations and checks that the analyzer reports the expected diagnostics.
    /// </summary>
    /// <returns>A task that completes when setup is done.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the analyzer does not report exactly one <c>SRC1000</c> diagnostic per method.
    /// </exception>
    [GlobalSetup]
    public async Task SetupAsync()
    {
        ImmutableArray<MetadataReference> cachingReferences = await ReferenceAssemblies.Net.Net80
            .AddPackages([new PackageIdentity("System.Runtime.Caching", "8.0.0")])
            .ResolveAsync(LanguageNames.CSharp, CancellationToken.None)
            .ConfigureAwait(false);
        ImmutableArray<MetadataReference> plainReferences = await ReferenceAssemblies.Net.Net80
            .ResolveAsync(LanguageNames.CSharp, CancellationToken.None)
            .ConfigureAwait(false);

        _withCaching = CreateCompilation("WithCaching", "new System.Runtime.Caching.MemoryCache(\"name\")", cachingReferences);
        _withoutCaching = CreateCompilation("WithoutCaching", "new object()", plainReferences);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(_withCaching).ConfigureAwait(false);
        int expected = FileCount * MethodsPerFile;
        int actual = CountRuleDiagnostics(diagnostics);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                "Expected " + expected.ToString(CultureInfo.InvariantCulture) + " " + SystemRuntimeCachingAnalyzer.DiagnosticId
                + " diagnostics but found " + actual.ToString(CultureInfo.InvariantCulture) + ". The benchmark setup is broken.");
        }
    }

    /// <summary>
    /// Runs the analyzer on a compilation that does not reference <c>System.Runtime.Caching</c>.
    /// </summary>
    /// <returns>The analyzer diagnostics.</returns>
    [Benchmark(Baseline = true)]
    public Task<ImmutableArray<Diagnostic>> WithoutSystemRuntimeCaching()
    {
        return AnalyzeAsync(_withoutCaching ?? throw new InvalidOperationException("Run setup first."));
    }

    /// <summary>
    /// Runs the analyzer on a compilation where every method creates a <c>MemoryCache</c>.
    /// </summary>
    /// <returns>The analyzer diagnostics.</returns>
    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> WithSystemRuntimeCaching()
    {
        return AnalyzeAsync(_withCaching ?? throw new InvalidOperationException("Run setup first."));
    }

    private static int CountRuleDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        int count = 0;
        foreach (Diagnostic diagnostic in diagnostics)
        {
            count += string.Equals(diagnostic.Id, SystemRuntimeCachingAnalyzer.DiagnosticId, StringComparison.Ordinal) ? 1 : 0;
        }

        return count;
    }

    private static string CreateSource(string fileName, string creationExpression)
    {
        StringBuilder builder = new();
        builder.Append("namespace Generated").Append(fileName).AppendLine(";");
        builder.Append("public class Class").AppendLine(fileName);
        builder.AppendLine("{");
        for (int method = 0; method < MethodsPerFile; method++)
        {
            builder.Append("    public object Method").Append(method.ToString(CultureInfo.InvariantCulture))
                .Append("() => ").Append(creationExpression).AppendLine(";");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Compilation compilation)
    {
        return compilation.WithAnalyzers(_analyzers).GetAnalyzerDiagnosticsAsync();
    }

    private Compilation CreateCompilation(string assemblyName, string creationExpression, ImmutableArray<MetadataReference> references)
    {
        IEnumerable<SyntaxTree> syntaxTrees = Enumerable
            .Range(0, FileCount)
            .Select(fileIndex => fileIndex.ToString(CultureInfo.InvariantCulture))
            .Select(fileName => CSharpSyntaxTree.ParseText(CreateSource(fileName, creationExpression), path: "Class" + fileName + ".cs"));

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references.AsSpan().ToArray(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
