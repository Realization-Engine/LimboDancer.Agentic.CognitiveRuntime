## 10. The five reviews and the Studio check

Run on 2026-10-12 under the user's word of that day for this pass: five read-only agents in one message (the referee in two halves, the table player, the Rules boundary, the UI and Blazor review), their briefs in `Prompts/Pass 50 Review Briefs.md`. They found one BLOCKER. Each finding was judged against the code and its page; those with a contained fix were fixed, and the rest have a row in the backlog's section 58. Nothing was put to the user first; every fix below is open to the user's veto.

### 10.1 What departs from the plan's entry and the frame, first

Each was decided under the user's word of 2026-10-12, and each has a backlog row (section 58.1).

- **Task 50.4, Gun Duels (C2.2401), is not built** (section 6, F5).
- **Task 50.5, MG fire against armor (A9.6, A9.61, A9.611, D3.54), is not built** (section 9.1). The frame placed it in increment 2c.
- **A BMG fires only inside the VCA;** the hull's pivot for a shot is not built, against the answer to F4 (section 7.1).
- **A turret turned without fire at the end of a fire phase (C3.22) is not built,** against the answer to F6 (section 8.1).
- **"One hex for all weapons" (D3.51's last sentence) is not built.** Section 3 placed it in 50.2 and section 7.1 did not list it; the referee found it. A tank may Bounding First Fire one weapon in one hex and another in the next.
- **A CMG fired at another target does not lose the MA's Acquisition (D1.82)** (section 7.1).
- **A declared higher MP expenditure (D2.18); the TCA turned with a Start MP, or in woods, a building, or rubble; a Motion firer of the side that is not phasing; the step bar of a TI vehicle** (section 8.1).
- **A5.132 and OVR Prevention (C5.64)** (section 6, F1).
- **Defensive First Fire at a vehicle entering the firer's hex, a SW at a vehicle in its own Location, and everything of Bypass** in the same-hex shot and the turret's turn (section 9.1; the reviews).
- **No Play test for task 50.6:** the setup refuses a vehicle in an enemy vehicle's Location. Two Studio games and a Rules test of six cases hold it.
- **No real key was pressed in the Studio check.** The pane was not on screen, and the harness sends keys only to a pane that draws; the controls were driven by their click handlers and each was checked for a label, a title, and a reason when disabled.

### 10.2 Fixed

| Review | What was wrong | What was done |
|---|---|---|
| Table player 1; referee, second half, 2 (BLOCKER) | An Acquisition DRM was taken in Bounding First Fire, which C6.55 (p. 175) gives to a Stabilized Gun alone; the old Case N line was reached by the new shot, and it turned one recorded miss into a hit. | No Case N for a Bounding First Fire shot or a Motion firer's, in both calculators; what such a shot acquires starts from nothing (ruling R50.13). The games recorded again. |
| Referee, second half, 3; table player 3 | Case L was given to a Motion firer at Infantry. | Not when the firer is in Motion or has not stopped (C6.3, p. 174), as the Vehicle Target Type already had it. |
| Referee, first half, 3; second half, 1 | A Bounding Fire counter a MG placed let the MA fire twice with no MP between. | The MP between shots is waived only when the MA's own ROF is spent. |
| Referee, first half, 1 | A tank that had Overrun could fire its MGs and MA apart from it. | An OVR is written into the vehicle's shots of the MPh, and a Bounding Fire counter beside a shot of the MA stops the MA unless it kept its ROF (`BoundingFireSpent`). |
| Referee, first half, 2 | A First Fire counter a MG placed stopped a MA that had kept its ROF. | The counter is not read as the MA's while the MA's last shot of the phase kept its ROF. Across the MPh and the DFPh: backlog. |
| Referee, first half, 5 | An Abandoned tank fired its MGs and repaired its weapons. | Refused in both (D5.41). |
| Referee, first half, 7 | The MA paid nothing for a turret its CMG turned after the MA's own first shot. | The MA pays on its first shot after the CMG's turn, whenever that comes. |
| Referee, first half, 8 | The MGs were barred from the AFPh after a MA-only Bounding First Fire that left no counter. | Allowed, as D3.31 has it (`OtherFirePhase`). |
| Referee, second half, 4, 9, 10 | The same-hex shot and the turret turned with an MP read nothing of Bypass; a maintained Case A was added to Case E. | A same-hex shot by or at a vehicle in Bypass and a turret turned in Bypass are refused with the reason; no maintained Case A within the hex (C5.51). |
| Referee, second half, 7 | A turret turned with an MP kept the MA's Acquisition. | It is a CA change for C6.5. |
| Rules boundary 1, 2, 5, 9 | The calculator did not bar a tank's MA at Infantry in its own hex; the projector took any weapon name for a repair; the page repeated the list of steps a turret turns with; the older-record leave reached records that cannot predate the pass. | Barred in the calculator; an unknown weapon name is refused by the one member; the page calls `TurretTurnsWith`; a Bounding First Fire shot and a same-hex shot at a vehicle keep their facts. |
| UI 1, 2, 3, 6 | The turret choice was never reset, was sent for vehicles that have none, and was cleared before the gate answered; the vehicle named for the MA's shot and the ticked MGs were kept across vehicles and views. | Reset on a change of vehicle, a hand-over, and a phase change; sent only for the vehicle that shows the choice; cleared when its step commits; the vehicle is sent only while the list still offers it. |
| UI 4, 5, 7; table player 12 | Two buttons were disabled with no reason; the Fire panel let a tank be proposed with no MG ticked; the MA's button showed for a MA that cannot fire. | "Propose: MG Bounding First Fire" with its note; the Fire panel waits with "Tick the BMG or the CMG that fires"; the MA's button is hidden when the MA is malfunctioned, disabled, or its crew Recalled, stunned, or shocked. |
| Table player 4, 5, 6, 7, 9, 10, 11; UI 8 | The To Hit cases showed as identifiers; the Recall was told as a condition; a malfunctioned MG was not named; the MA's Bounding First Fire was not called that and its counter not told; one repair refusal covered three causes; the Replay page called a Delay a move and a disabled weapon eliminated; three sentences misread. | Each reworded: the cases in words in the proposal and the record; "the vehicle is Recalled: buttoned up and treated as stunned until the Player Turn ends, then it must leave"; "vehicle's BMG malfunctioned (D3.7)" with its Random Selection dr; "as Bounding First Fire (D3.3)", "it may fire again after another MP", "a Bounding Fire counter: no more fire this Player Turn"; a refusal for each cause; the Replay's two words; the tank's label names each weapon's state. |
| Table player 2, 12 | The three MG games held a firer name the code no longer writes; the AFPh control differed from its game by Case A as well; the superseded copies kept their games' labels. | Every game recorded again under a new id (`p50-mg-both2`, `p50-mg-turn2`, `p50-mg-first2`, `p50-repair2`, `p50-repair-ctl2`, `p50-bff-outset2`, `p50-bff-move2`, `p50-afph2`, `p50-afph-ctl2`); the control now faces east and shows Case B alone; every older copy's label begins SUPERSEDED or NOT PLAYED. |

### 10.3 Not fixed: a row each in the backlog's section 58

| Review | What |
|---|---|
| Referee, first half, 4 | One hex for all of a vehicle's weapons (D3.51, p. 201) |
| Referee, first half, 6 | A CMG turns the turret in woods, a building, or rubble after the MA fired there (C5.11, p. 172) |
| Referee, first half, 2 | A First Fire counter a MG placed in the MPh, against a MA that kept its ROF there, in the DFPh (D3.5, A.15) |
| Referee, first half, 10 | A Recalled tank's working MGs after its counter is flipped (D5.341; ruling R7.10) |
| Referee, second half, 5 | Case J against a vehicle that has only spent Delay or Start MP (C6.1, D2.12) |
| Referee, second half, 6 | Case C in the AFPh for a vehicle that began in Motion and only stopped (C5.2; ruling R50.10) |
| Referee, second half, 11 | A bogged or immobilized vehicle's MP for a TCA change (D8.5, p. 209) |
| Rules boundary 2, 3, 4, 6, 7, 8, 10, 11 | Decisions still made in Play, Units, or the page: that the counter prints the weapon repaired; what an obstacle and a turret are for the turret's turn, with the woods-road exemption for a stop or a Delay; which facing a weapon reads; the bars of a vehicle's MGs gathered twice; the repair list's own table of conditions; three smaller copies |
| UI 7, 9 | The turret choice shows where the gate will only refuse; the MG read runs three times a vehicle in the render path |
| Table player 8 | The record does not say that a CMG turned the turret, only its Case A |
| (the pass) | No test of the Case L fix; no Play test of task 50.6; the Studio check by real keys |

The referee's second half, finding 8 (Case C from woods), is answered by ruling R50.14 and not by a row.

### 10.4 The Studio check

In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's, in game `p50-w-mph`: the vehicle panel's Delay button, the turret choice, the Bounding First Fire target, the two MG rows, the MA's button, the MGs' button and its note, each measured for anything past either edge. Nothing at any width, and the page no wider than its window. The Russian view shows none of them.
- **At 320, the Fire panel and the repair list** (`p50-w-pfph`, `p50-w-rph`): the tank as a firer, its two MG rows, the note "Tick the BMG or the CMG that fires", and the three weapons of the repair list, inside the page. The Russian view's repair list holds no German weapon.
- **A full Player Turn at 1920 by 1080** (`p50-bff-move2`, the adjudicator's view): the Start MP, the entry with the turret turned east, the MA's Bounding First Fire while moving, after the Stop MP, and after each of two Delay MP, the end of the move, and every phase to the Russian RPh.
- **A full Player Turn at 320** (`p50-walk320`): the same Start, entry with the turret, and MA shot while moving; the Stop MP; a Delay MP; the CMG's Bounding First Fire at half FP after its row was ticked (the button waits with its note until then); the end of the move; and every phase to the Russian RPh. The page stayed 320 wide at every step but one: while a proposal is open the inspector's tab row is 11 pixels wider, as pass 40 found; it has its backlog row.
- **Each side's view:** `p50-mg-both2` and `p50-repair2` were played in the German view; the others in the adjudicator's, which a vehicle's move needs for the DEFENDER's passes.
- **Not done:** real keys (section 10.1); the Replay page was not opened, though its two changed sentences are in its code; the turret choice and the MA's button were not measured with a vehicle in the target Location, which adds one more list.

## 11. As built

Six tasks planned; four built, in three commits, then the reviews' fixes in one.

| Task | Rules | Play and Units | What a player meets |
|---|---|---|---|
| 50.1 | `FirePhase`, `WithVehicleWeapons`, `OtherFirePhase`, `VehicleWeapons`, `VehicleWeaponRepairBar`, `VehicleWeaponRepairChanges`, `VehicleCrewImpaired` | `GameState.VehicleWeaponShots`; the conditions of a disabled BMG and CMG; a repair by weapon | Each weapon of a tank fires and is repaired on its own; all of them in one fire phase; a disabled MA Recalls the tank. |
| 50.2 | `VehicleWeaponFp`, `VehicleWeaponRange`, `VehicleCaseA`, `VehicleWeaponEffects`, `MaintainedCaseA`, `VehicleWeaponArcBar` | the vehicle's entry in the fire request's `weapons`; the fire record's `turretFacing` | A tank's BMG and CMG on the IFT, added together or apart; the CMG's turret turn with Case A, and the MA's shot after it. |
| 50.3 | `CaseC`, `MotionFirer`, `AcquisitionDenied`, `MphShotKind`, `BoundingShotBar`, `BoundingArcRefusal`, `DelayBar`, `TurretStepBar` | the shot kind `bounding-first-fire`; the step kind `delay`; a step's `turret` | The MA fires while the tank moves, with Cases C, C1, C2, and C4; a Delay MP; the turret turned with an MP; Case C in the AFPh. |
| 50.4 | none | none | Not built. |
| 50.5 | none | none | Not built. |
| 50.6 | `SameHexFacing`, Case E in the armor run, `SameHexBypassRefusal` | the same-Location branch of the ordnance shot | A Gun or a tank fires at a vehicle in its own hex; the colored dr gives the Target Facing. |

**New in a game's record:** on a vehicle's fire record, `vehicleFire.weapons` (each with `turn` and `maintainedCaseA`), `otherPhase`, `inWoodsOrBuilding`, and the event's `turretFacing`; on a weapon effect, `vehicleWeapon`; on an ordnance shot, the kind `bounding-first-fire` and `vehicle.weapons`, `vehicle.mpInLos`, `vehicle.boundingFirer`; on a repair, `weapon`; on a vehicle step, the kind `delay` and `turret`. Each is absent from a record made before the pass, which is checked as it was. No event type is new. In the state: `VehicleWeaponShots`, and the conditions `asl:bmg-disabled` and `asl:cmg-disabled`.

**The replay digest over the proof store** (461 games, 93,050 lines) was run four times, after each of the three build commits and after the reviews' fixes, with the pass's new properties left out of the dump: no line differs from pass 45's last digest in any of the four. The proof tool was rebuilt against the build each time and its copy compared byte for byte. The store holds no game of the pass's own, so this shows that nothing recorded before the pass replays differently, and nothing more.

**Rulings:** R50.1 to R50.14, every one decided under the user's word of 2026-10-12 and open to a veto. R50.13: no Acquisition DRM in Bounding First Fire or Motion Fire, and what such a shot acquires starts from nothing (C6.55). R50.14: the Case B part of Case C is the 2 that C5.3 names, in woods, a building, or rubble too; the chart of p. 189 can be read to make it 3 there. All fourteen are in the Passes Plan.

**Tests after the reviews' fixes:** the pass's own, 18 Rules tests in `ScenarioA1Pass50RulesTests` and 6 Play tests in `BacklogPass50Tests`; the classes the pass touches, Rules 308, Play 174; Units 409 and MapStudio 390, each project whole. The Rules and Play projects were not run whole: that is the gate's. The two test projects build with warnings as errors.

**The gate is not run.** It is the user's to start.
