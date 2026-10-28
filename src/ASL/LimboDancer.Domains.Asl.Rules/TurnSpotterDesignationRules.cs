namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Bounded C9.3 original declaration facts. Replacement and advance recording remain separate pending routes.</summary>
public sealed record TurnSpotterDesignationFacts(TurnSpotterTimingFacts Timing, bool Mortar, bool ActiveOwner,
    bool GoodOrderPersonnel, bool? InReach, bool? OtherMortarsSameHex, bool Night, bool TemporarilyImmobilized,
    bool Squad, bool Fired, bool UsedAsSpotter, SquadSupportFacts Support, bool PendingDecision);

public static class TurnSpotterDesignationRules
{
    public static UnitCondition FireCounter(string phase) => phase switch
    {
        "pfph" => UnitCondition.PrepFire,
        "dfph" => UnitCondition.FinalFire,
        _ => throw new ArgumentException("An admitted original designation uses PFPh or DFPh.", nameof(phase)),
    };

    public static string? Refusal(TurnSpotterDesignationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.PendingDecision)
        {
            return "Complete the outstanding decision or movement before designating a Spotter.";
        }
        if (!facts.Mortar || !facts.ActiveOwner || !facts.GoodOrderPersonnel || facts.TemporarilyImmobilized)
        {
            return "C9.3 requires an active mortar and an eligible Good Order Personnel Spotter of its owner's side.";
        }
        if (facts.Night)
        {
            return "Night designation and E1.89 fire-marker handling remain pending.";
        }
        var timing = TurnSpotterTimingRules.Evaluate(facts.Timing);
        if (timing.State != TurnSpotterTimingState.InitialDesignationWindow)
        {
            return "This route admits original PFPh/DFPh designation only. " + timing.Reason;
        }
        if (facts.InReach != true || facts.OtherMortarsSameHex != true)
        {
            return "Prove the mortar's same/adjacent hex and one-hex compatibility with the Spotter's other mortars (C9.3).";
        }
        if (!facts.Squad && facts.Fired && !facts.UsedAsSpotter)
        {
            return "A non-squad that already fired cannot also Spot (A7.352).";
        }
        return facts.Squad && !facts.UsedAsSpotter
            ? ScenarioA1OrdnanceEligibility.SquadSupportRefusal(facts.Support, facts.Timing.Now.Phase == "pfph" ? "PFPh" : "DFPh", false)
            : null;
    }
}
