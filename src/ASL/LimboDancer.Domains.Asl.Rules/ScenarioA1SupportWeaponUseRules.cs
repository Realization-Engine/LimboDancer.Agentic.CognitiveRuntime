namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Shared weapon-count ceiling for fire, repair and configuration: A7.35 (PDF p. 56), A9.72/.8 (p. 65).</summary>
public static class ScenarioA1SupportWeaponUseRules
{
    /// <summary>The existing fire calculator's count ceiling. Individual weapon and participant restrictions apply separately.</summary>
    public static int Capacity(string kind) => kind == "asl:squad" ? 2 : 1;

    /// <summary>A.15, A8.4 and A9.8: First Fire and Final Fire share defensive weapon-use history.</summary>
    public static bool RetainHistory(string? previousPhase, string? nextPhase) => previousPhase == "mph" && nextPhase == "dfph";

    public static bool SameUseWindow(string usedPhase, string currentPhase) => usedPhase == currentPhase || RetainHistory(usedPhase, currentPhase);

    /// <summary>Configuration consumes all of that weapon's ROF and its operator's use slot, including at night without a fire counter.</summary>
    public static string? FireAfterConfiguration(string phase, string kind, IReadOnlyList<string> used, IReadOnlyList<string> configured,
        bool inherentUsed, IReadOnlyList<string> requested, bool inherentRequested, bool configuredDuringPrep)
    {
        if (phase == "afph" && configuredDuringPrep)
        {
            return "This unit configured a weapon during Prep Fire and cannot fire now.";
        }

        if (phase is not ("pfph" or "dfph") || configured.Count == 0)
        {
            return null;
        }
        if (requested.Any(configured.Contains))
        {
            return "A configured weapon has used all its fire allowance this phase.";
        }
        var weapons = used.Concat(requested).Distinct(StringComparer.Ordinal).Count();
        return weapons + (inherentUsed || inherentRequested ? 1 : 0) > Capacity(kind)
            ? "This attack exceeds the unit's remaining weapon-use allowance." : null;
    }
}
