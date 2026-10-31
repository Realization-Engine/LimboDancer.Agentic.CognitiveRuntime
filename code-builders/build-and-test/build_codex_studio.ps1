param([string]$OutputName = 'action-outcome/studio-6680')
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK to the Codex working folder first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$outputPath = Join-Path $env:ASL_GATE_WORK $OutputName
$logPath = Join-Path $env:ASL_GATE_WORK 'build-codex-studio.log'
$registerScript = "$PSScriptRoot/outside_outputs.sh".Replace('\', '/')
& 'C:/Program Files/Git/bin/bash.exe' $registerScript register $outputPath.Replace('\', '/')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
& dotnet build "$repoRoot/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" --disable-build-servers -m:1 "-p:OutDir=$outputPath/" -p:RestoreLockedMode=true --warnaserror -v:q -nologo *> $logPath
$buildExit = $LASTEXITCODE
Get-Content $logPath -Tail 45
exit $buildExit
