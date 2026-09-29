using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 11 in live play: Reverse movement (R11.1), VBM (R11.2), ESB (R11.3), Mechanical Reliability (R11.4), ALL entries and Minimum Move
/// (R11.5), vehicle stacking and D2.6 (R11.6), vehicle terrain and Bog (R11.7 to R11.10), OVR (R11.11), the A12.41 choice (R11.12), CC Reaction Fire
/// (R11.13), and CC with vehicles (R11.14 to R11.17). A board 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass11Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f511");
    private static readonly GameScope Scope = new(Tenant, "pass11");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R1 = ["r1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass11-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass11Board board = new();

    public BacklogPass11Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>
    private sealed class Pass11Board
    {
        private static readonly BoardHandle Blank = Build(new Dictionary<string, string>(), new Dictionary<string, int>(), []);

        public Dictionary<string, string> Terrain { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Base { get; } = new(StringComparer.Ordinal);

        public List<(string Hex, HexsideDirection Side, string? Terrain, string? Hexside)> Sides { get; } = [];

        public static string N(string hex, HexsideDirection side) => Blank.Neighbor(HexName.Parse(hex), side)!.ToString()!;

        public void Side(string hex, HexsideDirection side, string? terrain = null, string? hexside = null) => Sides.Add((hex, side, terrain, hexside));

        public BoardHandle Handle() => Build(Terrain, Base, Sides);

        private static TerrainType Type(string name) => new()
        {
            Code = (byte)(Math.Abs(name.GetHashCode(StringComparison.Ordinal)) % 250 + 1),
            Name = name,
            Category = name switch
            {
                "Woods" => LosCategory.Woods,
                "Paved Road" or "Dirt Road" => LosCategory.Road,
                "Wall" or "Hedge" => LosCategory.Hexside,
                _ when name.Contains("Building", StringComparison.Ordinal) => LosCategory.Building,
                _ => LosCategory.Open,
            },
        };

        private static BoardHandle Build(IReadOnlyDictionary<string, string> terrain, IReadOnlyDictionary<string, int> levels,
            IReadOnlyList<(string Hex, HexsideDirection Side, string? Terrain, string? Hexside)> sides)
        {
            var geometry = BoardGeometry.StandardGeomorphic;
            var marked = new Dictionary<(string, HexsideDirection), (string? Terrain, string? Hexside)>();
            foreach (var (hex, side, sideTerrain, hexside) in sides)
            {
                marked[(hex, side)] = (sideTerrain, hexside);
                if (Blank?.Neighbor(HexName.Parse(hex), side) is { } across)
                {
                    marked[(across.ToString(), (HexsideDirection)(((int)side + 3) % 6))] = (sideTerrain, hexside);
                }
            }

            var hexes = new List<HexFacts>();
            foreach (var text in Board01Fixture.Hexes())
            {
                var name = HexName.Parse(text);
                Assert.True(geometry.TryGetIndex(name, out var index));
                var center = new LocationFacts(0, Type(terrain.GetValueOrDefault(text) ?? "Open Ground"), null);
                HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => marked.TryGetValue((text, side), out var mark)
                    ? new HexsideFacts(side, true, Type(mark.Terrain ?? terrain.GetValueOrDefault(text) ?? "Open Ground"), mark.Hexside is { } wall ? Type(wall) : null, false, false, false, false, null)
                    : new HexsideFacts(side, true, Type(terrain.GetValueOrDefault(text) ?? "Open Ground"), null, false, false, false, false, null))];
                hexes.Add(new HexFacts(name, index, levels.GetValueOrDefault(text), false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
            }

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 11 test board", new HexFactSet(geometry, "pass11", hexes));
        }
    }

    // Clear LOS at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            return new(LosStatus.Clear, false, range, 0, null, string.Empty);
        }
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([board.Handle()]), Vocabulary, [Catalog], fireLos: los);

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

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>The special rules of the next setup (backlog pass 16: night and weather).</summary>
    private string[] rules = [];

    private async Task Setup(string firstSide, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 11",
                ["catalog"] = "asl-scenario-a1@1.10.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
                ["specialRules"] = rules,
            },
            placements,
        })));
        await Advance(2);
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

    private Task<PlayResult> Step(string vehicle, string kind, string? to = null, string? facing = null, DiceRoller? roller = null, Dictionary<string, object>? options = null)
    {
        var arguments = new Dictionary<string, object> { ["vehicleId"] = vehicle, ["kind"] = kind };
        if (to is not null)
        {
            arguments["to"] = to;
        }

        if (facing is not null)
        {
            arguments["facing"] = facing;
        }

        foreach (var (name, value) in options ?? [])
        {
            arguments[name] = value;
        }

        return Do(GameActions.MoveVehicle, roller ?? NoRoll(), arguments);
    }

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private Task<PlayResult> EndMove(string? intended = null) => Do(GameActions.EndMove, NoRoll(), intended is null
        ? new Dictionary<string, object>()
        : new Dictionary<string, object> { ["intended"] = intended });

    private async Task FinishVehicle(string? intended = null)
    {
        Committed(await EndMove(intended));
        if (Current.Movement is { WindowOpen: true })
        {
            await Pass();
        }
    }

    private Task<PlayResult> Choose(string key, string option, DiceRoller? roller = null) => Do(GameActions.Choose, roller ?? NoRoll(), new
    {
        key,
        option
    });

    private Task<PlayResult> VehicleCc(string location, DiceRoller roller, string? vehicle = null, string[]? attackers = null, string[]? defenders = null, bool pass = false)
    {
        var arguments = new Dictionary<string, object> { ["location"] = location };
        if (vehicle is not null)
        {
            arguments["vehicleId"] = vehicle;
        }

        if (attackers is not null)
        {
            arguments["attackers"] = attackers;
        }

        if (defenders is not null)
        {
            arguments["defenders"] = defenders;
        }

        if (pass)
        {
            arguments["pass"] = true;
        }

        return Do(GameActions.VehicleCloseCombat, roller, arguments);
    }

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private VehicleStepped LastStep(long before) => Since(before).Select(item => item.Payload).OfType<VehicleStepped>().Last();

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static string L(string hex) => $"bd01:{hex}:0";

    private static string Ne(string hex) => Pass11Board.N(hex, HexsideDirection.NorthEast);

    private static string Se(string hex) => Pass11Board.N(hex, HexsideDirection.SouthEast);

    private static string Nw(string hex) => Pass11Board.N(hex, HexsideDirection.NorthWest);

    private static string Sw(string hex) => Pass11Board.N(hex, HexsideDirection.SouthWest);

    private (int Spent, int Allotment) Mp(string vehicle) => GamePlanner.VehicleHalfMp(Current.Unit(vehicle)!);

    [Fact]
    public async Task ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp()
    {
        // D2.2, D2.21, D2.23 (R11.1): the halftrack in E5 facing east Starts in Reverse and backs into a rear hex for 4 x 1 MP; a forward hex is refused.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        var before = Revision;
        Committed(await Step("de-ht", "start", options: new()
        {
            ["reverse"] = true
        }));
        Assert.True(LastStep(before).Reverse);
        await Pass();
        Refused(await Step("de-ht", "enter", L(Ne("E5"))), "play.move-vehicle");
        before = Revision;
        Committed(await Step("de-ht", "enter", L(Nw("E5"))));
        Assert.Equal((8, true), (LastStep(before).HalfMp, LastStep(before).Reverse));
        Assert.Equal(UnitFacing.East, ((MapPosition)Current.Unit("de-ht")!.Position).Facing);
        await Pass();

        // 1 + 4 + 4 MP and three VCA changes of 1 MP leave 4 of 16 MP: a Reverse entry of 4 MP would leave none to Stop, and the vehicle may not end
        // in Reverse Motion.
        var here = Nw(Nw("E5"));
        Committed(await Step("de-ht", "enter", L(here)));
        await Pass();
        foreach (var facing in new[] { "north-east", "east", "north-east" })
        {
            Committed(await Step("de-ht", "turn", facing: facing));
            await Pass();
        }

        Refused(await Step("de-ht", "enter", L(Sw(here))), "play.move-vehicle-reverse");
        Refused(await EndMove(L(Sw(here))), "play.vehicle-motion");
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();
        NoReplayErrors();
    }

    [Fact]
    public async Task VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere()
    {
        // D2.3 (R11.2): from E5 facing east the halftrack Bypasses the woods of the NE hex along the hexside it shares with the SE hex, for 2 x 1 MP.
        var woods = Ne("E5");
        var open = Se("E5");
        board.Terrain[woods] = "Woods";
        board.Side(woods, HexsideDirection.South, terrain: "Open Ground");
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        var entries = Planner().VehicleEntries(Current, Current.Unit("de-ht")!);
        Assert.Contains(entries, item => item.To == BoardLocation.Parse(L(woods)) && item.Straddling == BoardLocation.Parse(L(open)) && item.HalfMp == 4);
        var before = Revision;
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new Dictionary<string, object> { ["vehicleId"] = "de-ht", ["kind"] = "enter", ["to"] = L(woods), ["bypass"] = true }));
        Assert.Equal(BoardLocation.Parse(L(open)), LastStep(before).Straddling);
        Assert.Equal(BoardLocation.Parse(L(open)), Current.Unit("de-ht")!.Straddling);
        await Pass();

        // D2.34: it Stops and ends its MPh in Stationary Bypass, which the next phases keep.
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();
        await Advance();
        Assert.Equal(BoardLocation.Parse(L(open)), Current.Unit("de-ht")!.Straddling);
        NoReplayErrors();
    }

    [Fact]
    public async Task VbmIsRefusedAlongAHexsideTheObstacleTouches()
    {
        // D2.3: the woods touch the hexside the lane would use, so the VBM is refused; the outright ALL entry with a Bog DR is offered instead.
        var woods = Ne("E5");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        var bypass = Planner().VehicleEntries(Current, Current.Unit("de-ht")!).Single(item => item.To == BoardLocation.Parse(L(woods)) && item.Straddling is not null);
        Assert.Contains("the obstacle touches", bypass.Bar, StringComparison.Ordinal);
        var outright = Planner().VehicleEntries(Current, Current.Unit("de-ht")!).Single(item => item.To == BoardLocation.Parse(L(woods)) && item.Straddling is null);
        Assert.True(outright.All);
        Assert.Equal(2, outright.BogDrm);
    }

    [Fact]
    public async Task EsbAddsMpOnceOrImmobilizesTheVehicle()
    {
        // D2.5 (R11.3): the German halftrack seeks 4 MP at +4 +2 = +6: a DR of 5 is 11, which gains them; a second ESB is refused.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Vehicle("de-t", "attacker-truck", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Refused(await Step("de-ht", "esb", options: new()
        {
            ["mp"] = 2
        }), "play.move-vehicle-esb");
        Committed(await Step("de-ht", "start"));
        await Pass();
        Refused(await Step("de-ht", "esb", options: new()
        {
            ["mp"] = 5
        }), "play.move-vehicle-esb");
        Committed(await Step("de-ht", "esb", roller: Once(2, 3), options: new()
        {
            ["mp"] = 4
        }));
        Assert.Equal(40, Mp("de-ht").Allotment);
        Refused(await Step("de-ht", "esb", options: new()
        {
            ["mp"] = 1
        }), "play.move-vehicle-esb");
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();

        // A truck is not tracked (D2.5).
        Committed(await Step("de-t", "start"));
        await Pass();
        Refused(await Step("de-t", "esb", options: new()
        {
            ["mp"] = 1
        }), "only a tracked vehicle");
        NoReplayErrors();
    }

    [Fact]
    public async Task AFailedEsbImmobilizesTheVehicleAndEndsItsMove()
    {
        // D2.5: 1 MP sought, +1 +2: a DR of 9 is 12, which immobilizes the halftrack.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        Committed(await Step("de-ht", "esb", roller: Once(4, 5), options: new()
        {
            ["mp"] = 1
        }));
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.Immobilized));
        Assert.True(Current.Unit("de-ht")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task TheRedMpT34RollsForMechanicalReliabilityWhenItStarts()
    {
        // D2.51 (R11.4): the T-34's Start MP with a DR of 12 immobilizes it; the Start stands and the DEFENDER may fire.
        await Setup("russian", Vehicle("ru-tk", "defender-tank", L("E5"), "russian"), Squad("g1", L("J9"), "german"));
        Committed(await Step("ru-tk", "start", roller: Once(6, 6)));
        Assert.True(Is(Current.Unit("ru-tk")!, Conditions.Immobilized));
        Assert.True(Current.Movement is { WindowOpen: true, Ending: true });
        await Pass();
        Assert.True(Current.Unit("ru-tk")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck()
    {
        // B13.41, D2.7 (R11.5, R11.7, R11.9): the truck enters woods for ALL its 28 MP with a Bog DR at +3 (Normal GP, not fully-tracked, truck MP):
        // 4 + 3 = 7 passes; then it may only Stop, beyond its allotment.
        var woods = Ne("E5");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-t", "enter", L(woods), roller: Once(2, 2)));
        Assert.Equal(56, LastStep(before).HalfMp);
        var check = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().Single();
        Assert.Equal((VehicleCheckRolled.Bog, 3, VehicleCheckRolled.Passed), (check.Check, check.Drm, check.Result));
        await Pass();
        Refused(await Step("de-t", "turn", facing: "north-east"), "play.move-vehicle-all");
        Committed(await Step("de-t", "stop"));
        await Pass();
        await FinishVehicle();

        // B13.42: the PzKpfw IIIH enters at half its 13 MP, 6½ MP, with its Bog DR at +4 (+3 half allotment, +1 Normal GP): 4 + 4 + 4 = 12 bogs it.
        var tankWoods = Ne("E8");
        board.Terrain[tankWoods] = "Woods";
        Committed(await Step("de-tk", "start"));
        await Pass();
        before = Revision;
        Committed(await Step("de-tk", "enter", L(tankWoods), roller: Once(4, 4)));
        Assert.Equal(13, LastStep(before).HalfMp);
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        await Pass();
        Assert.True(Current.Unit("de-tk")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABoggedTankIsFreedMiredOrImmobilizedByItsBogRemoval()
    {
        // D8.3 (R11.10): bogged in the woods, the tank spends colored x white dr as its Start MP next MPh: 2 x 3 = 6 MP, and a colored dr of 2 frees it.
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Committed(await Step("de-tk", "enter", L(woods), roller: Once(4, 4)));
        await Pass();
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        await Advance(16);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
        Refused(await Step("de-tk", "turn", facing: "north-east"), "play.move-vehicle-bog");
        var before = Revision;
        Committed(await Step("de-tk", "start", roller: Once(2, 3)));
        Assert.Equal((12, true), (LastStep(before).HalfMp, LastStep(before).BogRemoval));
        Assert.False(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        await Pass();
        NoReplayErrors();
    }

    [Fact]
    public async Task ABogRemovalOfFiveMiresTheTank()
    {
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Committed(await Step("de-tk", "enter", L(woods), roller: Once(4, 4)));
        await Pass();
        await Advance(16);
        Committed(await Step("de-tk", "start", roller: Once(5, 1)));
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Mired));
        await Pass();
        Assert.True(Current.Unit("de-tk")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task AMinimumMoveEntersAHexCostingMoreThanTheAllotment()
    {
        // D2.15 (R11.5): uphill woods at half the tank's allotment, 6½ + 4 MP, plus two friendly vehicles there doubled in woods, 4 MP: 14½ MP
        // exceeds 13, so only a Minimum Move enters it, for 13 MP, ending in Motion.
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        board.Base[woods] = 1;
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Vehicle("de-t1", "attacker-truck", L(woods), "german"),
            Vehicle("de-t2", "attacker-truck", L(woods), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Refused(await Step("de-tk", "enter", L(woods)), "only a Minimum Move");
        var before = Revision;
        Committed(await Step("de-tk", "enter", L(woods), roller: Once(1, 2), options: new()
        {
            ["minimumMove"] = true
        }));
        Assert.Equal((26, true), (LastStep(before).HalfMp, LastStep(before).MinimumMove));
        await Pass();
        Assert.True(Current.Unit("de-tk")!.MovementEnded);

        // A4.134 is Infantry's (table-player finding): the tank is neither pinned nor CX.
        Assert.False(Is(Current.Unit("de-tk")!, Conditions.Pinned) || Is(Current.Unit("de-tk")!, Conditions.Cx));
        await Advance();
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Motion));
        NoReplayErrors();
    }

    [Fact]
    public async Task WallsHedgesAndHillsCostWhatTheTerrainChartSays()
    {
        // B9.4, Terrain Chart (R11.7): a halftrack may not cross a wall; a tank crosses it for 1 + 1 MP; one level up adds 4 MP.
        board.Side("E5", HexsideDirection.NorthEast, hexside: "Wall");
        board.Base[Ne("E8")] = 1;
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Vehicle("de-tk", "attacker-tank", L("E5"), "german"),
            Vehicle("de-t", "attacker-truck", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        Refused(await Step("de-ht", "enter", L(Ne("E5"))), "may not cross a wall");
        Committed(await Step("de-ht", "stop"));
        await Pass();
        await FinishVehicle();
        Committed(await Step("de-tk", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-tk", "enter", L(Ne("E5"))));
        Assert.Equal(4, LastStep(before).HalfMp);
        await Pass();
        Committed(await Step("de-tk", "stop"));
        await Pass();
        await FinishVehicle();
        Committed(await Step("de-t", "start"));
        await Pass();
        before = Revision;
        Committed(await Step("de-t", "enter", L(Ne("E8"))));
        Assert.Equal(16, LastStep(before).HalfMp);
        NoReplayErrors();
    }

    [Fact]
    public async Task VehiclesShareALocationAndPayForTheVehiclesThere()
    {
        // D2.14, A5.2 (R11.6): the truck enters the hex of the halftrack for 4 + 1 MP.
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L(Ne("E5")), "german"), Vehicle("de-t", "attacker-truck", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-t", "enter", L(Ne("E5"))));
        Assert.Equal(10, LastStep(before).HalfMp);
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleMayNotStopInAnEnemyAfvsHexItCouldNotKill()
    {
        // D2.6 (R11.6): the halftrack's AAMG cannot destroy the T-34 with an Original TK DR of 5, so it may not Stop in the T-34's hex; the tank's 50L can.
        await Setup("german", Vehicle("ru-tk", "defender-tank", L(Ne("E5")), "russian", "west"), Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"),
            Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        Committed(await Step("de-ht", "enter", L(Ne("E5"))));
        await Pass();
        Refused(await Step("de-ht", "stop"), "play.move-vehicle-enemy-afv");
        Refused(await EndMove(), "D2.6");
        NoReplayErrors();
    }

    [Fact]
    public async Task ATankOverrunsASquadAndTheSquadReactsInCc()
    {
        // D7.1, D7.11 (R11.11): the PzKpfw IIIH enters the Russian squad's Open Ground hex with an OVR: 1 + 4 MP; after the DEFENDER passes, the OVR
        // attacks with 4 + (3 + 5) x 3 / 2 = 16 FP, FFMO -1.
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-tk", "enter", L(Ne("E5")), options: new()
        {
            ["overrun"] = true
        }));
        Assert.Equal((10, true), (LastStep(before).HalfMp, LastStep(before).Overrunning));
        Refused(await Do(GameActions.Overrun, NoRoll(), new
        {
        }), "play.overrun");

        // D7.1, D7.2 (referee, pass 11): no CC Reaction Fire before the OVR is resolved.
        Refused(await VehicleCc(L(Ne("E5")), NoRoll(), attackers: ["r1"]), "the OVR is resolved first");
        await Pass();
        Refused(await Step("de-tk", "stop"), "play.move-vehicle-ovr");

        // An Original 12, Final 11 on the 16 column: a PTC, which the squad passes (DR 4), and D7.17's Random Selection among MA, BMG, and CMG (6, 1, 1)
        // malfunctions the MA.
        before = Revision;
        Committed(await Do(GameActions.Overrun, Once(6, 6, 2, 2, 6, 1, 1), new
        {
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(16m, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.Contains(fire.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "ffmo");
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Malfunctioned));
        Assert.True(Current.Movement is { WindowOpen: true, Reaction: true });
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.BoundingFire));

        // D7.21, A11.6 (R11.13): the squad passes its PAATC (2 + 5 = 7) and attacks in CC Reaction Fire: CCV 5, +2 against the moving tank; an Original 2
        // is 4, below 5, which eliminates it, and the Unlikely Kill dr of 4 does not better it.
        before = Revision;
        Committed(await VehicleCc(L(Ne("E5")), Once(2, 5, 1, 1, 4), attackers: ["r1"]));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<PaatcTaken>());
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-tk")!.Status);

        NoReplayErrors();
    }

    [Fact]
    public async Task AnOvrIsRefusedInReverseAndFromVbm()
    {
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E5"), "german"), Squad("r1", L(Nw("E5")), "russian"), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-tk", "start", options: new()
        {
            ["reverse"] = true
        }));
        await Pass();
        Refused(await Step("de-tk", "enter", L(Nw("E5")), options: new()
        {
            ["overrun"] = true
        }), "no OVR is made in Reverse");
    }

    [Fact]
    public async Task ConcealedUnitsEnteredByAVehicleTakeACombinedPaatcAndStayConcealedWhenTheyPass()
    {
        // A12.41 (R11.12): the Russian owner chooses the PAATC: Morale Level 7, DR 6 passes, and the squad stays concealed; the truck then declares its
        // OVR in place, which is Area Fire at the concealed squad.
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian", Conditions.Concealed), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        Refused(await Step("de-t", "enter", L(Ne("E5")), options: new()
        {
            ["overrun"] = true
        }), "A12.41");
        Committed(await Step("de-t", "enter", L(Ne("E5"))));
        Assert.Equal(ChoicePending.Paatc, Current.Choice!.Kind);
        Refused(await Pass2(), "play.choice-pending");
        Committed(await Choose($"paatc:de-t:{L(Ne("E5"))}", ChoicePending.Check, Once(3, 3)));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Contains("r1|de-t", Current.PaatcPassed);
        await Pass();
        Committed(await Step("de-t", "overrun"));
        await Pass();
        var before = Revision;
        Committed(await Do(GameActions.Overrun, Once(3, 4), new
        {
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Contains(fire.Resolution.GetProperty("arithmetic").GetProperty("firers").EnumerateArray().SelectMany(item => item.GetProperty("multipliers").EnumerateArray()),
            item => item.GetProperty("name").GetString() == "area-fire-concealed-target");
        NoReplayErrors();
    }

    private Task<PlayResult> Pass2() => Do(GameActions.PassFire, NoRoll(), new
    {
    });

    [Fact]
    public async Task AFailedCombinedPaatcPinsAndRevealsTheUnits()
    {
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian", Conditions.Concealed), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", L(Ne("E5"))));
        Committed(await Choose($"paatc:de-t:{L(Ne("E5"))}", ChoicePending.Check, Once(5, 4)));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Pinned));
        NoReplayErrors();
    }

    [Fact]
    public async Task InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc()
    {
        // A11.6 (R11.17): the Russian squad in the APh passes its PAATC (DR 7 against 7) and advances into the halftrack's Open Ground hex.
        await Setup("russian", Vehicle("de-ht", "attacker-halftrack", L(Ne("E5")), "german"), Squad("r1", L("E5"), "russian"), Squad("g1", L("J9"), "german"));
        await Advance(4);
        Assert.Equal("aph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = R1,
            to = L(Ne("E5"))
        }));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<PaatcTaken>());
        Assert.Equal(BoardLocation.Parse(L(Ne("E5"))), Current.Location("r1")!.Location);
        await Advance();
        Assert.Equal("ccph", Current.Phase);

        // A11.31 (R11.16): the non-vehicular Russians attack first; CCV 5, -2 OT: an Original 6 is 4, below 5: the halftrack is eliminated.
        Refused(await VehicleCc(L(Ne("E5")), NoRoll(), vehicle: "de-ht", defenders: ["r1"]), "play.cc-vehicle-order");
        Committed(await VehicleCc(L(Ne("E5")), Once(3, 3), vehicle: "de-ht", attackers: ["r1"]));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-ht")!.Status);
        await Advance();
        Assert.False(Is(Current.Unit("r1")!, Conditions.Melee));
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleAttacksInfantryInCcAndHoldsItInMelee()
    {
        // A11.62 (R11.15): after the Russian squad's attack fails (Original 11), the CE halftrack attacks it with its 3 FP AAMG against CCV 5: 1-2, Kill 4.
        await Setup("russian", Vehicle("de-ht", "attacker-halftrack", L(Ne("E5")), "german"), Squad("r1", L("E5"), "russian"), Squad("g1", L("J9"), "german"));
        await Advance(4);
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = R1,
            to = L(Ne("E5"))
        }));
        await Advance();
        Committed(await VehicleCc(L(Ne("E5")), Once(5, 6), vehicle: "de-ht", attackers: ["r1"]));
        Assert.Equal("german", Current.CloseCombats.Single().Next);
        var before = Revision;
        Committed(await VehicleCc(L(Ne("E5")), Once(4, 4), vehicle: "de-ht", defenders: ["r1"]));
        var record = Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>().Single();
        Assert.Equal("1-2", record.Resolution.GetProperty("odds").GetString());
        Assert.True(Current.CloseCombats.Single().Closed);

        // A11.7: the halftrack, stopped, holds the squad in Melee after the CCPh.
        await Advance();
        Assert.True(Is(Current.Unit("r1")!, Conditions.Melee));
        Assert.False(Is(Current.Unit("de-ht")!, Conditions.Melee));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnUnarmedTruckAloneWithEnemyInfantryIsCaptured()
    {
        // A11.52 (R11.16; referee, pass 11): the stopped German truck with no German Personnel there is captured as the CCPh begins.
        await Setup("russian", Vehicle("de-t", "attacker-truck", L(Ne("E5")), "german"), Squad("r1", L("E5"), "russian"), Squad("g1", L("J9"), "german"));
        await Advance(4);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = R1,
            to = L(Ne("E5"))
        }));
        await Advance();
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Captured));
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Abandoned));
        Refused(await VehicleCc(L(Ne("E5")), NoRoll(), vehicle: "de-t", attackers: ["r1"]), "play.cc-vehicle");
        await Advance();
        Assert.False(Is(Current.Unit("r1")!, Conditions.Melee));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAbruptElevationChangeIsCrossedByRoadAtItsB1051Cost()
    {
        // B10.51, B10.52 (referee, pass 11): two levels up off the road is a Double-Crest, refused; by road, 1/2 MP + 2 MP for the last level + 4 MP
        // for the intermediate level: 6 1/2 MP.
        board.Base[Ne("E5")] = 2;
        board.Base[Ne("E8")] = 2;
        board.Side("E8", HexsideDirection.NorthEast, terrain: "Dirt Road");
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Vehicle("de-t2", "attacker-truck", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        Refused(await Step("de-t", "enter", L(Ne("E5"))), "Double-Crest");
        Committed(await Step("de-t", "stop"));
        await Pass();
        await FinishVehicle();
        Committed(await Step("de-t2", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-t2", "enter", L(Ne("E8"))));
        Assert.Equal(13, LastStep(before).HalfMp);
        NoReplayErrors();
    }

    [Fact]
    public async Task AReverseAllEntryKeepsItsStopMpAndNoOvrGoesWithAnAllEntry()
    {
        // B13.41, D2.7 (referee, pass 11): the truck backs into the woods behind it for ALL its MP, which leaves its Stop MP beyond the allotment; an
        // OVR may not go with an ALL entry.
        var woods = Nw("E5");
        board.Terrain[woods] = "Woods";
        board.Terrain[Ne("E8")] = "Woods";
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Vehicle("de-t2", "attacker-truck", L("E8"), "german"), Squad("r1", L(Ne("E8")), "russian"),
            Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-t", "start", options: new()
        {
            ["reverse"] = true
        }));
        await Pass();
        Committed(await Step("de-t", "enter", L(woods), roller: Once(2, 2)));
        await Pass();
        Committed(await Step("de-t", "stop"));
        await Pass();
        await FinishVehicle();
        Committed(await Step("de-t2", "start"));
        await Pass();
        Refused(await Step("de-t2", "enter", L(Ne("E8")), options: new()
        {
            ["overrun"] = true
        }), "leaving no MP for an OVR");
        NoReplayErrors();
    }

    [Fact]
    public async Task AHalftrackCrossingAHedgeIntoWoodsTakesBothBogDrs()
    {
        // B9.4 (referee, pass 11): the hedge's Bog DR in the hex it leaves (+2: Normal GP, not fully-tracked), then the woods' (+2): both 7, both pass.
        var woods = Ne("E5");
        board.Terrain[woods] = "Woods";
        board.Side("E5", HexsideDirection.NorthEast, hexside: "Hedge");
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-ht", "enter", L(woods), roller: Once(2, 3, 3, 2)));
        var checks = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().ToArray();
        Assert.Equal([(2, VehicleCheckRolled.Passed), (2, VehicleCheckRolled.Passed)], checks.Select(item => (item.Drm, item.Result)));
        Assert.Equal(BoardLocation.Parse(L(woods)), Current.Location("de-ht")!.Location);
        NoReplayErrors();
    }

    [Fact]
    public async Task AHalftrackFailingItsHedgeBogDrBogsInTheHexItLeft()
    {
        // B9.4: 5 + 5 + 2 = 12: bogged in E5, having spent the entry's MP there.
        board.Side("E5", HexsideDirection.NorthEast, hexside: "Hedge");
        await Setup("german", Vehicle("de-ht", "attacker-halftrack", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-ht", "start"));
        await Pass();
        Committed(await Step("de-ht", "enter", L(Ne("E5")), roller: Once(5, 5)));
        Assert.Equal(BoardLocation.Parse(L("E5")), Current.Location("de-ht")!.Location);
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.Bogged));
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleThatCannotMoveOnMayStopBesideAnEnemyAfv()
    {
        // D2.6 (table-player finding): the truck's ALL entry into the T-34's woods hex leaves it no MP to move on, so it may Stop there although it could
        // not kill the T-34.
        var woods = Ne("E5");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("ru-tk", "defender-tank", L(woods), "russian", "west"), Vehicle("de-t", "attacker-truck", L("E5"), "german"),
            Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", L(woods), roller: Once(2, 2)));
        await Pass();
        Committed(await Step("de-t", "stop"));
        await Pass();
        await FinishVehicle();
        Assert.True(Current.Unit("de-t")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task ATankBoggingAsItOverrunsStillResolvesTheOvr()
    {
        // D7.1, D8.2 (R11.11; table-player finding): the PzKpfw IIIH enters the squad's woods hex with an OVR, 6½ + 4 MP, and bogs (4 + 4 + 4); its OVR
        // is still resolved at 16 FP halved for the Immobile tank, 8 FP, and its move ends after the Reaction window. 6 + 5, +1 for the woods, has no
        // effect.
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L(woods), "russian"), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Committed(await Step("de-tk", "enter", L(woods), roller: Once(4, 4), options: new()
        {
            ["overrun"] = true
        }));
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        await Pass();
        Assert.Equal(BoardLocation.Parse(L(woods)), Current.Movement!.Overrun);
        var before = Revision;
        Committed(await Do(GameActions.Overrun, Once(6, 5), new
        {
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(8m, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.Single(Since(before).Select(item => item.Payload).OfType<OverrunResolved>());
        Assert.True(Current.Movement is { WindowOpen: true, Reaction: true });
        await Pass();
        Assert.Null(Current.Movement);
        Assert.True(Current.Unit("de-tk")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOvrStoppedAtAnOwnersOptionIsResolvedWhenTheAnswerResumesIt()
    {
        // D7.1 (table-player finding): the OVR's 16 FP, final 10, is a NMC; the squad's Original 2 brings Heat of Battle (3 + 3: a hero and Battle
        // Hardening), which waits for the Russian answer; once it is given, the OVR is resolved and the Reaction window opens, and no second OVR is made.
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Committed(await Step("de-tk", "enter", L(Ne("E5")), options: new()
        {
            ["overrun"] = true
        }));
        await Pass();
        Committed(await Do(GameActions.Overrun, Once(6, 5, 1, 1, 3, 3), new
        {
        }));
        Assert.NotNull(Current.Choice);
        Assert.NotNull(Current.Movement!.Overrun);
        var before = Revision;
        Committed(await Choose(Current.Choice!.Key, "decline"));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<OverrunResolved>());
        Assert.True(Current.Movement is { WindowOpen: true, Reaction: true, Overrun: null });
        Refused(await Do(GameActions.Overrun, NoRoll(), new
        {
        }), "play.overrun");
        NoReplayErrors();
    }

    [Fact]
    public async Task ABogOnEntryStillMakesTheConcealedUnitsChoose()
    {
        // A12.41 (R11.12; table-player finding): the tank bogs entering the concealed squad's woods hex (4 + 4 + 4), and the squad still reveals itself or
        // takes its PAATC.
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L(woods), "russian", Conditions.Concealed), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        Committed(await Step("de-tk", "enter", L(woods), roller: Once(4, 4)));
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Bogged));
        Assert.Equal(ChoicePending.Paatc, Current.Choice!.Kind);
        NoReplayErrors();
    }

    [Fact]
    public async Task InTheMphUnitsFireAtAVehicleInTheirOwnLocationOnlyAfterItsOvr()
    {
        // D7.22 (table-player finding): the truck enters the Russian squad's hex without an OVR; the squad may not fire at it from within the Location.
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        Committed(await Step("de-t", "enter", L(Ne("E5"))));
        Refused(await Do(GameActions.Fire, Once(3, 3), new
        {
            firers = R1,
            target = L(Ne("E5"))
        }), "play.fire-reaction");
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadReducedInCcWithAVehicleHasMadeItsAttackAndSoHasItsHalfSquad()
    {
        // A11.5, A11.31 (table-player finding): the Russian squad's Original 12 against the halftrack Reduces it to a HS; after the German pass, the CC is
        // over, since the HS has attacked.
        await Setup("russian", Vehicle("de-ht", "attacker-halftrack", L(Ne("E5")), "german"), Squad("r1", L("E5"), "russian"), Squad("g1", L("J9"), "german"));
        await Advance(4);
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = R1,
            to = L(Ne("E5"))
        }));
        await Advance();
        Committed(await VehicleCc(L(Ne("E5")), Once(6, 6), vehicle: "de-ht", attackers: ["r1"]));
        Assert.Contains(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Side == "russian" && unit.Kind == "asl:half-squad");
        Committed(await VehicleCc(L(Ne("E5")), NoRoll(), pass: true));
        Assert.True(Current.CloseCombats.Single().Closed);
        NoReplayErrors();
    }

    [Fact]
    public async Task InMudAnUnpavedRoadIntoWoodsCostsTheOpenGroundRate()
    {
        // E3.6, E3.64 (backlog pass 16; referee, pass 16): a tank using a dirt road into woods pays Open Ground (1 MP) and 1 MP for Mud, with no Bog DR.
        var woods = Ne("E8");
        board.Terrain[woods] = "Woods";
        board.Side("E8", HexsideDirection.NorthEast, terrain: "Dirt Road");
        rules = ["weather:mud"];
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-tk", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-tk", "enter", L(woods)));
        Assert.Equal(4, LastStep(before).HalfMp);
        NoReplayErrors();
    }

    [Fact]
    public async Task InDeepSnowARoadEntryCostsAtLeastOneMp()
    {
        // E3.7331 (referee, pass 16): a truck along an unplowed road pays the road entry of at least 1 MP and 2 MP more for Deep Snow.
        board.Side("E8", HexsideDirection.NorthEast, terrain: "Dirt Road");
        rules = ["weather:deep-snow"];
        await Setup("german", Vehicle("de-t", "attacker-truck", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Committed(await Step("de-t", "start"));
        await Pass();
        var before = Revision;
        Committed(await Step("de-t", "enter", L(Ne("E8"))));
        Assert.Equal(6, LastStep(before).HalfMp);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABuAfvWhoseNvrIsZeroOnlyStops()
    {
        // E1.52 (referee, pass 16): at a Base NVR of 1 a BU AFV's NVR is 0, so it spends no MP but to Stop.
        rules = ["night:1"];
        await Setup("german", Vehicle("de-tk", "attacker-tank", L("E8"), "german", "east", "asl:bu"), Squad("r1", L("J9"), "russian"));
        Refused(await Step("de-tk", "start"), "play.night-bu");
    }
}
