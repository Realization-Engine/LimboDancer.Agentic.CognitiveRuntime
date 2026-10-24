"""The Studio games of increment 2d (loading and unloading completed; rulings R65.14, R65.15), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402


def aboard(game, identifier, kind, definition, side, vehicle, **conditions):
    """A unit riding a vehicle as a Passenger (D6.1); game_file has no helper for one."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": "passenger"}, "conditions": marks(CLEAR, conditions)}})


# An unload under fire (D6.5, A4.6, R65.14): a Stopped German truck at F8 (Open Ground) with a 4-6-7 aboard; a Russian 4-4-7 with a LMG at F9, ADJACENT;
# the German MPh. The squad unloads; the Russian's First Fire takes FFNAM and FFMO (-1 each). The truck's move then ends, and the squad moves next, on four
# MF without Double Time, before any other German unit (the 4-6-7 at F7 is refused until then).
g = GameFile("p65-unload-fire", "pass 65, 2d: a 4-6-7 unloads from a truck in the open beside a 4-4-7 with a LMG (D6.5, A4.6)", year=1943, month=7)
g.vehicle("dt", "attacker-truck", "german", "bd03:F8:0", "east")
aboard(g, "ds", "squad", "attacker-squad", "german", "dt")
g.unit("dw", "squad", "attacker-squad", "german", "bd03:F7:0")
g.unit("rs", "squad", "defender-squad", "russian", "bd03:F9:0")
g.weapon("rl", "defender-lmg", "russian", "rs")
g.phases("rph", "pfph", "mph")
g.write()

# Boarding at the end of a move (D6.4, R65.15): a German 4-6-7 at F10 moves to F9 and F8 (two MF, Open Ground) and boards the unmoved SPW 251/1 at F8 as
# its last step, once the Russian passes; the halftrack keeps a quarter of its MP. The Russian 4-4-7 is far off at J2.
g = GameFile("p65-board-after-move", "pass 65, 2d: a 4-6-7 moves two hexes and boards a halftrack at the end of its move (D6.4)", year=1943, month=7)
g.vehicle("dh", "attacker-halftrack", "german", "bd03:F8:0", "east", bu=False)
g.unit("ds", "squad", "attacker-squad", "german", "bd03:F10:0")
g.weapon("dlmg", "attacker-lmg", "german", "ds")
g.unit("rs", "squad", "defender-squad", "russian", "bd03:J2:0")
g.phases("rph", "pfph", "mph")
g.write()
