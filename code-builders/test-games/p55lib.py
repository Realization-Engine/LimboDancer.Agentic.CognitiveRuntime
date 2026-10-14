"""Pass 55's test games: pass 50's library with a second nationality, a date, a Gun with its crew, and catalog 1.14.0."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import sys

sys.path.insert(0, WORK + "/pass50")
from p50lib import Game as Base  # noqa: E402

CREWS = {"german": "attacker-crew", "russian": "defender-crew", "british": "british-crew", "american": "american-crew"}


class Game(Base):
    """other: the nationality that takes the Russian side's place (its side id is the nationality); year, month: the scenario's date."""

    def __init__(self, game, label, phasing="german", other="russian", year=1942, month=7, special=None, **more):
        super().__init__(game, label, phasing=phasing, special=special, **more)
        started = self.events[0]["payload"]
        started["catalog"] = "asl-scenario-a1@1.14.0"
        started["scenarioYear"], started["scenarioMonth"] = year, month
        if other != "russian":
            started["sides"] = [dict(side, id=other, nationality=other) if side["id"] == "russian" else side for side in started["sides"]]
        self.other = other

    def gun(self, identifier, definition, side, at, facing, crew=None, **conditions):
        crew = crew or identifier + "-crew"
        self.unit(crew, "crew", CREWS[side], side, at)
        marks = {"asl:malfunctioned": False}
        marks.update({"asl:" + k.replace("_", "-"): v for k, v in conditions.items()})
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:gun", "definition": definition, "side": side,
                                                    "position": {"at": at, "facing": facing}, "holding": {"holder": crew, "role": "manned"},
                                                    "conditions": marks}})
