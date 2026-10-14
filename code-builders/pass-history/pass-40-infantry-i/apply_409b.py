"""Pass 40 task 40.9, second part: one member for the US# (A1.6, p. 45), with the 4 and the 5. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1Definitions.cs", [
    ("    public static bool GoodOrderOf(bool active, bool broken, bool berserk, bool melee, bool captured, bool stunned, bool shocked) =>",
     "    /// <summary>\n"
     "    /// A1.6 (p. 45; pass 40): the Unit Size Number. \"Personnel units have a US# equal to the number of silhouettes on their counter (SMC: 1;\n"
     "    /// Crew/HS: 2; Squad: 3). Horses and all other 5/8\\\" counters have a US# of 4 ... [EXC: Any vehicle or Gun classified as large or very large\n"
     "    /// (red H, AF, or M#) has a US# of 5]\". The one member for it: Concealment (A12.12), Guarding prisoners (A20.5), and CC read it here.\n"
     "    /// </summary>\n"
     "    public static int UnitSize(bool squad, bool halfSquadOrCrew, bool fiveEighths = false, bool large = false) =>\n"
     "        fiveEighths ? (large ? 5 : 4) : squad ? 3 : halfSquadOrCrew ? 2 : 1;\n\n"
     "    /// <summary>The US# of a Personnel unit by its kind (A1.6).</summary>\n"
     "    public static int UnitSize(string? kind) => UnitSize(kind == \"asl:squad\", kind is \"asl:half-squad\" or \"asl:crew\");\n\n"
     "    public static bool GoodOrderOf(bool active, bool broken, bool berserk, bool melee, bool captured, bool stunned, bool shocked) =>"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1PrisonerCalculator.cs", [
    ("    /// <summary>A1.6 (p. 45), A20.51: a unit's US#: a squad 3, a HS or crew 2, a SMC 1.</summary>\n"
     "    public static int UnitSize(bool squad, bool halfSquadOrCrew) => squad ? 3 : halfSquadOrCrew ? 2 : 1;\n\n", ""),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1CloseCombatCalculator.cs", [
    ("    private static int Size(FireDefinition definition) => definition.Kind == \"asl:squad\" ? 3 : definition.Kind is \"asl:half-squad\" or \"asl:crew\" ? 2 : 1;",
     "    private static int Size(FireDefinition definition) => ScenarioA1Definitions.UnitSize(definition.Kind);"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs", [
    ("    public static int GuardSize(string kind) => kind == \"asl:squad\" ? 3 : kind is \"asl:half-squad\" or \"asl:crew\" ? 2 : 1;",
     "    public static int GuardSize(string kind) => ScenarioA1Definitions.UnitSize(kind);"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs", [
    ("            var size = unit.Squad ? 3 : unit.HalfSquad || unit.Kind == \"asl:crew\" ? 2 : 1;",
     "            var size = ScenarioA1Definitions.UnitSize(unit.Squad, unit.HalfSquad || unit.Kind == \"asl:crew\");"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.CloseCombat.cs", [
    ("    private int UnitSize(UnitInstance unit) => ScenarioA1PrisonerCalculator.UnitSize(vocabulary.IsA(unit.Kind, \"asl:squad\"), vocabulary.IsA(unit.Kind, \"asl:half-squad\") || vocabulary.IsA(unit.Kind, \"asl:crew\"));",
     "    private int UnitSize(UnitInstance unit) => ScenarioA1Definitions.UnitSize(vocabulary.IsA(unit.Kind, \"asl:squad\"),\n"
     "        vocabulary.IsA(unit.Kind, \"asl:half-squad\") || vocabulary.IsA(unit.Kind, \"asl:crew\"), LiveFire.IsVehicle(unit));"),
])
apply("LimboDancer.Domains.Asl.Units/State/GameProjector.cs", [
    ("            static int Size(UnitInstance unit) => Rules.ScenarioA1PrisonerCalculator.UnitSize(unit.Kind == \"asl:squad\", unit.Kind is \"asl:half-squad\" or \"asl:crew\");",
     "            static int Size(UnitInstance unit) => Rules.ScenarioA1Definitions.UnitSize(unit.Kind);"),
])
print("ok")
