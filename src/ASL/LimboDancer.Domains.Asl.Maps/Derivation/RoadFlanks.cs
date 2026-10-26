using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>
/// The sides of a road through a hex and the building hexes ADJACENT to it on each (A11.8, p. 76; pass 70, ruling R70.33): a road hex is one whose road
/// crosses two or more of its hexsides; its other hexsides fall into runs between the road hexsides, each run one side of the road, and each run's neighbors
/// of building terrain are that side's buildings. Rules decides from the runs whether a vehicle there is open to Street Fighting.
/// </summary>
public static class RoadFlanks
{
    /// <summary>
    /// The runs of a hex's non-road hexsides, each with the building hexes beyond it, in hexside order; empty when the road crosses fewer than two of its
    /// hexsides (no road through it). A run with no building is listed empty.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<HexIndex>> Of(HexFactSet facts, HexIndex hex)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var sides = facts[hex].Hexsides;
        var road = sides.Select(side => side.Terrain?.IsRoad == true).ToArray();
        if (road.Count(item => item) < 2)
        {
            return [];
        }

        // Start just after a road hexside, so each run lies whole between two of them.
        var start = Array.IndexOf(road, true);
        var runs = new List<IReadOnlyList<HexIndex>>();
        List<HexIndex>? run = null;
        for (var step = 1; step <= sides.Count; step++)
        {
            var at = (start + step) % sides.Count;
            if (road[at])
            {
                if (run is not null)
                {
                    runs.Add(run);
                    run = null;
                }

                continue;
            }

            run ??= [];
            if (sides[at].OnMap && facts.Geometry.Neighbor(hex, sides[at].Side) is { } next && facts.Contains(next) && facts[next].Center.Terrain is { IsBuilding: true })
            {
                run.Add(next);
            }
        }

        return runs;
    }
}
