namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A9.72, PDF p. 65: eligibility for one selected SW repair attempt.</summary>
public static class ScenarioA1SupportWeaponRepairRules
{
    public static SupportWeaponReadiness Evaluate(SupportWeaponRepairFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase != "rph")
        {
            return SupportWeaponReadiness.Refused("Repair weapons during the Rally phase.", applicable: false);
        }

        if (facts.GoodOrder is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The holder's condition is not known.");
        }

        if (!facts.Active || facts.GoodOrder != true)
        {
            return SupportWeaponReadiness.Refused("The repairing unit must be in Good Order.");
        }

        if (!facts.Possessed || !facts.WeaponActive || !facts.Malfunctioned)
        {
            return SupportWeaponReadiness.Refused("Choose a malfunctioned weapon this unit possesses.");
        }

        if (facts.Gun)
        {
            return SupportWeaponReadiness.NotEvaluated("Gun repair requires the crew and hook-up workflow.");
        }

        if (facts.Captured is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The weapon's original side is not available.");
        }

        if (facts.Captured == true)
        {
            return SupportWeaponReadiness.Refused("A captured weapon cannot be repaired until its original side recaptures it.");
        }

        if (facts.RepairNumber is null)
        {
            return SupportWeaponReadiness.NotEvaluated("This weapon has no supported repair number.");
        }

        if (facts.OtherRallyAction)
        {
            return SupportWeaponReadiness.Refused("This unit has already taken a different Rally phase action.");
        }

        if (facts.AttemptedThisPlayerTurn)
        {
            return SupportWeaponReadiness.Refused("This weapon already had a repair attempt this Player Turn.");
        }

        if (facts.Capacity is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The repair capacity for this unit is not available.");
        }

        if (facts.AttemptsThisPhase >= facts.Capacity)
        {
            return SupportWeaponReadiness.Refused("This unit has used its repair attempts this Player Turn.");
        }

        return SupportWeaponReadiness.Allowed;
    }
}

/// <summary>Repair history is per weapon and per holder; the unit-level action marker is insufficient.</summary>
public sealed record SupportWeaponRepairFacts(string? Phase, bool Active, bool? GoodOrder, bool Possessed,
    bool WeaponActive, bool Malfunctioned, bool? Captured, int? RepairNumber, bool OtherRallyAction,
    bool AttemptedThisPlayerTurn, int AttemptsThisPhase, int? Capacity, bool Gun);
