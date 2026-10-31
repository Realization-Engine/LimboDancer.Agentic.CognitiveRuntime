"""Copy a recorded setup prefix into a separate Studio demo, never overwrite a game."""

import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path)
parser.add_argument("destination", type=Path)
parser.add_argument("--game", required=True)
parser.add_argument("--through-revision", type=int, required=True)
args = parser.parse_args()
record = json.loads(args.source.read_text(encoding="utf-8-sig"))
source_name = record["game"]
events = [event for event in record["events"] if event["revision"] <= args.through_revision]
if [event["revision"] for event in events] != list(range(1, args.through_revision + 1)):
    raise SystemExit("The selected prefix must have consecutive revisions starting at one.")
if events[-1]["type"] != "phase-changed" or events[-1]["payload"]["phase"] != "pfph":
    raise SystemExit("Choose the setup prefix ending at Prep Fire; advance normally in Studio.")
if any(event["type"] not in {"game-started", "instance-created", "phase-changed"} for event in events):
    raise SystemExit("The demo prefix must contain only setup and phase events.")
if args.destination.name != args.game + ".game.json":
    raise SystemExit("The destination file must match the new game's name.")
record["game"] = args.game
record["label"] = f"Movement UI demo (setup copied from {source_name})"
record["events"] = events
with args.destination.open("x", encoding="utf-8", newline="\n") as output:
    json.dump(record, output, indent=2)
    output.write("\n")
print(f"Created {args.game} with {len(events)} setup events; source unchanged.")
