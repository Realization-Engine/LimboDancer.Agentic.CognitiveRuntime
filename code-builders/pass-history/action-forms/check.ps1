param([switch]$Movement, [switch]$Fire, [switch]$Rout, [switch]$Advance, [switch]$CloseCombat, [switch]$Rally, [switch]$RallyRefusal, [switch]$CiPresentation, [string]$Only)
$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK first.' }
$repoRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
$outputPath = "$env:ASL_GATE_WORK/action-form-design/checks"
& 'C:/Program Files/Git/bin/bash.exe' "$repoRoot/code-builders/build-and-test/outside_outputs.sh" register $outputPath
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$methods = @(
    'ActionLocationTests.InvalidEditBlocksOldValueUntilExplicitReplacement',
    'ActionLocationTests.HiddenFieldCancelsOnlyItsOwnPicker',
    'ActionLocationTests.ResetWithUnchangedValueDropsUnfinishedText',
    'ActionLocationTests.RestrictedPickWaitsForAnExplicitLevelAndExcludesDisabledChoices',
    'LocationWordsTests.AnArmedFieldTakesTheNextPickAndDisarms',
    'CloseCombatComponentTests.TheFirePanelFollowsTheSelectionAndThePhase',
    'PlayPageDemoFixesTests.TheOpportunityFirePanelMarksTheUnitsAndTheFirePanelOffersSprayingFire'
)
$checkName = 'action-forms'
if ($Movement) {
    $methods = @('MovementComponentTests.MapOriginsKeepBoardsLevelsAndUnitSelectionDistinct', 'PlayPagePass31cTests.ARouteIsPlottedOnTheMapAndMadeStepByStep')
    $checkName = 'movement-workflow'
}
if ($Fire) {
    $methods = @(
        'CloseCombatComponentTests.TheFirePanelFollowsTheSelectionAndThePhase',
        'PlayPagePass31dTests.AFtIsSaidToFireAloneAndHoldsProposeWhenTickedWithAnotherUnit',
        'PlayPagePass31dTests.AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked',
        'PlayPagePass35Tests.AKeptRateOfFireIsSaidToBothSidesAndTheMgIsOfferedAgain'
    )
    $checkName = 'fire-presentation'
}
if ($Rout) {
    $methods = @(
        'MovementComponentTests.RoutMapScopeFollowsInspectionAndLeavesRoutePickingAlone',
        'PlayPagePass31dTests.TheRoutPanelSaysWhatALadenUnitLeavesOffersAWayAndTheReviewSaysWhatIsLeft'
    )
    $checkName = 'rout-workflow'
}
if ($Advance) {
    $methods = @(
        'MovementComponentTests.AdvanceScopePreservesDestinationPicksAfterTheChooserDisarms',
        'MovementComponentTests.TheAdvanceNamesUnitsWaitingOffBoard',
        'IconCoverageTests.TheCoverageFileMatchesTheSources'
    )
    $checkName = 'advance-workflow'
    $env:ASL_ICONS_UPDATE_COVERAGE = '1'
}
if ($CloseCombat) {
    $methods = @(
        'CloseCombatComponentTests.TheCloseCombatPanelHoldsAttacksUntilTheAmbushIsRolled',
        'CloseCombatComponentTests.TheCloseCombatPanelListsItsAttacksAndChoices',
        'CloseCombatComponentTests.CloseCombatRequiresFinishedDeclarationsAndReview',
        'CloseCombatComponentTests.CloseCombatMapSelectionPreservesDraftsAndDistinguishesLevels',
        'PlayPageCloseCombatTests.U33OnThePageASquadAdvancesAndItsCloseCombatLeavesTheSurvivorsInMelee',
        'PlayPageCloseCombatTests.ACaptureAttemptIsDeclaredOnThePageAndItsRecordNamesTheGuard',
        'PlayPageCloseCombatTests.AFirstSidesDeclarationWaitsForTheOtherAndClearAllStartsTheRoundOver',
        'PlayPageCloseCombatTests.AUnitHeldInMeleeWithdrawsFromThePage',
        'IconCoverageTests.TheCoverageFileMatchesTheSources'
    )
    $checkName = 'cc-workflow'
    $env:ASL_ICONS_UPDATE_COVERAGE = '1'
}
$testProject = "$repoRoot/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/LimboDancer.Domains.Asl.MapStudio.Tests.csproj"
if ($Rally) {
    $methods = @(
        'RallyAndRecordComponentTests.RallyOffersOnlySuppliedAssistanceAndRequiresASelectedMethod',
        'RallyAndRecordComponentTests.TheRallyPanelHoldsBackAnAttemptTheGateWouldRefuse',
        'RallyAndRecordComponentTests.DeployPanelRequiresTheBackendRequestedSpotterChoice',
        'PlayPagePass28bTests.DeployGivesEachCheckedSwToTheSecondHs',
        'PlayPagePass28bTests.AnotherViewClearsTheRallyAndSupportWeaponChoices',
        'PlayPagePass35Tests.ATagIsKeptThroughDeploymentAndRecombinationInBothViews',
        'PlayPageStepsTests.ALeaderRalliesInTheRphAndAMgJoinsTheFireThatRemovesADummy',
        'IconCoverageTests.TheCoverageFileMatchesTheSources'
    )
    $checkName = 'rally-workflow'
    $env:ASL_ICONS_UPDATE_COVERAGE = '1'
}
if ($RallyRefusal) {
    $methods = @('MovementPlotTests.TheRallyBarIsTheGatesOwnRefusal')
    $testProject = "$repoRoot/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/LimboDancer.Domains.Asl.Play.Tests.csproj"
    $checkName = 'rally-refusal'
}
if ($CiPresentation) {
    $methods = @(
        'PlayPagePass31cTests.AnArmedLocationTakesTheNextHexClickedAndAHandOverDropsIt',
        'PlayPagePass31cTests.TheFirePanelReadsItsTargetsByRangeOnceAFirerIsTicked',
        'PlayWorkspaceComponentTests.ADisabledProposeButtonSaysWhatIsMissing',
        'CloseCombatComponentTests.TheAttackBuilderOffersCaptureFieldsOnlyForACapture'
    )
    $checkName = 'ci-presentation'
}
$filter = ($methods | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
if ($Only) {
    $selectedMethods = @($methods | Where-Object { $_.EndsWith(".$Only", [StringComparison]::Ordinal) })
    if ($selectedMethods.Count -ne 1) { throw 'Only must name one method from the selected check group.' }
    $filter = "FullyQualifiedName~$($selectedMethods[0])"
    $checkName += '-single'
}
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$ErrorActionPreference = 'Continue' # Let the test runner finish its report when a failure is written to stderr.
& dotnet test $testProject --disable-build-servers -m:1 "-p:OutDir=$outputPath/" -p:RestoreLockedMode=true --warnaserror --filter $filter --logger "trx;LogFileName=$checkName.trx" --results-directory "$env:ASL_GATE_WORK/action-form-design/results" -v:q -nologo *> "$env:ASL_GATE_WORK/action-form-design/$checkName-checks.log"
$result = $LASTEXITCODE
Get-Content "$env:ASL_GATE_WORK/action-form-design/$checkName-checks.log" -Tail 60
exit $result
