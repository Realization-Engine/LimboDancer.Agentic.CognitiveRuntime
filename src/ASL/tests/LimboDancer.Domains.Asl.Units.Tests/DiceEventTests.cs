using System.Text;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The dice-rolled event (Random Selection and Declined OVR Design, section 4; DICE-09, DICE-12): its record format, and
/// the replay checks on its count, bounds, source, and identity. Replay uses the recorded values and never draws.
/// </summary>
public sealed class DiceEventTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static List<GameEvent> With(params DiceRolled[] rolls)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(9)];
        foreach (var roll in rolls)
        {
            events.Add(events[^1] with
            {
                EventId = "d" + events.Count,
                Revision = events[^1].Revision + 1,
                Type = "dice-rolled",
                Payload = roll,
                Causes = [],
                Visibility = null
            });
        }

        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());

    private static DiceRolled Roll(string id = "a1-roll-1", int count = 2, int sides = 6, int[]? values = null, string source = DiceRolled.SystemSource) =>
        new(id, "random-selection", count, sides, values ?? [3, 6], source, "studio-user");

    [Fact]
    public void ARecordedRollReplaysWithoutChangingState()
    {
        var history = Project(With(Roll()));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.Equal(history.At(9)!.Units, history.Current!.Units);
    }

    [Fact]
    public void ARollRoundTripsThroughTheGameRecord()
    {
        var events = With(Roll(values: [1, 6]));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("dice", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        var roll = Assert.IsType<DiceRolled>(read.Record!.Events[^1].Payload);
        Assert.Equal(("a1-roll-1", "random-selection", 2, 6, "system", "studio-user"), (roll.Roll, roll.Purpose, roll.Count, roll.Sides, roll.Source, roll.Actor));
        Assert.Equal([1, 6], roll.Values);
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
    }

    [Theory]
    [InlineData("out-of-bounds")]
    [InlineData("zero")]
    [InlineData("count-mismatch")]
    [InlineData("one-side")]
    [InlineData("too-many")]
    [InlineData("player-source")]
    [InlineData("twice")]
    public void ReplayRefusesARollThatCannotHaveBeenDrawn(string change)
    {
        var events = change switch
        {
            "out-of-bounds" => With(Roll(values: [3, 7])),
            "zero" => With(Roll(values: [0, 6])),
            "count-mismatch" => With(Roll(count: 3)),
            "one-side" => With(Roll(sides: 1, values: [1, 1])),
            "too-many" => With(Roll(count: 101, values: [.. Enumerable.Repeat(1, 101)])),
            "player-source" => With(Roll(source: "player")),
            _ => With(Roll(), Roll()),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-019");
    }
}
