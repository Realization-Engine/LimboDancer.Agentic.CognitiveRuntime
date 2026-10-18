using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The Midjourney prompt template every batch shares. A prompt is the opening, the four numbered briefs after "Icons:", the style, the
/// batch's <c>--sref</c> when it has one, and the parameters. A one-icon batch opens with <see cref="SingleOpening"/> and has its one
/// brief after "Icon:", so Midjourney's four images are four tries at the same icon.
/// </summary>
public sealed record IconPromptStyle(string Opening, string Style, string Parameters, string SingleOpening = "");

/// <summary>
/// One icon to make or already made: its key (the file name in wwwroot/icons without <c>.svg</c>), the label of the control it stands
/// for, the brief (what is drawn, one sentence, as it goes into a prompt's numbered slot), what the drawing must respect, and the
/// Midjourney result chosen as its reference.
/// </summary>
public sealed record IconBrief(string Key, string Label, string Brief, string Meaning = "", string Reference = "");

/// <summary>
/// A control's icon decision: the control's key (<see cref="IconPlan.ControlKey"/>) and an icon key, or
/// <see cref="IconLibrary.TextByDesign"/> when it keeps its words.
/// </summary>
public sealed record IconAssignment(string Control, string Icon);

/// <summary>
/// A batch of up to four briefs prompted together, in the order of Midjourney's 2x2 grid, with its style reference; or, when
/// <see cref="OneIcon"/>, one brief prompted alone for four tries at it.
/// </summary>
/// <see cref="Sent"/> is the prompt exactly as saved and copied, so a brief edited later does not rewrite what this batch sent.
public sealed record IconBatch(string Id, string Name, IReadOnlyList<string> Icons, string Sref, string Created, bool OneIcon = false, string Sent = "");

/// <summary>
/// The Icon Lab's plan, kept in <c>wwwroot/icons/icon-plan.json</c> and committed with the icons: the prompt style, the briefs, which
/// control uses which icon, and the batches prompted.
/// </summary>
public sealed partial record IconPlan(IconPromptStyle Style, IReadOnlyList<IconBrief> Briefs, IReadOnlyList<IconAssignment> Assignments,
    IReadOnlyList<IconBatch> Batches)
{
    public const string File = "icon-plan.json";

    /// <summary>The slots of a batch: Midjourney's 2x2 grid.</summary>
    public const int Slots = 4;

    public static readonly IReadOnlyList<string> SlotNames = ["top left", "top right", "bottom left", "bottom right"];

    public static IconPlan Empty { get; } = new(new IconPromptStyle(string.Empty, string.Empty, string.Empty), [], [], []);

    public IconBrief? Brief(string key) => Briefs.FirstOrDefault(brief => brief.Key == key);

    public string? IconOf(string control) => Assignments.FirstOrDefault(assignment => assignment.Control == control)?.Icon;

    public IEnumerable<string> ControlsOf(string icon) => Assignments.Where(assignment => assignment.Icon == icon).Select(assignment => assignment.Control);

    public bool InBatch(string key) => Batches.Any(batch => batch.Icons.Contains(key));

    /// <summary>
    /// A control's key: its component and id, or its component and label when it has no id; null when it has neither an id nor a label
    /// fixed in the source, since such a control cannot be found again.
    /// </summary>
    public static string? ControlKey(ComponentCoverage component, ControlCoverage control) =>
        control.Id is { } id ? $"{component.Name}#{id}" : control.Dynamic ? null : $"{component.Name}:{control.Label}";

    /// <summary>The full Midjourney prompt of a batch's briefs, in slot order; or, for one icon, of its first brief alone.</summary>
    public string Prompt(IReadOnlyList<IconBrief> briefs, string sref, bool oneIcon = false)
    {
        var (before, numbered, after) = PromptParts(briefs, sref, oneIcon);
        return before + numbered + after;
    }

    /// <summary>
    /// A prompt in three parts: the opening up to "Icons:" (or "Icon:" for one icon), the briefs, and the rest (the style, the --sref,
    /// the parameters), so a page can show the batch's own words apart from the shared style.
    /// </summary>
    public (string Before, string Briefs, string After) PromptParts(IReadOnlyList<IconBrief> briefs, string sref, bool oneIcon = false)
    {
        var after = new StringBuilder(". ").Append(Style.Style.Trim());
        if (sref.Trim() is { Length: > 0 } reference)
        {
            after.Append(" --sref ").Append(reference);
        }

        after.Append(' ').Append(Style.Parameters.Trim());
        return oneIcon
            ? (Style.SingleOpening.Trim() + " Icon: ", briefs.Count == 0 ? string.Empty : Clean(briefs[0]), after.ToString())
            : (Style.Opening.Trim() + " Icons: ", string.Join("; ", briefs.Select((brief, index) => $"{index + 1}. {Clean(brief)}")), after.ToString());
    }

    /// <summary>The prompt a saved batch sent: the one kept with it, or, for a batch saved before prompts were kept, the one its briefs make
    /// now (null when one of them is gone).</summary>
    public string? Prompt(IconBatch batch)
    {
        if (batch.Sent.Length > 0)
        {
            return batch.Sent;
        }

        var briefs = batch.Icons.Select(Brief).ToList();
        return briefs.Any(brief => brief is null) ? null : Prompt(briefs!, batch.Sref, batch.OneIcon);
    }

    private static string Clean(IconBrief brief) => brief.Brief.Trim().TrimEnd('.', ';');

    public IconPlan WithBrief(IconBrief brief) =>
        this with
        {
            Briefs = [.. Briefs.Where(other => other.Key != brief.Key), brief]
        };

    /// <summary>Records a control's icon, or clears its decision when the icon is null.</summary>
    public IconPlan WithAssignment(string control, string? icon)
    {
        var others = Assignments.Where(other => other.Control != control);
        return this with
        {
            Assignments = icon is null ? [.. others] : [.. others, new IconAssignment(control, icon)]
        };
    }

    public IconPlan WithReference(string key, string reference) =>
        Brief(key) is { } brief ? WithBrief(brief with
        {
            Reference = reference.Trim()
        }) : this;

    public IconPlan WithBatch(IconBatch batch) =>
        this with
        {
            Batches = [.. Batches.Where(other => other.Id != batch.Id), batch]
        };

    public IconPlan WithoutBatch(string id) => this with { Batches = [.. Batches.Where(batch => batch.Id != id)] };

    /// <summary>A new batch id: the date and the next number that day.</summary>
    public string NextBatchId(DateOnly day)
    {
        var prefix = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var number = 1;
        while (Batches.Any(batch => batch.Id == $"{prefix}-{number}"))
        {
            number++;
        }

        return $"{prefix}-{number}";
    }

    /// <summary>A key made from a label: lower case, words joined by hyphens.</summary>
    public static string Slug(string label) => NotSlug().Replace(label.ToLowerInvariant(), "-").Trim('-');

    public static bool IsKey(string key) => key.Length > 0 && KeyPattern().IsMatch(key);

    public string Write() => JsonSerializer.Serialize(this, IconCoverage.Json).ReplaceLineEndings("\n") + "\n";

    public static IconPlan Read(string json) => JsonSerializer.Deserialize<IconPlan>(json, IconCoverage.Json) ?? Empty;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotSlug();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KeyPattern();
}

/// <summary>How far an icon has come, from a brief to a drawing the Studio uses.</summary>
public enum IconStage
{
    /// <summary>A brief, in no batch yet.</summary>
    Briefed,

    /// <summary>In a batch prompted to Midjourney.</summary>
    Prompted,

    /// <summary>A Midjourney result chosen as its reference.</summary>
    Referenced,

    /// <summary>Its SVG is in wwwroot/icons.</summary>
    Drawn,

    /// <summary>A control in the source draws it.</summary>
    Wired,
}

public static class IconStages
{
    public static IconStage Of(string key, IconPlan plan, IArtShelf shelf, IconCoverage? coverage)
    {
        if (coverage is not null && coverage.Controls.Any(control => control.Status == IconCoverage.HasIcon && control.Icon == key))
        {
            return IconStage.Wired;
        }

        if (shelf.Has(key))
        {
            return IconStage.Drawn;
        }

        if (plan.Brief(key) is { Reference.Length: > 0 })
        {
            return IconStage.Referenced;
        }

        return plan.InBatch(key) ? IconStage.Prompted : IconStage.Briefed;
    }

    public static string Words(IconStage stage) => stage switch
    {
        IconStage.Briefed => "briefed",
        IconStage.Prompted => "in a batch",
        IconStage.Referenced => "reference chosen",
        IconStage.Drawn => "drawn",
        _ => "wired",
    };
}

/// <summary>
/// Reads and writes the plan file. The Studio runs from its project folder, so a save changes the committed file; the caller commits
/// it. Saves are whole-file and serialized.
/// </summary>
public sealed class IconPlanStore(string filePath)
{
    private readonly Lock gate = new();

    public IconPlanStore(IWebHostEnvironment environment)
        : this(Path.Combine(environment.WebRootPath, IconLibrary.Folder, IconPlan.File))
    {
    }

    public string FilePath { get; } = filePath;

    public IconPlan Load()
    {
        lock (gate)
        {
            return Read();
        }
    }

    /// <summary>Applies a change to the plan as it is on disk and saves it; returns the saved plan.</summary>
    public IconPlan Update(Func<IconPlan, IconPlan> change)
    {
        lock (gate)
        {
            var plan = change(Read());
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, plan.Write());
            File.Move(temporary, FilePath, overwrite: true);
            return plan;
        }
    }

    private IconPlan Read() => File.Exists(FilePath) ? IconPlan.Read(File.ReadAllText(FilePath)) : IconPlan.Empty;
}
