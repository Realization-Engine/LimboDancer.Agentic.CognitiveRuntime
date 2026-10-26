# ASL Acronym Search Implementation Plan

Date: 2026-10-26

Status: Implemented and locally verified, 2026-10-26.

Branch: `codex/asl-acronym-search`

References: [Analysis](ASL%20Acronym%20Search%20Analysis.md), [Design](ASL%20Acronym%20Search%20Design.md).

## 1. Working rules

- **Standing testing rule: run only relevant tests.** Select tests by the behavior changed and its direct dependencies. Do not run the ASL solution suite, repository suite, or unrelated game/map tests for this feature. Broaden a test selection only when an observed failure or a changed shared contract gives a concrete reason, and record that reason.
- Keep implementation small. Use a local in-memory search over the 685 reviewed entries, ordinary C# types, and the app's existing Blazor patterns.
- Place feature tests in the separate `ASL.Acronyms.Tests` project, with test and fixture classes declared in `namespace ASL.Acronyms;`.
- Extend the existing `wwwroot/site.css`, reusing its tokens, controls, and layout conventions. No new theme, inline styles, CSS framework, or `.razor.css` files.
- Preserve both source documents and their reviewed wording. Source disagreement is a diagnostic, not permission to rewrite a definition.
- Complete each step and its focused checks before advancing. Once relevant checks pass, do not repeat them unless subsequent changes affect their behavior.

## 2. Keep the implementation bounded

The design's type and component tables describe responsibilities, not a requirement for one file, interface, or test class per row.

Start with one catalog builder, one catalog loader, one search service, one reusable search panel, one details component, and the thin routed page. Keep filters and result markup inside the panel initially. Extract them only if actual complexity or reuse makes that clearer. Group related records in a small number of files.

Use concrete services with dependency injection. Do not add repository abstractions, generic search providers, mediator/event infrastructure, a database, HTTP search endpoints, background refresh, or a new production library. A simple collection scan is sufficient unless measurement demonstrates otherwise.

Keep the parser limited to the documented source formats. Prefer an existing or framework CSV reader over writing a CSV parser; do not build a general Markdown engine for the known tables. Validate unsupported structures explicitly. Keep citations as text plus a small exact-token matcher; do not implement a comprehensive ASL reference grammar.

Do not add fuzzy search, autocomplete, match highlighting, exports, configurable page sizes, a PDF viewer, or performance benchmark infrastructure. Preserve the designed case handling, category filter, audit opt-in, 25-result pagination, evidence disclosure, and URL state.

## 3. Step 1: Catalog import and dedicated tests

### Changes

- [x] Add `src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj`, reference the required production/tool projects, and add it to the ASL solution. Use existing centrally managed test packages and commit its lock file.
- [x] Add compact catalog records in MapStudio `Services/Acronyms/`.
- [x] Add the small C# builder under `code-builders/asl-acronyms/`, explicitly importing the ASL build settings because this directory does not inherit them. Commit its lock file. Provide generate and verify commands with explicit file paths.
- [x] Parse the ledger and the two definition-table shapes. Carry source guidance and evidence into the catalog. Reconcile rows by exact category/form and complete definition/citation fields, retaining ledger IDs.
- [x] Validate duplicate/missing/ambiguous identities, CSV/JSON shapes, page bounds, and ranges. Preserve empty query results and source qualifications.
- [x] Generate deterministic `Data/acronyms.catalog.json` only after validation succeeds. Normalize text-source BOM/line endings before parsing/hashing, sort query-map keys, and use the design's canonical output/verify rules. Verify mode compares without writing. Keep revision/diagnostics in the build report, not the catalog.

### Relevant checks

Run importer/reconciliation/generation tests only. Use the two actual source files copied to test output and small synthetic failure cases. Check all 685 entries, including the separate CA/PC `:2` identities, case distinctions, quoted CSV content, and identical output for equivalent LF/CRLF inputs. Reordering duplicate display forms must not swap identities. Assert that changing report revision metadata does not change catalog bytes; do not build temporary Git repositories for this check.

**Exit condition:** all entries reconcile one-to-one, generated output is repeatable, and failure messages identify the affected row/field. No reviewed source files changed.

## 4. Step 2: Catalog loading and search

### Changes

- [x] Embed the generated catalog explicitly in the MapStudio project and load it once into an immutable catalog. Condition the resource item on file existence so the builder can compile without generated data; verification/resource tests must fail if the deliverable lacks it. Generate explicitly, then rebuild the app. Do not add a build target that invokes the app-dependent builder.
- [x] Expose a clear unavailable state for invalid/missing data. Keep this distinct from successful searches with zero results.
- [x] Implement a stateless search method with the design's rank buckets, stable ordering, category filtering, case option, audit opt-in, and pagination.
- [x] Preserve punctuation and exact-case IDs. Add secondary prose normalization without changing displayed wording.
- [x] Match exact citation tokens without expanding shorthand ranges or treating discovery page hits as definition evidence.
- [x] Register the catalog and search service in `Program.cs`.

### Relevant checks

Run catalog-resource and search tests only. Cover DR/dr, HOB/HoB, WWI, the two equipment CA/PC meanings, MC's complete contexts, OC, G13.1 versus G13.12, symbol codes, audit-only matches, empty input, no matches, and page bounds.

Strict acronym case controls form matching; a prose match may still return a differently cased form with the appropriate match reason. Test this explicitly rather than asserting that every case variant disappears.

**Exit condition:** search results preserve meaning and identity, return deterministic ranks/reasons, and load without source-document filesystem access.

## 5. Step 3: Reusable Blazor UI and page

### Changes

- [x] Implement `AcronymSearchPanel.razor` with native labeled controls, results, and paging. The host owns committed state; the panel owns transient input/debounce work. Emit one state change per action. An embedding host can use it without browser navigation.
- [x] Implement `AcronymEntryDetails.razor` with the complete definition, separate citations, and collapsed search/review evidence. Render source text with Razor encoding.
- [x] Implement `Components/Pages/AcronymSearch.razor` at `/acronyms`, using the existing `PageHeader`, `PageTitle`, and layout.
- [x] Add the Reference navigation group and Acronym Search destination.
- [x] Add query-string state for query, category, case, audit, page, and selected identity. Encode punctuation and preserve case. Parse filter/page strings with non-throwing helpers; cancel pending input when URL/host state changes. Handle invalid values and back/forward navigation.
- [x] Add 200 ms input debounce with cancellation/disposal, immediate Enter search, filter page reset, and clear/reset behavior. Keep this local; do not introduce a general scheduling service.
- [x] Provide loading/unavailable, no-results, unknown-selection, and selection-outside-filters states. Give panel instances unique control IDs.

### Relevant checks

Run acronym component and state/navigation tests only. Cover callbacks, independent panel instances, complete definitions, evidence labels, encoding, selection, reset, query round trips, and stale debounce callbacks. Use controlled timing rather than sleeps.

Existing navigation coverage is in `SharedComponentTests.cs`; select only its methods that exercise `StudioNavigation` or `GroupOf`, plus the new feature navigation tests. Do not run the full MapStudio suite just because it contains those tests.

**Exit condition:** the new page is reachable, the panel is reusable, and state behaves correctly without modifying unrelated pages or shared user state.

## 6. Step 4: Existing stylesheet and browser verification

### Changes

- [x] Add a scoped Acronym Search section to the existing `wwwroot/site.css` using current tokens and applicable shared classes.
- [x] Use two columns on wide screens and stack details below results at the existing breakpoint. Let controls and long citations wrap.
- [x] Preserve acronym case with scoped `text-transform: none`, including codes inside headings/buttons.
- [x] Keep existing focus styling and add a selection indication that does not rely on color alone. Include a polite result-count announcement.

### Relevant checks

Inspect `/acronyms` in a browser at desktop and narrow widths, including 320 px and 200% zoom. Check keyboard navigation, visible focus, DR/dr typography, long references, disclosures, no horizontal overflow, and back/forward navigation.

Compare styling with Board Library. If changes are confined to new scoped selectors, no unrelated component suite is needed. If an existing shared selector must change, inspect and test the directly affected consumer(s), documenting why. Prefer avoiding such shared changes.

Do not add tests that assert CSS declaration strings or duplicate the stylesheet implementation. Browser inspection checks the rendered result.

**Exit condition:** the page matches the current UI, remains usable at narrow widths, and displays meaningful case distinctions accurately.

## 7. Step 5: Focused integration and delivery

### Changes and checks

- [x] Add scoped catalog verification and acronym tests to CI. The existing `asl-authoring-ci.yml` broadly selects tests for `src/ASL/**`, so adding a job alone is insufficient. Add narrow job selection for acronym-only changes and document-only changes; retain existing behavior for mixed/unrelated changes. Include builder, generated data, and relevant build/dependency changes in feature triggers. Avoid redesigning unrelated jobs or creating a general-purpose build system.
- [x] Run the dedicated acronym suite once after integration. Every test in that project concerns this feature; this is the integration gate, not a reason to rerun unrelated suites.
- [x] Run catalog verify mode against the committed JSON.
- [x] Publish MapStudio to a temporary output directory and smoke-check `/acronyms` from that output with a working directory outside the checkout. Confirm catalog loading without repository/PDF paths. Do not launch unrelated map/game workflows.
- [x] Take a one-off search timing measurement over the full catalog. Treat the design's 50 ms target as diagnostic evidence, not a timing assertion or a reason to create benchmark tooling. Optimize only a measured bottleneck.
- [x] Review the final diff for unintended source edits, unrelated changes, added dependencies, and unnecessary abstractions. Record completed checks and any limitations.

**Exit condition:** the feature and relevant checks pass, publication carries the catalog, and the implementation remains confined to this feature and its integration points.

## 8. Test execution guide

During development, filter the dedicated project to the test classes relevant to the current step. The following example assumes the design's class names; adjust it to the actual names rather than creating extra classes merely to fit this command:

```powershell
dotnet test src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj --filter "FullyQualifiedName~ASL.Acronyms.AcronymSearchTests"
```

At the final feature integration gate:

```powershell
dotnet test src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj
```

For an affected existing test, specify its project and exact class/method filter. Discover its actual name before running it. Building project dependencies is expected; running their test suites is not.

Documentation-only changes require link/format checks, not application tests. After a failure, rerun the affected tests following the fix. Repeat a previously passing group only when the fix can affect it. Do not expand to solution-wide testing as a default troubleshooting step.

## 9. Handoff record

At implementation completion, record the new/changed files, source/canonical catalog counts, focused test commands and results, browser and publish checks, and any remaining limitation. Mark completed steps above. Do not claim browser, publish, or test verification that was not actually performed.

### Documentation review, 2026-10-26

Reviewed all three documents against the source tables/ledger, app hosting and navigation, stylesheet conventions, project settings, and existing CI. Reconfirmed 685 unique reconciled entries and 14 ledger columns; parsed 1,257 per-query page arrays and checked their page bounds. Corrected discrepancies in case filtering, sorting, state ownership, service locations, catalog metadata, and implementation scope. Clarified line-ending-independent generation, missing-catalog build sequencing, safe URL parsing, and focused CI selection. Document link/format checks apply to this review; application tests, browser checks, and publish checks remain implementation work. The original PDF was not reverified.

### Implementation handoff, 2026-10-26

Implemented `/acronyms` in the Reference navigation group. The reusable search panel and details component use the existing stylesheet. Search includes ranked form/definition/citation matching, category and case filters, optional audit text, 25-result pages, evidence disclosures, and URL state with back/forward support.

The builder reconciles 685 entries (438 main and 247 equipment) from the unchanged scan and ledger. The generated catalog is embedded in MapStudio. Generation normalizes BOM/newlines, including serialized output, so Windows and Linux verification agree. Builder usage is documented in `code-builders/asl-acronyms/README.md`.

Completed validation:

- Dedicated `ASL.Acronyms.Tests` project: 30/30 passed. Tests use namespace `ASL.Acronyms`.
- Existing navigation tests only: `SharedComponentTests.EveryRouteBelongsToItsGroup` and `SharedComponentTests.TheNavigationMarksTheCurrentGroupAndHidesWhenAsked`, 13/13 passed.
- Following the canonical newline correction, `CatalogTests` passed 10/10 and generate/verify succeeded. Following the final UI/resource rebuild, component tests and `PublishedResourceHasAllEntries` passed 6/6. No solution-wide test suite was run.
- Browser checks covered desktop/narrow layouts, zoom, keyboard focus, case-sensitive typography, evidence disclosure, no results, Clear, selection, and browser back/forward. Compared styling with Board Library.
- Published app served the acronym page successfully from a temporary directory outside the checkout, including the Ocean overlay query. Both temporary published builds were removed and their preview servers stopped after testing.
- One-off warm search measurement: 11.17 ms over 685 entries. This is a local observation, not a performance guarantee.
- Local CI selection checks cover isolated feature changes, documentation-only changes, and mixed/unrelated changes. Remote GitHub Actions have not been run.
- Final diff checks passed; the two reviewed source documents remain unchanged. No additional runtime package dependency or general search infrastructure was introduced.

Changes are local on `codex/asl-acronym-search`. The PDF metadata is carried from the reviewed scan; the original PDF was not independently reverified during implementation.
