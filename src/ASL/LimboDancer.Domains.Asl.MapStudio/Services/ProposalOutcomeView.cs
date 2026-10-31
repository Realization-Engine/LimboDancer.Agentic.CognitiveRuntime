using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Lifecycle wording only. The parent retains disclosure, confirmation eligibility, and execution.</summary>
public sealed record ProposalOutcomeView(string Lifecycle)
{
    public static ProposalOutcomeView From(PlayOutcome outcome) => new(outcome switch
    {
        PlayOutcome.NeedsConfirmation => "Awaiting confirmation",
        PlayOutcome.Committed => "Recorded",
        PlayOutcome.Replay => "Already recorded",
        PlayOutcome.Stale => "Proposal is stale",
        PlayOutcome.Denied => "Refused",
        _ => "Failed",
    });
}
