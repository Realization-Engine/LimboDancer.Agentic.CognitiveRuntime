namespace LimboDancer.Domains.Asl.Authoring.Tests;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(Environment.GetEnvironmentVariable("ASL_REPO") is { Length: > 0 } repo ? repo : AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if ((Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
                && Directory.Exists(Path.Combine(directory.FullName, "docs", "ASL")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
