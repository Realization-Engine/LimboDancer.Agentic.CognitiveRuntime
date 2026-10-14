"""Pass 45, task 45.10: a CX leader lends no IPC (A4.42, A4.52); a TI unit and the SW and Deployment actions (A.7, A4.8; question F8)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1MovementCalculator.cs": [
        ("""    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden, string? chosen = null)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(laden);
""",
         """    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden, string? chosen = null,
        bool doubleTime = false)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(laden);

        // A4.52 (p. 51; pass 45): "CX Infantry have an IPC one < normal." A leader's IPC is one, so a leader who Double Times with his stack has
        // none to lend; the page's example gives a leader's squad five MF, not six, when the leader's own IPC is used up.
        if (doubleTime)
        {
            return (null, null);
        }

"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1Definitions.cs": [
        ("""    public static bool FreeToActAsPlanned(bool goodOrder, bool ti) => goodOrder && !ti;
""",
         """    public static bool FreeToActAsPlanned(bool goodOrder, bool ti) => goodOrder && !ti;

    /// <summary>
    /// Whether a TI unit may take a SW or Deployment action (A.7, p. 43; A4.8, p. 52; pass 45, ruling R45.6): "A unit can be pinned, CX, TI, and/or
    /// unarmed and still be considered in 'Good Order'", and "A TI unit may not move, advance, fire, or perform other labor tasks". Deployment,
    /// Recombining, a Transfer, a Drop, and Recovery in the RPh spend no MF and are no labor task, so Good Order is enough; Recovery in the MPh
    /// costs a MF, which a TI unit may not spend (<paramref name="spendsMf"/>).
    /// </summary>
    public static bool FreeToAct(bool goodOrder, bool ti, bool spendsMf) => goodOrder && !(ti && spendsMf);
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Terrain.cs": [
        ("""    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, UnitInstance[] movers, string? chosen = null) =>
        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]), chosen);
""",
         """    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, UnitInstance[] movers, string? chosen = null, bool doubleTime = false) =>
        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]), chosen,
            doubleTime || movers.Any(unit => unit.DoubleTimeMf > 0 || Is(unit, Conditions.Cx)));
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs": [
        ("""        var leaderIpcTo = LeaderIpcRecipient(state, movers, ipcChosen ?? current?.IpcTo);
""",
         """        var leaderIpcTo = LeaderIpcRecipient(state, movers, ipcChosen ?? current?.IpcTo, doubleTime);
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs": [
        ("""    /// <summary>Whether a unit may take a SW or Deployment action: in Good Order and not TI, as the planner had it before A.7 was read into Good Order.</summary>
    private static bool FreeToAct(UnitInstance unit) => ScenarioA1Definitions.FreeToActAsPlanned(GoodOrder(unit), Is(unit, "asl:ti"));
""",
         """    /// <summary>
    /// Whether a unit may take a SW or Deployment action (pass 45, ruling R45.6): in Good Order; a TI unit only when the action spends no MF.
    /// </summary>
    private static bool FreeToAct(UnitInstance unit, bool spendsMf = false) => ScenarioA1Definitions.FreeToAct(GoodOrder(unit), Is(unit, "asl:ti"), spendsMf);
"""),
        ("""        if (state.Unit(unitId) is not { } unit || !FreeToAct(unit) || Is(unit, Conditions.Pinned) || LiveFire.IsVehicle(unit) || state.Location(unit.Id)?.Location is not { } at)
""",
         """        if (state.Unit(unitId) is not { } unit || !FreeToAct(unit, spendsMf: state.Phase == "mph") || Is(unit, Conditions.Pinned) || LiveFire.IsVehicle(unit)
            || state.Location(unit.Id)?.Location is not { } at)
"""),
        # Dismantling counts as the weapon's use, as fire does, which a TI unit may not do: its bar stays.
        ("""        if (state.Unit(unitId) is not { } unit || !FreeToAct(unit) || Held(state, unit.Id).FirstOrDefault(item => item.Id == equipmentId) is not { } weapon)
""",
         """        if (state.Unit(unitId) is not { } unit || !FreeToAct(unit, spendsMf: true) || Held(state, unit.Id).FirstOrDefault(item => item.Id == equipmentId) is not { } weapon)
"""),
    ],
}
texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
