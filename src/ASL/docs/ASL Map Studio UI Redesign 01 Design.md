# ASL Map Studio UI Redesign 01 Design

**Status:** Consolidated design and component extraction plan. The supplied visual theme and stylesheet foundation are implemented in site.css; layout changes and component extraction remain proposed.

**Date:** 2026-09-28. **Updated:** 2026-09-29, reviewed main through `ab32b53a8740f45dc47f8d349b0dcc5076d89f81` and local main follow-up `fd1e71b`.

**Branch:** `UI-Redesign-01`, based on local `main` at `2fa3f564056049d222f722e4b5bc925d51c3abd3`.

**Purpose:** Evolve the supplied UX audit and stylesheet into an implementable design for browsing, authoring, inspecting, and playing in Map Studio.

**Parents and companions:** [Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), [Architecture and Rendering Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design.md>), [Unit Display Design](<ASL Unit Display Design.md>), [Governed Writes Design](<ASL Unit Governed Writes Design.md>), and [Unit Read Contract Design](<ASL Unit Read Contract Design.md>).

This document incorporates the former Razor Component Extraction Inventory and is the single reference for design decisions, component contracts, applied CSS values, and delivery evidence.

**Contents:** [Visual system](#4-layout-and-visual-system), [Play](#6-play-workspace), [Component architecture](#8-blazor-component-architecture), [155-candidate inventory](#9-component-extraction-inventory), [Delivery](#10-delivery-sequence), [Verification](#12-acceptance-and-verification).

## 1. Outcome and boundaries

The Studio should make the next useful task apparent while keeping the board, current selection, and result of an action understandable. The redesign combines a consistent application shell with two page families: collection pages for finding things and workspaces for manipulating or inspecting them.

The supplied `ASL Map Studio UX Audit & Redesign PDF` (one page, dated 28 Sep 2026) and `app.css` are design inputs supplied by the user. Their recommendations and the stylesheet's claim to be a drop-in replacement are evaluated here, not treated as implementation instructions. The originals remain outside the repository; this document records the decisions needed without requiring those files at build time.

The branch implementation baseline includes backlog passes 10 through 13. The design inventory now also accounts for main passes 14 through 16: Close Combat/capture, special units and nationalities, and night/weather. Section 9.15 records the reviewed commits and additional boundaries. This documentation update does not merge main into the redesign branch; reconcile the implementation baseline before extracting the newly added controls.

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

The applied theme uses locally bundled IBM Plex Sans/Mono and Barlow Condensed, distributed with their SIL Open Font License notices. System fonts remain fallbacks. The supplied Google Fonts import is replaced with local @font-face declarations; there is no runtime font service dependency. Use condensed uppercase type sparingly for page identity, not dense form labels or instructions.

### 4.3 Controls

Use one visually dominant action per active task. Play can offer multiple action families, but only the active form's Review action or pending proposal's Confirm action is dominant. Avoid promoting every submit button globally.

Buttons use minimum height and allow meaningful labels to wrap when space requires it. Disabled controls remain legible and have a visible nearby reason when the reason is actionable. Preserve focus indicators on links, buttons, inputs, tabs, and canvas controls, including disabled link-style button overrides.

Statuses combine words and an optional symbol with color. Distinguish Not checked, Checking, Verified, Failed, Out of scope, and Authored valid. Never present approximate vectorization as exact or interpret missing verification as failure.

### 4.4 Required stylesheet ownership

All Map Studio CSS lives in `wwwroot/site.css`, including tokens, base elements, shared controls, page layouts, component classes, responsive rules, and interaction states. The application loads it through `Assets["site.css"]`.

Do not use HTML/SVG `style` attributes, embedded `<style>` elements, JavaScript `.style` writes or CSS injection, Blazor CSS isolation, or `.razor.css` files. Components express presentation state through semantic classes and data attributes; the corresponding CSS is defined in site.css. Scope page/component rules with explicit root classes rather than Blazor-generated selectors.

The existing app.css has been migrated in full, so there is no second stylesheet or duplicate cascade to maintain. Although retaining app.css for basics was permitted, it is unnecessary for this foundation. The supplied redesign app.css now provides the theme rules and values in site.css. Its :root block and following rules are preserved, with local font declarations before them and the viewport state rules after them.

SVG geometry and renderer-owned presentation attributes such as viewBox, coordinates, fill, stroke, and clip-path remain part of the existing deterministic vector output. They are not CSS declarations or style attributes. Dynamic Studio layer visibility and comparison opacity are controlled by data attributes whose rules live in site.css, including the comparison slider's 0 through 100 percent values.

Acceptance includes checking rendered DOM after interactions, not just searching Razor markup: the previous viewport JavaScript created style attributes even though Razor had none.

### 4.5 Applied theme values

The following values are implemented in [site.css](<../LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css>). They describe the current baseline, while sections 4.1 and 4.3 describe further layout and control refinements. All root custom properties are listed here; site.css remains the canonical source for individual selector declarations.

| Token | Applied value |
|---|---|
| `--ink` | `#1b2025` |
| `--muted` | `#5e656c` |
| `--line` | `#dad5c8` |
| `--panel` | `#e6e2d8` |
| `--accent` | `#4a6629` |
| `--pass` | `#23592f` |
| `--fail` | `#8e2e1f` |
| `--ground` | `#f3f1ea` |
| `--surface` | `#ffffff` |
| `--surface-2` | `#faf9f5` |
| `--line-soft` | `#efece4` |
| `--control-line` | `#cfc9ba` |
| `--command` | `#1e2419` |
| `--command-ink` | `#e8e6dd` |
| `--accent-ink` | `#3f5a22` |
| `--accent-tint` | `#dce5c4` |
| `--pass-bg` | `#ddeedf` |
| `--fail-bg` | `#f6e1dc` |
| `--warn` | `#7a4f0b` |
| `--warn-bg` | `#f6ecd6` |
| `--info` | `#244f77` |
| `--info-bg` | `#dce7f2` |
| `--radius` | `6px` |
| `--font-sans` | `"IBM Plex Sans", "Segoe UI", system-ui, sans-serif` |
| `--font-mono` | `"IBM Plex Mono", ui-monospace, Consolas, monospace` |
| `--font-display` | `"Barlow Condensed", "Segoe UI", sans-serif` |
| `--chrome` | `5.5rem` |

| Treatment | Applied value |
|---|---|
| Base typography | 15px; line-height 1.45; IBM Plex Sans |
| Main page headings | Barlow Condensed, 1.6rem, weight 600, uppercase, line-height 1.2 |
| Header | 3.5rem height; dark command surface; title 1.35rem, weight 700 |
| Main spacing | 1rem 1.25rem padding |
| Buttons | 2.1rem height; .9rem text, weight 500 |
| Text controls | 2.1rem minimum height; .9rem text |
| Tables | .875rem text; .55rem .9rem cell padding; .7rem uppercase headings |
| Focus indicator | 2px solid accent outline; 2px offset |
| Viewer | minmax(0, 1fr) 22rem columns; calc(100vh - var(--chrome)) height |
| Editor | 11rem tool rail, flexible canvas, 22rem inspector |
| Unit Lab | minmax(320px, 420px) 1fr columns; 1.25rem gap |
| Viewport comparison | Data attributes select 101 opacity values, 0 through 100 percent, in site.css |

Local font weights are IBM Plex Sans 400/500/600, IBM Plex Mono 400/500, and Barlow Condensed 600/700, each with font-display: swap. [Font sources and licenses](<../LimboDancer.Domains.Asl.MapStudio/wwwroot/fonts/README.md>) record the seven bundled files and redistribution notices.

The supplied theme is an applied visual baseline, not completion of the redesign. It still uses fixed chrome and pane sizes, lacks responsive breakpoints and general Play form layouts, and includes active/primary selectors that need corresponding markup. The generic .banner is currently informational blue; a dedicated warning variant is still required for source-derived draft restrictions. Unit Lab's wide starting-template select also needs a layout correction: it extends into the preview column at the inspected desktop width. These remaining refinements belong to the staged layout and component work below.


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

Promote the existing map from below the action forms into the central workspace. Keep game, side/perspective, turn, phase, and revision visible. Show live versus historical state explicitly. Keep the current NVR, weather, precipitation and illumination summary visible when relevant, supplied by the authoritative state adapter. The selected unit or stack drives the task pane, but manual unit and Location entry remain available.

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

Separate game identity, board/map choice, sides and scenario conditions, and unit placement into named sections. Give each side its own ELR, SAN, and Friendly Board Edge labels. Preserve Sniper counter placement and scenario SSR text, including night and weather; a structured picker must round-trip existing tokens and retain unsupported-rule diagnostics. Put rule explanations in help text beneath fields, with expandable citations. Do not force experienced users through a multi-page wizard; allow moving between sections and reviewing the complete setup before proposal.

### 6.3 Phase and action organization

| Context | Task groups, reflecting currently implemented capabilities |
|---|---|
| Rally | Rally, repair, Deploy/Recombine, eligible support-weapon handling, recovery choices |
| Movement | Infantry/vehicle movement, SMOKE, movement costs, DC placement, berserk retained SW, defender response window, overrun/reaction |
| Fire phases | Small arms including FT/MOL, thrown/placed DCs, Starshell attempts, ordnance, targets and modifiers |
| Rout | Rout requirement, destination/cover, route, Low Crawl, Interdiction and surrender results |
| Advance | Unit selection, destination, movement implications |
| Close Combat | Location, participants, Ambush/withdrawal, attacks/capture, prisoner escape round, infiltration, Ambush Withdrawal, Hand-to-Hand option, vehicle sequence |

This table organizes tasks; it is not a new eligibility specification. Existing planners and phase rules remain authoritative. An action chooser can show an unavailable action with a public reason, but must not infer permission or disclose hidden state. Keep End phase separate from an active unit task so it cannot be confused with completing that task.

### 6.4 Review and confirmation

Selecting a unit, clicking a target, or editing a field only prepares an action. The UI's Review action invokes the existing proposal path. A successful proposal opens a persistent review pane with action, participants, declared destination/target, disclosed consequences, costs, and current revision. Confirm invokes the existing confirmation path. Cancel dismisses the proposal without changing game state.

Show refused, unsupported, indeterminate, stale, already committed, and committed results distinctly, preserving their actual service meaning. Never translate an unknown result into success. While planning, show a busy state and prevent duplicate submissions. Do not promise a completion percentage when none exists.

Changing action arguments, game, perspective, or revision invalidates the displayed proposal for confirmation. A stale result requires a fresh proposal and review; it must not silently confirm a revised plan. Retain existing attempt/idempotency behavior for repeated confirmation and retries. On reconnect, refresh authoritative state and reconcile any pending attempt before enabling confirmation.

The review pane must not reveal undisclosed defenders or consequences that the existing flow withholds until confirmation. Any unit search, tooltip, target list, activity entry, and accessibility label must use the appropriate projection. Clear selection and cached presentation data when perspective changes. Terrain-only LOS retains its existing availability.

### 6.5 Activity and evidence

Keep a compact latest-result summary visible. Expand activity into grouped records for movement, fire, ordnance, Close Combat, Rally/Rout/support weapons, Sniper attacks, night/weather events, and dice. Retain Fire Lane MG attribution and other explanatory facts added by recent commits. Rule citations are relevant evidence, not clutter to remove indiscriminately. Technical package identities and internal design references belong in a separate evidence disclosure.

## 7. Interaction and accessibility acceptance targets

- Every workflow can be reached and completed with a keyboard. Canvas actions have equivalent labeled controls or Location inputs.
- Searchable pickers support typing, arrow navigation, selection, Escape, and an announced result count; retain native selects until a replacement supports these behaviors.
- Tabs expose selected state and predictable keyboard navigation. Opening a drawer moves focus appropriately; closing restores focus to its opener. Persistent desktop panes do not trap focus.
- Loading and action completion are announced without repeatedly reading large logs. Validation summaries link to fields; urgent errors remain visible until addressed.
- Use proposed contrast targets of 4.5:1 for normal text and 3:1 for large text and meaningful control boundaries/focus indicators, verified during implementation rather than assumed from token names.
- Target at least 32px control height in dense desktop toolbars and 44px for primary touch interactions. Treat these as design targets, not a claim of accessibility certification.
- Avoid mandatory animation, respect reduced-motion preferences, and keep hover-only content accessible by focus or explicit activation.
- At 200% zoom, no primary action or confirmation is clipped. At 320 CSS pixels, forms reflow; canvas and wide tables have explicit viewing/scrolling controls.

## 8. Blazor component architecture

Keep `MainLayout.razor` responsible for the shell. Introduce small shared components for page headers, status badges, field/help/error groups, searchable pickers, empty/loading states, workbench panes, and proposal review. Define all styles in site.css, using explicit page and component root classes where behavior differs. Do not create component stylesheets or inline styles.

Extract Play presentation in small steps: context header, action groups, review pane, and activity. Keep the existing services and action arguments intact. Do not reimplement eligibility in visual components or couple the renderer to Blazor.

Before extracting a form, inventory its bindings, conditional controls, action identifiers, test selectors, and diagnostic outputs. Preserve stable IDs where practical. Component tests should assert behavior rather than the old markup tree. Reuse `boardViewport.js` interactions where appropriate, checking mounting and disposal when panels or routes change. An expanded Play canvas must use the same perspective-safe drawing source as the existing Play view.

Separate ephemeral UI state (pane widths, filters, active inspector tab) from game state. Pane preferences may persist locally; hidden unit data and proposal authority must not. Coalesce or cancel obsolete search/preview requests and discard responses for a superseded selection or perspective.

### 8.1 Reuse existing components

The application already has useful component boundaries:

| Existing component | Decision |
|---|---|
| `Diagnostics` | Retain its map-diagnostic grouping adapter. Improve keyboard-accessible detail disclosure; a title tooltip alone is insufficient. Compose shared FindingList/StatusBadge where useful rather than replacing its domain contract with object. |
| `ToolPalette` | Retain. It already has a tool value and ToolChanged callback. Restyle within this boundary. |
| `LayerList` | Retain. It represents authored features in paint order, not renderer layer visibility. Do not merge it with BoardLayerToggles. |
| `ValidationPanel` | Retain its report and finding-selection contract; share findings presentation underneath. |
| `HexInspector` | Retain as a task-level component. Extract grid evidence or feature lists only when tabs/reuse need them. The small Locations and Hexsides tables can remain inline initially. |
| `FeatureProperties` | Retain. Its replacement-feature callback is a good domain boundary. Share TerrainPicker; do not split every conditional property input. |
| `UnitEditor` | Retain as the public vocabulary-driven editor. VocabularyAttributeInput is the strongest internal extraction; face/identity/state children are subsequent choices. |

Do not add wrappers around App's document structure, Routes' router, a single heading, every status-bearing span, every table cell, or an empty toolbar. The empty toolbar at Play:1238-1239 is a cleanup candidate, not a component. CSS classes are sufficient for repeated visual treatment that has no behavioral or semantic contract.

### 8.2 Page/container responsibilities

Keep route/query handling, dependency injection for application services, data loading, projection, action proposal/confirmation, persistence, and navigation in the page or a dedicated page-level coordinator. Extracting a panel must not grant it authority to mutate a live game or call a planner from its markup.

Children should receive narrow task-specific data, not a reference to Play, a broad cascading page object, or the entire unrestricted GameState. A form may receive an editable local draft and permitted choices, but its action callback returns declared intent. The page invokes the same existing action handler or adapter.

Use coherent drafts such as RoutDraft, MovementDraft, FireDraft, and CloseCombatDraft. These are proposed UI data structures. Do not turn them into a second rules engine. When a draft is passed by reference during an incremental extraction, document its ownership and notify changes explicitly. Prefer value replacement for new contracts so parameter updates and invalidation are visible.

For example, RoutActionPanel should receive a RoutDraft, prepared obligations, authorized unit choices, and busy state. It raises DraftChanged and ReviewRequested. It does not receive LivePlay, calculate a mandatory route, or confirm a proposal.

ProposalReviewPanel receives a presentation model that has already removed withheld facts, plus confirm availability. ConfirmRequested and CancelRequested identify the displayed proposal attempt. The page checks that the attempt still matches the current proposal before executing the existing confirmation path.

### 8.3 Draft reset rules are part of extraction

Existing code contains important dependent changes and reset helpers. Preserve or deliberately improve them with a focused test:

| Trigger | Coupled state to preserve |
|---|---|
| Game, phase or perspective change | Selection, eligible choices, current proposal and any stale form values; re-run the appropriate reset/pruning path |
| Manual boards text changes | Clear the saved-map choice |
| Selected support weapon changes | Clear receiving/recovering unit |
| Rally unit changes | Clear rally leader |
| Deployment squad changes | Clear leader and SW assignment |
| First recombining HS changes | Clear partner and leader |
| Fire origin changes | Apply PruneFire; remove invalid firer, weapon and director selections |
| Ordnance weapon changes | Reset ammunition default, spotter and director |
| Ordnance target changes | Clear selected target vehicle |
| Close Combat Location changes | Clear attacks/round-related draft and restore default stacking as existing helpers require |
| Repeated attachment or placement removed | Preserve remaining objects' identity; do not transfer draft/focus to the next index |

Pass view context keys such as game, revision, perspective, Location, and stable object ID explicitly where state can survive rerenders. Use `@key` for attachment editors, placement rows, repeated Location panels, and event cards. Do not key on list index or regenerate keys on every render.

### 8.4 Projection and disclosure

Several existing Play selectors query full `state`; extracting them does not by itself prove they are safe for every perspective. Audit the projection boundary for each new contract. Unknown/withheld is not an empty list, false, or zero.

Specific hotspots in the source:

- Play:63 builds the board link with `Perspective.Adjudicator`, even though the toolbar has a selected perspective. Treat this as a separate explicit behavior decision and test it during the redesign; do not bury a perspective change in a component refactor.
- Play:246-250 builds building-entry choices from active state units. Other task lists also consult state and planners. Pass only the disclosure appropriate to the current workflow; preserve adjudication authority on the server.
- Required choice, surrender, audit, fire-effects and sealed-presence blocks have different disclosure rules. A generic card must not receive their secret data and merely hide it with CSS.
- Changing perspective must clear unauthorized details from child state, caches, tooltips, and accessible names. A reused component instance can otherwise retain content that its new parameters no longer authorize.

These are review targets based on source shape, not a claim that an exploit or runtime leak was demonstrated.

### 8.5 Viewport ownership

BoardViewport is a high-value but higher-risk extraction. BoardViewer and Author currently own the host ElementReference, JS module/viewport references, DotNetObjectReference, event callbacks, and disposal. Move that lifecycle as one unit or leave the host inline until it can move safely.

Use child-owned lifecycle with a narrow callback/API contract for Fit, Highlight, render updates, and gesture mode. Do not leave both page and component installing listeners on the same element. Verify reconnect, route changes, tab collapse, repeated mount/unmount, comparison mode, focus, and disposing the last JS/DotNet reference.

Play currently inserts rendered SVG directly. Converting that output to the interactive viewport changes behavior and requires separate verification. It is not implied by extracting PlayMapPanel.

### 8.6 Forms, DOM, and styling

Keep the existing binding event timing unless a task explicitly changes it. Several fields use oninput, while others use onchange. Replacing all of them with one generalized control can change preview timing and draft validation.

Do not impose EditForm on every panel or create nested forms. Native buttons, inputs, labels and selects remain useful. Introduce validation contracts deliberately rather than wrapping controls so deeply that IDs, focus, and errors become difficult to inspect.

Components used inside tables must render valid table elements. A row component emits tr; it does not put a div between tbody and tr. Preserve label/for pairs and unique IDs, especially attachment prefixes and repeated vehicle towing controls.

Apply the stylesheet ownership and rendered-DOM checks in section 4.4 to every extraction. Use explicit component/page root classes and data attributes across nested Razor components.

### 8.7 Suggested organization

Keep domain-related names close together without introducing a new project:

```text
Components/
  Layout/       StudioNavigation, PageHeader, InspectorTabs
  Shared/       OperationFeedback, StatusBadge, FindingList, FieldGroup, JsonDisclosure
  Board/        existing components, BoardViewport, viewer/editor controls, provenance
  Maps/         MapPlacementEditor, ScenarioRuleEditor, PlacementTextEditor
  Units/        UnitEditor and its children, Lab previews/style/placement panels
  Games/        PerspectivePicker, RevisionNavigator, replay tables, ReadCase panels
  Play/         context/setup, action families, proposal review, activity, map/table panels
  Fidelity/     runner controls, report picker/metadata/results, differences
  Pages/        route-aware composition and orchestration
```

These folders express ownership, not a requirement to create every directory immediately. Promote a component to Shared only after its semantics fit multiple domains. A generic SearchablePicker is a redesign addition, not an existing extracted HTML block in this inventory; use it later beneath domain pickers after keyboard and accessibility behavior is defined. Likewise, new thumbnails and a new phase-action chooser should not inflate the extraction count.

## 9. Component extraction inventory

### 9.1 Findings and source baseline

The existing pages contain **155 candidate component boundaries**. They include shared components, page-specific task panels, and optional nested children. This is a broad inventory for selection, not a target of 155 new Razor files. A parent and its listed children are separate possible boundaries within overlapping source, not independent chunks to extract twice.

The review covers all ten routed pages, MainLayout, the application/router shells, and the seven existing content components. It examines Razor markup, bindings, callbacks, relevant state-reset and viewport lifecycle code, and existing component-test selectors. This is source analysis, not a fresh browser usability or performance measurement.

Sections 9.3 through 9.14 retain their source references at `2fa3f564056049d222f722e4b5bc925d51c3abd3`, the implementation baseline of this redesign branch. Section 9.15 adds commit-pinned main references and contract amendments that supersede the older descriptions where noted. Local source links intentionally address the branch baseline; new main blocks use immutable GitHub links because those blocks are not yet in this checkout. The original checkout and its uncommitted files were not modified. Line numbers will shift after integration/extraction.

Original branch baseline measurements:

| Page | Total source lines | Lines before @code |
|---|---:|---:|
| Author | 591 | 152 |
| BoardViewer | 759 | 309 |
| Fidelity | 309 | 217 |
| Games | 342 | 179 |
| Home | 215 | 150 |
| Maps | 321 | 123 |
| NewBoard | 159 | 61 |
| Play | 3,755 | 1,532 |
| Settings | 47 | 31 |
| UnitLab | 478 | 227 |
| **Total** | **6,976** | **2,981** |

The pre-`@code` count includes directives, whitespace and Razor control flow, not just HTML. Play accounts for roughly 54% of page source and 51% of these markup-region lines. Its state and service coupling matter more than line count: moving markup alone will not resolve that coupling.

The best extraction units are complete tasks or representations: a rout form, an ordered placement editor, a proposal review, a counter preview matrix, or a provenance panel. Shared controls should follow actual duplicated behavior, not visual similarity alone.

### 9.2 Priorities and contracts

- **P1: strong candidate.** A coherent task, meaningful duplicated behavior, accessibility boundary, or lifecycle boundary. Priority is benefit, not a promise that the change is easy.
- **P2: useful second pass.** Extract with its parent or when the redesign independently places/reuses the block.
- **P3: conditional.** A sensible possible boundary, but leave inline unless reuse or complexity justifies it.

The inventory contains **102 P1**, **49 P2**, and **4 P3** candidates. Names are proposed `.razor` filenames, not existing classes. Contracts are suggested inputs and outputs, not claims that these view models already exist. Callback names describe intent; use typed `EventCallback<T>` contracts during implementation.

Each table gives source location, proposed contract, and the reason for extraction. Linked ranges are source evidence, not code changes. A single line or small range listed for a shared primitive is one concrete usage, not a recommendation to make every similarly shaped element a component.

### 9.3 Shared layout and interaction blocks

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| S01 / P1 | `StudioNavigation` | [Layout/MainLayout:3-14](<../LimboDancer.Domains.Asl.MapStudio/Components/Layout/MainLayout.razor#L3>) | Destination definitions, current route, collapsed state; Navigate/Collapse callbacks. | Own grouped navigation, active-route behavior, and keyboard access in one place. |
| S02 / P1 | `PageHeader` | [Maps:10-15](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L10>); [Games:11-15](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L11>); [UnitLab:12-16](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L12>); [Play:19-23](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L19>) | Title, description and action slots; no service access. | Repeated title/intro blocks become a consistent heading hierarchy without embedding page logic. |
| S03 / P1 | `SourceRequiredNotice` | [Home:10-18](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L10>); [Maps:17-20](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L17>); [NewBoard:10-13](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L10>) | Missing prerequisites, explanation, settings link, optional setup instructions. | Unify configuration failures while retaining page-specific requirements. |
| S04 / P1 | `OperationFeedback` | [Maps:106-121](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L106>); [NewBoard:53-59](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L53>); [UnitLab:131-137](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L131>); [Author:46-49](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L46>) | Busy state, severity, message and optional diagnostics; retry callback if offered. | Consistent busy, failure and completion announcements; never infer success from an empty message. |
| S05 / P1 | `StatusBadge` | [Home:118-121](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L118>); [BoardViewer:27-28](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L27>); [Fidelity:170-175](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L170>) | Explicit label, tone, optional explanation and symbol; no callbacks. | Reuse presentation while each caller maps its domain statuses, including unknown and not checked. |
| S06 / P1 | `FindingList` | [UnitLab:111-129](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L111>); [Games:53-62](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L53>); [Maps:118-121](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L118>); [Board/ValidationPanel:21-31](<../LimboDancer.Domains.Asl.MapStudio/Components/Board/ValidationPanel.razor#L21>) | Normalized display items with category/code/message/severity, optional selection callback. | Share accessible findings presentation; keep map, unit, plausibility and style diagnostic adapters separate. |
| S07 / P1 | `FieldGroup` | [Play:100-123](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L100>); [Maps:46-49](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L46>); [NewBoard:16-46](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L16>) | Label, input ID, help/error text and child control slot. | Keep label, help and error associations together; preserve native input binding and validation. |
| S08 / P2 | `RuleHelp` | [Play:263-270](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L263>); [Play:303-309](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L303>); [Play:605-614](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L605>) | Short help, detailed explanation, citations; expansion state only. | Long rule paragraphs can move out of the task flow without losing actionable explanations. |
| S09 / P1 | `PerspectivePicker` | [Games:35-43](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L35>); [BoardViewer:113-118](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L113>); [Play:54-62](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L54>) | Allowed perspective choices, value, ID; ValueChanged. | A genuinely repeated domain control. Parent performs reprojection and proposal invalidation. |
| S10 / P1 | `RevisionNavigator` | [Games:44-46](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L44>); [BoardViewer:119-121](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L119>) | Current/min/max revision, busy state, optional slider; RevisionChanged. | Share boundary handling and accessible previous/next controls while parents own history loading. |
| S11 / P2 | `LocationField` | [Play:200-201](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L200>); [Play:541-543](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L541>); [BoardViewer:222-228](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L222>); [Games:137-138](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L137>) | Raw text, label, examples, error, optional selected-location action; TextChanged/UseSelection. | A reusable canonical Location entry, distinct from the optional holder field or bypass-route syntax beside it. |
| S12 / P2 | `UnitSelectionList` | [Play:537-540](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L537>); [Play:738-741](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L738>); [Play:1005-1008](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1005>); [Play:1032-1039](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1032>) | Projected choice rows, selected IDs, per-row disabled reason, ID/class hooks; SelectionChanged. | Repeated checkbox lists gain consistent labels and selection behavior. Caller supplies eligibility. |
| S13 / P2 | `FacingPicker` | [Units/UnitEditor:22-32](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L22>); [Units/UnitEditor:48-58](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L48>); [Play:205-213](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L205>); [Play:1220-1228](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1220>) | Hexspine choices, nullable value and null label, control ID; ValueChanged. | Share facing controls without conflating hexspines with the different Hexside vocabulary. |
| S14 / P2 | `JsonDisclosure` | [UnitLab:138-141](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L138>); [Play:1524-1529](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1524>) | Title and already-authorized text; expansion state. | Share expandable code presentation. Adjudicator authorization stays outside this generic component. |

### 9.4 Board library

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| H01 / P1 | `AuthoredBoardList` | [Home:21-45](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L21>) | Authored listing rows and create/edit/view links. | Independent collection with an empty state; keeps draft labels together with board actions. |
| H02 / P1 | `MapSummaryList` | [Home:47-69](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L47>); [Maps:23-43](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L23>) | Map summaries, optional edit/delete callbacks, view links. | Reuse the overlapping map/name/placement listing, exposing actions only where the caller supports them. |
| H03 / P1 | `BoardScopeSummary` | [Home:71-81](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L71>); [Fidelity:103-111](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L103>) | Explicit scope/outcome counts and optional not-checked count. | Shared totals display, with no hidden assumption that library and batch totals have identical categories. |
| H04 / P2 | `LibraryReportContext` | [Home:82-92](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L82>) | Last fresh report timestamp, out-of-scope count/value; IncludeOutOfScopeChanged. | Own the report explanation and existing filter before adding search. |
| H05 / P1 | `VaslBoardTable` | [Home:93-148](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L93>) | Resolved display rows, view links; optional row-selection callback. | Move the five conditional result paths out of route markup; resolve cached versus fresh report data upstream. |
| H06 / P2 | `BoardVerificationRow` | [Home:107-145](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L107>) | One normalized board result with F1/F2, scope reason and diagnostics. | Complex repeated row merits its own tests. Render a tr, not a div inside tbody; extract with H05 if useful. |

### 9.5 Map composition and new-board forms

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| M01 / P1 | `MapPlacementEditor` | [Maps:51-75](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L51>) | Placement drafts, board choices, bounds; Add/Change/Remove callbacks. | Own editable placement table and add action; page retains map building and persistence. |
| M02 / P2 | `MapPlacementRow` | [Maps:58-71](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L58>) | Stable row key, selected board/column/row/reversal; RowChanged/Remove. | Repeated multi-field row has identity and validation. Key by a stable draft ID, not row index. |
| M03 / P1 | `ScenarioRuleEditor` | [Maps:77-99](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L77>) | Ordered selected rules, catalog choices and descriptions; Add/Remove callbacks. | One coherent rule-selection task that can gain search while preserving rule order. |
| M04 / P1 | `PlacementTextEditor` | [Maps:101-104](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L101>) | Draft text, help and parse feedback; TextChanged/ApplyText. | Separates advanced compact syntax from ordinary placement editing and preserves apply semantics. |
| M05 / P2 | `MapBuildActions` | [Maps:106-121](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L106>) | Working state, result/diagnostics; Check/SaveAndView. | A page-specific command group composed with OperationFeedback, not a new map service. |
| M06 / P1 | `NewBoardForm` | [NewBoard:16-59](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L16>) | Creation draft, board choices, busy/result state; DraftChanged/Create. | A complete creation task with conditional blank/vectorized modes; page owns slug validation and creation initially. |
| M07 / P3 | `BoardDimensionsFields` | [NewBoard:29-35](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L29>) | Width/height and their distinct bounds; DimensionsChanged. | Optional child if dimensions become reusable or per-field validation grows; otherwise keep in NewBoardForm. |
| M08 / P1 | `SourceDraftNotice` | [NewBoard:48-51](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L48>); [Author:21-27](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L21>) | Context-specific draft restriction text and evidence links. | Shared warning semantics; never downgrade the authoring restriction to an informational notice. |

### 9.6 Board viewer and editor

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| B01 / P1 | `BoardViewerToolbar` | [BoardViewer:26-137](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L26>) | Title/status, supported views, navigation links, toolbar child slots. | A composition boundary for the existing crowded toolbar, not one component with all game/editor state. |
| B02 / P1 | `BoardViewPicker` | [BoardViewer:33-47](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L33>); [Author:36-43](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L36>) | Supported BoardView choices and value; ViewChanged. | Shared capability-aware view selection; editor does not automatically gain Comparison. |
| B03 / P1 | `BoardComparisonControls` | [BoardViewer:56-71](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L56>) | Mode, opacity/divider value; ModeChanged/ValueChanged. | Separate tightly coupled comparison mode and slider from unrelated controls. |
| B04 / P1 | `BoardLayerToggles` | [BoardViewer:72-85](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L72>) | Available layers, visibility set, optional derivation trace; LayerChanged/TraceChanged. | Group rendering toggles. This differs from existing LayerList, which selects authored features. |
| B05 / P1 | `UnitOverlayControls` | [BoardViewer:86-130](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L86>) | Show state, game/set choices, sheet names, projected perspective/revision; typed change callbacks. | Keep synthetic placement sets and games distinct while composing shared perspective/revision controls. |
| B06 / P1 | `BoardViewport` | [BoardViewer:139-140](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L139>); [Author:108-111](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L108>) | Render source/options and interaction mode; HexHovered/HexSelected/UnitSelected/GestureCompleted; limited Fit/Highlight commands. | Own ElementReference, JS initialization, callbacks and disposal together. HTML-only extraction would leave lifecycle split. |
| B07 / P1 | `SelectedUnitInspector` | [BoardViewer:143-198](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L143>) | Projected unit display details, allowed game facts and source summary. | Unit identity and game facts form a complete inspector; never accept unrestricted state for convenience. |
| B08 / P2 | `UnitGameFacts` | [BoardViewer:154-193](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L154>) | Projected unit/equipment/presence facts with explicit withheld state. | Optional child of SelectedUnitInspector; centralizes custody, containment and withheld rendering. |
| B09 / P1 | `HexUnitStackList` | [BoardViewer:205-214](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L205>) | Hex label and projected stack members with drawn/not-drawn flag; SelectUnit. | Repeated selectable stack entries belong together; preserve the beyond-six explanation. |
| B10 / P1 | `LosPanel` | [BoardViewer:218-236](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L218>); [Play:1468-1489](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1468>) | Source/target draft, busy/result; Check/Clear; optional selected-hex and visible-unit slots. | Substantial overlap, with capability-specific children for auxiliary vertex and Play unit selection. |
| B11 / P2 | `LosResult` | [BoardViewer:232-235](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L232>); [Play:1485-1488](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1485>) | Explicit answered/blocked/refused status, summary and optional details. | Share result semantics; clear LOS and unanswered LOS must never look identical. |
| B12 / P1 | `BoardProvenancePanel` | [BoardViewer:258-306](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L258>) | Read-only provenance, composition, fidelity, validation and renderer metadata. | Independent evidence panel suitable for a tab without exposing the entire BoardViewer. |
| B13 / P1 | `BoardEditorHeader` | [Author:28-50](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L28>) | Name/reference/version/status, dirty/undo/redo state; Rename/Undo/Redo/Save; view controls slot. | Cohesive document commands and save feedback, separate from drawing tools. |
| B14 / P1 | `EditorToolOptions` | [Author:55-106](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L55>) | Tool-specific option draft and catalog choices; OptionsChanged. | Move the terrain/elevation/linear/building/annotation switch into one focused component, not five tiny controls. |
| B15 / P1 | `InspectorTabs` | [Author:115-147](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L115>) | Tab IDs/titles/counts, selected ID and panel fragments; SelectedChanged. | Own tab behavior and focus while retaining existing FeatureProperties, LayerList, ValidationPanel and HexInspector. |

### 9.7 Unit Lab

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| U01 / P1 | `UnitTemplatePicker` | [UnitLab:20-50](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L20>) | Synthetic examples, catalog definitions grouped by publication, selection; SelectedChanged. | Keep the grouped picker and catalog provenance notice together, preserving catalog-versus-Lab semantics. |
| U02 / P1 | `AttachedEquipmentEditor` | [UnitLab:54-67](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L54>) | Attachment drafts, vocabulary and stable IDs; Add/Remove/AttachmentChanged. | Composes existing UnitEditor; fixes ownership and identity for repeated attachment forms. |
| U03 / P1 | `CounterTierPreviewGrid` | [UnitLab:71-88](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L71>) | Labeled rows of trusted rendered SVG for far/mid/near; refusal state. | Independent comparison view; rendering service and source validation remain outside. |
| U04 / P2 | `CounterFacePreviewGallery` | [UnitLab:90-96](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L90>) | Sheet label and labeled face/perspective SVG previews. | Distinct axis from detail-tier comparison; reuse a small SVG preview tile only if needed. |
| U05 / P2 | `UnitAccessibleDetails` | [UnitLab:98-108](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L98>); [BoardViewer:145-153](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L145>) | Accessible name, detail pairs and optional Location. | Reuse read-only unit labeling presentation, preserving exact accessible-name content. |
| U06 / P1 | `UnitLabFindings` | [UnitLab:111-129](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L111>) | Validation, plausibility and style findings; no callbacks unless navigation added. | Domain adapter around FindingList, preserving categories and explicit No findings. |
| U07 / P2 | `UnitDocumentOutput` | [UnitLab:131-141](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L131>) | Can-save state, JSON and save feedback; Save. | Document output is a complete task. Composes JsonDisclosure and OperationFeedback. |
| U08 / P1 | `UnitStyleSheetEditor` | [UnitLab:143-179](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L143>) | Sheet/palette choices, raw text, parsed status, diagnostics and save-name draft; Select/Edit/SaveAs. | A substantial independent editor; preserve raw invalid text and existing oninput parsing timing. |
| U09 / P1 | `UnitPlacementSetEditor` | [UnitLab:181-224](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L181>) | Board/hex/level/set draft, placed units, save result; Place/Load/Save/Remove. | A complete synthetic placement workflow; must never call live-game writes. |
| U10 / P2 | `PlacedUnitList` | [UnitLab:209-220](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L209>) | Projected display rows with stable IDs/Location/name; Remove. | Optional child reusable in read-only previews; do not confuse synthetic rows with live setup commands. |

### 9.8 Game states and case reading

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| G01 / P1 | `GameReplayToolbar` | [Games:23-49](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L23>) | Game choices, perspective/revision state and board target; ChangeGame/ShowOnBoard. | Compose shared PerspectivePicker and RevisionNavigator, with parent-owned replay loading. |
| G02 / P2 | `GameReadFindings` | [Games:51-63](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L51>) | Diagnostics and positions-checked evidence. | Read-only result adapter; successful position validation is evidence, not just another warning. |
| G03 / P1 | `GameContextSummary` | [Games:67-71](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L67>); [Play:175-179](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L175>) | Revision, turn, phase, phasing side, perspective, synthetic/setup labels. | Shared context strip with explicit optional fields; do not pretend Play supports revision scrubbing here. |
| G04 / P1 | `ProjectedGameUnitTable` | [Games:73-102](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L73>) | Visible unit and sealed-presence row models, trusted preview SVG. | Own display-only table semantics; do not merge with Play's different movement/action table prematurely. |
| G05 / P2 | `GameEquipmentTable` | [Games:104-120](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L104>) | Projected equipment/entity rows and formatted custody/Location. | Independent non-unit inventory with a different schema from personnel. |
| G06 / P1 | `ReadCaseForm` | [Games:122-140](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L122>) | Projected attacker choices, Location and expected revision draft; Read. | Separate request preparation from result inspection; parent owns ReadCase service call. |
| G07 / P1 | `ReadCaseResult` | [Games:141-168](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L141>) | Already-authorized status/reason and optional snapshot display model. | Cohesive evidence table, including incomplete occupancy and nondefinitive results. |
| G08 / P2 | `PerspectiveEventList` | [Games:170-176](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L170>) | Only entitled event rows and selected revision. | Safe, read-only timeline with revision numbering; no raw event-log injection. |

### 9.9 Fidelity and Settings

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| F01 / P1 | `FidelityRunControls` | [Fidelity:14-42](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L14>) | Availability, IncludeF3, runner state/progress/error; Start/Cancel/IncludeF3Changed. | Own the batch lifecycle display; page retains runner subscription and disposal initially. |
| F02 / P1 | `LosFidelityPanel` | [Fidelity:44-75](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L44>) | Fixture availability/count, running/progress and results; Run. | Separate LOS verification job from board fidelity without duplicating service orchestration. |
| F03 / P2 | `LosFidelityResultsTable` | [Fidelity:55-70](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L55>) | Fixture result rows with unsupported counts and disagreements. | Optional child of LosFidelityPanel for independent sorting/details later. |
| F04 / P1 | `FidelityReportPicker` | [Fidelity:77-99](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L77>) | Saved report summaries, selection and download URL; SelectedChanged. | One task with empty state and download, not a generic select wrapper. |
| F05 / P1 | `FidelityReportMetadata` | [Fidelity:112-131](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L112>) | Timestamps, source IDs, versions, timings and diagnostics. | Evidence presentation independent of job controls and filtering. |
| F06 / P1 | `FidelityResultsTable` | [Fidelity:133-215](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L133>) | Board result rows and filter; FilterChanged. | Own filter/table composition. Service lookups and report selection stay above it. |
| F07 / P2 | `FidelityDifferences` | [Fidelity:172-194](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L172>) | F2 status, differences, diagnostics, display limit and report link. | Extract the nested disclosure, preserving truncation and the full-report escape path. |
| F08 / P2 | `FidelityCheckBadges` | [Fidelity:195-200](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L195>) | Named checks with passed/gating/detail fields. | Differentiate failed gating checks from informational F3 results; compose accessible badges. |
| F09 / P3 | `StudioConfigurationReport` | [Settings:10-30](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Settings.razor#L10>) | Resolved paths, catalog/version metadata and precomputed cache size. | Low urgency on a small read-only page; filesystem enumeration should not become a child's render-time job. |

### 9.10 Play: context and setup

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| P01 / P1 | `ScriptedDicePanel` | [Play:25-39](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L25>) | Queue values, input/error and enabled flag; Enqueue/Clear. | Isolate development-only controls and their conspicuous test-data warning. |
| P02 / P1 | `LiveGameToolbar` | [Play:41-65](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L41>) | Game choices, selected game, permitted perspectives and board URL; GameChanged/PerspectiveChanged. | Shared context boundary; explicitly review the existing adjudicator board link rather than silently preserving or changing it. |
| P03 / P1 | `NewGameForm` | [Play:67-156](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L67>) | New-game draft and choice catalogs; DraftChanged. | One setup root with the following field-group children; parent retains proposal construction. |
| P04 / P2 | `GameMapChoice` | [Play:73-85](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L73>) | Board placement text, saved-map choices and selection; MapChoiceChanged. | Enforce mutual exclusivity in one place: typing board text clears saved-map selection. |
| P05 / P1 | `ScenarioSidesFields` | [Play:86-123](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L86>) | Two side drafts, ELR and edge choices; SidesChanged. | Reorganize interleaved side labels into two coherent groups without losing null/default distinctions. |
| P06 / P2 | `ScenarioConditionsFields` | [Play:124-154](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L124>) | Month/year/defender draft and side choices; ConditionsChanged. | Scenario conditions and their help form a reusable setup section. |
| P07 / P1 | `GameReplayFailure` | [Play:157-172](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L157>) | Filtered replay diagnostics and catalog-availability explanation. | Independent recovery/error block; preserves the five-item limit and missing-catalog explanation. |
| P08 / P1 | `SetupPlacementEditor` | [Play:184-224](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L184>) | Definition choices, placement draft and applicable options; DraftChanged/Add. | Complete placement task with Gun/vehicle facing, Bore Sight, concealment and holder alternatives. |
| P09 / P1 | `SetupPlacementList` | [Play:225-234](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L225>) | Pending setup placements and busy state; Remove/ReviewSetup. | Reviewable setup queue; stable placement keys and explicit proposal action. |

### 9.11 Play: Rally, Rout, movement and vehicles

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| A01 / P1 | `BuildingEntryAction` | [Play:243-259](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L243>) | Authorized unit choices, destination and help; DraftChanged/ReviewEntry. | Small complete action; retain withheld-information explanation. |
| A02 / P1 | `RoutActionPanel` | [Play:260-299](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L260>) | Rout obligations/advice, eligible choices and route/LowCrawl draft; ReviewRout. | Own coherent rout task; planners compute obligations upstream. |
| A03 / P2 | `RoutObligations` | [Play:271-279](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L271>) | Precomputed unit/side/requirement/cover/routed rows. | Optional child separates advice from route editing and removes repeated planner work from markup. |
| A04 / P1 | `SupportWeaponActionPanel` | [Play:300-336](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L300>) | Weapon/receiver choices, selection and action availability; ReviewTransfer/Drop/Recover/Dismantle. | Keep dependent weapon/receiver selection together; changing weapon resets receiver. |
| A05 / P1 | `RallyActionPanel` | [Play:339-368](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L339>) | Broken-unit and leader choices, rally draft; DraftChanged/Review. | One action with self-rally option; changing unit resets leader. |
| A06 / P1 | `RepairActionPanel` | [Play:369-387](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L369>) | Eligible SW/AAMG choices and selection; ReviewRepair. | Separate repair from rally while preserving the different weapon and vehicle cases. |
| A07 / P1 | `DeployActionPanel` | [Play:388-425](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L388>) | Squad, leader and SW choices/draft; ReviewDeploy. | Selection dependencies and second-HS equipment assignment form one task. |
| A08 / P1 | `RecombineActionPanel` | [Play:426-458](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L426>) | First HS, compatible partner and leaders; ReviewRecombine. | Keep resets of partner/leader with first-HS changes. |
| A09 / P2 | `DmRetentionChoices` | [Play:459-470](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L459>) | Eligible projected units and selected IDs; SelectionChanged. | Phase-end choices, not a standalone committed action. Parent includes them in phase advancement. |
| A10 / P1 | `ShockRecoveryAction` | [Play:471-490](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L471>) | Shocked/UK vehicle choices and selection; ReviewRecovery. | Independent required recovery step with clear phase-end consequence. |
| A11 / P1 | `MovementStatus` | [Play:500-519](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L500>) | Movement window, movers, remaining members, bypass/minimum/end facts. | A persistent status strip shared by movement-related tasks; no service calls. |
| A12 / P2 | `BerserkChargeNotices` | [Play:520-535](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L520>) | Authorized charge target/next-step/undecided advice. | Dedicated obligation list; distinguish undecided from a mandatory known route. |
| A13 / P1 | `InfantryMovementAction` | [Play:536-550](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L536>) | Move draft, projected candidates, pushable-Gun facts; ReviewMove. | Cohesive movement fields; compose UnitSelectionList and LocationField. |
| A14 / P1 | `SmokeGrenadeAction` | [Play:551-565](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L551>) | Eligible placers, target draft and move-selection context; ReviewSmoke. | Separate action currently embedded inside the movement toolbar; retain its moveUnits dependency. |
| A15 / P2 | `MovementWindowActions` | [Play:566-567](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L566>) | Defender-window/end availability, selected ending members; ReviewPass/ReviewEndMove. | Two related phase-window commands whose labels and enabled states must remain synchronized. |
| A16 / P1 | `ReactionFireAction` | [Play:569-601](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L569>) | Reaction Location/vehicle, authorized attackers/SMCs, draft; ReviewReaction. | A complete defender response, distinct from the ordinary Close Combat phase. |
| A17 / P1 | `VehicleMovementPanel` | [Play:602-713](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L602>) | Selected vehicle, movement options/status and draft; typed review callbacks. | Large task root. Build the following children only as needed; do not pass the whole Play page. |
| A18 / P2 | `VehicleMovementStatus` | [Play:626-634](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L626>); [Play:707-710](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L707>) | VCA, MP spent/remaining, movement/bog/reverse/bypass/recall display facts. | Complex state explanation deserves independent presentation tests. |
| A19 / P2 | `VehicleStepChoices` | [Play:635-666](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L635>); [Play:685-690](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L685>) | Precomputed legal/blocked steps with costs/reasons, movement-option draft; StepRequested. | Group start/turn/entry/stop/exit/OVR/ESB choices; parent serializes existing action arguments. |
| A20 / P1 | `VehicleTowingActions` | [Play:667-684](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L667>) | Towable Gun choices, current towing state and facing; ReviewHook/ReviewUnhook. | A distinct attachment operation; unique IDs for repeated unhook-facing controls. |
| A21 / P1 | `BoundingFireAction` | [Play:692-706](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L692>) | Projected target choices, selected vehicle/target; ReviewBoundingFire. | Fire from vehicle movement is a separate task with its own eligibility context. |
| A22 / P1 | `CrewExposureActions` | [Play:715-729](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L715>) | Eligible vehicles and current CE/BU state; ReviewExposureChange. | Independent action group shared across Movement and Advance contexts. |
| A23 / P1 | `AdvanceActionPanel` | [Play:730-754](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L730>) | Advance candidates, selected IDs and destinations; ReviewAdvance. | Separate movement-phase stepping from Advance rules; share presentation controls only. |

### 9.12 Play: combat and required choices

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| C01 / P1 | `VehicleCloseCombatPanel` | [Play:755-810](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L755>) | Per-Location projected participants, next side, selected attack draft; ReviewAttack/ReviewPass. | Use one keyed Location child for each repeated block; prevent selection leaking between Locations. |
| C02 / P1 | `CloseCombatPanel` | [Play:811-940](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L811>) | Selected Location, due/status/round facts and CC draft; reviewed action callbacks. | Task root for the following independently meaningful blocks; include the pass 14 extensions in section 9.15. |
| C03 / P2 | `CloseCombatLocationControl` | [Play:822-860](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L822>) | Due Locations, selected Location, Ambush/round status; LocationChanged/RoundChanged/ReviewAmbush. | Own Location changes and the explicit draft-reset boundary. |
| C04 / P1 | `CloseCombatStacking` | [Play:863-877](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L863>) | Authorized SMC/MMC choices and stacking map; StackingChanged. | Repeated relational editor with unit IDs, separate from attack participant selection. |
| C05 / P1 | `CloseCombatWithdrawals` | [Play:878-895](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L878>) | Authorized unit destinations and mandatory flags; WithdrawalsChanged. | Repeated per-unit choices; planner supplies destinations and must-withdraw facts. |
| C06 / P1 | `CloseCombatAttackBuilder` | [Play:900-930](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L900>) | Projected sides/participants/directors and draft; SelectionChanged/AddAttack. | Own coordinated attacker/defender/director input; do not give each checkbox rule knowledge. |
| C07 / P2 | `CloseCombatAttackQueue` | [Play:931-937](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L931>) | Declared attacks and review availability; Remove/ReviewRound. | A reviewable local queue, including explicit resolution with no attacks. |
| C08 / P1 | `PendingChoicePanel` | [Play:941-957](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L941>) | Authorized choice description/options or waiting state; ReviewChoice. | Independent blocking interaction; unauthorized viewers must not receive hidden option data. |
| C09 / P1 | `PendingSurrenderPanel` | [Play:958-972](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L958>) | Authorized surrender/Guard choices and waiting state; ReviewAccept/ReviewReject. | Repeated prompt keyed by surrender identity; retain No Quarter implications. |
| C10 / P2 | `PrisonerActionPanel` | [Play:973-982](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L973>) | Permitted unit/prisoner action rows; ReviewAction. | Baseline block contains massacre choices; keep that contract distinct from N09 prisoner custody in section 9.15. |
| C11 / P2 | `FireMarkerSummary` | [Play:983-995](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L983>); [Play:1444-1450](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1444>) | Authorized Fire Lane, Encirclement and Residual FP summaries. | One map-context summary with meaningful categories, preserving MG/operator attribution. |
| C12 / P1 | `OpportunityFireAction` | [Play:996-1011](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L996>) | Eligible units and selected IDs; ReviewOpportunityFire. | Distinct preparatory action, not another option on immediate fire. |
| C13 / P1 | `SmallArmsFirePanel` | [Play:1012-1100](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1012>) | Fire draft plus precomputed firer/weapon/director/target options; Review/Clear. | Complete task; group selection dependencies belong together. |
| C14 / P2 | `FireGroupSelector` | [Play:1022-1053](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1022>); [Play:1086-1097](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1086>) | Location, unit/weapon/MG-alone/leader/partner choices; GroupChanged. | Optional child for the most interdependent fire-selection block; preserve PruneFire behavior. |
| C15 / P2 | `FireTargetOptions` | [Play:1054-1085](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1054>) | Target/free-Location draft, phase options and selected weapons; TargetOptionsChanged. | Keeps Snap Shot, Spraying Fire and Fire Lane fields with their target semantics. |
| C16 / P1 | `OrdnanceFirePanel` | [Play:1101-1216](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1101>) | Gun/MA/LATW choices, draft, target/ammunition/spotter/director options; ReviewFire. | Independent action family; preserve gun-change and target-change resets. |
| C17 / P2 | `OrdnanceTargetFields` | [Play:1161-1206](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1161>) | Target and vehicle choices, ammunition capability/draft; TargetChanged/VehicleChanged/AmmoChanged. | Consolidate duplicated vehicle selects while retaining fixed AP/HEAT versus selectable ammunition. |
| C18 / P1 | `GunArcAction` | [Play:1217-1237](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1217>) | Selected Gun, facing and precomputed target-arc readings; FacingChanged/ReviewTurn. | A separate non-firing action with evidence; parent keeps GunTargetStatus calls. |
| C19 / P1 | `OpenEntryDeclaration` | [Play:1316-1323](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1316>) | Authorized pending entry attempts; ReviewElect/ReviewDecline. | Blocking Infantry OVR declaration is separate from general vehicle overrun controls. |

### 9.13 Play: review, activity and map

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| R01 / P1 | `ProposalReviewPanel` | [Play:1244-1312](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1244>) | Already-disclosed proposal summary, reasons, optional evidence, can-confirm/busy state; Confirm/Cancel. | Highest-value extraction for consistent confirmation. Parent owns attempt ID, revision, withholding and execution. |
| R02 / P1 | `EntryReviewFacts` | [Play:1257-1272](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1257>) | Disclosed tri-state facts and conclusion/provenance. | Independent evidence block; unknown stays distinct from false. |
| R03 / P1 | `FireReviewFacts` | [Play:1273-1305](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1273>) | Authorized attack facts including range, LOS, levels, terrain and scenario month. | Preserves recent pass 10 details in a testable evidence table. |
| R04 / P1 | `DiceRollHistory` | [Play:1325-1344](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1325>) | Entitled rolls/subjects and scripted-mode notice. | Distinct record type with roll IDs, provenance and test-data warning. |
| R05 / P1 | `ActionRecordList` | [Play:1346-1355](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1346>); [Play:1421-1441](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1421>) | Heading, list ID and already-formatted event ID/kind/text rows. | One reusable list for ordnance, Close Combat and rally records; do not build three identical components. |
| R06 / P1 | `FireHistory` | [Play:1357-1419](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1357>) | Authorized fire records; no callbacks required initially. | Specialized records are richer than ActionRecordList; compose the following children. |
| R07 / P1 | `FireResolutionCard` | [Play:1363-1416](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1363>) | One projected fire record with explicit withheld effects. | Natural repeated boundary keyed by event ID; keep withheld state explicit. |
| R08 / P2 | `FireArithmeticBreakdown` | [Play:1367-1379](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1367>) | Authorized firer arithmetic, multipliers, column shifts, dice and DRM. | Independently verifiable explanation; presentation must not recalculate the adjudication. |
| R09 / P2 | `InfantryFireEffectsTable` | [Play:1380-1395](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1380>) | Disclosed target effects, definition changes and checks. | A coherent nested table; null/withheld effects must not be treated as an empty success. |
| R10 / P2 | `VehicleFireEffectsTable` | [Play:1396-1411](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1396>) | Disclosed vehicle-hit, vehicle-line and crew results. | Different data schema from Infantry effects warrants its own component. |
| R11 / P1 | `PlayMapPanel` | [Play:1443-1466](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1443>) | Perspective-safe drawing SVG/source/problems and current selection; LocationSelected if supported. | Own drawing success/error presentation. Reusing interactive BoardViewport is later behavior work, not a markup-only extraction. |
| R12 / P1 | `PlayUnitTable` | [Play:1491-1523](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1491>) | Projected visible-unit/sealed rows, formatted costs/conditions; Locate. | Separate live movement-aware table; never pass full state just to compute a row label. |
| R13 / P2 | `AdjudicatorAuditPanel` | [Play:1524-1529](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1524>) | Explicitly authorized audit lines; no service access in generic child. | Authorization wrapper around JsonDisclosure; hiding with CSS does not protect the audit. |

### 9.14 Further boundaries inside existing components

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| E01 / P1 | `VocabularyAttributeInput` | [Units/UnitEditor:175-188](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L175>) | Attribute definition, raw string value, ID; ValueChanged. | Extract the existing RenderFragment switch into a testable input component, preserving enum blanks and number/list parsing semantics. |
| E02 / P2 | `UnitIdentityFields` | [Units/UnitEditor:4-73](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L4>) | Identity draft, vocabulary choices and attached/concealed capabilities; IdentityChanged. | Focused identity/orientation group; use FacingPicker while keeping Hexside distinct. |
| E03 / P2 | `UnitFaceEditor` | [Units/UnitEditor:82-105](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L82>) | Face label, accepted attribute/trait definitions and values, stable prefix; ValuesChanged/TraitsChanged. | Repeated face fieldset is a natural child, keyed by face. |
| E04 / P2 | `UnitAttributeFields` | [Units/UnitEditor:86-93](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L86>); [Units/UnitEditor:111-119](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L111>) | Ordered accepted definitions and value access/change adapter. | Reuse face/unit attribute layout around VocabularyAttributeInput; scope stays explicit. |
| E05 / P3 | `UnitStateChecks` | [Units/UnitEditor:122-134](<../LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L122>) | State definitions, selected names and prefix; SelectionChanged. | Optional semantic group for independent state validation; otherwise leave in UnitEditor. |
| E06 / P2 | `HexGridSamples` | [Board/HexInspector:53-72](<../LimboDancer.Domains.Asl.MapStudio/Components/Board/HexInspector.razor#L53>) | Prepared sample label/pixel/terrain/elevation/off-grid rows. | Move dense evidence table to the Evidence tab without making the child read or derive the grid. |
| E07 / P3 | `HexFeatureList` | [Board/HexInspector:74-90](<../LimboDancer.Domains.Asl.MapStudio/Components/Board/HexInspector.razor#L74>) | Covering feature display rows; SelectFeature. | Optional only if feature evidence is reused outside HexInspector. |
| E08 / P2 | `TerrainPicker` | [Board/FeatureProperties:12-21](<../LimboDancer.Domains.Asl.MapStudio/Components/Board/FeatureProperties.razor#L12>); [Author:59-68](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L59>) | Already-filtered terrain codes/names, selected code, ID; CodeChanged. | A genuinely shared terrain selection; feature-kind filtering remains with the authoring adapter. |

### 9.15 Main review: passes 14 through 16 (2026-09-29)

The reviewed remote main is `ab32b53a8740f45dc47f8d349b0dcc5076d89f81`. Local main additionally contains `fd1e71b`, a Docker merge-check script and plan update with no Studio markup changes. This review inspected commit diffs, current Play markup and action/reset helpers, the pass review reports, and the added MapStudio tests. It did not execute their tests or repeat their browser sessions.

| Commits | Changes relevant to this redesign | Component response |
|---|---|---|
| `6cf7ea1`, merged by `5e58210`; review record `2fd2c24` | CC capture/Guard/yield, prisoner escape and custody, infiltration, Ambush Withdrawal and Hand-to-Hand | Extend C02-C09; add N05-N09; retain explicit proposal and disclosure boundaries |
| `0af4dde`, merged by `91a18a4`; review record `ec7bf80` | SAN/Sniper placement/history, FT/MOL, thrown/placed DCs, berserk retained SW; expanded nationality/catalog rules | Extend P05/P08, A13, C13/C14 and R05/R06; add N03/N04/N10-N12 |
| `eec0739`, merged by `ab32b53`; review record `68ca5e2` | Setup SSR text, night/weather summary, Starshell actions and event history | Extend P06 and R05; add N01/N02/N13 |
| `e2ac914` | Scenario cards moved to pass 17 | No scenario-card component is justified by current markup |
| `fd1e71b` (local main only) | Linux/Docker merge-check script and plan maintenance | No new UI boundary |

Only Play.razor changed among Studio pages/components: 493 lines added and 20 removed. It now has 4,228 lines, including 1,740 before @code, up 473 and 208 respectively. Across the ten pages this is 7,449 total lines and 3,189 before @code. Play now accounts for approximately 57% and 55%. Other pages may display richer vocabulary/catalog data, but this does not justify nationality-specific editor components or duplicating UnitEditor's vocabulary-driven controls.

#### Additional candidate boundaries

These 13 additions contain nine P1 and four P2 candidates. Optional children overlap their named parents in the same way as the original inventory. No new component files are implemented by this review.

| ID / priority | Proposed component | Existing source block on reviewed main | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| N01 / P1 | `ScenarioSpecialRulesField` | [Play:137-144](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L137-L144) | SSR draft text, help and validation; RulesChanged. | Child of P06. Keep exact tokens and advanced text round-trip; the server validates combinations. Do not conflate game SSRs with Maps terrain rules. |
| N02 / P1 | `NightWeatherSummary` | [Play:189-192](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L189-L192) | Prepared NVR/weather/precipitation/illumination text; no service access. | Persistent context independent of the action pane. Distinguish no applicable conditions from withheld facts. |
| N03 / P1 | `PlaceDemolitionChargeAction` | [Play:580-594](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L580-L594) | Mover IDs, permitted charge/holder choices, target draft and busy state; DraftChanged/ReviewPlace. | Complete movement subtask. The page retains GameActions.Move and its placeDc/placeDcAt arguments, not a new independent mutation. |
| N04 / P2 | `BerserkRetainedWeaponsField` | [Play:595-598](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L595-L598) | Authorized berserk-mover context and retained SW draft; KeepChanged. | Optional A13 child for the conditional Keep SW field; preserve comma-separated IDs and server portage validation. |
| N05 / P2 | `CloseCombatRoundOptions` | [Play:896-905](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L896-L905) | Prisoner-round selection, Hand-to-Hand capability/value; RoundChanged/HandToHandChanged. | Optional C03 child. Round changes reset declarations; Hand-to-Hand is capability-gated, not a global preference. |
| N06 / P1 | `AmbushWithdrawalActions` | [Play:907-921](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L907-L921) | Authorized ambusher units and precomputed destinations; ReviewWithdraw(unit, destination). | A separate immediate proposal from C05 withdrawal declarations; preserve before-round/closed-round timing. |
| N07 / P2 | `CaptureAttemptFields` | [Play:988-1003](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L988-L1003) | Capture flag, attacker-derived Guard choices and ordered defender yield draft; DraftChanged. | Optional C06 child; capture belongs to a queued attack and must not execute when toggled. |
| N08 / P1 | `CloseCombatInfiltrationChoices` | [Play:1005-1028](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1005-L1028) | Authorized unit destinations, selected infiltration map; InfiltrationsChanged. | Distinct conditional destination declarations; planner supplies eligibility and destinations, page submits them with the round. |
| N09 / P1 | `PrisonerCustodyActions` | [Play:1072-1086](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1072-L1086) | Projected Guards, permitted recipients, transfer/abandon availability; ReviewTransfer/ReviewAbandon. | Rally/Advance custody operations differ from C10 massacre actions and C09 pending surrender; preserve explicit abandonment intent. |
| N10 / P2 | `MolFirerChoice` | [Play:1167-1179](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1167-L1179) | SSR-enabled state, selected firer IDs and optional MOL user; FirerChanged. | Optional C14 child. Preserve none, SSR gating and the selected-firer dependency; do not duplicate the fire planner. |
| N11 / P1 | `ThrowDemolitionChargeAction` | [Play:1226-1246](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1226-L1246) | Projected holder/charge pairs, target draft and busy state; DraftChanged/ReviewThrow. | Independent fire task. Use a typed pair in a new contract; the page adapts the current holder|charge encoding and two-Location result. |
| N12 / P1 | `PlacedChargeDetonationActions` | [Play:1277-1287](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1277-L1287) | Authorized operable charge rows, disclosed target/concealment/CX text; ReviewDetonate(chargeId). | AFPh task with phase-end implications; buttons propose rather than detonate directly. |
| N13 / P1 | `StarshellActionPanel` | [Play:1247-1276](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1247-L1276) | Eligible projected firers, method/target draft and busy state; DraftChanged/ReviewStarshell. | Separate night task: own-hex, at-target and three-hexes have different target requirements; preserve server timing, LOS and NVR checks. |

#### Amendments to existing candidates

These amendments expand existing boundaries and do not increase the count.

| Candidate | Revised contract and preservation requirement |
|---|---|
| P03/P05, NewGameForm / ScenarioSidesFields | Each side draft now includes optional SAN alongside ELR and edge; preserve unset versus zero and the relation to a placed Sniper counter. Keep both side labels explicitly associated with their fields. |
| P06, ScenarioConditionsFields | Compose N01 with month/year/defender inputs. Expose rule validation without turning the component into a rules engine. |
| P08, SetupPlacementEditor | Include the per-side Sniper choice beside Dummy choices. Preserve the special Sniper placement/serialization path rather than requiring a catalog unit definition. |
| A13, InfantryMovementAction | Include N03 and optional N04 with shared mover context, Assault Movement and Double Time. Review place versus ordinary move through their existing handlers. |
| C02/C03, CloseCombatPanel / CloseCombatLocationControl | Include prisoner-round and Hand-to-Hand options, infiltration declarations and separate Ambush Withdrawal. A Location or round change is an explicit draft boundary. |
| C06/C07, CloseCombatAttackBuilder / CloseCombatAttackQueue | DeclaredAttack now includes Capture, ordered Yield and optional Guard. Show capture versus attack, preserve yield order, and carry these values into review; adding an attack resets its fields. |
| C09, PendingSurrenderPanel | Add ReviewFree as Unarmed beside accept/reject. Preserve the existing service meaning and availability; it is not equivalent to rejecting surrender with No Quarter. |
| C10, PrisonerActionPanel | Retain the baseline massacre-specific actions. N09 separately owns transfer/abandon controls; a shared visual wrapper must not merge their command semantics. |
| C13/C14, SmallArmsFirePanel / FireGroupSelector | Include FT among weapon choices and optional MOL user. Parent serialization preserves a FT holder's withoutInherent treatment and validates the MOL user against selected firers. |
| R05, ActionRecordList | Reuse for NightRecords and SniperRecords, retaining play-night/night-record and play-snipers/sniper-record hooks and data-event IDs. No separate near-identical history components are needed. |
| R06/R07, FireHistory / FireResolutionCard | Preserve DC identity, user and mode, including the thrown charge's attack at its own Location; retain FT/MOL and low-visibility arithmetic supplied by existing records. |
| R05 Close Combat adapter | Keep capture/Guard, freed-as-Unarmed, escape NTC, rearming, infiltration, concealment loss and Hand-to-Hand evidence in formatted records. Generic list rendering must not discard these facts. |

New reusable history blocks: [Play:1618-1638](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1618-L1638). State and formatting evidence: [Play:2159-2199](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2159-L2199), [Play:2261-2289](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2261-L2289), [Play:2918-3037](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2918-L3037).

#### State, disclosure and verification additions

- Adding a CC attack resets capture, Guard and yield along with participants/director. ClearCloseCombat also clears infiltration and Hand-to-Hand. ChooseRound currently clears participants/director/queued attacks, but not every new CC field; specify and test which fields should survive rather than assuming all resets are identical.
- Revalidate charge/holder choices when movers, game, perspective or revision change. Revalidate the retained SW list, MOL user and Starshell firer/target when their dependent selections change. These are extraction requirements, not a claim that the present page already resets every field.
- Keep IsVisibleTo filtering in the NightRecords/SniperRecords adapters. Night/weather summaries currently read state, and custody/detonation choices also inspect state; audit their presentation contracts for each perspective before passing data to children. Concealed target flags and illumination locations require the same scrutiny as ordinary fire facts. These are review targets, not demonstrated leaks.
- Preserve the existing capture, two-Location thrown-DC, and night/Starshell page-test flows. Extend extraction coverage to freeing surrender, transferring/abandoning prisoners, capture/yield queue reset, prisoner rounds, infiltration and Ambush Withdrawal, FT/MOL dependencies, SAN/Sniper setup, and all Starshell methods. Check refused/stale/confirmed outcomes through the parent gate path.
- Keep the pass reports' known rule limitations distinct from UI capabilities. This design does not promise additional night routing, Gunflash, terrain/weather, or Sniper adjudication beyond the server's implemented scope. Scenario-card workflow remains later work.

[Pass 14 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2014%20Review%202026-09-28.md); [Pass 15 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2015%20Review%202026-09-29.md); [Pass 16 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2016%20Review%202026-09-29.md).

## 10. Delivery sequence

The stylesheet foundation and supplied theme are implemented. Shell/navigation, responsive layouts, task panels, and the candidate component extractions remain to be delivered. Each stage should be reviewable on its own.

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

### 10.1 Extraction order within the stages

A broad inventory is useful, but the first changes should prove the contracts without restructuring everything at once.

1. **Small shared behavior and read-only panels:** PerspectivePicker, RevisionNavigator, BoardScopeSummary, BoardProvenancePanel, CounterTierPreviewGrid and ActionRecordList. These offer visible reuse with limited mutation risk.
2. **Proposal review and evidence:** ProposalReviewPanel, EntryReviewFacts and FireReviewFacts. Establish a narrow disclosed-data contract while preserving current handlers and test hooks.
3. **Representative complete forms:** RoutActionPanel, SupportWeaponActionPanel, ScenarioRuleEditor and MapPlacementEditor. Exercise draft ownership and dependent selections before applying the pattern to every task.
4. **Play task families:** Rally/repair/deploy/recombine, Infantry movement including DC placement and retained SW, vehicle movement, small arms including FT/MOL, thrown/placed DC actions, Starshells and ordnance. Keep each family working before extracting the next.
5. **Close Combat and required choices:** Integrate the reviewed main baseline, then extract Location-specific CC, capture/escape/infiltration, attack queues, surrender, prisoner custody and required-choice panels. Preserve the distinct review handlers.
6. **Viewport lifecycle and layout:** Extract BoardViewport with lifecycle tests, then rearrange tasks into the proposed map-centered workspace.
7. **Optional children and refinements:** Split P2/P3 children only when the resulting parent is still difficult to understand, independently rendered, or reused. Leave small cohesive components intact.

P1 does not override dependency order. Shell/navigation and shared visual primitives can be developed with the early read-only work. Commit presentation extraction separately from action semantics or service changes so reviewers can distinguish them.

## 11. Decisions and remaining validation

| Decision | Initial proposal | Validate during implementation |
|---|---|---|
| Navigation | Grouped sidebar, collapsible; current routes retained | Canvas width and route ancestry |
| Visual direction | Warm neutral surfaces and olive controls | Contrast and terrain separation |
| Fonts | Bundled IBM Plex Sans/Mono and Barlow Condensed, with system fallbacks | Long labels, readability and loading |
| Library | Compact list with optional lazy SVG previews | Render cost and browsing density |
| Play | Map-centered workspace with phase task groups | Passes 14-16 contracts and disclosure; reconcile main before implementation |
| Proposal review | Persistent pane; explicit confirmation | Stale/reconnect and withheld-information behavior |
| Mobile | Reflow and task access, not a separate gameplay product | Keyboard, zoom, drawers, long labels |

Continue stage 1 with shell and layout work using the applied theme. A small shell and representative library/viewer/Play layout should establish spacing and pane behavior before restyling every form. Changes to these proposed decisions belong in this document so later stages share the same rationale.

## 12. Acceptance and verification

### 12.1 Component extraction acceptance

An extraction is complete when the user can still perform the same task and the ownership is clearer, not merely when the page is shorter.

- Preserve existing routes, query behavior, element IDs/data attributes used by tests, labels, disabled conditions, conditional options, and displayed evidence unless the change explicitly revises them.
- Existing bUnit tests exercise IDs such as play-confirm, play-outcome, fire-facts, lab-name, game-summary and data-unit/data-event rows. Retain these hooks initially and add focused child tests for meaningful contracts.
- Verify enum/null/unknown handling in VocabularyAttributeInput, selection reset in action forms, multiple Location panels, and attachment deletion with stable keys.
- Test parent integration for proposal then confirm, cancel, refusal, stale state and idempotent replay. A child callback test cannot replace gate-path coverage.
- Verify both player and adjudicator views, including switching between them with a pending proposal or selected unit. Check the DOM and accessible text for withheld information.
- Keep the existing renderer's SVG output unchanged for presentation-only extraction. Verify counter tier switching, keyboard focus and viewport lifecycle in a browser when touched.
- Do not call planners or perform recursive filesystem enumeration from a frequently rerendered child. Prepare expensive display facts outside render loops; measure before introducing caching that might become stale.
- Run the relevant MapStudio tests and build for implementation changes. For inventory-only edits, validate source references, links, priorities/counts, and the diff.

The most useful initial outcome is a set of focused page coordinators composing understandable task panels. The inventory is intentionally broader than the first implementation pass so redesign decisions can choose boundaries with evidence rather than inventing them during markup changes.

### 12.2 Completed stylesheet foundation checks

The existing stylesheet was moved from app.css to site.css, retaining its original rules. App.razor now loads site.css through the static asset map. The viewport uses data attributes for layer visibility and comparison state; all display, visibility, and opacity declarations live in site.css.

Validation: Release build succeeded with the existing warning policy; 23 targeted MapStudio render-endpoint, map-page, and component tests passed; all five existing viewport pointer/tier tests passed. A headless Edge check using the real viewport module and stylesheet verified all 101 opacity values, layer hiding/showing across reload, side-by-side reload, swipe clipping, mode transitions, and disposal, with zero style attributes or embedded style elements in the resulting DOM.

Initial page tests exposed local Git CRLF conversion of checksum-pinned JSON resources in the isolated checkout. Restoring their exact committed bytes resolved those failures; no rule-package content or checksum was changed.

### 12.3 Supplied theme and consolidated document checks

The theme build succeeded. A headless Edge smoke check at 1366x900 reached nine routes (library, Maps, Unit Lab, Play, Settings, New board, Fidelity, Game states, and board viewer). All used site.css, the expected warm background and dark header, and 15px base text, with no horizontal page overflow or inline styles in those sampled states. All seven local font faces loaded, with zero external requests. Library and Unit Lab screenshots were inspected; the Unit Lab select overlap is recorded in section 4.5. The viewer was sampled in its loading state, so this does not establish loaded-board visual acceptance or completion of every workflow.

The viewport browser check was repeated against the supplied theme: all 101 opacity values, layer visibility/reload, side-by-side/reload, swipe and mode transitions, and disposal passed, with no style attributes or embedded styles.

The supplied :root block and following theme rules are preserved exactly after newline normalization. The initial consolidation retained all 142 baseline candidates and their source references; the subsequent main review adds 13 candidates (section 9.15), for 155 total. Responsive, interactive, populated-game, and accessibility acceptance remains subject to the delivery matrix above.

### 12.4 Main-review document verification

Checked the 2026-09-29 additions against the reviewed Git source, including their line ranges, total counts and priorities. The inventory now has 155 unique candidate IDs: 102 P1, 49 P2 and four P3. Existing source links remain at the branch baseline; new references pin main at ab32b53. CSS values and the site.css-only rule are unchanged. This is a documentation update, so no application build, test run or fresh visual acceptance is claimed.
