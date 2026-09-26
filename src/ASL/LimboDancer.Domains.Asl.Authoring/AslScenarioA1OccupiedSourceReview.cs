using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>Delegated, bounded PDF-fidelity disposition for the occupied-case expansion.</summary>
public static class AslScenarioA1OccupiedSourceReview
{
    public const string ComparisonSha256 = "c64fe3229d5a3541357fe6948ec037fcfbdbfde8810a599f21df81aed0a94bc5";
    private static readonly (string Rule, int Line, SourceFragmentKind Kind, int Page)[] Subjects =
    [
        ("A4.151", 259, SourceFragmentKind.RuleText, 49),
        ("A4.152", 263, SourceFragmentKind.RuleText, 49),
        ("A5.5", 363, SourceFragmentKind.RuleText, 53),
        ("A5.5-footnote-7", 2087, SourceFragmentKind.Paragraph, 101),
        ("B23.711", 1436, SourceFragmentKind.RuleText, 137),
        ("B23.711", 1442, SourceFragmentKind.RuleContinuation, 138),
        ("B23.711", 1438, SourceFragmentKind.FigureReference, 137),
    ];

    public static AslScenarioA1VerificationBatch Build(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(manifests);
        var path = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.occupied-pdf-comparison.json");
        if (Hashing.Sha256File(path) != ComparisonSha256)
        {
            throw new InvalidOperationException("The reviewed PDF comparison changed.");
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
            throw new InvalidOperationException("The occupied-case source subjects changed.");
        }

        var records = new List<TirSourceVerificationRecord>();
        for (var index = 0; index < Subjects.Length; index++)
        {
            var (rule, line, kind, page) = Subjects[index];
            var evidence = subjects[index];
            var fragment = manifests.Fragments.SingleOrDefault(candidate =>
                candidate.Locator.StartLine == line && candidate.Kind == kind
                && (rule == "A5.5-footnote-7"
                    ? candidate.SourceId == "asl-easlrb-3.10:chapter-a"
                    : candidate.Locator.NormalizedElementId == rule))
                ?? throw new InvalidOperationException("An exact source fragment is missing.");
            if (evidence.GetProperty("ruleId").GetString() != rule
                || evidence.GetProperty("startLine").GetInt32() != line
                || evidence.GetProperty("endLine").GetInt32() != fragment.Locator.EndLine
                || evidence.GetProperty("physicalPdfPage").GetInt32() != page
                || evidence.GetProperty("fragmentId").GetString() != fragment.FragmentId
                || evidence.GetProperty("sourceId").GetString() != fragment.SourceId
                || evidence.GetProperty("sourceSha256").GetString() != fragment.SourceSha256
                || evidence.GetProperty("contentSha256").GetString() != fragment.ContentSha256
                || evidence.GetProperty("kind").GetString() != KindName(kind))
            {
                throw new InvalidOperationException("Comparison no longer names the exact registered fragment.");
            }

            if (kind == SourceFragmentKind.FigureReference)
            {
                var imagePath = evidence.GetProperty("imagePath").GetString()!;
                if (!evidence.GetProperty("visuallyMatchesRenderedPdf").GetBoolean()
                    || !fragment.Dependencies.Any(dependency =>
                        imagePath.EndsWith(dependency, StringComparison.Ordinal))
                    || Hashing.Sha256File(Path.Combine(repositoryRoot, imagePath.Replace('/', Path.DirectorySeparatorChar)))
                        != evidence.GetProperty("imageSha256").GetString())
                {
                    throw new InvalidOperationException("The Breach image dependency changed.");
                }
            }
            else if (evidence.GetProperty("comparison").GetString() != "complete-alphanumeric-match"
                || evidence.GetProperty("normalizedAlphanumericSha256").GetString()
                    != Hashing.Sha256Text(Normalize(fragment.Content)))
            {
                throw new InvalidOperationException("The reviewed prose or footnote differs from PDF comparison.");
            }

            var artifact = source.SourceDocument.Artifacts.OfType<TirSourceFragmentArtifact>()
                .Single(item => item.Envelope.SourceFragments.Count == 1
                    && item.Envelope.SourceFragments[0].FragmentId == fragment.FragmentId);
            records.Add(TirSourceVerificationService.CreateRecord(
                source.SourceDocument, artifact.Envelope.ArtifactId, manifests.Registry, fragment,
                $"Delegated xUnit source review; PDF SHA-256 {AslScenarioA1SourceInventory.PdfDigest}; "
                    + $"physical PDF page {page}; comparison SHA-256 {ComparisonSha256}. "
                    + "A5.5 footnote 7 is explanatory, not an additional normative rule.",
                TirSourceVerificationDisposition.Verified, null, [],
                new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero),
                "user-directed-xunit-review-2026-09-24", "source-provider:delegated-xunit-review"));
        }

        return new AslScenarioA1VerificationBatch(source.SourceDocument, records);
    }

    private static string KindName(SourceFragmentKind kind) => kind switch
    {
        SourceFragmentKind.RuleText => "ruleText",
        SourceFragmentKind.RuleContinuation => "ruleContinuation",
        SourceFragmentKind.FigureReference => "figureReference",
        SourceFragmentKind.Paragraph => "paragraph",
        _ => throw new InvalidOperationException("Unexpected review subject kind."),
    };

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
