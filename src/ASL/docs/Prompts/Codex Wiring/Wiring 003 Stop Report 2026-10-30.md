# Wiring 003 stopping-point report

Stopped at the user's request. No further implementation, tests, rebuilds, gates, or merges are authorized by this checkpoint. Await instructions.

## Completed and verified

- Vehicle breakdown and repair and crossed-LOS SMOKE: implemented on `codex/wire-fire-3`. Its replay gate passed: 128 older games, four new games, and 55,839 sweep lines. Differences were confined to the documented current-reading acceptance game. CI passed on code head `6e03c959` (run 41137914246). The later documentation commit `b1f1595d` records that evidence and the rebuilt SMOKE acceptance view. The user requested that this branch be pushed at shutdown. PR 68 remains unmerged.
- Two-SMC Gun participation: implemented on `codex/wire-fire-2`, including the actual SMC count passed to non-qualified use. Code head `33c95b8b`, documentation head `ee017fe0`, both published. Replay gate passed for 133 games and 56,550 sweep lines; the documented village acceptance changes account for its replay differences. CI passed on the code head (run 41151516582). PR 85 remains unmerged.
- Final Fire Rules adapters and the bounded Fire Lane wreck adapter: committed and pushed on `codex/rules-wiring003-adapters`, head `b3133699`. PR 75 now targets `codex/wire-fire-2`. Focused Rules checks passed. The adapters preserve nullable recorded fields and the earlier null reading. The current claim board is merged into this branch.
- Final Fire and Blaze caller integration: implemented locally on `codex/wire-fire-5`, with the Rules prerequisite merged at `84151654`. The shutdown checkpoint preserves the caller edits, their tests, and a provisional Fire5 gate configuration. This work has not passed its replay gate or CI and is not ready to merge.

## Latest focused validation

Only affected test methods were run locally. Evidence is under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`:

- `Fire5-Blaze-FinalFire-7.log`: seven passing cases for bounded Blaze height, legacy height reading, unknown Sniper versions, and Gun Final Fire with recorded replay.
- `FireLane-Wreck-Provenance-2.log`: two passing tests covering current wreck facts, same-range soft terrain, endpoint visibility, provenance rejection, and an existing lane replay.
- `FinalFire-Ma-Live-Replay-1.log`: one passing test for the MA MG's own counter, picker/command agreement, adjacent Sustained Final Fire, spent state, and replay.
- `Fire5-Smoke-Continuations-9.log`: nine passing cases for Sniper continuations and changed SMOKE recording, ordinary burning-wreck fire, smoke grenades, and mortar recording.
- `Fire5-Ma-Picker-And-Wreck-Rules-2.log`: two passing Rules checks for MA picker availability and Fire Lane hard Hindrance arithmetic/serialization.

The last operation was whitespace formatting of changed C# files. It completed successfully. No tests or builds remain running from this work. A read-only review of the final Fire Lane adapter found no remaining blocker. Geometry is still trusted as a recorded map fact; the verifier validates lane binding and authoritative wreck state, not completeness of the geometric candidate list.

## Work still required

1. Review the final Fire5 diff and complete its documentation, backlog, and performance evidence. Check the provisional `passwire-fire-5.conf` before use. Its proposed acceptance game names are `wire-final-fire` and `wire-blaze-lane`; those games have not been created.
2. Rebuild the Studio from `codex/wire-fire-5`, then exercise Final Fire and Blaze in the browser and record the acceptance games. The current server on port 6680 is still the older Wire2 build (`33c95b8b`), not the Final Fire/Blaze changes. Its copied output is `asl-gate-work-codex/studio-6680-c9`; the live game store is in the `wiring-003-rules` worktree's boards directory. Do not replace user-played games.
3. Run Fire5's required replay gate against the completed Wire2 code baseline (`33c95b8b`, or its documentation-only successor after checking the gate setup). Run the required base/strict/baseline/build/proofs stages and account for every difference. No Fire5 gate stage has started. Earlier Wire2 and Fire3 gates have not been carried forward.
4. Complete claim-state and generated-document updates only after the evidence supports them. W-A8.41, W-B25.2, and W-A9.222 remain claimed by wire-fire-5. The bounded A9.222 Rules supplement is recorded as built; other hard terrain/hexside/bridge and Impulse cases remain outside it.
5. Publish Fire5 and open its PR against the Rules adapter branch. Confirm green CI for the current Rules PR and Fire5 PR. Latest Rules CI was not checked at shutdown. Do not merge code without the user's word.
6. The earlier movement/close-combat wiring and review queue were outside this latest fire continuation and were not re-audited at shutdown. Do not treat them as complete based on this report.

## Locations and dependencies

- Fire3: `C:/Users/dland/.codex/worktrees/wiring-003-rules/LimboDancer.MCP`, branch `codex/wire-fire-3`.
- Wire2: `C:/Users/dland/.codex/worktrees/wiring-003/LimboDancer.MCP`, branch `codex/wire-fire-2`.
- Rules prerequisite: `C:/Users/dland/.codex/worktrees/wire-finalfire-rules/LimboDancer.MCP`, branch `codex/rules-wiring003-adapters`.
- Fire5 checkpoint: `C:/Users/dland/.codex/worktrees/wire-fire-5/LimboDancer.MCP`, branch `codex/wire-fire-5`.
- Preserve wire-fire-4's `UseMultiple` legacy branch, Wire2's single participant implementation, and the current D3.7 recorded-reading fields throughout subsequent merges.

Main received document-only board updates, most recently `af734181`. No implementation from these branches was merged into main. The other agent's main checkout was not edited.
