# ASL Unit Deviations Pass 1 Design

**Status:** Built. Unit steps 26 (stack splitting), 27 (Leader Creation), and 28 (Heat of Battle with heroes and Battle Hardening) in one pass, as the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<../Plans/ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), section 3, set out. Berserk and Surrender stay recorded as not taken until step 30.

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 26 to 28, and acceptance scenarios U30 to U32 (section 14).

**Related documents:** the [Scenario A1 Heat of Battle Review](<../Reviews/Scenario A1 Heat of Battle Review 2026-09-27.md>) (the review stage, with both second-pass reports), the [ASL Unit Rally and Fire Extensions in Live Play Design](<../Designs/ASL Unit Rally and Fire Extensions in Live Play Design.md>), whose principles this pass keeps, the [Scenario A1 Catalog Design](<../Designs/ASL Scenario A1 Catalog Design.md>), section 9.4 (catalog 1.3.0), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 1 and 10.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Stack splitting (step 26).** A moving stack may split: `asl.game.move` names any members still moving that share a Location, and `asl.game.end-move` names the members it ends (A4.2, p. 49). No other stack moves until every member has ended.
- **Leader Creation (step 27).** An Original 2 on the side's first MMC Self-Rally of its own RPh rallies the unit and calls for a Leader Creation dr (A18.11, A18.2, p. 85). A Final dr of 6 or less creates a 6+1 to an 8-1 of the unit's nationality in its Location.
- **Heat of Battle (step 28).** An Original 2 on a MC, or on a Rally other than Self-Rally, calls for a Heat of Battle DR (A15.1, p. 83). A Final DR of 6 or less creates a hero or makes a leader heroic (A15.2, A15.21). A Final DR of 5 to 8 Battle Hardens the unit, or makes it Fanatic where it has no better class (A15.3, A10.8, A25.25). A Final DR of 9 or more is recorded as Berserk or Surrender not taken (ruling R28.1).

## 2. Stack splitting

- **Model.** `MovementState` gains `Members`: the units that began the move together. `Movers` stays the units of the current step.
- **Moving.** A move's units must be a subset of `Members`. The projector's `StepMovement` checks this on replay.
- **Leaving the stack.** After each settled step, `KeepMovingStack` drops members that are broken, pinned, or no longer active. A member Reduced to a HS is followed by the HS through lineage.
- **Ending.** `asl.game.end-move` takes optional `unitIds`, by default every member still moving (or the last movers, when none is left). The stack's move is over, and `Movement` is cleared, only when `Members` is empty. The gate's `MovementEnded` holds when every named unit is inactive or has ended.
- **Play page.** The move panel lists the members still moving; the player chooses who moves on and who ends ("End the move" for all, or for the selected units only).

## 3. Catalog 1.3.0 and vocabulary asl@1.7.0

- **Catalog.** 25 definitions: the classes Battle Hardening exchanges units for, every leader grade of both sides, and a hero of each side. The [Scenario A1 Catalog Design](<../Designs/ASL Scenario A1 Catalog Design.md>), section 9.4, gives the sources and the review.
- **Fire reference.** `FireDefinition` gains `IsHero` and the wounded side (FP, range, morale). The reference knows the leader chains (6+1 to 10-3), the hardened class of each MMC and leader, the classes with no better one, and the NKVD classes.
- **Vocabulary.** The state `asl:heroic` ("HRO"), drawn as a "Hero" badge on both sheets. `Conditions` gains `Fanatic` (`asl:fanatic`) and `Heroic` (`asl:heroic`).

## 4. The packages

Both `scenario-a1-fire` and `scenario-a1-rally` are revised and republished; their earlier manifest digests are kept as the prior digests.

- **Heat of Battle.** `ScenarioA1HeatOfBattle.Resolve` is shared by both packages. The DRM are -1 elite, -1 NKVD, +1 broken (before the MC or by it), +1 Inexperienced, and the nationality DRM (German 0, Russian +2). A unit takes one Heat of Battle DR per attack (ruling R28.8), and a Fanatic unit's 12 is Berserk. A Conscript is always Inexperienced (A19.3); a Green unit needs the Inexperienced fact, and without it the attempt is refused before any roll (`A1-fire-heat-of-battle-undecided`).
- **Heroes.** A hero may fire (1-4-9, wounded 1-3-8) with the -1 heroic DRM at Normal Range, and may not use a SW until A15.23 is reviewed. A group all of heroes or Fanatic units does not Cower. A failed MC or Casualty Reduction wounds a hero, and a second one eliminates him (A15.2); a Casualty MC adds +1 to the Wound Severity dr (A10.31). He takes no PTC or LLTC, and a KIA that would break him Casualty Reduces him instead (A7.301). A heroic leader keeps his counter and leadership, rallies even on a failed Rally DR, and has a Morale Level of at least 9 (A15.21); a hero's never exceeds 10, or 9 if wounded.
- **Battle Hardening.** The unit is Replaced by an unbroken, unpinned unit of the next higher quality: the same size, no printed number lower, and the least gain (ruling R28.6). An elite MMC, an NKVD MMC, or a 10-3 becomes Fanatic instead, and is unbroken and unpinned too (A15.3, ruling R28.7). A Russian Conscript becomes NKVD 2nd Line (A25.25). A Disrupted unit is no longer Disrupted, and neither is one a leader rallies (A19.12).
- **Fanaticism.** +1 to both Morale Levels, no Cowering, and never Disrupted (A10.8).
- **Leader Creation.** The dr takes -1 German, +1 Russian, -1 for a Morale Level of 8 or more, +1 for 6 or less, and +1 broken; the base Morale Level is the broken one. The bands are 7 or more none, 6 a 6+1, 4 or 5 a 7-0, 2 or 3 an 8-0, 1 or less an 8-1 (A18.2). An NKVD unit's Field Promotion creates a Commissar (A25.25), which is refused before any roll (`A1-rally-field-promotion-unreviewed`).
- **The source review.** Twelve new fragments are compared with their pages (`asl-scenario-a1.heat-of-battle-pdf-comparison.json`). The Fire matrix has 122 fragments and the Rally matrix 27.

## 5. The live records

- **Rolls on demand.** The Fire planner asks for `heatOfBattle` (purpose `fire-heat-of-battle`) per unit. The Rally planner asks for `heatOfBattle` (`rally-heat-of-battle`) and `leaderCreation` (`rally-leader-creation`). Each is drawn only when the package reports it missing.
- **A created hero.** An `instance-created` event caused by the fire or rally record: the hero of the unit's nationality in its Location, unbroken and known, with the unit's fire markers and Fanaticism. Its id is `{attemptId}-{unitId}-hero`.
- **A created leader.** An `instance-created` event caused by the rally record: Good Order and known in the rallied unit's Location, Fanatic if the unit is. Its id is `{attemptId}-{unitId}-leader`.
- **Battle Hardening.** A `lineage` event with `Replaced`: the new unit is unbroken, unpinned, and not Disrupted, with no DM; it keeps its concealment unless the attempt lost it, carries Fanaticism and heroic status, and keeps the unit's SW. Any lineage carries the MF spent and the move's end to the produced unit.
- **Created units in the MPh.** A unit created in its own side's MPh moves no further that phase.
- **Conditions.** The effect sets `asl:fanatic` and `asl:heroic` as the package gives them. A heroic result also clears DM.
- **Replay.** `FireRecordVerifier` and `RallyRecordVerifier` rebuild each record's facts, including the new ones (Fanatic, Wounded, Inexperienced), and require the package to reproduce the resolution. Created units follow from the recorded events without dice.

## 6. The Play page

- The rally and fire records show the Heat of Battle DR with its DRM parts, the Final DR, and the result, including "Berserk not taken" and "Surrender not taken". A Leader Creation line shows the dr, its DRM, and the leader created.
- Heroes appear in the fire panel's firers and targets like any other unit.
- **Scripted dice for UI test runs.** Heat of Battle and Leader Creation need an Original 2, so a UI run rarely meets them. With the Studio in the Development environment and `Play:ScriptedDice` set to true (for example `dotnet run --project src/ASL/LimboDancer.Domains.Asl.MapStudio -- --Play:ScriptedDice=true`), the Play page shows a banner and a queue: each die comes from the queue until it is empty, then from the system (`ScriptedDice`). The rolls are still recorded as system rolls, which replay requires, so a game played with queued dice is test data. Without the flag, or outside Development, the Studio has no queue.

## 7. Compatibility

Catalog 1.3.0 changes the catalog version the game files name. Games that name 1.2.0 (for example the live games in `boards/`) no longer replay; the committed sample game names 1.3.0.

## 8. Tests

- **ScenarioA1:** the Heat of Battle outcomes, the Conscript's inexperience, heroes firing and wounded, Fanaticism, the NKVD DRM, Leader Creation, and the reachability walks with the new rolls and two new scenarios ("heroes-and-fanatic" and "conscript-and-elite").
- **Authoring:** the matrices with the new fragments, and the twelve Heat of Battle subjects verified.
- **Play:** U30 (a stack splits when a member breaks; a split stack ends one member while the other moves on), U31 (a first MMC Self-Rally creates a leader), and U32 (Heat of Battle creating a hero, Battle Hardening, and both).
- **Catalog and vocabulary:** the 1.3.0 definition lists, the `asl:heroic` style rows, and the new goldens.

## 9. Not in this pass

In the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 10: a created hero or leader moving in the MPh he was created in, SW use by a hero, refusing Battle Hardening, declining the Leader Creation dr, NKVD Commissars, and attacks on units with an underscored morale (A19.13). Berserk and Surrender are step 30.
