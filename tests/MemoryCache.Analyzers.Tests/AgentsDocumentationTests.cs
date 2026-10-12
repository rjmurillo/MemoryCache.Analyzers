using System.Text.RegularExpressions;

namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Keeps <c>AGENTS.md</c> in sync with the repository. The checks read the
/// file content, so new sections and new references are checked without
/// changes to this class.
/// </summary>
public sealed partial class AgentsDocumentationTests
{
    /// <summary>
    /// Checks that every file or folder named in inline code in <c>AGENTS.md</c>
    /// exists. A span with a <c>/</c> is relative to the repository root. A bare
    /// file name, such as <c>CodeAnalysis.targets</c>, may live in any folder.
    /// </summary>
    [Fact]
    public void ReferencedPathsExist()
    {
        string root = RepositoryRoot.Find();
        IReadOnlyList<string> paths = ExtractReferencedPaths(ReadRepositoryFile(root, "AGENTS.md"));

        string[] missing = paths.Distinct(StringComparer.Ordinal).Where(path => !PathExists(root, path)).ToArray();

        Assert.NotEmpty(paths);
        Assert.True(
            missing.Length == 0,
            $"AGENTS.md references paths that do not exist: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Checks that every <c>dotnet</c> command in a code block in <c>AGENTS.md</c>
    /// also appears in a code block in <c>CONTRIBUTING.md</c>.
    /// </summary>
    [Fact]
    public void VerificationCommandsMatchContributing()
    {
        string root = RepositoryRoot.Find();
        IReadOnlyList<string> agentCommands = ExtractDotnetCommands(ReadRepositoryFile(root, "AGENTS.md"));
        HashSet<string> contributingCommands =
            new(ExtractDotnetCommands(ReadRepositoryFile(root, "CONTRIBUTING.md")), StringComparer.Ordinal);

        string[] unmatched = agentCommands.Where(command => !contributingCommands.Contains(command)).ToArray();

        Assert.NotEmpty(agentCommands);
        Assert.True(
            unmatched.Length == 0,
            $"AGENTS.md commands missing from CONTRIBUTING.md: {string.Join(" | ", unmatched)}");
    }

    /// <summary>
    /// Checks which inline code spans count as paths, and how they are normalized.
    /// </summary>
    /// <param name="markdown">The markdown to scan.</param>
    /// <param name="expected">The expected paths, joined with <c>|</c>.</param>
    [Theory]
    [InlineData("Read `CLAUDE.md` and `docs/rules/`.", "CLAUDE.md|docs/rules/")]
    [InlineData("Run `./build/targets/tests/test.runsettings`.", "build/targets/tests/test.runsettings")]
    [InlineData("Targets `netstandard2.0` and `SRC1000-1099`.", "")]
    [InlineData("Use `a.yml` or `Foo.cs`.", "Foo.cs")]
    [InlineData("```shell\n`inside/fence.md`\n```\n`outside.md`", "outside.md")]
    public void ExtractReferencedPathsKeepsOnlyPaths(string markdown, string expected)
    {
        string actual = string.Join('|', ExtractReferencedPaths(markdown));

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Checks that only trimmed <c>dotnet</c> lines inside code blocks are collected.
    /// </summary>
    [Fact]
    public void ExtractDotnetCommandsReadsOnlyCodeBlocks()
    {
        string markdown = "dotnet outside\r\n```shell\r\n  dotnet build x.sln  \r\ngit status\r\n```\r\ndotnet after";

        Assert.Equal(["dotnet build x.sln"], ExtractDotnetCommands(markdown));
    }

    /// <summary>
    /// Checks that folders, root files, and bare file names in nested folders resolve.
    /// </summary>
    /// <param name="path">A path that exists in the repository.</param>
    [Theory]
    [InlineData("docs/rules/")]
    [InlineData("docs/rules/README.md")]
    [InlineData("CONTRIBUTING.md")]
    [InlineData("CodeAnalysis.targets")]
    public void PathExistsFindsExistingPaths(string path)
    {
        Assert.True(PathExists(RepositoryRoot.Find(), path));
    }

    /// <summary>
    /// Checks that missing root-relative paths and missing bare file names do not resolve.
    /// </summary>
    /// <param name="path">A path that does not exist in the repository.</param>
    [Theory]
    [InlineData("docs/rules/INDEX.md")]
    [InlineData("Missing.props")]
    public void PathExistsRejectsMissingPaths(string path)
    {
        Assert.False(PathExists(RepositoryRoot.Find(), path));
    }

    private static string ReadRepositoryFile(string root, string name)
    {
        return File.ReadAllText(Path.Combine(root, name));
    }

    private static List<string> ExtractReferencedPaths(string markdown)
    {
        List<string> paths = [];

        foreach (Match match in InlineCodePattern().Matches(RemoveCodeBlocks(markdown)))
        {
            string span = match.Groups["path"].Value;
            if (IsPath(span))
            {
                paths.Add(span.StartsWith("./", StringComparison.Ordinal) ? span[2..] : span);
            }
        }

        return paths;
    }

    private static bool IsPath(string span)
    {
        return span.Contains('/', StringComparison.Ordinal)
            || Path.GetExtension(span) is ".md" or ".json" or ".props" or ".targets" or ".sln" or ".csproj" or ".cs" or ".runsettings";
    }

    private static List<string> ExtractDotnetCommands(string markdown)
    {
        return ReadLines(markdown)
            .Where(line => line.InCodeBlock)
            .Select(line => line.Text.Trim())
            .Where(text => text.StartsWith("dotnet ", StringComparison.Ordinal))
            .ToList();
    }

    private static string RemoveCodeBlocks(string markdown)
    {
        IEnumerable<string> outside = ReadLines(markdown)
            .Where(line => !line.InCodeBlock)
            .Select(line => line.Text);

        return string.Join('\n', outside);
    }

    private static IEnumerable<(string Text, bool InCodeBlock)> ReadLines(string markdown)
    {
        bool inCodeBlock = false;

        foreach (string rawLine in markdown.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            yield return (line, inCodeBlock);
        }
    }

    private static bool PathExists(string root, string path)
    {
        string fullPath = Path.Combine(root, path);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            return true;
        }

        return !path.Contains('/', StringComparison.Ordinal) && FileExistsBelow(root, path);
    }

    private static bool FileExistsBelow(string directory, string fileName)
    {
        return File.Exists(Path.Combine(directory, fileName))
            || Directory.EnumerateDirectories(directory)
                .Where(child => !IsSkippedFolder(Path.GetFileName(child)))
                .Any(child => FileExistsBelow(child, fileName));
    }

    private static bool IsSkippedFolder(string name)
    {
        return name is ".git" or "artifacts" or "bin" or "obj" or "node_modules";
    }

    [GeneratedRegex(@"`(?<path>[^`\s]+)`", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InlineCodePattern();
}
