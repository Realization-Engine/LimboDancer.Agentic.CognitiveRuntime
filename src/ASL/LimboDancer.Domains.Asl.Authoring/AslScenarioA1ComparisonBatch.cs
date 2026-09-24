using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// Records reproducible, tool-assisted comparisons as indeterminate source decisions.
/// Only a later source-provider attestation may produce Verified dispositions.
/// </summary>
public static class AslScenarioA1ComparisonBatch
{
    private static readonly string[] ComparedRules =
    [
        "A2.4", "A3.3", "A4.1", "A4.11", "A4.13", "A5.1", "A5.11", "B23.1",
    ];

    public static AslScenarioA1VerificationBatch Build(
        GeneratedManifests manifests,
        AslScenarioA1SourceAttestation existingAttestation,
        JsonDocument comparison)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(existingAttestation);
        ArgumentNullException.ThrowIfNull(comparison);

        var root = comparison.RootElement;
        if (root.GetProperty("pdfSha256").GetString() != AslScenarioA1SourceInventory.PdfDigest
            || root.GetProperty("status").GetString()
                != "pdf-comparison-complete-source-verification-pending")
        {
            throw new InvalidOperationException("Comparison evidence does not match the pinned pending PDF review.");
        }

        var createdAt = root.GetProperty("comparisonRecordedAt").GetDateTimeOffset();
        var expected = manifests.Fragments.Where(fragment =>
                fragment.Locator.NormalizedElementId is not null
                && ComparedRules.Contains(fragment.Locator.NormalizedElementId, StringComparer.Ordinal)
                && fragment.Kind is SourceFragmentKind.RuleText
                    or SourceFragmentKind.RuleContinuation
                    or SourceFragmentKind.FigureReference)
            .ToDictionary(fragment => fragment.FragmentId, StringComparer.Ordinal);
        var records = root.GetProperty("records").EnumerateArray().ToArray();
        var actual = records.Select(record => record.GetProperty("fragmentId").GetString()
                ?? throw new InvalidOperationException("Comparison fragment ID is missing."))
            .ToArray();
        if (expected.Count != 10 || records.Length != expected.Count
            || actual.Distinct(StringComparer.Ordinal).Count() != expected.Count
            || actual.Any(fragmentId => !expected.ContainsKey(fragmentId)))
        {
            throw new InvalidOperationException("Comparison must name the ten exact first-case source fragments once.");
        }

        // Reuse the same extraction metadata as the attested batch. The two record sets
        // consequently bind to one exact full TIR document rather than parallel builds.
        var document = AslScenarioA1VerificationBatchBuilder.Build(manifests, existingAttestation).SourceDocument;
        var artifacts = document.Artifacts.OfType<TirSourceFragmentArtifact>()
            .Where(artifact => artifact.Envelope.SourceFragments.Count == 1
                && expected.ContainsKey(artifact.Envelope.SourceFragments[0].FragmentId))
            .ToDictionary(artifact => artifact.Envelope.SourceFragments[0].FragmentId, StringComparer.Ordinal);
        if (artifacts.Count != expected.Count)
        {
            throw new InvalidOperationException("Full TIR does not contain all ten comparison subjects.");
        }

        var decisions = new List<TirSourceVerificationRecord>();
        foreach (var evidence in records)
        {
            var fragmentId = evidence.GetProperty("fragmentId").GetString()!;
            var fragment = expected[fragmentId];
            var physicalPage = evidence.GetProperty("physicalPdfPage").GetInt32();
            if (evidence.GetProperty("sourceVerificationStatus").GetString() != "unverified"
                || evidence.GetProperty("sourceSha256").GetString() != fragment.SourceSha256
                || evidence.GetProperty("contentSha256").GetString() != fragment.ContentSha256
                || evidence.GetProperty("normalizedRuleId").GetString() != fragment.Locator.NormalizedElementId
                || evidence.GetProperty("startLine").GetInt32() != fragment.Locator.StartLine
                || evidence.GetProperty("endLine").GetInt32() != fragment.Locator.EndLine
                || evidence.GetProperty("conversionPage").GetInt32() != fragment.Locator.StartPage
                || physicalPage is < 6 or > 253)
            {
                throw new InvalidOperationException("Comparison evidence differs from the pinned source fragment.");
            }

            if (fragment.Kind == SourceFragmentKind.FigureReference)
            {
                var imagePath = evidence.GetProperty("imagePath").GetString()!;
                var image = manifests.Registry.Artifacts.SingleOrDefault(artifact =>
                    artifact.Path.EndsWith(imagePath, StringComparison.Ordinal));
                if (!fragment.Dependencies.Contains(imagePath, StringComparer.Ordinal)
                    || image?.Sha256 != evidence.GetProperty("imageSha256").GetString()
                    || !evidence.GetProperty("visuallyMatchesRenderedPdf").GetBoolean())
                {
                    throw new InvalidOperationException("Compared figure is not the exact registered dependency.");
                }
            }
            else if (!evidence.GetProperty("alphanumericSequenceMatch").GetBoolean())
            {
                throw new InvalidOperationException("Compared rule text has no matching PDF alphanumeric sequence.");
            }

            decisions.Add(TirSourceVerificationService.CreateRecord(
                document,
                artifacts[fragmentId].Envelope.ArtifactId,
                manifests.Registry,
                fragment,
                $"Tool-assisted comparison; PDF SHA-256 {AslScenarioA1SourceInventory.PdfDigest}; "
                    + $"physical PDF page {physicalPage}; Markdown conversion page {fragment.Locator.StartPage}; "
                    + "evidence docs/ASL/SourceRegistry/asl-scenario-a1.first-case-pdf-comparison.json.",
                TirSourceVerificationDisposition.Indeterminate,
                "Human source-fidelity attestation for this fragment is pending; "
                    + "comparison alone cannot approve it.",
                [],
                createdAt,
                "tool-assisted-pdf-comparison-awaiting-human-attestation",
                "comparison-process:codex-not-human-verifier"));
        }

        return new AslScenarioA1VerificationBatch(document, decisions);
    }
}
