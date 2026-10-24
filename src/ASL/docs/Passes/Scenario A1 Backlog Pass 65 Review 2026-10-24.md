# Scenario A1 Backlog Pass 65: Passengers, Riders, and crews

The review of pass 65, written on 2026-10-24 after the five reviews, their four rounds of fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 65 Design](<ASL Unit Backlog Pass 65 Design.md>); its sections 7 to 17 say what each increment built, its section 18 holds the reviews, their fixes, and the Studio check, and its sections 19 and 20 the pass as built and the gate.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-65`, cut from d0a8f4cd (the head of pull request 33, merged into main as 7040c768 with the same tree): the frame (7306e834), eleven increments in eleven commits (0ccb9f4b, 7baee874, c7ece170, 1edd653c, ce468692, 49be757b, 3bd63d52, 9b118ad3, 92ea61dc, c01a1eba, 17820400), the reviews (a0906fbf), four rounds of fixes (fbca6ac1, adf1081a, 4e45b63a, ca0e14fa), and these documents. Everything was done under the user's word of 2026-10-23, "start on Pass 65 without my intervention. Keep meticulous notes as you go", renewed on 2026-10-24 ("Proceed without my intervention."). The rulings R65.1 to R65.33 are decided under that word and open to the user's veto, as are the departures and the passes given to the rows left open. **The gate has not been run:** under the standing rules the word ends at the gate, and the gate, the push, the merge, and any closing commit are each the user's to start. The gate's strict stage passed on 17820400 (0 warnings, 0 errors); the fix rounds' strict builds of the test projects they touched each succeeded.

## What departs from the plan's entry and the frame

- **The pass is more than twice the plan's size** (design section 2; ruling R65.1). The plan names 63 rows, nine tasks, and 7:00 of building; the inventory named 83 rows, and the frame's estimate from the code was 15:35 of building, 16:50 in all, since no rule reached a Passenger at all before the pass. It stayed one pass, in ten increments and an eleventh (2k) for what 2b and 2h left.
- **The catalog changed** to `asl-scenario-a1` 1.18.0 with vocabulary asl@1.19.0 (ruling R65.3): the chart's eight vehicle crews, a vehicle class on the 17 vehicles, the Bren carrier's Passenger capacity 5 to 4, and the red CS# trait, which no listed vehicle prints. The four rule packages were pinned again. The catalog checks accept an older minor version (ruling R65.4); a game recorded at 1.17.0 replays and fires.
- **The prompt was wrong** on replay after a catalog bump: a recorded catalog version locks no replay. Its line is corrected in place (design section 5).
- **Six rulings were made after the frame:** R65.26 (D5.311 and A7.308 to increment 2k), R65.27 (a Passenger's Prep Fire marks its vehicle, D.3), R65.28 and R65.29 (a crew that mans no Gun is an ordinary Infantry target and fights in CC, lifting rulings R8.3 and R24.3 for it), R65.30 (a scrounged MG is the LMG, assembled, amending R65.23); and at the reviews R65.31 (D5.311's broken crew), R65.32 (A21.22's squad exchange not built), and R65.33 (Scrounging takes no leadership, revised at fix Round B: A.10's ∆).
- **The frame's section 4 was wrong on one point:** an Unconfirmed Kill rightly has no Crew Survival (C7.42), as the second referee showed.
- **Moved on** (ruling R65.24): unloading in Bypass and fire from a CAFP to pass 100; an unarmored Target Facing under D5.311 to pass 105; A11.52's capture of Passengers, rearming with scrounged small arms, and captured vehicles' VP to pass 75; CC against a wreck and a Carrier HS recombining without a TC to pass 70; Cavalry and motorcycle Riders, a Rider's PP against a towed Gun's ammunition, and Bailing Out in Bypass to the backlog.
- **Left out, with a backlog row each (section 71.1 of the backlog):**
  - Passengers as targets (design 8.1): Heat of Battle, leadership, and LLMC for PRC; A9.74's dr for a PRC's SW; PRC Survival after an Unconfirmed Kill; an armed unarmored vehicle's crew under a Specific Collateral Attack.
  - Passenger action (design 9.1): CC with PRC and D6.651's reach, decided in Rules and not wired into Play; a vehicle and its Passengers in one FG; a Passenger's Bounding First Fire and DC; D.3's exact reading (ruling R65.27).
  - Loading (design 10.1): D6.5's interleaving of the interim move; no CC counter on an unload into an enemy Location; Ordnance First Fire at (un)loaders; the Road Bonus above four MF; an OVR after an unload losing the interim stack.
  - Riders (design 11.1): Riders in an OVR; a pin or break by anything but the IFT; a friendly Vehicular-Sized Entrance and streams; the Bail Out NMC's Heat of Battle, leadership, Wound Severity, and ELR; an entry from off board.
  - Carriers (design 12.1): an elite Carrier HS; D6.82's "specified as towing ordnance".
  - The crew's acts (design 13.1): D5.43's failure of a re-crewing; MG Removal of a heavier MG or a mortar; an Abandonment in Motion; "own crew" read from the id; unhooking a towed Gun.
  - The TC and Survival (design 14.1): the red CS# on the IFT's Vehicle line; D9.3's exception for units leaving a vehicle.
  - Captured vehicles (design 15.1): entering at capture in the CCPh; the one-man turret of D5.343; the start dr in a Motion attempt; A21.21's "removed" SMC captor riding as the crew.
  - Wrecks (design 16.1): the IFT at a wreck of the other side or beside other vehicles, Infantry Target Type and mortar hits at a wreck; the Random Selection of the scrounged SW; the American exemption, a mortar MA's dm form, a Disabled AAMG.
  - 2k (design 17.1): A7.308 on the ordnance Infantry Target Type and the mortar; D5.311's ELR, Heat of Battle, and a crew attacked again once broken.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 60's narrower check as the model: the section below.

**Departures of sections 7.1 to 17.1 that the reviews closed,** and so have no row: a unit the plan creates named by its raw id in its proposal (2f, 2g), now by its counter; the vehicle CC panel's and the ordnance and FT/MOL lists' vehicle choices not hex-first (2i, 2j), now hex-first; D5.311's crew placed beneath at once (2k), now kept inside until the RtPh (ruling R65.31); the Armor Leader not carried on Survival (2g), now carried; the unload proposal's promise of a CC counter (2d), now worded.

## What was built, by task

- **2a, part of 65.6: the foundations and the vehicle crew** (design section 7; A1.123, D5.1, D6.81). `ScenarioA1Definitions.IsCrew` for both crew kinds at every site; catalog 1.18.0; the relaxed catalog checks; `GamePlanner.LeavingCrew` places the nationality's vehicle crew; `ObjectIndex` holds each vehicle's PRC.
- **2b, 65.1: Passengers and Riders as targets** (design section 8; D.6, D.8, D.8A, D.8B, D5.31, D6.61 to D6.66, D6.7, D6.71, D6.9, D9.1, A7.211). The fact `prc`; Vulnerability (`ScenarioA1PrcRules`); the General and Specific Collateral Attacks on PRC; D5.31's CE DRM not added to a positive TEM or Height Advantage, lifting ruling R25.6 for the IFT; Survival for each Passenger and Rider.
- **2c, 65.2: Passenger action** (design section 9; D6.1, D6.31, D6.63 to D6.65, D6.72, D7.11, D7.16, D.3). Mounted Fire, passing a SW, rout beneath a Stopped vehicle, a leader aboard directing his vehicle's Passengers, Passengers in an OVR.
- **2d, 65.3: loading and unloading completed** (design section 10; D6.4, D6.5, D5.43, A4.6, A4.42, A7.821, D2.401). FFNAM and FFMO on (un)loaders; a load that fails; boarding at the end of a move with a leader's IPC; a SW left aboard; four MF and no Double Time after unloading; the interim move; record lines and the move-vehicle schema.
- **2e, 65.4: Riders and Bailing Out** (design section 11; D6.2 to D6.24, B13.43). Boarding as Riders by class and date; the terrain and turret Bail Out; a Rider's fire; the event `rider-bailed-out`.
- **2f, 65.5: Carriers** (design section 12; D6.8 to D6.84). Never BU, always CE; the inherent 2-4-7 HS; its quicker unload; 4 Passenger PP and 8 Rider PP; its 2 VP counted once.
- **2g, 65.6: the crew's own acts** (design section 13; D5.41, D5.411, D5.42, D5.8, D6.631, D3.43). Voluntary Abandonment with self-destruction and MG Removal; re-crewing by a containment role of its own, with the Armor Leader's return; a leaving crew's fire counter.
- **2h, 65.7: the TC, Crew Survival, and Brew Up** (design section 14; D5.5, D5.6, D5.7, D8.11, D8.22, D9.2, C1.55). The TC's two causes; one Crew Survival member over every path; Hazardous Movement for survivors; the red CS#'s -1.
- **2i, 65.8: captured vehicles** (design section 15; A21.2, A21.21, A21.22, D5.343, A26.12). Capture of an Abandoned AFV; Temporary Drivers and crews with their penalties; a SMC crew; Control.
- **2j, 65.9: wrecks and Scrounging** (design section 16; D10.1, D10.5 to D10.52). A wreck attacked as its vehicle; Scrounging.
- **2k: what 2b and 2h left** (design section 17; A1.123, A7.308, D5.311). A crew that mans no Gun as a target and in CC; A7.308's Random Selection on the IFT; D5.311's crew.

## The reviews

Five read-only agents in one message on 17820400: two referees (Passengers, 65.1 to 65.3 and 2k; and Riders to wrecks, 65.4 to 65.9 and 2a), a table player who read the 26 games' records, a UI and Blazor review, and a Rules boundary and memory review. Their briefs are `brief_reviews.md` in `code-builders/pass-history/pass-65-passengers-riders-crews`; each finding was judged by the session against its page and the code, and the findings as reported are `fixes_referee1.md`, `fixes_referee2.md`, `fixes_table.md`, `fixes_ui.md`, and `fixes_boundary.md` in the same folder. Four agents fixed them in four rounds, one commit each (design section 18.2).

**No BLOCKER.** **Eight MAJOR findings, all fixed:**

- **A loader eliminated with a vehicle its boarding's fire destroyed** (first referee 1): it is back on foot (D5.43 EX).
- **Riders spared a Specific Collateral Attack when the hit Shocks or Stuns** (first referee 2): a Shock, a Stun, or an Abandonment stops only the crew's attack (D.8 and its table's note 1).
- **Hazardous Movement's -2 and Mud's cushion reaching PRC** (first referee 3): kept off them (A4.62, D.6, D.8B, D5.31).
- **A Rider firing at an eighth in the AFPh** (first referee 4): a quarter (D6.63).
- **Riders not Bailing Out when a fire turns the turret** (second referee 1): every turret turn, by an ordnance or Canister shot, CMG fire, or a Motion attempt, makes them Bail Out (D6.21).
- **The crew-act and Scrounging drafts never cleared** (UI 1; boundary 20): cleared on commit and on a change of game or view.
- **The "Leave aboard" choices naming a SW with no hex or holder** (UI 2): "[F8]: LMG of 4-6-7 squad G1".
- **Five projector arms deciding rule conditions inline,** duplicating Rules' bars with weaker checks, and a Blaze id spelled out in Units (boundary 1): Rules verdicts built from the planner's own bars (`ScenarioA1VehicleProjection`), one Blaze name.

**Fixed, the other findings:**

- **Rules, Passengers** (first referee 5 to 11): a Shocked AFV's Passengers spared; Air Bursts reach an OT AFV's BU Passengers; the crew's reduced CE DRM from above; D5.311's broken crew under ruling R65.31; an Abandonment stops only the crew's Specific Collateral Attack; open PRC claim Height Advantage apart; the OVR's share by vehicle class.
- **Rules, Riders to wrecks** (second referee 2 to 9, 11, 13, 14): a Carrier's MG Removal; a leaving crew's Stun +1 and Armor Leader on the TC, Survival, and Carrier-unload paths; survivors spend their MF (`ended`); the TC's second cause not on a MG's To Kill and after a passed Possible Shock; the Stun +1 counted apart for Crew Survival; an Abandoned AFV's absent crew not attacked; a Shocked or Stunned crew may not Abandon; Riders may not board in terrain that bars them; a Wounded SMC Temporary crew Wounded again is eliminated; a broken Rider never pinned; a Rider needs a Morale Level. Finding 10 was not changed, rightly (ruling R65.33).
- **Into Rules** (boundary 2 to 6, 9 to 11, 13 to 18; second referee 12): Passengers' exposure, "moved", the OVR's Passengers, the aboard rout, the own crew, "fired", "turreted", the Bail Out Location, the capture attempt's phase, a SW kept aboard; "captured" from `UnitInstance.CapturedFrom`; D5.42's Player Turn from `GameState.AbandonedThisPlayerTurn`; the event-history scans per plan, `CrewInside`, `KeepPassengers`, and the fire group through the index; absent false flags not written.
- **The page and the texts** (UI 3 to 12; table player 1 to 6, 8, 10 to 13, 15): no crew inside offered as a firer; a just-wrecked target not kept; disabled controls say why; `@key` on the new lists; the Rider box for Personnel only; the page's pre-filters through planner members over Rules verdicts; record lines for a rout beneath, a Carrier Abandoned by its HS's unload, a failed load, the PRC on the IFT line, a vehicle CC round's arithmetic, and the Bail Out's dice; roll words; the capture texts; `PlayPagePass65Tests` (seven page tests).
- **The two known faults:** a unit the plan creates is named by its counter, never a raw id; every vehicle choice names its hex first.

**Not fixed: a row each in the backlog's section 71.2** (and three in 71.1):

| Review | What |
|---|---|
| Second referee | A re-crewing unit that leaves by `instance-moved` does not carry the Armor Leader (finding 3, in part) |
| Second referee | D6.82's "specified as towing ordnance" read from the Gun towed now (finding 15; 71.1) |
| First referee | D5.311's "treated as unarmored" for the AFV itself (finding 7; 71.1, pass 105) |
| Table player | A Bail Out NMC with no ELR replacement (finding 7; 71.1) |
| Table player | A21.22's squad exchange for a HS (finding 9; ruling R65.32) |
| Table player | The automatic capture forced and needing Good Order Infantry (finding 14) |
| Rules boundary | The Carrier's class read from the loaded catalog, not the record (finding 12; also design section 4) |
| Rules boundary | `IsBurning` per wreck per render; `CargoNotes` and a concealed vehicle's id (findings 19, 21) |
| The fixing rounds | Hazardous Movement on an AFV crew's CE line; a truck's Vehicle line at the Original DR + 1, to check; Victory's `HasInherentCrew` scan; per-render scans older than the pass |

## The games

In the session's Studio on port 6670, board 3, scripted dice; each played game's file read back. The pass's 26 games are `OWN` in `pass65.conf`; the measurement copy `p65-perf-tw` is not.

- **2a** (design 7.3): `p65-crew-tank` (played); `p65-crew-gun`.
- **2b** (design 8.3): `p65-prc-halftrack` (played); `p65-prc-halftrack-bu`, `p65-prc-truck`.
- **2c** (design 9.3): `p65-mounted-truck`, `p65-rout-beneath` (played); `p65-ovr-halftrack`.
- **2d** (design 10.3): `p65-unload-fire` (played); `p65-board-after-move`.
- **2e** (design 11.3): `p65-riders-woods` (played); `p65-riders-fire`.
- **2f** (design 12.3): `p65-carrier-unload` (played); `p65-carrier-fire`.
- **2g** (design 13.3): `p65-crew-acts` (played).
- **2h** (design 14.3): `p65-tc-mortar` (played); `p65-tc-second`, `p65-tc-second-t34`.
- **2i** (design 15.3): `p65-capture-cc` (played); `p65-capture-afv`, `p65-capture-truck`.
- **2j** (design 16.3): `p65-wreck-scrounge` (played).
- **2k** (design 17.3): `p65-crew-target` (played); `p65-crew-cc`, `p65-three-trucks`, `p65-air-burst-crew`.

After fix Round C, which made the projector refuse at replay what the planner refuses, the ten p65 games that hold play events were loaded in the Studio: each replays to its last event.

## The tests and the build

Each increment's and each fix round's new Rules, Play, and page tests were run by method only, the user's rule of 2026-10-23; the counts the notes keep are the fix rounds': Round A, Rules 25 of 25 and Play 13; Round B, Rules 17 of 17 and Play 22; Round C, Rules and Play by method, all passing after one test fixed; Round D, Rules 4 of 4, Play 10 of 10, and MapStudio by method. The pass's own classes are `ScenarioA1Pass65RulesTests`, `BacklogPass65Tests`, and `PlayPagePass65Tests`. The Rules, Play, Units, and MapStudio projects were not run whole: that is the gate's. The strict stage passed on 17820400, and each round's strict builds of its test projects succeeded.

## The replay digest

Not run on the older games after the tasks; the gate's proofs make it. Every effect that changes what an attack or a destruction recordable before the pass does is gated by a fact on the record (rulings R65.5, R65.16): `prc` and the facts beside it on attacks and shots, `riders` and `interim` on vehicle steps, `ended` on `instance-moved`, `unmanned` on a vehicle CC, and `gunCrews` on a fire attack. The Carrier's class is the one read taken from the loaded catalog, safe because no live or store game holds a Carrier. Play tests that replay an older record as it was: `AnOvrWhoseRecordNamesNoPassengerResolvesAsBefore`, `ACapturedTrucksTemporaryDriverRunsItAndAnOlderCaptureReplaysAsBefore`, and `AGameRecordedAtCatalog117Fires`; and in Rules, `ALoadRecordedWithNoUnloadingFlagIsALoad` and `TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact`.

## What is open to a veto

- **The rulings R65.1 to R65.33,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-23. Those most likely to be read again: R65.4 (the relaxed catalog checks), R65.19 (the vehicle crew placed in new plans), R65.27 (D.3), R65.28 and R65.29 (a crew that mans no Gun), R65.30 (the LMG assembled), R65.31 (D5.311's broken crew), and R65.33 (revised).
- **The departures** listed above, and the passes this document gives the rows the pass did not finish (the inventory has each row's own pass).

## What the gate will meet

- **The catalog is 1.18.0 and the four packages are pinned again** (ruling R65.3). `BOTH_SKIP` holds the catalog's identity in the state and `GameEvent.RulePackage`.
- **New facts in a record** (design section 19): `SKIP_JSON` names those written on records of kinds that existed before (`prc`, `passengers`, `interim`, `embarking`, `shocked`, `abandoned`, `immobile`, `unmanned`, `gunCrews`); the new events, actions, conditions, and the containment role Crew are the pass's own.
- **Intended differences** (design section 20): a surviving crew placed as a vehicle crew in new plans; a leaving crew's fire counter and Armor Leader in new plans; an unarmed vehicle's capture by the new event; refusals turned into plans; and `MovementState`'s three new state properties in older games with a load or unload, which the computed `PROOF_SKIP` takes.
- **The store** holds none of the pass's games until the gate copies the 26 named in `OWN`.

## The Studio check

At 08:36 to 08:41 on 2026-10-24, in my Studio on port 6670, on the build of ca0e14fa, its scope decided under the user's word (design 18.4):

- **1920,** `p65-crew-acts`: the Abandon proposal names no raw id; the focused Confirm and a real Enter key committed it: "[F10]: 1-2-7 vehicle crew G1 Abandons vehicle G2, taking its MG as the LMG (D5.41, D6.631)".
- **The Russian view** of that game names no German crew, crew inside, or Armor Leader.
- **1024:** the Scrounging section and the Rider checkbox, with their titles, inside the page, with no horizontal scroll.
- **320:** the Scrounging section, the rout beneath, and the Vehicle crews section inside the page, document width 320.
- **The measurement:** a proposal at about 1,000 ms against the base's 150 was three Studios and a profiler appending to one audit file; with my Studio alone, hand-over 268, 229, 342 ms and proposal 118, 167, 158 ms, level with the base (design section 19).
- **Not done,** as the scope allowed: the five widths in each side's view for every control; a whole Player Turn at 320; the Replay page.
