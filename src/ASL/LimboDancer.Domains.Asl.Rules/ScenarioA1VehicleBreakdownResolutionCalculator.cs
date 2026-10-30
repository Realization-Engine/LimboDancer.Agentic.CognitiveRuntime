namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Opt-in composition for D3.7/D3.71 (PDF pp. 201-202), A.11 (p. 43), A21.11/.13 (p. 88), C5.62 (p. 173),
/// E3.741 (p. 231) and D7.17 (p. 207). Old resolvers do not call this entry point without supplemental evidence.
/// </summary>
public static class ScenarioA1VehicleBreakdownResolutionCalculator
{
    /// <summary>D3.7 (PDF p. 201): a Disabled armament cannot fire again.</summary>
    public static string? WeaponUseRefusal(bool disabled) => disabled
        ? "play.vehicle-weapon-disabled: the selected vehicle armament is permanently disabled (D3.7)" : null;

    /// <summary>D3.7/D5.341: apply the weapon's Disabled result and, when ordered, the complete Recall transition.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> DisabledChanges(string weapon, bool recalled) => recalled
        ? [(ScenarioA1RallyRules.VehicleWeaponDisabled(weapon), true), (UnitCondition.Recalled, true), (UnitCondition.Stunned, false),
            (UnitCondition.StunRecovery, false), (UnitCondition.ButtonedUp, true), (UnitCondition.Motion, false)]
        : [(ScenarioA1RallyRules.VehicleWeaponDisabled(weapon), true)];

    /// <summary>C8.9 (PDF p. 179): a vehicle's Low Ammo or breakdown result counts as firing despite absent special ammunition.</summary>
    public static bool NeverFired(string? ammunitionUse, bool malfunctioned, bool eliminated, VehicleBreakdownResolved? current) =>
        ScenarioA1OrdnanceProjection.NeverFired(ammunitionUse, malfunctioned, eliminated || current?.CountsAsFiredOnDepletionFailure == true);

    /// <summary>D3.7/D5.341 (PDF pp. 201, 204): explicit printed capacity facts, with absence already distinguished from unknown by the caller.</summary>
    public static bool Recalled(bool maDisabled, bool hasSecondary, bool? everySecondaryDisabled, int passengerCapacity, int towingCapacity) =>
        ScenarioA1VehicleBreakdownCalculator.Recalled(maDisabled, hasSecondary, everySecondaryDisabled, passengerCapacity > 0 || towingCapacity > 0);

    /// <summary>A21.22 (PDF pp. 88-89): preserve the existing Temporary crew penalty while distinguishing actual capture from the other use penalty.</summary>
    public static VehicleBreakdownUseFacts WithVehicleCrew(VehicleBreakdownUseFacts facts, string? manning, bool captured)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var multiple = ScenarioA1CapturedVehicleRules.WeaponPenalty(manning, captured) ?? 0;
        return facts with
        {
            Captured = captured && multiple > 0,
            NonQualified = multiple > (captured ? 1 : 0),
        };
    }

    /// <summary>A nonthrowing validation entry point for proposal/precheck, before any dice are requested.</summary>
    public static string? FactsRefusal(VehicleBreakdownUseFacts facts, VehicleBreakdownUse use)
    {
        try
        {
            _ = Numbers(facts, use);
            return null;
        }
        catch (ArgumentException)
        {
            return "vehicle-breakdown-facts-invalid: supply a known weapon/use, printed numbers 2 to 12 and weather reduction 0 to 2";
        }
    }

    public static VehicleBreakdownNumbers Numbers(VehicleBreakdownUseFacts facts, VehicleBreakdownUse use)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!Enum.IsDefined(use))
        {
            throw new ArgumentException("Unknown vehicle weapon use.", nameof(use));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(facts.WinterReduction);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(facts.WinterReduction, 2);
        if (facts.PrintedB is < 2 or > 12 || facts.PrintedX is < 2 or > 12)
        {
            throw new ArgumentException("Printed vehicle breakdown numbers must be between 2 and 12.", nameof(facts));
        }

        // A19.131 exempts CC, not the IFT DR of an OVR. Captured vehicles retain the vehicle shortage reading:
        // A21.11's shortage exception names captured Gun/SW, whereas its lowering explicitly also names vehicles.
        return ScenarioA1VehicleBreakdownCalculator.Numbers(new(facts.Weapon, facts.PrintedB, facts.PrintedX, facts.Circled,
            facts.NoteExtends, facts.LowAmmo, facts.AmmunitionShortage && use != VehicleBreakdownUse.CloseCombat,
            facts.IntensiveFire ? 2 : 0, facts.WinterReduction + (facts.Captured ? 2 : 0) + (facts.NonQualified ? 2 : 0)));
    }

    public static VehicleBreakdownResolved Resolve(VehicleBreakdownUseFacts facts, VehicleBreakdownUse use, int originalDr)
    {
        var numbers = Numbers(facts, use);
        var kind = use == VehicleBreakdownUse.OrdnanceToHit ? VehicleBreakdownDr.ToHit
            : use == VehicleBreakdownUse.CloseCombat ? VehicleBreakdownDr.CloseCombat : VehicleBreakdownDr.Ift;
        return new(numbers, ScenarioA1VehicleBreakdownCalculator.Outcome(originalDr, numbers, kind));
    }

    /// <summary>
    /// D3.71: the Gun's own shared Area IFT DR qualifies in addition to its To Hit DR. Both belong to one use and read
    /// its pre-shot numbers; Low Ammo gained during this use is not applied as a second lowering inside the same shot.
    /// Supply no effects DR for a miss, and never pass a target's morale/collateral follow-up. The strongest outcome is retained.
    /// </summary>
    public static VehicleBreakdownResolved ResolveArea(VehicleBreakdownUseFacts facts, int toHitDr, int? sharedEffectsDr)
    {
        var hit = Resolve(facts, VehicleBreakdownUse.OrdnanceToHit, toHitDr);
        if (sharedEffectsDr is not { } dr)
        {
            return hit;
        }

        var effects = Resolve(facts, VehicleBreakdownUse.AreaHit, dr);
        var strongest = Rank(effects.Outcome) > Rank(hit.Outcome) ? effects : hit;
        return strongest with
        {
            LowAmmoGained = hit.LowAmmoGained || effects.LowAmmoGained
        };
    }

    private static int Rank(VehicleBreakdownOutcome outcome) => outcome switch
    {
        VehicleBreakdownOutcome.Disabled => 3,
        VehicleBreakdownOutcome.Malfunction => 2,
        VehicleBreakdownOutcome.LowAmmo => 1,
        _ => 0,
    };

    /// <summary>D3.7's Hero Rider exception requires this actual friendly active Hero aboard this vehicle as a Rider.</summary>
    public static bool HeroRider(string vehicleId, VehicleRepairHeroFacts? hero) => !string.IsNullOrWhiteSpace(vehicleId) && hero is not null
        && !string.IsNullOrWhiteSpace(hero.UnitId) && hero.Active && hero.IsHero && hero.SameSide && hero.IsRider
        && hero.CarrierId == vehicleId && !hero.Broken && !hero.Captured;

    public static string? RepairRefusal(VehicleRepairUseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.WeaponFacts);
        var weapon = facts.WeaponFacts;
        if (string.IsNullOrWhiteSpace(weapon.VehicleId))
        {
            return "play.repair-unit: identify the vehicle whose weapon is to be repaired (D3.7)";
        }

        if (!facts.WeaponExists || string.IsNullOrWhiteSpace(weapon.Weapon))
        {
            return $"play.repair-weapon: {weapon.VehicleId} has no listed weapon named '{weapon.Weapon}' to repair (D3.7)";
        }

        if (facts.IsSecondaryArmament && weapon.Weapon is "ma" or "bmg" or "cmg" or "aamg")
        {
            return $"play.repair-weapon: {weapon.VehicleId}'s secondary armament needs its own listed identity (D3.7)";
        }

        if (facts.Hero is not null && (weapon.Weapon != ScenarioA1VehicleBreakdownCalculator.AntiAircraftMg
            || !HeroRider(weapon.VehicleId, facts.Hero)))
        {
            return $"play.repair-unit: the selected participant is not an active friendly Hero Rider of {weapon.VehicleId} repairing its AAMG (D3.7)";
        }

        var authoritative = weapon with
        {
            HeroRider = HeroRider(weapon.VehicleId, facts.Hero)
        };
        if (!facts.IsSecondaryArmament)
        {
            return ScenarioA1VehicleBreakdownCalculator.RepairRefusal(authoritative);
        }

        // D3.7 gives each listed SA the same inherent-crew repair as the MA; the caller preserves its actual identity/attempt.
        var refusal = ScenarioA1VehicleBreakdownCalculator.RepairRefusal(authoritative with
        {
            Weapon = "ma"
        });
        return refusal?.Replace("MA", weapon.Weapon, StringComparison.Ordinal);
    }
}
