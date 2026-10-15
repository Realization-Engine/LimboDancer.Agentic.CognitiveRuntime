using System.Globalization;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationCell(int Column, int Row, string Parent)
{
    public string GlobalKey => FormattableString.Invariant($"{Column}:{Row}");
}

public sealed record SituationTile(string Key, Crop Crop, IReadOnlyList<SituationCell> Cells);

/// <summary>Stable ownership on the common lattice, independent of seed and terrain admission.</summary>
public static class SituationCoverage
{
    public const string Version = "goodwood-coverage-1";
    public const int Columns = 32;
    public const int Rows = 16;
    public const int ColumnMargin = 4;
    public const int RowMargin = 2;

    public static IReadOnlyList<SituationTile> Plan(PilotInputs inputs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        // Enumerate each region's small bounding box instead of testing the whole map
        // against every polygon. Ordinal insertion preserves the existing parent tie-break.
        var cells = new Dictionary<(int Column, int Row), SituationCell>();
        foreach (var region in inputs.Regions.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var minX = region.Polygon.Min(p => p.X); var maxX = region.Polygon.Max(p => p.X);
            var minY = region.Polygon.Min(p => p.Y); var maxY = region.Polygon.Max(p => p.Y);
            var firstColumn = checked((int)Math.Floor(minX / SpatialFrame.ColumnMm)) - 1;
            var lastColumn = checked((int)Math.Ceiling(maxX / SpatialFrame.ColumnMm)) + 1;
            var firstRow = checked((int)Math.Floor(minY / 40000.0)) - 1;
            var lastRow = checked((int)Math.Ceiling(maxY / 40000.0)) + 1;
            for (var column = firstColumn; column <= lastColumn; column++)
            for (var row = firstRow; row <= lastRow; row++)
            {
                var point = SpatialFrame.Center(column, row);
                if (point.X < minX || point.X > maxX || point.Y < minY || point.Y > maxY ||
                    !MetricGeometry.Contains(region.Polygon, point)) continue;
                cells.TryAdd((column, row), new(column, row, region.Key));
            }
        }
        return cells.Values.GroupBy(c => (Column: Floor(c.Column, Columns), Row: Floor(c.Row, Rows)))
            .OrderBy(g => g.Key.Column).ThenBy(g => g.Key.Row).Select(group =>
            {
                var (column, row) = group.Key;
                var crop = new Crop(column * Columns - ColumnMargin, row * Rows - RowMargin,
                    Columns + 2 * ColumnMargin, Rows + 2 * RowMargin);
                _ = crop.Geometry();
                return new SituationTile("tile-" + column.ToString(CultureInfo.InvariantCulture) + "-" + row.ToString(CultureInfo.InvariantCulture),
                    crop, Array.AsReadOnly(group.OrderBy(c => c.Column).ThenBy(c => c.Row).ToArray()));
            }).ToArray();
    }

    private static int Floor(int value, int divisor) => checked((int)Math.Floor((double)value / divisor));
}
