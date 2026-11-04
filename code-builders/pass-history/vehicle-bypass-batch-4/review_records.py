"""Correct PR 126's coverage wording and record its legacy-refusal consolidation."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
docs = root / 'src/ASL/docs'
path = docs / 'ASL Rule Coverage.md'
text = path.read_text(encoding='utf-8')
old = 'Delay (2.17) and the Bypass TCA (2.321) not built;'
new = 'Delay (2.17) was not built at the original audit; Bypass TCA (2.321) Rules components are partly built by Batch 4, with live wiring and special composition still open;'
assert old in text or new in text
path.write_text(text.replace(old, new), encoding='utf-8', newline='\n')

path = docs / 'ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
text = text.replace('Keep TurretStepBar, MotionAttempts.ChangeBar, CoveredArcTurn and old fire-history answers while any old-reading caller needs them.',
    'Keep TurretStepBar and MotionAttempts.ChangeBar, now delegating their Bypass refusals to ScenarioA1BypassTurretCalculator.LegacyTurretStepRefusal and LegacyMotionChangeRefusal; keep CoveredArcTurn and old fire-history answers while any old-reading caller needs them.')
path.write_text(text, encoding='utf-8', newline='\n')

path = docs / 'Designs/ASL Vehicle Bypass Batch 4 Rules Design.md'
text = path.read_text(encoding='utf-8').replace('Only compatibility documentation changes the existing turret/Motion-attempt refusals.',
    'The existing turret/Motion-attempt Bypass refusals delegate to compatibility helpers in the turret calculator, preserving their text and precedence.')
heading = '## 12. PR 126 assigned review corrections'
if heading not in text:
    text += '\n' + heading + '\n\nThe assigned reviewer found the Rules, geometry, old answers and disclosure sound. The D2 coverage summary now identifies D2.321 as partly built, with wiring and special composition open. The optional consolidation is also implemented: `LegacyTurretStepRefusal` and `LegacyMotionChangeRefusal` own the two historical Bypass refusals; public methods retain earlier prerequisite checks and exact text. Focused regressions cover refusal precedence, no-change/non-Bypass results and the existing null interpolation. Work remains constant-sized with no new retained state or map reads. W-D2.321 in inventory section 7 names the helpers; no new wiring record or production wiring was added. Claim states remain built pending the user\'s word.\n'
path.write_text(text, encoding='utf-8', newline='\n')
