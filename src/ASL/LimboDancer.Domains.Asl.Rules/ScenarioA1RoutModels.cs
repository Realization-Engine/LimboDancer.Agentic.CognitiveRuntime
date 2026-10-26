namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Known enemy unit as the rout reads it (A10.51, A10.533; pass 32.f), in the order the state lists them: its Location as an index into the
/// caller's table, whether it is armed (A10.5, A10.62), its conditions, its Normal Range (A10.532), and the range within which it may Interdict
/// (pass 35, task 35.4), its Normal Range when none is given.
/// </summary>
public sealed record RoutEnemyFacts(string Id, int Location, bool Armed, bool Broken, bool Melee, bool Vehicle, bool Cx, bool Pinned, bool Encircled, int NormalRange,
    int? InterdictionRange = null)
{
    /// <summary>
    /// The weapons that Interdict only through a Covered Arc (A10.532; pass 70, ruling R70.14): a vehicle's BMG, CMG, and MA, and a Gun the unit mans; null
    /// for none. A vehicle Interdicts by these alone, and by its <see cref="InterdictionRange"/>.
    /// </summary>
    public IReadOnlyList<RoutArcWeapon>? ArcWeapons
    {
        get; init;
    }

    /// <summary>
    /// Whether the unit is Unarmed Personnel (A20.5; pass 70, ruling R70.43): read by the Voluntary Break alone, whose LOS clause asks for an armed enemy and
    /// whose ADJACENT clause does not (A10.41). Null is false; the rout reads <see cref="RoutEnemyFacts.Armed"/> as before.
    /// </summary>
    public bool? Unarmed
    {
        get; init;
    }
}

/// <summary>
/// A weapon that Interdicts only within its Covered Arc (A10.532, p. 67; pass 70, ruling R70.14): the range it Interdicts within (its Normal Range, at most
/// 16 hexes) and the hexspine facing (0 to 5) of the arc it fires through now: the VCA, the TCA, or a Gun's CA. One that would have to change its CA to
/// fire would take a positive DRM, so it does not Interdict there.
/// </summary>
public sealed record RoutArcWeapon(int Range, int Facing);

/// <summary>
/// A vehicle as the rout reads its weapons (A10.5, A10.532; D1.81 to D1.83, D5.2, D5.3; pass 70, ruling R70.14): whether it is Abandoned, in Motion,
/// Stunned, or Shocked; unarmored or open-topped; whether its crew is CE; its hull (VCA) and turret (TCA) facings; which of its BMG, CMG, AAMG, MA MG, and MA
/// Gun work; the MA Gun's printed maximum range, if any; and its MA Type.
/// </summary>
public sealed record RoutVehicleFacts(bool Abandoned, bool Motion, bool Stunned, bool Shocked, bool Unarmored, bool OpenTopped, bool CrewExposed, int? Hull, int? Turret,
    bool Bmg, bool Cmg, bool Aamg, bool MainMg, bool MainGun, int? MainGunRange, string? MaType);

/// <summary>
/// What keeps one enemy unit from applying the FFMO DRM to a Location (A10.531; pass 35, task 35.4), beyond the LOS itself: the map Hindrance along
/// the LOS as fire counts it by terrain and season, or null when fire cannot attribute it and the map's own total stands; the Hindrance of vehicles,
/// wrecks, and SMOKE along the LOS (D9.4, A24.2, B25.2); the wall or hedge TEM of the hexside the LOS crosses (B9.3); Height Advantage over that
/// enemy (B10.31); and the cover of a wreck or AFV in the Location (D9.3).
/// </summary>
public sealed record RoutCoverFacts(int? MapHindrance, int OtherHindrance, bool HexsideTem, bool HeightAdvantage, bool InHexCover)
{
    /// <summary>No cover read: the map's own Hindrance total decides alone, as before pass 35.</summary>
    public static RoutCoverFacts None { get; } = new(null, 0, false, false, false);
}

/// <summary>A Location as the rout reads it: the terrain key the map gives it, and whether SMOKE is there.</summary>
public sealed record RoutLocationFacts(string? TerrainKey, bool Smoke);

/// <summary>A LOS between two Locations as the rout reads it: whether it is clear, its Hindrance, and its range.</summary>
public sealed record RoutLosFacts(bool Clear, int Hindrance, int Range);

/// <summary>
/// What happens when a rout step enters a Location holding enemy units the routing side does not know (A10.533; pass 35, task 35.4): whether the
/// routing unit is repulsed; the hidden units that first go beneath a "?"; the real units among which one loses its "?", by Random Selection when
/// there are several; and the Dummies removed when no real unit is there and the rout goes on.
/// </summary>
public sealed record RoutRepulseVerdict(bool Repulsed, IReadOnlyList<string> ToConceal, IReadOnlyList<string> Pool, bool NeedsSelection, IReadOnlyList<string> Dummies);

/// <summary>
/// The half MF a rout step costs (A10.5, A7.7), ALL when the entry takes every MF, or why the step is not allowed.
/// </summary>
public sealed record RoutStepCost(int? HalfMf, bool AllMf, string? Reason);

/// <summary>What an Interdiction result does to the routing unit (A10.53, A10.31): it routs on, is pinned, is Casualty Reduced, or is eliminated.</summary>
public enum InterdictionOutcome
{
    Passed,
    Pinned,
    Reduced,
    Eliminated,
}

/// <summary>What Casualty Reduction does to a unit (A7.302): a squad becomes its HS, an unwounded leader or hero is wounded, anything else is eliminated.</summary>
public enum CasualtyOutcome
{
    Reduced,
    Wounded,
    Eliminated,
}

/// <summary>
/// A fact reader for the rout's search and scans (the pass 32 design, D4): Locations are indexes into the caller's table, which grows as the
/// reader names a neighbor. Every read is a map or state read made in Play; the LOS keeps its cache there.
/// </summary>
public interface IRoutFactReader
{
    /// <summary>The Location's text, for a refusal.</summary>
    public string Name(int location);

    /// <summary>
    /// The Locations one rout step may reach from a Location, in the order the map enumerates them: the ADJACENT hexes at ground level, from an upper
    /// level that level of the ADJACENT hexes, and the levels above and below in the same hex (A10.5, B23.4, B23.421); <see cref="Entry"/> decides which are legal.
    /// </summary>
    public IEnumerable<int> Neighbors(int location);

    /// <summary>Whether the card's playable area holds the Location (A2.1; ruling R20.6).</summary>
    public bool Playable(int location);

    /// <summary>The Location's terrain and SMOKE, or null when the map does not read it.</summary>
    public RoutLocationFacts? Location(int location);

    /// <summary>The LOS from one Location to another, or null when the map cannot give it.</summary>
    public RoutLosFacts? Los(int fromLocation, int toLocation);

    /// <summary>
    /// What keeps the enemy unit in one Location from applying the FFMO DRM to another (A10.531; pass 35, task 35.4), read only for a clear LOS
    /// within range. A reader that does not give it leaves the map's own Hindrance to decide. The Location the routing unit steps in from, when it
    /// is entering, lets Height Advantage be read as B1.14 and B10.31 read it for a unit crossing a Crest Line.
    /// </summary>
    public RoutCoverFacts Cover(int enemyLocation, int location, int? steppedFrom = null) => RoutCoverFacts.None;

    /// <summary>The distance in hexes between two Locations, or null when the map cannot give it.</summary>
    public int? Distance(int one, int two);

    /// <summary>Whether two Locations are ADJACENT (A.8).</summary>
    public bool Adjacent(int one, int two);

    /// <summary>
    /// The bearing in degrees from one Location to another, which a Covered Arc is read against (C3.2, A10.532; pass 70, ruling R70.14), or null when the map
    /// cannot give it; then no weapon held to a Covered Arc Interdicts.
    /// </summary>
    public double? Bearing(int fromLocation, int toLocation) => null;

    /// <summary>
    /// Whether two Locations lie in the same building (A10.51's EXC, p. 67; pass 70, ruling R70.13): hexes joined across hexsides of building terrain, a
    /// Rowhouse wall dividing them. A reader that does not give it reads none as the same building.
    /// </summary>
    public bool SameBuilding(int one, int two) => false;

    /// <summary>The Infantry entry of a step between two Locations (A4.1), or why there is none.</summary>
    public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation);
}
