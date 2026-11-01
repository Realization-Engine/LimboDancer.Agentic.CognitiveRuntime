namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A9.8, PDF p. 65: assembly and dismantling are weapon uses, independent of attack-target selection.</summary>
public static class ScenarioA1SupportWeaponConfigurationRules
{
    /// <summary>The current condition-based representation lacks the exceptional Japanese HMG and medium-mortar portage profiles.</summary>
    public static bool SupportsConfiguration(string? kind, string? nationality, string? size) =>
        kind is "asl:mg" or "asl:light-mortar" && !(nationality == "japanese" && size == "heavy");

    public static string FireCounter(string? phase) => phase == "pfph" ? "asl:prep-fire" : "asl:final-fire";

    /// <summary>The established ordinary dm representation; exceptional profiles are outside SupportsConfiguration.</summary>
    public static int Portage(int printed, bool dismantled) => dismantled ? (printed + 1) / 2 : printed;

    public static SupportWeaponReadiness Evaluate(SupportWeaponConfigurationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!ScenarioA1SupportWeaponRules.DismantlePhase(facts.Phase, facts.Phasing))
        {
            return SupportWeaponReadiness.Refused("Configure weapons during your Prep Fire or Defensive Fire phase.", applicable: false);
        }

        if (facts.Aboard)
        {
            return SupportWeaponReadiness.NotEvaluated("Weapon configuration aboard a vehicle is not supported.");
        }

        if (facts.GoodOrder is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The holder's condition cannot be evaluated.");
        }
        if (!facts.Active || facts.GoodOrder != true || facts.TemporarilyImmobilized)
        {
            return SupportWeaponReadiness.Refused("Choose a Good Order holder able to use its weapon.");
        }

        if (!facts.Possessed || !facts.WeaponActive)
        {
            return SupportWeaponReadiness.Refused("Choose a weapon this unit possesses.");
        }

        if (facts.UnitKind is null || facts.WeaponKind is null || facts.WeaponNationality is null || facts.WeaponSize is null || facts.PrintedPortage is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The unit or weapon definition is not known.");
        }

        if (!ScenarioA1FireEligibility.Dismantlable(facts.WeaponKind, facts.WeaponNationality == "none" ? null : facts.WeaponNationality,
            facts.WeaponSize == "none" ? null : facts.WeaponSize, facts.PrintedPortage))
        {
            return SupportWeaponReadiness.Refused("play.dismantle-weapon: This weapon type cannot be dismantled (A9.8).", applicable: false);
        }

        if (!SupportsConfiguration(facts.WeaponKind, facts.WeaponNationality, facts.WeaponSize))
        {
            return SupportWeaponReadiness.NotEvaluated("This weapon has no supported dismantled configuration.");
        }

        if (facts.WeaponUsed)
        {
            return SupportWeaponReadiness.Refused("play.dismantle-weapon: This weapon has already been used this phase (A9.8).");
        }

        if (facts.UsesThisPhase >= ScenarioA1SupportWeaponUseRules.Capacity(facts.UnitKind) - (facts.InherentFireUsed ? 1 : 0))
        {
            return SupportWeaponReadiness.Refused("This unit has used its weapon allowance this phase.");
        }

        return SupportWeaponReadiness.Allowed;
    }
}

/// <summary>Use history includes earlier configuration even when no ordinary fire counter is placed.</summary>
public sealed record SupportWeaponConfigurationFacts(string? Phase, bool Phasing, bool Active, bool? GoodOrder,
    bool TemporarilyImmobilized, bool Possessed, bool WeaponActive, string? WeaponKind,
    bool WeaponUsed, int UsesThisPhase, string? UnitKind, string? WeaponNationality, string? WeaponSize, int? PrintedPortage,
    bool InherentFireUsed = false, bool Aboard = false);
