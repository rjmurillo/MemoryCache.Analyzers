using Verifier = MemoryCache.Analyzers.Tests.Helpers.AnalyzerVerifier<MemoryCache.Analyzers.SystemRuntimeCachingAnalyzer>;

namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Unit tests for <see cref="SystemRuntimeCachingAnalyzer"/>.
/// </summary>
public class SystemRuntimeCachingAnalyzerTests
{
    public static IEnumerable<object[]> MemoryCacheTestData()
    {
        IEnumerable<object[]> net80SystemRuntimeCaching = new object[][]
        {
            ["""var cache = {|SRC1000:new MemoryCache("test")|};"""],
        }.WithSystemRuntimeCachingNamespaces().WithNet80SystemRuntimeCaching();

        IEnumerable<object[]> net80MicrosoftExtensionsCachingMemory = new object[][]
        {
            ["""var cache = new MemoryCache(new MemoryCacheOptions());"""],
        }.WithMicrosoftExtensionsCachingMemoryNamespaces().WithNet80MicrosoftExtensionsCachingMemory();

        IEnumerable<object[]> framework462SystemRuntimeCaching = new object[][]
        {
            ["""var cache = new MemoryCache("test");"""],
        }.WithSystemRuntimeCachingNamespaces().WithFramework462SystemRuntimeCaching();

        IEnumerable<object[]> framework462MicrosoftExtensionsCachingMemory = new object[][]
        {
            ["""var cache = new MemoryCache(new MemoryCacheOptions());"""],
        }.WithMicrosoftExtensionsCachingMemoryNamespaces().WithFramework462MicrosoftExtensionsCachingMemory();

        return net80SystemRuntimeCaching
            .Union(net80MicrosoftExtensionsCachingMemory)
            .Union(framework462SystemRuntimeCaching)
            .Union(framework462MicrosoftExtensionsCachingMemory);
    }

    [Fact]
    public void HelpLinkTargetsRuleDocumentation()
    {
        Microsoft.CodeAnalysis.DiagnosticDescriptor diagnostic = Assert.Single(new SystemRuntimeCachingAnalyzer().SupportedDiagnostics);

        Assert.StartsWith("https://github.com/rjmurillo/MemoryCache.Analyzers/blob/", diagnostic.HelpLinkUri, StringComparison.Ordinal);
        Assert.EndsWith("/docs/rules/SRC1000.md", diagnostic.HelpLinkUri, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(MemoryCacheTestData))]
    public async Task FlagsMemoryCacheUsageInNetCoreProject(string referenceAssemblyGroup, string @namespace, string code)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              class C
              {
                  void M()
                  {
                      {{code}}
                  }

              }
              """,
            referenceAssemblyGroup);
    }
}
