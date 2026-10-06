namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// One Infantry step's cost in half MF and what it enters (pass 32.b, from Play's planner): the terrain key, whether it crossed a road hexside at
/// the road rate into a hex with no SMOKE, burning wreck, or rubble (the Road Bonus, B3.4), whether it costs the unit's whole MF allotment (marsh,
/// B16.4), and whether only a Minimum Move may make it (marsh from a lower elevation, B16.4).
/// </summary>
public sealed record InfantryEntry(int HalfMf, string Terrain, bool RoadRate, bool AllMf, bool MinimumMoveOnly, bool LevelChange);

/// <summary>
/// The hexside an Infantry step crosses, as the map reads it: a cliff or Continuous Slope, the hexside terrain's name (a wall, a hedge, or another),
/// whether the hexside carries a road, and the name of the terrain on it (the building two hexes of one building share).
/// </summary>
public sealed record CrossedHexsideFacts(bool Cliff, bool Slope, string? HexsideTerrain, bool Road, string? Terrain);

/// <summary>One Location of an Infantry step as the map reads it: the terrain key the packages admit (null when none), the terrain's own name, the hex's base level, and its stairwell.</summary>
public sealed record StepLocationFacts(string? TerrainKey, string? TerrainName, int BaseLevel, bool Stairway);

/// <summary>
/// The facts of one Infantry step from one Location to another (rulings R10.1 to R10.3, R10.5). <paramref name="From"/> and <paramref name="To"/> are
/// null where the map cannot read the Location; <paramref name="Crossed"/> is the hexside between the two hexes, null when they are not adjacent or
/// the map cannot give it. The texts are the Locations as the planner names them in a refusal.
/// </summary>
public sealed record InfantryStepFacts(
    bool FromIsHexside,
    bool ToIsHexside,
    bool SameHex,
    int FromLevel,
    int ToLevel,
    string FromText,
    string ToText,
    string FromHexText,
    StepLocationFacts? From,
    StepLocationFacts? To,
    bool Adjacent,
    CrossedHexsideFacts? Crossed,
    bool Night,
    int? Month);

/// <summary>One hexside a Bypass runs along, as the map reads the target hex's hexside: its direction as an int and its lower-case name, whether it was read, and the names of its terrain and hexside terrain and whether it carries a road.</summary>
public sealed record BypassedHexsideFacts(int Side, string SideText, bool Read, string? Terrain, string? HexsideTerrain, bool Road);

/// <summary>
/// The facts of a Bypass step into a woods or building hex (A4.3, A4.31; ruling R10.7) across the target's hexside <paramref name="Crossed"/>:
/// the hexsides named, the target Location and hex as the map reads them, who is in the hex, the base level of the hex left (or the mirror hex
/// beyond the map edge), and the wall or hedge on the hexside crossed.
/// </summary>
public sealed record BypassStepFacts(
    IReadOnlyList<BypassedHexsideFacts> Bypass,
    int Crossed,
    int ToLevel,
    string ToText,
    bool TargetReadable,
    string? TargetTerrainKey,
    int TargetBaseLevel,
    IReadOnlyList<string?> TargetHexsideTerrains,
    bool FriendlyUnitsAtTarget,
    bool ArmedKnownEnemyAtTarget,
    int FromBaseLevel,
    string? EntryWall,
    int? Month);

/// <summary>
/// The facts of a moving stack's step (rulings R10.1 to R10.3, R10.7): the moving stack's Bypass as the state has it, the movers against it, the
/// Locations named, and the reads of the Location left. <paramref name="SideTowardFrom"/> is the target hex's hexside toward the origin, null when
/// they are not adjacent; <paramref name="EntryWall"/> the wall or hedge on the origin's hexside toward the target.
/// </summary>
public sealed record MoveEntryFacts(
    bool StackInBypass,
    IReadOnlyList<int> Lane,
    bool CurrentAtFrom,
    bool MoversAllInCurrent,
    bool CurrentMemberLeftBehind,
    bool ToIsFrom,
    bool EnemyAtFrom,
    bool BypassGiven,
    string? ObstacleTerrainKey,
    int FromLevel,
    bool FromReadable,
    bool ToIsGround,
    int? SideTowardFrom,
    string? EntryWall,
    string ToText);

/// <summary>
/// The verdict on a moving stack's step: the first refusal; the cost of occupying the Bypassed obstacle; or which step the caller reads next, the
/// ordinary Infantry step from the origin to the target or a Bypass step into the target.
/// </summary>
public sealed record MoveEntryVerdict(string? Refusal, InfantryEntry? Occupy, bool OrdinaryStep, bool BypassStep);

/// <summary>The night and weather half MF of an Infantry step and whether the road rate survives them (E1.51, E3.54, E3.6, E3.64, E3.723, E3.733), read by the caller for a terrain, a road crossing, and a rise.</summary>
public delegate (int HalfMf, bool RoadRate) InfantryWeatherRead(string terrain, bool road, int rise);
