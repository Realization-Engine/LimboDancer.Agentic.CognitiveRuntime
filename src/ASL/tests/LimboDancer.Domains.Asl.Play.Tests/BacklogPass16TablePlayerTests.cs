using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 16 table player's situations in live play (rulings R16.1 to R16.14): night and weather SSRs, what a unit sees at night, the Low
/// Visibility DRM, Starshells,
/// the Wind Change DR, rout, DM, movement, and concealment at night, and the weather's MF. A board 01 grid of Open Ground with woods at E4, fixed dice,
/// and a clear LOS everywhere.
/// </summary>
public sealed class BacklogPass16TablePlayerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-0000000fa716");
    private static readonly GameScope Scope = new(Tenant, "tp16");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] Gi = ["gi"];
    private static readonly string[] G1Gi = ["g1", "gi"];
    private static readonly string[] Gh = ["gh"];
    private static readonly string[] G1G2 = ["g1", "g2"];
    private static readonly string[] G1G2G3 = ["g1", "g2", "g3"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] H1 = ["h1"];
    private static readonly string[] R1R2 = ["r1", "r2"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] R2R3R4 = ["r2", "r3", "r4"];
    private static readonly string[] KeepThree = ["m2", "m3", "m4"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-tp16-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass16TablePlayerTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static readonly TerrainType Woods = new() { Code = 2, Name = "Woods", Category = LosCategory.Woods, Height = 1 };
    private static readonly TerrainType Road = new() { Code = 3, Name = "Dirt Road", Category = LosCategory.Road };

    /// <summary>Board 01's grid, Open Ground at level 0 but woods at E4.</summary>
    private BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, text == "E4" ? Woods : type, null);
            var sideType = roads.Contains(text) ? Road : type;
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, sideType, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 16 test board", new HexFactSet(geometry, "pass16", hexes));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: los);

    /// <summary>These values first, then the fallback face for every other die.</summary>
    private static DiceRoller Then(int fallback, params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => (queue.Count > 0 ? queue.Dequeue() : fallback) - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        var kind = definition.Contains("half-squad", StringComparison.Ordinal) ? "asl:half-squad"
            : definition.Contains("leader", StringComparison.Ordinal) || definition.Contains("commissar", StringComparison.Ordinal) ? "asl:leader"
            : definition.Contains("hero", StringComparison.Ordinal) ? "asl:hero" : "asl:squad";
        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Weapon(string id, string kind, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static Dictionary<string, object> Sniper(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:sniper",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task SetupAt(int advances, string firstSide, string[] specialRules, object[] sides, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 15",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["specialRules"] = specialRules,
                ["sides"] = sides,
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        })));
        await Advance(advances);
    }

    private static object[] Sides(int? germanSan = null, int? russianSan = null, string enemy = "russian") =>
    [
        new { id = "german", nationality = "german", elr = 3, san = germanSan },
        new { id = enemy, nationality = enemy, elr = 2, san = russianSan },
    ];

    private Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements) => SetupAt(advances, firstSide, [], Sides(), placements);

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

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static FireResolution Resolution(FireResolved fire) => fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)!;

    private FireResolved[] Fires(long since) => [.. Since(since).Select(item => item.Payload).OfType<FireResolved>()];

    private IFireLosReader los = new ClearLos();

    /// <summary>A LOS with a brush Hindrance one hex out, for shots at range 2 or more.</summary>
    private sealed class BrushLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            return range < 2 ? new(LosStatus.Clear, false, range, 0, null, string.Empty)
                : new(LosStatus.Clear, false, range, 1, null, string.Empty)
                {
                    Hindrances = [new LosHindrance(1, 1) { Terrains = ["Brush"] }]
                };
        }
    }

    private static Dictionary<string, object> Dummy(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    /// <summary>One action; <paramref name="what"/> names the step for the reader.</summary>
    private Task<PlayResult> Try(string what, Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments) =>
        string.IsNullOrEmpty(what) ? throw new ArgumentException("Name the step.", nameof(what)) : Do(action, roller, arguments);

    private async Task<PlayResult> TryAdvance(string what) => await Try(what, GameActions.AdvancePhase, NoRoll(), new
    {
    });

    private static Dictionary<string, string[]> Uses(string unit, string weapon) => new() { [unit] = [weapon] };


    private PlayResult? refusedSetup;

    private async Task<PlayResult> SetupRaw(string[] rules, int? month = 7, int? year = 1942)
    {
        var start = new Dictionary<string, object?>
        {
            ["label"] = "Pass 16",
            ["catalog"] = "asl-scenario-a1@1.13.0",
            ["boards"] = Bd01,
            ["firstSide"] = "german",
            ["specialRules"] = rules,
            ["sides"] = Sides(),
        };
        if (month is not null)
        {
            start["scenarioMonth"] = month;
        }

        if (year is not null)
        {
            start["scenarioYear"] = year;
        }

        refusedSetup = await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-raw",
            expectedRevision = 0,
            start,
            placements = new[] { Unit("g1", "attacker-squad", "E3", "german") },
        }));
        return refusedSetup;
    }

    private FireResolution LastFire(long since) => Resolution(Fires(since).Last());

    private readonly HashSet<string> roads = new(StringComparer.Ordinal);

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, string side, string facing = "east", params string[] states)
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
            ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = facing },
            ["conditions"] = conditions,
        };
    }

    private Task<PlayResult> FireAt(string[] firers, string hex, DiceRoller? roller = null) => Do(GameActions.Fire, roller ?? Then(3), new
    {
        firers,
        target = L(hex)
    });

    private async Task<FireResolution> FireOk(string[] firers, string hex, DiceRoller? roller = null)
    {
        var before = Revision;
        Committed(await FireAt(firers, hex, roller));
        return LastFire(before);
    }

    private static int? Dist(object a, string b) => Board01Fixture.Handle().Distance(HexName.Parse(a.ToString()!), HexName.Parse(b));

    private static decimal Lv(FireResolution resolution) => resolution.Arithmetic!.Drm.Where(item => item.Name == "lv-hindrance").Sum(item => item.Value);

    private Task<PlayResult> Starshell(string unit, string method, DiceRoller roller, string? at = null) => Do(GameActions.FireStarshell, roller,
        at is null ? new Dictionary<string, object> { ["unitId"] = unit, ["method"] = method } : new Dictionary<string, object> { ["unitId"] = unit, ["method"] = method, ["at"] = L(at) });

    /// <summary>On to the next RPh, making the Wind Change DR with the dice given.</summary>
    private async Task<WindChanged?> ToNextRph(params int[] dice)
    {
        while (Current.Phase != "ccph")
        {
            await Advance();
        }

        var before = Revision;
        Committed(await Do(GameActions.AdvancePhase, Then(1, dice), new
        {
        }));
        Assert.Equal("rph", Current.Phase);
        return Since(before).Select(item => item.Payload).OfType<WindChanged>().SingleOrDefault();
    }

    private IEnumerable<IGameObject> Starshells => Current.Entities.Where(entity => entity.Status == InstanceStatus.Active && entity.Kind == "asl:starshell");

    private static bool NoNvrRefusal(PlayResult result) => !result.Reasons.Any(reason => reason.StartsWith("play.night-", StringComparison.Ordinal));

    // ---------------------------------------------------------------- Fire at night

    // 1. E1.7: the target in woods (a full level above the ground-level firer) takes no night LV DRM.
    [Fact]
    public async Task NoNightLvAtATargetInWoodsAFullLevelUp()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        Assert.Equal(0m, Lv(await FireOk(G1, "E4")));
    }

    // 2. E1.7: a firer in woods at an Open Ground target still takes the +1.
    [Fact]
    public async Task FirerInWoodsAtOpenGroundTakesTheNightLv()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E4", "german"), Unit("r1", "defender-squad", "E6", "russian"));
        Assert.Equal(1m, Lv(await FireOk(G1, "E6")));
    }

    // 3. E1.8, E1.81: every kind of fire counter is a Gunflash beyond NVR; fire at it is Area Fire.
    [Theory]
    [InlineData("asl:prep-fire")]
    [InlineData("asl:final-fire")]
    [InlineData("asl:bounding-fire")]
    [InlineData("asl:intensive-fire")]
    public async Task EachFireCounterIsAGunflashBeyondNvr(string counter)
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E8", "russian", counter));
        var resolution = await FireOk(G1, "E8");
        Assert.Contains(resolution.Arithmetic!.Firers.SelectMany(item => item.Multipliers), item => item.Name == "area-fire-gunflash");
    }

    // 4. E1.82: a Melee is a Gunflash beyond NVR.
    [Fact]
    public async Task AMeleeIsAGunflashBeyondNvr()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E8", "russian", "asl:melee"),
            Unit("g2", "attacker-squad", "E8", "german", "asl:melee"));
        var result = await FireAt(G1, "E8");
        Assert.True(NoNvrRefusal(result), string.Join("; ", result.Reasons));
    }

    // 5. E1.81: a concealed unit also marked by a Gunflash is halved once only.
    [Fact]
    public async Task AConcealedGunflashIsHalvedOnce()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "E8", "russian", "asl:first-fire", "asl:concealed"));
        var resolution = await FireOk(G1, "E8");
        var halvings = resolution.Arithmetic!.Firers.SelectMany(item => item.Multipliers).Where(item => item.Value == 0.5m).ToArray();
        Assert.True(halvings.Length == 1, string.Join(", ", halvings.Select(item => item.Name)));
    }

    // 6. E1.101, E1.33: an unconcealed unit beyond NVR is not Known; a Gunflash next door does not make its hex visible.
    [Fact]
    public async Task AUnitBeyondNvrWithoutGunflashIsRefused()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E8", "russian"),
            Unit("r2", "defender-squad", "E9", "russian", "asl:first-fire"));
        Refused(await FireAt(G1, "E8"), "play.night-nvr");
    }

    // 7. E1.75: a fire group in one Location is fine at night.
    [Fact]
    public async Task OneLocationFireGroupAtNight()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "E5", "russian"));
        Assert.Equal(1m, Lv(await FireOk(G1G2, "E5")));
    }

    // 8. E1.14: a moving truck is seen at 1.5 x NVR (FRU): NVR 2 -> 3; at 4 it is not.
    [Fact]
    public async Task AMovingTruckIsSeenAtOneAndAHalfNvr()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Vehicle("rt", "defender-truck", "E6", "russian", "east", "asl:motion"), Vehicle("rt2", "defender-truck", "E7", "russian", "east", "asl:motion"));
        var near = await FireAt(G1, "E6");
        Assert.True(NoNvrRefusal(near), string.Join("; ", near.Reasons));
        Refused(await FireAt(G2, "E7"), "play.night-nvr");
    }

    // 9. E1.14: a moving tank is seen at 2 x NVR: NVR 2 -> 4.
    [Fact]
    public async Task AMovingTankIsSeenAtTwiceNvr()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Vehicle("rk", "defender-tank", "E7", "russian", "east", "asl:motion"), Vehicle("rk2", "defender-tank", "E8", "russian", "east", "asl:motion"));
        var near = await FireAt(G1, "E7");
        Assert.True(NoNvrRefusal(near), string.Join("; ", near.Reasons));
        Refused(await FireAt(G2, "E8"), "play.night-nvr");
    }

    // 10. E1.14: with NVR 0 the viewer treats its NVR as 1 for a wheeled vehicle: a moving truck two hexes away stays unseen.
    [Fact]
    public async Task NvrZeroSeesAMovingTruckOnlyOneHexAway()
    {
        await SetupAt(1, "german", ["night:0"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Vehicle("rt", "defender-truck", "E5", "russian", "east", "asl:motion"));
        Refused(await FireAt(G1, "E5"), "play.night-nvr");
    }

    // 11. E1.9, E1.101: an Illuminated firer sees only Illuminated Locations and Gunflashes.
    [Fact]
    public async Task AnIlluminatedFirerSeesOnlyLightAndGunflashes()
    {
        await SetupAt(1, "german", ["night:6"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("r1", "defender-squad", "E9", "russian"), Unit("r2", "defender-squad", "F9", "russian", "asl:first-fire"));
        Committed(await Starshell("gl", "own-hex", Then(1, 1, 1)));
        var at = ((MapPosition)Assert.Single(Starshells).Position).Location;
        Assert.True(Dist(at.Hex, "E9") > 3, at.Hex.ToString());
        Refused(await FireAt(G1, "E9"), "play.night-illuminated");
        var result = await FireAt(G2, "F9");
        Assert.True(NoNvrRefusal(result), string.Join("; ", result.Reasons));
    }

    // 12. E1.7: a DC attack takes no night LV DRM.
    [Fact]
    public async Task ADcAttackTakesNoNightLv()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc1", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E2", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.ThrowDc, Then(6, 2, 2), new
        {
            unitId = "g1",
            equipmentId = "dc1",
            target = L("E2")
        }));
        Assert.NotEmpty(Fires(before));
        Assert.All(Fires(before), fire => Assert.Equal(0m, Lv(Resolution(fire))));
    }

    // ---------------------------------------------------------------- Weather Hindrances

    // 13. E3.32: Mist: nothing at six hexes, +1 at seven, +2 at thirteen.
    [Fact]
    public async Task MistByRange()
    {
        await SetupAt(1, "german", ["weather:mist"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("g3", "attacker-leader-8-0", "E3", "german"), Weapon("g3m", "asl:mg", "attacker-lmg", "g3", "german"), Unit("r1", "defender-squad", "K3", "russian"),
            Unit("r2", "defender-squad", "L3", "russian"), Unit("r3", "defender-squad", "R3", "russian"));
        Assert.Equal(6, Dist("E3", "K3"));
        Assert.Equal(0m, Lv(await FireOk(G1, "K3")));
        Assert.Equal(1m, Lv(await FireOk(G2, "L3")));
        Assert.Equal(13, Dist("E3", "R3"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G3,
            weapons = new Dictionary<string, string[]> { ["g3"] = ["g3m"] },
            target = L("R3")
        }));
        Assert.Equal(2m, Lv(LastFire(before)));
    }

    // 14. E3.51: heavy rain: +1 at six hexes or less, +2 at seven to twelve.
    [Fact]
    public async Task HeavyRainByRange()
    {
        await SetupAt(1, "german", ["weather:heavy-rain"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "E5", "russian"), Unit("r2", "defender-squad", "L3", "russian"));
        Assert.Equal(1m, Lv(await FireOk(G1, "E5")));
        Assert.Equal(2m, Lv(await FireOk(G2, "L3")));
    }

    // 15. E3.52, E3.711: rain and Falling Snow cause Mist; Overcast and Gusty alone do not.
    [Theory]
    [InlineData("weather:falling-snow", 1)]
    [InlineData("weather:rain", 1)]
    [InlineData("weather:overcast", 0)]
    [InlineData("weather:gusty", 0)]
    public async Task PrecipitationCausesMist(string rule, int drm)
    {
        await SetupAt(1, "german", [rule], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "L3", "russian"));
        Assert.Equal(drm, Lv(await FireOk(G1, "L3")));
    }

    // 16. E1.7, E3.32: night:6 and Mist at 6 hexes: +1 night only.
    [Fact]
    public async Task NightAndMistAtSixHexes()
    {
        await SetupAt(1, "german", ["night:6", "weather:mist"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "K3", "russian"));
        Assert.Equal(1m, Lv(await FireOk(G1, "K3")));
    }

    // ---------------------------------------------------------------- Starshells

    // 17. E1.921, E1.92: a leader's failed Usage dr is the hex's attempt: no Starshell, and no second attempt from that hex this phase.
    [Fact]
    public async Task AFailedUsageDrUsesTheHexAttempt()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("gl", "attacker-leader-8-0", "E3", "german"),
            Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("gl", "own-hex", Then(5)));
        Assert.Empty(Starshells);
        Assert.False(Current.StarshellUsed);
        Refused(await Starshell("g1", "own-hex", Then(1)), "play.starshell-once");
    }

    // 18. E1.921: an MMC needs a Usage dr of 2 or less.
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task AnMmcUsageDr(int dr, bool fired)
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("g1", "own-hex", Then(1, dr)));
        Assert.Equal(fired, Starshells.Any());
    }

    // 19. E1.91, E1.101: the first Starshell needs an enemy unit in the firer's LOS; a unit beyond NVR is out of its LOS.
    [Fact]
    public async Task TheFirstStarshellNeedsAnEnemyWithinNvr()
    {
        await SetupAt(1, "german", ["night:2"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("r1", "defender-squad", "E9", "russian"));
        Refused(await Starshell("gl", "own-hex", Then(1)), "play.starshell-first");
    }

    // 20. E1.922 method 2, E1.33: an unconcealed unit beyond NVR is not Known, so it is no aiming point; a Gunflash beyond NVR is.
    [Fact]
    public async Task AtTargetNeedsAKnownUnitOrAGunflash()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("g1", "attacker-squad", "F3", "german"),
            Unit("r1", "defender-squad", "E5", "russian"), Unit("r2", "defender-squad", "E8", "russian"), Unit("r3", "defender-squad", "G8", "russian", "asl:first-fire"));
        // Referee and table player, pass 16: the unconcealed r2 beyond NVR is not Known, so it is refused; the Gunflash at G8 is a target.
        Refused(await Starshell("gl", "at-target", Then(1), "E8"), "play.starshell-placement");
        Committed(await Starshell("g1", "at-target", Then(1, 1, 1, 1), "G8"));
    }

    // 21. E1.922, E1.923: a Starshell that drifts off the map edge stops at its last hex on the map (ruling R16.8); no crash.
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task AStarshellAtTheMapEdge(int direction)
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("gl", "attacker-leader-8-0", "A2", "german"), Unit("r1", "defender-squad", "A4", "russian"));
        Committed(await Starshell("gl", "three-hexes", Then(1, 1, direction, 6), "A5"));
        var at = ((MapPosition)Assert.Single(Starshells).Position).Location;
        Assert.Contains(at.Hex.ToString(), Board01Fixture.Hexes());
    }

    // 22. E1.921: the timing limit starts only AFTER the Player Turn of the first Starshell: in that PFPh an MMC may fire one after other fire.
    [Fact]
    public async Task AnMmcMayFireAStarshellLaterInThePlayerTurnOfTheFirst()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("g2", "attacker-squad", "H3", "german"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("gl", "own-hex", Then(1, 1, 1)));
        Committed(await FireAt(G1, "E5"));
        Committed(await Starshell("g2", "own-hex", Then(1, 1, 1)));
    }

    // 23. E1.921: in a later Player Turn an MMC fires one only before any fire of the PFPh; a leader may fire one any time.
    [Fact]
    public async Task AfterTheFirstPlayerTurnAnMmcFiresOnlyAtTheStart()
    {
        await SetupAt(1, "russian", ["night:3"], Sides(), Unit("rl", "defender-leader", "E5", "russian"), Unit("r1", "defender-squad", "E5", "russian"),
            Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "H3", "german"), Unit("gl", "attacker-leader-8-0", "J3", "german"));
        Committed(await Starshell("rl", "own-hex", Then(1, 1, 1)));
        Assert.True(Current.StarshellUsed);
        await ToNextRph(1, 1);
        await Advance();
        Assert.Equal(("pfph", "german"), (Current.Phase, Current.PhasingSide));
        Committed(await FireAt(G1, "E5"));
        Refused(await Starshell("g2", "own-hex", Then(1, 1, 1)), "play.starshell-timing");
        Committed(await Starshell("gl", "own-hex", Then(1, 1, 1)));
    }

    // 24. E1.92: the phasing side fires none in its MPh; the other side may, as Defensive First Fire; the other side not in the PFPh.
    [Fact]
    public async Task StarshellPhases()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("rl", "defender-leader", "E5", "russian"),
            Unit("r1", "defender-squad", "E5", "russian"), Unit("g1", "attacker-squad", "E3", "german"));
        Refused(await Starshell("rl", "own-hex", Then(1, 1, 1)), "play.starshell-phase");
        await Advance();
        Assert.Equal("mph", Current.Phase);
        Refused(await Starshell("gl", "own-hex", Then(1, 1, 1)), "play.starshell-phase");
        Committed(await Starshell("rl", "own-hex", Then(1, 1, 1)));
    }

    // 25. E1.921: firing a Starshell is not firing: no fire counter, no loss of "?", and the MMC may Prep Fire after it.
    [Fact]
    public async Task AStarshellIsNotFiring()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german", "asl:concealed"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("g1", "own-hex", Then(1, 1, 1)));
        Assert.False(Is(Current.Unit("g1")!, Conditions.PrepFire));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
        Committed(await FireAt(G1, "E5"));
    }

    // 26. E1.921: a hidden firer is placed beneath a "?".
    [Fact]
    public async Task AHiddenStarshellFirerIsConcealed()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german", "asl:hidden"), Unit("r1", "defender-squad", "E5", "russian"));
        var result = await Starshell("g1", "own-hex", Then(1, 1, 1));
        // The log holds the attempt whatever the outcome says.
        Assert.True(Starshells.Any() && Is(Current.Unit("g1")!, Conditions.Concealed) && !Is(Current.Unit("g1")!, Conditions.Hidden), "not in the log");
        Committed(result);
    }

    // ---------------------------------------------------------------- Wind Change DR

    // 27. E1.12: a white 4 raises the NVR before the first Starshell.
    [Fact]
    public async Task WhiteFourBeforeTheFirstStarshellRaises()
    {
        await SetupAt(0, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        await ToNextRph(6, 4);
        Assert.Equal(4, Current.Nvr);
    }

    // 28. E1.12, E1.923: after the first Starshell a white 4 is No Change; the Starshell is gone after the CCPh.
    [Fact]
    public async Task WhiteFourAfterTheFirstStarshellHolds()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("gl", "own-hex", Then(1, 1, 1)));
        await ToNextRph(6, 4);
        Assert.Equal(3, Current.Nvr);
        Assert.Empty(Starshells);
    }

    // 29. E1.12: Scattered clouds and a Full Moon: the change is a further dr / 2 (FRU): dr 5 -> 3.
    [Fact]
    public async Task ScatteredCloudsAndAFullMoon()
    {
        await SetupAt(0, "german", ["night:2", "night-clouds:scattered", "night-moon:full"], Sides(), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "H8", "russian"));
        var wind = await ToNextRph(6, 5, 5);
        Assert.Equal(5, Current.Nvr);
        Assert.NotNull(wind!.NvrRoll);
    }

    // 30. E1.12, E1.15: the NVR stays within 0..6, and 2..9 with snow.
    [Theory]
    [InlineData("night:0", null, 1, 0)]
    [InlineData("night:6", null, 6, 6)]
    [InlineData("night:2", "weather:ground-snow", 1, 2)]
    [InlineData("night:9", "weather:deep-snow", 6, 9)]
    [InlineData("night:6", "weather:ground-snow", 6, 7)]
    public async Task NvrLimits(string night, string? snow, int white, int expected)
    {
        string[] rules = snow is null ? [night] : [night, snow];
        await SetupAt(0, "german", rules, Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        await ToNextRph(6, white);
        Assert.Equal(expected, Current.Nvr);
    }

    // 31. E3.51: in rain a Wind Change DR of 10 makes it heavy; a later 3 stops it (no Mist at 7 hexes).
    [Fact]
    public async Task RainIntensifiesThenStops()
    {
        await SetupAt(0, "german", ["weather:rain"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r2", "defender-squad", "L3", "russian"));
        await ToNextRph(5, 5);
        Assert.Equal("heavy-rain", Current.Precipitation);
        await ToNextRph(1, 2);
        Assert.Null(Current.Precipitation);
        await Advance();
        Assert.Equal(("pfph", "german"), (Current.Phase, Current.PhasingSide));
        Assert.Equal(0m, Lv(await FireOk(G1, "L3")));
    }

    // 32. E3.4: Gusty weather: a Gust on a DR of 10 or more.
    [Fact]
    public async Task AGust()
    {
        await SetupAt(0, "german", ["weather:gusty"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        var wind = await ToNextRph(4, 6);
        Assert.True(wind!.Gust);
    }

    // 33. E3.71: Falling Snow: 10 intensifies (heavy snow: +1 at 2 hexes).
    [Fact]
    public async Task HeavySnowfall()
    {
        await SetupAt(0, "german", ["weather:falling-snow"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian"));
        await ToNextRph(5, 6);
        Assert.Equal("heavy-snow", Current.Precipitation);
        await ToNextRph(2, 2);
        Assert.Equal("heavy-snow", Current.Precipitation);
        await Advance();
        Assert.Equal(1m, Lv(await FireOk(G1, "E5")));
    }

    // 34. A12.122 with B25.65 on one advance: a Concealment dr (daylight, Overcast) and the Wind Change DR together; no freeze.
    [Fact]
    public async Task AConcealmentDrAndTheWindChangeDrOnOneAdvance()
    {
        await SetupAt(0, "russian", ["weather:overcast"], Sides(), Unit("r1", "defender-squad", "E4", "russian"), Unit("g1", "attacker-squad", "Z4", "german"));
        Assert.True(Dist("E4", "Z4") > 16);
        await ToNextRph(1, 5, 5);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Equal("rain", Current.Precipitation);
    }

    // ---------------------------------------------------------------- Rout, DM, Rally

    // 35. E1.54: at night a broken unit need not Low Crawl toward any particular terrain.
    [Fact]
    public async Task NightLowCrawlNeedNotGoTowardWoods()
    {
        await SetupAt(5, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E1", "german"), Unit("r1", "defender-squad", "E6", "russian", "asl:broken", "asl:dm"));
        Assert.Equal("rtph", Current.Phase);
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "r1",
            route = new[] { L("E7") },
            lowCrawl = true
        }));
    }

    [Fact]
    public async Task NoFailureToRoutAtNight()
    {
        await SetupAt(5, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E5", "german"), Unit("r1", "defender-squad", "E6", "russian", "asl:broken"));
        Assert.Equal("rtph", Current.Phase);
        Committed(await TryAdvance("end the RtPh"));
        Assert.Equal(InstanceStatus.Active, Current.Unit("r1")!.Status);
        Assert.False(Is(Current.Unit("r1")!, Conditions.Captured));
    }

    // 37. E1.54: still no Low Crawl toward a Known enemy unit.
    [Fact]
    public async Task NoLowCrawlTowardAKnownEnemy()
    {
        await SetupAt(5, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E9", "german"), Unit("r1", "defender-squad", "E6", "russian", "asl:broken", "asl:dm"));
        Refused(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "r1",
            route = new[] { L("E7") },
            lowCrawl = true
        }), "play.rout");
    }

    // 38. E1.54: a Rally Original DR at most the printed (broken) morale removes DM at night even if the Rally fails.
    [Theory]
    [InlineData(3, 4, false)]
    [InlineData(4, 5, true)]
    public async Task NightDmAndTheRallyDr(int colored, int white, bool keeps)
    {
        await SetupAt(0, "russian", ["night:3"], Sides(), Unit("r1", "defender-squad", "E3", "russian", "asl:broken", "asl:dm"),
            Unit("rl", "defender-leader-7-0", "E3", "russian"), Unit("g1", "attacker-squad", "L8", "german"));
        var result = await Try("rally", GameActions.Rally, Then(3, colored, white), new
        {
            unitId = "r1",
            leader = "rl"
        });
        Committed(result);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Broken));
        if (Current.Phase == "rph")
        {
            Committed(await TryAdvance("end the RPh"));
        }

        Assert.Equal(keeps, Is(Current.Unit("r1")!, Conditions.DesperationMorale));
    }

    // 39. E1.56: +1 to a Recovery dr at night: a 5 fails.
    [Fact]
    public async Task RecoveryAtNight()
    {
        await SetupAt(2, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Weapon("gm", "asl:mg", "attacker-lmg", "g2", "german"), Unit("r1", "defender-squad", "L8", "russian"));
        Committed(await Do(GameActions.Drop, NoRoll(), new
        {
            unitId = "g2",
            equipmentId = "gm"
        }));
        Committed(await Do(GameActions.Recover, Then(5), new
        {
            unitId = "g1",
            equipmentId = "gm"
        }));
        Assert.Null(((EquipmentInstance)Current.Find("gm")!).Holding);
    }

    // 40. E1.76: SAN +2 at night to at most 7: a printed 6 activates on 7, not on 8.
    [Theory]
    [InlineData(3, 4, true)]
    [InlineData(4, 4, false)]
    public async Task SanCappedAtSeven(int colored, int white, bool activates)
    {
        await SetupAt(1, "german", ["night:3"], Sides(russianSan: 6), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian"),
            Sniper("rs", "H8", "russian"));
        var before = Revision;
        Committed(await FireAt(G1, "E5", Then(3, colored, white)));
        Assert.Equal(activates, Since(before).Select(item => item.Payload).OfType<SniperAttacked>().Any(item => item.Sniper == "rs"));
    }

    // ---------------------------------------------------------------- Concealment at night

    // 41. E1.31: a concealed unit moving (not Assault) in Open Ground within the enemy's NVR keeps "?".
    [Fact]
    public async Task MovingInTheDarkKeepsConcealment()
    {
        await SetupAt(2, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E2", "german", "asl:concealed"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3")
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
    }

    // 42. E1.31: entering an Illuminated Location by Non-Assault Movement loses "?"; Assault Movement keeps it.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task MovingIntoLightLosesConcealment(bool assault, bool keeps)
    {
        await SetupAt(2, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E2", "german", "asl:concealed"), Unit("rl", "defender-leader", "E5", "russian"),
            Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Starshell("rl", "own-hex", Then(1, 1, 1)));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            assault
        }));
        Assert.Equal(keeps, Is(Current.Unit("g1")!, Conditions.Concealed));
    }

    // 43. E1.31: advancing at night into Open Ground in the enemy's LOS keeps "?".
    [Fact]
    public async Task AdvancingAtNightKeepsConcealment()
    {
        await SetupAt(6, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E2", "german", "asl:concealed"), Unit("r1", "defender-squad", "E5", "russian"));
        Assert.Equal("aph", Current.Phase);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3")
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
    }

    // ---------------------------------------------------------------- Movement

    // 44. E3.733, E1.51: Deep Snow: half an MF more into Open Ground, none into woods; night adds one into woods.
    [Theory]
    [InlineData("F3", false, 1, true)]
    [InlineData("E4", false, 2, false)]
    [InlineData("E4", true, 3, false)]
    public async Task DeepSnowInfantryMf(string to, bool night, int mf, bool half)
    {
        string[] rules = night ? ["weather:deep-snow", "night:3"] : ["weather:deep-snow"];
        await SetupAt(2, "german", rules, Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "L8", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L(to)
        }));
        Assert.Equal((mf, half), (Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.HalfMfSpent));
    }

    // 45. E1.51: NVR 0: no Double Time.
    [Fact]
    public async Task NoDoubleTimeAtNvrZero()
    {
        await SetupAt(2, "german", ["night:0"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "L8", "russian"));
        Refused(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("F3"),
            doubleTime = true
        }), "play.night-double-time");
    }

    private async Task StartVehicle(string vehicle)
    {
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new Dictionary<string, object> { ["vehicleId"] = vehicle, ["kind"] = "start" }));
        if (Current.Movement is { WindowOpen: true })
        {
            Committed(await Do(GameActions.PassFire, NoRoll(), new
            {
            }));
        }
    }

    private int[] EntryCosts(string vehicle) =>
        [.. Planner().VehicleEntries(Current, Current.Unit(vehicle)!).Where(item => item.Straddling is null && item.HalfMp is not null).Select(item => item.HalfMp!.Value)];

    // 46. E1.52, E3.64, E3.7331: a tank's entry into Open Ground: 1 MP; +1 at night, +1 in Mud, +1 in Deep Snow, none in Ground Snow.
    [Theory]
    [InlineData(null, 2)]
    [InlineData("night:3", 4)]
    [InlineData("weather:mud", 4)]
    [InlineData("weather:deep-snow", 4)]
    [InlineData("weather:ground-snow", 2)]
    public async Task TankOpenGroundMp(string? rule, int halfMp)
    {
        string[] rules = rule is null ? [] : [rule];
        await SetupAt(2, "german", rules, Sides(), Vehicle("gt", "attacker-tank", "H3", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var costs = EntryCosts("gt");
        Assert.True(costs.Min() == halfMp, string.Join(",", costs));
    }

    // 47. E3.724: Ground Snow: a truck on an unplowed road pays the road rate raised to one MP, plus one: 2 MP, not the Open Ground rate plus one.
    [Fact]
    public async Task TruckOnAnUnplowedRoadInGroundSnow()
    {
        foreach (var hex in Board01Fixture.Hexes())
        {
            roads.Add(hex);
        }

        await SetupAt(2, "german", ["weather:ground-snow"], Sides(), Vehicle("gt", "attacker-truck", "H3", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var costs = EntryCosts("gt");
        Assert.True(costs.Min() == 4, string.Join(",", costs));
    }

    // 48. E3.724: the same on a plowed road (SSR): 2 MP.
    [Fact]
    public async Task TruckOnAPlowedRoadInGroundSnow()
    {
        foreach (var hex in Board01Fixture.Hexes())
        {
            roads.Add(hex);
        }

        await SetupAt(2, "german", ["weather:ground-snow", "plowed-roads"], Sides(), Vehicle("gt", "attacker-truck", "H3", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var costs = EntryCosts("gt");
        Assert.True(costs.Min() == 4, string.Join(",", costs));
    }

    // 49. D8.21, E3.61, E3.7332: the Bog DRM into woods: +1 in Mud, +2 in Deep Snow.
    [Theory]
    [InlineData(null, 0)]
    [InlineData("weather:mud", 1)]
    [InlineData("weather:deep-snow", 2)]
    public async Task BogDrmInMudAndDeepSnow(string? rule, int extra)
    {
        string[] rules = rule is null ? [] : [rule];
        await SetupAt(2, "german", rules, Sides(), Vehicle("gt", "attacker-tank", "D4", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var all = Planner().VehicleEntries(Current, Current.Unit("gt")!);
        var woods = all.Where(item => item.To == BoardLocation.Parse(L("E4")) && item.Straddling is null).ToArray();
        Assert.True(woods.Length == 1 && woods[0].BogDrm == 4 + extra, string.Join(" | ", all.Select(item => $"{item.To} {item.Straddling} {item.HalfMp} {item.BogDrm} {item.Bar}")));
    }


    // 47b. B3.4, D1.1: the road board works: a truck on a road in clear weather pays 1/2 MP.
    [Fact]
    public async Task TruckOnARoadInClearWeather()
    {
        foreach (var hex in Board01Fixture.Hexes())
        {
            roads.Add(hex);
        }

        await SetupAt(2, "german", [], Sides(), Vehicle("gt", "attacker-truck", "H3", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var costs = EntryCosts("gt");
        Assert.True(costs.Min() == 1, string.Join(",", costs));
    }

    // 47c. E3.6, E3.64: Mud: an unpaved road is Open Ground for a truck (4 MP) plus 1 MP.
    [Fact]
    public async Task TruckOnAnUnpavedRoadInMud()
    {
        foreach (var hex in Board01Fixture.Hexes())
        {
            roads.Add(hex);
        }

        await SetupAt(2, "german", ["weather:mud"], Sides(), Vehicle("gt", "attacker-truck", "H3", "german"), Unit("r1", "defender-squad", "Q8", "russian"));
        await StartVehicle("gt");
        var costs = EntryCosts("gt");
        Assert.True(costs.Min() == 10, string.Join(",", costs));
    }

    // 53. E1.77: at night the ATTACKER ambushes with a Final dr two less than the DEFENDER's; the DEFENDER still needs three less (A11.4).
    [Theory]
    [InlineData(1, 3, "german")]
    [InlineData(3, 1, null)]
    [InlineData(4, 1, "russian")]
    public async Task NightAmbushMargin(int german, int russian, string? ambusher)
    {
        await SetupAt(6, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        Assert.Equal("aph", Current.Phase);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        }));
        await Advance();
        Assert.Equal("ccph", Current.Phase);
        Committed(await Do(GameActions.Ambush, Then(3, german, russian), new
        {
            location = L("E4")
        }));
        Assert.Equal(ambusher, Current.CloseCombats.Single().Ambusher);
    }


    // 54. E1.32, E1.101: as the Player Turn ends, an Open Ground unit gains "?" without a dr unless an enemy sees it within NVR.
    [Theory]
    [InlineData("E5", false)]
    [InlineData("E8", true)]
    public async Task NightConcealmentGainAndNvr(string enemy, bool gains)
    {
        await SetupAt(0, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", enemy, "russian"));
        await ToNextRph(1, 1);
        Assert.Equal(gains, Is(Current.Unit("g1")!, Conditions.Concealed));
    }

    // 55. E1.13: with NVR 0 every other Location is beyond NVR, even an ADJACENT one.
    [Fact]
    public async Task NvrZeroSeesOnlyItsOwnLocation()
    {
        await SetupAt(1, "german", ["night:0"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E2", "russian"));
        Refused(await FireAt(G1, "E2"), "play.night-nvr");
    }

    // ---------------------------------------------------------------- Extreme Winter

    private async Task SetupDated(int month, int year, string[] rules, int advances, string firstSide, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "TP16",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["specialRules"] = rules,
                ["sides"] = Sides(),
                ["scenarioMonth"] = month,
                ["scenarioYear"] = year,
            },
            placements,
        })));
        await Advance(advances);
    }

    // 50. E3.741: German MG before April 1942: B# 11 - 2 = 9.
    [Fact]
    public async Task ExtremeWinterGermanLmgBreaksOnNine()
    {
        await SetupDated(1, 1942, ["weather:extreme-winter", "weather:ground-snow"], 1, "german", Unit("g1", "attacker-squad", "E3", "german"),
            Weapon("gm", "asl:mg", "attacker-lmg", "g1", "german"), Unit("r1", "defender-squad", "E5", "russian"));
        Committed(await Do(GameActions.Fire, Then(3, 4, 5), new
        {
            firers = G1,
            weapons = Uses("g1", "gm"),
            target = L("E5")
        }));
        Assert.True(Is(Current.Find("gm")!, Conditions.Malfunctioned));
    }

    // 51. E3.742: a German broken squad's Rally DR 11 outside a building before April 1942: Casualty Reduction.
    [Fact]
    public async Task ExtremeWinterFateOnEleven()
    {
        await SetupDated(1, 1942, ["weather:extreme-winter", "weather:ground-snow"], 0, "german", Unit("g1", "attacker-squad", "E3", "german", "asl:broken"),
            Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("r1", "defender-squad", "L8", "russian"));
        var result = await Try("rally", GameActions.Rally, Then(3, 5, 6), new
        {
            unitId = "g1",
            leader = "gl"
        });
        Committed(result);
        var unit = Current.Unit("g1");
        Assert.True(unit is null || unit.Status != InstanceStatus.Active || unit.Kind != "asl:squad", unit?.Kind + " " + unit?.Status);
    }

    // 52. E3.741: Russians after April 1941 (January 1942) are not affected; in January 1941 they are (B# one lower).
    [Theory]
    [InlineData(1942, false)]
    [InlineData(1941, true)]
    public async Task ExtremeWinterRussians(int year, bool breaks)
    {
        await SetupDated(1, year, ["weather:extreme-winter", "weather:ground-snow"], 1, "russian", Unit("g1", "attacker-squad", "E5", "german"), Unit("r1", "defender-squad", "E3", "russian"),
            Weapon("rm", "asl:mg", "defender-lmg", "r1", "russian"));
        var breakdown = FireReference();
        Committed(await Do(GameActions.Fire, Then(3, breakdown - 6, 5), new
        {
            firers = R1,
            weapons = Uses("r1", "rm"),
            target = L("E5")
        }));
        Assert.Equal(breaks, Is(Current.Find("rm")!, Conditions.Malfunctioned));
    }

    private static int FireReference() => 11;
}
