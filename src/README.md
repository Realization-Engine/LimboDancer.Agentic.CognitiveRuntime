# LimboDancer source tree

This directory is the transition boundary between the legacy `LimboDancer.MCP` implementation and the new `LimboDancer.Agentic.CognitiveRuntime` implementation.

## Current layout

| Path | Status | Purpose |
| --- | --- | --- |
| [`LimboDancer/`](./LimboDancer/) | Active production source | New `.NET 10` runtime solution, production projects, and tests. |
| [`docs/`](./docs/) | Active documentation | Current runtime specifications, design guidance, implementation planning, and retained legacy reference documents. |
| [`Legacy/`](./Legacy/) | Temporary legacy source | Previous `LimboDancer.MCP.*` projects retained only as behavioral and migration references. |

The new production solution baseline now exists under `src/LimboDancer/`. It is intentionally structural: runtime contracts and behavior are introduced incrementally in the order defined by the implementation plan.

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
  tests/
```

All new projects target `net10.0`. Architecture tests enforce the approved project dependency graph and forbid references from new production projects into `src/Legacy/` or the `LimboDancer.MCP.*` project family.

The exact implementation sequence and admission criteria are defined in the [Implementation Plan](<./docs/LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md>). The [documentation index](./docs/) identifies the normative specification and its supporting design documents.

## Legacy boundary

New production projects must not reference projects or assemblies under `Legacy/`. Required behavior may be inspected, reimplemented, tested, and admitted into the new architecture. The intended end state is deletion of `src/Legacy/` after the necessary behavior has been ported and validated.
