using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1VerificationBatchTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    [Fact]
    public void AttestedSourceSubjectsBindToFullTirWithoutAcceptingSemantics()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var inventory = AslScenarioA1SourceInventory.Extract(manifests);
        var batch = AslScenarioA1VerificationBatchBuilder.Build(manifests, ReadAttestation());

        Assert.Equal(11, batch.Records.Count);
        Assert.All(batch.Records, record =>
        {
            Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition);
            Assert.Equal(TirReviewActorRole.SourceVerifier, record.Actor.Role);
            Assert.Contains(inventory.PdfSha256, record.ComparisonMethod, StringComparison.Ordinal);
            Assert.Equal(TirCanonicalJson.ComputePayloadSha256(batch.SourceDocument),
                record.Subject.TirDocumentSha256);
            Assert.NotEmpty(TirReviewCanonicalJson.Serialize(record));
            var artifact = Assert.Single(batch.SourceDocument.Artifacts, candidate =>
                candidate.Envelope.ArtifactId == record.Subject.ArtifactId);
            Assert.Equal(TirFormalizationStatus.Unmodeled, artifact.Envelope.FormalizationStatus);
        });
        var pageCorrection = Assert.Single(batch.Records, record =>
            record.SourceFragment.FragmentId == Assert.Single(
                inventory.Rules, rule => rule.NormalizedRuleId == "A4.14").Fragments[0].FragmentId);
        Assert.Equal(48, pageCorrection.SourceEvidence.StartPage);
        Assert.Contains("physical PDF page 49", pageCorrection.ComparisonMethod, StringComparison.Ordinal);
        var footnote = Assert.Single(batch.Records, record =>
            record.SourceFragment.FragmentId == Assert.Single(inventory.LinkedFootnotes).FragmentId);
        Assert.Equal(98, footnote.SourceEvidence.StartPage);
        Assert.Contains("physical PDF page 101", footnote.ComparisonMethod, StringComparison.Ordinal);
        var breach = Assert.Single(batch.Records, record =>
            record.SourceFragment.FragmentId == Assert.Single(
                inventory.Rules, rule => rule.NormalizedRuleId == "B23.9221")
                .Fragments.Single(fragment => fragment.Kind == SourceFragmentKind.FigureReference).FragmentId);
        Assert.Contains(breach.Dependencies, dependency =>
            dependency.Kind == TirDependencyKind.Figure
            && dependency.Target.EndsWith("eASLRB_v3_01-p141-1.png", StringComparison.Ordinal));
        using var serialized = JsonDocument.Parse(batch.Serialize());
        Assert.Equal(11, serialized.RootElement.GetProperty("records").GetArrayLength());
        Assert.Equal("source-fidelity-verified-semantic-review-pending",
            serialized.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void IncompleteOrWrongPdfAttestationCannotCreateVerificationRecords()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var inventory = AslScenarioA1SourceInventory.Extract(manifests);
        var attestation = ReadAttestation();
        Assert.Equal(inventory.PdfSha256, attestation.PdfSha256);

        Assert.Throws<InvalidOperationException>(() => AslScenarioA1VerificationBatchBuilder.Build(
            manifests, attestation with { PdfSha256 = new string('0', 64) }));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1VerificationBatchBuilder.Build(
            manifests, attestation with { ApprovedFragmentIds = attestation.ApprovedFragmentIds.Skip(1).ToArray() }));
    }

    private static AslScenarioA1SourceAttestation ReadAttestation()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.source-attestation.json");
        return JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Source attestation could not be read.");
    }
}
