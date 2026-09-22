# LimboDancer.Agentic.CognitiveRuntime Domain Integration Model

**Status:** Current design guidance

**Authority:** Subordinate to the Plane Runtime Specification and Plane Runtime Design

**Reference domain:** `docs/ASL/legacy-limbodancer-mcp-system-design.md`

## 1. Purpose

LimboDancer must support independently implemented domain packages without allowing a domain implementation to become the runtime architecture.

This document defines:

- the dependency boundary between the runtime and domain packages;
- how the Host composes them;
- which concepts require domain-neutral primitives;
- when those primitives and interfaces should be defined;
- how the first ASL scenario will drive the contracts;
- which generalized extension mechanisms remain deferred.

It does not define final C# signatures. The [Milestone A Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone A Conformance Review.md>) selected occupied-building entry eligibility as the first ASL scenario and admitted the minimum PR-12/PR-13 contract boundary. Concrete signatures remain subject to those limits.

## 2. Core integration principle

The runtime owns reusable cognition, authority, lifecycle, and evidence semantics. A domain package owns the vocabulary and behavior unique to its domain.

```text
LimboDancer runtime
├── authority types and transitions
├── Goal and Observation lifecycle
├── semantic-action contracts
├── Governance and Diagnostics
├── Decision and Orchestration
├── Execution Gate
├── audit and verification
└── domain-neutral integration ports

Domain package
├── domain identity and semantic package
├── ontology and canonical references
├── entity and vocabulary resolution
├── reference-state providers
├── domain calculations
├── semantic actions and constraints
├── domain diagnostics
└── conclusion semantics and explanation
```

The runtime must not contain ASL units, hexes, terrain, phases, or line-of-sight rules. The ASL package must not redefine runtime authority.

## 3. Dependency direction

The intended dependency shape is:

```text
                    Host
                  /      \
                 v        v
            Runtime    Domain package
                 \        /
                  v      v
                 Abstractions
```

Permanent dependency rules:

1. `LimboDancer.Runtime` must not reference ASL or another concrete domain package.
2. A domain package may depend on approved `LimboDancer.Abstractions` contracts.
3. Domain-specific infrastructure must remain behind inward-facing ports.
4. The Host is the composition root that selects and registers domain packages.
5. Removing a domain package must not break the runtime build or core conformance tests.
6. Adding a domain must not require changing core authority transitions.
7. Protocol adapters must not become the domain-integration boundary.

Illustrative project names such as `LimboDancer.Domains.Asl` and `LimboDancer.Domains.Asl.Infrastructure` are not yet prescribed.

## 4. Composition and registration

The first domain package should use ordinary compile-time composition and dependency injection.

An ASL package may eventually expose a registration extension such as:

```text
AddLimboDancerAsl(...)
```

That registration may contribute implementations of approved runtime ports, ActionDescriptors, action bindings, constraint evaluators, diagnostic checks, observation providers, executors, and conclusion services.

This does not require a generalized `IDomainPlugin`, dynamic assembly discovery, hot loading, activation lifecycle, or plugin registry.

A generalized module abstraction should be extracted only after at least two real domain implementations demonstrate common registration or lifecycle requirements that ordinary Host composition cannot express cleanly.

## 5. Domain-neutral primitives

The first ASL adjudication scenario is expected to require concepts in the following categories:

- domain identity;
- domain-package identity and version;
- canonical source reference;
- semantic identifier;
- evidence reference;
- domain question;
- conclusion disposition;
- DomainConclusion;
- applicability or explanation evidence.

Candidate names include:

```text
DomainId
DomainPackageRef
CanonicalReference
SemanticIdentifier
EvidenceReference
DomainQuestion
ConclusionDisposition
DomainConclusion
```

The Milestone A review approved these names for PR-12 because the selected scenario exercises each one. Exact signatures remain implementation decisions. Each type must remain free of ASL-specific vocabulary and infrastructure SDK types.

Domain and package identity must be carried with tenant context where required. A conclusion must identify the semantic package and material evidence versions used to interpret the domain state.

## 6. Reuse existing runtime ports

A separate domain library does not imply a parallel domain runtime.

Domain implementations should reuse existing LimboDancer contracts where they fit:

| Domain contribution | Preferred runtime integration |
|---|---|
| Read reference or current state | Observation producer or inward-facing State port |
| Resolve semantic actions | `IActionResolver` |
| Evaluate deterministic applicability | `IActionConstraintEvaluator` or constraint pipeline |
| Perform a read-only calculation | Registered semantic action and executor |
| Perform a mutation | Registered semantic action and executor |
| Declare execution semantics | Trusted ActionDescriptor |
| Check invariants | Diagnostic check and profile |
| Verify a mutation | Effect verification |
| Expose a protocol operation | Interaction adapter binding |

A new domain-specific extension interface should be introduced only when existing ports cannot express the demonstrated requirement without mixing plane responsibilities.

## 7. Likely extension boundaries

The ASL reference scenarios may demonstrate a need for small domain-neutral boundaries in these areas:

- resolving a versioned domain package;
- resolving canonical domain entities and detecting ambiguity;
- assembling an evidence-backed DomainConclusion from resolved semantics and observations.

Possible interface concepts include a domain-package resolver, domain-entity resolver, and domain-conclusion resolver. Their names, responsibilities, and signatures are intentionally deferred.

A conclusion resolver must not become a service that retrieves arbitrary state, invents semantics, authorizes actions, and executes mutations. Its context must respect the established Semantic, State, Reasoning, Governance, and Execution boundaries.

## 8. Runtime integration versus authoring

Domain authoring and runtime use are separate lifecycles.

```text
Authoring lifecycle
source material
-> extraction
-> candidate semantic artifacts
-> validation and curation
-> versioned domain package
-> publication
```

```text
Runtime lifecycle
resolve published package
-> observe state
-> resolve semantics
-> perform registered calculations
-> conclude or act
```

The first runtime interfaces should consume a validated, published domain package. They should not assume that document extraction or ontology generation occurs during a live invocation.

Authoring interfaces such as extraction, validation, and publication ports should be defined only when the ingestion implementation slice begins.

## 9. Contract timing

### Before Milestone A

Implement only the domain-neutral authority and directed-runtime substrate required by PR-01 through PR-11. Do not add a domain framework.

### Milestone A review

Select the first ASL read-only adjudication scenario and document:

- inputs and domain identities;
- required observations;
- authoritative source references;
- semantic and exception-resolution steps;
- deterministic calculations;
- DomainConclusion fields;
- ambiguity and indeterminate cases;
- explanation and audit expectations.

Use that scenario to approve the smallest domain-neutral vocabulary. Reject interfaces that the scenario does not exercise.

This checkpoint is complete. The selected scenario, approved vocabulary, admitted extension responsibilities, deferred interfaces, and required conformance tests are recorded in the Milestone A Conformance Review.

### PR-12: Autonomous runtime contracts

Introduce only lifecycle-level primitives proven necessary by the scenario, potentially including DomainQuestion, DomainConclusion, conclusion disposition, domain/package identity, and GoalResult support.

### PR-13: Observation and semantic resolution

Introduce the minimum runtime-facing extension points proven necessary for domain-package resolution, entity resolution, evidence references, conclusion resolution, and integration with Observation and semantic-action resolution.

### PR-14 through PR-18

Exercise the approved contracts through deterministic Decision, Reasoning, Orchestration, verification, audit, abstention, and indeterminate outcomes.

### Milestone F

Implement the separate ASL package in small scenario-driven slices. Begin with read-only adjudication; add governed state mutation only after conclusion integrity is proven.

## 10. Interface admission rule

A domain-neutral interface should be added only when all of the following are true:

1. a named reference-domain scenario requires it;
2. an existing runtime port cannot express the requirement cleanly;
3. its owning plane and authority are unambiguous;
4. its inputs and outputs use domain-neutral vocabulary;
5. tenant, domain-package, provenance, version, and cancellation requirements are defined where applicable;
6. infrastructure and protocol SDK types do not leak through it;
7. failure, ambiguity, staleness, and abstention behavior are defined;
8. conformance tests can prove the boundary;
9. it does not grant execution authority or bypass the Execution Gate.

The sequencing rule is:

> Define a domain-neutral interface immediately before the first concrete ASL scenario needs it, but after the underlying runtime authority primitive it depends on has been implemented and tested.

## 11. Architecture and conformance tests

Before domain-integration contracts are considered stable, tests should prove:

- Runtime has no reference to the ASL package;
- the ASL package depends only on allowed LimboDancer contracts;
- ASL can register semantic actions, constraints, diagnostics, observations, and executors without modifying Runtime;
- canonical identifiers survive round trips unchanged;
- observations carry tenant, package, provenance, and version information where required;
- DomainConclusion cannot be supplied to an executor as authorization;
- a later mutation requires independent action binding and authorization;
- a minimal fake domain can implement the same approved contracts;
- domain-specific infrastructure SDK types remain contained;
- removing ASL leaves the runtime build and conformance suite intact.

The fake domain is a contract test fixture, not a second production domain or justification for a generalized plugin system.

## 12. Deferred decisions

The following remain deferred until implementation evidence exists:

- exact primitive and interface names;
- generic versus non-generic conclusion payloads;
- domain-package discovery beyond Host registration;
- dynamic loading or unloading;
- authoring and publication interfaces;
- package persistence format;
- cross-domain composition;
- cross-domain action resolution;
- shared vocabulary mapping between independently versioned domains;
- whether a small module abstraction becomes useful after multiple real domains exist.

Deferral must not weaken the permanent dependency direction or the ASL reference-domain requirements.
