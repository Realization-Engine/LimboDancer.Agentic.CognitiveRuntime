"""The Studio games of increment 3j (Street Fighting, A11.52's Passengers, A18.12 against a vehicle, D5.42's entry at a capture, task 70.7 part II;
rulings R70.33 and R70.34), on board 3.

Board 3's hexes as the oracle's hex facts read them (board3_street_3j.py, 2026-10-25): [N5] is a Dirt Road hex whose road crosses two of its hexsides,
with the building hex [M5] on one side of the road and [O5] on the other, so it is open to Street Fighting (A11.8); [F9] is Open Ground at level 0 and
[F8] Open Ground on a hill ADJACENT to it (3i's games). No LOS is read by these games: the advance, the PAATC, and the CC read none, and no unit is
concealed.

Each game starts in the Russian APh: tick the advancing unit on the advance panel, #advance-to the hex named, #propose-advance-units; a MMC advancing onto a
manned German AFV passes a PAATC first (A11.6): dice 3 4. Then end the phase (the CCPh begins).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402

TO_APH = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")

# A11.8 (ruling R70.33): the German PzKpfw IIIH gt (BU) stands in the road hex [N5]; the Russian 4-4-7 r1 in the building [M5] advances onto it (PAATC
# 3 4). In the CCPh, on the vehicle CC panel (.vehicle-cc[data-location='bd03:N5:0']):
#   1. Attacker r1, vehicle gt, "Propose: Infantry attack the vehicle": the proposal adds "; the attackers advanced from an ADJACENT building, so Street
#      Fighting gives their attack on the vehicle -1 (A11.8)". Dice 5 5: 10 - 1 = 9 against CCV 5, no effect.
#   2. The German: vehicle gt, tick r1, "Propose: the vehicle attacks the checked Infantry": "; the units that advanced from an ADJACENT building qualify
#      for Street Fighting, so the vehicle's attack on them takes +1 (A11.8)". Dice 5 6, no effect. The Location's CC is over.
#   3. The "Street Fighting" section offers "Propose: [N5]: 4-4-7 squad r1 returns to [M5] (A11.8)" (.street-fighting-return[data-unit='r1']); commit it,
#      and r1 is back in [M5]. Staying needs no step. A pinned r1 would be offered nothing (the I5 example).
# Read back: the two "vehicle-close-combat-resolved" records' facts carry "streetFighters": ["r1"], the first resolution's drm "street-fighting" -1, the
# second's defending[0].drm "vs-street-fighting" +1; then an "instance-moved" of r1 to bd03:M5:0.
g = GameFile("p70-street-fighting", "pass 70, 3j: a tank in a road hex between buildings fought by Street Fighting (A11.8)", phasing="russian")
g.vehicle("gt", "attacker-tank", "german", "bd03:N5:0", "west")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:M5:0")
g.phases(*TO_APH)
g.write()

# A11.52 (ruling R70.34): the German truck gtr, stopped in [F8], carries the 4-6-7 Passenger gp. The Russian 4-4-7 r1 in [F9] advances into [F8] (no
# PAATC against an unarmored vehicle); the truck is not captured as the CCPh begins, since it is not alone with r1. On the vehicle CC panel gp is offered
# "[F8]: 4-6-7 squad gp, Passenger in ..." among the defenders; "Propose: the Infantry attack the checked enemy Infantry (A11.31)" stays disabled for it,
# and "Propose: the Infantry attempt to capture the checked Passengers (A11.52)" (.propose-vehicle-cc-capture-passengers) takes attacker r1 and gp ticked.
# The proposal adds "; an attempt to capture the Passengers of an unarmored stationary vehicle, at -2 with the capture's +1 ...". Dice 2 2: 4 - 1 = 3, below
# the Kill Number: gp is captured, leaves the truck for [F8], and the truck is captured with it.
# Read back: "close-combat-resolved" with "round": "sequential" and "captors": ["r1"], its attacks[0].defending[0].drm "capture" +1 and
# "vs-passenger-capture" -2; facts.units name gp with "passengerOf": "gtr"; then "instance-captured" of gp, "instance-moved" of gp to bd03:F8:0, and
# "vehicle-captured" of gtr, "side": "russian", "cause": "unarmed".
g = GameFile("p70-capture-passengers", "pass 70, 3j: a truck's Passenger captured in CC, and the truck with it (A11.52)", phasing="russian")
g.vehicle("gtr", "attacker-truck", "german", "bd03:F8:0", "west")
g.add("instance-created", {"instance": {"id": "gp", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                        "position": {"in": "gtr", "role": "passenger"}, "conditions": marks(CLEAR, {})}})
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()

# A18.12 (ruling R70.34): the Russian 4-4-7 r1 in [F9] advances onto the German PzKpfw IIIH gt in [F8] (PAATC 3 4). On the vehicle CC panel: attacker r1,
# vehicle gt, "Propose: Infantry attack the vehicle". Dice 1 1, then the Leader Creation dr 5: +1 Russian, Final 6, a 6+1, who adds his +1 to the DR and
# one to the CCV: Kill Number 6, Final DR 3, a burning wreck. The record line reads "...; the Original 2 draws a Leader Creation dr 5, Final 6: a
# defender-leader-6-plus-1 is created and joins the attack (A18.12)", and the leader stands in [F8].
# Read back: "vehicle-close-combat-resolved" with facts "leaderCreation": true, resolution "leaderCreation" {"dr": 5, "finalDr": 6, "leaderDefinitionId":
# "defender-leader-6-plus-1"}, killNumberModifiers "created-leader-combining:defender-leader-6-plus-1", drm "created-leader:defender-leader-6-plus-1" +1,
# and "createdLeader" naming the leader whose "instance-created" follows.
g = GameFile("p70-leader-creation-vehicle", "pass 70, 3j: an Original 2 against a tank creates a leader (A18.12)", phasing="russian")
g.vehicle("gt", "attacker-tank", "german", "bd03:F8:0", "west")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()

# D5.42's EXC (ruling R70.34): the German PzKpfw IIIH gt in [F8] is Abandoned; the Russian HS rh in [F9] advances into its Location. In the CCPh: attacker
# rh, vehicle gt, "Propose: Infantry attempt to capture the Abandoned AFV (A21.2)" (.propose-vehicle-cc-capture). Dice 1 2: 3 - 3 against CCV 3, captured.
# The "Vehicle crews" section then offers "Enter ..." gt with rh (.recrew-unit[data-vehicle='gt']) and "Propose: re-crew (D5.42)" in this CCPh, rh's MF
# spent by its advance: rh crews gt as a Temporary crew, a HS (A21.22).
# Read back: "vehicle-captured" of gt with "cause": "cc", then "vehicle-recrewed" with "unit": "rh", "manning": "half-squad", "captured": true.
g = GameFile("p70-recrew-captured", "pass 70, 3j: a captor enters the AFV it captured in that CCPh (D5.42)", phasing="russian")
g.vehicle("gt", "attacker-tank", "german", "bd03:F8:0", "west", abandoned=True)
g.unit("rh", "half-squad", "defender-half-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()
