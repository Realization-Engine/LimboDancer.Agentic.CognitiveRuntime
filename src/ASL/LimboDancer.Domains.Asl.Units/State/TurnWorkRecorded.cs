using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Permissioned adjudication. Eligibility and packing duration remain explicitly external rule facts.</summary>
public sealed record TurnWorkRecorded(string WorkId, string Operation, TurnWorkDeclaration? Declaration,
    string? Roll, string RuleBasis) : EventPayload;
