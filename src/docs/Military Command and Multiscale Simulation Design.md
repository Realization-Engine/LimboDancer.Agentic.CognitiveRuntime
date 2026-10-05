# Military Command and Multiscale Simulation Design

**Status:** Proposed design for review, not an implementation authorization or a published rules package

**Date:** 2026-10-05

**Authority:** Subordinate to the Plane Runtime Specification and Domain Integration Model

**Scope:** A military simulation architecture spanning squad engagements through higher command, organized around hierarchy, organization and communication, with shared time and sustainment constraints. ASL supplies the tactical foundation; platoon/company simulation is the first delivery scope.

## 1. Design decision

Build a persistent military command and multiscale simulation architecture on the existing cognitive runtime and ASL combat foundation. Hierarchy, organization and communication are core domain responsibilities at every supported echelon. The first new simulation domain is the sibling Panzer domain, following the ASL implementation's evidence, state and execution patterns. Preserve ASL as an independently functioning domain. The first objective is a complete, bounded formation-scale engagement using a new eight-phase cycle modeled on ASL. Section 6 defines that cycle. Reproducing either original Panzer game is optional future work, not a prerequisite.

The change in scale belongs primarily in the domain: represented forces, map semantics, combat resolution, time, information, and coordination. It does not require new runtime authority transitions. Reasoning still proposes; published semantics establish applicability; Governance and the Execution Gate permit execution; committed events establish game state.

Three choices define the design:

1. **Separate organizational formations from playable units.** A company can contain several counters, while a particular counter can itself represent a company. Neither representation implies a collection of individually simulated ASL squads.
2. **Publish our own explicit simulation rules.** ASL provides the phase structure and architectural discipline; PanzerBlitz and Panzer Leader provide scale, combined-arms and support-fire references. Adopted, modified and newly designed mechanics are identified separately. Digital bookkeeping enables persistent orders, interrupted movement, support commitments and restricted observations without inheriting the limits of physical counters.
3. **Prove reuse through a second working domain.** Reuse the runtime now. Extract shared wargame components only where both ASL and Panzer demonstrate equivalent semantics and tests prove that ASL behavior is preserved.

The first executable scope remains platoon/company combat. The architectural direction extends through battalion, regiment or brigade, division, corps, army, army group and theater where appropriate to the modeled organization. Section 17 defines that direction without claiming a working campaign engine or expanding the initial delivery gate.

## 2. Evidence and limits

The design uses the local WWII corpus and the current working-tree implementation. Appendix A lists the particular source files and passages used. The corpus index is a discovery aid, not an authority for a game rule.

The original Panzer rules were inspected for sequence, movement, transport, combat, spotting and optional rules. Panzer Leader's indirect-fire procedure and opportunity-fire provisions were examined in particular. Historical reading focused on the organization and tank-infantry coordination passages in Rottman's *World War II US Armored Infantry Tactics*, with Bull's squad/platoon study and the archive's organization and fortification material providing further reading directions. This is not a completed historical validation of every proposed model field.

Some PDF text extraction corrupts fractions and table layout. Exact numeric thresholds, counter values, combat tables and precedence must be visually checked during source admission. No unverified OCR value is proposed as executable authority here.

The supplied ASL PDF `eASLRB_v3_01.pdf` is already available through the existing source references. The [ASL source registry](../../docs/ASL/SourceRegistry/README.md) records its identity and whole-rulebook scope, including Chapter H. A new copy is not needed.

Historical evidence informs model requirements; it does not automatically establish simulation parameters. Rottman's discussion of changing organization and uneven tank-infantry doctrine motivates dated establishments and command models. It does not establish a numerical command-delay rule. Proposed mechanics become authoritative for our simulation through review and publication in our own package. [H1]

The submitted organization and scale notes are design prompts, not admitted historical or rules evidence. We adopt their interest in constituent readiness and company coordination, with the constraints in Sections 5.4 and 8.2. We adopt communication as a foundational model in Section 8.3 and do not adopt a replacement impulse turn or independent reaction-point currency. Their counter statistics, Situation 1 account and universal organization assumptions must not populate scenario fixtures. Exact source identity and reviewed passages take precedence. [N1, S1]

## 3. What the current architecture already gives us

The architecture is more mature than its earliest ASL implementation notes. The present source includes many specialized movement, fire, ordnance, vehicle, disclosure and consequence planners. The following are concrete implementation anchors, rather than a claim that every ASL rule is implemented.

| Existing foundation | Evidence in the repository | Treatment for Panzer |
| --- | --- | --- |
| Domain-neutral identity, evidence and conclusions | `DomainPackageRef`, `DomainConclusion`, `DomainConclusionContext` in Abstractions | Reuse exact contracts and package scoping |
| Governed mutation | ASL `GameConstraintEvaluator` and `GameActionExecutor` in `GameGate.cs` | Follow replan, validated-version check, commit and effect-readback sequence |
| Event store and optimistic revision checks | ASL `IGameStore`, `FileGameStore`, `AppendRolled` | Reuse the pattern; Panzer needs its own event payloads and projector |
| Projection and replay | ASL `GameProjector`; bounded projection caching in `GamePlanner` | Preserve deterministic state derivation; do not assume the cached ASL projector is domain-neutral |
| Delayed consequences and player choices | ASL planner partials and state records | Inspect as design precedents for persisted reaction windows |
| Perspective-dependent disclosure | ASL `GameView`, `EntryDisclosure`, disclosure planners | Preserve withholding before commitment and audience-specific explanations |
| Dice drawn inside commit | `PlannedRoll`, `AppendRolled`, `LimboDancer.Dice` | Reuse dice arithmetic; preserve transactional draw/commit discipline |
| Exact boards, derivation and source provenance | ASL Maps and Authoring packages | Reuse identity and verification principles; implement Panzer terrain semantics separately |

The [Domain Integration Model](<LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md>) expressly keeps units, hexes, phases and LOS outside Runtime. The [runtime specification](<LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md>) remains authoritative.

There are also concrete limits to direct reuse. ASL `SideState` contains ELR and SAN; ASL positions include building levels and location chains; ASL LOS implements VASL behavior. The ASL Play project currently references Runtime for integration. A new domain should reuse that integration pattern without importing ASL records or copying the entire planner.

## 4. Scale and rules profiles

| Dimension | Existing ASL domain | Proposed formation simulation baseline |
| --- | --- | --- |
| Typical represented unit | Squad, half-squad, leader, individual vehicle or gun | Maneuver platoon or support section/battery; company as organizational command group |
| Map scale | Abstract 40 meters per hex | 250 meters per hex |
| Nominal game turn | Two minutes | Six minutes per complete pair of player turns, provisional calibration |
| Terrain semantics | Detailed locations, levels, hexsides and VASL geometry | Formation-scale terrain, elevation, obstacles, cover and observation |
| Combat state | ASL-specific conditions and procedures | Cohesion, suppression, fire/movement commitments and engagement state |
| Command view | Individual units and local interactions | Groups of counters, supporting elements and formation objectives |

ASL and the Panzer sources supply the reference scales. For our baseline, 250 meters per hex and six minutes per complete game turn are initial design choices, subject to calibration of movement, fire and command timing together. A larger hex covers roughly 39 times the area of an ASL hex of the same shape, but this ratio is not a terrain-conversion rule. Three nominal ASL game turns are not automatically interchangeable with a formation turn. [R1, R2, R3]

### 4.1 Exact profile identity

Each game pins a profile manifest with:

- simulation family, authored rules version and reference source editions;
- published semantic package identity and content hash;
- admitted errata and interpretation decisions;
- explicit optional-rule selections and their compatibility constraints;
- counter catalog, board packages, scenario and special rules;
- state/event schema and resolver versions;
- information/disclosure policy and supported-capability manifest.

`formation-asl-v1` is the proposed baseline profile identity, not an existing registration. Its authoring manifest records ASL inspiration, Panzer references, historical evidence and deliberate design departures. Possible future `panzerblitz-original` and `panzerleader-original` profiles would require separate exact packages and conformance tests. No setting changes silently during play.

Scenario special rules are reviewed package content with explicit scope and precedence. Optional combinations need conformance cases; they are not an unrestricted bag of switches. A package update produces a new game or explicit migration, never reinterpretation of an existing log by the latest rules.

### 4.2 Baseline and later fidelity

The baseline includes formation orders, communication links and message delivery, cohesion, support commitments and defensive reactions. Its initial communication policy delivers valid orders and eligible reports immediately over configured links, recording issue and receipt even when they share a cursor. Order execution still follows the phase and authority rules. This is an explicit idealized policy within the core communication architecture, not a bypass around it. Later profiles may add transmission delays, link failures, detailed ammunition, fatigue and persistent equipment losses. Each added behavior needs evidence, explicit assumptions, calibrated parameters and sensitivity tests; no universal radio radius or random delay is presumed.

A company grouping coordinates subordinate units but has no extra movement or fire budget of its own. Changes to modeled behavior require a new package version, even when the UI looks the same.

## 5. Domain model

### 5.1 Definitions and instances

The following are proposed domain records, not additions to Runtime Abstractions.

| Record | Responsibility |
| --- | --- |
| `UnitDefinition` | Versioned playable-unit definition: designation, echelon, weapon capabilities, fire modes, movement, cohesion/recovery parameters, target class and evidence/assumption references |
| `UnitInstance` | A counter in a game: owning side, definition reference, position, lifecycle status and rule-specific conditions |
| `CompositionEvidence` | Men, weapons or vehicles represented by a definition, with date, source and uncertainty; descriptive unless a rule explicitly uses it |
| `FormationInstance` | Company, battalion or task grouping with a stable identity and membership; not an additional combat counter |
| `EstablishmentDefinition` | Dated authorized organization by army, formation type and theater, where evidence supports that specificity |
| `Attachment` | Temporary command/support relationship with effective start/end and provenance |
| `ScenarioForce` | Actual selected counters and historical labels for this situation, distinct from paper establishment |
| `TransportAssignment` | Rule-defined carrier/passenger relationship and activity constraints |
| `ContactRecord` | Information available to a perspective, where the profile supports uncertain or concealed information |
| `FormationOrder` | Persistent objective, assignments, constraints and completion/supersession state |
| `ActivityLedger` | Activity-group fire/recovery ownership, constituent movement expenditure and reservations, with exact reset boundaries |
| `SupportMission` | Committed area, resource, payload, observation requirements, due cursor and resolution/cancellation state |
| `EngagementInstance` | Local assault participants, concealed choices, contested position and continuing engagement status |
| `ForceRoster` and `DeploymentRecord` | Stable constituent identities and persistent deployment for cross-scale projection; see Section 14 |
| `TerrainRegion` | Shared ground-space identity, admitted detail and persistent material changes beneath both map views |
| `EngagementPlan` | Parent scenario's versioned set of linked ASL engagements, activation conditions, dependencies and synchronization boundaries |
| `EngagementContract` | One child engagement's region, roster assignments, interval, objectives, support reservations and exact mapping/package identities |
| `EngagementOutcome` | Durable local facts and consumed resources awaiting or completing parent reconciliation |

Organizational parentage, current tactical control and transport are different relationships. A platoon can belong to one company, be attached for a mission, and ride in carriers without becoming three units. Validate acyclic membership and a single effective controlling assignment where the profile requires it. Formation strength summaries count each active playable instance once.

A definition's equipment count is not multiplied into an already aggregated attack value. A five-vehicle composition annotation describes what a unit represents; it does not authorize five separate shots. Similarly, eliminating a unit does not prove that every represented person died or every vehicle was physically destroyed.

### 5.2 State fidelity

Baseline state stores position, carrier/passenger status, cohesion, suppression, elimination, movement and fire budgets, orders, committed support, reaction windows and scenario progress. Cohesion distinguishes effective, disorganized and broken formations; suppression is a separate restriction. Combat tables must explicitly map outcomes to these states. Panzer dispersal and ASL broken morale are reference mechanisms, not interchangeable state codes. [R1, R2, R3]

Baseline state also records communication endpoints, configured links, message identities, delivery state and recipient knowledge under Section 8.3. Later versions may store detailed personnel and vehicle availability, ammunition, fatigue and transmission conditions. Unknown values remain unknown. Do not populate invented precise strengths merely because a record has a numeric field.

Use separate fields for authorized establishment, scenario starting strength, current modeled strength and estimated enemy strength. Each has different evidence and visibility. Nationality selects applicable catalog and rule content; it does not by itself supply an assumed quality score.

### 5.3 Maps and spatial queries

A Panzer board package carries its native coordinates, orientation, grid topology, hex/hexside features, elevations, roads and source evidence. Rendering geometry is distinct from ground scale and movement costs. Roads have connected edges, not merely a visual line crossing a hex.

Provide formation-scale movement, LOS and observation calculations. Visible map terrain, clear LOS, an eligible target and knowledge of enemy identity are separate facts. PanzerBlitz town/woods spotting and command-post-directed indirect fire, and Panzer Leader's observation requirements, provide comparison cases. Our package explicitly chooses its own observation and support rules; it must not combine the originals accidentally. [R1, R2]

Each calculation returns the facts used, rule/profile identity, board version, applicable modifiers and a disposition. Unsupported terrain or ambiguous board evidence returns an explicit unsupported/indeterminate result. Do not fall back to ASL LOS or geometric visibility alone.

Candidate reusable components include coordinate arithmetic, board placement transforms and rendering primitives. Extract them only after verifying they do not depend on ASL naming, VASL dimensions, terrain codes or location semantics.

### 5.4 Formation readiness and capability

A formation summary is a projection of authoritative state. Where constituent conditions have been admitted, derive the summary from those roster assets, their conditions, attachments and activity commitments. Where only aggregate conditions exist, show that supported resolution and leave constituent readiness unknown. Do not synthesize squad casualties from a coarse cohesion result. The baseline retains Section 5.2's aggregate resolution; detailed loss allocation and cross-scale condition mappings require published rules before use.

Keep surviving strength, temporary suppression/cohesion, equipment availability, command/support relationships and remaining action commitments distinct. A squad can be both surviving and suppressed; these are separate dimensions, not mutually exclusive steps. Detachment changes assignment and location, not survival. Company totals count each constituent once even when several counters or attachments refer to it. The summary is not a second writable store of strength.

For an authored three-squad example, two effective squads and one suppressed squad still constitute three surviving squads. Recovery can restore the third squad's availability; it cannot recreate a destroyed squad. Three squads are a scenario choice, not the definition of every platoon. Dated establishment, actual starting strength and subsequent events determine composition.

Capability depends on which assets remain usable, not merely the fraction of surviving squads. Loss of an attached antitank gun can remove an antitank capability without a proportional personnel loss. A versioned capability projection must specify eligibility, modifiers and treatment of unknown conditions; it cannot multiply an already aggregated fire value by an assumed squad count or grant extra attacks. Section 6.2's activity ledger remains authoritative.

The displayed summary applies the viewer's disclosure policy after derivation. Enemy strength, readiness counts and missing capabilities must not reveal hidden constituent state through totals, tooltips or action previews. Preserve detailed facts for future expansion even when several conditions map to the same displayed readiness category.

## 6. Time, actions and durable consequences

### 6.1 The ASL-inspired formation turn

The baseline adopts ASL's eight-phase order, adapted for platoons and company command. This is our proposed rules design, not a transcription of ASL or either Panzer manual. ASL A3 supplies the sequence and the distinction between the active side and a reacting opponent. PanzerBlitz supplies a comparison for fire-versus-maneuver commitments, transport and combined arms; Panzer Leader supplies references for planned indirect fire, air support and reactions. [R1, R2, R3]

One game turn contains Side A's player turn followed by Side B's player turn. Scenario setup fixes the first side. Each player turn traverses all eight phases below. The active side is the maneuvering side; the other side is the defender. These roles reverse at the player-turn boundary. Both player turns collectively represent the nominal six-minute interval. Phases are an adjudication order, not eight equal time slices, and the second player turn does not add another six minutes.

| Order | Formation phase | ASL foundation | Acting side and concrete responsibilities | Required exit state |
| --- | --- | --- | --- | --- |
| 1 | **Command and Rally (RPh)** | Rally | Process due arrivals and orders. Active side attempts eligible cohesion recovery, issues or revises company/platoon orders, and assigns support and maneuver roles. Defender then attempts eligible recovery and maintains standing orders, without gaining a general maneuver action. | Recovery decisions and active-side order choices completed or passed; available resources and restrictions recorded. |
| 2 | **Preparatory Fire (PFPh)** | Prep Fire | Active side resolves previously committed support due now, then declares stationary direct fire and support missions for a later turn. Smoke missions due now resolve before other support, then direct fire. Firing units commit their offensive fire budget and forgo voluntary movement and advance this player turn. | Due support resolved or explicitly cancelled; all selected attacks resolved; fire commitments recorded. |
| 3 | **Movement (MPh)** | Movement with Defensive First Fire | Active side moves eligible formations one step at a time, loads/unloads, changes deployment and approaches objectives. Each exposure step can open defender reaction fire. Minefields and other entry hazards resolve before that step's reaction window. | Every started movement completed or stopped; all hazards and reaction windows resolved; optional advance reserve retained. |
| 4 | **Defensive Fire (DFPh)** | Defensive Fire | Defender spends remaining defensive fire budgets against currently eligible targets. A unit that spent its budget reacting during Movement cannot fire again here. | Defender completes or passes remaining fire; mandatory attack consequences resolved. |
| 5 | **Advancing Fire (AFPh)** | Advancing Fire | Active units that did not Prep Fire may spend their offensive fire budget. Units that moved use the published moving-fire mode; eligible stationary units use the stationary mode. Weapon capabilities may prohibit moving fire. | Selected attacks resolved; offensive fire window closed. |
| 6 | **Rout and Withdrawal (RtPh)** | Rout | Active side resolves compulsory withdrawals first, then defender. Broken formations in immediate danger must use a legal retreat or take the prescribed trapped outcome. Other broken formations may seek cover. | All compulsory withdrawals, captures or disintegrations resolved; no unresolved retreat choice. |
| 7 | **Advance and Assault Deployment (APh)** | Advance | Active, effective, unsuppressed units may spend previously reserved movement on at most one adjacent-hex approach, including an assault entry. Eligible defenders may react using only unspent defensive budgets. No loading, unloading or ordinary direct fire by the advancing side. | Approaches, reactions and assault entries resolved; unused movement reserve expired. |
| 8 | **Close Combat and Consolidation (CCPh)** | Close Combat | Both sides resolve local assaults and continuing close engagements. Apply the engagement's simultaneous outcomes, withdrawals and control changes. Perform player-turn cleanup and scenario checks. | All mandatory engagement results resolved; surviving contested positions marked; boundary events committed. |

```text
RPh -> PFPh -> MPh [defender reactions] -> DFPh -> AFPh
    -> RtPh -> APh [limited defender reactions] -> CCPh
    -> switch active side -> RPh ... CCPh
    -> complete game turn -> next game turn
```

An empty phase is explicitly completed in the event log. A scenario without aircraft or artillery uses the same cycle. There is no separate Air Phase: committed air support uses a support-resolution window, with interception/anti-aircraft reactions nested before the strike. A package without air support declares that capability unsupported.

### 6.2 Formation commitments and eligibility

The phase order needs enforceable budgets, not just labels. In the rules below, a unit means its original activity group for fire and recovery accounting; detachments inherit that ownership as specified after the list. Proposed baseline rules are:

- **Offensive fire:** one fire commitment per unit in its own player turn, spent in PFPh or AFPh, not both. Several units combining fire each spend their commitment. Close combat is a separate engagement entitlement, not a second ordinary fire attack.
- **Defensive fire:** one fire commitment per unit during the opponent's player turn, shared across MPh reactions, DFPh and APh reactions. Passing an opportunity preserves it; DFPh passage does not refresh it. Multiple ROF, subsequent fire and final protective fire are not baseline mechanics. Each would require an explicit later rule.
- **Movement:** one allowance per unit in its own player turn, shared between MPh and APh. PFPh firing or a committed support mission due that turn prohibits voluntary movement and APh. Compulsory retreat remains possible. Transport operations consume specified movement and prevent the passenger/carrier combination from obtaining duplicate allowances.
- **Enemy occupancy:** ordinary MPh movement cannot enter a known enemy-occupied position. Newly discovered blocking enemies stop movement at a recorded contact checkpoint before entry, spending the published approach cost. Deliberate entry requires APh assault deployment. Vehicle overruns are a possible later capability, not an implicit exception.
- **Advance reserve:** before ending that unit's MPh movement, designate at most one adjacent destination and reserve its full terrain entry cost. The unit may instead reserve an adjacent destination while remaining stationary. APh may execute that approach only if the destination remains legal and its cost still fits the reserve; otherwise cancel it. No new allowance, cheaper substitute destination or free 250-meter advance appears in APh. This deliberately differs from ASL's one-hex advance.
- **Condition restrictions:** effective units can act within their capabilities. Suppressed units cannot voluntarily move or assault; disorganized units cannot assault and use only the fire modes explicitly allowed by their definition. Broken units cannot voluntarily fire, maneuver or advance, but can withdraw. Conditions apply immediately, including inside a reaction window.
- **Recovery:** each side receives one recovery opportunity per unit per complete game turn, usable in either RPh. Recovery is never automatic phase cleanup. The package defines the check and condition change; the per-game-turn limit prevents two recovery attempts merely because both player turns have an RPh.
- **Assaults:** an eligible unit may use AFPh and subsequently assault, paying the reserved approach cost. A moving assault uses both the offensive-fire and movement restrictions above. Units in a continuing close engagement cannot conduct ordinary movement or ranged fire; they must resolve disengagement or combat through the engagement rules.

Offensive and defensive fire ledgers are distinct abstractions, as active and defensive opportunities are distinct in ASL. Both can be used within one game turn; calibration must account for that total fire volume. If detailed ammunition is added, both draw from the same ammunition inventory. Fire budgets reset only at the start of the corresponding player turn, never at a phase transition. Recovery opportunities reset at the complete game-turn boundary. These entitlements belong to persistent activity groups, not to the number of displayed counters or newly created detachments.

A formation unit starts with an `ActivityGroupId` identifying its budget owner. Splitting its roster creates disjoint detachments but retains that group and its spent/reserved ledger. In the baseline, the group still has one offensive commitment, one defensive commitment and one recovery opportunity at the specified boundaries. A firing detachment spends the shared commitment and uses only its own admitted capability; other detachments do not supply remote firepower. A recovery action fixes the eligible participants before its roll, applies the published result to those participants and consumes the group's opportunity. It cannot be retried by another fragment. Movement expenditure and reservations remain attached to constituent assets; each detachment inherits their remaining allowances, with its group movement limited by its participating assets. Recombination preserves all constituent expenditure and originating budget ownership rather than adding or refreshing allowances. Independent new groups require an explicit reorganization rule and capability allocation at a future boundary; splitting for display is not that rule.

Formation activity groups are not imposed as literal ASL firing limits. An admitted cross-scale mapping translates the group's interval entitlement to child capabilities once, reserves the parent group for that interval and records the child expenditures. Splitting a group across several children requires a reviewed allocation of constituent capabilities and interval entitlements; if that mapping is absent, keep the group under one adjudicator. Recombination cannot restore either a spent parent entitlement or a child action.

For the initial retreat rule, immediate danger means an adjacent known effective enemy or exposed terrain under a known enemy's eligible direct-fire threat. A retreat step must increase separation from the triggering threat, avoid known enemy-occupied positions and stay within the package's withdrawal allowance; stop when a qualifying covered position is reached. If no legal retreat exists, surrender when an adjacent enemy can accept it under the capture rule; otherwise record disintegration. These are proposed game abstractions requiring fixtures, not historical casualty assertions. No defensive reaction fire is allowed during RtPh in the baseline; retreat hazard rules still apply.

At 250 meters per hex, co-location does not imply every platoon is in hand-to-hand combat. An `EngagementInstance` records participants, assault objective and contested position inside the hex. Close combat includes short-range fire, armor-infantry interaction and loss of local control. Both sides commit their choices before outcomes are disclosed; resolve from one engagement snapshot so the first serialized attack cannot erase an opponent's already committed simultaneous attack. Each unit participates in at most one engagement per CCPh. Continued engagement state survives the player-turn boundary.

### 6.3 Domain time and phase advancement

Represent game time as `gameTurn`, `playerTurnOrdinal`, `activeSide`, `phase`, `substep` and `interruptOrdinal`. A separate `decisionSide` identifies who currently owes a choice. An interrupt does not change the active side or silently advance simulated time. Runtime wall-clock budgets remain separate.

Each phase transition has this ordered contract:

1. Check the expected game revision and the current decision owner.
2. Refuse advancement while attacks, required withdrawals, choices or interrupt windows remain unresolved. Record passes for optional actions.
3. Apply that phase's explicit exit effects, including expiring only the budgets or conditions named by the rules.
4. Commit the next cursor and its due-effect queue together. Resolve mandatory entry effects before accepting discretionary actions in the new phase.
5. Publish a new observation separately for each side and replan from that revision.

Due effects use a declared ordering: scheduled expiry and arrival effects, support smoke, other support, then discretionary phase actions. Within a class, use declared mission order unless the package assigns a player choice; choices are logged. Recompute observation after each committed effect. Newly submitted missions cannot insert themselves into a support window already being resolved.

After A's CCPh, evaluate player-turn objectives, switch roles and enter B's RPh without incrementing the game turn. After B's CCPh, additionally resolve game-turn-duration expiry and game-turn objectives, advance the game turn and reset its recovery ledger before A's RPh. Control changes are recorded when they occur; scoring reads those facts at the scenario's declared boundary. An immediate-victory rule is checked after the causally complete action batch. Ending a scenario records outstanding missions as terminated, not silently resolved attacks.

Suppression and smoke carry explicit expiry cursors; they never disappear because a view clears its markers. The first playable package must specify their durations, recovery checks, retreat allowances, movement costs and combat modifiers. The turn order and resource lifecycle above are concrete design decisions; numerical calibration remains work to complete before executable rule admission.

### 6.4 Orders and committed support

Company orders identify objective, assigned platoons, support relationships, route/axis, constraints and termination conditions. The active side issues or revises them in RPh; they persist across turns until completed or superseded. They guide planning but do not authorize execution or create extra actions. Within a turn, units may stop a route, decline an attack or make mandatory defensive responses when circumstances change. A new discretionary company mission waits for the next RPh in the baseline.

Panzer Leader's advance target designation illustrates why a fire mission must be durable rather than an agent reminder. Our baseline adopts that commitment principle with explicit new timing: declare a mission in the owner's PFPh, due at the opening of that owner's PFPh in the next game turn. Scenario-defined preplanned missions may be due on turn one. [R2, VII.C]

Declaration pins the target area, payload, battery/support resource, observer evidence and due cursor. It reserves the resource's offensive commitment for the due player turn; the resource cannot accept another overlapping mission. Declaration alone is not a shot. At the due turn, the resource forgoes voluntary movement; resolution spends the reserved fire budget. A pre-resolution cancellation forfeits that reserved budget for the due turn, preventing cancellation from manufacturing a new fire opportunity. Defensive fire is independently budgeted but still subject to equipment, ammunition and condition restrictions.

At resolution, validate the committed resource and observation conditions. A preplanned mission may resolve at its committed area without a live observer if its package permits it. An observed mission that has lost its required observer cancels under the baseline. A destroyed or ineligible resource cancels; an empty target area does not permit retargeting. Retargeting requires cancelling and declaring a new mission with the normal delay. Smoke duration and dispersion are published parameters, not improvised by the scheduler.

Use `FireMissionDeclared`, `FireMissionResolved` and `FireMissionCancelled` with causal IDs and explicit resource disposition. Aircraft missions, when supported, add a persisted air-defense window before strike resolution. There is no general free interrupt for artillery. Faster reactive missions need their own response-delay and resource rules in a later package.

Stored missions never contain reusable Runtime authorization. Due resolution passes through a fresh gate. Retries neither reserve a resource twice nor resolve a mission twice; replay reconstructs every pending commitment.

### 6.5 Defensive reactions and atomic actions

ASL Defensive First Fire supplies the architectural place for movement reactions; Panzer Leader's optional opportunity fire supplies a reference for exposure and later activity restrictions. Our baseline does not inherit its optional status or its expenditure threshold. Reactions are a core capability. [R2, XV.B; R3, A3.3]

Ordinary MPh movement commits the destination position and due entry hazards before opening its defensive reaction window. Surviving targets are evaluated at that destination for observation, LOS, range and cover. APh instead uses the explicit pre-entry approach state below. Eligible defenders need an unspent defensive budget, legal weapon/target conditions and an observed target at the applicable checkpoint. A defender chooses firing units in recorded order; after each attack, re-evaluate eligibility and the mover's condition. A decline closes that checkpoint for the declining unit but does not spend its budget. There are no reactions to reactions in the baseline; air defense has its separately specified support window.

An APh `ApproachState` records origin, reserved destination, cost, participant identities, intended assault and current substep. Its baseline sequence is:

1. Revalidate the reserve and participants. Commit the approach cost and pending state; the unit still occupies its origin. Resolve only hazards whose published trigger is crossing/approach, not destination occupancy.
2. Recheck eligibility after those hazards. If still eligible, open a defender window using the origin position for observation, LOS, range and cover. Destination cover never protects a unit before entry. This is an explicit coarse-scale abstraction; no intermediate position or destination-based shot is inferred.
3. After each reaction and again at window closure, apply all entry restrictions. Elimination, suppression, broken or disorganized condition, immobilization where relevant, an invalid destination or any other failed requirement cancels entry. Surviving units remain at the origin; spent cost and hazard/attack effects persist. Cancel outstanding approach choices when entry is no longer possible.
4. If eligible, commit entry once, then resolve destination-entry hazards at the new position. Failure caused after entry does not roll back the position or restore movement. Resolve mandatory consequences and admit only eligible participants to an assault. Broken entrants use the next RtPh unless the engagement rules require an immediate capture or other consequence.

No later movement or phase advancement can bypass these pending substeps. Opening a window, committing entry and applying hazards use unique causal IDs, so replay or retry cannot charge cost, trigger a hazard or move a unit twice. If pre-entry hazards cancel an approach, destination-entry hazards never fire. Ordinary MPh interruption leaves the unit at its already committed destination.

A UI route or company maneuver is a proposal of atomic actions, each freshly validated after earlier consequences. Semantic actions include issue order, attempt rally, declare/resolve/cancel support, fire, move step, load/unload, reserve advance, withdraw, assault approach, commit close-combat choice, react/pass and advance phase. Multi-unit fire and simultaneous close-combat outcomes commit as causally complete batches.

Internal planners include `CommandPlanner`, `RecoveryPlanner`, `MovementPlanner`, `FirePlanner`, `TransportPlanner`, `WithdrawalPlanner`, `AssaultPlanner`, `TurnPlanner` and `ReactionPlanner`. These remain domain components behind registered actions, not new runtime planes.

### 6.6 Worked player turn

A company has a weapons platoon, two rifle platoons and attached tanks. During RPh it orders one rifle platoon to seize a village while the second remains in support. In PFPh a smoke mission declared last turn arrives, then the weapons platoon fires and loses voluntary movement for this player turn. In MPh the assault platoon approaches, keeping enough movement for one declared village entry; a defending gun reacts and spends its defensive budget. The tanks' movement is committed separately under the same company intent.

In DFPh the gun cannot fire again, but an enemy platoon with an unspent budget may fire. In AFPh surviving eligible attackers use their remaining offensive fire, with moving-fire restrictions where applicable. In RtPh broken formations resolve required withdrawals. In APh the effective assault platoon pays its reserved village approach cost; a defender that withheld its fire can still react. If entry succeeds, CCPh resolves the local engagement and control. The other side then starts its own eight phases. A cancelled approach does not grant a replacement move.

## 7. State authority, transactions and replay

The existing ASL execution path supplies the template. The proposed Panzer path explicitly adds candidate-state validation before append and the failure-handling requirements below:

```text
Player or agent request
  -> registered descriptor and arguments
  -> current perspective observation
  -> deterministic domain plan and evidence
  -> Governance, Diagnostics and Execution Gate
  -> AuthorizedAction
  -> fresh plan and validated revision check
  -> candidate events and projected-state validation, with transactional dice
  -> atomic durable event append
  -> committed-state projection and effect readback
  -> audience-filtered result
```

Use one ordered authoritative event stream per game initially. Each envelope carries tenant/game scope, event and attempt IDs, revision, exact profile/package identities, causal links, time cursor and disclosure classification. Payloads are domain-specific. The runtime audit records the authority decision; the domain event log records what occurred in the simulated world. They refer to each other without being interchangeable.

Within the single-writer commit boundary, check idempotency and expected revision before drawing dice. Build a candidate event batch, project it against the validated state and check resulting invariants before authoritative append. Only a valid, causally complete batch becomes durable game history. Publish its outcomes after successful commit; post-commit projection and effect readback verify the committed result rather than deciding whether an invalid state should have been accepted.

Randomness used to construct the candidate belongs to the attempt. Invalid candidates do not mutate game state or reveal rolls to the player. An engine invariant failure faults that attempt and blocks automatic retry; retain its diagnostic roll record so repair cannot silently reroll it. An uncertain persistence outcome is recovered by attempt ID before any retry or new draw. Repeating a committed attempt returns its stored outcome, and a stale attempt draws nothing. Replay consumes recorded random outcomes, never fresh randomness. The new store must prove these contracts; citing ASL `AppendRolled` as a precedent does not establish that candidate validation and failure recovery already exist.

For simultaneous agent proposals, the single writer establishes a total committed order. A losing proposal is stale and must be re-observed. Do not resolve contradictory moves by last-writer-wins state replacement.

Persisted checkpoints are a later optimization. A checkpoint must bind event-prefix hash, revision, profile, schema and projector version; replay from the checkpoint must equal replay from the beginning. Projection caches may accelerate reads but never establish legality or authority by themselves.

The existing file store is suitable as a single-process reference implementation. Its process-local locking and whole-file persistence are not a claim of multi-process transactional support. Move to a transactional store only when deployment or measured workloads require it, preserving the contract and conformance cases.

## 8. Information and agent behavior

### 8.1 Profile-specific information

The baseline uses side-specific observations: own forces and admitted terrain are known, enemy units appear through contacts permitted by the published observation rules. LOS alone does not reveal exact identity, strength or orders. An open-information diagnostic setup may be used for fixtures, but is explicitly labeled and cannot be mistaken for the baseline information policy.

For profiles that support concealment or uncertainty, maintain adjudicator state separately from a side's observation. Contacts can have estimated type, location region, confidence class, source and last-observed cursor. The identity behind a contact is not available to the deciding agent until the profile permits it.

Filter plans, legal-action lists, refusal reasons, logs and explanations as well as rendered counters. A hidden unit must not leak through a path cost, forbidden-target message or a planner's explanation. Reuse ASL's disclosure discipline, not just its display filtering.

### 8.2 Company intent and subordinate execution

A company-level interface can record an objective, assigned formations, route or phase line, support allocation, constraints and termination conditions. These are planning artifacts until committed by actions authorized under the selected profile.

Orders retain issued, executing, superseded and completed states under Section 6's RPh timing. Their associated messages independently record issue, receipt and any required acknowledgment under Section 8.3. Receipt makes an order available for eligible execution; it does not mean the order has been executed or the mission completed. An order constrains candidate plans but never grants Runtime permission. Unspecified historical behavior produces an unresolved modeling requirement, not an LLM ruling.

Company selection groups planning and presentation within the current phase. During RPh, assign objectives, subordinate roles, routes and support under the existing order rules. During MPh, execute eligible subordinate moves as individually validated steps, resolving each reaction before continuing. Re-observe and replan after material changes; a company plan is interruptible, not an atomic promise that every subordinate action will succeed.

Company selection does not run a private eight-phase cycle, skip the opponent's decision windows or create movement/fire resources. Defensive reactions use Section 6's existing shared commitments, with no separate reaction-point pool. A local ambush resolves at formation scale during that interrupt. If detailed adjudication is desired, admit a later ASL engagement only at Section 14.5's supported boundary; never replay an already resolved ambush in the child.

Agent hierarchy and military hierarchy need not be one-to-one. Start with one bounded planner operating on a side's view, with a deterministic policy baseline. Later subordinate agents may propose platoon tasks, but each proposal remains subject to the same permissions, state versions and gate. An organizational commander never acquires Runtime authorization by virtue of its simulated rank.

Evaluation separates legality, information discipline, plan completion and tactical utility. Winning a game does not prove historical validity. Use held-out situations, multiple recorded random streams and comparable information budgets; compare against legal deterministic policies before attributing gains to a more complex agent arrangement.

### 8.3 Communication architecture

Communication is a central pillar alongside hierarchy and organization. Orders, reports, support requests, acknowledgments and changes of command pass through explicit domain communication records. This model is required from the baseline; historical transmission fidelity can grow without replacing its lifecycle.

| Concept | Required responsibility |
| --- | --- |
| Endpoint | Identifies a headquarters, formation or observer and its authorized recipients and senders |
| Link | Defines a permitted command, reporting or support path; the selected profile supplies its availability and delivery policy |
| Message | Stable identity, kind, sender, recipients, mission/order reference, payload provenance, issue cursor and supersession reference where applicable |
| Delivery | Per-recipient pending, delivered or explicitly failed/cancelled state, delivery cursor and any required acknowledgment |
| Recipient knowledge | Information available to that command from its permitted observations and delivered messages |

The initial policy assumes configured links remain available and eligible messages arrive immediately. Issue and receipt remain distinct recorded facts even at the same cursor. Acknowledgment, where required, confirms receipt rather than acceptance, execution or success. Delivery does not bypass RPh order eligibility, the Execution Gate or a Scenario Card's declared interaction rules. Later profiles may model means of communication, delay, congestion, interruption and restoration. Historical signal constraints motivate those extensions but do not supply numerical reliability or radius parameters. [H1, H4]

Maintain authoritative battlefield state separately from each command's knowledge. Reports carry observation provenance and the time of the reported event as well as delivery time. A report cannot refresh its underlying observation merely by arriving later. Intelligence can remain partial or stale. A shared player interface or planner must use the selected command's permitted view; military rank and access to a child scene do not grant omniscient information. The immediate-delivery baseline simplifies timing without removing these disclosure constraints.

Issued orders can be superseded or cancelled only through declared rules. Delivery retries and duplicate acknowledgments must not repeat execution, support commitments or parent outcome imports. If a later policy allows out-of-order arrival, explicit order versions and supersession references determine applicability; a late obsolete order cannot silently replace a newer one. Sending, delivery and handling are durable, replayable transitions, distinct from mission and engagement status.

When a profile models link or headquarters loss, the unit retains its roster, representation scale and spent actions. It continues its last valid standing order within a published local-initiative policy, including permitted defense and withdrawal. New coordinated orders and support requests require the specified path. The profile defines succession and treatment of already committed missions. Reconnection cannot replay completed orders, replenish budgets or grant recovery. Baseline tests establish delivery, disclosure and idempotency; delay, failure and succession tests apply when those mechanisms are enabled.

Communication records and scheduling belong to domain services composed by the Host. They use existing Runtime authority and persistence patterns. Domain message delivery is a simulated event, not permission to send real messages or to introduce a second execution path.

## 9. Historical modeling without false precision

Rottman's armored-infantry discussion distinguishes changing establishments, temporary tank-infantry combinations and practical coordination from published doctrine. It describes transport and supporting weapons at several echelons, and communication methods that cannot be represented by a universal radio-radius assumption. [H1, printed pp. 17-24, 30-31, 48-54]

The proposed historical model therefore records:

- period and theater, with formation-type-specific establishment;
- actual scenario composition and attachments separately from establishment;
- equipment capability separately from crew/training evidence;
- observed practice separately from prescribed doctrine;
- uncertainty and source disagreement, rather than invented exact values.

Imported Panzer counter data retains its original identity as reference data. Our unit catalog may use different capabilities or calibrated values, with the transformation and assumptions recorded. A historical gun specification alone does not determine a platoon's combat value. Changing the model creates a new published definition, not an invisible alteration to a running game.

The Advanced Panzer Blitz orders-of-battle material explicitly acknowledges invented data in one edition. It can suggest organization questions and candidate scenarios, but is not admitted as historical establishment authority without corroboration. The excluded promotional file remains outside the source set. [V1]

Model validation must address message delivery, recipient knowledge, command timing, cohesion and observation in the baseline, and transmission failures, detailed losses and sustainment as they are added. The present corpus is stronger for German equipment and U.S. armored infantry than for Soviet infantry command practice. The first bounded engagement should reflect that evidence imbalance rather than claim symmetrical coverage of every army.

## 10. Source authoring and rule admission

Follow the ASL authoring lifecycle: source registration, extracted fragments, structured rule candidates, deterministic checks, review, immutable publication, exact runtime resolution. Keep extraction and historical retrieval out of live adjudication.

For each imported source fragment, retain PDF hash, physical page or sheet, printed section, extraction/transcription identity and review decision. For each new rule, retain its authored specification, rationale, reference lineage, deliberate departures and validation fixtures. Tables require cell-level checks for headings, ranges, fractions, die modifiers and outcome codes. A source file being readable is not semantic admission; a design proposal is not executable until reviewed and published.

Maintain separate evidence classes:

| Evidence class | Permitted role |
| --- | --- |
| Original rulebook, chart or counter | Reference mechanic or data; binding only where our authored package explicitly adopts it |
| Errata or clarification | Specific correction or interpretation with identified authority and precedence |
| Scenario special rule | Local override within an identified situation |
| Historical study or period document | Historical model evidence and explanatory context |
| Authored simulation rule or design proposal | Primary semantics for our simulation after review and immutable publication |
| Player aid or extracted summary | Discovery and comparison; not independent authority |

Publish a capability manifest with each partial package. A scenario is playable only if every reachable required mechanic is supported, including exceptional branches. A synthetic direct-fire fixture is not advertised as a complete formation simulation or original-game compatibility.

Existing `DomainConclusion` evidence and rule references must match the question's package. Do not combine arbitrary ASL, Panzer and historical references into one conclusion that violates that contract. A compiled Panzer package can retain external source lineage in its authoring manifest while issuing canonical evidence in its own package scope. Cross-domain comparison uses separate conclusions, or a later explicitly admitted bridge package.

## 11. Implementation layout and dependency boundaries

Proposed initial layout:

```text
src/Panzer/
  docs/
  LimboDancer.Domains.Panzer/
    Catalog/  Maps/  Rules/  State/  Planning/
  LimboDancer.Domains.Panzer.Integration/
  tests/
docs/Panzer/
  SourceRegistry/  Schemas/  Fixtures/
```

`Panzer` depends on approved Abstractions and the existing dice utility where necessary. `Panzer.Integration` owns concrete runtime registration, constraints, executors, observations and infrastructure adapters. It may reference Runtime and the Panzer library. Host or a domain application composes these explicitly. Runtime and Abstractions must not reference either game domain.

The formation resolver and its exact package can initially live in one Panzer assembly. Legacy compatibility, if later requested, must use separate packages. Avoid both a copied ASL solution and an immediate proliferation of generic interfaces. Split projects for demonstrated dependency or deployment boundaries, not for every domain noun.

The current dice utility sits under `src/ASL` but has a domain-neutral namespace. Use it in place initially; relocation is a separate mechanical change with unchanged APIs and test evidence. A directory name alone does not justify copying the implementation.

Keep ASL file formats and package identities unchanged. Do not add a required Panzer field to existing ASL events. If shared geometry or persistence code is later extracted, first characterize ASL behavior, then prove equivalent results and dependency isolation with both domains.

## 12. User-facing command view

A Panzer application should reuse suitable visual components without turning the ASL Play page into a large game-family switch. Start with a separate route or application composition that selects the exact domain before setup.

The main view combines a map, a formation tree, selected-unit details, legal actions and pending commitments. Company summaries show subordinate counters, readiness under Section 5.4, transport and support assignments. Distinguish surviving strength from temporary effectiveness and remaining action commitments; show unknown detail explicitly. Selecting a company exposes its current-phase subordinate actions and order progress without changing the decision owner. Preserve access to each counter's actual values and rule explanation.

Show the eight-phase strip, active side and current decision owner, plus pending support, fire budgets, reserved approaches and objectives. Friendly artillery commitments are visible to their owner; enemy missions remain hidden unless observation rules reveal them. An agent proposal states intended action, known cost, unresolved consequences and rule basis. A preview does not reveal future dice or concealed information. The review and confirmation behavior follows existing Governance policy; grouping actions visually does not bypass individual authorization.

Display the active game/profile and enabled optional rules in ordinary terms. Distinguish a historical formation label from a rules-bearing counter, and recorded facts from estimates. This prevents a visually richer interface from implying more simulation detail than exists.

## 13. Scale, performance and evaluation

Platoon counters reduce some entity detail, while larger boards, combined attacks, many potential targets and historical information models create different costs. Do not assume zooming out automatically makes execution cheaper. Measure player decision workload separately from computational cost; neither a high squad count nor detailed ASL rules alone establishes an engine capacity limit.

Bound candidate generation by phase, side, supported capabilities and legal range. Maintain indexed positions, roads, formation membership and pending effects. Cache immutable board geometry and rule tables by exact content identity; cache LOS with all material terrain/profile versions. Never reuse perspective-dependent observations across tenants, sides or revisions.

Use deterministic domain calculations to generate a bounded set of legal action candidates before reasoning ranks them. Enforce the runtime's existing step/token/time/cost budgets. A company instruction cannot expand into unbounded autonomous play.

Proposed workload classes for measurement are 50, 200 and 500 active counters with short and long event histories. These are engineering test assumptions, not claims about original scenario sizes. Measure plan latency, projection latency, memory, event growth and candidate counts before setting acceptance thresholds. Optimize only after the deterministic reference implementation passes correctness tests.

## 14. One battlefield at formation and ASL scales

### 14.1 Design principle

A formation board should be expandable into ASL-compatible terrain and units. Design for this relationship now, while staging executable cross-scale play after both domains work independently. The formation view summarizes a battlefield and its forces; the ASL view resolves a selected region in greater detail. Expansion is a repeatable projection with explicit authored assumptions, not a fresh random scenario on every visit.

One parent formation scenario can decompose into several linked ASL scenarios. Each child resolves a bounded local engagement while the parent retains the overall mission, force roster, shared clock, support allocation and victory evaluation. The decomposition can evolve as movement, contact and player decisions create or remove opportunities. It is not a fixed partition into one child per board or formation hex.

There are two entry paths. A newly authored scenario can start with detailed terrain and force rosters, then derive its formation view. An imported Panzer board and counter set starts with less information: its first expansion must supply the missing terrain detail, composition and deployment. A town hex alone cannot tell us where individual buildings stand, and a platoon label cannot establish its exact squads or remaining tanks. Store those additions as scenario assumptions with provenance. They are not recovered historical facts.

This changes the earlier bridge concept: multiscale identity is a foundational data requirement, although transferring live adjudication remains a later implementation stage. Existing ASL packages stay independent. Shared scenario records and a mapping package connect the two domains without adding formation semantics to Runtime.

Scale changes preserve military identity and recorded consequences while changing the decisions presented to the player. They do not require continuously running every squad's ASL procedures beneath formation play. Inspection is state-preserving; live resolution uses the admitted domain for that interval. Calibration should compare movement, losses, suppression and objective outcomes under comparable conditions across repeated trials, rather than claim identical combat results from different resolvers or identical random seeds.

### 14.2 Expanding the board

Use the campaign geographic reference defined in Section 18. Engagement-local frames map into that reference through pinned transforms. Both tactical grids use explicit ground scale, origin and orientation; local coordinates do not create a separate world. Keep rendering pixels separate, especially because the existing ASL board design documents non-regular display hex geometry.

At the initial scales, 250 / 40 = 6.25 is the linear ratio, and its square is approximately 39.1 in area for similarly shaped hexes. Thus one formation hex covers roughly 39 ASL hex areas. It does not map to a neat six-by-six block or a whole ASL board. The grids do not nest exactly; ASL hexes can cross formation boundaries. Use polygon overlap to summarize terrain, and the unit's precise position to determine its formation-cell membership. Never duplicate a unit because its location overlaps a boundary.

Expand a contiguous region into one continuous detailed terrain scene, then tile that scene into ASL boards or board-sized display panels. Roads, rivers, ridgelines and settlement footprints cross panel boundaries consistently. Independent generation inside each parent hex would create road discontinuities and arbitrary terrain seams.

The terrain model separates:

- **Fixed scenario facts:** known roads, crossings, major watercourses, elevation structure, settlement areas and objectives.
- **Authored detail:** individual buildings, hedges, minor tracks and detailed ground cover where evidence is absent.
- **Persistent changes:** damaged bridges, wrecks, rubble, fortifications and other effects produced during play.

Imported coarse terrain constrains detailed authoring. For example, expanding a wooded hex should preserve its intended cover and access characteristics, not quietly introduce a highway through it. Fine terrain is summarized back into coarse coverage, mobility and observation features. If the detailed scene contradicts an important coarse property, resolve that discrepancy during scenario admission rather than changing terrain when a player zooms.

Dynamically assemble each ASL engagement map from its selected formation-map region, preserving established terrain, connectivity and geographic identity. Generate missing detail under the campaign Map Seed and pinned generation rules in Section 18, then construct ASL hexes, hexside features and locations through the existing terrain compiler and board-package validation. Stock ASL boards may supply compatible reusable material; display panels do not define simulation boundaries. Persist the generated artifacts and bind their versions to the Scenario Card. The same constrained generation approach applies to formation, operational and regional maps.

### 14.3 Expanding the forces

Each formation unit receives a stable `ForceRoster` identifying represented subunits and equipment. The roster separates dated establishment, scenario-authorized starting composition and current survivors. A platoon expands into the admitted squad/half-squad, leader, support-weapon, gun, crew and vehicle instances that its roster specifies. A company expands through its subordinate platoons and attached elements; the company itself never creates an extra combat unit.

Roster values require explicit scenario decisions. ASL leadership ratings, squad quality and equipment availability cannot be calculated reliably from a Panzer attack factor. Mapping them needs reviewed unit definitions, date/theater evidence and declared assumptions. Likewise, a counter annotated as representing five tanks expands into five individual vehicles only if the scenario roster actually establishes five available tanks.

Every detailed instance keeps its force identity and parent lineage. Crew, passengers, carriers and attached weapons remain separate linked objects, preventing duplication during expansion. Deployment uses a persistent `DeploymentRecord`: occupied area, posture, facing, route progress and actual fine positions once materialized. The parent hex is a coarse location report, not an instruction to place every squad and tank at its center.

If only aggregate deployment exists, generate or author a legal detailed deployment consistent with that footprint and posture before the engagement starts. Record the result and its assumptions. Store the deployment generator version, its reproducibility inputs and resulting immutable artifact. Deployment randomness is separate from terrain generation: a different mission or force assignment cannot reseed the campaign geography. Repeated expansion retrieves that deployment. It cannot reroll cover, leaders, weapons or surviving vehicles.

### 14.4 Detail ownership and information

Maintain a scenario registry with `TerrainRegion`, `ForceRoster`, `DeploymentRecord` and persistent material changes. Terrain records belong to the campaign geography registry in Section 18 and are referenced by scenarios. Fine geometry may exist from campaign authoring or be materialized on demand under its pinned manifest. Each region records which representation currently adjudicates its state and the exact revision of its last cross-scale mapping.

Coarse resolution must preserve enough detail to support later expansion. When a formation-scale result specifies actual equipment losses, select and record the affected roster assets under an admitted allocation rule at resolution time. If a result only establishes lost combat effectiveness, record that condition without inventing deaths or vehicle destruction. First expansion must explicitly translate any unresolved aggregate condition through a versioned rule. It may not postpone casualty allocation until a player can inspect alternative favorable deployments.

Disorganized or suppressed platoons need a declared distribution of detailed conditions. They do not automatically become uniformly broken ASL squads. In the other direction, a broken squad, an abandoned vehicle and a killed crew convey different facts even if they contribute to the same coarse readiness summary. Preserve the detailed facts alongside the summary.

Zooming changes resolution, not knowledge permissions. An enemy contact at company scale does not reveal every hidden squad when the map opens. The adjudicator can possess full terrain and roster detail while each side receives only its permitted ASL observation. Conversely, local discoveries reach company-level contacts only under the communication/disclosure rules. Whether minor terrain is publicly known or discoverable is an explicit scenario policy.

### 14.5 Three different uses of expansion

| Mode | What happens | Effect on the parent battle |
| --- | --- | --- |
| Inspect | Show existing or admitted detailed terrain and the viewer's permitted forces | No combat, movement, recovery or simulated time advances |
| Explore a branch | Copy a pinned state into a separate ASL what-if engagement | No automatic return; speculative results never change the live battle |
| Resolve in ASL | Transfer adjudication of an agreed region, force set and interval to a linked ASL engagement | One governed, idempotent reconciliation of actual outcomes |

The first executable handoff uses a game-turn boundary with no unresolved attacks or reactions. Pause normal parent advancement while coordinating that interval. A region cannot be independently played while the parent simultaneously moves reinforcements into it or fires the same artillery. The data model supports several children immediately; the first executable slice resolves one child at a time. Mid-phase handoffs and dynamically interacting concurrent engagements require a more advanced scheduler and are deferred.

An `EngagementContract` records the starting-state revision and the admitted reservation revision defined in Section 14.8, region and surrounding influence area, participating roster IDs, ASL package and board identities, objectives, information policy, start/end cursors, external support, entry/exit rules and reconciliation version. Include enough surrounding terrain to represent approaches and fire into the region, not just the selected objective hex. Outside forces either join that scope or act through explicit, budgeted support/arrival events. Never invent an unlimited off-map fire source.

At the nominal scales, three complete ASL game turns are a candidate interval for one six-minute formation game turn. This is a scheduling proposal, not proof of equivalent action rates or movement. Calibration must show that the mapping does not give transferred forces extra fire, recovery or distance. Transfer at a full-turn boundary, not from the formation APh into a fresh ASL RPh that grants an extra turn of actions. An early local victory does not reset the interval; represent the remaining time explicitly before reconciliation.

Resolve all children and the remaining formation-scale activity for the interval under pinned boundary contracts before publishing its synchronized boundary. Processing one child first does not place it earlier in simulated time. Do not reveal its results to another child or outside decision-makers before their information-arrival time. If a previously unmodeled cross-boundary interaction is required, stop at a safe checkpoint and revise or enlarge the contract before proceeding. Until interval coordination exists, the first live experiment should encompass the whole active engagement, leaving inspection and isolated branches available for smaller regions.

### 14.6 Returning the outcome

Return an `EngagementOutcome` containing surviving roster identities, detailed positions, conditions, equipment and crew status, material terrain changes, control and disclosed contacts, plus consumed support and elapsed interval. Reconcile these facts through the mapping package. Do not translate an ASL result into a single Panzer elimination/dispersal roll and discard the underlying survivors.

If a platoon's survivors end in different formation hexes, create linked detachments or represent a multi-cell footprint. Do not teleport them together to recover one convenient counter. Recombining them later requires actual movement and reorganization. Company totals always count the underlying roster once, regardless of how many display markers represent it. Detachments inherit the activity-group and constituent ledgers in Section 6.2; reconciliation validates both roster conservation and entitlement conservation.

The parent treats the child interval as already adjudicated for those assets. Their movement, fire, recovery and support expenditures cannot also be executed at formation scale. Store each completed child outcome durably under its engagement and contract IDs; completion alone does not advance the parent. The interval coordinator validates all required outcomes against the common starting snapshot and admitted interval contract, then compares the live parent revision with the reservation revision and commits one causally complete reconciliation batch through the Execution Gate. A retry returns the prior result. Failed or abandoned sessions preserve an explicit pending state; restoring a pre-engagement snapshot must be a declared branch or rollback, not a concealed reroll.

### 14.7 Several ASL scenarios within one parent battle

An `EngagementPlan` is a versioned graph of candidate and active local engagements. Dependency edges identify prerequisites and transfer conditions. Concurrent children share an interval rather than a completion dependency. A continuing battle keeps its engagement identity across intervals; later phases do not create cyclic prerequisites. An engagement can span several intervals, producing checkpoints until its local objective or termination condition is reached.

| Relationship | Example | Coordination requirement |
| --- | --- | --- |
| Sequential | Secure an approach, then attack the installation beyond it | Success enables a successor; survivors enter only after recorded movement and elapsed time |
| Concurrent | Attack separate signal installations during the same interval | Disjoint asset ownership and compatible boundary contracts; no result leakage between children |
| Conditional | Intercept reserves if they take a particular road | Activate from committed parent facts, not a planner's prediction |
| Continuing | Withdraw survivors into a second defensive position | Preserve identities, losses, equipment, conditions and physical transit |

The parent owns global objectives, reinforcement pools, weather/time policy and shared support. A child receives a local objective, bounded force assignment, terrain and permitted external effects. Neither a company commander nor an ASL scenario generator can create new parent resources. Child completion contributes facts such as an installation destroyed, a route opened or a reserve delayed. Winning a majority of child scenarios is not a parent victory rule.

An engagement boundary follows tactical influence, not the edge of an image. If two fights permit direct fire, immediate movement or a shared unresolved terrain interaction across their boundaries, combine them into a larger ASL scene or admit an explicit interaction protocol. Non-overlapping map areas alone do not prove independence. Long-range support can remain outside the scene only through declared missions with an owner, target, time and resource cost.

Reserve each roster asset for at most one adjudicator at a time. Shared artillery and other support use a parent allocation ledger, with unique mission IDs and consumption intervals. Transfers between children require a departure event, physical travel, an arrival condition and an explicit change of ownership. A tank leaving one child is unavailable to another until it can actually arrive. A platoon split between engagements is represented by disjoint constituent rosters, not two copies of the parent counter.

### 14.8 Interval coordination and recovery

The proposed coordinator belongs in the domain integration layer. It uses ordinary registered actions and existing Runtime authority, not an independent execution path. Its first lifecycle is:

1. **Plan:** identify candidate engagements, local objectives and boundaries from the parent state and reviewed scenario rules. Candidate plans grant no authority and can be revised freely before admission.
2. **Admit and reserve:** require the issued Scenario Card and operational bindings from Section 14.12; pin their identity and hash. Validate card completeness, executable capability coverage, dependencies, influence boundaries, force exclusivity, shared support and information policies against `StartRevision`. Atomically append reservations and the interval contract, producing `ReservationRevision`. Record the plan version and starting snapshot identity. The resulting reservation revision, not the earlier starting revision, becomes the expected parent revision for reconciliation.
3. **Resolve:** freeze normal writes to the parent game stream at `ReservationRevision`. Run admitted children and the remaining coarse activity for the agreed interval from the starting snapshot constrained by the admitted reservations. Each child uses its own revisioned log; coarse activity uses an interval-local provisional log. Both are durable execution records under registered actions, but neither advances the live parent revision. The first implementation processes them sequentially while preserving their common simulated start/end times.
4. **Stage outcomes:** persist a checkpoint or final outcome for every participant through the agreed interval end. An engagement may remain ongoing while its current interval is complete. Only an incomplete interval, an unresolved choice/effect due within it or required boundary repair blocks reconciliation. Ready sibling outcomes remain staged across restart and are not replayed with new dice.
5. **Reconcile:** verify complete interval coverage, all participant checkpoint/log hashes, no duplicated assets/effects and consistent shared facts. Build and validate the candidate parent projection before commit. Through the gate, compare against `ReservationRevision` and atomically append the outcome batch, resource consumption, continuing reservations and next boundary. Record the interval ID and imported participant ranges exactly once. Only then release resources or activate eligible successor engagements.

Keep engagement lifetime and interval readiness separate. `EngagementStatus` can be ongoing, completed or explicitly terminated; `IntervalStatus` progresses through resolving, ready and reconciled, with blocked for required repair. An ongoing engagement becomes ready when its checkpoint reaches the contracted end cursor with all consequences due through that cursor resolved. Its future missions and continuing close combats remain recorded pending state, not reasons to block the completed interval. Early local completion must still account for the rest of the interval through an explicit hold, withdrawal or termination rule; it cannot leave an unadjudicated time gap. No child executes its next interval until parent reconciliation admits it.

Staged outcomes, checkpoint references and coordinator progress belong to the interval record outside the frozen parent game stream. Their durable updates have their own revision checks. Lifecycle operations that must change parent reservations, including cancellation or boundary repair, explicitly invalidate the old reservation revision and require a new admitted contract before further play. Preserve committed child history during that repair; never reinterpret it under a new contract or reroll it automatically. Unexpected parent changes make reconciliation fail closed. Resume only after an explicit compatible re-admission or a separately identified branch.

Independent children with the same input boundary should produce the same reconciled result regardless of processing order, using their respective recorded random streams. Conflicting outcomes do not resolve by last-writer-wins. They reveal a failed boundary or ownership assumption and require explicit repair before parent advancement.

Sequential processing must not provide hindsight. Keep unrelated child observations isolated and commit shared allocation decisions before exposing outcomes. Where a human controls multiple concurrent children, lock decisions that cannot legitimately depend on the first child's result, or coordinate play in shorter synchronized intervals. Merely labeling two sessions concurrent does not remove the information advantage of having watched one finish.

Do not cancel an unfavorable completed child and regenerate it. Cancellation before play releases reservations under an explicit rule; interruption after committed play requires a checkpoint, reconciliation or an explicitly separate exploratory branch. A live parent revision different from `ReservationRevision` blocks the whole interval commit. Recover from durable plan, reservation, child-log and staged-outcome records without silently changing force ownership.

### 14.9 Scenario-driven decomposition from the corpus

The corpus supplies four complementary inputs. Preserve source identity and distinguish directly specified facts, reference-derived interpretations and authored assumptions in every decomposition record.

| Input | What it establishes | What still requires an explicit choice |
| --- | --- | --- |
| Scenario card | Counter identities/counts, date, setting, board arrangement, deployment, special roles and victory conditions | Detailed local engagements, adapted objectives and support/arrival contracts |
| Counter art and unit composition charts | Represented echelon, personnel, weapons and equipment where stated | Actual scenario strength, ASL unit definitions, leadership and deployment |
| PanzerClass articles and function charts | Rating abstractions, embedded capabilities and exceptional uses | Historical corroboration, applicable edition and adopted mapping rules |
| Boards and terrain rules | Terrain categories, roads, crossings, elevation relationships and objective geography | Fine buildings, fortifications, detailed contours and other absent geometry |

The mapping key must include counter definition, source edition, date/theater and scenario role. It cannot be a universal conversion from attack factor to squad count or ASL firepower. Arvold describes infantry attack ratings as emphasizing anti-armor capability and explains that some counter names stand for several organizational uses. Light mortars can be embedded in infantry; fort counters can represent several types of defensive position. Those capabilities must be allocated once, with no duplicated organic weapon or fortification. His articles contain interpretive reconstruction as well as concrete descriptions; attribution and uncertainty remain visible. [C1-C4]

Composition charts provide stronger count-level inputs where available. The inspected Polish chart describes a four-gun 37mm Bofors unit and detailed infantry equipment, but does not itself determine ASL leader ratings or support-weapon representation. Function charts describe game capabilities, not necessarily establishment; the inspected PanzerBlitz Master Unit Function Chart explicitly assumes Panzer Leader rules with noted differences. [C5, C6]

The conversion pipeline is: scenario admission, counter/role resolution, reviewed constituent rosters, continuous detailed terrain, candidate engagement boundaries, local objectives and resource contracts, then validation and publication. Generate only the detailed regions required for admitted engagements, while preserving persistent detail for later visits. A scenario may be adapted from a historical setting or explicitly hypothetical; preserve that distinction.

### 14.10 Situation 1 as the first decomposition exercise

The inspected PanzerBlitz Situation 1 card identifies a rear-area raid in White Russia in July 1944, uses boards 1-3, places the German defenders on board 2 and states that its CP units represent signal elements. Three CP units must occupy forts; two forts occupy the numbered hilltop hexes 129 and 132. CP units count as five units for the original destruction-based victory conditions. These are scenario facts, not a generic definition of every CP counter. [S1]

Start with one defended signal-installation sector as a bounded authoring and handoff exercise. The complete parent could later activate several children: attacks on separate installations, a fight over an approach and a reserve interception if committed movement produces one. These child missions are proposed adaptations, not additional engagements asserted by the source. Do not assign enemy reinforcements or precise installation garrisons that the card has not established without recording the design decision.

The installation's objective identity and parent value persist across children. Splitting its garrison into ASL squads does not multiply victory points, and two children cannot each claim destruction of the same installation. Define physical success conditions and parent contribution in the mapping package. Original destruction thresholds are reference material to adapt deliberately, rather than automatically counting every detailed casualty as a destroyed formation unit.

The two visually inspected Imaginative Strategist scenarios also supply deployment boundaries, forces, board arrangements and dates, but label their engagements hypothetical. They are further decomposition candidates, not proof of those exact historical orders of battle. [S2, S3]

### 14.11 Local outcome example and acceptance criteria

A rifle platoon and a tank platoon approach a village on the formation board. The admitted scenario roster establishes three rifle squads, a particular leader and assigned support weapons, plus four surviving tanks. Those are example scenario choices, not a universal platoon establishment. Selecting the village and approaches opens linked ASL terrain panels containing the same road, stream crossing and settlement footprint, with detailed buildings already authored or admitted on first expansion.

The ASL engagement starts with those exact assets in their recorded approach positions. Suppose it ends with one squad broken, one tank destroyed and another immobilized. The coarse view retains those outcomes: the destroyed tank is removed from the available roster, the immobilized tank remains at its physical location, and the broken squad contributes to a reviewed cohesion assessment. The village's control follows the objective rule. Opening the ASL view again shows those survivors and material changes, with no regenerated tanks or restored readiness.

Required bridge tests include: unchanged expand/collapse preserves state; repeated expansion preserves detail; terrain connectivity survives panel seams; roster and equipment totals are conserved except for recorded events; fine positions aggregate without teleportation; hidden contacts remain hidden; interval budgets are spent only once; neighboring effects cannot cross an unhandled boundary; and reconciliation is deterministic and idempotent. Authoring-time aggregation need not be mathematically invertible, but returning from an unchanged detailed state must not create or lose information that was already known.

### 14.12 Scenario Cards as the ASL mission contract

Every ASL-level mission must be expressed through the Scenario Card model, whether standalone, imported, generated from a parent mission or explored in a separate branch. Higher headquarters assign subordinate missions; planning and contact determine which become ASL engagements. One mission can generate several cards over time. Missions resolved without ASL execution need no artificial combat scenario.

Reuse the existing card reader, validation and setup path. The current ScenarioCard describes boards, turns, sides, order-of-battle groups, setup/entry, special rules and structured victory conditions. ScenarioCardReference records an ID and SHA-256. These are implementation anchors, not evidence that operational bindings already exist. The existing minimal-card mode remains available for standalone manual play; it cannot bypass completeness and capability checks for linked missions. [A1]

Extend the existing model through a versioned operational context. Exact field names and storage layout remain implementation decisions.

| Existing card content | Required operational binding |
| --- | --- |
| Situation and introduction | Parent operation and mission IDs, issuing command, intent and contribution to the parent objective |
| Boards and setup | Campaign geography manifest, region and generated map hashes, terrain-change revision, deployment snapshot, entry/exit constraints and influence boundaries; no independent terrain seed |
| Order of battle | Stable roster-to-counter bindings, admitted starting conditions, attachments and allocated support |
| Turns and reinforcements | Shared-clock interval, first side, arrival dependencies and behavior after early local completion |
| Victory conditions | Executable local criteria and mapping from recorded facts and occurrence times to parent assessment |
| Special rules | Published external-effect and amendment policies, exact rule/package versions and supported capabilities |

An issued card is an immutable mission definition. The EngagementContract pins its ID, version and content hash and binds it to admission, reservations, temporal ownership and reconciliation. Card bindings and contract references use one authoritative assignment snapshot; independently editable copies of forces, objectives or timing are prohibited. Both hashes belong in execution provenance. A mismatch blocks admission. The setup adapter can attach persistent identities and conditions through validated bindings, but cannot introduce unrecorded forces or instructions outside the card and its declared rules.

Admission verifies playable terrain, complete force assignments, starting conditions, setup legality, interval coverage, executable victory criteria and every required special rule. A parseable card is insufficient: display-only or unsupported rules cannot silently govern linked play. Missing capability returns an explicit authoring or implementation requirement. Preserve existing standalone cards through a compatible versioned extension.

Reinforcements, support and changed orders enter through the issued card's declared mechanisms. An amendment outside those mechanisms creates a new version, effective cursor and governed transition; validate it against committed history and reservations at a supported boundary. Never overwrite the original card or reinterpret earlier actions. Reservation changes also require re-admission under Section 14.8. Continuing engagements preserve their identity and checkpoint across intervals rather than restarting from fresh cards.

Card views obey disclosure policy. A briefing must not expose an opponent's hidden roster, setup, future arrivals or parent orders merely because the adjudicator's card contains them. The present standalone setup workflow is not proof that protected linked setup exists; establish that capability before admitting missions that require it.

Outcomes report facts with occurrence times, roster lineage and card/contract identities. Local victory is separate from parent success: capturing a bridge after the operation's deadline can satisfy the local card while failing the parent mission. Reports convey only information available to their sender at the applicable delivery time. Authoritative reconciliation does not automatically give every headquarters knowledge of the result.

## 15. Delivery sequence and acceptance gates

| Stage | Deliverable | Evidence required to proceed |
| --- | --- | --- |
| 0. Authored baseline | Publish the Section 6 phase contract, initial numerical rules, source/departure register and a small designed engagement; establish terrain/roster lineage for later ASL expansion | Reviewed phase ownership, budgets, recovery, withdrawal, observation and combat fixtures; unsupported capabilities named |
| 1. Read-only formation slice | Unit definitions, a verified board and direct-fire adjudication | Legal/illegal/unknown cases with authored rule and evidence references; no mutations |
| 2. Governed turn cycle | Setup, all eight phases, baseline message issue/delivery and recipient views, basic movement/fire, reserve/withdrawal/assault and boundary state | Event replay, expected revision, idempotency, transactional dice and lifecycle tests |
| 3. Reactions and support | Interruptible movement, delayed support and condition/control consequences | No double fire, no free advance, no silent retarget, correct cancellation and crash recovery |
| 4. Complete bounded engagement | One company-scale combined-arms engagement traversing the entire cycle | Capability closure, scenario objectives, transport and information discipline; verified complete-game replay |
| 5. Formation planning | Company view, persistent orders and bounded intent-to-action proposals | No extra authority; no information leaks; stale plans re-observe; objective progress reported honestly |
| 6. Calibrated historical fidelity | Evidence-supported force and measured model improvements | Parameter provenance, uncertainty, sensitivity analysis and versioned behavior changes |
| 7a. Scenario decomposition | An inspectable EngagementPlan and validated Scenario Cards for a parent scenario, beginning with one Situation 1 installation sector | Source/assumption separation, complete card bindings, roster lineage, terrain continuity, local objectives and reviewed interaction boundaries |
| 7b. Bounded ASL handoff | One child engagement with persistent outcome and parent return | Conservation, no double simulation, temporal ownership, visibility and retry/recovery proofs from Section 14 |
| 7c. Linked ASL engagements | Multiple children with sequential dependencies and independently resolvable shared intervals; process one child at a time initially | Exclusive asset reservations, support accounting, staged outcomes, synchronized parent commit and no processing-order information advantage |

After Stage 7c, prove a bounded battalion operation with several company missions, shared support, a reserve, report delivery and one supply constraint before adding division or higher domains. This later acceptance gate does not expand the formation baseline.

The first implementation slice should complete Stage 0 and one Stage 1 adjudication fixture. Use a small designed engagement whose units and terrain exercise the authored rules. Original Panzer situations may inspire the setting and forces, but adaptation must account for our reaction, recovery, advance and observation rules. Legacy compatibility is not an acceptance gate.

### 15.1 Required conformance cases

1. The same state and recorded rolls replay to the same result, including delayed effects and interrupts.
2. Duplicate and stale actions neither append twice nor draw additional dice.
3. A published conclusion cannot be passed as execution authorization; due effects also require a new gate pass.
4. A crash/restart during a pending fire mission or reaction window preserves the decision still owed.
5. A support mission declared in PFPh is due in its owner's next PFPh; retargeting, cancellation, resource loss and empty target areas follow Section 6 without duplicate budgets.
6. MPh, DFPh and APh share one defensive fire commitment; passing preserves it, firing spends it, and no phase transition replenishes it.
7. Prep Fire blocks voluntary movement/advance; AFPh does not duplicate offensive fire; advance consumes its declared remaining movement and cannot turn into an alternate move.
8. Carrier/passenger constraints and aggregate attack values do not double-count composition or formation membership.
9. Hidden state cannot be inferred from previews, reasons or legal-action discovery beyond the profile's allowed information.
10. Missing terrain, unknown rule coverage, wrong package versions and conflicting interpretations refuse or remain indeterminate with precise evidence.
11. Runtime builds and core conformance tests remain independent of ASL and Panzer; existing ASL behavior and serialized files remain valid.
12. Scenario victory follows the exact situation conditions, not the planner's declaration that its objective was achieved.
13. Every player turn traverses the eight ordered phases, including empty ones; interrupts preserve active side and restore the suspended action exactly once.
14. Recovery is limited to once per unit per game turn; suppression/other conditions expire only at their declared cursors, including across restarts.
15. Rout processes active side before defender, uses permitted knowledge and resolves trapped formations; failed assault approaches retain the correct origin and spent cost.
16. Close-combat choices resolve from one engagement snapshot; serial event ordering does not remove a committed simultaneous attack.
17. A/B role switches and the complete game-turn boundary reset only the appropriate budgets; victory and pending support termination are not skipped.
18. One roster asset cannot belong to two adjudicators in the same interval; child transfers preserve physical travel, surviving strength and equipment.
19. Two children cannot consume the same support mission or claim the same parent objective twice; decomposing a garrison does not multiply its objective value.
20. Resolving independent concurrent children in a different processing order produces equivalent parent state using the same per-child recorded rolls; no child receives premature sibling results.
21. Restart after one child reaches interval readiness preserves its staged result and remaining reservations; the parent advances only when all required interval outcomes are complete and consistent, even if some engagements remain ongoing.
22. Cross-boundary fire or movement outside admitted contracts blocks independent resolution; conflicts cannot be hidden by the order of parent reconciliation.
23. Sequential child activation uses committed prerequisites and feasible arrival times, while continuing engagements carry forward their state without resetting actions or recovery.
24. Admission advances `StartRevision` to `ReservationRevision`; child, staging and provisional coarse writes do not advance the parent. Reconciliation succeeds against the reservation revision, imports each outcome once and refuses an unexpected parent change.
25. Splitting and recombining a formation preserves activity-group fire/recovery commitments and constituent movement expenditure. A spent entitlement cannot be restored by new counter IDs, and multiple children cannot each receive the full original capability.
26. APh reaction observation, LOS, range and cover use the recorded origin. Pre-entry hazards or reactions that invalidate eligibility cancel entry without refund; destination hazards apply only after entry and cannot roll it back. Each hazard and cost applies once across retries.
27. Invalid candidate projections never enter authoritative game history or expose random results. Faulted attempts do not silently reroll, and uncertain commit outcomes are recovered by attempt ID before retry.
28. An ongoing engagement at a complete interval checkpoint permits reconciliation; a finished engagement without coverage through interval end does not. Future effects survive into the next admitted interval without resetting conditions, missions or budgets.

29. For an admitted constituent roster, suppressing and recovering a squad changes availability without changing surviving strength; destroyed assets remain lost. Aggregate-only conditions do not invent constituent casualties.
30. Removing an attached support weapon changes only capabilities justified by the published projection; summaries do not multiply aggregate attack factors, double-count attachments or expose hidden enemy conditions.
31. A company movement plan pauses for every required reaction, rechecks eligibility after consequences and preserves the common phase cursor and action ledger; selecting another company grants no new entitlements.
32. Baseline issue and per-recipient delivery remain recorded even under immediate delivery; duplicate messages and acknowledgments cannot repeat actions or reveal information to unintended recipients. Profiles with link loss preserve standing orders and local defense; succession and reconnection neither repeat orders nor reset budgets.

33. Every ASL mission starts through a validated Scenario Card with pinned identity and operational bindings. Missing cards, mismatched assignments and unsupported mandatory rules block admission.
34. Card amendments preserve issued versions and committed history, take effect only at an admitted cursor and cannot duplicate reinforcements, support or reservations.
35. Local victory after a parent deadline retains its actual occurrence time and is assessed under parent criteria; card views and report delivery do not disclose hidden or premature information.
36. Subordinate missions preserve lineage and exclusive resource ownership across command levels. Repeated receipt, execution or reporting cannot duplicate tasks, actions or outcome imports.

37. Recipient knowledge changes only through permitted observations and deliveries; event time and report arrival time remain distinct, and a delayed report does not become a fresh observation.
38. Receipt or acknowledgment cannot execute an order outside its phase or card contract. Profiles allowing reordered delivery reject obsolete superseded orders without undoing committed actions.

39. The same campaign location regenerates identical base terrain from pinned inputs across save/load, supported machines, different request extents and generation orders. Changing combat or deployment random streams cannot change terrain.
40. Overlapping and adjacent requests agree on shared features and boundaries; operational, formation and ASL views preserve feature identity and connectivity without requiring identical grids or visual detail.
41. Different Map Seeds can vary admitted missing detail while preserving fixed historical constraints. Reusing the same seed and pinned world inputs reproduces the same base; a new campaign ID alone does not change it.
42. Campaign damage survives regeneration and zoom changes. New campaigns do not inherit that damage; branches inherit the original geography and existing event history unless explicitly created as new worlds.
43. Missing pinned generator or source versions block regeneration rather than silently substitute newer versions. Existing verified artifacts remain identifiable and usable where supported.

44. The Europe view remains a clean hex map with essential geography and selective overlays. Hiding labels or fine features does not change route/crossing connectivity, lower-scale generation or world state.

These are future implementation acceptance tests. No new simulation implementation or runtime test execution is claimed by this design document.

## 16. Decisions still needed

The proposed eight-phase order, acting sides, fire budgets, advance reservation and support timing are specified in Section 6. Before executable publication, settle numerical movement/fire/recovery tables, suppression durations, withdrawal allowances, observation and capture criteria, initial unit/board definitions and the first army/date/theater. Validate the six-minute/250-meter baseline against those rules together. These are calibration and rule-completion tasks, not a requirement to follow either legacy Panzer sequence. Before autonomous play, settle control permissions within existing Governance.

Before admitting constituent readiness mechanics, settle the condition-to-capability projection, allocation of coarse losses and handling of unknown constituent state. These rules must preserve detailed facts and the existing action ledger. Communication identities, permitted paths, delivery transitions, disclosure and order applicability are baseline requirements. Quantitative delays, transmission failures and succession under disruption remain profile-specific fidelity decisions.

Terrain and force identity must support ASL expansion from the initial scenario design. The parent-to-many-child EngagementPlan and reservation records should be designed now; live scheduling and condition conversion follow Section 14's staged implementation. Before linked live play, settle safe influence boundaries, interval granularity, cross-boundary interactions, human information controls and parent objective mappings. No decision is currently needed on distributed simulation, dynamic domain plugins, a universal war-game ontology or a new database. Evidence from bounded formation engagements should determine whether those become necessary.

## 17. Command hierarchy and mission lifecycle

### 17.1 Architecture across echelons

The existing ASL combat engine is the tactical resolution foundation. Higher command architecture organizes hierarchy, forces and communication around admitted engagements, with time and sustainment as shared constraints. Reuse remains subject to verified capability coverage and performance; it does not imply that every theater engagement can already be resolved or continuously simulated at squad detail.

Keep organizational echelon, simulation resolution and player role independent. A division commander can inspect an admitted company engagement without acquiring instantaneous control of each squad or its hidden information. The eight-phase formation sequence remains the baseline in Section 6. Higher domains need separately published decision cycles and timing; they do not inherit ASL phases solely because subordinate battles use ASL.

| Pillar | Domain responsibility |
| --- | --- |
| Hierarchy | Command relationships, mission delegation, objective ownership and assessment |
| Organization | Persistent force identity, composition, attachments, reserves and support assignments |
| Communication | Orders, acknowledgments, reports and requests with source, destination and delivery state |
| Time and sustainment | Feasible movement and arrival, consumption, supply, maintenance and recovery |

Organic membership, tactical control, support, supply and communications are distinct time-bounded relationships. Validate permitted structures by army, period and formation type; do not require every command to have three children or include every echelon. Military command authority never substitutes for Runtime authorization.

Higher formations may occupy regions and corridors with dispersed subordinates, headquarters and supply routes. Their display marker summarizes those dispositions. Preserve known identities at the finest admitted resolution; missing detail requires explicit authoring with conservation and provenance before expansion. Continuous squad-level simulation is not a prerequisite for maintaining an organization.

### 17.2 Mission lifecycle

A proposed Mission record carries identity and parent lineage, issuing and executing commands, objective, constraints, allocated resources, dependencies, deadline and assessment criteria. A FormationOrder carries executable direction within the selected profile and can reference a mission. A mission may produce several subordinate missions and ASL Scenario Cards; it is not synonymous with one battle.

The lifecycle is assignment, receipt, planning, execution, reporting and assessment. For an ASL task, card authoring, validation and engagement admission occur between planning and execution:

1. **Assignment:** record intent, permitted resources and success criteria. Assignment alone neither moves forces nor reserves the same asset in several children.
2. **Receipt:** deliver the order under the current profile and record receipt or acknowledgment as required. The formation baseline records immediate delivery through Section 8.3's core lifecycle; later fidelity profiles may introduce transmission delay and failure.
3. **Planning:** create bounded subordinate tasks, dependencies and support requests. For every ASL task, produce and validate its Scenario Card, then admit the EngagementContract and reservations through Section 14.8.
4. **Execution:** perform authorized actions under the pinned card or higher-domain rules. Interruptions, continuing engagements and revised orders preserve committed history.
5. **Reporting:** send progress and results with observation provenance, occurrence time and delivery state. Reports can be partial or delayed and differ from the adjudicator's complete outcome.
6. **Assessment:** evaluate parent objectives and deadlines from authoritative facts, while commander decisions use only delivered information. Record continuation, completion, failure, cancellation or supersession explicitly.

These responsibilities can overlap: progress reports and replanning occur during execution. Keep mission status, order delivery, card version, engagement lifetime and interval readiness distinct. Cancellation after combat begins must reconcile committed effects; it cannot release spent resources or erase losses. Persistent identities and idempotent transitions prevent duplicate orders or reports from repeating execution.

### 17.3 Coordination and the next proof

The first higher-level proof is a battalion operation to secure a crossing: one company attacks the approach, another protects the flank and a third remains in reserve. Subordinate missions generate complete ASL cards when tactical execution is required. Outcomes determine crossing availability, losses, routes and feasible reserve arrival. The battalion objective and deadline govern overall success; a majority of local victories does not.

Use Section 14.8's bounded synchronization first. A later operational coordinator may let independent regions advance within a common clock, but interacting regions must synchronize before dependent movement, supply or fire resolves. Road movement cannot assume an unresolved crossing is available. Avoiding indefinite theater-wide pauses while preserving local reservations requires a later scheduler, not a change to the initial contract.

Progress toward regiment/brigade, division, corps, army and theater introduces wider objectives, transport capacity, supply allocation, replacement and recovery, and competing operations. Each added mechanic needs an explicit domain profile and evidence. Shared supplies need quantity, location, ownership and transit constraints, with consumption recorded once. A force roster alone does not establish executable logistics.

Keep military models and coordination in domain packages composed by the Host. Reuse Runtime evidence, lifecycle, governance and execution contracts. Extract shared military components only when concrete domains demonstrate common behavior; this direction does not authorize a universal ontology, distributed engine or new Runtime authority path.

## 18. Campaign geography and deterministic maps

### 18.1 Historical foundation and display scales

Use a clean, uncluttered, historically dated hex map of Europe as the shared geographic foundation. Include only geographic detail needed for gameplay and consistent generation at lower scales. The visible map shows coastlines and major water bodies, broad terrain and elevation, major rivers, cities and transport corridors. Show campaign-relevant boundaries and objectives as selective overlays; avoid dense labels and fine tactical features at this level.

Each Europe hex references the geographic constraints needed to generate its region and connect it to neighboring regions. Underlying data may be richer than the visible symbols, but every retained feature should support movement, observation, supply, objectives or lower-scale terrain generation. This is a game map, not a requirement to reproduce every geographic feature. Visual simplification must preserve consequential connectivity: omitting a crossing or route from the display does not delete it from the underlying world.

The Europe grid is the top-level display and spatial index. Operational, formation and ASL grids use explicit transforms into the same geographic reference; they need not nest exactly inside Europe hexes. Roads, rivers and generated terrain remain continuous across Europe hex boundaries.

Select and admit source data appropriate to the campaign period; this document does not yet select a geographic dataset. Modern roads, bridges or settlement footprints must not silently become WWII facts. Record known features, uncertain interpretations and generated assumptions separately.

| View | Principal map content |
| --- | --- |
| ASL tactical | Local buildings, walls, hexside features, positions and detailed observation geometry |
| Formation tactical | Connected engagements, villages, hills, woods and platoon/company maneuver |
| Operational | Towns, route networks, crossings, assembly areas, reserves and supply routes |
| Regional campaign | Cities, major rivers, transport corridors, operation boundaries and supply centers |
| Theater | Clean Europe hex map with essential broad geography and major transport links; selective campaign overlays |

These are proposed display bands, not fixed echelon assignments or prescribed hex sizes. Generate maps below the Europe foundation for the region and resolution a mission requires. All views refer to one continuous world; the Europe view uses hexes, while other views need not share its hex boundaries and may supplement their grids with routes, regions and networks. Zooming changes presentation and inspection, not the active adjudicator or simulation clock. ASL execution still requires Scenario Card admission.

Broader geographic constraints guide finer generation, while broader views summarize established detailed facts and campaign changes. Known roads omitted for readability remain present in the world. A missing hedge generated as an assumption is different from a known feature hidden by display simplification. Preserve identities and connectivity across resolutions, including when detail is first requested in a different order.

Legacy geomorphic boards remain useful for constructed scenarios. They do not automatically describe a real European location. Importing one into a historical campaign requires an explicit, validated geographic adaptation; incompatible geometry must not overwrite the campaign foundation.

The initial European transport baseline is **1 September 1939**. Admit each road or railway segment only with reviewed, dated evidence for its existence and alignment at that date. Modern networks are research candidates, not a fallback historical network. Separate route existence from operational state and from capacity, gauge, track count, electrification, surface and junction characteristics; unknown attributes stay unknown. A modern multi-track or highway classification cannot establish 1939 strategic importance.

Maintain source fragments, date applicability, spatial accuracy, uncertainty and admission decisions per segment, plus geographic coverage records. An empty unreviewed region means unknown coverage, not absence of transport. Later construction, destruction, closure, repair and gauge conversion are dated campaign events. Seeded generation cannot invent or relocate admitted historical routes. Start with a bounded France/Low Countries review before claiming Europe-wide coverage.

Modern comparison overlays must be explicitly separated from campaign transport, hidden by default and excluded from campaign exports and movement/supply calculations. Maps published after the baseline and reconstructions using year-end status require additional evidence before admission; a year label alone does not prove the September snapshot.

### 18.2 Campaign creation and Map Seed

When a new Campaign is created, generate and durably record one immutable Map Seed. Reuse it throughout the campaign at every scale. A proposed CampaignGeographyManifest pins that seed, historical baseline/date, source-data versions and hashes, geographic reference and transforms, generation rules and parameters, deterministic algorithm version and artifact schema. Campaign creation must persist the manifest before any map generation; retries recover the original seed rather than choose another.

The seed varies detail for which historical sources leave room for interpretation. Major rivers, documented settlements, crossings and other admitted facts constrain generation. A fresh seed can produce fresh detail without moving established historical features. Fixed source coverage may leave some regions unchanged between campaigns; every campaign need not differ at every location.

Determinism means the same seed and pinned world inputs produce the same base geography. Campaign ID, scenario ID, player identity, request order, viewport and requested extent are not terrain-randomness inputs. Scenario Cards reference the campaign manifest and selected region; they cannot assign a separate map seed. A saved game or exploratory branch retains the manifest. Generating a fresh seed creates a new world rather than quietly changing an ongoing campaign.

### 18.3 Generation independent of request order

Derive local randomness with a specified stable hash or equivalent deterministic function of the Map Seed, canonical geographic feature or generation-cell identity, generation layer and pinned rule version. Do not use a process-dependent hash, a mutable global random stream or iteration order. Separate terrain generation from combat dice, weather and force deployment streams.

Generate canonical geographic features and then derive each scale's representation from them. A different resolution must not independently invent a conflicting river or settlement. Larger requests must preserve the geometry that a smaller request would produce, even when neither request has been cached. Request extent selects output, not the world to generate.

Generation rules must define deterministic ownership of boundary features and sufficient surrounding context for connected roads, rivers and terrain. Cross-region features require consistent identities and constraints; processing neighboring cells independently without shared boundary rules is insufficient. Pin coordinate precision, ordering and numerical behavior so supported environments produce the same authoritative geometry and artifact hashes. Screen rendering need not be pixel-identical.

Cache and retain validated map artifacts with their manifest, geographic coverage, resolution, generator provenance and content hash. A seed alone is not a reproducibility guarantee after software or source changes. Retain compatible generators and admitted inputs or verified artifacts; if reproduction is unsupported, return an explicit failure rather than regenerate with current defaults.

### 18.4 Persistent campaign changes and delivery

Keep deterministic base terrain separate from campaign events such as demolished bridges, craters, rubble and field fortifications. The effective map combines the base with recorded changes through a particular campaign cursor. Apply each event once to stable geographic identities, then derive its effects at each supported scale. Returning to a region, expanding it or restoring a cache cannot repair a destroyed bridge.

Environmental or seasonal state follows separately pinned rules and event time; it does not trigger a new terrain seed. A new campaign starts from its admitted historical baseline, while a branch preserves the parent campaign state at the branch point. Generator or source upgrades create a new manifest version through explicit migration or a new campaign; they never silently reinterpret existing terrain or outcomes.

Before generated maps enter executable play, choose source coverage, geographic transforms, canonical generation units, feature ownership and constraint rules. Prove a bounded region first: request overlapping formation maps, dynamically compile an ASL region within them, regenerate in a different order and compare shared geography. Include an operational view and one persistent crossing change. Use the existing map validation and Scenario Card gates; full European detail need not be materialized in advance.

## Appendix A Source references

### Repository foundations

- [Plane Runtime Specification](<LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md>), authority transitions, dependency rules and conformance.
- [Domain Integration Model](<LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md>), sections 2-8 and 10-12.
- [ASL Ontology Transformation Specification](<../ASL/docs/Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>), sections 1-5, source admission and immutable packages.
- [ASL Unit State Model Design](<../ASL/docs/Designs/ASL Unit State Model Design.md>) and [Governed Writes Design](<../ASL/docs/Designs/ASL Unit Governed Writes Design.md>), architectural precedents; early scope statements are historical.
- [ASL Map Model and Authoring Design](<../ASL/docs/Designs/LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md>), coordinates, terrain and exact derivation.
- [DomainConclusion](../LimboDancer/LimboDancer.Abstractions/Domain/DomainConclusion.cs) and [DomainConclusionContext](../LimboDancer/LimboDancer.Abstractions/Domain/DomainConclusionContext.cs), current exact-package constraints.
- [GameGate](../ASL/LimboDancer.Domains.Asl.Play/GameGate.cs), [GameStore](../ASL/LimboDancer.Domains.Asl.Play/GameStore.cs) and [GamePlanner](../ASL/LimboDancer.Domains.Asl.Play/GamePlanner.cs), current commit/replay and disclosure patterns.
- [ASL LOS calculator](../ASL/LimboDancer.Domains.Asl.Maps/Los/LosCalculator.cs) and [ASL state types](../ASL/LimboDancer.Domains.Asl.Units/State/GameTypes.cs), evidence of domain-specific semantics.

- **A1. Existing Scenario Card foundation:** [Card model and validation](../ASL/LimboDancer.Domains.Asl.Play/ScenarioCards.cs), [setup adapter](../ASL/LimboDancer.Domains.Asl.Play/ScenarioSetup.cs), [card reference](../ASL/LimboDancer.Domains.Asl.Units/State/GameTypes.cs), and [Scenario Card Games Plan](<../ASL/docs/ASL Unit Scenario Card Games Plan.md>), Section 1. Establishes the existing card-based entry path, core fields and ID/hash provenance. Section 14.12 proposes operational extensions; it does not claim they are implemented.

### Corpus references used in this design

Paths below refer to the local archive. They are reading references, not a completed source-registration manifest. Physical PDF page numbers are distinguished from printed page numbers where necessary. A reproducible implementation package must add hashes and reviewed fragment locators.

- **R1. PanzerBlitz Rules of Play:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/152049563-Panzer-Blitz-Rules.pdf>). Two PDF sheets. Sheet 1: unit definitions, movement and transport. Sheet 2: spotting, combat results, play sequence and Optional Rules, including indirect fire. Supports separate original-game semantics.
- **R2. Panzer Leader Rules:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/152048103-Panzer-Leader-Rules.pdf>). PDF p. 3: scale and IV sequence; pp. 4-5: movement/transport and combat; p. 6: VII.C indirect fire; p. 7: weapon effectiveness; p. 15: XV.B optional opportunity fire. Numeric tables and fractions still require visual verification before implementation.
- **R3. ASL rulebook:** existing `eASLRB_v3_01.pdf`, Chapter A §§2.1 and 3.1-3.9; source identity in the existing registry. The [local Chapter A transcription](<../../docs/ASL/Rulebook_Markdown/02 - Chapter A - Infantry and Basic Game Rules.md>) supplies the eight-phase reference, active/defending roles and player-turn cycle. Section 6 explicitly authors the formation-scale departures.
- **D1. PanzerBlitz Designer's Notes and Campaign Analysis:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/396376051-PanzerBlitz-Designer-s-Notes-Campaign-Analysis.pdf>), opening designer discussion, PDF pp. 3-5. Supports examining how factors combine technical and organizational judgments, not adopting every historical generalization uncritically.
- **H1. Gordon L. Rottman, World War II US Armored Infantry Tactics:** [Local PDF](<E:/Archive/WWII-Docs/3-Tactics-Platoon-and-Company/462234665-azdoc-pl-elite-176-world-war-ii-us-armored-infantry-tactics-pdf.pdf>). Contents at PDF p. 5; organization section starts printed p. 17/PDF p. 19; company/platoon discussion includes printed p. 20/PDF p. 22; doctrine discussion printed p. 31/PDF p. 33; tank-infantry coordination printed pp. 48-54/PDF pp. 50-56. Selected passages, rather than all pages in those ranges, were read for this design.
- **H2. Stephen Bull, World War II Infantry Tactics, Squad and Platoon:** [Local PDF](<E:/Archive/WWII-Docs/3-Tactics-Platoon-and-Company/516380891-World-War-II-Infantry-Tactics-Squad-and-Platoon-Osprey-Elite-51-Text.pdf>). Contents at PDF p. 2 and introductory soldier-experience passages sampled. The platoon chapter starts printed p. 48; recommended for detailed model work, not treated here as a verified numerical model.
- **H3. German Field Fortifications 1939-45:** [Local PDF](<E:/Archive/WWII-Docs/3-Tactics-Platoon-and-Company/399303335-German-Field-Fortifications-1939-45-pdf.pdf>), contents and introduction at PDF pp. 4-5. Defense doctrine, planning and firepower are identified follow-up readings.
- **V1. Advanced Panzer Blitz Wargaming Orders of Battle:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Variants-and-Articles/37053579-The-Advanced-Panzer-Blitz-to-e-Book-Version-3.pdf>), PDF p. 2 introduction. Explicit invented-data qualification governs its use here.

- **H4. War Department, FM 7-15, Heavy Weapons Company, Rifle Regiment, 19 May 1942:** [Manual](https://www.ibiblio.org/hyperwar/USA/ref/FM/PDFs/FM7-15.PDF), printed pp. 1-4 (PDF pp. 5-8). Specifies two .30-caliber heavy-machine-gun platoons and an 81mm mortar platoon; discusses observation, ammunition and signal constraints on fire. Supports separating weapon capability, organization and communications. Does not establish simulation command radii or delay values.
- **N1. Submitted organization and scale notes:** [Local note](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/WWII-Organization-and-Panzer-Scale-User-Notes.md>), including the 5 October 2026 scale comparison. Unverified proposal material. Constituent readiness and company coordination are adapted in Sections 5.4 and 8.2; communication is foundational in Section 8.3, with transmission fidelity staged by profile. Counter factors, scenario narrative and claims of universal organization are not admitted evidence.

### Decomposition references inspected

- **C1. Alan R. Arvold, Infantry Factors in the Dunnigan System:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/PanzerClass/AD Infantry.pdf>), pp. 1-3 and 7-9. Rating interpretation, heavily armed German infantry and scenario-dependent Russian Recon roles. Interpretive claims require attribution and appropriate historical corroboration.
- **C2. Alan R. Arvold, Anti-Armor Attack and Range Factors:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/PanzerClass/AD Anti-Armor.pdf>), pp. 8-10, selected passages on represented vehicle counts and four-vehicle platoons. Equipment counts cannot be inferred from attack factor alone.
- **C3. Alan R. Arvold, Mortar Factors in the Dunnigan System:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/PanzerClass/AD Mortars.pdf>), p. 1. Light mortars embedded in infantry and differing mortar abstractions.
- **C4. Alan R. Arvold, Miscellaneous Units in the Dunnigan System:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/PanzerClass/AD Miscellany.pdf>), p. 6. Fort counters aggregate several defensive-position types.
- **C5. Byron Henderson, Polish Unit Composition Charts, 1 of 3:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/Counters/UCT Poles 1.pdf>), one sheet, visually inspected. Counter-specific personnel/equipment descriptions; illustrative, not an establishment authority for the German or Soviet forces in Situation 1.
- **C6. Alan R. Arvold, PanzerBlitz Master Unit Function Charts:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/Counters/MUFC-PB.pdf>), p. 1 introduction/key visually inspected. The chart assumes Panzer Leader rules and notes differences; its role is capability interpretation rather than complete roster composition.
- **S1. PanzerBlitz Situation 1:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/Scenarios/PBO-01.pdf>), one sheet, visually inspected. Scenario setting, forces, signal-installation role, fort placement and weighted objectives.
- **S2. Barrie McTaggart, Arvoldgrad Counter-attack:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/Scenarios/ISPB No 1.pdf>), one sheet, visually inspected; revised 9 April 2007. Hypothetical summer 1943 engagement on boards 3 and 6.
- **S3. Barrie McTaggart, Attack up a Valley:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/Scenarios/ISPB No 2.pdf>), one sheet, visually inspected. Hypothetical winter 1942-43 engagement on boards 12 and 10.
- **M1. Imaginative Strategist PanzerBlitz Board 1:** [Local PDF](<E:/Archive/WWII-Docs/1-PanzerBlitz-PanzerLeader/Original-Rules-and-Situations/MapBoards/PB Map 1 Full.pdf>), full-map image visually inspected for roads, settlements, woods, water features and elevation markings. Provides coarse terrain constraints, not surveyed individual building geometry. Situation 1's defended installations are on board 2; their detailed terrain still needs specific inspection.

The [consolidated archive index](E:/Archive/WWII-Docs/WWII-Study-Index.html) is a discovery aid for the earlier survey. The later download folders and their manifests contain additional references, including those above. Historical organization and campaign titles not examined beyond the survey are follow-up sources, not evidence for detailed assertions in this design.
