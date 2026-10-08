namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A node of a charge's route (ruling R27.2): a Location, or an obstacle hex in Bypass with the hexside entered by (0 to 5) and the lane's key.</summary>
public readonly record struct ChargeNodeFacts(int At, int? Entered, string? Lane);

/// <summary>A first move of a charge's route: the Location entered, the Bypass lane's key when it enters Bypass, and its half MF.</summary>
public readonly record struct ChargeMoveFacts(int To, string? Lane, int HalfMf);

/// <summary>
/// A step a berserk stack may take, as the search finds it (A15.431; ruling R27.2): the Location charged, the least half MF of the step, whether it may be
/// taken as an ordinary step, and the Bypass lanes (their keys) it may be taken in. <see cref="ChargeStepFacts"/> is the same step with its Locations named.
/// </summary>
public sealed record ChargeSearchStep(int Target, int HalfMf, bool Plain, IReadOnlyList<string> Lanes);

/// <summary>What a Location holds that bars a charge into it (rulings R24.3, R11.16): a Gun's crew, an enemy vehicle, enemy Infantry.</summary>
public readonly record struct ChargeOccupantFacts(bool Crew, bool EnemyVehicle, bool EnemyInfantry);

/// <summary>
/// A fact reader for the berserk charge's search (A15.43, A15.431; the pass 32 design, D4): Locations are indexes into the caller's table, hexsides 0 to 5
/// in the map's order, Bypass lanes by their key text. The map reads, the entry costs, and the Bypass costs are Play's.
/// </summary>
public interface IChargeFactReader
{
    /// <summary>The Location's text, for a refusal and for the order of equal routes.</summary>
    public string Name(int location);

    /// <summary>The Location's level.</summary>
    public int Level(int location);

    /// <summary>The Locations a charge may step to: the ADJACENT hexes at ground level and at the mover's upper level, and the levels one above or below.</summary>
    public IEnumerable<int> Neighbors(int location);

    /// <summary>Whether the card's playable area holds the Location (A2.1).</summary>
    public bool Playable(int location);

    /// <summary>The half MF of a charge step between two Locations as the movement rules decide it, or null when the entry is not reviewed; 16 for all the MF.</summary>
    public int? EntryCost(int fromLocation, int toLocation);

    /// <summary>The Location across a hexside of a hex, or null.</summary>
    public int? Across(int location, int side);

    /// <summary>The hexside of a hex toward an ADJACENT one, or null.</summary>
    public int? SideToward(int fromLocation, int toLocation);

    /// <summary>Whether the map reads the Location.</summary>
    public bool Reads(int location);

    /// <summary>The half MF of a Bypass of an obstacle hex entered by a hexside along a lane, from a Location, or null when the Bypass is not allowed.</summary>
    public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation);

    /// <summary>A lane's key: its hexsides' names, lowercase, comma-separated.</summary>
    public string LaneKey(IReadOnlyList<int> lane);

    /// <summary>The hexsides a lane's key names.</summary>
    public IReadOnlyList<int> Lane(string key);

    /// <summary>Whether the LOS between two Locations is clear.</summary>
    public bool LosClear(int fromLocation, int toLocation);

    /// <summary>The distance in hexes between two Locations, or null when the map cannot give it.</summary>
    public int? Distance(int one, int two);

    /// <summary>What a Location holds that may bar a charge into it.</summary>
    public ChargeOccupantFacts Occupants(int location);
}
