using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// The pinned reference data of the Close Combat package: the CCT transcription (A11.11, p. 692) and the reviewed Scenario
/// A1 catalog, each checked against the digest the case matrix records.
/// </summary>
public sealed class ScenarioA1CloseCombatReference
{
    private readonly (string Label, int Attack, int Defense, bool Below, bool Above, int Kill, int Red)[] columns;

    private ScenarioA1CloseCombatReference((string, int, int, bool, bool, int, int)[] columns, IReadOnlyDictionary<string, FireDefinition> definitions,
        IReadOnlyDictionary<string, int> bpv)
    {
        this.columns = columns;
        Definitions = definitions;
        Bpv = bpv;
        OneToOneColumn = Array.FindIndex(columns, column => column.Item1 == "1-1");
    }

    /// <summary>
    /// Each catalog MMC's Basic Point Value from the A./G. National Capabilities Chart (p. 109), which picks the MMC a leader created in CC is
    /// founded on (A18.2; ruling R14.12).
    /// </summary>
    public IReadOnlyDictionary<string, int> Bpv
    {
        get;
    }

    /// <summary>The red Kill Number of a CCT column, for Hand-to-Hand CC (A11.11, J2.31; ruling R14.1).</summary>
    public int RedKill(int column) => columns[column].Red;

    public IReadOnlyDictionary<string, FireDefinition> Definitions
    {
        get;
    }

    /// <summary>The index of the 1-1 column, from which Leader Creation counts the columns below (A18.2).</summary>
    public int OneToOneColumn
    {
        get;
    }

    /// <summary>
    /// The CCT column of an attack (A11.11): the ratio of the attacking to the defending FP rounded down to the nearest printed
    /// odds, with its label, black Kill Number, and index.
    /// </summary>
    public (string Label, int Kill, int Column) Column(decimal attack, decimal defense)
    {
        var chosen = 0;
        for (var index = 0; index < columns.Length; index++)
        {
            var column = columns[index];
            var ratio = attack * column.Defense;
            var printed = defense * column.Attack;
            // "< 1-8" is the floor; odds are rounded down to a printed ratio, so "> 10-1" is reached at 11 to 1 (ruling R14.13); every other column is
            // reached at its own ratio.
            if (column.Below || (column.Above ? ratio >= defense * (column.Attack + 1) : ratio >= printed))
            {
                chosen = index;
            }
        }

        return (columns[chosen].Label, columns[chosen].Kill, chosen);
    }

    internal static ScenarioA1CloseCombatReference Load(JsonElement matrix)
    {
        using var cct = ScenarioA1FirePackage.Read("ScenarioA1.close-combat-cct.json", matrix.GetProperty("cctTranscriptionSha256").GetString()!);
        using var catalog = ScenarioA1FirePackage.Read("ScenarioA1.fire-catalog.json", matrix.GetProperty("catalogSha256").GetString()!);
        var columns = cct.RootElement.GetProperty("columns").EnumerateArray().Select(item => (
            item.GetProperty("odds").GetString()!,
            item.GetProperty("attack").GetInt32(),
            item.GetProperty("defense").GetInt32(),
            item.TryGetProperty("strictlyBelow", out var below) && below.GetBoolean(),
            item.TryGetProperty("strictlyAbove", out var above) && above.GetBoolean(),
            item.GetProperty("blackKill").GetInt32(),
            item.GetProperty("redKill").GetInt32())).ToArray();
        if (columns.Length != 14 || columns.Select(item => item.Item6).Where((kill, index) => kill != index).Any() || columns.Any(item => item.Item7 != item.Item6 + 2)
            || columns[5].Item1 != "1-1")
        {
            throw new InvalidOperationException("The CCT transcription changed shape.");
        }

        var bpv = matrix.TryGetProperty("bpv", out var values)
            ? values.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetInt32(), StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);
        return new ScenarioA1CloseCombatReference(columns, ScenarioA1FireReference.ReadDefinitions(catalog.RootElement), bpv);
    }
}

/// <summary>
/// Pinned read-only package for Close Combat in the CCPh (unit step 29; Scenario A1 Close Combat Review 2026-09-27): the
/// Ambush drs, and for each declared Infantry attack the odds, the black Kill Number, the DRM, and the effects, with Field
/// Promotion on an Original 2 (A18.12); since backlog pass 11, CC by and against vehicles (R11.12 to R11.17); since backlog pass 14, Hand-to-Hand,
/// concealment, TI, capture, prisoners, Infiltration, and overstacking (R14.1 to R14.13). The manifest records its predecessor, 2dde0d2b (backlog
/// pass 11). It never rolls, attacks, or changes a game.
/// </summary>
public sealed class ScenarioA1CloseCombatPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "7ca1ea7cbaf78d149a753edf9c57578d8c1520d13a392a5046d9175237783f6c";
    public const string MatrixSha256 = "6aad3831098cccea0bb1934e493c33d4abea951b4b6d44008911da28f98b679b";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-close-combat", "sha256:" + ManifestSha256);

    private static readonly string[] Cases =
    [
        "A1-cc-resolved", "A1-cc-ambush-resolved", "A1-cc-phase-outside", "A1-cc-unit-outside", "A1-cc-attack-outside", "A1-cc-round-outside",
        "A1-cc-stacking-outside", "A1-cc-director-outside", "A1-cc-berserk-must-attack", "A1-cc-field-promotion-undecided", "A1-cc-roll-missing", "A1-cc-cx",
        "A1-cc-vehicle-attacked", "A1-cc-vehicle-attacks", "A1-cc-vehicle-sequential", "A1-cc-vehicle-paatc", "A1-cc-vehicle-capture",
        "A1-cc-vehicle-roll-missing", "A1-cc-hand-to-hand", "A1-cc-concealed", "A1-cc-ti", "A1-cc-capture", "A1-cc-capture-outside",
        "A1-cc-capture-choice-undecided", "A1-cc-prisoners-escape", "A1-cc-prisoner-outside", "A1-cc-infiltration", "A1-cc-overstacked",
    ];

    private static readonly string[] PinnedDigests = ["sourcePdfSha256", "cctTranscriptionSha256", "catalogSha256"];

    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1CloseCombatPackage()
    {
        using var manifest = ScenarioA1FirePackage.Read("ScenarioA1.close-combat-package.json", ManifestSha256);
        using var matrix = ScenarioA1FirePackage.Read("ScenarioA1.close-combat-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().Select(item => item.GetProperty("caseId").GetString()).ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-close-combat-package"
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
            throw new InvalidOperationException("The Close Combat admission inputs changed.");
        }

        Reference = ScenarioA1CloseCombatReference.Load(reviewed);
        var rules = reviewed.GetProperty("sourceFragments").EnumerateArray()
            .Select(item => item.GetProperty("ruleId").GetString()!).Distinct(StringComparer.Ordinal);
        descriptor = new DomainPackageDescriptor(Identity, rules
            .Select(rule => new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .Append(new CanonicalReference(Identity, ScenarioA1FirePackage.ChartSupplementId, "A11.11-CCT", "3.01"))
            .ToArray());
    }

    /// <summary>The pinned CCT and catalog values.</summary>
    public ScenarioA1CloseCombatReference Reference
    {
        get;
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved, descriptor, "asl.a1.cc.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable, null, "asl.a1.cc.package-unavailable"));
    }
}
