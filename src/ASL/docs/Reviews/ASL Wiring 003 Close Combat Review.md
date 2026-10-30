# Wiring 003 close-combat implementation evidence

Status: implementation in progress on `codex/wire-close-combat-1`. Neither wiring row is marked wired. The user approved the designs on 2026-10-30.

## Stealth callers

New Ambush records carry nullable `StealthVersion = 1`. The planner, offered action, and verifier gather the same explicit OB designation and SSR facts. Experience comes from the unit's validated class and its same-side leaders at its Location. The null version uses the previous recorded reading. Unknown versions are refused.

Scenario groups may record an explicit `stealthDesignation`, omitted when null. Rules parses the designation and side-level `stealthy:<side>` and `lax:<side>` tokens. Gambit's New Zealand group names ANZAC explicitly. No identity is inferred from the group's prose name.

Search, Search Casualties, Mop Up Casualties, and concealment growth read `ScenarioA1StealthCalculator.Status`. Search checks its own participating units before proposing. Hidden defenders are checked only after the actual searched hexes are known. Mop Up checks only its casualty contributors. A missing contributing fact rejects the entire event batch through the store, before replay, serialization, file replacement, or cache update. The strict concealment adapter asks for Stealth only when the eligible unit needs a dr; automatic and night gains preserve their answers.

Focused builds with warnings as errors passed:

- The existing Search Casualty reduction test.
- Search and casualty SSR modifiers, with Stealthy searchers and Lax defenders.
- Store-level rejection after a roll, with the complete stored game unchanged.
- A public Search proposal followed by atomic refusal for an unknown contributing Green defender's experience.
- Search succeeding when that unknown defender is outside the actual searched hexes.
- Mop Up refusing without writing queued hidden-placement events.
- A Finnish unit changing the new Ambush outcome, the same dice resolving under the old null-version reading, and unsupported-version refusal.

The separate Rules adapter PR 79 has eight passing focused cases, including strict unknown-fact handling and unchanged legacy concealment answers. Independent read-only review found the new caller gathering equivalent to the existing experience helper for the validated catalog. A previously suspected Green/null issue was retracted after checking the resolver's existing precheck.

## Remaining checks and limits

Vehicle CCV context, Assault Engineer lineage, attacker offers, and PRC elimination are still being implemented. Played caller minigames, the complete wiring gate, and final review remain pending. The replay gate has not yet been run for this branch.

Backlog section 87 records the unresolved policy for retrying a Search whose roll encounters unknown hidden facts. The existing store intentionally does not retain failed rolled batches. It also records state facts not yet represented for Banzai/Human Wave, swimming, and special national CCV cases.

## Memory and performance

The caller reads only the participating units' facts. Concealment uses its existing indexed traversal. Search reuses its selected enemies; Mop Up reuses its selected defenders. Ambush scans state units once for participant designations and reads indexed stacks for experience. No new persistent cache, map rebuild, or retained game graph is introduced. Recorded properties are nullable and omitted when absent. No performance improvement is claimed.
