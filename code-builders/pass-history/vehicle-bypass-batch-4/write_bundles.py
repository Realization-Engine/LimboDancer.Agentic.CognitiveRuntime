"""Setup-only real-board anchors with explicitly hypothetical Bypass frame checks."""
from pathlib import Path
import json
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/test-games'))
from game_file import GameFile

def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8', newline='\n')

frame = dict(frameObstacleQ=0, frameObstacleR=0, frameAcrossQ=0, frameAcrossR=1, frameFrontQ=1, frameFrontR=0)
checks = []
def add(contract, facts, name, outcome='evaluated', viewer='german', sealed=None):
    check = dict(name=name, position='setup', viewer=viewer, contract=contract, facts=facts, expect=dict(outcome=outcome))
    if sealed: check['sealed'] = sealed
    checks.append(check)

base = dict(frame, inBypass=True, vehicleId='tank', targetQ=0, targetR=2, current='Front', proposed='Left',
            traverse='St', weapon='Ordnance', mode='Ordinary', specialTerrain=False)
tid = 'd2.321-bypass-tca'
add(tid, base, 'Hypothetical ST front-to-side: normal 2 and separate 1')
add(tid, dict(base, current='Left', applicableAcquisition=False), 'Unchanged side at new target: normal 2, separate 0')
add(tid, dict(base, current='Left', applicableAcquisition=True), 'Unchanged side acquired target: zero')
add(tid, dict(base, targetQ=-1, targetR=1, proposed='Rear', direction='Clockwise'), 'ST front-to-rear through side: normal 3 and separate 1')
add(tid, dict(base, targetQ=-1, targetR=1, proposed='Rear'), 'Opposite position requires direction', 'not-evaluated')
add(tid, dict(base, current='Left'), 'Unknown side acquisition stays unknown', 'not-evaluated')
add(tid, dict(base, current='Left', weapon='Cmg', sameKnownTarget=True, sameTargetFacing=True), 'CMG same Known target in same facing')
add(tid, dict(base, mode='BoundingFirstFire'), 'BFF cannot turn with shot', 'refused')
add(tid, dict(base, current='Left', mode='BoundingFirstFire', earlierMpTcaChange=False), 'BFF no earlier MP turret change')
add(tid, dict(base, current='Left', mode='BoundingFirstFire', earlierMpTcaChange=True), 'Earlier MP turret change requires separate composition', 'not-evaluated')
add(tid, dict(base, specialTerrain=True), 'Special firing-point terrain is unsupported', 'not-evaluated')
private = dict(base, vehicleId=None, current=None, proposed=None, traverse=None, weapon=None)
add(tid, private, 'Opponent cannot inspect turret profile', 'not-evaluated', 'british', {'vehicleId': 'german'})
turret_checks = checks
checks = []
turn = dict(frame, inBypass=True, pendingTurn=False, edgeOneQ=0, edgeOneR=0, edgeTwoQ=1, edgeTwoR=0, purpose='ContinueMovement')
add('d2.33-bypass-turn', turn, 'Public local turn costs 1 MP, continuation required', viewer='british')
add('d2.33-bypass-turn', dict(turn, purpose='FireOnly'), 'VCA cannot pivot only to fire', 'refused')
add('d2.33-bypass-turn', dict(turn, pendingTurn=True), 'Second pending turn refused', 'refused')
continuation = dict(turn)
continuation.pop('purpose')
continuation.update(action='Entry', pendingTurn=True, reverse=True, positionKind='Ordinary', ordinaryQ=0, ordinaryR=1, ordinaryFacing=5)
add('d2.33-bypass-continuation', continuation, 'Reverse through old CAFP enters across-lane hex')
add('d2.33-bypass-continuation', dict(continuation, ordinaryQ=0, ordinaryR=0), 'Cannot enter bypassed obstacle', 'refused')
add('d2.33-bypass-continuation', dict(inBypass=True, action='EndMovement', pendingTurn=True, endCause='Voluntary'), 'No destination needed to refuse voluntary end', 'refused')
add('d2.33-bypass-continuation', dict(inBypass=True, action='EndMovement', pendingTurn=True, endCause='Immobilized', immobilized=True), 'Known immobilization forces end', 'forced-end')
add('d2.33-bypass-continuation', dict(inBypass=True, action='EndMovement', endCause='OtherForcedHalt'), 'Other forced halts stay unsupported', 'not-evaluated')
defensive = dict(frame, inBypass=True, stage='TurnPending', originKind='HexCenter', originQ=1, originR=0)
add('d2.33-bypass-defensive-position', defensive, 'Public pending-turn shot uses old front frame', viewer='british')
add('d2.33-bypass-defensive-position', dict(defensive, immobilized=True), 'Immobilized while pending keeps old frame')
add('d2.33-bypass-defensive-position', dict(defensive, originKind='Cafp'), 'Incoming CAFP origin is unsupported', 'not-evaluated')
add('d2.33-bypass-defensive-position', dict(stage='EntryCompleted', positionKind='Ordinary', ordinaryQ=0, ordinaryR=1, ordinaryFacing=5), 'Completed ordinary exit needs no old frame', 'not-applicable')

for identifier, rule, entries, title in [
    ('d2321-bypass-tca', 'D2.321', turret_checks, 'Bypass turret positions and Case A'),
    ('d233-bypass-vca', 'D2.33', checks, 'Bypass VCA and retained defensive position')]:
    folder = root / 'src/ASL/units/minigames' / identifier
    game = GameFile(identifier + '-position', title + ': setup anchors, hypothetical Bypass facts', board='23', other='british', year=1944)
    game.events[0]['payload']['phase'] = 'rph'
    game.vehicle('tank', 'attacker-tank', 'german', 'bd23:BB3:0', 'south-east', concealed=True)
    game.vehicle('observer', 'campaign-british-sherman75', 'british', 'bd23:AA4:0', 'north-east')
    # Actual catalog anchor; neither turret nor movement state is changed by these hand-entered checks.
    save(folder / 'games' / (identifier + '-position.game.json'), dict(schemaVersion=1, tenant=game.tenant, game=game.game,
         label=game.label, synthetic=False, minigame=identifier, events=game.events))
    card = json.loads((root / 'src/ASL/units/minigames/c221-ammunition-default/c221-ammunition-default.scenario-card.json').read_text())
    card.update(id=identifier, title=title, minigame=identifier)
    card['catalog'] = game.events[0]['payload']['catalog']
    card['boards'][0]['board'] = 'bd23'
    card['source']['basis'] = 'Manufactured under R0.3 for approved Rules-only Vehicle Bypass Batch 4.'
    card['source']['adaptation'] = ['Board 23 anchors [BB3] and [AA4]. Counters are at hex centers, not in a played Bypass state.',
        'All axial Bypass positions, turret profiles and pending-turn stages are labelled hypothetical Workbench facts. No clearance, LOS or live firing/movement acceptance is claimed.']
    card['introduction'] = 'Inspect board 23 and the catalog anchors. Use the hand-entered canonical axial frame for the four turret positions and retained CAFP; it is not a live game transition.'
    card['victoryConditions']['text'] = 'A setup-only Rules fixture with no victory condition or played acceptance. The German anchor is concealed for opponent-disclosure checks.'
    save(folder / (identifier + '.scenario-card.json'), card)
    save(folder / (identifier + '.minigame.json'), dict(id=identifier, title=title, card=identifier, rules=[rule],
         contracts=list(dict.fromkeys(item['contract'] for item in entries)), units={'tank': 'german', 'observer': 'british'}, checks=entries))
