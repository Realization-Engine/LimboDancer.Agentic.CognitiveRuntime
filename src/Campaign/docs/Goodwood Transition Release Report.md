# Operation Goodwood transition: release acceptance

Date: 15 October 2026. Implementation baseline: `7f4b9397`, branch `codex/formation-asl-map-alignment`.

Audit status: accepted for the complete Formation Map/counter transition into the existing ASL setup/play system. The final Campaign regression run passed all 124 tests. The requirement audit below preserves the complete Goodwood scope and records the measured performance and existing-engine limitations.

## Delivered scope

The complete joined Goodwood Formation Map transitions into one authored ASL board, using accepted deterministic terrain rather than regenerating it at handoff. All 114 ground formation counters become 624 ASL instances: 530 units and 94 equipment instances, with 94 explicit weapon/operator relationships. Eight Typhoons remain off-map support. Campaign supply/command markers retain their campaign identities and positions in context rather than becoming invented ASL ground units.

Nominal scale remains 250 m at formation level and 40 m at tactical level. A common lattice covers 1,038 formation parents and 37,788 uniquely owned tactical centers. Eighty-four terrain packages retain compilation overlap; the assembled board removes internal margins and has 40,441 addressable centers including its exterior guard. The guard is excluded from the accepted playable footprint.

## Requirement audit

| Goal requirement | Evidence inspected | Result |
| --- | --- | --- |
| Full geography, orientation, connections, elevation and parent ownership | `SituationCoverageTests`, `SituationSeamTests`, `SituationMapOwnershipTests`, the complete `SituationDeploymentTests` tile-core raster comparison and fresh accepted-seed package/hash comparison; actual parent inspector and boundary overlay | Verified across the complete map |
| Deterministic additional detail and accepted terrain on reload | Pinned source/catalog/profile and saved 128-bit seed; `PilotTests` 32-seed/fixed-geometry and overlap cases, `SituationTerrainTests`, `SituationBundleTests`; byte-identical terrain after actual file transfer | Verified; permitted building detail varies, required source geography remains fixed |
| Every ground counter, stable identity, position and catalog reference | Source [inventory](Goodwood%20Coverage%20Inventory.json), reproducible inventory audit, `VehicleDeploymentTests`, `GunDeploymentTests`, `SituationRosterTests`, complete handoff assertions for all 530 units and 94 equipment objects | Verified; all 24 ground source types accounted for |
| Plausible explicit defaults and equipment/crew relationships | Catalog 1.17.0; [mortar](Campaign%20Mortar%20Game%20Default.md), [guns](Goodwood%20Gun%20Game%20Defaults.md), [vehicles](Goodwood%20Vehicle%20Game%20Defaults.md); complete import asserts catalog references, operator identities and co-location; actual inspector shows Manned by / Operates | Verified with manufactured provenance and current engine limits stated |
| Aircraft and campaign support markers | Full roster retains eight aircraft and the five-aircraft concurrent limit; campaign context retains formation/supply adjustments; readiness and return panel expose the support distinction | Verified as explicit retained support, not ground instances |
| Formation setup through full ASL setup/play, readiness and return/resume | Actual complete-ASL dialog: 114 formations, 624 counters, zero review findings, eight off-map aircraft; actual ASL play, return to Campaign and reopening the same game; controller/file tests | Verified through the interface |
| Keyboard, map controls and inspection | 41 location tests; selected-counter/operator tests; nine viewport JavaScript tests; actual Enter-submitted full-map coordinate, parent inspector, keyboard boundary toggle with exact retained viewBox, readable zoom-dependent/upright labels | Verified for the recorded Goodwood workflow |
| Existing ASL validation and playable bounds | Complete handoff uses GamePlanner setup and replay. Restricted BoardHandle location reads survive replay and live import. Actual same-counter setup proposal refused exterior A1 and accepted owned CCCCCCC13; neither proposal was committed | Verified; no new combat rules |
| Save/load, browser/service restart, campaign-file transfer and later ASL progress | `SituationSessionTests`, complete `StudioHandoffTests.AssertFullImport`; actual export/restore/reload and reconstruction into empty storage at Allied Prep Fire; board/features/context/ownership/footprint/history bytes compared exactly | Verified |
| Existing/manual deployments | Earlier catalog and complete/incomplete legacy crew migration cases; original live 534-position save recovered to 624 without changing those coordinates; complete integration swaps eight infantry/vehicle/crew/weapon positions and preserves them through reload and ASL import/restart | Verified |
| Failure/cancellation preserves accepted state | Publication, bundle, session and import tests reject incomplete/corrupt/future-version data, cancelled jobs, mismatched handoffs and divergent history. Interrupted package publication and board-before-game recovery are fault-injected; original accepted files remain intact | Verified within stated atomic-publication contract |
| Full-size performance, memory, package size and practical usability | Measurements below; redundant grid tracing/unused document rendering removed, retained-board lookup cached, overview text reduced, keyboard submission corrected; screenshots show the complete map and inspection | Measured with documented latency/resource limitations |
| Plan, release results and limitations | [Current README](../README.md), [implementation plan](../../ASL/docs/Formation%20to%20ASL%20Map%20Alignment%20Design%20and%20Implementation%20Plan.md), [chronological evidence](Goodwood%20Transition%20Coverage.md), this audit and raw metrics | Verified; current results and limitations consolidated |

## Current verification

- Complete Campaign suite: **124 passed**, zero failed/skipped, in **9 minutes 28 seconds**. This includes the expanded full-map manual-deployment integration.
- Current focused ASL campaign/context/origin/ownership/footprint/operator/keyboard suite: **79 passed**.
- Viewport JavaScript suite: **9 passed**.
- Campaign-file and full-Situation browser-controller checks: both passed.
- Expanded full-map manual-position/ASL integration: passed in **3 minutes 10 seconds** before the final suite run.
- Builds retained warnings as errors; latest Studio Release build: zero warnings/errors. The reproducible source inventory audit passed. README/release links, metrics JSON, copied evidence and retained terrain/history checks passed.

The full-map integration compares accepted tile-core terrain/elevation pixels, checks every owned center, validates all 624 counters through ASL, exercises phase advance, and reconstructs services before repeated import. This provides complete transition coverage beyond the bounded pilot tests.

## Actual-interface evidence

The original accepted game is `campaign-d52f7b3704cf7cd4d56818f1`, board version `20cf4a5568a997ca0030e53a11d5d20237ff9a42382a18b7852b8213c786b85e`, seed `dccee2730588257b5ba1eb135b1824fd`. The accepted later history contains 626 events and resumes Turn 1 Allied Prep Fire. Its SHA-256 is `155cd7bc7870ca1d2582534edf567dca4ea3df49e59e28fdfb9bee00e7d485aa`.

The empty-storage receiver reconstructed board.json, features.json, campaign context, ownership, footprint and game history byte-for-byte. This proves file transfer independently of a retained server game. The perimeter and typed-setup checks used an isolated 625-event initial-setup copy and cancelled all staged proposals; that history remained `b106c5ad14cee682b679e86e752f980f46cd40456a89a84913c4d3da58b6af93`.

- [Empty-storage transfer at Prep Fire](goodwood-release-evidence/goodwood-empty-store-transfer.png)
- [Visible weapon/operator relationship](goodwood-release-evidence/goodwood-manned-by-inspector.png)
- [Formation parent inspection](goodwood-release-evidence/goodwood-hex-parent-inspection.png)
- [Return to campaign](goodwood-release-evidence/goodwood-campaign-return.png)
- [Exterior setup refusal](goodwood-release-evidence/goodwood-exterior-setup-refused.png)
- [Typed full-map location accepted for review](goodwood-release-evidence/goodwood-typed-location-accepted.png)
- [Readable upright labels after rotation](goodwood-release-evidence/goodwood-upright-zoom-labels.png)

## Performance and resources

Release runs on Windows 10 Pro for Workstations, Xeon W-10885M, 16 logical processors, approximately 128 GiB RAM. SDK 11.0.100-rc.2.26504.105, target net10.0. [Raw results and conditions](goodwood-release-metrics-2026-10-15.json).

| Measurement | Result | Scope |
| --- | ---: | --- |
| Fresh complete terrain generation/publication | 29.29 s | 84 packages, accepted seed, new output directory; process startup excluded |
| Complete-set verification after generation | 7.35 s | Same process, package/grid integrity verification |
| Complete handoff preparation | 27.98 s | Review, terrain reads/assembly, compile, validation, ASL planner/replay, immutable publication |
| Repeated Studio launch/import | 1.62 s | Existing accepted game; browser display excluded |
| Fresh-service navigation to handover scene | 21.37 s | Existing browser cache; full terrain/counters plus two animation frames |
| Updated warm navigation to handover scene | 3.91 / 4.12 / 4.35 s | Three full-page reloads; warm service/browser cache |
| Updated warm viewport scene stage | 2.40 / 2.44 / 2.64 s | Layer load through counter attachment and two animation frames |
| Terrain-generation process peak working set | 489.5 MB | Server process only |
| Campaign process peak after preparation | 1.996 GB | Process lifetime peak |
| Studio process peak after first scene | 1.806 GB | Browser/GPU excluded |
| Stored complete terrain set | 47.06 MB | Manifest/metadata included; Studio caches excluded |
| Full portable campaign file | 32.77 MB | Includes terrain, deployment and 626 ASL events |
| Assembled ASL board package | 0.647 MB | board.json and features.json |

Earlier standalone rendering improved from 32.961 s to 10.626/10.631 s with about 46% fewer cumulative allocations. The label change subsequently reduced the measured warm scene-stage median from 2.7142 s to 2.4394 s in three runs. That small comparison is not a statistically established speedup. Neither figure is a claim of a two-second complete-map open.

The original 10-second generation and two-second reload proposals applied to the bounded pilot; its [separate review](Pilot-Release-Review.md) records that acceptance. The full-Situation costs above remain release limitations. OS/browser caches were not flushed. Two frames allow a paint opportunity, not proof of GPU completion or continuous responsiveness. Browser observation timeouts occurred during drawing; the same tabs subsequently yielded the completed scenes and controls.

## Limits retained

- Setup-ready data does not enable absent ASL combat mechanics. Medium-mortar firing, rocket salvo/reload behavior and any other unsupported actions retain the existing engine's limits. Manufactured factors are explicit game defaults, not historical certification.
- Campaign supply/HQ/depot/truck context and aircraft remain outside the ASL ground roster. Returning ASL outcomes to campaign combat/victory adjudication is outside this transition.
- New procedural detail is the admitted Goodwood profile, including seeded buildings. Additional regional profiles, generated woodland clearings, advanced buildings/fortifications, 240 m rollout and other Situations remain deferred.
- Invalid retained placements require review. Recovery fills gaps explicitly and preserves accepted coordinates; changing a formation does not silently reposition its existing children.
- Portable ASL history is currently limited to 20,000,000 UTF-8 bytes at Studio capture/import; terrain attachment content is size-limited and version-checked. The verified Goodwood history is 580,423 bytes.
- Atomic publication and replay are verified. Automatic cleanup of orphan staging files and power-loss/fsync durability guarantees are not implemented; unaccepted stages remain ignored.
- Full cold startup/preparation is substantial and server memory can approach 2 GB per process. Fast warm opening, cross-machine performance and combined browser/GPU memory are not certified by these measurements.

## Reproduce the checks

Use the current README's build/run instructions. Run `dotnet test src/Campaign/Campaign.slnx -c Release`, `python src/Campaign/scripts/audit_goodwood.py --check`, the viewport Node test, and the two controller/file Node checks from their recorded working directories. Generate with the CLI's `--situation` mode into a new output directory to measure generation rather than reuse. `Measure-GoodwoodHandoff.ps1` measures explicitly selected local job APIs. `mapMetrics=1` exposes browser scene timing attributes for full-page navigation/reload.

The chronological coverage document retains intermediate failures and superseded next steps. This report and the current plan status identify the accepted final behavior.

## Sign-off

The original numbered goal requirements are satisfied for the complete Goodwood transition. The original game retains 626 events at Allied Prep Fire, and both independent acceptance stores retain identical board/features bytes. All verification changes are committed on the implementation branch. The bounded-pilot timing budgets are not claimed as complete-map performance guarantees. No new combat-rule work or historical-research prerequisite was introduced.
