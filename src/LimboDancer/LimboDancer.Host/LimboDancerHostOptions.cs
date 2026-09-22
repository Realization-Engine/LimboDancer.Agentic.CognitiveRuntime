namespace LimboDancer.Host;

public sealed class LimboDancerHostOptions
{
    public const string SectionName = "LimboDancer";

    public string ServerName { get; set; } = "LimboDancer";

    public string ServerVersion { get; set; } = "0.1.0";

    public int InvocationTimeoutSeconds { get; set; } = 30;

    public List<ApiKeyCredentialOptions> ApiKeys { get; set; } = [];
}

public sealed class ApiKeyCredentialOptions
{
    public string Key { get; set; } = string.Empty;

    public string PrincipalId { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public List<string> Permissions { get; set; } = [];
}
