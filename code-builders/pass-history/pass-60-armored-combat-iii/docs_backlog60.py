"""Pass 60 documents: the backlog's section 60, "Added by pass 60", and the older rows that named pass 60, corrected where they stand.
Every anchor is counted before the file is written. LF, UTF-8, no BOM."""
import io

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog.md"
text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("\ufeff") and text.endswith("\n")
assert "## 60. Added by pass 60" not in text

D = "Pass 60 (design {})"
V = "Pass 60 reviews, 2026-10-15 ({})"

LEFT = [
    ("Note A of the To Kill table: a red CS# lowers the DR for the burning test", D.format("2"), "C7.7 note A, p. 190",
     "A color on the catalog's Crew Survival number, at the catalog's next version", "Not applied, to the FT/MOL and DC columns as to the others."),
    ("A hit on another vehicle of a vehicle-overstacked hex", D.format("2, F9; ruling R60.21"), "A5.132, p. 52",
     "Pass 110, beside OVR Prevention: a Random Selection by size among vehicles and a target choice the To Hit does not have", "No such hit."),
    ("The upper superstructure of a non-turreted AFV", D.format("2; 12.1; ruling R60.22"), "C3.9, p. 171; ruling R7.4",
     "Pass 105, with a non-turreted AFV counter and a fact on the record so that a recorded hit keeps its Location", "A non-turreted AFV (the SPW 251/1) is always hit in the hull."),
    ("The PF Check's +1 in August and September 1943", D.format("7.1; ruling R60.5"), "C13.31, p. 183",
     "Pass 110 (task 110.7, the PFk), or a card dated then", "In the calculator with no test: the PF's date bar refuses it before October 1943."),
    ("HEAT at Infantry in a pillbox", D.format("7.1; ruling R60.4"), "C8.31, p. 178",
     "Pass 115, with the pillboxes", "HEAT is fired at Infantry in a building, in rubble, or behind a wall. Behind a wall it takes no Case Q from the wall (the older gap of section 59.1, pass 100)."),
    ("The H#[9] HEAT of the German 37mm Guns", D.format("7.1"), "C8.31, p. 178",
     "Pass 110: a counter that lists H#[9]", "In the HE Equivalency table at 12 FP; no catalog Gun maps to it."),
    ("A non-20L ATR at anything but a Gun's crew; a 20L ATR", D.format("7.1; rulings R60.4, R60.6"), "C13.23, C13.24, p. 183",
     "Pass 110 (task 110.7, ATR usage), with a 20L ATR counter", "A non-20L ATR attacks only the crew of a Gun `GunAt` finds; at any other target it stays Small Arms and is refused as ordnance. The 20L case is in the calculator; no catalog ATR is 20L."),
    ("The firer's choice of a Known unit manning a Gun or SW after a PF's hit", D.format("6, F4; ruling R60.5"), "C13.31, p. 183",
     "Pass 110: a choice offered to the firer before the Random Selection", "A PF's hit affects one unit by Random Selection, always."),
    ("More unarmored vehicles in a Location than the column's highest KIA#", D.format("8.1"), "A7.308, p. 55; C1.55, p. 165",
     "Pass 65, with A7.308's Random Selection of the vehicles affected", "The shot is refused (`vehicles-beyond-kia`)."),
    ("A PF's or an ATR's shot, the 20L ATR's included, at a Location that holds a vehicle, with no vehicle named", D.format("8.1"), "C13.31, C13.24; C3.32",
     "Pass 110", "Refused; the vehicle may be named on the Vehicle Target Type."),
    ("Defensive First Fire on the Infantry Target Type at a Location where a vehicle moves with Infantry; a phasing vehicle's Bounding First Fire there", D.format("8.1"), "C3.4, C3.32, p. 170",
     "Pass 110", "Refused in the MPh; vehicles that are not moving are left out of the hit, as C3.4 has it for every non-moving unit."),
    ("No Specific Collateral Attack after the crew Abandons on a failed Immobilization Task Check, or for a Gun's HEAT with no HE FP to step from; an unarmored vehicle's Vulnerable PRC on the Vehicle Target Type", D.format("8.1; ruling R60.7"), "D.8A, p. 192; D5.5",
     "Pass 65 (tasks 65.1 and 65.7)", "None is made."),
    ("A CE AFV in terrain with a positive TEM under an IFT attack that is not an ordnance hit or a named FT, MOL, or DC", D.format("8.1; 10.1; ruling R25.6"), "D5.31, p. 203; A7.307",
     "Pass 65, with D5.31's row", "Refused (ruling R25.6). An ordnance hit, or an attack that names the AFV, attacks its crew with the CE DRM alone."),
    ("A vehicle's own To Hit DRM on the Infantry Target Type: Bypass, and a shellhole's TEM that is the Infantry's alone", D.format("8.1"), "C3.32 and its example, p. 169; D2.32",
     "Pass 100 (task 100.2, Bypass); pass 135 for the shellhole", "Case K or N, Case P for its size, and the Location's terrain TEM as on the Vehicle Target Type."),
    ("Units out of LOS or too close on the Infantry Target Type", D.format("6, F5; ruling R60.9"), "C3.4, p. 170",
     "Pass 110", "Not handled; the Area Target Type's are (ruling R60.24)."),
    ("No Immobilization Task Check after a mortar immobilizes an AFV", D.format("9.1"), "D5.5, p. 204; C1.55",
     "Pass 65 (task 65.7)", "As on the IFT's Vehicle line today: none."),
    ("An OT AFV hit by Air Bursts: its crew, which D5.311 has break and rout like Infantry", D.format("9.1; ruling R60.12"), "D5.311, p. 203",
     "Pass 65", "The AFV is on the Vehicle line with no Air Burst TEM; its crew is not attacked."),
    ("Crew Survival's +1 for a Shocked or Abandoned vehicle", D.format("9.1"), "D5.6, p. 204",
     "Pass 65 (task 65.7): Shocked and Abandoned among the vehicle's facts", "The +1 reads Stunned, Recalled, or Stun +1 only."),
    ("A mortar's shot at a hex with a vehicle in Bypass; a mortar's Defensive First Fire at a hex with a vehicle", D.format("9.1"), "C1.55, p. 165; D2.32",
     "Pass 100 (task 100.2, Bypass) and pass 110", "Refused with the reason."),
    ("Air Bursts with an entrenchment; a building's Interior Building Hexes and rooftops", D.format("9.1"), "B13.3, p. 128; B23.32, B23.8",
     "Pass 115 (entrenchments); pass 140 (Interior Building Hexes, rooftops), with the board data", "No entrenchment exists: Air Bursts with one rest on an Emplaced Gun in woods. Every building hex is taken as not Interior, and no rooftop is attacked."),
    ("Friendly units in a mortar's target hex", D.format("6, F6; ruling R60.11"), "C9.1, C3.33, p. 169",
     "Pass 110", "Refused (`area-friendly-units`), now with its sentence."),
    ("The Aerial AF where C7.11's row prints none", D.format("10.1"), "C7.11, C7.12, p. 190",
     "A second reading of the render; a counter with AF 14, 18, or 26 (pass 105)", "Read from a render: AF 0 and 1 give 0, 2 gives 1, 3 gives 2, 4 and 6 give 3, 8 and 11 give 4; for 14, 18, and 26 the lowest printed hull AF stands."),
    ("Naming an AFV in a Location that also holds concealed or hidden units or a Dummy", D.format("10.1"), "A22.34, A22.612, A23.5; A12.1",
     "Pass 70: a record that names the AFV without telling the firing side what else is there", "Refused (`target-vehicle-concealed`), counting only what the firer can see."),
    ("A DC at an AFV: the Bypass -2; Case B beyond a target level above; C7.346's IFT attack through an unarmored Target Facing; a DC Placed where an unarmored or a friendly vehicle is", D.format("10.1"), "C7.346, p. 177; A23.3",
     "Pass 105 (an unarmored Target Facing, D1.22); pass 100 (Bypass); pass 75 (A23.3's Placement)", "The Bypass -2 is never reached; no catalog AFV has an unarmored Target Facing; the Placement is refused with the rule (`ScenarioA1FireEligibility.DcPlacementVehicleRefusal`)."),
    ("A MOL at an unarmored vehicle; a MOL's Flame; ATMM; a vehicle as the thrower of a MOL or DC; a throw down a level", D.format("6, F7; ruling R60.16"), "A22.612, A22.613, p. 89; C13.7; A23.6",
     "Pass 75 (task 75.6), pass 110 (task 110.8, ATMM), pass 185 (Flame), pass 105 (a vehicle thrower)", "A MOL at a Location with an unarmored vehicle is refused; no Flame is placed."),
    ("No Gun Emplaced on a paved road or a bridge", D.format("11.1"), "C11.2, p. 181",
     "A recorded map fact on the ordnance record, so that the record check need not read the map; pass 110", "A Gun there is Emplaced as anywhere else."),
    ("An unattended Gun on the Area Target Type", D.format("11.1; ruling R60.18"), "C3.41, C11.4, p. 182",
     "Pass 110", "A mortar's shot at one is `target-outside`. The Infantry Target Type is built."),
    ("A DC at a manned Gun: C11.4's Random Selection with the Gun counted among the targets", D.format("11.1"), "C11.4, C11.6, p. 182",
     "Pass 110", "The Gun's fate is read from the result its manning unit's group took."),
    ("A Critical Hit on an unattended Gun", D.format("11.1"), "C11.4, C3.71",
     "The user's reading", "The Gun is destroyed with no IFT DR."),
    ("Unpossessed weapons checked by Indirect Fire and by an OVR; the OVR's end-of-MPh check; an OVR of an empty Location", D.format("6, F8; ruling R60.17"), "A9.74, p. 65",
     "Pass 105 (task 105.4, OVR completed); pass 110 for Indirect Fire", "No dr is made for them. An OVR's KIA: section 60.2."),
    ("The Bren Carrier's MA prints no FP", D.format("12.1"), "D3.54, p. 201; ruling R0.3",
     "A value manufactured under R0.3 on sheet MFG, at the catalog's next version", "It fires at the British LMG's 2 FP with a Normal Range of 8, like the other vehicle MGs."),
    ("A MG fired as ordnance: no C4 modification but C4.2's, the plain black Vehicle row, Case D's 25mm row, no Acquisition", D.format("12.1; ruling R60.19"), "A9.61, p. 64; C4; C6.5",
     "The user's reading", "As built; Inexperienced Personnel lower its B# by one (A19.32)."),
    ("A MG's To Kill attack in the AFPh, as Bounding First Fire, by a pinned possessor, by a lone leader or hero, by a vehicle's MA MG in Motion or BU, as Intensive Fire, or with a directing leader", D.format("12.1; ruling R60.19"), "A9.61, p. 64; A9.12; D3.53",
     "Pass 110", "Refused with Rules' reason, which the Gun panel shows."),
    ("A 1MT AFV's Recall on a MG's Stun; the AAMG's all-round arc against a bow MG's VCA; a MG's Residual FP after its To Kill attack; the 12.7mm and 15mm rows", D.format("12.1"), "D5.34; D1.8; A8.2; A9.61",
     "Pass 105 (a 1MT AFV, the arcs); pass 110 (the Residual FP, the counters)", "None is built: no Case A is charged and no arc enforced for the AAMG; no catalog MG is 12.7mm or 15mm, so the Basic TK# is always 4."),
    ("A9.223's broader reading: a Fire Lane cancelled whenever its MG's Location is entered for CC", D.format("12.1; ruling R60.20"), "A9.223, p. 63; D7.21",
     "The user's reading", "Cancelled when its MG's manning Infantry fires CC Reaction Fire, gated by `cancelsFireLanes` on the reaction's record."),
    ("Whether Case K replaces the halving for a MG's To Kill attack at a concealed AFV", "Pass 60 reviews, 2026-10-15 (second referee; ruling R60.27)", "A9.61, p. 64; A7.23",
     "The user's reading", "Refused."),
    ("The Studio check of the whole pass at the five widths, a Player Turn at 320, and the Replay page", D.format("13.1, 13.4"), "(page)",
     "The user's word of 2026-10-15: the need is reassessed pass by pass", "The narrower check covered the unseen vehicle, hidden and concealed units, and the new controls at 1920, 1024, and 320."),
]

FOUND = [
    ("The pass 55 page test of HE refused after it ran out", V.format("UI"), "C8.9, p. 179",
     "A page test of a Gun with neither HE nor AP left", "The Gun now offers AP instead, so the refusal has no page test; its Rules and Play tests stand."),
    ("Three rule conditions decided in the page: an AAMG MA fires only CE, a plain MA MG's 8-hex Normal Range, and when HE is left unnamed", V.format("UI; Rules boundary"), "D3.54; D1.83; C8.1",
     "A fix in the member named in the review: Rules members the page reads", "Each works for the catalog's vehicles; extensions of patterns from passes 50 and 55."),
    ("`#propose-throw-dc` is disabled with no title", V.format("UI"), "(page)",
     "A title with the reason, as other disabled controls carry", "Older than the pass."),
    ("C11.3 is gated in the verifier by the presence of `randomDestruction`", V.format("Rules boundary"), "C11.3, p. 182",
     "A fact of its own on the ordnance record", "Both facts are set on every new shot, so the proxy holds; a comment names it."),
    ("`vehicles-beyond-kia` refuses a mortar's shot that C1.55 only limits", V.format("first referee; design 9.1"), "C1.55, p. 165; A7.308",
     "Pass 65, with A7.308's Random Selection of the vehicles affected", "Refused, with the advice worded for Guns only."),
    ("A tie between Locations in a Critical Hit's Random Selection", V.format("first referee; design 9.1"), "C3.74, p. 171; A.9",
     "A reading of A.9 for Locations; pass 150, where OBA uses it", "It goes to the lowest Location."),
    ("An OVR's KIA makes no A9.74 dr", V.format("second referee; design 11.1"), "A9.74, p. 65",
     "Pass 105 (task 105.4): a plain dr by a vehicle that is not fully-tracked and -1 by one that is", "No dr: the fact is left off OVR attacks."),
    ("A vehicle's MA MG's To Kill attack in the AFPh, unhalved", V.format("second referee"), "D3.53, D3.54, p. 201",
     "Pass 110, with the MG's AFPh", "Refused."),
    ("A Manned Gun counted as possessed for A9.74; a DC Placement's PAATC decided in Play", V.format("table player"), "A9.74; A23.3, A23.5",
     "A fix in the member named in the review", "Each works today; the Placement bar itself is now Rules'."),
]


def row(item, deferred, rules, depends, now):
    for cell in (item, deferred, rules, depends, now):
        assert "|" not in cell and "\u2014" not in cell and "\n" not in cell, item
    return f"| {item} | {deferred} | {rules} | {depends} | {now} |"


HEAD = "| Item | Deferred by | Rules | Depends on | What happens now |\n|---|---|---|---|---|\n"
section = (
    "\n## 60. Added by pass 60\n\n"
    "Written with the pass's documents on 2026-10-15. The first table is what a section of the [pass 60 design](<Passes/ASL Unit Backlog Pass 60 Design.md>) lists as left out (sections 2, 6, 7.1 to 12.1, and 13.1), with what the plan's entry or the frame placed in the pass and the build did not make; the second is what the reviews and the build found and did not fix (design 13.3). Rows of earlier sections that named pass 60 are corrected where they stand: what the pass built is marked there, and what it moved on names its new pass. The [review](<Passes/Scenario A1 Backlog Pass 60 Review 2026-10-15.md>) says the same in prose.\n\n"
    "### 60.1 What the pass left out\n\n" + HEAD + "\n".join(row(*r) for r in LEFT) + "\n\n"
    "### 60.2 What the reviews and the build found and did not fix\n\n" + HEAD + "\n".join(row(*r) for r in FOUND) + "\n"
)

EDITS = [
    # 55.2: the mixed hit
    ("| The mixed hit (pass 60) | Such a shot is refused; `OwnHexHindrance` is tested in Rules for a live AFV. |",
     "| The mixed hit (pass 60) | Such a shot is refused; `OwnHexHindrance` is tested in Rules for a live AFV. **Built in pass 60 (design 8.2; ruling R60.8): an Infantry Target Type shot at a Location with an unarmored vehicle judges each unit and vehicle on its own TH#, and a CE AFV's crew is attacked; a Location holding only AFVs stays the Vehicle Target Type's. More vehicles than the column's highest KIA# is refused (section 60.1).** |"),
    # 56.1: A5.132
    ("| A5.132, p. 52 | Pass 60, task 60.6: the To Hit's target choice (passes 50 and 55 left it; 58.1, the pass 55 design's section 3) | No such hit. |",
     "| A5.132, p. 52 | Pass 110 (moved on by pass 60, ruling R60.21): a Random Selection by size among vehicles and the To Hit's target choice (passes 50, 55, and 60 left it; 58.1, the pass 55 design's section 3) | No such hit. |"),
    # 58.1: MG fire against armor
    ("| A MG's IFT attack has no effect on armor and attacks a Vulnerable crew. |",
     "| A MG's IFT attack has no effect on armor and attacks a Vulnerable crew. **Built in pass 60 (design 12; rulings R60.19, R60.25, R60.27): a possessed MG or a vehicle's MA MG makes a To Hit DR and a To Kill DR with Basic TK# 4 at a named AFV, and the CE crew's Collateral Attack follows unless the attack Stuns. Left: the AFPh, the MG's Residual FP, a concealed AFV (section 60.1).** |"),
    ("| A5.132; C5.64 | Passes 60 (task 60.6; moved on by pass 55, design 3) and 110 | Neither is built. |",
     "| A5.132; C5.64 | Pass 110 (A5.132 moved on by pass 55 to pass 60, and by pass 60 to pass 110, ruling R60.21) | Neither is built. |"),
    ("| Pass 60; the Bypass part with D2.35 in pass 105 (pass 55 took none of it, and its design's section 3 does not name this row) | Refused with the reason. |",
     "| Pass 60; the Bypass part with D2.35 in pass 105 (pass 55 took none of it, and its design's section 3 does not name this row) | Refused with the reason. **Pass 60 built a SW MG's To Kill attack at an AFV in its own Location, with D3.2's colored dr and no Case E, and a FT, MOL, or DC at an AFV in the attacker's hex, through the rear. Defensive First Fire at a vehicle entering the firer's hex moves to pass 105, with C5.51's row.** |"),
    # 59.1
    ("| Pass 110: a PF's check before the declaration. Pass 60 (task 60.5): the tank's shot at a Location a Gun shares with other Infantry, which an older check refuses | The declared shot resolves alone. The duel's shot is AP at a vehicle and HE otherwise. |",
     "| Pass 110: a PF's check before the declaration. Pass 60 (task 60.5): the tank's shot at a Location a Gun shares with other Infantry, which an older check refuses | The declared shot resolves alone. The duel's shot is AP at a vehicle and HE otherwise. **Pass 60 did not build the Gun stacked with other Infantry; it moves to pass 110 with the rest of the row.** |"),
    ("| Pass 60 (task 60.1): the mixed Locations the IFT refuses | The shot is refused. For a hidden AFV the refusal could tell the firer something; not tested. |",
     "| Pass 60 (task 60.1): the mixed Locations the IFT refuses | The shot is refused. For a hidden AFV the refusal could tell the firer something; not tested. **Not built by pass 60, whose mixed Locations are the Infantry Target Type's; it moves to pass 110 with Canister's other clauses.** |"),
    ("| Pass 60: the fact carried on the hit's IFT attack | An ordnance Critical Hit uphill gets no -1. |",
     "| Pass 60: the fact carried on the hit's IFT attack | An ordnance Critical Hit uphill gets no -1. **Built in pass 60 (design 9.2; ruling R60.14): a Direct Fire Critical Hit reverses Height Advantage to -1 where the shot carries `reversesHeightAdvantage`.** |"),
    ("| Pass 60 (task 60.4) | A 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry with the reason. |",
     "| Pass 60 (task 60.4) | A 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry with the reason. **Built in pass 60 (design 7.2; ruling R60.4): AP, APCR, APDS, and HEAT fire at Infantry at their HE Equivalency FP.** |"),
    # 59.2
    ("| Pass 60 (task 60.5, with A9.74) | No SW is destroyed by any KIA in the code. |",
     "| Pass 60 (task 60.5, with A9.74) | No SW is destroyed by any KIA in the code. **Built in pass 60 (design 11.2; ruling R60.17): every Final KIA of an IFT attack, a Canister KIA included, makes each SW and Gun its victims held roll on the same column.** |"),
]
for old, new in EDITS:
    assert text.count(old) == 1, old[:90]
for old, new in EDITS:
    text = text.replace(old, new)
text = text.rstrip("\n") + "\n" + section
assert "\u2014" not in text
io.open(P, "w", encoding="utf-8", newline="").write(text)
print("edits:", len(EDITS), "rows added:", len(LEFT), "and", len(FOUND))
