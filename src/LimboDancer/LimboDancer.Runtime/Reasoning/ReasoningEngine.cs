using System.Security.Cryptography;
using System.Text;
using LimboDancer.Abstractions.Reasoning;

namespace LimboDancer.Runtime.Reasoning;

public sealed class ReasoningEngine : IReasoningEngine
{
    private readonly IReasoningProvider provider;
    private readonly ReasoningGuard guard;

    public ReasoningEngine(IReasoningProvider provider, ReasoningGuard guard)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.ProviderId);
        if (provider.ProviderVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider.ProviderVersion);
        }

        ArgumentNullException.ThrowIfNull(guard);
        this.provider = provider;
        this.guard = guard;
    }

    public async Task<ReasoningEvaluation> ReasonAsync(
        ReasoningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await provider.ReasonAsync(context, cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(result);
        if (!string.Equals(result.ProviderId, provider.ProviderId, StringComparison.Ordinal)
            || !string.Equals(result.ProviderVersion, provider.ProviderVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Reasoning provider identity does not match its result.");
        }

        if (result.ObservationRequests.Any(request => request.TenantId != context.Goal.TenantId))
        {
            throw new InvalidOperationException("Reasoning observation requests must match the Goal tenant.");
        }

        var stepRecord = CreateStepRecord(context, result);
        return new ReasoningEvaluation(result, guard.Evaluate(context, result, stepRecord), stepRecord);
    }

    private static ReasoningStepRecord? CreateStepRecord(
        ReasoningContext context,
        ReasoningResult result)
    {
        if (result.Disposition != ReasoningDisposition.ProposedAction)
        {
            return null;
        }

        var intent = result.Intent!;
        var proposal = $"{intent.Value}\n{intent.Arguments.GetRawText()}";
        var state = context.Observations.Count == 0
            ? "state:none"
            : string.Join(
                "\n",
                context.Observations
                    .OrderBy(static observation => observation.ObservationId, StringComparer.Ordinal)
                    .Select(static observation =>
                        $"{observation.ObservationId}:{observation.Version ?? "unversioned"}"));
        if (context.ActionOutcomes.Count != 0)
        {
            state = string.Join(
                "\n",
                new[] { state }.Concat(context.ActionOutcomes.Select(static outcome =>
                    $"{outcome.StepId}:{outcome.SemanticIntent}:{outcome.Succeeded}:{outcome.Code}:{outcome.Output?.GetRawText()}")));
        }

        return new ReasoningStepRecord(
            intent.Value,
            Fingerprint(proposal),
            Fingerprint(state));
    }

    private static string Fingerprint(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
