"""Fresh D3.7 Studio positions. Refuse to overwrite any existing or played game."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / "test-games"))
from game_file import GameFile

destination = Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
positions = (("wire-ma-breakdown", False), ("wire-ma-low-ammo", True), ("wire-vehicle-repair", False))
selected = set(sys.argv[2:])
if selected - {name for name, _ in positions}:
    raise SystemExit("Unknown position requested")
for name, shortage in positions:
    if selected and name not in selected:
        continue
    path = destination / (name + ".game.json")
    if path.exists():
        raise SystemExit(f"Refusing to overwrite {path}")
    # Board 01 B10/B8 is the established pass-50 MA/repair acceptance position.
    repair = name == "wire-vehicle-repair"
    game = GameFile(name, "W-D3.7: " + ("crew and Hero Rider repair" if repair else
                    "shortage, Low Ammo and Disabled" if shortage else "MA malfunction and repair"),
                    board="01", year=1943 if repair else 1942, special=["ammo-shortage:german"] if shortage else None)
    conditions = {"malfunctioned": True, "bmg_malfunctioned": True, "cmg_malfunctioned": True} if repair else {}
    game.vehicle("g-tank", "attacker-tank", "german", "bd01:B10:0", "north-east", bu=repair, **conditions)
    if repair:
        game.vehicle("g-halftrack", "attacker-halftrack", "german", "bd01:C10:0", "north-east", bu=True, malfunctioned=True)
        game.passenger("g-hero", "hero", "attacker-hero", "german", "g-halftrack", role="rider")
    game.unit("r1", "squad", "defender-squad", "russian", "bd01:B8:0")
    game.san("german", 0)
    game.san("russian", 0)
    game.phases(*("rph",) if repair else ("rph", "pfph"))
    for event in game.events:
        event["eventId"] = f"{name}-fixture-{event['revision']}-1"
    record = {"schemaVersion": 1, "tenant": game.tenant, "game": name, "label": game.label,
              "synthetic": False, "minigame": "d3-breakdown", "events": game.events}
    path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(path)
