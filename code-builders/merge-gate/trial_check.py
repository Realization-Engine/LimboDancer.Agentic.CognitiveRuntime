"""The gate's trial: a few games replayed and swept on both sides with the gate's skip lists. Exit 1 unless they agree.

The replay digests of the games made before the pass must be equal line for line. The pass's own games (the second argument,
comma-separated) may differ and must only be written by both sides: they are in the trial so that a game the baseline cannot read stops
the trial and not a long run (pass 55's gate lost a 53 minute sweep to one). An ERROR line on either side fails the trial.

In the sweep, a line whose answer and text are equal and whose hash alone differs means a fact of the pass is still in the hashed plan (a
property missing from PROOF_SKIP, a JSON key missing from PROOF_SKIP_JSON, or something that changed in every record and belongs in
BOTH_SKIP, such as GameEvent.RulePackage after the packages are pinned again), or a skip that the baseline also holds: the long runs would
differ in every such line. A changed answer or text is listed and does not fail the trial; it is read at the gate as one task's intended
change or a fault.

Usage: python trial_check.py <trial folder> [own games]   (the folder holds replay-main.txt, replay-gate.txt, sweep-main.txt, sweep-gate.txt)
"""
import collections
import sys

folder = sys.argv[1].rstrip("/") + "/"
own = set(sys.argv[2].split(",")) if len(sys.argv) > 2 and sys.argv[2] else set()


def lines(name):
    return [line for line in open(folder + name, encoding="utf-8", errors="replace").read().split("\n") if line]


def by_game(name):
    games = collections.defaultdict(list)
    for line in lines(name):
        games[line.split("\t", 1)[0]].append(line)
    return games


def rows(name):
    found = {}
    for line in lines(name):
        parts = line.split("\t")
        if len(parts) >= 5:
            found[tuple(parts[:4])] = parts[4:]
    return found


errors = [f"{name}: {line[:240]}" for name in ("replay-main.txt", "replay-gate.txt", "sweep-main.txt", "sweep-gate.txt")
          for line in lines(name) if line.split("\t")[1:2] == ["ERROR"]]
replay_main, replay_gate = by_game("replay-main.txt"), by_game("replay-gate.txt")
replay_differs, own_differs = 0, 0
for game in sorted(set(replay_main) | set(replay_gate)):
    a, b = replay_main.get(game, []), replay_gate.get(game, [])
    differing = sum(1 for x, y in zip(a, b) if x != y) + abs(len(a) - len(b))
    if game in own:
        own_differs += 1 if differing else 0
    else:
        replay_differs += differing
missing = sorted(game for game in own if game not in replay_main or game not in replay_gate)
before, after = rows("sweep-main.txt"), rows("sweep-gate.txt")
hash_only, changed, example = collections.Counter(), collections.Counter(), {}
for key, rest in before.items():
    other = after.get(key)
    if other is None:
        changed[(key[2], "missing on the gate's side")] += 1
    elif rest != other:
        if key[1] == "ACT" and rest[:2] == other[:2]:
            hash_only[key[2]] += 1
            example.setdefault(key[2], f"{key[0]} {key[3][:120]}")
        else:
            changed[(key[2], "answer or text differs" if key[1] == "ACT" else "read differs")] += 1
new_lines = sum(1 for key in after if key not in before)
lines_main = sum(len(item) for item in replay_main.values())
print(f"replay: {lines_main} lines in {len(replay_main)} games on main; {replay_differs} differ in the games made before the pass; "
      f"{own_differs} of the pass's {len(own)} games differ")
print(f"sweep: {len(before)} lines on main, {len(after)} on the gate's side, {new_lines} of them new there (the pass's own games)")
for (action, how), count in sorted(changed.items()):
    print(f"  {count} {action}: {how} (read these at the gate)")
for action, count in sorted(hash_only.items()):
    print(f"  FAULT {count} {action}: hash alone differs, for example {example[action]}")
for line in errors[:10]:
    print("  ERROR LINE", line)
if missing:
    print("  NOT WRITTEN by a side: " + ", ".join(missing))
if not before or not after or lines_main < 2:
    print("TRIAL VOID: a side wrote nothing")
    sys.exit(1)
if errors or missing:
    print("TRIAL FAILED: a side stopped on a game; correct the tool or the pass's .conf before the long runs")
    sys.exit(1)
if replay_differs or hash_only:
    print("TRIAL FAILED: name the differing fact (PROOF_DUMP_EVENTS, PROOF_DUMP_FIRE, PROOF_DUMP_STATE) and correct the pass's .conf before the long runs")
    sys.exit(1)
print("TRIAL PASSED")
