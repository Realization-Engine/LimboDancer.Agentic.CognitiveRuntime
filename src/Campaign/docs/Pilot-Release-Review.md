# Goodwood ASL pilot release review

Date: 14 October 2026.
Baseline: `7a63f7aa`, campaign deployment integration. Review changes remain on `codex/formation-asl-map-alignment`.


## Current decision after first-open investigation, 14 October 2026

The bounded pilot's saved-map open target now passes in three fresh-host browser checks on the recorded development machine: **1.63, 1.87 and 1.68 seconds**. Warm repeats take **1.07, 1.04 and 1.31 seconds**. This supersedes the earlier outstanding cold-open performance gate below. The fixed Goodwood crop and eight-squad scope, functional acceptance and stated recovery/accessibility limitations remain unchanged. This is not a cross-machine guarantee or approval to expand the pilot automatically.
## Baseline decision

The bounded eight-squad workflow passes the recorded functional checks. The first-version performance gate is **not fully passed**: one of three fresh-host saved-map reloads exceeded the proposed 2-second budget. Do not expand the roster, terrain profile or Situation coverage on the assumption that milestone 4 is complete. Profile and reduce cold reload cost next, then repeat the same benchmark and measure browser click-to-ready time.

## Baseline performance measurements

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

## Initial profiling follow-up, 14 October 2026

The preceding review was committed as b5f1d064. Its profiling action is complete. The API reload target now passes in three runs, but the first browser open still exceeds 2 seconds. Milestone 4 remains open.

### Changes and correctness

Saved-map loading reports individual validation, compilation, fact derivation, parent lookup and SVG rendering stages. Exact parent lookup originally took 708.5 ms. Inclusive source-polygon bounding boxes now reject impossible candidates before the same exact point-in-polygon check, retaining ordinal tie order. This stage takes 25 to 32 ms in the new server runs. A regression compares every pilot cell with the original exhaustive search.

The browser no longer makes a redundant health request before opening. It avoids rewriting the campaign when resuming an already saved package and setup. New setup and deployment edits still persist; a failed write retains the previous visible deployment. No validation was removed, no accepted-package cache was added, and timings do not participate in saved package identity.

### Repeated server measurements

Same machine, Release build and process-cold method as the baseline:

| Seed suffix | Host startup | Generate and publish | Generate through open response | Saved-map reload |
|---|---:|---:|---:|---:|
| 00 | 1.31 s | 2.00 s | 2.92 s | 1.74 s |
| 01 | 1.19 s | 2.44 s | 3.18 s | 1.85 s |
| 02 | 1.18 s | 1.75 s | 2.49 s | 1.80 s |

All three API reloads pass 2 seconds; generation including startup remains below 4.38 seconds. Peak server working set is approximately 218 to 230 MiB. Package, saved-request and SVG byte counts are unchanged for the three seeds. Browser memory was not measured, so this does not establish a combined memory budget.

Raw evidence: [instrumented pre-optimization run](pilot-reload-profile-before-2026-10-14.json), [optimized benchmark](pilot-performance-optimized-2026-10-14.json).

### Browser open-to-ready measurements

Measured in in-app Chromium against the existing complete eight-squad campaign, with a different saved seed from the server fixture. Timing starts in the pilot open handler and ends after two animation frames following DOM construction. This measures a rendering opportunity, not hardware presentation or a pixel-based readiness assertion.

| Build stage / service state | Open to ready | Campaign write | Longest observed main-thread task |
|---|---:|---:|---:|
| Backend optimized, original browser resume | 3.75 s | 437 ms | 489 ms |
| Skip unchanged write, health request retained | 2.98 s | 0 ms | 129 ms |
| Final changes, first open against restarted host | **3.14 s** | 0 ms | 233 ms |
| Final changes, warm repeat 1 | 1.93 s | 0 ms | 184 ms |
| Final changes, warm repeat 2 | 1.20 s | 0 ms | 231 ms |

The intermediate run spent 1.83 seconds on the health request, now removed. The final cold run spent 2.61 seconds in the open request, including response transfer and parsing, and about 45 ms building the modal. Server-reported load and SVG rendering were 1.31 s and 593 ms. Remaining time includes request handling, scheduling, serialization and rendering opportunities; these measurements do not isolate every component. Warm runs benefit from process/runtime warm-up and must not substitute for the cold result.

[Browser evidence](pilot-browser-performance-2026-10-14.json) contains these five entries in table order. Append &pilotMetrics=1 to the Situation URL and inspect ASL_PILOT_PERFORMANCE console records to reproduce diagnostics. Restart the host for a process-cold first open, then reopen the same saved pilot for warm repeats. Diagnostics do not modify saves. Long-task observation covers only the open interval; a complete responsiveness pass is not claimed.

### Verification and next work

Release .NET suite: **28 passed**. The new browser-module regression passes for read-only resume, direct open without a health preflight, persistence of edits, initial setup persistence and failed-write recovery. Existing campaign-file tests and JavaScript syntax checks pass. Package identities and all eight browser placements remain intact.

Next: profile first-request HTTP/serialization costs, fact derivation, SVG rendering and modal layout. These are candidates, not individually proven causes of the entire remaining gap. Repeat fresh-host and warm browser measurements without weakening validation. Keep the fixed crop and eight-squad scope until the end-to-end gate closes or its budget is explicitly revised. The earlier failure-recovery and accessibility limitations still apply.
## Bounded first-open investigation

### Finding and correction

Terrain-fact derivation checked every hex for bridge/tunnel terrain by building its complete exact polygon cell table. Most pilot hexes contain neither. A clipped bounding-rectangle scan now first checks terrain codes for bridge/tunnel candidates. Only a negative result returns early. Positive rectangles still use the original exact polygon membership and scan order, including cases where terrain lies in a rectangle corner outside the hex. Terrain rules, identities and version constants are unchanged.

Initial three-run HTTP profiling put cold derivation at 523 to 556 ms before and 112 to 147 ms after the optimization. A subsequent controlled original/optimized comparison took 414 versus 122 ms. These timings vary with machine load and runtime warm-up. Exact SVG rendering, roughly 350 to 550 ms in these runs, is now the larger remaining stage.

### Request overhead and runtime experiment

The open endpoint emits Server-Timing for request binding, service work and time from service completion until response headers start. Browser diagnostics capture that header. These are inclusive phase timings, not a CPU/JIT trace, and responseHeaders does not measure the complete response transfer or all serialization. On the final browser cold opens, binding was 18 to 34 ms and responseHeaders 13 to 16 ms. Service work was 1.13 to 1.29 seconds. Browser response transfer/parsing and scheduling account for additional elapsed time.

A single isolated run with runtime tiered compilation disabled took 926 ms for its first HTTP open and 405 to 467 ms for repeats. That falls within the observed default-runtime range and is insufficient evidence for a global runtime setting change. Default tiering remains enabled. No warm-up generation or deferred validation was introduced.

### Controlled content comparison

[Original lookup](pilot-first-open-before-2026-10-14.json) and [optimized lookup](pilot-first-open-after-2026-10-14.json) use the same seed-00 accepted request in separate fresh Release hosts, followed by two repeated opens. First HTTP opens took 1.46 and 1.14 seconds respectively. The stable ordered fingerprint of SVG, cells, setup and accepted bundle is identical in all six responses:

DFFE203D11DFBE5C4A44E578DD6A09A5D21C435D3E4471389C53D58499404190

The comparison uses the same health-probed, process-cold method as the initial profiling. It excludes host startup and PowerShell response parsing. Restore/build transitions were followed by a forced rebuild before the final optimized measurement. Earlier exploratory unordered wrapper hashes were not used as cross-run identity evidence.

The reusable Profile-PilotOpen.ps1 script takes an accepted request file from Measure-Pilot.ps1, starts isolated hosts on port 8097 and measures three consecutive opens per host. It checks stable returned-content identity across opens. The optional DisableTiering switch changes only child-process settings. [Runtime experiment evidence](pilot-tiering-experiment-2026-10-14.json).

### End-to-end browser verification

[Browser evidence](pilot-browser-first-open-final-2026-10-14.json) contains six entries, alternating cold and warm opens of the existing complete campaign:

| Fresh host | First open | Warm repeat | Cold terrain derivation |
|---|---:|---:|---:|
| 1 | 1.63 s | 1.07 s | 150 ms |
| 2 | 1.87 s | 1.04 s | 226 ms |
| 3 | 1.68 s | 1.31 s | 223 ms |

Each cold open followed a host restart without first opening a pilot through another client. Browser code and OS caches were warm; these are not reboot-cold or first-install measurements. The first row followed a page reload; the others reused the mounted formation map. The same accepted campaign, seed, terrain and eight placements were retained. Open-to-ready still means the double-animation-frame opportunity defined above. Longest main-thread tasks were 126 to 226 ms across these six opens, so eliminating all visible pauses is not claimed.

### Validation and stopping point

- Campaign Release tests: 29 passed, including a complete derived-fact fingerprint captured before this change.
- Shared ASL map tests: 245 passed, including a new bridge-in-bounding-box-corner regression.
- Browser-module persistence and campaign-file tests pass; JavaScript syntax and whitespace checks pass.
- Forced Release build: zero warnings and errors. TreatWarningsAsErrors remains enabled.
- Returned terrain, placement and package content matches the original lookup. No save migration is required.

The measured cold-open budget is now met for the bounded pilot. Further SVG/rendering optimization is deferred. Retain broader browser/accessibility checks, power-loss durability limitations, multi-crop/elevation work and full formation conversion as follow-up scope.