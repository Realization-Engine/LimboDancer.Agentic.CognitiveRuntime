"""The Studio games of increment 2b (Passengers as targets, D.8, D6.9; rulings R65.5 to R65.9), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402


def aboard(game, identifier, kind, definition, side, vehicle):
    """A unit riding a vehicle as a Passenger (D6.1); game_file has no helper for one."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": "passenger"}, "conditions": marks(CLEAR, {})}})


# The brief's IFT game: a Russian 4-4-7 with a LMG fires at a CE SPW 251/1 two hexes away, in the open, with a German 4-6-7 aboard, in the Russian
# PFPh. Its crew and the squad aboard each take the attack's DR with +2 for CE (D.8B, D5.31).
g = GameFile("p65-prc-halftrack", "pass 65, 2b: a 4-4-7 and LMG fire at a CE SPW 251/1 carrying a 4-6-7 (D.8B, D5.31)", phasing="russian", year=1943, month=7)
g.unit("rs", "squad", "defender-squad", "russian", "bd03:F10:0")
g.weapon("rl", "defender-lmg", "russian", "rs")
g.vehicle("dh", "attacker-halftrack", "german", "bd03:F8:0", "east", bu=False)
aboard(g, "ds", "squad", "attacker-squad", "german", "dh")
g.phases("rph", "pfph")
g.write()

# The same at a BU SPW 251/1: its Passengers are not attacked from two hexes away at its level (D6.61).
g = GameFile("p65-prc-halftrack-bu", "pass 65, 2b: the same at a BU SPW 251/1; its Passengers are not attacked (D6.61)", phasing="russian", year=1943, month=7)
g.unit("rs", "squad", "defender-squad", "russian", "bd03:F10:0")
g.weapon("rl", "defender-lmg", "russian", "rs")
g.vehicle("dh", "attacker-halftrack", "german", "bd03:F8:0", "east", bu=True)
aboard(g, "ds", "squad", "attacker-squad", "german", "dh")
g.phases("rph", "pfph")
g.write()

# The brief's kill game: a Russian 45mm AT Gun beside a German truck carrying a HS, in the Russian PFPh. An AP hit that eliminates the truck gives the
# HS its Survival DR against the truck's cs# 6 (D6.9); one that leaves it standing attacks the HS with no DRM (D.8A, D6.71).
g = GameFile("p65-prc-truck", "pass 65, 2b: a 45mm AT Gun at a German truck carrying a HS (D.8A, D6.9)", phasing="russian", year=1943, month=7)
g.gun("rg", "defender-at-gun", "russian", "bd03:F10:0", "north-east")
g.vehicle("dt", "attacker-truck", "german", "bd03:F9:0", "east")
aboard(g, "dhs", "half-squad", "attacker-half-squad", "german", "dt")
g.phases("rph", "pfph")
g.write()
