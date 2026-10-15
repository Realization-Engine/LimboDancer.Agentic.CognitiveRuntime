# Operation Goodwood transition coverage

Baseline: 14 October 2026, following fe470551. This audit separates setup representation from combat support. It does not schedule new ASL rules.

## Source accounting

The source has 122 counters in 25 types: 68 Allied and 54 German. Eight Allied Typhoons remain off-map support, leaving 114 ground formations. All 24 ground types covering 114 source counters have selected-formation ASL conversion. No ground type lacks a selected-formation conversion. This is potential conversion coverage, not a claim that all 114 have been deployed or transferred.

The adjacent JSON inventory enumerates every source instance and pins the source and composition files. Regenerate with `python src/Campaign/scripts/audit_goodwood.py`; verify with `--check`.

| Source type | Source counters | Principal objects per formation | Current transition |
| --- | ---: | ---: | --- |
| uk-rifle | 12 | 4 squads (game default) | british-squad |
| uk-engineer | 2 | 4 squads (game default) | british-elite-squad |
| uk-25pdr | 3 | 6 | campaign-british-25pdr + british-crew |
| uk-sexton | 3 | 6 | campaign-british-sexton |
| uk-achilles | 2 | 4 | campaign-british-achilles |
| uk-cromwell-goodwood | 4 | 4 | campaign-british-cromwell |
| uk-sherman | 20 | 4 | 3 campaign-british-sherman75 + 1 campaign-british-firefly |
| uk-typhoon-rockets | 8 | off-map | retain off-map air support |
| uk-bren | 14 | 6 | campaign-british-bren-carrier |
| de-rifle | 15 | 4 squads (game default) | attacker-squad |
| de-75mm | 3 | 4 | campaign-german-75mm-at + attacker-crew |
| de-88mm | 4 | 4 | campaign-german-88mm-at + attacker-crew |
| de-20mm | 3 | 6 | campaign-german-20mm-aa + attacker-crew |
| de-quad20 | 2 | 3 | campaign-german-quad20-aa + attacker-crew |
| de-81mm | 2 | 6 | campaign-german-81mm-mortar + attacker-crew |
| de-nebel | 2 | 6 | campaign-german-nebelwerfer + attacker-crew |
| de-wespe | 2 | 6 | campaign-german-wespe |
| de-maultier-goodwood | 1 | 6 | campaign-german-maultier |
| de-hummel | 1 | 6 | campaign-german-hummel |
| de-pziv-w | 2 | 4 | campaign-german-pziv |
| de-pziv | 3 | 5 | campaign-german-pziv |
| de-panther-w | 3 | 4 | campaign-german-panther |
| de-panther | 4 | 5 | campaign-german-panther |
| de-tiger | 3 | 5 | campaign-german-tiger |
| de-halftrack | 4 | 5 | attacker-halftrack |

Principal object counts are existing composition evidence, not ASL catalog approval. Crew-served weapons require separate crew identities in addition to these counts. Integrated vehicle crews and machine guns must not become free extra infantry or equipment. The Sherman formation explicitly contains three 75 mm Shermans and one Firefly. Existing generic tank, halftrack and gun definitions are candidates only until their designation and properties match. Missing variants need labeled manufactured profiles in existing ASL kinds.

Infantry currently receives zero separate equipment counters; source support inventories do not establish ASL allotments. The engineer elite-squad default does not establish sapper abilities. Make the final default allotment explicit without adding rules. German 81 mm conversion already supplies six crew/weapon pairs with MFG provenance and retained operator links.

Typhoons require eight stable off-map records, retaining source availability and the five-aircraft concurrent limit. Supply, HQ and depot markers require retained campaign references, not invented ground-unit identities.

## Map coverage and gaps

The three source boards contain 1,038 addressable hex fragments: 707 open, 240 woods, 52 water, 34 town and five marsh. The adapter already projects all three boards into one metric frame. Counts of source fragments do not imply resolved playable ownership at board joins.

| Area | Existing capability | Remaining work |
| --- | --- | --- |
| Joined boards B/A/C | Source rotations projected before tactical generation | Full package coverage and unique ownership across joins |
| Open, woods, water, town | Deterministic terrain and settlement generation | Whole-Situation verification, including board edges |
| Marsh | ASL terrain catalog already has code 14 | Generator currently rejects marsh; add versioned support without changing old saved maps |
| Elevation | Levels 0, 1 and 2 supported by generator v2 | Verify full coverage; retain existing rules boundary |
| Roads and streams | 26 road and eight stream routes in source | Preserve continuity across package boundaries |
| Bridges | Three authored bridge features | Verify joins, orientation and supported setup locations |
| Buildings | Seeded town parcels; source roof illustration is not directly imported | Preserve accepted deterministic geometry on reload |
| Tactical packages | Selected formation crop 17 by 14; bounded compiler maximum 65 by 40 | Plan full common-lattice coverage with explicit ownership |
| Setup admission | Two-hex guard at each crop edge | Resolve ownership separately from compilation margin, so adjacent packages do not leave playable gaps |

The current deployment filter admits open ground, paved road, woods and buildings; mortar/crew setup admits open ground and road only. Bridge decks and water/stream locations need explicit representation and supported setup handling. Do not bypass existing ASL validation or implement missing movement rules to make the audit green.

## Integration and acceptance gaps

1. Build a deterministic package index covering all source geography, with stable global cell IDs and one owner per playable cell. Keep compilation overlap separate from ownership.
2. Add marsh using a new pinned generation/profile version, preserving older references and generated packages.
3. Complete catalog mappings and explicit composition policies for each remaining ground family. Check both side and nationality, provenance, quantities, vehicle variants and crew relationships.
4. Assemble the complete deployment through existing ASL setup validation. Show unsupported representations individually, never silently remove them or label the entire roster ready.
5. Verify campaign export/import, Studio resume and repeated handoff preserve terrain, placements and identities. Existing Studio gameplay history must not be replaced by the initial campaign setup.
6. Measure full coverage generation and reopening. Pilot timing and package size are not evidence for whole-Situation performance.

The next implementation increment is the package coverage/ownership planner. The subsequent terrain increment adds marsh compatibility. Counter-family conversion follows the inventory above. Each increment needs its own verification and commit.

## G2 increment: ownership planner

`SituationCoverage.Plan` now enumerates all source polygons on the common ASL lattice. Each cell receives one ordinal parent and one 32-by-16 owning tile. Compilation crops add four columns and two rows on each side, with origins aligned to four columns and two rows. Margin cells do not acquire duplicate ownership. Source coverage and tactical terrain/setup legality remain separate concerns.

Two Release tests pass with warnings treated as errors: a separate whole-map scan verifies every covered center, all 1,038 source parents, all three boards, unique ownership and compilation margins; another verifies repeatability and cancellation. This index is not yet wired to the campaign UI or Studio. Edge crops still require generator support for unowned exterior margins, followed by package generation and persistence.

## G2 increment: marsh and exterior margins

The opt-in `goodwood-situation-1` generator uses a separate pinned profile with ASL marsh code 14. All five source marsh regions retain their polygons. Exterior compilation-margin centers may have no source parent; they remain unowned, not playable source locations. Existing pilot versions 1 and 2 retain their original profile bytes, identities and rejection behavior. The campaign pilot default remains version 2 until the whole-Situation workflow is integrated.

Three targeted Release tests pass: marsh features and tile-owned parent correspondence, version/profile isolation, and legacy deployment reload. No ASL movement or combat rules changed. Next: generate and publish every indexed package and record complete-map validation and performance.

## G2 increment: complete package publication

The CLI accepts `--situation` after the optional seed. It generates the coverage index, publishes each immutable package, then atomically publishes a content-addressed complete-set manifest. Cancellation can leave verified reusable packages, but cannot publish a partial set as complete. Reopening verifies the manifest identity, versions, coverage, ownership counts, feature packages, refinement metadata, catalog and compiled terrain hashes without procedural regeneration.

Full run with seed `00112233445566778899aabbccddeeff` on 14 October 2026:

| Measure | Result |
| --- | ---: |
| Packages | 84 |
| Owned tactical centers | 37,788 |
| Source parents represented | 1,038 |
| Generation, publication and per-package verification | 29.87 seconds |
| Complete-set reload and grid verification | 5.99 seconds |
| Peak process working set | 439,574,528 bytes |
| Stored output, including metadata | 47,040,289 bytes |

The accepted set is `b6ec6fbf0b964264f3c15e0999c223a61458b63d27a4957f3f79e4ccd128d020`. All 1,094 repeated metric-feature occurrences across packages match exactly. The 1,128 unique features include 204 elevation polygons, 297 terrain areas, 374 road segments, 127 stream segments, 123 generated buildings and all three bridges. These checks establish shared source geometry, not yet raster seam or browser acceptance.

All 89 Campaign Release tests pass, including cancellation after the first package and rejection of incomplete, incompatible and tampered set manifests. These are batch timings, not browser first-open timings. They exceed the original pilot targets when the entire Situation is processed synchronously. Load packages on demand in the remaining UI integration; do not advertise pilot timings for this full set.

Remaining G2 acceptance: raster seam comparison, viewport-driven package inspection and retained outer-boundary presentation. G3 still requires the 83 remaining ground source counters and explicit off-map support records. No complete-transition claim is made yet.

## G2 increment: raster seams

A complete-map Release test generates all 84 packages and compares terrain and elevation pixel spans across every horizontally and vertically adjacent package. It includes ownership boundaries while excluding one compiler-border hex. The test passes in 20 seconds. Source ownership, shared metric geometry and compiled raster seams are now verified independently. UI integration, demand loading and final save/navigation acceptance remain outstanding.

Next counter increment: German SdKfz 251 halftracks. The existing `attacker-halftrack` definition is SPW 251/1 and can be reused after setup validation. Existing `attacker-tank` is PzKpfw IIIH, so it must not substitute for Goodwood Panzer IV, Panther or Tiger formations. Those require distinct manufactured profiles.

## G3 increment: German halftracks

All four German halftrack source formations now convert to five distinct SPW 251/1 vehicles each, reusing the reviewed `attacker-halftrack` catalog definition. The integrated crew and AAMG remain vehicle capabilities; no free passengers, separate crews or equipment are created. IDs retain their source parent and vehicle ordinal. The bounded setup policy admits an unoccupied open-ground or paved-road hex. Initial facing is explicitly west as a game default for review in ASL setup.

Five handoff/reload cases pass through the existing ASL planner and replay, including the new halftrack case and unchanged infantry, engineer and mortar cases. A separate accounting test verifies all twenty halftrack identities. The browser-controller regression includes opening a halftrack formation and saving its context. This adds no vehicle movement or combat rules. Complete Situation assembly, remaining vehicle/gun profiles and final browser acceptance remain pending.

## G3 increment: crew-served guns

All 17 source formations across the six newly cataloged gun types now convert to 82 weapons and 82 distinct assigned crews. IDs preserve the source parent and ordinal; each weapon has exactly one same-side Manned relationship. British guns use the existing British 2-2-8 crew and German guns the existing German 2-2-8 crew. Counts follow the composition table; the one-crew-per-weapon allocation is an explicit game default, not a personnel census.

Gun/crew pairs move together during campaign setup and retain co-location through ASL setup and reload. Guns receive an explicit initial facing, east for Allied and west for German, for review in ASL setup. The bounded setup policy currently permits open ground and paved road. Eleven handoff/reload cases cover every implemented family, plus a separate test reconciles all 164 new identities. Browser-controller tests cover access and saved context for the new gun families.

The remaining 62 ground source counters are vehicles: British Sexton, Achilles, Cromwell, Sherman/Firefly and Bren carrier; German Wespe, Maultier, Hummel, Panzer IV, Panther and Tiger. These need twelve distinct game-default vehicle profiles, preserving the source's differing Wehrmacht/SS quantities without inventing a different vehicle model for those quantity variants. Eight Typhoons still need explicit retained off-map records in the complete handoff.

Verification for the gun increment: 98 Campaign tests passed in the full run; the sole failure was the old assertion that the now-supported 75 mm formation must be rejected. That assertion was changed to an unknown source ID and its targeted rerun passed. The catalog/source, integrity-pin and Studio checks for catalog 1.16.0 also passed.

## G3 increment: remaining vehicles

Catalog 1.17.0 supplies twelve labeled MFG vehicle profiles. Every ground source type now has a selected-formation conversion. The 114 source ground counters produce 624 distinct child identities: 116 infantry squads, 94 crews, 94 manned weapons and 320 vehicles. Each of the twenty Sherman formations produces three Sherman 75s and one Firefly. German W variants retain four vehicles per formation and their SS counterparts five; parent identities retain the source affiliation.

All 24 source ground-type handoff/reload cases pass the existing ASL setup validator and event replay. Roster accounting verifies unique IDs, no missing definitions, exact crew/weapon counts and Sherman/Firefly quantities. Vehicles keep integrated crews and weapons without extra passenger or equipment counters. The selected-map UI uses one supported-type list for all ground formations. Older halftrack setup policy and identities are retained.

This is complete ground conversion coverage, not full Situation completion. Off-map aircraft records, whole-Situation assembly, demand-loaded map UI, deployment preservation across packages and final browser/save acceptance still remain. The selected crop's bounded setup restrictions are not a substitute for validating a complete imported deployment.

## G3 increment: complete source roster snapshot

The versioned Situation roster accounts for all 122 source identities: 114 ground formations containing 624 ASL counters, plus eight individually identified off-map Typhoons. Aircraft availability (turn 1), the shared maximum of five concurrent aircraft, group and reference-only instructions come from the pinned source. These records do not invent ASL aircraft ground counters or air-combat rules.

Roster reload validates source identity, composition policies, catalog references, complete aircraft membership and limits. Missing or altered records and future roster versions are rejected rather than silently reconstructed. Equipment operators remain linked within their source formation. This snapshot is a building block for whole-Situation deployment integration, not yet a browser save or full ASL handoff.

Verification: all 115 Campaign regression tests passed, including full source accounting and roster serialization/rejection checks.

## G4 increment: world placement addressing

SituationAddresses provides one ASL board/hex address for each of the 37,788 owned cells. Tile compilation margins do not provide additional playable addresses. Existing accepted pilot placements can be translated through their original crop geometry into stable world column/row coordinates without auto-deployment or modification of the saved setup. Seed mismatches are rejected.

Two acceptance tests cover round trips across every owned cell, margin rejection, and mortar/crew coordinate preservation through JSON reload. This establishes coordinate continuity only: destination terrain, complete roster setup legality and existing terrain-version compatibility still require validation before whole-Situation admission.

## G4 increment: demand-loaded terrain

SituationMap opens the complete coverage manifest without compiling all 84 packages. Requested tiles load their saved features, verify package/refinement/catalog/grid identities, then derive ASL hex facts. A bounded LRU cache defaults to four tiles. Evicted tiles are revalidated on reload; cached objects represent accepted in-memory terrain. No procedural regeneration occurs.

Manifest acceptance alone does not prove all package files exist or are intact. Full release validation still uses SituationPublisher.Verify to inspect every package. Interactive tile access reports missing or corrupt packages when requested. Three tests pass for publication recovery and demand loading, including canceled access, cache eviction, missing packages, altered refinement data and protection against mutation of exported manifest arrays. Browser integration and full deployment admission remain outstanding.

## G4 increment: complete ground deployment assembly

SituationDeployment assembles all 624 ground counters against the verified whole-map terrain. Formation anchors are supplied by campaign setup; the adapter does not infer or change campaign-side deployment permissions. New squad positions are assigned deterministically inside each supplied parent hex. Preserved positions reserve their cells first and are never relocated to resolve conflicts. Missing capacity, incompatible terrain, changed anchors, opposing stacks and separated weapon/crew pairs produce explicit per-counter review records.

The campaign setup policy retains the existing bounded terrain/stacking admission: two personnel counters per hex, weapons co-located with their crews, vehicles alone, and vehicle/weapon detachments on open ground or paved road. These are adapter restrictions, not new ASL rules. A Ready draft still requires validation through the existing ASL setup implementation before handoff.

A full-map test generated all 84 packages and assembled a dispersed test deployment using 114 distinct open formation hexes. All 624 counters were assigned; all 94 weapon/operator pairs remained co-located. Reversed input ordering, JSON reload and preserved-position assembly returned the same draft. An anchor change retained every position and produced review; invalid identities, future policy and cancellation were rejected. The test passed in 96 seconds including full terrain generation and repeated assembly. This is not browser timing or proof for every possible campaign placement.

## G4 integration decision: existing ASL map format

The existing ASL composed-map layout accepts at most three board slots across. Goodwood demand loading uses seven tile columns and internal compilation margins, so importing the 84 tile boards directly would both exceed that limit and duplicate terrain at seams. The ASL transition will assemble one authored board from the accepted tile features, retaining one exterior guard and removing internal margins. This uses existing authored-board support without changing combat rules or VASL layout limits.

The adapter translates fixed-point geometry, preserves feature IDs and rejects disagreement among repeated features. It does not regenerate procedural detail. World-to-ASL positions use the fixed exterior origin (-4 columns, -2 rows). The campaign map remains demand-loaded. ASL full-board memory, rendering latency and browser usability must be measured before release; this integration choice is not evidence that those gates have passed.

Verification: full-map deployment/ASL-board integration passed in 195 seconds, including fresh tile publication, board assembly, full tile-core terrain/elevation pixel comparison, all 37,788 owned hex centers, repeat board identity and deployment reload. Shared fixed-point coordinates may differ by one 1/64-pixel unit because of crop-relative rounding; the adapter permits only that coordinate tolerance, requires identical semantic fields/topology, and deterministically retains the first accepted occurrence. Pixel-equivalence verification passed despite that quantization difference. No terrain regeneration or rules expansion was introduced.

## G4 increment: complete ASL setup handoff

The full handoff prepares the accepted authored board and all 624 ground counters through the existing GamePlanner setup action and event replay. Identity binds the campaign, world, accepted deployment, board, catalog and complete roster. Ground-relative positions translate from world coordinates using the exterior guard origin; existing vehicle/gun facing defaults remain explicit. The complete roster, including eight off-map aircraft, accompanies the handoff.

Map Studio imports can retain an optional immutable JSON campaign context beside their recorded games. Retrying an identical import reuses it; a conflicting context or existing initial setup is rejected. The context is published before game events so interrupted publication can be retried. Browser launch, campaign-file integration, return navigation and full-scale UI/performance acceptance are still outstanding.

Verification: the complete handoff, import, phase advance, fresh-service reload and repeat import passed, alongside the existing engineer and mortar import tests (3 tests). All 530 unit instances and 94 equipment instances retained catalog references and world positions; all 94 operator links and eight off-map aircraft context records survived. A conflicting campaign context was rejected without overwriting the accepted context or later game events.

Performance limitation: the combined full test took 15 minutes 21 seconds, including fresh terrain publication, raster comparison, repeated assembly, full-board validation and multiple imports. Sampled test-process working set reached about 4.77 GB; this is an observation, not a measured peak or single-open browser timing. The one-board transition is functionally verified but needs profiling and full-scale browser acceptance. Its exterior compilation guard is not yet restricted by an ASL playable-area definition; this must remain explicit until the boundary integration is resolved within existing ASL capabilities.

## G4 increment: portable accepted terrain

SituationBundles exports all 84 accepted packages, their refinement records and the pinned terrain catalog. Restore verifies manifest coverage, package/refinement/catalog identities and every compiled grid before publishing the complete directory atomically. It never calls the generator. Identical restores reuse a verified published copy; unsupported versions, missing/duplicate entries, altered terrain and cancellation fail without publishing a partial map.

The full portable-terrain test passed in 114 seconds, including fresh generation, JSON round trip, exact re-export equality, repeat restore and rejection checks. The serialized terrain bundle fits below 50 MB. This is the terrain component of a campaign save; browser campaign-file wiring, deployment/context persistence and storage-quota handling remain to be integrated.

## Full ASL board performance baseline

The CLI `--profile-asl-board <source-json> <accepted-manifest>` measures accepted-map assembly, compilation, derivation and validation without regeneration. On the saved coverage artifact with manifest hash b6ec6fbf0b964264f3c15e0999c223a61458b63d27a4957f3f79e4ccd128d020: assembly 11.91 s, compilation 1.07 s, derivation 2.16 s, validation 17.97 s. Validation allocated 26.69 GB cumulatively; process peak working set reached 6.36 GB. These are command-line stage timings, not browser load measurements.

Baseline board version f8674fed1f3b2e0cbaff7b739ab8da2646fff6afff33fd77aadc594249712f14; compiled grid SHA-256 c9173537e2c67e1d087b034178920f6e8a769e9bd7323c753dbf78304c6580bb. The board has 40,441 cells including exterior guards, with 0 validation errors and 4,824 warnings. Inspection found full-grid coverage allocation for each building in BoardValidator; bounded temporary coverage is the next optimization candidate.

### Bounded validation coverage

BoardValidator now uses the existing integer rasterizer with temporary coverage clipped to each shape's bounds, including building footprints and elevation support regions. No validation or combat rule changed. Eighty deterministic fractional/clipped/hole cases match every pixel of the original full-grid coverage; a 10 by 10 pixel footprint on a 12,000 by 12,000 grid stores 100 flags. All 42 targeted raster/compiler/authoring/validator tests passed.

Repeating the same full-board profile: assembly 10.36 s, compilation 1.02 s, derivation 1.83 s, validation 12.01 s. Validation allocated 7.84 GB cumulatively (down from 26.69 GB), and process peak working set was 1.92 GB (down from 6.36 GB). Board version, grid SHA-256, cell count and validation totals were unchanged. These single-run measurements establish a useful reduction, not completion of the browser responsiveness gate or an explanation of all time in the 15-minute integration test. Next: whole-Situation host/browser and campaign-file integration, plus remaining full handoff profiling.

## G4 increment: persistent whole-Situation preparation service

The local campaign host now exposes background preparation, status/cancellation, immutable saves, portable terrain/deployment export and import, ASL handoff preparation, and background Studio launch under `/api/asl-situation`. Terrain and full snapshots remain on disk; browser integration can retain small content-addressed references. Saves bind campaign identity, seed, accepted terrain, the complete roster, deployed positions and an opaque JSON campaign context for formation/supply information. Full handoff identity now also binds that context.

Reopening a saved deployment reviews the same positions against current formation anchors; it does not auto-fill gaps or relocate accepted counters. Changed anchors create review findings and a new immutable snapshot, retaining the previous one. A new deployment can reuse complete accepted terrain for its seed; an incomplete generation has no discoverable complete manifest. Imported terrain is restored and verified without regeneration. Missing/corrupt saves and missing accepted terrain produce recovery messages.

The full service acceptance test passed in 107 seconds: 624 counters, eight off-map aircraft, service restart, identical-save reuse, changed-anchor review with unchanged positions, portable transfer to a fresh store, wrong campaign/version rejection, cancellation and corrupted-save rejection. Host build: zero warnings/errors. Live API checks passed for invalid input, missing saves/handoffs and cross-origin rejection. The launch endpoint is compiled but its complete browser-to-Studio path still needs end-to-end acceptance. Cancellation of launch stops waiting; an ASL import already executing may finish, and an identical retry preserves that game. Browser controls and campaign-file wiring are the next step.

## G4 increment: campaign browser entry and portable file wiring

The Formation Map now opens complete Goodwood setup using all 114 ground formation anchors. A searchable roster shows all 624 counter identities, catalog definitions, positions, operator links and review findings. Preparation runs in cancellable background jobs. Keyboard Escape returns focus to the original toolbar control. The handoff action remains disabled while deployment review is outstanding.

Campaign file export attaches accepted terrain and deployment from the host; import verifies and restores those attachments before installing the browser campaign. Browser storage retains only small save references. Restored files discard host-specific prepared handoff links, allowing preparation on the destination host. Export uses compact JSON and checks the same 50 MB limit as import.

Verification: new browser-module tests and existing campaign-file tests passed, including exact formation coverage, supply context, portable restoration, legacy files, invalid attachments, failure and cancellation without changing the input campaign. Live browser inspection confirmed searchable mortar/crew links, disabled incomplete handoff, and keyboard return/focus restoration. Full file download/upload and ASL launch remain acceptance work.

The live committed Goodwood deployment exposed an important integration gap: 534 counters received positions, while 90 needed review because the current conservative terrain/capacity admission policy could not place them inside their parent formation hex. No counter was silently dropped or moved outside its parent. The next step is to reconcile that policy with existing ASL setup admission and provide explicit recovery for accepted incomplete deployments. This finding prevents claiming a complete playable transition.

### Accepted deployment diagnostic

The CLI `--profile-deployment <source-json> <accepted-manifest> <Situation-save-json>` reviews candidate placement using an accepted snapshot without writing to terrain or saves. It reports preserved/placed counts, position equality and terrain counts for each formation still needing review. The CLI dependency lock now includes its existing transitive unit/catalog dependencies; an outdated restore manifest had omitted them at runtime.

On live save 23ca721fb27a0e0d3462ccdb22de20ce0c61d2dd70eb1cf1230ca5761cbfc720, the original policy retained 534 placements and reported 90 missing counters across 18 wooded parent hexes. Some parents had only two road/open cells, most were entirely woods. ASL VehicleSetupBar already admits woods and the setup rules support manned guns in woods, so the campaign adapter's open-ground/road-only detachment rule is unnecessarily restrictive. A woods-admission correction and full ASL import acceptance are in progress. Saved gaps must be filled explicitly, preserving the accepted positions, rather than implicitly on reopen.

### Woods setup admission and explicit recovery

Campaign setup now admits vehicle and weapon detachments in woods already supported by ASL setup. The existing elevation, bridge, other-terrain, parent ownership and stacking checks remain. On the accepted live campaign snapshot, all 624 counters could be placed with zero review findings while preserving the 534 existing coordinates. Terrain was not regenerated or modified.

The full terrain/deployment/handoff/Studio test passed in 10 minutes 14 seconds with a manned 88 mm formation and a Panther formation placed in wooded parents. It verifies ASL setup, all counter identities/positions/catalog references, operator links, phase advance, reload and idempotent re-import. This is combined test duration, not browser opening latency.

An explicit Place remaining counters action creates a new immutable save while retaining every accepted coordinate. A missing weapon can join its retained crew; a wholly missing crew/weapon group receives a legal shared position. Conflicting or invalid retained positions remain review findings. Ordinary reopen and file restore still do not fill gaps implicitly. Recovery clears stale prepared-handoff links in the browser.

The service persistence/recovery test passed in 2 minutes 23 seconds: missing pair and missing weapon recovery, unchanged terrain/roster/context and accepted coordinates, old snapshot retention, idempotent repeated recovery, invalid retained-position preservation, portable reload and cancellation. Browser module and campaign-file tests also passed. Live browser recovery and full launch/return/resume acceptance remain next.

### Live browser recovery and initial import

The updated campaign host reopened the original incomplete live save with 90 gaps unchanged. Selecting Place remaining counters produced save 7c12837c0e4b62229cb233ec4010d9391e8928f1f862f3634701f89419e8158b: 624 positions, zero review findings. Direct comparison with the old snapshot confirms all 534 old coordinates and the terrain manifest, roster and campaign context are identical.

Prepare and open ASL setup reached Ready in approximately 332 seconds, observed by browser polling rather than instrumented stage timing. The generated handoff is bd6932874e5285982bb0d26274f83baa5fea64cd24656af15e00d0c1abaf1cb0; Studio game campaign-d52f7b3704cf7cd4d56818f1 opened at its Allied hand-over screen. The popup appeared after a long delay; a duplicate diagnostic tab was closed. Initial page navigation caused observation timeouts before eventually loading. Selecting the Allied view still requires rendering acceptance. These results verify live recovery and import, not full map usability or the performance target.

### ASL campaign provenance and return navigation

The ASL Play context now includes a compact Goodwood campaign panel with imported formation/counter totals, off-map aircraft accounting, and an explanation that supply/command markers remain in campaign setup. Return navigation opens Campaign Atlas without modifying the ASL game or campaign deployment. The destination comes from the local Studio configuration `Campaign:AtlasOrigin` (default http://127.0.0.1:8093), never from an imported save URL. Unknown or damaged context does not prevent ordinary play.

Sixteen targeted context and card-editor tests passed with warnings treated as errors. The newer SDK also exposed an existing undisposed JSON document in a card-editor test; it now has an explicit lifetime. Live rendered return/navigation acceptance remains outstanding.

### Pinned board lookup performance

StudioBoardCatalog now resolves a requested terrain version through the existing LoadVersion API. Previously every rules lookup loaded and hashed the current authored package before checking its version, even when the accepted version was already compiled in memory. Unversioned lookups still read the latest board; an unavailable pinned version remains an error. A regression test verifies accepted-version reuse, changed-current isolation and refusal of an unavailable version. Thirty-eight targeted catalog, render-endpoint and play-page tests passed. Full Goodwood browser timing after this change is still pending.

Live inspection confirmed the campaign provenance totals and return link. Its styles were moved into the existing site stylesheet because this application does not include a component-scoped stylesheet bundle.

### Live return navigation and restart observation

After rebuilding and restarting Studio, the existing campaign-d52f7b3704cf7cd4d56818f1 game reopened at Turn 1 Allied Rally Phase with setup open. The new disclosure reports 624 ground counters, 114 formations and eight off-map aircraft. Its Return to campaign map link opened the retained Goodwood campaign at port 8093 with Committed / Supplies issued and CMD, DEP, SUP, HQ and TRK markers visible. No game or campaign state was rewritten by navigation.

The restarted hand-over screen was observed after approximately 173 seconds elapsed, including polling and inspection gaps. This is not an instrumented time-to-interactive measurement and does not establish a performance improvement. Opening the Allied full-map view remains under acceptance; stage-level profiling is needed before the performance gate can pass.

### Full-map replay profiling and optimization

Optional information-level stage logs now separate stored-game read, terrain compile/derive/validate, replay including board load, and SVG rendering. On the live saved Goodwood game before the replay correction: read 58.7 ms; compile 2.176 s; derive 2.485 s; validate 13.499 s; replay including board load 103.582 s.

GameProjector was scanning opposing units for every active counter after every setup event, even when the counter was not in Melee. The existing LeavesMelee rule is false for those counters; checking that condition first avoids the unnecessary scan without changing rule results. After the correction on a fresh Studio process and the same save: read 50.1 ms; compile 1.817 s; derive 2.244 s; validate 12.421 s; replay including board load 18.289 s (about 82 percent lower). A repeated replay against the retained events took 0.342 ms. These are service-stage timings, not end-to-end browser timings.

All 416 unit-state tests and 37 Studio play/render tests passed. An existing fixture assertion hardcoded catalog 1.15 while using the current catalog hash; it now verifies the actual pinned catalog identity. Full-map rendering has been visually confirmed; interaction, portable later-game progress and remaining release gates are still outstanding.

### Full-board outline tracing memory and time

The outline tracer now retains boundary cells only while counting every component cell. Interior cells do not contribute edges and no longer require a second full-region scan or a retained list. Ring starts use a forward scan of the original boundary vertices instead of restarting dictionary enumeration after each hole. Pixel geometry, component IDs, elevations, ring winding and canonical ordering are unchanged.

A Release-mode comparison ran the previous committed tracer and the updated tracer against the same compiled accepted Goodwood board `ab-goodwood-full-e19998371d0e6f8d9dfc1160e0fbe92e6713ce06cc97a327d325219521b43715`: 145,653,300 pixel cells and 370 terrain/elevation components. In original/updated/updated/original order, tracing took 8.028 / 6.033 / 6.176 / 7.288 seconds. Managed allocations fell from approximately 3.170 GB to 1.237 GB, about 61 percent. These are cumulative allocations, not peak resident memory, and exclude board compilation and SVG formatting. The benchmark explicitly collected garbage between runs; these timings are not browser opening measurements.

All four serialized region outputs had SHA-256 `5a142d5d0839e34565957aa052686765e58ae1b2a58cb6f1d9796c27c4013165`. This verifies equality against the previous implementation on the complete accepted board. A regression with hundreds of islands and shared pinch vertices checks pixel coverage and hole handling. All 248 map-domain tests and 34 rendering tests passed; two external-ingestion rendering tests were skipped because their external fixtures were unavailable. The earlier live 34.578-second SVG render has not yet been remeasured with this change. Browser interaction, subsequent ASL progress transfer and final release acceptance remain open.

### Live full-map inspection acceptance findings

On the existing full Goodwood game, keyboard `+` changed the SVG viewBox from `-16 -16 11732 12501` to `1157.2 1234.1 9385.6 10000.8`; keyboard `0` restored the fit viewBox. The browser automation reported focus-command timeouts, but subsequent DOM observations confirmed both actions took effect. Rotate map changed its pressed state and applied the rotated map class.

Selecting British crew `uk-25pdr-01--crew-1` at VVVVVV30 displayed its 2-2-8 profile, identity, Allied game side, Good Order and terrain inspection (base level 2, Open Ground, six hexside entries). Selecting colocated gun `uk-25pdr-01--gun-1` displayed its identity and facing. The inspector lists both counters in the hex but does not explicitly display the existing manning relationship. This is an open UI acceptance defect, not evidence that the saved relationship is missing. The inspector should expose that relationship using only the current perspective's visible state. These interaction checks used the already-running Studio build before the outline optimization was deployed.

### Explicit operator inspection and updated live render

The ASL inspector now shows Operates / Manned by for the existing manning relationship, Carries / Carried by for possession, and Tows / Towed by for towing. Both directions read only equipment and units in the projected GameView. Hidden holders are not resolved through a name callback, and released or inactive equipment no longer appears as operated. No rules or saved state changed.

All 42 targeted inspector, play-page and render-endpoint tests passed. Five inspector cases cover the three relationship roles, hidden-holder privacy and released/inactive equipment. After restarting Studio and reopening the full saved Goodwood map, selecting crew `uk-25pdr-01--crew-1` showed Operates `uk-25pdr-01--gun-1`; selecting the gun showed Manned by that same 2-2-8 crew. The prior inspection defect is resolved.

On this fresh Studio process, stage logs recorded compile 1.417 s, derive 2.118 s, validate 9.957 s, replay including board load 15.004 s, and full SVG generation 13.403 s. The latter is lower than the earlier 34.578 s observation; this is a service-stage comparison rather than a controlled end-to-end browser benchmark. Browser observation still encountered timeouts while rendering, then the same tab became usable without another restart. Full campaign-file transfer of later ASL progress remains the next persistence gate.

### ASL progress transfer service

Campaign import accepts an optional complete progress record in addition to the independently validated initial setup and exact board package. The progress prefix must equal the accepted setup, its game identity must match, and its entire history must replay through the existing ASL implementation before publication. A fresh store receives the full history; a matching shorter local history receives only its continuation; a longer local history is retained. Divergent histories are rejected without replacement. The existing atomic, revision-checked event store remains the commit boundary. Cancellation is checked before validation work, board publication and the game append.

A local server-to-server endpoint exports the complete campaign game record with no-store caching and the same loopback/no-Origin boundary as import. It is not a player-perspective view. The progress payload is limited to 20 MB. Campaign-file capture and restore are not yet connected to this service, so this checkpoint does not close portable-progress acceptance.

Two expanded integration cases passed, covering British engineer squads and German 81 mm mortar/crew pairs: advance phase, export, restore into an empty store, replay the restored revision, repeated restore, older initial setup without rollback, continuation of an existing setup, conflicting-history refusal, malformed progress and cancellation before publication. Accepted campaign deployment and terrain remain unchanged. The full Goodwood transfer and actual campaign-file UI remain to be verified after integration.


### Campaign-file ASL progress integration

Campaign-file capture now attaches the launched ASL game's complete history and its independently verifiable prepared handoff. The host checks that the handoff belongs to the exported campaign save. Missing or unavailable launched-game history prevents export instead of silently producing a deployment-only file. Older files without progress remain supported.

Restore reconstructs the handoff from the transferred accepted terrain and deployment, rejects any mismatch before calling Studio, then passes the full history through Studio's existing replay and prefix validation. Only a successful job publishes the immutable campaign save and local prepared/resume references. Existing local history is not rolled back. Ordinary deployment saves retain their small reference format.

The host Release build passed with zero warnings/errors. Browser-module checks passed for progress attachments, local resume metadata, older files, missing history, missing handoff references, unsupported versions, unsafe resume URLs, failed restores and cancellation. Three integration cases passed in 4 minutes 44 seconds, including full Goodwood terrain/deployment transfer with deterministic handoff reconstruction and mismatch rejection, and the two Studio history/restart cases.

A live HTTP export of the accepted 624-position Goodwood save took 17.077 seconds and produced 31,390,922 bytes including 580,107 bytes of ASL history. The actual Save campaign to file button produced a 32,769,662-byte campaign file containing the same complete Situation attachment. These measurements occurred alongside integration tests and are observations, not controlled performance benchmarks. Actual interface restore is under verification; this checkpoint does not yet close that acceptance gate.


Live restore follow-up: the exported campaign file was loaded through Load campaign from file and its confirmation dialog. The host reconstructed the handoff and returned to Campaign Atlas. Open complete ASL setup showed 114 formations, 624 counters, zero review findings, eight off-map aircraft and an Open ASL map link to the original campaign-d52f7b3704cf7cd4d56818f1 game. Following the link reopened Turn 1 Allied Rally Phase with setup open. The exported and retained live ASL histories are byte-identical (SHA-256 b106c5ad14cee682b679e86e752f980f46cd40456a89a84913c4d3da58b6af93). This verifies the live full-map setup-history round trip; later-phase history is covered by the two Studio integration cases, not yet by a full-map live later-phase transfer.


### Persistent map view acceptance

ASL map pan/zoom now survives page reload and browser navigation through local view storage scoped to game, viewing side and exact board render version. Ordinary views keep their in-page fallback when storage is blocked. Invalid stored boxes are ignored; explicit Fit is retained. A newly loaded or changed board does not reuse a different board version's position. Initial setup-area framing no longer overrides a restored view.

Seven viewport JavaScript tests and 37 targeted Studio play/render tests passed. On the actual full Goodwood map, Zoom in produced viewBox `1157.1999999999998 1234.0999999999995 9385.6 10000.800000000001`. Reloading and reopening the Allied view restored precisely the same value. The browser observation timed out during full-map drawing, then inspection of the same tab succeeded without restarting it. Screenshot: goodwood-retained-map-view.png. This proves live page-reload view retention; it is not a performance-target pass. Parent-boundary inspection and the final full-map resource/performance audit remain open.


### Counter provenance inspection

The ASL selected-counter inspector now shows parent formation, original formation hex and catalog reference from the immutable campaign handoff, plus the reviewed/manufactured profile status. The original hex is labelled "Formation hex at handoff" rather than presented as a current ASL location. Provenance is resolved only for a unit or equipment instance visible in the selected perspective. Missing, unsupported or inconsistent context produces no invented origin and does not block ordinary inspection. This uses existing saved context and changes neither terrain nor ASL events.

Forty-nine targeted Studio provenance, inspector and play/render cases passed. An additional full Goodwood roster test verified all 624 stable ASL IDs against their formation parents, catalog definitions and profile statuses. The coverage inventory audit passed. Live full-map inspector acceptance is still pending while the same browser tab finishes rendering. Per-hex ownership and parent-boundary overlays are separate remaining work; counter origin does not claim to provide them.


### Full-board ownership metadata foundation

SituationMapOwnership now projects the stable formation ownership lattice into the assembled ASL board's hex addresses. The optional metadata records the exact board identity/version and groups tactical hexes by parent. It does not change terrain, accepted setup, campaign context or game events. Exterior compilation guards are intentionally absent from the ownership map; absence does not itself enforce a play restriction.

Two Release tests passed with warnings treated as errors. They inspect every hex in the full board geometry, verify 37,788 unique owned centers across 1,038 parents, prove guard exclusion and unchanged feature bytes, check deterministic JSON/save reload, reject tile-sized geometry and honor cancellation. The test board uses the real Goodwood ownership geometry with an empty feature model, so this is ownership/address validation, not a new terrain-render acceptance claim.

Next: transmit and validate this separate metadata alongside the existing immutable handoff, retain it safely on repeated import, expose parent ownership in terrain inspection, and render optional parent boundaries. Actual exterior play restriction remains a separate acceptance item.


### Ownership handoff and hex inspection

The campaign host now computes ownership metadata from the full accepted board when launching or restoring a handoff. Studio validates the exact board/version, schema version, canonical in-bounds hex addresses, unique parents and unique ownership before publication. The optional metadata is stored atomically in a separate immutable sidecar. It can be attached to an older accepted game; older callers cannot erase it, and conflicting replacements are refused. Terrain, context and game-event identities remain unchanged.

Selecting an ASL hex now shows its formation parent in the existing inspector. An unowned hex is labelled outside campaign ownership. This inspection label is not an engine play restriction. Parent-boundary drawing and exterior movement restrictions remain open.

Thirty-one targeted Studio metadata/play-page tests passed. Three Campaign integration cases passed in 2 minutes 41 seconds, including the full Goodwood map and all 624 counters, every one of the 37,788 ownership entries, preserved progress on repeated import, restart retention, attachment to older games and conflict rejection. Actual full-map inspector acceptance is in progress.


Live ownership acceptance: the existing full Goodwood save was reopened through Open Situation map, Open complete ASL setup and Recheck ASL handoff. It retained 114 formations, 624 counters and zero review findings. The 387,498-byte sidecar contains all 1,038 parents and 37,788 hexes. Opening ASL and keyboard-selecting de-rifle-01--squad-1 at MM150 displayed both the counter's original formation hex C/AA-4 and the selected hex note "Formation parent: C/AA-4". Screenshot: goodwood-hex-parent-inspection.png. The normal repeated handoff attached metadata without changing the game identity. This also supplies live counter-origin evidence for that squad; earlier unit tests cover all 624 origins.

This run's map document generation took 15.353 seconds, following 1.529 seconds compile, 1.760 seconds derive and 10.103 seconds validation during repeated handoff. These are diagnostic stage observations, not controlled cold-opening measurements or a performance pass. The saved map view was rotated, as retained from prior interaction. Parent boundary drawing and final performance acceptance remain open.


### Optional formation boundary overlay

ASL Play now offers a Formation boundaries toggle when validated campaign ownership is available. It draws parent transitions and the owned perimeter in one cached SVG path, omits edges within a parent, and draws shared transitions only once. The overlay ignores pointer events and uses a non-scaling stroke. Existing setup marks are retained. The cache is scoped to game and exact displayed board version, and inspection refuses metadata for another displayed version.

Thirty-four targeted Studio tests passed, including same-parent suppression, distinct-parent shared-edge deduplication, expected ASL vertex placement, deterministic output across input ordering and decimal-comma cultures, empty ownership, metadata validation and existing play-page behavior. Live full-map toggle, map-position preservation and visual acceptance remain under verification.


Live boundary acceptance: the existing Goodwood game reopened with the Formation boundaries control. Its complete overlay contains 24,951 segments in one SVG path (706,950 characters of path data), with non-scaling stroke and no pointer interception. Keyboard toggling removed/restored the layer. A directly measured off/on check preserved viewBox `5627.423627061753 4576.132985952769 716.064453125 763.0004882812501` exactly. An earlier observation saw the view scale change between reads and was not used as evidence of preservation. Screenshot goodwood-formation-boundaries.png shows the enabled overlay and counter inspector. Exterior playable-boundary enforcement remains open; the overlay alone is visual.


### Playable footprint validation foundation

BoardHandle now accepts an optional immutable set of location hexes. Resolve, terrain-location reads, neighbors and distance reject excluded cells; the complete terrain facts and geometry remain available for rendering and LOS. Ordinary boards retain their existing location area.

A planner test exposed that replay previously reduced board handles to unrestricted terrain facts. The planner now retains handles in its location-chain adapter, so the existing event projector also checks the footprint. Nine targeted tests passed, including an accepted interior setup and refusal of an exterior setup. The extended test also checks replay of an older unrestricted exterior placement. All 820 Play regression tests passed in 8 minutes, including rejection on replay of the older exterior placement. The Maps suite previously passed all 251 tests.

This is a foundation checkpoint, not live Goodwood perimeter acceptance. The authoritative footprint must still be published separately from optional inspection metadata, validated against retained history, and loaded consistently by import and live play. Partial inspection metadata must never silently restrict an existing game. Terrain version, accepted deployment and game identity must remain unchanged; incompatible retained history must be refused without overwriting it.


### Explicit playable-footprint integration (verification in progress)

The full Goodwood host requests enforcement explicitly. Studio stores the validated area separately from display-only ownership metadata and accepted terrain. A board/version-specific file is published by atomic rename and cannot be replaced with a different area. Import replay uses the proposed area before publication, and checks retained games that use the board before attaching a new restriction. Older requests retain an already accepted footprint. Live board reads use the same persisted area and replace previously unrestricted handles without changing terrain facts or board version.

All 13 targeted ownership, storage and board-cache tests passed, including immediate enforcement after publication and restart. All three campaign integration cases passed in 3 minutes 54 seconds, including the complete Goodwood import, exterior counter rejection before publication, all 624 accepted counters, phase advancement, repeat import and restart. Actual-interface perimeter acceptance remains outstanding. No live game has yet been migrated by this change.


Live migration checkpoint: after restarting Studio and Campaign, the normal complete-ASL dialog showed 114 formations, 624 counters, zero review findings and 8 off-map aircraft. Recheck ASL handoff published the 387,498-byte explicit footprint containing all 1,038 parents and 37,788 owned hexes. The accepted game history SHA256 remained `b106c5ad14cee682b679e86e752f980f46cd40456a89a84913c4d3da58b6af93` before and after migration. Open ASL map reopened the same game and displayed Allied side selection.

Visual play acceptance remains incomplete: after selecting the Allied view, browser observations timed out during full rendering. Studio reported 40.971 seconds for document generation in this run, versus 1.007 seconds for the first warm-board history replay. This is a diagnostic observation, not a controlled performance benchmark, and is a practical release issue to investigate. Do not treat the reopened link or successful migration as proof of responsive play. The services remain running for continued investigation.


### Full-map rendering optimization

The renderer reuses terrain regions, which already partition pixels by terrain and elevation, instead of tracing the entire elevation grid a second time. Game opening no longer generates an unused standalone SVG document before the viewport requests its layers. Renderer version 1.0.1 is included in layer requests to invalidate immutable browser responses while retaining the board/view key used for saved pan and zoom.

A controlled Release comparison on the accepted full Goodwood board measured standalone SVG generation at 32.961 seconds before the change and 10.626 / 10.631 seconds after it. Cumulative managed allocations fell from 3,124,497,504 bytes to 1,687,116,504 / 1,687,117,000 bytes. These figures exclude compilation, derivation and browser display, and are not peak resident memory or a browser-opening acceptance result. The final reverse-order baseline output was not recovered and is not included.

Terrain, grid, labels and legend layer hashes matched. Updated elevation SVG paths were rasterized independently: levels -1, 1 and 2 each matched all 145,653,300 source pixel cells. A synthetic regression also covers adjacent terrain regions, negative levels and holes. Rendering tests: 35 passed, two external-VASL-dependent tests skipped. Focused Studio viewport/play/replay/render checks: 37 passed. Golden SVG metadata and the equivalent elevation path representation were updated.

Browser responsiveness, actual perimeter interaction and later-phase campaign-file transfer remain open acceptance gates. The previous services were stopped for rebuilding; the earlier running-services note is historical.


Live optimized-render checkpoint: the retained full Goodwood game opened in Allied view with terrain and counters visible. Cold service logs measured compilation 2.221 seconds, derivation 2.260 seconds, validation 12.873 seconds, and history replay including board load 19.399 seconds. Layer generation measured exact terrain 8.509 seconds and elevation 8.470 seconds (concurrent requests sharing traced terrain); other layers were below 0.361 seconds. These diagnostic stages must not be added as if sequential or reported as browser opening time.

Broad browser observations initially timed out, but a subsequent screenshot and scoped toolbar/inspector observations succeeded. Keyboard activation of Formation boundaries set aria-pressed=true and preserved viewBox `5627.423627061753 4576.132985952769 716.064453125 763.0004882812501` exactly. Screenshot: goodwood-optimized-render.png. No browser warning/error logs were returned. This demonstrates eventual interactive rendering after restart, not a measured cold-open latency pass or proof of exterior movement rejection. Use scoped observations rather than traversing every map label during the remaining browser checks.


### Live later-phase campaign restore acceptance

The complete Goodwood game advanced through the ordinary proposal-and-confirm interface to Turn 1, Allied Prep Fire. Campaign Save campaign to file captured 626 events, including phase-changed revision 626 (`pfph`, Allied), in a 32,770,026-byte portable file. Compared with the previous setup export, the portable terrain and formation mission data are identical; the deployment save differs only in its restored terrain-manifest path. No counter was moved during this check.

Load campaign from file and its confirmation restored the exported file through the normal terrain and saved-progress validation stages. Open complete ASL setup reported 114 formations, 624 counters, zero review findings and eight off-map aircraft. Open ASL map retained game `campaign-d52f7b3704cf7cd4d56818f1`. A full page reload and Allied-view selection displayed Turn 1, Allied Prep Fire and the corresponding end-Prep-Fire proposal control. The retained history equals the exported JSON record, with SHA-256 `155cd7bc7870ca1d2582534edf567dca4ea3df49e59e28fdfb9bee00e7d485aa`. Screenshot: goodwood-restored-prep-fire.png.

This proves actual-interface later-phase restoration against the existing service and browser reload. It does not prove transfer into empty service storage; that remains a distinct acceptance check. One screenshot request timed out during rendering; a subsequent capture of the same tab succeeded. No browser-opening performance pass is inferred.


### Empty-storage portable transfer acceptance

Started separate Campaign (8098) and Studio (5198) processes with a previously nonexistent `artifacts/goodwood-transfer-acceptance` storage root and separate browser origin. The original services and saves were left intact. The new Campaign page initially showed an unstarted Goodwood operation. Through Load campaign from file and confirmation, imported the 32,770,026-byte Prep Fire export. No source service files were copied into the new store.

The restored complete-ASL dialog reported 114 formations, 624 counters, zero review findings and eight off-map aircraft, with its Open ASL map link targeting port 5198. Following that link and choosing the Allied view displayed Turn 1, Allied Prep Fire with the full map rendered. Screenshot: goodwood-empty-store-transfer.png.

Independent file comparison found byte-identical board.json, features.json, campaign context, parent ownership metadata, playable footprint and 626-event game history between original and reconstructed stores. The history SHA-256 remains `155cd7bc7870ca1d2582534edf567dca4ea3df49e59e28fdfb9bee00e7d485aa`. This closes the full-map portable later-phase transfer check against empty service storage.

Diagnostic new-service timings were compilation 1.632 seconds, derivation 1.897 seconds, validation 10.571 seconds, and subsequent warm-board history replay 0.587 seconds. Terrain/elevation layer requests took 7.716 / 7.560 seconds with shared work. These are stage diagnostics, not an end-to-end timing benchmark. Actual perimeter interaction, final deployment compatibility review and controlled performance/resource acceptance remain open.


### Full-map typed-location compatibility

Review during perimeter acceptance found that DisplayText.ReadPlace limited short input to two column letters and two row digits. Goodwood's assembled ASL map uses longer repeated-letter columns and three-digit rows. The parser now accepts the coordinate range supported by HexName, validates it through BoardLocation, normalizes lowercase published/authored identifiers, and reads the authored-board wording emitted by DisplayText.Place. Invalid names and ambiguous boardless input on several boards remain unaccepted.

All 40 focused LocationWords tests passed with warnings treated as errors. The new full-map case round-trips every one of the 40,441 Goodwood geometry addresses in both single-board and multiple-board display forms; it also checks lowercase input, levels, authored-board words, malformed names and ambiguous board selection. The Goodwood inventory audit and diff whitespace checks passed.

The live isolated game reopened after the service restart in Allied Movement Phase, retaining the previously accepted history. Browser automation entered a long destination, but the movement form did not enable reliably after field entry; live typed-input acceptance remains unresolved. No movement was confirmed. The earlier exterior A1 proposal was refused as a non-adjacent/unreadable Location, which alone does not isolate playable-footprint enforcement. No accepted counter is adjacent to the outer guard; an initial-setup copy is the next suitable actual-interface perimeter test.


### Actual-interface playable perimeter acceptance

Restored the earlier 625-event initial-setup campaign export into a separate previously empty `artifacts/goodwood-perimeter-acceptance` store, preserving both original and later-phase transfer stores. The complete Goodwood ASL page displayed Turn 1 Allied Rally with setup open. A British squad was staged through the existing setup editor, then its destination was selected through the map.

At exterior guard A1, Propose setup was refused with `UNIT-STATE-010 revision 626: A1 is not a hex on` the accepted full Goodwood board. The persisted footprint independently excludes A1. Selecting owned interior CCCCCCC13 for the same staged counter and proposing again passed every check and offered Confirm. Setup admission has no adjacency requirement, so this paired actual-interface check isolates the accepted location area from the earlier non-adjacent movement rejection. Screenshots: goodwood-exterior-setup-refused.png and goodwood-interior-setup-accepted.png.

The interior proposal was cancelled. No setup placement or movement was confirmed. The accepted history still contains 625 events and retains SHA-256 `b106c5ad14cee682b679e86e752f980f46cd40456a89a84913c4d3da58b6af93`. This closes actual-interface exterior setup admission acceptance, supported by the existing engine/import/replay footprint checks. It does not claim a live adjacent movement crossing was exercised.

Browser typed-input automation remains unresolved: selecting definitions and map locations worked, but entered text did not reliably reach the staged form state. The perimeter result uses map selection and a generated temporary counter ID. The full-map typed-coordinate parser itself passed all 40 focused tests in the prior checkpoint. Final compatibility, performance/resource and release-audit work remains open.


### Keyboard location submission and compatibility recheck

LocationField now applies typed input explicitly on Enter as well as on its existing change event. In-progress typing stays local, without normalizing every keystroke. A pending edit is not applied if an intervening external value change has superseded it. All 41 focused LocationWords tests passed, including full-map parsing, keyboard submission and stale-input rejection.

Actual-interface acceptance on the complete Goodwood initial-setup copy entered the fully qualified address ending `:ccccccc123:0` and pressed Enter. The field displayed normalized `[CCCCCCC123] on` the accepted board, and Add staged its canonical address. Propose setup passed every check and offered confirmation. The proposal was cancelled and the draft cleared. No placement was committed; history remains 625 events with SHA-256 `b106c5ad14cee682b679e86e752f980f46cd40456a89a84913c4d3da58b6af93`. Screenshot: goodwood-typed-location-accepted.png.

The setup editor intentionally has no active map-picking board context, so its short address alone is not resolved. The full board-qualified form succeeds. Earlier short-form browser observations therefore do not prove an input failure. Movement fields with one known board and the parser regression retain short-address support; the setup row's map picker is also available.

Nine selected Campaign compatibility tests passed without rebuilding the running services: older catalog positions/operator links, complete and incomplete legacy mortar migration, pair movement/stacking, infantry catalog metadata preservation and accepted legacy terrain identity. The coverage inventory audit and whitespace checks passed. This supplies focused compatibility evidence; the final full-map/manual-deployment evidence audit remains required.
