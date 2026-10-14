# Formation-to-ASL pilot

This implements the bounded package pilot from the Formation to ASL Map Alignment Design and Implementation Plan: pinned inputs, deterministic metric features, an ASL authored-board package, compiled terrain, derived hex facts and inspection output. It does not enable the campaign deployment/gameplay workflow.

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

The package, manifest and catalog are staged and atomically published together. The CLI adds derived previews afterwards. Repeated publication verifies existing content instead of overwriting it. Copy the complete hash directory to transfer an accepted package; the campaign file-import UI is not integrated yet.

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

These codes are supported by compilation and fact derivation. That is separate from executable rule coverage: the existing Scenario A1 building-evidence path is pinned to VASL board 01. No pilot hex is marked deployment-admitted. Nonzero source elevations and unsupported terrain in the requested crop/guard region fail closed. The primary crop contains no woodland; generated connected clearings are deferred, not silently claimed as implemented.

The overlap test verifies shared world-feature IDs and geometry, not crop-edge fact equivalence. A/C join projection is covered; the proposed seam crop at (52,116) is deliberately rejected because it includes unsupported hills. Further join admission requires an elevation profile. Parent ownership is center-based with ordinal source-key ties; overlap references use the tactical hex polygon.

## Validation and limits

Tests cover independent SHA-256 stream vectors, rotations, negative parity, quantization, source/seed rejection, repeated output, 32 seeds, bridge-to-road connectivity, shared features in overlapping crops, source joins, unsupported hills, corruption and cancellation. Shared ASL build settings are imported and every pilot project explicitly sets `TreatWarningsAsErrors` to `true`.

The first tested seed produced only MAP-VAL-007 warnings: feature boundaries close to ASL sampling points. These are exported, not discarded. A zero-error authored package is sufficient for inspection; playable setup and movement require separate admission work.

Next phase: validate crop-interior fact continuity and canonical raster alignment, define the bounded roster/setup rules, add the campaign service and navigation workflow, and verify save/resume through the user interface. This pilot does not resize existing maps or migrate saved deployments.

## Recorded pilot verification

On 13 October 2026, .NET SDK 10.0.402 on Windows: 14 tests passed, including the 32-seed acceptance test. The final CLI run with seed `00112233445566778899aabbccddeeff` generated 65 features and 676 hexes, published/reloaded/recompiled the package and rendered the preview in 2.18 seconds. This is one measured development-machine run, not a cold-start or broad hardware benchmark. Package version: `6907b65ed7ab86be88d3add25d78639eecdeef86ad7304abfdb9ec81e7d4677b`.

The package has zero validator errors and 175 MAP-VAL-007 sampling-boundary warnings, retained in the manifest. In native Map Studio it loaded as Authored, valid; selecting R10 displayed Stone Bridge, Shallow Stream locations and road/bridge hexside facts. These observations verify compilation and inspection, not combat admission. No existing campaign save or application UI was modified by the pilot.
