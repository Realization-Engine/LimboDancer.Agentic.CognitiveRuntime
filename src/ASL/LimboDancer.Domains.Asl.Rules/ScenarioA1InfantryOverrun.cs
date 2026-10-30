namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One unit of a stack that would make an Infantry OVR (A4.15): its kind, and the states that bar it.</summary>
public sealed record InfantryOverrunMoverFacts(string Id, bool Mmc, bool Leader, bool GoodOrder, bool Berserk, bool Pinned, bool MovementEnded);

/// <summary>One enemy unit in the Location an Infantry OVR would enter, as the moving side knows it (A4.14, A4.15, A20.54).</summary>
public sealed record InfantryOverrunDefenderFacts(string Id, bool Known, bool Smc, bool Melee, bool Unarmed, bool Vehicle);

/// <summary>
/// Infantry OVR (A4.15 to A4.152, p. 49; pass 40, task 40.4): an Infantry MMC enters, in its MPh, a Location that holds only one Known enemy SMC, at
/// double the total MF cost of entry, once it has passed a NTC; the SMC fires or moves away; if it stays, CC follows at once. The berserk OVR of
/// ruling R27.3 is the case with no NTC and no doubled MF (A15.432). Unarmed enemy units are entered and recaptured the same way without a NTC (A20.54).
/// </summary>
public static class ScenarioA1InfantryOverrun
{
    /// <summary>The roll's purpose in the records.</summary>
    public const string NtcPurpose = "infantry-ovr-ntc";

    /// <summary>
    /// A4.15: who takes the NTC. "A leader may exempt all MMC he is stacked with and moves with from that NTC by passing it himself"; without a
    /// leader each MMC takes its own, so a stack of several MMC and no leader is asked to name one at a time. A berserk unit charges with no NTC.
    /// </summary>
    public static (string? Taker, string? Refusal) NtcTaker(IReadOnlyList<InfantryOverrunMoverFacts> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        if (movers.Count == 0 || movers.Any(unit => !unit.GoodOrder || unit.Pinned || unit.MovementEnded))
        {
            return (null, "play.overrun-ntc: an Infantry OVR is made by Good Order, unpinned Infantry that may still move (A4.15)");
        }

        if (movers.Any(unit => unit.Berserk))
        {
            return (null, "play.overrun-ntc: a berserk unit charges without a NTC (A15.432)");
        }

        var mmc = movers.Where(unit => unit.Mmc).ToArray();
        if (mmc.Length == 0)
        {
            return (null, "play.overrun-ntc: an Infantry OVR is made by a MMC; a SMC alone makes none (A4.15)");
        }

        if (movers.FirstOrDefault(unit => unit.Leader) is { } leader)
        {
            return (leader.Id, null);
        }

        return mmc.Length == 1 && movers.Count == 1
            ? (mmc[0].Id, null)
            : (null, "play.overrun-ntc: without a leader each MMC takes its own NTC; name one MMC at a time (A4.15)");
    }

    /// <summary>
    /// A4.15: the Location holds "only one Known enemy SMC (unless that SMC occupies an AFV)". Concealed units there are not counted here: A12.15
    /// applies to them when the MMC enters. A second OVR against a SMC already held in Melee (A4.152) is not built. Null when the OVR may be made.
    /// </summary>
    public static string? TargetBar(IReadOnlyList<InfantryOverrunDefenderFacts> enemies, string toText)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        var known = enemies.Where(unit => unit.Known).ToArray();
        return known is [{ Smc: true, Melee: false, Vehicle: false }]
            ? null
            : $"play.overrun-target: an Infantry OVR enters a Location whose only Known enemy unit is one SMC, not in an AFV and not held in Melee; {toText} is not one (A4.15, A4.152)";
    }

    /// <summary>A20.54: "Unarmed units are not an obstacle to movement": every Known enemy unit there is Unarmed, and there is at least one.</summary>
    public static bool OnlyUnarmed(IReadOnlyList<InfantryOverrunDefenderFacts> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        var known = enemies.Where(unit => unit.Known).ToArray();
        return known.Length > 0 && known.All(unit => unit.Unarmed && !unit.Vehicle);
    }

    /// <summary>A4.15: one NTC for a unit against a Location in a MPh; "other MMC could attempt subsequent Infantry OVR attacks but each would require a separate NTC".</summary>
    public static string? AlreadyTakenBar(bool taken, string toText) =>
        taken ? $"play.overrun-ntc: this stack has taken its NTC for an Infantry OVR of {toText} this MPh (A4.15)" : null;

    /// <summary>
    /// A4.15: the NTC's DRM is the TEM of the enemy's Location: the hex's, or a hexside's when the stack would cross it and it is the larger. The
    /// two are not added: a wall's or hedge's TEM is "not cumulative with positive TEM of other terrain in that hex" (B9.31, p. 119).
    /// </summary>
    public static int NtcDrm(int tem, int hexsideTem) => Math.Max(Math.Max(tem, 0), Math.Max(hexsideTem, 0));

    /// <summary>
    /// A4.15: whether the stack's MMC are covered by passed NTCs for this Location: each MMC is among the units of a passed NTC whose taker enters
    /// with it. A leader's NTC exempts the MMC "he is stacked with and moves with", so it covers them only while he moves with them; MMC that
    /// each passed their own are covered together.
    /// </summary>
    public static bool NtcCovers(IReadOnlyList<string> mmcIds, IReadOnlyList<string> moverIds, IReadOnlyList<(IReadOnlyList<string> Units, string? Taker, bool Passed)> taken)
    {
        ArgumentNullException.ThrowIfNull(mmcIds);
        ArgumentNullException.ThrowIfNull(moverIds);
        ArgumentNullException.ThrowIfNull(taken);
        return mmcIds.Count > 0 && mmcIds.All(id => taken.Any(ntc => ntc.Passed && ntc.Units.Contains(id, StringComparer.Ordinal)
            && (ntc.Taker is null || moverIds.Contains(ntc.Taker, StringComparer.Ordinal))));
    }

    /// <summary>
    /// A20.54 (p. 88): who may enter Unarmed enemy units to recapture them. A "non-prisoner, unarmed Infantry unit may not enter a Known
    /// enemy-occupied Location", and a stack of Dummies enters no enemy unit's Location (A12.11).
    /// </summary>
    public static bool MayRecapture(bool anyMoverUnarmed, bool allMoversDummies) => !anyMoverUnarmed && !allMoversDummies;

    /// <summary>A10.1: the check passes at or below the Morale Level.</summary>
    public static bool NtcPassed(int die1, int die2, int drm, int morale) => die1 + die2 + drm <= morale;

    /// <summary>
    /// A4.15: when a leader fails the NTC, "no unit in his stack may attempt a NTC individually nor may any of those units move or take any other
    /// action during that phase". A MMC that fails its own makes no OVR there, and may otherwise go on.
    /// </summary>
    public static bool EndsStackMove(bool takerIsLeader, bool passed) => takerIsLeader && !passed;

    /// <summary>The NTC's summary, before the dice.</summary>
    public static string NtcText(string takerId, bool leader, string idsText, string toText, int morale, int drm) =>
        $"play.overrun-ntc: {takerId} takes the NTC for an Infantry OVR of {toText}{(leader ? $" for {idsText}" : string.Empty)}, against Morale Level {morale} with DRM +{drm} "
        + $"(the TEM there): passed, the stack may enter at double the MF{(leader ? "; failed, the stack's MPh ends" : "; failed, it makes no OVR there")} (A4.15)";

    /// <summary>
    /// A4.15: a step declared as an Infantry OVR needs a passed NTC that covers every MMC of the stack, and is an ordinary step: no Minimum Move,
    /// no Bypass, no entry from off board. Null when it stands.
    /// </summary>
    public static string? EntryBar(bool ntcPassed, bool ntcFailed, bool minimumMove, bool bypassing, bool entering, bool berserk, string toText)
    {
        if (berserk)
        {
            return "play.move-overrun: a berserk stack charges; it declares no Infantry OVR (A15.432)";
        }

        if (minimumMove || bypassing || entering)
        {
            return "play.move-overrun: an Infantry OVR is an ordinary entry of the Location, not a Minimum Move, a Bypass, or an entry from off board (A4.15)";
        }

        return ntcPassed ? null
            : ntcFailed ? $"play.move-overrun: the NTC for an Infantry OVR of {toText} was failed, so the stack does not enter (A4.15)"
            : $"play.move-overrun: the stack first passes its NTC for an Infantry OVR of {toText} (A4.15)";
    }

    /// <summary>A4.15: the entry is "at double the total MF cost of entry".</summary>
    public static int EntryHalfMf(int halfMf) => halfMf * 2;

    /// <summary>The summary's clause for the entry.</summary>
    public static string EntryText(bool recapture) => recapture
        ? ", entering the Unarmed units' Location: CC follows at once to recapture them (A20.54)"
        : ", an Infantry OVR at double the MF of entry: the SMC fires or moves away, and if it stays CC follows at once (A4.15, A4.151, A4.152)";

    /// <summary>
    /// A4.152, A20.54, A15.432: whether the CC of an entry is still owed in the MPh: the only enemy units there are one Known SMC not held in
    /// Melee, entered by a MMC of the stack, or Unarmed units alone.
    /// </summary>
    public static bool CloseCombatOwed(bool mmcOfTheStackThere, IReadOnlyList<InfantryOverrunDefenderFacts> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        return mmcOfTheStackThere && enemies.Count > 0 && enemies.All(unit => unit.Known && !unit.Vehicle)
            && (enemies is [{ Smc: true, Melee: false }] || enemies.All(unit => unit.Unarmed));
    }

    /// <summary>
    /// A4.151: the SMC's second option. It "may immediately, prior to any Defensive First Fire vs the ATTACKER, enter ... any adjacent Accessible
    /// Location of the ATTACKER's choice which is not occupied by an enemy unit". A SMC that is "broken, pinned, already held in Melee, TI,
    /// occupying a vehicle, or otherwise incapable of movement" has no such option. Null when it may.
    /// </summary>
    public static string? EvadeBar(string unitId, bool overrunOfItsLocation, bool windowOpen, bool firedAtTheEntry, bool smc, bool goodOrder, bool pinned, bool melee, bool ti,
        int destinations)
    {
        if (!overrunOfItsLocation || !windowOpen || !smc)
        {
            return $"play.overrun-evade: {unitId} is not a SMC whose Location an Infantry OVR has just entered (A4.151)";
        }

        if (firedAtTheEntry)
        {
            return $"play.overrun-evade: the SMC moves away before any Defensive First Fire at the entering units, not after it (A4.151)";
        }

        if (!goodOrder || pinned || melee || ti)
        {
            return $"play.overrun-evade: {unitId} is broken, pinned, held in Melee, or TI, and has no move away (A4.151)";
        }

        return destinations == 0 ? $"play.overrun-evade: no adjacent Accessible Location free of enemy units is open to {unitId} (A4.151)" : null;
    }

    /// <summary>
    /// A4.151: the Locations the ATTACKER may send the SMC to. "The ATTACKER may not force the SMC into a minefield, FFE, Wire, or Open Ground hex if
    /// an alternate choice is available": minefields, FFE, and Wire have no counters here, so Open Ground is left out when anything else is open.
    /// </summary>
    public static (IReadOnlyList<string> Destinations, string? Refusal) EvadeDestinationsBySeason(
        IReadOnlyList<(string Location, bool Enterable, bool Playable, bool EnemyOccupied, bool? OpenGround)> adjacent)
    {
        ArgumentNullException.ThrowIfNull(adjacent);
        var coveredReading = EvadeDestinations([.. adjacent.Select(item => (item.Location, item.Enterable, item.Playable, item.EnemyOccupied, item.OpenGround ?? false))]);
        var openReading = EvadeDestinations([.. adjacent.Select(item => (item.Location, item.Enterable, item.Playable, item.EnemyOccupied, item.OpenGround ?? true))]);
        return coveredReading.SequenceEqual(openReading, StringComparer.Ordinal) ? (coveredReading, null)
            : ([], "play.overrun-evade-season: the scenario month is needed to decide the Open Ground destinations (A4.151, B15.6)");
    }

    public static IReadOnlyList<string> EvadeDestinations(IReadOnlyList<(string Location, bool Enterable, bool Playable, bool EnemyOccupied, bool OpenGround)> adjacent)
    {
        ArgumentNullException.ThrowIfNull(adjacent);
        var accessible = adjacent.Where(item => item.Enterable && item.Playable && !item.EnemyOccupied).ToArray();
        var covered = accessible.Where(item => !item.OpenGround).Select(item => item.Location).ToArray();
        return covered.Length > 0 ? covered : [.. accessible.Select(item => item.Location)];
    }

    /// <summary>What the ATTACKER is asked, for the page and the plan's reasons.</summary>
    public static string EvadeChoiceText(string unitId) =>
        $"{unitId} moves away from the Infantry OVR: the ATTACKER names the adjacent Location it enters, free of all fire (A4.151)";

    /// <summary>The DEFENDER's declaration, in words.</summary>
    public static string EvadeText(string unitId, IReadOnlyList<string> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return $"play.overrun-evade: {unitId} moves away from the Infantry OVR before any fire; the ATTACKER names the adjacent Location it enters (A4.151)";
    }

    /// <summary>The move away, in words.</summary>
    public static string EvadedText(string unitId, string toText) => $"play.overrun-evade: {unitId} enters {toText}, free of all enemy fire (A4.151)";

    /// <summary>A4.152, A15.432: the CC of an Infantry OVR comes first once the DEFENDER's window on the entry closes.</summary>
    public static string CloseCombatFirstText(string at) =>
        $"play.cc-overrun-first: the Infantry OVR in {at} has its CC at once, before anything else happens (A4.152, A15.432, A20.54)";
}
