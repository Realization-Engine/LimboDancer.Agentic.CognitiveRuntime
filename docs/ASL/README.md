# Advanced Squad Leader domain material

This subtree contains Advanced Squad Leader (ASL) domain research, schemas, renderer experiments, sample data, and historical design notes.

ASL is the first reference domain and architectural fitness test for LimboDancer. ASL-specific concepts are not part of the core `LimboDancer.Agentic.CognitiveRuntime` architecture. The requirements and acceptance scenarios in `legacy-limbodancer-mcp-system-design.md` are authoritative for reference-domain capability but do not prescribe runtime structure, technology choices, or implementation sequence.

The separate-package boundary and contract timing are defined in `src/docs/LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`.

Several documents predate the current six-plane architecture and may mention `.NET 9`, `LimboDancer.MCP`, direct MCP tools, legacy source paths, or earlier ontology implementation choices. Treat those details as historical unless a current document under `src/docs/` explicitly adopts them.

## Domain documents

- `asl-map-architecture.md` — map, scene, hex, terrain, and rendering model.
- `asl-schema-appendix.md` — detailed schema reference.
- `legacy-limbodancer-mcp-system-design.md` — current ASL reference-domain requirements and acceptance scenarios followed by the preserved MCP-era system design.
- `asl-board-hex-management.md` — historical board, LOS, and dynamic-state design notes.
- `asl-rulebook-semantic-primer.md` — historical semantic-search, RDF, and validation exploration.
- `asl-building-renderer-discussion-01.md` — building-renderer design notes.

## Prototypes and sample data

The HTML, JavaScript, and JSON files in this subtree are ASL visualization prototypes and reference data. The `Hex Generators/` subtree contains later modular renderer experiments.

The legacy .NET ASL sample application remains under `src/Legacy/Samples/ASL/` until the broader legacy migration determines its disposition.
