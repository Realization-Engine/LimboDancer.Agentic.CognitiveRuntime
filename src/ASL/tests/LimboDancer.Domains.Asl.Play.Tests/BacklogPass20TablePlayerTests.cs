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
using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// A table player tries pass 20 (rulings R20.1 to R20.6): the three cards played through the planner as players would, looking for dead ends, illegal
/// moves accepted, legal moves refused, and unclear refusals. Each test states what a player expects.
/// </summary>
public sealed class BacklogPass20TablePlayerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-0000000b7220");
    private static readonly GameScope Scope = new(Tenant, "tp20");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] RouteD4D3 = ["bd04:D4:0", "bd04:D3:0"];
    private static readonly string[] RouteN10 = ["bd01:N10:0"];
    private static readonly string[] RouteP10 = ["bd01:P10:0"];
    private static readonly string[] RouteO9O8 = ["bd01:O9:0", "bd01:O8:0"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-tp20-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Queue<int> dice = new();
    private readonly ITestOutputHelper output;

    public BacklogPass20TablePlayerTests(ITestOutputHelper output)
    {
        this.output = output;
        store = new FileGameStore(root);
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

    private static BoardHandle OpenBoard(string board)
    {
        var open = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var center = new LocationFacts(0, open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, 0, false, center, [center], sides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse(board), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle(), OpenBoard("bd04"), OpenBoard("bd02")]), Vocabulary, [Catalog],
        fireLos: new ClearLos());

    private DiceRoller Roller() => new(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: Roller());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private PlayResult Log(string what, PlayResult result)
    {
        output.WriteLine($"{what}: {result.Outcome} | {string.Join(" | ", result.Reasons)}");
        return result;
    }

    private static Dictionary<string, object?> Start(string card, string[] boards, object? balance = null)
    {
        var start = new Dictionary<string, object?>
        {
            ["label"] = card,
            ["catalog"] = "asl-scenario-a1@1.13.0",
            ["boards"] = boards,
            ["sides"] = Array.Empty<object>(),
            ["scenario"] = new
            {
                id = card,
                sha256 = ScenarioCards.Sha256(card),
                title = card
            },
        };
        if (balance is not null)
        {
            start["balance"] = balance;
        }

        return start;
    }

    private async Task<PlayResult> PlaceWith(Dictionary<string, object?> start, params Dictionary<string, object>[] placements)
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
            node["start"] = JsonSerializer.SerializeToNode(start);
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

    private async Task AdvanceTo(string side, string phase)
    {
        for (var step = 0; step < 40 && (Current.PhasingSide, Current.Phase) != (side, phase); step++)
        {
            Committed(Log($"advance from {Current.PhasingSide} {Current.Phase} turn {Current.Turn}", await Advance()));
        }

        Assert.Equal((side, phase), (Current.PhasingSide, Current.Phase));
    }

    private Task<PlayResult> Move(string[] ids, string to, bool doubleTime = false, bool assault = false) => Act(GameActions.Move, new
    {
        unitIds = ids,
        to,
        doubleTime,
        assault
    });

    /// <summary>A step and the DEFENDER's pass, as at the table.</summary>
    private async Task<PlayResult> Step(string[] ids, string to, bool doubleTime = false, bool assault = false)
    {
        var moved = Log($"move {string.Join(",", ids)} to {to}", await Move(ids, to, doubleTime, assault));
        if (moved.Outcome == PlayOutcome.Committed)
        {
            Log("  pass", await Act(GameActions.PassFire));
        }

        return moved;
    }

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group, bool broken = false) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = at == "off" ? new Dictionary<string, object> { ["offMap"] = true } : new Dictionary<string, object> { ["at"] = at },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = broken, ["asl:concealed"] = false, ["asl:hidden"] = false },
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

    private static void MoveTo(List<Dictionary<string, object>> placements, string id, string hex) =>
        placements.Single(item => (string)item["id"] == id)["position"] = new Dictionary<string, object> { ["at"] = $"bd01:{hex}:0" };

    // ---------------------------------------------------------------- Gambit

    private static readonly string[] Bd04 = ["bd04"];

    /// <summary>
    /// Gambit: the British five in hexrows 5 to 7 (three squads, two 8-0), the Germans in hexrows 8 to 10 (a 9-2 and a squad in C8 to dash north), and
    /// the rest of the British off board: nine squads (or the HS of one Deployed), the 9-1, and the five SW.
    /// </summary>
    private async Task SetUpGambit(bool deployOne = false, object? balance = null, params Dictionary<string, object>[] germanExtra)
    {
        var start = Start("gambit", Bd04, balance);
        Committed(Log("british five", await PlaceWith(start, Unit("b1", "british-elite-squad", "bd04:D5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:D5:0", "british", "british-1"), Unit("b2", "british-elite-squad", "bd04:D6:0", "british", "british-1"),
            Unit("l3", "british-leader-8-0", "bd04:D6:0", "british", "british-1"), Unit("b3", "british-elite-squad", "bd04:D7:0", "british", "british-1"))));
        List<Dictionary<string, object>> german = [Unit("g0", "attacker-elite-squad-5-4-8", "bd04:C8:0", "german", "german-1"),
            Unit("gl1", "attacker-leader-9-2", "bd04:C8:0", "german", "german-1"),
            .. Enumerable.Range(1, 7).Select(index => Unit($"g{index}", "attacker-elite-squad-5-4-8", $"bd04:{(char)('D' + index)}9:0", "german", "german-1")),
            Unit("gl2", "attacker-leader-9-1", "bd04:E9:0", "german", "german-1"), Unit("gl3", "attacker-leader-8-1", "bd04:F9:0", "german", "german-1"), .. germanExtra];
        Committed(Log("germans", await PlaceWith(start, [.. german])));
        var british = new List<Dictionary<string, object>> { Unit("l1", "british-leader-9-1", "off", "british", "british-1") };
        for (var index = 1; index <= 9; index++)
        {
            if (deployOne && index == 1)
            {
                british.Add(Unit("bh1a", "british-elite-half-squad", "off", "british", "british-1"));
                british.Add(Unit("bh1b", "british-elite-half-squad", "off", "british", "british-1"));
            }
            else
            {
                british.Add(Unit($"bs{index}", "british-elite-squad", "off", "british", "british-1"));
            }
        }

        british.AddRange([Weapon("blmg1", "british-lmg", "bs9", "british"), Weapon("blmg2", "british-lmg", "bs8", "british"),
            Weapon("bmtr1", "british-light-mortar", "bs7", "british"), Weapon("bmtr2", "british-light-mortar", "bs6", "british"), Weapon("batr", "british-atr", "bs5", "british")]);
        Committed(Log("british off board", await PlaceWith(start, [.. british])));
    }

    // 1. A2.51, A4.12, A4.5, A4.61: a whole Gambit start through the British entry: a German dash to the north edge, entry refused in the RPh and PFPh,
    // into the German-held hex, and off the edge; a leader with a SW-carrying squad gets the leader's +2 MF from entry; Double Time; Assault.
    [Fact]
    public async Task GambitStartThroughTheBritishEntry()
    {
        await SetUpGambit();
        Assert.Equal("german", Current.FirstSide);
        await AdvanceTo("german", "mph");

        // The 9-2 and a 5-4-8 Double Time from C8 to C1 (4 + 2 + 2 MF, seven hexes).
        string[] dash = ["g0", "gl1"];
        Committed(await Step(dash, "bd04:C7:0", doubleTime: true));
        foreach (var row in new[] { 6, 5, 4, 3, 2, 1 })
        {
            Committed(await Step(dash, $"bd04:C{row}:0"));
        }

        Log("german end move", await Act(GameActions.EndMove, new
        {
            unitIds = dash
        }));
        await AdvanceTo("british", "rph");
        string[] bs1 = ["bs1"];
        var inRph = Log("enter in RPh", await Move(bs1, "bd04:E1:0"));
        Assert.NotEqual(PlayOutcome.Committed, inRph.Outcome);
        await AdvanceTo("british", "pfph");
        var inPfph = Log("enter in PFPh", await Move(bs1, "bd04:E1:0"));
        Assert.NotEqual(PlayOutcome.Committed, inPfph.Outcome);
        await AdvanceTo("british", "mph");
        output.WriteLine("top entry hexes: " + string.Join(" ", Planner().EntryHexes(Current, "top").Select(at => at.Hex.ToString())));

        Refused(Log("enter the German-held C1", await Move(bs1, "bd04:C1:0")), "play.entry-occupied");
        Refused(Log("enter mid-board", await Move(bs1, "bd02:E5:0")), "play.entry-edge");

        // The 9-1 with a squad carrying a LMG: 4 + 2 MF, the entry the first.
        string[] withLeader = ["bs9", "l1"];
        Committed(await Step(withLeader, "bd04:E1:0"));
        foreach (var row in new[] { 2, 3, 4, 5, 6 })
        {
            Committed(await Step(withLeader, $"bd04:E{row}:0"));
        }

        Refused(Log("seventh MF", await Move(withLeader, "bd04:E7:0")), "play.move-mf");
        Assert.Equal(BoardLocation.Parse("bd04:E6:0"), Current.Location("blmg1")!.Location);
        Committed(Log("end move", await Act(GameActions.EndMove, new
        {
            unitIds = withLeader
        })));

        // Double Time at entry: CX, 6 MF.
        string[] runner = ["bs8"];
        Committed(await Step(runner, "bd04:G1:0", doubleTime: true));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("bs8")!, Conditions.Cx));
        foreach (var row in new[] { 2, 3, 4, 5, 6 })
        {
            Committed(await Step(runner, $"bd04:G{row}:0"));
        }

        Refused(Log("DT seventh MF", await Move(runner, "bd04:G7:0")), "play.move-mf");
        Committed(await Act(GameActions.EndMove, new
        {
            unitIds = runner
        }));

        // Assault Movement at entry: one Location only.
        string[] assault = ["bs7"];
        Committed(await Step(assault, "bd04:I1:0", assault: true));
        Assert.NotEqual(PlayOutcome.Committed, Log("assault second step", await Move(assault, "bd04:I2:0")).Outcome);
        Committed(await Act(GameActions.EndMove, new
        {
            unitIds = assault
        }));
    }

    // 2. A2.5, A4.2: a player ends the move of a stack still off board before it has entered; the MPh then must still be endable, or the entry possible.
    [Fact]
    public async Task EndingTheMoveOfAWaitingStackIsNoDeadEnd()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        string[] waiting = ["bs1"];
        var ended = Log("end move off board", await Act(GameActions.EndMove, new
        {
            unitIds = waiting
        }));
        var entered = Log("then enter", await Move(waiting, "bd04:M1:0"));
        output.WriteLine($"bs1 MovementEnded={Current.Unit("bs1")!.MovementEnded}");

        // A player expects either the end-move refused, or the unit still able to enter.
        Assert.True(ended.Outcome != PlayOutcome.Committed || entered.Outcome == PlayOutcome.Committed,
            "A stack whose move ended off board can neither enter nor let the MPh end: " + string.Join("; ", entered.Reasons));
    }

    // 3. A2.9, A2.51: one of nine entering squads Deployed; the two HS enter apart and together; a HS with a squad.
    [Fact]
    public async Task DeployedHalfSquadsEnter()
    {
        await SetUpGambit(deployOne: true);
        await AdvanceTo("british", "mph");
        string[] one = ["bh1a"];
        Committed(await Step(one, "bd04:M1:0"));
        Committed(await Act(GameActions.EndMove, new
        {
            unitIds = one
        }));
        string[] pair = ["bh1b", "bs2"];
        Committed(await Step(pair, "bd04:O1:0"));
        Committed(await Act(GameActions.EndMove, new
        {
            unitIds = pair
        }));
    }

    // 4. A2.52 (ruling R25.4, pass 25): a waiting squad attempts to Deploy off board in its RPh with a leader waiting along the same edge; the squad or
    // its two HS then enter with him.
    [Fact]
    public async Task DeployingOffBoardIsAttemptedWithALeaderWaitingAlongTheEdge()
    {
        await SetUpGambit();
        await AdvanceTo("british", "rph");
        var deploy = Log("deploy off board", await Act(GameActions.Deploy, new
        {
            squadId = "bs1",
            leader = "l1"
        }));
        Committed(deploy);
        await AdvanceTo("british", "mph");
        string[] movers = Current.Unit("bs1") is { Status: InstanceStatus.Active } ? ["bs1", "l1"]
            : [.. Current.Units.Where(unit => unit.Kind == "asl:half-squad" && unit.Status == InstanceStatus.Active).Select(unit => unit.Id), "l1"];
        Committed(await Step(movers, "bd04:M1:0"));
    }

    // 5. A2.5: every unit enters, then the MPh ends; a few more phases and the German Turn 2 with fire and a rout.
    [Fact]
    public async Task GambitPlaysOnAfterTheEntry()
    {
        await SetUpGambit();
        await AdvanceTo("british", "mph");
        string[][] stacks = [["bs9", "bs8", "bs7"], ["bs6", "bs5", "l1"], ["bs4", "bs3"], ["bs2", "bs1"]];
        string[] hexes = ["bd04:K1:0", "bd04:M1:0", "bd04:O1:0", "bd04:Q1:0"];
        for (var index = 0; index < stacks.Length; index++)
        {
            Committed(await Step(stacks[index], hexes[index]));
            Committed(await Act(GameActions.EndMove, new
            {
                unitIds = stacks[index]
            }));
        }

        Committed(Log("end British MPh", await Advance()));
        await AdvanceTo("german", "pfph");
        Assert.Equal(2, Current.Turn);

        // German Prep Fire from E9 at the 4-4-8 and 8-0 in D5, four hexes off.
        foreach (var die in new[] { 2, 2, 6, 5, 6, 5 })
        {
            dice.Enqueue(die);
        }

        string[] firers = ["g1"];
        Committed(Log("german fire at D5", await Act(GameActions.Fire, new
        {
            firers,
            target = "bd04:D5:0"
        })));
        var hit = Current.At(BoardLocation.Parse("bd04:D5:0")).OfType<UnitInstance>().ToArray();
        output.WriteLine("D5 now: " + string.Join(", ", hit.Select(unit => $"{unit.Id} {unit.Definition?.Definition} broken={GameState.Condition(unit, Conditions.Broken)}")));
        await AdvanceTo("german", "rtph");
        foreach (var unit in hit.Where(unit => GameState.Condition(unit, Conditions.Broken) == ConditionState.True))
        {
            output.WriteLine($"{unit.Id} must rout: {Planner().MustRout(Current, Current.Unit(unit.Id)!)}");
            foreach (var die in new[] { 1, 1, 1, 1, 1, 1 })
            {
                dice.Enqueue(die);
            }

            Log($"rout {unit.Id} D5 to D4, D3", await Act(GameActions.Rout, new
            {
                unitId = unit.Id,
                route = RouteD4D3
            }));
        }

        await AdvanceTo("british", "rph");
        Assert.Equal(2, Current.Turn);
        foreach (var unit in hit)
        {
            output.WriteLine($"{unit.Id} at {Current.Location(unit.Id)?.Location}, status {Current.Unit(unit.Id)?.Status}");
        }
    }

    // 6. R20.3, R20.4: Gambit's British Balance (foxholes) is recorded; the German LMG is then refused.
    [Fact]
    public async Task GambitsBritishBalanceIsRecordedAndTheGermanLmgRefused()
    {
        var start = Start("gambit", Bd04, new
        {
            side = "british"
        });
        Committed(await PlaceWith(start, Unit("b1", "british-elite-squad", "bd04:D5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:D5:0", "british", "british-1"), Unit("b2", "british-elite-squad", "bd04:D6:0", "british", "british-1"),
            Unit("l3", "british-leader-8-0", "bd04:D6:0", "british", "british-1"), Unit("b3", "british-elite-squad", "bd04:D7:0", "british", "british-1")));
        Assert.Equal("british", Current.Scenario!.Balance);
        Refused(Log("german lmg without the Balance", await PlaceWith(start, Unit("g0", "attacker-elite-squad-5-4-8", "bd04:C8:0", "german", "german-1"),
            Weapon("glmg", "attacker-lmg", "g0", "german"))), "play.setup-pool");
    }

    // 7. R20.4: Gambit's German Balance LMG joins the German OB; play starts.
    [Fact]
    public async Task GambitsGermanBalanceLmgIsSetUpAndPlayStarts()
    {
        await SetUpGambit(balance: new
        {
            side = "german"
        }, germanExtra: Weapon("glmg", "attacker-lmg", "g1", "german"));
        Assert.Equal("german", Current.Scenario!.Balance);
        Committed(Log("start play", await Advance()));
    }

    // 8. R20.4: the Balance granted, a player forgets its counter: the game should say so before play starts, as for any OB counter (A2.9).
    [Fact]
    public async Task AForgottenBalanceCounterIsNamedBeforePlay()
    {
        await SetUpGambit(balance: new
        {
            side = "german"
        });
        var started = Log("start play without the Balance LMG", await Advance());
        Assert.NotEqual(PlayOutcome.Committed, started.Outcome);
    }

    // ---------------------------------------------------------------- The Guards Counterattack

    private async Task SetUpGuards(object? balance = null, params Dictionary<string, object>[] extra)
    {
        var card = Card("guards-counterattack");
        var start = Start("guards-counterattack", ["bd01"], balance);
        Committed(Log("germans", await PlaceWith(start, [.. Ob(card, "german", 0), .. extra])));
        Committed(Log("russians", await PlaceWith(start, [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)])));
    }

    // 9. A3.9, A2.1: The Guards Counterattack played to its end with moves; advance at the playable edge refused, inside accepted; then game over.
    [Fact]
    public async Task TheGuardsCounterattackToItsEnd()
    {
        await SetUpGuards();
        await AdvanceTo("russian", "mph");
        var squad = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && Current.Location(unit.Id)!.Location.Hex.ToString() == "N3").Id;
        string[] mover = [squad];
        Committed(await Step(mover, "bd01:O3:0"));
        Committed(Log("end move", await Act(GameActions.EndMove, new
        {
            unitIds = mover
        })));
        for (var step = 0; step < 300 && Current.Ended is null; step++)
        {
            Committed(await Advance());
        }

        Assert.Equal((5, "last-game-turn"), (Current.Ended!.Turn, Current.Ended.Reason));
        Assert.Equal("german", Current.PhasingSide);
        Refused(Log("move after the end", await Move(mover, "bd01:O4:0")), "play.game-over");
        Refused(Log("setup after the end", await PlaceWith(Start("guards-counterattack", ["bd01"]), Unit("x", "attacker-squad", "bd01:F5:0", "german", "german-1"))), "play.");
    }

    // 10. A2.1, A4.7: a Russian at the P edge advances into Q: refused.
    [Fact]
    public async Task AnAdvanceAcrossThePlayableEdgeIsRefused()
    {
        await SetUpGuards();
        await AdvanceTo("russian", "mph");
        var squad = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && Current.Location(unit.Id)!.Location.Hex.ToString() == "N3").Id;
        string[] mover = [squad];
        Committed(await Step(mover, "bd01:O3:0", doubleTime: true));
        Committed(await Step(mover, "bd01:P3:0"));
        Committed(await Act(GameActions.EndMove, new
        {
            unitIds = mover
        }));
        await AdvanceTo("russian", "aph");
        Refused(Log("advance P3 to Q3", await Act(GameActions.Advance, new
        {
            unitIds = mover,
            to = "bd01:Q3:0"
        })), "play.playable");
        Committed(Log("advance P3 to P4", await Act(GameActions.Advance, new
        {
            unitIds = mover,
            to = "bd01:P4:0"
        })));
    }

    // 11. R20.3: players who wish different sides: no Balance, and the Hero is refused; the same player named twice is refused.
    [Fact]
    public async Task DifferentWishesGiveNoBalance()
    {
        var card = Card("guards-counterattack");
        var balance = new
        {
            players = new[] { new { name = "Ann", wants = "german" }, new { name = "Ben", wants = "russian" } }
        };
        Refused(Log("hero without Balance", await PlaceWith(Start("guards-counterattack", ["bd01"], balance), [.. Ob(card, "german", 0),
            Unit("hero", "attacker-hero", "bd01:F5:0", "german", "german-1")])), "play.setup-pool");
        Committed(await PlaceWith(Start("guards-counterattack", ["bd01"], balance), [.. Ob(card, "german", 0)]));
        Assert.Null(Current.Scenario!.Balance);
        Assert.Equal(2, Current.Scenario.Players.Count);
    }

    // 12. A2.9: a card game should not let a counter set up broken.
    [Fact]
    public async Task NoCounterSetsUpBroken()
    {
        var card = Card("guards-counterattack");
        var german = Ob(card, "german", 0);
        german[0]["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        var result = Log("broken at setup", await PlaceWith(Start("guards-counterattack", ["bd01"]), [.. german]));
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
    }

    // ---------------------------------------------------------------- The Tractor Works

    private async Task SetUpTractor(object? balance, bool hero, Action<List<Dictionary<string, object>>>? stahler = null)
    {
        var card = Card("tractor-works");
        var start = Start("tractor-works", ["bd01"], balance);
        Committed(Log("308th", await PlaceWith(start, [.. Ob(card, "russian", 0)])));
        Committed(Log("company A", await PlaceWith(start, [.. Ob(card, "german", 0)])));
        var second = Ob(card, "german", 1);
        stahler?.Invoke(second);
        if (hero)
        {
            second.Add(Unit("hero", "attacker-hero", "bd01:T2:0", "german", "german-2"));
        }

        Committed(Log("stahler", await PlaceWith(start, [.. second])));
        Committed(Log("tienham", await PlaceWith(start, [.. Ob(card, "german", 2)])));
        Committed(Log("295th", await PlaceWith(start, [.. Ob(card, "russian", 1)])));
    }

    // 13. R20.2, R20.3, R20.4 (A26.4, A3.9): The Tractor Works: first-move dr (a tie rerolled), then both wish the Russians: Ann wins them, Ben's Germans
    // take the Hero; the Hero sets up with Stahler; the game ends after the second side's Player Turn of Turn 8.
    [Fact]
    public async Task TheTractorWorksRollAndBalanceHero()
    {
        var balance = new
        {
            players = new[] { new { name = "Ann", wants = "russian" }, new { name = "Ben", wants = "russian" } }
        };
        foreach (var die in new[] { 4, 4, 2, 5, 5, 1 })
        {
            dice.Enqueue(die);
        }

        await SetUpTractor(balance, hero: true);
        Assert.Equal("german", Current.FirstSide);
        Assert.Equal("german", Current.Scenario!.Balance);
        Assert.Equal([new ScenarioPlayer("Ann", "russian"), new ScenarioPlayer("Ben", "german")], Current.Scenario.Players);
        Assert.Equal(BoardLocation.Parse("bd01:T2:0"), Current.Location("hero")!.Location);
        for (var step = 0; step < 400 && Current.Ended is null; step++)
        {
            Committed(await Advance());
        }

        Assert.Equal(8, Current.Ended!.Turn);
        Assert.Equal(("russian", "ccph"), (Current.PhasingSide, Current.Phase));
    }

    // 14. R20.4: the Hero in a Russian area, or with the 308th, is refused.
    [Fact]
    public async Task TheHeroSetsUpOnlyWithAGermanGroup()
    {
        var card = Card("tractor-works");
        var start = Start("tractor-works", ["bd01"], new
        {
            side = "german"
        });
        Committed(await PlaceWith(start, [.. Ob(card, "russian", 0)]));
        Committed(await PlaceWith(start, [.. Ob(card, "german", 0)]));
        Assert.NotEqual(PlayOutcome.Committed, Log("hero in X3", await PlaceWith(start, Unit("hero", "attacker-hero", "bd01:X3:0", "german", "german-2"))).Outcome);
        Assert.NotEqual(PlayOutcome.Committed, Log("hero as russian group", await PlaceWith(start, Unit("hero", "attacker-hero", "bd01:T2:0", "german", "russian-1"))).Outcome);
    }

    // 15. A2.1, A10.51: a broken German in O10, next to the unplayable N-row woods, must rout; the nearest cover (N10) is outside the playable area.
    // A player expects the rout to seek the nearest cover inside the area (or any legal route), not a dead end that eliminates the unit.
    [Fact]
    public async Task ARoutAtThePlayableEdgeIsNotADeadEnd()
    {
        foreach (var die in new[] { 1, 6 })
        {
            dice.Enqueue(die);
        }

        await SetUpTractor(new
        {
            side = "german"
        }, hero: true, stahler: list => MoveTo(list, "german-2-7", "R7"));
        Assert.Equal("german", Current.FirstSide);
        await AdvanceTo("german", "mph");
        string[] runner = ["german-2-7"];
        Committed(await Step(runner, "bd01:Q8:0"));
        Committed(await Step(runner, "bd01:Q9:0"));
        Committed(await Step(runner, "bd01:P9:0"));
        Committed(Log("to O10", await Move(runner, "bd01:O10:0")));

        // Russian Defensive First Fire from P8.
        var firer = Current.At(BoardLocation.Parse("bd01:P8:0")).OfType<UnitInstance>().First(unit => unit.Kind == "asl:squad").Id;
        foreach (var die in new[] { 4, 3, 6, 5 })
        {
            dice.Enqueue(die);
        }

        Log("first fire at O10", await Act(GameActions.Fire, new
        {
            firers = new[] { firer },
            target = "bd01:O10:0"
        }));
        var broken = Current.At(BoardLocation.Parse("bd01:O10:0")).OfType<UnitInstance>().FirstOrDefault(unit => GameState.Condition(unit, Conditions.Broken) == ConditionState.True);
        Assert.NotNull(broken);
        Log("end move", await Act(GameActions.EndMove, new
        {
            unitIds = new[] { broken.Id }
        }));
        await AdvanceTo("german", "rtph");
        output.WriteLine($"must rout: {Planner().MustRout(Current, Current.Unit(broken.Id)!)}");
        Refused(Log("rout to N10", await Act(GameActions.Rout, new
        {
            unitId = broken.Id,
            route = RouteN10
        })), "play.playable");
        dice.Enqueue(1);
        dice.Enqueue(1);
        var away = Log("rout to P10", await Act(GameActions.Rout, new
        {
            unitId = broken.Id,
            route = RouteP10
        }));
        var along = Log("rout to O9 then O8", await Act(GameActions.Rout, new
        {
            unitId = broken.Id,
            route = RouteO9O8
        }));
        Assert.True(new[] { away, along }.Any(result => result.Outcome == PlayOutcome.Committed) || new[] { away, along }.All(result => !result.Reasons.Any(reason => reason.Contains("N10", StringComparison.Ordinal))),
            "Every in-area rout is refused because the nearest cover named is N10, outside the playable area");
    }

    // 15b. Fixed after the table player (ruling R20.6): the rout advice names cover within the playable area only, so the unit is not left to die.
    [Fact]
    public async Task ARoutAtThePlayableEdgeSeeksCoverWithinIt()
    {
        foreach (var die in new[] { 1, 6 })
        {
            dice.Enqueue(die);
        }

        await SetUpTractor(new
        {
            side = "german"
        }, hero: true, stahler: list => MoveTo(list, "german-2-7", "R7"));
        await AdvanceTo("german", "mph");
        string[] runner = ["german-2-7"];
        Committed(await Step(runner, "bd01:Q8:0"));
        Committed(await Step(runner, "bd01:Q9:0"));
        Committed(await Step(runner, "bd01:P9:0"));
        Committed(await Move(runner, "bd01:O10:0"));
        var firer = Current.At(BoardLocation.Parse("bd01:P8:0")).OfType<UnitInstance>().First(unit => unit.Kind == "asl:squad").Id;
        foreach (var die in new[] { 4, 3, 6, 5 })
        {
            dice.Enqueue(die);
        }

        Committed(await Act(GameActions.Fire, new
        {
            firers = new[] { firer },
            target = "bd01:O10:0"
        }));
        var broken = Current.At(BoardLocation.Parse("bd01:O10:0")).OfType<UnitInstance>().Single(unit => GameState.Condition(unit, Conditions.Broken) == ConditionState.True);
        await AdvanceTo("german", "rtph");
        var (targets, canRout, _) = Planner().RoutAdvice(Current, Current.Unit(broken.Id)!);
        output.WriteLine($"{broken.Id}: may rout {canRout}, to {string.Join(", ", targets)}");
        Assert.True(canRout);
        Assert.All(targets, target => Assert.True(ScenarioCards.Playable(Card("tractor-works"), target)));
    }

    // 16. A2.1: in The Tractor Works, a Russian of the 295th in P3 steps to O3, then N3 is refused.
    [Fact]
    public async Task TheTractorWorksWestEdgeHolds()
    {
        foreach (var die in new[] { 6, 1 })
        {
            dice.Enqueue(die);
        }

        await SetUpTractor(null, hero: false);
        Assert.Equal("russian", Current.FirstSide);
        await AdvanceTo("russian", "mph");
        var squad = Current.At(BoardLocation.Parse("bd01:P3:0")).OfType<UnitInstance>().First(unit => unit.Kind == "asl:squad").Id;
        string[] mover = [squad];
        Committed(await Step(mover, "bd01:O3:0"));
        Refused(Log("N3", await Move(mover, "bd01:N3:0")), "play.playable");
    }

    // 17. R20.3: malformed Balance requests are refused with a clear reason; the Russian Balance (sewer movement) is recorded by agreement.
    [Fact]
    public async Task BalanceRequestsAreCheckedAndTheRussianBalanceIsRecorded()
    {
        var card = Card("guards-counterattack");
        Log("side and players", await PlaceWith(Start("guards-counterattack", ["bd01"], new
        {
            side = "german",
            players = new[] { new { name = "Ann", wants = "german" }, new { name = "Ben", wants = "german" } }
        }), [.. Ob(card, "german", 0)]));
        Log("unknown side", await PlaceWith(Start("guards-counterattack", ["bd01"], new
        {
            side = "french"
        }), [.. Ob(card, "german", 0)]));
        Log("same name twice", await PlaceWith(Start("guards-counterattack", ["bd01"], new
        {
            players = new[] { new { name = "Ann", wants = "german" }, new { name = "Ann", wants = "german" } }
        }), [.. Ob(card, "german", 0)]));
        Assert.Equal(0, Revision);
        Committed(Log("russian balance", await PlaceWith(Start("guards-counterattack", ["bd01"], new
        {
            side = "russian"
        }), [.. Ob(card, "german", 0)])));
        Assert.Equal("russian", Current.Scenario!.Balance);
    }

    // 18. R20.4, R19.2: the Hero added after the German groups finished, while no later group has begun, then after the 295th began.
    [Fact]
    public async Task TheHeroAddedLate()
    {
        foreach (var die in new[] { 1, 6 })
        {
            dice.Enqueue(die);
        }

        var card = Card("tractor-works");
        var start = Start("tractor-works", ["bd01"], new
        {
            side = "german"
        });
        Committed(await PlaceWith(start, [.. Ob(card, "russian", 0)]));
        Committed(await PlaceWith(start, [.. Ob(card, "german", 0)]));
        Committed(await PlaceWith(start, [.. Ob(card, "german", 1)]));
        Committed(await PlaceWith(start, [.. Ob(card, "german", 2)]));
        Log("hero after the German groups", await PlaceWith(start, Unit("hero", "attacker-hero", "bd01:T2:0", "german", "german-2")));
        Committed(await PlaceWith(start, [.. Ob(card, "russian", 1)]));
        Log("advance with every group done", await Advance());
    }
}
