"""The Studio games of increment 5b (the return to normal: A15.46; ruling R72.7).

Both games are on board 3, in Open Ground at level 0 (code-builders/boards/los-survey "terrain" mode, bd03, 2026-10-27: [A1], [A2], and [A3] are
Open Ground at level 0). LOS and ranges, read with the LosCalculator through code-builders/boards/los-survey ("pair" mode, built outside the repository,
2026-10-27):
- game a fires at its own Location ([A2] to [A2], TPBF, range 0) and game b is CC in [A2]: neither relies on any LOS pair or route.

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# (a) A15.46, A15.432, A7.21, ruling R72.7: the berserk Russian 4-4-7 shares [A2] with the German 4-4-7 HS, not in Melee; Russian AFPh. Fire r-1 at
# [A2] (its own Location): before the pass the Fire package refused it ("a unit or weapon is named twice"), the firer listed among its targets; now the
# proposal reads TPBF, 4 FP x 3, halved for the AFPh: 6 FP. Dice: 1, 1 (a 1KIA on the 6 column); a Random Selection dr may follow, any value. The HS is
# eliminated, and the record line reads "4-4-7 squad ... in [A2] eliminated every Known enemy unit in its Location by its TPBF or FT in the AFPh, so it is
# no longer berserk and returns to Good Order (A15.46)". Read back: after "fire-resolved", a "conditions-changed" of r-1 with "asl:berserk": false, its
# "causes" the fire's event id; the fire's "facts" "targets" list g-hs alone. With a 3 + 4 instead (no kill), r-1 stays berserk and no such event is written.
g = GameFile("p72-berserk-tpbf-return", "pass 72, 5b: a berserk squad's halved TPBF eliminates the last German there and it returns to normal (A15.46)",
             phasing="russian")
g.unit("r-1", "squad", "defender-squad", "russian", "bd03:A2:0", berserk=True)
g.unit("g-hs", "half-squad", "attacker-half-squad", "german", "bd03:A2:0")
g.phases("rph", "pfph", "mph", "dfph", "afph", side="russian")
g.write()

# (b) A15.46, ruling R72.7: the berserk German 4-6-7 g-1 and the lone Russian tank ru-tk share [A2] as the German CCPh begins (a berserk unit may not move
# in the APh, A15.431, so the file writes them there; the Setup action would refuse a vehicle beside an enemy, ruling R25.3, but a game file does not pass
# through it). The same setup, the same nationalities and dice, is the Play test BacklogPass72Tests.ABerserkSquadSetUpWithALoneTankInItsLocationDestroysIt
# InTheCcphAndReturnsToNormal (events loaded through the projector, the CC planned and committed, no replay error).
# Clicks: German side, Close Combat panel, the vehicle attack in [A2]: attackers g-1, the vehicle ru-tk (g-1 owes it, A15.43); propose, confirm.
# Dice: one roll, 1 + 2 = 3 against the record's Kill Number 5: "eliminated", the tank a wreck. (Not 1 + 1: its Original 2 draws a Leader Creation dr.)
# The record line reads "4-6-7 squad ... in [A2] eliminated every Known enemy unit in its Location by destroying the enemy vehicle in CC, so it is no longer
# berserk and returns to Good Order (A15.46)". Read back: after "vehicle-close-combat-resolved" and "vehicle-wrecked", a "conditions-changed" of g-1 with
# "asl:berserk": false, its "causes" the CC record's event id. With a 6 + 6 instead, no kill and g-1 stays berserk (no such event).
g = GameFile("p72-berserk-vehicle-cc-return", "pass 72, 5b: a berserk squad destroys the Russian tank alone in its Location in CC and returns to normal (A15.46)",
             phasing="german")
g.unit("g-1", "squad", "attacker-squad", "german", "bd03:A2:0", berserk=True)
g.vehicle("ru-tk", "defender-tank", "russian", "bd03:A2:0", "east")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph", side="german")
g.write()
