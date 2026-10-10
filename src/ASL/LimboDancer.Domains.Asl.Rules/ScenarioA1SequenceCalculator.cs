namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The sequence of play (pass 32.i, S2): who may propose what (pass 31, ruling R31.6), the owners' options and the Massacre (rulings R5.7, R5.8), and, as the
/// later commits add them, the bars every request meets, the phase's end and what it removes and requires, and the game's start. Play reads the request and
/// the state, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1SequenceCalculator
{
    // Who may propose what (pass 31, ruling R31.6).

    /// <summary>The arguments that name the units an action is taken by; an action not listed is taken by no single side's units.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ActorArguments
    {
        get;
    } = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        ["asl.game.rout"] = ["unitId"],
        ["asl.game.deploy"] = ["squadId"],
        ["asl.game.recombine"] = ["halfSquads"],
        ["asl.game.transfer"] = ["unitId"],
        ["asl.game.drop"] = ["unitId"],
        ["asl.game.recover"] = ["unitId"],
        ["asl.game.dismantle"] = ["unitId"],
        ["asl.game.enter-empty-building"] = ["unitId"],
        ["asl.game.enter-building"] = ["unitId"],
        ["asl.game.declare-overrun"] = ["unitId"],
        ["asl.game.fire"] = ["firers"],
        ["asl.game.opportunity-fire"] = ["unitIds"],
        ["asl.game.rally"] = ["unitId"],
        ["asl.game.repair"] = ["unitId", "equipmentId"],
        ["asl.game.move"] = ["unitIds"],
        ["asl.game.end-move"] = ["unitIds"],
        ["asl.game.advance"] = ["unitIds"],
        ["asl.game.ambush-withdraw"] = ["unitIds"],
        ["asl.game.guard-prisoners"] = ["guardId"],
        ["asl.game.fire-ordnance"] = ["gunId"],
        ["asl.game.recover-shock"] = ["vehicleId"],
        ["asl.game.turn-gun"] = ["gunId"],
        ["asl.game.hook-gun"] = ["vehicleId"],
        ["asl.game.move-vehicle"] = ["vehicleId"],
        ["asl.game.overrun"] = ["vehicleId"],
        ["asl.game.button-up"] = ["vehicleId"],
        ["asl.game.massacre"] = ["unitId"],
        ["asl.game.throw-dc"] = ["unitId"],
        ["asl.game.fire-starshell"] = ["unitId"],
        ["asl.game.place-hidden"] = ["unitIds"],
        ["asl.game.mop-up"] = ["unitIds"],
    };

    /// <summary>The side that ends the present phase: either side in the RPh, RtPh, and CCPh, where both act; the DEFENDER in the DFPh; else the ATTACKER.</summary>
    public static string? PhaseEndedBy(string? phase, string proposer, string? defender, string? phasing) =>
        phase is "rph" or "rtph" or "ccph" ? proposer
            : phase == "dfph" ? defender ?? phasing
            : phasing;

    /// <summary>A view that is not one of the game's sides is not checked: the game has no side to hold it to.</summary>
    public static bool ProposerChecked(bool proposerIsSide) => proposerIsSide;

    /// <summary>
    /// Whose action a proposal is (ruling R31.6): the end of a phase is the side's that ends it, the pass the DEFENDER's, a choice the side's it was put to, a
    /// surrender the captors' side's; any other action is no one side's here. Each side is read only for its action.
    /// </summary>
    public static ProposalOwner Owner(string action, Func<string?> phaseEndedBy, string? defender, Func<string?> choiceSide, Func<string?> captorSide)
    {
        ArgumentNullException.ThrowIfNull(phaseEndedBy);
        ArgumentNullException.ThrowIfNull(choiceSide);
        ArgumentNullException.ThrowIfNull(captorSide);
        return action switch
        {
            "asl.game.advance-phase" => new ProposalOwner(phaseEndedBy(), "ends this phase"),
            "asl.game.pass-fire" => new ProposalOwner(defender, "is the DEFENDER and passes"),
            "asl.game.choose" => new ProposalOwner(choiceSide(), "answers this choice"),
            "asl.game.take-prisoner" => new ProposalOwner(captorSide(), "answers this surrender"),
            _ => new ProposalOwner(null, string.Empty),
        };
    }

    /// <summary>Ruling R31.6: the action belongs to another side's view.</summary>
    public static string? NotYourActionBar(ProposalOwner owner, string proposer)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return owner.Side is not null && owner.Side != proposer
            ? $"play.not-your-action: the {owner.Side} side {owner.What}, not the {proposer} side (ruling R31.6)"
            : null;
    }

    /// <summary>
    /// Table player, pass 31: either side ends the Rout Phase, but not while the other side still has a unit that must rout: the end would eliminate it, or
    /// make it surrender, on its opponent's word (A10.5, A20.21). <paramref name="owedSide"/> reads the side of such a unit, only for the RtPh's end: a
    /// unit that still owes a rout (<see cref="ScenarioA1RoutCalculator.RoutStillOwed"/>; pass 35, task 35.17), since one that has routed, is pinned, has
    /// no legal step, or surrenders instead can do nothing more, and waiting for it would keep the phase from ever ending.
    /// </summary>
    public static string? RoutPhaseEndBar(string action, string? phase, Func<string?> owedSide)
    {
        ArgumentNullException.ThrowIfNull(owedSide);
        return action == "asl.game.advance-phase" && phase == "rtph" && owedSide() is { } owed
            ? $"play.not-your-action: the {owed} side still has a unit that must rout, and ending the Rout Phase now would eliminate it or make it surrender; hand over to the {owed} side first (A10.5; ruling R31.6)"
            : null;
    }

    /// <summary>Ruling R31.6: a side's view does not act for another side's unit.</summary>
    public static string? NotYourUnitBar(string id, string? side, string proposer) =>
        side is not null && side != proposer ? $"play.not-your-unit: {id} belongs to the {side} side; the {proposer} side's view does not act for it (ruling R31.6)" : null;

    // The bars every request meets, in their order (pass 32.i); each read of the game is made only when its bar is checked.

    /// <summary>Pass 20 (ruling R20.1): nothing happens in a game that has ended.</summary>
    public static string? GameOverBar(int? endedAfterTurn) => endedAfterTurn is { } turn ? $"play.game-over: the game ended after Game Turn {turn} (A3.9; ruling R20.1)" : null;

    /// <summary>Pass 19 (ruling R19.2; referee, pass 19): in a game from a card nothing but setup happens until every group has set up.</summary>
    public static string? SetupOnlyBar(string action, bool anyEvents, Func<string?> cardSetupIncomplete)
    {
        ArgumentNullException.ThrowIfNull(cardSetupIncomplete);
        return action != "asl.game.setup" && anyEvents && cardSetupIncomplete() is { } unfinished ? unfinished : null;
    }

    /// <summary>Pass 31 (ruling R31.6): a side's view proposes only what its side may do; setup is guarded by its own checks of each group's side.</summary>
    public static string? ProposerViewBar(string action, string? proposer, bool anyEvents, Func<string?> proposerBar)
    {
        ArgumentNullException.ThrowIfNull(proposerBar);
        return action != "asl.game.setup" && proposer is not null && anyEvents && proposerBar() is { } notYours ? notYours : null;
    }

    /// <summary>Ruling R26.2: a Passenger acts only with its vehicle until it unloads; eight actions are exempt.</summary>
    public static string? PassengerBar(string action, bool anyEvents, Func<string?> aboardBar)
    {
        ArgumentNullException.ThrowIfNull(aboardBar);
        return action is not ("asl.game.setup" or "asl.game.move-vehicle" or "asl.game.hook-gun" or "asl.game.advance-phase" or "asl.game.choose" or "asl.game.pass-fire"
            or "asl.game.end-move" or "asl.game.button-up") && anyEvents && aboardBar() is { } aboard ? aboard : null;
    }

    /// <summary>A4.152 (ruling R27.3): once the DEFENDER's window on a berserk OVR's entry closes, its CC comes first; <paramref name="overrunAt"/> reads where it is pending.</summary>
    public static string? OverrunCloseCombatFirstBar(string action, bool anyEvents, Func<string?> overrunAt)
    {
        ArgumentNullException.ThrowIfNull(overrunAt);
        return action is not ("asl.game.close-combat" or "asl.game.choose" or "asl.game.take-prisoner") && anyEvents && overrunAt() is { } at
            ? $"play.cc-overrun-first: the berserk Infantry OVR in {at} has its CC at once, before anything else happens (A4.152, A15.432)"
            : null;
    }

    /// <summary>
    /// Whether a counter is one whose rules are not built (pass 35, task 35.15): a fortification (wire, foxhole, trench, minefield, roadblock, pillbox,
    /// Fortified Building Location; B23.9, B26 to B30), a rubble counter (B24), or a Flame (B25.15). Passes 115 and 120 build them.
    /// </summary>
    public static bool UnbuiltCounter(string kind, bool fortification) => fortification || kind is "asl:rubble" or "asl:flame";

    /// <summary>
    /// Pass 35, task 35.15: a game that places or holds a counter whose rules are not built is refused, where until now it played as if the counter
    /// were absent. <paramref name="kinds"/> are the unbuilt kinds as the vocabulary names them, read when the game has events or is being set up.
    /// </summary>
    public static string? UnbuiltCounterBar(bool setup, IReadOnlyList<string> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        return kinds.Count == 0 ? null
            : $"play.unbuilt-counters: this game {(setup ? "would place" : "holds")} {string.Join(", ", kinds)} counters, whose rules are not built yet, so "
                + $"{(setup ? "it is not set up with them" : "it cannot be played")} (B23.9, B24, B25.15, B26 to B30)";
    }

    /// <summary>Ruling R5.8: a pending choice is answered before anything else happens in the game; <paramref name="pendingChoice"/> reads its side and its words.</summary>
    public static string? ChoicePendingBar(bool ready, string action, Func<(string Side, string Described)?> pendingChoice)
    {
        ArgumentNullException.ThrowIfNull(pendingChoice);
        return ready && action is not ("asl.game.choose" or "asl.game.setup") && pendingChoice() is { } choice
            ? $"play.choice-pending: the {choice.Side} side answers first: {choice.Described}"
            : null;
    }

    /// <summary>A15.5: a surrender waits for its captor before anything else happens in the game; <paramref name="surrenderedUnit"/> reads the first unit that waits.</summary>
    public static string? SurrenderPendingBar(bool ready, string action, Func<string?> surrenderedUnit)
    {
        ArgumentNullException.ThrowIfNull(surrenderedUnit);
        return ready && action is not ("asl.game.take-prisoner" or "asl.game.setup") && surrenderedUnit() is { } pending
            ? $"play.surrender-pending: {pending} has surrendered; its captor's side chooses the Guard or rejects it first (A15.5, A20.3)"
            : null;
    }

    /// <summary>A10.62, A20.551, pass 21 (rulings R13.1, R31.8, R21.4): the follow-ups of a plan (ADJACENT DM, the re-armed SMC, an immediate Victory) apply to every action but setup.</summary>
    public static bool FollowUpsApply(string action) => action != "asl.game.setup";

    // The phase's end (A3.1 to A3.9) and what it requires, in the planner's order (pass 32.i); the other slices' reads cross as delegates or as facts.

    /// <summary>The sequence of play alternates two sides.</summary>
    public static string? TwoSidesBar(int sideCount) => sideCount != 2 ? "play.two-sides: the sequence of play alternates two sides" : null;

    /// <summary>A12.15: an entry attempt is resolved within its phase, so the phase cannot advance while one awaits the attacker's OVR declaration.</summary>
    public static string? DeclarationPendingBar(string? openAttempt, string? unit) =>
        openAttempt is not null
            ? $"play.declaration-pending: the entry attempt '{openAttempt}' by " + $"{unit} awaits the attacker's OVR declaration (A12.15, p. 78), so the phase cannot advance"
            : null;

    /// <summary>C7.42 (ruling R7.8): the RPh does not end while a Shocked AFV or an Unconfirmed Kill owes its dr.</summary>
    public static string? ShockPendingBar(string? shockedId) =>
        shockedId is not null ? $"play.shock-recovery-pending: {shockedId} makes its Shock or Unconfirmed Kill dr before the RPh ends (C7.42)" : null;

    /// <summary>A25.222 (ruling R15.6): a broken unit a Commissar must still attempt to rally in the RPh: active, broken, not a prisoner, no attempt this Player Turn, no RPh action.</summary>
    public static bool CommissarCandidate(string? phase, bool active, bool broken, bool captured, bool attemptedThisPlayerTurn, bool rallyPhaseAction) =>
        phase == "rph" && active && broken && !captured && !attemptedThisPlayerTurn && !rallyPhaseAction;

    /// <summary>A25.222: the Commissar owes the attempt when he has taken no RPh action and the Rally package would decide it.</summary>
    public static bool CommissarOwes(bool commissarActed, bool rallyReady) => !commissarActed && rallyReady;

    /// <summary>A25.222: the RPh does not end while a Commissar owes an attempt.</summary>
    public static string CommissarRallyText(string commissarId, string brokenId) => $"play.commissar-rally: {commissarId} must attempt to rally {brokenId} before the RPh ends (A25.222)";

    /// <summary>
    /// A3.1 to A3.8: the eight phases in order; after the CCPh the other side's Player Turn begins, and a new Game Turn begins when the side that moved first is
    /// phasing again.
    /// </summary>
    public static (int Turn, string Phase, string Phasing) NextPhase(string phase, int turn, string phasing, string otherSide, string? firstSide)
    {
        var phases = ScenarioA1Definitions.Phases;
        var index = phases.ToList().IndexOf(phase);
        var (nextTurn, nextPhase, nextPhasing) = index < phases.Count - 1 ? (turn, phases[index + 1], phasing) : (turn, phases[0], otherSide);
        if (index == phases.Count - 1 && nextPhasing == firstSide)
        {
            nextTurn++;
        }

        return (nextTurn, nextPhase, nextPhasing);
    }

    /// <summary>A3.8: whether the phase ending is the Player Turn's last.</summary>
    public static bool LastPhase(string phase) => phase == ScenarioA1Definitions.Phases[^1];

    /// <summary>A23.4 (ruling R15.2): an operably Placed DC detonates before the AFPh ends, unless the Fire package refuses its attack.</summary>
    public static bool PlacedChargeDue(string? phase, bool operable, Func<bool> detonationReady)
    {
        ArgumentNullException.ThrowIfNull(detonationReady);
        return phase == "afph" && operable && detonationReady();
    }

    /// <summary>A23.4: the AFPh does not end while a Placed DC could still detonate.</summary>
    public static string DcDetonatePendingText(string charge, string target) => $"play.dc-detonate-pending: {charge} was Placed in {target} and detonates before the AFPh ends (A23.4)";

    /// <summary>A2.5 (rulings R20.5, R25.1): the APh does not end while a unit whose entry turn has come waits off board and may still enter by advance.</summary>
    public static string? EntryDueBar(string? phase, Func<string?> entryDue)
    {
        ArgumentNullException.ThrowIfNull(entryDue);
        return phase == "aph" ? entryDue() : null;
    }

    /// <summary>A2.5 (ruling R26.1): vehicles cannot advance, so the MPh does not end while a vehicle whose entry turn has come waits off board and may enter.</summary>
    public static string? VehicleEntryDueBar(string? phase, Func<string?> vehicleEntryDue)
    {
        ArgumentNullException.ThrowIfNull(vehicleEntryDue);
        return phase == "mph" ? vehicleEntryDue() : null;
    }

    /// <summary>A3.9 (ruling R20.1): the game from a card ends after its last Game Turn, or after the first side's Player Turn of a half turn; the card's reading is asked only then.</summary>
    public static bool GameEnds(bool lastPhase, Func<bool?> cardEndsAfter)
    {
        ArgumentNullException.ThrowIfNull(cardEndsAfter);
        return lastPhase && cardEndsAfter() == true;
    }

    /// <summary>A10.62 (ruling R13.1): DM is retained as the RPh ends.</summary>
    public static string? RetainDmPhaseBar(int retainedCount, string? phase) => retainedCount > 0 && phase != "rph" ? "play.retain-dm: DM is retained as the RPh ends (A10.62)" : null;

    /// <summary>A10.5, A20.21 (ruling R13.3): as the RtPh ends, a broken unit that failed to rout is eliminated, or surrenders to its captors first.</summary>
    public static bool FailuresToRoutDue(string? phase) => phase == "rtph";

    /// <summary>A20.21: a unit that failed to rout surrenders to its captors.</summary>
    public static string FailureToRoutSurrenderText(string unitId, string why, IReadOnlyList<string> captors)
    {
        ArgumentNullException.ThrowIfNull(captors);
        return $"play.failure-to-rout-surrender: {unitId} would be eliminated for Failure to Rout ({why}), so it surrenders to {string.Join(" or ", captors)} (A20.21); advance the phase again once its captor's side has chosen";
    }

    /// <summary>A10.5: a unit that failed to rout with no captors is eliminated.</summary>
    public static string FailureToRoutText(string unitId, string why) => $"play.failure-to-rout: {unitId} is eliminated for Failure to Rout: {why} (A10.5)";

    /// <summary>A15.43: the MPh does not end while a berserk unit must still charge; <paramref name="charging"/> reads the first such unit, only in the MPh.</summary>
    public static string? BerserkChargeBar(string? phase, Func<string?> charging)
    {
        ArgumentNullException.ThrowIfNull(charging);
        return phase == "mph" && charging() is { } unit ? $"play.berserk-charge: {unit} is berserk and must charge before the MPh ends (A15.43)" : null;
    }

    /// <summary>D2.4: a vehicle under a Motion counter of the phasing side that spent nothing and has not ended its move.</summary>
    public static bool IdleMotionVehicle(string? phase, bool active, bool phasing, bool vehicle, bool motion, int mfSpent, bool halfMfSpent, bool movementEnded) =>
        phase == "mph" && active && phasing && vehicle && motion && mfSpent == 0 && !halfMfSpent && !movementEnded;

    /// <summary>D2.4: a vehicle under a Motion counter must expend at least one MP in its MPh.</summary>
    public static string VehicleMotionText(string vehicleId) => $"play.vehicle-motion: {vehicleId} is in Motion and must expend at least one MP this MPh (D2.4)";

    /// <summary>D5.341 (ruling R5.17): a Recalled AFV of the phasing side whose move has not ended, in the MPh, must move toward its Friendly Board Edge when its route is decided.</summary>
    public static bool RecallMoveDue(string? phase, bool active, bool phasing, bool mustLeave, bool movementEnded, Func<bool> routeDecided)
    {
        ArgumentNullException.ThrowIfNull(routeDecided);
        return phase == "mph" && active && phasing && mustLeave && !movementEnded && routeDecided();
    }

    /// <summary>D5.341: the MPh does not end before the Recalled AFV has moved.</summary>
    public static string RecallMoveText(string vehicleId) => $"play.recall-move: {vehicleId} is Recalled and must move off by its side's Friendly Board Edge this MPh (D5.341)";

    /// <summary>A15.431, A15.46: at the end of its MPh a berserk unit not in Melee with no Known enemy unit in its LOS returns to normal; the LOS read is asked only then.</summary>
    public static bool BerserkReturns(string? phase, bool active, bool phasing, bool berserk, bool melee, bool onMap, Func<bool?> knownEnemyInLos)
    {
        ArgumentNullException.ThrowIfNull(knownEnemyInLos);
        return phase == "mph" && active && phasing && berserk && !melee && onMap && knownEnemyInLos() == false;
    }

    /// <summary>A15.431, A15.46: the unit returns to normal.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> BerserkEndsConditions() => [(UnitCondition.Berserk, false)];

    /// <summary>A15.431, A15.46, in words.</summary>
    public static string BerserkEndsText(string unitId) => $"play.berserk-ends: {unitId} sees no Known enemy unit and returns to normal (A15.431, A15.46)";

    /// <summary>A11.16, A19.12 (ruling R29.11): a broken unit held in Melee in a Location whose CC is not yet declared must attempt to withdraw first; the withdrawal read is asked only then.</summary>
    public static bool WithdrawalRequired(string? phase, bool active, bool onMap, bool closeCombatDeclared, Func<bool> mustWithdraw)
    {
        ArgumentNullException.ThrowIfNull(mustWithdraw);
        return phase == "ccph" && active && onMap && !closeCombatDeclared && mustWithdraw();
    }

    /// <summary>A11.16: the CCPh does not end before the withdrawal attempt.</summary>
    public static string CcWithdrawRequiredText(string unitId) => $"play.cc-withdraw-required: {unitId} is broken in Melee and must attempt to withdraw in its Location's CC first (A11.16)";

    /// <summary>A15.43, A11.15: a Location whose CC the package can resolve has its round before the CCPh ends; the required read is asked only in the CCPh.</summary>
    public static string? CloseCombatRequiredBar(string? phase, Func<string?> closeCombatRequired)
    {
        ArgumentNullException.ThrowIfNull(closeCombatRequired);
        return phase == "ccph" ? closeCombatRequired() : null;
    }

    /// <summary>A11.16, A19.12: a broken or Disrupted unit held in Melee, not captured and not a Guard, is eliminated at the end of the CCPh.</summary>
    public static bool MeleeEliminated(string? phase, bool active, bool melee, bool captured, bool guard, bool broken, bool disrupted) =>
        phase == "ccph" && active && melee && !captured && !guard && (broken || disrupted);

    /// <summary>A11.16, A19.12, in words.</summary>
    public static string MeleeEliminatedText(string unitId) => $"play.melee-eliminated: {unitId} is broken or Disrupted in Melee and cannot withdraw, so it is eliminated (A11.16, A19.12)";

    /// <summary>Pass 31d (design D9; A11.15; ruling R31d.6): a unit that counts toward a Location of both sides: active, not a prisoner, not a Dummy, on the map.</summary>
    public static bool UnfoughtCandidate(string? phase, bool active, bool captured, bool dummy, bool onMap) => phase == "ccph" && active && !captured && !dummy && onMap;

    /// <summary>A11.15: a Location holding units of both sides in which no round was fought this phase.</summary>
    public static bool Unfought(int sideCount, bool anyRound) => sideCount > 1 && !anyRound;

    /// <summary>A11.15 (ruling R31c.5): said before the phase ends, as a consequence and not a refusal.</summary>
    public static string CcUnfoughtText(string location) =>
        $"play.cc-unfought: no Close Combat was fought in {location} this phase; the units of both sides stay there, held in Melee unless they keep their \"?\" (A11.15)";

    /// <summary>A12.12, A12.122 (ruling R12.5): as a Player Turn ends, the phasing side's Good Order Infantry may gain "?", unless the game ends.</summary>
    public static bool ConcealmentGainsDue(bool ending, string? phase, bool newPlayerTurn) => !ending && phase == "ccph" && newPlayerTurn;

    /// <summary>The phase change in words.</summary>
    public static string AdvanceText(int turn, string phase, string phasing) => $"play.advance: turn {turn}, {phase}, {phasing} phasing";

    /// <summary>A12.122: the units that make a Final Concealment dr, in words.</summary>
    public static string ConcealmentRollText(IReadOnlyList<string> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return $"play.concealment: {string.Join(", ", units)} make a Final Concealment dr (A12.122)";
    }

    /// <summary>B25.65: the Wind Change DR is made as the RPh begins.</summary>
    public static string WindChangeDueText() => "play.wind-change: the Wind Change DR is made as the RPh begins (B25.65)";

    // What follows the phase change (pass 32.i): the game's end, the DM of the night and of the RtPh, the captured vehicle, the Recalled AFV's crew.

    /// <summary>A3.9 (ruling R20.1): why the game ends: after its last Game Turn, or after the first side's Player Turn of a half turn.</summary>
    public static string GameEndReason(bool turnAdvanced) => turnAdvanced ? "last-game-turn" : "half-turn";

    /// <summary>E1.54 (ruling R16.6): at night DM leaves only with a Rally Original DR at most the printed morale.</summary>
    public static bool NightDmShed(int originalDr, int printedMorale) => originalDr <= printedMorale;

    /// <summary>E1.54: as the RPh ends at night, a broken DM unit that shed no DM and is not already retained keeps it.</summary>
    public static bool NightDmKept(string? phase, bool night, bool active, bool broken, bool desperationMorale, bool shed, bool retained) =>
        phase == "rph" && night && active && broken && desperationMorale && !shed && !retained;

    /// <summary>A10.62: a unit under DM stays or comes under it.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> DesperationMoraleConditions() => [(UnitCondition.DesperationMorale, true)];

    /// <summary>E1.54, in words.</summary>
    public static string NightDmText(string unitId) => $"play.night-dm: {unitId} keeps its DM: at night DM leaves only with a Rally Original DR at most the printed morale (E1.54)";

    /// <summary>A10.62 (ruling R13.1): the DM its owner keeps as the RPh ends, in words.</summary>
    public static string RetainDmText(string unitId) => $"play.retain-dm: {unitId} keeps its DM (A10.62)";

    /// <summary>A10.62: units come under DM as the RtPh begins.</summary>
    public static bool RoutPhaseDmDue(string nextPhase) => nextPhase == "rtph";

    /// <summary>A10.62, in words.</summary>
    public static string RoutPhaseDmText(string unitId, string why) => $"play.dm: {unitId} comes under DM {why} as the RtPh begins (A10.62)";

    /// <summary>A11.52 (ruling R11.16): an unarmed vehicle alone with enemy Infantry is captured as the CCPh begins.</summary>
    public static bool VehicleCaptureDue(string nextPhase) => nextPhase == "ccph";

    /// <summary>A11.52, A21.2: the captured vehicle is Abandoned too.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CapturedVehicleConditions() => [(UnitCondition.Captured, true), (UnitCondition.Abandoned, true)];

    /// <summary>A11.52, in words.</summary>
    public static string CcVehicleCaptureText(string vehicleId) =>
        $"play.cc-vehicle-capture: {vehicleId} is unarmed and alone with enemy Infantry, so it is captured; the use of captured vehicles is not built (A11.52, A21.2)";

    /// <summary>
    /// D5.341, D5.41 (ruling R5.18): at the end of a Player Turn, a Recalled AFV that is immobilized, or bogged (p. 203; pass 35, task 35.13 g), and not yet
    /// Abandoned is Abandoned by its crew.
    /// </summary>
    public static bool RecallAbandoned(bool newPlayerTurn, bool active, bool vehicle, bool recalled, bool immobilized, bool abandoned, bool bogged = false) =>
        newPlayerTurn && active && vehicle && recalled && (immobilized || bogged) && !abandoned;

    /// <summary>D5.341, D5.41, in words.</summary>
    public static string RecallAbandonedText(string vehicleId, bool bogged = false) =>
        $"play.recall-abandoned: {vehicleId} is Recalled and {(bogged ? "bogged" : "immobilized")}, so its crew Abandons it (D5.341, D5.41)";

    // The owners' options (ruling R5.8) and the Massacre (A20.4; ruling R5.7).

    /// <summary>The unit a resolution's option key names: the key is kind:subject, and the subject starts with the unit.</summary>
    public static (string Kind, string Unit) OptionKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, subject) = (key[..split], key[(split + 1)..]);
        return (kind, subject.Split(':')[0]);
    }

    /// <summary>
    /// The pending choice for an option a package asks for (ruling R5.8): Battle Hardening and the Leader Creation dr are the unit's owner's, the Unlikely Kill dr
    /// the firing side's, the side other than the vehicle's; a unit no longer in the game is the phasing side's.
    /// </summary>
    public static (ChoiceKind Kind, string Side) PendingChoice(string kind, string? unitSide, string? otherSide, string phasing)
    {
        var owner = unitSide ?? phasing;
        return kind switch
        {
            "battleHardening" => (ChoiceKind.BattleHardening, owner),
            "leaderCreation" => (ChoiceKind.LeaderCreation, owner),
            _ => (ChoiceKind.UnlikelyKill, otherSide ?? phasing),
        };
    }

    /// <summary>What a pending choice asks, in words, for the Play page and the plan's reasons; <paramref name="subject"/> is the key after its kind.</summary>
    public static string DescribeChoice(ChoiceKind kind, string subject, IReadOnlyList<string> options)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(options);
        return kind switch
        {
            ChoiceKind.BattleHardening => $"{subject.Split(':')[0]} may be Battle Hardened; its owner takes it or refuses it (A15.3)",
            ChoiceKind.LeaderCreation => $"{subject} rolled an Original 2 on its Self-Rally; its side may make the Leader Creation dr or decline it (A18.11)",
            ChoiceKind.UnlikelyKill => $"an Original 2 on the Vehicle line: the firer may make the Unlikely Kill dr against {subject} or decline it (A7.309)",
            ChoiceKind.Paatc => $"a vehicle entered concealed units' Location ({subject}): their owner reveals them or takes one combined PAATC (A12.41)",
            _ => $"the acquired units are in {string.Join(" and ", options)}; the Gun's side chooses which Location keeps the Acquisition (C6.51)",
        };
    }

    /// <summary>Ruling R5.8: an answer names the pending choice; this one did not.</summary>
    public static string NoChoiceText(string key) => $"play.no-choice: '{key}' is not the pending choice";

    /// <summary>Ruling R5.8: an answer is one of the choice's options.</summary>
    public static string? OptionBar(IReadOnlyList<string> options, string option)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Contains(option, StringComparer.Ordinal) ? null : $"play.choice-option: the options are {string.Join(", ", options)}";
    }

    /// <summary>The answer in words (ruling R5.8).</summary>
    public static string ChoiceSummary(string side, string option, string described) => $"play.choice: the {side} side answers '{option}' to {described}";

    /// <summary>Ruling R5.8: a stopped resolution continues from the rolls it drew, which must all be recorded.</summary>
    public static string? ResumeBar(bool everyRollRecorded) => everyRollRecorded ? null : "play.choice-resume: a roll the resolution drew is not recorded";

    /// <summary>A20.4: the prisoner is a captured unit of the other side in the unit's Location.</summary>
    public static bool MassacreTarget(bool captured, bool sameSide, bool sameLocation) => captured && !sameSide && sameLocation;

    /// <summary>A20.4: the unit named is no such prisoner.</summary>
    public static string MassacreTargetText(string prisonerId, string unitId) => $"play.massacre-target: {prisonerId} is not a prisoner in {unitId}'s Location (A20.4)";

    /// <summary>A20.4: a Massacre is made in the unit's own fire phase: the PFPh or AFPh of its side, or the DFPh of the other.</summary>
    public static string? MassacrePhaseBar(string? phase, bool unitIsPhasing) =>
        (phase is "pfph" or "afph" ? unitIsPhasing : phase == "dfph" && !unitIsPhasing) ? null : "play.massacre-phase: a Massacre is made in the unit's own fire phase (A20.4)";

    /// <summary>A20.4: only Russian or berserk Infantry not in Melee, and not itself a prisoner, massacre prisoners.</summary>
    public static string? MassacreUnitBar(bool personnel, bool melee, bool captured, bool russian, bool berserk, string unitId) =>
        !personnel || melee || captured || !(russian || berserk)
            ? $"play.massacre-unit: only Russian or berserk Infantry not in Melee massacre prisoners (A20.4); {unitId} is not one"
            : null;

    /// <summary>A20.4 (ruling R5.7): a Massacre is made "as if using a SW", once per phase.</summary>
    public static string? MassacreOnceBar(bool massacredThisPhase, string unitId) =>
        massacredThisPhase ? $"play.massacre-once: {unitId} has already massacred a prisoner this phase; a Massacre is its SW use (A20.4, A7.351)" : null;

    /// <summary>A20.4, A7.352: a SMC forfeits its inherent FP, so it may not massacre after firing, unless it is a DFPh First Fire it keeps.</summary>
    public static string? MassacreFiredBar(bool smc, bool fired, string? phase, bool firstFire, string unitId) =>
        smc && fired && !(phase == "dfph" && firstFire)
            ? $"play.massacre-fired: {unitId} is a SMC that has already fired this phase; a Massacre forfeits its inherent FP (A20.4, A7.352)"
            : null;

    /// <summary>A20.4, A7.352: the fire marker a SMC's Massacre earns it: Final Fire in the DFPh, Prep Fire otherwise.</summary>
    public static UnitCondition MassacreMarker(string? phase) => phase == "dfph" ? UnitCondition.FinalFire : UnitCondition.PrepFire;

    /// <summary>The Massacre in words (A20.4, A20.3).</summary>
    public static string MassacreSummary(string unitId, string prisonerId, string? prisonerSide) =>
        $"play.massacre: {unitId} eliminates the prisoner {prisonerId}; the {prisonerSide} side's ELR rises by one (once) and it is faced with No Quarter (A20.4, A20.3)";

    /// <summary>A20.4 (ruling R5.7): the side whose berserk units massacre as a phase begins: the phasing side's AFPh, the other side's DFPh; no other phase.</summary>
    public static string? BerserkMassacringSide(string phase, string phasing, string? otherSide) =>
        phase == "afph" ? phasing : phase == "dfph" ? otherSide : null;

    /// <summary>A20.4 (ruling R5.7): a berserk unit not in Melee in a Location with enemy prisoners massacres them as its side's fire phase begins.</summary>
    public static bool BerserkMassacres(bool active, bool ofSide, bool berserk, bool melee, bool onMap) => active && ofSide && berserk && !melee && onMap;

    /// <summary>A20.4: a prisoner the berserk units massacre: an active captured enemy unit in their Location.</summary>
    public static bool MassacredPrisoner(bool active, bool enemy, bool captured) => active && enemy && captured;

    /// <summary>The berserk Massacre in words (A20.4).</summary>
    public static string BerserkMassacreText(IReadOnlyList<string> units, IReadOnlyList<string> prisoners, string location)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(prisoners);
        return $"play.berserk-massacre: {string.Join(", ", units)} massacre the prisoners {string.Join(", ", prisoners)} in {location} and return to normal (A20.4)";
    }
}
