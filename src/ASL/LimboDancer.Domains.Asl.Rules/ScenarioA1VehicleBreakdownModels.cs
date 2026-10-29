namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A vehicle's weapon, for its breakdown (D3.7): the MA, an MG named apart from the SA (BMG, CMG, AAMG), or a weapon of the SA column (D1.34).</summary>
public enum VehicleBreakdownWeapon
{
    MainArmament, BowMg, CoaxialMg, AntiAircraftMg, SecondaryArmament
}

/// <summary>The Original DR a breakdown is read on (D3.7, D3.71): a To Hit DR, an IFT DR, or a CC DR.</summary>
public enum VehicleBreakdownDr
{
    ToHit, Ift, CloseCombat
}

/// <summary>What an Original DR does to a vehicle's weapon (D3.7, D3.71; ruling RD3.3).</summary>
public enum VehicleBreakdownOutcome
{
    None, Malfunction, LowAmmo, Disabled
}

/// <summary>
/// The facts of a vehicle weapon's breakdown numbers this use (D3.7, D3.71, A.11, A19.131; ruling RD3.2): the weapon; its printed B# and X# (null when
/// none is printed); whether the B# is circled; whether a Vehicle Note extends them beyond the MA; Low Ammo; an Ammunition Shortage; and the A.11
/// lowerings this use, of the B# only (C5.62) and of the B# and X# (A21.11, E3.741, A19.32).
/// </summary>
public sealed record BreakdownNumberFacts(VehicleBreakdownWeapon Weapon, int? PrintedB, int? PrintedX, bool Circled, bool NoteExtends, bool LowAmmo,
    bool AmmunitionShortage, int LowersB = 0, int LowersBX = 0);

/// <summary>A vehicle weapon's breakdown numbers this use (ruling RD3.2): its B#, its X# (null when it has none), whether its B# is circled, and Low Ammo.</summary>
public sealed record VehicleBreakdownNumbers(int BNumber, int? XNumber, bool Circled, bool LowAmmo);

/// <summary>
/// The facts of a vehicle weapon's repair (D3.7; ruling RD3.4): the vehicle; the weapon (<c>ma</c>, <c>bmg</c>, <c>cmg</c>, or <c>aamg</c>); malfunctioned;
/// Disabled; whether its crew is impaired (<see cref="ScenarioA1RallyRules.VehicleCrewImpaired"/>); whether this weapon was repaired this RPh; and, for an
/// AAMG, whether the crew is CE and whether a Hero Rider makes the attempt.
/// </summary>
public sealed record VehicleRepairFacts(string VehicleId, string Weapon, bool Malfunctioned, bool Disabled, bool CrewImpaired, bool RepairedThisPhase,
    bool CrewExposed = false, bool HeroRider = false);
