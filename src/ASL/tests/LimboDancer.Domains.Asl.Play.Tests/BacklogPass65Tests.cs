using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;
using ScenarioA1CapturedVehicleRules = LimboDancer.Domains.Asl.Rules.ScenarioA1CapturedVehicleRules;
using ScenarioA1CrewActsRules = LimboDancer.Domains.Asl.Rules.ScenarioA1CrewActsRules;
using ScenarioA1RiderRules = LimboDancer.Domains.Asl.Rules.ScenarioA1RiderRules;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 65 in live play, Passengers, Riders, and crews. Increment 2a: a crew that leaves its vehicle is its nationality's vehicle crew (D5.1;
/// ruling R65.19), a vehicle crew mans a Gun as a crew does (A1.123, D5.1; ruling R65.2), and a game recorded at catalog 1.17.0 fires (ruling R65.4).
/// Board 01's hex facts, fixed dice, and a stub LOS reader; every read of the state replays the game, so each record is checked by the projector.
/// </summary>
public sealed class BacklogPass65Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f565");
    private static readonly GameScope Scope = new(Tenant, "pass65");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] B9Route = ["bd01:B9:0"];
    private static readonly string[] HalfSquadOnly = ["de-h"];
    private static readonly string[] LmgOnly = ["de-lmg"];
    private const string Firer = "bd01:B10:0";
    private const string Target = "bd01:B8:0";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass65-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass65Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    // A hex of board 01 read as another terrain (increment 2e: woods for a Bail Out).
    private (string Hex, string Terrain)? changed;

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle(changed: changed)]), Vocabulary, [Catalog], fireLos: new StubLos());

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

    /// <summary>A Gun manned by a unit of the given kind, its barrel to the given facing.</summary>
    private static Dictionary<string, object>[] Gun(string id, string definition, string crew, string crewKind, string crewDefinition, string at, string side, string facing) =>
    [
        Unit(crew, crewKind, crewDefinition, at, side),
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

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    /// <summary>A unit aboard a vehicle as a Passenger (D6.1).</summary>
    private static Dictionary<string, object> Aboard(string id, string kind, string definition, string vehicle, string side)
    {
        var unit = Unit(id, kind, definition, Target, side);
        unit["position"] = new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "passenger" };
        return unit;
    }

    /// <summary>A vehicle whose crew is CE: its BU counter removed (D5.2, D5.3).</summary>
    private static Dictionary<string, object> Exposed(Dictionary<string, object> vehicle)
    {
        vehicle["conditions"] = new Dictionary<string, bool> { ["asl:bu"] = false };
        return vehicle;
    }

    /// <summary>
    /// The game's records as a game recorded before pass 65 holds them (ruling R65.5): no PRC in the facts, no PRC result or roll, and no event of one of
    /// the named units.
    /// </summary>
    private GameEvent[] AsBeforeThePass(params string[] prc)
    {
        static void Remove(JsonNode? node, string[] names)
        {
            if (node is JsonObject item)
            {
                foreach (var name in names)
                {
                    item.Remove(name);
                }

                foreach (var (_, child) in item.ToArray())
                {
                    Remove(child, names);
                }
            }
            else if (node is JsonArray list)
            {
                foreach (var child in list)
                {
                    Remove(child, names);
                }
            }
        }

        static JsonElement Strip(JsonElement element, params string[] names)
        {
            var node = JsonNode.Parse(element.GetRawText());
            Remove(node, names);
            return JsonSerializer.SerializeToElement(node);
        }

        Dictionary<string, string> Kept(IReadOnlyDictionary<string, string> rolls) =>
            rolls.Where(item => !item.Key.StartsWith("prc:", StringComparison.Ordinal) && !prc.Any(id => item.Key.EndsWith(":" + id, StringComparison.Ordinal)))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        return [.. store.Read(Scope)!.Events
            .Where(item => item.Payload is not (ConditionsChanged or InstanceMoved) || !prc.Contains((item.Payload as ConditionsChanged)?.Id ?? ((InstanceMoved)item.Payload).Id))
            .Select((item, index) => item with { Revision = index + 1 })
            .Select(item => item.Payload switch
            {
                FireResolved fire => item with
                {
                    Payload = fire with { Rolls = Kept(fire.Rolls), Facts = Strip(fire.Facts, "prc"), Resolution = Strip(fire.Resolution, "prcAttacks", "prcDesperation", "prcSurvival") }
                },
                OrdnanceFired shot => item with
                {
                    Payload = shot with { Rolls = Kept(shot.Rolls), Facts = Strip(shot.Facts, "prc"), Resolution = Strip(shot.Resolution, "prcAttacks", "prcDesperation", "prcSurvival") }
                },
                _ => item,
            })];
    }

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other at a date, under the named catalog, gone on to the first side's PFPh.</summary>
    private async Task Setup(string firstSide, string otherSide, int year, int month, string catalog, params Dictionary<string, object>[] placements)
    {
        await Start(firstSide, otherSide, year, month, catalog, placements);
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    /// <summary>The same game left in the first side's RPh, where a Shocked AFV makes its recovery dr (C7.42).</summary>
    private async Task Start(string firstSide, string otherSide, int year, int month, string catalog, Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass65Tests),
                ["catalog"] = catalog,
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
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
    }

    /// <summary>
    /// A game whose named AFV starts Shocked: in the first side's RPh it makes its recovery dr of 3, an Unconfirmed Kill, still Shocked (C7.42), and the game
    /// goes on to the PFPh.
    /// </summary>
    private async Task SetupShocked(string firstSide, string otherSide, string shocked, params Dictionary<string, object>[] placements)
    {
        await Start(firstSide, otherSide, 1943, 7, "asl-scenario-a1@1.18.0", placements);
        Committed(await Do(GameActions.RecoverShock, Once(3), new
        {
            vehicleId = shocked
        }));
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

    private Task<PlayResult> Shoot(string gun, string? vehicle, string ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = Target, ["ammunition"] = ammunition };
        if (vehicle is not null)
        {
            arguments["targetVehicle"] = vehicle;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private static int HitColumn(OrdnanceFired shot) => shot.Resolution.GetProperty("hit").GetProperty("arithmetic").GetProperty("columnFp").GetInt32();

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AKilledGermanTanksSurvivingCrewIsTheGermanVehicleCrew()
    {
        // D5.6, D5.1 (ruling R65.19): the T-34's AP eliminates the PzKpfw IIIH (CS# 6); a Crew Survival DR of 4 places the German vehicle crew,
        // 1-2-7, beneath the wreck, with the id {attempt}-{vehicle}-crew.
        await Setup("russian", "german", 1942, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Vehicle("de-tank", "attacker-tank", Target, "german", "east"));
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-tank", "ap", 4, 2, 3, 3, 2, 2));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.True(Current.Unit("de-tank")!.Status == InstanceStatus.Wrecked, kill.GetRawText());
        var crew = Assert.Single(Current.Units, unit => unit.Side == "german" && unit.Kind == "asl:vehicle-crew");
        Assert.Equal(("german-vehicle-crew", Target, true), (crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(), crew.Id.EndsWith("-de-tank-crew", StringComparison.Ordinal)));
        var created = Since(before).Select(item => item.Payload).OfType<InstanceCreated>().Single(item => item.Instance.Id == crew.Id);
        Assert.Equal("asl:vehicle-crew", created.Instance.Kind);
        Assert.Equal(1, Catalog.Definition("german-vehicle-crew")!.Printed("front", "asl:firepower")!.Value!.Number);
        NoReplayErrors();

        // A game recorded before the pass placed the infantry crew; the definition is in its instance-created, so it replays as it did.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is InstanceCreated { Instance.Kind: "asl:vehicle-crew" } placed
            ? item with { Payload = new InstanceCreated(placed.Instance with { Kind = "asl:crew", Definition = "attacker-crew" }) }
            : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Equal(("asl:crew", "attacker-crew"), (replayed.Current!.Unit(crew.Id)!.Kind, replayed.Current.Unit(crew.Id)!.Definition!.Definition));
    }

    [Fact]
    public async Task AVehicleCrewMansAGunAsACrewDoes()
    {
        // A1.123, D5.1 (ruling R65.2): a vehicle crew has a crew's capabilities, so it mans the 6-Pounder as qualified Infantry: the shot is the one
        // pass 60's British crew fires, its AP at the squad on the 2 column by HE Equivalency (C8.31).
        await Setup("british", "german", 1942, 6, "asl-scenario-a1@1.18.0",
            [.. Gun("uk-gun", "british-at-gun", "uk-crew", "asl:vehicle-crew", "british-vehicle-crew", Firer, "british", "north-east"),
                Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        Committed(await Shoot("uk-gun", null, "ap", 4, 3, 6, 6));
        var shot = LastShot(before);
        Assert.Equal(2, HitColumn(shot));
        Assert.False(shot.Facts.TryGetProperty("nonQualified", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task AGameRecordedAtCatalog117Fires()
    {
        // Ruling R65.4: a game set up at 1.17.0 reads the loaded 1.18.0 catalog, and the packages serve an older minor version of it.
        await Setup("british", "german", 1942, 6, "asl-scenario-a1@1.17.0",
            [.. Gun("uk-gun", "british-at-gun", "uk-crew", "asl:crew", "british-crew", Firer, "british", "north-east"),
                Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        Assert.Equal("asl-scenario-a1@1.17.0", Assert.IsType<GameStarted>(store.Read(Scope)!.Events[0].Payload).Catalog);
        Assert.True(LiveFire.ServesCatalog(Current));
        Committed(await Shoot("uk-gun", null, "ap", 4, 3, 6, 6));
        NoReplayErrors();
    }

    // Increment 2b: Passengers and Riders as targets (D.8, D.8A, D.8B, D6.9; rulings R65.5 to R65.9).

    [Fact]
    public async Task AnIftAttackOnALoadedHalftrackPinsItsCePassengers()
    {
        // D.8B, D5.31: two Russian 4-4-7s fire 8 FP at the CE SPW 251/1 two hexes off, an Original DR of 6. Its crew and the 4-6-7 aboard each take the
        // NMC of a Final DR of 8 (+2 for CE): the crew passes (2); the squad passes on its Morale Level, 7, and is pinned (A7.8), still aboard. The record
        // carries the PRC.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"), Exposed(Vehicle("de-ht", "attacker-halftrack", Target, "german", "east")),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-ht", "german"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(2, 4, 1, 1, 3, 4), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var prc = fire.Facts.GetProperty("vehicles")[0].GetProperty("prc")[0];
        Assert.Equal(("de-s", "passenger"), (prc.GetProperty("unitId").GetString(), prc.GetProperty("role").GetString()));
        var attack = fire.Resolution.GetProperty("vehicleEffects")[0].GetProperty("prcAttacks")[0];
        Assert.Equal(("general", 8), (attack.GetProperty("kind").GetString(), attack.GetProperty("finalDr").GetInt32()));
        var squad = Current.Unit("de-s")!;
        Assert.Equal((ConditionState.True, "de-ht"), (GameState.Condition(squad, "asl:pinned"), Current.Aboard("de-s")));
        NoReplayErrors();

        // An attack recorded before the pass carries no PRC and replays as it was: the squad aboard untouched.
        var older = Planner().Replay(AsBeforeThePass("de-s"));
        Assert.False(older.HasErrors);
        Assert.NotEqual(ConditionState.True, GameState.Condition(older.Current!.Unit("de-s")!, "asl:pinned"));
    }

    [Fact]
    public async Task AKilledTrucksPassengersRollForSurvival()
    {
        // D6.9, D5.6: the T-34's HE eliminates the German truck (TK DR 11 below its Final TK# of 12, more than half of it). Each PRC rolls against the cs#
        // 6: the HS aboard survives on a 6 and is placed beneath the wreck; the squad aboard dies on a 12, with the truck.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Vehicle("de-truck", "attacker-truck", Target, "german", "east"), Aboard("de-h", "asl:half-squad", "attacker-half-squad", "de-truck", "german"),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-truck", "german"));
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-truck", "he", 4, 2, 5, 6, 3, 3, 6, 6));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.Equal("eliminated", kill.GetProperty("result").GetString());
        Assert.Equal([true, false], kill.GetProperty("prcSurvival").EnumerateArray().Select(item => item.GetProperty("survived").GetBoolean()));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-truck")!.Status);
        var survivor = Current.Unit("de-h")!;
        Assert.Equal((InstanceStatus.Active, Target, (string?)null, true), (survivor.Status, Current.Location("de-h")!.Location.ToString(), Current.Aboard("de-h"),
            survivor.MovementEnded));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("de-s")!.Status);
        NoReplayErrors();

        // A destruction recorded before the pass carries no Survival: both are eliminated with the truck, as before.
        var older = Planner().Replay(AsBeforeThePass("de-h", "de-s"));
        Assert.False(older.HasErrors);
        Assert.NotEqual(InstanceStatus.Active, older.Current!.Unit("de-h")!.Status);
    }

    // Increment 2c: Passenger action (D6.1, D6.5, D6.63 to D6.65, D7.11, D7.16, D.3; rulings R65.10 to R65.13).

    private static void Refused(PlayResult result, string text)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(text, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Broken(Dictionary<string, object> unit)
    {
        unit["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        return unit;
    }

    private async Task Advance() => Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
    {
    }));

    /// <summary>The hex beyond a hexside of a hex of board 01, as the vehicle tests of pass 11 name it.</summary>
    private static string Beyond(string hex, HexsideDirection side) => $"bd01:{Board01Fixture.Handle().Neighbor(HexName.Parse(hex), side)}:0";

    [Fact]
    public async Task ATrucksPassengerFiresAtHalfFpAndFreezesTheTruck()
    {
        // D6.1, D6.72: the 4-6-7 aboard the Stopped truck fires at the Russian squad two hexes off, directed by the 9-1 aboard with it (D6.65), its 4 FP
        // halved as Mounted Fire: the 2 column, an Original DR of 11 and -1 leadership: no effect. D.3: the truck is marked as having Prep Fired.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-truck", "attacker-truck", Firer, "german", "north-east"),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-truck", "german"), Aboard("de-l", "asl:leader", "attacker-leader-9-1", "de-truck", "german"),
            Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        var fired = await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = SquadOnly,
            director = "de-l",
            target = Target,
        });
        Committed(fired);
        Assert.Contains(fired.Reasons, reason => reason.Contains("de-s fires from de-truck as Mounted Fire, its FP times 1/2 (D6.1)", StringComparison.Ordinal));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal("de-truck", fire.Facts.GetProperty("firers")[0].GetProperty("mounted").GetProperty("vehicleId").GetString());
        Assert.Equal(2m, fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("de-truck")!, Conditions.PrepFire));
        NoReplayErrors();

        // D6.5, D.3: in the MPh the squad that fired does not unload.
        await Advance();
        Refused(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-truck",
            kind = "unload",
            units = SquadOnly,
        }), "fired from its vehicle");
    }

    [Fact]
    public async Task ACeHalftracksPassengerFiresNoMmg()
    {
        // D6.1: a Passenger fires a LMG, a PIAT, or a DC, so the squad aboard the CE halftrack does not fire its MMG.
        var mmg = new Dictionary<string, object>
        {
            ["id"] = "de-mmg",
            ["kind"] = "asl:mg",
            ["definition"] = "attacker-mmg",
            ["side"] = "german",
            ["holding"] = new
            {
                holder = "de-s",
                role = "possessed"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        };
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-ht", "attacker-halftrack", Firer, "german", "north-east"),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-ht", "german"), mmg, Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"));
        Refused(await Do(GameActions.Fire, NoRoll(), new
        {
            firers = SquadOnly,
            target = Target,
            weapons = new Dictionary<string, string[]> { ["de-s"] = ["de-mmg"] },
        }), "a Passenger fires only a LMG, a PIAT, or a Thrown DC (D6.1)");
    }

    [Fact]
    public async Task ABuHalftracksPassengerDoesNotFire()
    {
        // D6.62: an armored halftrack's Passengers fire only while CE; its crew is BU.
        var halftrack = Vehicle("de-ht", "attacker-halftrack", Firer, "german", "north-east");
        halftrack["conditions"] = new Dictionary<string, bool> { ["asl:bu"] = true };
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", halftrack, Aboard("de-s", "asl:squad", "attacker-squad", "de-ht", "german"),
            Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"));
        Refused(await Do(GameActions.Fire, NoRoll(), new
        {
            firers = SquadOnly,
            target = Target,
        }), "fire only while CE");
    }

    [Fact]
    public async Task ABrokenPassengerRoutsBeneathItsStoppedTruck()
    {
        // D6.1, D5.311 (ruling R65.11): in the RtPh the broken 4-6-7 aboard the Stopped truck routs beneath it with all its MF, on foot in its Location.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-truck", "attacker-truck", Firer, "german", "north-east"),
            Broken(Aboard("de-s", "asl:squad", "attacker-squad", "de-truck", "german")), Unit("ru-s", "asl:squad", "defender-squad", "bd01:J2:0", "russian"));
        for (var phase = 0; phase < 4; phase++)
        {
            await Advance();
        }

        Assert.Equal("rtph", Current.Phase);
        Assert.Null(Planner().MustRout(Current, Current.Unit("de-s")!));
        Assert.True(Planner().MayRout(Current, Current.Unit("de-s")!));
        Refused(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "de-s",
            route = B9Route,
        }), "play.rout-beneath");
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "de-s",
            beneath = true,
        }));
        Assert.Null(Current.Aboard("de-s"));
        Assert.Equal(Firer, Current.Location("de-s")!.Location.ToString());
        Assert.Contains("de-s", Current.RoutedThisPhase);
        NoReplayErrors();
    }

    /// <summary>
    /// The CE SPW 251/1 at A9 with a 4-6-7 and a 9-1 aboard starts and enters the Russian squad's Open Ground hex with an OVR (D7.1), its Passengers named
    /// as <paramref name="units"/> gives (all that may fire when null); the DEFENDER passes, and the OVR is resolved.
    /// </summary>
    private async Task<FireResolved> OverrunWithPassengers(string[]? units)
    {
        var at = Beyond("A9", HexsideDirection.NorthEast);
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Exposed(Vehicle("de-ht", "attacker-halftrack", "bd01:A9:0", "german", "east")),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-ht", "german"), Aboard("de-l", "asl:leader", "attacker-leader-9-1", "de-ht", "german"),
            Unit("ru-s", "asl:squad", "defender-squad", at, "russian"));
        await Advance();
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-ht",
            kind = "start",
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        var entry = new Dictionary<string, object> { ["vehicleId"] = "de-ht", ["kind"] = "enter", ["to"] = at, ["overrun"] = true };
        if (units is not null)
        {
            entry["units"] = units;
        }

        Committed(await Do(GameActions.MoveVehicle, NoRoll(), entry));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        var before = Revision;
        Committed(await Do(GameActions.Overrun, Once(5, 6, 3, 3, 3, 3), new
        {
        }));
        return Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
    }

    [Fact]
    public async Task ALoadedHalftracksOvrAddsHalfItsPassengersFpTripledAndTheirLeader()
    {
        // D7.11: the CE halftrack's 4-6-7 adds half of 4, tripled: 6 FP; D7.16: the 9-1 aboard modifies the OVR DR by -1, the one leadership modifier.
        var fire = await OverrunWithPassengers(null);
        var declared = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<VehicleStepped>().Last(step => step.Overrunning);
        Assert.Equal(["de-l", "de-s"], declared.Units!.Order(StringComparer.Ordinal));
        Assert.Equal(2, fire.Facts.GetProperty("overrun").GetProperty("passengers").GetArrayLength());
        var arithmetic = fire.Resolution.GetProperty("arithmetic");
        Assert.Contains(arithmetic.GetProperty("firers").EnumerateArray(), item => item.GetProperty("unitId").GetString() == "de-s" && item.GetProperty("firepower").GetDecimal() == 6m);
        Assert.Contains(arithmetic.GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "leadership:de-l");
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOvrWhoseRecordNamesNoPassengerResolvesAsBefore()
    {
        // An OVR declared with no Passenger adding FP is recorded as one made before the pass: no units on the step, no Passengers in the facts, and the
        // OVR's FP is the vehicle's alone, 6 less than with the squad.
        var fire = await OverrunWithPassengers([]);
        var declared = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<VehicleStepped>().Last(step => step.Overrunning);
        Assert.Null(declared.Units);
        Assert.False(fire.Facts.GetProperty("overrun").TryGetProperty("passengers", out _));
        var arithmetic = fire.Resolution.GetProperty("arithmetic");
        Assert.DoesNotContain(arithmetic.GetProperty("firers").EnumerateArray(), item => item.GetProperty("unitId").GetString() == "de-s");
        Assert.DoesNotContain(arithmetic.GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "leadership:de-l");
        NoReplayErrors();
    }

    // Increment 2d: loading and unloading completed (D6.4, D6.5, D5.43, A4.6, A4.11, A4.5, A7.821, D2.401; rulings R65.14, R65.15).

    private static readonly string[] SquadOnly = ["de-s"];

    private Task<PlayResult> Drive(string vehicle, string kind, params string[] units) => Do(GameActions.MoveVehicle, NoRoll(), new
    {
        vehicleId = vehicle,
        kind,
        units,
    });

    private async Task PassFire() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    /// <summary>The moving vehicle ends its move, the DEFENDER passing on any MP it spends to do so.</summary>
    private async Task EndTheMove()
    {
        for (var step = 0; step < 4 && Current.Movement is { Vehicle: true } moving; step++)
        {
            Committed(await Do(moving.WindowOpen ? GameActions.PassFire : GameActions.EndMove, NoRoll(), new
            {
            }));
        }
    }

    /// <summary>The names and rules of the FFNAM and FFMO DRM an attack took.</summary>
    private static string[] MovementDrm(FireResolved fire) =>
        [.. fire.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)
            .Where(name => name is "ffnam" or "ffmo")];

    /// <summary>A German truck Stopped in Open Ground at B8 with a 4-6-7 aboard, the Russian squads two hexes off, in the German MPh.</summary>
    private async Task TruckInTheOpen(params Dictionary<string, object>[] others)
    {
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", [Vehicle("de-truck", "attacker-truck", Target, "german", "east"),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-truck", "german"), Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"), .. others]);
        await Advance();
    }

    [Fact]
    public async Task AnUnloadFromATruckInTheOpenTakesFfnamAndFfmo()
    {
        // D6.5, A4.6 (ruling R65.14): the 4-6-7 unloads from the Stopped truck; the Russians' First Fire attacks it on foot, FFNAM and FFMO each -1, since
        // the truck is an unarmored conveyance in Open Ground. Original DR 12: no effect. The record names the unloader.
        await TruckInTheOpen();
        var unloaded = await Drive("de-truck", "unload", "de-s");
        Committed(unloaded);
        Assert.Contains(unloaded.Reasons, reason => reason.Contains("FFNAM, and FFMO in Open Ground from an unarmored vehicle (D6.5)", StringComparison.Ordinal));
        Assert.True(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<VehicleStepped>().Last().Interim);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 6, 6, 6, 6, 6), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var embarking = fire.Facts.GetProperty("embarking");
        Assert.Equal(("de-s", true, true), (embarking.GetProperty("units")[0].GetString(), embarking.GetProperty("unloading").GetBoolean(),
            embarking.GetProperty("unarmoredConveyance").GetBoolean()));
        Assert.Contains(fire.Facts.GetProperty("targets").EnumerateArray(), item => item.GetProperty("unitId").GetString() == "de-s");
        Assert.Equal(["ffnam", "ffmo"], MovementDrm(fire));
        NoReplayErrors();
    }

    [Fact]
    public async Task AFailedLoadLeavesThePinnedSquadOnFoot()
    {
        // D5.43 (ruling R65.14): the 4-6-7 boards the truck; the Russians' 8 FP at it, FFNAM alone (no FFMO for a loader): Original DR 9, Final 8, a NMC;
        // its DR of 7 equals its Morale Level, so it is pinned (A7.8) and stays on foot in B8.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-truck", "attacker-truck", Target, "german", "east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"));
        await Advance();
        Committed(await Drive("de-truck", "load", "de-s"));
        Assert.Equal("de-truck", Current.Aboard("de-s"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 5, 3, 4, 6, 6, 6, 6), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        // A load writes no unloading flag; absent is false (the boundary review).
        Assert.False(fire.Facts.GetProperty("embarking").TryGetProperty("unloading", out _));
        Assert.Equal(["ffnam"], MovementDrm(fire));
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<InstanceMoved>(), moved => moved.Id == "de-s");
        var squad = Current.Unit("de-s")!;
        Assert.Equal((ConditionState.True, (string?)null, Target), (GameState.Condition(squad, Conditions.Pinned), Current.Aboard("de-s"), Current.Location("de-s")!.Location.ToString()));
        NoReplayErrors();
    }

    [Fact]
    public async Task ALoaderSparedByTheAttackThatDestroysItsTruckStaysOnFoot()
    {
        // D5.43 and its EX (p. 204): the 4-6-7 boards the truck; the Russians' 8 FP, Original DR 5: on the Vehicle line, below the Kill Number of 7, the
        // truck is eliminated; the squad, FFNAM alone, takes a 2MC on a Final 4 and passes it on a 3. The load fails with the vehicle destroyed, so the
        // squad is back on foot in B8 before the wreck is written, not lost with the truck.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-truck", "attacker-truck", Target, "german", "east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"));
        await Advance();
        Committed(await Drive("de-truck", "load", "de-s"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(2, 3, 1, 2, 6, 6, 6, 6), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var events = Since(before).Select(item => item.Payload).ToArray();
        Assert.Equal("eliminated", events.OfType<FireResolved>().Single().Resolution.GetProperty("vehicleEffects")[0].GetProperty("result").GetString());
        var moved = Array.FindIndex(events, payload => payload is InstanceMoved { Id: "de-s" });
        Assert.InRange(moved, 0, Array.FindIndex(events, payload => payload is VehicleWrecked) - 1);
        var squad = Current.Unit("de-s")!;
        Assert.Equal((InstanceStatus.Active, (string?)null, Target), (squad.Status, Current.Aboard("de-s"), Current.Location("de-s")!.Location.ToString()));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("de-truck")!.Status);
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadBoardsAtTheEndOfItsMove()
    {
        // D6.4 (ruling R65.15): the 4-6-7 enters B9 and B8 (two MF) and boards the unmoved truck there as its last step, once the DEFENDER passes.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-truck", "attacker-truck", Target, "german", "east"),
            Unit("de-s", "asl:squad", "attacker-squad", Firer, "german"), Unit("de-h", "asl:half-squad", "attacker-half-squad", Target, "german"),
            Unit("ru-s", "asl:squad", "defender-squad", "bd01:J2:0", "russian"));
        await Advance();
        foreach (var hex in new[] { "bd01:B9:0", Target })
        {
            Committed(await Do(GameActions.Move, NoRoll(), new
            {
                unitIds = SquadOnly,
                to = hex,
            }));
            Refused(await Drive("de-truck", "load", "de-s"), "the DEFENDER may still fire at the moving stack's last MF expenditure");
            await PassFire();
        }

        Refused(await Drive("de-truck", "load", "de-s", "de-h"), "de-h is not of the moving stack");
        var boarded = await Drive("de-truck", "load", "de-s");
        Committed(boarded);
        Assert.Contains(boarded.Reasons, reason => reason.Contains("the last step of its move", StringComparison.Ordinal));
        Assert.Equal("de-truck", Current.Aboard("de-s"));
        Assert.Equal((true, true), (Current.Unit("de-s")!.MovementEnded, Current.Movement is { Vehicle: true, Embarking: ["de-s"], Unloading: false }));
        NoReplayErrors();
    }

    [Fact]
    public async Task UnloadedUnitsMoveNextOnFourMfWithoutDoubleTime()
    {
        // D6.5 (ruling R65.15): once the truck's move ends, the squad it unloaded moves before any other unit, without Double Time (A4.5).
        await TruckInTheOpen(Unit("de-h", "asl:half-squad", "attacker-half-squad", "bd01:A9:0", "german"));
        Committed(await Drive("de-truck", "unload", "de-s"));
        await PassFire();
        await EndTheMove();
        var vehicleEnded = Revision;
        Assert.Equal(["de-s"], Current.Movement!.Members);
        Refused(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = HalfSquadOnly,
            to = "bd01:A10:0",
        }), "play.move-order");
        Refused(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = SquadOnly,
            to = "bd01:B9:0",
            doubleTime = true,
        }), "rode or dismounted a vehicle this Player Turn and does not Double Time (A4.5)");
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = SquadOnly,
            to = "bd01:B9:0",
        }));
        NoReplayErrors();

        // An unload recorded before the pass carries no interim fact: the truck's move ends with no stack held, so any unit moved next, as before.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Take((int)vehicleEnded)
            .Select(item => item.Payload is VehicleStepped { Interim: true } step ? item with { Payload = step with { Interim = false } } : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Null(replayed.Current!.Movement);
    }

    [Fact]
    public async Task AHalftrackKeepsTheLmgItsSquadLeavesAboard()
    {
        // D6.4's EXC (ruling R65.15): the 4-6-7 unloads from the CE SPW 251/1 and leaves its LMG aboard, held by no unit; a truck keeps none.
        var lmg = new Dictionary<string, object>
        {
            ["id"] = "de-lmg",
            ["kind"] = "asl:mg",
            ["definition"] = "attacker-lmg",
            ["side"] = "german",
            ["holding"] = new
            {
                holder = "de-s",
                role = "possessed"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        };
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Exposed(Vehicle("de-ht", "attacker-halftrack", Target, "german", "east")),
            Aboard("de-s", "asl:squad", "attacker-squad", "de-ht", "german"), lmg, Unit("ru-s", "asl:squad", "defender-squad", "bd01:J2:0", "russian"));
        await Advance();
        var unloaded = await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-ht",
            kind = "unload",
            units = SquadOnly,
            leave = LmgOnly,
        });
        Committed(unloaded);
        var weapon = (EquipmentInstance)Current.Find("de-lmg")!;
        Assert.Equal(((Holding?)null, "de-ht", (string?)null), (weapon.Holding, Current.Aboard("de-lmg"), Current.Aboard("de-s")));
        NoReplayErrors();
    }

    [Fact]
    public void TheMoveVehicleDescriptorTakesALoadAndAnUnload()
    {
        // The descriptor (additionalProperties false) lists every kind and argument the planner takes, so a load names its units within the schema.
        var schema = GameActions.MoveVehicle.InputSchema;
        var properties = schema.GetProperty("properties");
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        string[] kinds = [.. properties.GetProperty("kind").GetProperty("enum").EnumerateArray().Select(item => item.GetString()!)];
        Assert.Contains("load", kinds);
        Assert.Contains("unload", kinds);
        Assert.Contains("delay", kinds);
        var load = JsonSerializer.SerializeToElement(new
        {
            gameId = "g",
            attemptId = "a",
            expectedRevision = 0,
            vehicleId = "de-truck",
            kind = "load",
            units = SquadOnly,
            turret = "east"
        });
        Assert.All(load.EnumerateObject(), item => Assert.True(properties.TryGetProperty(item.Name, out _), item.Name));
        Assert.True(properties.TryGetProperty("leave", out _));
        Assert.True(properties.TryGetProperty("take", out _));
    }

    // Increment 2e: Riders and Bailing Out (D6.2, D6.21 to D6.24, B13.43; rulings R65.16, R65.17).

    private static readonly string[] RussianRiders = ["ru-s", "ru-l"];
    private static readonly string[] RussianSquad = ["ru-s"];
    private static readonly string[] RussianSquads = ["ru-1", "ru-2"];

    /// <summary>A unit riding an AFV as a Rider (D6.2).</summary>
    private static Dictionary<string, object> Rider(string id, string kind, string definition, string vehicle, string side)
    {
        var unit = Unit(id, kind, definition, Target, side);
        unit["position"] = new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "rider" };
        return unit;
    }

    /// <summary>A SW possessed by a unit.</summary>
    private static Dictionary<string, object> Weapon(string id, string definition, string side, string holder) => new()
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

    private Task<PlayResult> Board(string vehicle, bool riders, params string[] units) => Do(GameActions.MoveVehicle, NoRoll(), new
    {
        vehicleId = vehicle,
        kind = "load",
        units,
        riders,
    });

    [Fact]
    public async Task RidersBoardAT34In1943AndReplay()
    {
        // D6.2 (ruling R65.16): a 4-4-7 and a 7-0 board the T-34 M41 as Riders (10 PP of its 14); the T-34 takes no Passengers. The load records riders.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Unit("ru-s", "asl:squad", "defender-squad", Firer, "russian"), Unit("ru-l", "asl:leader", "defender-leader-7-0", Firer, "russian"),
            Unit("de-s", "asl:squad", "attacker-squad", "bd01:J2:0", "german"));
        await Advance();
        Refused(await Board("ru-tank", false, "ru-s", "ru-l"), "carries no Passengers");
        var boarded = await Board("ru-tank", true, "ru-s", "ru-l");
        Committed(boarded);
        Assert.Contains(boarded.Reasons, reason => reason.Contains("as Riders (D6.2)", StringComparison.Ordinal));
        Assert.True(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<VehicleStepped>().Last().Riders);
        Assert.All(RussianRiders, id => Assert.Equal(new ContainedPosition("ru-tank", ContainmentRole.Rider), Current.Unit(id)!.Position));
        Assert.Equal(4, Planner().RiderPpLeft(Current, Current.Unit("ru-tank")!));
        NoReplayErrors();
    }

    [Fact]
    public async Task AGermanRiderIsRefusedIn1942()
    {
        // D6.2: only the Russians use Riders in 1942.
        await Setup("german", "russian", 1942, 7, "asl-scenario-a1@1.18.0", Vehicle("de-tank", "attacker-tank", Target, "german", "east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Unit("ru-s", "asl:squad", "defender-squad", "bd01:J2:0", "russian"));
        await Advance();
        Refused(await Board("de-tank", true, "de-s"), "Riders are allowed only from 1942 for the Russians and from 1943 for every nationality");
    }

    [Fact]
    public async Task ARiderBailsOutAsItsTankEntersWoods()
    {
        // D6.21, D6.24 (ruling R65.17): the PzKpfw IIIH starts at B10 and enters B9, read as woods; its Rider, a 4-6-7 with a LMG, Bails Out in B10 first:
        // its NMC (2, 3) passes, the LMG's dr of 5 malfunctions it; it is on foot in B10, its move ended, under a Prep Fire counter. The tank then takes its
        // Bog DR in the woods (1, 1). The Russian 4-4-7 at B8 fires at B10 with FFNAM, as at an unloader (D6.5), and no FFMO.
        changed = ("B9", "Woods");
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-tank", "attacker-tank", Firer, "german", "north-east"),
            Rider("de-s", "asl:squad", "attacker-squad", "de-tank", "german"), Weapon("de-lmg", "attacker-lmg", "german", "de-s"),
            Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"));
        await Advance();
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-tank",
            kind = "start",
        }));
        await PassFire();
        var before = Revision;
        var entered = await Do(GameActions.MoveVehicle, Once(2, 3, 5, 1, 1), new
        {
            vehicleId = "de-tank",
            kind = "enter",
            to = "bd01:B9:0",
        });
        Committed(entered);
        var bailed = Assert.Single(Since(before).Select(item => item.Payload).OfType<RiderBailedOut>());
        Assert.Equal(("de-s", ScenarioA1RiderRules.Terrain, Firer, ScenarioA1RiderRules.Passed), (bailed.Unit, bailed.Cause, bailed.At.ToString(), bailed.Result));
        Assert.Equal(ScenarioA1RiderRules.Malfunctioned, Assert.Single(bailed.Weapons!).Fate);
        var squad = Current.Unit("de-s")!;
        Assert.Equal((Firer, true, ConditionState.True), (Current.Location("de-s")!.Location.ToString(), squad.MovementEnded, GameState.Condition(squad, Conditions.PrepFire)));
        Assert.Equal(ConditionState.True, GameState.Condition((EquipmentInstance)Current.Find("de-lmg")!, Conditions.Malfunctioned));
        Assert.Equal(SquadOnly, Current.Movement!.BailedOut);
        NoReplayErrors();

        var fired = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 6), new
        {
            firers = RussianSquad,
            target = Firer,
        }));
        var fire = Since(fired).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(["ffnam"], MovementDrm(fire));
        NoReplayErrors();
    }

    [Fact]
    public async Task APinnedRiderBailsOutAndChecksItsSw()
    {
        // D6.23, D6.24 (ruling R65.17): two Russian 4-4-7s fire 8 FP at the BU PzKpfw IIIH two hexes off, an Original DR of 8; its Rider, always
        // Vulnerable with no CE DRM, takes the NMC (3, 5: no Cowering) and is pinned on a 7 (3, 4). It Bails Out beneath the tank: its NMC (1, 1) passes and the LMG's dr of 2
        // keeps it; in the Russian Player Turn it is marked with a Final Fire counter.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"), Vehicle("de-tank", "attacker-tank", Target, "german", "east"),
            Rider("de-s", "asl:squad", "attacker-squad", "de-tank", "german"), Weapon("de-lmg", "attacker-lmg", "german", "de-s"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(3, 5, 3, 4, 1, 1, 2), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var bailed = Assert.Single(Since(before).Select(item => item.Payload).OfType<RiderBailedOut>());
        Assert.Equal((ScenarioA1RiderRules.Pin, Target, ScenarioA1RiderRules.Passed), (bailed.Cause, bailed.At.ToString(), bailed.Result));
        Assert.Equal(ScenarioA1RiderRules.Kept, Assert.Single(bailed.Weapons!).Fate);
        var squad = Current.Unit("de-s")!;
        Assert.Equal(((string?)null, ConditionState.True, ConditionState.True), (Current.Aboard("de-s"), GameState.Condition(squad, Conditions.Pinned),
            GameState.Condition(squad, Conditions.FinalFire)));
        NoReplayErrors();
    }

    // Increment 2f: Carriers (D6.8 to D6.84, A26.213; ruling R65.18).

    private static readonly string[] GermanSquads = ["de-1", "de-2"];

    [Fact]
    public async Task AKilledBrenCarriersSurvivingCrewIsABritishHalfSquad()
    {
        // D6.82, D5.6: the PzKpfw IIIH's AP eliminates the Bren carrier; a Crew Survival DR of 4, within its CS# 5, places the British 2-4-7 HS beneath
        // the wreck, not a vehicle crew, with the id {attempt}-{vehicle}-crew. It is a HS, one VP (A26.211).
        await Setup("german", "british", 1944, 7, "asl-scenario-a1@1.18.0", Vehicle("de-tank", "attacker-tank", Firer, "german", "north-east"),
            Vehicle("uk-bren", "campaign-british-bren-carrier", Target, "british", "east"));
        var before = Revision;
        Committed(await Shoot("de-tank", "uk-bren", "ap", 4, 2, 4, 4, 2, 2));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.True(Current.Unit("uk-bren")!.Status == InstanceStatus.Wrecked, kill.GetRawText());
        var crew = Assert.Single(Current.Units, unit => unit.Side == "british" && unit.Status == InstanceStatus.Active && unit.Kind != "asl:vehicle");
        Assert.Equal(("asl:half-squad", "british-half-squad", Target, true), (crew.Kind, crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(),
            crew.Id.EndsWith("-uk-bren-crew", StringComparison.Ordinal)));
        Assert.Equal(1, Planner().VictoryPoints(Current, crew));
        NoReplayErrors();
    }

    [Fact]
    public async Task ABrenCarrierIsNeverButtonedUpAndItsStunnedCrewStaysExposed()
    {
        // D6.84: a Carrier sets up CE and its owner may not button it up; the page offers it no BU toggle.
        var buttoned = Vehicle("uk-bren", "campaign-british-bren-carrier", Target, "british", "east");
        buttoned["conditions"] = new Dictionary<string, bool> { ["asl:bu"] = true };
        Refused(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-bu",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass65Tests),
                ["catalog"] = "asl-scenario-a1@1.18.0",
                ["boards"] = Bd01,
                ["firstSide"] = "british",
                ["sides"] = new object[]
                {
                    new { id = "british", nationality = "british", elr = 3, friendlyEdge = "left" },
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "right" },
                },
            },
            placements = new[] { buttoned },
        })), "play.setup-carrier");

        await Setup("british", "german", 1944, 7, "asl-scenario-a1@1.18.0", Vehicle("uk-bren", "campaign-british-bren-carrier", Target, "british", "east"),
            Unit("de-1", "asl:squad", "attacker-squad", Firer, "german"));
        await Advance();
        Refused(await Do(GameActions.ButtonUp, NoRoll(), new
        {
            vehicleId = "uk-bren",
            buttonedUp = true,
        }), "uk-bren is a Carrier, which is never BU");
        var bren = Current.Unit("uk-bren")!;
        Assert.False(GamePlanner.MayChangeExposure(bren));
        Assert.True(LiveFire.CrewExposed(bren));
        Assert.True(LiveFire.CrewExposed(bren with
        {
            Conditions = new Dictionary<string, ConditionState>(bren.Conditions) { [Conditions.Stunned] = ConditionState.True, [Conditions.ButtonedUp] = ConditionState.True }
        }));
        NoReplayErrors();
    }

    [Fact]
    public async Task AStunnedBrenCarriersCrewTakesTheCollateralAttack()
    {
        // D6.84, D.8B: two German 4-6-7s fire 8 FP at the stunned Bren carrier two hexes off, an Original DR of 6. Its crew, CE though stunned, takes the
        // attack on the 8 column at a Final DR of 8 (+2 for CE), a NMC; the check (6, 6) fails, a second Stun, so it is Recalled (D5.342).
        var stunned = Vehicle("uk-bren", "campaign-british-bren-carrier", Target, "british", "east");
        stunned["conditions"] = new Dictionary<string, bool> { ["asl:stunned"] = true };
        await Setup("german", "british", 1944, 7, "asl-scenario-a1@1.18.0", Unit("de-1", "asl:squad", "attacker-squad", Firer, "german"),
            Unit("de-2", "asl:squad", "attacker-squad", Firer, "german"), stunned);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(2, 4, 6, 6), new
        {
            firers = GermanSquads,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var vehicle = fire.Facts.GetProperty("vehicles")[0];
        Assert.Equal((true, true, 0), (vehicle.GetProperty("crewExposed").GetBoolean(), vehicle.GetProperty("stunned").GetBoolean(), vehicle.GetProperty("prc").GetArrayLength()));
        var effect = fire.Resolution.GetProperty("vehicleEffects")[0];
        Assert.Equal(8, effect.GetProperty("finalDr").GetInt32());
        Assert.Equal("recalled", effect.GetProperty("crewResult").GetString());
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("uk-bren")!, Conditions.Recalled));
        NoReplayErrors();
    }

    [Fact]
    public async Task ABrenCarriersInherentHalfSquadUnloadsAndAbandonsIt()
    {
        // D6.83: in the British MPh the Bren carrier's inherent HS unloads as a Passenger does, for a quarter of its MP; it is placed beneath the Carrier
        // as the British 2-4-7 HS, and the Carrier is Abandoned. The Carrier's VP fall from 5 to 3, the HS's own one VP aside (A26.211, A26.213).
        await Setup("british", "german", 1944, 7, "asl-scenario-a1@1.18.0", Vehicle("uk-bren", "campaign-british-bren-carrier", Target, "british", "east"),
            Vehicle("uk-tank", "campaign-british-cromwell", Firer, "british", "east"), Unit("de-1", "asl:squad", "attacker-squad", "bd01:J2:0", "german"));
        Assert.Equal(5, Planner().VictoryPoints(Current, Current.Unit("uk-bren")!));
        await Advance();
        Assert.True(GamePlanner.CrewMayUnload(Current, Current.Unit("uk-bren")!));
        Refused(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "uk-tank",
            kind = "unload",
            crew = true,
        }), "only a Carrier's inherent HS does (D6.83)");

        var before = Revision;
        var unloaded = await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "uk-bren",
            kind = "unload",
            crew = true,
        });
        Committed(unloaded);
        Assert.Contains(unloaded.Reasons, reason => reason.Contains("so uk-bren is Abandoned", StringComparison.Ordinal));
        var halfSquad = Assert.Single(Since(before).Select(item => item.Payload).OfType<InstanceCreated>()).Instance;
        var step = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleStepped>());
        Assert.Equal(halfSquad.Id, Assert.Single(step.Units!));
        Assert.True(step.Interim);
        var unit = Current.Unit(halfSquad.Id)!;
        Assert.Equal(("asl:half-squad", "british-half-squad", Target, (string?)null), (unit.Kind, unit.Definition!.Definition, Current.Location(unit.Id)!.Location.ToString(),
            Current.Aboard(unit.Id)));
        var bren = Current.Unit("uk-bren")!;
        Assert.Equal((ConditionState.True, false), (GameState.Condition(bren, Conditions.Abandoned), GamePlanner.CrewMayUnload(Current, bren)));
        Assert.Equal((3, 1), (Planner().VictoryPoints(Current, bren), Planner().VictoryPoints(Current, unit)));
        NoReplayErrors();
    }

    // Increment 2g: the crew's own acts (D5.41, D5.411, D5.42, D5.43, D5.8, D6.631, D3.43; ruling R65.20).

    private const string Behind = "bd01:B9:0";
    private const string Far = "bd01:J2:0";

    /// <summary>A vehicle with a 9-1 Armor Leader (D3.4).</summary>
    private static Dictionary<string, object> Led(Dictionary<string, object> vehicle)
    {
        vehicle["armorLeader"] = new
        {
            morale = 9,
            leadership = -1
        };
        return vehicle;
    }

    private Task<PlayResult> Abandon(string vehicle, string? destroy = null, bool removeMg = false)
    {
        var arguments = new Dictionary<string, object> { ["vehicleId"] = vehicle };
        if (destroy is not null)
        {
            arguments["destroy"] = destroy;
        }

        if (removeMg)
        {
            arguments["removeMg"] = true;
        }

        return Do(GameActions.AbandonVehicle, NoRoll(), arguments);
    }

    private Task<PlayResult> Recrew(string unit, string vehicle) => Do(GameActions.RecrewVehicle, NoRoll(), new
    {
        unitId = unit,
        vehicleId = vehicle,
    });

    /// <summary>The phases advanced to the German MPh of Game Turn 2.</summary>
    private async Task NextGermanMph()
    {
        for (var step = 0; step < 40 && (Current.PhasingSide, Current.Phase, Current.Turn) != ("german", "mph", 2); step++)
        {
            await Advance();
        }

        Assert.Equal(("german", "mph", 2), (Current.PhasingSide, Current.Phase, Current.Turn));
    }

    [Fact]
    public async Task AVoluntaryAbandonmentMalfunctionsOrWrecksTheVehicleAsTheCrewLeaves()
    {
        // D5.41, D5.411: in the German MPh the PzKpfw IIIH's crew Abandons it, malfunctioning its weapons; it is placed beneath the tank as the German vehicle
        // crew with its MF spent, carrying the 9-1 Armor Leader dormant (D3.43), and the DEFENDER's window opens on it (D5.43). A second tank's crew leaves it a
        // non-burning wreck with no CS roll. A tank that Prep Fired may not be Abandoned this Player Turn.
        var fired = Vehicle("de-tank3", "attacker-tank", Firer, "german", "east");
        fired["conditions"] = new Dictionary<string, bool> { ["asl:prep-fire"] = true };
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Led(Vehicle("de-tank", "attacker-tank", Target, "german", "east")),
            Vehicle("de-tank2", "attacker-tank", Behind, "german", "east"), fired, Unit("ru-1", "asl:squad", "defender-squad", Far, "russian"));
        await Advance();
        Refused(await Abandon("de-tank3"), "has fired this Player Turn");

        var before = Revision;
        var abandoned = await Abandon("de-tank", ScenarioA1CrewActsRules.Malfunction);
        Committed(abandoned);
        Assert.Contains(abandoned.Reasons, reason => reason.Contains("malfunctions de-tank's MA and every MG", StringComparison.Ordinal));
        var record = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleAbandoned>());
        var crew = Current.Unit(record.Crew)!;
        Assert.Equal(("asl:vehicle-crew", "german-vehicle-crew", Target, true), (crew.Kind, crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(),
            crew.MovementEnded));
        Assert.Equal(new ArmorLeader(9, -1), crew.ArmorLeader);
        var tank = Current.Unit("de-tank")!;
        Assert.Equal((ConditionState.True, ConditionState.True, (ArmorLeader?)null), (GameState.Condition(tank, Conditions.Abandoned),
            GameState.Condition(tank, Conditions.Malfunctioned), tank.ArmorLeader));
        Assert.True(Current.Movement is { WindowOpen: true, Embarking: [var leaving] } && leaving == crew.Id);
        await EndTheMove();

        Committed(await Abandon("de-tank2", ScenarioA1CrewActsRules.Wreck));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-tank2")!.Status);
        Assert.Single(Current.Units, unit => unit.Status == InstanceStatus.Active && unit.Id.EndsWith("-de-tank2-crew", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task ACrewReCrewsItsTankAPlayerTurnLaterAndItsArmorLeaderReturns()
    {
        // D5.42: a German squad in the tank's Location may not enter it in the Player Turn its crew Abandoned it. In the next German MPh the crew enters it,
        // keeping its id, in the crew's role, not a Passenger and not in the Location's stack; the tank is manned again and its 9-1 is back (D3.43).
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Led(Vehicle("de-tank", "attacker-tank", Target, "german", "east")),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Unit("ru-1", "asl:squad", "defender-squad", Far, "russian"));
        await Advance();
        Committed(await Abandon("de-tank"));
        var crew = Current.Units.Single(unit => unit.Id.EndsWith("-de-tank-crew", StringComparison.Ordinal)).Id;
        await EndTheMove();
        Refused(await Recrew("de-s", "de-tank"), "left it this Player Turn");

        await NextGermanMph();
        var before = Revision;
        Committed(await Recrew(crew, "de-tank"));
        Assert.False(Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleRecrewed>()).Temporary);
        var tank = Current.Unit("de-tank")!;
        Assert.Equal((ConditionState.False, new ArmorLeader(9, -1)), (GameState.Condition(tank, Conditions.Abandoned), tank.ArmorLeader));
        var inside = Current.Unit(crew)!;
        Assert.Equal((ContainmentRole.Crew, (ArmorLeader?)null, "de-tank"), ((inside.Position as ContainedPosition)?.Role, inside.ArmorLeader, Current.Aboard(crew)));
        Assert.DoesNotContain(Current.At(Current.Location("de-tank")!.Location), item => item.Id == crew);
        Assert.Empty(Current.Passengers("de-tank"));
        Assert.Equal(crew, Planner().CrewInsideOf(Current, "de-tank")?.Id);
        NoReplayErrors();
    }

    /// <summary>The events renumbered from revision 1, as a store holds them.</summary>
    private static GameEvent[] Renumbered(IEnumerable<GameEvent> events) => [.. events.Select((item, index) => item with { Revision = index + 1 })];

    /// <summary>A record written by hand after the game's events, for the projector to read.</summary>
    private static GameEvent Written(int revision, string type, EventPayload payload) =>
        new(Scope, $"written-{revision}", revision, DateTimeOffset.UnixEpoch, LiveGames.Source, type, payload, null, [], null);

    [Fact]
    public async Task TheProjectorRefusesAReCrewingInThePlayerTurnTheCrewLeftAndAnAbandonmentAfterFire()
    {
        // Fix Round C (the boundary review): the projector reads each crew act by the planner's bar. A re-crewing written in the Player Turn the tank's crew
        // left it (D5.42), which the planner refuses, is refused at replay (UNIT-STATE-034), the state holding the tank among those Abandoned this Player
        // Turn; so is the same Abandonment written after the tank Prep Fired (D5.41).
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Led(Vehicle("de-tank", "attacker-tank", Target, "german", "east")),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german"), Unit("ru-1", "asl:squad", "defender-squad", Far, "russian"));
        await Advance();
        var before = Revision;
        Committed(await Abandon("de-tank"));
        await EndTheMove();
        Assert.Equal(["de-tank"], Current.AbandonedThisPlayerTurn);
        Refused(await Recrew("de-s", "de-tank"), "left it this Player Turn");

        var events = store.Read(Scope)!.Events;
        var recrewed = Planner().Replay([.. events, Written(events.Count + 1, "vehicle-recrewed", new VehicleRecrewed("de-tank", BoardLocation.Parse(Target), "de-s"))]);
        Assert.Contains(recrewed.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-034");

        var fired = new ConditionsChanged("de-tank", new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.PrepFire] = ConditionState.True });
        var afterFire = Planner().Replay(Renumbered([.. events.Where(item => item.Revision <= before), Written(0, "conditions-changed", fired),
            .. events.Where(item => item.Revision > before)]));
        Assert.Contains(afterFire.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-034");
        NoReplayErrors();
    }

    [Fact]
    public async Task AHalftracksMgIsRemovedAsItsCrewAbandonsItAndReturnedWhenItReCrews()
    {
        // D6.631: the SPW 251/1's crew Abandons it, Removing its 3 FP AAMG MA as the German LMG it holds; the halftrack is Disabled. A Player Turn later the
        // crew re-enters with the LMG, which goes back, and the halftrack is no longer Disabled.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-ht", "attacker-halftrack", Target, "german", "east"),
            Unit("ru-1", "asl:squad", "defender-squad", Far, "russian"));
        await Advance();
        var before = Revision;
        Committed(await Abandon("de-ht", removeMg: true));
        var record = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleAbandoned>());
        Assert.Equal(ScenarioA1CrewActsRules.MainArmament, record.Mount);
        var lmg = (EquipmentInstance)Current.Find(record.Removed!)!;
        Assert.Equal(("attacker-lmg", record.Crew), (lmg.Definition!.Definition, lmg.Holding!.Holder));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("de-ht")!, Conditions.Disabled));
        await EndTheMove();

        await NextGermanMph();
        before = Revision;
        Committed(await Recrew(record.Crew, "de-ht"));
        Assert.Equal(record.Removed, Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleRecrewed>()).Returned);
        Assert.Equal(InstanceStatus.Consumed, Current.Find(record.Removed!)!.Status);
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("de-ht")!, Conditions.Disabled));
        NoReplayErrors();
    }

    // Increment 2h: the Immobilization TC's causes, Crew Survival, Brew Up, and Hazardous Movement for survivors (D5.5, D5.6, D9.2; ruling R65.21).

    private static readonly string[] RiflemenR1 = ["r1"];
    private const string FiveHexesOff = "bd01:B3:0";

    /// <summary>A light mortar possessed by its holder.</summary>
    private static Dictionary<string, object> LightMortar(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:light-mortar",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A mortar's shot on the Area Target Type at the target Location.</summary>
    private Task<PlayResult> Lob(string mortar, params int[] dice) =>
        Do(GameActions.FireOrdnance, Once(dice), new Dictionary<string, object> { ["gunId"] = mortar, ["target"] = Target });

    /// <summary>
    /// The game's records as a game recorded before increment 2h holds them (ruling R65.21): no PRC, Shock, or Abandonment fact on a vehicle, no TC or Crew
    /// Survival in a mortar's or the Vehicle line's result, no roll of either, and neither the crew it placed nor a Hazardous Movement.
    /// </summary>
    private GameEvent[] BeforeIncrement2h()
    {
        // The names each list's items lose: a vehicle's facts, a mortar's AFV result, and the Vehicle line's result.
        static void Remove(JsonNode? node, Dictionary<string, string[]> names, string[] own)
        {
            if (node is JsonObject item)
            {
                foreach (var name in own)
                {
                    item.Remove(name);
                }

                foreach (var (key, child) in item.ToArray())
                {
                    if (child is JsonArray list)
                    {
                        foreach (var element in list)
                        {
                            Remove(element, names, names.GetValueOrDefault(key) ?? []);
                        }
                    }
                    else
                    {
                        Remove(child, names, []);
                    }
                }
            }
        }

        static JsonElement Strip(JsonElement element, Dictionary<string, string[]> names)
        {
            var node = JsonNode.Parse(element.GetRawText());
            Remove(node, names, []);
            return JsonSerializer.SerializeToElement(node);
        }

        var facts = new Dictionary<string, string[]>(StringComparer.Ordinal) { ["vehicles"] = ["prc", "shocked", "abandoned"] };
        var results = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["areaVehicles"] = ["crewCheck", "abandoned", "prcSurvival"],
            ["vehicleEffects"] = ["taskCheck", "abandoned", "crewSurvival"],
        };
        static bool Added(string key) => key.StartsWith("areaCrewCheck:", StringComparison.Ordinal) || key.Contains("vehicleSurvival:", StringComparison.Ordinal);
        static Dictionary<string, string> Kept(IReadOnlyDictionary<string, string> rolls) =>
            rolls.Where(item => !Added(item.Key)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        return [.. store.Read(Scope)!.Events
            .Where(item => item.Payload is not InstanceCreated { Instance.Kind: "asl:vehicle-crew" }
                && !(item.Payload is ConditionsChanged changed && (changed.Conditions.ContainsKey(Conditions.HazardousMovement) || changed.Conditions.ContainsKey(Conditions.Abandoned))))
            .Select((item, index) => item with { Revision = index + 1 })
            .Select(item => item.Payload is OrdnanceFired shot
                ? item with { Payload = shot with { Rolls = Kept(shot.Rolls), Facts = Strip(shot.Facts, facts), Resolution = Strip(shot.Resolution, results) } }
                : item)];
    }

    [Fact]
    public async Task AMortarImmobilizesATankAndItsCrewFailsTheTcUnderHazardousMovement()
    {
        // C1.55, D5.5: the German 50mm at the T-34 two hexes off, a To Hit DR of 6; an IFT DR of 3 on the 2 column, one above its K/# DR, its colored 2 not
        // below its white 1: a hull hit that immobilizes it. Its crew, inside, takes the TC on a DR of 12 and fails: the T-34 is Abandoned and the Russian
        // vehicle crew is beneath it, under Hazardous Movement until the phase ends (D5.5).
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Unit("g1", "asl:squad", "attacker-squad", Firer, "german"),
            LightMortar("de-mtr", "attacker-light-mortar", "g1", "german"), Vehicle("ru-tank", "defender-tank", Target, "russian", "south-west"));
        var before = Revision;
        Committed(await Lob("de-mtr", 4, 2, 2, 1, 6, 6, 3, 3, 3, 3));
        var shot = LastShot(before);
        var tank = shot.Resolution.GetProperty("areaVehicles").EnumerateArray().Single();
        Assert.Equal(("hull", "immobilized"), (tank.GetProperty("hitLocation").GetString(), tank.GetProperty("result").GetString()));
        var tc = tank.GetProperty("crewCheck");
        Assert.Equal(("TC", 12, false), (tc.GetProperty("kind").GetString(), tc.GetProperty("finalDr").GetInt32(), tc.GetProperty("passed").GetBoolean()));
        Assert.True(tank.GetProperty("abandoned").GetBoolean());
        Assert.Contains("areaCrewCheck:ru-tank", shot.Rolls.Keys);
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("ru-tank")!, Conditions.Abandoned));
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:vehicle-crew");
        Assert.Equal(("russian-vehicle-crew", Target, ConditionState.True),
            (crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(), GameState.Condition(crew, Conditions.HazardousMovement)));
        NoReplayErrors();

        // A game recorded before the pass: the hit's facts carry no PRC, so no TC is taken and the shot replays as it was resolved.
        var older = Planner().Replay(BeforeIncrement2h());
        Assert.False(older.HasErrors, string.Join("; ", older.Diagnostics.Select(item => item.Message)));
        Assert.Equal(ConditionState.True, GameState.Condition(older.Current!.Unit("ru-tank")!, Conditions.Immobilized));
        Assert.NotEqual(ConditionState.True, GameState.Condition(older.Current.Unit("ru-tank")!, Conditions.Abandoned));
        Assert.DoesNotContain(older.Current.Units, unit => unit.Kind == "asl:vehicle-crew");

        // The phase's end removes the Hazardous Movement.
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit(crew.Id)!, Conditions.HazardousMovement));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAirBurstWrecksAShockedHalftrackAndItsCrewSurvivesWithThePlusOne()
    {
        // D5.311, A7.308 (pass 60): the Russian 50mm hits the OT SPW 251/1 in woods, attacked as unarmored on the Vehicle line: an IFT DR of 3 is below the 2
        // column's Kill Number of 4 and more than half of it, a wreck. D5.6, D9.2 (ruling R65.21): the armed halftrack's crew rolls its Crew Survival DR, +1 as
        // it was Shocked: 4 + 1 = 5 against its CS# 5, so the German vehicle crew is beneath the wreck, under Hazardous Movement.
        changed = ("B8", "Woods");
        var halftrack = Vehicle("de-ht", "attacker-halftrack", Target, "german", "east");
        halftrack["conditions"] = new Dictionary<string, bool> { ["asl:shocked"] = true };
        await SetupShocked("russian", "german", "de-ht", Unit("r1", "asl:squad", "defender-squad", FiveHexesOff, "russian"),
            LightMortar("ru-mtr", "defender-light-mortar", "r1", "russian"), halftrack);
        var before = Revision;
        Committed(await Lob("ru-mtr", 2, 2, 1, 2, 2, 2, 3, 3, 3, 3));
        var shot = LastShot(before);
        Assert.True(shot.Facts.GetProperty("hit").GetProperty("vehicles")[0].GetProperty("shocked").GetBoolean());
        var line = shot.Resolution.GetProperty("hit").GetProperty("vehicleEffects").EnumerateArray().Single();
        Assert.Equal(("eliminated", 4), (line.GetProperty("result").GetString(), line.GetProperty("killNumber").GetInt32()));
        var survival = line.GetProperty("crewSurvival");
        Assert.Equal((1, 5, true), (survival.GetProperty("drm").GetInt32(), survival.GetProperty("finalDr").GetInt32(), survival.GetProperty("survived").GetBoolean()));
        Assert.Contains("hit:vehicleSurvival:de-ht", shot.Rolls.Keys);
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-ht")!.Status);
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:vehicle-crew");
        Assert.Equal(("german-vehicle-crew", ConditionState.True), (crew.Definition!.Definition, GameState.Condition(crew, Conditions.HazardousMovement)));
        NoReplayErrors();

        // A game recorded before the pass: no Crew Survival on the Vehicle line, as it was resolved.
        var older = Planner().Replay(BeforeIncrement2h());
        Assert.False(older.HasErrors, string.Join("; ", older.Diagnostics.Select(item => item.Message)));
        Assert.Equal(InstanceStatus.Wrecked, older.Current!.Unit("de-ht")!.Status);
        Assert.DoesNotContain(older.Current.Units, unit => unit.Kind == "asl:vehicle-crew");
    }

    [Fact]
    public async Task AShockedTankKilledByAnAtShotRollsItsCrewSurvivalAtPlusOne()
    {
        // D5.6 (ruling R65.21): the T-34's AP eliminates the PzKpfw IIIH, Shocked and then an Unconfirmed Kill, on a TK DR of 6; its Crew Survival DR of 4
        // takes the +1 for the Shock, 5 against its CS# 6, and the crew is beneath the wreck, under Hazardous Movement for the phase.
        var tank = Vehicle("de-tank", "attacker-tank", Target, "german", "east");
        tank["conditions"] = new Dictionary<string, bool> { ["asl:shocked"] = true };
        await SetupShocked("russian", "german", "de-tank", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"), tank);
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-tank", "ap", 4, 2, 3, 3, 2, 2));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.Equal("eliminated", kill.GetProperty("result").GetString());
        var survival = kill.GetProperty("crewSurvival");
        Assert.Equal((1, 5, true), (survival.GetProperty("drm").GetInt32(), survival.GetProperty("finalDr").GetInt32(), survival.GetProperty("survived").GetBoolean()));
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:vehicle-crew");
        Assert.Equal(ConditionState.True, GameState.Condition(crew, Conditions.HazardousMovement));
        NoReplayErrors();
    }

    [Fact]
    public async Task ASurvivingRiderIsUnderHazardousMovementAgainstTheNextAttackOfThePhase()
    {
        // D5.6, D6.9 (ruling R65.21): the T-34's AP eliminates the PzKpfw IIIH on a TK DR of 6; its crew's Crew Survival DR of 12 fails, and its Rider, a
        // 4-6-7, survives on a 4 against the CS# 6, beneath the wreck. The Russian 4-4-7's fire at it in the same PFPh takes Hazardous Movement's -2 (A4.62).
        var rider = Aboard("de-s", "asl:squad", "attacker-squad", "de-tank", "german");
        rider["position"] = new Dictionary<string, string> { ["in"] = "de-tank", ["role"] = "rider" };
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Unit("r1", "asl:squad", "defender-squad", Firer, "russian"), Vehicle("de-tank", "attacker-tank", Target, "german", "east"), rider);
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-tank", "ap", 4, 2, 3, 3, 6, 6, 2, 2));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.False(kill.GetProperty("crewSurvival").GetProperty("survived").GetBoolean());
        Assert.True(kill.GetProperty("prcSurvival")[0].GetProperty("survived").GetBoolean());
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("de-s")!, Conditions.HazardousMovement));
        Assert.DoesNotContain(Current.Units, unit => unit.Kind == "asl:vehicle-crew");

        before = Revision;
        Committed(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = RiflemenR1,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.True(fire.Facts.GetProperty("hazardousMovement").GetBoolean());
        Assert.Contains(fire.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "hazardous-movement" && item.GetProperty("value").GetDecimal() == -2);
        NoReplayErrors();
    }

    // Increment 2i: captured vehicles (A21.2, A21.21, A21.22, D5.343, A26.12; ruling R65.22).

    private void Inject(params (string Type, EventPayload Payload)[] items)
    {
        var revision = Revision;
        GameEvent[] events = [.. items.Select((item, index) => new GameEvent(Scope, $"inject-{revision}-{index + 1}", revision + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, [], null))];
        var result = store.Append(Scope, "inject", revision, events, Planner().Replay);
        Assert.True(result.Status == AppendStatus.Committed, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
    }

    /// <summary>The phases advanced to the named side's MPh of the named Game Turn.</summary>
    private async Task NextMph(string side, int turn)
    {
        for (var step = 0; step < 60 && (Current.PhasingSide, Current.Phase, Current.Turn) != (side, "mph", turn); step++)
        {
            await Advance();
        }

        Assert.Equal((side, "mph", turn), (Current.PhasingSide, Current.Phase, Current.Turn));
    }

    /// <summary>
    /// A Russian game of 1943 in which an Abandoned PzKpfw IIIH shares its Location with a Russian 4-4-7, a Russian infantry crew, and a 6+1 leader, and no
    /// German unit: as the Russian CCPh ends the tank is captured (A21.2), and the game goes on to the Russian MPh of Game Turn 2.
    /// </summary>
    private async Task<long> CaptureThePanzer()
    {
        var tank = Vehicle("de-tank", "attacker-tank", Target, "german", "east");
        tank["conditions"] = new Dictionary<string, bool> { ["asl:abandoned"] = true };
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", tank, Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian"),
            Unit("ru-c", "asl:crew", "defender-crew", Behind, "russian"), Unit("ru-l", "asl:leader", "defender-leader", Behind, "russian"),
            Unit("de-1", "asl:squad", "attacker-squad", Far, "german"));
        await AdvanceInto(RussiansAtB9);
        for (var step = 0; step < 4 && Current.Phase != "ccph"; step++)
        {
            await Advance();
        }

        var before = Revision;
        await Advance();
        return before;
    }

    private static readonly string[] RussiansAtB9 = ["ru-s", "ru-c", "ru-l"];
    private static readonly string[] RussianSquadAtB9 = ["ru-s"];

    /// <summary>The game advanced to the first side's APh, where the named units advance into the vehicle's Location (setup keeps enemies apart, R25.3).</summary>
    private async Task AdvanceInto(string[] units)
    {
        for (var step = 0; step < 10 && Current.Phase != "aph"; step++)
        {
            await Advance();
        }

        Committed(await Do(GameActions.Advance, Once(2, 2, 2, 2, 2, 2), new
        {
            unitIds = units,
            to = Target,
        }));
    }

    [Fact]
    public async Task AnAbandonedPanzerIsCapturedAsTheCcphEndsAndCrewedByARussianCrewUnderTheTemporaryPenalties()
    {
        // A21.2: no German Personnel is with the Abandoned PzKpfw IIIH as the Russian CCPh ends, so the Russians capture it; it stays Abandoned.
        var before = await CaptureThePanzer();
        var captured = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCaptured>());
        Assert.Equal(("de-tank", "russian", VehicleCaptured.Automatic, (string?)null), (captured.Vehicle, captured.Side, captured.Cause, captured.Driver));
        var tank = Current.Unit("de-tank")!;
        Assert.Equal(("russian", ConditionState.True, ConditionState.Unknown), (tank.Side, GameState.Condition(tank, Conditions.Abandoned), GameState.Condition(tank, Conditions.Captured)));

        // Fix Round C (the referee's review): the state keeps the side the record took it from, which Rules reads as captured (A21.22).
        Assert.Equal("german", tank.CapturedFrom);

        // A21.22: in the next Russian MPh the infantry crew enters it as a Temporary crew: its MP allotment is halved (11 MP to 5 1/2), and a squad must
        // Deploy first.
        await NextMph("russian", 2);
        var full = GamePlanner.VehicleHalfMp(Current.Unit("de-tank")!).Allotment;
        Refused(await Recrew("ru-s", "de-tank"), "Deploys first");
        before = Revision;
        var recrewed = await Recrew("ru-c", "de-tank");
        Committed(recrewed);
        Assert.Contains(recrewed.Reasons, reason => reason.Contains("an infantry crew: its MP allotment is halved", StringComparison.Ordinal));
        var record = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleRecrewed>());
        Assert.Equal((true, ScenarioA1CapturedVehicleRules.InfantryCrew, true), (record.Temporary, record.Manning, record.Captured));
        tank = Current.Unit("de-tank")!;
        Assert.Equal(new TemporaryCrew(ScenarioA1CapturedVehicleRules.InfantryCrew, true), tank.TemporaryCrew);
        Assert.Equal(full, 2 * GamePlanner.VehicleHalfMp(tank).Allotment);
        Assert.Equal(1, LiveFire.CapturedPenalty(tank));

        // In the Russian MPh of Game Turn 3 the start is preceded by the dr: a colored 4, +1 for a Russian captor, allows no movement this phase.
        await NextMph("russian", 3);
        before = Revision;
        var started = await Do(GameActions.MoveVehicle, Once(4, 1), new
        {
            vehicleId = "de-tank",
            kind = "start",
        });
        Committed(started);
        Assert.Contains(started.Reasons, reason => reason.Contains("start dr at +1", StringComparison.Ordinal));
        var check = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>());
        Assert.Equal((VehicleCheckRolled.TemporaryStart, 1, VehicleCheckRolled.NoMove), (check.Check, check.Drm, check.Result));
        Assert.True(Current.Movement is { Ending: true });
        NoReplayErrors();
    }

    [Fact]
    public async Task ACaptureAttemptInCcTakesAnEscortedAbandonedPanzer()
    {
        // A21.2: a German HS beside the Abandoned PzKpfw IIIH prevents the automatic capture, so the Russian squad attempts it in CC: CCV 5, -1 (no manned MG),
        // -1 (assumed CE), -1 (assumed Immobile), +1 (the HS, an escort): a DR of 6 is a Final 4, which would eliminate it, so it is captured. (A crew there
        // would bar the advance: CC with a Gun's crew is not reviewed, R24.3.)
        var tank = Vehicle("de-tank", "attacker-tank", Target, "german", "east");
        tank["conditions"] = new Dictionary<string, bool> { ["asl:abandoned"] = true };
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", tank, Unit("de-h", "asl:half-squad", "attacker-half-squad", Target, "german"),
            Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian"));
        await AdvanceInto(RussianSquadAtB9);
        await Advance();
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        var attempt = await Do(GameActions.VehicleCloseCombat, Once(3, 3), new
        {
            location = Target,
            attackers = RussianSquadAtB9,
            vehicleId = "de-tank",
            capture = true,
        });
        Committed(attempt);
        Assert.Contains(attempt.Reasons, reason => reason.Contains("as a capture attempt", StringComparison.Ordinal));
        var combat = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>());
        Assert.Equal("captured", combat.Resolution.GetProperty("vehicleResult").GetString());
        Assert.Equal(4, combat.Resolution.GetProperty("finalDr").GetInt32());
        var captured = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCaptured>());
        Assert.Equal(("russian", VehicleCaptured.CloseCombat), (captured.Side, captured.Cause));
        Assert.Equal("russian", Current.Unit("de-tank")!.Side);
        NoReplayErrors();
    }

    [Fact]
    public async Task ASmcTemporaryCrewIsWoundedByAStunAndEliminatedByARecall()
    {
        // D5.343: the 6+1 crews the captured tank; a Stun Wounds him, and a Recall eliminates him, leaving the tank Abandoned.
        await CaptureThePanzer();
        await NextMph("russian", 2);
        Committed(await Recrew("ru-l", "de-tank"));
        Assert.Equal(ScenarioA1CapturedVehicleRules.Smc, Current.Unit("de-tank")!.TemporaryCrew?.Manning);
        Inject(("conditions-changed", new ConditionsChanged("de-tank", new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Stunned] = ConditionState.True })));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("ru-l")!, Conditions.Wounded));
        Inject(("conditions-changed", new ConditionsChanged("de-tank", new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Recalled] = ConditionState.True })));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-l")!.Status);
        var tank = Current.Unit("de-tank")!;
        Assert.Equal((ConditionState.True, (TemporaryCrew?)null), (GameState.Condition(tank, Conditions.Abandoned), tank.TemporaryCrew));
        NoReplayErrors();
    }

    [Fact]
    public async Task ACapturedTrucksTemporaryDriverRunsItAndAnOlderCaptureReplaysAsBefore()
    {
        // A11.52, A21.21: the stopped German truck alone with the Russian squad as the CCPh begins passes to the Russians with a Temporary Driver, at no cost.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-t", "attacker-truck", Target, "german", "east"),
            Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian"), Unit("de-1", "asl:squad", "attacker-squad", Far, "german"));
        await AdvanceInto(RussianSquadAtB9);
        var before = Revision;
        for (var step = 0; step < 4 && Current.Phase != "ccph"; step++)
        {
            await Advance();
        }

        var captured = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCaptured>());
        Assert.Equal(("russian", VehicleCaptured.Unarmed, (string?)null), (captured.Side, captured.Cause, captured.Driver));
        var truck = Current.Unit("de-t")!;
        Assert.Equal(("russian", ConditionState.Unknown), (truck.Side, GameState.Condition(truck, Conditions.Abandoned)));
        NoReplayErrors();

        // A game recorded before the pass marked the truck Captured and Abandoned (ruling R11.16); that record replays as it did.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is VehicleCaptured taken
            ? item with
            {
                Type = "conditions-changed",
                Payload = new ConditionsChanged(taken.Vehicle, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                {
                    [Conditions.Captured] = ConditionState.True,
                    [Conditions.Abandoned] = ConditionState.True,
                }),
            }
            : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        var kept = replayed.Current!.Unit("de-t")!;
        Assert.Equal(("german", ConditionState.True, ConditionState.True), (kept.Side, GameState.Condition(kept, Conditions.Captured), GameState.Condition(kept, Conditions.Abandoned)));
    }

    // Increment 2j: a wreck attacked as its vehicle, and Scrounging (D10.1, D10.5 to D10.52; ruling R65.23).

    [Fact]
    public async Task AT34BurnsAPanzerWreckThatAnEliminatingDrLeftAsItWas()
    {
        // D10.1: two T-34s fire AP at the non-burning PzKpfw IIIH wreck as at the tank. The first's TK DR of 6 would eliminate a tank and does nothing to the
        // wreck; the second's DR of 2 burns it, placing a Blaze.
        await Setup("russian", "german", 1942, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Vehicle("ru-tank2", "defender-tank", Behind, "russian", "north-east"), Vehicle("de-tank", "attacker-tank", Target, "german", "east"));
        Inject(("vehicle-wrecked", new VehicleWrecked("de-tank", false)));
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-tank", "ap", 4, 2, 3, 3));
        var shot = LastShot(before);
        Assert.True(shot.Facts.GetProperty("vehicleTarget").GetProperty("wreck").GetBoolean());
        Assert.Equal("none", shot.Resolution.GetProperty("kill").GetProperty("result").GetString());
        Assert.DoesNotContain(Since(before), item => item.Payload is VehicleWrecked);
        Assert.False(GamePlanner.IsBurning(Current, Current.Unit("de-tank")!));

        before = Revision;
        Committed(await Shoot("ru-tank2", "de-tank", "ap", 4, 2, 1, 1));
        Assert.Equal("burn", LastShot(before).Resolution.GetProperty("kill").GetProperty("result").GetString());
        Assert.True(Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleWrecked>()).Burning);
        Assert.True(GamePlanner.IsBurning(Current, Current.Unit("de-tank")!));
        NoReplayErrors();
    }

    [Fact]
    public async Task TwoSquadsBurnATruckWreckOnTheIftsVehicleLine()
    {
        // D10.1, A7.308: two Russian 4-4-7s fire 8 FP at the German truck's non-burning wreck; their DR of 2 is a burning wreck on the Vehicle line, so it
        // burns. The record lists the wreck among its vehicles, and replays. The dice after the DR serve the attack's other drs.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-1", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("ru-2", "asl:squad", "defender-squad", Firer, "russian"), Vehicle("de-t", "attacker-truck", Target, "german", "east"));
        Inject(("vehicle-wrecked", new VehicleWrecked("de-t", false)));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 1, 6, 6, 6, 6), new
        {
            firers = RussianSquads,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.True(fire.Facts.GetProperty("vehicles")[0].GetProperty("wreck").GetBoolean());
        Assert.True(Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleWrecked>()).Burning);
        Assert.True(GamePlanner.IsBurning(Current, Current.Unit("de-t")!));
        NoReplayErrors();
    }

    private static readonly string[] RussiansAtTheWreck = ["ru-h", "ru-s"];

    private Task<PlayResult> Scrounge(string unit, string vehicle, DiceRoller roller) => Do(GameActions.Scrounge, roller, new
    {
        unitId = unit,
        vehicleId = vehicle,
    });

    [Fact]
    public async Task ARussianSquadScroungesAPanzerWrecksBmgAsAGermanLmgAndTheWreckIsScroungedOnce()
    {
        // D10.5, D10.52: in the Russian RPh a 4-4-7 in the Location of the PzKpfw IIIH wreck scrounges it; a dr of 1 salvages its one scroungeable weapon, the
        // BMG (its CMG and Gun are never scrounged), as the German LMG, whole, since a LMG has no dm form. The squad and the LMG are TI, and the wreck is
        // Scrounged with its BMG Disabled.
        await Start("russian", "german", 1942, 7, "asl-scenario-a1@1.18.0",
            [Vehicle("de-tank", "attacker-tank", Target, "german", "east"), Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian"),
                Unit("ru-h", "asl:half-squad", "defender-half-squad", Behind, "russian"), Unit("de-1", "asl:squad", "attacker-squad", Far, "german")]);
        Inject(("vehicle-wrecked", new VehicleWrecked("de-tank", false)), ("instance-moved", new InstanceMoved("ru-s", new MapPosition(BoardLocation.Parse(Target)))),
            ("instance-moved", new InstanceMoved("ru-h", new MapPosition(BoardLocation.Parse(Target)))));
        Assert.Equal("rph", Current.Phase);
        var candidates = Planner().ScroungeCandidates(Current, Current.Unit("de-tank")!);
        Assert.Equal(RussiansAtTheWreck, candidates.Select(unit => unit.Id).Order(StringComparer.Ordinal));

        var before = Revision;
        var scrounged = await Scrounge("ru-s", "de-tank", Once(1));
        Committed(scrounged);
        Assert.Contains(scrounged.Reasons, reason => reason.Contains("its BMG (D10.5, D10.51)", StringComparison.Ordinal));
        var record = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleScrounged>());
        var taken = Assert.Single(record.Taken!);
        Assert.Equal((1, "bmg", (string?)null), (record.Dr, taken.Mount, record.Selection));
        var lmg = (EquipmentInstance)Current.Find(taken.Weapon)!;
        Assert.Equal(("attacker-lmg", "ru-s", ConditionState.True), (lmg.Definition!.Definition, lmg.Holding!.Holder, GameState.Condition(lmg, "asl:ti")));
        Assert.NotEqual(ConditionState.True, GameState.Condition(lmg, Conditions.Dismantled));
        var wreck = Current.Unit("de-tank")!;
        Assert.Equal((ConditionState.True, ConditionState.True, ConditionState.True), (GameState.Condition(wreck, Conditions.Scrounged),
            GameState.Condition(wreck, Conditions.BmgDisabled), GameState.Condition(Current.Unit("ru-s")!, "asl:ti")));
        Assert.False(GamePlanner.MayBeScrounged(Current, wreck));

        // D10.5: the wreck is scrounged once; the HS beside the squad is refused, and the squad, TI, would be too.
        Refused(await Scrounge("ru-h", "de-tank", Once(1)), "scrounged already");
        Refused(await Scrounge("ru-s", "de-tank", Once(1)), "not TI");
        NoReplayErrors();
    }

    // Increment 2k: a crew that mans no Gun as an ordinary target and in CC (A1.123; rulings R65.28, R65.29), and A7.308's Random Selection of the vehicles
    // beyond the KIA# (ruling R65.26).

    private Task<PlayResult> SquadFires(DiceRoller roller) => Do(GameActions.Fire, roller, new
    {
        firers = RussianSquadAtB9,
        target = Target,
    });

    [Fact]
    public async Task AGermanVehicleCrewAloneInTheOpenIsAttackedAsInfantry()
    {
        // A1.123 (ruling R65.28): the 4-4-7 fires 4 FP at the German vehicle crew in the open two hexes off, which ruling R8.3 refused before: an Original
        // DR of 7, and the crew is a target of the record.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-s", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("de-c", "asl:vehicle-crew", "german-vehicle-crew", Target, "german"));
        var before = Revision;
        Committed(await SquadFires(Once(3, 4, 2, 2, 2, 2)));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal("de-c", Assert.Single(fire.Facts.GetProperty("targets").EnumerateArray()).GetProperty("unitId").GetString());
        Assert.False(fire.Facts.TryGetProperty("gunCrews", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task AGermanVehicleCrewWithASquadIsAttackedWithIt()
    {
        // A1.123 (ruling R65.28): a crew that mans no Gun is attacked with the other units of its Location.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-s", "asl:squad", "defender-squad", Firer, "russian"),
            Unit("de-c", "asl:vehicle-crew", "german-vehicle-crew", Target, "german"), Unit("de-s", "asl:squad", "attacker-squad", Target, "german"));
        var before = Revision;
        Committed(await SquadFires(Once(3, 4, 2, 2, 2, 2, 2, 2)));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(2, fire.Facts.GetProperty("targets").GetArrayLength());
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunsCrewWithASquadIsStillRefusedAsATarget()
    {
        // C11 (ruling R8.3): the German crew mans its Gun, so with a squad beside it the attack is not reviewed; the record would name it a Gun's crew.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0",
            [Unit("ru-s", "asl:squad", "defender-squad", Firer, "russian"), .. Gun("de-gun", "attacker-inf-gun", "de-gc", "asl:crew", "attacker-crew", Target, "german", "east"),
                Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        Refused(await SquadFires(NoRoll()), "crew-target-unreviewed");
        var (attack, _) = LiveFire.FromState(Current, RussianSquadAtB9, Array.Empty<string>(), BoardLocation.Parse(Target));
        Assert.Equal(GunCrewOnly, attack!.GunCrews);
    }

    private static readonly string[] GunCrewOnly = ["de-gc"];

    [Fact]
    public async Task ARussianSquadAdvancesOntoAGermanVehicleCrewAndTheCcResolves()
    {
        // A1.123 (ruling R65.29): a crew that mans no Gun is advanced against, which ruling R24.3 refused before, and fights in CC as Infantry.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("de-c", "asl:vehicle-crew", "german-vehicle-crew", Target, "german"),
            Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian"), Unit("de-1", "asl:squad", "attacker-squad", Far, "german"));
        await AdvanceInto(RussianSquadAtB9);
        await Advance();
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.CloseCombat, Once(3, 3, 2, 2, 2, 2, 2, 2), new
        {
            location = Target,
            attacks = new[] { new { attackers = RussianSquadAtB9, defenders = VehicleCrewOnly } },
        }));
        var round = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        Assert.Contains("de-c", round.Resolution.GetRawText(), StringComparison.Ordinal);
        NoReplayErrors();
    }

    private static readonly string[] VehicleCrewOnly = ["de-c"];

    [Fact]
    public async Task AnAdvanceOntoAGunsCrewIsStillRefused()
    {
        // C11 (ruling R24.3): CC with a Gun's crew is not reviewed, so the advance into its Location is refused.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0",
            [.. Gun("de-gun", "attacker-inf-gun", "de-gc", "asl:crew", "attacker-crew", Target, "german", "east"), Unit("ru-s", "asl:squad", "defender-squad", Behind, "russian")]);
        for (var step = 0; step < 10 && Current.Phase != "aph"; step++)
        {
            await Advance();
        }

        Refused(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = RussianSquadAtB9,
            to = Target,
        }), "play.advance-crew");
    }

    [Fact]
    public async Task ARussianHalfSquadsFireMeetsThreeTrucksAndRandomSelectionPicksTwo()
    {
        // A7.308 (p. 56): the 2-2-7's 2 FP at three German trucks in the open: the 2 column's highest KIA# is 2, so each truck makes a Random Selection dr
        // (4, 5, 1) and the two highest take the IFT DR of 3; the third is not affected. Ruling R25.3's one vehicle is lifted for an attack that carries
        // the PRC fact, as every attack planned since 2b does.
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("ru-h", "asl:half-squad", "defender-half-squad", Firer, "russian"),
            Vehicle("de-t1", "attacker-truck", Target, "german", "east"), Vehicle("de-t2", "attacker-truck", Target, "german", "east"),
            Vehicle("de-t3", "attacker-truck", Target, "german", "east"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 2, 4, 5, 1), new
        {
            firers = HalfSquadAtFirer,
            target = Target,
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var effects = fire.Resolution.GetProperty("vehicleEffects").EnumerateArray().ToDictionary(item => item.GetProperty("vehicleId").GetString()!);
        Assert.True(effects["de-t3"].GetProperty("notSelected").GetBoolean());
        Assert.Equal(1, effects["de-t3"].GetProperty("randomSelectionDr").GetInt32());
        Assert.False(effects["de-t1"].TryGetProperty("notSelected", out _));
        Assert.Equal(InstanceStatus.Active, Current.Unit("de-t3")!.Status);
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("de-t1")!.Status);
        NoReplayErrors();

        // An attack recorded before the pass carries no PRC fact and was never made at three vehicles: its record is refused as ruling R25.3 has it.
        var older = Planner().Replay(AsBeforeThePass());
        Assert.True(older.HasErrors);
    }

    private static readonly string[] HalfSquadAtFirer = ["ru-h"];

    [Fact]
    public async Task AnAirBurstBreaksAnOtHalftracksCrewWhichRoutsBeneathAndAbandonsIt()
    {
        // D5.311 (p. 203): the Russian 50mm hits the OT SPW 251/1 in woods. On the Vehicle line its IFT DR of 6 on the 2 column is above the Kill Number of 4,
        // so the halftrack is unharmed; its crew is attacked as Infantry with the Air Burst's -1: a Final DR of 5, a NMC. The crew's DR of 11 fails its Morale
        // Level: it breaks and stays inside, broken, and the halftrack Stops (ruling R65.31). In the RtPh the crew must rout beneath it, which Abandons it.
        changed = ("B8", "Woods");
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Unit("r1", "asl:squad", "defender-squad", FiveHexesOff, "russian"),
            LightMortar("ru-mtr", "defender-light-mortar", "r1", "russian"), Vehicle("de-ht", "attacker-halftrack", Target, "german", "east"));
        var before = Revision;
        Committed(await Lob("ru-mtr", 2, 2, 3, 3, 6, 5, 3, 3, 3, 3));
        var shot = LastShot(before);
        var line = shot.Resolution.GetProperty("hit").GetProperty("vehicleEffects").EnumerateArray().Single();
        Assert.Equal(("none", "broken", 5), (line.GetProperty("result").GetString(), line.GetProperty("crewResult").GetString(), line.GetProperty("unprotectedCrewFinalDr").GetInt32()));
        Assert.Equal(("NMC", 11, false), (line.GetProperty("crewCheck").GetProperty("kind").GetString(), line.GetProperty("crewCheck").GetProperty("finalDr").GetInt32(),
            line.GetProperty("crewCheck").GetProperty("passed").GetBoolean()));
        Assert.Contains("hit:checks:de-ht", shot.Rolls.Keys);
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit("de-ht")!, Conditions.Abandoned));
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit("de-ht")!, Conditions.Motion));
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:vehicle-crew");
        Assert.Equal(("german-vehicle-crew", Target, ConditionState.True),
            (crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(), GameState.Condition(crew, Conditions.Broken)));
        Assert.Equal(new ContainedPosition("de-ht", ContainmentRole.Crew), crew.Position);
        NoReplayErrors();
        var recorded = store.Read(Scope)!.Events.ToArray();

        // R65.31: in the RtPh the broken crew must rout beneath the halftrack, with all its MF, and the halftrack is then Abandoned.
        for (var phase = 0; phase < 6 && Current.Phase != "rtph"; phase++)
        {
            await Advance();
        }

        Assert.Equal("rtph", Current.Phase);
        Assert.Contains("broken crew of de-ht", Planner().MustRout(Current, Current.Unit(crew.Id)!), StringComparison.Ordinal);
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = crew.Id,
            beneath = true,
        }));
        Assert.Null(Current.Aboard(crew.Id));
        Assert.Equal(Target, Current.Location(crew.Id)!.Location.ToString());
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("de-ht")!, Conditions.Abandoned));
        NoReplayErrors();

        // A shot recorded before the pass carries no PRC fact: the crew is not attacked and the halftrack is untouched, as it was resolved.
        static JsonElement Older(JsonElement element, Action<JsonObject> change)
        {
            var node = JsonNode.Parse(element.GetRawText())!.AsObject();
            change(node);
            return JsonSerializer.SerializeToElement(node);
        }

        GameEvent[] older = [.. recorded
            .Where(item => item.Payload is not InstanceCreated { Instance.Kind: "asl:vehicle-crew" }
                && !(item.Payload is ConditionsChanged changed && (changed.Conditions.ContainsKey(Conditions.Abandoned) || changed.Id == crew.Id
                    || (changed.Id == "de-ht" && changed.Conditions.ContainsKey(Conditions.Motion)))))
            .Select((item, index) => item with { Revision = index + 1 })
            .Select(item => item.Payload is OrdnanceFired fired
                ? item with
                {
                    Payload = fired with
                    {
                        Rolls = fired.Rolls.Where(roll => roll.Key != "hit:checks:de-ht").ToDictionary(roll => roll.Key, roll => roll.Value, StringComparer.Ordinal),
                        Facts = Older(fired.Facts, node => node["hit"]!["vehicles"]![0]!.AsObject().Remove("prc")),
                        Resolution = Older(fired.Resolution, node =>
                        {
                            var effect = node["hit"]!["vehicleEffects"]![0]!.AsObject();
                            effect.Remove("unprotectedCrewFinalDr");
                            effect["crewCheck"] = null;
                            effect["crewResult"] = "none";
                        }),
                    }
                }
                : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors, string.Join("; ", replayed.Diagnostics.Select(item => item.Message)));
        Assert.NotEqual(ConditionState.True, GameState.Condition(replayed.Current!.Unit("de-ht")!, Conditions.Abandoned));
        Assert.DoesNotContain(replayed.Current.Units, unit => unit.Kind == "asl:vehicle-crew");
    }

    // The fixes of the referee's review of Riders, Carriers, crews, captures, and wrecks (R2).

    [Fact]
    public async Task ATankShotThatTurnsItsTurretMakesItsRiderBailOut()
    {
        // D6.21: the PzKpfw IIIH at B10, its turret to the south-west, fires HE at the Russian squad at B8, turning its turret; its Rider, a 4-6-7, Bails
        // Out beneath it on the shot's record, its NMC (2, 3) passing, under a Prep Fire counter. The proposal says so first.
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("de-tank", "attacker-tank", Firer, "german", "south-west"),
            Rider("de-s", "asl:squad", "attacker-squad", "de-tank", "german"), Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        var shot = await Shoot("de-tank", null, "he", 6, 5, 2, 3);
        Committed(shot);
        Assert.Contains(shot.Reasons, reason => reason.Contains("de-s Bails Out", StringComparison.Ordinal));
        var bailed = Assert.Single(Since(before).Select(item => item.Payload).OfType<RiderBailedOut>());
        Assert.Equal(("de-s", ScenarioA1RiderRules.Turret, Firer, ScenarioA1RiderRules.Passed), (bailed.Unit, bailed.Cause, bailed.At.ToString(), bailed.Result));
        var squad = Current.Unit("de-s")!;
        Assert.Equal(((string?)null, true, ConditionState.True), (Current.Aboard("de-s"), squad.MovementEnded, GameState.Condition(squad, Conditions.PrepFire)));
        NoReplayErrors();
    }

    [Fact]
    public async Task RidersMayNotBoardATankInWoods()
    {
        // D6.21, B13.43: the T-34 stands in woods at B8, read so; the Russian squad there may not board it as a Rider, only as a Passenger would, which it
        // carries none of.
        changed = ("B8", "Woods");
        await Setup("russian", "german", 1943, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Target, "russian", "east"),
            Unit("ru-s", "asl:squad", "defender-squad", Target, "russian"), Unit("de-s", "asl:squad", "attacker-squad", Far, "german"));
        await Advance();
        Refused(await Board("ru-tank", true, "ru-s"), "where Riders are not allowed but on a road, in a stream, or in Bypass (D6.21, B13.43)");
    }

    [Fact]
    public async Task AShockedOrStunnedTanksCrewMayNotAbandonIt()
    {
        // C7.42, D5.34: in the German MPh the Shocked (an Unconfirmed Kill now) PzKpfw IIIH's crew and the Stunned one's may not Abandon them.
        var shocked = Vehicle("de-tank", "attacker-tank", Target, "german", "east");
        shocked["conditions"] = new Dictionary<string, bool> { ["asl:shocked"] = true };
        var stunned = Vehicle("de-tank2", "attacker-tank", Behind, "german", "east");
        stunned["conditions"] = new Dictionary<string, bool> { ["asl:stunned"] = true };
        await SetupShocked("german", "russian", "de-tank", shocked, stunned, Unit("ru-1", "asl:squad", "defender-squad", Far, "russian"));
        await Advance();
        Assert.False(GamePlanner.MayAbandon(Current, Current.Unit("de-tank")!));
        Refused(await Abandon("de-tank"), "is Shocked");
        Refused(await Abandon("de-tank2"), "is Stunned");
    }

    [Fact]
    public async Task ACrewThatFailsItsTcTakesTheStunPlusOneWithIt()
    {
        // D5.5, D5.34: the mortar's hit immobilizes the T-34 under a "+1" counter, and its crew fails the TC (12, +1): the Russian vehicle crew beneath it
        // carries the "+1".
        var tank = Vehicle("ru-tank", "defender-tank", Target, "russian", "south-west");
        tank["conditions"] = new Dictionary<string, bool> { ["asl:stun-recovery"] = true };
        await Setup("german", "russian", 1943, 7, "asl-scenario-a1@1.18.0", Unit("g1", "asl:squad", "attacker-squad", Firer, "german"),
            LightMortar("de-mtr", "attacker-light-mortar", "g1", "german"), tank);
        Committed(await Lob("de-mtr", 4, 2, 2, 1, 6, 6, 3, 3, 3, 3));
        var crew = Assert.Single(Current.Units, unit => unit.Kind == "asl:vehicle-crew");
        Assert.Equal(ConditionState.True, GameState.Condition(crew, Conditions.StunRecovery));
        NoReplayErrors();
    }

    [Fact]
    public async Task AWoundedSmcTemporaryCrewWoundedAgainIsEliminated()
    {
        // D5.343, A17.2: the 6+1 crewing the captured tank is Wounded by a Stun; a second Stun Wounds him again, which eliminates him.
        await CaptureThePanzer();
        await NextMph("russian", 2);
        Committed(await Recrew("ru-l", "de-tank"));
        static (string, EventPayload) Stun(bool value) => ("conditions-changed", new ConditionsChanged("de-tank",
            new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Stunned] = value ? ConditionState.True : ConditionState.False }));
        Inject(Stun(true));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("ru-l")!, Conditions.Wounded));
        Inject(Stun(false));
        Inject(Stun(true));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-l")!.Status);
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("de-tank")!, Conditions.Abandoned));
        NoReplayErrors();
    }
}
