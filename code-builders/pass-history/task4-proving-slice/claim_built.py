"""First task 4: the proving slice's claim set to "built" in the claims table and its three rows (AGENTS.md, "To build"; the review is Codex's).

    python claim_built.py --apply
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402
import claim_check  # noqa: E402


def main():
    lines, nl = inventory_table.read_lines()
    at = lines.index(claim_check.CLAIMS_HEADER)
    index = next(i for i in range(at + 2, len(lines)) if lines[i].startswith("| proving-slice |"))
    values = inventory_table.cells(lines[index])
    if values[5] != "claimed":
        sys.exit(f"the claim is {values[5]}, not claimed")
    values[5] = "built"
    lines[index] = "| " + " | ".join(values) + " |"
    for row in inventory_table.rows(lines):
        if row["Owner"] == "proving-slice (Claude)":
            cells = inventory_table.cells(lines[row["line"]])
            names = [name for name in row if name not in ("chapter", "line")]
            cells[names.index("Claim state")] = "built"
            lines[row["line"]] = "| " + " | ".join(cells) + " |"
    if "--apply" in sys.argv:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
    print(lines[index])


if __name__ == "__main__":
    main()
