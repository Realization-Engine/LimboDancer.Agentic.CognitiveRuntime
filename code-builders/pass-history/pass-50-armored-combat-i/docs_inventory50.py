"""Pass 50 documents: the rule inventory's rows that waited on pass 50. Each row keeps its note and gains a sentence; its status and the pass
that takes what is left are set here. The count tables are rewritten by pass 35's docs_inventory_tables.py and docs_coverage_counts.py.

Usage: python docs_inventory50.py [--check]
"""
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
B = "Pass 50 (2026-10-12): "
T = "ScenarioA1Pass50RulesTests"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]

# rule -> (status or None to keep it, the pass for what is left or "", the sentence added to the note, a test class or "")
SET = {
    "A5.132": (None, "Pass 55", "not built (design 6, F1): it stands on vehicular overstacking, a subsequent dr, and a Random Selection by size.", ""),
    "A7.21": ("partly", "Pass 65", "a vehicle's BMG and CMG do not fire at another level of their own hex (ruling R50.5): refused as out of range. Left: TPBF against PRC from a higher Location (A7.211); IFE, which has no counter.", T),
    "A8.311": (None, "Pass 110", "nothing built: OVR Prevention (C5.64) is a non-vehicular Gun's rule (design 6, F1).", ""),
    "A8.4": ("built", "", "the remainder closes on the page: D3.5 gives a vehicle's MG one fire attack a Player Turn, so it has no Final Fire after its First Fire; its other weapons fire in the same fire phase, which for the DEFENDER joins the MPh and the DFPh (A.15; ruling R50.2).", T),
    "A9.3": ("built", "", "the remainder closes on the page: a vehicle's MG makes one attack a Player Turn (D3.5), so it has no Sustained Fire.", ""),
    "A9.6": (None, "Pass 60", "not built: task 50.5 was left out (design 9.1). A MG as an ordnance weapon differs from a LATW at every station of the ordnance shot.", ""),
    "A9.61": (None, "Pass 60", "not built: task 50.5 was left out (design 9.1).", ""),
    "A9.611": (None, "Pass 60", "not built: task 50.5 was left out (design 9.1); Collateral Attacks are pass 65's.", ""),
    "C2.2401": (None, "Pass 55", "not built: task 50.4 was left out (design 6, F5). It needs a declared attack that stands open between the two sides, an answer by the other side, and two resolutions in an order the Final DRs may decide. Bounding First Fire by ordnance, which it waited on, is built.", ""),
    "C3.22": (None, "Pass 55", "nothing built (design 8.1): a tank's turret turns with a shot and with an MP expenditure, not without fire at the end of a fire phase.", ""),
    "C5.13": ("built", "", "a vehicle's MA fires in its own MPh within its current TCA; a shot that would turn it is refused (covered-arc-fixed; play.ordnance-bounding-arc), and the CA changes by MP alone.", T),
    "C5.3": ("partly", "Pass 65", "Case C: 2 and the mount's 2 or 3, for every MPh shot of a vehicle's MA and for its AFPh shot after it entered a new hex, is in Motion, or fired in the MPh (rulings R50.10, R50.11); another MP between its shots (BoundingShotBar). Left: Passenger ordnance.", T),
    "C5.31": ("built", "", "Case C1, one more than Case C, for a continuous LOS of two and a half to three MP, read from the phase's vehicle steps and rounded up (ruling R50.8).", T),
    "C5.32": ("built", "", "Case C2, two more than Case C, for a continuous LOS of two MP or less.", T),
    "C5.33": ("partly", "Pass 55", "Case C for more than three MP, at the outset of the MPh, and for a LOS held since before it. Left: the Gun Duel clause (C2.2401).", T),
    "C5.35": ("partly", "Pass 55", "Case C4 for a vehicle's own MPh and AFPh: the applicable Case C and the lower dr of the TH DR doubled, added to the Final DR alone (ruling R50.9). Left: a Gyrostabilizer; a Motion firer of the side that is not phasing; Guns en portee.", T),
    "C5.4": (None, "Pass 110", "nothing built: the +2 for each instance of Area Fire on the firer is a LATW's case.", ""),
    "C6.1": ("partly", "Pass 55", "the firer's half is settled: a tank that only started and stopped takes Case B in the AFPh, and one that entered a new hex Case C (ruling R50.10). Left: the target's half for a vehicle that has paid only its Start MP (backlog 55.2); VBM as a cause.", T),
    "C6.16": ("built", "", "read at task 50.3: the MP left at the end of a move are spent in the vehicle's hex by the remain step, which adds them to the MP spent in its Location; a Delay MP adds to them too.", ""),
    "D1.8": ("partly", "Pass 105", "the BMG and CMG fire on the IFT by their printed FP (VehicleWeaponFp). Left: a vehicular FT, which has no counter.", T),
    "D1.81": ("partly", "Pass 55", "a BMG fires on the IFT at a target inside the VCA, with a Normal Range of eight hexes. Left: firing outside the VCA by a pivot of the hull (ruling R50.4); a hull RMG and a Fixed Mount, which have no counter.", T),
    "D1.82": ("partly", "Pass 110", "a CMG fires on the IFT through the TCA, turning the turret, with a Normal Range of twelve hexes and no leader beyond sixteen (A9.4). Left: the loss of the MA's Acquisition; a turret Rear MG and CMG VCA Only, which have no counter.", T),
    "D2.11": ("partly", "Pass 55", "nothing built: a VCA changed by firing outside the CA, at the end of a fire phase, or after a Motion Attempt is left (design 7.1, 8.1).", ""),
    "D2.17": ("built", "", "a Delay MP is a vehicle step of its own: one MP in place by a vehicle that is stopped or has not started, answered by the DEFENDER's window; it leaves the vehicle stopped (DelayBar).", T),
    "D2.18": (None, "Pass 55", "not built (design 8.1): a Delay MP after stopping gives what the rule is used for.", ""),
    "D2.35": ("partly", "Pass 55", "the bar holds for the BMG that now fires: it fires only inside the VCA, in Bypass as anywhere. Left: the changed TCA in Bypass (D2.321).", T),
    "D2.41": (None, "Pass 60", "nothing built: DC and MOL attacks on vehicles are pass 60's.", ""),
    "D2.42": ("partly", "Pass 65", "a vehicle's MA fires from Motion, or moving and not stopped, with Case C4 in its own MPh and AFPh. Left: Passengers' and Riders' Motion Fire; a Motion firer of the side that is not phasing.", T),
    "D3.11": ("partly", "Pass 55", "the VCA governs a BMG's fire: a target outside it is refused (play.fire-vehicle-vca). Left: the VCA changed for the shot.", T),
    "D3.12": ("partly", "Pass 55", "the TCA turns for a CMG's shot, and with a Delay MP, a stop, a VCA change, or an entry in the MPh (TurretStepBar). Left: with a Start MP; in woods, a building, or rubble, where the expenditure is doubled; at the end of a fire phase; the TCA turning with the hull.", T),
    "D3.2": ("partly", "Pass 60", "from inside the target's hex a Gun or a tank's MA fires with Case E, and the colored dr gives the Target Facing, for the hull and the turret alike (ruling R50.12). Left: FT, MOL, and DC from the same hex; a SW; Defensive First Fire at a vehicle entering the hex (C5.51).", T),
    "D3.3": ("partly", "Pass 65", "a tank's MA fires in its own MPh as Bounding First Fire, at the outset or once the DEFENDER has passed, leaves a Bounding Fire counter when its ROF is spent, and the vehicle may move on. Left: Passengers and Riders.", T),
    "D3.31": ("partly", "Pass 55", "a BMG or CMG fired in the MPh is halved and bars every weapon of the vehicle from the AFPh. Left: Canister and the Gyrostabilized CMG, which have no counter.", T),
    "D3.32": ("built", "", "the MA that kept its ROF in the MPh, with no other weapon fired there, fires once in the AFPh with Case C, or Case C4 in Motion (ruling R50.11).", T),
    "D3.5": ("partly", "Pass 65", "a BMG and a CMG each make one fire attack a Player Turn, added together or apart, in the fire phase of the vehicle's other weapons, under the vehicle's one fire counter (rulings R50.1, R50.2). Left: the fire phase of its PRC; IFE.", T),
    "D3.51": ("partly", "Pass 55", "the MA's first shot within a TCA its CMG changed in the fire phase pays the CMG's Case A, and the CMG's after the MA (ruling R50.3). Left: the Known-target condition; the TCA turning with a VCA change; the same for bow-mounted weapons.", T),
    "D3.52": ("partly", "Pass 55", "a CMG fired outside its TCA adds the To Hit DRM of Case A for the turret's type to its IFT DR, doubled in woods, a building, or rubble, and never in Bounding First Fire. Left: the BMG's NT penalty for a pivot.", T),
    "D3.54": (None, "Pass 60", "not built: task 50.5 was left out (design 9.1).", ""),
    "D3.7": ("partly", "Pass 105", "a tank's Gun MA, BMG, and CMG each malfunction and are repaired by their own dr, by a crew that is not shocked or stunned; a 6 disables the weapon, and a tank whose MA is disabled is Recalled (rulings R50.6, R50.7). Left: SA and a Hero Rider, which have no counter; the AAMG's two copies of the CE test, kept as they are.", T),
}
RENAMED = [("ScenarioA1Pass7Tests.AMovingFirerIsOutside", "ScenarioA1Pass7Tests.AMovingFirerTakesCaseCInTheAfphAndAMotionFirerIsOutsideInThePfph")]


def cells(line):
    return [cell.strip() for cell in line.split("|")[1:-1]]


index_of = {}
chapter = None
for index, line in enumerate(lines):
    if line.startswith("### Chapter "):
        chapter = line[len("### Chapter "):][0]
    if chapter and line.startswith("| ") and not line.startswith("| §") and not line.startswith("|---"):
        c = cells(line)
        if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
            index_of[c[0]] = (index, c)

waiting = sorted(rule for rule, (_, c) in index_of.items() if c[6] == "Pass 50")
assert waiting == sorted(SET), (sorted(set(waiting) - set(SET)), sorted(set(SET) - set(waiting)))
if "--check" in sys.argv:
    print(len(waiting), "rows wait on pass 50, all named")
    sys.exit(0)

for rule, (status, target, sentence, test) in SET.items():
    index, c = index_of[rule]
    if status is not None:
        assert status in STATUSES
        c[3] = status
    c[6] = target
    if test and test not in c[5]:
        c[5] = (c[5] + "; " if c[5] and c[5] != "none found" else "") + test
    c[7] = (c[7].rstrip() + " " if c[7] else "") + B + sentence
    lines[index] = "| " + " | ".join(c) + " |"
out = nl.join(lines)
for old, new in RENAMED:
    out = out.replace(old, new)
open(P, "w", encoding="utf-8", newline="").write(out)
print("rows changed:", len(SET))
