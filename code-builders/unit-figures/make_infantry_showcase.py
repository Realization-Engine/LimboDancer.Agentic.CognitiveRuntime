"""Writes src/ASL/units/examples/bd01-infantry-figures.units.json: a synthetic placement set on board 1 with every catalog side in every
infantry counter type, to see the figures (ASL Unit Figure Art Plan) on a real board (the user, 2026-10-18).

Each side has a column of board 1 (C, G, K, O, S, W, AA, EE); down it, rows 1 to 10: squad, half-squad, crew, leader, hero, wounded hero,
broken squad, pinned squad, berserk squad, and a squad taken prisoner. The values are illustrative, as the other examples' are; the set is
display input, never game state.

Run from the repository root:  python code-builders/unit-figures/make_infantry_showcase.py
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "src" / "ASL" / "units" / "examples" / "bd01-infantry-figures.units.json"

SIDES = ["german", "russian", "british", "american", "finnish", "italian", "axis-minor", "french"]
COLUMNS = ["C", "G", "K", "O", "S", "W", "AA", "EE"]
# Illustrative squad values per side, near each army's 1st Line squad: firepower, range, morale.
SQUADS = {
    "german": (4, 6, 7), "russian": (4, 4, 7), "british": (4, 5, 7), "american": (6, 6, 6),
    "finnish": (6, 4, 8), "italian": (3, 4, 7), "axis-minor": (4, 4, 7), "french": (4, 5, 7),
}


def squad(side, unit_id, location, states=()):
    fp, rng, morale = SQUADS[side]
    unit = {
        "id": unit_id, "kind": "asl:squad", "side": side, "location": location,
        "faces": {
            "front": {"firepower": fp, "range": rng, "morale": morale, "identity": "A", "class": "1st-line"},
            "broken": {"broken-morale": morale, "bpv": 10},
        },
    }
    if states:
        unit["states"] = list(states)
    return unit


def units_for(side, column):
    fp, rng, morale = SQUADS[side]
    at = lambda row: f"bd01:{column}{row}:0"
    key = side.replace("-", "")
    return [
        squad(side, f"{key}-squad", at(1)),
        {"id": f"{key}-half-squad", "kind": "asl:half-squad", "side": side, "location": at(2),
         "faces": {"front": {"firepower": max(1, fp // 2), "range": max(1, rng // 2), "morale": morale, "identity": "B", "class": "1st-line"},
                   "broken": {"broken-morale": morale, "bpv": 5}}},
        {"id": f"{key}-crew", "kind": "asl:crew", "side": side, "location": at(3),
         "faces": {"front": {"firepower": 2, "range": 2, "morale": 7, "identity": "3"}, "broken": {"broken-morale": 7}}},
        {"id": f"{key}-leader", "kind": "asl:leader", "side": side, "location": at(4),
         "faces": {"front": {"leadership": -1, "morale": 8}, "broken": {"broken-morale": 8}}},
        {"id": f"{key}-hero", "kind": "asl:hero", "side": side, "location": at(5),
         "faces": {"front": {"firepower": 1, "range": 4, "morale": 9}, "wounded": {"firepower": 1, "range": 3, "morale": 8}}},
        {"id": f"{key}-hero-wounded", "kind": "asl:hero", "side": side, "location": at(6), "states": ["asl:wounded"],
         "faces": {"front": {"firepower": 1, "range": 4, "morale": 9}, "wounded": {"firepower": 1, "range": 3, "morale": 8}}},
        squad(side, f"{key}-broken", at(7), ["asl:broken"]),
        squad(side, f"{key}-pinned", at(8), ["asl:pinned"]),
        squad(side, f"{key}-berserk", at(9), ["asl:berserk"]),
        squad(side, f"{key}-prisoner", at(10), ["asl:captured"]),
    ]


def main():
    units = [unit for side, column in zip(SIDES, COLUMNS) for unit in units_for(side, column)]
    data = {
        "schemaVersion": 1,
        "setId": "bd01-infantry-figures",
        "label": "Synthetic infantry on board 1: every side in every infantry type",
        "synthetic": True,
        "vocabulary": ["asl@1.17.0"],
        "units": units,
    }
    OUT.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"wrote {OUT.relative_to(ROOT)}: {len(units)} units")


if __name__ == "__main__":
    main()
