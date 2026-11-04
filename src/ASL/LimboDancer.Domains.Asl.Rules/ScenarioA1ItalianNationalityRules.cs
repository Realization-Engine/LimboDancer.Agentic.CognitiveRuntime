namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.66 (p. 97): inheritance limited to this batch's deployment, capture and escape clauses.</summary>
public static class ScenarioA1ItalianNationalityRules
{
    public static bool UsesItalianDeploymentAndCapture(string? nationality) => nationality is "italian" or "eritrean";
}
