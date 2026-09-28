using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Pinned read-only package for Infantry fire (unit step 17, revised at steps 18 and 19 to 23; Scenario A1 Fire Review
/// 2026-09-26): PFPh, AFPh, DFPh, and MPh fire by fire groups in one Location or across ADJACENT Locations, with MGs, at
/// known, concealed, hidden, and Dummy targets; the FP columns, the DRM, the IFT results, each target unit's effect, each
/// MG's malfunction and Multiple ROF, and Residual FP. It never rolls, fires, or changes a game.
/// </summary>
public sealed class ScenarioA1FirePackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "e00107a7f92c118f4432d4248b4c20ee1c4ad252d142fa90f54911884c17ba07";
    public const string MatrixSha256 = "c34229b7b10ac61133bc5f5dd0686d4fb4e99f9c41cd0792504aaed5729bb392";
    public const string ChartSupplementId = "asl-supplement:fire-charts";

    /// <summary>
    /// The package as revised at backlog pass 6 (catalog 1.5.0), before the backlog pass 7 revision (closed-topped AFVs, catalog 1.6.0); each
    /// revision's manifest records its predecessor. Earlier manifests: unit steps 27 and 28, 8ac91d104a02c40f5237b4bfd924c1dde1f106aa3e7e1d5aab31725eae6499f9;
    /// unit step 18, e0c28e88cda17bd71efd63f569c713d4d921e958dc7bc848b3c4c4acead143e1; and unit step 17,
    /// 45011b56946be8a7aba1676e6aea8d5ad0fa82193e4ebac24e06fd36bdff2b69 (Fire Review, "Revision at unit step 18").
    /// </summary>
    public const string PriorManifestSha256 = "a30379390feaf8e54e02892936a2a9dfd11db1ade315344036781ae4bf12ad4e";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-fire", "sha256:" + ManifestSha256);

    private static readonly string[] Cases =
    [
        "A1-fire-resolved", "A1-fire-phase-outside", "A1-fire-firer-outside", "A1-fire-target-outside", "A1-fire-range-or-los-denied", "A1-fire-weapon-outside", "A1-fire-movement-fire-outside", "A1-fire-movement-drm-differs", "A1-fire-levels-differ", "A1-fire-hindrance-unattributed", "A1-fire-elr-undecided", "A1-fire-leaders-interact", "A1-fire-heat-of-battle-undecided", "A1-fire-roll-missing", "A1-fire-vehicle-line-resolved", "A1-fire-vehicle-collateral-resolved", "A1-fire-vehicle-fire-resolved", "A1-fire-vehicle-outside", "A1-fire-cx", "A1-fire-second-heat-of-battle", "A1-fire-owner-options", "A1-fire-no-quarter", "A1-fire-afv-cover", "A1-fire-residual-vehicle", "A1-fire-concealed-vehicle", "A1-fire-bounding-first-fire", "A1-fire-closed-topped-afv",
    ];

    private static readonly string[] PinnedDigests =
    [
        "sourcePdfSha256", "chartSupplementSha256", "chartReviewDecisionSha256", "iftTranscriptionSha256",
        "terrainChartTranscriptionSha256", "catalogSha256", "vehicleLineTranscriptionSha256",
    ];

    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1FirePackage()
    {
        using var manifest = Read("ScenarioA1.fire-package.json", ManifestSha256);
        using var matrix = Read("ScenarioA1.fire-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().Select(item => item.GetProperty("caseId").GetString()).ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-fire-package"
            || reviewed.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-26"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || reviewed.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || root.GetProperty("priorFirePackageManifestSha256").GetString() != PriorManifestSha256
            || !PinnedDigests.All(name => root.GetProperty(name).GetString() == reviewed.GetProperty(name).GetString())
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"), reviewed.GetProperty("sourceFragments"))
            || !JsonElement.DeepEquals(root.GetProperty("visualReadings"), reviewed.GetProperty("visualReadings"))
            || !JsonElement.DeepEquals(root.GetProperty("rulings"), reviewed.GetProperty("rulings"))
            || !JsonElement.DeepEquals(root.GetProperty("resolution"), reviewed.GetProperty("resolution"))
            || !Cases.SequenceEqual(cases)
            || !root.GetProperty("exactCaseIds").EnumerateArray().Select(item => item.GetString()).SequenceEqual(cases)
            || !JsonElement.DeepEquals(root.GetProperty("excludedOutcomes"), reviewed.GetProperty("excludedConsequences")))
        {
            throw new InvalidOperationException("The Fire admission inputs changed.");
        }

        Reference = ScenarioA1FireReference.Load(reviewed);
        var rules = reviewed.GetProperty("sourceFragments").EnumerateArray()
            .Select(item => item.GetProperty("ruleId").GetString()!).Distinct(StringComparer.Ordinal);
        descriptor = new DomainPackageDescriptor(Identity, rules
            .Select(rule => new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-" + char.ToLowerInvariant(rule[0]), rule, "3.01"))
            .Append(new CanonicalReference(Identity, ChartSupplementId, "A7-IFT", "3.01"))
            .Append(new CanonicalReference(Identity, ChartSupplementId, "A7-IFT-VEHICLE-LINE", "3.01"))
            .Append(new CanonicalReference(Identity, ChartSupplementId, "B-Terrain-Chart-TEM", "3.01"))
            .ToArray());
    }

    /// <summary>The pinned IFT, TEM, and catalog values.</summary>
    public ScenarioA1FireReference Reference
    {
        get;
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved, descriptor, "asl.a1.fire.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable, null, "asl.a1.fire.package-unavailable"));
    }

    internal static JsonDocument Read(string name, string digest)
    {
        using var stream = typeof(ScenarioA1FirePackage).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("A Fire artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
        {
            throw new InvalidOperationException("A Fire artifact changed: " + name);
        }

        return JsonDocument.Parse(bytes);
    }
}
