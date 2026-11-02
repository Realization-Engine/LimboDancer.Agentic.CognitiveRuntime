using System.Globalization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>How a Map Lab map is drawn (Map Lab Plan, section 3.3).</summary>
public enum LabTechnique
{
    /// <summary>An SVG element per hex, panned by the view box alone: the baseline.</summary>
    Elements,

    /// <summary>One path per terrain colour and one for the grid.</summary>
    Paths,

    /// <summary>Terrain as raster tiles, grid and boundaries as paths.</summary>
    Tiles,

    /// <summary>Terrain, grid, and boundaries all painted into the raster tiles (Map Lab Plan, section 8, findings F3 and F4).</summary>
    TilesLines,
}

/// <summary>
/// A Map Lab map, wholly described by its form values (Map Lab Plan, section 3.1, decision L1): the same values make the same map, so the map is
/// never saved and its key is its address.
/// </summary>
public sealed partial record LabMapSpec(int Hexes, int Group, int Seed, LabTechnique Technique)
{
    /// <summary>The largest map the Lab builds (decision L2).</summary>
    public const int MaxHexes = 1_000_000;

    public const int MinHexes = 4;

    public const int MaxGroup = 400;

    /// <summary>The key in the map's address: <c>n{hexes}-g{group}-s{seed}-{technique}</c>.</summary>
    public string Key => string.Create(CultureInfo.InvariantCulture, $"n{Hexes}-g{Group}-s{Seed}-{Technique.ToString().ToLowerInvariant()}");

    /// <summary>The key of the map without its technique: two techniques of one map share its hexes.</summary>
    public string MapKey => string.Create(CultureInfo.InvariantCulture, $"n{Hexes}-g{Group}-s{Seed}");

    /// <summary>Whether the terrain is drawn as raster tiles.</summary>
    public bool Tiled => Technique is LabTechnique.Tiles or LabTechnique.TilesLines;

    /// <summary>A spec with its values brought into range: hexes 4 to 1,000,000, group 1 (no formations) to 400.</summary>
    public static LabMapSpec Clamped(int hexes, int group, int seed, LabTechnique technique) =>
        new(Math.Clamp(hexes, MinHexes, MaxHexes), Math.Clamp(group, 1, MaxGroup), seed, technique);

    /// <summary>Reads a key, or false when it is not one or is out of range.</summary>
    public static bool TryReadKey(string? key, out LabMapSpec spec)
    {
        spec = new LabMapSpec(MinHexes, 1, 0, LabTechnique.Elements);
        var match = key is null ? Match.Empty : KeyPattern().Match(key);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var hexes)
            || !int.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var group)
            || !int.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out var seed)
            || !Enum.TryParse<LabTechnique>(match.Groups[4].Value, ignoreCase: true, out var technique))
        {
            return false;
        }

        spec = new LabMapSpec(hexes, group, seed, technique);
        return spec == Clamped(hexes, group, seed, technique);
    }

    [GeneratedRegex(@"^n(\d{1,7})-g(\d{1,3})-s(\d{1,9})-(elements|paths|tiles|tileslines)$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
