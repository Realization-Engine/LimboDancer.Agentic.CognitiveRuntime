# Campaign Supply and ASL Rulebook Supply Hooks Analysis

Date: 31 October 2026

Status: analysis and proposal. Nothing here is built by this document, and no ruling is made. The proposed mapping in section 6 needs the user's approval before any design or code follows.

## 1. Question and conclusion

The campaign layer (`src/Campaign` and the HTML/JS prototype under `prototypes/europe-hex-map`) models supply: finite ammunition and fuel packages, custody, convoys, routes, receiving points, and isolated forces. The question is whether the ASL rulebook has hooks that this campaign supply state can drive when a Situation becomes an ASL Scenario Card.

It does, but not in the form of a supply line. Chapters A to H have no rule for a unit being "out of supply". Supply reaches an ASL scenario in three ways:

1. Ammunition states that a scenario special rule (SSR) switches on for a side (section 3).
2. Resupply on the map: Ammo Vehicles and Ammo Dumps, which also exist only by SSR or DYO purchase (section 4).
3. The Campaign Game chapters of the historical modules, which decide isolation and replenishment between scenarios. Their text is not in the eASLRB (section 5).

Vehicle fuel has no ASL rule at all (section 5.2).

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

### 5.2 Fuel

No ASL rule governs vehicle fuel. The scan found 21 mentions on 15 pages. All are historical notes (vehicles lost to fuel shortage, for example pages 481, 519, and 522), the flamethrower rule above, aircraft recall (E7.31's note), or napalm. A campaign fuel shortage has no rule to drive. It can only reach an ASL scenario through the order of battle, for example by withholding vehicles or setting them up Immobile by SSR. That would be a rule the project writes, not one the rulebook prints, and it needs the user's ruling.

## 6. Proposed mapping

The campaign already says it should use "a published campaign expenditure rule" at a mission boundary until tactical combat reports actual use, and that it must not charge both. The mapping below is that rule's ASL side.

### 6.1 Campaign state to the Scenario Card

| Campaign state at commitment | ASL hook | Notes |
|---|---|---|
| A force that is isolated, or whose ammunition allowance is below a threshold | A19.131 SSR on that side's on-board OB | The threshold is a campaign parameter, to be authored and labeled as such |
| An ammunition package delivered to a receiving point inside the board area | E10.6 Ammo Dump at that hex | The receiving point already has a hex in the board supply binding |
| An ammunition package assigned to a formation with circled-B# vehicles | E10.1 Ammo Vehicle, with a vehicle of the formation chosen under E10.11 | Rare in the current catalog, since no vehicle prints a circled B#; it becomes common under A19.131 |
| Artillery ammunition allowance | C1.211 Plentiful or Scarce Ammunition for the battery | Waits on OBA (Pass 150); A25.33 sets the American default |
| Fuel shortage | No ASL rule; an OB change written by the campaign | Needs a ruling and a label as manufactured |

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

1. Whether to obtain and read the module Campaign Game rules (O11, P8, Q9, R9, T15) as the model for isolation and refit between Situations.
2. The threshold at which a campaign ammunition state becomes an A19.131 SSR.
3. Whether and how a fuel shortage reaches the order of battle.
4. The order of work: W-E10's wiring and the OBA pass (150) both come before the full mapping works in play.

## Related documents

- [Campaign Atlas Supply and Shared Map Integration Analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>)
- [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- [LimboDancer.Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>)
- [ASL Rule Inventory A to E](<ASL Rule Inventory A to E.md>)
- [ASL E10 Ammo Vehicles Claim Design](<Designs/ASL E10 Ammo Vehicles Claim Design.md>)
