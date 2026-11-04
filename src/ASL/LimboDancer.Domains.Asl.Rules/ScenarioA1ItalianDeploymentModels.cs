namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.61 (p. 97): facts for the nationality restriction alone, not full deployment permission.</summary>
public sealed record ItalianDeploymentFacts(string? Nationality, string? Kind, bool? Elite, string Context, bool? Guard, bool? Unarmed, bool? Broken);

/// <summary>A known restriction answer, or an unknown/not applicable result. Passing it does not admit an action.</summary>
public sealed record ItalianDeploymentVerdict(string Outcome, string Reason)
{
    public bool? Restricted => Outcome switch
    {
        "restricted" => true,
        "unrestricted" => false,
        _ => null,
    };
}
