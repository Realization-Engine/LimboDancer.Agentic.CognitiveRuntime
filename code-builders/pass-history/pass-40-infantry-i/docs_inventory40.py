"""Pass 40 documents: the rule inventory's rows that waited on pass 40. Each row keeps its note and gains a sentence; its status and the pass
that takes what is left are set here. The count tables are rewritten by pass 35's docs_inventory_tables.py and docs_coverage_counts.py.

Usage: python docs_inventory40.py [--check]
"""
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
B = "Pass 40 (2026-10-11): "
T = "ScenarioA1Pass40RulesTests"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]

# rule -> (status or None to keep it, the pass for what is left or "", the sentence added to the note, a test class or "")
SET = {
    "A1.31": ("partly", "Pass 80", "a Guard of prisoners and an Unarmed squad Deploy with no leader, and an unbroken Guard squad Deploys at will in any phase with no NTC (A20.5; ruling R40.21). Left: Finns, Carrier HS, U.S.M.C. 7-6-8s.", T),
    "A1.32": ("partly", "Pass 100", "only at the start of the RPh, before the side's other RPh actions (ruling R40.22); Guards and Unarmed HS with no leader. Left: the same terrain; Finns and Carrier HS (pass 80).", T),
    "A1.6": ("partly", "Pass 165", "one member, ScenarioA1Definitions.UnitSize, with the 4 and the 5; no caller hands it a vehicle or a Gun yet (ruling R40.25). Left: horses, an attendant crew's 4, a large vehicle's 5 from the catalog.", T),
    "A2.2": (None, "", "the deviation is now ruling R40.23: the board placed later names a shared hex, as VASL does.", ""),
    "A2.3": ("partly", "Pass 185", "at setup a butted half-hex is in a group's areas only under both boards' names (ScenarioA1SetupCalculator.InAreas). Left: entry and occupation.", T),
    "A2.4": (None, "Pass 135", "nothing built; moved by question F2.", ""),
    "A2.52": (None, "Pass 65", "read: an AFV sets up CE and no choice is offered; a CE or BU choice at setup is not built (design 11.4). SW dismantled is pass 45's, ordnance limbered pass 110's.", ""),
    "A2.9": (None, "Pass 125", "nothing built; Crest status is pass 125's and HD status pass 100's.", ""),
    "A3.2": (None, "Pass 150", "nothing built; moved by question F2, with entrenching for pass 115.", ""),
    "A3.9": (None, "Pass 185", "no card of the library prints a provisional turn or a chart past END; built with the first that does (ruling R40.24).", ""),
    "A4.11": ("partly", "Pass 65", "a unit that boards a vehicle this Player Turn has four MF. Left: after it dismounts; Riders.", T),
    "A4.13": (None, "Pass 135", "nothing built; moved by question F2.", ""),
    "A4.131": (None, "Pass 135", "nothing built; moved by question F2.", ""),
    "A4.132": ("partly", "Pass 135", "the road's rate is the mover's choice, and a mover at the road rate is fired on at the center dot or the point where it crossed the hexside (rulings R40.4 to R40.6). Left: the LOS to the hexside point outside the target hex, and a firer with a LOS to that point alone (backlog).", T),
    "A4.14": ("partly", "Pass 75", "the Infantry OVR of a lone Known SMC and the recapture of Unarmed units enter an enemy Location. Left: Human Wave; the Disrupted exception.", T),
    "A4.15": ("partly", "Pass 75", "built: the NTC with the TEM as its DRM, the doubled MF, a leader's NTC for the stack he moves with (rulings R40.12, R40.13). Left: a second OVR against a SMC in Melee; a SMC in an AFV; Hindrance in the DRM.", T),
    "A4.151": ("partly", "Pass 75", "the SMC's move away before any fire, into a Location the ATTACKER names, Open Ground left out where another is open (ruling R40.14). Left: minefields, FFE, and Wire among the Locations.", T),
    "A4.152": ("partly", "Pass 75", "the CC at once for an Infantry OVR and for a recapture, with Melee for the survivors (ruling R40.15). Left: the leader in the prefilled attack; a lone SMC's recapture; the recapture as a capture attempt.", T),
    "A4.4": ("built", "", "a SW dropped mid-move gives no MF back, and no item is portaged twice in a phase.", T),
    "A4.41": ("partly", "Pass 45", "a MMG or HMG that moved in the MPh, other than by Recovery, does not fire in the AFPh. Left: the German dismantled MMG or HMG as a LMG; INF and RCL SW, which the catalog does not hold.", T),
    "A4.42": ("partly", "Pass 45", "the leader's IPC goes to the laden MMC the mover names (ruling R40.11). Left: a hero's IPC; the choice in the APh; a CX leader, who still lends it; a SMC as the one helped.", T),
    "A4.43": (None, "Pass 125", "nothing built: the removal of unpossessed SW in marsh and water is pass 125's, in a Blaze pass 185's.", ""),
    "A4.44": ("partly", "Pass 45", "no Recovery in Bypass. Left: a SMC's Recovery at once when a unit is eliminated, surrenders, or routs away.", T),
    "A4.5": ("partly", "Pass 65", "no boarding after Double Time. Left: the bar for a unit that boards later in the turn.", T),
    "A4.51": (None, "", "read on its page in task 40.7; nothing changed.", ""),
    "A4.61": ("partly", "Pass 45", "MF spent within a Location beside the one change of Location do not prevent Assault Movement, and a break or berserk ends it. Left: it ends for the whole stack, where the page ends it for the unit (backlog).", T),
    "A4.62": ("partly", "Pass 110", "Hazardous Movement holds in every fire phase until the unit is pinned or broken or the Player Turn ends, against any attack, and a failed push counts (ruling R40.10). Left: every hazardous act but pushing a Gun.", T),
    "A4.63": ("partly", "Pass 45", "Dash is built: the declaration, the two steps, Area Fire for a firer to whom neither side of the road is Open Ground, Case J and the CA limit for ordnance (rulings R40.7 to R40.9). Left: the halving by firer in a Fire Group; the overstack MF of the Location Dashed to.", T),
    "A4.72": ("partly", "Pass 125", "SMOKE's MF are left out of what makes a hex Difficult Terrain. Left: the terrain not built.", T),
    "A5.11": ("built", "", "the MF for each excess squad-equivalent is charged in the MPh as in the APh, from one count of the stack.", T),
    "A5.12": ("built", "", "the +1 for each excess is on the IFT DR too, for small arms, MG, and vehicle fire.", T),
    "A5.13": ("built", "", "in the MPh the target's penalty is the movers' alone, and it is counted over the units the firer knows (ScenarioA1Stacking.TargetPenaltyApplies).", T),
    "A5.131": ("built", "", "the -1 for each excess is on the IFT DR too, for Personnel targets.", T),
    "A5.132": (None, "Pass 50", "not built; moved at the user's word of 2026-10-11 (a hit on another vehicle of the hex needs the To Hit's target choice).", ""),
    "A5.2": (None, "", "a side's vehicles above one in a Location now add to its attack penalty on the IFT, To Hit, and CC DR (ruling R40.3).", T),
    "A5.3": (None, "Pass 65", "nothing built; Riders are pass 65's.", ""),
    "A5.4": (None, "Pass 110", "nothing built in pass 40.", ""),
    "A5.5": ("built", "", "one count of a stack in every phase; a crew or HS manning a Gun is a squad in play as at setup; five SMC are a HS for every DR (ruling R40.2).", T),
    "A8.311": ("partly", "Pass 50", "a crew that fired its Gun adds its inherent FP to an FPF attack (ruling R40.18). Left: OVR Prevention (C5.64); A7.353's halved FP outside FPF (pass 45).", T),
    "A8.312": ("partly", "Pass 45", "built: while the attack is owed the DEFENDER's pass is refused and its fire is the Mandatory FG's, a unit already marked firing its MG with it (rulings R40.17, R40.19, R40.20). Left: one FG of units whose fire counters differ; a Gun's shot and other acts held behind the attack.", T),
    "A20.54": ("partly", "Pass 75", "Unarmed enemy units are entered in the MPh with no NTC and attacked in CC at once; an Unarmed mover and a stack of Dummies enter none. Left: a Guard's attack on a unit that enters its Location.", T),
    "B3.3": (None, "", "the deviation is now ruling R40.5: the road's rate is the default and the other terrain's cost the mover's choice.", ""),
    "B3.4": (None, "Pass 135", "the road hexside's choice of rate is built (A4.132); nothing else changed.", ""),
    "B3.41": (None, "Pass 145", "nothing built; moved by question F2, with Convoy for pass 175.", ""),
    "B13.31": ("partly", "Pass 135", "a woods-road hex is read as VASL names it, and a mover at the road rate there is in Open Ground to a firer whose LOS to its point crosses no woods (ruling R40.4). Left: the exception for vehicles is decided in Play (backlog).", T),
    "B13.32": (None, "Pass 135", "nothing built in pass 40.", ""),
    "B13.41": (None, "Pass 135", "nothing built; a vehicle leaving the road inside a woods-road hex, and motorcycles (pass 175).", ""),
    "C6.1": ("partly", "Pass 50", "Dashing Infantry is a Case J target. Left: the firer's half for a tank that only Started and Stopped.", T),
}


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

waiting = sorted(rule for rule, (_, c) in index_of.items() if c[6] == "Pass 40")
assert waiting == sorted(SET), (sorted(set(waiting) - set(SET)), sorted(set(SET) - set(waiting)))
if "--check" in sys.argv:
    print(len(waiting), "rows wait on pass 40, all named")
    sys.exit(0)

for rule, (status, target, sentence, test) in SET.items():
    index, c = index_of[rule]
    if status is not None:
        assert status in STATUSES
        c[3] = status
    c[6] = target
    if test and test not in c[5]:
        c[5] = (c[5] + "; " if c[5] else "") + test
    c[7] = (c[7].rstrip() + " " if c[7] else "") + B + sentence
    lines[index] = "| " + " | ".join(c) + " |"
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
print("rows changed:", len(SET))
