"""Lock the two approved Rules-only Vehicle Bypass claims."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

path = root / 'src/ASL/docs/ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
claims = {'D2.321': 'BypassTurret', 'D2.33': 'BypassVca'}
lines = text.splitlines()
for row in inventory_table.rows(lines):
    if row['§'] in claims:
        assert row['Claim state'] == 'open'
        names = list(row)[:-2]
        values = inventory_table.cells(lines[row['line']])
        values[names.index('Owner')] = row['§'] + ' (Codex)'
        values[names.index('Claim state')] = 'claimed'
        lines[row['line']] = '| ' + ' | '.join(values) + ' |'
text = '\n'.join(lines) + '\n'
records = []
for rule, name in claims.items():
    assert f'| {rule} | Codex |' not in text
    files = [f'ScenarioA1{name}{part}.cs' for part in ('Models', 'Calculator', 'Contract')]
    files.append('ScenarioA1VehicleMovementCalculator.cs (shared)')
    if rule == 'D2.321':
        files.append('ScenarioA1MotionAttempts.cs (shared)')
    records.append(f'| {rule} | Codex | Claude | codex/vehicle-bypass-batch-4 | {", ".join(files)} | claimed | 2026-11-04 | Approved revised Batch 4 Rules-only design. Existing callers and serialized facts retain their answers and shape. Explicit unsupported boundaries; zero exclusive ASOP rows. Proposed wiring stays in the design until built, then inventory section 7. |')
index = text.index('| C2.21 | Codex | Claude |')
text = text[:index] + '\n'.join(records) + '\n' + text[index:]
path.write_text(text, encoding='utf-8', newline='\n')
