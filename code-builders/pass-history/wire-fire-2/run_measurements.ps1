param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$BaselineCommit,
    [Parameter(Mandatory)][string]$CurrentCommit
)

$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK first.' }
$env:ASL_REPO = $Repository
$env:DOTNET_TieredCompilation = '0'
foreach ($runName in @('baseline-1', 'current-1', 'current-2', 'baseline-2')) {
    $baseline = $runName.StartsWith('baseline')
    $probeKind = if ($baseline) { 'base' } else { 'head' }
    $commit = if ($baseline) { $BaselineCommit } else { $CurrentCommit }
    $probe = Join-Path $env:ASL_GATE_WORK "pr85-measure-$probeKind/bin/Debug/net10.0/Measure.dll"
    $recordStore = Join-Path $env:ASL_GATE_WORK 'store'
    $result = Join-Path $PSScriptRoot "$runName.json"
    $log = Join-Path $env:ASL_GATE_WORK "pr85-measure-$runName.log"
    & dotnet $probe $Repository $recordStore $result $commit *> $log
    if ($LASTEXITCODE -ne 0) { throw "$runName failed; read $log" }
    Write-Output "$runName completed"
}
