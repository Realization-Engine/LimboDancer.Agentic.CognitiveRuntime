using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Shared state facts for the planner and verification of current repair records.</summary>
public static class SupportWeaponRepairRead
{
    public static SupportWeaponReadiness Evaluate(GameState state, string unitId, string weaponId, bool? captured, int? repairNumber)
    {
        ArgumentNullException.ThrowIfNull(state);
        var unit = state.Unit(unitId);
        var weapon = state.Find(weaponId) as EquipmentInstance;
        bool Condition(string condition) => unit is not null && GameState.Condition(unit, condition) == ConditionState.True;
        var active = unit?.Status == InstanceStatus.Active;
        bool? goodOrder = unit is not null && !SupportWeaponRecoveryRead.ConditionsKnown(unit) ? null
            : ScenarioA1Definitions.GoodOrderOf(active, Condition(Conditions.Broken), Condition(Conditions.Berserk),
            Condition(Conditions.Melee), Condition(Conditions.Captured), Condition(Conditions.Stunned), Condition(Conditions.Shocked));
        var attempts = 0;
        var attempted = false;
        foreach (var use in state.SupportWeaponRepairs)
        {
            if (use.PlayerTurn != state.PlayerTurnOrdinal)
            {
                continue;
            }

            attempted |= use.Weapon == weaponId;
            if (use.Unit == unitId)
            {
                attempts++;
            }
        }

        return ScenarioA1SupportWeaponRepairRules.Evaluate(new SupportWeaponRepairFacts(state.Phase, active, goodOrder,
            weapon is { Holding.Role: HoldingRole.Possessed } && weapon.Holding.Holder == unitId,
            weapon?.Status == InstanceStatus.Active, weapon is not null && GameState.Condition(weapon, Conditions.Malfunctioned) == ConditionState.True,
            captured, repairNumber, state.RallyPhaseActions.Contains(unitId) || state.SupportWeaponTransferParticipants.Contains(unitId),
            attempted, attempts, unit?.Kind, weapon?.Kind == "asl:gun", state.RallyAttemptsThisPlayerTurn.Contains(unitId)));
    }
}
