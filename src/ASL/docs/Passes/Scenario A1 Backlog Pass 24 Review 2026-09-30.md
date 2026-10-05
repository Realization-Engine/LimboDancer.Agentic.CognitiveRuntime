# Scenario A1 Backlog Pass 24: Control of Locations and More VP

**Status:** Reviewed. Pass 24 of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the second game pass: Control of Locations, Mopping Up with its Search casualties, Gun and vehicle VP, the start Control edge cases, vehicles' temporary Control, the Control fold cached by revision, and K08 extended. No package changes.

**Date:** 2026-09-30

**Plan:** the Redesign Plan, pass 24 (tasks 24.1 to 24.5) and sections 15 and 17.2; rulings R24.1 to R24.7 in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. A referee, a table player, and a UI and Blazor reviewer read the change, in parallel; all were read-only. The UI reviewer ran because Mopping Up added a Play panel beyond K08.

**Design:** [ASL Unit Backlog Pass 24 Design](<ASL Unit Backlog Pass 24 Design.md>).

## The user's rulings

Before the build the user answered three questions, each with the rulebook: an upper level or cellar no one has entered keeps its own Control (A26.1, the A26.16 EX), not its hex's; a building's Control does not need every Location (A26.14); and Mopping Up comes with the A12.154 Search casualties.

## Guns and vehicles tested with constructed games

No card fields a Gun or a vehicle yet (plan pass 26). Task 24.2 is therefore tested on constructed states: the catalog's PzKpfw IIIH, T-34 M41, SPW 251/1, and truck valued against A26.212 and its EX; a German Gun eliminated, captured, and left unheld; a wrecked tank and an Abandoned captured one; a tank's temporary hold of a Location and an unarmed truck's lack of one. The table player also checked the KV-1E capture example.

## Visual checks

Before the reviews: a Guards Counterattack game (`p24-visual`) set up in the Studio and played to the Russian PFPh. The Mopping Up panel in the Fire panel; a Russian squad Mopping Up F3 (committed, TI, the building and its Locations Russian); a Russian squad walked G4, H4, H5 into the German building F5, and the standing then showed "bd01:H5 ground level of building F5: russian" with its note; in the German PFPh the German view's panel, and F5 refused for the unbroken Russian inside. Found and fixed: every unit checked by default (each becomes TI), unit labels without places, one-hex one-level buildings (I7, M9) offered though always refused, and "Mop Up ... become" for one unit. After the fixes and the review fixes: the German view's panel offering F5, K5, and M7 only, units named with their board and hex, none checked, the "Choose a building and at least one unit" hint, a German Mop Up of K5 ("german-1-6 Mops Up building K5 and becomes TI"), K5 then gone from the list and the selection reset. Clean.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. An enemy vehicle in Bypass blocked Mopping Up, so A26.11's exception never ran | Fixed: a vehicle in Bypass is not inside the building; its ground level stays out of the secured set. A planner test waits for a card with vehicles (section 38). |
| 2. An unarmed truck counted as armed | Fixed: a vehicle is armed only with an inherent crew (the Index's Armed, D5.1), read through `VictoryReading.ArmedVehicle`; R24.5 says so; a test. |
| 3. A captured vehicle neither holds nor prevents Control, with no reason given | Recorded as a deviation in R24.5; backlog (section 38). |
| 4. The guard did not follow A20.5 | Fixed: any armed Personnel unit, even a broken SMC, never berserk, Unarmed, or a vehicle; one guard for all is ruled a simplification; a test. |
| 5. Cellar units were invisible to Mopping Up while their Locations were secured | Fixed with the table player's finding 3: cellars are left out of Location Control (B23.41). |
| 6. A disabled MA kept its VP; an armored unarmed vehicle got crew VP; the crew counter is found by its id | Fixed: a MG MA loses its point when disabled (the Disabled condition is a vehicle MG's); crew VP only with a MA or a MG. The id link is backlog (section 38). |
| 7. The leader's "unless alone" read across the building | Kept as the referee proposed it; R24.2 says so. |
| 8. Parts of A12.153 and A12.154 not built and not in the backlog | Backlog (section 38): Rowhouses, rubble and Blazes, Fortified Locations, minefield, Residual FP, FFE, and Booby Trap casualties, nationality Stealth. |
| 9. A future SSR filling `NoQuarter` would bar Mopping Up | Backlog (section 38). |
| 10. The range refusal can tell of Control a concealed enemy holds | Accepted (A26.15 lets Mopping Up verify Control); R24.2 says so. |
| 11. Checked and correct | The automatic Casualty dr, Random Selection ties, the HS-equivalent and wounded-leader drm, the Final dr threshold, once per Player Turn, the fold order, and Gun CVP. |
| 12. Untested paths | Tests added for the refusal reasons, the guard, and the truck; the table player ran several moppers with a tie, the leadership drm, and a surrender with its SW; the Bypass and range paths are backlog. |

## Table player findings

The table player ran five situations through the planner (stairs, Mopping Up an enemy and an own building, a concealed squad and leader with two Mopping-Up squads, a hidden lone leader, a German named as guard) and checked vehicle VP against the A26.212 examples. All passed but the findings below.

| Finding | Disposition |
|---|---|
| 1. A vehicle in Bypass blocked Mopping Up | Fixed (the referee's finding 1). |
| 2. One squad on the stairs filled the standing with a dozen Location rows | Fixed: one row per building and side, its Locations in a disclosure; a component test. |
| 3. Cellar rows, though cellars have no use in play (B23.41) | Fixed: cellars are left out, as rooftops are. |
| 4. The same refusal for a pinned and a Prep-Fired unit | Fixed: the refusal says which ("is pinned", "fired in this PFPh"); a test. |
| 5. The Casualty dr's outcome is not in the result text | Backlog (section 38); the dr and the Reduction show in the rolls and the unit table. |
| 6. Hidden units found are not mentioned | Fixed: the result says how many were placed beneath "?" and Dummies removed. |
| 7. The range check cannot fire on this card | Backlog (section 38). |
| 8. Missing backlog rows | Section 38 added; the planned row in section 37's list marked built. |
| 9. The crew counter found by its id | Backlog (section 38). |
| 10. "level 1" against "1st level" | Kept: clear as it is. |

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1, 2. Stale checked units and a stale guard or building after the choices change | Fixed: `OnParametersSet` drops what the new choices no longer hold; the button reads the checked units still offered; a component test. |
| 3. Drafts and hand-over | Complies. |
| 4. Places without the board | Fixed: "bd01:E4", as the standing names Locations. |
| 5. Hard-coded kind and TI strings | Kept: the codebase has no constants for them (TI is a raw string elsewhere too). |
| 6. The help not tied to the controls; no reason for the disabled button | Fixed: `aria-describedby` on the building picker and the button, and a "Choose a building and at least one unit" note. |
| 7. The fieldset floating in the toolbar | Fixed: it takes a row of its own. |
| 8. Labels and ids | Comply. |
| 9. Choices rebuilt each render | Kept: small; noted. |
| 10. Location rows ignore `InMelee` | Kept: only hex items carry it. |
| 11. A vehicle hold on the building's own side loses its mark | Kept: such a hold changes nothing in the standing. |
| 12. Tests | Added: the stale draft, no guard, the grouped rows. A page test of the panel waits for a page fixture with a card building on board 01. |

## Tests

`BacklogPass24Tests` (Play, 12): Locations keeping their own Control (the A26.16 EX), Mopping Up securing F3 with a surrender, the Casualty dr at 2 and 3, a hidden unit placed beneath "?", the refusals (building, units, enemy, phase), the refusal reasons and the guard, vehicle VP, Gun and vehicle CVP, a tank's temporary hold, an unarmed truck holding nothing, and the cached fold. `PlayComponentTests` (MapStudio, 3 new): the grouped and single Location rows and the Gun and vehicle CVP, the Mopping Up panel's proposal, and its stale draft.
