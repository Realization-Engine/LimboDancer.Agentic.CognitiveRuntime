namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A4.43 and A11.21, PDF pp. 50 and 73: equipment choices belong to a declared withdrawal, including voluntary drops.</summary>
public static class ScenarioA1WithdrawalEquipmentRules
{
    public static bool MayPrepareMelee(string? phase, bool roundStarted, bool active, bool melee, bool pinned, bool berserk, bool disrupted, bool captured) =>
        phase == "ccph" && !roundStarted && active && melee && !pinned && !berserk && !disrupted && !captured;

    public static bool MayChooseDrop(string? phase, bool withdrawalEligible, bool unitActive, bool weaponActive, bool possessed) =>
        phase == "ccph" && withdrawalEligible && unitActive && weaponActive && possessed;

    public static WithdrawalEquipmentLoad Load(bool smc, bool wounded, bool cx, IReadOnlyList<int>? retained)
    {
        var capacity = ScenarioA1MovementCalculator.PortageCapacity(smc, wounded, cx);
        var readiness = retained is null ? SupportWeaponReadiness.NotEvaluated("A carried weapon's PP is not available.")
            : ScenarioA1MovementCalculator.Laden(smc, wounded, cx, retained)
                ? SupportWeaponReadiness.Refused("play.cc-withdrawal-portage: Leave enough equipment to withdraw.") : SupportWeaponReadiness.Allowed;
        return new WithdrawalEquipmentLoad(retained?.Sum(), capacity, readiness);
    }
}

public sealed record WithdrawalEquipmentLoad(int? Carried, int Capacity, SupportWeaponReadiness Readiness);
