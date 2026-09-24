namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// Declared facts for a bounded investigation case, not observations inferred from a board.
/// A false or unknown fact moves the case outside this investigation scope.
/// </summary>
public sealed record AslScenarioA1CaseFacts(
    bool? IsKnownGoodOrderInfantrySquad,
    bool? IsAttackerMovementPhase,
    bool? CanMoveThisPhase,
    bool? IsAdjacentGroundLevelOrdinaryBuilding,
    bool? IsDestinationKnownEmpty,
    bool? HasNoRoadBypassElevationOrAdditionalTerrain,
    bool? HasEnoughMovementFactors,
    bool? IsBelowStackingLimit,
    bool? HasNoSpecialRuleOrOtherModifier)
{
    public static AslScenarioA1CaseFacts CreateDeclaredFirstCase() =>
        new(true, true, true, true, true, true, true, true, true);
}

public enum AslScenarioA1CaseBlockerKind
{
    FactOutsideDeclaredScope,
    SourceRuleNotLocated,
    SourceFragmentUnverified,
    SourceBoundaryUnresolved,
    DependencyAndSemanticReviewPending,
}

public sealed record AslScenarioA1CaseBlocker(
    AslScenarioA1CaseBlockerKind Kind,
    string Reference);

public sealed record AslScenarioA1SupplementalSourceCandidate(
    string CandidateId,
    int PhysicalPdfPage,
    string SourcePdfSha256);

public sealed record AslScenarioA1CaseAssessment(
    IReadOnlyList<string> RequiredRuleIds,
    IReadOnlyList<string> ExcludedBranchRuleIds,
    IReadOnlyList<AslScenarioA1SupplementalSourceCandidate> UnresolvedSupplementalSources,
    IReadOnlyList<AslScenarioA1CaseBlocker> Blockers)
{
    // A source inventory is not an accepted semantic rule model or a legality decision.
    public bool CanIssueDefinitiveRuling => Blockers.Count == 0;
}

public static class AslScenarioA1CaseAssessor
{
    public static AslScenarioA1CaseAssessment AssessWithReviewedChart(
        AslScenarioA1CaseFacts facts,
        IReadOnlyList<SourceFragment> fragments,
        IReadOnlySet<string> verifiedFragmentIds,
        string repositoryRoot,
        AslScenarioA1ChartReviewDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        using var registry = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.supplementary-source-registry.json")));
        using var comparison = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.backmatter-chart-pdf-comparison.json")));
        var expected = AslScenarioA1ChartReview.Evaluate(repositoryRoot, registry, comparison);
        if (!(decision with { RuleBasis = expected.RuleBasis }).Equals(expected)
            || decision.RuleBasis is null
            || !decision.RuleBasis.SequenceEqual(expected.RuleBasis))
            throw new InvalidOperationException("The chart review does not match the pinned evidence.");

        var baseAssessment = Assess(facts, fragments, verifiedFragmentIds);
        if (baseAssessment.Blockers.Any(blocker => blocker.Kind ==
            AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope))
            return baseAssessment;

        return baseAssessment with
        {
            UnresolvedSupplementalSources = [],
            Blockers = baseAssessment.Blockers.Where(blocker =>
                blocker.Kind != AslScenarioA1CaseBlockerKind.SourceBoundaryUnresolved).ToArray(),
        };
    }

    public static AslScenarioA1SupplementalSourceCandidate CreateBuildingChartCandidate() =>
        new("asl-supplement:b-terrain-chart-building-entry", 698,
            AslScenarioA1SourceInventory.PdfDigest);

    // Candidate baseline for one declared MPh entry into an empty ordinary building.
    // The domain reviewer must extend or correct this list and approve its applicability.
    private static readonly string[] RequiredRules =
    [
        "A2.4", "A2.8", "A3.3", "A4.1", "A4.11", "A4.13", "A4.14",
        "A5.1", "A5.11", "B23.1", "B23.4",
    ];

    // A reviewer must confirm the declared facts really exclude these branches.
    private static readonly string[] ExcludedBranchRules =
    [
        "A4.132", "A4.134", "A4.15", "A4.7", "A12.15",
        "B23.711", "B23.922", "B23.9221",
    ];

    public static AslScenarioA1CaseAssessment Assess(
        AslScenarioA1CaseFacts facts,
        IReadOnlyList<SourceFragment> fragments,
        IReadOnlySet<string> verifiedFragmentIds)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(fragments);
        ArgumentNullException.ThrowIfNull(verifiedFragmentIds);

        var blockers = new List<AslScenarioA1CaseBlocker>();
        (string Name, bool? Value)[] declaredFacts =
        [
            (nameof(facts.IsKnownGoodOrderInfantrySquad), facts.IsKnownGoodOrderInfantrySquad),
            (nameof(facts.IsAttackerMovementPhase), facts.IsAttackerMovementPhase),
            (nameof(facts.CanMoveThisPhase), facts.CanMoveThisPhase),
            (nameof(facts.IsAdjacentGroundLevelOrdinaryBuilding), facts.IsAdjacentGroundLevelOrdinaryBuilding),
            (nameof(facts.IsDestinationKnownEmpty), facts.IsDestinationKnownEmpty),
            (nameof(facts.HasNoRoadBypassElevationOrAdditionalTerrain), facts.HasNoRoadBypassElevationOrAdditionalTerrain),
            (nameof(facts.HasEnoughMovementFactors), facts.HasEnoughMovementFactors),
            (nameof(facts.IsBelowStackingLimit), facts.IsBelowStackingLimit),
            (nameof(facts.HasNoSpecialRuleOrOtherModifier), facts.HasNoSpecialRuleOrOtherModifier),
        ];
        foreach (var (name, value) in declaredFacts)
        {
            if (value is not true)
            {
                blockers.Add(new AslScenarioA1CaseBlocker(
                    AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope, name));
            }
        }

        foreach (var ruleId in RequiredRules)
        {
            var ruleFragments = fragments.Where(fragment =>
                    fragment.Locator.NormalizedElementId == ruleId
                    && fragment.Kind is SourceFragmentKind.RuleText
                        or SourceFragmentKind.RuleContinuation
                        or SourceFragmentKind.FigureReference)
                .ToArray();
            if (!ruleFragments.Any(fragment => fragment.Kind == SourceFragmentKind.RuleText))
            {
                blockers.Add(new AslScenarioA1CaseBlocker(
                    AslScenarioA1CaseBlockerKind.SourceRuleNotLocated, ruleId));
                continue;
            }

            foreach (var fragment in ruleFragments.Where(fragment =>
                !verifiedFragmentIds.Contains(fragment.FragmentId)))
            {
                blockers.Add(new AslScenarioA1CaseBlocker(
                    AslScenarioA1CaseBlockerKind.SourceFragmentUnverified,
                    fragment.FragmentId));
            }
        }

        var chartCandidate = CreateBuildingChartCandidate();
        blockers.Add(new AslScenarioA1CaseBlocker(
            AslScenarioA1CaseBlockerKind.SourceBoundaryUnresolved,
            chartCandidate.CandidateId));
        blockers.Add(new AslScenarioA1CaseBlocker(
            AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending,
            "Scenario A1 first-case applicability, exceptions and dependency closure"));
        return new AslScenarioA1CaseAssessment(RequiredRules, ExcludedBranchRules,
            [chartCandidate], blockers);
    }
}
