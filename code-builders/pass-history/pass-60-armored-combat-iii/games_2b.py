"""The Studio games of increment 2b (the Specific Collateral Attack, mixed Locations), on board 3: firer in [F10], target in [D10]."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# D.8A: a PzKpfw IIIH's HE at a CE T-34 that it does not kill; the crew takes 6 FP on the Original TK DR, +2 for CE.
g = GameFile("p60-coll", "pass 60, 2b: a Specific Collateral Attack on a CE T-34's crew (D.8A)", phasing="german", year=1942, month=7)
g.vehicle("gt", "attacker-tank", "german", "bd03:F10:0", "west")
g.vehicle("rt", "defender-tank", "russian", "bd03:D10:0", "east", bu=False)
g.phases("rph", "pfph")
g.write()

# C3.32: a 45L's HE at a squad and a truck in one Location; and at a squad beside a CE PzKpfw IIIH, whose crew takes a General
# Collateral Attack; and at a Known squad beside a concealed one (C3.4).
for game, label, extra in (("p60-mixed", "HE at a squad and a truck together (C3.32)", "truck"),
                           ("p60-mixed-ce", "HE at a squad beside a CE PzKpfw IIIH (C3.32, D.8B)", "tank"),
                           ("p60-conc", "HE at a Known squad beside a concealed one (C3.4)", "concealed")):
    g = GameFile(game, "pass 60, 2b: " + label, phasing="russian", year=1943, month=7)
    g.gun("rg", "defender-at-gun", "russian", "bd03:F10:0", "west")
    g.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
    if extra == "truck":
        g.vehicle("gtr", "attacker-truck", "german", "bd03:D10:0", "east")
    elif extra == "tank":
        g.vehicle("gt", "attacker-tank", "german", "bd03:D10:0", "east", bu=False)
    else:
        g.unit("g2", "squad", "attacker-squad", "german", "bd03:D10:0", concealed=True)
    g.phases("rph", "pfph")
    g.write()
