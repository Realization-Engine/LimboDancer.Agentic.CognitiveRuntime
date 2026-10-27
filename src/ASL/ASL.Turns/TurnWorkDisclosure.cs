using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

/// <summary>The state of the bound completion effects, distinct from rule automation.</summary>
public enum TurnWorkEffectStatus
{
    NotSpecified, AwaitingCompletion, Applied, Cancelled
}

/// <summary>Only return after host authorization. No raw event IDs, rolls or adjudicator notes.</summary>
public sealed record TurnWorkDisclosure(string Id, string Subject, TurnWorkKind Kind, int CompletedGameTurns,
    int RequiredGameTurns, bool Working, bool Completed, bool Cancelled, bool CompletionDue, bool SupplyDepleted,
    TurnSupport RuleAutomation)
{
    public TurnWorkEffectKind? EffectKind
    {
        get; init;
    }
    public TurnWorkEffectStatus EffectStatus
    {
        get; init;
    }

    public static TurnWorkDisclosure From(TurnLiveWork work, bool due)
    {
        ArgumentNullException.ThrowIfNull(work);
        return new(work.Declaration.Id, work.Declaration.Subject, work.Declaration.Kind, work.CompletedGameTurns,
            work.RequiredGameTurns, work.Working, work.Completed, work.Cancelled, due, work.SupplyDepleted, TurnSupport.Pending)
        {
            EffectKind = work.Declaration.CompletionEffects?.Kind,
            EffectStatus = work.Declaration.CompletionEffects is null ? TurnWorkEffectStatus.NotSpecified
                : work.EffectsApplied ? TurnWorkEffectStatus.Applied : work.Cancelled ? TurnWorkEffectStatus.Cancelled : TurnWorkEffectStatus.AwaitingCompletion,
        };
    }
}
