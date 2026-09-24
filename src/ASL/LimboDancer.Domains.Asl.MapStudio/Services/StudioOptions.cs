namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Studio configuration under <c>AslMaps</c> (Architecture and Rendering Design, section 4.2).</summary>
public sealed class StudioOptions
{
    public string? VaslRoot
    {
        get; set;
    }

    /// <summary>Directory of F2 oracle fixtures; defaults to the repository's test fixtures when found.</summary>
    public string? OracleFixtures
    {
        get; set;
    }

    /// <summary>Local cache for fidelity reports; defaults to a folder under the user's local application data, outside the repository.</summary>
    public string? CacheRoot
    {
        get; set;
    }

    public string? ResolveOracleFixtures() => OracleFixtures ?? FindRepositoryFixtures();

    public string ResolveCacheRoot() =>
        CacheRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LimboDancer", "AslMaps", "cache");

    private static string? FindRepositoryFixtures()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "ASL", "tests", "LimboDancer.Domains.Asl.Maps.Vasl.Tests", "Oracle");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
