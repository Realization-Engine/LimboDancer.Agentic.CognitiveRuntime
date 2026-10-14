"""Pass 40: the fixes after the five reviews, in Rules, Play, Units, and the records (the page's are in apply_review_page.py).

Exact anchors, asserted once each. The MandatoryFire files were rewritten whole beside this script.
"""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:90], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Play/LiveCloseCombat.cs", [
    # Boundary 1: a CC record made before the pass does not hold whether a crew mans its Gun.
    ("            recorded.HandToHand == true, recorded.InfantryOverrun == true);\n"
     "        if (expected is null)\n"
     "        {\n"
     "            return reason;\n"
     "        }\n\n"
     "        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))\n"
     "        {\n"
     "            return \"The CC record's facts do not match the game state.\";",
     "            recorded.HandToHand == true, recorded.InfantryOverrun == true);\n"
     "        if (expected is null)\n"
     "        {\n"
     "            return reason;\n"
     "        }\n\n"
     "        // Pass 40: whether a crew mans its Gun (A5.5) is taken as recorded, so a record made before the pass, which lacks the fact, replays.\n"
     "        var manning = recorded.Units.Where(unit => unit.UnitId is not null).ToDictionary(unit => unit.UnitId!, unit => unit.Manning, StringComparer.Ordinal);\n"
     "        expected = expected with { Units = [.. expected.Units.Select(unit => unit with { Manning = unit.UnitId is { } id ? manning.GetValueOrDefault(id) : null })] };\n"
     "        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))\n"
     "        {\n"
     "            return \"The CC record's facts do not match the game state.\";"),
])
apply("LimboDancer.Domains.Asl.Play/LiveStacking.cs", [
    # Referee (movement) 5: the target's stack is counted over the units the firer knows, so a proposal's DRM tells it of no hidden or "?" unit.
    ("    public static StackCount Of(GameState state, BoardLocation at, string side, IEnumerable<UnitInstance>? arriving = null)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(state);\n"
     "        var units = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side)\n",
     "    public static StackCount Of(GameState state, BoardLocation at, string side, IEnumerable<UnitInstance>? arriving = null, bool knownOnly = false)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(state);\n"
     "        var units = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side\n"
     "                && (!knownOnly || (GameState.Condition(unit, Conditions.Hidden) != ConditionState.True && GameState.Condition(unit, Conditions.Concealed) != ConditionState.True)))\n"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs", [
    ("&& LiveStacking.Of(state, target, enemy).DefensePenalty is var crowded and > 0\n", "&& LiveStacking.Of(state, target, enemy, knownOnly: true).DefensePenalty is var crowded and > 0\n"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs", [
    # Referee (movement) 4 and 5: the target's penalty in the MPh is the movers', never a vehicle's, and is counted over Known units.
    ("            TargetOverstack = targetSide is not null && LiveStacking.Of(state, target, targetSide).DefensePenalty is var crowded and > 0 ? crowded : null,\n",
     "            TargetOverstack = targetSide is not null && ScenarioA1Stacking.TargetPenaltyApplies(state.Phase == \"mph\", targetSide == state.PhasingSide,\n"
     "                    (attack.Targets ?? []).Any(item => item.UnitId is { } id && state.Unit(id) is { } aimed && vocabulary.IsA(aimed.Kind, \"asl:personnel\") && !LiveFire.IsVehicle(aimed)))\n"
     "                && LiveStacking.Of(state, target, targetSide, knownOnly: true).DefensePenalty is var crowded and > 0 ? crowded : null,\n\n"
     "            // A4.62 (p. 51; pass 40): Hazardous Movement holds against any attack, a vehicle's and Residual FP's too.\n"
     "            HazardousMovement = ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, attack),\n"),
    ("                    () => HeightAdvantageAt(state, target, targetRead, [.. sources.Select(item => (item.From, item.Height))], attack.SnapShot == true)),\n"
     "                HazardousMovement = ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, attack),\n",
     "                    () => HeightAdvantageAt(state, target, targetRead, [.. sources.Select(item => (item.From, item.Height))], attack.SnapShot == true)),\n"),
    # Referee (movement) 2: the Dash holds while a member still Dashes; a mover broken or pinned no longer does.
    ("        UnitInstance[] movers = [.. dashing.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>()];\n"
     "        if (movers.Length == 0 || !ScenarioA1Dash.BenefitsHold(",
     "        UnitInstance[] movers = [.. dashing.Members.Select(id => state.Unit(id)).OfType<UnitInstance>()];\n"
     "        if (movers.Length == 0 || !ScenarioA1Dash.BenefitsHold("),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1Stacking.cs", [
    ("public static class ScenarioA1Stacking\n{\n",
     "public static class ScenarioA1Stacking\n{\n"
     "    /// <summary>\n"
     "    /// A5.13, A5.131, A5.132 (p. 52; pass 40): whether the target's overstacking penalty is read for an attack. \"During the MPh, this penalty\n"
     "    /// applies only to moving units\", so not to the DEFENDER's stack under the ATTACKER's own fire then; and it is Personnel's, never a vehicle's.\n"
     "    /// </summary>\n"
     "    public static bool TargetPenaltyApplies(bool mph, bool targetsOfPhasingSide, bool anyPersonnelTarget) => (!mph || targetsOfPhasingSide) && anyPersonnelTarget;\n\n"),
])

# --- The records ---------------------------------------------------------------------------------------------------------------------
apply("LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs", [
    # Table player 4: a Guard's Deployment at will is A20.5's, not A1.31's.
    ("                        ? $\"{lineage.Consumed[0]}{In(before, lineage.Consumed[0])} becomes the HS {string.Join(\" and \", lineage.Produced.Select(unit => unit.Id))} (A1.31){DeploySplit(attempts[attempt], lineage)}\"",
     "                        ? $\"{lineage.Consumed[0]}{In(before, lineage.Consumed[0])} becomes the HS {string.Join(\" and \", lineage.Produced.Select(unit => unit.Id))} \"\n"
     "                            + (attempts[attempt].Any(other => other.Payload is DeploymentAttempted) ? \"(A1.31)\" : \"with no NTC, as a Guard of prisoners (A20.5)\")\n"
     "                            + DeploySplit(attempts[attempt], lineage)"),
    # Table player 5: the stack a leader's NTC is taken for, and what a failure costs.
    ("                    records.Add((item.EventId, \"overrun-ntc\", $\"{ntc.Taker}{In(before, ntc.Taker)} takes the NTC for an Infantry OVR of {ntc.Target}: DR {dr} \"\n"
     "                        + $\"{ntc.Drm:+0;-0;+0} against morale {ntc.Morale}: {(ntc.Passed ? \"passed\" : \"failed\")} (A4.15)\"));",
     "                    string[] others = [.. ntc.Units.Where(id => id != ntc.Taker)];\n"
     "                    records.Add((item.EventId, \"overrun-ntc\", $\"{ntc.Taker}{In(before, ntc.Taker)} takes the NTC{(others.Length > 0 ? $\" for {string.Join(\", \", others)}\" : \"\")} \"\n"
     "                        + $\"for an Infantry OVR into {ntc.Target}: DR {dr} {ntc.Drm:+0;-0;+0} against morale {ntc.Morale}: \"\n"
     "                        + (ntc.Passed ? \"passed\" : others.Length > 0 ? \"failed; none of them moves or acts again this MPh\" : \"failed; it makes no OVR there\") + \" (A4.15)\"));"),
])
print("ok")
