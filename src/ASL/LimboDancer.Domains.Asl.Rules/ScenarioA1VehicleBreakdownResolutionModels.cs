namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The weapon's actual use, not the target's later morale or collateral roll (D3.7, D3.71, D7.17).</summary>
public enum VehicleBreakdownUse
{
    OrdnanceToHit, Canister, InfantryFireEquivalent, MachineGun, AreaHit, Overrun, CloseCombat
}

/// <summary>
/// Authoritative supplemental facts for a new vehicle weapon reading. Printed numbers are null only when absent, not hidden.
/// WinterReduction is the existing E3.741 weather fact (0, 1 or 2); Captured and NonQualified are the two independent A21.11/.13 penalties.
/// These facts are supplemental to shot/attack records. Mounted weapons do not acquire SW inexperience penalties.
/// The resolver separately takes the Final Fire calculator's Sustained Fire verdict for a vehicle MA MG (A9.3's exception).
/// </summary>
public sealed record VehicleBreakdownUseFacts(VehicleBreakdownWeapon Weapon, int? PrintedB, int? PrintedX, bool Circled, bool NoteExtends,
    bool LowAmmo, bool AmmunitionShortage, bool Captured = false, bool NonQualified = false, int WinterReduction = 0, bool IntensiveFire = false);

/// <summary>The numbers and outcome of this use. Low Ammo is distinct from a weapon failure.</summary>
public sealed record VehicleBreakdownResolved(VehicleBreakdownNumbers Numbers, VehicleBreakdownOutcome Outcome)
{
    /// <summary>Low Ammo gained during this use remains recorded even when another qualifying DR also fails the weapon.</summary>
    public bool LowAmmoGained { get; init; } = Outcome == VehicleBreakdownOutcome.LowAmmo;

    public bool Failed => Outcome is VehicleBreakdownOutcome.Malfunction or VehicleBreakdownOutcome.Disabled;

    /// <summary>C8.9 (PDF p. 179): Low Ammo as well as weapon failure makes an otherwise absent special-ammunition attempt count as firing.</summary>
    public bool CountsAsFiredOnDepletionFailure => Outcome != VehicleBreakdownOutcome.None;
}

/// <summary>Recorded identity and current status of the actual Hero who proposes to repair an AAMG as a Rider (D3.7).</summary>
public sealed record VehicleRepairHeroFacts(string UnitId, bool Active, bool IsHero, bool SameSide, string? CarrierId, bool IsRider,
    bool Broken = false, bool Captured = false);

/// <summary>
/// New repair evidence: actual listed weapon existence, whether this identity is Secondary Armament, and the selected Hero Rider if any.
/// The caller gathers the per-weapon attempt and condition facts in WeaponFacts. A true legacy HeroRider flag is not identity evidence.
/// </summary>
public sealed record VehicleRepairUseFacts(VehicleRepairFacts WeaponFacts, bool WeaponExists, bool IsSecondaryArmament = false,
    VehicleRepairHeroFacts? Hero = null);
