$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$taskRoot = Join-Path $env:ASL_GATE_WORK 'c617-recovery-history'
$dataRoot = Join-Path $taskRoot 'boards'
$liveRoot = Join-Path $dataRoot 'units/live/5a7d1f000000400080000000000057d0'
New-Item -ItemType Directory -Force -Path $liveRoot | Out-Null
$demo = Join-Path $liveRoot 'ui-c617-rules-01.game.json'
if (-not (Test-Path -LiteralPath $demo)) {
    Copy-Item -LiteralPath "$repoRoot/src/ASL/units/minigames/c617-recovery-history/games/ui-c617-rules-01.game.json" -Destination $demo
}
$env:AslMaps__BoardsRoot = $dataRoot
$env:AslMaps__VaslRoot = 'E:/Archive/GitHub/dlandi/vasl'
$env:AslMaps__OracleFixtures = "$repoRoot/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:6680'
$env:Kestrel__Endpoints__Http__Url = 'http://localhost:6680'
$studioRoot = "$repoRoot/src/ASL/LimboDancer.Domains.Asl.MapStudio"
$process = Start-Process dotnet -ArgumentList @("$taskRoot/studio-6680/LimboDancer.Domains.Asl.MapStudio.dll", '--contentRoot', $studioRoot) -WorkingDirectory $studioRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "$taskRoot/studio.log" -RedirectStandardError "$taskRoot/studio-error.log"
$process.Id | Set-Content "$taskRoot/studio.pid"
Write-Output "C6.17 Studio process $($process.Id), isolated data: $dataRoot"
