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
