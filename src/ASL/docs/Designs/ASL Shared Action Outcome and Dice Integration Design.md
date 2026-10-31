# ASL Shared Action Outcome and Dice Integration Design

Date: 2026-10-31.

Status: Implemented and approved by the user on 2026-10-31 on `codex/action-outcome-dice`, originally branched from fetched `origin/main` at `ad9a1cd8`. The user authorized pushing and merging once CI passes. Section 10 records the implementation and checks.

Based on the reviewed [Dice Display and Action Outcome Analysis](<../ASL Dice Display and Action Outcome Analysis.md>). Source baseline: `038bc28ece9c2a3387044c248ff131594e51490f`, the head inspected in the `dice-lab-01` worktree. Reconcile with current main before implementation.

## 1. Intended result and scope

Present each disclosed action as one coherent outcome: what happened, what changed, the rolls that explain it, and any live response required. Reuse that presentation in Play and Replay. Use `AslDice` for actual dice values wherever the active Studio presents them.

An action's lifecycle is distinct from its tactical result. A recorded action may have an unsuccessful result. An uncommitted proposal may be awaiting confirmation, refused, stale, or failed. Historical results must never offer live confirmation controls.

### Limits and deferred work

- No changes to rules, game commands, event schemas, recorded dice, or random-number consumption are planned. This is a MapStudio presentation change.
- Workbench dice-fact editors and richer Rules contract metadata are deferred. Existing contracts do not always identify count, roles, or grouping. Show a textual pending-roll notice where those facts are unavailable; do not invent faces or infer dice semantics from prose. Add this deferral to the existing backlog during implementation.
- Category filtering would be a new feature and is excluded. The chronological Records presentation preserves category labels and turn/phase context, with no new filter controls.
- Rule-help prose mentioning dice is not a recorded-roll site and remains prose.
- New animation beyond the existing brief SVG effect, sound, persistent dice history storage, and a general event-schema redesign are excluded.

Any additional gap discovered during implementation must be named in the as-built report and backlog. It must not silently reduce the coverage described here.

## 2. Presentation decisions

### Recorded action

Use `ActionOutcomeCard` with this reading order:

1. Action title, actor/target and disclosed hexes, turn/phase context, and a secondary lifecycle label.
2. A concise result headline, followed by separate changes for affected units or equipment.
3. Resolution rows: purpose, subject, dice, supplied arithmetic, and result.
4. A live response area, supplied only by the existing live action flow when applicable.
5. Expandable evidence: full arithmetic, modifiers, references, explanatory notes, and Replay links.

Do not sum tactical effects into a universal success/failure badge. For multiple targets, show each target's outcome. Actions without dice omit resolution rows. Use bracketed hexes through the existing display helpers.

The collapsed card shows the headline and relevant disclosure notice. Expansion reveals the complete disclosed action, not a four-entry truncation. Detailed multi-target tables can remain inside the shared card; they do not need to be forced into identical columns. Retain the useful fire and close-combat layouts while replacing their dice text with the shared display.

Use one containing scroll area instead of nested fixed-height scroll boxes for the latest action. On narrow screens, resolution rows stack in their reading order. Tables retain accessible overflow only where tabular comparison is necessary.

### Proposal

`ProposalReviewPanel` uses the same visual shell but retains ownership of its existing confirmation interaction. Its action controls remain near the top. Required consequence warnings remain prominent and precede confirmation when the existing flow requires that order. Routine evidence may collapse; consequence warnings may not.

| Supplied proposal outcome | Presentation | Controls |
|---|---|---|
| `NeedsConfirmation` | Awaiting confirmation; known facts and disclosed consequences | Existing Confirm/Cancel behavior, eligibility and busy state |
| `Committed` | Recorded; tactical result shown separately where available | No repeated confirmation |
| `Replay` | Already recorded; this attempt changed nothing | No repeated confirmation |
| `Stale` | Game changed; proposal must be prepared again | Preserve existing recovery flow |
| `Denied` | Refused, with disclosed reasons | No Confirm |
| `Failed` | Failed, with disclosed explanation | No Confirm |

The proposal outcome named `Replay` is not the Replay page. Never turn every uncommitted proposal into an awaiting-confirmation card. Do not draw preview placeholders as real dice.

### History surfaces

- **Latest:** Complete latest meaningful attempt, using the existing selection semantics so an automatic phase event does not hide the action result.
- **Records:** Newest actions first, with chronological resolution order within each action and turn/phase context. Initially materialize 25 card summaries, with a keyboard-accessible "Show more" control adding 25. Never truncate an action at the batch boundary. This is cumulative loading, not fixed-page navigation or virtualization: repeated requests can render every summary in the history. Reset the loaded count on game/view changes and check the fully loaded long-history case.
- **Since you last looked:** Compact attempt summaries for the existing revision interval. Expansion stays within that interval; label a partial attempt when the interval cuts through it.
- **Replay:** One selected step's disclosed action content, preserving partial/no-disclosure notices, the existing separate "What changed" projection, and step navigation. A historical pending choice is descriptive only. Visible changes remain readable even when the step has no disclosed action events.
- **Raw rolls and completeness:** Play's compact diagnostic history retains its latest-six-roll limit. An action card includes all associated disclosed rolls without that limit. Replay's step roll list includes every disclosed roll in the selected revision interval, including rolls not associated with a card. Show a roll only once within a card, although a separate diagnostic list may also show it. Never derive complete action or Replay content from the six-item `RollRows` list.

## 3. Component boundaries and contracts

All new models belong to MapStudio Services and contain presentation data, not rule decisions. The following names are proposed implementation names.

| Type/component | Contract |
|---|---|
| `AslDieView` | Optional value, sides, explicit role (`White`, `Colored`, `Subsequent`, `Independent`, or `Unspecified`), and safe accessible label |
| `AslRollView` | Stable key, purpose, safe subject label, state (`Recorded`, `Queued`, or `Pending`), ordered dice, explicit grouping, optional supplied arithmetic/result, and provenance |
| `RollArithmeticView` | Optional original value, ordered modifier descriptions/values, optional final value and comparison/result text; numeric fields support decimal values |
| `ActionEffectView` | Stable event/subject key, disclosed subject, change text, and associated roll keys |
| `ActionOutcomeView` | Stable attempt/step identity, title/context, lifecycle, disclosure notice, summary, ordered effects and resolution groups, evidence and replay destination |
| `ActionOutcomeCard` | Renders a supplied outcome; accepts controlled expansion and optional live content slots; reads no game history and invokes no calculator |
| `AslDice` | Renders one supplied roll in compact or expanded form; owns no random generator and offers no Roll button |
| `DiceLabRoller` | Owns the lab controls, random generation and example state; constructs input for `AslDice` |

Keep models immutable to callers. Do not store raw hidden events or unrestricted state objects in them. Prefer plain strings and typed values over prebuilt markup. Use Blazor's normal escaping.

`ActionOutcomeView` stores no command callbacks. The live parent supplies existing controls as content; Replay supplies none. This prevents cached or historical models retaining stale command closures.

A role and grouping describe supplied data, not inferred rules. The adapter for a known event contract assigns them explicitly. An unknown role remains unspecified. An independent collection has no automatic summed total. A pair with a subsequent die retains a separate subsequent group.

### Dice rendering

Use SVG pips for valid d6 values. For other sides or unsupported values, preserve the supplied numeric text with its die specification; never clamp a value into a d6 face. Pending values use an unrolled indicator only when count and roles are known. A notice alone is the fallback when they are not. Omit undisclosed dice rather than exposing their count through placeholders.

The existing lab treats index zero as white; existing fire calculations treat the first stored attack die as colored. Map these explicitly at the adapter. Do not globally swap every array or change stored order.

Production displays show supplied arithmetic. They do not calculate tactical success, thresholds, rounding, or modifier applicability. Preserve decimal modified totals, including those already supported by the close-combat presentation. Missing arithmetic stays absent; the available dice and existing result text remain useful.

Move production styles into a shared dice stylesheet, leaving lab layout styles separate. Load shared styles once through the normal application stylesheet path. Preserve text equivalents, visible role labels, focus indicators, and reduced motion. Static history must not contain an assertive or repeated live region for every die.

The parent may request the existing brief animation for newly committed live roll keys only. Initial page load, handover, history expansion and Replay navigation never request it. Use CSS animation and stable keys, with no per-row timer or polling. Announce one live action summary rather than every die independently.

## 4. Two adapters and their sources

### Recorded outcomes

Extend `PlayRecords` through a partial file such as `PlayRecords.Outcomes.cs`, with model definitions in `ActionOutcomeView.cs` and `AslRollView.cs`. Reuse the existing attempt association used by `Latest` and Replay. Do not group by text similarity, wall-clock time, or equal dice values.

Build a viewer-safe event/roll index for the reader's revision range. Each lookup applies the relevant disclosure rules before producing a roll, effect, or label. A visible parent event does not authorize disclosure of every linked event or modifier. Existing `PlayRecords` internals retain raw history and some lookups have their own disclosure logic; the class itself is not proof that a new join is safe.

Preserve names as disclosed at the event's existing naming boundary, generally just before it, including the special treatment of reveals. Never substitute a current unit name into an older concealed record. Preserve the distinction between withheld effects, no known target, and a disclosed empty result. In particular, retain `FireResolutionCard`'s suppression of irrelevant arithmetic for no-known-target and named-vehicle-only cases.

Extend `RollRow`, `ActionRecordList.Entry`, `CloseCombatTableAttack`, and relevant `FireView` fields with structured display payloads. Keep text fallbacks during migration. Change producer methods that currently flatten nested checks, vehicle effects or weapon outcomes into strings so they supply structure before formatting. Do not extract dice from those strings.

For an embedded check without a separate roll event, use an event ID plus check/subject ordinal as its display key. Deduplicate only when the source establishes that two presentations refer to the same roll. Unassociated visible dice remain in raw history rather than being attached to a guessed action.

Keep linked follow-up attempts separate, with contextual links if useful. Preserve full event ordering inside an attempt. A replay step or revision interval must never obtain later results by joining against the game's unrestricted current history.

### Replay's separate change projection

Retain the Replay page's existing `ChangeLines` input and the "What changed" section in `ReplayStepPanel`, adjacent to the shared outcome cards. This is an additional source of disclosed outcome information, not a replacement for either the recorded-outcome or proposal adapter. Do not rebuild it solely from `PlayRecords` or the categorized event lists.

The existing projection includes `ReplayDiff` results from the viewer's before/after snapshots, setup-transition notices, and residual-fire changes. Preserve its current inputs, disclosure handling and selected-step boundaries. In a `ReplayRead.None` step, show the existing safe step description and visible changes even if no action card can be constructed. Do not fabricate a hidden actor, roll, or tactical explanation for those changes.

Use `RollsIn(step.First, step.Last)` or its equivalent complete structured projection for Replay's roll list. The six-roll compact-history bound does not apply. Preserve unassociated visible rolls here so the card migration cannot discard them.

### Proposal outcomes

Adapt the existing `Play.razor` proposal state through a small UI helper such as `ProposalOutcomeView`. Reuse the existing `OutcomeText`, disclosure-aware reasons, evidence and consequences paths. Map lifecycle labels without changing eligibility. Keep pending attempt identity, expected revision, confirmation callbacks and execution in the existing parent.

After commit, associate recorded content using the existing attempt identity. If no visible recorded content exists, retain the safe lifecycle message; do not replace it with an older action's result. Any current response required by the game remains supplied by its existing live flow.

## 5. Integration map

Paths below are relative to `LimboDancer.Domains.Asl.MapStudio`.

| Sites | Planned change |
|---|---|
| `Components/Shared/AslDice.razor`; `Components/Pages/DiceLab.razor`; new lab wrapper and shared styles | Separate generation from display; preserve lab behavior |
| `Services/PlayRecords.cs` and new outcomes partial; `RollRow.cs`, `CloseCombatTable.cs`, `FireText.cs` | Retain structured data and construct safe action/roll models |
| `Components/Play/FireArithmeticBreakdown.razor`, `InfantryFireEffectsTable.razor`, `VehicleFireEffectsTable.razor`, `FireResolutionCard.razor` | Display primary and nested rolls beside their results while retaining existing suppression rules |
| `Components/Play/CloseCombatResultTable.razor` | Per-attack dice with separate defender arithmetic and results |
| `Components/Play/ActionRecordList.razor`, `FireHistory.razor`, `DiceRollHistory.razor` | Use shared outcomes and compact supplied-roll displays; preserve replay destinations and diagnostic metadata |
| `Components/Pages/Play.razor`; `Components/Play/ProposalReviewPanel.razor` | Connect both adapters; replace latest/Records/since-looked composition without changing action execution |
| `Components/Replay/ReplayStepPanel.razor`; Replay page adapter | Render the selected revision range read-only, retaining `ChangeLines` and its separate "What changed" section, partial/no-disclosure notices, and every disclosed roll in the step |
| Game inspector and shared event-summary sites | Add a supplied-roll display for visible dice events, including any shared inspector summaries that currently flatten those events |
| `Components/Play/ScriptedDicePanel.razor` | Queued independent dice in order; retain Add/Clear behavior and development warning; no assumed colors or totals |
| Workbench, Turn Guide, and scenario-start summaries | Retain required-roll notices; render pending dice only with explicit known metadata; actual startup rolls use recorded outcomes |
| Icon coverage inventory | Regenerate using the established tool after final control changes |

Ordnance, rally, rout, recovery/repair, support weapons, snipers, weather/night, search, startup, and other recorded actions use the same producer migration. Inventory producer cases during implementation and record coverage by event type. Unknown cases retain readable text and visible raw dice rather than disappearing.

## 6. Memory and performance contract

| Concern | Decision |
|---|---|
| Ownership and lifetime | Outcome indexes and memoized models belong to one `PlayRecords` instance; proposal models belong to the current proposal/view |
| Complete inputs | Game history snapshot, through-revision, viewer and naming context; range requests additionally include first/last revision |
| Bounds | One current reader per page; Play's compact raw history retains six rolls. Action cards and Replay step rolls have no six-roll truncation. Records initially renders 25 summaries and cumulatively adds 25 per request; expanded details are built lazily |
| Invalidation | Replace reader/model when history, revision or viewer changes; clear expansion/page state on game/view change; never reuse across handover |
| Missing data | Cache absence only within that immutable reader; a new revision gets a new reader; absence never means a zero die or no effect |
| Concurrency | Keep new page adapters circuit-owned, with no static mutable cache; publish immutable results. If existing callers share a reader concurrently, use local construction and safe publication before enabling reuse |
| Added work | One linear indexing pass per reader, then indexed roll/attempt joins. No per-card full-history scan. Initial rendering is bounded by page size; expanded work is proportional to that action's disclosed events |
| Map impact | No new per-unit/per-hex map queries, counter fragment work, full-layer generation, or viewport transmission |

Cumulative loading bounds the initial render and each added batch, not the total rendered cards. Repeated "Show more" requests can render the entire history, and expanding every card can materialize all its disclosed details. The index also scales with events already retained by the reader. This design accepts that tradeoff rather than introducing virtualization; do not claim a fixed total rendering or memory bound. Replacing a reader must release its outcome index when no longer referenced.

For regression checks, use identical recorded games, viewer/revision ranges and build settings before and after. Include ordinary live steps, a multi-roll action, long Records history and Replay jumps. For long history, check both the initial batch and the fully loaded history with details expanded, then switch game/view to check reset and release behavior. Record observed response and retained-memory behavior if a regression is suspected; no performance gain is claimed. If cumulative loading causes an unacceptable regression, revise the design explicitly rather than silently truncating history. No new test project or expensive fixture generation is planned. State snapshots, rout settlement, pinned maps and seams are untouched.

## 7. Implementation tasks and evidence

1. Reconcile current main and shared-file changes in a Codex-owned implementation branch. Read applicable instructions and check open claims. This presentation scope takes no Rules claim unless it expands into Rules changes.
2. Extract the passive dice display and lab wrapper. Build MapStudio and verify the existing lab controls, values, roles, keyboard behavior and narrow layout.
3. Add immutable display contracts and indexed recorded adapters. Connect a compound fire action and a close-combat action first to prove nested rolls, role mapping, decimal arithmetic and disclosure.
4. Add the shared card and proposal adapter. Connect latest outcomes, Records, since-looked and Replay. Preserve Replay's separate `ChangeLines` projection and complete step roll list. Verify lifecycle distinctions, live controls and historical read-only behavior.
5. Migrate the remaining event producers and inspector/queue sites. Check the inventory against source searches; record every unconverted or unsupported case explicitly.
6. Complete the scoped Studio acceptance below. Fix presentation and disclosure failures before targeted automated checks. Regenerate icon coverage, run required source/CI checks, and follow the repository's applicable merge-gate process without duplicating the full suite locally.
7. Update as-built notes and backlog, report evidence and limitations, and clean registered outputs. New files and code reach main through a PR with green CI and the user's word for that merge.

## 8. Studio acceptance and targeted checks

Proposed whole-change Studio scope: 1920, 1024 and 320 pixel widths, keyboard and mouse, both sides and adjudicator. Before the final whole-pass walk, present the concrete changed surfaces and remaining coverage for approval as required by the standing rules. Task-level Studio checks proceed throughout implementation.

| Case | Evidence required |
|---|---|
| Lab | Single die, pair, separate subsequent die, positive/negative modifier, incomplete modifier, worked example; unchanged lab semantics |
| Recorded dice | Values and explicit colors match source; independent collections not summed; subsequent die excluded; decimal arithmetic retained |
| Compound outcome | Primary roll, target checks and follow-ups appear with their subjects and effects, without duplicate roll rows within a card |
| Lifecycle | Awaiting, refused, stale, failed, committed and already-recorded states remain distinct; actual tactical failure is independent of commitment |
| Disclosure | Opposing view has no hidden values, labels, counts or modifiers in visible content, attributes or accessible text; no-known-target and withheld-effect behavior preserved |
| Handover | Previous viewer's card/details cleared before next viewer; no replay of old animation or announcement |
| Replay | Forward/backward/jump show only the selected step's data; partial visibility retained; historical pending choices have no active controls. A step with visible map changes but no readable action events still shows its safe changes. Setup-transition notices and residual-fire changes remain present where the existing projection supplies them |
| Roll completeness | Play's compact history shows at most six rolls; an action and Replay step containing more than six disclosed rolls retain all of them. An unassociated visible roll remains in the complete Replay step roll list |
| Records | Cumulative loading and expansion do not split attempts or consume dice; category and phase context remain readable. Check initial loading and fully loaded/expanded long history, then verify loaded-count reset and old-view removal on game/view change |
| Queue/inspector | Queue faces remain queued values; visible inspector dice use recorded values; no hidden lookup through a linked event |
| Accessibility | Labels distinguish purpose and roles without color alone; disclosure toggles work by keyboard; no repeated live announcements; reduced motion respected |
| Integrity | Viewing, expanding, paging and replaying outcomes append no events and consume no random values |

After Studio completion, add/run only focused tests that protect changed risks: explicit die-role mapping, independent/subsequent grouping, disclosure across linked records and revision boundaries, completeness of an action/step with more than six rolls, preservation of visible Replay changes without readable action events, and lifecycle/control availability. Reuse existing fixtures. Do not add tests that merely assert component markup mirrors its implementation. No full local test suite is proposed; required CI and gate checks remain applicable.

## 9. Build outputs outside the repository

Writing this design creates no build output. Implementation uses Codex Studio on port 6680 and sets `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` before tools under `code-builders` run.

Use `E:/Archive/GitHub/dlandi/asl-gate-work-codex/action-outcome/studio-6680` for the implementation Studio output and `action-outcome/checks` under the same Codex working folder for the targeted checks. Both outputs are registered. Gate outputs use the existing gate registration. No full-suite or merge-gate build was run for this review.

Remove task-owned outputs at their earliest safe opportunity, no later than successful CI cleanup. The final implementation task checks the output registry and reports any remaining owned output. Leave Claude's checkout/output and the user's Studio untouched.

## 10. As-built record

### Implementation

Implemented in the working tree on `codex/action-outcome-dice`, from `ad9a1cd8`. No implementation commit, pull request, CI run, merge, or merge-gate run is claimed here.

- `AslDice` is passive. `DiceLabRoller` owns the Lab's generation and modifier controls. `AslRollView` maps explicit recorded purposes to colored/white or subsequent roles. Unknown collections remain neutral; a disclosed ordered subject list supplies independent-die labels. Unsupported faces remain numeric.
- `PlayRecords.Outcomes.cs` groups visible entries and rolls by their existing attempt boundary. `ActionOutcomeCard`, `ActionOutcomeFrame`, and `ActionOutcomeHistory` serve latest, historical, interval, and Replay outcomes. History loads 25 more complete actions at a time. Close Combat headings use the existing table's concise recorded outcomes; full arithmetic, defenders, and original explanation remain available.
- Fire, nested checks, vehicle/PRC effects, Close Combat and Leader Creation use the shared dice component. Ordnance, Rally and other textual records keep their recorded arithmetic, with linked visible rolls attached to their entries. Unassociated rolls remain in recorded order. Raw history, the inspector, ReadCase, and the scripted queue also use `AslDice`.
- Proposal presentation shares the frame through `ProposalOutcomeView`; existing proposal controls, confirmation rules and consequence ordering remain with their existing parent. Only the latest live card can offer the link to an existing required response. Historical cards have no command callbacks.
- Replay keeps `ChangeLines` separate. Its cached panel now contains outcomes, raw rolls, and changes instead of building the removed category lists. A no-roll action no longer incorrectly says that it has no record.

The conceptual arithmetic/effect models in section 3 are implemented by retaining the existing typed `FireView`, fire-check records, and `CloseCombatTable` in the entry model. Recorded textual arithmetic is preserved rather than parsed or recomputed. Public `FireReported` records do not supply a public roll-ID link: when the same attempt includes a visible primary roll, their inline arithmetic omits the duplicate dice widget and the identified raw roll is shown below. This follows no hidden record and compares no dice values.

### Studio evidence

Codex Studio runs on port 6680. The final application build passed with zero warnings and zero errors. The walkthrough covered:

| Case | Observed result |
|---|---|
| Dice Lab | Paired roll and modifier; three dice keep the subsequent die out of the primary total; negative modifier remains editable. |
| `p72-ammo-shortage-mmg`, Russian Replay step 2 | Colored 4, white 6; no effect and MMG malfunction preserved; one primary dice widget within the outcome card. |
| `p72-ammo-shortage-replacement`, Russian Replay step 2 | Colored/white IFT pair; neutral independent Cowering and Ammunition Shortage collections; replacement and lost LMG remain visible. Raw rows have separate purpose, faces, labels, source metadata, and optional recorded details. |
| `p71-check-kept-concealed`, Replay | Firing side retains the withheld-target explanation; target owner reads its own unit. Handover removes the previous view. Partial reports do not duplicate the primary dice inside the card. The final choice action remains a record despite having no dice. |
| `dl-tractor-works-01`, Play | Latest deployment includes its follow-up lineage. History grows from 25 to 50 cards without replacing the first 25. Rally and Close Combat, including Leader Creation, retain their arithmetic and effects. |
| Responsive and keyboard | Checked actual CSS viewport widths 320, 1024 and 1920. Expanded narrow combat card width and scroll width were both 248 pixels. Enter toggles disclosure, with explicit true/false accessibility state. Temporary viewport overrides were reset. |
| Game inspector and ReadCase | The available synthetic fixture has no dice. It remains readable and adds no fabricated roll. A dice-bearing inspector fixture was not available for this walkthrough. |

Screenshots are saved under `C:/Users/dland/.codex/visualizations/2026/10/31/01a1920e-7ed2-7df2-b739-b840a9c89e1d/`: `replay-colored-dice.jpg`, `replay-roll-layout.jpg`, and `outcome-narrow.jpg`. Build log: `E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log`.

### Targeted checks and remaining evidence

After the Studio walkthrough, existing page assertions were migrated to expand and read the new cards while retaining their behavioral checks. The final selection contains 28 affected existing methods, three focused regressions for roles, complete/disclosed roll collections and Replay map-only changes, and the required icon coverage regeneration. All 32 distinct checks passed: 26 on the initial run, two after updating old layout/punctuation assertions, and four on the interval/disclosure follow-up. `code-builders/build-and-test/check_codex_outcomes.ps1` runs that selection or an explicit smaller retry. Logs and TRX files are under `E:/Archive/GitHub/dlandi/asl-gate-work-codex/action-outcome/`: `checks.log`, `results/outcome-checks.trx`, `outcome-recheck.log`, `results/outcome-recheck.trx`, `outcome-interval-check.log`, and `results/outcome-interval-check.trx`.

The user's subsequent Studio feedback requested a collapse control for the entire bottom activity panel. `play-outcome-panel-toggle` now hides its full content behind a slim Show latest outcome bar, independently of the card's own detail expansion. The page retains the choice across ordinary rerenders and game/view selection. A fresh Studio build completed with zero warnings/errors. In `dl-tractor-works-04`, Russian view, the whole panel shrank from 206.69 to 31.73 CSS pixels and the map pane grew from 325.46 to 500.42 pixels. Enter reopened the content and restored `aria-expanded="true"`; clicking again hid it and restored `aria-expanded="false"`. The page is left collapsed for user review. The required icon inventory refresh is the only additional automated check for this small layout change.

No full local test suite was requested or run. Proposal failure/confirmation branches are covered by existing affected page checks rather than changes to a user's saved game during the browser walkthrough. No gameplay rules, events, persisted schemas, random-number generation in Play, or package references changed.

The approved request-metadata and legacy-subject limitations are recorded in backlog section 90. Detailed DOM is created on expansion, but the view adapter still materializes the source readers' models once per view/revision. Loading every history card can therefore still be expensive; no claim of virtualization or constant-memory full-history expansion is made.

The final icon inventory refresh passed: `outcome-collapse-icons.log` and `results/outcome-collapse-icons.trx` under the same evidence folder. The previously passing behavioral checks were not rerun for the collapse control.

The outcome bar places its Show/Hide control at the left edge and the current viewer-safe latest summary at the right. Summaries longer than 75 characters display their first 72 characters followed by `...`; shorter summaries remain unchanged. The full summary is available in the title tooltip and expanded card. The bar stays on one line and clips further to fit narrow screens. This reads the existing cached outcome projection and adds no history scan or cache.

### Owned outputs

The final summary uses bold red text (`#b3261e`, weight 700), verified in the running Studio. This stylesheet-only adjustment required no rebuild or additional automated tests.

The summary-bar follow-up was checked in Studio on `dl-tractor-works-04`, Russian view: left toggle, right summary, and successful collapse. Its build passed with zero warnings/errors in `E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log` (elapsed 00:02:10.49). No additional automated tests were run for this layout-only follow-up.

Remaining registered task output: `E:/Archive/GitHub/dlandi/asl-gate-work-codex/action-outcome/studio-6680`, retained for user review on port 6680. The completed targeted-check output `action-outcome/checks` was removed and its registry entry cleared by `clean_codex_outcome_checks.ps1`; logs and TRX evidence are retained. Claude's outputs and the user's Studio have not been used for this work. Changed text files satisfy LF and UTF-8 without BOM, and `git diff --check` passes.
