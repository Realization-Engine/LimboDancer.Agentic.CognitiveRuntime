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
/// Pass 25 of the Card Play and Map Studio Redesign Plan (rulings R25.1 to R25.5): entry by advance, named entry hexes and delayed entry, entry through the
/// full movement step, offboard Deployment, and exits in the APh, from Bypass, at the road rate, and by a Guard with its prisoners. Gambit on stand-in
/// boards: board 4 Open Ground but for woods in G1, H0, and I1 (a road across I1's north hexside) and water in C1; board 2 open.
/// </summary>
public sealed class BacklogPass25Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a725");
    private static readonly GameScope Scope = new(Tenant, "p25");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] FirstStack = ["bs9", "bs8", "bs7"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private static readonly BoardHandle Board4 = Board("bd04",
        new Dictionary<string, string>(StringComparer.Ordinal) { ["G1"] = "Woods", ["H0"] = "Woods", ["I1"] = "Woods", ["C1"] = "Water", ["Q1"] = "Marsh" },
        new Dictionary<(string, HexsideDirection), string>
        {
            [("G1", HexsideDirection.NorthEast)] = "Open Ground",
            [("H0", HexsideDirection.SouthWest)] = "Open Ground",
            [("I1", HexsideDirection.North)] = "Dirt Road",
        });

    private static readonly BoardHandle Board2 = Board("bd02", [], []);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p25-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly ScenarioCardLibrary cards;
    private readonly Queue<int> dice = new();
    private readonly ClearLos los = new();

    public BacklogPass25Tests()
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
        Category = name switch
        {
            "Woods" => LosCategory.Woods,
            "Dirt Road" => LosCategory.Road,
            "Water" => LosCategory.Water,
            _ => LosCategory.Open,
        },
    };

    /// <summary>A stand-in board, verified: Open Ground but for the terrain and hexsides named.</summary>
    private static BoardHandle Board(string board, Dictionary<string, string> terrain, Dictionary<(string, HexsideDirection), string> sides)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index).ToString();
            var center = new LocationFacts(0, Type(terrain.GetValueOrDefault(name) ?? "Open Ground"), null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side =>
                new HexsideFacts(side, true, Type(sides.GetValueOrDefault((name, side)) ?? terrain.GetValueOrDefault(name) ?? "Open Ground"), null, false, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse(board), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board4, Board2]), Vocabulary, [Catalog], fireLos: los, cardLibrary: cards);

    /// <summary>The dice the test queues, then 6s.</summary>
    private DiceRoller Roller() => new(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: Roller());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private Dictionary<string, object?> Start(string card) => new()
    {
        ["label"] = card,
        ["catalog"] = "asl-scenario-a1@1.13.0",
        ["boards"] = Ids("bd04"),
        ["sides"] = Array.Empty<object>(),
        ["scenario"] = new
        {
            id = card,
            sha256 = cards.Sha256(card),
            title = card
        },
    };

    private async Task<PlayResult> Place(string card, params Dictionary<string, object>[] placements)
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
            node["start"] = JsonSerializer.SerializeToNode(Start(card));
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

    private Task<PlayResult> Advance() => Act(GameActions.AdvancePhase);

    private Task<PlayResult> Move(string[] ids, string to, object? extra = null)
    {
        var node = JsonSerializer.SerializeToNode(extra ?? new Dictionary<string, object>())!.AsObject();
        node["unitIds"] = JsonSerializer.SerializeToNode(ids);
        node["to"] = to;
        return Act(GameActions.Move, node);
    }

    private async Task EndMove(params string[] ids)
    {
        if (Current.Movement is { WindowOpen: true })
        {
            Committed(await Act(GameActions.PassFire));
        }

        if (Current.Movement is not null)
        {
            Committed(await Act(GameActions.EndMove, new
            {
                unitIds = ids
            }));
        }
    }

    private async Task AdvanceTo(string side, string phase)
    {
        for (var step = 0; step < 40 && (Current.PhasingSide, Current.Phase) != (side, phase); step++)
        {
            Committed(await Advance());
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

    private static (string, EventPayload) MovedTo(string unit, string at) => ("instance-moved", new InstanceMoved(unit, new MapPosition(BoardLocation.Parse(at))));

    private static (string, EventPayload) Set(string unit, string condition, bool value = true) =>
        ("conditions-changed", new ConditionsChanged(unit, new Dictionary<string, ConditionState> { [condition] = value ? ConditionState.True : ConditionState.False }));

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static void Says(PlayResult result, string text) =>
        Assert.True(result.Reasons.Any(reason => reason.Contains(text, StringComparison.Ordinal)), $"'{text}' not in: {string.Join("; ", result.Reasons)}");

    private static string[] Ids(params string[] ids) => ids;

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group, string? entry = null)
    {
        var position = new Dictionary<string, object>();
        if (at == "off")
        {
            position["offMap"] = true;
            if (entry is not null)
            {
                position["entry"] = entry;
            }
        }
        else
        {
            position["at"] = at;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = Catalog.Definition(definition)!.Kind,
            ["definition"] = definition,
            ["side"] = side,
            ["group"] = group,
            ["position"] = position,
            ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
        };
    }

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

    /// <summary>
    /// Gambit set up: the British forward five (b1, b2, l1, l2, and l3, or with <paramref name="leaderOffBoard"/> the squad bs0 instead of l3), the
    /// Germans in hexrows 8 and 9, and the rest of the British off board with their SW.
    /// </summary>
    private async Task SetUpGambit(string card = "gambit", bool leaderOffBoard = false)
    {
        List<Dictionary<string, object>> forward = [Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1"),
            Unit("b2", "british-elite-squad", "bd04:E6:0", "british", "british-1"), Unit("l1", "british-leader-9-1", "bd04:E5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:E6:0", "british", "british-1"),
            leaderOffBoard ? Unit("bs0", "british-elite-squad", "bd04:E7:0", "british", "british-1") : Unit("l3", "british-leader-8-0", "bd04:E7:0", "british", "british-1")];
        Committed(await Place(card, [.. forward]));
        Committed(await Place(card, [.. Enumerable.Range(0, 8).Select(index => Unit($"g{index}", "attacker-elite-squad-5-4-8", $"bd04:{(char)('C' + (index / 2))}9:0", "german", "german-1")),
            Unit("gl1", "attacker-leader-9-2", "bd04:C9:0", "german", "german-1"), Unit("gl2", "attacker-leader-9-1", "bd04:D9:0", "german", "german-1"),
            Unit("gl3", "attacker-leader-8-1", "bd04:E9:0", "german", "german-1")]));
        List<Dictionary<string, object>> offBoard = [.. Enumerable.Range(leaderOffBoard ? 1 : 0, leaderOffBoard ? 9 : 10)
            .Select(index => Unit($"bs{index}", "british-elite-squad", "off", "british", "british-1"))];
        if (leaderOffBoard)
        {
            offBoard.Add(Unit("l3", "british-leader-8-0", "off", "british", "british-1"));
        }

        offBoard.AddRange([Weapon("blmg1", "british-lmg", "bs9", "british"), Weapon("blmg2", "british-lmg", "bs8", "british"),
            Weapon("bmtr1", "british-light-mortar", "bs7", "british"), Weapon("bmtr2", "british-light-mortar", "bs6", "british"), Weapon("batr", "british-atr", "bs5", "british")]);
        Committed(await Place(card, [.. offBoard]));
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Complete);
    }

    // R25.1 (A2.5, A4.7): the British MPh ends with them waiting off board; their APh does not, until they enter by advance, one stack into a hex held
    // by a Known German, where CC follows (A4.14).
    [Fact]
    public async Task UnitsThatWaitedEnterByAdvanceAndTheAdvancePhaseHoldsThem()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Committed(await Advance());
        await AdvanceTo("british", "aph");
        Refused(await Advance(), "play.entry-due");

        var entered = await Act(GameActions.Advance, new
        {
            unitIds = FirstStack,
            to = "bd04:E1:0"
        });
        Committed(entered);
        Says(entered, "from off board");
        Assert.Equal(BoardLocation.Parse("bd04:E1:0"), Current.Location("bs9")!.Location);
        Assert.Equal(BoardLocation.Parse("bd04:E1:0"), Current.Location("blmg1")!.Location);
        Committed(await Act(GameActions.Advance, new
        {
            unitIds = Ids("bs6", "bs5", "bs4"),
            to = "bd04:G1:0"
        }));
        Committed(await Act(GameActions.Advance, new
        {
            unitIds = Ids("bs3", "bs2", "bs1"),
            to = "bd04:K1:0"
        }));
        Refused(await Advance(), "play.entry-due");

        Inject(MovedTo("g0", "bd04:M1:0"));
        var into = await Act(GameActions.Advance, new
        {
            unitIds = Ids("bs0"),
            to = "bd04:M1:0"
        });
        Committed(into);
        Says(into, "CC follows");
        Committed(await Advance());
    }

    // R25.3 (A12.15, A2.51): a stack entering a hex of concealed enemy units reveals one and is forced back off board, its MPh over; it enters by
    // advance in the APh.
    [Fact]
    public async Task AConcealedUnitInTheEntryHexForcesTheStackBackOffBoard()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Inject(MovedTo("g0", "bd04:E1:0"), Set("g0", Conditions.Concealed));

        var attempt = await Move(FirstStack, "bd04:E1:0");
        Committed(attempt);
        Says(attempt, "forced back off board");
        Assert.Null(Current.Location("bs9"));
        Assert.True(Current.Unit("bs9")!.MovementEnded);
        Assert.False(Is(Current.Unit("g0")!, Conditions.Concealed));
        Assert.Null(Current.Movement);
        Refused(await Move(["bs9"], "bd04:K1:0"), "play.move-unit");

        // The Known German now blocks E1 in the MPh (A4.14); others enter elsewhere.
        Refused(await Move(["bs6"], "bd04:E1:0"), "play.entry-occupied");
        Committed(await Move(["bs6", "bs5", "bs4"], "bd04:K1:0"));
        await EndMove("bs6", "bs5", "bs4");
        await AdvanceTo("british", "aph");
        var advanced = await Act(GameActions.Advance, new
        {
            unitIds = FirstStack,
            to = "bd04:E1:0"
        });
        Committed(advanced);
        Says(advanced, "CC follows");
    }

    // R25.3 (A8.22): Residual FP in the entry hex attacks a stack entering it, first.
    [Fact]
    public async Task ResidualFpAttacksAStackEnteringItsHex()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Inject(MovedTo("g0", "bd04:E2:0"));
        Committed(await Move(["bs9"], "bd04:E1:0"));
        Committed(await Act(GameActions.Fire, new
        {
            firers = Ids("g0"),
            target = "bd04:E1:0"
        }));
        Assert.Contains(Current.ResidualFire, item => item.Location == BoardLocation.Parse("bd04:E1:0"));
        await EndMove("bs9");

        var entering = await Move(["bs8"], "bd04:E1:0");
        Committed(entering);
        Says(entering, "Residual FP in bd04:E1:0 attacks the stack first");
    }

    // R25.3 (A4.3, A4.134, A2.52): a stack enters a woods hex of the edge in Bypass and then occupies it; another enters woods by Minimum Move and is
    // pinned and CX; no SMOKE from off board.
    [Fact]
    public async Task BypassAndMinimumMoveAtEntryButNoSmokeFromOffBoard()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        var bypassing = await Move(["bs9"], "bd04:G1:0", new
        {
            bypass = Ids("northeast")
        });
        Committed(bypassing);
        Says(bypassing, "in Bypass along northeast");
        Assert.NotEmpty(Current.Movement!.Bypass!);
        Committed(await Act(GameActions.PassFire));
        Committed(await Move(["bs9"], "bd04:G1:0"));
        await EndMove("bs9");
        Assert.Equal(3, Current.Unit("bs9")!.MfSpent);

        Committed(await Move(["bs8", "bs7"], "bd04:I1:0", new
        {
            minimumMove = true
        }));
        await EndMove("bs8", "bs7");
        Assert.True(Is(Current.Unit("bs8")!, Conditions.Pinned) && Is(Current.Unit("bs8")!, Conditions.Cx));

        Refused(await Move(["bs6"], "bd04:E1:0", new
        {
            smoke = "bd04:E1:0",
            smokeBy = "bs6"
        }), "play.entry-offboard-action");

        // Referee, pass 25 (A5.1, A2.51): no more than three squads enter as one stack.
        Refused(await Move(["bs6", "bs5", "bs4", "bs3"], "bd04:E1:0"), "play.entry-stacking");
    }

    /// <summary>Gambit saved as a user card whose British entry names one hex of board 4.</summary>
    private void SaveNamedEntry(string id, string hex)
    {
        var gambit = ScenarioCards.Read("gambit", Catalog)!.Card!;
        var named = gambit with
        {
            Id = id,
            Sides = [.. gambit.Sides.Select(side => side.Side != "british" ? side : side with
            {
                Groups = [.. side.Groups.Select(group => group with
                {
                    Areas = [.. group.Areas.Select(area => area.Kind == "entry" ? area with { Hexes = [hex], Board = "bd04" } : area)],
                })],
            })],
        };
        Assert.Empty(cards.Save(named, Catalog));
    }

    // R25.1 (table player, pass 25): an entry hex of marsh, never entered in the APh (B16.4), holds no APh; it is entered in the MPh.
    [Fact]
    public async Task AMarshEntryHexDoesNotHoldTheAdvancePhase()
    {
        SaveNamedEntry("gambit-marsh", "Q1");
        await SetUpGambit("gambit-marsh");
        await AdvanceTo("british", "mph");
        Committed(await Advance());
        await AdvanceTo("british", "aph");
        Refused(await Act(GameActions.Advance, new
        {
            unitIds = Ids("bs9"),
            to = "bd04:Q1:0"
        }), "play.advance-marsh");
        Committed(await Advance());
    }

    // R25.2 (A2.5): entry by a named hex; a Known German there blocks it, so the British enter a Game Turn later within four hexes of it, not past the
    // water of C1; a Game Turn after that, within eight.
    [Fact]
    public async Task ABlockedEntryHexDelaysEntryAGameTurnWithinFourHexes()
    {
        SaveNamedEntry("gambit-entry", "E1");
        await SetUpGambit("gambit-entry");
        await AdvanceTo("british", "mph");
        Refused(await Move(["bs9"], "bd04:G1:0"), "play.entry-hex");
        Inject(MovedTo("g0", "bd04:E1:0"));
        Refused(await Move(["bs9"], "bd04:E1:0"), "play.entry-occupied");

        // Nothing can enter, so neither phase holds the British.
        Committed(await Advance());
        await AdvanceTo("british", "aph");
        Committed(await Advance());
        await AdvanceTo("british", "mph");
        Assert.Equal(2, Current.Turn);
        Refused(await Move(["bs9"], "bd04:A1:0"), "play.entry-hex");
        Refused(await Move(["bs9"], "bd04:K1:0"), "play.entry-hex");
        var entry = Planner().EntryFor(Current, Current.Unit("bs9")!)!.Value;
        var third = Planner().EntryHexesFor(Current with
        {
            Turn = 3
        }, entry).Select(at => at.Hex.ToString()).ToArray();
        Assert.Contains("K1", third);
        Assert.Contains("M1", third);
        Assert.DoesNotContain("O1", third);
        Assert.DoesNotContain("A1", third);
        Committed(await Move(["bs9"], "bd04:G1:0"));
    }

    // R25.4 (A2.52, A2.51): a squad waiting off board attempts to Deploy in its RPh with a leader waiting to enter along the same edge; its HS keep the entry.
    [Fact]
    public async Task AnOffBoardSquadDeploysWithAnOffBoardLeader()
    {
        await SetUpGambit(leaderOffBoard: true);
        await AdvanceTo("british", "rph");
        var alone = await Act(GameActions.Deploy, new
        {
            squadId = "bs8"
        });
        Refused(alone, "play.deploy-leader");
        Says(alone, "waiting off board to enter along the top edge");

        dice.Enqueue(1);
        dice.Enqueue(1);
        Committed(await Act(GameActions.Deploy, new
        {
            squadId = "bs9",
            leader = "l3"
        }));
        var halves = Current.Units.Where(unit => unit.Kind == "asl:half-squad" && unit.Status == InstanceStatus.Active).ToArray();
        Assert.Equal(2, halves.Length);
        Assert.All(halves, half => Assert.Equal((1, "top"), Planner().EntryFor(Current, half) is { } entry ? (entry.Turn, entry.Edge) : default));
    }

    // R25.4 (A2.5): an off-board counter may name its group's entry area, and only one its group has; a unit sets up only in its own side's group.
    [Fact]
    public async Task AnOffBoardCounterNamesItsEntryAreaAndAUnitItsOwnSidesGroup()
    {
        Committed(await Place("gambit", Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1")));
        Refused(await Place("gambit", Unit("bs1", "british-elite-squad", "off", "british", "british-1", "nowhere")), "play.setup-entry");
        Committed(await Place("gambit", Unit("bs2", "british-elite-squad", "off", "british", "british-1", "entry")));
        Assert.Equal("entry", (Current.Unit("bs2")!.Position as OffMapPosition)!.Entry);
        Refused(await Place("gambit", Unit("g1", "attacker-elite-squad-5-4-8", "bd04:C9:0", "german", "british-1")), "play.setup-group");
    }

    // R25.5 (A2.6): leaving in the APh by advance, from Bypass with one MF beyond it (the A2.6 EX), and at the road rate across a road hexside (2Y1).
    [Fact]
    public async Task UnitsLeaveByAdvanceFromBypassAndAtTheRoadRate()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Inject(MovedTo("b1", "bd04:H1:0"), MovedTo("b2", "bd04:I1:0"), MovedTo("l3", "bd04:E1:0"));

        // H0's woods are Bypassed along its southwest hexside; its far vertex lies on the top edge.
        Committed(await Move(["b1"], "bd04:H0:0", new
        {
            bypass = Ids("southwest")
        }));
        Committed(await Act(GameActions.PassFire));

        // Table player, pass 25: another stack is told the moving one must end first.
        Refused(await Act(GameActions.Move, new
        {
            unitIds = Ids("b2"),
            exit = "top"
        }), "play.move-order");
        var fromBypass = await Act(GameActions.Move, new
        {
            unitIds = Ids("b1"),
            exit = "top"
        });
        Committed(fromBypass);
        Says(fromBypass, "for 1 MF from Bypass");
        Assert.Equal(InstanceStatus.Exited, Current.Unit("b1")!.Status);

        var road = await Act(GameActions.Move, new
        {
            unitIds = Ids("b2"),
            exit = "top"
        });
        Committed(road);
        Says(road, "for 1 MF at the road rate");
        Says(road, "meets no exit condition of the side, so the units count as eliminated for the enemy's CVP");

        await AdvanceTo("british", "aph");
        Committed(await Act(GameActions.Advance, new
        {
            unitIds = Ids("l3"),
            exit = "top"
        }));
        Assert.Equal(InstanceStatus.Exited, Current.Unit("l3")!.Status);
        Assert.Contains(Current.Exits, exit => exit.Unit == "l3" && exit.Edge == "top");
    }

    // R25.5 (A20.53, A26.221, A26.222, A26.23): a Guard leaves with its prisoner by any edge; off its Friendly Board Edge or its exit condition's edge it is
    // not eliminated for CVP; the prisoner stays captured, and off the exit condition's edge it counts Exit VP for its captor.
    [Fact]
    public async Task AGuardLeavesWithItsPrisonerOffItsFriendlyOrExitEdge()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Inject(MovedTo("b1", "bd04:A3:0"), MovedTo("g1", "bd04:A3:0"), ("instance-captured", new InstanceCaptured("g1", "b1")),
            MovedTo("b2", "bd02:I1:0"), MovedTo("g2", "bd02:I1:0"), ("instance-captured", new InstanceCaptured("g2", "b2")),
            MovedTo("bs9", "bd04:E1:0"), MovedTo("g3", "bd04:E1:0"), ("instance-captured", new InstanceCaptured("g3", "bs9")));

        // Referee, pass 25: b1 may leave with g1 off the west edge, but that is not a British Friendly Board Edge, so b1 counts as eliminated.
        var west = await Act(GameActions.Move, new
        {
            unitIds = Ids("b1"),
            exit = "left"
        });
        Committed(west);
        Says(west, "so the Guard counts as eliminated for the enemy's CVP; its prisoners stay captured");

        // bs9 leaves with g3 off the top edge, the British Friendly Board Edge.
        var escorted = await Act(GameActions.Move, new
        {
            unitIds = Ids("bs9"),
            exit = "top"
        });
        Committed(escorted);
        Says(escorted, "escorting g3");
        Says(escorted, "a Guard escorting prisoners off its side's Friendly Board Edge is not eliminated for CVP");
        Assert.Equal(InstanceStatus.Exited, Current.Unit("g3")!.Status);
        Assert.Contains(Current.Exits, exit => exit.Unit == "g3" && exit.CapturedBy == "british");
        Assert.Contains(Current.Exits, exit => exit.Unit == "bs9" && exit.Escort);

        // b2 leaves with g2 off the south edge beside 2I1, the British exit condition.
        var scoring = await Act(GameActions.Move, new
        {
            unitIds = Ids("b2"),
            exit = "bottom"
        });
        Committed(scoring);
        Says(scoring, "it counts toward the side's Exit VP");
        var standing = Planner().Victory(History)!.Sides;
        var british = standing.Single(item => item.Side == "british");
        var german = standing.Single(item => item.Side == "german");

        // The British: g1, g2, and g3 captured (2 CVP each); Exit VP for b2 and g2 (2 each). The Germans: b1's 2 CVP, none for bs9 or b2.
        Assert.Equal(6, british.Cvp);
        Assert.Equal(4, british.ExitVp);
        Assert.Equal(2, german.Cvp);
        var ended = Planner().Victory(History, ended: true)!.Sides.Single(item => item.Side == "british");
        Assert.Equal((12, 6), (ended.Cvp, ended.ExitVp));
    }

    // Pass 25 (table player, pass 15): once the DEFENDER's fire has eliminated every mover, passing ends the move, and another stack moves.
    [Fact]
    public async Task PassingEndsAMoveWhoseMoversAreAllGone()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        Committed(await Move(["bs9"], "bd04:E1:0"));
        Inject(("instance-eliminated", new InstanceEliminated("bs9")));
        Refused(await Act(GameActions.EndMove), "play.end-move: the DEFENDER's window at bd04:E1:0 is open; the DEFENDER fires or passes first");
        var passed = await Act(GameActions.PassFire);
        Committed(passed);
        Says(passed, "every mover eliminated");
        Assert.Null(Current.Movement);
        Committed(await Move(["bs8"], "bd04:G1:0"));
    }
}
