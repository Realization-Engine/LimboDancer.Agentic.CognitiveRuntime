# ASL Acronym Search Analysis

Date: 2026-10-26

Status: Analysis complete; implementation and local verification are recorded in the implementation plan.

Baseline: `origin/main`, commit `32f6d46d20a553b561b258d67382871e7905f2da`

Working branch: `codex/asl-acronym-search`

Implementation decisions are specified in the separate [ASL Acronym Search Design](ASL%20Acronym%20Search%20Design.md), including dedicated tests under `ASL.Acronyms` and reuse of the existing UI stylesheet.

## 1. Purpose and scope

Add an **Acronym Search** page to the ASL app so a player can find an abbreviation, search its meaning, and follow its definition and contributing citations. The page must use both the [acronym scan](ASL%20Rulebook%20Acronym%20Scan.md) and [expanded-term search ledger](ASL%20Acronym%20Expanded-Term%20Search%20Ledger.csv).

This document proposes the data contract, search behavior, Blazor components, integration points, and acceptance checks. It does not implement the page. The current host is `LimboDancer.Domains.Asl.MapStudio`; the search UI should also be usable as a component within that host's other pages.

Initial scope is a read-only lookup over the curated reference. Full rulebook text search, automatic rules interpretation, editing the source documents, generated acronym expansions, and a PDF viewer are separate work.

## 2. What the two sources contain

The ledger contains 685 entries, divided into 438 `main:` identities and 247 `equipment:` identities. Its current review dispositions are 276 `Reviewed: corrected`, 234 `Reviewed: retained`, and 175 `Earlier 175-entry expanded-term review`. Those labels describe review history; they are not confidence scores or unreviewed states.

The scan documents the original source as `eASLRB_v3_01.pdf`, 716 physical pages, with SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. Physical PDF pages are counted from 1 and differ from printed page labels.

| Source | Contribution to the feature |
|---|---|
| Scan: alphabetical tables | Display acronym, definition, contributing rules, and definition source in four columns. |
| Scan: equipment table | Display form, explanation, and contributing source in three columns. |
| Scan: introductory and closing sections | Citation conventions, case/context distinctions, compound-form guidance, scope limitations, and review provenance. |
| Ledger: identity and definition fields | Stable `entry_id`, acronym, definition, reference trail, and definition source for cross-checking the scan. |
| Ledger: query and page fields | Search variants, expanded/context queries, page-hit sets, and a JSON map from expanded query to matched pages. |
| Ledger: review fields | Review status, basis, changes, and date for the evidence disclosure. |

Important constraints:

- `main:DR` and `main:dr` are separate entries. The same applies to other case distinctions. Use ordinal, case-sensitive entry identities throughout.
- Repeated display forms can also be separate entries within a category: `equipment:CA` and `equipment:CA:2`, and `equipment:PC` and `equipment:PC:2`. The ledger suffix distinguishes their meanings; category plus display form alone is not a unique key.
- Some entries combine forms, such as `equipment:WW1 / WWI`, whose ledger short forms are `WW1; WWI`. Preserve the combined entry and offer both forms as search candidates.
- Some single entries contain multiple meanings. `MC` includes Morale Check, motorcycle, and Mortar Carrier. Keep its complete explanation rather than automatically splitting it into invented senses.
- Short-form variants and expanded queries are historical search inputs. They are useful retrieval evidence, but are not all authoritative synonyms. An old query can remain after a definition correction.
- Page hits are discovery leads, not proof that a page defines the term. Broad queries such as `British` can produce large, noisy hit sets.
- Chapters O through T and module-specific references can be external locators whose rule text is absent from this PDF. Do not represent those references as verified passages in the PDF.
- The scan distinguishes conventional expansions from expansions actually spelled out in the source. Retain those qualifications verbatim.

## 3. Recommended data pipeline

Use a deterministic catalog built from both documents. At this size an immutable in-memory collection and a direct scan are sufficient as the initial design; measure before introducing an external search service or database.

### Import and reconciliation

1. Parse the CSV with a CSV-aware reader supporting quoted commas, doubled quotes, and multiline fields. Parse `expanded_query_page_map` separately as JSON. Do not split CSV rows on commas.
2. Parse only the scan's definition tables, using their enclosing sections and expected headers. The main tables have four columns; the equipment table has three. The case/context table is guidance, not another set of entries. Support escaped pipes and inline code through a Markdown parser or a deliberately constrained, tested table reader.
3. Match scan rows to ledger candidates by category and exact displayed acronym/form after removing presentation backticks. Do not lowercase or remove punctuation. For repeated forms, reconcile the complete definition and source fields to select the ledger identity, including suffixes such as `:2`. The current source order also corresponds to first occurrence, second occurrence, but order alone must not silently reassign identities after an edit. Preserve existing ledger IDs; ambiguous or changed duplicate-form rows need an explicit mapping or source correction.
4. Require a one-to-one join. Compare the acronym, definition, and citations, allowing only documented presentation normalization such as line endings, outer whitespace, and equivalent inline-code delimiters. Do not normalize away case, punctuation, accents, or substantive words.
5. For main rows, map columns three and four to `document_reference_trail` and `definition_source`. For equipment rows, compare the single source column against both ledger fields, which currently carry that source text.
6. Retain the scan's source conventions, case/context table, and compound guidance as catalog-level content. Search matches remain entry-based; guidance is displayed separately.
7. Validate identities, required fields, JSON types, and page ranges. Reject duplicate identities, orphaned rows, mismatched text, malformed ranges, and pages outside 1 through 716 for this edition. Empty query-result arrays are valid.
8. Emit a versioned JSON catalog with canonical source-text hashes, PDF metadata, entries, and guidance. Normalize source line endings before hashing and serialization, as specified in the design. Keep revision information and validation diagnostics in a separate build report. Rebuilding equivalent inputs must produce identical catalog content, independent of checkout line endings and unrelated commits.

The current 685-entry count is a baseline fixture, not a permanent production limit. Future intentional additions should update the expected fixture and still reconcile one-to-one.

Analysis verification: all 685 Markdown definition rows were matched to 685 unique ledger IDs using case-sensitive category/form, definition, reference trail, and definition-source comparisons. No field mismatches remained after accounting for the separate CA and PC equipment identities. This check validates the current source alignment; it is not yet a production importer.

The Markdown supplies the curated display text, the ledger supplies search/audit detail, and disagreement fails catalog generation. Neither silently overrides the other. Do not edit the two source documents merely to make an import pass.

### Proposed catalog model

| Field | Meaning |
|---|---|
| `Id`, `Category`, `DisplayForm` | Exact ledger identity, main/equipment category, original display text. |
| `Definition`, `ReferenceTrail`, `DefinitionSource` | Reconciled, complete source wording. |
| `ShortFormCandidates` | Ledger search forms, retaining original case and punctuation; historical variants are not automatically labeled synonyms. |
| `ExpandedQueries` | Original query strings with their page sets, including zero-hit queries. |
| `ShortFormPages`, `ExpandedContextPages` | Parsed, sorted, deduplicated physical page sets, with original range strings retained for provenance. |
| `Review` | Original status, basis, change description, and date. |
| `SourceLocator` | Scan section and row identity plus ledger entry identity, for maintenance and diagnostics. |

Keep original reference text alongside any parsed rule/page tokens. References include `A.9`, `C8.1-.2`, national notes, chart names, and external locators; parsing must not discard unrecognized text or invent page numbers. Structured citation links can be added incrementally when the target is unambiguous.

### Build and deployment

Add the C# catalog-generation utility under `code-builders/asl-acronyms` with explicit generation/verification commands. Commit the generated catalog under the app's `Data` directory and have narrowly scoped CI regenerate and compare it to catch stale output. Embed it in the app project rather than assuming the deployed app has access to `src/ASL/docs`.

Load and validate the catalog once in an immutable singleton. A missing or invalid catalog produces an explicit unavailable state and a server diagnostic, not an empty set presented as a successful search. Keep query and selection state in each component instance, never in the singleton.

## 4. Search contract

### Default behavior

- Empty input browses all entries, sorted by display form ordinal-ignore-case with an exact-ID ordinal tie-breaker; show 25 results per page.
- Search acronym/form, definition, and citation text by default. Offer category selection: All, Rules and counter codes, or Equipment and historical terms.
- Provide an optional **Include audit search terms** switch, off by default. This adds ledger expanded/context queries as a lowest-ranked match source and labels those matches explicitly.
- Search is case-tolerant, but case-exact acronym matches rank first. Display other case variants rather than hiding them. Offer **Match acronym case** for users who want strict acronym/form matching; it does not make definition prose case-sensitive.
- Trim the query and normalize Unicode consistently. Preserve `#`, `/`, parentheses, hyphens, ampersands, and periods in the primary acronym comparison. `C#`, `K/#`, `(a)`, `A-T`, `TO&E`, and `TOE` must remain distinct.
- Use accent-insensitive and hyphen/space-tolerant matching only as secondary prose retrieval. Keep the original display strings and exact identities intact.
- For multiword prose queries, require all words across the selected searchable fields, then prefer a contiguous phrase in the definition. Do not impose a minimum length that prevents searching one-letter codes.

### Ranking, highest first

1. Exact case-sensitive display form or short-form candidate.
2. Exact case-insensitive display form or short-form candidate.
3. Acronym/form prefix match, with case-exact prefixes first.
4. Exact definition phrase or exact citation token.
5. All query words found in definition/reference text.
6. Partial acronym/form match.
7. Expanded/context audit-query match, only when enabled.

Use explicit rank buckets, then display form ordinal-ignore-case and exact-ID ordinal ordering, not an opaque relevance score. Deduplicate matches by `Id`, never by a lowercased acronym. Return a match reason such as `Exact acronym`, `Definition`, `Reference`, or `Audit search term`. Search definition, reference trail, and definition source as curated text. When an exact citation and a definition phrase share rank, report `Reference`; retain the complete entry either way.

Rule searches such as `G13.1` should prefer an exact citation token over substring matches like `G13.12`. Preserve shorthand ranges as source text; do not claim that a rule falls inside a range unless a tested range parser supports it. A dedicated physical-page filter can follow later; bare numeric input should not silently search every audit page number.

Do not automatically stem every acronym plural or decode arbitrary compound forms. Initially use recorded forms and show the scan's compound guidance. This avoids inventing equivalences for equipment codes.

### Representative outcomes

| Input | Expected behavior |
|---|---|
| `DR` / `dr` | Exact case first. Strict case affects acronym/form matching only; the other entry can still appear through independently matching prose, with a prose match reason. |
| `HoB` | Find the HOB entry through its recorded short form. |
| `Morale Check` | Find definitions containing the phrase, including MC and qualified check entries. |
| `MC` | Show the complete explanation with all three documented contexts. |
| `WWI` | Find the combined WW1 / WWI equipment entry once. |
| `Ocean overlay` | Find OC through its corrected definition. |
| `G13.1` | Find OC through its reference trail with an exact-token match reason. |
| `British` | Search curated text normally; broad audit-query matches appear only when enabled. |
| No match | Show the query, a clear no-results message, and a reset-filters action. |

## 5. Blazor page and reusable component

### Integration with the current app

The app targets .NET 10 and uses interactive server rendering. `Program.cs` registers services with dependency injection; `Components/App.razor` applies `InteractiveServer` to `Routes`; `Components/Routes.razor` discovers routable components through the app assembly. A new page therefore needs no separate router registration or HTTP search endpoint.

| Proposed file/change | Responsibility |
|---|---|
| `Components/Pages/AcronymSearch.razor` | Route `/acronyms`, page title, `PageHeader`, and URL query-state coordination. |
| `Components/Acronyms/AcronymSearchPanel.razor` | Reusable search input, filters, result count, pagination, and selection state. |
| Result markup within `AcronymSearchPanel.razor` | Accessible results with form, definition excerpt, category, and match reason; extract a child component only if complexity warrants it. |
| `Components/Acronyms/AcronymEntryDetails.razor` | Complete definition, citations, physical-page labels, and expandable evidence. |
| `Services/Acronyms/AcronymCatalog.cs` | Load validated, immutable catalog and source metadata. |
| `Services/Acronyms/AcronymSearchService.cs` | Pure filtering/ranking over catalog entries, independent of rendering. |
| `Program.cs` | Register catalog and stateless search service. |
| `Components/Layout/StudioNavigation.razor` | Add a Reference group and an Acronym Search destination for `acronyms`. |
| App project file and styling | Package the catalog and style the page consistently with `wwwroot/site.css`. |

Keep these types in MapStudio initially. A separate Razor class library is justified only when a second host needs the feature. The reusable panel accepts the complete state value and emits state changes and entry-selection notifications. The host owns committed state; the panel owns only transient input/debounce state. The page owns browser navigation; embedding the panel must not rewrite another page's URL.

### Visible layout

```text
Acronym Search
[Search acronym, meaning, or rule________________] [Clear]
[All categories v] [ ] Match acronym case
[ ] Include audit search terms

23 results                         [Previous] [Next]
Results                            Selected entry
DR  Dice roll ...                  DR
dr  Die roll ...                   Complete definition and context
...                                Definition source
                                   Contributing rules and applications
                                   > Search and review evidence
                                   > Source conventions
```

On narrow screens, place details below the results. Use ordinary buttons/links and headings, avoiding a custom keyboard-heavy listbox. Each result should expose the acronym's exact case and enough definition text to distinguish meanings. Never abbreviate the complete selected definition.

The evidence disclosure contains the historical queries, separate short-form and expanded/context page lists, per-query page maps, and review disposition/basis/date. Label it: **Search hits are leads; not every hit defines this term.** Show large page lists in collapsed, bounded sections. A zero-hit query means no match for that query, not that the term has no definition.

Show PDF locators as `PDF p. 308` or `PDF pp. 308-309`. Keep printed labels and rule numbers distinct. Initially these are text citations. Add links only when an authorized document location and matching edition are configured; do not ship a PDF or construct links from a user's local file path. Preserve external-locator qualifications even when a PDF location is available.

Use a visible input label, keyboard-accessible controls, a polite result-count announcement, and explicit loading/error/empty states. Selection must not unexpectedly move keyboard focus. Render text through Razor escaping; do not inject raw source Markdown as `MarkupString`. Match highlighting is outside this implementation.

### State and navigation

Support shareable page parameters such as `q`, `category`, `case`, `audit`, `page`, and `entry`. Percent-encode identity and query values, especially `#`, `/`, and spaces. Parse invalid filter values safely to defaults and bound page numbers. A selected entry ID must resolve by exact case.

Use about 200 ms input debounce with cancellation/disposal support and Enter for immediate search. Reset pagination when the query or filters change. Replace history during typing; deliberate selection can create a history entry. Handle back/forward navigation in parameter updates. An explicit entry link may open details even when the current filters exclude it, but the UI must explain that state. Unknown IDs should show an entry-not-found message while leaving search usable.

## 6. Implementation sequence and validation

1. Build the two-source importer and reconciliation report. Verify every current identity and field, plus publication packaging independent of repository paths.
2. Implement the immutable catalog and deterministic search service. Measure cold load and repeated queries over the complete catalog; a provisional target is under 50 ms for server-side search on development hardware, excluding network/debounce time.
3. Build the reusable panel and details components, then the thin `/acronyms` page and navigation entry.
4. Add URL state, responsive layout, accessibility checks, and clear unavailable/no-results behavior.
5. Verify publish output, run the dedicated acronym tests plus only directly affected existing navigation tests, and inspect the page at desktop and narrow widths before calling implementation complete. Standing rule: run only relevant tests; do not run solution-wide or unrelated suites.

The existing `LimboDancer.Domains.Asl.MapStudio.Tests` project provides examples of xUnit, bUnit, and ASP.NET Core integration testing. Per the subsequent design requirements, create a separate `ASL.Acronyms.Tests` project with feature tests in the `ASL.Acronyms` namespace; see the design document for the test layout and acceptance matrix.

Acceptance checks:

- All 685 baseline entries reconcile, with 438 main and 247 equipment entries. A deliberately changed definition, missing row, duplicate ID, or malformed CSV/JSON causes a useful diagnostic.
- Repeated CA and PC equipment forms retain their separate ledger identities, including `:2`, and cannot overwrite one another. Reordering those rows must not swap their identities.
- Case-sensitive identities survive import, lookup, sorting, selection, and URL round trips. DR/dr, DM/dm, and HT/ht never collapse.
- Quoted CSV fields, combined forms, accented names, punctuation codes, empty query arrays, and page ranges round-trip without loss.
- The search examples above produce the intended ranking and explanations. Audit-only hits cannot appear with the switch off.
- Main and equipment rows render their complete source wording, including conventional-expansion and external-reference qualifications.
- Two simultaneous component instances keep independent queries and selections despite sharing the catalog.
- Pagination, clear/reset, keyboard selection, back/forward navigation, invalid query parameters, no-results state, and catalog failure behave predictably.
- The published app loads the catalog without the source checkout or the original PDF.

## 7. Decisions for the implementation pass

Recommended defaults are `/acronyms`, a Reference navigation group, all categories included, audit-query matching off, exact-case matches ranked first, and a packaged generated catalog. These allow implementation to proceed without deciding on a full-text search platform.

The design selects a C# builder, embedded catalog, and existing stylesheet. Remaining implementation choices are the CSV reader and precise responsive spacing. Links to a rule viewer or PDF remain future work. None requires inventing new definitions or changing the reviewed reference data.
