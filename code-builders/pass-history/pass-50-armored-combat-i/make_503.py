"""Pass 50, task 50.3: the MA while moving. Board 3."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from p50lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""

g = Game("p50-bff-outset" + SUFFIX, "Pass 50 task 50.3: a PzKpfw IIIH in [F10] fires its MA at [D10] at the outset of its MPh (Case C), must spend an MP before the next shot, and Delays (board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
g.phases("rph", "pfph", "mph")
g.write()

for game_id, label, phases in (
        ("p50-bff-move", "Pass 50 task 50.3: a tank starts in [D9], enters [D10] turning its turret east, and fires at [H10] moving (Case C2, the lower dr doubled), stopped, and after Delay MP (board 3)",
         ("rph", "pfph", "mph")),
        ("p50-afph", "Pass 50 task 50.3: a tank moves from [D9] to [D10], stops, and fires its MA in the AFPh with Case C (board 3)", ("rph", "pfph", "mph")),
        ("p50-afph-ctl", "CONTROL of p50-afph: the tank in [D10], facing east, has not moved and fires in the AFPh with Case B alone (board 3)", ("rph", "pfph", "mph", "dfph", "afph"))):
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:D10:0" if game_id == "p50-afph-ctl" else "bd03:D9:0", "east" if game_id == "p50-afph-ctl" else "south-east")
    g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:H10:0")
    g.phases(*phases)
    g.write()
