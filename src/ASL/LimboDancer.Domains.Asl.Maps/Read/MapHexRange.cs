using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Read;

/// <summary>Hex distance from an already resolved map context, independent of level, terrain and LOS.</summary>
public static class MapHexRange
{
    public static int? Read(BoardLocation? from, BoardLocation? to, MapLayout? layout, BoardGeometry? geometry, bool placed = false)
    {
        if (from is null || to is null)
        {
            return null;
        }

        if (layout is not null)
        {
            return layout.Locate(from.Board, from.Hex) is { } source && layout.Locate(to.Board, to.Hex) is { } target
                ? layout.Geometry.Distance(source, target) : null;
        }

        if (placed || from.Board != to.Board)
        {
            return null;
        }

        if (geometry is not null)
        {
            return geometry.TryGetIndex(from.Hex, out var source) && geometry.TryGetIndex(to.Hex, out var target)
                ? geometry.Distance(source, target) : null;
        }

        return from.Hex == to.Hex ? 0 : null;
    }
}
