using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
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

    /// <summary>An authored board with validation errors.</summary>
    Authored,

    /// <summary>An authored board with no validation errors; its Feature Model is the source of truth.</summary>
    AuthoredValid,
}

/// <summary>
/// A board ready to view: its render input, derived facts, fidelity results, and provenance. VASL boards get their
/// Styled input on first use, by vectorizing; authored boards carry their Feature Model in <see cref="Render"/>.
/// </summary>
public sealed record StudioBoard(
    BoardRef Ref,
    string Version,
    string Title,
    BoardStatus Status,
    BoardRenderInput Render,
    TerrainCatalog Catalog,
    F1Result? F1,
    F2Result? F2,
    BoardProvenance? Provenance,
    IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public HexFactSet Facts => Render.Facts;

    /// <summary>The vectorized Styled input for a VASL board, computed on first use.</summary>
    public Lazy<BoardRenderInput>? StyledRender
    {
        get; init;
    }

    public ValidationReport? Validation
    {
        get; init;
    }

    /// <summary>An authored board saved as a draft, such as one derived from a VASL board (ASL-MAP-074).</summary>
    public bool IsDraft
    {
        get; init;
    }

    public bool IsAuthored => Ref.Kind == BoardRefKind.Authored;

    /// <summary>The render input for a view, or null when the board has no Feature Model for it.</summary>
    public BoardRenderInput? InputFor(BoardView view) =>
        view is BoardView.Styled or BoardView.Comparison ? Render.Styled is not null ? Render : StyledRender?.Value : Render;
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

/// <summary>The boards the Studio can show: VASL boards and authored boards.</summary>
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

    /// <summary>
    /// One exact version, for render URLs. Authored boards keep recent edit versions; other boards only their current one.
    /// </summary>
    public StudioBoard? LoadVersion(BoardRef board, string version) =>
        Load(board).Board is { } loaded && loaded.Version == version ? loaded : null;
}

/// <summary>The terrain catalog authored boards use, and its blob (Model Design section 4.2).</summary>
public interface ICatalogSource
{
    public (TerrainCatalog Catalog, string Hash)? Catalog();
}

/// <summary>Loads VASL boards on demand from the configured checkout, caching each result for the process lifetime.</summary>
public sealed class VaslBoardProvider : IBoardProvider, ICatalogSource
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
        var styled = new Lazy<BoardRenderInput>(() =>
        {
            var result = Vectorize(ingested, facts, terrain);
            return BoardRenderInput.Create(board, title, ingested.Grid, terrain, facts, new StyledSource(result.Model, result.Compiled, result.CompiledFacts));
        });
        return new BoardLoadResult(
            new StudioBoard(board, ingested.Provenance.LosData.ContentBlob, title, status, render, terrain, ingested.F1, f2, ingested.Provenance, diagnostics)
            {
                StyledRender = styled,
            },
            diagnostics);
    }

    /// <summary>The terrain catalog and its blob, for authored boards, which use the same catalog (Model Design section 4.2).</summary>
    public (TerrainCatalog Catalog, string Hash)? Catalog() =>
        catalog.Value?.Catalog is { } terrain ? (terrain, vasl!.SharedBoardMetadataProvenance().ContentBlob) : null;

    /// <summary>Vectorizes an ingested board (Model Design section 7), for the Styled view and for new authored boards.</summary>
    public static VectorizeResult Vectorize(IngestedBoard board, HexFactSet facts, TerrainCatalog terrain)
    {
        ArgumentNullException.ThrowIfNull(board);
        return Vectorizer.Vectorize(new VectorizerSource(board.Board, board.Provenance.LosData.ContentBlob, board.Grid, facts,
            HexFactFidelity.Annotations(board.Metadata), board.Provenance.SharedBoardMetadata.ContentBlob), terrain);
    }

    /// <summary>Imports a VASL board again, for vectorizing it into a new authored board.</summary>
    public IngestedBoard? Ingest(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return vasl is null || catalog.Value?.Catalog is not { } terrain
            ? null
            : VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, board.VaslBoardName), terrain).Board;
    }

    private F2Result? CheckF2(IngestedBoard board, HexFactSet facts) => HexFactFidelity.CompareWithFixture(board, facts, oracleFixtures);

    private static BoardLoadResult Failed(MapDiagnostic diagnostic) => new(null, [diagnostic]);
}

/// <summary>Routes each board reference to its source: VASL boards to the checkout, authored boards to their packages.</summary>
public sealed class StudioBoardProvider(VaslBoardProvider vasl, AuthoredBoardService authored) : IBoardProvider
{
    public string? SourceDescription => vasl.SourceDescription;

    public string? CatalogBlob => vasl.CatalogBlob;

    public IReadOnlyList<BoardListing> List() => vasl.List();

    public BoardLoadResult Load(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Kind == BoardRefKind.Authored ? authored.Load(board) : vasl.Load(board);
    }

    public BoardLoadResult? Cached(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Kind == BoardRefKind.Authored ? authored.Load(board) : vasl.Cached(board);
    }

    public StudioBoard? LoadVersion(BoardRef board, string version)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Kind == BoardRefKind.Authored
            ? authored.LoadVersion(board, version)
            : vasl.Load(board).Board is { } loaded && loaded.Version == version ? loaded : null;
    }
}
