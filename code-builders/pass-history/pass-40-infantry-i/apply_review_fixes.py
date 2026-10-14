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


# --- Rules: Infantry OVR -------------------------------------------------------------------------------------------------------------
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1InfantryOverrun.cs", [
    # Referee 8a: a concealed or hidden vehicle beside the SMC must not turn the NTC away (it told the ATTACKER a "?" was a vehicle).
    ("        return known is [{ Smc: true, Melee: false, Vehicle: false }] && !enemies.Any(unit => unit.Vehicle)\n",
     "        return known is [{ Smc: true, Melee: false, Vehicle: false }]\n"),
    # Referee 3: a hexside's TEM is not added to the hex's (B9.31).
    ("    /// <summary>A4.15: the NTC's DRM is the TEM of the enemy's Location, and a hexside's TEM when the LOS crosses it.</summary>\n"
     "    public static int NtcDrm(int tem, int hexsideTem) => Math.Max(tem, 0) + Math.Max(hexsideTem, 0);",
     "    /// <summary>\n"
     "    /// A4.15: the NTC's DRM is the TEM of the enemy's Location: the hex's, or a hexside's when the stack would cross it and it is the larger. The\n"
     "    /// two are not added: a wall's or hedge's TEM is \"not cumulative with positive TEM of other terrain in that hex\" (B9.31, p. 119).\n"
     "    /// </summary>\n"
     "    public static int NtcDrm(int tem, int hexsideTem) => Math.Max(Math.Max(tem, 0), Math.Max(hexsideTem, 0));\n\n"
     "    /// <summary>\n"
     "    /// A4.15: whether the stack's MMC are covered by passed NTCs for this Location: each MMC is among the units of a passed NTC whose taker enters\n"
     "    /// with it. A leader's NTC exempts the MMC \"he is stacked with and moves with\", so it covers them only while he moves with them; MMC that\n"
     "    /// each passed their own are covered together.\n"
     "    /// </summary>\n"
     "    public static bool NtcCovers(IReadOnlyList<string> mmcIds, IReadOnlyList<string> moverIds, IReadOnlyList<(IReadOnlyList<string> Units, string? Taker, bool Passed)> taken)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(mmcIds);\n"
     "        ArgumentNullException.ThrowIfNull(moverIds);\n"
     "        ArgumentNullException.ThrowIfNull(taken);\n"
     "        return mmcIds.Count > 0 && mmcIds.All(id => taken.Any(ntc => ntc.Passed && ntc.Units.Contains(id, StringComparer.Ordinal)\n"
     "            && (ntc.Taker is null || moverIds.Contains(ntc.Taker, StringComparer.Ordinal))));\n"
     "    }\n\n"
     "    /// <summary>\n"
     "    /// A20.54 (p. 88): who may enter Unarmed enemy units to recapture them. A \"non-prisoner, unarmed Infantry unit may not enter a Known\n"
     "    /// enemy-occupied Location\", and a stack of Dummies enters no enemy unit's Location (A12.11).\n"
     "    /// </summary>\n"
     "    public static bool MayRecapture(bool anyMoverUnarmed, bool allMoversDummies) => !anyMoverUnarmed && !allMoversDummies;"),
    # Boundary 4: the Location's fitness is decided here, from each neighbour's facts.
    ("    public static IReadOnlyList<string> EvadeDestinations(IReadOnlyList<(string Location, bool OpenGround)> accessible)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(accessible);\n"
     "        var covered = accessible.Where(item => !item.OpenGround).Select(item => item.Location).ToArray();\n"
     "        return covered.Length > 0 ? covered : [.. accessible.Select(item => item.Location)];\n"
     "    }",
     "    public static IReadOnlyList<string> EvadeDestinations(IReadOnlyList<(string Location, bool Enterable, bool Playable, bool EnemyOccupied, bool OpenGround)> adjacent)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(adjacent);\n"
     "        var accessible = adjacent.Where(item => item.Enterable && item.Playable && !item.EnemyOccupied).ToArray();\n"
     "        var covered = accessible.Where(item => !item.OpenGround).Select(item => item.Location).ToArray();\n"
     "        return covered.Length > 0 ? covered : [.. accessible.Select(item => item.Location)];\n"
     "    }"),
    # UI 5, referee 8b: the DEFENDER's summary does not list the Locations (a Location left out told it of a hidden ATTACKER unit).
    ("    public static string EvadeText(string unitId, IReadOnlyList<string> options)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(options);\n"
     "        return $\"play.overrun-evade: {unitId} moves away from the Infantry OVR before any fire; the ATTACKER chooses among {string.Join(\", \", options)} (A4.151)\";",
     "    public static string EvadeText(string unitId, IReadOnlyList<string> options)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(options);\n"
     "        return $\"play.overrun-evade: {unitId} moves away from the Infantry OVR before any fire; the ATTACKER names the adjacent Location it enters (A4.151)\";"),
])

# --- Rules: stacking, Dash, texts ----------------------------------------------------------------------------------------------------
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1Dash.cs", [
    ("the DEFENDER's small arms fire at it in the road Location is Area Fire where both sides of the road are not Open Ground to the firer, and its move ends in {dashText} (A4.63)",
     "the DEFENDER's non-ordnance Defensive First Fire at it in the road Location (but a Fire Lane and Residual FP already there) is Area Fire for a firer to whom neither side of the road is Open Ground, and its move ends in {dashText} (A4.63)"),
    ("{declared} is not on the other side of the road: a Dash goes directly into the road Location and directly on into a ground-level Location beside it that is neither the Location left nor beside it (A4.63)",
     "{declared} is not across the road: the Location Dashed to is at ground level, adjacent to the road Location, and neither the Location left nor adjacent to the Location left (A4.63)"),
    ("    public static string? EndBar(string? pendingText, bool benefitsHold) => pendingText is { } pending && benefitsHold\n",
     "    public static string? EndBar(string? pendingText, bool benefitsHold, bool unableToEnter = false) => pendingText is { } pending && benefitsHold && !unableToEnter\n"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs", [
    # Boundary 6: the kinds of fire the Dash halves are tested here, not only by the caller.
    ("            if (attack.Dash == true && attack.FireLane != true)\n",
     "            if (attack.Dash == true && attack.FireLane != true && attack.OrdnanceHit is null && attack.FireKind is FirstFire or SubsequentFirstFire or FinalProtectiveFire)\n"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementCalculator.cs", [
    ("\"play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh, but by an Infantry OVR of a lone SMC or to recapture Unarmed units (A4.14, A4.15, A20.54; ruling R10.11)\"",
     "\"play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh, except by a berserk charge, an Infantry OVR of a lone Known SMC (take its NTC first), or to recapture Unarmed units (A4.14, A4.15, A20.54)\""),
])

# --- Units ---------------------------------------------------------------------------------------------------------------------------
apply("LimboDancer.Domains.Asl.Units/State/LiveRecords.cs", [
    ("public sealed record InfantryOverrunNtcTaken(IReadOnlyList<string> Units, string Target, bool Passed);",
     "public sealed record InfantryOverrunNtcTaken(IReadOnlyList<string> Units, string Target, bool Passed, string? Taker = null);"),
])
apply("LimboDancer.Domains.Asl.Units/State/GameProjector.cs", [
    ("new InfantryOverrunNtcTaken(ntc.Units, ntc.Target.ToString(), ntc.Passed)]", "new InfantryOverrunNtcTaken(ntc.Units, ntc.Target.ToString(), ntc.Passed, ntc.Taker)]"),
    # Referee (movement) 7a: the Manhandling DR itself is Hazardous Movement, whether or not the Gun moves.
    ("            if (state.Phase != \"mph\" || Active(state, manhandling.Gun) is not EquipmentInstance || !rolls.TryGetValue(manhandling.Roll, out var roll) || roll.Count != 2\n",
     "            if (state.Phase != \"mph\" || Active(state, manhandling.Gun) is not EquipmentInstance pushed || !rolls.TryGetValue(manhandling.Roll, out var roll) || roll.Count != 2\n"),
    ("                return Fail<GameState>(\"UNIT-STATE-039\", \"A Manhandling DR is made in the MPh for an active Gun, and its result follows from its Final DR and the M# (C10.3).\");\n"
     "            }\n\n"
     "            return state;\n",
     "                return Fail<GameState>(\"UNIT-STATE-039\", \"A Manhandling DR is made in the MPh for an active Gun, and its result follows from its Final DR and the M# (C10.3).\");\n"
     "            }\n\n"
     "            // Pass 40 (C10.3, p. 181): the attempt is Hazardous Movement whether or not the Gun moves, so a failed push counts too.\n"
     "            return pushed.Holding is { Role: HoldingRole.Manned } manned\n"
     "                ? state with { HazardousMovers = Rules.ScenarioA1MovementCalculator.HazardousMovers(state.HazardousMovers, true, [manned.Holder]) }\n"
     "                : state;\n"),
])

# --- Play ----------------------------------------------------------------------------------------------------------------------------
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs", [
    # Referee 4: Unarmed movers and a stack of Dummies recapture nothing.
    ("&& !occupy && !minimumMove && to != from && ScenarioA1InfantryOverrun.OnlyUnarmed(defenders);",
     "&& !occupy && !minimumMove && to != from && ScenarioA1InfantryOverrun.OnlyUnarmed(defenders)\n"
     "            && ScenarioA1InfantryOverrun.MayRecapture(movers.Any(unit => Is(unit, Conditions.Unarmed)), movers.All(unit => unit.Kind == UnitKinds.Dummy));"),
    # Referee 2 and 5: each MMC under a passed NTC whose taker enters with it.
    ("ScenarioA1InfantryOverrun.EntryBar(mmcIds.Length > 0 && taken.Any(ntc => ntc.Passed && mmcIds.All(id => ntc.Units.Contains(id, StringComparer.Ordinal))),",
     "ScenarioA1InfantryOverrun.EntryBar(ScenarioA1InfantryOverrun.NtcCovers(mmcIds, ids, [.. taken.Select(ntc => (ntc.Units, ntc.Taker, ntc.Passed))]),"),
    ("not for an entry from off board, a Bypass, or an obstacle occupied (A4.132)", "not for an entry from off board, a Bypass, or a step from Bypass into the obstacle of the same hex (A4.132)"),
    # Referee (movement) 1: a Dash to a Location the stack cannot enter does not hold the stack in the road.
    ("        if (ScenarioA1Dash.EndBar(movement?.Dash?.ToString(), movement is not null && ScenarioA1Dash.BenefitsHold(movement.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>()\n"
     "            .Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk))))) is { } dashEnd)\n",
     "        // \"... or otherwise unable to enter\": the step into the Location Dashed to is asked of the move's own plan, which draws no dice.\n"
     "        var dashBlocked = movement?.Dash is { } dashTo && movement.Members.Count > 0 && PlanMove(scope, JsonSerializer.SerializeToElement(new Dictionary<string, object>\n"
     "        {\n"
     "            [\"unitIds\"] = movement.Members, [\"to\"] = dashTo.ToString(),\n"
     "        }), existing, \"dash-read\", expected, label, actor).Status != GamePlanStatus.Ready;\n"
     "        if (ScenarioA1Dash.EndBar(movement?.Dash?.ToString(), movement is not null && ScenarioA1Dash.BenefitsHold(movement.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>()\n"
     "            .Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk)))), dashBlocked) is { } dashEnd)\n"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.InfantryOverrun.cs", [
    ("        if (origins is not [{ } from] || (state.Movement is { } moving && !ids.All(id => moving.Members.Contains(id, StringComparer.Ordinal))))\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.overrun-ntc: the stack stands in one Location, and no other stack's move is open (A4.15, A4.2)\");\n"
     "        }\n",
     "        if (origins is not [{ } from])\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.overrun-ntc: the units named are not all in one Location (A4.15)\");\n"
     "        }\n\n"
     "        if (state.Movement is { } moving && !ids.All(id => moving.Members.Contains(id, StringComparer.Ordinal)))\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.overrun-ntc: another stack's move is still open; end it first (A4.2)\");\n"
     "        }\n"),
    # Boundary 4: every neighbour's facts go to Rules.
    ("        ScenarioA1InfantryOverrun.EvadeDestinations([.. Neighbors(state, from)\n"
     "            .Where(near => InfantryStep(state, from, near).Entry is not null && PlayableBar(state, near) is null\n"
     "                && !state.At(near).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != side && !Is(other, Conditions.Captured)))\n"
     "            .Select(near => (near.ToString(), ReadLocation(state, near) is { } read && TerrainKey(read) == \"open-ground\"))]);",
     "        ScenarioA1InfantryOverrun.EvadeDestinations([.. Neighbors(state, from)\n"
     "            .Select(near => (near.ToString(), InfantryStep(state, from, near).Entry is not null, PlayableBar(state, near) is null,\n"
     "                state.At(near).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != side && !Is(other, Conditions.Captured)),\n"
     "                ReadLocation(state, near) is { } read && TerrainKey(read) == \"open-ground\"))]);"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs", [
    # Referee 10: a Guard's Deployment at will is no RPh action, in the RPh either.
    ("            AddDeployed(deployed, null, rphAction: state.Phase == \"rph\");\n", "            AddDeployed(deployed, null, rphAction: false);\n"),
    ("    /// <summary>The SW a unit possesses, in id order.</summary>\n",
     "    /// <summary>The squads of a side (every side when null) that may Deploy at will now, as Guards of prisoners (A20.5; pass 40), for the Play page.</summary>\n"
     "    public IReadOnlyList<string> GuardSquadsAtWill(GameState state, string? side)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(state);\n"
     "        return [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && (side is null || unit.Side == side) && state.Location(unit.Id) is not null\n"
     "            && ScenarioA1SupportWeaponRules.GuardDeploysAtWill(vocabulary.IsA(unit.Kind, \"asl:squad\"), IsGuard(state, unit), Is(unit, Conditions.Broken))).Select(unit => unit.Id)];\n"
     "    }\n\n"
     "    /// <summary>The SW a unit possesses, in id order.</summary>\n"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs", [
    ("        if (MandatoryFire(state, existing) is { } owed && ScenarioA1MandatoryFire.PassBar(owed.Firers, owed.At.ToString()) is { } mandatoryBar)",
     "        if (MandatoryFire(state, existing) is { } owed && ScenarioA1MandatoryFire.PassBar(owed.Firers, owed.Group, owed.At.ToString()) is { } mandatoryBar)"),
])
apply("LimboDancer.Domains.Asl.Play/LiveCloseCombat.cs", [
    # Boundary 1: a CC record made before the pass does not hold whether a crew mans its Gun.
    ("        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))\n"
     "        {\n"
     "            return \"The CC record's facts do not match the game state.\";",
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
