"""The Studio game of increment 2j (a wreck attacked as its vehicle, and Scrounging; D10.1, D10.5 to D10.52; ruling R65.23), on board 3, in 1942."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# D10.5: a German PzKpfw IIIH wreck (not burning) at F8 with a Russian 4-4-7 and a Russian HS in it, in the Russian RPh. The squad scrounges the wreck
# (the Scrounging section): a dr of 1 to 3 salvages its BMG as the German LMG; the HS is then refused (Scrounged). D10.1: in the PFPh the T-34 M41 at C8
# fires AP at F8 naming the wreck; a TK DR of half its Final TK# or less burns it (a Blaze), anything else does nothing.
g = GameFile("p65-wreck-scrounge", "pass 65, 2j: a PzKpfw IIIH wreck scrounged by a Russian squad and burned by a T-34 (D10.1, D10.5)",
             phasing="russian", year=1942, month=7)
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east")
g.add("vehicle-wrecked", {"id": "dt", "burning": False})
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F8:0")
g.unit("rh", "half-squad", "defender-half-squad", "russian", "bd03:F8:0")
g.vehicle("rt", "defender-tank", "russian", "bd03:C8:0", "east")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B2:0")
g.phases("rph")
g.write()
