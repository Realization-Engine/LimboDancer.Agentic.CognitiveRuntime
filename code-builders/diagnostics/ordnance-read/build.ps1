param([Parameter(Mandatory)][string]$BaselineBin, [Parameter(Mandatory)][string]$CurrentBin)
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK before building this probe.' }
$outputRoot = Join-Path $env:ASL_GATE_WORK 'ordnance-read'
$toolsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& 'C:/Program Files/Git/bin/bash.exe' (Join-Path $toolsRoot 'build-and-test/outside_outputs.sh') register $outputRoot
if ($LASTEXITCODE -ne 0) { throw 'Output registration failed.' }
foreach ($entry in @(@('before', $BaselineBin), @('after', $CurrentBin))) {
    $name = $entry[0]
    $output = Join-Path $outputRoot $name
    $intermediate = Join-Path $outputRoot "$name-obj/"
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    & dotnet build (Join-Path $PSScriptRoot 'OrdnanceRead.csproj') --disable-build-servers -m:1 -v:q `
        '-p:RestoreLockedMode=true' "-p:StudioBin=$($entry[1])/" "-p:BaseIntermediateOutputPath=$intermediate" "-p:OutDir=$output/" `
        *> (Join-Path $outputRoot "$name-build.log")
    if ($LASTEXITCODE -ne 0) {
        Get-Content (Join-Path $outputRoot "$name-build.log") -Tail 15
        throw "$name probe build failed."
    }
    Write-Output "$name probe built: $output"
}
