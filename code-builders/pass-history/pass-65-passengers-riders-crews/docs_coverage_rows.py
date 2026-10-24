"""Pass 65 documents: the coverage's sentences for the sections the pass changed, the status words its counts move, the D introduction's
count (which coverage_counts.py does not reach), the chapter summary table, the Status paragraph, and limit 7. Every anchor is checked first."""
import io

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Coverage.md"
N = " **Pass 65 (2026-10-24):** "

# section: (status now or None, old status for a change, sentence)
SECTIONS = {
    "A4": (None, None, "a unit that unloads keeps four MF with no Double Time or leader bonus, and a leader's IPC counts when his stack boards (4.11, 4.42, 4.5). Left: the Road Bonus can lift an unloader above four MF."),
    "A5": ("Mostly built", "Partly", "Riders board within their own capacity, apart from Passenger PP and the Location's limits (5.3)."),
    "A7": (None, None, "TPBF against PRC (7.211), Random Selection of the vehicles beyond the column's highest KIA# on the IFT (7.308), and Passengers and Riders as Vulnerable PRC (7.307) are built; a pinned PRC is BU (7.821). Left: the ordnance Infantry Target Type and a mortar's share of 7.308 (pass 110); IFE; a pin on a Rider by a hit that is not the IFT's."),
    "A11": (None, None, "a crew that mans no Gun is advanced against and fights in CC as Infantry (R65.29); PRC in CC (11.611, 11.71) are decided in Rules and not yet played (pass 70)."),
    "A21": ("Mostly built", "Partly", "an Abandoned AFV is captured automatically or by a capture attempt in CC, and a captured vehicle is run by a Temporary Driver or crew with A21.22's penalties (21.2 to 21.22). Left: a capturing squad's exchange for a HS (R65.32, pass 110); a captured vehicle's VP (pass 75)."),
    "A26": (None, None, "a manned vehicle captured by the pass's event holds Control for its captor (26.12); older captures and PRC keep R24.5; a Carrier's inherent HS counts its VP once (26.213)."),
    "B10": (None, None, "under Infantry fire a CE crew's DRM is not added to Height Advantage where the attack carries the pass's fact, and open PRC claim it apart (10.31)."),
    "B13": ("Partly", "Mostly not built", "Riders Bail Out before woods and may not board there, unless on a road, in Bypass, or in a stream (13.43)."),
    "C1": (None, None, "a mortar's Immobilization takes the crew's TC, and PRC roll for Survival on its kill (1.55). More vehicles than the column's highest KIA# stays refused (pass 110)."),
    "C3": (None, None, "an OT AFV's crew under Air Bursts is attacked as unarmored (3.332, 3.72). The Infantry Target Type keeps `vehicles-beyond-kia` (3.32; pass 110)."),
    "D.1 to D.8B": (None, None, "Passengers and Riders take the General and Specific Collateral Attacks, with no FFNAM or FFMO, and when crew and Passengers are stunned or shocked only Riders are attacked (D.6 to D.8B). A Passenger's Prep Fire marks its vehicle Prep Fired (D.3, ruling R65.27, a deviation). Left: mines; OBA; a Gun's HEAT with no HE FP."),
    "D2": (None, None, "Passengers' and Riders' Motion Fire is halved (2.42), and a Passenger's time aboard counts toward a Motion attempt (2.401)."),
    "D3": (None, None, "an Armor Leader's return with his crew is built (3.43); a halftrack's FG with its Passengers is decided in Rules and not formed in Play (3.4, pass 105); a Passenger's Bounding First Fire is refused (3.3)."),
    "D5": ("Mostly built", "Partly", "the vehicle crew counter (5.1), Voluntary Abandonment, self-destruction, and re-crewing (5.41 to 5.43), Unprotected Crews under Air Bursts (5.311), the TC's causes and Crew Survival on every path (5.5, 5.6), the red CS# (5.7), and a crew's fire counter (5.8) are built. Left: an unarmored Target Facing (pass 105); a SMC crew's one-man turret; mines' Immobilization."),
    "D6": ("Mostly built", "Mostly not built", "Passengers are targets with their Collateral Attacks, morale, DM, and Survival; they fire, pass SW, rout beneath, join an OVR, and board and unload under FFNAM; Riders board, fire, and Bail Out; Carriers are built (6.1 to 6.9). The bold text before this is the first version's. Left: PRC in CC and D6.651's reach in Play (pass 70); a vehicle's FG with its Passengers (pass 105); Bypass (pass 100)."),
    "D7": (None, None, "a loaded halftrack's Passengers add their FP to its OVR with one leadership modifier, and an OVR attacks an enemy vehicle's Vulnerable PRC (7.11, 7.12, 7.16). Left: Riders' FP (pass 105)."),
    "D8": (None, None, "the Immobilization TC's second cause on an Immobile or bogged target (8.11, 8.22)."),
    "D9": ("Partly", "Mostly not built", "Passengers and Riders of an OT AFV are attacked as its crew (9.1), and a kill that does not burn rolls Survival on every path but CC (9.2). Left: 9.3's exception for units Abandoning, Surviving, unloading, or Bailing Out (pass 100)."),
    "D10": ("Mostly built", "Partly", "a non-burning wreck is attacked as its vehicle where only a Burn counts (10.1), and Scrounging is built (10.5 to 10.52; the LMG assembled, R65.30). Left: the Infantry Target Type, a mortar, and a DC at a wreck (pass 110); CC at a wreck (pass 70)."),
}

EDITS = [
    ("| A. Infantry and basic rules | 26 | 0 | 15 | 8 | 1 | 0 | 2 |", "| A. Infantry and basic rules | 26 | 0 | 17 | 6 | 1 | 0 | 2 |"),
    ("| B. Terrain | 37 | 0 | 3 | 7 | 20 | 0 | 7 |", "| B. Terrain | 37 | 0 | 3 | 8 | 19 | 0 | 7 |"),
    ("| D. Vehicles | 17 | 0 | 2 | 5 | 3 | 0 | 7 |", "| D. Vehicles | 17 | 0 | 5 | 4 | 1 | 0 | 7 |"),
    ("| **A to E** | **105** | **0** | **25** | **25** | **28** | **0** | **27** |", "| **A to E** | **105** | **0** | **30** | **23** | **25** | **0** | **27** |"),
    ("| 7: 2 built, 4 partly, 1 not built |", "| 7: 2 built, 1 deviation, 3 partly, 1 not built |"),
    ("A9 and C8 are now Mostly built, and C1 Mostly not built.",
     "A9 and C8 are now Mostly built, and C1 Mostly not built. **Pass 65 (2026-10-24, on its branch)** changed 99 rows of the inventory (83 of its own and 16 that its code touched) and the counts of fourteen sections (A4, A5, A7, A11, A21, B13, D.1 to D.8B, D2, D5, D6, D7, D8, D9, D10), and added its sentence to those rows and to A26, B10, C1, C3, and D3; A5, A21, D5, D6, and D10 are now Mostly built, and B13 and D9 Partly."),
    ("7. **Passengers are cargo.** They load, ride, and unload. They do not fire, rout, or fight, they are never a target, and they die with their vehicle without a roll.",
     "7. **Passengers are cargo.** They load, ride, and unload. They do not fire, rout, or fight, they are never a target, and they die with their vehicle without a roll. **Pass 65 (2026-10-24): Passengers and Riders are Vulnerable targets with Collateral Attacks and Survival, fire from their vehicle, rout beneath it, add FP to an OVR, and Riders Bail Out; only their part in CC waits (pass 70).**"),
]

raw = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in raw
for old, new in EDITS:
    assert raw.count(old) == 1, ("anchor", old[:80], raw.count(old))
lines = raw.split("\n")
found = {}
for i, line in enumerate(lines):
    if line.startswith("| "):
        c = line.split("|")[1:-1]
        key = c[0].strip()
        if key in SECTIONS and 141 > 0:
            if key in found:
                continue  # the section 8 table repeats some keys; the first is the section's row
            found[key] = i
missing = [k for k in SECTIONS if k not in found]
assert not missing, missing
for key, (status, old_status, sentence) in SECTIONS.items():
    i = found[key]
    c = lines[i].split("|")[1:-1]
    assert len(c) in (7, 8), (key, len(c))
    assert "\u2014" not in sentence
    if status:
        assert c[2].strip() == old_status, (key, c[2])
        c[2] = f" {status} "
    c[-2] = " " + c[-2].strip() + N + sentence + " "
    lines[i] = "|" + "|".join(c) + "|"
text = "\n".join(lines)
for old, new in EDITS:
    text = text.replace(old, new)
io.open(P, "w", encoding="utf-8", newline="").write(text)
print("sections", len(SECTIONS), "edits", len(EDITS))
