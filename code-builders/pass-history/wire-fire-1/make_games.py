"""Fresh positions for W-C9.3 governed Studio checks; never overwrite a played game."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / 'test-games'))
from game_file import GameFile

destination = Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
for name, replacement in [('wire-c93-original-check', False), ('wire-c93-replacement-check', True)]:
    game = GameFile(name, 'W-C9.3: ' + ('replace a lost Spotter at the MPh opening' if replacement else 'declare a Spotter and fire'), board='03')
    game.unit('g1', 'squad', 'attacker-squad', 'german', 'bd03:D10:0')
    game.weapon('m1', 'attacker-light-mortar', 'german', 'g1', kind='light-mortar', ti=False)
    game.unit('g2', 'squad', 'attacker-squad', 'german', 'bd03:D9:0')
    game.unit('g3', 'squad', 'attacker-squad', 'german', 'bd03:D9:0')
    game.unit('r1', 'squad', 'defender-squad', 'russian', 'bd03:G10:0')
    game.phases('rph', 'pfph')
    if replacement:
        game.add('mortar-spotter-designated', {'gun': 'm1', 'spotter': 'g2'})
        game.events[-1]['rulePackage'] = 'asl.spotter.v2'
        game.add('conditions-changed', {'id': 'g2', 'conditions': {'asl:broken': True}})
        game.phases('mph')
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
