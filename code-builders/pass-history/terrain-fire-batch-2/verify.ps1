# Run only the methods covering the new calculators, contracts, compatibility edge and bundles.
$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_REPO = (Resolve-Path "$PSScriptRoot/../../..").Path
function Assert-Results([string] $project) {
    [xml] $result = Get-Content "$env:ASL_GATE_WORK/test-classes/$project.trx"
    $counts = $result.TestRun.ResultSummary.Counters
    if ([int]$counts.failed -ne 0 -or [int]$counts.total -eq 0) { throw "$project tests did not all pass." }
}
$methods = @(
    'ScenarioA1BuildingFireTests.OrdinaryBuildingsReturnOnlyTheirMaterialContribution',
    'ScenarioA1BuildingFireTests.UnknownFactsBlockOnlyMaterialBuildingBranches',
    'ScenarioA1BuildingFireTests.FactoryDirectFireDelegatesWithoutRequiringHindrance',
    'ScenarioA1BuildingFireTests.FactoryResidualNormalizesEntryBeforeCheckingDeluxeAndContradictions',
    'ScenarioA1BuildingFireTests.UnsupportedAndMissingBuildingFactsNeverBecomeZeroTem',
    'ScenarioA1BuildingFireTests.BuildingContractRequiresPublicContextAndMaterialPrivateAnchors',
    'ScenarioA1BuildingFireTests.LegacyBypassLaneStillIncludesResidualWhileNewTemRetainsBuilding',
    'ScenarioA1MarshHeTests.EffectClassificationIncludesDcAndHeEquivalencyButNeverRehalvesResidual',
    'ScenarioA1MarshHeTests.WeatherAndConnectedFloodingRemoveTheModifierIncludingMudflat',
    'ScenarioA1MarshHeTests.BridgePresenceMattersOnlyWhenItCanChangeTheTargetExemption',
    'ScenarioA1MarshHeTests.KnownExemptionsIgnoreIrrelevantUnknownsButSpecialProfilesDoNotMasqueradeAsMarsh',
    'ScenarioA1MarshHeTests.ExactMultiplierComposesWithAlreadyAppliedModifiersWithoutRounding',
    'ScenarioA1MarshHeTests.LegacyOrdnancePreservesSerializedFirepowerIncludingFractionalAndCriticalHits',
    'ScenarioA1MarshHeTests.LegacyDcStillKeepsThirtyFpIntoMarshUntilWiring',
    'ScenarioA1MarshHeTests.MarshContractRequiresPublicFactsAndKeepsPrivateBridgePositionBehindItsAnchor',
    'ScenarioA1OrdnanceTests.HeIsHalvedIntoAMarsh',
    'ScenarioA1RuleContractTests.EveryContractHasAUniqueIdAndRulesThatAreRowsOfTheInventory'
)
Write-Output ($methods -join ', ')
$filter = ($methods | ForEach-Object { 'FullyQualifiedName~' + $_ }) -join '|'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Rules $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Assert-Results 'Rules'
Write-Output 'MinigameBundleTests.EveryBundleIsSound, EveryCheckGivesItsExpectedVerdict (the two new bundles only), EveryGameInABundlesFolderNamesItsBundle'
$filter = 'FullyQualifiedName~MinigameBundleTests.EveryBundleIsSound|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&(DisplayName~b23-building-fire|DisplayName~b16-marsh-he))|FullyQualifiedName~MinigameBundleTests.EveryGameInABundlesFolderNamesItsBundle'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Play $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
Assert-Results 'Play'
exit $LASTEXITCODE
