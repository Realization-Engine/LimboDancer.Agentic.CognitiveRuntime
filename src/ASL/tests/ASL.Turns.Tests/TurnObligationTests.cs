using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnObligationTests
{
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();
    private static TurnObligationService Service => new(Catalog);
    private static TurnObligationRead Read => new("game", 20, new(1, 1, "side-a", "ccph", TurnCheckpoint.OrdinaryStatusCleanup));
    private static TurnObligationCandidate Candidate(string definition = "stun", string subject = "unit-1") => new(
        new("game", "origin", definition, subject, 0), 2, "side-a", "side-b", new(1, 1, "side-a", "mph", TurnCheckpoint.DuringPhase),
        new(true, true, false, false, true, false, false, true), [], null, []);

    [Fact]
    public void EveryDefinitionRemainsVisibleWithOrWithoutInstances()
    {
        var result = Service.Assess(Read, []);
        Assert.Equal(38, result.Definitions.Length);
        Assert.Empty(result.Obligations);
        var pending = Assert.Single(Service.Assess(Read, [Candidate("pack")]).Obligations);
        Assert.Equal(TurnSupport.Pending, pending.TimingSupport);
        Assert.Equal(TurnSupport.Pending, pending.CommandSupport);
        Assert.Equal(TurnObligationState.NotEvaluated, pending.Verdict.State);
    }

    [Theory]
    [InlineData("stun")]
    [InlineData("cx")]
    [InlineData("tripFlare")]
    [InlineData("clearance")]
    public void TimingCallsRulesButDoesNotClaimCommandIntegration(string definition)
    {
        var item = Assert.Single(Service.Assess(Read, [Candidate(definition)]).Obligations);
        Assert.Equal(TurnSupport.Partial, item.TimingSupport);
        Assert.Equal(TurnSupport.Pending, item.CommandSupport);
        Assert.NotEmpty(item.Verdict.Reference);
        Assert.NotNull(item.Verdict.Deadline);
        Assert.Equal("side-a", item.Facts.OwnerSideId);
        Assert.Equal("side-b", item.Facts.DecisionOwnerSideId);
    }

    [Fact]
    public void ReplayRetryAndMarkerRemovalKeepIdentityAndResolutionEvidence()
    {
        var origin = Candidate("cx");
        var resolved = origin with
        {
            Outcome = new(TurnRecordedOutcome.Resolved, "cx-release", 10),
            Subject = origin.Subject with
            {
                Cx = false
            }
        };
        var serialized = JsonSerializer.Serialize(resolved);
        var loaded = JsonSerializer.Deserialize<TurnObligationCandidate>(serialized)!;
        var first = Service.Assess(Read, [resolved]);
        var again = Service.Assess(Read, [loaded]);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(again));
        var item = Assert.Single(again.Obligations);
        Assert.Equal(origin.Key, item.Facts.Key);
        Assert.Equal(TurnObligationState.Resolved, item.Verdict.State);
        Assert.Equal("cx-release", item.Facts.Outcome!.EventId);
        Assert.Equal(serialized, JsonSerializer.Serialize(resolved));
        var missingEvidence = resolved with
        {
            Outcome = null
        };
        Assert.Equal(TurnObligationState.NotEvaluated, Assert.Single(Service.Assess(Read, [missingEvidence]).Obligations).Verdict.State);
    }

    [Fact]
    public void NestedEffectsPrecedeParentsAndCancellationDoesNotPretendCompletion()
    {
        var child = Candidate("clearance");
        var parent = Candidate("stun") with
        {
            Prerequisites = [child.Key]
        };
        var unresolved = Service.Assess(Read, [parent, child]);
        Assert.Equal(child.Key, unresolved.Obligations[0].Facts.Key);
        Assert.True(unresolved.Obligations[1].WaitingForPrerequisites);
        Assert.Equal(TurnObligationState.Due, unresolved.Obligations[1].Verdict.State);
        var resolved = child with
        {
            Outcome = new(TurnRecordedOutcome.Resolved, "effect", 10)
        };
        var result = Service.Assess(Read, [parent, resolved]);
        Assert.False(result.Obligations[1].WaitingForPrerequisites);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(Service.Assess(Read, [resolved, parent])));
        var cancelled = child with
        {
            Outcome = new(TurnRecordedOutcome.Cancelled, "cancel", 10)
        };
        Assert.True(Service.Assess(Read, [cancelled, parent]).Obligations[1].WaitingForPrerequisites);
    }

    [Fact]
    public void ProgressIsEvidenceNotANumberOfReadsOrPlayerTurns()
    {
        var item = Candidate("clearance") with
        {
            Progress = [new(1, "interval-one", 5)],
            Outcome = new(TurnRecordedOutcome.Interrupted, "interrupt", 10)
        };
        var later = Read with
        {
            Position = new(2, 3, "side-a", "rph", TurnCheckpoint.DuringPhase)
        };
        var result = Assert.Single(Service.Assess(later, [item]).Obligations);
        Assert.Single(result.Facts.Progress);
        Assert.Equal(TurnObligationState.Interrupted, result.Verdict.State);
        Assert.Equal(1, result.Facts.Progress[0].CompletedGameTurn);
        Assert.Equal("interval-one", result.Facts.Progress[0].EventId);
        var otherPlayerTurn = Read with
        {
            Position = new(1, 2, "side-b", "rph", TurnCheckpoint.DuringPhase)
        };
        Assert.Single(Assert.Single(Service.Assess(otherPlayerTurn, [item]).Obligations).Facts.Progress);
        Assert.Single(Assert.Single(Service.Assess(later, [item]).Obligations).Facts.Progress);
    }

    [Fact]
    public void MultipleInstancesAndMissingLegacyOriginsDoNotCollapseOrInventHistory()
    {
        var first = Candidate();
        var second = Candidate(subject: "unit-2") with
        {
            Origin = null
        };
        var third = first with
        {
            Key = first.Key with
            {
                Occurrence = 1
            }
        };
        var result = Service.Assess(Read, [second, third, first]);
        Assert.Equal(3, result.Obligations.Length);
        Assert.Equal(TurnObligationState.NotEvaluated, result.Obligations.Single(item => item.Facts.Key == second.Key).Verdict.State);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(Service.Assess(Read, [first, second, third])));
    }

    [Theory]
    [InlineData("game")]
    [InlineData("definition")]
    [InlineData("early-origin")]
    [InlineData("future-origin")]
    [InlineData("progress-default")]
    [InlineData("prerequisites-default")]
    [InlineData("duplicate-interval")]
    [InlineData("early-interval")]
    [InlineData("future-interval")]
    [InlineData("early-progress-sequence")]
    [InlineData("future-progress-sequence")]
    [InlineData("invalid-outcome")]
    [InlineData("empty-outcome")]
    [InlineData("early-outcome")]
    [InlineData("future-outcome")]
    public void InvalidHistoryCannotProduceAnObligation(string fault)
    {
        var item = Candidate();
        item = fault switch
        {
            "game" => item with { Key = item.Key with { GameId = "other" } },
            "definition" => item with { Key = item.Key with { DefinitionId = "unknown" } },
            "early-origin" => item with { OriginSequence = -1 },
            "future-origin" => item with { OriginSequence = 21 },
            "progress-default" => item with { Progress = default },
            "prerequisites-default" => item with { Prerequisites = default },
            "duplicate-interval" => item with { Progress = [new(1, "one", 5), new(1, "two", 6)] },
            "early-interval" => item with { Progress = [new(0, "one", 5)] },
            "future-interval" => item with { Progress = [new(2, "one", 5)] },
            "early-progress-sequence" => item with { Progress = [new(1, "one", 1)] },
            "future-progress-sequence" => item with { Progress = [new(1, "one", 21)] },
            "invalid-outcome" => item with { Outcome = new((TurnRecordedOutcome)999, "one", 5) },
            "empty-outcome" => item with { Outcome = new(TurnRecordedOutcome.None, "one", 5) },
            "early-outcome" => item with { Outcome = new(TurnRecordedOutcome.Resolved, "one", 1) },
            _ => item with { Outcome = new(TurnRecordedOutcome.Resolved, "one", 21) },
        };
        Assert.Throws<InvalidDataException>(() => Service.Assess(Read, [item]));
    }

    [Fact]
    public void GraphRequiresUniqueOriginsKnownPrerequisitesAndNoCycles()
    {
        var first = Candidate();
        var second = Candidate("clearance") with
        {
            Prerequisites = [first.Key]
        };
        Assert.Throws<ArgumentException>(() => Service.Assess(Read, [first, first]));
        Assert.Throws<InvalidDataException>(() => Service.Assess(Read, [second]));
        Assert.Throws<InvalidDataException>(() => Service.Assess(Read, [first with { Prerequisites = [second.Key] }, second]));
    }

    [Fact]
    public void RequiredInputAndEvidenceIdentitiesCannotBeBlank()
    {
        Assert.Throws<ArgumentNullException>(() => new TurnObligationService(null!));
        Assert.Throws<ArgumentNullException>(() => Service.Assess(null!, []));
        Assert.Throws<ArgumentNullException>(() => Service.Assess(Read, null!));
        Assert.Throws<ArgumentException>(() => Service.Assess(Read with { GameId = " " }, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.Assess(Read with { AsOfSequence = -1 }, []));
        Assert.Throws<ArgumentException>(() => Service.Assess(Read, [Candidate() with { Outcome = new(TurnRecordedOutcome.Resolved, " ", 10) }]));
        Assert.Throws<ArgumentException>(() => Service.Assess(Read, [Candidate() with { Progress = [new(1, " ", 10)] }]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.Assess(Read, [Candidate() with { Key = Candidate().Key with { Occurrence = -1 } }]));
    }
}
