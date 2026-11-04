"""Reserve the approved C2.21 default-ammunition slice, without claiming wiring."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

path = root / 'src/ASL/docs/ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
assert '| C2.21 | Codex |' not in text
lines = text.splitlines()
for row in inventory_table.rows(lines):
    if row['§'] == 'C2.21':
        assert row['Claim state'] == 'open'
        names = list(row)[:-2]
        values = inventory_table.cells(lines[row['line']])
        values[names.index('Owner')] = 'C2.21 (Codex)'
        values[names.index('Claim state')] = 'claimed'
        lines[row['line']] = '| ' + ' | '.join(values) + ' |'
text = '\n'.join(lines) + '\n'
files = [f'ScenarioA1AmmunitionDefault{part}.cs' for part in ('Models', 'Calculator', 'Contract')]
files += [f'{name}.cs (shared)' for name in ('ScenarioA1Definitions', 'ScenarioA1OrdnanceCalculator', 'ScenarioA1GunDuels')]
row = f'| C2.21 | Codex | Claude | codex/ordnance-decisions-batch-3 | {", ".join(files)} | claimed | 2026-11-04 | Approved revised Batch 3 design: ammunition defaults only. Existing answers and serialized facts retained. C2.21 overall stays partial for limbered caliber; C6.57 separate. Zero ASOP rows. Wiring recorded in section 7 at built. |\n'
index = text.index('| B23.31 | Codex | Claude |')
text = text[:index] + row + text[index:]
path.write_text(text, encoding='utf-8', newline='\n')
