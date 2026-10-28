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

    // The game's special rules, as a card's tokens (increment 5c); none unless a test sets them.
    private string[] ssrs = [];

    // The scenario year (increment 5e: a PSK needs one from September 1943); none unless a test sets it.
    private int? year;

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
        var start = new Dictionary<string, object>
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
        };
        if (ssrs.Length > 0)
        {
            start["specialRules"] = ssrs;
        }

        if (year is { } scenarioYear)
        {
            start["scenarioYear"] = scenarioYear;
        }

        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start,
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
        Assert.StartsWith("[C5]: g1 is berserk", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.True(Is(Current.Unit("g1")!, Conditions.Berserk));

        var answered = await Do(GameActions.Choose, NoRoll(), new
        {
            key = choice.Key,
            option,
        });
        Committed(answered);
        Assert.Contains(answered.Reasons, reason => reason.StartsWith(option == ChoicePending.Take
            ? "play.berserk-hardening: g1 in bd01:C5:0 loses berserk status and is Battle Hardened: it is Replaced by an unbroken attacker-elite-squad"
            : "play.berserk-hardening: g1 in bd01:C5:0 loses berserk status and its owner refuses Battle Hardening", StringComparison.Ordinal));
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

    // Increment 5b: the return to normal (A15.46; ruling R72.7).

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    /// <summary>Ends phases until the game is in <paramref name="phase"/> of the German Player Turn.</summary>
    private async Task AdvanceTo(string phase)
    {
        for (var index = 0; index < 8 && Current.Phase != phase; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }

        Assert.Equal((phase, "german"), (Current.Phase, Current.PhasingSide));
    }

    /// <summary>Makes a unit berserk with a written condition, as a Heat of Battle result would (the 5a test of a unit made berserk in mid move does the same).</summary>
    private void MakeBerserk(string unit)
    {
        var existing = store.Read(Scope)!.Events;
        var berserk = existing[^1] with
        {
            EventId = "berserk-" + unit,
            Revision = existing.Count + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged(unit, new Dictionary<string, ConditionState> { [Conditions.Berserk] = ConditionState.True }),
            Causes = [],
            Visibility = null,
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, nameof(BacklogPass72Tests), existing.Count, [berserk], Planner().Replay).Status);
    }

    /// <summary>The units a plan's events took the berserk status from.</summary>
    private string[] BerserkEnded(long since) => [.. Since(since).Select(item => item.Payload).OfType<ConditionsChanged>()
        .Where(change => change.Conditions.TryGetValue(Conditions.Berserk, out var berserk) && berserk == ConditionState.False).Select(change => change.Id)];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ABerserkSquadsHalvedTpbfInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal(bool concealedLeft)
    {
        // A15.46, A15.432 (p. 84; ruling R72.7): the berserk German 4-6-7 shares C5 with the Russian HS, not in Melee. In the AFPh it fires TPBF at its own
        // Location (4 FP x 3, halved: 6 FP); the DR 1 + 1 is a 1KIA on the HS. With no other Known enemy unit there, it returns to normal; a concealed Russian
        // squad left there is not Known (A12.1) and changes nothing. The attack's targets are the Russian units only (A7.212); before the pass the firer was
        // listed among them and the attack refused.
        Dictionary<string, object>[] placements = [Squad("g1", "C5", "german", Conditions.Berserk), Unit("r1", "asl:half-squad", "defender-half-squad", "C5", "russian")];
        await Setup(concealedLeft ? [.. placements, Squad("r2", "C5", "russian", Conditions.Concealed)] : placements);
        await AdvanceTo("afph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 1, 3, 3, 4, 4, 4, 4), new Dictionary<string, object> { ["firers"] = G1, ["target"] = L("C5") }));
        var fire = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        var resolution = fire.Resolution.GetRawText();
        Assert.Contains("triple-point-blank-fire", resolution, StringComparison.Ordinal);
        Assert.Contains("advancing-fire", resolution, StringComparison.Ordinal);
        Assert.DoesNotContain("\"g1\"", fire.Facts.GetProperty("targets").GetRawText(), StringComparison.Ordinal);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r1")!.Status);
        if (concealedLeft)
        {
            // The concealed squad is attacked on the halved FP's column (A12.13: 2 FP, a K/1) and survives its MC (3 + 3) as a HS, not Known.
            Assert.Contains(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Side == "russian" && Current.Location(unit.Id)?.Location == At("C5"));
        }

        Assert.Equal(G1, BerserkEnded(before));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Berserk));
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkSquadsFtInItsOwnLocationInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal()
    {
        // A15.46 (p. 84), A22.1 (p. 89: "never increased for PBF/TPBF", "not for use in the AFPh"; ruling R72.7): the berserk German 4-6-7 fires its FT alone at
        // its own Location, 24 FP neither tripled nor halved; the DR eliminates the Russian HS there, and the squad returns to normal.
        await Setup(Squad("g1", "C5", "german", Conditions.Berserk), new Dictionary<string, object>
        {
            ["id"] = "ft",
            ["kind"] = "asl:ft",
            ["definition"] = "attacker-ft",
            ["side"] = "german",
            ["holding"] = new
            {
                holder = "g1",
                role = "possessed"
            },
            ["conditions"] = new Dictionary<string, bool>(),
        }, Unit("r1", "asl:half-squad", "defender-half-squad", "C5", "russian"));
        await AdvanceTo("afph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 2, 6, 6, 6, 6), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["ft"] },
            withoutInherent = G1,
            target = L("C5"),
        }));
        var fire = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        Assert.Equal(24m, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r1")!.Status);
        Assert.Equal(G1, BerserkEnded(before));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Berserk));
        NoReplayErrors();
    }

    private static readonly string[] ToTheCcph = ["dfph", "afph", "rtph", "aph", "ccph"];

    [Fact]
    public async Task ABerserkSquadSetUpWithALoneTankInItsLocationDestroysItInTheCcphAndReturnsToNormal()
    {
        // The Studio game p72-berserk-vehicle-cc-return (games_5b.py, game b): the game file writes the berserk 4-6-7 and the lone enemy tank into C4 with
        // "instance-created" events (the Setup action refuses a vehicle beside an enemy, ruling R25.3, but the file does not pass through it), then
        // "phase-changed" events to the CCPh. The projector reads them; the planner takes the CC attack on the tank, whose destruction returns the squad.
        await Setup(Squad("g1", "C4", "german", Conditions.Berserk));
        var existing = store.Read(Scope)!.Events;
        EventPayload[] written =
        [
            new InstanceCreated(new NewInstance("ru-t", "asl:vehicle", "defender-tank", "russian", new MapPosition(At("C4"), Facing: LimboDancer.Domains.Asl.Units.Documents.UnitFacing.East),
                null, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False })),
            .. ToTheCcph.Select(phase => (EventPayload)new PhaseChanged(1, phase, "german")),
        ];
        GameEvent[] appended = [.. written.Select((payload, index) => existing[^1] with
        {
            EventId = $"file-{index + 1}",
            Revision = existing.Count + index + 1,
            Type = payload is InstanceCreated ? "instance-created" : "phase-changed",
            Payload = payload,
            Causes = [],
            Visibility = null,
        })];
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, nameof(BacklogPass72Tests), existing.Count, appended, Planner().Replay).Status);
        NoReplayErrors();
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Once(1, 2, 6, 6), new Dictionary<string, object> { ["location"] = L("C4"), ["attackers"] = G1, ["vehicleId"] = "ru-t" }));
        // One roll: 1 + 2 = 3 against the record's Kill Number 5, "eliminated" (a wreck).
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-t")!.Status);
        Assert.Equal(G1, BerserkEnded(before));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ABerserkSquadThatDestroysTheTankInCcReturnsToNormalOnlyWhenNoKnownEnemyIsLeft(bool infantryLeft)
    {
        // A15.46 (p. 84; ruling R72.7): the German 4-6-7 advances into C4, the Russian tank's Location (its PAATC 3 + 4 against 7, A11.6), and is made berserk
        // there. In the CCPh it attacks the tank and destroys it: alone there, the tank was the last Known enemy unit, and the squad returns to normal; with a
        // Russian HS beside it, the squad stays berserk. (A lone truck would be captured as the CCPh begins, A11.52.)
        Dictionary<string, object>[] placements = [Squad("g1", "C5", "german"), Vehicle("ru-t", "defender-tank", "C4", "russian", "east")];
        await Setup(infantryLeft ? [.. placements, Unit("r1", "asl:half-squad", "defender-half-squad", "C4", "russian")] : placements);
        await AdvanceTo("aph");
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = G1,
            to = L("C4"),
        }));
        MakeBerserk("g1");
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Once(1, 2, 6, 6), new Dictionary<string, object> { ["location"] = L("C4"), ["attackers"] = G1, ["vehicleId"] = "ru-t" }));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-t")!.Status);
        Assert.Equal(infantryLeft ? [] : G1, BerserkEnded(before));
        Assert.Equal(infantryLeft, Is(Current.Unit("g1")!, Conditions.Berserk));
        NoReplayErrors();
    }

    // Increment 5c: the SSR tokens, A19.132's SSR ELR, and A19.32's MG B# (rulings R72.9, R72.10, R72.13).

    private static readonly string[] RussianPair = ["r1", "r2"];
    private static readonly string[] RussianGunner = ["r1"];

    [Fact]
    public async Task UnderAnSsrElrOfThreeAFailedMcReplacesTheGermanEngineerSquadByTheConscript()
    {
        // A19.132 (p. 86; ruling R72.13): the SSR underscored-elr:german:3. In the German DFPh the two Russian 4-4-7 in C4 fire PBF at the German 8-3-8 in C5:
        // 16 FP, the DR 3 + 4 a 2MC; its check 5 + 5 (12) fails Morale Level 8 by four, beyond the SSR's ELR of 3 (R15.9's 5 would only break it). It is
        // Replaced by a broken 4-3-6 Conscript; the record carries the target's SSR ELR and the pass's stamp, and the game replays without error.
        ssrs = ["underscored-elr:german:3"];
        await Setup(Unit("g1", "asl:squad", "attacker-elite-squad-8-3-8", "C5", "german"), Squad("r1", "C4", "russian"), Squad("r2", "C4", "russian"));
        Assert.Contains("underscored-elr:german:3", Current.SpecialRules);
        await AdvanceTo("dfph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(3, 4, 5, 5), new Dictionary<string, object> { ["firers"] = RussianPair, ["target"] = L("C5") }));
        var fire = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        Assert.True(fire.Facts.GetProperty("unitSubstitution").GetBoolean());
        Assert.Equal(3, fire.Facts.GetProperty("targets")[0].GetProperty("underscoredElr").GetInt32());
        var replaced = Assert.Single(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Side == "german");
        Assert.Equal(("attacker-conscript-squad", At("C5"), true), (replaced.Definition!.Definition, Current.Location(replaced.Id)!.Location, Is(replaced, Conditions.Broken)));
        NoReplayErrors();
    }

    [Fact]
    public async Task AConscriptSquadsLmgMalfunctionsAtItsLoweredBreakdownNumber()
    {
        // A19.32 (p. 86; ruling R72.9, R9.10 lifted): in the German DFPh the Russian 4-2-6 Conscript in C3 fires its LMG with its inherent FP at the German 4-6-7
        // in C5, two hexes off. Its Original DR 4 + 6 (10) is one below the LMG's B11: in a Conscript's hands its B# is 10, so it malfunctions.
        await Setup(Squad("g1", "C5", "german"), Unit("r1", "asl:squad", "defender-conscript-squad", "C3", "russian"), new Dictionary<string, object>
        {
            ["id"] = "m1",
            ["kind"] = "asl:mg",
            ["definition"] = "defender-lmg",
            ["side"] = "russian",
            ["holding"] = new
            {
                holder = "r1",
                role = "possessed"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        });
        await AdvanceTo("dfph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 6, 2, 3), new
        {
            firers = RussianGunner,
            weapons = new Dictionary<string, string[]> { ["r1"] = ["m1"] },
            target = L("C5"),
        }));
        var fire = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        var effect = fire.Resolution.GetProperty("weaponEffects")[0];
        Assert.Equal((10, true), (effect.GetProperty("breakdownNumber").GetInt32(), effect.GetProperty("malfunctioned").GetBoolean()));
        Assert.True(Is(Current.Find("m1")!, Conditions.Malfunctioned));
        NoReplayErrors();
    }

    // Increment 5d: Ammunition Shortage (rulings R72.11, R72.12).

    [Fact]
    public async Task UnderAmmunitionShortageARussianFireGroupsOriginal12ReplacesOneSquadInPublic()
    {
        // A19.131 (p. 86; ruling R72.11): the SSR ammo-shortage:russian. In the German DFPh the two Russian 4-4-7 in C4 fire PBF with their Inherent FP at the
        // German 4-6-7 in C5: the Original 12 (6 + 6) does nothing to it; the doubles make both Russian squads cower (the Cowering dr 1, 1); the Random Selection
        // dr 2 for r1 and 5 for r2 picks r2, Replaced by an unbroken 4-2-6 Conscript in C4. The record carries the fact, the effects are public, and the game
        // replays without error.
        ssrs = ["ammo-shortage:russian"];
        await Setup(Squad("g1", "C5", "german"), Squad("r1", "C4", "russian"), Squad("r2", "C4", "russian"));
        await AdvanceTo("dfph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 6, 1, 1, 2, 5), new Dictionary<string, object> { ["firers"] = RussianPair, ["target"] = L("C5") }));
        var events = Since(before);
        var fire = Assert.Single(events.Select(item => item.Payload).OfType<FireResolved>());
        Assert.True(fire.Facts.GetProperty("ammunitionShortage").GetBoolean());
        var effect = Assert.Single(fire.Resolution.GetProperty("shortageEffects").EnumerateArray());
        Assert.Equal(("r2", "defender-conscript-squad", 5), (effect.GetProperty("unitId").GetString(), effect.GetProperty("finalDefinitionId").GetString(),
            effect.GetProperty("randomSelectionDr").GetInt32()));
        Assert.Contains(events, item => item.Payload is DiceRolled { Purpose: "fire-shortage-selection", Count: 2 } && item.Visibility is null);
        var lineage = Assert.Single(events, item => item.Payload is LineageRecorded { Action: LineageAction.Replaced } replaced && replaced.Consumed.Contains("r2"));
        Assert.Null(lineage.Visibility);

        var conscript = Assert.Single(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Side == "russian" && unit.Id != "r1");
        Assert.Equal(("defender-conscript-squad", At("C4"), false), (conscript.Definition!.Definition, Current.Location(conscript.Id)!.Location, Is(conscript, Conditions.Broken)));

        // It has fired: it takes the Final Fire counter r2 would have taken.
        Assert.True(Is(conscript, Conditions.FinalFire));
        NoReplayErrors();
    }

    [Fact]
    public async Task AFireRecordMadeBeforeTheShortageFactVerifiesAsItWasRecorded()
    {
        // Absent-safety (ruling R72.12): in a game under ammo-shortage:russian, a fire record without the pass's stamp, as one made before it would be, carries no
        // Ammunition Shortage and verifies as recorded; a stamped record that leaves the fact out is refused, since the state gives it.
        ssrs = ["ammo-shortage:russian"];
        await Setup(Squad("g1", "C5", "german"), Squad("r1", "C4", "russian"), Squad("r2", "C4", "russian"));
        await AdvanceTo("dfph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(3, 4, 2, 2), new Dictionary<string, object> { ["firers"] = RussianPair, ["target"] = L("C5") }));
        var all = store.Read(Scope)!.Events;
        var index = all.ToList().FindIndex((int)before, item => item.Payload is FireResolved);
        var fire = (FireResolved)all[index].Payload;
        var state = Planner().Replay([.. all.Take(index)]).Current!;
        var rolls = all.Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(roll => roll.Roll, StringComparer.Ordinal);
        var verifier = new FireRecordVerifier(new ScenarioA1FirePackage().Reference);
        Assert.Null(verifier.Verify(state, fire, rolls));

        JsonElement Without(params string[] names)
        {
            var facts = System.Text.Json.Nodes.JsonNode.Parse(fire.Facts.GetRawText())!.AsObject();
            foreach (var name in names)
            {
                facts.Remove(name);
            }

            return JsonSerializer.SerializeToElement(facts);
        }

        Assert.Null(verifier.Verify(state, fire with
        {
            Facts = Without("unitSubstitution", "ammunitionShortage")
        }, rolls));
        Assert.NotNull(verifier.Verify(state, fire with
        {
            Facts = Without("ammunitionShortage")
        }, rolls));
    }

    [Fact]
    public async Task AFireLaneIsRefusedUnderAmmunitionShortage()
    {
        // A19.131 (ruling R72.12): Ammunition Shortages "do prevent placement of Fire Lanes": the Russian 4-4-7 in C3 declares a Fire Lane with its LMG's
        // Defensive First Fire at the German squad moving into C5, and is refused.
        ssrs = ["ammo-shortage:russian"];
        await Setup(Squad("g1", "C6", "german"), Squad("r1", "C3", "russian"), new Dictionary<string, object>
        {
            ["id"] = "m1",
            ["kind"] = "asl:mg",
            ["definition"] = "defender-lmg",
            ["side"] = "russian",
            ["holding"] = new
            {
                holder = "r1",
                role = "possessed"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        });
        Committed(await Move("C5"));
        var refused = await Do(GameActions.Fire, NoRoll(), new
        {
            firers = RussianGunner,
            weapons = new Dictionary<string, string[]> { ["r1"] = ["m1"] },
            target = L("C5"),
            fireLane = new Dictionary<string, string> { ["weapon"] = "m1", ["to"] = L("C6") },
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.Contains(ScenarioA1FireEligibility.AmmunitionShortageFireLaneRefusal, StringComparison.Ordinal));
    }

    // Increment 5e: the hero's weapons (ruling R72.8).

    private static readonly string[] RussianHero = ["rh"];

    private static Dictionary<string, object> Latw(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:latw",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    [Fact]
    public async Task ALoneHeroFiresAPskAtATankWithTheTwoManDrmNegated()
    {
        // C13.45, C13.48 (p. 184), A15.23 (ruling R72.8): in the German AFPh of July 1944 the German hero in C5 fires his PSK at the Russian T-34 in C3, two hexes
        // off: the PSK's 9, Case C3 +2 for the AFPh, his -1 and the +1 of one man firing a two-man SW. The DR 2 + 3 (7) hits; the To Kill DR 6 + 6 is a dud and
        // takes no hero DRM (C13.25). The record carries the pass's fact, and the game replays without error. Before the pass a lone hero was refused the PSK.
        year = 1944;
        await Setup(Unit("gh", "asl:hero", "attacker-hero", "C5", "german"), Latw("psk", "attacker-psk", "gh", "german"), Vehicle("ru-t", "defender-tank", "C3", "russian", "east"));
        await AdvanceTo("afph");
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(2, 3, 6, 6, 3, 4, 3, 4), new Dictionary<string, object> { ["gunId"] = "psk", ["target"] = L("C3"), ["targetVehicle"] = "ru-t" }));
        var fired = Assert.Single(Since(before).Select(item => item.Payload).OfType<OrdnanceFired>());
        Assert.True(fired.Facts.Deserialize<OrdnanceShot>(LiveFire.Json)!.HeroWeapons);
        var shot = fired.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json)!;
        Assert.Equal([("hero:gh", -1m), ("hero-two-man:gh", 1m)],
            shot.ToHit!.Drm.Where(item => item.Name.StartsWith("hero", StringComparison.Ordinal)).Select(item => (item.Name, item.Value)));
        Assert.True(shot.ToHit.Hit);
        Assert.DoesNotContain(shot.Kill!.Modifications, item => item.Name.StartsWith("hero", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task AHeroFiresAnAtrAtASquadAsSmallArmsWithHisHeroicDrm()
    {
        // C13.24, A15.23, A15.24 (ruling R72.8): in the German DFPh the Russian hero in C4 fires his ATR as Small Arms at the German 4-6-7 in C6, two hexes off:
        // 1 FP, A15.24's -1 within the ATR's Normal Range, no hero-mg +1, no inherent FP of his own. The DR 5 + 4 is a Final 8, no effect. Before the pass a hero was
        // refused the ATR.
        await Setup(Squad("g1", "C6", "german"), Unit("rh", "asl:hero", "defender-hero", "C4", "russian"), Latw("atr", "defender-atr", "rh", "russian"));
        await AdvanceTo("dfph");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(5, 4), new
        {
            firers = RussianHero,
            weapons = new Dictionary<string, string[]> { ["rh"] = ["atr"] },
            target = L("C6"),
        }));
        var fire = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        var arithmetic = fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)!.Arithmetic!;
        Assert.Equal([("heroic:rh", -1m)], arithmetic.Drm.Where(item => item.Name.StartsWith("hero", StringComparison.Ordinal)).Select(item => (item.Name, item.Value)));
        Assert.DoesNotContain(arithmetic.Firers, item => item.UnitId == "rh");
        Assert.Equal(1m, arithmetic.TotalFirepower);
        NoReplayErrors();
    }
}
