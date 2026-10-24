# Pass 65 session notes

A running record kept as the pass goes, at the user's word of 2026-10-23 ("Keep meticulous notes as you go"). Times are read from the clock. The design holds the decisions; this file holds what was done, in order, and why.

## The user's word

2026-10-23, about 22:25: "Go ahead and push and merge vehicle-crew-figure. And then start on Pass 65 without my intervention. Keep meticulous notes as you go."

- The push and the merge of `vehicle-crew-figure` are given by name.
- Pass 65 runs without the user's intervention, as passes 55 and 60 did. The word names no end; under the standing rules (section 2, the rule for a pass run without intervention) it ends at the gate: the gate, the push of the pass's branch, its merge, and a closing commit are each the user's to start.
- Questions go to read-only sub-agents; each decision is a numbered ruling R65.n, open to the user's veto.
- Unit tests (the user, 2026-10-23): only those pertaining to the pass's new code, by method; the full run at the gate.

## Timeline

- 22:25 `vehicle-crew-figure` pushed; pull request 33 opened. `how_to_reach_main.sh`: files added, so a PR and green CI. Main is not branch-protected, so GitHub auto-merge would merge at once without waiting for CI; not used. The app's CI monitor is on for PR 33; the merge is made by hand once CI is green.
- 22:27 Standing rules re-read (sections 0 to 2). Pass 60's design, build brief, and code map read for form.
- 22:29 Pass folder made. Rulebook page texts checked: every page the pass names is in `asl-gate-work/rulebook-pages` (279 pages).
- 22:30 Four read-only frame agents launched (`brief_frame.md`): the code map (14 questions, writes `codemap.md`); the rules of 65.1 to 65.3; of 65.4, 65.5, and the vehicle-crew values on p. 695; of 65.6 to 65.9.
- 22:31 PR 33's CI: one check pending.
- (Times marked "by order" below were not read from the clock when written; the clock read 22:50 after all four reports and the CI fix. A slip of section 0, point 10, corrected here. From 22:50 on every time is read.)
- By order, about 22:35 The Passengers referee (65.1 to 65.3) reported. Read pp. 51, 55, 56, 58, 74 to 76, 192, 193, 200, 203 to 207, 209. Key findings:
  - `GameState.At` leaves Passengers out of their Location (GameState.cs:445-451), so no attack reaches one, and `KeepPassengers` eliminates them with their vehicle (GameProjector.cs:895). No recorded game holds a broken, pinned, or CE Passenger, so rules about a Passenger's own state need no fact; attack resolution and vehicle destruction do.
  - Pass 60's `SpecificCollateral` fact cannot gate the new Passenger effects (records since pass 60 carry it). Proposed: a nullable PRC list on `FireVehicle` and on the ordnance `VehicleTarget`; null resolves as before.
  - Passenger CE derived from the vehicle's `ButtonedUp` (D5.33); nothing new stored.
  - Estimates: 65.1 3:00, 65.2 2:30, 65.3 1:15 (plan 1:00, 1:00, 0:40).
  - Moves proposed: unloading in Bypass and fire from a CAFP to pass 100; D5.311 unarmored Target Facing to pass 105; A11.52 capture of Passengers to pass 75.
- By order, about 22:37 The Riders, Carriers, and crew-values referee (65.4, 65.5) reported. Read pp. 128, 205 to 207, 338, 419; rendered p. 695 at 200 to 300 dpi (renders in the scratchpad's `frame/`, not committed).
  - Vehicle-crew values, confirmed on the render (FP-range-morale, broken, BPV): German 1-2-7, 7 (7); Russian 1-2-7, 7 (5); British 1-2-7, 7 (7); American 1-2-6, 7 (6); Finnish 1-2-7, 7 (7); Italian 1-2-6, 5 (5); Axis Minor 1-2-6, 5 (5); French 1-2-7, 6 (5). The four catalog infantry crews agree with the chart.
  - Hazard: `CrewCounter` finds no `asl:crew` for Finnish, Italian, Axis Minor, or French vehicles, so they get no survivors today.
  - Hazard: about 60 product-code `"asl:crew"` string comparisons would not match `asl:vehicle-crew`; proposed one Rules helper `IsCrew(kind)`.
  - Riders: `ContainmentRole.Rider` exists (GameTypes.cs:354) but Load always writes Passenger. Proposed `Riders = true` on `VehicleStepped.Load`, absent meaning Passenger.
  - Vehicle class: none in the catalog; proposed one enumeration `asl:vehicle-class` from the Chapter H Type column, Rider capacity derived (14 PP, 8 PP for a Carrier, one SMC), with the 17 vehicles classed, in the same catalog bump as the crews.
  - Carrier: the Bren carrier's passenger capacity is 5 and D6.81 says 4. Proposed an `inherent-crew` attribute naming the HS definition.
  - Estimates: 65.4 1:30, 65.5 0:50, vehicle crews 0:45.
  - Moves proposed: Cavalry and motorcycle Riders to the backlog; Bail Out in Bypass to the backlog; Rider PP and a towed Gun's ammo to the backlog; a Carrier HS's recombining without a TC to pass 70.
- By order, about 22:40 to 22:48 PR 33's CI failed (source-conformance): GunTests' pack-version case (asl@1.18.0 now served), DocumentTests' second examples count, the "crowded" layer goldens and the counter layer manifest's two crowded rows (the fixture draws every example, so it now draws `example-vehicle-crew`). My filtered runs had not reached them. **Lesson:** adding an example or bumping the vocabulary reaches every test that counts the examples, checks the pack version, or draws all examples; grep for `Examples`, `Catalog(vocabulary).Count`, `asl@1.1`, and the `crowded` fixture. Regenerated with `ASL_UNITS_UPDATE_GOLDEN=1` and `ASL_LAYER_MANIFEST_UPDATE=1`; checked that only the crowded rows changed and that the new example is what entered. Fixed in d0a8f4cd, pushed.
- By order, about 22:40 The crews, captures, and wrecks referee (65.6 to 65.9) reported. Read pp. 88, 89, 99, 165, 176, 177, 199, 200, 203, 204, 206, 208 to 211; confirmed the four main vehicle-crew values on the render.
  - Two places place a crew: `CrewCounter` (Ordnance.cs:1281) and `AbandonEvents` (Recall.cs:242), both an infantry `asl:crew`. A recorded `instance-created` keeps its definition, so older games keep their infantry crew with no new fact.
  - Crew Survival is decided in three places with three different +1 groupings (Armor 816, Area 521, Fire 2025); no CS on the IFT Vehicle-line kill, in vehicle CC, after a UK, or for a burning wreck.
  - Estimates: 65.6 1:40, 65.7 1:10, 65.8 1:40, 65.9 0:50.
  - Moves proposed: rearming with scrounged small arms to pass 75; CC against a wreck to pass 70; captured vehicles' VP to pass 75.
- By order, about 22:41 The code map was written (`codemap.md`, 14 sections). Key findings:
  - `At`, `Location`, and `Aboard` each scan the whole state; the `ObjectIndex` of PR 32 (PlayStepMemo.cs:151) indexes units by Location but has no vehicle-to-Passengers map. Proposed: add one there (the guidelines).
  - About 40 lines compare the literal `"asl:crew"`. The Rules project has no vocabulary, so the helper is a Rules member over the two kind names.
  - Load and unload records read as "X moves to ..." (PlayRecords.cs:310, ReplaySteps.cs:315); the move-vehicle action's schema omits `load`, `unload`, `delay`, `units`, and `turret` (GameActions.cs:549-566).
  - Catalog 1.17.0 pins vocabulary asl@1.3.0; the Bren carrier's passenger capacity is 5.
- 22:50 Catalog check read: `play.fire-catalog` (LiveFire.cs:79, :323) compares the version exactly, so a bump to 1.18.0 would stop every 1.17.0 game (the user's Studio games among them) from firing. Ruling R65.4 relaxes it to the vocabulary's `Serves` form. The standing rules (section 4, line 112) say a recorded catalog version locks no replay, so the prompt's line that 1.17.0 games stop replaying is wrong (design, section 5).
- 22:50 to 22:54 The design's frame written (sections 1 to 6, rulings R65.1 to R65.25); `brief_build.md` (from pass 60's, with the rule for unit tests of 2026-10-23 and the memory contracts) and `brief_2a.md`.
- 22:55 Branch `feature/asl-backlog-pass-65` cut from d0a8f4cd, the head of PR 33, before PR 33 merged: its CI was still running and the app wakes me only on a failure. d0a8f4cd is what main becomes when PR 33 merges, if main does not move; checked again before the baseline's use and before the merge.
- 22:56 `build_studio.sh` found building into MapStudio's `bin/p31b`, which the rule of 2026-10-19 forbids (a gate build copies every folder under bin into itself). Repaired to build into `$WORK/studio-6670`, registered with `outside_outputs.sh`; run at once (checklist point 4): exit 0, 1:57.
- 22:58 Frame committed, 7306e834. `pass65.conf` written (SWEEP_EXTRA: p26-visual, p40-boarding, p55-al-fire, p60-coll, p60-mixed-ce, p60-mtr-truck; BOTH_SKIP with the catalog).
- 22:59 `gate.sh 65 d0a8f4cd base` started in the background (log `asl-gate-work/base-65.log`). The 2a building agent launched on `brief_2a.md`.
- 23:05 `base` done (22:58 to 23:05): baseline 0738528e38fe2d41 of d0a8f4cd, 528 older games; text list 1,399 distinct texts, replay digest 20,913 lines in 45 s, sweep 135,936 lines in 146 s. It noted origin/main at 4702d90c (PR 33 not yet merged; nothing of main's changed since the cut).
- 23:05 `brief_2b.md` written while 2a builds.
- About 23:27 The 2a agent reported (built 22:59 to about 23:27). Its departures, judged:
  - R65.4 changes nothing visible today: `GameProjector.Start` reads the loaded catalog by name (`UnitCatalogs.For` ignores the version), so `state.Catalog.Version` is always the loaded one, and 1.17.0 games already fired. The member matters only if a loaded catalog ever differs from the code's. It relaxed all five checks (LiveFire twice, LiveRally, CloseCombatFactBuilders.CatalogBar, OrdnanceEligibility.CatalogRefusal) through one Rules member, `ScenarioA1Definitions.CatalogServes`. Accepted.
  - No Italian or Finnish vehicle exists in the catalog (all 17 are German, Russian, British), so an Italian crew's survival is tested in Rules only. Accepted.
  - Vehicle-crew class not-in-source (D5.1 gives none; A1.123's elite is the Infantry crew's); Self-Rally yes per D5.1. Accepted.
  - Red CS#: none of the five listed vehicles prints one (every span on pp. 337, 339, 355, 356 is #231f20). Agrees with the catalog design's section 9.7.
  - Three older tests asserted the infantry crew and now expect the vehicle crew (BacklogPass5Tests, BacklogPass7Tests x3): the intended change.
  - The four rule packages were re-pinned (pass 55's `repin.py`), since their case matrices hash the catalog. `BOTH_SKIP` already holds `GameEvent.RulePackage`.
  - It ran `CounterSheetCatalogBuilderTests` whole (16 methods): every method builds from the worksheet the change altered, so each asserts it. Within the rule.
- 23:2x Read the index and `AboardBar` diffs by hand: one pass over the state, `Aboard` once per distinct id, PRC by vehicle. `AboardBar` now builds the memoised index once per state instead of a scan per argument.
- 23:2x `games_2a.py` run (p65-crew-gun, p65-crew-tank). Studio check in p65-crew-tank: the T-34's AP shot (dice 4 2, 3 3, 2 2) wrecks the PzKpfw IIIH and its crew survives as `german-vehicle-crew`, kind `asl:vehicle-crew`, id `gun-94f860606b0f-dt-crew` (read back from the game file). Found and fixed: the page named it "1-2-7 vehicle-crew" (DisplayText.Kind strips the prefix only); now "1-2-7 vehicle crew", as D5.1 writes it.
- 23:3x The measurement (R65.25): `p65-perf-tw`, a copy of `dl-tractor-works-02` cut at revision 1316 (`perf_cut.py`; the user's game is not touched). The base Studio is the gate's `main-bin` build of d0a8f4cd on 6671 (launch config `map-studio-base65`, local only); the pass on 6670. Warm runs (the first of each is the cold start):
  - base: hand-over 276, 265, 355 ms; an "end the phase" proposal 135, 157, 147 ms;
  - pass 65 after 2a: hand-over 225, 245, 241 ms; the proposal 112, 137, 123 ms.
  No regression; within noise or a little faster.
- 23:35 2a committed (below).
- The four estimates sum to about 15:00 of build against the plan's 7:00 (65.1 3:00, 65.2 2:30, 65.3 1:15, 65.4 1:30, 65.5 0:50, crews 0:45, 65.6 1:40, 65.7 1:10, 65.8 1:40, 65.9 0:50).
