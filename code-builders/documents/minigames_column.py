"""The rule inventory's Minigames column, written from the minigame bundles' manifests (the Rules-first plan, section 7.1: the manifest is the one source
of the link between a rule and its minigames; the column is written from it by script, never by hand).

    python minigames_column.py            rewrites the column
    python minigames_column.py --check    lists every row whose cell differs from the manifests and exits 1 if any does

Each row's cell is the ids of the bundles whose manifest names the rule, comma-separated in id order; empty when none does. A rule a manifest
names that is not a row of the inventory is a fault.
"""
import glob
import json
import os as _os
import sys
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402

BUNDLES = REPO + "/src/ASL/units/minigames"


def wanted():
    by_rule = {}
    for path in sorted(glob.glob(BUNDLES + "/*/*.minigame.json")):
        with open(path, encoding="utf-8") as source:
            manifest = json.load(source)
        for rule in manifest["rules"]:
            by_rule.setdefault(rule, []).append(manifest["id"])
    return {rule: ", ".join(sorted(ids)) for rule, ids in by_rule.items()}


def main():
    check = "--check" in sys.argv
    lines, nl = inventory_table.read_lines()
    rows = inventory_table.rows(lines)
    want = wanted()
    faults = [f"a manifest names {rule}, which is not a row of the inventory" for rule in sorted(set(want) - {row["§"] for row in rows})]
    differ = 0
    for row in rows:
        cell = want.get(row["§"], "")
        if row["Minigames"] == cell:
            continue
        differ += 1
        print(f"{row['§']}: {row['Minigames']!r} -> {cell!r}")
        if not check:
            values = inventory_table.cells(lines[row["line"]])
            names = [name for name in row if name not in ("chapter", "line")]
            values[names.index("Minigames")] = cell
            lines[row["line"]] = "| " + " | ".join(values) + " |"
    for fault in faults:
        print("FAULT:", fault)
    print(f"rules with minigames: {len(want)}; rows that differ: {differ}; faults: {len(faults)}")
    if check:
        sys.exit(1 if differ or faults else 0)
    if faults:
        sys.exit(1)
    open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))


if __name__ == "__main__":
    main()
