using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>An adjudicated Ammo Supply counter bound to a vehicle. Unavailable counters cannot be reused.</summary>
public sealed record TurnAmmoSupply(string Id, string Vehicle, string Owner, bool Available);

/// <summary>Permissioned adjudication. Eligibility and packing duration remain explicitly external rule facts.</summary>
public sealed record TurnWorkRecorded(string WorkId, string Operation, TurnWorkDeclaration? Declaration,
    string? Roll, string RuleBasis) : EventPayload
{
    public const string LifecycleType = "turn-work-recorded";
    public const string EffectsType = "turn-work-effects-recorded";
}
