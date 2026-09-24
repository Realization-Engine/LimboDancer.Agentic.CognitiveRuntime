using Microsoft.Extensions.Hosting;

namespace LimboDancer.Host;

internal sealed class RuntimeStartupValidationService : IHostedService
{
    private readonly RuntimeStructureValidator validator;

    public RuntimeStartupValidationService(RuntimeStructureValidator validator)
    {
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = validator.Validate();
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                $"Runtime structural validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, result.Failures)}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
