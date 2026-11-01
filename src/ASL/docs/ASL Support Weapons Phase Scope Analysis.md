# Support Weapons: phase scope analysis

Status: proposal for approval, 2026-10-31. No application or Rules implementation is authorized by this analysis.

Base: `origin/main` at `7b5655ac7c2c433a524803b1ccd3307ae38db0cf`, fetched before creating branch `codex/support-weapons-phase-scope`. Worktree: `C:/Users/dland/.codex/worktrees/support-weapons-phase-scope/LimboDancer.MCP`.

## 1. Recommendation

Replace the generic Support weapons form with contextual operations. Weapon use belongs with the action that uses it; possession and configuration changes belong with the relevant unit, phase step, or withdrawal. A phase alone is insufficient to establish eligibility.

Rules must decide timing, participants, possession, costs, action limits and outcomes. Play gathers the facts and creates events. The page presents viewer-safe offers and collects choices. The same Rules checks must govern the offer, proposal and confirmation, with the projector using the same current reading when verifying new records.

Three kinds of opportunity are needed:

1. Routine phase operations, such as Rally-phase repair or transfer.
2. Choices within another action, such as the load kept during rout or weapons dropped for withdrawal.
3. Event-triggered opportunities, notably the special immediate SMC recovery in A4.44. These can occur outside the usual recovery phases and cannot be expressed by a static phase whitelist.

See the [proposed design](<Designs/ASL Support Weapons Phase Scope Proposed Design.md>) for presentation, contracts, delivery boundaries and verification.

## 2. Evidence and limits

Source: local `eASLRB_v3_01.pdf`, all 716 PDF pages searched for recovery, transfer, dropping, repair, dismantling and portage/withdrawal references. The search identifies related rules; it does not constitute a complete audit of every nationality, terrain or vehicle exception. The following controlling passages were read directly from extracted PDF pages. Page numbers are physical PDF pages.

| Subject | Rule and PDF page | Consequence for this design |
|---|---|---|
| Rally action types | A3.1, p. 47; ASOP 1, p. 678 | Both sides act in Rally. The one-action-type restriction is not a blanket one-weapon limit. Recovery is a start-of-RPh activity; repair and transfer are listed during RPh. Exceptions and sequencing need Rules decisions. |
| Possession, portage and transfer | A4.4, A4.42, A4.43, A4.431, p. 50 | Holding, carrying and firing are different. Dropping during movement does not restore portage already spent. Transfer has participant, Location and timing restrictions, plus terrain and vehicle exceptions. Sub-unit creation has its own transfer context. |
| Recovery | A4.44, p. 50 | Distinguish unattended recovery, SMC recovery from a friendly broken holder, and immediate SMC recovery following surrender, elimination or departure by rout. The latter is allowed regardless of phase. Routine MPh unattended recovery costs a MF; the broken-holder SMC case does not. |
| Repair | A9.72, p. 65 | Good Order, capacity to use weapons, printed repair data, capture/recapture and Gun-crew restrictions matter. Multiple repair attempts on different weapons can be permitted. |
| Deliberate damage | A9.73, p. 65 | Deliberate malfunction/destruction is a PFPh/DFPh alternative to firing, subject to firing eligibility and use limits. It is not ordinary Drop. |
| Assembly and dismantling | A9.8, p. 65; C9.2, p. 179 | This is not limited to the German MMG. Relevant weapon type, counter availability, previous use, capacity, malfunction and capture matter. A German dm MMG/HMG can fire as an LMG. A 76-82mm mortar has a special dm representation and portage cost. |
| Rout loads | A10.4, p. 66 | Broken units must retain a permitted maximum load, not freely abandon everything. No recovery or abandonment after starting rout. |
| CC use and withdrawal | A11.13/.16, p. 72; A11.21, p. 73; ASOP 8.15B, p. 681 | SW/ordnance counters cannot be used in CC attacks. Weapon losses after CC are resolution effects. Withdrawal carries no more than IPC; drop choices belong with that declaration. The sequence is per CC Location, not one global start-of-CC flag. Broken units' required withdrawal must be supported. |
| Vehicle passengers/riders | D6.31, p. 205; A4.431, p. 50 | Same hex is insufficient. The vehicle carrying the weapon and participants, and Motion, affect transfer/recovery. |
| Night configuration | E1.89, p. 226 | Dismantling/assembly consumes use but does not place the usual fire counter or create a Gunflash at night. |
| Phase placement | ASOP 2.22A and 3.22A/3.32A, p. 679; 4.21D and 6, p. 680; 7.11A and 8, p. 681 | Distinguish Defensive First Fire during MPh from the separate DFPh. Place transfer, recovery, configuration and withdrawal at their proper steps. |

This analysis covers the phase-facing weapon workflows and their dependencies. It does not declare all SW rules implemented. Fire, ordnance, demolition charges, radio/OBA, vehicle armament and scrounging retain their specialized workflows. Guns share several cited rules but are not interchangeable with the page's current SW list.

## 3. What the application does today

Paths below are relative to `src/ASL/`.

| Site | Current responsibility and finding |
|---|---|
| `LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor`: `SupportWeaponsNow`, `SupportWeapons`, `SupportWeaponUnits` | Caches weapons and four aggregate offer flags per state/view. Lists active visible weapons on the map, excluding Guns, manned and towed equipment. Recipient candidates are nearby visible non-vehicle units, not complete action-specific eligibility results. Passenger/rider cases need separate fact gathering. |
| `Components/Play/SupportWeaponActionPanel.razor` in MapStudio | One weapon selector, one ambiguous “Recovering or receiving unit” selector, four possible buttons, and long cross-phase help. Recipient input appears even for Drop or configuration. `Held` disables recovery outright, hiding the broken-holder SMC case that the planner supports. |
| `LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs`: `SupportWeaponActions` | Calls Rules phase helpers but combines availability across all supplied weapons. A flag true for one weapon can show an action beside another. Its CC Drop condition checks enemy presence, not a withdrawal declaration. |
| Same file: `PlanTransfer`, `PlanDrop`, `RecoverPhaseBar`, `PlanRecover`, `PlanDismantle` | Mixes fact gathering and Rules calls with decisions in Play: phase windows, participants, prior actions, possession, recovery origin, and costs. UI and command checks differ. `MayDismantle` checks type and phase, not the complete command precheck. |
| `Play.razor`: `RepairChoices`, `RepairReady`; `GamePlanner.Rally.cs`: `PlanRepair` | SW choices include possessed malfunctioned equipment, but SW readiness is just a nonempty selection. Vehicle repair already uses a richer refusal path. SW repair uses Rules helpers, including a legacy “Good Order as planned” check based only on the Broken fact. Full repair eligibility and use accounting require an audit. |
| `Play.razor`: `WithdrawalUnits`, `DropChoices`; `CloseCombatWithdrawals.razor`; `GamePlanner.CloseCombat.cs`, `GamePlanner.Withdrawal.cs` | Contextual CC drop support already exists, including selected drops, load-sensitive destinations and validation of possession. The generic panel is an additional route. The page itself decides which candidates/drop controls to expose; moving those decisions into Rules is part of the work. |
| `GamePlanner.Rout.cs`, `RoutLoadOf`, Rout components | Retained-load choices already belong to the rout action. Preserve that integration and Rules' best-load calculation. Do not replace it with independent drop buttons. |
| `LimboDancer.Domains.Asl.Rules/ScenarioA1SupportWeaponRules.cs` | Existing phase, recovery modifier/cost, bypass and rout-load helpers are useful, but are not a complete per-operation eligibility contract. |
| Rules: `ScenarioA1FireEligibility`, `ScenarioA1RallyRules`, `ScenarioA1Withdrawal`, passenger and fire-use calculators | Reuse the relevant calculators. Consolidate conflicting or incomplete decisions through Rules claims instead of adding another parallel policy in Studio. |
| `LimboDancer.Domains.Asl.Units/State/GameProjector.cs`: `AttemptRecovery`, `Transfer`, repair verification and dismantling-use projection | Recovery verification still contains phase/attempt/action decisions, and records its expenditure and attempts. `Transfer` is a general equipment state operation, not proof that a voluntary transfer was legal. Replay compatibility and action causality must survive tighter current eligibility. |

## 4. Specific corrections required

1. **Aggregate offers are not selected-action eligibility.** A phase-level true flag cannot enable a selected weapon, holder and recipient. Produce per-operation offers with matching command refusals.
2. **Held recovery is wrongly hidden.** A4.44's friendly broken-holder case is supported by `PlanRecover`, but disabled by the component and absent from the aggregate recovery offer. Its zero-MF case also reaches `RecoverPhaseBar` before `fromBroken` is identified, so the generic MF check needs auditing rather than simply enabling the button.
3. **CC has a second, insufficiently scoped drop route.** `PlanDrop` accepts an unbroken holder while `state.CloseCombats.Count == 0`, without withdrawal intent. The correct contextual path already accepts declared drops. Audit location-specific timing and broken withdrawal separately; do not route every withdrawing unit through the standalone command's unbroken restriction.
4. **Configuration copy is obsolete.** The help says German MMG only. `Dismantlable` already accepts non-Russian medium/heavy MG and light mortars of at least four PP. The full A9.8 text includes further cases that this predicate/counter representation does not establish. Say “Assemble” or “Dismantle” for the selected weapon; do not invent missing counters or capabilities.
5. **Repair needs an authoritative offer.** The page can offer weapons whose holder, captured status, prior actions or repair data cause later refusal. Audit attempt identity and unit capacity: a failed repair must not become repeatedly available, while a permitted repair of a second weapon must not be hidden by a blanket one-repair-per-unit rule.
6. **Exceptional recovery is missing from the current workflow.** The inventory identifies immediate SMC recovery as unfinished, and both the command and recovery projector constrain routine recovery to RPh/MPh. Adding an any-phase button would not implement its causal opportunity or limits.
7. **Detailed phase steps are missing from broad flags.** Start-of-RPh recovery and start-of-APh transfer need an agreed Rules reading and enough facts to distinguish open and closed windows. The absence of a step in current state must not be treated as permission.

## 5. Proposed phase scope

This table describes ordinary opportunities. Every row remains subject to Rules eligibility, side order, pending consequences and viewer authorization. The immediate-recovery exception is handled separately below.

| Phase/context | Action-pane placement |
|---|---|
| Setup | Possession and permitted starting dm state belong to setup, not play-time management. Missing setup capabilities remain explicit backlog work. |
| Rally, both sides | Separate compact Recover, Transfer and Repair sections, shown only with applicable candidates. Recovery first where its start window is open. Use actor-specific action-type and capacity accounting. Deployment and Recombination keep their own distribution choices. |
| Prep Fire, phasing side | Weapon firing stays beneath its owning unit in Fire/ordnance. Show an eligible Assemble or Dismantle operation under that weapon's configuration controls. A9.73 destructive alternatives need their own implementation before being offered. No routine transfer, drop or recovery panel. |
| Movement, moving side | Under the selected mover/current Location, offer eligible Drop and Recover operations with their actual costs and timing. Preserve active-mover and reaction windows. A unit can drop without moving; do not require a plotted route. No transfer between units as an ordinary MPh action. |
| Movement, defending side | Weapon fire remains in First Fire and reaction workflows. Do not expose DFPh configuration merely because Defensive First Fire is occurring. |
| Defensive Fire, defending side | Fire and weapon configuration as in PFPh, with DFPh-specific eligibility and previous use. No routine transfer/drop/recovery section. |
| Advancing Fire, phasing side | Weapon selection under Fire/ordnance, with movement/use restrictions decided by their calculators. No ordinary management section. Preserve supported German dm MG fire behavior. |
| Rout, both sides in sequence | Show “Carry” and “Leave behind” inside each selected router's preparation, with Rules-computed valid loads and feedback. Freeze preparation when rout starts. No generic Support weapons form. |
| Advance, phasing side | Weapon handling contains eligible Transfer and Drop operations for the selected units/Location. Transfer has its start-of-phase window; A4.43 permits dropping during the APh, including after advancement. Only a drop before advancing receives the start-of-phase unpossessed treatment. Keep carried-load consequences with destination readiness and preserve prior portage. Do not turn a normal transfer into an implicit part of an advance. |
| Close Combat | No generic Support weapons section or SW-counter attack selector (A11.13). Put permitted drop choices beneath a withdrawing unit, with carried PP and destination readiness. Treat ordinary Melee withdrawal, Ambush withdrawal and infiltration as distinct contexts requiring their own eligibility, rather than extending start-of-Melee permission automatically. Weapon loss/destruction remains a resolution effect, not a management button. |
| Triggered opportunity in any phase | A dedicated pending Recovery opportunity for an eligible SMC after the qualifying event, with explicit attempt/decline and expiry. This needs Rules and event/projector work before it can be offered. It is not a freely reusable phase action. |

## 6. Review conclusion and delivery boundary

The proposal was checked against the current source, PDF passages, existing inventory and backlog section 92. Review corrections incorporated: distinguish per-Location CC timing from a global flag; retain existing withdrawal load support; distinguish ordinary and exceptional recovery; preserve Rout's required load rather than importing it into CC; distinguish a Rules restriction from missing implementation/counter data; and avoid treating main's broad “built” inventory labels as proof of complete eligibility.

A presentation-only fix cannot satisfy the requested Rules ownership. The proposed delivery starts with Rules audits/contracts, then their callers and contextual UI. No claims were taken, no tests/builds were run, and no application behavior changed. Support for additional capabilities is explicitly listed in backlog section 94 rather than silently assumed. Approval of the proposed design is needed before implementation.
