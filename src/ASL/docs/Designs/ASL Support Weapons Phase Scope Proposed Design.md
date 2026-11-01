# Support Weapons: proposed phase scope design

Status: implementation approved. Based on the [phase scope analysis](<../ASL Support Weapons Phase Scope Analysis.md>) at main `7b5655ac`.

Task-specific authorization: the user approved implementing the Rules prerequisites and dependent UI together on `codex/support-weapons-implementation` in the isolated `support-weapons-implementation` worktree, before either merges. This overrides this document's separate-merge sequencing requirement for this task only. Rule ownership, the Rules-only decision boundary, targeted verification, replay compatibility and claim coordination still apply. Prepare a full Studio demo for review; do not merge this work without the user's approval.

## 1. User experience

The pane should answer: whose weapon, which operation, what is needed, and what happens next. Remove the common four-button form and its cross-phase paragraph. Preserve the established free-flow layout, with labeled fields, weapon choices beneath their owner, and a short action footer.

Use separate Recover, Transfer and Repair workflows during Rally. Keep configuration beside weapons during PFPh/DFPh, handling beside the moving/advancing unit, and load choices inside Rout or withdrawal. Specialized fire, ordnance, scrounging and vehicle workflows remain distinct.

### Map scope

- A map inspection click on a hex with applicable candidates scopes the relevant workflow to that hex. Show `Weapons in [G4]` and `Show all locations`.
- Clicking another applicable hex switches scope. Clicking a hex without applicable candidates releases it. Scope affects the visible candidate list, not a Rules answer.
- Keep full Location, level and vehicle identity in candidate records. A hex scope can contain several Locations; it must not allow a cross-level or cross-vehicle transfer.
- Explicit destination/target pickers own their clicks. A route or withdrawal destination click must not also rescope the source. Reuse the action pane's existing picker ownership.
- Respect the phase's existing acting-unit scope. Weapon rows follow the selected mover/router/advancer. Do not create a second independently scoped location that contradicts it. A selected eligible holder does not imply that all its weapons support every operation.
- For embedded controls, the parent workflow determines whether the clicked hex is applicable. A hex with an eligible router but no weapon operation must retain its Rout scope. The standalone weapon-scoping behavior above applies only when no parent action owns that scope.
- Unscoped lists contain viewer-authorized candidates only. Inspection can explain a known weapon's missing capability; it must not reveal hidden units, weapons or constraints through an offer's presence or absence.

### Operation layouts

| Operation | Fields and feedback |
|---|---|
| Recover | Weapon, current Location/possession, `Recover with`, concise cost and success information from Rules, then `Propose recovery`. A friendly broken holder is a valid distinct context when Rules permits it. Do not require an unpossessed weapon in the component. |
| Transfer | Weapon and current holder, `Give to`, then `Propose transfer`. Show eligible recipients and a concise explanation when a deliberately selected visible recipient is refused. No recovery label or roll wording. |
| Drop during movement/advance | Weapon beneath its holder, destination of the dropped weapon implicit from the holder's full Location, then `Propose drop`. Explain any relevant carrying consequence. No receiving-unit field. |
| Repair | Holder's eligible or relevant malfunctioned weapons, actual repair number/outcome information when known, remaining permitted attempts where Rules supplies them, then `Propose repair`. Vehicle repair keeps its established workflow and calculator. |
| Configure | Selected weapon's present state and exactly one applicable `Propose assemble` or `Propose dismantle` button. Show PP/use consequences. Do not require selecting the weapon for an attack or having a fire target. Malfunctioned but configurable weapons must remain reachable. |
| Rout preparation | `Carry` and `Leave behind`, with the Rules-approved load choices. Required retained equipment cannot be freely unchecked. Recompute route readiness after changing the load. |
| CC withdrawal | Per withdrawing unit: carried PP/capacity, permitted drops, destination and readiness. A short instruction such as `Leave enough equipment to withdraw` appears only when needed. Rules determines whether voluntary drops below the capacity limit are permitted; the page must not assume an overload is the sole reason to offer them. |

If configuration is eligible but the unit cannot otherwise fire, its weapon row still appears in a compact configuration group within the phase workflow. Configuration availability must not depend on Fire's attacker-candidate list. Likewise, movement weapon handling must remain reachable without first plotting a route.

Hide operations that do not apply to the context. Show a disabled operation with a short reason when it is relevant to an explicit selection but blocked. Missing input is different from ineligibility: `Choose a recovering unit` is guidance, not a rule refusal. Missing implementation/data is different again: `This weapon's dismantled counter is not supported` must not be described as an ASL prohibition.

Keep rule references and full text in expandable help. State only the next useful fact inline, for example `No MF available for recovery`, `Transfer window has closed`, or `Already attempted repair this Player Turn`. These examples describe required outputs; their precise conditions must come from the Rules audit, not literal UI comparisons.

## 2. Rules contract

Names here are proposed and must be settled with the Rules owner. Extend or consolidate the existing SupportWeapon, Rally, Fire and Withdrawal calculators. Do not establish a second calculator for a rule already owned elsewhere. Rules references Abstractions only.

Each operation needs a pure evaluation taking facts and returning:

- applicability to the requested context;
- allowed, refused, or not-evaluated status;
- a stable refusal/missing-facts code, concise explanation, and rule/page reference;
- relevant costs, allowed participants/load choices, use consumed and resulting configuration, where applicable;
- the timing/causal context to which that decision applies.

Separate applicability from readiness. An expected repair action can be applicable but refused for this holder. A request for normal transfer during AFPh is not an ordinary phase opportunity. Both still receive authoritative command refusals when submitted directly.

Contracts must name fact disclosure, allowed unknown values, timing, use/cost, effects and the actor who may make each choice. Missing facts yield not evaluated, never guessed permission. Costs and restrictions are not reconstructed from explanation text.

### Required facts

| Group | Examples the gatherer must provide |
|---|---|
| Identity and relationships | Weapon identity/type/definition, original side and capture history, holder/possession role, candidate actor and recipient, side, full Location, vehicle/carriage relationships. |
| Condition and capability | Good Order facts appropriate to the rule, pin/broken/TI/CX and other relevant states, Infantry/SMC/crew capabilities, malfunction, dm state, counter/repair data and hook-up state where applicable. Do not equate “not broken” with every rule's Good Order requirement. |
| Timing and prior use | Phase and phasing side, current side's opportunity, phase-start windows, unit movement/advance state, pending reactions, prior transfers/recoveries/repair attempts and weapon-use budgets. |
| Environment | Relevant Known enemy facts, bypass, Wire/Panji/Crest relationships, night and supported terrain/SSR modifiers. Unknown required environmental facts block evaluation rather than silently falling back to open-ground assumptions. |
| Withdrawal/load | Withdrawal kind, Location and round, declared intent, unit load and capacity facts, chosen drops and destination facts. Separate normal Melee withdrawal, Ambush and infiltration. |
| Triggered recovery, later capability | Qualifying event identity, original holder and weapon set, eligible SMC opportunities, already recovered item/attempts, and closure of that event window. |

Every fact set is operation-specific. Do not require irrelevant facts, such as repair numbers for a transfer, or an attack target for assembly. Application authentication/authorization stays outside Rules; ASL side and participant eligibility stays inside it.

## 3. Play, projection and UI wiring

1. **Gather candidates safely.** Use state indexes and the established disclosure view to identify relevant weapon/actor relationships. Candidate gathering is not eligibility. Include supported exceptional holding/carriage relationships rather than filtering to “unheld on the ground” before asking Rules.
2. **Evaluate without side effects.** A shared fact-building path feeds the operation's calculator. The offer carries weapon, actor, recipient if chosen, context, and structured verdict. Do not call roll-producing execution or allocate proposal events for every candidate during rendering.
3. **Present the verdict.** Components format labels and collect choices. They contain no rule-numbered tests for phase, Good Order, IPC, recovery cost, captured status or withdrawal legality.
4. **Revalidate the proposal.** The command rebuilds current facts and asks the same calculator. Reject a stale selection or closed timing window with the same rule reason. A ready UI offer is not a reusable authorization token.
5. **Confirm against the expected stream version.** Do not draw dice or mutate state on selection/render/preview. Preserve existing proposal confirmation, rolling and outcome-card behavior.
6. **Project the outcome.** Use the same relevant Rules decisions to verify new operation records and their consequences. A generic `EquipmentTransferred` event also serves automatic losses and other workflows; do not apply voluntary-transfer restrictions indiscriminately to every occurrence. Preserve its causal action/context.

The UI state machine clears recipient, weapon-specific input, load-dependent destinations and stale previews when their parent selection changes. On game, side, phase, stream version or context change, revalidate the draft and release expired pickers. Do not discard unrelated valid work on every render. A scope change must not submit, drop equipment or finish an action.

### Sequence and event obligations

Phase flags alone cannot prove a start window. Audit existing events for the facts needed to derive closure of RPh recovery and APh transfer. Where insufficient, design a recorded transition or opportunity with the Rules owner. ASL.Turns may gather and present this state, but must not calculate legality itself. State the closure rule and user action before implementation.

CC checks must refer to the current Location/round and declaration context, not whether any CC has been resolved anywhere. Retain the two-side declaration workflow and both sides' own private drafts, including the distinct handling of sequential CC. Do not add a SW-counter attack selector: A11.13 (PDF p. 72) prohibits such use. Preserve the existing resolution effects for weapon losses and destruction. Dropping for withdrawal is part of the declared withdrawal package, not an earlier independent mutation that can survive cancellation. Audit when equipment is left if withdrawal fails or the unit is eliminated; do not assume drops occur only on arrival. Existing `CloseCombatEffects` already contains departure/drop handling that must be preserved or deliberately corrected by Rules.

Immediate A4.44 recovery requires a durable causal opportunity and its own closure/accounting. Do not create it merely because an unheld weapon is visible. The ordinary Recover command must not be extended to arbitrary phases as a substitute. This new capability is separately proposed in backlog section 93.

### Recorded-game compatibility

Corrected eligibility may reject actions accepted by old planners. Preserve the recorded reading for existing games/events using the repository's rule-package/version conventions. Do not globally tighten legacy projection and break replay. Specify the version boundary, facts/events and named behavior changes before wiring. The merge gate must distinguish intended new behavior from accidental changes in historical replay.

## 4. Delivery and ownership

The [inventory](<../ASL Rule Inventory A to E.md>) remains the only claim board. This analysis takes no holds. Check the current board again when implementation is approved.

### A. Rules prerequisites

Audit and define complete operation contracts for currently exposed recovery, transfer, drop, repair and configuration, plus contextual rout/withdrawal handling. Relevant sections include A4.43/.431/.44, A9.72/.8, A10.4 and A11.21 and their cited dependencies. Existing calculators remain the starting point; a built legacy member is not proof of complete eligibility.

Use the repository's separate Rules-claim and wiring-claim process. The default ownership is Claude for Rules and Codex for wiring; agree concrete claims/files on the board rather than treating this document as a claim. Rules changes must be merged to main before callers depend on them. Preserve legacy member answers until the versioned wiring change moves their callers.

E1.8 calculators are present on this main, and the inventory records W-E1.8 as pending wiring. Coordinate configuration's E1.89 behavior with that work, using its authoritative use/counter/Gunflash decisions. Do not independently reimplement the night exception.

### B. First UI/wiring delivery

After prerequisites merge, replace aggregate offer flags with per-operation verdicts; integrate contextual weapon handling across all reviewed phases; fix broken-holder recovery reachability; replace generic CC dropping with Rules-approved withdrawal choices; and use authoritative repair/configuration readiness and concise labels.

Review the application sites listed in the analysis, the relevant Rules contracts, `GameActions` descriptors, projection handlers and phase-guide text together. UI changes must not leave direct API commands with different eligibility. Vehicle repair/scrounging and weapon firing remain on their specialized paths, with regression checks where shared presentation changes touch them.

### C. Explicit separate capabilities

The first delivery does not claim full rulebook completion. Immediate any-phase SMC recovery, A9.73 deliberate damage, missing dm counter/catalog representations, and unsupported special terrain/vehicle cases need approved capability work and the required facts/events before controls are exposed. Track these in backlog section 93. Existing supported vehicle/terrain behavior must not be removed under this boundary.

For an unsupported case encountered in a supported workflow, show an honest not-evaluated/capability message. Never substitute a stricter invented ASL rule or silently enable the operation. This boundary permits an honest contextual UI without pretending absent rules are complete.

## 5. Performance and disclosure checklist

| Concern | Design response |
|---|---|
| State/unit/hex work | Read relevant units and equipment once from state indexes. Build holder and full-Location groups once, then evaluate relevant combinations. Avoid scanning the entire game for every weapon/recipient pair. Do not enumerate every possible load in the UI; ask existing load calculators and retain their supported limits explicitly. |
| Cache key and lifetime | Bounded page/service memo for immutable state/version, game, viewer/side/disclosure context, rules reading and scoped operation. Selected recipient, load and withdrawal intent belong in selected-verdict keys. Replace the old memo on changes; no global unbounded game cache. |
| Concurrency | No shared mutable draft or memo between circuits/players. Expected stream version protects confirmation; re-evaluate after competing commits. |
| Disclosure and snapshots | Enumerate authorized candidates, use rule-specific Known facts, and filter explanations. Do not let hidden enemy presence change a public generic offer. Do not send privileged candidate lists to the browser. No new serialized cache or history-wide copied snapshots. |
| Rendering | Stable keys for weapon/actor rows, materialized small view models, and no Rules/whole-history work repeated in Razor getters. Use existing action components and AslDice/outcome display. |
| Measurement | Compare offer generation and allocations on a representative populated scenario before/after wiring, with two hexes and multiple owners/weapons. Bound retained memo sizes. No performance suite for this document-only work. |
| Outputs | Future Studio builds use port 6680 and `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`, with registered isolated outputs and normal cleanup. This analysis creates no build outputs. |

## 6. Verification and acceptance

Prioritize the Studio check once implementation is ready. Do not run a broad unit suite for presentation changes. Rules calculator changes need targeted behavior assertions, followed by the required wiring merge gate and CI. No tests are needed for these documents.

Use clean demos with rout/withdrawal and weapon opportunities in more than one hex, several weapons, and both sides where relevant. Do not mutate the user's ongoing game just to construct a demo.

| Check | Required result |
|---|---|
| Every phase | Only contextual operations appear, with correct side/step restrictions. DFF does not inherit DFPh configuration. AFPh has no routine management panel. Specialized fire and scrounging remain reachable. |
| Scope | Select either of two eligible hexes, release scope, and select a non-applicable hex. Destination-picker ownership wins over source rescoping. Full Location/vehicle boundaries remain enforced. |
| Recovery | Unattended and friendly broken-holder paths; insufficient MF versus the valid zero-MF case; failed-repeat limits; pending reaction restrictions; recipient changes clear stale previews. No arbitrary any-phase Recover action. |
| Transfer | Allowed participant pair, refused recipient, recovered-this-phase restriction, closed APh window and a supported PRC case. Direct command and UI agree. |
| Repair | Captured/recaptured status, missing repair data, prior action and attempt limits; a holder permitted to repair a second weapon can do so. Distinguish weapon and unit budgets. Vehicle repair still works. |
| Configuration | Current state changes the verb; relevant non-German or mortar capability where supported; malfunctioned weapon still reachable; previous use and unit capacity enforced; night counter/use behavior follows E1.89 wiring. No fire target required. |
| Rout | Required best load, alternative valid combinations, frozen load after rout starts, and no independent drop path that evades A10.4. |
| CC | No generic SW panel. Rules-approved voluntary/mandatory withdrawal and each supported withdrawal kind; permitted drops and destinations update together; cancellation has no mutation. A second CC Location is not blocked merely because the first resolved. Both sides still declare before applicable resolution. |
| Privacy and consistency | Owner/opponent/adjudicator views, hidden-enemy non-disclosure, unknown facts, stale stream/draft and direct command checks. Automatic transfers/losses still project correctly. |
| Historical behavior | Existing demos/replays remain on their recorded reading; all intended new behavior differences are named and reviewed in the wiring gate. |

## 7. Review record

PR 96 review correction: preserve the established APh drop window, before the unit advances. Add the recorded Advance fact to the shared Rules verdict so the page and command agree. This supersedes the earlier implementation review that widened that window.

**RSW.1, amendment to R13.5, approved by the user 2026-10-31:** A4.43 (PDF p. 50) ties the CCPh drop to withdrawing from Melee; A11.21 (PDF p. 73) limits the retained load. New commands offer equipment choices within a valid withdrawal, not a standalone CCPh drop. Equipment is dropped with the withdrawal commit; cancelling the draft drops nothing. Older recorded events retain their recorded reading. The user's approval was: "Approve withdrawal-only CCPh drops".

Self-review compared this proposal with the analysis, main's source and the cited PDF passages. It corrected a DFPh ASOP reference, separated contextual from routine dropping, preserved existing CC support, made configuration reachable without a firing target, and retained exceptional recovery as an explicit capability rather than a phase-toggle workaround. It also separated UI readiness, Rules applicability and implementation support, made sequence facts and replay versioning prerequisites instead of assuming they already exist, and clarified that weapon scope cannot override a parent workflow's valid origin scope.

The proposal was approved and implemented under the combined Rules/caller exception. All eight phase demos were reviewed and approved by the user. The later instruction pauses merging PR 96, even after CI passes. No auto-merge is authorized. The Studio Demo document records implementation and release evidence.

### PR 96 cache and contract corrections

The generator declares unknown values only for nullable facts. Phase is a public Choice; transfer participants are self-concerning Owner Unit facts, and their dependent facts concern the appropriate participant. Configuration and repair receive unit kind and printed weapon facts, not caller-supplied capacity or type permission. Recovery and Transfer return structured readiness. Legacy helper answers remain available, and the new calculators call those helpers for the common decisions.

The page owns one latest-state/latest-view cache for equipment locations and duplicate-label ordinals. Withdrawal choices share that lifetime; per-unit load text keeps only the latest drop selection. The workflow component refreshes on a new state snapshot, context, selection or explicit scope change, not every unrelated parent render. No cache is global, persists across views, or stores asynchronous work. Unknown load results are cached for that snapshot too. Stable keyed details elements retain their browser open state. Committed outcomes survive candidate-list removal.

A7.353 (PDF p. 56) keeps earlier First Fire uses separate from the current Final Fire allowance. The restored-inherent verdict preserves the actual First Fire marker so the complete fire calculator still halves FP and enforces adjacency. Current full SW capability still prevents inherent FP, and configuration still consumes the configured weapon's entire ROF. Null-version fire records keep their old reconstruction.
