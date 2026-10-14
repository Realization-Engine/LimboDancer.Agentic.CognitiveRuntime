$ErrorActionPreference = 'Stop'
Set-Location 'E:\Archive\GitHub\dlandi\LimboDancer.MCP'
$utf8 = New-Object Text.UTF8Encoding $false

function Swap([string]$text, [string]$old, [string]$new) {
    $count = ([regex]::Matches($text, [regex]::Escape($old))).Count
    if ($count -ne 1) { throw "expected one match, found $count for: $old" }
    return $text.Replace($old, $new)
}

$path = 'src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.VehicleConcealment.cs'
$text = (git show "HEAD:$path") -join "`n"
$text += "`n"
$text = Swap $text 'ScenarioA1VehicleMovementCalculator.ConcealmentTerrain(' 'ScenarioA1VehicleSightRules.ConcealmentTerrain('
$text = Swap $text 'ScenarioA1VehicleMovementCalculator.Watching(' 'ScenarioA1VehicleSightRules.Watching('
$text = Swap $text 'ScenarioA1VehicleMovementCalculator.ConcealmentLost(' 'ScenarioA1VehicleSightRules.ConcealmentLost('
$text = Swap $text 'if (within is { } limit && (HexDistance(state, from, at) is not { } range || range > limit))' 'if (!ScenarioA1VehicleSightRules.WithinRange(within, () => HexDistance(state, from, at)))'
[IO.File]::WriteAllText((Join-Path (Get-Location) $path), $text, $utf8)

$path = 'src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Wrecks.cs'
$text = (git show "HEAD:$path") -join "`n"
$text += "`n"
$text = Swap $text 'ScenarioA1VehicleTerrainCosts.Standing(' 'ScenarioA1VehicleSightRules.Standing('
$text = Swap $text 'ScenarioA1VehicleTerrainCosts.SmokeDrm(' 'ScenarioA1VehicleSightRules.SmokeDrm('
$text = Swap $text 'if (mapRanges.Contains(crossed.Range) && !ScenarioA1VehicleTerrainCosts.AddsToOwnHindrance(ownTerrain, state.ScenarioMonth))' 'if (!ScenarioA1VehicleSightRules.HindersAtRange(mapRanges.Contains(crossed.Range), ownTerrain, state.ScenarioMonth))'
$oldCover = @(
    '        return WrecksAt(state, at).FirstOrDefault(wreck => !IsBurning(state, wreck) && Standing(state, wreck))?.Id',
    '            ?? state.At(at).OfType<UnitInstance>().FirstOrDefault(unit => IsAfv(unit) && (unit.Side == infantrySide || Is(unit, Conditions.Abandoned))',
    '                && Standing(state, unit))?.Id;') -join "`n"
$newCover = @(
    '        return ScenarioA1VehicleSightRules.Cover(',
    '            WrecksAt(state, at).Select(wreck => (wreck.Id, (Func<bool>)(() => IsBurning(state, wreck)), (Func<bool>)(() => Standing(state, wreck)))),',
    '            state.At(at).OfType<UnitInstance>().Select(unit => (unit.Id, (Func<bool>)(() => IsAfv(unit)),',
    '                (Func<bool>)(() => unit.Side == infantrySide || Is(unit, Conditions.Abandoned)), (Func<bool>)(() => Standing(state, unit)))));') -join "`n"
$text = Swap $text $oldCover $newCover
[IO.File]::WriteAllText((Join-Path (Get-Location) $path), $text, $utf8)
'done'
