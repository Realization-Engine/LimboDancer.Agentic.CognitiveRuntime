"""Enforce LF/UTF-8 on the explicitly scoped Batch 4 delivery files after formatting."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
rules = root / 'src/ASL/LimboDancer.Domains.Asl.Rules'
paths = list(rules.glob('ScenarioA1Bypass*.cs'))
paths += [rules / name for name in ('ScenarioA1MotionAttempts.cs', 'ScenarioA1VehicleMovementCalculator.cs')]
paths += list((root / 'src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests').glob('ScenarioA1Bypass*Tests.cs'))
paths += list(Path(__file__).parent.glob('*.py')) + list(Path(__file__).parent.glob('*.ps1'))
for bundle in ('d2321-bypass-tca', 'd233-bypass-vca'):
    paths += list((root / 'src/ASL/units/minigames' / bundle).rglob('*.json'))
for name in ('ASL Rule Inventory A to E.md', 'ASL Rule Coverage.md', 'ASL Unit Backlog.md', 'ASL Unit Time Log.md',
             'Designs/ASL Vehicle Bypass Batch 4 Rules Design.md', 'ASL Card Play and Map Studio Redesign Plan.md'):
    paths.append(root / 'src/ASL/docs' / name)
missing = [str(path) for path in paths if not path.is_file()]
assert not missing, missing
for path in paths:
    path.write_text(path.read_text(encoding='utf-8-sig'), encoding='utf-8', newline='\n')
print(f'Normalized {len(paths)} scoped files.')
