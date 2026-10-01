# Scenario A1 Backlog Pass 26: Vehicles and Guns on a Card

**Date:** 2026-10-01

**Pass:** 26 of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), the fourth game pass. Design: [ASL Unit Backlog Pass 26 Design](<ASL Unit Backlog Pass 26 Design.md>). Rulings R26.1 to R26.8 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5; what is left out is in section 40 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

Three reviews ran in parallel on the built pass: a skeptical ASL referee, an experienced table player, and a UI and Blazor reviewer, since task 26.5 extracts Play components. Their findings and what was done follow.

## The user's rulings

Asked before building (plan decision 5): Passengers load and unload (D6.4, D6.5); limbering (C10.2) and en portee (C10.5) wait, since both catalog Guns are QSU; A12.34's HIP is built for Concealment Terrain only; the MPh holds vehicles until they enter, since they cannot advance. The pass ran as one, not split.

## Known bugs fixed

- A vehicle's exit recorded no `UnitExit`, so it scored no Exit VP and gave the enemy CVP: its exit is now recorded with its Passengers' and its towed Gun's (R26.4).
- A manned Gun's hidden flag escaped the R23.5 setup refusal: a Gun is hidden only under A12.34 now (R26.5).
- A crew manning a Gun counted half a squad in setup stacking: it counts as a squad (A5.5; R26.3).

## Visual checks

The first check (Armor Test, `p26-visual`) played the setup with a hidden Emplaced Gun, Passengers, and a towed Gun; the MPh hold; the halftrack's entry, stop, and unload; the truck's entry and unhooking for its crew; the tank's entry, the hex it wished to enter next, its exit off a hex no condition names, and the standing (6 CVP to the Russians); and boarding on Turn 2. Fixes: the off-board entry as a hex select, a VCA picker, and one button (it had been 66 buttons); "manned by" in the placement list; the `data-bypass` values; the vehicle exit's note on what it counts for; Passengers left out of every action list. The hidden Gun had no LOS to any German on the real board 4, so its reveal stays with the planner tests. The second check (`p26-check`) after the reviews: the placement note for a holder not yet placed, the entry button's reason, the moving and stopped panels, and one crew-boards box; clean. Screenshots timed out (the app window was behind others), so both checks read the page's DOM.

## Referee findings

| # | Finding | Done |
|---|---|---|
| 1 | Unloading refused from a vehicle that Prep Fired, is immobilized, or is Abandoned (D6.5, D6.1) | Allowed; units from a vehicle that Prep Fired stay in its Location. An immobilization ends the vehicle's move at once here, so its Passengers get off in its next MPh (section 40). |
| 2 | Broken Passengers fell under Failure to Rout and held up the DEFENDER's rout (D6.1) | Passengers are left out of `MustRout` and `FailureToRout`; a test. |
| 3 | Passengers gained and prevented Control | Left out of `ScenarioVictory.Inside`. |
| 4 | A vehicle leaving under Recall would score (A26.23, A26.221) | `UnitExit.Recalled`; no Exit VP and no CVP. |
| 5 | Unloaded units regained the MF spent boarding | Boarding spends its MF; an unload adds to what the unit spent. A leader's four-MF limit is in section 40. |
| 6 | A crew could not push its Gun on and then off the map in one MPh (C10.3) | The exit takes the pushing-on exemption the move has. |
| 7 | Boarding after moving (the D6.4 EX), and a second stack boarding | Section 40. |
| 8 | A12.34's crew SW exception and the C8.9 "not fired" exception | Section 40. |
| 9 | Unloaded units moving only in the interim; unloading into an enemy-occupied hex | Section 40. |
| 10 | Five SMC equal a HS as Passengers | Section 40; more than four are refused. |
| 11 | A doc comment out of place | Fixed. |
| 12 | A captured Gun towed off scored for its owner | The Gun's exit belongs to the side last holding it, and counts as captured for that side. |
| 13 | The MPh hold and the entry could disagree for a vehicle with no MP | The hold reads the vehicle's MP too. |

The referee confirmed the entry VCA, the load and unload arithmetic, the PP and ammunition, the A12.34 thresholds, and the Gun VP against the PDF.

## Table player findings

| # | Finding | Done |
|---|---|---|
| 1 | The page could not push a Gun off the map | The exit sends the push as the move does; the refusal names the box to check. |
| 2 | Passengers offered as firers and directors | Left out of the fire group and leader lists. |
| 3 | Passengers gained Control | Fixed (referee 3). |
| 4 | Infantry cannot walk to a vehicle and board | Section 40 (referee 7); the ruling says so. |
| 5 | A Passenger's or towed Gun's holder depended on the order of placements, and was dropped silently | A holder not yet placed is refused with a note; a Gun off board needs its tower named. |
| 6 | No voluntary delay of a vehicle's entry | A2.5: "All forces scheduled to arrive on a certain turn must enter the mapboard on that turn"; vehicles cannot use the APh's delay. Kept. |
| 7 | An entry hex's reason hidden in a disabled option's title | The reason is in the option's text. |
| 8 | The push refusal lumped its conditions | Each condition its own message. |
| 9, 10, 11 | Unload offered beyond three-fourths of the MP; unused facing fields; exit buttons not saying what they score | Section 40; the proposal's review says each. |
| 12, 13 | Passengers take no fire, and die with their vehicle | Recorded (R26.2, section 40). |
| 14 | Passengers spotted for the A12.34 reveal | Left out of the nearest enemy read. |
| 15 | Only the crew pushes a Gun off the map | Section 40. |
| 16 | A crew boarding with a hook-up stays TI | C10.11 makes it TI; kept. |

## UI and Blazor findings

| # | Finding | Done |
|---|---|---|
| 1 | The moving options showed beside a pending OVR or a Bog Removal | `VehicleFreeToMove` gates the options, ESB, the exits, and the hex wished to enter next. |
| 2 | Towing offered in states the planner refuses | `VehicleStopped` mirrors the planner: the vehicle's own Stopped movement, or none, not in Motion or bogged; unload reads it too. |
| 3 | Cargo notes read the full state for the enemy's vehicles | Cargo is shown for the viewer's own side; a page test switches the view. |
| 4 | The entry area draft went stale | Accepted only when the group offers it; cleared with the card, the game, and the view. |
| 5 | The Passengers chosen were sent unfiltered and kept after a commit | Sent as the chosen ones still offered; cleared after the commit, with the entry and the crew boarding. |
| 6 | Choosing another vehicle kept the option boxes | All the vehicle's drafts reset; the Bounding Fire target resets with the phase. |
| 7 | Removing a placement removed every placement with its id | Each placement has its own key, used for removal and `@key`; a page test. |
| 8 | The crew-boards box repeated per Gun | One box beside the hook-ups; a comment says one Gun is towed. |
| 9 | The entry button could propose an empty VCA | Disabled until the hex is open and the VCA allowed, with the reason shown and described. |
| 10 | Planner reads per render | The off-board entries are read once per revision; the rest in section 40. |
| 11, 12 | Reasons only in titles; the hex buttons not grouped | Notes described by the load and unload buttons; the hex buttons grouped and labelled. |
| 13 | Empty titles; the redundant wrapper; moved order | Titles only when there is one; the wrapper removed. The recall note now follows the status, and the options follow Stop; no test reads the order. |
| 14 | The Gun note named the old field label | Fixed. |
| 15 | Removing a vehicle leaves its Passengers' placements | Section 40. |
| 16 | No page test of the new flows | `PlayPagePass26Tests`: the placement note, keyed removal, a Passenger kept out of the movers, the cargo per view, and the unload through the panel. |

## Components

All ten candidates of task 26.5 are extracted: P08 `SetupPlacementEditor`, P09 `SetupPlacementList`, A17 `VehicleMovementPanel`, A18 `VehicleMovementStatus`, A19 `VehicleStepChoices`, A20 `VehicleTowingActions`, A21 `BoundingFireAction`, A22 `CrewExposureActions`, C18 `GunArcAction`, and S13 `FacingPicker`. None is left inline.

## Tests

`BacklogPass26Tests` (10 cases), `VehicleComponentTests` (7), and `PlayPagePass26Tests` (1) are new. Three earlier tests changed with the pass: the embedded cards now include Armor Test (pass 17), the hidden refusal's text (pass 23), and a concealed Emplaced Gun firing now follows A12.34 (an ordnance test's colored dr). The full suite found two more: the cards page shows Armor Test first, so its Gambit test chooses Gambit; and a vehicle page test that had always stopped early, because Blazor left `data-bypass` out for `false`, now reaches its end and reads the CC Reaction Fire panel from the DEFENDER's view, as pass 25 made it.
