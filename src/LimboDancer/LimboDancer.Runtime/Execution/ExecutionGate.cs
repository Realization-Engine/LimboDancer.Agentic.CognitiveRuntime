using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;

namespace LimboDancer.Runtime.Execution;

public sealed class ExecutionGate : IExecutionGate
{
    private readonly IActionRegistry actionRegistry;
    private readonly IActionExecutorResolver executorResolver;
    private readonly IActionConstraintEvaluator constraintEvaluator;
    private readonly IDiagnosticPolicy diagnosticPolicy;
    private readonly IExecutionRiskPolicy riskPolicy;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public ExecutionGate(
        IActionRegistry actionRegistry,
        IActionExecutorResolver executorResolver,
        IActionConstraintEvaluator constraintEvaluator,
        IDiagnosticPolicy diagnosticPolicy,
        IExecutionRiskPolicy riskPolicy,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.constraintEvaluator = constraintEvaluator ?? throw new ArgumentNullException(nameof(constraintEvaluator));
        this.diagnosticPolicy = diagnosticPolicy ?? throw new ArgumentNullException(nameof(diagnosticPolicy));
        this.riskPolicy = riskPolicy ?? throw new ArgumentNullException(nameof(riskPolicy));
        this.auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ExecutionGateResult> AuthorizeAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await AuthorizeCoreAsync(action, context, cancellationToken).ConfigureAwait(false);
        await WriteGateAuditAsync(action, context, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<ExecutionGateResult> AuthorizeCoreAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty
            || context.Principal.TenantId != context.TenantId)
        {
            return Denied("tenant.invalid");
        }

        if (!context.Principal.IsAuthenticated)
        {
            return Denied("principal.unauthenticated");
        }

        var descriptor = action.Candidate.Descriptor;
        if (!actionRegistry.TryGet(descriptor.Id, descriptor.Version, out var registeredDescriptor)
            || !ReferenceEquals(descriptor, registeredDescriptor))
        {
            return Result(ExecutionGateOutcome.Stale, "descriptor.stale");
        }

        if (!executorResolver.TryResolve(descriptor.Executor, out var executor)
            || executor.ActionId != descriptor.Id)
        {
            return Denied("executor.unresolved");
        }

        var missingPermissions = descriptor.RequiredPermissions
            .Where(permission => !context.Principal.Permissions.Contains(permission))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (missingPermissions.Length != 0)
        {
            return new ExecutionGateResult(
                ExecutionGateOutcome.Denied,
                authorizedAction: null,
                missingPermissions.Select(static permission => $"permission.missing:{permission}"));
        }

        ConstraintEvaluationResult constraintResult;
        try
        {
            constraintResult = await constraintEvaluator
                .EvaluateAsync(action, context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            await WriteConstraintAuditAsync(
                    action,
                    context,
                    ConstraintEvaluationOutcome.Indeterminate.ToString(),
                    ["constraint.indeterminate"],
                    cancellationToken)
                .ConfigureAwait(false);
            return Denied("constraint.indeterminate");
        }

        if (constraintResult is null)
        {
            await WriteConstraintAuditAsync(
                    action,
                    context,
                    ConstraintEvaluationOutcome.Indeterminate.ToString(),
                    ["constraint.indeterminate"],
                    cancellationToken)
                .ConfigureAwait(false);
            return Denied("constraint.indeterminate");
        }

        await WriteConstraintAuditAsync(
                action,
                context,
                constraintResult.Outcome.ToString(),
                constraintResult.ReasonCodes,
                cancellationToken)
            .ConfigureAwait(false);

        if (constraintResult.Outcome == ConstraintEvaluationOutcome.Stale)
        {
            return new ExecutionGateResult(
                ExecutionGateOutcome.Stale,
                authorizedAction: null,
                ReasonsOrDefault(constraintResult.ReasonCodes, "state.stale"));
        }

        if (constraintResult.Outcome != ConstraintEvaluationOutcome.Satisfied)
        {
            return new ExecutionGateResult(
                ExecutionGateOutcome.Denied,
                authorizedAction: null,
                ReasonsOrDefault(constraintResult.ReasonCodes, "constraint.not_satisfied"));
        }

        var diagnosticFailure = await EvaluateDiagnosticsAsync(
                descriptor,
                action,
                context,
                cancellationToken)
            .ConfigureAwait(false);
        if (diagnosticFailure is not null)
        {
            return diagnosticFailure;
        }

        RiskEvaluationResult riskResult;
        try
        {
            riskResult = riskPolicy.Evaluate(action, context);
        }
        catch (Exception)
        {
            return Denied("risk.indeterminate");
        }

        if (riskResult is null)
        {
            return Denied("risk.indeterminate");
        }

        if (riskResult.Outcome == RiskEvaluationOutcome.ConfirmationRequired)
        {
            return new ExecutionGateResult(
                ExecutionGateOutcome.ConfirmationRequired,
                authorizedAction: null,
                ReasonsOrDefault(riskResult.ReasonCodes, "confirmation.required"));
        }

        if (riskResult.Outcome != RiskEvaluationOutcome.Allowed)
        {
            return new ExecutionGateResult(
                ExecutionGateOutcome.Denied,
                authorizedAction: null,
                ReasonsOrDefault(riskResult.ReasonCodes, "risk.denied"));
        }

        var authorized = new AuthorizedAction(
            Guid.NewGuid().ToString("N"),
            action,
            context.InvocationId,
            context.CorrelationId,
            context.TenantId,
            context.Principal.PrincipalId,
            timeProvider.GetUtcNow(),
            expiresAt: null,
            constraintResult.ValidatedStateVersions);
        return new ExecutionGateResult(ExecutionGateOutcome.Authorized, authorized);
    }

    private async Task<ExecutionGateResult?> EvaluateDiagnosticsAsync(
        ActionDescriptor descriptor,
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var reference in descriptor.Diagnostics.Checks)
        {
            if (reference.Position != DiagnosticPosition.PreFlight
                || (reference.Phase is { } phase && phase != GoalLifecycleState.Gating))
            {
                continue;
            }

            var finding = context.DiagnosticFindings.FirstOrDefault(candidate =>
                candidate.CheckId == reference.Id
                && (reference.Version is null
                    || string.Equals(reference.Version, candidate.CheckVersion, StringComparison.Ordinal)));
            if (finding is null)
            {
                if (reference.Required)
                {
                    await WriteDiagnosticAuditAsync(
                            action,
                            context,
                            finding: null,
                            disposition: null,
                            outcomeCode: "MissingRequired",
                            reasonCodes: [$"diagnostic.missing:{reference.Id}"],
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return Result(
                        ExecutionGateOutcome.DiagnosticBlocked,
                        $"diagnostic.missing:{reference.Id}");
                }

                continue;
            }

            DiagnosticDisposition disposition;
            try
            {
                disposition = diagnosticPolicy.Evaluate(
                    finding,
                    new DiagnosticPolicyContext(reference, descriptor.Risk));
            }
            catch (Exception)
            {
                await WriteDiagnosticAuditAsync(
                        action,
                        context,
                        finding,
                        disposition: null,
                        outcomeCode: "PolicyIndeterminate",
                        reasonCodes: [$"diagnostic.indeterminate:{reference.Id}"],
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Result(
                    ExecutionGateOutcome.DiagnosticBlocked,
                    $"diagnostic.indeterminate:{reference.Id}");
            }

            await WriteDiagnosticAuditAsync(
                    action,
                    context,
                    finding,
                    disposition,
                    disposition.ToString(),
                    reasonCodes: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (disposition is not DiagnosticDisposition.Continue
                and not DiagnosticDisposition.ContinueDegraded)
            {
                return Result(
                    ExecutionGateOutcome.DiagnosticBlocked,
                    $"diagnostic.blocked:{reference.Id}");
            }
        }

        return null;
    }

    private ValueTask WriteConstraintAuditAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        string outcomeCode,
        IReadOnlyList<string> reasonCodes,
        CancellationToken cancellationToken) => auditSink.WriteAsync(
            CreateAuditEvent(
                AuditEventType.ConstraintEvaluated,
                action,
                context,
                outcomeCode: outcomeCode,
                reasonCodes: reasonCodes),
            cancellationToken);

    private ValueTask WriteDiagnosticAuditAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        DiagnosticFinding? finding,
        DiagnosticDisposition? disposition,
        string outcomeCode,
        IReadOnlyList<string>? reasonCodes,
        CancellationToken cancellationToken) => auditSink.WriteAsync(
            CreateAuditEvent(
                AuditEventType.DiagnosticEvaluated,
                action,
                context,
                outcomeCode: outcomeCode,
                reasonCodes: reasonCodes,
                diagnosticFinding: finding,
                diagnosticDisposition: disposition),
            cancellationToken);

    private ValueTask WriteGateAuditAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        ExecutionGateResult result,
        CancellationToken cancellationToken)
    {
        var eventType = result.Outcome switch
        {
            ExecutionGateOutcome.Authorized => AuditEventType.GateAuthorized,
            ExecutionGateOutcome.Stale => AuditEventType.GateStale,
            _ => AuditEventType.GateDenied,
        };

        return auditSink.WriteAsync(
            CreateAuditEvent(
                eventType,
                action,
                context,
                outcomeCode: result.Outcome.ToString(),
                reasonCodes: result.ReasonCodes,
                executionGateOutcome: result.Outcome,
                authorizationId: result.AuthorizedAction?.AuthorizationId),
            cancellationToken);
    }

    private RuntimeAuditEvent CreateAuditEvent(
        AuditEventType eventType,
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        string? outcomeCode = null,
        IReadOnlyList<string>? reasonCodes = null,
        DiagnosticFinding? diagnosticFinding = null,
        DiagnosticDisposition? diagnosticDisposition = null,
        ExecutionGateOutcome? executionGateOutcome = null,
        string? authorizationId = null) => new(
            Guid.NewGuid(),
            eventType,
            context.InvocationId,
            context.CorrelationId,
            context.TenantId,
            timeProvider.GetUtcNow(),
            action.Candidate.Descriptor.Id,
            action.Candidate.Descriptor.Version,
            context.Principal.PrincipalId,
            action.Candidate.CandidateId,
            action.Origin,
            outcomeCode,
            reasonCodes,
            diagnosticFinding is null
                ? null
                : new AuditDiagnosticFinding(
                    diagnosticFinding.CheckId,
                    diagnosticFinding.CheckVersion,
                    diagnosticFinding.Outcome,
                    diagnosticFinding.Severity,
                    diagnosticFinding.Code),
            diagnosticDisposition,
            executionGateOutcome,
            authorizationId);

    private static ExecutionGateResult Denied(string reasonCode) =>
        Result(ExecutionGateOutcome.Denied, reasonCode);

    private static ExecutionGateResult Result(
        ExecutionGateOutcome outcome,
        string reasonCode) => new(outcome, authorizedAction: null, [reasonCode]);

    private static IReadOnlyList<string> ReasonsOrDefault(
        IReadOnlyList<string> reasons,
        string defaultReason) => reasons.Count == 0 ? [defaultReason] : reasons;
}
