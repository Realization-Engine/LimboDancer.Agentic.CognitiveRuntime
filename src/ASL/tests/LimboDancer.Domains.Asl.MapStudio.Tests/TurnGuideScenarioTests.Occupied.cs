using System.Text.Json;
using ASL.Turns;
using Bunit;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed partial class TurnGuideScenarioTests
{
    private static readonly string[] OccupiedBoards = ["bd01"];
    private static readonly int[] CasualtyDice = [2, 4, 5, 3, 3];
    private static readonly int[] ChoiceDice = [4, 4, 1, 1, 3, 3];

    private async Task OccupiedSetup(string suffix)
    {
        // A separate authored encounter on the real board, not a change to the recorded scenario or its OB.
        game = "turn-guide-occupied" + suffix;
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = "Occupied DC acceptance" + suffix,
                catalog = "asl-scenario-a1@1.13.0",
                boards = OccupiedBoards,
                firstSide = "german",
                scenarioMonth = 10,
                scenarioYear = 1942,
                sides = new[] { new { id = "german", nationality = "german", elr = 5 }, new { id = "russian", nationality = "russian", elr = 5 } }
            },
            placements = new object[]
            {
                new { id = "g-engineer-3", kind = "asl:squad", definition = "attacker-elite-squad-8-3-8", side = "german",
                    position = new { at = "bd01:BB4:0" }, conditions = new Dictionary<string, bool> { [Conditions.Broken] = false, [Conditions.Concealed] = false, [Conditions.Hidden] = false } },
                new { id = "g-dc-3", kind = "asl:dc", definition = "attacker-dc", side = "german",
                    holding = new { holder = "g-engineer-3", role = "possessed" }, conditions = new Dictionary<string, bool> { [Conditions.Malfunctioned] = false } },
                new { id = "r-target", kind = "asl:squad", definition = "defender-squad", side = "russian",
                    position = new { at = "bd01:BB3:0" }, conditions = new Dictionary<string, bool> { [Conditions.Broken] = false, [Conditions.Concealed] = false, [Conditions.Hidden] = false } }
            }
        });
    }

    [TurnGuideScenarioFact]
    public async Task OccupiedDcCasualtyReductionAndMoraleSurvivePhaseCloseAndReplay()
    {
        await OccupiedSetup(string.Empty);
        var page = await PlaceAndReachAfph();
        SaveBrowserSeed(); // The browser must author its own opening review.
        ReviewInUi(page);
        var before = Current.Revision;
        dice.Enqueue(CasualtyDice); // 30 FP K/4, Random Selection, failed 4MC without ELR replacement.
        Commit(page, ".turn-action");
        Assert.Empty(dice.Queued);
        var events = live.Store.Read(Scope)!.Events.Skip((int)before).ToArray();
        var fire = Assert.Single(events.Select(item => item.Payload).OfType<FireResolved>());
        var resolution = fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)!;
        Assert.Equal("K/4", resolution.Arithmetic!.Result);
        var lineage = Assert.Single(events.Select(item => item.Payload).OfType<LineageRecorded>());
        Assert.Equal(LineageAction.Reduced, lineage.Action);
        Assert.Equal("r-target", Assert.Single(lineage.Consumed));
        var survivorId = Assert.Single(lineage.Produced).Id;
        Assert.Equal(InstanceStatus.Consumed, Current.Unit("r-target")!.Status);
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(survivorId)!, Conditions.Broken));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(survivorId)!, Conditions.DesperationMorale));
        Assert.Empty(Current.PlacedCharges);
        Assert.Null(Current.Choice);
        Assert.Contains("Resolved", page.Find(".turn-evidence").TextContent, StringComparison.Ordinal);
        Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Commit(page, ".turn-close");
        Assert.Equal("rtph", Current.Phase);
        var replay = live.Planner.Replay(live.Store.Read(Scope)!.Events);
        Assert.False(replay.HasErrors);
        Assert.Equal(ConditionState.True, GameState.Condition(replay.Current!.Unit(survivorId)!, Conditions.Broken));
        Assert.Equal(ConditionState.True, GameState.Condition(replay.Current.Unit(survivorId)!, Conditions.DesperationMorale));
    }

    [TurnGuideScenarioFact]
    public async Task OccupiedDcPendingConsequenceBlocksCloseUntilTheOwningSideAnswers()
    {
        await OccupiedSetup("-choice");
        var page = await PlaceAndReachAfph();
        SaveBrowserSeed();
        ReviewInUi(page);
        dice.Enqueue(ChoiceDice); // MC snake eyes, then optional Battle Hardening.
        Commit(page, ".turn-action");
        Assert.Equal(ChoicePending.BattleHardening, Current.Choice!.Kind);
        Assert.Equal("russian", Current.Choice.Side);
        Assert.Contains("required decision still pending", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("and is removed", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Assert.True(page.Markup.IndexOf("id=\"play-waiting\"", StringComparison.Ordinal) < page.Markup.IndexOf("class=\"turn-guide\"", StringComparison.Ordinal));
        Assert.Single(Current.PlacedCharges);
        Assert.Empty(page.FindAll(".turn-action, #play-choice .choose"));
        var before = Current.Revision;
        page.Find(".turn-close").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before, Current.Revision);
        Assert.Equal(TurnCloseDisposition.Blocked, (await live.Planner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-work-v1")).PhaseClose.Disposition);
        page.ViewAs("russian");
        Assert.NotEmpty(page.FindAll("#play-choice .choose"));
        Assert.True(page.Markup.IndexOf("id=\"play-choice\"", StringComparison.Ordinal) < page.Markup.IndexOf("class=\"turn-guide\"", StringComparison.Ordinal));
        dice.Enqueue(Enumerable.Repeat(6, 10));
        Commit(page, "#play-choice .choose[data-option=decline]");
        Assert.Null(Current.Choice);
        Assert.Empty(Current.PlacedCharges);
        Assert.Single(live.Store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>());
        page.ViewAs("german");
        page.WaitForAssertion(() => Assert.Contains("Resolved", page.Find(".turn-evidence").TextContent, StringComparison.Ordinal));
        Commit(page, ".turn-close");
        Assert.Equal("rtph", Current.Phase);
        Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Assert.False(live.Planner.Replay(live.Store.Read(Scope)!.Events).HasErrors);
    }
}
