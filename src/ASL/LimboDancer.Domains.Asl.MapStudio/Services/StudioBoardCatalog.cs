using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;


namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The map read API (ASL-MAP-080) over the boards the Studio can load: each board's exact version, its derived hex
/// facts, its verification status (ASL-MAP-044), and its provenance.
/// </summary>
public sealed class StudioBoardCatalog(IBoardProvider boards) : IBoardCatalog
{
    // Pass 31c (design D18; play test P-24): one handle for a loaded board, for as long as the board is loaded. The planner keeps a board's LOS
    // map and every LOS it has read by the board's handle, so a handle made anew on each call threw that work away: a rout's search then built
    // the LOS map again for every LOS it read. The handle holds what the board holds and nothing else, so its reads are the same.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<StudioBoard, BoardHandle> Handles = new();

    public BoardReadResult TryGetBoard(BoardRef board, string? version = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (boards.Load(board).Board is not { } loaded)
        {
            return new BoardReadResult(null, [new MapDiagnostic("MAP-READ-001", MapDiagnosticSeverity.Error, $"The Studio cannot load {board}.")]);
        }

        return new InMemoryBoardCatalog([Handles.GetValue(loaded, Handle)]).TryGetBoard(board, version);
    }

    // LOS data (LOS Design, section 5): the board's grid and catalog, and a VASL board's hexside annotations.
    private static BoardHandle Handle(StudioBoard loaded) => new(loaded.Ref, loaded.Version, Status(loaded.Status), Provenance(loaded), loaded.Facts, VaslSource(loaded))
    {
        Los = loaded.Composition is not null ? null
            : loaded.Ingested is { } ingested
                ? new LosData(loaded.Render.Grid, loaded.Catalog, Maps.Vasl.HexFactFidelity.Annotations(ingested.Metadata), new([]))
                : new LosData(loaded.Render.Grid, loaded.Catalog),
    };

    /// <summary>The typed VASL source of an ingested board: its metadata version and blob, LOSData blob, and commit.</summary>
    private static VaslBoardSource? VaslSource(StudioBoard board) => board is { Provenance: { } provenance, Ingested: { } ingested }
        ? new VaslBoardSource(board.Ref.VaslBoardName, ingested.Metadata.Version, provenance.Metadata.IndexBlob ?? provenance.Metadata.ContentBlob,
            provenance.LosData.ContentBlob, provenance.VaslCommit)
        : null;

    private static BoardReadStatus Status(BoardStatus status) => status switch
    {
        BoardStatus.Verified => BoardReadStatus.Verified,
        BoardStatus.AuthoredValid => BoardReadStatus.AuthoredValid,
        BoardStatus.Authored => BoardReadStatus.Authored,
        _ => BoardReadStatus.Ingested,
    };

    private static string Provenance(StudioBoard board) => board.Provenance is { } provenance
        ? $"VASL {provenance.VaslCommit ?? "working tree"}, {provenance.LosData.RepositoryPath} blob {provenance.LosData.ContentBlob}"
        : $"{board.Title}, version {board.Version}";
}
