"""Pass 50's test games: pass 45's library with vehicles."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass45")
from p45lib import Game as Base  # noqa: E402


class Game(Base):
    def vehicle(self, identifier, definition, side, at, facing, **conditions):
        marks = {"asl:concealed": False, "asl:hidden": False}
        marks.update({"asl:" + k.replace("_", "-"): v for k, v in conditions.items()})
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:vehicle", "definition": definition, "side": side,
                                                    "position": {"at": at, "facing": facing}, "conditions": marks}})
