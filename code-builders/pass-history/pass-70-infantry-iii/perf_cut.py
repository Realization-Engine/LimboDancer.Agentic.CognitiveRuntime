"""Copies a live game, cut at a revision, under a new id, for the pass's Studio measurement (design, section 7).

    python perf_cut.py <source game id> <revision> <new game id>

The source game is never written. The copy is synthetic and labelled as a measurement copy. Pass 65's script of the same name, with
pass 70's label.
"""
import json
import sys
from pathlib import Path

LIVE = Path(__file__).resolve().parents[3] / "src" / "ASL" / "boards" / "units" / "live" / "5a7d1f000000400080000000000057d0"

source, revision, target = sys.argv[1], int(sys.argv[2]), sys.argv[3]
game = json.loads((LIVE / f"{source}.game.json").read_text(encoding="utf-8"))
game["game"] = target
game["label"] = f"{target}: {source} cut at revision {revision} (pass 70 measurement copy)"
game["synthetic"] = True
game["events"] = [event for event in game["events"] if event["revision"] <= revision]
(LIVE / f"{target}.game.json").write_text(json.dumps(game, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
print(target, len(game["events"]), "events, last revision", game["events"][-1]["revision"])
