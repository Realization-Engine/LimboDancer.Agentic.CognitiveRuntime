using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Fire in live play (unit step 18; U19, U20): two Russian squads and the 8-0 in D4 Prep Fire at a German squad and
/// half-squad in the stone building E4, with fixed rolls from the Dice seam and a stub LOS reader, since the board
/// fixture has hex facts but no LOS data.
/// </summary>
public sealed partial class FireTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f118");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] FireGroup = ["r1", "r2", "rl"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-fire-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public FireTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubLos : IFireLosReader
    {
        public LosResult Result { get; set; } = new(LosStatus.Clear, false, 1, 0, null, string.Empty);

        /// <summary>A LOS for each pair of Locations, for a test that needs some units to see and others not; the one result otherwise.</summary>
        public Func<BoardLocation, BoardLocation, LosResult?>? By
        {
            get; set;
        }

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => By is { } by ? by(from, target) : Result;
    }

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog], fireLos: los);

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static object Placement(string id, string kind, string definition, string at, string side, bool concealed = false, bool hidden = false) => new
    {
        id,
        kind,
        definition,
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = concealed, ["asl:hidden"] = hidden },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        if (proposed.Outcome != PlayOutcome.NeedsConfirmation)
        {
            return proposed;
        }

        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game in the Russian PFPh: r1, r2, and the 8-0 rl in D4; the German g1 and gh in the stone building E4.</summary>
    private async Task Setup(int? germanElr = 3, string[]? concealed = null, string[]? hidden = null)
    {
        object Place(string id, string kind, string definition, string at, string side) =>
            Placement(id, kind, definition, at, side, concealed?.Contains(id) == true, hidden?.Contains(id) == true);

        var play = Play();
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Village fire",
                catalog = "asl-scenario-a1@1.13.0",
                boards = Bd01,
                firstSide = "russian",
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = germanElr }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements = new[]
            {
                Place("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian"),
                Place("r2", "asl:squad", "defender-squad", "bd01:D4:0", "russian"),
                Place("r3", "asl:squad", "defender-squad", "bd01:D4:0", "russian"),
                Place("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"),
                Place("g1", "asl:squad", "attacker-squad", "bd01:E4:0", "german"),
                Place("gh", "asl:half-squad", "attacker-half-squad", "bd01:E4:0", "german"),
                Place("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
                Place("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
                Place("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            },
        }))).Outcome);
        await Advance();
        Assert.Equal("pfph", Current.Phase);
    }

    private async Task Advance()
    {
        var result = await Commit(Play(), GameActions.AdvancePhase, Args(new
        {
            gameId = Scope.Game,
            attemptId = $"advance-{Revision}",
            expectedRevision = Revision
        }));
        Assert.Equal(PlayOutcome.Committed, result.Outcome);
    }

    private JsonElement Fire(string attempt, string[] firers, string? director = "rl", string target = "bd01:E4:0", long? expected = null) =>
        director is null
            ? Args(new
            {
                gameId = Scope.Game,
                attemptId = attempt,
                expectedRevision = expected ?? Revision,
                firers,
                target
            })
            : Args(new
            {
                gameId = Scope.Game,
                attemptId = attempt,
                expectedRevision = expected ?? Revision,
                firers,
                director,
                target
            });

    [Fact]
    public async Task U19APrepFireAttackCommitsItsRollsArithmeticAndEffects()
    {
        await Setup();
        var before = Revision;

        // IFT 3+4 = 7; 16 FP (two squads, PBF); +3 stone building, +0 leadership: Final DR 10 on the 16 column is a NMC.
        // g1 rolls 2+3 and passes; gh rolls 4+4 = 8 against 7 and breaks, one within the German ELR of 3.
        var result = await Commit(Play(Once(3, 4, 2, 3, 4, 4)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.Equal(PlayOutcome.Committed, result.Outcome);

        var events = store.Read(Scope)!.Events.Skip((int)before).ToArray();
        Assert.Equal(["dice-rolled", "dice-rolled", "dice-rolled", "fire-resolved", "conditions-changed", "conditions-changed", "conditions-changed",
            "conditions-changed"], events.Select(item => item.Type));
        var fire = Assert.IsType<FireResolved>(events[3].Payload);
        var arithmetic = fire.Resolution.GetProperty("arithmetic");
        Assert.Equal((16, 7, 10, "NMC"), (arithmetic.GetProperty("columnFp").GetInt32(), arithmetic.GetProperty("originalDr").GetInt32(),
            arithmetic.GetProperty("finalDr").GetInt32(), arithmetic.GetProperty("result").GetString()));
        Assert.Equal("stone-building", fire.Facts.GetProperty("targetTerrain").GetString());

        var state = Current;
        Assert.Equal(ConditionState.True, GameState.Condition(state.Unit("gh")!, Conditions.Broken));
        Assert.NotEqual(ConditionState.True, GameState.Condition(state.Unit("g1")!, Conditions.Broken));
        Assert.All(FireGroup, id => Assert.Equal(ConditionState.True, GameState.Condition(state.Unit(id)!, Conditions.PrepFire)));

        // Replaying draws nothing, and confirming the same attempt again returns the recorded rolls.
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
        var again = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-1", ["r1", "r2"], expected: before));
        Assert.Equal(PlayOutcome.Replay, again.Outcome);
        Assert.Equal(before + events.Length, Revision);

        // The Prep Fire state is removed at the end of the AFPh (A3.5).
        foreach (var phase in new[] { "mph", "dfph", "afph" })
        {
            await Advance();
            Assert.Equal(phase, Current.Phase);
        }

        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("r1")!, Conditions.PrepFire));
        await Advance();
        Assert.False(Current.Unit("r1")!.Conditions.ContainsKey(Conditions.PrepFire));
    }

    [Fact]
    public async Task U20RefusalsComeBeforeAnyRollAndChangeNothing()
    {
        await Setup();
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(3, 4, 2, 3, 2, 3)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]))).Outcome);
        var revision = Revision;

        // A second attack from the same Location on the same target (A7.55), and fire by a unit already marked (A7.1).
        var group = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-2", ["r3"], director: null));
        Assert.Contains(group.Reasons, reason => reason.Contains("A7.55", StringComparison.Ordinal));
        var marked = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-3", ["r1"], director: null, target: "bd01:F4:0"));
        Assert.NotEqual(PlayOutcome.Committed, marked.Outcome);
        Assert.Equal(revision, Revision);

        // Fire in the MPh answers a moving stack's MF expenditure (A8.1); with none, it is refused.
        await Advance();
        var moving = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-4", ["r3"], director: null));
        Assert.Contains(moving.Reasons, reason => reason.Contains("fire-window", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CasualtyReductionRecordsTheHalfSquadThroughLineage()
    {
        await Setup();

        // IFT 1+3 = 4 in Open Ground, 16 FP: K/3. The Random Selection dr picks g2, the only target, which becomes a
        // half-squad (A7.302) and then passes its 3MC with 1+1+3 = 5.
        var result = await Commit(Play(Once(1, 3, 5, 1, 1, 6, 6)), GameActions.Fire, Fire("fire-k", ["r4", "r5"], director: null, target: "bd01:A2:0"));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        var lineage = Assert.Single(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<LineageRecorded>());
        Assert.Equal((LineageAction.Reduced, "g2"), (lineage.Action, lineage.Consumed[0]));
        var half = Current.Unit(lineage.Produced[0].Id)!;
        Assert.Equal(("asl:half-squad", "attacker-half-squad", InstanceStatus.Active), (half.Kind, half.Definition!.Definition, half.Status));
        Assert.Equal(InstanceStatus.Consumed, Current.Unit("g2")!.Status);
    }

    [Fact]
    public async Task TheStoreRefusesABatchThatWouldNotReadBack()
    {
        await Setup();
        var existing = store.Read(Scope)!.Events;
        var unreadable = existing[^1] with
        {
            EventId = "bad-1",
            Revision = existing.Count + 1,
            Type = "lineage-recorded",
            Payload = new ConditionsChanged("r1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
            Causes = [],
            Visibility = null,
        };
        var appended = store.Append(Scope, "Village fire", existing.Count, [unreadable], Planner().Replay);
        Assert.Equal(AppendStatus.Invalid, appended.Status);
        Assert.Equal(existing.Count, Revision);
    }

    [Fact]
    public async Task AnAttackThatCouldReachAnUndecidedOutcomeIsRefused()
    {
        await Setup(germanElr: null);
        var result = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains("elr-undeclared", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnUnansweredLosIsRefused()
    {
        await Setup();
        los.Result = new LosResult(LosStatus.Unsupported, null, 1, 0, null, "Bridge hexes in the depression rule (A6.3)");
        var result = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.Contains(result.Reasons, reason => reason.Contains("play.fire-los", StringComparison.Ordinal));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
