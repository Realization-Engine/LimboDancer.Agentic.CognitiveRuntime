namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A unit a side would break voluntarily (A10.3, A10.41; pass 70, ruling R70.11), as the planner reads it: whether it is an active unit of that side on
/// the map, Personnel, already broken, a hero or heroic, berserk, Unarmed, a prisoner, in Melee, aboard a vehicle, and whether its counter has a broken side.
/// </summary>
public sealed record VoluntaryBreakFacts(string UnitId, bool OnMap, bool OfSide, bool Personnel, bool Broken, bool Hero, bool Berserk, bool Unarmed, bool Captured,
    bool Melee, bool Aboard, bool BrokenSide);

/// <summary>
/// A leader a broken unit would rout with (A10.711; pass 70, ruling R70.12), as the planner reads him: active on the map, of the unit's side, a leader,
/// broken, pinned, berserk, in Melee, a prisoner, aboard a vehicle, stacked with the broken unit before it routs, and whether he has routed this RtPh.
/// </summary>
public sealed record VoluntaryRoutLeaderFacts(string LeaderId, bool OnMap, bool OfSide, bool Leader, bool Broken, bool Pinned, bool Berserk, bool Melee, bool Captured,
    bool Aboard, bool StackedWith, bool RoutedThisPhase);

/// <summary>A unit that breaks voluntarily, as its effects read it (A10.41, A7.83, A12.14): whether it is pinned, and whether it is concealed or hidden.</summary>
public sealed record VoluntaryBreakUnit(string Id, bool Pinned, bool Concealed);

/// <summary>
/// Voluntary Break and Voluntary Rout (pass 70, task 70.1; rulings R70.11, R70.12). A10.41 (p. 66): "Units within both the LOS and Normal Range per A10.532
/// of an armed, unbroken Known enemy ground--and/or ADJACENT to any unbroken enemy ground unit--may voluntarily break (even if pinned) at the start of the
/// RtPh so as to be able to rout during that RtPh (but only if breaking will not cause their immediate Reduction or elimination)". A10.711 (p. 69): a
/// non-berserk, non-pinned leader already stacked with a broken unit before it routs may rout with it, takes no Interdiction NMC himself, may add his
/// leadership DRM to its NMC, and is eliminated if it fails. Play reads the state and the map and hands the facts over; these decide.
/// </summary>
public static class ScenarioA1VoluntaryRout
{
    /// <summary>
    /// A10.41 (ruling R70.11): a Voluntary Break is made at the start of the RtPh, before any unit of either side routs.
    /// </summary>
    public static string? BreakPhaseBar(string? phase, bool routStepTaken) =>
        phase != "rtph" ? "play.voluntary-break: units break voluntarily at the start of the RtPh (A10.41)"
            : routStepTaken ? "play.voluntary-break: units break voluntarily at the start of the RtPh, before any unit routs; a unit has routed this RtPh (A10.41)"
            : null;

    /// <summary>
    /// Why a unit may not break voluntarily (A10.3, A10.41; ruling R70.11), or null. The enemy unit that allows it is read last, only for a unit that
    /// could break: <paramref name="threat"/> names the Known enemy unit in whose LOS and Normal Range, or ADJACENT to which, the unit stands, or null.
    /// A unit already broken would be Reduced by breaking (A10.3), and one with no broken side eliminated, so neither may; heroes, berserk units, and
    /// Unarmed units never break (A10.3). A unit in Melee could not rout (A10.5), which is the break's purpose. A Passenger or Rider is not built here.
    /// </summary>
    public static string? BreakUnitBar(VoluntaryBreakFacts unit, Func<string?> threat)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(threat);
        return !unit.OnMap || !unit.OfSide ? $"play.voluntary-break: '{unit.UnitId}' is not a unit of the side on the map (A10.41)"
            : !unit.Personnel ? $"play.voluntary-break: {unit.UnitId} is not Personnel; only Personnel break (A10.3, A10.41)"
            : unit.Broken ? $"play.voluntary-break: {unit.UnitId} is already broken, and breaking again would Reduce it at once (A10.3, A10.41)"
            : unit.Hero ? $"play.voluntary-break: {unit.UnitId} is a hero, which never breaks (A10.3, A15.2)"
            : unit.Berserk ? $"play.voluntary-break: {unit.UnitId} is berserk, which never breaks (A10.3, A15.42)"
            : unit.Unarmed || unit.Captured ? $"play.voluntary-break: {unit.UnitId} is Unarmed, which never breaks (A10.3, A20.54)"
            : !unit.BrokenSide ? $"play.voluntary-break: {unit.UnitId} has no broken side, so breaking would eliminate it at once (A10.41)"
            : unit.Melee ? $"play.voluntary-break: {unit.UnitId} is in Melee, where a broken unit does not rout (A10.41, A10.5)"
            : unit.Aboard ? $"play.voluntary-break: {unit.UnitId} rides a vehicle; a Passenger's or Rider's Voluntary Break is not built"
            : threat() is null ? $"play.voluntary-break: {unit.UnitId} is neither in the LOS and Normal Range of a Known armed unbroken enemy ground unit nor ADJACENT to a Known unbroken one, so it may not break voluntarily (A10.41); a concealed or hidden enemy unit does not count (ruling R70.43)"
            : null;
    }

    /// <summary>
    /// The Known enemy unit that lets a unit in a Location break voluntarily (A10.41, A10.532; rulings R70.11, R70.43), or null: an unbroken one ADJACENT to
    /// it or in its Location, armed or not ("ADJACENT to any unbroken enemy ground unit"; an Abandoned vehicle, with no crew, is no ground unit that counts),
    /// or an armed unbroken one, not Unarmed Personnel, with a clear LOS to it within its Normal Range. The enemies are the Known ones the rout reads
    /// (A10.533), so the answer tells the side nothing it does not hold; a concealed or hidden enemy does not allow the break, a deviation (ruling R70.43).
    /// A vehicle's Normal Range is its weapons', and a Gun's crew's takes in the Gun's, as the rout reads them (ruling R70.14, increment 3d).
    /// </summary>
    public static string? Threat(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        // RoutEnemyFacts.Armed is false only for an Abandoned vehicle (ScenarioA1RoutCalculator.Armed); Unarmed Personnel stay in the ADJACENT clause.
        return enemies.Where(item => item.Armed && !item.Broken && ScenarioA1RoutCalculator.AdjacentOrSame(reader, item.Location, at))
                .Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault()
            ?? enemies.Where(item => item.Armed && item.Unarmed != true && !item.Broken && item.NormalRange > 0).OrderBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault(item => reader.Los(item.Location, at) is { Clear: true } los && los.Range <= item.NormalRange)?.Id;
    }

    /// <summary>
    /// What a Voluntary Break does to its units (A10.41, p. 66; pass 70, ruling R70.11; the Rules boundary review): each is broken and under DM (A10.62:
    /// "even if it breaks voluntarily"), a pinned one loses its pin (A7.83, and the pinned HS of the example on p. 69), and a concealed or hidden one loses
    /// its "?" (A12.14: "When a concealed unit ... breaks it loses its '?', regardless of the range ... or even if out of the LOS of all enemy units"). The
    /// units revealed and unpinned, in the order given, and each unit's condition changes in the record's order.
    /// </summary>
    public static (IReadOnlyList<string> Revealed, IReadOnlyList<string> Unpinned, IReadOnlyList<(string Id, IReadOnlyList<(UnitCondition Condition, bool Value)> Changes)> Changes)
        BreakEffects(IReadOnlyList<VoluntaryBreakUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        static IReadOnlyList<(UnitCondition Condition, bool Value)> Changes(bool pinned) => pinned
            ? [(UnitCondition.Broken, true), (UnitCondition.DesperationMorale, true), (UnitCondition.Pinned, false)]
            : [(UnitCondition.Broken, true), (UnitCondition.DesperationMorale, true)];
        return ([.. units.Where(unit => unit.Concealed).Select(unit => unit.Id)], [.. units.Where(unit => unit.Pinned).Select(unit => unit.Id)],
            [.. units.Select(unit => (unit.Id, Changes(unit.Pinned)))]);
    }

    /// <summary>
    /// A10.711 (ruling R70.12): the half MF a rout with a Voluntary Rout leader may spend: the leader "must remain with the broken unit throughout the RtPh",
    /// so the least of the unit's and his; the unit's alone with no leader.
    /// </summary>
    public static int RoutLimit(int unitHalfMf, int? leaderHalfMf) => leaderHalfMf is { } leader ? Math.Min(unitHalfMf, leader) : unitHalfMf;

    /// <summary>
    /// A12.14 (ruling R70.12): a Voluntary Rout leader still under "?" when the unit he routs with is Interdicted has moved into Open Ground in the LOS of
    /// the unbroken Interdictor, and is named with his DRM, so he loses his "?".
    /// </summary>
    public static bool InterdictionRevealsLeader(bool concealedOrHidden) => concealedOrHidden;

    /// <summary>
    /// The proposal's lines of a Voluntary Break (A10.41, A10.62, A7.83, A12.14): the units break, under DM; a pinned one loses its pin; a concealed one
    /// loses its "?" by breaking, whatever the LOS.
    /// </summary>
    public static string[] BreakSummary(IReadOnlyList<string> units, IReadOnlyList<string> unpinned, IReadOnlyList<string> revealed)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(unpinned);
        ArgumentNullException.ThrowIfNull(revealed);
        return
        [
            $"play.voluntary-break: {string.Join(", ", units)} {(units.Count == 1 ? "breaks" : "break")} voluntarily at the start of the RtPh, under DM, so as to rout (A10.41, A10.62)",
            .. unpinned.Count == 0 ? Array.Empty<string>()
                : [$"play.voluntary-break: {string.Join(", ", unpinned)} {(unpinned.Count == 1 ? "loses its" : "lose their")} pin by breaking (A7.83)"],
            .. revealed.Count == 0 ? Array.Empty<string>()
                : [$"play.concealment: {string.Join(", ", revealed)} {(revealed.Count == 1 ? "loses its" : "lose their")} \"?\" by breaking (A12.14)"],
        ];
    }

    /// <summary>The proposal's line for a leader whose Voluntary Break calls for a LLTC (A10.2, p. 65: "Should an unbroken leader break"; ruling R70.11).</summary>
    public static string LeaderBreakText(IReadOnlyList<string> leaders)
    {
        ArgumentNullException.ThrowIfNull(leaders);
        return $"play.leader-loss: the break of {string.Join(", ", leaders)} calls for a LLTC of the unbroken units of lower Morale Level in {(leaders.Count == 1 ? "his Location" : "their Locations")}, rolled now (A10.2)";
    }

    /// <summary>
    /// Why a leader may not rout with a broken unit (A10.711, A7.831; ruling R70.12), or null: he is an active, unbroken leader of its side on foot,
    /// not pinned, berserk, in Melee, or a prisoner, stacked with it before it routs, who has not routed this RtPh.
    /// </summary>
    public static string? LeaderBar(string unitId, VoluntaryRoutLeaderFacts leader)
    {
        ArgumentNullException.ThrowIfNull(leader);
        var id = leader.LeaderId;
        return !leader.OnMap || !leader.OfSide || !leader.Leader ? $"play.voluntary-rout: '{id}' is not a leader of {unitId}'s side on the map (A10.711)"
            : leader.Broken ? $"play.voluntary-rout: {id} is broken; only an unbroken leader routs with a broken unit (A10.711)"
            : leader.Pinned ? $"play.voluntary-rout: {id} is pinned and may not use Voluntary Rout (A7.831, A10.711)"
            : leader.Berserk ? $"play.voluntary-rout: {id} is berserk and may not use Voluntary Rout (A10.711)"
            : leader.Melee || leader.Captured ? $"play.voluntary-rout: {id} is in Melee or a prisoner and does not rout (A10.711)"
            : leader.Aboard ? $"play.voluntary-rout: {id} rides a vehicle; a leader on foot routs with a broken unit (A10.711)"
            : !leader.StackedWith ? $"play.voluntary-rout: {id} is not stacked with {unitId}, so he may not rout with it (A10.711)"
            : leader.RoutedThisPhase ? $"play.voluntary-rout: {id} has routed this RtPh (A10.711)"
            : null;
    }

    /// <summary>Why a broken Passenger names no Voluntary Rout leader (D6.1; ruling R70.12): it stays aboard or routs beneath its vehicle alone.</summary>
    public static string BeneathLeaderText(string unitId, string vehicleId) =>
        $"play.voluntary-rout: {unitId} rides {vehicleId} and stays aboard or routs beneath it alone; a leader routs with a broken unit on foot (A10.711, D6.1)";

    /// <summary>A10.711 (ruling R70.12): the leadership DRM the Voluntary Rout leader adds to the broken unit's Interdiction NMC, one worse when wounded (A17.3).</summary>
    public static int LeaderDrm(int? leadership, bool wounded) => ScenarioA1SupportWeaponRules.LeaderDrm(leadership, wounded);

    /// <summary>A10.711: the Voluntary Rout leader "is eliminated if the broken unit he is stacked on top of fails an Interdiction MC": a Reduction or an elimination.</summary>
    public static bool LeaderEliminated(InterdictionOutcome outcome) => outcome is InterdictionOutcome.Reduced or InterdictionOutcome.Eliminated;

    /// <summary>The proposal's line for a Voluntary Rout (A10.711; ruling R70.12).</summary>
    public static string LeaderRoutText(string leaderId, string unitId) =>
        $"play.voluntary-rout: {leaderId} routs with {unitId} by Voluntary Rout: he takes no Interdiction NMC himself, adds his leadership DRM to its NMC, and is eliminated if it fails (A10.711)";

    /// <summary>
    /// A10.711 (UNIT-STATE-041): a Voluntary Rout step moves an unbroken leader, not pinned, berserk, or in Melee, into the Location the broken unit he
    /// routs with has just entered, within his own MF, as a rout step does.
    /// </summary>
    public static bool LeaderStepAllowed(bool broken, bool pinned, bool berserk, bool melee, bool withBrokenThere, int halfMf, bool lowCrawl, int mfSpent, bool halfMfSpent,
        int routHalfMf, bool routedThisPhase) =>
        !broken && !pinned && !berserk && withBrokenThere
            && ScenarioA1RoutRallyProjection.RoutStepAllowed(true, melee, false, halfMf, lowCrawl, mfSpent, halfMfSpent, routHalfMf, routedThisPhase);

    /// <summary>A10.711 (UNIT-STATE-041): the leader an Interdiction names is an active unbroken leader who routed this RtPh and stands where it is taken.</summary>
    public static bool InterdictionLeaderAgrees(bool active, bool broken, bool there, bool routedThisPhase) => active && !broken && there && routedThisPhase;

    /// <summary>A10.41 (UNIT-STATE-041): a Voluntary Break is in the RtPh before any rout step, of active units not broken.</summary>
    public static bool BreakRecordAllowed(string? phase, bool routStepTaken, IEnumerable<(bool Active, bool Broken)> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var list = units.ToArray();
        return phase == "rtph" && !routStepTaken && list.Length > 0 && list.All(unit => unit.Active && !unit.Broken);
    }

    /// <summary>A10.41, A10.711 (UNIT-STATE-041): the refusal of a Voluntary Break or a Voluntary Rout record.</summary>
    public static RecordRefusal Refusal() =>
        new("UNIT-STATE-041", "A Voluntary Break is made in the RtPh before any rout, by units not broken; a Voluntary Rout leader is unbroken, unpinned, and routs with the broken unit he is stacked with (A10.41, A10.711).");
}
