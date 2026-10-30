"""Fresh D3.7 Studio positions. Refuse to overwrite any existing or played game."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / "test-games"))
from game_file import GameFile

destination = Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
for name, shortage in (("wire-ma-breakdown", False), ("wire-ma-low-ammo", True)):
    path = destination / (name + ".game.json")
    if path.exists():
        raise SystemExit(f"Refusing to overwrite {path}")
    # Board 01 B10/B8 is the established pass-50 MA/repair acceptance position.
    game = GameFile(name, "W-D3.7: " + ("shortage, Low Ammo and Disabled" if shortage else "MA malfunction and repair"),
                    board="01", special=["ammo-shortage:german"] if shortage else None)
    game.vehicle("g-tank", "attacker-tank", "german", "bd01:B10:0", "north-east", bu=False)
    game.unit("r1", "squad", "defender-squad", "russian", "bd01:B8:0")
    game.san("german", 0)
    game.san("russian", 0)
    game.phases("rph", "pfph")
    for event in game.events:
        event["eventId"] = f"{name}-fixture-{event['revision']}-1"
    record = {"schemaVersion": 1, "tenant": game.tenant, "game": name, "label": game.label,
              "synthetic": False, "minigame": "d3-breakdown", "events": game.events}
    path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(path)
