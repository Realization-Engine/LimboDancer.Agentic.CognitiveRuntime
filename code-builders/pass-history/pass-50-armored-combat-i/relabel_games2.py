"""Pass 50: the games recorded before the reviews' fixes, each superseded by its copy recorded after them."""
import json
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass45")
from p45lib import LIVE

WHY = " (recorded before the reviews' fixes: a firer name and the Case N of Bounding First Fire changed): "
LABELS = {
    "p50-mg-both": "p50-mg-both2", "p50-mg-turn": "p50-mg-turn2", "p50-mg-first": "p50-mg-first2", "p50-repair": "p50-repair2",
    "p50-bff-outsetb": "p50-bff-outset2", "p50-bff-movec": "p50-bff-move2", "p50-afphc": "p50-afph2", "p50-afph-ctlc": "p50-afph-ctl2",
}
RELABEL = {game: f"SUPERSEDED by {new}{WHY}" for game, new in LABELS.items()}
RELABEL["p50-mg-ctl2"] = "NOT PLAYED (p50-mg-both2 is the control of p50-mg-turn2 in effect): "
for old in ("p50-bff-outset", "p50-bff-outsetc", "p50-bff-move", "p50-bff-moveb", "p50-afph", "p50-afphb", "p50-afph-ctl", "p50-afph-ctlb"):
    pass
for game, prefix in RELABEL.items():
    path = LIVE + game + ".game.json"
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    data = json.loads(text)
    old = data["label"]
    if old.startswith("SUPERSEDED") or old.startswith("NOT PLAYED"):
        print("already", game)
        continue
    marker = '"label": ' + json.dumps(old)
    assert text.count(marker) == 1, game
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text.replace(marker, '"label": ' + json.dumps(prefix + old)))
    print("relabelled", game, len(data["events"]))
