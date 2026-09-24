using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Additional immutable exact-case package; the earlier occupied package remains intact.</summary>
public sealed class ScenarioA1PostRevealPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "0d721e581a0201d79cd2ab18c06032c2875c2910c848796dad62052db9b52c0b";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-concealment-post-reveal", "sha256:" + ManifestSha256);
    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1PostRevealPackage()
    {
        using var stream = typeof(ScenarioA1PostRevealPackage).Assembly
            .GetManifestResourceStream("ScenarioA1.post-reveal-package.json")
            ?? throw new InvalidOperationException("The post-reveal manifest is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != ManifestSha256)
            throw new InvalidOperationException("The post-reveal manifest changed.");
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        var cases = root.GetProperty("acceptedCaseIds").EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        var fragments = root.GetProperty("sourceFragments").EnumerateArray().ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-post-reveal-package"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("priorPackageManifestSha256").GetString() !=
                ScenarioA1OccupiedPackage.ManifestSha256
            || root.GetProperty("sourcePdfSha256").GetString() !=
                "957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247"
            || cases.Length != 2
            || cases[0] != "A1-post-reveal-nondummy-forced-back"
            || cases[1] != "A1-post-reveal-dummies-only-continue"
            || root.GetProperty("nonDefinitiveCaseId").GetString() !=
                "A1-concealed-occupancy-attempt"
            || root.GetProperty("exclusions").GetArrayLength() != 6
            || fragments.Length != 2
            || !Fragment(fragments[0], "A12.15", 78,
                "asl-fragment:sha256:e92332292b4bde444aff7f84127793084e1f0ecfd99af9750da6e4aa8af40ea3",
                "ce0cc02826bf66951ff4a1ce06b7a51d105eb2f2540f3710c2e49c2455306fcf")
            || !Fragment(fragments[1], "A4.14", 49,
                "asl-fragment:sha256:d22c4de11eefffac89c9585633ca3f4eeff313cc2d1ddd535975e1e8c23e0e69",
                "65adb630eb862eb54b49feb1e414bdf9b0c5132f8d0c55e6eaa94eb5b44c6995"))
            throw new InvalidOperationException("The post-reveal admission inputs changed.");
        descriptor = new DomainPackageDescriptor(Identity,
        [
            new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-a", "A12.15", "3.01"),
            new CanonicalReference(Identity, "asl-easlrb-3.10:chapter-a", "A4.14", "3.01"),
        ]);
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                descriptor, "asl.a1.post-reveal.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.post-reveal.package-unavailable"));
    }

    private static bool Fragment(JsonElement value, string rule, int page, string id,
        string digest) => value.GetProperty("ruleId").GetString() == rule
            && value.GetProperty("physicalPdfPage").GetInt32() == page
            && value.GetProperty("fragmentId").GetString() == id
            && value.GetProperty("contentSha256").GetString() == digest;
}
