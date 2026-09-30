# ASL Unit Card Play, Deferred Rules, and DYO Plan

**Status:** Approved by the user on 2026-09-30, with all five questions of section 5 answered. No pass has started; pass 23 waits for the user's go-ahead.

**Date:** 2026-09-30

**Scope:** three groups of open backlog rows in the [ASL Unit Backlog](<ASL Unit Backlog.md>), in eight passes numbered 23 to 30 after the [ASL Unit Scenario Card Games Plan](<ASL Unit Scenario Card Games Plan.md>):

1. **Card-driven play** (sections 27 to 32): per-side views and hidden setup, Control of Locations and Gun and vehicle VP, vehicles and Guns entering from off board, and the card editor's forms and map picking.
2. **Rules deferred by earlier passes:** entry and exit (advance and Bypass exits, blocked entry, entering occupied or fired-on hexes), and the Heat of Battle, Leader Creation, and berserk gaps.
3. **The Chapter H DYO purchase** (H1 to H1.84), which generates a scenario card, as the user ruled on 2026-09-29.

Section 1 of the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>) (how a pass is run, the standing rules, and the merge gate) applies unchanged to every pass here.

**Related documents:** the designs and reviews of passes 17 to 22; the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), whose ordnance and vehicle passes this plan builds on.

## 1. Direction

**Every game stays a card.** Each pass either makes a card's play fuller (groups 1 and 2) or makes cards (group 3). The DYO purchase ends in a saved user card that the Play page starts like any other, so it adds no second path into a game.

**Rules first, then tools.** The deferred rules (passes 25 and 27) come before the editor forms (pass 28), so the forms write fields that play already reads, as pass 22 did for passes 18 to 21.

**Only what the game plays is purchasable.** A DYO purchase of something the game has no rule for (OBA, Air Support, fortifications, boats, gliders, horses) is refused with its backlog row, never accepted and then ignored.

**Missing values are manufactured, never blocking.** Counter values no registered source prints (for example, Axis Minor or Japanese counters for their Heat of Battle exceptions) are manufactured under R0.3 on sheet MFG, as before.

## 2. The passes

### Pass 23: Per-side views and hidden setup

**Purpose:** a side sees only what it may see. **After:** pass 22. **From:** backlog sections 27 (hidden setup per side), 29 (HIP by SSR, the non-OB "?"), and 31 (Control left undeclared).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 23.1 A side's view | The Play page's perspective becomes a view: a side's view hides the enemy's concealed and HIP units' identities, the contents of "?" stacks, and undeclared Control; the adjudicator view shows all. A hand-over screen between sides (user answer, 2026-09-30) blanks the map and panels until the next side confirms. | A12.1, A12.3, A26.15 | 0:50 |
| 23.2 Hidden sequential setup | In a sequential setup, the side setting up second does not see the first side's placements until both have set up, except what the card places in view. | A2.9 | 0:35 |
| 23.3 HIP by SSR | An SSR token names the units that set up HIP; they are recorded hidden and revealed by the A12.3 triggers already used for concealment loss. | A12.3, A12.33, A12.34 | 0:40 |
| 23.4 The non-OB "?" | After both sides have set up, each side places "?" on units out of the enemy's LOS or at least 17 hexes away, checked against the board's LOS. | A12.12 | 0:25 |
| | Overhead | | 1:15 |
| | **Pass 23 total** (build 2:30) | | **3:45** |

### Pass 24: Control of Locations and more VP

**Purpose:** Victory Conditions count everything A26 counts. **After:** pass 23 (undeclared Control needs the side's view). **From:** backlog section 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 24.1 Control of Locations | Control kept per Location (upper levels, cellars), a building's Control from its Locations; Control by Mopping Up. | A26.1, A26.12, A12.153 | 0:50 |
| 24.2 Gun and vehicle VP | A Gun 2 VP; a vehicle 1 plus its MA and AF values; their CVP when eliminated or captured. | A26.212, A26.22 | 0:30 |
| 24.3 Start Control edge cases | A building partly in one side's area and partly on a board only that side sets up on; areas with no board on a multi-board card. | A26.11 | 0:15 |
| 24.4 Vehicles' temporary Control and the Control cache | A vehicle's temporary Control of its hex; the Control fold cached by revision. | A26.12 | 0:25 |
| | Overhead | | 1:15 |
| | **Pass 24 total** (build 2:00) | | **3:15** |

Control forfeited to a Kindled Fire (A26.16) waits for Fire spread (section 27) and stays in the backlog.

### Pass 25: Entry and exit

**Purpose:** units enter and leave the map as A2.5 and A2.6 allow. **After:** pass 24 (exits by captors count double CVP). **From:** backlog sections 30 and 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 25.1 Entry by advance | An offboard unit capable of movement may enter by advance in its APh. | A2.5, A4.7 | 0:25 |
| 25.2 Blocked and delayed entry | The four-hex radius a Game Turn later; rubble or Blaze cutting an entry hex off; never across a river. | A2.5 | 0:35 |
| 25.3 Entry through the full movement step | Entering a hex with concealed enemy units, Residual FP, or a Fire Lane; Bypass, Minimum Move, SMOKE, or a DC at entry. | A2.51, A12.15, A8.22, A4.3, A4.134 | 0:40 |
| 25.4 Offboard actions | An offboard squad's Deployment attempt in its RPh with a leader; several entry areas for one OB line; a Balance counter in an entering group. | A2.52, A2.5 | 0:20 |
| 25.5 Exits | Leaving in the APh, by Bypass with its extra MF, and at the road rate; a Guard leaving with prisoners, and captured units exited for double Exit VP. | A2.6, A20.53, A26.23, A26.222 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 25 total** (build 2:30) | | **3:45** |

### Pass 26: Vehicles and Guns on a card

**Purpose:** a card may field vehicles and Guns from setup to exit. **After:** pass 25 (entry and exit rules). **From:** backlog sections 29, 30, and 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 26.1 Vehicles entering | Vehicles enter from off board in Motion, loaded with their Passengers; Guns enter limbered and towed. | A2.52, D2.4, C10.1 | 0:50 |
| 26.2 Guns at setup | A Gun and its manning crew or HS set up together; the crew stacks as its own size. | A5.5, C10 | 0:25 |
| 26.3 Crews leaving with their Gun | A crew with its Gun leaves the map; the Exit VP count the Gun. | A2.6, C10.3, A26.23 | 0:20 |
| 26.4 A test card with armor | A manufactured test card (R0.3) fielding a halftrack, a tank, and a Gun, played end to end by the table player. | R0.3 | 0:40 |
| | Overhead | | 1:15 |
| | **Pass 26 total** (build 2:15) | | **3:30** |

### Pass 27: Heat of Battle, Leader Creation, and berserk gaps

**Purpose:** the recorded gaps in A15 close. **After:** pass 26 (charges at vehicles need vehicles on a card). **From:** the backlog rows on berserk charges, Heat of Battle, and owner's options (sections 1, 11, 20 to 22, and 25).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 27.1 Nationality exceptions | The Axis Minor and Japanese Heat of Battle and Leader Creation exceptions, with manufactured counters where no source prints them (R0.3). | A15 table notes, p. 83; A25 | 0:40 |
| 27.2 The berserk route | A berserk charge's route counting Bypass, stairwells and upper levels, and a charge at a vehicle. | A15.431, A4.3, B23.4, A15.43 | 0:50 |
| 27.3 Berserk contact | A charge into concealed units draws Dummies by Random Selection (A.9); a berserk OVR's CC against a lone SMC resolved in the MPh; the berserk leader's companions' TCs in the CC record. | A.9, A12.15, A4.152, A15.432, A20.24 | 0:40 |
| 27.4 Owner's options mid-attack | A DC's Battle Hardening and Unlikely Kill options, and Spraying Fire's second Location or a Fire Lane after a choice, resumed. | A15.3, A7.309, A9.5, A9.22 | 0:20 |
| | Overhead | | 1:15 |
| | **Pass 27 total** (build 2:30) | | **3:45** |

### Pass 28: The card editor's forms and map picking

**Purpose:** a card is made without editing JSON. **After:** pass 27, so the forms write every field play reads. **From:** backlog section 32.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 28.1 Forms for the OB, SSRs, and Victory Conditions | A group's name, ELR, areas, and counter lines; an SSR's text, status, and tokens; an outcome's winner and conditions; each as a form, the JSON kept as a view. | A19.1, A26, R17.1 to R17.13 | 1:00 |
| 28.2 Picking on the map | Boards composed or taken from a saved map; setup, entry, and exit areas picked on the map. | R17.8, R20.5, R21.5 | 0:50 |
| 28.3 Checks at edit time | Areas and hexrows checked against the chosen boards as the card is edited; plain messages for bad JSON and board text. | R19.3 | 0:25 |
| 28.4 Card management | Renaming a user card; a confirmation before deleting; a warning when a live game uses the card (a game-to-card index); the Guards card's stale SSR 3 note revised. | R22.2 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 28 total** (build 2:45) | | **4:00** |

### Pass 29: DYO purchase I: Infantry, leaders, and SW

**Purpose:** Chapter H makes a card. **After:** pass 28 (a DYO card opens in the editor's forms). **From:** backlog section 27.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 29.1 The DYO setup and roster | A DYO page: German and Russian (user answer, 2026-09-30), the date, boards, and the points each side spends; the Roster, with the counter limits and purchase mechanics. | H1.1 to H1.14 | 0:45 |
| 29.2 Infantry purchase | Squads and crews by BPV and MPV, with Assault Engineers, Sappers, Commandos, and MOL capabilities where the game plays them; the ELR Chart and SAN. | H1.2 to H1.29 | 0:50 |
| 29.3 Bonus Infantry and leaders | The second Infantry purchase and bonus Infantry; leader quality and the Leader Exchange DR. | H1.7 to H1.74, H1.8 to H1.82 | 0:40 |
| 29.4 SW allotment and the card | SW allotted by ratio; the purchase written as a user card (sides, ELR, SAN, OB groups) and opened in the editor. | H1.83, H1.84 | 0:45 |
| | Overhead | | 1:15 |
| | **Pass 29 total** (build 3:00) | | **4:15** |

### Pass 30: DYO purchase II: ordnance, vehicles, and conditions

**Purpose:** the DYO purchase covers what the game plays beyond Infantry. **After:** pass 29. **From:** backlog section 27.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 30.1 Ordnance and vehicles | Guns and vehicles by BPV with the Availability DR and RF; optional armament and Armor Leaders where the game plays them. | H1.3, H1.4 to H1.43 | 1:00 |
| 30.2 DYO conditions | The DYO Weather, EC, and NVR tables written to the card's SSR tokens. | E1.11, E3 | 0:40 |
| 30.3 What is not played | OBA, Air Support, fortifications, boats, gliders, horses, and OP tanks refused with their backlog rows. | H1.44 to H1.6 | 0:20 |
| 30.4 End to end | A DYO game bought, saved, started, and played to its end by the table player. | | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 30 total** (build 2:30) | | **3:45** |

## 3. Duration report

The estimates keep the Scenario Card Games Plan's basis: 1:15 of overhead per pass (reading and rulings, the two reviews and their fixes, the documents, the full local suite, and the merge gate). Actuals have run well under plan: pass 20 took 2:17 against 3:00, pass 21 2:22 against 3:45, and pass 22 2:18 against 3:30. The estimates are not cut for that, because passes 26, 29, and 30 reach into areas (vehicles from off board, Chapter H) the recent passes did not.

| Pass | Title | Tasks | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| 23 | Per-side views and hidden setup | 4 | 2:30 | 3:45 | 2:38 to 4:53 |
| 24 | Control of Locations and more VP | 4 | 2:00 | 3:15 | 2:17 to 4:14 |
| 25 | Entry and exit | 5 | 2:30 | 3:45 | 2:38 to 4:53 |
| 26 | Vehicles and Guns on a card | 4 | 2:15 | 3:30 | 2:27 to 4:33 |
| 27 | Heat of Battle, Leader Creation, and berserk gaps | 4 | 2:30 | 3:45 | 2:38 to 4:53 |
| 28 | The card editor's forms and map picking | 4 | 2:45 | 4:00 | 2:48 to 5:12 |
| 29 | DYO purchase I: Infantry, leaders, and SW | 4 | 3:00 | 4:15 | 2:59 to 5:32 |
| 30 | DYO purchase II: ordnance, vehicles, and conditions | 4 | 2:30 | 3:45 | 2:38 to 4:53 |
| | **All passes** | **33** | **20:00** | **30:00** | **21:00 to 39:00** |

About 30:00 of working time, 4 working days of 7.5 hours. At the recent rate (about 65 % of the estimate) it would be nearer 20:00.

**Order.** The passes run 23 to 30. The groups can be separated: group 1 alone is passes 23, 24, 26, and 28 (pass 26 then takes pass 25's entry tasks it needs); group 2 alone is passes 25 and 27; group 3 needs pass 28 only for opening a DYO card in forms and could follow pass 22 directly with the JSON editor.

**Risks.** Pass 23 changes what every Play page panel shows; the page tests read the adjudicator's view and may need a side's view added throughout. Pass 26 depends on vehicle movement from off board, which the ordnance and vehicle passes did not build; if Motion and Passengers at entry need more of Chapter D, the pass grows. Pass 29 needs the catalog's BPV for every purchasable counter; counters the catalog lacks are manufactured (R0.3), which may add catalog versions and package re-pins as pass 17 did.

## 4. Left out

These stay in the backlog: Fire spread and Kindling (so Control forfeited to a Kindled Fire, and EC and wind as an SSR the game reads), Sewer Movement and foxholes, Battlefield Integrity, the Factory and Fanaticism SSRs, OBA and Air Support, fortifications, the national rules of A25 for nationalities without counters, Chapter G (the Pacific), and more built-in cards.

## 5. Questions for the user

**Answered by the user, 2026-09-30:**

1. **Hidden setup in a hot-seat game (pass 23): a hand-over screen.** The Studio blanks the map and the side panels between sides until the next side confirms it is at the screen; there is no separate tab per side.
2. **Scope of group 2: the berserk gaps stay.** Tasks 27.2 and 27.3 remain in pass 27.
3. **DYO nationalities (passes 29 and 30): German and Russian first.** The DYO page offers the two nationalities the cards use; the other nationalities the catalog carries, with manufactured counters where needed, go to the backlog when pass 29 is built.
4. **Order: as numbered.** The passes run 23 to 30 (card play, the deferred rules, then DYO, interleaved as section 2 lists them), so each pass builds on the one before and the editor forms (pass 28) follow the rules they edit.
5. **Autonomy: one pass at a time.** Each pass starts only on the user's go-ahead and stops after its merge, with its times reported; a rule question that changes a pass's scope, or a failure that needs a design change, still stops the pass. Passes 26, 29, and 30 reach into areas not built before (vehicles from off board, Chapter H), which is why they are not run unattended.

## 6. Rulings

Each pass adds its rulings to the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5, R23.1 onward, subject to the referee's review.
