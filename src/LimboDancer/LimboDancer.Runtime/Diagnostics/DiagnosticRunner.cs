using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class DiagnosticRunner : IDiagnosticRunner, IDiagnosticProfileValidator
{
    private readonly Dictionary<(DiagnosticCheckId Id, string Version), IDiagnosticCheck<DiagnosticContext>> checks;
    private readonly Dictionary<DiagnosticCheckId, IDiagnosticCheck<DiagnosticContext>> unambiguousChecks;
    private readonly TimeSpan checkTimeout;
    private readonly TimeProvider timeProvider;
    private readonly AsyncLocal<int> executionDepth = new();

    public DiagnosticRunner(
        IEnumerable<IDiagnosticCheck<DiagnosticContext>> checks,
        TimeSpan? checkTimeout = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(checks);

        this.checkTimeout = checkTimeout ?? TimeSpan.FromSeconds(5);
        if (this.checkTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(checkTimeout),
                checkTimeout,
                "The diagnostic timeout must be greater than zero.");
        }

        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.checks = [];
        foreach (var check in checks)
        {
            ArgumentNullException.ThrowIfNull(check);
            ArgumentException.ThrowIfNullOrWhiteSpace(check.Id.Value);
            ArgumentException.ThrowIfNullOrWhiteSpace(check.Version);

            if (!this.checks.TryAdd((check.Id, check.Version), check))
            {
                throw new InvalidOperationException(
                    $"Diagnostic check '{check.Id}' version '{check.Version}' is already registered.");
            }
        }

        unambiguousChecks = this.checks.Values
            .GroupBy(static check => check.Id)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single());
    }

    public void EnsureRequiredChecksResolvable(DiagnosticProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        foreach (var reference in profile.Checks.Where(static reference => reference.Required))
        {
            if (!TryResolve(reference, out _))
            {
                throw new InvalidOperationException(
                    $"Required diagnostic check '{reference.Id}' version '{reference.Version ?? "<unambiguous>"}' is not registered.");
            }
        }
    }

    public async Task<IReadOnlyList<DiagnosticFinding>> RunAsync(
        DiagnosticContext context,
        DiagnosticProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        if (executionDepth.Value != 0)
        {
            throw new InvalidOperationException("Recursive diagnostic execution is not allowed.");
        }

        executionDepth.Value++;
        try
        {
            var findings = new List<DiagnosticFinding>();
            foreach (var reference in profile.Checks)
            {
                if (reference.Position != context.Position
                    || (reference.Phase is { } phase && phase != context.Phase))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!TryResolve(reference, out var check))
                {
                    findings.Add(CreateMissingCheckFinding(reference));
                    continue;
                }

                using var timeoutSource = new CancellationTokenSource(checkTimeout, timeProvider);
                using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

                try
                {
                    var evaluation = check.EvaluateAsync(context, linkedSource.Token);
                    var finding = await evaluation
                        .WaitAsync(checkTimeout, timeProvider, cancellationToken)
                        .ConfigureAwait(false);
                    findings.Add(ValidateFindingIdentity(check, finding));
                }
                catch (TimeoutException)
                {
                    findings.Add(CreateTimeoutFinding(check));
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    findings.Add(CreateTimeoutFinding(check));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    findings.Add(CreateEvaluationErrorFinding(check, exception));
                }
            }

            return new ReadOnlyCollection<DiagnosticFinding>(findings);
        }
        finally
        {
            executionDepth.Value--;
        }
    }

    private bool TryResolve(
        DiagnosticReference reference,
        [NotNullWhen(true)] out IDiagnosticCheck<DiagnosticContext>? check)
    {
        if (reference.Version is { } version)
        {
            if (checks.TryGetValue((reference.Id, version), out check)
                && check.Position == reference.Position)
            {
                return true;
            }

            check = null;
            return false;
        }

        if (unambiguousChecks.TryGetValue(reference.Id, out check)
            && check.Position == reference.Position)
        {
            return true;
        }

        check = null;
        return false;
    }

    private DiagnosticFinding ValidateFindingIdentity(
        IDiagnosticCheck<DiagnosticContext> check,
        DiagnosticFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        if (finding.CheckId == check.Id
            && string.Equals(finding.CheckVersion, check.Version, StringComparison.Ordinal))
        {
            return finding;
        }

        return new DiagnosticFinding(
            check.Id,
            check.Version,
            DiagnosticOutcome.Indeterminate,
            DiagnosticSeverity.Critical,
            "diagnostic.invalid_finding_identity",
            "The diagnostic check returned a finding with a mismatched identity.",
            timeProvider.GetUtcNow());
    }

    private DiagnosticFinding CreateMissingCheckFinding(DiagnosticReference reference) => new(
        reference.Id,
        reference.Version ?? "unresolved",
        DiagnosticOutcome.Indeterminate,
        reference.Required ? DiagnosticSeverity.Critical : DiagnosticSeverity.Warning,
        "diagnostic.check_missing",
        reference.Required
            ? "A required diagnostic check is not registered."
            : "An optional diagnostic check is not registered.",
        timeProvider.GetUtcNow());

    private DiagnosticFinding CreateTimeoutFinding(IDiagnosticCheck check) => new(
        check.Id,
        check.Version,
        DiagnosticOutcome.Indeterminate,
        DiagnosticSeverity.Error,
        "diagnostic.timeout",
        "The diagnostic check exceeded its bounded execution time.",
        timeProvider.GetUtcNow());

    private DiagnosticFinding CreateEvaluationErrorFinding(
        IDiagnosticCheck check,
        Exception exception) => new(
            check.Id,
            check.Version,
            DiagnosticOutcome.Indeterminate,
            DiagnosticSeverity.Error,
            "diagnostic.evaluation_error",
            "The diagnostic check could not complete its evaluation.",
            timeProvider.GetUtcNow(),
            new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal)
            {
                ["exceptionType"] = System.Text.Json.JsonSerializer.SerializeToElement(exception.GetType().FullName),
            });
}
