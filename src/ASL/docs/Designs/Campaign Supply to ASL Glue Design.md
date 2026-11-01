# Campaign Supply to ASL Glue Design

Date: 1 November 2026

Status: proposed design. Nothing is built. Implementation waits for the user's approval of this design and of the rulings in section 10. The user said on 2026-10-31 that the Campaign's supply mechanics may be redesigned; this design therefore states what the Campaign's supply model must provide, and proposes that model, rather than adapting to the prototype's current packages.

Source analysis: [Campaign Supply and ASL Rulebook Supply Hooks Analysis](<../Campaign Supply and ASL Rulebook Supply Hooks Analysis.md>), which lists the rulebook's supply rules, their pages, and their state in the code. Section 8 of that analysis gives the findings this design rests on.

## 1. Purpose and scope

The glue connects the Campaign's supply state to an ASL scenario and back:

- **Out:** when a Situation is committed, the Campaign's supply state for the forces in it becomes terms of the Scenario Card: SSR tokens and the Ammo Vehicles and Ammo Dumps the card sets up.
- **Back:** when the ASL game ends, the facts the game record holds about ammunition become spending and loss events in the Campaign, applied once.
- **Between:** a Campaign step at every Situation boundary decides which forces are supplied, short, or isolated. It takes the place of the module Campaign Games' Refit Phase isolation steps (O11.605 to O11.6072, P8.6052, Q9.6052, T15.6072), whose texts we do not have. It follows their pattern as the eASLRB index shows it (Perimeter, Pocket, isolation decided between scenarios) without claiming their rules.

In scope: the Campaign supply model the glue needs (section 3), the Supply Determination step (section 4), the outbound terms (section 5), the inbound report (section 6), ownership (section 7), disclosure (section 8).

Out of scope: tracing supply during an ASL scenario (no ASL rule does it), per-shot accounting in the Campaign, OBA until pass 150 builds it, and any fuel rule inside ASL play (section 5.4).

## 2. Principles

1. **The Campaign decides the terms; ASL decides their effects.** Which SSRs a card carries is a Campaign rule. Every effect of an SSR during play is decided by a Rules calculator. Play gathers facts and writes events; it decides neither. This is the Rules boundary of AGENTS.md applied across the two games.
2. **Every game is a card** (the user's ruling of 2026-09-29). The handoff produces a Scenario Card. The card path validates its SSR tokens (`ScenarioA1SetupCalculator.RuleDiagnostics`, reached from `ScenarioCards`); the raw start path does not (`GamePlanner.Start` copies `start.specialRules` unchecked). Today's Goodwood handoff (`SituationHandoffs.Prepare`) uses the raw start path with no SSRs; the glue moves it onto a card.
3. **Supply acts at the boundary.** Every ASL supply hook acts at scenario start (A19.131, E10.1, E10.6, C1.211). Supply state therefore crosses between the games only at commitment and at the result, never during play.
4. **One owner for each effect.** An ammunition shortage is charged once: as an A19.131 SSR in the scenario, or as a Campaign attrition between Situations, never as both for the same boundary. Spending is charged by the boundary rule of section 6.2, never also per shot.
5. **Only published ASL rules in play.** The glue uses tokens for rules the rulebook prints. Where no rule exists (vehicle fuel), the Campaign acts on its own side of the boundary, through the order of battle, rather than inventing an ASL rule (section 5.4).

## 3. The Campaign supply model the glue needs

### 3.1 Per-formation supply state

The glue needs, for each formation that can be committed, a supply state with discrete steps that map one to one onto ASL terms:

| Field | Values | Meaning |
|---|---|---|
| `Ammunition` | `Full`, `Low`, `Exhausted` | The formation's ammunition step |
| `Fuel` | `Full`, `Low`, `Exhausted` | The formation's fuel step; only for formations with vehicles |
| `Trace` | `Supplied`, `Isolated` | The result of the last Supply Determination (section 4) |
| `Pocket` | pocket id or none | The pocket an isolated formation belongs to |
| `IsolatedSince` | boundary number or none | The boundary at which it was first found isolated |

Batteries (OBA) carry their own `Ammunition` step, read by C1.211 once OBA is built.

Facilities carry stock: a receiving point inside a Situation's board area holds zero or more ammunition packages, which can become Ammo Dumps (section 5.3).

### 3.2 What a package means

The prototype's custody, transport, routes, reservations, and convoys stay as they are (the Atlas analysis, sections "A concrete supply loop" and "Advanced logistics"). Only the meaning of a delivered package changes:

- Today a package enables an authored roster and is then "issued" or "consumed", with no continuing level.
- Proposed: issuing one ammunition package to a formation raises its `Ammunition` by one step, to at most `Full`. Issuing a fuel package raises `Fuel` the same way. A package held at a receiving point inside a board area, and not issued, is stock that can become an Ammo Dump.

A package's compatible recipients stay as the analysis requires: an ammunition package for infantry does not become tank fuel.

### 3.3 Why steps, not quantities

ASL's hooks are switches: a side has Ammunition Shortages or it does not; a battery is Plentiful, normal, or Scarce; a vehicle has its Ammo Vehicle or not. A Campaign quantity would have to be cut into those switches anyway. Three steps carry exactly what ASL can use, keep the Campaign's arithmetic small, and stay visible to the player as symbols on the map.

## 4. The Supply Determination step

The Campaign runs it at every Situation boundary: before a Situation is committed, and after its result is applied. It is deterministic, takes no die roll, and is recorded as one Campaign event with its inputs' revision.

1. **Trace.** From each formation's position, trace along the Campaign route graph to a supply source of its side (a depot, a rear base, or an authored board-edge entry). A segment is open unless it is enemy-controlled or physically blocked under the Campaign's control and interdiction rules. Enemy proximity alone does not block a segment (the Atlas analysis, "Route availability and enemy threat"). A formation that traces is `Supplied`.
2. **Group pockets.** Formations that cannot trace are `Isolated`. Isolated formations connected to each other by open segments form one pocket. A pocket shares any stock held at a receiving point inside it.
3. **Deliver.** Shipments that reached a receiving point since the last boundary are received; packages issued to formations raise their steps (section 3.2). A pocket can be resupplied only by stock already inside it or by an authored delivery that does not use a ground route (an air delivery, which is a Campaign event, not E9).
4. **Attrition.** A formation isolated at this boundary and at the previous one loses one `Ammunition` step and, if it has vehicles, one `Fuel` step. A formation isolated for the first time loses nothing yet: its issued allowance is its buffer (the Atlas analysis: "A blockage does not instantly disable all dependent units").
5. **Terms.** For the Situation about to be committed, compute the outbound terms (section 5) from the states the steps above produced.

The step answers the same question as the module Refit Phases: which forces are cut off, and what that does to the next scenario. It is our Campaign rule, written for our Campaign, and it is labeled as such wherever it is shown.

## 5. Outbound: Supply Terms on the Scenario Card

### 5.1 The Supply Terms record

The Campaign produces one `SupplyTerms` record per committed Situation:

- `Version`, `CampaignId`, `SituationId`, the Supply Determination event it was computed from, and its hash;
- for each side, the committed formations with their states (section 3.1);
- the receiving points inside the board area, with their stock and their hex;
- the card terms derived from them (sections 5.2 to 5.5), each with the formation states that produced it.

The record travels with the handoff (`SituationHandoff.CampaignContext`) and enters the handoff hash, so a different supply state yields a different game identity. The handoff version moves from `campaign-goodwood-handoff-1` to a new version; `CampaignContextSummary.Read` reads both.

### 5.2 Ammunition Shortage (A19.131)

A19.131 applies to "a side's on board OB". The existing token is `ammo-shortage:<side>` (pass 72, ruling R72.10).

Proposed Campaign rule: a side's card carries `ammo-shortage:<side>` when the formations holding more than half of that side's committed on-board squads (by squad-equivalents) are at `Ammunition` `Low` or `Exhausted`, or are `Isolated` and have been for two boundaries. The SSR's text names the formations that caused it.

A formation-level shortage would need a token of the form `ammo-shortage:<side>:<group>`. A19.131 does not print a partial OB, so that form is a deviation and is not proposed now (ruling G2 in section 10).

`Exhausted` has a further Campaign effect: the formation cannot be committed to an attack mission (a Campaign rule), though it may defend.

### 5.3 Ammo Dumps and Ammo Vehicles (E10)

E10's benefit is for vehicles with a circled B# (D3.71), which our catalog prints only under A19.131. ASL has no on-map resupply for infantry. So:

- **Ammo Dump (E10.6).** When a receiving point inside the board area holds an ammunition package compatible with a committed formation's vehicles, and that side's card carries `ammo-shortage:<side>` (so its vehicles have circled B#), the card sets up an Ammo Dump at the receiving point's hex. Proposed token: `ammo-dump:<side>:<location>`, with the location written as the card writes setup locations.
- **Ammo Vehicle (E10.1).** When a committed formation has a vehicle that E10.11 allows (15 PP or more, 20 PP for armament of 100mm or more) and is assigned an issued ammunition package, the card names it an Ammo Vehicle. Proposed token: `ammo-vehicle:<side>:<unit id>`.
- Otherwise a held package has no ASL counter. It stays Campaign stock, and the scenario's result decides whether it survives (section 6.2).

The token formats are Rules decisions. W-E10's wiring row already lists "a card's Ammo Vehicle and Ammo Dump SSR" among its sites (claimed by `wire-work-1`, Codex). The tokens' parsing and validation belong in the Rules project (with `ScenarioA1SetupCalculator.RuleDiagnostics` and `ScenarioA1AmmoVehicleCalculator`), by a rule claim on E10's setup part; their application at setup belongs to the wiring. This design proposes the formats to that claim and does not decide them.

### 5.4 Fuel

No rule in Chapters A to H governs vehicle fuel; the one published model is Kampfgruppe Peiper's Out-of-Gas DR, SSR KGP13, whose text we have not read (analysis section 5.2). Until it is read and a ruling made, fuel stays on the Campaign's side of the boundary:

- `Fuel` `Low`: Campaign movement of the formation's vehicles is reduced (a Campaign rule); no ASL effect.
- `Fuel` `Exhausted`: the formation's vehicles cannot be committed to the Situation; they stay in the Campaign at their position. The rest of the formation can be committed.

No ASL token is proposed for fuel. An Out-of-Gas token modeled on SSR KGP13 is a later option (ruling G4).

### 5.5 OBA ammunition (C1.211, A25.33)

When OBA is built (Pass 150), a committed battery's `Ammunition` step becomes Plentiful (`Full`, for a side whose national default is Plentiful, A25.33), normal (`Full` otherwise, or `Low` for a Plentiful-default side), or Scarce (`Low` otherwise, or `Exhausted`). The token form waits on the OBA pass.

### 5.6 The card

The handoff builds a Scenario Card for the Situation: its sides, boards, OB placements (from the accepted deployment, as today), and its SSRs, one `ScenarioCardRule` per term with its number, text, tokens, and cited rules (A19.131, E10.1, E10.6). The game starts from the card, so its tokens pass the card checks. The card is stored with the Campaign's Situation, so the same supply state always yields the same card.

## 6. Inbound: the Supply Report

### 6.1 What the game record already holds

At the end of a scenario, the game state the Units projector replays carries:

- `LowAmmoVehicles`: vehicles with Low Ammo at the end;
- `TurnAmmoSupplies`: each Ammo Vehicle and Ammo Dump, its owner, and whether it is still `Available` (E10.3's Ammunition DR of 12 makes it not);
- depleted special ammunition (C8.9) on each Gun and vehicle;
- the Unit Replacements A19.131 caused, as fire follow-up events;
- each unit's status (eliminated, captured, active) and each piece of equipment's possessor (captured weapons, A21.11).

The glue reads these facts; it re-decides nothing.

### 6.2 The boundary expenditure rule

Proposed Campaign rule, applied once when the result is applied:

1. Each committed formation that fired in the scenario loses one `Ammunition` step.
2. A formation with more than half of its committed vehicles in `LowAmmoVehicles` at the end loses one more step (to at least `Exhausted`).
3. An Ammo Dump or Ammo Vehicle that is not `Available` at the end consumes its package. One that is `Available` returns its package to the formation or receiving point, unless the dump's Location is enemy-controlled at the end, in which case the enemy side gains the package (Campaign capture).
4. Losses, Unit Replacements, and captured equipment update the Campaign roster through the result path the transition already plans ("Returning ASL battle outcomes to campaign adjudication").

### 6.3 Exactly once

The report is keyed by the ASL game id and the revision of its last event. Applying a report whose key is already applied is refused; a report for a later revision of a game already applied is refused unless the Campaign explicitly reopens that result. Reopening a screen applies nothing. This matches the Atlas analysis's requirement that "engagement results must be applied once, with a recorded result identity".

## 7. Ownership

| Part | Owner | Notes |
|---|---|---|
| Supply model, Supply Determination, the outbound rules (5.2, 5.4), the expenditure rule (6.2) | Campaign (`src/Campaign`) | Campaign rules, labeled as authored; no ASL rule is decided here |
| `SupplyTerms` and the card it produces | Campaign (`LimboDancer.Campaign.PilotHost`, `SituationHandoffs`) | Uses `ScenarioCards` to build and check the card |
| `ammo-shortage:<side>` parsing, validation, effects | Rules (built, pass 72) | No change |
| `ammo-dump:` and `ammo-vehicle:` parsing and validation | Rules, by a rule claim on E10's setup part | Formats proposed in 5.3 |
| Applying the dump and vehicle tokens at setup | Track 3, W-E10 (`wire-work-1`) | Already listed in W-E10's sites |
| Plentiful and Scarce OBA | Rules, Pass 150 | Waits on OBA |
| Reading the game record for the report | Campaign, through the Units projector's replay | Read only |
| The campaign context panel in the Studio | MapStudio | Shows the card's supply SSRs and their Campaign causes |

The Campaign references ASL's Play and Units projects today (for the handoff). It needs no reference to Rules beyond what Play already exposes.

## 8. Disclosure

- An SSR is public in ASL: both players read the card. A side's `ammo-shortage` is therefore known to the opponent in the scenario, as it is in any printed scenario.
- The Campaign's hidden information stays hidden: the card names the formations that caused a side's own shortage only in that side's Campaign view; the shared card text says only that the side's on-board OB has Ammunition Shortages.
- An Ammo Dump is a counter set up on the map under the card's setup rules, with whatever concealment the card allows; its existence is not disclosed through the Campaign.
- Pockets, traces, and Supply Determination results follow the Campaign's visibility rules: a side sees its own; an enemy pocket is known only as far as the Campaign's intelligence rules disclose it.

## 9. Example

Arnhem (Situation 10), British side. The Campaign finds the British 1st Airborne formations at the bridge `Isolated` in one pocket at boundary 3, as they were at boundary 2; attrition moves their `Ammunition` from `Low` to `Exhausted`. They hold all the British on-board squads, so the card carries SSR 1, `ammo-shortage:allied` (the token names the card's side id; the Campaign's cards use `allied` and `german`, with the nationality `british`), text "The British on-board OB has Ammunition Shortages (A19.131); cause: isolated since boundary 2." There is no compatible stock at a receiving point in the pocket, so no Ammo Dump. The formations cannot be committed to an attack mission; this Situation is a defense, so they are committed. At the end, every British formation that fired loses one step (already `Exhausted`), British losses and Replacements return to the roster, and German captured weapons go to the German side.

## 10. Rulings needed

Each carries a proposal.

| Id | Question | Proposal |
|---|---|---|
| G1 | Adopt per-formation steps (`Full`, `Low`, `Exhausted`) for ammunition and fuel as the Campaign's supply state, with packages raising steps | Yes; the prototype's custody and transport stay as they are |
| G2 | A19.131 per side or per group | Per side, as printed, with the majority rule of 5.2; a group form only by a later ruling and a Rules claim |
| G3 | The attrition rule (one step after a second isolated boundary) | Yes, as in section 4, step 4 |
| G4 | Fuel in ASL play | None for now; `Exhausted` vehicles are not committed; read SSR KGP13 before any Out-of-Gas token |
| G5 | The expenditure rule of 6.2 | Yes, as written |
| G6 | The `ammo-dump:` and `ammo-vehicle:` token formats | Propose them to an E10 setup rule claim; the claim decides |
| G7 | Move the handoff onto a Scenario Card | Yes, under the ruling that every game is a card |

## 11. Tasks, in order

| Task | Work | Depends on |
|---|---|---|
| S1 | Rulings G1 to G7 | This design |
| S2 | Campaign supply state and Supply Determination (sections 3, 4), with tests over the 17 Situations | G1, G3 |
| S3 | The handoff builds a Scenario Card (section 5.6), with no supply terms yet; replay of existing handoffs unchanged | G7 |
| S4 | `SupplyTerms` and `ammo-shortage:<side>` on the card (5.1, 5.2) | S2, S3, G2 |
| S5 | Rule claim on E10's setup tokens (Rules), then W-E10's wiring applies them | G6; the claim board |
| S6 | Ammo Dumps and Ammo Vehicles on the card (5.3) | S4, S5 |
| S7 | The Supply Report and the expenditure rule (section 6), exactly once | S4, G5 |
| S8 | Fuel commitment rule (5.4) | S2, G4 |
| S9 | OBA terms (5.5) | Pass 150 |

S2, S3, S4, S7, and S8 are Campaign work. S5 is a rule claim and a wiring claim on the board. Each task's out-of-scope items get backlog rows when it is approved.

## 12. Memory and performance

Answers to the [guidelines'](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>) checklist:

- **Work added.** Supply Determination runs once per Situation boundary in the Campaign: a route-graph search per formation (17 Situations, at most a few hundred formations), not per ASL step. Card building runs once per handoff. The report reads the final state once. Nothing is added to any ASL Play step, render, or read path.
- **Ownership, keys, lifetime.** `SupplyTerms` is immutable, keyed by its Determination event and hash, and stored with the Situation. The report is keyed by game id and revision. No cache is added.
- **Disclosure, snapshots, seams.** Section 8. The ASL game state gains nothing new: the tokens use existing state (`SpecialRules`, `TurnAmmoSupplies`, `LowAmmoVehicles`).
- **Counters and fragments.** An Ammo Dump is one more counter at setup; it uses the counter layer's existing fragments.
- **Checks.** Campaign tests over all 17 Situations and both sides for Determination and terms; a card round trip; a report applied twice and refused; ASL replay of existing handoff games unchanged (S3). The merge gate is needed only where an ASL project changes (S5).
- **Tests.** Campaign tests build each Situation's inputs once per run; any new test project joins a CI share.
- **Benefit and noise.** No performance claim is made; the work is outside the measured paths.
- **Records.** Results, limits, backlog rows, and any outside build outputs are recorded with each task.

## 13. Acceptance

- Every committed Situation has a `SupplyTerms` record and a Scenario Card whose supply SSRs follow from it; the same state always produces the same card.
- A side whose majority of committed squads is short or long isolated plays under A19.131; a side that is not does not.
- No ASL rule is decided outside the Rules project; no Campaign rule is presented as an ASL rule.
- An Ammo Dump or Ammo Vehicle appears only where E10 gives it an effect.
- A scenario's result changes Campaign ammunition steps, stock, and rosters exactly once.
- Existing handoff games replay unchanged.
- Fuel never reaches ASL play until ruling G4 says otherwise.

## Related documents

- [Campaign Supply and ASL Rulebook Supply Hooks Analysis](<../Campaign Supply and ASL Rulebook Supply Hooks Analysis.md>)
- [Campaign Atlas Supply and Shared Map Integration Analysis](<../Campaign Atlas Supply and Shared Map Integration Analysis.md>)
- [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<../Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- [ASL E10 Ammo Vehicles Claim Design](<ASL E10 Ammo Vehicles Claim Design.md>)
- [Formation to ASL Map Alignment Design and Implementation Plan](<../Formation to ASL Map Alignment Design and Implementation Plan.md>)
