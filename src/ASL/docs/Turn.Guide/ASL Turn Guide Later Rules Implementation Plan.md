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
| D1.2 | Durable Spotter designation, loss, replacement and reduced-squad successor history; governed designation/replacement (including setup predesignation and Defensive First Fire activation), deployment to a chosen half-squad, concealed/HIP disclosure, same-Location/same-target multi-mortar use, and legacy unknown handling. Replayed lifecycle and command refusals prove the contract. | D1.1 |
| D1.3 | Opportunity Fire declarations and required no-shot breakdown; remaining fire, artillery and vehicle/PRC dependency admissions. Audit existing execution, fill missing facts/commands, and connect reviewed phase topics without guessing eligibility. | D1.2 |
| D1.4 | Fire/Spotter package acceptance across profiles: initial/replacement Spotter, failed eligibility, Opportunity Fire carryover/breakdown, artillery and vehicle/PRC consequences, stale/disclosure/replay and live guide proof. | D1.3 |
| D2.1 | White SMOKE creation, dispersal/removal and distinct owner-relative cleanup; persisted type/size/source provenance and legacy unknown cases. | Accepted B5/U1 |
| D2.2 | Drift, Gust effects and cave WP opening sequence, including applicable morale/Flame consequences and authoritative substep evidence. | D2.1 |
| D2.3 | Vehicle, FFE and ambient dust: separate creation, shift, departure, wind, Motion and cleanup rules with source-specific histories and commands. | D2.2 |
| D2.4 | Atmosphere acceptance: ordering, interrupted consequences, historical/disclosed reads and phase close without generic cleanup or premature readiness. | D2.3 |
| D3.1 | Reconcile full G10 authoritative source; define eligibility, base duration, night/minimum adjustments and interruption/restart contracts. The local PDF supplement now resolves the source dependency. | Verified G10 supplement |
| D3.2 | Persist Gun/Mule/crew identities and midpoint loads/unloads; implement duration progress, reversal and restart using real participant roles. | D3.1 |
| D3.3 | Governed packing/unpacking and animal transport integrate assembly, containment, movement, prohibited unload terrain, vulnerability/cumulative Low Ammo and missing-Mule recovery, preserving legacy adjudication records. | D3.2 |
| D3.4 | Packing acceptance: night/minimum, midpoint/reversal, loss of eligibility, loading/transport, Mule-loss consequences, replay/disclosure and release timing in the live guide. | D3.3 |
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

Current ledger: 2/36 complete; D1.1 and D3.1 are complete; D1.2, D2.1 and D3.2 are in progress. The replacement marker/movement source dependency is recorded below. B5-D1 through B5-D9 all retain Pending rule support. The legacy baseline remains accepted independently of this new plan.

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


### D1.2 source clarification register

The local C9.3/.31 text remains the implementation basis. The [Perry Sez Q&A compilation, version 35](https://www.klasm.com/ASL/ErrataQA/QA/GS%20Perry%20Sez%20ch.pdf), pages 2 and 112-113, adds review requirements: deployment may transfer spotting to a chosen half-squad; setup predesignation takes effect at a firing-phase opening, including Defensive First Fire; concealed/HIP identities require private recording; vehicle mortar MA may use spotted fire; a berserk original does not by itself allow replacement. This is a third-party compilation of attributed rulings, not a new local rulebook edition. Its provenance must remain explicit.

These findings expand the existing D1.2/D1.3 acceptance scope without adding numbered tasks or treating them as implemented. Marker/movement effects and activation ordering must be reconciled before admitting designation commands. The narrower timing contract and observed-history increments do not certify these paths. All affected reference topics remain pending.


### D1.2 progress: shared mortar use

C9.31 now permits one Spotter to serve multiple mortars only when their recorded firing Location and target agree for the entire Player Turn. The state preserves each use across phase changes, clears it at the next Player Turn, and carries it to a retained reduction successor. Different levels are different Locations. Once mortars share the Spotter, changing the target is refused; earlier shots cannot be forgotten to permit another mortar. Phase SW-use records retain every mortar served.

New spotted-shot facts carry `SpotterUseVersion = 1`; legacy records with no version retain their earlier replay validation, and unsupported versions are rejected. This preserves committed history while applying the new contract to new commands. Missing historical Locations cannot justify multi-mortar sharing. Original designation, replacement and the additional source-clarification cases remain pending, so D1.2 is still In progress and B5-D1 remains pending.

Validation: all 1,128 Play tests pass with no skips, including 25 focused pass-9 cases. All 30 focused Spotter Rules tests pass. All 181 ASL.Turns tests pass with 823/823 lines and 470/470 branches covered (100%, no exclusions).


### D1.2 progress: governed original designation

`asl.game.designate-spotter` now records a bounded daytime original designation during the owner's PFPh/DFPh. ASL.Rules checks timing, known Good Order Personnel, ownership, range, one-hex mortar compatibility, TI, phase SW use and outstanding decisions. Unknown Good Order or legacy observed-only origin is not upgraded to proof. Setup/DFF activation, replacement, night, vehicle/PRC and wider profile cases remain pending. The ordinary play permission and confirmation gate apply; this is not an adjudication override or a new guided topic route.

Units records a typed `mortar-spotter-designated` event and explicit provenance separately from shot observations, applies the Rules fire marker and SW-use effects, and tracks the actual journal-relative Player Turn ordinal. Packed/aboard cases remain refused. A squad's shared spotting activity consumes one spotting use, preserving its permitted inherent fire. Recorded loss now bars new spotted fire until a governed replacement exists; rally or naming a different Spotter cannot erase that loss. Legacy unversioned firing records retain their replay behavior.

The read model and thin guide show explicit declarations before any shot without manufacturing a firing history. Declaration and phase-origin visibility are both required. Concealed mortar/holder or Spotter identities are recorded privately. Proposal reads do not write; stale confirmations, changed attempt inputs and duplicate original declarations are rejected. All 538 topics, 38 dependencies and nine pending packages remain represented. The Movement checklist, cross-phase label, walkthrough and saved preview now clarify that C9.3 requires elimination, break or capture; generic non-Good-Order status is insufficient.

Validation: the full 1,131-test Play suite and all 421 Units tests pass. The final disclosure adjustment also passes all 29 focused pass-9/registered-action cases. All 33 focused Spotter Rules tests and 24 Turn Guide component tests pass. All 182 ASL.Turns tests pass with 825/825 lines and 470/470 branches covered, without exclusions. D1.2 remains In progress, with replacement, setup/DFF activation, deployment selection and wider profile acceptance still required.


### D1.2 progress: chosen Deployment successor

The governed Deployment action now requires `spotterHalf` (first or second) for a squad with a retained assignment. The choice is bound before its NTC, including failed attempts, and successful lineage must name the matching produced HS. A Guard can make the same choice during its at-will Deployment. Missing/invalid choices fail before dice or writes; changed retries and altered successor evidence are refused. New choice-bearing records are owner/adjudicator-only so they cannot disclose a concealed or HIP Spotter assignment. Legacy lineage without a chosen successor stays unknown.

Units transfers each retained mortar assignment, phase spotting/SW-use evidence and Player Turn mortar-use history to the chosen Good Order HS. Existing movement restrictions carry through the split; deployment does not create fresh movement or firing permission. Loss history is not erased. The ordinary Deployment form and Guard buttons expose the backend choice. The guide's existing successor evidence remains an owner/adjudicator read, with withheld succession producing unknown evidence.

Validation: the full Play suite passed 1,134 tests before the final evidence/disclosure tightening. The final change passes 74 focused Play tests, including both HS choices, failed NTC, Guard succession, changed retries, altered lineage, legacy records, private history, continued fire and movement restrictions. All 421 Units tests, 45 focused Spotter Rules tests and 37 affected component/page tests pass. ASL.Turns remains 182 passing tests, 825/825 lines and 470/470 branches (100%, no exclusions). All 538 topics, 38 dependencies and nine pending packages remain represented. D1.2 is not complete.

Replacement source dependency: C9.3 and ASOP 3.11A establish replacement at the following owner's MPh opening. The reviewed local text and attributed Q&A do not settle the exact marker/movement effects to apply to that replacement designation. That execution path remains blocked on source clarification; no inferred Prep Fire marker or movement permission is admitted. Setup/DFF activation and wider-profile acceptance are also outstanding. Independent source-backed tasks may proceed under the execution-order policy while this point remains visible.


### D2.1 progress: original white SMOKE rule contract

`TurnWhiteSmokeRules` separates source, chemical, counter size/color, strength, owner, origin and applied-opening evidence. A24.5 establishes conventional/WP strength for an admitted ordnance/OBA placement; D13.31-.34 dispenser sources remain dispersed conventional smoke. Strength calculation is not placement permission. Unknown early-fire history does not justify full strength. Grenades, gray drift, Blaze, cave and Napalm sources are not silently treated as ordinary white counters.

A24.4 and ASOP 2.11A cleanup is assessed only at an explicit `PrepFireOpening` checkpoint for the owner. One immutable snapshot orders all existing dispersed-counter removals before full-counter dispersal. A receipt prevents a newly dispersed counter from being removed by repeated assessment in the same opening; a newly created PFPh counter also waits for a later opening. Inactive and unknown counters do not acquire invented expiration evidence. Wind/Gust, linked drift, WP/Napalm consequences, causal source creation, durable receipts and governed cleanup still require integration before D2.1 is complete.

Validation: 84 focused Rules tests pass, including the existing obligation/Spotter contracts. All 183 ASL.Turns tests pass with 825/825 lines and 470/470 branches (100%, no exclusions). The inventory regression preserves all seven atmosphere gaps, unclassified counters, nine packages and 538 topics. No atmosphere command or profile is promoted by this read-only rule contract.


### D3.1 acceptance: G10 source and packing rule contracts

The missing-source dependency is resolved by the locally available `eASLRB_v3_14.pdf`, SHA-256 `d2fedd572b5256d35a6ef4a787da72102211b4d595f090bea81a4c7ee107dfee`, physical pages 291-292 (printed G16-G17). Complete page renders were checked against column-ordered extraction. The filename says 3.14; the credits on physical page 5 print Version 3.13, January 2026. Both facts are preserved in `ASL Turn Guide Later Rules Source Register.json`. The PDF is not copied into the repository, and the accepted 3.01 source baseline is not silently replaced.

`TurnPackingRules` defines ordinary G10 participant/weapon/terrain/assembly eligibility, including 76-82mm mortars, other Guns/mortars allowed by a note or DYO, designated squad Mules and the qualified-MMC exception for listed SW. Unknown qualification remains NotEvaluated. It computes the M#-based period, one-Game-Turn minimum, night doubling and G10.7 effective M# 12. G10.11/.32 distinguishes interrupted crew activity from allowed unloaded-Gun handling. The existing lifecycle passes the printed G10.3 example: Turn 2 counts, interruption loses Turn 3, Turns 4-5 finish the period, and release waits for the first RPh of Turn 6. G10.33 reversal uses net elapsed complete Game Turns; involuntary unloading restarts the full ordinary period. The specific reversal formula uses elapsed time directly rather than doubling already elapsed night turns again.

Five packing-related source-review flags are now Reconciled, with execution support still Pending. This certifies the source/contract task only. D3.2 must persist actual Gun/Mule/crew roles, midpoint load state, exact opening evidence and reversal/restart records. D3.3 must integrate movement/transport, prohibited unload terrain, G10.4-.42 vulnerability, cumulative Low Ammo and missing-Mule recovery consequences. D3.4 must prove those paths in replay, disclosure, phase close and the live guide. Existing B5 adjudicated durations and assembly effects remain bounded legacy behavior.

Validation: 43 focused packing/work Rules tests and all 24 Turn Guide component tests pass. The component test proves that reconciled packing sources still show pending execution. All 183 ASL.Turns tests pass with 825/825 lines and 470/470 branches covered, without exclusions. Source reconciliation does not promote a packing command, remove any of the nine packages, or hide any of the 538 topics.


### D3.2 progress: packing period transitions (2026-10-28)

`TurnPackingPeriods` now owns a serializable G10.3/.31/.33 transition contract with distinct weapon, Mule and Personnel identities. Consecutive first/second RPh work boundaries credit complete Game Turns and detect odd- or even-length midpoints. A due midpoint must be resolved before the next boundary; its receipt preserves the physical load state. Full release waits for the following first RPh. Night uses the D3.1 duration contract.

Crew interruption retains prior complete Game Turns and any already completed physical load/unload, discards the interrupted Game Turn, and leaves the weapon and Mule Pack-TI. Its recorded boundary prevents resuming in that same Game Turn. Resume requires fresh eligibility at a later first RPh. Voluntary reversal retains physical load, starts the net-elapsed period and requires a new midpoint receipt. Repeated, skipped, future or contradictory boundary/load transitions are refused. G10.3/.31's printed example now runs through Turn 3 interruption, Turn 4 second-RPh unloading and Turn 6 first-RPh release.

This is the Rules transition layer, not a live command or proof that supplied serialized state is authoritative. D3.2 remains In progress: the host must bind these identities to actual pieces and location, prove each ASOP 1.13B occurrence from the journal, record versioned start/interruption/resume/reversal/midpoint/release events, and commit legal load and Pack-TI effects atomically. Midpoint terrain/CA choices, involuntary unloading and missing-Mule recovery require the D3.3 consequence path. The existing adjudicated work protocol is unchanged; its generic TI behavior is not certified as this G10 lifecycle. Disclosure, shared close obligations and live-guide acceptance still require integration. All nine packages and every pending reference option remain visible.

Validation: all 1,325 Rules tests pass with no skips. Seven new period tests exercise JSON round trips, the printed example, night and odd/even midpoints, reversal, interruption before/after unloading, terminal release, eligibility and altered history. ASL.Turns remains 183 passing tests with 825/825 lines and 470/470 branches covered (100%, no exclusions). No command or capability is promoted by this increment.
