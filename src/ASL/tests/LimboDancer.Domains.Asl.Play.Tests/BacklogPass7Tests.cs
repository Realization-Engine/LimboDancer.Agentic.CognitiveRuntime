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
/// The backlog pass 7 in live play: a tank's MA fire at an enemy tank on the Vehicle Target Type, its turret and TCA, the To Kill results,
/// Shock and the Unconfirmed Kill in the RPh, Special Ammunition and its depletion, and the crews' fates (rulings R7.1 to R7.11). Board 01's
/// hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass7Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f507");
    private static readonly GameScope Scope = new(Tenant, "pass7");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] GH = ["gh"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] R1R2 = ["r1", "r2"];
    private static readonly string[] R3 = ["r3"];
    private static readonly string[] HalftrackIds = ["de-ht"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass7-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public BacklogPass7Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range, except to the Locations named blocked.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public HashSet<BoardLocation> Blocked { get; } = [];

        public Dictionary<(string From, string To), LosCrossedHex[]> Crossed { get; } = [];

        public Dictionary<(string From, string To), LosHindrance[]> Hindrances { get; } = [];

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => Blocked.Contains(target) || Blocked.Contains(from)
            ? new(LosStatus.Blocked, true, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty)
            : new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty)
            {
                Crossed = Crossed.GetValueOrDefault((from.Hex.ToString(), target.Hex.ToString())) ?? [],
                Hindrances = Hindrances.GetValueOrDefault((from.Hex.ToString(), target.Hex.ToString())) ?? [],
            };
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

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side, params string[] states)
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
                at
            },
            ["conditions"] = conditions
        };
    }

    private static Dictionary<string, object> Squad(string id, string at, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", at, side, states);

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing = "east", params string[] states)
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
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Gun(string id, string definition, string at, string facing, string crew, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:gun",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
        ["holding"] = new
        {
            holder = crew,
            role = "manned"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task Setup(string firstSide, int? month, int? year, params Dictionary<string, object>[] placements)
    {
        var start = new Dictionary<string, object>
        {
            ["label"] = "Pass 7",
            ["catalog"] = "asl-scenario-a1@1.11.0",
            ["boards"] = Bd01,
            ["firstSide"] = firstSide,
            ["sides"] = new object[]
            {
                new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
            },
        };
        if (month is { } value)
        {
            start["scenarioMonth"] = value;
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
    }

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

    private Task<PlayResult> Move(string[] units, string to, bool doubleTime = false, DiceRoller? roller = null) =>
        Do(GameActions.Move, roller ?? NoRoll(), new
        {
            unitIds = units,
            to,
            doubleTime
        });

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private async Task EndMove() => Committed(await Do(GameActions.EndMove, NoRoll(), new
    {
    }));

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

    private Task<PlayResult> Choose(string key, string option, DiceRoller? roller = null) => Do(GameActions.Choose, roller ?? NoRoll(), new
    {
        key,
        option
    });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static BoardLocation At(string location) => BoardLocation.Parse(location);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static Dictionary<string, object> Dummy(string id, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    private async Task<PlayResult> TrySetup(int month, params Dictionary<string, object>[] placements) => await Commit(Play(), GameActions.Setup,
        JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Guid.NewGuid():N}",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 7",
                ["catalog"] = "asl-scenario-a1@1.11.0",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = month,
            },
            placements,
        }));

    private Task<PlayResult> Fire(string[] firers, string target, params int[] dice) => Do(GameActions.Fire, Once(dice), new
    {
        firers,
        target
    });

    // Ends the moving vehicle's move: its MP left are spent in its hex, and the DEFENDER passes (R5.15).
    private async Task FinishVehicle()
    {
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            await Pass();
        }
    }

    private static JsonElement Drm(FireResolved record) => record.Resolution.GetProperty("arithmetic").GetProperty("drm");

    private static bool HasDrm(FireResolved record, string name, decimal value) =>
        Drm(record).EnumerateArray().Any(item => item.GetProperty("name").GetString() == name && item.GetProperty("value").GetDecimal() == value);

    private static LosCrossedHex Crossing(string hex, int range) => new(At($"bd01:{hex}:0").Board, At($"bd01:{hex}:0").Hex, range);

    private FireResolved LastFire(long before) => Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();

    private Task<PlayResult> FireAt(string gun, string target, string? vehicle, string? ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, string> { ["gunId"] = gun, ["target"] = target };
        if (vehicle is not null)
        {
            arguments["targetVehicle"] = vehicle;
        }

        if (ammunition is not null)
        {
            arguments["ammunition"] = ammunition;
        }

        return Do(GameActions.FireOrdnance, dice.Length == 0 ? NoRoll() : Once(dice), arguments);
    }

    private OrdnanceResolution LastShot(long before) =>
        Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json)!;

    // The German PzKpfw IIIH in B10 faces north-east, so the T-34 in B8 lies in its TCA; the T-34 faces east, showing the Germans its side.
    private Task Tanks(int? year = 1942, string germanFacing = "north-east", string russianFacing = "east", params Dictionary<string, object>[] others) =>
        Setup("german", 7, year, [Vehicle("de-tank", "attacker-tank", "bd01:B10:0", "german", germanFacing),
            Vehicle("ru-tank", "defender-tank", "bd01:B8:0", "russian", russianFacing), .. others]);

    [Fact]
    public async Task ATankFiresApAtAnEnemyTanksSideAndBurnsIt()
    {
        // C3.31: the black Vehicle row at 2 hexes is 10; Case I +1 for the BU PzKpfw IIIH, Case L -1: 4+2 = 6 hits the hull (colored 4 not
        // below white 2). D3.2: the LOS from B10 crosses the T-34's side. AP 50 is 11, Case D +1 at 2 hexes, less the side AF 6: 6; a TK
        // DR of 2 is at most half of it: a burning wreck with its Blaze (C7.7, D10.1).
        await Tanks();
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 4, 2, 1, 1));
        var shot = LastShot(before);
        Assert.Equal((10, 6, true), (shot.ToHit!.ModifiedToHit, shot.ToHit.FinalDr, shot.ToHit.Hit));
        Assert.Equal(("hull", "side", 6, OrdnanceKill.Burn), (shot.Kill!.HitLocation, shot.Kill.TargetFacing, shot.Kill.FinalTk, shot.Kill.Result));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-tank")!.Status);
        Assert.Contains(Current.Entities, entity => entity.Id == GamePlanner.BlazeId("ru-tank") && entity.Status == InstanceStatus.Active);
        Assert.True(Is(Current.Unit("de-tank")!, Conditions.PrepFire));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph()
    {
        // C5.1, D3.12: facing south-east, the PzKpfw IIIH turns its turret two hexspines to north-east (T: +2), its hull unmoved; colored 1
        // below white 3 strikes the T-34's turret, facing the firer with its side (AF 6): TK 6, and a TK DR of 6 Shocks it (C7.7). Its colored
        // 1 is within ROF 2, the turn not lowering a vehicle's ROF.
        await Tanks(germanFacing: "south-east");
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 1, 3, 3, 3));
        var shot = LastShot(before);
        Assert.Contains(shot.ToHit!.Drm, item => item.Name == "case-a:2" && item.Value == 2);
        Assert.Equal(("turret", OrdnanceKill.Shock), (shot.Kill!.HitLocation, shot.Kill.Result));
        Assert.True(shot.Gun!.RateOfFireKept);

        // C6.5: the tank keeps its Acquisition of the target for its next shot (table player, item 4).
        Assert.Contains(Current.Acquisitions, item => item.Gun == "de-tank" && item.Level == -1);
        Assert.Equal(Units.Documents.UnitFacing.NorthEast, LiveOrdnance.TurretFacing(Current, Current.Unit("de-tank")!));
        Assert.Equal(Units.Documents.UnitFacing.SouthEast, ((MapPosition)Current.Unit("de-tank")!.Position).Facing);
        var shocked = Current.Unit("ru-tank")!;
        Assert.True(Is(shocked, Conditions.Shocked) && Is(shocked, Conditions.ButtonedUp));

        // The Russian RPh: the Shocked T-34 rolls before the phase ends; a 3 makes it an Unconfirmed Kill (C7.42).
        await Advance(7);
        Assert.Equal(("rph", "russian"), (Current.Phase, Current.PhasingSide));
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.shock-recovery-pending");
        Committed(await Do(GameActions.RecoverShock, Once(3), new
        {
            vehicleId = "ru-tank"
        }));
        Assert.True(Is(Current.Unit("ru-tank")!, Conditions.UnconfirmedKill) && !Is(Current.Unit("ru-tank")!, Conditions.Shocked));
        Refused(await Do(GameActions.RecoverShock, Once(1), new
        {
            vehicleId = "ru-tank"
        }), "play.shock-vehicle");

        // An Unconfirmed Kill is still Shocked (C7.42): it does not fire or move (table player, item 1); in the next RPh a 5 wrecks it, with
        // no crew.
        await Advance();
        Refused(await FireAt("ru-tank", "bd01:B10:0", "de-tank", "ap"), "vehicle-fire-outside");
        await Advance();
        Assert.Equal("mph", Current.Phase);
        Refused(await Step("ru-tank", "start"), "Unconfirmed Kill");
        await Advance(6);
        Committed(await Do(GameActions.RecoverShock, Once(5), new
        {
            vehicleId = "ru-tank"
        }));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-tank")!.Status);
        Assert.DoesNotContain(Current.Units, unit => unit.Kind == "asl:crew" && unit.Side == "russian");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ApcrAboveItsDepletionNumberIsNotFiredAndRunsOut()
    {
        // C8.9 (R7.6): the PzKpfw IIIH's APCR is A5 in 1942; an Original 6 finds none: nothing fired, no marker, and none for the rest of
        // the scenario; AP may still fire.
        await Tanks();
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr", 4, 2));
        Assert.Equal("none", LastShot(before).AmmunitionUse);
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "de-tank" && item.Ammunition == "apcr");
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.PrepFire));
        Refused(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr"), "ammunition-depleted");
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 6, 5));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task SpecialAmmunitionNeedsAScenarioYear()
    {
        // R7.6: with no scenario year, APCR is refused.
        await Tanks(year: null);
        await Advance();
        Refused(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr"), "ammunition-unavailable");
    }

    [Fact]
    public async Task AnEliminatedTanksCrewMaySurvive()
    {
        // D5.6: a TK DR of 5 below the TK 6 eliminates the T-34 (CS# 5); a Crew Survival DR of 4 places a Russian crew beneath the wreck.
        await Tanks();
        await Advance();
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 4, 2, 2, 3, 2, 2));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-tank")!.Status);
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:crew" && unit.Side == "russian");
        Assert.Equal(At("bd01:B8:0"), Current.Location(crew.Id)!.Location);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnImmobilizedTanksCrewMayAbandonIt()
    {
        // D5.5: a TK DR equal to the TK immobilizes the hull; the crew's TC (Elite morale 8) fails on 11 and the crew Abandons it.
        await Tanks();
        await Advance();
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 4, 2, 3, 3, 5, 6));
        var abandoned = Current.Unit("ru-tank")!;
        Assert.True(Is(abandoned, Conditions.Immobilized) && Is(abandoned, Conditions.Abandoned));
        Assert.Single(Current.Units, unit => unit.Kind == "asl:crew" && unit.Side == "russian" && unit.Status == InstanceStatus.Active);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AVehicleInTheTargetLocationMustBeNamedAndMustBeAnEnemy()
    {
        // C3.31 (R7.2): a Location holding a vehicle is fired at on the Vehicle Target Type, naming it; a friendly vehicle is no target.
        await Tanks(others: [Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "east")]);
        await Advance();
        Refused(await FireAt("de-tank", "bd01:B8:0", null, null), "play.ordnance-vehicle");
        Refused(await FireAt("de-tank", "bd01:A8:0", "de-t", "ap"), "play.ordnance-vehicle-target");
    }

    [Fact]
    public async Task AClosedToppedTanksCrewIsButtonedUpUnlessExposed()
    {
        // D5.2 (R7.11): a CT AFV is BU by default: Case I +1; exposed, its RST MA may not fire (D1.321).
        await Tanks();
        Assert.False(LiveFire.CrewExposed(Current.Unit("ru-tank")!));
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 6, 5));
        Assert.Contains(LastShot(before).ToHit!.Drm, item => item.Name == "case-i" && item.Value == 1);
    }

    [Fact]
    public async Task AnAbandonedTankDoesNotFire()
    {
        // D5.41 (table player, item 2): an Abandoned AFV has no crew to fire its MA.
        await Setup("german", 7, 1942, Vehicle("de-tank", "attacker-tank", "bd01:B10:0", "german", "north-east", Conditions.Abandoned),
            Vehicle("ru-tank", "defender-tank", "bd01:B8:0", "russian", "east"));
        await Advance();
        Refused(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap"), "play.ordnance-abandoned");
    }

    [Fact]
    public async Task AConcealedTankThatFiresLosesItsQuestionMark()
    {
        // A12.14 (table player, item 3): the tank in grain in July fires and loses its "?".
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("B10", "Grain"))]);
        await Setup("german", 7, 1942, Vehicle("de-tank", "attacker-tank", "bd01:B10:0", "german", "north-east", Conditions.Concealed),
            Vehicle("ru-tank", "defender-tank", "bd01:B8:0", "russian", "east"));
        Assert.True(Is(Current.Unit("de-tank")!, Conditions.Concealed));
        await Advance();
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "ap", 6, 5));
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Concealed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AMalfunctionOnMissingApcrCountsAsFire()
    {
        // C8.9 (table player, item 6): an Original 12 finds no APCR above its Depletion Number, but the MA malfunctions (B# 12), and the tank
        // has fired.
        await Tanks();
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr", 6, 6));
        var shot = LastShot(before);
        Assert.Equal(("none", true), (shot.AmmunitionUse, shot.Gun!.Malfunctioned));
        var tank = Current.Unit("de-tank")!;
        Assert.True(Is(tank, Conditions.Malfunctioned) && Is(tank, Conditions.PrepFire));
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "de-tank" && item.Ammunition == "apcr");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }
}
