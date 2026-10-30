namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// B23.74 Factory (B23.74, B23.741, B23.742 pp. 139 and 140; B23.743 p. 140; the B23.74 claim, rulings RB23F.1 to RB23F.14): whether a building is a
/// Factory, its height as a LOS obstacle, who may occupy it, the TEM and Hindrance of an attack into it, and the cost of a step within or into it. A
/// Factory keys as a stone or wooden building (RB23F.4); this calculator decides where it differs. Every geometry fact (a LOS traced within the depiction,
/// a Factory hexside, the Factory hexes traced) is a map reading Play hands over: Maps measures, Rules decides.
/// </summary>
public static class ScenarioA1FactoryCalculator
{
    /// <summary>The prefix of the SSR token that makes a building a Factory: <c>factory:&lt;building&gt;</c>.</summary>
    public const string FactoryRule = "factory:";

    /// <summary>
    /// The building an SSR token <c>factory:&lt;building&gt;</c> makes a Factory (B23.74, p. 139: "A Factory exists only by SSR"), as the card names it
    /// (an area id or a hex, "X3"); null when the token is not a well-formed one.
    /// </summary>
    public static string? FactoryToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Split(':') switch
        {
            ["factory", { Length: > 0 } building] when !building.Any(char.IsWhiteSpace) => building,
            _ => null,
        };
    }

    /// <summary>Whether a building is a Factory (B23.74): its printed terrain is Factory terrain, or an SSR names it.</summary>
    public static bool IsFactory(bool printedFactoryTerrain, bool namedBySsr) => printedFactoryTerrain || namedBySsr;

    /// <summary>
    /// A Factory's height as a LOS obstacle in half levels (B23.74; RB23F.3): 2½ levels (5) with a printed stairwell, 1½ (3) without. Play hands it to Maps
    /// when it paints a Factory by SSR.
    /// </summary>
    public static int ObstacleHalfLevels(bool printedStairwell) => printedStairwell ? 5 : 3;

    /// <summary>
    /// Whether a unit may occupy a Location of a Factory (B23.74, B23.742; RB23F.2): only ground level, since a Factory has no upper level floors and no
    /// cellar; its rooftop only when an SSR puts rooftops in play (B23.8).
    /// </summary>
    public static bool OccupiableLevel(int level, bool rooftop, bool rooftopsBySsr) => rooftop ? rooftopsBySsr : level == 0;

    /// <summary>Why a unit may not occupy this Location of a Factory, or null (<see cref="OccupiableLevel"/>).</summary>
    public static string? OccupationRefusal(int level, bool rooftop, bool rooftopsBySsr) =>
        OccupiableLevel(level, rooftop, rooftopsBySsr) ? null
        : rooftop ? "play.factory-level: a Factory's rooftop is in play only by SSR (B23.8)"
        : level < 0 ? "play.factory-level: a Factory has no cellar (B23.41, B23.742)"
        : "play.factory-level: a Factory has no upper level floors; all its occupants are at ground level (B23.74)";

    /// <summary>
    /// Why a unit may not change levels within a Factory hex (B23.74; RB23F.7): it has no upper floor or cellar, and a climb to its rooftop is B23.87's
    /// (an Access Point at 2 or 3 MF), not built here.
    /// </summary>
    public static string LevelChangeRefusal(string at) =>
        $"play.factory-level: {at} is a Factory hex, with no upper level floor or cellar; a climb to its rooftop (B23.87) is not built (B23.74)";

    /// <summary>
    /// The non-rooftop building levels above a ground Location of a Factory, for B23.32's DRM against an Area Target Type hit: none, since a Factory has
    /// no upper level floors (B23.74), whatever levels the map gives a stairwell hex for B23.87 and B23.88.
    /// </summary>
    public static int NonRooftopLevelsAbove() => 0;

    /// <summary>The TEM of a building of this material (B23.741): +3 stone, +2 wooden.</summary>
    public static int BuildingTem(bool stone) => stone ? 3 : 2;

    /// <summary>
    /// The TEM an attack meets in a ground-level Factory Location (B23.741, B23.743, pp. 139 and 140; RB23F.6), in the rule's order: Indirect Fire meets the
    /// building's TEM, not evaluated at a Roofless Location (O5.45 is not in these pages); Residual FP the building's TEM when the mover entered across a
    /// non-building hexside or Bypasses, else +1; a rooftop firer +1 (B23.88); a firer in a rubbled hex of the Factory +1, even when the LOF leaves the
    /// depiction (B23.743); a firer inside +1 when the LOS is traced entirely within the depiction, else the building's TEM; a firer outside +1 when the LOS
    /// is over an adjacent rubble hex of the Factory and crosses the depiction while entering the target (B23.743), else the building's TEM. An unknown
    /// fact is not evaluated. A Fortified Factory (B23.9) is B23.9's row.
    /// </summary>
    public static FactoryTemVerdict Tem(FactoryTemFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var building = BuildingTem(facts.Stone);
        var buildingText = facts.Stone ? "+3, a stone building's TEM" : "+2, a wooden building's TEM";
        if (facts.IndirectFire)
        {
            return facts.Roofless
                ? new(null, false, "Indirect Fire at a Roofless Factory Location (O5.45) is not built")
                : new(building, false, $"{buildingText}: Indirect Fire meets the building's TEM (B23.741)");
        }

        if (facts.Residual)
        {
            return facts.ResidualEntry switch
            {
                null => new(null, false, "how the moving unit entered the Factory Location is not known"),
                FactoryResidualEntry.BuildingHexside => new(1, true, "+1, the Factory's TEM: the mover entered across a building hexside (B23.741)"),
                _ => new(building, false, $"{buildingText}: the mover entered across a non-building hexside or Bypasses (B23.741)"),
            };
        }

        return facts.Firer switch
        {
            null => new(null, false, "where the firer is relative to the Factory is not known"),
            FactoryFirerPlace.Rooftop => new(1, true, "+1, the Factory's TEM, from its rooftop (B23.741, B23.88)"),
            FactoryFirerPlace.Rubble => new(1, true, "+1, the Factory's TEM: fire from its rubble is fire from a Factory Location (B23.743)"),
            FactoryFirerPlace.Inside => facts.TracedWithinDepiction switch
            {
                null => new(null, false, "whether the LOS is traced entirely within the building depiction is not known"),
                true => new(1, true, "+1, the Factory's TEM: the fire is traced completely through the same building (B23.741)"),
                false => new(building, false, $"{buildingText}: the LOS is not traced entirely within the depiction (B23.741)"),
            },
            _ => facts.OverAdjacentRubbleEnteringAcrossDepiction switch
            {
                null => new(null, false, "whether the LOS is over the Factory's adjacent rubble and crosses the depiction entering the target is not known"),
                true => new(1, true, "+1: the LOS is over the Factory's adjacent rubble and crosses the depiction while entering the target (B23.743)"),
                false => new(building, false, $"{buildingText}: fire from outside the Factory (B23.741)"),
            },
        };
    }

    /// <summary>
    /// The Factory Hindrance of a LOS (B23.741): +1 for each Factory hex the LOS is traced through within the depiction, which the map counts without the
    /// Factory's rubble hexes (B23.743, B.4); none for a rooftop firer, or a firer outside, whose LOS no Factory hex hinders.
    /// </summary>
    public static int Hindrance(int factoryHexesTraced, FactoryFirerPlace firer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factoryHexesTraced);
        return firer is FactoryFirerPlace.Inside or FactoryFirerPlace.Rubble ? factoryHexesTraced : 0;
    }

    /// <summary>
    /// Whether a LOS or a placement is completely within one Factory (B23.741; RB23F.9): the fact A24.6 ("completely within a factory") and E3.8 read. It
    /// decides no weather.
    /// </summary>
    public static bool TracedWithinFactory(bool sameFactory, bool tracedWithinDepiction) => sameFactory && tracedWithinDepiction;

    /// <summary>
    /// The half MF of an Infantry step that touches a Factory (B23.742, B23.743; RB23F.10): 2 (one MF) from a Factory hex or its rubble into another hex
    /// of the same Factory across a Factory hexside; 2 (Open Ground) through a Vehicular-Sized Entrance entered from outside the Factory, not when it is
    /// Roofless; null otherwise, where the building's normal entry applies.
    /// </summary>
    public static int? InfantryHalfMf(FactoryStepFacts step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.ToFactory && step.SameFactory && (step.FromFactory || step.FromRubbleOfFactory) && step.AcrossFactoryHexside)
        {
            return 2;
        }

        return step.ToFactory && !step.SameFactory && step.VehicularEntrance && !step.EntranceRoofless ? 2 : null;
    }

    /// <summary>
    /// A vehicle's step that touches a Factory (B23.742, pp. 139 and 140; B23.41; RB23F.10), in the rule's order. By Bypass, or entering from outside with
    /// no usable Vehicular-Sized Entrance, it is not the Factory's case (B23.41's AFV entry stays its own row). Through a Vehicular-Sized Entrance whose hex
    /// its side controls, entering from outside or leaving to it: Open Ground cost. A fully-tracked AFV inside, to another hex of the same Factory:
    /// one-fourth of its MP allotment (FRU) with a Bog Check at +1. A fully-tracked AFV leaving without an Entrance: the building's entrance cost (half its
    /// printed MP allotment, B23.41), spent inside, with a Bog Check at +3 (+4 stone) and a rubble check before the next hex. Any other vehicle move
    /// between hexes of the same Factory, or out of it, is refused.
    /// </summary>
    public static FactoryVehicleStepVerdict VehicleStep(FactoryVehicleStepFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var step = facts.Step;
        var within = (step.FromFactory || step.FromRubbleOfFactory) && step.ToFactory && step.SameFactory;
        var entering = step.ToFactory && !step.SameFactory;
        var leaving = step.FromFactory && !step.SameFactory;
        if (facts.Bypass || (!within && !entering && !leaving))
        {
            return new(null, false, null, false, null);
        }

        if ((entering || leaving) && step.VehicularEntrance && step.EntranceFriendlyControlled)
        {
            return facts.MovementType is { } type && ScenarioA1ResultTables.VehicleTerrainHalfMp.TryGetValue((type, "open-ground"), out var open)
                ? new(open, false, null, false, null)
                : new(null, false, null, false, $"play.factory-vehicle: a {facts.MovementType ?? "unknown"} vehicle's Open Ground cost is not reviewed (B23.742)");
        }

        if (entering)
        {
            return new(null, false, null, false, null);
        }

        var fullyTrackedAfv = facts.Afv && facts.MovementType == "fully-tracked";
        if (within)
        {
            return fullyTrackedAfv
                ? new(2 * ((facts.MpAllotment + 3) / 4), true, BogDrm().Drm, false, null)
                : new(null, false, null, false, "play.factory-vehicle: only a fully-tracked AFV moves between hexes of the same Factory, other than by Bypass (B23.742)");
        }

        return fullyTrackedAfv
            ? new(facts.MpAllotment, true, facts.Stone ? 4 : 3, true, null)
            : new(null, false, null, false,
                "play.factory-vehicle: a vehicle leaves a Factory only through a Vehicular-Sized Entrance its side controls, or as a fully-tracked AFV (B23.742, B23.41)");
    }

    /// <summary>Whether a vehicle changing its VCA in this Location is subject to Bog (B23.742): in any Factory Location.</summary>
    public static bool VcaChangeBog(bool inFactory) => inFactory;

    /// <summary>The Factory DRM of a Bog Check made in a Factory (B23.742): +1.</summary>
    public static (string Cause, int Drm) BogDrm() => ("factory", 1);

    /// <summary>The TEM a unit in a Factory counts for Sniper Target Selection (B23.741, A14.21): +3 stone, +2 wooden.</summary>
    public static int SniperTem(bool stone) => BuildingTem(stone);

    /// <summary>
    /// Whether a Factory's outside wall divides two hexes into separate buildings (B23.74, B23.71): a wall on the hexside's terrain or on the hexside
    /// terrain is one. A Factory's interior walls do not divide it.
    /// </summary>
    public static bool DividedByFactoryWall(bool sideOutsideFactoryWall, bool hexsideOutsideFactoryWall) => sideOutsideFactoryWall || hexsideOutsideFactoryWall;
}
