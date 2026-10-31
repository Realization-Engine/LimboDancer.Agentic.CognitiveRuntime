"""Fresh governed Studio positions for W-A23.3; never overwrite played games."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / "test-games"))
from game_file import GameFile

destination = Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
for suffix in ("departure", "other-mover", "phase-end"):
    name = "wire-dc-" + suffix
    path = destination / (name + ".game.json")
    if path.exists():
        raise SystemExit(f"Refusing to overwrite {path}")
    game = GameFile(name, "W-A23.3 DC settlement: " + suffix, board="03")
    game.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
    game.weapon("dc", "attacker-dc", "german", "g1", kind="dc")
    game.unit("g2", "squad", "attacker-squad", "german", "bd03:E10:0")
    game.unit("r1", "squad", "defender-squad", "russian", "bd03:G10:0")
    game.phases("rph", "pfph", "mph")
    for event in game.events:
        event["eventId"] = f"{name}-fixture-{event['revision']}-1"
    record = {"schemaVersion": 1, "tenant": game.tenant, "game": name, "label": game.label, "synthetic": False, "events": game.events}
    path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(path)
