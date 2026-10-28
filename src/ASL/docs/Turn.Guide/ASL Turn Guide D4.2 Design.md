# D4.2: ammunition sources, shared recipients, dumps and relocation

Date: 2026-10-28. Status: **design for review, not approved for implementation**.

Prepared against main `747fc9dc154bb024ab5c2cbe0cbe7cfc90124271`. Preparation read source and code only; no build, test, Studio session, game modification or implementation change was made. This document is not evidence of working functionality.

## 1. Scope and approval boundary

D4.2 in the [later Rules plan](<ASL Turn Guide Later Rules Implementation Plan.md>) follows the completed D4.1 eligibility contract. It adds durable simultaneous-recipient assignments, Ammo Dump setup and replenishment, source-backed SSR/DYO facts, and transfer/offload disposition. B5-D4 stays Pending until D4.4 passes. All nine packages and every reference topic remain visible.

The remaining nine packages are explicitly deferred to later ASL.Rules passes. D4.2 is a bounded implementation task within that work, not acceptance of all E10 or an implementation of the entire existing Pass 175 inventory.

User rulings on 2026-10-28 supersede older Turn Guide validation and autonomy wording:

- Only changed-behavior test methods run during implementation. Implementation commits may follow a successful Studio check and those relevant unit tests.
- Measure and enforce 100% ASL.Turns line and branch coverage, without exclusions, at the gate and CI. Do not run the whole project for every increment.
- Preparation and saving this design are authorized. Coding is not yet authorized. Earlier independent-work instructions do not restart the halted work or its automation.

### Explicit boundaries and dependencies

| Item | D4.2 treatment | Remaining owner |
|---|---|---|
| Source identities, authorization, complete shared assignments, dumps and replenishment | Implement the approved routes, with unknown facts distinguished from negative facts | D4.2 |
| Transfer/offload capacity and occupant/towed-equipment disposition | Implement after the rulings in section 5; unresolved disposition prevents that route's acceptance | D4.2 |
| Ordinary fire's automatic circled-B#/Low Ammo production and B12 benefit | Preserve existing behavior; expose source eligibility but do not call a shot integrated | D4.3, with Pass 105 inventory dependency |
| Mandatory movement restrictions produced by a newly admitted relocation | Must accompany that route, not be silently deferred while movement remains legal | D4.2 prerequisite pulled from D4.3, subject to approval |
| General capture/destruction, burning-supply explosion and broader movement/fire interactions | Remain visibly pending. New routes must invalidate lost sources and must not certify these consequences complete | D4.3 |
| Chinese/French Gun exceptions, special vehicle-note armament and national profiles | Persist exact profile/source requirements; unsupported execution stays Pending, not NotApplicable | D4.3/D4.4 and D7; no general Gun or vehicle catalog expansion implied |
| Complete package, cross-profile and live acceptance | D4.2 provides its evidence, not a package promotion | D4.4 |

These boundaries must be reflected in the backlog and rule inventory during implementation. Section 76's work-participant eligibility item is directly reached: shared supply must replace the blanket overlapping-work refusal with a Rules verdict. Unrelated Spotter, packing and display-label backlog items remain untouched.

## 2. Source reconciliation

Authoritative text inspected: local eASLRB 3.01 at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`. The existing source register's 3.14 supplement was not re-read for this preparation; it is not silently substituted for 3.01.

All page numbers below are one-based physical PDF pages. Text was extracted directly from the PDF using pypdf. No new visual/table-render verification is claimed. Before implementing the circled-B# distinction, visually verify the counter notation and table against the PDF; ordinary extracted `B#` loses the circle.

| Source and page | Finding and design consequence |
|---|---|
| E10.1-.12, p. 244 | Source authorization comes from SSR or the applicable Chapter H/DYO route. Default vehicle capacity is 15 PP, or 20 PP for the specified caliber threshold; named vehicle exceptions must remain distinct. The supply counter makes usable Passenger capacity zero. |
| E10.2-.21, p. 244 | Up to two identically armed recipients; each needs same/Accessible Location and CE for the benefit. Prep Fire use affects later movement; Bounding (First) Fire cannot use the benefit. This is not a generic two-slot UI limitation. |
| E10.3-.31, p. 244 | Each recipient accumulates its own complete Game Turns; required durations are 1/2/3 for original circled B# at most 9/10/11. CE, TI and participant activity determine interruption. Completion restores applicable depleted ammunition and requires an Ammunition DR for each completed recipient. Original 12 depletes the source. |
| E10.4, p. 244 | Four complete Game Turns for relocation. Offload creates a same-Location dump; transfer names an Immobile source and a qualifying same/Accessible destination. An Immobilized source flips to a wreck at completion. Becoming or ceasing to be an Ammo Vehicle bars MP expenditure during that turn. |
| E10.6, p. 244 | SSR or DYO can create a stationary dump. Apply supply rules to a stationary Location, not to an invented vehicle or crew. |
| ASOP 8.43B, p. 681 | Ordinary TI removal has a replenishment exception. Check replenishment at this checkpoint; do not erase work at generic CCPh cleanup or infer the checkpoint merely from a phase label. |
| H1.49, p. 332; H1.6 and table note 15, p. 334 | Ammo Vehicle DYO cost is twice normal BPV; dump cost is 25. Persist purchase authorization and provenance; a text label saying DYO is not purchase evidence. A full DYO shopping system is outside this task. |
| D.7, p. 192; Accessible glossary, p. 11 | Reuse D4.1's full Immobile predicate and Rules accessibility decision. Raw board/level, adjacency and advance facts come from the host. Same hex or adjacency alone is not enough. These locators are inherited from D4.1; p. 192 was inspected again, glossary p. 11 must be rechecked when wiring accessibility. |
| C10.1-.13, p. 180; D8.32, p. 209 | Towing has its own capacity, unhooking, timing and destruction rules. An Immobile vehicle can be unhooked under the normal procedure. Do not delete held equipment merely because the supply moves. |
| D5.1, p. 203; D5.4-.6, p. 204; D6.2-.5, p. 205; D10.1, p. 210 | Distinguish Inherent Driver, Inherent/temporary crew, Passengers, Riders and held equipment. Ordinary abandonment/unloading has its own timing and costs; survival follows elimination rules. E10.4's administrative wreck needs an explicit ruling before reusing a combat-wreck path. |
| E1.89, p. 226 | Ammo benefit does not cause a Gunflash; night omits the usual fire marker. Persist the underlying use independently of whether a marker is shown. Night execution remains a named profile dependency. |

### Whole-rulebook scan and exceptions

Every page was searched for Ammo Vehicle, Ammo Dump, Ammo Supply, E10.1-.6 and H1.49. Matches occurred on pp. 7, 12, 25, 34, 37, 42, 226, 244, 292, 332, 334, 439, 468, 470, 483, 494, 502, 599, 681 and 710. The index and duplicated table hits establish cross-references, not that every rule on those pages was reviewed. Line-wrapped or differently named references can require further searches when implementing a named profile.

Relevant passages read on their pages include:

- p. 439: the Italian SMV 90/53 supply-vehicle substitution modifies the designated SMV 47/32's armament and crew-survival characteristics.
- pp. 468 and 470: Chinese ordnance includes Guns whose Low Ammo status can be negated by SSR, vehicle or dump. D4.1's `IsVehicle` requirement cannot be advertised as universal E10 coverage.
- p. 483: the French Autocanon supply is necessary for functioning armament, with named vehicles and an MP exception.
- p. 494: French ordnance note G has date-dependent Low Ammo status.
- p. 502: the Polish Dion-Bouton also needs supply to fire at all.

The A-to-E inventory still calls E10 unbuilt under Pass 175 despite the bounded Turn Guide work. Update each reached row to describe its actual contract/command coverage and remaining dependency; do not replace all rows with Built. The circled-B# catalog/automatic Low Ammo gap remains linked to Pass 105 and D4.3.

## 3. Current implementation and concrete gaps

| Existing seam | Observed gap |
|---|---|
| `TurnAmmoEligibilityRules` | Checks supplied participant facts, compatibility, same/Accessible and up to two recipients. It does not establish scenario authorization, live assignment inventory or source provenance. |
| `TurnAmmoSupply` | Holds ID, vehicle or dump Location, owner and availability. No ammunition identity, source authorization or durable recipient assignment history. Preserve its legacy serialization contract rather than silently treating missing fields as known. |
| `GameProjector.TurnWork.cs` | Refuses a participant already in any open work and checks TI/ownership/capture outside Rules. One shared vehicle cannot currently serve two independent work orders. TI release is participant-wide. |
| `GameProjector.WorkEffects.cs` | Replenishment requires a supply vehicle and same Location. Relocation rejects all occupants and held equipment; both transfer and offload use the same restricted Immobile predicate. Completion invokes the general wreck helper. |
| `GameProjector.Wreck` | Eliminates a towed Gun and drops held equipment. This is not sufficient evidence that it is correct for the E10.4 completion cause, and does not provide an E10-specific occupant decision. |
| `GamePlanner.TurnWork.cs` | Generic permissioned adjudication with one subject and one completion roll. Retain this as its recorded legacy contract; do not promote it by changing prose. |
| `TurnWorkDisclosure` / host capability bounds | Report adjudicated effects as Pending automation. New governed evidence needs separate capability bounds and disclosure-safe results. |
| ASL CI | ASL.Turns.Tests is in the test matrix and has coverlet.collector, but the inspected test step does not collect or enforce coverage. Add gate/CI coverage enforcement as delivery work, not a claimed existing guarantee. |

## 4. Proposed component contracts

### Rules owns every decision

Add focused files with the `ScenarioA1` prefix, proposed names `ScenarioA1AmmoSupplyModels.cs`, `ScenarioA1AmmoSupplyRules.cs` and `ScenarioA1AmmoSupplyLifecycle.cs`. Keep existing public names as forwards where needed. No new project dependency is expected.

Facts are Rules records of primitive values and nullable facts: source ID/type/owner, exact ammunition caliber/barrel, authorization kind and reference, actual vehicle definition/normal capacity, recipients with roles and posture, raw Location relationships, complete assignment inventory, current phase occurrence/checkpoint, observed participant activity, occupants/equipment and pending consequences. A Rules fact-reader interface is appropriate only if a real accessibility search needs it; it must not expose GameState or a map.

Rules returns eligibility plus reasons, missing-fact requirements, legal transitions, work/TI ownership changes, required rolls, completion effects, movement restrictions and phase-close obligations. In particular, Rules decides:

- Source setup and destination eligibility, including capacity and specified-vehicle exceptions.
- Whether proposed and existing assignments can coexist; no host-side recipient-count or overlap rule.
- Interruption scope, retained complete turns and TI release for each participant.
- Whether a dump, vehicle or recipient can perform the requested use at this checkpoint.
- Exact disposition for each participant/equipment identity and whether a consequence is still unresolved.
- Whether phase close is refused. ASL.Turns and the UI display this verdict unchanged.

A permission check and a revision check remain host responsibilities. An eligibility verdict is not permission to confirm an action.

### Durable facts and replay

Use a new versioned event contract for governed ammunition work, leaving all existing work event types and recorded fact record shapes intact. Proposed event families: source established, assignment declared/released, interval observed, completion resolved and supply relocated. Final event names/package IDs are fixed with the first implementation increment.

Persist source provenance and stable counter identity separately from its vehicle or dump Location. A transfer moves one source, never clones it. Store recipient-specific progress and each assignment's causal origin. Store references to consumed system dice, not user-entered outcomes. A completion batch records the snapshot and ordered recipient decisions needed to reproduce the approved simultaneous-completion ruling.

Projectors gather raw facts, call the same calculators used by Play, reject invalid event transitions, and apply the returned effects atomically. No partial restoration, capacity change, source relocation or TI removal may survive a failed append. Historical reads use the facts effective at that revision. Retries cannot draw a second accepted roll, duplicate a supply, or restore ammunition twice.

Legacy work remains readable and retains its historical verdicts. Missing authorization, ammunition identity or checkpoint evidence stays unknown. Do not import old progress into the new contract by assuming it was eligible. Migration of boundary checks preserves old refusal text/order; changed new-route verdicts are intentional and separately tested.

### Play, guidance and UI

Play reads scenario/catalog provenance, participants and map facts at the proposed revision, then calls Rules. Confirmation rechecks the current revision and facts through the existing governed command path. Source setup requires the established scenario-authoring authority; play permission cannot invent an SSR or purchase. Exact permission mapping must follow the existing setup command inventory during implementation, not a new generic override.

Expose source setup, assign/release recipient, start/resume work and resolve due completion through headless commands first. Thin Studio controls show eligible choices, missing facts and Rules refusals. Keep the adjudicated baseline explicitly separate from governed capabilities.

Each guide entry distinguishes: reference exists; profile/source is unknown; Rules contract exists; host execution is admitted; obligation is due; completion consequences remain. Unsupported profiles stay visible. The owner may see authorized details; opponents receive only permitted public consequences. Do not expose hidden assignment counts, private SSR text, IDs or roll provenance through a refusal or phase-close list. Review disclosures through the existing Rules disclosure mechanisms; add a Rules verdict where new ASL disclosure decisions are needed.

## 5. Proposed rulings requiring review

These are proposals, not implemented rules or assertions that the source explicitly answers every detail.

| ID | Analysis | Proposed decision |
|---|---|---|
| D42-R1 | E10.12 generally restricts counter removal to Immobile/Depleted, while E10.4 separately permits offloading and explicitly attaches Immobile to transfer. Existing code restricts both. | Treat E10.4 as the specific four-turn offload procedure, allowing a mobile source to offload while obeying TI/no-MP conditions. Require full D.7 Immobility for vehicle-to-vehicle transfer. Approve this interpretation before changing new-route admission. |
| D42-R2 | E10.3 allows two simultaneous recipients and requires a DR after each completes; it does not explicitly order two periods ending at the same checkpoint if one DR is 12. Sequential API calls would otherwise make the first call determine the second's entitlement. | Freeze all recipients that completed at that checkpoint; resolve their restorations and one DR each as one batch, then mark the shared source depleted if any roll is 12. A recipient not yet due is not restored. This needs an explicit ruling or authoritative clarification. |
| D42-R3 | E10.4 orders a wreck flip for an Immobilized emptied source. C10.1 destroys a towed Gun when its vehicle is destroyed; D5.6 ties survival to elimination. The administrative flip's effect on PRC/towed equipment is not explicit in the read text. | Do not call the general combat-wreck helper blindly. Obtain a ruling on whether the flip constitutes elimination for these clauses. Until resolved, refuse only the affected occupied/towing completion with an explicit dependency; do not claim D4.2 complete. Existing legal unloading/unhooking may prepare an empty source but is not a substitute for implementing the requested disposition cases. |
| D42-R4 | One source must serve two recipients, but current participant exclusivity and blanket TI clearing prevent that. Recipient CE loss and source activity have different potential scopes. | Track source-level participation and per-recipient periods. Recipient loss interrupts that recipient; source movement/fire/CC or loss interrupts all dependent periods. Rules retains source TI while any qualifying work requires it. Confirm this reading alongside R2. |
| D42-R5 | Newly admitted transfer/offload without the E10.4 MP restriction would create a knowingly illegal route, although general restrictions are listed in D4.3. | Bring the relocation-specific no-MP consequence into D4.2 as a prerequisite. Leave wider fire-benefit movement rules and automatic Low Ammo in D4.3; update both task rows to prevent duplicate ownership. |
| D42-R6 | E10 inventory ownership is Pass 175, but D4.2 is a smaller slice and does not fulfill that pass's broader scope. | Use a dedicated D4.2 branch and gate configuration, with a D4.2-specific gate identifier supported by the existing script. Record this as partial Pass 175 work, not Pass 175 completion. Confirm the identifier before the baseline is created. |

## 6. Finite implementation sequence after approval

These are six increments within D4.2, not six additions to the 36-task ledger.

1. **Rules and source contract:** approved rulings, source authorization/selection and typed participant facts; move reached boundary decisions into Rules. Add versioned setup evidence and a small Studio setup/read route so the increment can be exercised before its commit.
2. **Durable assignments:** one/two recipients, complete inventory, source identity, unknown evidence, role-specific TI ownership and historical disclosure. Show assignments in Studio, including a third-recipient refusal.
3. **Dump use and replenishment:** stationary source, same/Accessible Locations, recipient progress, interruption/resume, completion batch and ordinary system dice. Prove retained TI and shared phase-close behavior.
4. **Transfer/offload:** approved mobile-offload reading, full Immobile transfer facts, qualifying destination and atomic relocation. Implement occupant/equipment outcomes and the mandatory no-MP consequence; no completion claim with R3 unresolved.
5. **Guide and legacy acceptance:** exact capability admissions, all pending topics retained, owner/opponent/adjudicator reads, stale confirmation, interrupted consequences, duplicate/retry and legacy event verification.
6. **Delivery:** strict checks, source/inventory/backlog/design evidence, gate and CI coverage enforcement, replay/sweep attribution, PR and green CI. No merge without authorization.

Each completed increment gets its successful Studio check and only relevant test methods before an implementation commit. Editing and its dependent build/run follow the standing separate-step rule. Do not claim a test passed without its actual log.

## 7. Meaningful tests and Studio evidence

Tests below are planned assertions, not existing method names or executed results. Before each test command, map the changed behavior to exact existing/new method names, announce the list, and filter by method. Do not run an entire class or project during implementation.

| Area | Assertions |
|---|---|
| Rules setup | Specified/default source, 15/20 PP boundaries, caliber/barrel mismatch, known invalid versus unknown, SSR/DYO provenance and cost, dump stationary semantics, national-profile unsupported result. |
| Assignments | One/two allowed, third refused, duplicate identities refused, incomplete inventory unknown, shared source allowed without allowing arbitrary overlapping work. |
| Progress/TI | Independent start/duration, CE loss, source/recipient movement/fire/CC, retained prior complete turns, active sibling retaining source TI, exact checkpoint and no skipped phase boundaries. |
| Completion | One DR per due recipient; R2 both orders including a 12; non-due sibling; special-ammo restoration; no new roll on retry; source depletion and restored portage; missing dice refused atomically. |
| Relocation | Same versus Accessible versus merely adjacent; mobile offload versus transfer; all D.7 conditions; single preserved supply ID; destination capacity; exact occupant/towing outcomes under R3; no MP in the restricted turn. |
| Replay/host | Stale setup/assignment/completion, forged or incompatible evidence, invalid batch leaves no changes, historical state before/after, legacy records unchanged. |
| Guidance/disclosure | Pending families retained, no false NotApplicable, no private assignment count/SSR leak, same phase-close answer in command and guide, unresolved mandatory effects block close. |

Existing regression seams include `TurnAmmoEligibilityTests`, `TurnWorkEffectsTests`, `TurnWorkLifecycleTests`, `BacklogPass7Tests.WorkAudit.cs`, `BacklogPass7Tests.SupplyEffects.cs`, `PlayTests.TurnWork.cs`, and the Turns host-bound/disclosure/phase-close tests. Select the exact reached methods, not all tests in these files. Preserve tests proving the accepted adjudicated contract separately from new governed behavior.

Proposed dedicated Studio games: `d42-two-recipients`, `d42-dump-replenishment`, `d42-accessible-transfer`, `d42-mobile-offload`, `d42-relocation-disposition`, and `d42-hidden-legacy`. Choose real supported board Locations from map facts during setup; do not invent terrain or supported counter definitions. Normal live play uses ordinary dice. Deterministic boundary rolls belong in unit fixtures or clearly separate authorized test fixtures, never by changing the user's dice source.

Check proposal, cancellation, confirmation, reloaded history and close in each relevant view. A source-only example without recorded play is not completion evidence. Proposed whole-task UI scope: changed setup/work/guide controls and phase-close, keyboard and mouse, owner/opponent/adjudicator at the standing viewport sizes. Broader unrelated page walkthroughs require a scope reason.

## 8. Performance, gate and delivery

- Build one supply/assignment index per immutable state only if existing reads do not suffice; reuse `PlayStepMemo`, map handles, `ReadLocation` and established adjacency/step facts. No global revision cache and no whole-state scan per candidate inside traversal.
- If a cache is introduced, document owner, state lifetime, full identity/view keys, invalidation, missing-result policy and pure concurrent fills. Test a new snapshot and changed viewer. Do not serialize runtime caches.
- Guidance work should scale with admitted sources and assignments; unchanged map fragments continue to reuse the normal incremental rendering path. No full-layer send for a progress label.
- For measurable new read cost, compare the same build configuration, game/revisions/view, with the browser foreground and no competing builds. Separate Rout and ordinary phases. Declare the acceptance threshold before measuring; historical speedups are not new evidence.
- At the gate, collect ASL.Turns coverage and fail for any uncovered line or branch, missing report, missing assembly or zero-instrumentation result. Use an assembly inclusion boundary, no exclusions hiding production code. Archive reports and exact numerator/denominator counts. Add the same assertion to its existing CI matrix share; avoid running that project twice in CI just to collect coverage.
- Use the existing gate stages and baseline/proof machinery with a dedicated approved configuration. Derive skip lists from the actual diff, trial the pass's new games on the baseline, explain every intentional difference and reject meaningless hash-only comparisons. Full suites remain gate/CI work.
- No reference changes are expected. If needed, regenerate all transitive lock files, including both affected Campaign projects, and verify locked restores. Tests alone do not establish lock-file correctness.
- New files require a PR and green CI. Stage explicit paths, never the existing untracked `.codex/` directory. Do not commit this preparation or start a branch merely because the document was saved. Push/PR and merge follow their authorization boundaries.

### Build outputs outside the repository

Preparation created no build, game or PDF-render output. During implementation, put owned Studio builds, proof/log/coverage output under a dedicated `E:/Archive/GitHub/dlandi/asl-gate-work/d42` area where compatible with the established gate paths. Register every created outside-output path immediately using `outside_outputs.sh`; name any shared gate-owned paths in the implementation record. Use the agent Studio port and established launch path, never the user's 5178 instance. Stop only owned processes. Clean owned output at the earliest safe point and confirm the registry is clear at completion. Do not delete shared proof stores or user games.

## 9. Review requested

Approve or amend D42-R1 through R6, especially simultaneous depletion and the administrative-wreck disposition. The rest of the proposed architecture preserves the accepted backend and puts all new rule decisions in Rules. After approval, record the rulings and boundaries in the main design, finite plan, source register and backlog before implementing the affected route. D4.2 remains Pending until every approved in-scope route, including disposition, has its acceptance evidence.
