using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The entry-attempted and entry-forced-back events (Occupied and Concealed Entry Design, section 8): their record
/// format, the replay checks, movement ended, reveal detection, and staleness. Revision 9 of the synthetic fixture is
/// the German MPh, with g1 in D4 and the concealed Russian squad r1 in E4.
/// </summary>
public sealed class EntryEventTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly BoardLocation D4 = BoardLocation.Parse("bd01:D4:0");
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");

    private static List<GameEvent> Base(int revision = 9) => [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(revision)];

    private static GameEvent Next(List<GameEvent> events, string id, string type, EventPayload payload, params string[] causes) =>
        events[^1] with
        {
            EventId = id,
            Revision = events[^1].Revision + 1,
            Type = type,
            Payload = payload,
            Causes = causes,
            Visibility = null
        };

    private static List<GameEvent> With(List<GameEvent> events, params (string Id, string Type, EventPayload Payload, string[] Causes)[] added)
    {
        foreach (var (id, type, payload, causes) in added)
        {
            events.Add(Next(events, id, type, payload, causes));
        }

        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());

    private static List<GameEvent> ForcedBack() => With(Base(),
        ("a1", "entry-attempted", new EntryAttempted("g1", E4, 2), []),
        ("a2", "conditions-changed", new ConditionsChanged("r1", new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False }), ["a1"]),
        ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1"]));

    [Fact]
    public void AForcedBackKeepsTheUnitWhereItWasSpendsTheMfThereAndEndsItsMovement()
    {
        var history = Project(ForcedBack());
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        var attempted = history.At(10)!;
        Assert.Equal((D4, 0, false), (attempted.Location("g1")!.Location, attempted.Unit("g1")!.MfSpent, attempted.Unit("g1")!.MovementEnded));

        var state = history.Current!;
        Assert.Equal((D4, 2, true), (state.Location("g1")!.Location, state.Unit("g1")!.MfSpent, state.Unit("g1")!.MovementEnded));
    }

    [Fact]
    public void MovementEndsForThePhaseOnly()
    {
        var events = ForcedBack();
        var refused = Project([.. events, Next(events, "a4", "instance-moved", new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd01:D5:0")), 1))]);
        Assert.Contains(refused.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-018");

        var next = Project([.. events, Next(events, "a4", "phase-changed", new PhaseChanged(1, "dfph", "german"))]).Current!;
        Assert.Equal((0, false), (next.Unit("g1")!.MfSpent, next.Unit("g1")!.MovementEnded));
    }

    [Theory]
    [InlineData("no-attempt")]
    [InlineData("no-cause")]
    [InlineData("other-mf")]
    [InlineData("other-location")]
    [InlineData("twice")]
    [InlineData("other-phase")]
    [InlineData("not-phasing")]
    [InlineData("not-a-unit")]
    [InlineData("after-phase-change")]
    public void ReplayRefusesAnAttemptOrForcedBackThatBreaksItsRules(string change)
    {
        var attempt = ("a1", "entry-attempted", (EventPayload)new EntryAttempted("g1", E4, 2), Array.Empty<string>());
        var events = change switch
        {
            "no-attempt" => With(Base(), ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), [])),
            "no-cause" => With(Base(), attempt, ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), [])),
            "other-mf" => With(Base(), attempt, ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 3, false), ["a1"])),
            "other-location" => With(Base(), attempt, ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", BoardLocation.Parse("bd01:D5:0"), 2, false), ["a1"])),
            "twice" => With(ForcedBack(), ("a4", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1"])),
            "other-phase" => With(Base(8), attempt),
            "not-phasing" => With(Base(), ("a1", "entry-attempted", new EntryAttempted("r1", D4, 2), [])),
            "not-a-unit" => With(Base(), ("a1", "entry-attempted", new EntryAttempted("f1", D4, 2), [])),
            _ => With(Base(), attempt, ("p1", "phase-changed", new PhaseChanged(1, "dfph", "german"), []),
                ("a3", "entry-forced-back", new EntryForcedBack("g1", "a1", D4, 2, false), ["a1"])),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-018");
    }

    [Fact]
    public void TheEventsRoundTripThroughTheGameRecord()
    {
        var events = ForcedBack();
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("round trip", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(events.Select(item => item.Payload).TakeLast(3), read.Record!.Events.Select(item => item.Payload).TakeLast(3), new PayloadComparer());
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
    }

    [Fact]
    public void AHiddenUnitPlacedBeneathAQuestionMarkIsNotRevealed()
    {
        // r2 is hidden in the foxhole f1 in E5; placing it beneath a "?" is not a reveal, losing concealment is.
        var placed = With(Base(), ("p1", "conditions-changed",
            new ConditionsChanged("r2", new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }), []));
        var request = new CaseRequest(placed[0].Scope, "test", "g1", BoardLocation.Parse("bd01:E5:0"), 10, Perspective.Adjudicator);
        var before = Reader(Project(placed)).Read(request).Snapshot!;
        Assert.Empty(before.Reveals);

        var revealed = With(placed, ("p2", "conditions-changed", new ConditionsChanged("r2", new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False }), []));
        var after = Reader(Project(revealed)).Read(request with
        {
            ExpectedRevision = 11
        }).Snapshot!;
        Assert.Equal("p2", Assert.Single(after.Reveals).EventId);
    }

    [Fact]
    public void AnAttemptAndAForcedBackMakeAnEarlierCaseReadStale()
    {
        var history = Project(ForcedBack());
        var request = new CaseRequest(history.Events[0].Scope, "test", "g1", E4, 9, Perspective.Adjudicator);
        var snapshot = Reader(Project(Base())).Read(request).Snapshot!;
        Assert.True(CaseReader.Affects(snapshot, history.Events[9]));
        Assert.True(CaseReader.Affects(snapshot, history.Events[11]));
        Assert.True(CaseReader.IsStale(snapshot, history));
    }

    private static CaseReader Reader(GameHistory history) =>
        new(new HistoryGameSource([history]), new InMemoryBoardCatalog([CaseReadTests.Board()]), UnitsTestData.Asl.Value, [Catalog.Value]);

    /// <summary>Payload records hold dictionaries, so they are compared by their serialized form.</summary>
    private sealed class PayloadComparer : IEqualityComparer<EventPayload>
    {
        public bool Equals(EventPayload? x, EventPayload? y) =>
            System.Text.Json.JsonSerializer.Serialize(x, x?.GetType() ?? typeof(object)) == System.Text.Json.JsonSerializer.Serialize(y, y?.GetType() ?? typeof(object));

        public int GetHashCode(EventPayload obj) => 0;
    }
}
