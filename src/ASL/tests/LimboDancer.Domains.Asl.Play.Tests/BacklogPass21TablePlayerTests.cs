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
/// A table player tries pass 21 (rulings R21.1 to R21.5): Control, CVP, Exit VP, and the result of the three cards, played through the planner as
/// players would where the engine allows it, and read from changed states where a position is hard to reach in play.
/// </summary>
public sealed class BacklogPass21TablePlayerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-0000000c7221");
    private static readonly GameScope Scope = new(Tenant, "tp21");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd04 = ["bd04"];
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-tp21-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Queue<int> dice = new();
    private readonly ITestOutputHelper output;

    public BacklogPass21TablePlayerTests(ITestOutputHelper output)
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

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private VictoryReport Report => Planner().Victory(History)!;

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

    private static Dictionary<string, object?> Start(string card, string[] boards)
    {
        return new Dictionary<string, object?>
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
            Committed(await Advance());
        }

        Assert.Equal((side, phase), (Current.PhasingSide, Current.Phase));
    }

    private Task<PlayResult> Move(string[] ids, string to, object? bypass = null) => bypass is null
        ? Act(GameActions.Move, new
        {
            unitIds = ids,
            to
        })
        : Act(GameActions.Move, new
        {
            unitIds = ids,
            to,
            bypass
        });

    private Task<PlayResult> Exit(string[] ids, string edge) => Act(GameActions.Move, new
    {
        unitIds = ids,
        exit = edge
    });

    private Task<PlayResult> EndMove(string[] ids) => Act(GameActions.EndMove, new
    {
        unitIds = ids
    });

    private Task<PlayResult> Pass() => Act(GameActions.PassFire);

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
        ["position"] = at == "off" ? new Dictionary<string, object> { ["offMap"] = true } : new Dictionary<string, object> { ["at"] = at },
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

    private async Task SetUpGuards()
    {
        var card = Card("guards-counterattack");
        var start = Start("guards-counterattack", Bd01);
        Committed(await PlaceWith(start, [.. Ob(card, "german", 0)]));
        Committed(await PlaceWith(start, [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
    }

    private static BoardLocation L(string at) => BoardLocation.Parse(at);

    private static UnitInstance[] UnitsAt(GameState state, string at) =>
        [.. state.At(L(at)).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active)];

    private static bool Same(BoardLocation one, BoardLocation two) => one.Board == two.Board && one.Hex == two.Hex;

    private static bool Occupied(GameState state, BoardLocation at, ICollection<string> own) =>
        state.Units.Any(unit => unit.Status == InstanceStatus.Active && !own.Contains(unit.Id) && !Vocabulary.IsA(unit.Kind, "asl:equipment")
            && state.Location(unit.Id)?.Location is { } there && Same(there, at));

    /// <summary>A shortest path of unoccupied ground Locations from the sources to a goal; the first element is the first hex to enter.</summary>
    private List<BoardLocation>? PathTo(GameState state, IEnumerable<BoardLocation> sources, bool sourcesAreSteps, Func<BoardLocation, bool> goal, ICollection<string> own,
        Func<BoardLocation, bool>? allowed = null)
    {
        var planner = Planner();
        var previous = new Dictionary<string, (BoardLocation At, string? From)>(StringComparer.Ordinal);
        var queue = new Queue<BoardLocation>();
        foreach (var source in sources)
        {
            previous[source.ToString()] = (source, null);
            queue.Enqueue(source);
        }

        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            if ((sourcesAreSteps || previous[at.ToString()].From is not null) && goal(at))
            {
                var path = new List<BoardLocation>();
                for (var key = at.ToString(); key is not null; key = previous[key].From)
                {
                    path.Insert(0, previous[key].At);
                }

                return sourcesAreSteps ? path : path.Skip(1).ToList();
            }

            foreach (var next in planner.AdjacentLocations(state, at))
            {
                if (previous.ContainsKey(next.ToString()) || Occupied(state, next, own) || (allowed is not null && !allowed(next)))
                {
                    continue;
                }

                previous[next.ToString()] = (next, at.ToString());
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    /// One MPh of a stack marching toward a goal hex, then exiting across <paramref name="edge"/> if it stands on a goal; the DEFENDER passes each step.
    /// True when the stack left the map.
    /// </summary>
    private async Task<bool> March(string[] ids, Func<BoardLocation, bool> goal, string? edge, Func<BoardLocation, bool>? allowed = null)
    {
        for (var steps = 0; steps < 20; steps++)
        {
            if (Current.Ended is not null)
            {
                return false;
            }

            var state = Current;
            var at = state.Location(ids[0])?.Location;
            if (at is not null && goal(at) && edge is null)
            {
                Log($"  {string.Join(",", ids)} holds at {at}", await EndMove(ids));
                return false;
            }

            if (at is not null && goal(at) && edge is not null)
            {
                var exited = Log($"exit {string.Join(",", ids)} from {at} {edge}", await Exit(ids, edge));
                if (exited.Outcome == PlayOutcome.Committed)
                {
                    return true;
                }

                Log("  end move", await EndMove(ids));
                return false;
            }

            var sources = at is null ? Planner().EntryHexes(state, "top").Where(hex => !Occupied(state, hex, ids)).ToArray() : [at];
            var path = PathTo(state, sources, at is null, goal, ids, allowed);
            if (path is null || path.Count == 0)
            {
                Log($"no path for {string.Join(",", ids)} from {at}", await EndMove(ids));
                return false;
            }

            var moved = await Move(ids, path[0].ToString());
            if (moved.Outcome != PlayOutcome.Committed)
            {
                output.WriteLine($"  {string.Join(",", ids)} stops at {at}: {string.Join(" | ", moved.Reasons)}");
                Log("  end move", await EndMove(ids));
                return false;
            }

            Committed(await Pass());
        }

        return false;
    }

    private void Show(string title)
    {
        var report = Report;
        output.WriteLine($"-- {title}: at end {report.AtEnd.Winner ?? "draw"}: {report.AtEnd.Reason}; immediate {report.Immediate?.Winner ?? "none"}");
        foreach (var fact in report.AtEnd.Facts)
        {
            output.WriteLine("   " + fact);
        }
    }

    private static string? ControlOf(VictoryReport report, string id) => report.Control.Single(item => item.Id == id).Side;

    // ------------------------------------------------------------------------------------------------ Control in real movement

    private bool TopEdge(BoardLocation at) => Planner().EntryHexes(Current, "top").Any(hex => Same(hex, at));

    // 1a. A2.6, A26.221: the Russian squad in J2 carrying the MMG, with the 9-1, walks to the top edge and leaves; the Germans gain its CVP.
    [Fact]
    public async Task ASquadCarryingASwLeavesTheMap()
    {
        await SetUpGuards();
        await AdvanceTo("russian", "mph");
        var j2 = UnitsAt(Current, "bd01:J2:0").Select(unit => unit.Id).ToArray();
        output.WriteLine("J2 holds " + string.Join(", ", j2));
        var left = await March(j2, TopEdge, "top");
        if (!left)
        {
            // Out of MF on the edge: leave in the next Russian MPh.
            Committed(await Advance());
            await AdvanceTo("russian", "mph");
            left = await March(j2, TopEdge, "top");
        }

        Assert.True(left, "the J2 stack with its MMG should leave across the top edge");
        Assert.All(j2, id => Assert.Equal(InstanceStatus.Exited, Current.Unit(id)!.Status));
        Show("after the Russian exit");
        Assert.Equal(4, Report.Sides.Single(side => side.Side == "german").Cvp); // the squad (2) and the 9-1 (1 + 1)
        Assert.Equal("russian", ControlOf(Report, "J2"));
    }

    // 1b. A26.11: the Russians walk out of J2 to the top edge; a German squad walks into the empty J2 and gains it; Russian Defensive First Fire then
    // breaks it, and the Germans keep J2.
    [Fact]
    public async Task ControlGainedDuringMovementSurvivesDefensiveFirstFire()
    {
        await SetUpGuards();
        await AdvanceTo("russian", "mph");
        var j2 = UnitsAt(Current, "bd01:J2:0").Select(unit => unit.Id).ToArray();
        await March(j2, TopEdge, null);
        output.WriteLine("J2 stack now at " + Current.Location(j2[0])!.Location);
        Assert.Equal("russian", ControlOf(Report, "J2"));

        Committed(await Advance());
        await AdvanceTo("german", "mph");
        var holder = Current.Units.Where(unit => unit.Side == "german" && unit.Kind == "asl:squad").First(unit => Current.Location(unit.Id)!.Location.Hex.ToString() == "J4");
        var mover = UnitsAt(Current, "bd01:J4:0").First(unit => unit.Kind == "asl:squad" && unit.Id != holder.Id).Id;
        string[] ids = [mover];
        var j2At = L("bd01:J2:0");
        for (var step = 0; step < 4 && !Same(Current.Location(mover)!.Location, j2At); step++)
        {
            var path = PathTo(Current, [Current.Location(mover)!.Location], false, at => Same(at, j2At), ids)!;
            Committed(Log($"german {mover} to {path[0]}", await Move(ids, path[0].ToString())));
            if (!Same(path[0], j2At))
            {
                Committed(await Pass());
            }
        }

        Assert.Equal("german", ControlOf(Report, "J2"));

        // Russian Defensive First Fire from M2 at the squad in J2: an NMC, then a failed MC.
        var firer = UnitsAt(Current, "bd01:M2:0").First(unit => unit.Kind == "asl:squad").Id;
        foreach (var die in new[] { 1, 3, 6, 5, 6, 5 })
        {
            dice.Enqueue(die);
        }

        Committed(Log("first fire at J2", await Act(GameActions.Fire, new
        {
            firers = new[] { firer },
            target = "bd01:J2:0"
        })));

        var after = Current.Unit(mover)!;
        output.WriteLine($"{mover}: status {after.Status}, broken {GameState.Condition(after, Conditions.Broken)}");
        output.WriteLine("J2 now: " + string.Join(", ", UnitsAt(Current, "bd01:J2:0").Select(unit => $"{unit.Id} {unit.Kind} broken={GameState.Condition(unit, Conditions.Broken)}")));
        Assert.Equal(ConditionState.True, GameState.Condition(after, Conditions.Broken));
        Show("after the fire");
        Assert.Equal("german", ControlOf(Report, "J2"));
    }

    private async Task<string[]> RussianNextToM9()
    {
        await SetUpGuards();
        var m9 = L("bd01:M9:0");
        var near = Planner().AdjacentLocations(Current, m9);
        var squad = Current.Units.Where(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && Current.Location(unit.Id)!.Location.Hex.ToString() == "N5")
            .Select(unit => unit.Id).FirstOrDefault() ?? Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad"
            && Current.Location(unit.Id)!.Location.Hex.ToString() == "N3").Id;
        string[] ids = [squad];
        for (var turn = 0; turn < 3 && !near.Any(hex => Same(hex, Current.Location(squad)!.Location)); turn++)
        {
            await AdvanceTo("russian", "mph");
            await March(ids, at => near.Any(hex => Same(hex, at)), null);
            if (!near.Any(hex => Same(hex, Current.Location(squad)!.Location)))
            {
                Log("end move", await EndMove(ids));
            }

            Committed(await Advance());
        }

        output.WriteLine($"russian {squad} at {Current.Location(squad)!.Location}, turn {Current.Turn}");
        Assert.Contains(near, hex => Same(hex, Current.Location(squad)!.Location));
        return ids;
    }

    /// <summary>The German M9 stack in its MPh leaves M9 for an empty neighbor, dropping the HMG first if asked.</summary>
    private async Task GermansLeaveM9(bool dropHmg)
    {
        await AdvanceTo("german", "mph");
        var m9 = L("bd01:M9:0");
        var stack = Current.Units.Where(unit => unit.Side == "german" && unit.Status == InstanceStatus.Active && !Vocabulary.IsA(unit.Kind, "asl:equipment")
            && Current.Location(unit.Id)?.Location is { } at && Same(at, m9)).Select(unit => unit.Id).ToArray();
        var hmg = Current.Equipment.First(item => item.Side == "german" && item.Definition?.Definition == "attacker-hmg");
        if (dropHmg)
        {
            var squad = stack.First(id => Current.Unit(id)!.Kind == "asl:squad");
            Committed(Log("drop the HMG", await Act(GameActions.Drop, new
            {
                unitId = squad,
                equipmentId = hmg.Id
            })));
        }

        foreach (var next in Planner().AdjacentLocations(Current, m9).Where(at => !Occupied(Current, at, stack)))
        {
            var moved = Log($"german M9 stack to {next}", await Move(stack, next.ToString()));
            if (moved.Outcome == PlayOutcome.Committed)
            {
                Committed(await Pass());
                Committed(await EndMove(stack));
                break;
            }
        }

        Assert.All(stack, id => Assert.False(Same(Current.Location(id)!.Location, m9)));
        output.WriteLine($"HMG {hmg.Id} now at {Current.Location(hmg.Id)?.Location}");
        await AdvanceTo("russian", "mph");
    }

    // 2. A26.11: a SW is not a unit; the German HMG dropped and left alone in M9 should not stop a Russian squad from gaining the building.
    [Fact]
    public async Task AnAbandonedSwDoesNotPreventControl()
    {
        var ids = await RussianNextToM9();
        await GermansLeaveM9(dropHmg: true);
        Committed(Log("russian into M9", await Move(ids, "bd01:M9:0")));
        Show("russian in M9 with the German HMG on the floor");
        Assert.Equal("russian", ControlOf(Report, "M9"));
    }

    // 3. A26.11: Control is never gained via Bypass. In play M9 refuses every Bypass (its building fills the hex), so the moving stack is set in Bypass of
    // the emptied M9 directly; the following state, the stack occupying it, gains it.
    [Fact]
    public async Task BypassGainsNoControlButOccupyingDoes()
    {
        await SetUpGuards();
        Committed(await Advance());
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        var states = With(state => Moved(Gone(state, GermansIn("M9")), russian, "bd01:M9:0") with
        {
            Movement = new MovementState([russian], L("bd01:M9:0"), 4, 1, false, true) { Bypass = [HexsideDirection.North] },
        });
        Assert.Equal("german", ControlOf(Evaluate("guards-counterattack", states), "M9"));
        var occupied = Evaluate("guards-counterattack", [.. states, states[^1] with { Movement = null }]);
        Assert.Equal("russian", ControlOf(occupied, "M9"));
    }

    // ------------------------------------------------------------------------------------------------ Control and CVP from positions

    private IReadOnlyList<GameState> With(Func<GameState, GameState> change)
    {
        var states = History.States;
        return [.. states, change(states[^1])];
    }

    private static GameState Moved(GameState state, string unit, string at) =>
        state with
        {
            Units = [.. state.Units.Select(item => item.Id == unit ? item with { Position = new MapPosition(BoardLocation.Parse(at)) } : item)]
        };

    private static GameState Gone(GameState state, Func<UnitInstance, bool> which, InstanceStatus status = InstanceStatus.Eliminated) =>
        state with
        {
            Units = [.. state.Units.Select(item => which(item) ? item with { Status = status } : item)]
        };

    private static GameState Marked(GameState state, string unit, string condition) =>
        state with
        {
            Units = [.. state.Units.Select(item => item.Id == unit
                ? item with { Conditions = new Dictionary<string, ConditionState>(item.Conditions, StringComparer.Ordinal) { [condition] = ConditionState.True } }
                : item)]
        };

    private static GameState Dummied(GameState state, string unit) =>
        state with
        {
            Units = [.. state.Units.Select(item => item.Id == unit ? item with { Kind = UnitKinds.Dummy, Definition = null } : item)]
        };

    private VictoryReport Evaluate(string card, IReadOnlyList<GameState> states, bool ended = false) =>
        ScenarioVictory.Evaluate(Card(card), states, unit => Planner().VictoryPoints(states[^1], unit), _ => [], ended)!;

    private Func<UnitInstance, bool> GermansIn(params string[] hexes) =>
        unit => unit.Side == "german" && Current.Location(unit.Id)?.Location is { } at && hexes.Contains(at.Hex.ToString());

    // 4. A26.11, A26.15, A.7: who gains an empty M9: a broken, berserk, or Melee squad, a Dummy, and a squad with no MMC (a leader) do not; a concealed
    // squad does. A German Dummy left in M9 does not prevent the gain; a German leader does.
    [Fact]
    public async Task WhoGainsControl()
    {
        await SetUpGuards();
        Committed(await Advance());
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        var leader = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:leader").Id;
        var m9Germans = GermansIn("M9");
        var germanLeader = Current.Units.First(unit => m9Germans(unit) && unit.Kind == "asl:leader").Id;
        GameState Empty(GameState state) => Gone(state, m9Germans);
        string? M9(Func<GameState, GameState> change) => ControlOf(Evaluate("guards-counterattack", With(change)), "M9");

        Assert.Equal("russian", M9(state => Moved(Empty(state), russian, "bd01:M9:0")));
        Assert.Equal("german", M9(state => Marked(Moved(Empty(state), russian, "bd01:M9:0"), russian, Conditions.Broken)));
        Assert.Equal("german", M9(state => Marked(Moved(Empty(state), russian, "bd01:M9:0"), russian, Conditions.Berserk)));
        Assert.Equal("german", M9(state => Marked(Moved(Empty(state), russian, "bd01:M9:0"), russian, Conditions.Melee)));
        Assert.Equal("german", M9(state => Dummied(Moved(Empty(state), russian, "bd01:M9:0"), russian)));
        Assert.Equal("german", M9(state => Moved(Empty(state), leader, "bd01:M9:0")));
        Assert.Equal("russian", M9(state => Marked(Moved(Empty(state), russian, "bd01:M9:0"), russian, Conditions.Concealed)));

        // Every German but the 8-1 gone: the leader prevents the gain; the leader made a Dummy does not.
        GameState LeaderOnly(GameState state) => Gone(state, unit => m9Germans(unit) && unit.Id != germanLeader);
        Assert.Equal("german", M9(state => Moved(LeaderOnly(state), russian, "bd01:M9:0")));
        Assert.Equal("russian", M9(state => Dummied(Moved(LeaderOnly(state), russian, "bd01:M9:0"), germanLeader)));

        // A German prisoner in M9 neither prevents nor keeps it.
        Assert.Equal("russian", M9(state => Marked(Moved(LeaderOnly(state), russian, "bd01:M9:0"), germanLeader, Conditions.Captured)));
    }

    // 5. A26.22, A26.222: eliminated and captured Germans give the Russians CVP; a prisoner counts double at game end. A26.211: VP by unit.
    [Fact]
    public async Task CasualtiesAndPrisonersGiveCvp()
    {
        await SetUpGuards();
        Committed(await Advance());
        var m9 = Current.Units.Where(GermansIn("M9")).ToArray();
        var squad = m9.First(unit => unit.Kind == "asl:squad").Id;
        var leader = m9.First(unit => unit.Kind == "asl:leader").Id;

        var killed = Evaluate("guards-counterattack", With(state => Gone(state, unit => unit.Id == squad || unit.Id == leader)));
        Assert.Equal(4, killed.Sides.Single(side => side.Side == "russian").Cvp); // squad 2, 8-1 leader 1 + 1
        var captured = With(state => Marked(Gone(state, unit => unit.Id == leader), squad, Conditions.Captured));
        Assert.Equal(4, Evaluate("guards-counterattack", captured).Sides.Single(side => side.Side == "russian").Cvp);
        Assert.Equal(6, Evaluate("guards-counterattack", captured, ended: true).Sides.Single(side => side.Side == "russian").Cvp);
        Assert.Equal(0, Evaluate("guards-counterattack", captured).Sides.Single(side => side.Side == "german").Cvp);
    }

    // 6. A26.14, A26.3: The Guards Counterattack's margin: two German buildings for none gives the Russians the win; the Germans taking J2 back cuts it
    // to one and the Germans win; a third Russian building restores it.
    [Fact]
    public async Task TheGuardsMarginAtGameEnd()
    {
        await SetUpGuards();
        Committed(await Advance());
        var russians = Current.Units.Where(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && Current.Location(unit.Id)!.Location.Hex.ToString() is "E4" or "F3")
            .Select(unit => unit.Id).ToArray();
        var german = Current.Units.First(unit => unit.Side == "german" && unit.Kind == "asl:squad").Id;
        GameState TwoTaken(GameState state) => Moved(Moved(Gone(state, GermansIn("M9", "I7")), russians[0], "bd01:M9:0"), russians[1], "bd01:I7:0");
        GameState JTaken(GameState state) =>
            Moved(Gone(TwoTaken(state), unit => unit.Side == "russian" && Current.Location(unit.Id)?.Location.Hex.ToString() == "J2"), german, "bd01:J2:0");

        var two = Evaluate("guards-counterattack", With(TwoTaken), ended: true);
        output.WriteLine($"two for none: {two.AtEnd.Winner}: {two.AtEnd.Reason}");
        foreach (var fact in two.AtEnd.Facts)
        {
            output.WriteLine("   " + fact);
        }

        Assert.Equal("russian", two.AtEnd.Winner);
        var cut = Evaluate("guards-counterattack", With(JTaken), ended: true);
        output.WriteLine($"two for one: {cut.AtEnd.Winner}: {cut.AtEnd.Reason}");
        Assert.Equal("german", cut.AtEnd.Winner);
        var three = Evaluate("guards-counterattack", With(state => Moved(Gone(JTaken(state), GermansIn("F5", "F6", "G6", "H5")), russians[2], "bd01:G6:0")), ended: true);
        output.WriteLine($"three for one: {three.AtEnd.Winner}: {three.AtEnd.Reason}");
        Assert.Equal("russian", three.AtEnd.Winner);
    }

    // 7. A26.13 and the card: The Tractor Works: Russians keep six hexes of X3 and win; a Melee in one Russian hex drops them to five, both sides still have
    // unbroken units in X3, and the game is a draw; the facts name each hex.
    [Fact]
    public async Task TheTractorWorksDraw()
    {
        await SetUpGuards();
        Committed(await Advance());
        var germans = Current.Units.Where(unit => unit.Side == "german" && unit.Kind == "asl:squad").Select(unit => unit.Id).Take(4).ToArray();
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        GameState Three(GameState state) => Moved(Moved(Moved(state, germans[0], "bd01:X2:0"), germans[1], "bd01:X3:0"), germans[2], "bd01:X4:0");
        var six = Evaluate("tractor-works", With(state => Moved(Three(state), russian, "bd01:Y5:0")), ended: true);
        output.WriteLine($"six: {six.AtEnd.Winner}: {six.AtEnd.Reason}");
        Assert.Equal("russian", six.AtEnd.Winner);

        var melee = Evaluate("tractor-works", With(state => Marked(Marked(Moved(Moved(Three(state), russian, "bd01:Y5:0"), germans[3], "bd01:Y5:0"), russian, Conditions.Melee),
            germans[3], Conditions.Melee)), ended: true);
        output.WriteLine($"melee: {melee.AtEnd.Winner ?? "draw"}: {melee.AtEnd.Reason}");
        foreach (var fact in melee.AtEnd.Facts)
        {
            output.WriteLine("   " + fact);
        }

        Assert.Null(melee.AtEnd.Winner);

        // A player reading the facts expects Y5 not to be shown as Russian when the result treats it as neither's.
        Assert.DoesNotContain(melee.AtEnd.Facts, fact => fact.StartsWith("hexes", StringComparison.Ordinal) && fact.Contains("bd01:Y5", StringComparison.Ordinal)
            && fact.EndsWith(": russian", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------------------------------------ Gambit in play

    private async Task SetUpGambit()
    {
        var start = Start("gambit", Bd04);
        Committed(await PlaceWith(start, Unit("b1", "british-elite-squad", "bd04:D5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:D5:0", "british", "british-1"), Unit("b2", "british-elite-squad", "bd04:D6:0", "british", "british-1"),
            Unit("l3", "british-leader-8-0", "bd04:D6:0", "british", "british-1"), Unit("b3", "british-elite-squad", "bd04:D7:0", "british", "british-1")));
        List<Dictionary<string, object>> german = [Unit("g0", "attacker-elite-squad-5-4-8", "bd04:C8:0", "german", "german-1"),
            Unit("gl1", "attacker-leader-9-2", "bd04:C8:0", "german", "german-1"),
            .. Enumerable.Range(1, 7).Select(index => Unit($"g{index}", "attacker-elite-squad-5-4-8", $"bd04:{(char)('D' + index)}9:0", "german", "german-1")),
            Unit("gl2", "attacker-leader-9-1", "bd04:E9:0", "german", "german-1"), Unit("gl3", "attacker-leader-8-1", "bd04:F9:0", "german", "german-1")];
        Committed(await PlaceWith(start, [.. german]));
        var british = new List<Dictionary<string, object>> { Unit("l1", "british-leader-9-1", "off", "british", "british-1") };
        british.AddRange(Enumerable.Range(1, 9).Select(index => Unit($"bs{index}", "british-elite-squad", "off", "british", "british-1")));
        british.AddRange([Weapon("blmg1", "british-lmg", "bs9", "british"), Weapon("blmg2", "british-lmg", "bs8", "british"),
            Weapon("bmtr1", "british-light-mortar", "bs7", "british"), Weapon("bmtr2", "british-light-mortar", "bs6", "british"), Weapon("batr", "british-atr", "bs5", "british")]);
        Committed(await PlaceWith(start, [.. british]));
    }

    private static readonly string[] Near = ["I1", "Q1", "Y1"];

    /// <summary>Each unit of an on-map stack drops the SW it holds, before it moves.</summary>
    private async Task DropAll(string[] ids)
    {
        if (Current.Location(ids[0]) is null)
        {
            return;
        }

        foreach (var item in Current.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { } holding && ids.Contains(holding.Holder)).ToArray())
        {
            Log($"  {item.Holding!.Holder} drops {item.Id}", await Act(GameActions.Drop, new
            {
                unitId = item.Holding!.Holder,
                equipmentId = item.Id
            }));
        }
    }

    // 8. A26.23, A2.6: Gambit played out: the British walk to the south edge of board 2 and exit near 2I1, 2Q1, and 2Y1; b3 leaves by the west edge and
    // b1 with its 8-0 off the south edge far from the road (German CVP); the game ends at once at 20 Exit VP, and nothing is accepted after.
    // Fixed after the table player: the stacks carry their SW off the map (the run that dropped them first is not kept).
    [Theory]
    [InlineData(false)]
    public async Task GambitsBritishWalkOffTheSouthEdge(bool dropSw)
    {
        await SetUpGambit();
        string[][] runners = [["bs1", "bs2", "bs3", "l1"], ["bs4", "bs5", "bs6"], ["bs7", "bs8", "bs9"]];
        var done = new HashSet<int>();
        bool OnNear(BoardLocation at) => at.Board.Value == "bd02" && Near.Contains(at.Hex.ToString());
        var bottom = Planner().EntryHexes(Current, "bottom");
        output.WriteLine("bottom edge: " + string.Join(" ", bottom.Select(hex => hex.ToString())));
        bool Far(BoardLocation at) => at.Board.Value == "bd02" && at.Hex.ToString() is "EE1" or "CC1" or "GG1" && bottom.Any(hex => Same(hex, at));
        bool West(BoardLocation at) => at.Board.Value == "bd04" && at.Hex.ToString().StartsWith('A') && !at.Hex.ToString().StartsWith("AA", StringComparison.Ordinal);
        var sideDone = new HashSet<string>(StringComparer.Ordinal);
        for (var turn = 1; turn <= 8 && Current.Ended is null; turn++)
        {
            await AdvanceTo("british", "mph");
            output.WriteLine($"== British MPh, turn {Current.Turn}");
            if (!sideDone.Contains("b3") && await March(["b3"], West, "left"))
            {
                sideDone.Add("b3");
                Show("b3 left by the west edge");
            }

            if (!sideDone.Contains("b1") && await March(["b1", "l2"], Far, "bottom"))
            {
                sideDone.Add("b1");
                Show("b1 and l2 left off the south edge far from the road");
            }

            for (var index = 0; index < runners.Length && Current.Ended is null; index++)
            {
                if (!done.Contains(index) && dropSw)
                {
                    await DropAll(runners[index]);
                }

                if (!done.Contains(index) && await March(runners[index], OnNear, "bottom"))
                {
                    done.Add(index);
                    Show($"stack {index} left");
                }
            }

            if (Current.Ended is not null)
            {
                break;
            }

            Committed(await Advance());
        }

        output.WriteLine($"ended: {Current.Ended}");
        var result = Current.Ended?.Result;
        Assert.NotNull(result);
        output.WriteLine($"result: {result.Winner}: {result.Reason}");
        foreach (var fact in result.Facts)
        {
            output.WriteLine("   " + fact);
        }

        Assert.Equal("british", result.Winner);
        Assert.Contains(result.Facts, fact => fact.StartsWith("british:", StringComparison.Ordinal) && fact.Contains("20 Exit VP", StringComparison.Ordinal));
        if (sideDone.Contains("b3") && sideDone.Contains("b1"))
        {
            Assert.Contains(result.Facts, fact => fact.StartsWith("german: 5 CVP", StringComparison.Ordinal));
        }

        Refused(Log("move after the end", await Move(["b2"], "bd04:D7:0")), "play.game-over");
        Refused(Log("advance after the end", await Advance()), "play.game-over");
    }
}
