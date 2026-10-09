namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a Sniper dr of 1 or 2 does to the unit it picks (A14.3, A17.11; ruling R15.5).</summary>
public enum SniperEffect
{
    /// <summary>A SMC on dr 1, or a HS that cannot be Reduced: eliminated.</summary>
    Eliminated,

    /// <summary>A SMC on dr 2: a Wound Severity dr decides between wounded and eliminated.</summary>
    WoundSeverity,

    /// <summary>A MMC on dr 1 that can break: broken, under DM, no longer pinned.</summary>
    Broken,

    /// <summary>A broken or berserk MMC on dr 1: Casualty Reduced, a squad to its HS, a HS eliminated.</summary>
    CasualtyReduction,

    /// <summary>A MMC on dr 2 not broken, berserk, or pinned: pinned.</summary>
    Pinned,

    /// <summary>No effect.</summary>
    None,
}

/// <summary>A hex holding eligible Sniper targets as the target choice reads it (A14.21): its index in the caller's list, its distance from the Random Location (null when unread), its lowest TEM, and its name.</summary>
public sealed record SniperTargetHex(int Index, int? Distance, int Tem, string Hex);

/// <summary>A Location of the chosen hex as the Location choice reads it (ruling R15.5): its index, how many eligible targets it holds, and its level.</summary>
public sealed record SniperTargetLocation(int Index, int Count, int Level);

/// <summary>
/// Snipers (A14; backlog pass 15, ruling R15.5; pass 32.h, S9): which DRs call for a Sniper attack, the Sniper dr, the Random Location and the target it finds,
/// Random Selection among the targets, and the results. Play reads the records, the state, and the map, draws the dice, and writes the events.
/// </summary>
public static class ScenarioA1Sniper
{
    public const string NoEffect = "none";
    public const string NoTarget = "no-target";
    public const string DummyStackEliminated = "dummy-stack-eliminated";
    public const string Eliminated = "eliminated";
    public const string Wounded = "wounded";
    public const string Broken = "broken";
    public const string CasualtyReduced = "casualty-reduced";
    public const string Pinned = "pinned";

    /// <summary>A14.1: Sniper attacks follow the DRs of the PFPh, MPh, DFPh, and AFPh.</summary>
    public static bool TriggerPhase(string? phase) => phase is "pfph" or "mph" or "dfph" or "afph";

    /// <summary>A fire record's roll key, split into its kind and its unit; pass 31 (ruling R31.5): a unit's second Leader Loss check, keyed "unit#2", is the same unit's DR.</summary>
    public static (string Kind, string UnitId) RollKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unitId) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        if (unitId.IndexOf('#', StringComparison.Ordinal) is var numbered and > 0)
        {
            unitId = unitId[..numbered];
        }

        return (kind, unitId);
    }

    /// <summary>
    /// A14.1: whose DR a roll is: an IFT DR is its firing side's; a MC or TC DR (a check, a leader loss check, a berserk NTC, a crew's) its unit's, never a
    /// prisoner's; any other roll is no side's. <paramref name="checker"/> reads the unit's side and whether it is captured, only for a check.
    /// </summary>
    public static string? RollMaker(string kind, string? firingSide, Func<(string? Side, bool Captured)?> checker)
    {
        ArgumentNullException.ThrowIfNull(checker);
        return kind switch
        {
            "attack" => firingSide,
            "checks" or "leaderLoss" or "berserkCheck" or "crewCheck" when checker() is { Captured: false } found => found.Side,
            _ => null,
        };
    }

    /// <summary>E1.76 (backlog pass 16, ruling R16.7): at night each side's SAN is two higher, to at most 7.</summary>
    public static int SniperActivationNumber(int printed, bool night) => night ? Math.Min(printed + 2, 7) : printed;

    /// <summary>A14.1: a two-dice DR equal to the enemy's SAN calls for the Sniper attack.</summary>
    public static bool Triggers(int sum, int printedSan, bool night) => sum == SniperActivationNumber(printedSan, night);

    /// <summary>A14.1, A8.2: the side that made a fire record's IFT DR: its firers', a DC's user's, or the DEFENDER's for Residual FP; each read when the one before gives none.</summary>
    public static string? FiringSide(bool hasFirers, Func<string?> firstFirerSide, Func<string?> demolitionChargeUserSide, Func<bool> residualFire, Func<string?> defenderSide)
    {
        ArgumentNullException.ThrowIfNull(firstFirerSide);
        ArgumentNullException.ThrowIfNull(demolitionChargeUserSide);
        ArgumentNullException.ThrowIfNull(residualFire);
        ArgumentNullException.ThrowIfNull(defenderSide);
        if (hasFirers)
        {
            return firstFirerSide();
        }

        if (demolitionChargeUserSide() is { } userSide)
        {
            return userSide;
        }

        return residualFire() ? defenderSide() : null;
    }

    /// <summary>A14.31: a pinned Sniper makes no further attack this Player Turn.</summary>
    public static bool Attacks(bool pinned) => !pinned;

    /// <summary>A14.3: the Sniper dr attacks only on 1 or 2.</summary>
    public static bool Effective(int dr) => dr <= 2;

    /// <summary>A14.2 (ruling R15.5): the Random Location DR: the colored dr a hexside direction (1 the top hexside, clockwise, as 0 to 5), the white dr the hexes along it.</summary>
    public static (int Direction, int Extent) RandomLocation(int colored, int white) => (colored - 1, white);

    /// <summary>A14.22 (ruling R15.5): the eligible targets are the attacked side's Personnel and Dummies on the map, not hidden, not prisoners.</summary>
    public static bool Eligible(bool active, bool attackedSide, bool vehicle, bool hidden, bool captured, bool onMap) => active && attackedSide && !vehicle && !hidden && !captured && onMap;

    /// <summary>The TEM of a target hex's terrain, for the tie between hexes (A14.21).</summary>
    public static int TargetTem(string? terrain) => terrain is not null ? ScenarioA1FireReference.Tem.GetValueOrDefault(terrain) : 0;

    /// <summary>A14.21: the target hex, or the closest hex holding an eligible target; ties go to the lowest TEM, then the first hex by name; an unread distance is the farthest.</summary>
    public static int TargetHex(IReadOnlyList<SniperTargetHex> hexes)
    {
        ArgumentNullException.ThrowIfNull(hexes);
        return hexes.OrderBy(item => item.Distance ?? int.MaxValue).ThenBy(item => item.Tem).ThenBy(item => item.Hex, StringComparer.Ordinal).First().Index;
    }

    /// <summary>Ruling R15.5: of several Locations in the hex, the one holding the most eligible targets, then the lowest.</summary>
    public static int TargetLocation(IReadOnlyList<SniperTargetLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        return locations.OrderByDescending(item => item.Count).ThenBy(item => item.Level).First().Index;
    }

    /// <summary>A14.23: a concealed stack is one possible target; the others one each. The targets come in id order, each with whether it is concealed or a Dummy.</summary>
    public static IReadOnlyList<IReadOnlyList<int>> Candidates(IReadOnlyList<bool> concealedOrDummy)
    {
        ArgumentNullException.ThrowIfNull(concealedOrDummy);
        var candidates = Enumerable.Range(0, concealedOrDummy.Count).Where(index => !concealedOrDummy[index]).Select(index => (IReadOnlyList<int>)[index]).ToList();
        int[] hidden = [.. Enumerable.Range(0, concealedOrDummy.Count).Where(index => concealedOrDummy[index])];
        if (hidden.Length > 0)
        {
            candidates.Add(hidden);
        }

        return candidates;
    }

    /// <summary>A.9: Random Selection: the candidates whose die shows the highest value, in order; the first takes the Sniper dr, each other a new Sniper dr of its own (A14.2).</summary>
    public static IReadOnlyList<int> Tied(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var highest = values.Max();
        return [.. Enumerable.Range(0, values.Count).Where(index => values[index] == highest)];
    }

    /// <summary>A.9: among several real units of a concealed stack, the first whose die shows the highest value.</summary>
    public static int Picked(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.IndexOf([.. values], values.Max());
    }

    /// <summary>
    /// A14.3 (ruling R15.5): dr 1 eliminates a SMC and breaks a MMC, Casualty Reducing one that cannot break (broken or berserk); dr 2 wounds a SMC (its Wound
    /// Severity dr, A17.11) and pins a MMC not immune to Pin results.
    /// </summary>
    public static SniperEffect UnitEffect(bool smc, int dr, bool broken, bool berserk, bool pinned) =>
        smc && dr == 1 ? SniperEffect.Eliminated
        : smc ? SniperEffect.WoundSeverity
        : dr == 1 && !broken && !berserk ? SniperEffect.Broken
        : dr == 1 ? SniperEffect.CasualtyReduction
        : !broken && !berserk && !pinned ? SniperEffect.Pinned
        : SniperEffect.None;

    /// <summary>A17.11: a Wound Severity dr of 5 or more (+1 for a unit already wounded) is mortal.</summary>
    public static bool Mortal(int severityDr, bool alreadyWounded) => severityDr + (alreadyWounded ? 1 : 0) >= 5;

    /// <summary>A7.302: a squad with a HS definition is Reduced to it; any other unit is eliminated.</summary>
    public static string ReductionResult(bool squadWithHalfSquad) => squadWithHalfSquad ? CasualtyReduced : Eliminated;

    /// <summary>A unit the attack wounds loses its "?" and hidden status (A12.14).</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> WoundedConditions() => [(UnitCondition.Wounded, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>A unit the attack breaks goes under DM, is no longer pinned, and loses its "?" and hidden status.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> BrokenConditions() =>
        [(UnitCondition.Broken, true), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>A unit the attack pins loses its "?" and hidden status.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> PinnedConditions() => [(UnitCondition.Pinned, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>The projector's check of a recorded Sniper attack (A14; ruling R15.5): its Sniper counter is in play, its rolls recorded, and its dr 1 to 6; the events after it apply it.</summary>
    public static RecordRefusal? VerifySniperAttack(bool sniperActive, bool rollRecorded, bool triggerRecorded, int dr) =>
        !sniperActive || !rollRecorded || !triggerRecorded || dr is < 1 or > 6
            ? new RecordRefusal("UNIT-STATE-042", "A Sniper attack names a Sniper counter in play and recorded rolls (A14.1).")
            : null;

    /// <summary>A14.3: an effective Sniper attack puts every broken unit of the attacked side in the Location under DM.</summary>
    public static bool ComesUnderDm(bool active, bool attackedSide, bool broken, bool desperationMorale) => active && attackedSide && broken && !desperationMorale;
}
