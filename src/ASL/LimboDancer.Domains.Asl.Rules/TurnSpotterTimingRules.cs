namespace LimboDancer.Domains.Asl.Rules;

public enum TurnSpotterChange
{
    None, Eliminated, Broken, Captured, SquadReduced, Unknown
}
public enum TurnSpotterTimingState
{
    NotEvaluated, Retained, InitialDesignationWindow, ReplacementWindow, OutsideDesignationWindow
}

/// <summary>C9.3 timing evidence, not candidate eligibility, adjacency, LOS or firing permission.</summary>
public sealed record TurnSpotterTimingFacts(string OwnerSide, TurnRulePosition Now, bool? HasDesignation,
    TurnSpotterChange Change, TurnRulePosition? LossAt = null, bool? GoodOrderReductionSurvivor = null);
public sealed record TurnSpotterTimingVerdict(TurnSpotterTimingState State, string Reason)
{
    public string Reference { get; } = "C9.3; ASOP 3.11A (replacement)";
}

/// <summary>Read-only designation timing. A host must prove the checkpoint and the authoritative assignment/loss history.</summary>
public static class TurnSpotterTimingRules
{
    private static readonly string[] Phases = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];

    public static TurnSpotterTimingVerdict Evaluate(TurnSpotterTimingFacts facts)
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
        if (facts.HasDesignation is null || facts.Change == TurnSpotterChange.Unknown)
        {
            return Result(TurnSpotterTimingState.NotEvaluated, "Original designation or loss history is unknown.");
        }
        if (facts.HasDesignation == false)
        {
            if (facts.Change != TurnSpotterChange.None || facts.LossAt is not null || facts.GoodOrderReductionSurvivor is not null)
            {
                return Result(TurnSpotterTimingState.NotEvaluated, "A loss or successor without a known original designation is inconsistent evidence.");
            }
            var original = facts.Now.Phase == "pfph" && facts.Now.PhasingSide == facts.OwnerSide
                || facts.Now.Phase == "dfph" && facts.Now.PhasingSide != facts.OwnerSide;
            return Result(original ? TurnSpotterTimingState.InitialDesignationWindow : TurnSpotterTimingState.OutsideDesignationWindow,
                original ? "Original designation may be considered in the owner's PFPh/DFPh; candidate eligibility remains to be checked."
                    : "Original designation belongs to the owner's PFPh/DFPh.");
        }
        if (facts.Change != TurnSpotterChange.SquadReduced && facts.GoodOrderReductionSurvivor is not null)
        {
            return Result(TurnSpotterTimingState.NotEvaluated, "Successor evidence requires a recorded squad reduction.");
        }
        if (facts.Change == TurnSpotterChange.SquadReduced)
        {
            return facts.GoodOrderReductionSurvivor == true
                ? Result(TurnSpotterTimingState.Retained, "The surviving Good Order half-squad retains spotting ability; this is not a replacement designation.")
                : Result(TurnSpotterTimingState.NotEvaluated, "Prove the surviving half-squad's Good Order retention or record its actual elimination, break or capture; reduction alone does not prove a replacement window.");
        }
        if (facts.Change == TurnSpotterChange.None)
        {
            return facts.LossAt is null
                ? Result(TurnSpotterTimingState.Retained, "The recorded designation remains; a different Spotter cannot replace it without the required loss.")
                : Result(TurnSpotterTimingState.NotEvaluated, "A loss position without a recorded loss reason is insufficient.");
        }
        if (facts.LossAt is not { } lost)
        {
            return Result(TurnSpotterTimingState.NotEvaluated, "The loss timing is missing; current counter status cannot establish the replacement window.");
        }
        var afterLoss = facts.Now.PlayerTurn > lost.PlayerTurn || Rank(lost) < Rank(facts.Now);
        var replacement = afterLoss && facts.Now.PhasingSide == facts.OwnerSide && facts.Now.Phase == "mph"
            && facts.Now.Checkpoint == TurnCheckpoint.MovementStart;
        return Result(replacement ? TurnSpotterTimingState.ReplacementWindow : TurnSpotterTimingState.OutsideDesignationWindow,
            replacement ? "Replacement may be considered at the owner's MPh opening following the recorded loss; candidate eligibility remains to be checked."
                : "Wait for an owner's MPh opening following the recorded loss. A phase label alone does not prove the opening checkpoint.");
    }

    private static int Rank(TurnRulePosition position) => Array.IndexOf(Phases, position.Phase);
    private static TurnSpotterTimingVerdict Result(TurnSpotterTimingState state, string reason) => new(state, reason);
}
