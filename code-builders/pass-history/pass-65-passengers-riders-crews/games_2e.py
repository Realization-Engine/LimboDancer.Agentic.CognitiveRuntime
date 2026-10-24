"""The Studio games of increment 2e (Riders and Bailing Out; rulings R65.16, R65.17), on board 3, in 1943."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402


def rider(game, identifier, kind, definition, side, vehicle, **conditions):
    """A unit riding an AFV as a Rider (D6.2); game_file has no helper for one."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": "rider"}, "conditions": marks(CLEAR, conditions)}})


# Boarding as Riders and a terrain Bail Out (D6.2, D6.21, D6.24): a Russian T-34 M41 at E8 (Open Ground), facing west toward the woods of D8, with a
# 4-4-7 and a 7-0 beside it, in the Russian MPh. They board as Riders (10 of its 14 PP); it starts (a Mechanical Reliability DR) and enters the woods,
# and they Bail Out in E8 first, each with a NMC, under a Prep Fire counter. The German 4-6-7 at G8 may then fire at E8 with FFNAM.
g = GameFile("p65-riders-woods", "pass 65, 2e: a 4-4-7 and a 7-0 ride a T-34 into woods and Bail Out (D6.2, D6.21, D6.24)", phasing="russian", year=1943, month=7)
g.vehicle("rt", "defender-tank", "russian", "bd03:E8:0", "west")
g.unit("rs", "squad", "defender-squad", "russian", "bd03:E8:0")
g.unit("rl", "leader", "defender-leader-7-0", "russian", "bd03:E8:0")
g.unit("ds", "squad", "attacker-squad", "german", "bd03:G8:0")
g.phases("rph", "pfph", "mph")
g.write()

# Riders under fire (D6.23, D6.24): a German PzKpfw IIIH at F8 (BU) with a 4-6-7 Rider holding a LMG; two Russian 4-4-7s at F10, in the Russian PFPh.
# The Rider is always Vulnerable; a pin or break makes it Bail Out beneath the tank with a NMC and the LMG's dr, under a Final Fire counter.
g = GameFile("p65-riders-fire", "pass 65, 2e: a PzKpfw IIIH's 4-6-7 Rider under a Russian attack (D6.23, D6.24)", phasing="russian", year=1943, month=7)
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east")
rider(g, "ds", "squad", "attacker-squad", "german", "dt")
g.weapon("dlmg", "attacker-lmg", "german", "ds")
g.unit("rs1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.unit("rs2", "squad", "defender-squad", "russian", "bd03:F10:0")
g.phases("rph", "pfph")
g.write()
