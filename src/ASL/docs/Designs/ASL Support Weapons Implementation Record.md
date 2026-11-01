# Support Weapons implementation record

Status: historical checkpoint, superseded by [Support Weapons Studio Demo](<ASL Support Weapons Studio Demo.md>). The sections below describe the earlier partial implementation, not the current code. The user approved all phase demos, then paused merging. PR 96 must not merge or enable auto-merge without a fresh instruction. Current release evidence belongs in the demo document.

## Authorization and checkout

The user approved implementing Rules and callers together on this branch before merge. The exception is recorded in the proposed design. Worktree: `C:/Users/dland/.codex/worktrees/support-weapons-implementation/LimboDancer.MCP`; branch `codex/support-weapons-implementation`.

Document-only claim commit `c2d03ac3` was pushed to main as the required claim lock. No application implementation has been merged. Claims A4.43 (including A4.431/.44), A9.72, A9.8 and A11.21 remain claimed, not built/reviewed. Their Rules file holds are in the inventory.

## Implemented so far

- Standalone Drop delegates to `ScenarioA1SupportWeaponPossessionRules.DropRefusal`. Its offers use that same check, eliminating the generic CC drop route and its enemy-presence check. Rout and CC already have separate integrated equipment paths; neither calls `PlanDrop`.
- The initial review incorrectly widened the APh drop window. PR 96 restores the established before-advance restriction through the shared Rules verdict and an Advance fact. This supersedes that checkpoint's conclusion.
- Added `SupportWeaponWorkflow.razor` and connected Drop. It supports map scope, release, participant inputs when needed, selected-action refusal, context resets and draft-change notification. The other operations still use the old UI.
- Added initial recovery, repair and configuration fact models/calculators. Recovery, repair and configuration are NOT wired. Repair/configuration use structured readiness to distinguish missing support from refusal.
- Added an eight-phase demo generator, `code-builders/pass-history/action-forms/prepare_support_weapons_demo.py`. It refuses overwrites. Generated `ui-sw-review-01-{rph,pfph,mph,dfph,afph,rtph,aph,ccph}` in the shared live-game folder. These are development fixtures, not accepted full-demo evidence.

## Studio evidence

Isolated output: `E:/Archive/GitHub/dlandi/asl-gate-work-codex/support-weapons/studio-6680`, registered with the output registry. Studio process 82992 started on port 6680 using this worktree. Verify the process identity before stopping it.

`E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log` recorded a successful strict build with zero warnings/errors and elapsed 1:01.55 after connecting Drop. Earlier build failures were CA1822 (fixed by making the helper static), formatting, and missing braces (fixed). No unit tests have run.

Browser verification in `ui-sw-review-01-mph`:

1. German handover opened the map and action pane successfully.
2. Selecting the squad in [F5] scoped Drop to the three eligible held weapons there; no recipient input appeared.
3. Selecting its MMG enabled Propose. The command produced a confirmation proposal naming that squad, weapon and [F5]. No action was committed.
4. Selecting the eligible receiver in [J5] switched scope, cleared the weapon selection and removed the stale proposal.
5. The status banner retained its old confirmation text after the proposal cleared. Source now clears `status` alongside `pending`; this last small fix needs a rebuild/browser recheck.

Temporary browser tab 16, bound as `swTab`, is marked for handoff. It is not a completed deliverable. The user's original game has not been altered.

## Required next work

1. Finish the Rules contracts, applicability and fact gathering. Add transfer eligibility, recovery phase-start/current-mover opportunities, supported terrain/carriage relationships, and the remaining withdrawal facts. Do not wire the incomplete recovery evaluator merely to enable held recovery.
2. Consolidate weapon capacity in Rules. Review found an inline `squad ? 2 : 1` in `ScenarioA1FireCalculator.cs:841`, but no complete public generic helper. Add the necessary claim holds before editing existing fire files. Repair/configuration must consume the same authoritative result, not duplicate arithmetic in Play.
3. Derive per-weapon repair attempts and complete per-holder phase-use histories. `SupportWeaponUses` is NOT a cumulative ledger: the second weapon can remove a squad's entry. Existing configuration projection records use only in PFPh. Handle DFPh and night E1.89, coordinating W-E1.8. Preserve recorded readings and name sweep differences.
4. Wire the selected-action readiness APIs into both commands and UI. Replace the old Support Weapons panel, separate Recover/Transfer/Repair, and put configuration beneath owning units. Remove obsolete German-MMG-only help.
5. Integrate source scope with parent Movement/Advance workflows. The new component's standalone map scope is not sufficient to enforce parent acting-unit scope. Finish contextual withdrawal handling and keep Rout's required best-load behavior.
6. Add the required wiring contracts/minigame checks and narrow the claim checker exception to the user-approved combined branch, retaining normal gate/CI requirements. Claims remain unreviewed.
7. Complete Studio checks across all phases, including both eligible hexes, actor changes, scope release, confirmation, stale-preview invalidation, owner/opponent visibility, repair capacity and repeated attempts, broken-holder recovery, configuration without a fire target, and no generic CC/Rout drops. Tune the fixtures against actual legal route/withdrawal destinations rather than assuming their initial positions suffice.
8. Run only necessary focused assertions after the Studio check, then the required compatibility checks. Prepare a new untouched demo prefix for the user's review. Do not call the full demo ready while the old workflows remain.

## Independent review

Read-only reviewer `/root/sw_rules_review` found the APh restriction and the incomplete recovery timing/relationship facts. The zero-MF exception now derives from held/friendly/broken/SMC facts within Rules, so an unheld weapon cannot claim it through an inconsistent precomputed flag. The inherited unarmed-recovery prohibition still needs its supporting ruling audited.

The reviewer also identified capacity/accounting gaps, Gun repair limitations, conflation of implementation support and legality, and component draft/reset issues. Structured readiness, explicit Gun unsupported handling, separate configuration type/support inputs, context reset and draft callbacks were added. These changes have not received final independent acceptance. Scope integration and the full Rules/caller contract remain outstanding.
