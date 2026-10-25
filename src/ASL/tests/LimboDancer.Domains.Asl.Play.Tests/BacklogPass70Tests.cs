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
/// Pass 70 in live play, Infantry III. Increment 3a: who sees (A12.14, A12.34; ruling R70.16), an action that costs a "?" (A12.141; ruling R70.17), and
/// the Right of Inspection (A12.16; rulings R70.18, R70.19). Board 01's grid, all Open Ground, fixed dice, and a LOS that is clear at the board's range
/// but between the pairs a test blocks; every read of the state replays the game, so each record is checked by the projector.
/// </summary>
public sealed class BacklogPass70Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f570");
    private static readonly GameScope Scope = new(Tenant, "pass70");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] BothGermans = ["de-1", "de-2"];
    private static readonly string[] TopGerman = ["de-1"];
    private static readonly string[] Transferers = ["de-s", "de-h"];
    private static readonly string[] HiddenState = ["asl:hidden"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass70-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly HashSet<(string, string)> blocked = [];

    public BacklogPass70Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static TerrainType Open() => new()
    {
        Code = 1,
        Name = "Open Ground",
        Category = LosCategory.Open,
    };

    /// <summary>Board 01's grid, all Open Ground at level 0.</summary>
    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, Open(), null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, Open(), null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 70 test board", new HexFactSet(geometry, "pass70", hexes));
    }

    // Clear LOS at the board's true range, blocked between the pairs of hexes a test names (either way).
    private sealed class PairLos(HashSet<(string, string)> blocked) : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            var shut = blocked.Contains((from.Hex.ToString(), target.Hex.ToString())) || blocked.Contains((target.Hex.ToString(), from.Hex.ToString()));
            return shut ? new(LosStatus.Blocked, true, range, 0, null, string.Empty) : new(LosStatus.Clear, false, range, 0, null, string.Empty);
        }
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new PairLos(blocked));

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Squad(string id, string hex, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", hex, side, states);

    private static Dictionary<string, object> Lmg(string id, string holder) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = "attacker-lmg",
        ["side"] = "german",
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A German infantry Gun manned by its crew, both hidden (A12.34).</summary>
    private static Dictionary<string, object>[] HiddenGun(string hex) =>
    [
        Unit("de-gc", "asl:crew", "attacker-crew", hex, "german", "asl:hidden"),
        new()
        {
            ["id"] = "de-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "attacker-inf-gun",
            ["side"] = "german",
            ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = "east" },
            ["holding"] = new
            {
                holder = "de-gc",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false, ["asl:hidden"] = true },
        },
    ];

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other, left in the first side's RPh and then advanced the times given (1: its PFPh, 2: its MPh).</summary>
    private async Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass70Tests),
                ["catalog"] = $"asl-scenario-a1@{LiveFire.CatalogVersion}",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        })));
        for (var index = 0; index < advances; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(History.HasErrors);

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private GameView ViewAs(string side)
    {
        var history = History;
        return Planner().ViewOf(history, history.Current!.Revision, history.Current.Perspectives.Single(item => item.Name == side));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData("asl:broken", true, false)]
    [InlineData("asl:hidden", true, true)]
    public async Task AnUnconcealedGermanStackOutOfEveryRussianGoodOrderUnitsLosShowsItsTopOnly(string? viewer, bool inLos, bool inspected)
    {
        // A12.16 (p. 79; ruling R70.18): after play begins the Russian inspects the German stack in E5 only from the LOS of a Good Order ground unit of
        // his, a hidden one included; a broken one is not Good Order. Out of every such LOS he sees its top counter, the 4-6-7 de-1, and a count for
        // de-2, whose LMG is withheld.
        if (!inLos)
        {
            blocked.Add(("E2", "E5"));
        }

        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Lmg("de-lmg", "de-2"),
            Squad("ru-1", "E2", "russian", viewer is null ? Array.Empty<string>() : new[] { viewer }));
        Assert.True(Current.SetupClosed);
        var russian = ViewAs("russian");
        string[] germans = [.. russian.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id)];
        if (inspected)
        {
            Assert.Equal(BothGermans, germans);
            Assert.Contains(russian.Equipment, item => item.Id == "de-lmg");
            Assert.DoesNotContain(russian.Sealed, item => item.Side == "german");
        }
        else
        {
            Assert.Equal(TopGerman, germans);
            Assert.DoesNotContain(russian.Equipment, item => item.Id == "de-lmg");
            var sealedOne = Assert.Single(russian.Sealed, item => item.Side == "german");
            Assert.Equal((L("E5"), true, true), (sealedOne.Location.ToString(), sealedOne.Uninspected, sealedOne.OutOfLos));
        }

        // The German sees his own stack in full; a view built without the set (Units, no map) shows it in full as before the pass (ruling R70.19).
        Assert.Equal(BothGermans, ViewAs("german").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        var history = History;
        var plain = GameView.Of(history, history.Current!.Revision, history.Current.Perspectives.Single(item => item.Name == "russian"));
        Assert.Equal(BothGermans, plain.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        NoReplayErrors();
    }

    [Fact]
    public async Task TheStackIsInspectedInFullOnceARussianUnitSeesIt()
    {
        // A12.16: the same stack, out of ru-1's LOS, is shown in full to the Russian once ru-2 in G2 has a LOS to it. The view is derived, never
        // recorded, so the record is the same either way (ruling R70.19).
        blocked.Add(("E2", "E5"));
        blocked.Add(("G2", "E5"));
        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Squad("ru-1", "E2", "russian"), Squad("ru-2", "G2", "russian"));
        Assert.Equal(TopGerman, ViewAs("russian").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        blocked.Remove(("G2", "E5"));
        Assert.Equal(BothGermans, ViewAs("russian").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AHiddenGunThatFiresIsNotRevealedByAHiddenEnemy(bool viewerHidden, bool hiddenAfter)
    {
        // A12.34 (p. 80; rulings R26.5, R70.16): the hidden German Gun in E5 fires at the broken Russian squad in G5 with a colored dr of 5. The nearest
        // Good Order enemy ground unit with a LOS is ru-1 in E2, three hexes away, which reveals it; a hidden ru-1 forces nothing (R35.5), so the Gun keeps
        // HIP. Before the pass a hidden unit was counted here.
        await SetupAt(1, "german", [.. HiddenGun("E5"), Squad("ru-t", "G5", "russian", "asl:broken"), Squad("ru-1", "E2", "russian", viewerHidden ? HiddenState : Array.Empty<string>())]);
        Committed(await Do(GameActions.FireOrdnance, Once(5, 6, 3, 3, 3, 3, 3, 3), new
        {
            gunId = "de-gun",
            target = L("G5"),
            ammunition = "he"
        }));
        Assert.Equal(hiddenAfter, Is(Current.Find("de-gun")!, Conditions.Hidden));
        Assert.Equal(hiddenAfter, Is(Current.Unit("de-gc")!, Conditions.Hidden));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AConcealedMoverIsNotRevealedByAHiddenUnit(bool viewerHidden)
    {
        // A12.14 (p. 77; ruling R70.16, the Loss seers): r1 moves in Open Ground without Assault Movement in the LOS of g1, three hexes away. A Good Order
        // g1 takes its "?"; a hidden one does not.
        await SetupAt(2, "russian", Squad("r1", "E5", "russian", "asl:concealed"), Squad("g1", "E2", "german", viewerHidden ? HiddenState : Array.Empty<string>()));
        Assert.Equal("mph", Current.Phase);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = R1,
            to = L("E6")
        }));
        Assert.Equal(viewerHidden, Is(Current.Unit("r1")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ATransferCostsBothUnitsTheirQuestionMarkInTheLosOfAGoodOrderEnemy(bool inLos)
    {
        // A12.141 (p. 78; ruling R70.17): a concealed squad passes its LMG to a concealed HS in its RPh; both engage in the transfer, so both lose their
        // "?" in the LOS of the Good Order Russian squad in E2 three hexes away, and keep it out of that LOS.
        if (!inLos)
        {
            blocked.Add(("E2", "E5"));
        }

        await SetupAt(0, "german", Squad("de-s", "E5", "german", "asl:concealed"), Unit("de-h", "asl:half-squad", "attacker-half-squad", "E5", "german", "asl:concealed"),
            Lmg("de-lmg", "de-s"), Squad("ru-1", "E2", "russian"));
        Assert.Equal("rph", Current.Phase);
        var before = Revision;
        var result = await Do(GameActions.Transfer, NoRoll(), new
        {
            unitId = "de-s",
            equipmentId = "de-lmg",
            toUnitId = "de-h"
        });
        Committed(result);
        string[] lost = [.. Since(before).Where(item => item.Type == "concealment-lost").Select(item => ((ConditionsChanged)item.Payload).Id)];
        Assert.Equal(inLos ? Transferers : Array.Empty<string>(), lost);
        Assert.Equal(!inLos, Is(Current.Unit("de-s")!, Conditions.Concealed));
        Assert.Equal(!inLos, Is(Current.Unit("de-h")!, Conditions.Concealed));
        Assert.Equal(inLos, result.Reasons.Any(reason => reason.StartsWith("play.concealment:", StringComparison.Ordinal) && reason.Contains("(A12.141)", StringComparison.Ordinal)));
        NoReplayErrors();
    }
}
