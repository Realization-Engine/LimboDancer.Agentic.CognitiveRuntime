using System.Collections.Concurrent;
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

/// <summary>
/// A library entry. <see cref="Scope"/> is decided from metadata alone, before the board is loaded. The source blobs
/// identify the bytes a batch result must have been computed from to still apply.
/// </summary>
public sealed record BoardListing(
    BoardRef Ref,
    string Title,
    BoardScope Scope = BoardScope.InScope,
    string? ScopeReason = null,
    string? LosDataBlob = null,
    string? MetadataBlob = null);

/// <summary>The boards the Studio can show. VASL boards today; authored boards arrive with ASL-MAP-07.</summary>
public interface IBoardProvider
{
    /// <summary>A short description of the source, or null when it is not configured.</summary>
    public string? SourceDescription
    {
        get;
    }

    /// <summary>The Git blob id of the terrain catalog source, or null when it is unavailable.</summary>
    public string? CatalogBlob
    {
        get;
    }

    public IReadOnlyList<BoardListing> List();

    public BoardLoadResult Load(BoardRef board);

    /// <summary>The result of an earlier load, without loading.</summary>
    public BoardLoadResult? Cached(BoardRef board);
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
        oracleFixtures = options.ResolveOracleFixtures();
        catalog = new Lazy<SharedBoardMetadataResult?>(() => vasl?.ReadTerrainCatalog());
        listing = new Lazy<IReadOnlyList<BoardListing>>(ListUncached);
    }

    public string? SourceDescription => vasl is null ? null : $"VASL checkout {vasl.Root} at {vasl.Git?.HeadCommit ?? "an unknown commit"}";

    public string? CatalogBlob => catalog.Value?.Catalog is null ? null : vasl!.SharedBoardMetadataProvenance().ContentBlob;

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
                var source = VaslBoardSource.SourceDirectory(vasl, name);
                var scope = VaslBoardImporter.CheckScope(source);
                var losData = scope.Scope == BoardScope.OutOfScope ? null : source.ReadEntry(VaslBoardSource.LosDataEntry);
                entries.Add(new BoardListing(boardRef, "VASL board " + name, scope.Scope, scope.Reason,
                    losData is null ? null : GitBlob.Sha(losData), scope.MetadataBytes is null ? null : GitBlob.Sha(scope.MetadataBytes)));
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

    private F2Result? CheckF2(IngestedBoard board, HexFactSet facts) => HexFactFidelity.CompareWithFixture(board, facts, oracleFixtures);

    private static BoardLoadResult Failed(MapDiagnostic diagnostic) => new(null, [diagnostic]);

}
