# ASL A24.5 Smoke Strength Claim Design

**Status:** Design, 2026-10-29, revised after a sub-agent's review (section 9), waiting for the user's approval. The A24.5 claim (Claude; the inventory's section 6, claimed on main as c2d5b07b), built with the E10 claim as one task on the branch `claim/a24.5-e10` (the user, 2026-10-29; `ASL E10 Ammo Vehicles Claim Design.md`). Rules only: no caller changes, so no recorded game replays differently. Rulings are numbered RA24.n.

## 1. The rule (A24.5, p. 92; A24.2, A24.11, p. 91; A24.8, A24.61, pp. 92 and 93; D13.31 to D13.35, p. 212)

- **A24.5:** a smoke counter has a full-strength Hindrance of +3 and a WP counter +2. A counter is placed full-strength side up only if placed by ordnance or OBA as the first resolved attack of its PFPh, "before non-SMOKE attacks are made". All other ordnance and OBA placement uses the Dispersed side, one less. Smoke grenades are +2, WP grenades +1. A Mild Breeze does not lower a SMOKE source's Hindrance.
- **A24.2:** the Hindrance is cumulative for each SMOKE counter along the LOS, and one Location's SMOKE is never more than +3, except outgoing LOS (A24.8). **A24.8:** fire traced out of or within a SMOKE Location adds +1. **A24.11:** grenade SMOKE is a 1/2" counter, never Dispersed. **A24.61:** drifting SMOKE is a number of Dispersed counters.
- **D13.31 to D13.34:** smoke dischargers, mortars, pots, and the Nahverteidigungswaffe place white Dispersed SMOKE. **D13.35:** vehicular smoke grenades are treated as A24.11.
- **Not this rule:** a Wreck or terrain Blaze's SMOKE (B25.2, p. 143: a Wreck Blaze +2 "rather than the +3 ... of regular smoke", with its own Heavy Winds and Fire Lane conditions) is B25.2's.

## 2. What decides it today

Inside Rules (the claim's to merge):
- `TurnWhiteSmokeRules.InitialStrength` (`TurnWhiteSmokeRules.cs:44`) **decides** the placement strength and value of ordnance, OBA, and dispenser SMOKE: 3 or 2 by chemical, one less unless full; full only for ordnance or OBA in the owner's PFPh before non-SMOKE fire; dispensers always Dispersed (even when the window is unknown); grenades, Drift, Blaze, Cave, and Napalm not evaluated; an undefined enum throws. No production caller, only its tests.
- **Four copies of "+2 a source, at most +3"**, each **deciding** A24.5's grenade value inside A24.2's total: `ScenarioA1VehicleSightRules.SmokeDrm` (`:50`, with A24.8's +1), `ScenarioA1FireMapRules.ResidualMapFacts` (`ScenarioA1FireMapRules.cs:92`), and `ScenarioA1OverrunCalculator.OverrunSmoke` (`ScenarioA1OverrunCalculator.cs:85`, with A24.8's +1). The +2 is right for a smoke grenade, and for a Wreck Blaze by B25.2; it is wrong for any other source.
- `ScenarioA1VehicleSightRules.OwnHexHindrance` (`:73`) and `ScenarioA1Sniper.TargetTem` (`ScenarioA1Sniper.cs:449`) **read** `SmokeDrm` by a count.
- `TurnWhiteSmokeRules.Evaluate` and `Opening` decide A24.4's removal and dispersal: A24.4's (RA24.7).
- `ScenarioA1SmokeCalculator.cs` holds A24.1's `Plan` and `Verify` (grenade placement): not touched.

Outside Rules (track 3's, the wiring row): `Play/GamePlanner.Wrecks.cs` `SmokeSources` (:143) **gathers** only a count of sources (burning wrecks and active `asl:smoke` entities), and `VehicleHindrance` (:129) totals them by hex, not Location; `GamePlanner.Ordnance.cs:296` (also by hex), `GamePlanner.Snipers.cs:487`, `GamePlanner.Fire.cs:1706`, and `GamePlanner.Overrun.cs:148` **gather** the same counts. `ASL.Turns/TurnAtmosphere.cs:40`, the Authoring source review, and two MapStudio Turn labels name A24.5 in text only.

Play makes only smoke grenades today; there is no WP, ordnance, OBA, dispenser, or drifting SMOKE in play.

## 3. Rulings

- **RA24.1 One calculator, in new files.** `ScenarioA1SmokeStrengthCalculator.cs` and `ScenarioA1SmokeStrengthContract.cs`. The facts reuse `TurnSmokeSource`, `TurnSmokeChemical`, and `TurnSmokeStrength` (`TurnWhiteSmokeRules.cs`), so nothing is duplicated and the forwarding is exact.
- **RA24.2 A source's Hindrance.** `Hindrance(source, chemical, strength)`: a full counter +3 (conventional) or +2 (WP); Dispersed one less (+2, +1); a grenade (infantry or vehicular) +2 or +1, never Dispersed (a Dispersed or Full grenade is inconsistent facts: null); a dispenser's white Dispersed SMOKE +2 (WP from a dispenser null, as today); drifting SMOKE the Dispersed value (A24.61). Null for an unknown fact, and for Blaze, Cave, and Napalm, which are other rules'.
- **RA24.3 The placement's side.** `PlacementStrength` is `InitialStrength`'s body moved, with the same signature, result record, reason text, and throws: full only for ordnance or OBA in the owner's PFPh before any non-SMOKE attack, Dispersed otherwise, dispensers always Dispersed, unknown when an ordnance or OBA placement's window is not known. A24.5 refuses no placement; it only chooses the side.
- **RA24.4 A Mild Breeze** lowers nothing, so there is no member; the calculator's summary cites the sentence. A24.4's height in a Mild Breeze stays A24.4's.
- **RA24.5 The totals stay A24.2's and A24.8's, by value.** New overloads take each source's value instead of a count: `SmokeDrm(IReadOnlyList<int> hindrances, firerThere, targetThere, crossed)` (capped at +3 before A24.8's +1, `crossed` read only when neither end is there), `OwnHexHindrance(IReadOnlyList<int>, wreckOrAfv)`, `TargetTem(terrain, IReadOnlyList<int>)`, `ResidualMapFacts`'s total, and `OverrunSmoke(IReadOnlyList<int>)`. Each old count member becomes a one-line call with +2 a source: the same answer for every count of 0 or more. A negative count, which no caller makes, keeps its old arithmetic answer.
- **RA24.6 B25.2 stays where it is.** A Wreck Blaze's +2 is B25.2's (Heavy Winds, an established Fire Lane). Until a B25.2 claim, the wiring counts a Wreck Blaze through the old count member, as today, and hands grenades by value; B25.2's row is left to its claim.
- **RA24.7 Out of this claim: A24.4.** `TurnWhiteSmokeRules.Evaluate` and `Opening` stay; `InitialStrength` becomes a one-line call into `PlacementStrength`.
- **RA24.8 The contract `a24.5-smoke-placement`.** Which side up does this placement take, and what is its Hindrance? Facts, all disclosed to All (the counter is on the map, WP is announced, and the PFPh's order of attacks is public): `source` (choice), `chemical` (choice), `ownerPrepFire` (flag), `beforeNonSmokeFire` (flag, may be unknown). Outcomes: `full`, `dispersed`, `not-evaluated`; the reason gives the Hindrance (RA24.2) and cites A24.5 (or D13.3x for a dispenser, A24.11 for a grenade). Places: the PFPh, ASOP 2.12A, for ordnance SMOKE fired first; any fire phase for ordnance SMOKE otherwise; the MPh for a grenade. Event: `instance-created` (the counter, its side up).
- **RA24.9 Old members.** `InitialStrength`, `SmokeDrm(int ...)`, `OwnHexHindrance(int ...)`, `TargetTem(string?, int)`, `ResidualMapFacts`'s count, and `OverrunSmoke(int)` keep their callers and answers and forward. None keeps a different answer.

## 4. Out of the claim

Nothing to the backlog. A24.4 (height, duration, removal), A24.6 and A24.61's drift procedure (weather, drift), A24.3 (WP's effects), and B25.2 (Blaze SMOKE) are their own rows. Play placing ordnance, OBA, WP, dispenser, or drifting SMOKE is those rules' and the wiring's.

## 5. The wiring row W-A24.5

- `GamePlanner.Wrecks.cs` `SmokeSources` and `VehicleHindrance`, `GamePlanner.Ordnance.cs:296`, `GamePlanner.Snipers.cs:487`, `GamePlanner.Fire.cs:1706`, `GamePlanner.Overrun.cs:148`: gather each SMOKE counter's source, chemical, and strength, ask `ScenarioA1SmokeStrengthCalculator.Hindrance`, and hand the values to the by-value overloads, by Location rather than by hex (A24.2); Wreck Blazes as in RA24.6.
- Old members to remove once callers move: the count overloads of RA24.9 (the Wreck Blaze's until B25.2 is claimed) and `TurnWhiteSmokeRules.InitialStrength`.
- Behavior changes: none in play today (grenades keep +2); SMOKE in two Locations of one hex no longer adds together.

## 6. Tests and minigame

- `ScenarioA1SmokeStrengthCalculatorTests`: each source's value, full and Dispersed, WP, grenades, a Dispersed grenade null, dispensers, drift, the other rules' sources null; `PlacementStrength` equal to `InitialStrength` (strength, Hindrance, and reason) over every source, chemical, and window, with the throws; each by-value overload equal to its count member for counts 0 to 3, the `crossed` laziness kept, and a negative count unchanged.
- The contract: its outcomes and not-evaluated cases.
- A new bundle `a24.5-smoke` on an imported board, with its card and checks: a grenade +2, a WP grenade +1, ordnance SMOKE fired first full +3, after a non-SMOKE attack Dispersed +2, WP full +2, a dispenser Dispersed +2, an unknown window not evaluated.

## 7. Memory and performance

Pure static checks over a few values; the by-value overloads take a short list a caller already holds. No state, cache, or event.

## 8. Build outputs outside the repository

The filtered test builds in `asl-gate-work`, registered as made; a Studio check of the new bundle on the Rule Workbench in Claude's Studio (6670), stopped when done.

## 9. Review of the design

A sub-agent read the first draft against pp. 91 to 93, 143, and 212 and the code, read-only (2026-10-29): ten findings, all taken. Blocking: two more Rules copies of the +2 (`ResidualMapFacts`, `OverrunSmoke`), now in the claim as shared files; the Blaze values decided B25.2, which the claim does not hold (RA24.6); `ScenarioA1SmokeCalculator.cs` already exists (A24.1's), so the new files are named apart. Also taken: the enums reused so `PlacementStrength` forwards exactly, with the dispenser's unknown window; the `crossed` laziness and negative counts; a `TargetTem` overload; `ScenarioA1FireCalculator.cs` dropped from the holds (a false match); the Ordnance site's hex; drifting SMOKE's value; one placement question in the contract, with its places; the old members' tests. Confirmed: the values of A24.5 and D13.31 to D13.35, that A24.5 refuses no placement, the Mild Breeze, and A24.4 left out.
