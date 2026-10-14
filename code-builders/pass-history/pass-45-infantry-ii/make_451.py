"""Pass 45, task 45.1: the games. Board 3.

Usage: python make_451.py <game id> <mode> [hex]
  up         a German squad at ground level of a building hex and a Russian squad at its level 1. The German PFPh: fire up the stairwell (A7.21, B23.26)
  two        the control: the Russian squad at level 2; the fire is refused (B23.26)
  encircle   a Russian squad at level 1, German squads at ground level and at level 2 of the hex. The German PFPh: two attacks, from below and from
             above (A7.7)
The hex defaults to M5 for "up" and N2 for the others.
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
HEX = sys.argv[3] if len(sys.argv) > 3 else ("M5" if MODE == "up" else "N2")
LABELS = {
    "up": f"Pass 45 task 45.1: a squad at ground level of [{HEX}] fires up the stairwell at a squad at level 1, as PBF (A7.21, B23.26; board 3)",
    "two": f"Pass 45 task 45.1 control: a squad at ground level of [{HEX}] may not fire at level 2 of its hex (B23.26; board 3)",
    "encircle": f"Pass 45 task 45.1: fire from the levels above and below Encircles a squad at level 1 of [{HEX}] (A7.7; board 3)",
}
game = Game(GAME, LABELS[MODE])
at = "bd03:" + HEX + ":"
game.unit("g-one", "squad", "attacker-squad", "german", at + "0")
if MODE == "up":
    game.unit("r-one", "squad", "defender-squad", "russian", at + "1")
elif MODE == "two":
    game.unit("r-one", "squad", "defender-squad", "russian", at + "2")
else:
    game.unit("r-one", "squad", "defender-squad", "russian", at + "1")
    game.unit("g-two", "squad", "attacker-squad", "german", at + "2")
game.phases("pfph")
game.write()
