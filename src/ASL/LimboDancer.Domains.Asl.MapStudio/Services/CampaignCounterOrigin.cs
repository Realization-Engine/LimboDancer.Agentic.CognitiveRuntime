using System.Text.Json;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Immutable campaign provenance, never the counter's current ASL position or capabilities.</summary>
public sealed record CampaignCounterOrigin(string Formation, string FormationHex, string Definition, string CatalogStatus)
{
    public static IReadOnlyDictionary<string, CampaignCounterOrigin> Read(string? json)
    {
        var result = new Dictionary<string, CampaignCounterOrigin>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.GetProperty("Version").GetString() != "campaign-goodwood-handoff-1")
            {
                return result;
            }
            var anchors = root.GetProperty("Setup").GetProperty("Formations").EnumerateArray()
                .ToDictionary(f => f.GetProperty("UnitId").GetString()!, f => f.GetProperty("Anchor").GetString()!, StringComparer.Ordinal);
            foreach (var formation in root.GetProperty("Roster").GetProperty("Ground").EnumerateArray())
            {
                var parent = formation.GetProperty("Id").GetString()!;
                foreach (var unit in formation.GetProperty("Units").EnumerateArray())
                {
                    if (unit.GetProperty("ParentId").GetString() != parent || !anchors.TryGetValue(parent, out var anchor))
                    {
                        return new Dictionary<string, CampaignCounterOrigin>();
                    }
                    var definition = unit.GetProperty(nameof(Definition));
                    var catalog = definition.GetProperty("Catalog");
                    var reference = catalog.GetProperty("Catalog").GetString() + "@" + catalog.GetProperty("Version").GetString()
                        + " / " + definition.GetProperty(nameof(Definition)).GetString();
                    result.Add(unit.GetProperty("Id").GetString()!.Replace("/", "--", StringComparison.Ordinal),
                        new(parent, anchor, reference, unit.GetProperty(nameof(CatalogStatus)).GetString() ?? "Unspecified profile provenance"));
                }
            }
            return result;
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or NullReferenceException)
        {
            return new Dictionary<string, CampaignCounterOrigin>();
        }
    }
}
