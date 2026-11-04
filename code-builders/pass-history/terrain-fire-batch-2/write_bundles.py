"""Manufactured Rules-only positions; every terrain variant is explicitly hypothetical."""
from pathlib import Path
import copy
import json
import sys

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders/test-games'))
from game_file import GameFile
from paths import WORK

home = root / 'src/ASL/units/minigames'
template = json.loads((home / 'c617-recovery-history/c617-recovery-history.scenario-card.json').read_text())


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')


def check(name, contract, facts, outcome, reason=None, viewer='adjudicator', sealed=None):
    result = dict(name=name, position='setup', viewer=viewer, contract=contract, facts=facts, expect=dict(outcome=outcome))
    if reason:
        result['expect']['reasons'] = [reason]
    if sealed:
        result['sealed'] = sealed
    return result


def bundle(identifier, title, board, rules, checks, target, firer, note):
    folder = home / identifier
    card = copy.deepcopy(template)
    card.update(id=identifier, title=title, minigame=identifier)
    card['date'] = dict(day=0, month=7, year=1944)
    card['boards'][0]['board'] = 'bd' + board
    card['source']['basis'] = 'Manufactured under R0.3 for the approved Terrain Fire Batch 2 design; Rules-only hand-entered verdicts.'
    card['source']['adaptation'] = [note, 'No live caller is wired by these examples.']
    card['introduction'] = title + '. ' + note
    save(folder / (identifier + '.minigame.json'), dict(id=identifier, title=title, card=identifier, rules=rules,
         contracts=list(dict.fromkeys(item['contract'] for item in checks)), checks=checks, units={'target': 'russian', 'firer': 'german'}))
    game = GameFile(identifier + '-position', title + ': Rules-only setup', board=board, year=1944)
    game.events[0]['payload']['phase'] = 'rph'
    started = game.events[0]['payload']
    card['catalog'] = started['catalog']
    card['turns']['movesFirst'] = started['phasingSide']
    for side in card['sides']:
        recorded = next(item for item in started['sides'] if item['id'] == side['side'])
        side['elr'] = recorded['elr']
        side['friendlyEdge'] = dict(edge=recorded['friendlyEdge'], basis='manufactured', note=None)
    save(folder / (identifier + '.scenario-card.json'), card)
    game.unit('target', 'squad', 'defender-squad', 'russian', f'bd{board}:{target}:0', concealed=True)
    game.unit('firer', 'squad', 'attacker-squad', 'german', f'bd{board}:{firer}:0')
    if board == '07':
        game.weapon('charge', 'attacker-dc', 'german', 'firer', kind='dc')
    save(folder / 'games' / (identifier + '-position.game.json'), dict(schemaVersion=1, tenant=game.tenant,
         game=game.game, label=game.label, synthetic=False, minigame=identifier, events=game.events))


contract = 'b23.31-building-fire-tem'
base = dict(buildingKind='stone', locationKind='ordinary', profile='standard', attackKind='direct', roofless=False,
            targetId='target', bypassing=False)
checks = [
    check('Ordinary stone building, no firer identity needed', contract, base, 'building-tem', 'contribution: 3', 'russian'),
    check('Hypothetical wooden material', contract, dict(base, buildingKind='wood'), 'building-tem', 'contribution: 2'),
    check('Hypothetical non-Residual Bypass removes only building TEM', contract, dict(base, bypassing=True), 'no-building-tem', 'contribution: 0', 'russian'),
    check('Hypothetical standard Residual retains building TEM', contract, dict(base, bypassing=True, attackKind='residual'), 'building-tem', 'contribution: 3', 'russian'),
    check('Hypothetical Fire Lane uses the Residual classification', contract, dict(base, bypassing=True, attackKind='residual'), 'building-tem', 'contribution: 3'),
    check('Hypothetical Deluxe Bypass Residual remains unevaluated', contract, dict(base, bypassing=True, attackKind='residual', profile='deluxe'), 'not-evaluated', 'J2.23'),
    check('Enemy private Bypass cannot be supplied', contract, dict(base, targetId=None, bypassing=None), 'not-evaluated', viewer='german', sealed={'targetId': 'russian'}),
    check('Residual answer does not reveal enemy Bypass', contract, dict(base, targetId=None, bypassing=None, attackKind='residual'), 'building-tem', 'contribution: 3', 'german', {'targetId': 'russian'}),
    check('Hypothetical Factory internal trace delegates plus one', contract, dict(base, buildingKind='stone-factory', firerId='firer', firerPlace='inside', tracedWithinDepiction=True), 'building-tem', 'contribution: 1'),
    check('Hypothetical Factory Residual across building hexside', contract, dict(base, buildingKind='stone-factory', attackKind='residual', residualEntry='building-hexside'), 'building-tem', 'contribution: 1', 'russian'),
    check('Hypothetical Factory Residual Bypass infers entry', contract, dict(base, buildingKind='stone-factory', attackKind='residual', bypassing=True), 'building-tem', 'contribution: 3', 'russian'),
    check('Hypothetical contradictory Factory entry is rejected', contract, dict(base, buildingKind='stone-factory', attackKind='residual', bypassing=True, residualEntry='building-hexside'), 'not-evaluated', 'contradict'),
]
bundle('b23-building-fire', 'B23.31 building fire', '01', ['B23.31'], checks, 'X3', 'X4',
       'Actual stone building at [X3]/[X4], with no Factory SSR. Bypass, entry, wood, Factory and Deluxe variants are labelled hypothetical facts. Use the existing b23-factory card for a real Factory SSR position.')

contract = 'b16.31-marsh-he'
base = dict(effectFamily='he', terrainKind='marsh', snowCover='none', subZero=False, connectedToFloodedWater=False,
            bridgePresent=False, targetKind='unit', targetId='target', onBridge=None)
checks = [
    check('Ordinary HE into actual marsh', contract, base, 'halved', 'multiplier: 0.5', 'russian'),
    check('DC component into actual marsh', contract, dict(base, effectFamily='dc'), 'halved', 'multiplier: 0.5', 'russian'),
    check('HE equivalency is also halved', contract, dict(base, effectFamily='he-equivalency'), 'halved', 'multiplier: 0.5'),
    check('Residual from HE is not halved again', contract, dict(base, effectFamily='residual'), 'unchanged', 'multiplier: 1'),
    check('Enemy bridge position is irrelevant where no bridge exists', contract, dict(base, targetId=None), 'halved', 'multiplier: 0.5', 'german', {'targetId': 'russian'}),
    check('Hypothetical bridge presence does not exempt an off-bridge unit', contract, dict(base, bridgePresent=True, onBridge=False), 'halved', 'multiplier: 0.5', 'russian'),
    check('Hypothetical on-bridge unit is exempt', contract, dict(base, bridgePresent=True, onBridge=True), 'unchanged', 'multiplier: 1', 'russian'),
    check('Hypothetical bridge with hidden enemy position is unevaluated', contract, dict(base, bridgePresent=True, targetId=None), 'not-evaluated', viewer='german', sealed={'targetId': 'russian'}),
    check('Hypothetical bridge structure is exempt without a unit', contract, dict(base, bridgePresent=True, targetKind='bridge', targetId=None), 'unchanged', 'multiplier: 1'),
    check('Hypothetical ground effect is separate from bridge units', contract, dict(base, bridgePresent=True, targetKind='ground-location', targetId=None), 'halved', 'multiplier: 0.5'),
    check('Hypothetical mudflat inherits halving', contract, dict(base, terrainKind='mudflat'), 'halved', 'multiplier: 0.5'),
    check('Hypothetical Snow removes marsh treatment', contract, dict(base, snowCover='snow'), 'unchanged', 'multiplier: 1'),
    check('Hypothetical flooded connection removes marsh treatment', contract, dict(base, connectedToFloodedWater=True), 'unchanged', 'multiplier: 1'),
    check('Hypothetical swamp HE is explicitly unsupported', contract, dict(base, terrainKind='swamp'), 'not-evaluated', 'Swamp'),
    check('Hypothetical swamp Residual does not get HE reduction', contract, dict(base, terrainKind='swamp', effectFamily='residual'), 'unchanged', 'multiplier: 1'),
    check('Hypothetical VT FFE does not use ordinary halving', contract, dict(base, effectFamily='unsupported-vt-ffe'), 'not-evaluated', 'VT FFE'),
]
bundle('b16-marsh-he', 'B16.31 marsh HE', '07', ['B16.31', 'B16.6', 'B16.7', 'B16.8'], checks, 'P6', 'O6',
       'Actual marsh at [P6] and an adjacent German DC carrier at [O6]. No bridge is printed at [P6]. Bridge, mudflat, cold, flood and special-profile checks are hypothetical facts, not alterations of this board.')

# Additional local evidence on the existing Factory card; no new Factory bundle or wiring.
demo = GameFile('b23-factory-terrain-fire', 'Existing Factory SSR: Batch 2 contract demonstration', board='01', special=['factory:X3'])
demo.events[0]['payload']['phase'] = 'rph'
demo.unit('target', 'squad', 'defender-squad', 'russian', 'bd01:X3:0')
demo.unit('firer', 'squad', 'attacker-squad', 'german', 'bd01:X4:0')
save(Path(WORK) / 'terrain-fire-factory-position.game.json', dict(schemaVersion=1, tenant=demo.tenant, game=demo.game,
     label=demo.label, synthetic=False, minigame='b23-factory', events=demo.events))
