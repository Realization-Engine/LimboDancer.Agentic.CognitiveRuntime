namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A concealed armed Good Order enemy unit as the Casualty dr reads it (A12.154): a hero or a heroic leader is Stealthy, a leader's leadership counts, a Lax unit adds.</summary>
public sealed record MopUpDefenderFacts(bool Hero, bool Leader, bool Heroic, int HalfSquads, int? Leadership, bool Wounded, bool Lax)
{
    /// <summary>
    /// Its status as <see cref="ScenarioA1StealthCalculator.Status"/> gives it for the Search Casualties dr (the A11.17 claim, ruling RA11S.7): when
    /// set, <see cref="ScenarioA1MoppingUp.CasualtyDrm"/> counts it in place of the hero test and <see cref="Lax"/>. Null, as before the claim, reads them.
    /// </summary>
    public StealthStatus? Stealth
    {
        get; init;
    }
}

/// <summary>
/// Mopping Up (A12.153, A12.154; pass 24 of the Card Play and Map Studio Redesign Plan, ruling R24.2; pass 32.g, S6; pass 70, ruling R70.25: any building of
/// the map): the bars in their order, who Mops Up, what is found, the secured building, the surrenders, and the DEFENDER's Casualty dr. Play reads the card,
/// the state, and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1MoppingUp
{
    /// <summary>
    /// A12.153 (rulings R24.2, R70.25): the building named is one of the map's, by the card's id where the card names it and otherwise by its board and its
    /// lowest hex, such as "bd03:M2"; a building of no card is Mopped Up as a card's is.
    /// </summary>
    public static string? BuildingBar(string? building, bool found) => found ? null
        : $"play.mop-up-building: '{building}' is not a building of the map; a building is named by the card's id, or by its board and its lowest hex, such as bd03:M2 (A12.153; ruling R70.25)";

    /// <summary>A12.153: Mopping Up is declared in its side's PFPh.</summary>
    public static string? PhaseBar(string? phase) => phase != "pfph" ? "play.mop-up-phase: Mopping Up is declared in its side's PFPh (A12.153)" : null;

    /// <summary>A12.153: a Location is inside the building when its hex is one of the building's and it is not below ground.</summary>
    public static bool InBuilding(int level, bool hexOfBuilding) => level >= 0 && hexOfBuilding;

    /// <summary>A12.153: Mopping Up is of a multi-hex or multi-level building.</summary>
    public static string? SingleHexBar(int hexCount, bool anyUpperLevel, string building) =>
        hexCount == 1 && !anyUpperLevel ? $"play.mop-up-building: building {building} has one hex and one level; Mopping Up is of a multi-hex or multi-level building (A12.153)" : null;

    /// <summary>A12.153: once per building per Player Turn.</summary>
    public static string? OnceBar(bool moppedUpThisPlayerTurn, string building) =>
        moppedUpThisPlayerTurn ? $"play.mop-up-once: building {building} was Mopped Up this Player Turn; it may be once per Player Turn (A12.153)" : null;

    /// <summary>A12.153, A20.3, A20.4: a side that has employed No Quarter or Massacre may no longer Mop Up.</summary>
    public static string? NoQuarterBar(bool employed, string side) =>
        employed ? $"play.mop-up-no-quarter: the {side} side has employed No Quarter or Massacre and may no longer Mop Up (A12.153, A20.3, A20.4)" : null;

    /// <summary>A12.153: a unit Mopping Up is an active Infantry MMC (squad, HS, or crew) of the side inside the building.</summary>
    public static bool MopUpUnit(bool active, bool ofSide, bool mmc, bool inBuilding) => active && ofSide && mmc && inBuilding;

    /// <summary>A12.153: the units named are one or more such units.</summary>
    public static string? UnitsBar(int count, bool anyIneligible, string side, string building) =>
        count == 0 || anyIneligible ? $"play.mop-up-units: each unit Mopping Up is an active Infantry MMC of the {side} side in building {building} (A12.153)" : null;

    /// <summary>A12.153 (table player, pass 24): why a unit may not Mop Up, in the order checked; null when it may.</summary>
    public static string? UnitWhy(bool broken, bool berserk, bool pinned, bool captured, bool unarmed, bool melee, bool ti, bool prepFired) =>
        broken ? "is broken" : berserk ? "is berserk" : pinned ? "is pinned" : captured || unarmed ? "is not armed" : melee ? "is in Melee" : ti ? "is already TI" : prepFired ? "fired in this PFPh" : null;

    /// <summary>A12.153: the refusal that says why.</summary>
    public static string UnitText(string unitId, string why) => $"play.mop-up-unit: {unitId} {why}; Mopping Up takes an armed, unpinned, Good Order MMC free to become TI (A12.153)";

    /// <summary>A26.11 (referee and table player, pass 24): an enemy unit inside the building; an enemy vehicle in Bypass is not inside it.</summary>
    public static bool EnemyInside(bool active, bool enemy, bool inBuilding, bool vehicle, bool straddling) => active && enemy && inBuilding && !(vehicle && straddling);

    /// <summary>A12.153: an unconcealed unbroken enemy unit, which bars Mopping Up.</summary>
    public static bool UnconcealedUnbroken(bool dummy, bool concealed, bool hidden, bool broken, bool captured) => !dummy && !concealed && !hidden && !broken && !captured;

    /// <summary>A12.153: the building holds an unconcealed unbroken enemy unit.</summary>
    public static string EnemyText(string building) => $"play.mop-up-enemy: building {building} holds an unconcealed unbroken enemy unit (A12.153)";

    /// <summary>
    /// A12.153, A26.11 (ruling R24.1): some unit Mopping Up is within two hexes of a ground-level Location its side does not Control; the distances are the
    /// units' to that hex, undecided when the map cannot give one.
    /// </summary>
    public static string? RangeBar(IReadOnlyList<int?> near, string hexText, string side)
    {
        ArgumentNullException.ThrowIfNull(near);
        if (near.Any(distance => distance is null))
        {
            return $"play.mop-up-range: the distance to {hexText} is not decided on this map (A12.153)";
        }

        return !near.Any(distance => distance <= 2)
            ? $"play.mop-up-range: no unit Mopping Up is within two hexes of {hexText}, a ground-level Location the {side} side does not Control (A12.153, A26.11)"
            : null;
    }

    /// <summary>
    /// A12.153, A20.5, A20.4 (referee, pass 24): the guard the broken enemy surrender to: an armed Personnel unit of the side inside the building, even a
    /// broken SMC, never a berserk one.
    /// </summary>
    public static bool Guard(bool active, bool ofSide, bool dummy, bool vehicle, bool broken, bool leaderOrHero, bool berserk, bool unarmed, bool captured, bool inBuilding) =>
        active && ofSide && !dummy && !vehicle && !(broken && !leaderOrHero) && !berserk && !unarmed && !captured && inBuilding;

    /// <summary>A12.153, A20.5: the guard named may not guard.</summary>
    public static string GuardText(string guardId, string side, string building) =>
        $"play.mop-up-guard: {guardId} is not an armed Personnel unit of the {side} side inside building {building} that may guard prisoners (A12.153, A20.5)";

    /// <summary>A12.153 (ruling R24.2): a hidden enemy unit is placed beneath "?", in the record's order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> HiddenPlacedConditions() => [(UnitCondition.Hidden, false), (UnitCondition.Concealed, true)];

    /// <summary>
    /// A12.153, A12.154 (rulings R24.2, R70.25; the referee's finding at the reviews of pass 70): the proposal's words, from what the declaring side knows:
    /// what the Mopping Up finds under the "?" there is decided at the commit and said by the records.
    /// </summary>
    public static string ProposalText(IReadOnlyList<string> ids, string building, string side, string guardId)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return (ids.Count == 1
            ? $"play.mop-up: {ids[0]} Mops Up building {building} and becomes TI (A12.153; ruling R24.2)"
            : $"play.mop-up: {string.Join(", ", ids)} Mop Up building {building} and become TI (A12.153; ruling R24.2)")
            + "; hidden enemy units there are placed beneath \"?\" and enemy Dummies removed; with no concealed enemy unit left, the "
            + $"{side} side Controls the building and its Locations (A26.11) and its broken enemy units surrender to {guardId} (A20.5); otherwise the DEFENDER's "
            + "Casualty dr, which is always made, Reduces a Mopping-Up unit on a Final dr of 1 or less (A12.154; ruling R70.45). What it finds is said when it is made";
    }

    /// <summary>A12.153: a concealed enemy unit left in the building: not a Dummy, concealed or hidden.</summary>
    public static bool Concealed(bool dummy, bool concealed, bool hidden) => !dummy && (concealed || hidden);

    /// <summary>A26.11: an armed enemy vehicle in Bypass keeps its ground level out of the secured set.</summary>
    public static bool BypassedVehicle(bool active, bool enemy, bool vehicle, bool straddling, bool abandoned, bool captured) => active && enemy && vehicle && straddling && !abandoned && !captured;

    /// <summary>A12.153: a broken enemy unit in the secured building surrenders: not a Dummy, broken, not already a prisoner.</summary>
    public static bool Surrenders(bool dummy, bool broken, bool captured) => !dummy && broken && !captured;

    /// <summary>A12.153, A20.5: the surrendering unit's conditions, in the record's order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> SurrenderConditions() =>
        [(UnitCondition.Broken, false), (UnitCondition.Disrupted, false), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, false)];

    /// <summary>A12.154: a concealed enemy unit that defends with the Casualty dr: unbroken, armed, not a prisoner.</summary>
    public static bool Defender(bool broken, bool unarmed, bool captured) => !broken && !unarmed && !captured;

    /// <summary>
    /// A12.154: the Casualty dr's drm and its notes: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader's leadership unless alone, +1
    /// per Lax unit. A defender whose <see cref="MopUpDefenderFacts.Stealth"/> is null keeps the old answer (the A11.17 claim, ruling RA11S.7):
    /// <see cref="ScenarioA1StealthCalculator.PerUnitStealthy"/> and <see cref="ScenarioA1StealthCalculator.PerUnitLax"/> replace the hero test and the Lax
    /// fact, and differ in that Elite and 1st Line Finns, designated forces, and SSR Stealth are Stealthy, Green and Conscript never, SSR Lax and
    /// non-elite Italians are Lax, and a Lax unit is never also Stealthy.
    /// </summary>
    public static (int Drm, IReadOnlyList<string> Notes) CasualtyDrm(IReadOnlyList<MopUpDefenderFacts> defenders)
    {
        ArgumentNullException.ThrowIfNull(defenders);
        var drm = 0;
        var notes = new List<string>();
        var stealthy = defenders.Count(unit => unit.Stealth is { } status ? status == StealthStatus.Stealthy : unit.Hero || (unit.Leader && unit.Heroic));
        if (stealthy > 0)
        {
            drm -= stealthy;
            notes.Add($"-{stealthy} Stealthy");
        }

        var halfSquads = defenders.Sum(unit => unit.HalfSquads);
        if (halfSquads > 1)
        {
            drm -= halfSquads - 1;
            notes.Add($"-{halfSquads - 1} for {halfSquads} HS-equivalents");
        }

        if (defenders.Count > 1 && defenders.Where(unit => unit.Leader && unit.Leadership is not null)
            .Select(unit => unit.Leadership!.Value + (unit.Wounded ? 1 : 0)).DefaultIfEmpty(0).Min() is var leadership and not 0)
        {
            drm += leadership;
            notes.Add($"{leadership:+0;-0} leadership");
        }

        var lax = defenders.Count(unit => unit.Stealth is { } status ? status == StealthStatus.Lax : unit.Lax);
        if (lax > 0)
        {
            drm += lax;
            notes.Add($"+{lax} Lax");
        }

        return (drm, notes);
    }

    /// <summary>A12.154: a unit's HS-equivalents: a squad two, a HS or crew one, others none.</summary>
    public static int HalfSquads(bool squad, bool halfSquadOrCrew) => squad ? 2 : halfSquadOrCrew ? 1 : 0;

    /// <summary>A12.154 (ruling R24.2): a Final dr of 1 or less Reduces a Mopping-Up unit.</summary>
    public static bool CasualtyReduces(int dr, int drm) => dr + drm <= 1;

    /// <summary>
    /// A12.154 (rulings R24.2, R70.45; pass 70's fixes): a record's words for the DEFENDER's Casualty dr of a Search or a Mopping Up: the dr, its drm and Final dr
    /// where the reading view may know them (null otherwise, since the drm tells what his "?" hide), and the units it Reduced, or none.
    /// </summary>
    public static string CasualtyRecordText(int dr, int? drm, IReadOnlyList<string> reduced)
    {
        ArgumentNullException.ThrowIfNull(reduced);
        var roll = drm is { } value && value != 0 ? $"{dr} {(value < 0 ? "-" : "+")} {Math.Abs(value)} = {dr + value}" : $"{dr}";
        return $"the DEFENDER's Casualty dr {roll}: "
            + (reduced.Count == 0 ? "no casualty" : $"{string.Join(", ", reduced)} {(reduced.Count == 1 ? "is" : "are")} Casualty Reduced") + " (A12.154)";
    }

    /// <summary>A12.154 (ruling R24.2): the units struck, by Random Selection among several: the highest dr, ties all.</summary>
    public static IReadOnlyList<string> Struck(IReadOnlyList<string> ids, IReadOnlyList<int> selection)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(selection);
        return [.. ids.Where((_, index) => selection[index] == selection.Max())];
    }

    /// <summary>A12.153 (rulings R24.2, R70.25): a building that may be Mopped Up: more than one hex, or more than one level.</summary>
    public static bool MopUpBuilding(int hexCount, bool anyUpperLevel) => hexCount > 1 || anyUpperLevel;

    /// <summary>
    /// A12.153 (p. 79; ruling R70.25): "A rubbled/Blazing building Location is not considered part of a building for Mopping-Up purposes": a hex with a Blaze
    /// is left out; a rubbled one has no building terrain, so the map never finds it in a building.
    /// </summary>
    public static bool PartOfBuilding(bool blaze) => !blaze;
}
