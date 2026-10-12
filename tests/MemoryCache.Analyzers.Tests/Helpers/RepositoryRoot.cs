namespace MemoryCache.Analyzers.Tests.Helpers;

/// <summary>
/// Finds the repository root so tests can read checked-in files, such as
/// <c>AGENTS.md</c> and the rule pages in <c>docs/rules</c>.
/// </summary>
public static class RepositoryRoot
{
    /// <summary>
    /// The solution file that marks the repository root.
    /// </summary>
    public static readonly string SolutionFileName = "MemoryCache.Analyzers.sln";

    /// <summary>
    /// Finds the repository root, starting from the test output folder.
    /// </summary>
    /// <returns>The full path of the folder that contains <see cref="SolutionFileName"/>.</returns>
    /// <exception cref="InvalidOperationException">No parent folder contains the solution file.</exception>
    public static string Find()
    {
        return Find(AppContext.BaseDirectory, SolutionFileName);
    }

    /// <summary>
    /// Walks up from <paramref name="start"/> until a folder contains
    /// <paramref name="solutionFileName"/>.
    /// </summary>
    /// <param name="start">The folder where the search starts.</param>
    /// <param name="solutionFileName">The file name that marks the root.</param>
    /// <returns>The full path of the first folder that contains the file.</returns>
    /// <exception cref="InvalidOperationException">The search reached the file system root.</exception>
    public static string Find(string start, string solutionFileName)
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
}
