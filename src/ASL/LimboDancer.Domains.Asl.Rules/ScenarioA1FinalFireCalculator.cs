namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A8.4 Final Fire and A8.41 Multiple ROF (p. 61), A9.3 Sustained Fire (p. 63), and C2.241 First/Final Fire (p. 168), the one calculator of the
/// rules (the A8.41 claim's design, ruling RA8.1): who may fire in the DFPh, how, and at what range. The counter that decides is the weapon's own for a
/// weapon and the unit's own for its inherent FP (ruling RA8.2): a First-Fire-marked MG fires again only at an ADJACENT or same-hex target whoever
/// holds it, and a MG with no counter fires at any target though its unit is marked. Fire counters, the range, and the phase are public (ruling
/// RA8.5). A FT, DC, or MOL use's own once-per-Player-Turn rule (A22.3, A22.6, A23.63) is its caller's question (ruling RA8.8). Facts in, verdicts out.
/// </summary>
public static class ScenarioA1FinalFireCalculator
{
    /// <summary>A9.3 (p. 63; A.11, p. 43): Sustained Fire lowers a MG's B# by two, and its Original B# is its X# during that use.</summary>
    public const int SustainedBreakdownReduction = 2;

    /// <summary>A9.3 (p. 63): Sustained Fire places a Final Fire counter on the weapon, whatever its Multiple ROF and colored dr.</summary>
    public const string SustainedCounter = "final-fire";

    private const string FinalFireMarked = "marked Final Fire: a unit or weapon already so marked cannot fire during Final Fire (A8.4)";
    private const string IntensiveFireMarked = "marked Intensive Fire: it cannot fire during Final Fire (A8.4, C5.6)";
    private const string NoFireMarked = "marked No Fire: it cannot fire during Final Fire (A8.4)";
    private const string NoSecondAttack =
        "marked First Fire and able to use neither Sustained nor Intensive Fire: it has no Final Fire (A8.41, A9.3, C2.241)";
    private const string OutOfReach = "marked First Fire: it fires again only at an ADJACENT or same-hex target (A8.4, A8.41)";

    /// <summary>
    /// A8.4 (p. 61): whether a unit or weapon fires again in Final Fire on its First Fire counter: the phase is the DFPh and it is marked First Fire.
    /// The fire calculator's own check and the Fire package's spent fire (<see cref="ScenarioA1FireEligibility.FireSpent"/>) ask it.
    /// </summary>
    public static bool FiresAgain(bool dfph, bool firstFireMarked) => dfph && firstFireMarked;

    /// <summary>
    /// A8.4 (p. 61): whether a unit may fire its inherent FP in Final Fire, the range aside: not marked Final Fire, and marked First Fire or not yet
    /// fired this Player Turn (A7.1).
    /// </summary>
    public static bool UnitMayFinalFire(bool firstFireMarked, bool finalFireMarked, bool firedThisPlayerTurn) =>
        !finalFireMarked && (firstFireMarked || !firedThisPlayerTurn);

    /// <summary>
    /// A8.4, A8.41 (p. 61; ruling RA8.8): whether a weapon may be chosen to fire in Final Fire by its counter alone: not marked Prep or Final Fire. A
    /// First Fire counter does not bar it; its range is <see cref="FinalFireRefusal"/>'s.
    /// </summary>
    public static bool WeaponMayFinalFire(bool markedPrepOrFinalFire) => !markedPrepOrFinalFire;

    /// <summary>
    /// A9.3 (p. 63): whether a weapon may use Sustained Fire: a MG, not a vehicular MG unless it is the MA ("as per 8.4"; D3.5, ruling RA8.3), not
    /// firing as ordnance, and not fired by a lone SMC (also A8.312's EXC).
    /// </summary>
    public static bool MaySustain(bool isMg, bool vehicular, bool mainArmament, bool asOrdnance, bool loneSmc) =>
        isMg && (!vehicular || mainArmament) && !asOrdnance && !loneSmc;

    /// <summary>
    /// A9.3 (p. 63): whether a weapon's attack is Sustained Fire: it is used as Subsequent First Fire or FPF (A8.3, A8.31), or fires in the DFPh while
    /// marked First Fire (A8.4); and it is a MG, a weapon not known to be one counting as one (the referee of pass 9b).
    /// </summary>
    public static bool Sustained(bool subsequentOrFpfUsed, bool dfph, bool weaponFirstFireMarked, bool? isMg) =>
        (subsequentOrFpfUsed || (dfph && weaponFirstFireMarked)) && isMg != false;

    /// <summary>A8.4, A8.41 (p. 61): a target in an ADJACENT or the same hex, range one or zero. An unknown range is not in reach.</summary>
    public static bool InReach(int? range) => range is >= 0 and <= 1;

    /// <summary>
    /// The vehicle MA MG's own counter and crew facts for A8.4 and A9.3 (pp. 61, 63). D3.5 (p. 200): a vehicle's MG "may make only one fire attack
    /// per Player Turn unless it is the vehicle's MA with a specific Multiple ROF", so an MA MG sustains only when its definition prints a Multiple
    /// ROF; with none (or no definition) it counts as a vehicular MG that is not the MA and has no Final Fire (the review of PR 75, item 46).
    /// </summary>
    public static FinalFireFacts VehicleFacts(FireAttack attack, FireVehicleFire vehicle, FireDefinition? definition) =>
        VehicleFacts(vehicle, definition?.RateOfFire is > 0, attack.Range);

    /// <summary>The same own-counter facts before a target is chosen; a missing range does not decide whether the weapon is spent.</summary>
    public static FinalFireFacts VehicleFacts(FireVehicleFire vehicle, bool multipleRof, int? range = null) =>
        new(FinalFireWeapon.Mg, vehicle.FinalFireMarker ?? FireMarker.None, range, false, true, multipleRof, false, false,
            vehicle.Pinned != true && vehicle.Stunned != true);

    /// <summary>A8.41/A9.3 (pp. 61, 63): the MA's own recorded counter takes precedence over the vehicle's other weapons. Null uses the earlier unit counter.</summary>
    public static FireMarker VehicleMarker(string? counter, bool ownShot, bool firstFire, bool finalFire, bool rateOfFireKept) =>
        !ownShot || rateOfFireKept ? FireMarker.None : counter switch
        {
            "none" => FireMarker.None,
            "first-fire" => FireMarker.FirstFire,
            "final-fire" => FireMarker.FinalFire,
            "intensive-fire" => FireMarker.IntensiveFire,
            null => finalFire ? FireMarker.FinalFire : firstFire ? FireMarker.FirstFire : FireMarker.None,
            _ => FireMarker.NoFire,
        };

    /// <summary>C2.241, C5.6 (pp. 168, 172): the Gun's own marker, firing mode and actual crew for Final Fire.</summary>
    public static FinalFireFacts OrdnanceFacts(OrdnanceShot shot, GunDefinition definition, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(reference);
        var gun = shot.Gun!;
        var othersOnly = shot.Vehicle?.Weapons?.MarkedByOthers == true;
        var vehicleRof = shot.Vehicle is not null && gun.ShotsThisPhase > 0 && gun.RateOfFireKept == true;
        var marker = gun.IntensiveFired == true ? FireMarker.IntensiveFire
            : !othersOnly && gun.FinalFire == true ? FireMarker.FinalFire
            : !othersOnly && !vehicleRof && gun.FirstFire == true ? FireMarker.FirstFire : FireMarker.None;
        var sw = ScenarioA1OrdnanceParticipationCalculator.FiresAsLight(shot, definition, reference)
            || definition.LatwType is not null || definition.GunType == ScenarioA1MachineGunKill.GunType;
        return new(sw ? FinalFireWeapon.OrdnanceSw : FinalFireWeapon.Gun, marker, shot.Range,
            ScenarioA1OrdnanceParticipationCalculator.SmcCount(reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId!)?.Kind, shot.Participation) == 1,
            shot.Vehicle is not null, shot.Vehicle is not null, true, definition.NoIntensiveFire,
            IntensiveFireCrewFit(ScenarioA1OrdnanceParticipationCalculator.Pinned(shot), shot.Crew.Broken == true,
                shot.Vehicle?.Shocked == true, shot.Vehicle?.Stunned == true));
    }

    /// <summary>A8.4, A8.41 (p. 61): facts for the selected inherent FP or held weapon, using that subject's own counter.</summary>
    public static FinalFireFacts InfantryFacts(FireAttack attack, FireFirer firer, FireWeapon? weapon, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(firer);
        ArgumentNullException.ThrowIfNull(reference);
        var definition = reference.Definitions.GetValueOrDefault(firer.DefinitionId ?? string.Empty);
        var first = weapon is null ? firer.FirstFireMarked == true : weapon.FirstFireMarked == true;
        var final = weapon is null ? firer.FinalFireMarked == true : weapon.FiredThisPlayerTurn == true;
        var kind = weapon is null ? FinalFireWeapon.Inherent
            : reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsMg == true ? FinalFireWeapon.Mg : FinalFireWeapon.OtherSw;
        return new(kind, final ? FireMarker.FinalFire : first ? FireMarker.FirstFire : FireMarker.None,
            firer.Range ?? attack.Range, (definition is { IsLeader: true } or { IsHero: true }) && firer.Partner is null,
            false, false, false, false, firer.Pinned != true && firer.Broken != true);
    }

    /// <summary>C5.6, C5.63 (p. 172): whether a Gun's crew may Intensive Fire it: not a "No IF" Gun, and its crew not pinned, shocked, or stunned.</summary>
    public static bool MayIntensiveFire(bool noIntensiveFire, bool crewFit) => !noIntensiveFire && crewFit;

    /// <summary>C5.6 (p. 172): the same crew-state test for the command and its Intensive Fire control.</summary>
    public static bool IntensiveFireCrewFit(bool pinned, bool broken, bool shocked, bool stunned) => !pinned && !broken && !shocked && !stunned;

    /// <summary>
    /// A8.4, A8.41 (p. 61), A9.3 (p. 63), C2.241 (p. 168), C2.29: what Final Fire allows a unit or weapon by its own counter. With no counter, Normal
    /// (a Multiple-ROF weapon that kept its ROF in First Fire has no counter). Marked Final, Intensive, or No Fire, None. Marked First Fire: OnceMore
    /// for a unit's inherent FP (A8.4), for a MG that <see cref="MaySustain"/>, for IFE not fired by a lone SMC (A8.312, C2.29), and for a Gun that
    /// <see cref="MayIntensiveFire"/> (C2.241); None for an ordnance SW (no SW Intensive Fires, C5.6, and no ordnance sustains, A9.3), for a MG that
    /// may not sustain, and for another SW (a FT or DC, which can use neither; ruling RA8.8).
    /// </summary>
    public static FinalFireUse Use(FinalFireFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        Validate(facts);
        return facts.Marker switch
        {
            FireMarker.None => FinalFireUse.Normal,
            FireMarker.FirstFire => facts.Weapon switch
            {
                FinalFireWeapon.Inherent => FinalFireUse.OnceMore,
                FinalFireWeapon.Ife => facts.LoneSmc ? FinalFireUse.None : FinalFireUse.OnceMore,
                FinalFireWeapon.Mg => MaySustain(true, facts.Vehicular, facts.MainArmament, facts.AsOrdnance, facts.LoneSmc) ? FinalFireUse.OnceMore : FinalFireUse.None,
                FinalFireWeapon.Gun => MayIntensiveFire(facts.NoIntensiveFire, facts.CrewFit) ? FinalFireUse.OnceMore : FinalFireUse.None,
                _ => FinalFireUse.None,
            },
            _ => FinalFireUse.None,
        };
    }

    /// <summary>
    /// A8.4, A8.41 (p. 61), A9.3 (p. 63), C2.241 (p. 168): why this unit or weapon may not fire in Final Fire at this target, or null when it may. The
    /// checks in order: its counter bars it; marked First Fire, it may use neither Sustained nor Intensive Fire; marked First Fire, the target is not
    /// ADJACENT or in its hex (an unknown range is not in reach).
    /// </summary>
    public static string? FinalFireRefusal(FinalFireFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        Validate(facts);
        return facts.Marker switch
        {
            FireMarker.None => null,
            FireMarker.FinalFire => FinalFireMarked,
            FireMarker.IntensiveFire => IntensiveFireMarked,
            FireMarker.NoFire => NoFireMarked,
            _ => Use(facts) == FinalFireUse.None ? NoSecondAttack : InReach(facts.Range) ? null : OutOfReach,
        };
    }

    /// <summary>
    /// The verdict of one Final Fire at this target (ruling RA8.1). Refused, the use None and the refusal. Otherwise the use; a OnceMore attack is
    /// Area Fire except for ordnance (A7.23, p. 55), Sustained for a MG or IFE (A9.3, C2.29) with the B# two lower, and Intensive for a Gun (C2.241). A Normal attack is none of
    /// these here: the attack's other facts (Subsequent First Fire, a pinned firer) are the fire calculator's.
    /// </summary>
    public static FinalFireVerdict Verdict(FinalFireFacts facts)
    {
        if (FinalFireRefusal(facts) is { } refusal)
        {
            return new(FinalFireUse.None, false, false, false, 0, refusal);
        }

        var use = Use(facts);
        if (use == FinalFireUse.Normal)
        {
            return new(FinalFireUse.Normal, false, false, false, 0, null);
        }

        var sustained = facts.Weapon is FinalFireWeapon.Mg or FinalFireWeapon.Ife;
        return new(FinalFireUse.OnceMore, facts.Weapon != FinalFireWeapon.Gun, sustained, facts.Weapon == FinalFireWeapon.Gun, sustained ? SustainedBreakdownReduction : 0, null);
    }

    private static void Validate(FinalFireFacts facts)
    {
        if (!Enum.IsDefined(facts.Weapon))
        {
            throw new ArgumentException("Unknown Final Fire weapon.", nameof(facts));
        }

        if (!Enum.IsDefined(facts.Marker))
        {
            throw new ArgumentException("Unknown fire counter.", nameof(facts));
        }
    }
}
