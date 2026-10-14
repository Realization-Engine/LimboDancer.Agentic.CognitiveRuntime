"""Survey the scenario cards for pass 40 task 40.9: boards per card, setup areas by board, provisional turns, and a chart past END.

Usage: python cards_survey.py
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import glob
import json

ROOT = REPO + "/src/ASL/"
for path in sorted(glob.glob(ROOT + "units/scenarios/*.scenario-card.json") + glob.glob(ROOT + "boards/cards/*.scenario-card.json")):
    card = json.load(open(path, encoding="utf-8"))
    boards = [(item.get("board"), item.get("slot"), item.get("row"), item.get("reversed")) for item in card.get("boards", [])]
    areas = []
    for side in card.get("sides", []):
        for group in side.get("groups", []):
            for area in group.get("areas", []):
                areas.append((side.get("side"), area.get("kind"), area.get("board"), area.get("from"), area.get("to"), len(area.get("hexes") or [])))
    turns = card.get("turns")
    text = json.dumps(card)
    print(path.split("/")[-1])
    print("   boards", boards)
    print("   turns", turns if not isinstance(turns, dict) else {key: turns[key] for key in list(turns)[:8]})
    print("   areas", areas)
    print("   provisional/gray/END mentioned:", any(word in text.lower() for word in ("provisional", "gray turn", "grey turn", "past end")))
