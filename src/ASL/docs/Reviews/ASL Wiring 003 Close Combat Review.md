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

Vehicle CCV context now gathers Guard status, printed or inherited Assault Engineer status, vehicle concealment, and the represented Ethiopian nationality. New records carry `CcvVersion = 1`; older records retain the no-context reading. Current experience facts are gathered by the same helper in the command and verifier. The UI and command ask the same attacker refusal. A versioned CC record eliminates active passengers, riders, and represented crews when Rules says the vehicle result eliminates them, before either the reaction or sequential projector path returns.

Four existing vehicle CC tests passed: sequential Infantry attack, vehicle attack, Reaction Fire, and an older Original-Two record. A new played-command test passed for vehicle destruction eliminating its passengers. Three Units cases passed for nullable combat JSON and AE lineage: a printed AE squad produces an AE HS, lower-quality Replacement permanently loses the status, later Battle Hardening does not restore it, and null legacy lineage retains the old projector reading. Nine focused Rules cases distinguish ordinary and lower-quality lineage, including a casualty-plus-ELR result after earlier Battle Hardening. These are intermediate results; the final UI build and affected tests still need their current run.

Independent review found two defects and verified their fixes: represented crews are included in PRC elimination, and current vehicle attacks refuse unknown Green or Conscript defender experience before drawing dice. The Rules resolver also enforces this refusal, while the null-context entry point retains its answer. The Rules regression passed. Focused caller tests passed for all three occupant roles and represented crew loss during Reaction Fire. The UI's attacker filtering and existing Location selection tests passed with warnings as errors.

Two acceptance games were played through the governed Studio controls on port 6680. In `wire-stealth-ambush`, the Finnish squad advances from [E5] to the concealed Russian stack in woods at [F4] on board 04. Drs 2 and 6 become 1 (Stealthy) and 4 (concealed), producing a Finnish Ambush. The record carries `StealthVersion = 1`. In `wire-cc-passengers`, the Russian squad passes its PAATC and advances onto the German halftrack at [F4] on board 02. Its CC DR 3,3 becomes 4 against Kill Number 5: the halftrack becomes a wreck, and the adjudicator's Units view shows its passenger eliminated. That record carries `CcvVersion = 1`.

The strict gate passed on 2026-10-30 with zero warnings or errors. The dependency baseline is `ee9856f7`, containing PRs 75, 79, 77, and 78 in that order, plus main's merged wire-fire-4, without the close-combat caller changes. Its baseline completed with 126 games, 3,471 replay lines and 55,753 sweep lines. Comparison proofs and final review remain pending. The earlier local composition `96e79798` was superseded before gate execution.

Backlog section 87 records the unresolved policy for retrying a Search whose roll encounters unknown hidden facts. The existing store intentionally does not retain failed rolled batches. It also records state facts not yet represented for Banzai/Human Wave, swimming, and special national CCV cases.

## Memory and performance

The caller reads only the participating units' facts. Concealment uses its existing indexed traversal. Search reuses its selected enemies; Mop Up reuses its selected defenders. Ambush scans state units once for participant designations and reads indexed stacks for experience. No new persistent cache, map rebuild, or retained game graph is introduced. Recorded properties are nullable and omitted when absent. No performance improvement is claimed.
