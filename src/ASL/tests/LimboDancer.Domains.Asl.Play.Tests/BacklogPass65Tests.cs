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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog], fireLos: new StubLos());

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
            firers = new[] { "ru-1", "ru-2" },
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
            firers = new[] { "de-s" },
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
            units = new[] { "de-s" },
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
            firers = new[] { "de-s" },
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
            firers = new[] { "de-s" },
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
            route = new[] { "bd01:B9:0" },
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
}
