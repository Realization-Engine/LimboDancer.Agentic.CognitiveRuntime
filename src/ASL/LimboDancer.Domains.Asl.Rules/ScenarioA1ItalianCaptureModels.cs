namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One defender's facts, never the most favorable member of a defending group.</summary>
public sealed record ItalianCaptureFacts(string? Nationality, bool? Elite, bool? Inexperienced);

/// <summary>The alternative ordinary capture term; other attack modifiers are independent.</summary>
public sealed record ItalianCaptureVerdict(int? CaptureDrm, string Reason);

/// <summary>Explicit Rules-only escape reading. No serialized game fact changes in this claim.</summary>
public enum ItalianEscapeContext
{
    Legacy,
    WithEritreanInheritance,
}
