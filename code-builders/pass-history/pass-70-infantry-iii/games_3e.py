"""The Studio games of increment 3e (the owner's and the viewer's choices on a "?", and concealment for vehicles and an Emplaced Gun, tasks 70.4 and 70.5;
rulings R70.20 to R70.22), on board 3.

Board 3's hexes as board3_facts_3d.py and board3_terrain_3e.py read them (the oracle's hex facts): [F5], [C6], [C7], [D6] Woods at level 0; [E6] Dirt Road at
level 0; [G7] Open Ground at level 1; [K9] Grain at level 0; [L8] Open Ground at level 0. Board 3 is hilly, so check each LOS named below on /boards/bd03's
LOS tab before the play test: [F5] to [E6] (adjacent, so clear unless a hexside blocks), and [L8] to [G7].
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CREWS, GameFile  # noqa: E402

TO_PFPH = ("rph", "pfph")
TO_MPH = ("rph", "pfph", "mph")
TO_CCPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph")

# A12.14 (ruling R70.20). The German PFPh: his concealed 4-6-7 g1 in the woods of [F5], his Dummy gd in [C6], and his hidden 4-6-7 gh in [C7]; the
# Russian 4-4-7 r1 far off in [L8]. The German view's "Give up ?" list (#play-remove-concealment, inputs .remove-concealment-unit) offers the three by hex;
# tick them and press "Propose: give up ?" (#propose-remove-concealment), then confirm. Read back: "concealment-lost" for g1 and gh, "instance-eliminated"
# for gd, each with "visibility": "all"; the Russian view now shows g1 and gh. The Russian view offers no such list.
g = GameFile("p70-remove-concealment", "pass 70, 3e: the owner gives up a \"?\" (A12.14)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F5:0", concealed=True)
g.unit("gh", "squad", "attacker-squad", "german", "bd03:C7:0", hidden=True)
g.add("instance-created", {"instance": {"id": "gd", "kind": "asl:dummy", "side": "german", "position": {"at": "bd03:C6:0"},
                                        "conditions": {"asl:concealed": True, "asl:hidden": False}}})
g.unit("r1", "squad", "defender-squad", "russian", "bd03:L8:0")
g.phases(*TO_PFPH)
g.write()

# A12.14 (ruling R70.21). The Russian MPh: his concealed 4-4-7 r1 in the woods of [D6] moves to [E6] (a road hex, Open Ground) without Assault Movement, in
# the LOS of the German 4-6-7 g1 in [F5], itself concealed and the only German unit. The step commits for the Russian with no word of a choice; his view
# shows the DEFENDER's window only. Hand over to the German: his view shows the choice (#play-choice) with "Propose: force the loss of its ?" and "Propose:
# decline: it keeps its ?". Forced: r1 loses its "?", and the Russian's records read "g1 in [F5] shows itself for a moment ..." (the event
# "concealment-verified", "visibility": ["russian"]); the German's do not. Declined: r1 keeps its "?", and the Russian's records hold nothing of it
# ("choice-pending" and "choice-made" are "visibility": ["german"]). Play it once each way (copy the game).
g = GameFile("p70-viewer-option", "pass 70, 3e: the viewer declines to force a \"?\" loss (A12.14)", phasing="russian")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:D6:0", concealed=True)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F5:0", concealed=True)
g.phases(*TO_MPH)
g.write()

# The Concealment Table, Cases J and K (ruling R70.22). The German CCPh, no Russian unit on the map: the German PzKpfw IIIH gt1 in the grain of [K9] (in
# season, July: a vehicle's Concealment Terrain, Case J) and gt2 in the Open Ground of [L8] (no enemy within 16, Case K with no dr). End the CCPh: both
# gain "?" with no dr. Read back: two "concealment-gained" events, for gt1 and gt2.
g = GameFile("p70-vehicle-gain", "pass 70, 3e: vehicles gain \"?\" (Cases J and K)", phasing="german")
g.vehicle("gt1", "attacker-tank", "german", "bd03:K9:0", "east")
g.vehicle("gt2", "attacker-tank", "german", "bd03:L8:0", "east")
g.phases(*TO_CCPH)
g.write()

# A12.34 (ruling R70.22). The Russian RPh: the German infantry Gun gg and its crew gg-crew hidden in the Open Ground of [G7], out of Concealment Terrain;
# the Russian 4-4-7 r1 in [L8]. End the RPh: if [L8] sees [G7], both are placed beneath "?" as the PFPh begins ("hidden-placed", "visibility":
# ["german"]); the Russian view shows a "?" in [G7] and no record line of it. If [L8] has no LOS to [G7], move r1 toward [G7] in the MPh until it sees it:
# the step places them beneath "?".
g = GameFile("p70-hidden-gun", "pass 70, 3e: a hidden Gun out of Concealment Terrain is found (A12.34)", phasing="russian")
g.unit("gg-crew", "crew", CREWS["german"], "german", "bd03:G7:0", hidden=True)
g.add("instance-created", {"instance": {"id": "gg", "kind": "asl:gun", "definition": "attacker-inf-gun", "side": "german",
                                        "position": {"at": "bd03:G7:0", "facing": "east"}, "holding": {"holder": "gg-crew", "role": "manned"},
                                        "conditions": {"asl:malfunctioned": False, "asl:hidden": True}}})
g.unit("r1", "squad", "defender-squad", "russian", "bd03:L8:0")
g.phases("rph")
g.write()
