namespace LimboDancer.Host;

public sealed class LimboDancerHostOptions
{
    public const string SectionName = "LimboDancer";

    public string ServerName { get; set; } = "LimboDancer";

    public string ServerVersion { get; set; } = "0.1.0";

    public int InvocationTimeoutSeconds { get; set; } = 30;

    public string DecisionProvider { get; set; } = "Rule";

    public OpenAiDecisionProviderHostOptions OpenAiDecision { get; set; } = new();

    public List<ApiKeyCredentialOptions> ApiKeys { get; set; } = [];
}

public sealed class OpenAiDecisionProviderHostOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";

    public int TimeoutSeconds { get; set; } = 15;

    public int MaxOutputTokens { get; set; } = 256;

    public decimal InputCostPerMillionTokens { get; set; }

    public decimal OutputCostPerMillionTokens { get; set; }
}

public sealed class ApiKeyCredentialOptions
{
    public string Key { get; set; } = string.Empty;

    public string PrincipalId { get; set; } = string.Empty;

    public Guid TenantId
    {
        get;
        set;
    }

    public List<string> Permissions { get; set; } = [];
}
