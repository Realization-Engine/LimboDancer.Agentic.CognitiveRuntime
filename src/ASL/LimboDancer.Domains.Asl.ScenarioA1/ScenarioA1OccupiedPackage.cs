using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Immutable publication of the seven admitted exact contracts and two unresolved cases.</summary>
public sealed class ScenarioA1OccupiedPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "2ab3bde60ecc9855f2e35197e1921cfcfee3e8da3cd168a9cceef66ada6be6e9";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-occupied-exact", "sha256:" + ManifestSha256);
    private readonly DomainPackageDescriptor _descriptor;

    public ScenarioA1OccupiedPackage()
    {
        ScenarioA1ConformanceAdmission.Validate();
        var candidate = new ScenarioA1SemanticCandidate();
        ScenarioA1SemanticAcceptance.Validate(candidate);
        using var stream = typeof(ScenarioA1OccupiedPackage).Assembly
            .GetManifestResourceStream("ScenarioA1.occupied-package.json")
            ?? throw new InvalidOperationException("The published package manifest is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != ManifestSha256)
            throw new InvalidOperationException("The package manifest changed.");
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0.0"
            || root.GetProperty("status").GetString() != "published-bounded-exact-case-package"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("declaredUse").GetString() != ScenarioA1SemanticAcceptance.DeclaredUse
            || root.GetProperty("semanticAcceptanceSha256").GetString() != ScenarioA1SemanticAcceptance.Sha256
            || root.GetProperty("candidateManifestRootSha256").GetString() != candidate.RootSha256
            || root.GetProperty("boundedAdmissionSha256").GetString() != ScenarioA1BoundedAdmission.Sha256
            || root.GetProperty("sourceComparisonSha256").GetString() !=
                "c64fe3229d5a3541357fe6948ec037fcfbdbfde8810a599f21df81aed0a94bc5"
            || root.GetProperty("sourcePdfSha256").GetString() !=
                "957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247"
            || root.GetProperty("verifiedSourceSubjectCount").GetInt32() != 28
            || !Ids(root, "acceptedCaseIds").ToHashSet(StringComparer.Ordinal)
                .SetEquals(candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded")
                    .Select(item => item.Id))
            || !Ids(root, "nonDefinitiveCaseIds").ToHashSet(StringComparer.Ordinal)
                .SetEquals(candidate.Cases.Where(item => item.ReviewStatus == "reviewed-nondefinitive")
                    .Select(item => item.Id)))
        {
            throw new InvalidOperationException("The package manifest does not bind the admitted use.");
        }

        var sources = candidate.Cases.SelectMany(item => item.SourceRules)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(id => new CanonicalReference(Identity,
                id.StartsWith('B') ? "asl-easlrb-3.10:chapter-b" : "asl-easlrb-3.10:chapter-a",
                id, "3.01"));
        _descriptor = new DomainPackageDescriptor(Identity, sources);
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                _descriptor, "asl.a1.occupied.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.occupied.package-unavailable"));
    }

    private static string[] Ids(JsonElement root, string name) => root.GetProperty(name)
        .EnumerateArray().Select(item => item.GetString()!).ToArray();
}
