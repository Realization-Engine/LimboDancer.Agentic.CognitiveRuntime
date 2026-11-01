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
        bool inherentUsed, IReadOnlyList<string> requested, bool inherentRequested, bool configuredDuringPrep) =>
        ConfigurationFireUse(phase, kind, used, configured, inherentUsed, requested, inherentRequested, configuredDuringPrep).Reason;

    /// <summary>A7.353 (PDF p. 56): prior First Fire use does not consume the restored Final Fire allowance. The complete fire calculator still checks adjacency and halves FP.</summary>
    public static SupportWeaponFireUse ConfigurationFireUse(string phase, string kind, IReadOnlyList<string> used, IReadOnlyList<string> configured,
        bool inherentUsed, IReadOnlyList<string> requested, bool inherentRequested, bool configuredDuringPrep,
        bool firstFireMarked = false, IReadOnlyList<string>? usedThisPhase = null, bool inherentUsedThisPhase = false, bool weaponFirstFireMarked = false)
    {
        if (phase == "afph" && configuredDuringPrep)
        {
            return new("This unit configured a weapon during Prep Fire and cannot fire now.", false);
        }

        if (phase is not ("pfph" or "dfph") || configured.Count == 0)
        {
            return new(null, false);
        }
        if (requested.Any(configured.Contains))
        {
            return new("play.fire-sw-limit: A configured weapon has used all its fire allowance this phase (A9.8).", false);
        }
        var restores = ScenarioA1FinalFireCalculator.FiresAgain(phase == "dfph", firstFireMarked || weaponFirstFireMarked)
            && inherentRequested && !inherentUsedThisPhase && usedThisPhase is not null
            && (inherentUsed || used.Distinct(StringComparer.Ordinal).Count() >= Capacity(kind));
        var weapons = (restores ? usedThisPhase! : used).Concat(requested).Distinct(StringComparer.Ordinal).Count();
        var reason = weapons + (inherentUsed || inherentRequested ? 1 : 0) > Capacity(kind)
            ? "play.fire-sw-limit: This attack exceeds the unit's remaining weapon-use allowance." : null;
        return new(reason, reason is null && restores);
    }

    public static string? FirerLimitBar(FirerLimitFacts facts, bool restoresInherent) =>
        restoresInherent ? null : ScenarioA1FireEligibility.FirerLimitBar(facts);

    public static FireFirer Firer(FirerRecordFacts facts, bool restoresInherent) => restoresInherent
        ? ScenarioA1FireEligibility.Firer(facts) with
        {
            FirstFireMarked = true,
        }
        : ScenarioA1FireEligibility.Firer(facts);

    /// <summary>The fire calculator's A7.351/.352 and A15.23 reading, also used when a historical record omits the optional inherent-FP field.</summary>
    public static bool UsesInherentFp(bool? requested, string kind, bool hero, int weapons) => requested != false
        && !(kind == "asl:squad" && weapons >= 2) && !((kind == "asl:half-squad" || hero) && weapons >= 1);
}

public sealed record SupportWeaponFireUse(string? Reason, bool RestoresInherent);
