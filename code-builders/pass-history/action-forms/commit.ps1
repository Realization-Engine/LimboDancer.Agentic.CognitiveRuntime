param([switch]$Movement)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
$panels = 'ActionFormGroup,ActionProposalArea,ActionLocationChoice,ActionSection,AdvanceActionPanel,AmbushWithdrawalActions,BerserkOverrunCloseCombat,CloseCombatAttackBuilder,CloseCombatInfiltrationChoices,CloseCombatStacking,CloseCombatWithdrawals,CrewExposureActions,DeployActionPanel,DmRetentionChoices,GunArcAction,InfantryMovementAction,LocationField,MoppingUpAction,OpportunityFireAction,OrdnanceFirePanel,OrdnanceTargetFields,PlacedChargeDetonationActions,PrisonerCustodyActions,RallyActionPanel,ReactionFireAction,RecombineActionPanel,RepairActionPanel,RoutActionPanel,ShockRecoveryAction,SmallArmsFirePanel,SpotterDesignationPanel,StarshellActionPanel,SupportWeaponActionPanel,ThrowDemolitionChargeAction,VehicleCrewActions,VehicleMovementPanel,VehicleScroungeActions'.Split(',')
$paths = @($panels | ForEach-Object { "src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/$_.razor" })
$paths += @(
 'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor',
 'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Shared/RuleHelp.razor',
 'src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css',
 'src/ASL/docs/Designs/ASL Play Action Interface Implementation Design.md',
 'src/ASL/docs/Reviews/ASL Action Forms Implementation Record.md'
)
$paths += @('ActionLocationTests','CloseCombatComponentTests','LocationWordsTests','PlayPageDemoFixesTests','PlayPagePass31cTests' | ForEach-Object { "src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/$_.cs" })
$paths += @('implement.py','refine.py','contextual.py','locations.py','check.ps1','commit.ps1','cleanup.ps1' | ForEach-Object { "code-builders/pass-history/action-forms/$_" })
if ($Movement) {
    $paths = @(
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/InfantryMovementAction.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/MovementRoutePanel.razor',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/MovementComponentTests.cs',
        'src/ASL/docs/Designs/ASL Play Action Interface Implementation Design.md',
        'src/ASL/docs/Reviews/ASL Action Forms Implementation Record.md',
        'src/ASL/docs/ASL Unit Backlog.md',
        'code-builders/pass-history/action-forms/check.ps1',
        'code-builders/pass-history/action-forms/cleanup.ps1',
        'code-builders/pass-history/action-forms/commit.ps1',
        'code-builders/pass-history/action-forms/prepare_movement_demo.py'
    )
}
foreach ($path in $paths) {
    $absolute = Join-Path $repoRoot $path
    $content = [IO.File]::ReadAllText($absolute).Replace("`r`n", "`n")
    [IO.File]::WriteAllText($absolute, $content, [Text.UTF8Encoding]::new($false))
}
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" add -- $paths
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" diff --cached --check
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$branch = & git -c "safe.directory=$($repoRoot.Replace('\','/'))" branch --show-current
if ($branch -ne 'codex/action-form-design') { throw "Unexpected branch: $branch" }
$message = if ($Movement) { 'feat(asl): connect map picks to movement actions' } else { 'feat(asl): organize action forms and unify target entry' }
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" commit -m $message -m 'Co-Authored-By: Codex <noreply@openai.com>'
exit $LASTEXITCODE
