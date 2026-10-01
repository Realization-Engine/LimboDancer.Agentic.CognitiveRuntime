using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 16 in live play (rulings R16.1 to R16.14): night and weather SSRs, what a unit sees at night, the Low Visibility DRM, Starshells,
/// the Wind Change DR, rout, DM, movement, and concealment at night, and the weather's MF. A board 01 grid of Open Ground with woods at E4, fixed dice,
/// and a clear LOS everywhere.
/// </summary>
public sealed class BacklogPass16Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a716");
    private static readonly GameScope Scope = new(Tenant, "p16");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
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

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p16-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass16Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static readonly TerrainType Woods = new() { Code = 2, Name = "Woods", Category = LosCategory.Woods };

    /// <summary>Board 01's grid, Open Ground at level 0 but woods at E4.</summary>
    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, text == "E4" ? Woods : type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
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

    // R16.1, R16.9: the night and weather SSRs are checked at setup.
    [Fact]
    public async Task TheNightAndWeatherRulesAreCheckedAtSetup()
    {
        Refused(await SetupRaw(["weather:fog"]), "play.weather-rule");
        Refused(await SetupRaw(["night:7"]), "play.night-rule");
        Refused(await SetupRaw(["weather:mud", "weather:deep-snow"]), "play.weather-rule");
        Refused(await SetupRaw(["weather:extreme-winter", "weather:deep-snow"], null, null), "play.weather-rule");
        Refused(await SetupRaw(["night-moon:full"]), "play.night-rule");
        Refused(await SetupRaw(["weather:extreme-winter"]), "play.weather-rule");
        Committed(await SetupRaw(["night:8", "weather:ground-snow", "night-clouds:scattered", "night-moon:half"]));
        Assert.Equal(8, Current.Nvr);
    }

    // R16.2, R16.3 (E1.101, E1.7): a Location beyond NVR is out of sight; within it, +1 Low Visibility DRM.
    [Fact]
    public async Task FireBeyondNvrIsRefusedAndWithinItTakesTheNightDrm()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E7", "russian"),
            Unit("r2", "defender-squad", "E5", "russian"));
        Refused(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E7")
        }), "play.night-nvr");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E5")
        }));
        var resolution = LastFire(before);
        Assert.Contains(resolution.Arithmetic!.Drm, item => item.Name == "lv-hindrance" && item.Value == 1m);
    }

    // R16.2 (E1.8, E1.81): a Gunflash beyond NVR is seen, and fire at it is Area Fire.
    [Fact]
    public async Task AGunflashBeyondNvrIsAreaFire()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "E7", "russian", "asl:first-fire"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E7")
        }));
        var fire = Fires(before).Single();
        Assert.True(fire.Facts.Deserialize<FireAttack>(LiveFire.Json)!.BeyondNvr);
        Assert.Contains(Resolution(fire).Arithmetic!.Firers.SelectMany(item => item.Multipliers), item => item.Name == "area-fire-gunflash");
    }

    // R16.3 (E1.75): no multi-Location fire group at night.
    [Fact]
    public async Task NoFireGroupSpansLocationsAtNight()
    {
        await SetupAt(1, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E4", "german"),
            Unit("r1", "defender-squad", "E5", "russian"));
        Refused(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1G2,
            target = L("E5")
        }), "play.night-fire-group");
    }

    // R16.11 (E3.32): Mist adds +1 beyond six hexes, by day.
    [Fact]
    public async Task MistHindersFireBeyondSixHexes()
    {
        await SetupAt(1, "german", ["weather:mist"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "M3", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("M3")
        }));
        Assert.Contains(LastFire(before).Arithmetic!.Drm, item => item.Name == "lv-hindrance" && item.Value == 1m);
    }

    // R16.8 (E1.92, E1.923): a Starshell Illuminates three hexes around where it lands, so a unit beyond NVR there may be fired on.
    [Fact]
    public async Task AStarshellIlluminatesATargetBeyondNvr()
    {
        await SetupAt(1, "german", ["night:1"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("gl", "attacker-leader-8-0", "E3", "german"),
            Unit("r1", "defender-squad", "E7", "russian"), Unit("r2", "defender-squad", "F3", "russian"));
        Refused(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E7")
        }), "play.night-nvr");
        Committed(await Do(GameActions.FireStarshell, Then(1, 1, 1, 1), new
        {
            unitId = "gl",
            method = "three-hexes",
            at = L("E6")
        }));
        var starshell = Assert.Single(Current.Entities, entity => entity.Kind == "asl:starshell");
        Assert.True(Current.StarshellUsed);
        Refused(await Do(GameActions.FireStarshell, Then(1), new
        {
            unitId = "gl",
            method = "own-hex"
        }), "play.starshell-once");
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E7")
        }));
        Assert.Null(Fires(before).Single().Facts.Deserialize<FireAttack>(LiveFire.Json)!.BeyondNvr);

        // E1.923: the Starshell is removed at the end of the CCPh.
        await Advance(6);
        Assert.Equal("ccph", Current.Phase);
        Committed(await Do(GameActions.AdvancePhase, Then(1), new
        {
        }));

        Assert.DoesNotContain(Current.Entities, entity => entity.Id == starshell.Id && entity.Kind == "asl:starshell");
    }

    // R16.1, R16.10 (E1.12, B25.65): the Wind Change DR at the start of the second RPh changes the NVR on a colored 6.
    [Fact]
    public async Task TheWindChangeDrChangesTheNvr()
    {
        await SetupAt(0, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        await Advance(7);
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.AdvancePhase, Then(1, 6, 5), new
        {
        }));
        Assert.Equal("rph", Current.Phase);
        Assert.Equal(4, Current.Nvr);
        Assert.Single(Since(before).Select(item => item.Payload).OfType<WindChanged>());
    }

    // R16.10 (E3.51): with Overcast, a Wind Change DR of 10 or more starts the rain, which causes Mist.
    [Fact]
    public async Task RainStartsOnAWindChangeDrOfTen()
    {
        await SetupAt(0, "german", ["weather:overcast"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.AdvancePhase, Then(1, 5, 5), new
        {
        }));
        Assert.Equal("rain", Current.Precipitation);
        Assert.Null(Current.Nvr);
    }

    // R16.6 (E1.54): at night a broken unit Low Crawls rather than routs.
    [Fact]
    public async Task AtNightABrokenUnitLowCrawls()
    {
        await SetupAt(5, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E6", "russian", "asl:broken"));
        Assert.Equal("rtph", Current.Phase);
        Refused(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "r1",
            route = new[] { L("E7"), L("E8") }
        }), "play.night-rout");
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "r1",
            route = new[] { L("E7") },
            lowCrawl = true
        }));
    }

    // R16.6 (E1.54): at night DM stays after a RPh with no Rally DR at most the printed morale.
    [Fact]
    public async Task AtNightDmStaysWithoutAQualifyingRallyDr()
    {
        await SetupAt(0, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "H8", "russian", "asl:broken", "asl:dm"));
        await Advance();
        Assert.True(Is(Current.Unit("r1")!, Conditions.DesperationMorale));
    }

    // R16.5, R16.12 (E1.51, E3.64): one MF more into woods at night; half an MF more into Open Ground in Mud.
    [Fact]
    public async Task NightAndMudCostMoreMf()
    {
        await SetupAt(2, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        }));
        Assert.Equal(3, Current.Unit("g1")!.MfSpent);
    }

    [Fact]
    public async Task MudCostsHalfAnMfMoreInOpenGround()
    {
        await SetupAt(2, "german", ["weather:mud"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("F3")
        }));
        var mover = Current.Unit("g1")!;
        Assert.Equal(1, mover.MfSpent);
        Assert.True(mover.HalfMfSpent);
    }

    // R16.4 (E1.32, E1.101): at night a unit that would need a Concealment dr gains "?" without one, when no enemy sees it within NVR.
    [Fact]
    public async Task AtNightConcealmentIsGainedWithoutADr()
    {
        await SetupAt(7, "german", ["night:1"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E6", "russian"));
        Assert.Equal("ccph", Current.Phase);
        Committed(await Do(GameActions.AdvancePhase, Then(1), new
        {
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
    }

    // R16.2 (E1.8): at night First Fire counters stay, as Gunflashes, until the end of the AFPh.
    [Fact]
    public async Task AtNightFirstFireCountersStayUntilTheEndOfTheAfph()
    {
        await SetupAt(3, "german", ["night:3"], Sides(), Unit("g1", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-squad", "H8", "russian", "asl:first-fire"));
        Assert.Equal("dfph", Current.Phase);
        await Advance();
        Assert.True(Is(Current.Unit("r1")!, Conditions.FirstFire));
        await Advance();
        Assert.False(Is(Current.Unit("r1")!, Conditions.FirstFire));
    }

    // R16.7 (E1.76): at night the SAN is two higher.
    [Fact]
    public async Task AtNightTheSanIsTwoHigher()
    {
        await SetupAt(1, "german", ["night:3"], Sides(russianSan: 5), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian"),
            Sniper("rs", "H8", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 3, 4), new
        {
            firers = G1,
            target = L("E5")
        }));
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<SniperAttacked>(), item => item.Sniper == "rs");
    }

    // E1.91, E1.33 (referee, pass 16): the first Starshell needs an enemy unit the firer sees, within its NVR or Illuminated.
    [Fact]
    public async Task TheFirstStarshellNeedsAnEnemyUnitSeenWithinNvr()
    {
        await SetupAt(1, "german", ["night:1"], Sides(), Unit("gl", "attacker-leader-8-0", "E3", "german"), Unit("r1", "defender-squad", "E7", "russian"));
        Refused(await Do(GameActions.FireStarshell, Then(1), new
        {
            unitId = "gl",
            method = "own-hex"
        }), "play.starshell-first");
    }

    // E3.54 (referee, pass 16): rain that stopped has still fallen.
    [Fact]
    public async Task RainThatStoppedHasStillFallen()
    {
        await SetupAt(0, "german", ["weather:overcast"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.AdvancePhase, Then(1, 5, 5), new
        {
        }));
        Assert.Equal("rain", Current.Precipitation);
        await Advance(7);
        Committed(await Do(GameActions.AdvancePhase, Then(1, 1, 1), new
        {
        }));
        Assert.Null(Current.Precipitation);
        Assert.True(Current.Rained);
    }

    // E3.734 (referee, pass 16): no SMOKE grenades in Mud but inside a building.
    [Fact]
    public async Task NoSmokeIsPlacedInMud()
    {
        await SetupAt(2, "german", ["weather:mud"], Sides(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        Refused(await Do(GameActions.Move, Then(1), new
        {
            unitIds = G1,
            to = L("E3"),
            smoke = L("E3"),
            smokeBy = "g1"
        }), "play.smoke-weather");
    }
}
