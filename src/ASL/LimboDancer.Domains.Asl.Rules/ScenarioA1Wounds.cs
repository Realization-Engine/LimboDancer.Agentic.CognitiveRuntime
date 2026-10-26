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
    /// wounded in the MPh past 3 MF, 3 1/2 counting. A routing SMC wounded in the RtPh is not read here (the backlog). Ruling R71.27: never a broken SMC, A7.8
    /// (p. 58): "Pinning affects broken units only during Interdiction ... there is no need to place Pin counters on broken units".
    /// </summary>
    public static bool PinnedByWound(bool smc, bool broken, string? phase, int mfSpent, bool halfMfSpent) =>
        smc && !broken && phase == "mph" && (mfSpent > 3 || (mfSpent == 3 && halfMfSpent));

    /// <summary>
    /// As above, with <paramref name="extraHalfMf"/> half MF the wounding act itself costs added to those spent (pass 71, ruling R71.14; R3's finding 4): a
    /// Search casualty counts the Search's MF among his (A12.152, p. 79).
    /// </summary>
    public static bool PinnedByWound(bool smc, bool broken, string? phase, int mfSpent, bool halfMfSpent, int extraHalfMf)
    {
        var halves = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + extraHalfMf;
        return PinnedByWound(smc, broken, phase, halves / 2, halves % 2 == 1);
    }

    /// <summary>A17.2 (p. 85): "A Good Order wounded man who is not portaging a SW may be carried by any Good Order MMC at a cost of five PP".</summary>
    public const int CarriedManPp = 5;

    /// <summary>
    /// The PP a unit portages (A4.4, A4.42) with a wounded man it carries in this move (A17.2; pass 71, ruling R71.16): his five PP join its SW's, and,
    /// as a SW's, are not recouped once he is set down (A4.4). Null stays null: a SW whose PP are not recorded leaves the portage undecided.
    /// </summary>
    public static int[]? Portage(int[]? weapons, bool carriesMan) => weapons is null || !carriesMan ? weapons : [.. weapons, CarriedManPp];

    /// <summary>The half MF a mover spends with a step (A17.2; ruling R71.16): none for a wounded man carried, who spends no MF of his own.</summary>
    public static int MoverHalfMf(bool carried, int halfMf) => carried ? 0 : halfMf;

    /// <summary>
    /// The carries a step names (A17.2, p. 85; pass 71, ruling R71.16), as the planner proposes them and the projector checks the record: each man and his
    /// carrier in the step's stack; the man wounded, in Good Order, and "not portaging a SW"; the carrier a Good Order MMC; one man to a carrier and one
    /// carrier to a man, counting the carries of the move's earlier steps (<paramref name="earlier"/>). Null when every carry stands.
    /// </summary>
    public static string? CarryBar(IReadOnlyList<CarryFacts> carries, IReadOnlyList<(string Man, string Carrier)> earlier)
    {
        ArgumentNullException.ThrowIfNull(carries);
        ArgumentNullException.ThrowIfNull(earlier);
        (string Man, string Carrier)[] all = [.. earlier, .. carries.Select(carry => (carry.Man, carry.Carrier))];
        foreach (var carry in carries)
        {
            var bar = carry.Man == carry.Carrier || !carry.ManMoving || !carry.CarrierMoving
                    ? $"{carry.Man} and {carry.Carrier} must be two units of the moving stack for one to carry the other (A17.2)"
                : !carry.ManWounded ? $"{carry.Man} is not wounded; only a wounded man is carried (A17.2)"
                : !carry.ManGoodOrder ? $"{carry.Man} is not in Good Order; only a Good Order wounded man is carried (A17.2)"
                : carry.ManHoldsSw ? $"{carry.Man} portages a SW, so he is not carried; he drops it first, at no MF cost (A17.2, A4.43)"
                : !carry.CarrierMmc ? $"{carry.Carrier} is no MMC; only a MMC carries a wounded man (A17.2)"
                : !carry.CarrierGoodOrder ? $"{carry.Carrier} is not in Good Order; only a Good Order MMC carries a wounded man (A17.2)"
                : all.Any(other => (other.Carrier == carry.Carrier) != (other.Man == carry.Man))
                    ? $"{carry.Carrier} carries one wounded man and {carry.Man} is carried by one MMC in a move (A17.2)"
                : null;
            if (bar is not null)
            {
                return $"play.move-carry: {bar}";
            }
        }

        return null;
    }

    /// <summary>
    /// A17.2 (ruling R71.16): a man carried with the move's earlier steps moves on only with his carrier; without it (it broke, was pinned, or moves on
    /// alone) he has been set down, and moves no farther in this MPh. Null when the step's movers keep that rule.
    /// </summary>
    public static string? CarriedMoveBar(IReadOnlyList<(string Man, string Carrier)> earlier, IReadOnlyCollection<string> movers)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        ArgumentNullException.ThrowIfNull(movers);
        return earlier.FirstOrDefault(carry => movers.Contains(carry.Man) && !movers.Contains(carry.Carrier)) is { Man: { } man } set
            ? $"play.move-carry: {man} was carried by {set.Carrier} in this move and was set down without it, so he moves no farther this MPh; end his move (A17.2; ruling R71.16)"
            : null;
    }

    /// <summary>What a move's proposal says of a carry (A17.2): the man, his carrier, and the five PP.</summary>
    public static string CarriedText(string man, string carrier) => $"; {carrier} carries the wounded {man} at five PP, and he spends no MF (A17.2)";

    /// <summary>
    /// What a record says of a Wound Severity dr (A17.11): the man when the record knows him, the dr with the +1 of a man already wounded, the
    /// Final dr, and what it comes to.
    /// </summary>
    public static string SeverityText(int severityDr, string? unitId = null, bool alreadyWounded = false) =>
        $"Wound Severity dr {severityDr}{(alreadyWounded ? $" + 1 (already wounded) = {severityDr + 1}" : string.Empty)}{(unitId is null ? string.Empty : $" for {unitId}")}: "
            + $"{(Mortal(severityDr, alreadyWounded) ? "a mortal wound, eliminated" : "a wound, not mortal")}; a Final dr of 5 or more is mortal (A17.11)";
}
