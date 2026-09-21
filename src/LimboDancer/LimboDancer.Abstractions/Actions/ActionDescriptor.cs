using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace LimboDancer.Abstractions.Actions;

public sealed class ActionDescriptor
{
    public ActionDescriptor(
        ActionId id,
        ActionVersion version,
        string name,
        string? description,
        JsonElement inputSchema,
        JsonElement? outputSchema,
        ActionRiskProfile risk,
        IEnumerable<string>? requiredPermissions,
        IEnumerable<PreconditionDescriptor>? preconditions,
        IEnumerable<EffectDescriptor>? expectedEffects,
        IdempotencyMode idempotency,
        ExecutorBinding executor,
        DiagnosticProfile? diagnostics = null,
        VerificationProfile? verification = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(version.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(risk);
        ArgumentException.ThrowIfNullOrWhiteSpace(executor.Value);

        Id = id;
        Version = version;
        Name = name;
        Description = description;
        InputSchema = inputSchema.Clone();
        OutputSchema = outputSchema?.Clone();
        Risk = risk;
        var permissions = (requiredPermissions ?? []).ToArray();
        if (permissions.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Permission identifiers cannot be empty.", nameof(requiredPermissions));
        }

        RequiredPermissions = permissions.ToFrozenSet(StringComparer.Ordinal);
        Preconditions = new ReadOnlyCollection<PreconditionDescriptor>((preconditions ?? []).ToArray());
        ExpectedEffects = new ReadOnlyCollection<EffectDescriptor>((expectedEffects ?? []).ToArray());
        Idempotency = idempotency;
        Executor = executor;
        Diagnostics = diagnostics ?? DiagnosticProfile.Empty;
        Verification = verification ?? VerificationProfile.Default;
    }

    public ActionId Id
    {
        get;
    }

    public ActionVersion Version
    {
        get;
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

    public ActionRiskProfile Risk
    {
        get;
    }

    public IReadOnlySet<string> RequiredPermissions
    {
        get;
    }

    public IReadOnlyList<PreconditionDescriptor> Preconditions
    {
        get;
    }

    public IReadOnlyList<EffectDescriptor> ExpectedEffects
    {
        get;
    }

    public IdempotencyMode Idempotency
    {
        get;
    }

    public ExecutorBinding Executor
    {
        get;
    }

    public DiagnosticProfile Diagnostics
    {
        get;
    }

    public VerificationProfile Verification
    {
        get;
    }
}
