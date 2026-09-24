namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// A local VASL repository checkout (configuration key <c>AslMaps:VaslRoot</c>). Nothing is downloaded;
/// when no checkout is configured, VASL-backed features are unavailable (ASL-MAP-030).
/// </summary>
public sealed class VaslSource
{
    public const string ConfigurationKey = "AslMaps:VaslRoot";

    /// <summary>Environment form of <see cref="ConfigurationKey"/>, as .NET configuration maps it.</summary>
    public const string EnvironmentVariable = "AslMaps__VaslRoot";

    public const string SharedBoardMetadataRepositoryPath = "dist/boardData/SharedBoardMetadata.xml";

    private readonly Lazy<GitCheckout?> git;

    private VaslSource(string root)
    {
        Root = root;
        git = new Lazy<GitCheckout?>(() => GitCheckout.TryOpen(root));
    }

    public string Root
    {
        get;
    }

    /// <summary>The checkout's git metadata, or null when the root is not a git working tree.</summary>
    public GitCheckout? Git => git.Value;

    /// <summary>VASL board names with a source directory (<c>boards/src/bdNN</c>), sorted ordinally.</summary>
    public IReadOnlyList<string> BoardNames() =>
        Directory.EnumerateDirectories(Path.Combine(Root, "boards", "src"), "bd*")
            .Select(path => Path.GetFileName(path)[2..])
            .Where(name => name.Length > 0)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public SourceFileProvenance SharedBoardMetadataProvenance() =>
        new(SharedBoardMetadataRepositoryPath, GitBlob.Sha(File.ReadAllBytes(SharedBoardMetadataPath)), Git?.IndexBlob(SharedBoardMetadataRepositoryPath));

    public string SharedBoardMetadataPath => Path.Combine(Root, "dist", "boardData", "SharedBoardMetadata.xml");

    public string BoardSourceDirectory(string vaslBoardName) => Path.Combine(Root, "boards", "src", "bd" + vaslBoardName);

    public string BoardArchivePath(string vaslBoardName) => Path.Combine(Root, "boards", "bdFiles", "bd" + vaslBoardName);

    /// <summary>Returns a source when the directory looks like a VASL checkout, otherwise null.</summary>
    public static VaslSource? TryOpen(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var full = Path.GetFullPath(root);
        return Directory.Exists(Path.Combine(full, "boards")) && File.Exists(Path.Combine(full, "dist", "boardData", "SharedBoardMetadata.xml"))
            ? new VaslSource(full)
            : null;
    }

    public static VaslSource? FromEnvironment() => TryOpen(Environment.GetEnvironmentVariable(EnvironmentVariable));

    public SharedBoardMetadataResult ReadTerrainCatalog()
    {
        using var stream = File.OpenRead(SharedBoardMetadataPath);
        return SharedBoardMetadataParser.Parse(stream);
    }
}
