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

        if (!string.Equals(options.DecisionProvider, "Rule", StringComparison.Ordinal)
            && !string.Equals(options.DecisionProvider, "OpenAI", StringComparison.Ordinal))
        {
            failures.Add("LimboDancer:DecisionProvider must be Rule or OpenAI.");
        }

        if (string.Equals(options.DecisionProvider, "OpenAI", StringComparison.Ordinal))
        {
            ValidateOpenAiDecision(options.OpenAiDecision, failures);
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

    private static void ValidateOpenAiDecision(
        OpenAiDecisionProviderHostOptions options,
        List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)
            || string.IsNullOrWhiteSpace(options.Model))
        {
            failures.Add("LimboDancer:OpenAiDecision requires ApiKey and a pinned Model.");
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("LimboDancer:OpenAiDecision:Endpoint must be an absolute HTTPS URI.");
        }

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("LimboDancer:OpenAiDecision:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.MaxOutputTokens < 1)
        {
            failures.Add("LimboDancer:OpenAiDecision:MaxOutputTokens must be positive.");
        }

        if (options.InputCostPerMillionTokens <= 0
            || options.OutputCostPerMillionTokens <= 0)
        {
            failures.Add("LimboDancer:OpenAiDecision model token prices must be positive.");
        }
    }
}
