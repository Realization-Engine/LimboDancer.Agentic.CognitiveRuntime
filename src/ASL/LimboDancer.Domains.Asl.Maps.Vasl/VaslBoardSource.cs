using System.IO.Compression;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

public enum VaslBoardSourceKind
{
    /// <summary>An unpacked board under <c>boards/src/bdNN/</c>; the pinned identity (Ingestion Design 8.2).</summary>
    SourceDirectory,

    /// <summary>A packaged board archive at <c>boards/bdFiles/bdNN</c>.</summary>
    Archive,
}

/// <summary>
/// Where a file's bytes came from. <see cref="ContentBlob"/> is the Git blob id of the bytes actually read;
/// <see cref="IndexBlob"/> is the committed blob id from the git index, which can differ for text files
/// checked out with line-ending conversion.
/// </summary>
public sealed record SourceFileProvenance(string RepositoryPath, string ContentBlob, string? IndexBlob)
{
    public bool MatchesIndex => IndexBlob is not null && IndexBlob == ContentBlob;
}

/// <summary>One board's files, from a source directory or a packaged archive.</summary>
public sealed class VaslBoardSource
{
    public const string LosDataEntry = "LOSData";
    public const string MetadataEntry = "BoardMetadata.xml";

    private readonly VaslSource vasl;

    private VaslBoardSource(VaslSource vasl, string boardName, VaslBoardSourceKind kind)
    {
        this.vasl = vasl;
        BoardName = boardName;
        Kind = kind;
    }

    /// <summary>The VASL board name, such as <c>01</c> for <c>bd01</c>.</summary>
    public string BoardName
    {
        get;
    }

    public VaslBoardSourceKind Kind
    {
        get;
    }

    /// <summary>The source directory or archive file, relative to the VASL checkout root.</summary>
    public string RepositoryPath => Kind == VaslBoardSourceKind.SourceDirectory
        ? $"boards/src/bd{BoardName}"
        : $"boards/bdFiles/bd{BoardName}";

    public static VaslBoardSource SourceDirectory(VaslSource vasl, string boardName) =>
        new(vasl ?? throw new ArgumentNullException(nameof(vasl)), boardName, VaslBoardSourceKind.SourceDirectory);

    public static VaslBoardSource Archive(VaslSource vasl, string boardName) =>
        new(vasl ?? throw new ArgumentNullException(nameof(vasl)), boardName, VaslBoardSourceKind.Archive);

    public bool Exists => Kind == VaslBoardSourceKind.SourceDirectory
        ? Directory.Exists(vasl.BoardSourceDirectory(BoardName))
        : File.Exists(vasl.BoardArchivePath(BoardName));

    /// <summary>Reads one entry's bytes, or null when the board has no such entry.</summary>
    public byte[]? ReadEntry(string entryName)
    {
        if (Kind == VaslBoardSourceKind.SourceDirectory)
        {
            var path = Path.Combine(vasl.BoardSourceDirectory(BoardName), entryName);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        var archivePath = vasl.BoardArchivePath(BoardName);
        if (!File.Exists(archivePath))
        {
            return null;
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Provenance for an entry's bytes. In a source directory each file is a repository file; in an archive the
    /// entries are identified by content and the archive file itself carries the repository identity.
    /// </summary>
    public SourceFileProvenance Provenance(string entryName, ReadOnlySpan<byte> content)
    {
        var repositoryPath = Kind == VaslBoardSourceKind.SourceDirectory ? $"{RepositoryPath}/{entryName}" : $"{RepositoryPath}!{entryName}";
        var indexBlob = Kind == VaslBoardSourceKind.SourceDirectory ? vasl.Git?.IndexBlob(repositoryPath) : null;
        return new SourceFileProvenance(repositoryPath, GitBlob.Sha(content), indexBlob);
    }

    /// <summary>Provenance of the archive file itself; null for a source directory.</summary>
    public SourceFileProvenance? ArchiveProvenance()
    {
        if (Kind != VaslBoardSourceKind.Archive || !Exists)
        {
            return null;
        }

        var bytes = File.ReadAllBytes(vasl.BoardArchivePath(BoardName));
        return new SourceFileProvenance(RepositoryPath, GitBlob.Sha(bytes), vasl.Git?.IndexBlob(RepositoryPath));
    }
}
