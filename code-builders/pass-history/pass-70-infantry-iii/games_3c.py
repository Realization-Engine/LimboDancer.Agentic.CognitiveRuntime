"""The Studio games of increment 3c (Voluntary Break and Voluntary Rout, task 70.1 part II; rulings R70.11, R70.12), on board 3.

Board 3's hexes as games_3b.py read them (code-builders/boards/hex_facts.py 03, 2026-10-25): [P8] Woods, [P9] and [P10] Open Ground, [O10] a Wooden
Building, [Q9] and [Q10] Dirt Road. The LOS pairs are 3a's Studio check: from [P8] the Russian sees both [O10] and [P10] (p70-inspect-seen), and 3b's
LOS read gives [P8] to [P10] at range 2. The Voluntary Rout starts in [Q10], as 3b's Interdiction game does: from [P10] the building [O10] is ADJACENT
and entered with no Open Ground, so no Interdiction could happen there. The catalog's German HS is the 2-4-7 (attacker-half-squad), not a 2-3-7.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

RTPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph")

# A10.41, A10.62, A7.83 (ruling R70.11). The German RtPh's start: the 4-6-7 g1 and the pinned 2-4-7 g2 in the wooden building [O10], in the LOS of the
# Russian 4-4-7 in the woods of [P8] (range 2, its Normal Range 4). The Rout panel's "Voluntary Break" list offers both under [O10]; tick g2 and
# "Propose: Voluntary Break": broken, under DM, its pin gone (no dice). It must then rout. Read back: "voluntary-break" (units ["g2"]), then
# "conditions-changed" for g2 (asl:broken true, asl:dm true, asl:pinned false). After any rout step the list is gone and the action refused.
g = GameFile("p70-voluntary-break", "pass 70, 3c: a pinned HS breaks voluntarily to rout (A10.41)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:O10:0")
g.unit("g2", "half-squad", "attacker-half-squad", "german", "bd03:O10:0", pinned=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases(*RTPH)
g.write()

# A10.2 (ruling R70.11). The German RtPh's start: the 9-1 gl breaks voluntarily in [O10]; the 4-6-7 g1 with him, of lower Morale Level, takes a LLTC
# with his -1 reversed. Dice 5, 4: 9 + 1 = 10 against 7, failed, so g1 is pinned; dice 2, 3: 6, passed. Read back: "voluntary-break" (["gl"]), its
# conditions, "dice-rolled", "morale-check-resolved" (cause "voluntary-break", facts.brokenLeaders[0].unitId "gl", resolution.effects[0].checks[0].kind
# "LLTC"), then g1's "conditions-changed" (asl:pinned true) on a failure.
g = GameFile("p70-voluntary-break-leader", "pass 70, 3c: a leader's Voluntary Break calls for a LLTC (A10.2, A10.41)", phasing="german")
g.unit("gl", "leader", "attacker-leader-9-1", "german", "bd03:O10:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:O10:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases(*RTPH)
g.write()

# A10.711, A10.2 (rulings R70.12, R70.3). German RtPh: the broken 4-6-7 g1 with the unbroken 9-1 gl in [Q10], in the Open Ground in the LOS of the
# Russian in [P8]. Choose g1 to rout, the leader gl in "Leader routing with it", and the route [P10], [O10]. The Interdiction NMC in [P10]: dice 3, 4
# is 7 - 1 = 6 against 7, passed (without gl it would pin), and both end in [O10]. Dice 5, 4 is 8: g1 is Reduced to a broken 2-4-7 and gl eliminated;
# the HS takes a LLMC with his -1 reversed: 2, 2 is 5 against 6, passed, and it routs on to [O10]; 4, 3 is 8, failed, and the HS is eliminated.
# Read back: "rout-stepped" for gl with "with": "g1" after each of g1's; "rout-interdicted" with "drm": -1 and "leader": "gl"; on a failure
# "instance-eliminated" gl and "morale-check-resolved" (cause "interdiction", facts.lostLeaders[0].unitId "gl").
g = GameFile("p70-voluntary-rout", "pass 70, 3c: a leader routs with a broken squad (A10.711)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:Q10:0", broken=True)
g.unit("gl", "leader", "attacker-leader-9-1", "german", "bd03:Q10:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases(*RTPH)
g.write()
