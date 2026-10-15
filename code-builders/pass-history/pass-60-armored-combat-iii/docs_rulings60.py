"""Pass 60 documents: rulings R60.1 to R60.27 as rows of the Passes Plan's section 5, after R55.33; R60.10, R60.11, R60.18 marked as corrected.
Every anchor is counted before the file is written. LF, UTF-8, no BOM."""
import io

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog Passes Plan.md"
text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("\ufeff")
W = 'Pass 60; decided under the user\'s word of 2026-10-15 ("this is my word to start pass 60, run without my intervention up to the gate"), open to a veto.'

R = [
    ("R60.1", "Is a pass larger than the plan's entry split, and in what order is it built?",
     "No: it stays one pass. The plan names 37 rows and 4:40 of building; the inventory names 49 rows and the prompt carries in task 50.5 (MG fire against armor), and the frame's estimate is 6:40 of building. The rows are placed by the design's section 3 and built in the order of its section 2: HE Equivalency first, since D.8A's attack takes its FP; the Specific Collateral Attack; the mortar; FT, MOL, and DC; the Guns; the carried-in MG last, so that whatever a session's end leaves out is the task carried in."),
    ("R60.2", "Where do the pass's tables go?",
     "Into the Rules project as small tables with their page, not into a transcription, so that no case matrix or package is changed and the four packages are not pinned again: C8.31's HE Equivalency (p. 178: AP below 37mm 1; AP 37mm and up 2; every APCR, APDS, and ATR 1; BAZ and PIAT 8; PSK, PFk, and H#[9] 12; PF 16; other HEAT one IFT column left of the Gun's HE FP), C7.34's Basic TK# (p. 190: DC 16, FT 8, MOL 6), the DC and FT/MOL columns of C7.7 (p. 190), and C11.6 (p. 182). `BOTH_SKIP` keeps `GameEvent.RulePackage` in case a later task must pin."),
    ("R60.3", "How does a rule that changes an attack that could be made before the pass reach a recorded game?",
     "Only through a fact on the record: A9.74, C3.71's reversal of Height Advantage, and D.8A after a Vehicle Target Type hit are applied only where the record carries the pass's fact for them, taken from the record at replay and never recomputed from the state. The planner sets the fact on every new attack, so an attack recorded before the pass resolves as it did. The replay digest is where a miss shows."),
    ("R60.4", "How do AP, APCR, APDS, HEAT, and an ATR fire at an unarmored target?",
     "On the Infantry Target Type at their HE Equivalency FP (C8.31, p. 178), by the HE path that exists: TEM as Case Q on the To Hit DR, one IFT DR for every unit hit, a Critical Hit doubling it and reversing positive TEM. HEAT is fired at Infantry only where the target Location gives a wall, building, rubble, or pillbox TEM; an ordnance HEAT round needs an H# and a printed HE FP to step left from, and a Gun with no HE is refused it. None leaves Residual FP. An ATR fires at Personnel on the Infantry Target Type only if it is 20L; the catalog's two ATRs are not, so they keep firing at Personnel as Small Arms."),
    ("R60.5", "How do a PF and a PSK fire at Personnel?",
     "Under C8.31's bar, at 16 and 12 FP (C13.31, C13.9). The PF Check takes +1 at a target that is not an AFV and +1 in August and September 1943; a PF's hit affects one unit by Random Selection. The firer's choice of a Known unit manning a Gun or SW after the hit is left out (backlog)."),
    ("R60.6", "How do AP, HEAT, and an ATR fire at a Gun?",
     "AP, APCR, APDS, HEAT, and any ATR fire at a Gun on the Infantry Target Type at their equivalency FP, and the Gun's fate is C11.4's with C11.6's note 5: a K eliminates the Gun when AP is fired (C11.52, C13.23, p. 183). A non-20L ATR attacks only the Gun and its manning crew."),
    ("R60.7", "When is the Specific Collateral Attack made?",
     "After a Vehicle Target Type hit on an AFV that does not destroy it (a TK DR missed, an Immobilization, an Unconfirmed Kill), its CE crew takes an IFT attack at the round's IFT FP (HE, or its HE Equivalency) on the Original TK DR, with the CE DRM alone (D.8A, p. 192). None after a Dud, a Shock this hit causes (C7.42), a crew already stunned or shocked, a buttoned-up crew, or a vehicle destroyed. One attack for a Multiple Hit, and no Residual FP. Passengers and Riders: pass 65."),
    ("R60.8", "May an Infantry Target Type shot name a Location that holds a vehicle?",
     "Yes, one that holds an unarmored vehicle (C3.32, C3.41, p. 169). Each vehicle and each unit is hit on its own Final TH# (the vehicle by its size; a concealed unit with Case K); every unit and unarmored vehicle hit shares one Effects DR on the HE FP column, the vehicle on the Vehicle line (A7.308, A7.309). An AFV is immune, and its CE crew takes a General Collateral Attack on the same DR when the AFV's own TH# is reached. A Location holding only AFVs stays the Vehicle Target Type's (C3.41, D.6)."),
    ("R60.9", "How is a Location of Known and concealed units hit?",
     "By one To Hit DR judged per unit (C3.4, p. 170); a concealed unit hit loses its \"?\" as A12.14 has it, and one missed keeps it. Units out of LOS or too close: backlog."),
    ("R60.10", "How does a mortar's hit attack a vehicle?",
     "On the IFT at half the HE FP, never To Kill (C1.55, p. 165; C3.332, C7.345, C9.1): -1 if every AF is 4 or less, -1 if OT, +1 if every AF is 8 or more, with the TEM that gives no HD; a KIA destroys it, a DR at most half the K/# DR burns it, and the K/# DR and one above Shock a turret hit or Immobilize a hull hit. An unarmored vehicle is on the Vehicle line. The CE crew takes a General Collateral Attack on the same DR. A Critical Hit is at double the full FP with no TEM, the AF and OT modifiers kept (C3.72). **Corrected by R60.23 (pass 60): the hit's Location is read from the Original IFT DR's dice, not from the To Hit DR's, as the frame first said.**"),
    ("R60.11", "May a mortar fire at a hex whose units are on several levels of a building?",
     "Yes, outside an Interior Building Hex (B23.32, p. 136): each Location is hit on its own To Hit DR modifiers, and one Effects DR attacks every Location hit, each with its building TEM and +1 for each level above it; a Critical Hit goes to one Location by Random Selection. Friendly units in the target hex stay refused (backlog). **Corrected by R60.24 (pass 60): the frame said a Location out of the firer's LOS is not attacked; C3.33 attacks it when the hardest target in the firer's LOS is hit.**"),
    ("R60.12", "What do Air Bursts do in full?",
     "-1 against an unarmored, CE, or OT target in woods, combined with an entrenchment or Emplacement, and kept on a Critical Hit (B13.3, p. 128); an OT AFV so hit is attacked as unarmored (D5.311, p. 203). None against a unit in Bypass."),
    ("R60.13", "What does a mortar's Area Acquisition cover?",
     "Every Location of the hex in its LOS (C6.521, p. 174)."),
    ("R60.14", "What do a Critical Hit and Indirect Fire do to Height Advantage and a gunshield?",
     "A Critical Hit on a Direct Fire target that claimed Height Advantage reverses it to -1 (C3.71, p. 170). The gunshield gives +1, not +2, against Indirect Fire (C11.4, C11.5, p. 182)."),
    ("R60.15", "How do a FT and a MOL attack an AFV?",
     "A fire request names an AFV in the target Location as the FT's or MOL's target (predesignation; A22.34, A22.612, p. 89). A FT's Basic TK# is 8, halved at Long Range before +1 CE and +2 OT; a MOL's is 6 with +2 OT or +1 CE (closed-topped), -2 against a moving vehicle (C.8), and +1 or +2 for an elevation advantage; both take +1 through the rear, no AF, and the FT/MOL column (below the TK# burns; equal eliminates with Crew Survival). A MOL's DR also attacks the CE crew at the Small Arms FP plus 4 and every other unarmored unit at the Small Arms FP; a failed MOL Check is an IFT attack on them all without the 4. A FT attacks only the AFV and, if it survives, its CE crew."),
    ("R60.16", "How does a DC attack an AFV?",
     "By the Position DR (C7.346, p. 177) with its modifiers whose facts exist: 5 or less uses the Aerial AF, 6 to 8 the AF, 9 to 11 attacks the CE crew only on a new DR, 12 or more attacks the other unarmored units of the Location. Its TK# is 16 less the AF, +1 through the rear, on the DC column. A Placed DC needs the PAATC that exists (A23.3, A23.5). A same-hex attack is through the rear; otherwise the hexside crossed. A MOL at an unarmored vehicle, a MOL's Flame, ATMM, a vehicle as the thrower, and a throw down a level: backlog."),
    ("R60.17", "Which KIA destroys the weapons its victims held?",
     "Every Final KIA of an IFT attack, a Canister KIA included (A9.74, p. 65; C8.42, p. 178): each SW and Gun held by a unit it eliminated rolls a dr on the same column; a KIA destroys it, a K malfunctions it, or eliminates it if it was malfunctioned or has an X#. Ordnance, DC, and OBA at the Gun itself follow C11.4 instead. Unpossessed weapons checked by Indirect Fire and by an OVR, the OVR's end-of-MPh check, and an OVR of an empty Location: backlog."),
    ("R60.18", "May an unattended Gun be a target, and what ends Emplacement?",
     "An unattended Gun may be the target of the Infantry and Area Target Types: a KIA destroys it and a K malfunctions it (C3.41, C11.4). A Gun manned by a non-crew loses Emplacement for the game. An attack by a MG, small arms, a FT, or a MOL destroys a Gun only through A9.74 (C11.51, C11.6). **Corrected by R60.26 (pass 60): only a Gun that starts the scenario manned by a non-crew unit loses its Emplacement for the game.**"),
    ("R60.19", "How does a MG make its To Kill attack on an AFV?",
     "A SW MG fired alone at a named AFV within Normal Range in the PFPh or DFPh makes a To Hit DR on the Vehicle Target Type with black numbers, then a To Kill DR with Basic TK# 4 (12.7mm and 15mm by their rows), Case A, the 25mm row of Case D, less the AF (A9.61, p. 64); below the number eliminates (at most half burns), equal Stuns, and there is no Shock, Immobilization, Critical Hit, or Multiple Hit; the ROF is kept as ordnance; R60.7's attack follows. A vehicle's MA that is a MG may make it (D3.54, p. 201); its other MGs may not. Its Residual FP and the AFPh: backlog."),
    ("R60.20", "When does CC Reaction Fire cancel a Fire Lane, and what does its CC counter bar?",
     "A9.223's narrow reading (p. 63): a Fire Lane is cancelled when its MG fires CC Reaction Fire. D7.21's CC counter (p. 207) bars its SW and Guns from Non-CC Reaction Fire."),
    ("R60.21", "Where does A5.132 go?",
     "To pass 110, beside OVR Prevention: a hit on another vehicle of a vehicle-overstacked hex needs a Random Selection by size among vehicles and a target choice the To Hit does not have (A5.132, p. 52)."),
    ("R60.22", "Is the upper superstructure of a non-turreted AFV built?",
     "With a fact on the record if 2f has time, else moved to pass 105 (C3.9, p. 171; the end of ruling R7.4). 2f had no time: it is moved to pass 105."),
    ("R60.23", "From which DR does a mortar's hit on an AFV take its hit Location? (corrects R60.10)",
     "From the Original IFT DR's dice, not the To Hit DR's: the C9.5 example and the C7.7 chart's note E (\"Indirect Fire uses the Original IFT DR for hit location\", p. 190). The frame's reading was built first and sent back."),
    ("R60.24", "Does a mortar's hit attack the target hex's units out of its LOS? (corrects R60.11)",
     "Yes, when the shot hit the non-hidden enemy target in its LOS that was the hardest to hit (the highest net To Hit DRM): they are attacked on the same Effects DR with their own TEM and levels above (C3.33, p. 169; the C3.331 example, p. 170). Found by the first referee."),
    ("R60.25", "Does a Collateral Attack follow a Stun the hit causes?",
     "No: a Stun buttons the crew up \"immediately\" (D5.34, p. 203), so no Vulnerable crew is left, as after a Shock (R60.7). The record says so. Found by the table player."),
    ("R60.26", "Which non-crew manning ends a Gun's Emplacement? (corrects R60.18)",
     "Only a Gun that \"starts a scenario hooked up or manned by a non-crew unit\" loses its Emplacement for the scenario, with a move and a forfeit of Wall Advantage (C11.3, p. 182). A non-crew unit that mans a Gun later does not end it; C11.2 only withholds the +2 while a squad mans it. Found by the second referee."),
    ("R60.27", "May a MG make its To Kill attack at a concealed AFV?",
     "No: A9.61 asks for no halved FP, and on the IFT a concealed target would halve it (A7.23). Whether Case K replaces the halving is left to the backlog. Found by the second referee."),
]
assert len(R) == 27 and [r[0] for r in R] == [f"R60.{n}" for n in range(1, 28)]
rows = []
for ruling, question, answer in R:
    for cell in (question, answer):
        assert "|" not in cell and "\u2014" not in cell and "\n" not in cell, ruling
    rows.append(f"| {ruling} | {question} | {W} {answer} |")

anchor = next(line for line in text.split("\n") if line.startswith("| R55.33 |"))
assert text.count(anchor + "\n") == 1
assert "| R60.1 |" not in text
text = text.replace(anchor + "\n", anchor + "\n" + "\n".join(rows) + "\n")
io.open(P, "w", encoding="utf-8", newline="").write(text)
print("rulings written:", len(rows))
