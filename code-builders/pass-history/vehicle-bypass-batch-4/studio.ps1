param([switch] $StartOnly)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
$taskWork = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_GATE_WORK = $taskWork
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$project = "$taskRoot/src/ASL/LimboDancer.Domains.Asl.MapStudio"
if (Get-NetTCPConnection -LocalPort 6680 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 6680 is occupied.' }
if (!$StartOnly) {
    & 'C:/Program Files/Git/bin/bash.exe' "$taskRoot/code-builders/build-and-test/outside_outputs.sh" register "$taskWork/studio-6680"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build "$project/LimboDancer.Domains.Asl.MapStudio.csproj" --disable-build-servers -m:1 '-p:RestoreLockedMode=true' "-p:OutDir=$taskWork/studio-6680/" --warnaserror -v:q -nologo *> "$taskWork/vehicle-bypass-batch4-build.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$taskWork/vehicle-bypass-batch4-build.log" -Tail 25; exit $LASTEXITCODE }
}
$live = "$taskWork/vehicle-bypass-batch4-boards/units/live/5a7d1f000000400080000000000057d0"
New-Item -ItemType Directory -Force $live | Out-Null
foreach ($bundle in @('d2321-bypass-tca', 'd233-bypass-vca')) {
    Copy-Item -LiteralPath "$taskRoot/src/ASL/units/minigames/$bundle/games/$bundle-position.game.json" -Destination $live
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$process = Start-Process dotnet -ArgumentList @("$taskWork/studio-6680/LimboDancer.Domains.Asl.MapStudio.dll", '--contentRoot', $project,
    '--Kestrel:Endpoints:Http:Url', 'http://localhost:6680', '--AslMaps:VaslRoot', 'E:/Archive/GitHub/dlandi/vasl',
    '--AslMaps:BoardsRoot', "$taskWork/vehicle-bypass-batch4-boards", '--AslMaps:CacheRoot', "$taskWork/vehicle-bypass-batch4-cache") -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput "$taskWork/vehicle-bypass-batch4-studio.log" -RedirectStandardError "$taskWork/vehicle-bypass-batch4-studio-error.log"
$process.Id | Set-Content "$taskWork/vehicle-bypass-batch4-studio.pid"
Write-Output "Codex Studio started on port 6680, process $($process.Id)."
