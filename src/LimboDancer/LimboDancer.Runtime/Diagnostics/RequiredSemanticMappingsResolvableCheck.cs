using System.Text.Json;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class RequiredSemanticMappingsResolvableCheck : IDiagnosticCheck<DiagnosticContext>
{
    private readonly IRequiredSemanticMappingResolver mappingResolver;
    private readonly TimeProvider timeProvider;

    public RequiredSemanticMappingsResolvableCheck(
        IRequiredSemanticMappingResolver mappingResolver,
        TimeProvider? timeProvider = null)
    {
        this.mappingResolver = mappingResolver ?? throw new ArgumentNullException(nameof(mappingResolver));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DiagnosticCheckId Id => WellKnownDiagnosticChecks.RequiredSemanticMappingsResolvable;

    public string Version => WellKnownDiagnosticChecks.InitialVersion;

    public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

    public Task<DiagnosticFinding> EvaluateAsync(
        DiagnosticContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var unresolved = context.RequiredSemanticMappingIds
            .Where(mappingId => !mappingResolver.CanResolve(mappingId))
            .ToArray();
        var resolved = unresolved.Length == 0;
        var evidence = resolved
            ? null
            : new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["unresolvedMappingIds"] = JsonSerializer.SerializeToElement(unresolved),
            };

        return Task.FromResult(new DiagnosticFinding(
            Id,
            Version,
            resolved ? DiagnosticOutcome.Pass : DiagnosticOutcome.Fail,
            resolved ? DiagnosticSeverity.Info : DiagnosticSeverity.Critical,
            resolved ? "semantic_mappings.resolved" : "semantic_mappings.unresolved",
            resolved
                ? "All required semantic mappings resolve."
                : "One or more required semantic mappings do not resolve.",
            timeProvider.GetUtcNow(),
            evidence));
    }
}
