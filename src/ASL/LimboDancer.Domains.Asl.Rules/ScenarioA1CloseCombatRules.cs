namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What the Close Combat package asks for next in a round (ruling R5.8): the round is resolved; or a roll (its kind, the rest of its key, the units
/// selected, the dice count, the purpose); or the package left the round undecided (its reasons joined).
/// </summary>
public sealed record CloseCombatNextStep(bool Resolved, string? Kind, string? Rest, IReadOnlyList<string> Selected, int Count, string? Purpose, string? Undecided);

/// <summary>
/// The Close Combat decisions the planner made around the Close Combat package (unit steps 29 and 30; pass 32.g, S6): the Ambush drs and the CC round
/// before and after <see cref="ScenarioA1CloseCombatCalculator"/> runs, withdrawals and Infiltrations, the rounds' order, and the plan's words. The
/// package itself does not grow.
/// </summary>
/// <summary>A unit of a Location as the CC-required read sees it (A15.43, A11.15): berserk with a Known enemy there, or reinforcing a Melee this APh.</summary>
public sealed record RequiredCcUnitFacts(string Id, string Side, bool Berserk, bool Melee, bool Broken, bool Known, bool AdvancedHere, bool Crew, bool Vehicle);

public static class ScenarioA1CloseCombatRules
{
    /// <summary>A3.8, A11.1: CC is resolved in the CCPh.</summary>
    public static string? CcPhaseBar(string? phase) => phase != "ccph" ? "play.cc-phase: CC is resolved in the CCPh (A3.8, A11.1)" : null;

    /// <summary>A11.4, A11.12: the Ambush drs come before any CC in the Location, with no other Location's CC open.</summary>
    public static string? AmbushOrderBar(bool ccHereOrOpenElsewhere) =>
        ccHereOrOpenElsewhere ? "play.ambush-order: the Ambush drs come before any CC in the Location, with no other Location's CC open (A11.4, A11.12)" : null;

    /// <summary>E1.77 (backlog pass 16, ruling R16.7): at night, unless Illuminated, an Ambush needs a Final dr only two lower; null when not dark.</summary>
    public static bool? DarkNight(bool night, bool illuminated) => night && !illuminated ? true : null;

    /// <summary>The side whose Ambush dr the package asks for next, or null when it asks for none (the Ambush is resolved or refused).</summary>
    public static string? AmbushRollSide(IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        return reasons is [{ } missing] && missing.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal) ? missing["asl.a1.cc.roll-missing:ambush:".Length..] : null;
    }

    /// <summary>A11.4 (ruling R14.2): the ambushed side's units lose all their concealment; prisoners keep theirs.</summary>
    public static bool AmbushedLosesConcealment(string? ambusher, string unitSide, bool? concealed, bool? captured) =>
        ambusher is not null && unitSide != ambusher && concealed == true && captured != true;

    /// <summary>A11.4: the conditions an ambushed unit loses, in the record's order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> AmbushedConditions() => [(UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>The Ambush plan's words.</summary>
    public static string AmbushSummary(string locationText) => $"play.ambush: each side makes an Ambush dr for {locationText}; one at least three below the other ambushes (A11.4)";

    /// <summary>A4.152, A15.432 (ruling R27.3): the CC of a berserk Infantry OVR onto a lone SMC is resolved at once in the MPh.</summary>
    public static bool OverrunCc(string? phase, bool pendingHere) => phase == "mph" && pendingHere;

    /// <summary>A3.8, A11.1, A4.152: CC is resolved in the CCPh, or in the MPh after a berserk Infantry OVR onto a lone SMC.</summary>
    public static string? RoundPhaseBar(string? phase, bool overrun) =>
        phase != "ccph" && !overrun ? "play.cc-phase: CC is resolved in the CCPh, or in the MPh after a berserk Infantry OVR onto a lone SMC (A3.8, A11.1, A4.152)" : null;

    /// <summary>A11.31 (ruling R11.16): CC in a Location holding a vehicle is sequential, one attack at a time, with the vehicle CC action.</summary>
    public static string? VehiclePresentBar(string? vehicleId, string locationText) =>
        vehicleId is { } present ? $"play.cc-vehicle: {present} is in {locationText}, so its CC is sequential, one attack at a time (asl.game.vehicle-close-combat; A11.31)" : null;

    /// <summary>
    /// A15.432 (ruling R27.3): in the OVR's CC every berserk unit attacks the SMC together, and the SMC may attack them; at most two attacks, no capture,
    /// withdrawal, or Infiltration.
    /// </summary>
    public static string? OverrunAttacksBar(int phasingAttacks, bool phasingIsEveryBerserk, int attackCount, bool anyCapture, bool anyWithdrawal, bool anyInfiltration) =>
        phasingAttacks != 1 || !phasingIsEveryBerserk || attackCount > 2 || anyCapture || anyWithdrawal || anyInfiltration
            ? "play.cc-overrun: in the OVR's CC every berserk unit attacks the SMC together, and the SMC may attack them; no capture, withdrawal, or Infiltration (A4.152, A15.432, A20.21)"
            : null;

    /// <summary>A11.14: the ambushed side's round keeps the first round's SMC stacking once a round has been resolved in the Location.</summary>
    public static bool KeepsFirstRoundStacking(int roundsResolved) => roundsResolved > 0;

    /// <summary>A11.21, A4.43 (ruling R5.5): a withdrawing unit carries no more than its IPC; dropping the SW beyond it is not built.</summary>
    public static string? WithdrawalPortageBar(string unitId, bool laden) =>
        laden ? $"play.cc-withdrawal-portage: {unitId} carries more than its IPC and may not withdraw with it; dropping a SW is not built (A11.21, A4.43)" : null;

    /// <summary>A11.2, A11.21: a withdrawal is from Melee, to an ADJACENT Location the unit could advance into that holds no Known enemy unit.</summary>
    public static string? WithdrawalBar(string unitId, bool allowed) =>
        allowed ? null : $"play.cc-withdrawal: {unitId} withdraws only from Melee, to an ADJACENT Location it could advance into that holds no Known enemy unit (A11.2, A11.21)";

    /// <summary>A11.16: a broken unit in Melee that can withdraw must attempt it.</summary>
    public static string WithdrawRequiredText(string unitId) => $"play.cc-withdraw-required: {unitId} is broken in Melee and must attempt to withdraw (A11.16)";

    /// <summary>A11.22 (ruling R14.8): each Infiltration destination is one a withdrawal could reach.</summary>
    public static string? InfiltrationBar(string unitId, bool allowed) =>
        allowed ? null : $"play.cc-infiltration: {unitId} may infiltrate only to an ADJACENT Location a withdrawal could reach (A11.22, A11.21)";

    /// <summary>J2.31 (ruling R14.1), G1.64: Hand-to-Hand is declared only where an SSR allows it.</summary>
    public static string? HandToHandSsrBar(bool declared, bool ssrAllows) =>
        declared && !ssrAllows ? "play.cc-hand-to-hand: Hand-to-Hand CC is declared only where an SSR allows it (J2.31, G1.64)" : null;

    /// <summary>Referee, pass 14 (A25.43, G1.64): the ATTACKER declares Hand-to-Hand with the Location's first round other than the prisoners', unless it was ambushed.</summary>
    public static string? HandToHandTimingBar(bool declared, bool prisonersRound, bool roundsOtherThanPrisoners, bool ambushedByOther) =>
        declared && (prisonersRound || roundsOtherThanPrisoners || ambushedByOther)
            ? "play.cc-hand-to-hand: the ATTACKER declares Hand-to-Hand with the Location's first round, not in the prisoners' round, and not after being ambushed (J2.31, A25.43)"
            : null;

    /// <summary>
    /// What the Close Combat package asks for next in a round: the attack and the escape NTC take two dice, a leader stack or Random Selection one per unit
    /// named, others one; or the round is resolved, or left undecided.
    /// </summary>
    public static CloseCombatNextStep NextStep(string disposition, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        if (disposition == CloseCombatResolution.Resolved)
        {
            return new CloseCombatNextStep(true, null, null, [], 0, null, null);
        }

        if (reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc.roll-missing:", StringComparison.Ordinal))
        {
            return new CloseCombatNextStep(false, null, null, [], 0, null, string.Join("; ", reasons));
        }

        var key = missing["asl.a1.cc.roll-missing:".Length..];
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, rest) = (key[..split], key[(split + 1)..]);
        string[] selected = kind == "randomSelection" ? rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',') : [];
        if (kind == "leaderStack")
        {
            selected = rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',');
        }

        var (count, purpose) = kind switch
        {
            "attack" => (2, "cc-attack"),
            "escapeNtc" => (2, "cc-escape-ntc"),
            "leaderStack" => (selected.Length, "cc-leader-stack"),
            "randomSelection" => (selected.Length, "cc-random-selection"),
            "woundSeverity" => (1, "cc-wound-severity"),
            "leaderCreation" => (1, "cc-leader-creation"),
            _ => (1, "cc-weapon-loss"),
        };
        return new CloseCombatNextStep(false, kind, rest, selected, count, purpose, null);
    }

    /// <summary>The key a roll is kept under: the whole key for most, the attack's part for a leader stack or a Random Selection of one unit.</summary>
    public static string SelectionKey(string rest, string unitId) => rest[..rest.IndexOf(':', StringComparison.Ordinal)] + ":" + unitId;

    /// <summary>A4.152 (ruling R27.3): an SMC that survives the OVR's CC and the units that OVR it are held in Melee, to fight again in the CCPh.</summary>
    public static bool OverrunHoldsInMelee(bool overrun, int sidesLeft) => overrun && sidesLeft > 1;

    /// <summary>The attacks in words.</summary>
    public static string DescribedAttacks(IReadOnlyList<CloseCombatDeclaration> attacks)
    {
        ArgumentNullException.ThrowIfNull(attacks);
        return attacks.Count == 0 ? "no attacks"
            : string.Join("; ", attacks.Select(item => $"{string.Join(", ", item.Attackers!)} {(item.Capture == true ? "attempt to capture" : "attack")} {string.Join(", ", item.Defenders!)}"
                + (item.Director is { } director ? $", directed by {director}" : string.Empty)));
    }

    /// <summary>The round's words (A11.11, A11.12).</summary>
    public static string RoundSummary(CloseCombatFacts facts, string locationText, string described)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return $"play.cc: {facts.Round} round in {locationText}{(facts.HandToHand == true ? ", Hand-to-Hand" : string.Empty)}: {described} (A11.11, A11.12)";
    }

    /// <summary>
    /// A11.21 (A4.72; ruling R5.5): an ADJACENT Location a unit held in Melee may withdraw to: playable, enterable by an advance (not for all MF), not Difficult
    /// Terrain for a CX unit, and holding no enemy unit other than a prisoner.
    /// </summary>
    public static bool WithdrawalDestination(bool playable, bool enterable, bool? difficult, bool cx, bool enemyThere) =>
        playable && enterable && difficult is { } hard && !(hard && cx) && !enemyThere;

    /// <summary>A11.21, A4.72 (ruling R5.5): a withdrawal an advance could make only by becoming CX makes the unit CX.</summary>
    public static bool WithdrawalTires(bool enterable, bool? difficult) => enterable && difficult == true;

    /// <summary>A11.16: a broken unit held in Melee, not Disrupted, captured, or a Guard, must attempt to withdraw when it has a destination.</summary>
    public static bool MustWithdraw(bool broken, bool melee, bool disrupted, bool captured, Func<bool> guard, Func<bool> hasDestination)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(hasDestination);
        return broken && melee && !disrupted && !captured && !guard() && hasDestination();
    }

    /// <summary>A15.43, A11.31 (ruling R27.2): a berserk unit owing an attack on a Known enemy vehicle attacks it in the Location's sequential CC first.</summary>
    public static string CcRequiredVehicleText(string unitId, string locationText) =>
        $"play.cc-required: {unitId} is berserk with a Known enemy vehicle in {locationText}, so it attacks the vehicle in CC before the CCPh ends (A15.43, A11.31)";

    /// <summary>
    /// A15.43, A11.15: the unit whose CC the Location still requires: a berserk unit with a Known enemy there, else, where a Melee is held, an unbroken unit
    /// that advanced in this APh; null when a crew or a vehicle is there (CC with them is not reviewed or is sequential) or none qualifies. The flag says
    /// which rule.
    /// </summary>
    public static (string Id, bool Berserk)? RequiredUnit(IReadOnlyList<RequiredCcUnitFacts> here)
    {
        ArgumentNullException.ThrowIfNull(here);
        if (here.Any(unit => unit.Crew || unit.Vehicle))
        {
            return null;
        }

        var berserk = here.FirstOrDefault(unit => unit.Berserk && here.Any(other => other.Side != unit.Side && other.Known));
        var reinforcing = here.Any(unit => unit.Melee) ? here.FirstOrDefault(unit => unit.AdvancedHere && !unit.Broken) : null;
        return berserk is not null ? (berserk.Id, true) : reinforcing is not null ? (reinforcing.Id, false) : null;
    }

    /// <summary>A15.43, A11.15: why the CCPh may not end.</summary>
    public static string CcRequiredText(string unitId, string locationText, bool berserk) => berserk
        ? $"play.cc-required: {unitId} is berserk with a Known enemy unit in {locationText}, so it attacks in CC before the CCPh ends (A15.43)"
        : $"play.cc-required: {unitId} advanced into the Melee in {locationText}, so it attacks in CC before the CCPh ends (A11.15)";

    /// <summary>
    /// Ruling R14.14 (referee, pass 14): the units of a trial attack by one unit alone: the unit, every unit of the other side, every prisoner, and the
    /// Guards of the prisoners kept.
    /// </summary>
    public static IReadOnlyList<CloseCombatUnit> TrialUnits(IReadOnlyList<CloseCombatUnit> units, string unitId, string side)
    {
        ArgumentNullException.ThrowIfNull(units);
        var kept = units.Where(item => item.UnitId == unitId || item.Side != side || item.Captured == true).ToList();
        kept.AddRange(units.Where(item => item.Side == side && item.UnitId != unitId && kept.Any(prisoner => prisoner.GuardId == item.UnitId)));
        return kept;
    }

    /// <summary>
    /// A11.4, A11.3, A11.32, A11.11: the sides that may declare CC attacks now, phasing side first: after an Ambush, the ambusher until its first round is
    /// resolved or when it attacks again, otherwise the ambushed side; both in a simultaneous round.
    /// </summary>
    public static IReadOnlyList<string> DeclaringSides(IReadOnlyList<string> sidesPhasingFirst, string? ambusher, int rounds, bool ambusherAgain)
    {
        ArgumentNullException.ThrowIfNull(sidesPhasingFirst);
        return ambusher is { } side ? rounds == 0 || ambusherAgain ? [side] : [.. sidesPhasingFirst.Where(item => item != side)] : sidesPhasingFirst;
    }

    /// <summary>A11.4 (ruling R11.16): Ambush drs may be due in the CCPh where no CC has begun and no vehicle is present; the package then decides.</summary>
    public static bool AmbushDueCandidate(string? phase, bool ccBegun, bool vehicleThere) => phase == "ccph" && !ccBegun && !vehicleThere;

    /// <summary>Why the Close Combat package would refuse a Location's Ambush drs, each reason as its sentence (pass 31d, design D9).</summary>
    public static string AmbushUndecidedText(IReadOnlyList<string> reasons, Func<string, string> explain)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(explain);
        return string.Join("; ", reasons.Select(reason => explain(reason).Split(": ", 2) is [_, var sentence] ? sentence : reason));
    }

    /// <summary>Pass 31d (design D9; ruling R31c.5): a Location with an Ambush and a round whose ambushed side has no unit left holds the phase no longer.</summary>
    public static bool NothingLeft(bool ambushed, int rounds, bool ambushedUnitLeft) => ambushed && rounds > 0 && !ambushedUnitLeft;

    /// <summary>What an open CC Location still owes (A11.31, A11.12, A11.3, A11.32).</summary>
    public static string DueOpenLine(string locationText, string? next, string? ambusher) =>
        next is { } side ? $"{locationText}: the {side} side attacks or passes (A11.31)"
            : ambusher is null ? $"{locationText}: its round after the Ambush drs (A11.12)"
            : $"{locationText}: more attacks by the {ambusher} side, then the ambushed side's round, which may declare no attacks (A11.3, A11.32)";

    /// <summary>A11.4 (pass 31d, design D9): the Ambush drs a Location owes, or why the package does not decide it.</summary>
    public static string DueAmbushLine(string locationText, string? undecided) =>
        undecided is { } why ? $"{locationText}: the Close Combat package does not decide this Location, since {why}" : $"{locationText}: the Ambush drs (A11.4)";

    /// <summary>A11.16: a broken unit in Melee that must attempt to withdraw.</summary>
    public static string DueWithdrawLine(string locationText, string unitId) => $"{locationText}: {unitId} is broken in Melee and must attempt to withdraw (A11.16)";

    /// <summary>
    /// A15.431, A15.432 (rulings R10.15, R24.3, R30.5; table player, pass 27): why a berserk unit may not step into a Location: it holds a Gun's crew, or an
    /// enemy vehicle together with enemy Infantry; null when the step is allowed.
    /// </summary>
    public static string? ChargeBarred(bool crewThere, bool enemyVehicleThere, bool enemyInfantryThere, string toText) =>
        crewThere ? $"play.berserk-crew: a charge into {toText}, which holds a Gun's crew, is CC with a crew, which is not reviewed (C11, R24.3); the charge ends in place (ruling R30.5)"
            : enemyVehicleThere && enemyInfantryThere
                ? $"play.berserk-vehicle: a charge into {toText}, which holds an enemy vehicle and enemy Infantry, needs CC between Infantry beside a vehicle, which is not built (R11.16); the charge ends in place (ruling R30.5)"
                : null;
}
