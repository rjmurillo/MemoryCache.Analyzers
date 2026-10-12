using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Tests how <c>CompilationExtensions.IsTargetingNetFramework</c> detects the target framework from the core library.
/// The method is internal, so each test runs <see cref="SystemRuntimeCachingAnalyzer"/> on a real compilation
/// whose core library has a known name, version, or module name.
/// </summary>
public class CompilationExtensionsTests
{
    private static readonly string MinimalCoreLibrarySource =
        """
        namespace System
        {
            public class Object { }
            public abstract class ValueType { }
            public struct Void { }
            public struct Boolean { }
            public struct Int32 { }
            public sealed class String { }
            public abstract class Enum : ValueType { }
            public abstract class Attribute { }
            public enum AttributeTargets { All = 32767 }

            public sealed class AttributeUsageAttribute : Attribute
            {
                public AttributeUsageAttribute(AttributeTargets validOn) { }

                public bool AllowMultiple { get; set; }

                public bool Inherited { get; set; }
            }
        }

        namespace System.Reflection
        {
            public sealed class AssemblyVersionAttribute : Attribute
            {
                public AssemblyVersionAttribute(string version) { }
            }
        }
        """;

    private static readonly string MemoryCacheUsageSource =
        """
        namespace System.Runtime.Caching
        {
            public class MemoryCache
            {
                public MemoryCache(string name) { }
            }
        }

        class C
        {
            void M()
            {
                var cache = new System.Runtime.Caching.MemoryCache("test");
            }
        }
        """;

    [Fact]
    public async Task FlagsCreationWhenCompilationDefinesCoreLibrary()
    {
        CSharpCompilation compilation = CreateCompilation(
            "CoreLibraryWithCache",
            [MinimalCoreLibrarySource, MemoryCacheUsageSource],
            []);

        Diagnostic[] diagnostics = await GetAnalyzerDiagnosticsAsync(compilation);

        Assert.Equal(SystemRuntimeCachingAnalyzer.DiagnosticId, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task FlagsCreationWhenCoreLibraryIsSystemPrivateCoreLib()
    {
        MetadataReference coreLibrary = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        CSharpCompilation compilation = CreateCompilation("UsesPrivateCoreLib", [MemoryCacheUsageSource], [coreLibrary]);

        Diagnostic[] diagnostics = await GetAnalyzerDiagnosticsAsync(compilation);

        Assert.Equal(SystemRuntimeCachingAnalyzer.DiagnosticId, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task DoesNotFlagCreationWhenMscorlibComesFromSource()
    {
        CSharpCompilation coreLibrary = CreateCompilation("mscorlib", [MinimalCoreLibrarySource], []);
        CSharpCompilation compilation = CreateCompilation(
            "UsesSourceMscorlib",
            [MemoryCacheUsageSource],
            [coreLibrary.ToMetadataReference()]);

        Diagnostic[] diagnostics = await GetAnalyzerDiagnosticsAsync(compilation);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task FlagsCreationWhenMscorlibModuleBelongsToNetCoreApp()
    {
        MetadataReference coreLibrary = EmitCoreLibrary(
            MinimalCoreLibrarySource,
            "Microsoft.NETCore.App.mscorlib.dll");
        CSharpCompilation compilation = CreateCompilation("UsesNetCoreMscorlib", [MemoryCacheUsageSource], [coreLibrary]);

        Diagnostic[] diagnostics = await GetAnalyzerDiagnosticsAsync(compilation);

        Assert.Equal(SystemRuntimeCachingAnalyzer.DiagnosticId, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task FlagsCreationWhenMscorlibVersionIsFiveOrLater()
    {
        MetadataReference coreLibrary = EmitCoreLibrary(
            """[assembly: System.Reflection.AssemblyVersion("5.0.0.0")]""" + MinimalCoreLibrarySource,
            "mscorlib.dll");
        CSharpCompilation compilation = CreateCompilation("UsesVersionFiveMscorlib", [MemoryCacheUsageSource], [coreLibrary]);

        Diagnostic[] diagnostics = await GetAnalyzerDiagnosticsAsync(compilation);

        Assert.Equal(SystemRuntimeCachingAnalyzer.DiagnosticId, Assert.Single(diagnostics).Id);
    }

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        IEnumerable<string> sources,
        IEnumerable<MetadataReference> references,
        string? moduleName = null)
    {
        CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary, moduleName: moduleName);
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source)),
            references,
            options);

        Assert.DoesNotContain(compilation.GetDiagnostics().ToArray(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        return compilation;
    }

    private static MetadataReference EmitCoreLibrary(string source, string moduleName)
    {
        CSharpCompilation compilation = CreateCompilation("mscorlib", [source], [], moduleName);

        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(
            stream,
            options: new Microsoft.CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.0.30319"));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.ToString())));

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(CSharpCompilation compilation)
    {
        CompilationWithAnalyzers analyzers = compilation.WithAnalyzers([new SystemRuntimeCachingAnalyzer()]);

        ImmutableArray<Diagnostic> diagnostics = await analyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).ConfigureAwait(false);

        return diagnostics.ToArray();
    }
}
