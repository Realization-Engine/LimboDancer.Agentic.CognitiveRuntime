using System.Collections.Immutable;

namespace ASL.Turns;

public enum TurnClosePlanStatus
{
    Ready, Refused, Stale
}
public enum TurnCloseDisposition
{
    Ready, Blocked, Stale, ConsequencePending, GovernedCheckRequired, ReadOnly
}
/// <summary>Internal host facts from the actual governed planner. Never send this record directly to a side.</summary>
public sealed record TurnClosePlanFacts(TurnClosePlanStatus Status, bool AdvancesPhase, bool RequiresRoll,
    ImmutableArray<string> Reasons, ImmutableArray<string> Effects);
public sealed record TurnPhaseCloseAssessment(TurnCloseDisposition Disposition, ImmutableArray<string> Reasons,
    ImmutableArray<string> AutomaticEffects, bool RequiresRoll, ImmutableArray<string> EvaluationGaps);

public static class TurnPhaseClose
{
    /// <summary>Project a shared plan assessment without revealing hidden readiness, reasons or effect counts.</summary>
    public static TurnPhaseCloseAssessment Describe(TurnClosePlanFacts facts, bool adjudicator, bool historical)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ImmutableArray<string> gaps = ["This assessment covers the existing governed engine, not all ASL rules.",
            "Additional dependency families and Trip Flare/Clearance command integration remain pending."];
        if (historical)
        {
            return new(TurnCloseDisposition.ReadOnly, ["Historical guidance cannot close a phase."], [], false, gaps);
        }
        if (!adjudicator)
        {
            return new(TurnCloseDisposition.GovernedCheckRequired,
                ["Use the governed phase-advance action to check and confirm the current phase. Private decisions may remain."], [], false, gaps);
        }
        if (!Enum.IsDefined(facts.Status) || facts.Reasons.IsDefault || facts.Effects.IsDefault)
        {
            throw new ArgumentException("Invalid planner assessment.", nameof(facts));
        }
        var disposition = facts.Status switch
        {
            TurnClosePlanStatus.Refused => TurnCloseDisposition.Blocked,
            TurnClosePlanStatus.Stale => TurnCloseDisposition.Stale,
            _ => facts.AdvancesPhase ? TurnCloseDisposition.Ready : TurnCloseDisposition.ConsequencePending,
        };
        return new(disposition, facts.Reasons, facts.Effects, facts.RequiresRoll, gaps);
    }
}
