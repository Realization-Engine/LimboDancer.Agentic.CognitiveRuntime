using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Immutable A12.15 second-reveal eligibility package; never executes a board change.</summary>
public sealed class ScenarioA1SecondDefenderPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "bd74b9d3757cba5dcd7d9690050514cc853573f73a11f14e56e299781084ca04";
    public const string MatrixSha256 = "7db79dc8a634712983b5a14c5b4353b6a57038c89c822012c614e99105e34e6c";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-concealment-second-defender-reveal", "sha256:" + ManifestSha256);

    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15"];
    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1SecondDefenderPackage()
    {
        using var manifest = Read("ScenarioA1.second-defender-package.json", ManifestSha256);
        using var matrix = Read("ScenarioA1.second-defender-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-second-defender-eligibility-package"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("priorPackageManifestSha256").GetString()
                != ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256
            || reviewed.GetProperty("priorPackageManifestSha256").GetString()
                != ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || root.GetProperty("sourcePdfSha256").GetString()
                != reviewed.GetProperty("sourcePdfSha256").GetString()
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"),
                reviewed.GetProperty("sourceFragments"))
            || !Rules.SequenceEqual(reviewed.GetProperty("sourceRules")
                .EnumerateArray().Select(item => item.GetString()))
            || cases.Length != 7
            || !root.GetProperty("exactCaseIds").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(cases.Select(item =>
                    item.GetProperty("caseId").GetString()))
            || !root.GetProperty("positiveCaseIds").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(cases.Where(item =>
                    item.GetProperty("reviewStatus").GetString() == "reviewed-bounded")
                    .Select(item => item.GetProperty("caseId").GetString()))
            || root.GetProperty("positiveCaseIds").GetArrayLength() != 2
            || root.GetProperty("excludedOutcomes").GetArrayLength() != 5)
            throw new InvalidOperationException("The second-defender package admission inputs changed.");

        descriptor = new DomainPackageDescriptor(Identity, Rules.Select(rule =>
            new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-a", rule, "3.01"))
            .ToArray());
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                descriptor, "asl.a1.second-defender.exact-eligibility-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.second-defender.package-unavailable"));
    }

    internal static JsonDocument Read(string name, string digest)
    {
        using var stream = typeof(ScenarioA1SecondDefenderPackage).Assembly
            .GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("A second-defender artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
            throw new InvalidOperationException("A second-defender artifact changed: " + name);
        return JsonDocument.Parse(bytes);
    }
}
