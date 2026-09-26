using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>The status of an LOS read (LOS Design, section 5).</summary>
public enum LosStatus
{
    /// <summary>LOS exists, as VASL finds it, on a board whose terrain is definitive.</summary>
    Clear,

    /// <summary>LOS is blocked, as VASL finds it, on a board whose terrain is definitive.</summary>
    Blocked,

    /// <summary>The line needs a rule that is not reproduced yet; <see cref="LosResult.Reason"/> names it. Never a guess.</summary>
    Unsupported,

    /// <summary>
    /// An answer on a board that is not Verified or AuthoredValid (ASL-MAP-044): <see cref="LosResult.IsBlocked"/> holds
    /// it, but it is not definitive.
    /// </summary>
    Nondefinitive,
}

/// <summary>
/// Where LOS is first blocked: VASL's blocking point on the map's grid and the hex VASL's <c>Map.gridToHex</c> gives
/// that point, named board-relative by the board that owns it. The hex is null where VASL finds none.
/// </summary>
public sealed record LosBlockedAt(GridPoint Point, BoardRef? Board, HexName? Hex)
{
    public override string ToString() => Board is null || Hex is null ? $"({Point.X}, {Point.Y})" : $"{Board}:{Hex} ({Point.X}, {Point.Y})";
}

/// <summary>
/// One entry of the hindrance breakdown: a range from the source hex and the largest map hindrance met at that range,
/// as VASL's <c>Map.addHindranceHex</c> values it (1 for most hindrances, 2 for light woods and roofless hexes, 0.5
/// for light grain and in-season rice paddies).
/// </summary>
public sealed record LosHindrance(int Range, double Value);

/// <summary>
/// Where LOS first meets a map hindrance (<c>LOSResult.firstHindranceAt</c>): the point on the map's grid and the hex
/// VASL's <c>Map.gridToHex</c> gives that point, named board-relative by the board that owns it. The hex is null where
/// VASL finds none.
/// </summary>
public sealed record LosHindranceAt(GridPoint Point, BoardRef? Board, HexName? Hex)
{
    public override string ToString() => Board is null || Hex is null ? $"({Point.X}, {Point.Y})" : $"{Board}:{Hex} ({Point.X}, {Point.Y})";
}

/// <summary>
/// The result of an LOS read, as VASL's <c>LOSResult</c> reports it: whether LOS is blocked, where, the range
/// (<c>Map.range</c>), the hindrance total (<c>LOSResult.getHindrance</c>: the floor of the sum of the largest map
/// hindrance at each range), and VASL's reason text when blocked. An unsupported result carries the range and the
/// name of the rule that is not reproduced, and no answer.
/// </summary>
public sealed record LosResult(LosStatus Status, bool? IsBlocked, int Range, int Hindrance, LosBlockedAt? BlockedAt, string Reason)
{
    /// <summary>Whether the result answers the question: Clear or Blocked, definitive or not.</summary>
    public bool IsAnswered => IsBlocked is not null;

    /// <summary>
    /// The hindrance breakdown (<c>LOSResult.mapHindrances</c>, kept by <c>addMapHindrance</c>): the largest map
    /// hindrance at each range from the source hex, in range order. <see cref="Hindrance"/> is the floor of their sum.
    /// </summary>
    public IReadOnlyList<LosHindrance> Hindrances
    {
        get;
        init;
    } = [];

    /// <summary>Where the LOS first met a map hindrance (<c>LOSResult.setFirstHindrance</c>), or null when it met none.</summary>
    public LosHindranceAt? FirstHindranceAt
    {
        get;
        init;
    }

    public bool Equals(LosResult? other) =>
        other is not null && Status == other.Status && IsBlocked == other.IsBlocked && Range == other.Range && Hindrance == other.Hindrance
        && Equals(BlockedAt, other.BlockedAt) && Reason == other.Reason && Hindrances.SequenceEqual(other.Hindrances)
        && Equals(FirstHindranceAt, other.FirstHindranceAt);

    public override int GetHashCode() => HashCode.Combine(Status, IsBlocked, Range, Hindrance, BlockedAt, Reason, Hindrances.Count, FirstHindranceAt);

    internal static LosResult Unsupported(int range, string rule) => new(LosStatus.Unsupported, null, range, 0, null, rule);
}

/// <summary>
/// Which point of a hexside location an LOS starts or ends at (<c>Map.LOS</c>'s <c>useAux</c> flags): the LOS point,
/// the hexside's counterclockwise vertex, or the auxiliary point, its clockwise vertex. Center locations have one point.
/// </summary>
public enum LosAim
{
    LosPoint,
    AuxiliaryPoint,
}

/// <summary>
/// The names an unsupported LOS result gives for the VASL rules this step does not reproduce (LOS Design, section 3).
/// The walk stops with one of them the moment VASL would need the rule.
/// </summary>
public static class LosUnsupportedRule
{
    /// <summary>A hex whose center is a bridge, in the depression rule when an end is in a depression.</summary>
    public const string BridgeInDepression = "Bridge hexes in the depression rule (A6.3)";

    /// <summary>
    /// A factory blind-hex check along a hexside, which no fixture reaches. Factory rooftop LOS is reproduced.
    /// </summary>
    public const string FactoryRooftop = "Factory rooftop and hexside cases (B23.87)";

    /// <summary>Interior factory walls and breaches; rowhouse walls are reproduced.</summary>
    public const string InteriorFactoryWall = "Interior factory walls and breaches (B23.71, O5.31)";

    /// <summary>Bocage that makes a blind hex (B9.52), which no fixture reaches; bocage that blocks is reproduced.</summary>
    public const string BocageBlindHex = "Bocage blind hexes (B9.52)";

    /// <summary>
    /// The hillock outcomes no fixture reaches: a hillock summit, hillocks between a hillock source and its target, a
    /// second wall or hedge, and a second rubble hex (F6.4). Other hillock LOS is reproduced.
    /// </summary>
    public const string HillockCases = "Hillock summits and repeated crossings (F6.4)";

    /// <summary>
    /// The slope outcomes no fixture reaches: an up-slope end that sees over hexside terrain, or past a hindrance higher
    /// than both ends (F2.3). Other slope LOS is reproduced.
    /// </summary>
    public const string SlopeCases = "Slopes over hexside terrain and hindrances (F2.3)";

    /// <summary>
    /// The out-of-season orchard outcomes no fixture reaches: an orchard hex whose ground is as high as the higher end,
    /// and an orchard between the ends' levels in the blind hex rule. Other out-of-season orchard LOS is reproduced.
    /// </summary>
    public const string OrchardOutOfSeasonCases = "Out-of-season orchard height and blind hex cases";

    public const string Deir = "Deir (F4.4)";

    public const string SandDune = "Sand dunes";

    public const string VolgaPier = "Volga piers";

    public const string UnknownTerrain = "Terrain code not in the catalog";

    public const string MissingLocationTerrain = "Location without terrain";

    /// <summary>A path on which VASL's own code fails with an exception, so it gives no answer to reproduce.</summary>
    public const string VaslFails = "A line on which VASL's LOS fails";
}
