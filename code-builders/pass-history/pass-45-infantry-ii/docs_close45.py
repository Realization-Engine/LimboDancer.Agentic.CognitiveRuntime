"""Pass 45 documents: the design's sections 12 and 13, the review file, the backlog's section 57, six rulings into the Passes Plan, the plan's
status, the standing rules' lessons, and the time log's row. Every target is read and checked before any is written.

Usage: python docs_close45.py <start HH:MM> <end HH:MM>
"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)
WORD = "decided under the user's word of 2026-10-12 (\"Implement all parts of Pass 45 with out my intervention\"), open to the user's veto"


def read(name):
    return open(DOCS + name, encoding="utf-8", newline="").read()


def once(text, old, new):
    assert text.count(old) == 1, (old[:80], text.count(old))
    return text.replace(old, new)


# --- What departs from the frame, once, for the design, the review, and the backlog -------------------------------------------------------
DEPARTURES = [
    "Ordnance HE's Residual FP (A8.25) is not built; it goes to pass 110. The frame placed it in task 45.3 (design 9.1).",
    "Spraying Fire in the MPh (A9.52, A8.24) is not built. The frame placed it in tasks 45.3 and 45.4 (design 10.1).",
    "Fire Lanes are completed in one point of six, the forced cancellation (A9.223). Lanes against vehicles, attacks on later MF expenditures, the Alternate Hex Grain, the Fire Lane Snap Shot, and hexside TEM and Hindrance stay in the backlog (design 10.1).",
    "The mortars' Repair number is not manufactured, so a mortar is still not repaired (A9.72). The answer to question F9 was to manufacture it; that needs a catalog version, which the pass did not open (design 11.1).",
    "SW Self-Destruction (A9.73) is not built (design 11.1).",
    "Task 45.10 built two things of pass 40's movement rows: a CX leader lends no IPC, and the TI rule of question F8. A hero's IPC, the choice in the APh, the Recovery at once of A4.44, A4.61 by unit, and the overstack MF of A4.63 stay in the backlog (design 11.3).",
    "No Studio game for Mandatory Fire Direction (A9.4): it needs a clear LOS of 17 hexes, which I did not look for. A Rules test holds it.",
    "The frame's point 5.7 is corrected: A7.353's halving is the Area Fire's own, not a second one. Point 5.6 is withdrawn.",
]

# --- The reviews' findings ------------------------------------------------------------------------------------------------------------------
FIXED = [
    ("Referee, first half, 1 (BLOCKER)", "A squad marked First Fire could not fire its own FP in one group with a unit that fires its MG alone, which is the example of p. 62: the attack was read as First Fire because a firer fired a MG alone, and the marked squad was then refused.",
     "`FireKindFacts.EveryFirstFireMarkedFiresAlone`: the attack is First Fire only when every marked firer fires a MG alone on its ROF. An unmarked unit fires beside a marked one. Rules test `TheReviewsFixesOfTheMixedGroup`."),
    ("Referee, first half, 2", "The \"every MG\" bar of A8.3 bound a unit with no counter that fired beside a marked one.", "`EveryMgBar` reads the firers that use Subsequent First Fire or FPF."),
    ("Referee, first half, 3", "A crew that fired its Gun was judged by the attack's kind, not by its own counter.", "Its own kind: with no counter it has no FP in the group's First Fire; marked Final Fire it joins FPF."),
    ("Referee, first half, 4", "A leader who had directed could direct a group that holds a unit with no counter.", "`director-outside`, as the example says (\"leader direction is NA\")."),
    ("Referee, first half, 5", "Prep Fire at a \"?\" stack of Dummies took the Bore Sighting DRM.", "A Dummy is Infantry to the firer: outside First Fire the DRM is given only where the Location holds no target at all."),
    ("Referee, first half, 6; Rules boundary, 7", "The four conditions of C6.43 and the dismantled weapon were decided in Play; the setup check read no Normal Range and no 16-hex limit.",
     "`MgSightingFacts` and `MgBoreSighted` in Rules; `MgBoreSightBar` takes the Normal Range and the LOS with its range."),
    ("Referee, first half, 7", "A Residual FP attack did not read the Encirclement of A7.72.", "It does."),
    ("Referee, second half, 1", "A HS that fires a MG, which adds no FP of its own, was refused Spraying Fire with its MG.", "The capability test counts a firer's own FP only where it adds it (`AddsOwnFp`)."),
    ("Referee, second half, 2", "Extreme Winter's lowered B# made an X#.", "Only the causes A.11 lists do (Sustained Fire, capture, Inexperience); the weather lowers the B# and the X# alike."),
    ("Referee, second half, 3 and 12; Rules boundary, 1", "The Field of Fire gave a shot within the MG's own hex a bearing, and found a MG's earlier shots by its unit.", "No bearing within the hex; the earlier shots are found by the weapon."),
    ("Referee, second half, 5", "The projector's leave for a MG's later shot at the same Location held for any MG fired alone.", "`KeptRofThisPhase`: only a weapon that kept its ROF in an earlier record of the phase."),
    ("Referee, second half, 6", "A lone hero used Sustained Fire.", "A hero is a SMC (A9.3)."),
    ("Referee, second half, 7", "An ATR's FP was set apart from the Residual FP twice.", "Once."),
    ("Referee, second half, 9", "The halving at 17 hexes and more was applied against every target, and a second time beyond the NVR.", "Against Personnel only, and once."),
    ("Referee, second half, 10; Rules boundary, 4", "The projector took the Residual FP of a ROF given up without asking whether the weapons had kept it.", "It takes the figure only when the weapons that kept their ROF carry a fire counter."),
    ("Referee, second half (A8.21, A8.22, A8.23)", "The ROF was offered for a counter no larger than the one standing there; a Sniper Check written with the attack closed the offer; the once-a-Location rule was read for a stack, not a unit.",
     "`RofForfeitOpen` takes the standing FP; `PartOfTheAttack`; `ResidualAttacksAgain` by unit."),
    ("Referee, second half (A4.52, A22.1, A9.8)", "One CX leader took the IPC from a fresh leader of the stack; a FT at the next level of its hex was given PBF; a dismantled mortar's refusal was Play's.",
     "The leader that lends is one that is not CX; the FT's band is Normal; `DismantledShotBar` and `LmgDefinition` in Rules."),
    ("Rules boundary (two members)", "Play decided when the other acts wait behind the attack owed, and who holds a Location of the A7.72 path and whom the rule exempts.",
     "`OtherActBar` takes the phase, the side, and a reader of the attack owed; `ScenarioA1UpperLevelEncirclement` takes `UpperLevelUnitFacts` and `PathUnitFacts` (`Applies`, `Holds`). A search that reaches its limit calls no unit Encircled."),
    ("Rules boundary (the bar's words)", "`play.act-mandatory` could name the units that owe the attack to an ATTACKER whose shot named a DEFENDER's vehicle as its target.", "The bar reads the acting units and weapons only."),
    ("UI 1 (BLOCKER)", "The page dropped a possessed MG's Bore Sighted Location from the setup it sent, so no MG set up on the page was ever Bore Sighted.", "Sent, as a Gun's is. Seen in game `p45-setup-bore`: the `bore-sighted` event, visible to its side only."),
    ("UI 4 and 5", "The A7.72 search ran for every unit at every render, and during setup, when a view does not hold what the rule counts.", "`CutOffUpstairs`: once for a revision and a view, for units above ground level, and not while setup is open."),
    ("UI 7, 8, 10", "An AAMG's row called another level of its hex \"out of range\"; two Locations of one hex were compared through the map's layers; the Bore Sighted Location was shown without brackets.", "The row says no attack at another level; the levels are compared directly; `PlaceOf`."),
    ("UI 9; table player, 5", "The ROF offer's note spoke of one weapon where several kept their ROF, and was not announced.", "Singular and plural; `role=\"status\"` and `aria-describedby`."),
    ("UI (the Spray field); table player (three words)", "The Spray field's title sat on an empty label; the DRM list showed `bore-sighting` and `mandatory-fire-direction` raw; the dice list showed `fire-cower-selection` raw; a weapon removed on its X# was called malfunctioned.",
     "The title is the field's; `DisplayText` has the three; `FireText` says the weapon is removed (A.11, A9.7)."),
    ("Table player (two refusals)", "`play.fire-field` did not say what the Field of Fire is; a TI unit's refusals did not say what it may still do.", "Both reworded."),
    ("Table player (the games)", "The Fire Lane game placed no lane: its DR of 11 malfunctioned the MMG. The two Bore Sighting games rolled 11 too.",
     "Recorded again: `p45-fl-lane4` (the lane placed along [F10], [E10], [D9], and gone after the attack owed in [F10]); `p45-fg-bore3` (the MMG alone, DR 7, minus 2); `p45-fg-unsighted2` (its control); `p45-fg-bore2` (the MMG with its squad's FP: no DRM)."),
    ("Table player (the labels)", "The false starts and one game that is no control did not say so in their labels.",
     "Seven labels now begin with SUPERSEDED, CONTROL, or NOT A CONTROL and the reason: `p45-fl-lane`, `p45-fl-lane2`, `p45-fl-lane3`, `p45-fg-bore`, `p45-fg-unsighted`, `p45-fg-bore2`, `p45-up-open`. The games are live Studio data and are not committed."),
]
ROWS = [
    ("Referee, second half, 4", "A Fire Lane's MG: the FP its lane takes, and the Residual FP and ROF offer of the attack that lays it", "A9.22, A8.23", "The lane's FP is the MG's as built in pass 12; the attack that lays it leaves its own Residual FP and makes no ROF offer."),
    ("Referee, second half", "The ROF is given up for all the weapons that kept it, not weapon by weapon", "A8.23, p. 60", "One offer a record: every weapon of it that kept its ROF gives it up."),
    ("Rules boundary", "`DirectedTheseMgs` is taken from the fire record and not checked against the phase's records at replay", "A7.53, p. 57", "The planner writes it from the records; the projector trusts it."),
    ("Referee, first half", "Directed FPF in the long example of p. 62, and the order of the director's NMC", "A8.31, A7.53", "Older than the pass: a director of FPF takes his NMC with the firers."),
    ("Referee, first half", "`WithinSubsequentFirstFireRange` is one fact of the attack, read over every firer's Location", "A8.3, p. 61", "A group of several Locations is held to the fact of the farthest."),
    ("Referee, first half", "TPBF against PRC from a higher Location", "A7.211, p. 54", "Not built; pass 65."),
    ("Table player", "The Cowering sentence, the Fire Lane's cancellation, the A7.72 morale penalty, and the ROF given up are not told in the \"Latest\" line or the outcome", "A7.9, A9.223, A7.72, A8.23", "Each is in the record and on the Records tab; the view the sentence is built from lacks the data."),
    ("UI", "The Target list offers the next level of a hex that no stairwell joins", "A7.21, B23.25", "The proposal is refused with the reason."),
    ("UI", "The Bore Sighted field shows for a LMG and for either side", "C6.41", "The setup check refuses it with the reason."),
    ("Table player", "The German LMG has B11 in the catalog, and the page's own example has B12", "A9.7; the catalog", "A catalog value, for the catalog's next version."),
    ("Referee, first half", "A DC attack does not read the Encirclement of A7.72", "A7.72, A23", "A Set or Thrown DC resolves without the upper-level read."),
    ("Rules boundary", "The test helper of `BacklogPass30Tests` is a copy of the page's `PlacementArguments`", "(tests)", "It hid the page's dropped field: the test passed and the page did not send it."),
]
LEFT = [
    ("Ordnance HE's Residual FP, and Air Bursts raising it", "Pass 45 (design 9.1), against the frame", "A8.25, A8.26, p. 60", "Pass 110: a fact that survives the ordnance record's replay", "An ordnance hit leaves none."),
    ("Spraying Fire in the MPh", "Pass 45 (design 10.1), against the frame", "A9.52, A8.24", "A design of its own: two records and the ROF-or-Residual offer", "Refused in the MPh, as before."),
    ("Fire Lanes: against vehicles, on later MF expenditures, the Alternate Hex Grain, the Snap Shot, hexside TEM and Hindrance", "Pass 45 (design 10.1), against the frame", "A9.22 to A9.222, p. 63", "Pass 135 (a LOS to a hexside); a record of whom a lane has attacked", "A lane attacks Infantry once as it enters a lane Location."),
    ("The mortars' Repair number, and so a mortar's repair", "Pass 45 (design 11.1), against the answer to F9", "A9.72, p. 65", "A catalog version, with the rulebook scan for the printed value first", "A malfunctioned mortar is not repaired."),
    ("SW Self-Destruction", "Pass 45 (design 11.1), against the frame", "A9.73, p. 65", "An action of its own", "Not offered."),
    ("A hero's IPC; a SMC as the one helped; the leader's IPC as a choice in the APh, and the unasked default kept in the record", "Pass 45 (design 11.3)", "A4.42, p. 50", "Pass 75", "Only a leader who is not CX lends it; in the APh it goes to the only laden MMC."),
    ("A SMC's Recovery at once when a unit is eliminated, surrenders, or routs away", "Pass 45 (design 11.3)", "A4.44, p. 50", "Pass 75: an act in the other side's phase", "Recovery by the ordinary attempt only."),
    ("A break ends Assault Movement for the unit, not the stack; the overstack MF and lost Road Bonus of the Location Dashed to", "Pass 45 (design 11.3)", "A4.61, A4.63", "Pass 75: a Final DR for each kind of target", "As pass 40 left them."),
    ("A MG that leaves its setup Location and comes back claims its Bore Sighting again; only the ground level of a building may be Bore Sighted; a light mortar's Bore Sighting", "Pass 45 (design 7.1)", "C6.42, C6.43", "Pass 110", "The game tests where the MG stands when it fires."),
    ("The CC counter on the co-occupants after a TPBF FPF", "Pass 45 (design 7.1)", "A8.31, p. 61", "Pass 75", "Nothing reads it."),
    ("The Gun's target list offers a Location that A7.212 refuses", "Pass 45 (design 7.2)", "A7.212, p. 55", "Pass 110", "The proposal is refused with the reason."),
    ("A group whose firers stand in the target's own Location and at the next level of its hex; a vehicle's MG at another level; IFE", "Pass 45 (design 8.1)", "A7.21, A7.5", "Pass 50; the catalog", "Refused, as any group that mixes TPBF with other fire."),
    ("A terrain Blaze on the path of A7.72; Scaling", "Pass 45 (design 8.2)", "A7.72, B23.424", "Pass 140", "Neither is read."),
    ("A Residual FP attack on an Infantry MF expenditure inside the Location; the activity completed first", "Pass 45 (design 9.3)", "A8.2, A8.22", "Pass 75", "SMOKE placement there is still refused (ruling R9.5)."),
    ("The pinned operator's CA carried to the DFPh, a stairwell's CA, and the INF, RCL, and 20L ATR types", "Pass 45 (design 10.4)", "A9.21, p. 62", "Pass 110; the catalog", "The Field of Fire is the phase's, for MGs."),
    ("Broken, berserk, or overstacked Infantry at 17 hexes and more; no effect against armored targets", "Pass 45 (design 10.4)", "A9.4, p. 64", "Pass 75", "Every Personnel target is treated as concealed at that range."),
    ("Starting a scenario dismantled; a captor dismantling; the fire panel's label for a dismantled HMG", "Pass 45 (design 11.2)", "A9.8, p. 65", "Pass 75; the fire panel", "The dismantle action is the owner's, in play."),
    ("No Studio game for Mandatory Fire Direction", "Pass 45 (design 10.1)", "A9.4, p. 64", "A board with a clear LOS of 17 hexes", "A Rules test holds it."),
]
RULINGS = [
    (1, "By whose counter does a firer of a Defensive fire attack fire? (replaces part of R12.3; closes R40.17)", "approved by the user on 2026-10-12 (question F4)",
     "Each firer of a Defensive fire attack fires by its own counter, and the attack's recorded kind is the heaviest. The Mandatory FG of A8.312 is every unit that owes the attack."),
    (2, "Which of the DEFENDER's acts wait behind the attack owed? (replaces R40.19)", "approved by the user on 2026-10-12 (question F4)",
     "While the Mandatory FG's attack is owed, a Gun's shot, a thrown DC, and a Starshell by the DEFENDER wait for it. The bar is on a proposal that names a unit or a weapon of the side that is not moving, and stands only while the attack can be made at all."),
    (3, "When does a MG take the Bore Sighting DRM against Infantry?", WORD,
     "Against Infantry the minus 2 is given in an attack of the kind First Fire only (C6.44 cites A8.1), and only where every element of the attack has Bore Sighted the Location: a MG fired with its squad's FP has none."),
    (4, "Which attacks take the pass's rules for Cowering, Residual FP, the X#, and Mandatory Fire Direction?", WORD,
     "Those planned from pass 45 on. Each records the fact `coweringMarksAndWeaponResidual`; an attack recorded before the pass has none and resolves as it was recorded, so no recorded game changes (the pattern of ruling R31.3)."),
    (5, "May a squad with no Spraying Fire option add its FP to its MG's Spraying Fire? (replaces the leave of R12.6)", WORD,
     "No: its MG sprays alone. A firer that adds no FP of its own (a HS with a MG, a squad with two SW) sprays with its MG as before. An attack recorded before the pass keeps R12.6."),
    (6, "What may a TI unit do with its SW and its HS?", "approved by the user on 2026-10-12 (question F8)",
     "Good Order is enough for Deployment, Recombining, a Transfer, a Drop, and Recovery in the RPh (A.7, A4.8). Recovery in the MPh, which costs a MF, and dismantling, which is the weapon's use, stay barred."),
]
LESSONS = """- **A page test whose helper copies the page's arguments proves nothing about the page (pass 45).** The MG's Bore Sighted Location was built, tested through a copy of `PlacementArguments`, and never sent by the page; a review found it. For a field the page sends, set it up by hand in the Studio and read the game's file.
- **Scripted dice of 11 malfunction a B11 weapon (pass 45).** Three games rolled 6,5 to make a point and proved another: the MMG malfunctioned, so no Fire Lane was placed and the Bore Sighting DRM sat on a broken weapon's attack. Read the weapon's B# before choosing the DR, and read the game's file back for the event the game is for.
- **A rule that changes a result needs a fact in the record (pass 45, ruling R45.4).** Two recorded games stopped replaying when Cowering's marks changed. The attack carries a fact from the pass on, and an older record resolves as it was; ask of every such rule whether a recorded attack would now resolve differently.
- **Under a word to go on, what the frame placed in a task and the build left out goes first (pass 45):** first in its design section, first in the review, first in the report, each with its backlog row and its reason.
- **A Hex Grain alternates rows:** [F10], [E10], [D9], not [D10]. The gate says so; the board's coordinates do not look it.
"""

# --- Read and check everything first ----------------------------------------------------------------------------------------------------------
design = read("Passes/ASL Unit Backlog Pass 45 Design.md")
backlog = read("ASL Unit Backlog.md")
passes = read("ASL Unit Backlog Passes Plan.md")
plan = read("ASL Card Play and Map Studio Redesign Plan.md")
rules = read("Prompts/Pass Standing Rules and Harness Lessons.md")
log = read("ASL Unit Time Log.md")
assert "## 12. The five reviews" not in design and "## 57. Added by pass 45" not in backlog and "| R45.1 |" not in passes
assert "Scripted dice of 11" not in rules
head = "**Pass 45: Infantry II: fire, MGs, and SW** (new; 38 rows of the inventory). Counters: none (the German HMG's and the mortars' dm values are manufactured, R0.3). Needs first: 40 (ADJACENT).\n"
assert plan.count(head) == 1
lines = passes.split("\n")
at = max(index for index, line in enumerate(lines) if line.startswith("| R40.26 |"))
for item in FIXED + ROWS + LEFT:
    assert all("|" not in part for part in item), item[0]

rows_fixed = "\n".join(f"| {who} | {what} | {done} |" for who, what, done in FIXED)
rows_left = "\n".join(f"| {who} | {what} ({rule}) |" for who, what, rule, _ in ROWS)
departures = "\n".join(f"- {item}" for item in DEPARTURES)
studio = """In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's: the ROF offer with its note and button (game `p45-walk-wide`), measured for anything past the right edge. Nothing at any width. The offer shows to the DEFENDER and the adjudicator and not to the ATTACKER. The setup's Bore Sighted field was measured at four widths (`p45-setup-bore`): inside the page at each.
- **A full Player Turn at 1920 by 1080:** the move into [D10], the LMG's shot alone that kept its ROF, the ROF given up with the Enter key on the offer and on Confirm, and every phase to the next Player Turn's MPh.
- **A full Player Turn at 320:** the same move and shot in each side's view, the offer, a second shot at the same MF refused with its reason (A8.3, A9.2), and every phase to the next Player Turn's PFPh; there the fire panel's Spray field with its title, and the dismantle button, inside the page. The inspector's tab row is 11 pixels wider than the page while a proposal is open, as pass 40 found; it has its backlog row.
- **The A7.72 tag:** "Encircled: no path to ground level (A7.72)" on the unit's row in `p45-up-cut` at 1920 and at 320, and absent in the control `p45-up-free`.
- **After the fixes:** a MMG set up by hand on the page with its Bore Sighted Location, and the event in the game's file; the Fire Lane placed and then gone after the attack owed in the MG's own Location; the Bore Sighting DRM on the MMG fired alone, and absent with its squad's FP and in the control.
- **Not walked:** every control of the pass at every width in every view (the offer and the setup field were; the fire panel's fields were measured at 320 and 1920 only); a turn that uses Spraying Fire or a Fire Lane at 320; the Replay page; the turn's phases beyond advancing them, since the two walk games hold one unit a side."""
built = """Ten tasks and 45.1b in eight commits, then the reviews' fixes in one.

| Task | Rules | Play and Units | What a player meets |
|---|---|---|---|
| 45.8 | `MphFireKind` by firer, `GroupMapFacts`, `MgBoreSighted`, `MgBoreSightBar`, `OtherActBar` | the fire plan's kinds, the `bore-sighted` event for a MG, the acts held behind the attack owed | A group of units with different fire counters; one chain of Locations; no berserk unit in a multi-Location group; a crew's FP; MG Bore Sighting. |
| 45.9 | the crew's Casualty Reduction, the A7.212 limit for ordnance | none | A crew eliminated by a K result; a shot held to the nearer enemy. |
| 45.1 | `SameHexLevelBar`, `FireRangeBand.SameHexFartherLevel` | the fire plan's level facts | PBF up and down a stairwell; Encirclement from above and below. |
| 45.1b | `ScenarioA1UpperLevelEncirclement` | `GamePlanner.Encirclement` | A unit upstairs with no free path to ground level is Encircled. |
| 45.2 | the Cowering marks and their Random Selection, `RofForfeitOpen` | the action `forfeit-rof`, the fact `coweringMarksAndWeaponResidual` | A cowering unit and its SW marked; the ROF given up for Residual FP. |
| 45.3 | the Residual FP cap, the firers' DRM, `ResidualAttacksAgain` | the projector's Residual FP check | Residual FP held to 12 after its reduction, a column lower for CX, none for a malfunctioned weapon, once a Location. |
| 45.4 | the spray capability off the trait (`SprayGroupBar`) | none | Squad Spraying Fire. |
| 45.5 | `FireLaneCancelledBy` | the projector ends the lane | A Fire Lane gone when its MG or squad fires in its own Location. |
| 45.6 | `FieldOfFireBar`, the 16-hex limit, `DirectedTheseMgs` | the phase's records read for a MG's earlier shots | A MG's Field of Fire in woods and buildings; a leader for fire beyond 16 hexes; his direction of a MG's later shots. |
| 45.7 | `FireWeaponEffect.Removed`, `MayDismantle`, `LmgDefinition`, `DismantledShotBar` | the removal on the X# | The X#; a lone SMC's MG; dismantling for the weapons A9.8 names; a German dm MG fired as a LMG. |
| 45.10 | `LeaderIpcRecipient` with `Cx`, the TI bars | none | A CX leader lends no IPC; a TI unit Transfers, Drops, Recovers, Deploys, and Recombines in the RPh. |

**New in a game's record:** the action `asl.game.forfeit-rof`, which writes events the game already had; the roll kind `fire-cower-selection`; on a fire record, `coweringMarksAndWeaponResidual`, `mgBoreSighted`, the level facts, and `directedTheseMgs`; on a weapon effect, `removed`; a MG as the `gun` of a `bore-sighted` event. Each is absent from an older record, which reads as it did (ruling R45.4). No event type is new.

**Rulings:** R45.1, R45.2, and R45.6 came from questions F4 and F8, which the user answered yes on 2026-10-12. R45.3, R45.4, and R45.5 were decided under the user's word of 2026-10-12 and are open to a veto. All six are in the Passes Plan.

**Not built, against the frame:** section 12.1.

**Tests after the reviews' fixes:** Rules 818 of 818, Units 409 of 409, Play 742 of 742, MapStudio 390 of 390, each project whole. The gate's sweep and digest, Docker, and the Node viewport test are the gate's and were not run."""

design_new = design.rstrip("\n") + f"""

## 12. The five reviews and the Studio check

Run on 2026-10-12 under the user's word of that day ("Implement all parts of Pass 45 with out my intervention", then "Continue without my intervention, but stop at the Gate"): five read-only agents in one message (the referee in two halves, the table player, the Rules boundary, the UI and Blazor review), their briefs in `Prompts/Pass 45 Review Briefs.md`. They found two BLOCKERs. Each finding was judged against the code and its page; those with a contained fix were fixed, and the rest have a row in the backlog's section 57. Nothing was put to the user first, which the user's word allowed; every fix below is open to the user's veto.

### 12.1 What departs from the frame, first

Each was decided under the user's word of 2026-10-12, and each has a backlog row (section 57.1).

{departures}

### 12.2 Fixed

| Review | What was wrong | What was done |
|---|---|---|
{rows_fixed}

One older test changed its expected reason: `SubsequentFirstFireIsAreaFireWithinRange` expected `firer-already-fired` for an unmarked unit alone in Subsequent First Fire, and now gets `subsequent-first-fire-outside`, since an unmarked unit may fire in such a group and an attack with no firer that uses Subsequent First Fire is not one.

### 12.3 Not fixed: a row each in the backlog's section 57

| Review | What |
|---|---|
{rows_left}

### 12.4 The Studio check

{studio}

## 13. As built

{built}
"""

review = f"""# Scenario A1 Backlog Pass 45: Infantry II

The review of pass 45, written on 2026-10-12 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 45 Design](<ASL Unit Backlog Pass 45 Design.md>); its section 13 says what was built.

## Status

Built on branch `feature/asl-backlog-pass-45`, cut from main at 5982767e: the frame, ten tasks and 45.1b in eight commits, then the five reviews, their fixes, the Studio check, and these documents. The frame's questions F1 to F10 were answered yes by the user on 2026-10-12. Everything after the frame was done under the user's word of that day, "Implement all parts of Pass 45 with out my intervention", and is open to a veto. **The gate has not been run, and nothing is merged or pushed:** the user said to stop before the gate.

## What departs from the frame

{departures}

## What a player now meets

- A unit fires up or down a stairwell at the next level of its own hex as PBF, and a LOF from directly above or below counts toward Encirclement (A7.21, A7.7). A unit upstairs with no path to ground level free of an unbroken, armed, unconcealed enemy unit is Encircled (A7.72).
- A unit that cowers is marked Final Fire or Prep Fire with all its SW; in a group, the unit is found by Random Selection (A7.9).
- A weapon that kept its Multiple ROF in the MPh left no Residual FP for its own FP; the DEFENDER may give the ROF up at once and place it (A8.23). Residual FP is held to 12 after its reduction, is a column lower for a CX firer, is none for a malfunctioned weapon, and attacks a vehicle once a Location (A8.2 to A8.26).
- A squad whose Range is underscored uses Spraying Fire with its own FP (A7.34). A squad without the option does not add its FP to its MG's spray (ruling R45.5).
- A Fire Lane is gone when its MG or its squad fires in its own Location (A9.223).
- A MG in woods or a building fires its later shots of a phase only inside the CA of its first (A9.21). A MG fires beyond 16 hexes only when a leader directs it, and its target at 17 or more is treated as concealed (A9.4). A leader directs the later shots of a MG he directed (A7.53).
- A weapon whose B# an attack lowered is removed on a DR at least its printed B# (A.11). A lone SMC uses no Sustained Fire (A9.3). The weapons A9.8 names are dismantled, and a German dm MMG or HMG fires as a LMG.
- Units with different fire counters fire as one group, each by its own counter (ruling R45.1); the group's Locations are one chain; a berserk unit joins no group of several Locations; a crew that fired its Gun keeps its FP in Subsequent First Fire and FPF (A7.5, A7.54, A7.353).
- A MMG or HMG of the Scenario Defender Bore Sights a Location at setup and deducts two from its attack there, fired alone (C6.41 to C6.44).
- A K result eliminates a crew (A7.302). A Gun's shot, a thrown DC, and a Starshell wait behind the attack owed under A8.312 (ruling R45.2).
- A CX leader lends no IPC (A4.52). A TI unit Transfers, Drops, Recovers, Deploys, and Recombines in the RPh (ruling R45.6).

## The rulings

R45.1 to R45.6, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. R45.1, R45.2, and R45.6 came from questions the user answered yes. R45.3, R45.4, and R45.5 were decided without the user, each by the proposal that would have been put.

## The reviews' findings and what was done

Two BLOCKERs, both fixed. The findings fixed:

| Review | What was wrong | What was done |
|---|---|---|
{rows_fixed}

The findings not fixed, each with a row in the backlog's section 57:

| Review | What | What happens now |
|---|---|---|
""" + "\n".join(f"| {who} | {what} ({rule}) | {now} |" for who, what, rule, now in ROWS) + f"""

## The Studio check

{studio}

## What was not done

- The gate, the merge, and the push: each is the user's to start.
- A Studio game for Mandatory Fire Direction, and Play tests for the two levels of a hex and for A7.72: the test boards have no stairwell building, so the games and the Rules tests stand for them.
- Each control at each width in each view, and the Replay page.
"""

section = """
## 57. Added by pass 45

Written with the pass's documents on 2026-10-12. The first table is what a section of the [pass 45 design](<Passes/ASL Unit Backlog Pass 45 Design.md>) lists as left out, with what the frame placed in the pass and the build did not make first; the second is the reviews' findings that were not fixed (design 12.3).

### 57.1 What the pass left out

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + "\n".join(f"| {a} | {b} | {c} | {d} | {e} |" for a, b, c, d, e in LEFT) + """

### 57.2 The reviews' findings not fixed

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + "\n".join(f"| {what} | Pass 45 reviews, 2026-10-12 ({who}) | {rule} | A fix in the member named in the review | {now} |" for who, what, rule, now in ROWS) + "\n"

lines[at + 1:at + 1] = [f"| R45.{number} | {question} | Pass 45; {decided}. {text} |" for number, question, decided, text in RULINGS]
plan_new = plan.replace(head, head + "\n**Status, 2026-10-12:** built on the branch `feature/asl-backlog-pass-45`, cut from main at 5982767e, with the "
                        "[design](<Passes/ASL Unit Backlog Pass 45 Design.md>) written as it went: tasks 45.1 to 45.10 and 45.1b, the five reviews with their fixes, the Studio check, and the "
                        "[review](<Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md>). Rulings R45.1 to R45.6. Not built, against the frame: ordnance HE's Residual FP (pass 110), "
                        "Spraying Fire in the MPh, five of six Fire Lane points, the mortars' Repair number, SW Self-Destruction, and most of pass 40's movement rows (pass 75). "
                        "**Stopped before the gate at the user's word; not merged.**\n")
rules_new = rules.rstrip("\n") + "\n" + LESSONS
row = ("| 45.10 committed (d138689f), which ended the build. The five reviews in one message, their briefs in a prompt file; the findings judged: 27 groups fixed (two BLOCKERs: a marked squad "
       "could not fire beside a unit firing its MG alone, and the page dropped a MG's Bore Sighted Location) and 12 given backlog rows; the A7.72 search and its facts moved wholly into Rules; "
       "two Rules tests of the fixes and my earlier tests moved to the members' new shapes; MapStudio built once; Rules whole (818), Units whole (409), Play whole (742, 13:56), MapStudio whole (390); four games recorded again "
       "and one set up by hand; the Studio check at five widths with a Player Turn at 1920 by keyboard and one at 320; the documents: the design's sections 12 and 13, the review, six rulings "
       f"into the Passes Plan, the inventory's rows with the count tables and coverage counts, the backlog's section 57 (18 and 12 rows), the plan's status, five lessons | {start} | {end} | {minutes} min | "
       "No estimate in the plan beyond the overhead of 1:15. One older Rules test changed its expected reason. My own slips: a Dummy flag in the wrong place of a test's constructor; a lane "
       "end named off the Hex Grain; the first Bore Sighting game fired the MMG with its squad, which became a control; one empty heredoc in a shell command, against the standing rule. "
       "Stopped before the gate at the user's word. |\n")

# --- Write ------------------------------------------------------------------------------------------------------------------------------------
for name, text in [
    ("Passes/ASL Unit Backlog Pass 45 Design.md", design_new),
    ("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md", review),
    ("ASL Unit Backlog.md", backlog.rstrip("\n") + "\n" + section),
    ("ASL Unit Backlog Passes Plan.md", "\n".join(lines)),
    ("ASL Card Play and Map Studio Redesign Plan.md", plan_new),
    ("Prompts/Pass Standing Rules and Harness Lessons.md", rules_new),
    ("ASL Unit Time Log.md", log.rstrip("\n") + "\n" + row),
]:
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text)
for new in (design_new[len(design.rstrip("\n")):], review, section, LESSONS, row, "\n".join(f"{q} {t}" for _, q, _, t in RULINGS)):
    assert "\u2014" not in new
print("ok", minutes, len(FIXED), len(ROWS), len(LEFT))
