using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>A supply counter is either aboard Vehicle or at DumpLocation. Unavailable identities cannot be reused.</summary>
public sealed record TurnAmmoSupply(string Id, string? Vehicle, string Owner, bool Available, string? DumpLocation = null);

/// <summary>Permissioned adjudication. Eligibility and packing duration remain explicitly external rule facts.</summary>
public sealed record TurnWorkRecorded(string WorkId, string Operation, TurnWorkDeclaration? Declaration,
    string? Roll, string RuleBasis) : EventPayload
{
    public const string AuditedType = "turn-work-audited";
    public const string LifecycleType = "turn-work-recorded";
    public const string EffectsType = "turn-work-effects-recorded";
}
