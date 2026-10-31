"""Record PR85's completed wiring gate without changing another claim's rows."""
import os
from pathlib import Path

repository = Path(__file__).resolve().parents[3]
gate = Path(os.environ["ASL_GATE_WORK"])
assert "DONE proofs" in (gate / "gate-pr85-proofs.log").read_text(encoding="utf-8")
assert not (gate / "after-wire-fire-2/sweep-pairs.txt").read_text(encoding="utf-8").strip()
inventory = repository / "src/ASL/docs/ASL Rule Inventory A to E.md"
lines = inventory.read_text(encoding="utf-8").splitlines()
headers = None
changed = 0
evidence = ("W-C9 wired at 68908b2e against main ba97f7d3, 2026-10-31: "
            "56,550 planner lines unchanged; village replays 11/11 and wire-c9-pair-review 12/12. "
            "Final alternating allocation pairs pass the original bounds. "
            "Design section 10 records the Workbench, proof and compatibility evidence. Claude's review remains pending.")
for index, line in enumerate(lines):
    if not line.startswith("|"):
        headers = None
        continue
    cells = [cell.strip() for cell in line.strip("|").split("|")]
    if cells[0] in ("§", "Claim", "Wiring"):
        headers = cells
        continue
    if headers is None or len(cells) != len(headers) or cells[0].startswith("---"):
        continue
    row = dict(zip(headers, cells))
    original = row.copy()
    if row.get("Owner") in ("C9 (Claude)", "C9.2 (Codex)") and "Claim state" in row:
        row["Claim state"] = "wired"
        if row.get("§") == "C9.2":
            tests = ("BacklogPass9Tests.TwoSmc*; "
                     "BacklogPass9Tests.ParticipantOpportunityDeclarationAgreesBetweenWorkbenchAndPlay; "
                     "BacklogPass9Tests.TurningAnNtGunWithNoRofMarksItIntensiveFiredAndBarsAnotherShot; "
                     "BacklogPass55Tests.TwoSmcCanisterUsesTheSameParticipantsAndRecordsBothFirers")
            if tests not in row["Test"]:
                row["Test"] += "; " + tests
            row["Note"] = row["Note"].replace("Callers and both firers' effects remain W-C9;",
                                             "W-C9 now gathers and verifies both firers and applies their effects;")
    if row.get("Claim") in ("C9", "C9.2", "wire-fire-2") and "Branch" in row:
        row["State"] = "wired"
        row["Note"] = row["Note"].replace("the wiring remains claimed pending its own gate and review.",
                                         "the wiring gate is complete and its review remains pending.")
        row["Note"] = row["Note"].replace("Callers, Workbench inspection and replay proof remain wire-fire-2.",
                                         "Caller, Workbench and replay evidence is recorded by wire-fire-2.")
        if evidence not in row["Note"]:
            row["Note"] += " " + evidence
    if row.get("Wiring") == "W-C9":
        row["State"] = "wired"
    if row != original:
        lines[index] = "| " + " | ".join(row[name] for name in headers) + " |"
        changed += 1
inventory.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
print(f"Updated {changed} C9 board rows; no other claim selected.")
for name in ("baseline-1.json", "current-1.json", "current-2.json", "baseline-2.json"):
    result = Path(__file__).with_name(name)
    result.write_bytes(result.read_bytes().replace(b"\r\n", b"\n"))
