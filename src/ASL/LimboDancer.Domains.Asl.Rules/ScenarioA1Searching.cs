namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A unit that would Search (A12.152; pass 70, ruling R70.24), as the planner reads it: active on the map, of the phasing side, a member of the moving stack,
/// Infantry on foot (Personnel, not aboard a vehicle), a MMC, a leader, broken, berserk, pinned, a prisoner or Unarmed, a hero, heroic, its HS-equivalents,
/// a leader's leadership, wounded, Lax, and CX.
/// </summary>
public sealed record SearcherFacts(string Id, bool Active, bool OfSide, bool Member, bool Infantry, bool Mmc, bool Leader, bool Broken, bool Berserk, bool Pinned,
    bool Captured, bool Unarmed, bool Hero, bool Heroic, int HalfSquads, int? Leadership, bool Wounded, bool Lax, bool Cx);

/// <summary>
/// Searching (A12.152, A12.154, p. 79; pass 70, task 70.9, ruling R70.24). "As each Good Order Infantry/Cavalry MMC, or moving stack that contains ≥ one
/// MMC, ends its move it may attempt to reveal concealed enemy units ... in Accessible hexes (including its own) by expending one additional MF in its
/// present hex and making a Search dr, provided that all units making the attempt are neither Pinned nor using Assault Movement." The searchers are TI for
/// the rest of the Player Turn; the Final dr is the number of Accessible hexes other than its own, of the ATTACKER's choice, it may not Search. The Index's
/// "Accessible" is not in the text of the book's pages (searched 2026-10-25), so ruling R70.24's reading holds: the stack's own hex and the ADJACENT hexes it
/// could enter. Minefields (A12.33), Fortified Buildings, and Booby Traps and HE FFE as Casualty triggers are not built (the backlog). Play reads the
/// state and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1Searching
{
    /// <summary>The purpose of the Search dr's record.</summary>
    public const string Purpose = "search";

    /// <summary>A12.152: a Search is made in the MPh.</summary>
    public static string? PhaseBar(string? phase) => phase != "mph" ? "play.search-phase: a stack Searches in its MPh, as it ends its move (A12.152)" : null;

    /// <summary>
    /// A12.152, A8.1 (ruling R70.24): a Search is made by Infantry of the moving stack as it ends its move, so once the DEFENDER's window on its last MF
    /// is closed; never while Assault Movement is used.
    /// </summary>
    public static string? MoveBar(bool moving, bool vehicle, bool windowOpen, bool assault) =>
        !moving || vehicle ? "play.search-move: a Search is made by Infantry of the moving stack as it ends its move; no such stack is moving (A12.152)"
        : windowOpen ? "play.search-move: the DEFENDER's window on the stack's last MF is open; once he fires or passes the stack may Search as it ends its move (A12.152, A8.1)"
        : assault ? "play.search-assault: units using Assault Movement may not Search (A12.152)"
        : null;

    /// <summary>A12.152: why a unit may not Search, in the order checked; null when it may.</summary>
    public static string? UnitBar(SearcherFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return !unit.Active || !unit.OfSide || !unit.Member ? $"play.search-units: '{unit.Id}' is not a member of the moving stack; only its units Search as it ends its move (A12.152)"
            : !unit.Infantry ? $"play.search-units: {unit.Id} is not Infantry on foot; Infantry Searches (A12.152)"
            : unit.Broken || unit.Berserk || unit.Captured || unit.Unarmed ? $"play.search-units: {unit.Id} is not a Good Order unit free to Search (A12.152)"
            : unit.Pinned ? $"play.search-units: {unit.Id} is pinned, and a pinned unit may not Search (A12.152)"
            : null;
    }

    /// <summary>A12.152: the searchers are a MMC or a stack with one.</summary>
    public static string? MmcBar(IReadOnlyList<SearcherFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return units.Count == 0 || !units.Any(unit => unit.Mmc) ? "play.search-units: a Search is made by a Good Order MMC, or a moving stack with one; a SMC alone may not Search (A12.152)" : null;
    }

    /// <summary>A12.152: each searcher spends one more MF in its hex, so it must have one left.</summary>
    public static string MfText(string unitId) => $"play.search-mf: {unitId} has no MF left to Search; a Search costs one more MF in its present hex (A12.152)";

    /// <summary>A12.152 (ruling R70.24): the hexes named are the stack's Accessible ones other than its own, once each.</summary>
    public static string? HexesBar(IReadOnlyList<string> named, Func<string, bool> accessible)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(accessible);
        if (named.Distinct(StringComparer.Ordinal).Count() != named.Count)
        {
            return "play.search-hexes: a Search names each hex it prefers once (A12.152; ruling R70.24)";
        }

        return named.FirstOrDefault(hex => !accessible(hex)) is { } other
            ? $"play.search-hexes: {other} is not an Accessible hex of the stack: a hex ADJACENT to it that it could enter (A12.152; ruling R70.24)"
            : null;
    }

    /// <summary>
    /// A12.152: the Search dr's drm and its notes: -1 per Stealthy searching unit (a hero, or a heroic leader), -1 per HS-equivalent beyond one HS, the
    /// leadership of the best participating leader (a wounded one's one worse), +1 per Lax or CX searching unit, for each condition. The +2 against the
    /// Japanese (G1.63) is not built.
    /// </summary>
    public static (int Drm, IReadOnlyList<string> Notes) Drm(IReadOnlyList<SearcherFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var drm = 0;
        var notes = new List<string>();
        var stealthy = units.Count(unit => unit.Hero || (unit.Leader && unit.Heroic));
        if (stealthy > 0)
        {
            drm -= stealthy;
            notes.Add($"-{stealthy} Stealthy");
        }

        var halfSquads = units.Sum(unit => unit.HalfSquads);
        if (halfSquads > 1)
        {
            drm -= halfSquads - 1;
            notes.Add($"-{halfSquads - 1} for {halfSquads} HS-equivalents");
        }

        if (units.Where(unit => unit.Leader && unit.Leadership is not null).Select(unit => unit.Leadership!.Value + (unit.Wounded ? 1 : 0)).DefaultIfEmpty(0).Min() is var leadership and not 0)
        {
            drm += leadership;
            notes.Add($"{leadership:+0;-0} leadership");
        }

        var lax = units.Count(unit => unit.Lax);
        if (lax > 0)
        {
            drm += lax;
            notes.Add($"+{lax} Lax");
        }

        var cx = units.Count(unit => unit.Cx);
        if (cx > 0)
        {
            drm += cx;
            notes.Add($"+{cx} CX");
        }

        return (drm, notes);
    }

    /// <summary>
    /// A12.152 (ruling R70.24): whether a leader's leadership is applied to the Search dr, which costs the searchers' "?" in the LOS of a Good Order enemy
    /// ground unit within 16 hexes (A12.141; ruling R70.17).
    /// </summary>
    public static bool LeadershipApplied(IReadOnlyList<SearcherFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return units.Any(unit => unit.Leader && unit.Leadership is { } leadership && leadership + (unit.Wounded ? 1 : 0) != 0);
    }

    /// <summary>
    /// A12.152 (ruling R70.24): how many of the hexes named, in the ATTACKER's order of preference, are searched besides the stack's own: its Accessible hexes
    /// other than its own less the Final dr, never fewer than none, never more than he named.
    /// </summary>
    public static int SearchedCount(int accessible, int finalDr, int named) => Math.Max(0, Math.Min(named, accessible - finalDr));

    /// <summary>
    /// A12.154: a unit of the DEFENDER in a Searched Location that calls for his Casualty dr: an armed Good Order unit, not a Dummy, a vehicle, a prisoner, or
    /// Unarmed, concealed or not.
    /// </summary>
    public static bool CasualtyTrigger(bool dummy, bool vehicle, bool broken, bool unarmed, bool captured) => !dummy && !vehicle && !broken && !unarmed && !captured;

    /// <summary>
    /// A12.154: whether the DEFENDER makes his Casualty dr: a Searched Location holds an armed Good Order unit of his, or Residual FP. A minefield, a HE FFE
    /// Blast Area, and a Booby Trap capability are not built.
    /// </summary>
    public static bool CasualtyDue(bool defender, bool residualFp) => defender || residualFp;

    /// <summary>The searchers as the record line names them.</summary>
    private static string Names(IReadOnlyList<string> ids) => ids.Count == 1 ? $"{ids[0]} Searches" : $"{string.Join(", ", ids)} Search";

    /// <summary>
    /// A12.152 (ruling R70.24): the proposal's words, before the dr: who Searches from where, the drm, the hexes in the order named, and what the dr decides.
    /// </summary>
    public static string ProposalText(IReadOnlyList<string> ids, string at, int drm, IReadOnlyList<string> notes, int accessible, IReadOnlyList<string> named)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(notes);
        ArgumentNullException.ThrowIfNull(named);
        return $"play.search: {Names(ids)} from {at}, spending one more MF there, and {(ids.Count == 1 ? "becomes" : "become")} TI for the rest of the Player Turn (A12.152): "
            + $"a Search dr ({(notes.Count == 0 ? "no drm" : string.Join(", ", notes))}) leaves that many of its {accessible} Accessible hexes other than its own unsearched; "
            + (named.Count == 0 ? "no other hex is named, so only its own is searched" : $"the hexes are searched in the order {string.Join(", ", named)}")
            + "; any Location Searched with an armed Good Order enemy unit draws the DEFENDER's Casualty dr (A12.154; ruling R70.24)";
    }

    /// <summary>
    /// A12.152 (ruling R70.24): the record's words, public to both sides: who Searched from where, the dr and its drm, the hexes searched, and what was found;
    /// a "?" placed over hidden units is said by its Location alone, as the map shows it, and "nothing is found" is said too.
    /// </summary>
    public static string RecordText(IReadOnlyList<string> units, string at, int? dr, int drm, IReadOnlyList<string> searched, int revealed, IReadOnlyList<string> placedIn, int dummies)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(searched);
        ArgumentNullException.ThrowIfNull(placedIn);
        var found = new List<string>();
        if (revealed > 0)
        {
            found.Add($"{revealed} concealed enemy unit{(revealed == 1 ? " loses its" : "s lose their")} \"?\"");
        }

        if (placedIn.Count > 0)
        {
            found.Add($"hidden enemy units are placed beneath \"?\" in {string.Join(", ", placedIn)}");
        }

        if (dummies > 0)
        {
            found.Add($"{dummies} enemy Dumm{(dummies == 1 ? "y is" : "ies are")} removed");
        }

        var roll = dr is { } value ? $"the Search dr {value}{(drm == 0 ? string.Empty : $" {(drm < 0 ? "-" : "+")} {Math.Abs(drm)} = {value + drm}")}" : "the Search dr";
        return $"{Names(units)} from {at} and {(units.Count == 1 ? "becomes" : "become")} TI: {roll}; {string.Join(", ", searched)} {(searched.Count == 1 ? "is" : "are")} searched: "
            + (found.Count == 0 ? "nothing is found" : string.Join("; ", found)) + " (A12.152)";
    }

    /// <summary>
    /// A12.154 (ruling R70.24): the units revealed by a Search: concealed ones lose "?", hidden ones are placed beneath "?", Dummies removed; a unit neither
    /// concealed nor hidden is not touched.
    /// </summary>
    public static string? Reveal(bool dummy, bool concealed, bool hidden) => dummy ? "removed" : hidden ? "placed" : concealed ? "revealed" : null;

    /// <summary>
    /// The projector's check of a <c>units-searched</c> record (A12.152; ruling R70.24): in the MPh, by members of the moving stack in its Location with the
    /// DEFENDER's window closed, its own hex searched first and no more of the others than the Accessible count less the Final dr.
    /// </summary>
    public static RecordRefusal? VerifySearched(string? phase, bool membersInLocation, bool windowClosed, bool ownFirst, int accessible, int finalDr, int others) =>
        phase != "mph" || !membersInLocation || !windowClosed || !ownFirst || others < 0 || others > Math.Max(0, accessible - finalDr)
            ? new RecordRefusal("UNIT-STATE-051", "A Search is made in the MPh by members of the moving stack in its Location once the DEFENDER's window is closed, its own hex first and no more other hexes than the Accessible count less the Final dr (A12.152).")
            : null;
}
