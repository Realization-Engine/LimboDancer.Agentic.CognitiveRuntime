namespace LimboDancer.Abstractions.Actions;

public enum ActionMutability
{
    ReadOnly,
    Write,
}

public enum ActionIdempotency
{
    Idempotent,
    NonIdempotent,
}

public enum ActionReversibility
{
    Reversible,
    Compensatable,
    Irreversible,
}

public enum ActionBoundary
{
    Internal,
    ExternalSideEffect,
}

public enum ActionPrivilege
{
    Normal,
    Privileged,
}

public sealed record ActionRiskProfile(
    ActionMutability Mutability,
    ActionIdempotency Idempotency,
    ActionReversibility Reversibility,
    ActionBoundary Boundary,
    ActionPrivilege Privilege);
