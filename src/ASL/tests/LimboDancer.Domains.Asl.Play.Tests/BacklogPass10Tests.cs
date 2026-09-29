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
/// The backlog pass 10 in live play: step costs over walls, hills, marsh, and building levels (R10.1 to R10.3), wall TEM and Wall Advantage
/// (R10.5, R10.6), Height Advantage (R10.4), Bypass (R10.7), the Road and leader bonuses (R10.8), Minimum Move (R10.9), concealed movement
/// (R10.10), entry into concealed enemy Locations (R10.11), Snap Shots (R10.13), and berserk charges (R10.15). A board 01 grid whose terrain
/// each test draws, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass10Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f510");
    private static readonly GameScope Scope = new(Tenant, "pass10");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] G1L1 = ["g1", "l1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass10-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass10Board board = new();

    public BacklogPass10Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>
    private sealed class Pass10Board
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

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 10 test board", new HexFactSet(geometry, "pass10", hexes));
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
                ["catalog"] = "asl-scenario-a1@1.10.0",
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

    [Fact]
    public async Task AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap()
    {
        // B9.4 (R10.1): C5 to C4 across a wall costs 1 + 1 MF; D5 to D4 across a wall with a road through it costs the road's 1 MF.
        board.Side("C5", HexsideDirection.North, hexside: "Wall");
        board.Side("D5", HexsideDirection.North, terrain: "Dirt Road", hexside: "Hedge");
        await Setup(Squad("g1", L("C5"), "german"), Squad("g2", L("D5"), "german"), Squad("r1", L("J9"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("C4")));
        Assert.Equal(4, LastStep(before).HalfMf);
        await Pass();
        Committed(await EndMove());
        before = Revision;
        Committed(await Move(G2, L("D4")));
        Assert.Equal(2, LastStep(before).HalfMf);
        NoReplayErrors();
    }

    [Fact]
    public async Task HillsDoubleTheCostUpAndAbruptChangesAddPerLevel()
    {
        // A4.133, B10.4, B10.51 (R10.3): up one level to Open Ground 2 MF; up two levels 2 + 2 MF; down two levels 1 + 1 MF.
        board.Base["C4"] = 1;
        board.Base["E4"] = 2;
        board.Base["F6"] = 2;
        await Setup(Squad("g1", L("C5"), "german"), Squad("g2", L("E5"), "german"), Unit("g3", "asl:squad", "attacker-squad", L("F6"), "german"), Squad("r1", L("J9"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("C4")));
        Assert.Equal(4, LastStep(before).HalfMf);
        await Pass();
        Committed(await EndMove());
        before = Revision;
        Committed(await Move(G2, L("E4")));
        Assert.Equal(8, LastStep(before).HalfMf);
        await Pass();
        Committed(await EndMove());
        before = Revision;
        Committed(await Move(["g3"], L(Pass10Board.N("F6", HexsideDirection.South))));
        Assert.Equal(4, LastStep(before).HalfMf);
        NoReplayErrors();
    }

    [Fact]
    public async Task StairwellsConnectLevelsAndUpperLevelsStayInTheirBuilding()
    {
        // B23.4, B23.421, B23.422 (R10.2): up the stairwell of D5 for 1 MF, across to D4 at level 1 for 2 MF; no stairwell in D4; no step out of the building.
        const string building = "Stone Building, 2 Level";
        foreach (var hex in new[] { "D5", "D4" })
        {
            board.Terrain[hex] = building;
            board.Upper[hex] = [building, building];
        }

        board.Stairs.Add("D5");
        board.Side("D5", HexsideDirection.North, terrain: building);
        await Setup(Squad("g1", L("D5"), "german"), Squad("r1", L("J9"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("D5", 1)));
        Assert.Equal(2, LastStep(before).HalfMf);
        await Pass();
        before = Revision;
        Committed(await Move(G1, L("D4", 1)));
        Assert.Equal(4, LastStep(before).HalfMf);
        await Pass();
        Refused(await Move(G1, L("D4", 2)), "play.move-stairwell");
        Refused(await Move(G1, L("D3", 1)), "play.move-");
        Refused(await Move(G1, L("D3")), "play.move-upper-level");
        NoReplayErrors();
    }

    [Fact]
    public async Task MarshTakesTheWholeAllotment()
    {
        // B16.4 (R10.1): a fresh squad enters marsh for its four MF; one that has moved may not; from a lower hex only by Minimum Move.
        board.Terrain["C4"] = "Marsh";
        board.Terrain["E4"] = "Marsh";
        board.Base["E4"] = 1;
        await Setup(Squad("g1", L("C5"), "german"), Squad("g2", L("C6"), "german"), Squad("g3", L("E5"), "german"), Squad("r1", L("J9"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("C4")));
        Assert.Equal(8, LastStep(before).HalfMf);
        await Pass();
        Committed(await EndMove());
        Committed(await Move(G2, L("C5")));
        await Pass();
        Refused(await Move(G2, L("C4")), "play.move-marsh");
        Committed(await EndMove());
        Refused(await Move(["g3"], L("E4")), "play.move-marsh");
        before = Revision;
        Committed(await Move(["g3"], L("E4"), minimumMove: true));
        Assert.Equal(16, LastStep(before).HalfMf);
        NoReplayErrors();
    }

    [Fact]
    public async Task AMinimumMoveEntersOneHexAndLeavesTheUnitPinnedAndCx()
    {
        // A4.134 (R10.9): woods two levels up costs 2 + 4 MF, more than the squad's four; a Minimum Move enters it, and once the DEFENDER passes
        // the squad is pinned and CX with its move ended.
        board.Terrain["C4"] = "Woods";
        board.Base["C4"] = 2;
        await Setup(Squad("g1", L("C5"), "german"), Squad("r1", L("J9"), "russian"));
        Refused(await Move(G1, L("C4")), "play.move-mf");
        Committed(await Move(G1, L("C4"), minimumMove: true));
        await Pass();
        var squad = Current.Unit("g1")!;
        Assert.True(squad.MovementEnded);
        Assert.True(Is(squad, Conditions.Pinned));
        Assert.True(Is(squad, Conditions.Cx));
        NoReplayErrors();
    }

    [Fact]
    public async Task TheRoadBonusAddsOneMfToAMoveAlongTheRoadOnly()
    {
        // B3.4 (R10.8): along the road from C1 a squad enters five hexes at 1 MF; leaving the road loses the bonus.
        var hex = "C1";
        for (var index = 0; index < 6; index++)
        {
            board.Side(hex, HexsideDirection.South, terrain: "Dirt Road");
            hex = Pass10Board.N(hex, HexsideDirection.South);
        }

        await Setup(Squad("g1", L("C1"), "german"), Squad("g2", L("E1"), "german"), Squad("r1", L("J9"), "russian"));
        hex = "C1";
        for (var index = 0; index < 5; index++)
        {
            hex = Pass10Board.N(hex, HexsideDirection.South);
            Committed(await Move(G1, L(hex)));
            await Pass();
        }

        Refused(await Move(G1, L(Pass10Board.N(hex, HexsideDirection.South))), "play.move-mf");
        Committed(await EndMove());

        // Off the road the same squad has four MF.
        hex = "E1";
        for (var index = 0; index < 4; index++)
        {
            hex = Pass10Board.N(hex, HexsideDirection.South);
            Committed(await Move(G2, L(hex)));
            await Pass();
        }

        Refused(await Move(G2, L(Pass10Board.N(hex, HexsideDirection.South))), "play.move-mf");
        NoReplayErrors();
    }

    [Fact]
    public async Task ALeaderMovingWithASquadAddsTwoMf()
    {
        // A4.12 (R10.8): a 4-6-7 and a 7-0 that begin the MPh together move six hexes of Open Ground together.
        await Setup(Squad("g1", L("C8"), "german"), Unit("l1", "asl:leader", "attacker-leader-7-0", L("C8"), "german"), Squad("r1", L("J9"), "russian"));
        var hex = "C8";
        for (var index = 0; index < 6; index++)
        {
            hex = Pass10Board.N(hex, HexsideDirection.North);
            Committed(await Move(G1L1, L(hex)));
            await Pass();
        }

        Refused(await Move(G1L1, L(Pass10Board.N(hex, HexsideDirection.North))), "play.move-mf");
        NoReplayErrors();
    }

    [Fact]
    public async Task ConcealedUnitsMoveAndLoseTheirConcealmentInTheOpen()
    {
        // A12.14 (R10.10): in the LOS of a Good Order enemy within 16 hexes, non-Assault Movement reveals a concealed squad; Assault Movement into
        // woods keeps its "?"; a Dummy moving in the open is removed.
        board.Terrain["E4"] = "Woods";
        await Setup(Squad("g1", L("C5"), "german", Conditions.Concealed), Squad("g2", L("E5"), "german", Conditions.Concealed), Dummy("gd", L("G5"), "german"),
            Squad("r1", L("E9"), "russian"));
        Committed(await Move(G1, L("C4")));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Concealed));
        await Pass();
        Committed(await EndMove());
        Committed(await Move(G2, L("E4"), assault: true));
        Assert.True(Is(Current.Unit("g2")!, Conditions.Concealed));
        await Pass();
        Committed(await EndMove());
        Committed(await Move(["gd"], L("G4")));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("gd")!.Status);
        NoReplayErrors();
    }

    [Fact]
    public async Task EnteringAConcealedEnemyForcesTheStackBackAndDummiesAreRemoved()
    {
        // A12.15 (R10.11): the concealed Russian squad in C4 is revealed and the German squad stays in C5 with the MF spent, its move ending; the
        // Dummy in E4 is removed and the squad enters.
        board.Terrain["C4"] = "Woods";
        await Setup(Squad("g1", L("C5"), "german"), Squad("g2", L("E5"), "german"), Squad("r1", L("C4"), "russian", Conditions.Concealed), Dummy("rd", L("E4"), "russian"),
            Squad("r2", L("J9"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("C4")));
        var step = LastStep(before);
        Assert.Equal((At("C5"), At("C4"), 4), (step.To, step.Attempted!, step.HalfMf));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Equal(At("C5"), Current.Location("g1")!.Location);
        await Pass();
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.Null(Current.Movement);
        Committed(await Move(G2, L("E4")));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rd")!.Status);
        Assert.Equal(At("E4"), Current.Location("g2")!.Location);

        NoReplayErrors();
    }

    private static BoardLocation At(string hex) => BoardLocation.Parse(L(hex));

    [Fact]
    public async Task BypassRefusesWallsFriendsSplitsAndEnemyObstacles()
    {
        // Table player, pass 10: no Bypass of a hex with a hedge or with friendly units; a stack in Bypass moves together; it does not occupy an
        // obstacle holding enemy units; marsh is never entered by Assault Movement.
        board.Terrain["D4"] = "Woods";
        board.Terrain["F4"] = "Woods";
        board.Terrain["H4"] = "Woods";
        board.Terrain["C4"] = "Marsh";
        foreach (var hex in new[] { "D4", "F4", "H4" })
        {
            board.Side(hex, HexsideDirection.SouthEast, terrain: "Open Ground");
            board.Side(hex, HexsideDirection.NorthEast, terrain: "Open Ground");
        }

        board.Side("F4", HexsideDirection.North, hexside: "Hedge");
        await Setup(Squad("g1", L("D5"), "german"), Squad("g2", L("D5"), "german"), Squad("g3", L("F5"), "german"), Squad("g4", L("H5"), "german"),
            Squad("g5", L("H4"), "german"), Squad("g6", L("C5"), "german"), Squad("r1", L("D4"), "russian", Conditions.Concealed), Squad("r2", L("J9"), "russian"));
        Refused(await Move(["g6"], L("C4"), assault: true), "play.move-assault");
        Refused(await Move(["g3"], L("F4"), bypass: ["southeast", "northeast"]), "play.move-bypass");
        Refused(await Move(["g4"], L("H4"), bypass: ["southeast", "northeast"]), "play.move-bypass");
        Committed(await Move(["g1", "g2"], L("D4"), bypass: ["southeast", "northeast"]));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        await Pass();
        Refused(await Move(G1, L(Pass10Board.N("D4", HexsideDirection.North))), "play.move-bypass");
        Refused(await Move(["g1", "g2"], L("D4")), "play.move-bypass");
        Committed(await Move(["g1", "g2"], L(Pass10Board.N("D4", HexsideDirection.North))));
        NoReplayErrors();
    }

    [Fact]
    public async Task ADummyMovesWithItsLeadersBonus()
    {
        // A12.11 (table player, pass 10): a Dummy and a leader of its side that begin the MPh together move six hexes together.
        await Setup(Dummy("gd", L("C8"), "german"), Unit("l1", "asl:leader", "attacker-leader-7-0", L("C8"), "german"), Squad("r1", L("J2"), "russian"));
        los.Blocked.Add("J2");
        var hex = "C8";
        for (var index = 0; index < 6; index++)
        {
            hex = Pass10Board.N(hex, HexsideDirection.North);
            Committed(await Move(["gd", "l1"], L(hex)));
            await Pass();
        }

        NoReplayErrors();
    }

    [Fact]
    public async Task AStackOfDummiesEnteringConcealedUnitsIsRemoved()
    {
        // A12.15 (referee, pass 10): the DEFENDER asks the moving "?" to show a real unit; a Dummy stack cannot, and is removed.
        await Setup(Dummy("gd", L("C5"), "german"), Squad("r1", L("C4"), "russian", Conditions.Concealed), Squad("r2", L("J9"), "russian"));
        Committed(await Move(["gd"], L("C4")));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("gd")!.Status);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage()
    {
        // B9.3, B9.31 (R10.5, R10.6): the German squad enters C4, behind a wall on its north hexside. From C2, across the wall, the Russians fire
        // with +2 and no FFMO. From C3, sharing the wall, the Russian squad there first holds Wall Advantage: FFMO and no wall TEM.
        board.Side("C4", HexsideDirection.North, hexside: "Wall");
        await Setup(Squad("g1", L("C5"), "german"), Squad("r1", L("C2"), "russian"), Squad("r2", L("C3"), "russian"));
        Committed(await Move(G1, L("C4")));
        var before = Revision;
        Committed(await Fire(R1, L("C4"), false, 6, 5));
        var walled = LastFire(before);
        Assert.Contains(("wall", 2m), Drm(walled));
        Assert.DoesNotContain(Drm(walled), item => item.Name == "ffmo");
        before = Revision;
        Committed(await Fire(R2, L("C4"), false, 6, 6, 6, 6, 6, 6));
        var advantage = LastFire(before);
        Assert.DoesNotContain(Drm(advantage), item => item.Name == "wall");
        Assert.Contains(("ffmo", -1m), Drm(advantage));
        NoReplayErrors();
    }

    [Fact]
    public async Task HeightAdvantageExceptAcrossTheClimbedCrestLine()
    {
        // B10.31 (R10.4): the squad climbs from C5 into C4 on a level 1 hill. From C2 the LOS crosses C4's north hexside: +1 and no FFMO. From C7 it
        // crosses the south hexside the squad climbed across: FFMO and no Height Advantage.
        board.Base["C4"] = 1;
        await Setup(Squad("g1", L("C5"), "german"), Squad("r1", L("C2"), "russian"), Squad("r2", L("C7"), "russian"));
        Committed(await Move(G1, L("C4")));
        var before = Revision;
        Committed(await Fire(R1, L("C4"), false, 6, 5));
        Assert.Contains(("height-advantage", 1m), Drm(LastFire(before)));
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "ffmo");
        before = Revision;
        Committed(await Fire(R2, L("C4"), false, 6, 5));
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "height-advantage");
        Assert.Contains(("ffmo", -1m), Drm(LastFire(before)));
        NoReplayErrors();
    }

    [Fact]
    public async Task ASnapShotIsHalvedAndLeavesNoResidualFp()
    {
        // A8.15 (R10.13): at the C5-C4 hexside the squad crossed, from E3.
        await Setup(Squad("g1", L("C5"), "german"), Squad("r1", L("E3"), "russian"));
        Committed(await Move(G1, L("C4")));
        var before = Revision;
        Committed(await Fire(R1, L("C4"), true, 6, 5));
        var snap = LastFire(before);
        Assert.Contains("snap-shot", Multipliers(snap));
        Assert.DoesNotContain(Drm(snap), item => item.Name is "ffmo" or "ffnam");
        Assert.DoesNotContain(Current.ResidualFire, item => item.Location == At("C4"));
        NoReplayErrors();
    }

    [Fact]
    public async Task BypassMovesAlongTheOpenHexsidesOfAWoodsHex()
    {
        // A4.3, A4.31 (R10.7): D4 is woods touching all but its southeast and northeast hexsides. The squad from D5 Bypasses along those two for
        // 1 MF, is fired on in the open, may not end its move there, and leaves north through the far vertex.
        board.Terrain["D4"] = "Woods";
        board.Side("D4", HexsideDirection.SouthEast, terrain: "Open Ground");
        board.Side("D4", HexsideDirection.NorthEast, terrain: "Open Ground");
        var southeast = Pass10Board.N(Pass10Board.N("D4", HexsideDirection.SouthEast), HexsideDirection.SouthEast);
        await Setup(Squad("g1", L("D5"), "german"), Squad("r1", L(southeast), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("D4"), bypass: ["southeast", "northeast"]));
        Assert.Equal(2, LastStep(before).HalfMf);
        before = Revision;
        Committed(await Fire(R1, L("D4"), false, 6, 5));
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "tem:woods");
        Assert.Contains(("ffmo", -1m), Drm(LastFire(before)));
        await Pass();
        Refused(await EndMove(), "play.end-move-bypass");
        Refused(await Move(G1, L(Pass10Board.N("D4", HexsideDirection.South))), "play.move-bypass");
        Committed(await Move(G1, L(Pass10Board.N("D4", HexsideDirection.North))));
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkChargeOverrunsALoneSmcAndRevealsConcealedUnitsOnItsRoute()
    {
        // A15.432 (R10.15): the berserk squad in C6 charges the lone Russian leader in C4 and enters its Location. A15.431, A12.15: the second
        // berserk squad, charging the Russian squad in G3, enters the concealed Russian squad in G5 on its route, reveals it, and stays.
        await Setup(Squad("g1", L("C6"), "german", Conditions.Berserk), Unit("rl", "asl:leader", "defender-leader-7-0", L("C4"), "russian"),
            Squad("g2", L("G6"), "german", Conditions.Berserk), Squad("r2", L("G5"), "russian", Conditions.Concealed), Squad("r3", L("G3"), "russian"));
        Committed(await Move(G1, L("C5")));
        await Pass();
        Committed(await Move(G1, L("C4")));
        Assert.Equal(At("C4"), Current.Location("g1")!.Location);
        await Pass();
        Committed(await EndMove());
        Committed(await Move(G2, L("G5")));
        Assert.False(Is(Current.Unit("r2")!, Conditions.Concealed));
        Assert.Equal(At("G5"), Current.Location("g2")!.Location);
        NoReplayErrors();
    }
}
