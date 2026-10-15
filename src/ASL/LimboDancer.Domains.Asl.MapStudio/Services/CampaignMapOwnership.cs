using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Validated, optional campaign inspection data. Does not change ASL terrain or events.</summary>
public static class CampaignMapOwnership
{
    public static IReadOnlyDictionary<string, string> Read(string json, BoardPackage board)
    {
        if (Encoding.UTF8.GetByteCount(json) > 2_000_000)
        {
            throw new InvalidDataException("Campaign map ownership is too large.");
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.GetProperty("Version").GetString() != "goodwood-map-ownership-1" ||
                root.GetProperty("Board").GetString() != board.Board.Value ||
                root.GetProperty("BoardVersion").GetString() != board.Version)
            {
                throw new InvalidDataException("Campaign ownership does not match this board version.");
            }
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parent in root.GetProperty("Parents").EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(parent.Name) || parent.Name.Length > 100 || !parents.Add(parent.Name) ||
                    parents.Count > 4096 || parent.Value.GetArrayLength() == 0)
                {
                    throw new InvalidDataException("Invalid campaign parent identity.");
                }
                foreach (var item in parent.Value.EnumerateArray())
                {
                    var name = item.GetString();
                    if (name is null || !HexName.TryParse(name, out var hex) || hex.ToString() != name ||
                        !board.Model.Geometry.TryGetIndex(hex, out _) || !result.TryAdd(name, parent.Name))
                    {
                        throw new InvalidDataException("Invalid or duplicate campaign ownership hex.");
                    }
                }
            }
            if (result.Count == 0)
            {
                throw new InvalidDataException("Campaign ownership is empty.");
            }
            return result;
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException("Invalid campaign map ownership document.", error);
        }
    }
}
