using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// One Studio icon: its file name without <c>.svg</c>, the control it labels, the group it belongs to (such as "Map toolbar"), and its SVG
/// markup, or null when the manifest names a file that is missing.
/// </summary>
public sealed record StudioIcon(string Key, string Label, string Group, string? Markup);

/// <summary>
/// The Studio's icons under <c>wwwroot/icons</c>: <c>icons.json</c> lists them in order, and each is a 24-unit SVG whose colors are the
/// <c>--i-*</c> variables of an icon palette in site.css, with the Midjourney palette as their fallbacks. They are written by
/// <c>code-builders/studio-icons/make_icons.py</c>; the Icon Lab shows them.
/// </summary>
public sealed class IconLibrary(IFileProvider webRoot) : IArtShelf
{
    public const string Folder = "icons";
    public const string Manifest = "icons.json";

    /// <summary>The icon key that marks a tab as keeping its words by design.</summary>
    public const string TextByDesign = "none";

    private IReadOnlyList<StudioIcon>? icons;
    private string? problem;
    private IconCoverage? coverage;
    private bool coverageRead;

    public IconLibrary(IWebHostEnvironment environment)
        : this(environment.WebRootFileProvider)
    {
    }

    /// <summary>Why the manifest could not be read, if it could not; the list is then empty.</summary>
    public string? Problem => Icons.Count == 0 ? problem : null;

    public IReadOnlyList<StudioIcon> Icons => icons ??= Load();

    /// <summary>The groups of the icons, in the order the manifest first names them.</summary>
    public IReadOnlyList<string> Groups => Icons.Select(icon => icon.Group).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Which buttons and tabs of each component have icons, or null when icon-coverage.json is missing.</summary>
    public IconCoverage? Coverage
    {
        get
        {
            if (!coverageRead)
            {
                coverage = IconCoverage.Read(webRoot);
                coverageRead = true;
            }

            return coverage;
        }
    }

    public StudioIcon? Find(string key) => Icons.FirstOrDefault(icon => icon.Key == key);

    IEnumerable<string> IArtShelf.Keys => Icons.Select(icon => icon.Key);

    string IArtShelf.Noun => "icon";

    bool IArtShelf.Has(string key) => Find(key)?.Markup is not null;

    string? IArtShelf.Thumbnail(string key, int pixels) => Find(key) is { } icon ? Sized(icon, pixels) : null;

    /// <summary>An icon's markup drawn at a size in CSS pixels, or null when it has none.</summary>
    public static string? Sized(StudioIcon icon, int pixels) =>
        icon.Markup?.Replace("width=\"24\" height=\"24\"", $"width=\"{pixels}\" height=\"{pixels}\"", StringComparison.Ordinal);

    private List<StudioIcon> Load()
    {
        var manifest = webRoot.GetFileInfo($"{Folder}/{Manifest}");
        if (!manifest.Exists)
        {
            problem = $"wwwroot/{Folder}/{Manifest} is missing.";
            return [];
        }

        List<ManifestEntry>? entries;
        try
        {
            using var stream = manifest.CreateReadStream();
            entries = JsonSerializer.Deserialize<List<ManifestEntry>>(stream, JsonOptions);
        }
        catch (JsonException e)
        {
            problem = $"wwwroot/{Folder}/{Manifest} is not valid: {e.Message}";
            return [];
        }

        return (entries ?? []).Select(entry => new StudioIcon(entry.Key, entry.Label, entry.Group, Read(entry.Key))).ToList();
    }

    private string? Read(string key)
    {
        var file = webRoot.GetFileInfo($"{Folder}/{key}.svg");
        if (!file.Exists)
        {
            return null;
        }

        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd().Trim();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record ManifestEntry(string Key, string Label, string Group);
}
