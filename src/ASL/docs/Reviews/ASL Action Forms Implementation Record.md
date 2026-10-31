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

## Defensive Fire verification follow-up, 2026-10-31

Advanced only `ui-movement-demo-01` to Defensive Fire and handed the view to German. Checked source selection, target selection from the inspected hex, explicit target picking, replacement of an unfinished manual target edit, optional spraying-fire controls, and Escape cancellation. The first proposal from German [A2] to Russian [A3] was refused with a nondefinitive LOS result.

The external Codex Studio output could not discover the repository's oracle fixtures by searching its parent directories. `start_codex_studio.ps1` now explicitly supplies the worktree's oracle-fixture directory and refuses startup if that directory is missing. Verification criteria, board data, game rules, and application binaries are unchanged. This is a startup-only change with no new cache, state scan, or rendering work.

Restarted the existing `action-form-design/studio-map-movement` binary on port 6680 (process 75704 when started). Retried the same unit, source, target, and phase in the Studio: the proposal reports every check passed, range 1, same level, clear LOS, Hindrance DRM 0, and open ground. The proposal remains awaiting confirmation; no fire was committed. This isolates the earlier refusal to verification-data discovery rather than the edge hexes. No rebuild or unit tests were needed for this launcher change. No merge is authorized.

## Advancing Fire review, 2026-10-31

Advanced only `ui-movement-demo-01` from Defensive Fire through the normal phase-end confirmation, then handed the view to Russian. Selected [A3] through the map's inspected-location action, selected squad R1 and its LMG, and picked German [A2] on the map. The attack preview passed every check and displayed separate squad and LMG contributions with the Advancing Fire factor. The MG-only selection produced a passing preview containing only the LMG contribution. Invalid manual target input disabled proposal; selecting [A2] again cleared the invalid edit and restored readiness. No attack was committed. No build or automated tests ran for this browser review.

Two presentation findings were identified: the long MG-only checkbox wrapped together with Director, making the firing-group layout harder to scan; and the MG-only preview's main sentence said the squad fired, although its contribution list and facts table identified the LMG alone. The user approved both corrections; their implementation is recorded below.

## Fire-presentation corrections, 2026-10-31

From and Director now occupy one row above the unit choices. Each unit contains its weapons and the short `MG only` choice. Visible weapon labels omit the repeated holder name; accessible labels retain the holder and location. Shorter readiness messages name only the missing step, and a valid target no longer receives a redundant summary below the form. Removed the help paragraph about internal processing and shortened other repeated labels. Existing weapon restrictions and callbacks remain unchanged.

The preview now names weapon-only contributions from the gathered attack facts. Studio verified the disabled MG-only choice before selecting the LMG, its enabled state afterwards, the nested hierarchy, and a passing preview saying `The LMG of 4-4-7 squad R1 fires at [A2]`. No attack was committed. The first visual check caught an unloaded isolated stylesheet; the final styles are scoped by fire-form classes in the page's existing `site.css`.

The final `build-codex-studio.log` reports zero warnings/errors for `action-form-design/studio-fire`. The demo uses process 47568 when started, on port 6680, and remains in Russian Advancing Fire. No merge is authorized.

All four affected existing regression methods passed: the fire panel's selection/phase check, the FT-alone label and proposal restriction, the malfunctioned-weapon label and disabled state, and the weapon offered after retained rate of fire. Evidence: `action-form-design/fire-presentation-checks.log` and `action-form-design/results/fire-presentation.trx` (4 passed, 0 failed). No broader suite ran. Continue shortening low-value text as the remaining phases are reviewed.

## Rout workflow and hex scope, 2026-10-31

The user requested the same map-selection approach for Rout, then refined it to automatic hex scoping with explicit release and release on a non-routable hex. Inspecting an eligible hex now filters obligations, shared and individual route suggestions, and the unit dropdown to that board and hex. Levels keep their existing labels. No unit is selected automatically. `Show all rout units` releases the filter; a fresh inspection switches or releases it. Route-picking clicks append steps without changing the origin scope. Switching to another origin clears an excluded unit's draft. The scope also releases when it has no remaining eligible routers, and resets with the game, view, turn or phase.

Removed the idle route form when no unit is eligible. Disabled route entry until a unit is chosen, shortened the readiness and route-entry instructions, and retained detailed rules in the existing disclosure. Selecting a unit or a suggested route stops its route picker, including a suggestion for the same unit. A review caught the picker cascade's missing name; corrected and verified in Studio. The existing planner, rout obligations, passenger actions, retained-equipment choices, group sequencing, and confirmation path remain authoritative.

Advanced `ui-movement-demo-01` normally to Rout. Created separate Rout demos from its setup/phase prefix, without overwriting any game. The multi-hex position places Russian R1 in [A3], R2 in [B2], and the German squad in [A1]; both Russian squads broke through the normal Voluntary Break proposal and confirmation. User review continued in `ui-rout-demo-02`; a fresh `ui-rout-demo-03` supplied the final checks and remains available with both squads eligible.

Studio verified: [A3] shows only R1 and its advice/actions; [B2] shows only R2 and its advice/actions; explicit release restores both; re-clicking the same hex restores scope; clicking an empty map hex releases it. Picking [B2] as a route step while scoped to [A3] retains [A3] and appends [B2]. Cancelling picking and inspecting [B2] switches scope and clears the previous route and Low Crawl choice. A suggested route for the same unit cancels picking. R1's preview to [A4] passed all checks and remains uncommitted in demo 03. The empty state was checked before the voluntary breaks. These checks do not claim a full Rout rules audit or a full viewport sweep.

The user's LMG question was checked against A4.42 and A10.4 on PDF pages 50 and 66, the demo catalog and the existing rout planner. This LMG is 1 PP, within the squad's 3-PP IPC; retaining it is valid. Overload choices and abandonment remain the existing planner's responsibility.

The final `build-codex-studio.log` reports zero warnings/errors in 1:11 for `action-form-design/studio-rout`. The active Studio process was 26900 at launch on port 6680. The launcher now waits for its own process to listen before reporting success, preventing the startup race encountered during browser reload. No merge is authorized.

Focused checks: the existing retained-load/suggested-route/preview test passed in `action-form-design/results/rout-workflow.trx`. The new board/level, scope-transition and picker-ownership regression passed in `action-form-design/results/rout-workflow-single.trx` (1 passed, 0 failed), with its log at `action-form-design/rout-workflow-single-checks.log`. Two earlier scope-test attempts failed because bUnit cannot replace a cascade directly through Render, then because its CascadingValue host requires ChildContent to be supplied again. The test now uses a stable child fragment in a real cascade host. The final retry ran only that method; no full suite ran.

## Advance workflow and clean demo, 2026-10-31

Advance now scopes the offered units to an inspected board and hex, without checking them automatically. All levels retain their labels. `Show all advance units`, another eligible origin and an empty or ineligible hex provide the same scope controls as Rout. Changing to a scope that excludes the selected units clears their destination and exit draft. Destination and exit controls appear after explicit unit selection; map exit is in a disclosure. Existing off-board candidates and entry destinations remain available when unscoped. Visible help and readiness text are shorter; detailed rules stay in the disclosure. A4.7 was read on rulebook PDF page 52; no rule or planner behavior changed.

The first Studio check found that a successful restricted destination pick disarmed before the origin component received its updated location cascade. The same click was then treated as inspection and replaced the origin. The page now records `LocationPicking.PickedBy` at click time, and Advance preserves that ownership after disarming. The regression reproduces the separate picker and location updates, rather than assuming their render order. Location facts reuse the already rendered viewer-scoped unit rows, avoiding an additional full candidate scan. No new persistent cache, map traversal or counter-layer change.

Studio checks in `ui-advance-demo-01`: explicit selection in [A3]; map destination [B2] retains [A3] and R1; unavailable [A1] gives feedback and no destination; cancel restores the prior destination; inspecting [B2] clears the previous selection; release, re-clicking the same hex and empty-hex release work. R1 advanced to [A4] and R2 to [A3] through passing previews and normal confirmations. Completed units disappear from the choices, an exhausted scope releases, and no eligible units leaves a concise empty state. This was a UI workflow check, not an Advance rules audit or full viewport sweep.

`ui-advance-demo-02` is a separate fresh two-hex demo, made with the existing exclusive-create setup-copy tool and advanced normally to the Advance phase. It has eligible Russian squads in [A3] and [B2], no advance committed by the agent and no pending proposal. The user asked for a clean demo during final checks; its tab was shown and its draft reset, then the browser was left to the user. The earlier Rout games are preserved.

The final `build-codex-studio.log` reports zero warnings/errors in 1:51 for `action-form-design/studio-advance-final`. Studio process 81720 listens on port 6680. Only Codex's previous demo processes were replaced. A browser connection negotiation failed after restart; a reload restored it, without application changes.

`action-form-design/advance-workflow-checks.log` and `action-form-design/results/advance-workflow.trx` report 3 passed, 0 failed: the new click-ownership/scope regression, the affected off-board presentation check, and the required icon-coverage source check. The generated icon coverage also catches up this branch's earlier action-form layout changes. No full suite ran. Code and design were self-reviewed for scope transitions, board/level distinction, disclosure, reset behavior and bounded work. No merge is authorized.

Cleanup initially refused the retired `studio-advance` because its process check also matched the active `studio-advance-final` prefix. The check now requires a directory separator after the full target path; the active-demo protection remains in place. This cost one cleanup retry, with no application rebuild or repeated tests.

## Current outputs

`ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`. Task builds and checks are registered under `action-form-design`. The active demo stays on port 6680 for review; other Studio ports are untouched. Keep logs/results as verification evidence, and clean superseded task-owned binaries after the active demo build is established.
