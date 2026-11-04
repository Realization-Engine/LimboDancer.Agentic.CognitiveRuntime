"""Rules-only C2.21 position and labelled hand-entered Workbench checks."""
from pathlib import Path
import json
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/test-games'))
from game_file import GameFile

identifier = 'c221-ammunition-default'
folder = root / 'src/ASL/units/minigames' / identifier


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')


game = GameFile(identifier + '-position', 'C2.21: Guns, truck and AFV, Rules-only setup', board='03', other='british', year=1944)
game.events[0]['payload']['phase'] = 'rph'
game.gun('gun', 'campaign-british-25pdr', 'british', 'bd03:D5:0', 'east', crew='firer')
game.gun('limited-gun', 'british-at-gun', 'british', 'bd03:D6:0', 'east', crew='limited-firer')
game.vehicle('truck', 'attacker-truck', 'german', 'bd03:F5:0', 'west')
game.vehicle('tank', 'attacker-tank', 'german', 'bd03:F6:0', 'west')
save(folder / 'games' / (identifier + '-position.game.json'), dict(schemaVersion=1, tenant=game.tenant,
     game=game.game, label=game.label, synthetic=False, minigame=identifier, events=game.events))
card = json.loads((root / 'src/ASL/units/minigames/c617-recovery-history/c617-recovery-history.scenario-card.json').read_text())
card.update(id=identifier, title='C2.21 ammunition defaults', minigame=identifier)
card['catalog'] = game.events[0]['payload']['catalog']
card['boards'][0]['board'] = 'bd03'
card['date'] = dict(day=0, month=7, year=1944)
card['turns']['movesFirst'] = game.events[0]['payload']['phasingSide']
for side in card['sides']:
    if side['side'] == 'russian':
        side['side'] = 'british'
    source = next(item for item in game.events[0]['payload']['sides'] if item['id'] == side['side'])
    side['elr'] = source['elr']
    side['friendlyEdge'] = dict(edge=source['friendlyEdge'], basis='manufactured', note=None)
card['source']['basis'] = 'Manufactured under R0.3 for approved C2.21 Rules-only Batch 3.'
card['source']['adaptation'] = ['Actual catalog counters on board 3; checks are hand-entered facts, not played firing events.',
    'Target protection is known-public or hypothetical. The 6-pounder HE# example uses its catalog profile; other AP#/HE#, exhaustion and alternative weapon variants are labelled hypothetical.']
card['introduction'] = 'Inspect the catalog 25-pounder game-default profile at [D5], British 6-pounder with HE# at [D6], truck at [F5], and AFV at [F6]. No shot is played and no live default changes here.'
save(folder / (identifier + '.scenario-card.json'), card)
base = dict(declared=False, firerId='firer', weaponKind='gun', targetType='vehicle', targetProtection='unarmored',
    apProhibited=False, apListedDepletion=False, apExhausted=False, heProhibited=False, heListedDepletion=False, heExhausted=False)
checks = []


def check(name, outcome, changes=None, viewer='british', sealed=None):
    facts = dict(base, **(changes or {}))
    entry = dict(name=name, position='setup', viewer=viewer, contract='c2.21-ammunition-default', facts=facts, expect=dict(outcome=outcome))
    if sealed:
        entry['sealed'] = sealed
    checks.append(entry)


check('Ordinary HE at the visible truck', 'default-he')
check('Ordinary AP at the visible AFV', 'default-ap', dict(targetProtection='armored'))
check('Hypothetical Infantry Target Type at the truck', 'default-he', dict(targetType='infantry'))
check('Hypothetical mortar at an unarmored target', 'default-he', dict(weaponKind='mortar', targetType='area'))
check('Hypothetical armored Area target needs declaration', 'declaration-required', dict(targetProtection='armored', targetType='area'))
check('Hypothetical HE# never defaults', 'declaration-required', dict(heListedDepletion=True))
check('Catalog 6-pounder HE# requires declaration', 'declaration-required', dict(firerId='limited-firer', heProhibited=True, heListedDepletion=True))
check('Hypothetical AP# never defaults', 'declaration-required', dict(targetProtection='armored', apListedDepletion=True))
check('Hypothetical exhausted HE has no AP fallback', 'declaration-required', dict(heExhausted=True))
check('Hypothetical no-HE counter has no AP fallback', 'declaration-required', dict(heProhibited=True))
check('Explicit declaration bypasses selection', 'not-applicable', dict(declared=True))
check('Unknown target protection remains unknown', 'not-evaluated', dict(targetProtection=None))
check('Opponent cannot inspect stock', 'not-evaluated', dict(firerId=None, weaponKind=None,
    apProhibited=None, apListedDepletion=None, apExhausted=None, heProhibited=None, heListedDepletion=None, heExhausted=None),
    'german', {'firerId': 'british'})
check('Adjudicator ordinary truck default', 'default-he', viewer='adjudicator')
save(folder / (identifier + '.minigame.json'), dict(id=identifier, title='C2.21 ammunition defaults', card=identifier,
    rules=['C2.21'], contracts=['c2.21-ammunition-default'], units={'firer': 'british', 'limited-firer': 'british', 'truck': 'german', 'tank': 'german'}, checks=checks))
