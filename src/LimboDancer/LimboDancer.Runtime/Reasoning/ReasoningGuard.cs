using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Reasoning;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Reasoning;

public sealed class ReasoningGuard
{
    private readonly IActionRegistry actionRegistry;
    private readonly TimeProvider timeProvider;

    public ReasoningGuard(IActionRegistry actionRegistry, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(actionRegistry);
        this.actionRegistry = actionRegistry;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ReasoningGuardResult Evaluate(
        ReasoningContext context,
        ReasoningResult result,
        ReasoningStepRecord? proposedStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);
        var reasons = new List<string>();
        if (timeProvider.GetUtcNow() >= context.Budget.Deadline)
        {
            reasons.Add("reasoning.deadline_exceeded");
        }

        if (result.Disposition == ReasoningDisposition.ProposedAction)
        {
            if (context.History.Count >= context.Budget.MaxSteps)
            {
                reasons.Add("reasoning.step_budget_exhausted");
            }

            ArgumentNullException.ThrowIfNull(result.Intent);
            ArgumentNullException.ThrowIfNull(proposedStep);
            if (!actionRegistry.TryGet(new ActionId(result.Intent.Value), version: null, out _))
            {
                reasons.Add("reasoning.semantic_intent_unresolved");
            }

            var previous = context.History.Count == 0
                ? null
                : context.History[^1];
            if (previous is not null
                && string.Equals(
                    previous.ProposalFingerprint,
                    proposedStep.ProposalFingerprint,
                    StringComparison.Ordinal)
                && string.Equals(
                    previous.StateFingerprint,
                    proposedStep.StateFingerprint,
                    StringComparison.Ordinal))
            {
                reasons.Add("reasoning.repeated_proposal_without_state_change");
            }
            else if (previous is not null
                && string.Equals(previous.SemanticIntent, proposedStep.SemanticIntent, StringComparison.Ordinal))
            {
                reasons.Add("reasoning.repeated_next_step");
            }
        }

        return reasons.Count == 0
            ? new ReasoningGuardResult(canContinue: true)
            : new ReasoningGuardResult(canContinue: false, reasons);
    }
}
