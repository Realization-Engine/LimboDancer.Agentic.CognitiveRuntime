"""Create a separate Rally workflow review position, without overwriting any game."""
import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'test-games'))
from game_file import GameFile  # noqa: E402

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=Path)
parser.add_argument('--game', required=True)
args = parser.parse_args()
if args.destination.name != args.game + '.game.json':
    raise SystemExit('The destination filename must match the game id.')
if args.destination.exists():
    raise SystemExit('Refusing to overwrite an existing game.')

game = GameFile(args.game, 'Rally workflow review: multiple hexes, both sides, leaders and unassisted units')
game.unit('g-rally', 'squad', 'attacker-squad', 'german', 'bd03:F5:0', broken=True)
game.unit('g-leader', 'leader', 'attacker-leader-8-1', 'german', 'bd03:F5:0')
game.unit('g-alone', 'squad', 'attacker-squad', 'german', 'bd03:D10:0', broken=True, dm=True)
game.unit('g-broken-leader', 'leader', 'attacker-leader-8-1', 'german', 'bd03:F6:0', broken=True)
game.unit('g-deploy', 'squad', 'attacker-squad', 'german', 'bd03:J5:0')
game.unit('g-organize', 'leader', 'attacker-leader-8-1', 'german', 'bd03:J5:0')
game.unit('g-half-1', 'half-squad', 'attacker-half-squad', 'german', 'bd03:J5:0')
game.unit('g-half-2', 'half-squad', 'attacker-half-squad', 'german', 'bd03:J5:0')
game.unit('r-rally', 'squad', 'defender-squad', 'russian', 'bd03:P5:0', broken=True)
game.unit('r-leader', 'leader', 'defender-leader', 'russian', 'bd03:P5:0')
game.unit('r-alone', 'squad', 'defender-squad', 'russian', 'bd03:R5:0', broken=True)
game.phases('rph')
record = dict(schemaVersion=1, tenant=game.tenant, game=game.game, label=game.label, synthetic=False, events=game.events)
with args.destination.open('x', encoding='utf-8', newline='\n') as output:
    json.dump(record, output, indent=2)
    output.write('\n')
print(f'Created {game.game}: {len(game.events)} setup events; no weapons or prior actions.')
