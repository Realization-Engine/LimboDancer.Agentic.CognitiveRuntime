"""The Studio games of increment 3b (the morale check outside fire, task 70.1 part I; rulings R70.3 to R70.10), on board 3.

Board 3's hexes were read from the oracle's hex facts (code-builders/boards/hex_facts.py 03) on 2026-10-25: [P8] Woods, [P9] and [P10] Open Ground,
[O10] a Wooden Building, [Q9] and [Q10] Dirt Road; row 10 is the board's last. 3a's LOS read: [P8] sees [P10] at range 2. The brief's [P10] start
was moved to [Q10]: from [P10] the nearest building, [O10], is ADJACENT and entered with no Open Ground, so no Interdiction could happen there.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

RTPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph")

# A10.53, A15.1 (rulings R70.4, R70.5). German RtPh: the broken 4-6-7 in [Q10] must rout for the wooden building [O10] through [P10], Open Ground in the
# LOS and Normal Range of the Russian 4-4-7 in the woods of [P8]. Rout g1 with the route [P10], [O10]: the Interdiction NMC on 1, 1 (an Original 2)
# passes and calls for Heat of Battle (+1 broken). A Heat of Battle DR of 4, 4 is a Final 9, Berserk: g1 rallies and its rout ends in [P10]. A DR of
# 2, 2 is a Final 5: a hero and Battle Hardening, g1 unbroken in [P10]. Read back: "rout-interdicted" then "morale-check-resolved" (cause
# "interdiction", location "bd03:P10:0", resolution.effects[0].heatOfBattle).
g = GameFile("p70-interdiction-hob", "pass 70, 3b: an Interdiction NMC's Original 2 calls for Heat of Battle (A10.53, A15.1)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:Q10:0", broken=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases(*RTPH)
g.write()

# A10.2, A10.5 (ruling R70.3). German RtPh: the broken 8-1 in [Q10] with the unbroken 4-6-7 g2, in the Open Ground in the LOS and Normal Range of the
# Russian in [P8]. Do not rout gl; end the phase: gl is eliminated for Failure to Rout, and the proposal says a LLMC is rolled now. g2's LLMC on 4, 3
# is 7 + 1 (his -1 reversed) = 8 against 7: broken, within the German ELR of 3; on 2, 3 it passes (6). Read back: "morale-check-resolved" (cause
# "failure-to-rout", facts.lostLeaders[0].unitId "gl", resolution.effects[0].checks[0].kind "LLMC") before the "phase-changed".
g = GameFile("p70-llmc-failure-to-rout", "pass 70, 3b: a leader eliminated for Failure to Rout causes a LLMC (A10.2)", phasing="german")
g.unit("gl", "leader", "attacker-leader-8-1", "german", "bd03:Q10:0", broken=True)
g.unit("g2", "squad", "attacker-squad", "german", "bd03:Q10:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.phases(*RTPH)
g.write()
