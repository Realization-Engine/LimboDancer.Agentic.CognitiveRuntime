# LimboDancer source tree

This directory is the transition boundary between the legacy `LimboDancer.MCP` implementation and the new `LimboDancer.Agentic.CognitiveRuntime` implementation.

## Current layout

| Path | Status | Purpose |
| --- | --- | --- |
| [`LimboDancer/`](./LimboDancer/) | Active production source | New `.NET 10` runtime solution, production projects, and tests. |
| [`docs/`](./docs/) | Active documentation | Current runtime specifications, design guidance, implementation planning, and retained legacy reference documents. |
| [`Legacy/`](./Legacy/) | Temporary legacy source | Previous `LimboDancer.MCP.*` projects retained only as behavioral and migration references. |

The new production solution under `src/LimboDancer/` now contains the action-authority, diagnostic, execution-gate, audit, and tenant-safe State foundations; new-runtime executors for the four initial directed actions; a transport-neutral MCP interaction adapter; an independently runnable runtime Host; the autonomous Goal, observation, constraint, Decision, and evidence-backed domain-conclusion contracts; exact package, bounded observation, explicit entity-resolution, conclusion-resolution, registered-action, and semantic-precondition boundaries; a deterministic, audited Decision Plane; a deterministic proposal-only Reasoning boundary; and a bounded Goal orchestration loop that revalidates every autonomous step through resolution, constraints, Decision, diagnostics, and the common Execution Gate. Autonomous admission remains deny-by-default until a trusted adapter supplies the authenticated principal and runtime budget.

## Active production layout

```text
src/LimboDancer/
  LimboDancer.sln
  Directory.Build.props
  Directory.Packages.props
  LimboDancer.Abstractions/
  LimboDancer.Runtime/
  LimboDancer.Infrastructure/
  LimboDancer.Adapters.Mcp/
  LimboDancer.Host/
  LimboDancer.AppHost/
  tests/
```

All new projects target `net10.0`. The five runtime production projects retain the approved dependency graph. `LimboDancer.AppHost` is an outer Aspire application-orchestration project that references `LimboDancer.Host` only; it does not participate in runtime action authority. Architecture tests enforce these boundaries and forbid references from new projects into `src/Legacy/` or the `LimboDancer.MCP.*` project family.

The initial AppHost deliberately orchestrates only `LimboDancer.Host`. The State ports are provider-neutral, and the first Infrastructure implementations are deterministic in-memory reference providers used to prove tenant isolation and fail-closed semantic behavior. PostgreSQL, graph, vector, ontology persistence, and their Aspire resources are admitted only when a concrete provider is selected and configured. Aspire operational telemetry does not replace LimboDancer diagnostics, Governance decisions, the Execution Gate, or authoritative runtime audit evidence.

`LimboDancer.Host` is the production composition root for the directed runtime. It validates configuration and the descriptor/binding/executor graph at startup, derives tenant and principal context from authenticated API-key credentials, hosts the MCP adapter over HTTP, exposes observational `/health/live` and `/health/ready` probes, and emits host-level activity and metric signals. No API key is configured by default, so protected MCP tool endpoints fail closed until credentials are supplied under `LimboDancer:ApiKeys`. Discovery and legacy protocol negotiation remain anonymous; tool listing and execution require authentication.

The initial host intentionally uses the admitted in-memory reference providers. Readiness validates runtime structure only and never performs schema migration or state mutation. A descriptor that declares preconditions also fails closed unless a concrete constraint evaluator is registered; the composition root does not invent action authority.

The exact implementation sequence and admission criteria are defined in the [Implementation Plan](<./docs/LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md>). The [documentation index](./docs/) identifies the normative specification and its supporting design documents.

Milestones A and B are approved. The runtime now has both a governed directed-execution path and a deterministic autonomous-selection path. Reasoning and Goal orchestration remain the next bounded increments; autonomous selection still creates no execution authority until the existing gate independently authorizes current state.

## Legacy boundary

New production projects must not reference projects or assemblies under `Legacy/`. Required behavior may be inspected, reimplemented, tested, and admitted into the new architecture. The intended end state is deletion of `src/Legacy/` after the necessary behavior has been ported and validated.
