using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The ordnance record of unit step 24 (UNIT-STATE-033): its round trip, the verifier replay needs, and the projector's checks that a
/// Gun fires in a fire phase, manned by its crew, and again only on a kept Multiple ROF, turning when the record names a facing and
/// keeping its Acquisition. The synthetic fixture gains a manned Gun in D4; the record is checked by a stub verifier.
/// </summary>
public sealed class OrdnanceRecordTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);
    private static readonly JsonElement Facts = JsonSerializer.SerializeToElement(new { phase = "PFPh" });
    private static readonly BoardLocation D4 = BoardLocation.Parse("bd01:D4:0");
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");

    private sealed class Verifier : IOrdnanceRecordVerifier
    {
        public string? Verify(GameState state, OrdnanceFired fired, IReadOnlyDictionary<string, DiceRolled> rolls) => null;
    }

    private static List<GameEvent> With(int take, params (string Id, string Type, EventPayload Payload)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(take)];
        foreach (var (id, type, payload) in added)
        {
            events.Add(events[^1] with
            {
                EventId = id,
                Revision = events[^1].Revision + 1,
                Type = type,
                Payload = payload,
                Causes = [],
                Visibility = null
            });
        }

        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events, IOrdnanceRecordVerifier? verifier) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains(), ordnance: verifier);

    // A German Gun in D4, manned by g1, facing east.
    private static (string, string, EventPayload) Gun() => ("gun", "instance-created", new InstanceCreated(new NewInstance("de-gun", "asl:gun",
        "attacker-inf-gun", "german", new MapPosition(D4, Facing: UnitFacing.East), new Holding("g1", HoldingRole.Manned),
        new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.False })));

    private static (string, string, EventPayload) Phase(string id, string phase) => (id, "phase-changed", new PhaseChanged(1, phase, "german"));

    private static (string, string, EventPayload) Fired(string id, bool kept, UnitFacing? facing = null) =>
        (id, "ordnance-fired", new OrdnanceFired("de-gun", "g1", E4, facing, kept, -1, E4, new Dictionary<string, string>(), Facts, Facts));

    [Fact]
    public void AGunFiresInAFirePhaseAgainOnlyOnAKeptRofAndTurnsAsTheRecordSays()
    {
        var events = With(8, Gun(), Phase("p1", "pfph"), Fired("f1", kept: true, UnitFacing.SouthEast));
        var history = Project(events, new Verifier());
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        var state = history.Current!;
        Assert.Equal(UnitFacing.SouthEast, ((MapPosition)state.Find("de-gun")!.Position).Facing);
        Assert.Equal(new OrdnanceShotRecord("de-gun", 1, true), state.OrdnanceShots.Single());
        Assert.Equal(new GunAcquisition("de-gun", E4, -1), state.Acquisitions.Single());

        // A second shot on the kept ROF; a third after it lost the ROF is refused (C2.24); the shots reset at the phase change.
        Assert.False(Project(Next(events, Fired("f2", kept: false)), new Verifier()).HasErrors);
        var third = Next(events, Fired("f2", kept: false), Fired("f3", kept: false));
        Assert.Contains(Project(third, new Verifier()).Diagnostics, item => item.Code == "UNIT-STATE-033");
        Assert.Empty(Project(Next(events, Phase("p2", "mph")), new Verifier()).Current!.OrdnanceShots);

        // Outside a fire phase, or without the verifier, the record is refused.
        Assert.Contains(Project(With(8, Gun(), Phase("p1", "rph"), Fired("f1", true)), new Verifier()).Diagnostics, item => item.Code == "UNIT-STATE-033");
        Assert.Contains(Project(events, null).Diagnostics, item => item.Code == "UNIT-STATE-023");
    }

    [Fact]
    public void TheOrdnanceRecordRoundTrips()
    {
        var events = With(8, Gun(), Phase("p1", "pfph"), Fired("f1", kept: true, UnitFacing.SouthWest));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("live", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record!));
        var fired = Assert.IsType<OrdnanceFired>(read.Record!.Events[^1].Payload);
        Assert.Equal((UnitFacing.SouthWest, true, -1, E4), (fired.Facing!.Value, fired.RateOfFireKept, fired.Acquisition, fired.Acquired!));
    }

    private static List<GameEvent> Next(List<GameEvent> events, params (string Id, string Type, EventPayload Payload)[] added)
    {
        List<GameEvent> next = [.. events];
        foreach (var (id, type, payload) in added)
        {
            next.Add(next[^1] with
            {
                EventId = id,
                Revision = next[^1].Revision + 1,
                Type = type,
                Payload = payload,
                Causes = [],
                Visibility = null
            });
        }

        return next;
    }
}
