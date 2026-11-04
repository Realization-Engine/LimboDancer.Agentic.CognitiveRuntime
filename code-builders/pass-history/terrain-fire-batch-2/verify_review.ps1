$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_REPO = (Resolve-Path "$PSScriptRoot/../../..").Path
foreach ($project in @('Rules', 'Play')) {
    foreach ($extension in @('log', 'trx')) {
        $source = "$env:ASL_GATE_WORK/test-classes/$project.$extension"
        $archive = "$env:ASL_GATE_WORK/terrain-fire-original-$project.$extension"
        if ((Test-Path -LiteralPath $source) -and !(Test-Path -LiteralPath $archive)) { Copy-Item -LiteralPath $source -Destination $archive }
    }
}
$filters = @{
    Rules = 'FullyQualifiedName~ScenarioA1BuildingFireTests.BuildingContractRequiresPublicContextAndMaterialPrivateAnchors'
    Play = 'FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&DisplayName~b23-building-fire'
}
foreach ($project in @('Rules', 'Play')) {
    & 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" $project $filters[$project] '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    [xml] $result = Get-Content "$env:ASL_GATE_WORK/test-classes/$project.trx"
    if ([int]$result.TestRun.ResultSummary.Counters.failed -ne 0 -or [int]$result.TestRun.ResultSummary.Counters.total -eq 0) { throw "$project checks did not pass." }
}
