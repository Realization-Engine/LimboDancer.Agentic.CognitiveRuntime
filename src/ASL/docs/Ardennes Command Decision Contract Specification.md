# Ardennes Command Decision Contract Specification

Status: implementation contract with a partial HTML/JS smoke test, 9 October 2026. The implemented slice and verified limits are recorded in [prototype acceptance](<Ardennes Command Prototype Acceptance.md>). This is an authored exercise, not a historically reconstructed command network.

Companions: [facet analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>), [Situation catalog and decision cascade](<Western Theater Situation Catalog and Decision Cascade.md>), and [Blazor implementation plan](<LimboDancer.Campaign Implementation Plan.md>).

## 1. Scope and playable purpose

Use PL14 Bulge: Thrust and PL15 Elsenborn Ridge as two competing Allied defensive missions. Both cards print 18 December 1944. Their different objectives test route denial and town/exit defense. The source cards establish neither a shared headquarters nor a shared depot, reserve, clock or supply network. Every such relationship and quantity below is an authored exercise assumption.

The first command window asks: **which defensive mission receives the mobile reserve, and which supplies arrive first?** The choices change eligible counters and readiness time. They do not change ASL firepower, morale or movement factors.

Expose one parent coordinating headquarters and two subordinate mission commands. Do not label the parent as a historical corps without evidence. Other echelons are authored context for this slice. More hierarchy is justified only when it owns another consequential decision.

Standalone mode continues to open the unchanged imported cards. Campaign mode creates new variant instances with a visible difference record. Neither mode gains combat capability from this specification.

## 2. Source bindings and identities

| Mission | Source binding | Unchanged source constraints | Authored campaign restriction |
|---|---|---|---|
| M14, crossing defense | PL14; Ardennes local 01 | Allied setup groups and board C restriction; German exit objective; source demolition and turn-two conditions | Hold `us-57mm-03` outside the available roster until M14's ammunition package is issued |
| M15, ridge defense | PL15; Ardennes local 04 | Source board arrangement, setup, roadblock and German town-plus-exit objective | Hold `us-105mm-02` outside the available roster until M15's ammunition package is issued |

The two withheld counters exist in the source rosters and are available in the deployed packages. They are explicitly selected design fixtures, not evidence that these units historically lacked ammunition. Existing source counter IDs are unique only within their package. Campaign asset identity must include the campaign instance and source-instance identity.

Create one additional authored reserve detachment R1 containing two M10 platoon counters. Reuse the verified M10 type/artwork, but assign new persistent asset IDs `reserve-r1-m10-01` and `reserve-r1-m10-02`. Do not borrow or copy either source card's existing M10 instances. Clearly show this reinforcement as an addition to the original card. Personnel and vehicle decomposition remains unresolved, so it cannot enter an executable ASL Scenario Card yet.

Each variant records source package version/hash, source Situation number, campaign-local number, mission ID, force allocation IDs, ruleset/profile versions, seed, start time and adaptation revision. Source-package JSON remains immutable. Counter exclusions, additions and timing changes are separate records.

## 3. Authored initial state

Exercise date: 18 December 1944. The exercise clock starts at 0600. This time is an authored coordination clock, not the historical start time of either battle.

| Resource | Initial state | Meaning |
|---|---|---|
| A14 | One package at depot D0 | M14-specific ammunition allowance, sufficient for its withheld gun and, if assigned, the two reserve M10s for this one engagement |
| A15 | One package at D0 | M15-specific ammunition allowance, sufficient for its withheld howitzer and, if assigned, the two reserve M10s for this one engagement |
| F1 | One fuel package at D0 | One reserve transfer to either mission and one engagement allowance for R1 |
| T1 | One transport reservation resource at D0 | Capacity two packages per trip; this is an authored transport abstraction, not an extra on-map truck counter |
| R1 | Two M10 assets at the reserve assembly point | Uncommitted; requires F1 and the receiving mission's ammunition package |
| Route R14 | D0 to M14 supply point | 25 minutes outward, five minutes unloading, 30 minutes return |
| Route R15 | D0 to M15 supply point | Same timing; distinct destination |

A14 and A15 are not interchangeable. Package composition is tailored to the receiving mission. Baseline local allowances fund the remaining source roster for one engagement and are not free general-purpose stock. This first slice contains no tonnage, shot-level ammunition accounting or automatic resupply.

A mixed trip can carry A14 and A15 using a separately authored distribution circuit: both receipts occur 30 minutes after dispatch and T1 returns after 60 minutes. This is an explicit exercise abstraction. It is not derived from the geographical distance between the legacy boards. The mixed circuit requires both routes to be open.

Leadership can alter dispatch time as specified below. For a parent commitment at 0600 with the standard appointment, first dispatch is 0610, first receipt 0640, T1 return 0710, second dispatch 0710 and second receipt 0740. All setup commitments must occur by 0810. The display derives times from events, never from wall-clock browser time.

## 4. Parent decision card

Show three required selectors and a computed preview. Notes are collapsed and never affect validation.

| Field | Definitive choices | Consequence |
|---|---|---|
| Main effort | Crossing defense / Ridge defense | Receives the first order delivery; the other order waits one parent preparation interval. Also sets the default reserve suggestion; does not itself transfer resources |
| Reserve destination | M14 / M15 / Retain R1 | Reserves the two named M10 assets for one recipient, or leaves them unavailable to either battle |
| Delivery plan | Support M14 first / Support M15 first / Guns first | Determines shipments below |

Delivery plans:

- **Support M14 first:** trip 1 carries A14 + F1 to M14; trip 2 carries A15 to M15. Requires reserve destination M14.
- **Support M15 first:** trip 1 carries A15 + F1 to M15; trip 2 carries A14 to M14. Requires reserve destination M15.
- **Guns first:** trip 1 distributes A14 + A15. Trip 2 carries F1 to the selected reserve destination. If R1 is retained, omit trip 2 and leave F1 at D0.

Do not silently change a conflicting reserve selection. Disable the incompatible plan with a public explanation and let the player choose. Show the resulting roster counts, package custody, earliest readiness and blocked capabilities for each mission before commitment.

Commit validates the entire allocation against one authoritative revision and reserves stocks, R1 and T1 atomically. Two clients cannot independently obtain the same reserve. A repeated command ID returns its original result. It does not create another convoy or mission.

## 5. Delivery, planning and subordinate choices

At 0600 the parent receives an authored coordinating objective. After it commits, the main-effort order dispatches immediately and takes five exercise minutes to arrive. The other order dispatches after one parent preparation interval, then takes five minutes to arrive. With a 0600 commitment and standard staff, receipt is 0605 for the main effort and 0615 for the other mission. Both allocations are reserved atomically at commitment despite these staggered notifications. The source setup is not exposed as ready merely because an order was issued. At receipt the subordinate acknowledges or selects a revision reason: `ConflictingAllocation`, `KnownRouteUnavailable`, or `CannotMeetDeadline`.

A received mission exposes these choices:

| Choice | Preconditions | Resulting variant |
|---|---|---|
| Baseline defense | Order received; preparation complete | Remaining source roster, excluding the named withheld gun; no R1; pending supplies may arrive later but do not modify an executing Situation |
| Supplied defense | Mission ammunition received and issued | Restores the withheld source counter; no R1 |
| Reinforced defense | Mission ammunition issued; F1 received and issued; R1 assigned and transfer complete | Restores the withheld counter and adds both R1 M10 counters |
| Wait for assigned delivery | Shipment still scheduled; projected readiness no later than 0810 | No tactical launch; clock and other missions continue |
| Request revision | Allowed reason applies to the commander's known state | No automatic resource grant; parent receives a request after five minutes |

With the standard appointment, preparation completes ten minutes after order receipt: 0615 for the main effort and 0625 for the other mission in the 0600 example. This gives main-effort selection a real effect on the earliest baseline defense. R1's authored transfer takes ten minutes after fuel receipt. Thus the example's first reinforced mission is ready at 0650; a Guns-first reserve is ready at 0750. Launch requires the recipient to acknowledge the current order version and preparation to complete, in addition to supply prerequisites.

Selection of Baseline or Supplied defense does not silently cancel an R1 commitment or scheduled supply. The preview lists stranded reservations and offers an explicit parent revision. A mission can launch without them while they remain reserved, but cannot later insert them into the running battle. There is no automatic change to the opposing source roster when the player waits. Any timed enemy response would need a separately authored event and variant record.

R1 starts in eligible Allied board C setup hexes for either card, as an explicit addition to the source setup contract. It does not arrive as a mid-game reinforcement in this slice. Preserve PL14's existing setup-group restrictions for its original counters; R1 gets its own named setup group. Preserve the original German rosters and both source victory conditions.

Issue ammunition/fuel at variant commitment, not on preview. Delivery transfers custody to the recipient; commitment consumes the fixed engagement allowance once. Unissued packages remain stock. Supply is not also deducted per tactical shot under this profile. A later detailed tactical adapter must choose a replacement accounting policy, not add a second charge.

## 6. Route interruptions and cancellation

Routes have `Open`, `Interrupted` or `UnknownToCommand` knowledge presentations. Authoritative availability is separate from what the commander has been told. This slice has no random interdiction generator. A dated fixture event can interrupt a route for contract testing; it must be labeled as a test event.

Before dispatch, an interrupted route prevents departure and keeps packages at D0, reserved for their mission. A mixed trip requires both routes. If a route is interrupted during transit, the whole trip holds at its last safe abstract checkpoint with cargo still in transit. T1 remains occupied and its return time is unscheduled. No package becomes frontline stock on its old ETA.

After a route report arrives, offer `WaitForReopening`, `RecallConvoy`, or `RequestPlanRevision`. Recall takes an authored 30 minutes from the hold checkpoint to D0. Only actual depot receipt releases its packages for reallocation. Reopening resumes a held trip with its remaining travel and unloading duration. A revised itinerary must be validated; this slice offers no invented alternate route.

Order cancellation releases undisbursed reservations. It does not teleport cargo, reclaim consumed allowances, delete casualties or free executing R1 assets. Replacing an order creates a new version and notifies recipients; already executing missions reject changes to their committed Situation snapshot.

## 7. Leadership and communication contract

Every decision belongs to a command appointment, not to a free-floating player or map icon. Use authored role incumbents in this slice; historical names and effectiveness ratings require separate evidence.

- Parent appointment owns allocations, reserve release and order revisions.
- Mission appointments own the three defense choices within their received authority.
- Deputy succeeds an unavailable incumbent after ten minutes when leadership is player-managed. During the vacancy no new discretionary commitment is legal. Previously authorized shipments and executing missions continue.
- A capable staff appointment has preparation duration ten minutes. A reduced staff appointment has duration twenty minutes. These are transparent exercise parameters, not nationality or rank bonuses.
- Parent transport preparation uses the same ten/twenty-minute parameter. Travel durations do not change with leadership ratings. Dispatch cannot precede plan commitment plus preparation.

One headquarters processes one new discretionary commitment at a time. A second command queues until preparation completes. A preset delegation policy may permit `BaselineDefenseOnTimeout`, but only after a delivered order explicitly grants it and its timeout expires. No LLM decides what a note means or invents an exception to authority.

Once dispatched, orders, requests and reports each take five minutes in the standard profile. The secondary order also incurs the explicit dispatch queue described above. Authoritative receipt of supplies and tactical reconciliation occur at their actual times. Superior knowledge changes only when the appropriate report arrives. A parent may remain unaware that a route has failed; visible choices must not reveal a hidden failure by instantly changing their availability. Commitment can fail on known authority/resource conflicts, while an unknown route failure becomes an execution delay and subsequently a delivered report.

## 8. Optional facets and automatic policies

Persistent identities, a clock, legal authority, conservation, conflict-free reservations and versioned transitions are always active.

| Profile / facet | Player-managed choices | Automatic policy |
|---|---|---|
| Standalone | Original Situation selection and deployment | No campaign variant or shared resource pool |
| Essentials | Main effort and subordinate defense choice | Reserve goes to main effort; matching Support-first delivery; standard staff; orders acknowledged on arrival; auto-wait for assigned supplies within deadline |
| Operational supply | Main effort, reserve and delivery plan; response to known disruption | Standard staff and acknowledgment; transport follows the committed itinerary |
| Optional leadership | Appointment/delegation choices and succession acknowledgment | When disabled, generic incumbents remain available with standard preparation times |

Automatic actions emit the same validated events as player actions and use the same knowledge restrictions. Disabling a facet does not replenish its resources. Explain the automatic policy in a short read-only disclosure.

For this first slice, profile changes are allowed only before the parent allocation commits. Afterward, defer the change to the next decision window. No mid-flight conversion or active-engagement rule change is supported. The application should explain this limit rather than approximate detailed-to-abstract stocks.

## 9. Mission and Situation state boundaries

Mission lifecycle: Draft -> Issued -> InTransit -> Received -> Planned -> Committed -> Executing -> ReportPending -> ReportDelivered -> Assessed. Cancelled and Superseded are explicit outcomes permitted only at the applicable boundaries. Shipment states and commander availability are separate state machines.

`Committed` freezes the variant, reserves its force and issues allowances. It does not mean combat has started. The setup screen places exactly the committed roster. Before execution, abort releases force reservations and returns unconsumed issued allowance to that recipient's stock by one explicit event. After execution begins, cancellation requires a supported withdrawal result; no free reset exists.

`Executing` requires an admitted resolver. The current HTML prototype supports deployment and map inspection only, so it cannot perform this transition for these cards. Opening its game map is presentation state, not evidence of execution. An independent planning demonstration may show a committed map, but must disclose this gate.

Both missions may execute concurrently only when all asset reservations are disjoint. Simulation progress uses a common committed time horizon; opening a browser tab or resolving a second mission first cannot advance past an unresolved earlier event. This slice needs a synchronization boundary even while the combat resolver is absent.

## 10. Results, reporting and assessment

The resolver contract returns engagement ID, result ID, expected committed version, start/end time, participating asset IDs, losses, surviving strengths, objective facts and any supported infrastructure changes. Unsupported facts remain unknown, not false or zero. Tactical duration must be supplied by an admitted time adapter; do not equate a source turn to a campaign day or invent an ASL conversion.

Tests use explicitly authored result fixtures with start/end times. Fixtures are available only in an isolated test session, never a normal player outcome selector. They prove orchestration without pretending that combat ran.

Reconcile a valid result exactly once, then schedule its knowledge report five minutes later. Duplicate results are idempotent; a different payload using the same result ID is rejected. Losses cannot exceed committed assets. Do not charge fixed supply allowances again on result receipt.

Source success for M14 depends on whether the German exit threshold was prevented. M15 depends on the conjunction of German town control and the required exits. A casualty total alone cannot determine either result. A bridge demolition claim requires a supported rule result; the current reference clause is insufficient evidence.

After a report arrives, assessment offers only choices justified by reported facts:

- `ContinueAssignedDefense`: objective remains assigned and a reported surviving force is available.
- `RequestReinforcement`: a valid recipient and request channel exist; does not grant assets.
- `ReleaseSurvivingReserve`: identified R1 survivors are no longer engaged; starts a dated recovery/transfer process.
- `RecordObjectiveFailure`: report establishes failure; opens a new authored planning window if one exists.

No automatic march from PL14 to PL15 or respawn from a later historical roster occurs. Released reserve survivors require a new supply allowance and travel before another mission. This first window has no replenishment event, so a second sortie may legitimately be unavailable.

## 11. Campaign-owned implementation contracts

Implement within LimboDancer.Campaign, with no ASL project changes in this slice. Proposed records:

| Record | Minimum authoritative fields |
|---|---|
| DecisionWindow | ID, date/time limits, mission IDs, ruleset/profile versions, revision |
| CommandAppointment | HQ, incumbent/deputy IDs, authority, availability, preparation duration, delegation policy |
| MissionOrder | ID/version, issuer/recipient, objective template, allowed choices, allocation references, delivery time |
| SupplyPackage | ID/type, compatible mission, custody, reservation, consumption event |
| Shipment | ID, cargo IDs, route/circuit, transport ID, state, remaining duration, scheduled events |
| ForceReservation | Asset IDs, owning mission, interval/state, release condition |
| DecisionCommand | Command ID, actor appointment, expected revision, option ID, typed selections |
| SituationVariant | Source identity/hash, committed roster, setup amendments, start conditions, difference record, admission blockers |
| Result / KnowledgeReport | Stable IDs, occurrence/delivery times, source engagement, audience, supported facts |

Validation returns typed reason codes and audience-safe explanations. Numeric quantities are fixed or bounded selections. Notes are separately stored and excluded from the authoritative transition input and deterministic state hash.

Event ordering at the same timestamp is explicit: external condition changes, physical arrivals/reconciliation, message deliveries, automatic decisions, then player decisions against the resulting revision. Stable event sequence resolves ties within a class. Store the rule version and sequence for replay. Transactions persist ledger events and state together.

Every eventual tactical engagement still requires the ASL Scenario Card model. The Campaign adapter may reference consumable ASL contracts later; ASL must not reference Campaign. The original card, committed variant and generated ASL cards remain three distinct artifacts. Multiple ASL engagements reserve subsets of the same persistent assets without copying them.

## 12. UI acceptance walkthrough

1. Choose **Campaign operation > Ardennes coordination exercise**. Show authored assumptions, source-card links and the selected complexity profile.
2. At the parent HQ choose **Crossing defense**, **M14**, **Support M14 first**. Preview A14/F1 at 0640, R1 ready at 0650, and A15 at 0740 under standard staffing.
3. Commit. Show only the two subordinate mission children, their order-delivery state and read-only parent allocation. No free-form order is required.
4. Advance to the next scheduled event. After M14 receives its order, show its definitive defense choices with readiness times. A future choice may be planned now, but cannot commit before its prerequisites.
5. Select Reinforced defense for M14. At 0650 commit its variant. Show the restored source gun and two added M10s in the difference panel, then open setup for that exact roster.
6. For M15, choose Baseline defense early or wait for Supplied defense at 0740. Show the single withheld/restored howitzer explicitly.
7. At the execution gate disclose that the present resolver is unavailable. In a test session only, apply a labeled fixture result, deliver its report and inspect assessment options.

Use a breadcrumb for parent context and show only the selected node's controls. A clock control advances to a named event; it is not a free-form time entry. Players can review all commitments before advancing. Existing standalone Situation entry remains directly accessible.

## 13. Required verification before implementation acceptance

- M14 Support-first and M15 Support-first produce opposite delivery priorities and distinct variant rosters.
- Guns-first restores both guns at 0640 but cannot make a reserve ready before 0750; Retain R1 leaves F1 at D0.
- Missing fuel never enables R1; A14 never satisfies M15; source counters keep their numeric factors.
- Duplicate commands/results do not duplicate stocks, counters or effects. Concurrent stale allocations fail atomically.
- A route interruption suspends arrival and transport return; recall preserves cargo and prevents early reallocation.
- Baseline launch does not inject a later shipment or reserve into the running Situation.
- Leadership vacancy blocks new discretionary commands while committed activity continues; deputy succession and delegated timeout are deterministic.
- Undelivered information does not leak through option labels or disabled reasons.
- Notes have no effect on authoritative state. Automatic profiles preserve all accounting invariants.
- Aborting before execution returns issued allowances once; execution prevents a free reset.
- Source packages stay byte-identical. Variant save/reload preserves package hashes, allocations and profile versions.
- Unsupported combat, demolition, timing and ASL decomposition block execution with specific reasons.
- Fixture outcomes alter surviving assets and later legal choices, while the UI labels them as simulated contract tests.

Deliver the domain and event tests first, then a Blazor planning UI, then resolver integration behind capability checks. This specification does not require extending the HTML prototype or modifying ongoing ASL rules work.
