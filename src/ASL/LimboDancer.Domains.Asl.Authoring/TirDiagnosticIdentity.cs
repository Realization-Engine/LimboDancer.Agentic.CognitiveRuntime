namespace LimboDancer.Domains.Asl.Authoring;

public static class TirDiagnosticIdentity
{
    public static string Create(
        TirReviewSubjectReference subject,
        TirDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(diagnostic);

        var material = string.Join(
            "\n",
            [
                "asl-tir-extracted-diagnostic/v1",
                subject.TirDocumentSha256,
                subject.ArtifactId,
                subject.ArtifactSha256,
                diagnostic.Code,
                Severity(diagnostic.Severity),
                diagnostic.ArtifactId ?? string.Empty,
                diagnostic.Message,
            ]);
        return $"asl-tir-diagnostic:sha256:{Hashing.Sha256Text(material)}";
    }

    private static string Severity(TirDiagnosticSeverity severity) => severity switch
    {
        TirDiagnosticSeverity.Information => "information",
        TirDiagnosticSeverity.Warning => "warning",
        TirDiagnosticSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };
}
