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
            AslScenarioA1CaseFacts.DeclaredFirstCase, manifests.Fragments, verified);
        Assert.False(result.CanIssueDefinitiveRuling);
        Assert.Contains("A2.4", result.RequiredRuleIds);
        Assert.Contains("A3.3", result.RequiredRuleIds);
        Assert.Contains("A4.11", result.RequiredRuleIds);
        Assert.Contains("A5.11", result.RequiredRuleIds);
        Assert.Contains("B23.1", result.RequiredRuleIds);
        Assert.Contains("B23.711", result.ExcludedBranchRuleIds);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending);
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
    }

    [Fact]
    public void UnknownOccupancyOrSpecialRulePreventsFirstCaseScope()
    {
        var result = AslScenarioA1CaseAssessor.Assess(
            AslScenarioA1CaseFacts.DeclaredFirstCase with
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
            AslScenarioA1CaseFacts.DeclaredFirstCase, manifests.Fragments, all);

        Assert.DoesNotContain(result.Blockers, blocker =>
            blocker.Kind is AslScenarioA1CaseBlockerKind.SourceRuleNotLocated
                or AslScenarioA1CaseBlockerKind.SourceFragmentUnverified);
        Assert.False(result.CanIssueDefinitiveRuling);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending);
    }
}
