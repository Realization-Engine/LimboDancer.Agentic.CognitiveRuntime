"""Four manufactured Rules-only positions with explicit what-if contract facts."""
from pathlib import Path
import copy
import json
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/test-games'))
from game_file import GameFile

home = root / 'src/ASL/units/minigames'
template = json.loads((home / 'c617-recovery-history/c617-recovery-history.scenario-card.json').read_text())

def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')

def check(name, contract, facts, outcome, viewer, sealed=None):
    result = dict(name=name, position='setup', viewer=viewer, contract=contract, facts=facts, expect=dict(outcome=outcome))
    if sealed:
        result['sealed'] = sealed
    return result

def bundle(identifier, title, rules, checks, side):
    folder = home / identifier
    card = copy.deepcopy(template)
    card.update(id=identifier, title=title, minigame=identifier)
    card['date'] = dict(day=1, month=7, year=1944)
    card['source']['basis'] = 'Manufactured under R0.3 for the approved Nationality Batch 1 design; Rules-only verdicts on imported bd01.'
    card['source']['adaptation'] = ['Actual catalog counters at [G4], [H4] and [G5]. Missing Eritrean, crew and weapon variants are explicit hand-entered what-if facts, not catalog definitions. Live caller behavior waits for the separate wiring rows.']
    card['introduction'] = title + '. The owner knows its own counters; the opponent sees a sealed enemy stack. Contract examples vary facts explicitly, including prisoner status, printed class and date.'
    card['turns'].update(setsUpFirst=side, movesFirst=side)
    card['sides'][1]['side'] = side
    save(folder / (identifier + '.scenario-card.json'), card)
    ids = {'subject': side, 'hs': side, 'elite': side, 'enemy': 'german'}
    save(folder / (identifier + '.minigame.json'), dict(id=identifier, title=title, card=identifier, rules=rules,
         contracts=list(dict.fromkeys(item['contract'] for item in checks)), checks=checks, units=ids))
    game = GameFile(identifier + '-position', title + ': Rules-only position', phasing=side, board='01', other=side, year=1944, month=7)
    game.events[0]['payload']['phase'] = 'rph'
    game.unit('subject', 'squad', side + '-squad', side, 'bd01:G4:0', concealed=True)
    game.unit('elite', 'squad', side + '-elite-squad', side, 'bd01:H4:0')
    game.unit('hs', 'half-squad', side + '-half-squad', side, 'bd01:H4:0')
    game.unit('enemy', 'squad', 'attacker-squad', 'german', 'bd01:G5:0')
    if side == 'finnish':
        game.unit('leader', 'leader', 'finnish-leader-8-0', side, 'bd01:G4:0')
    save(folder / 'games' / (identifier + '-position.game.json'), dict(schemaVersion=1, tenant=game.tenant, game=game.game,
         label=game.label, synthetic=False, minigame=identifier, events=game.events))

def hidden(facts):
    return {key: value if key in ('context', 'action', 'mode', 'scenarioDate') else None for key, value in facts.items()}

contract = 'a25.61-italian-deployment'
base = dict(subjectId='subject', nationality='italian', kind='squad', elite=False, context='ordinary', guard=False, unarmed=False, broken=False)
checks = [check('Italian non-elite squad nationality restriction', contract, base, 'restricted', 'italian'),
          check('Elite squad passes only the nationality restriction', contract, dict(base, elite=True, subjectId='elite'), 'unrestricted', 'italian'),
          check('Unbroken non-elite Guard exception', contract, dict(base, context='guard', guard=True), 'unrestricted', 'italian'),
          check('Temporary Crew exchange retains its separate prerequisites', contract, dict(base, context='temporary-crew'), 'unrestricted', 'adjudicator'),
          check('Eritrean inheritance with explicit facts', contract, dict(base, nationality='eritrean'), 'restricted', 'adjudicator'),
          check('Opponent cannot learn a concealed squad class', contract, hidden(base), 'not-evaluated', 'german', {'subjectId':'italian'})]
bundle('a25-italian-deployment', 'A25.61 Italian deployment', ['A25.61', 'A25.66'], checks, 'italian')

contract = 'a25.71-organization'
base = dict(subjectId='subject', nationality='finnish', kind='squad', action='deploy', mode='leaderless', guard=False, unarmed=False, broken=False, carrierHalfSquad=False)
checks = [check('Finnish squad chooses a leaderless 1TC', contract, base, 'leaderless-1tc', 'finnish'),
          check('The same squad explicitly chooses directed NTC', contract, dict(base, mode='directed'), 'directed-ntc', 'finnish'),
          check('Unbroken Guard deploys automatically', contract, dict(base, guard=True), 'automatic', 'adjudicator'),
          check('Finnish HS need no leader to recombine', contract, dict(base, subjectId='hs', kind='half-squad', action='recombine', mode='not-applicable'), 'no-leader-required', 'finnish'),
          check('Opponent cannot evaluate the concealed Finnish squad', contract, hidden(base), 'not-evaluated', 'german', {'subjectId':'finnish'})]
bundle('a25-finnish-organization', 'A25.71 Finnish organization', ['A25.71'], checks, 'finnish')

contract = 'a25.63-capture-modifier'
base = dict(subjectId='subject', nationality='italian', elite=False, inexperienced=False)
checks = [check('Experienced non-elite Italian capture term is zero', contract, base, 'zero', 'italian'),
          check('Inexperience replaces the capture term with minus one', contract, dict(base, inexperienced=True), 'minus-one', 'italian'),
          check('Elite Italian capture term remains plus one', contract, dict(base, elite=True, subjectId='elite'), 'plus-one', 'italian'),
          check('Opponent cannot read hidden defender class', contract, hidden(base), 'not-evaluated', 'german', {'subjectId':'italian'})]
escape = dict(prisonerId='hs', nationality='italian', captorNationality='german', infantry=True, axisMinorAtHome=False, guardBroken=True, meleeInLocation=False)
for national, old in [('italian', 'refused-nationality'), ('eritrean', 'not-evaluated')]:
    checks.append(check(national + ' in unchanged legacy contract', 'a20.55-prisoner-escape', dict(escape, nationality=national), old, 'german'))
    checks.append(check(national + ' in current inheritance contract', 'a25.63-prisoner-escape', dict(escape, nationality=national), 'refused-nationality', 'german'))
bundle('a25-italian-capture', 'A25.63 Italian capture and escape', ['A25.63', 'A25.66'], checks, 'italian')

contract = 'a25.75-equipment-use'
base = dict(subjectId='subject', userNationality='finnish', personnel=True, printedClass='elite', weaponNationality='russian', weaponKind='lmg', originMark='not-applicable', scenarioDate='1940-01', physicallyCaptured=True)
checks = [check('Russian LMG in 1940 is exempt for Finnish Personnel', contract, base, 'exempt', 'finnish'),
          check('Russian LMG in 1939 still has Captured Use penalties', contract, dict(base, scenarioDate='1939-12'), 'captured-use', 'finnish'),
          check('Russian .50 HMG is never within the exemption', contract, dict(base, weaponKind='hmg50', scenarioDate='not-applicable'), 'captured-use', 'finnish'),
          check('Finnish PSK before July 1944 is refused', contract, dict(base, weaponKind='psk', weaponNationality='german', scenarioDate='1944-06'), 'refused-use', 'adjudicator'),
          check('Finnish PSK in July 1944 is exempt for elite Personnel', contract, dict(base, weaponKind='psk', weaponNationality='german', scenarioDate='1944-07'), 'exempt', 'finnish'),
          check('Finnish Conscript pays penalties even on Finnish-color PSK', contract, dict(base, weaponKind='psk', weaponNationality='finnish', printedClass='conscript', scenarioDate='1945-12'), 'captured-use', 'adjudicator'),
          check('Reciprocal Finnish r-mark MG exemption', contract, dict(base, userNationality='russian', weaponNationality='finnish', originMark='r', scenarioDate='not-applicable'), 'exempt', 'adjudicator'),
          check('Relevant missing date cannot grant LMG exemption', contract, dict(base, scenarioDate='not-applicable'), 'not-evaluated', 'finnish'),
          check('Opponent cannot inspect the concealed firer equipment', contract, hidden(base), 'not-evaluated', 'german', {'subjectId':'finnish'})]
bundle('a25-finnish-equipment', 'A25.75 Finnish equipment use', ['A25.75', 'A25.77'], checks, 'finnish')
