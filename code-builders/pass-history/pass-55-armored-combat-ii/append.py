"""Append a section file to the pass 55 design: append.py <section file>"""
import io
import sys

DESIGN = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 55 Design.md"
with io.open(sys.argv[1], encoding="utf-8", newline="") as f:
    section = f.read().replace("\r\n", "\n")
with io.open(DESIGN, encoding="utf-8", newline="") as f:
    text = f.read()
assert "\r\n" not in text
first = section.strip().splitlines()[0]
assert first not in text, "already appended: " + first
with io.open(DESIGN, "w", encoding="utf-8", newline="") as f:
    f.write(text.rstrip("\n") + "\n\n" + section.strip() + "\n")
print("appended", first)
