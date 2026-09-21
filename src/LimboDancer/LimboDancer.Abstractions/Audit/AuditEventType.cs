namespace LimboDancer.Abstractions.Audit;

public enum AuditEventType
{
    InvocationAdmitted,
    ActionResolved,
    ConstraintEvaluated,
    DiagnosticEvaluated,
    GateAuthorized,
    GateDenied,
    GateStale,
    ExecutorStarted,
    ExecutorCompleted,
    ExecutorFailed,
}
