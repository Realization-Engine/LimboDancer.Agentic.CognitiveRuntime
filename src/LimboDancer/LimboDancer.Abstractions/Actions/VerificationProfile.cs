namespace LimboDancer.Abstractions.Actions;

public sealed record VerificationProfile(bool RequireEffectVerification)
{
    public static VerificationProfile Default
    {
        get;
    } = new(false);
}
