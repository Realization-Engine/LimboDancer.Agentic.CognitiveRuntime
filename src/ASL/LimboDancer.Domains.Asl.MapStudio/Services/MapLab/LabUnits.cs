using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>
/// Counters on a Map Lab map, to measure what the counter layer costs at scale (the Goodwood Lab Techniques Analysis, section 5.2). The counters
/// are the shipped catalog's own German and Russian units, as <see cref="UnitLibrary.CatalogChoices"/> makes them; this class only places them.
/// The same map, side, and count always give the same hexes. Display input only: no rule reads it.
/// </summary>
public static class LabUnits
{
    /// <summary>The board reference the Lab's units are placed on; the map's own geometry places them.</summary>
    public static BoardRef Board { get; } = BoardRef.Parse("ab-map-lab");

    /// <summary>The counter layer's set id.</summary>
    public const string SetId = "map-lab";

    public const string German = "german";

    public const string Russian = "russian";

    /// <summary>The sides the page adds units for, in order.</summary>
    public static IReadOnlyList<string> Sides { get; } = [German, Russian];

    /// <summary>The numbers of units one button adds.</summary>
    public static IReadOnlyList<int> Increments { get; } = [10, 50, 100, 200, 300, 400, 500, 1000];

    /// <summary>The most units a side may hold, so a run of clicks cannot exhaust the Studio.</summary>
    public const int MaxPerSide = 10_000;

    // Infantry and its support weapons: the counters a map holds most of.
    private static readonly string[] Kinds = ["asl:squad", "asl:half-squad", "asl:leader", "asl:mg"];

    /// <summary>The side's counters the page cycles through: its squads, half-squads, leaders, and machine guns, in catalog order.</summary>
    public static IReadOnlyList<UnitDocument> Pool(IEnumerable<CatalogChoice> choices, string side)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(side);
        return [.. choices.Select(choice => choice.Document).Where(document => document.Side == side && Kinds.Contains(document.Kind, StringComparer.Ordinal))];
    }

    /// <summary>
    /// The next <paramref name="count"/> units of a side, after the <paramref name="already"/> it holds: unit i is the pool's (i mod its size)
    /// counter, on a hex picked from unit i's own seeded sequence, in the side's half of the map (German the west, Russian the east), never water.
    /// Fewer when the side would pass <see cref="MaxPerSide"/>; none when the pool is empty.
    /// </summary>
    public static IReadOnlyList<UnitDocument> Add(LabMap map, IReadOnlyList<UnitDocument> pool, string side, int already, int count)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(side);
        var added = Math.Clamp(Math.Min(count, MaxPerSide - already), 0, MaxPerSide);
        if (pool.Count == 0 || added == 0)
        {
            return [];
        }

        var geometry = map.Geometry;
        var half = Math.Max(1, geometry.WidthInHexes / 2);
        var first = side == Russian ? geometry.WidthInHexes - half : 0;
        var units = new UnitDocument[added];
        for (var i = 0; i < added; i++)
        {
            var index = already + i;
            var hex = Pick(map, first, half, Seed(map.Spec.Seed, side, index));
            units[i] = pool[index % pool.Count] with
            {
                Id = string.Create(CultureInfo.InvariantCulture, $"lab-{side}-{index + 1}"),
                Location = new BoardLocation(Board, geometry.NameOf(hex), 0).ToString(),
            };
        }

        return units;
    }

    // A few tries for a hex that is not water; the last try stands, so a map of water still places every unit.
    private static HexIndex Pick(LabMap map, int first, int width, uint seed)
    {
        var hex = default(HexIndex);
        for (var attempt = 0u; attempt < 8; attempt++)
        {
            var draw = Mix(unchecked(seed + (attempt * 0x9e3779b9)));
            var column = first + (int)(draw % (uint)width);
            hex = new HexIndex(column, (int)(Mix(draw) % (uint)map.Geometry.RowCount(column)));
            if (map.Terrain[map.Geometry.HexOrdinal(hex)] != LabTerrain.Water)
            {
                break;
            }
        }

        return hex;
    }

    // Unit i's own seed. Fixed arithmetic, not HashCode, which differs from one run of the Studio to the next; mixed, since System.Random
    // seeded with neighbouring numbers draws neighbouring hexes (the first build placed its units in pairs along diagonals).
    private static uint Seed(int mapSeed, string side, int index) =>
        Mix(unchecked(Mix((uint)mapSeed) + (side == Russian ? 0x85ebca6bu : 0xc2b2ae35u) + (uint)index));

    // A 32-bit integer hash (the finalizer of MurmurHash3): each bit of the input changes about half the bits of the output.
    private static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x85ebca6b;
            value ^= value >> 13;
            value *= 0xc2b2ae35;
            value ^= value >> 16;
            return value;
        }
    }
}
