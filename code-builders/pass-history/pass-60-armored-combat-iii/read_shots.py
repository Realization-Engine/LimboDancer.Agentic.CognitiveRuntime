"""Print the played events of a live test game after its setup: read_shots.py <game> [more games]. For an ordnance or fire record, the
facts and resolution keys a pass checks; for other events, the payload in short."""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
SETUP = {"game-started", "instance-created", "phase-changed", "conditions-changed"}


def short(value, limit=600):
    text = json.dumps(value, separators=(",", ":"))
    return text if len(text) <= limit else text[:limit] + "..."


for game in sys.argv[1:]:
    with open(LIVE + game + ".game.json", encoding="utf-8") as source:
        events = json.load(source)["events"]
    played = [e for e in events if e["type"] not in SETUP or e["revision"] > 1 and events[e["revision"] - 2]["type"] not in SETUP]
    print(f"== {game}: {len(events)} events")
    for e in events:
        if e["type"] in ("game-started", "instance-created"):
            continue
        p = e["payload"]
        if e["type"] == "ordnance-fired":
            r = p.get("resolution", {})
            print(f"  {e['revision']} ordnance-fired facts.ammunition={p['facts'].get('ammunition')} rolls={p.get('rolls')}")
            print("    resolution:", short({k: v for k, v in r.items() if k not in ("toHit",)}, 1500))
        elif e["type"] == "fire-resolved":
            print(f"  {e['revision']} fire-resolved", short(p.get("resolution", p), 1200))
        else:
            print(f"  {e['revision']} {e['type']}", short(p, 300))
