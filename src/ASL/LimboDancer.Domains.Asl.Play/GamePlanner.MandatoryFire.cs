using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The attack the DEFENDER owes when an Infantry MMC enters its Location in the MPh (A8.312, p. 61; pass 40, task 40.5). Rules decides who owes
/// it and what is refused meanwhile (<see cref="ScenarioA1MandatoryFire"/>); this file reads the state and answers Rules' question whether a unit
/// has an attack to make, by asking the fire plan.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The Mandatory FG of the moving stack's latest step: the Location entered, the units that owe the attack, and whether they may fire as one group.</summary>
    public sealed record MandatoryFireOwed(BoardLocation At, IReadOnlyList<string> Firers, bool Group);

    /// <summary>The attack owed now (A8.312), for the planner's bars and the Play page; null when none is.</summary>
    public MandatoryFireOwed? MandatoryFire(GameState state, IReadOnlyList<GameEvent> existing)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(existing);
        if (existing.Count == 0 || state.Movement is not { Vehicle: false } movement)
        {
            return null;
        }

        var at = movement.Location;
        var mmcEntered = movement.Movers.Select(state.Unit).Any(unit => unit is { Status: InstanceStatus.Active } && vocabulary.IsA(unit.Kind, "asl:mmc")
            && !LiveFire.IsVehicle(unit) && state.Location(unit.Id)?.Location == at);
        var fired = existing.Select(item => item.Payload).OfType<FireResolved>()
            .Where(record => record.MovementStep == movement.Step && record.TargetLocation == at.ToString() && record.FirerLocation == at.ToString())
            .SelectMany(record => record.Firers).ToHashSet(StringComparer.Ordinal);
        var defenders = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide)
            .Select(unit => new MandatoryDefenderFacts(unit.Id, vocabulary.IsA(unit.Kind, "asl:personnel") && !LiveFire.IsVehicle(unit) && state.Aboard(unit.Id) is null,
                vocabulary.IsA(unit.Kind, "asl:smc"), HasInherentFp(unit), Possessed(state, unit.Id).Length, Is(unit, Conditions.Unarmed), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire), fired.Contains(unit.Id)))
            .ToArray();

        // Rules asks, of a unit that would otherwise owe the attack, whether the fire rules give it one now; the fire plan answers.
        var owed = ScenarioA1MandatoryFire.Owed(state.Phase == "mph", movement.WindowOpen, movement.From is not null, mmcEntered, defenders,
            id => MandatoryPossible(state, existing, [id], at));
        return owed.Count == 0 ? null : new MandatoryFireOwed(at, owed, owed.Count == 1 || MandatoryPossible(state, existing, owed, at));
    }

    /// <summary>Whether the plainest attack by these units at their own Location is one the fire plan allows now. It draws no dice and writes nothing.</summary>
    private bool MandatoryPossible(GameState state, IReadOnlyList<GameEvent> existing, IReadOnlyList<string> firers, BoardLocation at) => firers.Count > 0
        && PlanFireOf(existing[0].Scope, JsonSerializer.SerializeToElement(MandatoryArguments(state, firers, at)), existing, "mandatory-fire-read", existing.Count, "A8.312", "system")
            .Status == GamePlanStatus.Ready;

    private bool HasInherentFp(UnitInstance unit) => unit.Definition is { } reference
        && catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:firepower")?.Value?.Number is > 0;

    /// <summary>
    /// The plainest attack by the units named at their own Location; Rules says how many of its MG each fires (<see cref="ScenarioA1MandatoryFire.WeaponsFired"/>).
    /// The DEFENDER may add a MG and a leader's direction with the fire panel.
    /// </summary>
    private Dictionary<string, object> MandatoryArguments(GameState state, IReadOnlyList<string> firers, BoardLocation at)
    {
        var weapons = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var alone = new List<string>();
        foreach (var id in firers)
        {
            if (state.Unit(id) is not { } unit)
            {
                continue;
            }

            var usable = Possessed(state, id);
            var inherent = HasInherentFp(unit);
            var count = ScenarioA1MandatoryFire.WeaponsFired(vocabulary.IsA(unit.Kind, "asl:squad"), inherent, Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire), usable.Length);
            if (count > 0)
            {
                weapons[id] = usable[..count];
            }

            if (!inherent && count > 0)
            {
                alone.Add(id);
            }
        }

        return new Dictionary<string, object>
        {
            ["firers"] = firers,
            ["target"] = at.ToString(),
            ["weapons"] = weapons,
            ["withoutInherent"] = alone,
        };
    }

    /// <summary>The arguments of the plainest Mandatory FG attack, or of one unit's part of it, for the Play page's buttons.</summary>
    public Dictionary<string, object> MandatoryFireArguments(GameState state, MandatoryFireOwed owed, string? only = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(owed);
        return MandatoryArguments(state, only is null ? owed.Firers : [only], owed.At);
    }

    /// <summary>
    /// The fire action, with the Mandatory FG's bar before it (A8.312). The bar is on the DEFENDER's proposal alone: a proposal by the phasing side
    /// goes to the fire plan as before, so no refusal tells the ATTACKER which of the DEFENDER's units owe the attack.
    /// </summary>
    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is { } state && Strings(arguments, "firers").ToArray() is { Length: > 0 } firers
            && firers.Select(state.Unit).Any(unit => unit is not null && unit.Side != state.PhasingSide) && MandatoryFire(state, existing) is { } owed)
        {
            var target = Text(arguments, "target", out var named) ? named : string.Empty;

            // Ruling R40.17: where the units form no one group, each that could have fired with those named must be named too.
            string[] couldJoin = owed.Group || target != owed.At.ToString() ? []
                : [.. owed.Firers.Where(id => !firers.Contains(id, StringComparer.Ordinal)
                    && MandatoryPossible(state, existing, [.. firers.Where(owed.Firers.Contains), id], owed.At))];
            if (ScenarioA1MandatoryFire.FireBar(owed.Firers, owed.Group, firers, target, owed.At.ToString(), couldJoin) is { } bar)
            {
                return Refused(scope, label, expected, bar);
            }
        }

        return PlanFireOf(scope, arguments, existing, attemptId, expected, label, actor);
    }
}
