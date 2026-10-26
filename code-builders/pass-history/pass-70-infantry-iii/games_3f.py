"""The Studio games of increment 3f (Bypass and detection, task 70.8; ruling R70.23), on board 3.

Board 3's hexes as the oracle's hex facts read them (a scratch tool on the game's own LosCalculator and the Verified board, 2026-10-25): [F5] is Woods at
level 0 with all six hexsides Open Ground, so it may be Bypassed along any; [E6] is a Dirt Road hex at level 0, south-west of [F5]; [F6] a Dirt Road hex
at level 0, south of [F5]; [G6] Shellholes at level 0, south-east of [F5]; [F7] Open Ground at level 1, south of [F6]. Entering [F5] from [E6] along its
south hexside (Bypass "south", the [F5]-[F6] hexside), the stack leaves into [F6] or [G6] (A4.31). LOS read: [E6] to [F5] Clear at range 1; [F5] to
[F6] Clear at 1; [F7] to [F5] Clear at 2 (through [F6], across the south hexside, so it reaches the lane); [F8] to [F5] Blocked, so no firer stands there.
Check [F7] to [F5] once more on /boards/bd03's LOS tab before the play test.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

TO_MPH = ("rph", "pfph", "mph")

# A12.15 with A4.134 (ruling R70.23): the German MPh. The German 4-6-7 g1 in [E6] makes a Minimum Move (the move form's Minimum box) into [F5], where
# the Russian 4-4-7 r1 stands concealed. No dice. The proposal reads "g1 attempts bd03:F5:0 (woods) ..., a Minimum Move (A4.134); a concealed unit at
# [F5] is revealed and the stack stays in [E6] with the MF spent, its move ending (A12.15)"; once the DEFENDER passes (#propose-pass), g1's move is over
# and it is neither pinned nor CX. Read back: the "movement-step" with "to": "bd03:E6:0", "attempted": "bd03:F5:0", "minimumMove": true; then
# "concealment-lost" for r1.
g = GameFile("p70-minimum-move-forced-back", "pass 70, 3f: a Minimum Move into a concealed unit is forced back (A12.15)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F5:0", concealed=True)
g.phases(*TO_MPH)
g.write()

# A12.151 (ruling R70.23): the German MPh. g1 in [E6] Bypasses [F5] (#move-to bd03:F5:0, #move-bypass "south"); r1, concealed in the woods of [F5], keeps
# its "?". The DEFENDER passes. g1 then moves to [F6], where the Russian r2 stands concealed: r2 is revealed, g1 is forced back into its lane in [F5],
# and, a concealed enemy unit sharing that Location, r1 loses its "?" too. The proposal ends "; it stays in Bypass in bd03:F5:0, in its lane's terrain
# (A12.151), and a concealed enemy unit shares that Location, so every "?" there is lost". Then the Russian (DEFENDER) view: r1 fires TPBF at [F5]
# (#fire-from [F5], firer r1, #fire-target [F5]; any dice): the arithmetic shows FFNAM and FFMO and no woods TEM. Pass; g1's move is over. Read back:
# the "movement-step" with "to": "bd03:F5:0", "attempted": "bd03:F6:0", "bypass": ["south"]; "concealment-lost" for r2 and r1.
g = GameFile("p70-bypass-exit-forced-back", "pass 70, 3f: forced back from a Bypass exit into the lane (A12.151)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F5:0", concealed=True)
g.unit("r2", "squad", "defender-squad", "russian", "bd03:F6:0", concealed=True)
g.phases(*TO_MPH)
g.write()

# A12.151 with A12.15 (ruling R70.23): the German MPh. g1 in [E6] Bypasses [F5] along "south" past r1 (concealed) and r2 (hidden) in the woods. The
# DEFENDER passes. g1 then attempts to occupy the obstacle (#move-to bd03:F5:0, no Bypass): no dice; the proposal reads "g1 attempts to occupy the
# obstacle of bd03:F5:0 ...; concealed enemy units hold it, so the stack stays in Bypass in bd03:F5:0 with the MF spent, its move ending, and every "?"
# there is lost (A12.15, A12.151)". r1 and r2 are revealed, r2 placed on the map first (A12.15). Read back: the "movement-step" with "to" and
# "attempted" both "bd03:F5:0" and "bypass": ["south"]; "concealment-lost" for r1 and r2.
g = GameFile("p70-bypass-occupy-forced-back", "pass 70, 3f: an attempt to occupy a held obstacle from Bypass (A12.151)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F5:0", concealed=True)
g.unit("r2", "squad", "defender-squad", "russian", "bd03:F5:0", hidden=True)
g.phases(*TO_MPH)
g.write()

# A12.151, A4.33 (ruling R70.23): the German MPh. g1 in [E6] Bypasses [F5] along "south"; r1 is concealed in the woods; the Russian 4-4-7 r3 stands in
# [F7], two hexes off. In the DEFENDER's window r3 fires at [F5] with dice 5 4: 4 FP, 9 - 1 FFNAM - 1 FFMO = 7, a PTC; g1's PTC with dice 6 6 fails,
# pinned (the same arithmetic as the Play test). The DEFENDER passes: the proposal ends "; a broken or pinned member of the Bypassing stack ends its MPh
# in bd03:F5:0 with a concealed enemy unit, so every "?" there is lost (A12.151)", and r1 loses its "?". Read back: after "movement-window-closed",
# "concealment-lost" for r1.
g = GameFile("p70-bypass-pinned", "pass 70, 3f: a pinned Bypass member reveals the \"?\" of its Location (A12.151)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F5:0", concealed=True)
g.unit("r3", "squad", "defender-squad", "russian", "bd03:F7:0")
g.phases(*TO_MPH)
g.write()

# A12.42 (ruling R70.23): the German MPh. The German halftrack ght in [E6], facing east, Starts (#vehicle-unit, "start"), the DEFENDER passes, and it
# enters [F5] in VBM, straddling the [F5]-[F6] hexside (the step's button "enter bd03:F5:0 in VBM ..."), as pass 11's test from [E5] does; r1 (concealed)
# and r2 (hidden) stand in the woods of [F5]; r1 keeps its "?" as it enters. Pass, Stop (#vehicle-stop), pass, end the move (#propose-end-move), pass.
# No dice. The end of the move reads "; ght ends its MPh in Bypass of bd03:F5:0, so the concealed enemy units there lose their "?" (A12.42)": r1 is
# revealed, r2 stays hidden. Read back: "concealment-lost" for r1 only, after the "vehicle-step" (Remain) or the "movement-ended".
g = GameFile("p70-vbm-ends-in-bypass", "pass 70, 3f: a vehicle ending its MPh in Bypass reveals concealed units (A12.42)", phasing="german")
g.vehicle("ght", "attacker-halftrack", "german", "bd03:E6:0", "east")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F5:0", concealed=True)
g.unit("r2", "squad", "defender-squad", "russian", "bd03:F5:0", hidden=True)
g.phases(*TO_MPH)
g.write()
