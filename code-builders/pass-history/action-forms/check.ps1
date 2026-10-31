param([switch]$Movement, [switch]$Fire)
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
$filter = ($methods | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$ErrorActionPreference = 'Continue' # Let the test runner finish its report when a failure is written to stderr.
& dotnet test "$repoRoot/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/LimboDancer.Domains.Asl.MapStudio.Tests.csproj" --disable-build-servers -m:1 "-p:OutDir=$outputPath/" -p:RestoreLockedMode=true --warnaserror --filter $filter --logger "trx;LogFileName=$checkName.trx" --results-directory "$env:ASL_GATE_WORK/action-form-design/results" -v:q -nologo *> "$env:ASL_GATE_WORK/action-form-design/$checkName-checks.log"
$result = $LASTEXITCODE
Get-Content "$env:ASL_GATE_WORK/action-form-design/$checkName-checks.log" -Tail 60
exit $result
