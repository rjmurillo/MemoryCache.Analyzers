using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Keeps the rule documentation in <c>docs/rules</c> in sync with analyzer metadata.
/// </summary>
/// <remarks>
/// Set the <c>UPDATE_RULE_DOCS</c> environment variable to <c>1</c> to rewrite the
/// rules table in <c>docs/rules/README.md</c> instead of checking it.
/// </remarks>
public sealed class RuleDocumentationTests
{
    private static readonly string StartMarker = "<!-- rules-table:start -->";
    private static readonly string EndMarker = "<!-- rules-table:end -->";
    private static readonly string HeaderLine = "| ID | Category | Severity | Code fix | Title | Implementation file |";
    private static readonly string SeparatorLine = "| --- | --- | --- | --- | --- | --- |";
    private static readonly string UpdateVariable = "UPDATE_RULE_DOCS";
    private static readonly string SolutionFileName = "MemoryCache.Analyzers.sln";

    private static Assembly AnalyzerAssembly => typeof(SystemRuntimeCachingAnalyzer).Assembly;

    /// <summary>
    /// Checks that the rules table in <c>docs/rules/README.md</c> matches the
    /// metadata of every analyzer, or rewrites it when <c>UPDATE_RULE_DOCS=1</c>.
    /// </summary>
    [Fact]
    public void RulesTableMatchesAnalyzerMetadata()
    {
        string readmePath = Path.Combine(FindRepositoryRoot(), "docs", "rules", "README.md");
        string expected = BuildRulesTable();
        string actual = File.ReadAllText(readmePath).Replace("\r\n", "\n", StringComparison.Ordinal);

        if (string.Equals(Environment.GetEnvironmentVariable(UpdateVariable), "1", StringComparison.Ordinal))
        {
            File.WriteAllText(readmePath, ReplaceRulesTable(actual, expected));
            return;
        }

        Assert.True(
            actual.Contains(expected, StringComparison.Ordinal),
            "Run with UPDATE_RULE_DOCS=1 to regenerate docs/rules/README.md.");
    }

    /// <summary>
    /// Checks that every diagnostic ID has a rule page at <c>docs/rules/{Id}.md</c>.
    /// </summary>
    [Fact]
    public void EveryRuleHasARulePage()
    {
        string rulesDirectory = Path.Combine(FindRepositoryRoot(), "docs", "rules");

        foreach ((DiagnosticDescriptor descriptor, _) in GetDescriptors())
        {
            string rulePage = Path.Combine(rulesDirectory, descriptor.Id + ".md");
            Assert.True(File.Exists(rulePage), $"Missing rule page: docs/rules/{descriptor.Id}.md");
        }
    }

    private static string FindRepositoryRoot()
    {
        return FindRepositoryRoot(AppContext.BaseDirectory, SolutionFileName);
    }

    private static string FindRepositoryRoot(string start, string solutionFileName)
    {
        DirectoryInfo? directory = new(start);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, solutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find {solutionFileName} above {start}.");
    }

    private static string BuildRulesTable()
    {
        HashSet<string> fixableIds = GetFixableIds();
        StringBuilder builder = new();
        builder.Append(StartMarker).Append('\n');
        builder.Append(HeaderLine).Append('\n');
        builder.Append(SeparatorLine).Append('\n');

        foreach ((DiagnosticDescriptor descriptor, string typeName) in GetDescriptors())
        {
            builder.Append(FormatRow(descriptor, typeName, fixableIds)).Append('\n');
        }

        builder.Append(EndMarker);
        return builder.ToString();
    }

    private static string FormatRow(DiagnosticDescriptor descriptor, string typeName, HashSet<string> fixableIds)
    {
        string id = descriptor.Id;
        string title = descriptor.Title.ToString(CultureInfo.InvariantCulture);
        string codeFix = FormatCodeFix(id, fixableIds);

        return $"| [{id}]({id}.md) | {descriptor.Category} | {descriptor.DefaultSeverity} | {codeFix} | {title} | [{typeName}](../../src/MemoryCache.Analyzers/{typeName}.cs) |";
    }

    private static string FormatCodeFix(string id, HashSet<string> fixableIds)
    {
        return fixableIds.Contains(id) ? "Yes" : "No";
    }

    private static string ReplaceRulesTable(string content, string table)
    {
        int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
        int end = content.IndexOf(EndMarker, StringComparison.Ordinal);

        if (start < 0 || end < start)
        {
            throw new InvalidOperationException($"docs/rules/README.md must contain {StartMarker} followed by {EndMarker}.");
        }

        return content[..start] + table + content[(end + EndMarker.Length)..];
    }

    private static List<(DiagnosticDescriptor Descriptor, string TypeName)> GetDescriptors()
    {
        List<(DiagnosticDescriptor Descriptor, string TypeName)> descriptors = [];

        foreach (Type type in GetConcreteTypes<DiagnosticAnalyzer>())
        {
            if (!type.IsPublic)
            {
                continue;
            }

            DiagnosticAnalyzer analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
            foreach (DiagnosticDescriptor descriptor in analyzer.SupportedDiagnostics)
            {
                descriptors.Add((descriptor, type.Name));
            }
        }

        descriptors.Sort((left, right) => string.CompareOrdinal(left.Descriptor.Id, right.Descriptor.Id));
        return descriptors;
    }

    private static HashSet<string> GetFixableIds()
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (Type type in GetConcreteTypes<CodeFixProvider>())
        {
            CodeFixProvider provider = (CodeFixProvider)Activator.CreateInstance(type)!;
            foreach (string id in provider.FixableDiagnosticIds)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static IEnumerable<Type> GetConcreteTypes<TBase>()
    {
        return AnalyzerAssembly.GetTypes().Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type));
    }
}
