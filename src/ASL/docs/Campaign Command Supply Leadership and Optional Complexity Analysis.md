# Campaign Command, Supply, Leadership and Optional Complexity

Status: proposed analysis, not an implemented ruleset. Date: 2026-10-08.

This document gives the command hierarchy and mission lifecycle a playable purpose. It extends the campaign design with supply, leadership and optional operational facets, while keeping every authoritative player action structured and every ASL engagement expressed through a Scenario Card.

Companion documents: [migration and terrain analysis](<Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis.md>), [implementation plan](<LimboDancer.Campaign Implementation Plan.md>), and [multiscale simulation design](<../../docs/Military Command and Multiscale Simulation Design.md>).

## 1. The problem to solve

A prepared Campaign Situation can currently provide a map, forces and deployment without requiring the command exercise. Merely asking players to issue, acknowledge and annotate orders adds work without changing the battle. The descent through headquarters needs consequences.

The proposed purpose is simple: command decisions determine which situations become available, what resources they receive, when they can start, and what their outcomes make possible next. Tactical play determines what actually happens to those commitments.

Keep two valid entry points:

- **Standalone Situation:** play an authored battle with its fixed conditions. No hierarchy exercise is required.
- **Campaign:** play situations linked by persistent forces, time, objectives, resources and delivered information. Command choices and tactical results change subsequent choices.

A campaign should remain enjoyable without manually operating every headquarters. Players choose a command role; higher orders can be authored, and subordinate decisions can follow explicit delegation policies. Multiplayer may assign different headquarters to different players.

The acceptance question is: if a player makes a different command decision, does a meaningful playable condition change? If not, that decision should be automated or omitted.

## 2. A purposeful descent

Geographic display scale, command echelon and the scope of a playable situation are related but separate. A theater hex does not imply one particular unit size. A Campaign Situation may contain several company and platoon counters; it does not represent the whole campaign.

The following responsibilities are proposed game roles, not a universal table of WWII organization. Nationality, date, formation type and temporary attachments determine the actual hierarchy. Skip absent echelons.

| Command level | Decision worth asking the player to make | Consequence below |
|---|---|---|
| Theater / army group | Choose an operation and priority among competing fronts | Available campaign objectives and resource budgets |
| Army | Select the main effort and allocate scarce support | Which corps can attack and which must hold |
| Corps | Assign division objectives and retain or release reserves | Boundaries, support allocations and reinforcement conditions |
| Division | Choose axes and regimental missions | Access to routes, tasks and supporting formations |
| Regiment / brigade | Assign attack, support and reserve roles to battalions | Which force receives the next situation and with what support |
| Battalion | Choose a local plan, timing and company commitments | A concrete Campaign Situation with constrained alternatives |
| Company / platoon | Deploy and maneuver actual assigned units | Contacts that can produce one or more ASL Scenario Cards |
| ASL engagement | Resolve the tactical action | Persistent losses, elapsed time, expenditure and control changes |

No echelon should create troops or supplies by issuing an order. Each subordinate mission receives a bounded allocation from its parent. A division order produces subordinate missions, not hundreds of automatically generated ASL scenarios.

Results travel back through the same organization, but assessment is not a majority vote among tactical victories. A costly bridge seizure may satisfy the parent objective; winning several firefights while missing the deadline may not.

## 3. Mission lifecycle and structured interaction

Use explicit states: Draft, Issued, In transit, Received, Planned, Committed, Executing, Report pending, Report delivered and Assessed. Cancellation, supersession and failure are explicit transitions with rules for already committed resources. Receipt acknowledges an order; it does not imply that the recipient can perform it.

| Stage | Player input | System responsibility |
|---|---|---|
| Assignment | Select a mission template, recipient, objective and allowed allocation | Check authority, availability, dependencies and time window |
| Delivery | Select an available communication method if the profile exposes it | Schedule arrival using campaign time and communication conditions |
| Receipt | Acknowledge, or request an allowed revision with a reason code | Preserve order version and the recipient's known information |
| Planning | Choose an allowed approach, posture, start window and support package | Show commitments, tradeoffs and unmet requirements |
| Execution | Commit or launch when eligible | Reserve assets and instantiate the Situation or ASL Scenario Card |
| Reporting | Submit an engine-generated report, optionally with a structured recommendation | Reconcile actual results once and deliver the report separately |
| Assessment | Continue, reinforce, hold, redirect or withdraw where authorized | Evaluate objectives and expose the next legal decisions |

Personal and team notes remain available but never satisfy a prerequisite, allocate resources or change victory conditions. Generated prose can explain a structured order; prose is not the order's authority.

The UI should show the active headquarters, its received mission, the choices it can currently make, and the consequences of each choice. Show relevant children after the parent commits its decision. Keep a breadcrumb and read-only parent context. Do not require repeated confirmation at every echelon when there is no decision to make.

Deterministic interaction means the same state and action produce the same validated transition under the same rules and recorded random stream. It does not remove dice, uncertainty or enemy choice. Hidden supply or enemy intelligence must not leak through disabled-button explanations.

## 4. Supply: resources must reach the force

Supply is a strong first facet because it turns a broad objective into a choice among feasible operations. Possession at a depot is different from availability at the front.

Distinguish five things:

1. **Stock:** what exists, where, and in whose custody.
2. **Demand:** what a particular mission requires or is expected to consume.
3. **Capacity:** what can move through the transport system within a period.
4. **Delivery:** a shipment with a route, departure, estimated arrival and actual receipt.
5. **Readiness:** what a unit can do with its current stock, personnel and equipment.

Use a supply graph connecting ports, depots, railheads, transfer points and unit supply locations. Edges carry travel time, permitted transport, throughput, control and disruption state. Bridges and junctions can be bottlenecks. Model a last-mile connection rather than assuming arrival at a railhead supplies every nearby unit.

For the first version, use explicitly defined ammunition, fuel and general-supply packages with published scenario-specific meanings. Do not make all commodities interchangeable. An infantry ammunition delivery cannot silently become tank fuel. Detailed profiles can later add compatibility, spare parts, medical stores and engineering material.

The historical rationale is substantial. The Army Transportation Corps account of the Red Ball Express describes damaged railways, road supply routes, return circulation, vehicle maintenance and driver fatigue. It also dates that operation to late August 1944. Those are useful mechanisms for the model, but its network and demands must not be transplanted into a June Normandy situation. [Army Transportation Corps study](https://transportation.army.mil/history/studies/red_ball_express.html).

The Normandy campaigns discussed here begin after the beachhead is secured. A beach or port may be an authored supply entry point; this does not require simulating an amphibious landing. Every campaign needs its own dated entry points, stocks, capacities and evidence limitations.

### A concrete constrained decision

The following is an authored game example, not a historical claim about St. Lo or real tonnage.

A battalion has enough local stock for a limited infantry action. Its supporting depot holds two ammunition packages and one fuel package. Transport can deliver two packages before 0800. One ammunition package supports an additional infantry attack; a second funds a defined artillery support allocation. The fuel package enables a specified armored detachment's movement budget. The packages are indivisible in this example.

| Choice | Delivery before 0800 | Playable consequence |
|---|---|---|
| Infantry with fire support | Two ammunition packages | Infantry attack and allocated artillery; armor remains unavailable |
| Combined arms | One ammunition and one fuel package | Infantry and armor; no additional artillery allocation |
| Delay the attack | Two packages now, remaining package on a later trip | All three benefits only after that trip actually arrives; the campaign clock advances |
| Protect the supply route first | Commit a suitable available force to a route-security mission | Main attack is postponed; success may preserve future delivery capacity |

The delayed option must include return travel, transport availability and a new departure. It cannot assume a third package arrives merely because the player clicked Wait. Enemy actions and dated reinforcement events continue during the delay.

The choice should change force availability, support allocations, entry times or a permitted mission objective. It should not arbitrarily change ASL firepower, morale or movement factors. If a desired shortage effect requires an unsupported tactical rule, keep its effect at campaign level or block that adaptation until the rule is supported.

### Supply can generate situations

Useful mission templates include securing a junction, reopening a route, holding a bridge until a convoy passes, relieving an isolated force, recovering abandoned equipment and interdicting enemy transport. These need forces, timing, opposing interests and measurable objectives. A shipment does not automatically create a battle; contact and engagement-selection rules decide whether tactical resolution is warranted.

Finite supply should create alternatives, not repeated dead ends. A blocked attack may allow waiting, requesting reallocation, reducing scope, rerouting or withdrawing. If the historical situation offers no feasible action, the campaign should explicitly resolve that operational failure rather than inventing resources.

### Accounting and tactical boundaries

Reserve stock when committed, transfer custody on dispatch and receipt, and consume or lose it through explicit events. Shared transport and artillery cannot serve overlapping missions beyond their capacity. Cancellation releases only what remains recoverable.

Use estimates for planning and actual consumption where the tactical engine supports it. Where it does not, use a declared campaign-level expenditure rule at a defined boundary. Do not charge both a reserved package and every simulated shot for the same ammunition. Never reset a surviving force's losses or supply by generating its next Scenario Card.

## 5. Leadership: who has authority and who acts?

The inspected prototype does not yet demonstrate a persistent commander assigned to every headquarters or a functioning supply-chain ledger. These are proposed additions, not existing capabilities.

Separate the player, command appointment, individual leader, headquarters staff and tactical leader counter. A player exercises a command role. A historical or fictional person occupies that appointment. The headquarters provides organizational capacity. An ASL leader counter is a tactical game entity with its own provenance and rules.

Every decision-owning headquarters needs an accountable command role. It does not need a named historical personality or a player-controlled character. Unknown appointments can use a generic incumbent without inventing biographical facts.

Start leadership with authority and delegation:

- Which missions can this headquarters issue or accept?
- Which assets can it allocate, and which require superior approval?
- What may a subordinate do while awaiting a reply?
- Who succeeds a commander who becomes unavailable?
- What information has this headquarters actually received?

For example, an isolated company might have a standing instruction to hold until a specified time, withdraw when a defined condition is met, or exploit only within an assigned boundary. The player chooses from authorized policies in advance. Loss of communications then has a concrete effect without random refusal of valid commands.

A later leadership module could model planning capacity, staff coordination, succession delays and initiative within delegated limits. Ratings require transparent definitions and evidence or clearly labeled authored assumptions. Avoid national stereotypes, automatic rank-to-skill conversion and unreviewed personality bonuses.

Do not transform a division commander's rating into ASL leadership modifiers. Assign actual tactical leaders through the force roster and Scenario Card. If one identified person can appear both organizationally and tactically, preserve identity and reconcile casualties once.

## 6. How many facets are useful?

There is no useful fixed maximum. The limiting factor is whether a facet creates a distinct decision with an observable consequence. The following fourteen groups are a practical catalog, not a commitment to implement fourteen subsystems now.

| Facet | Meaningful decision | Effect on situations | Suggested introduction |
|---|---|---|---|
| Supply | Allocate ammunition, fuel and stores | Available force, support and endurance | First slice |
| Transport and infrastructure | Select routes, repair bottlenecks, allocate lift | Arrival time, access and delivery capacity | First slice, simplified |
| Leadership and staff | Delegate, appoint, set authority limits | Available decisions and coordination time | First slice, roles only |
| Communications | Choose method and priority, set standing orders | Order and report arrival; autonomous action limits | First slice, simple timing |
| Intelligence and reconnaissance | Reconnoiter or commit with uncertainty | Known routes, contacts and planning confidence | Next |
| Time and operational tempo | Attack now, synchronize or wait | Reinforcements, deadlines and concurrent opportunities | Core clock; detailed costs later |
| Fatigue, morale and cohesion | Rest, rotate or continue | Mission readiness and recovery | Later, with supported tactical mapping |
| Maintenance and recovery | Repair, recover or abandon | Equipment availability across situations | Later |
| Personnel and replacements | Reinforce, reorganize or retain reserve | Persistent strengths and available formations | Next, initially authored arrivals |
| Engineering | Allocate breaching, bridge or clearance assets | Traversable routes and mission alternatives | Next |
| Weather and daylight | Choose an available operating window | Movement, visibility and support conditions | Authored baseline; dynamic later |
| Fire and air support | Allocate a scarce support window | Supported situations and competing priorities | Next; capability-gated |
| Medical evacuation | Allocate transport and treatment capacity | Personnel availability and return timing | Advanced |
| Coalition and civilian constraints | Respect operational boundaries and protected routes | Legal mission choices and capacity constraints | Advanced, campaign-specific |

Each effect needs a single owner. Engineering changes a bridge's state; transport uses that state; supply uses transport capacity. Do not apply three independent penalties for the same broken bridge. Similarly, fatigue and maintenance must not independently consume the same transport availability without a defined composition rule.

Reserve detailed morale, medical and air-support features until they provide a distinct benefit beyond what existing readiness and reinforcement mechanisms already represent.

## 7. Optional complexity without inconsistent rules

Use three treatment modes per facet: **authored baseline**, **automatically managed**, and **player managed**. A simplified facet still has a defined resolution policy. Turning supply detail off does not grant unlimited ammunition or duplicate assets.

| Suggested profile | Player experience |
|---|---|
| Situation | Choose a prepared battle; fixed roster, support and conditions; no operational administration |
| Campaign Essentials | Choose objectives, force commitments and simple support packages; deliveries and command staffing handled automatically |
| Operational | Manage ammunition/fuel allocations, main supply routes, reserves, order delays and reconnaissance |
| Advanced | Add selected maintenance, recovery, fatigue, engineering, leadership succession and other campaign-supported facets |

Profiles are presets, not mandatory difficulty ladders. A player can enjoy supply decisions while leaving leadership automatically managed. Automatic decisions use explicit, inspectable policies and the same information restrictions as the player; they do not receive hidden enemy knowledge.

Some rules are always present: persistent identity, campaign time, resource conservation, legal authority, side-specific knowledge, conflict-free commitments, versioned state, reproducible generation and Scenario Card validation. These protect coherence even in the simplest profile.

A facet registry should declare prerequisites, conflicting settings, compatible content, its automatic policy and its tactical capabilities. Detailed supply needs transport and time, but the transport UI can remain automatic. A source scenario with a mandatory shortage cannot be played in a profile that silently removes it; offer a supported abstraction or a clearly labeled variant.

### Adding complexity during a campaign

Let players request a profile change at a safe checkpoint between commitments or engagements. Preview the consequences, validate dependencies, and record the new ruleset version with its effective time. Never change a ruleset inside an active ASL engagement.

Moving from abstract supply to detailed supply requires a declared conversion from remaining allowances into stocks and transport commitments. Do not fabricate a fresh fully stocked army. Moving back to abstraction must retain losses, commitments, expenditure and relevant bottlenecks. If a safe conversion is unavailable, defer the change or start a new campaign.

Record the campaign seed, content versions, profile history and random events needed for replay. In multiplayer, complexity changes are campaign settings subject to the agreed authority policy, not a private player preference.

## 8. UI: decisions rather than forms

Present a mission as a compact decision card containing objective, known conditions, assigned forces, deadline and next action. For the supply example, show the four allowed plans with their costs, arrival times and support differences. A disabled option explains a known unmet requirement and, when possible, offers a legal remedy.

Use selectors, bounded allocations, map-object choices, checkboxes and predefined reason codes. A map selection identifies an objective or route in the model; it does not become an unvalidated prose instruction. Show the selected option's consequences before commitment.

Keep notes in an optional collapsed panel. Reports summarize engine facts and distinguish occurrence time from observation and delivery time. The commander's displayed knowledge may lag the world state. Reserve authoritative bookkeeping for the simulation, not a player-entered report field.

Operational and tactical time must be reconciled. A tactical engagement occupies an explicit interval; committed forces remain unavailable elsewhere during it. Other deliveries and missions advance through a defined synchronization policy, not by whichever browser view happens to be open.

## 9. Campaign architecture and ASL boundary

These concepts belong in LimboDancer.Campaign and can be developed independently of ongoing ASL work. Campaign references consumable ASL projects when integration begins; ASL projects must not reference Campaign.

Proposed campaign concepts include Mission, CommandAppointment, DelegationPolicy, ResourceAllocation, SupplyStock, Shipment, TransportReservation, KnowledgeReport, FacetProfile and RulesetVersion. Each needs stable identity and explicit ownership; this list is a domain proposal, not a demand for a separate project per concept.

Facets should contribute typed preconditions, costs, scheduled effects and allowed alternatives to a common command-validation path. Record each accepted change once in an event ledger. Conflicting simultaneous allocations must fail validation against the authoritative revision. Repeated delivery or tactical-result messages must not duplicate supplies or casualties.

The integration path is:

Parent mission and allocations -> Campaign Situation instance -> bounded tactical engagement -> validated ASL Scenario Card -> tactical result -> campaign reconciliation -> delivered report -> next command decision.

Every tactical engagement needs an ASL Scenario Card, including its roster, terrain, sides, entry conditions, timing, supported special rules and victory conditions. One Campaign Situation can yield several cards, with reservations preventing simultaneous use of the same people, vehicles or support.

Keep immutable source content separate from a campaign instance and its adaptation record. For St. Lo, retain the Panzer Leader source situation's identity and published baseline. Any changed roster, support, timing or objective is a named campaign adaptation with a recorded reason. Unsupported tactical constraints are admission failures, not free-form special rules interpreted opportunistically by an LLM.

The first implementation must not pretend the formation layer already has movement, combat or complete tactical reconciliation. Test-result injection can validate contracts in an isolated test mode, but it is not campaign gameplay.

## 10. Recommended first slice and proof

The selected first slice is now the [Ardennes Command Decision Contract Specification](<Ardennes Command Decision Contract Specification.md>), dated 9 October 2026. It uses PL14 and PL15 as separate defensive missions under an explicitly authored coordinating headquarters, with finite mission-specific ammunition, fuel, transport and a named reserve detachment. This replaces the earlier St. Lo-plus-alternative-task proposal. Appointments, preparation times and delegation are transparent exercise parameters; there are no historical personality ratings. Other facets remain at authored baselines.

Build and verify this loop before adding further echelons or detailed logistics:

1. Receive a structured objective and finite resources.
2. Choose between at least two feasible plans with different commitments.
3. Deliver the order, reserve assets and schedule supply.
4. Make the relevant Situation available when its prerequisites are satisfied.
5. Resolve or test-inject a versioned outcome through the defined boundary.
6. Reconcile forces, resources, geography and time exactly once.
7. Deliver a report and offer materially different follow-up choices.

Acceptance evidence should demonstrate that:

- A different allocation changes force/support availability or start time.
- A route interruption changes a pending shipment and its dependent mission.
- A delayed report changes what a headquarters can know without delaying actual world-state bookkeeping.
- Duplicate events do not duplicate resources, and overlapping missions cannot borrow the same assets.
- An absent commander follows an explicit succession/delegation policy.
- Notes never change authoritative state.
- Simplified and detailed profiles preserve the same conservation and identity invariants.
- Profile changes cannot replenish supplies or erase commitments.
- Every ASL handoff passes Scenario Card and capability validation.
- Tactical outcomes change the next operational choice rather than merely filling a report screen.

This analysis supplements the existing implementation plan. It proposes the next campaign-domain design work; it does not silently expand the approved ASL rule scope or authorize implementing the full facet catalog.

## 11. Evidence and remaining research

Historical sources constrain dates, organizations and plausible mechanisms. They do not by themselves prescribe game ratings, stock quantities or automatic decision policies. Those require dated scenario evidence or clearly labeled design assumptions.

The [Army Transportation Corps Red Ball Express study](https://transportation.army.mil/history/studies/red_ball_express.html) supports the logistics mechanisms discussed above, with the explicit late-August date limitation. The Army Center of Military History's [Logistical Support of the Armies catalog](https://history.army.mil/Publications/Publications-Catalog/Logistical-Support-of-the-Armies/) identifies the broader research series for theater supply. Its relevant chapters and maps still need examination before authoring quantitative Normandy stocks, routes and delivery rates.

Before calling a St. Lo supply or leadership model historical, verify the selected date's formation appointments, subordinate strengths, attachments, support authority, available transport, supply origins and route condition. Record unknown values as unknown or authored approximations. No historical commander effectiveness ratings or real logistical quantities are established by this document.

The [Western Theater Situation Catalog and Decision Cascade](<Western Theater Situation Catalog and Decision Cascade.md>) adds a source-grounded candidate library and decision matrix for the first campaign slices.
