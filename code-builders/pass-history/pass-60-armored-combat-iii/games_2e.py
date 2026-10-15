"""The Studio games of increment 2e (task 60.5, Guns as targets and Random SW/Gun Destruction), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# A9.74: two Russian squads in [D9] fire at a German squad with a LMG in [D10]; a KIA, then the LMG's dr.
g = GameFile("p60-random-sw", "pass 60, 60.5: a KIA on a squad holding a LMG, the LMG's dr (A9.74)", phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:D9:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:D9:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
g.weapon("lmg", "attacker-lmg", "german", "g1", kind="mg")
g.phases("rph", "pfph")
g.write()

# C3.41, C11.4: a German INF Gun in [F10] fires at an unattended Russian 45mm Gun in [D10].
g = GameFile("p60-unattended", "pass 60, 60.5: a shot at an unattended Gun (C3.41, C11.4)", phasing="german", year=1943, month=7)
g.gun("gg", "attacker-inf-gun", "german", "bd03:F10:0", "west")
g.add("instance-created", {"instance": {"id": "rg", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                        "position": {"at": "bd03:D10:0", "facing": "east"}, "conditions": {"asl:malfunctioned": False}}})
g.phases("rph", "pfph")
g.write()
