using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>The pinned reference data of the Rally package: the reviewed Scenario A1 catalog, checked against its digest.</summary>
public sealed class ScenarioA1RallyReference
{
    private ScenarioA1RallyReference(IReadOnlyDictionary<string, FireDefinition> definitions) => Definitions = definitions;

    public IReadOnlyDictionary<string, FireDefinition> Definitions
    {
        get;
    }

    internal static ScenarioA1RallyReference Load(JsonElement matrix)
    {
        using var catalog = ScenarioA1FirePackage.Read("ScenarioA1.fire-catalog.json", matrix.GetProperty("catalogSha256").GetString()!);
        return new ScenarioA1RallyReference(ScenarioA1FireReference.ReadDefinitions(catalog.RootElement));
    }
}

/// <summary>
/// Pinned read-only package for Rally in the RPh (unit step 19; Scenario A1 Rally Review 2026-09-26): the Rally DR and its
/// DRM, Fate, and the unit's effect. It never rolls, rallies, or changes a game.
/// </summary>
public sealed class ScenarioA1RallyPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "f18137cb4193e08c59c85c8015585bcca7d0cd5407e8b0a377a6190734e9fedb";
    public const string MatrixSha256 = "7d18da6b1e493e8c5f9458ed99a268b7e18d795d786bb8c92901148acfc2e268";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-rally", "sha256:" + ManifestSha256);

    private static readonly string[] Cases =
    [
        "A1-rally-resolved", "A1-rally-phase-outside", "A1-rally-unit-outside", "A1-rally-already-attempted", "A1-rally-leader-outside", "A1-rally-self-rally-refused", "A1-rally-terrain-outside", "A1-rally-capability-unrecorded", "A1-rally-reduction-counter-missing", "A1-rally-field-promotion-unreviewed", "A1-rally-roll-missing", "A1-rally-owner-options", "A1-rally-no-quarter",
    ];

    private static readonly string[] PinnedDigests = ["sourcePdfSha256", "catalogSha256"];

    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1RallyPackage()
    {
        using var manifest = ScenarioA1FirePackage.Read("ScenarioA1.rally-package.json", ManifestSha256);
        using var matrix = ScenarioA1FirePackage.Read("ScenarioA1.rally-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().Select(item => item.GetProperty("caseId").GetString()).ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-rally-package"
            || reviewed.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-26"
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
            throw new InvalidOperationException("The Rally admission inputs changed.");
        }

        Reference = ScenarioA1RallyReference.Load(reviewed);
        var rules = reviewed.GetProperty("sourceFragments").EnumerateArray()
            .Select(item => item.GetProperty("ruleId").GetString()!).Distinct(StringComparer.Ordinal);
        descriptor = new DomainPackageDescriptor(Identity, rules
            .Select(rule => new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .ToArray());
    }

    /// <summary>The pinned catalog values.</summary>
    public ScenarioA1RallyReference Reference
    {
        get;
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved, descriptor, "asl.a1.rally.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable, null, "asl.a1.rally.package-unavailable"));
    }
}
