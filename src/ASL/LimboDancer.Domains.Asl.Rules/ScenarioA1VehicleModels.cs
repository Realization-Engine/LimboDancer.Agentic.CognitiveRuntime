namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Passenger a vehicle sets up with (A2.52, D6.1; ruling R26.2), as the state finds it: its id, its side and OB group, whether it is a vehicle or
/// Personnel, and whether it is concealed or hidden.
/// </summary>
public sealed record PassengerSetupFacts(string Id, string Side, string? Group, bool Vehicle, bool Personnel, bool ConcealedOrHidden);

/// <summary>
/// The rule half of a vehicle's outright entry (rulings R11.1, R11.7, R11.9): its cost in half MP, ALL, the Bog DR with its causes, the terrain, the
/// road rate, and the Bog DR of a hedge taken in the hex left. Play adds the Location entered.
/// </summary>
public sealed record VehicleTerrainEntry(int HalfMp, bool All, int? BogDrm, IReadOnlyList<string> BogCauses, string Terrain, bool Road, int? HedgeBogDrm);

/// <summary>
/// One step a moving vehicle may try (rulings R11.1, R11.2, R11.7), in the order the search finds them: an outright entry of <see cref="To"/> from
/// <see cref="From"/>, or a VBM occupying <see cref="To"/> along its hexside with <see cref="Other"/>. Locations are indexes into the caller's table.
/// </summary>
public sealed record VehicleStepOption(int To, int From, int? Other)
{
    public bool Bypass => Other is not null;
}

/// <summary>
/// A fact reader for the search of a vehicle's steps (D2.11, D2.3, D2.32, D2.33; the pass 32 design, D4): Locations are indexes into the caller's
/// table, and a facing is a hexspine, 0 to 5 counterclockwise from east.
/// </summary>
public interface IVehicleStepFactReader
{
    /// <summary>The two ADJACENT Locations a VCA along a facing points at, in the map's order.</summary>
    public IReadOnlyList<int> VcaHexes(int location, int facing);

    /// <summary>The two ADJACENT hexes both of two ADJACENT hexes border, ground level.</summary>
    public IReadOnlyList<int> SharedNeighbors(int one, int two);

    /// <summary>The hexes at each end of the hexside two ADJACENT hexes share, for a vehicle facing along it: ahead and behind; nulls when it does not.</summary>
    public (int? Front, int? Rear) LaneEnds(int one, int two, int facing);

    /// <summary>Whether a hex is a woods or building obstacle a vehicle may Bypass (D2.3, D2.31).</summary>
    public bool Bypassable(int location);
}
