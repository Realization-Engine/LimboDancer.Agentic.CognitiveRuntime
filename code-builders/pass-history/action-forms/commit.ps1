param([switch]$Movement, [switch]$Fire, [switch]$Rout, [switch]$Advance)
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
if ($Fire) {
    $paths = @(
        'code-builders/build-and-test/start_codex_studio.ps1',
        'code-builders/pass-history/action-forms/check.ps1',
        'code-builders/pass-history/action-forms/cleanup.ps1',
        'code-builders/pass-history/action-forms/commit.ps1',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/SmallArmsFirePanel.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css',
        'src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs',
        'src/ASL/docs/Designs/ASL Play Action Interface Implementation Design.md',
        'src/ASL/docs/Reviews/ASL Action Forms Implementation Record.md',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/CloseCombatComponentTests.cs',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPagePass31dTests.cs',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPagePass35Tests.cs'
    )
}
if ($Rout) {
    $paths = @(
        'code-builders/build-and-test/start_codex_studio.ps1',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/RoutActionPanel.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/MovementComponentTests.cs',
        'src/ASL/docs/Designs/ASL Play Action Interface Implementation Design.md',
        'src/ASL/docs/Reviews/ASL Action Forms Implementation Record.md',
        'src/ASL/docs/ASL Unit Backlog.md',
        'src/ASL/docs/ASL Unit Time Log.md',
        'code-builders/pass-history/action-forms/check.ps1',
        'code-builders/pass-history/action-forms/cleanup.ps1',
        'code-builders/pass-history/action-forms/commit.ps1',
        'code-builders/pass-history/action-forms/prepare_movement_demo.py'
    )
}
if ($Advance) {
    $paths = @(
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/AdvanceActionPanel.razor',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/Services/LocationPicking.cs',
        'src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/icons/icon-coverage.json',
        'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/MovementComponentTests.cs',
        'src/ASL/docs/Designs/ASL Play Action Interface Implementation Design.md',
        'src/ASL/docs/Reviews/ASL Action Forms Implementation Record.md',
        'src/ASL/docs/ASL Unit Backlog.md',
        'src/ASL/docs/ASL Unit Time Log.md',
        'code-builders/pass-history/action-forms/check.ps1',
        'code-builders/pass-history/action-forms/cleanup.ps1',
        'code-builders/pass-history/action-forms/commit.ps1'
    )
}
foreach ($path in $paths) {
    $absolute = Join-Path $repoRoot $path
    $content = [IO.File]::ReadAllText($absolute).Replace("`r`n", "`n")
    [IO.File]::WriteAllText($absolute, $content, [Text.UTF8Encoding]::new($false))
}
& git -C $repoRoot -c "safe.directory=$($repoRoot.Replace('\','/'))" add -- $paths
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& git -C $repoRoot -c "safe.directory=$($repoRoot.Replace('\','/'))" diff --cached --check
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$branch = & git -C $repoRoot -c "safe.directory=$($repoRoot.Replace('\','/'))" branch --show-current
if ($branch -ne 'codex/action-form-design') { throw "Unexpected branch: $branch" }
$message = if ($Advance) { 'feat(asl): scope Advance actions to map selections' } elseif ($Rout) { 'feat(asl): connect map picks to rout actions' } elseif ($Fire) { 'fix(asl): clarify firing groups and weapon-only previews' } elseif ($Movement) { 'feat(asl): connect map picks to movement actions' } else { 'feat(asl): organize action forms and unify target entry' }
& git -C $repoRoot -c "safe.directory=$($repoRoot.Replace('\','/'))" commit -m $message -m 'Co-Authored-By: Codex <noreply@openai.com>'
exit $LASTEXITCODE
