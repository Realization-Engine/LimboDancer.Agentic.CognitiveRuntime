# LimboDancer.Agentic.CognitiveRuntime — Documentation Index

This directory contains the active architecture and implementation documentation for **LimboDancer.Agentic.CognitiveRuntime**, together with selected `LimboDancer.MCP` documents retained as legacy reference material.

The current system is a domain-aware runtime that lets AI systems reason, choose, and act against real state without giving the AI direct authority over the world. MCP is one interaction adapter; it is no longer the product or architecture boundary.

## Document authority

When documents disagree, use this order of authority:

1. [Plane Runtime Specification](<./LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md>) — normative implementation requirements.
2. [Plane Runtime Design](<./LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md>) — target runtime design supporting the specification.
3. [Implementation Plan](<./LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md>) — current engineering sequence.
4. Current supporting analyses and models listed below.
5. Legacy `LimboDancer.MCP` documents — historical and behavioral reference only.

## Current implementation documents

| Document | Role |
| --- | --- |
| [Plane Runtime Specification](<./LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md>) | Normative runtime contract and conformance requirements. |
| [Plane Runtime Design](<./LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md>) | Detailed target design for the six-plane runtime and cross-cutting fabrics. |
| [Implementation Plan](<./LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md>) | Ordered milestones, pull-request sequence, tests, and acceptance gates. |
| [Milestone A Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone A Conformance Review.md>) | Directed-runtime conformance decision, first ASL adjudication scenario, and approved PR-12/PR-13 contract boundary. |
| [Milestone B Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone B Conformance Review.md>) | Autonomous-selection conformance decision, integrated Goal-to-SelectedAction proof, and approved PR-15 Reasoning boundary. |
| [Milestone C Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone C Conformance Review.md>) | Autonomous-execution conformance decision, bounded multi-step Goal loop, diagnostic lifecycle handling, and approved PR-17 verification boundary. |
| [Milestone D Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone D Conformance Review.md>) | Effect-verification and replay-evidence conformance decision plus the bounded entry conditions for additional Decision providers. |
| [PR-19 OpenAI Decision Provider Design](<./LimboDancer.Agentic.CognitiveRuntime PR-19 OpenAI Decision Provider Design.md>) | Bounded structured-output provider slice, budget semantics, configuration boundary, and replay-only evaluation requirements. |
| [PR-19 Evaluation Review](<./LimboDancer.Agentic.CognitiveRuntime PR-19 Evaluation Review.md>) | Provider-boundary conformance evidence, offline evaluation measures, and the evidence still required before adoption. |
| [Decision Evaluation Corpus Specification and Runbook](<./LimboDancer.Agentic.CognitiveRuntime Decision Evaluation Corpus Specification and Runbook.md>) | Corpus evidence classes, manifest and review requirements, ASL rule-source profile, holdout controls, thresholds, and operator-run procedure. |

## Architecture analyses and runtime models

| Document | Role |
| --- | --- |
| [Plane Architecture Analysis](<./LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis.md>) | Recasts the system as six logical planes. |
| [Plane Architecture Codebase Validation](<./LimboDancer.Agentic.CognitiveRuntime Plane Architecture Codebase Validation.md>) | Tests the plane model against the legacy implementation and identifies gaps. |
| [Decision Plane Architecture](<./Decision Plane Architecture.md>) | Defines bounded selection, abstention, escalation, and the relationship between decision and execution authority. |
| [Runtime Orchestration Model](<./LimboDancer.Agentic.CognitiveRuntime Runtime Orchestration Model.md>) | Defines how goals and directed requests progress across runtime authority boundaries. |
| [Native Local Decision Model](<./LimboDancer.Agentic.CognitiveRuntime Native Local Decision Model.md>) | Research direction for a future LimboDancer-owned local semantic choice engine, including surveyed implementations, invariants, corpus requirements, and the evidence gate that keeps implementation deferred. |
| [Anthropic Decision Provider Feasibility and Design](<./LimboDancer.Agentic.CognitiveRuntime Anthropic Decision Provider Feasibility and Design.md>) | Candidate remote-provider design based on Storyvizor, cookbook, and the official Anthropic C# SDK; implementation remains outside the admission gate. |

## Domain integration and knowledge modeling

| Document | Role |
| --- | --- |
| [Domain Integration Model](<./LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md>) | Defines the boundary, dependency direction, composition model, and interface timing for separately implemented domain packages. |
| [Domain Knowledge Modeling Requirements](<./LimboDancer.Agentic.CognitiveRuntime Domain Knowledge Modeling Requirements.md>) | Supporting guidance for authoritative knowledge, semantic evidence, and changing domain state. |
| [ASL Ontology Transformation Specification](<../../docs/ASL/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) | Current authoring lifecycle, intermediate representation, validation and publication gates, package profile, and first occupied-building adjudication slice. |
| [ASL-OT-01 Source Registry Review](<../../docs/ASL/LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review.md>) | Approved source registry, fragment-locator evidence, explicit unverified sample, isolation boundary, and ASL-OT-02 admission. |
| [ASL-OT-02 TIR Schema and Deterministic Extraction Design](<../../docs/ASL/LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design.md>) | C# authoring boundary, TIR schema design, deterministic structural extraction, canonical serialization, and diagnostic requirements. |
| [ASL reference-domain requirements](<../../docs/ASL/legacy-limbodancer-mcp-system-design.md>) | Concrete requirements and acceptance scenarios the reusable architecture must ultimately support. |
| [ASL documentation index](../../docs/ASL/) | Index of ASL domain research, schemas, prototypes, and historical material. |

Advanced Squad Leader is the first reference domain and architectural fitness test. ASL-specific concepts belong in a separate domain package and must integrate through domain-neutral runtime contracts.

## Current source status

The new `.NET 10` production solution is active under `src/LimboDancer/`. Milestone A includes the directed action-authority substrate, Diagnostics, the Execution Gate, audit, tenant-safe reference State providers, four compatibility actions, the MCP interaction adapter, and the independently runnable Host. Milestone B adds the domain-neutral observation and semantic-resolution boundaries plus the deterministic, audited Goal-to-SelectedAction path. Milestone C adds deterministic Reasoning and a bounded, deny-by-default autonomous Goal loop that revalidates every step through the common authority path. Milestone D adds opt-in deterministic Effect Verification and tenant-scoped replay-capable Decision evidence without treating executor success as semantic proof or stored evidence as execution authority. PR-19 adds the first disabled-by-default structured-output provider experiment and complete offline replay measures while retaining the deterministic rule provider as the default; adoption remains deferred pending representative operator-controlled evidence. ASL-OT-01 adds isolated offline source registration and deterministic fragment location for the ASL 3.10 A-E corpus without adding ontology semantics or runtime dependencies.

- `src/LimboDancer/` — active production projects and conformance tests;
- `src/Legacy/` — isolated `LimboDancer.MCP.*` implementation retained temporarily for behavioral reference; and
- `src/docs/` — current specifications, reviews, planning documents, and legacy documentation awaiting retirement or migration.

New production code must not depend on `src/Legacy/`. Architecture tests enforce the approved project graph and legacy boundary.

See the [`src/` overview](../) for the current and target source layouts.

## Legacy documentation

The following documents describe the former `.NET 9`, Azure-first, MCP-centered implementation or its original build sequence. They are **not authoritative for new implementation work**:

- [LimboDancer.MCP — Architecture](<./LimboDancer.MCP — Architecture.md>)
- [LimboDancer.MCP — Design Map](<./LimboDancer.MCP — Design Map.md>)
- [LimboDancer.MCP — Roadmap](<./LimboDancer.MCP — Roadmap.md>)
- [Implementation Prototype Plan](<./Implementation Prototype Plan.md>)
- [Configuration Guide](./CONFIGURATION.md)
- [Vector Search Configuration](./VECTOR_SEARCH.md)
- [Tenancy Conventions](./tenancy.md)
- [Knowledge Graphs, Ontologies, and the LimboDancer.MCP Platform](<./Knowledge Graphs, Ontologies, and the LimboDancer.MCP Platform.md>)
- [LimboDancer Ontology Design and Implementation](<./LimboDancer Ontology Design and Implementation.md>)
- [LimboDancer Ontology Reference](<./LimboDancer Ontology Reference.md>)
- [Ontology and Agentic AI in LimboDancer.MCP](<./Ontology and Agentic AI in LimboDancer.MCP.md>)
- [Ontology Generator](<./Ontology Generator.md>)

These documents may contain useful behavior, constraints, and design evidence. Any retained requirement must be reconciled with the current Plane Runtime Specification before it is implemented.
