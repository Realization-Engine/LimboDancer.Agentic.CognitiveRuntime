using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LimboDancer.Host;

internal sealed class RuntimeReadinessHealthCheck : IHealthCheck
{
    private readonly RuntimeStructureValidator validator;

    public RuntimeReadinessHealthCheck(RuntimeStructureValidator validator)
    {
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = validator.Validate();
        return Task.FromResult(result.IsValid
            ? HealthCheckResult.Healthy("The runtime composition is structurally ready.")
            : HealthCheckResult.Unhealthy(
                "The runtime composition is structurally invalid.",
                data: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["failures"] = result.Failures,
                }));
    }
}
