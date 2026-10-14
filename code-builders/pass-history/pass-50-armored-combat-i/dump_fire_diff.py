"""Pass 50, the gate: the first fire plan whose full dump differs between dbg50/main.txt and dbg50/gate.txt, with the text around each difference."""
import difflib

S = "E:/Archive/GitHub/dlandi/pass32-tools/dbg50/"
a = open(S + "main.txt", encoding="utf-8", errors="replace").read().split("\n")
b = open(S + "gate.txt", encoding="utf-8", errors="replace").read().split("\n")
print(len(a), len(b), "lines")
shown = 0
for x, y in zip(a, b):
    if x == y:
        continue
    for tag, i1, i2, j1, j2 in difflib.SequenceMatcher(None, x, y, autojunk=False).get_opcodes():
        if tag != "equal":
            print(tag, "| main:", repr(x[max(0, i1 - 80):i2 + 40]), "| gate:", repr(y[max(0, j1 - 80):j2 + 40]))
    shown += 1
    if shown == 3:
        break
print("differing lines:", sum(1 for x, y in zip(a, b) if x != y))
