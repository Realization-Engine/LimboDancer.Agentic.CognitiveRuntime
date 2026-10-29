namespace LimboDancer.Domains.Asl.Rules;

/// <summary>How a Spotter's assignment changed (C9.3): no change, the losses that end it, a squad's Reduction, or not known.</summary>
public enum SpotterChange
{
    None, Eliminated, Broken, Captured, SquadReduced, Unknown
}

/// <summary>Where a Spotter's designation stands in time (C9.3; ASOP 3.11A for a replacement).</summary>
public enum SpotterTimingState
{
    NotEvaluated, Retained, InitialDesignationWindow, ReplacementWindow, OutsideDesignationWindow
}

/// <summary>
/// The timing evidence of a Spotter's designation (C9.3): the owner, the position now, whether an original designation is recorded (null when not
/// known), its change, when it was lost, and whether a squad's Reduction left a Good Order half-squad. Not eligibility, reach, LOS, or fire.
/// </summary>
public sealed record SpotterTimingFacts(string OwnerSide, TurnRulePosition Now, bool? HasDesignation,
    SpotterChange Change, TurnRulePosition? LossAt = null, bool? GoodOrderReductionSurvivor = null);

/// <summary>A timing verdict and its reason.</summary>
public sealed record SpotterTimingVerdict(SpotterTimingState State, string Reason)
{
    public string Reference { get; } = "C9.3; ASOP 3.11A (replacement)";
}

/// <summary>
/// The facts of an original designation (C9.3): its timing; whether the weapon is a mortar; the Spotter as <see cref="SpotterCandidateFacts"/>;
/// the hex range from the mortar to the Spotter (null when not read); the Locations of the other mortars it already spots for, against the mortar's
/// own (null when not read); night; whether the mortar is Temporarily Immobilized; the Spotter's fire and spotting this phase; its SW use; a
/// decision or move still open; and whether the Spotter itself is TI (A4.8; the review of Wiring Prompt 001, C931). The last is null only from a
/// caller that does not yet gather it, which keeps its old answer until W-C9.3 supplies it.
/// </summary>
public sealed record SpotterDesignationFacts(SpotterTimingFacts Timing, bool Mortar, SpotterCandidateFacts Candidate, int? HexRange,
    bool? SharesLocation, bool Night, bool TemporarilyImmobilized, bool Squad, bool Fired, bool UsedAsSpotter, SquadSupportFacts Support,
    bool PendingDecision, bool? SpotterTemporarilyImmobilized = null);

/// <summary>
/// A unit offered as a Spotter (C9.3): active (on the map, not eliminated), of the mortar's side, Personnel, Good Order, Captured, in Melee, and
/// whether it is the mortar's own crew.
/// </summary>
public sealed record SpotterCandidateFacts(bool Active, bool SameSide, bool Personnel, bool GoodOrder, bool Captured, bool Melee, bool MortarCrew);

/// <summary>One mortar use by a Spotter in this Player Turn (C9.31): the mortar, its Location with its level (null when not read), and the target.</summary>
public sealed record SpotterUse(string Gun, string? Location, string Target);

/// <summary>
/// A step of the opening of the MPh, in the ASOP's order (p. 678; the replacement design, ruling RC93.11): 3.11A a new mortar Spotter, 3.12D
/// Starshells, 3.13A Gliders, 3.21A a berserk unit's charge, 3.22A any other unit's move, or any other action.
/// </summary>
public enum MovementOpeningStep
{
    SpotterReplacement, Starshell, GliderPlacement, BerserkCharge, UnitMovement, Other
}

/// <summary>Where a replacement Spotter's designation stands (ruling RC93.13).</summary>
public enum SpotterReplacementState
{
    MayReplace, Refused, NotEvaluated
}

/// <summary>
/// The facts of a replacement Spotter's designation at the owner's MPh opening (C9.3; ASOP 3.11A; ruling RC93.12): its timing, whose
/// <c>Now</c> is the owner's MPh (its checkpoint is set from <paramref name="StepsTaken"/>); the steps taken in this MPh so far; whether the weapon is
/// an active mortar whose crew is on the map; the candidate; the hex range from the mortar to it (null when not read); whether every other mortar it
/// spots for is in this mortar's Location (null when not read); night; and a decision or move still open.
/// </summary>
public sealed record SpotterReplacementFacts(SpotterTimingFacts Timing, IReadOnlyList<MovementOpeningStep> StepsTaken, bool Mortar,
    SpotterCandidateFacts Candidate, int? HexRange, bool? SharesLocation, bool Night, bool PendingDecision);

/// <summary>
/// A replacement's verdict (ruling RC93.13): when allowed, the candidate holds the mortar's assignment from now on, a new one, with no fire counter
/// and no movement bar: the replacement designates the Spotter and does not use it.
/// </summary>
public sealed record SpotterReplacementVerdict(SpotterReplacementState State, string? Refusal, bool EstablishesAssignment, string Reason)
{
    public string Reference { get; } = "C9.3; ASOP 3.11A";
}

/// <summary>
/// What the fire that spots does to its Spotter (C9.3, spotting the equivalent of using a SW; ruling RC93.13): the phase's fire counter, or none when
/// the Spotter already carries it, and whether it may no longer move this Player Turn.
/// </summary>
public sealed record SpottingMarksVerdict(UnitCondition? FireCounter, bool BarsMovement);
