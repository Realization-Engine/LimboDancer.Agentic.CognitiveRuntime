"""A small Studio test game written as a file: a few counters on one board, at the phase wanted.

A rule pass shows each thing it builds in a game of a few counters made for it (the standing rules). This is the library those games
are written with; it replaces the three that passes 45, 50, and 55 each stacked on the last. A game is written under its own id into
the live folder, where the Studio finds it; the folder is not committed.

    from game_file import GameFile
    game = GameFile("p60-example", "what the game is for", phasing="german")
    game.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
    game.vehicle("t1", "defender-tank", "russian", "bd03:F10:0", "west")
    game.phases("rph", "pfph", "mph")
    game.write()

Never build a game from one that has been played: it carries the played events and does not replay. The game-started payload alone is
taken from TEMPLATE, a live game of board 3.
"""
import hashlib
import json
import os
import sys
import zipfile

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
from paths import REPO, VASL_ROOT  # noqa: E402

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
TEMPLATE = "p35-rst-cc.game.json"
CATALOG = "asl-scenario-a1@1.14.0"
CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
CREWS = {"german": "attacker-crew", "russian": "defender-crew", "british": "british-crew", "american": "american-crew"}


def board_version(name):
    """The version a game records for a VASL board: the Git blob id of the board archive's LOSData entry."""
    with zipfile.ZipFile(VASL_ROOT + "/boards/bdFiles/bd" + name) as archive:
        data = archive.read("LOSData")
    return hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()


def marks(defaults, conditions):
    return dict(defaults, **{"asl:" + key.replace("_", "-"): value for key, value in conditions.items()})


class GameFile:
    """other: the nationality that takes the Russian side's place (its side id is the nationality). year, month: the scenario's date.
    defender: the Scenario Defender, for a game that starts in setup. special: the game's special rules, such as "night:2"."""

    def __init__(self, game, label, phasing="german", board="03", other="russian", year=1942, month=7, defender=None, special=None, catalog=CATALOG):
        self.game, self.label, self.phasing, self.other = game, label, phasing, other
        with open(LIVE + TEMPLATE, encoding="utf-8") as source:
            template = json.load(source)
        self.tenant = template["tenant"]
        started = dict(template["events"][0]["payload"])
        blob = board_version(board)
        started["map"] = {"reference": "bd" + board, "version": blob, "boards": [{"board": "bd" + board, "version": blob}]}
        started["phasingSide"] = phasing
        started["catalog"] = catalog
        started["scenarioYear"], started["scenarioMonth"] = year, month
        if defender:
            started["scenarioDefender"] = defender
        if special:
            started["specialRules"] = special
        if other != "russian":
            started["sides"] = [dict(side, id=other, nationality=other) if side["id"] == "russian" else side for side in started["sides"]]
        self.events = []
        self.add("game-started", started)

    def add(self, kind, payload):
        number = len(self.events) + 1
        self.events.append({"eventId": f"{self.game}-{number:04d}", "revision": number, "time": "2026-10-12T13:00:00.0000000Z",
                            "source": "map-studio", "type": kind, "visibility": "all", "payload": payload})

    def unit(self, identifier, kind, definition, side, at, **conditions):
        """A squad, half-squad, crew, leader, or other Infantry counter. A condition is a keyword: broken=True, concealed=True."""
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                    "position": {"at": at}, "conditions": marks(CLEAR, conditions)}})

    def weapon(self, identifier, definition, side, holder, kind="mg", **conditions):
        """A SW in a unit's possession; kind is "mg", "latw", and so on."""
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                    "holding": {"holder": holder, "role": "possessed"},
                                                    "conditions": marks({"asl:malfunctioned": False}, conditions)}})

    def vehicle(self, identifier, definition, side, at, facing, **conditions):
        """A vehicle with its VCA, such as "east"; never "north". Conditions: motion=True, bogged=True, bu=False for a CE crew."""
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:vehicle", "definition": definition, "side": side,
                                                    "position": {"at": at, "facing": facing},
                                                    "conditions": marks({"asl:concealed": False, "asl:hidden": False}, conditions)}})

    def gun(self, identifier, definition, side, at, facing, crew=None, **conditions):
        """A Gun with the crew that mans it, which is created with it under the id given or <gun>-crew."""
        crew = crew or identifier + "-crew"
        self.unit(crew, "crew", CREWS[side], side, at)
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:gun", "definition": definition, "side": side,
                                                    "position": {"at": at, "facing": facing}, "holding": {"holder": crew, "role": "manned"},
                                                    "conditions": marks({"asl:malfunctioned": False}, conditions)}})

    def phases(self, *names, turn=1, side=None):
        """The phase changes, in order, to the phase wanted."""
        for name in names:
            self.add("phase-changed", {"turn": turn, "phase": name, "phasingSide": side or self.phasing})

    def write(self):
        game = {"schemaVersion": 1, "tenant": self.tenant, "game": self.game, "label": self.label, "synthetic": False, "events": self.events}
        with open(LIVE + self.game + ".game.json", "w", encoding="utf-8", newline="") as out:
            json.dump(game, out, indent=2)
            out.write("\n")
        print(self.game, len(self.events))
