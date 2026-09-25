using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// The Scenario A1 terrain evidence read through the map read API (ASL-MAP-081; Occupied and Concealed Entry Design,
/// section 3). It accepts exactly the locations <see cref="Board01TerrainCatalog"/> accepts, and only while board 01
/// reads from the pinned VASL metadata, as a verified board, with the override's building type as the derived terrain
/// of the hex's ground level. A board that disagrees with an override is not evidence.
/// </summary>
public sealed class BoardCatalogTerrainEvidence(IBoardCatalog boards) : IScenarioA1TerrainEvidence
{
    public static readonly BoardRef Board = BoardRef.ForVaslBoard(Board01TerrainCatalog.BoardId);

    private readonly Board01TerrainCatalog pinned = new();

    public bool IsSupportedGroundLevel(ScenarioA1TerrainBinding? binding)
    {
        ArgumentNullException.ThrowIfNull(boards);
        return pinned.IsSupportedGroundLevel(binding) && boards.TryGetBoard(Board).Board is { } handle && Agrees(handle, binding!.Hex);
    }

    /// <summary>
    /// The binding the packages expect for a location on a board in play, or null when the board is not the pinned
    /// board 01 source or the location is not a supported ground-level building on it.
    /// </summary>
    public ScenarioA1TerrainBinding? Bind(BoardHandle handle, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(location);
        if (location.Board != handle.Ref || location.Level != 0 || location.Side is not null)
        {
            return null;
        }

        var binding = new ScenarioA1TerrainBinding(Board01TerrainCatalog.BoardId, Board01TerrainCatalog.BoardVersion,
            Board01TerrainCatalog.MetadataGitBlobSha, location.Hex.ToString(), 0, null);
        return pinned.IsSupportedGroundLevel(binding) && Agrees(handle, binding.Hex) ? binding : null;
    }

    /// <summary>Whether a board handle reads from the VASL metadata the packages are pinned to.</summary>
    public static bool IsPinned(BoardHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return handle.Ref == Board && handle.VaslSource is
        {
            BoardName: Board01TerrainCatalog.BoardId,
            MetadataVersion: Board01TerrainCatalog.BoardVersion,
            MetadataBlob: Board01TerrainCatalog.MetadataGitBlobSha,
        };
    }

    /// <summary>The terrain name VASL gives an override's building type.</summary>
    public static string TerrainName(Board01Building building)
    {
        ArgumentNullException.ThrowIfNull(building);
        var material = building.Material switch
        {
            "stone" => "Stone",
            "wooden" => "Wooden",
            _ => throw new InvalidOperationException("Unknown board building material."),
        };
        return $"{material} Building, {building.Levels} Level";
    }

    private bool Agrees(BoardHandle handle, string hex) =>
        IsPinned(handle) && pinned.TryGetBuilding(hex, out var building) && HexName.TryParse(hex, out var name)
        && handle.Resolve(new BoardLocation(handle.Ref, name, 0)).Read is { IsDefinitive: true } read
        && read.Level.Terrain?.Name == TerrainName(building!);
}
