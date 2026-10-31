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
foreach ($path in $paths) {
    $absolute = Join-Path $repoRoot $path
    $content = [IO.File]::ReadAllText($absolute).Replace("`r`n", "`n")
    [IO.File]::WriteAllText($absolute, $content, [Text.UTF8Encoding]::new($false))
}
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" add -- $paths
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" diff --cached --check
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& git -c "safe.directory=$($repoRoot.Replace('\','/'))" commit -m 'feat(asl): organize action forms and unify target entry'
exit $LASTEXITCODE
