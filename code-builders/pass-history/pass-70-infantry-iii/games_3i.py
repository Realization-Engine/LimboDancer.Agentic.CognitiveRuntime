"""The Studio games of increment 3i (Infantry against Infantry in a Location with a vehicle, its Ambush, PRC in CC, task 70.7 part I; rulings R70.30 to
R70.32), on board 3.

Board 3's hexes as the oracle's hex facts read them (board3_facts_3d.py, 2026-10-25): [F9] is Open Ground at level 0; [F8] is Open Ground on a hill at
base level 1, ADJACENT to [F9] (3h's withdrawal entered it as an advance would, up the slope). No LOS is read by these games: the advance, the PAATC, the
Ambush drs, and the CC read none, and the advancing Russians are not concealed, so no "?" is lost by sight.

A vehicle sharing a Location with an enemy unit at setup is not reviewed (ruling R25.3), so each game starts in the Russian APh with the Russians in [F9]:
tick them on the advance panel, #advance-to [F8], #propose-advance-units. A MMC advancing onto the manned, unconcealed German AFV passes a PAATC first
(A11.6): dice 3 4 for each (7 against Morale 7). Then end the phase (the CCPh begins).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402

TO_APH = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")


def aboard(game, identifier, definition, side, vehicle, role):
    """A squad aboard a vehicle as a Passenger or a Rider (D6.1, D6.2)."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:squad", "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": role}, "conditions": marks(CLEAR, {})}})


# A11.31 and the H3 example (p. 74; ruling R70.30): the Russian APh. The German PzKpfw IIIH gt (BU) and 4-6-7 g1 stand in [F8]; the Russian 4-4-7s r1 and
# r2 in [F9] advance onto them (PAATC dice 3 4, 3 4). In the CCPh the "Close Combat with vehicles" panel (.vehicle-cc[data-location='bd03:F8:0']) says
# "[F8]: the russian side attacks next". The CC panel's round for [F8] is refused: "play.cc-vehicle: gt is in bd03:F8:0, so its CC is sequential, one
# attack at a time, Infantry against Infantry too ...". On the vehicle CC panel:
#   1. Attacker r1 (.vehicle-cc-attacker), tick g1 (.vehicle-cc-defender, labelled "[F8]: ... 4-6-7 ..."), "Propose: the Infantry attack the checked enemy
#      Infantry (A11.31)" (.propose-vehicle-cc-infantry). Dice 6 6: 4 FP to 4, 1-1, no effect. The proposal reads "play.cc-vehicle: r1 attacks g1 in CC in
#      bd03:F8:0, one attack of the sequence with no simultaneous reply (A11.31)". Then "the german side attacks next".
#   2. The German: attacker g1, tick r1, the Infantry button. Dice 6 6. Next: russian.
#   3. The Russian: r2 with g1 ticked is refused, "play.cc-vehicle-defenders: Infantry attack enemy Infantry in bd03:F8:0 ... that no Infantry has attacked
#      this CCPh; an attack by a vehicle is counted apart (A11.12)". Attacker r2, vehicle gt, "Propose: Infantry attack the vehicle": dice 4 4 (8, +2 for
#      the escorting squad: no effect).
#   4. The German: vehicle gt, tick r1 and r2 (r1 was attacked by Infantry, which a vehicle's attack does not count), "Propose: the vehicle attacks the
#      checked Infantry": dice 5 6, no effect. The Location's CC is over.
# Read back: "close-combat-resolved" with "round": "sequential", "next": "german" (then "russian"), facts.round "sequential"; the last
# "vehicle-close-combat-resolved" with "closed": true. End the CCPh: the "phase-changed" to the German RPh carries "prcHeldInMelee": []. r1 and r2 are
# held in Melee by gt and g1.
g = GameFile("p70-vehicle-cc-h3", "pass 70, 3i: Infantry against Infantry beside a tank, one attack at a time (A11.31, the H3 example)", phasing="russian")
g.vehicle("gt", "attacker-tank", "german", "bd03:F8:0", "west")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F8:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()

# A11.4, A11.34, A11.51 (ruling R70.30): the Russian APh. As above, but the German 4-6-7 g1 is concealed, so an Ambush can occur in Open Ground (A11.4):
# advance r1 alone (PAATC 3 4) and end the phase. On the CC panel (#cc-location [F8]) the Ambush drs are due; propose them. Dice: Russian 1, German 4. The
# German dr's drm: -2 concealed, +2 vehicle, +1 BU AFV: 5. 1 is at least three lower: the Russians ambush, and g1 loses its "?". On the vehicle CC panel
# the Russian attacks first (A11.34): r1 attacks g1 (dice 6 6) with -1 for the Ambush; the record's attacks[0].drm holds "ambush". Next the German: gt
# attacks r1 (dice 5 6) with +1 against the ambusher (A11.51; the record's facts carry "ambusher": "russian", its defending[0].drm "vs-ambush"). Read
# back: "ambush-rolled" whose facts carry "vehicles": [{"side": "german", "buttonedOrStunnedAfvs": 1}] and whose resolution's German side lists the
# drm "concealed", "vehicle", "bu-or-stunned-afv".
g = GameFile("p70-vehicle-cc-ambush", "pass 70, 3i: an Ambush in a tank's Location takes the vehicle drm (A11.4, A11.34, A11.51)", phasing="russian")
g.vehicle("gt", "attacker-tank", "german", "bd03:F8:0", "west")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F8:0", concealed=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()

# A11.611, A11.62, A11.71 (ruling R70.31): the Russian APh, 1943. In [F8] the German PzKpfw IIIH gt, Immobilized, carries the 4-6-7 Rider gr; the German
# halftrack ght, CE, carries the 4-6-7 Passenger gp. Advance r1 from [F9] (one PAATC, 3 4) and end the phase. On the vehicle CC panel the Rider is offered
# "[F8]: ... 4-6-7 ..., Rider on ..." among the defenders:
#   1. r1 attacks gr (the Infantry button), dice 6 6: the record's attacks[0].defending[0].drm holds "vs-rider" -1 (A11.611). Next: german.
#   2. The halftrack attacks r1 (vehicle ght, tick r1, "Propose: the vehicle attacks the checked Infantry"): its 3 FP AAMG and gp's 4 FP, 7 to CCV 5;
#      the proposal adds "; gp adds its FP to the attack as Passengers (A11.62)", the record's facts carry "passengers" [gp]. Dice 6 6, no effect.
#   3. The German Rider gr attacks r1 (the Infantry button; r1 was attacked by a vehicle only): the record's attacks[0].drm holds "rider:gr" +1. Dice 6 6.
#   4. gt attacks r1 or the German side passes ("Propose: the german side passes").
# End the CCPh: the "phase-changed" carries "prcHeldInMelee": ["gr"] (the Immobile tank's Rider); gp, aboard the Mobile halftrack, is not held in Melee,
# r1 is (held by both vehicles). Unload or Bail Out into an enemy Location is shown by 3i's text only: the proposal line "...; bd03:F8:0 holds a Known
# enemy unit, so they are in an enemy Location and not held in Melee until the CCPh ends (D6.5)".
g = GameFile("p70-vehicle-cc-prc", "pass 70, 3i: a Rider attacked apart and a halftrack's Passengers in its attack (A11.611, A11.62, A11.71)", phasing="russian",
             year=1943)
g.vehicle("gt", "attacker-tank", "german", "bd03:F8:0", "west", immobilized=True)
aboard(g, "gr", "attacker-squad", "german", "gt", "rider")
g.vehicle("ght", "attacker-halftrack", "german", "bd03:F8:0", "west", bu=False)
aboard(g, "gp", "attacker-squad", "german", "ght", "passenger")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases(*TO_APH)
g.write()
