using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Pinned read-only package for an elected Infantry OVR after a lone concealed SMC reveal (unit step 10; Scenario A1 OVR
/// NTC Review 2026-09-26): the second reveal on election, the OVR NTC, and a failed NTC. It never executes a move,
/// a roll, or an MF charge.
/// </summary>
public sealed class ScenarioA1OvrNtcPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "e8a35e01929f4bc44fb082b915200920d46a7d262d04c0b157f55580f47bde13";
    public const string MatrixSha256 = "cb6c494ea5972b16cb4191771a5010c525c7fa4b249c48b186e6b585f8041eef";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"), "scenario-a1-concealment-ovr-ntc", "sha256:" + ManifestSha256);

    private static readonly string[] Rules = ["A.9", "A4.15", "A10.1", "A12.15", "B23.3", "NTC-glossary"];
    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1OvrNtcPackage()
    {
        using var manifest = Read("ScenarioA1.ovr-ntc-package.json", ManifestSha256);
        using var matrix = Read("ScenarioA1.ovr-ntc-matrix.json", MatrixSha256);
        var root = manifest.RootElement;
        var reviewed = matrix.RootElement;
        var cases = reviewed.GetProperty("cases").EnumerateArray().ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-ovr-ntc-package"
            || reviewed.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-26"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || reviewed.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("priorConcealedSmcOverrunPackageManifestSha256").GetString() != ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256
            || reviewed.GetProperty("priorConcealedSmcOverrunPackageManifestSha256").GetString() != ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256
            || root.GetProperty("priorPostRevealPackageManifestSha256").GetString() != ScenarioA1PostRevealPackage.ManifestSha256
            || reviewed.GetProperty("priorPostRevealPackageManifestSha256").GetString() != ScenarioA1PostRevealPackage.ManifestSha256
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || root.GetProperty("sourcePdfSha256").GetString() != reviewed.GetProperty("sourcePdfSha256").GetString()
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"), reviewed.GetProperty("sourceFragments"))
            || !JsonElement.DeepEquals(root.GetProperty("rulings"), reviewed.GetProperty("rulings"))
            || !JsonElement.DeepEquals(root.GetProperty("ntcResolution"), reviewed.GetProperty("ntcResolution"))
            || !Rules.SequenceEqual(reviewed.GetProperty("sourceFragments").EnumerateArray().Select(item => item.GetProperty("ruleId").GetString()))
            || cases.Length != 5
            || !root.GetProperty("exactCaseIds").EnumerateArray().Select(item => item.GetString())
                .SequenceEqual(cases.Select(item => item.GetProperty("caseId").GetString()))
            || !root.GetProperty("positiveCaseIds").EnumerateArray().Select(item => item.GetString())
                .SequenceEqual(cases.Where(item => item.GetProperty("reviewStatus").GetString() == "reviewed-bounded")
                    .Select(item => item.GetProperty("caseId").GetString()))
            || root.GetProperty("positiveCaseIds").GetArrayLength() != 3
            || !JsonElement.DeepEquals(root.GetProperty("excludedOutcomes"), reviewed.GetProperty("excludedConsequences")))
        {
            throw new InvalidOperationException("The OVR NTC admission inputs changed.");
        }

        descriptor = new DomainPackageDescriptor(Identity, Rules.Select(rule => new CanonicalReference(Identity, rule switch
        {
            "B23.3" => "asl-easlrb-3.10:chapter-b",
            "NTC-glossary" => "asl-easlrb-3.10:index-glossary",
            _ => "asl-easlrb-3.10:chapter-a",
        }, rule == "NTC-glossary" ? "NTC" : rule, "3.01")).ToArray());
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved, descriptor, "asl.a1.ovr-ntc.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable, null, "asl.a1.ovr-ntc.package-unavailable"));
    }

    internal static JsonDocument Read(string name, string digest)
    {
        using var stream = typeof(ScenarioA1OvrNtcPackage).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("An OVR NTC artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
        {
            throw new InvalidOperationException("An OVR NTC artifact changed: " + name);
        }

        return JsonDocument.Parse(bytes);
    }
}
