using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The DEFENDER's choice between a weapon's Multiple ROF and Residual FP (A8.23, p. 60; pass 45, task 45.2): "A weapon does not automatically leave
/// Residual FP if it retains a Multiple ROF ... The decision to forfeit possible remaining First Fire capability in order to leave Residual FP must
/// be made immediately after resolving its attack." The attack leaves the other firers' Residual FP and the weapon keeps its ROF; this action,
/// open only until anything else happens, gives the ROF up. Rules decides (<see cref="ScenarioA1FireFollowUps"/>); this file reads the record.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The ROF that may be given up now: the attack's record, its Location, the Residual FP that would stand there, and the weapons.</summary>
    public sealed record RofForfeit(string Fire, BoardLocation At, int Fp, IReadOnlyList<string> Weapons);

    /// <summary>The choice open now (A8.23), for the action and the Play page; null when none is.</summary>
    public static RofForfeit? RateOfFireForfeit(GameState state, IReadOnlyList<GameEvent> existing)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(existing);
        var index = -1;
        for (var at = existing.Count - 1; at >= 0 && index < 0; at--)
        {
            if (existing[at].Payload is FireResolved)
            {
                index = at;
            }
        }

        if (index < 0 || existing[index].Payload is not FireResolved record || !BoardLocation.TryParse(record.TargetLocation, out var target))
        {
            return null;
        }

        var fire = existing[index].EventId;
        var forfeited = record.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.ValueKind == JsonValueKind.Object
            && arithmetic.TryGetProperty("residualFpIfForfeited", out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : (int?)null;
        string[] kept = record.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
            ? [.. effects.EnumerateArray().Where(item => item.TryGetProperty("rateOfFireRetained", out var retained) && retained.ValueKind == JsonValueKind.True)
                .Select(item => item.GetProperty("equipmentId").GetString()!)]
            : [];

        // Nothing has happened since the attack but what it caused; and each weapon is as the attack left it.
        var latest = existing.Skip(index + 1).All(later => later.Causes.Contains(fire, StringComparer.Ordinal));
        string[] still = [.. kept.Where(id => state.Find(id) is EquipmentInstance { Status: InstanceStatus.Active } weapon && !Is(weapon, Conditions.FirstFire)
            && !Is(weapon, Conditions.FinalFire) && !Is(weapon, Conditions.Malfunctioned))];
        return ScenarioA1FireFollowUps.RofForfeitOpen(state.Phase == "mph", latest, forfeited, still.Length) ? new RofForfeit(fire, target!, forfeited!.Value, still) : null;
    }

    private GamePlan PlanForfeitRof(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (RateOfFireForfeit(state, existing) is not { } forfeit)
        {
            return Refused(scope, label, expected, ScenarioA1FireFollowUps.RofForfeitClosedText());
        }

        var events = new List<GameEvent>();
        foreach (var weapon in forfeit.Weapons)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(weapon, ConditionChanges([(UnitCondition.FirstFire, true)])), ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
        }

        // A8.21: only a larger counter replaces one in the Location.
        if (!state.ResidualFire.Any(item => item.Location == forfeit.At && item.Fp >= forfeit.Fp))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "residual-fp-placed", new ResidualFirePlaced(forfeit.Fire, forfeit.At, forfeit.Fp),
                ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1FireFollowUps.RofForfeitText(forfeit.Weapons, forfeit.Fp, forfeit.At.ToString())]);
    }
}
