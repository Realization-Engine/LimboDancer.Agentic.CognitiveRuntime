param([string]$OutputName = 'action-outcome/studio-6680')
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$outputPath = Join-Path $env:ASL_GATE_WORK $OutputName
$oracleFixtures = Join-Path $repoRoot 'src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle'
if (-not (Test-Path -LiteralPath $oracleFixtures -PathType Container)) {
    throw "Board verification fixtures are missing: $oracleFixtures"
}
$env:AslMaps__OracleFixtures = $oracleFixtures
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:6680'
$env:Kestrel__Endpoints__Http__Url = 'http://localhost:6680'
$env:AslMaps__BoardsRoot = 'E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards'
$studioRoot = Join-Path $repoRoot 'src/ASL/LimboDancer.Domains.Asl.MapStudio'
$process = Start-Process dotnet -ArgumentList @("$outputPath/LimboDancer.Domains.Asl.MapStudio.dll", '--contentRoot', $studioRoot) -WorkingDirectory $studioRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "$env:ASL_GATE_WORK/studio-6680.log" -RedirectStandardError "$env:ASL_GATE_WORK/studio-6680-error.log"
$process.Id | Set-Content "$env:ASL_GATE_WORK/studio-6680.pid"
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    $process.Refresh()
    if ($process.HasExited) { throw "Studio exited during startup. Read $env:ASL_GATE_WORK/studio-6680-error.log" }
    $listening = Get-NetTCPConnection -LocalPort 6680 -State Listen -ErrorAction SilentlyContinue | Where-Object OwningProcess -EQ $process.Id
    if ($listening) {
        Write-Output "Studio process $($process.Id), listening at http://localhost:6680"
        exit 0
    }
    Start-Sleep -Milliseconds 500
}
throw "Studio did not start listening. Read $env:ASL_GATE_WORK/studio-6680.log"
