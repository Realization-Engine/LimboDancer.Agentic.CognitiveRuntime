using System.Text.Json;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Read-only provenance for a complete campaign import, never a source of live game state.</summary>
public sealed record CampaignContextSummary(int Formations, int Counters, int Aircraft)
{
    public static CampaignContextSummary? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.GetProperty("Version").GetString() != "campaign-goodwood-handoff-1")
            {
                return null;
            }
            var roster = root.GetProperty("Roster");
            var ground = roster.GetProperty("Ground");
            return new(ground.GetArrayLength(), ground.EnumerateArray().Sum(f => f.GetProperty("Units").GetArrayLength()),
                roster.GetProperty("AirSupport").GetProperty(nameof(Aircraft)).GetArrayLength());
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    // Navigation is configured by the local host, not by an imported save's untrusted URL.
    public static string? ReturnUrl(string? origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !uri.IsLoopback ||
            uri.Scheme is not ("http" or "https") || uri.UserInfo.Length != 0 ||
            uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0)
        {
            return null;
        }
        return new Uri(uri, "/index.html?exercise=source&source=5").AbsoluteUri;
    }
}
