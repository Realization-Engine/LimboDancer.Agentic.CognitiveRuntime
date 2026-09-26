using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The fire record, the scenario month, and the fire phase markers (Fire in Live Play, unit step 18). Revision 9 of the
/// synthetic fixture is the German MPh; the record's facts and resolution are opaque here and checked by a verifier.
/// </summary>
public sealed class FireRecordTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly JsonElement Facts = JsonSerializer.SerializeToElement(new { phase = "PFPh" });
    private static readonly JsonElement Resolution = JsonSerializer.SerializeToElement(new { disposition = "resolved" });

    private sealed class Verifier(string? reason) : IFireRecordVerifier
    {
        public int Calls
        {
            get; private set;
        }

        public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls)
        {
            Calls++;
            Assert.True(rolls.ContainsKey(fire.Rolls["attack"]));
            return reason;
        }
    }

    private static List<GameEvent> With(params (string Id, string Type, EventPayload Payload)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(9)];
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

    private static (string, string, EventPayload) Roll(string id) =>
        ("r-" + id, "dice-rolled", new DiceRolled(id, "fire-ift", 2, 6, [3, 4], DiceRolled.SystemSource, "player"));

    private static (string, string, EventPayload) Fire(string id, string roll, string from = "bd01:D4:0", string to = "bd01:E4:0") =>
        (id, "fire-resolved", new FireResolved(["g1"], null, from, to, new Dictionary<string, string> { ["attack"] = roll }, Facts, Resolution));

    private static GameHistory Project(IReadOnlyList<GameEvent> events, IFireRecordVerifier? verifier) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains(), fire: verifier);

    [Fact]
    public void AVerifiedFireRecordIsKeptForThePhase()
    {
        var verifier = new Verifier(null);
        var history = Project(With(Roll("x-roll-1"), Fire("f1", "x-roll-1")), verifier);
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.Equal(1, verifier.Calls);
        Assert.Equal(new FireRecord("f1", "bd01:D4:0", "bd01:E4:0"), Assert.Single(history.Current!.FiresThisPhase));
    }

    [Fact]
    public void ReplayRefusesAFireRecordItCannotVerify()
    {
        Assert.Contains(Project(With(Roll("x-roll-1"), Fire("f1", "x-roll-1")), null).Diagnostics, item => item.Code == "UNIT-STATE-023");
        Assert.Contains(Project(With(Fire("f1", "x-roll-1")), new Verifier(null)).Diagnostics, item => item.Code == "UNIT-STATE-023");
        Assert.Contains(Project(With(Roll("x-roll-1"), Fire("f1", "x-roll-1")), new Verifier("the arithmetic differs")).Diagnostics,
            item => item.Code == "UNIT-STATE-024" && item.Message.Contains("arithmetic", StringComparison.Ordinal));
    }

    [Fact]
    public void ALocationFiresAtATargetOncePerPhase()
    {
        var twice = With(Roll("x-roll-1"), Fire("f1", "x-roll-1"), Roll("y-roll-1"), Fire("f2", "y-roll-1"));
        Assert.Contains(Project(twice, new Verifier(null)).Diagnostics, item => item.Code == "UNIT-STATE-024" && item.Message.Contains("A7.55", StringComparison.Ordinal));

        var elsewhere = With(Roll("x-roll-1"), Fire("f1", "x-roll-1"), Roll("y-roll-1"), Fire("f2", "y-roll-1", to: "bd01:F4:0"));
        Assert.False(Project(elsewhere, new Verifier(null)).HasErrors);
    }

    [Fact]
    public void TheFireRecordAndTheMonthRoundTrip()
    {
        var events = With(Roll("x-roll-1"), Fire("f1", "x-roll-1"));
        var started = (GameStarted)events[0].Payload;
        events[0] = events[0] with
        {
            Payload = started with
            {
                ScenarioMonth = 7
            }
        };
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("fire", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(7, ((GameStarted)read.Record!.Events[0].Payload).ScenarioMonth);
        var fire = Assert.IsType<FireResolved>(read.Record.Events[^1].Payload);
        Assert.Equal(("bd01:D4:0", "bd01:E4:0", "x-roll-1"), (fire.FirerLocation, fire.TargetLocation, fire.Rolls["attack"]));
        Assert.Equal("PFPh", fire.Facts.GetProperty("phase").GetString());
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));
        Assert.Equal(7, Project(read.Record.Events, new Verifier(null)).Current!.ScenarioMonth);

        var invalid = text.Replace("\"scenarioMonth\": 7", "\"scenarioMonth\": 13", StringComparison.Ordinal);
        Assert.NotEqual(text, invalid);
        Assert.True(GameEventReader.Read(Encoding.UTF8.GetBytes(invalid)).HasErrors);
    }

    [Fact]
    public void EachFireMarkerIsRemovedByThePhaseThatEndsIt()
    {
        var marked = new Dictionary<string, ConditionState>
        {
            [Conditions.PrepFire] = ConditionState.True,
            [Conditions.FinalFire] = ConditionState.True,
            [Conditions.Pinned] = ConditionState.True,
        };
        var events = With(("m1", "conditions-changed", new ConditionsChanged("g1", marked)));
        var state = Project(events, null).Current!;
        var phasing = state.PhasingSide;
        string[] order = ["dfph", "afph", "rtph", "aph", "ccph"];
        var expected = new[]
        {
            (Conditions.PrepFire, true, Conditions.FinalFire, true, Conditions.Pinned, true),     // entering the DFPh
            (Conditions.PrepFire, true, Conditions.FinalFire, false, Conditions.Pinned, true),    // the DFPh ended
            (Conditions.PrepFire, false, Conditions.FinalFire, false, Conditions.Pinned, true),   // the AFPh ended
        };
        for (var index = 0; index < order.Length; index++)
        {
            events = [.. events, events[^1] with
            {
                EventId = "p" + index, Revision = events[^1].Revision + 1, Type = "phase-changed",
                Payload = new PhaseChanged(state.Turn, order[index], phasing), Causes = [], Visibility = null
            }];
            var unit = Project(events, null).Current!.Unit("g1")!;
            if (index < expected.Length)
            {
                var (prep, hasPrep, final, hasFinal, pin, hasPin) = expected[index];
                Assert.Equal((hasPrep, hasFinal, hasPin), (unit.Conditions.ContainsKey(prep), unit.Conditions.ContainsKey(final), unit.Conditions.ContainsKey(pin)));
            }
        }

        // Leaving the CCPh for the other side's RPh removes pins (A3.8).
        var other = state.Sides.First(side => side.Id != phasing).Id;
        events = [.. events, events[^1] with
        {
            EventId = "p9", Revision = events[^1].Revision + 1, Type = "phase-changed", Payload = new PhaseChanged(state.Turn, "rph", other),
            Causes = [], Visibility = null
        }];
        Assert.False(Project(events, null).Current!.Unit("g1")!.Conditions.ContainsKey(Conditions.Pinned));
    }

    [Fact]
    public void AFireReportStandsOnceForAWithheldRecordAndRepeatsItsArithmetic()
    {
        var arithmetic = JsonSerializer.SerializeToElement(new
        {
            totalFirepower = 8,
            finalDr = 9,
            result = "none"
        });
        var resolution = JsonSerializer.SerializeToElement(new
        {
            disposition = "resolved",
            arithmetic
        });
        List<GameEvent> Events(IReadOnlyList<string>? recordVisibility, params (string Id, EventPayload Payload, IReadOnlyList<string>? Visibility)[] reports)
        {
            var events = With(Roll("x-roll-1"), ("f1", "fire-resolved",
                new FireResolved(["g1"], null, "bd01:D4:0", "bd01:E4:0", new Dictionary<string, string> { ["attack"] = "x-roll-1" }, Facts, resolution)));
            events[^1] = events[^1] with
            {
                Visibility = recordVisibility
            };
            foreach (var (id, payload, visibility) in reports)
            {
                events.Add(events[^1] with
                {
                    EventId = id,
                    Revision = events[^1].Revision + 1,
                    Type = "fire-reported",
                    Payload = payload,
                    Visibility = visibility
                });
            }

            return events;
        }

        var report = new FireReported("f1", "bd01:D4:0", "bd01:E4:0", arithmetic);
        var accepted = Events(["russian"], ("r1", report, null));
        Assert.False(Project(accepted, new Verifier(null)).HasErrors, string.Join(" ", Project(accepted, new Verifier(null)).Diagnostics));

        // The report round-trips.
        var text = GameEventWriter.Write(accepted[0].Scope, new GameRecord("fire", true, accepted));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        var roundTripped = Assert.IsType<FireReported>(read.Record!.Events[^1].Payload);
        Assert.Equal(("f1", 9), (roundTripped.Fire, roundTripped.Arithmetic.GetProperty("finalDr").GetInt32()));
        Assert.Equal(text, GameEventWriter.Write(accepted[0].Scope, read.Record));

        var different = report with
        {
            Arithmetic = JsonSerializer.SerializeToElement(new
            {
                totalFirepower = 8,
                finalDr = 10,
                result = "none"
            })
        };
        foreach (var refused in new[]
        {
            Events(null, ("r1", report, null)),                            // the record is public already
            Events(["russian"], ("r1", report, ["german"])),              // the report is not public
            Events(["russian"], ("r1", report, null), ("r2", report, null)), // twice
            Events(["russian"], ("r1", report with { Fire = "x" }, null)),  // no such record
            Events(["russian"], ("r1", report with { TargetLocation = "bd01:F4:0" }, null)),
            Events(["russian"], ("r1", different, null)),
        })
        {
            Assert.Contains(Project(refused, new Verifier(null)).Diagnostics, item => item.Code == "UNIT-STATE-025");
        }
    }
}
