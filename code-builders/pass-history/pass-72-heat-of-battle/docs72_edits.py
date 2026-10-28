"""Pass 72 documents: the Passes Plan's rulings, the backlog's section 75 and its older rows, the redesign plan's entries, and the coverage notes.
Every anchor is counted before any file is written (edit_files.Edits)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
from edit_files import Edits  # noqa: E402

e = Edits()
W = 'Pass 72; decided under the user\'s word of 2026-10-27 ("Run Pass 72 without my intervention up to the merge gate."), open to a veto.'

# ---- 1. The Passes Plan's rulings, after R71.28.
PLAN = "docs/ASL Unit Backlog Passes Plan.md"
R71_28_END = "the set-down that ends a carried man's move when his carrier is not among a later step's movers is this ruling's, not A17.2's. Decided at the reviews. |\n"
rulings = [
    ("R72.1", "Is a pass about 1.7 times the plan's size in building kept whole, and in what order is it built?",
     "Yes: it stays one pass, as R71.1 kept pass 71. The calibrated forecast was about 2:50 of building (about 5:20 to the gate) against the plan's 1:40 (2:55 in all). Built in the design's increments 5a to 5e, so that whatever a session's end left out would be the last increment's (the hero's weapons), to pass 73 with its row. Nothing was left out: the build took 3:15 by the clock (19:10 to 22:25)."),
    ("R72.2", "What leaves the pass?",
     "To pass 73: a berserk Dash across a road (with 73.3), and a heroic leader's heroic qualities in fire (A15.21: `FireFirer` has no heroic fact); to pass 105: the circled B# of vehicles and Guns and the Low Ammo counter at start under Ammunition Shortage (A19.131, D3.71; with 105.7), and a hero firing an AFV's AAMG as a Rider (with 105.1); to pass 110: a hero firing a Gun (at most 82mm as captured, above 82mm once a Game Turn, the CA bar of C2.27; with 110.6), a bazooka, a MOL-Projector, and a RCL (110.7 to 110.9); to pass 120: Wire as 1 MF in a charge's route, and minefields; to pass 150: an FFE, and OBA's Scarce Ammo under Ammunition Shortage (p. 391); to pass 185: a Blaze as a charge bar; to the backlog: the ordnance package's Inexperienced B# read by class only. Each has a row in the backlog's section 75.1."),
    ("R72.3", "How is the next nearest Known enemy found (A15.43, A15.45)?",
     "`Steps` runs one single-source search with every Known enemy Location terminal and reads the targets off it in groups by range in hexes: the nearest group first; when no route reaches any of it, the next group, and so on. The charge stays on a nearest unit whose route counts, in hexes stepped, no more than the next nearest group's range; a tie stays with the nearest. The answer stays under PR 32's memo key; a fall back makes one search, not more. A distance the map cannot read keeps R30.5. Amended by R72.17 (the comparison only where a bar lies across the route, against the next group that has a reachable target, and strict)."),
    ("R72.4", "What happens when no Known enemy can be reached (A15.44, E1.533)?",
     "A berserk unit whose charge reaches no Known enemy in LOS, where every refused entry on its search's frontier is a rule bar, stays in place and at the MPh's end loses berserk status and is Battle Hardened; its owner may refuse the Battle Hardening (A15.3, R5.9) by a pending choice of his side, kind `battle-hardening` (key `berserkHardening:<unit>`); refused, the unit is simply Good Order. A refusal that is a gap of the model keeps R30.5: the unit ends in place and stays berserk. The change is a planner-written event, never derived in the projector. Amended by R72.17 (a bar is a cliff or an unbridged Water Obstacle; the verdict needs a Known enemy unit in LOS and no target cut off only by other enemy Locations)."),
    ("R72.5", "May a charge enter a Location holding an enemy vehicle and enemy Infantry (moved in from pass 70)?",
     "Yes, like any other: `ChargeBarred`'s refusal and its text go; A8.312's FPF and pass 70's owed attack follow unchanged. A15.46's return after CC there needs every Known enemy unit eliminated, the vehicle included (`vehicleSides` on the CC facts, absent as before)."),
    ("R72.6", "What does a unit made berserk in mid move do (A15.431)?",
     "It charges with eight MF less those spent (built before the pass), and abandons at once every SW beyond A15.431's PP limits, not only 1 PP ones, the abandonment read at every step; one Play test pins the remaining MF."),
    ("R72.7", "When does a berserk unit return to normal (A15.46)?",
     "When it, or the group it attacks with, eliminates every Known enemy unit in its Location (at least one) with TPBF or a FT in the AFPh, or with CC that destroys an enemy vehicle there and leaves no other Known enemy. Decided in Rules on facts of the fire or the CC; written by Play as `conditions-changed` for each berserk member after the attack's effects. A FT in its own Location was already allowed by the Fire package. Read with R72.20."),
    ("R72.8", "What does a hero's weapon give (A15.23, A15.24, C13.25)?",
     "A hero firing a non-MG SW takes -1 on the To Hit DR of an ordnance SW (a light mortar, a PF, a PIAT, an ATR at a vehicle), never again on the HE's IFT after a hit and never on a To Kill (C13.25); an ATR fired as Small Arms is allowed and takes A15.24's heroic -1 and no second one. A lone hero may fire a PSK: +1 for one man firing a two-man SW, negated by his -1 (C13.45, C13.48). The DRM is named `hero:<id>` in the arithmetic and worded in the record lines, behind the shot's `heroWeapons` fact (absent as before). Guns, the AAMG as a Rider, the bazooka, the RCL, and the MOL-Projector leave (R72.2)."),
    ("R72.9", "Is a MG's B# lowered in Inexperienced hands (A19.32)?",
     "Yes, for attacks recorded with the pass's stamp (`unitSubstitution`): any SW used by Inexperienced Personnel has its B# (and X#) one lower, a MG included, which lifts R9.10 for stamped records. Inexperienced is read from the firer's fact (A19.3: a Green MMC stacked with an unbroken leader is not), or by Conscript class where an older record lacks it; the ATR's existing -1 takes the same read, and the FT keeps the -1 `AssaultWeaponRemoval` gives it (R15.1). A record without the stamp replays as recorded: `dl-tractor-works-01`'s Conscript LMG at revision 1267 (B11, DR 10) stays a hit."),
    ("R72.10", "How are Ammunition Shortage and an SSR ELR given (A19.131, A19.132)?",
     "As two card tokens on the pattern of `fanatic:`: `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>` with n from 0 to 4. Rules parses them; the card's diagnostics and a new game's `RulesBar` refuse a malformed one; the planner hands Rules a fact that is null when the token is absent. Both are public."),
    ("R72.11", "What does Ammunition Shortage's Unit Replacement do (A19.131)?",
     "When a side's on-board OB has Ammunition Shortage, an attack with an Original 12 IFT DR in which any MMC of that side used its Inherent FP Replaces one such MMC after the attack is resolved, chosen by Random Selection among them (A.9) after any SW malfunction or elimination; the Replaced unit keeps its broken or unbroken status (an MMC incapable of further quality loss is broken, unless berserk); an underscored squad is Replaced by its two HS, an underscored HS and a crew are incapable and break. No effect on CC, a Sniper, or Residual FP, nor on a To Hit DR of 12. The Random Selection dr and the Replacement are public. Read with R72.16 (ties) and R72.22 (A19.13's EXC)."),
    ("R72.12", "Which numbers does Ammunition Shortage lower (A19.131, A.11, A21.11)?",
     "Every SW's Original B# and X# one lower (not a DC, not a captured SW, A21.11), in IFT fire and as ordnance, a lowered B# becoming an X# by A.11; Ammunition Depletion Numbers one lower; no Fire Lane may be declared. The fact is recorded on the attack or shot, so the verifier checks it against the card. Vehicles' and Guns' circled B# go to pass 105 (R72.2). Amended by R72.18 (the Original B# becomes the X#, a printed X# alone lowered; a captured Gun's Depletion Number not lowered)."),
    ("R72.13", "How does an SSR ELR reach underscored units (A19.132, R15.9)?",
     "A target, and an FPF firer, carry an `underscoredElr` fact from the token; `ElrOf` reads it in place of R15.9's 5, and a failure beyond it Replaces the unit by the normal table. The German 8-3-8 has no entry: it is Replaced by the 4-3-6 Conscript, the only lower-Class German MMC none of whose factors exceeds the 8-3-8's (A19.13). The Finnish 8-3-8 stays as R15.13 has it. This replaces R15.9 where a token is given; R15.9 still holds where none is. Read with R72.15 and R72.19."),
    ("R72.14", "What is left of A19.31?",
     "Nothing in this pass: its conveyance exception is built (passes 40 and 65), verified and recorded only."),
    ("R72.15", "What replaces an underscored unit with no table entry under an SSR ELR? (decided at the build, 5c)",
     "Its HS of R19.7, where it has one (the German 3-3-8 by the 2-3-7: lesser Class, no factor higher, A19.13); only one with neither is Disrupted (A19.12). Decided at the build (increment 5c)."),
    ("R72.16", "Who is Replaced on a tie for the highest Random Selection dr? (decided at the build, 5d)",
     "Every tied MMC, as A.9 says (\"all of the applicable units which rolled that number are affected equally\"); R72.11's \"one such MMC\" is read so. Decided at the build (increment 5d)."),
    ("R72.17", "What is a rule bar in a charge, and when does A15.45's comparison apply? (added at the reviews)",
     "A rule bar is A15.45's terrain only: a cliff or an unbridged Water Obstacle (a Blaze is not built); a building connection is ordinary movement the shortest route already prices, never a bar. The comparison applies to a nearest target only when a bar lies across its would-be route (the route that ignores bars is cheaper than the real one), against the range of the next group that has a reachable target; with none, the charge stays on the nearest. \"Still closer\" is strict: at a tie the targets are equidistant and the ATTACKER chooses among both groups (A15.43). A15.44's verdict needs a Known enemy unit in LOS, none reachable, and only bars keeping them out of reach; a target cut off only by other enemy Locations, or a gap of the model, keeps R30.5; a unit with no Known enemy in LOS returns to normal (A15.431) and is never hardened. This amends R72.3 and R72.4. Decided at the reviews, built at fix Round A."),
    ("R72.18", "What is a SW's X# under Ammunition Shortage? (added at the reviews)",
     "Its B# is one lower and its Original B# becomes its X# (A.11 and its example, p. 43): a B11 LMG is B10 and X11; only a printed X# (a FT's removal number) is itself one lower. Depletion Numbers are not lowered for a captured Gun (A21.11). This amends R72.12. Decided at the reviews, built at fix Round B."),
    ("R72.19", "Is a morale check's SSR ELR checked at replay? (added at the reviews)",
     "Yes: a morale check record carries the stamp `unitSubstitution`, and with it its `underscoredElr` is checked against the state; a record without it verifies as before. Decided at the reviews, built at fix Round B."),
    ("R72.20", "Do a destroyed vehicle's PRC, or concealed enemies, keep a berserk unit berserk? (added at the reviews)",
     "No: a destroyed vehicle's PRC are eliminated with it (A11.5, p. 74) and are not Known enemies left for A15.46, after CC and after fire; CC's A15.46 counts Known enemy units only, as fire does (`knownEnemiesOnly` on the CC facts, absent as before). Decided at the reviews, built at fix Round B."),
    ("R72.21", "Is a vehicle's fire at range 0 into its own Location TPBF? (added at the reviews)",
     "Yes, outside the MPh (A7.211, p. 55), and it stays allowed; the comment on `tpbfSide` was corrected. Decided at the reviews, its comment at fix Round B."),
    ("R72.22", "Does A19.132 reach the Shortage's Unit Replacement? (added at the reviews)",
     "No: A19.132 reaches ELR failures only; the Shortage's Replacement keeps A19.13's EXC, an underscored squad becoming its two HS. Decided at the reviews, pinned by a Rules test at fix Round B."),
]
rows = "".join(f"| {r} | {q} | {W} {t} |\n" for r, q, t in rulings)
e.edit(PLAN, R71_28_END, R71_28_END + rows)

# ---- 2. The backlog: section 75, after section 74's last row.
BL = "docs/ASL Unit Backlog.md"
LAST = "| The strict test build and the gate's strict stage disagree: CA1860 in pass 70's `GamePlanner.StreetFighting.cs:87` failed the strict Play test build while the gate's strict stage passed | Fix Round B (the agent) | (tooling) | One analyzer configuration for both builds | The site was fixed in Round B (`.Count > 0`); the two builds still differ, and the strict test build is the stricter. |\n"
S75 = """
## 75. Added by pass 72

Written with the pass's documents on 2026-10-27. The first table is what the [pass 72 design](<Passes/ASL Unit Backlog Pass 72 Design.md>) moves to a later pass (section 2 and ruling R72.2); the second is what the build and the reviews found and did not fix (sections 8 to 13). Rows of earlier sections that named pass 72, a berserk charge that "ends in place", R9.10, R15.9, R15.11, or a hero's weapons are corrected where they stand (sections 13, 19, 25, 28, 41, 56.1, and 72.1): what the pass built is marked there, and what it moved on names its new pass. The [review](<Passes/Scenario A1 Backlog Pass 72 Review 2026-10-27.md>) says the same in prose.

### 75.1 What the pass left out or moved

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A berserk Dash across a road | Pass 72 (design 2; ruling R72.2) | A15.431, p. 84; A4.63, p. 51 | Pass 73 (task 73.3, Dash) | A berserk unit's charge does not Dash. |
| A heroic leader's heroic qualities in fire | Pass 72 (design 2 and 5, item 5; ruling R72.2) | A15.21, A15.24, p. 83 | Pass 73 (a heroic fact on `FireFirer`) | A heroic leader fires a MG as a leader and takes no heroic -1. |
| The circled B# of vehicles and Guns, and the Low Ammo counter at start, under Ammunition Shortage | Pass 72 (design 2; ruling R72.2) | A19.131, p. 86; D3.71, p. 201 | Pass 105 (task 105.7, Low Ammo) | No catalog vehicle or Gun prints a circled B#; a vehicle's MA and a Gun keep their numbers under the Shortage. |
| A hero firing an AFV's AAMG as a Rider | Pass 72 (design 2; ruling R72.2) | A15.23, p. 83 | Pass 105 (task 105.1) | Refused. |
| A hero firing a Gun (at most 82mm as captured, above 82mm once a Game Turn; the CA bar of C2.27) | Pass 72 (design 2; ruling R72.2) | A15.23, p. 83; C2.27 | Pass 110 (task 110.6) | Refused. |
| A hero firing a bazooka, a MOL-Projector, or a RCL | Pass 72 (design 2; ruling R72.2) | A15.23, p. 83 | Pass 110 (tasks 110.7 to 110.9; no counter exists) | No such counter exists. |
| Wire as 1 MF in a charge's route; minefields in a charge's route | Pass 72 (design 2; ruling R72.2) | A15.431, A15.45, p. 84 | Pass 120 (tasks 120.1, 120.2) | No Wire or minefield exists. |
| An FFE in a charge's route, and OBA's Scarce Ammo under Ammunition Shortage | Pass 72 (design 2; ruling R72.2) | A15.45, p. 84; A19.131, p. 86; p. 391 | Pass 150 | No OBA exists. |
| A Blaze as a charge bar | Pass 72 (design 2; ruling R72.2) | A15.45, p. 84 | Pass 185 (task 185.1) | No Blaze exists. |
| The ordnance package's Inexperienced B# read by class only | Pass 72 (design 2 and 5, item 6; ruling R72.2) | A19.3, A19.32, p. 86 | A firer's `Inexperienced` fact on the ordnance shot | A Green MMC stacked with an unbroken leader still has its ordnance SW's B# lowered; the Fire package reads the fact (5c). |

### 75.2 What the reviews and the build found and did not fix

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A bridged hex, and a pond hexside around a hex with no water at its center, are read as gaps of the model in a charge, not as A15.45's bars or as passable | Pass 72, 5a (design 8.1); fix Round A kept the bridge a gap (`ChargeEntryFacts.Bridged`, decided in `RuleBar`) | A15.45, p. 84 | The movement model reading a bridge (pass 140) and the pond hexside | The charge ends in place and the unit stays berserk (R30.5). |
| A PRC keeps R15.9's ELR 5 under an SSR ELR | Pass 72, 5c (design 10.1) | A19.132, p. 86; R15.9 | `underscoredElr` on `FirePrc` | An underscored Passenger or Rider is read at ELR 5 whatever the token gives. |
| Ordnance has no X# removal under Ammunition Shortage | Pass 72, 5d (design 11.1) | A19.131, p. 86; A.11, p. 43 | An X# removal in the ordnance package | The ordnance SW's B# is one lower; a To Hit DR at its Original B# malfunctions it and never removes it. |
| A7.353's crew exception still lets a hero who fired a SW add halved inherent FP later in the same phase, where A15.23 forfeits it for the phase | Pass 72, 5e (design 12.1) | A15.23, p. 83; A7.353, p. 56 | The phase of a SW's use kept as state | Forfeited in the SW's own attack only (the projector's `GunCrewsFired`). |
| A MG fired as ordnance at an AFV by a hero keeps no hero DRM | Pass 72, 5e (design 12.1) | A15.23, p. 83; A9.61, p. 64 | Pass 110 (A9.61's lone leader or hero) | No hero DRM on that shot. |
| A destroyed vehicle's PRC stay in the state | Fix Round B (ruling R72.20; the Heat of Battle referee's finding 8) | A11.5, p. 74 | The projector eliminating a destroyed vehicle's PRC | A15.46 does not count them as Known enemies left; they remain in the game's state. |
| A chosen Fire Lane weapon is still sent when the Fire Lane field hides under Ammunition Shortage | Fix Round B (the UI review's note 13) | A9.22, p. 63; A19.131, p. 86 | Clearing the choice when the field hides | The proposal is refused with A19.131's text. |
| The Shortage's Replacement: the latest line names only the chosen dr | The whole-pass Studio check (the table player's note 10) | A.9, p. 43; A19.131, p. 86 | The latest-line text reading every candidate's dr | The record line lists every candidate's dr; the dice line shows them all. |
| The CC and ordnance verifiers accept a record made after the pass that drops `vehicleSides` or `heroWeapons` | The Heat of Battle referee's note 9 | A15.46, A15.23, p. 84, p. 83 | A stamp on CC and ordnance records | Such a record verifies and resolves as a record made before the pass would; no regression. |
"""
e.edit(BL, LAST, LAST + S75)

# The older rows, marked where they stand.
def mark(old_line, add):
    assert old_line.endswith(" |")
    e.edit(BL, old_line + "\n", old_line[:-2] + " " + add + " |\n")

mark("| The Movement panel did not mark a berserk unit, that it must move first, or its charge target and route | A15.43, A15.431 | The planner's `Charges` gives each berserk unit's target and next steps, or why the model cannot decide its charge (ruling R30.5); the page marks it, checks it, and fills in its only next step. |",
     "**Pass 72 (design 8 and 13; rulings R72.3, R72.4, R72.17): past a cliff or an unbridged Water Obstacle the charge takes the next nearest target, a unit that can reach no Known enemy is offered Battle Hardening as the MPh ends, and the undecided line names the unit, its hex, and the bar met.**")
mark("| The ATR's fire at Guns, the Panzerschreck's 12 FP attack on Infantry, a lone hero or two SMC firing a PSK, and a SMC firing an ATR at Personnel | R9.10, R9.11 | C13.23, C13.24, C13.45, C13.48, C8.31 | HE Equivalency; SMC fire with a SW | Refused. |",
     "**Built in pass 72 (design 12; ruling R72.8): a lone hero fires a PSK, its +1 for one man negated by his -1, and a hero fires an ATR at Personnel as Small Arms. The ATR at a Gun and the PSK at Personnel by HE Equivalency were built in pass 60. Left: two SMC firing a PSK, and an ATR at Personnel by a SMC that is no hero (pass 110, task 110.7).**")
mark("| A leader firing an ATR from the fire panel, which lists no leader as a firer | R9.10 | C13.21 | SMC fire with a SW | The panel leaves leaders out; the ATR's vehicle shots take a SMC. |",
     "Pass 72 left it as it was: it is pass 110's (task 110.7, PSK and ATR usage by SMC).")
mark("| A MG's B# one lower in Inexperienced hands | R9.10 | A19.32, p. 86 | none | The MG keeps its printed B#; the ATR, mortars, and PSK take the -1. |",
     "**Built in pass 72 (design 10; ruling R72.9): with the pass's stamp a MG's B# is one lower in Inexperienced hands, read from the firer's `Inexperienced` fact; a record without the stamp replays as recorded.**")
mark("| An SSR assigning an ELR of 4 or less to underscored units | R15.9 | A19.132 | Setting up a game from a card (section 27) | The underscored exception always applies. |",
     "**Built in pass 72 (design 10; rulings R72.10, R72.13, R72.15): the token `underscored-elr:<side>[:<group>]:<n>` gives underscored MMC that ELR, and a failure beyond it Replaces them by the table; R15.9 holds where no token is given. Left: a PRC keeps R15.9 (backlog 75.2).**")
mark("| A hero firing a LATW, a light mortar, or a Gun | R15.11 | A15.23 | Those weapons' packages reading a hero | Refused. |",
     "**Corrected and built in pass 72 (design 4 and 12; ruling R72.8): the row was stale, since a hero already fired a light mortar, a PF, and an ATR at a vehicle; since the pass he takes his -1 on their To Hit DR, and a lone hero fires a PSK. A Gun, a bazooka, a MOL-Projector, and a RCL go to pass 110, the AAMG as a Rider to pass 105 (backlog 75.1).**")
mark("| An underscored MMC's fixed ELR 5 after a Massacre raises its group's past 5, the fixed ELR 5 of some squads (A25.12, A25.25, A25.31, A25.42), and Integrity Check ELR changes reaching each group | Referee, pass 18 | A20.4, A19.132, A16.2 | ELR by unit type; Battlefield Integrity | A group's ELR rises to at most 6; the underscored MMC stay at 5. |",
     "Pass 72 (ruling R72.13): with an `underscored-elr:` token the underscored MMC take its ELR; without one they stay at 5.")
mark("| A berserk charge into a Gun crew's Location | R27.2 | A15.43, C11 | Section 12 (CC with a crew) | The charge is undecided and ends in place (R30.5). |",
     "**Lifted by pass 70 (ruling R70.27): a Gun's crew no longer bars the charge; found stale by pass 72's frame (design section 5, item 1).**")
mark("| A berserk charge into a vehicle's Location that also holds enemy Infantry | Table player, pass 27 | A15.43, A11.31 | CC between Infantry beside a vehicle (R11.16) | The charge is undecided and ends in place (R30.5). |",
     "**Built in pass 72 (design 8; ruling R72.5): the charge enters such a Location; A8.312's FPF and pass 70's owed attack follow.**")
mark("| Destroying a vehicle in CC ending berserk | Table player, pass 27 | A15.45, p. 84 | A berserk end in vehicle CC | The unit stays berserk and charges on. |",
     "**Built in pass 72 (design 9; rulings R72.7, R72.20): a berserk unit that destroys an enemy vehicle in CC returns to normal when no other Known enemy is left, the vehicle's PRC not counted; the rule is A15.46, not A15.45.**")
mark("| A crew offered as a firer on the page; a berserk charge into a crew's Location | Pass 40 (design 10.2); rulings R24.8, R24.3, R30.5 | A8.311 | Pass 110 | A crew's FPF is reached only by the action; the charge is refused. |",
     "**Corrected by pass 72's frame (design section 5, item 1): the charge into a crew's Location was lifted by pass 70 (ruling R70.27). Left: the crew as a firer on the page (pass 110).**")
mark("| A berserk charge into a Location holding an enemy vehicle and enemy Infantry, now that CC between Infantry there is built | Pass 70, 3i (ruling R30.5) | A15.431, A15.432, p. 84 | Pass 72 (task 72.1) | The charge ends in place, as before; a berserk unit already there owes its attack (fix Round C). |",
     "**Built in pass 72 (design 8; ruling R72.5): the charge enters the Location; A8.312's FPF and the owed attack follow.**")

# ---- 3. The redesign plan: pass 72's status line and the moves under later entries.
RP = "docs/ASL Card Play and Map Studio Redesign Plan.md"
P72_HEAD = "**Pass 72: Infantry V: Heat of Battle and Unit Substitution** (10 rows of the inventory). Counters: none. Needs first: 40; 71, since a berserk unit (A15.431), a wounded SMC (A17.2), and an Inexperienced unit (A19.31) all change the same movement allowance, and the two passes run one after the other."
STATUS = (" **Status, 2026-10-27: built to the merge gate on `feature/asl-backlog-pass-72`, cut from b0a36132; the gate not run. The user's word of 2026-10-27 (\"Run Pass 72 without my intervention up to the merge gate.\") ends at the gate, and the gate, the push, the pull request, and the merge are each the user's to start.**"
          " The two tasks are built, in five increments (5a to 5e) in five commits and two rounds of the reviews' fixes (76cac041, 1fa0a214); no increment was left out (ruling R72.1). Of the 10 inventory rows, 5 are built (3 of them with a deviation) and the remainders of 5 went to the pass that takes what is left (pass 73 1, pass 105 1, pass 110 1, pass 120 1, pass 180 1); the item moved in is built."
          " The calibrated forecast was about 2:50 of building (about 5:20 to the gate) against the plan's 1:40 (2:55 in all); the build took 3:15 by the clock (19:10 to 22:25). Rulings R72.1 to R72.22 are open to a veto."
          " What the pass moved went to pass 73 (a berserk Dash, with 73.3; a heroic leader's heroic qualities in fire), pass 105 (the circled B# and Low Ammo under Ammunition Shortage, with 105.7; a hero's AAMG as a Rider, with 105.1), pass 110 (a hero firing a Gun, with 110.6, and a bazooka, a MOL-Projector, or a RCL, with 110.7 to 110.9), pass 120 (Wire and minefields in a charge's route), pass 150 (an FFE in a charge's route; OBA's Scarce Ammo), pass 185 (a Blaze as a charge bar), and the backlog (the ordnance package's Inexperienced B# read by class only), each with its reason in the [design](<Passes/ASL Unit Backlog Pass 72 Design.md>), sections 2, 8.1 to 12.1, and 13, the [review](<Passes/Scenario A1 Backlog Pass 72 Review 2026-10-27.md>), and the backlog's section 75.")
e.edit(RP, P72_HEAD + "\n", P72_HEAD + STATUS + "\n")

P73_FRAME_END = "A9.8's row stays with pass 110 for its other parts, and A8.31's row with no pass: 73.6 and 73.5 take only the backlog's items from them.\n"
e.edit(RP, P73_FRAME_END, P73_FRAME_END + "\nMoved here by pass 72 (2026-10-27), with no task renumbered: a berserk Dash across a road (A15.431, with 73.3); a heroic leader's heroic qualities in fire, which `FireFirer` does not carry (A15.21; ruling R72.2).\n")


def after_line(prefix, text):
    raw = e._text(RP)[1]
    lines = [line for line in raw.split("\n") if line.startswith(prefix)]
    assert len(lines) == 1, (prefix, len(lines))
    e.edit(RP, lines[0] + "\n", lines[0] + "\n\n" + text + "\n")


after_line("Moved here by pass 65 (2026-10-24), with no task renumbered: D5.311's unarmored Target Facing",
           "Moved here by pass 72 (2026-10-27), with no task renumbered: the circled B# of vehicles and Guns and the Low Ammo counter at start under Ammunition Shortage (A19.131, D3.71, with 105.7); a hero firing an AFV's AAMG as a Rider (A15.23, with 105.1; ruling R72.2).")
after_line("Moved here by pass 70 (2026-10-25), with no task renumbered: CC against a wreck, with the rest of D10.1",
           "Moved here by pass 72 (2026-10-27), with no task renumbered: a hero firing a Gun, at most 82mm as captured and above 82mm once a Game Turn, with the CA bar of C2.27 (A15.23, with 110.6); a hero firing a bazooka, a MOL-Projector, or a RCL (A15.23, with 110.7 to 110.9; ruling R72.2).")
after_line("Moved here by pass 65 (2026-10-24), with no task renumbered: the Specific and General Collateral Attacks, and the Immobilization TC, after a mine's attack",
           "Moved here by pass 72 (2026-10-27), with no task renumbered: Wire as 1 MF in a berserk charge's route, and minefields in it (A15.431, A15.45, with 120.1 and 120.2; ruling R72.2).")
after_line("Moved here by pass 65 (2026-10-24), with no task renumbered: the General Collateral Attack by OBA on a vehicle's PRC",
           "Moved here by pass 72 (2026-10-27), with no task renumbered: an FFE in a berserk charge's route (A15.45, with 150.3); OBA's Scarce Ammo under Ammunition Shortage (A19.131, p. 391; ruling R72.2).")
after_line("| **Pass 185 total** (build 3:40) | | **4:55** |",
           "Moved here by pass 72 (2026-10-27), with no task renumbered: a Blaze as a bar to a berserk charge (A15.45, with 185.1; ruling R72.2).")

# ---- 4. The coverage: a note on the three sections whose counts changed.
CV = "docs/ASL Rule Coverage.md"
e.edit(CV, "Battle Hardening is always taken there (backlog 72.1). | 49 |",
       "Battle Hardening is always taken there (backlog 72.1). **Pass 72 (2026-10-27):** the next nearest past a cliff or an unbridged Water Obstacle, and A15.44's Battle Hardening when none can be reached (15.45; R72.3, R72.4, R72.17); a charge into a vehicle-and-Infantry Location (15.43; R72.5); the return to normal after AFPh TPBF, a FT, and a vehicle destroyed in CC (15.46; R72.7, R72.20); a hero's -1 with a non-MG SW, a lone hero's PSK, the ATR as Small Arms (15.23; R72.8). Left: a gap of the model keeps R30.5 (backlog 75.2); a heroic leader in fire (15.21) and the Dash (15.431) to pass 73; the hero's Guns and other weapons to passes 105 and 110; Wire, minefields, an FFE, a Blaze to passes 120, 150, 185. | 49 |")
e.edit(CV, "PRC are never Disrupted (19.12). | 45, 49 |",
       "PRC are never Disrupted (19.12). **Pass 72 (2026-10-27):** Ammunition Shortage's Unit Replacement, lowered B#, X#, and Depletion Numbers, and its Fire Lane bar (19.131; R72.10 to R72.12, R72.16, R72.18, R72.22); an SSR ELR for underscored units (19.132; R72.13, R72.15, R72.19); a MG's B# for Inexperienced users under the pass's stamp (19.32; R72.9, lifting R9.10). Left: the circled B# and Low Ammo (pass 105), a PRC's SSR ELR and the ordnance package's class read (backlog 75). | 45, 49 |")
e.edit(CV, "and an ATR at a Gun (13.23). | 45, 50, 54; ruling |",
       "and an ATR at a Gun (13.23). **Pass 72 (2026-10-27):** a lone hero fires a PSK (13.45, 13.48; R72.8). | 45, 50, 54; ruling |")

e.apply()
