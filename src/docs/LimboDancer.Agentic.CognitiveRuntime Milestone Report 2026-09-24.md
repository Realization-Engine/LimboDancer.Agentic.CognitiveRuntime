# LimboDancer milestone report — 24 September 2026

**Reporting window:** 23–24 September 2026 (UTC)  
**Repository:** `Realization-Engine/LimboDancer.Agentic.CognitiveRuntime`  
**Status at publication:** `decision-plane` merged into `main`; ASL Authoring CI and LimboDancer CI passed on the merge.

## Executive summary

Scenario A1 moved from a reviewed first example to a **published, content-addressed ASL domain package with seven bounded occupied-building case contracts**. Each contract can produce a cited, read-only `DomainConclusion` when its exact facts are supplied. Source evidence, semantic acceptance, package identity, and conformance are pinned and checked by affirmative xUnit tests. Two original case labels remain nondefinitive when their controlling facts are unknown.

We also bound Scenario A1 to VASL board 01's explicit building metadata, projected all seven admitted cases from test-supplied state variables, and published a separate post-reveal package for two A12.15 concealment consequences. The new work was merged into `main` with both formerly unrelated branch histories preserved.

## Milestones completed

| Milestone | Result and evidence |
| --- | --- |
| Occupied-building source and case closure | The case matrix records nine labels. Seven have exact admitted fact sets, including ordinary entry, known enemy prohibition, fortified prohibition, Infantry overrun attempt, breach entry attempt, stacking cost, and Advance Phase entry. The source-review chain pins 28 verified subjects; affirmative xUnit checks fragment coverage and rejects missing or altered facts. See the [ASL-OT admission review](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review.md>) and [case matrix](../../docs/ASL/SourceRegistry/asl-scenario-a1.occupied-case-matrix.json). |
| Semantic acceptance and package publication | The exact semantic profile, accepted-case set, source comparison, and package manifest reproduce by digest. [The conformance admission record](../../docs/ASL/SourceRegistry/asl-scenario-a1.conformance-admission.json) admits **seven exact synthetic contracts**, with two nondefinitive labels. The resolver returns applicable rules and controlling exceptions without authority to execute movement. |
| Generic curated review gate | Typed semantic evidence can pass an accepting review only after source readiness and declared-use dependency checks, independent affirmative approval, and separate adjudication when reviewers disagree. Earlier opaque proposal history cannot accept itself. This mechanism is distinct from the bounded Scenario A1 package decision. |
| Board 01 evidence and observation path | [VASL board 01 metadata](<../ASL/docs/VASL Board 01 Terrain Evidence.md>) supplies 63 explicit building-type overrides, pinned to board version 6.9 and its Git blob. xUnit-supplied snapshots provide occupancy, phase, movement, adjacency, and other dynamic variables. The adapter derives all seven exact observations and rejects unknown terrain, conflicting or missing state, changed versions, and unsupported variants. Tests carry those observations through the package to read-only conclusions. |
| Concealment after defender resolution | The [separate post-reveal review](<../ASL/docs/Scenario A1 Concealment Post-Reveal Review.md>) pins A12.15 and A4.14 source fragments and admits two bounded results: forced back after a non-Dummy reveal, and qualified continuation when all defenders are Dummies. The original pre-reveal label remains indeterminate. Bypass, Infantry overrun election, concealed attackers, and follow-on fire remain excluded. |
| Draft rulebook edition | The curated edition gained a rebuilt table of contents and a consolidated builder. It remains draft tooling, isolated from the admitted ASL source and package authority; it is not certified as a replacement rulebook. |
| Integration into `main` | [Merge commit `fd7e016`](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/commit/fd7e016cfbb66d0f46f6cb9e1ef5bf9c9fe9abcf) joined the unrelated histories. The three conflicting architecture documents use `decision-plane`'s versions, while existing `main` files remain present. Both workflows now run on `main`. |

## Verification and scope

- [ASL Authoring CI on `main`](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/35952951955) passed the source, semantic, package, and Scenario A1 checks.
- [LimboDancer CI on `main`](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/35952952008) passed the runtime build and tests.
- Admission covers the declared exact cases and a separate two-outcome post-reveal package. It does **not** infer arbitrary board states, resolve unknown or concealed occupants before disclosure, process board variants or unlisted LOS terrain, or execute an action.
- The unrelated-history merge retained older top-level source paths from `main` alongside the `src/Legacy/` tree. They need an inventory and consolidation decision; neither CI workflow treats them as the active `src/LimboDancer/` solution.

## Next work

1. Model the optional Infantry overrun branch after a concealed SMC reveal, including NTC, MF, another defender reveal, and unresolved response or close combat. Keep it under an exact source-backed package contract.
2. Inventory the older top-level source paths retained by the merge, reconcile unique changes with `src/Legacy/`, and retire duplicates deliberately.
3. Expand the unknown-Location or modifier case only when its controlling variables and source dependencies are explicit. Use LOSData only for terrain facts absent from the verified building overrides.
