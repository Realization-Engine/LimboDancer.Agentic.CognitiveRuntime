"""Record the user's answers on task 40.3 in the pass 40 design (exact blocks, each asserted once)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ("### 8.2 Task 40.3: Dash\n\nBuilt on 2026-10-11 and waiting for the user's word before its commit.",
     "### 8.2 Task 40.3: Dash\n\nBuilt on 2026-10-11 and committed at the user's word of the same day, with yes to rulings R40.7, R40.8, and R40.9."),
    ("**Rulings proposed.**\n\n- **R40.7:**", "**Rulings (approved 2026-10-11).**\n\n- **R40.7:**"),
]
for old, new in edits:
    assert text.count(old) == 1, old
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
