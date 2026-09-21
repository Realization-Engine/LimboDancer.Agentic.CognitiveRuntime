# Advanced Squad Leader domain material

This subtree contains Advanced Squad Leader (ASL) domain research, schemas, renderer experiments, sample data, and historical design notes.

ASL is a reference domain and use case for LimboDancer. It is not part of the core `LimboDancer.Agentic.CognitiveRuntime` architecture, and the material here is not normative for the runtime.

Several documents predate the current six-plane architecture and may mention `.NET 9`, `LimboDancer.MCP`, direct MCP tools, legacy source paths, or earlier ontology implementation choices. Treat those details as historical unless a current document under `src/docs/` explicitly adopts them.

## Domain documents

- `asl-map-architecture.md` — map, scene, hex, terrain, and rendering model.
- `asl-schema-appendix.md` — detailed schema reference.
- `legacy-limbodancer-mcp-system-design.md` — original repository README and MCP-era system design, preserved with a future cognitive-runtime integration map.
- `asl-board-hex-management.md` — historical board, LOS, and dynamic-state design notes.
- `asl-rulebook-semantic-primer.md` — historical semantic-search, RDF, and validation exploration.
- `asl-building-renderer-discussion-01.md` — building-renderer design notes.

## Prototypes and sample data

The HTML, JavaScript, and JSON files in this subtree are ASL visualization prototypes and reference data. The `Hex Generators/` subtree contains later modular renderer experiments.

The legacy .NET ASL sample application remains under `src/Legacy/Samples/ASL/` until the broader legacy migration determines its disposition.
