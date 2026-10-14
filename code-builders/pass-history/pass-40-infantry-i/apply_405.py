"""Pass 40 task 40.5: the forced TPBF of A8.312, a crew's inherent FP in FPF (A8.311), and the unread Broken fact of an enemy scan. Exact anchors."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


# The fire plan keeps its body under a new name; the action's entry is the new PlanFire of GamePlanner.MandatoryFire.cs.
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs", [
    ("    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,\n        string actor)",
     "    private GamePlan PlanFireOf(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,\n        string actor)"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.MandatoryFire.cs", [
    ("    private JsonElement MandatoryArguments(GameState state, IReadOnlyList<string> firers, BoardLocation at)",
     "    private Dictionary<string, object> MandatoryArguments(GameState state, IReadOnlyList<string> firers, BoardLocation at)"),
    ("        return JsonSerializer.SerializeToElement(new Dictionary<string, object>\n        {\n            [\"firers\"] = firers, [\"target\"] = at.ToString(), [\"weapons\"] = weapons, [\"withoutInherent\"] = alone,\n        });",
     "        return new Dictionary<string, object>\n        {\n            [\"firers\"] = firers, [\"target\"] = at.ToString(), [\"weapons\"] = weapons, [\"withoutInherent\"] = alone,\n        };"),
    ("    public JsonElement MandatoryFireArguments(GameState state, MandatoryFireOwed owed)", "    public Dictionary<string, object> MandatoryFireArguments(GameState state, MandatoryFireOwed owed)"),
    ("            && PlanFireOf(scope, MandatoryArguments(state, firers, at), existing,", "            && PlanFireOf(scope, JsonSerializer.SerializeToElement(MandatoryArguments(state, firers, at)), existing,"),
])

# The DEFENDER's pass is refused while the attack is owed.
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs", [
    ("        // Pass 32.b: the moving stack is read here, and Rules decides.\n        var movement = state.Movement;\n",
     "        // Pass 40 (A8.312): no pass while the DEFENDER owes its attack on the units that entered its Location.\n"
     "        if (MandatoryFire(state, existing) is { } owed && ScenarioA1MandatoryFire.PassBar(owed.Firers, owed.At.ToString()) is { } mandatoryBar)\n"
     "        {\n"
     "            return Refused(scope, label, expected, mandatoryBar);\n"
     "        }\n\n"
     "        // Pass 32.b: the moving stack is read here, and Rules decides.\n        var movement = state.Movement;\n"),
])

# A8.311: a crew that fired its Gun adds its inherent FP to FPF.
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs", [
    ("            // A7.352 (ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP.\n"
     "            || firers.Any(item => item.GunFired == true && item.UsesInherentFp != false))",
     "            // A7.352 (ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP, but in FPF, where it \"must add\" it (A8.311; pass 40,\n"
     "            // ruling R40.18).\n"
     "            || firers.Any(item => item.UsesInherentFp != false\n"
     "                && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true, attack.FireKind == FinalProtectiveFire, item.FinalFireMarked == true)))"),
])

# The enemy scan's Broken fact, which nothing reads since pass 35 (Good Order is the fact read).
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementModels.cs", [
    ("public sealed record EnemyUnitFacts(bool Active, string? Side, bool Dummy, bool Aboard, bool? Broken, bool Hidden, bool GoodOrder, int? Location);",
     "public sealed record EnemyUnitFacts(bool Active, string? Side, bool Dummy, bool Aboard, bool Hidden, bool GoodOrder, int? Location);"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Map.cs", [
    ("state.Aboard(unit.Id) is not null,\n                GameState.Condition(unit, Conditions.Broken) switch { ConditionState.True => true, ConditionState.False => false, _ => null },\n                Is(unit, Conditions.Hidden),",
     "state.Aboard(unit.Id) is not null,\n                Is(unit, Conditions.Hidden),"),
])
apply("tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass35RulesTests.cs", [
    ("bool aboard = false, bool? broken = false, bool dummy = false) =>\n        new(true, \"german\", dummy, aboard, broken, hidden, goodOrder, at);",
     "bool aboard = false, bool dummy = false) =>\n        new(true, \"german\", dummy, aboard, hidden, goodOrder, at);"),
])

# The page: the DEFENDER's note and the Mandatory FG's button, and no pass button while the attack is owed.
apply("LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor", [
    ("                            <button id=\"propose-pass\" @onclick='@(() => ProposeSimple(GameActions.PassFire, \"pass\"))'>Propose: the DEFENDER passes</button>\n",
     "                            @if (Live.Planner.MandatoryFire(state, history!.Events) is { } mandatory)\n"
     "                            {\n"
     "                                @* Pass 40 (A8.312): the attack owed in the DEFENDER's own Location comes before a pass. *@\n"
     "                                <span class=\"toolbar-note\" id=\"mandatory-fire-note\">@ScenarioA1MandatoryFire.Note(mandatory.Firers, PlaceOf(mandatory.At))</span>\n"
     "                                <button id=\"propose-mandatory-fire\" title=\"Each unit fires its inherent FP, and a unit with none its MG. To add a MG or a leader's direction, name the same units in the fire panel below.\" @onclick=\"@(() => ProposeMandatoryFire(mandatory))\">Propose: the Mandatory FG attacks</button>\n"
     "                            }\n"
     "                            else\n"
     "                            {\n"
     "                                <button id=\"propose-pass\" @onclick='@(() => ProposeSimple(GameActions.PassFire, \"pass\"))'>Propose: the DEFENDER passes</button>\n"
     "                            }\n"),
    ("    /// <summary>The DEFENDER's SMC moves away from an Infantry OVR (A4.151; pass 40); the ATTACKER is then asked where.</summary>\n",
     "    /// <summary>The attack the DEFENDER owes on the units that entered its Location (A8.312; pass 40), as its plainest Mandatory FG.</summary>\n"
     "    private Task ProposeMandatoryFire(GamePlanner.MandatoryFireOwed owed)\n"
     "    {\n"
     "        var arguments = Live.Planner.MandatoryFireArguments(state!, owed);\n"
     "        arguments[\"gameId\"] = gameId;\n"
     "        arguments[\"attemptId\"] = Attempt(\"fire\");\n"
     "        arguments[\"expectedRevision\"] = (long)history!.Events.Count;\n"
     "        return Propose(GameActions.Fire, JsonSerializer.SerializeToElement(arguments));\n"
     "    }\n\n"
     "    /// <summary>The DEFENDER's SMC moves away from an Infantry OVR (A4.151; pass 40); the ATTACKER is then asked where.</summary>\n"),
])
print("ok")
