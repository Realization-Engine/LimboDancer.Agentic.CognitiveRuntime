# Goodwood ASL pilot release review

Date: 14 October 2026.
Baseline: `7a63f7aa`, campaign deployment integration. Review changes remain on `codex/formation-asl-map-alignment`.

## Decision

The bounded eight-squad workflow passes the recorded functional checks. The first-version performance gate is **not fully passed**: one of three fresh-host saved-map reloads exceeded the proposed 2-second budget. Do not expand the roster, terrain profile or Situation coverage on the assumption that milestone 4 is complete. Profile and reduce cold reload cost next, then repeat the same benchmark and measure browser click-to-ready time.

## Performance measurements

Measured the Release host through its HTTP API on Windows 10 Pro for Workstations, Intel Xeon W-10885M at 2.40 GHz, 16 logical processors, approximately 128 GiB RAM. SDK 10.0.402; test runner reports runtime 10.0.13. Three seeds, each using a fresh generation process, a unique empty package root and a second fresh process for accepted-save reload. No compiler/package cache survives between processes. OS disk caches were not flushed; these are process-cold measurements, not reboot-cold measurements.

| Seed suffix | Host startup | Generate and publish | Generate through open response | Saved-map reload | Generation peak working set | Reload peak working set |
|---|---:|---:|---:|---:|---:|---:|
| 00 | 1.34 s | 2.69 s | 3.28 s | 2.08 s | 223.5 MiB | 220.8 MiB |
| 01 | 1.13 s | 2.12 s | 2.65 s | 1.81 s | 226.8 MiB | 230.4 MiB |
| 02 | 1.13 s | 1.91 s | 2.44 s | 1.95 s | 239.2 MiB | 224.0 MiB |

Generation plus host startup remains below 4.62 seconds in all three runs, within the 10-second proposal. Reload median is 1.95 seconds, maximum 2.08 seconds, with one failure of the 2-second proposal. Do not round this into a pass. Reload excludes host startup because the application normally already has a running service; even so, it uses a fresh service with no compiled-map cache.

Timings include HTTP transfer and PowerShell request/response serialization. Generation includes polling at 20 ms intervals; the through-open column also includes setup validation, cells and SVG rendering. Reload verifies accepted features, package and compiled-grid identity, derives facts, validates setup and renders the response. It does not regenerate terrain. Browser JSON parsing, storage, DOM construction and paint are not included. Browser responsiveness was exercised functionally, but no instrumented browser long-task or click-to-ready budget is claimed.

Published package directories were 684,746 to 695,368 bytes (669 to 679 KiB). Accepted save requests, including the fixture context and eight placements, were 479,212 to 491,194 UTF-8 bytes (468 to 480 KiB). Exact SVG responses were 227,405 to 227,878 bytes (about 222 KiB). These are pilot payloads, not whole-campaign file sizes. Peak working set is the OS-reported host process high-water mark, not managed heap or combined browser/server memory. No memory budget has yet been agreed.

Raw measurements: [pilot-performance-2026-10-14.json](pilot-performance-2026-10-14.json).

Reproduce from the repository root, with port 8096 free:

```powershell
dotnet build src/Campaign/Campaign.slnx -c Release --nologo
./src/Campaign/scripts/Measure-Pilot.ps1
```

The script starts hidden loopback hosts on an isolated port, leaves timestamped evidence in a unique `artifacts/pilot-review-*` directory and stops only its own processes. Existing browser campaigns and the host on 8093 are untouched.

## Failure recovery

The full Release test suite passes: **27 tests**, including the existing 32-seed capacity and overlap checks and six new recovery cases.

| Failure | Evidence and result |
|---|---|
| Cancellation | Cancel an active generation job, wait for cancelled status, verify no accepted directory, retry the identical seed and verify the expected package identity. Pass. |
| Cancellation during staged publication | Existing pre-cancelled publication test exercises the cancellation check after staging/verification and verifies staging cleanup with no published package. Pass. |
| Interrupted publication | Construct an incomplete orphan staging directory, inject a rename failure by occupying the final path with a file, verify no accepted directory and cleanup of the current stage; remove the obstruction and retry successfully. The orphan remains ignored. Pass. This is fault injection, not a power-loss durability test. |
| Invalid save | Malformed feature JSON and corrupt package/grid/catalog identities are rejected. A previously opened session can still complete its unchanged eight placements. Pass. |
| Incompatible versions | Reject changed compiler, derivation, generator and serialization versions. The existing session remains usable. Pass. |
| Campaign/context mismatch | Existing tests reject seed, campaign, formation and permission changes without replacing accepted deployment. Pass. |
| Browser close during generation request | Added a cancellation check after the generation request returns, covering closure before its response. This race fix is code-reviewed; not claimed as a browser fault-injection test. |

A forced process exit may leave `.stage-*` directories on disk. They are never accepted or scanned as published maps, and retry uses another staging directory. Automatic orphan cleanup and power-loss/fsync guarantees are not implemented. Atomic rename is the publication boundary; derived CLI previews are outside that transaction.

## UI acceptance and corrections

Verified in the in-app Chromium browser on the existing Goodwood campaign at port 8093:

- Open via keyboard; select a destination and activate placement, completion and return with Enter. All eight existing placements remain intact.
- Added a focusable map region, visible focus outline, arrow-key panning, plus/minus zoom and Home to fit. Verified viewBox changes and fit restoration through rendered DOM. Mouse pan/wheel remain available.
- Changed the parent overlay from repeated outlines/labels on every tactical hex to shared parent-ownership boundaries and one label per parent. This is the tactical center-based ownership boundary, not an exact source-polygon overlay.
- Confirmed inspection of E6 stone building, L4 paved road, R9 shallow stream and R10 stone bridge. Bridge and stream locations explicitly report that setup is not admitted. Inspection now explains side-specific formation restrictions separately from terrain admission.
- Markers remain 26 by 26 map units against 64.5-unit vertical hex spacing, about 40 percent of a hex, and scale with the map. Inspected at fit and enlarged views. Text stays upright. At overview scale small markers/labels require zooming to read.
- A rejected A1 boundary placement retains all eight squads; its warning disappears after five seconds.
- Complete/review and placement work by keyboard. Replacing the British squad's placement with its existing C7 location preserves the roster. Left the setup complete.
- Return and reopen preserve the exact pilot viewBox. Focus returns to the pilot launcher. The formation view remains mounted while the pilot is open. Prior integration acceptance also verified campaign export/import and restoration after a host restart.
- Disabled text selection in the map to prevent blue selection highlights while panning.

This is a functional keyboard and visual check, not a complete screen-reader, high-contrast, mobile or cross-browser accessibility audit. Chrome-specific testing remains separate from in-app Chromium acceptance. Parent visibility is a temporary display preference and is not saved.

## Remaining release limits and next work

1. Profile cold accepted-package reload, reduce its cost without skipping identity/terrain validation, then repeat the benchmark. Add browser-side open-to-ready and long-task measurements before declaring the full performance gate passed.
2. The fixed primary Goodwood crop and eight infantry squads remain the admitted scope. No selection-based crop, full roster conversion, formation-anchor deployment radius or automatic context rebasing.
3. No movement, LOS, combat, victory adjudication or campaign outcome reconciliation. Generated buildings are plausible detail, not verified historical footprints.
4. No admitted elevated A/C seam crop, arbitrary overlapping crops or generated woodland clearings. Boundary fact equivalence is proved only for the recorded primary/(60,92) fixture with its two-hex guard.
5. Sampling-boundary warnings remain visible in the manifest. The package's inspection-only status and the separate bounded setup policy remain distinct.
6. Repeat broader accessibility and browser acceptance when the next workflow is introduced. Address the unrelated existing Ardennes command-card transition test separately.
