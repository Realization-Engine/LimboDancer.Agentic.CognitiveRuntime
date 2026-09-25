using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// Open attempts and the random-selection event (Random Selection and Declined OVR Design, section 4). Revision 9 of the
/// synthetic fixture is the German MPh with g1 in D4 and the concealed r1 in E4; a second concealed squad, r9, joins it.
/// </summary>
public sealed class RandomSelectionEventTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly BoardLocation D4 = BoardLocation.Parse("bd01:D4:0");
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");

    private static readonly Dictionary<string, ConditionState> Revealed = new() { [Conditions.Concealed] = ConditionState.False };

    private static List<GameEvent> With(params (string Id, string Type, EventPayload Payload, string[] Causes)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(9)];
        var squad = new NewInstance("r9", "asl:squad", "defender-squad", "russian", new MapPosition(E4), null,
            new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.True, [Conditions.Hidden] = ConditionState.False });
        foreach (var (id, type, payload, causes) in added.Prepend(("c1", "instance-created", new InstanceCreated(squad), [])))
        {
            events.Add(events[^1] with
            {
                EventId = id,
                Revision = events[^1].Revision + 1,
                Type = type,
                Payload = payload,
                Causes = causes,
                Visibility = null
            });
        }

        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());

    private static (string, string, EventPayload, string[]) Attempt(string unit = "g1") => ("a1", "entry-attempted", new EntryAttempted(unit, E4, 2), []);

    private static (string, string, EventPayload, string[]) Roll(params int[] values) =>
        ("a2", "dice-rolled", new DiceRolled("a1-roll-1", "random-selection", values.Length, 6, values, DiceRolled.SystemSource, "player"), ["a1"]);

    private static (string, string, EventPayload, string[]) Select(params string[] subjects) =>
        ("a3", "random-selection", new RandomSelection("a1-roll-1", "a1", subjects), ["a1"]);

    [Fact]
    public void TheHighestDieSelectsTheUnitsTheAttemptMustReveal()
    {
        var history = Project(With(Attempt(), Roll(2, 5), Select("r1", "r9"), ("a4", "conditions-changed", new ConditionsChanged("r9", Revealed), ["a1"]),
            ("a5", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1"])));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.Equal(["r9"], history.At(13)!.OpenAttempts.Single().Revealing);
        Assert.Empty(history.Current!.OpenAttempts);
    }

    [Fact]
    public void ATieSelectsEveryTiedUnit()
    {
        var history = Project(With(Attempt(), Roll(4, 4), Select("r1", "r9")));
        Assert.Equal(["r1", "r9"], history.Current!.OpenAttempts.Single().Revealing);
    }

    [Fact]
    public void TheSelectionRoundTripsThroughTheGameRecord()
    {
        var events = With(Attempt(), Roll(2, 5), Select("r1", "r9"));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("selection", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(["r1", "r9"], Assert.IsType<RandomSelection>(read.Record!.Events[^1].Payload).Subjects);
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
    }

    [Theory]
    [InlineData("count", "UNIT-STATE-020")]
    [InlineData("not-at-target", "UNIT-STATE-020")]
    [InlineData("unknown-roll", "UNIT-STATE-020")]
    [InlineData("no-attempt", "UNIT-STATE-020")]
    [InlineData("twice", "UNIT-STATE-020")]
    [InlineData("reveal-unselected", "UNIT-STATE-020")]
    [InlineData("forced-back-before-reveal", "UNIT-STATE-020")]
    [InlineData("phase-change", "UNIT-STATE-018")]
    [InlineData("move", "UNIT-STATE-018")]
    [InlineData("second-attempt", "UNIT-STATE-018")]
    public void ReplayRefusesASelectionOrAnOpenAttemptThatBreaksItsRules(string change, string code)
    {
        var events = change switch
        {
            "count" => With(Attempt(), Roll(2, 5), Select("r1")),
            "not-at-target" => With(Attempt(), Roll(2, 5), Select("r1", "g1")),
            "unknown-roll" => With(Attempt(), ("a3", "random-selection", new RandomSelection("other", "a1", ["r1"]), ["a1"])),
            "no-attempt" => With(("a2", "dice-rolled", new DiceRolled("a1-roll-1", "random-selection", 2, 6, [2, 5], DiceRolled.SystemSource, "player"), []),
                ("a3", "random-selection", new RandomSelection("a1-roll-1", "a1", ["r1", "r9"]), [])),
            "twice" => With(Attempt(), Roll(2, 5), Select("r1", "r9"), ("a4", "random-selection", new RandomSelection("a1-roll-1", "a1", ["r1", "r9"]), ["a1"])),
            "reveal-unselected" => With(Attempt(), Roll(2, 5), Select("r1", "r9"), ("a4", "conditions-changed", new ConditionsChanged("r1", Revealed), ["a1"])),
            "forced-back-before-reveal" => With(Attempt(), Roll(2, 5), Select("r1", "r9"),
                ("a4", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1"])),
            "phase-change" => With(Attempt(), ("p1", "phase-changed", new PhaseChanged(1, "dfph", "german"), [])),
            "move" => With(Attempt(), ("m1", "instance-moved", new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd01:D5:0")), 1), [])),
            _ => With(Attempt(), ("a9", "entry-attempted", new EntryAttempted("g1", BoardLocation.Parse("bd01:D5:0"), 1), [])),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == code);
    }
}
