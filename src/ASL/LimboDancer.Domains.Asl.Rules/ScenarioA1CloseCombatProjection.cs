namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A unit of a contested Location as the Melee read sees it (A11.15, A11.7): its side, whether it is a vehicle, whether it holds enemy Infantry in Melee, and its "?".</summary>
public sealed record MeleeUnitFacts(string Id, string Side, bool Vehicle, bool Holds, bool Concealed, bool Hidden);

/// <summary>
/// The projector's Close Combat, advance, and prisoner decisions (pass 32.g, S6): a record's legality as refusals with their codes, and the state rules a
/// record applies: the CCPh's start, Melee, the advance, a Guard's prisoners, the Ambush and CC records, a vehicle's CC, capture, freeing, and the Guards
/// kept. The projector reads the state and rewrites it; these take plain values and decide.
/// </summary>
public static class ScenarioA1CloseCombatProjection
{
    /// <summary>A11.19 (ruling R14.2): a Location holding units of more than one side is contested as the CCPh begins.</summary>
    public static bool Contested(int sideCount) => sideCount > 1;

    /// <summary>A11.19: a unit the CCPh's start reads: active, not a prisoner, in a Location.</summary>
    public static bool StartUnit(bool active, bool captured, bool located) => active && !captured && located;

    /// <summary>A11.19 (ruling R14.2): a Dummy in a contested Location is removed as the CCPh begins.</summary>
    public static bool StartEliminates(bool dummy) => dummy;

    /// <summary>A11.19 (ruling R14.2): a hidden unit in a contested Location is placed beneath a "?" as the CCPh begins.</summary>
    public static bool StartPlaces(bool dummy, bool hidden) => !dummy && hidden;

    /// <summary>A11.15, A20.5 (ruling R14.2): a unit the Melee read considers: active, not a prisoner, not concealed or hidden, not a Dummy, in a Location.</summary>
    public static bool MeleeCandidate(bool active, bool captured, bool concealed, bool hidden, bool dummy, bool located) => active && !captured && !concealed && !hidden && !dummy && located;

    /// <summary>
    /// A11.15, A11.7 (rulings R11.16, R14.2): the units of one Location held in Melee at the CCPh's end: each non-vehicle with an enemy there that holds, where a
    /// holding vehicle holds only a unit without "?".
    /// </summary>
    public static IEnumerable<string> InMelee(IReadOnlyList<MeleeUnitFacts> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.Where(unit => !unit.Vehicle && group.Any(other => other.Side != unit.Side && other.Holds && (!other.Vehicle || (!unit.Concealed && !unit.Hidden)))).Select(unit => unit.Id);
    }

    /// <summary>A11.15, A11.7: a unit holds the enemy Infantry in its Location in Melee: any Infantry, or a vehicle not Abandoned and not in Motion.</summary>
    public static bool HoldsInMelee(bool vehicle, bool abandoned, bool motion) => !vehicle || (!abandoned && !motion);

    /// <summary>A11.15: the Melee a unit takes or leaves at the CCPh's end, or null when its Melee already agrees with the set.</summary>
    public static bool? MeleeChange(bool held, bool melee) => held == melee ? null : held;

    /// <summary>A11.15: a unit is no longer held in Melee once no enemy unit that is not a prisoner shares its Location.</summary>
    public static bool LeavesMelee(bool melee, bool uncapturedEnemyThere) => melee && !uncapturedEnemyThere;

    /// <summary>A4.7 (UNIT-STATE-032): an advance is in the APh by units of the phasing side from one Location, once each, named once.</summary>
    public static RecordRefusal? AdvanceRefusal(string? phase, int count, bool anyInvalid, int locationCount, int distinctCount) =>
        phase != "aph" || count == 0 || anyInvalid || locationCount != 1 || distinctCount != count
            ? new RecordRefusal("UNIT-STATE-032", "An advance moves units of the phasing side from one Location, once each, in the APh (A4.7).")
            : null;

    /// <summary>A4.7, A15.431, A11.15: a unit broken, pinned, berserk, in Melee, or captured may not advance.</summary>
    public static bool AdvanceBarred(bool broken, bool pinned, bool berserk, bool melee, bool captured) => broken || pinned || berserk || melee || captured;

    /// <summary>A4.7, A15.431, A11.15 (UNIT-STATE-032): the barred unit's refusal.</summary>
    public static RecordRefusal AdvanceBarredRefusal(string unitId) =>
        new("UNIT-STATE-032", $"'{unitId}' is broken, pinned, berserk, in Melee, or captured, so it may not advance (A4.7, A15.431, A11.15).");

    /// <summary>A2.6 (ruling R25.5; UNIT-STATE-032): an exit by advance leaves the map from the units' own Location.</summary>
    public static RecordRefusal? ExitRefusal(bool anyElsewhere) =>
        anyElsewhere ? new RecordRefusal("UNIT-STATE-032", "An exit by advance leaves the map from the units' own Location (A2.6).") : null;

    /// <summary>A20.53: a prisoner follows its Guard when active and in the Guard's custody.</summary>
    public static bool FollowsGuard(bool active, bool custodianIsGuard) => active && custodianIsGuard;

    /// <summary>UNIT-STATE-023: an Ambush record replays only with the Close Combat verifier.</summary>
    public static RecordRefusal AmbushVerifierRefusal() => new("UNIT-STATE-023", "An Ambush record can be replayed only with the Close Combat verifier.");

    /// <summary>UNIT-STATE-023: an Ambush record's rolls are recorded before it.</summary>
    public static RecordRefusal AmbushRollRefusal(string roll) => new("UNIT-STATE-023", $"The Ambush record's roll '{roll}' is not recorded before it.");

    /// <summary>A11.4, A11.12 (UNIT-STATE-030): the Ambush drs are made in the CCPh before any CC in the Location, with no other Location's CC open.</summary>
    public static RecordRefusal? AmbushOrderRefusal(string? phase, bool ccHereOrOpenElsewhere) =>
        phase != "ccph" || ccHereOrOpenElsewhere
            ? new RecordRefusal("UNIT-STATE-030", "The Ambush drs are made in the CCPh before any CC in the Location, with no other Location's CC open (A11.4, A11.12).")
            : null;

    /// <summary>UNIT-STATE-030: the Close Combat verifier's reason.</summary>
    public static RecordRefusal VerifierRefusal(string reason) => new("UNIT-STATE-030", reason);

    /// <summary>UNIT-STATE-023: a CC record replays only with the Close Combat verifier.</summary>
    public static RecordRefusal CcVerifierRefusal() => new("UNIT-STATE-023", "A CC record can be replayed only with the Close Combat verifier.");

    /// <summary>UNIT-STATE-023: a CC record's rolls are recorded before it.</summary>
    public static RecordRefusal CcRollRefusal(string roll) => new("UNIT-STATE-023", $"The CC record's roll '{roll}' is not recorded before it.");

    /// <summary>D7.21 (UNIT-STATE-030): CC Reaction Fire answers the open window on the moving vehicle's MP expenditure in the MPh.</summary>
    public static RecordRefusal? ReactionRecordRefusal(string? phase, bool windowOpen, bool vehicleMove, bool vehicleIsMember) =>
        phase != "mph" || !windowOpen || !vehicleMove || !vehicleIsMember
            ? new RecordRefusal("UNIT-STATE-030", "CC Reaction Fire answers the open window on the moving vehicle's MP expenditure (D7.21).")
            : null;

    /// <summary>A11.31 (UNIT-STATE-030): CC with a vehicle is sequential in the CCPh: the side named attacks next, and no unit attacks twice.</summary>
    public static RecordRefusal? SequentialRecordRefusal(string? phase, bool closed, string? attackerSide, string? next, bool anyAttackedTwice, bool otherOpen) =>
        phase != "ccph" || closed || attackerSide is null || (next is not null && next != attackerSide) || anyAttackedTwice || otherOpen
            ? new RecordRefusal("UNIT-STATE-030", "CC with a vehicle is sequential in the CCPh: the side named attacks next, and no unit attacks twice (A11.31).")
            : null;

    /// <summary>D7.21: CC Reaction Fire leaves the Location's CC as it was.</summary>
    public static bool ReactionLeavesState(bool reaction) => reaction;

    /// <summary>A11.31 (UNIT-STATE-030): a side passes in a CC Location holding a vehicle when its attack is next and it has not passed.</summary>
    public static RecordRefusal? PassRecordRefusal(string? phase, bool closed, string? next, string side, bool passedAlready, bool sideExists) =>
        phase != "ccph" || closed || (next is not null && next != side) || passedAlready || !sideExists
            ? new RecordRefusal("UNIT-STATE-030", "A side passes in a CC Location holding a vehicle when its attack is next (A11.31).")
            : null;

    /// <summary>A4.152 (ruling R27.3): the record is the CC of a berserk Infantry OVR: in the MPh, flagged, with the DEFENDER's window on the entry closed at its Location.</summary>
    public static bool OverrunRecord(string? phase, bool infantryOverrun, bool windowClosedAtLocation) => phase == "mph" && infantryOverrun && windowClosedAtLocation;

    /// <summary>A11.12, A4.152 (UNIT-STATE-030): CC is resolved in the CCPh, once per Location, one Location at a time, or at once after a berserk Infantry OVR in the MPh.</summary>
    public static RecordRefusal? CcRecordRefusal(string? phase, bool overrun, bool closed, bool otherOpen) =>
        (phase != "ccph" && !overrun) || closed || otherOpen
            ? new RecordRefusal("UNIT-STATE-030", "CC is resolved in the CCPh, once per Location, one Location at a time, or at once after a berserk Infantry OVR in the MPh (A11.12, A4.152).")
            : null;

    /// <summary>
    /// A11.3, A11.33, A11.34, A11.41 (rulings R14.6; referee, pass 14): the rounds that may come next: one simultaneous round with no Ambush; after an Ambush the
    /// ambushed side's round when every ambusher is gone, the ambusher's first, then either; and the prisoners' round before any other.
    /// </summary>
    public static IReadOnlyList<string> AllowedRounds(string? ambusher, bool ambushersPresent, int roundsOtherThanPrisoners, int roundsRecorded)
    {
        string[] allowed = ambusher is null ? [CloseCombatFacts.Simultaneous]
            : !ambushersPresent ? [CloseCombatFacts.AmbushedRound]
            : roundsOtherThanPrisoners == 0 ? [CloseCombatFacts.AmbusherRound]
            : [CloseCombatFacts.AmbusherRound, CloseCombatFacts.AmbushedRound];
        return roundsRecorded == 0 ? [.. allowed, CloseCombatFacts.PrisonersRound] : allowed;
    }

    /// <summary>A11.3, A11.12, A11.32 (UNIT-STATE-030): the round is one allowed, and no unit attacks or is attacked twice.</summary>
    public static RecordRefusal? RoundRecordRefusal(IReadOnlyList<string> allowed, string round, bool anyAttackedTwice, bool anyDefendedTwice, string locationText)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        return !allowed.Contains(round) || anyAttackedTwice || anyDefendedTwice
            ? new RecordRefusal("UNIT-STATE-030", $"The next CC round in {locationText} is {string.Join(" or ", allowed)}, and no unit attacks or is attacked twice (A11.3, A11.12, A11.32).")
            : null;
    }

    /// <summary>A11.3: the Location closes unless the round was the ambusher's or the prisoners'.</summary>
    public static bool Closes(string round) => round is not (CloseCombatFacts.AmbusherRound or CloseCombatFacts.PrisonersRound);

    /// <summary>J2.31 (ruling R14.1): the first round other than the prisoners' declares the Location Hand-to-Hand for the CCPh; once declared it stays.</summary>
    public static bool RecordHandToHand(bool entryHandToHand, string round, bool factsHandToHand) => entryHandToHand || (round != CloseCombatFacts.PrisonersRound && factsHandToHand);

    /// <summary>A11.31 (table-player finding, pass 11): a HS a CC attacker is Reduced to has made its attack in the sequential CC of that Location.</summary>
    public static bool ReducedAttackerHasAttacked(bool consumedHasAttacked) => consumedHasAttacked;

    /// <summary>UNIT-STATE-014: a capture names an active unit.</summary>
    public static RecordRefusal CaptureNotActiveRefusal(string unitId) => new("UNIT-STATE-014", $"'{unitId}' is not an active unit.");

    /// <summary>A20.2 (UNIT-STATE-014): berserk units cannot be captured.</summary>
    public static RecordRefusal? CaptureBerserkRefusal(bool berserk) => berserk ? new RecordRefusal("UNIT-STATE-014", "Berserk units cannot be captured (A20.2, p. 86).") : null;

    /// <summary>A15.5, A20.21 (UNIT-STATE-031): a pending surrender is taken by one of its captors.</summary>
    public static RecordRefusal? CaptorRefusal(string unitId, IReadOnlyList<string>? pendingCaptors, string custodian) =>
        pendingCaptors is { } captors && !captors.Contains(custodian, StringComparer.Ordinal)
            ? new RecordRefusal("UNIT-STATE-031", $"'{unitId}' surrenders to one of {string.Join(", ", captors)} (A15.5).")
            : null;

    /// <summary>A20.5, A20.55 (table player, pass 14): a prisoner whose escape eliminated its Guard was already freed when the Guard left play.</summary>
    public static bool AlreadyFreed(bool active, bool noCustodian, bool unarmed, bool captured) => active && noCustodian && unarmed && !captured;

    /// <summary>UNIT-STATE-014: a prisoner freed is a guarded prisoner.</summary>
    public static RecordRefusal NotGuardedRefusal(string unitId) => new("UNIT-STATE-014", $"'{unitId}' is not a guarded prisoner.");

    /// <summary>A20.5 (ruling R14.5): a Guard has left play or been captured, so its prisoners pass on or are freed.</summary>
    public static bool GuardLeft(bool guardActive, bool guardCaptured) => !(guardActive && !guardCaptured);

    /// <summary>A20.5 (ruling R14.5): an heir to a Guard's prisoner: an active armed Personnel unit of the Guard's side in the prisoner's Location with Guard capacity for it.</summary>
    public static bool GuardHeir(bool active, bool ofSide, bool atLocation, bool captured, bool unarmed, bool personnel, bool vehicle, bool capacity) =>
        active && ofSide && atLocation && !captured && !unarmed && personnel && !vehicle && capacity;
}
