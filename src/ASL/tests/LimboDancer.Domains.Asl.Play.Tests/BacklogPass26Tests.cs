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
/// Pass 26 of the Card Play and Map Studio Redesign Plan (rulings R26.1 to R26.7): vehicles entering from off board in Motion, loaded and towing; Passengers
/// loading and unloading; a crew manning a Gun stacking as a squad; A12.34's hidden Guns; vehicles and Guns leaving the map for Exit VP; the Covered Arc
/// on a reversed board. The manufactured card Armor Test on a stand-in board 4: Open Ground but for woods in G6 and H6.
/// </summary>
public sealed class BacklogPass26Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a726");
    private static readonly GameScope Scope = new(Tenant, "p26");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private static readonly BoardHandle Board4 = Board("bd04", new Dictionary<string, string>(StringComparer.Ordinal) { ["G6"] = "Woods", ["H6"] = "Woods" });
    private static readonly BoardHandle Board2 = Board("bd02", []);
    private static readonly string[] Board4Only = ["bd04"];
    private static readonly string[] HalftrackPassengers = ["gs", "gl"];
    private static readonly string[] SquadOnly = ["gs"];
    private static readonly string[] LeaderOnly = ["gl"];
    private static readonly string[] CrewOnly = ["rc"];
    private static readonly string[] GoodOrder = ["asl:broken", "asl:concealed", "asl:hidden"];

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p26-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly ScenarioCardLibrary cards;
    private readonly Queue<int> dice = new();

    public BacklogPass26Tests()
    {
        store = new FileGameStore(root);
        cards = new ScenarioCardLibrary(Path.Combine(root, "cards"));
    }

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

    // Clear LOS at the board's range: the stand-in boards have no LOS data.
    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private static TerrainType Type(string name) => new()
    {
        Code = (byte)(Math.Abs(name.GetHashCode(StringComparison.Ordinal)) % 250 + 1),
        Name = name,
        Category = name == "Woods" ? LosCategory.Woods : LosCategory.Open,
    };

    private static BoardHandle Board(string board, Dictionary<string, string> terrain)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index).ToString();
            var center = new LocationFacts(0, Type(terrain.GetValueOrDefault(name) ?? "Open Ground"), null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side =>
                new HexsideFacts(side, true, Type(terrain.GetValueOrDefault(name) ?? "Open Ground"), null, false, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse(board), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board4, Board2]), Vocabulary, [Catalog], fireLos: new ClearLos(), cardLibrary: cards);

    private DiceRoller Roller() => new(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: Roller());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task<PlayResult> Place(params Dictionary<string, object>[] placements)
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
            node["start"] = JsonSerializer.SerializeToNode(new Dictionary<string, object?>
            {
                ["label"] = "armor-test",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Board4Only,
                ["sides"] = Array.Empty<object>(),
                ["scenario"] = new
                {
                    id = "armor-test",
                    sha256 = cards.Sha256("armor-test"),
                    title = "Armor Test"
                },
            });
        }

        return await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node));
    }

    private async Task<PlayResult> Act(Abstractions.Actions.ActionDescriptor action, object? arguments = null)
    {
        var node = arguments is null ? new System.Text.Json.Nodes.JsonObject() : JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Drive(string vehicle, string kind, object? extra = null)
    {
        var node = JsonSerializer.SerializeToNode(extra ?? new Dictionary<string, object>())!.AsObject();
        node["vehicleId"] = vehicle;
        node["kind"] = kind;
        return Act(GameActions.MoveVehicle, node);
    }

    private async Task Pass()
    {
        if (Current.Movement is { WindowOpen: true })
        {
            Committed(await Act(GameActions.PassFire));
        }
    }

    private async Task EndMove()
    {
        for (var step = 0; step < 6 && Current.Movement is not null; step++)
        {
            Committed(await Act(Current.Movement is { WindowOpen: true } ? GameActions.PassFire : GameActions.EndMove));
        }
    }

    private async Task AdvanceTo(string side, string phase, int turn = 0)
    {
        for (var step = 0; step < 40 && ((Current.PhasingSide, Current.Phase) != (side, phase) || (turn > 0 && Current.Turn != turn)); step++)
        {
            Committed(await Act(GameActions.AdvancePhase));
        }

        Assert.Equal((side, phase), (Current.PhasingSide, Current.Phase));
    }

    private void Inject(params (string Type, EventPayload Payload)[] items)
    {
        var revision = Revision;
        GameEvent[] events = [.. items.Select((item, index) => new GameEvent(Scope, $"inject-{revision}-{index + 1}", revision + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, [], null))];
        var result = store.Append(Scope, "inject", revision, events, Planner().Replay);
        Assert.True(result.Status == AppendStatus.Committed, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
    }

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static void Says(PlayResult result, string text) =>
        Assert.True(result.Reasons.Any(reason => reason.Contains(text, StringComparison.Ordinal)), $"'{text}' not in: {string.Join("; ", result.Reasons)}");

    private static BoardLocation At(string at) => BoardLocation.Parse(at);

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static Dictionary<string, object> Counter(string id, string definition, string side, object position, Dictionary<string, bool>? conditions = null, object? holding = null)
    {
        var counter = new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = Catalog.Definition(definition)!.Kind,
            ["definition"] = definition,
            ["side"] = side,
            ["group"] = side + "-1",
            ["position"] = position,
            ["conditions"] = conditions ?? new Dictionary<string, bool>(),
        };
        if (Catalog.Definition(definition)!.Kind is not ("asl:vehicle" or "asl:gun"))
        {
            var states = (Dictionary<string, bool>)counter["conditions"];
            foreach (var name in GoodOrder)
            {
                states.TryAdd(name, false);
            }
        }
        if (holding is not null)
        {
            counter["holding"] = holding;
            counter.Remove("group");
        }

        return counter;
    }

    private static Dictionary<string, object> OnMap(string id, string definition, string side, string at, string? facing = null, params string[] states) =>
        Counter(id, definition, side, facing is null ? new
        {
            at
        } : new
        {
            at,
            facing
        }, states.ToDictionary(state => state, _ => true));

    private static Dictionary<string, object> Aboard(string id, string definition, string vehicle) =>
        Counter(id, definition, "german", new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "passenger" });

    private static Dictionary<string, object> OffBoard(string id, string definition) => Counter(id, definition, "german", new { offMap = true });

    private static Dictionary<string, object> Gun(string id, string definition, string side, string at, string facing, string crew, params string[] states)
    {
        var gun = Counter(id, definition, side, new
        {
            at,
            facing
        }, states.ToDictionary(state => state, _ => true), new
        {
            holder = crew,
            role = "manned"
        });
        return gun;
    }

    private static Dictionary<string, object> Towed(string id, string definition, string vehicle) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:gun",
        ["definition"] = definition,
        ["side"] = "german",
        ["holding"] = new
        {
            holder = vehicle,
            role = "towed"
        },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    /// <summary>The Russians in hexrows 5 to 10, the AT Gun manned in G10 (or as given); the Germans off board, the squad and leader in the halftrack, the infantry gun towed by the truck with its crew aboard.</summary>
    private async Task SetUp(string gunAt = "bd04:G10:0", params string[] gunStates)
    {
        Committed(await Place(OnMap("rs1", "defender-squad", "russian", "bd04:K7:0"), OnMap("rs2", "defender-squad", "russian", "bd04:L8:0"),
            OnMap("rs3", "defender-squad", "russian", "bd04:M7:0"), OnMap("rl", "defender-leader-8-1", "russian", "bd04:K7:0"),
            OnMap("rc", "defender-crew", "russian", gunAt, null, gunStates), Gun("rg", "defender-at-gun", "russian", gunAt, "north-east", "rc", gunStates),
            OnMap("rk", "defender-tank", "russian", "bd04:Q7:0", "north-west")));
        Committed(await Place(OffBoard("ght", "attacker-halftrack"), Aboard("gs", "attacker-squad", "ght"), Aboard("gl", "attacker-leader-8-1", "ght"),
            OffBoard("gt", "attacker-truck"), Towed("gg", "attacker-inf-gun", "gt"), Aboard("gc", "attacker-crew", "gt"), OffBoard("gk", "attacker-tank")));
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Complete);
    }

    // R26.1 (A2.52, D2.4): the German MPh holds its vehicles until they enter; the halftrack enters in Motion, with no Start MP, a VCA holding the hex,
    // and its Passengers aboard; they ride out of reach of the Location's units, and act only with their vehicle.
    [Fact]
    public async Task AVehicleEntersFromOffBoardInMotionWithItsPassengers()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        Refused(await Act(GameActions.AdvancePhase), "play.entry-due-vehicle");
        Refused(await Drive("ght", "start"), "play.entry-vehicle");
        Refused(await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "east"
        }), "play.entry-vca");
        Refused(await Drive("ght", "enter", new
        {
            to = "bd04:I3:0",
            facing = "south-east"
        }), "play.entry-edge");

        var entered = await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "south-east"
        });
        Committed(entered);
        Says(entered, "from off board across the top edge, in Motion");
        Says(entered, "with gs, gl aboard");
        Assert.Equal(At("bd04:I1:0"), Current.Location("ght")!.Location);
        Assert.Equal(At("bd04:I1:0"), Current.Location("gs")!.Location);
        Assert.DoesNotContain(Current.At(At("bd04:I1:0")), item => item.Id == "gs");
        Assert.Equal("ght", Current.Aboard("gs"));
        Assert.True(Current.Movement!.Started);

        // Ruling R26.2: a Passenger acts only with its vehicle.
        await Pass();
        Refused(await Act(GameActions.Move, new
        {
            unitIds = SquadOnly,
            to = "bd04:I2:0"
        }), "play.aboard");
    }

    // R26.2 (D6.5, D6.4): the halftrack stops and its Passengers disembark for a quarter of its MP, each having spent one MF plus one per quarter used; the
    // squad moves on; the leader boards again, in the next MPh, before the halftrack spends any MP.
    [Fact]
    public async Task PassengersUnloadFromAStoppedVehicleAndBoardAgain()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        Committed(await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "south-east"
        }));
        await Pass();
        Refused(await Drive("ght", "unload", new
        {
            units = HalftrackPassengers
        }), "play.unload");
        Committed(await Drive("ght", "stop"));
        await Pass();
        var unloaded = await Drive("ght", "unload", new
        {
            units = HalftrackPassengers
        });
        Committed(unloaded);
        Says(unloaded, "disembark beneath ght");
        Assert.Null(Current.Aboard("gs"));
        Assert.Contains(Current.At(At("bd04:I1:0")), item => item.Id == "gs");
        Assert.True(Current.Unit("gs")!.MfSpent >= 2);
        await EndMove();
        Committed(await Act(GameActions.Move, new
        {
            unitIds = SquadOnly,
            to = "bd04:I2:0"
        }));
        await EndMove();
        Refused(await Drive("ght", "load", new
        {
            units = LeaderOnly
        }), "play.move-vehicle");

        foreach (var vehicle in new[] { "gt", "gk" })
        {
            Committed(await Drive(vehicle, "enter", new
            {
                to = vehicle == "gt" ? "bd04:Q1:0" : "bd04:Y1:0",
                facing = "south-west"
            }));
            await Pass();
            Committed(await Drive(vehicle, "stop"));
            await EndMove();
        }

        await AdvanceTo("german", "mph", 2);
        var boarded = await Drive("ght", "load", new
        {
            units = LeaderOnly
        });
        Committed(boarded);
        Says(boarded, "gl boards ght");
        Assert.Equal("ght", Current.Aboard("gl"));

        // The tank carries no Passengers (D6.1).
        Assert.Null(GamePlanner.PassengerCapacity(Current, Current.Unit("gk")!));
    }

    // R26.1, R26.2 (C10.1, C10.12): the truck enters towing the infantry gun with its crew aboard; stopped, it unhooks the Gun and the crew disembarks to man it.
    [Fact]
    public async Task ATruckEntersTowingAGunAndUnhooksItForItsCrew()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        Committed(await Drive("gt", "enter", new
        {
            to = "bd04:Q1:0",
            facing = "south-west"
        }));
        Assert.Equal(At("bd04:Q1:0"), Current.Location("gg")!.Location);
        await Pass();
        Committed(await Drive("gt", "stop"));
        await Pass();
        var unhooked = await Act(GameActions.HookGun, new
        {
            vehicleId = "gt",
            gunId = "gg",
            hooked = false,
            facing = "south-west"
        });
        Committed(unhooked);
        Says(unhooked, "gc disembarks to man it");
        Assert.Equal(new Holding("gc", HoldingRole.Manned), ((EquipmentInstance)Current.Find("gg")!).Holding);
        Assert.Null(Current.Aboard("gc"));
    }

    // R26.4 (A2.6, A26.23): a vehicle that leaves the map records its exit with its Passengers'; their VP win the Germans the game at once.
    [Fact]
    public async Task AVehicleLeavingWithItsPassengersCountsExitVp()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        Committed(await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "south-east"
        }));
        await Pass();
        Committed(await Drive("ght", "stop"));
        await EndMove();
        foreach (var vehicle in new[] { "gt", "gk" })
        {
            Committed(await Drive(vehicle, "enter", new
            {
                to = vehicle == "gt" ? "bd04:Q1:0" : "bd04:Y1:0",
                facing = "south-west"
            }));
            await Pass();
            Committed(await Drive(vehicle, "stop"));
            await EndMove();
        }

        await AdvanceTo("german", "mph", 2);
        Inject(("instance-moved", new InstanceMoved("ght", new MapPosition(At("bd04:Q10:0")) { Facing = Units.Documents.UnitFacing.SouthEast })));
        Committed(await Drive("ght", "start"));
        await Pass();
        var left = await Drive("ght", "exit", new
        {
            edge = "bottom"
        });
        Committed(left);
        Says(left, "with gs, gl aboard");
        Assert.Contains(Current.Exits, exit => exit.Unit == "ght" && exit.Edge == "bottom");
        Assert.Contains(Current.Exits, exit => exit.Unit == "gs");
        Assert.Equal(InstanceStatus.Exited, Current.Unit("gl")!.Status);
        Assert.Equal("german", Current.Ended?.Result?.Winner);
    }

    // R26.3 (A5.5): a crew manning a Gun stacks as a squad; R26.5 (A12.34): an Emplaced Gun and its crew set up hidden in woods with no SSR, together;
    // not in Open Ground, and not the Gun alone. R26.3: a Gun sets up manned.
    [Fact]
    public async Task AGunCrewStacksAsASquadAndAnEmplacedGunHidesInConcealmentTerrain()
    {
        Assert.Empty(ScenarioCards.Read("armor-test", Catalog)!.Diagnostics);
        var card = ScenarioCards.Read("armor-test", Catalog)!.Card!;
        SetupCounter Squad(string id) => new(id, "russian", "russian-1", "defender-squad", "asl:squad", At("bd04:K7:0"), false, false, false, false, true);
        SetupCounter Crew(string id) => new(id, "russian", "russian-1", "defender-crew", "asl:crew", At("bd04:K7:0"), false, false, false, false, true)
        {
            Manning = true
        };
        var report = ScenarioSetup.Check(card, [Squad("a"), Squad("b"), Crew("c"), Crew("d")], _ => "open-ground", _ => true, _ => null, 8);
        Assert.Contains(report.Reasons, reason => reason.StartsWith("play.setup-stacking: russian sets up 4 squad-equivalents", StringComparison.Ordinal));

        Refused(await Place(OnMap("rc", "defender-crew", "russian", "bd04:G10:0"),
            Counter("rg", "defender-at-gun", "russian", new
            {
                at = "bd04:G10:0",
                facing = "north-east"
            })), "play.setup-gun");
        Assert.Equal(0, Revision);
        Refused(await Place(OnMap("rc", "defender-crew", "russian", "bd04:G9:0", null, "asl:hidden"), Gun("rg", "defender-at-gun", "russian", "bd04:G9:0", "north-east", "rc", "asl:hidden")),
            "play.setup-hidden");
        Refused(await Place(OnMap("rc", "defender-crew", "russian", "bd04:G6:0"), Gun("rg", "defender-at-gun", "russian", "bd04:G6:0", "north-east", "rc", "asl:hidden")),
            "play.setup-hidden");
        await SetUp("bd04:G6:0", "asl:hidden");
        Assert.True(Is(Current.Find("rg")!, Conditions.Hidden));
    }

    // R26.5 (A12.34): the hidden Gun fires; a colored dr of 5 with the Germans within 16 hexes reveals it and its crew, a lower one puts both beneath "?".
    [Theory]
    [InlineData(5, false)]
    [InlineData(2, true)]
    public async Task AHiddenEmplacedGunThatFiresIsRevealedOrConcealedByItsColoredDr(int colored, bool concealed)
    {
        await SetUp("bd04:G6:0", "asl:hidden");
        await AdvanceTo("german", "mph");
        Committed(await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "south-east"
        }));
        await Pass();
        Committed(await Drive("ght", "stop"));
        await EndMove();
        foreach (var vehicle in new[] { "gt", "gk" })
        {
            Committed(await Drive(vehicle, "enter", new
            {
                to = vehicle == "gt" ? "bd04:Q1:0" : "bd04:Y1:0",
                facing = "south-west"
            }));
            await Pass();
            Committed(await Drive(vehicle, "stop"));
            await EndMove();
        }

        await AdvanceTo("russian", "pfph");
        dice.Enqueue(colored);
        dice.Enqueue(6);
        var fired = await Act(GameActions.FireOrdnance, new
        {
            gunId = "rg",
            target = "bd04:I1:0",
            targetVehicle = "ght"
        });
        Committed(fired);
        var gun = Current.Find("rg")!;
        var crew = Current.Unit("rc")!;
        Assert.False(Is(gun, Conditions.Hidden));
        Assert.False(Is(crew, Conditions.Hidden));
        Assert.Equal(concealed, Is(gun, Conditions.Concealed));
        Assert.Equal(concealed, Is(crew, Conditions.Concealed));
    }

    // R26.4 (C10.3, A26.221): a crew pushes its Gun off the map with a Manhandling DR; the Gun and crew are Exited, the Gun recorded, and the Russians, who
    // have no exit condition, give the Germans its CVP.
    [Fact]
    public async Task ACrewPushesItsGunOffTheMap()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        foreach (var (vehicle, to) in new[] { ("ght", "bd04:I1:0"), ("gt", "bd04:Q1:0"), ("gk", "bd04:Y1:0") })
        {
            Committed(await Drive(vehicle, "enter", new
            {
                to,
                facing = "south-west"
            }));
            await Pass();
            Committed(await Drive(vehicle, "stop"));
            await EndMove();
        }

        await AdvanceTo("russian", "mph");
        Refused(await Act(GameActions.Move, new
        {
            unitIds = CrewOnly,
            exit = "bottom"
        }), "play.exit-unit");
        dice.Enqueue(1);
        dice.Enqueue(1);
        var pushed = await Act(GameActions.Move, new
        {
            unitIds = CrewOnly,
            exit = "bottom",
            pushGun = "rg"
        });
        Committed(pushed);
        Says(pushed, "Manhandling DR");
        Assert.Equal(InstanceStatus.Exited, Current.Find("rg")!.Status);
        Assert.Contains(Current.Exits, exit => exit.Unit == "rg" && exit.Edge == "bottom");
    }

    // R26.2 (D6.5, D6.1; referee, pass 26): Passengers get off an immobilized vehicle in its next MPh, and a broken one may stay aboard with no need to
    // rout, so Failure to Rout does not eliminate it.
    [Fact]
    public async Task PassengersLeaveAnImmobilizedVehicleAndABrokenOneNeedNotRout()
    {
        await SetUp();
        await AdvanceTo("german", "mph");
        Committed(await Drive("ght", "enter", new
        {
            to = "bd04:I1:0",
            facing = "south-east"
        }));
        await Pass();
        Committed(await Drive("ght", "stop"));
        await Pass();
        Inject(("conditions-changed", new ConditionsChanged("ght", new Dictionary<string, ConditionState> { [Conditions.Immobilized] = ConditionState.True })),
            ("conditions-changed", new ConditionsChanged("gl", new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.True })),
            ("instance-moved", new InstanceMoved("rs1", new MapPosition(At("bd04:I2:0")))));
        Assert.Null(Planner().MustRout(Current, Current.Unit("gl")!));
        await EndMove();
        foreach (var vehicle in new[] { "gt", "gk" })
        {
            Committed(await Drive(vehicle, "enter", new
            {
                to = vehicle == "gt" ? "bd04:Q1:0" : "bd04:Y1:0",
                facing = "south-west"
            }));
            await Pass();
            Committed(await Drive(vehicle, "stop"));
            await EndMove();
        }

        // Its move ended with the immobilization; in its next MPh the squad gets off, and the broken leader stays aboard through the RtPh.
        await AdvanceTo("german", "mph", 2);
        Assert.Equal(InstanceStatus.Active, Current.Unit("gl")!.Status);
        Committed(await Drive("ght", "unload", new
        {
            units = SquadOnly
        }));
        Assert.Null(Current.Aboard("gs"));
        Assert.Equal("ght", Current.Aboard("gl"));
    }

    // R26.6 (C3.2): on a reversed board the Covered Arc is read in the map's frame: a Gun facing north-east covers the hex toward the top of the map.
    [Fact]
    public async Task ACoveredArcOnAReversedBoardIsReadInTheMapsFrame()
    {
        var setup = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-ca",
            expectedRevision = 0,
            start = new
            {
                label = "Covered Arc",
                catalog = "asl-scenario-a1@1.13.0",
                boards = new object[] { new { board = "bd04", column = 0, row = 0, reversed = false }, new { board = "bd02", column = 0, row = 1, reversed = true } },
                firstSide = "german",
                sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
            },
            placements = new object[]
            {
                new Dictionary<string, object> { ["id"] = "dc", ["kind"] = "asl:crew", ["definition"] = "attacker-crew", ["side"] = "german", ["position"] = new { at = "bd02:P5:0" } },
                new Dictionary<string, object>
                {
                    ["id"] = "dg", ["kind"] = "asl:gun", ["definition"] = "attacker-inf-gun", ["side"] = "german", ["position"] = new { at = "bd02:P5:0", facing = "north-east" },
                    ["holding"] = new { holder = "dc", role = "manned" },
                },
            },
        });
        Committed(await Commit(Play(), GameActions.Setup, setup));

        // bd02 is turned: its P4 lies below P5 on the map, and P6 above it.
        Assert.EndsWith("in its CA", Planner().GunTargetStatus(Current, "dg", At("bd02:P6:0")));
        Assert.Contains("Case A", Planner().GunTargetStatus(Current, "dg", At("bd02:P4:0")));
    }
}
