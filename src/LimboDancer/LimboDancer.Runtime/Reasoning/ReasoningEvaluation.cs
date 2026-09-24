using LimboDancer.Abstractions.Reasoning;

namespace LimboDancer.Runtime.Reasoning;

public sealed class ReasoningEvaluation
{
    public ReasoningEvaluation(
        ReasoningResult result,
        ReasoningGuardResult guard,
        ReasoningStepRecord? stepRecord)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(guard);
        if ((result.Disposition == ReasoningDisposition.ProposedAction) != (stepRecord is not null))
        {
            throw new ArgumentException("Only an action proposal may create a Reasoning step record.", nameof(stepRecord));
        }

        Result = result;
        Guard = guard;
        StepRecord = stepRecord;
    }

    public ReasoningResult Result
    {
        get;
    }

    public ReasoningGuardResult Guard
    {
        get;
    }

    public ReasoningStepRecord? StepRecord
    {
        get;
    }
}
