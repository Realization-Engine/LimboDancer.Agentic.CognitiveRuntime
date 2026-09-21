# LimboDancer source tree

This directory is the transition boundary between the legacy `LimboDancer.MCP` implementation and the new `LimboDancer.Agentic.CognitiveRuntime` implementation.

## Current layout

| Path | Status | Purpose |
| --- | --- | --- |
| [`docs/`](./docs/) | Active documentation | Current runtime specifications, design guidance, implementation planning, and retained legacy reference documents. |
| [`Legacy/`](./Legacy/) | Temporary legacy source | Previous `LimboDancer.MCP.*` projects retained only as behavioral and migration references. |

There are not yet any new production projects under `src/`. The planned `src/LimboDancer/` solution and projects described in the architecture documents are the **target source layout**, not the current repository state.

## Target layout

The first production implementation is expected to introduce:

```text
src/LimboDancer/
  LimboDancer.sln
  LimboDancer.Abstractions/
  LimboDancer.Runtime/
  LimboDancer.Infrastructure/
  LimboDancer.Adapters.Mcp/
  LimboDancer.Host/
  tests/
```

The exact sequence and admission criteria are defined in the [Implementation Plan](<./docs/LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md>). The [documentation index](./docs/) identifies the normative specification and its supporting design documents.

## Legacy boundary

New production projects must not reference projects or assemblies under `Legacy/`. Required behavior may be inspected, reimplemented, tested, and admitted into the new architecture. The intended end state is deletion of `src/Legacy/` after the necessary behavior has been ported and validated.
