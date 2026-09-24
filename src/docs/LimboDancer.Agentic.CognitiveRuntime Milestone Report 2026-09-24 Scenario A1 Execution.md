# LimboDancer milestone report: Scenario A1 overrun, second defender, and first governed execution

**Reporting window:** 24 September 2026 (UTC), after the [earlier 24 September milestone report](<./LimboDancer.Agentic.CognitiveRuntime Milestone Report 2026-09-24.md>)
**Repository:** `Realization-Engine/LimboDancer.Agentic.CognitiveRuntime`
**Status at publication:** the remaining `decision-plane` work was merged into `main` by merge commit `10ec7e5`, and the branch was then deleted. ASL Authoring CI and LimboDancer CI passed on `main` at `c082e44`.

## Executive summary

Scenario A1 extended its bounded A12.15 concealment work through three further immutable packages, and then produced LimboDancer's **first governed ASL state change**.

- An optional Infantry overrun after a concealed SMC is revealed is now a separate exact package with ten reviewed cases.
- A second defender revealed before that overrun resolves has separate packages for eligibility and for consequence.
- For the two exact second-defender cases, the mover's return to its previous Location is implemented as a registered action. It runs only through the Execution Gate, with a mandatory permission and irreversible-risk approval, and commits atomically to a versioned, checksummed journal.

The Host can opt in to that action through an explicit registration call. Nothing in the production Host calls it yet.

## Milestones completed

| Milestone | Result and evidence |
| --- | --- |
| Concealed-SMC Infantry overrun | [Transition review](<../../docs/ASL/SourceRegistry/asl-scenario-a1.concealed-smc-overrun-transition.json>), [ten-case matrix](<../../docs/ASL/SourceRegistry/asl-scenario-a1.concealed-smc-overrun-case-matrix.json>), and [immutable package](<../../docs/ASL/SourceRegistry/asl-scenario-a1.concealed-smc-overrun-package.json>). One case is qualified: an explicitly verified sole SMC, a passed NTC, at least 4 MF, and an unresolved defender response or close combat. It yields a read-only permission to attempt the overrun. Of the other nine, one delegates to the earlier post-reveal package, two abstain, and six are indeterminate. See the [review](<../ASL/docs/Scenario A1 Concealed SMC Infantry OVR Review.md>) and [milestone review](<../ASL/docs/Scenario A1 Concealed SMC Infantry OVR Milestone Review 2026-09-24.md>). |
| Second-defender eligibility | [Case matrix](<../../docs/ASL/SourceRegistry/asl-scenario-a1.second-defender-reveal-case-matrix.json>) and [package](<../../docs/ASL/SourceRegistry/asl-scenario-a1.second-defender-reveal-package.json>). Of seven cases, two are definitive: a second revealed SMC denies the single-SMC overrun, and a revealed MMC makes the single-SMC exception inapplicable. Four are indeterminate and one abstains. Revelation order is modeled as supplied, ordered events. |
| Second-defender consequence | [Case matrix](<../../docs/ASL/SourceRegistry/asl-scenario-a1.second-defender-consequence-case-matrix.json>) and [package](<../../docs/ASL/SourceRegistry/asl-scenario-a1.second-defender-consequence-package.json>). The same two cases conclude, read-only, that the mover returns to its previous Location and that the attempted 2 MF ordinary building entry counts as spent there. No doubled overrun charge is inferred. Four cases are indeterminate and one abstains. See the [source review](<../ASL/docs/Scenario A1 Additional Defender Reveal Source Review 2026-09-24.md>). |
| Execution boundary review | The [execution review record](<../../docs/ASL/SourceRegistry/asl-scenario-a1.second-defender-execution-review.json>) pins the core effects: return, MF attributed to the previous Location, and the end of the MPh. It keeps conditional hazards in the return Location outside the first subset: Residual FP, fire for effect, minefields, wire, depressions, entrenchments, and shellholes. See the [boundary review](<../ASL/docs/Scenario A1 Second Defender Execution Boundary Review 2026-09-24.md>). |
| Pure transition and atomic store | `ScenarioA1SecondDefenderReturnTransition` evaluates a versioned aggregate without mutating it. It distinguishes an MF cost that is only recorded from one already debited. It is idempotent for identical retries and rejects conflicting ones. `ScenarioA1InMemoryReturnStore` adds compare-and-swap, and concurrent tests confirm a single version increment and a single MF debit. |
| Durable journal | `ScenarioA1JournalReturnStore` is an append-only journal keyed by tenant, game, and unit. Each aggregate has an exclusive file lock, each record is checksummed and flushed, and commits compare the full expected state. It recovers after a restart and refuses a corrupt or truncated tail rather than guessing. It claims no power-loss durability for directory or lock-file creation. |
| Gate binding | `LimboDancer.Domains.Asl.Execution` supplies the action descriptor (`ScenarioA1ReturnAction`, irreversible, internal), the mandatory `asl.game.return` permission, a constraint evaluator, and an executor. The evaluator rebuilds the conclusion from a server-owned ID and passes the checked aggregate version through `AuthorizedAction.ValidatedStateVersions`. The executor rechecks that version at commit and reads the committed effects back. Callers cannot supply preconditions, effects, or a conclusion body. The runtime's default risk policy returns `ConfirmationRequired`, so only an explicitly configured policy can authorize the action. |
| Host opt-in | `AddScenarioA1Return(cases, snapshots, store)` registers the action, binding, executor, and a Host constraint evaluator that keeps fail-closed behavior for every other action. `ScenarioA1VerifiedReturnConclusionSource` rebuilds each conclusion from a trusted case ticket and current authoritative events, and accepts only a definitive result. The call is exercised by `ScenarioA1HostRegistrationTests` and is not made by the production Host. |

## Architecture boundary

- `LimboDancer.Host` is the only runtime project that references an ASL assembly, and it references only `LimboDancer.Domains.Asl.Execution`. `ProductionDependencyTests` enforces an exact allowed reference set for every runtime project: Runtime and Infrastructure may reference only Abstractions.
- The ASL authoring architecture test was narrowed from "no runtime project references `LimboDancer.Domains.Asl`" to "no runtime project references `LimboDancer.Domains.Asl.Authoring`". The runtime-side allowed-reference test remains the authoritative guard.
- ASL Authoring CI now also runs when `src/LimboDancer/LimboDancer.Host/**` changes.
- Board 01 terrain evidence remains static. The post-reveal, overrun, and both second-defender observation providers take the concrete `Board01TerrainCatalog` class as a constructor parameter, and the execution path's `ScenarioA1VerifiedReturnConclusionSource` constructs it directly. Every reveal, unit, NTC, MF, event-order, and Location fact comes from the supplied state source.

## Verification

- [ASL Authoring CI](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/36012927124) on `c082e44` passed the source registry, Scenario A1 domain adapter, verification batch, comparison record, and chart supplement steps.
- [LimboDancer CI](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/36012927138) on `c082e44` passed the runtime build and tests.
- Before the merge, the merged tree was built and tested locally. The runtime solution had 0 warnings and passed 200 of 200 tests, and Scenario A1 passed 144 of 144. Seven ASL authoring tests fail only on Windows, identically on `main` before the merge, because of line-ending handling; they pass in CI on Linux.

## Scope and limits

- Only the clear-return subset of the two exact second-defender cases can execute. Unknown or active hazards in the return Location block execution.
- Defender fire, Residual FP, fire for effect, minefields, special return placement, further movement, and close combat remain unresolved.
- The journal is a process-level store for tests and local use. It does not synchronize with an external game engine or with VASL.
- No MCP exposure of the action exists.

## Corrections to earlier documents

These dated documents recorded the state at the time they were written. This report supersedes the following statements:

- The [Second Defender Execution Boundary Review](<../ASL/docs/Scenario A1 Second Defender Execution Boundary Review 2026-09-24.md>) status line says no binding, executor, store, or mutation is admitted, and its gate-binding section says the adapter has no Host registration. Both were superseded by later commits: the store, executor, and binding in the same review's own increments, and the Host opt-in in `a71b941`.
- The [Additional Defender Reveal Source Review](<../ASL/docs/Scenario A1 Additional Defender Reveal Source Review 2026-09-24.md>) ends by saying no executor or state mutation is introduced. The execution path above supersedes it.
- The [earlier 24 September milestone report](<./LimboDancer.Agentic.CognitiveRuntime Milestone Report 2026-09-24.md>) says admission does not execute an action. Next-work item 1 (the overrun branch) is complete. Next-work item 2 (the legacy path inventory) was completed by archiving the legacy tree as `src/_Legacy/`.

## Next work

1. Review and resolve the conditional return-Location hazards before any executor handles them. Depressions and shellholes are printed board terrain, which the planned ASL Map Studio Hex Facts can later supply. Wire, entrenchments, Residual FP, fire for effect, and minefields are game state and need their own supplied-state contracts.
2. Decide whether and how a production Host should call `AddScenarioA1Return`, including the trusted case source, the risk policy, and the store location.
3. Begin the ASL Map Studio implementation sequence at ASL-MAP-01, as defined in the [ASL Map Studio Requirements](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>).
