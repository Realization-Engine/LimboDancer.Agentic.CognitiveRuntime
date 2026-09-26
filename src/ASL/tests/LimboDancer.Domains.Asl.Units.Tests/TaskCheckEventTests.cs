using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The task-check event and the forced back after an elected OVR (Infantry OVR Design, sections 4 and 5). Revision 9
/// of the synthetic fixture is the German MPh with g1 (printed morale 7) in D4 and the concealed r1 in E4; a second
/// concealed squad, r9, joins it.
/// </summary>
public sealed class TaskCheckEventTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly BoardLocation D4 = BoardLocation.Parse("bd01:D4:0");
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");
    private static readonly Dictionary<string, ConditionState> Revealed = new() { [Conditions.Concealed] = ConditionState.False };
    private static readonly TaskCheckModifier StoneTem = new("B23.3", 3);

    private static List<GameEvent> With(params (string Id, string Type, EventPayload Payload, string[] Causes)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(9)];
        var squad = new NewInstance("r9", "asl:squad", "defender-squad", "russian", new MapPosition(E4), null,
            new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.True, [Conditions.Hidden] = ConditionState.False });
        (string, string, EventPayload, string[])[] prefix =
        [
            ("c1", "instance-created", new InstanceCreated(squad), []),
            ("a1", "entry-attempted", new EntryAttempted("g1", E4, 2), []),
            ("a2", "conditions-changed", new ConditionsChanged("r1", Revealed), ["a1"]),
            ("d1", "overrun-declared", new OverrunDeclared("g1", "a1", OverrunDeclared.Elected), ["a1"]),
        ];
        foreach (var (id, type, payload, causes) in prefix.Concat(added))
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

    private static (string, string, EventPayload, string[]) Ntc(params int[] values) =>
        ("n1", "dice-rolled", new DiceRolled("d1-roll-1", "ovr-ntc", values.Length, 6, values, DiceRolled.SystemSource, "player"), ["a1"]);

    private static (string, string, EventPayload, string[]) Check(int finalDr, bool passed, int morale = 7, string purpose = TaskCheck.OvrNtc) =>
        ("t1", "task-check", new TaskCheck("g1", "d1-roll-1", purpose, morale, [StoneTem], finalDr, passed), ["a1"]);

    private static readonly (string, string, EventPayload, string[]) ForcedBack =
        ("f1", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1", "d1"]);

    [Fact]
    public void AFailedNtcForcesTheMoverBack()
    {
        var history = Project(With(Ntc(4, 4), Check(11, false), ForcedBack));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.True(history.Current!.Unit("g1")!.MovementEnded);
        Assert.Empty(history.Current.OpenAttempts);
    }

    [Fact]
    public void APassedNtcIsForcedBackOnlyAfterTheSecondReveal()
    {
        Assert.Contains(Project(With(Ntc(1, 2), Check(6, true), ForcedBack)).Diagnostics, item => item.Code == "UNIT-STATE-021");

        var revealed = With(Ntc(1, 2), Check(6, true),
            ("s1", "dice-rolled", new DiceRolled("d1-roll-2", "random-selection", 1, 6, [4], DiceRolled.SystemSource, "player"), ["a1"]),
            ("s2", "random-selection", new RandomSelection("d1-roll-2", "a1", ["r9"]), ["a1"]),
            ("s3", "conditions-changed", new ConditionsChanged("r9", Revealed), ["a1"]),
            ForcedBack);
        var history = Project(revealed);
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.True(history.Current!.Unit("g1")!.MovementEnded);
    }

    [Fact]
    public void TheTaskCheckRoundTripsThroughTheGameRecord()
    {
        var events = With(Ntc(4, 4), Check(11, false));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("check", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        var check = Assert.IsType<TaskCheck>(read.Record!.Events[^1].Payload);
        Assert.Equal(("g1", "d1-roll-1", 7, 11, false), (check.Id, check.Roll, check.MoraleLevel, check.FinalDr, check.Passed));
        Assert.Equal([StoneTem], check.Modifiers);
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
    }

    [Theory]
    [InlineData("wrong-final-dr", "UNIT-STATE-022")]
    [InlineData("wrong-result", "UNIT-STATE-022")]
    [InlineData("wrong-morale", "UNIT-STATE-022")]
    [InlineData("three-dice", "UNIT-STATE-022")]
    [InlineData("other-purpose", "UNIT-STATE-022")]
    [InlineData("twice", "UNIT-STATE-022")]
    [InlineData("without-election", "UNIT-STATE-022")]
    [InlineData("selection-before-ntc", "UNIT-STATE-020")]
    [InlineData("selection-after-failed-ntc", "UNIT-STATE-020")]
    public void ReplayRefusesATaskCheckOrSelectionThatBreaksItsRules(string change, string code)
    {
        var selection = ("s2", "random-selection", (EventPayload)new RandomSelection("d1-roll-1", "a1", ["r9", "r1"]), new[] { "a1" });
        var events = change switch
        {
            "wrong-final-dr" => With(Ntc(4, 4), Check(10, false)),
            "wrong-result" => With(Ntc(1, 2), Check(6, false)),
            "wrong-morale" => With(Ntc(1, 2), Check(6, true, morale: 8)),
            "three-dice" => With(Ntc(1, 1, 1), Check(6, true)),
            "other-purpose" => With(Ntc(1, 2), Check(6, true, purpose: "ptc")),
            "twice" => With(Ntc(1, 2), Check(6, true), ("t2", "task-check", new TaskCheck("g1", "d1-roll-1", TaskCheck.OvrNtc, 7, [StoneTem], 6, true), ["a1"])),
            "without-election" => [.. With(Ntc(1, 2), Check(6, true)).Where(item => item.EventId != "d1")
                .Select((item, index) => item with { Revision = index + 1, Causes = [.. item.Causes.Where(cause => cause != "d1")] })],
            "selection-before-ntc" => With(Ntc(3, 5), selection),
            _ => With(Ntc(4, 4), Check(11, false), selection),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == code);
    }
}
