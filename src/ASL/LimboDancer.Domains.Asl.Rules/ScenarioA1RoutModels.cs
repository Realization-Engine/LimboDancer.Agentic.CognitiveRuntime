namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Known enemy unit as the rout reads it (A10.51, A10.533; pass 32.f), in the order the state lists them: its Location as an index into the
/// caller's table, whether it is armed (A10.5, A10.62), its conditions, its Normal Range (A10.532), and the range within which it may Interdict
/// (pass 35, task 35.4), its Normal Range when none is given.
/// </summary>
public sealed record RoutEnemyFacts(string Id, int Location, bool Armed, bool Broken, bool Melee, bool Vehicle, bool Cx, bool Pinned, bool Encircled, int NormalRange,
    int? InterdictionRange = null);

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
    /// within range. A reader that does not give it leaves the map's own Hindrance to decide.
    /// </summary>
    public RoutCoverFacts Cover(int enemyLocation, int location) => RoutCoverFacts.None;

    /// <summary>The distance in hexes between two Locations, or null when the map cannot give it.</summary>
    public int? Distance(int one, int two);

    /// <summary>Whether two Locations are ADJACENT (A.8).</summary>
    public bool Adjacent(int one, int two);

    /// <summary>The Infantry entry of a step between two Locations (A4.1), or why there is none.</summary>
    public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation);
}
