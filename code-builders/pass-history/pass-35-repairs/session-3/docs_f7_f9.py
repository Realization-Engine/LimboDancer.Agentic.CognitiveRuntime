"""Pass 35 review: rulings R35.2 (fix 7) and R35.9 (finding 9) brought in line. Exact blocks, each asserted once; run once."""
PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog Passes Plan.md"
EDITS = [
    ("the end still shows each surrender and elimination as a consequence to confirm.",
     "the end still shows each surrender and elimination as a consequence to confirm. For the other side's rout routes, though, a unit bound to "
     "surrender is a prisoner from the moment the page has it surrender, and no longer a Known enemy unit that bars a step (A10.51): a Disrupted "
     "unit from the start of the RtPh (A19.12), and an ATTACKER's unit before the DEFENDER routs, as the Comprehensive Rout Example has both "
     "(p. 69); a DEFENDER's unit that is not Disrupted still stands while the ATTACKER routs (added 2026-10-10 after the referee's review)."),
    ("one freed by its Bog Removal is Non-Stopped from then on, since Bog Removal is its Start MP.",
     "one freed by its Bog Removal is Non-Stopped from then on, since Bog Removal is its Start MP. This is a reading: C.8 (p. 162) defines a "
     "Non-Stopped vehicle as \"a moving, Mobile (D.7) vehicle that has not expended a Stop MP\" since its last Start MP, and says that a Bog "
     "Removal attempt does not by itself make a moving target; the reading takes \"moving\" there as having started, by D8.3, and not as a "
     "moving target, which D8.4 denies the vehicle until it leaves its Bog hex. The referee's review of 2026-10-09 found the page silent "
     "between the two; no code was changed for it."),
]
text = open(PATH, encoding="utf-8", newline="").read()
for old, new in EDITS:
    assert text.count(old) == 1, (text.count(old), old[:60])
for old, new in EDITS:
    text = text.replace(old, new)
with open(PATH, "w", encoding="utf-8", newline="") as out:
    out.write(text)
print("ok", len(EDITS))
