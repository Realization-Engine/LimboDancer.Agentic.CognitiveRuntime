namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One unit of an advancing stack as the leader aid reads it (A4.72 EX, A4.12, A4.42; ruling R10.8).</summary>
public sealed record AdvancingUnitFacts(string Id, bool Mmc, bool Leader, string? Nationality, bool Broken, bool Wounded, bool Laden);

/// <summary>One enemy counter an advance meets, as the advancing side may read it (rulings R23.1, R31d.3): Known by its id, hidden, or a "?" stack.</summary>
public sealed record OpposingCounterFacts(string Id, bool Known, bool Hidden);

/// <summary>
/// The advance in the APh (A3.7, A4.7; pass 32.g, S6): the bars of PlanAdvanceUnits in their order, the MF and its Difficult Terrain, the leader's
/// aid, the enemies met, the surrenders, and the plan's words. Play reads the state and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1AdvanceCalculator
{
    /// <summary>A3.7 (p. 47): units advance in their side's APh.</summary>
    public static string? AdvancePhaseBar(string? phase) => phase != "aph" ? "play.advance-phase: units advance in their side's APh (A3.7, p. 47)" : null;

    /// <summary>A2.5 (ruling R25.1): every advancing unit is an active unit of the phasing side from the catalog.</summary>
    public static bool AdvanceUnitAllowed(bool active, bool phasing, bool fromCatalog) => active && phasing && fromCatalog;

    /// <summary>A2.5: the refusal of a unit that is not one.</summary>
    public static string AdvanceUnitText() => "play.advance-unit: every unit is an active unit of the phasing side from the catalog";

    /// <summary>A2.51 (ruling R20.5): units waiting off board enter apart from units on the map.</summary>
    public static string? EntryStackBar(int offBoard, int count) =>
        offBoard > 0 && offBoard < count ? "play.entry-stack: units waiting off board enter apart from units on the map (A2.51; ruling R20.5)" : null;

    /// <summary>The units advance from one Location.</summary>
    public static string OneOriginText() => "play.advance-unit: the units advance from one Location";

    /// <summary>
    /// A4.7, A15.431, A11.15, A12.3 (ruling R14.2, R23.5): the first condition that bars a unit from advancing, as the refusal names it (broken, pinned,
    /// berserk, melee, captured, hidden, ti), or null; a concealed unit may advance.
    /// </summary>
    public static string? AdvanceBarringCondition(bool broken, bool pinned, bool berserk, bool melee, bool captured, bool hidden, bool ti) =>
        broken ? "broken" : pinned ? "pinned" : berserk ? "berserk" : melee ? "melee" : captured ? "captured" : hidden ? "hidden" : ti ? "ti" : null;

    /// <summary>A4.7: a unit that has advanced or moved as far as it may this phase.</summary>
    public static string? MovementEndedBar(string unitId, bool movementEnded) =>
        movementEnded ? $"play.advance-unit: {unitId} has advanced or moved as far as it may this phase (A4.7)" : null;

    /// <summary>C10, A21.13: a crew manning a Gun does not advance; abandoning or moving a Gun is not reviewed.</summary>
    public static string GunnerText(string unitId) => $"play.advance-crew-mans-gun: {unitId} mans a Gun; abandoning or moving a Gun is not reviewed (C10, A21.13)";

    /// <summary>A4.7 (ruling R25.3): only Infantry advance.</summary>
    public static string VehicleText(string unitId) => $"play.advance-unit: {unitId} is a vehicle; only Infantry advance (A4.7)";

    /// <summary>A11.6 (ruling R11.17): the manned, unconcealed enemy AFV at the destination that calls for a PAATC.</summary>
    public static bool PaatcAfv(bool active, bool enemy, bool afv, bool abandoned, bool concealed, bool hidden) => active && enemy && afv && !abandoned && !concealed && !hidden;

    /// <summary>A4.7 (rulings R10.1 to R10.3): a step's refusal, worded for the advance.</summary>
    public static string AdvanceStepReason(string moveReason)
    {
        ArgumentNullException.ThrowIfNull(moveReason);
        return moveReason.Replace("play.move-", "play.advance-", StringComparison.Ordinal);
    }

    /// <summary>B16.4: a marsh hex cannot be entered in the APh.</summary>
    public static string? MarshBar(bool allMf) => allMf ? "play.advance-marsh: a marsh hex cannot be entered in the APh (B16.4)" : null;

    /// <summary>
    /// A5.1, A5.5 (ruling R14.10): the squad-equivalents of one side's units above three, rounded up, a HS or crew half and five SMC a HS (four or fewer none).
    /// </summary>
    public static int OverstackExcess(int squads, int halfSquadsAndCrews, int smc)
    {
        var equivalents = squads + (halfSquadsAndCrews / 2m) + (Math.Floor(smc / 5m) / 2m);
        return equivalents > 3 ? (int)Math.Ceiling(equivalents - 3) : 0;
    }

    /// <summary>A7.7 (ruling R12.11), A5.11 (ruling R14.10): the entry's half MF, doubled for an Encircled unit's first Location, plus one MF per excess squad-equivalent.</summary>
    public static int AdvanceHalfMf(int entryHalfMf, bool anyEncircled, int excess) => (entryHalfMf * (anyEncircled ? 2 : 1)) + (2 * excess);

    /// <summary>
    /// A4.72 EX, A4.12, A4.42 (ruling R10.8): the MMC advancing with an unbroken leader of their nationality, who add his two MF, and the one laden MMC that
    /// takes his IPC when an unbroken, unwounded leader of its nationality is with it.
    /// </summary>
    public static (HashSet<string> Aided, string? IpcTo) AdvanceAid(IReadOnlyList<AdvancingUnitFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var aided = units.Where(unit => unit.Mmc && unit.Nationality is { } nationality && units.Any(leader => leader.Id != unit.Id && leader.Leader && !leader.Broken
            && leader.Nationality == nationality)).Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
        var ipcTo = units.Where(unit => aided.Contains(unit.Id) && unit.Laden).ToArray() is [{ } onlyLaden]
            && units.Any(leader => leader.Leader && !leader.Broken && !leader.Wounded && leader.Nationality == onlyLaden.Nationality) ? onlyLaden.Id : null;
        return (aided, ipcTo);
    }

    /// <summary>
    /// A4.72: an advance into a Location of this half MF cost is into Difficult Terrain for a unit with this allotment: at least four MF, or all of its
    /// non-Double Time allotment after portage, whichever is less. Null when the allotment is not decided or none is left after portage.
    /// </summary>
    public static bool? DifficultAdvance(int? allotment, int halfMf) => allotment is { } value && value > 0 ? halfMf >= 2 * Math.Min(4, value) : null;

    /// <summary>A4.7, A4.72: a unit with no MF allotment the catalog decides, or none left after portage, does not advance.</summary>
    public static string NoMfText(string unitId) => $"play.advance-mf: {unitId} has no MF allotment the catalog decides, or none left after portage (A4.7, A4.72)";

    /// <summary>A4.72: a CX unit may not advance into Difficult Terrain.</summary>
    public static string? CxDifficultBar(string unitId, bool difficult, bool cx) =>
        difficult && cx ? $"play.advance-difficult-terrain: {unitId} is CX and may not advance into Difficult Terrain (A4.72)" : null;

    /// <summary>A20.54 (ruling R14.5): an Unarmed unit that is not a prisoner never enters a Location of a Known enemy unit.</summary>
    public static string? UnarmedBar(bool knownEnemyThere, string? unarmedId) =>
        knownEnemyThere && unarmedId is { } id ? $"play.advance-unarmed: {id} is Unarmed and may not enter a Location of a Known enemy unit (A20.54)" : null;

    /// <summary>C11 (ruling R24.3): CC with a Gun's crew is not reviewed.</summary>
    public static string? CrewBar(bool crewThere) => crewThere ? "play.advance-crew: CC with a Gun's crew is not reviewed (C11, ruling R24.3)" : null;

    /// <summary>A19.12 (ruling R14.11), E1.54 (ruling R16.6): a Disrupted enemy there surrenders by day unless its side is under No Quarter.</summary>
    public static bool Surrenders(bool night, bool disrupted, bool noQuarter) => !night && disrupted && !noQuarter;

    /// <summary>A19.12: an unconcealed armed Personnel unit among the advancers may take the surrender.</summary>
    public static bool Taker(bool unarmed, bool concealed, bool hidden, bool mmc, bool smc) => !unarmed && !concealed && !hidden && (mmc || smc);

    /// <summary>
    /// The enemy counters an advance meets, as the advancing side may read them (pass 31d; rulings R23.1, R31d.3): a Known unit by its id, every other
    /// counter on the map as one "concealed stack", and a hidden unit not at all.
    /// </summary>
    public static string Opposing(IReadOnlyList<OpposingCounterFacts> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        string[] known = [.. enemies.Where(unit => unit.Known).Select(unit => unit.Id)];
        var stack = enemies.Any(unit => !unit.Known && !unit.Hidden);
        return string.Join(", ", known) + (stack ? (known.Length > 0 ? " and " : string.Empty) + "a concealed stack" : string.Empty);
    }

    /// <summary>The plan's words (A3.7, A2.5, A4.72, A5.11, A19.12).</summary>
    public static string AdvanceSummary(IReadOnlyList<string> ids, string toText, string terrain, bool entering, string opposing, IReadOnlyList<string> tiring, int excess,
        IReadOnlyList<string> surrendering, bool takers)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(tiring);
        ArgumentNullException.ThrowIfNull(surrendering);
        return $"play.advance: {string.Join(", ", ids)} {(ids.Count == 1 ? "advances" : "advance")} into {toText} ({terrain})"
            + (entering ? " from off board, entering the map (A2.5; ruling R25.1)" : string.Empty) + (opposing.Length > 0 ? $", with {opposing}: CC follows (A3.7)" : string.Empty)
            + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} {(tiring.Count == 1 ? "becomes" : "become")} CX advancing into Difficult Terrain (A4.72)" : string.Empty)
            + (excess > 0 ? $"; the Location is overstacked by {excess} squad-equivalent(s), which costs {excess} more MF (A5.11)" : string.Empty)
            + (surrendering.Count > 0 && takers ? $"; {string.Join(", ", surrendering)} is Disrupted and surrenders (A19.12)" : string.Empty);
    }

    /// <summary>A11.19 (pass 31d, design D5): the proposer's own Dummies advancing into a Location with a visible enemy counter are removed as the CCPh begins.</summary>
    public static bool DummiesWarned(bool anyDummy, bool anyVisibleEnemy) => anyDummy && anyVisibleEnemy;

    /// <summary>A11.6: each testing unit passes its own PAATC before it advances.</summary>
    public static string PaatcText(IReadOnlyList<string> testing, string afvId)
    {
        ArgumentNullException.ThrowIfNull(testing);
        return $"play.advance-paatc: {string.Join(", ", testing)} each pass a PAATC before advancing on {afvId}; a unit that fails is pinned and stays (A11.6)";
    }

    /// <summary>A12.14 (ruling R14.2), E1.31 (ruling R16.4): concealed advancers lose their "?" entering Open Ground in the LOS of a Good Order enemy within 16 hexes, not at night; the LOS is read lazily.</summary>
    public static bool LosesConcealment(bool night, string terrain, Func<bool> enemyGoodOrderInLosWithin16)
    {
        ArgumentNullException.ThrowIfNull(enemyGoodOrderInLosWithin16);
        return !night && terrain == "open-ground" && enemyGoodOrderInLosWithin16();
    }
}
