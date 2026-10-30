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

    /// <summary>
    /// E1.92, E1.921: who may fire a Starshell: a Good Order, unpinned, not TI leader or MMC, or a vehicle not BU, Stunned, or Shocked. Forwards to
    /// <see cref="FirerRefusal"/> with the firer from <see cref="FirerOf"/> (no Armor Leader), and neither Aerial, in a pillbox, nor in an Interior Building
    /// Hex or subterranean Location: the same verdict and text on every input (the E1.8 claim, RE1.13).
    /// </summary>
    public static string? FirerBar(string unitId, bool leader, bool afv, bool mmc, bool buttonedUp, bool stunned, bool shocked, bool goodOrder, bool pinned, bool ti, bool captured) =>
        FirerRefusal(unitId, FirerOf(leader, afv, mmc, buttonedUp, false), stunned, shocked, goodOrder, pinned, ti, captured, false, false, false);

    /// <summary>
    /// E1.92, E1.921 (p. 226; RE1.6): who tries a Starshell. An AFV is read first, as <see cref="FirerBar"/> refuses a BU AFV whatever else it is: CE with an
    /// Armor Leader, CE with none, or BU (Other); then a leader, a MMC, or any other unit.
    /// </summary>
    public static StarshellFirer FirerOf(bool leader, bool afv, bool mmc, bool buttonedUp, bool armorLeader) =>
        afv ? (buttonedUp ? StarshellFirer.Other : armorLeader ? StarshellFirer.CeAfvWithArmorLeader : StarshellFirer.CeAfv)
        : leader ? StarshellFirer.Leader
        : mmc ? StarshellFirer.Mmc
        : StarshellFirer.Other;

    /// <summary>
    /// E1.921 (RE1.6): whether the firer fires as a leader, for the Usage dr of 4 and the exception to the timing restriction: a leader, and a CE AFV with an
    /// Armor Leader ("a leader (including a CE Armor Leader)").
    /// </summary>
    public static bool FiresAsLeader(StarshellFirer firer) => firer is StarshellFirer.Leader or StarshellFirer.CeAfvWithArmorLeader;

    /// <summary>
    /// E1.92, E1.921 (p. 226; RE1.8): why this unit may not fire a Starshell, or null. <see cref="FirerBar"/>'s check first, with its text: a leader, a CE AFV,
    /// or a MMC; an AFV not Stunned or Shocked; any other Good Order; none pinned, TI, or captured. Then not Aerial nor in a pillbox; then not in an Interior
    /// Building Hex or a subterranean Location (E1.921), where an unknown Location refuses.
    /// </summary>
    public static string? FirerRefusal(string unitId, StarshellFirer firer, bool stunned, bool shocked, bool goodOrder, bool pinned, bool ti, bool captured, bool aerial,
        bool inPillbox, bool? interiorOrSubterranean)
    {
        var afv = firer is StarshellFirer.CeAfv or StarshellFirer.CeAfvWithArmorLeader;
        if (firer == StarshellFirer.Other || (afv && (stunned || shocked)) || (!afv && !goodOrder) || pinned || ti || captured)
        {
            return $"play.starshell-firer: {unitId} is not a Good Order, unpinned, not TI leader, CE AFV, or MMC (E1.92, E1.921)";
        }

        if (aerial || inPillbox)
        {
            return $"play.starshell-firer: {unitId} is Aerial or in a pillbox, and fires no Starshell (E1.921)";
        }

        return interiorOrSubterranean != false
            ? $"play.starshell-firer: {unitId} is not known to be outside an Interior Building Hex or a subterranean Location, from which no Starshell is fired (E1.921)"
            : null;
    }

    /// <summary>
    /// E1.91 (p. 226; RE1.5, RE1.9): no Starshell is fired in a scenario until one of three events has occurred, or null once one has (or a Starshell has
    /// been fired): (1) while the friendly force has no motorized vehicle on board, an enemy motorized vehicle changes its hex or VCA within 16 hexes of the
    /// firer (an unknown motion does not open it); (2) the firer has a LOS to an enemy unit (gathered as today: seen within NVR or Illuminated, E1.33); (3) a
    /// Gunflash was placed by an attack vs an enemy unit (<see cref="ScenarioA1GunflashCalculator.OpensFirstStarshell(GunflashMark)"/>), at any time before.
    /// </summary>
    public static string? FirstStarshellRefusal(bool starshellFiredBefore, bool firerHasEnemyInLos, bool friendlyMotorizedOnboard, bool? enemyMotorizedMovedWithin16,
        bool qualifyingGunflashOccurred) =>
        starshellFiredBefore || firerHasEnemyInLos || (!friendlyMotorizedOnboard && enemyMotorizedMovedWithin16 == true) || qualifyingGunflashOccurred
            ? null
            : "play.starshell-first: no Starshell is fired until the firer sees an enemy unit, an enemy motorized vehicle moves within 16 hexes of it while no friendly one is on board, or a Gunflash is placed by an attack vs an enemy unit (E1.91)";

    /// <summary>
    /// E1.921, E1.923 (pp. 226 and 227; RE1.8): whether a Starshell Illuminates a Location: within three hexes, and not an Interior Building Hex or a
    /// subterranean Location; an unknown distance or Location is not lit.
    /// </summary>
    public static bool StarshellIlluminates(int? distance, bool? interiorOrSubterranean) => distance <= 3 && interiorOrSubterranean == false;

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
    /// unit is seen within the firer's NVR or Illuminated. The two scans are read in that order, only until the first Starshell has been fired. Replaced by
    /// <see cref="FirstStarshellRefusal"/> (the E1.8 claim, RE1.9, RE1.13), which keeps this answer: that one also opens on an enemy motorized vehicle's
    /// motion within 16 hexes (E1.91 event 1), and takes a Gunflash only from an attack vs an enemy unit, kept once it has occurred, where this one takes any
    /// Gunflash on the map now; its text names the three events. Equal in verdict when the Gunflash is a qualifying one and no vehicle moved, not in text.
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
    /// movement. <paramref name="starshellTurn"/> and <paramref name="thisTurn"/> are the state's Player Turn keys. <paramref name="leader"/> is a leader, a CE
    /// Armor Leader included (E1.921 "[EXC: a leader]"; the E1.8 claim, RE1.6): <see cref="FiresAsLeader"/>.
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

    /// <summary>
    /// E1.92: the Usage dr needed: 4 or less for a leader, 2 or less otherwise. Forwards to <see cref="UsageNeed(StarshellFirer)"/> of a leader or a MMC: the
    /// same answer on both inputs, which cannot name a CE Armor Leader (the E1.8 claim, RE1.13).
    /// </summary>
    public static int UsageNeed(bool leader) => UsageNeed(leader ? StarshellFirer.Leader : StarshellFirer.Mmc);

    /// <summary>
    /// E1.921 (p. 226; RE1.6): the Usage dr needed: 4 or less for a leader, a CE Armor Leader included; 2 or less for a CE AFV with no Armor Leader or a MMC.
    /// </summary>
    public static int UsageNeed(StarshellFirer firer) => FiresAsLeader(firer) ? 4 : 2;

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
