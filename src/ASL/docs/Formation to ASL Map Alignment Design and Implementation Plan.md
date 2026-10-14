# Formation to ASL Map Alignment Design and Implementation Plan

Date: 13 October 2026.
Status: first-version implementation design. Proposed contracts and project names below are new work, not existing APIs.
Branch: codex/formation-asl-map-alignment.
Baseline analysis: [Formation to ASL Map Alignment Analysis](Formation%20to%20ASL%20Map%20Alignment%20Analysis.md), committed at 452dad19.

## Outcome and scope

Deliver one deterministic, bounded Goodwood terrain refinement that loads through the ASL map pipeline, followed by a complete inspect, deploy, return and resume workflow. A new terrain seed can change permitted tactical detail; the same seed and pinned inputs reproduce the same accepted landscape.

Do not resize existing campaigns, replace ASL rules, or generate the entire continent in this version. Retain nominal 250 m formation spacing and 40 m tactical spacing. Keep a 240 m comparison as an offline fixture, not a player setting or an automatic save migration.

The first milestone is a command-line generator producing a validated ASL authored-board package and refinement manifest. Its success criterion is reproducible compilation and loading, not a screenshot of a fine grid.

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

Do not parallelize milestone dependencies by building UI against speculative geometry. The next implementation step is milestone 0, followed immediately by the milestone 1 generator once its fixtures pass review in code.

## Validation plan

Use deterministic fixtures for all four board rotations, parent-boundary ties, crop parity, metric round trips and shared hexsides. Include negative coordinates and outer-board half hexes. Test prototype-to-metric placement migration separately from new-grid generation.

Check at least 32 fixed root seeds for the pilot profile, plus specifically authored invalid inputs. Measure feature differences and tactical properties using supported rules. Require locked crossings and routes for every accepted map package. Once the pilot roster and setup policy are frozen at milestone 3, rerun the seed suite and require legal setup capacity before workflow admission; a milestone 1 package alone does not establish that capacity. Do not define balance as symmetry or tune output to current force placement.

Verify repeated compilation, reversed crop request order and parallel requests produce identical shared content. Assert no partial package becomes visible after cancellation or publication failure. Check hash corruption, unavailable versions, malformed seeds and cross-campaign storage collisions.

Initial performance targets for the bounded pilot are cold generation/compilation within 10 seconds, accepted-package reload within 2 seconds and no continuous UI blocking during generation on the development machine. These are proposed acceptance budgets, not measured claims. Record runtime, hardware, peak memory and package size; revise the bounded implementation or explicitly review the budgets before release. Do not claim continent-scale performance from these results.

UI acceptance covers keyboard access, label orientation, marker scale, zoom-dependent detail, parent overlays, precise setup errors, preserved viewport and unchanged saved terrain after reload. First-version admission is limited to the audited terrain and roster.

## Deferred work

Additional regional profiles, 240 m rollout, reconnaissance gameplay, complex building levels, generated fortifications, player reroll tools, formation-summary regeneration, full terrain damage UI, continent-wide tactical compilation and campaign combat reconciliation remain outside the first version.

## Implementation readiness

The proposed package contracts and first delivery boundary are specified, but implementation readiness remains conditional on milestone 0. No generated package or end-to-end workflow has yet been validated by this document. Milestone 0 resolves bounded fixture and capability details by inspecting and testing the existing code; it does not reopen broad architecture analysis. If that audit finds an unsupported required crossing or location, record the exact gap and revise the pilot explicitly before building dependent generation or UI behavior.

## Review record

Reviewed against the repository on 13 October 2026. Corrected crop parity, negative-coordinate parity, fixed-point rounding, feature-ID requirements, package identity and compilation-version pinning. Distinguished map validation from rule admission and roster-dependent setup capacity. Overlapping-crop raster/fact equivalence remains an explicit implementation gate. The companion analysis now permits the first-version rejection policy without requiring automatic repair.
