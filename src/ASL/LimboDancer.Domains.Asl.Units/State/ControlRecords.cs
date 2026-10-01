using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>building-mopped-up</c>: Mopping Up declared in the PFPh (A12.153; pass 24 of the Card Play and Map Studio Redesign Plan, ruling R24.2) by
/// <paramref name="Units"/> of <paramref name="Side"/> in <paramref name="Building"/>, a building of the card. The units become TI. When no concealed
/// enemy unit is left there, <paramref name="Secured"/> lists every Location of the building the side now Controls (A26.11); null when it is not secured.
/// A building is Mopped Up once per Player Turn.
/// </summary>
public sealed record BuildingMoppedUp(string Building, string Side, IReadOnlyList<string> Units, IReadOnlyList<BoardLocation>? Secured) : EventPayload;

/// <summary>A building secured by Mopping Up (A12.153, A26.11; ruling R24.2): the side that secured it and the Locations it Controls from then on.</summary>
public sealed record SecuredBuilding(string Building, string Side, IReadOnlyList<BoardLocation> Locations);
