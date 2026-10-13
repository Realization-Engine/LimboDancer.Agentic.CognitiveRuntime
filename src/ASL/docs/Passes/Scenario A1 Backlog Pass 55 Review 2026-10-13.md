# Scenario A1 Backlog Pass 55: Armored combat II

The review of pass 55, written on 2026-10-13 after the five reviews and their fixes. The Studio check of the whole pass is in its own section below. The gate was run later that day and passed: the last section. The design is [ASL Unit Backlog Pass 55 Design](<ASL Unit Backlog Pass 55 Design.md>); its section 15 says what was built.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-55`, cut from main at 846eb5a3: the frame, seven tasks in seven commits, then the five reviews, their fixes, and these documents. Everything was done under the user's word of 2026-10-12 for this pass, "I want you to run without my intervention up to the Gate", and is open to a veto. **The gate passed on 2026-10-13 (the last section). Merged into main as 50987ff3 and pushed that day** at the user's word.

## What departs from the plan's entry and the frame

- **The pass is about twice the plan's size** (section 2; ruling R55.1). The plan gives six tasks, 20 rows, and 3:40 of building; the inventory named 35 rows, Gun Duels came in as a seventh task, and the frame's estimate was 9:50 of building.
- **Gun Duels (C2.2401, p. 167) is a slice** (sections 2 and 13.1; rulings R55.19, R55.20): ordnance on both sides, the vehicle's Gun Duel DRM strictly lower, the first duel of an expenditure. **When the two totals are equal no duel is offered, though the rule gives one** with the Final DRs deciding the order; the rule's own example is that case. Fire that is not ordnance, the second duel with the ROF as a DRM, a duel declared by the DEFENDER (C5.33, p. 172), and a Gyrostabilizer are left with it.
- **No Armor Leader counter is in the catalog** (section 2; F7; ruling R55.15). The plan names "Armor Leaders 8-1 to 10-2" among the pass's counters. He is a morale and a leadership on his vehicle, as D3.41 (p. 200) keeps the counter out of play.
- **The Canister counter is a Gun, not an AFV** (section 2; F2), so the halvings for Bounding First Fire and Motion (C8.4, p. 178; D3.31, p. 200) are in the calculator and its tests and in no game. **Canister is not fired as Defensive First Fire** (section 10.1): in the MPh it is refused with its reason, and the first half of ruling R55.10 (FFMO and FFNAM for Canister) is not built.
- **A wall or hedge the LOS crosses gives an ordnance shot no TEM on its To Hit DR** (section 8.1). This is older than the pass and is left as found, since the repair would change what a recorded shot resolves to. Such a target claims no Height Advantage (ruling R55.24).
- **Nothing of C8.9 is built on the Area Target Type** (section 9.1), though ruling R55.7 named all three Target Types: only mortars fire on it, and no mortar of the catalog lists an HE number.
- **AP against Infantry (C8.31, p. 178) stays out** (section 9.1): a 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry.
- **A Critical Hit does not reverse the Height Advantage TEM** (C3.71, p. 170; section 8.1).
- **The halftrack's AAMG is held to C2.6's limit** (p. 168; section 8.1; ruling R55.23), though the rule names "non-AA vehicular MG". The AA Guns' leave is not built.
- **The British light mortar and every LATW keep red To Hit numbers** (section 7.1; ruling R55.22), though the chart of p. 695 gives the British black.
- **A Gun Duel is not offered** for a LATW's declared shot, or to a Gun stacked with other Infantry; **an Armor Leader who is not yet revealed is not counted** in either Gun Duel DRM at the declaration, his own vehicle's or the declaring vehicle's (section 13.1; ruling R55.29, which the reviews extended to the declaring vehicle), and this can deny the ATTACKER a duel his modifier would earn. A declined duel does not bar another at the same expenditure. The projector trusts a declaration. Special ammunition is not offered for the duel's shot (section 13.1).
- **A Hidden vehicle may not attempt Motion** (A12.32, p. 80; ruling R55.32), and **gaining Motion costs a concealed vehicle its "?" whether or not its CA changes** (A12.141, p. 78; ruling R55.33). Both came from the reviews; the frame had neither.
- **A Motion attempt that no dr could pass is refused and costs nothing** (section 12.1; ruling R55.28). A facing or a turret named for a vehicle in Bypass is refused. A stall, the platoon's and the radioless AFV's drm (D14.211, D14.23), and the Convoy's bar are not built. A vehicle's own Bog and Mechanical Reliability checks have no record line of their own on the page (section 12.1).
- **A Location that the IFT refuses refuses the whole Canister shot** (section 10.1): an AFV in positive TEM, a Melee, prisoners. Left out as the frame said: the building option, an aim at another elevation, the SMOKE and LV clause, pillboxes, Infantry that mans a Gun in the MPh, the Bypass TCA, an AFV with Canister. **A Canister shot calls no Sniper,** as ordnance does not (ruling R55.30).
- **An Intensive Fire shot that finds no round leaves no Intensive Fire counter** (section 9.1; ruling R55.25). **A number that starts in a month of the game's year is not available when the game gives no month** (ruling R55.31). A malfunction on a DR above the number keeps, for an APCR or HEAT record, the words it showed before the pass (section 9.1).
- **Armor Leaders** (section 11.1): a card's setup plan cannot give a vehicle one; the map counter's tooltip and the placement list's row do not name him; whether the vehicle is an armed AFV is checked by the planner's setup bar and not by the projector. Not built, as the frame said: the HD Maneuver dr, the halftrack's FG with its Passengers, the assisting AFV of D8.3, the leave to withhold his modifier (A10.72), his return when the crew re-crews, his VP. Added beyond the brief: an Inexperienced Crew's +1 lowers Residual FP (A8.26).
- **Tests and games that are lacking:** no Play test and no game for a vehicle's MG at another level of its own hex; a cliff (B11.31, B11.32, p. 126) by a Rules test only (section 8.1); no Studio game of an OVR or of the vehicle's Close Combat attack with an Armor Leader (section 11.1); no page test for the UI review's fixes 1, 2, 3, 5, and 6.
- **The counters need two crews as well,** and **the catalog goes to version 1.14.0** (section 2); the cards, the fixtures, and the tests that name 1.13.0 are left as they are.
- **Rows that earlier passes moved here are moved on** (section 3; ruling R55.2). Section 2 says fifteen. Section 3's table names nine rows moved whole (A5.132 to pass 60; C3.22, C6.1, D1.81, D2.18, D2.35, D3.11, D3.51, and D3.52 to pass 105) and C5.33 to pass 110; the others it counts are rows built in part whose remainder moves. The inventory has each row's own pass.

**Departures of sections 7.1 to 13.1 that the reviews closed,** and so have no row: the second reading of the 84 transcription rows (section 7.1), made by the referee's first half; the possible leak of a crew's Morale Level in a pending choice's resume (section 11.1); the filter for A7.55 that stood outside Rules (section 10.1); the projector's check of a recorded Motion attempt, which now applies the planner's bars (section 12.1, in part: the count and order of its checks are still Play's, section 14.3). Section 10.1 said that no test reaches a pending choice in one attacked Location of a Canister shot; `ACanisterShotResumedAfterAnOwnersChoiceCallsNoSniper` now does.

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

**Fixed:** 20 rows in the design's section 14.2. The main ones after the BLOCKER:

- A concealed vehicle kept its "?" after a passed Motion attempt. It loses it, with or without a change of CA (A12.141, p. 78; ruling R55.33).
- A Gun Duel declaration, and a pending choice's resume, could tell the enemy of an Armor Leader not yet revealed. He is left out of both Gun Duel DRM at the declaration (ruling R55.29, extended), and a resume no longer holds a crew's Morale Level.
- A CE crew fired on from below by a vehicle's MG got Height Advantage on top of its CE DRM (D5.31, p. 203). Dropped for a vehicle's fire and for Canister.
- The projector did not hold a recorded Motion attempt to the planner's bars; whether a duel's return shot is void was decided in Play; three filters for A7.55 stood outside Rules. Each is one Rules member now.
- The adjacent hex of a Canister shot was attacked when no Location of the cluster was (C8.41, p. 178). Sniper checks on a Canister shot depended on the path: none now (ruling R55.30).
- A shot that had no round was recorded as a "miss" that "may fire again (C2.24)". It reads "no shot: it had no APDS" and "it has not fired and may fire other ammunition (C8.9)", and the Depletion Number is carried in the resolution.
- The page: the placement form's and the Motion attempt's choices outlived a hand-over; the ammunition list and the shot followed different tests; a Canister aim was sent without checking it is among the aims shown.

**Two findings came after the five reviews, and both are fixed.** The fixing agent found that the answer to a duel's choice showed the DEFENDER's unrevealed Armor Leader in the declared shot's summary: the leak was real, and both answers now word the declared shot without his line (`ScenarioA1ArmorLeaders.ToldToTheEnemy`); the shot is still fired with his modifier, and its record reveals him. And the documents' agent found that C13.8's bar (p. 185) was missing: with levels opened, a PF or PSK, which C2.6 frees of its limit, could fire at a target two or more levels higher in an adjacent hex. It is refused there and at a higher Location of its own hex, with the rule in the refusal (`BackblastElevationRefusal`); an ATR is not barred. Before the pass every ordnance shot between levels was refused, so no record holds such a shot. A LATW fired down within its own hex stays refused, which is stricter than the rules; it has a backlog row.

**Not fixed:** 14 rows, each with a row in the backlog's section 59.2:

| Review | What |
|---|---|
| Rules boundary 2; referee, first half, 4 | A Canister fire record is not held to its shot at replay: its halvings, Case A, and roll are taken from the record |
| Rules boundary 5 | The count and order of a Motion attempt's checks are decided in Play, and replay does not ask that a passed attempt by a red-MP AFV carries its DR (D2.401, D2.51, p. 198) |
| Rules boundary 6 | Which Locations a Canister shot attacks is partly decided in Play: the LOS and NVR skip, the Melee refusal, two facts set to pass Rules' own tests |
| Rules boundary 8 | Two Canister decisions in `LiveOrdnance`: a second MPh refusal, and the Acquisition zeroed before Rules sees it |
| Rules boundary 9 | When an Armor Leader is revealed is decided in the projector, by the fact's presence and not by the modifier's use |
| Rules boundary 10 | Smaller copies outside Rules: the PFPh bar on an Opportunity Firer in the Canister planner, the turret types twice more, what Reaction Fire is, the four grades in the placement editor |
| Referee, first half, 7 | C8.42's second sentence (p. 178): a Canister KIA and a SW. No SW is destroyed by any KIA in the code |
| Referee, second half, 4 (the other half) | Height Advantage on top of a CE crew's DRM under Infantry fire (D5.31, p. 203); older than the pass |
| The fixing agent | A Motion attempt does not count an expenditure made in a Location whose LOS is undecided |
| The fixing agent | A Passenger's time aboard is not read when it later moves on foot and a Motion attempt is made against it |
| The fixing agent | A `gun-duel-voided` of cause `other` is admitted at replay without the planner's precheck |
| Referee, second half (not checked by any review) | Whether a cliff hexside is stored on both hexes that share it, for a vehicle's MG firing down through one (B11.31, p. 126); whether B10.31's Crest Line exception reaches a moving vehicle on the Vehicle Target Type |
| UI 1, 2, 3, 5, 6 | No page test for these five fixes |
| (the session) | A LATW fired at a lower Location of its own hex is refused as out of range, though neither C13.8 nor C2.6 bars it. Stricter than the rules; found with the C13.8 fix, and not tried in a game |

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

After the reviews' fixes, the fixing agent's runs: Rules 220 in nine classes; Play 184 in eleven; Units 416 whole; MapStudio 75; none failed. (A sentence here said that the four test projects build with warnings as errors; it was false, and the gate's section says why.) The pass's own classes are `ScenarioA1Pass55RulesTests` (60 test methods as this is written) and `BacklogPass55Tests` (69). The Rules and Play projects were not run whole.

Lacking: a Play test and a game for a vehicle's MG at another level of its own hex; a board with a cliff; a Studio game of an OVR or of the vehicle's Close Combat attack with an Armor Leader; page tests for the UI review's fixes 1, 2, 3, 5, and 6.

## The replay digest

Over the proof store, 473 games and 93,266 lines, after each of the seven increments. The base was built from main's sources at 846eb5a3. The catalog's identity, which is hashed into every state and went to 1.14.0, was left out on both sides, and the pass's new properties were left out. No line differed in any of the seven runs. The store holds none of the pass's own games, so this shows that nothing recorded before the pass replays differently, and nothing more. The run after the reviews' fixes (99456de6), with the pass's 144 new properties left out: no line differs. All 53 `p55-*` games of the live folder replay to their end on that build with no diagnostic.

## What is open to a veto

- **The rulings R55.1 to R55.33,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5, and one line each in the design's section 15. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-12. Those most likely to be read again: R55.14 (which Case C a DEFENDER's Motion firer takes, a reading with no sentence of its own), R55.15 (no Armor Leader counter), R55.24 (no Height Advantage behind a wall whose own TEM is not given), R55.29 (an unrevealed Armor Leader left out of a Gun Duel DRM), R55.31, R55.32, and R55.33. R55.6 supersedes R7.6.
- **The departures** listed above.
- **Pass 50's, still unanswered:** task 50.4, Gun Duels, is now built as a slice; task 50.5, MG fire against armor, is not built and waits on pass 60; rulings R50.1 to R50.14.

## What the gate will meet

- **The gate is one script now, and this is its first full run** (`pass32-tools/gate/gate.sh`). Its skip list from the diff, its `trial` stage, and the refusal of `proofs` without a passed trial were tested on pass 50's data; its `baseline`, `build`, `suite`, and `docker` stages and the long runs of `proofs` have had a syntax check only (`Prompts/Pass 55 Prompt I.md`). A stage may fail for a reason of the script's own.
- **`BOTH_SKIP`.** The catalog's identity differs in every state between main and the branch. The script has a list of properties left out on the baseline's side as well; it is in the script and has had a syntax check only. Without it every line differs and the run is void, as the pass's first trial showed.
- **`gate/pass55.conf` was written after this section:** the pass's recorded games, its extra sweep and trial games, and the JSON keys it added.
- **The sweep proves little of what the pass built.** It asks one request of each kind for each unit and names no new argument, so a Canister aim, a named ammunition, a Motion attempt, and a duel's answer are reached only in the pass's own games, where main has no answer to compare with. For those the proof is the pass's tests and Studio games.
- **The whole suite.** Rules and Play run whole for the first time on this branch; so do the Node viewport test, the chart supplement, the text list, and Docker.
- **The store** holds none of the pass's games. Those labelled SUPERSEDED are not to be taken.

## The Studio check

In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's, each control measured for anything past either edge and the page for being wider than its window:
  - `p55-w-can`: the 37mm AT Gun's ammunition list, the Canister facing and aim lists, and the propose button, in the American view and the adjudicator's; the German view shows none of them.
  - `p55-w-apds`: the 6-Pounder's ammunition list with its Depletion Numbers, the target and the vehicle lists, in the British view.
  - `p55-w-mot`: the "Motion attempt (D2.401)" block with its vehicle, VCA, and TCA lists and its button, in the Russian view and the adjudicator's; the German view shows none of it.
  - `p55-w-duel`: the Gun Duel's pending choice with its two options, in the German view and the adjudicator's; the Russian view shows the choice's text and no option.
  - the placement form's Armor Leader list and Inexperienced Crew box, at 1920 and at 320.
  Nothing past an edge at any width, and the page never wider than its window. Under 1024 pixels the controls were read in the Actions tab.
- **A Player Turn at 320** (`p55-w-mot`, the adjudicator's view): the T-34's Motion attempt with its VCA turned west, its MA fired from Motion (Case C +4, Case J2 +4, the lower dr doubled), the DEFENDER's pass, the German tank's Stop MP, the end of its move, and every phase to the Russian Rally Phase. The page stayed 320 wide at every step, and the T-34 still carried its Motion counter in the Russian RPh.
- **By the keyboard:** the pane was on screen in this session. In `p55-w-can` at 1920 the propose button and then the confirm button were given the focus and pressed with a real Enter key each; the Canister shot at aim 4 was proposed and committed that way. A real Tab key moved the focus on the new-game page. The lists were set by script, not by arrow keys.
- **The reviews' fixes, seen:** a shot with no round reads "no shot: it had no APDS ... it has not fired and may fire other ammunition (C8.9)" (`p55-w-apds`); a Canister aim reads "2 units in view, 1 concealed stack" and no longer counts the tank; the Motion record reads "attempts Motion as vehicle G1 moves: vehicle G1 has spent 1 MP in its LOS"; the Gun Duel's choice is one sentence with both totals; the placement form shows the Armor Leader for a tank only, and a chosen leader disables the Inexperienced Crew box with "an Inexperienced Crew is its own 6+1 Armor Leader (D3.45)".
- **All 48 `p55-*` games of the live folder** were replayed on the build with the fixes: each to its end, none with a diagnostic.
- **Not done:** `p55-fix-hidden` does not stage its case: the engine reveals a hidden vehicle when an enemy moves into its LOS, so the T-34 was no longer hidden when the attempt was offered; ruling R55.32's bar rests on its Rules and Play tests. No Studio game holds an OVR or a vehicle's Close Combat attack with an Armor Leader, a vehicle's MG at another level of its own hex, a cliff, or a PF or PSK fired at another level. The Replay page was not opened, though its new steps are in its code and its tests. The 683 and 320 widths were measured for overflow and not looked at one by one beyond one screenshot at 320.

## The gate

Run on 2026-10-13 at the user's word ("start the gate and stop before the merge"). **It passed;** the design's section 16 has the table, the differing lines, and what went wrong. In short:

- **The strict solution build failed at its first try** on 450 style errors in four of the pass's test files, fixed in 8862d209 (test files only). The sentence above that the four test projects build with warnings as errors was false when written: the pass's test script builds them with warnings not as errors, and no strict build of them had been run.
- The whole suite (eleven projects, none failed; Rules 897, Play 819), the Node viewport test, the chart supplement, and Docker on 8862d209 (3,488 tests) passed at their first run.
- The text list differs in 70 lines, each the pass's own. The replay digest differs only in 28 of the pass's 31 games; no game made before the pass replays differently.
- The sweep has 758 new lines, all in the pass's four games, and 2,511 lines of older games with the same text and another hash: the rule packages' digests, which changed with the catalog and are carried by every planned event. With that reference left out on both sides, no line of 515,357 differs.
- The baseline's sweep was lost once, after 53 minutes, to a game main cannot read; the gate took 2:45.

The user approved all that was open to a veto on 2026-10-13: the departures, rulings R55.1 to R55.33, and pass 50's R50.1 to R50.14. Merged into main as 50987ff3 and pushed on 2026-10-13 at the user's word ("merge and push"). The CI run of the merge failed at its locked restore for a reason outside the pass, a .NET SDK patch published that day; the design's section 16 has it, and 53a22fcf moved the lock file.
