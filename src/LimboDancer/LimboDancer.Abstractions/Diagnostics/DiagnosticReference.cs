using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Diagnostics;

public sealed record DiagnosticReference
{
    public DiagnosticReference(
        DiagnosticCheckId id,
        string? version,
        bool required,
        bool isHardInvariant,
        GoalLifecycleState? phase,
        DiagnosticPosition position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id.Value);
        if (version is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
        }

        Id = id;
        Version = version;
        Required = required;
        IsHardInvariant = isHardInvariant;
        Phase = phase;
        Position = position;
    }

    public DiagnosticCheckId Id
    {
        get;
    }

    public string? Version
    {
        get;
    }

    public bool Required
    {
        get;
    }

    public bool IsHardInvariant
    {
        get;
    }

    public GoalLifecycleState? Phase
    {
        get;
    }

    public DiagnosticPosition Position
    {
        get;
    }
}
