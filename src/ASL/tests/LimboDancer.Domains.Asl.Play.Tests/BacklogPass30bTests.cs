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
/// Pass 30b of the Card Play and Map Studio Redesign Plan: setup plans for the side that sets up second. A plan may answer a plan of the other side
/// or be for any setup; the gate accepts each as it accepts any setup, once the plan it answers is in the game; and a plan is compared with what the
/// other side sees from a <see cref="GameView"/> alone, so two games that look the same to a side give it the same scores. Board 01 with its real
/// terrain, for The Guards Counterattack and The Tractor Works.
/// </summary>
public sealed class BacklogPass30bTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-0000000a730b");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p30b-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass30bTests() => store = new FileGameStore(Path.Combine(root, "games"));

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

    private static GameScope Scope(string game = "p30b") => new(Tenant, game);

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog]);

    // The Tractor Works rolls for the first move as it starts (ruling R20.2); each roll's first die is a 6, its second a 1.
    private static DiceRoller FirstMoveRolls()
    {
        var die = 0;
        return new(_ => die++ % 2 == 0 ? 5 : 0);
    }

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: FirstMoveRolls());

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static SetupPlansRead Plans(string name) => ScenarioSetupPlans.Parse(Card(name), ScenarioSetupPlans.EmbeddedText(name)!, Catalog);

    private static SetupPlan Plan(string card, string id) => Plans(card).Plans.Single(item => item.Id == id);

    /// <summary>Sets up a plan: as the first setup of a game from the card, or as the next setup of the game already started.</summary>
    private async Task<PlayResult> SetUp(string card, SetupPlan plan, string game = "p30b")
    {
        var scope = Scope(game);
        var events = store.Read(scope)?.Events.Count ?? 0;
        object[] placements = [.. plan.Placements.Select(item => BacklogPass30Tests.Arguments(plan, item))];
        var arguments = events == 0
            ? JsonSerializer.SerializeToElement(new
            {
                gameId = scope.Game,
                attemptId = "setup-0",
                expectedRevision = 0L,
                start = new Dictionary<string, object?>
                {
                    ["label"] = card,
                    ["catalog"] = "asl-scenario-a1@1.13.0",
                    ["scenario"] = new Dictionary<string, string> { ["id"] = card, ["sha256"] = ScenarioCards.Sha256(card)!, ["title"] = card },
                },
                placements,
            })
            : JsonSerializer.SerializeToElement(new
            {
                gameId = scope.Game,
                attemptId = "setup-" + events,
                expectedRevision = (long)events,
                placements,
            });
        var play = Play();
        var proposed = await play.ProposeAsync(GameActions.Setup, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(GameActions.Setup, arguments, Player, proposed.Correlation);
    }

    /// <summary>The game as a side may know it now, with the groups setting up out of its sight left out, as the Play page reads it.</summary>
    private GameView ViewOf(string side, string game = "p30b")
    {
        var history = Planner().Replay(store.Read(Scope(game))!.Events);
        var state = history.Current!;
        var perspective = state.Perspectives.Single(item => item.Name == side);
        return GameView.Of(history, state.Revision, perspective, Planner().OutOfSight(state, perspective));
    }

    [Theory]
    [InlineData("guards-counterattack", "russian", 3, 1)]
    [InlineData("gambit", "german", 3, 1)]
    [InlineData("tractor-works", "german", 0, 2)]
    [InlineData("armor-test", "german", 0, 1)]
    public void TheSecondSideHasItsAnswersAndItsPlansForAnySetup(string name, string side, int answers, int forAny)
    {
        var card = Card(name);
        var read = Plans(name);
        Assert.Empty(read.Diagnostics);
        Assert.NotEqual(card.Turns.SetsUpFirst, side);

        var second = read.Plans.Where(plan => plan.Side == side).ToArray();
        Assert.Equal(answers, second.Count(plan => plan.Answers is not null));
        Assert.Equal(forAny, second.Count(plan => plan.Answers is null));
        Assert.All(second, plan =>
        {
            Assert.Equal(2, ScenarioSetupPlans.OrderOf(card, plan));
            Assert.Equal(ScenarioCards.Sha256(name), plan.CardSha256);
        });

        // An answer names a plan of the side that sets up first, and the placements that plan has now.
        Assert.All(second.Where(plan => plan.Answers is not null), plan =>
        {
            var answered = read.Plans.Single(other => other.Id == plan.Answers!.Plan);
            Assert.Equal(card.Turns.SetsUpFirst, answered.Side);
            Assert.Equal(1, ScenarioSetupPlans.OrderOf(card, answered));
            Assert.Equal(ScenarioSetupPlans.PlacementsSha256(answered), plan.Answers!.PlacementsSha256);
        });
        Assert.Equal(second.Count(plan => plan.Answers is not null), second.Where(plan => plan.Answers is not null).Select(plan => plan.Answers!.Plan).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ThePlacementsHashChangesWithAPlacementAndNotWithTheWords()
    {
        var plan = Plan("guards-counterattack", "forward-line");
        var hash = ScenarioSetupPlans.PlacementsSha256(plan);

        Assert.Equal(hash, ScenarioSetupPlans.PlacementsSha256(plan with
        {
            Name = "Another name",
            Idea = "Another idea.",
            Terrain = []
        }));
        Assert.Equal(hash, ScenarioSetupPlans.PlacementsSha256(plan with
        {
            Placements = [.. plan.Placements.Reverse()]
        }));
        var moved = plan.Placements[0] with
        {
            At = "bd01:F6:0"
        };
        Assert.NotEqual(hash, ScenarioSetupPlans.PlacementsSha256(plan with
        {
            Placements = [moved, .. plan.Placements.Skip(1)]
        }));
    }

    [Fact]
    public void AFileOfTheEarlierFormatReadsAsBeforeAndTheNewFormIsCheckedForAnswers()
    {
        var card = Card("guards-counterattack");
        var sha = ScenarioCards.Sha256("guards-counterattack")!;
        string Plan(string id, string side, string placements, string answers = "") =>
            $$"""{"id":"{{id}}","cardSha256":"{{sha}}","side":"{{side}}",{{answers}}"name":"N","idea":"I","givesUp":"G","terrain":[],"placements":[{{placements}}]}""";
        string Answers(string plan) => "\"answers\":{\"plan\":\"" + plan + "\",\"placementsSha256\":\"" + new string('0', 64) + "\"},";
        string File(string format, params string[] plans) => $$"""{"format":"{{format}}","card":"guards-counterattack","plans":[{{string.Join(",", plans)}}]}""";
        const string German = """{"id":"g1","definition":"attacker-squad","group":"german-1","at":"bd01:F5:0"}""";
        const string Russian = """{"id":"r1","definition":"defender-squad","group":"russian-1","at":"bd01:N4:0"}""";
        const string Old = "asl-setup-plans/1";
        const string New = ScenarioSetupPlans.Format;

        IReadOnlyList<string> Reasons(string json)
        {
            var read = ScenarioSetupPlans.Parse(card, json, Catalog);
            Assert.True(read.Diagnostics.Count == 0 || read.Plans.Count == 0);
            return read.Diagnostics;
        }

        void Refused(string json, string prefix) => Assert.Contains(Reasons(json), reason => reason.StartsWith(prefix, StringComparison.Ordinal));

        // The earlier format: plans for the side that sets up first only, none an answer, three in all.
        Assert.Empty(Reasons(File(Old, Plan("one", "german", German))));
        Refused(File(Old, Plan("one", "russian", Russian)), "setups.plan: 'one' is for russian");
        Refused(File(Old, Plan("one", "german", German), Plan("two", "russian", Russian, Answers("one"))), "setups.plan: 'two' is for russian");
        Refused(File(Old, Plan("a", "german", German), Plan("b", "german", German), Plan("c", "german", German), Plan("d", "german", German)), "setups.plans: a card offers at most 3 plans, and the file has 4");

        // The new format: the second side has its answers and its plans for any setup.
        Assert.Empty(Reasons(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, Answers("one")), Plan("any", "russian", Russian))));
        Assert.Empty(Reasons(File(New, Plan("one", "german", German), Plan("a", "russian", Russian), Plan("b", "russian", Russian), Plan("c", "russian", Russian))));
        Refused(File("asl-setup-plans/3", Plan("one", "german", German)), "setups.format:");
        Refused(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, Answers("none"))), "setups.plan: 'two' answers 'none'");
        Refused(File(New, Plan("one", "german", German), Plan("two", "german", German, Answers("one"))), "setups.plan: 'two' answers 'one'");
        Refused(File(New, Plan("one", "russian", Russian), Plan("two", "german", German, Answers("one"))), "setups.plan: 'two' answers 'one'");
        Refused(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, Answers("two"))), "setups.plan: 'two' answers 'two'");
        Refused(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, Answers("one")), Plan("three", "russian", Russian, Answers("one"))), "setups.plans: 2 plans of russian answer 'one'");
        Refused(File(New, Plan("a", "russian", Russian), Plan("b", "russian", Russian), Plan("c", "russian", Russian), Plan("d", "russian", Russian)), "setups.plans: a card offers at most 3 plans for any setup to russian");
        Refused(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, "\"answers\":{\"plan\":\"one\",\"placementsSha256\":\"short\"},")), "setups.plan: 'two' names the SHA-256 of the placements");
        Refused(File(New, Plan("one", "german", German), Plan("two", "russian", Russian, "\"answers\":{},")), "setups.plan: 'two' answers");
        Refused(File(New, Plan("one", "nobody", German)), "setups.plan: 'one' is for nobody");

        // A plan places OB groups of one setup order: The Tractor Works' 308th is order 1 and its remnants order 3.
        var works = Card("tractor-works");
        var mixed = $$"""{"format":"{{New}}","card":"tractor-works","plans":[{"id":"mixed","cardSha256":"{{ScenarioCards.Sha256("tractor-works")}}","side":"russian","name":"N","idea":"I","givesUp":"G","terrain":[],"placements":[{"id":"a","definition":"defender-squad","group":"russian-1","at":"bd01:X4:0"},{"id":"b","definition":"defender-line-squad","group":"russian-2","at":"bd01:P8:0"}]}]}""";
        Assert.Contains(ScenarioSetupPlans.Parse(works, mixed, Catalog).Diagnostics, reason => reason.StartsWith("setups.plan: 'mixed' places OB groups of one setup order", StringComparison.Ordinal));
    }

    // The gate checks a second side's plan like any setup, with the plan it answers already in the game.
    [Theory]
    [InlineData("guards-counterattack", "forward-line", "fire-first")]
    [InlineData("guards-counterattack", "out-of-sight", "cross-unseen")]
    [InlineData("guards-counterattack", "tripwire-and-reserve", "break-the-tripwires")]
    [InlineData("guards-counterattack", "forward-line", "two-up-two-back")]
    [InlineData("guards-counterattack", "out-of-sight", "two-up-two-back")]
    [InlineData("tractor-works", "dummy-west", "three-sides")]
    [InlineData("tractor-works", "east-front", "feint-west")]
    [InlineData("tractor-works", "hidden-core", "three-sides")]
    public async Task TheGateAcceptsTheSecondSidesPlansOfBoard01(string name, string first, string second)
    {
        var opening = await SetUp(name, Plan(name, first));
        Assert.True(opening.Outcome == PlayOutcome.Committed, string.Join("; ", opening.Reasons));

        var plan = Plan(name, second);
        var result = await SetUp(name, plan);

        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        var report = Planner().CardSetup(Planner().Replay(store.Read(Scope())!.Events).Current!, new HashSet<string>())!;
        Assert.All(report.Groups.Where(group => plan.Placements.Any(item => item.Group == group.Id)), group => Assert.True(group.Complete, group.Id));
    }

    // The comparison: the plan used matches the stacks the other side sees in full, and the card's other plans do not.
    [Theory]
    [InlineData("forward-line")]
    [InlineData("out-of-sight")]
    [InlineData("tripwire-and-reserve")]
    public async Task ThePlanUsedMatchesWhatTheOtherSideSeesAndTheOthersDoNot(string used)
    {
        Assert.Equal(PlayOutcome.Committed, (await SetUp("guards-counterattack", Plan("guards-counterattack", used))).Outcome);
        var view = ViewOf("russian");

        foreach (var plan in Plans("guards-counterattack").Plans.Where(plan => plan.Side == "german"))
        {
            var score = SetupPlanMatch.Score(view, plan, Catalog);
            if (plan.Id == used)
            {
                Assert.Equal(score.Hexes, score.Matching);
                Assert.Equal(SetupPlanMatch.Footprint(plan, Catalog).Count, score.Hexes);
            }
            else
            {
                Assert.True(score.Share < 0.6, $"{plan.Id}: {score.Matching} of {score.Hexes}");
            }
        }

        // What is counted is the 18 units, a top counter or a counted one in each stack; the SW are not counted, whoever holds them.
        Assert.Equal(18, SetupPlanMatch.Seen(view, "german").Values.Sum());
    }

    // Two games that look the same to a side give it the same scores, whatever stands under the "?": the comparison reads the view alone.
    [Fact]
    public async Task GamesThatLookTheSameGiveTheSameScores()
    {
        string[] plans = ["dummy-west", "east-front", "hidden-core"];
        var seen = new List<IReadOnlyDictionary<LimboDancer.Domains.Asl.Maps.Coordinates.BoardLocation, int>>();
        var scores = new List<SetupPlanScore[]>();
        foreach (var used in plans)
        {
            Assert.Equal(PlayOutcome.Committed, (await SetUp("tractor-works", Plan("tractor-works", used), "works-" + used)).Outcome);
            var view = ViewOf("german", "works-" + used);

            // Every counter of the 308th is under "?": the Germans see no unit of it, only sealed presences.
            Assert.DoesNotContain(view.Units, unit => unit.Side == "russian");
            seen.Add(SetupPlanMatch.Seen(view, "russian"));
            scores.Add([.. plans.Select(id => SetupPlanMatch.Score(view, Plan("tractor-works", id), Catalog))]);
        }

        Assert.All(seen, hexes => Assert.Equal(seen[0].OrderBy(item => item.Key.ToString(), StringComparer.Ordinal), hexes.OrderBy(item => item.Key.ToString(), StringComparer.Ordinal)));
        Assert.Equal(25, seen[0].Values.Sum());
        Assert.All(scores, game => Assert.All(game, score => Assert.Equal(new SetupPlanScore(9, 9), score)));
    }

    // Plans that look alike to the other side: the same "?" and the same top counters in the same Locations.
    [Theory]
    [InlineData("tractor-works", "dummy-west", "east-front", "hidden-core")]
    [InlineData("tractor-works", "three-sides", "feint-west")]
    [InlineData("armor-test", "forward-screen", "west-trap", "back-stop")]
    public void TheCardsLookAlikePlansShowTheOtherSideTheSameStacks(string name, params string[] ids)
    {
        var looks = ids.Select(id => SetupPlanMatch.Look(Plan(name, id), Catalog)).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Single(looks);
        Assert.Single(ids.Select(id => string.Join(',', SetupPlanMatch.Footprint(Plan(name, id), Catalog).OrderBy(item => item.Key.ToString(), StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"))).Distinct(StringComparer.Ordinal));

        // They differ beneath: no two place the same counters in the same way.
        Assert.Equal(ids.Length, ids.Select(id => ScenarioSetupPlans.PlacementsSha256(Plan(name, id))).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void APlansLookAndFootprintLeaveOutWhatTheOtherSideDoesNotSee()
    {
        // The Guards Counterattack's German plans differ to the eye.
        string[] german = ["forward-line", "out-of-sight", "tripwire-and-reserve"];
        Assert.Equal(3, german.Select(id => SetupPlanMatch.Look(Plan("guards-counterattack", id), Catalog)).Distinct(StringComparer.Ordinal).Count());

        // Forward line: 26 counters, of which the 8 SW are held and not seen; M7 holds counters on two levels, which the footprint counts as one hex.
        var forward = Plan("guards-counterattack", "forward-line");
        var footprint = SetupPlanMatch.Footprint(forward, Catalog);
        Assert.Equal(10, footprint.Count);
        Assert.Equal(forward.Placements.Count(item => item.Holder is null), footprint.Values.Sum());
        Assert.Equal(18, footprint.Values.Sum());

        // Armor Test: the hidden Gun and its crew are in no footprint, and the column off board has none at all.
        var screen = SetupPlanMatch.Footprint(Plan("armor-test", "forward-screen"), Catalog);
        Assert.Equal(["bd04:O5:0=2", "bd04:P6:0=1", "bd04:P8:0=1", "bd04:Q8:0=1"], screen.OrderBy(item => item.Key.ToString(), StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"));
        Assert.Empty(SetupPlanMatch.Footprint(Plan("armor-test", "loaded-column"), Catalog));
        Assert.Equal(string.Empty, SetupPlanMatch.Look(Plan("armor-test", "loaded-column"), Catalog));
    }
}
