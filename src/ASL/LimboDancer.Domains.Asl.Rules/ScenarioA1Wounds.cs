namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Wounds (A17; pass 35, task 35.1): the one procedure every wound follows. Whenever a SMC is wounded, by Casualty Reduction or by a Sniper, a Wound
/// Severity dr is made at once: a 5 or 6 is mortal and is treated as a KIA, a 1 to 4 is minor; a man already wounded adds +1 to it, and being wounded
/// again has no other penalty (A17.11, p. 85). Play rolls the dr and writes the events.
/// </summary>
public static class ScenarioA1Wounds
{
    /// <summary>The purpose of a Wound Severity dr in the record of a roll.</summary>
    public const string SeverityPurpose = "wound-severity";

    /// <summary>A17.1: wounds are accounted for only for a SMC, so only a leader's or a hero's Casualty Reduction calls for the Wound Severity dr.</summary>
    public static bool SeverityDue(bool leaderOrHero) => leaderOrHero;

    /// <summary>A17.11: a Wound Severity dr of 5 or more, with +1 for a man already wounded, is mortal.</summary>
    public static bool Mortal(int severityDr, bool alreadyWounded) => severityDr + (alreadyWounded ? 1 : 0) >= 5;

    /// <summary>
    /// A17.2 (p. 85; pass 71, ruling R71.14): "A SMC who becomes wounded (e.g., by Defensive First Fire) after already expending &gt; 3 MF is pinned": a SMC
    /// wounded in the MPh past 3 MF, 3 1/2 counting. A routing SMC wounded in the RtPh is not read here (the backlog).
    /// </summary>
    public static bool PinnedByWound(bool smc, string? phase, int mfSpent, bool halfMfSpent) => smc && phase == "mph" && (mfSpent > 3 || (mfSpent == 3 && halfMfSpent));

    /// <summary>
    /// What a record says of a Wound Severity dr (A17.11): the man when the record knows him, the dr with the +1 of a man already wounded, the
    /// Final dr, and what it comes to.
    /// </summary>
    public static string SeverityText(int severityDr, string? unitId = null, bool alreadyWounded = false) =>
        $"Wound Severity dr {severityDr}{(alreadyWounded ? $" + 1 (already wounded) = {severityDr + 1}" : string.Empty)}{(unitId is null ? string.Empty : $" for {unitId}")}: "
            + $"{(Mortal(severityDr, alreadyWounded) ? "a mortal wound, eliminated" : "a wound, not mortal")}; a Final dr of 5 or more is mortal (A17.11)";
}
