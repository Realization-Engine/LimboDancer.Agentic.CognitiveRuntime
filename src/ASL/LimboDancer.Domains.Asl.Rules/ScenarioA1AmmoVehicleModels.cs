namespace LimboDancer.Domains.Asl.Rules;

// The E10 claim (its design, RE10.1): the TurnAmmo* types moved here unchanged from TurnAmmoEligibilityRules.cs, with the records of the new verdicts.

public enum TurnAmmoEligibility
{
    NotEvaluated, Eligible, Ineligible
}
public enum TurnAmmoBarrel
{
    Unknown, ShortBarrel, Standard, LongBarrel, ExtraLongBarrel
}
public enum TurnAmmoUse
{
    FireBenefit, Replenishment
}
public sealed record TurnAmmoArmament(decimal? CaliberMm, TurnAmmoBarrel Barrel);
public sealed record TurnAmmoMobility(bool? Bogged, bool? Mired, bool? Immobilized, bool? Abandoned,
    bool? InherentCrewBroken, bool? Shocked, bool? Stunned);
public sealed record TurnAmmoVehicleSelection(bool? Authorized, bool? IsVehicle, bool? RecipientHasLowAmmoBNumber,
    bool? HasSpecifiedVehicle, bool? MatchesSpecifiedVehicle, int? OrdinaryPassengerPp, decimal? RecipientCaliberMm);
public sealed record TurnAmmoSupplyFacts(string? Id, string? Owner, bool? Active, bool? Authorized, bool? Available,
    TurnAmmoArmament Ammunition);
public sealed record TurnAmmoRecipientFacts(string? Id, string? Owner, bool? Active, bool? IsVehicle,
    bool? HasLowAmmoBNumber, bool? LowAmmo, bool? CrewExposed, bool? SameOrAccessibleLocation, TurnAmmoArmament Armament);
public sealed record TurnAmmoEligibilityVerdict(TurnAmmoEligibility State, string Reason)
{
    public string Reference { get; } = "E10.1-.4; D.7; Accessible glossary; ASOP 8.43B";
}

/// <summary>
/// A shot that may fire on its Ammo Vehicle's B12 (E10.2, E10.21; RE10.3): the supply and the firing vehicle; the other vehicles the supply serves now
/// (whose benefit it gave this Player Turn and that are still in its same or an Accessible Location); whether the shot is Bounding (First) Fire (null
/// when not known); and whether the supply is an Ammo Dump (E10.6).
/// </summary>
public sealed record AmmoBenefitFacts(TurnAmmoSupplyFacts Supply, TurnAmmoRecipientFacts Firer, IReadOnlyList<string> ServedNow, bool? BoundingFirstFire,
    bool IsDump = false);

/// <summary>
/// A Replenishment's start (E10.3, E10.31; RE10.4): the supply and its recipients, whether the list of recipients is complete, whether every recipient
/// and the supply vehicle are TI, whether Passengers are aboard the supply vehicle, and whether the supply is an Ammo Dump (TI trivially, no Passengers).
/// </summary>
public sealed record AmmoReplenishmentFacts(TurnAmmoSupplyFacts Supply, IReadOnlyList<TurnAmmoRecipientFacts> Recipients, bool? CompleteRecipientInventory,
    bool? RecipientsTi, bool? SupplyTi, bool? SupplyHasPassengers, bool IsDump = false);

/// <summary>
/// A transfer or offload of an Ammo Supply (E10.4, E10.12; RE10.6): the source Immobile or its supply Depleted; to a Dump in the source's own Location, or
/// to a vehicle of its side in the same or an Accessible Location that qualifies under E10.11 and holds no supply. Null when not known.
/// </summary>
public sealed record AmmoTransferFacts(bool? SourceImmobile, bool? SupplyDepleted, bool ToDump, bool? DumpInSourceLocation, bool? DestinationSameSide,
    bool? DestinationSameOrAccessible, bool? DestinationQualifies, bool? DestinationHasSupply);

/// <summary>The breakdown numbers a shot uses (D3.71, E10.2; RE10.11): its B# and its X#, either null when it has none.</summary>
public sealed record AmmoBreakdown(int? BNumber, int? XNumber);

/// <summary>What a completed Replenishment restores (E10.31; RE10.5): whether the Low Ammo ends, and the depleted Ammo Types restored.</summary>
public sealed record AmmoRestored(bool LowAmmoEnded, IReadOnlyList<string> AmmoTypes);

/// <summary>
/// A burning Ammo Vehicle's explosion (E10.5; Vehicle Note 93, p. 348; RE10.7), as facts for the Placed DC's calculator (A23): whether it explodes; its
/// wreck removed; one Effects DR for every attack; the charge at the vehicle's level; the FP against unarmored units in its hex and in each adjacent
/// hex; an AFV in its hex attacked as Successfully Positioned against its weakest rear AF, never Optimal; an adjacent AFV as Poorly Positioned;
/// Fortifications only in its hex; no designated target and no X#.
/// </summary>
public sealed record AmmoExplosion(bool Explodes, bool WreckRemoved, int HexFp, int AdjacentFp, string SameHexAfvPosition, string AdjacentAfvPosition,
    bool FortificationsOnlyInItsHex, bool OneEffectsDr);
