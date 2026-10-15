using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationCoverageTests
{
    private static readonly string[] Boards = ["A", "B", "C"];
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));

    [Fact]
    public void WholeSituationHasUniqueOwnersAndCompilationMargins()
    {
        var tiles = SituationCoverage.Plan(Inputs);
        var cells = tiles.SelectMany(t => t.Cells).ToArray();
        Assert.Equal(cells.Length, cells.Select(c => c.GlobalKey).Distinct().Count());
        Assert.Equal(Inputs.Regions.Select(r => r.Key).Order(), cells.Select(c => c.Parent).Distinct().Order());
        Assert.Equal(Boards, cells.Select(c => c.Parent.Split('/')[0]).Distinct().Order());
        Assert.All(tiles, tile =>
        {
            Assert.Equal(0, tile.Crop.Column % 4);
            Assert.Equal(0, tile.Crop.Row % 2);
            Assert.All(tile.Cells, cell =>
            {
                Assert.InRange(cell.Column - tile.Crop.Column, SituationCoverage.ColumnMargin,
                    tile.Crop.Width - SituationCoverage.ColumnMargin - 1);
                Assert.InRange(cell.Row - tile.Crop.Row, SituationCoverage.RowMargin,
                    tile.Crop.Height - SituationCoverage.RowMargin - 1);
            });
        });
        // Independent global scan catches holes from region enumeration, including joins.
        var byPosition = cells.ToDictionary(c => (c.Column, c.Row));
        var regions = Inputs.Regions.Select(r => (Region: r, MinX: r.Polygon.Min(p => p.X),
            MaxX: r.Polygon.Max(p => p.X), MinY: r.Polygon.Min(p => p.Y), MaxY: r.Polygon.Max(p => p.Y))).ToArray();
        for (var column = cells.Min(c => c.Column) - 1; column <= cells.Max(c => c.Column) + 1; column++)
        for (var row = cells.Min(c => c.Row) - 1; row <= cells.Max(c => c.Row) + 1; row++)
        {
            var point = SpatialFrame.Center(column, row);
            var parent = regions.FirstOrDefault(r => point.X >= r.MinX && point.X <= r.MaxX &&
                point.Y >= r.MinY && point.Y <= r.MaxY && MetricGeometry.Contains(r.Region.Polygon, point)).Region;
            if (parent is null) Assert.False(byPosition.ContainsKey((column, row)));
            else Assert.Equal(parent.Key, byPosition[(column, row)].Parent);
        }
    }

    [Fact]
    public void PlanIsRepeatableAndCancellationDoesNotPublishPartialCoverage()
    {
        var first = JsonSerializer.Serialize(SituationCoverage.Plan(Inputs));
        Assert.Equal(first, JsonSerializer.Serialize(SituationCoverage.Plan(Inputs)));
        Assert.Throws<OperationCanceledException>(() => SituationCoverage.Plan(Inputs, new CancellationToken(true)));
    }
}
