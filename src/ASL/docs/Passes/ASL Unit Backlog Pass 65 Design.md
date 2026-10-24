# ASL Unit Backlog Pass 65 Design

**Status:** The frame, written on 2026-10-23 under the user's word of that day ("start on Pass 65 without my intervention. Keep meticulous notes as you go"). The word names no end; under the standing rules it ends at the gate, and the gate, the push of the pass's branch, its merge, and any closing commit are each the user's to start. The frame's questions were put to four read-only agents, as the standing rules ask in this mode. Their answers were judged against the pages and the code. Each decision below is a ruling decided under that word, and each is open to the user's veto.

**Date:** 2026-10-23

**Related documents:**
- the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the entry "Pass 65: Passengers, Riders, and crews";
- the [rule inventory](<../ASL Rule Inventory A to E.md>);
- the [backlog](<../ASL Unit Backlog.md>), the rows that name pass 65;
- the [pass 60 design](<ASL Unit Backlog Pass 60 Design.md>), sections 2 to 6 and 8;
- the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>);
- the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>);
- the session's prompt, `Prompts/Pass 65 Prompt I.md`.

**The rulebook.** Three read-only agents read it for this frame, from the page texts of the working folder: pages 51, 55, 56, 58, 74 to 76, 88, 89, 99, 128, 165, 176, 177, 192, 193, 199, 200, 203 to 211, 338, and 419. The A./G. National Capabilities Chart (physical page 695) was read on renders at 200 to 300 dpi, since the text layer drops its superscripts; two agents confirmed it independently. I read the agents' answers against the inventory's rows and the code, but I did not read every page myself. Each task reads its own pages again before it builds.

**What the frame rests on.** One read-only agent mapped the code (`code-builders/pass-history/pass-65-passengers-riders-crews/codemap.md`, 14 sections). Three more read the rules of 65.1 to 65.3, of 65.4, 65.5, and the vehicle-crew values, and of 65.6 to 65.9. The session's running record is `notes.md` in the same folder.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The rows, by task and increment.
4. What is certain, and what needs a read.
5. What the frame found outside the rows.
6. The frame's questions, each with its ruling.

Then, as each is built: one section per increment, then the reviews, the Studio check, as built, and the gate.

## 2. What the pass is

Pass 65 is the units a vehicle carries and the crew it has:
- Passengers under fire and in action.
- Loading and unloading, completed.
- Riders and Bailing Out.
- Carriers.
- The vehicle crew as a counter of its own (D5.1).
- Abandonment, self-destruction, and re-crewing.
- Crew Survival after every kind of destruction.
- Captured vehicles.
- Wrecks attacked as their vehicle, and Scrounging.

**What departs from the plan's entry, first:**

- **The pass is twice the plan's size.** The plan gives 7:00 of building; the agents' estimates from the code sum to about 15:00 (section 6, F1).
  - Today no rule reaches a Passenger at all. `GameState.At` leaves them out of their Location, and `KeepPassengers` eliminates them with their vehicle, so 65.1 and 65.2 start from nothing.
  - The pass stays one pass, built in the dependency order of the table below. The captured vehicles (65.8) and the wrecks (65.9) come last, so that whatever a session's end leaves out is theirs (ruling R65.1).
- **The catalog changes,** to 1.18.0:
  - eight vehicle-crew definitions, read from the chart;
  - a vehicle class on the 17 vehicles;
  - the Bren carrier's Passenger capacity corrected from 5 to 4 (D6.81);
  - the red CS# trait (D5.7).
- **The Fire package's catalog check is relaxed.** Today it refuses any game whose catalog version is not the code's. It will accept the same catalog at an older minor version, so the games recorded at 1.17.0 can still fire (ruling R65.4).
- **A correction to the prompt.** `Pass 65 Prompt I.md` says games that name 1.17.0 stop replaying after the bump. They do not. The standing rules (section 4) say a game's recorded catalog version locks nothing: the loaded catalog of that name is read. What the bump does change is the digest's catalog identity in every state line, which the gate's `BOTH_SKIP` handles (the pass 55 lesson).
- **Moved to later passes** (ruling R65.24):
  - to pass 100: unloading in Bypass, and fire from a CAFP;
  - to pass 105: an unarmored Target Facing under D5.311;
  - to pass 75: the A11.52 capture of Passengers, rearming with scrounged small arms, and captured vehicles' VP;
  - to pass 70: CC against a wreck, and a Carrier HS recombining without a TC.
  Cavalry and motorcycle Riders, a Rider's PP against a towed Gun's ammunition, and Bailing Out in Bypass go to the backlog.

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 2a | Foundations; the crew of 65.6 | The crew-kind helper at every site; catalog 1.18.0 (vehicle crews, vehicle class, the Bren carrier, the red CS# trait); the relaxed catalog check; one crew-placement member taking the nationality's vehicle crew; the index of Passengers by vehicle. | 1:30 |
| 2b | 65.1 | Passengers and Riders as targets: General and Specific Collateral Attacks, Vulnerability, D5.31 with Height Advantage, D5.311, A7.211, A7.308's Random Selection, A7.82, Passenger morale and DM, Survival (D6.9). | 3:00 |
| 2c | 65.2 | Mounted Fire, passing a SW, rout beneath the vehicle, leaders aboard, Passengers in an OVR and in CC. | 2:30 |
| 2d | 65.3 | Loading and unloading completed: FFNAM and FFMO on the (un)loaders, boarding at the end of a move, a leader's IPC, SW left aboard, the four-MF cap and the Double Time bar, unloading into an enemy Location, D2.401; the records' own text; the action's schema. | 1:15 |
| 2e | 65.4 | Riders: boarding as a Rider, the capacity by class, the dates; D6.21 and B13.43; D6.22 and D6.23; Bailing Out (D6.24). | 1:30 |
| 2f | 65.5 | Carriers: the inherent HS, never BU, always CE, the 2 VP. | 0:50 |
| 2g | 65.6 | Voluntary Abandonment and self-destruction; re-crewing and the Armor Leader's return; MG Removal; D5.8. | 1:30 |
| 2h | 65.7 | The Immobilization TC's causes; one Crew Survival member over every path; the red CS#; Brew Up; Hazardous Movement for survivors. | 1:10 |
| 2i | 65.8 | Captured vehicles: capture of an AFV, Temporary Drivers and crews, a SMC crew, Control. | 1:40 |
| 2j | 65.9 | A wreck attacked as its vehicle; Scrounging. | 0:50 |
| | Overhead | The reviews, the Studio check, the documents (the plan's 1:15). | 1:15 |
| | **Total** | build 15:35 | **16:50** |

## 3. The rows, by task and increment

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 65.1 | D.6, D.8, D.8A, D.8B (192) | No FFNAM or FFMO on PRC aboard; General and Specific Collateral Attacks on every Vulnerable PRC | 2b |
| 65.1 | D5.31 (203) | +2 CE DRM, not cumulative with positive TEM or Height Advantage | 2b, behind the pass's fact |
| 65.1 | D5.311 (203) | Unprotected PRC: elevation, Air Bursts, an OT crew that breaks | 2b; an unarmored Target Facing to pass 105 |
| 65.1 | D6.61, D6.62, D6.66, D9.1 (205, 206, 209) | Halftrack Passengers BU or CE with the crew; no Stun; DM | 2b |
| 65.1 | D6.7, D6.71 (206) | A truck's PRC always Vulnerable; Specific Collateral after an ordnance hit | 2b |
| 65.1 | D6.9, D5.6 (204, 207) | Survival per Passenger and Rider | 2b |
| 65.1 | A7.308 (55) | Vehicles beyond the column's highest KIA#, at random | 2b |
| 65.1 | A7.211 (55) | TPBF against non-BU PRC from the same or a higher Location | 2b |
| 65.1 | A7.82, A7.821 (58) | Pinned Passengers BU; unloading pinned | 2b, 2d |
| 65.2 | D6.1, D6.63, D6.72 (204, 206) | Mounted Fire and its halving; the SW allowed | 2c |
| 65.2 | D6.31 (205) | Passing a SW | 2c |
| 65.2 | D6.1 rout (204) | Rout beneath a Stopped vehicle | 2c |
| 65.2 | D6.64, D6.65, D6.651, D3.4 (200, 206) | Leaders aboard, Armor Leaders, FG makeup | 2c |
| 65.2 | D7.11, D7.16, A11.611, A11.71 (75, 76, 207) | Passengers in an OVR and in CC | 2c; A11.52 capture to pass 75 |
| 65.3 | D6.4, D6.5, D5.43, A4.6 (51, 204, 205) | Loading and unloading, FFNAM, FFMO | 2d; Bypass to pass 100 |
| 65.4 | D6.2, D6.21, D6.22, D6.23, D6.24, B13.43 (128, 205) | Riders and Bailing Out | 2e; Cavalry and motorcycles: backlog |
| 65.5 | D6.8, D6.81, D6.82, D6.83, D6.84 (206, 207) | Carriers | 2f; recombination to pass 70 |
| 65.6 | D5.1 (203) | The vehicle-crew counter | 2a |
| 65.6 | D5.41, D5.411, D5.42, D6.631, D5.8, D3.43 (200, 204, 206) | Abandonment, self-destruction, re-crewing, MG Removal | 2g |
| 65.7 | D5.5, D5.6, D5.7, D8.11, D8.22, D9.2 (204, 208, 209) | The TC's causes, Crew Survival, the red CS#, Brew Up | 2h |
| 65.8 | A21.2, A21.21, A21.22, A26.12, D5.343 (88, 89, 99, 204) | Captured vehicles | 2i; VP to pass 75 |
| 65.9 | D10.1, D10.5, D10.51, D10.52 (210, 211) | Wrecks and Scrounging | 2j; CC at a wreck to pass 70, rearming to pass 75 |

The items passes 55 and 60 moved under the entry are placed with their task:
- with 2b: the FG of a CE halftrack, the CE DRM with Height Advantage, more vehicles than the highest KIA#, D.8A on Passengers, Riders, and an unarmored vehicle's PRC, an AFV in positive TEM under other IFT attacks, and an OT AFV's crew under Air Bursts;
- with 2c: a Passenger leader in an OVR;
- with 2g: an Armor Leader's return;
- with 2h: the Immobilization TC after a mortar's hit, and Crew Survival's +1 for a Shocked or Abandoned vehicle.

The documents step gives each row its own note and pass.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **No rule reaches a Passenger today** (codemap, sections 1, 5, 6):
  - `GameState.At` leaves units aboard out of their Location (GameState.cs:450), so no fire, ordnance shot, OVR, CC, Collateral Attack, or rout touches them;
  - `KeepPassengers` (GameProjector.cs:895) eliminates them with their vehicle;
  - every action that names one, other than eight, is refused `play.aboard`.
  So no recorded game holds a broken, pinned, or CE Passenger. Rules about a Passenger's own state need no fact; rules that change what an attack or a destruction does to a vehicle's Location do.
- **Two places place a crew,** `CrewCounter` (GamePlanner.Ordnance.cs:1281) and `AbandonEvents` (GamePlanner.Recall.cs:242). Both take the nationality's first `asl:crew`, which is an infantry crew. Finnish, Italian, Axis Minor, and French vehicles have none, so their crews never survive.
  A recorded `instance-created` keeps its kind and definition, so an older game keeps the infantry crew it placed with no new fact.
- **About 40 lines compare the literal `"asl:crew"`**, among them Stacking, Victory, Setup, Fire, Ordnance, Definitions, VehicleCloseCombat, MoppingUp, and Starshells (codemap, section 10). A unit of kind `asl:vehicle-crew` would miss every one. The Rules project has no vocabulary, so the fix is a Rules member over the two kind names.
- **Crew Survival is decided in three places** with three different +1 groupings: ArmorCalculator.cs:816, AreaCalculator.cs:521, and FireCalculator.cs:2025. Four cases have no Survival at all: the IFT Vehicle-line kill, vehicle CC, an Unconfirmed Kill, and a burning wreck.
- **The catalog's recorded version locks no replay** (the standing rules, section 4). But the Fire package refuses fire in a game of another version (`play.fire-catalog`, LiveFire.cs:79), and the catalog's identity is hashed into every state (pass 55's lesson, `BOTH_SKIP`).
- **Riders exist only as `ContainmentRole.Rider`** (GameTypes.cs:354) with its reader and writer names. Load always writes Passenger, and the projector's Unload takes the Passenger role only.
- **The index of PR 32** (`ObjectIndex`, PlayStepMemo.cs:151) holds objects by Location and positions by id, built once per state. It has no Passengers by vehicle. `At`, `Location`, and `Aboard` each scan the state.

**Needs a read at its task.**

- 2a: every `"asl:crew"` site, each judged as "any crew" or "an infantry crew" (A1.123 gives a vehicle crew all an infantry crew's capability except its Strength Factor, so nearly all are "any crew"); the Chapter H Type column for the class of each vehicle.
- 2b: where `CrewOutcome` and the Specific Collateral path take a unit's results, so that Passengers enter at one place.
- 2c: how a FG is formed (D6.64 builds its own makeup, kept out of the general FG code).
- 2d: how the Defensive First Fire window sees a moving stack, so the (un)loaders can be put in it.
- 2e: the terrain a vehicle's entry step crosses (woods, orchard, building, rubble, Water Obstacle, bocage, the road hexside).
- 2g to 2j: the A21.22 penalties as they apply to the catalog's vehicles; the counter source for a red CS#.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and for the absent-safety of each):
- on a fire attack and an ordnance shot: the PRC of each vehicle in the Location (a nullable list on `FireVehicle` and on the ordnance vehicle target);
- on a destroying event: each Passenger's and Rider's Survival;
- on a load: `riders`;
- new events: a Rider's Bail Out, a voluntary Abandonment, a re-crewing, a capture, and a Scrounging;
- a new containment role for a crew inside its vehicle.

**Read from the loaded catalog, not the record** (the boundary review, fix Round C): the Carrier's reads (`CrewExposed`, `PassengerCapacity` 5 to 4, `MayChangeExposure`, `ButtonUpBar`) take the vehicle's class from the catalog loaded now, not from a fact recorded with the game. This is absent-safe only because no live game (470) or store game (528) holds a Carrier; a game recorded before catalog 1.18.0 with the Bren carrier would read its class at replay. Kept so; a game that needs it would carry the class as a record fact.

**State facts added at the fixes (Round C),** computed by the projector from records every game already holds, so no record gains a fact: `UnitInstance.CapturedFrom` (the side a `vehicle-captured` record took a vehicle from; null for every older game) and `GameState.AbandonedThisPlayerTurn` (the vehicles marked Abandoned in the current Player Turn). Both are new state properties, so the gate's computed `PROOF_SKIP` takes them; no older record's replay changes.

## 5. What the frame found outside the rows

1. **The Bren carrier's Passenger capacity is 5;** D6.81 gives 4 (sheet MFG, so a manufactured value corrected under its rule). Repaired in 2a.
2. **Load and unload records read as "X moves to ..."** with no Passenger named (PlayRecords.cs:310, ReplaySteps.cs:315). Repaired in 2d.
3. **The move-vehicle action's schema** omits the kinds `load`, `unload`, and `delay` and the arguments `units` and `turret`, with `additionalProperties: false` (GameActions.cs:549-566), so an external caller that validates against it would refuse a legal load. Repaired in 2d.
4. **The rule inventory's rows D6.8 to D6.84 say "No Carrier in the catalog".** That is stale since the Goodwood work added `campaign-british-bren-carrier`. Corrected in the documents step.
5. **The prompt's line on replay after a catalog bump** is wrong (section 2). Corrected in the prompt's next version.

## 6. The frame's questions

Each was put to a read-only agent with a proposal and is answered by a ruling decided under the user's word of 2026-10-23. Rulings made at a task are numbered on from these.

**F1. The size and the order.** The agents' estimates are about 15:00 of building against the plan's 7:00.

**R65.1:** one pass, built in section 2's order. The foundations go first, since every later task reads the crew helper, the catalog, and the index. Then the Passengers under fire (Riders and Carriers reuse their Collateral Attack and Survival), their actions, and the loading. Then the Riders, which need the Passenger fire path, and the Carriers, which need the Rider capacity. Then the crew's own acts and the TC and Survival paths. The captured vehicles and the wrecks come last.

**F2. The crew kind.** **R65.2:** a Rules member `IsCrew(kind)`, true for `asl:crew` and `asl:vehicle-crew`, replaces each literal comparison whose rule speaks of a crew. A1.123 and D5.1 give a vehicle crew every capability of an infantry crew but its Strength Factor, and that difference lives in the definition's values. A site whose rule is for an infantry crew alone keeps the literal and says why in a comment (none is known yet).

**F3. The catalog.**

**R65.3:** catalog `asl-scenario-a1` 1.18.0 adds the following.

- **Eight `asl:vehicle-crew` definitions,** from the chart (sheet NCC, physical page 695), confirmed on two independent renders:

| Definition | FP-range-morale | Broken morale | BPV |
|---|---|---|---|
| `german-vehicle-crew` | 1-2-7 | 7 | 7 |
| `russian-vehicle-crew` | 1-2-7 | 7 | 5 |
| `british-vehicle-crew` | 1-2-7 | 7 | 7 |
| `american-vehicle-crew` | 1-2-6 | 7 | 6 |
| `finnish-vehicle-crew` | 1-2-7 | 7 | 7 |
| `italian-vehicle-crew` | 1-2-6 | 5 | 5 |
| `axis-minor-vehicle-crew` | 1-2-6 | 5 | 5 |
| `french-vehicle-crew` | 1-2-7 | 6 | 5 |

- **A vehicle class** (`asl:vehicle-class`, vocabulary 1.19.0) on each of the 17 vehicles. The members are tank, tankette, td, spa, spaa, assault-gun, carrier, halftrack, armored-car, truck, and other.
  - For the two tanks, the trucks, and the halftrack, the value comes from the Chapter H listing's Type column, on their own sheets.
  - For the game-default vehicles (sheet MFG) it is manufactured under R0.3. The Maultier with its rocket launcher is classed halftrack.
- **The Bren carrier's Passenger capacity** corrected to 4 (D6.81).
- **The red CS# trait** (`asl:cs-red`, D5.7) where the listing prints a red CS#, read on renders. It is manufactured under R0.3 (sheet MFG) for the game-default vehicles.
- **Rider capacity is not an attribute.** It is derived in Rules from the class (D6.2: 14 PP; a Carrier 8 PP; any other AFV one SMC; a truck none).
- **No inherent-crew attribute** for the Carrier. D6.82's HS is the nationality's 2-4-7 HS (`british-half-squad`), and an elite Carrier HS waits for a scenario that names one (backlog).

The referee agent of the documents step checks the rows against the chart, as at earlier catalog additions. The synthetic catalog gains matching definitions. The pins that name 1.17.0 move with the bump: `LiveFire.CatalogVersion`, `game_file.py`'s `CATALOG`, and the tests' pins.

**F4. Older games and the catalog check.**

**R65.4:** `play.fire-catalog` accepts a game whose catalog is `asl-scenario-a1` at the code's major version and a minor version no later than the code's, as the vocabulary's `Serves` does. A catalog only adds and corrects definitions, never removes one (the standing rules, section 4).

- The games recorded at 1.17.0, the user's Studio games among them, keep firing.
- Older games that refused fire because of their version now fire. Their sweep lines are intended changes, named at the gate.

**F5. The Passengers' fact.** Pass 60's `SpecificCollateral` fact cannot gate the new effects, since records since pass 60 carry it.

**R65.5:** a nullable list of each vehicle's PRC, by id, on `FireVehicle` and on the ordnance vehicle target, built from the index of R65.25 and written by every plan since the pass, empty when nobody is aboard. Its absence resolves as before. A destroying event carries each Passenger's and Rider's Survival; where it is missing, the units are eliminated with the vehicle as before.

**F6. Vulnerability.**

**R65.6:**
- A halftrack's Passengers are CE or BU with its crew, derived from the vehicle's `asl:bu` (D5.33, D6.61): nothing new is stored.
- A broken, pinned, or shocked Passenger is BU. Passengers are never Stunned: a failed MC makes them BU and broken (D6.62, D9.1).
- A BU halftrack's Passengers are attacked only through D6.61's elevation exception (+2, less one per full level of advantage beyond the range) or by TPBF (A7.211).
- A truck's PRC are always Vulnerable, with no CE DRM (D6.7).
- Riders are always Vulnerable (D6.23).

**F7. Collateral Attacks.**

**R65.7:**
- The General Collateral Attack (D.8, D.8B) takes the attack's own column and DR against every Vulnerable PRC, with TEM or the CE DRM, whichever applies, never both.
- The Specific (D.8A, D6.71) takes the IFT FP, or the HE Equivalency, of the weapon or ammunition, with the CE DRM alone.
- Neither applies when the vehicle is destroyed, after a Dud or a Deliberate Immobilization, or to shocked or stunned PRC (then only Riders are attacked).
- Passengers take Infantry results through `CrewOutcome`'s Collateral path.
- D5.31's rule (the CE DRM not cumulative with positive TEM or with Height Advantage under Infantry fire) applies where the attack carries R65.5's fact. An attack recorded before the pass resolves as it did. This lifts ruling R25.6's refusal of an AFV in positive TEM under other IFT attacks.
- D5.311: an OT AFV's crew under Air Bursts is attacked as unarmored, and a crew that breaks routs beneath.

**F8. Vehicles beyond the KIA#.** **R65.8:** A7.308's Random Selection of the vehicles affected replaces the refusal `vehicles-beyond-kia`, for the IFT and for C1.55's mortar, with the selection's dr on the record.

**F9. Morale and Survival.**

**R65.9:**
- Passengers and Riders check at their own Morale Level and take Infantry results, with DM on a broken Passenger (D6.66).
- Survival (D6.9) is one DR per unit against the CS# or cs#: +1 if broken, no Shock or Stun DRM, none in CC or when the vehicle burns.
- Survivors are placed beneath the wreck. A SW not held by a survivor is eliminated.
- 65.1 reuses the Survival roll's machinery. The crew's own survival is 2h's.

**F10. Mounted Fire and the SW.**

**R65.10:**
- `PassengerBar` is lifted for fire, with LOS from the vehicle's Location.
- A truck's and other Passengers fire at half, halved again for Bounding, Motion, or Area fire. An armored halftrack's CE Passengers fire at half only when it moved.
- The SW allowed are a LMG, a PIAT, and a DC (D6.72).
- Firing bars unloading in that phase (D6.5), and Prep Fire freezes the vehicle (D.3).
- Fire from a vehicle in Bypass stays refused (pass 100).
- A SW may pass to PRC or Infantry in the Location (D6.31).

**F11. Rout.** **R65.11:** a broken Passenger of a Stopped vehicle may rout beneath it, spending all its initial RtPh MF. It must do so when the crew is eliminated, broken, or has Abandoned. No fact is needed.

**F12. Leaders aboard.** **R65.12:**
- A CE Passenger leader directs his halftrack's Passengers, and the hex's other units only if the halftrack has not moved that phase.
- An Armor Leader directs Passengers only inside his halftrack's FG.
- MC, Rally, and LLMC reach are confined to the same vehicle.
- This is D6.64's makeup alone, built apart from the general FG code.

**F13. OVR and CC.**

**R65.13:**
- In an OVR, a CE halftrack's Passengers add half their printed FP and other Passengers a quarter, then tripled, with one leadership modifier from the Armor or the Passenger leader (D7.11, D7.16).
- Results against Passengers after the declaration do not change the OVR's FP.
- An OVR may attack an enemy's Vulnerable PRC (D7.12).
- In CC: BU Passengers do not attack; Riders are apart (A11.611, +1 to their DR); Passengers of a Mobile vehicle leave Melee after the first round (A11.71).
- A11.52's capture of Passengers moves to pass 75.

**F14. FFNAM and FFMO on loading and unloading.**

**R65.14:**
- The step's (un)loaders are Infantry targets of Defensive First Fire with FFNAM, and with FFMO only when unloading from an unarmored conveyance in Open Ground (D6.5, A4.6).
- A load fails if the loader ends pinned, broken, or eliminated (D5.43), with a follow-up event that puts it back on foot.
- A pinned unloader stays in the Dismount Location (A7.821).

**F15. Loading completed.**

**R65.15:**
- A unit may board as the last step of its own move.
- A leader's IPC counts (A4.42), and a SW may be left aboard a halftrack or Carrier.
- After unloading, the four-MF cap holds and Double Time is barred (backlog rows of pass 40).
- Unloading into an enemy-occupied Location places a CC counter.
- Between moves, only the vehicle and its disembarking PRC may move.
- D2.401's time aboard counts toward a later Motion attempt.
- Load and unload get records of their own ("[G4]: 4-6-7 squad A boards the SPW 251/1"), and the move-vehicle schema gains its missing kinds and arguments.

**F16. Riders.**

**R65.16:**
- A load names `riders`, recorded as `riders: true` on the step. Its absence means Passengers, so older games replay as before.
- Rider capacity follows the class (R65.3) and is counted apart from Passenger PP. Unload takes either role.
- Riders are allowed for the Russians from 1942 and for every nationality from 1943 (D6.2). A game with no scenario year refuses Riders and says why.

**R65.17:**
- One Rules member decides D6.21's and B13.43's Bail Out:
  - when the vehicle enters woods, orchard, a building (but a friendly Vehicular-Sized Entrance), rubble, a Water Obstacle, or bocage (but a Breach);
  - unless it is crossing a road hexside, or is in Bypass, or is in a stream;
  - and when a turreted AFV changes its TCA.
- Riders use no SW, and fire at half their inherent FP, a quarter if the vehicle moved or is in Motion, with +1 to their CC DR (D6.22).
- A pin or break makes them Bail Out (D6.23).
- The event `rider-bailed-out` carries the cause, the NMC DR, and each SW's dr (1 to 3 kept, 4 to 6 malfunctioned, an X# SW eliminated; every SW eliminated if the carrier breaks). After it:
  - the unit is beneath the vehicle with its movement ended, and a Prep or Final Fire counter if unbroken;
  - FFNAM applies as to an unloader;
  - a Bail Out into an enemy-occupied Location places a CC counter.

**F17. Carriers.**

**R65.18:**
- A vehicle of class carrier is never BU, and its crew, HS, and Passengers are always CE and Vulnerable, stunned or shocked included (D6.84).
- Its inherent crew is the nationality's 2-4-7 HS. The HS is placed on dismount, on Abandonment, and on Survival, in place of a vehicle crew, unless the Carrier is armed with a FT or a 76mm mortar or tows a Gun (D6.82, D6.83).
- Its capacity is 4 Passenger PP, or none with a mortar, FT, or 6 FP AAMG as MA, and 8 Rider PP (D6.81).
- An inherent Carrier HS is worth 2 VP (p. 100).

**F18. The vehicle crew.**

**R65.19:** one member places a crew that leaves its vehicle, whether it survives, is Abandoned, is Recalled, or dismounts. It takes the vehicle nationality's `asl:vehicle-crew` definition, or the Carrier's HS (R65.18), and it replaces `CrewCounter` and `AbandonEvents`' lookups. An older game's recorded `instance-created` keeps the infantry crew it placed, so it replays as it did. The crew inside the AFV keeps checking at Elite or 1st Line morale (built).

**F19. The crew's own acts.**

**R65.20:**
- **Voluntary Abandonment** (D5.41) is an MPh action of an unmoved vehicle with no fire counter. It marks the vehicle Abandoned and places the crew by R65.19.
- **Self-destruction** (D5.411) is a choice on that action alone: a non-burning wreck with no CS, or every MG and the MA malfunctioned.
- **Re-crewing** (D5.42): Infantry in the Location at the start of its MPh spends all its MF and enters by a new containment role, the crew inside. It keeps its own id, its +1 (D5.34), and the Armor Leader stored with it (D3.43), and it leaves again on the next Abandonment. It is barred:
  - in the Player Turn the old crew left;
  - for a Recalled crew re-crewing its own AFV (D5.341).
  A friendly MMC other than a vehicle crew re-crews under A21.22's penalties (2i).
- **MG Removal** (D6.631) is an option of Abandonment for an armored halftrack of the crew's nationality. It takes the MG as the nationality's LMG when its FP is 3 or less, sets Disabled, and is undone by re-entry with that SW. A failed D5.5 TC forbids it.
- **D5.8:** a Rules member gives the new crew's Prep or Final Fire counter from the vehicle's fire counters, Motion, and moved state.

**F20. TC, Survival, Brew Up.**

**R65.21:**
- The Immobilization TC's first cause is built on the mortar path (C1.55) and on the IFT Vehicle line.
- Its second cause (D5.5) is a Rules member that recomputes the To Kill result on an Original DR of 5. It applies only to Direct Fire ordnance at a bogged or immobilized target, and only a Burn, an Eliminated, or an automatic Shock counts.
- One Rules member gives Crew Survival's +1 (broken, Stunned, Shocked, UK, Recalled, or a recorded +1) and the Abandoned bar for every path. Shocked and Abandoned enter `FireVehicle` (backlog rows of pass 60).
- The IFT Vehicle-line kill of an armed vehicle gets Survival.
- Survivors carry Hazardous Movement for the phase (D5.6), and the killing attack's Collateral Attack and Residual FP skip them.
- A red CS# gives -1 to the Final TK DR for the burn determination at each burn test (D5.7).
- Brew Up (D9.2) is the Survival roll on every destruction path that does not burn.

**F21. Captured vehicles.**

**R65.22:**
- An Abandoned AFV is captured at the end of the CCPh: automatically with no enemy Personnel present, or else by a capture attempt in vehicle CC at -1 with A11.61's CE and Immobile DRM. The event `vehicle-captured` names the captor's side.
- A Temporary Driver runs a captured unarmed vehicle for an MMC captor; a SMC captor is removed (A21.21).
- A Temporary crew enters by R65.20's role.
- Penalties: the existing captured-weapon and red-number rules, plus halved MP and the start dr (4 or less moves; 5 no move; 6 or more Bogs; +1 for a Russian captor) (A21.22). A SMC crew fires the AAMG only while CE, is Wounded on Stun, and is eliminated on Recall (D5.343).
- Ruling R24.5 (a captured vehicle neither holds nor prevents Control) is lifted for a vehicle captured with the pass's event: a manned captured vehicle holds Control for its captor (A26.12).
- Older records keep R11.16's captured and Abandoned state.

**F22. Wrecks and Scrounging.**

**R65.23:**
- A non-burning wreck may be the target of ordnance on the Vehicle Target Type, of the IFT, and of a FT or MOL, as its vehicle. Only a Burn matters, and it places a Blaze (D10.1). The projector accepts a burning `vehicle-wrecked` on a wreck.
- **Scrounging** (D10.5 to D10.52) is an RPh action. It is open to an unbroken, non-berserk MMC not in Melee, in the Location of a non-burning wreck or an Abandoned vehicle that is not Scrounged or Disabled.
  - A dr of 1 to 3 succeeds, and the dr is the number of SW taken by Random Selection.
  - Only a BMG that is not fixed, an AAMG, or a mortar MA qualifies. A MG becomes the nationality's LMG, dismantled.
  - The unit and its SW are TI, and the vehicle is marked Scrounged.

**F23. What moves on.** **R65.24:** section 2's moves, each with a backlog row naming the pass that takes it.

**F24. Memory and performance.**

**R65.25:**
- `ObjectIndex` gains the PRC of each vehicle, built in the same single pass over the state. Every Passenger, Rider, and crew-inside read of the pass goes through `MemoOf(state)`: no per-unit `Aboard`, `Passengers`, or `At` inside a loop.
- `KeepPassengers` keeps its once-per-event scan but reads the index where it can.
- The measurement:
  - the Studio's play-step time and allocation on two games: a game of loaded vehicles made for 2b (two halftracks with Passengers, a truck, and a tank with Riders), against the pass's base;
  - `dl-tractor-works-02` at revision 1316, where the berserk charge search was measured (PR 32), so the charge path does not regress.
- A rise beyond the noise of three runs is a fault to find before the task is done.

**F26. What 2b left (ruling made at the task, 2026-10-24).** **R65.26:** D5.311 (an OT AFV's crew under Air Bursts) and A7.308 (Random Selection of the vehicles beyond the KIA#, with ruling R25.3's one-vehicle refusal lifted for the IFT) move to a small increment 2k after 2j, or to the backlog if the session's end comes first.

**F27. A Passenger's Prep Fire and its vehicle (ruling made at task 2c, 2026-10-24).** D.3 (p. 192) bars a vehicle whose PRC Prep Fired from moving in the MPh and from firing in the MPh and the AFPh, but not from its own fire in that PFPh. The build marks the vehicle Prep Fired, the path the projector, the movement planner, and the fire bars already read, which also bars that PFPh fire. **R65.27:** keep the marker; a vehicle fires its own weapons before its Passengers in the PFPh; D.3's exact reading goes to the backlog.

**F28. A crew as a target (ruling made at task 2h, 2026-10-24).** Ruling R8.3 refuses IFT fire at a crew with other units or with no Gun named (`asl.a1.fire.crew-target-unreviewed`), so every vehicle crew the pass places could not be fired at. **R65.28:** a crew that mans no Gun is an ordinary Infantry target, alone or with other units (A1.123: it is an Infantry MMC with a crew's capabilities); a Gun's crew keeps R8.3's reading. It only turns refusals into plans, so no recorded game changes. Built in increment 2k.

**F29. CC with a crew (ruling made at task 2i, 2026-10-24).** Ruling R24.3 refuses an advance into a Location that holds any crew (`play.advance-crew`, ScenarioA1AdvanceCalculator.CrewBar), and the CC rules leave a crew out, so a vehicle crew beneath its Abandoned AFV blocks both the advance and a capture. **R65.29:** a crew that mans no Gun is advanced against and fights in CC as ordinary Infantry (A1.123); a Gun's crew keeps R24.3's reading. Built in increment 2k with R65.28. It only turns refusals into plans.

**F30. The scrounged LMG (ruling made at task 2j, 2026-10-24).** R65.23 said a scrounged MG becomes the nationality's LMG "dismantled". D10.52 says "in its dm form if possible", and A9.8 gives a LMG no dm form; the engine also refuses to assemble a LMG. **R65.30:** a scrounged MG becomes the nationality's LMG, assembled, since a LMG has no dm form; a mortar MA would be dismantled, but no catalog vehicle has one.

**F31. Choices the reviews left open (rulings made at the reviews, 2026-10-24).**

- **R65.31:** a crew that breaks under Air Bursts (D5.311) stays inside its AFV, broken, and must rout beneath in the RtPh; the AFV is Abandoned when it does.
- **R65.32:** A21.22's free exchange of a capturing squad for a HS is not built; the refusal says so, and a backlog row records it.
- **R65.33 (revised at Round B, 2026-10-24):** the Scrounging dr takes no leadership modifier. D10.5's "1-3 dr (∆)" marks it with the triangle, and A.10 (p. 43) says leadership ratings "do not modify the DR/dr of certain actions ... identified by a triangle symbol". The review (R2, finding 10) and the first wording of this ruling read the ∆ backwards; the fixing agent caught it on the page, and the session confirmed it there.

**F25. Testing** (the standing rules, section 2, the user's rule of 2026-10-23).
- While a task is built, the Studio is its test, in a small game made for it (`games_2x.py`).
- A task's new Rules and Play tests are run by method, and only those.
- The whole projects run at the gate and nowhere else.
- When a task adds an example, an id, a kind, or a catalog definition, the tests that count or draw them are part of its blast radius (the lesson of PR 33's CI, `notes.md`).

## 7. Foundations and the vehicle crew: part of 65.6 (increment 2a)

Built on 2026-10-23, 22:59 to about 23:27, in one commit (0ccb9f4b) by a building agent to a brief (`brief_build.md` and `brief_2a.md` in the pass's folder of `code-builders/pass-history`), its diff read and its games played in the Studio by the session.

### 7.1 Departures from the frame, first

Each decided under the user's word of 2026-10-23; each is open to a veto.

- **Ruling R65.4 changes nothing visible today.** `GameProjector.Start` reads the loaded catalog by name (`UnitCatalogs.For` ignores the version), so a game's `state.Catalog.Version` is always the loaded one, and the 1.17.0 games already fired. The relaxed check matters only if a loaded catalog ever differs from the code's. It covers all five checks (fire twice, rally, CC, ordnance), not the Fire package's alone.
- **No Italian or Finnish vehicle is in the catalog** (all 17 are German, Russian, or British), so an Italian crew's survival is tested in Rules only.
- **The red CS# is printed on none of the five listed vehicles:** every span of their Chapter H rows (pp. 337, 339, 355, 356) is black, as the catalog design's section 9.7 says. `asl:cs-red` is in the vocabulary and on no vehicle.
- **A vehicle crew's class is not in the source** (D5.1 gives none; A1.123's elite is the infantry crew's), and it Self-Rallies, as D5.1 has it.
- **Three older tests asserted the infantry crew** (`BacklogPass5Tests`, three of `BacklogPass7Tests`) and now expect the vehicle crew: the intended change.
- **The four rule packages were pinned again** (pass 55's `repin.py`), since their case matrices hash the catalog; `BOTH_SKIP` holds `GameEvent.RulePackage`.
- **Found in the Studio and fixed by the session:** the page named the new counter "1-2-7 vehicle-crew" (`DisplayText.Kind` strips the prefix only); it reads "1-2-7 vehicle crew", as D5.1 writes it.

### 7.2 What is built

`ScenarioA1Definitions.IsCrew(kind)` replaces the literal `"asl:crew"` at about 30 sites of Rules, Play, and Units (R65.2); seven more on the Play page were found and replaced by the session at 2k (section 17.1). Catalog `asl-scenario-a1` 1.18.0 and vocabulary asl@1.19.0 (R65.3): the eight vehicle crews of the chart on sheet NCC, `asl:vehicle-class` on the 17 vehicles (from the Chapter H Type column, or manufactured under R0.3 on sheet MFG for the game defaults), the Bren carrier's Passenger capacity 5 to 4, and the trait `asl:cs-red`; the synthetic catalog in step; the pins of 1.17.0 moved (`LiveFire.CatalogVersion`, `game_file.py`'s `CATALOG`, the tests'). `ScenarioA1Definitions.CatalogServes` relaxes the catalog checks (R65.4). `GamePlanner.LeavingCrew` places the crew that leaves a vehicle as the nationality's vehicle crew on the Survival and Abandonment paths, in place of `CrewCounter`'s and `AbandonEvents`' lookups (R65.19), with a marked place for the Carrier's HS (2f). `PlayStepMemo`'s `ObjectIndex` holds each vehicle's PRC and the vehicle of each unit aboard, built in its one pass over the state (R65.25); `AboardBar`, `PlanLoad`, and the vehicle step read it.

**New in a record:** none. A recorded `instance-created` keeps its kind and definition, so an older game keeps the infantry crew it placed; the catalog's identity, hashed into every state, is `BOTH_SKIP`'s.

### 7.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-crew-tank` | The T-34 M41's AP (dice 4 2, 3 3, 2 2) wrecks the PzKpfw IIIH, and its crew survives as `german-vehicle-crew`, kind `asl:vehicle-crew`, id `gun-94f860606b0f-dt-crew`, read back from the game file. Played. |
| `p65-crew-gun` | The brief's game: a Russian 45mm AT Gun at a CE PzKpfw IIIH. Made, not played. |

Rules: `AVehicleCrewIsACrewAndASquadIsNot`, `AVehicleCrewStacksCountsAndFightsAsACrew`, `TheCatalogHoldsTheEightVehicleCrewsOfTheChart`, `ACrewLeavesItsVehicleAsItsNationalitysVehicleCrew`, `APackageReadsItsCatalogAtAnOlderMinorVersion`. Play: `AKilledGermanTanksSurvivingCrewIsTheGermanVehicleCrew`, `AVehicleCrewMansAGunAsACrewDoes`, `AGameRecordedAtCatalog117Fires`, and `BacklogPass26Tests.TheObjectIndexReadsEveryLocationAndPositionAsTheStateDoes`, extended to the PRC map. The agent ran `CounterSheetCatalogBuilderTests` whole (16 methods), since each builds from the worksheet the change altered.

**The measurement (R65.25)** began here: `p65-perf-tw`, a copy of `dl-tractor-works-02` cut at revision 1316, in the base Studio (the gate's `main-bin` build of d0a8f4cd, port 6671) and in mine (6670), three warm runs each. Base: hand-over 276, 265, 355 ms; an "end the phase" proposal 135, 157, 147 ms. After 2a: 225, 245, 241 ms; 112, 137, 123 ms. No regression.

## 8. Passengers and Riders as targets: 65.1 (increment 2b)

Built on 2026-10-23 and 24, 23:37 to about 00:15, in one commit (7baee874) by a building agent to a brief (`brief_2b.md`), its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

- **D5.311 (an OT AFV's crew under Air Bursts) and A7.308 (the Random Selection of the vehicles beyond the KIA#) were left out,** as the brief allowed. **Ruling R65.26:** both go to a small increment 2k (section 17).
- **Left for later, each a row of the backlog's section 71:** Heat of Battle, leadership, and LLMC for PRC (D6.651's leadership is 2c's); A9.74's dr for a SW a PRC held; PRC Survival after a mortar's kill of an AFV (built in 2h) and after an Unconfirmed Kill (eliminated as before; section 18.1 says why that stands); an unarmored vehicle's crew under a Specific Collateral Attack (the catalog's unarmored vehicles are trucks with an Inherent Driver, D5.1, so there is nothing to attack); a Shocked AFV's Passengers on the IFT path (built in 2h and fix Round A).
- **A concealed or hidden vehicle's `prc` fact is null,** so the attack reads as before and nothing is revealed.
- **A slip against the rule for unit tests:** the agent ran the whole `ScenarioA1Pass65RulesTests` class once (5 methods of 2a beside its own 6).

### 8.2 What is built

`ScenarioA1PrcRules` decides how an attack reaches one PRC (`Exposure`): a halftrack's Passengers CE or BU with its crew, BU when broken, pinned, or shocked; a BU halftrack's only from an elevation advantage beyond the range (`CeDrm`, +2 less one a level) or by TPBF from the same or a higher Location (A7.211); a truck's PRC and Riders always Vulnerable (R65.6). `ScenarioA1FireCalculator`'s vehicle effects make the General Collateral Attack on every Vulnerable PRC of every vehicle in the Location, on the attack's column and DR, with TEM or the CE DRM, never both (D.8B, D5.31); `ScenarioA1ArmorCalculator.SpecificCollateral` makes the Specific on them after an ordnance hit or a FT, MOL, or DC at a named vehicle that leaves it standing (D.8A, D6.71) (R65.7). Passengers take Infantry results; a failed MC makes one broken and BU, never Stunned; a broken one takes DM (D6.62, D6.66, D9.1). Ruling R25.6's refusal of an AFV in positive TEM is lifted where the attack carries `prc`. Each Passenger and Rider of a vehicle destroyed on a To Kill, the Vehicle line, or a FT, MOL, or DC makes its own Survival DR against the CS# or cs#, +1 if broken; survivors are moved beneath before the wreck is made (R65.9). The record lines name the PRC attacked and their results (`FireText.PrcLines`).

**New in a record:** `prc` on `FireVehicle` and on the ordnance vehicle target (unit, definition, role, and `broken` and `pinned` only when true), written by every plan since the pass, empty when nobody is aboard, and taken from the record at replay; `prcAttacks` and `prcSurvival` in the resolutions; the rolls of each PRC's Survival. **An older record** carries no `prc`: its attack reaches no PRC, its CE DRM adds to a positive TEM and Height Advantage as before, and its destroyed vehicle's Passengers are eliminated with it by `KeepPassengers`.

### 8.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-prc-halftrack` | The Russian PFPh: a 4-4-7 with its LMG fires 6 FP at [F8], a CE SPW 251/1 carrying a German 4-6-7, one level up. The proposal says "it reaches 4-6-7 squad aboard vehicle in [F8] where Vulnerable (D.8B)"; with dice 2 4 1 1 the Passenger "takes a General Collateral Attack (D.8B), Final DR 8 (CE +2, D5.31): PTC; NTC 1, 5 = 6 against 7: passed". The CE DRM replaced Height Advantage. Played. |
| `p65-prc-halftrack-bu`, `p65-prc-truck` | The same at a BU SPW 251/1 (its Passengers not attacked, D6.61); a 45mm AT Gun at a truck carrying a HS (D.8A, D6.9). Made, not played. |

Rules: `AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack`, `ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf`, `ATrucksPassengersTakeTheTemAndNoCeDrm`, `TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact`, `ASpecificCollateralAttackReachesThePassengersAfterAMissedTkDr`, `NoCollateralAttackAfterAKillButEachPassengerRollsForSurvival`. Play: `AnIftAttackOnALoadedHalftrackPinsItsCePassengers`, `AKilledTrucksPassengersRollForSurvival`.

## 9. Passenger action: 65.2 (increment 2c)

Built on 2026-10-24, 00:23 to about 00:55, in one commit (c7ece170) by a building agent to a brief (`brief_2c.md`), its diff read and its games played in the Studio by the session, who fixed two faults the check found.

### 9.1 Departures from the frame, first

- **CC with PRC (R65.13) and D6.651's reach of a Passenger leader are decided in Rules and not wired into Play** (`ScenarioA1PassengerActionRules.CloseCombat`, `LeaderReaches`): a CC action naming a Passenger stays refused, and a Passenger leader reaches no one outside his vehicle. To pass 70 (backlog 71.1).
- **Vehicles still fire alone (ruling R25.7),** so a halftrack and its Passengers never make one FG in Play; D6.64's bar is tested in Rules. A Passenger's Bounding First Fire and a DC thrown by a Passenger stay refused. Backlog rows.
- **D.3, judged (ruling R65.27):** after a Passenger's Prep Fire the vehicle is marked Prep Fired, which also bars the vehicle's own PFPh fire afterward, where D.3 (p. 192) bars only its MPh move and its MPh and AFPh fire. The marker is kept; the vehicle fires its own weapons first.
- **D6.31 is read strictly:** Infantry on foot pass no SW to a Passenger, which takes one only as it loads.
- **Rout beneath is a must only when the vehicle is Abandoned:** here an eliminated crew takes its vehicle with it.
- **Found in the Studio and fixed by the session:** the Rout panel never offered a Passenger (`ReadRout` walked `ActingUnits`, whose `SeenOnly` leaves units aboard out); `SeenOnly(units, aboardToo)` now takes the flag, and its per-unit `state.Aboard` scan became the planner's index read (`VehicleRidden`). The fire panel's director list left out every leader aboard (`FireLeaders`); a leader aboard is offered when every firer rides his vehicle. The new button `button.rout-beneath` needed the Icon Lab's coverage file.

### 9.2 What is built

`ScenarioA1PassengerActionRules`: `MountedFire` and `Multipliers` (a truck's Passengers at half, a quarter when Bounding, in Motion, or Area fire; an armored halftrack's only while CE, at half when it moved; Bypass refused; a LMG, PIAT, or DC only, R65.10); `FireGroupBar` (D6.64, D6.65, D3.4) and `LeaderReaches` (D6.651, R65.12); `OverrunShare`, `OverrunFirepower`, and `OverrunLeadership` (D7.11, D7.16, R65.13); `CloseCombat` (A11.611, A11.71); `TransferBar` (D6.31); `UnloadAfterFireBar` (D6.5). `ScenarioA1RoutCalculator.RoutBeneath` (R65.11). `GamePlanner.PassengerActions.cs` builds the facts. The page: the fire panel offers a Passenger as a firer, "[F8]: 4-6-7 squad G1, aboard vehicle G1", and its proposal states the multiplier; the Rout panel's `button.rout-beneath`.

**New in a record:** `firers[].mounted` (the vehicle a Passenger fires from, with `moved`, `motion`, and `rider`), written only for a Passenger firer, which no older record has; `overrun.passengers[]` on an OVR's fire facts, read at replay so that later results do not change its FP; the OVR step's `units`. **An older OVR** names no Passenger and resolves as it did (`AnOvrWhoseRecordNamesNoPassengerResolvesAsBefore`).

### 9.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-mounted-truck` | The German PFPh: the director list offers "[F8]: 9-1 leader G1"; the proposal reads "4-6-7 squad G1: 4 FP × 0.5 (mounted fire, D6.1) = 2" and "... fires from vehicle G1 as Mounted Fire, its FP times 1/2 (D6.1)". Played. |
| `p65-rout-beneath` | The German RtPh: "Rout 4-6-7 squad G1 in [F8] beneath vehicle G1", committed: "4-6-7 squad G1 routs beneath vehicle G1 in [F8], spending all its MF (D6.1, D5.311)". Played. |
| `p65-ovr-halftrack` | A CE SPW 251/1 with a 4-6-7 and a 9-1 aboard overruns a 4-4-7 (D7.11, D7.16). Made, not played. |

Rules: `ATrucksPassengersFireAtHalfAndAtAQuarterWhenItMoves`, `ACeHalftracksPassengersFireAtFullUnlessItMovedAndABuOnesDoNot`, `APassengerFiresOnlyALmgAPiatOrADc`, `ABrokenPassengerRoutsBeneathAStoppedVehicleAndMustWhenItsCrewIsGone`, `D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry`, `APassengerLeadersReachIsHisVehicle`, `AnOvrAddsItsPassengersShareTripledWithOneLeadershipModifier`, `PassengersInCcAPassedSwAndAnUnloadAfterFire`. Play: `ATrucksPassengerFiresAtHalfFpAndFreezesTheTruck`, `ACeHalftracksPassengerFiresNoMmg`, `ABuHalftracksPassengerDoesNotFire`, `ABrokenPassengerRoutsBeneathItsStoppedTruck`, `ALoadedHalftracksOvrAddsHalfItsPassengersFpTripledAndTheirLeader`, `AnOvrWhoseRecordNamesNoPassengerResolvesAsBefore`.

The measurement after 2b and 2c (hand-over / proposal ms, warm runs): base 287/140, 241/160, 310/171; pass 65 253/104, 221/109, 322/126. No regression.

## 10. Loading and unloading completed: 65.3 (increment 2d)

Built on 2026-10-24, 01:07 to about 01:38, in one commit (1edd653c) by a building agent to a brief (`brief_2d.md`), its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

- **The interim move is simplified:** the unloaders move once the vehicle's move ends, before any other unit; D6.5's exact interleaving is a backlog row.
- **No CC counter on an unload into an enemy-occupied Location:** no such condition exists (`asl:cc-reaction` is another thing); the proposal said a CC counter would be placed until fix Round D, and now says the units are not held in Melee. Backlog row, pass 70.
- **Ordnance First Fire does not treat (un)loaders as targets** (only the IFT does), and the Road Bonus can still lift an unloader above four MF. Backlog rows.
- **A fault, small:** `ResolveOverrun` and the other members that build a new `MovementState` drop `Disembarked`, so an OVR after an unload loses the interim stack. Backlog row.
- **`MovementState` gains three state-only properties:** older games with a load or unload change their state lines, which the gate's computed `PROOF_SKIP` takes.
- **Fixed by the session:** `UnloadableUnits` on the page scanned `state.Passengers` on every render; it reads `Live.Planner.Riding` (made public) from the index.

### 10.2 What is built

The (un)loaders of a step are Infantry targets of First Fire with FFNAM, and with FFMO when unloading from an unarmored conveyance in Open Ground (`ScenarioA1PassengerCalculator.EmbarkingFfmo`, R65.14); a load fails on a pin, a break, or an elimination (`LoadFailures`, D5.43), and an `instance-moved` puts the loader back on foot; a pinned unloader stays in the Dismount Location (A7.821). A unit boards as the last step of its own move (`EndOfMoveLoadBar`) with a leader's IPC (`BoardingIpcBonus`, A4.42); a SW is left aboard a halftrack or Carrier and taken again (`LeaveAboardBar`, `KeepsSwAboard`); after unloading, four MF, no Double Time, and no leader's bonus (`ConveyedAllowance`, `ConveyedDoubleTimeBar`, `ConveyedLeaderBonus`); the interim move (`InterimMembers`); D2.401's time aboard toward a later Motion attempt (R65.15). Record lines "[F8]: 4-6-7 squad G1 boards vehicle G1 (D6.4)" and "... unloads from vehicle G1 (D6.5)" in place of "X moves to ..."; the move-vehicle action's schema takes `load`, `unload`, `delay`, `units`, `turret`, `leave`, and `take`; the load and unload lists name the hex first.

**New in a record:** `embarking` on a First Fire attack (the (un)loaders, `unloading`, `unarmoredConveyance`), absent from every older attack, whose (un)loaders were never targets; `interim` on every new unload step, gating the four-MF cap and the Double Time bar, so an older unload replays as it did; `leave` and `take` on a vehicle step.

### 10.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-unload-fire` | The German MPh: the vehicle panel offers "[F8]: vehicle G1 ([F8])"; unloading "4-6-7 squad G1 in [F8]" commits "[F8]: 4-6-7 squad G1 unloads from vehicle G1 (D6.5)", and the step carries `interim: true` (read from the file). The DFF on the unloaders is asserted by `AnUnloadFromATruckInTheOpenTakesFfnamAndFfmo`, not walked. Played. |
| `p65-board-after-move` | A 4-6-7 moves two hexes and boards a halftrack as its last step. Made, not played. |

Rules: `UnloadersTakeFfnamAndFfmoOnlyFromAnUnarmoredConveyance`, `ALoadFailsOnAPinOrABreakAndAnUnloadDoesNot`, `AfterUnloadingFourMfNoDoubleTimeAndNoLeaderBonus`, `AStackBoardsAtTheEndOfItsMoveWithItsLeadersIpc`, `ASwStaysAboardOnlyAHalftrackOrCarrierAndTimeAboardCounts`. Play: `AnUnloadFromATruckInTheOpenTakesFfnamAndFfmo`, `AFailedLoadLeavesThePinnedSquadOnFoot`, `ASquadBoardsAtTheEndOfItsMove`, `UnloadedUnitsMoveNextOnFourMfWithoutDoubleTime`, `AHalftrackKeepsTheLmgItsSquadLeavesAboard`, `TheMoveVehicleDescriptorTakesALoadAndAnUnload`; `PlayPagePass26Tests` and `BacklogPass26Tests` brought to the new record lines.

## 11. Riders and Bailing Out: 65.4 (increment 2e)

Built on 2026-10-24, 01:43 to about 02:12, in one commit (ce468692) by a building agent to a brief (`brief_2e.md`), its diff read and its games played in the Studio by the session.

### 11.1 Departures from the frame, first

Each to a row of the backlog's section 71.1:

- a Rider's +1 in CC is decided in Rules, and CC with PRC is still unwired (as in 2c); Riders add no FP to an OVR;
- a pin or break on a Rider by an ordnance, mortar, FT, MOL, or DC hit leaves it aboard; only the IFT path makes it Bail Out;
- a friendly Vehicular-Sized Entrance is always false, and a stream is read from the terrain's name only;
- no CC counter on a Bail Out into an enemy-occupied Location (no such condition, as in 2d);
- the Bail Out NMC has no Heat of Battle, no leadership DRM, and no Wound Severity dr for a SMC;
- a vehicle entering from off board with Riders is not checked for terrain.

Also reported: 10 CA1861 errors in `BacklogPass65Tests.cs` from 2b to 2d (inline constant arrays), which the strict build would refuse; fixed in 2f. **Found in the Studio and fixed by the session** (older than the pass, from 2026-10-01): the vehicle's entry buttons read "Propose: enter bd03:D8:0", the raw Location id, against the hex UI rule; they read "[D8]" (`PlaceOf`).

### 11.2 What is built

`ScenarioA1RiderRules`: `Capacity` by class (14 PP; a Carrier 8; another AFV one SMC; a truck none), `DateBar` (Russians from 1942, everyone from 1943; no year refuses), `CapacityBar`, `TerrainBailOut` (woods, orchard, a building, rubble, a Water Obstacle, bocage, unless on a road hexside, in Bypass, or in a stream; B13.43), `TurretBailOut`, `CombatBailOut` (a pin or break, D6.23), `Check` (the NMC), `WeaponFate` (1 to 3 kept, 4 to 6 malfunctioned, an X# SW eliminated, all if the Rider breaks), and `PrepFireCounter` (R65.16, R65.17). The projector places Riders as `ContainmentRole.Rider` and unloads either role. Mounted Fire's Rider multipliers (half, a quarter once the AFV moved or is in Motion; no SW, D6.22). The DEFENDER's MPh fire at the Location the Riders bailed into, with FFNAM. `GamePlanner.Riders.cs`. The page: the load's "as Riders (14 PP, N left)" (`#vehicle-load-riders`), setup's "as a Rider" (`#place-rider`), the records.

**New in a record:** `riders` on a load step (only when true; absent is a Passenger load, so older games replay as before); the event `rider-bailed-out` (unit, vehicle, at, cause, the NMC roll, each SW's `fate`), through the reader, writer, projector, PlayRecords, and ReplaySteps. No older record holds a Rider: no planner path created one before.

### 11.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-riders-woods` | The Russian MPh, 1943: the load fieldset reads "as Riders (14 PP, 14 left)", locked on for the T-34; boarding commits "[E8]: 7-0 leader R1, 4-4-7 squad R1 board vehicle R1 as Riders (D6.2, D6.4)"; the T-34 starts (dice 1 1), and entering [D8] (woods) proposes the Bail Out in [E8] "since the AFV moves through terrain Riders may not ride through (D6.21)"; committed with NMC dice 2 2 each: "[E8]: 4-4-7 squad R1 Bails Out of vehicle R1, ...; its NMC against 7: passed (D6.24)". Played. |
| `p65-riders-fire` | A PzKpfw IIIH's 4-6-7 Rider with a LMG under a Russian attack (D6.23, D6.24). Made, not played. |

Rules: `RiderCapacityFollowsTheVehicleClass`, `RidersAreRussianFrom1942AndEveryonesFrom1943`, `RidersBailOutInWoodsButNotAcrossARoadInBypassOrInAStream`, `ATurretedAfvsTcaChangeMakesItsRidersBailOut`, `ABailOutsNmcAndItsSwDrs`, `ARiderFiresAtHalfAQuarterOnceItsAfvMovedAndNoSw`. Play: `RidersBoardAT34In1943AndReplay`, `AGermanRiderIsRefusedIn1942`, `ARiderBailsOutAsItsTankEntersWoods`, `APinnedRiderBailsOutAndChecksItsSw`.

## 12. Carriers: 65.5 (increment 2f)

Built on 2026-10-24, 02:23 to about 02:50, in one commit (49be757b) by a building agent to a brief (`brief_2f.md`), its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

- **The Studio game puts an 8-0 aboard, not a squad:** D6.81's 4 PP are for SMC, SW, and ammunition; a squad is 10.
- **The 2 VP were already counted** (`HasInherentCrew`); the fault was a double count once the HS left, since the check knew a crew counter only. Fixed by `LeftItsVehicle`.
- **No live game (470) or store game (528) holds the Bren carrier,** so the BU refusal needs no time gate; and the Carrier's reads take its class from the loaded catalog (section 4, the boundary review's finding 12).
- **Seen, not fixed:** a Stun or Shock still writes `asl:bu` on a Carrier, and the page's `StarshellFirers` reads the raw condition. Backlog 71.2.
- **Found in the Studio:** the crew unload's proposal named the HS by its raw id ("Vehicle-beb23db0c4cc-ub-crew disembarks ..."). Fixed in Round D (section 18.2).
- The agent also fixed 2e's 10 CA1861 errors (`fix_ca1861_2f.py`) and built the Play and Rules test projects strictly outside the repository: 0 warnings, 0 errors.

### 12.2 What is built

`ScenarioA1CarrierRules` (R65.18): `IsCarrier`; `ButtonUpBar` and `SetupBar` (never BU, D6.84); `CrewExposed` (its crew, HS, and Passengers always CE and Vulnerable, stunned or shocked included, where the attack carries `prc`); `PassengerCapacity` (4 PP, none with a mortar, FT, or 6 FP AAMG as MA; D6.81); `InherentHalfSquad` (the nationality's 2-4-7 HS unless it has a FT or a 76mm mortar or tows a Gun; D6.82); `CrewUnloadBar` (D6.83's quicker unload). `LeavingCrew` places the HS on Survival, Abandonment, and dismount. The quicker unload is `crew: true` on an unload step: the HS is created aboard, the Carrier Abandoned, then the unload. The page's `#vehicle-unload-crew`. Victory counts the inherent HS's 2 VP once.

**New in a record:** `crew` on an unload step (only when true). An older record places what its `instance-created` names.

### 12.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-carrier-unload` | The British MPh: the vehicle panel shows "its inherent HS too, Abandoning it (D6.83)" and no BU control; unloading the crew commits "[F8]: 2-4-7 half-squad B1 unloads from vehicle B1 (D6.5)". Played. |
| `p65-carrier-fire` | A German 4-6-7 and a PzKpfw IIIH at a Bren carrier with an 8-0 aboard (D6.82, D6.84). Made, not played. |

Rules: `ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned`, `ACarrierCarriesFourPassengerPpAndEightRiderPp`, `ACarriersCrewLeavesAsAHalfSquadUnlessItHasAFtOr76mmMortarOrTowsAGun`, `ACarriersInherentHalfSquadIsWorthTwoVpAndOneOnceItLeaves`. Play: `AKilledBrenCarriersSurvivingCrewIsABritishHalfSquad`, `ABrenCarrierIsNeverButtonedUpAndItsStunnedCrewStaysExposed`, `AStunnedBrenCarriersCrewTakesTheCollateralAttack`, `ABrenCarriersInherentHalfSquadUnloadsAndAbandonsIt`.

## 13. The crew's own acts: 65.6 (increment 2g)

Built on 2026-10-24, 02:55 to about 03:22, in one commit (3bd63d52) by a building agent to a brief (`brief_2g.md`), its diff read and its game played in the Studio by the session.

### 13.1 Departures from the frame, first

Each to a row of the backlog's section 71.1, or judged right:

- re-crewing has no FFNAM window, so D5.43's failure on a pin or break is not built (the bar asks Good Order and unpinned before it);
- MG Removal takes a MG MA or a BMG of 3 FP or less only; a heavier MG and a mortar are refused; a malfunctioned MG stays so as the LMG;
- a self-destruction wreck is refused while PRC are aboard (they unload first), since they would be eliminated with it: right;
- an Abandonment in Motion is allowed (D5.41 bars a vehicle that moved or fired only);
- the Armor Leader goes with the crew on a voluntary and a failed-TC Abandonment, not on Survival or a Recall (D5.341); fix Round B carried him on Survival too (section 18.2);
- "own crew" is the id ending `-{vehicle}-crew` (moved into Rules at Round C, `ScenarioA1CrewActsRules.IsOwnCrew`);
- a re-crewed vehicle's movement is ended for the MPh;
- D5.8: the projector takes conditions and the leader from the recorded `instance-created`, so older crews keep theirs; new plans on the Survival, TC, and Recall paths may now add a Prep or Final Fire counter and the leader, intended sweep lines at the gate.
- **Found in the Studio:** the proposal named the units the plan creates by raw id ("Abandon-65e58cf27955-dh-crew Abandons ...", "the LMG abandon-65e58cf27955-dh-mg"). Fixed in Round D.

### 13.2 What is built

`ScenarioA1CrewActsRules` (R65.20): `AbandonBar` (an unmoved, unfired vehicle in its own MPh), `SelfDestructionBar` (D5.411: a non-burning wreck with no CS, or its MA and every MG malfunctioned), `RecrewBar` (Infantry of the side in the Location at the start of its MPh; not the Player Turn the old crew left, not a Recalled crew into its own AFV), `TemporaryCrew`, MG Removal (`RemovedMgDefinition`, `RemovedCondition`; a halftrack's MG of 3 FP or less as the nationality's LMG, the mount Disabled until the LMG returns with a re-crew), `CrewFireCounter` (D5.8's Prep or Final Fire counter), and `CrewInsideFireBar`. `ContainmentRole.Crew` holds a crew inside its vehicle, and `GameState.At`, `Aboard`, and `ObjectIndex` (`CrewOf`) leave it out of the Location as they do Passengers. The actions `asl.game.abandon-vehicle` and `asl.game.recrew-vehicle`; `GamePlanner.CrewActs.cs`; the page's "Vehicle crews" section (`VehicleCrewActions.razor`), with the Icon Lab's coverage regenerated for its two buttons. A crew that re-crews keeps its id, its Stun +1 (D5.34), and the Armor Leader stored with it (D3.43).

**New in a record:** the events `vehicle-abandoned` (the crew, its `fireCounter`, `destroyed`, `removed`) and `vehicle-recrewed` (the unit, `temporary`, `returned`). Every path is new, so no older record changes; the projector takes an older crew's conditions from its recorded `instance-created`.

### 13.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-crew-acts` | The German MPh, 1943: the "Vehicle crews" section offers "Propose: Abandon vehicle G2 in [F10]" and "... G1 in [F8]", the self-destruction choice, and "Remove a halftrack's MG as its LMG (D6.631)". Abandoning the SPW 251/1 with Removal commits "[F10]: 1-2-7 vehicle crew G1 Abandons vehicle G2, taking its MG as the LMG (D6.631) (D5.41)"; the Russian DEFENDER's pass ends the window; the tank's Abandon is still offered. Re-crewing a Player Turn later is asserted by `ACrewReCrewsItsTankAPlayerTurnLaterAndItsArmorLeaderReturns`, not walked. Played. |

Rules: `ACrewAbandonsOnlyAnUnmovedUnfiredArmedVehicleInItsOwnMph`, `OnlyAnInherentCrewDestroysItsVehicleAndAWreckWaitsForThePassengers`, `AReCrewingIsBarredTheTurnTheCrewLeftAndForARecalledCrew`, `ALeavingCrewIsMarkedPrepOrFinalFireAfterItsVehicleFiredOrMoved`, `AnArmoredHalftracksMgOfThreeFpOrLessIsRemovedAsTheNationalitysLmg`. Play: `AVoluntaryAbandonmentMalfunctionsOrWrecksTheVehicleAsTheCrewLeaves`, `ACrewReCrewsItsTankAPlayerTurnLaterAndItsArmorLeaderReturns`, `AHalftracksMgIsRemovedAsItsCrewAbandonsItAndReturnedWhenItReCrews`; `TheProjectorRefusesAReCrewingInThePlayerTurnTheCrewLeftAndAnAbandonmentAfterFire` was added at Round C.

## 14. The Immobilization TC, Crew Survival, and Brew Up: 65.7 (increment 2h)

Built on 2026-10-24, 03:28 to about 04:00, in one commit (9b118ad3) by a building agent to a brief (`brief_2h.md`), its diff read and its games played in the Studio by the session.

### 14.1 Departures from the frame, first

- A crew failing its TC also takes Hazardous Movement (D5.5's text): right.
- "A crew" is a vehicle with an upper-case CS# (D5.1: an unarmed vehicle's Inherent Driver is never a crew): right.
- The +1 for a broken crew is never set, since no crew inside was broken; ruling R65.31 later kept a broken crew inside (backlog 71.2).
- The red CS# is not applied on the IFT's Vehicle line; no catalog vehicle has one.
- No Residual FP skip is built: survivors spend all their MF, so it cannot arise.
- **Slips:** the agent's first Bash call changed into the repository, and it ran `games_2h.py`, which writes under the boards folder; it deleted the three games at once, and the session ran the script again.
- **Seen, outside the task:** IFT fire at a crew that mans no Gun was refused (ruling R8.3), so the pass's vehicle crews could not be fired at: **ruling R65.28**, built in 2k. Also, to backlog rows: the existing ordnance TC still lets an unarmed truck's Inherent Driver take one; after a failed mortar TC the AFV still enters the hit's IFT attack for a crew Collateral Attack; D9.3's exception for units Abandoning or Surviving is not built.

### 14.2 What is built

`ScenarioA1CrewSurvivalRules` (R65.21): `SurvivalDrm` and `CrewSurvival` (+1 for a broken, Stunned, Shocked, UK, or Recalled crew, none for an Abandoned vehicle, one member for the ordnance, mortar, FT, MOL, DC, and Vehicle-line paths); `Survival`; `TaskCheck`; `SecondCause` (D5.5, D8.11, D8.22: a Direct Fire ordnance shot at an Immobile target recomputed on an Original TK DR of 5; a Burn, an Eliminated, or an automatic Shock calls for the TC); `BurnsRed` (D5.7: -1 to the Final TK DR for the burn determination only). The TC's first cause on the mortar path (C1.55) and the IFT's Vehicle line; Survival on the Vehicle-line kill of an armed vehicle; PRC Survival on the mortar path (2b's leftover); the condition `asl:hazardous-movement` on survivors and TC-Abandoned crews, removed at the phase's end (D5.6); Brew Up's Survival on every destruction path but CC (D9.2).

**New in a record:** `shocked`, `abandoned`, and `immobile` on the vehicle facts, written with `prc` and taken from the record at replay; `secondCause` on an ordnance shot; on a mortar's hit `crewCheck`, `abandoned`, and `prcSurvival`. Each gates its rule: an older attack carries none and resolves as it did.

### 14.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-tc-mortar` | The Russian PFPh: the 50mm mortar hits the SPW 251/1 at [D10] (To Hit 2 1, IFT 2 2, TC 6 6): "... hull hit on the IFT (C1.55): 2 FP, DR 2, 2 - 1 (all af 4 or less) - 1 (open topped) = 2 against the K/# DR 2: immobilized; crew TC DR 12 against 8: Abandoned (D5.5) ...". Played. |
| `p65-tc-second`, `p65-tc-second-t34` | A 45mm AT Gun and a T-34 at an immobilized PzKpfw IIIH, for the second cause. Made, not played. |

Rules: `AnImmobileTanksCrewTakesTheTcWhenAnOriginalFiveWouldHaveKilledIt`, `CrewSurvivalTakesPlusOneForABrokenStunnedOrShockedCrewAndNoneForAnAbandonedVehicle`, `ARedCsNumberBurnsOneDrHigherAndChangesNothingElse`. Play: `AMortarImmobilizesATankAndItsCrewFailsTheTcUnderHazardousMovement`, `AnAirBurstWrecksAShockedHalftrackAndItsCrewSurvivesWithThePlusOne`, `AShockedTankKilledByAnAtShotRollsItsCrewSurvivalAtPlusOne`, `ASurvivingRiderIsUnderHazardousMovementAgainstTheNextAttackOfThePhase`.

## 15. Captured vehicles: 65.8 (increment 2i)

Built on 2026-10-24, 04:10 to about 04:45, in one commit (92ea61dc) by a building agent to a brief (`brief_2i.md`), its diff read and its games played in the Studio by the session.

### 15.1 Departures from the frame, first

- **An unarmed vehicle's capture uses the new event for new plans** (cause `unarmed`); an older R11.16 record replays as before (tested). An intended sweep change at the gate.
- **A21.21's "removed" SMC captor rides as the crew;** a squad as a Temporary crew is refused (it Deploys first). Round D reworded the refusal under ruling R65.32 (section 18.2).
- **The capture attempt captures where the attack would eliminate or burn;** the CE and Immobile assumptions apply only with the pass's `unmanned` fact.
- **Not built, to backlog rows:** entering at capture in the CCPh (D5.42's exception); the one-man turret of D5.343; the start dr in a Motion attempt.
- **Control needed no change:** a vehicle captured by the new event carries no `asl:captured`, which ruling R24.5 reads.
- **Seen, outside the task:** an advance into any Location that holds a crew was refused (ruling R24.3), so a vehicle crew beneath its Abandoned AFV blocked both the advance and a capture: **ruling R65.29**, built in 2k. The vehicle CC panel's vehicle choice read "dt (german)", against the hex UI rule; fixed in Round D.
- **A slip:** the agent sent a heredoc once; the hook refused it and nothing ran.

### 15.2 What is built

`ScenarioA1CapturedVehicleRules` (R65.22): `AutomaticCaptureDue` and `AutomaticCaptor` (an Abandoned AFV captured as the CCPh ends with no Personnel of its side there), `CaptureAttemptBar` and `CaptureAttemptPhaseBar` (a capture attempt in vehicle CC at -1, with A11.61's CE and Immobile DRM), `TemporaryDriver` (A21.21), `Manning`, `SquadBar`, `Halved` (MP), `StartDrm` and `StartStops` (4 or less moves, 5 no move, 6 or more Bogs, +1 for a Russian captor), `RedMp`, `WeaponPenalty` (captured B# and ROF, red To Hit numbers with Case H), `SmcFireBar`, `SmcMoveBar`, and the SMC crew's Stun and Recall (D5.343). `UnitInstance.TemporaryCrew`. Control: a manned captured vehicle holds Control for its captor (A26.12). The page's `.propose-vehicle-cc-capture`.

**New in a record:** the event `vehicle-captured` (vehicle, `side`, `cause`, `driver`); `unmanned` on a vehicle CC's facts, written by every new vehicle CC; a captured weapon's `capturedPenalty`. Older captures keep R11.16's captured and Abandoned state and R24.5's Control (`ACapturedTrucksTemporaryDriverRunsItAndAnOlderCaptureReplaysAsBefore`). Round C added `UnitInstance.CapturedFrom`, computed by the projector from the `vehicle-captured` record (section 4).

### 15.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-capture-cc` | The Russian 4-4-7 advances from [F9] into [F8], an Abandoned PzKpfw IIIH with a German HS; in the CCPh "Propose: Infantry attempt to capture the Abandoned AFV (A21.2)" with dice 3 3 commits "[F8]: vehicle in [F8] is captured by the Russian side (A21.2)". Played. |
| `p65-capture-afv`, `p65-capture-truck` | An Abandoned PzKpfw IIIH captured as the CCPh ends and crewed by a Russian crew; a truck captured with a Temporary Driver. Made, not played. |

Rules: `AnAbandonedAfvIsCapturedAsTheCcphEndsWhenNoneOfItsSidesPersonnelIsThere`, `ACaptureAttemptCapturesTheAbandonedTankWhereAnAttackWouldEliminateIt`, `ATemporaryCrewsPenaltiesFollowWhoMansTheVehicle`, `ATemporaryCrewsOrdnanceFiresOnTheRedNumbersWithCaseHAndALowerBreakdown`, `ASmcCrewIsWoundedByAStunAndEliminatedByARecall`, `AMannedCapturedVehicleHoldsControlForItsCaptor`. Play: `AnAbandonedPanzerIsCapturedAsTheCcphEndsAndCrewedByARussianCrewUnderTheTemporaryPenalties`, `ACaptureAttemptInCcTakesAnEscortedAbandonedPanzer`, `ASmcTemporaryCrewIsWoundedByAStunAndEliminatedByARecall`, `ACapturedTrucksTemporaryDriverRunsItAndAnOlderCaptureReplaysAsBefore`; `BacklogPass11Tests` brought to the new event.

## 16. Wrecks and Scrounging: 65.9 (increment 2j)

Built on 2026-10-24, 04:51 to about 05:28, in one commit (c01a1eba) by a building agent to a brief (`brief_2j.md`), its diff read and its game played in the Studio by the session.

### 16.1 Departures from the frame, first

- **The scrounged LMG is not dismantled:** A9.8 gives a LMG no dm form, and D10.52 says "if possible". **Ruling R65.30** amends R65.23.
- **The IFT attacks only a wreck of the side fired at,** and only as the Location's single vehicle (R25.3 stood until 2k's A7.308); Infantry Target Type and mortar Area hits do not attack wrecks; a DC may name one. Backlog row, pass 110.
- **Fire at a wreck gains no Acquisition** (C6.5: Known enemy units): right.
- **The Random Selection of the scrounged SW** takes the highest die first, ties by mount order, not a re-roll. Backlog row.
- **Not built:** the American exemption from captured-use penalties, a mortar MA's dm form, and a Disabled condition for an AAMG that is not the MA. Backlog rows.
- **Seen, for the reviews:** the ordnance and FT/MOL vehicle choices read "dt (attacker-tank), a wreck", not hex-first; fixed in Round D.

### 16.2 What is built

`ScenarioA1WreckRules` (R65.23, R65.30): `AttackedAsVehicle`, `WreckKill`, and `WreckLineResult` (a non-burning wreck is a target of ordnance on the Vehicle Target Type, of the IFT's Vehicle line, and of a FT or MOL, where only a Burn counts and places a Blaze, D10.1); `CloseCombatBar` (CC at a wreck refused with its reason, pass 70's); `ScroungeBar`, `Scroungeable`, `Succeeds`, `SelectionNeeded`, `Taken`, `ScroungedDefinition`, and `ScroungedNationality` (Scrounging, D10.5 to D10.52); `BlazeId` (Round C). A wreck's TCA is its VCA. The projector accepts a burning `vehicle-wrecked` on a wreck. The RPh action `asl.game.scrounge`, the condition `asl:scrounged`, `GamePlanner.Scrounging.cs`, and the page's "Scrounging" section (`VehicleScroungeActions.razor`).

**New in a record:** `wreck` on both vehicle-target records (only when true); the event `vehicle-scrounged` (unit, vehicle, `dr`, `selection`, `taken`). Fire at a wreck and Scrounging were impossible before, and a wreck is a target only where an attack names it or carries the pass's fact, so an older attack whose Location held a wreck resolves as it did.

### 16.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-wreck-scrounge` | The Russian RPh: the scrounge choice offers "[F8]: 4-4-7 squad R1" and "[F8]: 2-3-7 half-squad R2"; with a dr of 2, "[F8]: 4-4-7 squad R1 scrounges vehicle in [F8] on a dr of 2: it salvages the LMG, its BMG; vehicle in [F8] is Scrounged, and 4-4-7 squad R1 is TI (D10.5)". Burning a wreck is asserted by `AT34BurnsAPanzerWreckThatAnEliminatingDrLeftAsItWas`, not walked. Played. |

Rules: `AWreckBurnsOnABurnAndNothingElseTouchesIt`, `ScroungingTakesAsManyFunctioningSwAsItsDrFromAWreckOrAnAbandonedVehicle`. Play: `AT34BurnsAPanzerWreckThatAnEliminatingDrLeftAsItWas`, `TwoSquadsBurnATruckWreckOnTheIftsVehicleLine`, `ARussianSquadScroungesAPanzerWrecksBmgAsAGermanLmgAndTheWreckIsScroungedOnce`.

The measurement after 2d to 2j (hand-over / proposal ms, warm runs): base 337/170, 359/209, 340/187; pass 65 232/112, 225/125, 217/107. No regression: the pass reads its PRC through the index, and its page fixes removed per-render scans.

## 17. What 2b and 2h left: a crew as a target and in CC, A7.308, D5.311 (increment 2k)

Built on 2026-10-24, 05:35 to about 06:05, in one commit (17820400) by a building agent to a brief (`brief_2k.md`), rulings R65.26, R65.28, and R65.29, its diff read and its games played in the Studio by the session. All four items were built.

### 17.1 Departures from the frame, first

- **A new recorded fact, `gunCrews`, on a fire attack:** Rules can tell a crew mans a Gun only when the attack names the Gun, which vehicle MG fire, Canister, DC, and OVR do not. The four `LiveFire` builders fill it, and it is taken from the record at replay. Right.
- **The CC package refused every crew** (`asl.a1.cc.unit-outside`); lifted for a crew whose manning fact is not true, the crew now a MMC in the CC calculator, Casualty Reduction eliminating it (A1.123).
- **A7.308 for the IFT only;** the ordnance Infantry Target Type and the mortar keep `vehicles-beyond-kia`; a tie goes to the first listed vehicle, with no re-roll. Backlog rows.
- **D5.311 simplified:** the attack's own DRM with the Air Burst -1, no CE DRM; no Stop on the crew breaking, no ELR, no Heat of Battle. Round A rebuilt the broken crew under ruling R65.31 (section 18.2); the rest are backlog rows.
- **One existing test renamed as the intended change** (`TheFirePackageRefusesAGunsCrewWithOtherUnitsAsATarget`).
- **Slips:** one `grep -rln` over the tests in Bash and two `cd /tmp`; nothing changed.
- **Fixed by the session:** seven literal `"asl:crew"` comparisons on the Play page (setup's MMC count and Gun manning, Panzerfaust firers, Starshell firers, mopping up, `MayFire`) missed `asl:vehicle-crew`; they take `ScenarioA1Definitions.IsCrew`. 2a's sweep had covered Rules, Play, and Units, not the page.

### 17.2 What is built

R65.28: a crew that mans no Gun is an ordinary Infantry target on the IFT and the ordnance Infantry Target Type, alone or with other units; a Gun's crew keeps R8.3 (`ScenarioA1FireCalculator`, `GunCrews`). R65.29: such a crew is advanced against and fights in CC as Infantry (`ScenarioA1AdvanceCalculator`, `ScenarioA1CloseCombatRules`, `ScenarioA1CloseCombatCalculator`); a Gun's crew keeps R24.3. A7.308 (R65.26): the IFT's Random Selection of the vehicles beyond the column's highest KIA#, one dr per vehicle, the highest taking the DR, with ruling R25.3's one-vehicle refusal lifted where the attack carries `prc`. D5.311 (R65.26): an OT AFV's crew under Air Bursts attacked as unarmored, on the hit's DR with the Air Burst DRM (`UnprotectedCrewOutcome`).

**New in a record:** `gunCrews` on a fire attack at a Location holding a Gun's crew; `randomSelectionDr` and `notSelected` on a vehicle the selection reads. Each only turns a refusal into a plan or rides beside the `prc` fact, so no recorded game changes.

### 17.3 The games and the tests

| Game | What it shows |
|---|---|
| `p65-crew-target` | The Russian PFPh: the 4-4-7 at [F10] fires at [F8], which holds a German vehicle crew and a squad: "Every check passed" (R8.3 refused this before). Played. |
| `p65-crew-cc`, `p65-three-trucks`, `p65-air-burst-crew` | A Russian 4-4-7 advances onto a German vehicle crew; three trucks under a LMG's 2 FP; a SPW 251/1 in woods under a mortar's Air Burst. Made, not played. |

Rules: `ACrewThatMansNoGunIsAnOrdinaryTargetAndAGunsCrewKeepsRulingR83`, `RandomSelectionPicksTheTrucksA2FpAttackAffects`, `AnUnprotectedCrewThatBreaksIsPlacedBeneathAndOneEliminatedIsGone`. Play: `AGermanVehicleCrewAloneInTheOpenIsAttackedAsInfantry`, `AGermanVehicleCrewWithASquadIsAttackedWithIt`, `AGunsCrewWithASquadIsStillRefusedAsATarget`, `ARussianSquadAdvancesOntoAGermanVehicleCrewAndTheCcResolves`, `AnAdvanceOntoAGunsCrewIsStillRefused`, `ARussianHalfSquadsFireMeetsThreeTrucksAndRandomSelectionPicksTwo`, `AnAirBurstBreaksAnOtHalftracksCrewWhichRoutsBeneathAndAbandonsIt`; `ScenarioA1OrdnanceTests` brought to R65.28.

## 18. The five reviews, their fixes, and the Studio check

Five read-only reviews ran in parallel on 17820400, 06:14 to 06:25 (briefs in `brief_reviews.md`): two referees (R1, Passengers, 65.1 to 65.3 and 2k; R2, Riders to wrecks, 65.4 to 65.9 and 2a), a table player (R3) who read the 26 games' records, a UI and Blazor review (R4), and a Rules boundary and memory review (R5). Beside them the gate's strict stage (`gate.sh 65 d0a8f4cd strict`, 06:13 to 06:18) passed: 0 warnings, 0 errors, and its formatting left no test file changed (a0906fbf). In sum: no BLOCKER, 8 MAJOR (R1 4, R2 1, R4 2, R5 1), about 45 MINOR, and about 20 NOTE. No review found an older record whose replay changes, nor a name a side may not know. The findings as reported are `fixes_referee1.md`, `fixes_referee2.md`, `fixes_table.md`, `fixes_ui.md`, and `fixes_boundary.md` in the pass's folder; `brief_fixes.md` ordered the fixes in four rounds, one agent each.

### 18.1 What departs from the plan's entry and the frame, first

- **The frame's section 4 was wrong on one point:** it listed an Unconfirmed Kill among the four cases with no Crew Survival to build. C7.42 (p. 177) makes the UK's wreck in the RPh with no CS, so none is right (R2, checked and confirmed). The PRC of such a wreck are eliminated with it (backlog 71.1, the user's reading).
- **Three rulings were made at the reviews** (F31): R65.31 (D5.311's broken crew stays inside until the RtPh), R65.32 (A21.22's squad exchange not built, and the refusal says so), and R65.33 (Scrounging's dr).
- **R65.33 was revised at Round B:** its first wording, from R2's finding 10, gave the Scrounging dr the leadership modifier; A.10 (p. 43) says the ∆ marks the actions leadership does not modify. The fixing agent did not change the code and said why; the session read the page at 07:16 and revised the ruling.
- **The fixes' projector checks are stricter than the build's:** after Round C the projector refuses at replay what the planner refuses. The session loaded the ten p65 games that hold play events in its Studio: each replays to its last event.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 60's narrower check as the model (the user, 2026-10-15: the need is reassessed pass by pass): section 18.4.

### 18.2 Fixed

**Round A** (06:28 to about 06:52, fbca6ac1; R1's findings 1 to 11):
- a loader is back on foot when the attack its loading drew destroys the vehicle (D5.43 EX; MAJOR);
- a Shock, a Stun, or an Abandonment stops only the crew's Specific Collateral Attack, and Riders are still attacked (D.8 and the table's note 1, D.8A; MAJOR);
- Hazardous Movement's -2 and Mud's cushion kept off the PRC (A4.62, D.6, D.8B, D5.31; MAJOR);
- a Rider's AFPh fire is a quarter, not an eighth (D6.63; MAJOR);
- a Shocked AFV's Passengers spared; Air Bursts reach an OT AFV's BU Passengers (D5.311); the crew's reduced CE DRM from above (D5.31, D6.61); open PRC claim Height Advantage apart from the Infantry's cover (B10.31); the OVR's share by vehicle class (D7.11);
- ruling R65.31: the crew is created inside, broken, in the Crew role, with the Armor Leader dormant; the AFV Stops and is Abandoned only at the rout; a new bar `play.vehicle-crew-broken` on its fire and movement; its broken Passengers must rout beneath too. Mud's cushion is stripped only from CE and BU Passengers, since an open PRC takes the TEM (D6.7).
Each gated on the pass's facts; no new record fact. Rules 25 of 25 by method, Play 13 (one new test's dice fixed, then 1 of 1).

**Round B** (06:54 to about 07:15, adf1081a; R2's findings 1 to 9, 11, 13, 14):
- Riders Bail Out on every turret turn: an ordnance or Canister shot, CMG fire, a passed Motion attempt (D6.21; MAJOR);
- a Carrier's crew may Remove its MG (D6.83); a leaving crew keeps its Stun +1 and Armor Leader on the TC, Survival, and Carrier-unload paths (D5.34, D3.43); survivors placed beneath spend their MF (new fact `ended` on `instance-moved`, only when true; D5.6, D6.9);
- the TC's second cause not on a MG's To Kill, and after a passed Possible Shock (D5.5); Crew Survival counts the Stun +1 apart (D5.6, D5.34); an Abandoned AFV's absent crew is not attacked; a Shocked or Stunned crew may not Abandon (C7.42);
- Riders may not board or set up in terrain that bars them (B13.43); a broken Rider is never pinned; a Rider needs a Morale Level; a Wounded SMC Temporary crew Wounded again is eliminated (A17.2);
- R65.33 revised (above).
Rules 17 of 17, Play 22 (one test's facing fixed, then 1 of 1). The agent named a risk: the verifier re-resolves records carrying `prc`, so a pass 65 game recorded before these fixes could now disagree; the played p65 games were replayed at Round C's check (section 18.1).

**Round C** (07:17 to about 07:50, 4e45b63a; R5, R2's finding 12, R3's boundary line):
- R5's MAJOR: the projector's five new arms take Rules verdicts built from the planner's own bars (`ScenarioA1VehicleProjection.BoardsAfterMove`, `AbandonStands`, `RecrewStands`, `ScroungeStands`, `CaptureStands`, `DriverStands`; `ScenarioA1CrewActsRules.AbandonCarries` and `RecrewCarries` move the Stun +1 and the Armor Leader), and one Blaze name (`ScenarioA1WreckRules.BlazeId`);
- rule decisions moved from Play into Rules: Passengers' exposure, "moved", the OVR's Passengers, the aboard rout (`ScenarioA1RoutCalculator.AboardRout`), the own crew, "fired", "turreted", the Bail Out Location, the capture attempt's phase, a SW kept aboard;
- "captured" from `UnitInstance.CapturedFrom` (`ScenarioA1CapturedVehicleRules.Captured`), and D5.42's Player Turn from `GameState.AbandonedThisPlayerTurn`: new state properties computed from records every game holds (section 4);
- memory and performance: the phase found by walking back; one OVR scan per plan; `CrewInside` and the fire group read the index; `KeepPassengers` one walk; absent false flags not written; nine static helpers made instance members to read the index.
R5's findings 2 to 6, 9 to 11, 13 to 18 fixed; 7 already done; 8 a Rules comment; 12 a paragraph of section 4; 19 and 21 unchanged; 20 Round D's. Rules and Play by method, all passing after one test fixed.

**Round D** (07:56 to about 08:35, ca0e14fa; R4, R3, the two known faults):
- the page: drafts cleared on commit and on a change of game or view (R4's MAJOR 1); "[F8]: LMG of 4-6-7 squad G1" on `.vehicle-leave` (R4's MAJOR 2); no crew inside offered as a firer; a just-wrecked target not kept; disabled controls say why; `@key` on the new lists; the Rider box only for Personnel with a vehicle holder;
- the page's pre-filters through new planner members over Rules verdicts (`BoardsAsRiders`, `KeepsSwAboard`, `MayBeCaptureAttempted`, `WrecksFiredAt`, `DirectorOffered`);
- record lines: a rout beneath, a Carrier Abandoned by its HS's unload, a failed load (D5.43), the PRC on the IFT line, a vehicle CC round's arithmetic, the Bail Out's dice; roll words; the capture texts;
- the two known faults: a unit the plan creates is named by its counter ("its inherent 2-4-7 half-squad", "a 1-2-7 vehicle crew", "the LMG"), never a raw id; every vehicle choice names its hex first, through `UnitOptions`;
- the index holds units by id, wrecks by Location, and active entity ids, so `WordsAt` and the wreck lists read in O(1);
- ruling R65.32's refusal; `PlayPagePass65Tests` (seven page tests: `TheCrewActsSectionNamesTheHexSendsItsChoicesAndClearsThem`, `TheScroungingSectionNamesTheHexSendsTheUnitAndClearsItsPick`, `TheRiderCheckboxIsLockedOnATankAndTheLoadSendsRiders`, `TheCarrierCrewCheckboxUnloadsItsHalfSquadAndTheRecordSaysItIsAbandoned`, `TheRoutBeneathButtonNamesTheHexAndItsRecordSaysBeneath`, `TheRecordsSayABailOutsDiceAFailedLoadAndATemporaryStart`, `TheCaptureAttemptNamesTheAfvAfterItsHexSendsCaptureAndShowsItsArithmetic`).
R4's findings 1 to 12 fixed; R3's 1 to 6, 8, 10 to 13, and 15 fixed. Rules 4 of 4, Play 10 of 10, MapStudio by method, all passing.

After each round the fixing agent built the test projects it touched strictly outside the repository: each succeeded.

### 18.3 Not fixed: a row each in the backlog

The backlog's section 71.2 has each: an Armor Leader not carried on the `instance-moved` leaving paths (R2 3, in part); Hazardous Movement on an AFV crew's CE line and an unarmored vehicle's Vehicle line; a truck's Vehicle line at the Original DR + 1, to check; a Carrier's `asl:bu` after a Stun or Shock and the Starshell list's raw read; an unarmed truck's Inherent Driver taking the ordnance TC; the absent crew after a failed mortar TC; the broken crew's +1 for Survival; Victory's `HasInherentCrew` scan; the per-render scans older than the pass; the Carrier's class read from the loaded catalog (R5 12); `IsBurning` per wreck and `CargoNotes` (R5 19, 21); A21.22's squad exchange (R3 9; ruling R65.32); the forced automatic capture (R3 14). Section 71.1 has D5.311's "treated as unarmored" (R1 7), the Bail Out's ELR (R3 7), and D6.82's "specified as towing" (R2 15).

### 18.4 The Studio check

At 08:36 to 08:41, in my Studio on port 6670 on the build of ca0e14fa, the games written fresh by their scripts first. Its scope, decided under the user's word: the new controls at 1920, 1024, and 320; the view of the side that must not see; a real Enter key on a proposal and its commit; the raw-id fix; the measurement.

- **1920,** `p65-crew-acts`: the Abandon proposal reads "Its 1-2-7 vehicle crew Abandons vehicle G2 in [F10], ... the crew Removes its MG and takes it as its own LMG ..." (no raw id); the focused Confirm button and the real Enter key committed it: "[F10]: 1-2-7 vehicle crew G1 Abandons vehicle G2, taking its MG as the LMG (D5.41, D6.631)".
- **The Russian view** of that game names no German crew, crew inside, or Armor Leader.
- **1024:** the Scrounging section (title "Choose the unit that scrounges") and the Rider checkbox (title "This vehicle takes no Passengers: units board it as Riders (D6.2)") sit inside the page, with no horizontal scroll.
- **320:** the Scrounging section, "Rout 4-6-7 squad G1 in [F8] beneath vehicle G1", and the Vehicle crews section (the self-destruction choice, the MG box, both Abandon buttons) sit inside the page, with no horizontal scroll (document width 320).
- **Not done,** as the scope allowed: the five widths in each side's view for every control; a whole Player Turn at 320; the Replay page (backlog 71.1).

## 19. As built

**The pass in commits,** on `feature/asl-backlog-pass-65`, cut from d0a8f4cd (the head of pull request 33, which merged into main as 7040c768 with the same tree): the frame (7306e834); 2a (0ccb9f4b); 2b (7baee874); 2c (c7ece170); 2d (1edd653c); 2e (ce468692); 2f (49be757b); 2g (3bd63d52); 2h (9b118ad3); 2i (92ea61dc); 2j (c01a1eba); 2k (17820400); the reviews (a0906fbf); fix Rounds A to D (fbca6ac1, adf1081a, 4e45b63a, ca0e14fa); these documents. Each increment was built by an agent to a brief, its diff read, its game played in the Studio by the session, and its record facts added to `SKIP_JSON` and its games to `OWN` in `pass65.conf`.

**What the plan's entry asked, and where it went.** The plan names 63 inventory rows; the inventory named 83 for pass 65. After the pass 30 of them are built (two of them with a deviation) and 53 moved to the pass that takes what is left: pass 105 23, pass 110 10, pass 70 8, pass 100 3, pass 120 3, pass 75 2, pass 135 2, pass 150 1, pass 175 1. The nine tasks are built as sections 7 to 17 say, each with its departures; the moves of ruling R65.24 and every departure have a row in the backlog's section 71.1, and the findings not fixed in 71.2. Sixteen rows of other passes that the pass's code touched have a note of it. The inventory's rows D6.8 to D6.84 no longer say there is no Carrier (section 5, item 4), and the prompt's line on replay after a catalog bump is corrected in place (section 5, item 5).

**The rulings:** R65.1 to R65.33, decided under the user's word of 2026-10-23 and open to a veto; R65.26 to R65.30 made at the tasks, R65.31 to R65.33 at the reviews, R65.30 amending R65.23 and R65.33 revised at Round B. They are in the Passes Plan's section 5.

**The catalog** is `asl-scenario-a1` 1.18.0 with vocabulary asl@1.19.0 (ruling R65.3); the four rule packages were pinned again. A game recorded at 1.17.0 replays and fires (ruling R65.4).

**What a record gained** (each absent from every older record, or written only when true, and taken from the record at replay): on a fire attack `prc`, `shocked`, `abandoned`, `immobile`, `wreck` on its vehicles, `embarking`, `gunCrews`, `firers[].mounted`, and the OVR's `passengers`; on an ordnance shot `prc`, `wreck`, `secondCause`, and a mortar's `crewCheck`, `abandoned`, `prcSurvival`; on a vehicle step `riders`, `interim`, `crew`, `leave`, `take`; on `instance-moved` `ended`; on a vehicle CC `unmanned`; in resolutions `prcAttacks`, `prcSurvival`, `randomSelectionDr`, `notSelected`; the new events `rider-bailed-out`, `vehicle-abandoned`, `vehicle-recrewed`, `vehicle-captured`, and `vehicle-scrounged`; the actions `asl.game.abandon-vehicle`, `asl.game.recrew-vehicle`, and `asl.game.scrounge`; the conditions `asl:hazardous-movement` and `asl:scrounged`; the containment role Crew. New state properties computed from records every game holds: `MovementState`'s three (2d), `UnitInstance.TemporaryCrew` (2i), `UnitInstance.CapturedFrom` and `GameState.AbandonedThisPlayerTurn` (Round C).

**The tests:** `ScenarioA1Pass65RulesTests`, `BacklogPass65Tests`, and `PlayPagePass65Tests`, each increment's and each round's run by method only (the user's rule of 2026-10-23); the Rules, Play, Units, and MapStudio projects were not run whole, which is the gate's.

**The measurement (R65.25),** on `p65-perf-tw` (`dl-tractor-works-02` at revision 1316), warm runs, hand-over and proposal in ms. The base's runs: 276/135, 265/157, 355/147 at 2a, and from 241 to 359 and 140 to 209 at the later checks. After 2a, 2c, and 2j the pass was level or faster (sections 7.3, 9.3, 16.3). At the whole-pass check a proposal took about 1,000 ms against the base's 150, a seeming regression. Bisected with Studios built at Rounds B and C in a worktree outside the repository (`asl-gate-work/wt-65b`, removed after), each at base level; a `dotnet-trace` sample of my Studio gave 280 ms a proposal, most of it the audit sink's append (`FileAuditSink.WriteAsync`, unchanged by the pass) and the planner 40 ms. Measured again with my Studio alone: hand-over 268, 229, 342 ms; proposal 118, 167, 158 ms, against the base's 280 to 320 and 147 to 164 ms. **The seeming 6x regression was the measurement:** three Studios and a profiler ran at once, all appending to the one 7.8 MB audit file in the shared boards folder. The lesson (measure one Studio at a time) is in `brief_build.md`.

**The time:** the frame 22:30 to 22:58 on 2026-10-23; the build 22:59 to 06:12 (about 7:15 by the clock, the eleven agents' building about 5:45) against the frame's 15:35 and the plan's 7:00; the reviews 06:14 to 06:25; the fixes 06:28 to 08:35; the Studio check and the measurement 08:36 to 08:49; then these documents. The time log has each row.

**The strict build** (`gate.sh 65 d0a8f4cd strict`) passed on 17820400 with 0 warnings and 0 errors; the fix rounds' strict builds of the test projects they touched each succeeded. It ran again on the tree after Round D (ca82b646, 2026-10-24 08:55) and passed, 0 warnings and 0 errors.

## 20. The gate

Not run. The user's word of 2026-10-23, renewed on 2026-10-24 ("Proceed without my intervention."), ends at the gate: the gate, the push of the pass's branch, its merge, and any closing commit are each the user's to start. `pass65.conf` names the pass's 26 recorded games (`OWN`), the older games to sweep beside them (`SWEEP_EXTRA`), the record facts whose keys the proofs skip (`SKIP_JSON`: `prc`, `passengers`, `interim`, `embarking`, `shocked`, `abandoned`, `immobile`, `unmanned`, `gunCrews`), `BOTH_SKIP` with the catalog's identity, and `ABSENT="IsCrew"`. The baseline of d0a8f4cd was made at the cut (0738528e38fe2d41: 528 older games). What the gate should expect to see differ, as intended changes: the catalog's identity in every state line (`BOTH_SKIP`); a surviving crew placed as a vehicle crew in new plans; new plans on the Survival, TC, and Recall paths carrying a Prep or Final Fire counter and the Armor Leader (D5.8); an unarmed vehicle's capture by the new event; refusals the pass turned into plans (a crew as a target and in CC, an AFV in positive TEM, more trucks than the KIA#, Riders, a Passenger's fire); and older games with a load or unload whose state lines carry `MovementState`'s new properties.
