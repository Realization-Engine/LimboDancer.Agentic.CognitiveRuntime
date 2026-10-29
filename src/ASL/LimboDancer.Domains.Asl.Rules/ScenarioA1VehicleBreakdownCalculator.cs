namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// D3.7 Malfunction and D3.71 Low Ammo B# (pp. 201 and 202), the one calculator of the rules (the D3.7 claim's design, ruling RD3.1): a vehicle weapon's
/// breakdown numbers this use, what an Original DR does to it, its repair, and the Recall when its armament is Disabled. It decides one weapon; which of
/// several firing MGs malfunctions stays A9.71's Random Selection in fire. Facts in, verdicts out.
/// </summary>
public static class ScenarioA1VehicleBreakdownCalculator
{
    /// <summary>
    /// Ruling RD3.2: a vehicle weapon's B# and X# this use, in order. (1) The printed numbers (B12 and no X# when none is printed) apply to the MA only,
    /// unless a Vehicle Note extends them (D3.7). (2) An Ammunition Shortage makes the MA's B# circled and one lower, and one already circled starts
    /// with Low Ammo (A19.131). (3) Low Ammo turns the current B# into the X# and makes the B# one lower (D3.71, G10.41). (4) A.11's lowerings: the B#
    /// only (C5.62), or the B# and any X# (A21.11, E3.741, A19.32); with no X# yet, a lowering makes the Original B# the X#, unlowered (A.11).
    /// </summary>
    public static VehicleBreakdownNumbers Numbers(BreakdownNumberFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!Enum.IsDefined(facts.Weapon))
        {
            throw new ArgumentException("Unknown vehicle weapon.", nameof(facts));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(facts.LowersB);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.LowersBX);
        var printed = facts.Weapon == VehicleBreakdownWeapon.MainArmament || facts.NoteExtends;
        var b = printed ? facts.PrintedB ?? 12 : 12;
        var x = printed ? facts.PrintedX : null;
        var circled = printed && facts.Circled;
        var lowAmmo = facts.LowAmmo;
        if (facts.AmmunitionShortage && facts.Weapon == VehicleBreakdownWeapon.MainArmament)
        {
            lowAmmo |= circled;
            circled = true;
            b--;
        }

        // A Low Ammo counter on the weapon's numbers turns its current B# into the X#, whether its B# is circled or the counter came another way
        // (E10 work, G10.41): D3.71 says it of the counter (the build's review, finding 1).
        lowAmmo &= printed;
        if (lowAmmo)
        {
            x = b;
            b--;
        }

        if (facts.LowersB + facts.LowersBX > 0)
        {
            x = x is { } existing ? existing - facts.LowersBX : b;
            b -= facts.LowersB + facts.LowersBX;
        }

        return new(b, x, circled, lowAmmo);
    }

    /// <summary>
    /// Ruling RD3.3 (D3.7, D3.71): what an Original DR does to the weapon. An X# reached Disables it; else an Original 12 malfunctions it; else a circled
    /// B# before Low Ammo gives Low Ammo on a To Hit or IFT DR at or above the B#; else a DR at or above the B# malfunctions it. The caller passes
    /// <see cref="VehicleBreakdownDr.Ift"/> for the MA Gun only when it fires IFE or on the Area Target Type (D3.71's "IFT DR" is the Gun's own), and, for a
    /// CC DR, numbers read with no Ammunition Shortage, which "has no effect on CC" (A19.131).
    /// </summary>
    public static VehicleBreakdownOutcome Outcome(int originalDr, VehicleBreakdownNumbers numbers, VehicleBreakdownDr kind)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        ArgumentOutOfRangeException.ThrowIfLessThan(originalDr, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(originalDr, 12);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Unknown DR.", nameof(kind));
        }

        return numbers.XNumber is { } x && originalDr >= x ? VehicleBreakdownOutcome.Disabled
            : originalDr == 12 ? VehicleBreakdownOutcome.Malfunction
            : numbers.Circled && !numbers.LowAmmo && kind != VehicleBreakdownDr.CloseCombat && originalDr >= numbers.BNumber ? VehicleBreakdownOutcome.LowAmmo
            : originalDr >= numbers.BNumber ? VehicleBreakdownOutcome.Malfunction
            : VehicleBreakdownOutcome.None;
    }

    /// <summary>
    /// Ruling RD3.4 (D3.7, p. 201): why this vehicle weapon may not be repaired now, or null. For a Gun MA, BMG, or CMG: a malfunctioned weapon not
    /// Disabled, once per RPh, by an inherent crew not shocked or stunned (an Unconfirmed Kill, a Recall read as Stun under D5.341, or an Abandoned
    /// vehicle bars it too). For an AAMG, the same, but by a CE crew not shocked or stunned, or by a Hero Rider, who replaces the crew test (D3.7's EXC;
    /// a reading the user approved), so a Hero Rider may repair it even on an Abandoned vehicle or an Unconfirmed Kill. A weapon of the SA column is not
    /// named here (no vehicle the catalog holds has one; W-D3.7). The RPh itself is the caller's phase check.
    /// </summary>
    public static string? RepairRefusal(VehicleRepairFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var (id, weapon) = (facts.VehicleId, facts.Weapon);
        if (weapon == AntiAircraftMg)
        {
            return facts.Disabled ? $"play.repair-weapon: {id}'s AAMG is disabled for good (D3.7)"
                : !facts.Malfunctioned ? $"play.repair-weapon: {id}'s AAMG is not malfunctioned (D3.7)"
                : facts.RepairedThisPhase ? $"play.repair-unit: {id}'s AAMG has had its repair attempt this RPh (D3.7)"
                : !facts.HeroRider && (!facts.CrewExposed || facts.CrewImpaired)
                    ? $"play.repair-unit: {id}'s AAMG is repaired by a CE crew that is not shocked or stunned, or by a Hero Rider (D3.7)"
                : null;
        }

        return !ScenarioA1RallyRules.IsVehicleWeapon(weapon) ? $"play.repair-weapon: {id} has no weapon named '{weapon}' to repair (D3.7)"
            : facts.Disabled ? $"play.repair-weapon: {id}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} is disabled for good (D3.7)"
            : !facts.Malfunctioned ? $"play.repair-weapon: {id}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} is not malfunctioned (D3.7)"
            : facts.RepairedThisPhase ? $"play.repair-unit: {id}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} has had its repair attempt this RPh (D3.7)"
            : facts.CrewImpaired
                ? $"play.repair-unit: {id}'s crew repairs nothing while it is shocked, stunned, or Recalled in this Player Turn, which is treated as a Stun, or while the vehicle is Abandoned (D3.7, D5.341)"
            : null;
    }

    /// <summary>The name an AAMG is repaired under (ruling RD3.4).</summary>
    public const string AntiAircraftMg = "aamg";

    /// <summary>D3.7 (ruling RD3.4): a repair dr of 1 repairs the weapon; a 6 Disables it, its counter flipped to its Disabled side (Eliminated); else nothing.</summary>
    public static RepairOutcome RepairResult(int dr) => dr == 6 ? RepairOutcome.Eliminated : dr == 1 ? RepairOutcome.Repaired : RepairOutcome.NoChange;

    /// <summary>
    /// Ruling RD3.5 (D3.7, D5.341): a vehicle whose MA and every Secondary Armament its Vehicle Listing gives it are Disabled is Recalled, unless it has
    /// Passenger or Towing capability. With no SA the MA alone suffices; an MG is SA only when listed in the SA column (D1.34). An unknown SA state is not
    /// a Recall. Asked after a repair's 6 and after a fire result that Disables.
    /// </summary>
    public static bool Recalled(bool maDisabled, bool hasSecondary, bool? everySecondaryDisabled, bool passengerOrTowing) =>
        maDisabled && (!hasSecondary || everySecondaryDisabled == true) && !passengerOrTowing;
}
