# ASL Unit Backlog Pass 13 Design

**Status:** Built. Backlog pass 13 (Rally, Rout, and support weapons), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 13 Review](<Scenario A1 Backlog Pass 13 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 12 Design](<ASL Unit Backlog Pass 12 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 23.

Rulings R13.1 to R13.8 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **The RtPh (R13.3).** `asl.game.rout` names a broken unit's whole route, or one Location by Low Crawl. A unit must rout when a Known unbroken armed enemy unit is ADJACENT or in its Location, or it is in Open Ground in the LOS and Normal Range of a Known unbroken enemy unit; it may rout under DM (A10.5). The ATTACKER's units that must rout and can go first. A route never enters a Known enemy's Location, never goes ADJACENT to one unless leaving its Location, never closes on a Known armed enemy that has seen it, and does not end ADJACENT to one it began ADJACENT to (A10.51). It must reach the nearest woods or building Location within its six MF (three for a wounded SMC), found by a search that tracks which enemies have seen it; ignorable cover no farther in MF may be chosen. Entering Open Ground an unbroken Known Infantry unit could Interdict takes a NMC against the broken Morale Level: Casualty Reduction (a HS routs on), pin (the rout ends), or elimination on an Original 12 (A10.53). An Encircled unit's first step costs double.
- **The end of the RtPh (R13.3).** A broken unit ADJACENT to or in the Location of a Known unbroken armed enemy unit, or one that did not rout from Open Ground, is eliminated for Failure to Rout, unless it surrenders to ADJACENT Good Order enemy Infantry (A20.21); a unit that can get away only by Interdiction or Low Crawl, or is Disrupted or Encircled, may not rout and surrenders.
- **DM (R13.1).** A broken unit comes under DM when a Known armed enemy unit is ADJACENT or in its Location, after any action or phase change; at the start of the RtPh in Open Ground in the LOS and Normal Range of a Known enemy unit; and it keeps DM as the RPh ends when its owner names it (`retainDm`) outside woods and buildings (A10.62).
- **Squads (R13.4).** `asl.game.deploy`: in its own RPh a Good Order squad with a Good Order leader of its nationality takes a NTC modified by his leadership (Guards take it unmodified with no leader) and becomes two HS, the SW going to the first unless named for the second. `asl.game.recombine`: two Good Order HS of one kind with such a leader (Guards need none) become their squad in any RPh. Either is the sole RPh action of the units, the new units, and the leader; a leader may permit any number of Recombinations.
- **SW (R13.5).** `asl.game.transfer` between Good Order unpinned units in one Location in the RPh, or in the ATTACKER's APh before either advances, never in the phase of a Recovery. `asl.game.drop` during the unit's MPh move, before it advances, at the start of the CCPh, or, for a broken unit before it routs, the SW beyond its IPC toward its best load (A4.43, A10.4). `asl.game.recover` on a Final dr below 6 (+1 CX) as the RPh's sole action, or in the MPh for one MF; a SMC Recovers from its broken friendly unit (A4.44).
- **Dismantling (R13.6).** `asl.game.dismantle` toggles the German MMG in its side's PFPh or DFPh if it has not fired there; it counts as the MG's use and, in the PFPh, as its possessor's use of a SW, so the unit does not move. A dismantled MG is not fired and portages at half its PP (A9.8).
- **Captured MG (R13.7).** A MG or ATR of an enemy nationality fires with its B# two lower and its Multiple ROF one lower, a Fire Lane included (A21.11, A21.12).
- **Rally (R13.2, R13.8).** Marsh and rubble give no rally terrain DRM; a German or Russian MMC has no unrecorded Self-Rally capability.

## 2. The packages

The Fire package is revised with its prior manifest digest kept (`42fa78bb...`, prior `584d82d9...`; matrix `921bba54...`): `FireWeapon.Captured`, rulings `capturedWeapons` and `dismantledWeapons`, the case `A1-fire-captured-mg`, and 21 new fragments (330 in all). The Rally package (`ba0278e0...`, prior `f3017213...`; matrix `043b6ccb...`) revises `dmSources`, `terrain`, and `capabilityUnrecorded`, and adds the cases `A1-rally-marsh-rubble` and `A1-rally-mmc-self-rally-none`.

## 3. The game model and records

- New records: `rout-stepped`, `rout-interdicted` (with its Interdictor), `deployment-attempted`, `rph-action-taken`, and `recovery-attempted`; the new condition `asl:dismantled`.
- `GameState` adds `RoutedThisPhase` (a HS Reduced from a routing squad included), `RallyPhaseActions`, `RecoveryAttempts`, and `RecoveredThisPhase`, all cleared at every phase change.
- The projector checks a rout's MF, the Interdiction and Deployment rolls, the RPh actions, and Recovery's dr and MF; dismantling in the PFPh is a SW use.
- The gate reads back routs, Deployments, Recoveries, RPh actions, and dismantling; the DM an action adds does not decide its readback.
- `LiveFire` marks a MG of another nationality than its possessor's as captured, and refuses a dismantled weapon.
- The board's LOS map and its reads are kept per board, and a rout's search keeps its step, entry, and Interdiction reads.

## 4. The Play page

- A Rout panel in the RtPh: who must or may rout and why, the cover each must reach or that it has no legal step, the route, and Low Crawl.
- A Support weapons panel: a SW, the recovering or receiving unit, and transfer, drop, recover, and dismantle or assemble.
- In the RPh: Deploy (squad, leader, the second HS's SW), Recombine, and DM retention for the eligible units.
- The records list, now "Rally, Rout, and support weapons": rout steps, Interdiction with its Interdictor, Deployment with the new HS, Recombining, Recovery, dismantling, and dropped SW.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass13Tests` (5); two older rally tests follow R13.8.
- Play: `BacklogPass13Tests` (17), on a board 01 grid with a clear LOS; older tests follow the new rules (the action list, a Russian Self-Rally, and a Melee test that now starts in Melee).
- MapStudio: `TheSupportWeaponPanelTransfersAWeaponAndTheRoutPanelNamesWhoMustRout`; the fire page test takes the surrender of a broken HS left ADJACENT to its enemy.
- Authoring: `ThePass13SubjectsAreVerified` verifies the 21 fragments; the matrix tests pin the new digests and fragment count.

## 6. Not in this pass

Section 23 of the backlog lists what this pass defers.
