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
/// Pass 60 in live play, Armored combat III. Increment 2a: HE Equivalency (C8.31; rulings R60.4 to R60.6): a 6-Pounder's AP at a squad, a PSK and
/// a PF at a squad in a building, and an ATR at a Gun's crew. A board 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader.
/// Every read of the state replays the game, so each record written here is also checked by the projector.
/// </summary>
public sealed class BacklogPass60Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f560");
    private static readonly GameScope Scope = new(Tenant, "pass60");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private const string Firer = "bd01:B10:0";
    private const string Target = "bd01:B8:0";
    private const string Building = "Stone Building, 2 Level";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass60-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass60Board board = new();

    // The SSRs a test's game is set up with, such as the MOL's (A22.6).
    private string[] rules = [];

    public BacklogPass60Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the buildings a test draws on it (as pass 55's test board).</summary>
    private sealed class Pass60Board
    {
        public Dictionary<string, string> Terrain { get; } = new(StringComparer.Ordinal);

        /// <summary>The hexes with more than one level (pass 60), each of its terrain at every level.</summary>
        public Dictionary<string, int> Levels { get; } = new(StringComparer.Ordinal);

        private static TerrainType Type(string name) => new()
        {
            Code = (byte)(Math.Abs(name.GetHashCode(StringComparison.Ordinal)) % 250 + 1),
            Name = name,
            Category = name == "Woods" ? LosCategory.Woods : name.Contains("Building", StringComparison.Ordinal) ? LosCategory.Building : LosCategory.Open,
        };

        public BoardHandle Handle()
        {
            var geometry = BoardGeometry.StandardGeomorphic;
            var hexes = new List<HexFacts>();
            foreach (var text in Board01Fixture.Hexes())
            {
                var name = HexName.Parse(text);
                Assert.True(geometry.TryGetIndex(name, out var index));
                var type = Type(Terrain.GetValueOrDefault(text) ?? "Open Ground");
                var center = new LocationFacts(0, type, null);
                HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
                LocationFacts[] locations = [.. Enumerable.Range(0, Levels.GetValueOrDefault(text, 1)).Select(level => level == 0 ? center : new LocationFacts(level, type, null))];
                hexes.Add(new HexFacts(name, index, 0, false, center, locations, hexsides, null, CenterTerrainSource.CenterSample));
            }

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 60 test board", new HexFactSet(geometry, "pass60", hexes));
        }
    }

    // Clear LOS at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
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

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    /// <summary>A Gun manned by its crew, its barrel to the given facing.</summary>
    private static Dictionary<string, object>[] Gun(string id, string definition, string crew, string crewDefinition, string at, string side, string facing) =>
    [
        Unit(crew, "asl:crew", crewDefinition, at, side),
        new()
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
        },
    ];

    /// <summary>A LATW possessed by its holder.</summary>
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

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other at a date, gone on to the first side's PFPh.</summary>
    private async Task Setup(string firstSide, string otherSide, int year, int month, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass60Tests),
                ["catalog"] = "asl-scenario-a1@1.17.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["specialRules"] = rules,
                ["scenarioMonth"] = month,
                ["scenarioYear"] = year,
                ["sides"] = new object[]
                {
                    new { id = firstSide, nationality = firstSide, elr = 3, friendlyEdge = "left" },
                    new { id = otherSide, nationality = otherSide, elr = 3, friendlyEdge = "right" },
                },
            },
            placements,
        })));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    /// <summary>A shot at the target Location, naming an ammunition or none; the dice are every roll it asks for, in order.</summary>
    private Task<PlayResult> Shoot(string gun, string? ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = Target };
        if (ammunition is not null)
        {
            arguments["ammunition"] = ammunition;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private static int Equivalency(OrdnanceFired shot) => shot.Resolution.GetProperty("heEquivalency").GetInt32();

    /// <summary>The IFT column the shot's hit attacked on, its Critical Hit's halved.</summary>
    private static int HitColumn(OrdnanceFired shot) => shot.Resolution.TryGetProperty("hit", out var hit)
        ? hit.GetProperty("arithmetic").GetProperty("columnFp").GetInt32()
        : shot.Resolution.GetProperty("criticalHit").GetProperty("arithmetic").GetProperty("columnFp").GetInt32() / 2;

    /// <summary>A vehicle facing the given way; a closed-topped AFV's crew is CE only when its BU counter is removed (D5.2).</summary>
    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing, bool crewExposed = false) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
        ["conditions"] = crewExposed ? new Dictionary<string, bool> { ["asl:bu"] = false } : new Dictionary<string, bool>(),
    };

    private static Dictionary<string, object> Concealed(Dictionary<string, object> unit)
    {
        unit["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = true, ["asl:hidden"] = false };
        return unit;
    }

    private static string[] Ids(JsonElement resolution, string attack, string list, string id) =>
        resolution.TryGetProperty(attack, out var made) && made.TryGetProperty(list, out var items)
            ? [.. items.EnumerateArray().Select(item => item.GetProperty(id).GetString()!)] : [];

    private static (string Id, bool Hit)[] TargetHits(OrdnanceFired shot) =>
        [.. shot.Resolution.GetProperty("targetHits").EnumerateArray().Select(item => (item.GetProperty("unitId").GetString()!, item.GetProperty("hit").GetBoolean()))];

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ASixPoundersApAtASquadAttacksByItsHeEquivalency()
    {
        // British Ordnance Note 6: no HE before February 1943; C8.31: its AP fires at the squad on the 2 column instead (57mm, at least 37mm).
        await Setup("british", "german", 1942, 6, [.. Gun("uk-gun", "british-at-gun", "uk-crew", "british-crew", Firer, "british", "north-east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        var result = await Shoot("uk-gun", "ap", 4, 3, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal(("ap", 2, 2), (shot.Facts.GetProperty("ammunition").GetString(), Equivalency(shot), HitColumn(shot)));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains($"AP at {Target} (2 FP by HE Equivalency, C8.31)", StringComparison.Ordinal));
        Assert.False(shot.Resolution.TryGetProperty("ammunitionUse", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task APskAtASquadInABuildingAttacksEveryUnitAtTwelveFp()
    {
        // C13.48, C8.31: the PSK's IFT effect is 12, fired at Infantry in a building (its +3 is Case Q on the To Hit DR, 9 at two hexes).
        board.Terrain["B8"] = Building;
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Latw("de-psk", "attacker-psk", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"), Unit("r2", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        var result = await Shoot("de-psk", null, 2, 2, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal((12, 12), (Equivalency(shot), HitColumn(shot)));
        Assert.Equal(["r1", "r2"], shot.Resolution.GetProperty("hit").GetProperty("effects").EnumerateArray().Select(item => item.GetProperty("unitId").GetString()));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains($"HEAT at {Target} (12 FP by HE Equivalency, C8.31)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task APskAtASquadInTheOpenIsRefused()
    {
        // C8.31: HEAT goes only at a vehicle or Gun, or at Infantry with a wall, building, rubble, or pillbox TEM.
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Latw("de-psk", "attacker-psk", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"));
        var result = await Shoot("de-psk", null, 2, 2);
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains("asl.a1.ordnance.heat-target-outside", StringComparison.Ordinal) && reason.Contains("C8.31", StringComparison.Ordinal));
    }

    [Fact]
    public async Task APfAtASquadInABuildingTakesThePlusOneAndSixteenFp()
    {
        // C13.31: +1 to the PF Check at other than an AFV (a dr of 2 is a final 3: a shot); C13.33: 10 less two a hex; C8.31: 16 FP.
        board.Terrain["B8"] = Building;
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Unit("r1", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        Committed(await Shoot("g1:pf", null, 2, 1, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal((16, 16), (Equivalency(shot), HitColumn(shot)));
        Assert.Contains(shot.Resolution.GetProperty("panzerfaustCheck").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "not-afv");
        Assert.Equal(1, Planner().Replay(store.Read(Scope)!.Events).Current!.PanzerfaustShots["german"]);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAtrAtAGunAttacksOnlyItsCrewAtOneFp()
    {
        // C13.23, C11.52 (ruling R60.6): the Russian squad's 14mm ATR at the leIG 18 in B8: 1 FP on its crew alone, the German squad beside it
        // untouched. The black 8 at two hexes, Case L -1, the small Gun's +1 and its Emplacement's +2: a DR of 4 hits; an IFT DR of 12 does nothing.
        await Setup("russian", "german", 1943, 7, [Unit("r1", "asl:squad", "defender-squad", Firer, "russian"), Latw("ru-atr", "defender-atr", "r1", "russian"),
            .. Gun("de-gun", "attacker-inf-gun", "de-crew", "attacker-crew", Target, "german", "south-west"), Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        Committed(await Shoot("ru-atr", null, 2, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal((1, 1), (Equivalency(shot), HitColumn(shot)));
        Assert.Equal(["de-crew"], shot.Resolution.GetProperty("hit").GetProperty("effects").EnumerateArray().Select(item => item.GetProperty("unitId").GetString()));
        Assert.Equal("de-gun", shot.Facts.GetProperty("hit").GetProperty("gunTarget").GetProperty("gunId").GetString());
        NoReplayErrors();
    }

    // Increment 2b: the Specific Collateral Attack (D.8A; ruling R60.7), the Infantry Target Type at a Location holding a vehicle (C3.32; ruling
    // R60.8), and Known and concealed units together (C3.4; ruling R60.9).

    [Fact]
    public async Task AHitThatLeavesACeTankStandingAttacksItsCrew()
    {
        // D.8A: the PzKpfw IIIH's 50mm HE at the CE T-34 two hexes off, a hull hit (colored 4, white 2); a TK DR of 3 leaves it standing, and its
        // crew takes the HE FP on the same DR with +2 for CE. The record carries the fact that gates it (ruling R60.3).
        await Setup("german", "russian", 1942, 7, Vehicle("de-tank", "attacker-tank", Firer, "german", "north-east"),
            Vehicle("ru-tank", "defender-tank", Target, "russian", "south-west", crewExposed: true));
        var before = Revision;
        var result = await Do(GameActions.FireOrdnance, Once(4, 2, 1, 2, 2, 2, 2, 2), new
        {
            gunId = "de-tank",
            target = Target,
            targetVehicle = "ru-tank",
            ammunition = "he"
        });
        Committed(result);
        var shot = LastShot(before);
        Assert.True(shot.Facts.GetProperty("specificCollateral").GetBoolean());
        Assert.True(shot.Facts.GetProperty("vehicleTarget").GetProperty("crewExposed").GetBoolean());
        Assert.Equal(3, shot.Resolution.GetProperty("kill").GetProperty("originalDr").GetInt32());
        var collateral = shot.Resolution.GetProperty("specificCollateral");
        Assert.Equal(5, collateral.GetProperty("effect").GetProperty("finalDr").GetInt32());
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderRecordOfAHitOnACeTankReplaysAsItWas()
    {
        // Ruling R60.3: the 50mm's AP at the CE T-34's front, a TK DR of 7: its crew's attack (2 FP, Final DR 9) does nothing. The same record with the
        // pass's facts taken out, as one made before the pass, replays with no attack and no error.
        await Setup("german", "russian", 1942, 7, Vehicle("de-tank", "attacker-tank", Firer, "german", "north-east"),
            Vehicle("ru-tank", "defender-tank", Target, "russian", "south-west", crewExposed: true));
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(4, 2, 3, 4), new
        {
            gunId = "de-tank",
            target = Target,
            targetVehicle = "ru-tank",
            ammunition = "ap"
        }));
        var collateral = LastShot(before).Resolution.GetProperty("specificCollateral");
        Assert.Equal((2, "none", 9), (collateral.GetProperty("firepower").GetInt32(), collateral.GetProperty("result").GetString(),
            collateral.GetProperty("effect").GetProperty("finalDr").GetInt32()));

        var file = Directory.GetFiles(root, "*.game.json", SearchOption.AllDirectories).Single();
        var game = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!;
        static void Older(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is System.Text.Json.Nodes.JsonObject item)
            {
                item.Remove("specificCollateral");
                item.Remove("crewExposed");
                foreach (var (_, value) in item.ToList())
                {
                    Older(value);
                }
            }
            else if (node is System.Text.Json.Nodes.JsonArray items)
            {
                foreach (var value in items)
                {
                    Older(value);
                }
            }
        }

        Older(game);
        File.WriteAllText(file, game.ToJsonString());
        var older = LastShot(before);
        Assert.False(older.Facts.TryGetProperty("specificCollateral", out _) || older.Resolution.TryGetProperty("specificCollateral", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnHeHitAtASquadAndATruckAttacksBothOnOneDr()
    {
        // C3.32: the 45mm's HE at B8, its squad and its truck each hit on its own Final DR (an Original 6, Case L -1 at two hexes), then one IFT DR
        // of 7 attacks the squad and, on the Vehicle line, the truck (A7.308).
        await Setup("russian", "german", 1943, 7, [.. Gun("ru-gun", "defender-at-gun", "ru-crew", "defender-crew", Firer, "russian", "north-east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Vehicle("de-truck", "attacker-truck", Target, "german", "north-east")]);
        var before = Revision;
        var result = await Shoot("ru-gun", null, 3, 3, 3, 4, 3, 4, 3, 4);
        Committed(result);
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("hit target by target (C3.32, C3.4): de-s on an Original DR of", StringComparison.Ordinal)
            && reason.Contains("de-truck on an Original DR of", StringComparison.Ordinal));
        var shot = LastShot(before);
        Assert.Equal([("de-s", true), ("de-truck", true)], TargetHits(shot));
        Assert.Equal(["de-s"], Ids(shot.Resolution, "hit", "effects", "unitId"));
        Assert.Equal(["de-truck"], Ids(shot.Resolution, "hit", "vehicleEffects", "vehicleId"));
        NoReplayErrors();
    }

    [Fact]
    public async Task ACeAfvBesideASquadIsImmuneAndItsCrewIsAttacked()
    {
        // C3.32: the AFV is immune to the hit; D.8B: its CE crew takes the same IFT DR with +2 for CE, when the AFV's own TH# is reached.
        await Setup("russian", "german", 1943, 7, [.. Gun("ru-gun", "defender-at-gun", "ru-crew", "defender-crew", Firer, "russian", "north-east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Vehicle("de-tank", "attacker-tank", Target, "german", "north-east", crewExposed: true)]);
        var before = Revision;
        Committed(await Shoot("ru-gun", null, 3, 3, 3, 4, 3, 4, 3, 4));
        var shot = LastShot(before);
        Assert.Equal([("de-s", true), ("de-tank", true)], TargetHits(shot));
        var crew = shot.Resolution.GetProperty("hit").GetProperty("vehicleEffects").EnumerateArray().Single();
        Assert.Equal(("de-tank", "none"), (crew.GetProperty("vehicleId").GetString(), crew.GetProperty("result").GetString()));
        Assert.Contains(crew.GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "crew-exposed");
        NoReplayErrors();
    }

    [Fact]
    public async Task ADrThatHitsOnlyTheKnownSquadLeavesTheConcealedOneConcealed()
    {
        // C3.4, A12.13: an Original 8 is a Final 7 against the Known squad (Case L -1), a hit at the 45mm's 8, and 9 against the concealed one
        // (Case K +2), a miss: only the Known squad is attacked, and the concealed one keeps its "?" (A12.14).
        await Setup("russian", "german", 1943, 7, [.. Gun("ru-gun", "defender-at-gun", "ru-crew", "defender-crew", Firer, "russian", "north-east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Concealed(Unit("de-c", "asl:squad", "attacker-squad", Target, "german"))]);
        var before = Revision;
        Committed(await Shoot("ru-gun", null, 4, 4, 3, 4, 3, 4, 3, 4));
        var shot = LastShot(before);
        Assert.Equal([("de-c", false), ("de-s", true)], TargetHits(shot));
        Assert.Equal(["de-s"], Ids(shot.Resolution, "hit", "effects", "unitId"));
        Assert.Equal(ConditionState.True, GameState.Condition(Planner().Replay(store.Read(Scope)!.Events).Current!.Unit("de-c")!, Conditions.Concealed));
        NoReplayErrors();
    }

    // Increment 2c: Indirect Fire (C1.55, C3.332, B23.32, D9.3; rulings R60.10, R60.11).

    /// <summary>A light mortar possessed by its holder.</summary>
    private static Dictionary<string, object> LightMortar(string id, string definition, string holder, string side)
    {
        var mortar = Latw(id, definition, holder, side);
        mortar["kind"] = "asl:light-mortar";
        return mortar;
    }

    [Fact]
    public async Task AMortarsHitOnATankBesideASquadShocksItOnTheIft()
    {
        // C1.55, C3.332: the German 50mm at B8 two hexes off, a squad and its friendly T-34 there, both hit on a To Hit DR of 6. One IFT DR of 3 on the
        // 2 column is one above its K/# DR, its colored 1 below its white 2 a turret hit (C7.7 note E; ruling R60.23): it Shocks the T-34; the squad
        // takes the same DR with D9.3's +1 beside its AFV (C9.5 EX).
        await Setup("german", "russian", 1943, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), LightMortar("de-mtr", "attacker-light-mortar", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"), Vehicle("ru-tank", "defender-tank", Target, "russian", "south-west"));
        var before = Revision;
        Committed(await Shoot("de-mtr", null, 4, 2, 1, 2, 2, 2, 2, 2, 2, 2));
        var shot = LastShot(before);
        var tank = shot.Resolution.GetProperty("areaVehicles").EnumerateArray().Single();
        Assert.Equal(("ru-tank", "turret", "shock"), (tank.GetProperty("vehicleId").GetString(), tank.GetProperty("hitLocation").GetString(), tank.GetProperty("result").GetString()));
        Assert.Equal("ru-tank", shot.Facts.GetProperty("hit").GetProperty("afvCover").GetString());
        Assert.Contains(shot.Resolution.GetProperty("hit").GetProperty("arithmetic").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "afv-cover:ru-tank");
        Assert.Equal(ConditionState.True, GameState.Condition(Planner().Replay(store.Read(Scope)!.Events).Current!.Unit("ru-tank")!, Conditions.Shocked));
        NoReplayErrors();
    }

    [Fact]
    public async Task AMortarsHitOnATwoLevelBuildingAttacksBothLevels()
    {
        // B23.32: the German 50mm at B8, a stone building of two levels with a Russian squad on each; one IFT DR of 7 attacks the ground level with +3
        // and +1 for the level above it, and Level 1 with +3. The record keeps both Locations.
        board.Terrain["B8"] = Building;
        board.Levels["B8"] = 2;
        await Setup("german", "russian", 1943, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), LightMortar("de-mtr", "attacker-light-mortar", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"), Unit("r2", "asl:squad", "defender-squad", "bd01:B8:1", "russian"));
        var before = Revision;
        Committed(await Shoot("de-mtr", null, 2, 3, 3, 4));
        var shot = LastShot(before);
        var levels = shot.Facts.GetProperty("areaLocations").EnumerateArray().ToArray();
        Assert.Equal([Target, "bd01:B8:1"], levels.Select(item => item.GetProperty("locationId").GetString()));
        Assert.Equal(1, levels[0].GetProperty("levelsAbove").GetInt32());
        Assert.Equal(["r1"], Ids(shot.Resolution, "hit", "effects", "unitId"));
        Assert.Equal(11, shot.Resolution.GetProperty("hit").GetProperty("arithmetic").GetProperty("finalDr").GetInt32());
        var upper = shot.Resolution.GetProperty("levelHits").EnumerateArray().Single();
        Assert.Equal(("r2", 10), (upper.GetProperty("effects")[0].GetProperty("unitId").GetString(), upper.GetProperty("arithmetic").GetProperty("finalDr").GetInt32()));
        NoReplayErrors();
    }

    // Increment 2d: FT, MOL, and DC at an AFV (A22.34, A22.612, A23.5, C7.346; rulings R60.15, R60.16). The attacker in B9, beside the AFV in B8,
    // which faces north-east, so the attack crosses its rear.
    private const string Beside = "bd01:B9:0";

    /// <summary>A FT or DC possessed by its holder.</summary>
    private static Dictionary<string, object> Held(string id, string kind, string definition, string holder, string side)
    {
        var weapon = Latw(id, definition, holder, side);
        weapon["kind"] = kind;
        return weapon;
    }

    private FireResolved LastFire(long before) => Since(before).Select(item => item.Payload).OfType<FireResolved>().First();

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    [Fact]
    public async Task AFtAtACeTankMakesItsToKillDrAndAttacksItsCrew()
    {
        // A22.34, C7.344: 8, +1 CE, +1 rear: TK# 10; a DR of 11 leaves the PzKpfw IIIH standing, and its CE crew takes the FT's 24 FP on that DR with +2 for
        // CE alone (D.8A). The FT runs out of fuel on its Original 11 (A22.5).
        await Setup("russian", "german", 1943, 7, Unit("r1", "asl:squad", "defender-squad", Beside, "russian"), Held("ru-ft", "asl:ft", "defender-ft", "r1", "russian"),
            Vehicle("de-tank", "attacker-tank", Target, "german", "north-east", crewExposed: true));
        var before = Revision;
        var result = await Do(GameActions.Fire, Once(5, 6, 2, 2), new
        {
            firers = new[] { "r1" },
            target = Target,
            weapons = new Dictionary<string, string[]> { ["r1"] = ["ru-ft"] },
            vehicle = "de-tank",
        });
        Committed(result);
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("on the FT column, Basic TK# 8, CE +1, rear +1, no AF: Final TK# 10", StringComparison.Ordinal));
        var fire = LastFire(before);
        Assert.Equal(("de-tank", "rear"), (fire.Facts.GetProperty("targetVehicle").GetProperty("vehicleId").GetString(),
            fire.Facts.GetProperty("targetVehicle").GetProperty("hullFacing").GetString()));
        var kill = fire.Resolution.GetProperty("vehicleKill");
        Assert.Equal(("ft", 10, 11, "none"), (kill.GetProperty("ammunition").GetString(), kill.GetProperty("finalTk").GetInt32(), kill.GetProperty("originalDr").GetInt32(),
            kill.GetProperty("result").GetString()));
        var collateral = fire.Resolution.GetProperty("specificCollateral");
        Assert.Equal((24, 13), (collateral.GetProperty("firepower").GetInt32(), collateral.GetProperty("effect").GetProperty("finalDr").GetInt32()));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("ru-ft")!.Status);
        NoReplayErrors();
    }

    [Fact]
    public async Task AMolAtATankBesideASquadBurnsItAndAttacksTheSquadWithoutTheFourFp()
    {
        // A22.612: the MOL Check dr 1 passes; 6, +1 CE, +1 rear: TK# 8, and the DR of 7 burns the PzKpfw IIIH. The same DR attacks the German squad in
        // its Location at the Russian squad's 8 FP (PBF), not 12.
        rules = ["mol:russian"];
        await Setup("russian", "german", 1943, 7, Unit("r1", "asl:squad", "defender-squad", Beside, "russian"),
            Vehicle("de-tank", "attacker-tank", Target, "german", "north-east", crewExposed: true), Unit("g1", "asl:squad", "attacker-squad", Target, "german"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 3, 4, 2, 2, 2, 2), new
        {
            firers = new[] { "r1" },
            target = Target,
            mol = "r1",
            vehicle = "de-tank",
        }));
        var fire = LastFire(before);
        Assert.Equal((8, "burn"), (fire.Resolution.GetProperty("vehicleKill").GetProperty("finalTk").GetInt32(), fire.Resolution.GetProperty("vehicleKill").GetProperty("result").GetString()));
        Assert.Equal(8, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetInt32());
        Assert.Equal("g1", fire.Resolution.GetProperty("effects")[0].GetProperty("unitId").GetString());
        Assert.False(fire.Resolution.TryGetProperty("specificCollateral", out _));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-tank")!.Status);
        NoReplayErrors();
    }

    [Fact]
    public async Task APlacedDcAtATankAfterItsPaatcBurnsIt()
    {
        // A23.3, A11.6: the German squad passes a PAATC (DR 4) to Place its DC from B9 in B8, where the T-34 is. A23.5, C7.346: detonated at the T-34 in
        // the AFPh, a Position DR of 5 through the hull rear (-1) is 4: optimal, against the Aerial AF 3 (worst AF 6): TK# 16 + 1 - 3 = 14; the second DR
        // of 7 burns it.
        await Setup("german", "russian", 1943, 7, Unit("g1", "asl:squad", "attacker-squad", Beside, "german"), Held("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Vehicle("ru-tank", "defender-tank", Target, "russian", "north-east"));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        var placing = Revision;
        Committed(await Do(GameActions.Move, Once(2, 2), new
        {
            unitIds = new[] { "g1" },
            to = Beside,
            placeDc = "dc",
            placeDcAt = Target,
        }));
        Assert.Contains(Since(placing), item => item.Payload is PaatcTaken { Passed: true });
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        for (var step = 0; step < 2; step++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }

        var before = Revision;
        var result = await Do(GameActions.DetonateDc, Once(3, 4, 3, 2), new
        {
            equipmentId = "dc",
            vehicle = "ru-tank",
        });
        Committed(result);
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("a Position DR at ru-tank first, hull rear -1", StringComparison.Ordinal));
        var fire = LastFire(before);
        var position = fire.Resolution.GetProperty("position");
        Assert.Equal((4, "optimal", "hull"), (position.GetProperty("finalDr").GetInt32(), position.GetProperty("result").GetString(), position.GetProperty("hitLocation").GetString()));
        var kill = fire.Resolution.GetProperty("vehicleKill");
        Assert.Equal(("dc", 3, 14, "burn"), (kill.GetProperty("ammunition").GetString(), kill.GetProperty("armorFactor").GetInt32(), kill.GetProperty("finalTk").GetInt32(),
            kill.GetProperty("result").GetString()));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-tank")!.Status);
        NoReplayErrors();
    }

    // Increment 2e: Random SW and Gun Destruction (A9.74; ruling R60.17), an unattended Gun as a target (C3.41, C11.4; ruling R60.18), and the
    // Emplacement a non-crew manning ends (C11.3). Two Russian squads in B9 fire 16 FP (PBF) at a German squad in B8 holding a LMG.

    private async Task FireAtLmgSquad(params int[] dice)
    {
        await Setup("russian", "german", 1943, 7, Unit("r1", "asl:squad", "defender-squad", Beside, "russian"), Unit("r2", "asl:squad", "defender-squad", Beside, "russian"),
            Unit("g1", "asl:squad", "attacker-squad", Target, "german"), Held("de-lmg", "asl:mg", "attacker-lmg", "g1", "german"));
        Committed(await Do(GameActions.Fire, Once(dice), new
        {
            firers = new[] { "r1", "r2" },
            target = Target,
        }));
    }

    [Fact]
    public async Task AKiaOnASquadHoldingALmgDestroysTheLmgOnItsDr()
    {
        // A9.74: IFT DR 3 on the 16 FP column is a 1KIA; the LMG's dr of 3 on the same column is a 1KIA too: it is eliminated before its squad, so it is
        // not dropped. The record carries the rule and the weapons the target held (ruling R60.3).
        var before = Revision;
        await FireAtLmgSquad(1, 2, 3, 3);
        var fire = LastFire(before);
        Assert.True(fire.Facts.GetProperty("randomDestruction").GetBoolean());
        Assert.Equal("de-lmg", fire.Facts.GetProperty("targets")[0].GetProperty("heldWeapons")[0].GetProperty("equipmentId").GetString());
        var fate = fire.Resolution.GetProperty("weaponDestruction")[0];
        Assert.Equal((3, "1KIA", "destroyed"), (fate.GetProperty("dr").GetInt32(), fate.GetProperty("result").GetString(), fate.GetProperty("fate").GetString()));
        var eliminated = Since(before).Select(item => item.Payload).OfType<InstanceEliminated>().Select(item => item.Id).ToArray();
        Assert.Equal(["de-lmg", "g1"], eliminated);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("de-lmg")!.Status);
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "fire-weapon-destruction");
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderRecordOfAKiaOnASquadHoldingALmgReplaysAsItWas()
    {
        // Ruling R60.3: the LMG's dr of 5 is a 3MC: unharmed, dropped in B8. The same record with the pass's facts taken out, as one made before the
        // pass, replays with no dr read and the LMG dropped as before.
        var before = Revision;
        await FireAtLmgSquad(1, 2, 3, 5);
        Assert.Equal("none", LastFire(before).Resolution.GetProperty("weaponDestruction")[0].GetProperty("fate").GetString());

        var file = Directory.GetFiles(root, "*.game.json", SearchOption.AllDirectories).Single();
        var game = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!;
        static void Older(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is System.Text.Json.Nodes.JsonObject item)
            {
                foreach (var name in item.Select(pair => pair.Key).Where(name => name is "randomDestruction" or "heldWeapons" || name.StartsWith("weaponDestruction", StringComparison.Ordinal)).ToArray())
                {
                    item.Remove(name);
                }

                foreach (var (_, value) in item.ToList())
                {
                    Older(value);
                }
            }
            else if (node is System.Text.Json.Nodes.JsonArray items)
            {
                foreach (var value in items)
                {
                    Older(value);
                }
            }
        }

        Older(game);
        File.WriteAllText(file, game.ToJsonString());
        var older = LastFire(before);
        Assert.False(older.Facts.TryGetProperty("randomDestruction", out _) || older.Resolution.TryGetProperty("weaponDestruction", out _));
        NoReplayErrors();
        Assert.Equal((InstanceStatus.Active, (Holding?)null), (Current.Find("de-lmg")!.Status, ((EquipmentInstance)Current.Find("de-lmg")!).Holding));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("g1")!.Status);
    }

    [Fact]
    public async Task AShotAtAnUnattendedGunDestroysItOnAKia()
    {
        // C3.41, C11.4 (ruling R60.18): the leIG 18 fires HE at the Russian 45mm alone in B8, no unit manning it: Case P +1 for its size, no gunshield
        // or Emplacement; To Hit DR 7 hits, and the IFT DR 2 on the 12 FP column is a 1KIA: destroyed.
        await Setup("german", "russian", 1943, 7, [.. Gun("de-gun", "attacker-inf-gun", "de-crew", "attacker-crew", Firer, "german", "north-east"),
            new Dictionary<string, object>
            {
                ["id"] = "ru-gun",
                ["kind"] = "asl:gun",
                ["definition"] = "defender-at-gun",
                ["side"] = "russian",
                ["position"] = new Dictionary<string, string> { ["at"] = Target, ["facing"] = "south-west" },
                ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
            }]);
        var before = Revision;
        Committed(await Shoot("de-gun", null, 3, 4, 1, 1));
        var shot = LastShot(before);
        var gunTarget = shot.Facts.GetProperty("hit").GetProperty("gunTarget");
        Assert.Equal(("ru-gun", false), (gunTarget.GetProperty("gunId").GetString(), gunTarget.TryGetProperty("crewUnitId", out var crew) && crew.ValueKind == JsonValueKind.String));
        Assert.Contains(shot.Resolution.GetProperty("toHit").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString()!.StartsWith("case-p:", StringComparison.Ordinal));
        Assert.Equal(("1KIA", "destroyed"), (shot.Resolution.GetProperty("hit").GetProperty("arithmetic").GetProperty("result").GetString(),
            shot.Resolution.GetProperty("gunTargetFate").GetString()));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("ru-gun")!.Status);
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunMannedByASquadLosesItsEmplacementForTheGame()
    {
        // C11.3 (ruling R60.18): a Gun that starts manned by a non-crew unit loses Emplacement; a crew that mans it later does not regain it. A shot
        // recorded before the pass reads the Emplacement as it was read then.
        await Setup("russian", "german", 1943, 7, Unit("r1", "asl:squad", "defender-squad", Target, "russian"),
            Gun("ru-gun", "defender-at-gun", "r1", "defender-squad", Target, "russian", "south-west")[1], Unit("ru-crew", "asl:crew", "defender-crew", Target, "russian"));
        Assert.Contains("ru-gun", Current.NonCrewMannedGuns);
        var gun = (EquipmentInstance)Current.Find("ru-gun")!;
        var crewed = Current with
        {
            Equipment = [.. Current.Equipment.Select(item => item.Id == gun.Id ? gun with { Holding = new Holding("ru-crew", HoldingRole.Manned) } : item)],
        };
        var manned = (EquipmentInstance)crewed.Find("ru-gun")!;
        Assert.Equal((false, true), (LiveOrdnance.Emplaced(crewed, manned), LiveOrdnance.Emplaced(crewed, manned, nonCrewManning: false)));
    }
}
