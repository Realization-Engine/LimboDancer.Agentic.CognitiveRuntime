using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Immutable A12.15-origin Infantry OVR package; no game-state execution.</summary>
public sealed class ScenarioA1ConcealedSmcOverrunPackage : IDomainPackageResolver
{
    public const string ManifestSha256 = "052579ea6dcfb2da3221b39c524695a74637645ddd853d9a343c3d2db9b8d553";
    public const string MatrixSha256 = "f64f4749fed87ee9517130ba747ebec4aafee618bd86ab15c4984c6854c09afa";
    public const string TransitionSha256 = "d1db66dbc66b00272cf458b341f5789512d4900c405024292c676324dcaf32f0";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-concealment-infantry-overrun", "sha256:" + ManifestSha256);

    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15", "A4.151", "A4.152", "B23.4"];
    private readonly DomainPackageDescriptor descriptor;

    public ScenarioA1ConcealedSmcOverrunPackage()
    {
        using var manifest = Read("ScenarioA1.concealed-smc-overrun-package.json", ManifestSha256);
        using var matrix = Read("ScenarioA1.concealed-smc-overrun-matrix.json", MatrixSha256);
        using var transition = Read("ScenarioA1.concealed-smc-overrun-transition.json", TransitionSha256);
        var root = manifest.RootElement;
        var cases = matrix.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        if (root.GetProperty("status").GetString() != "published-bounded-concealed-smc-overrun-package"
            || root.GetProperty("domainId").GetString() != "asl"
            || root.GetProperty("packageId").GetString() != Identity.PackageId
            || root.GetProperty("executionAuthority").GetString() != "none"
            || root.GetProperty("priorOccupiedPackageManifestSha256").GetString()
                != ScenarioA1OccupiedPackage.ManifestSha256
            || root.GetProperty("priorPostRevealPackageManifestSha256").GetString()
                != ScenarioA1PostRevealPackage.ManifestSha256
            || root.GetProperty("transitionReviewSha256").GetString() != TransitionSha256
            || root.GetProperty("caseMatrixSha256").GetString() != MatrixSha256
            || matrix.RootElement.GetProperty("transitionReviewSha256").GetString() != TransitionSha256
            || root.GetProperty("sourcePdfSha256").GetString()
                != transition.RootElement.GetProperty("sourcePdfSha256").GetString()
            || root.GetProperty("buildingCostSupplement").GetString()
                != transition.RootElement.GetProperty("buildingCostSupplement").GetString()
            || root.GetProperty("qualifiedCaseId").GetString()
                != "A1-concealed-smc-qualified-response-unresolved"
            || root.GetProperty("delegatedCaseId").GetString() != "A1-concealed-smc-declined"
            || cases.Length != 10
            || !root.GetProperty("exactCaseIds").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(cases.Select(item =>
                    item.GetProperty("caseId").GetString()))
            || !Rules.SequenceEqual(matrix.RootElement.GetProperty("sourceRules").EnumerateArray()
                .Select(item => item.GetString()))
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"),
                transition.RootElement.GetProperty("sourceFragments"))
            || !JsonElement.DeepEquals(root.GetProperty("sourceFragments"),
                matrix.RootElement.GetProperty("sourceFragments")))
            throw new InvalidOperationException("The concealed-SMC OVR admission inputs changed.");

        descriptor = new DomainPackageDescriptor(Identity, Rules.Select(rule => new CanonicalReference(
            Identity, rule == "B23.4" ? "asl-easlrb-3.10:chapter-b" : "asl-easlrb-3.10:chapter-a",
            rule, "3.01")).ToArray());
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                descriptor, "asl.a1.concealed-smc-overrun.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.concealed-smc-overrun.package-unavailable"));
    }

    internal static JsonDocument Read(string name, string digest)
    {
        using var stream = typeof(ScenarioA1ConcealedSmcOverrunPackage).Assembly
            .GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("A concealed-SMC OVR artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
            throw new InvalidOperationException("A concealed-SMC OVR artifact changed: " + name);
        return JsonDocument.Parse(bytes);
    }
}
