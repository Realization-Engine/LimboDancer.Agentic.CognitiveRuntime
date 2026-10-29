"""The C9.3 claim's rows (AGENTS.md, "To build"): the two inventory rows, the claims table's state "built", and the C9 coverage row's note.

    python rows.py            dry run
    python rows.py --apply
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402

CALC = "ScenarioA1SpotterCalculator.cs"
ROWS = {
    "C9.3": {
        "Where": (f"{CALC}: CandidateRefusal, InReach, SharesLocation, Retains, HoldsAssignment, KeptRefusal, Supersedes, SpottedFireAllowed, Timing, "
                  "DesignationRefusal, FireCounter, BarsMovement, FireBar, ObservedLoss, RetainsReducedSquad, RetainsDeployedSquad, DeploymentChoiceRefusal, "
                  "DeploymentSuccessorValid; ScenarioA1OrdnanceEligibility.cs: MortarSupport; ScenarioA1OrdnanceCalculator.cs: SupportWeaponOutside, Outside; "
                  "ScenarioA1AreaCalculator.cs: Run (pinned Spotter); LiveOrdnance.cs: MortarSupport; GamePlanner.Ordnance.cs: SupportWeaponMapFacts; "
                  "GameProjector.SpotterDesignation.cs; GameProjector.Spotters.cs; TurnFireHistory.cs; ScenarioA1SpotterContract.cs"),
        "Rules calculator": f"{CALC} (the C9.3 claim, read against the PDF; one calculator, ruling RC93.1)",
        "Test": ("ScenarioA1SpotterDesignationTests; ScenarioA1SpotterRetentionTests; ScenarioA1SpotterTimingTests; MinigameBundleTests (c9-spotter); "
                 "BacklogPass9Tests.ASpottedMortarFiresBeyondItsOwnLosAndKeepsItsSpotter; "
                 "ScenarioA1Pass9Tests.SpottedFireIsDesignatedInThePfphOrDfphAndAPinnedPanzerfaustFirerInABuildingMayNotFire"),
        "Pass or ruling": "Pass 110",
        "Note": ("Built (ruling R9.4): a Good Order Personnel Spotter in the hex or adjacent, its LOS and Hindrance, a pinned Spotter, the SW-use marker, "
                 "the kept Spotter. The C9.3 claim (2026-10-28, rulings RC93.1 to RC93.10): one calculator for who may spot, reach, one Spotter for several mortars in one Location at "
                 "one target, the designation's timing and the wait until the owner's next MPh, retention, loss, and successors; the contract "
                 "c9.3-spotter-designation. Missing: the Opportunity Fire clauses, a hidden Spotter's record, and the replacement command (backlog 80)."),
    },
    "C9.31": {
        "Rules calculator": "ScenarioA1AreaCalculator.cs: Run (spotted, rof); ScenarioA1SpotterCalculator.cs: SharesWith (the C9.3 claim)",
        "Test": "ScenarioA1Pass9Tests.SpottedFireAddsTwoAndLowersTheRofAndAPinnedSpotterAddsCaseD; ScenarioA1SpotterSharingTests",
        "Pass or ruling": "",
        "Note": "Plus 2 and the Multiple ROF one lower. One Spotter's use of several mortars is the C9.3 claim's SharesWith (ruling RC93.5).",
    },
}
COVERAGE = os.path.join(os.path.dirname(inventory_table.PATH), "ASL Rule Coverage.md")
COVERAGE_ROW = "| C9 | Mortars |"
COVERAGE_NOTE = " **The C9.3 claim (2026-10-28):** one Spotter calculator, `ScenarioA1SpotterCalculator`; the minigame c9-spotter."


def main():
    apply = "--apply" in sys.argv
    lines, nl = inventory_table.read_lines()
    rows = {row["§"]: row for row in inventory_table.rows(lines)}
    for rule, changes in ROWS.items():
        row = rows[rule]
        names = [n for n in row if n not in ("chapter", "line")]
        values = inventory_table.cells(lines[row["line"]])
        for column, value in changes.items():
            values[names.index(column)] = value
        values[names.index("Claim state")] = "built"
        lines[row["line"]] = "| " + " | ".join(values) + " |"
        print(rule, "updated")
    at = next(i for i, line in enumerate(lines) if line.startswith("| C9.3 | Claude |") and "claim/c9.3-spotters" in line)
    cells = inventory_table.cells(lines[at])
    cells[5] = "built"
    lines[at] = "| " + " | ".join(cells) + " |"
    print("claims table: built")
    coverage = open(COVERAGE, encoding="utf-8", newline="").read()
    c_nl = "\r\n" if "\r\n" in coverage else "\n"
    c_lines = coverage.split(c_nl)
    c_at = next(i for i, line in enumerate(c_lines) if line.startswith(COVERAGE_ROW))
    # The note goes at the end of the row's notes cell, the one before its passes.
    c_cells = inventory_table.cells(c_lines[c_at].replace(COVERAGE_NOTE, ""))
    if COVERAGE_NOTE.strip() not in c_cells[-2]:
        c_cells[-2] = c_cells[-2].rstrip() + COVERAGE_NOTE
    c_lines[c_at] = "| " + " | ".join(c_cells) + " |"
    print("coverage C9:", c_lines[c_at][-160:])
    if apply:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        open(COVERAGE, "w", encoding="utf-8", newline="").write(c_nl.join(c_lines))
        print("written")


if __name__ == "__main__":
    main()
