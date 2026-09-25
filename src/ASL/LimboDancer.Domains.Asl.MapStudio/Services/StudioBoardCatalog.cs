using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The map read API (ASL-MAP-080) over the boards the Studio can load: each board's exact version, its derived hex
/// facts, its verification status (ASL-MAP-044), and its provenance.
/// </summary>
public sealed class StudioBoardCatalog(IBoardProvider boards) : IBoardCatalog
{
    public BoardReadResult TryGetBoard(BoardRef board, string? version = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (boards.Load(board).Board is not { } loaded)
        {
            return new BoardReadResult(null, [new MapDiagnostic("MAP-READ-001", MapDiagnosticSeverity.Error, $"The Studio cannot load {board}.")]);
        }

        var handle = new BoardHandle(loaded.Ref, loaded.Version, Status(loaded.Status), Provenance(loaded), loaded.Facts);
        return new InMemoryBoardCatalog([handle]).TryGetBoard(board, version);
    }

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
