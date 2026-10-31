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

## Close Combat declaration workflow, 2026-10-31

Implemented the approved location, attack building, declaration and review sequence. The first side hands over its declared attacks; the second explicitly declares attacks or no attacks, then reads both sides before proposing resolution. Unfinished checkbox/capture selections block handover and review; Clear selection is separate from clearing the round. Added attacks can be edited or removed, and review can return to editing. Existing view restrictions, first-side storage, sequential-round decisions, capture, stacking, withdrawals and infiltration remain in their existing paths. No rule calculator, plan result, event or dice handling changed.

Attackers and targets use aligned groups and one unit per row. Hide Director when no director is offered, Ambush when it is not due, and exhausted attack groups. Capture, round options and infiltration are disclosures. Empty and resolved locations omit idle attack forms. Map inspection automatically selects a uniquely offered location in that board and hex; multiple levels require a choice. Release and a new map selection protect drafts with an explicit clear-or-keep decision. Review state resets with location, round, step, game, view and revision.

Studio verification used `ui-cc-demo-02`, copied exclusively from the unresolved recorded position in demo 01. Confirmed: unfinished first-side selections disable handover; release protects the draft; Edit restores the attack and allows the second squad to be added; first-side declaration hands over without resolving; Germany sees the Russian attack; an explicit no-attacks declaration reaches review and can be edited; unfinished German selections block review; the completed combined proposal includes both attacks and passes every check, awaiting confirmation. No CC was committed in the check game. The first visual check found empty exhausted selection groups; the final build hides them.

`ui-cc-demo-03` was created separately at the start of CC for the clean review. Verified direct map selection of [A2], Release location, re-clicking the same hex, no automatic unit selection, exhausted-choice hiding and resetting the draft. It was opened clean for the user, who subsequently continued staging an attack. Leave that user interaction intact. Original demo 01 and the earlier phase games remain untouched.

The final Studio build at `action-form-design/studio-cc-final` reports zero warnings/errors in 1:41.19 in `E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log`. Studio process 47988 serves port 6680. The first strict test compile stopped on CA1861 in the new test fragment; hoisting the constant locations outside the repeated render fixes it. This cost one compile retry, with no failed runtime check. The focused retry log `action-form-design/cc-workflow-checks.log` and `results/cc-workflow.trx` report nine passed, zero failed, in nine seconds: two new declaration/map-transition regressions, two affected component checks, four existing simultaneous/capture/withdrawal page checks, and the icon-coverage source check. No full suite or broad viewport sweep ran.

Self-review covered staged versus unfinished attacks, explicit no-attack declarations, side handover, optional choice preservation, board/level distinction, map-input ownership, reset lifetime, empty states and bounded presentation state. The user's Support Weapons question exposed an existing rule/offer issue outside this presentation change: start-of-CC dropping is offered without withdrawal intent. A4.43 and A11.21 were checked on PDF pages 50 and 73. Analysis and backlog section 92 record the correction and the accompanying generic-control clutter; no new rule was added to the UI. The first text extraction hit the terminal's legacy encoding; using UTF-8 output completed the read. No merge is authorized.

## Rally map scoping, 2026-10-31

The user's report authorized the missing map-scoping correction. Rally now filters the existing viewer-scoped candidates by the inspected board and hex, retains level-qualified labels, switches on another candidate hex, and releases on an unrelated hex or "Show all rally units". Switching to a scope that excludes the selected unit clears the unit and leader together. Component identity resets scope with game, viewer, turn and phase; foreign picker clicks are ignored. No rule eligibility, assistance defaults, Deploy/Recombine behavior or Support Weapons behavior changed.

Studio verified in `ui-rally-demo-02`: [F5] shows only its squad; an assisted Rally preview passed every check and was cancelled; [D10] replaces the scope and clears the previous squad and leader; explicit release restores all three German candidates; clicking [D10] again restores scope; [J5], with no Rally candidates, releases it. The verification committed no Rally attempt. The user continued playing and opened Replay during documentation; that view was preserved, with the final scope capture taken in a separate tab. This check does not claim full Rally rules or special-case coverage. The remaining Rally review proposals await approval, and Support Weapons is deferred across all phases.

The initial review fixture used an unrecognized DM condition name. The helper now writes the vocabulary's `asl:dm`; the corrected, separately named demo replayed successfully. The failed unused fixture was removed, without modifying existing review games. `prepare_rally_demo.py` refuses overwrites.

`E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log` reports the strict Studio build passed with zero warnings/errors in 2:02.75. The replacement server is PID 54684 on port 6680, using `action-form-design/studio-rally-scope`. Superseded task binaries, including `studio-cc-final`, were removed after the Studio checks. No automated test suite was run for this presentation filter. The implementation self-review covered repeated picks, scope invalidation, viewer boundaries, stale selections and bounded component state. No merge is authorized.

## Rally assistance and separate organization workflows, 2026-10-31

Implemented the approved follow-up. Rally queries the existing planner only for the selected unit's methods, memoized by immutable state, viewer and unit. It offers only accepted methods and selects a sole method. Self-Rally remains the default when offered; otherwise multiple leaders require a choice. Assistance is hidden before selection or when no method qualifies. Both readiness and proposal handling reject an unavailable method. Refusal display now prefers a concrete Rules explanation over the generic package headline, with shorter Self-Rally sentences. No rule verdict, event, roll or confirmation semantics changed.

Deploy and Recombine have independent sections and progressively reveal their dependent choices. Both use the shared presentation-only `ActionUnitScope` wrapper for board/hex inspection, release and dependent-selection clearing. Existing candidate lists, planners, weapon distribution, Spotter and optional-leader behavior remain authoritative. Support Weapons is still a separate task. Self-review covered scope ownership, reset lifetime, stale selections, viewer boundaries, optional methods and bounded work.

Studio verification used the exclusively created `ui-rally-demo-03`, preserving the user's game 02. German [F5] offers only its eligible leader and passes an assisted Rally preview; [F6] offers Self-Rally. Russian [P5] offers its leader, while isolated [R5] has no method and explains the nonphasing-side restriction. An already-attempted unit in the user's existing game displays the once-per-Player-Turn refusal. Deploy and Recombine previews at [J5] pass, with dependent controls appearing in sequence and clearing with their parent selection. Recombine map scope and explicit release were checked. Every preview was cancelled; no action was committed. The final demo is back on German [F5], with the latest outcome collapsed. This does not claim a broad viewport sweep, a live multiple-leader fixture or exceptional Rally rules coverage.

The final strict Studio build, `action-form-design/studio-rally-final`, passed with zero warnings/errors in 1:31.87; its launch PID is 61420 on port 6680. Evidence is `action-form-design/studio-rally-final-build.log`. The eight focused UI/source checks initially passed seven; the remaining test expected the old empty Deploy control after its last candidate deployed. Its assertion now expects the approved hidden section, and the single-method retry passed. The existing planner refusal check also passed. Evidence: `rally-workflow-checks.log`, `rally-workflow-single-checks.log`, `rally-refusal-checks.log`, and their three TRX files under `action-form-design/results`. All nine distinct selected methods passed, with no full suite. Superseded Rally builds and test binaries were removed after verification; logs, results and the active demo remain. No merge is authorized.

## Approval and integration with main, 2026-10-31

The user approved committing, pushing and merging after green CI. Integrated main at `51964b62`, retaining its Minigames changes alongside the action workflows. The conflicts were independent appended styles and document entries; the repository's document merger retained both sets and renumbered this branch's backlog section to 92, with references updated. The merged Play page retains main's scoped minigame lists and this branch's action controls.

The one affected icon-inventory check passed after a strict build of the merged Studio test project. The current `rally-workflow-single-checks.log` and `results/rally-workflow-single.trx` now hold that integration check. The earlier successful deployment retry report was preserved as `results/rally-deploy-single.trx` before replacement. Full CI is the remaining merge condition.

### Retained outputs

`ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`. Task builds and checks are registered under `action-form-design`. The active demo stays on port 6680 for review; other Studio ports are untouched. Keep logs/results as verification evidence, and clean superseded task-owned binaries after the active demo build is established.
