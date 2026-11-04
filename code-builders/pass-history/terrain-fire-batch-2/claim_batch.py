"""Reserve the two approved Terrain Fire Rules claims, without taking wiring."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/documents'))
import inventory_table

path = root / 'src/ASL/docs/ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
claims = {
    'B23.31': ('BuildingFire', 'ScenarioA1FireMapRules.cs (shared)'),
    'B16.31': ('MarshHe', 'ScenarioA1FireCalculator.cs (shared)'),
}
owners = {rule: rule for rule in claims}
owners.update({rule: 'B16.31' for rule in ('B16.6', 'B16.7', 'B16.8')})
lines = text.splitlines()
for row in inventory_table.rows(lines):
    if row['§'] in owners:
        assert row['Claim state'] == 'open', row['§']
        names = list(row)[:-2]
        values = inventory_table.cells(lines[row['line']])
        values[names.index('Owner')] = owners[row['§']] + ' (Codex)'
        values[names.index('Claim state')] = 'claimed'
        lines[row['line']] = '| ' + ' | '.join(values) + ' |'
text = '\n'.join(lines) + '\n'
new_rows = []
for rule, (family, edge) in claims.items():
    assert f'| {rule} | Codex |' not in text
    files = [f'ScenarioA1{family}{part}.cs' for part in ('Models', 'Calculator', 'Contract')] + [edge]
    new_rows.append(f'| {rule} | Codex | Claude | codex/terrain-fire-batch-2 | {", ".join(files)} | claimed | 2026-11-04 | Approved revised Terrain Fire Batch 2 design. Rules only; B16.6/B16.7/B16.8 belong to B16.31 for HE applicability only. No ASOP rows cite these sections. Wiring remains separate in section 7 when built. |')
index = text.index('| A25.61 | Codex | Claude |')
text = text[:index] + '\n'.join(new_rows) + '\n' + text[index:]
path.write_text(text, encoding='utf-8', newline='\n')
