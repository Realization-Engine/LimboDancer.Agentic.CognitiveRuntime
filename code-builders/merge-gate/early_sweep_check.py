"""The proofs' first check: a few games made before the pass, swept on the gate's side and compared with the baseline's lines for them.

A plan whose answer and words are the same and whose hash alone differs means the skip lists are wrong for this pass: a property missing
from PROOF_SKIP, a JSON key missing from PROOF_SKIP_JSON, or something that changed in every record and belongs in BOTH_SKIP (the
catalog's identity, GameEvent.RulePackage after the packages are pinned again). The whole sweep would differ in every such line, so the
stage stops here, half a minute in. A changed answer or changed words do not stop it: those are read at the end, each as one task's
intended change or a fault.

This is what is left of the gate's trial stage, which the user dropped on 2026-10-13 once the proofs took seven minutes and not fifty.

Usage: python early_sweep_check.py <the baseline's sweep> <the gate's sweep of the few games>
"""
import collections
import sys


def rows(path):
    found = {}
    for line in open(path, encoding="utf-8", errors="replace").read().split("\n"):
        parts = line.split("\t")
        if len(parts) >= 5:
            found[tuple(parts[:4])] = parts[4:]
    return found


before, early = rows(sys.argv[1]), rows(sys.argv[2])
compared, changed = 0, 0
hash_only, example = collections.Counter(), {}
for key, rest in early.items():
    other = before.get(key)
    if other is None:
        continue
    compared += 1
    if rest != other:
        if key[1] == "ACT" and rest[:2] == other[:2]:
            hash_only[key[2]] += 1
            example.setdefault(key[2], f"{key[0]} {key[3][:120]}")
        else:
            changed += 1
print(f"early check: {compared} lines of {len(early)} compared with the baseline's; {changed} with a changed answer or words (read at the end)")
for action, count in sorted(hash_only.items()):
    print(f"  FAULT {count} {action}: hash alone differs, for example {example[action]}")
if not compared:
    print("EARLY CHECK VOID: no line of the few games is in the baseline's sweep")
    sys.exit(1)
if hash_only:
    print("EARLY CHECK FAILED: name the differing fact (code-builders/diagnostics/dump_both_sides.sh) and correct the pass's .conf")
    sys.exit(1)
print("EARLY CHECK PASSED")
