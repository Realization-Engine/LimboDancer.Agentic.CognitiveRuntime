"""The Studio games of increment 2d (task 60.3, FT, MOL, and DC at an AFV), on board 3: the attacker in [F9], the AFV in [F10]."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# A22.34: a Russian FT at a CE PzKpfw IIIH, named.
g = GameFile("p60-ft-afv", "pass 60, 60.3: a FT at a CE PzKpfw IIIH (A22.34, C7.344)", phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.weapon("ft", "defender-ft", "russian", "r1", kind="ft")
g.vehicle("gt", "attacker-tank", "german", "bd03:F10:0", "south-east", bu=False)
g.phases("rph", "pfph")
g.write()

# A22.612: a Russian MOL at a CE PzKpfw IIIH beside a German squad.
g = GameFile("p60-mol-afv", "pass 60, 60.3: a MOL at a CE PzKpfw IIIH and the squad beside it (A22.612)", phasing="russian", year=1943, month=7,
             special=["mol:russian"])
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.vehicle("gt", "attacker-tank", "german", "bd03:F10:0", "south-east", bu=False)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F10:0")
g.phases("rph", "pfph")
g.write()

# A23.5, C7.346: a German DC thrown at a BU T-34.
g = GameFile("p60-dc-afv", "pass 60, 60.3: a DC thrown at a BU T-34 (A23.5, C7.346)", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F9:0")
g.weapon("dc", "attacker-dc", "german", "g1", kind="dc")
g.vehicle("rt", "defender-tank", "russian", "bd03:F10:0", "south-east")
g.phases("rph", "pfph")
g.write()
