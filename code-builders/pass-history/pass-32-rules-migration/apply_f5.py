"""Pass 32.f commit 5: LiveRally's derived facts and Commissar, FirstMmcRallier, PlanRally's blocks, and AddRallyEvents' next step and surrender call Rules."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
LIVE = ROOT / "LimboDancer.Domains.Asl.Play/LiveRally.cs"
RALLY = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Rally.cs"


def read(p):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    return t


def apply(p, edits):
    t = read(p)
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:70])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(LIVE, [
    (
        "        var leaders = state.At(at).OfType<UnitInstance>()\n"
        "            .Where(item => item.Status == InstanceStatus.Active && item.Id != unit.Id && item.Side == unit.Side && item.Kind == \"asl:leader\").ToArray();\n",
        "        var leaders = state.At(at).OfType<UnitInstance>()\n"
        "            .Where(item => ScenarioA1RallyRules.OtherFriendlyLeader(item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Kind == \"asl:leader\")).ToArray();\n",
    ),
    (
        "            .Where(item => item.Status == InstanceStatus.Active && item.Id != unit.Id && item.Side == unit.Side && item.Definition is not null && !Is(item, Conditions.Captured))\n",
        "            .Where(item => ScenarioA1RallyRules.BerserkCompanion(item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Definition is not null,\n"
        "                Is(item, Conditions.Captured)))\n",
    ),
    (
        "        return (new RallyAttempt(\n"
        "            state.Phase == \"rph\" ? \"RPh\" : state.Phase,\n"
        "            unit.Side == state.PhasingSide ? \"phasing\" : \"non-phasing\",\n"
        "            new RallyUnit(unit.Id, unit.Definition.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Disrupted),\n"
        "                Is(unit, Conditions.Wounded), Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Concealed),\n"
        "                state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id) || state.RallyPhaseActions.Contains(unit.Id))\n"
        "            {\n"
        "                Fanatic = Is(unit, Conditions.Fanatic) ? true : null,\n"
        "                // A19.3 (ruling R15.10): a Green MMC's Inexperience, for its Heat of Battle DRM.\n"
        "                Inexperienced = LiveFire.GreenInexperienced(state, unit),\n"
        "            },\n"
        "            leader is null ? null : new RallyLeader(leader.Id, leader.Definition!.Definition, state.Location(leader.Id)?.Location.ToString(),\n"
        "                Is(leader, Conditions.Broken), Is(leader, Conditions.Wounded), Is(leader, Conditions.Concealed)),\n"
        "            at.ToString(),\n"
        "            terrain,\n"
        "            leaders.Any(item => !Is(item, Conditions.Broken)),\n"
        "            leaders.Any(item => Is(item, Conditions.Broken)),\n"
        "            unit.Side == state.PhasingSide && !state.FirstMmcRallyTaken.Contains(unit.Side),\n"
        "            enemyGoodOrderInLosWithin16,\n"
        "            null)\n"
        "        {\n"
        "            KnownEnemyInLos = knownEnemyInLos,\n"
        "            Captors = captors,\n"
        "            Companions = companions.Length > 0 ? companions : null,\n"
        "            NoQuarter = state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) ? true : null,\n"
        "            Commissar = Commissar(state, unit)?.Id,\n"
        "            // E3.742 (backlog pass 16, ruling R16.14): Extreme Winter's Fate outside a building.\n"
        "            ExtremeWinterFate = GamePlanner.ExtremeWinterReduction(state, unit.Side) is not null && terrain is not (\"wooden-building\" or \"stone-building\") ? true : null,\n"
        "        }, null);\n",
        "        // The state's facts cross to Rules, which decides what the attempt declares (pass 32.f); the record keeps its shape.\n"
        "        return (ScenarioA1RallyRules.Attempt(new RallyAttemptStateFacts(\n"
        "            state.Phase,\n"
        "            unit.Side == state.PhasingSide,\n"
        "            new RallyUnitStateFacts(unit.Id, unit.Definition.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Disrupted),\n"
        "                Is(unit, Conditions.Wounded), Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Concealed),\n"
        "                state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id), state.RallyPhaseActions.Contains(unit.Id),\n"
        "                Is(unit, Conditions.Fanatic), LiveFire.GreenInexperienced(state, unit)),\n"
        "            leader is null ? null : new RallyLeaderStateFacts(leader.Id, leader.Definition!.Definition, state.Location(leader.Id)?.Location.ToString(),\n"
        "                Is(leader, Conditions.Broken), Is(leader, Conditions.Wounded), Is(leader, Conditions.Concealed)),\n"
        "            terrain,\n"
        "            [.. leaders.Select(item => Is(item, Conditions.Broken))],\n"
        "            state.FirstMmcRallyTaken.Contains(unit.Side),\n"
        "            enemyGoodOrderInLosWithin16,\n"
        "            knownEnemyInLos,\n"
        "            captors,\n"
        "            companions,\n"
        "            state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal),\n"
        "            Commissar(state, unit)?.Id,\n"
        "            GamePlanner.ExtremeWinterReduction(state, unit.Side) is not null)), null);\n",
    ),
    (
        "        return state.Location(unit.Id) is not { } at ? null : state.At(at.Location).OfType<UnitInstance>().FirstOrDefault(item => item.Status == InstanceStatus.Active\n"
        "            && item.Id != unit.Id && item.Side == unit.Side && item.Definition is { } reference && ScenarioA1FireReference.IsCommissar(reference.Definition)\n"
        "            && !Is(item, Conditions.Broken) && !Is(item, Conditions.Pinned) && !Is(item, Conditions.Berserk) && !Is(item, Conditions.Captured));\n",
        "        return state.Location(unit.Id) is not { } at ? null : state.At(at.Location).OfType<UnitInstance>().FirstOrDefault(item => ScenarioA1RallyRules.Commissar(\n"
        "            item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Definition is { } reference && ScenarioA1FireReference.IsCommissar(reference.Definition),\n"
        "            Is(item, Conditions.Broken), Is(item, Conditions.Pinned), Is(item, Conditions.Berserk), Is(item, Conditions.Captured)));\n",
    ),
])

apply(RALLY, [
    (
        "            .LastOrDefault(item => state.Unit(item.Unit) is { } rallied && rallied.Side == side && rallied.Kind is \"asl:squad\" or \"asl:half-squad\" or \"asl:crew\")?.Unit;\n",
        "            .LastOrDefault(item => state.Unit(item.Unit) is { } rallied && ScenarioA1RallyRules.IsMmcRallier(rallied.Side == side, rallied.Kind))?.Unit;\n",
    ),
    (
        "        if (leaderId is not null && state.RallyPhaseActions.Contains(leaderId))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.rph-action: {leaderId} directed a Deployment or permitted a Recombination this RPh, his sole RPh action (A1.31, A1.32)\");\n"
        "        }\n"
        "        if (state.Unit(unitId) is not { } unit || state.Location(unit.Id)?.Location is not { } at || ReadLocation(state, at) is not { } read)\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.rally-unit: '{unitId}' is not a unit on a Location the map reads\");\n"
        "        }\n",
        "        if (ScenarioA1RallyRules.LeaderRallyPhaseActionBar(leaderId, leaderId is not null && state.RallyPhaseActions.Contains(leaderId)) is { } actionBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, actionBar);\n"
        "        }\n"
        "        if (state.Unit(unitId) is not { } unit || state.Location(unit.Id)?.Location is not { } at || ReadLocation(state, at) is not { } read)\n"
        "        {\n"
        "            return Refused(scope, label, expected, ScenarioA1RallyRules.RallyUnitUnreadText(unitId));\n"
        "        }\n",
    ),
    (
        "        if (leaderId is not null && state.Unit(leaderId) is { } rallying && rallying.Side != unit.Side)\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.rally-leader: {leaderId} is not of {unitId}'s side (A10.6)\");\n"
        "        }\n",
        "        if (ScenarioA1RallyRules.LeaderSideBar(leaderId, unitId, leaderId is not null && state.Unit(leaderId) is { } rallying && rallying.Side != unit.Side) is { } sideBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, sideBar);\n"
        "        }\n",
    ),
    (
        "        var concealed = GameState.Condition(unit, Conditions.Concealed) == ConditionState.True\n"
        "            || (leaderId is not null && state.Unit(leaderId) is { } leaderUnit && GameState.Condition(leaderUnit, Conditions.Concealed) == ConditionState.True);\n",
        "        var concealed = ScenarioA1RallyRules.ConcealedAttempt(GameState.Condition(unit, Conditions.Concealed) == ConditionState.True,\n"
        "            leaderId is not null && state.Unit(leaderId) is { } leaderUnit && GameState.Condition(leaderUnit, Conditions.Concealed) == ConditionState.True);\n",
    ),
    (
        "        var heat = leaderId is not null;\n",
        "        var heat = ScenarioA1RallyRules.ReachesHeatOfBattle(leaderId);\n",
    ),
    (
        "            string[] why = precheck.Contains(\"asl.a1.rally.self-rally-not-capable\")\n"
        "                ? [unit.Side != state.PhasingSide\n"
        "                    ? $\"play.rally-self: {unit.Id} has no Self-Rally capability, and only its own side's RPh gives one MMC a Self-Rally without it (A10.63, A18.11); it needs an unbroken leader in its Location\"\n"
        "                    : state.FirstMmcRallyTaken.Contains(unit.Side)\n"
        "                        // Pass 31c (backlog section 48): the refusal names the unit that used the attempt.\n"
        "                        ? $\"play.rally-self: {unit.Id} cannot Self-Rally: its side's one MMC Self-Rally of this RPh was used{(FirstMmcRallier(existing, state, unit.Side) is { } first ? $\" by {first}\" : string.Empty)} (A18.11). It needs an unbroken leader in its Location\"\n"
        "                        : $\"play.rally-self: {unit.Id} has no Self-Rally capability, and a broken leader is in its Location, so its side's first MMC Rally attempt is not open to it (A10.71, A18.11); rally the leader first\"]\n"
        "                : [];\n",
        "            var why = ScenarioA1RallyRules.SelfRallyRefusal(precheck, unit.Id, unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side),\n"
        "                () => FirstMmcRallier(existing, state, unit.Side));\n",
    ),
    (
        "        var withheld = concealed && attempt.EnemyGoodOrderInLosWithin16 != true;\n",
        "        var withheld = ScenarioA1RallyRules.Withheld(concealed, attempt.EnemyGoodOrderInLosWithin16);\n",
    ),
    (
        "            [$\"play.rally: {unit.Id} attempts to rally in the RPh\" + (leaderId is null ? \" by Self-Rally\" : $\", rallied by {leaderId}\")])\n",
        "            [ScenarioA1RallyRules.RallySummary(unit.Id, leaderId)])\n",
    ),
    (
        "            if (resolution.Disposition == RallyResolution.Resolved)\n"
        "            {\n"
        "                break;\n"
        "            }\n"
        "\n"
        "            // An option the attempt reaches stops it until its owner answers (ruling R5.8).\n"
        "            if (resolution.Reasons is [{ } option] && option.StartsWith(\"asl.a1.rally.choice-missing:\", StringComparison.Ordinal))\n"
        "            {\n",
        "            var next = ScenarioA1RallyRules.NextStep(resolution.Disposition, resolution.Reasons);\n"
        "            if (next.Resolved)\n"
        "            {\n"
        "                break;\n"
        "            }\n"
        "\n"
        "            // An option the attempt reaches stops it until its owner answers (ruling R5.8).\n"
        "            if (next.ChoiceKey is { } choiceKey)\n"
        "            {\n",
    ),
    (
        "                    Pending(state, option[\"asl.a1.rally.choice-missing:\".Length..], resume), package, visibility));\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            if (resolution.Reasons is not [{ } missing] || !missing.StartsWith(\"asl.a1.rally.roll-missing:\", StringComparison.Ordinal))\n"
        "            {\n"
        "                throw new InvalidOperationException(\"The Rally package left an attempt it had accepted undecided: \" + string.Join(\"; \", resolution.Reasons));\n"
        "            }\n"
        "\n"
        "            // The Rally DR, a leader's Wound Severity dr (A17.11), the Heat of Battle DR (A15.1), the Leader Creation dr (A18.2), or\n"
        "            // the NTC of a companion a berserk leader tries to take with him (A15.41).\n"
        "            const string BerserkCheck = \"asl.a1.rally.roll-missing:berserkCheck:\";\n"
        "            var companion = missing.StartsWith(BerserkCheck, StringComparison.Ordinal) ? missing[BerserkCheck.Length..] : null;\n"
        "            var rollKey = companion is not null ? \"berserkCheck:\" + companion\n"
        "                : missing.Contains(\"woundSeverity\", StringComparison.Ordinal) ? \"woundSeverity\"\n"
        "                : missing.EndsWith(\":heatOfBattle\", StringComparison.Ordinal) ? \"heatOfBattle\"\n"
        "                : missing.EndsWith(\":leaderCreation\", StringComparison.Ordinal) ? \"leaderCreation\"\n"
        "                : \"rally\";\n"
        "            var (count, purpose) = rollKey switch\n"
        "            {\n"
        "                \"woundSeverity\" => (1, \"rally-wound-severity\"),\n"
        "                \"heatOfBattle\" => (2, \"rally-heat-of-battle\"),\n"
        "                \"leaderCreation\" => (1, \"rally-leader-creation\"),\n"
        "                _ when companion is not null => (2, \"rally-berserk-check\"),\n"
        "                _ => (2, \"rally\"),\n"
        "            };\n",
        "                    Pending(state, choiceKey, resume), package, visibility));\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            if (next.Undecided is { } undecided)\n"
        "            {\n"
        "                throw new InvalidOperationException(\"The Rally package left an attempt it had accepted undecided: \" + undecided);\n"
        "            }\n"
        "\n"
        "            // The Rally DR, a leader's Wound Severity dr (A17.11), the Heat of Battle DR (A15.1), the Leader Creation dr (A18.2), or\n"
        "            // the NTC of a companion a berserk leader tries to take with him (A15.41), as Rules names them.\n"
        "            var rollKey = next.RollKey!;\n"
        "            var (count, purpose) = (next.Count, next.Purpose!);\n",
    ),
    (
        "        if (resolution.Arithmetic!.HeatOfBattle is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender && !resolution.Effect!.Eliminated)\n"
        "        {\n"
        "            var id = resolution.Effect.FinalDefinitionId != resolution.Effect.DefinitionId ? $\"{attemptId}-{unit.Id}\" : unit.Id;\n"
        "            events.Add(Event(scope, attemptId, events.Count + 1, expected, \"surrender-pending\", new SurrenderPending(id, surrender.Captors!), package, null, [rallyId]));\n",
        "        var heatOfBattle = resolution.Arithmetic!.HeatOfBattle;\n"
        "        if (ScenarioA1RallyRules.SurrendersAfterRally(heatOfBattle?.Result == HeatOfBattleOutcome.Surrender, heatOfBattle?.Captors is { Count: > 0 }, resolution.Effect!.Eliminated))\n"
        "        {\n"
        "            var id = ScenarioA1RallyRules.SurrenderingId(attemptId, unit.Id, resolution.Effect.FinalDefinitionId != resolution.Effect.DefinitionId);\n"
        "            events.Add(Event(scope, attemptId, events.Count + 1, expected, \"surrender-pending\", new SurrenderPending(id, heatOfBattle!.Captors!), package, null, [rallyId]));\n",
    ),
])
