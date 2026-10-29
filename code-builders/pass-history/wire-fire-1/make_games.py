"""Fresh positions for W-C9.3 governed Studio checks; never overwrite a played game."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / 'test-games'))
from game_file import GameFile

destination = Path(sys.argv[1])
fixtures = {
    'wire-c93-original-check': ('declare a Spotter and fire', 'original'),
    'wire-c93-replacement-check': ('replace a broken Spotter at the MPh opening', 'broken'),
    'wire-c93-captured-check': ('replace a captured Spotter at the MPh opening', 'captured'),
    'wire-c93-eliminated-check': ('replace an eliminated Spotter at the MPh opening', 'eliminated'),
    'wire-c93-retained-pfph-check': ('fire with a retained original Spotter in the later PFPh', 'pfph'),
    'wire-c93-retained-dfph-check': ('fire with a retained original Spotter in the enemy DFPh', 'dfph'),
}
selected = sys.argv[2] if len(sys.argv) > 2 else None
if selected is not None and selected not in fixtures:
    raise SystemExit(f'Unknown fixture {selected}; choose one of: {", ".join(fixtures)}')
names = [selected] if selected is not None else list(fixtures)
destination.mkdir(parents=True, exist_ok=True)
for name in names:
    path = destination / (name + '.game.json')
    if path.exists():
        raise SystemExit(f'Refusing to overwrite {path}')
for name in names:
    label, scenario = fixtures[name]
    game = GameFile(name, 'W-C9.3: ' + label, board='03')
    game.unit('g1', 'squad', 'attacker-squad', 'german', 'bd03:D10:0')
    game.weapon('m1', 'attacker-light-mortar', 'german', 'g1', kind='light-mortar', ti=False)
    game.unit('g2', 'squad', 'attacker-squad', 'german', 'bd03:D9:0')
    game.unit('g3', 'squad', 'attacker-squad', 'german', 'bd03:D9:0')
    game.unit('r1', 'squad', 'defender-squad', 'russian', 'bd03:G10:0')
    game.phases('rph', 'pfph')
    if scenario != 'original':
        game.add('mortar-spotter-designated', {'gun': 'm1', 'spotter': 'g2'})
        game.events[-1]['rulePackage'] = 'asl.spotter.v2'
    if scenario in ('broken', 'captured', 'eliminated'):
        if scenario == 'eliminated':
            game.add('instance-eliminated', {'id': 'g2'})
        else:
            game.add('conditions-changed', {'id': 'g2', 'conditions': {'asl:' + scenario: True}})
        game.phases('mph')
    elif scenario in ('pfph', 'dfph'):
        # Recorded transitions clear the designation's original PFPh marks before the later shot.
        game.phases('mph', 'dfph', 'afph', 'rtph', 'aph', 'ccph')
        game.phases('rph', 'pfph', 'mph', 'dfph', side='russian')
        if scenario == 'pfph':
            game.phases('afph', 'rtph', 'aph', 'ccph', side='russian')
            game.phases('rph', 'pfph', turn=2)
    # GameFile uses one numeric sequence for the entire fixture. These independent fixture actions must not look like one command:
    # the opening reader groups the live planner's {attempt}-{index} identifiers. No event in these fixtures has a causal reference.
    for event in game.events:
        if event.get('causes'):
            raise SystemExit('Update fixture causal references before changing its event identifiers')
        event['eventId'] = f"{name}-fixture-{event['revision']}-1"
    record = {'schemaVersion': 1, 'tenant': game.tenant, 'game': game.game, 'label': game.label, 'synthetic': False, 'events': game.events}
    path = destination / (name + '.game.json')
    if path.exists():
        raise SystemExit(f'Refusing to overwrite {path}')
    path.write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(path)
