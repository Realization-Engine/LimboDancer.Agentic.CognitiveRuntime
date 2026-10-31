# ASL Action Forms Implementation Record

Date: 2026-10-31. Branch: `codex/action-form-design`.

The user requires a Studio demo and a separate explicit approval before merge. The earlier permission to merge after CI was withdrawn. Nothing in this record authorizes a merge.

## Implementation inventory

| Surface | Treatment |
|---|---|
| Fire | Separate firing group, target, additional choices, and proposal regions. One page-owned target plus entry provenance replaces competing target values. Manual edits block submission until applied. Spray is explicitly added or removed. |
| Ordnance | Weapon/assistance and target/ammunition groups; existing alternate Canister aim retained. Restricted target picker accepts only supplied choices. |
| Rally, repair, deployment, recombination | Compact participant/equipment groups and separate proposal areas; no empty target groups. |
| Infantry movement | Existing unit selection and route planner retained. Manual step form separates destination, movement choices, and proposal. |
| Advance | Existing unit selection retained; restricted destination group and proposal area; map exit remains its own command. |
| Rout | Existing obligations, shared routes, and per-unit routes retained. Manual rout groups unit/assistance and route separately from its proposal. Voluntary break remains separate. |
| Close combat | Existing declaration sequence retained. Attack builder separates attackers and defenders. Withdrawal, infiltration, stacking, and berserk-overrun controls use consistent field alignment within their existing command groups. |
| Vehicle movement, crew, scrounging | Existing contextual command groups and vehicle-specific choices retained with aligned labels and bounded fields. |
| Crew exposure, DM retention, gun arc, mopping up, opportunity fire, placed-charge detonation, prisoners, reaction fire, shock recovery, spotter designation, starshells, support weapons, thrown charges | Existing small contextual groups retain their command boundaries and gain consistent field alignment. No new rule decisions. |
| Movement route and plan, pending choices/surrender, bounding fire, smoke, placed-charge entry, vehicle step/towing, close-combat resolution | Existing specialized command/route/obligation structures remain, using shared action-section help and layout where hosted inside the section. Not converted into one universal submit form. |
| Inline Play actions | Existing action-section boundaries remain. Phase-end placement, confirmation, handover, and proposal review remain unchanged. |
| Setup, replay, outcomes, scripted dice, record navigation | Outside the action-entry redesign; no layout migration. |

## Shared behavior

`ActionFormGroup` and `ActionProposalArea` contain presentation only. `ActionLocationChoice` shares the target across selection, typing, and map entry. It supports a free-entry policy and a restricted-choice policy. Restricted picking clears the submitted destination while a new selection is pending, so the previous destination cannot be submitted while a level choice is unresolved. Cancelling an armed restricted picker restores the previous choice if it is still offered.

`LocationField` retains route semantics and supports optional syntax/edit status. Only the new free-target editor opts into that status contract. Existing rule verdicts and event formats remain unchanged. Picker cancellation on section close is scoped to the editor in that section. Leaving the Play inspector tab cancels the current picker. The existing separate route planner is not replaced by this single-location picker.

Long rule summaries move inside their local disclosure when hosted in an action section. Content and IDs remain available. Scoped CSS does not change the global toolbar.

## Verification record

- First and refined Studio builds passed with zero warnings and zero errors, using locked restore and external Codex outputs.
- Studio Fire checked in the existing in-app browser, then in a separate `p72-ammo-shortage-mmg` tab once the user's Tractor Works game advanced to Rout. No test action was proposed or committed in either game.
- Visually checked the groups, proposal feedback, and prominent map button. Verified malformed input remains pending, list selection replaces it, leaving the inspector tab cancels picking, and adding/removing spraying fire resets that option.
- Six initial focused methods passed: three draft/picker regression checks, the existing armed-location check, the Fire component check, and the page's opportunity/spraying-fire check.
- Restricted multi-level destination selection received an additional regression check. Final build/check results are recorded below after completion.

Final results: the final Studio build passed with zero warnings/errors; all seven focused methods passed. The initial restricted-picker failure was an unsupported bUnit test rerender operation, corrected by providing the cascade at initial render. Studio also verified a populated Fire draft becomes disabled during invalid editing and is enabled again after explicit target replacement. Restricted ordnance picking clears the submitted destination while armed and restores the previous offered location on cancellation. The Fire layout was visually checked at the normal viewport and a 900px viewport; the temporary override was reset. No game action was submitted.

Active demo binary: `action-form-design/studio-final`, process 63132 when started. The separate in-app demo tab uses `p72-ammo-shortage-mmg`. The user's ongoing Tractor Works game was not advanced by these checks.

Limitations: this is not a claim of a visual audit of every phase, conditional option, board, or viewport. Rare contextual forms retain their existing action organization with shared alignment improvements. The dedicated route planner retains its existing lifecycle and is separate from `LocationPicking`. Further changes found necessary during the user's demo remain on this branch.

## Movement review follow-up, 2026-10-31

The user approved the map-to-mover workflow after the Prep Fire review. An inspected hex now offers its eligible mover locations, with each board and level distinct. Opening a location exposes explicit unit checkboxes and does not select or submit automatically. The normal unit list remains available. Destination entry is visible, with additional movement choices in a disclosure. Empty infantry lists omit idle movement controls while active route status and continuation remain available.

Field picking and route plotting exclude each other. Escape on the map, inspector changes, and closing the Movement action cancel the applicable input mode. The Unit Planner retains its existing stop-and-save behavior for queued routes. Existing eligibility and command logic are unchanged.

Studio evidence: `build-codex-studio.log` reports zero warnings/errors for `action-form-design/studio-map-movement`. In `ui-movement-demo-01`, checked origin selection, both directions of picker/plot switching, a plotted [B2] step leaving the manual destination empty, destination picking ending plotting and filling only that field, Escape, and inspector changes. No movement was committed in these checks. The demo was copied from the setup prefix of `p72-ammo-shortage-replacement`, then advanced normally through the phase proposal and confirmation. Source games and Tractor Works were not modified by this setup.

The active demo is `http://localhost:6680/games/play?game=ui-movement-demo-01`, using `action-form-design/studio-map-movement` (process 18412 when started). Both focused methods passed: `MovementComponentTests.MapOriginsKeepBoardsLevelsAndUnitSelectionDistinct` and `PlayPagePass31cTests.ARouteIsPlottedOnTheMapAndMadeStepByStep`. Evidence: `action-form-design/movement-workflow-checks.log` and `action-form-design/results/movement-workflow.trx`. The initial test build stopped on dictionary formatting in the new test; the scoped formatter corrected it before rerunning the same two methods. No broader test suite ran.

The user wants to resume the phase-by-phase workflow review after this correction. Defensive Fire is next. When the review reaches Rout, apply the same map-to-unit selection, clear destination/route entry, and single map-input ownership while preserving Rout's required decisions. Rout has not yet received that follow-up implementation. No merge is authorized.

## Current outputs

`ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`. Task builds and checks are registered under `action-form-design`. The active demo stays on port 6680 for review; other Studio ports are untouched. Keep logs/results as verification evidence, and clean superseded task-owned binaries after the active demo build is established.
