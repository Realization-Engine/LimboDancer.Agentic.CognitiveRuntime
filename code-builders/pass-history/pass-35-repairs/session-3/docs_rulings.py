"""Pass 35 documents: the rulings R35.1 to R35.10 in the Passes Plan's section 5, and a pointer on the three rows they amend."""
P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog Passes Plan.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)

ROWS = [
    ("R35.1", "What is Open Ground for a rout and for Interdiction? (amends R13.3)",
     "A10.53, A10.531, A10.532 (p. 67), B1.14, B1.16, B1.17 (p. 113), read in the PDF. Open Ground is a hex in which the particular enemy unit could apply the FFMO DRM in a "
     "hypothetical Defensive First Fire shot: a clear LOS within its range, no Hindrance of any kind along it, and no TEM the routing unit could claim against that enemy. One "
     "Rules verdict (`CouldApplyFfmo`) answers for the must-rout test and for Interdiction, over the cover fire reads: the map Hindrance by terrain and season, the Hindrance of "
     "vehicles, wrecks, and SMOKE along the LOS, the wall or hedge TEM of the hexside crossed, Height Advantage, and a wreck's or AFV's cover in the Location. This replaces "
     "R13.3's test by the terrain's name. A SMC with no other unbroken SMC of his side in his Location Interdicts with his own range alone, which for a leader is none (A10.532: a "
     "unit whose FP is halved may not Interdict); the must-rout test still counts his SW's range, since A10.5 names the SW itself. A Fanatic unit's Interdiction NMC is against "
     "its broken Morale Level raised by one (A10.8, p. 69), never above 10 (R35.5)."),
    ("R35.2", "Does a unit that will surrender still owe a rout, and who may end the RtPh? (amends R13.3)",
     "A10.5 (p. 66), A19.12 (p. 86), A20.21 (pp. 86 to 87), A25.22 (p. 94), read in the PDF; the user's answers of 2026-10-09 (the pass 35 design, section 7.5, questions 1 to 3). "
     "The surrender stays where R13.3 put it, as the RtPh ends: the page gives no other moment but for a Disrupted unit. A unit bound to surrender owes no rout, so the other "
     "side's units do not wait for it in the rout order, the Rout panel says it surrenders and offers it no route, and either side may end the RtPh while the other's unit is "
     "bound to surrender or has no legal step; the end still shows each surrender and elimination as a consequence to confirm. A Disrupted unit routs only when it must and never "
     "by Low Crawl (A19.12). A Commissar never surrenders this way: it routs through the Interdiction, and one that ends the RtPh ADJACENT to its enemy is eliminated, as a "
     "Fanatic unit and a unit faced with No Quarter are."),
    ("R35.3", "What happens to a rout that enters a Location of concealed enemy units? (amends R13.3)",
     "A10.533 (p. 68), A12.15 (p. 78), A.9, read in the PDF; ruling R10.11; the user's answer of 2026-10-09 (section 7.5, question 6). Concealed units are ignored when the route "
     "is planned, and the plan a side confirms says nothing of them. On entry: with a real unit among those the routing side does not know, hidden units first go beneath a \"?\", "
     "one real unit loses its \"?\" (the only one with no dice, among several by Random Selection as an ordinary entry draws, ties all), and the routing unit is repulsed: it stays "
     "in the Location it came from, pays the step's MF, and its rout ends there. With Dummies alone, they are removed and the rout goes in. The record is a rout step that names "
     "the Location attempted. A10.533 says a repulsed unit left ADJACENT to a Known enemy unit is eliminated; as built, the phase's end treats it as any broken unit that ends "
     "ADJACENT to its enemy, so it surrenders when a captor can take it (A20.21) and is eliminated otherwise. A20.21 covers any broken unit ADJACENT to Good Order armed enemy "
     "Infantry that cannot rout away, and the repulsed unit is one. A concealed unit giving up its \"?\" by choice to Interdict or to change the route is not built (backlog)."),
    ("R35.4", "May a broken unit that is not under DM rout by choice?",
     "A10.5 (p. 66), read in the PDF: \"A broken unit may rout if currently under DM\", and otherwise only when it must. The refusal stands, and the backlog row of section 52 "
     "(raised in the user's game `cd-guards-5`) is closed. The user's answer of 2026-10-09 (section 7.5, question 4)."),
    ("R35.5", "What is Good Order, who forces a concealed mover's loss of \"?\", and how high may a Morale Level go? (amends R10.10; withdraws the 11 of the berserk Fanatic)",
     "A.7 (p. 43), A.18 (p. 44), A12.14 (p. 77), read in the PDF; the user's answers of 2026-10-09 (section 6, F5; section 8.4, questions 1 and 2). Good Order is a unit that is "
     "not broken, berserk, captured, stunned, shocked, or held in Melee; a pinned, CX, TI, or unarmed unit is still in Good Order. One definition in Rules (`GoodOrderOf`) "
     "replaces the planner's, which left a TI unit out and let a berserk unit in. The SW and Deployment actions keep their bar on a TI unit under its own name "
     "(`FreeToActAsPlanned`): A.7 says what Good Order is, not what a TI unit may do, and the TI rules for those actions were not read (backlog). The enemy that forces a "
     "concealed mover's loss of \"?\" is a Good Order enemy ground unit within 16 hexes that is not hidden and not a Passenger, as fire reads it (R31d.2); R10.10's \"Good Order\" "
     "was tested as \"not broken\". No Morale Level is raised beyond 10, \"even if the unit is Fanatic, heroic, with a Commissar, and/or part of a Human Wave\": the 11 a berserk "
     "Fanatic unit was given (the code named it ruling R30.3; the Deviations Pass 2 design has the clause) is withdrawn, and the ceiling is applied wherever a Morale Level is "
     "raised. The Close Combat calculator's own Good Order, with no Melee clause, stays: it chooses the leader who directs a Close Combat, where every unit is in the Melee."),
    ("R35.6", "Is ordnance HE halved into a marsh? (amends R10.1)",
     "C3.53 (p. 170), B16.31 (p. 130), read in the PDF. Yes: a hit's HE FP is halved for a marsh target, beside any halving of the Area Target Type, and after a Critical Hit's "
     "doubling. R10.1's leave-out (full FP) ends. Fire from a marsh hex stays refused (B16.32; R10.1)."),
    ("R35.7", "Are the weather's and the night's movement costs multiplied when a vehicle reverses?",
     "D2.21 (p. 196), E3.9 (p. 231), E1.52 (p. 224), C10.1 (p. 180), read in the PDF; the user's answer of 2026-10-09. No. D2.21 puts the Reverse multiplier on \"the total cost "
     "of entry of that hex, including any penalties/multipliers\"; E3.9 adds the weather's MF and MP \"per hexside crossed (or Bypassed), after calculating total cost\"; E1.52 "
     "adds the night's MP \"as if towing a Gun\", and C10.1 adds the tow's \"after all other modifications\". So both are added once, after the multiplier, in an entry, in VBM, "
     "in Infantry Bypass, and on the Recall exit. The two sentences pull against each other only if D2.21's \"total cost\" is read to reach forward into E3.9; the later, narrower "
     "rule is taken. A reading."),
    ("R35.8", "When does a LOS \"touch\" the hexside a vehicle Bypasses?",
     "D9.4 (p. 210), D2.37, D2.38 (p. 198), read in the PDF; the user's answer of 2026-10-09. A Bypassing AFV or wreck is a Hindrance only to a LOS that touches the hexside it "
     "Bypasses. The LOS is read as touching that hexside when it passes through both hexes the hexside lies between, or starts or ends in the hex across it. The LOS to the "
     "vehicle's CAFP is not built (backlog). A vehicle in Bypass takes no beneficial TEM of the woods or building it Bypasses."),
    ("R35.9", "When are two Locations ADJACENT, and is a vehicle that has left its Bog Non-Stopped?",
     "A.8 (p. 43), B23.25 (p. 136), A10.5's example (p. 67), C6.3 (p. 174), D2.13 (p. 196), D8.3 and D8.4 (p. 209), read in the PDF; the user's word of 2026-10-09 for ADJACENT, "
     "which was pass 40's row. Locations are ADJACENT when there is a LOS between them (SMOKE Hindrance and NVR left out of it) and a hypothetical Infantry unit could move from "
     "the one into the other in the APh, enemy presence ignored. As built: a LOS and an Infantry step the movement rules allow, in either direction, so a hex one level up a hill, "
     "a hex across a wall or hedge, and the next level of a stairwell hex are ADJACENT; the upper level of the next building hex is not ADJACENT to the ground beside it; terrain "
     "the movement rules refuse stays not ADJACENT. \"Either direction\" is a reading: the rule speaks of moving from that Location into the other, and no step the game admits is "
     "one-way. A vehicle is Non-Stopped when it bears a Motion counter or has started in its MPh and not stopped since, never while bogged or immobilized; one freed by its Bog "
     "Removal is Non-Stopped from then on, since Bog Removal is its Start MP. No rule read says the last outright; it is a reading, and the referee's review of pass 35 is asked "
     "to check it."),
    ("R35.10", "What does the game do with a counter whose rules are not built?",
     "B23.9, B24, B25.15, B26 to B30; the coverage document's two notes; the user's word of 2026-10-09. A game that would place, or holds, a fortification (wire, foxhole, trench, "
     "minefield, roadblock, pillbox, Fortified Building Location), a rubble counter, or a Flame is refused whole, at setup and at every action, with the kinds named; until now it "
     "played as if the counter were absent. A stopgap, kept blunt at the user's word: passes 115 and 120 build the counters and take the refusal away kind by kind. A recorded "
     "game with such a counter still replays; only the planner refuses. The backlog's section 55.1 lists what each counter's rules are and where the inventory is silent on them."),
]

last = max(i for i, line in enumerate(lines) if line.startswith("| R31d.6 |"))
assert not any(line.startswith("| R35.") for line in lines)
new = [f"| {number} | {question} | {answer} |" for number, question, answer in ROWS]
lines[last + 1:last + 1] = new

AMEND = {
    "| R10.1 |": " **Amended by R35.6 (pass 35): ordnance HE is halved into a marsh.**",
    "| R10.10 |": " **Amended by R35.5 (pass 35): the enemy is Good Order as A.7 has it, not hidden, and not a Passenger.**",
    "| R13.3 |": " **Amended by R35.1 to R35.4 (pass 35): Open Ground is read by the FFMO a given enemy could apply; a unit bound to surrender owes no rout; the A10.533 repulse is built; no voluntary rout without DM.**",
}
for prefix, note in AMEND.items():
    index = [i for i, line in enumerate(lines) if line.startswith(prefix)]
    assert len(index) == 1, prefix
    line = lines[index[0]]
    assert line.endswith(" |") and "Amended by R35" not in line
    lines[index[0]] = line[:-2] + note + " |"

open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
print("rulings written:", len(new))
