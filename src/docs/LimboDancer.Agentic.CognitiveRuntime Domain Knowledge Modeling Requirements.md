# LimboDancer.Agentic.CognitiveRuntime Domain Knowledge Modeling Requirements

**Status:** Supporting, non-normative architecture guidance

**Authority:** Subordinate to the Plane Runtime Specification and Plane Runtime Design

**Scope:** Domain knowledge, semantic evidence, and state representations

## 1. Purpose

LimboDancer must support domains whose authoritative knowledge is distributed across structured rules, prose, tables, examples, cross-references, reference data, and changing world state.

This document preserves the still-valid domain-modeling requirements extracted from the retired root-level `docs/limbodancer-system-design.md` and the retired `Documentation/Ontology and Agentic AI.md`. The original historical ASL-centered design is retained as `docs/ASL/legacy-limbodancer-mcp-system-design.md`. This document does not preserve the retired material's MCP-centric product boundary, legacy project layout, plugin architecture, deployment topology, ReAct/tool-execution assumptions, or speculative implementation schedules.

The runtime authority model remains defined by the normative specification:

```text
Reasoning proposes.
Semantics constrains.
Governance permits.
Decision selects.
Execution acts.
State remembers.
Orchestration coordinates.
Diagnostics assure.
```

Domain knowledge supplies meaning and evidence within that model. It never grants execution authority.

## 2. Plane ownership

| Concern | Primary owner | Supporting concerns |
|---|---|---|
| Domain vocabulary, types, relations, aliases, applicability | Semantic | State persists definitions and observations |
| Canonical source identifiers and source structure | Semantic | State stores source and provenance records |
| Rule conditions, exceptions, and precedence | Semantic | Diagnostics validates integrity; Governance may impose policy |
| Graph-addressable relationships | State | Semantic interprets them |
| Similarity-oriented retrieval | State | Semantic and Reasoning consume results as evidence |
| Reference and world state | State | Semantic determines meaning and applicability |
| Domain-specific calculations | Execution | Semantic defines their meaning; Diagnostics checks required invariants |
| Ingestion and extraction | Orchestration | Semantic validates artifacts; State persists accepted results |

Persisting semantic data does not transfer semantic ownership to the State Plane. Retrieving relevant data does not make a graph store, vector index, model, or classifier an authority.

## 3. Canonical identity and provenance

Domain artifacts must preserve authoritative identifiers exactly as published when those identifiers carry meaning. Examples include rule numbers, regulation citations, specification clauses, document sections, board identifiers, and spatial coordinates.

A derived artifact should retain enough provenance to answer:

- which source and source version produced it;
- where in that source the claim originated;
- which ingestion or extraction process created it;
- whether it is authoritative, curated, inferred, or generated;
- which tenant and domain package own it;
- which ontology or schema version interpreted it.

Aliases may improve discovery, but they must not replace or silently rewrite canonical identity.

## 4. Structured rule knowledge

The Semantic Plane should be able to represent, when the domain requires them:

- hierarchical rules and clauses;
- exact cross-references;
- definitions and controlled terminology;
- conditions and applicability;
- exceptions, including nested exceptions;
- explicit precedence between base rules and exceptions;
- examples linked to the rule they illustrate;
- tables and other matrix-shaped rules;
- temporal or phase-specific applicability;
- module, jurisdiction, or package boundaries.

An exception must remain distinguishable from the rule it modifies. Flattening both into unstructured text destroys information needed for deterministic constraint evaluation and explanation.

## 5. Graph and vector evidence

Graph traversal and vector retrieval are complementary observation mechanisms.

Graph-oriented state is appropriate for precise relationships such as:

- hierarchy and containment;
- references and dependencies;
- typed entity relations;
- exception and precedence links;
- state transitions and entity identity.

Vector retrieval is appropriate for similarity-oriented discovery such as:

- semantically related rules or passages;
- examples expressed with different vocabulary;
- candidate source material for later structured resolution;
- hybrid retrieval constrained by tenant and ontology metadata.

Neither mechanism decides what action may execute. Retrieval results are evidence that must remain tenant-scoped, provenance-aware, and subject to semantic resolution, deterministic constraints, Governance, Diagnostics, and the Execution Gate where applicable.

## 6. Reference state and changing world state

The model must distinguish stable reference data from changing world state.

Examples of reference state include published rules, maps, product catalogs, regulatory taxonomies, schemas, and precomputed relationships. Examples of changing state include active sessions, resource conditions, entity locations, terrain modifications, workflow status, and current observations.

When a conclusion depends on both, the runtime should retain the identities and versions of the observations used. A precomputed result must identify the assumptions under which it remains valid, and changing state must not silently reuse a stale result.

## 7. Validation requirements

Domain ingestion and runtime resolution should validate the invariants relevant to the domain, including:

- canonical identifier format and uniqueness;
- cross-reference resolution;
- hierarchy consistency;
- exception precedence integrity;
- allowed terminology and aliases;
- schema or shape conformance;
- package or version compatibility;
- circular dependency detection where cycles are invalid;
- tenant isolation;
- ontology-bound identifier resolution;
- provenance completeness for consequential evidence.

Unknown semantic identifiers must fail closed on correctness- or security-sensitive paths. They must not silently fall through to physical property names, executor names, or broader searches.

## 8. Domain-specific capabilities

Spatial reasoning, legal citation analysis, regulatory applicability, game-rule calculations, and similar capabilities are domain services—not extensions of the core authority model.

A domain-specific capability may:

- acquire or derive Observations;
- perform deterministic calculations;
- evaluate a registered semantic constraint;
- execute an AuthorizedAction through a bound executor;
- provide evidence for effect verification.

It may not create an unregistered action, bypass Governance or Diagnostics, or convert model output directly into execution authority.

## 9. Ingestion and generation

Document extraction and ontology generation remain valuable future capabilities, but they are outside the first directed-runtime implementation sequence.

When introduced, their output should pass through an explicit lifecycle:

```text
source material
-> extraction
-> candidate semantic artifacts
-> validation and curation
-> versioned publication
-> tenant-safe persistence
-> runtime resolution
```

Probabilistic extraction may propose artifacts. Only validated, published artifacts may participate as trusted runtime semantics.

## 10. Semantic artifact distinctions

The runtime should keep several related artifacts conceptually distinct:

- an **ontology** defines domain concepts, properties, relations, constraints, and semantic action vocabulary;
- a **taxonomy** organizes concepts into a hierarchy but does not, by itself, define the full domain semantics;
- a **schema** defines structural expectations for stored data or exchanged payloads;
- a **knowledge graph** records instance facts and relationships interpreted through semantic definitions;
- a **validation shape or rule set** checks selected structural or semantic invariants.

One physical artifact may serve more than one of these roles, but the roles must not be conflated. A payload satisfying a schema is not necessarily semantically valid, and a fact existing in a graph does not make that fact current, trusted, or sufficient to authorize an action.

## 11. Representation strategies

Representation mechanisms are complementary choices rather than competing product identities:

- **RDF and OWL** can provide global identifiers, formal semantics, and bounded inference where their complexity is justified;
- **property graphs** can provide efficient traversal and developer-friendly relationship storage;
- **JSON-LD** can carry linked semantic identifiers in web and action payloads;
- **JSON Schema, Protobuf, and equivalent contract formats** can provide strong structural validation at system boundaries;
- **SHACL or equivalent validation mechanisms** can express selected graph and semantic constraints.

The runtime should choose the least complex representation that preserves the meaning, validation, interoperability, and evidence requirements of the concrete use case. No representation engine is execution authority, and inference results used on consequential paths remain subject to provenance, confidence, semantic resolution, deterministic constraints, Governance, Diagnostics, and the Execution Gate.

## 12. Modeling and evolution discipline

Ontology and domain models should grow from demonstrated runtime needs rather than speculative completeness.

The initial model for a domain should focus on the entities, relations, actions, constraints, and observations required by concrete use cases. New vocabulary should be added when it enables a required action, invariant, explanation, interoperability boundary, or evidence query.

Published semantic artifacts should be treated with the same discipline as code:

- assign stable identities and explicit versions;
- review changes and record provenance;
- test representative resolutions, constraints, and mappings;
- define compatibility expectations and migration behavior;
- detect ambiguous aliases and breaking vocabulary changes;
- keep inference profiles bounded and operationally observable;
- retire superseded artifacts without silently changing the meaning of recorded evidence.

Full ontology reasoning, cross-ontology mapping, multi-agent vocabularies, and learned skill graphs remain optional future capabilities. They should be introduced only when concrete requirements justify their operational and governance costs.

## 13. Deferred implementation choices

These requirements intentionally do not prescribe:

- a graph database product;
- a vector database product;
- RDF, JSON-LD, SHACL, or another representation as the sole canonical format;
- a generalized plugin registry;
- a domain-specific source project for every domain;
- a particular ingestion model or AI provider;
- deployment topology.

Those choices should follow concrete implementation evidence and remain replaceable behind inward-facing runtime ports.
