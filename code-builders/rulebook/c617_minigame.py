"""Generate the C6.17 Rules-only Workbench checks. No played events or rule decisions are generated."""
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
BUNDLE = ROOT / 'src/ASL/units/minigames/c617-recovery-history'
sys.path.insert(0, str(ROOT / 'code-builders/test-games'))
from game_file import GameFile  # noqa: E402


def main():
    BUNDLE.mkdir(parents=True, exist_ok=True)
    card = json.loads((ROOT / 'src/ASL/units/minigames/support-weapon-scope/support-weapon-scope.scenario-card.json').read_text(encoding='utf-8'))
    card.update(id='c617-recovery-history', title='C6.17 recovery history', minigame='c617-recovery-history',
                introduction='Two original moving-stack members keep their own MF and each weapon\'s shots. Hand-entered Rules checks, not a played acceptance game.')
    card['source']['basis'] = 'Manufactured under R0.3 to prove C6.17 (PDF pp. 173-174) with explicit single-target facts on imported bd01.'
    card['source']['adaptation'] = ['The Rules-stage Workbench supplies history facts. Played recovery transitions wait for caller wiring.']
    card['turns']['movesFirst'] = 'russian'
    base = dict(phase='MPh', firerId='g1', targetId='r2', weaponId='gun1', location='bd01:G5:0', halfMfSpent=4, shotsHere=0, inCurrentWindow=True)
    cases = [
        ('B entered and recovered: first shot', {}, 'allowed', 'adjudicator'),
        ('B retains a second shot', {'shotsHere': 1}, 'allowed', 'adjudicator'),
        ('B allowance exhausted', {'shotsHere': 2}, 'refused', 'adjudicator'),
        ('A shot does not charge B: joint shot already counted', {'shotsHere': 1}, 'allowed', 'adjudicator'),
        ('Half-MF rounds down', {'halfMfSpent': 3, 'shotsHere': 1}, 'refused', 'adjudicator'),
        ('Minimum one', {'halfMfSpent': 0}, 'allowed', 'adjudicator'),
        ('History does not reopen a window', {'inCurrentWindow': False}, 'refused', 'adjudicator'),
        ('Unknown MF is not permission', {'halfMfSpent': None}, 'not-evaluated', 'adjudicator'),
        ('Fractional half-MF is invalid', {'halfMfSpent': 1.5}, 'not-evaluated', 'adjudicator'),
        ('Target history hidden from firing side', {'targetId': None, 'halfMfSpent': None}, 'not-evaluated', 'german'),
        ('Weapon history hidden from target side', {'firerId': None, 'weaponId': None, 'shotsHere': None}, 'not-evaluated', 'russian'),
    ]
    manifest = dict(id=card['id'], title=card['title'], card=card['id'], rules=['C6.17'],
                    contracts=['c6.17-personnel-history'], units={'g1': 'german', 'r1': 'russian', 'r2': 'russian'},
                    example={'source': 'Manufactured under R0.3; not a printed example or a played recovery sequence.', 'page': 173},
                    checks=[dict(name=name, position='setup', viewer=viewer, contract='c6.17-personnel-history',
                                 facts=base | changes, expect={'outcome': outcome}) for name, changes, outcome, viewer in cases])
    for suffix, value in [('scenario-card', card), ('minigame', manifest)]:
        (BUNDLE / f'{card["id"]}.{suffix}.json').write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8', newline='')
    game = GameFile('ui-c617-rules-01', 'C6.17 Rules Workbench: supplied allowance facts', phasing='russian', board='01')
    game.gun('gun1', 'attacker-inf-gun', 'german', 'bd01:G2:0', 'south-east', crew='g1')
    for unit in ('r1', 'r2'):
        game.unit(unit, 'squad', 'defender-squad', 'russian', 'bd01:G5:0')
    for weapon in ('lmg1', 'lmg2'):
        game.add('instance-created', {'instance': {'id': weapon, 'kind': 'asl:mg', 'definition': 'defender-lmg',
                 'side': 'russian', 'position': {'at': 'bd01:G5:0'}, 'conditions': {'asl:malfunctioned': False}}})
    game.phases('rph', 'pfph', 'mph')
    record = dict(schemaVersion=1, tenant=game.tenant, game=game.game, label=game.label, synthetic=True,
                  minigame=card['id'], events=game.events)
    games = BUNDLE / 'games'
    games.mkdir(exist_ok=True)
    (games / f'{game.game}.game.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8', newline='')


if __name__ == '__main__':
    main()
