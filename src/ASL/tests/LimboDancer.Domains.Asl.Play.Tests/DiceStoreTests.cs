using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// System dice in the game store (Random Selection and Declined OVR Design, section 3; DICE-08, DICE-10, DICE-11): the
/// roll is drawn once, inside the commit, after the attempt and revision checks, and a draw whose events are not
/// written leaves no trace.
/// </summary>
public sealed class DiceStoreTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c309");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-dice-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public DiceStoreTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    /// <summary>A roller built on the Dice library's internal seam: each draw returns the next value, counting the draws.</summary>
    private sealed class Counting(params int[] values)
    {
        public int Draws
        {
            get; private set;
        }

        public DiceRoller Roller => new(sides =>
        {
            var value = values[Draws % values.Length] - 1;
            Draws++;
            return value;
        });
    }

    private static DiceRoller Throwing() => new(_ => throw new InvalidOperationException("The roller was called."));

    private static PlannedRoll Roll(string attempt, long expected, string? firstId = null) =>
        new("random-selection", draw =>
        {
            var result = draw(new RollRequest(2, 6));
            return
            [
                new GameEvent(Scope, firstId ?? attempt + "-1", expected + 1, DateTimeOffset.UnixEpoch, LiveGames.Source, "dice-rolled",
                    new DiceRolled(attempt + "-roll-1", "random-selection", result.Request.Count, result.Request.Sides, result.Values, DiceRolled.SystemSource, "player"),
                    null, [], null),
            ];
        });

    /// <summary>A batch that draws a second roll only when the first totals at least <paramref name="threshold"/>.</summary>
    private static PlannedRoll TwoRolls(string attempt, long expected, int threshold) =>
        new("test", draw =>
        {
            var first = draw(new RollRequest(2, 6));
            List<GameEvent> events =
            [
                new(Scope, attempt + "-1", expected + 1, DateTimeOffset.UnixEpoch, LiveGames.Source, "dice-rolled",
                    new DiceRolled(attempt + "-roll-1", "test", 2, 6, first.Values, DiceRolled.SystemSource, "player"), null, [], null),
            ];
            if (first.Values.Sum() >= threshold)
            {
                var second = draw(new RollRequest(1, 6));
                events.Add(new(Scope, attempt + "-2", expected + 2, DateTimeOffset.UnixEpoch, LiveGames.Source, "dice-rolled",
                    new DiceRolled(attempt + "-roll-2", "test", 1, 6, second.Values, DiceRolled.SystemSource, "player"), null, [], null));
            }

            return events;
        });

    private AppendResult Append(string attempt, long expected, DiceRoller roller, string? firstId = null) =>
        store.AppendRolled(Scope, "Village test", expected, attempt + "-1", Roll(attempt, expected, firstId), roller, Planner().Replay);

    private async Task SetUp()
    {
        var play = new GamePlay(Planner(), store, new NullAudit());
        var setup = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Village test",
                catalog = "asl-scenario-a1@1.1.0",
                boards = Bd01,
                firstSide = "german",
                sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
            },
            placements = new[]
            {
                new
                {
                    id = "g1", kind = "asl:squad", definition = "attacker-squad", side = "german", position = new { at = "bd01:D4:0" },
                    conditions = new Dictionary<string, bool>
                    {
                        ["asl:broken"] = false, ["asl:berserk"] = false, ["asl:captured"] = false, ["asl:melee"] = false,
                        ["asl:ti"] = false, ["asl:disrupted"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false,
                    },
                },
            },
        });
        var proposed = await play.ProposeAsync(GameActions.Setup, setup, Player);
        Assert.Equal(PlayOutcome.Committed, (await play.ConfirmAsync(GameActions.Setup, setup, Player, proposed.Correlation)).Outcome);
    }

    [Fact]
    public async Task ARollIsDrawnOnceInsideTheCommitAndRecorded()
    {
        await SetUp();
        var counting = new Counting(3, 6);
        var result = Append("roll", Revision, counting.Roller);
        Assert.Equal(AppendStatus.Committed, result.Status);
        Assert.Equal(2, counting.Draws);
        var recorded = Assert.IsType<DiceRolled>(store.Read(Scope)!.Events[^1].Payload);
        Assert.Equal([3, 6], recorded.Values);

        // Replaying the game reads the recorded values; nothing draws.
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ACommittedAttemptIsAReplayAndDrawsNothing()
    {
        await SetUp();
        var expected = Revision;
        Assert.Equal(AppendStatus.Committed, Append("roll", expected, new Counting(2, 5).Roller).Status);
        var again = Append("roll", expected, Throwing());
        Assert.Equal(AppendStatus.Replay, again.Status);
        Assert.Equal([2, 5], Assert.IsType<DiceRolled>(store.Read(Scope)!.Events[^1].Payload).Values);
    }

    [Theory]
    [InlineData(6, 6, 1, 3)]
    [InlineData(1, 2, 0, 2)]
    public async Task RollsAreDrawnOnDemandAndOnlyForTheBranchTaken(int first, int second, int extraRolls, int draws)
    {
        // Rolls on demand (Infantry OVR Design, section 3): the second roll is drawn only when the first calls for it.
        await SetUp();
        var before = Revision;
        var counting = new Counting(first, second, 4);
        Assert.Equal(AppendStatus.Committed, store.AppendRolled(Scope, "Village test", before, "pair-1", TwoRolls("pair", before, 7), counting.Roller,
            Planner().Replay).Status);
        Assert.Equal(draws, counting.Draws);
        Assert.Equal(1 + extraRolls, store.Read(Scope)!.Events.Skip((int)before).Count(item => item.Payload is DiceRolled));
        Assert.Equal(AppendStatus.Replay, store.AppendRolled(Scope, "Village test", before, "pair-1", TwoRolls("pair", before, 7), Throwing(),
            Planner().Replay).Status);
    }

    [Fact]
    public async Task AStaleRevisionDrawsNothing()
    {
        await SetUp();
        Assert.Equal(AppendStatus.Stale, Append("roll", Revision + 1, Throwing()).Status);
    }

    [Fact]
    public async Task AGeneratorFailureWritesNothingAndARetryCanDrawAgain()
    {
        await SetUp();
        var before = Revision;
        Assert.Throws<InvalidOperationException>(() => Append("roll", before, Throwing()));
        Assert.Equal(before, Revision);
        Assert.Equal(AppendStatus.Committed, Append("roll", before, new Counting(4, 4).Roller).Status);
    }

    [Fact]
    public async Task EventsThatDoNotReplayOrDoNotStartTheAttemptAreNotWritten()
    {
        await SetUp();
        var before = Revision;
        Assert.Equal(AppendStatus.Invalid, Append("roll", before, new Counting(1, 1).Roller, firstId: "other-1").Status);
        var tooHigh = store.AppendRolled(Scope, "Village test", before, "bad-1", new PlannedRoll("random-selection", draw =>
            {
                draw(new RollRequest(2, 6));
                return
                [
                    new GameEvent(Scope, "bad-1", before + 1, DateTimeOffset.UnixEpoch, LiveGames.Source, "dice-rolled",
                        new DiceRolled("bad-roll-1", "random-selection", 2, 6, [7, 1], DiceRolled.SystemSource, "player"), null, [], null),
                ];
            }), new Counting(1, 1).Roller, Planner().Replay);
        Assert.Equal(AppendStatus.Invalid, tooHigh.Status);
        Assert.Equal(before, Revision);
    }

    [Fact]
    public async Task ConcurrentConfirmationsOfOneAttemptCommitOneRoll()
    {
        await SetUp();
        var expected = Revision;
        var counting = new Counting(6, 1);
        var roller = counting.Roller;
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => Append("roll", expected, roller))));
        Assert.Single(results, result => result.Status == AppendStatus.Committed);
        Assert.All(results.Where(result => result.Status != AppendStatus.Committed), result => Assert.Equal(AppendStatus.Replay, result.Status));
        Assert.Equal(2, counting.Draws);
        Assert.Equal(expected + 1, Revision);
    }

    [Fact]
    public async Task AnAttemptReusedWithOtherInputsIsRefused()
    {
        // DICE-10: the same attempt id for another target is not a replay.
        await SetUp();
        var play = new GamePlay(Planner(), store, new NullAudit());
        foreach (var attempt in new[] { "advance-1", "advance-2" })
        {
            var advance = JsonSerializer.SerializeToElement(new
            {
                gameId = Scope.Game,
                attemptId = attempt,
                expectedRevision = Revision
            });
            var proposed = await play.ProposeAsync(GameActions.AdvancePhase, advance, Player);
            Assert.Equal(PlayOutcome.Committed, (await play.ConfirmAsync(GameActions.AdvancePhase, advance, Player, proposed.Correlation)).Outcome);
        }

        JsonElement Entry(string location, long expected) =>
            JsonSerializer.SerializeToElement(new
            {
                gameId = Scope.Game,
                attemptId = "enter-1",
                expectedRevision = expected,
                unitId = "g1",
                location
            });
        var start = Revision;
        var entry = await play.ProposeAsync(GameActions.EnterBuilding, Entry("bd01:E4:0", start), Player);
        Assert.Equal(PlayOutcome.Committed, (await play.ConfirmAsync(GameActions.EnterBuilding, Entry("bd01:E4:0", start), Player, entry.Correlation)).Outcome);

        var same = await Planner().PlanAsync(GameActions.EnterBuilding, Entry("bd01:E4:0", start), Tenant);
        Assert.Equal(GamePlanStatus.Replay, same.Status);
        var other = await Planner().PlanAsync(GameActions.EnterBuilding, Entry("bd01:E5:0", start), Tenant);
        Assert.Equal(GamePlanStatus.Refused, other.Status);
        Assert.StartsWith("play.attempt-reused", Assert.Single(other.Reasons), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
