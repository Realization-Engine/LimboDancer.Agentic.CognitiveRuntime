# Advanced Squad Leader domain material

This subtree contains Advanced Squad Leader (ASL) domain research, schemas, renderer experiments, sample data, and historical design notes.

ASL is the first reference domain and architectural fitness test for LimboDancer. ASL-specific concepts are not part of the core `LimboDancer.Agentic.CognitiveRuntime` architecture. The requirements and acceptance scenarios in `legacy-limbodancer-mcp-system-design.md` are authoritative for reference-domain capability but do not prescribe runtime structure, technology choices, or implementation sequence.

The separate-package boundary and contract timing are defined in `src/docs/LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`. The [ASL Ontology Transformation Specification](<./LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) governs the rulebook-to-package authoring lifecycle, validation and publication gates, and first occupied-building adjudication slice.

Several documents predate the current six-plane architecture and may mention `.NET 9`, `LimboDancer.MCP`, direct MCP tools, legacy source paths, or earlier ontology implementation choices. Treat those details as historical unless a current document under `src/docs/` explicitly adopts them.

## Current implementation sequence

```text
ASL-OT-01  Source registry and fragment locator (complete)
ASL-OT-02  Transformation intermediate representation
ASL-OT-03  Validation and review workflow
ASL-OT-04  Scenario A1 semantic package
ASL-OT-05  Immutable publication and exact resolution
ASL-OT-06  Occupied-building entry adjudication
```

No current ASL artifact authorizes execution. A `DomainConclusion` remains an interpretation; any state change independently passes through the common authority path and Execution Gate.

## Domain documents

- `LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md` — current rulebook-to-ontology authoring, validation, publication, and first-slice specification.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review.md` — conformance decision and implementation evidence for source registration and fragment location.
- `LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design.md` — C# design boundary for the TIR schema, deterministic structural extraction, ASL-OT-01 parity, canonical serialization, and reviewable diagnostics.
- `SourceRegistry/` — reproducible source registry, unverified representative fragment sample, and operator instructions.
- `asl-map-architecture.md` — map, scene, hex, terrain, and rendering model.
- `asl-schema-appendix.md` — detailed schema reference.
- `legacy-limbodancer-mcp-system-design.md` — current ASL reference-domain requirements and acceptance scenarios followed by the preserved MCP-era system design.
- `asl-board-hex-management.md` — historical board, LOS, and dynamic-state design notes.
- `asl-rulebook-semantic-primer.md` — historical semantic-search, RDF, and validation exploration.
- `asl-building-renderer-discussion-01.md` — building-renderer design notes.

## Prototypes and sample data

The HTML, JavaScript, and JSON files in this subtree are ASL visualization prototypes and reference data. The `Hex Generators/` subtree contains later modular renderer experiments.

The legacy .NET ASL sample application remains under `src/Legacy/Samples/ASL/` until the broader legacy migration determines its disposition.
