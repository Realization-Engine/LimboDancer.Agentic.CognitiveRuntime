using System.Globalization;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The Unit Lab's Art (ASL Unit Figure Art Plan, stage 3): the figure parts embedded in the renderer, stand-ins included; the plan of
/// briefs, prompts, and batches in <c>units/figures/figure-plan.json</c>; and the sample counters each side and pose is judged on. A pose's
/// key is <c>pose-&lt;pose&gt;</c>, a helmet's <c>&lt;side&gt;-helmet</c>, a weapon's <c>&lt;side&gt;-&lt;weapon&gt;</c>. A pose counts as
/// drawn once its traced art is accepted; a helmet or a weapon once it is drawn (Claude draws the kit, so it has no Midjourney stage).
/// </summary>
public sealed class FigureArtLab(string folder) : IArtShelf
{
    public const string PlanFile = "figure-plan.json";

    /// <summary>The catalog's sides, in the order the plan lists them (decision F5).</summary>
    public static readonly IReadOnlyList<string> Sides = ["german", "russian", "british", "american", "finnish", "italian", "axis-minor", "french"];

    public FigureArtLab(IWebHostEnvironment environment)
        : this(Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "units", "figures")))
    {
    }

    /// <summary>The figure sources' folder, where the plan file is kept.</summary>
    public string Folder { get; } = folder;

    public IconPlanStore Plans { get; } = new(Path.Combine(folder, PlanFile));

    /// <summary>Every part embedded, stand-ins included; a source changed since the build shows after build_figures.py and a rebuild.</summary>
    public FigureArt Art { get; } = FigureArt.BuiltIn.WithStandIns();

    public static string PoseKey(string pose) => "pose-" + pose;

    /// <summary>How far a pose has come: traced and accepted, a stand-in, or nothing yet.</summary>
    public string PoseState(string pose) => FigureArt.BuiltIn.Pose(pose) is not null ? "traced" : Art.Pose(pose) is not null ? "stand-in" : "none";

    /// <summary>What a side's figure in a pose still lacks: its pose, its helmet, or the weapon the pose holds.</summary>
    public IReadOnlyList<string> Missing(string pose, string side)
    {
        var missing = new List<string>();
        var drawn = Art.Pose(pose);
        if (FigureArt.BuiltIn.Pose(pose) is null)
        {
            missing.Add(drawn is null ? "pose" : "pose (stand-in)");
        }

        if (Art.Helmet(side) is null)
        {
            missing.Add("helmet");
        }

        if (drawn is { Grip: not null, Weapon: { } weapon } && Art.WeaponOf(side, weapon) is null)
        {
            missing.Add(weapon == "smg" ? "SMG" : weapon);
        }

        return missing;
    }

    /// <summary>
    /// The counter a side's figure is judged on in a pose: the Studio's synthetic example of the kind that takes the pose, given the side,
    /// with nothing attached, and the face or state that chooses the pose; null for a pose no counter can show yet (the prisoner, stage 7).
    /// </summary>
    public static (UnitDocument Document, string? Face)? Sample(IReadOnlyList<UnitDocument> examples, string pose, string side)
    {
        (string Id, string? Face, string? State)? choice = pose switch
        {
            "standing" => ("example-squad", null, null),
            "crew" => ("example-crew", null, null),
            "leader" => ("example-leader", null, null),
            "hero" => ("example-hero", null, null),
            "wounded" => ("example-hero", "wounded", null),
            "broken" => ("example-squad", "broken", null),
            "pinned" => ("example-squad", null, "asl:pinned"),
            "berserk" => ("example-squad", null, "asl:berserk"),
            _ => null,
        };
        if (choice is not { } chosen || examples.FirstOrDefault(example => example.Id == chosen.Id) is not { } document)
        {
            return null;
        }

        return (document with
        {
            Id = $"{chosen.Id}-{side}",
            Side = side,
            Attached = [],
            States = chosen.State is { } state ? [.. document.States, state] : document.States,
        }, chosen.Face);
    }

    /// <summary>A renderer like the given one that draws the stand-ins too.</summary>
    public UnitRenderer WithStandIns(UnitRenderer renderer) => new(renderer.Vocabulary, renderer.Sheet, renderer.Palette) { Figures = Art };

    public IEnumerable<string> Keys =>
        FigureArt.PoseNames.Where(pose => FigureArt.BuiltIn.Pose(pose) is not null).Select(PoseKey)
            .Concat(Art.HelmetSides.Select(side => side + "-helmet"))
            .Concat(Art.WeaponKeys.Select(key => $"{key.Side}-{key.Weapon}"));

    public string Noun => "figure part";

    public bool Has(string key) => Keys.Contains(key, StringComparer.Ordinal);

    /// <summary>A part drawn alone in the current color: a pose in its box, a helmet about its head, a weapon along its grip.</summary>
    public string? Thumbnail(string key, int pixels)
    {
        var (path, box) = Part(key);
        return path is null ? null
            : string.Create(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{box}\" width=\"{pixels}\" height=\"{pixels}\" aria-hidden=\"true\"><path d=\"{path}\" fill=\"currentColor\"/></svg>");
    }

    private (string? Path, string Box) Part(string key)
    {
        if (key.StartsWith("pose-", StringComparison.Ordinal) && Art.Pose(key["pose-".Length..]) is { } pose)
        {
            var side = Math.Max(pose.Width, pose.Height);
            return (pose.Path, string.Create(CultureInfo.InvariantCulture, $"{(pose.Width - side) / 2} {(pose.Height - side) / 2} {side} {side}"));
        }

        var dash = key.LastIndexOf('-');
        if (dash <= 0)
        {
            return (null, string.Empty);
        }

        var (owner, part) = (key[..dash], key[(dash + 1)..]);
        return part == "helmet" ? (Art.Helmet(owner)?.Path, "-200 -200 400 400") : (Art.WeaponOf(owner, part)?.Path, "-320 -500 1000 1000");
    }
}
