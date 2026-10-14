"""Record the user's answers on increment 2a in the pass 40 design (exact blocks, each asserted once)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ("Built on 2026-10-11 and waiting for the user's word before its commit.",
     "Built on 2026-10-11 and committed at the user's word of the same day, with yes to the two rulings, to the reading of A5.13, and to moving A5.132 to pass 50."),
    ("- **R40.2 (proposed; amends R26.3 and R8.10):**", "- **R40.2 (approved 2026-10-11; amends R26.3 and R8.10):**"),
    ("- **R40.3 (proposed; carries out R11.6):**", "- **R40.3 (approved 2026-10-11; carries out R11.6):**"),
    ("- **A5.13, as read:**", "- **A5.13, as read (approved 2026-10-11):**"),
    ("### 7.5 A5.132: a proposal to move it to pass 50", "### 7.5 A5.132: moved to pass 50 (the user, 2026-10-11)"),
    ("Increment 2a (stacking, 40.6) is next; nothing is built yet.",
     "Increment 2a (stacking, 40.6) is built and committed (section 7). Increment 2b (roads, Dash, Hazardous Movement) is next."),
]
for old, new in edits:
    assert text.count(old) == 1, old
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
