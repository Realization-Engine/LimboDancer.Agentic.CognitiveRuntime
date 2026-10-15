"""The narrower Studio check's control game: a German squad with a DC and a LMG in [F9] beside a CE T-34 in [F10], for the throw panel's AFV
list, the Gun panel's MG, and their layout at 1920, 1024, and 320 pixels."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

g = GameFile("p60-check-controls", "pass 60, the Studio check: the AFV lists and the MG at three widths", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F9:0")
g.weapon("dc", "attacker-dc", "german", "g1", kind="dc")
g.weapon("lmg", "attacker-lmg", "german", "g1", kind="mg")
g.vehicle("rt", "defender-tank", "russian", "bd03:F10:0", "south-east", bu=False)
g.phases("rph", "pfph")
g.write()
