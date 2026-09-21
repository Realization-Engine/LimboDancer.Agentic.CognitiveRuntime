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

## Architecture analyses and runtime models

| Document | Role |
| --- | --- |
| [Plane Architecture Analysis](<./LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis.md>) | Recasts the system as six logical planes. |
| [Plane Architecture Codebase Validation](<./LimboDancer.Agentic.CognitiveRuntime Plane Architecture Codebase Validation.md>) | Tests the plane model against the legacy implementation and identifies gaps. |
| [Decision Plane Architecture](<./Decision Plane Architecture.md>) | Defines bounded selection, abstention, escalation, and the relationship between decision and execution authority. |
| [Runtime Orchestration Model](<./LimboDancer.Agentic.CognitiveRuntime Runtime Orchestration Model.md>) | Defines how goals and directed requests progress across runtime authority boundaries. |

## Domain integration and knowledge modeling

| Document | Role |
| --- | --- |
| [Domain Integration Model](<./LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md>) | Defines the boundary, dependency direction, composition model, and interface timing for separately implemented domain packages. |
| [Domain Knowledge Modeling Requirements](<./LimboDancer.Agentic.CognitiveRuntime Domain Knowledge Modeling Requirements.md>) | Supporting guidance for authoritative knowledge, semantic evidence, and changing domain state. |
| [ASL reference-domain requirements](<../../docs/ASL/legacy-limbodancer-mcp-system-design.md>) | Concrete requirements and acceptance scenarios the reusable architecture must ultimately support. |
| [ASL documentation index](../../docs/ASL/) | Index of ASL domain research, schemas, prototypes, and historical material. |

Advanced Squad Leader is the first reference domain and architectural fitness test. ASL-specific concepts belong in a separate domain package and must integrate through domain-neutral runtime contracts.

## Current source status

The new production solution has not yet been generated. The present source tree contains:

- `src/Legacy/` — isolated `LimboDancer.MCP.*` implementation retained temporarily for behavioral reference;
- `src/docs/` — current specifications and planning documents, plus legacy documentation awaiting retirement or migration.

The planned `src/LimboDancer/` project structure is a target described by the implementation plan. New production code must not depend on `src/Legacy/`.

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
