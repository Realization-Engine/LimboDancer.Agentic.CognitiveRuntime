namespace LimboDancer.Abstractions.Diagnostics;

public enum DiagnosticDisposition
{
    Continue,
    ContinueDegraded,
    Retry,
    ReObserve,
    Escalate,
    Block,
    FailGoal,
}
