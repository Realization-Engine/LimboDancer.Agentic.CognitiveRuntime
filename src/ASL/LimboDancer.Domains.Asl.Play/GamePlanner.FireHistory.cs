using System.Collections.Immutable;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    private TurnFireHistorySnapshot ReadFireHistory(GameHistory history, long revision, Perspective perspective)
    {
        var state = history.At(revision)!;
        var currentView = ViewOf(history, revision, perspective);
        var lastBoundary = history.Events.Where(item => item.Revision <= revision && item.Payload is PhaseChanged)
            .Select(item => item.Revision).DefaultIfEmpty(0).Max();
        var shots = new List<TurnFireRecord>();
        foreach (var item in history.Events.Where(item => item.Revision <= revision && item.IsVisibleTo(perspective)))
        {
            ImmutableArray<string> firers;
            string? weapon = null;
            string? spotter = null;
            bool? rof = null;
            TurnFireKind kind;
            switch (item.Payload)
            {
                case FireResolved fire:
                    kind = TurnFireKind.Fire;
                    firers = [.. fire.Firers];
                    break;
                case OrdnanceFired ordnance:
                    kind = TurnFireKind.Ordnance;
                    firers = [ordnance.Crew];
                    weapon = ordnance.Gun;
                    rof = ordnance.RateOfFireKept;
                    if (ordnance.Facts.TryGetProperty("spotter", out var support) && support.ValueKind == JsonValueKind.Object
                        && support.TryGetProperty("unitId", out var id))
                    {
                        spotter = id.GetString();
                    }
                    break;
                default:
                    continue;
            }
            var before = history.At(item.Revision - 1);
            if (before is null)
            {
                continue;
            }
            var atShot = ViewOf(history, item.Revision - 1, perspective);
            bool OwnUnit(string id) => atShot.Units.Any(unit => unit.Id == id && unit.Side == perspective.Name);
            if (!perspective.IsAdjudicator && (firers.IsEmpty || firers.Any(id => !OwnUnit(id))
                || (spotter is not null && !OwnUnit(spotter))
                || (weapon is not null && !atShot.Equipment.Any(gun => gun.Id == weapon && gun.Side == perspective.Name)
                    && !OwnUnit(weapon))))
            {
                continue;
            }
            shots.Add(new(kind, before.Turn, before.PhasingSide, before.Phase, firers, weapon, spotter, rof,
                item.Revision > lastBoundary));
        }
        var observations = shots.Where(item => item.Weapon is not null && item.Spotter is not null)
            .Select(item => (Weapon: item.Weapon!, Spotter: item.Spotter!)).Distinct().Select(pair =>
            {
                var currentSpotter = pair.Spotter;
                var successionVisible = true;
                foreach (var succession in state.MortarSpotterSuccessors.Where(item => item.Gun == pair.Weapon))
                {
                    if (succession.Predecessor != currentSpotter)
                    {
                        continue;
                    }
                    successionVisible &= history.Events.Single(item => item.EventId == succession.EventId).IsVisibleTo(perspective);
                    currentSpotter = succession.Successor;
                }
                var unit = perspective.IsAdjudicator ? state.Unit(currentSpotter) : currentView.Units.FirstOrDefault(unit => unit.Id == currentSpotter);
                var weapon = perspective.IsAdjudicator ? state.Find(pair.Weapon) : currentView.Equipment.FirstOrDefault(gun => gun.Id == pair.Weapon);
                // The command compares the Spotter with the current firer, not the weapon's printed nationality.
                var holder = (weapon as EquipmentInstance)?.Holding?.Holder;
                var crew = perspective.IsAdjudicator ? state.Unit(holder ?? string.Empty)
                    : currentView.Units.FirstOrDefault(candidate => candidate.Id == holder);
                var loss = state.MortarSpotterLosses.LastOrDefault(item => item.Gun == pair.Weapon && item.Spotter == currentSpotter);
                var lossVisible = loss is null || history.Events.Single(item => item.EventId == loss.EventId).IsVisibleTo(perspective);
                var known = (unit is not null || loss is not null) && crew is not null && lossVisible && successionVisible
                    && (unit?.Status != InstanceStatus.Consumed || loss is not null);
                return new TurnSpotterRead(pair.Weapon, pair.Spotter, known, unit?.Status == InstanceStatus.Active,
                    unit?.Side == crew?.Side, unit is not null && GameState.Condition(unit, Conditions.Broken) == ConditionState.True,
                    unit is not null && GameState.Condition(unit, Conditions.Captured) == ConditionState.True,
                    shots.Any(item => item.InCurrentPhase && item.Weapon == pair.Weapon && (item.Spotter == pair.Spotter || item.Spotter == currentSpotter)), loss is not null && lossVisible, known && currentSpotter != pair.Spotter ? currentSpotter : null);
            }).ToImmutableArray();
        return TurnFireHistory.Compose([.. shots], observations);
    }
}
