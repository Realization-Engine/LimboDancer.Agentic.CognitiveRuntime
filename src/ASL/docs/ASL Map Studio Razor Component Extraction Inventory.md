# ASL Map Studio Razor Component Extraction Inventory

**Status:** Proposed analysis. No application components have been extracted.

**Date:** 2026-09-28

**Branch:** `UI-Redesign-01`

**Companion:** [UI Redesign 01 Design](<ASL Map Studio UI Redesign 01 Design.md>).

## 1. Findings and scope

The existing pages contain **142 candidate component boundaries**. They include shared components, page-specific task panels, and optional nested children. This is a broad inventory for selection, not a target of 142 new Razor files. A parent and its listed children are separate possible boundaries within overlapping source, not independent chunks to extract twice.

The review covers all ten routed pages, MainLayout, the application/router shells, and the seven existing content components. It examines Razor markup, bindings, callbacks, relevant state-reset and viewport lifecycle code, and existing component-test selectors. This is source analysis, not a fresh browser usability or performance measurement.

Source references are pinned to the application code at `2fa3f564056049d222f722e4b5bc925d51c3abd3`, the main baseline of this redesign branch. The original checkout contains ongoing pass 14 work; it was not modified. Reconcile that work before implementing the Close Combat and prisoner panels. Line numbers identify the original blocks; they will shift after extraction.

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

## 2. Reading the inventory

- **P1: strong candidate.** A coherent task, meaningful duplicated behavior, accessibility boundary, or lifecycle boundary. Priority is benefit, not a promise that the change is easy.
- **P2: useful second pass.** Extract with its parent or when the redesign independently places/reuses the block.
- **P3: conditional.** A sensible possible boundary, but leave inline unless reuse or complexity justifies it.

The inventory contains **93 P1**, **45 P2**, and **4 P3** candidates. Names are proposed `.razor` filenames, not existing classes. Contracts are suggested inputs and outputs, not claims that these view models already exist. Callback names describe intent; use typed `EventCallback<T>` contracts during implementation.

Each table gives source location, proposed contract, and the reason for extraction. Linked ranges are source evidence, not code changes. A single line or small range listed for a shared primitive is one concrete usage, not a recommendation to make every similarly shaped element a component.

## 3. Shared layout and interaction blocks

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

## 4. Board library

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| H01 / P1 | `AuthoredBoardList` | [Home:21-45](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L21>) | Authored listing rows and create/edit/view links. | Independent collection with an empty state; keeps draft labels together with board actions. |
| H02 / P1 | `MapSummaryList` | [Home:47-69](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L47>); [Maps:23-43](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L23>) | Map summaries, optional edit/delete callbacks, view links. | Reuse the overlapping map/name/placement listing, exposing actions only where the caller supports them. |
| H03 / P1 | `BoardScopeSummary` | [Home:71-81](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L71>); [Fidelity:103-111](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L103>) | Explicit scope/outcome counts and optional not-checked count. | Shared totals display, with no hidden assumption that library and batch totals have identical categories. |
| H04 / P2 | `LibraryReportContext` | [Home:82-92](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L82>) | Last fresh report timestamp, out-of-scope count/value; IncludeOutOfScopeChanged. | Own the report explanation and existing filter before adding search. |
| H05 / P1 | `VaslBoardTable` | [Home:93-148](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L93>) | Resolved display rows, view links; optional row-selection callback. | Move the five conditional result paths out of route markup; resolve cached versus fresh report data upstream. |
| H06 / P2 | `BoardVerificationRow` | [Home:107-145](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L107>) | One normalized board result with F1/F2, scope reason and diagnostics. | Complex repeated row merits its own tests. Render a tr, not a div inside tbody; extract with H05 if useful. |

## 5. Map composition and new-board forms

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

## 6. Board viewer and editor

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

## 7. Unit Lab

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

## 8. Game states and case reading

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

## 9. Fidelity and Settings

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

## 10. Play: context and setup

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

## 11. Play: Rally, Rout, movement and vehicles

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

## 12. Play: combat and required choices

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| C01 / P1 | `VehicleCloseCombatPanel` | [Play:755-810](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L755>) | Per-Location projected participants, next side, selected attack draft; ReviewAttack/ReviewPass. | Use one keyed Location child for each repeated block; prevent selection leaking between Locations. |
| C02 / P1 | `CloseCombatPanel` | [Play:811-940](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L811>) | Selected Location, due/status/round facts and CC draft; reviewed action callbacks. | Task root for the following independently meaningful blocks; sensitive to forthcoming pass 14 additions. |
| C03 / P2 | `CloseCombatLocationControl` | [Play:822-860](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L822>) | Due Locations, selected Location, Ambush/round status; LocationChanged/RoundChanged/ReviewAmbush. | Own Location changes and the explicit draft-reset boundary. |
| C04 / P1 | `CloseCombatStacking` | [Play:863-877](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L863>) | Authorized SMC/MMC choices and stacking map; StackingChanged. | Repeated relational editor with unit IDs, separate from attack participant selection. |
| C05 / P1 | `CloseCombatWithdrawals` | [Play:878-895](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L878>) | Authorized unit destinations and mandatory flags; WithdrawalsChanged. | Repeated per-unit choices; planner supplies destinations and must-withdraw facts. |
| C06 / P1 | `CloseCombatAttackBuilder` | [Play:900-930](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L900>) | Projected sides/participants/directors and draft; SelectionChanged/AddAttack. | Own coordinated attacker/defender/director input; do not give each checkbox rule knowledge. |
| C07 / P2 | `CloseCombatAttackQueue` | [Play:931-937](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L931>) | Declared attacks and review availability; Remove/ReviewRound. | A reviewable local queue, including explicit resolution with no attacks. |
| C08 / P1 | `PendingChoicePanel` | [Play:941-957](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L941>) | Authorized choice description/options or waiting state; ReviewChoice. | Independent blocking interaction; unauthorized viewers must not receive hidden option data. |
| C09 / P1 | `PendingSurrenderPanel` | [Play:958-972](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L958>) | Authorized surrender/Guard choices and waiting state; ReviewAccept/ReviewReject. | Repeated prompt keyed by surrender identity; retain No Quarter implications. |
| C10 / P2 | `PrisonerActionPanel` | [Play:973-982](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L973>) | Permitted unit/prisoner action rows; ReviewAction. | Current block contains massacre choices; extend only after pass 14 action inventory is reconciled. |
| C11 / P2 | `FireMarkerSummary` | [Play:983-995](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L983>); [Play:1444-1450](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1444>) | Authorized Fire Lane, Encirclement and Residual FP summaries. | One map-context summary with meaningful categories, preserving MG/operator attribution. |
| C12 / P1 | `OpportunityFireAction` | [Play:996-1011](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L996>) | Eligible units and selected IDs; ReviewOpportunityFire. | Distinct preparatory action, not another option on immediate fire. |
| C13 / P1 | `SmallArmsFirePanel` | [Play:1012-1100](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1012>) | Fire draft plus precomputed firer/weapon/director/target options; Review/Clear. | Complete task; group selection dependencies belong together. |
| C14 / P2 | `FireGroupSelector` | [Play:1022-1053](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1022>); [Play:1086-1097](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1086>) | Location, unit/weapon/MG-alone/leader/partner choices; GroupChanged. | Optional child for the most interdependent fire-selection block; preserve PruneFire behavior. |
| C15 / P2 | `FireTargetOptions` | [Play:1054-1085](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1054>) | Target/free-Location draft, phase options and selected weapons; TargetOptionsChanged. | Keeps Snap Shot, Spraying Fire and Fire Lane fields with their target semantics. |
| C16 / P1 | `OrdnanceFirePanel` | [Play:1101-1216](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1101>) | Gun/MA/LATW choices, draft, target/ammunition/spotter/director options; ReviewFire. | Independent action family; preserve gun-change and target-change resets. |
| C17 / P2 | `OrdnanceTargetFields` | [Play:1161-1206](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1161>) | Target and vehicle choices, ammunition capability/draft; TargetChanged/VehicleChanged/AmmoChanged. | Consolidate duplicated vehicle selects while retaining fixed AP/HEAT versus selectable ammunition. |
| C18 / P1 | `GunArcAction` | [Play:1217-1237](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1217>) | Selected Gun, facing and precomputed target-arc readings; FacingChanged/ReviewTurn. | A separate non-firing action with evidence; parent keeps GunTargetStatus calls. |
| C19 / P1 | `OpenEntryDeclaration` | [Play:1316-1323](<../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1316>) | Authorized pending entry attempts; ReviewElect/ReviewDecline. | Blocking Infantry OVR declaration is separate from general vehicle overrun controls. |

## 13. Play: review, activity and map

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

## 14. Further boundaries inside existing components

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

## 15. Reuse the existing components

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

## 16. State ownership and component contracts

### 16.1 Page/container responsibilities

Keep route/query handling, dependency injection for application services, data loading, projection, action proposal/confirmation, persistence, and navigation in the page or a dedicated page-level coordinator. Extracting a panel must not grant it authority to mutate a live game or call a planner from its markup.

Children should receive narrow task-specific data, not a reference to Play, a broad cascading page object, or the entire unrestricted GameState. A form may receive an editable local draft and permitted choices, but its action callback returns declared intent. The page invokes the same existing action handler or adapter.

Use coherent drafts such as RoutDraft, MovementDraft, FireDraft, and CloseCombatDraft. These are proposed UI data structures. Do not turn them into a second rules engine. When a draft is passed by reference during an incremental extraction, document its ownership and notify changes explicitly. Prefer value replacement for new contracts so parameter updates and invalidation are visible.

For example, RoutActionPanel should receive a RoutDraft, prepared obligations, authorized unit choices, and busy state. It raises DraftChanged and ReviewRequested. It does not receive LivePlay, calculate a mandatory route, or confirm a proposal.

ProposalReviewPanel receives a presentation model that has already removed withheld facts, plus confirm availability. ConfirmRequested and CancelRequested identify the displayed proposal attempt. The page checks that the attempt still matches the current proposal before executing the existing confirmation path.

### 16.2 Draft reset rules are part of extraction

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

### 16.3 Projection and disclosure

Several existing Play selectors query full `state`; extracting them does not by itself prove they are safe for every perspective. Audit the projection boundary for each new contract. Unknown/withheld is not an empty list, false, or zero.

Specific hotspots in the source:

- Play:63 builds the board link with `Perspective.Adjudicator`, even though the toolbar has a selected perspective. Treat this as a separate explicit behavior decision and test it during the redesign; do not bury a perspective change in a component refactor.
- Play:246-250 builds building-entry choices from active state units. Other task lists also consult state and planners. Pass only the disclosure appropriate to the current workflow; preserve adjudication authority on the server.
- Required choice, surrender, audit, fire-effects and sealed-presence blocks have different disclosure rules. A generic card must not receive their secret data and merely hide it with CSS.
- Changing perspective must clear unauthorized details from child state, caches, tooltips, and accessible names. A reused component instance can otherwise retain content that its new parameters no longer authorize.

These are review targets based on source shape, not a claim that an exploit or runtime leak was demonstrated.

### 16.4 Viewport ownership

BoardViewport is a high-value but higher-risk extraction. BoardViewer and Author currently own the host ElementReference, JS module/viewport references, DotNetObjectReference, event callbacks, and disposal. Move that lifecycle as one unit or leave the host inline until it can move safely.

Use child-owned lifecycle with a narrow callback/API contract for Fit, Highlight, render updates, and gesture mode. Do not leave both page and component installing listeners on the same element. Verify reconnect, route changes, tab collapse, repeated mount/unmount, comparison mode, focus, and disposing the last JS/DotNet reference.

Play currently inserts rendered SVG directly. Converting that output to the interactive viewport changes behavior and requires separate verification. It is not implied by extracting PlayMapPanel.

### 16.5 Forms, DOM, and styling

Keep the existing binding event timing unless a task explicitly changes it. Several fields use oninput, while others use onchange. Replacing all of them with one generalized control can change preview timing and draft validation.

Do not impose EditForm on every panel or create nested forms. Native buttons, inputs, labels and selects remain useful. Introduce validation contracts deliberately rather than wrapping controls so deeply that IDs, focus, and errors become difficult to inspect.

Components used inside tables must render valid table elements. A row component emits tr; it does not put a div between tbody and tr. Preserve label/for pairs and unique IDs, especially attachment prefixes and repeated vehicle towing controls.

All component, page, shared, and base CSS belongs in `wwwroot/site.css`. Do not use Blazor CSS isolation, `.razor.css` files, embedded style elements, style attributes, or JavaScript CSS injection. Use explicit component/page root classes and data attributes so selectors remain predictable across nested Razor components. The existing app.css has been migrated in full. Validate the resulting DOM after interaction as well as the source: layer visibility and comparison mode previously created style attributes from JavaScript.

## 17. Recommended extraction order

A broad inventory is useful, but the first changes should prove the contracts without restructuring everything at once.

1. **Small shared behavior and read-only panels:** PerspectivePicker, RevisionNavigator, BoardScopeSummary, BoardProvenancePanel, CounterTierPreviewGrid and ActionRecordList. These offer visible reuse with limited mutation risk.
2. **Proposal review and evidence:** ProposalReviewPanel, EntryReviewFacts and FireReviewFacts. Establish a narrow disclosed-data contract while preserving current handlers and test hooks.
3. **Representative complete forms:** RoutActionPanel, SupportWeaponActionPanel, ScenarioRuleEditor and MapPlacementEditor. Exercise draft ownership and dependent selections before applying the pattern to every task.
4. **Play task families:** Rally/repair/deploy/recombine, Infantry movement, vehicle movement, small arms fire and ordnance. Keep each family working before extracting the next.
5. **Close Combat and required choices:** Reconcile pass 14, then extract Location-specific CC, attack queues, surrender/prisoner and required-choice panels.
6. **Viewport lifecycle and layout:** Extract BoardViewport with lifecycle tests, then rearrange tasks into the proposed map-centered workspace.
7. **Optional children and refinements:** Split P2/P3 children only when the resulting parent is still difficult to understand, independently rendered, or reused. Leave small cohesive components intact.

P1 does not override dependency order. Shell/navigation and shared visual primitives can be developed with the early read-only work. Commit presentation extraction separately from action semantics or service changes so reviewers can distinguish them.

## 18. Suggested organization

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

## 19. Acceptance and evidence

An extraction is complete when the user can still perform the same task and the ownership is clearer, not merely when the page is shorter.

- Preserve existing routes, query behavior, element IDs/data attributes used by tests, labels, disabled conditions, conditional options, and displayed evidence unless the change explicitly revises them.
- Existing bUnit tests exercise IDs such as play-confirm, play-outcome, fire-facts, lab-name, game-summary and data-unit/data-event rows. Retain these hooks initially and add focused child tests for meaningful contracts.
- Verify enum/null/unknown handling in VocabularyAttributeInput, selection reset in action forms, multiple Location panels, and attachment deletion with stable keys.
- Test parent integration for proposal then confirm, cancel, refusal, stale state and idempotent replay. A child callback test cannot replace gate-path coverage.
- Verify both player and adjudicator views, including switching between them with a pending proposal or selected unit. Check the DOM and accessible text for withheld information.
- Keep the existing renderer's SVG output unchanged for presentation-only extraction. Verify counter tier switching, keyboard focus and viewport lifecycle in a browser when touched.
- Do not call planners or perform recursive filesystem enumeration from a frequently rerendered child. Prepare expensive display facts outside render loops; measure before introducing caching that might become stale.
- Run the relevant MapStudio tests and build for implementation changes. This inventory changes documentation only, so validation is limited to source-reference, link, priority/count, and diff checks.

The most useful initial outcome is a set of focused page coordinators composing understandable task panels. The inventory is intentionally broader than the first implementation pass so redesign decisions can choose boundaries with evidence rather than inventing them during markup changes.
