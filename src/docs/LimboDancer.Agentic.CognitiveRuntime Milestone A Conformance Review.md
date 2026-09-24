# LimboDancer.Agentic.CognitiveRuntime Milestone A Conformance Review

**Status:** Approved

**Review date:** 2026-09-22

**Reviewed branch:** `decision-plane`

**Baseline through:** PR-11, New runtime host

**Authority:** Architecture and contract-admission checkpoint subordinate to the Plane Runtime Specification and Plane Runtime Design

## 1. Review decision

Milestone A is conformant. The directed runtime has an independently buildable and hostable authority path in which external requests resolve registered actions, pass through Diagnostics and the Execution Gate, and reach executors only as `AuthorizedAction` values.

Autonomous implementation may proceed to PR-12 under the contract limits in this review. This approval does not authorize a generalized domain framework, an ASL implementation inside the runtime kernel, dynamic plugin loading, or autonomous execution before its later milestones.

The first ASL reference slice is **Scenario A: occupied-building entry eligibility**, a read-only specialization of the reference-domain State-aware rule-adjudication scenario.

## 2. Directed-runtime conformance evidence

| Milestone A criterion | Decision | Evidence |
| --- | --- | --- |
| New solution builds independently | Pass | `LimboDancer.sln`; `LimboDancer CI` restores, builds with warnings as errors, and tests only `src/LimboDancer/`. |
| No new production project references legacy production | Pass | `ProductionDependencyTests.SolutionProjectsDoNotReferenceLegacyProjects` and the approved dependency-graph test. |
| Four MCP compatibility actions execute through the new gate | Pass | `McpInteractionAdapterTests.AllFourCompatibilityToolsInvokeThroughAdapter` plus the gate/audit assertions in `KnownToolExecutesThroughRuntimeGateAndAudit`. |
| Relational, graph, and vector behavior is tenant-isolated | Pass | Integration tests under `State/InMemoryHistoryStoreTests`, `State/InMemoryGraphStoreTests`, and `State/InMemoryMemorySearchTests`; MCP tenant mismatch is denied. |
| Unknown required semantic mappings fail closed | Pass | `HardDiagnosticChecksTests.UnknownRequiredSemanticMappingFailsWithEvidence`, ontology unknown-term tests, and graph-query unresolved-mapping tests. |
| Hard Diagnostics fail closed | Pass | `DiagnosticPolicyTests`, `HardDiagnosticChecksTests`, and Execution Gate diagnostic-block tests. |
| Gate decisions are audited | Pass | Execution Gate tests assert authorized and denied events; MCP integration asserts `GateAuthorized` and executor completion. |
| Executors receive only `AuthorizedAction` | Pass | `IActionExecutor` and every concrete executor accept `AuthorizedAction`; `ExecutionGateTests.ExecutorContractRequiresAuthorizedAction` enforces the boundary. |
| Legacy caller effects cannot define execution semantics | Pass | Input schemas omit caller effects, registries retain trusted descriptor effects, and history append explicitly rejects an `effects` member. |
| The new Host starts without legacy assemblies | Pass | Host composition/startup tests, structural validation tests, legacy-assembly inspection, and the loopback HTTP conformance test covering liveness, readiness, authentication, tool exposure, and tenant admission. |

### Review observation closed

The initial PR-11 tests proved service composition and hosted-service startup but did not launch the actual HTTP process boundary. The review added `RuntimeHostTests.HostStartsAndAdmitsAuthenticatedTenantIntoMcpInvocation` to prove the executable Host boundary and credential-derived tenant flow end to end.

## 3. Selected ASL scenario

### 3.1 Name

**ASL Scenario A1: Occupied-building entry eligibility**

### 3.2 Question

Given a known unit, a target building location, the current phase, current occupants, scenario modifiers, and an exact published ASL semantic package, determine whether the unit may enter the location now.

The result is a `DomainConclusion`. It must not move the unit, reserve movement points, change occupancy, or create execution authority.

### 3.3 Why this scenario is first

This scenario exercises authoritative rules, entity resolution, current observations, applicability, exception precedence, deterministic facts, explanation, ambiguity, and change sensitivity without first requiring the additional geometry and invalidation surface of line-of-sight adjudication.

Spatial Scenario B remains the next useful pressure test, but it does not justify a general spatial-calculation interface in PR-12 or PR-13.

## 4. Scenario inputs and identities

The acceptance fixture must supply:

- tenant identity;
- `DomainId` for the ASL domain;
- an exact `DomainPackageRef` containing package identity and version;
- a `DomainQuestion` whose semantic kind identifies entry-eligibility adjudication;
- caller references for the moving unit and target location;
- the adjudication time or current-state boundary;
- canonical source references from a curated package, not invented rule numbers or model-generated citations.

Runtime contracts treat domain identifiers as opaque values. They must preserve exact spelling and version and must not parse ASL-specific structure.

## 5. Required observations

The scenario requires versioned, tenant-scoped observations for:

1. the moving unit and the material characteristics used by the ruling;
2. the target location and its stable building/terrain identity;
3. current occupants and the material occupancy relationship;
4. the current phase or timing context;
5. scenario, overlay, marker, or other modifiers declared material by the resolved rules; and
6. the resolved semantic-package and ontology versions.

Stable reference facts and changing game state must remain distinguishable. Each material changing-state observation must carry a revision, ETag, or equivalent version when its source provides one.

## 6. Semantic and exception resolution

The scenario processing order is:

1. resolve the exact domain package;
2. resolve the unit and location references without silently choosing among ambiguous matches;
3. acquire tenant-scoped current observations;
4. resolve the base entry, occupancy, timing, and scenario-applicability semantics;
5. traverse applicable exceptions and precedence relationships;
6. derive bounded deterministic facts required by those rules;
7. distinguish included, excluded, and controlling rules;
8. construct an evidence-backed conclusion; and
9. return a non-definitive disposition when material evidence or precedence is unresolved.

Retrieval rank, vector similarity, graph reachability, or model confidence may help locate evidence. None determines authority or rule precedence by itself.

## 7. Deterministic calculations

The first scenario admits only calculations actually required to derive entry eligibility, such as classification or aggregation of observed occupancy and comparison with published limits or conditions.

These calculations remain inside the ASL package or behind an already approved runtime execution/observation boundary. This review does not admit:

- a generalized calculation-plugin interface;
- a spatial or line-of-sight runtime interface;
- direct access from a conclusion resolver to arbitrary infrastructure; or
- model-generated deterministic facts.

If the ASL implementation later exposes a read-only calculation as a semantic action, it must use a registered descriptor and executor. That fact does not convert its resulting evidence into execution authority.

## 8. Conclusion requirements

The resulting `DomainConclusion` must preserve or reference:

- conclusion identity and the evaluated question identity;
- tenant and exact domain-package identity;
- a `ConclusionDisposition`;
- the semantic conclusion payload;
- applicable rules and controlling exceptions;
- material observations and deterministic calculation evidence;
- source, ontology, and state versions;
- assumptions and qualifications;
- missing, ambiguous, stale, or conflicting evidence;
- stable reason codes; and
- explanation text or explanation provenance suitable for the caller.

Approved dispositions are:

| Disposition | Meaning |
| --- | --- |
| `Definitive` | Material evidence is complete and supports one answer under the selected package. |
| `Qualified` | An answer is supported, but explicit non-material assumptions or limitations remain. |
| `Indeterminate` | Material evidence, mapping, or precedence is missing, ambiguous, stale, or conflicting. |
| `Abstained` | The runtime deliberately declines because the question is unsupported or cannot be evaluated safely. |

Re-observation is an orchestration response to stale or missing evidence, not a conclusion disposition.

## 9. Indeterminate and ambiguity cases

A definitive answer is prohibited when any material condition below remains unresolved:

- the unit or location reference has zero or multiple valid resolutions;
- the package or canonical source version is unavailable;
- phase, occupants, scenario modifiers, or another required observation is absent;
- a material observation version changed during adjudication;
- applicable rules or exceptions conflict without deterministic precedence;
- a required semantic identifier or ontology mapping is unknown; or
- evidence belongs to another tenant or package boundary.

The conclusion must identify the problem and the evidence needed to continue. It must not silently substitute a broader search, physical storage name, newer package, or likely entity.

## 10. Explanation and audit

The caller explanation must identify the controlling rule and exception path, material facts, assumptions, and why plausible alternatives were excluded.

Runtime audit must be sufficient to reconstruct:

- question admission and tenant/package boundary;
- package and entity-resolution outcomes;
- material observation and evidence references;
- semantic/exception resolution outcome;
- conclusion disposition and reason codes; and
- whether the runtime abstained or requested re-observation.

Explanation may contain domain-facing prose. Audit remains structured runtime evidence. Neither is an `AuthorizedAction`.

## 11. Contracts admitted for PR-12

The scenario approves the following domain-neutral vocabulary in `LimboDancer.Abstractions`:

| Contract | Minimum purpose |
| --- | --- |
| `DomainId` | Opaque stable identity of a domain. |
| `DomainPackageRef` | Exact domain, package, and version identity used for interpretation. |
| `CanonicalReference` | Exact reference to a published source or source element. |
| `SemanticIdentifier` | Opaque canonical identity for a question kind, entity kind, rule meaning, or other published semantic concept. |
| `EvidenceReference` | Versioned/provenanced reference to material observation, source, or calculation evidence without duplicating its payload. |
| `DomainQuestion` | Tenant-scoped, package-bound question with semantic kind and bounded domain parameters. |
| `ConclusionDisposition` | The four outcomes approved in section 8. |
| `DomainConclusion` | Terminal evidence-backed interpretation that carries no action authority. |

PR-12 may also introduce the already planned Goal, GoalResult, lifecycle, candidate, constraint, and decision contracts. DomainConclusion support must be a terminal Goal result variant, not an action candidate or implicit success authorization.

Exact constructors and collection types remain implementation decisions, but they must enforce non-empty identity, tenant/package consistency, immutable evidence, and disposition invariants.

## 12. Extension points admitted for PR-13

The scenario justifies these minimum runtime-facing boundaries:

1. **Domain package resolution** — resolve an exact published package or fail explicitly; never silently upgrade.
2. **Observation acquisition** — obtain bounded tenant-scoped, versioned observations independently of Reasoning.
3. **Domain entity resolution** — resolve caller references to canonical semantic identities with explicit resolved, unresolved, and ambiguous outcomes.
4. **Domain conclusion resolution** — combine already supplied package semantics, resolved entities, observations, and calculation evidence into a conclusion without retrieving arbitrary state or authorizing actions.

Names and signatures may be refined in PR-13, but responsibilities may not be collapsed into an omniscient domain service.

Existing tenant scope, State ports, ontology resolution, Diagnostics, audit, action descriptors, constraints, and the Execution Gate remain sufficient for their established responsibilities.

## 13. Explicitly deferred

This review does not admit:

- `IDomainPlugin`, dynamic discovery, hot loading, or unloading;
- generalized authoring, extraction, curation, or publication interfaces;
- a generalized rule engine or exception engine in the runtime kernel;
- a generalized deterministic-calculation registry;
- spatial/LOS contracts;
- a domain-conclusion persistence product;
- cross-domain composition or vocabulary mapping;
- ASL-specific types in Abstractions or Runtime; or
- any path from DomainConclusion to executor invocation without an independent action request and Execution Gate decision.

## 14. Required conformance tests for PR-12 and PR-13

The next increments must prove:

- canonical and semantic identifiers round-trip unchanged;
- package versions are exact and cannot silently float;
- conclusion evidence is immutable and tenant/package consistent;
- definitive conclusions cannot be constructed with declared material ambiguity or missing evidence;
- DomainConclusion is not assignable to action-authority contracts and cannot be passed to an executor;
- unknown, ambiguous, conflicting, stale, and cross-tenant evidence produce non-definitive outcomes;
- a minimal fake domain implements the approved interfaces without ASL vocabulary;
- Runtime has no ASL dependency and the fake domain can be removed without breaking core conformance; and
- a later mutation derived from a conclusion independently resolves, gates, authorizes, executes, and verifies current state.

## 15. Next implementation boundary

PR-12 may now implement autonomous lifecycle contracts and the approved conclusion primitives only. PR-13 may implement observation and semantic-resolution boundaries using a minimal fake domain. The actual ASL package remains a later scenario-driven reference-domain slice after the autonomous substrate can preserve the evidence and abstention semantics approved here.
