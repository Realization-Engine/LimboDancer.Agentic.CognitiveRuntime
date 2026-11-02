using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Draws parent transitions and the owned perimeter, without drawing internal tactical hex edges.</summary>
public static class CampaignMapBoundaries
{
    /// <summary>The value of a hex that no parent owns, in a parent array.</summary>
    public const int Unowned = -1;

    public static string Draw(BoardGeometry geometry, IReadOnlyDictionary<string, string> ownership)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(ownership);
        var parents = new int[geometry.HexCount];
        Array.Fill(parents, Unowned);
        var numbers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in ownership)
        {
            if (!numbers.TryGetValue(pair.Value, out var number))
            {
                number = numbers.Count;
                numbers[pair.Value] = number;
            }

            parents[geometry.HexOrdinal(geometry.IndexOf(HexName.Parse(pair.Key)))] = number;
        }

        var path = new StringBuilder();
        AppendEdges(path, geometry, parents);
        return path.Length == 0 ? string.Empty :
            $"<g id=\"campaign-parent-boundaries\" aria-hidden=\"true\" pointer-events=\"none\"><path d=\"{path}\" fill=\"none\" stroke=\"#9b5f17\" stroke-width=\"1.5\" vector-effect=\"non-scaling-stroke\"/></g>";
    }

    /// <summary>
    /// Appends, as path data, every edge between hexes of different parents and every edge of an owned hex that meets no owned hex, each edge
    /// once. <paramref name="parents"/> holds each hex's parent by its ordinal (<see cref="BoardGeometry.HexOrdinal"/>), or
    /// <see cref="Unowned"/>. With every hex its own parent, the edges are the whole grid.
    /// </summary>
    public static void AppendEdges(StringBuilder path, BoardGeometry geometry, ReadOnlySpan<int> parents)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentOutOfRangeException.ThrowIfNotEqual(parents.Length, geometry.HexCount, nameof(parents));
        var ordinal = 0;
        foreach (var cell in geometry.Hexes())
        {
            var parent = parents[ordinal];
            if (parent != Unowned)
            {
                var vertices = geometry.Vertices(cell);
                for (var side = 0; side < 6; side++)
                {
                    if (geometry.Neighbor(cell, (HexsideDirection)side) is { } neighbor)
                    {
                        var across = geometry.HexOrdinal(neighbor);
                        // A shared edge is drawn once, and only when its parents differ.
                        if (parents[across] != Unowned && (parents[across] == parent || ordinal > across))
                        {
                            continue;
                        }
                    }

                    var from = vertices[side];
                    var to = vertices[(side + 1) % 6];
                    if (from == to)
                    {
                        continue;
                    }

                    path.Append(CultureInfo.InvariantCulture, $"M{from.X},{from.Y}L{to.X},{to.Y}");
                }
            }

            ordinal++;
        }
    }
}
