"""Record the user's answers on task 40.2 in the pass 40 design (exact blocks, each asserted once)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ("### 8.1 Task 40.2: the road\n\nBuilt on 2026-10-11 and waiting for the user's word before its commit.",
     "### 8.1 Task 40.2: the road\n\nBuilt on 2026-10-11 and committed at the user's word of the same day, with yes to rulings R40.4, R40.5, and R40.6."),
    ("**Rulings proposed.**", "**Rulings (approved 2026-10-11).**"),
]
for old, new in edits:
    assert text.count(old) == 1, old
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
