# ASL Play Action Interface Implementation Design

Status: approved for implementation. The user subsequently withdrew merge authorization on 2026-10-31 and requires a Studio demo before explicitly approving any merge to main.

Date: 2026-10-31. Baseline: `96fcf297`.

Basis: [Play Action Interface Analysis](<../ASL Play Action Interface Analysis.md>). This design organizes existing action controls and corrects ambiguous location-entry state. It does not change ASL decisions, event contracts, replay, or action confirmation policy.

## 1. Intended result

Each action has an intelligible reading order: participants, location if applicable, additional choices, and proposal. Related fields flow within groups; groups stack within a narrow inspector. The player sees one effective target, a clearly associated map-picking button, active optional choices, and feedback explaining missing inputs. Short actions remain short. Multi-stage actions retain their separate commands.

No new wizard, global toolbar restyling, custom combobox, rule evaluator, or replacement proposal-review system is needed. Existing phase guidance remains available in its tab. Compact inline instruction and an expandable Rules explanation remain with each action.

## 2. Presentation structure

Add these presentation components under `LimboDancer.Domains.Asl.MapStudio/Components/Play/`:

| Component | Contract |
|---|---|
| `ActionFormGroup.razor` | Stable `Id`, `Legend`, optional help fragment, and child content. Renders a semantic fieldset with a visually light legend. No eligibility or draft state. |
| `ActionProposalArea.razor` | Optional draft-summary and feedback fragments plus a child slot for existing buttons. Does not calculate readiness, submit a form, or own confirmation. Multiple commands remain possible. |
| `ActionLocationChoice.razor` | A single-location editor with host-owned value, choices, disabled-choice information, free-entry policy, purpose label, and change callback. Owns entry mode and transient text only. Details in section 3. |

Use scoped `.action-form`, `.action-form-group`, `.action-fields`, `.action-field`, and `.action-proposal` styles in `wwwroot/site.css`. Do not change `.toolbar` globally. Fields have `min-width: 0`, bounded controls, wrapping labels, and visible focus. Use wrapping grid/flex within each group, with a full-width proposal area in normal flow. Do not introduce a sticky footer that reduces map or inspector space.

Retain existing control IDs wherever controls survive. Rename or remove an ID only after finding its tests and consumers. Preserve native labels, tab order, and existing unit names with their locations. Group fieldsets must not be nested inside labels. Hide empty groups completely.

## 3. One-value location contract

### 3.1 Policies and controls

The host supplies the effective `Value`, `ValueChanged`, existing location choices, a disabled/reason projection for choices, and whether arbitrary entry is currently allowed. Use the host's actual types or a small presentation adapter; do not add a new domain model. Choice order and viewer disclosure remain host-owned.

For a restricted-choice field, render a native select and a map-picking action that can select only an enabled supplied choice. Do not show free text. If a picked hex has several enabled levels, offer those levels explicitly instead of choosing the first. If none match, retain the current selection and show a local explanation. Disabled options remain disabled through every entry method.

While a new pick is awaiting a level choice, mark that editor pending and block its action proposal. Retaining the previous value internally must not allow it to be submitted while the interface is asking the player to finish the new selection. Explicit cancellation restores the previous selection and clears this pending state.

For a free-entry field, render a suggested-location select, an explicit Enter another location mode, and a prominent Pick target on map button inside the same group. The modes share one effective value. A free-entered location outside the suggestions remains visibly represented as the current location. Selecting a suggestion replaces it and exits manual editing. Entering manual mode alone preserves the visible current value. Once the user edits the text, that new text owns the draft and prevents submission of the previous location.

Do not convert disabled suggestions into a new blacklist for arbitrary entry. Keep those suggestions disabled in the select and suggested Use actions, but retain the existing free-entry command path and its normal proposal checks. The restricted-field policy does not apply to that path.

Use `DisplayText.ReadPlace` and `BoardLocation` parsing for syntax, not legality. Preserve supported location formats, board names, and level handling. For ambiguous multi-board text, ask for board qualification in the field's feedback. Do not infer a board. A syntactically valid location remains subject to the normal action proposal checks.

Do not turn `LocationField.Many` routes into this single-location component. Keep route ordering, repeated steps, Remove last, and Clear route. Improve their labels and grouping through the existing `LocationField`.

### 3.2 Draft state and transitions

`Play.razor` remains the authoritative owner of submitted action drafts. The component may retain `EntryMode`, `RawText`, and an editing/parse status, scoped to its stable field ID and current action context. It reports an editing/invalid status to the host so proposal readiness can include it. A pending edit must never leave an enabled proposal that submits the previous location.

| Event | Required result |
|---|---|
| Select a location | Replace effective value, discard obsolete raw text/error, run existing dependent-choice invalidation |
| Start typing | Mark the edit pending; prevent stale-value submission immediately, before blur |
| Apply text on change or Enter | Normalize; commit valid syntax, or keep raw text visible and mark invalid. No hidden fallback |
| Pick a map location | Replace draft using the configured policy; clear stale text/error after an accepted pick |
| Clear action | Clear effective value and local draft/picking state through an explicit reset token |
| Remove optional location | Clear its submitted values, reset its editor, cancel its picking ownership |
| Host resets context | Reset local state using a context/reset token even when the effective string happens to be unchanged |

Do not rely solely on comparing the incoming `Value`: a reset to the same value must still discard an unfinished edit. Programmatic updates and accepted user updates must be distinguishable enough that a rerender does not erase in-progress text.

Include edit status in the proposal handler's entry guard as well as the button's disabled state. Apply the pending status before any awaited operation. Enter followed by blur must not send the same value change twice. This protects against event ordering and keyboard submission, not only the appearance of a disabled button.

### 3.3 Fire migration

Replace `fireTarget`/`fireFreeTarget` precedence with one authoritative target draft and one target-change path. Remove the redundant effective-target fallback only after updating all reads, callbacks, clear/reset/prune paths, vehicle-choice derivation, range inputs, and `ProposeFire`. Preserve their existing invalidation behavior. A valid free target must not be dropped merely because it is absent from the suggested-target list; an invalidated list choice must follow the existing context policy.

Retain host-owned provenance alongside the single value, Suggested or Free, for this pruning distinction. It is metadata, not a second target. Selecting a suggestion sets Suggested; typing or a free-policy map pick sets Free. Clear/context reset clears both. Confirm the migration against every current `PruneFire` call and preserve dependent vehicle-choice handling. Do not infer provenance just by testing whether the current value happens to appear in the latest list.

`SmallArmsFirePanel` receives the effective target and editing status instead of two competing strings. Its current selected-firer and existing guard checks remain in force. New shared components may check missing input and parse state; they may not reproduce ASL legality checks. Existing component-level rule checks are not moved or duplicated as part of this presentation refactor.

## 4. Map picking and visibility

Reuse the page-local `LocationPicking` owner and serial mechanism. Extend the presentation API with purpose-specific button text and a clear cancel action; retain defaults for unmigrated callers. No second picker service or global cache.

An accepted single-location pick disarms. A route remains armed until cancelled, as today. Ground-level picks expose the existing level choices rather than claiming the building level was determined by the click. Already-picked Use actions remain secondary and must obey the same restricted-choice policy. `PickedOption` currently has no disabled-choice contract, so either extend it with a backward-compatible filter or have the new editor supply only enabled matches.

Cancellation must be scoped to the field or section that owns the armed picker. Closing an unrelated section must not cancel a picker elsewhere. A hidden editor preserves unfinished text and its pending status; visibility cancellation only stops map input. Clear or an explicit context reset clears the text and pending status too.

Add explicit visibility notification from the Play inspector/tab owner and `ActionSection` to active editors. Closing a section or leaving the Play tab cancels its picker even though children remain mounted. Removing an optional group also cancels it. Continue cancelling on game/view/context reset. Cancellation must clear the map instruction and field pressed state together. Disposal remains a fallback, not the primary visibility contract.

Escape cancels an armed picker when focus is in its editor or the map. Scope keyboard handling to those surfaces; do not intercept unrelated dialog keys. Retain focus on cancellation where possible; if the editor is hidden, its disclosure/tab control is the fallback. Announce accepted picks and errors through one local polite status region. Do not steal focus from the map after every click, especially for routes.

## 5. Optional choices, help, and proposal

Unused optional controls appear behind named Add controls only when the existing host offers them. For Fire, Add spraying fire reveals its location editor. Once configured, a collapsed editor still exposes its active-value summary and Remove button. Remove clears submitted values and dependent local state. Closing an editor preserves the option. If a host change makes it unavailable, use the existing pruning policy, then show no stale active summary. Where there is currently no policy, resolve that specific case against the existing command contract before implementation; do not invent a rule in the UI.

Move the current long explanation inside the local Rules disclosure and show one short task instruction above the fields. Retain rule links and explanation content. Do not remove the separate phase-guide tab or duplicate its entire contents in every form.

The proposal area presents, in order, a short description of selected inputs, a missing-input/parse/busy/refusal message when applicable, and existing buttons. Reuse existing host reasons verbatim where appropriate. A blank or disabled control needs an associated explanation, but absence of a refusal does not mean legal. Preserve action labels and the current routine-action versus confirmation paths. Phase ending remains outside individual action forms in its current location and still requires confirmation.

## 6. Rollout and file boundaries

The full rollout covers Play action-entry surfaces. It does not redesign Setup, Replay, recorded outcomes, the map renderer, or rules calculators. Those are separate interfaces, not unfinished portions of this action-form change.

All paths below are under `LimboDancer.Domains.Asl.MapStudio/` unless stated otherwise.

| Stage | Files/families | Completion evidence |
|---|---|---|
| 1: shared pattern and Fire | New components; `Components/Pages/Play.razor`; `SmallArmsFirePanel`, `LocationField`, `PickedOption`, `ActionSection`; `Services/LocationPicking.cs`; `wwwroot/site.css` | Empty and populated Fire forms, target replacement, levels, optional spray, hidden-picker cancellation |
| 2: prove different shapes | `RallyActionPanel`, `InfantryMovementAction`, `MovementRoutePanel`, `MovementPlanPanel`, `AdvanceActionPanel`, `RoutActionPanel` | Compact form, ordered route, restricted destination, shared and per-unit obligations |
| 3: fire and vehicle variations | `OrdnanceFirePanel`, `OrdnanceTargetFields`, `BoundingFireAction`, `ReactionFireAction`, `MovementWindowActions`, vehicle movement/step/towing/scrounge and crew-exposure controls | Alternate aim, weapon/ammunition choices, reaction commands, vehicle-specific grouping |
| 4: remaining contextual actions | Repair, deploy, recombine, support weapons, smoke, starshell, spotter, charges, search, mopping up, opportunity fire, prisoners, pending choices/surrender, and CloseCombat families | Each active form gets appropriate grouping; declarations and resolution commands retain order |

Before editing, build a complete inventory from the current `Play.razor` action tree, its child components, and callbacks. Record every action-entry component as migrate, already-conforming, or not-applicable, with a reason and Studio scenario where applicable. The family table is a rollout map, not a claim of an exhaustive component audit. Include inline actions in `Play.razor`, not just files with Panel in their names. Treat CloseCombat subcomponents and conditional vehicle controls as separate entries where they contain inputs.

A stage can be reviewed separately, but Stage 1 is not completion of the approved full design. Any subsequently agreed deferral must be recorded in the existing backlog and identified in the implementation report. No backlog or rule-claim changes are needed for this document-only proposal.

## 7. Verification, with Studio first

No build or tests are needed to write these documents. After implementation approval, refresh main and review concurrent changes before beginning. Use Codex's worktree and Studio on port 6680, allowing the build time it needs. Do not replace another agent's Studio or mutate the user's ongoing game for verification. Use suitable existing test games or dedicated copies.

Studio checks, first:

1. Fire at a narrow and a wide inspector width: no horizontal overflow, stable semantic ordering, long unit names, no empty groups, visible proposal feedback.
2. Select a suggested target, enter a different target, then select another suggestion. The displayed, summarized, and proposed targets agree. Repeat with an unfinished/invalid edit and Clear.
3. Pick a primary target, choose a level, pick a spray location, cancel, remove spray, close the section, and change tabs. Hidden fields never consume subsequent picks. Repeat view/context reset with a pending edit.
4. Multi-board input and multi-level candidates, including a disabled destination. No first-board/first-level guess and no bypass of disabled choices.
5. Keyboard-only list/manual entry, focus order, Escape cancellation, visible focus, associated feedback, and screen-reader names/status. Check browser zoom as well as viewport width.
6. One compact action, a movement route, rout obligations, alternate ordnance targeting, and staged close combat. Preserve required answers and distinct proposal/resolution actions.

Then run only affected existing test methods and targeted behavioral tests for the risky transitions: canonical target replacement, pending-edit stale submission, reset with unchanged value, restricted-choice map selection, hidden-picker cancellation, and optional removal. Locate the existing Studio test project and reuse its fixtures; do not add a project or broad per-component snapshot suites. Styling changes alone do not justify new tests. Search changed IDs and contracts to identify existing assertions that need updates.

The existing project is `src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests`. Start discovery with `PlayComponentTests.cs`, `PlayPageFireTests.cs`, `PlayWorkspaceComponentTests.cs`, and `LocationWordsTests.cs`; these are search locations, not instructions to run entire classes.

Before merge, follow the repository's applicable merge checks and green CI requirement. Do not run a full local suite merely because a form changed. If implementation expands into planner/event behavior, stop treating it as presentation-only and apply the appropriate gate and design review. User authorization is required for that expansion and for merging the eventual PR.

## 8. Memory and performance checklist

| Checklist question | Design answer and evidence |
|---|---|
| Work per state/unit/hex/step | Layout wraps the existing action inputs. Location matching scans the already supplied choices after a relevant interaction; no new map traversal, LOS computation, route search, or planner call. See analysis evidence and `LocationPicking`/`PickedOption`. |
| Ownership, keys, lifetime, bounds, concurrency | Effective drafts remain page-owned. Editors retain only current field text/mode/status and reset identity. Existing page-local picking owns one armed field. No shared cache, asynchronous fill, unbounded history, or new background task. |
| Disclosure, snapshots, pinned versions, seams | Reuse host-provided viewer-scoped choices and labels. No event, state, rule-version, rout-settlement, or rules API changes. Do not derive labels through privileged state. |
| Counter fragments and layers | No map-renderer or counter-layer changes. A field edit may cause the existing page render, but must not add whole-layer construction/transmission. |
| Regression checks and measurements | In Studio compare baseline and changed typing, selection, and picking responsiveness on the same scenario. Check for duplicate handler calls and unexpected map/planner work. Record observations and limitations; do not claim a performance gain without measurements. |
| Test input costs | Reuse existing small fixtures once per test run where supported; no repeated full game/map builds for layout assertions, no new test project or CI share. |
| Benefit versus noise | Intended benefit is clearer interaction, not faster rules processing. Repeated stalls or extra engine calls block completion until explained and corrected. |
| Outputs and cleanup | Before any builder, set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`. Put task build/test outputs under its `action-form-design` folder, register them per repository practice, and remove only verified task-owned disposable outputs after checks. Keep a running Studio only when needed for review and record its location. No repository `bin`/`obj` output. |

Authority: [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>) and [Standing Rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>), as amended by repository `AGENTS.md` for Codex.

## 9. Traceability and review

| Analysis requirement | Design coverage |
|---|---|
| Section 4: grouping and free flow | Sections 1, 2, 6; Studio checks 1 and 6 |
| A1: one value | Section 3, especially Fire migration; Studio check 2 |
| A2-A3: ownership and picking mode | Section 4; Studio checks 3 and 5 |
| A4: board and level | Sections 3 and 4; Studio check 4 |
| A5: different policies | Section 3.1; disabled-choice handling in section 4 |
| A6: lifecycle | Sections 3.2, 4, 5; Studio check 3 |
| A7: disclosure | Sections 3.1 and 8 |
| Section 6: options and feedback | Section 5; Studio checks 1-3 |
| Section 7: phase-specific adaptation | Section 6 inventory and all rollout stages |

Self-review: checked for accuracy, correctness, internal consistency, and coverage of the revised analysis. The review distinguishes proposed APIs from current code. No implementation or Studio success is claimed. Detailed findings and their disposition are recorded in the [review](<../Reviews/ASL Play Action Interface Document Review.md>).

## 10. Approved Movement workflow follow-up

The user approved the follow-up analysis on 2026-10-31: connect the selected map hex to the offered movers, keep destination entry visible, and give map input one active owner. This extends the original layout scope. A separate demo and explicit approval are still required before merge.

- An inspected hex offers `Move units from [hex]` for each complete location represented in the host's existing mover rows. Each level is separate, and another board's identically named hex does not match. The button exposes those rows for explicit checkbox selection; neither inspection nor opening the group selects units. The original full list and its selected-unit summary remain available and synchronized through the same callbacks.
- Route plotting and single-step destination entry remain visible. Additional movement choices use a disclosure which opens for configured choices. With no offered infantry movers, show a clear empty state and omit idle movement inputs, while retaining an active route's status or continuation controls.
- Starting field picking stops route plotting. Starting route plotting disarms field picking. Escape on the map and changing inspector tabs stop the active map input. Closing the Movement section stops its direct route picker. Existing Unit Planner stop behavior preserves checked queued steps; cancelling a direct, unsubmitted route clears that draft. View or phase changes discard the old input mode.
- The host passes locations only for its existing view-scoped mover rows. This adds linear presentation work over those rows and no new eligibility decision, map search, event, shared cache, or whole-layer rendering. The planner and existing proposal path remain authoritative.
- Studio checks cover an empty mover list, a populated origin, selection without auto-submission, destination picking, switching both directions between picking and plotting, Escape, and inspector changes. A small component regression covers board and level distinction. No full suite is required for these presentation changes.

External Studio outputs for this follow-up are `action-form-design/studio-movement` and `action-form-design/studio-map-movement` beneath the Codex gate folder. The builder registers each; superseded binaries are removed after the replacement demo is established. The latest active demo stays available for the user's review.

## 11. Approved fire-presentation follow-up

The user approved the Advancing Fire findings on 2026-10-31 and asked for concise text throughout the remaining phase reviews. Keep From and Director together above the unit list. Show each unit on its own row, with its weapons and the short `MG only` choice indented underneath. Preserve existing callbacks, disabled states, and selection requirements; accessible names retain the weapon's holder and location. Remove repeated holder names from the visible weapon labels, the repeated valid-target feedback, and the paragraph describing internal processing. Keep useful refusal reasons and rule references.

The MG-only preview names the selected weapon or weapons and their holder, using the already gathered attack facts. This changes presentation text in the plan's reasons, not legality, firepower, dice, or recorded events. Mixed groups continue to name each contributing unit or its weapon-only contribution. Vehicle wording is retained.

Memory/performance: group the view-scoped offered weapons by holder once per render, with render-local storage bounded by the offered list. No new map/state scan, persistent cache, disclosure path, or counter-rendering work. Verify the hierarchy and selection behavior in Studio, then run only existing regression methods whose labels changed. No performance improvement is claimed.

The registered external build is `action-form-design/studio-fire` under the Codex gate folder. The launcher explicitly supplies this worktree's verification fixtures because the external build cannot discover them by walking its parent folders. Keep the active demo for review; remove superseded task builds and disposable test binaries with the guarded cleanup script. Main remains unchanged until the user's explicit merge approval.

## 12. Approved Rout follow-up

The user requested the Movement map-selection pattern for Rout on 2026-10-31, then refined it to scope the pane automatically when a hex is inspected. A hex with eligible routers scopes the obligations, suggested routes, shared-route actions and unit dropdown to that board and hex. All levels remain available with their existing labels. No unit is automatically selected. `Show all rout units` releases the scope. A fresh inspection of another eligible hex switches scope; an inspection with no eligible routers releases it. Re-clicking the same hex after release scopes it again. Armed route picks only append steps and never change the origin scope. Voluntary Break is hidden while scoped, since that is a separate action.

Switching to a scope which excludes the selected unit clears its draft. Changing units clears the previous unit's route, leader, retained load and Low Crawl choice and stops route picking; choosing the same unit preserves its draft. A completed scope with no remaining eligible routers releases automatically. Scope is component-local and reset with the game, view, turn or phase. Passenger routes beneath vehicles count as routable actions using the host's existing offers.

Retain the existing suggested routes, shared-route actions, voluntary break, leader, load, passenger and waiting behavior. Route entry uses the existing ordered, multi-hex LocationField. It remains disabled until a unit is selected, with a concise instruction to pick each hex and cancel picking before inspection. Replacing a route from a suggestion stops picking. No eligible units means no idle route form. Obligations and surrender explanations remain visible, and full rules remain in the disclosure. No rules, recorded events or rout calculations change.

Memory/performance: locations are looked up only for the cached, viewer-scoped choices and obligations. Matching scans those rows, not the full state or map. The component retains one origin scope and the last observed pick and uses the page-owned draft and picker; no new cache, engine call, map rendering or background work. Studio will verify empty and populated forms, at least two eligible origin hexes, scope switching and both release paths, explicit selection, route picking, cancellation and a proposal. A focused component check covers board/level distinction, scope transitions and picker ownership; reuse an existing integration check for suggested routes and retained equipment.

Build outputs outside the repository: the registered Studio output is `action-form-design/studio-rout` under the Codex gate folder, with focused test outputs in `action-form-design/checks`. Remove superseded builds after the replacement is running; retain the active demo for review. The separate `ui-rout-demo-02` copies only setup and phase events, places the demo enemy in [A1] and Russian squads in [A3] and [B2], and uses the normal Voluntary Break workflow to obtain broken units. It never overwrites a game. Do not merge without the user's approval.

As built: `ui-rout-demo-03` is a fresh copy of that two-hex setup for the final Studio checks, while the user's play in demo 02 is preserved. The implementation record lists the verified scope transitions, passing preview and focused regressions.

## 13. Approved Advance follow-up

The user requested the same map-selection design for Advance on 2026-10-31. Inspecting a hex with host-offered advance candidates scopes the unit list to that board and hex, retaining all levels and their labels. `Show all advance units` releases it. Another inspected hex switches or releases scope; repeated clicks work after release. Inspection never selects units. A new scope excluding selected units clears the selection and destination/exit draft. Armed destination picks retain origin scope and use the existing restricted location chooser, including explicit level selection and unavailable-location feedback. Scope resets with game, viewer, turn and phase, and releases when its candidates disappear.

Keep the unit selection first, then the destination and proposal when units are selected. Omit idle destination and exit controls when no units are selected; retain a concise selection prompt or empty state. Keep map exit in a disclosure, expanded when configured. Preserve off-board candidates in the unscoped list and retain the existing entry information. Shorten the visible help to the interaction; detailed rule text stays in its disclosure. No eligibility, destination calculation, rule, event or proposal semantics change.

Memory/performance: pass locations only for the existing viewer-scoped advance candidates. Filter those bounded rows on interaction; no new map traversal, cache, counter rendering or engine query. Page-owned selection remains canonical. Selection changes stop only the Advance destination picker. Verify two origin hexes, both release paths, origin changes clearing drafts, armed destination picks preserving scope, invalid picks, cancellation, proposal and confirmation in Studio. Then add one focused regression for the scoping transitions and reuse the existing restricted chooser level check only if that chooser changes.

Build outputs outside the repository: register `action-form-design/studio-advance` under the Codex gate folder. Retain it for the demo and remove the superseded Rout build after verification. Use a separately named demo with eligible units in two hexes; preserve the Rout review game. No merge without the user's approval.

Studio refinement: `LocationPicking.PickedBy` records the armed field at the instant the page receives a click. Advance checks that ownership as well as the current picker state, so the restricted chooser disarming after a successful pick cannot turn that same click into a new origin inspection. The final refinement build is `action-form-design/studio-advance-final`; remove `studio-advance` after it replaces that build.

## 14. Approved Close Combat follow-up

The user approved this redesign on 2026-10-31. For simultaneous Close Combat, present the existing first-side declaration and handover, then the second side's explicit declaration, then a combined review and resolution proposal. Selecting checkboxes is an unfinished attack, not a declaration: neither handover nor review may skip that selection. Add attack or Clear selection completes that editing step. Added attacks have Edit and Remove controls. Review has a return-to-edit control and retains both sides' attacks and optional declarations. An explicit no-attacks declaration is valid UI input, with legality still checked by the existing planner. The adjudicator and sequential rounds use the existing supplied step/side facts and their own review, without imposing a new simultaneous sequence. A11.11-A11.12 were read on rulebook PDF page 72 and A11.4 on page 73.

Keep location first, then aligned attackers and targets with one unit per row, optional Director, Add attack, the declared-attack list, and one finish action. Capture, round options and infiltration use disclosures; configured choices open their disclosure. Ambush is shown when due and blocks attack building. SMC stacking and offered withdrawals stay available. Closed locations and empty candidate lists omit idle attack forms. Detailed rules stay in the rule disclosure.

Map inspection matches only the host's offered locations by board and hex. A unique match chooses that location; several levels require an explicit dropdown choice. Another hex changes or releases the location. Release location is explicit. Changing location with unfinished selections or declarations first asks whether to clear that round; it never silently discards them. Armed map picks are ignored using both current ownership and ownership captured at click time. Game, view and revision changes recreate the panel, discarding only its presentation state. Existing page-owned drafts and revision-bound first-side storage remain canonical.

Memory/performance: matching scans only supplied location strings after a fresh pick. The panel retains a review flag, one pending location and the last pick, with no new global cache, map traversal, rules logic, event or counter-rendering work. Progress labels use the host's existing declaring-side reader; optional declaration summaries enumerate only the current small dictionaries. Studio checks cover both-side declaration, no-attacks choices, unfinished draft guards, editing, review/back, map selection/release, and proposal contents. Focused regressions cover those transitions and the existing simultaneous, capture and withdrawal paths whose controls changed. No full local suite.

Build outputs outside the repository: register `action-form-design/studio-cc` under the Codex gate folder, retain the active build for review, and remove superseded task binaries after replacement. Copy the unresolved recorded position into separately named CC check and demo games using exclusive creation. Do not overwrite the user's game or merge this branch without explicit approval.
