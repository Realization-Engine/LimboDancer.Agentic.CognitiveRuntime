using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;

namespace LimboDancer.Domains.Asl.Maps.Read;

/// <summary>
/// Whether a board's terrain may be used as definitive evidence (ASL-MAP-044): only a verified VASL board or an
/// authored board without validation errors. Every other status makes terrain facts nondefinitive (ASL-RD-011).
/// </summary>
public enum BoardReadStatus
{
    /// <summary>A VASL board whose F1 and F2 checks pass for this exact source.</summary>
    Verified,

    /// <summary>An authored board with no validation errors.</summary>
    AuthoredValid,

    /// <summary>A VASL board decoded without errors whose checks have not both passed.</summary>
    Ingested,

    /// <summary>An authored board with validation errors.</summary>
    Authored,
}

/// <summary>
/// One location resolved on a board (Map Model and Authoring Design, section 11): the hex's derived facts, the facts of
/// the location's level, the board version and status, and an evidence reference an observation can cite.
/// </summary>
public sealed record LocationRead(
    BoardLocation Location,
    HexFacts Hex,
    LocationFacts Level,
    string BoardVersion,
    BoardReadStatus Status,
    string Evidence)
{
    /// <summary>Whether the terrain facts may support a definitive conclusion (ASL-MAP-044).</summary>
    public bool IsDefinitive => Status is BoardReadStatus.Verified or BoardReadStatus.AuthoredValid;
}

public sealed record LocationReadResult(LocationRead? Read, IReadOnlyList<MapDiagnostic> Diagnostics);

/// <summary>
/// The typed source identity of a board read from VASL: the VASL board name, the version its metadata declares, the Git
/// blobs of its metadata and LOSData, and the VASL commit when known. Evidence pinned to a VASL source (ASL-MAP-081)
/// compares these, never the free-text provenance.
/// </summary>
public sealed record VaslBoardSource(string BoardName, string MetadataVersion, string MetadataBlob, string LosDataBlob, string? Commit);

/// <summary>
/// A read-only handle on one exact version of a board (ASL-MAP-080): its derived hex facts, status, and provenance.
/// Positions are resolved against this version only.
/// </summary>
public sealed class BoardHandle
{
    public BoardHandle(BoardRef board, string version, BoardReadStatus status, string provenance, HexFactSet facts, VaslBoardSource? vaslSource = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(facts);
        Ref = board;
        Version = version;
        Status = status;
        Provenance = provenance;
        Facts = facts;
        VaslSource = vaslSource;
    }

    public BoardRef Ref
    {
        get;
    }

    public string Version
    {
        get;
    }

    public BoardReadStatus Status
    {
        get;
    }

    /// <summary>Where the board's data came from, in words, such as a VASL commit and file blob.</summary>
    public string Provenance
    {
        get;
    }

    public HexFactSet Facts
    {
        get;
    }

    /// <summary>The typed VASL source, or null for an authored or synthetic board.</summary>
    public VaslBoardSource? VaslSource
    {
        get;
    }

    public BoardGeometry Geometry => Facts.Geometry;

    /// <summary>
    /// What an LOS read needs beyond the hex facts (LOS Design, section 5): the terrain grid, the terrain catalog, the
    /// hexside annotations, and the LOS scenario-specific rules for composing maps. A handle without it answers no LOS.
    /// </summary>
    public LosData? Los
    {
        get; init;
    }

    /// <summary>The derived facts of a hex, or null when the hex is not on the board.</summary>
    public HexFacts? HexFacts(HexName hex) => Geometry.TryGetIndex(hex, out var index) ? Facts[index] : null;

    /// <summary>The hex across a hexside, or null at the board edge or when the hex is not on the board.</summary>
    public HexName? Neighbor(HexName hex, HexsideDirection side) =>
        Geometry.TryGetIndex(hex, out var index) && Geometry.Neighbor(index, side) is { } neighbor ? Geometry.NameOf(neighbor) : null;

    /// <summary>The distance in hexes, or null when either hex is not on the board.</summary>
    public int? Distance(HexName from, HexName to) =>
        Geometry.TryGetIndex(from, out var source) && Geometry.TryGetIndex(to, out var target) ? Geometry.Distance(source, target) : null;

    /// <summary>Resolves a location to its facts: the hex must be on this board and the level in the hex's location chain.</summary>
    public LocationReadResult Resolve(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (location.Board != Ref)
        {
            return Fail("MAP-READ-002", $"{location} is not on {Ref}.");
        }

        if (HexFacts(location.Hex) is not { } hex)
        {
            return Fail("MAP-READ-002", $"{location.Hex} is not a hex on {Ref}.");
        }

        if (hex.Locations.FirstOrDefault(level => level.Level == location.Level) is not { } level)
        {
            return Fail("MAP-READ-002", $"{location} is not in the hex's location chain ({string.Join(", ", hex.Locations.Select(item => item.Level))}).");
        }

        var evidence = $"board:{Ref.Value}@{Version}#{location.Hex}:{location.Level}";
        return new LocationReadResult(new LocationRead(location, hex, level, Version, Status, evidence), []);
    }

    private static LocationReadResult Fail(string code, string message) =>
        new(null, [new MapDiagnostic(code, MapDiagnosticSeverity.Error, message)]);
}

public sealed record BoardReadResult(BoardHandle? Board, IReadOnlyList<MapDiagnostic> Diagnostics);

/// <summary>
/// The read-only map API for observation providers (ASL-MAP-080; Map Model and Authoring Design, section 11). A caller
/// that names a version gets that version or a diagnostic, never another one.
/// </summary>
public interface IBoardCatalog
{
    public BoardReadResult TryGetBoard(BoardRef board, string? version = null);
}

/// <summary>A board catalog over boards already loaded, such as the Studio's or a test's.</summary>
public sealed class InMemoryBoardCatalog(IEnumerable<BoardHandle> boards) : IBoardCatalog
{
    private readonly Dictionary<BoardRef, BoardHandle> boards = boards.ToDictionary(board => board.Ref);

    public BoardReadResult TryGetBoard(BoardRef board, string? version = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (!boards.TryGetValue(board, out var handle))
        {
            return new BoardReadResult(null, [new MapDiagnostic("MAP-READ-001", MapDiagnosticSeverity.Error, $"{board} is not in the catalog.")]);
        }

        return version is not null && version != handle.Version
            ? new BoardReadResult(null, [new MapDiagnostic("MAP-READ-001", MapDiagnosticSeverity.Error,
                $"{board} is at version {handle.Version}, not the requested {version}.")])
            : new BoardReadResult(handle, []);
    }
}
