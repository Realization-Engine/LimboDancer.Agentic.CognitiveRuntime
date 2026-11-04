# Build and start only Codex's isolated Studio and its four manufactured positions.
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
$taskWork = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_GATE_WORK = $taskWork
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$project = "$taskRoot/src/ASL/LimboDancer.Domains.Asl.MapStudio"
dotnet build "$project/LimboDancer.Domains.Asl.MapStudio.csproj" --no-restore --disable-build-servers -m:1 "-p:OutDir=$taskWork/studio-6680/" --warnaserror -v:q -nologo *> "$taskWork/nationality-build.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$taskWork/nationality-build.log" -Tail 25; exit $LASTEXITCODE }
$live = "$taskWork/nationality-boards/units/live/5a7d1f000000400080000000000057d0"
New-Item -ItemType Directory -Force $live | Out-Null
foreach ($bundle in @('a25-italian-deployment','a25-italian-capture','a25-finnish-organization','a25-finnish-equipment')) {
    Copy-Item -LiteralPath "$taskRoot/src/ASL/units/minigames/$bundle/games/$bundle-position.game.json" -Destination $live
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$process = Start-Process dotnet -ArgumentList @("$taskWork/studio-6680/LimboDancer.Domains.Asl.MapStudio.dll", '--contentRoot', $project,
    '--Kestrel:Endpoints:Http:Url', 'http://localhost:6680', '--AslMaps:VaslRoot', 'E:/Archive/GitHub/dlandi/vasl',
    '--AslMaps:BoardsRoot', "$taskWork/nationality-boards", '--AslMaps:CacheRoot', "$taskWork/nationality-cache") -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput "$taskWork/nationality-studio.log" -RedirectStandardError "$taskWork/nationality-studio-error.log"
$process.Id | Set-Content "$taskWork/nationality-studio.pid"
Write-Output "Codex Studio started on port 6680, process $($process.Id)."
