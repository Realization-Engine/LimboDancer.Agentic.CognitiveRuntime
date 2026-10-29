"""Adds a Rules file to the C9.3 claim's row of the claims table (AGENTS.md, "Held files": a file a claim turns out to need is added by a commit
to main first).

    python add_file.py <File.cs>            dry run
    python add_file.py <File.cs> --apply    writes the inventory
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402


def main():
    name = sys.argv[1]
    lines, nl = inventory_table.read_lines()
    at = next(i for i, line in enumerate(lines) if line.startswith("| C9.3 | Claude |") and "claim/c9.3-spotters" in line)
    cells = inventory_table.cells(lines[at])
    files = [item.strip() for item in cells[4].split(",") if item.strip()]
    if name in files:
        sys.exit(f"{name} is already held by the claim")
    cells[4] = ", ".join([*files, name])
    lines[at] = "| " + " | ".join(cells) + " |"
    print(lines[at][:400])
    if "--apply" in sys.argv:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        print("written")


if __name__ == "__main__":
    main()
