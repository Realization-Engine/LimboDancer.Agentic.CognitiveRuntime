"""The Studio games of fix Round B of pass 70 (morale, rout, and Searching; rulings R70.42 to R70.47), on board 3.

Board 3's hexes as board3_facts_3d.py reads them (the oracle's hex facts, 2026-10-25): [B10], [C10], [D10], [E10], [F10] Open Ground at level 0. The LOS
along row 10 crosses only Open Ground (games_3d.py, p70-afv-interdiction); check [C10] to [E10] and [C10] to [F10] on /boards/bd03's LOS tab before the
play test. The rout game of the round is games_3d.py's p70-rout-own-building, whose comment says what its panel now reads.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

RTPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph")

# A10.41 (ruling R70.43). The German RtPh: the 2-3-7 HS gh in [C10], the Russian Unarmed 4-4-7 ru in [D10], ADJACENT. The Voluntary Break of gh is allowed
# ("ADJACENT to any unbroken enemy ground unit"). Read back: "voluntary-break" with "units": ["gh"].
g = GameFile("p70-break-unarmed-adjacent", "pass 70, fixes B: an Unarmed enemy ADJACENT allows a Voluntary Break (A10.41)", phasing="german")
g.unit("gh", "half-squad", "attacker-half-squad", "german", "bd03:C10:0")
g.unit("ru", "squad", "defender-squad", "russian", "bd03:D10:0", unarmed=True)
g.phases(*RTPH)
g.write()

# The same with the Unarmed 4-4-7 in [E10], two hexes off in the LOS (to be checked): the break is refused, the refusal ending "a concealed or hidden enemy
# unit does not count (ruling R70.43)", since the LOS clause asks for an armed enemy.
g = GameFile("p70-break-unarmed-far", "pass 70, fixes B: an Unarmed enemy in LOS allows no Voluntary Break (A10.41)", phasing="german")
g.unit("gh", "half-squad", "attacker-half-squad", "german", "bd03:C10:0")
g.unit("ru", "squad", "defender-squad", "russian", "bd03:E10:0", unarmed=True)
g.phases(*RTPH)
g.write()

# A20.54 (ruling R70.47). The German RtPh: the broken Russian 8-1 rl and the Unarmed 4-4-7 r2 in the Open Ground of [C10], the German 4-6-7 g1 in [F10],
# three hexes off in the LOS (to be checked), so rl, in its LOS and Normal Range, is eliminated for Failure to Rout when the RtPh is ended with rl left
# unrouted (Advance on the Play page). r2 takes its LLMC:
# dice 4, 3 with the +1 reversed is 8 against 7, failed: r2 is Casualty Reduced to a HS that is not broken. Read back: "morale-check-resolved" with
# "cause": "failure-to-rout", its resolution's consequence "casualty-reduced", then the lineage of r2.
g = GameFile("p70-unarmed-llmc", "pass 70, fixes B: an Unarmed squad that fails its LLMC is Casualty Reduced (A20.54)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F10:0")
g.unit("rl", "leader", "defender-leader-8-1", "russian", "bd03:C10:0", broken=True)
g.unit("r2", "squad", "defender-squad", "russian", "bd03:C10:0", unarmed=True)
g.phases(*RTPH)
g.write()
