"""Classify the differing lines of two sweeps: pair each line by its first three or four columns and show where the pair first differs.

Usage: python sweep_pairs.py <before> <after> [examples per class]
"""
import collections
import re
import sys

before = open(sys.argv[1], encoding="utf-8", errors="replace").read().split("\n")
after = open(sys.argv[2], encoding="utf-8", errors="replace").read().split("\n")
examples = int(sys.argv[3]) if len(sys.argv) > 3 else 1
assert len(before) == len(after), (len(before), len(after))
classes = collections.Counter()
shown = collections.Counter()
for old, new in zip(before, after):
    if old == new:
        continue
    at = next((index for index, (a, b) in enumerate(zip(old, new)) if a != b), min(len(old), len(new)))
    kind = "\t".join(old.split("\t")[1:3])[:40]
    old_piece, new_piece = old[max(0, at - 70):at + 110], new[max(0, at - 70):at + 110]
    key = kind + " | " + re.sub(r"[0-9a-f]{12,}", "#", re.sub(r"bd\d+:[A-Z]+\d+:\d", "LOC", new[at:at + 46]))
    classes[key] += 1
    if shown[kind] < examples:
        shown[kind] += 1
        print("EXAMPLE", old.split("\t")[0], kind)
        print("   old:", old_piece.encode("ascii", "replace").decode("ascii"))
        print("   new:", new_piece.encode("ascii", "replace").decode("ascii"))
print("---- classes (count, kind | first differing text on the new side)")
for key, count in classes.most_common(40):
    print(count, key.encode("ascii", "replace").decode("ascii"))
print("total", sum(classes.values()))
