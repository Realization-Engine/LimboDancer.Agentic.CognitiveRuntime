using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Draws parent transitions and the owned perimeter, without drawing internal tactical hex edges.</summary>
public static class CampaignMapBoundaries
{
    public static string Draw(BoardGeometry geometry, IReadOnlyDictionary<string, string> ownership)
    {
        var owned = ownership.ToDictionary(pair => geometry.IndexOf(HexName.Parse(pair.Key)), pair => pair.Value);
        var path = new StringBuilder();
        foreach (var cell in owned.OrderBy(pair => geometry.HexOrdinal(pair.Key)))
        {
            var vertices = geometry.Vertices(cell.Key);
            for (var side = 0; side < 6; side++)
            {
                if (geometry.Neighbor(cell.Key, (HexsideDirection)side) is { } neighbor && owned.TryGetValue(neighbor, out var parent))
                {
                    // A shared edge is drawn once, and only when its parents differ.
                    if (parent == cell.Value || geometry.HexOrdinal(cell.Key) > geometry.HexOrdinal(neighbor))
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
        return path.Length == 0 ? string.Empty :
            $"<g id=\"campaign-parent-boundaries\" aria-hidden=\"true\" pointer-events=\"none\"><path d=\"{path}\" fill=\"none\" stroke=\"#9b5f17\" stroke-width=\"1.5\" vector-effect=\"non-scaling-stroke\"/></g>";
    }
}
