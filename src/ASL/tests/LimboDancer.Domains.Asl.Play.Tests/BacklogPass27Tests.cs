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
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 27 of the Card Play and Map Studio Redesign Plan in live play (rulings R27.1 to R27.3): a berserk charge's route through Bypass and
/// stairwells, its Random Selection among Dummies, the CC of its OVR onto a lone SMC in the MPh, and Hungarians against Romanians. The pass 10
/// test board: board 01's grid, Open Ground at level 0, with the terrain each test draws on it, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass27Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f527");
    private static readonly GameScope Scope = new(Tenant, "pass27");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] Rl = ["rl"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] G1L1 = ["g1", "l1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass27-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass27Board board = new();

    public BacklogPass27Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>
    private sealed class Pass27Board
    {
        private static readonly BoardHandle Blank = Build(new Dictionary<string, string>(), new Dictionary<string, int>(), new HashSet<string>(), new Dictionary<string, string[]>(), []);

        public Dictionary<string, string> Terrain { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Base { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Stairs { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string[]> Upper { get; } = new(StringComparer.Ordinal);

        public List<(string Hex, HexsideDirection Side, string? Terrain, string? Hexside)> Sides { get; } = [];

        public static string N(string hex, HexsideDirection side) => Blank.Neighbor(HexName.Parse(hex), side)!.ToString()!;

        public void Side(string hex, HexsideDirection side, string? terrain = null, string? hexside = null) => Sides.Add((hex, side, terrain, hexside));

        public BoardHandle Handle() => Build(Terrain, Base, Stairs, Upper, Sides);

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

        private static BoardHandle Build(IReadOnlyDictionary<string, string> terrain, IReadOnlyDictionary<string, int> levels, HashSet<string> stairs,
            IReadOnlyDictionary<string, string[]> upper, IReadOnlyList<(string Hex, HexsideDirection Side, string? Terrain, string? Hexside)> sides)
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
                LocationFacts[] locations = [center, .. (upper.GetValueOrDefault(text) ?? []).Select((level, at) => new LocationFacts(at + 1, Type(level), null))];
                HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => marked.TryGetValue((text, side), out var mark)
                    ? new HexsideFacts(side, true, Type(mark.Terrain ?? terrain.GetValueOrDefault(text) ?? "Open Ground"), mark.Hexside is { } wall ? Type(wall) : null, false, false, false, false, null)
                    : new HexsideFacts(side, true, Type(terrain.GetValueOrDefault(text) ?? "Open Ground"), null, false, false, false, false, null))];
                hexes.Add(new HexFacts(name, index, levels.GetValueOrDefault(text), stairs.Contains(text), center, locations, hexsides, null, CenterTerrainSource.CenterSample));
            }

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 10 test board", new HexFactSet(geometry, "pass27", hexes));
        }
    }

    // Clear LOS at the board's true range, except to the Locations named blocked.
    private sealed class StubLos : IFireLosReader
    {
        public HashSet<string> Blocked { get; } = new(StringComparer.Ordinal);

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            return Blocked.Contains(target.Hex.ToString()) || Blocked.Contains(from.Hex.ToString())
                ? new(LosStatus.Blocked, true, range, 0, null, string.Empty)
                : new(LosStatus.Clear, false, range, 0, null, string.Empty);
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

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task Setup(params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 10",
                ["catalog"] = "asl-scenario-a1@1.13.0",
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
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Move(string[] units, string to, bool assault = false, bool minimumMove = false, string[]? bypass = null, DiceRoller? roller = null)
    {
        var arguments = new Dictionary<string, object> { ["unitIds"] = units, ["to"] = to };
        if (assault)
        {
            arguments["assault"] = true;
        }

        if (minimumMove)
        {
            arguments["minimumMove"] = true;
        }

        if (bypass is not null)
        {
            arguments["bypass"] = bypass;
        }

        return Do(GameActions.Move, roller ?? NoRoll(), arguments);
    }

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private Task<PlayResult> EndMove() => Do(GameActions.EndMove, NoRoll(), new
    {
    });

    private Task<PlayResult> Fire(string[] firers, string target, bool snapShot, params int[] dice) => Do(GameActions.Fire, Once(dice), snapShot
        ? new Dictionary<string, object> { ["firers"] = firers, ["target"] = target, ["snapShot"] = true }
        : new Dictionary<string, object> { ["firers"] = firers, ["target"] = target });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private MovementStepped LastStep(long before) => Since(before).Select(item => item.Payload).OfType<MovementStepped>().Last();

    private FireResolved LastFire(long before) => Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();

    private static IEnumerable<(string Name, decimal Value)> Drm(FireResolved record) =>
        record.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray().Select(item => (item.GetProperty("name").GetString()!, item.GetProperty("value").GetDecimal()));

    private static IEnumerable<string> Multipliers(FireResolved record) =>
        record.Resolution.GetProperty("arithmetic").GetProperty("firers").EnumerateArray().SelectMany(item => item.GetProperty("multipliers").EnumerateArray())
            .Select(item => item.GetProperty("name").GetString()!);

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static string L(string hex, int level = 0) => $"bd01:{hex}:{level}";

    private static BoardLocation At(string hex, int level = 0) => BoardLocation.Parse(L(hex, level));

    private (UnitInstance Unit, BoardLocation? Target, IReadOnlyList<BoardLocation> Next, string? Undecided) Charge(string unit) =>
        Assert.Single(Planner().Charges(Current), item => item.Unit.Id == unit);

    [Fact]
    public async Task ABerserkChargeTakesTheShorterRouteInBypass()
    {
        // A15.431, A4.3 (ruling R27.2): D4 is woods but for its southeast and northeast hexsides. Bypassing it along them (1 MF) and leaving north to
        // D3 (1 MF) is shorter than entering the woods (2 MF) or going round it (3 MF), so the charge's only first step is that Bypass.
        board.Terrain["D4"] = "Woods";
        board.Side("D4", HexsideDirection.SouthEast, terrain: "Open Ground");
        board.Side("D4", HexsideDirection.NorthEast, terrain: "Open Ground");
        await Setup(Squad("g1", L("D5"), "german", Conditions.Berserk), Squad("r1", L("D3"), "russian"));
        Assert.Equal([At("D4")], Charge("g1").Next);
        Refused(await Move(G1, L("D4")), "in Bypass along southeast and northeast");
        Committed(await Move(G1, L("D4"), bypass: ["southeast", "northeast"]));
        await Pass();
        Committed(await Move(G1, L("D3")));
        Assert.Equal(At("D3"), Current.Location("g1")!.Location);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkChargeClimbsAStairwellToAnEnemyUpstairs()
    {
        // B23.4, B23.421 (ruling R27.2): the Russian squad is on level 1 of D4, which has no stairwell; the charge climbs D5's stairwell and crosses
        // at level 1, its only route.
        const string building = "Stone Building, 2 Level";
        foreach (var hex in new[] { "D5", "D4" })
        {
            board.Terrain[hex] = building;
            board.Upper[hex] = [building, building];
        }

        board.Stairs.Add("D5");
        board.Side("D5", HexsideDirection.North, terrain: building);
        await Setup(Squad("g1", L("D5"), "german", Conditions.Berserk), Squad("r1", L("D4", 1), "russian"));
        Assert.Equal([At("D5", 1)], Charge("g1").Next);
        Committed(await Move(G1, L("D5", 1)));
        await Pass();
        Committed(await Move(G1, L("D4", 1)));
        Assert.Equal(At("D4", 1), Current.Location("g1")!.Location);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkChargeIntoConcealedUnitsDrawsDummiesOnlyUntilARealUnit()
    {
        // A.9 (ruling R27.3): the charge at the Russian squad in C3 enters C5, where a concealed squad and two Dummies are. One dr each, by id:
        // r2 4, rd1 6, rd2 2. rd1 is drawn first and eliminated; r2 is revealed; rd2, drawn below it, stays.
        await Setup(Squad("g1", L("C6"), "german", Conditions.Berserk), Squad("r2", L("C5"), "russian", Conditions.Concealed), Dummy("rd1", L("C5"), "russian"),
            Dummy("rd2", L("C5"), "russian"), Squad("r3", L("C3"), "russian"));
        Committed(await Move(G1, L("C5"), roller: Once(4, 6, 2)));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rd1")!.Status);
        Assert.Equal(InstanceStatus.Active, Current.Unit("rd2")!.Status);
        Assert.False(Is(Current.Unit("r2")!, Conditions.Concealed));
        Assert.Equal(At("C5"), Current.Location("g1")!.Location);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkOverrunOfALoneSmcHasItsCcAtOnceInTheMph()
    {
        // A4.152, A15.432 (ruling R27.3): the berserk squad enters the lone Russian leader's Location with no NTC; after the DEFENDER's window the CC
        // comes before anything else, in the MPh. Both miss, so both are held in Melee for the CCPh.
        await Setup(Squad("g1", L("C5"), "german", Conditions.Berserk), Unit("rl", "asl:leader", "defender-leader-7-0", L("C4"), "russian"));
        Committed(await Move(G1, L("C4")));
        await Pass();
        Assert.Equal(At("C4"), Planner().BerserkOverrunPending(Current));
        Refused(await EndMove(), "play.cc-overrun-first");
        Refused(await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = L("C4"),
            attacks = new[] { new { attackers = Rl, defenders = G1 } },
        }), "play.cc-overrun");
        Committed(await Do(GameActions.CloseCombat, Once(6, 6, 6, 6), new
        {
            location = L("C4"),
            attacks = new[] { new { attackers = G1, defenders = Rl }, new { attackers = Rl, defenders = G1 } },
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Melee));
        Assert.True(Is(Current.Unit("rl")!, Conditions.Melee));
        Assert.Null(Planner().BerserkOverrunPending(Current));
        Committed(await EndMove());
        NoReplayErrors();
    }

    [Fact]
    public async Task HungariansAgainstRomaniansStartUnderNoQuarterAndAnAxisMinorSideNamesItsNation()
    {
        // A25.8 (ruling R27.1): an Axis Minor side names its nation; Hungarians fighting Romanians face No Quarter on both sides from the start.
        var refused = await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 27",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = "hungarian",
                ["sides"] = new object[] { new { id = "hungarian", nationality = "axis-minor", elr = 2 }, new { id = "romanian", nationality = "axis-minor", nation = "romanian", elr = 2 } },
            },
            placements = new[] { Unit("h1", "asl:squad", "axis-minor-squad", L("C5"), "hungarian") },
        }));
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-2",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 27",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = "hungarian",
                ["sides"] = new object[]
                {
                    new { id = "hungarian", nationality = "axis-minor", nation = "hungarian", elr = 2 },
                    new { id = "romanian", nationality = "axis-minor", nation = "romanian", elr = 2 },
                },
            },
            placements = new[] { Unit("h1", "asl:squad", "axis-minor-squad", L("C5"), "hungarian"), Unit("o1", "asl:squad", "axis-minor-elite-squad", L("C3"), "romanian") },
        })));
        Assert.Equal(["hungarian", "romanian"], Current.NoQuarter.Order(StringComparer.Ordinal));
        Assert.Equal("hungarian", Current.Side("hungarian")!.Nation);
        NoReplayErrors();
    }
}
