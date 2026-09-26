using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Random Selection before the PostReveal forced back, and a lone revealed SMC pending the attacker's OVR declaration
/// (Random Selection and Declined OVR Design, sections 4 and 5; U9), on board 01 as the oracle fixture reads it. The dice
/// come from the Dice library's internal seam, so every branch is reached on purpose.
/// </summary>
public sealed class RandomSelectionTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c310");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly Perspective German = Perspective.Side("german");
    private static readonly Perspective Russian = Perspective.Side("russian");
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-rs-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private int draws;

    public RandomSelectionTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    /// <summary>A roller that returns these values in turn, and counts its draws.</summary>
    private DiceRoller Fixed(params int[] values) => new(_ => values[draws++ % values.Length] - 1);

    private static DiceRoller Throwing() => new(_ => throw new InvalidOperationException("The roller was called."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? Throwing());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History() => Planner().Replay(store.Read(Scope)!.Events);

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static object Placement(string id, string definition, string at, bool concealed = false, bool hidden = false, string side = "russian") => new
    {
        id,
        kind = definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : "asl:squad",
        definition,
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, bool>
        {
            ["asl:broken"] = false,
            ["asl:berserk"] = false,
            ["asl:captured"] = false,
            ["asl:melee"] = false,
            ["asl:ti"] = false,
            ["asl:disrupted"] = false,
            ["asl:concealed"] = concealed,
            ["asl:hidden"] = hidden,
        },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>The German squad g1 in D4 and these Russian units, advanced to the German MPh.</summary>
    private async Task SetUp(params object[] defenders)
    {
        var play = Play();
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup, Args(new
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
            placements = new[] { Placement("g1", "attacker-squad", "bd01:D4:0", side: "german") }.Concat(defenders).ToArray(),
        }))).Outcome);
        for (var step = 0; step < 2; step++)
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase,
                Args(new
                {
                    gameId = Scope.Game,
                    attemptId = $"advance-{step}",
                    expectedRevision = Revision
                }))).Outcome);
        }
    }

    private JsonElement Entry(string attempt = "enter-1", long? expected = null) =>
        Args(new
        {
            gameId = Scope.Game,
            attemptId = attempt,
            expectedRevision = expected ?? Revision,
            unitId = "g1",
            location = "bd01:E4:0"
        });

    private async Task<(PlayResult Result, long Start)> Enter(DiceRoller roller)
    {
        var start = Revision;
        return (await Commit(Play(roller), GameActions.EnterBuilding, Entry(expected: start)), start);
    }

    private IReadOnlyList<GameEvent> Since(long start) => [.. store.Read(Scope)!.Events.Skip((int)start)];

    private GameState Current => History().Current!;

    private bool Known(string id) => GameState.Condition(Current.Unit(id)!, Conditions.Concealed) == ConditionState.False
        && GameState.Condition(Current.Unit(id)!, Conditions.Hidden) != ConditionState.True;

    [Fact]
    public async Task ARandomSelectionAmongTwoSquadsRevealsTheHighestAndForcesTheMoverBack()
    {
        // U9. The dice are in unit id order: r1 rolls 6 and r2 rolls 2, so r1 is revealed.
        await SetUp(Placement("r1", "defender-squad", "bd01:E4:0", concealed: true), Placement("r2", "defender-squad", "bd01:E4:0", concealed: true));
        var (result, start) = await Enter(Fixed(6, 2));
        Assert.Equal(PlayOutcome.Committed, result.Outcome);
        Assert.Equal(2, draws);

        var events = Since(start);
        Assert.Equal(["entry-attempted", "dice-rolled", "random-selection", "conditions-changed", "entry-forced-back"], events.Select(item => item.Type));
        var roll = Assert.IsType<DiceRolled>(events[1].Payload);
        Assert.Equal([6, 2], roll.Values);
        Assert.Equal(("random-selection", "player", "system"), (roll.Purpose, roll.Actor, roll.Source));
        Assert.Equal(["r1", "r2"], Assert.IsType<RandomSelection>(events[2].Payload).Subjects);
        Assert.Equal(["russian"], events[2].Visibility);

        Assert.True(Known("r1"));
        Assert.False(Known("r2"));
        Assert.Equal((2, true), (Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.MovementEnded));
        Assert.Empty(Current.OpenAttempts);

        // The attacker sees the roll and the revealed squad, but not which die was whose; the other squad stays sealed.
        var german = GameView.Of(History(), Revision, German);
        Assert.Contains(german.Events, item => item.Type == "dice-rolled");
        Assert.DoesNotContain(german.Events, item => item.Type == "random-selection");
        Assert.Contains(german.Units, unit => unit.Id == "r1");
        Assert.Single(german.Sealed);
        Assert.Contains(GameView.Of(History(), Revision, Russian).Events, item => item.Type == "random-selection");
    }

    [Fact]
    public async Task ReplayUsesTheRecordedRollAndAConfirmedAttemptIsNotRolledAgain()
    {
        await SetUp(Placement("r1", "defender-squad", "bd01:E4:0", concealed: true), Placement("r2", "defender-squad", "bd01:E4:0", hidden: true));
        var start = Revision;
        var play = Play(Fixed(1, 5));
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry(expected: start), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Equal(0, draws);
        Assert.Equal(PlayOutcome.Committed, (await play.ConfirmAsync(GameActions.EnterBuilding, Entry(expected: start), Player, proposed.Correlation)).Outcome);

        // The hidden r2 is placed beneath a "?" first, then rolls 5 against r1's 1 and is revealed.
        Assert.Equal(["entry-attempted", "conditions-changed", "dice-rolled", "random-selection", "conditions-changed", "entry-forced-back"],
            Since(start).Select(item => item.Type));
        Assert.True(Known("r2"));
        Assert.False(Known("r1"));

        var again = await Play(Throwing()).ConfirmAsync(GameActions.EnterBuilding, Entry(expected: start), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Replay, again.Outcome);
        Assert.Equal(2, draws);
        Assert.False(History().HasErrors);
    }

    [Theory]
    [InlineData(4, 4, new[] { "r1", "r2" })]
    [InlineData(2, 3, new[] { "r2" })]
    public async Task TiedAndSingleRevealsOfSquadsForceTheMoverBack(int first, int second, string[] revealed)
    {
        await SetUp(Placement("r1", "defender-squad", "bd01:E4:0", concealed: true), Placement("r2", "defender-squad", "bd01:E4:0", concealed: true));
        var (result, _) = await Enter(Fixed(first, second));
        Assert.Equal(PlayOutcome.Committed, result.Outcome);
        Assert.All(revealed, id => Assert.True(Known(id)));
        Assert.True(Current.Unit("g1")!.MovementEnded);
    }

    [Theory]
    [InlineData("l1", "r1", 6, 1, true)]
    [InlineData("l1", "r1", 1, 6, false)]
    [InlineData("l1", "r1", 5, 5, false)]
    [InlineData("l1", "l2", 3, 3, false)]
    [InlineData("l1", "l2", 5, 2, true)]
    public async Task ALoneRevealedSmcLeavesTheAttemptPendingAndAnythingElseForcesTheMoverBack(string first, string second, int a, int b, bool pending)
    {
        // A4.15 (p. 49): more than one revealed SMC denies the OVR, so only a lone SMC leaves the choice open.
        static object Unit(string id) => Placement(id, id.StartsWith('l') ? "defender-leader" : "defender-squad", "bd01:E4:0", concealed: true);
        await SetUp(Unit(first), Unit(second));
        var (result, _) = await Enter(Fixed(a, b));
        Assert.Equal(PlayOutcome.Committed, result.Outcome);
        Assert.Equal(pending, Current.OpenAttempts.Count == 1);
        Assert.Equal(!pending, Current.Unit("g1")!.MovementEnded);
        if (pending)
        {
            Assert.Equal([first], Current.OpenAttempts[0].Revealing);
            Assert.True(Known(first));
        }
    }

    [Fact]
    public async Task ALoneConcealedSmcIsRevealedWithoutARollAndThePhaseCannotAdvance()
    {
        await SetUp(Placement("l1", "defender-leader", "bd01:E4:0", concealed: true));
        var (result, start) = await Enter(Throwing());
        Assert.Equal(PlayOutcome.Committed, result.Outcome);
        Assert.Equal(["entry-attempted", "conditions-changed"], Since(start).Select(item => item.Type));
        Assert.True(Known("l1"));
        var open = Assert.Single(Current.OpenAttempts);
        Assert.Equal(("g1", "bd01:E4:0"), (open.Unit, open.Target.ToString()));

        var advance = await Play().ProposeAsync(GameActions.AdvancePhase, Args(new
        {
            gameId = Scope.Game,
            attemptId = "advance-late",
            expectedRevision = Revision
        }), Player);
        Assert.Equal(PlayOutcome.Denied, advance.Outcome);
        Assert.StartsWith("play.declaration-pending", Assert.Single(advance.Plan!.Reasons), StringComparison.Ordinal);

        var again = await Play().ProposeAsync(GameActions.EnterBuilding, Entry("enter-2"), Player);
        Assert.Equal(PlayOutcome.Denied, again.Outcome);
        Assert.Contains("play.fact-false: canMoveThisPhase", again.Plan!.Disclosure!.MoverReasons);
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
