using System.Collections.Immutable;
using System.Text.Json;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnFireHistoryTests
{
    private static TurnFireRecord Shot(string? spotter = "s1") => new(TurnFireKind.Ordnance, 1, "a", "pfph", ["u1"], "m1", spotter, true, false);
    private static TurnSpotterRead Read => new("m1", "s1", true, true, true, false, false, false);

    [Fact]
    public void HistoryRetainsShotsAndOriginWithoutTurningRofIntoAnAction()
    {
        var shots = ImmutableArray.Create(Shot(), Shot() with
        {
            Phase = "dfph",
            InCurrentPhase = true,
            RateOfFireKept = false
        });
        var result = TurnFireHistory.Compose(shots, [Read with { SpottedThisPhase = true }]);
        Assert.Equal(shots, result.Shots);
        var spotter = Assert.Single(result.Spotters);
        Assert.Equal("m1", spotter.Weapon);
        Assert.Equal("s1", spotter.Spotter);
        Assert.Equal(1, spotter.FirstObservedGameTurn);
        Assert.Equal("pfph", spotter.FirstObservedPhase);
        Assert.Equal(TurnSpotterState.Retained, spotter.State);
        Assert.True(spotter.SpottedThisPhase);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(TurnFireHistory.Compose(shots, [Read with { SpottedThisPhase = true }])));
        Assert.NotEmpty(result.EvaluationGaps);
    }

    [Fact]
    public void MissingObservationsAreUnknownAndReplacementPreservesPriorHistory()
    {
        Assert.Equal(TurnSpotterState.NotEvaluated, Assert.Single(TurnFireHistory.Compose([Shot()], []).Spotters).State);
        var unknown = Assert.Single(TurnFireHistory.Compose([Shot()], [Read with { Known = false }]).Spotters);
        Assert.Equal(TurnSpotterState.NotEvaluated, unknown.State);
        Assert.Null(unknown.SpottedThisPhase);
        var lost = Assert.Single(TurnFireHistory.Compose([Shot()], [Read with { Broken = true }]).Spotters);
        Assert.Equal(TurnSpotterState.Lost, lost.State);
        var rallied = Assert.Single(TurnFireHistory.Compose([Shot()], [Read with { RecordedLoss = true }]).Spotters);
        Assert.Equal(TurnSpotterState.Lost, rallied.State);
        Assert.Equal(TurnSpotterState.NotEvaluated, Assert.Single(TurnFireHistory.Compose([Shot()],
            [Read with { Known = false, RecordedLoss = true }]).Spotters).State);
        var replaced = TurnFireHistory.Compose([Shot(), Shot("s2")], [Read, Read with { Spotter = "s2" }]);
        Assert.Equal(TurnSpotterState.Superseded, replaced.Spotters[0].State);
        Assert.Null(replaced.Spotters[0].SpottedThisPhase);
        Assert.Equal(TurnSpotterState.Retained, replaced.Spotters[1].State);
    }

    [Fact]
    public void DisclosedSuccessorKeepsTheOriginalObservedAssignment()
    {
        var read = Read with
        {
            Successor = "hs",
            SpottedThisPhase = true
        };
        var history = TurnFireHistory.Compose([Shot(), Shot("hs")], [read, Read with { Spotter = "hs" }]);
        var retained = Assert.Single(history.Spotters);
        Assert.Equal("s1", retained.Spotter);
        Assert.Equal("hs", retained.Successor);
        Assert.Equal(TurnSpotterState.Retained, retained.State);
        Assert.True(retained.SpottedThisPhase);
        var hidden = Assert.Single(TurnFireHistory.Compose([Shot()], [read with { Known = false }]).Spotters);
        Assert.Equal(TurnSpotterState.NotEvaluated, hidden.State);
        Assert.Null(hidden.Successor);
    }

    [Fact]
    public void DirectFireAndUnspottedOrIncompleteShotsNeverInventAssignments()
    {
        var shots = ImmutableArray.Create(Shot() with
        {
            Kind = TurnFireKind.Fire
        }, Shot() with
        {
            Weapon = null
        }, Shot(null));
        var result = TurnFireHistory.Compose(shots, []);
        Assert.Equal(3, result.Shots.Length);
        Assert.Empty(result.Spotters);
        Assert.Empty(TurnFireHistory.Compose([], []).Shots);
        Assert.Throws<ArgumentException>(() => TurnFireHistory.Compose(default, []));
        Assert.Throws<ArgumentException>(() => TurnFireHistory.Compose([], default));
        Assert.Throws<ArgumentException>(() => TurnFireHistory.Compose([], [Read, Read]));
    }
}
