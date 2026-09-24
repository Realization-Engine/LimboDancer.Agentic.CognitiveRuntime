namespace LimboDancer.Abstractions.Execution;

public enum ExecutionGateOutcome
{
    Authorized,
    Denied,
    Stale,
    ConfirmationRequired,
    DiagnosticBlocked,
}
