"""Pass 60 documents: the redesign plan's pass 60 entry gets its status line, pass 50's entry a sentence for task 50.5, and each later pass that
receives a moved item a "Moved here by pass 60" paragraph. Every anchor is counted before the file is written. LF, UTF-8, no BOM."""
import io

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md"
text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("\ufeff") and "Moved here by pass 60" not in text
lines = text.split("\n")

ENTRY = "**Pass 60: Armored combat III: other attacks on vehicles and Guns** (new; 37 rows of the inventory). Counters: none. Needs first: 50; 45 (Residual FP from ordnance)."
STATUS = (" **Status, 2026-10-15: built to the merge gate on `feature/asl-backlog-pass-60` (cut from main at 380b4a7b); the gate not run.** "
          "The six tasks are built, and task 50.5 (MG fire against armor) was carried in from pass 50 and built with 60.6, in six commits and one of the reviews' fixes (3bf7ec8e). "
          "The inventory named 49 rows, not 37: 14 are built and 35 moved to the pass that takes what is left. "
          "The frame's estimates, made from the code, with the plan's in brackets: 60.4 with C11.52 and C13.23, 0:50 (1:20); 60.1, 1:00 (0:40); 60.2, 1:10 (0:40); 60.3, 1:20 (0:40); 60.5, 1:00 (1:00); 60.6 with 50.5, 1:20 (0:20, and 0:40 for 50.5 in pass 50). "
          "That is 6:40 of building against 4:40, and 7:55 in all against 5:55. Rulings R60.1 to R60.27 are open to a veto. "
          "What the pass left out went to pass 65 (Passengers and Riders under the Specific Collateral Attack, more vehicles in a Location than the column's KIA#, the Immobilization Task Check after a mortar's hit), "
          "pass 100 (a Critical Hit's HD clauses and hexside TEM, Bypass), pass 105 (the upper superstructure of a non-turreted AFV, an OVR's A9.74 dr, an unarmored Target Facing), "
          "pass 110 (A5.132, a MG's To Kill attack in the AFPh and its Residual FP, the rest of the PF, an unattended Gun on the Area Target Type, friendly units in a mortar's hex), "
          "pass 140 (Interior Building Hexes and rooftops), pass 150 (the OBA share of the rules built for mortars), and passes 75, 120, and 160, "
          "each with its reason in the [design](<Passes/ASL Unit Backlog Pass 60 Design.md>), sections 2, 7.1 to 12.1, and 13, the [review](<Passes/Scenario A1 Backlog Pass 60 Review 2026-10-15.md>), and the backlog's section 60.")
P50 = "and 50.5 (MG fire against armor) to pass 60, each with its reason in the [design](<Passes/ASL Unit Backlog Pass 50 Design.md>), section 10.1, and the backlog's section 58."
P50_NEW = P50 + " **Task 50.5 was built in pass 60 (2026-10-15), with task 60.6.**"

M = "Moved here by pass 60 (2026-10-15), with no task renumbered: "
MOVED = {
    65: "more unarmored vehicles in a Location than the column's highest KIA#, on the Infantry and Area Target Types (A7.308, C1.55, C3.32, with 65.1); the Specific Collateral Attack on Passengers, Riders, and an unarmored vehicle's Vulnerable PRC, and after an Abandonment (D.8A, with 65.1); an AFV in positive TEM under other IFT attacks (A7.307, D5.31); an OT AFV's crew under Air Bursts (D5.311); the Immobilization Task Check after a mortar's hit, and Crew Survival's +1 for a Shocked or Abandoned vehicle (D5.5, D5.6, with 65.7).",
    75: "a MOL at an unarmored vehicle, a leader's MOL, and a MOL in a FG (A22.612, C11.51, with 75.6).",
    100: "a Critical Hit's HD clauses and the reversal of a hexside TEM (C3.7, C3.71); Emplacement ended by a forfeit of Wall Advantage (C11.3, with 100.4); a vehicle in Bypass in a mortar's target hex, and the DC's Bypass -2 (C1.55, C7.346, with 100.2).",
    105: "the upper superstructure of a non-turreted AFV (C3.9; ruling R60.22); an OVR's Random SW and Gun Destruction dr (A9.74, with 105.4); a DC's IFT attack through an unarmored Target Facing (C7.346, with 105.1); Defensive First Fire at a vehicle entering the firer's hex (D3.2, C5.51); the Unarmed bar of CC Reaction Fire (D7.21, with 105.4); a 1MT AFV's Recall on a MG's Stun.",
    110: "a hit on another vehicle of an overstacked hex (A5.132; ruling R60.21); a MG's To Kill attack in the AFPh, and its Residual FP (A9.6, A9.61, D3.53); a dm SW's capture (A9.8, with 110.6); units out of LOS or too close on the Infantry Target Type (C3.4); a transfer of Acquisition between Target Types, and its loss when a mortar is carried away (C6.52, C6.521); friendly units in a mortar's target hex (C9.1, with 110.5); no Emplacement on a paved road or a bridge, and the owner's choice between gunshield, Emplacement, and TEM (C11.2, C11.5); a DC at a manned Gun among several targets, and an unattended Gun on the Area Target Type (C11.4, C3.41); the PF's +1 of August and September 1943 and the firer's choice of a unit manning a Gun or SW (C13.31, with 110.7); Canister at a mixed Location the IFT refuses, and a Gun Duel for a Gun stacked with other Infantry (C8.4, C2.2401).",
    120: "the mine column of the AFV Destruction Table (C7.7).",
    140: "Interior Building Hexes and rooftops under a mortar's hit (B23.32); a building, a bridge, and an empty hex as targets of the Infantry and Area Target Types (C3.41).",
    150: "the OBA share of rules pass 60 built for mortars: Air Bursts (B13.3), the FFE Critical Hit (C3.72), Random Selection among Locations (C3.74), OBA at a Gun (C11.4, C11.6).",
    160: "Case A for an aircraft's attack (C7.21).",
}

assert lines.count(ENTRY) == 1
lines[lines.index(ENTRY)] = ENTRY + STATUS
joined = "\n".join(lines)
assert joined.count(P50) == 1
lines = joined.replace(P50, P50_NEW).split("\n")

for number, words in sorted(MOVED.items(), reverse=True):
    assert "\u2014" not in words and "\n" not in words
    total = [i for i, line in enumerate(lines) if line.startswith(f"| **Pass {number} total** ")]
    assert len(total) == 1, number
    at = total[0] + 1
    assert lines[at] == "", (number, lines[at][:40])
    if lines[at + 1].startswith("Moved here by pass 55"):
        at += 2
        assert lines[at] == "", number
    lines[at:at] = ["", M + words]

out = "\n".join(lines)
assert "\u2014" not in out and out.endswith("\n")
io.open(P, "w", encoding="utf-8", newline="").write(out)
print("status line, pass 50 sentence, moved paragraphs:", len(MOVED))
