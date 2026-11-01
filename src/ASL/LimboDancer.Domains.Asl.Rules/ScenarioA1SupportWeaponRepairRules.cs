namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A9.72, PDF p. 65: eligibility for one selected SW repair attempt.</summary>
public static class ScenarioA1SupportWeaponRepairRules
{
    /// <summary>Preserve the established A9.72 reading: a known definition without a nationality is not captured. Missing definitions remain unknown.</summary>
    public static bool? CapturedForRepair(bool definitionsKnown, string? weaponNationality, string? unitNationality) =>
        definitionsKnown ? ScenarioA1FireEligibility.CapturedBy(weaponNationality, unitNationality) == true : null;

    public static SupportWeaponReadiness Evaluate(SupportWeaponRepairFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The phase is not known.");
        }
        if (ScenarioA1RallyRules.SwRepairPhaseBar(facts.Phase) is { } phaseBar)
        {
            return SupportWeaponReadiness.Refused(phaseBar, applicable: false);
        }

        if (facts.GoodOrder is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The holder's condition is not known.");
        }

        if (!facts.Active || !ScenarioA1RallyRules.SwRepairGoodOrderAsPlanned(!facts.GoodOrder.Value))
        {
            return SupportWeaponReadiness.Refused("The repairing unit must be in Good Order.");
        }

        if (!facts.WeaponActive || !ScenarioA1RallyRules.SwRepairWeaponAllowed(facts.Possessed, facts.Malfunctioned))
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

        if (ScenarioA1RallyRules.SwRepairCapturedBar("the weapon", facts.Captured.Value) is not null)
        {
            return SupportWeaponReadiness.Refused("A captured weapon cannot be repaired until its original side recaptures it.");
        }

        if (ScenarioA1RallyRules.SwRepairNumberBar("the weapon", facts.RepairNumber) is not null)
        {
            return SupportWeaponReadiness.NotEvaluated("This weapon has no supported repair number.");
        }

        if (ScenarioA1RallyRules.SwRepairRalliedBar("the holder", facts.RalliedThisPlayerTurn) is not null
            || ScenarioA1RallyRules.SwRepairActionBar("the holder", facts.OtherRallyAction) is not null)
        {
            return SupportWeaponReadiness.Refused("This unit has already taken a different Rally phase action.");
        }

        if (facts.AttemptedThisPlayerTurn)
        {
            return SupportWeaponReadiness.Refused("This weapon already had a repair attempt this Player Turn.");
        }

        if (facts.UnitKind is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The repair capacity for this unit is not available.");
        }

        if (facts.AttemptsThisPhase >= ScenarioA1SupportWeaponUseRules.Capacity(facts.UnitKind))
        {
            return SupportWeaponReadiness.Refused("This unit has used its repair attempts this Player Turn.");
        }

        return SupportWeaponReadiness.Allowed;
    }
}

/// <summary>Repair history is per weapon and per holder; the unit-level action marker is insufficient.</summary>
public sealed record SupportWeaponRepairFacts(string? Phase, bool Active, bool? GoodOrder, bool Possessed,
    bool WeaponActive, bool Malfunctioned, bool? Captured, int? RepairNumber, bool OtherRallyAction,
    bool AttemptedThisPlayerTurn, int AttemptsThisPhase, string? UnitKind, bool Gun, bool RalliedThisPlayerTurn = false);
