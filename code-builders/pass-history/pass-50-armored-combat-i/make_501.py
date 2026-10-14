"""Pass 50, tasks 50.1 and 50.2: a tank's BMG and CMG on the IFT, and the repair of each weapon. Board 3."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from p50lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""


def both(game_id, label, facing):
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", facing)
    g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
    g.phases("rph", "pfph")
    g.write()


both("p50-mg-both", "Pass 50 tasks 50.1, 50.2: a PzKpfw IIIH in [F10] facing west fires its BMG and CMG together at [D10], then its MA (board 3)", "west")
both("p50-mg-turn", "Pass 50 task 50.2: the tank faces south-west; its CMG turns the turret for [D10] with Case A, the MA pays the same, the BMG is refused (board 3)",
     "south-west")
both("p50-mg-ctl", "CONTROL of p50-mg-turn: the tank faces west and its CMG fires at [D10] with no Case A (board 3)", "west")

g = Game("p50-repair" + SUFFIX, "Pass 50 task 50.1: a tank repairs its BMG (dr 1) and has its MA disabled (dr 6), which Recalls it (board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west", malfunctioned=True, bmg_malfunctioned=True, cmg_malfunctioned=True)
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
g.phases("rph")
g.write()

g = Game("p50-repair-ctl" + SUFFIX, "CONTROL of p50-repair: a CE halftrack repairs its MA AAMG as before the pass (board 3)")
g.vehicle("de-ht", "attacker-halftrack", "german", "bd03:F10:0", "west", malfunctioned=True)
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
g.phases("rph")
g.write()

g = Game("p50-mg-first" + SUFFIX, "Pass 50 task 50.1: a T-34 First Fires its CMG at a squad moving into [D10], then fires its MA in the DFPh (A.15; board 3)")
g.vehicle("ru-tank", "defender-tank", "russian", "bd03:F10:0", "west")
g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:C10:0")
g.phases("rph", "pfph", "mph")
g.write()
