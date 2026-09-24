namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// Finds the repository root by its ASL solution file, so tests also run from a git worktree or an
/// exported copy, where <c>.git</c> is a file or absent.
/// </summary>
internal static class RepositoryRoot
{
    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}
