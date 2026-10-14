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
