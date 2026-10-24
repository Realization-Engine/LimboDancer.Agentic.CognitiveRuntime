namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of an Infantry unit entering an Abandoned vehicle to crew it (D5.42, D5.43, D5.341; pass 65, ruling R65.20): the phase, the vehicle's
/// state, the unit's, and when and how the vehicle's last crew left it.
/// </summary>
public sealed record RecrewFacts(string Unit, string Vehicle, bool OwnMph, bool VehicleAbandoned, bool SameSide, bool Infantry, bool InLocation, bool MovedThisPhase,
    bool GoodOrder, bool LeftThisPlayerTurn, bool OwnCrew, bool Recalled);

/// <summary>
/// The crew's own acts (D5.4 to D5.43, D5.411, D5.8, D6.631, D3.43, pp. 200, 204, 206; pass 65, ruling R65.20): a crew Abandons its vehicle in its own MPh,
/// may destroy it as it leaves, and may Remove a halftrack's MG; an Infantry unit enters an Abandoned vehicle to crew it, bringing back the Armor Leader its
/// crew took with it; a crew counter that left its vehicle after the vehicle fired or moved marks the FP it may not use. Play reads the vehicle and the
/// units and writes the events. A21.22's penalties for a Temporary crew are increment 2i's.
/// </summary>
public static class ScenarioA1CrewActsRules
{
    /// <summary>D5.411: the crew turns the vehicle into a non-burning wreck as it leaves.</summary>
    public const string Wreck = "wreck";

    /// <summary>D5.411: the crew malfunctions the vehicle's weapons as it leaves.</summary>
    public const string Malfunction = "malfunction";

    /// <summary>D6.631: the MG Removed is the vehicle's MA (the SPW 251/1's AAMG).</summary>
    public const string MainArmament = "ma";

    /// <summary>D6.631: the MG Removed is the vehicle's BMG.</summary>
    public const string BowMg = "bmg";

    /// <summary>
    /// D5.41 (p. 204): "A crew may abandon its vehicle only during its own MPh, and does so by expending all of its MF to be placed beneath the vehicle.
    /// The vehicle/crew may not have moved or fired during that Player Turn." D5.1: "All unarmed vehicles are manned only by an Inherent Driver who never
    /// leaves the vehicle." Why a vehicle's crew may not Abandon it now, or null.
    /// </summary>
    public static string? AbandonBar(string vehicle, bool ownMph, bool onMap, bool armed, bool abandoned, bool unitMoving, bool moved, bool fired) =>
        !ownMph ? $"play.abandon: {vehicle}'s crew Abandons it only in its own MPh (D5.41)"
        : !onMap ? $"play.abandon: {vehicle} is not on the map"
        : !armed ? $"play.abandon: {vehicle} is unarmed; its Inherent Driver never leaves it (D5.1)"
        : abandoned ? $"play.abandon: {vehicle} is already Abandoned (D5.41)"
        : unitMoving ? $"play.abandon: a unit is moving; {vehicle}'s crew Abandons it once that move ends (A4.2)"
        : moved ? $"play.abandon: {vehicle} has moved this Player Turn, so its crew may not Abandon it now (D5.41)"
        : fired ? $"play.abandon: {vehicle} has fired this Player Turn, so its crew may not Abandon it now (D5.41)"
        : null;

    /// <summary>
    /// D5.411 (p. 204): "Only an Inherent crew that voluntarily abandons a vehicle may turn it into a non-burning wreck or malfunction its weapon(s) as it
    /// leaves." Why the choice may not be made, or null: an unknown choice, a Temporary crew (A21.22), or a wreck made with PRC still aboard, which would be
    /// lost with it; they unload first.
    /// </summary>
    public static string? SelfDestructionBar(string vehicle, string? choice, bool inherentCrew, bool prcAboard) =>
        choice is null ? null
        : choice is not (Wreck or Malfunction) ? $"play.abandon: '{choice}' is not a self-destruction: a crew leaving may make {vehicle} a wreck or malfunction its weapons (D5.411)"
        : !inherentCrew ? $"play.abandon: {vehicle}'s crew is a Temporary crew; only an Inherent crew destroys its vehicle as it leaves (D5.411)"
        : choice == Wreck && prcAboard ? $"play.abandon: {vehicle} carries Passengers or Riders; they unload before its crew wrecks it (D5.411, D6.5)"
        : null;

    /// <summary>
    /// D5.411: the weapons a crew malfunctions as it leaves, as the vehicle's conditions: every MG it prints and its MA. An AAMG that is not the MA is not
    /// kept apart from the MA in the state.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> SelfDestructionConditions(bool mainArmament, bool bowMg, bool coaxialMg) =>
        [.. new (bool Has, UnitCondition Condition)[] { (mainArmament, UnitCondition.Malfunctioned), (bowMg, UnitCondition.BmgMalfunctioned), (coaxialMg, UnitCondition.CmgMalfunctioned) }
            .Where(item => item.Has).Select(item => (item.Condition, true))];

    /// <summary>
    /// D6.631 (p. 206): "A crew abandoning (5.41) an armored halftrack of that crew's nationality may Remove from the AFV and take with them ... that
    /// halftrack's MG/mortar armament. The MG takes the form of a MG counter with the same or less FP as available to the vehicle." D5.5: "A crew thus
    /// forced to leave its vehicle may not Remove (6.631) any weapons from it." D5.41: a lone SMC Removes none. The MG Removed, its mount and FP, or why
    /// none may be: the MA when it is a MG (an AAMG), else the BMG. Only a MG of 3 FP or less is built, taken as the nationality's LMG; a mortar is not.
    /// </summary>
    public static (string? Mount, int Fp, string? Bar) MgRemoval(string vehicle, string? vehicleClass, bool unarmored, string? vehicleNationality, string? crewNationality,
        bool smcCrew, bool forcedByTc, string? mainArmament, int? antiAircraftMg, int? bowMg, bool maDisabled, bool bmgDisabled)
    {
        if (forcedByTc)
        {
            return (null, 0, $"play.abandon: {vehicle}'s crew was forced out by a failed Immobilization TC and may not Remove its weapons (D5.5)");
        }

        if (vehicleClass != "halftrack" || unarmored)
        {
            return (null, 0, $"play.abandon: only an armored halftrack's MG is Removed as its crew Abandons it; {vehicle}'s weapons may only be Scrounged (D6.631, D5.41)");
        }

        if (smcCrew)
        {
            return (null, 0, $"play.abandon: a lone SMC crew Removes no weapon from {vehicle} (D5.41)");
        }

        if (crewNationality != vehicleNationality)
        {
            return (null, 0, $"play.abandon: {vehicle}'s MG is Removed only by a crew of its own nationality (D6.631)");
        }

        var (mount, fp) = mainArmament == "aamg" && antiAircraftMg is { } aamg && !maDisabled ? (MainArmament, aamg)
            : bowMg is { } bmg && !bmgDisabled ? (BowMg, bmg)
            : ((string?)null, 0);
        return mount is null ? (null, 0, $"play.abandon: {vehicle} has no MG left to Remove (D6.631)")
            : fp > 3 ? (null, 0, $"play.abandon: {vehicle}'s MG has {fp} FP; only a MG of 3 FP or less, taken as its nationality's LMG, is Removed here (D6.631)")
            : (mount, fp, null);
    }

    /// <summary>
    /// D6.631: the counter a Removed MG takes: the nationality's light MG of the most FP not above the vehicle's MG, the first by id; null when the catalog
    /// has none.
    /// </summary>
    public static string? RemovedMgDefinition(IEnumerable<FireDefinition> definitions, string? nationality, int fp)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return definitions.Where(item => item.Kind == "asl:mg" && item.Size == "light" && item.Nationality == nationality && item.Firepower is { } own && own <= fp)
            .OrderByDescending(item => item.Firepower).ThenBy(item => item.Id, StringComparer.Ordinal).Select(item => item.Id).FirstOrDefault();
    }

    /// <summary>D6.631: the vehicle's condition that marks a MG Removed from its mount: Disabled for the MA, BMG Disabled for the BMG.</summary>
    public static UnitCondition RemovedCondition(string mount) => mount == BowMg ? UnitCondition.BmgDisabled : UnitCondition.Disabled;

    /// <summary>
    /// D5.8 (p. 204): an inherent crew "may not fire its inherent FP until it takes crew counter form outside that vehicle, and only if it has not yet fired a
    /// vehicular weapon or been a moving target (C.8) prior to taking counter form during that Player Turn ... Any crew counter thus lacking use of its
    /// inherent FP should be marked with an appropriate Prep Fire or Final Fire counter." The counter a crew leaving its vehicle takes: Prep Fire in its
    /// own Player Turn and Final Fire in the enemy's, when the vehicle fired or was a moving target (it moved this Player Turn, or is in Motion); none
    /// otherwise.
    /// </summary>
    public static UnitCondition? CrewFireCounter(bool vehicleFired, bool movedThisPlayerTurn, bool motion, bool phasing) =>
        vehicleFired || movedThisPlayerTurn || motion ? phasing ? UnitCondition.PrepFire : UnitCondition.FinalFire : null;

    /// <summary>
    /// D5.42 (p. 204): "An Infantry unit may enter an Abandoned vehicle to become its Inherent crew only if it occupies that vehicle's Location at the start of
    /// its MPh ... and expends all of its MF to do so ... A vehicle may not be entered during the same Player Turn in which its previous crew left." D5.43:
    /// it succeeds only unpinned and in Good Order. D5.341: a Recalled AFV's crew "may not re-crew that AFV". Why the unit may not enter, or null. A
    /// captured vehicle (A21.2) is increment 2i's.
    /// </summary>
    public static string? RecrewBar(RecrewFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var (unit, vehicle) = (facts.Unit, facts.Vehicle);
        return !facts.OwnMph ? $"play.recrew: {unit} enters an Abandoned vehicle to crew it only in its own MPh (D5.42)"
            : !facts.VehicleAbandoned ? $"play.recrew: {vehicle} is not an Abandoned vehicle on the map; only such a vehicle is entered to crew it (D5.42)"
            : !facts.SameSide ? $"play.recrew: {vehicle} is the enemy's; crewing a captured vehicle is not built (A21.2)"
            : !facts.Infantry ? $"play.recrew: {unit} is not Infantry on foot, which alone enters a vehicle to crew it (D5.42)"
            : !facts.InLocation || facts.MovedThisPhase ? $"play.recrew: {unit} enters {vehicle} only from its Location, where it is at the start of its MPh, before it spends any MF (D5.42)"
            : !facts.GoodOrder ? $"play.recrew: {unit} enters {vehicle} only unpinned and in Good Order (D5.43)"
            : facts.LeftThisPlayerTurn ? $"play.recrew: {vehicle}'s crew left it this Player Turn, so it may not be entered until a later one (D5.42)"
            : facts.OwnCrew && facts.Recalled ? $"play.recrew: {unit} is {vehicle}'s Recalled crew, which may not re-crew that AFV (D5.341)"
            : null;
    }

    /// <summary>
    /// D5.1, A21.22 (pp. 88, 203): a vehicle crew or an infantry crew re-crews as an Inherent crew; a vehicle's own crew is its Inherent crew whatever its
    /// kind (a Carrier's HS, D6.82); any other friendly unit enters as a Temporary crew, whose penalties are increment 2i's.
    /// </summary>
    public static bool TemporaryCrew(string? kind, bool ownCrew) => !ScenarioA1Definitions.IsCrew(kind) && !ownCrew;

    /// <summary>
    /// D5.8: why a unit that crews a vehicle from inside may not fire as Infantry: "An inherent crew may fire all the armament of a vehicle ... but may not
    /// fire its inherent FP until it takes crew counter form outside that vehicle."
    /// </summary>
    public static string CrewInsideFireBar(string unit, string vehicle) =>
        $"play.fire: {unit} crews {vehicle} from inside; it fires {vehicle}'s armament, and its own FP only once it leaves (D5.8)";
}
