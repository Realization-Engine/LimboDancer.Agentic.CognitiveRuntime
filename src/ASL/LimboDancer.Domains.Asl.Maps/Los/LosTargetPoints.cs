using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>
/// The two target points a firer may choose against Infantry that entered a hex at the road rate (A4.132, B3.3, B13.31): the hex center dot, or
/// the point where the hexside crossed meets the road. This is a read beside <see cref="LosCalculator"/>, not part of VASL's walk: it answers
/// only whether the straight line to the point crosses other terrain inside the target hex, which VASL's LOS to a center location never asks.
/// The LOS up to the target hex stays the calculator's.
/// </summary>
public static class LosTargetPoints
{
    /// <summary>
    /// Whether the line from the source hex's LOS point to a point of the target hex crosses, inside the target hex, a cell whose terrain is
    /// neither open (Open Ground, a road) nor hexside terrain (a wall or hedge has its own TEM). The point is the target hex's center dot when
    /// <paramref name="enteredAcross"/> is null, and otherwise the edge point of that hexside of the target hex, which stands for the point where
    /// the road crosses it. Null when either hex is not on the map.
    /// </summary>
    public static bool? CrossesTerrainInTargetHex(LosMap map, BoardLocation source, BoardLocation target, HexsideDirection? enteredAcross)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (map.Locate(source.Board, source.Hex) is not { } sourceHex || map.Locate(target.Board, target.Hex) is not { } targetHex)
        {
            return null;
        }

        var from = map.LosPoint(sourceHex);
        var to = enteredAcross is { } side ? map.EdgePoint(targetHex, side) : map.LosPoint(targetHex);
        var steps = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        for (var step = 0; step <= steps; step++)
        {
            var x = steps == 0 ? from.X : (int)Math.Round(from.X + ((to.X - from.X) * (double)step / steps));
            var y = steps == 0 ? from.Y : (int)Math.Round(from.Y + ((to.Y - from.Y) * (double)step / steps));
            if (map.Grid.TryGetCode(x, y, out var code) && map.Locator.GridToHex(x, y) == targetHex
                && map.Catalog[code] is { IsOpen: false, IsHexsideTerrain: false })
            {
                return true;
            }
        }

        return false;
    }
}
