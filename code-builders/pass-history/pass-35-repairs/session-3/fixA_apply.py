"""Pass 35 review fixes 7, 8, 10, 18 and a stale comment (Rules, Play, Units, tests). Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"
EDITS = {
    RULES + "ScenarioA1RoutCalculator.cs": [
        # Fix 7.
        ("""    /// <summary>A20.21: the refusal of a rout by a unit that surrenders instead.</summary>""",
         """    /// <summary>
    /// A19.12 (p. 86), A20.21, and the Comprehensive Rout Example (p. 69): whether a unit bound to surrender has done so before the other side's units
    /// rout. A Disrupted unit surrenders "at the start of any RtPh", before any rout; an ATTACKER's unit surrenders in its own turn among the
    /// ATTACKER's routs, which all come before the DEFENDER's. A DEFENDER's unit that is not Disrupted still stands while the ATTACKER routs. The
    /// surrender is recorded as the phase ends (ruling R35.2); for the other side's routes the unit is a prisoner from the moment the page has it
    /// surrender. Whether it is bound to surrender is read last.
    /// </summary>
    public static bool SurrenderedBeforeTheOtherSideRouts(bool disrupted, bool attacker, Func<bool> boundToSurrender)
    {
        ArgumentNullException.ThrowIfNull(boundToSurrender);
        return (disrupted || attacker) && boundToSurrender();
    }

    /// <summary>A20.21: the refusal of a rout by a unit that surrenders instead.</summary>"""),
        # Fix 18.
        ("""    /// <summary>A10.5, as the planner has it: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three.</summary>
    public static int RoutHalfMfAsPlanned(bool smc, bool wounded) => smc && wounded ? 6 : 12;""",
         """    /// <summary>
    /// A10.5, A17.2: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three. One member for the planner and the projector (pass 35, the
    /// Rules boundary review; the pass 32 design's section 12, item 7); each hands over whether the unit is a SMC as it reads kinds.
    /// </summary>
    public static int RoutHalfMf(bool smc, bool wounded) => smc && wounded ? 6 : 12;"""),
    ],
    RULES + "ScenarioA1RoutRallyProjection.cs": [
        ("""    /// <summary>A10.5, as the projector has it: a broken unit has six MF in the RtPh, a wounded leader or hero three; the planner counts any wounded SMC (section 12).</summary>
    public static int RoutHalfMfAsRecorded(bool leaderOrHero, bool wounded) => leaderOrHero && wounded ? 6 : 12;

""", ""),
    ],
    RULES + "ScenarioA1TerrainCosts.cs": [
        # Fix 10.
        ("""        var halfMf = (rise == 1 ? 2 * laneCost : laneCost) + (facts.EntryWall is not null ? 2 : 0) + smokeHalfMf() + extra;""",
         """        // B.2 (p. 112; the referees' review): SMOKE's MF is part of the COT doubled one level up, in Bypass as in an entry.
        var smoke = smokeHalfMf();
        var halfMf = (rise == 1 ? 2 * (laneCost + smoke) : laneCost + smoke) + (facts.EntryWall is not null ? 2 : 0) + extra;"""),
    ],
    RULES + "ScenarioA1Definitions.cs": [
        ("""    /// inapplicable to other kinds, since vehicular crews' stun and shock are not yet modelled. The state's own definition (pass 32.a, slice S11);
    /// the planner's and the Close Combat calculator's differ and are listed for pass 35.""",
         """    /// inapplicable to other kinds, since vehicular crews' stun and shock are not yet modelled. The state's own definition (pass 32.a, slice S11);
    /// <see cref="GoodOrderOf"/> is the planner's since pass 35, with a vehicle's stun and shock, and the Close Combat calculator keeps its own
    /// (the pass 35 design, section 8.2)."""),
    ],
    PLAY + "GamePlanner.Rout.cs": [
        ("""    private int RoutHalfMf(UnitInstance unit) => ScenarioA1RoutCalculator.RoutHalfMfAsPlanned(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded));""",
         """    private int RoutHalfMf(UnitInstance unit) => ScenarioA1RoutCalculator.RoutHalfMf(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded));"""),
        ("""            if (!enemies.TryGetValue(side, out var known))
            {
                enemies[side] = known = [.. KnownEnemies(state, side).Select(item => new RoutEnemyFacts(""",
         """            if (!enemies.TryGetValue(side, out var known))
            {
                // A19.12, A20.21 (the referee's review): a unit that has surrendered before this side routs is a prisoner to its routes.
                var prisoners = planner.SurrenderedBeforeRouts(state);
                enemies[side] = known = [.. KnownEnemies(state, side).Where(item => !prisoners.Contains(item.Unit.Id)).Select(item => new RoutEnemyFacts("""),
        ("""    /// <summary>A20.21 (pass 35, task 35.17): what the Rout panel says of a unit that surrenders instead of routing, or null when it routs as any other.</summary>""",
         """    // Kept once a state: the read asks for each broken unit's routes, which read the enemy list in turn.
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameState, HashSet<string>> surrenderedFirst = [];

    /// <summary>
    /// The units of a state that have surrendered before the other side's units rout (A19.12, A20.21, the example on p. 69): Rules decides, from
    /// whether each is Disrupted or the ATTACKER's and bound to surrender. While the set is being read it is empty, so the routes read for it see
    /// every unit standing, as the page has it at the moment each surrender is decided.
    /// </summary>
    private HashSet<string> SurrenderedBeforeRouts(GameState state)
    {
        if (surrenderedFirst.TryGetValue(state, out var known))
        {
            return known;
        }

        var found = surrenderedFirst.GetValue(state, static _ => new HashSet<string>(StringComparer.Ordinal));
        if (state.Phase != "rtph")
        {
            return found;
        }

        string[] units = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Captured)
            && ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(Is(unit, Conditions.Disrupted), unit.Side == state.PhasingSide,
                () => state.Location(unit.Id)?.Location is { } at && RoutSurrender(state, unit, at) is not null)).Select(unit => unit.Id)];
        found.UnionWith(units);
        return found;
    }

    /// <summary>A20.21 (pass 35, task 35.17): what the Rout panel says of a unit that surrenders instead of routing, or null when it routs as any other.</summary>"""),
    ],
    PLAY + "GamePlanner.Map.cs": [
        # Fix 8.
        ("""    /// either direction. The step and the LOS are read here when Rules asks, and Rules decides.
    /// </summary>
    private bool IsAdjacent(GameState state, BoardLocation one, BoardLocation two) =>
        one != two && ScenarioA1MovementCalculator.IsAdjacent(one.Board == two.Board && one.Hex == two.Hex,
            () => InfantryStep(state, one, two).Entry is not null || InfantryStep(state, two, one).Entry is not null,
            () => Los(state, one, two) is { Status: LosStatus.Clear });""",
         """    /// either direction. The step and the LOS are read here when Rules asks, and Rules decides. The step is one an advance may make: a marsh
    /// hex is never entered in the APh (B16.4, p. 130; the referee's review), which Rules answers for the entry read.
    /// </summary>
    private bool IsAdjacent(GameState state, BoardLocation one, BoardLocation two) =>
        one != two && ScenarioA1MovementCalculator.IsAdjacent(one.Board == two.Board && one.Hex == two.Hex,
            () => AdvanceStep(state, one, two) || AdvanceStep(state, two, one),
            () => Los(state, one, two) is { Status: LosStatus.Clear });

    /// <summary>Whether Infantry could advance from one Location into the other (A.8, A4.7, B16.4), enemy presence ignored.</summary>
    private bool AdvanceStep(GameState state, BoardLocation from, BoardLocation to) =>
        InfantryStep(state, from, to).Entry is { } entry && ScenarioA1AdvanceCalculator.MarshBar(entry.AllMf) is null;"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        ("""Rules.ScenarioA1RoutRallyProjection.RoutHalfMfAsRecorded(unit.Kind is "asl:leader" or "asl:hero",""",
         """Rules.ScenarioA1RoutCalculator.RoutHalfMf(unit.Kind is "asl:leader" or "asl:hero","""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1RoutRallyRulesTests.cs": [
        ("""    public void TheRoutMfOfAWoundedSmcDiffersBetweenThePlannerAndTheProjector()
    {
        Assert.Equal(6, ScenarioA1RoutCalculator.RoutHalfMfAsPlanned(true, true));
        Assert.Equal(12, ScenarioA1RoutCalculator.RoutHalfMfAsPlanned(true, false));
        Assert.Equal(6, ScenarioA1RoutRallyProjection.RoutHalfMfAsRecorded(true, true));
        Assert.Equal(12, ScenarioA1RoutRallyProjection.RoutHalfMfAsRecorded(false, true));
    }""",
         """    public void TheRoutMfOfAWoundedSmcIsOneMemberForThePlannerAndTheProjector()
    {
        Assert.Equal(6, ScenarioA1RoutCalculator.RoutHalfMf(true, true));
        Assert.Equal(12, ScenarioA1RoutCalculator.RoutHalfMf(true, false));
        Assert.Equal(12, ScenarioA1RoutCalculator.RoutHalfMf(false, true));
    }

    [Fact]
    public void AUnitThatHasSurrenderedBeforeTheOtherSideRoutsIsNoEnemyToItsRoutes()
    {
        // A19.12 (p. 86) and the Comprehensive Rout Example (p. 69): a Disrupted unit surrenders at the start of the RtPh, and an ATTACKER's unit
        // among the ATTACKER's routs, so both are prisoners when the other side routs; a DEFENDER's unit that is not Disrupted still stands
        // while the ATTACKER routs. Whether the unit is bound to surrender is read last.
        Assert.True(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: true, attacker: false, () => true));
        Assert.True(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: false, attacker: true, () => true));
        Assert.False(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: true, attacker: true, () => false));
        Assert.False(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: false, attacker: false, () => throw new InvalidOperationException("not read")));
    }"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
