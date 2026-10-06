namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Hex-by-hex Infantry movement in the MPh (A4; pass 32.b, slice S3, from Play's planner): the MF a unit may spend, portage and the leader's bonus
/// and IPC loan, and the checks of a move. The caller reads the state, the map, and the catalog and hands the facts over; a catalog read it makes only
/// when a function here asks, as the planner read it.
/// </summary>
public static class ScenarioA1MovementCalculator
{
    /// <summary>
    /// The MF a unit may spend this phase (A4.11, A4.42, A4.5, A4.52; rulings R5.1 and R5.4): its allotment (a MMC's four or three, a SMC's
    /// six or three, a berserk unit's eight), plus the MF Double Time adds, at most eight (seven for Conscripts), less one MF for each PP it
    /// carries beyond its IPC (three for a MMC, one for a SMC, none for a wounded SMC; one less while CX). A berserk unit counts only its 1PP SW,
    /// since it abandons the others before it charges (A15.431). Null when the catalog does not decide it. <paramref name="allowance"/> is the
    /// unit's allotment as the catalog gives it, read for a real unit; <paramref name="portage"/> the PP of each SW it possesses, null when one is
    /// not recorded, read once the allotment is known.
    /// </summary>
    public static int? MfAllotment(MfAllotmentFacts facts, int doubleTimeMf, bool cx, int bonusMf, int ipcBonus, Func<int?> allowance, Func<IReadOnlyList<int>?> portage)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(allowance);
        ArgumentNullException.ThrowIfNull(portage);

        // A12.11 (ruling R10.10): a Dummy stack moves as if it holds a real unit, with four MF.
        if ((facts.Dummy ? 4 : allowance()) is not { } allotment || portage() is not { } carried)
        {
            return null;
        }

        if (doubleTimeMf > 0)
        {
            var conscript = facts.Class == "conscript";
            allotment = Math.Min(allotment + doubleTimeMf, conscript ? 7 : 8);
        }

        var ipc = (facts.Smc ? (facts.Wounded ? 0 : 1) : 3) - (cx ? 1 : 0) + ipcBonus;
        var pp = facts.Berserk ? carried.Where(item => item == 1).Sum() : carried.Sum();

        // B3.4, A4.12 (ruling R10.8): the Road Bonus and a leader's bonus add to the allotment.
        return allotment + bonusMf - Math.Max(0, pp - Math.Max(ipc, 0));
    }

    /// <summary>Whether a unit carries more PP than its IPC (A4.42): MMC three, SMC one, wounded SMC none, one less while CX. <paramref name="carried"/> is the PP of each SW it possesses, null when one is not recorded.</summary>
    public static bool Laden(bool smc, bool wounded, bool cx, IReadOnlyList<int>? carried)
    {
        var ipc = (smc ? (wounded ? 0 : 1) : 3) - (cx ? 1 : 0);
        return carried is not null && carried.Sum() > Math.Max(ipc, 0);
    }

    /// <summary>
    /// Whether a Good Order MMC moving with a Good Order leader of its nationality, who began the MPh with it and has moved with it at every step, has
    /// the leader's two MF bonus (A4.12; ruling R10.8). A berserk unit's MF are never increased but by the Road Bonus (A15.431).
    /// </summary>
    public static bool LeaderBonus(MovingUnitFacts unit, IReadOnlyList<MovingUnitFacts> movers)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(movers);

        // A12.11 (table player, pass 10): a Dummy moves with a leader's bonus as a real MMC would.
        var dummy = unit.Dummy;
        if ((!dummy && !unit.Mmc) || unit.Berserk || unit.Broken
            || (dummy ? movers.FirstOrDefault(item => item.Leader && item.Side == unit.Side) is not { } guide ? null : guide.Nationality : unit.Nationality) is not { } nationality)
        {
            return false;
        }

        var first = unit.MovedWith is null && unit.MfSpent == 0 && !unit.HalfMfSpent;
        return movers.Any(leader => leader.Id != unit.Id && leader.Leader && !leader.Broken && !leader.Berserk
            && leader.Nationality == nationality
            && (first ? leader.MfSpent == 0 && !leader.HalfMfSpent && leader.MovedWith is null : unit.MovedWith?.Contains(leader.Id, StringComparer.Ordinal) == true));
    }

    /// <summary>
    /// The MMC a leader lends his IPC to (A4.42; ruling R10.8): the only MMC of the stack with his leader bonus that carries more than its own IPC; the
    /// leader's own IPC is then spent. Nulls when there is none, or more than one. <paramref name="laden"/> is read by a mover's index for the movers
    /// with the bonus, as the planner read it.
    /// </summary>
    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(laden);
        var heavy = movers.Select((unit, index) => (Unit: unit, Index: index)).Where(item => LeaderBonus(item.Unit, movers) && laden(item.Index)).Select(item => item.Unit).ToArray();
        var leader = heavy.Length != 1 ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded
            && item.Nationality == heavy[0].Nationality);
        return leader is not null ? (heavy[0].Id, leader.Id) : (null, null);
    }
}
