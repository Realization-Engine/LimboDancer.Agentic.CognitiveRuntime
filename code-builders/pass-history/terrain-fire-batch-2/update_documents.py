"""Record approved Batch 2 delivery after Workbench, focused tests and independent review."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

docs = root / 'src/ASL/docs'
path = docs / 'ASL Rule Inventory A to E.md'
lines = path.read_text(encoding='utf-8').splitlines()
notes = {
    'B23.31': 'Standard-ASL building TEM component, non-Residual Bypass, Residual/Fire Lane retention and delegation to the existing Factory calculator are built. Deluxe Bypass Residual and special Locations remain explicitly not-evaluated. Legacy lane terrain and live Residual behavior remain unchanged pending W-B23.31.',
    'B16.31': 'Supported HE/DC/HE-equivalency marsh multiplier is built, with per-target bridge exemptions and weather/flood/mudflat applicability. Residual is never halved again. Swamp HE and VT FFE remain not-evaluated. The legacy ordnance divisor preserves decimal division and serialization; live DC still keeps its old FP pending W-B16.31.',
    'B16.6': 'Partial: connected flooding removes the marsh HE modifier. Water depth authoring, map conversion, movement and other consequences remain open.',
    'B16.7': 'Partial: mudflat inherits the marsh HE modifier and its exemptions. Mudflat authoring, movement and other terrain consequences remain open.',
    'B16.8': 'Partial: Snow, Deep Snow and sub-zero conditions remove the marsh HE modifier. Production weather gathering, map conversion, movement and other consequences remain open.',
}
for row in inventory_table.rows(lines):
    rule = row['§']
    if rule not in notes:
        continue
    family = 'BuildingFire' if rule == 'B23.31' else 'MarshHe'
    names = list(row)[:-2]
    values = inventory_table.cells(lines[row['line']])
    updates = {
        'Status': 'built' if rule in ('B23.31', 'B16.31') else 'partly',
        'Claim state': 'built',
        'Where': f'ScenarioA1{family}Calculator.cs; existing callers retain their legacy reading',
        'Rules calculator': f'ScenarioA1{family}Calculator.cs; ScenarioA1{family}Contract.cs',
        'Test': f'ScenarioA1{family}Tests; MinigameBundleTests (terrain-fire bundles)',
        'Note': 'Terrain Fire Batch 2, 2026-11-04, Rules only: ' + notes[rule],
        'By': 'Codex; independent sub-agent review',
    }
    for name, value in updates.items():
        values[names.index(name)] = value
    lines[row['line']] = '| ' + ' | '.join(values) + ' |'
for index, line in enumerate(lines):
    if any(line.startswith('| ' + rule + ' | Codex |') for rule in ('B23.31', 'B16.31')):
        cells = inventory_table.cells(line)
        cells[2] = 'Claude'
        cells[5] = 'built'
        cells[7] = 'Approved revised Terrain Fire Batch 2 built Rules-only. Independent sub-agent review completed; assigned reviewer Claude confirmed rule correctness in PR 124. Review corrections merge main and restore Claude as Reviewer. State remains built until merge, when Claude will mark reviewed. B16.6/B16.7/B16.8 are partial HE applicability under B16.31; zero ASOP rows. W-B23.31/W-B16.31 remain open. User merge approval pending.'
        lines[index] = '| ' + ' | '.join(cells) + ' |'
    if line.startswith('| W-B23.74 |') and 'W-B23.31' not in line:
        cells = inventory_table.cells(line)
        cells[3] += ' Coordinate W-B23.31: BuildingFireCalculator.BuildingTem delegates Factory TEM to this calculator; reuse the same Factory facts and Residual-entry semantics, without another Factory implementation.'
        lines[index] = '| ' + ' | '.join(cells) + ' |'
wiring = [
    '| W-B23.31 | B23.31 | fire | Play/GamePlanner.Fire.FireMapFacts; GamePlanner.FireExtensions.FireLaneFacts; GamePlanner.Ordnance and AreaLocations; Rules/ScenarioA1FireFollowUps.FireLaneAttack and downstream fire arithmetic: gather per-Location building/material, attack kind, Bypass, profile and Factory facts, then ask ScenarioA1BuildingFireCalculator.BuildingTem. Keep lane terrain separate for FFMO/geometry. Coordinate Factory gathering with W-B23.74. Units/State/FireRecords.cs, GameProjector.Fire, Play/LiveFire.FireRecordVerifier and Studio previews must use the same recorded reading and verdict. See design section 11 for the approved handoff detail. | Retire only the Residual misuse of UsesBypassLane and BypassTerrain as the sole building TEM source after callers migrate; retain their other lane/geometry and legacy-record uses. | Standard Residual/Fire Lane attacks on ordinary-building Bypass targets regain +3 stone/+2 wood; Factory entries use the existing B23.741 answer. Apply TEM at the correct TH/effects stage once. Preserve FT, Snap Shot, CH and cover rules; never add this component to Set DC no-TEM resolution. Deluxe Bypass Residual remains deferred. Old events retain old answers; wiring needs an explicit recorded reading and its own gate. |  | open |',
    '| W-B16.31 | B16.31 | fire | Play/GamePlanner.DemolitionCharges, LiveFire.DemolitionChargeFromState, GamePlanner.Fire and GamePlanner.Ordnance: gather effect family, actual effect Location, terrain, bridge structure/unit position, weather and flooded connection, then ask ScenarioA1MarshHeCalculator.Evaluate. Evaluate thrown-DC target/thrower and Area Target Type Locations separately. Units/State/FireRecords.cs, GameProjector.Fire, Play/LiveFire.FireRecordVerifier and Studio previews must agree on recorded facts/reading. See design section 11. | Remove ScenarioA1MarshHeCalculator.LegacyOrdnanceDivisor only when all recorded-reading callers have a retained compatibility path; preserve original decimal division/serialization and old DC FP for old events. | Applicable marsh/mudflat halves supported HE, DC and HE-equivalency once, after independent CH/Area Fire changes and before IFT-column choice. Bridge structures/on-bridge units, cold and connected flooding can remove it; ground-Location effects do not exempt every bridge occupant. Never halve HE-origin Residual again or assume every OrdnanceHit is ordinary HE. No invented weather/bridge defaults. Swamp HE, VT FFE, TK and unreviewed collateral paths remain outside automatic application. |  | open |',
]
if not any(line.startswith('| W-B23.31 |') for line in lines):
    index = next(i for i, line in enumerate(lines) if line.startswith('| W-C6.17 |'))
    lines[index:index] = wiring
path.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')

path = docs / 'ASL Rule Coverage.md'
text = path.read_text(encoding='utf-8')
text = text.replace('| B16 | Marsh | Mostly not built |', '| B16 | Marsh | Partly |')
text = text.replace('HE into it not halved (16.31); depth, mudflats, and weather (16.5 to 16.8) not built', 'B16.31 supported HE/DC applicability is built in Rules with W-B16.31 open; depth, mudflat and weather consequences beyond this modifier remain open')
text = text.replace('**Cellars and rooftops refused; Rowhouses refused in movement and fire; Factories refused; Fortified Buildings not built in live play**', '**Cellars and rooftops refused; Rowhouses refused in movement and fire; Factory Rules are built with W-B23.74 open; Fortified Buildings not built in live play**')
text = text.replace('- **B23.31** (p. 136): Residual FP', '- **B23.31** (p. 136), Rules repaired by Terrain Fire Batch 2; live behavior remains pending W-B23.31: Residual FP')
text = text.replace('- **B16.31:** a DC into marsh', '- **B16.31**, Rules repaired by Terrain Fire Batch 2; live behavior remains pending W-B16.31: a DC into marsh')
text = text.replace('- **To check, not a settled fault:** ruling R10.7', '- **Settled by Terrain Fire Batch 2, repaired in Rules; live behavior remains pending W-B23.31:** ruling R10.7')
heading = '## Terrain Fire Batch 2, Rules-only delivery (2026-11-04)'
if heading not in text:
    text += '\n' + heading + '\n\nB23.31 and B16.31 have dedicated calculators, contracts, focused tests and two minigames under the approved [design](<Designs/ASL Terrain Fire Batch 2 Rules Design.md>). Ordinary building TEM includes the Residual/Fire Lane Bypass distinction; Factory TEM delegates to the existing B23.741 implementation. Supported marsh HE/DC/HE-equivalency returns an exact factor, with bridge, cold, flooding and mudflat applicability. B16.6/B16.7/B16.8 remain partial because only this modifier is built. Deluxe Bypass Residual, special building Locations, swamp HE and VT FFE remain explicit exclusions. Existing DC, lane-terrain and ordnance answers and serialized arithmetic are unchanged. Inventory section 7 holds W-B23.31 and W-B16.31, coordinated with W-B23.74; no caller or event-schema change is included.\n'
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Unit Backlog.md'
text = path.read_text(encoding='utf-8')
text = text.replace('## 99. Boundaries proposed for Terrain Fire Batch 2', '## 100. Deferred by Terrain Fire Batch 2')
text = text.replace('The [Rules design](<Designs/ASL Terrain Fire Batch 2 Rules Design.md>) awaits approval. These are scope boundaries, not claims or evidence of implementation. Its section 11 holds proposed downstream details separately from implementation scope; the inventory\'s section 7 remains the authoritative wiring record, populated when the Rules claims reach built.', 'The approved [Rules design](<Designs/ASL Terrain Fire Batch 2 Rules Design.md>) is built and independently reviewed. These are remaining boundaries, not another claim board. Inventory section 7 holds the open W-B23.31/W-B16.31 rows; design section 11 retains their handoff rationale.')
text = text.replace('| Proposed boundary |', '| Current boundary |').replace('Proposed W-B23.31 and W-B16.31', 'Inventory W-B23.31 and W-B16.31')
text = text.replace('by the proposed building contract', 'by the building contract').replace('Only HE applicability is proposed here;', 'Only HE applicability is built here;')
text = text.replace('Board 7 [P6] is oracle-verified marsh, not a verified Studio import;', 'Board 7 [P6] was loaded and inspected in isolated Studio 6680 as marsh with no bridge;')
path.write_text(text, encoding='utf-8', newline='\n')
