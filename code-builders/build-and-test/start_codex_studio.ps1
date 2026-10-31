param([string]$OutputName = 'action-outcome/studio-6680')
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$outputPath = Join-Path $env:ASL_GATE_WORK $OutputName
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:6680'
$env:Kestrel__Endpoints__Http__Url = 'http://localhost:6680'
$env:AslMaps__BoardsRoot = 'E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards'
$studioRoot = Join-Path $repoRoot 'src/ASL/LimboDancer.Domains.Asl.MapStudio'
$process = Start-Process dotnet -ArgumentList @("$outputPath/LimboDancer.Domains.Asl.MapStudio.dll", '--contentRoot', $studioRoot) -WorkingDirectory $studioRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "$env:ASL_GATE_WORK/studio-6680.log" -RedirectStandardError "$env:ASL_GATE_WORK/studio-6680-error.log"
$process.Id | Set-Content "$env:ASL_GATE_WORK/studio-6680.pid"
Write-Output "Studio process $($process.Id), http://localhost:6680"
