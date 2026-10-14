"""Pass 45, the table player's finding: the labels of the games that are false starts, superseded, or not what their label says."""
import json

from p45lib import LIVE

LABELS = {
    "p45-fl-lane": "SUPERSEDED by p45-fl-lane4 (a false start): ",
    "p45-fl-lane2": "SUPERSEDED by p45-fl-lane4 (a false start): ",
    "p45-fl-lane3": "SUPERSEDED by p45-fl-lane4 (DR 11 malfunctioned the MMG, so no Fire Lane was placed): ",
    "p45-fg-bore": "SUPERSEDED by p45-fg-bore3 (DR 11 malfunctioned the MMG): ",
    "p45-fg-unsighted": "SUPERSEDED by p45-fg-unsighted2 (DR 11 malfunctioned the MMG): ",
    "p45-fg-bore2": "CONTROL (the MMG fired with its squad's FP, so no Bore Sighting DRM): ",
    "p45-up-open": "NOT A CONTROL (ground level is still held, so the unit is still Encircled): ",
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
    print("relabelled", game)
