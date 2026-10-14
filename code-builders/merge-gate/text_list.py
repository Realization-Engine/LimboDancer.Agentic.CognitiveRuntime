"""P4: every play., asl., UNIT-STATE-, CASE-, and MAP- literal, every roll purpose and PlannedRoll key, with counts,
over Play, Units, Rules, and Maps. Usage: python textlist.py <out.txt>"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import pathlib
import re
import sys

ROOT = pathlib.Path(sys.argv[2] if len(sys.argv) > 2 else REPO + "/src/ASL")
HOLE = re.compile(r"\{[^{}]*\}")


def normalized(literal: str) -> str:
    """An interpolation hole's expression is not a character a player sees: every hole becomes {}."""
    while True:
        replaced = HOLE.sub("{}", literal)
        if replaced == literal:
            return literal
        literal = replaced

PROJECTS = ["LimboDancer.Domains.Asl.Play", "LimboDancer.Domains.Asl.Units", "LimboDancer.Domains.Asl.Rules", "LimboDancer.Domains.Asl.Maps"]
LITERAL = re.compile(r'"((?:play\.|asl\.|UNIT-STATE-|CASE-|MAP-)[^"\n]*)"')
PURPOSE = re.compile(r'new DiceRolled\([^,\n]+,\s*"([^"]+)"')
PLANNED = re.compile(r'new PlannedRoll\("([^"]+)"')

counts = collections.Counter()
for project in PROJECTS:
    for path in sorted((ROOT / project).rglob("*.cs")):
        if any(part in ("bin", "obj") for part in path.parts):
            continue
        text = path.read_text(encoding="utf-8")
        for match in LITERAL.finditer(text):
            counts["literal\t" + normalized(match.group(1))] += 1
        for match in PURPOSE.finditer(text):
            counts["purpose\t" + match.group(1)] += 1
        for match in PLANNED.finditer(text):
            counts["planned\t" + match.group(1)] += 1

out = pathlib.Path(sys.argv[1])
with open(out, "w", encoding="utf-8", newline="\n") as handle:
    for key in sorted(counts):
        handle.write(f"{counts[key]}\t{key}\n")
print(f"{len(counts)} distinct texts, {sum(counts.values())} occurrences -> {out}")
