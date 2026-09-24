using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>
/// Compares two Hex Fact sets field by field, as F2 does against the oracle and F3 does against the source board.
/// <see cref="HexFacts.CenterSource"/> is a derivation trace and is not compared.
/// </summary>
public static class HexFactComparer
{
    /// <summary>Differences as <c>hex.field: expected X, derived Y</c>, in hex order.</summary>
    public static IReadOnlyList<string> Differences(HexFactSet expected, HexFactSet actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        var differences = new List<string>();
        foreach (var hex in expected.Hexes)
        {
            var other = actual.Hexes.Count > 0 && actual.Geometry.Contains(hex.Index) ? actual[hex.Index] : null;
            if (other is null)
            {
                differences.Add($"{hex.Hex}: not on the compared board");
                continue;
            }

            Compare(hex, other, differences);
        }

        return differences;
    }

    /// <summary>The hexes whose facts differ.</summary>
    public static IReadOnlyList<HexIndex> DifferingHexes(HexFactSet expected, HexFactSet actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        var hexes = new List<HexIndex>();
        foreach (var hex in expected.Hexes)
        {
            var scratch = new List<string>();
            Compare(hex, actual[hex.Index], scratch);
            if (scratch.Count > 0)
            {
                hexes.Add(hex.Index);
            }
        }

        return hexes;
    }

    private static void Compare(HexFacts expected, HexFacts actual, List<string> differences)
    {
        var name = expected.Hex.ToString();
        Check(differences, name, "baseLevel", expected.BaseLevel, actual.BaseLevel);
        Check(differences, name, "stairway", expected.Stairway, actual.Stairway);
        Check(differences, name, "center", expected.Center, actual.Center);
        Check(differences, name, "locations.count", expected.Locations.Count, actual.Locations.Count);
        for (var index = 0; index < Math.Min(expected.Locations.Count, actual.Locations.Count); index++)
        {
            Check(differences, name, $"locations[{index}]", expected.Locations[index], actual.Locations[index]);
        }

        for (var side = 0; side < Math.Min(expected.Hexsides.Count, actual.Hexsides.Count); side++)
        {
            Check(differences, name, $"hexsides[{side}]", expected.Hexsides[side], actual.Hexsides[side]);
        }

        Check(differences, name, "bridge", expected.Bridge, actual.Bridge);
    }

    private static void Check<T>(List<string> differences, string hex, string field, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"{hex}.{field}: expected {Describe(expected)}, derived {Describe(actual)}"));
        }
    }

    private static string Describe(object? value) => value switch
    {
        null => "none",
        LocationFacts location => $"level {location.Level} {location.Terrain?.Name ?? "none"}"
            + (location.DepressionTerrain is { } depression ? $" in {depression.Name}" : string.Empty),
        HexsideFacts side => $"{side.Terrain?.Name ?? "none"}"
            + (side.HexsideTerrain is { } terrain ? $" with {terrain.Name}" : string.Empty)
            + (side.OnMap ? string.Empty : " off map"),
        BridgeFacts bridge => $"{bridge.Terrain?.Name ?? "none"} at road level {bridge.RoadLevel}",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };
}
