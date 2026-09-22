namespace LimboDancer.Abstractions.Audit;

public enum AuditEventType
{
    InvocationAdmitted,
    ActionResolved,
    ConstraintEvaluated,
    DecisionEvaluated,
    DecisionRejected,
    DiagnosticEvaluated,
    GateAuthorized,
    GateDenied,
    GateStale,
    ExecutorStarted,
    ExecutorCompleted,
    ExecutorFailed,
}
