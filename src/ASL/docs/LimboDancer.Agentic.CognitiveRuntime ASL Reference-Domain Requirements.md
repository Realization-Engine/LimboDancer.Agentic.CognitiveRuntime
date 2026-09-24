# LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements

**Status:** Current

**Origin:** Extracted from the pre-cognitive-runtime repository README, formerly titled `LimboDancer.MCP System Design`. The MCP-era architecture is preserved separately in [`src/_Legacy/legacy-limbodancer-mcp-system-design.md`](<../../_Legacy/legacy-limbodancer-mcp-system-design.md>) and is not current guidance.

**Capability authority:** Normative for the ASL reference-domain capabilities that LimboDancer must ultimately support.

**Architecture authority:** Non-normative for runtime structure, technology selection, project layout, implementation sequence, and deployment topology. The Plane Runtime Specification remains authoritative for runtime execution and authority semantics.

**Transformation authority:** The [ASL Ontology Transformation Specification](<LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) governs current rulebook-to-package authoring, validation, publication, and first-slice implementation.

## 1. Original product intent

LimboDancer was created to turn complex authoritative material into operational domain knowledge that an AI system can use against current state.

The originating scenario is Advanced Squad Leader (ASL): interpret a deeply cross-referenced, exception-heavy rule system together with maps, terrain, spatial relationships, units, phases, and changing game state; then produce either an explainable domain conclusion or a safely governed state change.

The new cognitive-runtime architecture changes how that goal is realized. It does not replace the goal.

The architecture is successful only if it can ultimately support this end-to-end outcome:

```text
authoritative rules and reference material
-> validated semantic model
-> current domain observations
-> rule, exception, and spatial resolution
-> evidence-backed DomainConclusion
   or
-> governed AuthorizedAction
-> explanation, verification, and audit evidence
```

ASL is the first demanding reference domain. Its concepts must not be embedded in the runtime kernel, but its requirements provide an architectural fitness test for the reusable runtime.

## 2. Reference-domain requirements

### ASL-RD-001: Authoritative source fidelity

The system must preserve exact published identifiers and sufficient provenance for rules, sections, paragraphs, definitions, examples, tables, charts, boards, hexes, and other authoritative source elements.

Aliases and normalized identifiers may improve discovery, but they must not replace or silently rewrite canonical identity.

### ASL-RD-002: Structured rule semantics

The system must distinguish rules, definitions, examples, conditions, effects, cross-references, tables, phase restrictions, base rules, exceptions, and exceptions to exceptions.

The system must not reduce all authoritative material to undifferentiated text chunks.

### ASL-RD-003: Applicability and precedence

The system must determine which rules and exceptions apply to a concrete situation using relevant entity types, unit characteristics, terrain, location, phase, scenario conditions, current markers, module boundaries, and precedence relationships.

It must retain evidence explaining why a rule or exception was included or excluded.

### ASL-RD-004: Complementary evidence mechanisms

The system must be able to combine semantic retrieval, canonical rule traversal, exception resolution, reference data, current state, and deterministic domain calculations.

No vector index, graph store, model, classifier, or calculation provider is authority merely because it supplied relevant evidence.

### ASL-RD-005: Reference state and changing state

The system must distinguish stable reference state—such as board geometry, printed terrain, elevations, and published rules—from changing state such as unit positions, phase, smoke, rubble, fire, weather, overlays, and scenario modifications.

A conclusion or action must retain the identities and versions of the state observations on which it depended.

### ASL-RD-006: Spatial reasoning

The system must ultimately support registered domain calculations for coordinate resolution, adjacency, distance, hexside relationships, elevation, terrain traversal, line of sight, state-dependent invalidation of precomputed results, and multi-board composition where required.

These capabilities may be implemented behind ordinary runtime ports and executors; this requirement does not mandate a generalized plugin registry.

### ASL-RD-007: Domain adjudication

The system must be able to answer a domain question by producing an evidence-backed `DomainConclusion`, not merely a set of retrieved passages.

A DomainConclusion should identify:

- the question or proposition evaluated;
- the conclusion and its disposition;
- applicable rules and controlling exceptions;
- material observations and calculated facts;
- source, ontology, and state-version references;
- assumptions and unresolved ambiguity;
- an explanation suitable for the caller.

The precise runtime type is deferred until implementation evidence justifies it.

### ASL-RD-008: Conclusions are not execution authority

A DomainConclusion is an interpretation of evidence. It is not a SelectedAction, AuthorizedAction, or permission to mutate state.

The runtime must distinguish:

```text
Can this unit enter that location?  -> DomainConclusion
Move this unit into that location.  -> governed action-authority path
```

### ASL-RD-009: Explanation and traceability

The system must be able to explain the controlling rule, applicable exceptions, material observations, calculated spatial facts, assumptions, and reasons plausible alternatives were rejected.

User-facing explanation and runtime audit are related but distinct outputs.

### ASL-RD-010: Change sensitivity

A conclusion must not silently remain authoritative after a material rule, ontology, board, unit, phase, terrain, weather, or other state dependency changes.

The runtime must be able to invalidate, qualify, or recompute conclusions derived from superseded evidence.

### ASL-RD-011: Indeterminate and abstention outcomes

The system must not fabricate a definitive conclusion when relevant rules, state, coordinates, mappings, or precedence relationships are missing, ambiguous, stale, or conflicting.

It must identify the missing or conflicting evidence and return an explicit indeterminate or abstention outcome.

### ASL-RD-012: Governed state change

An ASL state mutation must use a registered semantic action, current observations, deterministic constraints, Governance, Diagnostics, the Execution Gate, an AuthorizedAction, effect observation, and audit evidence.

### ASL-RD-013: Tenant and package isolation

Rules, ontologies, board data, current state, conclusions, and actions must remain within the established tenant and domain-package boundary.

### ASL-RD-014: Domain portability

The reusable architectural pattern is:

```text
authoritative rules
+ domain ontology
+ reference state
+ changing state
+ deterministic calculations
+ evidence-backed conclusions
+ governed semantic actions
```

ASL instantiates this pattern through rules, units, hexes, terrain, phases, and spatial calculations. Other rule-intensive domains may supply different vocabulary without changing the runtime authority model.

### ASL-RD-015: Separate domain-package integration

The ASL implementation must be delivered through one or more separate domain packages that integrate through approved domain-neutral LimboDancer contracts.

The runtime must not reference ASL assemblies or contain ASL-specific vocabulary. The Host composes the runtime and ASL packages. ASL-specific infrastructure remains behind inward-facing ports, and ASL must reuse existing Observation, semantic-action, constraint, Diagnostic, executor, verification, and audit contracts where they fit.

The first read-only ASL adjudication scenario must drive admission of the minimum domain-neutral primitives and interfaces during the Milestone A review and PR-12/PR-13. The complete integration and timing rules are defined in `src/docs/LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`.

## 3. End-to-end acceptance scenarios

### Scenario A: State-aware rule adjudication

Given a known unit, location, phase, scenario state, and authoritative rule package, when a caller asks whether the unit may enter an occupied building, LimboDancer must resolve the relevant entities, rules, conditions, and exceptions; incorporate current state; and return a DomainConclusion with controlling evidence and any remaining ambiguity without mutating state.

### Scenario B: Spatial adjudication

Given two board locations, reference terrain and elevation, and current smoke, rubble, weather, or other modifiers, when a caller asks whether line of sight exists, LimboDancer must resolve the coordinates, observe current state, perform the registered spatial calculation, apply relevant rules and exceptions, and return the conclusion with supporting or blocking evidence.

### Scenario C: Governed state mutation

Given a semantically valid movement possibility, when a caller requests that a unit move, LimboDancer must bind a registered action, re-observe material state, evaluate semantic and Governance constraints, run required Diagnostics, pass the Execution Gate, execute only an AuthorizedAction, observe the result, verify the expected effect, and record audit evidence.

### Scenario D: Stale or incomplete evidence

Given missing, conflicting, or superseded rules or state, when a caller requests a ruling or mutation, LimboDancer must not invent certainty. It must return an indeterminate conclusion, re-observe, abstain, or block execution as appropriate and identify the evidence problem.

## 4. Traceability to LimboDancer.Agentic.CognitiveRuntime

The reference-domain capabilities map to the new architecture as follows:

| ASL capability | Future architectural home |
|---|---|
| Rule vocabulary, canonical IDs, entities, relations, conditions, and exceptions | Semantic Plane |
| Rule text, graph relationships, embeddings, maps, board state, and provenance | State Plane through tenant-safe ports |
| Rule interpretation and goal decomposition | Reasoning Plane |
| Finite ASL action candidates and deterministic applicability | Semantic Plane |
| Choice among already permitted ASL actions | Decision Plane for autonomous goals only |
| LOS calculations, state queries, and authorized state changes | Execution Plane through registered executors |
| Tenant, policy, risk, and permission checks | Governance |
| Invariant checks, mapping validation, and effect verification | Diagnostics |
| MCP exposure | Interaction adapter, not ASL or runtime authority |

The intended integration path is:

```text
ASL rules, maps, and reference material
-> validated ASL ontology and state representations
-> observations, semantic resolution, and calculations
   -> DomainConclusion, explanation, and evidence
   or
   -> registered semantic action
   -> directed or autonomous authority path
   -> deterministic constraints and diagnostics
   -> Governance and Execution Gate
   -> authorized ASL executor
   -> observed effects and audit evidence
```

An LLM, embedding model, extraction model, classifier, or search provider may propose interpretations or supply evidence. It may not authorize or directly execute an ASL action.

Porting should be selective. Useful behavior from the legacy implementation should be cleaned, hardened, and retested against the new contracts; obsolete MCP-centric structure should not be reproduced.

## 5. Capability horizon

These requirements do not change the lean initial implementation sequence. They establish the capability horizon that the runtime must eventually reach after its authority substrate is proven.

```text
runtime authority substrate
-> tenant-safe evidence access
-> ASL ontology and authoritative-source package
-> rule ingestion, validation, and publication
-> board and reference-state provider
-> dynamic game-state observations
-> rule, exception, and spatial resolution
-> DomainConclusion and explanation
-> governed ASL mutations
-> end-to-end ASL conformance scenarios
```

Exact projects, contracts, providers, and PRs should be introduced only when the corresponding reference scenario is ready to drive them.
