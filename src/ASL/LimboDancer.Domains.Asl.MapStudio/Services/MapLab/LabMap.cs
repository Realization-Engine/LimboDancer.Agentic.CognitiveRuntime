using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>The terrain kinds of a generated map (decision L3).</summary>
public enum LabTerrain : byte
{
    Open,
    Woods,
    Town,
    Water,
    Marsh,
}

/// <summary>
/// A generated map: its geometry and, by hex ordinal (<see cref="BoardGeometry.HexOrdinal"/>), each hex's terrain, level (0 to 2), and
/// formation. One array per field, a few bytes a hex (the guidelines' compact per-hex tables), so a million hexes stay a few megabytes.
/// </summary>
public sealed class LabMap(LabMapSpec spec, BoardGeometry geometry, LabTerrain[] terrain, byte[] levels, int[] parents, int formations)
{
    public LabMapSpec Spec { get; } = spec;

    public BoardGeometry Geometry { get; } = geometry;

    public LabTerrain[] Terrain { get; } = terrain;

    public byte[] Levels { get; } = levels;

    /// <summary>Each hex's formation; every hex its own when the spec's group is 1.</summary>
    public int[] Parents { get; } = parents;

    /// <summary>The number of formations that hold at least one hex.</summary>
    public int Formations { get; } = formations;

    public int HexCount => Terrain.Length;

    /// <summary>The same hexes drawn by another technique: the arrays are shared, not copied.</summary>
    public LabMap With(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec.MapKey == Spec.MapKey
            ? new LabMap(spec, Geometry, Terrain, Levels, Parents, Formations)
            : throw new ArgumentException("Another map's spec.", nameof(spec));
    }

    /// <summary>The colour a hex is drawn in: its terrain, darker with each level.</summary>
    public string ColourOf(int ordinal) => Colours[(int)Terrain[ordinal], Levels[ordinal]];

    /// <summary>Colours by terrain and level, as RGB hex strings.</summary>
    public static readonly string[,] Colours =
    {
        { "#ece4bd", "#ddd09c", "#cbbb80" },
        { "#93ac68", "#829c5a", "#728d4d" },
        { "#c8a888", "#bb9a7a", "#ad8c6c" },
        { "#7cbac6", "#7cbac6", "#7cbac6" },
        { "#b9cfa9", "#b9cfa9", "#b9cfa9" },
    };
}
