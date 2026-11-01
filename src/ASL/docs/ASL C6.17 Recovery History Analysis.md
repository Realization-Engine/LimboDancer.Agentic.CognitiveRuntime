# C6.17 recovery history analysis

Status: approved 2026-11-01. The Rules-only stage is implemented and independently reviewed on `codex/c617-recovery-history`. Play and Units wiring remains pending the Rules PR's separately approved merge. The diagnosis below describes the pre-wiring application behavior.

Base: main `6cbf7df027e26dbb4696a14b080f109a26852521`, fetched before creating `codex/c617-recovery-history`. Worktree: `C:/Users/dland/.codex/worktrees/c617-recovery-history/LimboDancer.MCP`. Tool work folder: `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`.

## Problem and scope

This addresses the per-target ordnance allowance row in [backlog 94](<ASL Unit Backlog.md>). PR 96 preserved gun shots during recovery by the same current mover, but switching the recovery participant replaces the single current movement total and clears the single current gun-shot list. A different member's earlier expenditure and shots are lost.

The fix must preserve both MF and shots for each target. Merely restoring an MF total could grant extra shots if its previous shots were discarded. Replacing `Movers` with the original stack would expose non-spending units to Defensive First Fire and credit them with MF they did not spend.

This is an ordinary on-foot personnel recovery/accounting correction, including supported one-to-one target replacement, not completion of every C6.17 vehicle or Gun Duel provision. Existing vehicle Target Facing and MP-allocation limitations remain outside this task. Carried men, newly created additional units and unsupported lineage relationships need a separate accounting reading; the design explicitly preserves their existing reading rather than pretending their cumulative personal MF proves the new allowance. They are recorded in backlog 94. The separate allocation-measurement row there is not closed by designing this fix.

## Rulebook evidence

Read physical PDF pages in `eASLRB_v3_01.pdf`:

| Rule | PDF page | Consequence |
| --- | --- | --- |
| A4.2 | 49 | Units in an original moving stack can continue separately; its members finish before an unrelated unit starts. Membership and the current expenditure's participants are different facts. |
| A4.44 | 50 | Ordinary MPh recovery spends one MF, whether the attempt succeeds or fails. The friendly broken-holder SMC exception spends none. |
| A8.1, A8.11, A8.12 | 59 | Defensive First Fire concerns the current moving unit/stack, including MF spent in place. Past expenditures do not reopen a closed opportunity by themselves. |
| C6.17 | 173-174 | A weapon's shot ceiling concerns that target's expenditure in that Location, rounded down with a minimum of one. ROF and Intensive Fire do not override the ceiling. |
| A8.14 | 59 | Breaking or pinning does not remove a target from follow-up attacks during its current open window. `MovementEnded` alone must not erase its history. |
| A7.302, A19.13 | 55, 86 | Casualty reduction/replacement continues the target with a different counter. The implementation's new id must not create fresh MF or erase gun shots. The proposed transfer is limited to supported one-to-one successors. |
| A17.2, A15.21 | 85, 83 | Carrying and creation of an additional hero have different movement relationships. These pages do not justify treating zero personal MF or a copied counter total as a complete new target history. |

A whole-PDF literal cross-reference scan found C6.17 references on pages 173, 200, 639 and 648. Pages 200, 639 and 648 are vehicle examples, not authority for adding vehicle work to this scope. The continuation on page 174 was read separately. Vehicle Target Facing, MP allocation for Case J and Gun Duels need their own complete treatment; existing limitations are already recorded in the inventory/backlog.

## Reproducer and required result

Use two Good Order squads, A and B, moving together into [A3] for one MF each, with recoverable equipment there. A recovers one weapon, finishes its movement, then B recovers another. Close each intervening defensive-fire window normally. Assume no gun has yet fired and sufficient movement allowance. A does not resume after B starts its separate movement (A4.2).

| Event | Current `Movers` | A's MF in [A3] | B's MF in [A3] | Current scalar MF |
| --- | --- | --- | --- | --- |
| Joint entry | A, B | 1 | 1 | 1 |
| A's recovery | A | 2 | 1 | 2 |
| B's recovery | B | 2 | 2 | 1, incorrectly reset |

At B's window, a gun with appropriate ROF may have up to two shots against B, subject to all other fire rules. A is not a target merely because its history remains stored. Reusing the stack's accumulated total would instead give B three MF, also wrong.

A second case must include shots: a gun attacks the joint stack once, then attacks A after A's recovery. At B's recovery, that gun has already shot at B once, not zero or twice. A second gun has its own counts. Repeated recoveries by A before it ends its movement must retain its earlier shots.

These are source-derived reproducers, not claims of an executed demo or test. This analysis ran no build or tests.

## What decides it today

The `rule_sites.py C6.17 --words OrdnanceShotsHere,RecoveryMovement` scan was followed by reading the named sites and their callers. Method names are more durable than line numbers.

| Site | Role | Finding |
| --- | --- | --- |
| Rules `ScenarioA1OrdnanceCalculator.Outside` | Decides | Applies the C6.17 ceiling to scalar `SpentHere` and `ShotsHere`; the comparison is sensible only if those facts describe the target. |
| Rules `ScenarioA1OrdnanceEligibility.MphMovement` | Decides/translates | Rounds half-MF and creates scalar movement facts; also carries vehicle MP claimed. |
| Rules `ScenarioA1SupportWeaponPossessionRules.RecoveryMovement` | Decides | Adds MF and retains shots only when the recovering unit belongs to the current `Movers` at that Location. There is no independent history for another original member. |
| Rules `ScenarioA1SequenceProjection.ClearsShotsHere` | Decides | Clears the legacy list on movement-step/vehicle-entry boundaries. It has no target identity. |
| Units `GameProjector.AttemptRecovery` | Gathers/records | Supplies the same-mover comparison and applies the recovery verdict. Correctly narrows `Movers` while preserving `Members`. |
| Units `GameProjector.StepMovement`, `Apply`, `ChangePhase` | Gathers/records | Builds the movement window, clears the legacy list and resets phase state. These are transition integration points. |
| Units `RecordLineage`, `KeepMovingStack`, `CreateInPlay` | Gathers/records and existing succession behavior | Replacements acquire new ids and inherited counters; the moving stack's ids change. A new hero is additional, not a one-to-one replacement. Current history needs explicit Rules-owned succession and scope classification. |
| Units `GameProjector.Ordnance` | Currently decides/records | Increments `OrdnanceShotsHere` keyed only by gun. Current accounting must move to a Rules transition. |
| Units `GameState`, `MovementState`, `OrdnanceShotRecord` | Records | Store a single current Location/half-MF context and gun-only shot counts, without individual target context. |
| Play `LiveOrdnance.FromState`, `Panzerfaust` | Gathers | Both read the same scalar window and gun-only list. Both need the new fact reader. |
| Play ordnance verifier in `LiveOrdnance.cs` | Gathers/verifies | Rebuilds facts from state, then compares the recorded reading. A version switch is required here as well as in commands. |
| Play `GamePlanner.Ordnance` | Records | Writes `OrdnanceFired`; must explicitly select the new reading for supported new shots. |
| Authoring `AslScenarioA1FireSourceReview` | Source metadata | Registers the C6.17 rule and continuation pages; does not decide eligibility. |

`MovementState.HalfMfInLocation` and `OrdnanceShotsHere` must not simply change meaning: older recorded shots are verified against them. `ScenarioA1OrdnanceCalculator.Missing` currently requires scalar movement facts, so a new fact collection alone cannot activate the new reading: both `Missing` and `Outside` need explicit version dispatch. `ScenarioA1Wounds.MoverHalfMf` deliberately gives carried men zero personal MF, making them unsuitable for the proposed ordinary-target baseline without a separate rule decision. The existing `SupportWeaponScopeTests.RecoveryInPlaceKeepsGunShotsAndAddsOnlyItsMf` covers the same-mover transition, not alternating members. The two existing `ScenarioA1Pass8Tests` methods cover scalar ceilings and vehicle MP already claimed, not loss of target identity.

## Recommendation

Maintain a phase-scoped history per personnel target and full Location, with a separate shot count for each gun that has fired at that target there. Leaving and returning to a Location does not invent a fresh shot allowance: retain its earlier MF and shots. Keep attack-window eligibility separate. Record the new reading explicitly on ordnance events and facts; preserve the old path for events without it. Build the history from accepted events even during legacy replay so a continued old game can make a correct new proposal without rewriting its past.

Supported successor targets inherit and rebase the predecessor's history before its id is retired. Broken/pinned targets retain it through the open window. Known excluded relationships explicitly retain the old reading for the target set; incomplete ordinary history must instead report not-evaluated. These are separate cases, not permission to fall back when the new calculation fails.

The [implementation design](<Designs/ASL C6.17 Recovery History Design.md>) specifies the boundary, compatibility contract, checks and delivery sequence. C6.17's inventory row is currently open for claim despite its existing scalar calculator being marked built. No claim has been reserved during this analysis, and SW-01 is not reused for this new work.
