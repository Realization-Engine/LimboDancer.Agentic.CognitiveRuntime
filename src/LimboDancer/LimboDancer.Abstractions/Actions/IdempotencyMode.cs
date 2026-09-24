namespace LimboDancer.Abstractions.Actions;

public enum IdempotencyMode
{
    Intrinsic,
    KeyRequired,
    NotSupported,
}
