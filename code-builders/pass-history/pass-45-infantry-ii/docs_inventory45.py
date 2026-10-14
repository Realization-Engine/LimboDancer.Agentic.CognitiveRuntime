"""Pass 45 documents: the rule inventory's rows that waited on pass 45. Each row keeps its note and gains a sentence; its status and the pass
that takes what is left are set here. The count tables are rewritten by pass 35's docs_inventory_tables.py and docs_coverage_counts.py.

Usage: python docs_inventory45.py [--check]
"""
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
B = "Pass 45 (2026-10-12): "
T = "ScenarioA1Pass45RulesTests"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]

# rule -> (status or None to keep it, the pass for what is left or "", the sentence added to the note, a test class or "")
SET = {
    "A.7": ("built with a deviation", "", "a TI unit, being in Good Order, Deploys, Recombines, Transfers, Drops, and Recovers in the RPh; Recovery in the MPh and dismantling stay barred (ruling R45.6). The SW sense is what the fire package already asks of a weapon and its possessor. The deviation: the Close Combat calculator's own definition, kept on purpose.", T),
    "A.11": ("built", "", "a weapon whose B# an attack lowered (Sustained Fire, capture, Inexperience) is removed on a DR at least its printed B# (FireWeaponEffect.Removed), for attacks planned from the pass on (ruling R45.4).", T),
    "A4.41": ("partly", "Pass 110", "a German dm MMG or HMG fires as a LMG, and so in the AFPh after moving. Left: INF and RCL SW, which the catalog does not hold; a pushed Gun.", T),
    "A4.42": ("partly", "Pass 75", "a leader who Double Times with his stack, or is CX, lends no IPC (A4.52). Left: a hero's IPC; the choice in the APh; a SMC as the one helped; the unasked default kept in the record.", T),
    "A4.44": ("partly", "Pass 75", "a TI unit Recovers in the RPh (ruling R45.6). Left: a SMC's Recovery at once when a unit is eliminated, surrenders, or routs away.", T),
    "A4.61": (None, "Pass 75", "nothing built: ending Assault Movement for one unit of a stack needs a Final DR for each kind of target (design 11.3).", ""),
    "A4.63": (None, "Pass 75", "nothing built: the overstack MF of the Location Dashed to is left; the halving by firer stays as ruling R40.9 has it.", ""),
    "A7.21": ("partly", "Pass 50", "fire at the next level of the firer's own hex, up or down a stairwell, is PBF at range 0 (ScenarioA1FireMapRules.SameHexLevelBar). Left: a vehicle's MG at another level; IFE, which has no counter.", T),
    "A7.212": ("built", "", "the limit is read for a Gun's, a mortar's, and a LATW's shot, and at a mortar's Spotter's Location; the refusal has its test.", T),
    "A7.25": ("partly", "Pass 110", "the note was stale: Opportunity Fire by ordnance and LATW was built by pass 35 (task 35.10). Left: Intensive Fire by an Opportunity Firer.", ""),
    "A7.302": ("built", "", "Casualty Reduction eliminates a crew, and so a prisoner crew that fails a MC (A20.54).", T),
    "A7.34": ("built", "", "a squad whose Range is underscored (the catalog's trait) sprays with its own FP to three hexes, halved again beyond Normal Range; a squad with no option does not (ruling R45.5).", T),
    "A7.353": ("built", "", "a crew that fired its Gun keeps its FP in Subsequent First Fire and in Final Fire against an adjacent unit, as in FPF; the rule's halving is the Area Fire's own.", T),
    "A7.5": ("partly", "Pass 115", "the Locations of a group are one chain (GroupMapFacts); vertical and wall or hedge ADJACENT were built by pass 35. Left: the pillbox clause.", T),
    "A7.51": (None, "Pass 65", "nothing built in pass 45: a vehicle's MG, Passengers, and Riders in a group are D6.64's and D3.5's.", ""),
    "A7.53": ("partly", "Pass 75", "a leader directs the later shots of a MG he directed in the phase (FireDirector.DirectedTheseMgs). Left: only firers he directed in First Fire for his later direction.", T),
    "A7.54": ("built", "", "a berserk unit joins no multi-Location group (berserk-group-outside).", T),
    "A7.7": ("partly", "Pass 110", "a LOF from the Locations directly above and below Encircles, and Spraying Fire counts. Left: an ordnance hit and vehicular armament as valid fire; the +1 on an Encircled unit's To Hit DR; Vulnerable PRC (pass 65).", T),
    "A7.72": ("partly", "Pass 140", "built: a unit in an upper building level with no path to ground level free of an unbroken, armed, unconcealed enemy unit is Encircled, by a search read when asked (ScenarioA1UpperLevelEncirclement). Left: a Blaze on the path; Scaling.", T),
    "A7.9": ("built", "", "the cowering unit and all its SW are marked Final or Prep Fire, one or more of a group by Random Selection; a berserk firer never cowers; the double shift reads Inexperience (ruling R45.4).", T),
    "A8.2": ("partly", "Pass 110", "the Residual FP is held to 12 after its reduction. Left: Residual FP from an ordnance hit or a To Kill DR; an attack on a MF expenditure inside the Location; FFE Hindrance (pass 150).", T),
    "A8.22": ("partly", "Pass 75", "a vehicle is attacked by a Location's Residual FP once, and again only when the FP grew or its DRM fell (ResidualAttacksAgain). Left: the same for Infantry MF spent in the Location; the activity first.", T),
    "A8.221": ("partly", "Pass 75", "a weapon that malfunctions on the attack's DR leaves no Residual FP. Left: Ammunition Shortage.", T),
    "A8.23": ("built", "", "a weapon that keeps its Multiple ROF leaves no Residual FP of its own, and the DEFENDER may give the ROF up at once after the attack (asl.game.forfeit-rof).", T),
    "A8.24": (None, "Pass 75", "not built: Spraying Fire is still refused in the MPh (design 10.1).", ""),
    "A8.25": (None, "Pass 110", "not built: an ordnance hit is resolved in the ordnance record, which the Residual FP check does not read (design 9.1).", ""),
    "A8.26": ("partly", "Pass 110", "CX, an Encircled firer's +1, a hero's, a Stun, and firer overstacking lower the Residual FP. Left: BU; Air Bursts raising it.", T),
    "A8.31": ("built with a deviation", "", "FPF fires in one group with units not using it, each firer by its own counter (ruling R45.1, replacing R12.3's one kind an attack); an FPF firer Replaced by its own NMC commits. The deviation: the CC counter after a TPBF FPF is not placed.", T),
    "A8.312": ("built", "", "the Mandatory FG is every unit that owes the attack, whatever its counter (ruling R45.1, closing R40.17), and a Gun's shot, a thrown DC, and a Starshell wait for it (ruling R45.2, replacing R40.19).", T),
    "A8.4": ("partly", "Pass 50", "Final Fire reaches the next level of the firer's hex. Left: a vehicle's Final Fire after its First Fire (ruling R6.9).", T),
    "A9.21": ("partly", "Pass 75", "a MMG or HMG in woods, rubble, or a building fires again in a phase only inside the CA of its earlier shots (FieldOfFireBar). Left: the pinned operator's CA to the DFPh; a stairwell's CA; INF, RCL, and 20L ATR.", T),
    "A9.22": (None, "Pass 135", "nothing built in pass 45 (design 10.1).", ""),
    "A9.221": (None, "Pass 135", "nothing built: the Alternate Hex Grain and the Fire Lane Snap Shot need a LOS along a hexspine and to a hexside.", ""),
    "A9.222": (None, "Pass 135", "nothing built in pass 45.", ""),
    "A9.223": ("partly", "Pass 60", "an attack at its firers' own Location by a lane's MG or its manning Infantry is allowed and cancels the Fire Lane. Left: CC Reaction Fire.", T),
    "A9.3": ("partly", "Pass 50", "the printed B# is an X# under Sustained Fire, and a lone SMC's MG uses none. Left: a vehicle's MA (ruling R6.9).", T),
    "A9.4": ("partly", "Pass 75", "a MG fires beyond 16 hexes only when a leader directs it, halved at 17 and more. Left: the exception for broken, berserk, or overstacked Infantry; no effect against armored targets.", T),
    "A9.5": ("partly", "Pass 135", "two levels of one building hex, and Spraying Fire counted toward Encirclement. Left: the MPh (pass 75); Bypass and road points.", T),
    "A9.52": ("partly", "Pass 75", "every member of a spraying group must be capable: a MG, or a squad with the option (ruling R45.5, replacing R12.6's leave). Left: a moving unit and an empty Location in the MPh; paratroops (pass 170).", T),
    "A9.72": ("partly", "Pass 110", "a captured SW is not repaired. Left: a mortar's repair, which needs its Repair number in the catalog; a Gun's repair by its crew.", T),
    "A9.73": (None, "Pass 75", "not built: an action of its own (design 11.1).", ""),
    "A9.8": ("partly", "Pass 60", "the German HMG and the light mortars of four PP or more are dismantled, by a Rules member; a dismantled mortar is not fired; a German dm MMG or HMG fires as a LMG. Left: starting dm; capture; Scrounged weapons.", T),
    "B23.25": ("built", "", "fire reads the vertical ADJACENT of pass 35: a LOS between two levels of one hex exists only up or down a stairwell.", T),
    "B23.26": ("built", "", "attacks up or down a stairwell go to the next level only, as PBF; a farther level is refused (play.fire-level).", T),
    "C6.44": ("partly", "Pass 110", "a MMG or HMG of the Scenario Defender Bore Sights a Location at setup, and deducts 2 when fired alone at it in Defensive First Fire (ruling R45.3). Left: IFE; the loss for good when it leaves its setup Location; a light mortar's.", T),
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

waiting = sorted(rule for rule, (_, c) in index_of.items() if c[6] == "Pass 45")
assert waiting == sorted(SET), (sorted(set(waiting) - set(SET)), sorted(set(SET) - set(waiting)))
if "--check" in sys.argv:
    print(len(waiting), "rows wait on pass 45, all named")
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
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
print("rows changed:", len(SET))
