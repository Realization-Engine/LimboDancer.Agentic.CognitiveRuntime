# Advanced Squad Leader domain material

This subtree contains Advanced Squad Leader (ASL) domain research, source data, schemas, renderer experiments, sample data, and historical design notes.

The current ASL specifications, designs, reviews, and implementation sequence are in [`src/ASL/docs/`](../../src/ASL/docs/), alongside the domain package they govern. The source data, schemas, and generated artifacts below are the evidence those documents cite.

ASL is the first reference domain and architectural fitness test for LimboDancer. ASL-specific concepts are not part of the core `LimboDancer.Agentic.CognitiveRuntime` architecture. The requirements and acceptance scenarios in `legacy-limbodancer-mcp-system-design.md` are authoritative for reference-domain capability but do not prescribe runtime structure, technology choices, or implementation sequence.

Several documents predate the current six-plane architecture and may mention `.NET 9`, `LimboDancer.MCP`, direct MCP tools, legacy source paths, or earlier ontology implementation choices. Treat those details as historical unless a current document under `src/docs/` or `src/ASL/docs/` explicitly adopts them.

## Source data, schemas, and generated artifacts

- `Rulebook_Markdown/`: registered converter output for the rulebook TOC, Index/Glossary, and Chapters A-E.
- `CuratedEdition/`: curated Markdown edition built deterministically from the registered conversion, with its decisions, ledger, and review records.
- `SourceRegistry/`: reproducible source registry, unverified representative fragment sample, Scenario A1 evidence files, and operator instructions.
- `Schemas/asl-tir-1.3.schema.json`: versioned ASL-OT-02 structural TIR envelope and artifact schema, including typed section boundaries and exact UTF-8 sub-fragment spans.
- `Schemas/asl-tir-review-record-1.0.schema.json`: versioned ASL-OT-03 immutable review-record family and exact-subject identity contract.
- `Schemas/asl-tir-review-record-1.1.schema.json`: current ASL-OT-03 review-record schema, adding complete source-evidence context, verified dependency hashes, and finding severity while retaining schema 1.0 unchanged.
- `Schemas/asl-tir-review-bundle-1.0.schema.json`: captured-TIR review-bundle schema; does not authorize curated or accepted states.
- `Schemas/asl-tir-curated-proposal-1.0.schema.json` and `Schemas/asl-tir-curated-review-bundle-1.0.schema.json`: exact authored proposal and proposed-only submission.
- `Schemas/asl-tir-curated-review-transition-1.0.schema.json` and `Schemas/asl-tir-curated-review-bundle-1.1.schema.json`: non-accepting review opening/rejection records and history.
- `TIR/`: deterministic metadata-only structural TIR review artifacts and regeneration instructions.

## Domain requirements and design notes

- `legacy-limbodancer-mcp-system-design.md`: current ASL reference-domain requirements and acceptance scenarios followed by the preserved MCP-era system design.
- `asl-map-architecture.md`: map, scene, hex, terrain, and rendering model.
- `asl-schema-appendix.md`: detailed schema reference.
- `asl-board-hex-management.md`: historical board, LOS, and dynamic-state design notes.
- `asl-rulebook-semantic-primer.md`: historical semantic-search, RDF, and validation exploration.
- `asl-building-renderer-discussion-01.md`: building-renderer design notes.

## Prototypes and sample data

The HTML, JavaScript, and JSON files in this subtree are ASL visualization prototypes and reference data. The `Hex Generators/` subtree contains later modular renderer experiments.

The legacy .NET ASL sample application is archived under `src/_Legacy/Samples/ASL/` for archival purposes only.
