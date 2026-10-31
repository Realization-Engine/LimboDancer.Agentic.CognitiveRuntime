"""Copy a recorded setup prefix into a separate Studio demo, never overwrite a game."""

import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path)
parser.add_argument("destination", type=Path)
parser.add_argument("--game", required=True)
parser.add_argument("--through-revision", type=int, required=True)
parser.add_argument("--rout-demo", action="store_true", help="Copy the Rout prefix with the demo enemy in [A1].")
parser.add_argument("--split-rout-hexes", action="store_true", help="Place R2 in [B2] for a two-hex Rout demo.")
args = parser.parse_args()
if args.split_rout_hexes and not args.rout_demo:
    raise SystemExit("Split Rout hexes requires --rout-demo.")
record = json.loads(args.source.read_text(encoding="utf-8-sig"))
source_name = record["game"]
events = [event for event in record["events"] if event["revision"] <= args.through_revision]
if [event["revision"] for event in events] != list(range(1, args.through_revision + 1)):
    raise SystemExit("The selected prefix must have consecutive revisions starting at one.")
expected_phase = "rtph" if args.rout_demo else "pfph"
if events[-1]["type"] != "phase-changed" or events[-1]["payload"]["phase"] != expected_phase:
    raise SystemExit(f"Choose the setup prefix ending at {expected_phase}; advance normally in Studio.")
if any(event["type"] not in {"game-started", "instance-created", "phase-changed"} for event in events):
    raise SystemExit("The demo prefix must contain only setup and phase events.")
if args.destination.name != args.game + ".game.json":
    raise SystemExit("The destination file must match the new game's name.")
record["game"] = args.game
record["label"] = f"Movement UI demo (setup copied from {source_name})"
if args.rout_demo:
    enemies = [event["payload"]["instance"] for event in events
               if event["type"] == "instance-created" and event["payload"]["instance"]["side"] == "german"]
    if len(enemies) != 1 or enemies[0].get("position", {}).get("at") != "bd03:A2:0":
        raise SystemExit("The Rout demo requires the Movement demo's single German unit in [A2].")
    enemies[0]["position"]["at"] = "bd03:A1:0"
    record["label"] = f"Rout UI demo (setup copied from {source_name}, enemy in [A1])"
    if args.split_rout_hexes:
        squads = [event["payload"]["instance"] for event in events
                  if event["type"] == "instance-created" and event["payload"]["instance"]["id"] == "r-2"]
        if len(squads) != 1 or squads[0].get("position", {}).get("at") != "bd03:A3:0":
            raise SystemExit("Expected demo squad R2 in [A3].")
        squads[0]["position"]["at"] = "bd03:B2:0"
        record["label"] += "; Russian units in [A3] and [B2]"
record["events"] = events
with args.destination.open("x", encoding="utf-8", newline="\n") as output:
    json.dump(record, output, indent=2)
    output.write("\n")
print(f"Created {args.game} with {len(events)} setup events; source unchanged.")
