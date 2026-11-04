"""Record Batch 1 Rules delivery after the Workbench and focused tests pass."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

docs = root / 'src/ASL/docs'
path = docs / 'ASL Rule Inventory A to E.md'
lines = path.read_text(encoding='utf-8').splitlines()
families = {'A25.61':'ItalianDeployment', 'A25.63':'ItalianCapture', 'A25.71':'FinnishOrganization', 'A25.75':'CapturedEquipmentUse'}
methods = {'A25.61':'Evaluate, EliteMmc', 'A25.63':'CaptureModifier; ScenarioA1PrisonerCalculator.Escape(facts, ItalianEscapeContext.WithEritreanInheritance)',
           'A25.71':'Evaluate (requirements only)', 'A25.75':'Evaluate (Captured Use, night MG treatment and PSK use restriction)'}
for row in inventory_table.rows(lines):
    rule = row['§']
    owner = {'A25.66':'A25.61', 'A25.77':'A25.75'}.get(rule, rule)
    if owner not in families:
        continue
    names = list(row)[:-2]
    values = inventory_table.cells(lines[row['line']])
    values[names.index('Status')] = 'partly' if rule == 'A25.66' else 'built'
    values[names.index('Claim state')] = 'built'
    family = families[owner]
    values[names.index('Rules calculator')] = f'ScenarioA1{family}Calculator.cs: {methods[owner]}; ScenarioA1{family}Contract.cs'
    if rule == 'A25.66':
        values[names.index('Rules calculator')] += '; ScenarioA1ItalianNationalityRules.UsesItalianDeploymentAndCapture; ItalianCaptureCalculator.CaptureModifier; PrisonerCalculator.Escape current-reading overload'
    values[names.index('Test')] = f'ScenarioA1{family}Tests; MinigameBundleTests (A25 bundles)'
    values[names.index('Note')] = 'Nationality Batch 1, 2026-11-04: Rules-only calculator, contract and minigame built under the approved design. Legacy callers keep their answers; downstream work remains open in W-' + owner + '. ' + (
        'Only Italian deployment/capture/escape inheritance is built. Eritrean ELR, Battle Hardening, Lax, PAATC and counter catalog remain separate. ' if rule == 'A25.66' else '') + 'Before this claim: ' + row['Note']
    lines[row['line']] = '| ' + ' | '.join(values) + ' |'
for index, line in enumerate(lines):
    if any(line.startswith('| ' + rule + ' | Codex |') for rule in families):
        lines[index] = line.replace('| claimed |', '| built |')
    if line.startswith('| W-A20.54 |'):
        cells = inventory_table.cells(line)
        cells[3] += ' Nationality Batch 1 dependency: after A25.63 merges, current escape evaluation uses ScenarioA1PrisonerCalculator.Escape(facts, ItalianEscapeContext.WithEritreanInheritance), including Eritreans under A25.66. Keep the original overload and original contract for their recorded reading; do not create another escape implementation.'
        lines[index] = '| ' + ' | '.join(cells) + ' |'
wiring = [
    '| W-A25.61 | A25.61 | setup and work | Play/ScenarioSetup.MayDeploy and Fill; GamePlanner.SupportWeapons.PlanDeploy; Units/State/GameProjector deployment validation; Studio Setup and Play deployment choices: gather each squad nationality, printed class and explicit exception context, then ask ScenarioA1ItalianDeploymentCalculator.Evaluate. | Retire Italian admission based solely on ScenarioA1Definitions.MayDeploy and the old SetupCalculator.Fill path once callers migrate; preserve general Russian behavior and old recorded setup reading. | Ordinary non-elite Italian/Eritrean squads no longer Deploy; elite and stated Guard, Unarmed and Temporary Crew exceptions pass this restriction only. Broader action rules still apply. |  | open |',
    '| W-A25.63 | A25.63 | close combat | Play/GamePlanner.CloseCombat and LiveCloseCombat fact assembly; Units/State/GameProjector CC validation; Studio Play and CloseCombatPanel capture previews: supply nationality, elite class and Inexperienced status for each defender to ScenarioA1ItalianCaptureCalculator.CaptureModifier. Escape uses the existing W-A20.54 row and its current-reading overload dependency. | Replace the inline ordinary capture term in ScenarioA1CloseCombatCalculator for explicitly versioned new records only; retain old resolving answers until old replay support moves. | Experienced non-elite Italian/Eritrean defender capture term becomes zero; Inexperienced remains minus one, ordinary elite remains plus one. Mixed groups stay per-defender; Passenger, leadership and other terms stay independent. |  | open |',
    '| W-A25.71 | A25.71 | rally | Play/GamePlanner.SupportWeapons.PlanDeploy and PlanRecombine, Units/State/GameProjector organization validation, and Studio Play organization choices: ask ScenarioA1FinnishOrganizationCalculator.Evaluate, validate actual nominated leader and all general prerequisites separately, then compose LeaderDrm and DeploymentPassed. | Retain NeedsNoLeader and GuardDeploysAtWill as shared Guard/Unarmed/Carrier helpers; remove nationality-blind leader requirements from current caller paths only. | Finnish squads explicitly choose directed NTC or leaderless 1TC; unbroken Guard automatic deployment retains priority. Finnish HS need no leader to Recombine. No blanket legality verdict, new rule outside Rules or replay reinterpretation. |  | open |',
    '| W-A25.75 | A25.75 | fire | Play/LiveFire.CapturedBy and LiveOrdnance and corresponding GamePlanner fire gathers; Units/State/GameProjector fire reconstruction; Studio fire/ordnance previews: gather actual user/weapon nationality, Personnel, printed class, MG kind, provenance and scenario date for ScenarioA1CapturedEquipmentUseCalculator.Evaluate before the existing use-penalty arithmetic. | Remove CapturedBy as the sole use-penalty decision in current paths; retain it for legacy, physical-capture and repair readings. Keep qualification and ammunition-shortage arithmetic separate. | Directional Finnish/Russian MG exemptions include E1.76 treatment with LMG-1939 and .50 HMG exceptions; reciprocal German/Finnish PSK exemptions remain subject to July 1944 through 1945 and Finnish low-class penalties. Night Sniper resolution itself remains deferred. |  | open |',
]
index = next(index for index, line in enumerate(lines) if line.startswith('| W-C6.17 |'))
lines[index:index] = wiring
path.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')

path = docs / 'ASL Rule Coverage.md'
text = path.read_text(encoding='utf-8')
text = text.replace('Italian Lax and Deploy limit, Italian and Axis Minor 1PAATC and no-escape not built;', 'Italian and Axis Minor 1PAATC remain outside Batch 1; deployment, capture and escape calculators have separate open wiring;')
text = text.replace('Finns pay Captured Use on a Russian MG (25.75);', 'Finnish Captured Use exceptions are built in Rules (A25.75/A25.77), with current caller behavior unchanged pending W-A25.75;')
text += '\n## Nationality Batch 1, Rules-only delivery (2026-11-04)\n\nA25.61, A25.63, A25.71 and A25.75 have dedicated calculators, contracts, focused tests and four minigames under the approved [design](<Designs/ASL Nationality Batch 1 Rules Design.md>). A25.77 is included with captured equipment use. A25.66 is partial: only inherited Italian deployment/capture/escape treatment is included. Existing ELR, leader ranks, Field Promotion and leader-loss implementations remain the authority. Old public answers and recorded facts retain their shape. W-A25.61, W-A25.63, W-A25.71 and W-A25.75 are open; W-A20.54 holds the escape integration dependency. Calculator completion does not imply caller or catalog completion.\n'
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Unit Backlog.md'
text = path.read_text(encoding='utf-8')
text += '''
## 98. Deferred by Nationality Batch 1

The approved design is `Designs/ASL Nationality Batch 1 Rules Design.md`. The inventory remains the only claim board; these are boundaries, not duplicate assignments.

| Item | Rules | Separate work | Current boundary |
|---|---|---|---|
| Downstream nationality behavior | A25.61, A25.63, A25.71, A25.75 | Inventory W-A25.61, W-A25.63, W-A25.71, W-A25.75; escape dependency W-A20.54 | Rules-only delivery; callers and recorded games keep their existing answers |
| Remaining Eritrean distinctions | A25.66 | Nationality Rules and catalog claims | ELR, Battle Hardening, Lax and PAATC are not covered by the narrow Italian deployment/capture/escape inheritance helper |
| Missing Eritrean and Italian crew counters; Finnish weapon variants/provenance | A25.61, A25.66, A25.75 | Catalog work | Real existing counters demonstrate the position; missing variants use labelled, hand-entered what-if facts |
| Full deployment/recombination legality | A1.31, A1.32 | General organization Rules and phase wiring | Nationality verdicts do not validate a live leader, compatible HS, timing, terrain, Good Order or attempt budgets |
| Temporary Crew exchange command | A21.22 | Existing pass 110/vehicle-crewing backlog | Italian nationality exemption only; vehicle/manning prerequisites and command are not added |
| Automatic captured-MG night Sniper resolution | E1.76 | Existing night work | The nationality exemption is built; no Sniper resolution is added |

'''
path.write_text(text, encoding='utf-8', newline='\n')
