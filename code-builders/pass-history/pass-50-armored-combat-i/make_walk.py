"""Pass 50, the Studio check of the whole pass: the games the widths are measured in, and the turn walked at 320. Board 3."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from p50lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
for game_id, phases in (("p50-w-mph", ("rph", "pfph", "mph")), ("p50-walk320", ("rph", "pfph", "mph"))):
    g = Game(game_id + SUFFIX, "Pass 50, the Studio check: a tank in [D9] before its move, a squad in [H10] (board 3)")
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:D9:0", "south-east")
    g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:H10:0")
    g.phases(*phases)
    g.write()

g = Game("p50-w-pfph" + SUFFIX, "Pass 50, the Studio check: a tank in [F10] in its PFPh, a squad in [D10] (board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
g.phases("rph", "pfph")
g.write()

g = Game("p50-w-rph" + SUFFIX, "Pass 50, the Studio check: a tank with three malfunctioned weapons in its RPh (board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west", malfunctioned=True, bmg_malfunctioned=True, cmg_malfunctioned=True)
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
g.phases("rph")
g.write()
