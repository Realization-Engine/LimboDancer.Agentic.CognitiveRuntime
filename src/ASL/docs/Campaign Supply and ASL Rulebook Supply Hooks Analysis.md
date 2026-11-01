# Campaign Supply and ASL Rulebook Supply Hooks Analysis

Date: 31 October 2026

Status: analysis and proposal. Nothing here is built by this document, and no ruling is made. The proposed mapping in section 6 needs the user's approval before any code follows. Section 8 (2026-11-01) gives the findings that the [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>) rests on, in place of the module Campaign Game rules we do not have.

## 1. Question and conclusion

The campaign layer (`src/Campaign` and the HTML/JS prototype under `prototypes/europe-hex-map`) models supply: finite ammunition and fuel packages, custody, convoys, routes, receiving points, and isolated forces. The question is whether the ASL rulebook has hooks that this campaign supply state can drive when a Situation becomes an ASL Scenario Card.

It does, but not in the form of a supply line. Chapters A to H have no rule for a unit being "out of supply". Supply reaches an ASL scenario in three ways:

1. Ammunition states that a scenario special rule (SSR) switches on for a side (section 3).
2. Resupply on the map: Ammo Vehicles and Ammo Dumps, which also exist only by SSR or DYO purchase (section 4).
3. The Campaign Game chapters of the historical modules, which decide isolation and replenishment between scenarios. Their text is not in the eASLRB (section 5).

Vehicle fuel has no rule in Chapters A to H; the one published model is Kampfgruppe Peiper's Out-of-Gas DR, SSR KGP13 (section 5.2).

No existing document maps the campaign's supply state onto these hooks. Section 6 proposes that mapping.

## 2. Method

- Source: the registered `eASLRB_v3_01.pdf`, all 716 pages (the whole-rulebook ruling of 2026-09-27). Page numbers below are physical PDF pages.
- A text scan of every page for supply, supplies, resupply, ammo, ammunition, Low Ammo, Ammunition Shortage, fuel, isolated, cut off, Supply Depot, Air Drop, Replenishment, Depletion, CPP, Campaign Game, Refit, and related terms. The index (pages 3 to 44) was then read for every supply-related entry, and each cited rule was read on its page.
- The repository: the Rule Inventory A to E, the Rules project, the two campaign supply analyses ([Campaign Atlas Supply and Shared Map Integration Analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>), [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)), the [LimboDancer.Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>), the [Formation to ASL Map Alignment Analysis](<Formation to ASL Map Alignment Analysis.md>), and `src/Campaign`.

Most of the scan's hits are not rules. Of 139 hits for "supply" on 85 pages, most are in Chapter H's vehicle and ordnance notes and the national notes (pages 336 to 606 and 643 to 672) and are historical description. The rules are the ones listed below.

## 3. Ammunition states set by SSR

These are the simplest hooks for the campaign: each is one SSR line on the Scenario Card, naming a side or a battery.

| Rule | Page | What it does | State in the code |
|---|---|---|---|
| A19.131 Ammunition Shortages | 86 | When an SSR cites a side's on-board OB as afflicted: an MMC that rolls an Original 12 on the IFT with its Inherent FP suffers Unit Replacement; all SW Original B#/X# (except DC) and Ammunition Depletion Numbers drop by one; vehicles and Guns have a circled B# one lower than their inherent B#, and those already circled start with a Low Ammo counter; no Fire Lanes; no effect on CC, Sniper, or Residual FP attacks. | Partly built (pass 72, rulings R72.10 to R72.12): `ScenarioA1UnitSubstitution` (the SSR token `ammo-shortage:<side>`), `ScenarioA1FireCalculator`, `ScenarioA1FireEligibility` (Fire Lane refusal), `ScenarioA1ArmorCalculator` (Depletion Number) |
| A21.11 Captured weapons | 88 | A captured Gun, SW, or vehicle has its B#/X# lowered by two, "due to lack of familiarity with the equipment and difficulty of resupply". A19.131's penalties do not apply to captured weapons. | Partly built |
| C1.211 Draw Pile, Plentiful and Scarce Ammunition | 163 | An OBA battery's Draw Pile of black and red chits is changed for Plentiful or Scarce Ammunition. | Not built; OBA is absent (Pass 150) |
| A25.33 American ammunition | 96 | American OBA batteries are assumed to have Plentiful Ammunition unless specified otherwise. | Not built (Pass 180) |
| C8.9 Depletion Numbers, C8.12 Ammunition Supply Chart | 177 to 179 | Special ammunition (APCR, APDS, HEAT, smoke, and so on) is available on a TH DR at or below its Depletion Number, which varies with the scenario's date. | Built; C8.12 with a deviation (the counter's own numbers by year, not the chart) |
| D3.71 Low Ammo B# | 201 | Vehicles that carried little ammunition have a circled B#; reaching it brings Low Ammo Depletion. | Not built as printed: no catalog vehicle has a circled B#. A19.131 produces the circled B# and the Low Ammo counter. |

A19.131 is the strongest hook. It already exists as an SSR token, it applies to a whole side, and its effects are entirely in the Rules project.

## 4. Resupply on the map

### 4.1 E10 Ammo Vehicles and the Ammo Dump (page 244)

- E10.1: an SSR may give a vehicle with a circled B# an Ammo Vehicle.
- E10.11: with no named type, any vehicle of 15 PP Passenger capacity or more (20 PP when the armament is 100mm or larger) serves. DYO cost: H1.49.
- E10.12: the Ammo Supply counter marks the Ammo Vehicle and reduces its Passenger capacity to zero.
- E10.2: when the Ammo Vehicle is in the same or an Accessible Location as the CE vehicle it supplies, that vehicle's circled B# is treated as a normal B12. One Ammo Vehicle supplies at most two identically armed vehicles.
- E10.3: after Low Ammo Depletion, the vehicle's supply is Replenished once it and the Ammo Vehicle have been TI in the same or Accessible Locations for a number of complete Game Turns. Each Replenishment ends with an Ammunition DR; an Original 12 means the Ammo Vehicle has run out.
- E10.31: special ammunition depleted under C8.9 is Replenished too.
- E10.4 and E10.5: an Immobilized Ammo Vehicle keeps its role; its supply can be transferred over four Game Turns; a burning Ammo Vehicle explodes.
- E10.6: an SSR or DYO purchase (H1.6) can create an immobile Ammo Dump, an Ammo Supply counter in a fixed Location, under the same rules.

State in the code: the E10 claim (Claude) built `ScenarioA1AmmoVehicleCalculator` and `ScenarioA1AmmoVehicleContract`, reviewed, with the `e10-eligibility` minigame. The wiring row W-E10 (capability "setup and work") is open for track 3. The Units projector already carries supply work effects (`GameProjector.WorkEffects.cs`) that the wiring moves onto the calculator.

E10.6 is the natural place for a campaign supply point that lies inside the ASL board area: the campaign's delivered ammunition package becomes an Ammo Dump at the receiving hex.

### 4.2 Other on-map rules

- D10.5 Scrounging (page 211): Infantry may salvage a functioning SW from a wreck or abandoned vehicle. Built. A minor form of resupply.
- A22.5 (page 89): a flamethrower that rolls an Original resolution DR of 10 or more has "run out of fuel" and is removed. This is a weapon malfunction, not supply.
- E9 Air Drops (page 242) drop paratroops only; there is no supply drop rule.
- E11 Convoys (page 245) govern motorized vehicles moving in a line on the map; they carry no supply rule, but a campaign convoy that enters the board area could appear as one.

## 5. What the rulebook does not supply

### 5.1 Campaign Game supply and isolation

The index and the pre-game checklist (page 678) cite the Campaign Game (CG) rules of the historical modules:

| Module | CG rule |
|---|---|
| Red Barricades | O11 |
| Kampfgruppe Peiper | P8 |
| Pegasus Bridge | Q9 |
| A Bridge Too Far | R9 |
| Tarawa | T15 |
| Solitaire ASL | S17 |

The index entries name the concepts: the Refit Phase (RePh); Isolated Area, Isolated Location, and Isolated Unit, with isolation determined in the RePh (for example O11.6057, P8.6057, Q9.6057); the Perimeter (O11.605); Campaign Purchase Points (CPP) and CPP Replenishment (P8.616, Q9.617, R9.616); Depleted units; Front Line Location; Strategic Location; Reinforcement Groups; the German ammunition rule in Kampfgruppe Peiper (P8.618); and the British ammunition shortage in A Bridge Too Far (RCG17), which is listed under A19.131.

This is the closest published model of what the campaign layer does: between scenarios, a side's position decides which units are isolated, and isolation and ammunition change the next scenario's order of battle and rules. The rule texts themselves are not in the eASLRB. Reading them needs the module rulebooks, which are not among the reference folders recorded so far.

The index gives each module's supply and isolation rules by number (read 2026-10-31):

| Module | Rules the index cites |
|---|---|
| Red Barricades | Perimeter O11.2, its determination O11.605, Perimeter markers O11.6054; Pocket O11.2, in the RePh O11.6056; isolation in the RePh O11.6057; Isolated Area O11.606 and O11.6072; Isolated Unit O11.6061 |
| Kampfgruppe Peiper | German ammunition P8.618; Isolated Area and Isolated Unit P8.6052; isolation in the RePh P8.6057 (upper levels); the Out-of-Gas DR, SSR KGP13; German vehicle Recall, SSR KGP12 |
| Pegasus Bridge | Isolated Location Q9.2; Isolated Unit Q9.6052; isolation in the RePh Q9.6057; setup of an isolated unit in the RePh Q9.62021 |
| A Bridge Too Far | the British ammunition shortage RCG17 (under A19.131). The index lists no Isolated Area, Location, or Unit entry for R9. |
| Tarawa | Isolated Location T15.2; Isolated Unit T15.6072; setup of an isolated unit in the RePh T15.6201 |
| Solitaire ASL | mission selection S17.22, the Campaign Roster S17.15, Retained S17.31, and the changes at Mission End (S17.4 to S17.7, including Rearming S17.623). No isolation or supply entry. |

Secondary claims not yet checked. A summary of these rules from a secondary source (retailer and forum pages, not the rule texts; given by the user on 2026-10-31) adds details that the index does not confirm: a Line of Supply traced free of enemy LOS and an Isolation Attrition Table in Red Barricades; resupply containers dropped onto the map in A Bridge Too Far; supply that follows control of the bridges in Pegasus Bridge; a garrison permanently isolated in Tarawa; and isolation tied to a Friendly Front Line in Solitaire ASL. Some of these conflict with the index (it cites O11.605, not "9.605", and no isolation rule for R9). None is used in this analysis until the module texts are read.

### 5.2 Fuel

Chapters A to H have no rule for vehicle fuel. The scan found 21 mentions on 15 pages. All are historical notes (vehicles lost to fuel shortage, for example pages 481, 519, and 522), the flamethrower rule above, aircraft recall (E7.31's note), or napalm.

The one published fuel rule the index cites is Kampfgruppe Peiper's Out-of-Gas DR, SSR KGP13: a scenario rule of that module, outside the Campaign Game rule P8. Its text is not in the eASLRB. It is the model to read before the project writes a fuel rule of its own. Until then, a campaign fuel shortage can reach an ASL scenario only through the order of battle (for example withholding vehicles or setting them up Immobile by SSR), or through an Out-of-Gas DR modeled on SSR KGP13. Either is a rule the project writes, and it needs the user's ruling.

## 6. Proposed mapping

The campaign already says it should use "a published campaign expenditure rule" at a mission boundary until tactical combat reports actual use, and that it must not charge both. The mapping below is that rule's ASL side.

### 6.1 Campaign state to the Scenario Card

| Campaign state at commitment | ASL hook | Notes |
|---|---|---|
| A force that is isolated, or whose ammunition allowance is below a threshold | A19.131 SSR on that side's on-board OB | The threshold is a campaign parameter, to be authored and labeled as such |
| An ammunition package delivered to a receiving point inside the board area | E10.6 Ammo Dump at that hex | The receiving point already has a hex in the board supply binding |
| An ammunition package assigned to a formation with circled-B# vehicles | E10.1 Ammo Vehicle, with a vehicle of the formation chosen under E10.11 | Rare in the current catalog, since no vehicle prints a circled B#; it becomes common under A19.131 |
| Artillery ammunition allowance | C1.211 Plentiful or Scarce Ammunition for the battery | Waits on OBA (Pass 150); A25.33 sets the American default |
| Fuel shortage | No rule in Chapters A to H; an OB change written by the campaign, or an Out-of-Gas DR modeled on SSR KGP13 | Needs a ruling and a label as manufactured |

### 6.2 Scenario results back to the campaign

At the scenario's end, the campaign records as spending and loss events, once each:

- an Ammo Vehicle or Ammo Dump that ran out (E10.3's Ammunition DR of 12), or that was destroyed;
- vehicles left with a Low Ammo counter;
- captured weapons (A21.11), which the capturing side gains;
- losses and Unit Replacements, including those caused by A19.131.

### 6.3 Where each decision lives

This follows the Rules boundary in AGENTS.md:

- The Campaign decides which SSRs and which Ammo Vehicles or Dumps a Scenario Card carries. That is a campaign rule, not an ASL rule.
- The Rules calculators (A19.131's, E10's, and C1.211's when built) decide every effect of those SSRs during play.
- Play gathers facts and writes events; it decides none of this. The Units projector applies the calculators' answers.
- The return path reads recorded game events; it re-decides nothing.

## 7. Open points

1. Whether to obtain and read the module Campaign Game rules (O11, P8, Q9, R9, T15) as the model for isolation and refit between Situations, and with them the secondary claims in section 5.1.
2. The threshold at which a campaign ammunition state becomes an A19.131 SSR.
3. Whether and how a fuel shortage reaches the order of battle, and whether SSR KGP13's Out-of-Gas DR is read as its model.
4. The order of work: W-E10's wiring and the OBA pass (150) both come before the full mapping works in play.

## 8. Findings for the glue between the two games

Added 2026-11-01. Without the module Campaign Game texts, the glue is designed from the ASL hooks above, the module rules' structure as the index shows it, and the code as it stands. The user said on 2026-10-31 that the Campaign's supply mechanics may be redesigned, so these findings also say what the Campaign's model must provide.

### 8.1 From the rulebook

1. **Every hook acts at scenario start.** A19.131, E10.1, E10.6, and C1.211 are all set by SSR or purchase before play. No ASL rule traces supply during a scenario. The glue therefore works at the Situation boundary, which is also where the module Campaign Games decide isolation (their Refit Phase rules, section 5.1).
2. **A19.131 is side-wide.** It applies to "a side's on board OB". A Campaign shortage in one formation of several cannot become an A19.131 SSR without either a side-level rule (most of the side is short) or a deviation (a partial OB).
3. **On-map resupply is for vehicles only.** E10's benefit applies to a vehicle with a circled B# (D3.71). Our catalog prints none; A19.131 creates them. An Ammo Dump has no effect on infantry. Infantry ammunition reaches ASL only through A19.131.
4. **ASL switches, it does not count.** A side has Ammunition Shortages or not; a battery is Plentiful, normal, or Scarce; a vehicle has its Ammo Vehicle or not. A Campaign quantity must be cut into such steps at the boundary.
5. **Fuel has no ASL hook** in Chapters A to H (section 5.2). A Campaign fuel state can act only on the order of battle until SSR KGP13 is read.

### 8.2 From the code

1. **The handoff bypasses the Scenario Card.** `SituationHandoffs.Prepare` (`src/Campaign/LimboDancer.Campaign.PilotHost/SituationHandoff.cs`) starts the ASL game with raw start arguments and no SSRs. On that path `GamePlanner.Start` copies `start.specialRules` unchecked; only the card path runs the card checks (`ScenarioA1SetupCalculator.RuleDiagnostics`). Under the ruling that every game is a card (2026-09-29), supply terms should reach ASL on a card.
2. **The tokens and state already exist in part.** `ammo-shortage:<side>` is a built card token (pass 72). The game state already holds `TurnAmmoSupplies` (each Ammo Vehicle or Dump, its owner, its availability, its dump Location) and `LowAmmoVehicles`. W-E10's wiring row lists "a card's Ammo Vehicle and Ammo Dump SSR" as a site, so the setup tokens for E10 are expected but not yet defined.
3. **The Campaign has no per-force supply level.** The prototype's packages move between custody states (rear, transit, depot, receiving point, issued) and enable authored rosters (`campaign/supply-state.js`); its own note says "Issued allowances are not per-shot consumption" and, for basic supply, "Resupply and combat expenditure are not simulated." There is nothing per formation for the glue to read.
4. **No return path exists.** The Campaign README says returning ASL battle outcomes to Campaign adjudication is outside the transition. The facts a return path needs are already in the replayed ASL state (8.2.2, plus losses, Replacements, and captured equipment).

### 8.3 What follows for the design

- The Campaign keeps a per-formation supply step for ammunition and fuel (`Full`, `Low`, `Exhausted`) and a trace result (`Supplied`, `Isolated`, with a pocket), decided by a Campaign Supply Determination at every Situation boundary. This is our counterpart of the Refit Phase isolation steps, written as a Campaign rule.
- At commitment the steps become card terms: `ammo-shortage:<side>` by a majority rule; an Ammo Dump or Ammo Vehicle only where E10 gives it an effect; fuel only through which vehicles are committed.
- At the result, the replayed game's ammunition facts become Campaign spending by one boundary rule, applied once.
- The Campaign decides the terms; the Rules calculators decide their effects; Play decides neither.

The [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>) works these out, with the rulings they need (its section 10).

## Related documents

- [Campaign Atlas Supply and Shared Map Integration Analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>)
- [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- [LimboDancer.Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>)
- [ASL Rule Inventory A to E](<ASL Rule Inventory A to E.md>)
- [ASL E10 Ammo Vehicles Claim Design](<Designs/ASL E10 Ammo Vehicles Claim Design.md>)
- [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>)
