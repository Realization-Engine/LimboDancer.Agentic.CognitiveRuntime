using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// Delegated, bounded PDF-fidelity disposition for the Fire package's prose sources (unit step 17): the PFPh and DFPh,
/// LOS Hindrance, fire attacks and their results, fire groups and fire direction, TEM, pins, Cowering, morale checks,
/// leadership, concealment loss, ELR, and the TEM and Hindrance rules of the fixture terrain. A.9, A10.1, and B23.3
/// were verified by the step 10 review and are not repeated. The records are separate from the earlier reviews, so
/// their digests are unchanged.
/// </summary>
public static class AslScenarioA1FireSourceReview
{
    public const string ComparisonFile = "asl-scenario-a1.fire-pdf-comparison.json";
    public const string ComparisonSha256 = "00673414dabddc4469018c1abb9664325afac84016e63fc7645c5b84dcc023f4";

    /// <summary>The unit step 18 comparison: Self-Rally (A10.63) and Wounds (A17.1, A17.11, A17.3).</summary>
    public const string BranchesComparisonFile = "asl-scenario-a1.fire-branches-pdf-comparison.json";
    public const string BranchesComparisonSha256 = "417ca7726c27e070543f4b8bf75ef5ff79652e0cf3f3509a1c4da252968af155";

    private const string ChapterA = "asl-easlrb-3.10:chapter-a";
    private const string ChapterB = "asl-easlrb-3.10:chapter-b";

    // Rule, the element id the conversion registered the fragment under, source, line, kind, physical page.
    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] Subjects =
    [
        ("A3.2", "A3.2", ChapterA, 199, SourceFragmentKind.RuleText, 47),
        ("A3.4", "A3.4", ChapterA, 203, SourceFragmentKind.RuleText, 47),
        ("A6.7", "A6.7", ChapterA, 407, SourceFragmentKind.RuleText, 54),
        ("A6.7", "A6.7", ChapterA, 413, SourceFragmentKind.RuleContinuation, 54),
        ("A7.1", "A7.1", ChapterA, 431, SourceFragmentKind.RuleText, 54),
        ("A7.2", "A7.2", ChapterA, 433, SourceFragmentKind.RuleText, 54),
        ("A7.21", "A7.21", ChapterA, 435, SourceFragmentKind.RuleText, 54),
        ("A7.21", "A7.21", ChapterA, 439, SourceFragmentKind.RuleContinuation, 55),
        ("A7.212", "A7.212", ChapterA, 445, SourceFragmentKind.RuleText, 55),
        ("A7.22", "A7.22", ChapterA, 449, SourceFragmentKind.RuleText, 55),
        ("A7.23", "A7.23", ChapterA, 451, SourceFragmentKind.RuleText, 55),
        ("A7.3", "A7.3", ChapterA, 459, SourceFragmentKind.RuleText, 55),
        ("A7.301", "A7.301", ChapterA, 463, SourceFragmentKind.RuleText, 55),
        ("A7.302", "A7.302", ChapterA, 467, SourceFragmentKind.RuleText, 55),
        ("A7.303", "A7.303", ChapterA, 471, SourceFragmentKind.RuleText, 55),
        ("A7.304", "A7.304", ChapterA, 475, SourceFragmentKind.RuleText, 55),
        ("A7.305", "A7.305", ChapterA, 479, SourceFragmentKind.RuleText, 55),
        ("A7.306", "A7.306", ChapterA, 481, SourceFragmentKind.RuleText, 55),
        ("A7.31", "A7.31", ChapterA, 491, SourceFragmentKind.RuleText, 56),
        ("A7.4", "A7.4", ChapterA, 527, SourceFragmentKind.RuleText, 57),
        ("A7.5", "A7.5", ChapterA, 529, SourceFragmentKind.RuleText, 57),
        ("A7.52", "A7.52", ChapterA, 533, SourceFragmentKind.RuleText, 57),
        ("A7.53", "A7.53", ChapterA, 535, SourceFragmentKind.RuleText, 57),
        ("A7.531", "A7.531", ChapterA, 537, SourceFragmentKind.RuleText, 57),
        ("A7.55", "A7.55", ChapterA, 541, SourceFragmentKind.RuleText, 57),
        ("A7.6", "A7.6", ChapterA, 543, SourceFragmentKind.RuleText, 57),
        ("A7.8", "A7.72", ChapterA, 574, SourceFragmentKind.RuleContinuation, 58),
        ("A7.9", "A7.9", ChapterA, 586, SourceFragmentKind.RuleText, 58),
        ("A10.2", "A10.2", ChapterA, 764, SourceFragmentKind.RuleText, 65),
        ("A10.21", "A10.21", ChapterA, 768, SourceFragmentKind.RuleText, 66),
        ("A10.22", "A10.22", ChapterA, 772, SourceFragmentKind.RuleText, 66),
        ("A10.3", "A10.3", ChapterA, 774, SourceFragmentKind.RuleText, 66),
        ("A10.31", "A10.31", ChapterA, 776, SourceFragmentKind.RuleText, 66),
        ("A10.4", "A10.4", ChapterA, 778, SourceFragmentKind.RuleText, 66),
        ("A10.7", "A10.7", ChapterA, 832, SourceFragmentKind.RuleText, 68),
        ("A10.72", "A10.72", ChapterA, 844, SourceFragmentKind.RuleText, 69),
        ("A12.14", "A12.14", ChapterA, 1019, SourceFragmentKind.RuleText, 77),
        ("A12.14", "A12.14", ChapterA, 1021, SourceFragmentKind.RuleContinuation, 77),
        ("A12.14", "A12.14", ChapterA, 1026, SourceFragmentKind.RuleContinuation, 78),
        ("A12.14", "A12.14", ChapterA, 1028, SourceFragmentKind.RuleContinuation, 78),
        ("A12.14", "A12.14", ChapterA, 1034, SourceFragmentKind.RuleContinuation, 78),
        ("A12.141", "A12.141", ChapterA, 1040, SourceFragmentKind.RuleText, 78),
        ("A19.1", "A19.1", ChapterA, 1360, SourceFragmentKind.RuleText, 86),
        ("A19.11", "A19.11", ChapterA, 1362, SourceFragmentKind.RuleText, 86),
        ("A19.12", "A19.12", ChapterA, 1364, SourceFragmentKind.RuleText, 86),
        ("A19.13", "A19.13", ChapterA, 1368, SourceFragmentKind.RuleText, 86),
        ("A.5", "A.5", ChapterA, 31, SourceFragmentKind.RuleText, 43),
        ("A.17", "A.17", ChapterA, 73, SourceFragmentKind.RuleText, 44),
        ("B1.1", "B1.1", ChapterB, 66, SourceFragmentKind.RuleText, 113),
        ("B12.2", "B12.2", ChapterB, 796, SourceFragmentKind.RuleText, 127),
        ("B13.3", "B13.3", ChapterB, 836, SourceFragmentKind.RuleText, 128),
        ("B14.2", "B14.2", ChapterB, 908, SourceFragmentKind.RuleText, 129),
        ("B14.3", "B14.3", ChapterB, 918, SourceFragmentKind.RuleText, 129),
        ("B15.2", "B15.2", ChapterB, 950, SourceFragmentKind.RuleText, 129),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] BranchSubjects =
    [
        ("A10.63", "A10.63", ChapterA, 828, SourceFragmentKind.RuleText, 68),
        ("A17.1", "A17.1", ChapterA, 1316, SourceFragmentKind.RuleText, 85),
        ("A17.11", "A17.11", ChapterA, 1320, SourceFragmentKind.RuleText, 85),
        ("A17.3", "A17.3", ChapterA, 1324, SourceFragmentKind.RuleText, 85),
    ];

    /// <summary>The verified fragments, in subject order, keyed by rule id for the Fire package.</summary>
    public static IReadOnlyList<(string Rule, int Page, SourceFragment Fragment)> Fragments(GeneratedManifests manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        return Subjects.Select(subject => (subject.Rule, subject.Page, Find(manifests, subject))).ToArray();
    }

    public static AslScenarioA1VerificationBatch Build(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, ComparisonFile, ComparisonSha256, Subjects, "unit step 17 Fire review");

    /// <summary>The unit step 18 subjects that close the Fire package's undecided branches.</summary>
    public static AslScenarioA1VerificationBatch BuildBranches(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, BranchesComparisonFile, BranchesComparisonSha256, BranchSubjects,
            "unit step 18 Fire branches review");

    private static AslScenarioA1VerificationBatch Build(string repositoryRoot, GeneratedManifests manifests,
        AslScenarioA1SourceAttestation attestation, string file, string digest,
        (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] subjectList, string review)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(manifests);
        var path = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry", file);
        Require(Hashing.Sha256File(path) == digest, "The reviewed Fire PDF comparison changed.");

        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var root = report.RootElement;
        Require(root.GetProperty("pdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest
            && root.GetProperty("status").GetString() == "pdf-comparison-complete-delegated-xunit-review",
            "The reviewed PDF identity or status changed.");

        var source = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);
        var subjects = root.GetProperty("subjects").EnumerateArray().ToArray();
        Require(subjects.Length == subjectList.Length, "The Fire source subjects changed.");

        var records = new List<TirSourceVerificationRecord>();
        for (var index = 0; index < subjectList.Length; index++)
        {
            var subject = subjectList[index];
            var evidence = subjects[index];
            var fragment = Find(manifests, subject);
            var registered = evidence.TryGetProperty("registeredElementId", out var element)
                ? element.GetString()
                : subject.Rule;
            Require(evidence.GetProperty("ruleId").GetString() == subject.Rule
                && registered == subject.Registered
                && evidence.GetProperty("startLine").GetInt32() == subject.Line
                && evidence.GetProperty("endLine").GetInt32() == fragment.Locator.EndLine
                && evidence.GetProperty("physicalPdfPage").GetInt32() == subject.Page
                && evidence.GetProperty("fragmentId").GetString() == fragment.FragmentId
                && evidence.GetProperty("sourceId").GetString() == fragment.SourceId
                && evidence.GetProperty("sourceSha256").GetString() == fragment.SourceSha256
                && evidence.GetProperty("contentSha256").GetString() == fragment.ContentSha256
                && evidence.GetProperty("kind").GetString() == (subject.Kind == SourceFragmentKind.RuleText ? "ruleText" : "ruleContinuation"),
                "Comparison no longer names the exact registered fragment.");

            var normalized = Normalize(fragment.Content);
            var comparison = evidence.GetProperty("comparison").GetString();
            Require(evidence.GetProperty("normalizedAlphanumericSha256").GetString() == Hashing.Sha256Text(normalized)
                && evidence.GetProperty("normalizedAlphanumericLength").GetInt32() == normalized.Length
                && (comparison == "complete-alphanumeric-match"
                    || (comparison == "complete-alphanumeric-match-in-two-parts" && subject.Rule == "A7.212"
                        && evidence.GetProperty("partLengths").EnumerateArray().Sum(part => part.GetInt32()) == normalized.Length)),
                "The reviewed text differs from the PDF comparison.");

            var artifact = source.SourceDocument.Artifacts.OfType<TirSourceFragmentArtifact>()
                .Single(item => item.Envelope.SourceFragments.Count == 1
                    && item.Envelope.SourceFragments[0].FragmentId == fragment.FragmentId);
            records.Add(TirSourceVerificationService.CreateRecord(
                source.SourceDocument, artifact.Envelope.ArtifactId, manifests.Registry, fragment,
                $"Delegated xUnit source review; PDF SHA-256 {AslScenarioA1SourceInventory.PdfDigest}; "
                    + $"physical PDF page {subject.Page}; comparison SHA-256 {digest}. "
                    + $"Source for {subject.Rule} in the {review}.",
                TirSourceVerificationDisposition.Verified, null, [],
                new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero),
                "user-directed-xunit-review-2026-09-26", "source-provider:delegated-xunit-review"));
        }

        return new AslScenarioA1VerificationBatch(source.SourceDocument, records);
    }

    private static SourceFragment Find(GeneratedManifests manifests,
        (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page) subject) =>
        manifests.Fragments.SingleOrDefault(candidate =>
            candidate.SourceId == subject.Source && candidate.Locator.StartLine == subject.Line
            && candidate.Kind == subject.Kind && candidate.Locator.NormalizedElementId == subject.Registered)
        ?? throw new InvalidOperationException($"The exact source fragment for {subject.Rule} is missing.");

    private static string Normalize(string value)
    {
        var withoutMarkup = Regex.Replace(value, "<[^>]+>", string.Empty)
            .Replace("ﬂ", "fl", StringComparison.Ordinal)
            .Replace("ﬁ", "fi", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormKD);
        return new string(withoutMarkup.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant).ToArray());
    }

    private static void Require(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }
    }
}
