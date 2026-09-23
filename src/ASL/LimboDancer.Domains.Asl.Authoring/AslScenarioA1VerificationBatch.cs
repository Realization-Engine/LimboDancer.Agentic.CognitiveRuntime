using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record AslScenarioA1SourceAttestation(
    string PdfSha256,
    string ActorIdentity,
    DateTimeOffset AttestedAt,
    string AttestationSource,
    IReadOnlyList<string> ApprovedFragmentIds);

public sealed record AslScenarioA1VerificationBatch(
    TirDocument SourceDocument,
    IReadOnlyList<TirSourceVerificationRecord> Records)
{
    public string Serialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = true,
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", "1.0.0");
            writer.WriteString("status", "source-fidelity-verified-semantic-review-pending");
            writer.WriteString("sourceDocumentSha256", TirCanonicalJson.ComputePayloadSha256(SourceDocument));
            writer.WritePropertyName("records");
            writer.WriteStartArray();
            foreach (var record in Records)
            {
                writer.WriteRawValue(TirReviewCanonicalJson.Serialize(record));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }
}

public static class AslScenarioA1VerificationBatchBuilder
{
    public static AslScenarioA1VerificationBatch Build(
        GeneratedManifests manifests,
        AslScenarioA1SourceAttestation attestation)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(attestation);
        ArgumentException.ThrowIfNullOrWhiteSpace(attestation.ActorIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(attestation.AttestationSource);
        ArgumentNullException.ThrowIfNull(attestation.ApprovedFragmentIds);

        var inventory = AslScenarioA1SourceInventory.Extract(manifests);
        if (attestation.PdfSha256 != inventory.PdfSha256)
        {
            throw new InvalidOperationException("The attested PDF does not match the pinned Scenario A1 source PDF.");
        }

        var expected = inventory.Rules.SelectMany(rule => rule.Fragments.Select(fragment => fragment.FragmentId))
            .Concat(inventory.LinkedFootnotes.Select(footnote => footnote.FragmentId))
            .ToHashSet(StringComparer.Ordinal);
        if (expected.Count != 11
            || attestation.ApprovedFragmentIds.Count != expected.Count
            || !expected.SetEquals(attestation.ApprovedFragmentIds))
        {
            throw new InvalidOperationException("Source attestation must name each of the 11 exact Scenario A1 fragments once.");
        }

        var document = TirStructuralExtractor.Extract(
            manifests.Registry,
            manifests.Fragments,
            new TirExtractionOptions(
                new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1"),
                attestation.AttestedAt,
                attestation.AttestationSource));
        var artifacts = document.Artifacts.OfType<TirSourceFragmentArtifact>()
            .Where(artifact => artifact.Envelope.SourceFragments.Count == 1
                && expected.Contains(artifact.Envelope.SourceFragments[0].FragmentId))
            .ToDictionary(artifact => artifact.Envelope.SourceFragments[0].FragmentId, StringComparer.Ordinal);
        if (artifacts.Count != expected.Count)
        {
            throw new InvalidOperationException("Full TIR extraction does not contain all attested source fragment artifacts.");
        }

        var sourceFragments = manifests.Fragments.Where(fragment => expected.Contains(fragment.FragmentId))
            .ToDictionary(fragment => fragment.FragmentId, StringComparer.Ordinal);
        var records = new List<TirSourceVerificationRecord>();
        foreach (var rule in inventory.Rules)
        {
            foreach (var evidence in rule.Fragments)
            {
                var page = evidence.Kind == SourceFragmentKind.RuleContinuation
                    ? evidence.ConversionStartPage
                    : rule.PdfStartPage;
                records.Add(Verify(evidence.FragmentId,
                    page ?? throw new InvalidOperationException("Scenario A1 PDF page is missing.")));
            }
        }

        foreach (var footnote in inventory.LinkedFootnotes)
        {
            records.Add(Verify(footnote.FragmentId, footnote.PdfPage));
        }

        return new AslScenarioA1VerificationBatch(document, records);

        TirSourceVerificationRecord Verify(string fragmentId, int pdfPage)
        {
            var fragment = sourceFragments[fragmentId];
            var artifact = artifacts[fragmentId];
            return TirSourceVerificationService.CreateRecord(
                document,
                artifact.Envelope.ArtifactId,
                manifests.Registry,
                fragment,
                $"User-attested PDF fidelity; PDF SHA-256 {inventory.PdfSha256}; "
                    + $"physical PDF page {pdfPage}; Markdown conversion page {fragment.Locator.StartPage}; "
                    + "review evidence docs/ASL/SourceRegistry/asl-scenario-a1.pdf-comparison.json. "
                    + "Text, linked footnote and Breach figure were individually attested; "
                    + "source fidelity does not assert semantic validity.",
                TirSourceVerificationDisposition.Verified,
                null,
                [],
                attestation.AttestedAt,
                attestation.AttestationSource,
                attestation.ActorIdentity);
        }
    }
}
