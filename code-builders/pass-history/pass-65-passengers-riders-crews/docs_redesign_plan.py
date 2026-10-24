"""Pass 65 documents: the redesign plan's pass 65 entry gets its status line, and each later pass that receives a moved item gets a
"Moved here by pass 65" paragraph at the end of its entry (before the next pass's heading). Every anchor is asserted before writing.
LF, UTF-8, no BOM.
"""
import io
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402

P = REPO + "/src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md"

STATUS_ANCHOR = "with a vehicle class to tell tanks, tankettes, TD, SPA, SPAA, Assault Guns, and Carriers apart (corrected 2026-10-23). Needs first: 50, 60 (Collateral Attacks)."
STATUS = (
    " **Status, 2026-10-24: built to the merge gate on `feature/asl-backlog-pass-65`, cut from d0a8f4cd; the gate not run. The user's word of"
    " 2026-10-23 (\"start on Pass 65 without my intervention\") ends at the gate, and the gate, the push, and the merge are each the user's to"
    " start.** The nine tasks are built, in ten increments and an eleventh (2k) for what 2b and 2h left, in eleven commits and four rounds of the"
    " reviews' fixes (fbca6ac1 to ca0e14fa). The inventory named 83 rows, not 63: 30 are built (two of them with a deviation) and 53 moved to the"
    " pass that takes what is left. Catalog `asl-scenario-a1` 1.18.0 holds the chart's eight vehicle crews and a vehicle class on every vehicle. The"
    " frame's estimates, made from the code, with the plan's in brackets: 65.1, 3:00 (1:00); 65.2, 2:30 (1:00); 65.3, 1:15 (0:40); 65.4, 1:30 (0:40);"
    " 65.5, 0:50 (0:40); 65.6 with the vehicle crew (increments 2a and 2g), 3:00 (0:40); 65.7, 1:10 (0:40); 65.8, 1:40 (1:00); 65.9, 0:50 (0:40)."
    " The frame states 15:35 of building against 7:00, and 16:50 in all against 8:15 (its rows sum to 15:45); the build took about 7:15 by the"
    " clock. Rulings R65.1 to R65.33 are open to a veto. What the pass left out went to pass 70 (PRC in CC, Heat of Battle and leadership for PRC,"
    " CC at a wreck, a Carrier HS's recombination), pass 75 (A11.52's capture of Passengers, rearming by Scrounging, captured vehicles' VP, a"
    " Passenger's DC), pass 100 (unloading, Passenger fire, and Bailing Out in Bypass; D9.3's exception), pass 105 (an unarmored Target Facing"
    " under D5.311, a vehicle and its Passengers in one FG, Riders in an OVR, the rest of MG Removal and re-crewing, the one-man turret), pass"
    " 110 (A7.308 for ordnance and mortars, Ordnance First Fire at (un)loaders, a wreck under other attacks, A21.22's squad exchange), and"
    " passes 120, 125, 135, 150, 165, and 175, each with its reason in the [design](<Passes/ASL Unit Backlog Pass 65 Design.md>), sections 2,"
    " 7.1 to 17.1, and 18, the [review](<Passes/Scenario A1 Backlog Pass 65 Review 2026-10-24.md>), and the backlog's section 71."
)

MOVES = {
    "**Pass 75: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: PRC in CC, decided in Rules and not wired into Play (A11.611, A11.71, with"
        " 70.7); Heat of Battle, leadership, and LLMC for PRC, and D6.651's reach of a Passenger leader (D6.651); a CC counter on an unload or a Bail"
        " Out into an enemy Location, and a vehicle entered at its capture in the CCPh (D6.5, D6.24, D5.42); the Bail Out NMC's Heat of Battle,"
        " leadership, Wound Severity, and ELR, and D5.311's crew's (D6.24, D5.311); TPBF's CC counter at PRC (A7.211); CC against a wreck (D10.1);"
        " a Carrier HS's recombination without a TC or leader (D6.82)."
    ),
    "**Pass 80: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: A11.52's capture of Passengers; rearming with scrounged small arms (A20.552,"
        " D10.5, with 75.5); captured vehicles' VP (A21.2); a DC thrown or Placed by a Passenger (D6.72, with 75.6)."
    ),
    "**Pass 105: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: unloading, a Passenger's fire, and Bailing Out in Bypass (D6.5, D6.1, D6.21,"
        " with 100.2); D9.3's exception for units Abandoning, Surviving, unloading, or Bailing Out (with 100.7)."
    ),
    "**Pass 110: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: D5.311's unarmored Target Facing, and an OT AFV treated as unarmored (D5.311,"
        " with 105.1); the one-man turret of a SMC crew (D5.343, with 105.1); A9.74's dr for a PRC's SW, and Riders in an OVR (A9.74, D6.22, with"
        " 105.4); a vehicle and its Passengers in one FG, and a Passenger's Bounding First Fire (D6.64, D6.65, D3.3, D3.5); D6.5's interleaving of"
        " the interim move, and the Road Bonus above four MF after unloading (D6.5, A4.11); a pin or break on a Rider by an ordnance, mortar, FT, MOL,"
        " or DC hit, and an entry from off board with Riders (D6.23, D6.21); a re-crewing's failure under D5.43, MG Removal of a heavier MG or a"
        " mortar, an Armor Leader on the `instance-moved` paths, and the broken crew's +1 for Survival (D5.42, D5.43, D6.631, D3.43, D5.6); the red"
        " CS# on the IFT's Vehicle line (D5.7); the start dr in a Motion attempt (A21.22, D2.401); a Disabled AAMG that is not the MA (D10.52); a CE"
        " or BU choice at setup (A2.52, D5.3)."
    ),
    "**Pass 115: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: A7.308's Random Selection on the ordnance Infantry Target Type and a mortar's"
        " hit (A7.308, C1.55, C3.32); Ordnance First Fire at (un)loaders (D6.5); a wreck under the IFT beside other vehicles or of the other side,"
        " and under Infantry Target Type and mortar hits (D10.1); the American exemption and A21.22's squad exchange for a HS (D10.52, A21.22, with"
        " 110.6); unhooking a towed Gun as its vehicle's crew Abandons (D5.41, with 110.2); the Specific Collateral Attack after a Gun's HEAT with no"
        " HE FP (D.8A)."
    ),
    "**Pass 125: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: the Specific and General Collateral Attacks, and the Immobilization TC, after a"
        " mine's attack (D.8, D5.5, D8.1)."
    ),
    "**Pass 130: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: a stream read from the map, not the terrain's name, for a Rider's Bail Out"
        " (D6.21, B13.43)."
    ),
    "**Pass 140: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: a friendly Vehicular-Sized Entrance for a Rider's Bail Out (D6.21); a CE"
        " crew entering a building (D5.3)."
    ),
    "**Pass 155: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: the General Collateral Attack by OBA on a vehicle's PRC (D.8B)."
    ),
    "**Pass 170: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: Cavalry as Riders (D6.2)."
    ),
    "**Pass 180: ": (
        "Moved here by pass 65 (2026-10-24), with no task renumbered: motorcycle Riders and a Rider's PP against a towed Gun's ammunition (D6.2);"
        " an armed unarmored vehicle's crew under a Specific Collateral Attack (D6.71)."
    ),
}

text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("﻿") and "Moved here by pass 65" not in text and "—" not in STATUS
assert text.count(STATUS_ANCHOR) == 1
lines = text.split("\n")
start = next(i for i, line in enumerate(lines) if line.startswith("### 22.1 "))
end = next(i for i, line in enumerate(lines) if line.startswith("### 22.2 "))
targets = {}
for head, para in MOVES.items():
    hits = [i for i in range(start, end) if lines[i].startswith(head)]
    assert len(hits) == 1, (head, len(hits))
    assert lines[hits[0] - 1] == "", head
    assert "—" not in para
    targets[hits[0]] = para
for i in sorted(targets, reverse=True):
    lines[i:i] = [targets[i], ""]
out = "\n".join(lines).replace(STATUS_ANCHOR, STATUS_ANCHOR + STATUS)
io.open(P, "w", encoding="utf-8", newline="").write(out)
print("status line added; moves added before", len(targets), "entries")
