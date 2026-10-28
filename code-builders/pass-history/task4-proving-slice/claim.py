"""First task 4: the proving slice's claim on the board (AGENTS.md, "To claim"), written by script into the rule inventory on main.

    python claim.py            dry run
    python claim.py --apply    fills the claims table row and the three rows' Owner and Claim state cells
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402
import claim_check  # noqa: E402

CLAIM = ("| proving-slice | Claude | Codex | task4/proving-slice | ScenarioA1RuleContractModels.cs, ScenarioA1RuleContracts.cs | claimed | 2026-10-28 | "
         "First task 4 of the Rules-first plan (section 8): the wiring contract format and three contracts, the A10.6 rally (every fact gathered), "
         "E10.1 eligibility (facts entered by hand), and A12.14 concealment on advancing (what each viewer may know). The calculators are only read. |")
ROWS = ["A10.6", "E10.1", "A12.14"]


def main():
    lines, nl = inventory_table.read_lines()
    at = lines.index(claim_check.CLAIMS_HEADER)
    if any(line.startswith("| proving-slice |") for line in lines):
        sys.exit("already claimed")
    rows = {row["§"]: row for row in inventory_table.rows(lines)}
    for rule in ROWS:
        row = rows[rule]
        if row["Owner"] or row["Claim state"] != "open":
            sys.exit(f"{rule} is not open: {row['Owner']!r} {row['Claim state']!r}")
        names = [n for n in row if n not in ("chapter", "line")]
        values = inventory_table.cells(lines[row["line"]])
        values[names.index("Owner")] = "proving-slice (Claude)"
        values[names.index("Claim state")] = "claimed"
        lines[row["line"]] = "| " + " | ".join(values) + " |"
    lines.insert(at + 2, CLAIM)
    print("\n".join(lines[at:at + 3]))
    if "--apply" in sys.argv:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        print("written")


if __name__ == "__main__":
    main()
