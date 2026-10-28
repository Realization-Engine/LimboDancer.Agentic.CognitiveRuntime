"""Step 0: add the claim board's Owner, Claim state, and Minigames columns to the rule inventory (the plan's sections 4.2 and 7.1).

They go after "Pass or ruling". Every row that carries a rule starts "open" with no owner: no row has yet been through a claim
(section 4.3: contract, minigame bundle, tests, review), whatever its Status says of the code. A row not applicable has none.
Minigames stays empty until the bundle registry writes it.

    python add_claim_columns.py            dry run
    python add_claim_columns.py --apply    rewrites the inventory
"""
import collections
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402

NEW = ["Owner", "Claim state", "Minigames"]
AFTER = "Pass or ruling"


def main():
    apply = "--apply" in sys.argv
    lines, nl = inventory_table.read_lines()
    rows = inventory_table.rows(lines)
    states = collections.Counter()
    headers = 0
    for index, line in enumerate(lines):
        if line.startswith(inventory_table.HEADER_START):
            names = inventory_table.cells(line)
            if "Owner" in names:
                sys.exit("the inventory already has the claim columns")
            at = names.index(AFTER) + 1
            lines[index] = "| " + " | ".join(names[:at] + NEW + names[at:]) + " |"
            ruler = inventory_table.cells(lines[index + 1])
            lines[index + 1] = "|" + "|".join(ruler[:at] + ["---"] * len(NEW) + ruler[at:]) + "|"
            headers += 1
    for row in rows:
        values = inventory_table.cells(lines[row["line"]])
        names = [name for name in row if name not in ("chapter", "line")]
        at = names.index(AFTER) + 1
        if len(values) != len(names):
            sys.exit(f"{row['§']}: the row has {len(values)} cells")
        state = "" if row["Status"] == "not applicable" else "open"
        states[state or "(none)"] += 1
        values = values[:at] + ["", state, ""] + values[at:]
        lines[row["line"]] = "| " + " | ".join(values) + " |"
    print("tables:", headers, dict(states))
    if apply:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        print("written")
    else:
        print("dry run")


if __name__ == "__main__":
    main()
