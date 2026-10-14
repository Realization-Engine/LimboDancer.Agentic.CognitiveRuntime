"""Move section 7 of the pass 40 design after section 6 (it was inserted before it)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
start = text.index("## 7. Stacking: 40.6 (increment 2a)")
end = text.index("## 6. The frame's questions")
assert start < end and text.count("## 7. Stacking") == 1 and text.count("## 6. The frame's questions") == 1
section = text[start:end]
rest = text[:start] + text[end:]
if not rest.endswith("\n"):
    rest += "\n"
open(path, "w", encoding="utf-8", newline="").write(rest + "\n" + section.rstrip("\n") + "\n")
print("moved", len(section))
