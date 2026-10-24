"""The Studio game of increment 2g (the crew's own acts, D5.41, D5.411, D5.42, D6.631, D3.43, D5.8; ruling R65.20), on board 3, in 1943."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile, marks  # noqa: E402


def led_vehicle(game, identifier, definition, side, at, facing, morale, leadership):
    """A vehicle with an Armor Leader (D3.4); game_file's vehicle takes none."""
    game.add("instance-created", {"instance": {"id": identifier, "kind": "asl:vehicle", "definition": definition, "side": side,
                                                "position": {"at": at, "facing": facing}, "armorLeader": {"morale": morale, "leadership": leadership},
                                                "conditions": marks({"asl:concealed": False, "asl:hidden": False}, {})}})


# The brief's game: a German PzKpfw IIIH with a 9-1 Armor Leader at F8 and a German SPW 251/1 at F10, both unmoved, in the German MPh; a Russian squad
# far off at F2. Abandon the halftrack with its MG Removed (D6.631) and the tank with a self-destruction (malfunction, so it can be re-crewed); pass the
# DEFENDER's window on each crew; play on to the German MPh of Game Turn 2 and re-crew the tank with its crew, whose 9-1 returns (D3.43).
g = GameFile("p65-crew-acts", "pass 65, 2g: a PzKpfw IIIH with a 9-1 and a SPW 251/1 Abandoned and re-crewed (D5.41, D5.42, D6.631)", phasing="german",
             year=1943, month=7)
led_vehicle(g, "dt", "attacker-tank", "german", "bd03:F8:0", "east", 9, -1)
g.vehicle("dh", "attacker-halftrack", "german", "bd03:F10:0", "east", bu=False)
g.unit("rs", "squad", "defender-squad", "russian", "bd03:F2:0")
g.phases("rph", "pfph", "mph")
g.write()
