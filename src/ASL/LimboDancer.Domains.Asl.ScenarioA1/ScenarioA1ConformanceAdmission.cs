using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Admission gate for the published exact-case ASL-OT-04 slice.</summary>
public static class ScenarioA1ConformanceAdmission
{
    public const string Sha256 = "7a8cc062e3d60b7289e30a50981120e56544c1fc3555eb82cc6731473b9b14c8";

    public static void Validate()
    {
        using var stream = typeof(ScenarioA1ConformanceAdmission).Assembly
            .GetManifestResourceStream("ScenarioA1.conformance-admission.json")
            ?? throw new InvalidOperationException("The conformance admission record is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)) != Sha256)
            throw new InvalidOperationException("The conformance admission record changed.");
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0.0"
            || root.GetProperty("status").GetString() != "admitted-bounded-asl-ot-04-exact-case-package"
            || root.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-24"
            || root.GetProperty("declaredUse").GetString() != ScenarioA1SemanticAcceptance.DeclaredUse
            || root.GetProperty("packageManifestSha256").GetString() != ScenarioA1OccupiedPackage.ManifestSha256
            || root.GetProperty("semanticAcceptanceSha256").GetString() != ScenarioA1SemanticAcceptance.Sha256
            || root.GetProperty("boundedAdmissionSha256").GetString() != ScenarioA1BoundedAdmission.Sha256
            || root.GetProperty("candidateManifestRootSha256").GetString() !=
                new ScenarioA1SemanticCandidate().RootSha256
            || root.GetProperty("verifiedSourceSubjectCount").GetInt32() != 28
            || root.GetProperty("admittedExactCaseCount").GetInt32() != 7
            || root.GetProperty("nonDefinitiveCaseCount").GetInt32() != 2
            || root.GetProperty("requiredConformance").GetArrayLength() != 7)
            throw new InvalidOperationException("The ASL-OT-04 bounded conformance gate did not reproduce.");
    }
}
