namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The four purposes A10.531 (p. 67) defines Open Ground for: rout determination (A10.5), Dash (A4.63), concealment gain and loss (A12.14), and
/// Interdiction (A10.53). For concealment only what is in the hex counts, with a road-rate mover's view along the road (ruling RA10.2: A12.14, p. 78,
/// "The presence of a LOS Hindrance or beneficial defensive DRM ... along the LOS to a concealed unit does not prevent loss of its concealment");
/// the others take the full test of each enemy unit's view.
/// </summary>
public enum OpenGroundPurpose
{
    Rout, Dash, Interdiction, Concealment
}

/// <summary>
/// The Location as every enemy unit sees it (A10.531, p. 67; ruling RA10.1): its terrain key, as the Fire package names it (a road hex reads
/// "open-ground"); the scenario month, null when the game names none; whether SMOKE is there (A4.6, p. 51); and whether the unit entered a shellhole,
/// entrenchment, or pillbox at its protective cost, began the phase in one, is Entrenched, or mans an Emplaced Gun (the A10.531 EX; ruling RA10.8:
/// false wherever those do not exist, which is every map today).
/// </summary>
public sealed record OpenGroundLocationFacts(string? TerrainKey, int? Month, bool Smoke, bool ProtectedEntry);

/// <summary>
/// One enemy unit's view of the Location (A10.531, p. 67; ruling RA10.1): whether its LOS is clear, the map's Hindrance on it, the cover facts the
/// rout reads for that LOS (<see cref="RoutCoverFacts"/>: other Hindrances, a wall or hedge TEM on the hexside crossed, Height Advantage, a wreck or
/// AFV in the Location), and, for a mover that entered at the road rate, the terrain that unit sees it in
/// (<see cref="ScenarioA1Definitions.RoadRateTargetTerrain"/>: "open-ground" when its LOS reaches the road without crossing the hex's other terrain,
/// A4.132, p. 48; B13.31; B14.6); null reads the Location's own terrain.
/// </summary>
public sealed record OpenGroundViewFacts(bool LosClear, int LosHindrance, RoutCoverFacts Cover, string? TerrainAsSeen = null);
