using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A Gun's Acquisition follows its target units (C6.5, C6.51; ruling R5.13): the shot places it on the Known units it fired at; the planner
/// moves it back to the last Location in the Gun's LOS when they leave it, and asks the Gun's side which Location keeps it when the acquired
/// units end their move apart.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The Known units a shot leaves acquired (C6.5, C6.51): the target units that were Known or lost "?" to the shot and survived it, under
    /// the ids their Reduction or Replacement gives them; empty when there are none, and the Acquisition then stays on the Location.
    /// </summary>
    private static List<string> AcquiredUnits(OrdnanceShot facts, IReadOnlyList<FireUnitEffect> effects, string attemptId) =>
        ScenarioA1ResultTables.AcquiredUnits(facts, effects, attemptId);

    /// <summary>
    /// The Acquisition events a commit calls for after it moved acquired units (ruling R5.13): a unit that entered a Location out of its Gun's
    /// LOS is no longer acquired, and when none is left the counter stays in the last Location in LOS; when the acquired units are in more than
    /// one Location as one of them ends its MPh or APh, the Gun's side chooses which Location keeps it. Rules decides it (pass 32.c); Locations
    /// cross as their texts and come back as the Locations they were.
    /// </summary>
    private List<(string Type, EventPayload Payload)> AcquisitionFollowUp(GameState before, GameState after)
    {
        var locations = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
        string? Text(BoardLocation? location)
        {
            if (location is null)
            {
                return null;
            }

            var text = location.ToString();
            locations[text] = location;
            return text;
        }

        var verdicts = ScenarioA1FireFollowUps.AcquisitionFollowUp(after.Acquisitions.Select(acquisition =>
        {
            // A Gun where it lies; a tank, whose MA holds the Acquisition, where it stands (pass 35, task 35.11).
            var gun = after.Find(acquisition.Gun);
            var gunAt = gun is EquipmentInstance { Position: MapPosition lying } ? lying.Location : gun is UnitInstance tank ? after.Location(tank.Id)?.Location : null;
            var previous = before.Acquisitions.FirstOrDefault(item => item.Gun == acquisition.Gun);
            return new AcquisitionFacts(acquisition.Gun, gun?.Side, gunAt is not null, previous is not null, Text(previous?.Location), Text(acquisition.Location)!,
                [.. acquisition.Units.Select(id => new AcquiredUnitFacts(id, Text(before.Location(id)?.Location), Text(after.Location(id)?.Location),
                    () => Los(after, gunAt!, after.Location(id)!.Location) is { Status: LosStatus.Clear },
                    before.Unit(id)?.MovementEnded == true, after.Unit(id) is { MovementEnded: true }))]);
        }), after.Phase, after.Choice is not null);

        var events = new List<(string, EventPayload)>();
        foreach (var verdict in verdicts)
        {
            if (verdict.Changed is { } changed)
            {
                events.Add(("acquisition-changed", new AcquisitionChanged(verdict.Gun, locations[changed.Location], changed.Units)));
            }

            if (verdict.ChoiceLocations is { } options)
            {
                events.Add(("choice-pending", new ChoicePending($"acquisition:{verdict.Gun}", ChoicePending.Acquisition, after.Find(verdict.Gun)?.Side ?? after.PhasingSide,
                    options, JsonSerializer.SerializeToElement(new JsonObject { ["gun"] = verdict.Gun }))));
            }
        }

        return events;
    }

    /// <summary>
    /// The kinds among <paramref name="kinds"/> whose rules are not built (pass 35, task 35.15), each once, in order, by the vocabulary's own
    /// label ("Wire", "Fortified Building Location"); Rules says which.
    /// </summary>
    private IReadOnlyList<string> UnbuiltKinds(IEnumerable<string?> kinds) =>
        [.. kinds.OfType<string>().Where(kind => ScenarioA1SequenceCalculator.UnbuiltCounter(kind, vocabulary.IsA(kind, "asl:fortification")))
            .Select(kind => vocabulary.TryGetKind(kind, out var known) ? known.Label : kind).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>A plan with the Acquisition events its events call for appended (ruling R5.13); the plan itself when it moves no acquired unit.</summary>
    private GamePlan WithAcquisitions(GamePlan plan, GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected)
    {
        if (plan.Status != GamePlanStatus.Ready || Replay(existing).Current is not { } before || !before.Acquisitions.Any(item => item.Units.Count > 0))
        {
            return plan;
        }

        IReadOnlyList<GameEvent> Append(IReadOnlyList<GameEvent> events)
        {
            if (events.Count == 0 || Replay([.. existing, .. events]).Current is not { } after)
            {
                return events;
            }

            var cause = events[^1].EventId;
            var list = events.ToList();
            foreach (var (type, payload) in AcquisitionFollowUp(before, after))
            {
                list.Add(Event(scope, attemptId, list.Count + 1, expected, type, payload, ScenarioA1OrdnancePackage.Identity.ToString(), null, [cause]));
            }

            return list;
        }

        return plan.Roll is { } roll
            ? plan with
            {
                Roll = roll with
                {
                    Build = draw => Append(roll.Build(draw))
                }
            }
            : plan with
            {
                Events = Append(plan.Events)
            };
    }
}
