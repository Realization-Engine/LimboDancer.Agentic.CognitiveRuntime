using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Shared possession, activity, knowledge and movement facts for current recovery.</summary>
public static class SupportWeaponRecoveryRead
{
    public static bool KnownWirePresent(GameState state, string unitId) => state.Unit(unitId) is { } unit
        && state.Location(unitId)?.Location is { } at && state.Entities.Any(item => item.Kind == "asl:wire" && item.Status == InstanceStatus.Active
            && state.Location(item.Id)?.Location == at && (item.Side == unit.Side || GameState.Condition(item, Conditions.Hidden) != ConditionState.True));

    public static bool ConditionsKnown(UnitInstance unit) => GameState.Condition(unit, Conditions.Broken) != ConditionState.Unknown
        && !unit.Conditions.Values.Contains(ConditionState.Unknown);

    public static int? PortageOf(EquipmentInstance weapon, IReadOnlyList<UnitCatalog> catalogs) =>
        weapon.Definition is { } reference && catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition)
            ?.Printed("front", "asl:portage")?.Value?.Number is { } printed
            ? ScenarioA1SupportWeaponConfigurationRules.Portage(printed, GameState.Condition(weapon, Conditions.Dismantled) == ConditionState.True) : null;

    public static SupportWeaponRecoveryFacts Facts(GameState state, string unitId, string weaponId, IReadOnlyList<UnitCatalog> catalogs,
        UnitVocabulary vocabulary, string? terrain)
    {
        var unit = state.Unit(unitId);
        var weapon = state.Find(weaponId) as EquipmentInstance;
        var holder = weapon?.Holding is { Role: HoldingRole.Possessed } holding ? state.Unit(holding.Holder) : null;
        var at = state.Location(unitId)?.Location;
        bool Is(IGameObject? item, string condition) => item is not null && GameState.Condition(item, condition) == ConditionState.True;
        var active = unit?.Status == InstanceStatus.Active;
        var goodOrder = ScenarioA1Definitions.GoodOrderOf(active, Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk),
            Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), Is(unit, Conditions.Stunned), Is(unit, Conditions.Shocked));
        var smc = unit is not null && vocabulary.IsA(unit.Kind, "asl:smc");
        var held = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } heldBy && heldBy.Holder == unitId).ToArray();
        var portage = held.Concat(state.PortagedThisPhase.Where(item => item.Unit == unitId && !held.Any(now => now.Id == item.Weapon))
            .Select(item => state.Find(item.Weapon)).OfType<EquipmentInstance>()).Select(item => PortageOf(item, catalogs)).ToArray();
        var load = ScenarioA1Wounds.Portage(portage.Any(value => value is null) ? null : [.. portage.Select(value => value!.Value)],
            state.Movement?.Carried?.Any(carry => carry.Carrier == unitId) == true);
        var definition = unit?.Definition is { } named ? catalogs.FirstOrDefault(item => item.Identity == named.Catalog)?.Definition(named.Definition) : null;
        var allotment = unit is null ? null : ScenarioA1MovementCalculator.MfAllotment(
            new MfAllotmentFacts(unit.Kind == UnitKinds.Dummy, definition?.Class, smc, Is(unit, Conditions.Wounded), Is(unit, Conditions.Berserk)),
            unit.DoubleTimeMf, Is(unit, Conditions.Cx), 0, 0, () => Experience.MoveAllowance(state, unit, catalogs, vocabulary), () => load);
        var recoveredActors = state.RecoveryAttempts.Select(item => item.Split('|')[0]).ToHashSet(StringComparer.Ordinal);
        var window = ScenarioA1SupportWeaponPossessionRules.RecoveryWindowOpen(state.RallyAttemptsThisPlayerTurn.Count > 0,
            state.RepairsThisPhase.Count > 0, state.RallyPhaseActions.Any(id => !recoveredActors.Contains(id)), state.SupportWeaponTransferParticipants.Count > 0);
        var enemy = unit is not null && state.Units.Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side
            && state.Location(other.Id)?.Location == at
            && ScenarioA1SupportWeaponPossessionRules.RecoveryEnemyBlocks(
                ScenarioA1PrisonerCalculator.KnownEnemy(other.Kind == UnitKinds.Dummy, Is(other, Conditions.Concealed), Is(other, Conditions.Hidden), Is(other, Conditions.Captured)),
                Is(other, Conditions.Unarmed), vocabulary.IsA(other.Kind, "asl:vehicle"), Is(other, Conditions.Abandoned)));
        return new SupportWeaponRecoveryFacts(state.Phase, unit?.Side == state.PhasingSide, active, goodOrder,
            Is(unit, Conditions.Pinned), unit is not null && vocabulary.IsA(unit.Kind, "asl:personnel") && !vocabulary.IsA(unit.Kind, "asl:vehicle"),
            Is(unit, Conditions.Unarmed), state.Movement is { Bypass.Count: > 0 } bypass && bypass.Members.Contains(unitId),
            weapon?.Status == InstanceStatus.Active, at is not null && state.Location(weaponId)?.Location == at,
            weapon?.Holding is not null, holder is not null && holder.Side == unit?.Side, Is(holder, Conditions.Broken), smc, enemy,
            state.RecoveryAttempts.Contains(unitId + "|" + weaponId), state.RallyPhaseActions.Contains(unitId)
                || state.RallyAttemptsThisPlayerTurn.Contains(unitId) || state.RepairsThisPhase.Contains(unitId),
            unit?.MovementEnded == true, Is(unit, "asl:ti"),
            allotment is { } mf && unit is not null ? 2 * (mf - unit.MfSpent) - (unit.HalfMfSpent ? 1 : 0) : null,
            window, state.Movement is { WindowOpen: true }, state.Movement is { } move && !move.Members.Contains(unitId),
            terrain, state.Aboard(unitId) is not null,
            unit is not null && ConditionsKnown(unit)
                && (holder is null || GameState.Condition(holder, Conditions.Broken) != ConditionState.Unknown), KnownWirePresent(state, unitId));
    }
}
