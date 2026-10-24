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
- **R65.33:** the best Good Order leader's modifier is a drm on the Scrounging dr (D10.5's ∆, A.10), recorded on `vehicle-scrounged`.

**F25. Testing** (the standing rules, section 2, the user's rule of 2026-10-23).
- While a task is built, the Studio is its test, in a small game made for it (`games_2x.py`).
- A task's new Rules and Play tests are run by method, and only those.
- The whole projects run at the gate and nowhere else.
- When a task adds an example, an id, a kind, or a catalog definition, the tests that count or draw them are part of its blast radius (the lesson of PR 33's CI, `notes.md`).
