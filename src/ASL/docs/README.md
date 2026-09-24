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

No current ASL artifact authorizes execution. A `DomainConclusion` remains an interpretation; any state change independently passes through the common authority path and Execution Gate.

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
- `VASL Board 01 Terrain Evidence.md`: partial building-override inventory from VASL board 01 metadata and the validated snapshot rules that depend on it.
