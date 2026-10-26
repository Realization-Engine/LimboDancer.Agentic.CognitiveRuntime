# Scenario A1 Backlog Pass 70: Infantry III: morale, Close Combat, and concealment

The review of pass 70, written on 2026-10-25 after the six reviews, their five rounds of fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 70 Design](<ASL Unit Backlog Pass 70 Design.md>); its sections 8 to 17 say what each increment built, its section 18 holds the reviews, their fixes, and the Studio check, and its sections 19 and 20 the pass as built and the gate.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-70`, cut from 25aa97fd (main, pull request 39): the frame (7d38f69d), ten increments in ten commits (fc150800, 65ebcec8, 4189b73e, cb3110fb, e7f60a75, 45d52583, 293dd0b1, 077e6de5, 9601a3b6, 3384c2b2), the reviews (37191207), five rounds of fixes (8f92714a, 62e01dbf, 03af0037, ab082dc0, 4d53f29f), the whole-pass check (9d2e364a), and these documents. Everything was done under the user's word of 2026-10-25, "Start Pass 70 (Infantry III: morale, Close Combat, and concealment). Run it without my intervention, up to the merge gate. Questions go to read-only sub-agents; each decision is a numbered ruling R70.n, open to my veto." The rulings R70.1 to R70.48 are decided under that word and open to the user's veto, as are the departures and the passes given to the rows left open. **The gate has not been run:** the word ends at the gate, and the gate, the push, the pull request, and the merge are each the user's to start. The gate's strict stage passed on 3384c2b2 and again after fix Round E (0 warnings, 0 errors each time).

## What departs from the plan's entry and the frame

- **The pass is more than four times the plan's size by the frame's estimate** (design section 2; ruling R70.1). The plan names 35 rows, ten tasks, and 7:20 of building; the inventory named 48 rows (five moved from pass 75 and eight from pass 65), and the frame's estimate from the code was 33:05 of building, 34:20 in all. It stayed one pass, in ten increments, built in 8:07 by the clock; no increment was left out.
- **Moved on** (ruling R70.2): CC against a wreck to pass 110, with the rest of D10.1; Withdrawal to another level of a building to pass 135; naming an AFV in a Location that also holds concealed or hidden units or a Dummy to pass 75 (task 75.6); the SMC crew's Stun wound in the projector to pass 75 (task 75.3); an Infantry OVR on a lone SMC revealed outside the reviewed building case to pass 75; 5/8" Dummies, Cavalry, horses, bicycles, a vehicle's secret Target Size, and the Smoke Dispenser, entrenching, a Fire, and Clearance as actions that cost a "?", Searching for minefields and Booby Traps and the Fortified Building's reveal, a rout's destination refigured, and the minefield, FFE, shellhole, entrenchment, and Blaze rout options, to the backlog with the passes that build them.
- **Fourteen rulings were made after the frame:** R70.35 (any reinforcement may enter under one non-OB "?", A12.12) and R70.36 (a hidden vehicle takes one squad-equivalent of HIP) at 3e; R70.37 (no Mandatory Fire against a stack in Bypass) and R70.38 (a forced-back Minimum Move pins nothing) at 3f; R70.39 (the Search dr and its drm public) at 3g; R70.40 (units in an enemy's Location inspect it whatever their state) at 3h, after a read-only agent read A11 in full; R70.41 to R70.47 at the reviews; R70.48 (the Casualty dr's drm recorded for the views that may know it) at fix Round E.
- **Two rulings were corrected on the page:** R70.17 before any build (A12.141 names LLTC and LLMC among the actions that cost a "?"), and R70.13's citation at 3d (the starting-building exception is A10.51's, pp. 66 to 67, not A10.532's).
- **The frame's estimate was four times the build:** 33:05 against 8:07 by the clock. Pass 65 had built at about half its frame's; this pass at about a quarter.
- **The prompt was wrong** on three lines (main's head, the user's Studio at the start, a third worktree): corrected in place (design section 5, item 4).
- **The frame's section 5, item 2 found five stale inventory notes** (A12.1, A12.14, A10.5, A12.13, A12.15): corrected.
- **Left out, with a backlog row each (section 72.1 of the backlog):**
  - morale (design 9.1, 10.1): Battle Hardening's refusal outside fire; a CE Passenger leader in an unmoved halftrack leading units on foot, in Fire; a LLMC on a side with no declared ELR; a Passenger's or Rider's Voluntary Break; the Voluntary Rout leader's own portage;
  - rout (design 11.1): a mortar's minimum range, a restricted AAMG, a VCA-only CMG, and Rear MGs in Interdiction; Fanaticism for units created after setup;
  - concealment (design 12.1): the viewer's option in the advance, in fire, at night, and on a rolling step; the removal's Sniper exception; Dummies among OB "?" reinforcements; a crew with a Gun not Emplaced; an Emplaced Gun's HIP with no setup LOS check;
  - Bypass (design 13.1): A12.151's "any other Infantry unit which legally ends its MPh";
  - Withdrawal and CC (design 15.1, 16.1, 17.1): a concealed Gun in a withdrawal's Random Selection; an Ambush Withdrawal that drops SW on the page; Carrier squads Deploying to mount; a created leader whose infiltrating MMC dies on entry; withdrawal, Infiltration, and capture on foot in the sequential round; an Ambush where a side's force is a vehicle alone; a berserk charge into a vehicle's Location; a Rider's CC against a vehicle; R70.32's counter as a map badge; the TPBF record line at PRC; Street Fighting's return over mines and Wire, and a HS's return after Reduction.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 65's as the model: the section below. A real Enter key was not exercised (the section says why).

**Departures of sections 8.1 to 17.1 that the reviews closed,** and so have no row: `CaseRead` and `UnitNames` showing stacks the Right of Inspection hides (3a), now through the planner's view and "an uninspected unit"; the Records tab's missing LLMC line (3b); the rout advice of `p70-rout-own-building` (3d); the one-unit list with no hex (3e); a forced back's "moves from [F5] to [F5]" (3f); the Search's missing Casualty dr line and its unsearched unnamed hexes (3g; ruling R70.42); `prcHeldInMelee` naming Passengers (3i), now a flag; `ChargeBarred`'s "is not built" (3i); the enemy vehicle listed as "vehicle" (3i), now by its designation.

## What was built, by task

- **3a, 70.3 and 70.10: who sees, and the Right of Inspection by LOS** (design section 8; A12.1, A12.14, A12.141, A12.16). `ScenarioA1Sight` with three purposes, memoized per state in `PlayStepMemo`; every who-sees caller through it; the actions that cost a "?"; an enemy stack out of every Good Order seer's LOS shown as a stack before play in every view.
- **3b, 70.1 part I: one Morale Check member** (design section 9; A10.2, A15.1, A17.1, A19.11, A19.12, D5.311, D6.24, D6.651). `ScenarioA1MoraleCheck` on the Fire calculator's check-only branch; the event `morale-check-resolved`; Heat of Battle, leadership, wounds, ELR, and the LLMC cascade for every MC outside Fire and Rally; Unarmed and PRC exemptions.
- **3c, 70.1 part II: Voluntary Break and Voluntary Rout** (design section 10; A10.41, A10.711, A10.62, A7.83). The action `asl.game.voluntary-break`; `withLeader` on the rout.
- **3d, 70.2: rout** (design section 11; A10.5, A10.51, A10.53, A10.532, A10.8). Vehicles and Guns as Interdictors; the starting-building exception; Fanaticism by SSR.
- **3e, 70.4 and 70.5: the owner's choices on a "?", concealment for crews and vehicles** (design section 12; A12.11, A12.12, A12.121, A12.122, A12.14, A12.2, A12.3, A12.34). Voluntary removal; the viewer's option on a move; gains by Cases J and K; road concealment and HIP for vehicles; an Emplaced Gun hidden out of Concealment Terrain; reinforcements under a non-OB "?".
- **3f, 70.8: Bypass endings and detection** (design section 13; A12.15, A12.151, A12.42, A4.134). A Minimum Move forced back; the lane kept; a broken or pinned member; an obstacle occupied; TPBF inside the hex; a vehicle's Bypass ending.
- **3g, 70.9: Searching, and Mopping Up in any building** (design section 14; A12.152, A12.153, A12.154). The action `asl.game.search`; one building read for the map; one pass over the occupied Locations.
- **3h, 70.6: Withdrawal completed, CC with a Gun's crew, a Carrier HS** (design section 15; A11.1, A11.13, A11.21, A1.32, D6.82). Known enemies only; a concealed or Dummy destination; SW dropped above the IPC; the Gun's loss on a colored 1.
- **3i, 70.7 part I: Infantry against Infantry with a vehicle** (design section 16; A11.31, A11.34, A11.4, A11.51, A11.611, A11.62, A11.71, D6.5, D6.24, A7.211). The sequential round, its Ambush, PRC in CC, R70.32's lines.
- **3j, 70.7 part II** (design section 17; A11.8, A11.52, A18.12, D5.42, A21.21, A21.22). Street Fighting; the capture of Passengers; Leader Creation against a vehicle; a captor entering at its capture.

## The reviews

Six read-only agents in one message on 3384c2b2: three referees (morale and rout; who sees and hidden information, read from the side that must not see; Close Combat), a table player who read the events of every `p70-*` game, a UI and Blazor review, and a Rules boundary and memory review. Their briefs are `brief_reviews.md` in `code-builders/pass-history/pass-70-infantry-iii`; each finding was judged by the session against its page and the code, and the findings as reported are `fixes_referee_morale.md`, `fixes_referee_sight.md`, `fixes_referee_cc.md`, `fixes_table.md`, `fixes_ui.md`, and `fixes_boundary.md` in the same folder. Five agents fixed them in five rounds, one commit each (design section 18.2).

**No BLOCKER.** **Ten MAJOR findings, all fixed:**

- **The mover could tell the viewer's option was pending** (sight 1): a pending choice gives the open window's own refusal, and the mover's page treats the wait as the DEFENDER's turn (A12.14).
- **The public `phase-changed` named Passengers anywhere on the map** (sight 2): a flag, `prcMelee`, with the projector reading A11.71 from the vehicle.
- **A LLMC outside fire told whether units under "?" are real,** and a hidden or concealed unit was named in its public record (sight 3 and morale 1, two findings): "?" units that take no check are left out of the record or the record is withheld, the dice take its visibility, and the warning says "may call for a LLMC".
- **The case read bypassed the Right of Inspection** (sight 4; boundary 16): it takes the planner's view.
- **The Mopping Up proposal showed its result before Confirm** (sight 5): always a planned roll, what it finds said at commit.
- **A Search searched only the ticked hexes** (table 1): the named hexes first, then the other Accessible hexes up to the dr's count (ruling R70.42).
- **A laden unit was offered destinations its proposal was refused for** (UI 2): its row keeps its drop boxes, destinations once enough are ticked.
- **The Search button had no `Busy`** (UI 4): it waits while a proposal is made.
- **Every historical revision built and kept a planner memo** (boundary 1): a transient memo for a history's views (`GamePlanner.HistoryViewOf`); the cold open's memory 255 MB before, 225 MB after, against main's 231.

**Fixed, the other findings:**

- **Hidden information** (sight 6 to 8, 10; CC 1; UI 14; boundary 2): R70.40 leaves out Dummies and prisoners; units aboard never the top counter; "an uninspected unit"; A12.151's loss needs a real concealed enemy; an Ambush Withdrawal always a planned roll; the hand-over line names no leader; the view skips the Location read when nothing is uninspected.
- **Morale and rout** (morale 2 to 5; table 3, 5; boundary 5, 6, 8, 15): the rout advice for a unit that may ignore every cover in reach, and for one with no legal step; R70.47; R70.44 kept with its reason; R70.43's refusal; the Voluntary Rout's LLMC cause; the Voluntary Break's effects, the rout planner's decisions, and the LLMC subjects into Rules; the old who-sees members deleted.
- **Close Combat** (CC 2, 3, 6, 7; boundary 11, 13, 14; table 9): R70.46; a berserk or reinforcing unit owes its attack on enemy Infantry where a vehicle stands; `CaptureStands` for cause `passengers`; SW dropped before a withdrawer's elimination; vehicle CC conditions, one "enemy Location" predicate, and D5.42's exception into Rules.
- **The boundary and memory** (boundary 3, 4, 7, 9, 10, 12, 18, 20; UI 3, 12): the empty vehicle, the gain's facts, a vehicle's and a Gun's working weapons, the seers that forfeit a "?", the shared "?" both ways, the Search's MF and TI into Rules; a crew-to-Gun map per state; `OccupiedIn(hexes)`; the road sides per state; the CC lists and Mopping Up choices kept by their keys.
- **The page and the texts** (UI 1, 5 to 11; table 2, 6, 8; the notes' known items): a one-unit list keeps its hex; the Search's order keyed on game, view, and Location; disabled controls say why; the hex said once; the CCPh's crew help; `DisplayText.Side`; "Give up "?"" in an action section; `@key`; record lines for a "?" given up, lost by an action, and placed over hidden units; "an 8-0 leader"; the LLMC line in the Records tab; one citation; the Casualty dr in the Search's and Mopping Up's lines (ruling R70.48); the forced back's line; vehicles by their designation.
- **The proof tool** (boundary 19): it writes the view path it took on its log (done by the session).

**Not fixed: a row each in the backlog's section 72.2:**

| Review | What |
|---|---|
| Referee, who sees | A move step's proposal built at proposal time tells what an entry into a "?" Location finds (finding 5, its MINOR part; `ObstacleDummiesText`) |
| Rules boundary | A side's view without a live planner (the library and `UnitNames` fallbacks) shows an out-of-LOS stack in full (finding 16, its rest) |
| Referees, morale and who sees | A withheld morale check record tells that a "?" there took part (morale 1, sight 3) |
| Referee, who sees | The DEFENDER's choice not to make a Casualty dr (finding 9; ruling R70.45) |
| Table player | A concealed or hidden enemy allowing a Voluntary Break (finding 3; ruling R70.43) |
| Fix Round B | The rout's "Known armed enemy unit" reads Unarmed Personnel as armed |
| Referee, CC | A11.13's loss after a vehicle's CC attack (finding 4); the vehicle's attack with or without its Passengers, and A11.52's +2 (finding 5; the 3j reading) |
| Rules boundary | The morale check verifier compares the whole target (finding 17, for a ruling); `ObjectIndex.Of`'s O(n^2) (finding 1's note); `"guns": []` kept (finding 20); the "vice versa" of A12.2's shared "?" (finding 10) |
| UI | Page tests of `.street-fighting-return`, the CCPh re-crew, `.cc-infiltration-drop`, and a non-card building's Mopping Up (finding 13) |
| Table player | A "?" lost by a move, an advance, a rout, Smoke, a Withdrawal, or a Bypass has no record line of its own (finding 2, its rest) |
| Fix Round E | The `casualtyDrm` raw field on a public record (ruling R70.48) |
| The build | A created leader in a sequential Infantry round not counted as having attacked (3j); a Location whose Ambush drs were rolled with no attack left (3i); the hook's gap at the repository root (Round D) |

## The games

In the session's Studio on port 6670, board 3, scripted dice; each played game's file read back. The pass's 41 games are `OWN` in `pass70.conf`; the measurement copy `p70-perf-tw` is not.

- **3a** (design 8.3): `p70-inspect-out`, `p70-inspect-seen`, `p70-transfer-seen` (played); `p70-inspect-before`, `p70-transfer-unseen`.
- **3b** (design 9.3): `p70-llmc-failure-to-rout`, `p70-interdiction-hob` (played).
- **3c** (design 10.3): `p70-voluntary-break`, `p70-voluntary-rout` (played); `p70-voluntary-break-leader` (played at the whole-pass check).
- **3d** (design 11.3): `p70-afv-interdiction` (played); `p70-rout-own-building` (played at Round B's check); `p70-fanatic-ssr` (a start only).
- **3e** (design 12.3): `p70-viewer-option`, `p70-remove-concealment`, `p70-vehicle-gain`, `p70-hidden-gun-seen` (played); `p70-hidden-gun`.
- **3f** (design 13.3): `p70-bypass-occupy-forced-back` (played); `p70-minimum-move-forced-back`, `p70-bypass-exit-forced-back`, `p70-bypass-pinned`, `p70-vbm-ends-in-bypass`.
- **3g** (design 14.3): `p70-search` (played); `p70-mop-up-hidden` (played at Round A's check), `p70-mop-up-any-building`.
- **3h** (design 15.3): `p70-gun-crew-cc` (played); `p70-withdraw-drop` (played at Round C's check), `p70-withdraw-concealed`, `p70-withdraw-hidden-gun`, `p70-carrier-hs`.
- **3i** (design 16.3): `p70-vehicle-cc-h3` (played); `p70-vehicle-cc-ambush`, `p70-vehicle-cc-prc`.
- **3j** (design 17.3): `p70-street-fighting` (played); `p70-capture-passengers`, `p70-leader-creation-vehicle`, `p70-recrew-captured`.
- **Fix Round B:** `p70-break-unarmed-adjacent` (at 320 at the whole-pass check), `p70-break-unarmed-far`, `p70-unarmed-llmc`.

At the whole-pass check `replay_p70.sh` replayed the 41 games from copies outside the live folder: every one to its last event, with no ERROR, UNREADABLE, or NOSTATE line, those recorded before the fix rounds included.

## The tests and the build

Each increment's and each fix round's new Rules, Play, and page tests were run by method only, the user's rule of 2026-10-23; the counts are in the design's sections 8 to 18 and the notes. The pass's own classes are `ScenarioA1Pass70RulesTests` (68 methods), `BacklogPass70Tests` (62), and `PlayPagePass70Tests` (12). The Rules, Play, Units, and MapStudio projects were not run whole: that is the gate's. The strict stage passed on 3384c2b2 and again on the tree after Round E; each building agent's and each round's build of MapStudio ended with exit 0.

## The replay digest

Not run on the older games after the tasks; the gate's proofs make it. Every effect that changes what a record made before the pass does is gated by a fact on the record (rulings R70.4, R70.7 to R70.10, R70.23, R70.27, R70.28, R70.30, R70.31, R70.34): `unarmed`, `aboard`, `seenByGoodOrderEnemy`, `crewBroken`, and the PRC facts on attacks; `drm` and `elr` on a Bail Out; `with` and `leader` on a rout; a forced back's `bypass`; `guns`, `rider`, `passengerOf`, `sequential`, `vehicles`, `ambusher`, `passengers`, `streetFighters`, `leaderCreation`, and `captors` in CC; `crewOf` on a Carrier crew; `prcMelee` on a CCPh's end. The view's Right of Inspection is derived and never recorded (R70.19), so the digest, which hashes states, does not see it; the sweep's view lines do. Play tests that replay an older record as it was: `ARoutWithNoLeaderIsRecordedAndReplaysAsBeforeThePass`, `AnOlderRecordedRoutPastAnEnemyAfvReplaysAsItDid`, `ASmcRidersBailOutTakesItsWoundSeverityDrAndAnOlderRecordReplaysAsItWas`, `AnOlderRecordedMoppingUpOfACardsBuildingReplaysAsItDid`, `AnOlderCcRecordThatListsNoGunsReplaysAsItDid`, `AnOlderRecordedVehicleCcSequenceKeepsItsRecordedNextAndClose`, and `AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader`; in Rules, `AFirePassengerLeaderAidsHisFellowPassengersWhenTheRecordCarriesThePassFacts`.

## What is open to a veto

- **The rulings R70.1 to R70.48,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-25. Those most likely to be read again: R70.1 (the pass kept whole), R70.16 and R70.18 (who sees, and the Right of Inspection in every view, with no marker on older records, R70.19), R70.21 (the viewer's option on a move only), R70.30 (Infantry CC with a vehicle in the vehicle sequence), R70.32 (no stored CC counter), R70.36 (a hidden vehicle's HIP price), R70.40 (inspection in an enemy's Location), R70.43 and R70.45 (the two deviations), R70.44 (bystanders' LLMC after a rout), and R70.48 (the drm on a public record).
- **The departures** listed above, and the passes this document gives the rows the pass did not finish (the inventory has each row's own pass).
- **Two readings left for the user:** A11.52's +2 on the vehicle's whole attack (backlog 72.2), and A1.32's "Guards" read as prisoners' Guards since R31.4 (the CC referee's finding 9, older than the pass).

## What the gate will meet

- **New facts in a record** (design section 19): `SKIP_JSON` names those written on records of kinds that existed before (`unarmed`, `aboard`, `seenByGoodOrderEnemy`, `vehicleMoving`, `ceInUnmovedHalftrack`, `crewBroken`, `guns`, `sequential`, `rider`, `vehicles`, `ambusher`, `passengers`, `passengerOf`, `streetFighters`, `leaderCreation`, `captors`); the properties of event records (`drm`, `elr`, `with`, `leader`, `crewOf`, `prcMelee`, `casualtyDrm`, `createdLeader`) are taken by the computed `PROOF_SKIP`; the new events and actions are the pass's own, and main writes UNREADABLE for a game that holds one.
- **Intended differences** (design section 20): the swept views of stacks out of every seer's LOS (70.10); who sees in new proposals; rout by enemy vehicles and Guns; refusals turned into plans; vehicles named by their designation; the text list's new texts.
- **The baseline is remade:** the proof tool changed at 3a and Round D, and the baseline key holds it.
- **The store** holds none of the pass's games until the gate copies the 41 named in `OWN`.

## The Studio check

At 23:11 to 23:20 on 2026-10-25, in my Studio on port 6670, on the build of 4d53f29f, its scope decided under the user's word (design 18.4):

- **Every pass game replayed** to its last event (41 games).
- **The Russian view** of `p70-inspect-out` at 1920: the hand-over screen and the Units tab name nothing of the uninspected stack but its top and a sealed count; its LMG and second squad nowhere on the page.
- **1024:** the Voluntary Break list grouped under [O10], the leader first, inside the page (document 1,009 wide), the disabled button titled with its reason; the leader's break and the LLTC that follows.
- **320:** `#propose-voluntary-break` and `#propose-rout` inside the page, document width 320.
- **The hex audit** in three phases, both views: no counter choice without a hex.
- **The measurement:** hand-over median 300 ms against main's 415, the proposal level (56 to 74 against 66 to 81 after the first); the memory after Round D 225 MB against main's 231 (design section 19).
- **Not done:** the real Enter key (the pane was hidden and its active tab a stale one of main's Studio, so the key went nowhere and nothing was written); `p70-fanatic-ssr` (no card carries the SSR); and, as the scope allowed, the five widths in each side's view for every control, a whole Player Turn at 320, and the Replay page.
