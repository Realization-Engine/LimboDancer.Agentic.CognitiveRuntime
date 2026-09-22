using System.Collections.ObjectModel;
using System.Text.Json;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Adapters.Mcp;

public sealed record McpServerIdentity
{
    public McpServerIdentity(string name, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Name = name;
        Version = version;
    }

    public string Name
    {
        get;
    }

    public string Version
    {
        get;
    }
}

public sealed class McpDiscoveryResult
{
    public McpDiscoveryResult(McpServerIdentity server, IEnumerable<string> protocolVersions)
    {
        ArgumentNullException.ThrowIfNull(server);
        var versions = protocolVersions.ToArray();
        if (versions.Length == 0 || versions.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one protocol version is required.", nameof(protocolVersions));
        }

        Server = server;
        ProtocolVersions = new ReadOnlyCollection<string>(versions);
        SupportsTools = true;
    }

    public McpServerIdentity Server
    {
        get;
    }

    public IReadOnlyList<string> ProtocolVersions
    {
        get;
    }

    public bool SupportsTools
    {
        get;
    }
}

public sealed record McpLegacyInitializeRequest(
    string ProtocolVersion,
    string ClientName,
    string ClientVersion);

public sealed record McpLegacyInitializeResult(
    string ProtocolVersion,
    McpServerIdentity Server,
    bool SupportsTools);

public sealed record McpRequestMetadata
{
    public McpRequestMetadata(string protocolVersion, string clientName, string clientVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientVersion);
        ProtocolVersion = protocolVersion;
        ClientName = clientName;
        ClientVersion = clientVersion;
    }

    public string ProtocolVersion
    {
        get;
    }

    public string ClientName
    {
        get;
    }

    public string ClientVersion
    {
        get;
    }
}

public sealed class McpToolDefinition
{
    public McpToolDefinition(
        string name,
        string? description,
        JsonElement inputSchema,
        JsonElement? outputSchema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        InputSchema = inputSchema.Clone();
        OutputSchema = outputSchema?.Clone();
    }

    public string Name
    {
        get;
    }

    public string? Description
    {
        get;
    }

    public JsonElement InputSchema
    {
        get;
    }

    public JsonElement? OutputSchema
    {
        get;
    }
}

public sealed class McpToolCallRequest
{
    public McpToolCallRequest(string name, JsonElement arguments, McpRequestMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(metadata);
        Name = name;
        Arguments = arguments.Clone();
        Metadata = metadata;
    }

    public string Name
    {
        get;
    }

    public JsonElement Arguments
    {
        get;
    }

    public McpRequestMetadata Metadata
    {
        get;
    }
}

public sealed class McpCallerContext
{
    public McpCallerContext(
        Guid tenantId,
        RuntimePrincipal principal,
        IEnumerable<DiagnosticFinding>? diagnosticFindings = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(principal);
        var findings = (diagnosticFindings ?? []).ToArray();
        if (findings.Any(static finding => finding is null))
        {
            throw new ArgumentException("Diagnostic findings cannot be null.", nameof(diagnosticFindings));
        }

        TenantId = tenantId;
        Principal = principal;
        DiagnosticFindings = new ReadOnlyCollection<DiagnosticFinding>(findings);
    }

    public Guid TenantId
    {
        get;
    }

    public RuntimePrincipal Principal
    {
        get;
    }

    public IReadOnlyList<DiagnosticFinding> DiagnosticFindings
    {
        get;
    }
}

public sealed record McpProtocolError(int Code, string Category, string RuntimeCode, string Message);

public sealed class McpToolCallResult
{
    private McpToolCallResult(JsonElement? content, McpProtocolError? error)
    {
        Content = content?.Clone();
        Error = error;
    }

    public JsonElement? Content
    {
        get;
    }

    public McpProtocolError? Error
    {
        get;
    }

    public bool IsError => Error is not null;

    public static McpToolCallResult Success(JsonElement? content) => new(content, error: null);

    public static McpToolCallResult Failure(McpProtocolError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new McpToolCallResult(content: null, error: error);
    }
}
