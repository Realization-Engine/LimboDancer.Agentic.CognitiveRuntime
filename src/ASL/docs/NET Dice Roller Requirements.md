# .NET Dice Roller Requirements

Status: Approved. Library requirements DICE-01 through DICE-06 implemented; ASL integration DICE-07 through DICE-12 remains for step 9.

## Purpose and scope

Provide a small, reusable .NET class library within `src/ASL/LimboDancer.Domains.Asl.sln`. ASL is its first consumer, but the library must have no dependency on ASL rules or application infrastructure.

Agreed scope: system-generated dice, arbitrary integer die sizes, ordered individual results, and an application-owned audit trail. Keep the implementation small. This work does not implement unit step 9 or change step 8.

## Library requirements

| ID | Requirement |
|---|---|
| DICE-01 | Build as a standalone class library named `LimboDancer.Dice` in the ASL solution, using its existing .NET settings. No third-party runtime packages or project dependencies are required. |
| DICE-02 | Accept a structured request containing count and sides. Each request rolls dice of the same size. Support counts 1 through 100 and sides 2 through `int.MaxValue`, inclusive. The count limit bounds allocation and work. |
| DICE-03 | Return the requested number of individual integer values, each between 1 and sides inclusive, in generation order. Retain the request in the result and prevent callers from mutating the returned values. |
| DICE-04 | Generate production values with the .NET cryptographic random number generator, without introducing modulo bias. Do not accept caller seeds or supplied results in the production API. |
| DICE-05 | Reject null or invalid requests before drawing any values. Propagate generator failure without returning a partial result or substituting a value. |
| DICE-06 | Allow deterministic tests through a small internal randomness seam. Production callers use the default generator. |

## ASL integration requirements

These requirements belong to the future ASL integration, not to the standalone library.

| ID | Requirement |
|---|---|
| DICE-07 | ASL determines why a roll is needed, count, sides, positional die roles, modifiers, and rules outcomes. OVR elections remain separate game declarations. The library has no NTC or OVR vocabulary. |
| DICE-08 | Generate only after Execution Gate confirmation and authorization, with a final revision and duplicate-attempt check inside the game commit boundary. Planning, previews, gate evaluation, and event replay must not draw dice. |
| DICE-09 | Record accepted dice as game events, preserving request, ordered values, purpose, system source, initiating actor, attempt and roll identity, and correlation to the declaration and rules interpretation. Reuse existing event envelope fields where available. |
| DICE-10 | Retrying a committed attempt returns its recorded values without generating again. Reject reuse of the same attempt for different action inputs. Concurrent confirmations must produce at most one committed result for the attempt. |
| DICE-11 | Publish results only after successful persistence. If persistence has an uncertain outcome, read the canonical game log before retrying. A draw lost before commit has no game effect and is never shown as an accepted roll. |
| DICE-12 | Replay uses recorded values and validates their count and bounds. Link the existing execution audit to game event identifiers; the game log is authoritative for dice results. |

The audit model trusts the system. Independently verifiable randomness and tamper-proof storage are not requirements.

## Acceptance

Library tests cover ordered output from a fixed source, inclusive value boundaries, invalid inputs without any draw, immutable results, and generator failures without partial output. A small production smoke test checks count and bounds, not statistical fairness.

Before enabling live ASL dice, integration tests must cover confirmation, stale revisions, duplicate and concurrent attempts, mismatched attempt inputs, replay with a generator that fails if called, and failures before and after persistence. An unfavorable roll is a valid result and must be recorded.

## Exclusions

No dice notation parser, mixed-size pools, modifiers, totals, reroll policy, exploding dice, physical dice entry, UI, MCP endpoint, remote service, database, custom random algorithm, public seed management, or package publishing. Add capabilities only for a concrete consumer need.

Design: [NET Dice Roller Design](<NET Dice Roller Design.md>).
