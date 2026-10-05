# ASL Unit Backlog Pass 9 Design

**Status:** Built. Backlog pass 9 (Mortars, SMOKE, and anti-tank weapons), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 9.1 to 9.4.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 9 Review](<Scenario A1 Backlog Pass 9 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 8 Design](<ASL Unit Backlog Pass 8 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 1 and 19.

Rulings R9.1 to R9.9 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Counters (9.1).** Catalog 1.8.0 adds the German 5cm leGrW 36 and the Russian 50mm RM obr. 40 as light mortars (C9.2). The Panzerfaust is rule-defined, inherent in German Infantry (C13.3). The Panzerschreck and the ATR are not added, since no registered source prints their counters' values; this is a question for the user.
- **Area fire (9.2).** A light mortar is a SW its possessor fires, in the PFPh, DFPh, or AFPh, or as Defensive First Fire in the MPh at the moving units, always on the Area Target Type: the red Area row of the To Hit Table with the C4 modifications, within its minimum and maximum range (C9.4). The To Hit DR is judged for every unit of the target hex, each with its own Case K, Area Target Acquisition, and overstacking; Cases C1 to C4, E, G, L, and Q do not apply (C3.331). A Critical Hit needs an Original 2, judged per unit with one subsequent dr (C3.6, C3.7). Each unit hit is attacked at half the HE FP with its TEM on the IFT, woods giving the -1 of Air Bursts (B13.3); a Critical Hit attacks at the full FP doubled with positive TEM reversed. A Spotter in or next to the mortar's hex lets it fire along its LOS in the PFPh or DFPh, at +2 and with the Multiple ROF one lower (C9.3, C9.31). A leader in the firer's Location may direct the shot, and its further ROF shots, with his leadership modifier (A7.531, A7.53). A mortar does not fire from a building Location (B23.423), nor in the AFPh after it moved (A4.41), nor at a hex holding friendly units (a deviation) or a vehicle.
- **SMOKE (9.3).** In its MPh a squad with a Smoke Placement Exponent in the moving stack places SMOKE grenades once, in its Location for 1 MF or an ADJACENT one at its level for 2 MF, on a dr at most its exponent, +1 if CX; a 6 ends its MPh (A24.1, A4.51). The counter is a +2 Hindrance into, through, within, or out of its Location, +1 more out of or within it, at most +3 per Location with a Blaze's smoke; FFMO does not apply to a target in it; entering it costs one more MF or MP; its +2 applies to Residual FP (A24.2, A24.7, A24.8, A8.2). It leaves at the end of the MPh (A24.11). WP and ordnance SMOKE are not reached: no nationality in the game has WP grenades and no catalog Gun lists SMOKE.
- **Anti-tank weapons (9.4).** From October 1943 a Good Order or berserk German unit that may still fire makes a PF Check dr (-1 in 1945, +1 for a HS or crew, +2 for a SMC, +1 if CX): 1 to 3 gives a shot at once, an Original 6 pins the unit, or breaks it if pinned, or reduces it if berserk or heroic (C13.31). A squad makes one check whatever it has fired, and a second only while its first check is its only fire; any other unit only while it has not fired. The PF fires at an enemy AFV at TH# 10 less 2 per hex, at 1 hex before June 1944, 2 to the end of 1944, and 3 in 1945, with the LATW DRM, among them Case C3 for the AFPh and for the Backblast of a ground-level building; an Original 12 (11 or 12 for Inexperienced Infantry) misses and reduces the firer (C13.36). A hit is resolved at HEAT TK# 31. The side's PF shots are limited by its squad equivalents at setup: as many before 1944, one and a half times in 1944, and twice in 1945.

### Pass 9b

- **ATR and Panzerschreck (rulings R9.10, R9.11).** Catalog 1.9.0 adds a manufactured Russian ATR and German Panzerschreck (sheet MFG, ruling R0.3). A unit possessing either fires it at a vehicle through `asl.game.fire-ordnance`. The ATR fires AP on the C3 To Hit Table's black Vehicle row, up to 12 hexes, with the AP To Kill Table's Russian ATR TK# 6, and malfunctions on 11 or 12. The PSK fires HEAT on its own To Hit Table from September 1943 at TK# 26, with Backblast, and is removed on its X# of 11. Both take the LATW DRM and a leader's modifier. The ATR also adds 1 FP to its unit's fire at Personnel within 12 hexes, leaving no Residual FP. The Ordnance package reads `asl:latw` definitions (`GunDefinition.LatwType`, `ToHitTable`), and the Fire package admits an ATR as a weapon of a fire group.

## 2. The packages

All four Scenario A1 packages are revised for catalog 1.8.0 with their prior manifest digests kept. The Ordnance package pins the To Hit Table's Area row (`c3-to-hit-area-row.transcription.json`) and adds `OrdnanceShot.TargetType` (`area`), `Spotter`, `Director`, and `Panzerfaust` (shots taken and allowed, a ground-level building, heroic); `OrdnanceRolls.PanzerfaustCheck`; and `OrdnanceResolution.AreaTargets`, `PanzerfaustCheck`, and `FirerEffect`. `ScenarioA1AreaCalculator` resolves the Area Target Type; the Panzerfaust runs through the Vehicle Target Type code with its own To Hit and no ROF, breakdown, or Acquisition. The Fire package adds `FireOrdnanceHit.Area`: the TEM on the Effects DR and Air Bursts. New fragments come from `asl-scenario-a1.pass9-pdf-comparison.json` (23 subjects); new cases: `A1-fire-area-hit`, `A1-ordnance-light-mortar`, `A1-ordnance-area-target`, `A1-ordnance-spotting`, `A1-ordnance-panzerfaust`, and `A1-ordnance-panzerfaust-outside`.

## 3. The game model and records

- `movement-step` may carry a SMOKE attempt (`smokeBy`, `smokeAt`, `smokeRoll`, `smokeDr`, `smokeExponent`) made in the movers' Location; a placed counter is an `asl:smoke` entity whose id ends `-smoke-grenade`, removed when the MPh ends. `MovementState.EndingMembers` ends the move of a squad whose dr was a 6 when the DEFENDER's window closes.
- `ordnance-fired` is admitted for a light mortar fired by its possessor and for a PF, recorded as `<unit>:pf`; a PF Check that gave no shot is recorded with no To Hit DR.
- `GameState.SmokeAttempts`, `MortarSpotters`, `PanzerfaustShots`, `SetupHalfSquads` and `SetupClosed` (the OB at the end of setup), `SupportWeaponUses` (squads whose only fire this phase is one SW), `SupportWeaponDirectors`, and `MovedWeapons`.
- A PF's firer is pinned, broken, or reduced by events the record causes; the Spotter and the directing leader carry the phase's fire marker.
- Actions: `asl.game.move` takes `smoke` and `smokeBy`; `asl.game.fire-ordnance` takes `spotter` and `director`, and a PF as `gunId` `<unit>:pf`.
- Vocabulary 1.13.0: a light mortar's `asl:caliber-suffix` and `asl:dates`.

## 4. The Play page

- The move panel offers SMOKE grenades for a checked squad, naming the Location.
- The Ordnance panel lists light mortars with the Guns, and from October 1943 each German unit's Panzerfaust; a mortar offers a Spotter and a leader, a PF a leader.
- The ordnance records show each unit's Final DR of an Area shot, the PF Check, and the firer's fate.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass9Tests` (15).
- Play: `BacklogPass9Tests` (11).
- Authoring: `BuildPass9` verifies the 23 subjects; the matrix tests pin the new digests, fragment counts, and the Area row.
- Units: catalog 1.8.0, vocabulary 1.13.0. CounterSheets: the 36 new rows.

## 6. Not in this pass

Section 19 of the backlog lists what this pass defers, and section 1 its three deviations: friendly units in a mortar's target hex, Residual FP from ordnance, and SMOKE placement at another level.
