# Ordnance Decisions Batch 3 Rules Design

**Status:** Approved and implemented Rules-only, 2026-11-04; pending PR review and user merge approval. Baseline: main `23920b0e`, following merged Batch 2 PR #124; claim lock `6e2b253e`. Section 8 records design-review corrections; section 9 records implementation evidence. Claim state is built, with assigned reviewer Claude retained.

**Scope:** C2.21 ammunition defaults, following the user's latest scope statement. C6.57 concealed-target acquisition remains separate work. This design builds a Rules verdict and preserves existing callers' answers. The correction to live shots arrives through later wiring.

**Authoritative wiring record:** [Rule inventory, section 7](<../ASL Rule Inventory A to E.md#7-wiring>) now holds open, unclaimed W-C2.21. Section 7 below retains its handoff rationale. No separate wiring document or numbered wiring prompt was created.

## 1. Rule and scope decisions

Read C2.21 on physical PDF p. 167 of eASLRB v3.01. Its default is HE against an unarmored target and AP against an armored target, only if that ammunition is available, has no Depletion Number, and is allowed on the selected Target Type. Vehicle identity alone does not establish armor. An unarmored truck is an HE-default target, including when attacked on Vehicle Target Type.

Supporting reading: C3.31-C3.33, p. 169; C3.33-C3.41, p. 170; C8.31, p. 178; C8.8-C8.92, p. 179. The training example on p. 644 explicitly requires declaration of the 57mm AT Gun's limited-stowage HE.

The whole 716-page rulebook was searched for C2.21 references, assumed ammunition, limited stowage, HE equivalency and caliber prohibitions. The reproducible reader is `code-builders/pass-history/ordnance-batch-3/read_sources.py`; its report is in `$ASL_GATE_WORK/batch3-reading/whole-book-scan.txt`. Search hits are discovery evidence, not a claim that every referenced rule was read or implemented.

Decisions:

1. **No substitute default.** When C2.21's preferred round cannot be assumed, return `DeclarationRequired`, without selecting an alternative or declaring the entire shot illegal. A separately declared round remains subject to its own rules.
2. **Depletion is not probability here.** AP/HE with a Depletion Number is special ammunition and never becomes an assumed default, even when its current adjusted number is high, ammunition remains, or the number is unavailable at the scenario date. Do not confuse a null result from `Depletion(...)` with ordinary unlimited ammunition. Use whether the counter lists a number, separately from recorded exhaustion.
3. **Target Type is distinct from target protection.** HE is compatible with Infantry, Vehicle and Area Target Types. AP is compatible with Vehicle Target Type; AP HE equivalency on Infantry Target Type concerns explicitly declared ammunition, not an AP default against an armored target. AP is not compatible with Area Target Type. An armored target paired with Infantry Target Type is outside the supported combination, not permission to attack it.
4. **Area fire does not silently change the preferred round.** An armored target on Area Target Type cannot default to AP. Return `DeclarationRequired`; a separately declared permitted round, such as HE or SMOKE, is checked elsewhere (C3.31-C3.33). This is not an instruction to auto-select HE, nor a ruling that HE is the only permitted declaration. This calculator does not infer a target's protection from every occupant of a hex. Empty-hex and mixed-target Area selection without a single supplied target-protection fact is not evaluated in this slice. Any additional default rule for these cases requires Rules work before wiring can use it.
5. **Explicit declarations bypass default selection.** Return `NotApplicable` when a declaration is present. This does not validate the declaration, its date, depletion, Target Type, range, or weapon eligibility. Do not silently replace a declaration rejected by another calculator.
6. **Unknown stays unknown.** A missing fact needed to decide the default returns `NotEvaluated`. Facts for the unselected round are irrelevant. A known decisive obstacle, such as a listed Depletion Number, can establish `DeclarationRequired` without asking for otherwise irrelevant stock facts.
7. **C2.21 is a partial section claim.** This slice covers default ammunition and the AP/HE caliber prohibitions relevant to that choice. Existing caliber, suffix, TH and TK calculations remain unchanged. The limbered-caliber exception referring to C10.24 is not implemented here; the section's overall coverage must remain partial while that gap remains.
8. **This is an ammunition component, not Target Type legality.** A selected HE verdict on Vehicle Target Type does not establish that the target is a vehicle, exclude motorcycles, or validate LOS, range or firing timing. C3.31 excludes motorcycles and requires a specific vehicle. Those existing checks still compose with this verdict. The armored/Infantry and mortar/non-Area exclusions below prevent contradictory inputs within this component; they are not a complete C3 eligibility implementation.

## 2. What decides it today

Paths below are relative to `src/ASL/`. The map was checked against `rule_sites.py C2.21 --words ammunition` and focused searches for `HeAssumed`, unnamed ammunition and vehicle/AP defaults. Broad ammunition hits also cover replenishment, breakdown and effects; those are supporting rules, not additional C2.21 selectors.

| Site | Role and finding | Treatment |
|---|---|---|
| `LimboDancer.Domains.Asl.Play/LiveOrdnance.cs`, `FromState` | Decides: substitutes AP whenever a vehicle is selected and no ammunition was supplied | Later wiring asks `SelectDefault`; never infer armor from presence of `vehicleTarget` |
| Same file, `AmmunitionChoices` | Gathers definition, date, side and depleted rounds; delegates choices to Rules | Preserve; future default gathering uses the same state snapshot |
| `LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor`, `OrdnanceAmmunitionFor`, `ordnanceAmmunition` initialization/reset and shot construction near `HeAssumed` | Decides: initializes to AP, uses the first offered round when the current choice is absent, and suppresses some HE declarations | Later wiring distinguishes a deliberate declaration from an automatic default, and uses the same verdict as commands |
| `LimboDancer.Domains.Asl.Rules/ScenarioA1Definitions.cs`, `HeAssumed` | Decides a definition-only HE assumption, without target, Target Type or exhaustion | Retain public signature and exact answer through an explicitly named compatibility method; new requests use complete facts |
| Same file, `Ammunitions`, `AmmunitionChoices` | Lists counter ammunition and declaration choices, including C8 availability | Keep list semantics and ordering; a list's first entry is not a C2.21 verdict |
| `LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceCalculator.cs`, `Outside` | Decides the undeclared limited-stowage HE refusal for the legacy Infantry path; also checks C8 availability | Extract only the C2.21 declaration predicate into the dedicated calculator; preserve refusal order/text and the C8 checks |
| `LimboDancer.Domains.Asl.Rules/ScenarioA1ArmorCalculator.cs`, `Lists`, `Depletion`, `Outside`, `Run` | Supporting Rules: counter number, dated availability, exhaustion, declared-round legality and TK effects | Reuse counter interpretation; no new C8 algorithm. Armor consumes an already named round and does not fix Play's default |
| `LimboDancer.Domains.Asl.Rules/ScenarioA1GunDuels.cs`, duel eligibility | Decides a legacy restriction: AP at a vehicle, unnamed HE otherwise | Move that predicate into an explicitly named compatibility method in the dedicated calculator, preserving its answer; later wiring must revisit the duel path |
| `LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs`, shot description | Records/displays: falls back to AP for an old vehicle shot with no ammunition name | Preserve old-history rendering; later wiring displays the newly recorded selection, without recomputing it |
| `LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs` and existing shot/event projection | Gathers, resolves and records existing `OrdnanceShot` facts | No changes in this claim; do not add default-selection state to existing serialized records |

## 3. Rules interface and decision order

Add `ScenarioA1AmmunitionDefaultModels.cs`, `ScenarioA1AmmunitionDefaultCalculator.cs`, and `ScenarioA1AmmunitionDefaultContract.cs`.

The public entry is `SelectDefault(AmmunitionDefaultFacts facts)`. Use Rules-owned enums and immutable scalar records, with nullable inputs for unknown facts:

| Fact | Meaning |
|---|---|
| `Declared` (`bool?`) | A round was explicitly declared; does not mean it was accepted |
| `WeaponKind` | Ordinary Gun or vehicle Gun MA; mortar; unsupported fixed-ammunition/other weapon |
| `TargetProtection` | Armored or unarmored, based on known target facts, not vehicle identity or visible PRC |
| `TargetType` | Infantry, Vehicle or Area, explicitly supplied; no null-to-Infantry convention |
| `Ap`, `He` | Each is a stock-fact record: `CounterProhibits`, `ListsDepletionNumber`, `Exhausted`, all `bool?` |

`CounterProhibits` reflects the counter's overscore/underscore. `ListsDepletionNumber` means AP#/HE# exists, not that `Depletion(...)` returned a number today. `Exhausted` is recorded loss of that ammunition. These inputs concern only default selection. A listed special round may still be declared despite an ordinary-caliber prohibition, under the existing special-ammunition rules.

Provide `StockFacts(GunDefinition gun, bool? apExhausted, bool? heExhausted)` beside the selector to map a known Rules definition into the two stock records, reusing `ScenarioA1ArmorCalculator.Lists`. It copies `NoAp`/`NoHe` and preserves nullable exhaustion facts; it does not read game state or silently resolve unknown values. The caller must already be entitled to know the supplied definition. A missing/unknown definition is not an ordinary Gun with both ammunition types available. The selector still accepts plain stock facts for Workbench examples and synthetic boundary cases. Date, Elite and shortage adjustments are unnecessary for this decision: listed special ammunition always requires declaration. Existing C8 calculators retain their responsibility for a declared shot. Scenario-specific ammunition overrides must be resolved by their governing Rules before supplying these facts; unsupported overrides must not be flattened into ordinary stock.

Verdict: `Disposition`, nullable `Ammunition` (`Ap` or `He` only), stable `Reason`, `Rule` and physical PDF `Page`. Dispositions are `DefaultSelected`, `DeclarationRequired`, `NotApplicable`, and `NotEvaluated`. Only `DefaultSelected` has a non-null round. None means the complete shot is authorized.

Evaluate in this order:

1. Unknown declaration state: `NotEvaluated`. Declared: `NotApplicable`, without needing stock or target facts.
2. Missing/unsupported weapon or Target Type: `NotEvaluated`; mortar with a non-Area Target Type is unsupported. Fixed-ammunition LATW and MG attacks are not ordinary Gun defaults. Explicitly reject undefined enum values rather than treating a switch default as ordinary/Vehicle.
3. Missing or undefined protection: `NotEvaluated`. Armored plus Infantry Target Type: `NotEvaluated` with an unsupported-combination reason.
4. Select the C2.21 candidate, HE for unarmored or AP for armored. If that candidate is incompatible with the selected Target Type, return `DeclarationRequired`.
5. For that candidate only, known listed depletion, counter prohibition, or exhaustion establishes `DeclarationRequired`, in that reason precedence. Do not ask for the other round's stock. If no known obstacle exists but a required candidate fact is unknown, return `NotEvaluated`.
6. Otherwise return `DefaultSelected` with the candidate. No search over alternative rounds.

Compatibility methods live beside this decision and are explicitly marked for existing live and recorded-game callers. Preserve their predicates exactly:

| Method | Exact compatibility behavior and call boundary |
|---|---|
| `LegacyHeAssumed(GunDefinition gun)` | `!gun.NoHe && !Lists(gun, "he")`. `ScenarioA1Definitions.HeAssumed` retains its existing definition lookup and short-circuit behavior, including false for an unknown definition, before forwarding |
| `LegacyHeNeedsDeclaration(string? ammunition)` | `ammunition is null`. Invoke only inside the existing `Outside` guard: no vehicle target, not Canister, not HE equivalency, and HE has a listed Depletion Number. Keep the C8 availability `else if` paired with this result at its present location; do not broaden the guard or treat an empty string as null |
| `LegacyDuelAmmunitionMatches(bool vehicleTarget, string? ammunition)` | `ammunition == (vehicleTarget ? "ap" : null)`, with ordinal case-sensitive semantics. Preserve its position after the existing CA check and the existing refusal for a mismatch |

Existing public members retain their names, return values, refusal text/order and exception behavior. These methods are not the interface for new firing decisions. Do not route an old `OrdnanceShot` wholesale through `SelectDefault`, normalize its ammunition string, or change its serialization.

## 4. Contract and disclosure

Register `c2.21-ammunition-default` through `[RuleContract]`. Contract outcomes are `default-he`, `default-ap`, `declaration-required`, `not-applicable`, and `not-evaluated`: the first two map the selector's `DefaultSelected` and round explicitly, so a consumer never has to infer the round from prose. No rolls or events are produced. Cite C2.21 p. 167 for stock/default decisions, C3.31-C3.33 p. 169 for Target Type restrictions, and C8.8 p. 179 for limited stowage. Place: ammunition declaration/default selection before the shot's TH DR, with PFPh/2.22A as the Workbench demonstration location, not an exclusive phase restriction. The ASOP search found no rows citing C2.21; no ASOP step is claimed by this design.

Use a self-owned firer Unit fact, `Concerns = firerId`, for private weapon kind, counter ammunition and exhaustion facts. For a separate Gun, this anchor is its manning unit; for a vehicle MA it is the vehicle. Do not require an equipment id in a Unit picker. All private facts concern that anchor. If its identity is missing or sealed, the contract adapter passes unknown for the weapon kind and both stock records, even if hand-entered private values were retained from an earlier selection. A viewer cannot recover a hidden weapon's profile or ammunition by switching target or selecting a default.

Public `Declared` and `TargetType` concern the proposed action. For this Rules-only Workbench contract, `TargetProtection` is an All-disclosed, explicitly labelled hypothetical/known-public input, entered by hand; it is not a request to inspect a target's hidden definition. Bundle instructions must distinguish visible-counter examples from hypothetical variants. Do not introduce a dynamic disclosure mechanism or a production fact gatherer in this claim. Future wiring may supply actual protection only when established for the viewer; otherwise it passes unknown.

Mark all contract inputs `MayBeUnknown = true`, including `firerId`, weapon kind and each stock flag, so the existing generic contract validator does not reject irrelevant absent facts before the selector can apply its ordered rules. This does not make missing decision facts valid: the selector still returns `NotEvaluated` where they matter. A malformed supplied value remains `not-evaluated` under the existing validator, even on an otherwise irrelevant branch; omit an irrelevant input or supply JSON null instead. The adapter only parses facts, masks unanchored private values and maps verdicts; it must not duplicate C2.21 logic.

Run owner, opponent and adjudicator views of the bundle, including switching views after entering private facts. An opponent lacking the firer's private stock must not receive a selected default or a stock-specific refusal. Publicly established non-selection outcomes, including `NotApplicable` for an explicit declaration, do not require private stock. A displayed hypothetical answer is labelled as hypothetical, not evidence about a hidden counter. `ScenarioA1RuleContracts.Evaluate` validates supplied JSON, not a viewer's authority; disclosure tests must exercise the Workbench's filtering/anchor behavior as well as the pure adapter. This claim introduces no production disclosure or concealment changes.

## 5. Acceptance and minigame

Use one small bundle with an actual catalog Gun, a real unarmored truck and an AFV on inspected board positions. Record the actual definitions and hexes after checking the position in isolated Studio 6680. Include a counter with limited-stowage HE where the catalog supports it. Synthetic AP# and exhaustion variations are explicitly labelled hand-entered facts, not new catalog counters or played events.

Inspect the Workbench position before writing/running the focused tests. The bundle demonstrates the new verdict independently of the unchanged live Play default. Unless a case overrides them, use `Declared = false`, a known ordinary Gun, a known unarmored target, Vehicle Target Type, and known false prohibition/listed-depletion/exhaustion flags for both rounds. This baseline makes each changed input and expected result unambiguous. For direct contract comparisons, supply the owner anchor and equivalent authorized facts. Required cases:

| Case | Expected result |
|---|---|
| Ordinary HE, unarmored truck, Vehicle Target Type | HE selected |
| Same target and stock, Infantry Target Type | HE selected |
| Ordinary AP, armored target, Vehicle Target Type | AP selected |
| Ordinary HE, unarmored target, Area Target Type; mortar variant | HE selected |
| Armored target, Area Target Type, with HE available | Declaration required, not automatic HE or AP |
| Armored target, Infantry Target Type | Not evaluated, unsupported combination |
| Preferred AP/HE lists a Depletion Number | Declaration required, including high/dated numbers and no currently usable number |
| Preferred round exhausted or prohibited, other round usable | Declaration required; no fallback |
| Explicit AP against a truck, or any other declared round | Default selection not applicable; its legality remains a separate check |
| Missing required candidate fact, no known obstacle | Not evaluated |
| Known listed depletion, other candidate facts unknown | Declaration required; no false certainty about actual availability |
| Unselected round unknown or altered | Same verdict as before |
| Fixed-ammunition weapon, unsupported enum value, missing protection | Not evaluated |
| Hidden stock or hidden target protection | No unauthorized default or private explanation |
| Explicit declaration with all unrelated inputs absent | Not applicable through both the selector and contract |
| Missing unselected stock through the contract | Same result as the pure selector, not an eager missing-fact refusal |
| Missing or sealed firer anchor with retained private fields | Adapter masks private facts; no selected default or private reason |
| Counter prohibition or exhaustion known true, other candidate flags unknown | Declaration required; deterministic first known obstacle in section 3 order |
| Malformed supplied contract field, including an irrelevant one | Not evaluated by the existing validator, distinct from absent/null |
| Invalid weapon/Target Type/protection enum values individually | Not evaluated; never fall through to AP or HE |

Compatibility tests preserve `HeAssumed`, including unknown definitions, the existing `he-not-named` outcome and the duel restriction. Cover null, empty, lowercase and uppercase ammunition strings; vehicle and non-vehicle duel targets; and simultaneous legacy refusal conditions to prove ordering. The legacy vehicle duel with no explicit round remains refused. Prove the HE-declaration helper is reached only under its existing guard and does not replace the declared-round C8 availability check. Retain existing limited-stowage tests without changing expected answers. Assert all verdict invariants and that no input is mutated. Test the stock adapter separately for AP#/HE#, prohibited ammunition and nullable exhaustion. Test the pure selector and the contract's disclosure behavior, not a changed live firing flow.

## 6. Claim, validation and performance

After design approval, refresh main and claim C2.21 through the inventory. Hold the three new Rules files and shared edge-only holds on `ScenarioA1Definitions.cs`, `ScenarioA1OrdnanceCalculator.cs`, and `ScenarioA1GunDuels.cs`. No Armor file edit is required to reuse its existing internal `Lists` helper. Check current holds before claiming; add any newly required hold before editing it.

Add tests to the existing Rules test project and one minigame bundle. No new project/reference or lock-file change is planned. No production Play, Units, Turns, Maps, or Studio Play page change. A Workbench edit is allowed only if the contract cannot use its existing controls, and must be identified before implementation.

The selector adds constant work on a fixed number of scalar facts. The stock adapter and legacy definition helper scan only the supplied Gun's `SpecialAmmo` entries using existing `Lists`, O(n) in that counter's entries, not the whole catalog. No map reads, unit scans, I/O, caches or process-wide mutable state. Reuse existing immutable reference data. Facts and verdicts are bounded per invocation. Viewer-specific inputs are never shared as answers between viewers. Rendering, seams, pinned boards, rout settlement and state memo lifetimes are unaffected. Tests load expensive references once per fixture; no new CI share is needed.

Validation: Workbench evidence first, named Rules tests for the new verdict and legacy seams, claim/status checks, and green PR CI. Report actual logs and evidence, not presumed results. Section 9 records the completed local proof; PR CI remains a separate acceptance step. No performance improvement is claimed; allocation review and bounded-input checks are sufficient for this scalar addition. Register and clean build outputs under the existing outside-output procedure.

At this baseline two claims have merged since the last five-claim gate at `aa25111d`. C2.21 would make three if no other claims intervene. Recount at merge; run the gate only when the board requires it. User authorization is required for the implementation and later PR merge under AGENTS.md.

## 7. Downstream wiring rationale, recorded in inventory section 7

**W-C2.21**, capability **fire**, was recorded at built. This section explains the handoff, not a second claim ledger.

| Site / responsibility | Replacement or required migration |
|---|---|
| `LiveOrdnance.FromState` unnamed-round substitution | Gather actual known protection, declared Target Type, weapon and stock; ask `SelectDefault`. A truck defaults to HE, not AP. A required declaration is surfaced, not synthesized |
| Play page `OrdnanceAmmunitionFor`, initial/invalid-choice fallback and HE suppression | Keep explicit player choices distinct from the selector's result. Do not choose the first offered special round when no default exists. Use the command's same facts and verdict |
| Planner and shot persistence | Record the selected round and the applicable reading for new games; preserve old records and replay. Define the migration in the wiring claim, without adding fields in this Rules claim |
| Gun Duel preparation and legacy restriction | Reconcile new default selection with duel eligibility before moving it; do not allow the old AP-only vehicle shortcut to reject the corrected HE truck default silently. Preserve recorded duel readings |
| `PlayRecords` ammunition label | Use the newly recorded round; preserve the old AP fallback only for histories recorded under the old reading |
| Old Rules surfaces | Migrate callers of `HeAssumed` and compatibility predicates; retire `LegacyHeAssumed`, `LegacyHeNeedsDeclaration`, `LegacyDuelAmmunitionMatches` only when no old replay path needs them. Do not remove the general declaration-choice or C8 APIs |

The wiring gate must account for changed live defaults and menu behavior while older games replay identically. Wiring gathers authorized target knowledge and preserves unknowns. It must not decide new Area-hex default rules or guess protection from a mixed stack: any extension needs its own approved Rules design/claim first, then wiring to that verdict. If the existing inventory already has a W-C2.21 row at built time, update that row instead of creating a duplicate. No wiring rows are marked claimed, wired or reviewed by this design.

## 8. Review corrections and deferred scope

The requested design review on 2026-11-04 checked C2.21 and supporting Target Type/depletion text against the extracted PDF pages, the actual legacy predicates, `ScenarioA1RuleContracts.Evaluate`, and the Workbench's owner/anchor filtering. Corrections applied:

- Replaced the overly narrow Area-fire HE-declaration wording with a declaration of a permitted round; added the boundary between default selection and complete Target Type legality.
- Made the stock adapter concrete, specified unknown-definition/nullable-exhaustion behavior, and corrected its performance description from constant time to a per-counter ammunition scan.
- Defined distinct contract outcomes for AP and HE, nullable input metadata, malformed-input behavior and explicit rule citations.
- Specified the manning-unit/vehicle ownership anchor, masked stale private inputs, and made the Workbench's target-protection examples explicitly public/hypothetical rather than implying unsupported dynamic disclosure.
- Specified exact legacy predicates, guards, case/null semantics and refusal-order regression cases.
- Removed the assignment of unresolved Area-default rules to wiring. Future rule extensions belong in Rules; inventory section 7 remains the only downstream wiring ledger.

No production implementation or tests were run during the document review. The user subsequently approved implementation; its evidence follows. Independent review does not replace assigned PR review or user merge approval.

Deferred work is recorded in [the existing backlog, section 101](<../ASL Unit Backlog.md#101-deferred-by-ordnance-decisions-batch-3>). C2.21 remains partial overall until its other uncovered clauses are addressed. C6.57 is neither claimed nor implemented here.

## 9. As built and verification

Implemented the three planned Rules files, `SelectDefault`, the bounded per-counter `StockFacts` adapter, the registered contract, and all three named legacy predicates. The public `HeAssumed` lookup, guarded legacy HE refusal, and Gun Duel edge now delegate without changing their answers. No Play, Units, Turns, Maps, event schemas or Studio page code changed. No additional Rules holds were needed.

The `c221-ammunition-default` bundle has one setup-only game on board 03: the catalog 25-pounder game-default profile and British crew at [D5], actual catalog British 6-pounder with HE# at [D6], German Opel Blitz at [F5], and PzKpfw IIIH at [F6]. Ordinary and limited-stowage examples are distinguished from labelled hypothetical stock and target variants. No firing event is claimed as played acceptance.

Builder observed the position in isolated Studio 6680 before writing the focused tests. The board inspector confirmed the manning crew at [D5] and its operated Gun; the map showed both Guns, truck and AFV at their recorded positions. The adjudicator's hand-entered contract returned `default-he` for the truck, `default-ap` after changing known target protection to armored, and `declaration-required` after selecting Area Target Type. The British owner obtained `declaration-required` for listed HE depletion with other stock unknown. Switching to German cleared prior entries and hid weapon kind plus all six stock flags; re-entering public declaration/target facts returned `not-evaluated`, without a private stock reason. These are Workbench verdicts, not live shot changes.

Evidence under `$ASL_GATE_WORK` (`E:/Archive/GitHub/dlandi/asl-gate-work-codex`):

| Check | Read result / evidence |
|---|---|
| Isolated Studio, locked restore, warnings as errors | `ordnance-batch3-build.log`: build succeeded, 0 warnings and 0 errors |
| Named new Rules and legacy integration tests | `ordnance-batch3-Rules.log` and `.trx`: 14 passed, 0 failed |
| Shared contract missing/mistyped facts and JSON round-trip | `ordnance-batch3-contract-regression-Rules.log` and `.trx`: 2 passed, 0 failed |
| Bundle checks plus manifest/game-header integrity | `ordnance-batch3-Play.log` and `.trx`: 16 passed, 0 failed |
| Independent read-only review | `batch3_rules_review` reviewed rulebook, selector, disclosure, legacy edges, tests and the saved logs; no remaining material finding. Two documentation corrections were applied |

Initial checks caught enum/initializer formatting, a test call to an internal class rather than the existing public depletion API, and a regression fixture that mistook the non-vehicle Gun Duel reply for a vehicle target. These were corrected before the final green runs; production rule behavior was not changed to fit the fixtures. The verification script uses warnings as errors, checks nonzero passing TRX counts, and saves the final logs separately from transient test output.

Overall C2.21 coverage remains partial, and W-C2.21 remains open/unclaimed. No batch gate is due for this unmerged claim; recount merged claims at merge. PR CI, assigned review and user authorization to merge remain distinct from these local results.

The first PR CI run exposed a shared test assumption that every contract has an unconditionally required fact. This contract correctly defers conditional missing-fact decisions to its calculator. The shared test now checks every required field for absence and every field for invalid types, including contracts with no unconditionally required fields. Both affected contract checks passed locally; independent review found no weakened assertion or production behavior change.
