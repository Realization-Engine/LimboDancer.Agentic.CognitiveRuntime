# Scenario A1 concealed-SMC Infantry OVR continuation

**Review date:** 2026-09-24

The optional Infantry overrun after an A12.15 concealed-SMC reveal is a separate exact package from both the original known-SMC case and the ordinary post-reveal package. [Transition review](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-transition.json>), [ten-case matrix](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-case-matrix.json>), and [immutable package](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-package.json>) pin the prior package digests and the reviewed source subjects.

## Source and result

A12.15 is verified on physical PDF page 78; A4.14, A4.15, A4.151 and A4.152 on page 49; B23.4 on page 136. The previously reviewed B. Terrain Chart on page 698 supports ordinary building entry at 2 MF, doubled to 4 MF under A4.15. The affirmative xUnit review checks exact fragment IDs, content digests and the dependency chain.

Only one enemy SMC is **revealed first** under A12.15. That does not prove sole occupancy. The qualified case requires a separately supplied fact that no other defender exists, a passed NTC, at least 4 MF, and unresolved defender response/CC. Its output is a read-only permission to attempt Infantry OVR. A4.151 response and conditional A4.152 immediate CC remain unresolved.

| Supplied branch | Conclusion |
| --- | --- |
| Election unknown, NTC unresolved or failed, MF unknown, or further reveal unresolved | Indeterminate |
| Election declined | Abstain here; apply the earlier post-reveal package only under its own exact contract |
| NTC passed, MF insufficient | Abstain from the exact 4 MF attempt |
| Another non-Dummy defender revealed | Indeterminate; sole-SMC permission does not carry over |
| Passed NTC, at least 4 MF, sole SMC verified, response/CC unresolved | Qualified attempt |
| Response or CC already resolved | Abstain pending a separate resolution contract |

The board 01 catalog verifies only the pinned ground-level building terrain. The supplied state source provides occupancy, A12.15 reveal provenance, election, NTC, MF, any further defender reveal, and response status. Missing, stale, contradictory, extra, and wrong-package facts cannot inherit the qualified result. The provider and resolver neither move counters nor spend MF, reveal units, resolve defensive fire, or conduct CC. End-to-end xUnit tests carry supplied state through board validation, exact package, resolver and evidence; the case and provider suites cover all ten branches and negative inputs.

The [September 24 milestone review](<./Scenario A1 Concealed SMC Infantry OVR Milestone Review 2026-09-24.md>) records the admission decision and selects the further-defender reveal as the next separate branch.
