"""The Studio games of increment 3d (rout from a building, Interdiction by vehicles and Guns, Fanaticism by SSR, task 70.2; rulings R70.13 to R70.15), on board 3.

Board 3's hexes as board3_facts_3d.py reads them (the oracle's hex facts, 2026-10-25): [B9], [B10], [C10], [D10], [E10], [F10] Open Ground at level 0; [C9],
[D8], [E9] Woods; [M5] and [L4] each a "Wooden Building, 1 Level" with a stairway, joined across [M5]'s hexside 5 and [L4]'s hexside 2 (both of building
terrain), so one two-hex building. The LOS from [F10] along row 10 to [B10] crosses only Open Ground; check it on /boards/bd03's LOS tab before the play test.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

RTPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph")

# A10.5, A10.532 (ruling R70.14). The German RtPh: the broken 4-6-7 g1 in the Open Ground of [B10], the Russian T-34 M41 rt in [F10] with its VCA facing
# west, four hexes off. g1 must rout (the Rout panel says it is in Open Ground in the LOS and Normal Range of the T-34). Route [C10], [C9]: in [C10] the
# T-34's BMG (Normal Range 8, through the VCA) Interdicts: dice 3, 3 is 6 against 7, passed, and g1 ends in the woods of [C9]. The preview names the
# Interdictor as the vehicle in [F10]. Read back: "rout-interdicted" with "interdictor": "rt". Faced east instead (edit the facing), the T-34 Interdicts
# nothing (its BMG would change VCA, and its RST turret fires no CMG while CE, nor its MA), yet g1 must still rout.
g = GameFile("p70-afv-interdiction", "pass 70, 3d: a T-34 Interdicts a rout (A10.532)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B10:0", broken=True)
g.vehicle("rt", "defender-tank", "russian", "bd03:F10:0", "west")
g.phases(*RTPH)
g.write()

# A10.51's EXC (ruling R70.13). The German RtPh: the broken 4-6-7 g1 on level 1 of [M5], the Russian 4-4-7 r1 below it on [M5]'s ground level, ADJACENT by
# the stairway, so g1 must rout. Before the pass the nearest cover, level 1 of [L4], bound it; now [L4] is of its own building, which it need not
# consider: the Rout panel's destinations leave out [L4] (or offer none, and any legal route), and a route by [L4] level 1, [L4], and on out of the
# building is not refused for leaving cover. Read back: the "rout-stepped" events of g1, the first into "bd03:L4:1".
g = GameFile("p70-rout-own-building", "pass 70, 3d: a broken squad routs out of its own building (A10.51 EXC)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:M5:1", broken=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:M5:0")
g.phases(*RTPH)
g.write()

# A10.8 (ruling R70.15). A game holding only its start, with the SSR fanatic:russian: the condition is set by the planner's setup (asl.game.setup with
# placements), not by this file, so a unit this script wrote would not carry it. Each Russian Personnel unit a setup then places is written with
# "asl:fanatic": true in its "instance-created" record, the German ones not. If the Play page offers no setup for a game with no units, the Play test
# BacklogPass70Tests.AFanaticSsrMakesTheSidesPersonnelFanaticAsTheySetUp stands for this check.
g = GameFile("p70-fanatic-ssr", "pass 70, 3d: Fanaticism by SSR (A10.8)", phasing="german", special=["fanatic:russian"])
g.write()
