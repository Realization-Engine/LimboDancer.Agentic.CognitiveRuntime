# Scenario A1 concealed-SMC Infantry OVR milestone review

**Date:** 2026-09-24 (UTC)

**Branch:** `decision-plane`

**Decision:** The exact post-reveal Infantry OVR attempt slice is complete. Continue with a separate source-backed additional-defender reveal branch.

## Delivered boundary

| Gate | Reviewed evidence | Result |
| --- | --- | --- |
| Source fidelity | [Transition review](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-transition.json>) pins A12.15, A4.14, A4.15, A4.151, A4.152 and B23.4 fragments; prior PDF comparisons and the reviewed building-cost chart are reused. | Affirmative xUnit review passes. |
| Exact semantic cases | [Ten-case matrix](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-case-matrix.json>) distinguishes election, NTC, MF, additional reveal, sole occupancy and unresolved response/CC. | Only the explicitly verified sole-SMC, passed-NTC, at-least-4-MF attempt qualifies. |
| Publication | [Immutable package](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-package.json>) pins prior occupied and post-reveal package digests, source review and case matrix. | The resolver returns `DomainConclusion` only and rejects a floating package or source set. |
| Observation | The board 01 adapter checks the pinned ground-level building override and consumes versioned test-supplied dynamic state. | Unknown, conflicting, stale, extra, wrong-package or altered-board facts cannot become a qualified observation. |
| Conformance | The xUnit suite carries each of the ten exact cases from supplied snapshot through board terrain, package, resolver and evidence. Existing occupied and ordinary post-reveal package tests remain in CI. | Qualified output has no MF-spend, movement, reveal, fire or CC execution result. |

The earlier known-SMC OVR case begins with a known occupant. This package additionally requires provenance from the A12.15 defender reveal. The earlier ordinary post-reveal package still handles its exact forced-back and Dummies-only cases. A declined overrun election abstains in the new package and can be handled by the earlier package only after satisfying that package's separate observation contract.

## Remaining boundary and next branch

**Next ASL branch:** A12.15 reveals another non-Dummy defender after the attacker elects an optional Infantry OVR against the first revealed SMC. The current case `A1-concealed-smc-another-defender-revealed` is deliberately indeterminate. A4.15 discusses other concealed units and how multiple revealed SMC affect denial of an OVR-capable MMC; A12.15 controls further revelation. Neither rule should be flattened into a generic “another occupant” boolean.

The next increment should first review exact source semantics for a second SMC versus an MMC or other non-Dummy defender, the order and completeness of revelations, and any NTC/MF consequences. Then define separate exact facts and case outcomes with an affirmative xUnit review, publish a **new** immutable package, and project only explicitly supplied defender identity and occupancy state. The current package and its indeterminate second-defender case must remain unchanged. Defender fire and immediate CC after a sole-SMC qualified attempt are a later resolution branch.

Other work from the [September 24 milestone report on `main`](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/main/src/docs/LimboDancer.Agentic.CognitiveRuntime%20Milestone%20Report%202026-09-24.md) remains: inventory the older top-level source paths retained by the merge, and expand unknown Location/modifier cases only after their controlling variables and sources are explicit. Those items do not change the present OVR admission.
