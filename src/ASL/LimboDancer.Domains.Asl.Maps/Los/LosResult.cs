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
/// The result of an LOS read, as VASL's <c>LOSResult</c> reports it: whether LOS is blocked, where, the range
/// (<c>Map.range</c>), the hindrance total (<c>LOSResult.getHindrance</c>: the floor of the sum of the largest map
/// hindrance at each range), and VASL's reason text when blocked. An unsupported result carries the range and the
/// name of the rule that is not reproduced, and no answer.
/// </summary>
public sealed record LosResult(LosStatus Status, bool? IsBlocked, int Range, int Hindrance, LosBlockedAt? BlockedAt, string Reason)
{
    /// <summary>Whether the result answers the question: Clear or Blocked, definitive or not.</summary>
    public bool IsAnswered => IsBlocked is not null;

    internal static LosResult Unsupported(int range, string rule) => new(LosStatus.Unsupported, null, range, 0, null, rule);
}

/// <summary>
/// The names an unsupported LOS result gives for the VASL rules this step does not reproduce (LOS Design, section 3).
/// The walk stops with one of them the moment VASL would need the rule.
/// </summary>
public static class LosUnsupportedRule
{
    public const string Bridge = "Bridges and tunnels";

    /// <summary>Factories, their rooftops and walls, and roofless or gutted buildings; rooftops of other buildings are reproduced.</summary>
    public const string Factory = "Factories and roofless buildings (B23.87)";

    /// <summary>A location below level 0 that is not a cellar; cellars themselves are reproduced.</summary>
    public const string Cellar = "Cellars (O6.3)";

    public const string Entrenchment = "Entrenchments (B27.2)";

    public const string RowhouseWall = "Rowhouse and factory walls (B23.71)";

    public const string Bocage = "Bocage (B9.52)";

    public const string PartialOrchard = "Partial orchards (B14.2)";

    public const string RailroadEmbankment = "Railroad embankments";

    public const string Hillock = "Hillocks (F6.4)";

    public const string Rubble = "Rubble";

    public const string Deir = "Deir (F4.4)";

    public const string Slope = "Slopes (F2.3)";

    public const string OrchardOutOfSeason = "Out-of-season orchards";

    public const string SandDune = "Sand dunes";

    public const string VolgaPier = "Volga piers";

    public const string UnknownTerrain = "Terrain code not in the catalog";

    public const string MissingLocationTerrain = "Location without terrain";

    /// <summary>A path on which VASL's own code fails with an exception, so it gives no answer to reproduce.</summary>
    public const string VaslFails = "A line on which VASL's LOS fails";
}
