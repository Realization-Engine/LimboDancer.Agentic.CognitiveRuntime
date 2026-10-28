"""The Studio games of increment 5a (the berserk charge: A15.43 to A15.46, E1.533; rulings R72.3 to R72.6).

Board 3 has one cliff hexside ([C4]/[D3]) and no Water Obstacle: round a single hexside the charge always stays on the nearest (two hexes round it, no
next nearest is closer), and a search over all of board 3 meets Shellholes, terrain the movement model does not review (a gap, ruling R72.4). So the first
two games are on board 7, whose river (rows 4 to 7, Water, a depression at level -1) parts a north region from a south pocket; terrain_check_5a.py read
both from the oracle's hex facts (2026-10-27): neither holds terrain the model does not review, and no bridge is recorded. The third game is on board 3.

LOS and ranges, read with the LosCalculator through code-builders/boards/los-survey ("pair" and "routes" modes, built outside the repository,
2026-10-27). The first layout of game (a) put the far German in [G2]: [A3] to [G2] is blocked, so r-1 saw only [A8] and its charge was unreachable, as
the code says; game (a) now plays in the south pocket.
- board 7, [A8] to [A3]: range 5, clear (across the river, [A4] to [A7] Water).
- board 7, [A8] to [G8]: range 6, clear with a hindrance of 2.
- board 7, a land route from [A8] to [G8], each step ADJACENT: [A9], [B9] woods, [C10] woods, [D10], [E10], [F10], [G10] woods, [H10] woods, [I10]
  road, [I9], [H8] road, [G8] (more than one MPh of charge). [A3] is not reachable from the south pocket (oracle hex facts, terrain_check_5a.py).
- board 3, [A1] to [A2]: range 1, clear.

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# (a) A15.45, ruling R72.3: the berserk Russian 4-4-7 in [A8] (the south pocket) sees the German 4-6-7 in [A3] five hexes off across the river, and the
# German 4-6-7 in [G8] six hexes off, reachable by land. It will not charge through the Water: the Movement panel's charge for r-1 names [G8] as the
# Location charged, its next step [A9] (the pocket's only way out of [A8]). Russian MPh. Read back: after a step, "movement-stepped" with
# "charge": "bd07:G8:0".
g = GameFile("p72-charge-next-nearest", "pass 72, 5a: a berserk squad charges the next nearest German, not the one across the river (A15.45)",
             phasing="russian", board="07")
g.unit("r-1", "squad", "defender-squad", "russian", "bd07:A8:0", berserk=True)
g.unit("g-near", "squad", "attacker-squad", "german", "bd07:A3:0")
g.unit("g-far", "squad", "attacker-squad", "german", "bd07:G8:0")
g.phases("rph", "pfph", "mph", side="russian")
g.write()

# (b) A15.45, A15.44, E1.533, A15.3, ruling R72.4: the berserk Russian 4-4-7 in [A8] (the south pocket) sees only the German 4-6-7 in [A3], across the
# Water; nothing in the pocket can be reached. The Movement panel reads "4-4-7 squad ... in [A8] is berserk and stays in place: no route reaches a Known
# enemy unit in its LOS except across a Water Obstacle; at the end of the MPh ...". End the MPh ("end the phase"): the proposal line "4-4-7 squad ... in
# [A8] can reach no Known enemy unit ..." and, in the DFPh, the pending choice panel
# (#play-choice, data-key "berserkHardening:r-1") for the Russian side: "[A8]: 4-4-7 squad ... is berserk and can reach no Known enemy unit in its LOS
# (A15.45), so it loses berserk status; its owner takes Battle Hardening or refuses it (A15.44, A15.3)", with "take it" and "decline it"; Replay titles the
# step "The Russian side is to take or refuse Battle Hardening". Take: r-1 is Replaced by the Russian elite squad (a "lineage" event, "action": "replaced",
# the new instance "<attempt>-r-1", definition "defender-elite-squad", "asl:berserk": false); the record reads "4-4-7 squad ... in [A8] loses berserk status
# and is Battle Hardened: it is Replaced by an unbroken ..." and Replay "The Russian side takes Battle Hardening". Decline: a "conditions-changed" of r-1
# with "asl:berserk": false and nothing else; the record "... its owner refuses Battle Hardening, so it is Good Order". Both answers write "choice-made"
# with "key": "berserkHardening:r-1". Ruling R72.17 changes no verdict of the three games (re-read 2026-10-27): (a) [A3] is unreachable, so [G8] is
# charged with no comparison; (b) the frontier holds only Water bars and no German Location is reached; (c) has one target.
g = GameFile("p72-charge-unreachable", "pass 72, 5a: a berserk squad that can reach no German is offered Battle Hardening as the MPh ends (A15.44)",
             phasing="russian", board="07")
g.unit("r-1", "squad", "defender-squad", "russian", "bd07:A8:0", berserk=True)
g.unit("g-1", "squad", "attacker-squad", "german", "bd07:A3:0")
g.phases("rph", "pfph", "mph", side="russian")
g.write()

# (c) Ruling R72.5: the berserk Russian 4-4-7 in [A1] charges [A2], where the German PzKpfw IIIH and the German 4-6-7 are; before the pass its charge was
# undecided ("play.berserk-vehicle"). It enters [A2] (one MF); the German side's pass is refused until the 4-6-7 makes its FPF ("play.pass-mandatory: g-1
# ...", A8.312). In the CCPh the sequential CC follows (A11.31): if the Russian attack eliminates the 4-6-7, r-1 stays berserk while the tank is there
# (A15.46): the CC record's facts carry "vehicleSides": ["german"] and its effect for r-1 has no "berserkEnded".
g = GameFile("p72-charge-tank-and-squad", "pass 72, 5a: a berserk squad charges a Location holding a German tank and a German squad (A15.43, A8.312)",
             phasing="russian")
g.unit("r-1", "squad", "defender-squad", "russian", "bd03:A1:0", berserk=True)
g.vehicle("g-tk", "attacker-tank", "german", "bd03:A2:0", "east")
g.unit("g-1", "squad", "attacker-squad", "german", "bd03:A2:0")
g.phases("rph", "pfph", "mph", side="russian")
g.write()
