param([string[]]$Only = @(), [string]$RunName = 'outcome-checks')
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK to the Codex working folder first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$outputPath = Join-Path $env:ASL_GATE_WORK 'action-outcome/checks'
$logPath = Join-Path $env:ASL_GATE_WORK "action-outcome/$RunName.log"
$resultsPath = Join-Path $env:ASL_GATE_WORK 'action-outcome/results'
$registerScript = "$PSScriptRoot/outside_outputs.sh".Replace('\', '/')
& 'C:/Program Files/Git/bin/bash.exe' $registerScript register $outputPath.Replace('\', '/')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if ($Only.Count -gt 0) {
    $filters = @($Only | ForEach-Object { "FullyQualifiedName~$_" })
} else {
    $filters = @(Get-Content "$PSScriptRoot/../pass-history/action-outcome/affected-checks.txt" | ForEach-Object { "FullyQualifiedName~$_" })
    $filters += 'FullyQualifiedName~ActionOutcomeTests'
    $filters += 'FullyQualifiedName~IconCoverageTests.TheCoverageFileMatchesTheSources'
}
$env:ASL_ICONS_UPDATE_COVERAGE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
& dotnet test "$repoRoot/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/LimboDancer.Domains.Asl.MapStudio.Tests.csproj" --disable-build-servers -m:1 "-p:OutDir=$outputPath/" -p:RestoreLockedMode=true --warnaserror --filter ($filters -join '|') --logger "trx;LogFileName=$RunName.trx" --results-directory $resultsPath -v:q -nologo *> $logPath
$checkExit = $LASTEXITCODE
Get-Content $logPath -Tail 60
exit $checkExit
