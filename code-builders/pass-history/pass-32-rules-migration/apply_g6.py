"""Pass 32.g commit 6: CC with vehicles calls Rules (ScenarioA1VehicleCloseCombatRules)."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
VC = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.VehicleCloseCombat.cs"
FIRE = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs"
MODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1FireRuleModels.cs"
C = "ScenarioA1VehicleCloseCombatRules"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:90])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(MODELS, [("    Melee,\n    Unarmed,\n}\n", "    Melee,\n    Unarmed,\n    CcReaction,\n}\n")])
apply(FIRE, [(
    "        UnitCondition.Unarmed => Conditions.Unarmed,\n",
    "        UnitCondition.Unarmed => Conditions.Unarmed,\n        UnitCondition.CcReaction => Conditions.CcReaction,\n",
)])

apply(VC, [
    (
        '    private static bool VehicleCcArmament(UnitInstance vehicle) =>\n'
        '        VehicleDefinition(vehicle) is { } definition && !Is(vehicle, Conditions.Stunned) && !Is(vehicle, Conditions.Shocked) && !Is(vehicle, Conditions.UnconfirmedKill)\n'
        '        && !Is(vehicle, Conditions.Abandoned) && !Is(vehicle, Conditions.Recalled) && !Is(vehicle, Conditions.Captured)\n'
        '        && ((definition.CoaxialMg is not null && !Is(vehicle, Conditions.CmgMalfunctioned))\n'
        '            || (definition.AntiAircraftMg is not null && LiveFire.CrewExposed(vehicle) && !(definition.MainArmament == "aamg" && (Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled)))));\n',
        '    private static bool VehicleCcArmament(UnitInstance vehicle) => VehicleDefinition(vehicle) is var definition\n'
        f'        && {C}.VehicleCcArmament(definition is not null, Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),\n'
        '            Is(vehicle, Conditions.Abandoned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Captured), definition?.CoaxialMg is not null, Is(vehicle, Conditions.CmgMalfunctioned),\n'
        '            definition?.AntiAircraftMg is not null, LiveFire.CrewExposed(vehicle), definition?.MainArmament == "aamg", Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled));\n',
    ),
    (
        '    private bool CcAttacker(UnitInstance unit) =>\n'
        '        unit.Status == InstanceStatus.Active && !LiveFire.IsVehicle(unit) && vocabulary.IsA(unit.Kind, "asl:personnel") && !Is(unit, Conditions.Broken)\n'
        '        && !Is(unit, Conditions.Captured) && unit.Kind != UnitKinds.Dummy;\n',
        f'    private bool CcAttacker(UnitInstance unit) => {C}.CcAttacker(unit.Status == InstanceStatus.Active, LiveFire.IsVehicle(unit), vocabulary.IsA(unit.Kind, "asl:personnel"),\n'
        '        Is(unit, Conditions.Broken), Is(unit, Conditions.Captured), unit.Kind == UnitKinds.Dummy);\n'
        '\n'
        '    /// <summary>A unit of a Location holding a vehicle as Rules reads it (pass 32.g): its CC armament, attacker status, Personnel, prisoner, berserk, Known, and crew flags.</summary>\n'
        '    private VehicleCcUnitFacts VehicleCcUnit(UnitInstance unit) => new(unit.Id, unit.Side, LiveFire.IsVehicle(unit), VehicleCcArmament(unit), CcAttacker(unit),\n'
        '        vocabulary.IsA(unit.Kind, "asl:personnel"), Is(unit, Conditions.Captured), Is(unit, Conditions.Berserk), KnownEnemy(unit), vocabulary.IsA(unit.Kind, "asl:crew"));\n',
    ),
    (
        '        var here = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();\n'
        '        var sides = new HashSet<string>(StringComparer.Ordinal);\n'
        '        foreach (var unit in here.Where(unit => !attacked.Contains(unit.Id) && !passed.Contains(unit.Side)))\n'
        '        {\n'
        '            var enemies = here.Where(other => other.Side != unit.Side && !Is(other, Conditions.Captured)).ToArray();\n'
        '            if (LiveFire.IsVehicle(unit) ? VehicleCcArmament(unit) && enemies.Any(other => !LiveFire.IsVehicle(other) && CcAttacker(other) || (!LiveFire.IsVehicle(other) && vocabulary.IsA(other.Kind, "asl:personnel")))\n'
        '                : CcAttacker(unit) && enemies.Any(LiveFire.IsVehicle))\n'
        '            {\n'
        '                sides.Add(unit.Side);\n'
        '            }\n'
        '        }\n'
        '\n'
        '        return sides;\n',
        f'        return {C}.SidesWithAttacks([.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).Select(VehicleCcUnit)], attacked, passed);\n',
    ),
    (
        '        var sides = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();\n'
        '        var vehicular = sides.Where(LiveFire.IsVehicle).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();\n'
        '        return vehicular.Length == 1 ? sides.Select(unit => unit.Side).FirstOrDefault(side => side != vehicular[0]) ?? state.PhasingSide! : state.PhasingSide!;\n',
        f'        return {C}.FirstCcSide([.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured))\n'
        '            .Select(unit => (unit.Side, LiveFire.IsVehicle(unit)))], state.PhasingSide!);\n',
    ),
    (
        '        var left = SidesWithAttacks(after, location, attacked, passed);\n'
        '        var other = left.FirstOrDefault(side => side != moved);\n'
        '        return other is not null ? (other, false) : left.Contains(moved) ? (moved, false) : (null, true);\n',
        f'        return {C}.NextCcSide(SidesWithAttacks(after, location, attacked, passed), moved);\n',
    ),
    (
        '        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);\n'
        '        if (state.Phase != "ccph" || entry is { Closed: true } || !VehicleCloseCombatTurn(state, location).Open)\n'
        '        {\n'
        '            return null;\n'
        '        }\n'
        '\n'
        '        var here = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();\n'
        '        return here.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")) ? null\n'
        '            : here.Where(unit => (side is null || unit.Side == side) && Is(unit, Conditions.Berserk) && CcAttacker(unit) && entry?.Attacking.Contains(unit.Id) != true\n'
        '                && here.Any(other => other.Side != unit.Side && LiveFire.IsVehicle(other) && KnownEnemy(other) && !Is(other, Conditions.Captured)))\n'
        '                .OrderBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();\n',
        '        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);\n'
        f'        if (!{C}.OwingPossible(state.Phase, entry is {{ Closed: true }}, VehicleCloseCombatTurn(state, location).Open))\n'
        '        {\n'
        '            return null;\n'
        '        }\n'
        '\n'
        '        var here = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();\n'
        f'        return {C}.BerserkOwingVehicleAttack([.. here.Select(VehicleCcUnit)], side, entry?.Attacking ?? []) is {{ }} owing ? here.First(unit => unit.Id == owing) : null;\n',
    ),
    (
        '        if (state.Phase != "mph" || state.Movement is not { WindowOpen: false, Charge: { } charged } movement || movement.Location != charged\n'
        '            || state.CloseCombats.Any(item => item.Location == charged))\n'
        '        {\n'
        '            return null;\n'
        '        }\n'
        '\n'
        '        var here = state.At(charged).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();\n'
        '        var enemies = here.Where(unit => unit.Side != state.PhasingSide).ToArray();\n'
        '        if (!here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)\n'
        '            && vocabulary.IsA(unit.Kind, "asl:mmc")) || enemies is not [{ } smc] || !vocabulary.IsA(smc.Kind, "asl:smc") || !KnownEnemy(smc) || Is(smc, Conditions.Melee))\n'
        '        {\n'
        '            return null;\n'
        '        }\n',
        '        var movement = state.Movement;\n'
        f'        if (movement?.Charge is not {{ }} charged || !{C}.OverrunPendingPossible(state.Phase, movement.WindowOpen, true, movement.Location == charged,\n'
        '            state.CloseCombats.Any(item => item.Location == charged)))\n'
        '        {\n'
        '            return null;\n'
        '        }\n'
        '\n'
        '        var here = state.At(charged).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();\n'
        '        var enemies = here.Where(unit => unit.Side != state.PhasingSide).ToArray();\n'
        '        var smc = enemies.Length > 0 ? enemies[0] : null;\n'
        f'        if (smc is null || !{C}.OverrunSmc(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)\n'
        '            && vocabulary.IsA(unit.Kind, "asl:mmc")), enemies.Length, vocabulary.IsA(smc.Kind, "asl:smc"), KnownEnemy(smc), Is(smc, Conditions.Melee)))\n'
        '        {\n'
        '            return null;\n'
        '        }\n',
    ),
    (
        '        return CcAttacker(smc) ? [new CloseCombatDeclaration(berserk, [smc.Id]), new CloseCombatDeclaration([smc.Id], berserk)] : [new CloseCombatDeclaration(berserk, [smc.Id])];\n',
        f'        return {C}.OverrunAttacks(berserk, smc.Id, CcAttacker(smc));\n',
    ),
    (
        '        if (state.Phase != "ccph" || entry is { Closed: true } || !state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))\n'
        '        {\n'
        '            return (null, false);\n'
        '        }\n'
        '\n'
        '        var left = SidesWithAttacks(state, location, entry?.Attacking ?? [], entry?.Passed ?? []);\n'
        '        var next = entry?.Next ?? FirstCcSide(state, location);\n'
        '        return left.Count == 0 ? (null, false) : (left.Contains(next) ? next : left.First(), true);\n',
        f'        if (!{C}.TurnOpen(state.Phase, entry is {{ Closed: true }}, state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))))\n'
        '        {\n'
        '            return (null, false);\n'
        '        }\n'
        '\n'
        f'        return {C}.Turn(SidesWithAttacks(state, location, entry?.Attacking ?? [], entry?.Passed ?? []), entry?.Next ?? FirstCcSide(state, location));\n',
    ),
    (
        '        return state.Phase switch\n'
        '        {\n'
        '            "mph" => PlanReactionFire(scope, existing, attemptId, expected, label, actor, state, location, attackers, vehicleId),\n'
        '            "ccph" => PlanSequentialCloseCombat(scope, existing, attemptId, expected, label, actor, state, location, attackers, defenders, vehicleId, pass),\n'
        '            _ => Refused(scope, label, expected, "play.cc-vehicle-phase: CC with a vehicle is made in the CCPh, or as CC Reaction Fire in the MPh (A11.5, D7.21)"),\n'
        '        };\n',
        f'        if ({C}.VehicleCcPhaseBar(state.Phase) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n'
        '\n'
        '        return state.Phase == "mph"\n'
        '            ? PlanReactionFire(scope, existing, attemptId, expected, label, actor, state, location, attackers, vehicleId)\n'
        '            : PlanSequentialCloseCombat(scope, existing, attemptId, expected, label, actor, state, location, attackers, defenders, vehicleId, pass);\n',
    ),
    (
        '        if (state.Movement is not { WindowOpen: true, Vehicle: true, Members: [{ } moving] } window || state.Unit(moving) is not { } vehicle\n'
        '            || state.Location(moving)?.Location != location || (vehicleId is not null && vehicleId != moving))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.reaction-fire: CC Reaction Fire attacks the moving vehicle in the attackers\' Location while the DEFENDER\'s window on its MP expenditure is open (D7.2, D7.21)");\n'
        '        }\n'
        '\n'
        '        // D7.1, D7.2 (referee, pass 11): Reaction Fire at an OVRing vehicle comes after the OVR is resolved.\n'
        '        if (window.Overrun is not null)\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.reaction-fire: the OVR is resolved first; CC Reaction Fire follows it (D7.1, D7.2)");\n'
        '        }\n'
        '\n'
        '        var units = attackers.Select(state.Unit).ToArray();\n'
        '        if (units.Length is < 1 or > 2 || units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side == vehicle.Side || state.Location(unit.Id)?.Location != location\n'
        '            || LiveFire.IsVehicle(unit) || Is(unit, Conditions.Broken) || Is(unit, Conditions.Pinned) || Is(unit, Conditions.Captured) || Is(unit, Conditions.Melee)))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.reaction-fire: the attackers are one or two unbroken, unpinned, armed DEFENDER units in {location}, not in Melee (D7.21)");\n'
        '        }\n'
        '\n'
        '        if (units.FirstOrDefault(unit => Is(unit!, Conditions.FinalFire)) is { } finalFire)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.reaction-fire: {finalFire.Id} is marked Final Fire; FPF CC Reaction Fire is not built (D7.212; ruling R11.13)");\n'
        '        }\n',
        '        var window = state.Movement;\n'
        '        var vehicle = window is { Vehicle: true, Members: [{ } moving] } ? state.Unit(moving) : null;\n'
        f'        if ({C}.ReactionWindowBar(window is {{ WindowOpen: true, Vehicle: true, Members: [_] }} && vehicle is not null && state.Location(vehicle.Id)?.Location == location\n'
        '            && (vehicleId is null || vehicleId == vehicle.Id)) is { } windowBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, windowBar);\n'
        '        }\n'
        '\n'
        '        // D7.1, D7.2 (referee, pass 11): Reaction Fire at an OVRing vehicle comes after the OVR is resolved.\n'
        f'        if ({C}.ReactionOverrunBar(window!.Overrun is not null) is {{ }} overrunBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, overrunBar);\n'
        '        }\n'
        '\n'
        '        var units = attackers.Select(state.Unit).ToArray();\n'
        f'        if ({C}.ReactionAttackersBar(units.Length, units.Any(unit => !{C}.ReactionAttacker(unit is {{ Status: InstanceStatus.Active }}, unit?.Side != vehicle!.Side,\n'
        '            unit is not null && state.Location(unit.Id)?.Location == location, unit is not null && LiveFire.IsVehicle(unit), unit is not null && Is(unit, Conditions.Broken),\n'
        '            unit is not null && Is(unit, Conditions.Pinned), unit is not null && Is(unit, Conditions.Captured), unit is not null && Is(unit, Conditions.Melee))), location.ToString()) is { } attackersBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, attackersBar);\n'
        '        }\n'
        '\n'
        f'        if ({C}.ReactionFinalFireBar(units.FirstOrDefault(unit => Is(unit!, Conditions.FinalFire))?.Id) is {{ }} finalFireBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, finalFireBar);\n'
        '        }\n',
    ),
    (
        '        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle, [.. units.Select(unit => unit!)], true);\n',
        '        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle!, [.. units.Select(unit => unit!)], true);\n',
    ),
    (
        '    private IEnumerable<UnitInstance> CapturedVehicles(GameState state) => state.Units.Where(vehicle => vehicle.Status == InstanceStatus.Active && LiveFire.IsVehicle(vehicle)\n'
        '        && !Is(vehicle, Conditions.Captured) && !Is(vehicle, Conditions.Motion)\n'
        '        && VehicleDefinition(vehicle) is { MainArmament: null, AntiAircraftMg: null, CoaxialMg: null, BowMg: null }\n'
        '        && state.Location(vehicle.Id)?.Location is { } at\n'
        '        && !state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured))\n'
        '        && state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && CcAttacker(unit)))\n'
        '        .OrderBy(vehicle => vehicle.Id, StringComparer.Ordinal);\n',
        f'    private IEnumerable<UnitInstance> CapturedVehicles(GameState state) => state.Units.Where(vehicle => {C}.CapturedVehicle(vehicle.Status == InstanceStatus.Active, LiveFire.IsVehicle(vehicle),\n'
        '        Is(vehicle, Conditions.Captured), Is(vehicle, Conditions.Motion), VehicleDefinition(vehicle) is { MainArmament: null, AntiAircraftMg: null, CoaxialMg: null, BowMg: null },\n'
        '        state.Location(vehicle.Id)?.Location is not null,\n'
        '        state.Location(vehicle.Id)?.Location is { } at && state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured)),\n'
        '        state.Location(vehicle.Id)?.Location is { } there && state.At(there).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && CcAttacker(unit))))\n'
        '        .OrderBy(vehicle => vehicle.Id, StringComparer.Ordinal);\n',
    ),
    (
        '            return Refused(scope, label, expected, $"play.cc-vehicle: {location} holds no vehicle CC left this CCPh (A11.31)");\n'
        '        }\n'
        '\n'
        '        if (state.CloseCombats.Any(item => !item.Closed && item.Location != location))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.cc-vehicle: another Location\'s CC is open; one Location at a time (A11.12)");\n'
        '        }\n',
        f'            return Refused(scope, label, expected, {C}.NoVehicleCcText(location.ToString()));\n'
        '        }\n'
        '\n'
        f'        if ({C}.OtherOpenBar(state.CloseCombats.Any(item => !item.Closed && item.Location != location)) is {{ }} otherBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, otherBar);\n'
        '        }\n',
    ),
    (
        '                return Refused(scope, label, expected, $"play.cc-vehicle-berserk: {owing.Id} is berserk and attacks the enemy vehicle in {location}; its side does not pass (A15.43)");\n',
        f'                return Refused(scope, label, expected, {C}.BerserkNoPassText(owing.Id, location.ToString()));\n',
    ),
    (
        '                [$"play.cc-vehicle: the {next} side passes in {location} and makes no more attacks there (A11.31)"]);\n',
        f'                [{C}.PassSummary(next, location.ToString())]);\n',
    ),
    (
        '        if (vehicleId is null || state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle) || state.Location(vehicle.Id)?.Location != location)\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.cc-vehicle: a CC attack here names the vehicle attacking or attacked; CC between Infantry in a Location holding a vehicle is not built (ruling R11.16)");\n'
        '        }\n',
        '        var vehicle = vehicleId is null ? null : state.Unit(vehicleId);\n'
        f'        if ({C}.VehicleNamedBar(vehicle is {{ Status: InstanceStatus.Active }} && LiveFire.IsVehicle(vehicle) && state.Location(vehicle.Id)?.Location == location) is {{ }} namedBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, namedBar);\n'
        '        }\n',
    ),
    (
        '            if (vehicle.Side != next || attacked.Contains(vehicle.Id))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.cc-vehicle-order: the {next} side attacks next, and each unit attacks once (A11.31)");\n'
        '            }\n'
        '\n'
        '            return PlanVehicleAttacksInfantry(scope, existing, attemptId, expected, label, actor, state, location, vehicle, defenders, attacked, passed);\n',
        f'            if ({C}.VehicleAttackOrderBar(next, vehicle!.Side == next, attacked.Contains(vehicle.Id)) is {{ }} orderBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, orderBar);\n'
        '            }\n'
        '\n'
        '            return PlanVehicleAttacksInfantry(scope, existing, attemptId, expected, label, actor, state, location, vehicle, defenders, attacked, passed);\n',
    ),
    (
        '        if (units.Length is < 1 or > 2 || units.Any(unit => unit is not { } found || !CcAttacker(found) || found.Side != next || found.Side == vehicle.Side\n'
        '            || state.Location(found.Id)?.Location != location || attacked.Contains(found.Id)))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.cc-vehicle-order: the {next} side attacks next, with one or two of its unbroken Infantry units in {location} that have not attacked (A11.31, A11.5)");\n'
        '        }\n'
        '\n'
        '        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle, [.. units.Select(unit => unit!)], false);\n',
        f'        if ({C}.InfantryAttackersBar(units.Length, units.Any(unit => !{C}.SequentialAttacker(unit is not null, unit is not null && CcAttacker(unit), unit?.Side == next,\n'
        '            unit?.Side != vehicle!.Side, unit is not null && state.Location(unit.Id)?.Location == location, unit is not null && attacked.Contains(unit.Id))), next, location.ToString()) is { } attackersBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, attackersBar);\n'
        '        }\n'
        '\n'
        '        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle!, [.. units.Select(unit => unit!)], false);\n',
    ),
    (
        '    private VehicleCloseCombatFacts WithInexperience(GameState state, VehicleCloseCombatFacts facts) => facts with\n'
        '    {\n'
        '        Units = [.. facts.Units!.Select(unit => (state.Unit(unit.UnitId!) is { } found ? Experience.Inexperienced(state, found, catalogs, vocabulary) : ConditionState.Unknown) switch\n'
        '        {\n'
        '            ConditionState.True => unit with { Inexperienced = true },\n'
        '            ConditionState.False => unit with { Inexperienced = false },\n'
        '            _ => unit,\n'
        '        })],\n'
        '    };\n',
        f'    private VehicleCloseCombatFacts WithInexperience(GameState state, VehicleCloseCombatFacts facts) => {C}.WithInexperience(facts,\n'
        '        id => (state.Unit(id) is { } found ? Experience.Inexperienced(state, found, catalogs, vocabulary) : ConditionState.Unknown) switch\n'
        '        {\n'
        '            ConditionState.True => true,\n'
        '            ConditionState.False => false,\n'
        '            _ => null,\n'
        '        });\n',
    ),
    # PlanVehicleAttack's roll loop
    (
        '                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc-vehicle.roll-missing:", StringComparison.Ordinal))\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left an attack on a vehicle it had accepted undecided: " + string.Join("; ", resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var key = missing["asl.a1.cc-vehicle.roll-missing:".Length..];\n'
        '                var split = key.IndexOf(\':\', StringComparison.Ordinal);\n'
        '                var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);\n'
        '                var count = kind == "attack" ? 2 : 1;\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
        f'                if ({C}.VehicleRollMissing(resolution.Reasons) is not {{ }} asked)\n'
        '                {\n'
        f'                    throw new InvalidOperationException({C}.VehicleAttackUndecidedText(resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var (key, kind, rest) = asked;\n'
        f'                var count = {C}.VehicleAttackDice(kind);\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
    ),
    (
        '                rolls = kind switch\n'
        '                {\n'
        '                    "attack" => rolls with { Attack = drawn.Values },\n'
        '                    "unlikelyKill" => rolls with { UnlikelyKill = drawn.Values[0] },\n'
        '                    _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },\n'
        '                };\n'
        '            }\n'
        '\n'
        '            var destroyed = resolution.VehicleResult is VehicleCloseCombatResolution.Eliminated or VehicleCloseCombatResolution.BurningWreck;\n',
        f'                rolls = {C}.WithVehicleAttackRoll(rolls, kind, rest, drawn.Values);\n'
        '            }\n'
        '\n'
        f'            var destroyed = {C}.Destroyed(resolution.VehicleResult);\n',
    ),
    (
        '                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,\n'
        '                    new Dictionary<string, ConditionState> { [Conditions.Immobilized] = ConditionState.True, [Conditions.Motion] = ConditionState.False }), package, null, [recordId]));\n',
        '                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,\n'
        f'                    ConditionChanges({C}.ImmobilizedConditions())), package, null, [recordId]));\n',
    ),
    (
        '                foreach (var unit in units.Where(unit => !resolution.Effects.Any(effect => effect.UnitId == unit.Id && (effect.Eliminated || effect.FinalDefinitionId != effect.DefinitionId))))\n'
        '                {\n'
        '                    var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);\n'
        '                    if (Is(unit, Conditions.FirstFire))\n'
        '                    {\n'
        '                        conditions[Conditions.FinalFire] = ConditionState.True;\n'
        '                        conditions[Conditions.FirstFire] = ConditionState.False;\n'
        '                    }\n'
        '                    else\n'
        '                    {\n'
        '                        conditions[Conditions.FirstFire] = ConditionState.True;\n'
        '                    }\n'
        '\n'
        '                    if (!destroyed)\n'
        '                    {\n'
        '                        conditions[Conditions.CcReaction] = ConditionState.True;\n'
        '                    }\n'
        '\n'
        '                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, conditions), package, null, [recordId]));\n',
        f'                foreach (var unit in units.Where(unit => {C}.Survives(resolution.Effects, unit.Id)))\n'
        '                {\n'
        f'                    var conditions = ConditionChanges({C}.ReactionFireConditions(Is(unit, Conditions.FirstFire), destroyed));\n'
        '                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, conditions), package, null, [recordId]));\n',
    ),
    (
        '        var paatc = testing.Length > 0 ? $"; {string.Join(", ", testing.Select(unit => unit.Id))} first pass{(testing.Length == 1 ? "es" : string.Empty)} a PAATC (A11.6)" : string.Empty;\n'
        '        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],\n'
        '            [$"play.cc-vehicle: {string.Join(" and ", units.Select(unit => unit.Id))} attack{(units.Count == 1 ? "s" : string.Empty)} {vehicle.Id} in CC{(reaction ? " as CC Reaction Fire (D7.21)" : " (A11.5)")}{paatc}"])\n',
        f'        var paatc = {C}.PaatcText([.. testing.Select(unit => unit.Id)]);\n'
        '        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],\n'
        f'            [{C}.VehicleAttackSummary([.. units.Select(unit => unit.Id)], vehicle.Id, reaction, paatc)])\n',
    ),
    # PlanVehicleAttacksInfantry's roll loop
    (
        '                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc-vehicle.roll-missing:", StringComparison.Ordinal))\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left a vehicle\'s attack it had accepted undecided: " + string.Join("; ", resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var key = missing["asl.a1.cc-vehicle.roll-missing:".Length..];\n'
        '                var split = key.IndexOf(\':\', StringComparison.Ordinal);\n'
        '                var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);\n'
        '                var selected = kind == "randomSelection" ? rest.Split(\',\') : [];\n'
        '                var count = kind switch\n'
        '                {\n'
        '                    "attack" => 2,\n'
        '                    "randomSelection" => selected.Length,\n'
        '                    _ => 1,\n'
        '                };\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
        f'                if ({C}.VehicleRollMissing(resolution.Reasons) is not {{ }} asked)\n'
        '                {\n'
        f'                    throw new InvalidOperationException({C}.VehicleOwnAttackUndecidedText(resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var (key, kind, rest) = asked;\n'
        f'                var selected = {C}.RandomSelected(kind, rest);\n'
        f'                var count = {C}.VehicleInfantryDice(kind, selected.Length);\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
    ),
    (
        '                rolls = kind switch\n'
        '                {\n'
        '                    "attack" => rolls with { Attack = drawn.Values },\n'
        '                    "randomSelection" => rolls with\n'
        '                    {\n'
        '                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection, (map, pair) => With(map, pair.id, drawn.Values[pair.index])),\n'
        '                    },\n'
        '                    _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },\n'
        '                };\n',
        f'                rolls = {C}.WithVehicleInfantryRoll(rolls, kind, rest, selected, drawn.Values);\n',
    ),
    (
        '            [$"play.cc-vehicle: {vehicle.Id} attacks {string.Join(", ", defenders)} in CC with its CC armament (A11.62)"])\n',
        f'            [{C}.VehicleAttacksInfantrySummary(vehicle.Id, defenders)])\n',
    ),
])
