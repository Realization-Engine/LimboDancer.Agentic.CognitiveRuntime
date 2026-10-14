"""Pass 45, task 45.1b: the games. Board 3, the stone building of [M2] (its stairwell hex; [N1] and [N2] of the building have no stairwell).

Usage: python make_451b.py <game id> <mode>
  cut        a Russian squad at level 2 of [M2]; German squads at level 1 and at ground level of [M2]. The German PFPh: the squad at level 1
             fires up at it; the target is Encircled with no counter (A7.72)
  open       the German squad at level 1 is broken, but the one at ground level still holds the only stairwell: still Encircled
  free       the control: both German squads are broken, so the path to ground level is free
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "cut": "Pass 45 task 45.1b: a Russian squad at level 2 of [M2] is cut off from ground level by a German squad at level 1 (A7.72; board 3)",
    "open": "Pass 45 task 45.1b: the German squad at level 1 of [M2] is broken, but ground level is still held: no path (A7.72; board 3)",
    "free": "Pass 45 task 45.1b control: both German squads in [M2] are broken, so the squad at level 2 has its path (A7.72; board 3)",
}
game = Game(GAME, LABELS[MODE])
game.unit("r-one", "squad", "defender-squad", "russian", "bd03:M2:2")
game.unit("g-one", "squad", "attacker-squad", "german", "bd03:M2:1", **({"broken": True} if MODE in ("open", "free") else {}))
game.unit("g-two", "squad", "attacker-squad", "german", "bd03:M2:0", **({"broken": True} if MODE == "free" else {}))
game.phases("pfph")
game.write()
