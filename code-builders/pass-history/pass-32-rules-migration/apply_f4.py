"""Pass 32.f commit 4: PlanRout's Build and words, FailureToRout, the freed SMC, and the arming predicate call Rules."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
ROUT = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Rout.cs"
FREED = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.FreedSmc.cs"
CALC = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RoutCalculator.cs"
MODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RoutModels.cs"
DRAFT = pathlib.Path(r"E:\Archive\GitHub\dlandi\pass32-tools\draft_f4_rules.cs")


def read(p):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    return t


def write(p, t):
    p.write_bytes(t.encode("utf-8"))


def apply(p, edits):
    t = read(p)
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:70])
        t = t.replace(old, new)
    write(p, t)
    print("applied", p.name, len(edits))


# Rules: the draft appended inside the class's last brace, CultureInfo imported, the outcome enum added.
calc = read(CALC)
assert calc.endswith("}\n") and calc.count("\n}\n") == 1
draft = read(DRAFT)
assert draft.endswith("}\n")
calc = "using System.Globalization;\n\n" + calc[: -len("}\n")] + draft
write(CALC, calc)
print("calculator appended")

apply(MODELS, [(
    "/// <summary>What Casualty Reduction does to a unit (A7.302)",
    "/// <summary>What an Interdiction result does to the routing unit (A10.53, A10.31): it routs on, is pinned, is Casualty Reduced, or is eliminated.</summary>\n"
    "public enum InterdictionOutcome\n{\n    Passed,\n    Pinned,\n    Reduced,\n    Eliminated,\n}\n\n"
    "/// <summary>What Casualty Reduction does to a unit (A7.302)",
)])

apply(ROUT, [
    (
        "                if (used + costs[index] > RoutHalfMf(routing))\n",
        "                if (ScenarioA1RoutCalculator.StepExceedsMf(used, costs[index], RoutHalfMf(routing)))\n",
    ),
    (
        "                if (lowCrawl || route.IndexOf(to) != index || Interdictor(after, routing.Side, to) is not { } interdictor)\n",
        "                if (ScenarioA1RoutCalculator.InterdictionDue(lowCrawl, route.IndexOf(to) == index, () => Interdictor(after, routing.Side, to)) is not { } interdictor)\n",
    ),
    (
        "                var morale = BrokenMorale(current) ?? 0;\n",
        "                var morale = ScenarioA1RoutCalculator.InterdictionMorale(BrokenMorale(current));\n",
    ),
    (
        "                if (result == RoutInterdicted.Passed)\n"
        "                {\n"
        "                    continue;\n"
        "                }\n"
        "\n"
        "                if (result == RoutInterdicted.Pinned)\n",
        "                var outcome = ScenarioA1RoutCalculator.InterdictionEffect(result);\n"
        "                if (outcome == InterdictionOutcome.Passed)\n"
        "                {\n"
        "                    continue;\n"
        "                }\n"
        "\n"
        "                if (outcome == InterdictionOutcome.Pinned)\n",
    ),
    (
        "                var (type, payload) = result == RoutInterdicted.Reduced ? CasualtyReduction(current, attemptId) : (\"instance-eliminated\", new InstanceEliminated(routing.Id));\n",
        "                var (type, payload) = outcome == InterdictionOutcome.Reduced ? CasualtyReduction(current, attemptId) : (\"instance-eliminated\", new InstanceEliminated(routing.Id));\n",
    ),
    (
        "                // A10.53: a HS left by Casualty Reduction, or a wounded SMC, routs on; an eliminated unit does not.\n"
        "                if (payload is InstanceEliminated)\n",
        "                // A10.53: a HS left by Casualty Reduction, or a wounded SMC, routs on; an eliminated unit does not.\n"
        "                if (!ScenarioA1RoutCalculator.RoutsOn(payload is InstanceEliminated))\n",
    ),
    (
        "        var steps = string.Join(\", \", route.Select((step, index) => $\"{step} ({(costs[index] / 2.0).ToString(\"0.#\", CultureInfo.InvariantCulture)} MF)\"));\n"
        "        string[] threatened = lowCrawl ? [] : [.. route.Distinct().Select(step => (step, By: Interdictor(state, unit.Side, step))).Where(item => item.By is not null)\n"
        "            .Select(item => $\"{item.step} by {item.By}\")];\n"
        "        var interdiction = lowCrawl ? \"Low Crawl is never Interdicted (A10.52)\"\n"
        "            : threatened.Length == 0 ? \"no step enters Open Ground an enemy unit could Interdict (A10.53)\"\n"
        "            : $\"Interdicted as it enters {string.Join(\", \", threatened)}, a NMC each (A10.53)\";\n"
        "        string[] leaves = left.Count == 0 ? []\n"
        "            : [$\"play.rout-leaves: {unit.Id} leaves {string.Join(\" and \", left.Select(item => $\"{item.Weapon} ({item.Pp} PP)\"))} in {start}, unpossessed, and routs with \"\n"
        "                + $\"{(kept.Count == 0 ? \"no SW\" : string.Join(\" and \", kept))} (A10.4, A4.431)\"];\n"
        "        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$\"play.rout: {unit.Id} routs{(lowCrawl ? \" by Low Crawl\" : \"\")} to {steps}; {interdiction}\", .. leaves])\n",
        "        // The Interdictor of each distinct step is read only for a rout that is not a Low Crawl (A10.52); Rules words the plan (pass 32.f).\n"
        "        (string Step, string By)[] threatened = lowCrawl ? [] : [.. route.Distinct().Select(step => (Step: step.ToString(), By: Interdictor(state, unit.Side, step)))\n"
        "            .Where(item => item.By is not null).Select(item => (item.Step, item.By!))];\n"
        "        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], ScenarioA1RoutCalculator.RoutSummary(unit.Id, lowCrawl,\n"
        "            [.. route.Select((step, index) => (step.ToString(), costs[index]))], threatened, [.. left.Select(item => (item.Weapon, item.Pp))], kept, start.ToString()))\n",
    ),
    (
        "        if (state.Night)\n"
        "        {\n"
        "            return [];\n"
        "        }\n"
        "\n"
        "        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;\n",
        "        if (ScenarioA1RoutCalculator.NoFailureToRout(state.Night))\n"
        "        {\n"
        "            return [];\n"
        "        }\n"
        "\n"
        "        var scan = new RoutScan(this, state);\n"
        "        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;\n",
    ),
    (
        "        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Melee)\n"
        "            && !Is(unit, Conditions.Captured) && !LiveFire.IsVehicle(unit) && state.Aboard(unit.Id) is null).OrderBy(unit => unit.Id, StringComparer.Ordinal))\n",
        "        foreach (var unit in state.Units.Where(unit => ScenarioA1RoutCalculator.FailureToRoutCandidate(unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken),\n"
        "            Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), LiveFire.IsVehicle(unit), state.Aboard(unit.Id) is not null)).OrderBy(unit => unit.Id, StringComparer.Ordinal))\n",
    ),
    (
        "            var why = NearUnbrokenArmedEnemy(state, unit.Side, at) is { } enemy ? $\"it is ADJACENT to or in the Location of the Known unbroken armed enemy unit {enemy}\"\n"
        "                : !state.RoutedThisPhase.Contains(unit.Id) && !Is(unit, Conditions.Pinned) && ExposedInOpenGround(state, unit.Side, at) is { } seen\n"
        "                    ? $\"it did not rout from Open Ground in the LOS and Normal Range of {seen}\" : null;\n",
        "            var why = ScenarioA1RoutCalculator.FailureToRoutWhy(scan, scan.Enemies(unit.Side), scan.Index(at), state.RoutedThisPhase.Contains(unit.Id), Is(unit, Conditions.Pinned),\n"
        "                state.ScenarioMonth);\n",
    ),
    (
        "            var surrenders = !Is(unit, Conditions.Fanatic) && !state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) && !rejected.Contains(unit.Id)\n"
        "                && Captors(state, unit) is { Count: > 0 } captors ? captors : null;\n",
        "            var surrenders = ScenarioA1RoutCalculator.SurrendersInstead(Is(unit, Conditions.Fanatic), state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal), rejected.Contains(unit.Id))\n"
        "                && Captors(state, unit) is { Count: > 0 } captors ? captors : null;\n",
    ),
])

apply(FREED, [
    (
        "using LimboDancer.Domains.Asl.Units.State;\n",
        "using LimboDancer.Domains.Asl.Rules;\nusing LimboDancer.Domains.Asl.Units.State;\n",
    ),
    (
        "        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is null && vocabulary.IsA(unit.Kind, \"asl:smc\")\n"
        "            && Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Captured)).OrderBy(unit => unit.Id, StringComparer.Ordinal)];\n",
        "        [.. state.Units.Where(unit => ScenarioA1RoutCalculator.FreedUnarmedSmc(unit.Status == InstanceStatus.Active, unit.Custodian is null, vocabulary.IsA(unit.Kind, \"asl:smc\"),\n"
        "            Is(unit, Conditions.Unarmed), Is(unit, Conditions.Captured))).OrderBy(unit => unit.Id, StringComparer.Ordinal)];\n",
    ),
    (
        "                        if (events.Count > 0 && events[^1].Payload is not (GameEnded or ChoicePending or SurrenderPending) && Replay([.. existing, .. events]) is { HasErrors: false, Current: { } rolled })\n",
        "                        if (ScenarioA1RoutCalculator.ArmsFreedSmc(events.Count, events.Count > 0 && events[^1].Payload is GameEnded or ChoicePending or SurrenderPending)\n"
        "                            && Replay([.. existing, .. events]) is { HasErrors: false, Current: { } rolled })\n",
    ),
    (
        "        if (plan.Events.Count == 0 || plan.Events[^1].Payload is GameEnded or ChoicePending or SurrenderPending || Replay([.. existing, .. plan.Events]) is not { HasErrors: false, Current: { } after }\n"
        "            || FreedUnarmedSmc(after) is not { Length: > 0 } freed)\n",
        "        if (!ScenarioA1RoutCalculator.ArmsFreedSmc(plan.Events.Count, plan.Events.Count > 0 && plan.Events[^1].Payload is GameEnded or ChoicePending or SurrenderPending)\n"
        "            || Replay([.. existing, .. plan.Events]) is not { HasErrors: false, Current: { } after } || FreedUnarmedSmc(after) is not { Length: > 0 } freed)\n",
    ),
    (
        "            Reasons = [.. plan.Reasons, $\"play.smc-armed: {string.Join(\", \", freed.Select(unit => unit.Id))} {(freed.Length == 1 ? \"is\" : \"are\")} free and so Armed again (A20.551; ruling R31.8)\"],\n",
        "            Reasons = [.. plan.Reasons, ScenarioA1RoutCalculator.SmcArmedReason([.. freed.Select(unit => unit.Id)])],\n",
    ),
])
