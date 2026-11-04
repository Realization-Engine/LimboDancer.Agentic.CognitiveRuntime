"""Record approved nationality claims after the A20.54 review releases its hold."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'code-builders/documents'))
import inventory_table

path = ROOT / 'src/ASL/docs/ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
assert '| A25.61 | Codex |' not in text
claims = {
    'A25.61': ('ItalianDeployment', ['ScenarioA1ItalianNationalityRules.cs', 'ScenarioA1Definitions.cs (shared)']),
    'A25.63': ('ItalianCapture', ['ScenarioA1PrisonerCalculator.cs', 'ScenarioA1CloseCombatCalculator.cs (shared)']),
    'A25.71': ('FinnishOrganization', ['ScenarioA1SupportWeaponRules.cs (shared)']),
    'A25.75': ('CapturedEquipmentUse', ['ScenarioA1FireEligibility.cs (shared)']),
}
owners = {rule: rule for rule in claims}
owners.update({'A25.66': 'A25.61', 'A25.77': 'A25.75'})
lines = text.splitlines()
for row in inventory_table.rows(lines):
    rule = row['§']
    if row['Owner'] == 'A20.54 (Claude)' and row['Claim state'] == 'built':
        lines[row['line']] = lines[row['line']].replace('| built | a20-prisoners |', '| reviewed | a20-prisoners |')
    if rule in owners:
        assert row['Claim state'] == 'open', rule
        cells = inventory_table.cells(lines[row['line']])
        names = list(row)[:-2]
        cells[names.index('Owner')] = owners[rule] + ' (Codex)'
        cells[names.index('Claim state')] = 'claimed'
        if rule == 'A25.63':
            cells[names.index('Note')] = 'A25.63 removes the ordinary +1 capture DRM against non-elite Italians. The current CC resolver still applies it. Italian escape is already decided by the A20.54 prisoner calculator; this claim reuses it and adds explicit A25.66 inheritance without changing legacy callers.'
        lines[row['line']] = '| ' + ' | '.join(cells) + ' |'
text = '\n'.join(lines) + '\n'
lines = text.splitlines()
for index, line in enumerate(lines):
    if line.startswith('| A20.54 | Claude | Codex |'):
        lines[index] = line.replace('| built |', '| reviewed |')[:-1] + ' Read-only Codex review 2026-11-04 verified the prior independent review and b0eea430 fixes; no new blocker in scope. Eritrean inheritance follows in A25.63. |'
text = '\n'.join(lines) + '\n'
rows = []
for rule, (family, edges) in claims.items():
    files = [f'ScenarioA1{family}{part}.cs' for part in ('Models', 'Calculator', 'Contract')] + edges
    rows.append(f'| {rule} | Codex | Claude | codex/nationality-batch-1 | {", ".join(files)} | claimed | 2026-11-04 | User approved Nationality Batch 1 design, Rules only. A25.66 inheritance belongs to A25.61; A25.77 belongs to A25.75. Downstream wiring is separate. |')
index = text.index('| C6.17 | Codex | Claude |')
text = text[:index] + '\n'.join(rows) + '\n' + text[index:]
path.write_text(text, encoding='utf-8', newline='\n')
