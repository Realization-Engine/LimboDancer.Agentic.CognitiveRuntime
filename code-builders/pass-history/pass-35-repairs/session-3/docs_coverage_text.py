"""Pass 35 documents: the coverage document's text: the status line, a sentence in each touched section's gaps cell, and a mark on each fault of 8.3 the pass repaired."""
import re

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Coverage.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
assert "Pass 35 (2026-10-09)" not in text

GAPS = {
    "A10": "Open Ground for a rout and for Interdiction is read by the FFMO a given enemy could apply (R35.1); a rout into concealed units is repulsed (R35.3); a unit bound to surrender owes no rout (R35.2); a Fanatic's Interdiction NMC is right; no voluntary rout without DM (R35.4).",
    "A12": "the enemy that forces a concealed mover's loss of \"?\" is Good Order, not hidden, and not a Passenger (R35.5).",
    "A17": "the Wound Severity dr is made in every path.",
    "A19": "a Disrupted unit routs only when it must, and never by Low Crawl.",
    "A20": "a Commissar does not surrender in the RtPh; a repulsed unit may (R35.2, R35.3).",
    "A25": "a Russian 4-2-6 Battle Hardens to the 5-2-7.",
    "B1": "Interdiction and the must-rout test read Height Advantage, hexside TEM, and AFV or wreck cover, as fire does (R35.1).",
    "B14": "vehicles pay the Open Ground cost; the orchard's season in the LOS (14.2) moved to pass 135.",
    "B15": "out-of-season grain on a LOS is Open Ground; only a game that names no month leaves it undecided.",
    "B16": "ordnance HE is halved into a marsh (R35.6), and a hex beside a marsh is a Bog hex.",
    "B23": "Locations a stairwell joins are ADJACENT, with A.8 (R35.9).",
    "B25": "a wreck burned in Close Combat gets its Blaze.",
    "C3": "HE is halved into a marsh (3.53).",
    "C5": "rubble counts as a firer's terrain in Cases A, B, C3, and E and for the CA lock; an Opportunity Firer takes no Case B or AFPh C3; the own-hex Hindrance of Case E is read.",
    "C6": "Case O is built; the ATR takes Case L; an Acquisition is lost by a move or a turn without firing, and follows a vehicle target.",
    "C10": "a towing vehicle is refused Bypass, a wall or hedge, and rubble.",
    "C13": "Backblast from rubble; no AFPh +2 for an Opportunity Firer's LATW.",
    "D1": "a CE RST AFV adds neither MA nor CMG to an OVR, and no CMG to CC.",
    "D2": "the BU road rate for an AFV that is BU by default; no beneficial TEM in Bypass on the Vehicle Target Type; the ESB table by nationality; Non-Stopped read from the move in hand (R35.9).",
    "D5": "the Stun +1 on the OVR and CC DR; no CE change after Bounding First Fire; a bogged Recalled AFV is Abandoned, ESB is barred to it, and it may Stop to unload.",
    "D7": "the CC counter of CC Reaction Fire bars Non-CC Reaction Fire.",
    "D8": "a bogged vehicle loads and unloads; no Case J for Bog Removal MP in the Bog hex.",
    "D9": "a Bypassing AFV or wreck hinders only a LOS that touches its hexside (R35.8).",
    "E1": "the night's MP in VBM and on the Recall exit, after the Reverse multiplier (R35.7); the unload from a BU AFV at an NVR of 0.",
    "E3": "the weather's costs in Infantry Bypass and VBM, after the Reverse multiplier (R35.7); an unpaved road in Mud for Infantry.",
}

REPAIRED = [
    "- **A.8 ADJACENT too narrow.**", "- **A.18 Morale ceiling.**", "- **A.7 Good Order.**",
    "- **A rout into concealed or hidden enemy units is accepted (A10.533, A12.15).**", "- **Interdiction ignores Fanaticism (A10.8, A10.53).**",
    "- **A leader with a MG Interdicts (A10.532).**", "- **Recorded already, still true:** a leader's Casualty Reduction in a rout",
    "- **Wound Severity dr skipped", "- **A Disrupted unit may rout and Low Crawl**", "- **Commissars surrender in the RtPh (A20.21).**",
    "- **Russian Conscript Battle Hardening (A25.2, p. 93).**", "- **B.2, SMOKE's MF is not doubled uphill.**",
    "- **B.10, a total of 6 from SMOKE or vehicles is not a block.**", "- **B1.14, B1.16, B1.17, Interdiction where FFMO could not apply.**",
    "- **Interdiction and the rout's Open Ground test ignore wall or hedge TEM and Height Advantage.**", "- **Vehicles refused in an orchard**",
    "- **Fire through out-of-season grain refused**", "- **No Bog Check next to marsh**", "- **HE into marsh at full FP**",
    "- **A wreck burned in Close Combat does not burn.**", "- **Vertical ADJACENT is missing (B23.25).**",
    "- **Ordnance HE into a marsh is not halved (C3.53, p. 170).**", "- **Rubble is left out of the firer's terrain.**",
    "- **Acquisition is kept when the rule removes it**", "- **Acquisition does not follow a vehicle target**", "- **ATR denied Case L**",
    "- **Opportunity Fire by ordnance takes Case B**", "- **Case O missing where Hazardous Movement exists.**", "- **Case E Hindrance set to zero.**",
    "- **A vehicle towing a Gun may use Bypass.**", "- **A vehicle towing a Gun may cross a hedge.**",
    "- **A PF or PSK fired from rubble takes no Case C3 and no restriction.**", "- **An Opportunity Firer's LATW shot in the AFPh takes Case C3.**",
    "- **D2.16, BU road rate (p. 196).**", "- **D2.38, TEM in Bypass (p. 198).**", "- **D1.321, RST and the CMG (p. 194).**",
    "- **D2.5, ESB nationality DRM (p. 198), latent.**", "- **Stun +1 missing on an OVR DR (D5.34, p. 203).**",
    "- **Stun +1 missing on the vehicle's CC DR (D5.34).**", "- **The CC counter of CC Reaction Fire is never read (D7.21, p. 207).**",
    "- **A Bypassing AFV or wreck always hinders (D9.4, p. 210).**", "- **A bogged vehicle cannot unload (D8.5, D6.5).**",
    "- **ESB by a Recalled AFV is not barred", "- **CE status after Bounding First Fire (D5.33).**", "- **A bogged Recalled AFV (D5.341).**",
    "- **Probable: Case J against a vehicle spending Bog Removal MP (D8.4, p. 209).**", "- **VBM pays no night or weather MP.**",
    "- **Infantry Bypass pays no weather MF.**", "- **Infantry on an unpaved road in Mud,",
    "- **In Mud a road gap in a wall may be charged as a wall for a vehicle (unsettled).**",
    "- **Reverse movement multiplies the night and weather MP (unsettled).**",
]
REFUSED = ["- **Counters placed and ignored**", "- **Fortification counters are silent**"]
MOVED = {"- **Orchard always in season**": "**Moved to pass 135 (2026-10-09).** "}

start = next(i for i, line in enumerate(lines) if line.startswith("## 3. "))
end = next(i for i, line in enumerate(lines) if line.startswith("## 7. "))
done = set()
for i in range(start, end):
    m = re.match(r"^\| ([A-E]\d+) \|", lines[i])
    if m and m.group(1) in GAPS and m.group(1) not in done:
        cells = lines[i].split(" | ")
        assert len(cells) >= 6, lines[i][:80]
        cells[-2] = cells[-2].rstrip() + " **Pass 35 (2026-10-09):** " + GAPS[m.group(1)]
        lines[i] = " | ".join(cells)
        done.add(m.group(1))
assert done == set(GAPS), sorted(set(GAPS) - done)

f_start = next(i for i, line in enumerate(lines) if line.startswith("### 8.3 "))
f_end = next(i for i, line in enumerate(lines) if line.startswith("### 8.4 "))
marked = 0
for prefixes, mark in ((REPAIRED, "**Repaired in pass 35 (2026-10-09).** "), (REFUSED, "**Refused since pass 35 (ruling R35.10): a game with such a counter is not set up or played.** ")):
    for prefix in prefixes:
        hits = [i for i in range(f_start, f_end) if lines[i].startswith(prefix)]
        assert len(hits) == 1, (prefix, len(hits))
        lines[hits[0]] = "- " + mark + lines[hits[0]][2:]
        marked += 1
for prefix, mark in MOVED.items():
    hits = [i for i in range(f_start, f_end) if lines[i].startswith(prefix)]
    assert len(hits) == 1, prefix
    lines[hits[0]] = "- " + mark + lines[hits[0]][2:]
    marked += 1

intro = next(i for i in range(f_start, f_end) if lines[i].startswith("143 entries"))
lines[intro] += (" **Pass 35 (2026-10-09)** repaired 51 of them and made two refusals of two more; each is marked where it stands, and the entry's text is kept as the audit wrote it. "
                 "The pass's design, sections 7 to 13, says what each repair is.")

status = next(i for i, line in enumerate(lines) if line.startswith("**Status:**"))
lines[status] += (" **Pass 35 (2026-10-09, on its branch, not yet merged)** changed the rows of 25 sections and 59 rows of the inventory; the counts of sections 3 to 6 are "
                  "recomputed from the inventory, and the faults of section 8.3 it repaired are marked there.")
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
print("sections:", len(done), "faults marked:", marked)
