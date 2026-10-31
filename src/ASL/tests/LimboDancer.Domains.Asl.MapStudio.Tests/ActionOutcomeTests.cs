using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Replay;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class ActionOutcomeTests
{
    [Fact]
    public void RecordedRolesDoNotInferAPairFromTheNumberOfDice()
    {
        Assert.Equal([AslDieRole.Colored, AslDieRole.White], AslRollView.Recorded("ift", "fire-ift", [4, 6]).Dice.Select(die => die.Role));
        Assert.All(AslRollView.Recorded("selection", "fire-cower-selection", [1, 1]).Dice, die => Assert.Equal(AslDieRole.Unspecified, die.Role));
        var subjects = AslRollView.Recorded("selection", "random-selection", [2, 5], subjects: ["Squad A", "Squad B"]);
        Assert.Equal(["Squad A", "Squad B"], subjects.Dice.Select(die => die.Label));
        Assert.All(subjects.Dice, die => Assert.Equal(AslDieRole.Independent, die.Role));
        Assert.Equal(AslDieRole.Subsequent, Assert.Single(AslRollView.Recorded("subsequent", "ordnance-subsequent", [5]).Dice).Role);
    }

    [Fact]
    public void OutcomesKeepMoreThanSixRollsWithoutDisclosingOtherSidesOrFutureSubjects()
    {
        var scope = new GameScope(Guid.Empty, "outcome-check");
        GameEvent Event(int revision, EventPayload payload, IReadOnlyList<string>? visibility = null) =>
            new(scope, $"attempt-123-{revision}", revision, DateTimeOffset.UnixEpoch, "test", "dice-rolled", payload, null, [], visibility);
        var events = Enumerable.Range(1, 8).Select(index => Event(index,
            new DiceRolled($"roll-{index}", "random-selection", 2, 6, [2, 5], "system", "test"), index == 8 ? ["russian"] : null)).ToList();
        events.Add(Event(9, new RandomSelection("roll-1", "attempt-123", ["private-a", "private-b"]), ["russian"]));
        var history = new GameHistory(events, [], []);
        var german = new PlayRecords(history, new Perspective("german"), 9);
        var visible = Assert.Single(german.Outcomes).Rolls;
        Assert.Equal(7, visible.Count);
        Assert.Equal(7, visible.Select(roll => roll.Roll).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(6, german.RollRows.Count);
        Assert.DoesNotContain(visible.SelectMany(roll => roll.Display!.Dice), die => die.Label.StartsWith("private", StringComparison.Ordinal));
        var russian = new PlayRecords(history, new Perspective("russian"), 9);
        Assert.Equal(8, Assert.Single(russian.Outcomes).Rolls.Count);
        Assert.Equal("private-a", Assert.Single(russian.Outcomes).Rolls[0].Display!.Dice[0].Label);
        var earlier = new PlayRecords(history, new Perspective("russian"), 4);
        Assert.Equal(4, Assert.Single(earlier.Outcomes).Rolls.Count);
        Assert.Equal("Die 1", Assert.Single(earlier.Outcomes).Rolls[0].Display!.Dice[0].Label);
    }

    [Fact]
    public void ReplayKeepsVisibleMapChangesWhenThereIsNoReadableOutcome()
    {
        using var context = new BunitContext();
        var panel = context.Render<ReplayStepPanel>(parameters => parameters
            .Add(item => item.Step, new ReplayStep(1, 1, 1, "hidden", "action", 1, "mph", "german", null, "Map changed", ReplayRead.None))
            .Add(item => item.Count, 1)
            .Add(item => item.Changes, ["A visible counter left [G4]"]));
        Assert.Equal("A visible counter left [G4]", panel.Find("#replay-changes").TextContent.Trim());
        Assert.Contains("nothing of this step but what it sees change", panel.Find("#replay-step-part").TextContent, StringComparison.Ordinal);
        Assert.Empty(panel.FindAll("button"));
    }
}
