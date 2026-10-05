# ASL Unit Backlog Pass 28b Design

**Status:** Built. Pass 28b (The rest of Play) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 5, the fourth Studio pass.

**Date:** 2026-10-02

**Related documents:** the [Scenario A1 Backlog Pass 28b Review](<Scenario A1 Backlog Pass 28b Review 2026-10-02.md>), the plan's sections 13, 15.1 to 15.9, and 16.10 to 16.17, and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 23 and 43.

A Studio pass adds no rulings. Its design decisions are recorded here and in the plan's section 15.9.

## 1. Outcome

The Play panels no game pass touched are now components. The user answered four questions on 2026-10-01 and took every recommendation:

- **Backlog section 23.** Both additions are built here:
  - The Deploy control gives several SW to the second HS.
  - The records list shows DM gained, Failure to Rout eliminations, and SW transfers.

  Neither needs a new event or a rule change.
- **P2 candidates.** A09, C17, and R08 to R10 are extracted. A03 stays inline in A02, since it is one list.
- **RuleHelp's reach.** RuleHelp covers the long rule paragraphs still on the Play page and in this pass's panels. Those in components that passes 23 to 28 extracted go to pass 28c (backlog section 43).
- **RuleHelp's shape.** A one-line summary always shows. The full paragraph and its citations sit in a collapsed disclosure that keeps the paragraph's id.

The pass ran as one pass.

## 2. Components

The pass had 20 candidates: 19 are components, and one (A03) stays inline with its reason.

| Candidate | Component | Notes |
|---|---|---|
| A02 | `RoutActionPanel` | The obligations, already written for the view, and the units that may still rout; route and Low Crawl. A03 `RoutObligations` is its `<ul>`. |
| A04 | `SupportWeaponActionPanel` | Asks for `transfer`, `drop`, `recover`, or `dismantle`; the page maps each to its registered action and ignores anything else. A note says which button needs what; Recover is disabled for a SW a unit holds. |
| A05 | `RallyActionPanel` | The "Rally and Repair" heading is the page's, over the whole RPh group. |
| A06 | `RepairActionPanel` | SW and AAMG in one list, each labelled with its holder or vehicle. |
| A07 | `DeployActionPanel` | A checkbox per SW the squad possesses, for the second HS (section 3). |
| A08 | `RecombineActionPanel` | |
| A09 | `DmRetentionChoices` | A fieldset of choices sent with the phase's advance, not an action. |
| A10 | `ShockRecoveryAction` | Renders nothing when no vehicle owes its dr. |
| C16 | `OrdnanceFirePanel` | The weapon, Spotter, leader, Intensive Fire, and PF usage; the target fields come in its `Target` slot. |
| C17 | `OrdnanceTargetFields` | One vehicle select instead of two. The ammunition is none (a light mortar), fixed (a LATW or Panzerfaust), or chosen (a Gun or MA). |
| N13 | `StarshellActionPanel` | Each method names its limit; the target field is disabled for the firer's own hex. |
| R04 | `DiceRollHistory` | The latest six rolls the view may see, and the scripted-dice warning. |
| R05 | `ActionRecordList` | One list for ordnance, Close Combat, night and weather, Snipers, and Rally, Rout, and other actions. Each adapter on the page keeps its disclosure filter. |
| R06 | `FireHistory` | Keys each card by event id. |
| R07 | `FireResolutionCard` | Says when the target's effects are withheld rather than showing an empty table. |
| R08 | `FireArithmeticBreakdown` | Writes the record's numbers; never recalculates. |
| R09 | `InfantryFireEffectsTable` | Receives only disclosed effects. |
| R10 | `VehicleFireEffectsTable` | |
| S08 | `RuleHelp` (Shared) | Section 4. |

Two small shared pieces support them:

- `PlayChoice`: an option's value and label, read for the view.
- `FireText`: the fire wording the page and the fire components share. It moved from the page, with `FireView`, and is internal to the Studio. The page reaches it through `@using static`, so its other records keep their calls unchanged.

The empty toolbar beneath the ordnance block (section 15.1) is gone.

Each panel's Propose button is ready only when its choice is one the panel offers, so a stale id never stays proposable behind a select that shows "choose".

## 3. Deploy with several SW

The `deploy` action already took a list (`secondWeapons`), but the page held only one SW for the second HS. It now holds a set:

- **The control.** A checkbox for each SW the chosen squad possesses, under "SW for the second HS (the rest stay with the first)".
- **What is sent.** Only the checked SW that the squad still possesses, so a stale check is never sent.
- **The record.** It says who took what, for example "r2 becomes the HS r2-1 and r2-2 (A1.31); r2-1 takes r-lmg; r2-2 takes r-mmg".
- **Resets.** Choosing another squad clears the leader and the checks (section 15.3).

## 4. RuleHelp

`RuleHelp` takes an id, a summary, and the full text, and renders them in two parts:

- **The summary.** A paragraph with the id plus `-summary`.
- **The full text.** A paragraph with the id itself, inside a `<details>` that is closed by default, labelled "The rule in full". The disclosure's label is described by the summary, so a screen reader names which rule it opens.

A test or an `aria-describedby` that found the paragraph by its id still finds it. The movement destination now points at `move-help-summary`, since the full text is hidden by default.

The summaries are new text. Each says what a player needs in order to act, and cites the rule; the referee checked each against its paragraph and the rulebook. The non-OB "?" help, repeated per side, takes the id `non-ob-help-{side}`.

## 5. Records

Three records join the list now headed "Rally, Rout, and other actions" (backlog section 23). Each is read from events the game already writes:

- **DM (A10.62).** A change of conditions that sets DM. The end of the RPh clears DM and sets it again for a unit its owner keeps it on (A10.62 EXC; E1.54 at night), so a unit that had DM when the attempt began reads "keeps DM as the RPh ends" rather than "comes under DM".
- **Failure to Rout (A10.5).** An elimination with no rule package, in the attempt that ends the RtPh. A unit with a captor surrenders instead (A20.21), and the surrender records already show that.
- **A SW transfer (A4.431).** A SW passing to another unit as the only event of its attempt, apart from the DM the gate adds when a broken unit is left ADJACENT to its enemy.

Each record keeps the list's existing filter, the event's visibility, and adds one more: a record names a unit to a side only when it is that side's, or was neither concealed nor hidden just before the event (section 15.4).

The list now shows the latest attempt first, with each attempt's records in the order they happened, so a cause reads before its effect (the Deployment's NTC before the HS it made).

## 6. Resets

Before this pass, the page cleared few of these drafts when the game, phase, or view changed. `ResetPanelChoices` now holds every reset for this pass's panels and runs on each of those changes. It clears:

- Rally and Repair.
- Rout, SW, Deploy, Recombine, DM, and Shock.
- Starshell.
- The ordnance weapon, target, vehicle, Spotter, leader, Intensive Fire, ammunition, and Gun facing.

The resets within a panel stay in the page's handlers:

- Another SW clears the unit.
- Another Rally unit clears the leader.
- Another squad clears the leader and SW.
- Another first HS clears the partner and leader.
- Another ordnance weapon resets the ammunition, Spotter, leader, vehicle, Intensive Fire, and facing.
- Another target clears the vehicle.

A value the ordnance panel hides for the chosen weapon (a vehicle for a light mortar, Intensive Fire for a LATW) is never sent.

## 7. Disclosure

Every list reads the view (`VisibleUnits`, `VisibleEquipment`) and passes through `SeenOnly`, which also keeps Passengers out (ruling R26.2). Two lists were tightened by this pass:

- The enemy vehicles offered as an ordnance target. They came from the full state and could name a concealed or hidden vehicle.
- The firing side's own weapons and Starshell firers. They were safe only because the panel shows to the firing side, and no longer depend on that.

## 8. Tests

`PlayPagePass28bTests` has four page tests:

- Deploy with two of three SW checked, and its record.
- The transfer, DM gained, and DM kept records, in both views.
- A Failure to Rout against a lone tank.
- Resets on a view change.

`RallyAndRecordComponentTests` has six component tests:

- RuleHelp.
- The Deploy checkboxes.
- A loose SW.
- The empty DM and Shock panels.
- The three ammunition cases.
- The records list.

MapStudio has 252 tests, all passing.
