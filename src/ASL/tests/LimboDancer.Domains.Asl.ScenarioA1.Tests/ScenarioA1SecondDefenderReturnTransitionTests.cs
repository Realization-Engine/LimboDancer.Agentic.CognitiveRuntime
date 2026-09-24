using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SecondDefenderReturnTransitionTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("smc-revealed", "revealedEnemySmc-after-election")]
    [InlineData("mmc-revealed", "revealedEnemyMmc-after-election")]
    public async Task ExactConclusionProducesCandidateReturnAndSingleMfDebit(
        string suffix, string revealed)
    {
        var original = State();
        var conclusion = await Conclusion(suffix, revealed);
        var attempt = new ScenarioA1ReturnAttempt("attempt-1", original.Version, conclusion);
        var result = ScenarioA1SecondDefenderReturnTransition.Evaluate(original, attempt);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Applied, result.Status);
        Assert.Equal(10, original.Version);
        Assert.Equal("bd01:E4:0", original.UnitLocationId);
        Assert.Equal(4, original.RemainingMf);
        Assert.Equal(11, result.State.Version);
        Assert.Equal("bd01:D4:0", result.State.UnitLocationId);
        Assert.Equal("bd01:D4:0", result.State.MfExpenditureLocationId);
        Assert.Equal(2, result.State.RemainingMf);
        Assert.True(result.State.MovementEnded);
        Assert.True(result.State.AttemptedEntryMfAlreadyDebited);
        Assert.Equal(ScenarioA1ReturnStage.Returned, result.State.Stage);

        var replay = ScenarioA1SecondDefenderReturnTransition.Evaluate(result.State, attempt);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Replay, replay.Status);
        Assert.Same(result.State, replay.State);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Conflict,
            ScenarioA1SecondDefenderReturnTransition.Evaluate(result.State,
                attempt with { ExpectedVersion = 11 }).Status);
    }

    [Fact]
    public async Task AlreadyDebitedAttemptIsAttributedWithoutASecondDebit()
    {
        var state = State() with { RemainingMf = 2, AttemptedEntryMfAlreadyDebited = true };
        var result = ScenarioA1SecondDefenderReturnTransition.Evaluate(state,
            new ScenarioA1ReturnAttempt("attempt-1", 10,
                await Conclusion("smc-revealed", "revealedEnemySmc-after-election")));
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Applied, result.Status);
        Assert.Equal(2, result.State.RemainingMf);
        Assert.Equal("bd01:D4:0", result.State.MfExpenditureLocationId);
    }

    [Theory]
    [InlineData(ScenarioA1ReturnHazard.Unknown)]
    [InlineData(ScenarioA1ReturnHazard.ResidualFirepower)]
    [InlineData(ScenarioA1ReturnHazard.Ffe)]
    [InlineData(ScenarioA1ReturnHazard.Minefield)]
    [InlineData(ScenarioA1ReturnHazard.SpecialPlacement)]
    public async Task ConditionalReturnHazardsCannotEnterClearSubset(
        ScenarioA1ReturnHazard hazard)
    {
        var state = State() with { ReturnHazard = hazard };
        var result = ScenarioA1SecondDefenderReturnTransition.Evaluate(state,
            new ScenarioA1ReturnAttempt("attempt-1", 10,
                await Conclusion("smc-revealed", "revealedEnemySmc-after-election")));
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Denied, result.Status);
        Assert.Same(state, result.State);
    }

    [Fact]
    public async Task StaleOrChangedMovementStateAndForgedValueFailClosed()
    {
        var state = State();
        var conclusion = await Conclusion("smc-revealed", "revealedEnemySmc-after-election");
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Stale,
            ScenarioA1SecondDefenderReturnTransition.Evaluate(state,
                new ScenarioA1ReturnAttempt("attempt-1", 9, conclusion)).Status);
        foreach (var changed in new[]
        {
            state with { UnitLocationId = "bd01:D4:0" },
            state with { ObservationVersion = "snapshot-2" },
            state with { Stage = ScenarioA1ReturnStage.Returned },
            state with { IsBerserk = true },
            state with { OvrEntryResolved = true },
            state with { IsMovementPhase = false },
            state with { AttemptedEntryMfRecorded = null },
            state with { RemainingMf = 1 },
        })
        {
            var result = ScenarioA1SecondDefenderReturnTransition.Evaluate(changed,
                new ScenarioA1ReturnAttempt("attempt-1", 10, conclusion));
            Assert.NotEqual(ScenarioA1ReturnTransitionStatus.Applied, result.Status);
            Assert.Same(changed, result.State);
        }

        var forgedValue = JsonSerializer.SerializeToElement(new
        {
            caseId = "A1-second-defender-consequence-smc-revealed",
            returnToLocationId = "bd01:D5:0", attemptedEntryMf = 2,
            mfExpenditureLocationId = "bd01:D5:0", additionalOvrMfResolved = false,
            defensiveAttackResolved = false, responseOrCcResolved = false, executed = false,
        });
        var forged = new DomainConclusion(conclusion.ConclusionId, conclusion.Question,
            conclusion.Disposition, forgedValue, conclusion.Evidence, conclusion.ApplicableRules,
            conclusion.ControllingExceptions, conclusion.Assumptions, conclusion.Ambiguities,
            conclusion.ReasonCodes, conclusion.Explanation, conclusion.ExplanationProvenance,
            conclusion.ConcludedAt);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Denied,
            ScenarioA1SecondDefenderReturnTransition.Evaluate(state,
                new ScenarioA1ReturnAttempt("attempt-1", 10, forged)).Status);
    }

    internal static ScenarioA1ReturnState State() => new(
        Tenant, "game-1", "squad", 10, "snapshot-1", "bd01:E4:0",
        "bd01:E4:0", "bd01:D4:0", 4)
    {
        Stage = ScenarioA1ReturnStage.PendingA1215Return,
        ReturnHazard = ScenarioA1ReturnHazard.Clear,
        IsMovementPhase = true,
        AttemptedEntryMfRecorded = 2,
    };

    internal static async Task<DomainConclusion> Conclusion(string suffix, string reveal)
    {
        var caseId = "A1-second-defender-consequence-" + suffix;
        var package = ScenarioA1SecondDefenderConsequencePackage.Identity;
        var descriptor = (await new ScenarioA1SecondDefenderConsequencePackage()
            .ResolveAsync(package)).Package!;
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building", ["phase"] = "mph",
            ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass",
            ["initialDefenderState"] = "concealed", ["firstReveal"] = "enemySmc-under-A12.15",
            ["overrunElection"] = "elected-after-first-reveal",
            ["previousLocation"] = "knownLastOccupiedLocation",
            ["previousLocationId"] = "bd01:D4:0",
            ["attemptedEntryMf"] = "ordinaryBuildingTwoMf",
            ["a414ExceptionOtherThanInfantryOvr"] = "none",
            ["specialModifier"] = "none", ["leaderExemption"] = "none",
            ["secondReveal"] = reveal, ["attackerCapability"] = "passedNtcAndAtLeastFourMf",
        };
        var observation = new Observation("observation", new ObservationSource("supplied"),
            Tenant, Now, JsonSerializer.SerializeToElement(facts), "bd01:E4:0",
            "snapshot-1", "supplied", package);
        var question = new DomainQuestion("return", Tenant, package,
            new SemanticIdentifier(package.DomainId,
                ScenarioA1SecondDefenderConsequenceConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0", previousLocationId = "bd01:D4:0",
                observationVersion = "snapshot-1", caseId,
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, package,
                new SemanticIdentifier(package.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(package.DomainId, id),
                descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant,
                    package, id, package.Version, "test"))], ["test.resolution"]);
        return await new ScenarioA1SecondDefenderConsequenceConclusionResolver()
            .ConcludeAsync(new DomainConclusionContext(question, descriptor,
                [Entity("squad"), Entity("bd01:E4:0"), Entity("bd01:D4:0")], [observation]));
    }
}
