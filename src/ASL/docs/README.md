# ASL domain package documentation

This directory holds the current specifications, designs, and reviews for the Advanced Squad Leader (ASL) domain package under `src/ASL/`.

ASL is the first reference domain and architectural fitness test for LimboDancer. ASL-specific concepts are not part of the core `LimboDancer.Agentic.CognitiveRuntime` architecture. The requirements and acceptance scenarios in the [ASL Reference-Domain Requirements](<./LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements.md>) are authoritative for reference-domain capability but do not prescribe runtime structure, technology choices, or implementation sequence.

The separate-package boundary and contract timing are defined in the [Domain Integration Model](<../../docs/LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md>). The [ASL Ontology Transformation Specification](<./LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) governs the rulebook-to-package authoring lifecycle, validation and publication gates, and first occupied-building adjudication slice.

Source data, schemas, generated TIR artifacts, and the rulebook conversion stay under [`docs/ASL/`](../../../docs/ASL/). See that directory's README for those materials and for historical ASL research.

## Current implementation sequence

```text
ASL-OT-01  Source registry and fragment locator (complete)
ASL-OT-02  Transformation intermediate representation (complete)
ASL-OT-03  Validation and review workflow (03.1-03.3 complete; 03.4 typed-semantic acceptance gate implemented, opaque proposals still blocked)
ASL-OT-04  Scenario A1 semantic package (bounded admission: 7 exact cases admitted, 2 nondefinitive)
ASL-OT-05  Immutable publication and exact resolution (bounded: exact-case package published and resolved by exact identity)
ASL-OT-06  Occupied-building entry adjudication (bounded: 7 exact cases yield read-only conclusions; 2 remain nondefinitive)
```

Scenario A1 continuations, each a separate immutable package reusing the ASL-OT-04 to 06 pipeline:

```text
Post-reveal concealment           2 bounded A12.15 outcomes (forced back; all-Dummies continuation)
Concealed-SMC Infantry OVR        10 cases: 1 qualified attempt, 1 delegated, 2 abstained, 6 indeterminate
Second-defender eligibility       7 cases: 2 definitive, 4 indeterminate, 1 abstained
Second-defender consequence       7 cases: 2 definitive (return; 2 MF in previous Location), 4 indeterminate, 1 abstained
Second-defender return execution  gated action for the clear-return subset of the 2 definitive cases
```

See the [Scenario A1 execution milestone report](<../../docs/LimboDancer.Agentic.CognitiveRuntime Milestone Report 2026-09-24 Scenario A1 Execution.md>) for evidence and limits. The separate map sequence (ASL-MAP-01 to 08) is defined in the [ASL Map Studio Requirements](<./LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>).

Scenario A1 has one governed execution path: the second-defender return (`ScenarioA1ReturnAction` in `LimboDancer.Domains.Asl.Execution`). The Host registers it only through the explicit `AddScenarioA1Return` opt-in, which the production Host does not currently call, and it executes only after the common authority path and Execution Gate, persisting through an atomic versioned journal. No other ASL artifact authorizes execution. A `DomainConclusion` remains an interpretation; any state change independently passes through the common authority path and Execution Gate.

## Documents

- `LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements.md`: normative ASL reference-domain requirements (ASL-RD-001 through ASL-RD-015), end-to-end acceptance scenarios, and traceability to the runtime planes.
- `LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md`: current rulebook-to-ontology authoring, validation, publication, and first-slice specification.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review.md`: conformance decision and implementation evidence for source registration and fragment location.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design.md`: C# design boundary for the TIR schema, deterministic structural extraction, exact sub-fragment evidence, required embedded-boundary recovery, ASL-OT-01 parity, canonical serialization, and reviewable diagnostics.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Review.md`: approval decision, implementation evidence, corpus results, accepted structural interpretations, residual diagnostics, and ASL-OT-03 entry conditions.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-03 Validation and Review Workflow Design.md`: accepted validation/review contract; records completed 03.1-03.3 and the 03.4 work, including the typed-semantic acceptance gate.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review.md`: admission inventory, Scenario A1 source boundary, and evidence for the bounded ASL-OT-04 admission recorded in [`asl-scenario-a1.conformance-admission.json`](../../../docs/ASL/SourceRegistry/asl-scenario-a1.conformance-admission.json). The admission covers exact synthetic facts only; it excludes generic TIR curated acceptance, board-state inference, and action execution.
- `LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md`: candidate supplement for A-E-labelled charts and aids outside the initial PDF pages 6-253, including the B. Terrain Chart on page 698.
- `LimboDancer.Agentic.CognitiveRuntime Scenario A1 Source Review Packet.md`: review subjects, comparison evidence, and delegated review outcome for the bounded Scenario A1 first case.
- `Scenario A1 Concealment Post-Reveal Review.md`: bounded read-only conclusions under A12.15 after the defender reveals a concealed unit.
- `Scenario A1 Concealed SMC Infantry OVR Review.md`: the optional Infantry OVR continuation after an A12.15 concealed-SMC reveal, published as a separate exact package with a ten-case matrix.
- `Scenario A1 Concealed SMC Infantry OVR Milestone Review 2026-09-24.md`: milestone decision closing the concealed-SMC Infantry OVR attempt slice.
- `Scenario A1 Additional Defender Reveal Source Review 2026-09-24.md`: source review and conformance for a second defender revealed before OVR entry resolves, through separate eligibility and consequence packages.
- `Scenario A1 Second Defender Execution Boundary Review 2026-09-24.md`: source-backed execution boundary for the second-defender return. Its status line records the state at review time; the governed return path described above was implemented afterwards.
- `LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md`: proposed requirements (ASL-MAP-001 onward) for the map model, VASL board ingestion, fidelity verification, original map authoring, and the Map Studio application.
- `LimboDancer.Agentic.CognitiveRuntime ASL VASL Board Ingestion Design.md`: proposed design for reading VASL board archives, the `LOSData` wire format, metadata and terrain catalog parsing, VASL-compatible hex-fact derivation, provenance, and the F1/F2 fidelity checks and oracle harness.
- `LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md`: proposed design for coordinates and locations, the Terrain Grid, Hex Facts, the Feature Model and its deterministic compiler, validation, the vectorizer and F3 metrics, Scenes, board composition, the canonical board package, and the consumer read API.
- `LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design.md`: proposed project structure, SVG-only rendering (Exact, Styled, Hex-fact, and Comparison views), themes, render endpoints, and the Blazor Map Studio viewer and editor.
- `VASL Board 01 Terrain Evidence.md`: partial building-override inventory from VASL board 01 metadata and the validated snapshot rules that depend on it.
