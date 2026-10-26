"""The Studio games of increment 4d (the Sniper player's choices, A14.2, A14.21, A12.14; ruling R71.12), on board 3.

The firing pair is 4a's and 4b's, so its one LOS holds: the Russian 4-4-7 in [P8] (woods) fires in the Russian PFPh at the German 4-6-7 in [P10] (Open
Ground); pass 70's read of board 3 (LosCalculator, 2026-10-25) gave [P8] a LOS to [P10] at range 2. No other LOS pair is relied on: the Sniper needs none
(A14.3: no LOS, no modification). The German SAN is 2, so the Russian IFT DR of 1 + 1 calls the German Sniper in [M7] (A14.1). Its Random Location DR's
colored dr 1 is north along column M: a white 2 finds [M5], a white 3 finds [M4]. [M5] and [L4] are one two-hex wooden building with a level 1 (the
session's word); [M4], north of [M5] and beside [L4] (the same row number in the next column is always adjacent), is not of the building. Before the
games are used, check in the Studio's board viewer that [M4] is one hex from both [M5] and [L4] and that neither holds anything that lowers its TEM.

The dice, in order, entered in the Studio's dice box, start the same in each game: the IFT DR 1, 1 (the doubles cower it to the 2 FP column: K/1 on g1);
g1's MC 1, 2 (if the IFT calls none, the next die is the Sniper's); then the Sniper dr. An effective dr first stops at the German side's choice
(#play-choice: "Propose: attack: make the Random Location DR (A14.2)" and "Propose: forfeit the attack and reposition the Sniper counter (A14.2)"; the
Russian view reads the choice, with the dr, and has no button). Answer "attack" and enter the Random Location DR given below.

Read back in each game file: "choice-pending" with "kind": "sniper", "side": "german", a "resume" whose "step" is "forfeit", "reposition", "hex",
"location", "target", or "first", and "choice-made" with the answer. The Sniper's choice names no Russian unit under a "?".

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


def start(game, label):
    """The common part: g1 and r1, the German SAN of 2, and the German Sniper in [M7]; the Russian PFPh."""
    g = GameFile(game, label, phasing="russian")
    g.san("german", 2)
    g.unit("g1", "squad", "attacker-squad", "german", "bd03:P10:0")
    g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
    g.sniper("gs", "german", "bd03:M7:0")
    return g


# (a) Equidistant hexes of equal TEM (A14.21): Russian squads in [M5] and [L4], both of the wooden building. Dice: Sniper dr 1; "attack"; Random Location
# 1, 3, onto the empty [M4]. The game waits on the German side's choice of the hex (step "hex", options "bd03:L4:0" and "bd03:M5:0", read "[L4]" and "[M5]";
# the choice reads "the german Sniper's Random Location finds no target: of the equidistant hexes with the lowest TEM, the german side chooses which to
# attack (A14.21)"). While it waits, the Russian view's "Remove "?" voluntarily" is refused ("play.choice-pending: the german side answers first"), A12.14's
# exception. Answer "[L4]": its squad r-a takes the dr 1 and breaks; "sniper-attacked" reads "target": "bd03:L4:0", "unit": "r-a", "result": "broken".
g = start("p71-sniper-equidistant", "pass 71, 4d: the Sniper player chooses between equidistant hexes of equal TEM (A14.21)")
g.unit("r-a", "squad", "defender-squad", "russian", "bd03:L4:0")
g.unit("r-b", "squad", "defender-squad", "russian", "bd03:M5:0", concealed=True)
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A hex of two Locations (A14.2): Russian squads in [M5] at ground level and at level 1. Dice: Sniper dr 1; "attack"; Random Location 1, 2, onto
# [M5]. The game waits on the German side's choice of the Location (step "location", options "bd03:M5:0" and "bd03:M5:1", read "[M5]" and "[M5], level 1").
# Answer level 1: r-up takes the dr 1 and breaks, r-down is untouched; the Sniper counter moves to "bd03:M5:1".
g = start("p71-sniper-location", "pass 71, 4d: the Sniper player chooses the Location of a building hex of two levels (A14.2)")
g.unit("r-down", "squad", "defender-squad", "russian", "bd03:M5:0")
g.unit("r-up", "squad", "defender-squad", "russian", "bd03:M5:1")
g.phases("rph", "pfph", side="russian")
g.write()

# (c) Random Selection ties (A14.2): the Russian 2-3-7 r-h, 8-1 r-l, and 4-4-7 r-s in [M5] at ground level, candidates in id order. Dice: Sniper dr 2;
# "attack"; Random Location 1, 2, onto [M5]; Random Selection 6, 6, 1, which ties r-h and r-l. The game waits on the German side's choice (step "first",
# options "unit:r-h" and "unit:r-l", read "[M5]: ..." with each unit's name). Answer the 8-1: enter 3 (r-h's own new Sniper dr: no effect), then 1 (the
# 8-1's Wound Severity dr): the 8-1 is wounded by the dr 2, two "sniper-attacked" records follow (r-l "wounded" with the first dr's roll, then dr 3
# "none"), and the Russian side may then take its Sniper Check (4b) with r-s.
g = start("p71-sniper-tied", "pass 71, 4d: the Sniper player chooses which tied target takes the dr (A14.2)")
g.unit("r-h", "half-squad", "defender-half-squad", "russian", "bd03:M5:0")
g.unit("r-l", "leader", "defender-leader-8-1", "russian", "bd03:M5:0")
g.unit("r-s", "squad", "defender-squad", "russian", "bd03:M5:0")
g.phases("rph", "pfph", side="russian")
g.write()
