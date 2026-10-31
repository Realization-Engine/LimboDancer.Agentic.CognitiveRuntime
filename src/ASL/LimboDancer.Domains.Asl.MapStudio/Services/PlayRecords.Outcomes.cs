using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

public sealed partial class PlayRecords
{
    private IReadOnlyList<ActionOutcomeView>? outcomes;
    private (long First, long Last, IReadOnlyList<ActionOutcomeView>? Views) outcomeRange;

    /// <summary>Newest attempts first; each attempt retains the sequence of its disclosed entries and every visible roll.</summary>
    public IReadOnlyList<ActionOutcomeView> Outcomes => outcomes ??= OutcomesIn(1, Last);

    public ActionOutcomeView? LatestOutcome
    {
        get
        {
            var significant = Lines.LastOrDefault(line => !line.Minor);
            var key = significant.EventId is { } id ? AttemptOf(id) : null;
            return key is null ? (Outcomes.Count == 0 ? null : Outcomes[0]) : Outcomes.FirstOrDefault(item => item.Key == key);
        }
    }

    public IReadOnlyList<ActionOutcomeView> OutcomesIn(long first, long last)
    {
        if (outcomeRange.Views is { } cached && outcomeRange.First == first && outcomeRange.Last == last)
        {
            return cached;
        }
        var visible = events.Where(item => item.Revision >= first && item.Revision <= last && item.IsVisibleTo(viewer)).ToArray();
        var subjects = events.Where(item => item.IsVisibleTo(viewer)).Select(item => item.Payload).OfType<RandomSelection>()
            .GroupBy(item => item.Roll).ToDictionary(group => group.Key, group => group.First().Subjects, StringComparer.Ordinal);
        var rolls = visible.Where(item => item.Payload is DiceRolled).GroupBy(item => AttemptOf(item.EventId))
            .ToDictionary(group => group.Key, group => RowsOf(group.Select(item => ((DiceRolled)item.Payload,
                subjects.GetValueOrDefault(((DiceRolled)item.Payload).Roll))), roll => DisplayText.RollPurpose(roll.Purpose)), StringComparer.Ordinal);
        var fires = Fire.ToDictionary(item => item.EventId, StringComparer.Ordinal);
        var kinds = RallyAndRepair.GroupBy(item => item.EventId).ToDictionary(group => group.Key, group => group.First().Kind, StringComparer.Ordinal);
        foreach (var item in Ordnance)
        {
            kinds[item.EventId] = "ordnance";
        }
        foreach (var item in CloseCombat)
        {
            kinds[item.EventId] = "cc";
        }
        foreach (var item in Night)
        {
            kinds[item.EventId] = "night";
        }
        foreach (var item in Snipers)
        {
            kinds[item.EventId] = "sniper";
        }
        var visibleById = visible.ToDictionary(item => item.EventId, StringComparer.Ordinal);
        var primaryRollAttempts = visible.Where(item => item.Payload is DiceRolled { Purpose: "fire-ift" or "ordnance-canister" })
            .Select(item => AttemptOf(item.EventId)).ToHashSet(StringComparer.Ordinal);
        var groups = Lines.Where(line => line.Revision >= first && line.Revision <= last).GroupBy(line => AttemptOf(line.EventId));
        var partial = Lines.Where(line => line.Revision < first || line.Revision > last)
            .Select(line => AttemptOf(line.EventId)).ToHashSet(StringComparer.Ordinal);
        var result = new List<(long Revision, ActionOutcomeView View)>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var entries = group.Select(line => new ActionOutcomeEntry(line.EventId, line.Text,
                fires.GetValueOrDefault(line.EventId), CombatTable(line.EventId))
            {
                Kind = kinds.GetValueOrDefault(line.EventId, fires.ContainsKey(line.EventId) ? "fire" : "action"),
                // A public fire report has no roll-ID link. Keep its arithmetic, and show the identified raw roll once below it.
                ShowPrimaryDice = visibleById.ContainsKey(line.EventId) || !primaryRollAttempts.Contains(group.Key)
            }).ToArray();
            var lead = group.FirstOrDefault(line => !line.Minor);
            var firstEntry = entries[0];
            var displayed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry.Fire is { NoKnownTarget: false } view && visibleById.GetValueOrDefault(entry.EventId)?.Payload is FireResolved fire)
                {
                    void Mark(string key)
                    {
                        if (fire.Rolls.TryGetValue(key, out var id))
                        {
                            displayed.Add(id);
                        }
                    }
                    if (!(view.NamedVehicle is { Count: > 0 } && view.Effects is { Count: 0 } && view.Vehicles is not { Count: > 0 }))
                    {
                        Mark("attack");
                    }
                    var shownEffects = (view.Effects ?? []).Concat((view.Vehicles ?? [])
                        .SelectMany(vehicle => vehicle.PrcAttacks ?? []).SelectMany(attack => attack.Effects));
                    foreach (var effect in shownEffects)
                    {
                        if (effect.Checks.Count > 0)
                        {
                            Mark("checks:" + effect.UnitId);
                            Mark("leaderLoss:" + effect.UnitId);
                        }
                        if (effect.RandomSelectionDr is not null)
                        {
                            Mark("randomSelection:" + effect.UnitId);
                        }
                        if (effect.HeatOfBattle is not null)
                        {
                            Mark("heatOfBattle:" + effect.UnitId);
                        }
                        if (effect.SecondHeatOfBattle is not null)
                        {
                            Mark("heatOfBattle:" + effect.UnitId + ":2");
                        }
                    }
                    foreach (var vehicle in view.Vehicles ?? [])
                    {
                        if (vehicle.UnlikelyKillDr is not null)
                        {
                            Mark("unlikelyKill:" + vehicle.VehicleId);
                        }
                        if (vehicle.RandomSelectionDr is not null)
                        {
                            Mark("randomSelection:" + vehicle.VehicleId);
                        }
                        if (vehicle.CrewCheck is not null || vehicle.TaskCheck is not null)
                        {
                            Mark("crewCheck:" + vehicle.VehicleId);
                        }
                        if (vehicle.CrewSurvival is not null)
                        {
                            Mark("vehicleSurvival:" + vehicle.VehicleId);
                        }
                        foreach (var survivor in vehicle.PrcSurvival ?? [])
                        {
                            Mark("prcSurvival:" + survivor.UnitId);
                        }
                    }
                }
                if (entry.Combat is { } combat && visibleById.GetValueOrDefault(entry.EventId)?.Payload is CloseCombatResolved resolved)
                {
                    foreach (var pair in resolved.Rolls.Where(pair => pair.Key.StartsWith("attack:", StringComparison.Ordinal)
                        || pair.Key.StartsWith("leaderCreation:", StringComparison.Ordinal)))
                    {
                        if (combat.Attacks.Count > 0)
                        {
                            displayed.Add(pair.Value);
                        }
                    }
                }
            }
            var remainingRolls = (rolls.GetValueOrDefault(group.Key) ?? []).Where(roll => !displayed.Contains(roll.Roll)).ToArray();
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var ids = visibleById.GetValueOrDefault(entry.EventId)?.Payload switch
                {
                    OrdnanceFired record => record.Rolls.Values,
                    RallyAttempted record => record.Rolls.Values,
                    AmbushRolled record => record.Rolls.Values,
                    MoraleCheckResolved record => record.Rolls.Values,
                    FireResolved record => record.Rolls.Values,
                    CloseCombatResolved record => record.Rolls.Values,
                    _ => Enumerable.Empty<string>(),
                };
                var associated = ids.ToHashSet(StringComparer.Ordinal);
                var attached = remainingRolls.Where(roll => !displayed.Contains(roll.Roll) && associated.Contains(roll.Roll)).ToArray();
                entries[index] = entry with
                {
                    Rolls = attached
                };
                displayed.UnionWith(attached.Select(roll => roll.Roll));
            }
            var context = (WhenOf(firstEntry.EventId) ?? "Before play") + (partial.Contains(group.Key) ? " · Part of this action in the selected interval" : "");
            var summary = lead.Text ?? firstEntry.Text;
            var combats = entries.Select(entry => entry.Combat).OfType<CloseCombatTable>().ToArray();
            if (combats.Length > 0)
            {
                summary = string.Join("; ", combats.Select(combat => $"CC in {combat.Location}, {combat.Round}: "
                    + (combat.Attacks.Count == 0 ? "no attacks" : string.Join("; ", combat.Attacks.Select(attack => $"{attack.Side}: {attack.Outcome}")))));
            }
            result.Add((group.Max(line => line.Revision), new(group.Key, firstEntry.EventId, context,
                summary, entries, (rolls.GetValueOrDefault(group.Key) ?? []).Where(roll => !displayed.Contains(roll.Roll)).ToArray())));
            used.Add(group.Key);
        }
        foreach (var group in visible.Where(item => item.Payload is DiceRolled).GroupBy(item => AttemptOf(item.EventId)))
        {
            if (used.Contains(group.Key))
            {
                continue;
            }
            var item = group.First();
            result.Add((group.Max(value => value.Revision), new(group.Key, item.EventId, WhenOf(item.EventId) ?? "Before play",
                "Dice recorded", [], rolls[group.Key])));
        }
        var views = result.OrderByDescending(item => item.Revision).Select(item => item.View).ToArray();
        outcomeRange = (first, last, views);
        return views;
    }
}
