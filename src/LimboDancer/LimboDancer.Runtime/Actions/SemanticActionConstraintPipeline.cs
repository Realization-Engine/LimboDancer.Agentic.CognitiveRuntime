using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class SemanticActionConstraintPipeline : IActionConstraintPipeline
{
    private readonly ReadOnlyDictionary<string, ISemanticPreconditionEvaluator> evaluators;

    public SemanticActionConstraintPipeline(IEnumerable<ISemanticPreconditionEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);
        var registered = new Dictionary<string, ISemanticPreconditionEvaluator>(StringComparer.Ordinal);
        foreach (var evaluator in evaluators)
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            ArgumentException.ThrowIfNullOrWhiteSpace(evaluator.EvaluatorId);
            if (!registered.TryAdd(evaluator.EvaluatorId, evaluator))
            {
                throw new InvalidOperationException(
                    $"Semantic precondition evaluator '{evaluator.EvaluatorId}' is already registered.");
            }
        }

        this.evaluators = new ReadOnlyDictionary<string, ISemanticPreconditionEvaluator>(registered);
    }

    public async Task<ConstraintPipelineResult> EvaluateAsync(
        IReadOnlyList<ActionCandidate> candidates,
        ConstraintContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(context);
        if (candidates.Any(static candidate => candidate is null))
        {
            throw new ArgumentException("Candidates cannot contain null values.", nameof(candidates));
        }

        var candidateIds = candidates.Select(static candidate => candidate.CandidateId).ToArray();
        if (candidateIds.Distinct(StringComparer.Ordinal).Count() != candidateIds.Length)
        {
            throw new ArgumentException("Candidate identifiers must be unique.", nameof(candidates));
        }

        var permitted = new List<PermittedAction>();
        var rejected = new List<RejectedCandidate>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await EvaluateCandidateAsync(candidate, context, cancellationToken).ConfigureAwait(false);
            if (results.All(static result => result.Outcome == ConstraintOutcome.Passed))
            {
                permitted.Add(new PermittedAction(candidate, results));
            }
            else
            {
                rejected.Add(new RejectedCandidate(candidate, results));
            }
        }

        return new ConstraintPipelineResult(permitted, rejected);
    }

    private async ValueTask<IReadOnlyList<ConstraintResult>> EvaluateCandidateAsync(
        ActionCandidate candidate,
        ConstraintContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<ConstraintResult>();
        foreach (var precondition in candidate.Descriptor.Preconditions.Where(static item => item.Required))
        {
            if (precondition.Kind != PreconditionKind.Semantic)
            {
                results.Add(new ConstraintResult(
                    candidate.CandidateId,
                    precondition.Id,
                    ConstraintAuthorityClass.Governance,
                    ConstraintOutcome.Indeterminate,
                    "constraint.not_evaluated"));
                continue;
            }

            if (!evaluators.TryGetValue(precondition.EvaluatorId, out var evaluator))
            {
                results.Add(new ConstraintResult(
                    candidate.CandidateId,
                    precondition.Id,
                    ConstraintAuthorityClass.Semantic,
                    ConstraintOutcome.Indeterminate,
                    "semantic.evaluator_unavailable"));
                continue;
            }

            var evaluation = await evaluator
                .EvaluateAsync(candidate, precondition, context, cancellationToken)
                .ConfigureAwait(false);
            results.Add(new ConstraintResult(
                candidate.CandidateId,
                precondition.Id,
                ConstraintAuthorityClass.Semantic,
                evaluation.Outcome,
                evaluation.ReasonCode,
                evaluation.EvidenceRefs));
        }

        return new ReadOnlyCollection<ConstraintResult>(results);
    }
}
