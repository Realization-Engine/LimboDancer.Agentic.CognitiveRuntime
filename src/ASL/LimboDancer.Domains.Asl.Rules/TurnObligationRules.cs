namespace LimboDancer.Domains.Asl.Rules;

public enum TurnObligationFamily
{
    Unsupported, Stun, Cx, TripFlare, NonFlameClearance, PlacedDc
}
public enum TurnCheckpoint
{
    DuringPhase, MovementStart, PrepFireComplete, OpportunityDesignation, UnitMovement,
    BecomesTi, MovementEnd, ClearanceResolution, OrdinaryStatusCleanup,
    ConcealmentComplete, PlayerTurnEnd, AdvancingFireEnd, Immediate, PrepFireOpening
}
public enum TurnObligationState
{
    NotEvaluated, Scheduled, Due, Resolved, Interrupted, Cancelled
}
public enum TurnRecordedOutcome
{
    None, Resolved, Interrupted, Cancelled
}
/// <summary>PlayerTurn is an absolute ordinal, independent of GameTurn and permanent side identity.</summary>
public sealed record TurnRulePosition(int GameTurn, int PlayerTurn, string PhasingSide, string Phase, TurnCheckpoint Checkpoint);
/// <summary>Narrow, authoritative host facts. Null eligibility is unknown, not false.</summary>
public sealed record TurnObligationRuleFacts(TurnObligationFamily Family, TurnRulePosition? Origin,
    TurnRulePosition Now, string OwnerSide, TurnRecordedOutcome Outcome,
    bool Active, bool Stunned, bool Recalled, bool StunRecovery, bool Cx, bool Broken, bool Berserk,
    bool? ClearanceEligible)
{
    public bool? PlacedDcOperable
    {
        get; init;
    }
}
public enum TurnRuleDeadlineKind
{
    PlayerTurnCleanup, OwnerReleaseEvents, AfterConcealment, NextAdvancingFireEnd, ClearanceAfterCombat, PlacedDcAttack
}
public sealed record TurnRuleDeadline(TurnRuleDeadlineKind Kind, int EarliestPlayerTurn, string OwnerSide);
public sealed record TurnObligationRuleVerdict(TurnObligationState State, string Reference, string Reason)
{
    public TurnRuleDeadline? Deadline
    {
        get; init;
    }
}

/// <summary>Read-only timing subset. It does not execute cleanup, roll Clearance or infer missing origins.</summary>
public static class TurnObligationRules
{
    public static TurnObligationRuleVerdict Evaluate(TurnObligationRuleFacts facts)
    {
        var result = EvaluateCore(facts);
        if (facts.Origin is not { } origin)
        {
            return result;
        }

        TurnRuleDeadline? deadline = facts.Family switch
        {
            TurnObligationFamily.PlacedDc when origin.Phase == "mph" => new(TurnRuleDeadlineKind.PlacedDcAttack, origin.PlayerTurn, facts.OwnerSide),
            TurnObligationFamily.Stun => new(TurnRuleDeadlineKind.PlayerTurnCleanup, origin.PlayerTurn, facts.OwnerSide),
            TurnObligationFamily.Cx => new(TurnRuleDeadlineKind.OwnerReleaseEvents, origin.PlayerTurn, facts.OwnerSide),
            TurnObligationFamily.TripFlare when origin.Phase == "mph" => new(TurnRuleDeadlineKind.AfterConcealment, origin.PlayerTurn, facts.OwnerSide),
            TurnObligationFamily.TripFlare when origin.Phase is "rtph" or "aph" or "ccph" => new(TurnRuleDeadlineKind.NextAdvancingFireEnd, origin.PlayerTurn + 1, facts.OwnerSide),
            TurnObligationFamily.NonFlameClearance when origin.Phase is "mph" or "dfph" => new(TurnRuleDeadlineKind.ClearanceAfterCombat, origin.PlayerTurn, facts.OwnerSide),
            _ => null,
        };
        return result with
        {
            Deadline = deadline
        };
    }

    private static TurnObligationRuleVerdict EvaluateCore(TurnObligationRuleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!Enum.IsDefined(facts.Family) || !Enum.IsDefined(facts.Outcome))
        {
            throw new ArgumentException("Unknown obligation family or outcome.", nameof(facts));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(facts.OwnerSide);
        ValidatePosition(facts.Now);
        if (facts.Origin is not { } origin)
        {
            return Result(TurnObligationState.NotEvaluated, "", "Origin facts are missing.");
        }

        ValidatePosition(origin);
        if (origin.PlayerTurn > facts.Now.PlayerTurn || origin.GameTurn > facts.Now.GameTurn)
        {
            throw new ArgumentException("Origin is after the evaluation position.", nameof(facts));
        }

        if (origin.PlayerTurn == facts.Now.PlayerTurn && (origin.GameTurn != facts.Now.GameTurn || origin.PhasingSide != facts.Now.PhasingSide || PhaseRank(origin.Phase) > PhaseRank(facts.Now.Phase)))
        {
            throw new ArgumentException("Inconsistent or reversed Player Turn position.", nameof(facts));
        }

        var reference = facts.Family switch
        {
            TurnObligationFamily.PlacedDc => "A23.3-.4; ASOP 5.22A",
            TurnObligationFamily.Stun => "D5.34; D5.341; ASOP 8.43B",
            TurnObligationFamily.Cx => "A4.51-.52",
            TurnObligationFamily.TripFlare => "E1.95-.951",
            TurnObligationFamily.NonFlameClearance => "B24.7-.8; ASOP 8.42B",
            _ => "",
        };
        if (facts.Family == TurnObligationFamily.Unsupported)
        {
            return Result(TurnObligationState.NotEvaluated, reference, "Rule-family evaluation is pending.");
        }

        if (facts.Outcome != TurnRecordedOutcome.None)
        {
            var state = facts.Outcome switch
            {
                TurnRecordedOutcome.Resolved => TurnObligationState.Resolved,
                TurnRecordedOutcome.Interrupted => TurnObligationState.Interrupted,
                _ => TurnObligationState.Cancelled,
            };
            return Result(state, reference, "Recorded domain evidence determines this lifecycle state.");
        }
        bool due;
        switch (facts.Family)
        {
            case TurnObligationFamily.PlacedDc:
                // Keeps its answer (the A23.3 claim, RA23.8): an obligation's deadline, due from the AFPh on and overdue after, not the window itself,
                // which ScenarioA1DemolitionChargeRules.DetonationWindow decides.
                if (origin.Phase != "mph" || facts.PlacedDcOperable is null)
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "Placement or operability evidence is incomplete.");
                }
                due = facts.PlacedDcOperable == true && (facts.Now.PlayerTurn > origin.PlayerTurn ||
                    PhaseRank(facts.Now.Phase) >= PhaseRank("afph"));
                break;
            case TurnObligationFamily.Stun:
                if (ScenarioA1SequenceProjection.StunEnds(facts.Active, facts.Recalled, facts.StunRecovery, facts.Stunned).Conditions.Count == 0)
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "No supported pending Stun effect; completion evidence is required.");
                }

                due = facts.Now.PlayerTurn > origin.PlayerTurn ||
                    (facts.Now.Phase == "ccph" && facts.Now.Checkpoint is TurnCheckpoint.OrdinaryStatusCleanup or TurnCheckpoint.ConcealmentComplete or TurnCheckpoint.PlayerTurnEnd);
                break;
            case TurnObligationFamily.Cx:
                if (!facts.Active || !facts.Cx)
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "Marker absence is not resolution evidence; retain prior CX history.");
                }

                var friendlyLater = facts.Now.PlayerTurn > origin.PlayerTurn && facts.Now.PhasingSide == facts.OwnerSide;
                due = ScenarioA1SequenceProjection.LosesCx(facts.Active, facts.Cx, facts.Broken, facts.Berserk) ||
                    (friendlyLater && (CxRelease(facts) || PhaseRank(facts.Now.Phase) > PhaseRank("mph")));
                break;
            case TurnObligationFamily.TripFlare:
                if (origin.Phase == "mph")
                {
                    due = facts.Now.PlayerTurn > origin.PlayerTurn || (facts.Now.Phase == "ccph" &&
                        facts.Now.Checkpoint is TurnCheckpoint.ConcealmentComplete or TurnCheckpoint.PlayerTurnEnd);
                }
                else if (origin.Phase is "rtph" or "aph" or "ccph")
                {
                    due = facts.Now.PlayerTurn > origin.PlayerTurn + 1 || (facts.Now.PlayerTurn == origin.PlayerTurn + 1 &&
                        (PhaseRank(facts.Now.Phase) > PhaseRank("afph") || (facts.Now.Phase == "afph" && facts.Now.Checkpoint == TurnCheckpoint.AdvancingFireEnd)));
                }
                else
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "Unsupported Trip Flare activation phase.");
                }

                break;
            default:
                if (origin.Phase is not ("mph" or "dfph"))
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "Unsupported non-Flame declaration phase.");
                }

                if (facts.ClearanceEligible is null)
                {
                    return Result(TurnObligationState.NotEvaluated, reference, "Task-specific Clearance eligibility is unknown.");
                }

                if (facts.ClearanceEligible == false)
                {
                    return Result(TurnObligationState.Interrupted, reference, "Declared task no longer meets its rule-specific eligibility.");
                }

                due = facts.Now.PlayerTurn > origin.PlayerTurn || (facts.Now.Phase == "ccph" && facts.Now.Checkpoint is
                    TurnCheckpoint.ClearanceResolution or TurnCheckpoint.OrdinaryStatusCleanup or TurnCheckpoint.ConcealmentComplete or TurnCheckpoint.PlayerTurnEnd);
                break;
        }
        return Result(due ? TurnObligationState.Due : TurnObligationState.Scheduled, reference,
            due ? "The rule's release or resolution window has been reached; no effect is applied by this assessment." : "Retain the obligation until its rule-owned window.");
    }

    private static bool CxRelease(TurnObligationRuleFacts facts) =>
        ScenarioA1SequenceProjection.CxRests(facts.Now.Phase, facts.Active, facts.Now.PhasingSide == facts.OwnerSide, facts.Cx) ||
        (facts.Now.Phase == "pfph" && facts.Now.Checkpoint is TurnCheckpoint.PrepFireComplete or TurnCheckpoint.OpportunityDesignation) ||
        facts.Now.Checkpoint == TurnCheckpoint.BecomesTi;

    private static TurnObligationRuleVerdict Result(TurnObligationState state, string reference, string reason) => new(state, reference, reason);

    public static void ValidatePosition(TurnRulePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentOutOfRangeException.ThrowIfLessThan(position.GameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(position.PlayerTurn, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(position.PhasingSide);
        _ = PhaseRank(position.Phase);
        if (!Enum.IsDefined(position.Checkpoint))
        {
            throw new ArgumentException("Unknown semantic checkpoint.", nameof(position));
        }
    }

    private static int PhaseRank(string phase) => phase switch
    {
        "rph" => 0,
        "pfph" => 1,
        "mph" => 2,
        "dfph" => 3,
        "afph" => 4,
        "rtph" => 5,
        "aph" => 6,
        "ccph" => 7,
        _ => throw new ArgumentException("Unknown turn phase.", nameof(phase)),
    };
}
