"""Step 0: record the Turns track's Rules calculators against their inventory rows (A to E; G10 waits for the chapter G rows).

Each row named below gains, in its Rules calculator cell, "Turns <task> (not read against the PDF): <members>". A cell that read
"none" (a rule not built) is replaced; any other cell is extended. No status changes: a status is changed when the rule is read on
its page, by the claim that reconciles the calculator.

    python record_turns.py            dry run
    python record_turns.py --apply    rewrites the inventory
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402

OBLIGATION = ("Turn Guide B5", "TurnObligationRules.cs: Evaluate (the obligation's deadline)")
WORK_E103 = ("Turn Guide B5", "TurnWorkRules.cs: Evaluate, Window, Interrupted, Interval; TurnWorkLifecycle.cs: RequiresAmmunitionDr, PhaseChangeRefusal")
AMMO = ("Turns D4.1", "TurnAmmoEligibilityRules.cs: SelectVehicle, Immobile, SameOrAccessible, Compatible, EvaluateRecipients")
SMOKE = ("Turns D2.1", "TurnWhiteSmokeRules.cs: InitialStrength, Evaluate, Opening")
SPOTTER = ("Turns D1.1 and D1.2", "TurnSpotterTimingRules.cs: Evaluate; TurnSpotterDesignationRules.cs: FireCounter, BarsMovement, Refusal; "
           "TurnSpotterLossRules.cs: FireBar, RetainsReducedSquad, DeploymentChoiceRefusal, DeploymentSuccessorValid, RetainsDeployedSquad, ObservedLoss")

TURNS = {
    "A4.51": OBLIGATION,
    "A4.52": OBLIGATION,
    "D5.34": OBLIGATION,
    "D5.341": OBLIGATION,
    "E1.95": OBLIGATION,
    "E1.951": OBLIGATION,
    "B24.7": OBLIGATION,
    "B24.8": OBLIGATION,
    "A23.3": ("Turn Guide B5", "TurnPlacedDcWindowRules.cs: Valid, Matches, Assess; TurnObligationRules.cs: Evaluate (the obligation's deadline)"),
    "A13.32": ("Turns D3.2", "TurnHorseRules.cs: SetupRefusal, PlayIntegrationRefusal (the Horse and Mule identity)"),
    "A24.4": SMOKE,
    "A24.5": SMOKE,
    "A24.61": SMOKE,
    "A7.352": ("Turns D1.1", "TurnSpotterDesignationRules.cs: Refusal"),
    "C9.3": SPOTTER,
    "C9.31": ("Turns D1.1", "TurnSpotterUseRules.cs: Compatible"),
    "D.7": ("Turns D4.1", "TurnAmmoEligibilityRules.cs: Immobile"),
    "E10.1": AMMO,
    "E10.11": AMMO,
    "E10.2": AMMO,
    "E10.12": ("Turn Guide B5", "TurnWorkEffects.cs: CanRelocateSupply, RelocationSourceReady"),
    "E10.3": WORK_E103,
    "E10.4": ("Turn Guide B5", "TurnWorkEffects.cs: ReplenishmentReady, RelocationSourceReady, TransferDestinationReady"),
}


def main():
    apply = "--apply" in sys.argv
    lines, nl = inventory_table.read_lines()
    rows = {row["§"]: row for row in inventory_table.rows(lines)}
    missing = [rule for rule in TURNS if rule not in rows]
    if missing:
        sys.exit(f"not in the inventory: {missing}")
    for rule, (task, members) in TURNS.items():
        row = rows[rule]
        old = row["Rules calculator"]
        if "Turns " in old or "Turn Guide " in old:
            sys.exit(f"{rule} already records the Turns work")
        added = f"Turns work, {task} (not read against the PDF): {members}"
        new = added if old in ("none", "") else f"{old}; {added}"
        values = inventory_table.cells(lines[row["line"]])
        names = list(row.keys())[: len(values)]
        values[names.index("Rules calculator")] = new
        lines[row["line"]] = "| " + " | ".join(values) + " |"
        print(f"{rule} [{row['Status']}]: {new[:170]}")
    if apply:
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        print("written", len(TURNS))
    else:
        print("dry run", len(TURNS))


if __name__ == "__main__":
    main()
