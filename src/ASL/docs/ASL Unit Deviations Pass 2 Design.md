# ASL Unit Deviations Pass 2 Design

**Status:** Built. Unit steps 29 (Close Combat and Advance) and 30 (Berserk, Surrender, and capture) in one pass, with two fixes to live play (Fix A and Fix B), as the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), sections 7 and 8, set out. The Heat of Battle deviation of pass 1 (Berserk and Surrender not taken) is removed.

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 29 and 30, and acceptance scenarios U33 and U34 (section 14).

**Related documents:** the [Scenario A1 Close Combat Review](<Scenario A1 Close Combat Review 2026-09-27.md>) (the review stage, with both second-pass reports), the [ASL Unit Deviations Pass 1 Design](<ASL Unit Deviations Pass 1 Design.md>), whose principles this pass keeps, and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 11.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Advance (step 29).** In the APh, `asl.game.advance` moves units of the phasing side from one Location to an ADJACENT one, once each, including into a Location holding Known enemy units (A4.7, p. 52). Broken, pinned, berserk, captured, and Melee units may not advance, and an advance that would overstack is refused.
- **Close Combat (step 29).** In the CCPh, each Location holding units of both sides is resolved once: `asl.game.ambush` rolls the Ambush drs where A11.4 allows them (p. 73), and `asl.game.close-combat` declares every attack of a round and resolves it on the CCT (A11.11, p. 72). Units left together at the end of the CCPh are held in Melee (A11.15); broken units in Melee withdraw or are eliminated (A11.16, A11.2, A11.21).
- **Berserk (step 30).** A Heat of Battle Final DR of 9 to 11, or a Fanatic unit's 12, makes the unit berserk (A15.4, p. 84), unless no Known enemy unit is in its LOS, when A15.44 makes it Battle Hardened instead. A berserk leader's companions take a Berserk TC (A15.41). A berserk unit charges at the start of its MPh and fights in CC.
- **Surrender and capture (step 30).** A Final DR of 12 or more (not Fanatic) breaks and Disrupts the unit (A15.5); if a Known, Good Order, armed enemy Infantry unit with Guard capacity is ADJACENT, the unit surrenders and waits until the captor's side takes it with `asl.game.take-prisoner` (A20.21, A20.5, p. 87).
- **Fix A.** A unit that Prep Fired may not move (A3.3, p. 47): refused by the planner, by replay (UNIT-STATE-029), and absent from the Play page's movers.
- **Fix B.** The Play page's fire selection resets when the game changes and is pruned to the From Location; the Director list offers only Good Order, unpinned leaders.

## 2. The Close Combat package

`scenario-a1-close-combat` is a new package with no execution authority, pinned by digest like the others. It has 36 fragments and 14 cases (the [review](<Scenario A1 Close Combat Review 2026-09-27.md>) lists them), and its chart is the CCT transcription (`a11-close-combat-table.transcription.json`).

- **Calculator.** `ScenarioA1CloseCombatCalculator` is pure, in the order of the other packages: missing facts, then Outside (Abstained), then Undecided (Indeterminate), then the round, asking for its rolls one at a time (`roll-missing:<key>`). `Precheck` proves the round is decided before any roll.
- **Odds.** Each attack's FP against the defenders' Defense Strength, rounded in the defender's favor to a CCT column (A11.11), with a SMC counting one FP (A11.14). Odds above 10 to 1 but below 11 to 1 are undecided (R29.16).
- **DRM.** The attack's common DRM (the director's leadership, a hero, Ambush) and each defender's (broken, withdrawing, Inexperienced, the defender's leadership) are kept apart, so an attack on several defenders is resolved against each (A11.11, A4.8 EX).
- **Results.** A Final DR at or below the Kill Number eliminates the defender; one above it by the Casualty Reduction margin reduces it (A11.11). A Partial Kill among several defenders is placed by Random Selection. A leader's wound calls for a Wound Severity dr.
- **Leaders.** SMC are declared stacked with one MMC or alone (R29.4). A declared director is a leader of the attack who is not pinned or berserk, and only when the attack has no berserk unit (A11.141, A7.831; R29.8).
- **Field Promotion.** An attacking MMC's Original 2 calls for a Leader Creation dr with -1 per odds column below 1-1 (A18.12, A18.2). The shared table is `ScenarioA1FieldPromotion`, used by the Rally package too. The created leader's DRM replaces the director's (A10.7; R29.12).
- **SW.** An Original 12 by an attacking unit with a SW calls for a SW loss dr (A11.13's colored die).
- **Ambush.** `AmbushPossible` and `ResolveAmbush` give each side's dr with its drm (concealed, Lax, pinned, leadership, berserk) and the ambusher when one dr is at least 3 lower than the other (A11.4). The ambusher's attacks are sequential, one or more records (A11.3; R29.18); the ambushed side's round closes the Location.
- **Berserk units.** A berserk unit returns to normal when its own group's attack made every elimination in its Location, at least one, and no enemy unit is left (A15.46; R29.15).

## 3. The Fire and Rally packages

Both are revised and republished; their earlier manifest digests (`ab868f4c...` and `5a1fc655...`) are kept as the prior digests.

- **Heat of Battle.** `ScenarioA1HeatOfBattle.Resolve` now returns Berserk and Surrender. It needs two map facts the verifier takes as recorded: whether a Known enemy unit is in the unit's LOS (A15.44), and the captors (A15.5, A20.21). Without them the attempt is refused before any roll. A concealed firer that firing reveals counts as Known, and a captor the attack breaks, pins, eliminates, or makes berserk is none (referee D7).
- **Nationalities.** Italian, Axis Minor, and Japanese Heat of Battle subjects are refused until their notes (p. 83) are built.
- **A berserk unit under fire.** Morale Level 10, 11 when Fanatic, never lowered; a failed MC Casualty Reduces it; an Original 12 eliminates a berserk MMC and wounds a berserk leader as if already wounded (A10.31); no PTC, LLMC, LLTC, or ELR Replacement, and no leadership from a friendly leader (A15.42; R30.3).
- **Companions.** A leader going berserk makes each Good Order unit in his Location take a Berserk TC (A15.41); a failure makes that unit berserk too. The Fire and Rally records carry the companions and their effects.
- **Surrender.** A surrender to Guards with no capacity is refused before any roll (Unarmed units are not built; referee D8). A Heat of Battle subject sharing a Location with prisoners is refused, since a berserk unit would massacre them (A20.4; referee D10).

## 4. The live records

- **Events.** `advanced` (AdvanceMoved), `ambush-rolled` (AmbushRolled), `close-combat-resolved` (CloseCombatResolved, with the round: simultaneous, ambusher, or ambushed), and `surrender-pending` (SurrenderPending). The movement step carries `charge` for a berserk charge.
- **Replay codes.** UNIT-STATE-030 (CC and Melee), 031 (surrender), and 032 (advance). The projector refuses a phase change while a surrender is pending, a Location's CC is open, or a broken or Disrupted unit other than a Guard is left in Melee. The planner also refuses the end of the CCPh while a berserk or reinforcing unit's Location has had no CC (R29.17). At the end of the CCPh it sets `asl:melee` on units left together with enemy units, and clears it when no enemy unit is left.
- **Verification.** `ICloseCombatRecordVerifier` rebuilds each Ambush and CC record's facts from the state and requires the package to reproduce the resolution, like the Fire and Rally verifiers. The map reads (Known enemy in LOS, captors) are compared only when the record has them.
- **Rolls.** The CC planner asks for `cc-attack` per attack, `cc-random-selection`, `cc-wound-severity`, `cc-leader-creation`, and `cc-weapon-loss`, and the Ambush planner for `cc-ambush`, each only when the package reports it missing.
- **Effects.** Eliminations, Casualty Reductions, wounds, created leaders (placeholder ids `created-leader:` resolved to `{attemptId}-{unitId}-leader`), SW lost or abandoned, units returning from berserk, and withdrawals are events caused by the CC record. The gate's `EffectHolds` covers CC, Ambush, Advance, and capture.
- **Capture.** `asl.game.take-prisoner` moves the unit to its Guard's Location and captures it (`instance-captured`); its SW stay in its Location (A20.24). A prisoner moves and advances with its Guard (A20.53).
- **The charge.** At the start of its side's MPh a berserk unit must charge before any other unit moves: each step lies on a shortest route in MF to the nearest Known enemy unit in its LOS, it abandons each SW of more than one PP first, and it has 8 MF (3 wounded; `Experience`). `asl.game.end-move` is refused while it can still charge. A charge the model cannot decide (a route over unreviewed terrain, or a step into prisoners, concealed units, or a lone SMC) ends in place, a recorded deviation (R30.5).
- **Refusals.** Berserk, Melee, captured, and Guard units may not fire; fire at a Melee or prisoner Location is refused; FPF where prisoners share the firers' Location is refused.

## 5. The Play page

- **APh.** The destination is chosen from the ADJACENT Locations of the chosen units.
- **CCPh.** A list of the CC still due before the phase can end. A CC panel per Location: the SMC stacking (defaulting to the first MMC of the side), a withdrawal selector per unit in Melee, the attacks declared per side with the ATTACKER first (a broken unit cannot be chosen to attack), a Director defaulting to the best eligible leader, the ambusher's choice of another attack or the ambushed side's round, and the Ambush roll only where one is due.
- **Surrenders.** A panel listing each pending surrender and its possible Guards.
- **Records.** The CC record shows each attack's FP with its modifiers, odds, Kill Number, dice (the colored die marked), DRM, and each defender's own DRM leading to its Final DR and result, with withdrawals; the Heat of Battle lines show Berserk, Surrender, companions, and captors.

## 6. Compatibility

No catalog or vocabulary version changes. Games recorded before this pass replay unchanged, except that a Heat of Battle DR of 9 or more now resolves as Berserk or Surrender; a game that recorded one as "not taken" no longer replays. The committed sample games have none.

## 7. Tests

- **ScenarioA1:** the CC rounds, the odds rounding examples of A11.11 and p. 74, Ambush, Field Promotion, SW loss, berserk units in CC and under fire, surrender and captors, and the reachability walks of the three packages.
- **Authoring:** the three matrices and the two source comparisons (28 and 18 subjects).
- **Units:** the new records, their replay checks, and their round trip.
- **Play:** U33 (an advance, an Ambush, and a CC round leaving Melee), U34 (a berserk charge, a surrender and capture, and A15.44), withdrawal from Melee, SW abandonment, captor capacity, Fix A, and the table player's findings (a charge ending in place, a Guard's advance, a withdrawal next to a friendly Guard, sequential ambusher attacks, mandatory CC).
- **Units:** a broken Guard in Melee survives the CCPh; the refusal after an Ambush with no ambusher.
- **MapStudio:** the Play page's APh, CCPh, and surrender panels, Fix B, and a withdrawal chosen on the page.

## 8. Not in this pass

In the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 11: vehicles and Hand-to-Hand in CC, concealment and TI in CC, capture attempts, Infiltration and Ambush Withdrawal, overstacking, CX and level changes in an advance, fire into Melee, Unarmed units, No Quarter and Massacre, fire by berserk units, and the two rulings flagged for the user (R29.9 and R29.16).
