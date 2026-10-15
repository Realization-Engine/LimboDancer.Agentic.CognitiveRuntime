"""The Studio games of increment 2f (task 60.6 with 50.5, a MG's To Kill attack on an AFV), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# A9.61: a Russian HMG in [F10] at a CE SPW 251/1 in [D10] facing it; a Stun on the Kill Number.
g = GameFile("p60-mg-stun", "pass 60, 60.6: a HMG's To Kill attack on a CE halftrack (A9.61)", phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.weapon("hmg", "defender-hmg", "russian", "r1", kind="mg")
g.vehicle("ght", "attacker-halftrack", "german", "bd03:D10:0", "east", bu=False)
g.phases("rph", "pfph")
g.write()

# A9.611: a German LMG at a CE T-34 that it cannot harm; the crew's Specific Collateral Attack.
g = GameFile("p60-mg-coll", "pass 60, 60.6: a LMG's To Kill attack on a CE T-34, its Collateral Attack (A9.611)", phasing="german", year=1943, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F10:0")
g.weapon("lmg", "attacker-lmg", "german", "g1", kind="mg")
g.vehicle("rt", "defender-tank", "russian", "bd03:D10:0", "east", bu=False)
g.phases("rph", "pfph")
g.write()
