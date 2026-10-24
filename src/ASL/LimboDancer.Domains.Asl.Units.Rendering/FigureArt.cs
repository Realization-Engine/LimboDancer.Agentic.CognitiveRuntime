using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// A traced body pose (ASL Unit Figure Art Plan, section 3): one filled path, bareheaded and unarmed, in body units (a man standing is
/// 1000 high), in a box <see cref="Width"/> wide and <see cref="Height"/> high: 1000 for an upright pose, less for a lying one, cropped to
/// the body; with the head's center and radius (where the helmet sits) and, when the pose holds a weapon, the grip: the line
/// from the rear hand toward the front hand, which places and turns the weapon of kind <see cref="Weapon"/>.
/// </summary>
public sealed record FigurePose(string Path, double Width, FigureHead Head, FigureGrip? Grip, string? Weapon, bool Accepted, string Source,
    double Height = 1000, bool OwnHelmet = false, FigureBody? Body = null);

/// <summary>
/// Where the body lies across a pose, apart from what it carries beyond it (a rifle trailed behind): a file of men is spaced by the body's
/// width, so a long weapon does not push the men apart and shrink them.
/// </summary>
public sealed record FigureBody(double Left, double Right);

/// <summary>The head's center and radius, and its tilt in degrees clockwise (a bowed head's is positive), which turns the side's helmet with it.</summary>
public sealed record FigureHead(double X, double Y, double Radius, double Tilt = 0);

public sealed record FigureGrip(double X1, double Y1, double X2, double Y2);

/// <summary>
/// A side's helmet, drawn about a head of radius 100 at the origin and scaled to the pose's head; or a side's weapon, drawn in body units
/// (a figure is 1000 high) with the rear hand at the origin and the muzzle toward +x, placed and turned by the pose's grip. One filled path
/// either way.
/// </summary>
public sealed record FigureKit(string Path, bool Accepted, string Source);

/// <summary>
/// The figure parts the personnel counters are drawn from: <c>units/figures/figures.json</c>, written by
/// <c>code-builders/unit-figures/build_figures.py</c> from the SVG sources beside it and embedded here. A part not yet accepted by the
/// user is a stand-in: the renderer draws it only when <see cref="StandIns"/> is set, as the Unit Lab's Art tab does, so the counters in
/// play change only when a batch is accepted (decision F8).
/// </summary>
public sealed partial class FigureArt
{
    public const string ResourceName = "Figures.figures.json";

    /// <summary>The poses a style sheet may name in <c>glyph(size-figures, pose)</c>; <c>prone</c> is the pinned pose's older name.</summary>
    public static readonly IReadOnlyList<string> PoseNames = ["standing", "crew", "leader", "hero", "wounded", "broken", "pinned", "berserk", "prisoner", "vehicle-crew"];

    /// <summary>The weapon kinds a pose may hold.</summary>
    public static readonly IReadOnlyList<string> WeaponNames = ["rifle", "smg"];

    private static readonly Lazy<FigureArt> Embedded = new(() => Read(EmbeddedText()));

    private readonly IReadOnlyDictionary<string, FigurePose> poses;
    private readonly IReadOnlyDictionary<string, FigureKit> helmets;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, FigureKit>> weapons;

    private FigureArt(IReadOnlyDictionary<string, FigurePose> poses, IReadOnlyDictionary<string, FigureKit> helmets,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, FigureKit>> weapons, bool standIns)
    {
        this.poses = poses;
        this.helmets = helmets;
        this.weapons = weapons;
        StandIns = standIns;
    }

    /// <summary>The embedded parts, accepted ones only.</summary>
    public static FigureArt BuiltIn => Embedded.Value;

    /// <summary>No parts: every figure falls back to the drawn one.</summary>
    public static FigureArt None
    {
        get;
    } = new(new Dictionary<string, FigurePose>(), new Dictionary<string, FigureKit>(),
        new Dictionary<string, IReadOnlyDictionary<string, FigureKit>>(), false);

    /// <summary>Whether parts not yet accepted are drawn.</summary>
    public bool StandIns
    {
        get;
    }

    /// <summary>The same parts, with or without the stand-ins.</summary>
    public FigureArt WithStandIns(bool standIns = true) => new(poses, helmets, weapons, standIns);

    public IEnumerable<string> PoseKeys => poses.Keys;

    public IEnumerable<string> HelmetSides => helmets.Keys;

    public IEnumerable<(string Side, string Weapon)> WeaponKeys => weapons.SelectMany(side => side.Value.Keys.Select(weapon => (side.Key, weapon)));

    /// <summary>Every outline the art holds, each once: the poses' bodies, the helmets, and the weapons (the viewport's figure sprite, option C2).</summary>
    internal IEnumerable<string> Outlines => poses.Values.Select(pose => pose.Path).Concat(helmets.Values.Select(helmet => helmet.Path))
        .Concat(weapons.Values.SelectMany(side => side.Values.Select(weapon => weapon.Path))).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// A pose by its name or its older name, when it is drawn under this setting: the side's own version (<c>hero.german</c>, the hero
    /// with the side's grenade) when there is one, otherwise the general pose.
    /// </summary>
    public FigurePose? Pose(string? name, string? side = null)
    {
        var general = PoseName(name);
        return Drawn(side is null ? null : poses.GetValueOrDefault($"{general}.{side}")) ?? Drawn(poses.GetValueOrDefault(general));
    }

    private FigurePose? Drawn(FigurePose? pose) => pose is not null && (pose.Accepted || StandIns) ? pose : null;

    public FigureKit? Helmet(string? side) => side is not null && helmets.GetValueOrDefault(side) is { } helmet && (helmet.Accepted || StandIns) ? helmet : null;

    public FigureKit? WeaponOf(string? side, string? weapon) =>
        side is not null && weapon is not null && weapons.GetValueOrDefault(side)?.GetValueOrDefault(weapon) is { } kit && (kit.Accepted || StandIns) ? kit : null;

    /// <summary>The pose a glyph argument names: none is the standing pose, <c>prone</c> the pinned one.</summary>
    public static string PoseName(string? argument) => argument switch
    {
        null or "" => "standing",
        "prone" => "pinned",
        _ => argument,
    };

    /// <summary>
    /// Whether the drawn fallback of a pose lies prone: the broken pose, and <c>prone</c>, which the style sheets gave the broken face before
    /// the poses had names. Every other pose falls back to the standing figure, as those units were drawn before (decision F8).
    /// </summary>
    public static bool FallsBackProne(string? argument) => argument is "prone" or "broken";

    public static FigureArt Read(string json)
    {
        var file = JsonSerializer.Deserialize<FigureFile>(json, Options) ?? throw new InvalidDataException("The figure file is empty.");
        var poses = (file.Poses ?? []).ToDictionary(pair => pair.Key, pair => Checked(pair.Value), StringComparer.Ordinal);
        var helmets = (file.Helmets ?? []).ToDictionary(pair => pair.Key, pair => Checked(pair.Value), StringComparer.Ordinal);
        var weapons = (file.Weapons ?? []).ToDictionary(pair => pair.Key,
            pair => (IReadOnlyDictionary<string, FigureKit>)pair.Value.ToDictionary(kit => kit.Key, kit => Checked(kit.Value), StringComparer.Ordinal),
            StringComparer.Ordinal);
        return new FigureArt(poses, helmets, weapons, false);
    }

    private static string EmbeddedText()
    {
        using var stream = typeof(FigureArt).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The figure file is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static FigurePose Checked(FigurePose pose) =>
        pose.Width > 0 && pose.Height > 0 && pose.Head.Radius > 0 ? pose with
        {
            Path = CheckedPath(pose.Path)
        } : throw new InvalidDataException("A pose needs a width, a height, and a head.");

    private static FigureKit Checked(FigureKit kit) => kit with { Path = CheckedPath(kit.Path) };

    /// <summary>Path data only: commands, numbers, and separators, so nothing else can reach the SVG.</summary>
    private static string CheckedPath(string path) =>
        PathData().IsMatch(path) ? path : throw new InvalidDataException("A figure path holds something other than path data.");

    /// <summary>A number in a transform, to five places: the parts' coordinates are finer than the 1/64 pixel of positions.</summary>
    internal static string Number(double value)
    {
        var text = Math.Round(value, 5).ToString("0.#####", CultureInfo.InvariantCulture);
        return text == "-0" ? "0" : text;
    }

    [GeneratedRegex("^[MmLlHhVvCcSsQqTtAaZz0-9eE.,+\\- ]*$")]
    private static partial Regex PathData();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed record FigureFile(Dictionary<string, FigurePose>? Poses, Dictionary<string, FigureKit>? Helmets,
        Dictionary<string, Dictionary<string, FigureKit>>? Weapons);
}
