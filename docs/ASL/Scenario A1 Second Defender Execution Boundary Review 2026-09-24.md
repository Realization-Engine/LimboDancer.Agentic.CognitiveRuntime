# Scenario A1 second defender: execution boundary review

**Date:** 2026-09-24 (UTC)

**Branch:** `decision-plane`

**Status:** Source-backed execution review only; no action binding, executor, game-state store or mutation is admitted.

## What the sources establish

The [consequence package](<./SourceRegistry/asl-scenario-a1.second-defender-consequence-package.json>) answers a read-only question for two exact cases: a second revealed SMC or MMC defeats the elected Infantry OVR before entry resolves. The [execution review](<./SourceRegistry/asl-scenario-a1.second-defender-execution-review.json>) pins that package and the previously verified A12.15, A4.14, A4.15 and B23.4 fragments, as well as the reviewed 2 MF building chart. An affirmative xUnit review verifies those identities and the admitted effect vocabulary.

Under the narrow A12.15 facts, the mover returns to its last Location. The ordinary 2 MF attempted-entry expenditure is treated as spent in that Location. A12.15 also says the mover loses Concealment and ends its MPh unless it becomes Berserk first. This case already requires an unconcealed attacker and excludes Berserk, so the concealment effect is a no-op and ending the MPh is a required consequence. The rule does not establish that an additional doubled OVR entrance charge was paid when the OVR could not proceed.

A12.15 separately makes existing Residual FP in the return Location attack on re-entry, including a possible repeat attack in the same phase. FFE and minefields there also attack on re-entry. Wire, Depression, entrenchment and shellholes affect return placement or later TEM. Defender First Fire and Snap Shots are possible on return, while the mover is protected during the brief presence in the attempted target. The return conclusion alone does not choose or resolve any fire attack. This first execution subset therefore requires a confirmed return Location without these conditional hazards; unknown or active hazards block it pending another reviewed resolver.

## Required state and runtime contract

An action must bind the exact tenant, unit, attempted target, last occupied Location, event order, current Movement Phase and pinned package/observation digest. Its authoritative state must distinguish a recorded 2 MF *cost* from MF already *debited* and the pre-return movement stage from a completed return. The whole unit/attempt/return-location state needs one versioned consistency boundary. Otherwise the prior read-only conclusion can be correct at observation time yet stale by the time an executor writes.

The runtime's `ExecutionGate` checks a registered descriptor, executor binding, principal permissions, constraints, diagnostics and risk before producing `AuthorizedAction`. That token carries `ValidatedStateVersions`. `DirectedActionRuntime` then invokes `AuditedActionExecutor`, which audits start and result; `EffectVerifier` can evaluate expected effects after execution. None of these general contracts supplies an ASL state store or enforces an atomic version comparison when ASL position and MF are written. A concrete ASL executor must recheck the expected aggregate version at commit, apply the admitted position/MF/MPh effects together or none, and use a stable attempt key so retries cannot debit MF twice. It must audit and read back the committed state. A conclusion is never an authorization token.

| Input or state | Required handling |
| --- | --- |
| Indeterminate/abstained consequence, wrong package, or mismatched tenant and entities | Reject before the action gate or deny at the gate. |
| Stale observation or aggregate version, changed mover stage, resolved OVR entry | Refuse the write; require a fresh observation and decision. |
| Missing authorization, permission, executor binding, or exact action constraints | Deny; the conclusion has no execution authority. |
| Duplicate attempt with the same payload | Return the prior committed result without another MF debit. |
| Duplicate attempt with a different payload | Reject as a conflict. |
| Unknown or active return-location hazard | Do not apply this limited transition. Review and implement the relevant conditional attack or placement first. |

## Next implementation slice

1. Define a versioned ASL movement aggregate and a pure transition function for the two exact return cases. Test the already-debited versus recorded-cost distinction, ended MPh, stale versions, duplicate attempts and clear-location precondition.
2. Implement an atomic compare-and-swap store behind a domain-specific interface; keep it isolated from VASL board metadata, which supplies only static terrain.
3. Bind an action descriptor and executor through the existing gate with explicit permissions and exact preconditions, then verify effects by reading the committed aggregate. Only the reviewed clear-return subset is eligible.
4. Review conditional attacks and special return terrain separately before any executor handles those states.

### Pure transition increment

`ScenarioA1SecondDefenderReturnTransition` now evaluates those two exact conclusions against a supplied versioned aggregate without modifying its input. It requires matching package, matrix and observation evidence; a pending return stage; a recorded 2 MF attempt; an unconcealed non-Berserk mover in MPh; and a clear return Location. Its candidate state increments the aggregate version, returns the unit, attributes MF in the previous Location, ends movement and records an attempt fingerprint. If the 2 MF were already debited, it does not debit them again. Identical retries return the committed candidate state unchanged; a conflicting retry fails. Hazard and stale-state cases produce no candidate update. This remains a pure model and is not callable as an authorized action.

### Atomic simulation increment

`IScenarioA1ReturnStateStore` and `ScenarioA1InMemoryReturnStore` now connect the transition to a process-local compare-and-swap store. The store recomputes the pure candidate, rejects a changed payload and compares the entire expected aggregate while holding its lock. `ScenarioA1ReturnSimulation` reads, evaluates and attempts that atomic commit; if it loses the race, it reads again to classify an identical retry as a replay or a changed aggregate as stale. Concurrent xUnit attempts verify that only one version increment and MF debit occur. This is an isolated simulation, not a durable state backend or a registered runtime action. Restart, multiple processes, and external game-state synchronization remain outside this store's guarantees.

### Durable journal increment

`ScenarioA1JournalReturnStore` implements the same aggregate contract using an append-only journal keyed by tenant, game and unit. Cooperating store instances take a per-aggregate exclusive file lock; each commit recomputes the exact pure transition, compares the full expected state, appends one checksummed record and flushes it to disk. A new instance can recover the committed return and classify an identical retry without another MF debit. It refuses a truncated or corrupt journal instead of guessing the last valid state. The test covers process-object restart, competing instances and corrupt-tail refusal. No power-loss durability guarantee is claimed for creation of the journal directory or lock file.

The next boundary is a registered action whose executor requires a gate-produced authorization and validates the aggregate version at commit. The conditional A12.15 attacks and special return placement remain blocked.

### Gate binding increment

The separate `LimboDancer.Domains.Asl.Execution` adapter supplies an exact action descriptor, a mandatory `asl.game.return` permission, an operational constraint evaluator, and an executor. The constraint evaluator reads a server-owned conclusion by opaque ID, evaluates the pure transition against the current aggregate, and passes the checked aggregate version into `AuthorizedAction.ValidatedStateVersions`. The executor requires that gate-produced version, resolves the conclusion again, invokes the journal-compatible compare-and-swap simulation, and reads the committed effects back. Caller arguments cannot supply preconditions, effects or a conclusion body. The action has irreversible risk, so the runtime's default policy returns `ConfirmationRequired`; only an explicitly configured risk policy can authorize it. The adapter is opt-in and has no host or MCP registration. Integration tests use an explicit test risk policy and isolated state. Conditional attacks and special return placement stay blocked.

The end-to-end test runs the gate and executor with the journal store, reopens it, obtains a fresh authorization and verifies replay with one version increment and one MF debit. A production host still needs a trusted conclusion source, a configured risk policy and a deliberate action registration. No external ASL game engine or VASL state is changed by these tests.
