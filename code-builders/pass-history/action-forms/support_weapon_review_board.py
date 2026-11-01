"""Record the user's approved SW-01 exception and the review's missing file holds.

Run against a clean Codex claim worktree on current main. Only the existing inventory changes.
"""
from pathlib import Path
import subprocess
import sys

source = Path(__file__).resolve().parents[3]
target = Path(sys.argv[1])
relative = 'src/ASL/docs/ASL Rule Inventory A to E.md'
path = target / relative
rules_only = '--rules-only' in sys.argv[2:]
lines = (subprocess.check_output(['git', '-c', 'safe.directory=' + target.as_posix(), 'show', 'HEAD:' + relative],
                                cwd=target, text=True, encoding='utf-8') if rules_only else path.read_text(encoding='utf-8')).splitlines()
branch = 'codex/support-weapons-implementation'
groups = {'A4.43': 'movement', 'A9.72': 'rally', 'A9.8': 'fire', 'A11.21': 'close combat'}
ids = {rule: 'wire-' + capability.replace(' ', '-') + '-sw' for rule, capability in groups.items()}
extra = {'A4.43': ['ScenarioA1SupportWeaponRules.cs (shared)'],
         'A9.72': ['ScenarioA1RallyRules.cs (shared)'], 'A9.8': [], 'A11.21': []}
files = subprocess.check_output(['git', '-c', 'safe.directory=' + source.as_posix(), 'diff', '--name-only',
                                 'origin/main...HEAD'], cwd=source, text=True, encoding='utf-8').splitlines()
held = {rule: [] for rule in groups}
for file in files:
    if not file.startswith('src/ASL/') or file.endswith('.md') or '/tests/' in file or '/units/minigames/' in file or '.Rules/' in file:
        continue
    name = Path(file).name
    rule = ('A11.21' if any(term in name for term in ('CloseCombat', 'Withdrawal', 'Ambush'))
            else 'A9.72' if any(term in name for term in ('Rally', 'Repair'))
            else 'A9.8' if any(term in name for term in ('Fire', 'Ordnance', 'Configuration')) else 'A4.43')
    held[rule].append(file.removeprefix('src/ASL/') + ' (shared)')

for index, line in enumerate(lines):
    if not line.startswith('| '):
        continue
    cells = [cell.strip() for cell in line[1:-1].split('|')]
    if len(cells) == 8 and cells[0] in groups and cells[3] == branch:
        for file in extra[cells[0]]:
            if file not in cells[4]:
                cells[4] += ', ' + file
        cells[5] = 'built'
        cells[7] = 'SW-01, user approved 2026-10-31: A4.43, A9.72, A9.8 and A11.21 may ship with their explicitly held wiring files in PR 96. Wiring gate and green CI remain mandatory. Merge requires a fresh user instruction. User also approved reserving Rules files here while the wiring claims and checker update travel together in PR 96, because main cannot yet read the exception. Review corrections remain pending.'
        lines[index] = '| ' + ' | '.join(cells) + ' |'
    elif len(cells) > 10 and cells[0] in ('A4.43', 'A4.431', 'A4.44', 'A9.72', 'A9.8', 'A11.21'):
        # The inventory's Owner and Claim state are the two columns before its final audit column.
        header = next([cell.strip() for cell in text[1:-1].split('|')] for text in reversed(lines[:index]) if text.startswith('| § | Title | Page | Status |'))
        cells[header.index('Claim state')] = 'built'
        lines[index] = '| ' + ' | '.join(cells) + ' |'

if rules_only:
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')
    raise SystemExit(0)

source_lines = (source / relative).read_text(encoding='utf-8').splitlines()
for rule, capability in groups.items():
    row = next(text for text in source_lines if text.startswith('| W-' + rule + ' |'))
    cells = [cell.strip() for cell in row[1:-1].split('|')]
    cells[-2:] = [ids[rule] + ' (Codex)', 'claimed']
    cells[3] = cells[3].replace('Delivered under the combined exception:', 'Implemented; PR 96 corrections pending under SW-01:')
    row = '| ' + ' | '.join(cells) + ' |'
    existing = next((i for i, text in enumerate(lines) if text.startswith('| W-' + rule + ' |')), None)
    if existing is None:
        at = lines.index('## 8. Wiring claims')
        while not lines[at - 1].startswith('|'):
            at -= 1
        lines.insert(at, row)
    else:
        lines[existing] = row
    claim = '| ' + ' | '.join([ids[rule], 'Codex', 'Claude', branch, ', '.join(held[rule]), capability,
                               'claimed', '2026-10-31', 'SW-01. Implementation exists; review corrections, Studio verification and wiring gate pending. No merge authorization.']) + ' |'
    lines.append(claim)

lines.extend(['', '## 9. Approved combined deliveries', '',
              '| Exception | Rule claims | Wiring claims | Approved | State | Wiring gate | Conditions |',
              '|---|---|---|---|---|---|---|',
              '| SW-01 | ' + ', '.join(groups) + ' | ' + ', '.join(ids.values())
              + ' | 2026-10-31 | approved | passsupport-weapons.conf | A4.43, A9.72, A9.8 and A11.21 may ship with their explicitly held wiring files in PR 96. The wiring gate and green CI remain mandatory. Merge requires a fresh user instruction. User approved this record on 2026-10-31. |', ''])
path.write_text('\n'.join(lines), encoding='utf-8', newline='\n')
