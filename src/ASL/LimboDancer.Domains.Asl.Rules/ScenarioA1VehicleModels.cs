namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Passenger a vehicle sets up with (A2.52, D6.1; ruling R26.2), as the state finds it: its id, its side and OB group, whether it is a vehicle or
/// Personnel, and whether it is concealed or hidden.
/// </summary>
public sealed record PassengerSetupFacts(string Id, string Side, string? Group, bool Vehicle, bool Personnel, bool ConcealedOrHidden);
