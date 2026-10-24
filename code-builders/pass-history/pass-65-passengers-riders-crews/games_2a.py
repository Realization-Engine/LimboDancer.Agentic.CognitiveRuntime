"""The Studio games of increment 2a (the vehicle crew, D5.1; ruling R65.19), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# The brief's game: a Russian 45mm AT Gun with its crew at a CE PzKpfw IIIH in the open, two hexes away, in the Russian PFPh.
g = GameFile("p65-crew-gun", "pass 65, 2a: a 45mm AT Gun at a CE PzKpfw IIIH; a surviving crew is the German vehicle crew (D5.1, D5.6)",
             phasing="russian", year=1942, month=7)
g.gun("rg", "defender-at-gun", "russian", "bd03:F10:0", "north-east")
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east", bu=False)
g.phases("rph", "pfph")
g.write()

# The Play test's shot: the T-34 M41 at the PzKpfw IIIH two hexes away (To Hit 4 and 2, To Kill 3 and 3, Crew Survival 2 and 2).
g = GameFile("p65-crew-tank", "pass 65, 2a: a T-34's AP eliminates a PzKpfw IIIH; its crew survives as the German vehicle crew (D5.1, D5.6)",
             phasing="russian", year=1942, month=7)
g.vehicle("rt", "defender-tank", "russian", "bd03:F10:0", "north-east")
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east")
g.phases("rph", "pfph")
g.write()
