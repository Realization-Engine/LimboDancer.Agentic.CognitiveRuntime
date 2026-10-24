# Increment 2a: foundations, and the vehicle crew (part of task 65.6)

Read `brief_build.md` beside this file first; it binds you. The rulings are R65.2, R65.3, R65.4, R65.19, and R65.25 of the design (section 6, F2 to F4, F18, F24). Every later increment of the pass builds on this one.

## Pages
- p. 44 (A1.123: a crew's capabilities; a vehicle crew differs only by its Strength Factor).
- p. 203 (D5.1).
- p. 204 (D5.6: where a surviving crew is placed).
- p. 206 (D6.81: a Carrier's 4 PP).
- p. 338 (the Chapter H Listings Key: the Type column and a red CS#).
- p. 695: the chart's crew column, already read on renders. Its values are in the design's R65.3 table; take them from there.

## Build

1. **The crew kind (R65.2).**
   - Add one public Rules member, `IsCrew(string? kind)`, true for `asl:crew` and `asl:vehicle-crew`, in the Rules class where kind helpers already live (find the right one; `ScenarioA1Definitions` is a candidate).
   - Replace every literal comparison with `"asl:crew"` in Rules, Play, and Units whose rule speaks of any crew. The code map's section 10 lists about 40 lines; grep for `"asl:crew"` to find them all.
   - Where a site is an infantry crew alone, keep the literal and add a one-line comment with the rule. Say in the report which sites you kept and why; I expect none.
   - Units does reference Rules, so it can call the helper.
   - Do not change the vocabulary's `extends` handling.
2. **Catalog `asl-scenario-a1` 1.18.0 (R65.3).** Add rows to the transcription CSV (`docs/ASL/SourceRegistry/CounterSheets/scenario-a1.counter-transcription.csv`) and entries to the manifest (`src/ASL/units/catalog/scenario-a1.catalog-manifest.json`), then rebuild the catalog with the builder the code map names (section 10; find how earlier passes ran it, e.g. under `code-builders/pass-history/`).
   - **The eight `asl:vehicle-crew` definitions** of the design's table, on sheet NCC, with the same attribute set as the four infantry crews (firepower, range, morale, broken morale, BPV; whatever else an `asl:crew` definition carries, as not-printed or not-in-source where the chart is silent).
   - **The vehicle class.** Add an enumeration attribute `asl:vehicle-class` to the vocabulary (`src/ASL/units/vocabulary/asl.vocab.json`, bump to 1.19.0) for `asl:vehicle`, with the members tank, tankette, td, spa, spaa, assault-gun, carrier, halftrack, armored-car, truck, other, and the rule D6.2 and the Chapter H Type column (p. 338).
     - Give each of the 17 vehicles its class: attacker-truck and defender-truck truck; attacker-halftrack halftrack; attacker-tank and defender-tank tank; campaign-british cromwell, sherman75, firefly and campaign-german pziv, panther, tiger tank; campaign-british-achilles td; campaign-british-sexton, campaign-german-wespe, campaign-german-hummel spa; campaign-german-maultier halftrack; campaign-british-bren-carrier carrier.
     - For the five non-campaign vehicles the source is the Chapter H listing's Type column, on their own sheets (VLG, VLR). For the `campaign-` ones, sheet MFG, with a note "manufactured under R0.3".
   - **The Bren carrier's Passenger capacity:** correct it to 4 (D6.81), noting the correction in its row.
   - **The red CS# trait.** Add a trait `asl:cs-red` (vocabulary) for the wreck face. Render the Chapter H listing rows of the five non-campaign vehicles (pages: the catalog design's sections 9.6 and 9.7 name them, pp. 337, 339, 355, 356) and mark the trait only where the CS# is printed red. The design's section 9.7 says both tanks' CS# are black; confirm it. For the `campaign-` vehicles, record the trait absent on sheet MFG.
   - **The synthetic catalog** gains matching synthetic vehicle crews, and the class on its vehicles if it has any.
   - **Move every pin that names catalog 1.17.0:** `LiveFire.CatalogVersion`, `code-builders/test-games/game_file.py`'s `CATALOG`, and the tests' pins. Grep for `1.17.0` in .cs, .py, and .json under src/ASL and code-builders. The game files under `src/ASL/boards` are not yours: leave them.
   - Pass 55's `repin_tests.py` shows how a bump was done. The vocabulary bump to 1.19.0 moves the identity tests (`VocabularyTests` and `GunTests`' served cases).
3. **The relaxed catalog check (R65.4).** `play.fire-catalog` (LiveFire.cs about :79 and :323, and any twin in LiveOrdnance or elsewhere: grep for `CatalogVersion`) accepts a game whose catalog is `asl-scenario-a1` at the same major version and a minor version no later than the code's.
   - Put the comparison in one small member, modelled on the vocabulary's `Serves`.
   - A Rules test is not needed if the member lives in Play; a Play test is: a 1.17.0 game fires, a 2.0.0 game is refused.
4. **One crew-placement member (R65.19).**
   - `CrewCounter` (GamePlanner.Ordnance.cs about :1281) and `AbandonEvents`' lookup (GamePlanner.Recall.cs about :242) take one member. It returns the `asl:vehicle-crew` definition of the vehicle's nationality, with kind `asl:vehicle-crew`, keeping the id form `{attempt}-{vehicle}-crew`.
   - With 1.18.0 every catalog nationality has one, so Finnish, Italian, Axis Minor, and French crews now survive.
   - The decision of which definition is a fact Play reads from the catalog, not a rule. Keep the lookup in Play, but the choice of kind and the nationality match should be one member a Rules test can reach if that is natural (your judgement; say which).
   - An older game's recorded `instance-created` keeps its infantry crew. Check by replaying an older game that recorded a surviving crew (find one among the Play tests or the test games, e.g. pass 55's or 60's) and say how you checked.
   - The Carrier's HS (R65.18) is increment 2f's: leave a clear place for it, but do not build it.
5. **The index of PRC by vehicle (R65.25).**
   - Add to `PlayStepMemo`'s `ObjectIndex` (GamePlanner.PlayStepMemo.cs about :151) the units aboard each vehicle, by role, built in the same single pass over the state.
   - Give it a reader, for example `Aboard(vehicleId)` returning the Passengers and Riders.
   - Replace the passenger code's per-unit scans where the change is local and safe: `state.Passengers(id)` called up to three times in one vehicle step (GamePlanner.Vehicles.cs about :520, :544, :566), `AboardBar`'s per-argument `Find` plus `Aboard`, and `PlanLoad`'s loop. Leave `KeepPassengers` in the projector as it is (Units cannot reach the planner's memo).
   - Extend the existing test `TheObjectIndexReadsEveryLocationAndPositionAsTheStateDoes` (BacklogPass26Tests.cs about :427) to check the new map against `state.Passengers`.

## Gating
- Item 1 changes no result: a vehicle crew did not exist before.
- Item 2 changes only what new plans write.
- Item 3 admits fire that was refused.
- Item 4 changes what a new kill or Abandonment places. Older records keep theirs because the definition is in the recorded `instance-created`.
- Item 5 is a read path.

## Tests (by method; see brief_build.md)
- **Rules:** `IsCrew` for both kinds and for a squad.
- **Play:**
  - a German tank killed with its crew surviving places `german-vehicle-crew` (kind `asl:vehicle-crew`, FP 1) and the game replays clean;
  - an Italian (or Finnish) vehicle's crew now survives;
  - a vehicle crew stacks, counts for victory, and mans a Gun as a crew does (pick the two or three `IsCrew` sites most likely to matter and test those);
  - a 1.17.0 game fires;
  - the index test above.
- **Units and MapStudio:** whatever counts or pins the catalog or vocabulary version, and the examples (grep).

## Studio
- A game on board 3, written by `games_2a.py` (use `code-builders/test-games/game_file.py`): a German PzKpfw IIIH, CE, in the open, and a Russian 45mm AT Gun with its crew in range, in the Russian DFPh or PFPh. Give the dice so the shot kills the tank and the crew survives; if scripted dice are needed, say how the earlier pass games did it.
- An Italian truck on a second board position for the Italian crew, if the catalog's sides allow it; otherwise say so.
