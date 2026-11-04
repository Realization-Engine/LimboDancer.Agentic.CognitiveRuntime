$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_REPO = (Resolve-Path "$PSScriptRoot/../../..").Path
function Check-Results([string] $project) {
    [xml] $result = Get-Content "$env:ASL_GATE_WORK/test-classes/$project.trx"
    $counts = $result.TestRun.ResultSummary.Counters
    if ([int]$counts.failed -ne 0 -or [int]$counts.total -eq 0) { throw "$project tests did not all pass." }
    Copy-Item "$env:ASL_GATE_WORK/test-classes/$project.trx" "$env:ASL_GATE_WORK/ordnance-batch3-$project.trx"
    Copy-Item "$env:ASL_GATE_WORK/test-classes/$project.log" "$env:ASL_GATE_WORK/ordnance-batch3-$project.log"
}
$methods = @(
    'ScenarioA1AmmunitionDefaultTests.DefaultsFollowProtectionRatherThanVehicleIdentity',
    'ScenarioA1AmmunitionDefaultTests.TargetTypeRestrictionsNeverBecomeSubstituteAmmunition',
    'ScenarioA1AmmunitionDefaultTests.KnownStockObstaclesRequireDeclarationWithoutFallbackOrIrrelevantFacts',
    'ScenarioA1AmmunitionDefaultTests.UnknownAndUnsupportedInputsNeverBecomeOrdinaryDefaults',
    'ScenarioA1AmmunitionDefaultTests.CounterAdapterDistinguishesListedNumbersFromDatedAvailability',
    'ScenarioA1AmmunitionDefaultTests.ContractPreservesUnknownsExplicitDeclarationsAndPrivateAnchors',
    'ScenarioA1AmmunitionDefaultTests.LegacyReadingsKeepTheirIntentionalDifferencesAndExactStrings',
    'ScenarioA1Pass55RulesTests.AGunWhoseHeHasADepletionNumberNeverFiresItUndeclared',
    'ScenarioA1Pass55RulesTests.AGunDuelIsBarredByEachConditionOfTheRule',
    'ScenarioA1Pass55RulesTests.AmmunitionDefaultExtractionPreservesDuelRefusalOrderAndStrings',
    'ScenarioA1RuleContractTests.EveryContractHasAUniqueIdAndRulesThatAreRowsOfTheInventory'
)
$filter = ($methods | ForEach-Object { 'FullyQualifiedName~' + $_ }) -join '|'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Rules $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Check-Results 'Rules'
$filter = 'FullyQualifiedName~MinigameBundleTests.EveryBundleIsSound|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&DisplayName~c221-ammunition-default)|FullyQualifiedName~MinigameBundleTests.EveryGameInABundlesFolderNamesItsBundle'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Play $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Check-Results 'Play'
