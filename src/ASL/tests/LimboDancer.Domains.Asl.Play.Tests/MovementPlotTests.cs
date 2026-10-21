using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The Play streamlining plan, S3: a route plotted before any of it is committed. The plot reads each step from the move's own plan, so its MF and
/// its refusals are the gate's; it reads the Locations as the moving side knows them; and it stores nothing. Board 01's Open Ground in column FF
/// (FF10 to FF7) and the woods of EE7.
/// </summary>
public sealed class MovementPlotTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-0000000051a0");
    private static readonly GameScope Scope = new(Tenant, "plot");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-plot-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public MovementPlotTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => new(LosStatus.Clear, false, 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog], fireLos: new StubLos());

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: new DiceRoller(_ => throw new InvalidOperationException("No roll.")));

    private IReadOnlyList<GameEvent> Events => store.Read(Scope)?.Events ?? [];

    private static BoardLocation At(string hex) => BoardLocation.Parse($"bd01:{hex}:0");

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = "asl:squad",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at
            },
            ["conditions"] = conditions
        };
    }

    private async Task<PlayResult> Commit(Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Events.Count}";
        node["expectedRevision"] = Events.Count;
        var element = JsonSerializer.SerializeToElement(node);
        var play = Play();
        var proposed = await play.ProposeAsync(action, element, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, element, Player, proposed.Correlation);
    }

    /// <summary>A game on board 01 in the German MPh, the German squad g1 in FF10 and the Russians placed as given.</summary>
    private async Task GermanMph(params Dictionary<string, object>[] russians)
    {
        var setup = await Commit(GameActions.Setup, new
        {
            start = new
            {
                label = "Plot",
                catalog = "asl-scenario-a1@1.13.0",
                boards = Bd01,
                firstSide = "german",
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 3 } },
            },
            placements = new[] { Unit("g1", "attacker-squad", "bd01:FF10:0", "german") }.Concat(russians).ToArray(),
        });
        Assert.True(setup.Outcome == PlayOutcome.Committed, string.Join("; ", setup.Reasons));
        for (var index = 0; index < 2; index++)
        {
            var advanced = await Commit(GameActions.AdvancePhase, new
            {
            });
            Assert.True(advanced.Outcome == PlayOutcome.Committed, string.Join("; ", advanced.Reasons));
        }

        Assert.Equal("mph", Planner().Replay(Events).Current!.Phase);
    }

    [Fact]
    public async Task ThePlotOffersTheNextLocationsWithTheirMfAndWhatIsLeft()
    {
        await GermanMph(Unit("r1", "defender-squad", "bd01:AA10:0", "russian"));
        var plot = Planner().Plot(Scope, Events, G1, [], "german");

        Assert.Null(plot.Stop);
        Assert.Empty(plot.Route);
        var ff9 = Assert.Single(plot.Next, option => option.To == At("FF9"));
        Assert.Equal((2, 6, (string?)null), (ff9.HalfMf, ff9.HalfMfLeft, ff9.Refusal));
    }

    // The plot is the gate's: the route's MF are the MF the committed steps spend, and the store holds nothing until they are committed.
    [Fact]
    public async Task APlottedRouteCostsWhatTheCommittedStepsSpend()
    {
        await GermanMph(Unit("r1", "defender-squad", "bd01:AA10:0", "russian"));
        var before = Events.Count;
        var plot = Planner().Plot(Scope, Events, G1, [At("FF9"), At("FF8")], "german");

        Assert.Null(plot.Stop);
        Assert.Equal([(2, (int?)6), (2, 4)], plot.Route.Select(step => (step.HalfMf, step.HalfMfLeft)));
        Assert.Equal(before, Events.Count);
        var ff7 = Assert.Single(plot.Next, option => option.To == At("FF7"));
        Assert.Equal((int?)2, ff7.HalfMfLeft);

        foreach (var to in new[] { "FF9", "FF8" })
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(GameActions.Move, new
            {
                unitIds = G1,
                to = $"bd01:{to}:0"
            })).Outcome);
            Assert.Equal(PlayOutcome.Committed, (await Commit(GameActions.PassFire, new
            {
            })).Outcome);
        }

        var g1 = Planner().Replay(Events).Current!.Unit("g1")!;
        Assert.Equal(4, (g1.MfSpent * 2) + (g1.HalfMfSpent ? 1 : 0));
    }

    // A4.11: a route the stack cannot pay for stops at the step it cannot, with the gate's own refusal.
    [Fact]
    public async Task ARouteBeyondTheStacksMfStopsWithTheGatesRefusal()
    {
        await GermanMph(Unit("r1", "defender-squad", "bd01:AA10:0", "russian"));
        var plot = Planner().Plot(Scope, Events, G1, [At("FF9"), At("FF8"), At("FF7"), At("EE7")], "german");

        Assert.Equal(3, plot.Route.Count);
        Assert.StartsWith("play.move-mf", plot.Stop, StringComparison.Ordinal);
        Assert.Empty(plot.Next);
    }

    // A12.15: a concealed enemy on the route is met when the step is made; the plot neither refuses its Location nor reveals it.
    [Fact]
    public async Task APlotDoesNotRevealAConcealedEnemyOnTheRoute()
    {
        await GermanMph(Unit("r1", "defender-squad", "bd01:FF8:0", "russian", "asl:concealed"));
        var plot = Planner().Plot(Scope, Events, G1, [At("FF9"), At("FF8")], "german");

        Assert.Null(plot.Stop);
        Assert.Equal(2, plot.Route.Count);
        Assert.True(GameState.Condition(Planner().Replay(Events).Current!.Unit("r1")!, "asl:concealed") == ConditionState.True);
    }

    // A4.14: a Location holding a Known enemy unit is offered with the gate's refusal.
    [Fact]
    public async Task AKnownEnemysLocationIsOfferedWithItsRefusal()
    {
        await GermanMph(Unit("r1", "defender-squad", "bd01:FF9:0", "russian"));
        var plot = Planner().Plot(Scope, Events, G1, [], "german");

        var ff9 = Assert.Single(plot.Next, option => option.To == At("FF9"));
        Assert.StartsWith("play.move-occupied", ff9.Refusal, StringComparison.Ordinal);
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
