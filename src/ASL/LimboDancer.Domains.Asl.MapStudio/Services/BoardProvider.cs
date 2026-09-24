using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Board status as the Studio computes it (Model Design section 10). Never stored or edited.</summary>
public enum BoardStatus
{
    /// <summary>Decoded without errors; F1 and F2 not both passed.</summary>
    Ingested,

    /// <summary>F1 and F2 pass for this exact source.</summary>
    Verified,
}

/// <summary>A board ready to view: its render input, derived facts, fidelity results, and provenance.</summary>
public sealed record StudioBoard(
    BoardRef Ref,
    string Version,
    string Title,
    BoardStatus Status,
    BoardRenderInput Render,
    TerrainCatalog Catalog,
    F1Result F1,
    F2Result? F2,
    BoardProvenance? Provenance,
    IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public HexFactSet Facts => Render.Facts;
}

/// <summary>A load outcome. <see cref="OutOfScope"/> marks a board the importer declines by design, such as a non-geomorphic board.</summary>
public sealed record BoardLoadResult(StudioBoard? Board, IReadOnlyList<MapDiagnostic> Diagnostics, bool OutOfScope = false);

/// <summary>A library entry. <see cref="Scope"/> is decided from metadata alone, before the board is loaded.</summary>
public sealed record BoardListing(BoardRef Ref, string Title, BoardScope Scope = BoardScope.InScope, string? ScopeReason = null);

/// <summary>The boards the Studio can show. VASL boards today; authored boards arrive with ASL-MAP-07.</summary>
public interface IBoardProvider
{
    /// <summary>A short description of the source, or null when it is not configured.</summary>
    public string? SourceDescription
    {
        get;
    }

    public IReadOnlyList<BoardListing> List();

    public BoardLoadResult Load(BoardRef board);

    /// <summary>The result of an earlier load, without loading.</summary>
    public BoardLoadResult? Cached(BoardRef board);
}

/// <summary>Studio configuration (Architecture and Rendering Design, section 4.2).</summary>
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
}

/// <summary>Loads VASL boards on demand from the configured checkout, caching each result for the process lifetime.</summary>
public sealed class VaslBoardProvider : IBoardProvider
{
    private readonly VaslSource? vasl;
    private readonly string? oracleFixtures;
    private readonly Lazy<SharedBoardMetadataResult?> catalog;
    private readonly ConcurrentDictionary<string, Lazy<BoardLoadResult>> cache = new(StringComparer.Ordinal);
    private readonly Lazy<IReadOnlyList<BoardListing>> listing;

    public VaslBoardProvider(StudioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        vasl = VaslSource.TryOpen(options.VaslRoot);
        oracleFixtures = options.OracleFixtures ?? FindRepositoryFixtures();
        catalog = new Lazy<SharedBoardMetadataResult?>(() => vasl?.ReadTerrainCatalog());
        listing = new Lazy<IReadOnlyList<BoardListing>>(ListUncached);
    }

    public string? SourceDescription => vasl is null ? null : $"VASL checkout {vasl.Root} at {vasl.Git?.HeadCommit ?? "an unknown commit"}";

    public IReadOnlyList<BoardListing> List() => listing.Value;

    public BoardLoadResult? Cached(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return cache.TryGetValue(board.Value, out var entry) && entry.IsValueCreated ? entry.Value : null;
    }

    public BoardLoadResult Load(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return cache.GetOrAdd(board.Value, _ => new Lazy<BoardLoadResult>(() => LoadUncached(board))).Value;
    }

    private BoardListing[] ListUncached()
    {
        if (vasl is null)
        {
            return [];
        }

        var entries = new List<BoardListing>();
        foreach (var name in vasl.BoardNames())
        {
            if (BoardRef.TryParse("bd" + name, out var boardRef))
            {
                var scope = VaslBoardImporter.CheckScope(VaslBoardSource.SourceDirectory(vasl, name));
                entries.Add(new BoardListing(boardRef, "VASL board " + name, scope.Scope, scope.Reason));
            }
        }

        return [.. entries];
    }

    private BoardLoadResult LoadUncached(BoardRef board)
    {
        if (vasl is null || board.Kind != BoardRefKind.Vasl)
        {
            return Failed(new MapDiagnostic("STUDIO-001", MapDiagnosticSeverity.Error, $"{board} is not available from a configured source."));
        }

        if (catalog.Value is not { Catalog: { } terrain } catalogResult)
        {
            return new BoardLoadResult(null, catalog.Value?.Diagnostics ?? []);
        }

        var import = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, board.VaslBoardName), terrain);
        if (import.Board is not { } ingested)
        {
            return new BoardLoadResult(null, import.Diagnostics, import.OutOfScope);
        }

        var facts = HexFactFidelity.Derive(ingested, terrain);
        var f2 = CheckF2(ingested, facts);
        var status = ingested.F1.Passed && f2 is { Passed: true } ? BoardStatus.Verified : BoardStatus.Ingested;
        var title = $"VASL board {board.VaslBoardName} (version {ingested.Metadata.Version})";
        var render = BoardRenderInput.Create(board, title, ingested.Grid, terrain, facts);
        var diagnostics = catalogResult.Diagnostics.Concat(import.Diagnostics).ToArray();
        return new BoardLoadResult(
            new StudioBoard(board, ingested.Provenance.LosData.ContentBlob, title, status, render, terrain, ingested.F1, f2, ingested.Provenance, diagnostics),
            diagnostics);
    }

    private F2Result? CheckF2(IngestedBoard board, HexFactSet facts)
    {
        var path = oracleFixtures is null ? null : Path.Combine(oracleFixtures, board.Board.Value + ".hexfacts.json.gz");
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var fixture = JsonDocument.Parse(gzip);
        return HexFactFidelity.Compare(board, facts, fixture);
    }

    private static BoardLoadResult Failed(MapDiagnostic diagnostic) => new(null, [diagnostic]);

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
