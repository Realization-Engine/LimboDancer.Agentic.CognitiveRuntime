using System.Diagnostics.CodeAnalysis;

namespace LimboDancer.Domains.Asl.Maps.Terrain;

/// <summary>
/// The terrain types a board's grid codes refer to, keyed by code. Names are also unique; VASL itself
/// keys its terrain table by name, so a catalog with duplicate names or codes is rejected.
/// </summary>
public sealed class TerrainCatalog
{
    private readonly TerrainType?[] byCode = new TerrainType?[256];
    private readonly Dictionary<string, TerrainType> byName;

    public TerrainCatalog(IEnumerable<TerrainType> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        var ordered = types.OrderBy(type => type.Code).ToArray();
        byName = new Dictionary<string, TerrainType>(StringComparer.Ordinal);
        foreach (var type in ordered)
        {
            if (byCode[type.Code] is not null)
            {
                throw new ArgumentException($"Duplicate terrain code {type.Code}.", nameof(types));
            }

            if (!byName.TryAdd(type.Name, type))
            {
                throw new ArgumentException($"Duplicate terrain name '{type.Name}'.", nameof(types));
            }

            byCode[type.Code] = type;
        }

        Types = ordered;
    }

    /// <summary>All types in ascending code order.</summary>
    public IReadOnlyList<TerrainType> Types
    {
        get;
    }

    public int Count => Types.Count;

    public bool TryGet(byte code, [NotNullWhen(true)] out TerrainType? type)
    {
        type = byCode[code];
        return type is not null;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out TerrainType? type) => byName.TryGetValue(name, out type);

    public TerrainType this[byte code] =>
        byCode[code] ?? throw new KeyNotFoundException($"Terrain code {code} is not in the catalog.");

    public TerrainType this[string name] =>
        byName.TryGetValue(name, out var type) ? type : throw new KeyNotFoundException($"Terrain '{name}' is not in the catalog.");
}
