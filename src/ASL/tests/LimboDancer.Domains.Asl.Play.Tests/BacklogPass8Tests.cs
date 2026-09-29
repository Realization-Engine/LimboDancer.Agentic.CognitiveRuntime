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
/// The backlog pass 8 in live play: a Gun's Defensive First Fire in the MPh and its First Fire counter (R8.1), Intensive Fire (R8.2), Guns
/// and their crews as targets (R8.3), and a crew's own fire (R8.4). Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass8Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f508");
    private static readonly GameScope Scope = new(Tenant, "pass8");
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

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass8-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public BacklogPass8Tests() => store = new FileGameStore(root);

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
            ["label"] = "Pass 8",
            ["catalog"] = "asl-scenario-a1@1.10.0",
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
                ["label"] = "Pass 8",
                ["catalog"] = "asl-scenario-a1@1.10.0",
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

    private Task<PlayResult> FireAt(string gun, string target, string? vehicle, string? ammunition, params int[] dice) =>
        FireAt(gun, target, vehicle, ammunition, false, dice);

    private Task<PlayResult> FireAt(string gun, string target, string? vehicle, string? ammunition, bool intensive, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = target };
        if (intensive)
        {
            arguments["intensive"] = true;
        }

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

    private static Dictionary<string, object> Crew(string id, string at, string side) => Unit(id, "asl:crew", side == "german" ? "attacker-crew" : "defender-crew", at, side);

    private OrdnanceShot LastFacts(long before) =>
        Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Facts.Deserialize<OrdnanceShot>(LiveFire.Json)!;

    [Fact]
    public async Task AGunDefensiveFirstFiresAtAMovingSquadAndIsMarkedFirstFire()
    {
        // C6.13, C6.14 (R8.1): the Russian 45mm in B10 fires at the German squad entering Open Ground B8 by non-Assault Movement: Cases J3 and
        // J4; 6 and 5, -1 -1 -1 (Case L) = 8 hits at TH# 8, the IFT DR of 12 does nothing, and the colored 6 above ROF 3 leaves a First Fire
        // counter on the Gun and its crew.
        await Setup("german", 7, 1942, Squad("g1", "bd01:A8:0", "german"), Crew("ru-crew", "bd01:B10:0", "russian"),
            Gun("ru-gun", "defender-at-gun", "bd01:B10:0", "north-east", "ru-crew", "russian"));
        await Advance(2);
        Committed(await Move(["g1"], "bd01:B8:0"));
        var before = Revision;
        Committed(await FireAt("ru-gun", "bd01:B8:0", null, null, 6, 5, 6, 6));
        var facts = LastFacts(before);
        Assert.Equal(("first-fire", true, true), (facts.FireKind, facts.Movement!.NonAssault, facts.Movement.OpenGround));
        var shot = LastShot(before);
        Assert.Contains(shot.ToHit!.Drm, item => item.Name == "case-j3");
        Assert.Contains(shot.ToHit.Drm, item => item.Name == "case-j4");
        Assert.True(Is(Current.Find("ru-gun")!, Conditions.FirstFire) && Is(Current.Unit("ru-crew")!, Conditions.FirstFire));

        // C2.241, C6.17: marked First Fire, the Gun fires once more only as Intensive Fire, and only while the MF spent there allow it.
        Refused(await FireAt("ru-gun", "bd01:B8:0", null, null, 6, 5), "gun-already-fired");
        Refused(await FireAt("ru-gun", "bd01:B8:0", null, null, true, 6, 5), "first-fire-limit");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AGunThatUsedItsRofFiresOnceMoreAsIntensiveFire()
    {
        // C5.6, C5.61, C5.62 (R8.2): the German leIG misses in the PFPh (colored 5 above ROF 2), then Intensive Fires with Case F +2 and is
        // marked Intensive Fire; it fires no more.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:B10:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:B10:0", "north-east", "de-crew", "german"),
            Squad("r1", "bd01:B8:0", "russian"));
        await Advance();
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 5, 6));
        Refused(await FireAt("de-gun", "bd01:B8:0", null, null, 5, 6), "gun-already-fired");
        var before = Revision;
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, true, 5, 6));
        Assert.Contains(LastShot(before).ToHit!.Drm, item => item.Name == "case-f" && item.Value == 2);
        Assert.True(Is(Current.Find("de-gun")!, Conditions.IntensiveFire));
        Refused(await FireAt("de-gun", "bd01:B8:0", null, null, true, 5, 6), "gun-already-fired");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task InfantryFireAtAnEmplacedGunsCrewTakesPlusTwoAndAnHeKiaDestroysTheGun()
    {
        // C11.2, C11.5 (R8.3): the Russian crew alone with its Emplaced 45mm in Open Ground B8 takes +2 against the German squad in B10.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Crew("de-crew", "bd01:B10:0", "german"),
            Gun("de-gun", "attacker-inf-gun", "bd01:B10:0", "north-east", "de-crew", "german"), Crew("ru-crew", "bd01:B8:0", "russian"), Gun("ru-gun", "defender-at-gun", "bd01:B8:0", "south-east", "ru-crew", "russian"));
        await Advance();
        var before = Revision;
        Committed(await Fire(["g1"], "bd01:B8:0", 6, 5));
        Assert.True(HasDrm(LastFire(before), "emplacement:ru-gun", 2m), Drm(LastFire(before)).ToString());

        // C11.4, C11.6: the leIG's HE hit (DR 2 and 3, +1 Small, +2 Emplacement = 8 against TH# 8) and an IFT DR of 2 destroy the Gun and its crew.
        before = Revision;
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 2, 3, 1, 1, 3));
        Assert.Equal("destroyed", LastShot(before).GunTargetFate);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("ru-gun")!.Status);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-crew")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ACrewFiresItsOwnFpUnlessItFiredItsGun()
    {
        // A7.352 (R8.4): the German crew fires its inherent FP at the Russian squad; the other crew, having fired its Gun, may not.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:B10:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:B10:0", "north-east", "de-crew", "german"),
            Crew("de-c2", "bd01:B10:0", "german"), Squad("r1", "bd01:B8:0", "russian"));
        await Advance();
        Committed(await Fire(["de-c2"], "bd01:B8:0", 6, 5));
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 5, 6));
        Refused(await Fire(["de-crew"], "bd01:B8:0", 6, 5), "fire");
    }

    private static readonly string[] CrewIds = ["de-crew"];

    private Task<PlayResult> Push(string crew, string to, string gun, params int[] dice) => Do(GameActions.Move, Once(dice), new Dictionary<string, object>
    {
        ["unitIds"] = new[] { crew },
        ["to"] = to,
        ["pushGun"] = gun,
    });

    [Fact]
    public async Task AGunTurnsWithoutFiringAndThenFiresAndMovesNoMore()
    {
        // C3.22 (R8.9): in the PFPh the leIG turns to north-west; it fires no more this phase, and it and its crew do not move this Player Turn.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Squad("r1", "bd01:J9:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.TurnGun, NoRoll(), new
        {
            gunId = "de-gun",
            facing = "north-west"
        }));
        Assert.Equal(Units.Documents.UnitFacing.NorthWest, ((MapPosition)((EquipmentInstance)Current.Find("de-gun")!).Position).Facing);
        Refused(await Do(GameActions.TurnGun, NoRoll(), new
        {
            gunId = "de-gun",
            facing = "east"
        }), "play.turn-gun");
        await Advance();
        Refused(await Move(["de-crew"], "bd01:B8:0"), "play.move-halted");
    }

    [Fact]
    public async Task ACrewPushesItsGunOrStaysOnAHighManhandlingDr()
    {
        // C10.3 (R8.6): pushing into Open Ground costs 2 MF, the DRM +2; 2 and 3 + 2 = 7 below M10: the Gun enters B8 with its crew and loses
        // its Emplacement.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Crew("de-c2", "bd01:A10:0", "german"), Gun("de-g2", "attacker-inf-gun", "bd01:A10:0", "east", "de-c2", "german"), Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Push("de-crew", "bd01:B8:0", "de-gun", 2, 3));
        Assert.Equal(At("bd01:B8:0"), ((MapPosition)((EquipmentInstance)Current.Find("de-gun")!).Position).Location);
        Assert.Contains("de-gun", Current.UnemplacedGuns);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
        await Pass();
        await EndMove();

        // 6 and 6 + 2 = 14 above M10: the Gun and its crew stay and are TI.
        Committed(await Push("de-c2", "bd01:B10:0", "de-g2", 6, 6));
        Assert.Equal(At("bd01:A10:0"), Current.Location("de-c2")!.Location);
        Assert.True(Is(Current.Unit("de-c2")!, "asl:ti") && Is(Current.Find("de-g2")!, "asl:ti"));
    }

    [Fact]
    public async Task ACrewThatWalksAwayAbandonsItsGun()
    {
        // R8.6: a crew that moves without pushing leaves its Gun unmanned; the Gun does not fire.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Move(["de-crew"], "bd01:B8:0"));
        Assert.Null(((EquipmentInstance)Current.Find("de-gun")!).Holding);
    }

    [Fact]
    public async Task ATruckHooksUpAGunTowsItAndUnhooksIt()
    {
        // C10.11 (R8.6): the Stopped truck (T7) hooks up the leIG (M10) for 14 MP, half its 28; the truck, Gun, and crew are TI.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Do(GameActions.HookGun, NoRoll(), new
        {
            vehicleId = "de-t",
            gunId = "de-gun",
            hooked = true
        }));
        var gun = (EquipmentInstance)Current.Find("de-gun")!;
        Assert.Equal(new Holding("de-t", HoldingRole.Towed), gun.Holding);
        Assert.True(Is(Current.Unit("de-t")!, "asl:ti"));
        Refused(await FireAt("de-gun", "bd01:J9:0", null, null), "play.ordnance-gun");

        // The next German MPh: the crew walks to B8, the truck tows the Gun there (one more MP per hex), and unhooks it facing east.
        await Advance(16);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
        Committed(await Move(["de-crew"], "bd01:B8:0"));
        await Pass();
        await EndMove();
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        Assert.Equal(At("bd01:B8:0"), ((MapPosition)((EquipmentInstance)Current.Find("de-gun")!).Position).Location);
        await Pass();
        Committed(await Step("de-t", "stop"));
        await Pass();
        Committed(await Do(GameActions.HookGun, NoRoll(), new
        {
            vehicleId = "de-t",
            gunId = "de-gun",
            hooked = false,
            facing = "east"
        }));
        Assert.Equal(new Holding("de-crew", HoldingRole.Manned), ((EquipmentInstance)Current.Find("de-gun")!).Holding);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    private async Task SetupDefended(string defender, params Dictionary<string, object>[] placements) => Committed(await Commit(Play(), GameActions.Setup,
        JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 8",
                ["catalog"] = "asl-scenario-a1@1.10.0",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
                ["scenarioDefender"] = defender,
            },
            placements,
        })));

    [Fact]
    public async Task TheScenarioDefendersBoreSightedLocationTakesCaseM()
    {
        // C6.4, C6.42 (R8.8): the Russian 45mm Bore Sights B8 at setup; its Defensive First Fire at the German squad entering B8 takes -2.
        var gun = Gun("ru-gun", "defender-at-gun", "bd01:B10:0", "north-east", "ru-crew", "russian");
        gun["boreSighted"] = "bd01:B8:0";
        await SetupDefended("russian", Squad("g1", "bd01:A8:0", "german"), Crew("ru-crew", "bd01:B10:0", "russian"), gun);
        Assert.Single(Current.BoreSights);
        await Advance(2);
        Committed(await Move(["g1"], "bd01:B8:0"));
        var before = Revision;
        Committed(await FireAt("ru-gun", "bd01:B8:0", null, null, 6, 5, 6, 6));
        Assert.Contains(LastShot(before).ToHit!.Drm, item => item.Name == "case-m" && item.Value == -2);
    }

    [Fact]
    public async Task AGunFiresAtEnemyInfantryInItsOwnHexWithCaseE()
    {
        // C5.5 (R8.8): Case E +2 at range 0, no Case L.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:B8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:B8:0", "east", "de-crew", "german"),
            Squad("r1", "bd01:B8:0", "russian"));
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 6, 6));
        var shot = LastShot(before);
        Assert.Contains(shot.ToHit!.Drm, item => item.Name == "case-e" && item.Value == 2);
        Assert.DoesNotContain(shot.ToHit.Drm, item => item.Name == "case-l");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ASquadMansAGunWithCaseH()
    {
        // C5.8 (R8.8): a German squad manning the leIG is non-qualified: +2; the Gun set up without a crew is not Emplaced.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:B10:0", "north-east", "g1", "german"),
            Squad("r1", "bd01:B8:0", "russian"));
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 6, 6));
        Assert.Contains(LastShot(before).ToHit!.Drm, item => item.Name == "case-h" && item.Value == 2);
    }

    [Fact]
    public async Task APushMakesTheGunAndCrewTiYetTheCrewPushesOnAndTheBoreSightingIsLost()
    {
        // C10.3 (table player, pass 8): after each push the Gun and crew are TI, and the crew still pushing goes on; C6.43: a Gun that leaves
        // its setup Location loses its Bore Sighting.
        var gun = Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german");
        gun["boreSighted"] = "bd01:A5:0";
        await SetupDefended("german", Crew("de-crew", "bd01:A8:0", "german"), gun, Squad("r1", "bd01:J9:0", "russian"));
        Assert.Single(Current.BoreSights);
        await Advance(2);
        Committed(await Push("de-crew", "bd01:B8:0", "de-gun", 1, 1));
        Assert.True(Is(Current.Unit("de-crew")!, "asl:ti") && Is(Current.Find("de-gun")!, "asl:ti"));
        Assert.Empty(Current.BoreSights);
        await Pass();
        Refused(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = CrewIds, ["to"] = "bd01:B7:0", ["pushGun"] = "de-gun", ["assault"] = true }),
            "play.move-push");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AHookedUpTruckIsTiAndAnAbandonedGunCanBeHookedUp()
    {
        // C10.11 (table player, pass 8): the truck that hooked up is TI and does not start; a Gun its crew abandoned is hooked up by a crew on
        // foot in its hex.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Crew("de-c2", "bd01:A8:0", "german"), Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Move(["de-crew"], "bd01:B8:0"));
        Assert.Null(((EquipmentInstance)Current.Find("de-gun")!).Holding);
        await Pass();
        await EndMove();
        Committed(await Do(GameActions.HookGun, NoRoll(), new
        {
            vehicleId = "de-t",
            gunId = "de-gun",
            hooked = true
        }));
        Refused(await Do(GameActions.HookGun, NoRoll(), new
        {
            vehicleId = "de-t",
            gunId = "de-gun",
            hooked = true
        }), "play.hook");
        Refused(await Step("de-t", "start"), "TI");
    }

    [Fact]
    public async Task AGunTurnedWithoutFiringDoesNotIntensiveFireThatPhase()
    {
        // C3.22 (table player, pass 8): after a shot that kept the ROF, the Gun turns; it fires no more that phase, Intensive Fire included.
        await Setup("german", 7, 1942, Crew("de-crew", "bd01:B10:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:B10:0", "north-east", "de-crew", "german"),
            Squad("r1", "bd01:B8:0", "russian"));
        await Advance();
        Committed(await FireAt("de-gun", "bd01:B8:0", null, null, 1, 6, 6, 6));
        Committed(await Do(GameActions.TurnGun, NoRoll(), new
        {
            gunId = "de-gun",
            facing = "east"
        }));
        Refused(await FireAt("de-gun", "bd01:B8:0", null, null, true, 5, 6), "play.ordnance-halted");
    }
}
