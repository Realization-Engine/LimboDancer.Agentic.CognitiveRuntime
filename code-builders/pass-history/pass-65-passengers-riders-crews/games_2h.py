"""The Studio games of increment 2h (the Immobilization TC's causes, Crew Survival, Brew Up; D5.5, D5.6, D8.11, D9.2; ruling R65.21), on board 3, in 1943."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# The second cause (D5.5, D8.11): a German PzKpfw IIIH already immobilized at F8, under the Russian 45mm AT Gun's AP from F10, in the Russian PFPh.
# A TK DR that does nothing, where an Original 5 would have killed or Shocked it, makes its crew take the TC.
g = GameFile("p65-tc-second", "pass 65, 2h: a 45mm AT Gun at an immobilized PzKpfw IIIH; the TC's second cause (D5.5, D8.11)",
             phasing="russian", year=1943, month=7)
g.gun("rg", "defender-at-gun", "russian", "bd03:F10:0", "north-east")
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east", immobilized=True)
g.phases("rph", "pfph")
g.write()

# The same with the T-34's 76mm, whose Final TK# against the PzKpfw IIIH leaves room for a miss above it with a kill at 5.
g = GameFile("p65-tc-second-t34", "pass 65, 2h: a T-34 at an immobilized PzKpfw IIIH; the TC's second cause (D5.5, D8.11)",
             phasing="russian", year=1943, month=7)
g.vehicle("rt", "defender-tank", "russian", "bd03:F10:0", "north-east")
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east", immobilized=True)
g.phases("rph", "pfph")
g.write()

# The first cause on the mortar path (C1.55, D5.5): the Russian 50mm with its squad at H10 at a German SPW 251/1 at D10, in the Russian PFPh. An IFT DR of 4
# with a colored die not below the white is a Final 2 (AF and OT -1 each), the K/# DR: a hull hit that immobilizes it, and its crew takes the TC.
g = GameFile("p65-tc-mortar", "pass 65, 2h: a 50mm mortar immobilizes a SPW 251/1; its crew takes the TC (C1.55, D5.5)",
             phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:H10:0")
g.weapon("mtr", "defender-light-mortar", "russian", "r1", kind="light-mortar")
g.vehicle("dh", "attacker-halftrack", "german", "bd03:D10:0", "east")
g.phases("rph", "pfph")
g.write()
