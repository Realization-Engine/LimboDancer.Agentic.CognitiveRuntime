namespace LimboDancer.Infrastructure.Decision;

public sealed class OpenAiDecisionProviderOptions
{
    public OpenAiDecisionProviderOptions(
        string apiKey,
        string model,
        Uri endpoint,
        TimeSpan timeout,
        int maxOutputTokens,
        decimal inputCostPerMillionTokens,
        decimal outputCostPerMillionTokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("The OpenAI endpoint must be an absolute HTTPS URI.", nameof(endpoint));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOutputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(inputCostPerMillionTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputCostPerMillionTokens);
        ApiKey = apiKey;
        Model = model;
        Endpoint = endpoint;
        Timeout = timeout;
        MaxOutputTokens = maxOutputTokens;
        InputCostPerMillionTokens = inputCostPerMillionTokens;
        OutputCostPerMillionTokens = outputCostPerMillionTokens;
    }

    public string ApiKey
    {
        get;
    }

    public string Model
    {
        get;
    }

    public Uri Endpoint
    {
        get;
    }

    public TimeSpan Timeout
    {
        get;
    }

    public int MaxOutputTokens
    {
        get;
    }

    public decimal InputCostPerMillionTokens
    {
        get;
    }

    public decimal OutputCostPerMillionTokens
    {
        get;
    }
}
