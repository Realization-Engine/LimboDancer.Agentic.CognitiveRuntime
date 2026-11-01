using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>State facts shared by configuration proposals and current-record verification.</summary>
public static class SupportWeaponConfigurationRead
{
    public static SupportWeaponReadiness Evaluate(GameState state, string unitId, string weaponId, UnitCatalog? catalog)
    {
        ArgumentNullException.ThrowIfNull(state);
        var unit = state.Unit(unitId);
        var weapon = state.Find(weaponId) as EquipmentInstance;
        var definition = weapon?.Definition is { } reference && catalog?.Identity == reference.Catalog ? catalog.Definition(reference.Definition) : null;
        var size = definition?.Printed("front", "asl:size")?.Value?.Text;
        var typePermitted = ScenarioA1FireEligibility.Dismantlable(weapon?.Kind, definition?.Nationality, size,
            (int?)definition?.Printed("front", "asl:portage")?.Value?.Number);
        var supported = ScenarioA1SupportWeaponConfigurationRules.SupportsConfiguration(weapon?.Kind, definition?.Nationality, size);
        bool Condition(string name) => unit is not null && GameState.Condition(unit, name) == ConditionState.True;
        var active = unit?.Status == InstanceStatus.Active;
        bool? goodOrder = unit is not null && !SupportWeaponRecoveryRead.ConditionsKnown(unit) ? null
            : ScenarioA1Definitions.GoodOrderOf(active, Condition(Conditions.Broken), Condition(Conditions.Berserk),
            Condition(Conditions.Melee), Condition(Conditions.Captured), Condition(Conditions.Stunned), Condition(Conditions.Shocked));
        var history = UsesFor(state, unitId).ToArray();
        var uses = history.Where(use => use.Weapon is not null).Select(use => use.Weapon).Distinct(StringComparer.Ordinal).ToArray();
        var used = uses.Contains(weaponId, StringComparer.Ordinal) || state.SupportWeaponUses.Any(use => use.Weapon == weaponId)
            || weapon is not null && (GameState.Condition(weapon, Conditions.PrepFire) == ConditionState.True
                || GameState.Condition(weapon, Conditions.FinalFire) == ConditionState.True);
        return ScenarioA1SupportWeaponConfigurationRules.Evaluate(new SupportWeaponConfigurationFacts(state.Phase,
            unit?.Side == state.PhasingSide, active, goodOrder, Condition("asl:ti"),
            weapon is { Holding.Role: HoldingRole.Possessed } && weapon.Holding.Holder == unitId,
            weapon?.Status == InstanceStatus.Active, supported, used, uses.Length,
            unit is null ? null : ScenarioA1SupportWeaponUseRules.Capacity(unit.Kind), typePermitted,
            history.Any(use => use.Weapon is null), state.Aboard(unitId) is not null));
    }

    public static IEnumerable<SupportWeaponPhaseUse> UsesFor(GameState state, string unitId) => state.SupportWeaponPhaseUses
        .Where(use => use.Unit == unitId && ScenarioA1SupportWeaponUseRules.SameUseWindow(use.Phase, state.Phase));

    public static bool HasConfiguration(GameState state, string unitId) => UsesFor(state, unitId).Any(use => use.Configured);

    public static string? FireRefusal(GameState state, string unitId, IReadOnlyList<string> requested, bool inherent)
    {
        var history = UsesFor(state, unitId).ToArray();
        return ScenarioA1SupportWeaponUseRules.FireAfterConfiguration(state.Phase, state.Unit(unitId)?.Kind ?? string.Empty,
            [.. history.Where(use => use.Weapon is not null).Select(use => use.Weapon!).Distinct(StringComparer.Ordinal)],
            [.. history.Where(use => use.Configured && use.Weapon is not null).Select(use => use.Weapon!).Distinct(StringComparer.Ordinal)],
            history.Any(use => use.Weapon is null), requested, inherent,
            state.SupportWeaponPhaseUses.Any(use => use.Unit == unitId && use.Configured && use.Phase == "pfph"));
    }
}

/// <summary>A distinct weapon use, or inherent FP when Weapon is null; derived from recorded actions.</summary>
public sealed record SupportWeaponPhaseUse(string Unit, string? Weapon, string Phase, bool Configured = false);
