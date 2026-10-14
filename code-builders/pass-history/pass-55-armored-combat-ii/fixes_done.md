# What the five reviews found and what was done (the session's judgment and the fixing agent's report, 2026-10-13)

Five read-only agents in one message: the referee in two halves, the table player, the Rules boundary, the UI and Blazor review; briefs in src/ASL/docs/Prompts/Pass 55 Review Briefs.md. One BLOCKER (referee, second half, 1). The fix lists the session wrote are fixes_referee1.md, fixes_referee2.md, fixes_boundary.md, fixes_table.md, fixes_ui.md in this folder: each item has the file, the rule, and the fix decided.

## Confirmed sound by the reviews
- Referee 1: every catalog value of the two Guns and two crews agrees with renders of pp. 392, 426, 695 and Notes 6 and C (the second reading increment 2a lacked); the To Hit colors; the APDS Case D row of p. 190; C8.9's "not to have fired" on each Target Type and for Canister (the Gun is not turned, no counter, the ROF and Acquisition as they were); the Canister aim table for the figure's Z6 and for [F10] facing west; the halvings multiply; no older record changes.
- Referee 2: the DRM classification agrees with the chart of p. 189; the duel's paths neither lose nor double the declared shot; the Armor Leader is given on no BMG or CMG attack and none against the vehicle and is lost on Recall, wreck, Abandonment (and exit); new facts are absent-safe.
- Table player: the arithmetic of the recorded games checks against the charts (APDS 18 + 0 - 6 = 12; AP 15 + 1 - 6 = 10; each Depletion outcome; Canister 12, 6, 3 FP; Case A +3 on the IFT DR; Height Advantage +1; the Armor Leader's -1 and the NTC 10 against 10; 1 MP in LOS; the Gun Duel +7 against +8).
- UI: the Armor Leader in the units table comes from the view's units; the Motion block lists only the viewing side's vehicles; the gun-duel options go only to a view that may answer.

## Fixed (one line each)
- Referee 2, 1 (BLOCKER): a hidden vehicle's Motion attempt was written where the enemy reads it. A Hidden vehicle may not attempt Motion (A12.32; ruling R55.32); the same bar at replay.
- Referee 2, 2: a concealed vehicle kept its "?" after a passed Motion attempt. It loses it as a vehicle's own step does (A12.141), with or without a change of CA.
- Referee 2, 3: a Gun Duel declaration showed the DEFENDER vehicle's unrevealed Armor Leader. He is left out of the declared shot's total as of the potential shot's (ruling R55.29 extended).
- Referee 2, 4: a CE crew fired on from below by a vehicle's MG got Height Advantage on top of its CE DRM (D5.31). Dropped for a vehicle's fire and for Canister; the same line for Infantry fire is older than the pass and is left with a backlog row.
- Referee 2, 5 (the leak section 11.1 named): pending-choice resumes held a crew's Morale Level before the leader is revealed. The resumes are written without it and read it again from the state.
- Referee 2, 6 and Rules boundary 1: the projector did not hold a recorded Motion attempt to the bars. One Rules member serves the planner and the record check.
- Rules boundary 3: whether a duel's return shot is void was decided in Play. A Rules member decides it from the firer's facts; the projector refuses a void recorded for a firer still able to fire; a refusal for another cause is recorded as `other` with its reason.
- Rules boundary 4: a passed Motion attempt that named the TCA it already had lost the Acquisition. Only a TCA that changes loses it.
- Rules boundary 7: the three filters that passed over a Canister fire record for A7.55 are one clause inside the Rules members now.
- Rules boundary 11: an undecided LOS at the mover's starting Location was read as no LOS. The attempt is refused when it is undecided.
- Referee 1, 1: Sniper checks on a Canister shot depended on the path. A Canister shot follows ordnance: none (ruling R55.30).
- Referee 1, 3: the adjacent hex was attacked when no cluster Location was. A cluster Location must be attacked first (C8.41), and an aim with no cluster hex on the map is not offered; decided in Rules.
- Referee 1, 5: AP or APDS named at Infantry by the 6-Pounder was told to name HE. It is told that AP against Infantry (C8.31) is not built.
- Referee 1, 2: a game with a year and no month: a number that starts in a month of that year is not available (ruling R55.31); the note and the refusal now say why.
- Table player 1 and UI 9: a shot that had no round was recorded as a "miss" and "may fire again (C2.24)". It reads "no shot: it had no APDS" and "it has not fired and may fire other ammunition (C8.9)"; the Depletion Number is carried in the resolution, and an APCR or HEAT record made before the pass keeps its words exactly.
- Table player 2: "Case Q, height-advantage" reads "Case Q, Height Advantage".
- Table player 4: the Motion attempt list named the vehicle twice and put a whole refusal in an option. Short reasons; the sentence in the option's title.
- Table player 5, UI 8: the Motion record names the mover and whose MP they were; a hidden vehicle's hex is not given.
- Table player 6: an empty Location's Canister record read as a concealed target. "no Known unit".
- Table player 7: the Bog Removal proposal and the morale words had nested parentheses and read wrongly with an Armor Leader.
- Table player 8: the ammunition note shouted "CANISTER" and cited C8.9 for a date. The kind's own name; C8.11, C8.3, or C8.8.
- Table player 10: one refusal covered three faults. The elevation case has its own, with the numbers ("3 levels apart at range 2").
- Table player 11: capitals, "began their move", the Gun Duel choice's sentence.
- Table player 3: three games' labels promised what their files do not hold. Relabelled (by the session); the spoiled copies marked SUPERSEDED.
- UI 1, 2, 3: the placement form's Armor Leader and Inexperienced Crew, the Motion attempt's three choices, and the Motion options' cache outlived a hand-over or a change of game. Cleared.
- UI 4: the ammunition list followed "the Location holds a vehicle" while the shot followed "a vehicle is chosen". Both follow the chosen vehicle.
- UI 5: a Canister aim was sent without checking it is among the aims shown.
- UI 6: Canister lookups ran uncached on every render.
- UI 7: an aim's "N units" counted an AFV and left out the "?" stacks.
- UI 10: the form let an Armor Leader and an Inexperienced Crew be set together.
- UI 11, table player 9: the replay title of a fought Gun Duel named only the first shot.
- Found by the fixing agent and sent back: the answer to a duel's choice may show the DEFENDER's unrevealed Armor Leader in the declared shot's summary (being checked and fixed as this is written).

## Not fixed: a backlog row each (section 59)
- Rules boundary 2, referee 1, 4: a Canister fire record is not held to its shot at replay (its halvings, Case A, and roll are taken from the record).
- Rules boundary 5: the count and order of a Motion attempt's checks are decided in Play, and replay does not ask that a passed attempt by a red-MP AFV carries its DR.
- Rules boundary 6: which Locations a Canister shot attacks is partly decided in Play (the LOS and NVR skip, the Melee refusal, two facts set to pass Rules' own tests).
- Rules boundary 8: two Canister decisions in LiveOrdnance (a second MPh refusal; the Acquisition zeroed before Rules sees it).
- Rules boundary 9: when an Armor Leader is revealed is decided in the projector, by the fact's presence rather than the modifier's use.
- Rules boundary 10: smaller copies outside Rules (the PFPh bar on an Opportunity Firer in the Canister planner, the turret types twice more, what Reaction Fire is, the four grades in the placement editor).
- Referee 1, 7: C8.42's second sentence, a Canister KIA and a SW (no SW is destroyed by any KIA in the code).
- Referee 2, 4's other half: Height Advantage on top of a CE crew's DRM under Infantry fire (older than the pass; D5.31).
- The fixing agent's: a Motion attempt does not count an expenditure made in a Location whose LOS is undecided; a Passenger's time aboard is not read when it later moves on foot; a `gun-duel-voided` of cause `other` is admitted at replay without the planner's precheck.
- Not checked by any review (referee 2's list): whether a cliff hexside is stored on both hexes that share it (a vehicle's MG firing down through one, B11.31); whether B10.31's Crest Line exception reaches a moving vehicle on the Vehicle Target Type.
- No page test for UI 1, 2, 3, 5, 6.

## Referee 1, 6: answered, not a row
A Canister shot that malfunctions on a DR above the Depletion Number removes the Gun's Acquisition: right as built, since C6.5 takes the Acquisition when "the Gun malfunctions".

## Records of the pass made stale by a fix
Three kinds, none of them in a game the session recorded: a Motion attempt by a hidden vehicle; a vehicle MG's or Canister's fire record against a CE AFV with Height Advantage or behind a wall; a later MA shot after a Motion attempt that named its present TCA. All 48 `p55-*` games of the live folder replay to their end with no diagnostic on the build with the fixes (own_replay.sh).

## Tests after the fixes (the fixing agent's runs)
Rules 220 in nine classes; Play 184 in eleven; Units 416 whole; MapStudio 75; the four test projects build with warnings as errors, 0 warnings. The MapStudio build: exit 0.

## The four checks referee 2 could not make, answered by the fixing agent
1. A normal Bounding First Fire shot at the same MP expenditure after a fought duel is refused (the duel's shot is that step's shot).
2. No duel is offered between an OVR's resolution and the next expenditure (the OVR records the MA as fired at that step).
3. A Passenger is never among the movers; the vehicle alone is.
4. `asl.game.motion-attempt` is admitted only from the side whose vehicle it is and only for a vehicle that is not phasing; the adjudicator's view is unchecked, as elsewhere.
