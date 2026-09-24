using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1CaseAssessmentTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

    [Fact]
    public void InitialAttestationLeavesSpecificBaselineSourceAndSemanticGaps()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var initialAttested = AslScenarioA1SourceInventory.Extract(manifests);
        var verified = initialAttested.Rules.SelectMany(rule => rule.Fragments)
            .Select(fragment => fragment.FragmentId)
            .Concat(initialAttested.LinkedFootnotes.Select(footnote => footnote.FragmentId))
            .ToHashSet(StringComparer.Ordinal);

        var result = AslScenarioA1CaseAssessor.Assess(
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase(), manifests.Fragments, verified);
        Assert.False(result.CanIssueDefinitiveRuling);
        Assert.Contains("A2.4", result.RequiredRuleIds);
        Assert.Contains("A3.3", result.RequiredRuleIds);
        Assert.Contains("A4.11", result.RequiredRuleIds);
        Assert.Contains("A5.11", result.RequiredRuleIds);
        Assert.Contains("B23.1", result.RequiredRuleIds);
        Assert.Contains("B23.711", result.ExcludedBranchRuleIds);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.SourceBoundaryUnresolved
            && blocker.Reference.Contains("PDF page 698", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.SourceRuleNotLocated);
        foreach (var id in new[] { "A2.4", "A3.3", "A4.1", "A4.11", "A4.13", "A5.1", "A5.11", "B23.1" })
        {
            var fragment = Assert.Single(manifests.Fragments, candidate =>
                candidate.Locator.NormalizedElementId == id
                && candidate.Kind == SourceFragmentKind.RuleText);
            Assert.Contains(result.Blockers, blocker =>
                blocker.Kind == AslScenarioA1CaseBlockerKind.SourceFragmentUnverified
                && blocker.Reference == fragment.FragmentId);
        }

        foreach (var id in new[] { "A2.4", "B23.1" })
        {
            var figure = Assert.Single(manifests.Fragments, candidate =>
                candidate.Locator.NormalizedElementId == id
                && candidate.Kind == SourceFragmentKind.FigureReference);
            Assert.Contains(result.Blockers, blocker =>
                blocker.Kind == AslScenarioA1CaseBlockerKind.SourceFragmentUnverified
                && blocker.Reference == figure.FragmentId);
        }
    }

    [Fact]
    public void UnknownOccupancyOrSpecialRulePreventsFirstCaseScope()
    {
        var result = AslScenarioA1CaseAssessor.Assess(
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase() with
            {
                IsDestinationKnownEmpty = null,
                HasNoSpecialRuleOrOtherModifier = false,
            },
            [],
            new HashSet<string>(StringComparer.Ordinal));

        Assert.False(result.CanIssueDefinitiveRuling);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope
            && blocker.Reference == nameof(AslScenarioA1CaseFacts.IsDestinationKnownEmpty));
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope
            && blocker.Reference == nameof(AslScenarioA1CaseFacts.HasNoSpecialRuleOrOtherModifier));
    }

    [Fact]
    public void EvenEveryCandidateSourceFragmentWouldNotAuthorizeAnOutcome()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var all = manifests.Fragments.Select(fragment => fragment.FragmentId)
            .ToHashSet(StringComparer.Ordinal);
        var result = AslScenarioA1CaseAssessor.Assess(
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase(), manifests.Fragments, all);

        Assert.DoesNotContain(result.Blockers, blocker =>
            blocker.Kind is AslScenarioA1CaseBlockerKind.SourceRuleNotLocated
                or AslScenarioA1CaseBlockerKind.SourceFragmentUnverified);
        Assert.False(result.CanIssueDefinitiveRuling);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending);
    }

    [Fact]
    public void PdfComparisonEvidencePinsTenExactUnverifiedFragmentsAndTwoImages()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.first-case-pdf-comparison.json");
        using var evidence = JsonDocument.Parse(File.ReadAllText(path));
        var root = evidence.RootElement;
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest,
            root.GetProperty("pdfSha256").GetString());
        Assert.Equal("pdf-comparison-complete-source-verification-pending",
            root.GetProperty("status").GetString());
        var records = root.GetProperty("records").EnumerateArray().ToArray();
        Assert.Equal(10, records.Length);
        foreach (var record in records)
        {
            var fragment = Assert.Single(manifests.Fragments, candidate =>
                candidate.FragmentId == record.GetProperty("fragmentId").GetString());
            Assert.Equal(fragment.ContentSha256, record.GetProperty("contentSha256").GetString());
            Assert.Equal(fragment.SourceSha256, record.GetProperty("sourceSha256").GetString());
            Assert.Equal(fragment.Locator.StartLine, record.GetProperty("startLine").GetInt32());
            Assert.Equal(fragment.Locator.StartPage, record.GetProperty("conversionPage").GetInt32());
            Assert.Equal("unverified", record.GetProperty("sourceVerificationStatus").GetString());
            if (record.TryGetProperty("imagePath", out var imagePath))
            {
                Assert.Equal(SourceFragmentKind.FigureReference, fragment.Kind);
                Assert.Contains(imagePath.GetString()!, fragment.Dependencies);
                var image = Assert.Single(manifests.Registry.Artifacts, artifact =>
                    artifact.Path.EndsWith(imagePath.GetString()!, StringComparison.Ordinal));
                Assert.Equal(image.Sha256, record.GetProperty("imageSha256").GetString());
                Assert.True(record.GetProperty("visuallyMatchesRenderedPdf").GetBoolean());
            }
            else
            {
                Assert.Equal(SourceFragmentKind.RuleText, fragment.Kind);
                Assert.True(record.GetProperty("alphanumericSequenceMatch").GetBoolean());
            }
        }

        Assert.Equal(2, records.Count(record => record.TryGetProperty("imagePath", out _)));
        Assert.Equal(2, records.Count(record =>
            record.GetProperty("normalizedRuleId").GetString() == "B23.1"
            && record.GetProperty("conversionPage").GetInt32() == 134
            && record.GetProperty("physicalPdfPage").GetInt32() == 135));
    }
}
