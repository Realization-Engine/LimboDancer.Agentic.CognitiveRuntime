"""The Studio games of increment 4f (wounds: the pin after more than 3 MF and the SMC crew's Stun wound; rulings R71.14, R71.15), on board 3.

The one LOS pair relied on is 4a's to 4e's: the Russian 4-4-7 in [P8] (woods) to [P10] (Open Ground), range 2 (pass 70's read of board 3 with the
LosCalculator, 2026-10-25). Not checked by the building agent (no Studio): that [P11] to [P14] are Open Ground hexes in a line south of [P10], each 1 MF
(game a), that [P11] is beside [P10] (game b, as 4e's game b), and that [M7] is terrain a tank may stand in with [M8] south of it (game b). Check them in
the board viewer first, and move the path or the tank in this script if one is not.

The Russian SAN is 6 in both games. A German DR of 6 in a trigger phase calls the Russian Sniper.

Read back in each game file:
(a) the "fire-resolved" record's "facts" "targets" entry for de-l: "woundPins": true after 4 MF (absent after 3); de-l's conditions "asl:wounded" and
    "asl:pinned" after the fire.
(b) a "dice-rolled" with "purpose": "wound-severity" and "roll" "<attempt>-wound-de-l", just before the "conditions-changed" of de-tk that sets
    "asl:stunned"; with dr 1 de-l is "asl:wounded", with dr 5 he is eliminated and de-tk is "asl:abandoned". The Records tab's line reads
    "Wound Severity dr 1 for de-l: a wound, not mortal ...".

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


# (a) The pin after more than 3 MF (A17.2). The German MPh: the German 8-1 de-l moves alone [P14], [P13], [P12], [P11], [P10], 1 MF each, the Russian
# passing his fire after each step but the last; at [P10], after 4 MF, r1 in [P8] First Fires at him. Dice, every one a 2: the IFT DR 2, 2 is a Final 2
# (-1 FFMO, -1 FFNAM), K/2 on the 4 FP column; the Random Selection die; the Wound Severity dr 2 (a wound, not mortal); the 2MC 2, 2 (+2, against his
# wounded 7, passes). He is wounded and pinned. Fire at him in [P11] instead, after 3 MF, and he is wounded but not pinned.
g = GameFile("p71-wound-pin", "pass 71, 4f: a leader wounded by Defensive First Fire after 4 MF is pinned (A17.2)", phasing="german")
g.san("russian", 6)
g.unit("de-l", "leader", "attacker-leader-8-1", "german", "bd03:P14:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases("rph", "pfph", "mph", side="german")
g.write()

# (b) The SMC crew's Stun wound (D5.343, A17.11). The German MPh: the German 8-1 de-l crews the Abandoned, CE PzKpfw IIIH de-tk in [M7] (Recrew, a SMC
# Temporary crew); the Russian passes, and the move ends. g1 moves from [P11] to [P10]; r1 in [P8] First Fires at it: dice 4, 4 (a Final 6, NMC), g1's
# NMC 3, 3 (it passes, and its 6 equals the Russian SAN). The Russian Sniper in [M8] rolls dr 2 (answer "attack" to the Russian side's choice), then the
# Random Location DR 1, 1 (one hex north, [M7]), where the CE crew is the one target: the tank is Stunned, and the leader's Wound Severity dr comes first:
# a 1 wounds him, a 5 eliminates him and leaves the tank Abandoned.
g = GameFile("p71-smc-crew-stun", "pass 71, 4f: a SMC crew's Stun wound takes its Wound Severity dr (D5.343, A17.11)", phasing="german")
g.san("russian", 6)
g.vehicle("de-tk", "attacker-tank", "german", "bd03:M7:0", "east", bu=False, abandoned=True)
g.unit("de-l", "leader", "attacker-leader-8-1", "german", "bd03:M7:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:P11:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.sniper("rs", "russian", "bd03:M8:0")
g.phases("rph", "pfph", "mph", side="german")
g.write()
