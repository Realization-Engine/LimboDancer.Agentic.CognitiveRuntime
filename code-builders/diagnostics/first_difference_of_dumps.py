"""Where two full dumps first differ, with context: dump_diff.py <a> <b>"""
import io
import sys

a = io.open(sys.argv[1], encoding="utf-8").read()
b = io.open(sys.argv[2], encoding="utf-8").read()
if a == b:
    print("equal")
    sys.exit(0)
shown = 0
i = 0
n = min(len(a), len(b))
while i < n and shown < 6:
    if a[i] != b[i]:
        print("at %d:\n  base: ...%s\n  now:  ...%s" % (i, a[max(0, i - 160):i + 120].replace("\n", " "), b[max(0, i - 160):i + 120].replace("\n", " ")))
        shown += 1
        break
    i += 1
print("lengths", len(a), len(b))
