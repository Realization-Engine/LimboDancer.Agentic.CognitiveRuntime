using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed partial class TurnHorseIdentityTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);
    private const string Designation = "scenario-mule-designation";
    private static List<GameEvent> Beginning()
    {
        var first = UnitGames.Read("a1-village.synthetic")!.Record!.Events[0];
        return [first with { Payload = ((GameStarted)first.Payload) with { SpecialRules = [Designation] } }];
    }
    private static NewInstance Horse(string id = "mule") => new(id, "asl:horse", null, "german", new MapPosition(BoardLocation.Parse("bd01:E4:0")), null,
        new Dictionary<string, ConditionState>())
    {
        Horse = new(TurnHorseSize.Squad, true, Designation)
    };
    private static void Add(List<GameEvent> events, EventPayload payload, string type = "instance-created") => events.Add(events[^1] with
    {
        EventId = $"horse-event-{events.Count + 1}",
        Revision = events.Count + 1,
        Payload = payload,
        Type = type,
        Causes = [],
        Visibility = null
    });
    private static GameHistory Project(IReadOnlyList<GameEvent> events) => GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());

    [Fact]
    public void ScenarioMuleIsASeparateHorseCounterAndSurvivesJournalRoundTrip()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse()));
        var json = GameEventWriter.Write(events[0].Scope, new("packing setup", true, events));
        var parsed = GameEventReader.Read(Encoding.UTF8.GetBytes(json));
        Assert.False(parsed.HasErrors);
        var history = Project(parsed.Record!.Events);
        Assert.False(history.HasErrors);
        var horse = Assert.Single(history.Current!.Entities);
        Assert.Equal("asl:horse", horse.Kind);
        Assert.Equal(Horse().Horse, horse.Horse);
        Assert.Null(history.Current.Unit("mule"));
        Assert.Same(horse, history.Current.Find("mule"));
        Assert.Equal("bd01:E4:0", history.Current.Location("mule")!.Location.ToString());
        Assert.True(UnitsTestData.Asl.Value.IsA(horse.Kind, "asl:entity"));
        Assert.False(UnitsTestData.Asl.Value.IsA(horse.Kind, "asl:personnel"));
        Assert.False(UnitsTestData.Asl.Value.IsA(horse.Kind, "asl:vehicle"));
        Assert.Equal(horse.Horse, Assert.Single(GameView.Of(history, 2, Perspective.Side("german")).Entities).Horse);
        Assert.Equal(horse.Horse, Assert.Single(GameView.Of(history, 2, Perspective.Adjudicator).Entities).Horse);
    }

    [Fact]
    public void UnknownSizeOrDesignationRemainsUnknownAndLegacyEntitiesHaveNoHorseMetadata()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse() with
        {
            Horse = new(TurnHorseSize.Unknown, null, null)
        }));
        Add(events, new InstanceCreated(new("wire", "asl:wire", null, null, new MapPosition(BoardLocation.Parse("bd01:E4:0")), null, new Dictionary<string, ConditionState>())));
        var state = Project(events).Current!;
        Assert.Null(state.Entities.Single(item => item.Id == "mule").Horse!.Mule);
        Assert.Equal(TurnHorseSize.Unknown, state.Entities.Single(item => item.Id == "mule").Horse!.Size);
        Assert.Null(state.Entities.Single(item => item.Id == "wire").Horse);
    }

    [Fact]
    public void InvalidDesignationsRolesAndLateCreationAreRefused()
    {
        NewInstance[] invalid =
        [
            Horse() with { Horse = null }, Horse() with { Horse = new((TurnHorseSize)99, null, null) },
            Horse() with { Horse = new(TurnHorseSize.Smc, true, Designation) },
            Horse() with { Horse = new(TurnHorseSize.Squad, true, "unrecorded") },
            Horse() with { Horse = new(TurnHorseSize.Squad, true, null) },
            Horse() with { Horse = new(TurnHorseSize.Squad, false, Designation) },
            Horse() with { Horse = new(TurnHorseSize.Squad, null, Designation) },
            Horse() with { Side = null }, Horse() with { Position = NotEnteredPosition.Instance },
            Horse() with { Kind = "asl:wire" }, Horse() with { Definition = "attacker-squad" },
            Horse() with { Holding = new("crew", HoldingRole.Possessed) },
            Horse() with { Group = "ob-group" }, Horse() with { CrewOf = "vehicle" }, Horse() with { ArmorLeader = new(9, -1) }
        ];
        foreach (var item in invalid)
        {
            var events = Beginning();
            Add(events, new InstanceCreated(item));
            var history = Project(events);
            Assert.True(history.HasErrors);
            Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-PACK-001");
        }
        var late = Beginning();
        var start = (GameStarted)late[0].Payload;
        Add(late, new PhaseChanged(start.Turn, "pfph", start.PhasingSide), "phase-changed");
        Add(late, new InstanceCreated(Horse()));
        Assert.Contains(Project(late).Diagnostics, item => item.Code == "UNIT-PACK-001");
    }

    [Fact]
    public void IdentityDoesNotAdmitGenericMovementOrDuplicateCounters()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse()));
        Add(events, new InstanceMoved("mule", new MapPosition(BoardLocation.Parse("bd01:E5:0"))), "instance-moved");
        Assert.Contains(Project(events).Diagnostics, item => item.Code == "UNIT-PACK-002");
        events.RemoveAt(events.Count - 1);
        Add(events, new InstanceCreated(Horse()));
        Assert.Contains(Project(events).Diagnostics, item => item.Code == "UNIT-STATE-006");
    }

    [Fact]
    public void SetupIdentityCannotLetOrdinaryPlaySkipHorseConsequences()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse()));
        var start = (GameStarted)events[0].Payload;
        Add(events, new PhaseChanged(start.Turn, "pfph", start.PhasingSide), "phase-changed");
        var history = Project(events);
        Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-PACK-003");
        Assert.Equal(2, history.States.Count);
        Assert.False(history.States[^1].SetupClosed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcealedAndHiddenMulesDoNotDiscloseTheirIdentityOrDesignation(bool hidden)
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse() with
        {
            Conditions = new Dictionary<string, ConditionState> { [hidden ? Conditions.Hidden : Conditions.Concealed] = ConditionState.True }
        }));
        var history = Project(events);
        Assert.False(history.HasErrors);
        var view = GameView.Of(history, 2, Perspective.Side("russian"));
        Assert.Empty(view.Entities);
        Assert.DoesNotContain(view.Events, item => item.Payload is InstanceCreated);
        Assert.DoesNotContain("mule", view.Locations.Keys);
        Assert.Equal(hidden ? 0 : 1, view.Sealed.Count);
        Assert.Single(GameView.Of(history, 2, Perspective.Side("german")).Entities);
    }

    [Fact]
    public void UninspectedHorseStacksExposeOnlyTheTopCounter()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse("top")));
        Add(events, new InstanceCreated(Horse("beneath")));
        var history = Project(events);
        var view = GameView.Of(history, 3, Perspective.Side("russian"));
        Assert.Equal("top", Assert.Single(view.Entities).Id);
        Assert.True(Assert.Single(view.Sealed).Uninspected);
        Assert.DoesNotContain(view.Events, item => item.Payload is InstanceCreated { Instance.Id: "beneath" });
    }

    [Fact]
    public void MalformedHorseJsonIsReportedAndOrdinaryHorsesDoNotRequireDesignation()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse() with
        {
            Horse = new(TurnHorseSize.Smc, false, null)
        }));
        Assert.False(Project(events).HasErrors);
        var json = GameEventWriter.Write(events[0].Scope, new("horse", true, events));
        var broken = json.Replace("\"size\": 3", "\"size\": \"bad\"", StringComparison.Ordinal);
        Assert.NotEqual(json, broken);
        Assert.True(GameEventReader.Read(Encoding.UTF8.GetBytes(broken)).HasErrors);
    }
}
