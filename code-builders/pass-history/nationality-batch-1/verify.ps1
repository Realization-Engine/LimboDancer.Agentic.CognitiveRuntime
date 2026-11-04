# Named methods only. No whole test class or project run.
param([switch] $Review)
$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$env:ASL_REPO = (Resolve-Path "$PSScriptRoot/../../..").Path
$methods = @(
    'ScenarioA1ItalianDeploymentTests.NationalityRestrictionDoesNotGrantFullDeployment',
    'ScenarioA1ItalianDeploymentTests.ExceptionsReadTheirRawFactsAndIgnoreIrrelevantUnknowns',
    'ScenarioA1ItalianDeploymentTests.PrintedEliteClassificationIsItalianOnly',
    'ScenarioA1ItalianDeploymentTests.LegacyNationalityAndLeaderExceptionMembersKeepTheirAnswers',
    'ScenarioA1FinnishOrganizationTests.ActionAndExplicitModeDetermineRequirementsOnly',
    'ScenarioA1FinnishOrganizationTests.GuardPriorityAndConditionalUnknownsArePreserved',
    'ScenarioA1FinnishOrganizationTests.DeploymentRequirementComposesWithLeaderDrmAtThePassingBoundary',
    'ScenarioA1FinnishOrganizationTests.ExistingFinnishRanksAndFieldPromotionRemainTheSingleTables',
    'ScenarioA1ItalianCaptureTests.CaptureTermsAreAlternativesForEachDefender',
    'ScenarioA1ItalianCaptureTests.MixedDefendersNeverShareTheMostFavorableTerm',
    'ScenarioA1ItalianCaptureTests.EscapeInheritanceIsOptInAndPreservesEveryOtherBranch',
    'ScenarioA1CapturedEquipmentUseTests.MachineGunExemptionsAreDirectionalAndDateSpecific',
    'ScenarioA1CapturedEquipmentUseTests.FinnishPskDatesAndPrintedClassApplyEvenToFinnishColor',
    'ScenarioA1CapturedEquipmentUseTests.OnlyMaterialUnknownsBlockAnAnswer',
    'ScenarioA1CapturedEquipmentUseTests.LegacyCapturedByKeepsNullAndDoesNotAcquireAnExemption',
    'ScenarioA1CapturedEquipmentUseTests.RequiredPublicDateAndConditionalSentinelsAreValidatedByTheRegistry',
    'ScenarioA1CapturedEquipmentUseTests.OwnerFactsUseTheirSubjectAndPrisonerContractsKeepPublicDisclosure'
)
if ($Review) {
    $methods = @(
        'ScenarioA1ItalianDeploymentTests.NationalityRestrictionDoesNotGrantFullDeployment',
        'ScenarioA1ItalianDeploymentTests.ExceptionsReadTheirRawFactsAndIgnoreIrrelevantUnknowns',
        'ScenarioA1FinnishOrganizationTests.ActionAndExplicitModeDetermineRequirementsOnly',
        'ScenarioA1ItalianCaptureTests.EscapeInheritanceIsOptInAndPreservesEveryOtherBranch',
        'ScenarioA1CapturedEquipmentUseTests.FinnishPskDatesAndPrintedClassApplyEvenToFinnishColor',
        'ScenarioA1CapturedEquipmentUseTests.OwnerFactsUseTheirSubjectAndPrisonerContractsKeepPublicDisclosure',
        'ScenarioA1CapturedEquipmentUseTests.CaptureFactsArePublicAtDeclarationAndMissingMaterialFactsStayUnknown',
        'ScenarioA1CapturedEquipmentUseTests.PskContractCitesTheRuleThatDecidedItsTreatment',
        'ScenarioA1PrisonerEscapeTests.TheEscapeContractGivesTheVerdict',
        'ScenarioA1PrisonerEscapeTests.TheEscapeContractRefusesNonInfantryReadsAnUnknownHomeAsNotAndIsNotEvaluatedWithoutItsFacts'
    )
}
Write-Output ($methods -join ', ')
$filter = ($methods | ForEach-Object { 'FullyQualifiedName~' + $_ }) -join '|'
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Rules $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output 'MinigameBundleTests.EveryBundleIsSound, EveryCheckGivesItsExpectedVerdict (a25 bundles, plus a20-prisoners with -Review), EveryGameInABundlesFolderNamesItsBundle'
$filter = 'FullyQualifiedName~MinigameBundleTests.EveryBundleIsSound|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&DisplayName~a25-)|FullyQualifiedName~MinigameBundleTests.EveryGameInABundlesFolderNamesItsBundle'
if ($Review) {
    $filter = 'FullyQualifiedName~MinigameBundleTests.EveryBundleIsSound|(FullyQualifiedName~MinigameBundleTests.EveryCheckGivesItsExpectedVerdict&(DisplayName~a25-|DisplayName~a20-prisoners))|FullyQualifiedName~MinigameBundleTests.EveryGameInABundlesFolderNamesItsBundle'
}
& 'C:/Program Files/Git/bin/bash.exe' "$env:ASL_REPO/code-builders/build-and-test/run_test_classes.sh" Play $filter '-p:RestoreLockedMode=true' '-p:TreatWarningsAsErrors=true'
exit $LASTEXITCODE
