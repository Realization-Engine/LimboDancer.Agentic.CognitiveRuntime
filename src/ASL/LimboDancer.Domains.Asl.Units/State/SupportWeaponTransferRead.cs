using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Shared state facts for voluntary transfer proposals and current transfer records.</summary>
public static class SupportWeaponTransferRead
{
    public static string? Refusal(GameState state, string fromId, string weaponId, string toId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var giver = state.Unit(fromId);
        var taker = state.Unit(toId);
        var weapon = state.Find(weaponId) as EquipmentInstance;
        static bool Is(UnitInstance? unit, string condition) => unit is not null && GameState.Condition(unit, condition) == ConditionState.True;
        static bool? GoodOrder(UnitInstance? unit) => unit is not null && !SupportWeaponRecoveryRead.ConditionsKnown(unit) ? null
            : ScenarioA1Definitions.GoodOrderOf(unit?.Status == InstanceStatus.Active,
            Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), Is(unit, Conditions.Stunned), Is(unit, Conditions.Shocked));
        bool OtherAction(string id) => (state.RallyPhaseActions.Contains(id) && !state.SupportWeaponTransferParticipants.Contains(id))
            || state.RallyAttemptsThisPlayerTurn.Contains(id) || state.RepairsThisPhase.Contains(id);
        return ScenarioA1SupportWeaponPossessionRules.TransferRefusal(new SupportWeaponTransferFacts(
            state.Phase, fromId, toId, weaponId, fromId != toId, GoodOrder(giver), GoodOrder(taker), Is(giver, Conditions.Pinned), Is(taker, Conditions.Pinned),
            giver is not null && taker is not null && giver.Side == taker.Side,
            state.Location(fromId)?.Location is { } at && state.Location(toId)?.Location == at,
            giver is not null && taker is not null && giver.Kind != "asl:vehicle" && taker.Kind != "asl:vehicle",
            state.Aboard(fromId), state.Aboard(toId), Is(taker, Conditions.Unarmed),
            weapon is { Status: InstanceStatus.Active, Holding.Role: HoldingRole.Possessed } && weapon.Holding.Holder == fromId,
            state.RecoveredThisPhase.Contains(weaponId), OtherAction(fromId), OtherAction(toId), giver?.Side == state.PhasingSide,
            state.Advances.Count > 0,
            state.Aboard(fromId) is { } giverVehicle && Is(state.Unit(giverVehicle), Conditions.Motion)
                || state.Aboard(toId) is { } recipientVehicle && Is(state.Unit(recipientVehicle), Conditions.Motion),
            SupportWeaponRecoveryRead.KnownWirePresent(state, fromId) || SupportWeaponRecoveryRead.KnownWirePresent(state, toId)));
    }
}
