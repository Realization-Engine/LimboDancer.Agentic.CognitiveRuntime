"""Record the user's word on task 40.8 in the pass 40 design (exact block, asserted once)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
old = "### 8.3 Task 40.8: Hazardous Movement in every fire phase\n\nBuilt on 2026-10-11 and waiting for the user's word before its commit."
new = "### 8.3 Task 40.8: Hazardous Movement in every fire phase\n\nBuilt on 2026-10-11 and committed at the user's word of the same day. It ends increment 2b."
assert text.count(old) == 1
open(path, "w", encoding="utf-8", newline="").write(text.replace(old, new))
print("ok")
