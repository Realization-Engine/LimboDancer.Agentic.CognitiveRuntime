# ASL Acronym Search Design

Date: 2026-10-26

Status: Implemented and locally verified. See the implementation plan's handoff record for checks and limitations.

Branch: `codex/asl-acronym-search`

Related document: [ASL Acronym Search Analysis](ASL%20Acronym%20Search%20Analysis.md).

Execution sequence: [ASL Acronym Search Implementation Plan](ASL%20Acronym%20Search%20Implementation%20Plan.md). The plan refines component/file granularity to avoid unnecessary abstractions while preserving the behavior specified here.

## 1. Scope and required conventions

Implement a read-only **Acronym Search** page at `/acronyms` in `LimboDancer.Domains.Asl.MapStudio`, backed by both the [acronym scan](ASL%20Rulebook%20Acronym%20Scan.md) and [search ledger](ASL%20Acronym%20Expanded-Term%20Search%20Ledger.csv). Provide a reusable Blazor search component for later use elsewhere in the ASL app.

The following requirements govern implementation:

1. Create separate tests for this feature in `src/ASL/tests/ASL.Acronyms.Tests`. All feature test classes use the exact namespace `ASL.Acronyms`. Keep these tests separate from existing MapStudio test classes.
2. Extend the existing `LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css` and follow the current UI design. Its opening convention requires classes/data attributes and prohibits inline styles and Blazor CSS isolation. Do not introduce a separate theme, CSS framework, font set, or `.razor.css` files.
3. Preserve reviewed definitions, source qualifications, exact-case entry identities, and the distinction between definition citations and discovery hits.
4. **Standing testing rule: run only relevant tests.** Run the dedicated feature tests and narrowly selected existing tests for directly affected shared behavior. Do not run solution-wide or unrelated suites. Broaden checks only for a concrete changed dependency or observed failure, and record the reason.

This design supersedes the analysis document's initial suggestion to add feature tests to the existing MapStudio test project. The analysis remains the record of source findings; this document defines the implementation decisions.

Full PDF search, a PDF viewer, editing definitions, automatic acronym expansion, and external search infrastructure are outside this implementation.

## 2. Architecture and ownership

```text
Scan Markdown + ledger CSV
             |
      Catalog builder and validator
             |
      Deterministic catalog JSON
             |
      Embedded app resource
             |
      Immutable catalog + stateless search service
             |
      AcronymSearchPanel
             |
      /acronyms page, existing layout and navigation
```

| Location | Responsibility |
|---|---|
| `code-builders/asl-acronyms/` | C# console tool to generate or verify the catalog, with parsing and reconciliation logic independent of its command entry point. |
| `src/ASL/LimboDancer.Domains.Asl.MapStudio/Data/acronyms.catalog.json` | Generated, committed catalog. Embed explicitly through the app project file. |
| MapStudio `Services/Acronyms/` | Catalog contracts, resource loading, search and URL-state contracts. |
| MapStudio `Components/Acronyms/` | Reusable search panel, filter controls, result list, entry details, and evidence disclosure. |
| MapStudio `Components/Pages/AcronymSearch.razor` | Route, page header, and browser URL coordination. |
| `src/ASL/tests/ASL.Acronyms.Tests/` | Dedicated importer, search, state, component, and feature integration tests. |

Use the current .NET 10 target, nullable-reference settings, warnings-as-errors, and centrally managed test packages. The builder references the catalog contract in MapStudio initially; it has no runtime role in the deployed app. Because `code-builders` does not inherit `src/ASL/Directory.Build.props`, explicitly import those settings in the tool project and commit its package lock file. Avoid creating another production library solely for these types. If a second consumer later needs the catalog contracts, extract them then.

There is no automatic generation target in the app build. To allow the builder to compile when the generated file is absent, condition the app's embedded-resource item on file existence. The runtime loader reports unavailable if it is missing, while catalog verification and the resource test must fail before delivery. Generate the file explicitly, then rebuild the app to embed the new bytes. Never use an app binary built before generation as proof of the new catalog's contents.

Register the catalog and stateless search service as singletons in `Program.cs`. Store no query, selection, paging, or debounce state in those services. The app already renders routes through `InteractiveServer`; no additional search API or render-mode boundary is needed.

## 3. Catalog generation contract

The builder accepts explicit input/output paths and supports generate and verify modes. Generate writes a replacement only after complete validation succeeds. Verify regenerates in memory and compares canonical output to the committed JSON; it never rewrites sources or the catalog. Report failures with source file, row/section, entry ID when known, and the mismatched field.

### Source parsing and identity

- Parse all 14 CSV columns with a CSV-aware reader, including quoted commas, escaped quotes, and embedded newlines. Parse the expanded query map using `System.Text.Json` with duplicate-key detection.
- Validate the column names rather than their position. Split recorded short-form lists on semicolons and trim their outer whitespace; do not split on slash or remove case. Preserve the original expanded/context search text and use the JSON map's exact keys for per-query page evidence. Do not derive queries by splitting prose definitions.
- Read only the Markdown definition tables: four columns in the alphabetical section and three columns in the equipment section. Preserve the separate guidance sections. Use a constrained table parser that handles escaped pipes and inline code, and rejects unrecognized table shapes rather than silently skipping rows.
- Match category plus exact display form to ledger candidates. Reconcile definition and citation fields to select one ledger identity. Case-sensitive comparison is mandatory.
- Preserve `equipment:CA:2` and `equipment:PC:2`. Do not derive permanent IDs from row order or overwrite entries sharing a display form.
- Main rows compare definition, reference trail, and definition source independently. Equipment rows compare their single source column to both ledger citation fields.
- Permit only presentation normalization during reconciliation: outer whitespace, line endings, and equivalent inline-code wrappers. Preserve significant text, punctuation, accents, and case.
- Fail on missing rows, repeated IDs, ambiguous matches, unmatched rows, or mismatched fields. Do not repair reviewed documents automatically.

Parse physical-page range strings into distinct sorted integers and keep the original strings for display/provenance. Validate ascending ranges and bounds against the edition's page count. Empty hit sets and queries with no hits are valid. Do not infer definition pages from discovery hits. Do not require every historical query to equal a current definition or synonym.

### Runtime contracts

| Type | Required content |
|---|---|
| `AcronymCatalogDocument` | Schema version, canonical source-text hashes, original PDF metadata, guidance blocks, entries. |
| `AcronymEntry` | Exact ID, category, display form, definition, reference trail, definition source, short-form candidates, search evidence, review record, source locator. |
| `AcronymSearchEvidence` | Original query strings, per-query page arrays, short-form page set, expanded/context page set, original page-range text. |
| `AcronymReview` | Original disposition, basis, changes, and review date. |
| `AcronymCatalogState` | Available catalog or an explicit unavailable result suitable for the page and server diagnostics. |

Use immutable records/collections or defensive copies. Lookup dictionaries use `StringComparer.Ordinal`. Keep original citation text even when extracting rule tokens. Do not parse every note, chart, or shorthand reference into a fabricated structured citation.

Canonical output uses UTF-8 without BOM, LF line endings, fixed property ordering, ordinal entry-ID ordering, sorted per-query map keys, and sorted page sets. Decode each text source as UTF-8, remove an optional BOM, and normalize CRLF/CR to LF before parsing and hashing its UTF-8 bytes. Label these hashes as canonical text hashes; do not describe them as raw-file hashes. Normalize embedded text newlines too. Keep the original PDF SHA-256 as the raw binary hash reported by the scan; this task does not independently reverify the PDF.

Record the baseline source revision and diagnostics in a separate build report rather than using the working branch HEAD in the catalog. Equivalent LF/CRLF source files must produce identical catalog bytes even after unrelated commits. Verify mode compares generated bytes to the committed JSON after normalizing only its optional BOM and line endings, accommodating Git checkout conversion without hiding substantive changes.

The baseline fixture is 685 entries: 438 main and 247 equipment. Treat this as the current regression fixture, not a permanent capacity limit. Update it deliberately when source content changes.

CI runs the builder in verify mode and the dedicated tests for relevant changes. The current `.github/workflows/asl-authoring-ci.yml` runs a broad test matrix for `src/ASL/**`; merely adding a new job would not satisfy the standing testing rule. Apply narrow change-based job selection for acronym-only changes, including document-only changes, while retaining existing behavior for mixed/unrelated changes. Include the builder, generated data, and relevant build/dependency files in feature triggers. Do not redesign unrelated CI jobs.

Publish embeds the catalog, so deployed lookup requires neither repository paths nor the original PDF. On missing, malformed, or unsupported catalog data, expose an unavailable state and log the diagnostic; do not show zero matches as though loading succeeded.

## 4. Search service contract

`Search(AcronymSearchRequest)` returns an `AcronymSearchResponse` containing total matches, normalized paging information, and the current page of hits. Each hit carries its entry identity, rank bucket, and match reason. `FindById` uses exact-case identity independently of search filters.

Request defaults:

| Setting | Default and behavior |
|---|---|
| Query | Empty; browse the catalog. Trim input and normalize Unicode to NFC for matching. Preserve the original strings. |
| Category | All; alternatives are main or equipment. |
| Match acronym case | Off. When on, acronym/form comparisons are ordinal case-sensitive. Prose comparisons remain case-insensitive. |
| Include audit search terms | Off. When on, include expanded/context queries as the lowest-ranked retrieval source. |
| Page | 1, with 25 entries per page. Clamp to a valid page after filtering. For zero hits return page 1 and no entries. |

Apply these rank buckets in order:

1. Exact case-sensitive display form or recorded short form.
2. Exact case-insensitive display form or recorded short form, when case matching permits it.
3. Acronym/form prefix, preferring matching case.
4. Contiguous definition phrase or exact citation token.
5. All query words found across definition and reference text.
6. Partial acronym/form match.
7. Audit query text, when enabled.

A hit uses its strongest qualifying bucket. Within equivalent ranks, sort by display form ordinal-ignore-case, then exact ID ordinal. Empty browsing uses that same display ordering. Deduplicate by exact ID. Return reasons such as `Exact acronym`, `Definition`, `Reference`, and `Audit search term`. Curated prose/reference matching includes `Definition`, `ReferenceTrail`, and `DefinitionSource`. When bucket 4 matches both a definition phrase and an exact citation token, report `Reference`. Audit matching requires all query words within one recorded expanded/context query; it never matches review notes or numeric page arrays.

Preserve symbols in acronym matching: `C#`, `K/#`, `(a)`, `A-T`, and `TO&E` remain distinct. Use accent-insensitive and hyphen/space-tolerant comparison only as secondary prose matching. Require all words for multiword prose matches. If a nonempty symbol query yields no prose tokens, skip word matching rather than treating an empty token set as a match for every entry. Do not stem arbitrary acronym suffixes or fabricate compound forms.

Recognize complete citation tokens before prose fallback, so `G13.1` is not an exact match for `G13.12`. Keep shorthand ranges intact and searchable as text without claiming interval membership. Do not search numeric page arrays through the default text box.

Strict acronym case changes only form matching. An entry with a differently cased form may still appear if its prose independently matches the query, and must carry the prose match reason. The UI explains this behavior beside the option; it must not promise to hide all case variants.

Case variants, shared display forms, and combined forms remain separate concepts: DR/dr are separate IDs; equipment CA has two IDs; WW1 / WWI has one ID with two short-form candidates. MC keeps its entire multi-context definition.

## 5. Blazor component and state design

### Component boundaries

| Component | Contract |
|---|---|
| `AcronymSearch.razor` | `@page "/acronyms"`; `PageTitle`, existing `PageHeader`, query-string binding, and panel state callbacks. |
| `AcronymSearchPanel.razor` | Accepts a state value and emits `StateChanged` and `EntrySelected`; calls search service; no direct browser navigation. An embedding host supplies its own state. |
| `AcronymFilterBar.razor` | Labeled search input, category selector, two checkboxes, and clear action. Follow `LibraryFilterBar` presentation conventions without its board-specific behavior. |
| `AcronymResultList.razor` | Ordered results with native selection buttons, count and paging controls. Shows exact form, definition excerpt, category, and match reason. |
| `AcronymEntryDetails.razor` | Complete definition, source, contributing references, and expandable evidence/guidance. |

These are responsibility boundaries, not mandatory file splits. Initially keep filter and result markup in the reusable panel; extract child components only when their complexity or actual reuse warrants it. Keep related small records together and use concrete services rather than speculative interfaces.

`AcronymSearchState` contains query, category, case option, audit option, page, and selected entry ID. Give each panel instance a unique control-ID prefix so two panels have independent labels, disclosures, and state.

The host owns committed state and passes updates back to the panel. The panel owns only transient typed text and debounce work. Emit one `StateChanged` per action, including selection; `EntrySelected` is an optional notification, not a second state update. The page updates its URL only from `StateChanged`. A host/URL state update cancels stale pending input before applying the new value.

Debounce typed input by 200 ms. Cancel pending work on new input or disposal; Enter executes immediately. Use a generation counter or cancellation check so a delayed callback cannot overwrite newer state. Filtering resets page to 1. Clear resets query, filters, page, and selection.

Result selection opens the complete entry without truncation. Preserve an explicitly linked selection even if filters exclude it, accompanied by a short explanatory message. Unknown entry IDs show an entry-not-found message while search remains available. Do not auto-select the first entry or move keyboard focus unexpectedly.

### URL and navigation

Support `q`, `category`, `case`, `audit`, `page`, and `entry`. Omit defaults from generated URLs. Encode values through query helpers, not string concatenation. Preserve identity case and punctuation on round trips. Bind externally supplied filter/page parameters as strings and parse them with non-throwing helpers, so malformed booleans or numbers do not fail Blazor parameter conversion before defaults can be applied. Invalid enum/boolean values fall back to defaults; invalid page numbers normalize safely.

Apply navigation changes through parameter updates, including back/forward navigation. Replace history for typing; use a new history entry for deliberate selection or paging. Avoid feedback loops by comparing the canonical state before updating the URL. Standalone page behavior must not leak into the embedded component.

Add a Reference group to `StudioNavigation.Groups`, with route prefix `acronyms` and destination label **Acronym Search**. Use the existing layout and responsive navigation.

## 6. Existing UI and CSS integration

Add one clearly labeled Acronym Search section to the existing `wwwroot/site.css`. Reuse the current global element styles and existing classes where their meaning fits: `page-header` through `PageHeader`, `library-filters`, `toolbar`, `toggle`, `muted`, and native `details`/`summary` patterns. Do not change shared selectors solely to style this page.

| Visual concern | Required implementation |
|---|---|
| Typography | Use existing `--font-sans` for prose and `--font-mono` for acronym codes. Retain the app's page-heading design. |
| Color and surfaces | Reuse `--ground`, `--surface`, `--surface-2`, `--ink`, `--muted`, `--line`, `--accent`, and `--accent-tint`. Do not hard-code a replacement palette. |
| Controls | Inherit current input, select, checkbox, button, disabled, hover, and focus-visible styling. |
| Structure | Add feature-scoped classes under `.acronym-search`, such as `.acronym-search-layout`, `.acronym-results`, `.acronym-entry`, `.acronym-form`, and `.acronym-evidence`. |
| Selected result | Use an explicit selected-state attribute/class, existing accent tokens, and visible selected text or an accessible state. Do not rely on color alone. |
| Exact acronym case | Set `text-transform: none` on `.acronym-form`, including forms inside headings and buttons. The current theme uppercases some headings; `dr` must never visually become `DR`. |
| Responsive layout | Two columns on wide screens; one column with details below results at the existing 1023.98 px breakpoint. Let filter controls wrap and ensure no horizontal page overflow at 320 px. |
| Long content | Allow wrapping of citation strings and IDs. Put large page/query lists inside collapsed disclosures; do not force the whole page into a fixed-height scroll panel. |

Reuse the current spacing and border treatment, including `--radius`, rather than introducing a new visual system. Keep feature selectors scoped so Board Library, Map Studio, and Play retain their existing presentation.

Visible order: page header, search/filter bar, result count/paging, results, selected-entry details. At narrow widths preserve that DOM order. Use native controls, visible labels, an appropriate heading hierarchy, and a polite result-count status region. Selection buttons expose the acronym plus enough definition context to distinguish repeated forms.

The details view separates **Definition source**, **Contributing rules and applications**, and **Search and review evidence**. Evidence includes the original status/date/basis, query strings, and page sets. State plainly: **Search hits are leads; not every hit defines this term.** Conventional expansions and external-module limitations stay visible with their source wording.

Display physical pages as `PDF p. 308` or `PDF pp. 308-309`. Do not create broken links to local files or imply the PDF is bundled. Render source text through normal Razor escaping. Match highlighting is outside this implementation. Render guidance as encoded text/list/table content from the known source sections, not raw HTML or a new general Markdown renderer.

## 7. Dedicated tests under ASL.Acronyms

Create `src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj` and add it to `src/ASL/LimboDancer.Domains.Asl.sln`. Set `<RootNamespace>ASL.Acronyms</RootNamespace>` and declare `namespace ASL.Acronyms;` explicitly in all feature test and fixture classes. Keep the assembly name `ASL.Acronyms.Tests`.

Reference MapStudio and the catalog-builder project. Use the repository-managed xUnit, test SDK, and bUnit packages. Include integration-test dependencies only when needed, using versions compatible with the target and repository conventions. Commit the new project's package lock file. Do not copy unrelated board/play fixtures or put acronym tests inside existing test classes.

| Test class | Behavior to verify |
|---|---|
| `CatalogImportTests` | CSV quoting/newlines, Markdown table boundaries and escaped pipes, JSON validation, page bounds/ranges, empty hit arrays. |
| `CatalogReconciliationTests` | All 685 baseline entries align; missing, duplicate, mismatched, and ambiguous rows fail clearly; reordered CA/PC rows keep their IDs. |
| `CatalogGenerationTests` | Equivalent LF/CRLF input produces identical output; verify mode detects stale output without writing; revision/report changes do not change catalog bytes. No temporary Git repository is needed. |
| `AcronymSearchTests` | DR/dr case ranking, HOB/HoB, WWI, MC contexts, OC definition, exact citation tokens, punctuation codes, audit opt-in, deterministic tie-breaking and pagination. |
| `AcronymStateTests` | URL round trips, invalid parameters, clear/reset behavior, exact-case IDs, missing/out-of-filter selections. |
| `AcronymComponentTests` | bUnit checks labels, full details, disclosures, result reasons, error/empty states, callbacks, and two independent panel instances. |
| `AcronymNavigationTests` | Route and Reference destination integrate with current navigation; page parameters update state. |
| `AcronymCatalogResourceTests` | The resource loads without repository/PDF access; missing or invalid catalog state is distinct from no results. |

Use small targeted fixtures for edge cases and copy the two actual source files into test output for the complete baseline reconciliation check. Never discover fixtures through a developer's absolute checkout path. Fixtures should express intended outcomes independently of the search implementation. Use controlled timing for debounce tests rather than wall-clock sleeps.

The focused command is:

```powershell
dotnet test src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj
```

Component tests do not prove rendered CSS or browser navigation behavior. Supplement them with a browser pass at desktop and narrow widths: exact-case code rendering, keyboard focus, expanded evidence, long references, 200% zoom, and back/forward behavior. Compare the page with the existing Board Library and inspect those shared pages for styling regressions. Run a publish smoke check from outside the source tree. Record the outcomes in implementation notes.

## 8. Delivery sequence and completion criteria

1. Establish the separate test project and source fixtures. Implement catalog contracts, importer, reconciliation, and deterministic generation.
2. Embed the verified catalog; implement loading and stateless search with the dedicated behavior tests.
3. Implement reusable components, page, URL state, and Reference navigation.
4. Extend only the existing stylesheet, then verify responsive layout and case-preserving typography in a browser.
5. Run catalog verification, the dedicated acronym suite, relevant existing navigation/component checks, and publish smoke verification. Measure search latency over the full catalog; target under 50 ms on development hardware, excluding debounce and network latency, as a measurement rather than a flaky unit-test assertion.

The feature is complete when all source entries are available with their original meanings and qualifications, lookup and navigation satisfy the test matrix, tests live under `ASL.Acronyms`, the page follows the existing stylesheet and UI, and the deployed app works without source-document paths. This design document alone does not establish those implementation outcomes.
