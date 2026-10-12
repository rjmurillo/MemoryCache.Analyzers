namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Checks how <see cref="RepositoryRoot"/> finds the folder that holds the solution file.
/// </summary>
public sealed class RepositoryRootTests
{
    /// <summary>
    /// Verifies that the default search finds the folder that contains the solution file.
    /// </summary>
    [Fact]
    public void FindReturnsFolderThatContainsTheSolution()
    {
        string root = RepositoryRoot.Find();

        Assert.True(File.Exists(Path.Combine(root, RepositoryRoot.SolutionFileName)));
    }

    /// <summary>
    /// Verifies that the search throws when no parent folder contains the solution file.
    /// </summary>
    [Fact]
    public void FindThrowsWhenNoFolderContainsTheSolution()
    {
        string missingSolution = "MissingSolution.sln";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RepositoryRoot.Find(AppContext.BaseDirectory, missingSolution));

        Assert.Contains(missingSolution, exception.Message, StringComparison.Ordinal);
    }
}
