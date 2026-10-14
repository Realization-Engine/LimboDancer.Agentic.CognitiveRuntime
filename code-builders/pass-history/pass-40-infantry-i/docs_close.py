"""Pass 40 documents: the design's sections 12 and 13 and its corrections, the review file, the backlog's section 56, the plan's status, and
the time log's row. Usage: python docs_close.py <start HH:MM> <end HH:MM>
"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)


def read(name):
    return open(DOCS + name, encoding="utf-8", newline="").read()


def write(name, text):
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text)


def once(text, old, new):
    assert text.count(old) == 1, (old[:80], text.count(old))
    return text.replace(old, new)


# --- The findings, once, for the design and the review file ---------------------------------------------------------------------------
FIXED = [
    ("Referee, enemy Locations, 1 (BLOCKER)", "A squad already marked First Fire that holds a MG dropped out of the attack owed: its plainest attack named no MG, which Subsequent First Fire refuses.",
     "Rules says how many MG a unit fires in the attack it owes (`WeaponsFired`): a marked unit fires them with its own FP. Play test `ASquadAlreadyMarkedFirstFireStillOwesTheAttackWithItsMg`."),
    ("Referee, enemy Locations, 2 and 5", "Two MMC with no leader could never make an OVR together; a leader's passed NTC let his squads enter without him.",
     "`NtcCovers`: each MMC under a passed NTC whose taker enters with it. The state's record keeps the taker."),
    ("Referee, enemy Locations, 3", "The NTC's DRM added a wall or hedge to the hex's TEM (B9.31, p. 119).", "`NtcDrm` takes the larger. Ruling R40.12 reworded."),
    ("Referee, enemy Locations, 4", "An Unarmed mover and a stack of Dummies could enter Unarmed enemy units (A20.54).", "`MayRecapture` over the movers."),
    ("Referee, enemy Locations, 6", "Where the units form no one group, any one of them satisfied the bar, though others could have fired with it.",
     "`FireBar` refuses while a unit that could fire in one group with those named is left out; Play asks the fire plan. Ruling R40.17 reworded."),
    ("Referee, enemy Locations, 7", "The bars named the DEFENDER's units to an ATTACKER who proposed fire.", "The fire bar is read only for a proposal whose firers are of the non-phasing side."),
    ("Referee, enemy Locations, 8; UI 5", "A concealed or hidden vehicle beside the SMC turned the NTC away; the DEFENDER's summary listed the Locations open to its SMC, so one left out told of a hidden ATTACKER unit.",
     "The vehicle clause is gone from `TargetBar`; `EvadeText` names no Location."),
    ("Referee, enemy Locations, 10", "A Guard's Deployment at will in the RPh was written as a RPh action and then barred its side's Recombinations.", "It is no RPh action in any phase. The design's words on which unit inherits the prisoners are corrected (11.2)."),
    ("Rules boundary, 1", "A CC record made before the pass, with a crew at its Gun, would fail its check: the rebuilt facts hold `Manning` and the record does not.", "The verifier takes `Manning` as recorded."),
    ("Rules boundary, 2, 3, 4, 7", "Play decided A8.312's preconditions, had its own usable-MG rule (which counted a dismantled MG), filtered the SMC's Locations, and the page decided A20.5.",
     "`Owed` takes the four facts and asks the fire plan last through a reader; the fire package's `UsableMgs` is the one rule; `EvadeDestinations` filters from each neighbour's facts; the page reads `GuardSquadsAtWill`."),
    ("Rules boundary, 6", "The kinds of fire a Dash halves were tested by the caller alone.", "The multiplier in Rules tests the kind and the ordnance hit."),
    ("Referee, movement, 1", "A Dash declared to a Location the stack cannot enter left it unable to step or to end.", "`EndBar` is lifted when the move's own plan refuses the step into that Location."),
    ("Referee, movement, 2", "One mover's break or pin ended the Dash for the whole stack.", "The fire's Dash fact is read over the members still moving."),
    ("Referee, movement, 4 and 5", "The target's overstacking DRM was read for a vehicle's fire at a stationary stack in the MPh and for vehicle targets, and it counted hidden and concealed units, so a proposal's DRM told of them.",
     "`TargetPenaltyApplies` (the movers in the MPh; Personnel), and the count is over the units the firer knows. Ruling R40.26."),
    ("Referee, movement, 7", "A failed push and attacks other than direct fire took no Hazardous Movement.", "The projector lists the crew on the Manhandling DR; the fact is set for every attack."),
    ("UI 1", "The leader's IPC name was never cleared and could be sent from a hidden select.", "Cleared with the other move drafts and when its unit is unchecked; sent only when a stack moves."),
    ("UI 2, 3, 4", "The adjudicator had no path to the attack owed and an enabled pass; the attack owed was computed twice a render; a stopped stack's note still offered a pass.",
     "One read a revision (`MandatoryNow`); the block serves the adjudicator; the note gives way to the attack owed."),
    ("UI 7", "The Guard's block did not say where the SW go.", "It says they stay with the first HS."),
    ("Table player, 1 and 2", "The note for units with different fire counters gave a ruling as the rule, and the pass refusal contradicted it.", "Both cite ruling R40.17 and say what the game does."),
    ("Table player, 3, 7, 8, 9; 6 in part", "\"Small arms\" for the Dash's halving; \"beside it\" twice; `play.move-occupied` stale; the SMC's block promising fire a leader has not; two refusals that did not say what was wrong.",
     "Reworded as the reviewer proposed."),
    ("Table player, 4 and 5", "A Guard's Deployment recorded under A1.31; a leader's NTC record without his stack or the cost of failing.", "The record cites A20.5, and names the stack and the consequence."),
    ("The Studio check", "The hand-over prompt told the DEFENDER \"fire at it or pass\" while the attack was owed.", "It says the attack is owed (A8.312)."),
]
ROWS = [
    ("Referee, enemy Locations, 9", "A lone SMC that enters Unarmed units has no CC in the MPh", "A20.54, p. 88", "The recapture's CC asks for a MMC of the stack."),
    ("Referee, enemy Locations, 10", "A crew at its Gun has a US# of 2, where A1.6 gives an attendant crew 4", "A1.6, p. 45", "No rule built reads it for a crew at its Gun."),
    ("Rules boundary, 5; referee, movement, 12", "The woods-road exception for vehicles, the Dash's SMOKE and DC bar, the bar on the other terrain's cost, and the two exceptions to A5.11 are decided in Play", "B13.31, A4.63, A4.132, A5.11", "Each works; each belongs in the Rules member beside it."),
    ("Rules boundary, 8 and 9", "What a Guard's Deployment at will waives, which acts end the start of the RPh, the two-HS test, and the recapture's exceptions are decided in Play", "A20.5, A1.32, A20.54", "Rules gives the yes or no; Play lifts the bars one by one."),
    ("Referee, movement, 3", "One mover's break ends Assault Movement for the whole stack", "A4.61, p. 51", "The unbroken movers take FFNAM in later First Fire."),
    ("Referee, movement, 6", "The road's second target point is tested only inside the target hex", "A4.132, p. 48", "A firer with a LOS to the hexside point alone is refused; one whose line to that point is blocked outside the hex still gets Open Ground."),
    ("Referee, movement, 8", "A CX leader still lends his IPC (older than the pass)", "A4.42, A4.52, pp. 50 to 51", "A HS with 5 PP and a leader who Double Times has six MF, where the page's example has five."),
    ("Referee, movement, 9", "Ruling R40.9 halves a whole Fire Group against a Dash", "A4.63, A7.52", "The page says the penalties apply to some DEFENDERS and not others; halving by firer is the nearer reading. For the user's second look."),
    ("Referee, movement, 10", "The leader's IPC: only a laden MMC of his nationality is helped, and the unasked default is not kept in the record", "A4.42, p. 50", "A second MMC that becomes laden mid-move takes the IPC from the first."),
    ("Referee, movement, 11", "The overstack MF is added after an Encircled unit's doubling", "A7.7, A5.11", "As in the advance (rulings R12.11, R14.10)."),
    ("Referee, movement, 1 (rest)", "The overstack MF and the lost Road Bonus of the Location Dashed to are not in the Dash's MF check", "A4.63", "A Dash may be declared that the second step cannot pay; the stack may then end in the road."),
    ("UI 6", "The \"Leader's IPC to\" list offers units that cannot be helped, and shows hexes without brackets", "A4.42", "The planner refuses the wrong choice."),
    ("Table player, 6 (rest)", "The Dash's refusal lists eight causes in one sentence", "A4.63", "The player is not told which applies."),
    ("Table player, 10", "Eight games share three labels, and four that are not fixtures do not say so", "(games)", "`p40-ovr-stack`, `p40-board`, `p40-bd04-dash`, and `p40-bd05-road` are not fixtures; the design says so, the labels do not."),
    ("Table player, 11", "The overstacking clause shows the rounded figure as the overstack", "A5.11, p. 52", "\"overstacked by 2\" where the Location is over by one and a half; the MF charged is right."),
    ("Table player (not checked)", "Whether a squad that takes its own NTC beside a leader should take his leadership DRM", "A10.7, p. 68", "A leader in the stack always takes the NTC; the player names one MMC alone to avoid it."),
    ("Replay", "A Guard's Deployment is filed as a \"rally\" step on the Replay page", "(page)", "It is shown with the RPh's actions."),
]

# --- The design -----------------------------------------------------------------------------------------------------------------------
design = read("Passes/ASL Unit Backlog Pass 40 Design.md")
design = once(design, "- **R40.12:** the NTC is an act of its own, taken before the step it allows, for one stack against one Location; it is taken once in a phase by those units against that Location. Its DRM is the TEM of the Location and of a hexside the stack would cross (the page says \"the TEM of the enemy's Location\"; a wall between the two is that Location's TEM to this stack). Hindrance within the Location is not counted.",
              "- **R40.12:** the NTC is an act of its own, taken before the step it allows, for one stack against one Location; it is taken once in a phase by those units against that Location. Its DRM is the TEM of the Location: the hex's, or that of a wall or hedge the stack would cross when it is the larger, never both (B9.31). Hindrance within the Location is not counted. A passed NTC covers a MMC only while its taker enters with it.")
design = once(design, "In that case one of the units that owe the attack makes it, alone or with others that owe it, and the rest are released; the page says so. A group of mixed kinds is a backlog row.",
              "In that case the units that owe it and can fire as one group with those named must all be named, no one else fires with them, and the rest are released; the page says so and cites this ruling. A group of mixed kinds is a backlog row.")
design = once(design, "One of its HS keeps the prisoners, by the projector's rule for a Guard that leaves play.",
              "The prisoners pass by the projector's rule for a Guard that leaves play: to the first armed unit of its side in the Location, by id, with the capacity, which is one of the two HS unless a third unit stands there.")
assert "## 12. The five reviews" not in design
rows_fixed = "\n".join(f"| {who} | {what} | {done} |" for who, what, done in FIXED)
rows_left = "\n".join(f"| {who} | {what} ({rule}) |" for who, what, rule, _ in ROWS)
design = design.rstrip("\n") + f"""

## 12. The five reviews and the Studio check

Run on 2026-10-11 under the user's word of that evening: five read-only agents in one message (the referee in two halves, the table player, the Rules boundary, the UI and Blazor review), their briefs in `Prompts/Pass 40 Review Briefs.md`. They found one BLOCKER and about forty-five findings in all. Each was judged against the code and its page; those with a contained fix were fixed, and the rest have a row in the backlog's section 56. Nothing was put to the user first, which the user's word allowed; every fix below is open to a veto.

### 12.1 Fixed

| Review | What was wrong | What was done |
|---|---|---|
{rows_fixed}

### 12.2 One more ruling

- **R40.26 (A5.13, A5.131, A5.132):** the target's overstacking penalty is read for Personnel targets only, in the MPh for the moving side's units only, and over the units the firing side knows (neither hidden nor concealed), so that a proposal's DRM tells the firer of no unit it may not know. The firer's own penalty counts all its units.

### 12.3 Not fixed: a row each in the backlog's section 56

| Review | What |
|---|---|
{rows_left}

### 12.4 The Studio check

In my Studio on port 6671, built into `bin/p31b`, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320): the move panel with its new inputs, measured for anything past the right edge. Nothing at four widths. At 320 the inspector's tab row is 11 pixels wider than the page on the Map tab (`play-inspector-tab-evidence`); the pass did not touch it, and it has a backlog row. The Actions tab, where the pass's controls are, fits.
- **A full Player Turn at 320:** a move, the DEFENDER's pass, the end of the move, and every phase to the next Player Turn's PFPh, switching views as the game asked; then the berserk charge with the attack owed, its note and button within the page.
- **A full step at 1920 by keyboard:** the OVR's NTC proposed and confirmed with the Enter key; the entry at double the MF; the adjudicator's view (the SMC's block shown); the DEFENDER's view; the SMC's move away.
- **After the fixes:** the mixed-counter game again, with the hand-over prompt's new words, the note that cites ruling R40.17, the adjudicator's block with no pass, one unit's attack, and the pass.
- **The three small things of section 5.2:** the phasing side's view in the DFPh shows a sentence ("The Russian side ends the Defensive Fire Phase."), not a disabled button; the hand-over prompt at 320 lies in the page's flow and covers nothing; the "Latest" line still did not follow a phase change. The first two are closed; the third keeps its row.
- **Not walked:** each width in each of the three views for every control (the views were walked at 1920 and 320); the turn at 1920 beyond the MPh; the Replay page.

## 13. As built

Eight tasks, each a commit, then the reviews' fixes in one.

| Task | Rules | Play and Units | What a player meets |
|---|---|---|---|
| 40.6 | `ScenarioA1Stacking` | `LiveStacking`, the IFT's two overstack facts | One count of a stack in every phase; the MF and the DRM of overstacking in the MPh and on the IFT. |
| 40.2 | `IsWoodsRoadHex`, `RoadRateTargetTerrain`; `LosTargetPoints` in Maps | the move's `terrainRate`, the step's `RoadRate` | The road's rate by choice; a mover at the road rate fired on at one of two points. |
| 40.3 | `ScenarioA1Dash` | the move's `dash`, the fire's and ordnance's Dash facts | Dash. |
| 40.8 | `HazardousMovers` | the state's list, the ordnance fact | Hazardous Movement in every fire phase. |
| 40.7 | `PortagedTwiceBar`, `MoveTiming`, `AssaultHolds`, `RecoveryBypassBar` | the phase's portage list, the move's `ipcTo` | Seven corrections to portage, Assault Movement, Recovery, and boarding. |
| 40.4 | `ScenarioA1InfantryOverrun` | two actions, the move's `overrun`, the event `infantry-overrun-ntc` | Infantry OVR; recapture by entry. |
| 40.5 | `ScenarioA1MandatoryFire` | the pass and the fire refused while the attack is owed | The attack owed under A8.312; a crew's FP in FPF. |
| 40.9 | `NeedsNoLeader`, `GuardDeploysAtWill`, `RecombineStartBar`, `UnitSize`, `InAreas` | the Guard's Deployment, the setup's other half | Deployment and Recombining with no leader; Recombining at the RPh's start; the butted half-hex. |

**New in a game's record:** the event `infantry-overrun-ntc`; the choice kind `overrun-evade`; on a step, `roadRate`, `dash`, `ipcTo`, `infantryOverrun`; on a fire record, `firerOverstack`, `targetOverstack`, `dash`; on an ordnance record, the Dash and Hazardous Movement facts. Each is absent from an older record and reads as it did. A game that holds the new event cannot be read by a build before the pass.

**Rulings:** R40.1 to R40.11 approved by the user; R40.12 to R40.26 decided under the user's word of 2026-10-11 and open to a veto. All 26 are in the Passes Plan.

**Not built, against the answer to question F10:** a CE or BU choice at setup, and the half-hex rule for entry and occupation (section 11.4).
"""
write("Passes/ASL Unit Backlog Pass 40 Design.md", design)

# --- The review file --------------------------------------------------------------------------------------------------------------------
review = f"""# Scenario A1 Backlog Pass 40: Infantry I

The review of pass 40, written on 2026-10-11 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 40 Design](<ASL Unit Backlog Pass 40 Design.md>); its section 13 says what was built.

## Status

Built on branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc: the frame, eight tasks in eight commits, then the five reviews, their fixes, the Studio check, and these documents. The first five tasks were each committed at the user's word. The last three, the reviews' fixes, and rulings R40.12 to R40.26 were made under the user's word of 2026-10-11, "Complete Pass 40 without my intervention", and are open to a veto.

Two things the user's answer to question F10 named as built are not: a CE or BU choice at setup, and the half-hex rule for entry and occupation (design 11.4).

## What a player now meets

- A stack is counted one way in every phase. Overstacking costs MF in the MPh as in the APh, and its DRM is on the IFT as on the To Hit and CC DR (A5.1 to A5.5).
- Across a road hexside the mover chooses the road's rate or the other terrain's cost. At the road rate it is fired on at the hex's center or at the point where it crossed, whichever the firer sees better (A4.132).
- Dash (A4.63).
- Hazardous Movement holds in every fire phase until the unit is pinned or broken or the Player Turn ends (A4.62, C10.3).
- A SW dropped mid-move gives no MF back; no item is portaged twice in a phase; a moved MMG or HMG does not fire in the AFPh; the leader's IPC goes to the MMC the mover names; no Recovery in Bypass; four MF for a unit that boards (A4.4 to A4.44, A4.11).
- An Infantry MMC may overrun a lone Known SMC: a NTC, double the MF, the SMC's move away, CC at once. Unarmed enemy units are entered and attacked in CC at once (A4.15 to A4.152, A20.54).
- When an Infantry MMC enters a Location of the DEFENDER's armed, unbroken Infantry, the DEFENDER must attack it before it may pass (A8.312). A crew that fired its Gun adds its FP to FPF (A8.311).
- A Guard of prisoners and an Unarmed unit Deploy and Recombine with no leader; a Guard squad Deploys at will; HS Recombine only at the start of the RPh (A1.31, A1.32, A20.5).

## The rulings

R40.1 to R40.26, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. R40.1 to R40.11 were approved by the user on 2026-10-11. R40.12 to R40.26 were decided without the user, each by the proposal that would have been put.

## The reviews' findings and what was done

One BLOCKER, fixed. The findings fixed:

| Review | What was wrong | What was done |
|---|---|---|
{rows_fixed}

The findings not fixed, each with a row in the backlog's section 56:

| Review | What | What happens now |
|---|---|---|
""" + "\n".join(f"| {who} | {what} ({rule}) | {now} |" for who, what, rule, now in ROWS) + """

## The Studio check

Design section 12.4. The pass's controls fit at the five widths; a full Player Turn was walked at 320 pixels and a full OVR step at 1920 by keyboard; the hand-over prompt's words were found wrong for the attack owed and corrected.

## What was not done

- A Studio game for a crew's FP in FPF and for the butted half-hex: neither can be reached through the page (design 10.2, 11.5). Tests hold both.
- Each width in each view for every control, and the Replay page.
- Play tests for the recapture, the stack held in Melee, the leader's IPC choice, the Assault step, the advance through SMOKE, boarding, and Recovery in Bypass: each was seen in the Studio or rests on a Rules test.
"""
write("Passes/Scenario A1 Backlog Pass 40 Review 2026-10-11.md", review)

# --- The backlog ------------------------------------------------------------------------------------------------------------------------
LEFT = [
    ("A hit on another vehicle of a vehicle-overstacked hex", "Pass 40 (design 7; the user, 2026-10-11)", "A5.132, p. 52", "Pass 50: the To Hit's target choice", "No such hit."),
    ("A SMC's Recovery at once when a unit is eliminated, surrenders, or routs away", "Pass 40 (design 9.4)", "A4.44, p. 50", "Pass 45: an act in the other side's phase; each loss naming the SW it left", "Recovery by the ordinary attempt only."),
    ("Unpossessed SW removed in marsh, water, a Blaze, or a rubbled building", "Pass 40 (design 9.4)", "A4.43, p. 50", "Passes 125, 140, 185", "They stay where they were dropped."),
    ("Four MF after dismounting; the Double Time bar for a unit that boards later in the turn", "Pass 40 (design 9.4)", "A4.11, A4.5", "Pass 65", "Built for a unit that boards, at the boarding."),
    ("A hero lending his IPC; the leader's IPC as a choice in the APh", "Pass 40 (design 9.4; ruling R40.11)", "A4.42, p. 50", "Pass 45", "Only a leader lends it; in the APh it goes to the only laden MMC."),
    ("The German dismantled MMG or HMG firing as a LMG; INF and RCL SW", "Pass 40 (design 9.4)", "A4.41, p. 50", "Pass 45; the catalog", "A moved MMG or HMG does not fire in the AFPh."),
    ("A second Infantry OVR against a SMC already in Melee; a SMC in an AFV; Human Wave's entry; the Disrupted exception", "Pass 40 (design 10.1)", "A4.14, A4.15, A4.152, p. 49", "Pass 75", "Each is refused."),
    ("Hindrance within the Location in the NTC's DRM; minefields, FFE, and Wire among the SMC's Locations", "Pass 40 (design 10.1; rulings R40.12, R40.14)", "A4.15, A4.151", "Passes 45 and 120", "Not read."),
    ("A leader joined to the prefilled CC attack of an OVR; the recapture as a capture attempt; the CC panel's note, which says \"overran the lone ...\" for a recapture", "Pass 40 (design 10.1; ruling R40.15)", "A4.152, A20.54", "The CC panel", "The player may add the leader; the recapture is a plain CC attack."),
    ("One Mandatory FG of units whose fire counters differ", "Pass 40 (design 10.2; ruling R40.17)", "A8.312, A7.55; ruling R12.3", "Pass 45: a kind of Defensive fire to each firer of an attack", "The units that form one group fire; the rest are released, and the DEFENDER may pick the weaker group."),
    ("A Gun's shot, a thrown DC, and a Starshell held behind the attack owed", "Pass 40 (ruling R40.19)", "A8.312", "Pass 45", "Only small-arms and MG fire is held."),
    ("OVR Prevention", "Pass 40 (design 10.2)", "C5.64", "Pass 50", "A crew fires no Gun in FPF."),
    ("A Guard's attack on a unit that enters its Location", "Pass 40 (design 10.2; question 5 of the frame)", "A8.312, A20.5", "Pass 75", "A Guard owes no attack: fire from its Location is refused whole."),
    ("A crew offered as a firer on the page; a berserk charge into a crew's Location", "Pass 40 (design 10.2); rulings R24.8, R24.3, R30.5", "A8.311", "Pass 110", "A crew's FPF is reached only by the action; the charge is refused."),
    ("An FPF whose NMC replaces a firer by ELR is refused at the commit", "Pass 40, found by a test with DR 11 (design 10.2)", "A8.31, A19.13", "A read of the FPF record's follow-ups", "UNIT-STATE-007: the replaced unit \"is consumed and cannot act\". Not followed."),
    ("A CE or BU choice at setup", "Pass 40 (design 11.4), against the answer to F10", "A2.52, D5.3", "Pass 65: a field of the setup panel by counter kind", "An AFV sets up CE."),
    ("A butted half-hex for entry and for occupation; the setup highlight, which reads it by its owner's name", "Pass 40 (design 11.4), against the answer to F10", "A2.3, p. 45", "Pass 185, with the Victory Conditions", "The setup check is built."),
    ("Finns, U.S.M.C. 7-6-8s, and Carrier HS without a leader; \"the same terrain\" for Recombining; the US# of horses and of large vehicles", "Pass 40 (design 11.4)", "A1.31, A1.32, A1.6", "Passes 80, 100, 165, 180", "Not in the catalog or not read."),
    ("Provisional turns and a chart past END", "Pass 40 (ruling R40.24)", "A3.9, p. 47", "The first card that prints one", "No card of the library has one."),
    ("The inspector's tab row is 11 pixels wider than the page at 320 pixels, on the Map tab", "Pass 40's Studio check", "(page)", "The inspector's tabs", "The page scrolls sideways by 11 pixels there."),
]
backlog = read("ASL Unit Backlog.md")
assert "## 56. Added by pass 40" not in backlog
backlog = once(backlog, "| The phasing side's view in the DFPh, the \"Latest\" line through phase changes, and the hand-over prompt at 320 pixels |",
               "| The \"Latest\" line through phase changes (the DFPh's sentence and the hand-over prompt at 320 pixels were seen right in pass 40's Studio check, 2026-10-11) |")
section = """
## 56. Added by pass 40

Written with the pass's documents on 2026-10-11. The first table is what a section of the [pass 40 design](<Passes/ASL Unit Backlog Pass 40 Design.md>) lists as left out or that a game or a test found; the second is the reviews' findings that were not fixed (design 12.3).

### 56.1 What the pass left out, and what it found

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + "\n".join(f"| {a} | {b} | {c} | {d} | {e} |" for a, b, c, d, e in LEFT) + """

### 56.2 The reviews' findings not fixed

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + "\n".join(f"| {what} | Pass 40 reviews, 2026-10-11 ({who}) | {rule} | A fix in the member named in the review | {now} |" for who, what, rule, now in ROWS) + "\n"
write("ASL Unit Backlog.md", backlog.rstrip("\n") + "\n" + section)

# --- The plan's status ------------------------------------------------------------------------------------------------------------------
plan = read("ASL Card Play and Map Studio Redesign Plan.md")
head = "**Pass 40: Infantry I: movement, stacking, and Locations** (new; 49 rows of the inventory). Counters: none. Needs first: 35.\n"
assert plan.count(head) == 1 and "**Status, 2026-10-11:** built on the branch `feature/asl-backlog-pass-40`" not in plan
plan = plan.replace(head, head + "\n**Status, 2026-10-11:** built on the branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc, one commit a task, with the "
                    "[design](<Passes/ASL Unit Backlog Pass 40 Design.md>) written as it went: tasks 40.2 to 40.9 (40.1 was taken by pass 35), the five reviews with their fixes, "
                    "the Studio check, and the [review](<Passes/Scenario A1 Backlog Pass 40 Review 2026-10-11.md>). Rulings R40.1 to R40.26. Moved on: A5.132 to pass 50, a SMC's immediate "
                    "Recovery to pass 45, the removal of SW to passes 125 and 185, a CE or BU choice at setup to pass 65. The gate's result and the merge are in the time log.\n")
write("ASL Card Play and Map Studio Redesign Plan.md", plan)

# --- The time log -----------------------------------------------------------------------------------------------------------------------
log = read("ASL Unit Time Log.md")
row = ("| 40.9 committed (9d96001b), which ends the build. The five reviews in one message, their briefs in a prompt file (three to eight minutes each); the Studio check of the whole pass "
       "at five widths with a full Player Turn at 320 and an OVR step at 1920 by keyboard; the findings judged, 22 groups of them fixed (one BLOCKER) and 17 given backlog rows; "
       "the tests for the fixes; the documents: the design's sections 12 and 13, the review, 26 rulings into the Passes Plan, 48 inventory rows with the count tables and the coverage counts, "
       f"the backlog's section 56 (20 and 17 rows), the plan's status | {start} | {end} | {minutes} min | "
       "The gate's baseline ran beside this and its replay stopped on a game the old build cannot read (the pass's new event); the proof tool is patched to skip such games. "
       "A script of fixes stopped half way on an anchor found twice and was finished by a second script. One more ruling, R40.26, came from the reviews. "
       "Not done: each width in each view for every control; the Replay page; Studio games for a crew's FPF and the butted half-hex. |\n")
write("ASL Unit Time Log.md", log.rstrip("\n") + "\n" + row)
print("ok", minutes, len(FIXED), len(ROWS), len(LEFT))
