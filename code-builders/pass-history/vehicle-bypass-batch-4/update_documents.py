"""Publish built Batch 4 evidence into the existing inventory, design, coverage and backlog."""
from pathlib import Path
from datetime import datetime
import argparse
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

parser = argparse.ArgumentParser()
parser.add_argument('--end-utc', required=True, help='Observed UTC clock sample, YYYY-MM-DDTHH:MM:SS')
args = parser.parse_args()
work = Path('E:/Archive/GitHub/dlandi/asl-gate-work-codex')
for project, expected in [('Rules', 24), ('Play', 27)]:
    counters = ET.parse(work / f'vehicle-bypass-batch4-{project}.trx').find('.//{*}Counters')
    assert counters is not None and int(counters.attrib['total']) == expected and int(counters.attrib['passed']) == expected, project

docs = root / 'src/ASL/docs'
path = docs / 'ASL Rule Inventory A to E.md'
lines = path.read_text(encoding='utf-8').splitlines()
for row in inventory_table.rows(lines):
    rule = row['§']
    if rule not in ('D2.321', 'D2.33'):
        continue
    names = list(row)[:-2]
    values = inventory_table.cells(lines[row['line']])
    prefix = 'ScenarioA1BypassTurret' if rule == 'D2.321' else 'ScenarioA1BypassVca'
    updates = {'Claim state': 'built', 'By': 'Codex; independent sub-agent review'}
    updates['Status'] = 'partly' if rule == 'D2.321' else row['Status']
    additions = {
        'Where': prefix + 'Calculator.cs; existing callers retain their earlier reading',
        'Rules calculator': prefix + 'Calculator.cs, ' + prefix + 'Contract.cs',
        'Test': prefix + 'Tests; ' + ('d2321-bypass-tca' if rule == 'D2.321' else 'd233-bypass-vca') + ' bundle'
    }
    for name, addition in additions.items():
        existing = row[name] if row[name] not in ('', 'none') else ''
        updates[name] = existing if addition in existing else (existing + '; ' if existing else '') + addition
    updates['Note'] = ('Batch 4 Rules-only, 2026-11-04: four turret positions, occupied-hex sectors and local Case A/side-change components built. '
        'Overall coverage remains partial: special terrain, CAFP-origin/same-hex composition and maintained-history/IFT composition remain deferred. '
        'Ordinary live TCA and Case A behavior is unchanged pending open W-D2.321.' if rule == 'D2.321' else
        'Batch 4 Rules-only, 2026-11-04: local 1 MP turn, continuation/end verdicts and retained pre-turn defensive frame built. '
        'Legacy callbacks/order and BacklogPass11 behavior preserved. The production wrong-facing fault remains open under W-D2.33; '
        'code inspection is not a live reproduction. General no-pending-turn Reverse Bypass needs D2.22; other forced halts and CAFP-origin composition remain unsupported.')
    for name, value in updates.items():
        values[names.index(name)] = value
    lines[row['line']] = '| ' + ' | '.join(values) + ' |'
for index, line in enumerate(lines):
    if line.startswith(('| D2.321 | Codex |', '| D2.33 | Codex |')):
        values = inventory_table.cells(line)
        values[5] = 'built'
        values[7] = ('Approved Batch 4 Rules-only implementation; 24 focused Rules/contract and 27 bundle/legacy cases pass. '
                     'Isolated Workbench owner, sealed opponent and public defensive-position checks. Independent read-only review corrections incorporated; '
                     'assigned reviewer Claude and user PR review remain pending. Zero exclusive ASOP rows; separate wiring row open and unclaimed. Merge requires user approval.')
        lines[index] = '| ' + ' | '.join(values) + ' |'
text = '\n'.join(lines) + '\n'
wiring = [
    '| W-D2.321 | D2.321 | fire | Play/GamePlanner.Ordnance and GamePlanner.Fire vehicle weapon paths ask ScenarioA1BypassTurretCalculator.Frame, Sector, TurretChange and FireAdjustment. GamePlanner.Vehicles.TurretWithStep, LiveOrdnance.TurretFacing and MapStudio/Components/Pages/Play.razor turret choices use the same four-position verdict. Units/State/GameProjector gathers and records the selected position. Coordinate frame gathering with W-D2.33. | Keep TurretStepBar, MotionAttempts.ChangeBar, CoveredArcTurn and old fire-history answers while any old-reading caller needs them. Remove Bypass refusal-only and ordinary fallback paths only after migration. | Four Bypass TCA positions and Target Facing-counted steps replace ordinary hexspines for the new reading. Side-change +1 is To Hit only. Special terrain, earlier paid-MP BFF changes, D3.51/IFT history composition and Motion-attempt expansion require reviewed Rules prerequisites before their callers move; no hidden profile/history lookup. |  | open |',
    '| W-D2.33 | D2.33 | movement | Play/GamePlanner.VehicleTerrain.TurnedAtCafp and VehicleTurnCost, GamePlanner.Vehicles.VehicleMoveOptions, PlanMoveVehicle and PlanEndVehicle, Play.razor menus and Units/State/GameProjector ask ScenarioA1BypassVcaCalculator.TurnAtCafp, Continuation and EndMovement. BypassTargetFacing, Ordnance/Fire incoming attacks and immobilization ask DefensivePosition. Coordinate shared files with W-D2.321. | LegacyMayMoveOn, LegacyMoveOptions, LegacyTurnRefusal, LegacyStopRefusal and LegacyEndMoveRefusal remain behind existing public methods until every live/old-record caller has a compatible reading. | Retain the occupied CAFP and Target Facing until actual entry; distinguish proposed VCA from defensive frame. Correct post-turn Reverse and voluntary-end handling without changing old games. General Reverse Bypass without a pending turn remains a D2.22 prerequisite, not a refusal. Add a live wrong-facing reproducer and versioned event/state design; record every gate difference. No wiring or state-schema change in this Rules batch. |  | open |'
]
for entry in wiring:
    identifier = entry.split('|')[1].strip()
    if f'| {identifier} |' not in text:
        text = text.replace('| W-C2.21 |', entry + '\n| W-C2.21 |', 1)
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Rule Coverage.md'
text = path.read_text(encoding='utf-8')
text = text.replace('| D2.321 | 197 | not built | A tank in Bypass fires its MA with its ordinary hexspine TCA and hexspine-counted Case A; nothing refuses it. See findings, Faults. |',
    '| D2.321 | 197 | partly | Batch 4 builds four-position TCA and local modifiers. Live ordinary-TCA behavior remains pending W-D2.321; special composition is deferred. |')
text = text.replace('- **D2.321, Bypass TCA (p. 197).** Silent omission, as in part 1. Not a recorded deviation: R11.2 records only the hex-center LOS.',
    '- **D2.321, Bypass TCA (p. 197).** Rules component built by Batch 4; live ordinary-TCA behavior remains pending W-D2.321. R11.2 records only the historical hex-center LOS; the new component does not settle LOS.')
text = text.replace('- **D2.33, Target Facing after a VCA change at the CAFP (p. 198).**',
    '- **D2.33, Target Facing after a VCA change at the CAFP (p. 198), Rules repaired by Batch 4; live correction pending W-D2.33.**')
text = text.replace('- **D2.321:** a tank in Bypass fires with its ordinary TCA (not built). **D2.33:** the Target Facing after a VCA change in Bypass may be wrong',
    '- **D2.321:** Batch 4 Rules components are built; pending W-D2.321, live fire still uses ordinary TCA. **D2.33:** the new Rules retain the pre-turn frame; pending W-D2.33, live Target Facing after a VCA change may still be wrong')
text = text.replace('Left: a stall; the Bypass TCA (2.321); a declared higher expenditure (2.18).',
    'Left at Pass 55: a stall; the Bypass TCA (2.321); a declared higher expenditure (2.18). **Batch 4 (2026-11-04):** D2.321 local Rules components and D2.33 retained-frame verdicts are built; downstream wiring and special composition remain open.')
heading = '## Vehicle Bypass Batch 4, Rules-only delivery (2026-11-04)'
if heading not in text:
    text += '\n' + heading + '\n\nD2.321 and D2.33 have dedicated calculators, four contracts, two minigames and focused tests under the approved [design](<Designs/ASL Vehicle Bypass Batch 4 Rules Design.md>). Tests cover the printed board-23 named hexes, both side positions, all rotations/reflections, local modifier separation, post-turn Reverse and retained defensive frames. Legacy public answers and callbacks remain unchanged. D2.321 remains partial; special firing-point terrain, maintained/IFT composition, earlier-MP BFF history and CAFP-origin queries remain unsupported. General no-pending-turn Reverse Bypass remains D2.22 work. Inventory section 7 holds open W-D2.321 and W-D2.33; no live fault or replay behavior is claimed repaired here.\n'
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Unit Backlog.md'
text = path.read_text(encoding='utf-8').replace(
    'The [proposed Rules-only design](<Designs/ASL Vehicle Bypass Batch 4 Rules Design.md>) awaits approval. This section records boundaries, not claims or a wiring ledger. Proposed wiring remains in design section 8 until built, then inventory section 7.',
    'The approved [Rules-only design](<Designs/ASL Vehicle Bypass Batch 4 Rules Design.md>) is built, 2026-11-04. This section records remaining boundaries. Inventory section 7 holds open, unclaimed W-D2.321 and W-D2.33; design section 8 retains their rationale. Production callers and recorded games are unchanged.')
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'Designs/ASL Vehicle Bypass Batch 4 Rules Design.md'
text = path.read_text(encoding='utf-8')
text = text.replace('**Status:** Reviewed and revised for user approval, 2026-11-04. No implementation or claims started.',
    '**Status:** Approved and implemented Rules-only, 2026-11-04; prepared for PR review, not merged. Claim lock: `ad2187fc`.')
text = text.replace('Proposed wiring is in section 8 below until the claims reach built.', 'At built, W-D2.321 and W-D2.33 are open in inventory section 7; section 8 below retains handoff rationale.')
text = text.replace('## 8. Proposed downstream wiring, not a second ledger', '## 8. Downstream handoff rationale, recorded in inventory section 7')
text = text.replace('At built, add the following rows to **inventory section 7**, or update an existing matching row if another delivery has created it. Until then these are proposals only.',
    'The following handoffs are now recorded as open, unclaimed rows in **inventory section 7**. This section explains the design; inventory alone holds wiring ownership and state.')
text = text.replace('| Proposed row / capability |', '| Inventory row / capability |')
heading = '## 11. As built and verification'
if heading not in text:
    text += '\n' + heading + '\n\nSix new Rules files expose the typed facts/verdicts and four registered contracts. D2.33 legacy decisions are consolidated behind existing public methods with the same callbacks, short-circuit order, strings and outputs. Only compatibility documentation changes the existing turret/Motion-attempt refusals. No Play, Units, Maps, ASL.Turns, Authoring or Studio source changed. Existing Authoring assertions still describe the unchanged production reading.\n\n'
    text += 'Workbench inspection used isolated Studio 6680, board 23, catalog PzKpfw IIIH at [BB3] and Sherman 75 at [AA4]. The board inspector identified [BB3] as a stone building. These are setup anchors, not a legal live Bypass move or a map-clearance proof. Hypothetical ST facts produced Case A 2 plus separate side-change 1 for both Left and Right. The opponent public pending-turn query retained the old front facing. The rebuilt concealed-anchor fixture offered only a German "?" stack to the British viewer, hid turret/history facts and returned not-evaluated; owner input remained available. All values were entered by hand and no event was emitted.\n\n'
    text += 'Evidence in `E:/Archive/GitHub/dlandi/asl-gate-work-codex`: `vehicle-bypass-batch4-build.log` records the isolated Studio build with zero warnings/errors. `vehicle-bypass-batch4-Rules.log/.trx` records 24 passing focused Rules/contract cases; `vehicle-bypass-batch4-Play.log/.trx` records 27 passing cases, including 24 bundle checks, bundle integrity/header checks and unchanged `BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere`. Both used locked restore and warnings as errors. No full local suite or replay gate ran; CI and the eventual fifth-claim gate remain their separate required steps.\n\n'
    text += 'Read-only independent review found and corrected inactive-branch shape validation, the category-specific CMG/IFE/Canister exception and an unsupported-coordinate refusal. It also required the fixed published board-23 geometry fixture and the explicit D2.22 no-pending Reverse-Bypass boundary. Tests now cover those corrections. Initial formatting/analyzer failures were corrected before the strict harness passed; `verify.ps1` stops on build/test failure or zero tests. No assertion or gate filter was weakened.\n'
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Unit Time Log.md'
text = path.read_text(encoding='utf-8')
heading = '## Vehicle Bypass Batch 4, 2026-11-04 (Codex)'
if heading not in text:
    start = datetime.fromisoformat('2026-11-04T21:04:05')
    end = datetime.fromisoformat(args.end_utc)
    section = (f'\n{heading}\n\nObserved UTC boundaries; claim-lock preparation preceded this measured implementation interval. No estimate supplied.\n\n'
        '| Sub-task | Start UTC | End UTC | Duration | Notes |\n|---|---|---|---|---|\n'
        f'| D2.321/D2.33 implementation, Workbench, review and focused verification | {start:%H:%M:%S} | {end:%H:%M:%S} | {end-start} | '
        '24 focused Rules/contract and 27 bundle/legacy cases passed; strict isolated Studio build, owner/sealed-opponent and public defensive-frame checks. '
        'Read-only review corrections incorporated. Delivery records, PR and CI follow this measured interval. No wiring or merge. |\n')
    text = text.replace('# ASL Unit Time Log\n', '# ASL Unit Time Log\n' + section, 1)
path.write_text(text, encoding='utf-8', newline='\n')
