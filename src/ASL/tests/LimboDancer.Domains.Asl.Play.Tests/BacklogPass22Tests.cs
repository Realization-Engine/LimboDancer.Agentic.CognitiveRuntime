using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 22 of the Scenario Card Games Plan (rulings R22.2 to R22.4): the card library of built-in and user cards, a game from a user card and from a
/// minimal card, and a changed user card refused. Board 01 with its real terrain.
/// </summary>
public sealed class BacklogPass22Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a722");
    private static readonly GameScope Scope = new(Tenant, "p22");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p22-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly ScenarioCardLibrary cards;

    public BacklogPass22Tests()
    {
        store = new FileGameStore(Path.Combine(root, "games"));
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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog], cardLibrary: cards);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: new DiceRoller(_ => 5));

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Minimal(string id) => new(ScenarioCards.Format, id, "A minimal game", "asl-scenario-a1@1.13.0",
        new ScenarioCardSource("A user card (ruling R22.2).", "none", []), string.Empty, new ScenarioCardDate(0, 7, 0), string.Empty,
        [new ScenarioCardBoard("bd01", 0, 0, false)], "top", null, new ScenarioCardTurns(10, false, "german", "german"), null,
        [new ScenarioCardSide("german", 2, null, new ScenarioCardEdge("bottom", "manufactured", null), string.Empty, [], null, 3),
            new ScenarioCardSide("russian", 0, null, new ScenarioCardEdge(string.Empty, "none", null), string.Empty, [], null, 2)],
        [new ScenarioCardRule(1, "The special rules by token.", "token", ["weather:overcast"], [], null)],
        new ScenarioCardVictory("other", "The players judge the result.", []), null);

    private async Task<PlayResult> Setup(string card, params object[] placements)
    {
        var node = JsonSerializer.SerializeToNode(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Revision}",
            expectedRevision = Revision,
            placements
        })!.AsObject();
        if (Revision == 0)
        {
            node["start"] = JsonSerializer.SerializeToNode(new
            {
                label = card,
                catalog = "asl-scenario-a1@1.13.0",
                scenario = new
                {
                    id = card,
                    sha256 = cards.Sha256(card),
                    title = card
                },
            });
        }

        var arguments = JsonSerializer.SerializeToElement(node);
        var play = Play();
        var proposed = await play.ProposeAsync(GameActions.Setup, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(GameActions.Setup, arguments, Player, proposed.Correlation);
    }

    private static object Squad(string id, string at, string side = "german") => new
    {
        id,
        kind = "asl:squad",
        definition = side == "german" ? "attacker-squad" : "defender-squad",
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, bool> { ["asl:broken"] = false },
    };

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    // R22.2: the library lists the built-in cards and the user's, never lets a user card take a built-in name, and reads a changed card afresh.
    [Fact]
    public void TheLibraryHoldsBuiltInAndUserCards()
    {
        Assert.Equal(ScenarioCards.Names, cards.Names);
        Assert.NotEmpty(cards.Save(Minimal("guards-counterattack"), Catalog));
        Assert.NotEmpty(cards.Save(Minimal("Bad Name"), Catalog));
        Assert.NotEmpty(cards.Save(Minimal("village\n"), Catalog));
        Assert.NotEmpty(cards.Save(Minimal("with-outcomes") with
        {
            VictoryConditions = new ScenarioCardVictory("other", "The players judge the result.", [], [])
        }, Catalog));
        Assert.Empty(cards.Save(Minimal("village"), Catalog));
        Assert.Equal([.. ScenarioCards.Names, "village"], cards.Names);
        Assert.True(cards.IsUser("village"));
        Assert.False(cards.IsUser("gambit"));
        var before = cards.Sha256("village");

        // Referee, pass 22: a save replaces a user card only when told to.
        Assert.NotEmpty(cards.Save(Minimal("village") with
        {
            Title = "Another"
        }, Catalog));
        Assert.Equal(before, cards.Sha256("village"));
        Assert.Empty(cards.Save(Minimal("village") with
        {
            Title = "Another"
        }, Catalog, replace: true));
        Assert.NotEqual(before, cards.Sha256("village"));
        Assert.Equal(ScenarioCards.Sha256("gambit"), cards.Sha256("gambit"));
        Assert.True(cards.Delete("village"));
        Assert.False(cards.Delete("gambit"));
        Assert.DoesNotContain("village", cards.Names);
    }

    // R22.3: a game from a minimal card takes its sides' ELR, SAN, edges, month, and SSR tokens, and its units set up by hand with no OB group.
    [Fact]
    public async Task AGameStartsFromAMinimalCard()
    {
        Assert.Empty(cards.Save(Minimal("village"), Catalog));
        Committed(await Setup("village", Squad("g1", "bd01:A1:0"), Squad("r1", "bd01:C3:0", "russian")));
        var state = Current;
        Assert.Equal(("village", 3, 2), (state.Scenario!.Id, state.Side("german")!.Elr!.Value, state.Side("russian")!.Elr!.Value));
        Assert.Equal(("bottom", (string?)null), (state.Side("german")!.FriendlyEdge, state.Side("russian")!.FriendlyEdge));
        Assert.Equal(7, state.ScenarioMonth);
        Assert.Null(state.ScenarioYear);
        Assert.Contains("weather:overcast", state.SpecialRules);
        Assert.Null(Planner().CardSetup(state, new HashSet<string>()));
        Assert.Null(Planner().Victory(Planner().Replay(store.Read(Scope)!.Events)));
    }

    // R22.2 (R18.1, R19.1): a user card changed after the game started refuses more setup.
    [Fact]
    public async Task AChangedUserCardIsRefused()
    {
        Assert.Empty(cards.Save(Minimal("village"), Catalog));
        Committed(await Setup("village", Squad("g1", "bd01:A1:0")));
        Assert.Empty(cards.Save(Minimal("village") with
        {
            Title = "Changed"
        }, Catalog, replace: true));

        // Referee, pass 22: no rule follows the changed card; the game started from another text.
        Assert.Null(Planner().Victory(Planner().Replay(store.Read(Scope)!.Events)));
        var refused = await Setup("village", Squad("g2", "bd01:A2:0"));
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.scenario", StringComparison.Ordinal));
    }

    // R22.2: a copy of a built-in card saved as the user's starts a game with the card's OB checks.
    [Fact]
    public async Task ACopyOfABuiltInCardPlaysAsTheOriginal()
    {
        var copy = ScenarioCards.Read("guards-counterattack", Catalog)!.Card! with
        {
            Id = "guards-mine",
            Title = "My Guards"
        };
        Assert.Empty(cards.Save(copy, Catalog));
        Assert.Equal(copy.Sides.Select(side => side.IntegrityBpv), cards.Read("guards-mine", Catalog)!.Card!.Sides.Select(side => side.IntegrityBpv));
        var refused = await Setup("guards-mine", Squad("r1", "bd01:N4:0", "russian"));
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.setup-", StringComparison.Ordinal));
    }

    // Backlog section 29 (pass 28): a user card deleted after the game started refuses more setup and says the card is gone (rulings R19.1, R22.2).
    [Fact]
    public async Task ADeletedUserCardIsRefused()
    {
        Assert.Empty(cards.Save(Minimal("village"), Catalog));
        Committed(await Setup("village", Squad("g1", "bd01:A1:0")));
        Assert.True(cards.Delete("village"));
        var refused = await Setup("village", Squad("g2", "bd01:A2:0"));
        Assert.Contains(refused.Reasons, reason => reason.Contains("'village' is no longer among the scenario cards", StringComparison.Ordinal));
    }

    // Ruling R28.4: a game that recorded a built-in card's earlier text, revised only in a note, reads the current text; any other text gives it no card.
    [Fact]
    public async Task AGameFromABuiltInCardsEarlierTextPlaysOn()
    {
        Committed(await Setup("guards-counterattack"));
        var file = Path.Combine(root, "games", Tenant.ToString("N"), Scope.Game + ".game.json");
        var current = cards.Sha256("guards-counterattack")!;
        SetupReport? Report() => Planner().CardSetup(Current, new HashSet<string>());

        File.WriteAllText(file, File.ReadAllText(file).Replace(current, ScenarioCards.EarlierRevisions["guards-counterattack"][0].Earlier, StringComparison.Ordinal));
        Assert.NotNull(Report());

        File.WriteAllText(file, File.ReadAllText(file).Replace(ScenarioCards.EarlierRevisions["guards-counterattack"][0].Earlier, new string('0', 64), StringComparison.Ordinal));
        Assert.Null(Report());
    }
}
