"""Stops the gate's baseline stage when a key of the pass's SKIP_JSON already appears in an older game of the proof store.

SKIP_JSON strips a key on the gate's side only (the standing rules, pass 45), so a key an older record already writes makes every such line differ
by hash alone. Pass 70's gate lost two runs to five such keys (captors, ambusher, seenByGoodOrderEnemy, leaderCreation, vehicles), added from the
pass's code without a look at the older records. A key written as "key{}" or "key[{}]" (the tool's shapes) is checked by its bare name.

    python skip_json_collisions.py <store tenant folder> "<SKIP_JSON>" "<the pass's own games, space-separated>"

Prints each colliding key with its counts and exits 1; prints "SKIP_JSON: no key is an older record's" and exits 0 otherwise.
"""
import os
import sys

folder, skip, own = sys.argv[1], sys.argv[2], set(sys.argv[3].split())
keys = [key.strip().removesuffix("[{}]").removesuffix("{}") for key in skip.split(",") if key.strip()]
counts = {key: [0, 0] for key in keys}
for name in sorted(os.listdir(folder)):
    if not name.endswith(".game.json") or name[: -len(".game.json")] in own:
        continue
    text = open(os.path.join(folder, name), encoding="utf-8").read()
    for key in keys:
        n = text.count('"' + key + '":')
        if n:
            counts[key][0] += n
            counts[key][1] += 1
bad = [key for key in keys if counts[key][0]]
for key in bad:
    print(f"SKIP_JSON key '{key}' is an older record's: {counts[key][0]} occurrences in {counts[key][1]} older games; take it out of the .conf")
if bad:
    sys.exit(1)
print("SKIP_JSON: no key is an older record's")
