"""The Studio games of increment 3g (Searching and Mopping Up in any building, task 70.9; rulings R70.24, R70.25), on board 3.

Board 3's hexes as the oracle's hex facts read them (board3_facts_3d.py, 2026-10-25): [M5] and [L4] are one two-hex Wooden Building, 1 Level (M5's
north-west hexside and L4's south-east one are building terrain; both have stairs); [M4] is a Dirt Road hex north of [M5], [M6] Open Ground south of it,
[L5] Open Ground at level 1, [N4] and [N5] Dirt Road. No woods hex is ADJACENT to [M5] or [L4] on board 3, so the concealed Russian squad stands in the
road of [M4] instead of the woods the brief names. [M2], [N1], and [N2] are one Stone Building, 2 Level ([M2]'s north-east and south-east hexsides, [N1]'s
south and south-west ones, and [N2]'s north and north-west ones are building terrain); no card names it, so the game names it "bd03:M2", its board and
lowest hex. No LOS is read by these games: the searchers and the Mopping-Up squad are not concealed, so no "?" of theirs is at stake (A12.141).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

TO_MPH = ("rph", "pfph", "mph")
TO_PFPH = ("rph", "pfph")

# A12.152, A12.154 (ruling R70.24): the German MPh. The German 4-6-7 g1 and 8-1 l1 in [M6] move together into [M5] (2 MF, the building) and the Russian
# passes (#propose-pass). The Movement panel then shows the Search box (#search): tick [L4] first, then [M4] (input.search-hex, the order ticked is the
# order of preference, shown as (1), (2)), and #propose-search. The proposal reads "play.search: g1, l1 Search from bd03:M5:0 ... (-1 for 2
# HS-equivalents, -1 leadership) ...". Dice: the Search dr 3 (Final 1: one of the Accessible hexes other than [M5] unsearched, so both named are searched),
# then the Russian's Casualty dr 6 (-3 for the four concealed HS-equivalents: 3, no casualty). r2, hidden in [L4], is placed beneath "?" (the record is
# the Russian's alone; the German map shows a "?" in [L4]); r1, concealed in [M4], loses its "?". g1 and l1 are TI; the Russian may fire at the Search MF
# (A8.1), and once he passes their move ends. With a Casualty dr of 4 instead (4 - 3 = 1), one searcher, by a Random Selection dr each, is Reduced: g1 to
# a HS, or l1 wounded (its Wound Severity dr follows). Read back: "units-searched" with "units": ["g1", "l1"], "location": "bd03:M5:0", "roll", "drm": -2,
# "accessible", "searched": ["bd03:M5:0", "bd03:L4:0", "bd03:M4:0"]; the "dice-rolled" purposes "search" and "search-casualty"; "hidden-placed" for r2
# (visibility ["russian"]) and "concealment-lost" for r1.
g = GameFile("p70-search", "pass 70, 3g: a squad and leader Search as they end their move (A12.152)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:M6:0")
g.unit("l1", "leader", "attacker-leader-8-1", "german", "bd03:M6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:M4:0", concealed=True)
g.unit("r2", "squad", "defender-squad", "russian", "bd03:L4:0", hidden=True)
g.phases(*TO_MPH)
g.write()

# A12.153 (ruling R70.25): the German PFPh. The German 4-6-7 g1 in [M2] Mops Up the stone building [M2]-[N1]-[N2], which no card names: the Mopping Up
# box (#mop-up-building) offers "bd03:M2" (shown as [M2]); tick g1 (input.mop-up-unit) and #propose-mop-up. No dice. The broken Russian 4-4-7 r1 in [N2]
# surrenders to g1, and the German side Controls the building. Read back: "building-mopped-up" with "building": "bd03:M2", "side": "german", "secured"
# listing every Location of the three hexes, each level of each; then "instance-moved" and "instance-captured" for r1.
g = GameFile("p70-mop-up-any-building", "pass 70, 3g: Mopping Up a building no card names (A12.153)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:M2:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:N2:0", broken=True)
g.phases(*TO_PFPH)
g.write()

# A12.153, A12.154 (ruling R70.25): the German PFPh. g1 in [M2] Mops Up "bd03:M2" again, a hidden Russian 4-4-7 r2 on level 1 of [N1]: r2 is placed
# beneath "?", the building is not secured, and the Russian's Casualty dr (-1 for two HS-equivalents) is rolled at once: a dr of 2 Reduces g1 to a TI HS,
# a 3 does nothing. Read back: "hidden-placed" for r2, "building-mopped-up" with no "secured", the "dice-rolled" purpose "search-casualty".
g = GameFile("p70-mop-up-hidden", "pass 70, 3g: Mopping Up a building no card names finds a hidden squad (A12.153, A12.154)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:M2:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:N1:1", hidden=True)
g.phases(*TO_PFPH)
g.write()
