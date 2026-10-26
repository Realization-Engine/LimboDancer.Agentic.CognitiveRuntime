namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Carriers (D6.8 to D6.84, pp. 206, 207; pass 65, ruling R65.18): "A Carrier is treated as a halftrack except as stated otherwise below." It is
/// never BU and its crew, HS, and Passengers are always CE (D6.84); its inherent crew is a 2-4-7 HS (D6.82); it carries four Passenger PP and eight
/// Rider PP (D6.81); and its inherent HS is worth two VP (A26.213, p. 100). Play reads the vehicle's class and armament and writes the events.
/// The HS re-occupying its Carrier, with the MG as D6.631 has it (D6.83), is increment 2g's re-crewing; a Carrier HS's recombining without a TC or a
/// leader (D6.82) is pass 70's.
/// </summary>
public static class ScenarioA1CarrierRules
{
    /// <summary>The vehicle class of a Carrier (catalog 1.18.0, <c>asl:vehicle-class</c>; ruling R65.3).</summary>
    public const string VehicleClass = "carrier";

    /// <summary>Whether a vehicle class is a Carrier's.</summary>
    public static bool IsCarrier(string? vehicleClass) => vehicleClass == VehicleClass;

    /// <summary>
    /// D6.84 (p. 206): "A Carrier is never considered BU." Why a BU counter may not be placed on a vehicle, or null: a Carrier's crew never buttons up;
    /// it may still be offered as CE, which it already is.
    /// </summary>
    public static string? ButtonUpBar(string id, string? vehicleClass, bool buttonedUp) =>
        buttonedUp && IsCarrier(vehicleClass) ? $"play.button-up: {id} is a Carrier, which is never BU; its crew and Passengers are always CE (D6.84)" : null;

    /// <summary>D6.84: why a vehicle may not set up BU, or null: a Carrier sets up CE.</summary>
    public static string? SetupBar(string id, string? vehicleClass, bool buttonedUp) =>
        buttonedUp && IsCarrier(vehicleClass) ? $"play.setup-carrier: {id} is a Carrier, which is never BU, so it sets up CE (D6.84)" : null;

    /// <summary>
    /// D6.84 (pp. 206, 207): a Carrier's "inherent HS/Crew/Passengers are always considered CE and Vulnerable ... [EXC: they remain CE if stunned or
    /// shocked, and are subject to the Collateral Attack that may result from that attack]". Whether a vehicle's crew is CE: always for a Carrier,
    /// otherwise as its markers say.
    /// </summary>
    public static bool CrewExposed(string? vehicleClass, bool exposedOtherwise) => IsCarrier(vehicleClass) || exposedOtherwise;

    /// <summary>
    /// D6.81 (p. 206): "A Carrier without a mortar, FT, or 6 FP AAMG listed as MA on its counter has a Passenger capacity of four PP". A vehicle's
    /// Passenger capacity before any towed Gun: a Carrier's four, or none with such a MA; any other vehicle's as printed. A Carrier's MA with a caliber
    /// is its mortar (the 2-in. and 3-in. mortar Carriers, British Vehicle Notes 66 and 67, p. 419).
    /// </summary>
    public static int? PassengerCapacity(string? vehicleClass, int? printed, string? mainArmament, int? mainArmamentCaliber, int? antiAircraftMg) =>
        !IsCarrier(vehicleClass) ? printed
            : mainArmament == "ft" || mainArmamentCaliber is not null || (mainArmament == "aamg" && antiAircraftMg == 6) ? null
            : 4;

    /// <summary>
    /// D6.82 (p. 206): a Carrier's "inherent crews are really 2-4-7 HS ... [EXC: those Carriers armed with a FT or 76mm mortar or specified as towing
    /// ordnance, have inherent crews]". Whether a vehicle's inherent crew is a HS: a Carrier's, unless its MA is a FT or a 76mm mortar or it tows a Gun.
    /// </summary>
    public static bool InherentHalfSquad(string? vehicleClass, string? mainArmament, int? mainArmamentCaliber, bool towsGun) =>
        IsCarrier(vehicleClass) && mainArmament != "ft" && mainArmamentCaliber != 76 && !towsGun;

    /// <summary>
    /// D6.83 (p. 206): "The inherent crew/HS of a Carrier has the option to unload more quickly as per 6.5 but may not Remove any of the vehicle
    /// armament (6.631) or destroy the vehicle (5.411) if it does so." Why a vehicle's inherent crew may not unload as its Passengers do, or null: only a
    /// Carrier's inherent HS does, while it is still aboard; the Carrier is then Abandoned.
    /// </summary>
    public static string? CrewUnloadBar(string id, bool inherentHalfSquad, bool abandoned) =>
        !inherentHalfSquad ? $"play.unload: {id}'s crew does not unload as Passengers do; only a Carrier's inherent HS does (D6.83)"
        : abandoned ? $"play.unload: {id}'s inherent HS has already left it (D6.83, D5.41)"
        : null;

    /// <summary>
    /// D6.82 (p. 206), A1.32 (p. 45; pass 70, ruling R70.28): a Carrier HS is a HS whose creation recorded the Carrier it left (<paramref name="crewOf"/>);
    /// it Recombines with no TC and no leader present. An ordinary HS, and every HS of a record made before the pass, has none.
    /// </summary>
    public static bool CarrierHalfSquad(bool halfSquad, string? crewOf) => halfSquad && crewOf is not null;

    /// <summary>
    /// Whether a leaving crew's creation records the vehicle it left (pass 70, ruling R70.28): only a Carrier's inherent HS, which may later Recombine as a
    /// Carrier HS; a crew counter re-crews by its own rule (D5.42).
    /// </summary>
    public static bool RecordsCarrier(string leavingKind) => leavingKind == "asl:half-squad";

    /// <summary>
    /// The counter a crew that leaves its vehicle is placed as (D5.1, D6.82; rulings R65.18, R65.19), as its definition and kind: a Carrier's inherent HS
    /// is the nationality's 2-4-7 HS, not elite (an elite Carrier HS waits for a scenario that names one); any other crew, or a Carrier HS of a
    /// nationality with no 2-4-7 HS in the catalog, is the nationality's vehicle crew. Null when the catalog has neither.
    /// </summary>
    public static (string Definition, string Kind)? LeavingCrew(IEnumerable<FireDefinition> definitions, string? nationality, bool halfSquad)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        FireDefinition[] all = [.. definitions];
        if (halfSquad && all.Where(item => item.Kind == "asl:half-squad" && item.Nationality == nationality && item.Class != "elite"
            && item is { Firepower: 2, Range: 4, Morale: 7 }).Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault() is { } half)
        {
            return (half, "asl:half-squad");
        }

        return ScenarioA1Definitions.LeavingCrewDefinition(all, nationality) is { } crew ? (crew, ScenarioA1Definitions.VehicleCrewKind) : null;
    }
}
