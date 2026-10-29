using System.Globalization;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>A feature of a board (the MapHexIndex Design, ruling RMH.2): its id ("terrain:Woods"), its kind and name, and the hexes that hold it.</summary>
public sealed record MapHexFeature(string Id, string Kind, string Name, IReadOnlyList<string> Hexes);

/// <summary>One board of the index: its name, the batch's outcome, its LOSData blob, and its features; none for a board the batch could not derive.</summary>
public sealed record MapHexIndexBoard(string Board, string Outcome, string? LosDataBlob, IReadOnlyList<MapHexFeature> Features);

/// <summary>
/// The saved index of map hexes by terrain (the Rules-first plan, section 7.3; the user's name, 2026-10-28): for each board of a fidelity batch, every
/// terrain and feature it holds and the hexes where each occurs, stamped with the versions and the source it was derived from (ruling RMH.4).
/// </summary>
public sealed record MapHexIndex(string IndexVersion, string DerivationVersion, string? VaslCommit, string? SharedBoardMetadataBlob, string Report,
    IReadOnlyList<MapHexIndexBoard> Boards)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static MapHexIndex? FromJson(string json) => JsonSerializer.Deserialize<MapHexIndex>(json, Json);

    /// <summary>Every feature id any board holds, in ordinal order.</summary>
    public IReadOnlyList<string> FeatureIds() => [.. Boards.SelectMany(board => board.Features).Select(feature => feature.Id).Distinct().Order(StringComparer.Ordinal)];

    /// <summary>The boards that hold every one of the features, in the index's order.</summary>
    public IReadOnlyList<MapHexIndexBoard> BoardsWith(IReadOnlyCollection<string> featureIds) =>
        [.. Boards.Where(board => featureIds.All(id => board.Features.Any(feature => feature.Id == id)))];
}

/// <summary>The pure function behind the index (ruling RMH.2): a board's derived hex facts to its features.</summary>
public static class MapHexIndexer
{
    /// <summary>The index's own version: changed when the features read from the facts change.</summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// A board's features, sorted by id, each with its hexes in the board's hex order. Each Location's terrain and depression terrain, each on-map
    /// hexside's terrain, hexside terrain, depression terrain, and flags (cliff, slope, railroad embankment, partial orchard), the hex's bridge,
    /// stairway, and base level when it is not 0.
    /// </summary>
    public static IReadOnlyList<MapHexFeature> Features(HexFactSet facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var found = new Dictionary<string, (string Kind, string Name, List<string> Hexes)>(StringComparer.Ordinal);
        void Add(string kind, string? name, string hex)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var id = kind + ":" + name;
            if (!found.TryGetValue(id, out var entry))
            {
                entry = (kind, name, []);
                found[id] = entry;
            }

            if (entry.Hexes.Count == 0 || entry.Hexes[^1] != hex)
            {
                entry.Hexes.Add(hex);
            }
        }

        foreach (var hex in facts.Hexes)
        {
            var name = hex.Hex.ToString();
            foreach (var location in hex.Locations)
            {
                Add("terrain", location.Terrain?.Name, name);
                Add("depression", location.DepressionTerrain?.Name, name);
            }

            foreach (var side in hex.Hexsides.Where(side => side.OnMap))
            {
                Add("terrain", side.Terrain?.Name, name);
                Add("hexside", side.HexsideTerrain?.Name, name);
                Add("depression", side.DepressionTerrain?.Name, name);
                Add("flag", side.Cliff ? "cliff" : null, name);
                Add("flag", side.Slope ? "slope" : null, name);
                Add("flag", side.RailroadEmbankment ? "railroad-embankment" : null, name);
                Add("flag", side.PartialOrchard ? "partial-orchard" : null, name);
            }

            Add("bridge", hex.Bridge?.Terrain?.Name, name);
            Add("flag", hex.Stairway ? "stairway" : null, name);
            Add("level", hex.BaseLevel != 0 ? hex.BaseLevel.ToString(CultureInfo.InvariantCulture) : null, name);
        }

        return [.. found.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new MapHexFeature(pair.Key, pair.Value.Kind, pair.Value.Name, [.. pair.Value.Hexes.Distinct(StringComparer.Ordinal)]))];
    }
}
