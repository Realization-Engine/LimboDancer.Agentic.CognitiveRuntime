using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// The pinned reference data of the Ordnance package: the C3 To Hit Table transcription (p. 700), the IFT's Gun caliber headers
/// (C.6), the Ordnance TH# color of each nationality (A25, p. 695), the reviewed Guns of the Scenario A1 catalog, and the Fire
/// package that resolves a hit on the IFT.
/// </summary>
public sealed class ScenarioA1OrdnanceReference
{
    // The A./G. National Capabilities Chart's Ordnance TH# Color (p. 695): German Black, Russian Red.
    private static readonly IReadOnlyDictionary<string, string> Colors = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["german"] = "black",
        ["russian"] = "red",
    };

    private readonly (int Minimum, int? Maximum)[] ranges;
    private readonly IReadOnlyDictionary<string, int[]> infantry;
    private readonly IReadOnlyDictionary<string, int[]> modifications;
    private readonly (int Firepower, int Caliber)[] heColumns;

    private ScenarioA1OrdnanceReference((int, int?)[] ranges, IReadOnlyDictionary<string, int[]> infantry, IReadOnlyDictionary<string, int[]> modifications,
        (int, int)[] heColumns, IReadOnlyDictionary<string, GunDefinition> guns, ScenarioA1FireReference fire)
    {
        this.ranges = ranges;
        this.infantry = infantry;
        this.modifications = modifications;
        this.heColumns = heColumns;
        Guns = guns;
        Fire = fire;
    }

    /// <summary>The reviewed Guns, by definition id.</summary>
    public IReadOnlyDictionary<string, GunDefinition> Guns
    {
        get;
    }

    /// <summary>The Fire package's reference, which resolves a hit on the IFT and holds the crews' and targets' definitions.</summary>
    public ScenarioA1FireReference Fire
    {
        get;
    }

    /// <summary>The Ordnance TH# color of a nationality (C3.3, A25): black or red.</summary>
    public static string Color(string nationality) => Colors.GetValueOrDefault(nationality) ?? "red";

    private int RangeColumn(int range) => Array.FindIndex(ranges, item => range >= item.Minimum && (item.Maximum is null || range <= item.Maximum));

    /// <summary>The Basic TH# of the Infantry Target Type at a range, in a color (C3.3).</summary>
    public int BasicToHit(string color, int range) => infantry[color][RangeColumn(range)];

    /// <summary>The Gun and Ammo modifications of the Basic TH# at a range (C4.1 to C4.2, C4.5), each with its rule.</summary>
    public IReadOnlyList<FireModifier> Modifications(GunDefinition gun, int range)
    {
        ArgumentNullException.ThrowIfNull(gun);
        var column = RangeColumn(range);
        var list = new List<FireModifier>();
        void Add(string row, string name, string rule)
        {
            if (modifications[row][column] is var value && value != 0)
            {
                list.Add(new FireModifier(name, value, rule));
            }
        }

        if (gun.Suffix is { } suffix)
        {
            Add(suffix, "barrel:" + suffix, suffix switch
            {
                "star" => "C4.11",
                "l" => "C4.12",
                _ => "C4.13"
            });
        }

        if (gun.Caliber <= 57)
        {
            Add("atMost57mm", "caliber-57mm", "C4.2");
        }

        if (gun.Caliber <= 40)
        {
            Add("atMost40mm", "caliber-40mm", "C4.2");
        }

        return list;
    }

    /// <summary>The IFT column a Gun's HE uses (C.6): the highest FP whose minimum caliber is at most the Gun's.</summary>
    public int HeFirepower(int caliber) => heColumns.Where(item => item.Caliber <= caliber).Select(item => item.Firepower).DefaultIfEmpty(0).Max();

    internal static ScenarioA1OrdnanceReference Load(JsonElement matrix, ScenarioA1FireReference fire)
    {
        using var table = ScenarioA1FirePackage.Read("ScenarioA1.ordnance-to-hit.json", matrix.GetProperty("toHitTranscriptionSha256").GetString()!);
        using var ift = ScenarioA1FirePackage.Read("ScenarioA1.fire-ift.json", matrix.GetProperty("iftTranscriptionSha256").GetString()!);
        using var catalog = ScenarioA1FirePackage.Read("ScenarioA1.fire-catalog.json", matrix.GetProperty("catalogSha256").GetString()!);
        var root = table.RootElement;
        var ranges = root.GetProperty("ranges").EnumerateArray()
            .Select(item => (item.GetProperty("minimum").GetInt32(), item.GetProperty("maximum").ValueKind == JsonValueKind.Null ? (int?)null : item.GetProperty("maximum").GetInt32()))
            .ToArray();
        static int[] Row(JsonElement element) => [.. element.EnumerateArray().Select(item => item.GetInt32())];
        var infantry = root.GetProperty("infantry").EnumerateObject().ToDictionary(item => item.Name, item => Row(item.Value), StringComparer.Ordinal);
        var modifications = root.GetProperty("modifications").EnumerateObject().ToDictionary(item => item.Name, item => Row(item.Value), StringComparer.Ordinal);
        if (ranges.Length != 10 || infantry.Count != 2 || infantry.Values.Any(row => row.Length != 10) || modifications.Values.Any(row => row.Length != 10))
        {
            throw new InvalidOperationException("The To Hit Table transcription changed shape.");
        }

        // C.6: each IFT column prints its FP and the minimum Gun caliber that uses it, as "FP/mm".
        var heColumns = ift.RootElement.GetProperty("columns").EnumerateArray().Select(item =>
        {
            var header = item.GetProperty("printedHeader").GetString()!.Split('/');
            return (item.GetProperty("fp").GetInt32(), int.Parse(header[1].TrimEnd('+'), System.Globalization.CultureInfo.InvariantCulture));
        }).ToArray();

        var guns = catalog.RootElement.GetProperty("definitions").EnumerateArray().Where(item => item.GetProperty("kind").GetString() == "asl:gun")
            .Select(Gun).OfType<GunDefinition>().ToDictionary(item => item.Id, StringComparer.Ordinal);
        return new ScenarioA1OrdnanceReference(ranges, infantry, modifications, heColumns, guns, fire);
    }

    private static GunDefinition? Gun(JsonElement item)
    {
        JsonElement? Find(string key, string name) => item.GetProperty("values").EnumerateArray()
            .Where(value => value.GetProperty("face").GetString() == "front" && value.TryGetProperty(key, out var found) && found.GetString() == name)
            .Cast<JsonElement?>().FirstOrDefault();
        int? Number(string name) => Find("attribute", name) is { } value && value.TryGetProperty("value", out var number) && number.ValueKind == JsonValueKind.Number
            ? number.GetInt32() : null;
        string? Text(string name) => Find("attribute", name) is { } value && value.TryGetProperty("value", out var text) && text.ValueKind == JsonValueKind.String
            ? text.GetString() : null;
        bool Trait(string name) => Find("trait", name) is { } value && value.TryGetProperty("present", out var present) && present.GetBoolean();

        return Text("gun-type") is { } type && Number("caliber") is { } caliber && Number("breakdown") is { } breakdown
            ? new GunDefinition(item.GetProperty("id").GetString()!, item.GetProperty("nationality").GetString()!, type, caliber, Text("caliber-suffix"),
                Number("rate-of-fire"), breakdown, Number("range-maximum"), Trait("asl:no-he"), Trait("asl:mount-360"))
            : null;
    }
}

/// <summary>
/// Pinned read-only package for a Gun's HE shot at Infantry (unit step 24; Scenario A1 Ordnance Review 2026-09-27): the To
/// Hit DR against the Modified TH#, ROF, breakdown, Acquisition, Critical Hits, and the IFT effects of a hit through the Fire
/// package. It never rolls, attacks, or changes a game.
/// </summary>
public sealed class ScenarioA1OrdnancePackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "7338eb642304b2d5475d6a43c517c70a7b4aca4e0714f94312e22454da6575d0";
    public const string MatrixSha256 = "08f1089142cb89b71a6e4998c7222c0675960fe9201c4e0bb0539b649912b88f";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-ordnance", "sha256:" + ManifestSha256);

    private static readonly string[] Cases =
    [
        "A1-ordnance-hit-resolved", "A1-ordnance-miss-resolved", "A1-ordnance-phase-outside", "A1-ordnance-gun-outside", "A1-ordnance-crew-outside",
        "A1-ordnance-already-fired", "A1-ordnance-range-outside", "A1-ordnance-target-outside", "A1-ordnance-undecided", "A1-ordnance-roll-missing",
    ];

    private static readonly string[] PinnedDigests = ["sourcePdfSha256", "toHitTranscriptionSha256", "iftTranscriptionSha256", "catalogSha256"];

    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1OrdnancePackage()
    {
        using var manifest = ScenarioA1FirePackage.Read("ScenarioA1.ordnance-package.json", ManifestSha256);
        using var matrix = ScenarioA1FirePackage.Read("ScenarioA1.ordnance-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().Select(item => item.GetProperty("caseId").GetString()).ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-ordnance-package"
            || reviewed.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-27"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || reviewed.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || !PinnedDigests.All(name => root.GetProperty(name).GetString() == reviewed.GetProperty(name).GetString())
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"), reviewed.GetProperty("sourceFragments"))
            || !JsonElement.DeepEquals(root.GetProperty("rulings"), reviewed.GetProperty("rulings"))
            || !JsonElement.DeepEquals(root.GetProperty("resolution"), reviewed.GetProperty("resolution"))
            || !Cases.SequenceEqual(cases)
            || !root.GetProperty("exactCaseIds").EnumerateArray().Select(item => item.GetString()).SequenceEqual(cases)
            || !JsonElement.DeepEquals(root.GetProperty("excludedOutcomes"), reviewed.GetProperty("excludedConsequences")))
        {
            throw new InvalidOperationException("The Ordnance admission inputs changed.");
        }

        Reference = ScenarioA1OrdnanceReference.Load(reviewed, new ScenarioA1FirePackage().Reference);
        var rules = reviewed.GetProperty("sourceFragments").EnumerateArray().Select(item => item.GetProperty("ruleId").GetString()!).Distinct(StringComparer.Ordinal);
        descriptor = new DomainPackageDescriptor(Identity, rules
            .Select(rule => new CanonicalReference(Identity, rule.StartsWith('C') ? "asl-easlrb-3.10:chapter-c" : "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .Append(new CanonicalReference(Identity, ScenarioA1FirePackage.ChartSupplementId, "C3-TO-HIT-TABLE", "3.01"))
            .ToArray());
    }

    /// <summary>The pinned To Hit Table, Guns, and Fire package values.</summary>
    public ScenarioA1OrdnanceReference Reference
    {
        get;
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved, descriptor, "asl.a1.ordnance.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable, null, "asl.a1.ordnance.package-unavailable"));
    }
}
