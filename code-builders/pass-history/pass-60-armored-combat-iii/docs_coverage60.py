"""Pass 60 documents: the coverage's text beside the sections the pass changed, the three statuses that change, the summary by section, the
"which fire reaches which target" table, and the status line. The counts of sections 3 to 6 are code-builders/documents/coverage_counts.py's.
Every anchor is counted before the file is written. LF, UTF-8, no BOM."""
import collections
import io
import re

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Coverage.md"
text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("\ufeff") and "Pass 60 (2026-10-15)" not in text
lines = text.split("\n")
S = "**Pass 60 (2026-10-15):** "

MISSING = {
    "A9": "MG fire against armor is built: a possessed MG or a vehicle's MA MG makes a To Hit DR and an AP To Kill DR at a named AFV, and the CE crew's Collateral Attack follows unless it Stuns (9.6, 9.61, 9.611); Random SW and Gun Destruction on every Final KIA of an IFT attack, a dm SW as a SW (9.74, 9.8); CC Reaction Fire cancels a Fire Lane (9.223). Left: a MG's To Kill attack in the AFPh and its Residual FP; an OVR's KIA.",
    "A22": "a FT and a MOL name an AFV and make their To Kill attack on it (22.34, 22.612). Left: a MOL at an unarmored vehicle; a leader's MOL; Flame.",
    "A23": "a DC is Placed, after a PAATC, or Thrown at an AFV, with the Position DR and the DC column (23.5).",
    "B13": "Air Bursts in full against unarmored, CE, and OT targets, with the Emplacement TEM, kept on a Critical Hit (13.3). Left: OBA; entrenchments, which do not exist.",
    "B23": "a mortar's hit attacks each level of a building hex with +1 for each level above it (23.32), outside Interior Building Hexes and rooftops, which the board data does not mark.",
    "C1": "a mortar's hit on a vehicle is built (1.55): an AFV on the IFT at half the HE FP with the AF and OT DRM, an unarmored vehicle on the Vehicle line. The rest is OBA's (pass 150).",
    "C3": "the Infantry Target Type at a Location with an unarmored vehicle, and at Known and concealed units together, judged unit by unit (3.32, 3.4); an unattended Gun as a target (3.41); a mortar's hit on vehicles and on the units out of its LOS (3.33, 3.332, 3.72); a Critical Hit reverses Height Advantage and an unarmored vehicle's TEM (3.71); Random Selection among Locations (3.74). Left: the upper superstructure of a non-turreted AFV (3.9, pass 105); more vehicles in a Location than the column's highest KIA#.",
    "C7": "the FT/MOL, DC, MG, and Indirect Fire columns (7.344 to 7.346, 7.7), the DC's Aerial AF (7.12), and Case A for those attacks (7.21). Left: note A of 7.7; the mine column; aircraft.",
    "C8": "HE Equivalency (8.31) for AP, APCR, APDS, HEAT, a PF, a PSK, and a 20L ATR at unarmored targets; a Canister KIA destroys a SW as an HE KIA does (8.42).",
    "C9": "a mortar fires at a hex holding a vehicle, a manned Gun, or units on several levels of a building (9.1). Left: friendly units in the hex.",
    "C11": "AP, HEAT, and an ATR at a Gun, an unattended Gun, the gunshield's +1 against Indirect Fire, and the Gun Destruction Table's small arms, MG, FT, MOL, and DC columns are built; a Gun set up manned by a non-crew unit loses its Emplacement (11.3). Left: an OVR's dr; no Emplacement on a paved road or a bridge; OBA.",
    "C13": "a PF and a PSK fire at Personnel by HE Equivalency (13.31, 13.9), and an ATR at a Gun (13.23).",
    "D.1 to D.8B": "Specific Collateral Attacks are built after an ordnance hit, a MG's To Kill attack, a FT, a MOL, and a DC that leave an AFV standing (D.8A). Left: Passengers and Riders.",
    "D2": "the DC's and the MOL's DRM against a moving vehicle (2.41).",
    "D3": "MG against AFV (3.54) is built for a vehicle's MA MG; its other MGs may not make it. Left: the AFPh (3.53).",
    "D5": "a MG's To Kill DR equal to the TK# Stuns (5.34); an OT AFV under Air Bursts is attacked as unarmored, and its crew is not yet (5.311).",
}
STATUS = {"A9": ("Partly", "Mostly built"), "C1": ("Not built", "Mostly not built"), "C8": ("Partly", "Mostly built")}
COUNTS = {"D.1 to D.8B": ("7: 2 built, 3 partly, 2 not built", "7: 2 built, 4 partly, 1 not built")}

start = next(i for i, line in enumerate(lines) if line.startswith("## 3. "))
end = next(i for i, line in enumerate(lines) if line.startswith("## 7. "))
found = {}
for i in range(start, end):
    for key in MISSING:
        if lines[i].startswith(f"| {key} | "):
            assert key not in found, key
            found[key] = i
assert set(found) == set(MISSING), set(MISSING) - set(found)
for key, i in found.items():
    c = lines[i].split(" | ")
    assert c[-1].endswith("|") and len(c) in (7, 8), (key, len(c))
    sentence = MISSING[key]
    assert "|" not in sentence and "\u2014" not in sentence
    if key in STATUS:
        assert c[2] == STATUS[key][0], (key, c[2])
        c[2] = STATUS[key][1]
    if key in COUNTS:
        assert c[3] == COUNTS[key][0], (key, c[3])
        c[3] = COUNTS[key][1]
    c[-2] = c[-2].rstrip() + " " + S + sentence
    lines[i] = " | ".join(c)

EDITS = [
    ("| A Gun's or a tank's HE | Infantry | Built, on the Infantry Target Type |",
     "| A Gun's or a tank's HE | Infantry | Built, on the Infantry Target Type. **Pass 60:** also at a Location with an unarmored vehicle, or with Known and concealed units together; AP, APCR, APDS, and HEAT by HE Equivalency |"),
    ("| A light mortar | Infantry | Built, on the Area Target Type; refused if the hex holds a vehicle or a manned Gun |",
     "| A light mortar | Infantry | Built, on the Area Target Type; refused if the hex holds a vehicle or a manned Gun. **Pass 60:** a vehicle, a manned Gun, and units on several levels of a building are attacked; friendly units in the hex stay refused |"),
    ("| Ordnance HE | A Gun | Built only when the Gun's crew is alone in its Location |",
     "| Ordnance HE | A Gun | Built only when the Gun's crew is alone in its Location. **Pass 60:** AP, HEAT, an ATR, and a mortar as well; an unattended Gun on the Infantry Target Type |"),
    ("| Small arms and MGs | An AFV | The vehicle is unharmed; a CE crew takes a Collateral Attack. MG To Kill is not built |",
     "| Small arms and MGs | An AFV | The vehicle is unharmed; a CE crew takes a Collateral Attack. **Pass 60:** a MG's To Kill attack at a named AFV is built |"),
    ("| FT, MOL, DC | An AFV | Refused (MOL, DC) or impossible to declare (FT) |",
     "| FT, MOL, DC | An AFV | Built (pass 60): the attack names the AFV in the target Location |"),
    ("| PF, PSK, ATR | An AFV | Built |\n",
     "| PF, PSK, ATR | An AFV | Built |\n| PF, PSK | Infantry | Built (pass 60): by HE Equivalency, at a target in a building, rubble, or behind a wall |\n"),
    ("and D3; no section's status changed.",
     "and D3; no section's status changed. **Pass 60 (2026-10-15, on its branch)** changed 66 rows of the inventory and the counts of fourteen sections (A9, A22, A23, B23, C1, C3, C7, C8, C11, C13, D.1 to D.8B, D2, D3, D5), and added its sentence to those rows and to B13, C9, and the table of section 5; A9 and C8 are now Mostly built, and C1 Mostly not built."),
]
out = "\n".join(lines)
for old, new in EDITS:
    assert out.count(old) == 1, old[:80]
for old, new in EDITS:
    out = out.replace(old, new)

limit = next(line for line in out.split("\n") if line.startswith("4. **Mixed Locations are refused, not resolved.**"))
assert out.count(limit) == 1
out = out.replace(limit, limit + " **Pass 60 (2026-10-15): Infantry with an unarmored vehicle, and Known with concealed units, are resolved on the Infantry Target Type, and a mortar's hit resolves a hex with a vehicle or units on several levels; an AFV in terrain with a positive TEM is attacked when an ordnance hit, or a FT, MOL, or DC that names it, reaches it.**")

# The summary by section, recounted from the rows of sections 3 to 6.
WORDS = ["Built", "Mostly built", "Partly", "Mostly not built", "Refused", "Not built"]
olines = out.split("\n")
s3 = next(i for i, line in enumerate(olines) if line.startswith("## 3. "))
s7 = next(i for i, line in enumerate(olines) if line.startswith("## 7. "))
by = collections.defaultdict(collections.Counter)
for line in olines[s3:s7]:
    m = re.match(r"^\| ([A-E])(\d+) \| [^|]* \| ([^|]+?) \| \d+: ", line)
    if m:
        word = m.group(3).split(",")[0].strip()
        assert word in WORDS, line[:60]
        by[m.group(1)][word] += 1
NAMES = {"A": "A. Infantry and basic rules", "B": "B. Terrain", "C": "C. Ordnance and OBA", "D": "D. Vehicles", "E": "E. Miscellaneous"}
total = collections.Counter()
changed = 0
for letter, name in NAMES.items():
    old = next(line for line in olines if line.startswith(f"| {name} | "))
    new = f"| {name} | {sum(by[letter].values())} | " + " | ".join(str(by[letter][w]) for w in WORDS) + " |"
    total.update(by[letter])
    if old != new:
        print(old, "->", new)
        out = out.replace(old, new)
        changed += 1
old = next(line for line in olines if line.startswith("| **A to E** | **105** |"))
new = f"| **A to E** | **{sum(total.values())}** | " + " | ".join(f"**{total[w]}**" for w in WORDS) + " |"
if old != new:
    print(old, "->", new)
    out = out.replace(old, new)
assert "\u2014" not in out and out.endswith("\n")
io.open(P, "w", encoding="utf-8", newline="").write(out)
print("sections:", len(found), "edits:", len(EDITS) + 1, "summary lines changed:", changed)
