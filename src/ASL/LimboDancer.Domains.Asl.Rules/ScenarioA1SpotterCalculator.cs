namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// C9.3 Spotters (p. 179), the one calculator of the rule (the C9.3 claim's design, ruling RC93.1): who may spot, reach, sharing, the designation's
/// timing and refusal, retention, loss, successors, and supersession. C9.31's +2 To Hit and lowered ROF are decided with the fire they modify, in
/// <see cref="ScenarioA1AreaCalculator.Run"/>. Facts in, verdicts out: the callers gather the facts from the state and the map.
/// </summary>
public static class ScenarioA1SpotterCalculator
{
    private static readonly string[] Phases = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];

    private static readonly string[] PersonnelKinds = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero"];

    /// <summary>Whether a unit's kind is Personnel, as C9.3 asks of a Spotter.</summary>
    public static bool Personnel(string? kind) => kind is not null && PersonnelKinds.Contains(kind, StringComparer.Ordinal);

    /// <summary>
    /// Why a unit may not spot (ruling RC93.3), or null: C9.3's "one Good Order Personnel unit" of the owning player, so an active unit of the mortar's
    /// side, Personnel, not broken, Captured, or in Melee, and not the mortar's own crew, which observes for itself.
    /// </summary>
    public static string? CandidateRefusal(SpotterCandidateFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return !unit.Active || !unit.SameSide ? "A Spotter is an active unit of the mortar's side (C9.3)."
            : !unit.Personnel ? "A Spotter is a Personnel unit (C9.3)."
            : !unit.GoodOrder ? "A Spotter is Good Order (C9.3)."
            : unit.Captured || unit.Melee ? "A Captured unit or one in Melee does not spot (C9.3)."
            : unit.MortarCrew ? "The mortar's own crew observes for itself and is not its Spotter (C9.3)."
            : null;
    }

    /// <summary>Ruling RC93.4: the Spotter is in the mortar's hex or an adjacent one, whatever its level and LOS; an unknown range is not in reach.</summary>
    public static bool InReach(int? hexRange) => hexRange is >= 0 and <= 1;

    /// <summary>
    /// C9.3 (referee, pass 9): only a mortar fires spotted, and only in the PFPh or DFPh, where its Spotter is designated. Opportunity Fire, which would let
    /// it spot in the AFPh, is not built (backlog section 80).
    /// </summary>
    public static bool SpottedFireAllowed(bool mortar, string? phase) => mortar && phase is "PFPh" or "DFPh";

    /// <summary>The refusal Play gives a shot whose Spotter is out of reach.</summary>
    public static string ReachRefusal(string spotter) => $"play.ordnance-spotter: {spotter} spots only from the mortar's hex or an adjacent one (C9.3)";

    /// <summary>
    /// Ruling RC93.5 (C9.31, its example): a Spotter used this Player Turn may serve another mortar only if every earlier use was by that mortar, or
    /// all were from this Location (with its level) at this target.
    /// </summary>
    public static bool SharesWith(IReadOnlyList<SpotterUse> previous, string gun, string? location, string target)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentException.ThrowIfNullOrWhiteSpace(gun);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        return previous.All(item => item.Gun == gun)
            || location is not null && previous.All(item => item.Location == location && item.Target == target);
    }

    /// <summary>
    /// Ruling RC93.5 at designation, before a target is named: the other mortars the Spotter already serves are all in this mortar's Location (with
    /// its level). Null when a Location is not read.
    /// </summary>
    public static bool? SharesLocation(string? location, IReadOnlyList<string?> otherMortarLocations)
    {
        ArgumentNullException.ThrowIfNull(otherMortarLocations);
        return otherMortarLocations.Count == 0 ? true
            : location is null || otherMortarLocations.Any(other => other is null) ? null
            : otherMortarLocations.All(other => other == location);
    }

    /// <summary>
    /// Ruling RC93.6: the Spotter still serves its mortar: active, of its side, not broken or captured, and no loss recorded. A recorded loss is final:
    /// the mortar fires spotted only "as long as the Spotter remains in Good Order" (C9.3), and ASOP 3.11A names the new Spotter for one "not in Good
    /// Order", so a Spotter that broke and rallied does not resume by itself. Nothing bars designating the same unit again as the new Spotter.
    /// </summary>
    public static bool Retains(bool active, bool sameSide, bool broken, bool captured, bool recordedLoss) =>
        active && sameSide && !broken && !captured && !recordedLoss;

    /// <summary>Ruling RC93.6: the record still holds an assignment of this Spotter that was not lost.</summary>
    public static bool HoldsAssignment(bool assigned, bool recordedLoss) => assigned && !recordedLoss;

    /// <summary>Ruling RC93.7: while the original Spotter is retained, no other unit spots for that mortar.</summary>
    public static string? KeptRefusal(string gun, string? keptSpotter, string spotter) =>
        keptSpotter is not null && keptSpotter != spotter ? $"play.ordnance-spotter-kept: {keptSpotter} spots for {gun} while it is Good Order (C9.3)" : null;

    /// <summary>Ruling RC93.7: a later Spotter on the mortar supersedes the earlier assignment, unless it is that assignment's recorded successor.</summary>
    public static bool Supersedes(string earlierSpotter, string laterSpotter, string? recordedSuccessor) =>
        earlierSpotter != laterSpotter && laterSpotter != recordedSuccessor;

    /// <summary>The refusal of a spotted shot after the assignment's recorded loss: a governed replacement comes first (C9.3).</summary>
    public static string? FireBar(bool recordedLoss) => recordedLoss
        ? "play.ordnance-spotter-lost: the recorded assignment was lost; a governed replacement is required (C9.3). Replacement execution remains pending."
        : null;

    /// <summary>How an assignment was lost, from the Spotter's state: elimination before capture before breaking; none if it was not active.</summary>
    public static SpotterChange ObservedLoss(bool previouslyActive, bool eliminated, bool broken, bool captured) =>
        !previouslyActive ? SpotterChange.None
        : eliminated ? SpotterChange.Eliminated
        : captured ? SpotterChange.Captured
        : broken ? SpotterChange.Broken : SpotterChange.None;

    /// <summary>C9.3's exception: a Reduced Spotting squad's surviving Good Order half-squad keeps spotting, unless the assignment was already lost.</summary>
    public static bool RetainsReducedSquad(bool reducedSquad, bool activeHalfSquad, bool sameSide, bool goodOrder, bool recordedLoss) =>
        reducedSquad && activeHalfSquad && sameSide && goodOrder && !recordedLoss;

    /// <summary>A Deployment's chosen half-squad keeps the Spotting squad's assignment (A1.31; C9.3), unless it was already lost.</summary>
    public static bool RetainsDeployedSquad(bool deployedSquad, bool chosenProducedHalf, bool sameSide, bool goodOrder, bool recordedLoss) =>
        deployedSquad && chosenProducedHalf && sameSide && goodOrder && !recordedLoss;

    /// <summary>Why a Deployment's Spotter choice is refused (A1.31; C9.3): it is required for a held assignment and refused without one.</summary>
    public static string? DeploymentChoiceRefusal(bool assigned, string? choice) =>
        choice is not null && choice is not ("first" or "second")
            ? "play.deploy-spotter: choose the first or second half-squad to retain spotting (A1.31; C9.3)."
        : assigned && choice is null
            ? "play.deploy-spotter: choose which half-squad retains the Spotter assignment (A1.31; C9.3)."
        : !assigned && choice is not null
            ? "play.deploy-spotter: there is no retained Spotter assignment to pass to a half-squad."
        : null;

    /// <summary>
    /// A Deployment's named Spotter successor (A1.31; C9.3): the squad still holds an unlost assignment and is Good Order, and the successor is the
    /// half the owner chose before the NTC. A record made before the choice was recorded is checked by the squad's custody instead.
    /// </summary>
    public static bool DeploymentSuccessorValid(bool assignmentHeld, bool squadGoodOrder, string? recordedChoice, string successor,
        IReadOnlyList<string> producedIds, bool legacyCustodyHeld)
    {
        ArgumentNullException.ThrowIfNull(producedIds);
        return assignmentHeld && squadGoodOrder
            && (recordedChoice is not null ? producedIds[recordedChoice == "first" ? 0 : 1] == successor : legacyCustodyHeld);
    }

    /// <summary>The fire counter a Spotter designated in a phase takes (C9.3: marked as having used a SW that phase).</summary>
    public static UnitCondition FireCounter(string phase) => phase switch
    {
        "pfph" => UnitCondition.PrepFire,
        "dfph" => UnitCondition.FinalFire,
        _ => throw new ArgumentException("An admitted original designation uses PFPh or DFPh.", nameof(phase)),
    };

    /// <summary>A Spotter designated in the PFPh takes the Prep Fire counter (<see cref="FireCounter"/>) and so may not move this Player Turn.</summary>
    public static bool BarsMovement(string phase) => phase == "pfph";

    /// <summary>Why a unit may not be designated the mortar's Spotter now (C9.3; A7.351, A7.352), or null.</summary>
    public static string? DesignationRefusal(SpotterDesignationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.PendingDecision)
        {
            return "Complete the outstanding decision or movement before designating a Spotter.";
        }

        if (!facts.Mortar || facts.TemporarilyImmobilized)
        {
            return "C9.3 requires an active mortar that is not Temporarily Immobilized.";
        }

        if (CandidateRefusal(facts.Candidate) is { } candidate)
        {
            return candidate;
        }

        // A4.8 (p. 52): "A TI unit may not move, advance, fire, or perform other labor tasks"; C9.3 makes spotting the use of a SW (C931).
        if (facts.SpotterTemporarilyImmobilized == true)
        {
            return "A TI unit may not fire, and spotting is the use of a SW, so it may not spot (A4.8, C9.3).";
        }

        if (facts.Night)
        {
            return "Night designation and E1.89 fire-marker handling remain pending.";
        }

        var timing = Timing(facts.Timing);
        if (timing.State != SpotterTimingState.InitialDesignationWindow)
        {
            return "This route admits original PFPh/DFPh designation only. " + timing.Reason;
        }

        if (facts.HexRange is null || facts.SharesLocation is null)
        {
            return "Prove the mortar's same/adjacent hex and one Location with the Spotter's other mortars (C9.3).";
        }

        if (!InReach(facts.HexRange))
        {
            return $"A Spotter is in the mortar's hex or an adjacent one; this one is {facts.HexRange} hexes away (C9.3).";
        }

        if (facts.SharesLocation != true)
        {
            return "A Spotter serves more than one mortar only when they are in one Location (C9.3, C9.31).";
        }

        if (!facts.Squad && facts.Fired && !facts.UsedAsSpotter)
        {
            return "A non-squad that already fired cannot also Spot (A7.352).";
        }

        return facts.Squad && !facts.UsedAsSpotter
            ? ScenarioA1OrdnanceEligibility.SquadSupportRefusal(facts.Support, facts.Timing.Now.Phase == "pfph" ? "PFPh" : "DFPh", false)
            : null;
    }

    /// <summary>The timing of a designation (C9.3; ASOP 3.11A): the owner's PFPh or DFPh for the original, the owner's next MPh opening after a loss.</summary>
    public static SpotterTimingVerdict Timing(SpotterTimingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentException.ThrowIfNullOrWhiteSpace(facts.OwnerSide);
        TurnObligationRules.ValidatePosition(facts.Now);
        if (!Enum.IsDefined(facts.Change))
        {
            throw new ArgumentException("Unknown Spotter change.", nameof(facts));
        }

        if (facts.LossAt is { } loss)
        {
            TurnObligationRules.ValidatePosition(loss);
            if (loss.GameTurn > facts.Now.GameTurn || loss.PlayerTurn > facts.Now.PlayerTurn
                || loss.PlayerTurn == facts.Now.PlayerTurn && (loss.GameTurn != facts.Now.GameTurn
                    || loss.PhasingSide != facts.Now.PhasingSide || Rank(loss) > Rank(facts.Now)))
            {
                throw new ArgumentException("Spotter loss must precede the current position in a coherent turn history.", nameof(facts));
            }
        }

        if (facts.HasDesignation is null || facts.Change == SpotterChange.Unknown)
        {
            return Result(SpotterTimingState.NotEvaluated, "Original designation or loss history is unknown.");
        }

        if (facts.HasDesignation == false)
        {
            if (facts.Change != SpotterChange.None || facts.LossAt is not null || facts.GoodOrderReductionSurvivor is not null)
            {
                return Result(SpotterTimingState.NotEvaluated, "A loss or successor without a known original designation is inconsistent evidence.");
            }

            var original = facts.Now.Phase == "pfph" && facts.Now.PhasingSide == facts.OwnerSide
                || facts.Now.Phase == "dfph" && facts.Now.PhasingSide != facts.OwnerSide;
            return Result(original ? SpotterTimingState.InitialDesignationWindow : SpotterTimingState.OutsideDesignationWindow,
                original ? "Original designation may be considered in the owner's PFPh/DFPh; candidate eligibility remains to be checked."
                    : "Original designation belongs to the owner's PFPh/DFPh.");
        }

        if (facts.Change != SpotterChange.SquadReduced && facts.GoodOrderReductionSurvivor is not null)
        {
            return Result(SpotterTimingState.NotEvaluated, "Successor evidence requires a recorded squad reduction.");
        }

        if (facts.Change == SpotterChange.SquadReduced)
        {
            return facts.GoodOrderReductionSurvivor == true
                ? Result(SpotterTimingState.Retained, "The surviving Good Order half-squad retains spotting ability; this is not a replacement designation.")
                : Result(SpotterTimingState.NotEvaluated, "Prove the surviving half-squad's Good Order retention or record its actual elimination, break or capture; reduction alone does not prove a replacement window.");
        }

        if (facts.Change == SpotterChange.None)
        {
            return facts.LossAt is null
                ? Result(SpotterTimingState.Retained, "The recorded designation remains; a different Spotter cannot replace it without the required loss.")
                : Result(SpotterTimingState.NotEvaluated, "A loss position without a recorded loss reason is insufficient.");
        }

        if (facts.LossAt is not { } lost)
        {
            return Result(SpotterTimingState.NotEvaluated, "The loss timing is missing; current counter status cannot establish the replacement window.");
        }

        var afterLoss = facts.Now.PlayerTurn > lost.PlayerTurn || Rank(lost) < Rank(facts.Now);
        var replacement = afterLoss && facts.Now.PhasingSide == facts.OwnerSide && facts.Now.Phase == "mph"
            && facts.Now.Checkpoint == TurnCheckpoint.MovementStart;
        return Result(replacement ? SpotterTimingState.ReplacementWindow : SpotterTimingState.OutsideDesignationWindow,
            replacement ? "Replacement may be considered at the owner's MPh opening following the recorded loss; candidate eligibility remains to be checked."
                : "Wait for an owner's MPh opening following the recorded loss. A phase label alone does not prove the opening checkpoint.");
    }

    private static int Rank(TurnRulePosition position) => Array.IndexOf(Phases, position.Phase);

    private static SpotterTimingVerdict Result(SpotterTimingState state, string reason) => new(state, reason);
}
