using Microsoft.Extensions.Options;

namespace LimboDancer.Host;

internal sealed class LimboDancerHostOptionsValidator : IValidateOptions<LimboDancerHostOptions>
{
    public ValidateOptionsResult Validate(string? name, LimboDancerHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ServerName))
        {
            failures.Add("LimboDancer:ServerName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ServerVersion))
        {
            failures.Add("LimboDancer:ServerVersion is required.");
        }

        if (options.InvocationTimeoutSeconds is < 1 or > 300)
        {
            failures.Add("LimboDancer:InvocationTimeoutSeconds must be between 1 and 300.");
        }

        for (var index = 0; index < options.ApiKeys.Count; index++)
        {
            var credential = options.ApiKeys[index];
            if (string.IsNullOrWhiteSpace(credential.Key)
                || string.IsNullOrWhiteSpace(credential.PrincipalId)
                || credential.TenantId == Guid.Empty)
            {
                failures.Add($"LimboDancer:ApiKeys:{index} must define Key, PrincipalId, and TenantId.");
            }

            if (credential.Permissions.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"LimboDancer:ApiKeys:{index}:Permissions cannot contain empty values.");
            }
        }

        if (options.ApiKeys
            .Where(static credential => !string.IsNullOrWhiteSpace(credential.Key))
            .GroupBy(static credential => credential.Key, StringComparer.Ordinal)
            .Any(static group => group.Count() > 1))
        {
            failures.Add("LimboDancer:ApiKeys cannot contain duplicate keys.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
