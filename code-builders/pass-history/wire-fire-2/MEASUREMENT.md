# PR 85 allocation measurements

Use the same probe source against main `ba97f7d3` (which contains PR 68) and the final PR 85 build. Build both first, stop Codex's Studio, and finish all Codex tests before measurement. Run four fresh processes in order baseline/current/current/baseline, with `DOTNET_TieredCompilation=0`. Use the same saved inputs and boards for every process. Register disposable build outputs under `ASL_GATE_WORK`; keep raw JSON results for review.

The manufactured positions are `wire-c9-gun-review` (American 37mm, normal Gun fire and Canister) and `wire-c9-light-review` (two stacked leaders with a 50mm mortar). Cut each at its initial PFPh position, before its first shot's dice if it has been played. Compare the production `LiveOrdnance.FromState`, `GamePlanner.GunTargetStatus`, and complete fire proposals including their rolled event batches. The store throws on any append. Fixed dice are 3, so Canister is actually used rather than failing its depletion check.

The mortar is read both solo and paired. Main has no second-firer argument and therefore supplies its old lone-SMC reading; current uses the real second SMC. This row measures the cost of the newly supported participation, not an identical old verdict. Its retained ROF and participant evidence may differ as W-C9 specifies. Solo/Gun/Canister inputs and verdicts must otherwise agree. Reflection binds once, accommodates appended optional arguments, and is included on both builds. JSON, binding, replay, setup and garbage collection are outside timed regions.

Main omits `UseMultiple` for the light mortar; the participation reading records the calculator's zero. Keep that evidence difference visible. The comparison allows that change and the paired participant count, and otherwise compares helper answers, event counts, ROF retained and acquisition. An initial probe attempt stopped on the omitted field before writing its result file; both probes were corrected and the complete four-run sequence restarted. No production code or threshold changed for that correction.

The 50mm's colored 3 retains ROF for the pair, while the lone SMC loses ROF. The paired rolled batch has seven events instead of six. Those explicit W-C9 changes are the only additional answer allowances for this fixed position; final raw rows also name every produced event type. The first complete alternating pairs are retained as `initial-*.json`. They measured 8,224 added bytes for the paired warm fact read, crossing 4 KiB in both pairs. Investigation found the second participant gathered again after admission and complete Location resolution repeated even though both units already had direct map positions. Reusing the admitted participant and comparing those existing map positions removes duplicate gathering without a cache or a rule change. All ten affected participation Play cases passed after that adjustment.

The intermediate pairs (`intermediate-*.json`, production `61af8d55`) reduced that increase to 4,648 bytes, still above the unchanged bound. `FromState` also looked up the same weapon separately for each weapon-type branch and its early participant check. It now retains that selected object across those checks, keeping the existing refusal order. The same ten affected cases passed again. The paired batch's extra event is `ConditionsChanged`, recording the additional firer's effect; acquisition remains -1 on both sides.

Helper reads use exact per-thread CLR allocation totals: forty warmups, nine batches of 400 warm reads or 40 fresh-state reads. State cloning is included on both sides. Commands use exact process-wide CLR allocation totals in the otherwise idle measurement process: twenty warmups, nine batches of twenty operations. Record all batch samples, median and p90 time, input hashes, board identities, runtime, assembly identity, participant count and produced event counts. This does not measure browser latency or whole-game scalability.

Stop thresholds declared before collecting results: investigate added helper allocations above **4 KiB warm or 32 KiB fresh-state**, or median/p90 increases above **250 microseconds warm or 2 milliseconds fresh-state**, reproduced in both pairs. For complete proposals investigate added allocations above the greater of **64 KiB or 10% of baseline**, or median/p90 increases above the greater of **2 milliseconds or 20% of baseline**, reproduced in both pairs. Any unexplained verdict or event-count difference is a stop. These are the PR 68 thresholds applied to the changed C9 paths; do not raise them after measuring. Record smaller differences without claiming noisy timing changes as improvements.

## Final results, 2026-10-31

Main is `ba97f7d3edd0c6304cdcc186ebef21bff4ee5d7f`; the measured production build is `68908b2ee4b48dc53375f770e99c317fd357caf8`. Both compile with zero warnings and errors. Codex's Studio, tests and builds were stopped during the four fresh processes. The input hashes match in every run. Raw files are `baseline-1.json`, `current-1.json`, `current-2.json`, and `baseline-2.json`, in that execution order. `run_measurements.ps1` reproduces that order; `compare_measurements.py` compares all 20 workloads and reports zero stops.

| Workload | Added bytes per warm or fresh fact read, both pairs | Added bytes per complete proposal, both pairs |
|---|---:|---:|
| Gun | 696 | 4,408 |
| Canister | 696 | 15,392 |
| Lone-SMC light mortar | 664 | 7,744 |
| Paired light mortar | 3,976 | 33,904 |

Target advice adds zero bytes in every measured workload, warm and fresh. The paired helper increase fell from 8,224 to 4,648 to 3,976 bytes, below the original 4,096-byte warm bound. No threshold was raised. Across both final pairs, the largest helper median/p90 increases are 13.2/17.7 microseconds warm and 13.0/40.2 microseconds fresh. Complete proposals' largest median/p90 increases are 355.3/324.6 microseconds. Mixed timing deltas do not establish a speedup.

All helper answers, event counts, retained ROF and acquisition agree except the explicitly described light-mortar evidence and paired-firer changes. The additional participant is one immutable shot record, with a two-boolean history value. The optimization reuses local facts only: it introduces no cache, global root, viewer data retention or event-history retention. These small-position measurements do not establish whole-game scalability or retained process-memory savings.

## 81mm target advice, added 2026-10-31 (Claude)

Claude's review found that both mortar workloads above use the 50mm, which has no facing, so `GunTargetStatus` returns before the mortar branch PR 85 added; "target advice adds zero bytes" covered only unchanged paths. Two workloads read the 81mm's target advice from `wire-c9-pair-review` at `bd03:B6:0`: `mortar81-solo` (the lone leader) and `mortar81-pair` (with `g-second`). They read target advice only, warm and fresh; the probe takes them as its fifth argument and writes `mortar81-*.json`. Main reads the 81mm as an ordinary Gun and has no second-firer argument, so its paired row is its solo reading, and the answers may differ as W-C9 specifies (the mortar text of the advice); the rows measure the cost, and every answer is recorded.

Stop thresholds declared before collecting these results, the same as above: investigate added allocations above **4 KiB warm or 32 KiB fresh-state**, or median/p90 increases above **250 microseconds warm or 2 milliseconds fresh-state**, reproduced in both pairs.
