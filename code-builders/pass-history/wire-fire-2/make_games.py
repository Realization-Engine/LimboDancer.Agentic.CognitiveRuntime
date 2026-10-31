"""Fresh PR 85 positions. Refuse to overwrite any existing, possibly played game."""
import json
import sys
from pathlib import Path

tools = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(tools / "test-games"))
from game_file import GameFile

destination = Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
names = ("wire-c9-pair-review", "wire-c9-light-review", "wire-c9-gun-review")
selected = set(sys.argv[2:]) or set(names)
if selected - set(names):
    raise SystemExit("Unknown position requested")
for name in names:
    if name not in selected:
        continue
    path = destination / (name + ".game.json")
    if path.exists():
        raise SystemExit(f"Refusing to overwrite {path}")
    if name == "wire-c9-gun-review":
        game = GameFile(name, "PR 85: shared Gun and Canister participation reading", phasing="american", other="american", board="01", year=1943)
        game.gun("gun", "american-at-gun", "american", "bd01:F5:0", "west")
        for number, at in enumerate(("D4", "D5", "C5"), 1):
            game.unit(f"target-{number}", "squad", "attacker-squad", "german", f"bd01:{at}:0")
        game.san("german", 0)
        game.san("american", 0)
    else:
        game = GameFile(name, "PR 85: two stacked SMC with " + ("81mm Gun mortar" if name == "wire-c9-pair-review" else "50mm light mortar"))
        for leader in ("g-leader", "g-second"):
            game.unit(leader, "leader", "attacker-leader-8-0", "german", "bd03:B10:0")
        if name == "wire-c9-pair-review":
            game.add("instance-created", {"instance": {
                "id": "mortar", "kind": "asl:gun", "definition": "campaign-german-81mm-mortar", "side": "german",
                "position": {"at": "bd03:B10:0", "facing": "west"},
                "holding": {"holder": "g-leader", "role": "manned"}, "conditions": {"asl:malfunctioned": False}}})
        else:
            game.weapon("mortar", "attacker-light-mortar", "german", "g-leader", kind="light-mortar")
        game.unit("target", "squad", "defender-squad", "russian", "bd03:B6:0")
        game.san("german", 0)
        game.san("russian", 0)
    game.phases("rph", "pfph")
    for event in game.events:
        event["eventId"] = f"{name}-setup-{event['revision']}"
    record = {"schemaVersion": 1, "tenant": game.tenant, "game": name, "label": game.label,
              "synthetic": False, "minigame": "c9-mortar", "events": game.events}
    path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(path)
