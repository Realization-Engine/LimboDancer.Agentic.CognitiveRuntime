using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 72 in live play, Infantry V: Heat of Battle and Unit Substitution. Increment 5a: the berserk charge (A15.43 to A15.46, E1.533; rulings R72.3 to R72.6).
/// Board 01's grid, all Open Ground at level 0 but the hexes a test floods (every hex of column D, a Water Obstacle the charge never crosses), fixed dice, and a
/// LOS that is clear everywhere. The German side moves first; every read of the state replays the game.
/// </summary>
public sealed class BacklogPass72Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f572");
    private static readonly GameScope Scope = new(Tenant, "pass72");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] HeldAfterTheCharge = ["m1", "m2", "m3"];
    private static readonly string[] UpColumnC = ["C8", "C7", "C6", "C5", "C4", "C3"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass72-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    // The hexes a test floods: Water, a Water Obstacle (B21.1), whose entry the movement model refuses.
    private readonly HashSet<string> water = new(StringComparer.Ordinal);

    public BacklogPass72Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static readonly TerrainType Open = new() { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
    private static readonly TerrainType Water = new() { Code = 8, Name = "Water", Category = LosCategory.Water };

    /// <summary>Board 01's grid, all Open Ground at level 0, with Water in the hexes a test floods.</summary>
    private BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var type = water.Contains(text) ? Water : Open;
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 72 test board", new HexFactSet(geometry, "pass72", hexes));
    }

    /// <summary>Floods every hex of column D, from edge to edge, so nothing crosses from column C to column E.</summary>
    private void FloodColumnD()
    {
        foreach (var hex in Board01Fixture.Hexes().Where(text => text.Length > 1 && text[0] == 'D' && char.IsDigit(text[1])))
        {
            water.Add(hex);
        }
    }

    // Clear LOS at the board's true range.
    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static BoardLocation At(string hex) => BoardLocation.Parse(L(hex));

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Squad(string id, string hex, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", hex, side, states);

    private static Dictionary<string, object> Lmg(string id, string holder) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = "attacker-lmg",
        ["side"] = "german",
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, string side, string facing) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = facing },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A German game against the Russians, left in the German MPh.</summary>
    private async Task Setup(params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass72Tests),
                ["catalog"] = $"asl-scenario-a1@{LiveFire.CatalogVersion}",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
            },
            placements,
        })));
        for (var index = 0; index < 2; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }

        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Move(string to) => Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L(to) });

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(History.HasErrors);

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static (UnitInstance Unit, BoardLocation? Target, IReadOnlyList<BoardLocation> Next, string? Undecided) Charge(GamePlanner planner, GameState state) =>
        Assert.Single(planner.Charges(state), item => item.Unit.Id == "g1");

    [Fact]
    public async Task ABerserkSquadWhoseNearestEnemyIsAcrossWaterChargesTheNextNearestFromOneSearch()
    {
        // A15.45 (p. 84; ruling R72.3): the berserk German squad in C5 will not charge through the Water of column D to the Russian squad in E5 (two hexes off);
        // it charges the one in C2 (three hexes off) instead, by C4. LOS: clear from C5 to both. Route: C5, C4, C3, C2, all Open Ground (one MF each).
        FloodColumnD();
        await Setup(Squad("g1", "C5", "german", Conditions.Berserk), Squad("r1", "E5", "russian"), Squad("r2", "C2", "russian"));
        var planner = Planner();
        var state = planner.Replay(store.Read(Scope)!.Events).Current!;
        var charge = Charge(planner, state);
        Assert.Equal((At("C2"), (string?)null), (charge.Target, charge.Undecided));
        Assert.Equal([At("C4")], charge.Next);

        // The fall back costs no second search (the Berserk Charge Plan, ruling R1): the page's three reads are answered by one.
        planner.ChargeLanes(state, charge.Unit);
        Charge(planner, state);
        Assert.Equal(1, planner.ChargeSearches);

        Committed(await Move("C4"));
        Assert.Equal(At("C4"), Current.Location("g1")!.Location);
        NoReplayErrors();
    }

    [Theory]
    [InlineData(ChoicePending.Take)]
    [InlineData(ChoicePending.Decline)]
    public async Task ABerserkSquadThatCanReachNoEnemyIsOfferedBattleHardeningAsTheMphEnds(string option)
    {
        // A15.45, A15.44, E1.533 (pp. 84, 224; ruling R72.4): the only Russian squad, in E5, is across the Water of column D, so the berserk German squad in C5
        // stays in place; as the MPh ends it loses berserk status, and the German side takes Battle Hardening (a 4-6-7 becomes the elite 4-6-8, A15.3) or
        // refuses it (Good Order).
        FloodColumnD();
        await Setup(Squad("g1", "C5", "german", Conditions.Berserk), Squad("r1", "E5", "russian"));
        var charge = Charge(Planner(), Current);
        Assert.Empty(charge.Next);
        Assert.True(ScenarioA1ChargeCalculator.NoneReachable(charge.Undecided), charge.Undecided);

        var before = Revision;
        var ended = await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        });
        Committed(ended);
        Assert.Contains(ended.Reasons, reason => reason.StartsWith("play.berserk-hardening: g1 in bd01:C5:0 can reach no Known enemy unit", StringComparison.Ordinal));
        Assert.Equal("dfph", Current.Phase);
        var choice = Current.Choice!;
        Assert.Equal(($"{ScenarioA1ChargeCalculator.HardeningChoice}:g1", ChoicePending.BattleHardening, "german"), (choice.Key, choice.Kind, choice.Side));
        Assert.Contains("[C5]", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.True(Is(Current.Unit("g1")!, Conditions.Berserk));

        Committed(await Do(GameActions.Choose, NoRoll(), new
        {
            key = choice.Key,
            option,
        }));
        Assert.Null(Current.Choice);
        Assert.Empty(Current.ChoicesMade);
        var after = Since(before).Select(item => item.Payload).ToArray();
        if (option == ChoicePending.Take)
        {
            Assert.Equal(InstanceStatus.Consumed, Current.Unit("g1")!.Status);
            var hardened = Assert.Single(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Side == "german");
            Assert.Equal(("attacker-elite-squad", At("C5")), (hardened.Definition!.Definition, Current.Location(hardened.Id)!.Location));
            Assert.False(Is(hardened, Conditions.Berserk));
            Assert.Single(after.OfType<LineageRecorded>());
        }
        else
        {
            var unit = Current.Unit("g1")!;
            Assert.Equal((InstanceStatus.Active, "attacker-squad"), (unit.Status, unit.Definition!.Definition));
            Assert.False(Is(unit, Conditions.Berserk));
            Assert.Empty(after.OfType<LineageRecorded>());
        }

        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkChargeEntersALocationHoldingAnEnemyTankAndEnemyInfantryAndTheFpfIsOwed()
    {
        // Ruling R72.5 (moved in from pass 70): the berserk German squad in C5 charges C4, where the Russian tank and the Russian squad are; its charge is no
        // longer undecided. It enters, and the Russian squad owes its FPF on it before the DEFENDER may pass (A8.312, A15.432).
        await Setup(Squad("g1", "C5", "german", Conditions.Berserk), Vehicle("ru-t", "defender-tank", "C4", "russian", "east"), Squad("r1", "C4", "russian"));
        var charge = Charge(Planner(), Current);
        Assert.Equal((At("C4"), (string?)null), (charge.Target, charge.Undecided));
        Assert.Equal([At("C4")], charge.Next);
        Committed(await Move("C4"));
        Assert.Equal(At("C4"), Current.Location("g1")!.Location);
        var pass = await Do(GameActions.PassFire, NoRoll(), new
        {
        });
        Assert.NotEqual(PlayOutcome.Committed, pass.Outcome);
        Assert.Contains(pass.Reasons, reason => reason.StartsWith("play.pass-mandatory: r1", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadMadeBerserkInMidMoveHasEightMfLessThoseSpentAndAbandonsItsFourthLightSw()
    {
        // A15.431 (p. 84; ruling R72.6): the German squad in C10 holds four LMG (1 PP each), one PP over its IPC, so it has three MF (A4.42). It steps to C9
        // (one MF); there it becomes berserk (a Heat of Battle result of the DEFENDER's fire, written here as its condition). It must charge the Russian squad
        // in C2, seven hexes up column C (one MF each): eight MF less the one spent leave seven, since it abandons the fourth LMG with its next step.
        await Setup(Squad("g1", "C10", "german"), Lmg("m1", "g1"), Lmg("m2", "g1"), Lmg("m3", "g1"), Lmg("m4", "g1"), Squad("r1", "C2", "russian"));
        Committed(await Move("C9"));
        await Pass();
        var existing = store.Read(Scope)!.Events;
        var berserk = existing[^1] with
        {
            EventId = "berserk-1",
            Revision = existing.Count + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Berserk] = ConditionState.True }),
            Causes = [],
            Visibility = null,
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, nameof(BacklogPass72Tests), existing.Count, [berserk], Planner().Replay).Status);

        foreach (var hex in UpColumnC)
        {
            Committed(await Move(hex));
            await Pass();
        }

        Committed(await Move("C2"));
        var unit = Current.Unit("g1")!;
        Assert.Equal((At("C2"), 8), (Current.Location("g1")!.Location, unit.MfSpent));
        Assert.Equal(HeldAfterTheCharge, Current.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == "g1").Select(item => item.Id).Order(StringComparer.Ordinal));
        NoReplayErrors();
    }
}
