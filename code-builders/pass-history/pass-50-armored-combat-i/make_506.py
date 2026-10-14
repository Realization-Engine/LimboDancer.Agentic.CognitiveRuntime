"""Pass 50, task 50.6: a tank fires at an enemy tank in its own Location. Board 3."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from p50lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
for game_id, label in (
        ("p50-same-rear", "Pass 50 task 50.6: a PzKpfw IIIH fires at a T-34 in its own Location [F10]; a colored dr of 1 strikes the rear Target Facing (D3.2; board 3)"),
        ("p50-same-front", "CONTROL of p50-same-rear: the same shot with a colored dr of 5 strikes the front Target Facing (D3.2; board 3)")):
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
    g.vehicle("ru-tank", "defender-tank", "russian", "bd03:F10:0", "east")
    g.phases("rph", "pfph")
    g.write()
