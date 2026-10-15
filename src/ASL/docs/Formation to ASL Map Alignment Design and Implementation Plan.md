# Formation to ASL Map Alignment Design and Implementation Plan

Date: 13 October 2026.
Status: active goal expanded on 14 October 2026 to the complete Operation Goodwood Formation Map and counter transition into existing ASL setup. Full Goodwood terrain coverage, all 624 ground counters, equipment relationships, immutable saves and Studio import are implemented. Live return navigation is verified. Whole-Situation usability, performance, compatibility and portable ASL-progress acceptance remain incomplete. Dated checkpoints below describe their respective increments; the current scope and completion gates immediately below take precedence over earlier pilot-only restrictions and superseded next-step notes.
Branch: codex/formation-asl-map-alignment.
Baseline analysis: [Formation to ASL Map Alignment Analysis](Formation%20to%20ASL%20Map%20Alignment%20Analysis.md), committed at 452dad19.

## Outcome and scope

Deliver a complete transition of Operation Goodwood (Panzer Leader Situation 5), including its joined Formation Map and both sides' formation counters, into the existing ASL map and setup system. Deterministic terrain, stable counter identities and relationships, reliable navigation, and save/reload are the deliverable. A successful bounded crop is an intermediate result, not completion of this goal.

Reuse the current ASL rules implementation. Do not implement new combat, mortar firing, movement, transport, LOS or victory rules to complete this transition. An ASL combat capability that is absent remains a documented engine limitation, not an automatic reason to omit its map feature or counter from setup. Where the current setup model cannot represent an item, define an explicit retained campaign record or supported representation and expose that limitation; never silently discard it or mislabel it as another weapon. Such exceptions must be visible in the coverage audit and final acceptance report.

Retain nominal 250 m formation spacing and 40 m tactical spacing, subject to the existing coherence fixtures. Do not resize saved campaigns, generate the entire continent, or expand to other Situations in this goal. The 240 m comparison remains an offline fixture. The same seed and pinned inputs reproduce the accepted map; new campaigns may receive different seeds and permitted tactical detail.

## Current delivery plan: complete Goodwood transition

### Completion criteria

- **Whole Situation geography:** cover joined boards B, A and C in their authored orientations. Preserve roads, crossings, water, settlements, elevation and parent-hex correspondence. Use a common tactical lattice and stable global feature/hex identities across bounded packages. Shared boundaries must not create gaps, duplicate playable locations or disconnected routes. A full-size monolithic package is not required, but coverage must no longer stop at selected pilot crops.
- **Both complete rosters:** account for every Goodwood formation counter and its explicit ASL decomposition or retained off-map/support representation. Include personnel, crew-served weapons, artillery, vehicles, transports and aircraft where present. Reconcile source-to-ASL counts and parent links. Do not force off-map aircraft or campaign supply/HQ markers into inappropriate ground-unit definitions.
- **Explicit default data:** reuse existing catalog definitions where suitable. When missing, manufacture plausible, versioned game-default profiles and equipment allotments consistent with existing ASL nationalities and counter kinds. Label their provenance. Historical research is not a completion prerequisite. Preserve separate crew and weapon identities and supported operator/transport relationships.
- **Validated setup handoff:** preserve side, identity, catalog reference, global position and supported facing/location information through the ASL setup planner and replay. Report valid setup separately from supported combat actions. No unresolved omissions may be hidden behind an overall Ready label.
- **Continuity:** return to Formation view and reopen ASL without regenerating accepted terrain, duplicating units or losing placements. Campaign export/import and host/browser restart retain the seed, packages, conversion policy, roster, links and setup. Keep existing Studio progress when reopening the same handoff; do not overwrite it with a new setup.
- **Usable release:** clear loading/progress and cancellation, keyboard inspection/navigation, readable counters and operator details, preserved viewport, and measured full-Situation generation/reload, memory and package size. Earlier pilot timings do not establish whole-Situation performance.

### Ordered work

| Step | Deliverable | Acceptance gate |
| --- | --- | --- |
| G1 Coverage audit | Enumerate Goodwood source terrain, board joins, deployed/off-map counters, current catalog mappings, decomposition/allotments and existing setup capabilities | A checked-in coverage table identifies every gap, default and exception; no new ASL rules work is scheduled |
| G2 Situation map coverage | Extend the deterministic common lattice and package coverage beyond the pilot crops, including board joins and preserved feature connections | All Goodwood geography has an inspectable supported representation; overlap, boundary and seed fixtures pass |
| G3 Complete counter conversion | Implement remaining Goodwood counter families and explicit allotments using existing ASL setup kinds and labeled manufactured defaults | Every source counter is traceable to its child identities or an explicit off-map/support record; no silent loss or duplication |
| G4 Situation handoff and resume | Assemble the map coverage and both rosters into the existing ASL setup workflow, including supported equipment relationships and compatibility handling | End-to-end Formation to ASL to Formation to ASL navigation, export/import and restart preserve the accepted map and deployment |
| G5 Whole-Situation acceptance | Run coverage, deterministic generation, setup validation, failure recovery, UI and performance acceptance; document limitations | Full Goodwood accounting and reproducible save/reload demonstrated; remaining ASL combat limitations clearly distinguished from transition defects |

G2 and G3 may be delivered in small tested increments, but G1 determines the concrete next counter family and terrain gaps. G1 is recorded in [Goodwood Transition Coverage](../../Campaign/docs/Goodwood%20Transition%20Coverage.md), with a reproducible source inventory. G2 package coverage and G3 roster conversion are implemented and have full-size integration evidence. Current work is G4/G5: full-map performance, interface acceptance, boundary handling, existing-deployment compatibility and portable preservation of subsequent ASL progress. Reuse existing ASL work as it becomes available without making this goal depend on implementing its future passes.

### Current baseline and exclusions

The bounded pilot already generates authored terrain and preserves selected formation deployments. British/German rifles, British engineers, and six German 81 mm mortar/crew pairs have catalog-backed setup handoffs into ASL Studio. Catalog 1.15.0 supplies the explicitly manufactured mortar profile. These delivered slices do not establish coverage of the remaining Goodwood roster or the entire map.

Campaign combat adjudication, victory evaluation, return of ASL battle outcomes to the campaign, new ASL rules and other Situations remain outside this goal. Later Studio gameplay is stored by Studio; campaign export currently contains the initial handoff rather than subsequent game history. Keep that distinction explicit when verifying persistence.

The sections below retain the original spatial, deterministic generation and storage contracts, initial milestone design, and dated implementation evidence. Original first-version scope statements describe the pilot baseline; the Goodwood completion criteria above govern the remaining work.

## Confirmed integration points

Repository paths are relative to the root.

| Existing component | Reuse |
|---|---|
| prototypes/europe-hex-map/sources/situations/panzer-leader-05.json | Pilot source; pin its bytes and content hash |
| prototypes/europe-hex-map/tactical/spatial-frame.js | Source board projection behavior to reproduce in adapter fixtures |
| src/ASL/LimboDancer.Domains.Asl.Maps/Geometry/BoardGeometry.cs | ASL indexing, pixel geometry and sampling |
| src/ASL/LimboDancer.Domains.Asl.Maps/Features/FeatureModel.cs | Typed terrain features and fixed-point coordinates |
| src/ASL/LimboDancer.Domains.Asl.Maps/Features/FeatureCompiler.cs | Deterministic terrain compilation |
| src/ASL/LimboDancer.Domains.Asl.Maps/Features/BoardPackage.cs | Canonical feature serialization, package hash and storage |
| src/ASL/LimboDancer.Domains.Asl.MapStudio/Services/BoardProvider.cs | Existing authored-board loading and inspection integration |
| src/ASL/LimboDancer.Domains.Asl.MapStudio/Services/GameMaps.cs | Versioned game map loading and viewer-specific layers |

BoardPackage identity includes the defining manifest and feature content, including board reference, name, geometry, catalog hash and provenance. Cache files and the validation summary do not define that identity. Derive stable package names/references from the refinement and crop identity, never request timestamps. Keep campaign seed and refinement provenance in a companion manifest; do not assume that the ASL package format already stores them.

The previously proposed src/LimboDancer.Campaign host does not exist in this checkout. Do not build an implementation around an assumed host. The first milestone runs independently; later UI integration uses an explicit campaign-owned service boundary.

## Pilot definition

Use Panzer Leader Situation 5, Operation Goodwood. Its joined boards are B, A and C, each rotated 90 degrees. Anchor the primary pilot on the centroid of the source polygon grancelles-bridge-1 on board A. This is an authored source location, not a claim about historical coordinates.

Start with BoardGeometry.Standard(33, 20), twice the standard board's nominal row count, to bound compilation costs. Snap the crop origin to the persistent ASL lattice so the bridge lies near its center. Require an even global column origin in version 1 to retain stagger parity in a standalone package. Use an integer global row origin; odd-column crop origins require a separately tested parity adapter and are deferred. Record the exact origin and extent in a checked-in fixture before generation work proceeds.

Milestone 0 must demonstrate that the crop includes settlement, approach road, water and the crossing geometry. If the initial extent is insufficient, change and freeze the fixture dimensions explicitly. Never expand crop size as an incidental response to the seed.

Create additional overlapping-crop and A/C-join fixtures. These are verification inputs; the player does not need multiple pilot choices initially. No partial crop is presented as the complete Goodwood Situation or as satisfying its full victory conditions.

## Spatial contract

### Source to metric space

Use the source board dimensions 1130 by 3138, normalized coordinates and the existing joined-layout rotations. Apply rotation and join translation before multiplying by 250/113 metres per source unit. Store the joined coordinate origin and transform in the manifest.

All generated features use that stable local metric space. Continental registration remains a separate transform. Source features and their accepted generated detail do not move when a player pans, zooms or switches map levels.

### Metric to ASL geometry

Adopt an ASL-oriented staggered-column lattice in the joined metric frame. With tactical spacing S = 40 m, global column c and row r have ideal centers:

- x = originX + S * sqrt(3)/2 * c
- y = originY + S * (r - parity(c)/2)

Here parity(c) = ((c % 2) + 2) % 2, so negative columns also have parity 0 or 1. Division by 2 in the center formula is real-valued, not integer division.

This chooses the grid orientation deliberately; it is not the prototype axial grid with different labels. Preserve a mapping from old prototype placements rather than treating old q,r values as new ASL indices.

For the standard ASL pixel geometry, map horizontal lattice spacing S * sqrt(3)/2 to 56.25 pixels and vertical spacing S to 64.5 pixels. Include the board A1 center offset and crop translation. This affine mapping accommodates the VASL-compatible pixel aspect ratio without treating pixel dimensions as physical metres.

Use ASL geometry for hex naming, hit tests, edge clamping and fact sampling. Do not convert its clamped edge sample points back into authoritative metric centers. Quantize emitted feature vertices through FixedVector.FromPixels, which rounds to the nearest 1/64 pixel with midpoint ties away from zero. Pin positive, negative and half-step fixtures and reject values outside the underlying signed 32-bit fixed-point range. Numeric decisions affecting generation use integer metric coordinates; transcendental calculations belong only to the pinned transform and require golden boundary tests.

Use integer millimetres in the persistent metric feature representation. Quantization to ASL 1/64-pixel units happens at compilation boundaries. For integer-millimetre input, bound export/import round-trip error per axis by half a 1/64-pixel quantum converted through the inverse transform, plus half a millimetre for final integer rounding and the floating-point tolerance fixed by tests. Measure ideal-to-millimetre transform error separately.

### Parent and boundary rules

Primary parent ownership is determined by the tactical center in source formation polygons. Break ties by ordinal board ID and hex ID. Preserve overlap references for inspection. Parent ownership never overrides the actual geometry of mixed terrain.

Do not clip a tactical hex into separately playable fragments at a formation boundary. For initial setup, use the center-based source legality and explicit supply-margin exception; show the governing parent and reason in inspection. Check terrain and stacking legality through admitted tactical rules as separate constraints.

Board crops share a single origin, orientation and global hex key. Local package coordinates must map back to that key. Preserve global feature identities across crops; clipping changes local geometry but not the source feature identity.

Identical world geometry alone does not prove identical compiled facts at independently rasterized crop edges. Standard board edges use clamped sample points, and compilation includes neighborhood-dependent postprocessing. Milestone 2 must establish a tested guard region and raster alignment, or reuse canonical compiled regions, before admitting overlapping playable crops. Compare world geometry throughout the overlap and derived facts in the admitted interior. Exclude edge half hexes and unverified boundary locations from deployment. Boundary consistency remains a release gate, not an existing compiler guarantee.

## Seed and identity contract

Generate a cryptographically random 128-bit root seed once for a new terrain world. Store it as 32 lowercase hexadecimal characters before generation. An explicit supplied seed is accepted for fixtures and replay. Resume, retry, navigation and import never create a replacement seed.

Use a versioned SHA-256 counter-based random stream, named sha256-counter-v1, for the first implementation. This avoids dependence on framework PRNG implementations.

1. Define an ordered JSON array containing protocol version, seed, map-space ID, source digest, spatial-profile digest, terrain-profile digest, generator version, domain and stable feature or region key.
2. Serialize with the repository's canonical JSON behavior and UTF-8 encoding; fixture the exact bytes. Avoid locale-dependent values and floating-point parameters in this seed input.
3. Hash those bytes with SHA-256 to obtain the stream key.
4. For counter i starting at zero, hash the 32 stream-key bytes followed by i as an unsigned 64-bit big-endian integer. Read each resulting block as eight unsigned 32-bit big-endian values.
5. Choose bounded integers with rejection sampling: for bound n, accept values below floor(2^32/n)*n, then use value modulo n. Restrict n to 1 through 2^32 inclusive, calculate the cutoff with unsigned 64-bit arithmetic, and reject stream-counter overflow. Express profile probabilities as integer thresholds.

Use distinct domains for parcel layout, building detail and woodland detail, and stable keys within each domain. Never use engagement IDs, local crop coordinates, viewport state, traversal order or deployed forces as random inputs. Local counters are allowed inside a keyed feature, not shared across all features.

Distinguish world refinement identity from crop/package identity. The world refinement identity covers the root seed and defining source, spatial, profile, generator, evidence and override inputs, excluding crops and engagements. Compilation identity additionally pins catalog content hash, compiler and fact-derivation versions, and serialization version; admission records its rules-package and validator versions. Package identity additionally includes crop geometry and stable board reference/name.

Feature IDs derive from the stable world feature key and world refinement identity, not crop ordering or wall-clock time. AuthoringCommands.Check requires a nonblank ID, not a ULID; FeatureIds provides ULID-shaped helpers but these are not a validation requirement. Use the full lowercase SHA-256 feature digest as the proposed generated ID, test serialization and UI round trips in milestone 0, and reject duplicate IDs with differing content.

Create golden stream and selection vectors before implementing the generator. C# is the generation authority in version 1; the browser consumes output rather than maintaining a second generator.

## Terrain profile and pipeline

Use one versioned profile, goodwood-rural-settlement-v1. Milestone 0 must record exact supported catalog codes and rule coverage for the following intended set: open ground, woods, roads, water/channel terrain, the existing crossing and single-level buildings. Do not assume catalog presence proves executable movement, LOS or location support. BoardValidator implements only part of its documented validation set; successful package validation is not gameplay admission. In particular, Play/BoardCatalogTerrainEvidence.cs binds Scenario A1 building evidence to pinned VASL board 01, so that path cannot certify generated Goodwood buildings. Audit setup, movement, LOS and location support separately. The first release needs validated inspection and setup support; unsupported movement/combat must remain unavailable and must not be advertised as rule-backed.

Omit generated multi-level buildings, cliffs, new fords, fortifications and complex hexside terrain from the initial profile. If a locked source feature cannot be represented with supported semantics, stop admission and report it. Do not silently omit or substitute it.

Treat source terrain and explicit route/crossing anchors as fixed constraints. Classify source artwork objects individually: a decorative roof is not automatically a locked historical building. Store that classification in the source adapter fixture for review.

Generation order:

1. Normalize source geometry and extract fixed route, crossing, settlement, woodland and elevation constraints.
2. Build structural connectivity once for the connected source map, independent of requested crop.
3. Derive stable settlement parcels and woodland regions from source region identities. Generate each complete owning region before clipping to an engagement.
4. Generate permitted building arrangements, open yards and connected clearings using separate keyed streams.
5. Preserve required route clearance and crossing approaches. Reject overlaps and inaccessible required locations using deterministic ordering.
6. Translate accepted features into FeatureModel, compile, derive facts and run board plus pilot-specific validation.
7. Publish the package only when all required checks pass.

Freeze the initial profile's integer dimensions, density bounds and exclusion distances in a checked-in JSON fixture during milestone 0. Calibrate against the bounded source region and supported ASL locations, not arbitrary historical density claims. No production generator may use undocumented fallback parameters.

Version 1 does not automatically reroll invalid candidates. Return structured generation findings for the same reproducible input. Add deterministic repair or bounded candidate selection only as an explicit later generator version.

## Proposed application contracts

| Contract | Required content |
|---|---|
| RefinementRequest | Seed, stable map-space identity, source hash, spatial/profile versions, global crop origin and dimensions |
| MetricFeatureSet | Stable IDs, integer geometry, feature domains, source constraints and provenance |
| RefinementManifest | World and compilation identities, all defining input digests, crop transform, package reference/version, feature-output digest and separate map-validation/admission results |
| RefinementResult | Accepted package and manifest, or typed findings; no partially accepted terrain |
| TacticalMapLink | Campaign and Situation identity, refinement revision, package version, global/local coordinate mapping and deployment reference |

Return findings with feature/hex references and player-facing summaries. Separate generation failures from unsupported-rule admission and invalid deployment. Timings and diagnostic timestamps are not part of deterministic identity.

Suggested project structure is a campaign-owned class library plus CLI and tests under a new src/Campaign directory. Confirm names and shared build settings in milestone 0. The library may reference ASL Maps; tactical admission integration may consume ASL Play through a separate adapter. ASL projects must not reference Campaign or a host executable.

## Storage and compatibility

Store accepted packages under revision-specific roots. BoardPackageStore writes files directly and uses board slugs; wrap publication with staging, hash validation and atomic finalization so one world cannot overwrite another version under the same name.

Persist the seed and manifest before marking a generated package available. Retain accepted features, package bytes and explicit overrides with the campaign. Export/import must include these or resolvable pinned content, not only a seed that depends on unavailable software.

Keep accepted package identity immutable. Terrain damage is a subsequent revision/state change, not a mutation of the original seed. Preserve backups when introducing a new save schema. Existing prototype saves remain on their current map format unless the player explicitly starts the pilot flow; never reinterpret saved axial coordinates in place.

## UI integration for the first usable version

After the CLI/package gate, expose the generator through a local campaign-owned service used by the prototype pilot entry. Keep this endpoint limited to known source IDs and profiles; do not accept arbitrary filesystem paths. Use idempotent requests, staged publication and status polling. Configure deployment and same-origin/proxy details explicitly before this milestone rather than embedding them in the ASL library.

Load the resulting authored board through existing ASL provider tooling for inspection. The player flow must use the compiled geometry and facts, not continue rendering a separate prototype interpretation. Reuse existing viewport/rendering capabilities through an adapter or extracted component; do not reference the MapStudio executable from domain code. Exact UI hosting/extraction is resolved at the integration checkpoint after package proof, before beginning the UI milestone.

The pilot map starts with terrain visible during setup and existing viewer-specific unit visibility intact. Navigation retains geographic focus and formation selection context. Returning restores the previous formation viewport.

Keep the map primary. Use a compact toolbar for return, fit, parent boundaries and details. Terrain cards explain supported features, locations, provenance and rule-backed effects. Legal setup is an overlay with precise rejection messages. Generation status stays over the existing formation view until the new map is ready. Cancellation or failure must leave that view and the accepted save usable.

The first release supports inspection and setup of a bounded pilot roster. Freeze that roster and setup policy during the UI checkpoint; do not import the entire Goodwood roster into a small crop or infer that it is a complete playable Goodwood scenario. Campaign outcome reconciliation remains out of scope.

## Milestones and gates

| Milestone | Work | Gate |
|---|---|---|
| 0 Contract fixtures | Source hash, exact crop, scale transform, catalog/rule matrix, profile parameters, project locations and seed vectors | Checked-in fixtures with no implicit generator defaults; blocked terrain identified before implementation |
| 1 Reproducible package | C# adapter, keyed random stream, metric features, feature compilation and package export | Two runs produce identical feature, terrain and package hashes; authored package loads in ASL tooling |
| 2 Continuity and persistence | Overlapping/join fixtures, immutable publication, manifest and export/import | Shared features match; resume and transfer reproduce accepted terrain; incompatible versions fail clearly |
| 3 Usable pilot workflow | Service/host checkpoint, bounded roster, map navigation, inspection and setup | End-to-end open, inspect, deploy, return, save and resume verified in UI |
| 4 Seed and performance review | Representative seeds, boundary cases, performance measurements and failure recovery | First-version release criteria satisfied before expanding profile or Situation coverage |

Do not parallelize milestone dependencies by building UI against speculative geometry. The package pilot implements the bounded milestone 0/1 work listed in the progress record below. The deployment checkpoint below closes the fixed-crop interior-boundary and bounded-setup gates. Arbitrary crops and elevation remain excluded; the release-review checkpoint records the remaining milestone 4 work.

## Validation plan

Use deterministic fixtures for all four board rotations, parent-boundary ties, crop parity, metric round trips and shared hexsides. Include negative coordinates and outer-board half hexes. Test prototype-to-metric placement migration separately from new-grid generation.

Check at least 32 fixed root seeds for the pilot profile, plus specifically authored invalid inputs. Measure feature differences and tactical properties using supported rules. Require locked crossings and routes for every accepted map package. Once the pilot roster and setup policy are frozen at milestone 3, rerun the seed suite and require legal setup capacity before workflow admission; a milestone 1 package alone does not establish that capacity. Do not define balance as symmetry or tune output to current force placement.

Verify repeated compilation, reversed crop request order and parallel requests produce identical shared content. Assert no partial package becomes visible after cancellation or publication failure. Check hash corruption, unavailable versions, malformed seeds and cross-campaign storage collisions.

Initial performance targets for the bounded pilot are cold generation/compilation within 10 seconds, accepted-package reload within 2 seconds and no continuous UI blocking during generation on the development machine. These are proposed acceptance budgets, not measured claims. Record runtime, hardware, peak memory and package size; revise the bounded implementation or explicitly review the budgets before release. Do not claim continent-scale performance from these results.

UI acceptance covers keyboard access, label orientation, marker scale, zoom-dependent detail, parent overlays, precise setup errors, preserved viewport and unchanged saved terrain after reload. First-version admission is limited to the audited terrain and roster.

## Deferred work

Additional regional profiles, 240 m rollout, reconnaissance gameplay, complex building levels, generated fortifications, player reroll tools, formation-summary regeneration, full terrain damage UI, continent-wide tactical compilation and campaign combat reconciliation remain outside the first version.

## Implementation readiness

The package pilot has been generated, compiled, reloaded and inspected through ASL tooling. The fixed-crop campaign deployment workflow is implemented and verified as recorded in the checkpoint below. Milestone 0 resolves bounded fixture and capability details by inspecting and testing the existing code; it does not reopen broad architecture analysis. If that audit finds an unsupported required crossing or location, record the exact gap and revise the pilot explicitly before building dependent generation or UI behavior.

## Review record

Reviewed against the repository on 13 October 2026. Corrected crop parity, negative-coordinate parity, fixed-point rounding, feature-ID requirements, package identity and compilation-version pinning. Distinguished map validation from rule admission and roster-dependent setup capacity. Overlapping-crop raster/fact equivalence remains an explicit implementation gate. The companion analysis now permits the first-version rejection policy without requiring automatic repair.

## Package pilot progress, 13 October 2026

Implementation and run instructions: [Campaign pilot README](../../Campaign/README.md). New projects live under `src/Campaign`; ASL domain projects have no reverse dependency on Campaign.

- Delivered: pinned Goodwood source/catalog/profile, seed stream, metric projection, fixed crop (52,90,33,20), source-preserving routes/crossings, generated town buildings, native ASL compilation/facts/validation, exact preview, reproducibility manifest and immutable publication.
- Source identity uses UTF-8 after CRLF-to-LF normalization for portable checkouts. The embedded catalog/profile are pinned with LF fixture attributes. This explicitly refines the earlier source-byte pinning description.
- The initial crop contains settlement, roads, streams and bridges, but no woods. Woods can be compiled from preserved source polygons; generated connected clearings remain deferred. Profile numbers and source-artwork classifications are in the checked-in fixtures and their notice.
- Primary-parent references use tactical centers; geometric overlap references are also emitted. Edge half hexes remain inspection-only, as does the whole pilot until setup admission is implemented.
- Overlapping crops reproduce shared world features. Interior/edge fact equivalence is still a milestone 2 gate. The A/C seam transform is verified, but the candidate seam crop includes nonzero source elevations and is rejected until an elevation profile is admitted.
- Fourteen automated tests passed, including 32 seeds and primary-crossing connectivity. A final generation/publication/reload/render run took 2.18 seconds on the development machine. The default seed produced 65 features, 676 hexes, zero validator errors and 175 sampling-boundary warnings. This is not a cold-start benchmark.
- Package validation and native Map Studio inspection are exercised. Scenario A1 building bindings do not establish generated-map gameplay support; the manifest explicitly records no setup, movement, LOS or combat admission.
- The CLI persists the seed before generation and verifies package/catalog/manifest content before publication. Campaign file-import integration and a bounded roster remain later work.

The package pilot intentionally narrows the earlier intended terrain-generation set. This is a delivered inspection/compilation milestone, not completion of milestones 2 through 4 or the first usable campaign workflow.


## Campaign deployment integration checkpoint, 13 October 2026

The package pilot was committed as `3d41f327`. The next implementation adds `LimboDancer.Campaign.PilotHost`, a same-origin local host for the existing prototype and bounded pilot API, plus the Goodwood formation-toolbar entry. See the Campaign README for launch instructions, the frozen roster/policy and exact scope.

The first deployment slice deliberately admits only the fixed primary crop and eight infantry squads. The original package manifest stays inspection-only; a separate versioned setup record binds the accepted package and campaign context to the audited deployment policy. It permits admitted ground locations, applies campaign-owned source/supply permission snapshots, excludes a two-hex guard and bridge/stream locations, and enforces two friendly squads per hex. It does not claim Scenario A1 rule bindings, complete Goodwood forces or combat admission.

Generation is queued, idempotent by persisted campaign seed and cancellable. Campaign saves embed accepted features and the refinement manifest with deployment state. Import and resume verify package/catalog/source/profile/compiler identities and the compiled grid hash without generating replacement terrain. Changing formation placements or supply permissions invalidates the context binding and preserves the previous pilot for recovery rather than silently moving counters.

The primary and (60,92) overlap fixtures now also compare derived facts in their common two-hex interior. Their translation is exactly 450 by 129 pixels; this does not establish arbitrary-crop equivalence or elevation support. Only the fixed primary crop is offered for deployment. The 32-seed suite now checks setup capacity under source rules for the frozen roster.

UI acceptance includes opening from completed formation setup, placement and rejection feedback, full pilot deployment, completion/review, return to the retained formation map, and restoration of saved terrain and squads. Pilot setup does not yet derive a footprint from the selected formation's anchor, migrate the old squad grid, or support automatic rebasing when campaign setup changes. These are explicit follow-ups, alongside multi-crop/elevation admission and broader scenario support.

## First-version release review checkpoint, 14 October 2026

Full record: [Pilot release review](../../Campaign/docs/Pilot-Release-Review.md). Baseline integration commit: `7a63f7aa`.

- Release test suite: 27 passed, including 32 seeds, cancellation/retry, staging/rename failure recovery, invalid saves and four incompatible version cases. Invalid imports leave an existing session usable.
- Three fresh-process generation-through-open requests: 2.44 to 3.28 seconds; host startup adds 1.13 to 1.34 seconds. This passes the proposed 10-second budget on the recorded development machine.
- Fresh-process accepted-map reload: 1.81 to 2.08 seconds. One run exceeds 2 seconds, so the reload gate is not passed. HTTP timing excludes browser parsing, persistence and paint; instrumented browser timing remains outstanding.
- Peak server working set: approximately 221 to 239 MiB. Published packages: 669 to 679 KiB; accepted pilot save requests: 468 to 480 KiB. No browser memory or continent-scale claim.
- UI checked: keyboard placement/completion/return, new keyboard pan/zoom/fit, terrain inspection, clearer parent-ownership boundaries, marker scale, five-second rejection warnings and exact viewport restoration.
- Failure injection covers incomplete staging and rename failure, not power-loss durability. Orphan stages stay ignored; automatic cleanup remains deferred.

Milestone 4 is reviewed but not signed off. Next work is cold-reload profiling and browser open-to-ready/long-task measurement. Keep the fixed Goodwood crop and eight-squad scope until the remaining performance gate is closed or its budget is explicitly revised. Broader formation conversion remains a subsequent phase.
### Profiling follow-up, 14 October 2026

The release-review work was committed as b5f1d064. Its profiling action is complete. Exact parent lookup now takes 25 to 32 ms instead of 709 ms, with a regression against the exhaustive lookup. Browser resume skips a redundant health request and an unchanged-save rewrite. All identity, terrain and setup validation remains in place.

Three fresh-process API reloads now take 1.74 to 1.85 seconds. First browser open against a restarted host takes 3.14 seconds; warm repeats take 1.20 to 1.93 seconds. The Release suite passes 28 tests, with browser-module persistence and campaign-file regressions also passing. The Pilot release review records raw evidence and measurement boundaries.

Milestone 4 remains open because the end-to-end cold browser result exceeds the proposed 2-second budget. Next: isolate and reduce first-request handling and rendering costs, repeat cold/warm browser timing, and retain the bounded scope until the gate closes or the budget is explicitly revised.
### First-open gate result, 14 October 2026

A bounded follow-up now passes the proposed 2-second saved-map open target in three fresh-host in-app browser checks: 1.63, 1.87 and 1.68 seconds; warm repeats take 1.04 to 1.31 seconds. This supersedes the earlier outstanding cold-open result. Terrain derivation now rejects bridge/tunnel-free bounding rectangles before exact polygon scanning; positive rectangles retain the original semantics. All map response content matches the original lookup.

Campaign tests: 29 passed. Shared ASL map tests: 245 passed. Browser persistence and campaign-file regressions pass. Default runtime tiering is unchanged. See the Pilot release review for raw evidence, method and limits. Stop performance work here for the bounded pilot; wider browser/accessibility acceptance, full formation conversion and other deferred scope remain separate work.
## Selected-formation implementation checkpoint, 14 October 2026

The completed bounded-pilot performance work is committed as 033e4636. The next slice now opens a local map for a selected deployed Goodwood British/German rifle counter. It derives the parent identity and anchor from that selection and uses the existing provisional four-squad composition. This is not full formation-order-of-battle conversion.

A 17-by-14 crop snaps to four-column/two-row world offsets. This preserves the shared grid's parity and integer pixel phase, avoiding independent crop origins. Source-preserved features and generated detail continue to use campaign seed and world feature identity. Placement is limited to the selected formation's parent hex, admitted ground and current campaign permissions.

Legacy eight-squad saves remain separate. New formation slots retain accepted terrain and setup through file export/import and host restart. Changes to the selected anchor, permissions or the existing whole-Situation placement/supply signature reject resume and preserve the saved map. Automatic rebasing/replacement and more selective invalidation are explicitly deferred.

Evidence and instructions: Campaign README, Selected rifle formation slice. 34 Campaign tests pass, with new selected-roster/crop/context/import and neighboring-interior checks. Browser acceptance passed for de-rifle-03 at C/Y-4, including completion, restart/resume, keyboard boundary rejection and preservation of the legacy eight-squad map. Some default British placements are still rejected because nearby source elevation is not admitted.

Next implementation choices: support a deliberate replacement/rebase workflow, broaden flat-crop capacity and edge/overlap fixtures, then decide elevation admission and additional counter profiles. Do not infer broad crop equivalence, historical four-squad strength or combat support from this slice.
### Selected-formation save recovery, 14 October 2026

The selected-formation save now binds to campaign identity, policy, selected rifle unit, side and anchor. The current campaign permissions must still admit that anchor. Other formation placements and changes to permissions elsewhere do not invalidate this local deployment. Legacy eight-squad pilot binding is unchanged.

Opening an older selected-formation save validates its original context and placements before upgrading the context hash. It then validates against current permissions. Terrain, placements, completion, revision and viewport are retained. Changed campaign identity, selected unit or anchor cannot be silently rebound.

When the anchor changes, the dialog offers **Rebuild local map**, with an explanation that squad placements reset. The existing campaign save is replaced only after terrain generation, package validation, setup-capacity checks and persistence succeed. Cancellation, unsupported terrain, validation errors and failed writes leave the old save intact. The campaign seed remains unchanged. This is an explicit replacement, not relocation of existing squads or a retained map-history feature.

Verification: 34 Campaign Release tests pass, including both sides' scoped binding, old-binding upgrades, altered campaign rejection and revoked-anchor permissions. Browser-code tests cover successful replacement, cancellation, generation failure and storage failure. Campaign file round-trip tests pass. The live browser reopened the pre-change de-rifle-03 C/Y-4 save with 4/4 deployed. Live changed-anchor/rebuild acceptance remains to be exercised; automated tests cover its save-preservation paths.

Next: broaden edge and neighboring-crop fixtures across seeds, then decide elevation admission and additional counter profiles. Elevation remains unsupported. Replacement does not carry squad positions between different crops.


### Expanded crop verification, 14 October 2026

Four supported neighboring-crop pairs were exercised with three seeds (12 cases): A/X-6 with A/W-6; A/X-6 with A/X-7; A/V-7 with A/W-7; and A/W-6 with A/W-7. Every case compares exact shared interior facts (including locations, hexsides, stairways and bridge data), matching world-feature geometry, byte-identical replay manifests, package versions and compiled-grid hashes. Each also verifies four-squad capacity at the selected anchor and rejection of placement in the two-hex inspection boundary.

Two uncovered-edge fixtures, A/A-1 and C/GG-10, are explicitly rejected. Neighboring C/X-4 and C/Y-5 crops are separately tested as rejected because they include source elevation at C/V-3 and C/Z-7 respectively. These are supported failure cases, not evidence of flat-map inconsistency. C/Y-4 also has a cross-seed check proving that generated building detail changes while authored feature geometry remains unchanged.

Validation: the full Campaign Release suite passed 49 tests; the subsequently added seed-variation test passed separately (50 current test cases in total). No production generator changes were needed. These fixtures broaden bounded flat-crop confidence; they do not certify every anchor, board seam, seed or terrain class.

Next elevation increment:
- Define and version the source-level to ASL-level mapping explicitly. Do not silently equate historical heights with source illustration levels.
- Emit source-preserved ASL ElevationRegion features, with deterministic ordering and overlap semantics. Keep road, stream, bridge and building interactions explicit.
- Verify derived base levels and hexside slopes, rendering, supported ground-level setup, adjacent-crop equivalence and saved-package reload before allowing elevated deployments.
- Use a new generation/profile identity and preserve existing accepted flat packages. Migration must not silently regenerate saved terrain.
- Admit bounded setup and inspection only. Movement, LOS and combat require their own evidence and remain unavailable.


### Bounded elevation support, 14 October 2026

Source consulted directly: eASLRB_v3_01.pdf, 716 pages, SHA-256 957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247, at E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf. Physical pages 124-127 and 131 were read; page 125 was also rendered and inspected.

Rule basis and boundaries:
- B10.1 (physical 124-125): ground level at the hex centre determines the unit's elevation; terrain height is relative to the hill. The ASL compiler/derivation remains authoritative for map facts.
- B10.11 (125): full-level boundaries define crest lines. Source polygons provide those boundaries; no random hill geometry is added.
- B10.2-10.31 (125), B10.4-10.52 (126): LOS, height advantage, uphill movement and abrupt changes have additional rules. This increment does not enable those actions.
- B11.1 (126) distinguishes marked cliffs from ordinary elevation changes. No cliff or special Slope hexside is inferred from a height difference. B11 climbing (127) remains unavailable.
- B19.2 and B20.1 (131): depressions and streams are distinct from surrounding ground. Existing ASL depression derivation is retained, including negative levels; stream/deck setup remains excluded.

Generator goodwood-pilot-2 maps source levels 0, 1 and 2 one-to-one to ASL base elevations as an explicit scenario assumption, not a claim of equal measured heights between game systems. Nonzero source polygons produce ElevationRegion features. Compiled raster elevation and existing ASL derivation/rendering drive the map and inspector. Terrain types outside the existing profile, and source levels outside 0-2, remain rejected. Friendly infantry setup is admitted on supported ground terrain at these levels, subject to formation permission, parent boundary, stacking and crop guards. No upper-building location is selectable.

Accepted goodwood-pilot-1 bundles are read with their original identity and features, without regeneration. Both versions remain recognized; unknown versions fail closed. New generation uses v2, so world and package identities differ. Existing flat saves are not automatically upgraded to new terrain.

Verification includes formerly rejected British anchors B/N-8 and B/H-8, German C/X-4 and C/Y-5, and actual hill anchors B/M-6, B/F-7, C/V-3 and C/Z-7. Tests check elevated setup, rendering bands, no invented cliff/Slope annotations, accepted reload, and legacy identity preservation. Neighboring C/Y-4 to C/X-4 and C/Y-5 comparisons now run across three seeds alongside the flat cases. Live browser verification generated B/N-8, deployed four squads and completed setup. Broad movement/LOS conformance, measured-height calibration, unsupported terrain and all-scenario coverage remain outside this result.

Final validation: 65 Campaign Release tests pass with warnings treated as errors. Browser-code recovery and campaign-file round-trip tests also pass.

### Elevation UI acceptance, 14 October 2026

Used an exported campaign backup and a temporary import fixture changing only uk-rifle-02 from B/N-8 to B/M-6. The ready formation screen locks placement edits, so this exercises recovery from a changed saved formation, not an interactive formation move.

- The moved-anchor warning correctly identified both anchors and offered Rebuild local map. Successful rebuilding reset the selected local setup to 0/4.
- At hill-centred B/M-6, Deploy pilot roster placed 4/4 squads. Selecting G6 showed Open Ground, Level 1, Formation B/M-6 and admitted setup. Formation-parent labels displayed, setup completed, and a browser reload reopened the same anchor with 4/4 deployed and Review setup.
- Reloaded the temporary fixture, started rebuilding and pressed Escape before acceptance. Reopening still reported the original B/N-8 saved anchor. A campaign export compared exactly equal to the backup for the entire aslPilots object, including every accepted bundle and setup.
- Restored the original campaign through Load campaign from file. B/N-8 reopened with 4/4 deployed and Review setup. The temporary hill placement is not left in the active campaign.

The first cancellation attempt completed generation before cancellation could be confirmed; it was not counted as cancellation evidence. The subsequent Escape check above provided the verified result. Automated failure-injection tests remain the evidence for publication and storage failures. No production code changed during this acceptance pass; the previously passing 65 Campaign Release tests and browser-code checks remain applicable.

### Engineer and mortar composition increment, 14 October 2026

The selected-formation pilot now admits British engineer formations and German 81 mm mortar formations in Goodwood, alongside the unchanged rifle roster. It uses distinct setup policy identities for engineers and mortars; rifle identifiers and policy remain unchanged. Terrain identity and accepted terrain packages are independent of this roster extension.

- British engineers: four provisional squads, not a conversion of the source table's 65 personnel into verified ASL squads. No assault-engineer/sapper trait, demolition charges, flamethrowers or Bren allotment is inferred.
- German 81 mm mortars: six inseparable mortar-and-crew setup pieces. The count follows the existing source-composition.js entry (page 21 attribution: six mortars and 66 personnel); that underlying source was not independently rechecked in this increment. This is not six infantry squads or six light-mortar support weapons.
- ASL vocabulary inspection distinguishes asl:crew, asl:gun and asl:light-mortar (a subtype of asl:sw). This adapter does not yet instantiate catalog-backed ASL entities. Separate gun/crew identity, equipment ownership, facing, portage, manhandling, firing and engineer actions remain follow-up work. No combat factors are invented.
- Mortar setup is conservatively limited to open ground and paved road at admitted levels. This is a pilot admission policy, not a claim to implement full ASL Gun emplacement rules. Both compositions retain the two-piece-per-hex pilot stacking limit, formation permissions and two-hex crop guard.
- The selector uses unit terminology, with E and M marker labels and descriptive titles. Details states the composition assumptions. Mortar highlighting and inspection apply the same terrain restriction as server placement validation.

Validation: 68 Campaign Release tests pass with TreatWarningsAsErrors enabled, including typed roster persistence, completion, unsupported types, policy mismatch, unknown unit rejection, mortar terrain restriction and rifle compatibility. Browser-code recovery/composition checks and campaign-file tests pass. Live Goodwood UI deployed four engineers at B/I-3 and six mortar detachments at A/E-1. Both saved rosters reopened; mortar completion also survived a full browser reload. Existing formation positions were not changed.

Next: define catalog-backed crew/equipment relationships and explicit support-weapon allotments before enabling genuine ASL support-weapon interaction. The six mortar-and-crew pieces are an interim setup representation.

### Crew and equipment identity increment, 14 October 2026

The preceding composition increment was committed as `2de4ffb1`. This increment supersedes its six combined mortar-and-crew pieces and its pending separate-identity work.

Consulted the ongoing ASL Card Play and Map Studio Redesign Plan through completed Pass 55, the Pass 55 design, and the existing `GameTypes` holding model, `GamePlanner.CardSetup` separation of personnel and equipment, and catalog resolution/publication APIs. The campaign adapter reuses `DefinitionReference`, `Holding` and `HoldingRole.Manned`. It does not create a competing ASL rules implementation or instantiate a playable ASL GameState.

- Explicit allotment: six German crew identities and six German 81 mm mortar equipment identities per selected mortar formation. IDs are stable `/crew-N` and `/mortar-N`, with one fixed operator link for each pair. The six-weapon count retains the prior source-composition entry; the source page was not independently reviewed here.
- Reviewed data: each crew resolves to published `asl-scenario-a1` catalog 1.14.0 definition `attacker-crew`, after checking kind, nationality and reviewed printed source status. Historical Goodwood applicability and the one-crew-per-mortar assignment remain provisional. Printed-data review is not applicability review.
- User decision: retain unresolved mortar data for now. No reviewed German 81 mm mortar definition exists in this catalog. The weapon record has no definition reference or combat factors. It is not replaced by a light mortar or unrelated Gun, and it is not admitted to play.
- Placement: both members must be unplaced or share a hex. Selecting either moves both atomically. The pilot limit counts two friendly personnel units per hex; a weapon does not count as another personnel unit. Open-ground/paved-road restrictions, formation permissions and crop guards remain. This is bounded setup policy, not full ASL Gun emplacement or stacking adjudication.
- Inspection: the Unit details toolbar card identifies the selected entity, the weapon's operator or the crew's assigned weapon, the crew catalog reference and explicit review/unresolved status. Separate C and M markers represent the co-located identities.
- Persistence: `goodwood-selected-mortar-2` stores canonical equipment links and the initial catalog identity. ASL's existing catalog-name resolution uses the current catalog, while the saved version/hash is provenance. It does not pin an obsolete rules implementation. Existing rifle and engineer setup policies remain unchanged.
- Migration: only validated `goodwood-selected-mortar-1` records expand. Each old `/detachment-N` placement creates a crew and weapon at that exact hex; partial and complete states survive. Accepted feature packages are never regenerated. Unknown identities, duplicate placements, bad context bindings and incompatible operator links fail before replacement. UI selection maps an old detachment to its crew.

Validation: 72 Campaign Release tests pass, including partial/complete old-save conversion, JSON round-trip, paired movement, personnel stacking, missing/wrong links, unrecognized catalogs and malformed legacy data. Browser recovery/composition and campaign-file checks pass. Live Goodwood A/E-1 acceptance migrated a completed six-detachment save to 12/12, verified both inspection directions, moved crew 1 to E7 with mortar 1, then moved mortar 1 back to E6 with crew 1. Completion and reload retained 12/12. Export comparison confirmed identical terrain bundle, formation plans and original positions after restoration.

Remaining work: a reviewed German 81 mm mortar definition and scenario applicability, wider nationality/weapon allotments, engineer equipment, operator reassignment, facing and full ASL equipment setup/play integration. No firing, movement, manhandling, portage, casualties or combat behavior is claimed by this increment.

### Rifle and engineer catalog integration, 14 October 2026

Selected rifle and British engineer squads now reference the published ASL unit catalog, using the existing prototype defaults: British rifle `british-squad`, German rifle `attacker-squad`, British engineer `british-elite-squad`. The original eight-squad pilot uses the same rifle mappings. Resolution requires a published catalog, matching squad kind/nationality and reviewed printed source. Printed review does not approve Goodwood applicability, the four-squad conversion, or engineer capabilities. No traits are added to the elite-squad definition.

Unit details distinguishes catalog provenance from provisional scenario use. Rifle and engineer equipment allotments are explicitly unresolved, with zero separate equipment counters issued in this pilot. This is not a historical assertion that the formations had no weapons. No source-table inventory automatically creates a Bren, machine gun, light mortar, demolition charge, flamethrower or mine-clearing capability. Existing mortar links and unresolved mortar data remain unchanged.

New setups record the catalog identity. Validated older infantry saves gain this provenance when opened, without changing their policy, context hash, revision, completion state, unit IDs, positions or accepted terrain. The browser persists the metadata upgrade once; unchanged resumes avoid rewriting the save. An unrecognized non-null catalog identity is rejected. As in the ASL core, recorded catalog names resolve to the current catalog version.

Validation: 76 Campaign Release tests passed with warnings treated as errors. Browser recovery tests cover successful, unchanged and failed provenance writes. Live browser checks reopened completed engineer, British rifle and German rifle deployments and verified their details cards. A before/after campaign export comparison confirmed all existing terrain bundles, placements, completion states, policies, revisions, operator links, context hashes and formation plans remained identical. No combat or equipment actions were added.

### Bounded handoff into ASL setup, 14 October 2026

User decision: historical applicability is advisory. Campaign defaults may be manufactured when needed, provided they are identified as game assumptions rather than sourced historical facts. This increment retains the existing reviewed printed squad/crew definitions and accepts their campaign mappings for setup. It does not require new historical research or alter the ASL catalog's review flags.

The **ASL handoff** toolbar card offers **Prepare ASL setup** after campaign setup is complete. The host uses the existing `GamePlanner.PlanAsync(GameActions.Setup, ...)` and event replay, with the exact validated authored board version and its derived location chains. The resulting `GameState` contains actual ASL `UnitInstance` objects for supported personnel. This is a prepared setup artifact saved with the campaign, not a write to Map Studio's live game store, phase advancement or combat UI.

- Identity: ASL requires slug identifiers, whereas campaign IDs contain `/`. The handoff stores each original campaign ID alongside its stable ASL ID (the `/` separator becomes `--`). Side identifiers similarly become lowercase ASL slugs. No campaign identity is changed. Catalog references and hex positions remain intact; ground location uses relative level 0, including elevated source hexes.
- Readiness: rifles, engineers and mortar crews are Ready under campaign defaults. Unresolved mortar weapons are listed as Needs data and omitted from the prepared ASL record. Their campaign positions and crew links remain intact. Missing weapon data does not prevent personnel handoff. No new mortar profile was manufactured in this increment.
- Persistence: each pilot slot can store a `campaign-asl-handoff-1` artifact, containing the ASL event record, catalog provenance, identity/readiness rows and a digest binding the campaign identity, complete setup and accepted board version. A fixed epoch timestamp makes this preparation record reproducible; it is not a historical event time or live-play audit entry.
- Validation: reopen reconstructs the setup through the ASL planner and compares the saved artifact. Corrupt, stale or incompatible handoffs are not marked Ready; the campaign map still opens and offers preparation again. Deployment edits clear the derived handoff. Storage failure retains the prior save. Campaign file exports include the artifact.
- Defaults and limits: Allied first, British/German sides, no scenario-card OB restrictions, no extra equipment or specialist engineer traits. Campaign setup permissions remain enforced before handoff. This is setup validation, not general ASL movement, firing, LOS or historical scenario certification. The existing ASL rules implementation remains authoritative.

Verification: 80 Campaign Release tests pass with warnings treated as errors. New coverage includes British/German rifles, engineers and mortar crews through actual ASL setup planning and projection; exact positions and definition references; elevated anchors; preserved identity mappings; JSON round-trip; incomplete setup, corrupt records, incompatible versions and stale deployment rejection. Browser tests cover handoff persistence, failed writes and edit invalidation. Live browser acceptance prepared four engineers, reloaded and revalidated their Ready report, then prepared six mortar crews with six clearly omitted weapons. Before/after campaign exports had identical terrain bundles, complete setup records and formation plans.

Next boundary at the end of the preparation increment: publish the prepared artifact and generated board into ASL Studio, implemented below. Explicit game-default weapon profiles remain a separate increment.

### Open campaign setup in ASL Studio, 14 October 2026

The prepared handoff now has an **Open in ASL** action. It revalidates the current deployment, imports the exact generated authored board and initial setup through ASL Studio, then offers an **Enter ASL setup** link in a new tab. Reopening the same handoff offers **Continue in ASL** and retains the existing game's progress. The campaign map stays open.

The Studio validates the package hash, pinned terrain catalog, authored-board status and setup events before publication. A completed package directory is published by rename, followed by the existing `FileGameStore.Append` replay-and-save workflow. An interruption between those writes leaves a reusable board; retry finishes the game import. Existing boards with different versions and games with different initial event prefixes are refused. The same setup identity remains stable across browser and host restarts; an edited campaign setup produces a new game identity. Later ASL turns are never overwritten by reopening the campaign handoff.

The live Studio uses its local tenant, while original unit IDs, catalog references, relative levels and hex positions are preserved. Unresolved mortar weapons remain in campaign setup and are omitted from ASL, as the readiness report states. Historical applicability remains advisory. This opens the existing ASL play workspace without advancing a phase or adding rules. It does not transfer subsequent ASL gameplay back into the campaign. Campaign export contains the initial prepared handoff, not the later Studio event history; keep the Studio boards/game storage to preserve that history.

Configuration: the Campaign host uses `Campaign:AslStudioUrl` (default `http://127.0.0.1:5178`). The import endpoint accepts local server-to-server JSON only. ASL Studio supports `AslMaps:AuthoredCatalogPath` for an exact pinned SharedBoardMetadata catalog, independently of VASL ingestion. A different VASL catalog is not silently substituted. For this pilot, start Studio from the repository root with:

```powershell
$campaignRoot = (Get-Location).Path
dotnet run --project src/ASL/LimboDancer.Domains.Asl.MapStudio --no-launch-profile -- --environment Development --Kestrel:Endpoints:Http:Url http://127.0.0.1:5178 --AslMaps:AuthoredCatalogPath "$campaignRoot/src/Campaign/LimboDancer.Campaign.Refinement/Fixtures/SharedBoardMetadata.xml" --AslMaps:BoardsRoot "$campaignRoot/artifacts/campaign-asl-studio/boards" --AslMaps:CacheRoot "$campaignRoot/artifacts/campaign-asl-studio/cache"
```

The existing Campaign host still runs on port 8093. If Studio is stopped, the handoff reports that it is unavailable and leaves campaign setup unchanged. Both storage roots should be retained between runs.

Verification: 82 Campaign Release tests and 51 targeted ASL Studio authoring, rendering and play-page tests passed. The two import integration cases also passed after adding catalog-mismatch rejection. Coverage includes exact identities/positions/catalog references, unchanged campaign setup and terrain, malformed records, mismatched package/catalog versions, interrupted publication recovery, duplicate prevention, and reopening after a phase advance and service reconstruction. Live browser acceptance imported four British engineer squads at D5, D6, D7 and E4; ASL Studio displayed the generated terrain and counters after restart, and the campaign action then returned the same game link. The live check also confirmed that a different local VASL catalog is refused until the pinned authored catalog is configured.

Next increment: decide and implement explicit game-default mortar profiles, then validate weapon/crew links through this same ASL handoff. Returning ASL outcomes to the campaign remains a separate integration boundary.


### Explicit mortar defaults and weapon/crew handoff, 14 October 2026

The next increment is implemented: catalog 1.15.0 includes the manufactured MFG German 81 mm mortar, while retaining the existing 2-2-8 crew. All six weapons and six crews enter ASL setup with separate identities and manned links. Inspection states the operator and game-default provenance. Campaign terrain and deployment are unchanged; prepared artifacts from the previous catalog must be prepared again. Existing Studio game progress is retained separately.

See `src/Campaign/docs/Campaign Mortar Game Default.md` for values, cross-nationality comparisons, compatibility behavior and remaining engine boundaries. Medium-mortar combat support is separate from valid setup data; the profile is not substituted with a different weapon class.


Campaign-file progress checkpoint: full Situation capture/restore is now connected to the ASL history service. Full-map reconstruction and mismatch rejection pass integration tests; actual campaign-file export includes all 624 positions and ASL history. Finish live restore/resume acceptance, then continue parent-boundary/viewport usability and the final performance/resource audit. See Goodwood Transition Coverage.md for measured evidence and remaining gates.


Viewport checkpoint: per-game/per-viewer/per-board-version pan and zoom persistence is implemented and verified by a full Goodwood page reload. Automatic initial setup framing respects restored views. Continue with parent-hex ownership/boundary inspection and remaining full-map release gates.


Counter provenance checkpoint: visible ASL counters expose immutable parent formation, formation hex at handoff, catalog reference and reviewed/manufactured status. All 624 Goodwood IDs are covered by the roster check. This is counter provenance only; map-wide parent ownership inspection and boundary overlays remain required.


Map ownership checkpoint: the full-board ownership metadata producer is implemented and verified across every Goodwood hex address. Keep this optional metadata separate from immutable campaign context and game history so existing accepted games retain their identities. Complete import validation/storage and the inspector/boundary interface next. Guard exclusion in metadata alone does not enforce a playable boundary.


Ownership integration checkpoint: the host sends validated, exact-board-version metadata separately from immutable handoff context. Studio retains it across restart and repeated imports, and selected-hex inspection exposes the formation parent. Full Goodwood import tests cover every ownership entry alongside all 624 counters. Finish live inspector acceptance, then optional parent-boundary drawing and exterior playable-boundary enforcement.


Boundary overlay checkpoint: optional formation-parent transitions and the owned perimeter are implemented, tested and visually verified on the complete Goodwood map. Toggle operation preserves pan/zoom and retains existing setup marks. Next enforce the exterior playable perimeter through existing ASL map/setup validation, then finish full-map progress-transfer and performance/resource acceptance.


Playable-footprint foundation: board handles can retain an immutable location area while keeping all terrain facts for rendering/LOS. Planner replay now preserves the handle's area instead of stripping it to raw facts. All 820 Play tests passed. Next wire an explicit authoritative Goodwood footprint into import and live board loading. Validate retained history before publication; do not reinterpret optional partial ownership-inspection metadata as a play restriction. Actual perimeter movement, restart and existing-history acceptance remain required.


Explicit footprint integration checkpoint: full Goodwood import and live board catalogs now share a persisted, explicitly requested playable area. Thirteen Studio checks and three Campaign integration cases passed, including all 624 counters, rejected exterior setup before publication, phase advancement and restart/repeated-import progress preservation. Complete actual-interface acceptance on the existing Goodwood game next; final progress-transfer, compatibility and performance/resource gates remain open.
