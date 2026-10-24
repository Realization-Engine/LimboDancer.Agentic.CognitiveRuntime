"""The Studio games of increment 2c (Passenger action: Mounted Fire, OVR, rout beneath; rulings R65.10 to R65.13), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402


def aboard(game, identifier, kind, definition, side, vehicle, **conditions):
    """A unit riding a vehicle as a Passenger (D6.1); game_file has no helper for one."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": "passenger"}, "conditions": marks(CLEAR, conditions)}})


# Mounted Fire (D6.1, D6.65, D.3): a Stopped German truck at F8 carries a 4-6-7 and a 9-1, two hexes from a Russian 4-4-7 at F10, in the German PFPh.
# The squad fires at half FP, directed by the 9-1 aboard; the truck is then marked as having Prep Fired, and in the MPh the squad may not unload.
g = GameFile("p65-mounted-truck", "pass 65, 2c: a 4-6-7 and a 9-1 aboard a truck fire at a 4-4-7 two hexes off (D6.1, D6.65)", year=1943, month=7)
g.vehicle("dt", "attacker-truck", "german", "bd03:F8:0", "east")
aboard(g, "ds", "squad", "attacker-squad", "german", "dt")
aboard(g, "dl", "leader", "attacker-leader-9-1", "german", "dt")
g.unit("rs", "squad", "defender-squad", "russian", "bd03:F10:0")
g.phases("rph", "pfph")
g.write()

# An OVR by a loaded halftrack (D7.11, D7.16): a CE SPW 251/1 at F8, facing east, with a 4-6-7 and a 9-1 aboard, in the German MPh; the Russian 4-4-7
# at G8 (Open Ground, inside its VCA). Start, enter G8 with an OVR: the squad adds half its FP tripled (6), the 9-1 his -1.
g = GameFile("p65-ovr-halftrack", "pass 65, 2c: a CE SPW 251/1 with a 4-6-7 and a 9-1 aboard overruns a 4-4-7 (D7.11, D7.16)", year=1943, month=7)
g.vehicle("dh", "attacker-halftrack", "german", "bd03:F8:0", "east", bu=False)
aboard(g, "ds", "squad", "attacker-squad", "german", "dh")
aboard(g, "dl", "leader", "attacker-leader-9-1", "german", "dh")
g.unit("rs", "squad", "defender-squad", "russian", "bd03:G8:0")
g.phases("rph", "pfph", "mph")
g.write()

# Rout beneath (D6.1, D5.311): a broken 4-6-7 aboard a Stopped German truck at F8, in the German RtPh; the Russian 4-4-7 is far off at J2. The Rout
# panel offers "Rout ... beneath" the truck, which places the squad on foot in F8 with all its MF spent.
g = GameFile("p65-rout-beneath", "pass 65, 2c: a broken 4-6-7 aboard a Stopped truck routs beneath it (D6.1)", year=1943, month=7)
g.vehicle("dt", "attacker-truck", "german", "bd03:F8:0", "east")
aboard(g, "ds", "squad", "attacker-squad", "german", "dt", broken=True)
g.unit("rs", "squad", "defender-squad", "russian", "bd03:J2:0")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph")
g.write()
