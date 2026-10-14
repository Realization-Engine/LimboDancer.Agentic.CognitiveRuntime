# Formation-to-ASL pilot

This implements the bounded package pilot from the Formation to ASL Map Alignment Design and Implementation Plan: pinned inputs, deterministic metric features, an ASL authored-board package, compiled terrain, derived hex facts and inspection output. The campaign host now adds a bounded eight-squad deployment workflow. Movement, LOS, combat and full-Situation conversion remain unavailable.

## Run from the repository root

Requires .NET 10. No external VASL checkout is required to generate or test the package: the catalog fixture is included with provenance and its LGPL notice.

```powershell
dotnet run --project src/Campaign/LimboDancer.Campaign.Refinement.Cli -- prototypes/europe-hex-map/sources/situations/panzer-leader-05.json artifacts/goodwood-pilot 00112233445566778899aabbccddeeff
dotnet test src/Campaign/Campaign.slnx
```

Omit the seed argument to create a new random 128-bit seed. The CLI writes `seed.txt` before generation and reuses it on subsequent runs. Use a different output directory for a different world. Reusing a directory with a conflicting seed fails. Generation failures do not reroll the seed.

Each accepted package is published beneath its full package hash:

- `boards/goodwood-<identity>/board.json` and `features.json`: native ASL authored-board package.
- `refinement.json`: seed, pinned identities, metric features, parent/overlap references, warnings and inspection-only admission status.
- `SharedBoardMetadata.xml`: exact catalog required by the package.
- `preview.svg` and `exact.svg`: the ASL renderer's compiled-terrain view.
- `facts.json`: ASL-derived hex facts for inspection.

The package, manifest and catalog are staged and atomically published together. The CLI adds derived previews afterwards. Repeated publication verifies existing content instead of overwriting it. Copy the complete hash directory to transfer an accepted package; the campaign host also embeds the accepted package in campaign save files, as described below.

## Inspect in existing Map Studio

Use the existing host with a VASL checkout containing the pinned catalog and `AslMaps:BoardsRoot` set to the generated hash directory's `boards` folder. For a local development run, pass `--environment Development` so static assets load. The host's configured Kestrel endpoint overrides `--urls`; use `--Kestrel:Endpoints:Http:Url http://127.0.0.1:8092` to select an isolated port. The board URL is `/boards/<PackageReference>` from `refinement.json`.

Map Studio's `Authored, valid` status means its map validator found no errors. It does not certify combat, movement or scenario admission. Use Exact view for the crop: Styled view currently draws retained world-feature paths past board edges.

## Frozen spatial and terrain contract

The primary crop starts at global column 52, row 90, with 33 columns and 20 nominal rows (676 addressable ASL hexes including staggered edge fragments). The joined B/A/C source is rotated as authored. Formation scale stays nominally 250 m; tactical spacing is 40 m. The crossing is near the crop center. The crop is about 1.1 km by 0.8 km.

The profile generates seeded, nonoverlapping single-level building footprints inside town polygons. It preserves traced road and stream centerlines and bridge polygons. Roads and crossing geometry do not vary by seed. Footprint clearance protects those routes. Source roofs are decorative and are not copied into the authoritative building model.

| ASL code | Terrain | Pilot treatment |
|---|---|---|
| 0 | Open Ground | Base terrain; also the unbuilt parts of town regions |
| 8 | Water | Preserved source water polygons |
| 32 | Shallow Stream | Traced channels, assumed 8 m width |
| 40 | Stone Building | Generated ground-level footprints, assumed material |
| 60 | Woods | Preserved formation woods polygons |
| 66 | Paved Road | Traced roads, assumed 10 m width |
| 84 | Stone Bridge | Preserved crossing footprint, assumed material |

These codes are supported by compilation and fact derivation. That is separate from executable rule coverage: the existing Scenario A1 building-evidence path is pinned to VASL board 01. The generator manifest alone does not mark any hex deployment-admitted; the separate campaign setup policy described below supplies bounded admission. Nonzero source elevations and unsupported terrain in the requested crop/guard region fail closed. The primary crop contains no woodland; generated connected clearings are deferred, not silently claimed as implemented.

The overlap test verifies shared world-feature IDs and geometry plus derived facts in the admitted two-hex interior for the fixed (60,92) fixture, not arbitrary crop-edge equivalence. A/C join projection is covered; the proposed seam crop at (52,116) is deliberately rejected because it includes unsupported hills. Further join admission requires an elevation profile. Parent ownership is center-based with ordinal source-key ties; overlap references use the tactical hex polygon.

## Validation and limits

Tests cover independent SHA-256 stream vectors, rotations, negative parity, quantization, source/seed rejection, repeated output, 32 seeds, bridge-to-road connectivity, shared features in overlapping crops, source joins, unsupported hills, corruption and cancellation. Shared ASL build settings are imported and every pilot project explicitly sets `TreatWarningsAsErrors` to `true`.

The first tested seed produced only MAP-VAL-007 warnings: feature boundaries close to ASL sampling points. These are exported, not discarded. A zero-error authored package is sufficient for inspection. The separate bounded setup policy below admits the pilot roster; movement remains unavailable.

The bounded campaign integration below implements the next workflow slice. The release review records its performance and remaining gates. This pilot does not resize existing maps or migrate saved deployments.

## Recorded pilot verification

On 13 October 2026, .NET SDK 10.0.402 on Windows: 14 tests passed, including the 32-seed acceptance test. The final CLI run with seed `00112233445566778899aabbccddeeff` generated 65 features and 676 hexes, published/reloaded/recompiled the package and rendered the preview in 2.18 seconds. This is one measured development-machine run, not a cold-start or broad hardware benchmark. Package version: `6907b65ed7ab86be88d3add25d78639eecdeef86ad7304abfdb9ec81e7d4677b`.

The package has zero validator errors and 175 MAP-VAL-007 sampling-boundary warnings, retained in the manifest. In native Map Studio it loaded as Authored, valid; selecting R10 displayed Stone Bridge, Shallow Stream locations and road/bridge hexside facts. These observations verify compilation and inspection, not combat admission. No existing campaign save or application UI was modified by the pilot.


## Campaign deployment integration

Run the local campaign host from the repository root:

```powershell
dotnet run --project src/Campaign/LimboDancer.Campaign.PilotHost
```

Open `http://127.0.0.1:8093/index.html?exercise=source&source=5`. The host serves the existing prototype and `/api/asl-pilot` on the same origin. Existing saves on port 8080 belong to a different browser origin: use **Save campaign to file** there, then **Load campaign from file** on port 8093. The static-only server cannot compile ASL terrain. Optional host settings are `Campaign:PrototypeRoot` and `Campaign:PackagesRoot`; API requests cannot choose filesystem paths or source packages.

1. Commit the Goodwood mission and complete both sides' formation setup.
2. Select **Open ASL deployment pilot** in the formation toolbar.
3. Choose a squad and destination on the map or in the keyboard-accessible hex selector, then **Place selected squad**. **Deploy pilot roster** fills only missing placements.
4. **Complete setup** locks placement; **Review setup** unlocks it.
5. **Return to formation map** retains the underlying formation viewport and selection. The pilot's own viewport is saved on return or deployment edits.
6. Campaign save/load files include `world.aslPilots.M04`: campaign seed, setup-context snapshot, accepted feature package and refinement manifest, placements, completion state and viewport. File import validates accepted terrain through the host before installing the campaign. Resume compiles the saved features and verifies package/grid hashes; it does not run the generator.

### Frozen setup policy

`goodwood-infantry-setup-1` contains four British and four German infantry squads, associated with `uk-rifle-01` and `de-rifle-01`. This is a bounded test detachment and does not transfer the full formation roster, enforce a radius around its current formation position, or reconcile casualties. The crop stays fixed at (52,90,33,20).

The prototype campaign remains the authority for formation setup permissions, including its supply-margin and retained supply-area exceptions. It supplies `CampaignSupplyDeployment.refs` for the two roster parents. C# verifies references exist in the pinned source, derives each tactical hex's primary formation parent from its metric center, and enforces those permissions separately from terrain and stacking checks. The context hash binds the campaign identity, formation-placement/supply snapshot and sorted allowed-parent sets. It is an integrity binding, not a signature or multiplayer authorization system. If formation setup changes, reopening fails with an explanation and retains the old pilot. Automatic rebasing remains follow-up work.

Admitted setup locations are ground level, with center terrain code 0, 40, 60 or 66. Bridge locations, streams, water, unknown terrain, missing source parents and the outer two-hex guard are excluded. Friendly stacking is limited to two squads; opposing squads cannot share a hex. This is an explicit campaign pilot policy, not an assertion that ASL movement, LOS or combat rules have been implemented. The package's original inspection-only manifest remains unchanged; the separate setup record names the policy that admits the bounded deployment.

### Service and persistence behavior

Generation requests are idempotent by campaign seed, cancellable and polled. A single generation queue limits compilation concurrency. Packages publish through the existing staging/verification path. Each host instance retains at most eight generation jobs and sixteen deployment contexts; restarting releases caches without deleting browser campaign saves or accepted package directories. The API is loopback-only with same-origin, JSON-only mutations. Stop the host before rebuilding on Windows to release loaded assemblies.

Placement endpoints return a validated replacement setup. The browser installs it only after campaign persistence succeeds; failed validation cannot partially place the roster. Existing campaign revision checks protect against concurrent-tab writes. There is no migration of legacy squad q/r placements into ASL indices.

### Verification and remaining gates

The deployment tests cover source-rule capacity across 32 seeds, legal placement, completion/review, side restrictions, crop boundaries, unsupported locations, stacking, duplicate roster entries, insufficient capacity, context changes and package corruption. Host tests cover idempotent generation, open/deploy/complete and importing accepted terrain into a fresh host without generation.

The overlap fixture at (60,92) has an exact 450-by-129-pixel translation. More than 250 common interior hexes match derived location, terrain, elevation, hexside and bridge facts after excluding the two-hex guard. This is evidence for that fixture, not general admission of arbitrary overlapping crops. The elevation-bearing A/C seam crop remains rejected. Other Situation adapters, full rosters, woodland variation, automatic context rebasing, source-to-pilot focus matching and combat remain follow-up work.

Verification on 13 October 2026: 21 .NET tests passed, including the 32-seed capacity suite and fresh-host import. The solution builds with zero warnings and errors. Existing campaign-file round-trip and Panzer Situation UI/model tests passed. The unrelated `test_command_card_transition.cjs` still fails while looking up an old Ardennes button title; none of its loaded implementation files changed in this integration.

Browser acceptance also passed: fresh Goodwood preparation and formation setup, opening the generated pilot, automatic eight-squad deployment, a manual move from C3 to C7, crop-boundary rejection, completion/review, return with retained formation DOM, page reload, campaign file export/import, and reopening accepted terrain after a host restart. The exported campaign contained the same package identity and all eight placements.

## First-version release review, 14 October 2026

See [Pilot release review](docs/Pilot-Release-Review.md) for all measurements and acceptance results. The Release suite passes 27 tests, including cancellation retry, interrupted publication fault injection and preservation of an existing session after rejected imports. The UI adds keyboard pan/zoom/fit and clearer formation-parent boundaries.

Three fresh-process runs generated and opened terrain in 2.44 to 3.28 seconds, excluding host startup (1.13 to 1.34 seconds). Saved-map reload was 1.81 to 2.08 seconds, so the proposed 2-second gate is not fully passed. Browser paint is excluded. Next: profile cold reload and measure browser open-to-ready latency before expanding the pilot. Reproduce with `scripts/Measure-Pilot.ps1` after a Release build. Raw measurements and limitations are linked from the review.
### Profiling follow-up, 14 October 2026

The release-review work was committed as b5f1d064. Its profiling action is complete. Exact parent lookup now takes 25 to 32 ms instead of 709 ms, with a regression against the exhaustive lookup. Browser resume skips a redundant health request and an unchanged-save rewrite. All identity, terrain and setup validation remains in place.

Three fresh-process API reloads now take 1.74 to 1.85 seconds. First browser open against a restarted host takes 3.14 seconds; warm repeats take 1.20 to 1.93 seconds. The Release suite passes 28 tests, with browser-module persistence and campaign-file regressions also passing. The Pilot release review records raw evidence and measurement boundaries.

Milestone 4 remains open because the end-to-end cold browser result exceeds the proposed 2-second budget. Next: isolate and reduce first-request handling and rendering costs, repeat cold/warm browser timing, and retain the bounded scope until the gate closes or the budget is explicitly revised.
### First-open gate result, 14 October 2026

A bounded follow-up now passes the proposed 2-second saved-map open target in three fresh-host in-app browser checks: 1.63, 1.87 and 1.68 seconds; warm repeats take 1.04 to 1.31 seconds. This supersedes the earlier outstanding cold-open result. Terrain derivation now rejects bridge/tunnel-free bounding rectangles before exact polygon scanning; positive rectangles retain the original semantics. All map response content matches the original lookup.

Campaign tests: 29 passed. Shared ASL map tests: 245 passed. Browser persistence and campaign-file regressions pass. Default runtime tiering is unchanged. See the Pilot release review for raw evidence, method and limits. Stop performance work here for the bounded pilot; wider browser/accessibility acceptance, full formation conversion and other deferred scope remain separate work.
## Selected rifle formation slice, 14 October 2026

Performance and release evidence for the original bounded pilot is committed as 033e4636. That pilot remains available through Open ASL deployment pilot, with its existing eight squads and saves.

Complete Goodwood formation setup, select a deployed British or German rifle counter on the formation map, then choose **Open selected formation ASL map**. The toolbar action is disabled for unsupported counter types or incomplete setup. The map opens four squads belonging only to that selected counter. This follows the existing provisional four-squad rifle profiles, not historical personnel inference; vehicles, engineers and other types are not admitted.

The selected source hex determines a 17-column by 14-row crop. Its origin snaps to four world columns and two world rows, preserving column parity and integer raster translation between neighboring crops. The shared campaign seed, source geometry and feature identities still determine detail. The crop does not create an independent random map or alter formation hex size.

Placement is restricted to admitted ground cells whose primary formation parent is the selected anchor. The two-hex crop guard, terrain exclusions and two-friendly-squad stacking limit still apply. The side's current campaign permissions must include the anchor. Elevation, uncovered map edges or inadequate legal capacity can reject a location. The UI explains the rejection and retains existing accepted maps. Unsupported drafts may remain in the campaign until retried; no partial deployment is installed.

Each formation uses its own campaign slot, M04:formation:<unit ID>, alongside the untouched M04 legacy pilot. Export/import includes all slots. Saves bind the selected unit, anchor, policy, campaign placement/supply snapshot and allowed-parent sets. A changed context is rejected without replacing the saved map. This slice deliberately retains the existing conservative whole-Situation context binding: moving an unrelated formation can also require restoring the original context. Automatic rebasing, reset/replacement UI and migrating squads to another anchor remain follow-up work.

Verification: 34 Campaign Release tests pass. New coverage includes British and German four-squad deployment, parent-boundary rejection, accepted package resume, changed anchor/context rejection, unsupported counter rejection, separate generation job keys, idempotent generation and import into a fresh host without generation. One neighboring A/X-6 and A/W-6 crop pair has matching common interior facts; arbitrary crop-pair equivalence is not claimed. Existing browser persistence/campaign-file tests and syntax checks pass.

Browser acceptance: de-rifle-03 at C/Y-4 generated, deployed 4/4, completed, and resumed after host restart. Keyboard placement outside the parent was rejected without moving a squad. The legacy pilot still opens with 8/8. B/N-8 and B/H-8 examples were rejected because their surrounding crops include source elevation. The selected map was left complete. Broader selected-crop seed, edge and accessibility coverage remains follow-up scope.
### Selected-formation save recovery

Selected rifle maps now tolerate unrelated formation movements and permission changes elsewhere. Current permission for the selected anchor is still required. Older selected-formation saves are validated and upgraded on open without regenerating terrain or resetting squads.

A moved formation offers **Rebuild local map**. This explicitly resets squad placements at the new anchor using the same campaign seed. The old save is retained until generation, validation and storage succeed; cancelling or failing the rebuild preserves it. This does not transfer existing squad positions or retain a history after successful replacement. The original eight-squad pilot keeps its existing context rules.


Crop verification now covers four neighboring pairs across three seeds, deterministic replay, shared interior facts, edge placement rejection, uncovered map edges and explicit elevated-neighbor rejection. A separate cross-seed check verifies building variation with unchanged source geometry. See the implementation plan checkpoint for fixtures and elevation follow-up.


### Elevation-enabled generation

New goodwood-pilot-2 maps preserve source levels 0-2 as ASL base elevation regions, an explicit scenario mapping. The hex inspector shows derived levels. Supported infantry ground setup now admits these elevations. Cliffs and special Slope hexsides are not inferred; stream/deck setup, movement, LOS and combat remain unavailable. Accepted v1 maps load unchanged using their original version. See the implementation plan for PDF rule citations, assumptions and acceptance fixtures.
