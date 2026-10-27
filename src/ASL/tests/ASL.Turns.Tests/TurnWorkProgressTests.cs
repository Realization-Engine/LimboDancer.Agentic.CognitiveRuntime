using System.Collections.Immutable;
using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnWorkProgressTests
{
    private static TurnWorkRead Read => new("game", 50, 5, TurnWorkWindow.SecondCloseCombat);
    private static TurnWorkInterval Interval => new(1, "first-rally", 2, "second-cc", 8, false);
    private static TurnWorkEvidence Work => new(new("game", "start", "ammo", "vehicle", 0), 1, 1,
        ["vehicle", "supply"], TurnWorkKind.AmmoReplenishment, 10, true, [Interval]);

    [Fact]
    public void CompleteGameTurnsSurviveInterruptedAndIncompleteIntervalsAndReload()
    {
        var work = Work with
        {
            Intervals = [Interval,
            new(2, "r2", 10, "c2", 18, true), new(3, "r3", 20, null, null, false)]
        };
        var result = TurnWorkProgress.Assess(Read, [work]);
        var item = Assert.Single(result.Items);
        Assert.Equal(work.Key, item.Key);
        Assert.Equal(work.Participants, item.Participants);
        Assert.Equal(1, item.CompletedGameTurns);
        Assert.Equal(2, Assert.Single(item.InterruptedGameTurns));
        Assert.Equal(3, Assert.Single(item.IncompleteGameTurns));
        Assert.Equal(2, item.Verdict.RequiredGameTurns);
        Assert.Equal(TurnWorkState.InProgress, item.Verdict.State);
        Assert.Equal(TurnSupport.Pending, item.CommandSupport);
        var restored = JsonSerializer.Deserialize<TurnWorkEvidence>(JsonSerializer.Serialize(work))!;
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(TurnWorkProgress.Assess(Read, [restored])));
        var completed = work with
        {
            Intervals = [.. work.Intervals, new(4, "r4", 30, "c4", 38, false)]
        };
        var due = Assert.Single(TurnWorkProgress.Assess(Read, [completed]).Items);
        Assert.Equal(2, due.CompletedGameTurns);
        Assert.Equal(TurnWorkState.CompletionPending, due.Verdict.State);
        Assert.Equal(TurnWorkState.InProgress, Assert.Single(TurnWorkProgress.Assess(Read with
        {
            Window = TurnWorkWindow.FirstRally
        }, [completed]).Items).Verdict.State);
        Assert.Equal(TurnWorkState.Interrupted, Assert.Single(TurnWorkProgress.Assess(Read, [completed with { Eligible = false }]).Items).Verdict.State);
    }

    [Fact]
    public void MissingPackingOriginDurationAndPartialBoundariesNeverInventCompletion()
    {
        var work = Work with
        {
            Kind = TurnWorkKind.AnimalPacking,
            FirstGameTurn = null,
            Eligible = null,
            Intervals = [new(1, null, null, "end", 8, false)]
        };
        var result = TurnWorkProgress.Assess(Read, [work]);
        Assert.Equal(0, Assert.Single(result.Items).CompletedGameTurns);
        Assert.Equal(TurnWorkState.NotEvaluated, result.Items[0].Verdict.State);
        Assert.Null(result.Items[0].Verdict.RequiredGameTurns);
        Assert.Equal(5, result.PendingRules.Length);
        Assert.Empty(TurnWorkProgress.Assess(Read, []).Items);
    }

    [Fact]
    public void ReusedReceiptIdsAndReversedPartialIntervalsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => TurnWorkProgress.Assess(Read, [Work with { Key = Work.Key with { OriginEventId = "" } }]));
        Assert.Throws<ArgumentException>(() => TurnWorkProgress.Assess(Read, [Work with { Key = Work.Key with { SubjectId = "" } }]));
        Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [Work with
        {
            Intervals = [Interval, new(2, "first-rally", 10, "end2", 18, false)]
        }]));
        Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [Work with
        {
            Intervals = [new(1, "start1", 20, null, null, false), new(2, "start2", 10, "end2", 30, false)]
        }]));
        var unknown = TurnWorkProgress.Assess(Read, [Work with { Intervals = [new(1, null, null, null, null, false)] }]);
        Assert.Equal(0, unknown.Items[0].CompletedGameTurns);
        Assert.Equal(1, Assert.Single(unknown.Items[0].IncompleteGameTurns));
    }

    [Fact]
    public void InvalidReadAndOriginsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TurnWorkProgress.Assess(null!, []));
        Assert.Throws<ArgumentException>(() => TurnWorkProgress.Assess(Read with { GameId = "" }, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnWorkProgress.Assess(Read with { Sequence = -1 }, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnWorkProgress.Assess(Read with { GameTurn = 0 }, []));
        Assert.Throws<ArgumentException>(() => TurnWorkProgress.Assess(Read, default));
        Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [Work, Work]));
        TurnWorkEvidence[] invalid = [Work with { Key = Work.Key with { GameId = "other" } },
            Work with { OriginSequence = -1 }, Work with { OriginSequence = 51 }, Work with { FirstGameTurn = 0 },
            Work with { FirstGameTurn = 6 }, Work with { Participants = default }, Work with { Participants = [] },
            Work with { Participants = [" "] }, Work with { Intervals = default }];
        foreach (var work in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [work]));
        }
    }

    [Fact]
    public void InvalidAndOverlappingReceiptsCannotIncreaseProgress()
    {
        ImmutableArray<TurnWorkInterval>[] invalid = [[Interval, Interval], [Interval with { GameTurn = 0 }],
            [Interval with { GameTurn = 6 }], [Interval with { FirstRallyEvent = null }],
            [Interval with { FirstRallySequence = null }], [Interval with { FirstRallyEvent = " " }],
            [Interval with { FirstRallySequence = 0 }], [Interval with { SecondCloseCombatSequence = 51 }],
            [Interval with { SecondCloseCombatSequence = 1 }], [Interval with { SecondCloseCombatSequence = 2 }],
            [Interval, new(2, "r2", 7, "c2", 18, false)], [Interval, new(2, null, null, "c2", 7, false)]];
        foreach (var intervals in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [Work with { Intervals = intervals }]));
        }

        Assert.Throws<InvalidDataException>(() => TurnWorkProgress.Assess(Read, [Work with { FirstGameTurn = 2 }]));
    }
}
