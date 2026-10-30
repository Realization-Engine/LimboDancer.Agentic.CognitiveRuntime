# ASL B25.2 Blaze Smoke Claim Design

**Status:** Design, 2026-10-29, revised after a sub-agent's review (section 9), waiting for the user's approval. The B25.2 claim (Claude; the inventory's section 6, claimed on main as 8c7f5529, its holds corrected in 9991609d), on the branch `claim/b25.2-blaze-smoke`. It takes the SMOKE of a Blaze, which the A24.5 claim left (ruling RA24.6). Rules only: no caller changes, so no recorded game replays differently. Rulings are numbered RB25.n; RB25.4's second case is a reading the user may veto.

## 1. The rule (B25.2, p. 143; B25.63, B25.651, p. 144; D9.4, p. 210; A9.22, A9.222; A24.2, A24.4 to A24.8, pp. 91 to 93; E3.53, E3.734)

- **B25.2:** a Burning Wreck or a terrain Blaze is shrouded by smoke up to four levels above the level of the Fire in its hex [EXC: none in Heavy Winds (B25.63); two levels in a Mild Breeze (A24.4)], with no counter placed. A Wreck Blaze is a +2 Hindrance, "rather than the +3 Hindrance DRM of regular smoke", so a terrain Blaze is +3. The SMOKE replaces the wreck's own Hindrance (D9.4) except on an already established Fire Lane (A9.22) or in Heavy Winds, where only the wreck's Hindrance applies. Both Blazes can create drifting Dispersed SMOKE (A24.61) in a Mild Breeze. Flames make no smoke. Fire has no other effect on LOS.
- **Around it:** A24.2 caps a Location's SMOKE at +3, cumulative per source, and A24.8 adds +1 for a LOS traced out of or within the hex. A24.5: a Mild Breeze lowers no source's Hindrance. A9.222: SMOKE is never a DRM to a Fire Lane, while a wreck is a "hard" Hindrance. B25.63: Heavy Winds leave SMOKE no effect, and No Wind always holds in a non-rooftop Location of an Interior Building hex. B25.651: Gusts do not affect a Blaze's smoke. E3.53, E3.734: a Blaze's SMOKE survives Rain, Mud, and Deep Snow, but does not drift in Rain. A24.6: SMOKE has no effect where fog already hinders.
- **What each side knows:** a Blaze, a wreck, a Fire Lane, the weather, and the wind are on the map or in the scenario for both sides.

## 2. What decides it today

- **Inside Rules:** nothing decides B25.2 by name. The +2 a source that the count members of A24.2's total multiply (`ScenarioA1SmokeStrengthCalculator.CountedSourceHindrance`, the A24.5 claim's, held whole by it) stands for a grenade and a Wreck Blaze alike, in `ScenarioA1VehicleSightRules.SmokeDrm(int ...)` and `OwnHexHindrance(int, bool)`, `ScenarioA1FireMapRules.ResidualMapFacts(..., int)`, `ScenarioA1Sniper.TargetTem(string?, int)`, and `ScenarioA1OverrunCalculator.OverrunSmoke(int)`. There is no terrain Blaze (B25.1), no wind (B25.63), and no SMOKE height in LOS (A24.4).
- **Outside Rules**, each counting a burning wreck as a +2 source, dropping its D9.4 +1, or both:
  - `Play/GamePlanner.Wrecks.cs`: `SmokeSources` (:143) and the hindering filter (:97 to :104) of `VehicleHindrance`, which `Rout.cs:420` also reads (Interdiction); `HasSmoke` (:149).
  - `Play/GamePlanner.Ordnance.cs:296` to `:299` (C5.5's own hex: the wreck filter and the sources), `Fire.cs:1706` (Residual FP), `Overrun.cs:148`, `Snipers.cs:488`, `FireExtensions.cs:233` (the "in SMOKE" fact).
  - `Play/GamePlanner.FireExtensions.cs:150`, `FireLaneMap.Los`: a Fire Lane's Residual FP reads only the map's Hindrance, so a burning wreck adds nothing there today, where B25.2 and A9.222 give it the wreck's +1.

## 3. Rulings

- **RB25.1 One calculator.** `ScenarioA1BlazeSmokeCalculator` and `ScenarioA1BlazeSmokeContract.cs`. Its small enums, `BlazeKind` (WreckBlaze, TerrainBlaze, Flame) and `WindForce` (None, MildBreeze, HeavyWinds, Unknown), sit in the calculator's file.
- **RB25.2 The wind a Blaze feels.** Every member reads the wind as None in a non-rooftop Location of an Interior Building hex (B25.63), and Gusts never matter (B25.651).
- **RB25.3 The Hindrance.** `Hindrance(kind, wind)`: a Wreck Blaze +2, a terrain Blaze +3, a Flame none; none in Heavy Winds; a Mild Breeze, Rain, Mud, or Deep Snow lowers nothing (A24.5, E3.53, E3.734); null when the wind is not known. The value joins A24.2's total by its by-value overload, so a Wreck Blaze and a grenade in one Location cap at +3, and A24.8's +1 is added there. Fog is A24.6's, its own row.
- **RB25.4 The wreck's own Hindrance.** `WreckBlazeLos(fireLaneResidualAttack, wind)` gives two flags for a burning wreck's Location: whether its SMOKE applies, and whether D9.4 applies to it as if it were not burning. SMOKE, and no D9.4, as a rule; on a Fire Lane's Residual FP attack (A9.22, A9.222) or in Heavy Winds, D9.4 and no SMOKE. D9.4's own conditions (a same-level LOS through the hex, a wreck both ends see, Bypass, Case J) stay with the D9.4 calculator, which the caller asks next. **A reading open to veto:** a non-burning wreck or an AFV in a terrain Blaze's hex keeps its D9.4 +1 beside the Blaze's +3, since D9.4 exempts only a burning wreck and B25.2 replaces only the burning wreck's own Hindrance; the other reading drops it.
- **RB25.5 The height.** `Height(wind)`: four levels above the Fire, two in a Mild Breeze, none in Heavy Winds; null when the wind is not known. Its use in a LOS waits for A24.4's SMOKE heights in LOS.
- **RB25.6 Drift.** `DriftCounters(kind, wind, rain)`: in a Mild Breeze and not in Rain, a Blaze can create drifting Dispersed SMOKE of as many counters as its Hindrance (A24.61): 2 for a Wreck Blaze, 3 for a terrain Blaze; none otherwise. The drift itself is A24.61's procedure.
- **RB25.7 Old members.** None decides B25.2 by name; each count member carries the Wreck Blaze's +2 through `CountedSourceHindrance` and keeps its answer, its summary naming `Hindrance` (held shared: `ScenarioA1VehicleSightRules.cs`; the others are named in the wiring row and left untouched, since the A24.5 claim's by-value overloads already take the value).
- **RB25.8 The contract `b25.2-blaze-smoke`.** What does this Blaze add to a LOS through its Location? Facts, all disclosed to All: `kind`, `wind`, `interiorNonRooftop`, `fireLaneResidualAttack`, `rain`. Outcomes: `smoke` (its Hindrance, height, and drift in the reason), `wreck-only` (the wreck's D9.4, asked of a Wreck Blaze only), `none`, `not-evaluated` (also for a Fire Lane fact on a terrain Blaze or a Flame, which have no wreck). Place: any LOS check (fire, Defensive First Fire, Interdiction, a Sniper).

## 4. Out of the claim

Nothing to the backlog. A terrain Blaze (B25.1), the wind (B25.63), SMOKE heights in LOS (A24.4), drift (A24.61), and fog (A24.6) are their own rows; the calculator answers for each once it exists.

## 5. The wiring row W-B25.2

- Every site of section 2: hand each burning wreck's `Hindrance` to A24.2's by-value total (with W-A24.5), and ask `WreckBlazeLos` before D9.4's own test.
- `Play/GamePlanner.FireExtensions.cs:150`: a Fire Lane's Residual FP gains the burning wreck's D9.4 +1 (A9.222).
- The wind (B25.63, with the interior exception), the height (with A24.4), and drift (A24.61) as each is built.
- Old members to remove once callers move: the count members' Wreck Blaze use (RA24.6).
- Behavior changes: a Fire Lane's Residual FP gains +1 for a burning wreck where it had none. None other while there is no wind and no terrain Blaze.

## 6. Tests and minigame

- `ScenarioA1BlazeSmokeCalculatorTests`: each kind's Hindrance in each wind and under Rain, Mud, and Deep Snow; indoor No Wind; Unknown wind for each member; the heights; the two flags off and on a Fire Lane's attack and in Heavy Winds; drift counts, none in Rain; a Wreck Blaze with a grenade capped at +3 through A24.2's by-value total.
- The contract: its outcomes, `not-evaluated` for impossible pairs, every fact disclosed to All.
- A new bundle `b25-blaze-smoke` on an imported board: a burning wreck's +2, a terrain Blaze's +3, a Fire Lane's +1, Heavy Winds, a Flame, indoor No Wind.

## 7. Memory and performance

Pure static checks over a few facts; no state, cache, or event.

## 8. Build outputs outside the repository

The filtered test builds in `asl-gate-work`, registered as made, in the worktree `asl-gate-work/wt-b252`; a Studio check of the bundle on the Rule Workbench in Claude's Studio (6670), stopped when done.

## 9. Review of the design

A sub-agent read the first draft against pp. 91 to 93, 143, 144, and 210 (B25.14, B25.63, B25.651, A9.22, A9.222, D9.4, E3.53, E3.734) and the code, read-only (2026-10-29): ten findings, all taken. Blocking: a Fire Lane's Residual FP is read by `FireLaneMap.Los`, which sees no burning wreck today, so the change is +1 where there was none (A9.222), and the fact is the Fire Lane's attack (RB25.4, the wiring row); D9.4's +1 keeps its own conditions, so RB25.4 gives two flags; six more Play sites count a burning wreck's +2, and every count member is named (section 2, RB25.7). Also taken: indoor No Wind (RB25.2); weather, with fog left to A24.6 (RB25.3); a wreck in a terrain Blaze, a reading open to veto (RB25.4); drift counts (RB25.6); the contract's place and impossible pairs (RB25.8); A24.8's +1 (section 1); the missing tests. Confirmed: a terrain Blaze's +3, Heavy Winds leaving no SMOKE, the height from the Fire's level, a Flame without smoke, and a Mild Breeze lowering nothing.
