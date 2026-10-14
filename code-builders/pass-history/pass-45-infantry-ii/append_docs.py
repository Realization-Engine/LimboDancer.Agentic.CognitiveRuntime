"""Append a section file to the pass 45 design and a row file to the time log. Usage: python append_docs.py <section.md or -> <row.txt or ->"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
TARGETS = [(sys.argv[1], DOCS + "Passes/ASL Unit Backlog Pass 45 Design.md", "\n"), (sys.argv[2], DOCS + "ASL Unit Time Log.md", "")]
for source, target, gap in TARGETS:
    if source == "-":
        continue
    with open(source, encoding="utf-8") as f:
        addition = f.read().strip("\n")
    if "\u2014" in addition:
        sys.exit("an em-dash in " + source)
    with open(target, encoding="utf-8", newline="") as f:
        text = f.read()
    if addition[:60] in text:
        sys.exit("already appended: " + source)
    with open(target, "w", encoding="utf-8", newline="") as f:
        f.write(text.rstrip("\n") + "\n" + gap + addition + "\n")
    print("appended", source)
