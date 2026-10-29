namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// E10 Ammo Vehicles (p. 244), the one calculator of the section (the E10 claim's design, ruling RE10.1): selection and portage (E10.11, E10.12), the
/// B12 benefit and its restrictions (E10.2, E10.21), Replenishment and what it restores (E10.3, E10.31), Immobilization and transfer (E10.4), the burning
/// Ammo Vehicle (E10.5), and the Ammo Dump (E10.6). The bodies of <see cref="TurnAmmoEligibilityRules"/> moved here unchanged. Facts in, verdicts out:
/// an unknown fact never counts as allowed.
/// </summary>
public static class ScenarioA1AmmoVehicleCalculator
{
    /// <summary>E10.11 uses ordinary capacity before the Ammo Supply counter reduces usable capacity to zero.</summary>
    public static TurnAmmoEligibilityVerdict Selection(TurnAmmoVehicleSelection facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.OrdinaryPassengerPp ?? 0);
        ValidateCaliber(facts.RecipientCaliberMm);
        bool? capacity = facts.OrdinaryPassengerPp switch
        {
            null => null,
            < 15 => false,
            >= 20 => true,
            _ => facts.RecipientCaliberMm is null ? null : facts.RecipientCaliberMm < 100
        };
        var selection = facts.HasSpecifiedVehicle switch
        {
            true => facts.MatchesSpecifiedVehicle,
            false => capacity,
            null => null
        };
        return Assess([facts.Authorized, facts.IsVehicle, facts.RecipientHasLowAmmoBNumber, selection]);
    }

    /// <summary>D.7 (p. 192; RE10.2): Immobile is Bogged, Mired, Immobilized, Abandoned, its inherent crew broken, Shocked, or Stunned. TI, Prep Fire and radioless movement restrictions alone do not make a vehicle Immobile.</summary>
    public static bool? Immobile(TurnAmmoMobility facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        bool?[] conditions = [facts.Bogged, facts.Mired, facts.Immobilized, facts.Abandoned,
            facts.InherentCrewBroken, facts.Shocked, facts.Stunned];
        return conditions.Any(value => value == true) ? true : conditions.Any(value => value is null) ? null : false;
    }

    /// <summary>Accessible is an adjacent Location reachable by hypothetical Infantry in a normal APh, ignoring enemy presence. Same Location is separately sufficient.</summary>
    public static bool? SameOrAccessible(bool? sameLocation, bool? adjacent, bool? normalInfantryAdvanceIgnoringEnemy)
    {
        var accessible = Both(adjacent, normalInfantryAdvanceIgnoringEnemy);
        return sameLocation == true || accessible == true ? true
            : sameLocation == false && accessible == false ? false : null;
    }

    /// <summary>E10.2: identically armed, in Gun caliber and barrel length.</summary>
    public static bool? Compatible(TurnAmmoArmament supply, TurnAmmoArmament recipient)
    {
        ValidateArmament(supply);
        ValidateArmament(recipient);
        bool? caliber = supply.CaliberMm is null || recipient.CaliberMm is null ? null : supply.CaliberMm == recipient.CaliberMm;
        bool? barrel = supply.Barrel == TurnAmmoBarrel.Unknown || recipient.Barrel == TurnAmmoBarrel.Unknown ? null : supply.Barrel == recipient.Barrel;
        return Both(caliber, barrel);
    }

    /// <summary>E10.2/.3 complete simultaneous recipient set. A partial inventory cannot establish the two-recipient limit.</summary>
    public static TurnAmmoEligibilityVerdict Recipients(TurnAmmoSupplyFacts supply,
        IReadOnlyList<TurnAmmoRecipientFacts> recipients, bool? completeRecipientInventory, TurnAmmoUse use, bool? notBoundingFire = null)
    {
        ArgumentNullException.ThrowIfNull(supply);
        ArgumentNullException.ThrowIfNull(recipients);
        ValidateArmament(supply.Ammunition);
        if (!Enum.IsDefined(use))
        {
            throw new ArgumentException("Unknown ammunition use.", nameof(use));
        }
        if (recipients.Count is < 1 or > 2)
        {
            return new(TurnAmmoEligibility.Ineligible, "An E10 use needs one or two recipients, never more than two simultaneously.");
        }
        List<bool?> requirements = [supply.Active, supply.Authorized, supply.Available,
            KnownIdentity(supply.Id), KnownIdentity(supply.Owner), completeRecipientInventory == true ? true : null,
            use == TurnAmmoUse.FireBenefit ? notBoundingFire : true];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recipient in recipients)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            requirements.AddRange([KnownIdentity(recipient.Id), KnownIdentity(recipient.Owner),
                string.IsNullOrWhiteSpace(recipient.Id) ? null : ids.Add(recipient.Id) && recipient.Id != supply.Id,
                string.IsNullOrWhiteSpace(recipient.Owner) || string.IsNullOrWhiteSpace(supply.Owner) ? null : recipient.Owner == supply.Owner,
                recipient.Active, recipient.IsVehicle, recipient.HasLowAmmoBNumber, recipient.CrewExposed,
                recipient.SameOrAccessibleLocation, Compatible(supply.Ammunition, recipient.Armament),
                use == TurnAmmoUse.Replenishment ? recipient.LowAmmo : true]);
        }
        return Assess(requirements);
    }

    /// <summary>E10.12: an Ammo Vehicle's usable Passenger capacity is zero.</summary>
    public static int? PassengerCapacity(int? ordinary, bool hasAmmoSupply) => hasAmmoSupply ? 0 : ordinary;

    /// <summary>E10.12 (RE10.2): the Ammo Supply counter may not be removed unless its vehicle is Immobile (E10.4) or the supply Depleted (E10.3).</summary>
    public static string? SupplyRemovalRefusal(bool? immobile, bool? depleted) =>
        immobile == true || depleted == true ? null
        : immobile is null || depleted is null ? "Prove the Ammo Vehicle Immobile or its supply Depleted before removing the Ammo Supply (E10.12)."
        : "The Ammo Supply counter may not be removed unless its vehicle is Immobile or the supply Depleted (E10.12).";

    /// <summary>
    /// E10.2, E10.21 (RE10.3): may this shot fire on the B12? The correct Ammo Vehicle (authorized, of the firer's side, identically armed, with
    /// supply), the firer CE and in its same or an Accessible Location, at most two vehicles served including this one, and not Bounding (First) Fire.
    /// </summary>
    public static TurnAmmoEligibilityVerdict Benefit(AmmoBenefitFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.ServedNow);
        var others = facts.ServedNow.Where(id => id != facts.Firer.Id).Distinct(StringComparer.Ordinal).Count();
        return Check([
            (Recipients(facts.Supply, [facts.Firer], true, TurnAmmoUse.FireBenefit, Not(facts.BoundingFirstFire)) switch
            {
                { State: TurnAmmoEligibility.Eligible } => true,
                { State: TurnAmmoEligibility.Ineligible } => false,
                _ => null,
            }, "The correct Ammo Vehicle with supply, and the proper CE armed vehicle in its same or an Accessible Location, not in Bounding (First) Fire (E10.2, E10.21)."),
            (others <= 1, "One Ammo Vehicle supplies no more than two identically armed vehicles at a time (E10.2)."),
        ]);
    }

    /// <summary>The refusal of a shot's B12 benefit (RE10.3), or null when it applies.</summary>
    public static string? BenefitRefusal(AmmoBenefitFacts facts) => Refusal(Benefit(facts));

    /// <summary>
    /// D3.71, E10.2 (RE10.11): a shot's breakdown numbers. With the benefit, a normal B12 and no X#: "treated as a normal B12" is an uncircled B12, with
    /// none of the circled B#'s Low Ammo effects. Without it, the B# and X# as given.
    /// </summary>
    public static AmmoBreakdown BreakdownNumber(int? bNumber, int? xNumber, bool benefit) => benefit ? new(12, null) : new(bNumber, xNumber);

    /// <summary>E10.21 (RE10.3): a benefit used for Prep Fire marks the Ammo Vehicle with a Prep Fire counter, and it spends no MP in the next MPh.</summary>
    public static bool BenefitBarsMovement(string? phase) => phase is "pfph" or "PFPh";

    /// <summary>
    /// E10.3 (RE10.4): the complete Game Turns a Replenishment takes, by the circled B# before Low Ammo: 1 to 9 one (the table prints 9; a lower circled
    /// B#, as under A19.131, needs no more time), B10 two, B11 three; none for 0 or less and 12 or more.
    /// </summary>
    public static int? ReplenishmentTurns(int? originalBNumber) => originalBNumber switch
    {
        > 0 and <= 9 => 1,
        10 => 2,
        11 => 3,
        _ => null,
    };

    /// <summary>E10.4 (RE10.4): a transfer or offload takes the Replenishment procedure for four Game Turns.</summary>
    public static int TransferTurns => 4;

    /// <summary>E10.3: the armed vehicle must remain CE during Replenishment.</summary>
    public static bool ReplenishmentPosture(bool? buttonedUp) => buttonedUp == false;

    /// <summary>
    /// E10.3, E10.31 (RE10.4): may this Replenishment start? Today's recipient set (Low Ammo, CE, same or Accessible, identically armed, at most two at
    /// once), both TI (a Dump trivially), and no Passenger aboard the supply vehicle (E10.12's zero capacity).
    /// </summary>
    public static TurnAmmoEligibilityVerdict Replenishment(AmmoReplenishmentFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return Check([
            (Recipients(facts.Supply, facts.Recipients, facts.CompleteRecipientInventory, TurnAmmoUse.Replenishment) switch
            {
                { State: TurnAmmoEligibility.Eligible } => true,
                { State: TurnAmmoEligibility.Ineligible } => false,
                _ => null,
            }, "A Low Ammo, CE, identically armed vehicle in the same or an Accessible Location, at most two at once (E10.3, E10.31)."),
            (facts.RecipientsTi, "The armed vehicle is TI for the Replenishment (E10.3)."),
            (facts.IsDump ? true : facts.SupplyTi, "The Ammo Vehicle is TI for the Replenishment (E10.3)."),
            (facts.IsDump ? true : Not(facts.SupplyHasPassengers), "An Ammo Vehicle carries no Passengers (E10.12)."),
        ]);
    }

    /// <summary>The refusal of a Replenishment (RE10.4), or null when it may start.</summary>
    public static string? ReplenishmentRefusal(AmmoReplenishmentFacts facts) => Refusal(Replenishment(facts));

    /// <summary>E10.3: an Original 12 on the Ammunition DR exhausts the Ammo Vehicle; its Ammo Supply is removed and it takes its normal portage again.</summary>
    public static bool SupplyExhausted(int? originalAmmunitionDr) => originalAmmunitionDr == 12;

    /// <summary>E10.3: a completed Replenishment is followed by the Ammo Vehicle's Ammunition DR.</summary>
    public static bool RequiresAmmunitionDr(TurnWorkKind kind) => kind == TurnWorkKind.AmmoReplenishment;

    /// <summary>
    /// E10.31 (RE10.5): a completed Replenishment ends a vehicle's Low Ammo and restores every Ammo Type normal Depletion (C8.9) took; a vehicle without
    /// Low Ammo is not Replenished, and nothing is restored.
    /// </summary>
    public static AmmoRestored Restored(bool lowAmmo, IReadOnlyList<string> depletedTypes)
    {
        ArgumentNullException.ThrowIfNull(depletedTypes);
        return lowAmmo ? new(true, [.. depletedTypes]) : new(false, []);
    }

    /// <summary>E10.4 (RE10.6): an unarmored Ammo Vehicle that becomes Immobilized is not flipped to its Wreck side; it keeps supplying.</summary>
    public static bool WreckWhenImmobilized(bool armored, bool ammoVehicle) => armored || !ammoVehicle;

    /// <summary>
    /// E10.4, E10.12 (RE10.6, a reading open to veto): may this Ammo Supply move? The source Immobile or its supply Depleted, for an offload too; the
    /// destination a Dump in the source's own Location, or a vehicle of its side in the same or an Accessible Location that qualifies under E10.11 and
    /// holds no supply.
    /// </summary>
    public static TurnAmmoEligibilityVerdict Transfer(AmmoTransferFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        bool? source = facts.SourceImmobile == true || facts.SupplyDepleted == true ? true
            : facts.SourceImmobile is null || facts.SupplyDepleted is null ? null : false;
        return Check(facts.ToDump
            ? [(source, "Only an Immobile Ammo Vehicle, or a Depleted one, gives up its Ammo Supply (E10.12)."),
                (facts.DumpInSourceLocation, "An Ammo Dump forms in the Ammo Vehicle's own Location (E10.4).")]
            : [(source, "Only an Immobile Ammo Vehicle, or a Depleted one, gives up its Ammo Supply (E10.12)."),
                (facts.DestinationSameSide, "The new Ammo Vehicle is of the same side."),
                (facts.DestinationSameOrAccessible, "The new Ammo Vehicle is in the same or an Accessible Location (E10.4)."),
                (facts.DestinationQualifies, "The new Ammo Vehicle qualifies as one (E10.11)."),
                (Not(facts.DestinationHasSupply), "The new Ammo Vehicle holds no Ammo Supply already.")]);
    }

    /// <summary>The refusal of a transfer or offload (RE10.6), or null when it may go ahead.</summary>
    public static string? TransferRefusal(AmmoTransferFacts facts) => Refusal(Transfer(facts));

    /// <summary>E10.4: after a transfer, an Immobilized original is flipped to its Wreck side.</summary>
    public static bool WreckAfterTransfer(bool immobilized) => immobilized;

    /// <summary>E10.4 (RE10.6): a vehicle may not spend MP during the turn in which it becomes, or ceases to be, an Ammo Vehicle.</summary>
    public static bool MovementBarred(bool becameOrCeasedThisTurn) => becameOrCeasedThisTurn;

    /// <summary>
    /// E10.5 (RE10.7; Vehicle Note 93, p. 348): an Ammo Vehicle that becomes a Burning Wreck, or would if it had a Wreck side, explodes at once as a
    /// detonated Goliath with no designated target and no X#. Resolving each attack is the Placed DC's calculator (A23).
    /// </summary>
    public static AmmoExplosion Explosion(bool ammoVehicle, bool burningOrWouldBe) => ammoVehicle && burningOrWouldBe
        ? new(true, true, 36, 16, "Successfully Positioned, weakest rear AF", "Poorly Positioned", true, true)
        : new(false, false, 0, 0, "", "", false, false);

    /// <summary>
    /// E10.6 (RE10.8): an Ammo Dump is a stationary Ammo Supply: always Immobile, no Passenger capacity, no wreck, and TI trivially; every Ammo Vehicle
    /// rule applies to it in its own Location.
    /// </summary>
    public static bool? DumpImmobile(bool isDump, bool? vehicleImmobile) => isDump ? true : vehicleImmobile;

    private static TurnAmmoEligibilityVerdict Check(IReadOnlyList<(bool? Met, string Reason)> requirements) =>
        requirements.FirstOrDefault(item => item.Met == false) is { Reason: { } refused }
            ? new(TurnAmmoEligibility.Ineligible, refused)
            : requirements.FirstOrDefault(item => item.Met is null) is { Reason: { } open }
                ? new(TurnAmmoEligibility.NotEvaluated, "Not known: " + open)
                : new(TurnAmmoEligibility.Eligible, "The E10 requirements are met.");

    private static string? Refusal(TurnAmmoEligibilityVerdict verdict) => verdict.State == TurnAmmoEligibility.Eligible ? null : verdict.Reason;

    private static bool? Not(bool? value) => value is null ? null : !value;

    private static TurnAmmoEligibilityVerdict Assess(IEnumerable<bool?> requirements)
    {
        var values = requirements.ToArray();
        return values.Any(value => value == false)
            ? new(TurnAmmoEligibility.Ineligible, "A proven E10 participant, compatibility, capacity, posture or use requirement is not met.")
            : values.Any(value => value is null)
                ? new(TurnAmmoEligibility.NotEvaluated, "Supply the missing E10 identity, source, participant and complete-assignment facts.")
                : new(TurnAmmoEligibility.Eligible, "The supplied E10 participant facts qualify; timing, live assignments, command authority and consequences remain separate checks.");
    }

    private static bool? KnownIdentity(string? value) => string.IsNullOrWhiteSpace(value) ? null : true;
    private static bool? Both(bool? first, bool? second) => first == false || second == false ? false : first == true && second == true ? true : null;
    private static void ValidateCaliber(decimal? caliber)
    {
        if (caliber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(caliber));
        }
    }
    private static void ValidateArmament(TurnAmmoArmament armament)
    {
        ArgumentNullException.ThrowIfNull(armament);
        ValidateCaliber(armament.CaliberMm);
        if (!Enum.IsDefined(armament.Barrel))
        {
            throw new ArgumentException("Unknown barrel designation.", nameof(armament));
        }
    }
}
