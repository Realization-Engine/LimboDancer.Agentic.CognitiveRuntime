"""Pass 70's gate: which keys of the pass's SKIP_JSON already appear in the older games of the proof store. SKIP_JSON strips a key on the gate's
side only (the standing rules, pass 45), so a key an older record already writes makes every such line differ by hash alone. Reads the proof store's
copies under the working folder, never the live folder.

    python skip_json_collisions.py <store tenant folder> <comma-separated keys> <comma-separated games of the pass to leave out, by prefix>
"""
import os
import sys

folder, keys, own = sys.argv[1], sys.argv[2].split(","), sys.argv[3]
counts = {key: 0 for key in keys}
games = {key: 0 for key in keys}
for name in sorted(os.listdir(folder)):
    if not name.endswith(".game.json") or name.startswith(own):
        continue
    text = open(os.path.join(folder, name), encoding="utf-8").read()
    for key in keys:
        n = text.count('"' + key + '":')
        if n:
            counts[key] += n
            games[key] += 1
for key in keys:
    print(f"{key}: {counts[key]} occurrences in {games[key]} older games")
