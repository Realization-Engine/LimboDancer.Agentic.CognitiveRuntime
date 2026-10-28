# Pass 72: the reviews' fixes

Read `brief_build.md` beside this file first; it binds you. Then the four reviews beside it (`review_r1_heat_of_battle.md`, `review_r2_substitution.md`, `review_r3_table.md`, `review_r4_ui_boundary.md`; cited below as R1.n, R2.n, R3.n, R4.n) and the design's section 6 and sections 8 to 12. Head before the fixes: db15561b. The rulings R72.17 to R72.22 below are decided under the user's word of 2026-10-27 and bind you as the earlier ones do.

## The rulings of the reviews

- **R72.17** (amends R72.3 and R72.4; R1.1, R1.2, R1.3, R1.4, R1.5, R3.7): a rule bar is A15.45's terrain only: a cliff or a Water Obstacle (an unbridged one; a Blaze is not built). A building connection (level, stairwell, upper level, Rooftop, Cellar) is ordinary movement that the shortest route already prices, never a bar. A15.45's comparison applies to a nearest target only when a bar lies across its would-be route: the route that ignores bars is cheaper than the real one. It compares the real route's hexes with the range of the next group that has a reachable target; with no such group, the charge stays on the nearest. "Still closer" is strict: at a tie the targets are equidistant and the ATTACKER chooses among both groups (A15.43). A15.44's verdict (Battle Hardening at the MPh's end) is given only when a Known enemy unit is in LOS, none can be reached, and every refusal that kept one out of reach is a bar; a target cut off only by other enemy Locations, or a gap of the model, keeps R30.5. A unit with no Known enemy in LOS returns to normal by A15.431 and is never hardened.
- **R72.18** (amends R72.12; R2.1, R3.2): under Ammunition Shortage a SW's B# is one lower and, by A.11, its Original B# becomes its X# (a B11 LMG is B10 and X11); only a printed X# (a FT's removal number) is itself one lower. Depletion Numbers are not lowered for a captured Gun (A21.11; R2.3).
- **R72.19** (R2.4): a morale check record carries the stamp `unitSubstitution`; with it, its `underscoredElr` is checked against the state.
- **R72.20** (R1.8, R1.10): a destroyed vehicle's PRC are eliminated with it (A11.5) and are not Known enemies left for A15.46; CC's A15.46 counts Known enemy units only, as fire does.
- **R72.21** (R1.7): a vehicle firing at range 0 into its own Location outside the MPh is TPBF by A7.211 and stays allowed; the comment is corrected.
- **R72.22** (R2.6): A19.132 reaches ELR failures only; the Shortage's Unit Replacement keeps A19.13's EXC (an underscored squad becomes its two HS).

## Round A: the charge and Battle Hardening (Rules charge, the MPh end, the choice and its record)

1. R72.17 in `ScenarioA1ChargeCalculator` (one search still: carry a relaxed cost beside the real one; the memo key unchanged; `ChargeSearches` stays 1).
2. R1.6's bridge: a `Bridged` fact on `ChargeEntryFacts`, decided in `RuleBar` (still a gap until the movement model reads bridges); Play only reports it.
3. Texts (R3.5, R3.8, R3.6, R4.1): the unreachable text names the bar met (or "across terrain a berserk unit will not charge through", A15.45), "except across", no repeated hex or "stays in place" on the panel line; the "no route" line says "to no Known enemy unit in its LOS (the nearest [A3])" and cites R30.5, not A15.431; the Battle Hardening choice names the unit with its hex ("[A8]: 4-4-7 squad R1").
4. The record (R3.1, R4.2): a `ChoiceMade` of key `berserkHardening:` is worded through `HardeningText`: the unit, its hex, taken or refused, and what it became (the new unit or Fanatic); ReplaySteps names the `battle-hardening` kind in its pending and answered titles.
5. R4.9: `PlanBerserkHardeningAnswer` applies `BerserkEndsConditions()` to the produced unit; correct the comment.
6. R4.7: pre-filter the MPh and berserk before reading a unit's location at the phase advance.

Tests by method: the Rules charge tests you change or add (an ordinary charge past a multi-level building keeps the nearest; a bar across the route and the next group reachable; the next group unreachable keeps the nearest; a tie gives both groups' steps; cut off by enemy Locations keeps R30.5; no Known enemy in LOS gives no hardening), `BacklogPass72Tests`' charge and hardening methods, the `BacklogPass27Tests` and `CloseCombatStepsTests` charge methods 5a ran. Re-play the three 5a games' rulings in your head against R72.17 and say whether `games_5a.py` needs a change.

## Round B: fire, ordnance, Unit Substitution, the hero (Rules fire and ordnance, LiveFire, LiveOrdnance, the record lines)

1. R72.18: `WeaponEffects`' removal threshold without `- Shortage(weapon)` for a weapon with no printed X#; the test `ASquadFiringOnlyItsLmgIsNotReplacedAndItsLoweredBreakdownNumberIsItsX` expects B10 X11; `ShotDepletion` skips a captured Gun.
2. R72.19: the stamp on morale check records; the verifier compares `underscoredElr` with the state on a stamped record.
3. R72.20: A15.46 after a vehicle kill and after fire subtracts the destroyed vehicle's PRC; CC's check filters concealed enemies (`ScenarioA1CloseCombatCalculator` ~916).
4. Boundary (R1.6, R3.11, R4.11): `SelectTargets` decides TPBF from a firer-in-target fact Play hands it; `heroWeapons` is set for any hero crew and `HeroDrm` filters the gun type; the Replaced ids are a fact on `BerserkAttackFacts`, decided in `BerserkReturnsAfterAttack`. R72.21's comment.
5. Texts (R3.3, R3.4, R3.9, R3.10, the known items): a weapon whose recorded B# differs from its printed one says the number and its cause ("X11 (B11 ...)", "B10 (B11, less 1 for Ammunition Shortage, A19.131)", "less 1 in Inexperienced hands, A19.32"); the PSK's +1 cites A15.23; the A15.46 return line says "its TPBF" or "its FT"; the shortage line lists every candidate's dr; the PSK is never named "The PSK -" (find where the dash comes from; fix it for every weapon so named); the ordnance proposal's DRM list is worded as the record's ("hero's SW, 1-4-9 hero G1"), not "hero:1-4-9 hero G1"; `ordnance.crew-outside` says a lone leader may not fire a PSK.
6. Perf (R4.5, R4.6, R4.8): `BerserkReturnsAfterFire` tests for a berserk firer before taking the index; `BerserkClearedBy` reads a lazy EventId dictionary; the token parsers test the prefix before splitting.
7. R4.13: the Fire Lane fields are hidden under Ammunition Shortage for the firing side (a Rules query); if hiding them changes an id a test reads, run those methods.
8. R2.5: render A25.72's Finnish progression chart (p. 97) from the PDF; if the Sissi 8-3-8 or 3-3-8 is on it, add their rows on the SSR path only; say what you found.
9. R72.22: a Rules test pins an underscored squad under both SSRs.

Tests by method, as `brief_build.md` says; say the list first.
