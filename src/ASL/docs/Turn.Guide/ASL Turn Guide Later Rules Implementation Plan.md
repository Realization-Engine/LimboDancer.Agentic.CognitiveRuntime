# Turn Guide later Rules implementation plan

Date: 2026-10-27

## Scope and status

B5-D1 through B5-D9 are reinstated as active tracked requirements. **The remaining nine packages are explicitly deferred to later ASL.Rules passes.** Deferred describes their place after the accepted B5/U1 baseline, not removal from this plan or from the UI. The user has authorized independent implementation of this plan and commits as work completes. This supersedes the earlier statement that there is no next task; it does not reopen completed B5-R acceptance.

There are **36 numbered tasks**, four per retained package. The ledger below records completion; uncompleted tasks remain Pending or In progress. Dependencies below determine execution order; tasks within a package run in numeric order. A task may use multiple commits but does not acquire an unbounded list of unnamed follow-ups. New findings attach to an existing task, with a recorded scope change if they cannot fit. A blocked source or rule interpretation remains explicitly blocked, never counted as implemented. Each package remains pending until all four tasks meet their exit gates.

## Common implementation and acceptance contract

Each semantic task reconciles the local rulebook and ASOP, names the supported conditions and exceptions, puts verdicts in ASL.Rules, persists authoritative facts in Units, and uses governed Play commands. Do not invent an ASOP checkpoint from a phase label, migrate unknown legacy facts by guessing, or add a generic adjudication shortcut in place of missing rules. Existing source gaps stay visible until an authoritative source is supplied and reviewed.

Each integration task proves proposal/confirmation, stale rejection, replay/retry, interruption, consequence completion, phase-close parity and owner/opponent/adjudicator disclosure for its admitted cases. The UI consumes those results without a second rule engine. Every reference topic, dependency and deferred package remains visible. NotApplicable requires evidence; pending implementation cannot produce it.

Each package's fourth task is acceptance of its preceding scope, including negative cases, legacy records and a live consumer where commands are admitted. It is not permission to narrow away missing package requirements. ASL.Turns must retain 100% line and branch coverage without exclusions on every implementation commit; focused Rules, Units, Play and UI checks follow changed contracts. Record actual results, not inherited pass counts. No package becomes complete solely because tests for a smaller subset pass.

## Finite task register

| Task | Deliverable and specific exit evidence | Prerequisite |
|---|---|---|
| D1.1 | Rules-owned C9.3 original/replacement Spotter timing contract: original PFPh/DFPh ownership, loss reasons, next owner MPh opening, unknown evidence, and reduction-survivor exception. Source-backed boundary tests; no firing permission implied. | Accepted B5/U1 |
| D1.2 | Durable Spotter designation, loss, replacement and reduced-squad successor history; governed designation/replacement, same-Location/same-target multi-mortar use, and legacy unknown handling. Replayed lifecycle and command refusals prove the contract. | D1.1 |
| D1.3 | Opportunity Fire declarations and required no-shot breakdown; remaining fire, artillery and vehicle/PRC dependency admissions. Audit existing execution, fill missing facts/commands, and connect reviewed phase topics without guessing eligibility. | D1.2 |
| D1.4 | Fire/Spotter package acceptance across profiles: initial/replacement Spotter, failed eligibility, Opportunity Fire carryover/breakdown, artillery and vehicle/PRC consequences, stale/disclosure/replay and live guide proof. | D1.3 |
| D2.1 | White SMOKE creation, dispersal/removal and distinct owner-relative cleanup; persisted type/size/source provenance and legacy unknown cases. | Accepted B5/U1 |
| D2.2 | Drift, Gust effects and cave WP opening sequence, including applicable morale/Flame consequences and authoritative substep evidence. | D2.1 |
| D2.3 | Vehicle, FFE and ambient dust: separate creation, shift, departure, wind, Motion and cleanup rules with source-specific histories and commands. | D2.2 |
| D2.4 | Atmosphere acceptance: ordering, interrupted consequences, historical/disclosed reads and phase close without generic cleanup or premature readiness. | D2.3 |
| D3.1 | Reconcile full G10 authoritative source; define eligibility, base duration, night/minimum adjustments and interruption/restart contracts. Missing Chapter G source is a blocking dependency, not an inferred rule. | Authoritative G10 source |
| D3.2 | Persist Gun/Mule/crew identities and midpoint loads/unloads; implement duration progress, reversal and restart using real participant roles. | D3.1 |
| D3.3 | Governed packing/unpacking and animal transport integrate assembly, containment, movement and completion effects, preserving legacy adjudication records. | D3.2 |
| D3.4 | Packing acceptance: night/minimum, midpoint/reversal, loss of eligibility, loading/transport, replay/disclosure and release timing in the live guide. | D3.3 |
| D4.1 | Rule-owned E10 eligibility, caliber/barrel compatibility, Accessible Locations, Immobility and participant/capacity contracts. | Accepted B5/U1 |
| D4.2 | Simultaneous recipients, Ammo Dump setup/use/replenishment and SSR/DYO facts; integrate transfer/offload occupant and towed-equipment disposition. | D4.1 |
| D4.3 | Automatic Low Ammo and D3.71/B# behavior; supply movement restrictions, immobilization, capture/destruction/explosion and their mandatory consequences. | D4.2 |
| D4.4 | Ammunition acceptance: bounded effects versus automatic rules, shared resources, firing benefits/MP restrictions, durable depletion/restoration and closure. | D4.3 |
| D5.1 | Source-backed tunnel entry/transit/emergence identities, timing, eligibility, disclosure and interruption contracts. | Accepted B5/U1 |
| D5.2 | Governed tunnel lifecycle and consequences across turns, with hidden-history and phase-close evidence. | D5.1 |
| D5.3 | Airborne arrival/landing facts and commands, glider/paratroop contents and required consequences, integrated with opening windows. | D5.2; required airborne sources |
| D5.4 | Tunnel/airborne acceptance: hidden emergence, delayed/lost arrivals, landing consequences, stale/replay and live phase obligations. | D5.3 |
| D6.1 | Remaining Close Combat declaration and hero creation/use contracts, ownership, order and carryover inventory reconciled against actual engine support. | Accepted B5/U1 |
| D6.2 | Governed missing CC declarations, required decisions and hero creation/use with durable identity and consequences. | D6.1 |
| D6.3 | CC cross-phase and turn-boundary integration, retaining unresolved combat/consequences before cleanup and exposing supported guide routes. | D6.2 |
| D6.4 | CC/hero acceptance: both sides, special declarations, creation/loss, interrupted resolutions and exact cleanup/replay/disclosure. | D6.3 |
| D7.1 | Explicit water/amphibious, aerial, night/weather and module profiles; source and applicability inventory bound to existing topic IDs. Missing profiles stay unknown. | Accepted B5/U1 |
| D7.2 | Water/amphibious and aerial missing facts/commands, transport/arrival/fire consequences, and profile-specific timing. | D7.1; D5.3 |
| D7.3 | Night/weather and module-specific remaining execution/cleanup, preserving distinct exceptions and source gaps. | D7.2; D2.3 |
| D7.4 | Profile acceptance: applicability/legacy/hidden-world matrix, cross-profile replay and live guide proof for water, aerial, night and module contexts. | D7.3 |
| D8.1 | Authoritative Trip Flare activation, origin and expiry facts plus governed command integration for the existing Rules timing predicates. | Accepted B5/U1 |
| D8.2 | Non-Flame Clearance eligibility, attempts, outcomes and mandatory cleanup ordering through governed commands; preserve other Clearance exceptions as explicit scope until reconciled. | D8.1 |
| D8.3 | Prove and integrate clearance-before-status and concealment-before-illumination phase-close order, including interrupted consequences and late/unknown origins. | D8.2; relevant D7 night facts |
| D8.4 | Trip Flare/Clearance acceptance: exact activation/release windows, failed attempts, pending consequences, history/disclosure/replay and live phase close. | D8.3 |
| D9.1 | Freeze a source-to-command admission matrix for every remaining core Rally, movement, Rout, terrain/hazard, status, possession, prisoner and turn-boundary topic; no residual topic may lack an owner. | Accepted B5/U1 |
| D9.2 | Implement missing core eligibility/facts and governed command paths identified in D9.1, including mandatory hazard, prisoner and casualty continuations. Existing engine code is audited before admission. | D9.1; family contracts above where linked |
| D9.3 | Connect remaining core topics and cross-phase obligations to typed guide decisions and shared close assessment; retain any source-blocked path visibly pending. | D9.2 |
| D9.4 | Final all-package acceptance: every topic and dependency has evidence or an explicitly unresolved source block; all nine package gates pass before claiming the all-Passes implementation complete. | D1.4-D8.4; D9.3 |

## Execution order and current ledger

Start D1.1, then D1.2-D1.4. Continue D2, D3, D4, D5 and D6 in order; then D7, D8 and D9 respecting the table's dependencies. D9.1 may run early to supply the core-topic matrix. If an authoritative source blocks a task, record the exact missing source and continue independent ready tasks. Do not manufacture rules to clear the queue.

Current ledger: 1/36 complete; D1.1 is complete and D1.2 is in progress. B5-D1 through B5-D9 all retain Pending rule support. The legacy baseline remains accepted independently of this new plan.

### D1.1 acceptance

`TurnSpotterTimingRules` owns C9.3 timing: original designation in the owning side's PFPh/DFPh; replacement only at an owner MPh opening following recorded elimination, break or capture; and retention by a surviving Good Order half-squad. Unknown origin/loss/successor facts do not open a window. Invalid or future positions are rejected. A current MPh label without the opening checkpoint is insufficient, and a loss during that MPh cannot use the same opening.

Source: local Chapter C 9.3 and 9.31, with replacement ASOP 3.11A from the reconciled Movement catalog. Candidate eligibility, movement, LOS, fire counters, Opportunity Fire and actual designation commands belong to D1.2/D1.3. No live command or capability is promoted by this read-only contract.

Validation: 18 focused Rules tests pass. All 180 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered, without exclusions. An inventory regression proves that the new contract retains the pending Spotter command, B5-D1 package and all 538 reference topics.

### D1.2 progress: durable observed loss

The first D1.2 increment preserves observed assignment losses in replay, including the causing event and phase occurrence. A later rally cannot erase a recorded break and make the guide call that old assignment retained. The disclosed fire history now uses both current state and durable loss evidence; a withheld loss yields NotEvaluated rather than leaking it or suggesting retention. Historical reads before the loss remain unchanged. Existing shot records remain observations, not manufactured original designations.

Validation: 20 focused Play regressions and 23 focused Rules tests pass. All 180 ASL.Turns tests pass at 821/821 lines and 468/468 branches, without exclusions. Round-trip replay, rally, historical reads, owner/adjudicator views and withheld loss are covered across all admitted fire-history profiles.

D1.2 remains In progress. Explicit designation/replacement events and commands, reduction successor inheritance, multi-mortar use and their acceptance gates are still required. No Spotter command capability is promoted by this increment; all nine packages remain pending.


### D1.2 progress: reduced Spotter successor

A recorded squad reduction now transfers a retained observed spotting assignment to its surviving Good Order half-squad. The journal-derived succession carries event provenance, preserves phase SW use, and permits the successor to continue an otherwise legal ROF shot. Earlier loss prevents inheritance. A broken survivor records the actual break; an unknown survivor remains NotEvaluated. Mere lineage consumption never invents elimination or a replacement window.

The guide preserves the first observed assignment and names its disclosed half-squad successor. Hidden lineage yields NotEvaluated and omits the successor identity. Original designation and replacement commands remain pending; this exception grants no new firing permission and does not complete D1.2.

Validation: 24 focused Play tests, 29 focused Rules tests and 24 Turn Guide component tests pass. All 181 ASL.Turns tests pass at 823/823 lines and 470/470 branches (100%, no exclusions). Coverage includes prior loss, broken/unknown survivors, hidden lineage, serialized replay and continued firing.
