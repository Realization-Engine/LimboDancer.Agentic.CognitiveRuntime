using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 50 in live play, Armored combat I: a tank's BMG and CMG on the IFT with its MA beside them (D3.5, D3.51, D3.52), and the repair of
/// each of its weapons with the Recall of a tank whose MA is disabled (D3.7). Board 01's hex facts, fixed dice, and a stub LOS reader. Every
/// read of the state replays the game, so each record written here is also checked by the projector.
/// </summary>
public sealed class BacklogPass50Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f550");
    private static readonly GameScope Scope = new(Tenant, "pass50");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] TankFirer = ["de-tank"];
    private static readonly string[] BothMgs = ["bmg", "cmg"];
    private static readonly string[] Bmg = ["bmg"];
    private static readonly string[] Cmg = ["cmg"];
    private const string Target = "bd01:B8:0";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass50-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private string[] specialRules = [];
    private int scenarioYear = 1942;

    public BacklogPass50Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range.
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

    private static Dictionary<string, object> Squad(string id, string at) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:squad",
        ["definition"] = "defender-squad",
        ["side"] = "russian",
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    private static Dictionary<string, object> Tank(string facing, params string[] states) => new()
    {
        ["id"] = "de-tank",
        ["kind"] = "asl:vehicle",
        ["definition"] = "attacker-tank",
        ["side"] = "german",
        ["position"] = new Dictionary<string, string> { ["at"] = "bd01:B10:0", ["facing"] = facing },
        ["conditions"] = states.ToDictionary(state => state, _ => true),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task Setup(params Dictionary<string, object>[] placements) =>
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 50",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = scenarioYear,
                ["specialRules"] = specialRules,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
            },
            placements,
        })));

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private async Task Advance() => Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
    {
    }));

    private Task<PlayResult> FireMgs(string[] mgs, params int[] dice) => Do(GameActions.Fire, Once(dice), new
    {
        firers = TankFirer,
        target = Target,
        weapons = new Dictionary<string, string[]> { ["de-tank"] = mgs },
    });

    private Task<PlayResult> FireMa(params int[] dice) => Do(GameActions.FireOrdnance, Once(dice), new
    {
        gunId = "de-tank",
        target = Target,
    });

    private Task<PlayResult> Step(string kind, string? turret = null)
    {
        var arguments = new Dictionary<string, string> { ["vehicleId"] = "de-tank", ["kind"] = kind };
        if (turret is not null)
        {
            arguments["turret"] = turret;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private Task<PlayResult> Repair(string weapon, int dr) => Do(GameActions.Repair, Once(dr), new
    {
        unitId = "de-tank",
        equipmentId = "de-tank",
        weapon,
    });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static decimal DrmOf(JsonElement drm, string prefix) => drm.EnumerateArray()
        .Where(item => item.GetProperty("name").GetString()!.StartsWith(prefix, StringComparison.Ordinal)).Sum(item => item.GetProperty("value").GetDecimal());

    [Fact]
    public async Task VehicleLowAmmoPersistsAndTheNextDisablingShotRecallsWithoutEliminatingTheTank()
    {
        specialRules = ["ammo-shortage:german"];
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        var before = Revision;
        var beforeState = Current;
        Committed(await FireMa(5, 6));
        Assert.Contains("de-tank", Current.LowAmmoVehicles);
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Malfunctioned));
        var first = LastShot(before);
        Assert.False(first.Facts.GetProperty("vehicleBreakdown").GetProperty("lowAmmo").GetBoolean());
        var forged = JsonSerializer.SerializeToNode(first.Facts)!.AsObject();
        forged["vehicleBreakdown"]!["lowAmmo"] = true;
        var rolls = Since(before).Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(item => item.Roll);
        Assert.NotNull(OrdnanceRecordVerifier.Shared.Verify(beforeState, first with
        {
            Facts = JsonSerializer.SerializeToElement(forged)
        }, rolls));

        for (var phase = 0; phase < 10; phase++)
        {
            await Advance();
        }
        Assert.Equal("dfph", Current.Phase);
        before = Revision;
        Committed(await FireMa(5, 6));
        Assert.True(LastShot(before).Facts.GetProperty("vehicleBreakdown").GetProperty("lowAmmo").GetBoolean());
        var tank = Current.Unit("de-tank")!;
        Assert.Equal(InstanceStatus.Active, tank.Status);
        Assert.True(Is(tank, Conditions.Disabled));
        Assert.True(Is(tank, Conditions.Recalled));
        Assert.True(Is(tank, Conditions.ButtonedUp));
        Assert.False(Is(tank, Conditions.StunRecovery));
        var revision = Revision;
        Refused(await FireMa(), "play.vehicle-weapon-disabled");
        Assert.Equal(revision, Revision);
    }

    [Fact]
    public async Task LegacyVehicleShotsRebuildWithoutNewEvidenceOrDisabledRefusal()
    {
        await Setup(Tank("north-east", Conditions.Disabled), Squad("r1", Target));
        await Advance();
        var target = BoardLocation.Parse(Target);
        var current = LiveOrdnance.FromState(Current, "de-tank", target);
        Assert.Null(current.Shot);
        Assert.Contains("vehicle-weapon-disabled", current.Reason);
        var legacy = LiveOrdnance.FromState(Current, "de-tank", target, legacyVehicleBreakdown: true);
        Assert.NotNull(legacy.Shot);
        Assert.Null(legacy.Shot.VehicleBreakdown);
    }

    [Fact]
    public async Task ATanksBmgAndCmgFireTogetherAndItsMaFiresAfterThemInTheSamePhase()
    {
        // The PzKpfw IIIH in B10 faces north-east, so the squad in B8 lies in its VCA and TCA. D3.5: 3 + 5 FP in one attack.
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        var before = Revision;
        Committed(await FireMgs(BothMgs, 5, 6));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(8m, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.Null(fire.TurretFacing);

        // The state keeps which weapons fired; the counter is the vehicle's.
        var state = Current;
        Assert.Equal([("bmg", "pfph", 0), ("cmg", "pfph", 0)], state.VehicleWeaponShots.Select(item => (item.Weapon, item.FirePhase, item.Turn)));
        Assert.True(Is(state.Unit("de-tank")!, Conditions.PrepFire));

        // D3.5: each MG fires once a Player Turn.
        Refused(await FireMgs(Bmg, 5, 6), "firer-already-fired");

        // "It makes no difference which weapon fires first": the MA fires in the same phase, under the counter its MGs placed.
        Committed(await FireMa(6, 5));
        Assert.Contains(Current.VehicleWeaponShots, item => item.Weapon == "ma" && item.FirePhase == "pfph");

        // In the AFPh the tank has forfeited what did not fire in the PFPh.
        await Advance();
        await Advance();
        await Advance();
        Assert.Equal("afph", Current.Phase);
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(6, 5)).Outcome);
    }

    [Fact]
    public async Task MainArmamentMgGainsLowAmmoThenDisablesWithoutRecallingAPassengerVehicle()
    {
        specialRules = ["ammo-shortage:german"];
        var halftrack = Tank("north-east");
        halftrack["definition"] = "attacker-halftrack";
        halftrack["conditions"] = new Dictionary<string, bool> { [Conditions.ButtonedUp] = false };
        await Setup(halftrack, Squad("r1", Target));
        await Advance();
        var before = Revision;
        var beforeState = Current;
        Committed(await FireMgs([], 5, 6));
        Assert.Contains("de-tank", Current.LowAmmoVehicles);
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Malfunctioned));
        var first = Assert.Single(Since(before).Select(item => item.Payload).OfType<FireResolved>());
        var forged = JsonSerializer.SerializeToNode(first.Facts)!.AsObject();
        forged["vehicleBreakdown"]!["weapons"]!["ma"]!["lowAmmo"] = true;
        var rolls = Since(before).Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(item => item.Roll);
        Assert.NotNull(FireRecordVerifier.Shared.Verify(beforeState, first with
        {
            Facts = JsonSerializer.SerializeToElement(forged)
        }, rolls));
        for (var phase = 0; phase < 10; phase++)
        {
            await Advance();
        }
        Committed(await FireMgs([], 5, 6));
        var vehicle = Current.Unit("de-tank")!;
        Assert.True(Is(vehicle, Conditions.Disabled));
        Assert.False(Is(vehicle, Conditions.Recalled));
        before = Revision;
        Assert.NotEqual(PlayOutcome.Committed, (await FireMgs([])).Outcome);
        Assert.Equal(before, Revision);
        var legacy = LiveFire.FromState(Current, TankFirer, [], BoardLocation.Parse(Target), legacyVehicleBreakdown: true);
        Assert.NotNull(legacy.Attack);
        Assert.Null(legacy.Attack.VehicleBreakdown);
    }

    [Fact]
    public async Task ACmgFiredOutsideTheTcaTurnsTheTurretWithCaseAAndTheMaPaysTheSame()
    {
        // Facing south-east the tank has B8 outside both of its CAs.
        await Setup(Tank("south-east"), Squad("r1", Target));
        await Advance();

        // D1.81: the BMG fires only inside the VCA.
        Refused(await FireMgs(Bmg, 5, 6), "play.fire-vehicle-vca");

        var before = Revision;
        Committed(await FireMgs(Cmg, 5, 6));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var turn = fire.Facts.GetProperty("vehicleFire").GetProperty("weapons")[0].GetProperty("turn").GetInt32();
        Assert.InRange(turn, 1, 3);

        // D3.52: a T turret's Case A is one a hexspine, added to the IFT DR.
        Assert.Equal(turn, DrmOf(fire.Resolution.GetProperty("arithmetic").GetProperty("drm"), "case-a:"));

        // D3.12: the turret turned and the hull did not.
        var state = Current;
        Assert.Equal(fire.TurretFacing, state.TurretFacings.Single(item => item.Vehicle == "de-tank").Facing);
        Assert.Equal(UnitFacing.SouthEast, ((MapPosition)state.Unit("de-tank")!.Position!).Facing);
        Assert.Equal(turn, Assert.Single(state.VehicleWeaponShots).Turn);

        // D3.51: the MA's first shot within the TCA the CMG changed pays the same Case A.
        before = Revision;
        Committed(await FireMa(6, 5));
        var shot = Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        Assert.Equal(turn, DrmOf(shot.Resolution.GetProperty("toHit").GetProperty("drm"), "case-a:maintained"));
        Assert.Null(shot.Facing);
    }

    [Fact]
    public async Task ATankRepairsEachWeaponByItsOwnDrAndIsRecalledWhenItsMaIsDisabled()
    {
        await Setup(Tank("north-east", Conditions.Malfunctioned, Conditions.BmgMalfunctioned, Conditions.CmgMalfunctioned), Squad("r1", Target));
        Assert.Equal("rph", Current.Phase);

        // D3.7: a dr of 1 repairs the BMG; one attempt a RPh for each weapon; a BU crew repairs it (only an AAMG asks for a CE crew).
        var before = Revision;
        Committed(await Repair("bmg", 1));
        Assert.Equal("bmg", Since(before).Select(item => item.Payload).OfType<RepairAttempted>().Single().Weapon);
        var state = Current;
        Assert.False(Is(state.Unit("de-tank")!, Conditions.BmgMalfunctioned));
        Assert.True(Is(state.Unit("de-tank")!, Conditions.CmgMalfunctioned));
        Refused(await Repair("bmg", 1), "play.repair-weapon");

        // A dr of 2 to 5 changes nothing, and the CMG has had its attempt.
        Committed(await Repair("cmg", 3));
        Refused(await Repair("cmg", 1), "play.repair-unit");

        // A 6 disables the MA, and a tank carries no Passengers and tows nothing: it is Recalled (D5.341).
        Committed(await Repair("ma", 6));
        var tank = Current.Unit("de-tank")!;
        Assert.True(Is(tank, Conditions.Disabled));
        Assert.True(Is(tank, Conditions.Malfunctioned));
        Assert.True(Is(tank, Conditions.Recalled));
    }

    [Fact]
    public async Task HeroRiderRepairsTheButtonedUpAamgAndForgedParticipantsDoNotReplay()
    {
        scenarioYear = 1943;
        var halftrack = Tank("north-east", Conditions.Malfunctioned, Conditions.ButtonedUp);
        halftrack["definition"] = "attacker-halftrack";
        var hero = new Dictionary<string, object>
        {
            ["id"] = "hero",
            ["kind"] = "asl:leader",
            ["definition"] = "attacker-leader-8-0",
            ["side"] = "german",
            ["position"] = new Dictionary<string, string> { ["in"] = "de-tank", ["role"] = "rider" },
            ["conditions"] = new Dictionary<string, bool> { [Conditions.Broken] = false, [Conditions.Captured] = false, [Conditions.Heroic] = true },
        };
        await Setup(halftrack, hero, Squad("r1", Target));
        var before = Revision;
        Refused(await Repair("ma", 1), "play.repair-unit");
        Assert.Equal(before, Revision);
        Assert.Equal("hero", Assert.Single(Planner().VehicleRepairHeroes(Current, Current.Unit("de-tank")!, "ma")).Id);
        Committed(await Do(GameActions.Repair, Once(1), new
        {
            unitId = "de-tank",
            equipmentId = "de-tank",
            weapon = "ma",
            hero = "hero",
        }));
        var record = Assert.Single(Since(before).Select(item => item.Payload).OfType<RepairAttempted>());
        Assert.Equal(1, record.VehicleRepairVersion);
        Assert.Equal("hero", record.Hero);
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Malfunctioned));
        foreach (var forged in new[] { record with { Hero = "r1" }, record with { VehicleRepairVersion = 2 }, record with { VehicleRepairVersion = null } })
        {
            var events = store.Read(Scope)!.Events.Select(item => item.Payload is RepairAttempted ? item with { Payload = forged } : item).ToArray();
            Assert.True(Planner().Replay(events).HasErrors);
        }
    }

    [Fact]
    public async Task RecallGathersTheActualSecondaryStateAndCapacityExceptions()
    {
        // Synthetic SA presence facts exercise gathering without inventing a historical counter's armament.
        await Setup(Tank("north-east", Conditions.SaDisabled), Squad("r1", Target));
        var vehicle = Current.Unit("de-tank")!;
        Assert.True(VehicleArmamentFacts.RecalledAfterDisable(vehicle, true, true, 0, 0));
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(vehicle, false, true, 0, 0));
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(vehicle, true, null, 0, 0));
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(vehicle, true, true, 1, 0));
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(vehicle, true, true, 0, 1));
        var partial = vehicle with
        {
            Conditions = new Dictionary<string, ConditionState>(vehicle.Conditions) { [Conditions.SaDisabled] = ConditionState.False }
        };
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(partial, true, true, 0, 0));
        Assert.True(VehicleArmamentFacts.RecalledAfterDisable(partial, true, true, 0, 0, secondaryJustDisabled: true));
        Assert.False(VehicleArmamentFacts.RecalledAfterDisable(partial with
        {
            Conditions = new Dictionary<string, ConditionState>(partial.Conditions) { [Conditions.SaDisabled] = ConditionState.Unknown }
        }, true, true, 0, 0));
    }

    [Fact]
    public async Task LegacyAamgAttemptAndCapturedInherentCrewRemainRecognized()
    {
        var halftrack = Tank("north-east", Conditions.Malfunctioned);
        halftrack["definition"] = "attacker-halftrack";
        await Setup(halftrack, Squad("r1", Target));
        var state = Current;
        var vehicle = state.Unit("de-tank")!;
        var definition = Catalog.Definition("attacker-halftrack")!;
        var weapon = Assert.Single(VehicleArmamentFacts.Weapons(definition));
        var captured = vehicle with
        {
            TemporaryCrew = new TemporaryCrew(ScenarioA1CapturedVehicleRules.InfantryCrew, true)
        };
        Assert.Null(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(VehicleArmamentFacts.Repair(state, captured, definition, weapon, null)));
        Committed(await Repair("ma", 3));
        var events = store.Read(Scope)!.Events.Select(item => item.Payload is RepairAttempted repair
            ? item with
            {
                Payload = repair with
                {
                    VehicleRepairVersion = null,
                    Weapon = null
                }
            } : item).ToArray();
        var legacy = Planner().Replay(events);
        Assert.False(legacy.HasErrors);
        Assert.Contains("de-tank", legacy.Current!.RepairsThisPhase);
        Assert.NotNull(Planner().VehicleRepairRefusal(legacy.Current, legacy.Current.Unit("de-tank")!, "ma"));
        Assert.Empty(Planner().VehicleRepairWeapons(legacy.Current, legacy.Current.Unit("de-tank")!));
    }

    [Fact]
    public async Task ATanksMaFiresAtTheOutsetOfItsMphSpendsADelayMpAndFiresAgain()
    {
        // D3.3, C5.33: at the outset of its MPh the MA fires with Case C (+4 for a T turret). A colored dr of 2 keeps the ROF of 2: no counter.
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Assert.Equal("mph", Current.Phase);
        var before = Revision;
        Committed(await FireMa(2, 6));
        var first = LastShot(before);
        Assert.Equal(4, DrmOf(first.Resolution.GetProperty("toHit").GetProperty("drm"), "case-c"));
        Assert.Equal("bounding-first-fire", first.Facts.GetProperty("fireKind").GetString());
        var state = Current;
        Assert.False(Is(state.Unit("de-tank")!, Conditions.BoundingFire));
        Assert.Equal(0, Assert.Single(state.VehicleWeaponShots).Step);

        // C5.3: another MP before the next shot; D2.17: a Delay MP will do, and the DEFENDER may answer it.
        Refused(await FireMa(2, 6), "play.ordnance-bounding-mp");
        Committed(await Step("delay"));
        Refused(await FireMa(2, 6), "play.ordnance-bounding");
        await Pass();

        // The second shot loses the ROF on a colored 5: a Bounding Fire counter (D3.3), and no shot after it.
        Committed(await FireMa(5, 6));
        state = Current;
        Assert.True(Is(state.Unit("de-tank")!, Conditions.BoundingFire));
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);

        // A tank that only spent Delay MP ends its move stopped, not in Motion (D2.4), and does not fire in the AFPh (D3.3).
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            await Pass();
        }

        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Motion));
        await Advance();
        await Advance();
        Assert.Equal("afph", Current.Phase);
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);
    }

    [Fact]
    public async Task TheMaThatKeptItsRofInTheMphFiresOnceInTheAfphWithCaseC()
    {
        // D3.32: the MA kept its ROF in Bounding First Fire and no other weapon fired: one shot in the AFPh, with Case C and not Case B.
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Committed(await FireMa(2, 6));
        await Advance();
        await Advance();
        Assert.Equal("afph", Current.Phase);
        var before = Revision;
        Committed(await FireMa(2, 6));
        var drm = LastShot(before).Resolution.GetProperty("toHit").GetProperty("drm");
        Assert.Equal(4, DrmOf(drm, "case-c"));
        Assert.Equal(0, DrmOf(drm, "case-b"));
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);
    }

    [Fact]
    public async Task TheTurretTurnsWithADelayMpAndNotWithAStart()
    {
        // D3.12: "the TCA may also change freely with each MP expended during the MPh".
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Refused(await Step("start", "east"), "play.move-vehicle-turret");
        Committed(await Step("delay", "east"));
        var state = Current;
        Assert.Equal(UnitFacing.East, state.TurretFacings.Single(item => item.Vehicle == "de-tank").Facing);
        Assert.Equal(UnitFacing.NorthEast, ((MapPosition)state.Unit("de-tank")!.Position!).Facing);
        Assert.True(state.Movement is { WindowOpen: true, Stopped: true, Started: false });

        // C5.13: the MA now fires only within the TCA it was turned to: the squad in B8 lies outside it.
        await Pass();
        Refused(await FireMa(2, 6), "play.ordnance-bounding-arc");
    }
}
