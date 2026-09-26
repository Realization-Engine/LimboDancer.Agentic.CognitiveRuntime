using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// Delegated, bounded PDF-fidelity disposition for the OVR NTC package (unit step 10): Random Selection (A.9), the
/// Morale Check and Task Check (A10.1), building TEM (B23.3), and the NTC glossary entry. It is separate from the
/// attestation and the earlier reviews, so their records and digests are unchanged.
/// </summary>
public static class AslScenarioA1OvrNtcSourceReview
{
    public const string ComparisonSha256 = "a5e488176656adf765dc0dc1afea55db0fd60d03d0380a90443baa09b7505193";

    private static readonly (string Rule, string Source, int Line, SourceFragmentKind Kind, int Page)[] Subjects =
    [
        ("A.9", "asl-easlrb-3.10:chapter-a", 43, SourceFragmentKind.RuleText, 43),
        ("A10.1", "asl-easlrb-3.10:chapter-a", 762, SourceFragmentKind.RuleText, 65),
        ("B23.3", "asl-easlrb-3.10:chapter-b", 1372, SourceFragmentKind.RuleText, 136),
        ("NTC-glossary", "asl-easlrb-3.10:index-glossary", 1823, SourceFragmentKind.Paragraph, 30),
    ];

    public static AslScenarioA1VerificationBatch Build(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(manifests);
        var path = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.ovr-ntc-pdf-comparison.json");
        if (Hashing.Sha256File(path) != ComparisonSha256)
        {
            throw new InvalidOperationException("The reviewed OVR NTC PDF comparison changed.");
        }

        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var root = report.RootElement;
        if (root.GetProperty("pdfSha256").GetString() != AslScenarioA1SourceInventory.PdfDigest
            || root.GetProperty("status").GetString() != "pdf-comparison-complete-delegated-xunit-review")
        {
            throw new InvalidOperationException("The reviewed PDF identity or status changed.");
        }

        var source = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);
        var subjects = root.GetProperty("subjects").EnumerateArray().ToArray();
        if (subjects.Length != Subjects.Length)
        {
            throw new InvalidOperationException("The OVR NTC source subjects changed.");
        }

        var records = new List<TirSourceVerificationRecord>();
        for (var index = 0; index < Subjects.Length; index++)
        {
            var (rule, sourceId, line, kind, page) = Subjects[index];
            var evidence = subjects[index];

            // The glossary entry has no rule id, so it is matched by its source and line.
            var fragment = manifests.Fragments.SingleOrDefault(candidate =>
                candidate.SourceId == sourceId && candidate.Locator.StartLine == line && candidate.Kind == kind
                && (kind == SourceFragmentKind.Paragraph || candidate.Locator.NormalizedElementId == rule))
                ?? throw new InvalidOperationException("An exact source fragment is missing.");
            if (evidence.GetProperty("ruleId").GetString() != rule
                || evidence.GetProperty("startLine").GetInt32() != line
                || evidence.GetProperty("endLine").GetInt32() != fragment.Locator.EndLine
                || evidence.GetProperty("physicalPdfPage").GetInt32() != page
                || evidence.GetProperty("fragmentId").GetString() != fragment.FragmentId
                || evidence.GetProperty("sourceId").GetString() != fragment.SourceId
                || evidence.GetProperty("sourceSha256").GetString() != fragment.SourceSha256
                || evidence.GetProperty("contentSha256").GetString() != fragment.ContentSha256
                || evidence.GetProperty("kind").GetString() != (kind == SourceFragmentKind.Paragraph ? "paragraph" : "ruleText"))
            {
                throw new InvalidOperationException("Comparison no longer names the exact registered fragment.");
            }

            if (evidence.GetProperty("comparison").GetString() != "complete-alphanumeric-match"
                || evidence.GetProperty("normalizedAlphanumericSha256").GetString() != Hashing.Sha256Text(Normalize(fragment.Content)))
            {
                throw new InvalidOperationException("The reviewed text differs from the PDF comparison.");
            }

            var artifact = source.SourceDocument.Artifacts.OfType<TirSourceFragmentArtifact>()
                .Single(item => item.Envelope.SourceFragments.Count == 1
                    && item.Envelope.SourceFragments[0].FragmentId == fragment.FragmentId);
            records.Add(TirSourceVerificationService.CreateRecord(
                source.SourceDocument, artifact.Envelope.ArtifactId, manifests.Registry, fragment,
                $"Delegated xUnit source review; PDF SHA-256 {AslScenarioA1SourceInventory.PdfDigest}; "
                    + $"physical PDF page {page}; comparison SHA-256 {ComparisonSha256}. "
                    + "The NTC glossary entry defines the Normal Task Check the Index cites for A4.15.",
                TirSourceVerificationDisposition.Verified, null, [],
                new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero),
                "user-directed-xunit-review-2026-09-26", "source-provider:delegated-xunit-review"));
        }

        return new AslScenarioA1VerificationBatch(source.SourceDocument, records);
    }

    private static string Normalize(string value)
    {
        var withoutMarkup = Regex.Replace(value, "<[^>]+>", string.Empty)
            .Replace("ﬂ", "fl", StringComparison.Ordinal)
            .Replace("ﬁ", "fi", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormKD);
        return new string(withoutMarkup.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant).ToArray());
    }
}
