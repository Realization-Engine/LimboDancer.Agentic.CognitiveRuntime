import pathlib, sys

path = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Units\State\GameProjector.cs")
text = path.read_bytes().decode("utf-8")
P = "Rules.ScenarioA1VehicleProjection"
pairs = [
    (
        '            if (state.Phase != "mph" || Active(state, step.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")\n'
        '                || vehicle.Side != state.PhasingSide || vehicle.MovementEnded || step.HalfMp < 0 || (step.HalfMp == 0 && step.Kind != VehicleStepped.Load))',
        f'            if (Active(state, step.Vehicle) is not UnitInstance vehicle || !{P}.StepAllowed(state.Phase, vocabulary.IsA(vehicle.Kind, "asl:vehicle"),\n'
        '                vehicle.Side == state.PhasingSide, vehicle.MovementEnded, step.HalfMp, step.Kind == VehicleStepped.Load))',
    ),
    (
        'var recalling = GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True && GameState.Condition(vehicle, Conditions.StunRecovery) != ConditionState.True;',
        f'var recalling = {P}.Recalling(GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True);',
    ),
    (
        'step.Facing is { } turned && Math.Abs(((int)turned - (int)facing + 6) % 6) is 1 or 5',
        f'step.Facing is {{ }} turned && {P}.OneHexspine((int)turned, (int)facing)',
    ),
    (
        '                var spent = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + step.HalfMp;\n'
        '                var loaded = Replace(state, vehicle with\n'
        '                {\n'
        '                    MfSpent = spent / 2,\n'
        '                    HalfMfSpent = spent % 2 == 1,',
        f'                var (loadMf, loadHalfMf) = {P}.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);\n'
        '                var loaded = Replace(state, vehicle with\n'
        '                {\n'
        '                    MfSpent = loadMf,\n'
        '                    HalfMfSpent = loadHalfMf,',
    ),
    (
        '            var halves = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + step.HalfMp;\n'
        '            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);\n'
        '            if (inMotion)',
        f'            var (stepMf, stepHalfMf) = {P}.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);\n'
        '            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);\n'
        '            if (inMotion)',
    ),
    (
        '                MfSpent = halves / 2,\n'
        '                HalfMfSpent = halves % 2 == 1,\n'
        '                Conditions = conditions,\n'
        '                Straddling = step.Kind',
        '                MfSpent = stepMf,\n'
        '                HalfMfSpent = stepHalfMf,\n'
        '                Conditions = conditions,\n'
        '                Straddling = step.Kind',
    ),
    (
        'var final = (check.Check == VehicleCheckRolled.BogRemoval ? roll.Values[0] : roll.Values[0] + roll.Values[1]) + check.Drm;',
        f'var final = {P}.CheckFinal(check.Check == VehicleCheckRolled.BogRemoval, roll.Values[0], roll.Values[1], check.Drm);',
    ),
    (
        '            var stuck = check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized\n'
        '                || (check.Check == VehicleCheckRolled.BogRemoval && check.Result != VehicleCheckRolled.Freed);',
        f'            var stuck = {P}.CheckStops(check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized, check.Check == VehicleCheckRolled.BogRemoval,\n'
        '                check.Result == VehicleCheckRolled.Freed);',
    ),
    (
        '            if (state.Movement is not { Vehicle: true, WindowOpen: false } movement || movement.Overrun != overrun.At\n'
        '                || !movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal) || !fires.ContainsKey(overrun.Fire))',
        f'            if (state.Movement is not {{ }} movement || !{P}.OverrunResolvable(movement is {{ Vehicle: true, WindowOpen: false }}, movement.Overrun == overrun.At,\n'
        '                () => movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal), () => fires.ContainsKey(overrun.Fire)))',
    ),
    (
        '|| (roll.Values[0] + roll.Values[1] + paatc.Drm <= paatc.Morale) != paatc.Passed)',
        f'|| {P}.PaatcPassed(roll.Values[0], roll.Values[1], paatc.Drm, paatc.Morale) != paatc.Passed)',
    ),
    (
        '                    Status = container.Status == InstanceStatus.Exited ? InstanceStatus.Exited : InstanceStatus.Eliminated,\n'
        '                })!;',
        f'                    Status = {P}.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated,\n'
        '                })!;',
    ),
    (
        '? item with { Status = container.Status == InstanceStatus.Exited ? InstanceStatus.Exited : InstanceStatus.Eliminated }',
        f'? item with {{ Status = {P}.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated }}',
    ),
    (
        'if (Active(state, wreck.Id) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle") || state.Location(vehicle.Id) is null)',
        f'if (Active(state, wreck.Id) is not UnitInstance vehicle || !{P}.WreckAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"), () => state.Location(vehicle.Id) is not null))',
    ),
]
for old, new in pairs:
    n = text.count(old)
    if n != 1:
        sys.exit(f"expected 1 match, found {n}: {old[:90]}")
for old, new in pairs:
    text = text.replace(old, new)
path.write_bytes(text.encode("utf-8"))
print("applied", len(pairs))
