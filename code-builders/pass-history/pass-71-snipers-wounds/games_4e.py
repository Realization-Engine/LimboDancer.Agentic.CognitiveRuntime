"""The Studio games of increment 4e (every A14.1 trigger; ruling R71.13), on board 3.

The German SAN is 3 and the German Sniper is in [M7]; the Russian SAN is left unset, so no German DR calls anything. A Russian DR of 3 in a trigger phase
calls the German Sniper; its dr then decides (a 4, 5, or 6 has no effect and is recorded as "none"; a 1 or 2 stops at the German side's choice to attack
or forfeit, as in 4d).

The one LOS pair relied on is 4a's to 4d's: the Russian 4-4-7 in [P8] (woods) to the German 4-6-7 in [P10] (Open Ground), range 2 (pass 70's read of board
3 with the LosCalculator, 2026-10-25). The tank of game (a) is placed in [P8] with the squad's LOS. Before the games are used, check in the Studio's board
viewer that [P11] is beside [P10] and is terrain a squad may leave into [P10] for 1 MF (game b), and that the tank in [P8] may fire at [P10] (its VCA
south-east holds [P10]; if the panel refuses, turn the facing to south-west in this script).

Read back in each game file: "sniper-attacked" with "sniper": "gs" and "trigger" the roll id of the triggering DR: in (a) the "toHit" roll of the
"ordnance-fired" record's "rolls", in (b) the "attack" roll of the Residual FP "fire-resolved" record (its "facts" read "fireKind": "residual").

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


# (a) An ordnance To Hit DR (A14.1: "a TH ... Original DR"; R55.30 superseded). The Russian PFPh: the Russian tank (defender-tank) in [P8], facing
# south-east, fires its MA at g1 in [P10]. Dice, in order, in the dice box: To Hit 1, 2 (an Original 3: it hits and calls the German Sniper); the hit's IFT
# DR 6, 6 (no effect); then the German Sniper's dr 4 (no effect). The Russian view's record line reads the Sniper's dr; before the increment no shot called
# a Sniper.
g = GameFile("p71-sniper-to-hit", "pass 71, 4e: a Russian MA's To Hit DR of 3 calls the German Sniper (A14.1)", phasing="russian")
g.san("german", 3)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:P10:0")
g.vehicle("ru-tk", "defender-tank", "russian", "bd03:P8:0", "south-east")
g.sniper("gs", "german", "bd03:M7:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A Residual FP attack on a move (A8.2: the DEFENDER makes its DR). The German MPh: g1 moves from [P11] to [P10]; the Russian r1 in [P8] First Fires
# at it, dice 6, 5 (no effect), leaving Residual FP in [P10]; the Russian passes, and g1's move is ended. g2 then moves from [P11] to [P10] and is attacked
# by the Residual FP: its DR 1, 2 is the Russian's 3 and calls the German Sniper. Dice after it: any check g2 takes (enter 4, 4), then the Sniper's dr 4
# (no effect). Before the increment no move called a Sniper.
g = GameFile("p71-sniper-residual", "pass 71, 4e: a Residual FP DR of 3 on a move calls the German Sniper (A14.1, A8.2)", phasing="german")
g.san("german", 3)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:P11:0")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:P11:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.sniper("gs", "german", "bd03:M7:0")
g.phases("rph", "pfph", "mph", side="german")
g.write()
