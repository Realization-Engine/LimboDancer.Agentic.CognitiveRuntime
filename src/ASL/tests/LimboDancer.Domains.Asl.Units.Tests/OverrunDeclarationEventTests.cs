using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The overrun-declared event (Random Selection and Declined OVR Design, section 4): made once, by the attempt's unit,
/// after every selected unit is revealed; a decline may be forced back and an election may not. Revision 9 of the
/// synthetic fixture is the German MPh with g1 in D4 and the concealed r1 in E4.
/// </summary>
public sealed class OverrunDeclarationEventTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly BoardLocation D4 = BoardLocation.Parse("bd01:D4:0");
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");

    private static List<GameEvent> With(params (string Id, string Type, EventPayload Payload, string[] Causes)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(9)];
        foreach (var (id, type, payload, causes) in added)
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

    private static readonly (string, string, EventPayload, string[]) Attempt = ("a1", "entry-attempted", new EntryAttempted("g1", E4, 2), []);

    private static readonly (string, string, EventPayload, string[]) Reveal =
        ("a2", "conditions-changed", new ConditionsChanged("r1", new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False }), ["a1"]);

    private static (string, string, EventPayload, string[]) Declared(string choice, string unit = "g1", string attempt = "a1") =>
        ("d1", "overrun-declared", new OverrunDeclared(unit, attempt, choice), ["a1"]);

    private static readonly (string, string, EventPayload, string[]) ForcedBack =
        ("f1", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1", "d1"]);

    [Fact]
    public void ADeclineIsRecordedAndThenForcedBack()
    {
        var history = Project(With(Attempt, Reveal, Declared(OverrunDeclared.Declined), ForcedBack));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.Equal(OverrunDeclared.Declined, history.At(12)!.OpenAttempts.Single().Declaration);
        Assert.Empty(history.Current!.OpenAttempts);
        Assert.True(history.Current.Unit("g1")!.MovementEnded);
    }

    [Fact]
    public void TheDeclarationRoundTripsThroughTheGameRecord()
    {
        var events = With(Attempt, Reveal, Declared(OverrunDeclared.Declined));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("declaration", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(new OverrunDeclared("g1", "a1", OverrunDeclared.Declined), read.Record!.Events[^1].Payload);
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
    }

    [Theory]
    [InlineData("no-attempt")]
    [InlineData("other-unit")]
    [InlineData("other-choice")]
    [InlineData("twice")]
    [InlineData("elected-forced-back")]
    public void ReplayRefusesADeclarationThatBreaksItsRules(string change)
    {
        var events = change switch
        {
            "no-attempt" => With(("d1", "overrun-declared", new OverrunDeclared("g1", "a1", OverrunDeclared.Declined), [])),
            "other-unit" => With(Attempt, Reveal, Declared(OverrunDeclared.Declined, unit: "g2")),
            "other-choice" => With(Attempt, Reveal, Declared("maybe")),
            "twice" => With(Attempt, Reveal, Declared(OverrunDeclared.Declined), ("d2", "overrun-declared", new OverrunDeclared("g1", "a1", OverrunDeclared.Elected), ["a1"])),
            _ => With(Attempt, Reveal, Declared(OverrunDeclared.Elected), ForcedBack),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-021");
    }
}
