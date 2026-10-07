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
