namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Known enemy unit as the rout reads it (A10.51, A10.533; pass 32.f), in the order the state lists them: its Location as an index into the
/// caller's table, whether it is armed (A10.5, A10.62), its conditions, and its Normal Range (A10.532).
/// </summary>
public sealed record RoutEnemyFacts(string Id, int Location, bool Armed, bool Broken, bool Melee, bool Vehicle, bool Cx, bool Pinned, bool Encircled, int NormalRange);

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

    /// <summary>The distance in hexes between two Locations, or null when the map cannot give it.</summary>
    public int? Distance(int one, int two);

    /// <summary>Whether two Locations are ADJACENT (A.8).</summary>
    public bool Adjacent(int one, int two);

    /// <summary>The Infantry entry of a step between two Locations (A4.1), or why there is none.</summary>
    public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation);
}
