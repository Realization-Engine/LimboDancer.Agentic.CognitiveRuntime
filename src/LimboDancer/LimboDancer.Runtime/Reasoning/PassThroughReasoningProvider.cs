using System.Text.Json;
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
        if (context.ActionOutcomes.Count != 0)
        {
            var outcome = context.ActionOutcomes[^1];
            return Task.FromResult(outcome.Succeeded
                ? new ReasoningResult(
                    ReasoningDisposition.Completed,
                    ProviderId,
                    "reasoning.single_action_completed",
                    output: outcome.Output ?? JsonSerializer.SerializeToElement(
                        new
                        {
                            outcome.Code,
                        }),
                    providerVersion: ProviderVersion)
                : new ReasoningResult(
                    ReasoningDisposition.Abstained,
                    ProviderId,
                    "reasoning.action_failed",
                    providerVersion: ProviderVersion));
        }

        return Task.FromResult(new ReasoningResult(
            ReasoningDisposition.ProposedAction,
            ProviderId,
            "reasoning.structured_goal_passthrough",
            new SemanticActionIntent(context.Goal.Intent, context.Goal.Inputs),
            providerVersion: ProviderVersion));
    }
}
