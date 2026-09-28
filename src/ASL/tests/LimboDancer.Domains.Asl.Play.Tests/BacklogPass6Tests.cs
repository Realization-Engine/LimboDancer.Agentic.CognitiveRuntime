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
/// The backlog pass 6 in live play: wrecks and their cover, Hindrance, smoke, and entry costs (R6.1 to R6.5); Residual FP against vehicles
/// (R6.6); vehicle concealment and entry into concealed Locations (R6.7, R6.8); vehicle fire in the MPh (R6.9); and vehicle MG repair
/// (R6.10). Board 01's hex facts, fixed dice, and a stub LOS reader that names the hexes an LOS crosses.
/// </summary>
public sealed class BacklogPass6Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f506");
    private static readonly GameScope Scope = new(Tenant, "pass6");
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

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass6-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public BacklogPass6Tests() => store = new FileGameStore(root);

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

    private async Task Setup(string firstSide, int? month, params Dictionary<string, object>[] placements)
    {
        var start = new Dictionary<string, object>
        {
            ["label"] = "Pass 6",
            ["catalog"] = "asl-scenario-a1@1.8.0",
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
                ["label"] = "Pass 6",
                ["catalog"] = "asl-scenario-a1@1.8.0",
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

    [Fact]
    public async Task ADestroyedTruckBecomesAWreckThatCoversInfantryOfEitherSide()
    {
        // Two 4-4-7 at two hexes: 8 FP, Kill Number 7; 2+4 = 6 eliminates the truck in B8, which becomes a wreck there (D10.1; R6.5).
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Squad("r2", "bd01:B10:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:B8:0", "german"), Squad("g1", "bd01:A1:0", "german"));
        await Advance();
        Committed(await Fire(R1R2, "bd01:B8:0", 2, 4));
        var wreck = Current.Unit("de-t")!;
        Assert.Equal((InstanceStatus.Wrecked, true), (wreck.Status, Is(wreck, Conditions.Wrecked)));
        Assert.Equal(At("bd01:B8:0"), Current.Location("de-t")!.Location);
        Assert.Equal(["de-t"], GamePlanner.WrecksAt(Current, At("bd01:B8:0")).Select(item => item.Id));
        Assert.Contains(GameView.Of(Current, Perspective.Adjudicator, []).Units, unit => unit.Id == "de-t");

        // D9.3, D10.3 (R6.1): Infantry in the wreck's Location take its +1 TEM, whichever side they are.
        Assert.Equal("de-t", GamePlanner.CoverAt(Current, At("bd01:B8:0"), "german"));
        Assert.Equal("de-t", GamePlanner.CoverAt(Current, At("bd01:B8:0"), "russian"));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ABurningWreckCarriesABlazeWhoseSmokeHindersAndGivesNoCover()
    {
        // 1+2 = 3, at most half the Kill Number 7: a burning wreck with a Blaze (B25.14; R6.3).
        los.Crossed[("B10", "B6")] =
        [
            Crossing("B9", 1), Crossing("B8", 2),
            Crossing("B7", 3),
        ];
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Squad("r2", "bd01:B10:0", "russian"), Squad("r3", "bd01:B10:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:B8:0", "german"), Squad("g1", "bd01:B6:0", "german"));
        await Advance();
        Committed(await Fire(R1R2, "bd01:B8:0", 1, 2));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-t")!.Status);
        Assert.Contains(Current.Entities, entity => entity.Id == GamePlanner.BlazeId("de-t") && entity.Status == InstanceStatus.Active);
        Assert.Null(GamePlanner.CoverAt(Current, At("bd01:B8:0"), "german"));

        // Its smoke is +2 for fire traced through its Location (B25.2, A24.2): r3 at g1 in B6.
        var before = Revision;
        Committed(await Fire(R3, "bd01:B6:0", 6, 5));
        Assert.True(HasDrm(LastFire(before), "los-hindrance", 2m), Drm(LastFire(before)).ToString());
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAfvBetweenFirerAndTargetIsAHindrance()
    {
        // The German halftrack in B9 stands between the Russian squad in B10 and the German squad in B8 (D9.4; R6.2).
        los.Crossed[("B10", "B8")] = [Crossing("B9", 1)];
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Vehicle("de-ht", "attacker-halftrack", "bd01:B9:0", "german"),
            Squad("g1", "bd01:B8:0", "german"));
        await Advance();
        var before = Revision;
        Committed(await Fire(R1, "bd01:B8:0", 6, 5));
        Assert.True(HasDrm(LastFire(before), "los-hindrance", 1m), Drm(LastFire(before)).ToString());
    }

    [Fact]
    public async Task AFriendlyAfvThatEnteredAHexThisTurnGivesNoCoverUntilTheAfphEnds()
    {
        // C6.1 Case J (R6.1): the halftrack that entered B8 in its MPh gives g1 no cover through the AFPh; the next Player Turn it does.
        await Setup("german", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german"), Squad("g1", "bd01:B8:0", "german"),
            Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Step("de-ht", "start"));
        await Pass();
        Assert.Equal("de-ht", GamePlanner.CoverAt(Current, At("bd01:A8:0"), "german"));
        Committed(await Step("de-ht", "enter", to: "bd01:B8:0"));
        await Pass();
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();
        Assert.Null(GamePlanner.CoverAt(Current, At("bd01:B8:0"), "german"));
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        Assert.Null(GamePlanner.CoverAt(Current, At("bd01:B8:0"), "german"));
        await Advance();
        Assert.Equal("de-ht", GamePlanner.CoverAt(Current, At("bd01:B8:0"), "german"));
    }

    [Fact]
    public async Task AWreckRaisesAVehiclesEntryCost()
    {
        // D2.14 (R6.4): the halftrack pays one more MP to enter B8, where the truck's wreck is.
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Squad("r2", "bd01:B10:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:B8:0", "german"), Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german"));
        await Advance();
        Committed(await Fire(R1R2, "bd01:B8:0", 2, 4));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-t")!.Status);
        await Advance(9);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
        Committed(await Step("de-ht", "start"));
        await Pass();
        var entry = Planner().VehicleEntries(Current, Current.Unit("de-ht")!).Single(item => item.To == At("bd01:B8:0"));
        Assert.Equal(4, entry.HalfMp);
        Committed(await Step("de-ht", "enter", to: "bd01:B8:0"));
        Assert.Equal(6, GamePlanner.VehicleHalfMp(Current.Unit("de-ht")!).Spent);
    }

    [Fact]
    public async Task ResidualFpAttacksATruckThatSpendsMpInItsLocation()
    {
        // A8.2, A7.308 (R6.6): the Russians' First Fire leaves Residual FP in B8; the truck's Stop there is attacked on the Vehicle line.
        await Setup("german", 7, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "bd01:B10:0", "russian"));
        await Advance(2);
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        Committed(await Fire(R1, "bd01:B8:0", 5, 6));
        Assert.Single(Current.ResidualFire);
        await Pass();
        var before = Revision;
        Committed(await Do(GameActions.MoveVehicle, Once(5, 6), new Dictionary<string, string> { ["vehicleId"] = "de-t", ["kind"] = "stop" }));
        var record = LastFire(before);
        Assert.Equal(ScenarioA1FireCalculator.ResidualFire, record.Facts.GetProperty("fireKind").GetString());
        Assert.Single(record.Resolution.GetProperty("vehicleEffects").EnumerateArray());
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AVehicleSetsUpConcealedOnlyInGrainAndLosesItsQuestionMarkWhenItMovesInView()
    {
        // A12.2, A12.12 (R6.7): concealed in grain in season, not in Open Ground or out-of-season grain.
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("A8", "Grain"))]);
        Refused(await TrySetup(7, Vehicle("de-t", "attacker-truck", "bd01:B8:0", "german", "east", Conditions.Concealed), Squad("r1", "bd01:J9:0", "russian")),
            "play.setup-vehicle");
        Refused(await TrySetup(10, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "east", Conditions.Concealed), Squad("r1", "bd01:J9:0", "russian")),
            "play.setup-vehicle");
        Committed(await TrySetup(7, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "east", Conditions.Concealed), Squad("r1", "bd01:J9:0", "russian")));
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Concealed));

        // Its Start is no movement; entering B8 within 16 hexes and in the LOS of the Good Order r1 loses its "?".
        await Advance(2);
        Committed(await Step("de-t", "start"));
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Concealed));
        await Pass();
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Concealed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AConcealedVehicleOutOfConcealmentTerrainLosesItsQuestionMarkWhenAnEnemyComesIntoView()
    {
        // Out of every enemy's LOS the truck leaves the grain of A8 for B8 and keeps "?"; when r1's step brings B8 into view it loses it
        // (Case H; R6.7).
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("A8", "Grain"))]);
        los.Blocked.Add(At("bd01:A8:0"));
        los.Blocked.Add(At("bd01:B8:0"));
        Committed(await TrySetup(7, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german", "east", Conditions.Concealed), Squad("r1", "bd01:A1:0", "russian")));
        await Advance(2);
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        await Pass();
        Committed(await Step("de-t", "stop"));
        await Pass();
        await FinishVehicle();
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Concealed));

        await Advance(8);
        Assert.Equal(("mph", "russian"), (Current.Phase, Current.PhasingSide));
        los.Blocked.Clear();
        Committed(await Move(R1, "bd01:A2:0"));
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Concealed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AVehicleEnteringConcealedEnemyUnitsRevealsThemAndRemovesADummy()
    {
        // A12.41 (R6.8): the Russian squad under "?" in B8 is revealed, and the Dummy there removed.
        await Setup("german", 7, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "bd01:B8:0", "russian", Conditions.Concealed),
            Dummy("rd", "bd01:B8:0", "russian"), Squad("r2", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Step("de-t", "start"));
        await Pass();
        Assert.Null(Planner().VehicleEntries(Current, Current.Unit("de-t")!).Single(item => item.To == At("bd01:B8:0")).Bar);
        Committed(await Step("de-t", "enter", to: "bd01:B8:0"));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rd")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AHalftrackFiresAsDefensiveFirstFireAndAsBoundingFirstFire()
    {
        // D3.3, A8.1 (R6.9): the German halftrack in A8 fires at the Russian squad moving into B9 in the Russian MPh.
        await Setup("russian", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german"), Squad("r1", "bd01:B10:0", "russian"),
            Squad("r2", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Move(R1, "bd01:B9:0"));
        var before = Revision;
        Committed(await Fire(HalftrackIds, "bd01:B9:0", 6, 5));
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.FirstFire));
        Assert.Equal(ScenarioA1FireCalculator.FirstFire, LastFire(before).Facts.GetProperty("fireKind").GetString());

        // In its own MPh, once the DEFENDER passes on its Start, it fires as Bounding First Fire, and it may not fire in the AFPh.
        await Pass();
        await EndMove();
        await Advance(8);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
        Committed(await Step("de-ht", "start"));
        Refused(await Fire(HalftrackIds, "bd01:B9:0", 6, 5), "play.fire-window");
        await Pass();
        Committed(await Fire(HalftrackIds, "bd01:B9:0", 6, 5));
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.BoundingFire));
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        Refused(await Fire(HalftrackIds, "bd01:B9:0", 6, 5), "firer-already-fired");
    }

    [Fact]
    public async Task AVehiclesMalfunctionedAamgIsRepairedOnOneAndDisabledOnSix()
    {
        // D3.7 (R6.10): in its RPh the CE crew tries once: 1 repairs the AAMG; the other halftrack's 6 disables it.
        await Setup("german", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german", "east", Conditions.Malfunctioned),
            Vehicle("de-h2", "attacker-halftrack", "bd01:A10:0", "german", "east", Conditions.Malfunctioned), Squad("r1", "bd01:J9:0", "russian"));
        Assert.Equal("rph", Current.Phase);
        Committed(await Do(GameActions.Repair, Once(1), new
        {
            unitId = "de-ht",
            equipmentId = "de-ht"
        }));
        Assert.False(Is(Current.Unit("de-ht")!, Conditions.Malfunctioned));
        Committed(await Do(GameActions.Repair, Once(6), new
        {
            unitId = "de-h2",
            equipmentId = "de-h2"
        }));
        Assert.True(Is(Current.Unit("de-h2")!, Conditions.Disabled));
        Refused(await Do(GameActions.Repair, Once(1), new
        {
            unitId = "de-h2",
            equipmentId = "de-h2"
        }), "play.repair-weapon");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAfvInAGrainHexAddsItsHindranceToTheGrains()
    {
        // A6.7 and its example (table player, item 1): the halftrack in the grain of B9 makes that hex +2; the map's grain alone is +1.
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("B9", "Grain"))]);
        los.Crossed[("B10", "B8")] = [Crossing("B9", 1)];
        los.Hindrances[("B10", "B8")] = [new LosHindrance(1, 1) { Terrains = ["Grain"] }];
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Vehicle("de-ht", "attacker-halftrack", "bd01:B9:0", "german"),
            Squad("g1", "bd01:B8:0", "german"));
        await Advance();
        var before = Revision;
        Committed(await Fire(R1, "bd01:B8:0", 6, 5));
        Assert.True(HasDrm(LastFire(before), "los-hindrance", 2m), Drm(LastFire(before)).ToString());
    }

    [Fact]
    public async Task AConcealedVehicleDestroyedLeavesAKnownWreck()
    {
        // Table player, item 5: the wreck of a truck under "?" is seen by both sides (D10.1).
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("B8", "Grain"))]);
        await Setup("russian", 7, Squad("r1", "bd01:B10:0", "russian"), Squad("r2", "bd01:B10:0", "russian"),
            Vehicle("de-t", "attacker-truck", "bd01:B8:0", "german", "east", Conditions.Concealed));
        await Advance();
        Committed(await Fire(R1R2, "bd01:B8:0", 1, 2));
        var wreck = Current.Unit("de-t")!;
        Assert.Equal((InstanceStatus.Wrecked, false), (wreck.Status, Is(wreck, Conditions.Concealed)));
        Assert.Contains(GameView.Of(Current, Current.Perspectives.Single(item => item.Name == "russian"), []).Units, unit => unit.Id == "de-t");
    }

    [Fact]
    public async Task AVehicleMayFireAtTheOutsetOfItsMphAndTheDefenderMayRepair()
    {
        // D3.3 (table player, item 3): the halftrack fires before any MP, at half FP; D3.7 (item 4): the Russian halftrack repairs in the
        // German RPh.
        await Setup("german", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german"), Squad("r1", "bd01:B9:0", "russian"),
            Vehicle("ru-ht", "attacker-halftrack", "bd01:A10:0", "russian", "west", Conditions.Malfunctioned));
        Committed(await Do(GameActions.Repair, Once(1), new
        {
            unitId = "ru-ht",
            equipmentId = "ru-ht"
        }));
        Assert.False(Is(Current.Unit("ru-ht")!, Conditions.Malfunctioned));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);
        var before = Revision;
        Committed(await Fire(HalftrackIds, "bd01:B9:0", 6, 5));
        Assert.Equal(ScenarioA1FireCalculator.BoundingFirstFire, LastFire(before).Facts.GetProperty("fireKind").GetString());
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.BoundingFire));
        Committed(await Step("de-ht", "start"));
    }
}
