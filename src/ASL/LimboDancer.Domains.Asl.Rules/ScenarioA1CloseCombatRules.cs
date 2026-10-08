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
}
