using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 24 of the Card Play and Map Studio Redesign Plan (rulings R24.1 to R24.7): Control of Locations, Mopping Up, Gun and vehicle VP, a vehicle's
/// temporary Control, and the Control fold cached by revision. The Guards Counterattack on board 01 with its real levels (cellar, ground, levels 1 and 2,
/// a rooftop). No card fields a Gun or a vehicle yet (plan pass 26), so their VP are tested with constructed games.
/// </summary>
public sealed class BacklogPass24Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a724");
    private static readonly GameScope Scope = new(Tenant, "p24");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] F3Hexes = ["E4", "F3", "G3", "G4"];
    private static readonly string[] Boards = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p24-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Queue<int> dice = new();

    public BacklogPass24Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog]);

    /// <summary>The dice the test queues, then 6s.</summary>
    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: new DiceRoller(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1));

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task<PlayResult> Act(Abstractions.Actions.ActionDescriptor action, object? arguments = null)
    {
        var node = arguments is null ? new System.Text.Json.Nodes.JsonObject() : JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> MopUp(string building, string[] unitIds, string? guard = null) =>
        Act(GameActions.MopUp, guard is null ? new
        {
            building,
            unitIds
        } : new
        {
            building,
            unitIds,
            guard
        });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = new Dictionary<string, object> { ["at"] = at },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A group's whole OB on board 01, squads three to a hex of its buildings, SMC with the first squad, SW held by the area's first squad.</summary>
    private static List<Dictionary<string, object>> Ob(ScenarioCard card, string side, int groupIndex)
    {
        var group = card.Sides.Single(item => item.Side == side).Groups[groupIndex];
        var id = ScenarioCards.GroupId(side, groupIndex);
        var placements = new List<Dictionary<string, object>>();
        var squadsAt = new Dictionary<string, int>(StringComparer.Ordinal);
        var holders = new Dictionary<string, string>(StringComparer.Ordinal);
        var serial = 0;
        foreach (var line in group.Units)
        {
            var areas = line.Area is { } named ? group.Areas.Where(area => area.Id == named).ToArray() : [.. group.Areas.Where(area => area.Kind == "building")];
            var hexes = areas.SelectMany(area => area.Hexes!).ToArray();
            var definition = Catalog.Definition(line.Definition)!;
            for (var count = 0; count < line.Count; count++)
            {
                var unitId = $"{id}-{++serial}";
                if (Vocabulary.IsA(definition.Kind, "asl:equipment"))
                {
                    placements.Add(Weapon(unitId, line.Definition, holders[areas[0].Id], side));
                    continue;
                }

                var hex = definition.Kind == "asl:squad" ? hexes.First(item => squadsAt.GetValueOrDefault(item) < 3) : hexes[0];
                if (definition.Kind == "asl:squad")
                {
                    squadsAt[hex] = squadsAt.GetValueOrDefault(hex) + 1;
                    holders.TryAdd(areas[0].Id, unitId);
                }

                placements.Add(Unit(unitId, line.Definition, $"bd01:{hex}:0", side, id));
            }
        }

        return placements;
    }

    private async Task Place(params Dictionary<string, object>[] placements)
    {
        var node = JsonSerializer.SerializeToNode(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Revision}",
            expectedRevision = Revision,
            placements,
        })!.AsObject();
        if (Revision == 0)
        {
            node["start"] = JsonSerializer.SerializeToNode(new
            {
                label = "guards",
                catalog = "asl-scenario-a1@1.12.0",
                boards = Boards,
                sides = Array.Empty<object>(),
                scenario = new
                {
                    id = "guards-counterattack",
                    sha256 = ScenarioCards.Sha256("guards-counterattack"),
                    title = "guards"
                },
            });
        }

        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node)));
    }

    /// <summary>The Guards Counterattack set up, played to the Russian PFPh of Game Turn 1 (the Russians move first).</summary>
    private async Task GuardsInRussianPrepFire()
    {
        var card = Card("guards-counterattack");
        await Place([.. Ob(card, "german", 0)]);
        await Place([.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]);
        for (var step = 0; step < 6 && !(Current.SetupClosed && Current.Phase == "pfph" && Current.PhasingSide == "russian"); step++)
        {
            Committed(await Act(GameActions.AdvancePhase));
        }

        Assert.Equal("pfph", Current.Phase);
        Assert.Equal("russian", Current.PhasingSide);
    }

    /// <summary>Events written straight to the log, as an adjudicator's correction would be: they must still replay.</summary>
    private void Inject(params (string Type, EventPayload Payload)[] items)
    {
        var revision = Revision;
        GameEvent[] events = [.. items.Select((item, index) => new GameEvent(Scope, $"inject-{revision}-{index + 1}", revision + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, [], null))];
        var result = store.Append(Scope, "inject", revision, events, Planner().Replay);
        Assert.True(result.Status == AppendStatus.Committed, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
    }

    private static (string, EventPayload) MovedTo(string unit, string at) => ("instance-moved", new InstanceMoved(unit, new MapPosition(BoardLocation.Parse(at))));

    private static (string, EventPayload) Set(string unit, string condition, bool value = true) =>
        ("conditions-changed", new ConditionsChanged(unit, new Dictionary<string, ConditionState> { [condition] = value ? ConditionState.True : ConditionState.False }));

    private bool In(UnitInstance unit, params string[] hexes) => Current.Location(unit.Id)?.Location is { } at && hexes.Contains(at.Hex.ToString());

    private string RussianSquadIn(params string[] hexes) => Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && In(unit, hexes)).Id;

    private string GermanSquad() => Current.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad").Id;

    private static string? ControlOf(VictoryReport report, string id) => report.Control.Single(item => item.Id == id).Side;

    /// <summary>The report of the game with constructed states added after the present one.</summary>
    private VictoryReport ReportWith(GamePlanner planner, bool ended, params GameState[] added)
    {
        var history = History;
        return planner.Victory(new GameHistory(history.Events, [.. history.States, .. added], []), ended)!;
    }

    private static GameState Changed(GameState state, Func<UnitInstance, UnitInstance?> change) =>
        state with
        {
            Units = [.. state.Units.Select(unit => change(unit) ?? unit)]
        };

    private static UnitInstance At(UnitInstance unit, string at) => unit with { Position = new MapPosition(BoardLocation.Parse(at)) };

    private static UnitInstance With(UnitInstance unit, string condition) =>
        unit with
        {
            Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [condition] = ConditionState.True }
        };

    // R24.1 (A26.1, A26.11, A26.14, the A26.16 EX, B23.41): a building's Locations start with its setup side, rooftops and cellars left out; a Russian
    // squad on level 1 of F5, the Germans gone, gains the building and that Location only; the ground level and level 2 it never entered stay German, and
    // only they differ from the building. A German back at ground level does not regain the building while the Russian is there, broken or not (the EX's 4-4-7).
    [Fact]
    public async Task LocationsKeepTheirOwnControl()
    {
        await GuardsInRussianPrepFire();
        var planner = Planner();
        var start = planner.Victory(History)!;
        Assert.DoesNotContain(start.Control, item => item.Id == "bd01:F5:-1");
        Assert.Equal("german", ControlOf(start, "bd01:F5:2"));
        Assert.Equal("russian", ControlOf(start, "bd01:F3:1"));
        Assert.DoesNotContain(start.Control, item => item.Id == "bd01:F5:3");
        Assert.Empty(ScenarioVictory.DifferingLocations(start.Control));

        var russian = RussianSquadIn(F3Hexes);
        var german = Current.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad" && In(unit, "F5", "F6", "G6", "H5")).Id;
        var taken = Changed(Current, unit => unit.Side == "german" && In(unit, "F5", "F6", "G6", "H5") ? unit with
        {
            Status = InstanceStatus.Eliminated
        }
            : unit.Id == russian ? At(unit, "bd01:F5:1") : null);
        var report = ReportWith(planner, false, taken);
        Assert.Equal("russian", ControlOf(report, "F5"));
        Assert.Equal("russian", ControlOf(report, "bd01:F5:1"));
        Assert.Equal("german", ControlOf(report, "bd01:F5:0"));
        Assert.Equal("german", ControlOf(report, "bd01:F5:2"));
        var differing = ScenarioVictory.DifferingLocations(report.Control).Select(item => item.Id).ToArray();
        Assert.Contains("bd01:F5:0", differing);
        Assert.DoesNotContain("bd01:F5:1", differing);
        Assert.Contains(report.AtEnd.Facts, fact => fact == "Location bd01:F5 ground level of building F5: german");

        var back = Changed(taken, unit => unit.Id == german ? At(unit with { Status = InstanceStatus.Active }, "bd01:F6:0") : null);
        var broken = Changed(back, unit => unit.Id == russian ? With(unit, Conditions.Broken) : null);
        var held = ReportWith(planner, false, taken, back, broken);
        Assert.Equal("russian", ControlOf(held, "F5"));
        Assert.Equal("german", ControlOf(held, "bd01:F6:0"));

        var gone = Changed(broken, unit => unit.Id == russian ? unit with { Status = InstanceStatus.Eliminated } : null);
        var regained = ReportWith(planner, false, taken, back, broken, gone);
        Assert.Equal("german", ControlOf(regained, "F5"));
        Assert.Equal("russian", ControlOf(regained, "bd01:F5:1"));
    }

    // R24.2 (A12.153, A26.11): a broken German squad on level 1 of the Russians' F3, which it had gained in Good Order, surrenders when a Russian squad
    // Mops Up; the squad becomes TI, the guard takes the prisoner, the Russians Control every Location again, and F3 is not Mopped Up twice in a turn.
    [Fact]
    public async Task MoppingUpSecuresTheBuilding()
    {
        await GuardsInRussianPrepFire();
        var german = GermanSquad();
        Inject(MovedTo(german, "bd01:G4:1"));
        Inject(Set(german, Conditions.Broken));
        Assert.Equal("german", ControlOf(Planner().Victory(History)!, "bd01:G4:1"));

        var mopper = RussianSquadIn(F3Hexes);
        Committed(await MopUp("F3", [mopper]));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(mopper)!, "asl:ti"));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(german)!, Conditions.Captured));
        Assert.Equal(mopper, Current.Unit(german)!.Custodian);
        Assert.Equal(Current.Location(mopper)!.Location, Current.Location(german)!.Location);
        Assert.Contains(Current.Secured, item => item.Building == "F3" && item.Side == "russian");
        var report = Planner().Victory(History)!;
        Assert.Equal("russian", ControlOf(report, "bd01:G4:1"));
        Assert.Equal("russian", ControlOf(report, "F3"));
        Refused(await MopUp("F3", [RussianSquadIn(F3Hexes)]), "play.mop-up-once");
    }

    // R24.2 (A12.153, A12.154): a concealed German squad in F3 keeps it from being secured; the DEFENDER's Casualty dr has -1 for its two HS-equivalents,
    // so an Original dr of 2 Reduces the Mopping-Up squad (to a HS, still TI) and a 3 does not.
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task AConcealedEnemyCostsTheMoppingUp(int roll, bool reduced)
    {
        await GuardsInRussianPrepFire();
        var german = GermanSquad();
        Inject(MovedTo(german, "bd01:G4:1"), Set(german, Conditions.Concealed));
        var mopper = RussianSquadIn(F3Hexes);
        dice.Enqueue(roll);
        Committed(await MopUp("F3", [mopper]));
        Assert.Empty(Current.Secured);
        Assert.Contains("F3", Current.MoppedUpThisPlayerTurn);
        Assert.Equal(reduced, Current.Unit(mopper)!.Status != InstanceStatus.Active);
        if (reduced)
        {
            var half = Current.Units.Single(unit => unit.Kind == "asl:half-squad" && unit.Id.EndsWith(mopper, StringComparison.Ordinal));
            Assert.Equal(ConditionState.True, GameState.Condition(half, "asl:ti"));
        }
    }

    // R24.2 (A12.153): a hidden German squad in F3 is placed beneath "?" and the building is not secured.
    [Fact]
    public async Task MoppingUpPlacesHiddenUnitsBeneathQuestionMarks()
    {
        await GuardsInRussianPrepFire();
        var german = GermanSquad();
        Inject(MovedTo(german, "bd01:G4:2"), Set(german, Conditions.Hidden));
        Committed(await MopUp("F3", [RussianSquadIn(F3Hexes)]));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(german)!, Conditions.Concealed));
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit(german)!, Conditions.Hidden));
        Assert.Empty(Current.Secured);
    }

    // R24.2 (A12.153): Mopping Up is refused with an unconcealed unbroken enemy inside, in a one-hex one-level building (M9), by a unit outside the
    // building, and outside the PFPh.
    [Fact]
    public async Task MoppingUpIsRefusedWhereTheRuleForbidsIt()
    {
        await GuardsInRussianPrepFire();
        var mopper = RussianSquadIn(F3Hexes);
        Refused(await MopUp("M9", [mopper]), "play.mop-up-building");
        Refused(await MopUp("F5", [mopper]), "play.mop-up-units");
        var german = GermanSquad();
        Inject(MovedTo(german, "bd01:G4:1"));
        Refused(await MopUp("F3", [mopper]), "play.mop-up-enemy");
        Committed(await Act(GameActions.AdvancePhase));
        Refused(await MopUp("F3", [mopper]), "play.mop-up-phase");
    }

    // R24.3 (A26.212, A26.211, the A26.212 EX): a PzKpfw IIIH (AF 6) is worth 1 + 1 MA + 2 AF + 2 crew = 6, a T-34 M41 (AF 11) 7, a SPW 251/1 (AF 1, its
    // AAMG the catalog's MA) 5, an unarmed truck 1; a malfunctioned MA takes one away, and an Abandoned vehicle has no crew.
    [Fact]
    public async Task VehiclesAreWorthTheirVictoryPoints()
    {
        await GuardsInRussianPrepFire();
        var state = Current;
        var planner = Planner();
        var squad = state.Units.First(unit => unit.Kind == "asl:squad");
        UnitInstance Vehicle(string definition) => squad with
        {
            Id = definition,
            Kind = "asl:vehicle",
            Definition = squad.Definition! with
            {
                Definition = definition
            }
        };
        Assert.Equal(6, planner.VictoryPoints(state, Vehicle("attacker-tank")));
        Assert.Equal(7, planner.VictoryPoints(state, Vehicle("defender-tank")));
        Assert.Equal(5, planner.VictoryPoints(state, Vehicle("attacker-halftrack")));
        Assert.Equal(1, planner.VictoryPoints(state, Vehicle("attacker-truck")));
        Assert.Equal(5, planner.VictoryPoints(state, With(Vehicle("attacker-tank"), Conditions.Malfunctioned)));
        Assert.Equal(4, planner.VictoryPoints(state, With(Vehicle("attacker-tank"), Conditions.Abandoned)));
    }

    // R24.3 (A26.212, A26.221, A26.222), with a constructed game: a German Gun eliminated gives the Russians 2 CVP and a wrecked German tank 6; a German
    // Gun last held by a Russian squad is captured, 2 CVP during play and 4 at the end; an Abandoned captured tank 4, then 8.
    [Fact]
    public async Task GunsAndVehiclesGiveCasualtyVictoryPoints()
    {
        await GuardsInRussianPrepFire();
        var planner = Planner();
        var before = planner.Victory(History)!.Sides.Single(side => side.Side == "russian").Cvp;
        var state = Current;
        var squad = state.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad");
        var russian = RussianSquadIn(F3Hexes);
        var tank = squad with
        {
            Id = "tank",
            Kind = "asl:vehicle",
            Definition = squad.Definition! with
            {
                Definition = "attacker-tank"
            },
            Status = InstanceStatus.Wrecked
        };
        var gun = new EquipmentInstance("gun", "asl:gun", "german", squad.Position, null, new Dictionary<string, ConditionState>(), InstanceStatus.Eliminated);
        var lost = state with
        {
            Units = [.. state.Units, tank],
            Equipment = [.. state.Equipment, gun],
        };
        var russianSide = ReportWith(planner, false, lost).Sides.Single(side => side.Side == "russian");
        Assert.Equal(before + 8, russianSide.Cvp);
        Assert.Equal(8, russianSide.GunAndVehicleCvp);

        var captured = lost with
        {
            Units = [.. state.Units, With(With(tank with { Status = InstanceStatus.Active }, Conditions.Abandoned), Conditions.Captured)],
            Equipment = [.. state.Equipment, gun with { Status = InstanceStatus.Active, Holding = new Holding(russian, HoldingRole.Possessed), Position = NotEnteredPosition.Instance }],
        };
        Assert.Equal(6, ReportWith(planner, false, captured).Sides.Single(side => side.Side == "russian").GunAndVehicleCvp);

        // A26.222: a Gun left unheld stays captured by the side that last held it.
        var dropped = captured with
        {
            Equipment = [.. state.Equipment, gun with { Status = InstanceStatus.Active, Holding = null }],
        };
        Assert.Equal(12, ReportWith(planner, true, captured, dropped).Sides.Single(side => side.Side == "russian").GunAndVehicleCvp);
    }

    // R24.5 (A26.12): a German tank alone on the ground level of F3 holds that Location for now, never the building; when it leaves, the Location reverts.
    [Fact]
    public async Task AVehicleControlsItsLocationForNow()
    {
        await GuardsInRussianPrepFire();
        var planner = Planner();
        var state = Current;
        var squad = state.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad");
        var tank = At(squad with
        {
            Id = "tank",
            Kind = "asl:vehicle",
            Definition = squad.Definition! with
            {
                Definition = "attacker-tank"
            }
        }, "bd01:F3:0");
        var there = state with
        {
            Units = [.. state.Units.Select(unit => unit.Side == "russian" && In(unit, "F3") ? unit with { Status = InstanceStatus.Eliminated } : unit), tank],
        };
        var report = ReportWith(planner, false, there);
        var location = report.Control.Single(item => item.Id == "bd01:F3:0");
        Assert.Equal("german", location.Side);
        Assert.True(location.ByVehicle);
        Assert.Equal("russian", ControlOf(report, "F3"));

        var left = there with
        {
            Units = [.. there.Units.Select(unit => unit.Id == "tank" ? At(unit, "bd01:A1:0") : unit)],
        };
        Assert.Equal("russian", ControlOf(ReportWith(planner, false, there, left), "bd01:F3:0"));
    }

    // R24.6: a planner that read the game before reads the same Control after more events as a fresh one, folding only the new states.
    [Fact]
    public async Task TheControlFoldIsCachedByRevision()
    {
        await GuardsInRussianPrepFire();
        var planner = Planner();
        _ = planner.Victory(History);
        var german = GermanSquad();
        Inject(MovedTo(german, "bd01:G4:1"));
        string[] Sides(VictoryReport report) => [.. report.Control.Select(item => $"{item.Id}={item.Side}")];
        Assert.Equal(Sides(Planner().Victory(History)!), Sides(planner.Victory(History)!));
        Assert.Equal("german", ControlOf(planner.Victory(History)!, "bd01:G4:1"));
    }

    // R24.2 (table player, pass 24): a refusal names why the unit may not Mop Up; a broken MMC may not guard, nor an enemy unit (A20.5).
    [Fact]
    public async Task MoppingUpRefusalsSayWhy()
    {
        await GuardsInRussianPrepFire();
        var squads = Current.Units.Where(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && In(unit, F3Hexes)).Select(unit => unit.Id).ToArray();
        Inject(Set(squads[0], Conditions.Pinned), Set(squads[1], Conditions.Broken));
        var pinned = await MopUp("F3", [squads[0]]);
        Refused(pinned, "play.mop-up-unit");
        Assert.Contains(pinned.Reasons, reason => reason.Contains($"{squads[0]} is pinned", StringComparison.Ordinal));
        Refused(await MopUp("F3", [squads[2]], squads[1]), "play.mop-up-guard");
        Refused(await MopUp("F3", [squads[2]], GermanSquad()), "play.mop-up-guard");
        Committed(await MopUp("F3", [squads[2]], squads[3]));
    }

    // R24.5 (referee, pass 24; the Index's Armed, D5.1): an unarmed truck has no inherent crew, so it neither holds a Location nor prevents its Control.
    [Fact]
    public async Task AnUnarmedTruckHoldsNothing()
    {
        await GuardsInRussianPrepFire();
        var planner = Planner();
        var state = Current;
        var squad = state.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad");
        var truck = At(squad with
        {
            Id = "truck",
            Kind = "asl:vehicle",
            Definition = squad.Definition! with
            {
                Definition = "attacker-truck"
            }
        }, "bd01:F3:0");
        var there = state with
        {
            Units = [.. state.Units.Select(unit => unit.Side == "russian" && In(unit, "F3") ? unit with { Status = InstanceStatus.Eliminated } : unit), truck],
        };
        var location = ReportWith(planner, false, there).Control.Single(item => item.Id == "bd01:F3:0");
        Assert.Equal("russian", location.Side);
        Assert.False(location.ByVehicle);
    }
}
