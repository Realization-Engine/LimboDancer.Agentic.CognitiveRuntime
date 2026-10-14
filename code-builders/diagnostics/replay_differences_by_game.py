"""Pass 35: read the difference between two replay digests of the proof store.

    python replay_compare.py <before/replay.txt> <after/replay.txt> <store folder>

For each game whose digest differs: the first revision whose state digest differs, the type and a short payload of the
event at that revision, how many states differ, and the diagnostics line of each side. Games of a cut family (fx-...-rN)
are grouped under their base game, since a cut repeats its base's states.
"""
import collections
import json
import os
import re
import sys

before_path, after_path, store = sys.argv[1], sys.argv[2], sys.argv[3]


def read(path):
    states, diag = collections.defaultdict(dict), {}
    for line in open(path, encoding="utf-8").read().splitlines():
        parts = line.split("\t")
        if len(parts) >= 2 and parts[1] == "D":
            diag[parts[0]] = "\t".join(parts[2:])
        elif len(parts) == 3:
            states[parts[0]][int(parts[1])] = parts[2]
    return states, diag


b_states, b_diag = read(before_path)
a_states, a_diag = read(after_path)
games = sorted(set(b_states) | set(a_states) | set(b_diag) | set(a_diag))
print(f"{len(games)} games; before {sum(len(v) for v in b_states.values())} states, after {sum(len(v) for v in a_states.values())} states")
print("only before:", [g for g in games if g not in a_states and g not in a_diag][:20])
print("only after:", [g for g in games if g not in b_states and g not in b_diag][:20])

folder = next(os.path.join(store, d) for d in os.listdir(store))


def event(game, revision):
    path = os.path.join(folder, game + ".game.json")
    if not os.path.exists(path):
        return "?", ""
    events = json.load(open(path, encoding="utf-8"))["events"]
    if revision > len(events):
        return "past the end", ""
    item = events[revision - 1]
    return item["type"], json.dumps(item["payload"])[:300]


families = collections.defaultdict(list)
for game in games:
    b, a = b_states.get(game, {}), a_states.get(game, {})
    revisions = sorted(set(b) | set(a))
    differing = [r for r in revisions if b.get(r) != a.get(r)]
    diag_differs = b_diag.get(game) != a_diag.get(game)
    if differing or diag_differs:
        base = re.sub(r"-r\d+$", "", game)
        families[base].append((game, differing, diag_differs))

print(f"{sum(len(v) for v in families.values())} games differ, in {len(families)} families")
for base, members in sorted(families.items()):
    firsts = sorted({d[0] for _, d, _ in members if d})
    print(f"\n== {base}: {len(members)} game(s); first differing revisions {firsts[:12]}")
    game, differing, diag_differs = min(members, key=lambda m: (len(m[0]), m[0]))
    shown = 0
    last = None
    for revision in differing:
        kind, payload = event(game, revision)
        if last is None or revision != last + 1:
            if shown < 4:
                print(f"   {game} r{revision}: {kind} {payload}")
                shown += 1
        last = revision
    print(f"   {game}: {len(differing)} of {len(set(b_states.get(game, {})) | set(a_states.get(game, {})))} states differ")
    if diag_differs:
        print(f"   diagnostics before: {b_diag.get(game, '')[:400]}")
        print(f"   diagnostics after:  {a_diag.get(game, '')[:400]}")
