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
/// The backlog pass 12 in live play: Opportunity Fire (R12.1), fire at a blocked LOS (R12.2), split fire (R12.4), the concealment gained as a Player
/// Turn ends (R12.5), Spraying Fire (R12.6), Fire Lanes (R12.7), fire into a Melee and at prisoners (R12.8, R12.9), and Encirclement (R12.11). A board
/// 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader a test may block.
/// </summary>
public sealed class BacklogPass12Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f512");
    private static readonly GameScope Scope = new(Tenant, "pass12");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] G1 = ["g1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass12-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass12Board board = new();

    public BacklogPass12Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>
    private sealed class Pass12Board
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

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 12 test board", new HexFactSet(geometry, "pass12", hexes));
        }
    }

    // Clear LOS at the board's true range, unless the test blocks it everywhere or from one hex.
    private sealed class StubLos : IFireLosReader
    {
        public bool Blocked
        {
            get; set;
        }

        public HashSet<string> BlockedFrom { get; } = new(StringComparer.Ordinal);

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            var blocked = from != target && (Blocked || BlockedFrom.Contains(from.Hex.ToString()));
            return new(blocked ? LosStatus.Blocked : LosStatus.Clear, blocked, range, 0, null, string.Empty);
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

    private static Dictionary<string, object> Mg(string id, string definition, string holder, string side) => new()
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

    private Task Setup(string firstSide, params Dictionary<string, object>[] placements) => SetupAt(2, firstSide, placements);

    private async Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 12",
                ["catalog"] = "asl-scenario-a1@1.11.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        })));
        await Advance(advances);
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

    private static string Ne(string hex) => Pass12Board.N(hex, HexsideDirection.NorthEast);

    private static string Se(string hex) => Pass12Board.N(hex, HexsideDirection.SouthEast);

    private static string Nw(string hex) => Pass12Board.N(hex, HexsideDirection.NorthWest);

    private static string Sw(string hex) => Pass12Board.N(hex, HexsideDirection.SouthWest);

    private (int Spent, int Allotment) Mp(string vehicle) => GamePlanner.VehicleHalfMp(Current.Unit(vehicle)!);

    private Task<PlayResult> Move(string[] units, string to, DiceRoller? roller = null) =>
        Do(GameActions.Move, roller ?? NoRoll(), new Dictionary<string, object> { ["unitIds"] = units, ["to"] = to });

    private Task<PlayResult> Fire(string[] firers, string target, DiceRoller roller, Dictionary<string, object>? options = null)
    {
        var arguments = new Dictionary<string, object> { ["firers"] = firers, ["target"] = target };
        foreach (var (name, value) in options ?? [])
        {
            arguments[name] = value;
        }

        return Do(GameActions.Fire, roller, arguments);
    }

    private static string N(string hex) => Pass12Board.N(hex, HexsideDirection.North);

    private FireResolved[] Fires(long since) => [.. Since(since).Select(item => item.Payload).OfType<FireResolved>()];

    private static DiceRoller Threes() => new(_ => 2);

    [Fact]
    public async Task OpportunityFireHoldsFireForTheAfphAtFullFp()
    {
        // A7.25 (ruling R12.1): g1 is marked with a Bounding Fire counter in its PFPh; it may not fire then or move in the MPh.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"));
        Assert.Equal("pfph", Current.Phase);
        Committed(await Do(GameActions.OpportunityFire, NoRoll(), new
        {
            unitIds = G1
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.BoundingFire));
        Refused(await Fire(["g1"], L(Ne("E5")), NoRoll()), "play.fire-barred");
        await Advance();
        Refused(await Move(["g1"], L(Se("E5"))), "play.");

        // A7.24, A7.25: in the AFPh its PBF 8 FP is not halved.
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        var before = Revision;
        Committed(await Fire(["g1"], L(Ne("E5")), Once(6, 5)));
        Assert.Equal(8m, Fires(before).Single().Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        NoReplayErrors();
    }

    [Fact]
    public async Task FireAtABlockedLosStillCountsAsFire()
    {
        // A6.11 (ruling R12.2): the LOS is blocked; the DR is made, g1 is marked Prep Fire, and r1 is untouched.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Squad("r1", L(Ne(Ne("E5"))), "russian"));
        los.Blocked = true;
        var before = Revision;
        Committed(await Fire(["g1"], L(Ne(Ne("E5"))), Once(1, 2)));
        Assert.True(Fires(before).Single().Resolution.GetProperty("losBlocked").GetBoolean());
        Assert.True(Is(Current.Unit("g1")!, Conditions.PrepFire));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Broken) || Is(Current.Unit("r1")!, Conditions.Pinned));
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadFiresItsMgAndItsInherentFpInSeparateAttacks()
    {
        // A7.351 (ruling R12.4): g1's LMG fires alone at r1; g1 keeps its inherent FP and fires it at r2.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Mg("gmg", "attacker-lmg", "g1", "german"),
            Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L(Se("E5")), "russian"));
        Committed(await Fire(["g1"], L(Ne("E5")), Once(6, 5), new()
        {
            ["weapons"] = new Dictionary<string, string[]> { ["g1"] = ["gmg"] },
            ["withoutInherent"] = G1,
        }));
        Assert.False(Is(Current.Unit("g1")!, Conditions.PrepFire));
        Committed(await Fire(["g1"], L(Se("E5")), Once(6, 5)));
        Assert.True(Is(Current.Unit("g1")!, Conditions.PrepFire));
        NoReplayErrors();
    }

    [Fact]
    public async Task SprayingFireAttacksTwoLocationsOnOneDr()
    {
        // A9.5 (ruling R12.6): g1 with its LMG sprays the two Russian hexes sharing a hexside: two records, one Original DR, g1 marked once.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Mg("gmg", "attacker-lmg", "g1", "german"),
            Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L(N("E5")), "russian"));
        var before = Revision;
        Committed(await Fire(["g1"], L(Ne("E5")), Once(6, 5), new()
        {
            ["weapons"] = new Dictionary<string, string[]> { ["g1"] = ["gmg"] },
            ["sprayTarget"] = L(N("E5")),
        }));
        var fires = Fires(before);
        Assert.Equal(2, fires.Length);
        Assert.Equal(fires[0].Rolls["attack"], fires[1].Rolls["attack"]);
        Assert.True(Is(Current.Unit("g1")!, Conditions.PrepFire));
        NoReplayErrors();
    }

    [Fact]
    public async Task AFireLaneAttacksALaterMoverInItsHexGrain()
    {
        // A9.22 (ruling R12.7): r1's LMG (2 FP) Defensive First Fires at g1 entering the ADJACENT hex and lays a Fire Lane one hex beyond: Residual
        // FP 1, the column left of 2, doubled ADJACENT; g2 then enters the far hex and is attacked by it.
        var near = Ne("E5");
        var far = Ne(near);
        await Setup("german", Squad("r1", L("E5"), "russian"), Mg("rmg", "defender-lmg", "r1", "russian"),
            Squad("g1", L(N(near)), "german"), Squad("g2", L(N(far)), "german"));
        Committed(await Move(["g1"], L(near)));
        var before = Revision;
        Committed(await Fire(["r1"], L(near), Once(5, 4, 2, 2, 2, 2), new()
        {
            ["weapons"] = new Dictionary<string, string[]> { ["r1"] = ["rmg"] },
            ["fireLane"] = new Dictionary<string, string> { ["weapon"] = "rmg", ["to"] = L(far) },
        }));
        var lane = Since(before).Select(item => item.Payload).OfType<FireLanePlaced>().Single();
        Assert.Equal([(L(near), 2), (L(far), 1)], lane.Entries.Select(item => (item.Location.ToString(), item.Fp)));

        // A9.22, A9.223 (referee, pass 12): the lane's MG does not fire again this MPh, nor its manning squad as Subsequent First Fire.
        Refused(await Fire(["r1"], L(near), NoRoll()), "play.fire-lane-mg");
        await Pass();
        Committed(await EndMove());
        before = Revision;
        Committed(await Move(["g2"], L(far), Threes()));
        var laneFire = Fires(before).Single();
        Assert.True(laneFire.Facts.GetProperty("fireLane").GetBoolean());
        NoReplayErrors();
    }

    [Fact]
    public async Task TwoAttacksFromOppositeHexsidesEncircleALocation()
    {
        // A7.7 (ruling R12.11): r1 and r2 fire consecutively at g1 from opposite hexsides of its hex; the second attack Encircles it.
        await SetupAt(1, "russian", Squad("g1", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L(Sw("E5")), "russian"));
        Committed(await Fire(["r1"], L("E5"), Once(6, 5)));
        Assert.Empty(Current.Encirclements);
        var before = Revision;
        Committed(await Fire(["r2"], L("E5"), Once(6, 5)));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<EncirclementPlaced>());
        Assert.True(Fires(before).Single().Facts.GetProperty("targets")[0].GetProperty("encircled").GetBoolean());
        Assert.True(Current.Encircled(Current.Unit("g1")!));
        NoReplayErrors();
    }

    [Fact]
    public async Task InfantryGainConcealmentAsTheirPlayerTurnEnds()
    {
        // A12.12, A12.122 (ruling R12.5): out of every enemy LOS, g1 in woods gains "?" (Case J); g2 in the open within 16 hexes of r1 makes a
        // Final Concealment dr, 2 + 3 (US#) = 5, and gains it (Case K).
        board.Terrain["E5"] = "Woods";
        await SetupAt(7, "german", Squad("g1", L("E5"), "german"), Squad("g2", L("E8"), "german"), Squad("r1", L("J9"), "russian"));
        Assert.Equal("ccph", Current.Phase);
        los.Blocked = true;
        Committed(await Do(GameActions.AdvancePhase, Once(2), new
        {
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("g2")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAttackThatEncirclesAndEliminatesItsTargetsStillCommits()
    {
        // A7.7 (table player, pass 12): r2's sealing attack, an Original 2 with Cowering on the 6 column, is a 1KIA that eliminates the lone German HS;
        // no Encirclement is placed on an empty Location, and the attack stands.
        await SetupAt(1, "russian", Unit("g1", "asl:half-squad", "attacker-half-squad", L("E5"), "german"), Squad("r1", L(Ne("E5")), "russian"),
            Squad("r2", L(Sw("E5")), "russian"));
        Committed(await Fire(["r1"], L("E5"), Once(6, 5)));
        Committed(await Fire(["r2"], L("E5"), Once(1, 1, 1)));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("g1")!.Status);
        Assert.Empty(Current.Encirclements);
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadThatFiredOnlyItsMgInThePfphDoesNotMove()
    {
        // A3.3 (table player, pass 12): g1 fires its LMG alone in the PFPh and nothing else; it has Prep Fired, so it does not move in the MPh.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Mg("gmg", "attacker-lmg", "g1", "german"), Squad("r1", L(Ne("E5")), "russian"));
        Committed(await Fire(["g1"], L(Ne("E5")), Once(6, 5), new()
        {
            ["weapons"] = new Dictionary<string, string[]> { ["g1"] = ["gmg"] },
            ["withoutInherent"] = G1,
        }));
        await Advance();
        Refused(await Move(G1, L(Se("E5"))), "play.move-halted");
        NoReplayErrors();
    }

    [Fact]
    public async Task SprayingFireKeepsOneGroupPerTarget()
    {
        // A7.55 (table player, pass 12): g1 in E5 fires at the hex north of it; g2, in the same Location, may not then spray that hex too.
        await SetupAt(1, "german", Squad("g1", L("E5"), "german"), Squad("g2", L("E5"), "german"), Mg("gmg", "attacker-lmg", "g2", "german"),
            Squad("r1", L(Ne("E5")), "russian"), Squad("r2", L(N("E5")), "russian"));
        Committed(await Fire(["g1"], L(N("E5")), Once(6, 5)));
        Refused(await Fire(["g2"], L(Ne("E5")), NoRoll(), new()
        {
            ["weapons"] = new Dictionary<string, string[]> { ["g2"] = ["gmg"] },
            ["sprayTarget"] = L(N("E5")),
        }), "play.fire-group");
        NoReplayErrors();
    }
}
