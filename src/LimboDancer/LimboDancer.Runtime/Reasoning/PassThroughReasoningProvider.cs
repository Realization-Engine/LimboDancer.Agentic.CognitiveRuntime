using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Reasoning;

namespace LimboDancer.Runtime.Reasoning;

public sealed class PassThroughReasoningProvider : IReasoningProvider
{
    public const string Id = "runtime:reasoning/PassThrough";
    public const string Version = "1";

    public string ProviderId => Id;

    public string ProviderVersion => Version;

    public Task<ReasoningResult> ReasonAsync(
        ReasoningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ReasoningResult(
            ReasoningDisposition.ProposedAction,
            ProviderId,
            "reasoning.structured_goal_passthrough",
            new SemanticActionIntent(context.Goal.Intent, context.Goal.Inputs),
            providerVersion: ProviderVersion));
    }
}
