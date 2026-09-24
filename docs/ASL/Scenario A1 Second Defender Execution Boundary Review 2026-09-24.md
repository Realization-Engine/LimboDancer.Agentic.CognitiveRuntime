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
