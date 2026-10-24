namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The projector's vehicle decisions (pass 32.e): the vehicle step's, the vehicle check's, the OVR's, the PAATC's, the Passengers', and the
/// wreck's. The projector reads the state and rewrites it; these take plain values and decide.
/// </summary>
public static class ScenarioA1VehicleProjection
{
    /// <summary>D2.1 (UNIT-STATE-034): a vehicle step is made in the MPh; the projector asks before it reads the vehicle.</summary>
    public static bool StepPhase(string? phase) => phase == "mph";

    /// <summary>
    /// D2.1 (UNIT-STATE-034): a vehicle step moves a vehicle of the phasing side that has not ended its move; only a Load spends no MP.
    /// </summary>
    public static bool StepAllowed(bool vehicle, bool phasing, bool movementEnded, int halfMp, bool load) =>
        vehicle && phasing && !movementEnded && halfMp >= 0 && (halfMp != 0 || load);

    /// <summary>D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must move.</summary>
    public static bool Recalling(bool recalled, bool stunRecovery) => recalled && !stunRecovery;

    /// <summary>D2.11: a turn is one hexspine either way.</summary>
    public static bool OneHexspine(int turned, int facing) => Math.Abs((turned - facing + 6) % 6) is 1 or 5;

    /// <summary>D2.1: the vehicle's MP after an expenditure, counted in halves.</summary>
    public static (int MfSpent, bool HalfMfSpent) Spend(int mfSpent, bool halfMfSpent, int halfMp)
    {
        var halves = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + halfMp;
        return (halves / 2, halves % 2 == 1);
    }

    /// <summary>D8.3: a Bog Removal is decided by its colored dr, the first die; every other vehicle check by its DR.</summary>
    public static int CheckFinal(bool bogRemoval, int first, int second, int drm) => (bogRemoval ? first : first + second) + drm;

    /// <summary>
    /// D8.21, D2.5, D2.51, D8.3: a Bog or an Immobilization, or a Bog Removal that does not free the vehicle, ends its move once the DEFENDER passes.
    /// </summary>
    /// <summary>
    /// D8.3, D8.5 (p. 209; ruling R11.10; pass 35, task 35.13 f): what a bogged vehicle may spend MP on: its Bog Removal as its first expenditure,
    /// remaining where it is, and the non-movement expenditures of D8.5, of which Loading and Unloading are built. The planner and the projector
    /// read this one answer (the pass 32 design's section 12, item 6).
    /// </summary>
    public static bool BoggedMaySpend(string kind, bool firstExpenditure) => kind is "load" or "unload" or "remain" || (kind == "start" && firstExpenditure);

    public static bool CheckStops(bool boggedOrImmobilized, bool bogRemoval, bool freed) => boggedOrImmobilized || (bogRemoval && !freed);

    /// <summary>D7.1: an OVR is resolved by its vehicle's fire record after the DEFENDER's window on its declaration closes.</summary>
    public static bool OverrunResolvable(bool vehicleMoveClosed, bool declaredHere, Func<bool> mover, Func<bool> fireRecorded) =>
        vehicleMoveClosed && declaredHere && mover() && fireRecorded();

    /// <summary>A11.6: a PAATC passes when its Final DR does not exceed the Morale.</summary>
    public static bool PaatcPassed(int first, int second, int drm, int morale) => first + second + drm <= morale;

    /// <summary>A2.6, D6.1: a Passenger or Rider leaves with its vehicle when it exits, and is lost with it otherwise.</summary>
    public static bool PassengerExits(bool containerExited) => containerExited;

    /// <summary>D10.1: only an active vehicle on the map becomes a wreck.</summary>
    public static bool WreckAllowed(bool vehicle, Func<bool> onMap) => vehicle && onMap();

    /// <summary>
    /// D6.4, A4.2 (pass 65, ruling R65.15; the boundary review): whether a recorded load is the last step of a moving Infantry stack's own move, read by
    /// the planner's <see cref="ScenarioA1PassengerCalculator.EndOfMoveLoadBar"/>: every member of the stack boards, and no other unit, once the DEFENDER's
    /// window on its last step has closed. False when no Infantry stack moves, or when the record is no load.
    /// </summary>
    public static bool BoardsAfterMove(bool load, bool infantryMoving, bool windowOpen, IReadOnlyList<string> members, IReadOnlyCollection<string>? boarding) =>
        load && infantryMoving && boarding is { Count: > 0 } && ScenarioA1PassengerCalculator.EndOfMoveLoadBar(string.Empty, true, windowOpen, members, boarding) is null;

    /// <summary>
    /// D5.41 (pass 65, ruling R65.20; the boundary review): whether a recorded voluntary Abandonment stands (UNIT-STATE-034), by the planner's
    /// <see cref="ScenarioA1CrewActsRules.AbandonBar"/> over the facts the state holds, and what the record names: the vehicle in its recorded Location,
    /// and its crew, of its side, either crewing it from inside or the counter placed beneath it. The projector holds no fire definition, so whether the
    /// vehicle is armed (D5.1) is the planner's check alone.
    /// </summary>
    public static bool AbandonStands(AbandonRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Vehicle && ScenarioA1CrewActsRules.AbandonBar(string.Empty, facts.OwnMph, facts.InRecordedLocation, true, facts.Abandoned, facts.UnitMoving, facts.Moved,
            facts.Fired, facts.Shocked, facts.Stunned) is null && facts.CrewOfItsSide && (facts.CrewInside || facts.CrewInLocation);
    }

    /// <summary>
    /// D5.42, D5.43, D5.341 (pass 65, ruling R65.20; the boundary review): whether a recorded re-crewing stands (UNIT-STATE-034), by the planner's
    /// <see cref="ScenarioA1CrewActsRules.RecrewBar"/> over the facts the state holds (when the vehicle's crew left it is the state's list of this Player
    /// Turn), with no unit moving (A4.2), and a SW returned only when the entering unit holds it.
    /// </summary>
    public static bool RecrewStands(bool vehicle, bool unitMoving, RecrewFacts facts, bool returnedStands)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return vehicle && !unitMoving && ScenarioA1CrewActsRules.RecrewBar(facts) is null && returnedStands;
    }

    /// <summary>
    /// D10.5 (pass 65, ruling R65.23; the boundary review): whether a recorded Scrounging stands (UNIT-STATE-034), by the planner's
    /// <see cref="ScenarioA1WreckRules.ScroungeBar"/> over the facts the state holds, and what the record names: a dr rolled as one die, and no more SW
    /// taken than a dr of 1 to 3 allows, each the unit's own.
    /// </summary>
    public static bool ScroungeStands(bool vehicle, ScroungeFacts facts, bool drRolled, int dr, int taken, bool takenHeld)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return vehicle && ScenarioA1WreckRules.ScroungeBar(facts) is null && drRolled && taken <= (ScenarioA1WreckRules.Succeeds(dr) ? dr : 0) && takenHeld;
    }

    /// <summary>
    /// A11.52, A21.2, A21.21 (pass 65, ruling R65.22; the boundary review): whether a recorded capture stands (UNIT-STATE-034): in the CCPh, a vehicle in
    /// its recorded Location passes to another side in the game, by a cause Rules names; an AFV only when Abandoned, as the planner's
    /// <see cref="ScenarioA1CapturedVehicleRules.CaptureAttemptBar"/> has it. The projector holds no fire definition, so whether the vehicle is armed, and
    /// which units share its Location, are the planner's checks alone.
    /// </summary>
    public static bool CaptureStands(bool ccph, bool vehicle, bool otherSide, bool inRecordedLocation, bool sideInGame, string cause, bool abandoned) =>
        ccph && vehicle && otherSide && inRecordedLocation && sideInGame
        && cause is ScenarioA1CapturedVehicleRules.Automatic or ScenarioA1CapturedVehicleRules.CloseCombat or ScenarioA1CapturedVehicleRules.Unarmed
        && (cause == ScenarioA1CapturedVehicleRules.Unarmed || ScenarioA1CapturedVehicleRules.CaptureAttemptBar(string.Empty, true, abandoned) is null);

    /// <summary>
    /// A21.21: whether a recorded Temporary Driver stands: an unarmed vehicle's capture names him, a SMC of the captor's side on foot in its Location.
    /// </summary>
    public static bool DriverStands(string cause, bool smc, bool captorsSide, bool onFootInLocation) =>
        cause == ScenarioA1CapturedVehicleRules.Unarmed && smc && captorsSide && onFootInLocation;
}

/// <summary>
/// A recorded voluntary Abandonment as the projector reads it (D5.41; pass 65, ruling R65.20): the phase, the vehicle's state, and the crew the record
/// names. <see cref="Moved"/> and <see cref="Fired"/> are <see cref="ScenarioA1CrewActsRules.VehicleMoved"/> and
/// <see cref="ScenarioA1CrewActsRules.VehicleFired"/>, as the planner reads them.
/// </summary>
public sealed record AbandonRecordFacts(bool OwnMph, bool Vehicle, bool InRecordedLocation, bool Abandoned, bool UnitMoving, bool Moved, bool Fired, bool Shocked,
    bool Stunned, bool CrewOfItsSide, bool CrewInside, bool CrewInLocation);
