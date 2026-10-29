using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    /// <summary>Gathers a selected mortar and candidate after the caller authorizes both identities for its viewer.</summary>
    public SpotterDesignationFacts? ReadSpotterDesignation(GameState state, string gunId, string spotterId, MortarSpotterAssignmentRead? assignments = null)
    {
        if (state.Equipment.FirstOrDefault(item => item.Id == gunId) is not { } gun)
        {
            return null;
        }
        var definition = gun.Definition?.Definition;
        var mortar = ScenarioA1MortarCalculator.IsMortar(gun.Kind,
            definition is null ? null : OrdnanceReference.Value.Guns.GetValueOrDefault(definition)?.GunType);
        return MortarSpotterFacts.Designation(state, gunId, spotterId, vocabulary, mortar,
            SpotterRange(state, state.Location(gunId)?.Location, state.Location(spotterId)?.Location), assignments);
    }

    public SpotterReplacementFacts? ReadSpotterReplacement(GameState state, string gunId, string spotterId) =>
        ReadSpotterDesignation(state, gunId, spotterId) is { } facts ? MortarSpotterFacts.Replacement(state, facts) : null;

    public static SpotterReplacementFacts ReadSpotterReplacement(GameState state, SpotterDesignationFacts facts) =>
        MortarSpotterFacts.Replacement(state, facts);

    private GamePlan PlanSpotterDesignation(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing,
        string attemptId, long expected, string label)
    {
        if (!Text(arguments, "gunId", out var gun) || !Text(arguments, "spotter", out var spotter)
            || Replay(existing).Current is not { } state || state.Unit(spotter)?.Side is not { } owner)
        {
            return Refused(scope, label, expected, "play.spotter: identify the mortar and Spotter in an existing game");
        }
        var facts = ReadSpotterDesignation(state, gun, spotter);
        if (facts is null)
        {
            return Refused(scope, label, expected, "play.spotter: identify the mortar's holder and candidate");
        }
        var replacement = facts.Timing.HasDesignation == true;
        if (replacement)
        {
            var verdict = ScenarioA1SpotterCalculator.Replacement(MortarSpotterFacts.Replacement(state, facts));
            if (verdict.State != SpotterReplacementState.MayReplace || !verdict.EstablishesAssignment)
            {
                return Refused(scope, label, expected, "play.spotter: " + (verdict.Refusal ?? verdict.Reason));
            }
        }
        else if (ScenarioA1SpotterCalculator.DesignationRefusal(facts) is { } refusal)
        {
            return Refused(scope, label, expected, "play.spotter: " + refusal);
        }
        var privateRecord = state.Find(gun) is { } weapon && (Is(weapon, Conditions.Hidden) || Is(weapon, Conditions.Concealed))
            || state.Unit(spotter) is { } unit && (Is(unit, Conditions.Hidden) || Is(unit, Conditions.Concealed))
            || state.Find(gun) is EquipmentInstance { Holding: { } held } && state.Unit(held.Holder) is { } holder
                && (Is(holder, Conditions.Hidden) || Is(holder, Conditions.Concealed));
        var proposed = Event(scope, attemptId, 1, expected, "mortar-spotter-designated", new MortarSpotterDesignated(gun, spotter),
            "asl.spotter.v2", privateRecord ? [owner] : null);
        var replay = Replay([.. existing, proposed]);
        if (replay.HasErrors)
        {
            return Refused(scope, label, expected, "play.spotter: " + string.Join(" ", replay.Diagnostics.Where(item => item.Severity == Units.UnitDiagnosticSeverity.Error).Select(item => item.Message)));
        }
        return new(GamePlanStatus.Ready, scope, label, expected, [proposed],
            [replacement ? $"Replace the Spotter for {gun} with {spotter}; no fire marker or SW use is recorded."
                : $"Designate {spotter} for {gun}; record phase SW use and its fire marker. This does not authorize a shot."])
        {
            FirstEventId = EventId(attemptId, 1)
        };
    }
}
