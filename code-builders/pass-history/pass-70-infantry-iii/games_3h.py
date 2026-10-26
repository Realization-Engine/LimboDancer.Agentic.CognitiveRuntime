"""The Studio games of increment 3h (Withdrawal completed, CC with a Gun's crew, a Carrier HS, task 70.6; rulings R70.26 to R70.28), on board 3.

Board 3's hexes as the oracle's hex facts read them (board3_facts_3d.py, 2026-10-25): [F9], [F10], [D9], [D10], [C10] are Open Ground at level 0; [F8]
is Open Ground on a hill at base level 1 (a withdrawal enters it as an advance would, up the slope); [E9] is Woods. No LOS is read by these games: the
withdrawal's reveal is decided by what the Location holds, not by who sees it, and the CC and the Recombination read none.

The projector frees a unit placed in Melee while no enemy unit is yet in its Location (KeepMelee), so in each Melee the Russian is placed first and is
not held in Melee; the German placed beside it is. The Russian may still attack in the round.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

TO_CCPH = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph")
TO_APH = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")

# A11.21 (ruling R70.26): the German CCPh. The German 4-6-7 g1 is held in Melee in [F9] with the Russian 4-4-7 r1; a Russian 2-4-7 HS r2 is concealed in
# [F8], a Known Russian 4-4-7 r3 stands in [F10]. On the CC panel (#cc-location [F9]) g1's withdrawal list (select.cc-withdraw[data-unit='g1']) offers
# [F8] (a "?" does not bar it) and not [F10] (a Known enemy unit does). Choose "withdraws to [F8]", #declare-cc, hand over (#play-handover-confirm);
# the Russian may declare r1's attack on g1 (.cc-attacker r1, .cc-defender g1, #cc-add-attack) or none, then #propose-cc. Dice: r1's attack 6 6 (no
# effect; g1, withdrawing, is attacked with -2). g1 survives the round, enters [F8], and is eliminated; r2, the only concealed unit there, loses its
# "?" with no dr (a Random Selection of one). The CC record reads "g1 withdraws to [F8], which holds a concealed or hidden enemy unit, and is eliminated
# (A11.21)". Read back: "close-combat-resolved" (facts.units[g1].withdrawingTo "bd03:F8:0"), then "instance-eliminated" g1 and "concealment-lost" r2,
# visibility "all".
g = GameFile("p70-withdraw-concealed", "pass 70, 3h: a withdrawal into a concealed enemy's Location (A11.21)", phasing="german")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0", melee=True)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F9:0", melee=True)
g.unit("r2", "half-squad", "defender-half-squad", "russian", "bd03:F8:0", concealed=True)
g.unit("r3", "squad", "defender-squad", "russian", "bd03:F10:0")
g.phases(*TO_CCPH)
g.write()

# A11.21, A4.43 (ruling R70.26): the German CCPh. g1 in Melee in [F9] carries an MMG (4PP) and an LMG (1PP), above its IPC of three; a Russian Dummy is
# alone in [F10]. The panel shows "above its IPC, it drops (A11.21):" with a box for each SW (input.cc-drop[data-weapon='gm'], [data-weapon='gl']). Tick
# the MMG, choose "withdraws to [F10]", declare, hand over, and the Russian passes or attacks (dice 6 6). The proposal reads "play.cc-withdrawal-drop: gm
# is dropped, unpossessed in the Location, if its unit withdraws (A11.21, A4.43; ruling R70.26)". Without the tick the proposal is refused:
# "play.cc-withdrawal-portage: g1 carries more than its IPC, so it withdraws only by dropping SW down to its IPC ...". Read back: "instance-eliminated"
# rd (the Dummy, before the move), "equipment-transferred" gm to [F9] with no holder, "instance-moved" g1 to [F10]; gl stays g1's.
g = GameFile("p70-withdraw-drop", "pass 70, 3h: a withdrawal above the IPC drops a SW, and a Dummy is removed (A11.21)", phasing="german")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0", melee=True)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F9:0", melee=True)
g.weapon("gm", "attacker-mmg", "german", "g1")
g.weapon("gl", "attacker-lmg", "german", "g1")
g.add("instance-created", {"instance": {"id": "rd", "kind": "asl:dummy", "side": "russian", "position": {"at": "bd03:F10:0"},
                                        "conditions": {"asl:concealed": True, "asl:hidden": False}}})
g.phases(*TO_CCPH)
g.write()

# A11.21's p. 74 example (ruling R70.26): the German CCPh. As the first game, but [F8] holds a hidden Russian 45mm AT Gun with its crew (the Gun and
# crew hidden, "asl:hidden"). The German's list offers [F8] and says nothing of them. Withdraw g1 there (the Russian passes, or attacks with dice 6 6):
# g1 is eliminated, and the crew rc and the Gun rag are placed on board unconcealed, with no dr. Read back: "instance-eliminated" g1, "concealment-lost"
# rc and rag ("asl:concealed" and "asl:hidden" both false), visibility "all".
g = GameFile("p70-withdraw-hidden-gun", "pass 70, 3h: a withdrawal into a HIP Gun and crew (A11.21, the p. 74 example)", phasing="german")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0", melee=True)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F9:0", melee=True)
g.unit("rc", "crew", "defender-crew", "russian", "bd03:F8:0", hidden=True)
g.add("instance-created", {"instance": {"id": "rag", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                        "position": {"at": "bd03:F8:0", "facing": "west"}, "holding": {"holder": "rc", "role": "manned"},
                                        "conditions": {"asl:malfunctioned": False, "asl:hidden": True}}})
g.phases(*TO_CCPH)
g.write()

# A1.123, A11.13 (ruling R70.27): the German APh. The Russian 45mm AT Gun rag and its crew rag-crew stand in [D10]; the German 4-6-7s g1 and g2 are
# ADJACENT in [D9]. The advance panel offers [D10] (tick g1 and g2, #advance-to [D10], #propose-advance-units): no refusal of a Gun's crew any more. End
# the phase: the CCPh. On the CC panel (#cc-location [D10]) the attack builder names the crew hex first, "[D10]: ... crew ..." (.cc-defender
# [data-unit='rag-crew']); g1 and g2 attack it (8 FP against its inherent 2, 4-1, Kill Number 9); the Russian may attack back or not. Dice: the German
# attack 1 (colored) 2, then the Gun's loss dr 3 (<= 9: the Gun is eliminated too); with the Russian's attack, its two dice come in the order the round
# asks. With a colored die other than 1, no Gun dr is made and the Gun stays in [D10] unpossessed. Read back: "close-combat-resolved" whose
# facts.units[rag-crew] carry "manning": true and "guns": ["rag"], the "dice-rolled" purpose "cc-weapon-loss", "instance-eliminated" rag-crew and rag;
# the record line "rag lost (dr 3, A11.13)".
g = GameFile("p70-gun-crew-cc", "pass 70, 3h: an advance onto a Gun's crew and its CC (A11.13)", phasing="german")
g.gun("rag", "defender-at-gun", "russian", "bd03:D10:0", "west")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:D9:0")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:D9:0")
g.phases(*TO_APH)
g.write()

# A1.32, D6.82 (ruling R70.28): the British RPh. Two Abandoned Bren carriers b-bren and b-bren2 in [C10], each with the 2-4-7 HS that left it (b1, b2,
# whose creation records "crewOf"), and an ordinary British 2-4-7 HS b3 beside them, with no leader. The Recombine panel (#recombine-one b1,
# #recombine-two b2, no leader) proposes "play.recombine: b1 and b2 Recombine into their squad (A1.32)" with no dice; committed, the squad is
# "<attempt>-b1". b1 with b3 is refused: "play.deploy-leader ..." (b3 is no Carrier HS). Read back: the two "instance-created" of b1 and b2 carry
# "crewOf"; the Recombination writes "rph-action-taken" and "lineage" (recombined).
g = GameFile("p70-carrier-hs", "pass 70, 3h: Carrier HS Recombine with no leader (A1.32, D6.82)", phasing="british", other="british", year=1944)
g.vehicle("b-bren", "campaign-british-bren-carrier", "british", "bd03:C10:0", "east", abandoned=True, bu=False)
g.vehicle("b-bren2", "campaign-british-bren-carrier", "british", "bd03:C10:0", "east", abandoned=True, bu=False)
for identifier, carrier in (("b1", "b-bren"), ("b2", "b-bren2")):
    g.add("instance-created", {"instance": {"id": identifier, "kind": "asl:half-squad", "definition": "british-half-squad", "side": "british",
                                            "position": {"at": "bd03:C10:0"}, "conditions": {"asl:broken": False, "asl:concealed": False, "asl:hidden": False},
                                            "crewOf": carrier}})
g.unit("b3", "half-squad", "british-half-squad", "british", "bd03:C10:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:J5:0")
g.phases("rph")
g.write()
