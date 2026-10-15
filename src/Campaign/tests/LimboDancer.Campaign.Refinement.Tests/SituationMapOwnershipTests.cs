using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationMapOwnershipTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));

    private static BoardPackage Board()
    {
        var cells = SituationCoverage.Plan(Inputs).SelectMany(tile => tile.Cells).ToArray();
        var geometry = BoardGeometry.Standard(cells.Max(cell => cell.Column) + 2 * SituationCoverage.ColumnMargin + 1,
            cells.Max(cell => cell.Row) + 2 * SituationCoverage.RowMargin + 1);
        return new(BoardRef.Parse("ab-goodwood-ownership-test"), "Ownership test", FeatureModel.New(geometry, Inputs.CatalogBlob));
    }

    [Fact]
    public void EveryOwnedCenterHasExactlyOneParentAndGuardHexesHaveNone()
    {
        var board = Board();
        var original = board.FeaturesBytes();
        var ownership = SituationMapOwnership.Create(Inputs, board);
        var byHex = ownership.Parents.SelectMany(parent => parent.Value.Select(hex => (Hex: hex, Parent: parent.Key)))
            .ToDictionary(cell => cell.Hex, cell => cell.Parent);
        var coverage = SituationCoverage.Plan(Inputs).SelectMany(tile => tile.Cells)
            .ToDictionary(cell => (cell.Column, cell.Row));
        Assert.Equal(1038, ownership.Parents.Count);
        Assert.Equal(37788, byHex.Count);
        Assert.Equal(board.Board.Value, ownership.Board);
        Assert.Equal(board.Version, ownership.BoardVersion);
        foreach (var hex in board.Model.Geometry.Hexes())
        {
            var name = board.Model.Geometry.NameOf(hex).ToString();
            if (coverage.TryGetValue((hex.Column + SituationAslBoard.OriginColumn, hex.Row + SituationAslBoard.OriginRow), out var cell))
                Assert.Equal(cell.Parent, byHex[name]);
            else
                Assert.False(byHex.ContainsKey(name));
        }
        Assert.Equal(original, board.FeaturesBytes());
        var json = JsonSerializer.Serialize(ownership);
        Assert.Equal(json, JsonSerializer.Serialize(SituationMapOwnership.Create(Inputs, board)));
        Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<SituationMapOwnership>(json)));
    }

    [Fact]
    public void RejectsTileGeometryAndHonorsCancellation()
    {
        var tile = new BoardPackage(BoardRef.Parse("ab-goodwood-tile-test"), "Tile",
            FeatureModel.New(BoardGeometry.Standard(40, 20), Inputs.CatalogBlob));
        Assert.Throws<InvalidDataException>(() => SituationMapOwnership.Create(Inputs, tile));
        Assert.Throws<OperationCanceledException>(() => SituationMapOwnership.Create(Inputs, Board(), new CancellationToken(true)));
    }
}
