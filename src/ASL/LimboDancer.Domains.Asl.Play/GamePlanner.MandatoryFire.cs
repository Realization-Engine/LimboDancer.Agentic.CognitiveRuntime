using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The attack the DEFENDER owes when an Infantry MMC enters its Location in the MPh (A8.312, p. 61; pass 40, task 40.5). Rules decides who owes
/// it and what is refused meanwhile (<see cref="ScenarioA1MandatoryFire"/>); this file reads the state and asks the fire plan whether each unit has
/// an attack to make.
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
        if (state.Phase != "mph" || existing.Count == 0 || state.Movement is not { WindowOpen: true, Vehicle: false, From: not null } movement)
        {
            return null;
        }

        var at = movement.Location;
        var mmcEntered = movement.Movers.Select(state.Unit).Any(unit => unit is { Status: InstanceStatus.Active } && vocabulary.IsA(unit.Kind, "asl:mmc")
            && !LiveFire.IsVehicle(unit) && state.Location(unit.Id)?.Location == at);
        if (!mmcEntered)
        {
            return null;
        }

        var scope = existing[0].Scope;
        var expected = (long)existing.Count;
        var fired = existing.Select(item => item.Payload).OfType<FireResolved>()
            .Where(record => record.MovementStep == movement.Step && record.TargetLocation == at.ToString() && record.FirerLocation == at.ToString())
            .SelectMany(record => record.Firers).ToHashSet(StringComparer.Ordinal);
        var there = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide).ToArray();

        // A unit the fire rules give no attack now is not held to one, so the DEFENDER's pass is never refused for an attack that cannot be made.
        bool Possible(IReadOnlyList<string> firers) => firers.Count > 0
            && PlanFireOf(scope, JsonSerializer.SerializeToElement(MandatoryArguments(state, firers, at)), existing, "mandatory-fire-read", expected, "A8.312", "system").Status == GamePlanStatus.Ready;

        var defenders = there.Select(unit =>
        {
            var definition = unit.Definition is { } reference ? catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition) : null;
            var marked = Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire);
            var attacked = fired.Contains(unit.Id);
            return new MandatoryDefenderFacts(unit.Id, vocabulary.IsA(unit.Kind, "asl:personnel") && !LiveFire.IsVehicle(unit) && state.Aboard(unit.Id) is null,
                vocabulary.IsA(unit.Kind, "asl:smc"), definition?.Printed("front", "asl:firepower")?.Value?.Number is > 0, UsableMgs(state, unit.Id).Count, Is(unit, Conditions.Unarmed),
                Is(unit, Conditions.Broken), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), marked, attacked, !attacked && Possible([unit.Id]));
        }).ToArray();

        var owed = ScenarioA1MandatoryFire.Owed(true, true, true, true, defenders);
        return owed.Count == 0 ? null : new MandatoryFireOwed(at, owed, owed.Count == 1 || Possible(owed));
    }

    /// <summary>The MG a unit possesses that are not malfunctioned.</summary>
    private static List<string> UsableMgs(GameState state, string unitId) =>
        [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Kind == "asl:mg" && item.Holding is { Role: HoldingRole.Possessed } held && held.Holder == unitId
            && !Is(item, Conditions.Malfunctioned)).Select(item => item.Id)];

    /// <summary>
    /// The plainest attack by the units named at their own Location: each fires its inherent FP, and a unit that has none fires the first MG it
    /// possesses. The DEFENDER may add its MG and a leader's direction with the fire panel.
    /// </summary>
    private Dictionary<string, object> MandatoryArguments(GameState state, IReadOnlyList<string> firers, BoardLocation at)
    {
        var weapons = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var alone = new List<string>();
        foreach (var id in firers)
        {
            var unit = state.Unit(id);
            var definition = unit?.Definition is { } reference ? catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition) : null;
            if (definition?.Printed("front", "asl:firepower")?.Value?.Number is not > 0 && UsableMgs(state, id) is [var mg, ..])
            {
                weapons[id] = [mg];
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

    /// <summary>The fire action, with the Mandatory FG's bar before it (A8.312).</summary>
    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is { } state && MandatoryFire(state, existing) is { } owed
            && ScenarioA1MandatoryFire.FireBar(owed.Firers, owed.Group, [.. Strings(arguments, "firers")], Text(arguments, "target", out var target) ? target : string.Empty,
                owed.At.ToString()) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        return PlanFireOf(scope, arguments, existing, attemptId, expected, label, actor);
    }
}
