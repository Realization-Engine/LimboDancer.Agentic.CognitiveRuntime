# PR 68 allocation probe

Compare the same `Measure.cs` against baseline `ee967f71` and the final PR 68 Studio. Build outputs belong in the registered Codex working directory, never here. Supply `StudioBin` with its trailing slash. Run the compiled executable with repository, read-only proof store, and result JSON paths. Set `DOTNET_TieredCompilation=0` for both fresh processes. Complete all builds and tests and stop the Codex Studio before measuring; run baseline/current twice in alternating order.

The reference is `dl-tractor-works-02`; the small changed-behavior position is the first recorded SMOKE state of `wire-smoke-strength`. A labeled in-memory variant adds an active German Sniper to that SMOKE position, without writing a game event. Inputs, boards and revisions are identical on both sides. The probe binds the actual private fire-LOS and current SMOKE gathering/preflight methods once. The baseline's Location count feeds its own old Rules overloads, exactly as its OVR and Sniper paths do. Baseline Sniper preflight is a no-op because that check is new. This measures the changed gathering work, not browser painting or whole-game scalability.

Measure warm repeated reads and a fresh immutable state per read. State cloning is included on both sides. Forty warmup calls precede nine batches, each of 400 warm or 40 cold reads. Exact synchronous per-thread CLR allocation totals avoid the delayed runtime-counter problem; GC, setup, JSON output and delegate binding are outside measured regions. Report bytes per read, median and p90 batch microseconds per read, counts, verdicts and assembly identity. No absolute process-memory or UI performance claim follows from these figures.

Stop thresholds, set before collecting results: investigate an increase exceeding **4 KiB per warm read or 32 KiB per fresh-state read**, or median/p90 increases exceeding **250 microseconds warm or 2 milliseconds cold**, reproduced in both pairs. These bound this small new per-state index and per-Location work while allowing its new checks; historical A3 thresholds were for a different workload. Any unexplained changed verdict is also a stop. Record smaller differences without calling noisy timing changes a gain. The memory ownership review must still establish weak state/definition keys and no process-wide history retention.

The complete fire and repair proposals are also measured at cuts immediately before their first recorded rolls in `wire-smoke-fire` and `wire-vehicle-repair`. A read-only store rejects any attempt to append. The probe plans, then builds the rolled batch with fixed dice of 4; it never commits it. Nine batches of twenty operations follow twenty warmups. These potentially asynchronous operations use exact process-wide CLR allocated-byte totals in an otherwise idle dedicated process. This is not the delayed periodic runtime counter. Their stop thresholds are added allocations above the greater of 64 KiB or 10% of baseline, or median/p90 increases above the greater of 2 milliseconds or 20% of baseline, reproduced in both pairs. Counts of produced events must agree unless the wiring explicitly explains the difference.

Investigation at `5f48b3b3`: both alternating pairs reproduced 4,360 additional bytes per warm active-Sniper preflight, crossing the 4 KiB threshold. Each call repeated the same public SMOKE column geometry checks and allocated the visited set, Location enumerations and height facts. Keep that completed physical-fact check once per immutable state and planner, with weak keys and no viewer facts. The version/phase/Sniper guards still run before reuse. A new state or another planner gets its own check. Expected benefit: warm preflight below the existing 4 KiB threshold; cold preflight must remain below the existing 32 KiB added-allocation threshold, and every answer must stay equal. Re-run the paired measurements after this change; the thresholds stay unchanged.

## Final results, 2026-10-31

Baseline `ee967f71` versus implementation `db32441e`, Debug, SDK 10.0.403, runtime 10.0.14, 16 visible processors, default Workstation GC in the console probe. This does not change the Studio's collector. Both builds completed before the alternating fresh-process sequence baseline/current/current/baseline. No Codex build, test or Studio ran during measurement; other user applications were not controlled. These are direct production-method measurements, not browser latency or whole-game scalability results.

The reference is revision 1441 of `dl-tractor-works-02`, with 126 units and no active SMOKE. The small position is revision 9 of `wire-smoke-strength`, with three units and one SMOKE source; its in-memory Sniper variant changes no stored record. Fire and repair cuts are revisions 9 and 6 respectively. Raw results in `measurements/{baseline,current}-{1,2}.json` record input SHA-256 hashes, board versions, runtime, timestamps and all 26 workloads. All answers and generated event counts agree in both pairs: fire produces three events, repair two.

| Operation | Baseline bytes/read | Final bytes/read | Added bytes/read |
|---|---:|---:|---:|
| Reference, warm fire LOS | 824 | 136 | -688 |
| Reference, warm OVR or Sniper TEM | 552 | 88 | -464 |
| SMOKE position, warm fire LOS | 1,120 | 440 | -680 |
| SMOKE position, warm OVR or Sniper TEM | 584 | 248 | -336 |
| Active-Sniper preflight, warm | 0 | 40 | 40 |
| Active-Sniper preflight, fresh state | 824 | 23,795.4 | 22,971.4 |
| Complete fire proposal and rolled batch | 223,853.6 | 224,453.6 | 600 |
| Complete repair proposal and rolled batch | 25,513.2 | 65,209.2 | 39,696 |

The repeated Sniper allocation crossing is resolved: 4,360 bytes became 40. Across both final pairs, the largest added helper allocations are 40 bytes warm and 22,971.4 bytes cold, below 4 KiB and 32 KiB. The largest median/p90 helper increases are 0.192/0.238 microseconds warm and 42.555/67.153 microseconds cold, below 250 microseconds and 2 milliseconds. Fire and repair remain below the 64 KiB added-allocation bound and 2 millisecond timing bound; their largest median/p90 increases are 219.8/423.04 microseconds. No final threshold is crossed.

Repair's added work is material even though it stays within its bound: approximately 38.77 KiB for scoped catalog evidence and the new checks. Its final medians are 322.5 to 328.4 microseconds, versus 102.7 to 115.9 before. Fire medians range from 1,232.8 to 1,414.3 microseconds, versus 1,200.1 to 1,475.8 before; the mixed timing differences do not support a general speedup claim.

Ownership review: source snapshots have weak state keys and contain viewer-independent facts. Printed-number caches have weak definition keys. The added Sniper check is planner-local, weakly keyed by the exact state, and retains only its refusal string, never a state, history or viewer. Concurrent fills are equivalent. These checks and measurements do not establish a reduction in retained process memory or larger-game scalability.
