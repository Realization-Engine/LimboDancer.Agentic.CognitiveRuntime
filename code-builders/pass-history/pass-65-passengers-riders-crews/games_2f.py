"""The Studio games of increment 2f (Carriers, D6.8 to D6.84, A26.213; ruling R65.18), on board 3, in 1944."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CLEAR, GameFile, marks  # noqa: E402


def aboard(game, identifier, kind, definition, side, vehicle):
    """A unit riding a vehicle as a Passenger (D6.1); game_file has no helper for one."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                "position": {"in": vehicle, "role": "passenger"}, "conditions": marks(CLEAR, {})}})


# The brief's game: a British Bren carrier at F8 (Open Ground) under a German 4-6-7's fire from F10, with a PzKpfw IIIH beside the squad, in the German
# PFPh. The brief's 4-5-8 squad cannot ride it (D6.81: four PP, for SMC, SW, and ammunition; a squad is ten), so an 8-0 leader rides it as its Passenger.
# The squad's attack reaches the Carrier's crew and leader, always CE (D6.84); the tank's AP that eliminates it, with the crew surviving, places the
# British 2-4-7 HS beneath the wreck (D6.82, D5.6).
g = GameFile("p65-carrier-fire", "pass 65, 2f: a German 4-6-7 and a PzKpfw IIIH at a Bren carrier with an 8-0 aboard (D6.82, D6.84)", phasing="german",
             other="british", year=1944, month=7)
g.vehicle("ub", "campaign-british-bren-carrier", "british", "bd03:F8:0", "east")
aboard(g, "ul", "leader", "british-leader-8-0", "british", "ub")
g.unit("ds", "squad", "attacker-squad", "german", "bd03:F10:0")
g.vehicle("dt", "attacker-tank", "german", "bd03:F10:0", "north-east")
g.phases("rph", "pfph")
g.write()

# The quicker unload (D6.83): the same Bren carrier, unmoved, in the British MPh; its inherent HS unloads as a Passenger does and the Carrier is
# Abandoned. The BU toggle is not offered for it (D6.84).
g = GameFile("p65-carrier-unload", "pass 65, 2f: a Bren carrier's inherent HS unloads and Abandons it (D6.83)", phasing="british", other="british",
             year=1944, month=7)
g.vehicle("ub", "campaign-british-bren-carrier", "british", "bd03:F8:0", "east")
aboard(g, "ul", "leader", "british-leader-8-0", "british", "ub")
g.unit("ds", "squad", "attacker-squad", "german", "bd03:F2:0")
g.phases("rph", "pfph", "mph")
g.write()
