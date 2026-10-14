"""Pass 45's test games: one file a game, on board 3, written under its own id into the live folder."""
import hashlib
import json
import zipfile

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
VASL = "E:/Archive/GitHub/dlandi/vasl/boards/bdFiles/bd"
TENANT_SOURCE = "p35-rst-cc.game.json"
CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def version(name):
    with zipfile.ZipFile(VASL + name) as archive:
        data = archive.read("LOSData")
    return hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()


class Game:
    def __init__(self, game, label, phasing="german", board="03", defender=None, special=None):
        self.game, self.label, self.phasing = game, label, phasing
        source = json.load(open(LIVE + TENANT_SOURCE, encoding="utf-8"))
        self.tenant = source["tenant"]
        started = dict(source["events"][0]["payload"])
        blob = version(board)
        started["map"] = {"reference": "bd" + board, "version": blob, "boards": [{"board": "bd" + board, "version": blob}]}
        started["phasingSide"] = phasing
        if defender:
            started["scenarioDefender"] = defender
        if special:
            started["specialRules"] = special
        self.events = []
        self.add("game-started", started)

    def add(self, kind, payload):
        self.events.append({
            "eventId": f"{self.game}-{len(self.events) + 1:04d}", "revision": len(self.events) + 1, "time": "2026-10-12T13:00:00.0000000Z",
            "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
        })

    def unit(self, identifier, kind, definition, side, at, **conditions):
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                    "position": {"at": at}, "conditions": dict(CLEAR, **{"asl:" + k.replace("_", "-"): v for k, v in conditions.items()})}})

    def weapon(self, identifier, definition, side, holder, kind="mg", **conditions):
        marks = {"asl:malfunctioned": False}
        marks.update({"asl:" + k.replace("_", "-"): v for k, v in conditions.items()})
        self.add("instance-created", {"instance": {"id": identifier, "kind": "asl:" + kind, "definition": definition, "side": side,
                                                    "holding": {"holder": holder, "role": "possessed"}, "conditions": marks}})

    def phases(self, *names, turn=1, side=None):
        for name in names:
            self.add("phase-changed", {"turn": turn, "phase": name, "phasingSide": side or self.phasing})

    def write(self):
        game = {"schemaVersion": 1, "tenant": self.tenant, "game": self.game, "label": self.label, "synthetic": False, "events": self.events}
        with open(LIVE + self.game + ".game.json", "w", encoding="utf-8", newline="") as out:
            json.dump(game, out, indent=2)
            out.write("\n")
        print(self.game, len(self.events))
