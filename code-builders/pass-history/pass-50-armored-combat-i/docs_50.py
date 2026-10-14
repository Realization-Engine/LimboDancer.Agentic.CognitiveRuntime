"""Pass 50 documents: the design's last sections, the review, the rulings in the Passes Plan, the backlog's section 58 and the older rows the pass
answers, the plan's status, the time log, and the lessons. Every anchor is checked before any file is written.

Usage: python docs_50.py <HH:MM now>
"""
import io
import sys

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
T = "E:/Archive/GitHub/dlandi/pass32-tools/pass50/"
NOW = sys.argv[1]


def read(path):
    return io.open(path, encoding="utf-8", newline="").read().replace("\r\n", "\n")


files = {}


def text(name):
    if name not in files:
        files[name] = read(D + name)
    return files[name]


def replace(name, old, new, count=1):
    assert text(name).count(old) == count, (name, text(name).count(old), old[:80])
    files[name] = text(name).replace(old, new)


def append(name, more):
    files[name] = text(name).rstrip("\n") + "\n\n" + more.strip("\n") + "\n"


ten = read(T + "design_10.md")

# --- The design ------------------------------------------------------------------------------------------------------
DESIGN = "Passes/ASL Unit Backlog Pass 50 Design.md"
replace(DESIGN, "**Status:** The frame (increment 1), written 2026-10-12. Read-only: nothing is built. The frame's questions are answered by their own proposals under the user's word of 2026-10-12 for this pass (\"work independently as much as possible up to merge gate and then pause\"); each answer is open to a veto. Increment 2a (50.1, 50.2) is next.",
        "**Status:** Built to the merge gate on 2026-10-12 under the user's word of that day for this pass (\"work independently as much as possible up to merge gate and then pause\"): the frame, tasks 50.1, 50.2, 50.3, and 50.6, the five reviews and their fixes, the Studio check, and the documents. Tasks 50.4 (Gun Duels) and 50.5 (MG fire against armor) are not built. Sections 10 and 11 say how it ended; section 10.1 lists what departs from the plan's entry and the frame. Every answer and ruling is open to a veto. **The gate is not run:** it is the user's to start.")
replace(DESIGN, "Sections for the increments, the reviews, and \"As built\" are added as the pass goes.",
        "Then: 7, a vehicle fires weapon by weapon (50.1, 50.2); 8, the MA while moving (50.3); 9, the Target Facing from inside the target's hex (50.6); 10, the five reviews and the Studio check; 11, as built.")
append(DESIGN, ten)

# --- The review ------------------------------------------------------------------------------------------------------
def section(start, end):
    a = ten.index(start)
    b = ten.index(end) if end else len(ten)
    return ten[a + len(start):b].strip("\n")


review = """# Scenario A1 Backlog Pass 50: Armored combat I

The review of pass 50, written on 2026-10-12 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 50 Design](<ASL Unit Backlog Pass 50 Design.md>); its section 11 says what was built.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-50`, cut from main at b4ce31ef: the frame, four tasks of six in three commits, then the five reviews, their fixes, the Studio check, and these documents. Everything was done under the user's word of 2026-10-12 for this pass, "work independently as much as possible up to merge gate and then pause", and is open to a veto. **The gate is not run, and nothing is merged.**

## What departs from the plan's entry and the frame

""" + section("### 10.1 What departs from the plan's entry and the frame, first", "### 10.2 Fixed").split("\n\n", 1)[1] + """

## What a player now meets

- A tank is a firer in the Fire panel. Its BMG and CMG are listed with their FP; they fire together or apart, each once a Player Turn, and all of a vehicle's weapons fire in one fire phase (D3.5). For the DEFENDER, First Fire in the MPh and Final Fire in the DFPh are one (A.15). The vehicle carries one fire counter for all its weapons.
- A BMG fires at a target inside the VCA, to eight hexes at full FP and sixteen at half; a CMG through the TCA, to twelve and twenty-four, with no leader (D1.81, D1.82, A9.4).
- A CMG fired outside the TCA turns the turret and adds Case A to its IFT DR: 1 a hexspine for the PzKpfw IIIH, 2 and then 1 for the T-34, doubled in woods, a building, or rubble (D3.52, C5.11). The MA's next shot within that TCA pays the same (D3.51).
- A MG malfunctions on an Original 12; of two fired together, Random Selection finds the one (D3.7, A9.71).
- In the RPh a tank's crew that is not shocked or stunned tries to repair each malfunctioned weapon by a dr of its own: a 1 repairs it, a 6 disables it. A tank whose MA is disabled is Recalled (D3.7, D5.341).
- In its own MPh a tank's MA fires as Bounding First Fire, at the outset of its move or once the DEFENDER has passed: within its TCA, with Case C (+4 for both tanks), Case C1 (+5) for a LOS of two and a half to three MP, Case C2 (+6) for two or less, and the lower dr doubled while it has not stopped (C5.3 to C5.35). It takes no Acquisition DRM (C6.55). It spends another MP before it fires again; a spent ROF leaves a Bounding Fire counter (D3.3).
- "Propose: Delay" spends one MP in place while the tank is stopped or has not started (D2.17). "Turret with the next MP" turns the turret with a Delay, a stop, a VCA change, or an entry (D3.12).
- In the AFPh a vehicle that entered a new hex or is in Motion fires its MA with Case C, where it was refused; the MA that kept its ROF in the MPh fires once more (C5.3, D3.32).
- A Gun, or a tank, fires at an enemy vehicle in its own Location with Case E; the colored dr of the To Hit DR gives the Target Facing: 1 or 2 the rear, 3 or 4 the side, 5 or 6 the front (C5.5, D3.2).

## The rulings

R50.1 to R50.14, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All fourteen were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-12.

## The reviews' findings and what was done

One BLOCKER, fixed. The findings fixed:

""" + section("### 10.2 Fixed", "### 10.3 Not fixed").strip("\n") + """

The findings not fixed, each with a row in the backlog's section 58:

""" + section("### 10.3 Not fixed: a row each in the backlog's section 58", "### 10.4 The Studio check").strip("\n") + """

## The Studio check

""" + section("### 10.4 The Studio check", "## 11. As built").strip("\n") + """

## What was not done

- Tasks 50.4 and 50.5.
- The whole Rules and Play test projects, the solution build, the Node viewport test, the chart supplement, the planner sweep, and Docker: the gate's, which the user starts.
- A Play test for task 50.6 and a test of the Case L fix.
- Real keys in the Studio check, and the Replay page.
"""
files["Passes/Scenario A1 Backlog Pass 50 Review 2026-10-12.md"] = review

# --- The Passes Plan: the rulings ------------------------------------------------------------------------------------
PLAN5 = "ASL Unit Backlog Passes Plan.md"
W = "Pass 50; decided under the user's word of 2026-10-12 (\"work independently as much as possible up to merge gate and then pause\"), open to the user's veto. "
RULINGS = [
    ("R50.1", "How is a vehicle's weapon named and remembered?", "A vehicle's entry in the fire request's `weapons` names its BMG and CMG; the state keeps, until the Player Turn ends, which weapons fired and in which fire phase; the vehicle carries one fire counter for all of them. A record made before the pass names none and is checked as it was."),
    ("R50.2", "What is \"the same fire phase\" of D3.5?", "For the side that is not phasing, Defensive First Fire in the MPh and Final Fire in the DFPh are one (A.15's example). For the phasing side the PFPh, the MPh, and the AFPh are three, with D3.32's and D3.31's leave in the AFPh after a MA-only Bounding First Fire that left no counter."),
    ("R50.3", "What is D3.51's \"same CA Change penalty as the first weapon which fired\"?", "The Case A of the shot that turned the turret. The CMG's shot within the TCA as it stands pays the first turning shot's of the fire phase; the MA pays the CMG's on its first shot after the CMG's turn. A weapon that turns the turret again pays for its own turn alone. After two MA shots that each turned, the CMG pays the first's: the page does not settle it."),
    ("R50.4", "May a BMG fire outside the VCA?", "No: the hull's pivot for a shot is not built. D3.51's example stacks the NT penalty of the pivot on a turret weapon's own, with the turret carried round by the hull, which the state does not do."),
    ("R50.5", "May a vehicle's MG fire at another level of its own hex?", "No: refused as out of range, as before the pass."),
    ("R50.6", "What is the Recall of a tank whose MA is disabled?", "The Recall of D5.341 as the game has it: treated as a Stun until the Player Turn ends, then the counter's Recall side. A crew so Recalled repairs nothing more in that RPh."),
    ("R50.7", "Who repairs a tank's weapon?", "A crew that is not shocked or stunned, BU or CE, by one Rules member the planner and the projector both read. The halftrack's MA AAMG keeps the CE test and its two copies, so that a recorded repair checks as it did."),
    ("R50.8", "How are the MP of Cases C1 and C2 counted?", "As those of Cases J1 and J2 are: back from the vehicle's latest expenditure to its last one out of LOS of the target's Location, rounded up to whole MP; a LOS held since before the MPh is Case C."),
    ("R50.9", "What does Case C4's doubled lower dr change?", "The Final DR alone. The Original DR, which decides the ROF, a malfunction, and an Original 2, is unchanged; the lowest Final DR that C3.6 asks about is one higher."),
    ("R50.10", "When does a vehicle's AFPh shot take Case C?", "When the vehicle entered a new hex this Player Turn, is in Motion, or fired its MA in the MPh. One that only started and stopped, or only spent Delay MP, and did not fire, takes Case B (C5.2)."),
    ("R50.11", "When may the MA fire once more in the AFPh (D3.32)?", "When the vehicle carries no Bounding Fire counter and its MA was the only weapon that fired in the MPh."),
    ("R50.12", "What does the colored dr's facing apply to in a same-hex shot?", "The hull and the turret alike (D3.2 gives one Target Facing for the hit). The AP To Kill Table's Case D is read in its first column, \"0-1\"."),
    ("R50.13", "May a Bounding First Fire or Motion Fire shot claim an Acquisition?", "No: C6.55 gives it to a Stabilized Gun alone, and the catalog has none. Such a shot takes no Case N, and what it acquires starts from nothing. Found by the reviews as a BLOCKER."),
    ("R50.14", "Is the Case B part of Case C raised in woods, a building, or rubble?", "No: C5.3 says \"the +2 DRM of Case B\". The chart of p. 189 prints Case C as Case B plus the mount's figure and Case B as +3 there, so it can be read to make Case C one more; the text is followed."),
]
anchor = [line for line in text(PLAN5).split("\n") if line.startswith("| R45.6 |")]
assert len(anchor) == 1
replace(PLAN5, anchor[0] + "\n", anchor[0] + "\n" + "".join(f"| {n} | {q} | {W}{a} |\n" for n, q, a in RULINGS))

# --- The backlog -----------------------------------------------------------------------------------------------------
BACK = "ASL Unit Backlog.md"
replace(BACK, "| A5.132, p. 52 | Pass 50: the To Hit's target choice |", "| A5.132, p. 52 | Pass 55: the To Hit's target choice (pass 50 left it; 58.1) |")
replace(BACK, "| C5.64 | Pass 50 | A crew fires no Gun in FPF. |", "| C5.64 | Pass 110 (pass 50 left it; 58.1) | A crew fires no Gun in FPF. |")
replace(BACK, "| A7.21, A7.5 | Pass 50; the catalog |", "| A7.21, A7.5 | Pass 65 (A7.211); the catalog. A vehicle's MG at another level: answered by ruling R50.5, refused |")
replace(BACK, "One Rules member read by MoveBar and the projector (pass 50)", "One Rules member read by MoveBar and the projector (pass 55; pass 50 did not take it)")
replace(BACK, "the CE test of a vehicle MG's repair (pass 50)", "the CE test of a vehicle MG's repair (pass 50 kept the AAMG's two copies, ruling R50.7; a tank's weapons use one member)")
P = "Pass 50 (design {}), against the plan's entry"
F = "Pass 50 (design {}), against the frame"
LEFT = [
    ("Gun Duels", P.format("6, F5"), "C2.2401, pp. 167 to 168", "Pass 55: a declared attack that stands open between the two sides, an answer by the other side, and two resolutions in an order the Final DRs may decide", "The DEFENDER's shot resolves alone; the vehicle answers after the pass."),
    ("MG fire against armor: the To Hit and AP To Kill attack, its Stun on the Kill Number, the Collateral Attack, a vehicle's MA MG", P.format("9.1"), "A9.6, A9.61, A9.611, D3.54", "Pass 60: a MG as an ordnance weapon at every station of the ordnance shot (the Target Type, who fires it, its ROF and fire counter, no Case C3, the projector's test of a possessed weapon). Its Basic TK# of 4 is in the table's transcription", "A MG's IFT attack has no effect on armor and attacks a Vulnerable crew."),
    ("A BMG fired outside the VCA by a pivot of the hull, with the NT Case A on every weapon's first shot and the turret carried round", F.format("7.1") + " (the answer to F4)", "D1.81, D3.51, D3.52, D2.11, C5.11", "Pass 55: two remembered penalties a mount, and a TCA that turns with the hull (the row of 55.2)", "Refused: a BMG fires only inside the VCA."),
    ("A turret, or a VCA, changed without fire at the end of a fire phase", F.format("8.1") + " (the answer to F6)", "C3.22, D3.12, D2.11", "Pass 55: the Gun's action carried to a tank, with no more fire by any of its weapons, no move after a PFPh turn, and the gate's check", "A tank's turret turns with a shot or with an MP."),
    ("One hex for all of a vehicle's weapons", "Pass 50 reviews, 2026-10-12 (referee, first half, 4); the frame placed it in 50.2", "D3.51, p. 201", "The movement step on every weapon's shot, as the MA's has it", "A tank may Bounding First Fire one weapon in one hex and another in the next."),
    ("A CMG fired at another target loses the MA's Acquisition", F.format("7.1"), "D1.82, C6.5", "Pass 110, with the Acquisition's other losses (55.2)", "The counter is kept."),
    ("A declared higher MP expenditure on entry", "Pass 50 (design 8.1)", "D2.18, p. 196", "Pass 55: an entry step whose cost the planner and the projector take as declared", "A Delay MP after stopping serves."),
    ("The TCA turned with a Start MP; in woods, a building, or rubble (the doubled expenditure), with the woods-road exemption for a stop or a Delay; in Bypass", "Pass 50 (design 8.1; the reviews)", "D3.12, D2.32, D2.321, C5.11", "Pass 55", "Refused with the reason."),
    ("A Motion firer of the side that is not phasing", "Pass 50 (design 8.1)", "D2.42, C5.35", "Pass 55: which Case C a DEFENDER's vehicle in Motion adds to", "Refused."),
    ("A hit on another vehicle of a vehicle-overstacked hex; OVR Prevention", "Pass 50 (design 6, F1)", "A5.132; C5.64", "Passes 55 and 110", "Neither is built."),
    ("Defensive First Fire at a vehicle entering the firer's hex; a SW at a vehicle in its own Location; a same-hex shot by or at a vehicle in Bypass", "Pass 50 (design 9.1; the reviews)", "C5.51, D3.2, D2.32, C5.5", "Passes 55 and 60", "Refused with the reason."),
    ("A CMG's CA in Bypass", "Pass 50 (design 7.1)", "D2.32, D2.321", "Pass 55", "Read as its TCA is anywhere else."),
    ("The Known-target condition for a further change of CA in a phase", "Pass 50 (design 6, F4)", "D3.51, p. 201", "A fact on the attack's record that its target was Known", "A further change is allowed whatever the earlier target."),
    ("No Play test for the same-hex shot at a vehicle; no test of the Case L fix; no real key pressed in the Studio check", "Pass 50 (design 10.1)", "(tests)", "A setup that places a vehicle in an enemy vehicle's Location; a pane on screen", "Two Studio games and a Rules test of six cases hold task 50.6."),
]
R = "Pass 50 reviews, 2026-10-12 ({})"
FIX = "A fix in the member named in the review"
OPEN = [
    ("A CMG turns the turret in woods, a building, or rubble after the MA fired there", R.format("referee, first half, 6"), "C5.11, p. 172", FIX, "Allowed, with the doubled Case A."),
    ("A First Fire counter a MG placed in the MPh stops, in the DFPh, a MA that kept its ROF there", R.format("referee, first half, 2"), "D3.5, A.15", "A fact that survives the phase change: whose the counter is", "Within one phase the counter is not read as the MA's."),
    ("A Recalled tank's working MGs after its counter is flipped", R.format("referee, first half, 10"), "D5.341; ruling R7.10", "Recalled read as stunned only before the flip, as the repair's bar reads it", "A Recalled tank fires nothing."),
    ("Case J against a vehicle that has only spent Delay or Start MP", R.format("referee, second half, 5"), "C6.1, D2.12", "The moving-target fact of 55.2, with a fact on the record so that an older shot resolves as it did", "Any Defensive First Fire shot at a vehicle takes Case J, J1, or J2."),
    ("Case C in the AFPh for a vehicle that began in Motion and only stopped", R.format("referee, second half, 6"), "C5.2, C5.3; ruling R50.10", "A fact of its own for \"entered a new hex\", apart from the Case J list", "It takes Case C where the ruling gives Case B."),
    ("A bogged or immobilized vehicle's MP for a TCA change", R.format("referee, second half, 11"), "D8.5, p. 209", "Pass 105, with the row of 55.2", "The Delay step is refused for both."),
    ("Decisions still made outside Rules: that the counter prints the weapon repaired, and that the vehicle named is a tank; what an obstacle and a turret are for the turret's turn; which facing a weapon reads and which weapon turns the turret; the bars of a vehicle's MGs, gathered in Play; the repair list's own table of conditions; three smaller copies", R.format("Rules boundary 2, 3, 4, 6, 7, 8, 10, 11"), "D3.7, D3.12, D1.81, D1.82, D3.5", FIX, "Each works for the catalog's three vehicles; a record with a weapon name on a vehicle that prints none is not refused by the projector."),
    ("The turret choice shows where the gate will only refuse (bogged, an OVR pending, in woods); the MG read runs three times a vehicle in the render path", R.format("UI 7, 9"), "(page)", FIX, "The refusal gives the reason; the cost is small with three armed vehicles."),
    ("The record does not say that a CMG turned the turret", R.format("table player 8"), "D3.52, D3.12", "A reader of the fire record's `turretFacing` in PlayRecords and ReplaySteps", "Only the Case A line shows it; the ordnance list shows the new TCA."),
]
rows = lambda items: "".join(f"| {a} | {b} | {c} | {d} | {e} |\n" for a, b, c, d, e in items)
append(BACK, """## 58. Added by pass 50

Written with the pass's documents on 2026-10-12. The first table is what a section of the [pass 50 design](<Passes/ASL Unit Backlog Pass 50 Design.md>) lists as left out, with what the plan's entry or the frame placed in the pass and the build did not make first; the second is the reviews' findings that were not fixed (design 10.3).

### 58.1 What the pass left out

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + rows(LEFT) + """
### 58.2 The reviews' findings not fixed

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + rows(OPEN))

# --- The plan's status -----------------------------------------------------------------------------------------------
PLAN = "ASL Card Play and Map Studio Redesign Plan.md"
replace(PLAN, "Counters: none new (the PzKpfw IIIH and T-34 M41 suffice); a Gun Duel needs no counter. Needs first: 35 (vehicle states, Acquisition).",
        "Counters: none new (the PzKpfw IIIH and T-34 M41 suffice); a Gun Duel needs no counter. Needs first: 35 (vehicle states, Acquisition). **Status, 2026-10-12: built to the merge gate on `feature/asl-backlog-pass-50`, not merged.** Tasks 50.1, 50.2, 50.3, and 50.6 are built; 50.4 (Gun Duels) goes to pass 55 and 50.5 (MG fire against armor) to pass 60, each with its reason in the [design](<Passes/ASL Unit Backlog Pass 50 Design.md>), section 10.1, and the backlog's section 58.")

# --- The time log ----------------------------------------------------------------------------------------------------
append("ASL Unit Time Log.md", f"""## Pass 50: Armored combat I. To the merge gate

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| One session, 2026-10-12, under the user's word "work independently as much as possible up to merge gate and then pause". The start: the fetch, both of pass 45's CI runs green, the branch feature/asl-backlog-pass-50 cut from main at b4ce31ef, no Studio on 6671. Increment 1, the frame, read-only: the 39 rows listed; the rules read on pages 52, 64, 65, 167 to 169, 172 to 174, 195, 196, 198 to 201, and 203; one read-only agent mapped the vehicle weapon code (eight minutes); the design's six sections with questions F1 to F9 answered by their proposals; Gun Duels left out at the frame | 16:44 | 16:56 | 12 min | The plan gave no time to a frame. Committed bd65d433. |
| Increment 2a, tasks 50.1 and 50.2 in one commit: the fire pipeline read (the calculator, eligibility, follow-ups, the projector, the planner, the record's check); a vehicle's BMG and CMG named in the fire request, the state's list of weapons fired, Case A on the IFT with the turret's turn, the MA beside the MGs, the repair by weapon with the Recall; the page's rows; MapStudio built four times; six games on board 3; seven Rules tests and three Play tests; the touched classes, Units whole | 16:56 | 17:28 | 32 min | Estimate 0:40 and 1:00. The Studio found the gate's check after a repair looking for the wrong key. Committed e3253ad2; the replay digest after it: no line differs (785 s). |
| Increment 2b, task 50.3: the vehicle's steps and the projector's step check read; Bounding First Fire by the MA with Cases C, C1, C2, and C4, the MP between shots, the Bounding Fire counter, Case C in the AFPh, a Delay MP, the turret turned with an MP; the page's Delay, turret choice, and MA button; MapStudio built seven times; four games (the target moved to [H10] after the board's LOS tab showed [D10] does not see [I10]); four Rules tests and three Play tests | 17:28 | 18:12 | 44 min | Estimate 1:40. The Studio found four faults and the tests a fifth (design 8.5). One older Rules test rewritten to the rule. Committed 0fce601d; replay: no line differs (1,001 s, beside the tests). |
| Increment 2c, task 50.6: the same-Location branch and the armor run read; Case E and the colored dr's facing for a Gun's and a tank's shot at a vehicle in its own hex; two games; a Rules test of six cases. Task 50.5 read and left out: a MG is not a LATW at any station of the ordnance shot | 18:12 | 18:19 | 7 min | Estimate 0:20 and 0:40. Committed 1fa33dc8, which ended the build at 1:23 against the plan's 5:00, with two tasks of six not built. |
| The five reviews in one message, their briefs in a prompt file (three and a half to eight and a half minutes each); the findings judged: one BLOCKER (Case N in Bounding First Fire, C6.55) and some thirty others; the contained ones fixed in Rules, Play, Units, and the Studio, with a Rules test; nine games recorded again under new ids; the Studio check of the whole pass at five widths in three views, with a full Player Turn at 1920 by 1080 and at 320; the strict build of the two test projects; the replay digest a fourth time, no line differs; the documents | 18:19 | {NOW} | | The plan's overhead is 1:15. The pass by the clock, to the gate: 16:44 to {NOW}. The gate is not run: it is the user's to start. |
""")

# --- The standing rules: the lessons ---------------------------------------------------------------------------------
append("Prompts/Pass Standing Rules and Harness Lessons.md", """- **A rule a pass opens is reached by every old line beside it (pass 50).** The MA's Bounding First Fire was built with its own cases, and the old Case N and Case L lines, never reachable by a moving firer before, gave it an Acquisition DRM (C6.55) and Point Blank Range (C6.3). The build, the tests, and three Studio games passed; two reviews found it. When a shot that was refused is admitted, read every DRM line of its calculator against the new firer, not only the lines added.
- **Ask a read-only agent for the code map before the frame (pass 50).** One agent with eight numbered questions (records in full, members with lines, the page's panels, the tests' helpers, the catalog's values) answered in eight minutes what would have been an hour of reads, and the frame's "what is certain" came from it.
- **A vehicle's test game is three lines (pass 50):** `p50lib.Game.vehicle(id, definition, side, at, facing, **conditions)`; the tanks are `attacker-tank` (PzKpfw IIIH) and `defender-tank` (T-34 M41), the halftrack `attacker-halftrack`. A file may place two enemy vehicles in one Location, which the setup refuses. A turret cannot be preset in a file: it is turned by a shot or a step.
- **Board 3, more (pass 50):** [D10] sees [H10] (woods, range 4) and [G9] and [G10]; [D9] sees none of [H10], [I10], or [G9] (the woods of [E9] and the hill of [G10]); [D10] does not see [I10]. A tank that starts in [D9] facing south-east and enters [D10] comes into the LOS of a unit in [H10] with its first MP. The Play page's target lists name what a side knows, not what the firer sees: check a pair on `/boards/bd03`, LOS tab, before writing a game around it.
- **Driving the Play page, more (pass 50):** the vehicle panel has `#vehicle-delay`, `#vehicle-turret` (set it before clicking the step it goes with), `#vehicle-bff-target`, `.vehicle-bff-weapon[data-weapon]`, `#vehicle-bff` (the MGs), `#vehicle-bff-ma`, and `#vehicle-bff-vehicle`; the Fire panel lists a tank's MGs as `.fire-weapon` with `data-weapon` of `<vehicle>:bmg` and `<vehicle>:cmg`; the repair list's values for a tank are `<vehicle>|ma`, `|bmg`, `|cmg`. Under 1024 pixels the controls are in the Actions tab, which a commit may leave: click it again before each read. A collapsed section's options have an empty `innerText`: read `textContent`. A script of more than six commits passes the 45 second limit and still runs to its end.
- **The pane's real keys need the pane on screen.** `computer key` fails with "not on screen and has not drawn yet" while the pane is hidden, and a synthetic key event does not press a button. Say in the review that the keyboard was not exercised, rather than calling a script's `click()` a key.
- **Games recorded before a fix of the same pass are recorded again under a new id,** and the old ones labelled SUPERSEDED at once: a renamed firer id or a dropped DRM changes the recorded resolution, and the record's check compares it whole. The table player read the old labels while the relabelling ran.
- **The replay digest after each commit that adds a fact costs 13 to 17 minutes and overlaps the next task.** `pass50/replay_now.sh <tag>` rebuilds the proof tool against `bin/p31b`, compares its copy byte for byte, and runs from that copy, so the next task's edits and builds may go on beside it. With the pass's new properties in `PROOF_SKIP_50` every line must be equal; say that the store holds none of the pass's own games.
- **The slips of pass 50:** an Edit and the build that depends on it in one response (once; the build was run again); `cd` in a compound command (twice, each time returned); an empty heredoc (once, by reflex, as `python - file <<'EOF'`); a test that called an internal member of Rules (the build said so a task late). Tests of Rules reach its public members only.
""")

for name, body in files.items():
    io.open(D + name, "w", encoding="utf-8", newline="").write(body)
print("written", len(files), "files")
