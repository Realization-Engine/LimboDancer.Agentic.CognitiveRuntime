"""Pass 35 review fix 2: Interdiction against a unit routing up across the Crest Line the Interdictor's LOS crosses (B1.14, B10.31).
Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"
EDITS = {
    RULES + "ScenarioA1RoutCalculator.cs": [
        ("""    /// that enemy: a wall or hedge on the hexside crossed, Height Advantage, or a wreck or AFV in the Location. The cover is read only for a clear LOS in range.
    /// </summary>
    public static bool CouldApplyFfmo(IRoutFactReader reader, int enemyLocation, int at, int range)""",
         """    /// that enemy: a wall or hedge on the hexside crossed, Height Advantage, or a wreck or AFV in the Location. The cover is read only for a clear LOS in range.
    /// A unit entering from <paramref name="steppedFrom"/> has no Height Advantage against a LOS that crosses the Crest Line hexside it climbs (B1.14, p. 113).
    /// </summary>
    public static bool CouldApplyFfmo(IRoutFactReader reader, int enemyLocation, int at, int range, int? steppedFrom = null)"""),
        ("        var cover = reader.Cover(enemyLocation, at);\n", "        var cover = reader.Cover(enemyLocation, at, steppedFrom);\n"),
        ("""    public static string? Interdictor(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth)""",
         """    public static string? Interdictor(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth, int? steppedFrom = null)"""),
        ("""(enemy.InterdictionRange ?? enemy.NormalRange) is > 0 and var range && CouldApplyFfmo(reader, enemy.Location, at, range))
            {
                return enemy.Id;""",
         """(enemy.InterdictionRange ?? enemy.NormalRange) is > 0 and var range && CouldApplyFfmo(reader, enemy.Location, at, range, steppedFrom))
            {
                return enemy.Id;"""),
        ("        var interdicted = new Dictionary<int, bool>();\n", "        var interdicted = new Dictionary<(int, int), bool>();\n"),
        ("""(interdicted.TryGetValue(next, out var open) ? open : interdicted[next] = Interdictor(reader, enemies, next, scenarioMonth) is not null))""",
         """(interdicted.TryGetValue((node.At, next), out var open) ? open
                    : interdicted[(node.At, next)] = Interdictor(reader, enemies, next, scenarioMonth, node.At) is not null))"""),
    ],
    RULES + "ScenarioA1MovementModels.cs": [(
        """/// The facts of Height Advantage (B10.31; ruling R10.4): the target hex's base level and Location level, each firer's level, whether the moving stack is in
/// the target Location in the MPh with the base level of the hex it left and the target's hexside toward it (null when unread or not adjacent), and the""",
        """/// The facts of Height Advantage (B10.31; ruling R10.4): the target hex's base level and Location level, each firer's level, whether the moving stack is in
/// the target Location in the MPh (or a routing unit is entering it, B1.14) with the base level of the hex it left and the target's hexside toward it (null when unread or not adjacent), and the"""),
    ],
    PLAY + "GamePlanner.Terrain.cs": [(
        """    private bool HeightAdvantageAt(GameState state, BoardLocation target, LocationRead targetRead, IReadOnlyList<(BoardLocation From, int Level)> firers, bool snapShot)
    {
        // Pass 32.b: the moving stack's origin and the LOS entry hexsides are read here, the latter only when Rules asks, and Rules decides.
        var movement = state.Movement;
        var left = movement?.From;
        var facts = new HeightAdvantageFacts(targetRead.Hex.BaseLevel, target.Level, [.. firers.Select(item => item.Level)], state.Phase == "mph",
            left is not null && movement!.Location == target, left is null ? null : ReadLocation(state, left)?.Hex.BaseLevel,""",
        """    private bool HeightAdvantageAt(GameState state, BoardLocation target, LocationRead targetRead, IReadOnlyList<(BoardLocation From, int Level)> firers, bool snapShot,
        BoardLocation? routStepFrom = null)
    {
        // Pass 32.b: the moving stack's origin and the LOS entry hexsides are read here, the latter only when Rules asks, and Rules decides.
        // B1.14 (pass 35, the referee's review): a routing unit's step into the Location is handed over as a mover's is.
        var movement = state.Movement;
        var left = routStepFrom ?? movement?.From;
        var facts = new HeightAdvantageFacts(targetRead.Hex.BaseLevel, target.Level, [.. firers.Select(item => item.Level)], routStepFrom is not null || state.Phase == "mph",
            routStepFrom is not null || (left is not null && movement!.Location == target), left is null ? null : ReadLocation(state, left)?.Hex.BaseLevel,"""),
    ],
    PLAY + "GamePlanner.Rout.cs": [
        ("        private readonly Dictionary<(int, int), RoutCoverFacts> covers = [];\n", "        private readonly Dictionary<(int, int, int?), RoutCoverFacts> covers = [];\n"),
        ("""        public RoutCoverFacts Cover(int enemyLocation, int location) =>
            covers.TryGetValue((enemyLocation, location), out var cover) ? cover
                : covers[(enemyLocation, location)] = planner.InterdictionCover(state, locations[enemyLocation], locations[location], routingSide);""",
         """        public RoutCoverFacts Cover(int enemyLocation, int location, int? steppedFrom = null) =>
            covers.TryGetValue((enemyLocation, location, steppedFrom), out var cover) ? cover
                : covers[(enemyLocation, location, steppedFrom)] = planner.InterdictionCover(state, locations[enemyLocation], locations[location], routingSide,
                    steppedFrom is { } left ? locations[left] : null);"""),
        ("""    /// whose TEM fire cannot decide gives none here, as before the pass.
    /// </summary>
    private RoutCoverFacts InterdictionCover(GameState state, BoardLocation from, BoardLocation at, string? routingSide)""",
         """    /// whose TEM fire cannot decide gives none here, as before the pass. The Location the routing unit steps in from is handed to the Height
    /// Advantage read (B1.14, p. 113).
    /// </summary>
    private RoutCoverFacts InterdictionCover(GameState state, BoardLocation from, BoardLocation at, string? routingSide, BoardLocation? steppedFrom = null)"""),
        ("HeightAdvantageAt(state, at, atRead, [(from, height)], false), CoverAt", "HeightAdvantageAt(state, at, atRead, [(from, height)], false, steppedFrom), CoverAt"),
        ("""    private string? Interdictor(GameState state, string side, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.Interdictor(scan, scan.Enemies(side), scan.Index(at), state.ScenarioMonth);""",
         """    private string? Interdictor(GameState state, string side, BoardLocation at, BoardLocation steppedFrom)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.Interdictor(scan, scan.Enemies(side), scan.Index(at), state.ScenarioMonth, scan.Index(steppedFrom));"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
