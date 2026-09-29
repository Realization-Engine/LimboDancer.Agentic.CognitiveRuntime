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
/// The backlog pass 18 in live play (rulings R18.1 to R18.3): a game starts from a scenario card, records it, and gives each unit its OB group's
/// ELR. Board 01's grid of Open Ground, fixed dice, and a clear LOS everywhere.
/// </summary>
public sealed class BacklogPass18Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a718");
    private static readonly GameScope Scope = new(Tenant, "p18");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd05 = ["bd05"];
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p18-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass18Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 18 test board", new HexFactSet(geometry, "pass18", hexes));
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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());

    private static DiceRoller Fixed(int face) => new(_ => face - 1);

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string definition, string hex, string side, string? group = null)
    {
        var unit = new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : "asl:squad",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
        };
        if (group is not null)
        {
            unit["group"] = group;
        }

        return unit;
    }

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task<PlayResult> Setup(Dictionary<string, object?> start, params Dictionary<string, object>[] placements) =>
        await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start,
            placements,
        }));

    /// <summary>A start naming a card; its other fields are the request's, which the card's replace.</summary>
    private static Dictionary<string, object?> CardStart(string card, string? sha256 = null, string? firstSide = "german") => new()
    {
        ["label"] = "From a card",
        ["catalog"] = "asl-scenario-a1@1.12.0",
        ["boards"] = Bd05,
        ["firstSide"] = firstSide,
        ["sides"] = new object[] { new { id = "german", nationality = "german", elr = 1, san = 2 }, new { id = "russian", nationality = "russian", elr = 1 } },
        ["scenario"] = new
        {
            id = card,
            sha256 = sha256 ?? ScenarioCards.Sha256(card) ?? string.Empty,
            title = card
        },
    };

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

    // R18.1, R18.2: the card, not the request, gives the start; the game records the card.
    [Fact]
    public async Task TheGuardsCounterattackStartsAsTheCardSays()
    {
        Committed(await Setup(CardStart("guards-counterattack"), Unit("g1", "attacker-squad", "F5", "german", "german-1"),
            Unit("r1", "defender-squad", "N4", "russian", "russian-1")));
        var state = Current;
        Assert.Equal(("guards-counterattack", ScenarioCards.Sha256("guards-counterattack"), "The Guards Counterattack"),
            (state.Scenario!.Id, state.Scenario.Sha256, state.Scenario.Title));
        Assert.Equal("bd01", state.Map.Boards.Single().Board.Value);
        Assert.Equal(("russian", 10, 1942), (state.PhasingSide, state.ScenarioMonth!.Value, state.ScenarioYear!.Value));
        var german = state.Side("german")!;
        var russian = state.Side("russian")!;
        Assert.Equal((4, 6, "bottom"), (german.Elr!.Value, german.San!.Value, german.FriendlyEdge));
        Assert.Equal((3, 6, "top"), (russian.Elr!.Value, russian.San!.Value, russian.FriendlyEdge));
        Assert.Equal(["russian-1", "russian-2"], russian.Groups.Select(group => group.Id));
        Assert.Equal("Elements of 2nd Battalion, 37th Guards Division", russian.Groups[1].Name);
        Assert.Equal(("german-1", 4), (state.Unit("g1")!.Group, state.ElrOf(state.Unit("g1")!)!.Value));
    }

    [Fact]
    public async Task AnUnknownOrChangedCardIsRefused()
    {
        Refused(await Setup(CardStart("the-last-hurrah"), Unit("g1", "attacker-squad", "F5", "german")), "play.scenario");
        Refused(await Setup(CardStart("guards-counterattack", sha256: new string('0', 64)), Unit("g1", "attacker-squad", "F5", "german")), "play.scenario");
    }

    // R18.1 (A3.9): The Tractor Works leaves the first move to a die roll; the request names the side that won it.
    [Fact]
    public async Task TheTractorWorksTakesTheSideThatWonTheDieRoll()
    {
        Refused(await Setup(CardStart("tractor-works", firstSide: null), Unit("g1", "attacker-squad", "U3", "german", "german-2")), "play.scenario");
        Committed(await Setup(CardStart("tractor-works", firstSide: "german"), Unit("g1", "attacker-squad", "U3", "german", "german-2")));
        Assert.Equal("german", Current.PhasingSide);
        Assert.Equal(3, Current.Side("german")!.Groups.Count);
    }

    // R18.3: a unit names a group of its own side.
    [Fact]
    public async Task AUnitNamesAGroupOfItsOwnSide()
    {
        var refused = await Setup(CardStart("guards-counterattack"), Unit("g1", "attacker-squad", "F5", "german", "russian-1"));
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.Contains("russian-1", StringComparison.Ordinal));
    }

    /// <summary>A start with no card whose Russian groups differ in ELR, and so name no side ELR.</summary>
    private static Dictionary<string, object?> SplitStart() => new()
    {
        ["label"] = "Split ELR",
        ["catalog"] = "asl-scenario-a1@1.12.0",
        ["boards"] = Bd01,
        ["firstSide"] = "german",
        ["scenarioMonth"] = 7,
        ["scenarioYear"] = 1942,
        ["sides"] = new object[]
        {
            new { id = "german", nationality = "german", elr = 3 },
            new
            {
                id = "russian",
                nationality = "russian",
                groups = new object[] { new { id = "russian-1", name = "Conscripts", elr = 1 }, new { id = "russian-2", name = "Guards", elr = 4 } },
            },
        },
    };

    // R18.3 (A19.1): where a side's groups differ in ELR, each unit names its group, and fire takes the group's ELR.
    [Fact]
    public async Task FireTakesTheTargetsGroupElr()
    {
        Refused(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian")), "play.group");
        Committed(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", "russian-2")));
        Assert.Equal(4, Current.ElrOf(Current.Unit("r1")!));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Fixed(6), new
        {
            firers = G1,
            target = L("E5"),
        }));
        var fire = store.Read(Scope)!.Events.Skip((int)before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var attack = fire.Facts.Deserialize<FireAttack>(LiveFire.Json)!;
        Assert.Null(attack.TargetSideElr);
        Assert.Equal(4, attack.Targets!.Single().Elr);
    }

    /// <summary>These values first, then the fallback face for every other die.</summary>
    private static DiceRoller Then(int fallback, params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => (queue.Count > 0 ? queue.Dequeue() : fallback) - 1);
    }

    // R18.3 (A19.13): a unit Replaced beyond its group's ELR keeps its group.
    [Fact]
    public async Task AReplacedUnitKeepsItsGroup()
    {
        Committed(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", "russian-1")));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.Fire, Then(6, 2, 3, 6, 6), new
        {
            firers = G1,
            target = L("E5"),
        }));
        var replacement = Current.Units.Single(unit => unit.Side == "russian" && unit.Status == InstanceStatus.Active);
        Assert.NotEqual("r1", replacement.Id);
        Assert.Equal("russian-1", replacement.Group);
        Assert.Equal(1, Current.ElrOf(replacement));
    }

    // R18.3 (A19.13; referee, pass 18): a 4 FP 1MC (DR 5) and a MC DR of 8 fail a 4-4-7 by 2: Replaced at ELR 1, only broken at ELR 4.
    [Theory]
    [InlineData("russian-1", true)]
    [InlineData("russian-2", false)]
    public async Task FailingByTwoReplacesOnlyInTheLowElrGroup(string group, bool replaced)
    {
        Committed(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", group)));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.Fire, Then(6, 2, 3, 4, 4), new
        {
            firers = G1,
            target = L("E5"),
        }));
        var russian = Current.Units.Single(unit => unit.Side == "russian" && unit.Status == InstanceStatus.Active);
        Assert.Equal(replaced, russian.Id != "r1");
        Assert.Equal(ConditionState.True, GameState.Condition(russian, Conditions.Broken));
        Assert.Equal(group, russian.Group);
    }

    // R18.3 (referee, pass 18): a unit created in play (a hero, a created leader, a crew) joins the group of a unit of its side in its Location.
    [Fact]
    public async Task AUnitCreatedInPlayJoinsTheGroupInItsLocation()
    {
        Committed(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", "russian-2")));
        var events = store.Read(Scope)!.Events;
        var placed = events.Last(item => item.Payload is InstanceCreated);
        var r1 = ((InstanceCreated)placed.Payload).Instance;
        var leader = placed with
        {
            EventId = placed.EventId + "-leader",
            Revision = events.Count + 1,
            Payload = new InstanceCreated(new NewInstance("r-leader", "asl:leader", "defender-leader-9-1", "russian", r1.Position, null, r1.Conditions)),
        };
        var state = Planner().Replay([.. events, leader]).Current!;
        Assert.Equal("russian-2", state.Unit("r-leader")!.Group);
        Assert.Equal(4, state.ElrOf(state.Unit("r-leader")!));
    }

    [Fact]
    public async Task ADummyNeedsNoGroupAndASideElrMustMatchItsGroups()
    {
        var dummy = new Dictionary<string, object>
        {
            ["id"] = "r-dummy",
            ["kind"] = UnitKinds.Dummy,
            ["side"] = "russian",
            ["position"] = new
            {
                at = L("E6")
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
        };
        var contradicted = SplitStart();
        contradicted["sides"] = new object[]
        {
            new { id = "german", nationality = "german", elr = 3 },
            new { id = "russian", nationality = "russian", elr = 3, groups = new object[] { new { id = "russian-1", name = "A", elr = 1 }, new { id = "russian-2", name = "B", elr = 4 } } },
        };
        Assert.NotEqual(PlayOutcome.Committed, (await Setup(contradicted, Unit("g1", "attacker-squad", "E3", "german"))).Outcome);
        Committed(await Setup(SplitStart(), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", "russian-1"), dummy));
    }

    // R18.1, R18.3: a card's start carries its label, first side, each side's groups and shared ELR, and its placed boards.
    [Fact]
    public void ACardsSidesCarryTheirGroupsThroughTheRecord()
    {
        var card = ScenarioCards.Read("gambit", Catalog)!.Card!;
        var start = ScenarioCards.Start(card, ScenarioCards.Sha256("gambit")!, "asl-scenario-a1@1.12.0", null, null);
        Assert.Equal("Gambit", (string?)start["label"]);
        Assert.Equal("german", (string?)start["firstSide"]);
        var british = start["sides"]![0]!;
        Assert.Equal(3, (int?)british["elr"]);
        Assert.Equal("british-1", (string?)british["groups"]![0]!["id"]);
        Assert.Equal(2, start["boards"]!.AsArray().Count);
        Assert.True((bool?)start["boards"]![1]!["reversed"]);
    }
}
