namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of an Infantry unit entering an Abandoned vehicle to crew it (D5.42, D5.43, D5.341; pass 65, ruling R65.20): the phase, the vehicle's
/// state, the unit's, and when and how the vehicle's last crew left it.
/// </summary>
public sealed record RecrewFacts(string Unit, string Vehicle, bool OwnMph, bool VehicleAbandoned, bool SameSide, bool Infantry, bool InLocation, bool MovedThisPhase,
    bool GoodOrder, bool LeftThisPlayerTurn, bool OwnCrew, bool Recalled)
{
    /// <summary>
    /// D5.42's EXC (p. 204; pass 70, ruling R70.34): it is the CCPh, and the unit's side captured the vehicle in it (A21.2), so a captor may enter it then,
    /// though its MF are spent and in the Player Turn its crew left it. False in every other phase, and for a vehicle captured before this CCPh.
    /// </summary>
    public bool CapturedThisCcph
    {
        get; init;
    }
}

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

    /// <summary>D5.8: the fire counter a crew leaving its vehicle takes, as the <c>vehicle-abandoned</c> record names it.</summary>
    public const string PrepFireCounter = "prep";
    public const string FinalFireCounter = "final";

    /// <summary>
    /// D5.1, D5.6 (pass 65, ruling R65.19; the boundary review): the id of the crew counter a vehicle's inherent crew takes as it leaves the vehicle, made
    /// by the action that places it. The planner writes it, and the re-crewing and the Victory count know the vehicle's own crew by it.
    /// </summary>
    public static string CrewId(string attemptId, string vehicle) => $"{attemptId}-{vehicle}-crew";

    /// <summary>Whether a unit is the crew counter its vehicle's inherent crew became as it left (D5.42, D5.341), by the id <see cref="CrewId"/> gives it.</summary>
    public static bool IsOwnCrew(string unitId, string vehicle)
    {
        ArgumentNullException.ThrowIfNull(unitId);
        return unitId.EndsWith($"-{vehicle}-crew", StringComparison.Ordinal);
    }

    /// <summary>D5.8, C.8: whether a vehicle has fired this Player Turn: it carries a Prep, First, Final, Bounding, or Intensive Fire counter.</summary>
    public static bool VehicleFired(bool prepFire, bool firstFire, bool finalFire, bool boundingFire, bool intensiveFire) =>
        prepFire || firstFire || finalFire || boundingFire || intensiveFire;

    /// <summary>
    /// D5.8, C.8, D5.41: whether a vehicle has moved this Player Turn: it entered a new hex or moved under a Motion counter (the state's list of this
    /// Player Turn), or it has spent MP in this MPh.
    /// </summary>
    public static bool VehicleMoved(bool movedThisPlayerTurn, bool mph, bool spentMp) => movedThisPlayerTurn || (mph && spentMp);

    /// <summary>D5.43: a unit enters a vehicle to crew it only unpinned and in Good Order: not broken, berserk, in Melee, captured, or TI either.</summary>
    public static bool RecrewReady(bool broken, bool pinned, bool berserk, bool melee, bool captured, bool ti) => !broken && !pinned && !berserk && !melee && !captured && !ti;

    /// <summary>The D5.8 fire counter as the <c>vehicle-abandoned</c> record names it, from <see cref="CrewFireCounter"/>'s answer; null for none.</summary>
    public static string? FireCounterName(UnitCondition? counter) => counter switch
    {
        UnitCondition.PrepFire => PrepFireCounter,
        UnitCondition.FinalFire => FinalFireCounter,
        _ => null,
    };

    /// <summary>The D5.8 fire counter a <c>vehicle-abandoned</c> record names, read back for the crew; null for none or a name it does not know.</summary>
    public static UnitCondition? FireCounterOf(string? recorded) => recorded switch
    {
        PrepFireCounter => UnitCondition.PrepFire,
        FinalFireCounter => UnitCondition.FinalFire,
        _ => null,
    };

    /// <summary>
    /// D3.43, D5.34 (pp. 200, 203): what a crew Abandoning its vehicle takes with it. A unit crewing it from inside leaves as itself with the vehicle's Armor
    /// Leader, dormant, and its Stun "+1"; the inherent crew placed as its own counter carries both in the record that places it.
    /// </summary>
    public static (bool StunRecovery, bool ArmorLeader) AbandonCarries(bool crewInside, bool vehicleStunRecovery) => (crewInside && vehicleStunRecovery, crewInside);

    /// <summary>
    /// D3.43, D5.34, D5.42 (pp. 200, 203, 204): what a unit entering an Abandoned vehicle to crew it brings back: its Stun "+1" goes to the vehicle and
    /// leaves the unit, and an Armor Leader it carries is the vehicle's again (the vehicle keeps its own when the unit carries none).
    /// </summary>
    public static (bool VehicleStunRecovery, bool ArmorLeaderReturns) RecrewCarries(bool unitStunRecovery, bool unitArmorLeader) => (unitStunRecovery, unitArmorLeader);

    /// <summary>
    /// D5.41 (p. 204): "A crew may abandon its vehicle only during its own MPh, and does so by expending all of its MF to be placed beneath the vehicle.
    /// The vehicle/crew may not have moved or fired during that Player Turn." D5.1: "All unarmed vehicles are manned only by an Inherent Driver who never
    /// leaves the vehicle." C7.42 (p. 177): "The crew and passengers of a shocked AFV are incapable of any action", an Unconfirmed Kill still Shocked; D5.34
    /// (p. 203): a Stunned AFV may not move or spend MP "for any reason" in that Player Turn (the referee's review). Why a vehicle's crew may not Abandon it
    /// now, or null.
    /// </summary>
    public static string? AbandonBar(string vehicle, bool ownMph, bool onMap, bool armed, bool abandoned, bool unitMoving, bool moved, bool fired, bool shocked = false,
        bool stunned = false) =>
        !ownMph ? $"play.abandon: {vehicle}'s crew Abandons it only in its own MPh (D5.41)"
        : !onMap ? $"play.abandon: {vehicle} is not on the map"
        : !armed ? $"play.abandon: {vehicle} is unarmed; its Inherent Driver never leaves it (D5.1)"
        : abandoned ? $"play.abandon: {vehicle} is already Abandoned (D5.41)"
        : shocked ? $"play.abandon: {vehicle} is Shocked, and its crew is incapable of any action until it recovers (C7.42)"
        : stunned ? $"play.abandon: {vehicle} is Stunned, and its crew may not Abandon it this Player Turn (D5.34)"
        : unitMoving ? $"play.abandon: a unit is moving; {vehicle}'s crew Abandons it once that move ends (A4.2)"
        : moved ? $"play.abandon: {vehicle} has moved this Player Turn, so its crew may not Abandon it now (D5.41)"
        : fired ? $"play.abandon: {vehicle} has fired this Player Turn, so its crew may not Abandon it now (D5.41)"
        : null;

    /// <summary>
    /// D5.311 (p. 203; pass 65, ruling R65.31): an Unprotected crew that breaks stays inside its AFV, broken, until it routs beneath it in the RtPh; the
    /// vehicle "automatically Stops" and its broken crew neither moves nor fires it. Why the vehicle may not act, or null when its crew is not broken.
    /// </summary>
    public static string? BrokenCrewBar(string vehicle, string? brokenCrew) =>
        brokenCrew is null ? null
            : $"play.vehicle-crew-broken: {vehicle}'s crew {brokenCrew} is broken inside it, so {vehicle} neither moves nor fires; the crew routs beneath it in the RtPh, and {vehicle} is then Abandoned (D5.311)";

    /// <summary>D5.311 (p. 203; ruling R65.31): "The vehicle automatically Stops (no Stop MP is spent) when its crew breaks."</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CrewBrokeConditions { get; } = [(UnitCondition.Motion, false)];

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
    /// forced to leave its vehicle may not Remove (6.631) any weapons from it." D5.41: a lone SMC Removes none. D6.83 (p. 206): "A Carrier HS/crew may also
    /// abandon/re-occupy a Carrier along with the Carrier's MG/ATR/mortar as per 6.631" (the referee's review); its quick unload (D6.5) Removes nothing.
    /// The MG Removed, its mount and FP, or why none may be: the MA when it is a MG (an AAMG), else the BMG. Only a MG of 3 FP or less is built, taken as
    /// the nationality's LMG; a mortar or ATR is not.
    /// </summary>
    public static (string? Mount, int Fp, string? Bar) MgRemoval(string vehicle, string? vehicleClass, bool unarmored, string? vehicleNationality, string? crewNationality,
        bool smcCrew, bool forcedByTc, string? mainArmament, int? antiAircraftMg, int? bowMg, bool maDisabled, bool bmgDisabled)
    {
        if (forcedByTc)
        {
            return (null, 0, $"play.abandon: {vehicle}'s crew was forced out by a failed Immobilization TC and may not Remove its weapons (D5.5)");
        }

        if (vehicleClass is not ("halftrack" or ScenarioA1CarrierRules.VehicleClass) || unarmored)
        {
            return (null, 0, $"play.abandon: only an armored halftrack's or a Carrier's MG is Removed as its crew Abandons it; {vehicle}'s weapons may only be Scrounged (D6.631, D6.83, D5.41)");
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
    /// it succeeds only unpinned and in Good Order. D5.341: a Recalled AFV's crew "may not re-crew that AFV". Why the unit may not enter, or null. An enemy
    /// vehicle is entered once captured (A21.2; ruling R65.22), when it is the captor's.
    /// </summary>
    public static string? RecrewBar(RecrewFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var (unit, vehicle) = (facts.Unit, facts.Vehicle);
        // D5.42's EXC: "[EXC: CCPh; A21.2]": a captor enters the vehicle captured this CCPh in that CCPh, from its Location, whatever MF it spent.
        var captor = facts.CapturedThisCcph;
        return !facts.OwnMph && !captor ? $"play.recrew: {unit} enters an Abandoned vehicle to crew it only in its own MPh, or as its captor in the CCPh of its capture (D5.42, A21.2)"
            : !facts.VehicleAbandoned ? $"play.recrew: {vehicle} is not an Abandoned vehicle on the map; only such a vehicle is entered to crew it (D5.42)"
            : !facts.SameSide ? $"play.recrew: {vehicle} is the enemy's; it is captured first, at the end of a CCPh or by a capture attempt in CC, and then crewed (A21.2)"
            : !facts.Infantry ? $"play.recrew: {unit} is not Infantry on foot, which alone enters a vehicle to crew it (D5.42)"
            : !facts.InLocation ? $"play.recrew: {unit} enters {vehicle} only from its Location (D5.42)"
            : facts.MovedThisPhase && !captor ? $"play.recrew: {unit} enters {vehicle} only from its Location, where it is at the start of its MPh, before it spends any MF (D5.42)"
            : !facts.GoodOrder ? $"play.recrew: {unit} enters {vehicle} only unpinned and in Good Order (D5.43)"
            : facts.LeftThisPlayerTurn && !captor ? $"play.recrew: {vehicle}'s crew left it this Player Turn, so it may not be entered until a later one (D5.42)"
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
