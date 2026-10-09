namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Starshells (E1.91 to E1.923; backlog pass 16, ruling R16.8; pass 32.h, S9): who fires one, when, from where, the aim of the three methods, the Usage dr,
/// and where it lands. Play reads the state and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1Starshells
{
    /// <summary>E1.9: Starshells are fired only at night.</summary>
    public static string? DayBar(bool night) => night ? null : "play.starshell-day: Starshells are fired only at night (E1.9)";

    /// <summary>The firer named is not an active unit on the map.</summary>
    public static string FirerOffMapText(string unitId) => $"play.starshell-firer: '{unitId}' is not a unit on the map";

    /// <summary>E1.92, E1.921: who may fire a Starshell: a Good Order, unpinned, not TI leader or MMC, or a vehicle not BU, Stunned, or Shocked.</summary>
    public static string? FirerBar(string unitId, bool leader, bool afv, bool mmc, bool buttonedUp, bool stunned, bool shocked, bool goodOrder, bool pinned, bool ti, bool captured) =>
        (!leader && !afv && !mmc) || (afv && (buttonedUp || stunned || shocked)) || (!afv && !goodOrder) || pinned || ti || captured
            ? $"play.starshell-firer: {unitId} is not a Good Order, unpinned, not TI leader, CE AFV, or MMC (E1.92, E1.921)"
            : null;

    /// <summary>E1.92: the phases a Starshell is fired in: the PFPh by the phasing side, the DFPh or the MPh (as Defensive First Fire) by the other.</summary>
    public static string? PhaseBar(string? phase, bool phasing) =>
        !(phase == "pfph" && phasing) && !(phase is "dfph" or "mph" && !phasing)
            ? "play.starshell-phase: a Starshell is fired in the PFPh by the phasing side, or in the DFPh or as Defensive First Fire by the other (E1.92)"
            : null;

    /// <summary>E1.92: one attempt per hex per phase.</summary>
    public static string? OnceBar(bool attemptedFromHex, string hex) =>
        attemptedFromHex ? $"play.starshell-once: a Starshell attempt was made from {hex} this phase; one per hex (E1.92)" : null;

    /// <summary>
    /// E1.91: the first Starshell of the game needs an enemy unit in the firer's LOS, or a Gunflash on the map; E1.33 (referee, pass 16): at night an enemy
    /// unit is seen within the firer's NVR or Illuminated. The two scans are read in that order, only until the first Starshell has been fired.
    /// </summary>
    public static string? FirstBar(bool starshellUsed, Func<bool> anyEnemySeen, Func<bool> anyGunflash)
    {
        ArgumentNullException.ThrowIfNull(anyEnemySeen);
        ArgumentNullException.ThrowIfNull(anyGunflash);
        return !starshellUsed && !anyEnemySeen() && !anyGunflash()
            ? "play.starshell-first: no Starshell is fired until the firer sees an enemy unit or a Gunflash is placed (E1.91)"
            : null;
    }

    /// <summary>
    /// E1.921: after the Player Turn of the first Starshell, a firer other than a leader fires it at the start of the PFPh or the enemy MPh, before any fire or
    /// movement. <paramref name="starshellTurn"/> and <paramref name="thisTurn"/> are the state's Player Turn keys.
    /// </summary>
    public static string? TimingBar(bool starshellUsed, string? starshellTurn, string thisTurn, bool leader, string? phase, bool acted) =>
        starshellUsed && starshellTurn != thisTurn && !leader && (phase == "dfph" || acted)
            ? "play.starshell-timing: after the first Starshell, only a leader fires one after the start of the PFPh or the enemy MPh (E1.921)"
            : null;

    /// <summary>E1.922 method 3: the aimed hex is exactly three hexes away.</summary>
    public static string? ThreeHexesBar(string method, string aimedHex, int range) =>
        method == "three-hexes" && range != 3 ? $"play.starshell-placement: {aimedHex} is {range} hexes away, not exactly three (E1.922)" : null;

    /// <summary>
    /// E1.922 method 2: a Gunflash or Known enemy unit in the firer's LOS, less than nine hexes away, at most six (placement along the LOS is not built). The LOS
    /// and the occupants are read in that order, only within six hexes.
    /// </summary>
    public static string? AtTargetBar(string method, int range, Func<bool> losClear, Func<bool> knownEnemySeenOrGunflash, string aimedHex, string unitId)
    {
        ArgumentNullException.ThrowIfNull(losClear);
        ArgumentNullException.ThrowIfNull(knownEnemySeenOrGunflash);
        return method == "at-target" && (range > 6 || !losClear() || !knownEnemySeenOrGunflash())
            ? $"play.starshell-placement: {aimedHex} is not a Gunflash or Known enemy unit in {unitId}'s LOS within six hexes (E1.922)"
            : null;
    }

    /// <summary>E1.92: the Usage dr needed: 4 or less for a leader, 2 or less otherwise.</summary>
    public static int UsageNeed(bool leader) => leader ? 4 : 2;

    /// <summary>Whether the Usage dr fires the Starshell.</summary>
    public static bool UsagePassed(int dr, int need) => dr <= need;

    /// <summary>B.8: how many dice the placement takes: one for the firer's own hex, two for an aimed hex.</summary>
    public static int PlacementDice(string method) => method == "own-hex" ? 1 : 2;

    /// <summary>B.8: the hexes the Starshell drifts along the Random Direction: one from the firer's hex, the white dr for method 3, halved (FRU) for method 2.</summary>
    public static int PlacementExtent(string method, IReadOnlyList<int> dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        return method == "own-hex" ? 1 : method == "at-target" ? (dice[1] + 1) / 2 : dice[1];
    }

    /// <summary>E1.921: a hidden firer is placed beneath a "?".</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> HiddenFirerConditions() => [(UnitCondition.Hidden, false), (UnitCondition.Concealed, true)];

    /// <summary>
    /// The projector's check of a recorded Starshell attempt (E1.92 to E1.923; ruling R16.8): by an active unit at night, once per hex per phase, its rolls recorded; a
    /// Starshell is placed only where one that passed its Usage dr landed.
    /// </summary>
    public static RecordRefusal? VerifyStarshell(bool night, bool unitActive, bool usageRollRecorded, bool placementRollRecorded, bool landed, bool starshellNamed, bool passed,
        bool attemptedFromHex) =>
        !night || !unitActive || !usageRollRecorded || !placementRollRecorded || landed != starshellNamed || (!passed && landed) || attemptedFromHex
            ? new RecordRefusal("UNIT-STATE-044", "A Starshell is fired at night by a unit in play, once per hex per phase, with its rolls recorded (E1.92).")
            : null;

    /// <summary>E1.921: the Player Turn of the game's first Starshell, kept once a Starshell has passed its Usage dr.</summary>
    public static string? StarshellTurnAfter(string? starshellTurn, bool passed, string thisTurn) => starshellTurn ?? (passed ? thisTurn : null);

    /// <summary>The attempt in words (E1.92, E1.923).</summary>
    public static string Summary(string unitId, string method, int need) =>
        $"play.starshell: {unitId} tries to fire a Starshell ({method}): a Usage dr of {need} or less fires it, and it Illuminates three hexes around where it lands until the end of the CCPh (E1.92, E1.923)";
}
