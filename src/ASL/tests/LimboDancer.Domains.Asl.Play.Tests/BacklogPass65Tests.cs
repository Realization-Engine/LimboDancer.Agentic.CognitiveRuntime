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
        Assert.Equal((InstanceStatus.Active, Target, (string?)null), (survivor.Status, Current.Location("de-h")!.Location.ToString(), Current.Aboard("de-h")));
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
        Assert.False(fire.Facts.GetProperty("embarking").GetProperty("unloading").GetBoolean());
        Assert.Equal(["ffnam"], MovementDrm(fire));
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<InstanceMoved>(), moved => moved.Id == "de-s");
        var squad = Current.Unit("de-s")!;
        Assert.Equal((ConditionState.True, (string?)null, Target), (GameState.Condition(squad, Conditions.Pinned), Current.Aboard("de-s"), Current.Location("de-s")!.Location.ToString()));
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
}
