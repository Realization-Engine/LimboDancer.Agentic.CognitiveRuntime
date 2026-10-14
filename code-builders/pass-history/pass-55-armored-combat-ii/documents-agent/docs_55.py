"""Pass 55 documents: the design's sections 14 to 16 and its Status line, the review, the rulings in the Passes Plan, the backlog's section 59 and
the older rows the pass answers, the redesign plan's entry, and the coverage document's text. The rule inventory and the coverage counts are
docs_inventory55.py's and pass 35's docs_coverage_counts.py. Every anchor is counted before any file is written.

Usage: python docs_55.py [--check]
"""
import io
import os
import sys

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
T = "E:/Archive/GitHub/dlandi/pass32-tools/pass55/docs/"


def read(path):
    text = io.open(path, encoding="utf-8", newline="").read()
    assert "\r" not in text, path
    return text


files = {}


def text(name):
    if name not in files:
        files[name] = read(D + name)
    return files[name]


def replace(name, old, new, count=1):
    assert text(name).count(old) == count, (name, text(name).count(old), old[:90])
    assert old != new
    files[name] = text(name).replace(old, new)


def append(name, more):
    files[name] = text(name).rstrip("\n") + "\n\n" + more.strip("\n") + "\n"


fourteen = read(T + "design_14.md")


def section(start, end):
    a = fourteen.index(start)
    b = fourteen.index(end) if end else len(fourteen)
    return fourteen[a + len(start):b].strip("\n")


WORD = "I want you to run without my intervention up to the Gate"

# --- The design ------------------------------------------------------------------------------------------------------
DESIGN = "Passes/ASL Unit Backlog Pass 55 Design.md"
assert "## 14. " not in text(DESIGN)
replace(DESIGN,
        "**Status:** The frame, written on 2026-10-13 under the user's word of 2026-10-12 for this pass (\"" + WORD + "\"). The frame's questions were put to three read-only agents, as that word asks; their answers were judged against the pages and the code, and each decision below is a ruling decided under that word and open to a veto. Nothing is built yet.",
        "**Status:** Built to the merge gate on 2026-10-13 under the user's word of 2026-10-12 for this pass (\"" + WORD + "\"): the frame, seven tasks in seven commits (the plan's 55.1 to 55.6, and 55.7, Gun Duels, carried in from pass 50 and built as a slice), then the five reviews and their fixes. Sections 14 and 15 say how it ended; section 14.1 lists what departs from the plan's entry and the frame. The frame's questions were put to three read-only agents, as that word asks; their answers were judged against the pages and the code, and each decision below is a ruling decided under that word and open to a veto. **The gate is not run:** it is the user's to start (section 16).")
replace(DESIGN,
        "Each increment then gets a section of its own when it is built, and the pass ends with the reviews, the Studio check, and \"as built\".",
        "Then: 7, the notation and the counters (55.1 in part, 55.6); 8, fire at another level (55.4); 9, special ammunition (55.1, the rest); 10, Canister (55.2); 11, Armor Leaders and Inexperienced Crews (55.5); 12, Motion attempts and the DEFENDER's Motion firer (55.3); 13, Gun Duels (55.7); 14, the five reviews and the Studio check; 15, as built; 16, the gate.")
append(DESIGN, fourteen)

# --- The review ------------------------------------------------------------------------------------------------------
departures = section("### 14.1 What departs from the plan's entry and the frame, first", "### 14.2 Fixed").split("\n\n", 1)[1]
fixed_all = section("### 14.2 Fixed", "### 14.3 Not fixed")
fixed_table = fixed_all.split("\n\n", 1)[0]
not_fixed = section("### 14.3 Not fixed: a row each in the backlog's section 59", "### 14.4 The Studio check")
fixed_rows = sum(1 for line in fixed_table.split("\n") if line.startswith("| ")) - 1
open_rows = sum(1 for line in not_fixed.split("\n") if line.startswith("| ")) - 1
assert fixed_rows == 20 and open_rows == 14, (fixed_rows, open_rows)

REVIEW = "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"
assert not os.path.exists(D + REVIEW)
review = """# Scenario A1 Backlog Pass 55: Armored combat II

The review of pass 55, written on 2026-10-13 after the five reviews and their fixes. The Studio check of the whole pass is the session's to add, and the gate is not run. The design is [ASL Unit Backlog Pass 55 Design](<ASL Unit Backlog Pass 55 Design.md>); its section 15 says what was built.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-55`, cut from main at 846eb5a3: the frame, seven tasks in seven commits, then the five reviews, their fixes, and these documents. Everything was done under the user's word of 2026-10-12 for this pass, \"""" + WORD + """\", and is open to a veto. **The gate is not run, and nothing is merged:** the gate, the merge, and the push are the user's to start.

## What departs from the plan's entry and the frame

""" + departures + """

## What was built, by task

- **55.1, special ammunition** (design sections 7 and 9). A counter's special ammunition is read in one notation, with a start month where the book prints one. APDS (C4.3, p. 171; C7.32, p. 176; C8.11, p. 177) is an ammunition of its own for a Gun that lists it at the game's date. C8.9's three outcomes (p. 179) hold for every ammunition with a Depletion Number, on the Infantry and Vehicle Target Types: below the number the round is used, at it it is the last, above it the Gun had none and has not fired. A Gun whose HE has a Depletion Number fires HE only when it is named (C2.21, p. 167; C8.8, p. 179). An Elite force has one more on every number, by a special rule of the game (C8.2, p. 178). The Supply Chart of C8.12 is a test's data.
- **55.2, Canister** (design section 10; C8.4 to C8.42, p. 178). An ordnance shot at one of the five aims of the Gun's CA, with no To Hit DR. One IFT DR attacks every Location of the cluster and of the adjacent hex that is at the firer's elevation and in its LOS, each with its own TEM and Hindrance, at 12, 20, or 24 FP by caliber and each halving of the rule. The same DR is the Depletion, malfunction, and ROF check. It is refused in the MPh.
- **55.3, Motion attempts** (design section 12; D2.401, p. 198; C5.35, p. 172). A DEFENDER's Mobile vehicle attempts Motion once in an enemy MPh, by a dr against the MF or MP that a mover which began out of its LOS has spent in it. On a pass it takes the Motion counter and a free VCA and TCA, unless a Bog Check or its Mechanical Reliability DR undoes it. A Hidden vehicle attempts none, and a concealed one loses its "?" on a pass. A vehicle in Motion of the side that is not phasing fires its MA with Case C and the lower dr doubled.
- **55.4, fire at another level** (design section 8; C2.6, p. 168; B10.31, p. 125). A Gun, a tank's MA, a mortar firing down, and a vehicle's MGs fire at a Location of another level when the range is at least the elevation difference. A target above the firer takes Height Advantage, as Case Q +1 on the To Hit DR of the Infantry and Vehicle Target Types, and is then free of Case J4. Another level of the firer's own hex stays refused.
- **55.5, Armor Leaders and Inexperienced Crews** (design section 11; D3.4 to D3.45, p. 200; D7.16, p. 207). An armed AFV is given an Armor Leader of 8-1, 9-1, 9-2, or 10-2 at setup. His modifier is on the MA's To Hit DR, the IFT DR of a MA that is a MG, the OVR DR, the vehicle's own Close Combat attack, and the colored dr of Bog Removal; the crew checks at his Morale Level. The enemy learns of him at the first record that uses him. An Inexperienced Crew adds 1 on the same rolls and has a morale of 6.
- **55.6, the counters** (design section 7). The OQF 6-Pounder and the M3A1 37mm AT Gun, with a British and an American crew, every value printed in the book. The catalog is `asl-scenario-a1@1.14.0`. British To Hit numbers are black, American ones red before 1944 and black from 1944 (C3.3, p. 169).
- **55.7, Gun Duels, the slice** (design section 13; C2.2401, p. 167). A Defensive First Fire ordnance shot declared at a moving vehicle whose Gun Duel DRM is lower waits for the ATTACKER's answer. On `decline` the declared shot resolves. On `duel` the vehicle's MA fires first as Bounding First Fire, and the declared shot follows unless the first eliminated, broke, stunned, or shocked its firer.

## The reviews

Five read-only agents in one message: the referee in two halves, the table player, the Rules boundary, the UI and Blazor review. Their briefs are in `Prompts/Pass 55 Review Briefs.md`; the fix lists and what was done are in the tools folder (`pass55/fixes_*.md`).

**What they confirmed.** The referee's first half read every catalog value of the two Guns and the two crews against renders of pp. 392, 426, and 695, and found them right; so the To Hit colors, the APDS Case D row of p. 190, C8.9's "not to have fired" on each Target Type and for Canister, and the Canister aim table. The second half found that the classification of the To Hit DRM agrees with the chart of p. 189, that the duel's paths neither lose nor double the declared shot, and that the new facts are absent-safe. The table player checked the arithmetic of the recorded games against the charts.

**One BLOCKER, fixed.** A hidden vehicle's Motion attempt was written where the enemy reads it. A Hidden vehicle may not attempt Motion (A12.32, p. 80; ruling R55.32), and a record of such an attempt is refused at replay.

**Fixed:** """ + str(fixed_rows) + """ rows in the design's section 14.2. The main ones after the BLOCKER:

- A concealed vehicle kept its "?" after a passed Motion attempt. It loses it, with or without a change of CA (A12.141, p. 78; ruling R55.33).
- A Gun Duel declaration, and a pending choice's resume, could tell the enemy of an Armor Leader not yet revealed. He is left out of both Gun Duel DRM at the declaration (ruling R55.29, extended), and a resume no longer holds a crew's Morale Level.
- A CE crew fired on from below by a vehicle's MG got Height Advantage on top of its CE DRM (D5.31, p. 203). Dropped for a vehicle's fire and for Canister.
- The projector did not hold a recorded Motion attempt to the planner's bars; whether a duel's return shot is void was decided in Play; three filters for A7.55 stood outside Rules. Each is one Rules member now.
- The adjacent hex of a Canister shot was attacked when no Location of the cluster was (C8.41, p. 178). Sniper checks on a Canister shot depended on the path: none now (ruling R55.30).
- A shot that had no round was recorded as a "miss" that "may fire again (C2.24)". It reads "no shot: it had no APDS" and "it has not fired and may fire other ammunition (C8.9)", and the Depletion Number is carried in the resolution.
- The page: the placement form's and the Motion attempt's choices outlived a hand-over; the ammunition list and the shot followed different tests; a Canister aim was sent without checking it is among the aims shown.

**One finding was open when this was written:** the answer to a duel's choice may show the DEFENDER's unrevealed Armor Leader in the declared shot's summary. It was sent back to the fixing agent. (Its outcome is written by the session.)

**Not fixed:** """ + str(open_rows) + """ rows, each with a row in the backlog's section 59.2:

""" + not_fixed + """

The referee's first half, finding 6 (a Canister malfunction on a DR above the Depletion Number takes the Acquisition), is right as built by C6.5 (p. 174) and has no row.

## The games

In the session's Studio on port 6671, board 3, scripted dice; each is in the design's table for its task. "Recorded" is the design's word for a game whose file was kept.

- **55.1 and 55.6** (design 7.3, 9.3): `p55-6pdr`, `p55-37mm-1943`, `p55-37mm-1944`; `p55-apds-usedc`, `p55-apds-nonec`, `p55-apds-malfc`, `p55-apds-elitec`, `p55-he-numberc`; the controls `p55-apds-mayc` (May 1944: no APDS, nothing proposed) and `p55-he-1942c` (July 1942: no HE, refused). The same ids without a letter and with `b` are SUPERSEDED or spoiled copies.
- **55.4** (design 8.3): `p55-ha-inf`, `p55-ha-veh`, `p55-cmg-up`; the control `p55-ha-down`; `p55-own-hex`, a refusal with nothing recorded.
- **55.2** (design 10.3): `p55-can-used`, `p55-can-none`, `p55-can-malf`, `p55-can-turn`; the control `p55-can-1942`.
- **55.5** (design 11.3): `p55-al-fire`, `p55-al-shock`, `p55-al-shock2`, `p55-al-inexp`, `p55-al-bog`; `p55-al-ctl` was written and not played.
- **55.3** (design 12.3): `p55-mot-pass`, `p55-mot-fail`, `p55-mot-immob`; the control `p55-mot-seen`.
- **55.7** (design 13.3): `p55-duel-decline`, `p55-duel-miss`, `p55-duel-void`; the control `p55-duel-none`.

The table player found three games' labels promising what their files do not hold; they were relabelled, and the spoiled copies marked SUPERSEDED. All 48 `p55-*` games of the live folder replay to their end with no diagnostic on the build with the fixes. No game the session recorded is made stale by a fix (design 14.2).

## The tests

After the reviews' fixes, the fixing agent's runs: Rules 220 in nine classes; Play 184 in eleven; Units 416 whole; MapStudio 75; none failed. The four test projects build with warnings as errors, 0 warnings. The pass's own classes are `ScenarioA1Pass55RulesTests` (60 test methods as this is written) and `BacklogPass55Tests` (69). The Rules and Play projects were not run whole.

Lacking: a Play test and a game for a vehicle's MG at another level of its own hex; a board with a cliff; a Studio game of an OVR or of the vehicle's Close Combat attack with an Armor Leader; page tests for the UI review's fixes 1, 2, 3, 5, and 6.

## The replay digest

Over the proof store, 473 games and 93,266 lines, after each of the seven increments. The base was built from main's sources at 846eb5a3. The catalog's identity, which is hashed into every state and went to 1.14.0, was left out on both sides, and the pass's new properties were left out. No line differed in any of the seven runs. The store holds none of the pass's own games, so this shows that nothing recorded before the pass replays differently, and nothing more. The run after the reviews' fixes: (written by the session).

## What is open to a veto

- **The rulings R55.1 to R55.33,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5, and one line each in the design's section 15. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-12. Those most likely to be read again: R55.14 (which Case C a DEFENDER's Motion firer takes, a reading with no sentence of its own), R55.15 (no Armor Leader counter), R55.24 (no Height Advantage behind a wall whose own TEM is not given), R55.29 (an unrevealed Armor Leader left out of a Gun Duel DRM), R55.31, R55.32, and R55.33. R55.6 supersedes R7.6.
- **The departures** listed above.
- **Pass 50's, still unanswered:** task 50.4, Gun Duels, is now built as a slice; task 50.5, MG fire against armor, is not built and waits on pass 60; rulings R50.1 to R50.14.

## What the gate will meet

- **The gate is one script now, and this is its first full run** (`pass32-tools/gate/gate.sh`). Its skip list from the diff, its `trial` stage, and the refusal of `proofs` without a passed trial were tested on pass 50's data; its `baseline`, `build`, `suite`, and `docker` stages and the long runs of `proofs` have had a syntax check only (`Prompts/Pass 55 Prompt I.md`). A stage may fail for a reason of the script's own.
- **`BOTH_SKIP`.** The catalog's identity differs in every state between main and the branch. The script has a list of properties left out on the baseline's side as well; it is in the script and has had a syntax check only. Without it every line differs and the run is void, as the pass's first trial showed.
- **`gate/pass55.conf` was not there when this was written:** the pass's recorded games, its extra sweep and trial games, and the JSON keys it added.
- **The sweep proves little of what the pass built.** It asks one request of each kind for each unit and names no new argument, so a Canister aim, a named ammunition, a Motion attempt, and a duel's answer are reached only in the pass's own games, where main has no answer to compare with. For those the proof is the pass's tests and Studio games.
- **The whole suite.** Rules and Play run whole for the first time on this branch; so do the Node viewport test, the chart supplement, the text list, and Docker.
- **The store** holds none of the pass's games. Those labelled SUPERSEDED are not to be taken.

## The Studio check

(written by the session after the check)
"""
files[REVIEW] = review

# --- The Passes Plan: the rulings ------------------------------------------------------------------------------------
PLAN5 = "ASL Unit Backlog Passes Plan.md"
W = "Pass 55; decided under the user's word of 2026-10-12 (\"" + WORD + "\"), open to the user's veto. "
RULINGS = [
    ("R55.1", "Is a pass of about twice the plan's size split?", "No: it stays one pass and is built in the order of the design's section 2, Gun Duels last, so that whatever a session's end leaves out is the task that was carried in. The plan gives six tasks, 20 rows, and 3:40 of building; the inventory names 35 rows and the frame's estimate is 9:50."),
    ("R55.2", "Where do the 35 rows of the inventory go?", "By the design's section 3. What is moved on goes to the pass whose ground it is: A5.132 to pass 60, the rest of a vehicle's handling of its weapons and arcs to pass 105, the duel's remainder to pass 110 beside OVR Prevention."),
    ("R55.3", "Which counters does the pass add?", "Four, every value printed in the book and none manufactured: the OQF 6-Pounder (p. 426, note 6) for APDS, a start month, and an HE Depletion Number; the M3A1 37mm AT Gun (p. 392, Note C) for Canister; a British 2-2-8 and an American 2-2-7 crew (p. 695). The catalog becomes 1.14.0; the tests, cards, and fixtures that name 1.13.0 are left as they are."),
    ("R55.4", "How is a counter's special ammunition written?", "`<type><number>[^<year digit>[m<month>][+][E]]`, with the types A, D, H, HE, AP, and C. The month is a number, since the book's month letters are ambiguous. `+` is \"from that year on\". `E`, the ETO only, is kept and not read: every game is taken as the ETO, and the PTO's three more is never added."),
    ("R55.5", "Does the game read the Supply Chart of C8.12?", "No. C8.11 (p. 177) has the availability on the counter and C8.12 lets a counter's own number differ. The chart is a test's data: each catalog counter with A or D agrees with it or the test names the difference."),
    ("R55.6", "How is a force Elite for C8.2?", "By a special rule of the game, `elite-ammunition:<side>`, which a card's rule can carry; the shot records it, and a record without it is not Elite. No card is changed. It supersedes R7.6."),
    ("R55.7", "Which ammunition follows C8.9?", "Every ammunition with a Depletion Number, on all three Target Types (C8.9, p. 179); on the Area Target Type nothing is built, since no mortar lists a number. An Original DR above the number is \"not to have fired yet\": the ROF kept, no fire counter, the Acquisition as it was. A Gun whose HE has a Depletion Number never fires HE undeclared (C2.21, p. 167)."),
    ("R55.8", "Where may Canister be aimed?", "At the five vertices of a CA whose three hexes all lie in the CA at range 2 or 3: the five circles of the figure on p. 178."),
    ("R55.9", "How is a Canister shot resolved?", "As the ordnance shot with the ammunition `canister` and an aim of 1 to 5 (C8.4, p. 178). No To Hit DR: one IFT DR attacks every Location of the cluster and of the adjacent hex that is at the firer's elevation and in its LOS, each as a fire record on that DR with its own TEM and Hindrance. The same DR is the Depletion, the malfunction, and the ROF check. The FP is 12, 20, or 24 by caliber, halved for each cause the rule names. No leader, no FG, no Cowering; the Acquisition is lost (C6.5); friendly units are not attacked (A7.4)."),
    ("R55.10", "Do FFMO and FFNAM apply to Canister, and is an AFPh shot halved?", "FFMO and FFNAM apply to Canister in Defensive First Fire as to any IFT attack that is not an ordnance hit; this half is not built, since Canister is refused in the MPh (design 10.1). An AFPh shot by a Gun that did not move is not halved: C8.4 halves it only for Infantry that moved to man the Gun."),
    ("R55.11", "What is a Motion attempt, and how is its number counted?", "A new action, `asl.game.motion-attempt`, for the side that is not phasing while the window on an enemy ground unit's expenditure is open (D2.401, p. 198). The dr must be at most the MF or MP the mover has spent in the vehicle's LOS in this MPh, counted as a sum over its steps and not as an unbroken LOS."),
    ("R55.12", "When had the mover \"not been in the vehicle's LOS during that Player Turn\"?", "The LOS to the Location the unit began its move in decides it. The game makes that check first and refuses, which costs no attempt, where the rule has the attempt fail after a later LOS Check."),
    ("R55.13", "Which vehicle may attempt Motion, and what follows a pass?", "A Mobile vehicle that is not stunned, shocked, or Abandoned, carries no First, Final, or Intensive Fire counter, and has made no attempt in this MPh; one already in Motion may, for the free change of CA. A VCA change in woods, a building, or rubble takes its Bog Check, and a red-MP AFV makes its Mechanical Reliability DR (D2.51); either may leave it with neither Motion nor the change. The event is `motion-attempted`."),
    ("R55.14", "Which Case C does a DEFENDER's vehicle in Motion fire its MA with?", "Case C alone, with the lower dr doubled (C5.35, p. 172). C5.31 and C5.32 define C1 and C2 only for a vehicle First Firing in its own MPh. No sentence says so outright; it is a reading."),
    ("R55.15", "How is an Armor Leader held?", "As a morale and a leadership on his vehicle, of the grades 8-1, 9-1, 9-2, and 10-2, with no catalog definition: D3.41 (p. 200) keeps the counter out of play. The enemy's view does not show him until the first record that carries his modifier or his morale."),
    ("R55.16", "Where is his modifier applied?", "Always, where D3.44 lets it be: the MA's To Hit DR, the IFT DR of a MA that is a MG, the OVR DR (D7.16), the vehicle's own CC attack, and the colored dr of Bog Removal (D8.3). The leave to withhold it (A10.72) is not built."),
    ("R55.17", "What morale does his crew check at, and when is he lost?", "At his (D3.42), in the Shock and Immobilization checks and the CE crew's MC, without his modifier. He is lost with a Recall (D5.341) or a wreck, and when the crew takes counter form (D3.43); his return when that crew re-crews is left out."),
    ("R55.18", "What is an Inexperienced Crew?", "A condition of the vehicle, `asl:inexperienced-crew`, given at setup (D3.45, p. 200): plus 1 on the rolls an Armor Leader modifies, a crew morale of 6, never lost."),
    ("R55.19", "When and how is a Gun Duel offered?", "When the side that is not phasing proposes a Defensive First Fire ordnance shot at the moving vehicle, the firer is not concealed, the vehicle is not conducting an OVR, its MA could Bounding First Fire at the firer without a change of CA, and its Gun Duel DRM is lower (C2.2401, p. 167). The commit is the declaration: a pending choice of the kind `gun-duel` for the ATTACKER. On `decline` the declared shot resolves; on `duel` one commit holds the vehicle's shot and then the declared shot, which is void when its firer can no longer fire. The Gun Duel DRM is the firer-based To Hit DRM and the Acquisition, without Case C4's doubled dr."),
    ("R55.20", "What of the Gun Duel is left out?", "The equal case, in which the Final DRs decide the order; fire that is not ordnance on either side; the second duel with the ROF as a DRM; a duel declared by the DEFENDER (C5.33); a Gyrostabilizer. Each to pass 110. When the totals are equal no duel is offered, though the rule gives one."),
    ("R55.21", "In what order is the pass built?", "The counters first, with the notation they are written in; then fire at another level, the ammunition, Canister, Armor Leaders, Motion attempts, and the duel last, which sums the firer's DRM of all the others (the design's section 2)."),
    ("R55.22", "Whose To Hit numbers take the color of the chart of p. 695?", "A Gun's and a vehicle's MA's: British black, American red before 1944 and black from 1944 (C3.3, p. 169). A light mortar and a LATW keep the color they had before the pass, red for every nationality but the German, so that no recorded shot changes."),
    ("R55.23", "Is a vehicle's MA AAMG held to C2.6's limit?", "Yes, as its BMG and CMG are, though the rule names \"non-AA vehicular MG\" (C2.6, p. 168). What an AA weapon may do is pass 105's."),
    ("R55.24", "Does a target behind a wall or hedge claim Height Advantage on a To Hit DR?", "No: it has another positive TEM to claim (B10.31, p. 125). That TEM itself is not given on an ordnance To Hit DR, which is older than the pass and is left as found."),
    ("R55.25", "Does a shot that had no round leave an Intensive Fire counter?", "No: it leaves no fire counter of any kind (C8.9: \"not to have fired yet for any purpose\"). C8.92 only says the number is not changed by Intensive Fire."),
    ("R55.26", "What is \"barring obstacles in the first hex of the cluster\" (C8.4)?", "The LOS to each Location: a Location of the aim is attacked when the firer has a LOS to it, and no further test is made of the cluster's first hex."),
    ("R55.27", "When does a record carry the morale of a target's crew?", "Only when that crew took a check in it, so that a miss tells the enemy nothing of an Armor Leader."),
    ("R55.28", "Is a Motion attempt that no dr could pass an attempt?", "No: against a mover that has spent less than one MF or MP in the vehicle's LOS it is refused and costs nothing."),
    ("R55.29", "Is an Armor Leader who is not yet revealed counted in a Gun Duel DRM?", "No: he is left out when a duel is offered and in what is shown of it, for the moving vehicle and, since the reviews, for a declaring vehicle too. The shot each then fires uses him and reveals him. This can deny the ATTACKER a duel his modifier would earn."),
    ("R55.30", "Does a Canister shot call a Sniper?", "No: it follows ordnance, which calls none. Found by the reviews: the plain shot called none and a shot resumed after an owner's choice did."),
    ("R55.31", "Is a number that starts in a month of the game's year available when the game gives no month?", "No (C8.11, p. 177). The page's note and the refusal say why. Undated HEAT behaved so before the pass."),
    ("R55.32", "May a Hidden vehicle attempt Motion?", "No (A12.32, p. 80: \"Hidden units may not move (even within their hex or to change a CA)\"). A record of such an attempt is refused. Found by the reviews as a BLOCKER: the attempt was written where the enemy reads it."),
    ("R55.33", "Does gaining Motion by a Motion attempt cost a concealed vehicle its \"?\"?", "Yes, whether or not its CA changes (A12.141, p. 78, \"any other activity\"), as a vehicle's own step does. A failed attempt costs none (D2.401)."),
]
assert [n for n, _, _ in RULINGS] == ["R55.%d" % i for i in range(1, 34)]
anchor = [line for line in text(PLAN5).split("\n") if line.startswith("| R50.14 |")]
assert len(anchor) == 1 and "| R55." not in text(PLAN5)
for n, q, a in RULINGS:
    assert "|" not in q + a and "\u2014" not in q + a, n
replace(PLAN5, anchor[0] + "\n", anchor[0] + "\n" + "".join(f"| {n} | {q} | {W}{a} |\n" for n, q, a in RULINGS))
replace(PLAN5,
        "The Elite forces' higher Depletion Numbers (C8.2) are not built. A game with no scenario month has no year, so APCR and HEAT are refused. |",
        "The Elite forces' higher Depletion Numbers (C8.2) are not built. A game with no scenario month has no year, so APCR and HEAT are refused. **Pass 55's ruling R55.6 supersedes the sentence on Elite forces: they are built as a special rule of the game.** |")

# --- The backlog -----------------------------------------------------------------------------------------------------
BACK = "ASL Unit Backlog.md"
assert "## 59. " not in text(BACK)
BUILT = " **Built in pass 55 ({}): {}**"


def close(old_tail, design, words, count=1):
    """Append the pass's closing sentence to the last cell of the row that ends with old_tail."""
    assert old_tail.endswith(" |")
    replace(BACK, old_tail + "\n", old_tail[:-2] + BUILT.format(design, words) + " |\n", count)


# Older rows that named pass 55, or that the pass answers.
close("| Elite Depletion Numbers | R7.6 | C8.2, p. 179 | Force quality in the scenario setup | Depletion Numbers are the printed ones, also for Guards. |",
      "design 9.2; ruling R55.6", "a game's special rule `elite-ammunition:<side>` adds one. No card carries it; the DYO Majority Squad Type is in section 59.1.")
close("| Fire at another level for ordnance | R8.7 | C2.6 | Levels in fire (pass 10.2) | The C2.6 limit refuses; otherwise undecided. |",
      "design 8.2", "a Gun, a tank's MA, and a mortar fire at another level when the range is at least the elevation difference. Another level of the firer's own hex stays refused.")
close("| A vehicle's MG, ordnance, and mortars firing at another level, and the C13.8 bar on LATW at targets two levels higher | R10.4 | C2.6, C13.8 | Levels in those packages | Refused as before (backlog sections 18 and 19). |",
      "design 8.2", "all three fire at another level under C2.6's limit. The C13.8 bar is not built: section 59.2.")
close("| Motion Attempts, and a vehicle firing in Motion by its own choice | R11.18 | D2.401, D2.42 | Motion Attempts in the DFPh | Not offered; Motion comes only from ending a move without stopping. |",
      "design 12", "a DEFENDER's vehicle attempts Motion in the enemy MPh and then fires as a Motion firer.")
close("| Armor Leaders, in OVR and CC | R11.11, R11.14 | D7.16, A11.5, D3.44 | Armor Leader counters | Not in the catalog; no OVR leadership. |",
      "design 11", "an Armor Leader's modifier is on his vehicle's OVR DR and its own CC attack. He has no counter in the catalog (ruling R55.15).")
replace(BACK,
        "| Pass 105 | A bogged vehicle loads and unloads; a Recalled AFV carrying Passengers may Stop to unload them. |",
        "| Pass 105 | A bogged vehicle loads and unloads; a Recalled AFV carrying Passengers may Stop to unload them. **Armor Leader loss on a Recall: built in pass 55 (design 11.2).** |")
replace(BACK,
        "| One Rules member read by MoveBar and the projector (pass 55; pass 50 did not take it) | The projector keeps its own list with the same exemption. |",
        "| One Rules member read by MoveBar and the projector (no pass is named for it now: passes 50 and 55 did not take it) | The projector keeps its own list with the same exemption. **Pass 55 did not take it: the row stays.** |")
replace(BACK,
        "| A5.132, p. 52 | Pass 55: the To Hit's target choice (pass 50 left it; 58.1) | No such hit. |",
        "| A5.132, p. 52 | Pass 60, task 60.6: the To Hit's target choice (passes 50 and 55 left it; 58.1, the pass 55 design's section 3) | No such hit. |")
close("| Gun Duels | Pass 50 (design 6, F5), against the plan's entry | C2.2401, pp. 167 to 168 | Pass 55: a declared attack that stands open between the two sides, an answer by the other side, and two resolutions in an order the Final DRs may decide | The DEFENDER's shot resolves alone; the vehicle answers after the pass. |",
      "design 13, as a slice", "ordnance on both sides, the vehicle's Gun Duel DRM lower, the first duel. The equal case and the rest go to pass 110: section 59.1.")
replace(BACK,
        "| D1.81, D3.51, D3.52, D2.11, C5.11 | Pass 55: two remembered penalties a mount, and a TCA that turns with the hull (the row of 55.2) | Refused: a BMG fires only inside the VCA. |",
        "| D1.81, D3.51, D3.52, D2.11, C5.11 | Pass 105 (moved on by pass 55, design 3): two remembered penalties a mount, and a TCA that turns with the hull (the row of 55.2) | Refused: a BMG fires only inside the VCA. |")
replace(BACK,
        "| C3.22, D3.12, D2.11 | Pass 55: the Gun's action carried to a tank, with no more fire by any of its weapons, no move after a PFPh turn, and the gate's check | A tank's turret turns with a shot or with an MP. |",
        "| C3.22, D3.12, D2.11 | Pass 105 (moved on by pass 55, design 3): the Gun's action carried to a tank, with no more fire by any of its weapons, no move after a PFPh turn, and the gate's check | A tank's turret turns with a shot or with an MP. |")
replace(BACK,
        "| D2.18, p. 196 | Pass 55: an entry step whose cost the planner and the projector take as declared | A Delay MP after stopping serves. |",
        "| D2.18, p. 196 | Pass 105 (moved on by pass 55, design 3): an entry step whose cost the planner and the projector take as declared | A Delay MP after stopping serves. |")
replace(BACK,
        "| D3.12, D2.32, D2.321, C5.11 | Pass 55 | Refused with the reason. |",
        "| D3.12, D2.32, D2.321, C5.11 | Pass 105 (moved on by pass 55, design 3) | Refused with the reason. **Pass 55 built the free change of TCA on gaining Motion by a Motion attempt (design 12.2), and nothing else of this row.** |")
close("| A Motion firer of the side that is not phasing | Pass 50 (design 8.1) | D2.42, C5.35 | Pass 55: which Case C a DEFENDER's vehicle in Motion adds to | Refused. |",
      "design 12.2; ruling R55.14", "it fires its MA with Case C alone and the lower dr doubled, with no Acquisition DRM and no Case L.")
replace(BACK,
        "| A5.132; C5.64 | Passes 55 and 110 | Neither is built. |",
        "| A5.132; C5.64 | Passes 60 (task 60.6; moved on by pass 55, design 3) and 110 | Neither is built. |")
replace(BACK,
        "| C5.51, D3.2, D2.32, C5.5 | Passes 55 and 60 | Refused with the reason. |",
        "| C5.51, D3.2, D2.32, C5.5 | Pass 60; the Bypass part with D2.35 in pass 105 (pass 55 took none of it, and its design's section 3 does not name this row) | Refused with the reason. |")
replace(BACK,
        "| A CMG's CA in Bypass | Pass 50 (design 7.1) | D2.32, D2.321 | Pass 55 | Read as its TCA is anywhere else. |",
        "| A CMG's CA in Bypass | Pass 50 (design 7.1) | D2.32, D2.321 | Pass 105 (moved on by pass 55, design 3: D2.35) | Read as its TCA is anywhere else. |")
replace(BACK,
        "| Pass 55 (was 35 until 2026-10-05, then 33 until later that day, then 34 until 2026-10-09) |",
        "| Pass 55, built to the gate 2026-10-13: Motion attempts, fire at another level for ordnance and vehicles, and Armor Leaders (section 59 has what it left). Ordnance SMOKE and WP had moved to pass 85 and Passengers and Riders to pass 65 before it (the redesign plan, section 22.1); the pass took nothing of the Recall, Shock, and UK rows. (Was 35 until 2026-10-05, then 33 until later that day, then 34 until 2026-10-09.) |")

P55 = "Pass 55 (design {})"
LEFT = [
    ("Gun Duels: the case of equal Gun Duel DRM, in which the Final DRs decide the order", P55.format("13.1; rulings R55.19, R55.20") + ", against the plan's entry as carried in from pass 50", "C2.2401, pp. 167 to 168", "Pass 110: two shots rolled and then resolved in the order their Final DRs give", "No duel is offered, though the rule gives one: the declared shot resolves alone."),
    ("Gun Duels: fire that is not ordnance on either side; the second duel with the ROF as a DRM; a duel declared by the DEFENDER; a Gyrostabilizer", P55.format("13.1; ruling R55.20"), "C2.2401; C5.33, p. 172; D11.1", "Pass 110 (a Gyrostabilizer: pass 105)", "None is offered."),
    ("Gun Duels: a LATW's declared shot (a PF, a PSK, an ATR); a Gun stacked with other Infantry; a duel's shot with special ammunition", P55.format("13.1"), "C2.2401; C8.1", "Pass 110: a PF's check before the declaration. Pass 60 (task 60.5): the tank's shot at a Location a Gun shares with other Infantry, which an older check refuses", "The declared shot resolves alone. The duel's shot is AP at a vehicle and HE otherwise."),
    ("An Armor Leader not yet revealed is left out of both Gun Duel DRM at the declaration", P55.format("13.1; ruling R55.29") + "; the reviews", "C2.2401; D3.41, p. 200", "The user's reading of the ruling; or a declaration that tells the totals to no one", "The ATTACKER may be denied a duel that his leader's modifier would earn."),
    ("A declined duel does not bar another at the same expenditure; the projector trusts a declaration", P55.format("13.1"), "C2.2401", "Pass 110; a Rules check at replay that the choice was rightly offered", "A second declared shot at the same expenditure offers the duel again. The projector holds a duel shot to a `duel` answer for that vehicle at that expenditure and asks no more."),
    ("An Armor Leader counter; his purchase", P55.format("2, F7; ruling R55.15") + ", against the plan's entry", "D3.4, D3.41, p. 200; H1.43", "The DYO passes (190 and 195)", "He is a morale and a leadership on his vehicle, given at setup."),
    ("An AFV with Canister, and so the Bounding First Fire and Motion halvings in a game; the Gyrostabilized CMG", P55.format("2, F2; 10.1") + ", against the plan's entry", "C8.4, p. 178; D3.31, p. 200; D2.42", "Pass 105: a vehicle counter that lists Canister", "In the calculator and its test, and in no game: the Canister counter is a Gun."),
    ("Canister as Defensive First Fire, with FFMO and FFNAM", P55.format("10.1") + ", against the frame (ruling R55.10)", "C8.4; A8.2; C6.17", "Pass 110: the mover's own Location among several attacked, FFMO and FFNAM for one Location of several, the shots a Gun has at one expenditure, and Residual FP, which A8.2 gives Canister", "Refused in the MPh with its reason."),
    ("Canister: the building option; an aim at another elevation; the SMOKE, LV, and NVR clause; pillboxes; Infantry that mans a Gun in the MPh; the Bypass TCA", P55.format("3; 10.1"), "C8.4, p. 178", "Passes 140 (the building option), 85 (SMOKE), 115 (pillboxes), 110", "Not offered; a Location out of LOS is not attacked."),
    ("A Location that the IFT refuses refuses the whole Canister shot: an AFV in positive TEM, a Melee, prisoners", P55.format("10.1"), "C8.4; A7.4; ruling R25.6", "Pass 60 (task 60.1): the mixed Locations the IFT refuses", "The shot is refused. For a hidden AFV the refusal could tell the firer something; not tested."),
    ("A wall's or hedge's TEM on an ordnance To Hit DR", P55.format("8.1; ruling R55.24") + "; older than the pass", "C6.8, p. 175; B9.3", "Pass 100, with a fact on the record so that a shot recorded before resolves as it did", "The shot's map read finds the TEM and no ordnance calculator reads it. Such a target claims no Height Advantage."),
    ("A Critical Hit's reversal of the Height Advantage TEM", P55.format("8.1"), "C3.71, p. 170", "Pass 60: the fact carried on the hit's IFT attack", "An ordnance Critical Hit uphill gets no -1."),
    ("The leave of an AA weapon to fire at a higher target nearer than the elevation difference; the halftrack's AAMG", P55.format("8.1; ruling R55.23"), "C2.6, p. 168", "Pass 105, with the AA Guns", "The AAMG is held to the limit like any vehicle MG."),
    ("A Gun in a building hex firing within its hex; a vehicle's MG at another level of its own hex, which has no Play test and no game", P55.format("3; 8.1"), "C2.6; A7.21", "Pass 140; a setup that puts a tank in a building", "Refused with the rule; a Rules test holds the bar."),
    ("A cliff in a test board", P55.format("8.1"), "B11.31, B11.32, p. 126", "Pass 135; a board with a cliff in the tests", "The ordnance refusal with its exceptions rests on a Rules test."),
    ("The To Hit color of a light mortar and a LATW", P55.format("7.1; ruling R55.22"), "C3.3, p. 169; the chart of p. 695", "Pass 110 (task 110.1), with a fact on the record so that a recorded shot keeps its color", "Red for every nationality but the German; a Gun and a vehicle's MA follow the chart."),
    ("C8.9 on the Area Target Type", P55.format("9.1") + ", against the frame (ruling R55.7)", "C8.8, C8.9, p. 179", "A mortar that lists an HE number", "Nothing is built there; no mortar of the catalog lists one."),
    ("AP against Infantry by a Gun whose HE is by a Depletion Number", P55.format("9.1"), "C8.31, p. 178", "Pass 60 (task 60.4)", "A 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry with the reason."),
    ("The words of an APCR or HEAT record whose DR above the number is also a malfunction", P55.format("9.1"), "C8.9, p. 179", "A fact that tells a record made before the pass apart", "It keeps the To Hit result it showed before the pass, which may read \"hit\" beside a malfunction; APDS, HE, and AP by number say there was no shot."),
    ("The DYO Majority Squad Type for Elite; a card that carries `elite-ammunition`; the 28LL and 40LL; a listing's theater mark and the PTO's three more", P55.format("3; F3; rulings R55.4, R55.6"), "C8.2, p. 178; C4.3, p. 171; C7.32", "The DYO passes; pass 110 for the two Guns; Chapter G", "Elite only by a game's special rule; no card carries it. No such Gun in the catalog. Every game is taken as the ETO."),
    ("A game with a year and no month", "Pass 55 reviews, 2026-10-13 (referee, first half, 2; ruling R55.31)", "C8.11, p. 177", "A card that gives its month", "A number that starts in a month of that year is not available; the note and the refusal say why."),
    ("A card's setup plan cannot give a vehicle an Armor Leader or an Inexperienced Crew; the map counter's tooltip and the placement list's row do not name him", P55.format("11.1"), "D3.41, D3.45, p. 200", "The card's setup plan; the two page texts", "A hand setup on the page and a game file can. The units table and the firer's label name him for his owner."),
    ("Armor Leaders: the HD Maneuver dr; the halftrack's FG with its Passengers; the assisting AFV of D8.3; the leave to withhold his modifier; his return when the crew re-crews; his VP", P55.format("3; 11.1; rulings R55.16, R55.17"), "D3.44, D3.43, p. 200; D4.22; D6.64, D6.65; D8.3; A10.72", "Passes 105 (the HD Maneuver dr by the design's section 3; D4.22's own row is pass 100's) and 65", "His modifier is always applied; he does not return."),
    ("Whether a vehicle given an Armor Leader is an armed AFV is checked by the planner's setup bar alone", P55.format("11.1"), "D3.4, p. 200", "The same Rules member in the projector", "A hand-written file with a leader on a truck replays."),
    ("Motion attempts: a stall; the platoon's and the radioless AFV's drm; the Convoy's bar; a facing or a turret for a vehicle in Bypass", P55.format("3; 12.1"), "D2.401, D2.51, p. 198; D14.211, D14.23; D2.321", "Pass 105", "No stall anywhere in the code. A facing or a turret named for a vehicle in Bypass is refused; the attempt alone is allowed there."),
    ("A vehicle's own Bog and Mechanical Reliability checks have no record line of their own on the page", P55.format("12.1"), "(page)", "A reader of the event in PlayRecords and ReplaySteps", "The Motion attempt's line says what they did to it."),
    ("No Studio game of an OVR or of the vehicle's Close Combat attack with an Armor Leader", P55.format("11.1"), "(games)", "A game for each", "Their Play tests hold those two uses."),
]
R = "Pass 55 reviews, 2026-10-13 ({})"
FIX = "A fix in the member named in the review"
OPEN = [
    ("A Canister fire record is not held to its shot at replay", R.format("Rules boundary 2; referee, first half, 4"), "C8.4, p. 178", FIX, "Its halvings, its Case A, and its roll are taken from the record."),
    ("The count and order of a Motion attempt's checks are decided in Play", R.format("Rules boundary 5"), "D2.401, D2.51, p. 198", FIX, "Replay does not ask that a passed attempt by a red-MP AFV carries its Mechanical Reliability DR."),
    ("Which Locations a Canister shot attacks is partly decided in Play", R.format("Rules boundary 6"), "C8.4, C8.41", FIX, "Play makes the LOS and NVR skip and the Melee refusal, and sets two facts to pass Rules' own tests."),
    ("Two Canister decisions in LiveOrdnance", R.format("Rules boundary 8"), "C8.4; C6.5", FIX, "A second refusal in the MPh, and the Acquisition zeroed before Rules sees it."),
    ("When an Armor Leader is revealed is decided in the projector", R.format("Rules boundary 9"), "D3.41, p. 200", FIX, "By the fact's presence in the record, not by the modifier's use."),
    ("Smaller copies outside Rules: the PFPh bar on an Opportunity Firer in the Canister planner, the turret types twice more, what Reaction Fire is, the four grades in the placement editor", R.format("Rules boundary 10"), "A7.25; D1.3; D7.2; D3.4", FIX, "Each works today."),
    ("A Canister KIA's chance of destroying a SW", R.format("referee, first half, 7"), "C8.42, p. 178", "Pass 60 (task 60.5, with A9.74)", "No SW is destroyed by any KIA in the code."),
    ("Height Advantage on top of a CE crew's DRM under Infantry fire", R.format("referee, second half, 4") + "; older than the pass", "D5.31, p. 203; B10.31, p. 125", "Pass 65, with a fact on the record so that an older attack resolves as it did", "Infantry fire adds both; a vehicle's MG fire and Canister do not."),
    ("A Motion attempt does not count an expenditure made in a Location whose LOS is undecided", "Pass 55, the fixing agent, 2026-10-13", "D2.401, p. 198", "A decided LOS for every Location, or a refusal", "Such an expenditure adds nothing to the number."),
    ("A Passenger's time aboard is not read when it later moves on foot and a Motion attempt is made against it", "Pass 55, the fixing agent, 2026-10-13", "D2.401, p. 198", "Pass 65", "Only its steps on foot are read."),
    ("A `gun-duel-voided` of cause `other` is admitted at replay without the planner's precheck", "Pass 55, the fixing agent, 2026-10-13", "C2.2401", "The precheck's facts in the projector", "The projector refuses a void of the rule's four causes for a firer still able to fire; `other` it takes as recorded, with its reason."),
    ("Whether a cliff hexside is stored on both hexes that share it; whether B10.31's Crest Line exception reaches a moving vehicle on the Vehicle Target Type", R.format("referee, second half: not checked by any review"), "B11.31, p. 126; B10.31, p. 125", "A read of the map data; a test", "Not known."),
    ("The C13.8 bar on a PF or PSK at a target two or more levels higher in an adjacent hex, or directly above", "Pass 55, found while the documents were written, 2026-10-13; not tried in a game", "C13.8, p. 185; rulings R9.8, R9.11", "Pass 110", "A LATW is free of C2.6's limit, and a search of Rules and Play found no such bar; before the pass every ordnance shot at another level was refused, so the bar was not reached."),
    ("No page test for five fixes of the UI review", R.format("UI 1, 2, 3, 5, 6"), "(tests)", "Page tests of a hand-over, a change of game, and a Gun that turns after its aim is chosen", "The fixes rest on the build and on reading."),
]
for a, b, c, d, e in LEFT + OPEN:
    assert "|" not in a + b + c + d + e and "\u2014" not in a + b + c + d + e, a
rows = lambda items: "".join(f"| {a} | {b} | {c} | {d} | {e} |\n" for a, b, c, d, e in items)
append(BACK, """## 59. Added by pass 55

Written with the pass's documents on 2026-10-13. The first table is what a section of the [pass 55 design](<Passes/ASL Unit Backlog Pass 55 Design.md>) lists as left out, with what the plan's entry or the frame placed in the pass and the build did not make; the second is what the reviews and the build found and did not fix (design 14.3). Rows of earlier sections that named pass 55 are corrected where they stand: what the pass built is marked there, and what it moved on names its new pass.

### 59.1 What the pass left out

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + rows(LEFT) + """
### 59.2 What the reviews and the build found and did not fix

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
""" + rows(OPEN))

# --- The redesign plan -----------------------------------------------------------------------------------------------
PLAN = "ASL Card Play and Map Studio Redesign Plan.md"
replace(PLAN,
        "**Pass 55: Armored combat II** (planned at 5:25 of build; 20 rows of the inventory). Counters: a Gun or AFV with APDS; one with Canister; Armor Leaders 8-1 to 10-2. Needs first: 50.",
        "**Pass 55: Armored combat II** (planned at 5:25 of build; 20 rows of the inventory). Counters: a Gun or AFV with APDS; one with Canister; Armor Leaders 8-1 to 10-2. Needs first: 50. **Status, 2026-10-13: built to the merge gate on `feature/asl-backlog-pass-55`; the gate, the merge, and the push are the user's to start.** Tasks 55.1 to 55.6 are built, and a seventh was added: 55.7, Gun Duels, carried in from pass 50 and built as a slice (the frame's estimate 1:30). The inventory named 35 rows, not 20. The frame's estimates, made from the code, with the plan's in brackets: 55.1 with 55.6, 2:30 (1:00); 55.2, 2:15 (0:40); 55.3, 1:10 (0:40); 55.4, 1:10 (0:40); 55.5, 1:15 (0:40); 55.7, 1:30 (not in the plan). That is 9:50 of building against 3:40, and 11:05 in all against 4:55. What the pass left out went to pass 60 (A5.132; AP against Infantry, C8.31), pass 65 (the halftrack's FG and a Passenger leader), pass 105 (the rest of a vehicle's handling of its weapons and arcs; an AFV with Canister; a stall), pass 110 (the Gun Duel's equal case and its remainder; Canister as Defensive First Fire), and the DYO passes (an Armor Leader's counter and purchase), each with its reason in the [design](<Passes/ASL Unit Backlog Pass 55 Design.md>), sections 3 and 14.1, and the backlog's section 59.")
replace(PLAN,
        "| 55.6 The counters |  | 0:20 |\n| Overhead | | 1:15 |\n| **Pass 55 total** (build 3:40) | | **4:55** |",
        "| 55.6 The counters |  | 0:20 |\n| 55.7 Gun Duels, the slice | C2.2401 (p. 167) | 1:30 |\n| Overhead | | 1:15 |\n| **Pass 55 total** (build 3:40 as planned, without 55.7; 9:50 by the frame's estimates, with it) | | **4:55** as planned; **11:05** by the frame's estimates |")
replace(PLAN,
        "| **Pass 60 total** (build 4:40) | | **5:55** |",
        "| **Pass 60 total** (build 4:40) | | **5:55** |\n\nMoved here by pass 55 (2026-10-13), with no task renumbered: a hit on another vehicle of an overstacked hex (A5.132, with 60.6); AP against Infantry by a Gun whose HE is by a Depletion Number (C8.31, with 60.4); a Critical Hit's reversal of the Height Advantage TEM (C3.71); a Canister KIA and a SW (C8.42, with 60.5).")
replace(PLAN,
        "| **Pass 65 total** (build 7:00) | | **8:15** |",
        "| **Pass 65 total** (build 7:00) | | **8:15** |\n\nMoved here by pass 55 (2026-10-13), with no task renumbered: the FG of a CE halftrack with its Passengers under an Armor or a Passenger leader (D3.4, D6.64, D6.65, with 65.2); a Passenger leader in an OVR (D7.16); an Armor Leader's return when his crew re-crews (D3.43, with 65.6); the CE DRM beside Height Advantage under Infantry fire (D5.31, with 65.1).")
replace(PLAN,
        "| **Pass 105 total** (build 7:00) | | **8:15** |",
        "| **Pass 105 total** (build 7:00) | | **8:15** |\n\nMoved here by pass 55 (2026-10-13), with no task renumbered. Whole rows: a tank's turret or VCA changed without fire (C3.22); Case J against a vehicle that has paid only its Start MP, and VBM as a cause (C6.1); a BMG fired by a pivot of the hull (D1.81); a declared higher MP expenditure (D2.18); the TCA in Bypass (D2.35); the VCA changed for a shot (D3.11); the Known-target condition and the BMG's NT penalty (D3.51, D3.52). Remainders: the AA weapons' leave to fire up (C2.6); a Gyrostabilizer for a Motion firer (C5.35); a stall and the platoon's drm in a Motion attempt (D2.401); the other changes of VCA and TCA (D2.11, D3.12); an AFV with Canister (D3.31); the HD Maneuver dr and an assisting AFV for an Armor Leader (D3.44).")
replace(PLAN,
        "| **Pass 110 total** (build 7:20) | | **8:35** |",
        "| **Pass 110 total** (build 7:20) | | **8:35** |\n\nMoved here by pass 55 (2026-10-13), with no task renumbered: the Gun Duel's equal case, fire that is not ordnance, the second duel, and a duel declared by the DEFENDER (C2.2401, C5.33); the 28LL and 40LL (C4.3, C7.32); Canister as Defensive First Fire and its other clauses (C8.4). Of 110.1's To Hit colors (C3.3), pass 55 built a Gun's and a vehicle's MA's; a light mortar's and a LATW's are left.")

# --- The coverage document -------------------------------------------------------------------------------------------
COV = "ASL Rule Coverage.md"
S = " **Pass 55 (2026-10-13):** "


def cover(section_start, missing_end, passes, words):
    """Append the pass's sentence to the Missing cell of a section's row: the row is found by its start, the cell by its last words and the Pass cell after it."""
    old = missing_end + " | " + passes + " |"
    hits = [line for line in text(COV).split("\n") if line.startswith(section_start) and line.endswith(old)]
    assert len(hits) == 1, (section_start, len(hits))
    assert "Pass 55" not in hits[0] and "|" not in words
    replace(COV, hits[0], hits[0][:-len(old)] + missing_end + S + words + " | " + passes + " |")


cover("| B10 | Hills | ", "Interdiction ignores Height Advantage", "34, 60; ruling",
      "Height Advantage is Case Q +1 for ordnance and is read for a vehicle's MG fire (10.31); pass 35 had repaired Interdiction's read of it. Left: a Critical Hit's reversal of it.")
cover("| C2 | Gun Classifications | ", "where 2.21 has HE", "33, 34, 53, 54",
      "Gun Duels (2.2401) are built as a slice: ordnance on both sides, the vehicle's Gun Duel DRM lower, the first duel; the equal case is left. Ordnance and a vehicle's MGs fire at another level when the range is at least the elevation difference (2.6); another level of the firer's own hex stays refused. A Gun whose HE has a Depletion Number never fires it unnamed (2.21).")
cover("| C4 | Basic TH# Modifications | ", "APDS and SMOKE not built", "34, 56",
      "APDS is built (4.3); the 28LL and 40LL have no counter.")
cover("| C8 | Special Ammunition | ", "Illuminating Rounds (8.7) not built", "34, 50, 56, 58",
      "APDS (8.11), a start month in a counter's listing (8.12), Elite +1 by a game's special rule (8.2), HE and AP by a Depletion Number (8.8), and Canister with its adjacent hex (8.4, 8.41) are built; 8.9's three outcomes hold for each of them. Two counters carry them: the OQF 6-Pounder and the M3A1 37mm AT Gun. Left: Canister as Defensive First Fire and from an AFV; a Canister KIA and a SW (8.42); HE Equivalency; SMOKE and WP.")
cover("| D2 | Vehicular Movement | ", "Non-Stopped read from the move in hand (R35.9).", "33, 34, 45, 52, 53; ruling",
      "Motion attempts (2.401) are built for a DEFENDER's vehicle in the enemy MPh, with the free change of VCA and TCA, the Bog Check, and the Mechanical Reliability DR; a Hidden vehicle attempts none. Left: a stall; the Bypass TCA (2.321); a declared higher expenditure (2.18). The Delay MP (2.17) is pass 50's.")
cover("| D3 | AFV Combat | ", "BMG and CMG malfunctions have no repair", "33, 34, 53",
      "Armor Leaders and Inexperienced Crews (3.4 to 3.45) are built: his modifier on the MA's To Hit DR, a MG MA's IFT DR, the OVR, the vehicle's own CC attack, and Bog Removal, his morale as the crew's, hidden from the enemy until used; no counter in the catalog. A DEFENDER's vehicle in Motion fires its MA with Case C and the lower dr doubled. The bold text before this is the first version's and is older than pass 50, which built the MA's fire while moving, the BMG and CMG, and their repair. Left: MG against AFV (3.54); the HD Maneuver dr; the halftrack's FG.")
replace(COV,
        "**Pass 35 (2026-10-09, on its branch, not yet merged)** changed the rows of 25 sections and 59 rows of the inventory; the counts of sections 3 to 6 are recomputed from the inventory, and the faults of section 8.3 it repaired are marked there.",
        "**Pass 35 (2026-10-09, on its branch, not yet merged)** changed the rows of 25 sections and 59 rows of the inventory; the counts of sections 3 to 6 are recomputed from the inventory, and the faults of section 8.3 it repaired are marked there. **Pass 55 (2026-10-13, on its branch)** changed 58 rows of the inventory and the counts of seven sections (B10, C2, C8, D2, D3, D4, D7), and added its sentence to the rows of B10, C2, C4, C8, D2, and D3; no section's status changed.")

# --- Write -----------------------------------------------------------------------------------------------------------
for name, body in files.items():
    assert "\r" not in body and "\u2014" not in body and body.endswith("\n"), name
    assert name == COV or not body.endswith("\n\n"), name  # the coverage document ends with a blank line at HEAD, and keeps it
if "--check" in sys.argv:
    print("every anchor found;", len(files), "files would be written")
    sys.exit(0)
for name, body in files.items():
    io.open(D + name, "w", encoding="utf-8", newline="").write(body)
print("written", len(files), "files:", ", ".join(sorted(files)))
