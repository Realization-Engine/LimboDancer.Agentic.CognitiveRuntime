using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 30 of the Card Play and Map Studio Redesign Plan: a card's setup plans. They are kept in a file beside the card, so they never change the
/// card's text, its SHA-256, or its games; reading checks their form; and the gate accepts each kept plan as it accepts any setup. Board 01 with its
/// real terrain, for The Guards Counterattack and The Tractor Works.
/// </summary>
public sealed class BacklogPass30Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a730");
    private static readonly GameScope Scope = new(Tenant, "p30");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p30-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly ScenarioCardLibrary cards;

    public BacklogPass30Tests()
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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog]);

    // The Tractor Works rolls for the first move as it starts (ruling R20.2); each roll's first die is a 6, its second a 1.
    private static DiceRoller FirstMoveRolls()
    {
        var die = 0;
        return new(_ => die++ % 2 == 0 ? 5 : 0);
    }

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: FirstMoveRolls());

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static SetupPlansRead Plans(string name) => ScenarioSetupPlans.Parse(Card(name), ScenarioSetupPlans.EmbeddedText(name)!, Catalog);

    /// <summary>A plan's placement as the setup action reads it, the way the Play page sends it.</summary>
    private static Dictionary<string, object?> Arguments(SetupPlan plan, SetupPlanPlacement item)
    {
        var placement = new Dictionary<string, object?> { ["id"] = item.Id, ["side"] = plan.Side, ["group"] = item.Group };
        var waits = item.OffBoard && item.Holder is null;
        object position = waits
            ? item.Entry is { } entry ? new Dictionary<string, object> { ["offMap"] = true, ["entry"] = entry } : new Dictionary<string, object> { ["offMap"] = true }
            : new Dictionary<string, object> { ["at"] = item.At ?? string.Empty };
        if (item.Dummy)
        {
            placement["kind"] = UnitKinds.Dummy;
            placement["position"] = position;
            placement["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false };
            return placement;
        }

        var definition = Catalog.Definition(item.Definition!)!;
        placement["kind"] = definition.Kind;
        placement["definition"] = definition.Id;
        var byVehicle = item.Holder is { } named && plan.Placements.Single(other => other.Id == named) is { Definition: { } holding }
            && Catalog.Definition(holding)?.Kind == "asl:vehicle";
        if (definition.Kind == "asl:vehicle")
        {
            placement["position"] = waits ? position : new
            {
                at = item.At,
                facing = item.Facing ?? "east"
            };
            placement["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = item.Concealed, ["asl:hidden"] = item.Hidden };
        }
        else if (definition.Kind == "asl:gun" && byVehicle)
        {
            placement["holding"] = new
            {
                holder = item.Holder,
                role = "towed"
            };
            placement["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false };
        }
        else if (definition.Kind == "asl:gun")
        {
            placement["position"] = new
            {
                at = item.At,
                facing = item.Facing ?? "east"
            };
            if (item.Holder is { } crew)
            {
                placement["holding"] = new
                {
                    holder = crew,
                    role = "manned"
                };
            }

            placement["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false, ["asl:concealed"] = item.Concealed, ["asl:hidden"] = item.Hidden };
            if (item.BoreSighted is { } sighted)
            {
                placement["boreSighted"] = sighted;
            }
        }
        else if (Vocabulary.IsA(definition.Kind, "asl:equipment") && item.Holder is { } holder)
        {
            placement["holding"] = new
            {
                holder,
                role = "possessed"
            };
            placement["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false };
        }
        else
        {
            placement["position"] = item.Holder is { } vehicle ? new Dictionary<string, object> { ["in"] = vehicle, ["role"] = "passenger" } : position;
            placement["conditions"] = new Dictionary<string, bool>
            {
                ["asl:broken"] = false,
                ["asl:berserk"] = false,
                ["asl:captured"] = false,
                ["asl:melee"] = false,
                ["asl:ti"] = false,
                ["asl:disrupted"] = false,
                ["asl:concealed"] = item.Concealed,
                ["asl:hidden"] = item.Hidden,
            };
        }

        return placement;
    }

    /// <summary>Starts a game from a card with one of its plans as the first setup.</summary>
    private async Task<PlayResult> SetUp(string card, SetupPlan plan)
    {
        var arguments = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-0",
            expectedRevision = 0L,
            start = new Dictionary<string, object?>
            {
                ["label"] = card,
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["scenario"] = new Dictionary<string, string> { ["id"] = card, ["sha256"] = ScenarioCards.Sha256(card)!, ["title"] = card },
            },
            placements = plan.Placements.Select(item => Arguments(plan, item)).ToArray(),
        });
        var play = Play();
        var proposed = await play.ProposeAsync(GameActions.Setup, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(GameActions.Setup, arguments, Player, proposed.Correlation);
    }

    [Theory]
    [InlineData("armor-test", "forward-screen")]
    [InlineData("gambit", "farmhouse", "west-woods", "two-posts")]
    [InlineData("guards-counterattack", "forward-line", "out-of-sight", "tripwire-and-reserve")]
    [InlineData("tractor-works", "all-round", "east-front", "hidden-core")]
    public void EveryBuiltInCardKeepsItsPlansInAFileOfValidForm(string name, params string[] ids)
    {
        var card = Card(name);
        var read = Plans(name);

        Assert.Empty(read.Diagnostics);
        Assert.Equal(ids, read.Plans.Select(plan => plan.Id));
        Assert.All(read.Plans, plan =>
        {
            // A plan is for the side that sets up first, and names the card text it was made for: the card's current text.
            Assert.Equal(card.Turns.SetsUpFirst, plan.Side);
            Assert.Equal(ScenarioCards.Sha256(name), plan.CardSha256);
            Assert.NotEmpty(plan.Terrain);
        });
        Assert.Equal(read.Plans.Select(plan => plan.Id), ScenarioCardLibrary.Embedded.Plans(name, Catalog)!.Plans.Select(plan => plan.Id));
    }

    [Fact]
    public void ABuiltInCardsHashIsOfItsOwnTextAlone()
    {
        Assert.All(ScenarioCards.Names, name =>
        {
            var text = ScenarioCards.EmbeddedText(name)!;
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))), ScenarioCards.Sha256(name));
            Assert.DoesNotContain("asl-setup-plans", text, StringComparison.Ordinal);
            Assert.NotNull(ScenarioSetupPlans.EmbeddedText(name));
        });
    }

    [Fact]
    public void AUsersCardHasTheSameHashWithAndWithoutItsSetupsFile()
    {
        Assert.Empty(cards.Save(Card("guards-counterattack") with
        {
            Id = "my-guards"
        }, Catalog));
        var without = cards.Sha256("my-guards");
        Assert.Null(cards.PlansText("my-guards"));
        Assert.Null(cards.Plans("my-guards", Catalog));

        var file = Path.Combine(cards.Directory!, "my-guards" + ScenarioSetupPlans.Suffix);
        File.WriteAllText(file, ScenarioSetupPlans.EmbeddedText("guards-counterattack")!.Replace("\"card\": \"guards-counterattack\"", "\"card\": \"my-guards\"", StringComparison.Ordinal));
        var read = cards.Plans("my-guards", Catalog)!;

        Assert.Empty(read.Diagnostics);
        Assert.Equal(3, read.Plans.Count);
        Assert.Equal(without, cards.Sha256("my-guards"));
        Assert.Equal([.. ScenarioCards.Names, "my-guards"], cards.Names);

        // The plans were made for another text of the card: they are still read, and the page marks them.
        Assert.All(read.Plans, plan => Assert.NotEqual(without, plan.CardSha256));

        // A file that cannot be read gives no plan and says why; it never throws, and the card is unchanged.
        File.WriteAllText(file, "{");
        var broken = cards.Plans("my-guards", Catalog)!;
        Assert.Empty(broken.Plans);
        Assert.Single(broken.Diagnostics);
        Assert.Equal(without, cards.Sha256("my-guards"));
    }

    [Fact]
    public void ASetupsFileOfBadFormIsRefusedWithItsReasons()
    {
        var card = Card("guards-counterattack");
        var text = ScenarioSetupPlans.EmbeddedText("guards-counterattack")!;
        var sha = ScenarioCards.Sha256("guards-counterattack")!;
        void Refused(string json, string prefix)
        {
            var read = ScenarioSetupPlans.Parse(card, json, Catalog);
            Assert.Empty(read.Plans);
            Assert.Contains(read.Diagnostics, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
        }

        string Plan(string id, string placements, string side = "german") =>
            $$"""{"id":"{{id}}","cardSha256":"{{sha}}","side":"{{side}}","name":"N","idea":"I","givesUp":"G","terrain":[],"placements":[{{placements}}]}""";
        string File(params string[] plans) => $$"""{"format":"asl-setup-plans/1","card":"guards-counterattack","plans":[{{string.Join(",", plans)}}]}""";
        const string Squad = """{"id":"g1","definition":"attacker-squad","group":"german-1","at":"bd01:F5:0"}""";

        Assert.Empty(ScenarioSetupPlans.Parse(card, File(Plan("one", Squad)), Catalog).Diagnostics);
        Refused("{", "setups.json:");
        Refused("null", "setups.json:");
        Refused("""{"format":"asl-setup-plans/1","card":"guards-counterattack"}""", "setups.plans:");
        Refused("""{"format":"asl-setup-plans/1","card":"guards-counterattack","plans":[null]}""", "setups.plans:");
        Refused(File(Plan("one", "null")), "setups.plans:");
        Refused(text.Replace("asl-setup-plans/1", "asl-setup-plans/2", StringComparison.Ordinal), "setups.format:");
        Refused(text.Replace("\"card\": \"guards-counterattack\"", "\"card\": \"gambit\"", StringComparison.Ordinal), "setups.card:");
        Refused(text.Replace("\"idea\":", "\"thought\":", StringComparison.Ordinal), "setups.json:");
        Refused(File(Plan("a", Squad), Plan("b", Squad), Plan("c", Squad), Plan("d", Squad)), "setups.plans: a card offers at most 3");
        Refused(File(Plan("a", Squad), Plan("a", Squad)), "setups.plans: two plans share an id");
        Refused(File(Plan("Not An Id", Squad)), "setups.plan:");
        Refused(File(Plan("one", Squad, side: "russian")), "setups.plan: 'one' is for russian");
        Refused(File(Plan("one", string.Empty)), "setups.plan: 'one' places at least one counter");
        Refused(File(Plan("one", Squad + "," + Squad)), "setups.plan: 'one' gives two counters one id");
        Refused(File(Plan("one", Squad.Replace("attacker-squad", "defender-squad", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' is a Dummy, or a german definition");
        Refused(File(Plan("one", Squad.Replace("attacker-squad", "no-such-counter", StringComparison.Ordinal))), "setups.placement:");
        Refused(File(Plan("one", Squad.Replace("german-1", "russian-1", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' names an OB group");
        Refused(File(Plan("one", Squad.Replace("bd01:F5:0", "bd09:F5:0", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' is at 'bd09:F5:0'");
        Refused(File(Plan("one", Squad.Replace("\"at\":\"bd01:F5:0\"", "\"at\":\"bd01:F5:0\",\"offBoard\":true", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' has a Location, a holder, or waits off board");
        Refused(File(Plan("one", Squad.Replace("\"at\":\"bd01:F5:0\"", "\"holder\":\"g2\"", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' is held by 'g2'");
        Refused(File(Plan("one", Squad.Replace("\"at\":\"bd01:F5:0\"", "\"at\":\"bd01:F5:0\",\"entry\":\"e1\"", StringComparison.Ordinal))), "setups.placement: 'one', counter 'g1' names an entry area");
        Refused(File(Plan("one", """{"id":"d1","dummy":true,"definition":"attacker-squad","group":"german-1","at":"bd01:F5:0"}""")), "setups.placement: 'one', counter 'd1' is a Dummy");
    }

    // The gate checks a plan like any setup: each kept plan on board 01 is accepted as the first setup of a game from its card's current text.
    [Theory]
    [InlineData("guards-counterattack", "forward-line")]
    [InlineData("guards-counterattack", "out-of-sight")]
    [InlineData("guards-counterattack", "tripwire-and-reserve")]
    [InlineData("tractor-works", "all-round")]
    [InlineData("tractor-works", "east-front")]
    [InlineData("tractor-works", "hidden-core")]
    public async Task TheGateAcceptsEveryKeptPlanOfBoard01(string name, string id)
    {
        var plan = Plans(name).Plans.Single(item => item.Id == id);

        var result = await SetUp(name, plan);

        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        Assert.Equal(ScenarioCards.Sha256(name), plan.CardSha256);
        var report = Planner().CardSetup(Current, new HashSet<string>())!;
        Assert.All(report.Groups.Where(group => plan.Placements.Any(item => item.Group == group.Id)), group => Assert.True(group.Complete, group.Id));
    }

    // A12.11: a side's own Dummies are drawn for it, as "?" of their kind alone; the other side is given no Dummy to tell from a real stack.
    [Fact]
    public async Task ASidesOwnDummiesAreDrawnForItAlone()
    {
        var plan = Plans("tractor-works").Plans.Single(item => item.Id == "all-round");
        var dummies = plan.Placements.Count(item => item.Dummy);
        Assert.True(dummies > 0);
        Assert.Equal(PlayOutcome.Committed, (await SetUp("tractor-works", plan)).Outcome);

        IReadOnlyList<LimboDancer.Domains.Asl.Units.Documents.UnitDocument> Documents(string side) =>
            GameDocuments.For(GameView.Of(Current, Current.Perspectives.Single(item => item.Name == side), []), Vocabulary, [Catalog]);

        Assert.Equal(dummies, Documents("russian").Count(document => document.Dummy));
        Assert.DoesNotContain(Documents("german"), document => document.Dummy);
    }
}
