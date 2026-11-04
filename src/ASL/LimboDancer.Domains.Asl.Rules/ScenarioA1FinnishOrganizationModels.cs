namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.71 (p. 97): explicit action and direction mode, with raw exception facts.</summary>
public sealed record FinnishOrganizationFacts(string? Nationality, string? Kind, string Action, string Mode, bool? Guard, bool? Unarmed, bool? Broken);

/// <summary>Requirements only: neither a live leader's eligibility nor full action legality is certified.</summary>
public sealed record FinnishOrganizationVerdict(string Outcome, bool? LeaderRequired, int? BaseTcDrm, string Reason);
