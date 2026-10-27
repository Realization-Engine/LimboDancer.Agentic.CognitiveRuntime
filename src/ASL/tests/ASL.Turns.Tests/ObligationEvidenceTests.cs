using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class ObligationEvidenceTests
{
    private static TurnObligationCandidate Candidate(string id) => new(new("game", id, "stun", "unit", 0), 1, "a", "a",
        new(1, 1, "a", "mph", TurnCheckpoint.DuringPhase), new(true, true, false, false, false, false, false, null), [], null, []);
    private static TurnObligationRead Read => new("game", 20, new(2, 3, "a", "rph", TurnCheckpoint.DuringPhase));

    [Fact]
    public void ParentResolutionRequiresEarlierResolvedChildren()
    {
        var service = new TurnObligationService(TurnGuideCatalog.Load());
        var child = Candidate("child");
        var parent = Candidate("parent") with
        {
            Prerequisites = [child.Key],
            Outcome = new(TurnRecordedOutcome.Resolved, "parent-result", 10)
        };
        Assert.Throws<InvalidDataException>(() => service.Assess(Read, [child, parent]));
        var late = child with
        {
            Outcome = new(TurnRecordedOutcome.Resolved, "child-result", 11)
        };
        Assert.Throws<InvalidDataException>(() => service.Assess(Read, [late, parent]));
        var early = child with
        {
            Outcome = new(TurnRecordedOutcome.Resolved, "child-result", 9)
        };
        var result = service.Assess(Read, [parent, early]);
        Assert.All(result.Obligations, item => Assert.Equal(TurnObligationState.Resolved, item.Verdict.State));
        Assert.All(result.Obligations, item => Assert.False(item.WaitingForPrerequisites));
    }

    [Fact]
    public void ProgressCannotPrecedeOriginOrFollowTerminalEvidence()
    {
        var service = new TurnObligationService(TurnGuideCatalog.Load());
        var beforeOrigin = Candidate("one") with
        {
            Origin = new(2, 3, "a", "rph", TurnCheckpoint.DuringPhase),
            Progress = [new(1, "progress", 5)]
        };
        Assert.Throws<InvalidDataException>(() => service.Assess(Read, [beforeOrigin]));
        var legacy = Candidate("legacy") with
        {
            Origin = null,
            Progress = [new(1, "progress", 5)]
        };
        Assert.Equal(TurnObligationState.NotEvaluated, Assert.Single(service.Assess(Read, [legacy]).Obligations).Verdict.State);
        var afterResolution = Candidate("one") with
        {
            Progress = [new(1, "progress", 11)],
            Outcome = new(TurnRecordedOutcome.Resolved, "result", 10)
        };
        Assert.Throws<InvalidDataException>(() => service.Assess(Read, [afterResolution]));
    }
}
