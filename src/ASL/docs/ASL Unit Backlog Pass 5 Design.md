# ASL Unit Backlog Pass 5 Design

**Status:** Built. Backlog pass 5 (deviations and small items), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 5.1 to 5.10.

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 5 Review](<Scenario A1 Backlog Pass 5 Review 2026-09-27.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Deviations Pass 4 Design](<ASL Unit Deviations Pass 4 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 15.

Rulings R5.1 to R5.20 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **CX (5.1).** `asl.game.move` takes `doubleTime`: two more MF (one after the unit has spent MF), at most eight (seven for Conscripts), and the unit is CX (A4.5). Portage now reduces every Infantry unit's MF: one for each PP beyond its IPC, which CX lowers by one (A4.42, A4.52; R5.4). CX adds one to the IFT DR of an attack a CX unit makes or directs, once per attack, one to a Gun's To Hit DR when its crew is CX, one to a CX unit's CC attack, and takes one from a CC attack on it; an Ambush dr takes one for a CX side (A4.51; R5.2). An advance or a withdrawal into Difficult Terrain makes a unit CX, and a CX unit may not make it (A4.72, A11.21; R5.5). The counter leaves when the unit breaks or goes berserk, and at the start of its side's next MPh, when a unit that was CX may not Double Time (R5.3).
- **The captor's choice (5.2).** `asl.game.take-prisoner` with `reject` rejects a surrender: the unit is eliminated and its side is faced with No Quarter (A20.3; R5.6). A side faced with No Quarter treats a Surrender result as Berserk. `asl.game.massacre` lets a Russian or berserk unit in its own fire phase eliminate a prisoner in its Location, once per phase, as if using a SW; a berserk unit with enemy prisoners massacres them at the start of its AFPh or DFPh and returns to normal. A Massacre raises the victims' side's ELR by one, once, and faces it with No Quarter (A20.4; R5.7).
- **Owner options (5.3).** A pending choice stops play until the owner answers with `asl.game.choose`: declining the Leader Creation dr (A18.11), refusing Battle Hardening (A15.3), the firer's Unlikely Kill dr after a result that already harmed the vehicle (A7.309), and which Location keeps an Acquisition (R5.8, R5.9).
- **Second Heat of Battle DR (5.4).** A second Original 2 in one attack calls for a second DR under the key `<unit>:2`, unless the first left the unit eliminated, berserk, heroic, or surrendered to a captor (A15.1, A15.5; R5.10).
- **Hero moves on (5.5).** A hero created in the MPh takes his creator's MF spent and joins the moving stack (A15.21; R5.11).
- **HS keeps SW (5.6).** A squad Casualty Reduced to a HS passes its SW to the HS (A7.302; R5.12).
- **Acquisition on units (5.7).** A Gun's Acquisition records the units it fired at and follows them within the Gun's LOS; it is lost when the crew is no longer Good Order or the Gun is unmanned (C6.5, C6.51; R5.13).
- **Vehicle Motion (5.8).** Ending a vehicle's move names the hex it wished to enter next (`intended`), and MP left are spent in its hex with the DEFENDER's window first (D2.4, D2.1, A8.14; R5.14, R5.15).
- **Recall (5.9).** A Recalled vehicle leaves by its side's Friendly Board Edge by a shortest route in MP, in Motion, and is recorded as exited; an immobilized Recalled AFV is Abandoned at the next Player Turn and its crew is placed Stun-recovering (D5.341, D5.41, A2.6; R5.16 to R5.18).
- **Small fixes (5.10).** Grain costs Infantry 1½ MF only from April to September (B15.6; R5.19); a truck's wreck name reads "Passenger survival" (vocabulary 1.9.0); and the Play page fixes of section 5.

## 2. The packages

The Fire, Rally, Close Combat, and Ordnance packages are revised with their prior manifest digests kept. New fragments come from `asl-scenario-a1.pass5-pdf-comparison.json` (31 subjects). New cases: `A1-fire-cx`, `A1-fire-second-heat-of-battle`, `A1-fire-owner-options`, `A1-fire-no-quarter`, `A1-rally-owner-options`, `A1-rally-no-quarter`, `A1-cc-cx`, `A1-ordnance-owner-options`, and `A1-ordnance-cx`. A package reports a missing answer as `asl.a1.<package>.choice-missing:<key>` and an answer the resolution never reaches as `extra-choice`; with no `choices` recorded, every option is taken, so earlier records replay unchanged.

## 3. The live records

- **`choice-pending`** (`ChoicePending`): the key, kind, answering side, options, and the resolution's resume data. The projector refuses any other record while it is pending (UNIT-STATE-035). **`choice-made`** (`ChoiceMade`) records the answer; the planner resumes the resolution with the dice already drawn, and the record's facts carry `choices`, which the projector checks against the answers made.
- **`surrender-rejected`**, **`prisoners-massacred`**, and **`acquisition-changed`** record sections 1's captor choices and Acquisition changes.
- `MovementStepped.DoubleTime`; `VehicleStepped` kinds `remain` (MP spent in the hex) and `exit`; `InstanceStatus.Exited`; `SideState.FriendlyEdge`; `InstanceCreated.Creator` for a hero; `GunAcquisition.Units`; the conditions `asl:cx` and `asl:abandoned`.
- The gate reads back each new record after commit.

## 4. The planner's refusals

A CX unit may not Double Time, advance, or withdraw into Difficult Terrain, and Double Time may not be combined with Assault Movement (A4.61); a unit that massacred in the PFPh does not move; a SMC carrying more than 2PP may not move; a unit carrying more than its IPC may not withdraw, and a Good Order leader may not advance with a MMC carrying more than its IPC where the advance is Difficult for the MMC alone (both deferred, backlog section 15). Grain is refused for Infantry when the game names no month. A leaving Recalled vehicle may not Stop or leave its route.

## 5. The Play page

- A new game names each side's Friendly Board Edge.
- The Movement panel has a Double Time box, cleared after each step. The vehicle panel lists a Recalled vehicle that must leave, offers each exit by its edge, a "wished to enter next" field used when the move ends, and a Recall note naming the edge.
- A pending choice shows its question to all, and its buttons only to the answering side or the adjudicator. A surrender offers the captor's side "reject the surrender" beside the Guard choice. The fire phases offer Massacre buttons.
- In CC each side sees and declares only its own SMC stacking and attacks; the adjudicator sees both. CC labels show printed values ("4-6-7", "8-1"), and a CC record lists the SW its eliminated units leave unpossessed.
- The Units table shows "Stun +1"; a BU toggle used this phase is hidden.

## 6. Tests

- ScenarioA1: `ScenarioA1Pass5Tests` (10) and `ScenarioA1OrdnanceTests.ACxCrewAddsOneToTheToHitDr`.
- Play: `BacklogPass5Tests` (15), with the older step tests updated for the choice steps.
- Studio: `PlayPagePass5Tests` (4) and the CC page test for per-side declarations.
- Authoring: `BuildPass5` verifies the 31 subjects; the four matrix tests pin the new digests.
- Units: vocabulary 1.9.0.

## 7. Not in this pass

The berserk charge route and wrecks stay in backlog section 1. Section 15 lists what this pass defers: dropping SW on a withdrawal, the leader's MF and IPC bonus, the Friendly Board Edge from a scenario card, and routes to more than one edge.
