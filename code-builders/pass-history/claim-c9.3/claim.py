"""The first claim on the open board (AGENTS.md, "To claim"): C9.3 Spotters, written by script into the rule inventory on main.

    python claim.py            dry run
    python claim.py --apply    fills the claims table row and the two rows' Owner and Claim state cells

The contract registry, ScenarioA1RuleContracts.cs, was held by the proving-slice claim; every new contract is listed in it, so this claim takes it
over and makes each contract its own file (the claim's design, ruling RC93.1), after which no claim needs to hold the registry.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402
import claim_check  # noqa: E402

FILES = ("ScenarioA1SpotterCalculator.cs, ScenarioA1SpotterContract.cs, ScenarioA1RuleContracts.cs, ScenarioA1OrdnanceEligibility.cs, "
         "ScenarioA1OrdnanceCalculator.cs, ScenarioA1OrdnanceRuleModels.cs, TurnSpotterTimingRules.cs, TurnSpotterDesignationRules.cs, "
         "TurnSpotterLossRules.cs, TurnSpotterUseRules.cs")
CLAIM = (f"| C9.3 | Claude | sub-agent | claim/c9.3-spotters | {FILES} | claimed | 2026-10-28 | "
         "The first claim on the open board (plan question 5): C9.3 is decided today by the ScenarioA1 ordnance calculators and the four Turns "
         "spotter rules, and by checks in Play, Units, ASL.Turns, and the Play page; the claim makes them one calculator. It takes the contract "
         "registry from proving-slice so that later contracts need only their own file. |")
ROWS = ["C9.3", "C9.31"]
SLICE_OLD = "| proving-slice | Claude | Codex | task4/proving-slice | ScenarioA1RuleContractModels.cs, ScenarioA1RuleContracts.cs |"
SLICE_NEW = "| proving-slice | Claude | Codex | task4/proving-slice | ScenarioA1RuleContractModels.cs |"


def main():
    lines, nl = inventory_table.read_lines()
    at = lines.index(claim_check.CLAIMS_HEADER)
    if any(line.startswith("| C9.3 |") and "claim/c9.3-spotters" in line for line in lines):
        sys.exit("already claimed")
    slice_at = next(i for i, line in enumerate(lines) if line.startswith(SLICE_OLD))
    lines[slice_at] = lines[slice_at].replace(SLICE_OLD, SLICE_NEW, 1)
    rows = {row["§"]: row for row in inventory_table.rows(lines)}
    for rule in ROWS:
        row = rows[rule]
        if row["Owner"] or row["Claim state"] != "open":
            sys.exit(f"{rule} is not open: {row['Owner']!r} {row['Claim state']!r}")
        names = [n for n in row if n not in ("chapter", "line")]
        values = inventory_table.cells(lines[row["line"]])
        values[names.index("Owner")] = "C9.3 (Claude)"
        values[names.index("Claim state")] = "claimed"
        lines[row["line"]] = "| " + " | ".join(values) + " |"
    end = at + 2
    while end < len(lines) and lines[end].startswith("|"):
        end += 1
    lines.insert(end, CLAIM)
    print("\n".join(line[:160] for line in lines[at:end + 1]))
    if "--apply" in sys.argv:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        print("written")


if __name__ == "__main__":
    main()
