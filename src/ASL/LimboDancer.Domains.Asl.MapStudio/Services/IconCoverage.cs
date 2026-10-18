using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// Which buttons and tabs of each Blazor component draw an icon, which still need one, and which keep their words by design, read
/// from <c>wwwroot/icons/icon-coverage.json</c>. The file is written from the Razor sources by <c>IconCoverageTests</c> (set
/// <c>ASL_ICONS_UPDATE_COVERAGE=1</c>), and the same test fails when the sources and the file disagree, so a new button anywhere shows
/// in the Icon Lab as a gap.
/// </summary>
public sealed record IconCoverage(IReadOnlyList<ComponentCoverage> Components)
{
    /// <summary>The file's name; not coverage.json, which .gitignore keeps for code-coverage reports.</summary>
    public const string File = "icon-coverage.json";

    /// <summary>A control draws an icon.</summary>
    public const string HasIcon = "icon";

    /// <summary>A control has no icon and no decision against one.</summary>
    public const string Needed = "needed";

    /// <summary>A control keeps its words by design: <c>data-icon="none"</c> on a button, <c>Icon: "none"</c> on a tab.</summary>
    public const string Text = "text";

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [JsonIgnore]
    public IEnumerable<ControlCoverage> Controls => Components.SelectMany(component => component.Controls);

    public string Write() => JsonSerializer.Serialize(this, Json).ReplaceLineEndings("\n") + "\n";

    /// <summary>The coverage under a web root, or null when the file is missing or not valid.</summary>
    public static IconCoverage? Read(IFileProvider webRoot)
    {
        var file = webRoot.GetFileInfo($"{IconLibrary.Folder}/{File}");
        if (!file.Exists)
        {
            return null;
        }

        try
        {
            using var stream = file.CreateReadStream();
            return JsonSerializer.Deserialize<IconCoverage>(stream, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// One component (a .razor file): its name, its path under the Studio project, the routes of the pages it appears on (itself, or a page
/// that places it, directly or through other components), the components that place it, and its buttons and tabs in source order.
/// </summary>
public sealed record ComponentCoverage(string Name, string Path, IReadOnlyList<string> Pages, IReadOnlyList<string> UsedBy, IReadOnlyList<ControlCoverage> Controls)
{
    public int Count(string status) => Controls.Count(control => control.Status == status);
}

/// <summary>
/// One button or tab: "button" or "tab", its label (its tooltip, accessible name, or words; the source expression when the label is
/// worked out at run time, with <see cref="Dynamic"/> set), its id when it has one, its icon key, and its status; the strip it sits in
/// (a toolbar or tab strip, named the same wherever its members are written), and the component it renders in when that is not the one
/// that writes it, such as a tab Play hands to BoardInspector.
/// </summary>
public sealed record ControlCoverage(string Kind, string Label, bool Dynamic, string? Id, string? Icon, string Status, string? Strip = null,
    string? RendersIn = null);
