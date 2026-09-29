# ASL Map Studio UI Redesign 01 Design

**Status:** Proposed. Design only; implementation has not started.

**Date:** 2026-09-28

**Branch:** `UI-Redesign-01`, based on local `main` at `2fa3f564056049d222f722e4b5bc925d51c3abd3`.

**Purpose:** Evolve the supplied UX audit and stylesheet into an implementable design for browsing, authoring, inspecting, and playing in Map Studio.

**Parents and companions:** [Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), [Architecture and Rendering Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design.md>), [Unit Display Design](<ASL Unit Display Design.md>), [Governed Writes Design](<ASL Unit Governed Writes Design.md>), and [Unit Read Contract Design](<ASL Unit Read Contract Design.md>).

## 1. Outcome and boundaries

The Studio should make the next useful task apparent while keeping the board, current selection, and result of an action understandable. The redesign combines a consistent application shell with two page families: collection pages for finding things and workspaces for manipulating or inspecting them.

The supplied `ASL Map Studio UX Audit & Redesign PDF` (one page, dated 28 Sep 2026) and `app.css` are design inputs supplied by the user. Their recommendations and the stylesheet's claim to be a drop-in replacement are evaluated here, not treated as implementation instructions. The originals remain outside the repository; this document records the decisions needed without requiring those files at build time.

The baseline includes backlog passes 10 through 13: terrain and movement, vehicle movement and overruns, fire extensions, and Rally, Rout, and support weapons. Concurrent pass 14 work is outside this baseline. Before implementation, reconcile any newly merged actions and UI controls so the redesign does not remove capabilities.

This proposal changes presentation and interaction. It preserves:

- Deterministic server-generated SVG, existing terrain rendering, board labels, counter styles, and far/mid/near counter tiers.
- Browser-local pan and zoom, canonical board and Location identities, and existing rendering and read boundaries.
- Game projections, disclosure rules, replay, revision checks, and propose-then-confirm writes through the Execution Gate.
- Fidelity evidence, provenance, and restrictions on saving source-derived drafts.
- Existing routes and meaningful deep-link parameters, including board, game, perspective, and revision.

New rules adjudication, changes to game persistence, VASL artwork, a new component-library dependency, and a general mobile gameplay product are outside this redesign. Narrow-screen access and keyboard operation remain required.

## 2. Audit findings translated into decisions

| Audit finding | Design response | CSS alone? |
|---|---|---|
| Flat visual hierarchy | Shared tokens, page headers, labeled primary actions, consistent controls | Partly; actions need markup |
| Ungrouped navigation | Collapsible grouped navigation with current destination and breadcrumbs | No |
| Long selects and sentence-flow forms | Searchable categorized pickers, field groups, separate help text | No |
| Library hard to browse | Search, status/type filters, SVG previews, explicit result states | No |
| Dense viewer toolbar | Separate view, layers, game/replay, and inspection controls | No |
| Unbounded page layout | Collection width limits and viewport-sized workspaces | Partly |
| Weak feedback | Explicit loading, empty, failed, busy, disabled, and completed states | No |
| Internal references in primary copy | Plain task descriptions with expandable technical evidence | No |

The audit's reported 236 library rows, approximately 190 SSR rules, and 35 unit kinds are observations of its session, not fixed application limits or acceptance fixtures. Current code already supplies some colored statuses and loading messages. Make them consistent rather than assuming none exist.

## 3. Application shell and navigation

Use a compact top bar for application identity and a collapsible left navigation area. Put the page title, breadcrumb, short description, and page-level action in the content header. Avoid placing object-editing tools in global navigation.

| Group | Destinations and existing routes | Page-level actions |
|---|---|---|
| Boards | Board library `/`; viewer `/boards/{BoardName}`; editor `/author/{BoardName}` | New board `/author/new`, Edit |
| Maps | Maps `/maps` | New map, Preview, Save |
| Units | Unit Lab `/units/lab` | Existing unit and placement save/export actions |
| Play | Live play `/games/play`; Game states `/units/games` | New game, Open game |
| Verify | Fidelity `/fidelity` | Run verification |
| Settings | Settings `/settings` | Existing configuration tasks |

Game states remains distinct from live play and retains synthetic/live source labels. Do not rename it to History, because it also exposes fixtures and sources. Board viewer breadcrumbs reflect the opened object: a composed map belongs under Maps even though it shares a viewer route.

Use `NavLink` or equivalent route-aware state, including an exact root match and explicit parent-group selection for nested routes. Expose `aria-current="page"` on the current destination. A collapsed sidebar retains accessible destination names; narrow layouts use a labeled navigation button and drawer. Settings stays reachable in all modes.

Keep route changes separate from sidebar expansion. Collapsing navigation must not discard a draft, reset a viewport, or navigate away.

## 4. Layout and visual system

### 4.1 Page families

Collection pages use a centered content area with a proposed maximum width of 90rem; explanatory text is limited to about 70 to 80 characters per line. Tables may use the full content width. Workspaces use available width and height with explicit scroll ownership: the canvas stays visible while tools and inspector contents scroll independently.

Use a shell grid or flex layout based on actual header size, with `min-height: 0` and `min-width: 0` on shrinking children. Avoid a global `100vh - 5.5rem` assumption. Use dynamic viewport height where supported, with a fallback. Do not apply a content max-width to every `main` or every workspace.

Proposed responsive behavior, to validate against content:

| Available viewport width | Navigation | Workspace |
|---|---|---|
| At least 1440px | Expanded by default, collapsible | Tools, canvas, inspector; inspector initially about 22rem |
| 1024px to 1439px | Collapsed by default | Canvas plus one side pane; tools may open as a drawer |
| Below 1024px | Navigation drawer | Map and task/inspector views switch through labeled tabs |

Forms and library search reflow down to 320 CSS pixels. Wide data tables use a labeled horizontal scroll region. Keep the canvas usable rather than squeezing it between fixed-width sidebars. Browser zoom must trigger the same responsive behavior.

### 4.2 Palette and typography

Adopt the proposal's warm neutral background, white surfaces, dark header, and restrained olive accent as the initial direction. Keep magenta board selection, amber counter focus, and navy drawing gestures distinct from terrain. Interface tokens must not recolor terrain or counter SVG internals.

Use semantic tokens for text, muted text, surfaces, borders, accent, success, warning, error, and information. Warning banners retain warning meaning; the existing source-derived draft restriction must not become an informational blue banner simply because it shares `.banner`.

Begin with local system fonts for dependable offline rendering. IBM Plex Sans/Mono and Barlow Condensed are optional later refinements, self-hosted only after checking redistribution terms. Do not add the supplied Google Fonts import as a runtime dependency. Use condensed uppercase type sparingly for page identity, not dense form labels or instructions.

### 4.3 Controls

Use one visually dominant action per active task. Play can offer multiple action families, but only the active form's Review action or pending proposal's Confirm action is dominant. Avoid promoting every submit button globally.

Buttons use minimum height and allow meaningful labels to wrap when space requires it. Disabled controls remain legible and have a visible nearby reason when the reason is actionable. Preserve focus indicators on links, buttons, inputs, tabs, and canvas controls, including disabled link-style button overrides.

Statuses combine words and an optional symbol with color. Distinguish Not checked, Checking, Verified, Failed, Out of scope, and Authored valid. Never present approximate vectorization as exact or interpret missing verification as failure.

## 5. Collection and authoring screens

### 5.1 Board library

Keep authored boards and VASL boards discoverable through type filters. Preserve the existing Maps section as a compact link or summary leading to the Maps collection; avoid two competing map-management interfaces.

Place search and filters above results: board name/reference, source type, verification status, and out-of-scope inclusion. Show the result count and active filters together, with Clear filters. Search typed text locally where the existing catalog supports it; do not ingest every board on each keystroke. Preserve filters and scroll position when returning from a viewer.

Default to a compact list with optional small SVG previews. A preview uses the existing renderer and generated terrain, never VASL board artwork. Load visible previews on demand; cache by board identity/version and rendering options. Missing or failed previews show a labeled placeholder without making the board disappear. Do not trigger full fidelity verification to display a thumbnail.

Separate loading, no configured source, no authored boards, no matching results, and catalog failure. While loading, show a progress message instead of zero boards. Retain the full diagnostic detail through an expandable row or details panel.

### 5.2 Maps

Use a map collection followed by a composition workspace. The workspace has an ordered placement list, canvas preview, and selected placement/rule properties. Board pickers search reference and display name. Keep rotation, row, and column editable numerically; pointer rearrangement is optional later work, not a prerequisite.

SSR selection supports search and categories based on actual catalog metadata. Preserve the exact selected rule identifiers and their order. Explain scope and show unsupported or diagnostic results without inventing rule behavior.

Move compact placement syntax into an Advanced section, retaining full edit and round-trip functionality. Editing text or placements marks the preview stale until rebuilding finishes. An unsuccessful rebuild preserves the last valid preview, clearly labeled as out of date. Save must retain existing validation behavior.

### 5.3 Board viewer and editor

The viewer toolbar exposes view mode and Fit directly. Put layer toggles in a labeled Layers panel, and game source, perspective, revision, and counter style in a separate game/replay strip. Keep the current perspective and revision visible even when its controls collapse.

Inspector tabs are Selection, LOS, and Evidence. LOS is a first-class tab rather than a buried disclosure. Show source, target, result, and terrain contributions together; retain manual Location entry and selected-hex shortcuts. Evidence contains provenance, F1/F2 results, and diagnostic details.

The editor adds a tool rail and a document header with name, dirty state, Undo, Redo, and Save/Save draft. Preserve all tool options, layers, validation, gestures, keyboard shortcuts, and comparison modes. A warning on source-derived drafts remains visible whenever it constrains saving. Confirm navigation away from an unsaved draft using existing dirty-state information.

### 5.4 Unit Lab

Group fields into identity, combat/movement characteristics, capabilities, appearance, and placement. Hide irrelevant fields only when the underlying unit kind makes them inapplicable; changing kind must not silently destroy edited values without a clear reset policy.

Use a searchable kind picker and keep the preview adjacent to the active fields. Preserve side-by-side far/mid/near and classic/digital comparisons. Place raw JSON and style-sheet editing in Advanced sections without removing their existing functions. Validation points to the relevant group and field.

## 6. Play workspace

### 6.1 Persistent context

Promote the existing map from below the action forms into the central workspace. Keep game, side/perspective, turn, phase, and revision visible. Show live versus historical state explicitly. The selected unit or stack drives the task pane, but manual unit and Location entry remain available.

```text
+-----------------------------------------------------------------------+
| Game | View as | Turn / phase | Revision | Replay / live indicator      |
+----------+-----------------------------------+------------------------+
| Actions  |                                   | Selected unit / stack  |
| by phase |            Board / map            | Active task fields     |
|          |                                   | Review action          |
+----------+-----------------------------------+------------------------+
| Activity: latest result, rolls, changes; expandable history            |
+-----------------------------------------------------------------------+
```

This is a layout sketch, not a promise that all controls fit at all widths. On narrow screens, labeled Map, Action, and Activity views share the context header. A pending proposal is always reachable from that header.

### 6.2 Setup

Separate game identity, board/map choice, sides and scenario conditions, and unit placement into named sections. Give each side its own ELR and Friendly Board Edge labels. Put rule explanations in help text beneath fields, with expandable citations. Do not force experienced users through a multi-page wizard; allow moving between sections and reviewing the complete setup before proposal.

### 6.3 Phase and action organization

| Context | Task groups, reflecting currently implemented capabilities |
|---|---|
| Rally | Rally, repair, Deploy/Recombine, eligible support-weapon handling, recovery choices |
| Movement | Infantry/vehicle movement, SMOKE, movement costs, defender response window, overrun/reaction |
| Fire phases | Small arms and ordnance, applicable fire options, targets and modifiers |
| Rout | Rout requirement, destination/cover, route, Low Crawl, Interdiction and surrender results |
| Advance | Unit selection, destination, movement implications |
| Close Combat | Location, participants, Ambush/withdrawal, attacks, vehicle sequence, capture choices |

This table organizes tasks; it is not a new eligibility specification. Existing planners and phase rules remain authoritative. An action chooser can show an unavailable action with a public reason, but must not infer permission or disclose hidden state. Keep End phase separate from an active unit task so it cannot be confused with completing that task.

### 6.4 Review and confirmation

Selecting a unit, clicking a target, or editing a field only prepares an action. The UI's Review action invokes the existing proposal path. A successful proposal opens a persistent review pane with action, participants, declared destination/target, disclosed consequences, costs, and current revision. Confirm invokes the existing confirmation path. Cancel dismisses the proposal without changing game state.

Show refused, unsupported, indeterminate, stale, already committed, and committed results distinctly, preserving their actual service meaning. Never translate an unknown result into success. While planning, show a busy state and prevent duplicate submissions. Do not promise a completion percentage when none exists.

Changing action arguments, game, perspective, or revision invalidates the displayed proposal for confirmation. A stale result requires a fresh proposal and review; it must not silently confirm a revised plan. Retain existing attempt/idempotency behavior for repeated confirmation and retries. On reconnect, refresh authoritative state and reconcile any pending attempt before enabling confirmation.

The review pane must not reveal undisclosed defenders or consequences that the existing flow withholds until confirmation. Any unit search, tooltip, target list, activity entry, and accessibility label must use the appropriate projection. Clear selection and cached presentation data when perspective changes. Terrain-only LOS retains its existing availability.

### 6.5 Activity and evidence

Keep a compact latest-result summary visible. Expand activity into grouped records for movement, fire, ordnance, Close Combat, Rally/Rout/support weapons, and dice. Retain Fire Lane MG attribution and other explanatory facts added by recent commits. Rule citations are relevant evidence, not clutter to remove indiscriminately. Technical package identities and internal design references belong in a separate evidence disclosure.

## 7. Interaction and accessibility acceptance targets

- Every workflow can be reached and completed with a keyboard. Canvas actions have equivalent labeled controls or Location inputs.
- Searchable pickers support typing, arrow navigation, selection, Escape, and an announced result count; retain native selects until a replacement supports these behaviors.
- Tabs expose selected state and predictable keyboard navigation. Opening a drawer moves focus appropriately; closing restores focus to its opener. Persistent desktop panes do not trap focus.
- Loading and action completion are announced without repeatedly reading large logs. Validation summaries link to fields; urgent errors remain visible until addressed.
- Use proposed contrast targets of 4.5:1 for normal text and 3:1 for large text and meaningful control boundaries/focus indicators, verified during implementation rather than assumed from token names.
- Target at least 32px control height in dense desktop toolbars and 44px for primary touch interactions. Treat these as design targets, not a claim of accessibility certification.
- Avoid mandatory animation, respect reduced-motion preferences, and keep hover-only content accessible by focus or explicit activation.
- At 200% zoom, no primary action or confirmation is clipped. At 320 CSS pixels, forms reflow; canvas and wide tables have explicit viewing/scrolling controls.

## 8. Blazor implementation approach

Keep `MainLayout.razor` responsible for the shell. Introduce small shared components for page headers, status badges, field/help/error groups, searchable pickers, empty/loading states, workbench panes, and proposal review. Add scoped page styling where behavior differs; reserve `app.css` for tokens, base controls, and shared layout primitives.

Extract Play presentation in small steps: context header, action groups, review pane, and activity. Keep the existing services and action arguments intact. Do not reimplement eligibility in visual components or couple the renderer to Blazor.

Before extracting a form, inventory its bindings, conditional controls, action identifiers, test selectors, and diagnostic outputs. Preserve stable IDs where practical. Component tests should assert behavior rather than the old markup tree. Reuse `boardViewport.js` interactions where appropriate, checking mounting and disposal when panels or routes change. An expanded Play canvas must use the same perspective-safe drawing source as the existing Play view.

Separate ephemeral UI state (pane widths, filters, active inspector tab) from game state. Pane preferences may persist locally; hidden unit data and proposal authority must not. Coalesce or cancel obsolete search/preview requests and discard responses for a superseded selection or perspective.

Do not copy the supplied stylesheet wholesale: it lacks general Play form layouts and responsive breakpoints, assumes fixed chrome height, and adds active/primary selectors without corresponding markup. Introduce its useful tokens and treatments alongside the components they support.

## 9. Delivery sequence and verification

Each stage should be reviewable on its own. This document and its index entry are the only changes in the initial design commit.

| Stage | Deliverable | Completion evidence |
|---|---|---|
| 1. Foundation | Shell, active navigation, tokens, shared controls, responsive page families | Every route reachable; current group correct; keyboard and zoom inspection |
| 2. Discovery | Library search/filtering/states and lazy SVG previews | Matching, empty, loading, failure, back navigation; previews do not trigger full verification |
| 3. Workspaces | Viewer/editor controls, LOS/Evidence tabs, Maps and Unit Lab grouping | Existing edit/save/undo/redo, rule order, previews, counter tiers and comparison preserved |
| 4. Play | Context, phase task groups, map placement, review pane, activity | Representative actions from each group retain proposal, confirm, refusal, and replay behavior |
| 5. Hardening | Overflow, focus, reconnection, performance, copy and evidence review | Browser walkthrough matrix and relevant automated checks recorded |

Use representative data: empty/configuration failure, a full board library, a verified board and a failing board, a source-derived draft, a composed map with ordered SSR rules, multiple unit kinds, and live/synthetic games. Inspect at 1920x1080, 1366x768, 1024x768, and narrow/zoomed layouts. Include long names, diagnostics, and action labels.

Run targeted MapStudio component tests for changed flows and existing renderer/viewport checks when those boundaries are touched. For Play, verify visible versus adjudicator projections, proposal refusal, stale confirmation, duplicate confirmation, and record attribution. Build with existing warning policy; broaden testing when a change crosses service or rendering boundaries. Documentation-only changes require link, diff, and scope checks, not an application test run.

Record browser evidence separately from automated results. A compilation or selector check is not visual acceptance. No screen is complete while ordinary scrolling or pane collapse can make Confirm, Save, validation, or draft restrictions unreachable.

## 10. Proposed decisions and remaining validation

| Decision | Initial proposal | Validate during implementation |
|---|---|---|
| Navigation | Grouped sidebar, collapsible; current routes retained | Canvas width and route ancestry |
| Visual direction | Warm neutral surfaces and olive controls | Contrast and terrain separation |
| Fonts | Local system stack first | Optional self-hosted typography |
| Library | Compact list with optional lazy SVG previews | Render cost and browsing density |
| Play | Map-centered workspace with phase task groups | Full action inventory after pass 14 merges |
| Proposal review | Persistent pane; explicit confirmation | Stale/reconnect and withheld-information behavior |
| Mobile | Reflow and task access, not a separate gameplay product | Keyboard, zoom, drawers, long labels |

The next implementation step is stage 1 after design review. A small shell and representative library/viewer/Play layout should establish spacing and pane behavior before restyling every form. Changes to these proposed decisions belong in this document so later stages share the same rationale.
