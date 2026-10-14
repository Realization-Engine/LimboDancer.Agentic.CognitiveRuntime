"""Pass 35 documents: the rule inventory's rows, and its two count tables recomputed from the rows.

Each entry: rule -> (status, pass for what is left or "", text added to Where, text added to Test, the new note).
A note of None keeps the row's note and appends the given addition instead (the ADD table).
"""
import collections
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
B = "Pass 35 (2026-10-09): "

SET = {
    "A.7": ("partly", "Pass 45", "ScenarioA1Definitions.cs: GoodOrderOf, FreeToActAsPlanned", "ScenarioA1Pass35RulesTests; BacklogPass35Tests",
            B + "one definition for the planner (GoodOrderOf: not broken, berserk, captured, stunned, shocked, or in Melee; a TI unit is Good Order), ruling R35.5. The SW and Deployment actions keep their bar on a TI unit under its own name. Left: the Good Order sense of a SW, the TI rules for those actions, and the Close Combat calculator's own definition (no Melee clause, kept on purpose)."),
    "A.18": ("built", "", "ScenarioA1Definitions.cs: MoraleCeiling", "ScenarioA1Pass35RulesTests",
             B + "no Morale Level is raised beyond 10, wherever one is raised (fire, the rally, the PAATC, Leader Creation, the rout's broken Morale Level); the berserk Fanatic's 11 is withdrawn (ruling R35.5)."),
    "A10.531": ("partly", "Pass 100", "ScenarioA1RoutCalculator.cs: CouldApplyFfmo, RoutCoverFacts; GamePlanner.Rout.cs: InterdictionCover", "ScenarioA1RoutRallyRulesTests; BacklogPass35Tests",
                B + "Open Ground for the must-rout test and for Interdiction is the FFMO the particular enemy could apply: no Hindrance of any kind on the LOS, and no TEM, Height Advantage, or wreck or AFV cover the unit could claim (ruling R35.1, which replaces R13.3's test by the terrain's name). Left: the shellhole and entrenchment cases, and the Emplaced Gun's crew."),
    "A10.533": ("partly", "Pass 70", "ScenarioA1RoutCalculator.cs: RoutRepulse, RoutRepulseShown; RoutStepped.Attempted", "BacklogPass35Tests",
                B + "a rout step into a Location of concealed or hidden enemy units is repulsed: one real unit loses its \"?\" (Random Selection among several), the routing unit stays where it was and its rout ends; Dummies alone are removed and the rout goes in (ruling R35.3). A repulsed unit that ends ADJACENT to its enemy surrenders when a captor can take it and is eliminated otherwise, where the rule's words give elimination: a deviation the ruling records. Left: a concealed unit giving up its \"?\" by choice to Interdict or to change the route."),
    "A17.11": ("built", "", "ScenarioA1Wounds.cs: SeverityDue, Mortal; ScenarioA1RoutCalculator.cs: CasualtyReduction; ScenarioA1OrdnanceEventRules.cs: Casualty", "ScenarioA1Pass35RulesTests; BacklogPass35Tests",
               B + "one wound procedure: the Wound Severity dr is rolled and recorded in a rout's Interdiction, a Mopping Up casualty, and a PF firer's Casualty Reduction as elsewhere, with +1 for a man already wounded; a SMC's Casualty Reduction with no dr throws. No LLMC follows a mortal wound outside fire (backlog)."),
    "A19.12": ("partly", "Pass 75", "ScenarioA1RoutCalculator.cs: MayRout, DisruptedLowCrawlBar", "ScenarioA1RoutRallyRulesTests; BacklogPass35Tests",
               B + "a Disrupted unit routs only when it must and never by Low Crawl. Built before: Disruption when no lesser unit exists, never for Fanatic or a Commissar, no Self-Rally, surrender in the RtPh and when enemy units advance in, elimination in Melee. Left: surrender in other phases (backlog, R14.11); the SS against Russians and PRC exceptions."),
    "A25.2": ("partly", "Pass 80", "ScenarioA1FireReference.cs: Hardened", "ScenarioA1FireExtensionTests (corrected)",
              B + "a 4-2-6 Battle Hardens to the 5-2-7 and a 2-2-6 to the 2-2-7, as the rule names them. Russian no-Deploy is built (R31.4). Left: A25.211's exception before 1941; the Guards Depletion Number +1; the A20.5 and A21.22 Deploy exceptions."),
    "B.2": ("built", "", "ScenarioA1TerrainCosts.cs: GroundStep", "ScenarioA1Pass35RulesTests",
            B + "SMOKE's MF, and a burning wreck's, is doubled with the terrain's one level up (4 MF, the rule's own example), and added once across an Abrupt Elevation Change."),
    "B.10": ("built", "", "ScenarioA1FireMapRules.cs: HindranceBlocks, LocationLos", "ScenarioA1Pass35RulesTests",
             B + "a Hindrance total of six blocks the LOS whatever its sources: the terrain's count with the vehicles', wrecks', and SMOKE's is tested where the total is formed."),
    "B1.14": ("built", "", "ScenarioA1RoutCalculator.cs: CouldApplyFfmo", "ScenarioA1RoutRallyRulesTests",
              B + "Interdiction and the must-rout test read Height Advantage as fire does (ruling R35.1); shown in the Studio game p35-rout."),
    "B1.16": ("partly", "Pass 100", "ScenarioA1RoutCalculator.cs: CouldApplyFfmo", "ScenarioA1RoutRallyRulesTests",
              B + "Interdiction across a wall or hedge hexside is prevented by its TEM, as in fire (ruling R35.1). A wall whose TEM fire refuses to decide (a Hillside wall) gives none. Left: Crest and cliff hexsides, refused."),
    "B1.17": ("partly", "Pass 115", "ScenarioA1RoutCalculator.cs: CouldApplyFfmo", "ScenarioA1RoutRallyRulesTests",
              B + "for Interdiction a wreck or AFV in the hex, and SMOKE or a vehicle along the LOS, count as in fire (ruling R35.1). Left: Roadblocks, not built."),
    "B14.2": ("partly", "Pass 135", "", "",
              "LOS has both the in-season obstacle and the \"Orchard, Out of Season\" terrain, but nothing ties the season to GameState.ScenarioMonth, so every orchard is in season; some out-of-season LOS cases are refused (OrchardOutOfSeasonCases); fire leaves any orchard Hindrance undecided. Moved from pass 35 to pass 135 at the user's word of 2026-10-09 (the pass 35 design, section 9): the LOS is in Maps, and pass 135's task 135.2 decides every Hindrance in fire."),
    "B14.4": ("built", "", "ScenarioA1VehicleTerrainCosts.cs (the orchard rows)", "ScenarioA1Pass35RulesTests",
              B + "a vehicle pays the Open Ground cost in an orchard (three rows of the vehicle terrain table); Infantry already did."),
    "B15.2": ("built", "", "ScenarioA1FireMapRules.cs: LocationLos; ScenarioA1FireCalculator.cs; ScenarioA1OrdnanceCalculator.cs", "ScenarioA1FirePackageTests.GrainOutOfSeasonOnALosIsOpenGround; ScenarioA1OrdnanceTests.GrainOnAGunsLosIsDecidedOnceTheGameNamesItsMonth",
              B + "+1 per grain hex at the same level from June to September, and Open Ground with no Hindrance outside it (B15.6). Only a game that names no month leaves grain on a LOS undecided. Studio games p35-grain-nov and p35-grain-jul."),
    "B16.31": ("built", "", "ScenarioA1FireCalculator.cs (the hit's FP)", "ScenarioA1OrdnanceTests.HeIsHalvedIntoAMarsh",
               B + "ordnance HE is halved into a marsh, after a Critical Hit's doubling (ruling R35.6, which ends R10.1's leave-out)."),
    "B16.43": ("built", "", "ScenarioA1VehicleTerrainCosts.cs: EntryCost (beside a marsh)", "ScenarioA1Pass35RulesTests.AHexBesideAMarshIsABogHexOffTheRoad",
               B + "a ground level or level -1 hex adjacent to a marsh is a Bog hex for a vehicle entering by a non-road hexside; Play reads the hex's neighbours."),
    "B25.14": ("partly", "Pass 185", "GamePlanner.CloseCombat.cs (the Blaze entity after the wreck)", "AWreckBurnedInCloseCombatCarriesItsBlaze",
               B + "a wreck burned in Close Combat gets its Blaze entity, as one burned by fire or ordnance does. Left: FT and MOL against vehicles (refused); no spread to terrain."),
    "C3.53": ("partly", "Pass 125", "ScenarioA1FireCalculator.cs (the hit's FP)", "ScenarioA1OrdnanceTests.HeIsHalvedIntoAMarsh",
              B + "HE is halved into a marsh (ruling R35.6). Not halved for concealment, a pinned firer, or the AFPh. Left: fording units (not built). Inventory: Not in the PDF outline; found as a rule head on the page."),
    "C5.1": ("partly", "Pass 110", "ScenarioA1Definitions.cs: WoodsOrBuilding (rubble)", "ScenarioA1Pass35RulesTests",
             B + "Case A is doubled for a firer in rubble as in woods or a building. T, ST, and NT values are right. Left: 360-degree mounted Guns (refused), bow-mounted Guns and SA."),
    "C5.11": ("partly", "Pass 105", "ScenarioA1Definitions.cs: WoodsOrBuilding (rubble)", "ScenarioA1Pass35RulesTests",
              B + "the doubling and the CA lock hold in rubble as in woods and buildings. Left: a VCA pivot by a non-turreted vehicle with its Bog Check (refused); the woods-road exception."),
    "C5.2": ("built", "", "ScenarioA1OrdnanceCalculator.cs: OpportunityFire, AfphFire; ScenarioA1OrdnanceEligibility.cs: OpportunityFirer", "AnOpportunityFirersGunFiresInTheAfphWithoutCaseBAndOnItsKeptRof",
             B + "Case B is +3 in rubble as in woods or a building, and an Opportunity Firer's Gun takes no Case B and keeps its ROF in the AFPh (A7.25)."),
    "C5.34": ("partly", "Pass 110", "ScenarioA1OrdnanceMapRules.cs: IsBackblastLocation; ScenarioA1OrdnanceCalculator.cs: OpportunityFire", "ScenarioA1Pass35RulesTests",
              B + "ground-level rubble is a Backblast Location, and an Opportunity Firer's LATW takes no AFPh +2. Left: the Desperation choice (C13.81)."),
    "C5.5": ("partly", "Pass 105", "ScenarioA1VehicleSightRules.cs: OwnHexHindrance; ScenarioA1Definitions.cs: WoodsOrBuilding", "ScenarioA1Pass35RulesTests",
             B + "a wreck, SMOKE, or an AFV in the Gun's own hex, other than firer and target, adds its Hindrance as Case R, and Case E is doubled in rubble. A live AFV in the hex cannot be reached in play: the gate refuses an Infantry Target Type shot at a Location holding any vehicle (backlog). Left: a vehicle as firer or target within a hex, Bypass and the CAFP."),
    "C6.3": ("built", "", "ScenarioA1ArmorCalculator.cs (Case L for the ATR); GamePlanner (VehicleTarget: Non-Stopped)", "ScenarioA1Pass35RulesTests",
             B + "the ATR takes Case L; only a non-ATR LATW on its own To Hit Table is denied it. No Case L against a Non-Stopped vehicle, which is also one started in its MPh and not stopped since (ruling R35.9)."),
    "C6.5": ("partly", "Pass 110", "ScenarioA1FireFollowUps.cs: AcquisitionLostByMoveOrTurn; GameProjector.cs: KeepAcquisitions", "AnAcquisitionIsLostWhenTheGunTurnsWithoutFiringOrIsPushed",
             B + "an Acquisition is lost when its firer leaves its Location (pushed, towed, a tank's move, a mortar carried off) or turns without having fired on its target this phase; the projector decides it, so an older record may replay with a counter dropped earlier (the design's section 14). Left: loss to a CMG's or a CC attack on another target, the crew's Inherent FP, SW, or Interdiction, limbering; the 20mm test; a bridge target; the Gyrostabilizer (C6.55)."),
    "C6.51": ("built", "", "ScenarioA1ResultTables.cs: AcquiredVehicle", "AnAcquisitionFollowsAVehicleAndStaysWhereItLeftTheGunsLos",
              B + "a vehicle target and a tank's own Acquisition are followed as a Gun's at Infantry is: the counter goes with its target in the acquirer's LOS and stays in the last Location it held there."),
    "C6.6": ("built", "", "ScenarioA1OrdnanceCalculator.cs: MoverDrm; ScenarioA1FireMapRules.cs: HazardousMovement", "AGunFiringAtACrewPushingItsGunTakesCaseOAndNoCaseJ",
             B + "Case O, -2, against a target in Hazardous Movement, never with Case J or its subcases, on the Infantry and Area Target Types."),
    "C10.1": ("partly", "Pass 110", "ScenarioA1VehicleTerrainCosts.cs: EntryCost, TowingBypassBar, BypassHalfMp", "ScenarioA1Pass35RulesTests",
              B + "a towing vehicle is refused a wall or hedge hexside (a road's gap excepted), rubble, and VBM. Built before: T# at most M#, +1 MP per hex, no fire in tow, Reverse refused. Left: Deep Stream, wagons (MF), the 76 to 107mm MTR exception, Narrow Streets."),
    "C13.1": ("partly", "Pass 110", "ScenarioA1OrdnanceCalculator.cs: OpportunityFire", "ScenarioA1Pass35RulesTests",
              B + "an Opportunity Firer's LATW takes no +2 in the AFPh. Left: the BAZ, PIAT, MOL-Projector, ATMM, and PFk."),
    "C13.8": ("partly", "Pass 110", "ScenarioA1OrdnanceMapRules.cs: IsBackblastLocation", "ScenarioA1Pass35RulesTests",
              B + "a PF or PSK from ground-level rubble takes Case C3, and a pinned firer there is refused. Left: the Factory and rooftop exception, pillbox, cave, sewer, the bar on a target two or more levels higher when adjacent or directly above, Desperation fire."),
    "D1.321": ("partly", "Pass 105", "FireDefinition.MaType; TurretBarredWhileCe (OverrunWeapons, the vehicle's CC FP)", "ScenarioA1Pass11Tests",
               B + "a CE RST AFV adds neither its MA base nor its CMG to an OVR, and no CMG to CC. Left: whether its CMG still counts as a manned MG against the CC DRM (A11.51)."),
    "D2.16": ("built", "", "GamePlanner (ButtonedUpAfv, by ScenarioA1FireEligibility.CrewExposed); ScenarioA1RecallCalculator.cs: ExitHalfMp", "ATankButtonedUpByDefaultIsAlreadyBuAndMayExposeItsCrew",
              B + "a CT AFV that is BU by default pays the full MP a road hexside: one read of BU through the Crew Exposed verdict serves the road rate, the Recall exit, the NVR, the night move bar, the button-up answer, and the Starshell firer."),
    "D2.38": ("partly", "Pass 100", "ScenarioA1ArmorCalculator.cs (the target's Bypass fact)", "ScenarioA1Pass35RulesTests",
              B + "a Vehicle Target Type shot gives a Bypassing vehicle no beneficial Case Q of the obstacle (ruling R35.8). Left: a building Bypassed inside a woods hex; the LOS to the CAFP."),
    "D5.33": ("partly", "Pass 65", "ButtonUpBar (Bounding First Fire)", "ScenarioA1Pass35RulesTests",
              B + "no CE counter is placed or removed in a MPh after the vehicle's own Bounding First Fire, as after its Prep Fire. Left: Passengers becoming CE or BU with the crew, and fire by the PRC."),
    "D5.34": ("partly", "Pass 65", "the StunRecovery fact of the OVR and of the CC vehicle", "ScenarioA1Pass11Tests",
              B + "under a Stun +1 counter an AFV adds one to its OVR DR and to its CC DR. Left: Stun by a Sniper, by a MG TK DR equal to the TK#, by Falling Rubble; the crew counter's +1 after a bail-out."),
    "D5.341": ("partly", "Pass 105", "RecallAbandoned (the bog); EsbBar (the Recall); StopBar; ScenarioA1RecallCalculator.cs: StopsToUnload", "ScenarioA1Pass35RulesTests",
               B + "a bogged Recalled AFV is Abandoned at the Player Turn's end, ESB is barred to a Recalled AFV, and one carrying Passengers may Stop to unload them. Left: Recall of a 1MT AFV on a Stun, Sniper 1, Armor Leader loss, \"as soon as possible\" for the unloading, the crew's bar on re-crewing."),
    "D7.21": ("partly", "Pass 60", "ScenarioA1FireMapRules.cs: FirerLocation (the CC counter)", "ScenarioA1Pass35RulesTests",
              B + "the CC counter of CC Reaction Fire bars the unit's Non-CC Reaction Fire at the vehicle in its Location. Left: the Unarmed bar; Cavalry; the CC counter on SW and Guns."),
    "D8.4": ("built", "", "ScenarioA1OrdnanceMapRules.cs: InBogHex", "ScenarioA1Pass35RulesTests",
             B + "a vehicle that began its MPh bogged and has not left its Bog hex is fired on with no Case J for its Bog Removal MP. A vehicle that entered a hex or moved in Motion stays a moving target, as before."),
    "D8.5": ("partly", "Pass 105", "ScenarioA1VehicleProjection.cs: BoggedMaySpend", "ScenarioA1Pass35RulesTests",
             B + "a bogged vehicle loads and unloads, by one answer for the planner and the projector; the page offers the unload. Left: its other non-movement expenditures; the bow MG limits after Immobility."),
    "D9.4": ("partly", "Pass 100", "ScenarioA1VehicleSightRules.cs: BypassHinders", "ScenarioA1Pass35RulesTests",
             B + "a Bypassing AFV or wreck hinders only a LOS that touches the hexside it Bypasses (ruling R35.8). Built before: +1 through a hex with an AFV or non-burning wreck at the same level, not while Case J applies (R6.2). Left: the entrenched and Dug-In cases, the concealed AFV's reveal."),
    "E1.52": ("built", "", "ScenarioA1VehicleTerrainCosts.cs: EntryCost, BypassHalfMp; MoveBar; ScenarioA1RecallCalculator.cs: ExitHalfMp", "ScenarioA1Pass35RulesTests",
              B + "the night's +1 MP is charged in VBM and on the Recall exit as on an entry, after the Reverse multiplier (ruling R35.7), and Passengers unload from a BU AFV halted by an NVR of 0."),
    "E3.64": ("built", "", "ScenarioA1TerrainCosts.cs: InfantryWeatherHalfMf, BypassStep; ScenarioA1VehicleTerrainCosts.cs: BypassHalfMp", "ScenarioA1Pass35RulesTests",
              B + "Infantry on an unpaved road in Mud pay the half MF whatever else the hex holds, and Infantry Bypass and VBM add the weather's cost for each hexside. The Bypass step itself has no Rules test; Studio games p35-mud and p35-dry."),
    "E3.65": ("partly", "Pass 95", "ScenarioA1TerrainCosts.cs: InfantryWeatherHalfMf", "ScenarioA1Pass35RulesTests",
              B + "an unpaved road counts as Open Ground in Mud for Infantry as for vehicles. Left: gullies, dry streams, plowed fields, shellholes, trenches, unplowed roads in Deep Snow; a Recall exit by an unpaved road in Mud still pays the road rate."),
    "E3.9": ("built", "", "ScenarioA1VehicleTerrainCosts.cs: EntryCost, BypassHalfMp; ScenarioA1TerrainCosts.cs: BypassStep", "ScenarioA1Pass35RulesTests",
             B + "the weather's costs are added per hexside crossed or Bypassed, after the step's total and after the Reverse multiplier (ruling R35.7, a reading of E3.9 with D2.21, E1.52, and C10.1)."),
    "A.8": ("built", "", "ScenarioA1MovementCalculator.cs: IsAdjacent; GamePlanner (InfantryStep)", "ScenarioA1Pass35RulesTests; BacklogPass35Tests",
            B + "taken from pass 40 at the user's word. ADJACENT is a LOS between the two Locations and an Infantry step the movement rules allow, in either direction: a hex up a hill, across a wall or hedge, and the next level of a stairwell hex are ADJACENT; the upper level of the next building hex is not ADJACENT to the ground beside it (ruling R35.9; \"either direction\" is a reading)."),
}

ADD = {
    "A10.5": B + "a unit bound to surrender owes no rout and holds up no rout order, and either side may end the RtPh (task 35.17; ruling R35.2); a broken unit not under DM may not rout by choice (ruling R35.4).",
    "A10.53": B + "the Fanatic fault is repaired: the Interdiction NMC is against the broken Morale Level raised by one, never above 10; Interdiction needs the FFMO its enemy could apply (ruling R35.1).",
    "A10.532": B + "a SMC with no other unbroken SMC of his side in his Location Interdicts with his own range alone, which for a leader is none (task 35.4; ruling R35.1). The row stays this pass's for vehicles and Guns as Interdictors.",
    "A10.8": B + "the Interdiction NMC reads the Fanatic's raised broken Morale Level.",
    "A12.14": B + "the enemy that forces a concealed mover's loss of \"?\" is Good Order as A.7 has it, not hidden, and not a Passenger (ruling R35.5).",
    "A20.21": B + "a Commissar never surrenders in the RtPh: it routs through the Interdiction or is eliminated (ruling R35.2). A repulsed unit that ends ADJACENT to its enemy surrenders under this rule (ruling R35.3).",
    "B23.25": B + "the ADJACENT fault is repaired with A.8: two levels a stairwell joins are ADJACENT, and the same level of the next hex of the building.",
    "D2.13": B + "Non-Stopped is also read from the move in hand: a vehicle started in its MPh and not stopped since, never one bogged or immobilized (ruling R35.9).",
    "D2.5": B + "the ESB DRM Table by the manufacturer's nationality (EsbNationalDrm: U.S. and Czech 0, Russian and Chinese +1, British and German +2, others +3); ESB is barred to a Recalled AFV. Left: a vehicle in another nation's service.",
    "D7.211": B + "the rule names Wire, which is not built; a game with a Wire counter is refused (ruling R35.10; the backlog's section 55.1).",
    "D8.21": B + "the rule names Wire, which is not built; a game with a Wire counter is refused (ruling R35.10; the backlog's section 55.1). A hex beside a marsh is a Bog hex (B16.43).",
    "D2.21": B + "the weather's and the night's MP are added after the Reverse multiplier, not multiplied by it (ruling R35.7).",
    "A25.22": B + "the Commissar's exemption from RtPh surrender is built (ruling R35.2).",
}

STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]


def cells(line):
    return [cell.strip() for cell in line.split("|")[1:-1]]


def rows():
    chapter = None
    for index, line in enumerate(lines):
        if line.startswith("### Chapter "):
            chapter = line[len("### Chapter "):][0]
        if chapter and line.startswith("| ") and not line.startswith("| §") and not line.startswith("|---"):
            c = cells(line)
            if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
                yield index, chapter, c


def counts():
    by_chapter = collections.OrderedDict((letter, collections.Counter()) for letter in "ABCDE")
    by_pass = collections.Counter()
    for _, chapter, c in rows():
        by_chapter[chapter][c[3]] += 1
        if c[6]:
            by_pass[c[6]] += 1
    return by_chapter, by_pass


before_chapter, before_pass = counts()
if "--check" in sys.argv:
    for letter, counter in before_chapter.items():
        print(letter, sum(counter.values()), [counter[s] for s in STATUSES])
    print("total", sum(sum(c.values()) for c in before_chapter.values()), [sum(c[s] for c in before_chapter.values()) for s in STATUSES])
    for key, value in sorted(before_pass.items(), key=lambda item: item[0]):
        print(value, key)
    sys.exit(0)

index_of = {c[0]: (index, c) for index, _, c in rows()}
changed = 0
for rule, (status, target, where, test, note) in SET.items():
    index, c = index_of[rule]
    if rule not in ("A.8",):
        assert c[6] == "Pass 35", (rule, c[6])
    c[3] = status
    c[6] = target
    if where and where not in c[4]:
        c[4] = (c[4] + "; " if c[4] else "") + where
    if test and test not in c[5]:
        c[5] = (c[5] + "; " if c[5] else "") + test
    c[7] = note
    lines[index] = "| " + " | ".join(c) + " |"
    changed += 1
for rule, addition in ADD.items():
    index, c = index_of[rule]
    assert "Pass 35 (2026-10-09)" not in c[7], rule
    c[7] = (c[7].rstrip() + " " if c[7] else "") + addition
    lines[index] = "| " + " | ".join(c) + " |"
    changed += 1

after_chapter, after_pass = counts()
assert after_pass.get("Pass 35", 0) == 0
print("rows changed:", changed)
for letter in "ABCDE":
    print(letter, [before_chapter[letter][s] for s in STATUSES], "->", [after_chapter[letter][s] for s in STATUSES])
moved = {key: (before_pass.get(key, 0), after_pass.get(key, 0)) for key in set(before_pass) | set(after_pass) if before_pass.get(key, 0) != after_pass.get(key, 0)}
for key, value in sorted(moved.items()):
    print(key, value)
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
