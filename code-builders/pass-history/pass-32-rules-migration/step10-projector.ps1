$ErrorActionPreference = 'Stop'
Set-Location E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL
$path = (Resolve-Path 'LimboDancer.Domains.Asl.Units/State/GameProjector.cs').Path
$text = [IO.File]::ReadAllText($path, (New-Object Text.UTF8Encoding $false))
$P = 'Rules.ScenarioA1VehicleProjection'
$pairs = [System.Collections.Generic.List[object]]::new()
    $pairs.Add(@(
        '            if (state.Phase != "mph" || Active(state, step.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")' + "`n" + '                || vehicle.Side != state.PhasingSide || vehicle.MovementEnded || step.HalfMp < 0 || (step.HalfMp == 0 && step.Kind != VehicleStepped.Load))',
        '            if (Active(state, step.Vehicle) is not UnitInstance vehicle || !' + $P + '.StepAllowed(state.Phase, vocabulary.IsA(vehicle.Kind, "asl:vehicle"),' + "`n" + '                vehicle.Side == state.PhasingSide, vehicle.MovementEnded, step.HalfMp, step.Kind == VehicleStepped.Load))'
    ))
    $pairs.Add(@(
        'var recalling = GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True && GameState.Condition(vehicle, Conditions.StunRecovery) != ConditionState.True;',
        'var recalling = ' + $P + '.Recalling(GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True);'
    ))
    $pairs.Add(@(
        'step.Facing is { } turned && Math.Abs(((int)turned - (int)facing + 6) % 6) is 1 or 5',
        'step.Facing is { } turned && ' + $P + '.OneHexspine((int)turned, (int)facing)'
    ))
    $pairs.Add(@(
        '                var spent = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + step.HalfMp;' + "`n" + '                var loaded = Replace(state, vehicle with' + "`n" + '                {' + "`n" + '                    MfSpent = spent / 2,' + "`n" + '                    HalfMfSpent = spent % 2 == 1,',
        '                var (loadMf, loadHalfMf) = ' + $P + '.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);' + "`n" + '                var loaded = Replace(state, vehicle with' + "`n" + '                {' + "`n" + '                    MfSpent = loadMf,' + "`n" + '                    HalfMfSpent = loadHalfMf,'
    ))
    $pairs.Add(@(
        '            var halves = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + step.HalfMp;' + "`n" + '            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);' + "`n" + '            if (inMotion)',
        '            var (stepMf, stepHalfMf) = ' + $P + '.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);' + "`n" + '            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);' + "`n" + '            if (inMotion)'
    ))
    $pairs.Add(@(
        '                MfSpent = halves / 2,' + "`n" + '                HalfMfSpent = halves % 2 == 1,' + "`n" + '                Conditions = conditions,' + "`n" + '                Straddling = step.Kind',
        '                MfSpent = stepMf,' + "`n" + '                HalfMfSpent = stepHalfMf,' + "`n" + '                Conditions = conditions,' + "`n" + '                Straddling = step.Kind'
    ))
    $pairs.Add(@(
        'var final = (check.Check == VehicleCheckRolled.BogRemoval ? roll.Values[0] : roll.Values[0] + roll.Values[1]) + check.Drm;',
        'var final = ' + $P + '.CheckFinal(check.Check == VehicleCheckRolled.BogRemoval, roll.Values[0], roll.Values[1], check.Drm);'
    ))
    $pairs.Add(@(
        '            var stuck = check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized' + "`n" + '                || (check.Check == VehicleCheckRolled.BogRemoval && check.Result != VehicleCheckRolled.Freed);',
        '            var stuck = ' + $P + '.CheckStops(check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized, check.Check == VehicleCheckRolled.BogRemoval,' + "`n" + '                check.Result == VehicleCheckRolled.Freed);'
    ))
    $pairs.Add(@(
        '            if (state.Movement is not { Vehicle: true, WindowOpen: false } movement || movement.Overrun != overrun.At' + "`n" + '                || !movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal) || !fires.ContainsKey(overrun.Fire))',
        '            if (state.Movement is not { } movement || !' + $P + '.OverrunResolvable(movement is { Vehicle: true, WindowOpen: false }, movement.Overrun == overrun.At,' + "`n" + '                () => movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal), () => fires.ContainsKey(overrun.Fire)))'
    ))
    $pairs.Add(@(
        '|| (roll.Values[0] + roll.Values[1] + paatc.Drm <= paatc.Morale) != paatc.Passed)',
        '|| ' + $P + '.PaatcPassed(roll.Values[0], roll.Values[1], paatc.Drm, paatc.Morale) != paatc.Passed)'
    ))
    $pairs.Add(@(
        '                    Status = container.Status == InstanceStatus.Exited ? InstanceStatus.Exited : InstanceStatus.Eliminated,' + "`n" + '                })!;',
        '                    Status = ' + $P + '.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated,' + "`n" + '                })!;'
    ))
    $pairs.Add(@(
        '? item with { Status = container.Status == InstanceStatus.Exited ? InstanceStatus.Exited : InstanceStatus.Eliminated }',
        '? item with { Status = ' + $P + '.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated }'
    ))
    $pairs.Add(@(
        'if (Active(state, wreck.Id) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle") || state.Location(vehicle.Id) is null)',
        'if (Active(state, wreck.Id) is not UnitInstance vehicle || !' + $P + '.WreckAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"), () => state.Location(vehicle.Id) is not null))'
    ))
foreach ($pair in $pairs) {
    $count = ([regex]::Matches($text, [regex]::Escape($pair[0]))).Count
    if ($count -ne 1) { throw "Expected one match, found $count for: $($pair[0])" }
    $text = $text.Replace($pair[0], $pair[1])
}
[IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false))
'applied ' + $pairs.Count
