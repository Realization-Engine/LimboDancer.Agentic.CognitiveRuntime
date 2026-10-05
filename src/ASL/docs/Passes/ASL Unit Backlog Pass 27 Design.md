# ASL Unit Backlog Pass 27 Design

**Status:** Built. Pass 27 (Heat of Battle, Leader Creation, and berserk gaps) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 5, the fifth game pass.

**Date:** 2026-10-01

**Related documents:** the [Scenario A1 Backlog Pass 27 Review](<Scenario A1 Backlog Pass 27 Review 2026-10-01.md>), the [ASL Unit Backlog Pass 26 Design](<ASL Unit Backlog Pass 26 Design.md>) (vehicles on a card), the [ASL Unit Backlog Pass 17 Design](<ASL Unit Backlog Pass 17 Design.md>) (the last catalog bump), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 41.

Rulings R27.1 to R27.5 are in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

The recorded gaps in A15 close, and the Axis Minors play. The user answered the six questions on 2026-10-01: one Axis Minor counter set shared by the five nations, with the nation named per side; the counter values now and the other A25.8 rules in the backlog; catalog 1.13.0 with its re-pins; the berserk leader's companions' TCs shown beside the CC record, with no Heat of Battle in CC; a level-aware graph for the charge only; and one pass, C01 included.

- **Axis Minors (R27.1).** Vocabulary 1.16.0 adds the side `axis-minor`; catalog 1.13.0 adds 17 counters transcribed from the National Capabilities Chart (none manufactured). Heat of Battle +3, non-elite MMC surrender on 10 or more; Leader Creation with no drm, and an unreviewed nationality refused; A25.84's Replacement and Battle Hardening chains. An Axis Minor side names its nation, and Hungarians against Romanians start under No Quarter on both sides.
- **The charge's route (R27.2).** A forward search from the charging stack over Locations, stairwell levels, ADJACENT hexes at an upper level, and Bypass lanes keeps the first move of every shortest route. A charge enters an enemy vehicle's Location and attacks it in the CCPh's sequential CC.
- **Contact (R27.3).** A.9 Random Selection draws Dummies with the real units; the berserk OVR onto a lone SMC has its CC at once in the MPh; a CC record names a berserk leader's companions' TCs.
- **Owner's options (R27.4).** Spraying Fire's second Location, Encirclement, a Fire Lane, and a Thrown DC's attack on its thrower and removal resume after the answer; a DC asks its options.
- **Components (R27.5).** The 14 P1 roots of task 27.5, and `BerserkOverrunCloseCombat`.

## 2. Catalog and event format

| Item | Change |
|---|---|
| Vocabulary | `asl@1.16.0`: the side `axis-minor`. Palettes 1.1.0 color it. |
| Catalog | `asl-scenario-a1@1.13.0`: 17 Axis Minor counters. The four packages' case matrices and manifests, `LiveFire.CatalogVersion`, the cards, the synthetic game, and the tests are re-pinned. Games pinned to 1.12.0 keep their catalog and are not upgraded, as with earlier bumps. |
| `game-started` | A side's optional `nation` (Romanian, Hungarian, Slovakian, Croatian, Bulgarian), required for an Axis Minor side and refused for any other. A card side takes the same field. |
| `close-combat-resolved` | The facts' `infantryOverrun` marks the CC of a berserk OVR, which the projector accepts in the MPh once the DEFENDER's window on the entry has closed. |
| `choice-pending` | The fire resume carries `followUps`: the spray attack, the Encircling side, the Fire Lane, and a DC with its thrower's Location. |

## 3. Planner and page

`ChargeSteps` now searches forward (`ChargeEdges`, `ChargeFirstMoves`) and returns each step with its least half MF, whether it may be ordinary, and the Bypass lanes it may take; `BerserkStep` checks the `bypass` argument against them. `BerserkOverrunPending` names a Location whose OVR CC is due; the dispatcher refuses every action but `close-combat` and `choose` until it is resolved. `BerserkOwingVehicleAttack` keeps the CCPh open and refuses a side's pass while a berserk unit owes its vehicle attack. `AddFireFollowUps` is the one place the follow-ups of an attack are done, whether the attack resolved at once or after a choice.

The Play page shows the OVR's CC as a notice to every view in the MPh (`BerserkOverrunCloseCombat`), with the button for the view that may act for the phasing side; the attacks are the planner's (`OverrunAttacks`: the SMC attacks back whenever it can). The first CC record after a leader went berserk names the Berserk TCs taken with it, read from the fire and Rally records the viewer may see. The charge notice names each step's Bypass lanes (`ChargeLanes`).

The reviews' fixes are in the [review](<Scenario A1 Backlog Pass 27 Review 2026-10-01.md>): among them the 2-2-7's Fanaticism, the OVR given to a berserk MMC only, a gate that cannot stall the MPh, a charge barred from a vehicle's Location holding enemy Infantry, the Sniper checks after a resumed attack, and the custody and vehicle CC panels reading the view.

## 4. Tests

`ScenarioA1Pass27Tests` (4) checks the counters, Heat of Battle, Leader Creation, and the chains. `BacklogPass27Tests` (6) plays on the pass 10 test board: the Bypass route, the stairwell route, the A.9 draw, the OVR's CC in the MPh, a lone berserk leader that does not OVR, and Hungarians against Romanians. A DC test in `BacklogPass15Tests` asks Battle Hardening before the thrower's attack and removal; `VehicleStepsTests` now charges a tank. `CloseCombatComponentTests` (13) covers the components and the OVR panel. Re-pinned tests: about 45 files name the catalog version.
