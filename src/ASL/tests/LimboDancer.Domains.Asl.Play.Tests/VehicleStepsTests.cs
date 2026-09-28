using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Unit step 25 in live play (U29): a German truck moves one MP expenditure at a time and a Russian squad's Defensive First Fire
/// immobilizes it on the Vehicle line; a vehicle ends its MPh in Motion only when it cannot Stop or move on; the SPW 251/1's AAMG fires
/// with its Multiple ROF; a Russian attack on the CE halftrack Stuns its crew, which stays BU through Stun +1; and the refusals for an
/// AFV's cover, an enemy vehicle's Location, and vehicle set-up. Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class VehicleStepsTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f125");
    private static readonly GameScope Scope = new(Tenant, "convoy");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] Halftrack = ["de-ht"];
    private static readonly string[] G1 = ["g1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-vehicles-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public VehicleStepsTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS everywhere, at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
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

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static Dictionary<string, object> Squad(string id, string definition, string at, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = "asl:squad",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string? facing = "east", params string[] states)
    {
        var conditions = new Dictionary<string, bool>();
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = "asl:vehicle",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = facing is null ? new Dictionary<string, string> { ["at"] = at } : new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["conditions"] = conditions,
        };
    }

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task<PlayResult> TrySetup(string firstSide, params Dictionary<string, object>[] placements) =>
        await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-" + Revision,
            expectedRevision = Revision,
            start = new
            {
                label = "Convoy",
                catalog = "asl-scenario-a1@1.5.0",
                boards = Bd01,
                firstSide,
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements,
        }));

    private async Task Setup(string firstSide, params Dictionary<string, object>[] placements) => Committed(await TrySetup(firstSide, placements));

    private async Task Advance(int times = 1)
    {
        for (var index = 0; index < times; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Step(string vehicle, string kind, string? to = null, string? facing = null)
    {
        var arguments = new Dictionary<string, string> { ["vehicleId"] = vehicle, ["kind"] = kind };
        if (to is not null)
        {
            arguments["to"] = to;
        }

        if (facing is not null)
        {
            arguments["facing"] = facing;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private FireResolution LastFire(long before) =>
        Since(before).Select(item => item.Payload).OfType<FireResolved>().Single().Resolution.Deserialize<FireResolution>(LiveFire.Json)!;

    [Fact]
    public async Task U29ATruckMovesOneMpExpenditureAtATimeAndDefensiveFireImmobilizesIt()
    {
        // The Opel 6700 in A8 faces east: its VCA points at B7 and B8 (D2.11). The Russian 4-4-7 waits in B10.
        await Setup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);

        // D2.12: Start is 1 MP, after which the DEFENDER may fire; the next expenditure waits for his pass.
        Committed(await Step("de-t", "start"));
        Assert.True(Current.Movement is { Vehicle: true, WindowOpen: true, Started: true });
        Refused(await Step("de-t", "enter", to: "bd01:B8:0"), "play.move-window");
        await Pass();

        // D2.11: only a hex the VCA points at; C8 is not one.
        Refused(await Step("de-t", "enter", to: "bd01:C8:0"), "play.move-vehicle");

        // The truck enters B8, Open Ground, for 4 MP (Terrain Chart p. 698): 5 of its 28 MP spent.
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        var truck = Current.Unit("de-t")!;
        Assert.Equal((5, false, "bd01:B8:0"), (truck.MfSpent, truck.HalfMfSpent, Current.Location("de-t")!.Location.ToString()));

        // A7.308: 4 FP at two hexes, Kill Number 5; the Original 5 has no DRM (no FFMO or FFNAM for a vehicle), so it is immobilized.
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(2, 3), new
        {
            firers = R1,
            target = "bd01:B8:0"
        }));
        var effect = Assert.Single(LastFire(before).VehicleEffects!);
        Assert.Equal((FireVehicleEffect.Immobilized, 5, 5), (effect.Result, effect.KillNumber, effect.FinalDr));
        truck = Current.Unit("de-t")!;
        Assert.True(Is(truck, Conditions.Immobilized) && !Is(truck, Conditions.Motion));

        // An immobilized vehicle moves no more (D.7); its move ends, not in Motion.
        await Pass();
        Refused(await Step("de-t", "stop"), "play.move-vehicle");
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Motion));
    }

    [Fact]
    public async Task AVehicleEndsInMotionOnlyWhenItCannotStopOrMoveOnAndOneInMotionMustSpendAnMp()
    {
        await Setup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"),
            Vehicle("de-t2", "attacker-truck", "bd01:D10:0", "german", "north-east", Conditions.Motion), Squad("r1", "defender-squad", "bd01:I1:0", "russian"));
        await Advance(2);

        // D2.4: the truck in Motion must spend at least one MP this MPh.
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.vehicle-motion");

        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", to: "bd01:B7:0"));
        await Pass();

        // 23 MP left: enough to enter a hex of its VCA, so it must Stop before its move ends (D2.4).
        Refused(await Do(GameActions.EndMove, NoRoll(), new
        {
        }), "play.vehicle-motion");
        Committed(await Step("de-t", "stop"));
        await Pass();

        // D2.1 (ruling R5.15): its MP left are spent in its hex, which the DEFENDER may fire at; its move ends when he passes.
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.True(Current.Movement is { Ending: true, WindowOpen: true });
        await Pass();
        Assert.Null(Current.Movement);
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Motion));

        // The truck in Motion needs no Start (D2.4); it turns one hexspine for 1 MP (D2.11), but not two.
        Refused(await Step("de-t2", "start"), "play.move-vehicle");
        Refused(await Step("de-t2", "turn", facing: "west"), "play.move-vehicle");
        Committed(await Step("de-t2", "turn", facing: "north-west"));
        Assert.False(Is(Current.Unit("de-t2")!, Conditions.Motion));
        await Pass();
        Committed(await Step("de-t2", "stop"));
        await Pass();
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        await Pass();
        await Advance();
        Assert.Equal("dfph", Current.Phase);
    }

    [Fact]
    public async Task TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase()
    {
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german"), Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance();
        Assert.Equal("pfph", Current.Phase);

        // D1.83: the AAMG's 3 FP at two hexes; the colored 1 keeps its ROF of 1 (C2.24, D3.5).
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 2, 3, 3, 3, 3, 3, 3), new
        {
            firers = Halftrack,
            target = "bd01:B10:0"
        }));
        var record = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(Halftrack, record.Firers);
        Assert.True(Assert.Single(LastFire(before).WeaponEffects!).RateOfFireRetained);
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.PrepFire));

        // The second shot loses the ROF; a third is refused (D3.5).
        before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 5, 3, 3, 3, 3, 3, 3), new
        {
            firers = Halftrack,
            target = "bd01:B10:0"
        }));
        Assert.False(Assert.Single(LastFire(before).WeaponEffects!).RateOfFireRetained);
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Fire, Once(4, 5), new
        {
            firers = Halftrack,
            target = "bd01:B10:0"
        })).Outcome);

        // D.3, D5.33: having Prep Fired it neither moves nor changes its CE status this MPh.
        await Advance();
        Refused(await Step("de-ht", "start"), "play.move-vehicle");
        Refused(await Do(GameActions.ButtonUp, NoRoll(), new
        {
            vehicleId = "de-ht",
            buttonedUp = true
        }), "play.button-up");

        // In the APh it buttons up, once.
        await Advance(4);
        Assert.Equal("aph", Current.Phase);
        Committed(await Do(GameActions.ButtonUp, NoRoll(), new
        {
            vehicleId = "de-ht",
            buttonedUp = true
        }));
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.ButtonedUp));
        Refused(await Do(GameActions.ButtonUp, NoRoll(), new
        {
            vehicleId = "de-ht",
            buttonedUp = false
        }), "play.button-up");
    }

    [Fact]
    public async Task ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne()
    {
        // The Russian squad fires at the CE halftrack two hexes away: 4 FP; the crew's Final DR is the Original DR + 2 (D5.31).
        await Setup("russian", Vehicle("de-ht", "attacker-halftrack", "bd01:B8:0", "german"), Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance();
        var column = Array.IndexOf(ScenarioA1FireReference.ColumnFp, 4);
        var (colored, white) = Enumerable.Range(1, 6).SelectMany(one => Enumerable.Range(1, 6).Select(two => (one, two)))
            .First(pair => pair.one != pair.two && Reference.Result(pair.one + pair.two + 2, column) is var outcome
                && (outcome == "NMC" || (outcome.EndsWith("MC", StringComparison.Ordinal) && !outcome.Contains("KIA", StringComparison.Ordinal))));

        // A failed MC (6 and 5 = 11 against the crew's Morale 8) Stuns it (D5.34): it buttons up.
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(colored, white, 6, 5), new
        {
            firers = R1,
            target = "bd01:B8:0"
        }));
        Assert.Equal(FireVehicleEffect.Stunned, Assert.Single(LastFire(before).VehicleEffects!).CrewResult);
        var halftrack = Current.Unit("de-ht")!;
        Assert.True(Is(halftrack, Conditions.Stunned) && Is(halftrack, Conditions.ButtonedUp));
        Assert.False(LiveFire.CrewExposed(halftrack));

        // At the end of the Russian Player Turn the Stun becomes Stun +1 (D5.34); the crew stays BU (referee D2).
        await Advance(7);
        Assert.Equal(("rph", "german"), (Current.Phase, Current.PhasingSide));
        halftrack = Current.Unit("de-ht")!;
        Assert.True(Is(halftrack, Conditions.StunRecovery) && Is(halftrack, Conditions.ButtonedUp) && !Is(halftrack, Conditions.Stunned));

        // BU, its AAMG may not fire in the PFPh (D1.83, D5.3); in its MPh the crew may expose itself again (D5.33).
        await Advance();
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Fire, Once(2, 3), new
        {
            firers = Halftrack,
            target = "bd01:B10:0"
        })).Outcome);
        await Advance();
        Committed(await Do(GameActions.ButtonUp, NoRoll(), new
        {
            vehicleId = "de-ht",
            buttonedUp = false
        }));
        Assert.True(LiveFire.CrewExposed(Current.Unit("de-ht")!));
    }

    [Fact]
    public async Task AnAfvsCoverAnEnemyVehiclesLocationAndResidualFpAreRefused()
    {
        // The halftrack in B9 lies between the Russian squad in B10 and the German squad in B8; a second German squad shares B9 with it.
        await Setup("russian", Vehicle("de-ht", "attacker-halftrack", "bd01:B9:0", "german"), Squad("g1", "attacker-squad", "bd01:B8:0", "german"),
            Squad("g2", "attacker-squad", "bd01:B9:0", "german"), Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance();

        // D9.4, D9.3 (ruling R25.9): the AFV's Hindrance and TEM for Infantry are not reviewed.
        Refused(await Do(GameActions.Fire, Once(2, 3), new
        {
            firers = R1,
            target = "bd01:B8:0"
        }), "play.fire-afv-hindrance");
        Refused(await Do(GameActions.Fire, Once(2, 3), new
        {
            firers = R1,
            target = "bd01:B9:0"
        }), "play.fire-afv-cover");

        // Infantry may not enter an enemy vehicle's Location (OVR and CC against a vehicle are not reviewed).
        await Advance();
        Refused(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = R1,
            to = "bd01:B9:0"
        }), "play.move-enemy-vehicle");
    }

    [Fact]
    public async Task AVehicleSetsUpWithAVcaInReviewedTerrainAloneAmongVehicles()
    {
        Refused(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A7:0", "german")), "play.setup-vehicle");
        Refused(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", facing: null)), "play.setup-vehicle");
        Refused(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "east", Conditions.Concealed)), "play.setup-vehicle");
        Refused(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german")),
            "play.setup-vehicle");
        Refused(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "defender-squad", "bd01:A8:0", "russian")),
            "play.setup-vehicle");
        Committed(await TrySetup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german")));
    }

    [Fact]
    public async Task ABerserkChargeAtAnEnemyVehicleEndsInPlace()
    {
        // The berserk German squad in B9 has the Russian truck in B8 ADJACENT: CC and OVR against a vehicle are not built, so the charge
        // is undecided and ends in place (rulings R25.3, R30.5); the MPh is not stuck.
        await Setup("german", Squad("g1", "attacker-squad", "bd01:B9:0", "german", Conditions.Berserk), Vehicle("ru-t", "defender-truck", "bd01:B8:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:A10:0", "german"));
        await Advance(2);
        var charge = Assert.Single(Planner().Charges(Current));
        Assert.Contains("play.berserk-vehicle", charge.Undecided, StringComparison.Ordinal);
        await Advance();
        Assert.Equal("dfph", Current.Phase);
    }

    [Fact]
    public async Task AVehicleWaitsForABerserkUnitsCharge()
    {
        // A15.43: the berserk squad in B9 charges the Russian squad in B10 before the German truck may start.
        await Setup("german", Squad("g1", "attacker-squad", "bd01:B9:0", "german", Conditions.Berserk), Squad("r1", "defender-squad", "bd01:B10:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"));
        await Advance(2);
        Refused(await Step("de-t", "start"), "play.berserk-first");
    }

    [Fact]
    public async Task ABuHalftrackMayStopInResidualFp()
    {
        // A8.222: Residual FP has no effect on a BU AFV, so a BU halftrack may spend MP where the DEFENDER's fire left it.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german", "east", Conditions.ButtonedUp),
            Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance(2);
        Committed(await Step("de-ht", "start"));
        await Pass();
        Committed(await Step("de-ht", "enter", to: "bd01:B8:0"));
        Committed(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = R1,
            target = "bd01:B8:0"
        }));
        Assert.Contains(Current.ResidualFire, item => item.Location == BoardLocation.Parse("bd01:B8:0"));
        await Pass();
        Committed(await Step("de-ht", "stop"));
    }

    [Fact]
    public async Task TheMandatoryFireGroupBindsAVehiclesMgAndItsLocationsInfantry()
    {
        // D3.5: Mandatory FG (A7.55) applies: the squad in A10 fires at B10, so the halftrack in A10 may not fire at B10 this phase.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", "bd01:A10:0", "german"), Squad("g1", "attacker-squad", "bd01:A10:0", "german"),
            Squad("r1", "defender-squad", "bd01:B10:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = G1,
            target = "bd01:B10:0"
        }));
        Refused(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = Halftrack,
            target = "bd01:B10:0"
        }), "play.fire-group");
    }

    [Fact]
    public async Task MotionCountsTheVcaChangesAndAnUnseenEnemyIsNotDisclosed()
    {
        // The truck in A8 faces west, off the board's edge: after Start it can still turn and enter a hex, so it may not end in Motion (D2.4).
        await Setup("german", Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "west"),
            Vehicle("de-t2", "attacker-truck", "bd01:A2:0", "german"), Squad("r1", "defender-squad", "bd01:B2:0", "russian", Conditions.Hidden));
        await Advance(2);
        Committed(await Step("de-t", "start"));
        await Pass();
        Refused(await Do(GameActions.EndMove, NoRoll(), new
        {
        }), "play.vehicle-motion");
        Committed(await Step("de-t", "stop"));
        await Pass();
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        await Pass();

        // A12: the hidden squad in B2 is not disclosed by a refused entry.
        Committed(await Step("de-t2", "start"));
        await Pass();
        var refused = await Step("de-t2", "enter", to: "bd01:B2:0");
        Refused(refused, "not decided here");
        Assert.DoesNotContain(refused.Reasons, reason => reason.Contains("an enemy unit is there", StringComparison.Ordinal));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
