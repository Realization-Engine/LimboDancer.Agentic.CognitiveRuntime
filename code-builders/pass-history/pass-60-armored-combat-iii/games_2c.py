"""The Studio games of increment 2c (task 60.2, Indirect Fire), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# C1.55: a German 50mm mortar in the woods of [H10] fires at [D10], where a Russian squad stands beside a BU T-34 (range 4).
g = GameFile("p60-mtr-afv", "pass 60, 60.2: a mortar's hit on a T-34 and the squad beside it (C1.55, D9.3)", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:H10:0")
g.weapon("mtr", "attacker-light-mortar", "german", "g1", kind="light-mortar")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:D10:0")
g.vehicle("rt", "defender-tank", "russian", "bd03:D10:0", "east")
g.phases("rph", "pfph")
g.write()

# The same mortar at a truck in the open (the Vehicle line).
g = GameFile("p60-mtr-truck", "pass 60, 60.2: a mortar's hit on a truck (C1.55, A7.308)", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:H10:0")
g.weapon("mtr", "attacker-light-mortar", "german", "g1", kind="light-mortar")
g.vehicle("rtr", "defender-truck", "russian", "bd03:D10:0", "east")
g.phases("rph", "pfph")
g.write()

# B23.32: a mortar at the two-level stone building [M2], a Russian squad on each level.
g = GameFile("p60-mtr-levels", "pass 60, 60.2: a mortar at both levels of a stone building (B23.32)", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:K3:0")
g.weapon("mtr", "attacker-light-mortar", "german", "g1", kind="light-mortar")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:M2:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:M2:1")
g.phases("rph", "pfph")
g.write()

# C11.5: a Russian mortar at a German INF Gun facing it: the gunshield gives +1 against Indirect Fire.
g = GameFile("p60-mtr-gun", "pass 60, 60.2: a mortar at a crewed Gun, the gunshield +1 (C11.4, C11.5)", phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:H10:0")
g.weapon("mtr", "defender-light-mortar", "russian", "r1", kind="light-mortar")
g.gun("gg", "attacker-inf-gun", "german", "bd03:D10:0", "east")
g.phases("rph", "pfph")
g.write()
