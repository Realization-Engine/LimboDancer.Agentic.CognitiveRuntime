using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Pinned read-only consequence package; never executes a move or MF charge.</summary>
public sealed class ScenarioA1SecondDefenderConsequencePackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "7aa168439781bf889a5c05d797e86054f9e8baf0f396d76ba3f52a5e81125834";
    public const string MatrixSha256 = "50375415804bfb8cc0e1a16c09f80a992d4fdcedca19e41234a2a63715214ec2";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-concealment-second-defender-consequence", "sha256:" + ManifestSha256);

    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15", "B23.4"];
    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1SecondDefenderConsequencePackage()
    {
        using var manifest = Read("ScenarioA1.second-defender-consequence-package.json", ManifestSha256);
        using var matrix = Read("ScenarioA1.second-defender-consequence-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-second-defender-consequence-package"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("priorEligibilityPackageManifestSha256").GetString()
                != ScenarioA1SecondDefenderPackage.ManifestSha256
            || reviewed.GetProperty("priorEligibilityMatrixSha256").GetString()
                != ScenarioA1SecondDefenderPackage.MatrixSha256
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || root.GetProperty("sourcePdfSha256").GetString()
                != reviewed.GetProperty("sourcePdfSha256").GetString()
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"),
                reviewed.GetProperty("sourceFragments"))
            || root.GetProperty("buildingCostSupplement").GetString()
                != reviewed.GetProperty("buildingCostSupplement").GetString()
            || !Rules.SequenceEqual(reviewed.GetProperty("sourceFragments")
                .EnumerateArray().Select(item => item.GetProperty("ruleId").GetString()))
            || cases.Length != 7
            || !root.GetProperty("exactCaseIds").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(cases.Select(item =>
                    item.GetProperty("caseId").GetString()))
            || !root.GetProperty("positiveCaseIds").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(cases.Where(item =>
                    item.GetProperty("reviewStatus").GetString() == "reviewed-bounded")
                    .Select(item => item.GetProperty("caseId").GetString()))
            || root.GetProperty("positiveCaseIds").GetArrayLength() != 2
            || !JsonElement.DeepEquals(root.GetProperty("excludedOutcomes"),
                reviewed.GetProperty("excludedConsequences")))
            throw new InvalidOperationException("The second-defender consequence admission inputs changed.");

        descriptor = new DomainPackageDescriptor(Identity, Rules.Select(rule =>
            new CanonicalReference(Identity, rule == "B23.4"
                ? "asl-easlrb-3.10:chapter-b" : "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .ToArray());
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                descriptor, "asl.a1.second-defender-consequence.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.second-defender-consequence.package-unavailable"));
    }

    internal static JsonDocument Read(string name, string digest)
    {
        using var stream = typeof(ScenarioA1SecondDefenderConsequencePackage).Assembly
            .GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("A second-defender consequence artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
            throw new InvalidOperationException("A second-defender consequence artifact changed: " + name);
        return JsonDocument.Parse(bytes);
    }
}
