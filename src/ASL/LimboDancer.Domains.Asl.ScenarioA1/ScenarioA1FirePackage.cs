using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Pinned read-only package for PFPh and DFPh fire by a single-Location fire group (unit step 17; Scenario A1 Fire
/// Review 2026-09-26): the FP column, the DRM, the IFT result, and each target unit's effect. It never rolls, fires, or
/// changes a game.
/// </summary>
public sealed class ScenarioA1FirePackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "45011b56946be8a7aba1676e6aea8d5ad0fa82193e4ebac24e06fd36bdff2b69";
    public const string MatrixSha256 = "82d85ac93e01c81f5888209e6f2d4d5b1f8de6d4f86545d345b5fcbc76af743c";
    public const string ChartSupplementId = "asl-supplement:fire-charts";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-fire", "sha256:" + ManifestSha256);

    private static readonly string[] Cases =
    [
        "A1-fire-resolved", "A1-fire-phase-outside", "A1-fire-firer-outside", "A1-fire-target-outside", "A1-fire-range-or-los-denied",
        "A1-fire-levels-differ", "A1-fire-hindrance-unattributed", "A1-fire-concealment-unreviewed", "A1-fire-elr-undecided",
        "A1-fire-leader-wounded", "A1-fire-reduction-counter-missing", "A1-fire-leaders-interact", "A1-fire-roll-missing",
    ];

    private static readonly string[] PinnedDigests =
    [
        "sourcePdfSha256", "chartSupplementSha256", "chartReviewDecisionSha256", "iftTranscriptionSha256",
        "terrainChartTranscriptionSha256", "catalogSha256",
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
            .Select(rule => new CanonicalReference(Identity,
                rule.StartsWith('B') ? "asl-easlrb-3.10:chapter-b" : "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .Append(new CanonicalReference(Identity, ChartSupplementId, "A7-IFT", "3.01"))
            .Append(new CanonicalReference(Identity, ChartSupplementId, "B-Terrain-Chart-TEM", "3.01"))
            .ToArray());
    }

    /// <summary>The pinned IFT, TEM, and catalog values.</summary>
    public ScenarioA1FireReference Reference { get; }

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
