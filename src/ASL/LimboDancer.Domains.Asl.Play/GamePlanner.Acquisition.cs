using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.ScenarioA1;
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
    private static List<string> AcquiredUnits(OrdnanceShot facts, IReadOnlyList<FireUnitEffect> effects, string attemptId)
    {
        var units = new List<string>();
        foreach (var target in facts.Hit?.Targets ?? [])
        {
            if (target.Dummy == true)
            {
                continue;
            }

            var effect = effects.FirstOrDefault(item => item.UnitId == target.UnitId);
            var known = !(target.Concealed == true || target.Hidden == true) || effect?.ConcealmentLost == true;
            if (known && effect?.Eliminated != true)
            {
                units.Add(effect is not null && effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{target.UnitId}" : target.UnitId!);
            }
        }

        return units;
    }

    /// <summary>
    /// The Acquisition events a commit calls for after it moved acquired units (ruling R5.13): a unit that entered a Location out of its Gun's
    /// LOS is no longer acquired, and when none is left the counter stays in the last Location in LOS; when the acquired units are in more than
    /// one Location as one of them ends its MPh or APh, the Gun's side chooses which Location keeps it.
    /// </summary>
    private List<(string Type, EventPayload Payload)> AcquisitionFollowUp(GameState before, GameState after)
    {
        var events = new List<(string, EventPayload)>();
        foreach (var acquisition in after.Acquisitions.Where(item => item.Units.Count > 0))
        {
            if (after.Find(acquisition.Gun) is not EquipmentInstance { Position: MapPosition gunAt } gun
                || before.Acquisitions.FirstOrDefault(item => item.Gun == acquisition.Gun) is not { } previous)
            {
                continue;
            }

            var kept = new List<string>();
            BoardLocation? last = null;
            foreach (var id in acquisition.Units)
            {
                var was = before.Location(id)?.Location;
                var now = after.Location(id)?.Location;
                if (now is null || was is null || now == was || Los(after, gunAt.Location, now) is { Status: LosStatus.Clear })
                {
                    kept.Add(id);
                }
                else
                {
                    last = was;
                }
            }

            var locations = kept.Select(id => after.Location(id)!.Location).Distinct().ToArray();
            if (kept.Count < acquisition.Units.Count)
            {
                events.Add(("acquisition-changed", new AcquisitionChanged(gun.Id, locations is [{ } only] ? only : kept.Count == 0 ? last ?? previous.Location : acquisition.Location,
                    kept)));
            }

            // C6.51: the choice is due once a split unit has finished its MPh, APh, or CCPh withdrawal.
            var ended = kept.Any(id => (after.Unit(id) is { MovementEnded: true } && before.Unit(id)?.MovementEnded != true)
                || (after.Phase != "mph" && before.Location(id)?.Location != after.Location(id)?.Location));
            if (locations.Length > 1 && ended && after.Choice is null)
            {
                events.Add(("choice-pending", new ChoicePending($"acquisition:{gun.Id}", ChoicePending.Acquisition, gun.Side ?? after.PhasingSide,
                    [.. locations.Select(item => item.ToString()).Order(StringComparer.Ordinal)], JsonSerializer.SerializeToElement(new JsonObject { ["gun"] = gun.Id }))));
            }
        }

        return events;
    }

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
