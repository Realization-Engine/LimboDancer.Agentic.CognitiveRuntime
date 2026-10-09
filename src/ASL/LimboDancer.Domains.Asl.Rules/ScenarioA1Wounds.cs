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

    /// <summary>What a record says of a Wound Severity dr.</summary>
    public static string SeverityText(int severityDr) => $"Wound Severity dr {severityDr}: a 5 or more is mortal, with +1 for a man already wounded (A17.11)";
}
