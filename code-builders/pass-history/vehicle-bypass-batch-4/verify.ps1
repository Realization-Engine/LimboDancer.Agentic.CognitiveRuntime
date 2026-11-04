param([switch] $RulesOnly)
$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_REPO = (Resolve-Path "$PSScriptRoot/../../..").Path
function Check-Results([string] $project) {
    [xml] $result = Get-Content "$env:ASL_GATE_WORK/test-classes/$project.trx"
    $counts = $result.TestRun.ResultSummary.Counters
    if ([int]$counts.failed -ne 0 -or [int]$counts.total -eq 0) { throw "$project tests did not all pass." }
    Copy-Item "$env:ASL_GATE_WORK/test-classes/$project.trx" "$env:ASL_GATE_WORK/vehicle-bypass-batch4-$project.trx"
    Copy-Item "$env:ASL_GATE_WORK/test-classes/$project.log" "$env:ASL_GATE_WORK/vehicle-bypass-batch4-$project.log"
}
$filter = 'FullyQualifiedName~ScenarioA1BypassTurretTests|FullyQualifiedName~ScenarioA1BypassVcaTests|FullyQualifiedName~AVehicleMayMoveOnWithANonAllEntryOrATurnAndOneMp|FullyQualifiedName~ATurnIsOneHexspineAndNotAgainAtTheCafp|FullyQualifiedName~ALeavingVehicleDoesNotStopAndReadsNothingElse|FullyQualifiedName~ScenarioA1RuleContractTests.EveryContractHasAUniqueIdAndRulesThatAreRowsOfTheInventory|FullyQualifiedName~ScenarioA1RuleContractTests.AMissingAnUnknownOrAMistypedFactGivesNotEvaluated|FullyQualifiedName~ScenarioA1RuleContractTests.EveryFactTypeRoundTripsThroughTheJsonEntryPoint'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Rules $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Check-Results 'Rules'
if ($RulesOnly) { exit 0 }
$filter = 'FullyQualifiedName~MinigameBundleTests.EveryBundleIsSound|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&DisplayName~d2321-bypass-tca)|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&DisplayName~d233-bypass-vca)|FullyQualifiedName~MinigameBundleTests.EveryGameInABundlesFolderNamesItsBundle|FullyQualifiedName~BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Play $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Check-Results 'Play'
