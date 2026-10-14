"""Pass 50: the labels of the games that are false starts or superseded, so that a reader of the live folder is not misled."""
import json
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass45")
from p45lib import LIVE

LABELS = {
    "p50-bff-outset": "SUPERSEDED by p50-bff-outsetb (recorded before the Delay MP left the tank stopped): ",
    "p50-bff-outsetc": "SUPERSEDED by p50-bff-outsetb (a copy written and never played): ",
    "p50-bff-move": "SUPERSEDED by p50-bff-movec (a copy written and never played; its target stood in [I10], which [D10] does not see): ",
    "p50-bff-moveb": "SUPERSEDED by p50-bff-movec (its target stood in [I10], which [D10] does not see: every shot was refused): ",
    "p50-afph": "SUPERSEDED by p50-afphc (a copy written and never played): ",
    "p50-afphb": "SUPERSEDED by p50-afphc (a copy written and never played): ",
    "p50-afph-ctl": "SUPERSEDED by p50-afph-ctlc (a copy written and never played): ",
    "p50-afph-ctlb": "SUPERSEDED by p50-afph-ctlc (a copy written and never played): ",
    "p50-mg-ctl": "NOT PLAYED (p50-mg-both is the control of p50-mg-turn in effect): ",
    "p50-repair-ctl": "NOT PLAYED in the build; read in the Studio check: ",
}
for game, prefix in LABELS.items():
    path = LIVE + game + ".game.json"
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    data = json.loads(text)
    old = data["label"]
    if old.startswith(prefix):
        print("already", game)
        continue
    marker = '"label": ' + json.dumps(old)
    assert text.count(marker) == 1, game
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text.replace(marker, '"label": ' + json.dumps(prefix + old)))
    print("relabelled", game, len(data["events"]))
