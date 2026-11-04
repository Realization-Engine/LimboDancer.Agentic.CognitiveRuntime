"""Record the C2.21 Rules-only delivery in existing authoritative records."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

docs = root / 'src/ASL/docs'
path = docs / 'ASL Rule Inventory A to E.md'
lines = path.read_text(encoding='utf-8').splitlines()
for row in inventory_table.rows(lines):
    if row['§'] != 'C2.21':
        continue
    names = list(row)[:-2]
    values = inventory_table.cells(lines[row['line']])
    updates = {
        'Status': 'partly',
        'Claim state': 'built',
        'Where': 'ScenarioA1AmmunitionDefaultCalculator.cs: SelectDefault, StockFacts; legacy callers retain old answers',
        'Rules calculator': 'ScenarioA1AmmunitionDefaultCalculator.cs: SelectDefault, StockFacts; ScenarioA1AmmunitionDefaultContract.cs',
        'Test': 'ScenarioA1AmmunitionDefaultTests; ScenarioA1Pass55RulesTests.AGunWhoseHeHasADepletionNumberNeverFiresItUndeclared, AmmunitionDefaultExtractionPreservesDuelRefusalOrderAndStrings; c221-ammunition-default bundle',
        'Note': 'Batch 3, 2026-11-04: ordinary AP/HE defaults by target protection, Target Type, caliber prohibitions, listed depletion and exhaustion built. Unknowns remain unknown; no substitute default. W-C2.21 is open for live truck default, UI choice/declaration distinction, duel migration and recorded-reading compatibility. Limbered caliber exception C10.24 remains unbuilt; empty/mixed Area target selection and fixed-ammunition weapons are outside this slice. C6.57 separate.',
        'By': 'Codex; independent sub-agent review',
    }
    for name, value in updates.items():
        values[names.index(name)] = value
    lines[row['line']] = '| ' + ' | '.join(values) + ' |'
for index, line in enumerate(lines):
    if line.startswith('| C2.21 | Codex |'):
        values = inventory_table.cells(line)
        values[5] = 'built'
        values[7] = 'Approved revised Batch 3 implemented Rules-only; focused calculator/legacy and bundle checks plus isolated Workbench owner/opponent/adjudicator proof. Independent sub-agent review completed; assigned reviewer Claude and user PR review remain pending. C2.21 coverage stays partial; no ASOP rows. W-C2.21 open and unclaimed. Merge requires user approval.'
        lines[index] = '| ' + ' | '.join(values) + ' |'
text = '\n'.join(lines) + '\n'
wiring = '| W-C2.21 | C2.21 | fire | Play/LiveOrdnance.FromState: gather known target protection, declared Target Type and stock, then ask ScenarioA1AmmunitionDefaultCalculator.SelectDefault; use StockFacts for a known Rules definition. MapStudio/Components/Pages/Play.razor: OrdnanceAmmunitionFor, initialization/reset and HeAssumed suppression must distinguish declarations from defaults. GamePlanner.Ordnance, shot persistence and PlayRecords: retain the selected round and recorded reading. Coordinate Gun Duel candidate preparation with its legacy restriction. See Batch 3 design section 7. | Migrate ScenarioA1Definitions.HeAssumed and the LegacyHeAssumed, LegacyHeNeedsDeclaration and LegacyDuelAmmunitionMatches paths; retain them while any existing live or old-record caller needs the old reading. Keep declaration-choice and C8 APIs. | Ordinary HE replaces AP as the undeclared truck default; unavailable, prohibited, exhausted or limited-stowage preferred rounds require a declaration, without first-offered fallback. Old games retain their answers and schema until a versioned wiring migration is designed and gated. No hidden protection/stock lookup. Unresolved empty/mixed Area defaults need a separate Rules claim, not a Play rule. |  | open |'
if '| W-C2.21 |' not in text:
    text = text.replace('| W-B23.31 |', wiring + '\n| W-B23.31 |', 1)
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Rule Coverage.md'
text = path.read_text(encoding='utf-8')
text = text.replace('- **Default ammunition against an unarmored vehicle (C2.21, p. 167).**', '- **Default ammunition against an unarmored vehicle (C2.21, p. 167), Rules repaired by Batch 3; live behavior pending W-C2.21.**')
text = text.replace('- **C2.21:** a Gun with AP fires AP at an unarmored vehicle by default,', '- **C2.21:** Rules defaults are built by Batch 3; pending W-C2.21, a Gun with AP still fires AP at an unarmored vehicle by default,')
heading = '## Ordnance Decisions Batch 3, Rules-only delivery (2026-11-04)'
if heading not in text:
    text += '\n' + heading + '\n\nC2.21 ammunition defaults have a dedicated calculator, contract, stock adapter and minigame under the approved [design](<Designs/ASL Ordnance Decisions Batch 3 Rules Design.md>). The verdict selects ordinary HE against unarmored targets and AP against armored targets only when permitted and available; it never substitutes another round. Explicit declarations remain separate and unknown facts never become permission. The three legacy predicates preserve live and recorded answers. Inventory section 7 holds open W-C2.21. Overall C2.21 coverage remains partial for the limbered-caliber exception; C6.57, fixed-ammunition weapons and unspecified empty/mixed Area targets are outside this slice.\n'
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'ASL Unit Backlog.md'
text = path.read_text(encoding='utf-8')
text = text.replace('Design-stage scope, 2026-11-04. See [the C2.21 design](<Designs/ASL Ordnance Decisions Batch 3 Rules Design.md>). No claims or production changes are made by this record. Proposed wiring remains in that design until built, then in inventory section 7.', 'Rules-only C2.21 slice built, 2026-11-04. See [the C2.21 design](<Designs/ASL Ordnance Decisions Batch 3 Rules Design.md>). Production callers are unchanged. Inventory section 7 holds open, unclaimed W-C2.21; the design retains handoff rationale.')
text = text.replace('Separate W-C2.21 fire wiring at built;', 'Open W-C2.21 fire wiring in inventory section 7;')
path.write_text(text, encoding='utf-8', newline='\n')
