using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement;

/// <summary>Optional inspection metadata, scoped to exact accepted terrain. Never part of game history.</summary>
public sealed record SituationMapOwnership(string Version, string Board, string BoardVersion,
    IReadOnlyDictionary<string, string[]> Parents)
{
    public const string CurrentVersion = "goodwood-map-ownership-1";

    /// <summary>Map every owned tactical center to its formation parent in the assembled board's coordinates.</summary>
    public static SituationMapOwnership Create(PilotInputs source, BoardPackage board,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(board);
        var cells = SituationCoverage.Plan(source.ForVersion(PilotInputs.SituationGeneratorVersion), cancellationToken)
            .SelectMany(tile => tile.Cells).ToArray();
        if (cells.Any(cell => cell.Column < 0 || cell.Row < 0))
            throw new InvalidDataException("Goodwood ownership requires the nonnegative world origin.");
        var expected = BoardGeometry.Standard(cells.Max(cell => cell.Column) - SituationAslBoard.OriginColumn + SituationCoverage.ColumnMargin + 1,
            cells.Max(cell => cell.Row) - SituationAslBoard.OriginRow + SituationCoverage.RowMargin + 1);
        var geometry = board.Model.Geometry;
        if (geometry.WidthInHexes != expected.WidthInHexes || geometry.HeightInHexes != expected.HeightInHexes ||
            geometry.HexWidth != expected.HexWidth || geometry.HexHeight != expected.HexHeight ||
            geometry.A1CenterX != expected.A1CenterX || geometry.A1CenterY != expected.A1CenterY ||
            geometry.ColumnLetterOffset != expected.ColumnLetterOffset || geometry.RowNumberOffset != expected.RowNumberOffset ||
            geometry.GridWidth != expected.GridWidth || geometry.GridHeight != expected.GridHeight)
            throw new InvalidDataException("Ownership must use the full assembled Goodwood board geometry.");
        var parents = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var group in cells.GroupBy(cell => cell.Parent))
        {
            cancellationToken.ThrowIfCancellationRequested();
            parents.Add(group.Key, group.OrderBy(cell => cell.Column).ThenBy(cell => cell.Row)
                .Select(cell => geometry.NameOf(new HexIndex(cell.Column - SituationAslBoard.OriginColumn,
                    cell.Row - SituationAslBoard.OriginRow)).ToString()).ToArray());
        }
        return new(CurrentVersion, board.Board.Value, board.Version, parents);
    }
}
