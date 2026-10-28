# ASL Turn Phase Guide Analysis

Date: 2026-10-26

Status: Backend-first exploration, reopened 2026-10-27. The ASL.Turns reference foundation exists; bounded Rules timing, host history adapters and shared phase-close assessment now exist; additional families and obligation execution remain pending. No UI component is implemented.

Backend decision, 2026-10-27: [Design](ASL%20Turn%20Phase%20Guide%20Design.md) and [Implementation Plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md) now put ASL.Turns and ASL.Turns.Tests first. The headless mechanism owns content, guidance contracts, obligation coordination and closure assessment. Rules remains authoritative for semantics; Play adapts facts and executes governed commands. UI construction waits for backend proofs.

The [Rally Phase Component Walkthrough](ASL%20Rally%20Phase%20Component%20Walkthrough.md) expands the first phase into individual topics, consideration prompts, rule references, vocabulary, and an illustrative decision chain.

Rally reconciliation completed on 2026-10-26 against the supplied eASLRB v3.01 and ASOP (physical PDF page 678). The [Rally checklist catalog](ASL%20Rally%20Phase%20Checklist%20Catalog.json) contains 45 topics mapped to all ten printed Rally steps, plus supplemental procedures and timing reminders. The walkthrough records the coverage ledger and source limits. Deferred Chapters F, G, H and W remain represented. Named campaign exceptions are visible with a source-text gap where the full module rule is absent.

The audit adds concrete component requirements: retain printed step IDs; allow within-step ordering unless constrained by a rule; resolve conflicts ATTACKER first; share source dice across dependent weather procedures; and retain pre-roll versus post-roll state. Mandatory leadership, extended Fate, nationalities and campaign exceptions modify individual decisions. Animal-Pack and ammunition work require duration and interruption tracking across Player Turns. A generic acknowledgement cannot complete those commitments.

Prep Fire reconciliation is documented in the [Prep Fire walkthrough](ASL%20Prep%20Fire%20Phase%20Component%20Walkthrough.md) and [Prep Fire checklist catalog](ASL%20Prep%20Fire%20Phase%20Checklist%20Catalog.json): 58 topics, 108 contextual vocabulary labels, and all nine printed PFPh ASOP steps on PDF pages 678-679. The shared preview retains separate topic selections and consideration records for each phase. It remains a reference preview, not a live phase controller.

Prep Fire adds a different capability model from Rally. Track inherent fire, SW use, spotting, leadership, ROF and IF separately, including linked vehicle/PRC commitments. Show what firing, Opportunity Fire, labor or a final CA change gives up before the player commits. A retained-ROF weapon remains eligible for another decision; a completed attack does not mean that weapon has finished the phase. The first non-SMOKE attack can close a side-wide window, so the guide needs shared timing state rather than isolated per-weapon checkboxes.

Movement reconciliation is documented in the [Movement walkthrough](ASL%20Movement%20Phase%20Component%20Walkthrough.md) and [Movement checklist catalog](ASL%20Movement%20Phase%20Checklist%20Catalog.json): 114 topics, 92 contextual vocabulary labels, and all 20 printed MPh ASOP steps on physical PDF pages 679-680. The ledger accounts for every action family named in those steps, including special ground movement, defensive responses, aerial landings and cleanup. Supplemental records cover Minimum Move, Gun Duels, OVR Prevention, prisoner escorts and immediate recapture CC, Jitter and Chapter W movement/searchlight procedures. Named external-module gaps and scenario/equipment-note limits remain explicit. This is not a claim that every possible SSR or Chapter H note has been individually audited.

One source tension remains visible: ASOP 3.32A lists dropping prisoners during MPh, while A20.53 refers voluntary abandonment to A20.5, which names friendly RPh/APh. Preserve the catalog entry and both references; do not infer an executable MPh abandonment permission until that interpretation is settled. This is different from an implementation gap.

Movement requires four separate scopes: THE MPh, ITS MPh for a unit/stack, an expenditure or group impulse, and its response/consequence window. The proposed live guide records an expenditure receipt and offers distinct End this unit’s move and End phase decisions. Search is a last-window option that can itself draw defensive fire. A Stop followed by unloading and an entry followed by Recovery are separate expenditures. Required effects and mandatory TPBF cannot be cleared by a generic pass. Vehicle OVR must retain its before-OVR fire and after-OVR Reaction Fire windows, including required remaining attack effects after immobilization or destruction.

Movement uses a timing selector to keep its longer catalog navigable; every topic remains reachable, including pending rules. The preview illustrates explanations and consideration, not movement execution, legality evaluation or automatic obligation resolution. Its eventual live model must preserve the same complete catalog while using viewer-safe Rules projections.

Defensive Fire reconciliation is documented in the [Defensive Fire walkthrough](ASL%20Defensive%20Fire%20Phase%20Component%20Walkthrough.md) and [Defensive Fire checklist catalog](ASL%20Defensive%20Fire%20Phase%20Checklist%20Catalog.json): 72 topics, 116 contextual vocabulary labels, and all eight printed DFPh ASOP steps on physical PDF page 680. The review includes opening Dispersed SMOKE/IR, artillery and Creeping Barrage schedules, normal and restricted Final Fire, aircraft with ATTACKER AA responses, five Clearance declaration families, immediate Flame/Hamper results, equipment handling and configuration notes, final CA/AA choices and day/night cleanup. Deferred rules and named external-module gaps remain visible.

Defensive Fire adds a carry-forward capability view: distinguish an unmarked weapon retaining ROF, a First-Fired unit with adjacent/same-hex restrictions, and a Final/Intensive/No Fire subject with no ordinary further shot. Preserve MPh direction, CA and equipment-note commitments. Remove FFNAM/FFMO without resetting vehicle moving-target history or Hazardous Movement. Final Fire can affect stationary co-occupants. Optional firing, work and holding remain choices; required consequences retain their own gates.

The eight-phase preview now opens on Close Combat and preserves separate selections and consideration state for every phase. The proposed live checklist also carries consequences forward: a Clearance declaration resolves later, a Hampered Flame changes AFPh spread, and night First/Final Fire remains tied to its original Gunflash Location. End-phase CA/AA choices occur before daytime marker cleanup, so cleanup cannot create a retroactive firing-arc opportunity. Reference consideration still performs no game actions.

Mandatory effects must be attached to their originating event: WP checks, FFE terrain changes, attack results, Sniper responses and Field Phone cuts. Chapter W searchlights and FAC-directed Close Air Support are represented despite pending integration; the latter creates a DEFENDER Light AA response window after FFE. Ordinary Prep Fire attacks remain ATTACKER actions. The source's Mopping Up building conditions and the currently recorded any-building implementation differ; the final design must resolve that mismatch rather than silently using current behavior as the rule.

Advancing Fire reconciliation is documented in the [Advancing Fire walkthrough](ASL%20Advancing%20Fire%20Phase%20Component%20Walkthrough.md) and [Advancing Fire catalog](ASL%20Advancing%20Fire%20Phase%20Checklist%20Catalog.json): 58 topics, 91 contextual vocabulary labels and all nine printed AFPh steps on physical PDF page 680. It covers drift, opening WP and Gusts; glider contents; ordinary, Assault and Opportunity Fire; ordnance and vehicle carryover; Placed/Thrown/Set DC; Gun handling; berserk return; both fire-spread stages; and typed day/night cleanup. Supplemental entries include climbing, fording, sewer fire, cavalry Charge restrictions, DC Breaches, Goliath, LC Blaze damage, AA-mode preparation and searchlight exceptions. Pending and deferred rules remain visible.

The AFPh design adds per-squad FP arithmetic, separate operator/weapon movement histories, and provenance for preserved Opportunity Fire and D3.32’s single vehicle carryover shot. Required DC attacks belong to the placement record, not the carrier’s current eligibility. Fire spread is two ordered queues: eligible old Blazes create new Flames; eligible old unhampered Flames can become Blaze, extinguish, or remain. Newly created Flames cannot cascade through both stages. New terrain Blaze creates a linked Rout escape obligation. End-phase CA/AA eligibility is assessed before marker cleanup; a switched-on Chapter W searchlight retains its Gunflash. These are proposed live behaviors, not capabilities executed by the reference preview.

Rout reconciliation is documented in the [Rout walkthrough](ASL%20Rout%20Phase%20Component%20Walkthrough.md) and [Rout catalog](ASL%20Rout%20Phase%20Checklist%20Catalog.json): 56 topics, 51 contextual vocabulary labels and all four printed RtPh steps on physical PDF page 680. The catalog separates mandatory departure, optional DM rout, destination/path legality, Interdiction and surrender. It includes opening Voluntary Break/DM/Disrupted surrender; load and leader choices; known-enemy history; Low Crawl; captor decisions and conditional Interrogation; mine/FFE/Panji/flare/Column effects; night, ski, evacuation and terrain exceptions; and the separate Blaze deadline. All identified pending and deferred options remain visible.

The proposed live Rout guide explains nearest destinations by MF, lawful ignored candidates, and chosen shelter costs before a unit moves. It records known-enemy history and MF already spent when a newly Known enemy forces recalculation. An accompanying leader’s elimination risk is shown before commitment. Surrender switches decision ownership to the captor; it is not an optional route chosen by the broken unit. Night replaces ordinary Rout with its E1.54 procedure but does not remove the B25.4 Blaze deadline. End unit, end side and end phase are separate decisions. The reference preview demonstrates these explanations and consideration state, not route adjudication or execution.


Advance reconciliation is documented in the [Advance walkthrough](ASL%20Advance%20Phase%20Component%20Walkthrough.md) and [Advance catalog](ASL%20Advance%20Phase%20Checklist%20Catalog.json): 57 topics, 61 contextual vocabulary labels and all nine printed APh steps on physical PDF page 681. Rally through Advance contribute 460 topics. Opening transfers, ski-use checks, Japanese T-H Hero creation and surf obligations precede ordinary advances. The catalog includes load and leadership, difficult terrain, PAATC, concealed/enemy entry, terrain connections, night, skis, caves, Columns, boat/swimmer exceptions, CE/WA, both drift windows, FFE/mines/Panji/flares, Drowning and parachute replacement. Pending and deferred rules remain visible.

The proposed live Advance guide uses a per-unit advance receipt: origin/destination status, retained load, leader support, non-Double Time capability, entry cost excluding SMOKE, required checks and resulting CX or combat obligations. Passing PAATC commits immediate advance before another unit’s outcome is known. Exit mines resolve before arrival and may prevent the unit from leaving or discovering its destination. Tunnel emergence and T-H Hero target obligations can be compulsory even when ordinary advance is declined. Moderate river current, Heavy Current and Heavy-Surf Ocean drift have separate ownership and exceptions. Drowning retains its end-of-friendly-APh timing. CCPh reveal, Ambush, attacks, concealment gain and Pin/TI cleanup are not performed early.

Current integration labels reflect the amended coverage through Pass 71. In particular, APh wounded-man carrying remains backlog 74 despite MPh carrying being built, and the recorded A4.72 SMOKE-cost fault is not repaired by this design work. All nine ASOP steps have a clause ledger; module/SSR/equipment completeness remains a separate source-profile obligation.


Close Combat reconciliation is documented in the [Close Combat walkthrough](ASL%20Close%20Combat%20Phase%20Component%20Walkthrough.md) and [Close Combat catalog](ASL%20Close%20Combat%20Phase%20Checklist%20Catalog.json): 78 topics, 79 contextual vocabulary labels and all 17 printed CCPh steps on physical PDF page 681. The preview now contains all eight phase catalogs, totaling 538 reference topics. It includes opening disclosure, Ambush, Japanese hero creation, sequential precedence, prisoner escape, SMC pairing, Withdrawal, attack allocation, HtH, capture, vehicle/PRC/sN/ATMM procedures, Hara-Kiri, immediate results, Infiltration, Field Promotion, equipment loss, terrain profiles and Location closure. The phase-wide queue includes Dogfights, all named Clearance families, Labor, Recovered tunnel entrances, status and replenishment checks, concealment and night expiry.

The proposed live CCPh design uses two queues: completely resolve one Location before the next, then complete phase-wide obligations. A declaration receipt freezes attackers, defending groups, targets, mode, strength and modifiers where simultaneous commitment requires it; sequential attacks reopen selection after each resolved result. Prisoner attacks can outrank Ambush and vehicle sequencing. G1.421 delays a defending T-H Hero attempt until attacker Ambush attacks finish. Infiltration is immediate, captures may remain provisional until simultaneous outcomes settle, and Melee holds must be recorded per unit. Above-Panji Infantry cannot Withdraw from CC off the counter. Required consequences cannot be dismissed by marking topics considered.

Clearance consumes earlier declarations before Pin/TI removal. Ammo Replenishment and Pack-TI retain their own durations. Concealment is assessed before Starshell/IR removal, while trip flares expire according to their activation phase. The catalog distinguishes verified rule families, current partial integration and named module source gaps. Recent Pass 65/70/71 capture, Street Fighting, mixed vehicle combat and Field Promotion work is acknowledged rather than mislabeled as absent. All ordinary phase catalogs being present does not mean that every scenario source or Rules implementation pass is complete.

## Cross-phase dependency and cleanup audit

The [Cross-Phase Dependencies and Cleanup Audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md) and its [dependency catalog](ASL%20Cross-Phase%20Dependency%20Catalog.json) connect the existing phase topics through 38 timing chains and 191 links, with source events, due windows, retained history and interruption conditions. The preview now provides a Cross-phase timing selector and contextual links back to the relevant phase checklist. These are reference relationships, not a ninth phase or an executable scheduler.

The audit re-read all four ASOP pages, 678-681, and cited rulebook passages. It makes several previously implicit requirements explicit: CX expiry and the surviving Double Time restriction; smoke-source ownership and dependent drift removal; next-RPh Shock versus end-turn Stun; later-phase Trip Flares surviving to the next AFPh; Flame Hamper outcomes; subject-specific Blaze deadlines; compulsory tunnel emergence; staggered airborne release; and interruptions to complete-Game-Turn work. All identified pending rules remain discoverable.

The live model needs stable owner-side identity independently of ATTACKER/DEFENDER roles, creation and due timestamps expressed in ASL windows, source-to-effect links, interruption predicates, and exactly-once resolution evidence. Counter removal must not erase history needed by a later rule. A phase-close preview should explain what resolves now, what remains and what becomes due later. Consideration is never evidence that a mandatory effect was resolved.

The earlier blanket statement that specific rule timing overrides ASOP is corrected in the Close Combat walkthrough. ASOP governs ordering conflicts; ordinary eligibility and restrictions still apply. The MPh prisoner-abandonment permission tension remains an explicit interpretation issue, not an implementation gap or an automatic timing override. External module/SSR source profiles remain outside any claim of complete reconciliation.

## 1. Design objective

Build ASL.Turns as an independent back-end library describing all eight ASL Player Turn phases and producing the guidance a future UI displays. Its contracts and behavior must be testable without Blazor. A later reusable component helps players understand, consider and resolve choices by consuming those results. Its design target is the completed ASL.Rules implementation after all planned passes, not the subset playable today.

**All options must be represented from the outset.** An option whose rules are not integrated remains visible, with its timing, explanation, citations, and a **Rules integration pending** label. Implementation status must never be used as a content filter. A player should be able to learn about an option before the engine can execute it.

The strongest design is a phase guide backed by an option catalog, with a contextual checklist layered over it. The catalog answers “What can happen here?” The checklist answers “What requires attention in this position, whose decision is it, and when does the opportunity close?” Neither should suggest that exercising every optional action is good play.

The existing program of passes targets Chapters A to E, with explicit rulings and deferred Chapters F, G, and H. Represent those boundaries honestly: planned A to E options are included regardless of integration status; known deferred chapter families remain discoverable as deferred reference topics. Complete coverage of deferred chapters requires their own content inventory. Completion of the current passes must not be described as implementation of every ASL module.

## 2. Recent progress and reusable foundations

The checkout examined is at `08c80da8`. Recent history records the following relevant work:

| Evidence | Relevance to this component |
|---|---|
| `0b5b9a1d`, acronym search page and catalog; merged into the Pass 71 branch by `00fd8894` | An embedded, reviewed catalog of 685 acronym identities, reusable search and details components, and a Reference navigation group already exist. |
| `d1c18a3a`, Pass 71 hidden-information fixes | Guidance must preserve the viewer's knowledge boundary, including counts, explanations, and map highlights. |
| `a47712b7`, sniper and wound rules; `34862753`, Rules boundary fixes | The guide should consume Rules-derived answers through the existing play architecture, rather than implement a second rules system in Razor. |
| `889d4f1e`, whole-pass Studio checks; `53540236`, documents; `08c80da8`, gate notes | History records Pass 71 reaching its gate. This analysis does not imply that the branch has been merged or independently rerun its checks. |

The current phase identities already exist in `ScenarioA1Definitions.Phases`: `rph`, `pfph`, `mph`, `dfph`, `afph`, `rtph`, `aph`, `ccph`. `Phases.All` exposes them to unit state. Reuse these identities rather than introducing competing phase numbering.

`Play.razor` already distinguishes the side entitled to end a phase, obligations that prevent ending it, and movement response windows. `MovementWindowActions` already separates a defender's pass from ending a move. `PlayContextHeader` preserves turn, phase, pending review, and hand-over context. The new guide should organize and explain these interactions, not create another phase controller.

The existing Play streamlining work favors relevant actions, fewer routine confirmations, and map-centered interaction. A checklist that requires checking every optional item would undo those gains. Keep unavailable actions out of the executable action list, while retaining their explanations in the guide's all-options view.

## 3. Phase structure and player roles

The sequence is:

```text
RPh -> PFPh -> MPh -> DFPh -> AFPh -> RtPh -> APh -> CCPh
                  |
                  +-- moving actions and defender response windows

After CCPh: exchange ATTACKER and DEFENDER roles for the next Player Turn.
After both Player Turns: advance the Game Turn, subject to scenario ending rules.
```

A3 and A3.9 distinguish Game Turn from Player Turn. ATTACKER and DEFENDER here are turn roles, not permanent scenario identities. The displayed phase owner, the person viewing the screen, and the side currently required to decide can all differ.

Show, for example, **Turn 3 / German Player Turn / Movement Phase / Russian response**. Label a future phase selected for reading **Browsing Advance Phase**, while leaving the live phase evident. Selecting a phase must never advance the game.

The eight phases need internal timing windows. “Start,” “during,” and “end” are useful display groupings, but they are insufficient execution identities. Some options occur before another action, after a particular expenditure, on a qualifying roll, or in an ordered subprocedure. The detailed catalog must preserve the Advanced Sequence of Play and the governing rule's precedence.

## 4. Concepts and vocabulary by phase

This table is the conceptual spine, not an exhaustive executable sequence. Rule references identify the governing families; exact substeps and exceptions belong to individual option records.

| Phase | Decision focus and concepts | Initial vocabulary links | Rule anchors |
|---|---|---|---|
| Rally, RPh | Restore capability; sequence recovery, repair, transfers, deployment or recombination, and rally attempts; consider self-rally and leadership; resolve applicable recovery and environmental procedures. Both sides participate under their timing rules. | DM, SW, MMC, SMC, DR, drm, HOB; self-rally, Good Order, repair, deployment | A3.1, A10.6, A18.11; relevant weapon, vehicle, and environmental rules |
| Prep Fire, PFPh | Decide who fires now, reserves Opportunity Fire, or performs labor; consider SMOKE and OBA; recognize the resulting movement and later-fire commitments. | FP, IFT, ROF, LOS, TEM, TH, TK, OBA, TI; Opportunity Fire, Acquisition, SMOKE | A3.2, A7.25; Chapters B and C for applicable tasks and weapons |
| Movement, MPh | Choose routes, movement modes, carrying and transport actions, and expenditure order. Interrupt movement for the defender's responses. Consider Assault Movement, Double Time, Dash, Bypass, Smoke placement, vehicle actions, Bounding Fire, Overrun, and special movement. | MF, MP, PP, CX, DFF, SFF, FPF, FFNAM, FFMO, LOS; Residual FP, Fire Lane, Bounding Fire | A3.3, A4, A8, A9; Chapter D and special movement rules |
| Defensive Fire, DFPh | Use remaining defensive fire opportunities within marker, weapon, target, and range restrictions; distinguish Final Fire from the preceding movement responses. | First Fire, Final Fire, ROF, IF, FP, TH, TK, OBA; Acquisition | A3.4, A8; Chapters C and D |
| Advancing Fire, AFPh | Exercise eligible fire after movement; distinguish normal AFPh penalties, Opportunity Fire, and weapon exceptions; resolve applicable placed-charge or specialized attacks at their proper timing. | AFPh, FP, FT, MOL, DC, TH; Opportunity Fire, Bounding Fire marker | A3.5, A7.24, A7.25, A22, A23, C.4 |
| Rout, RtPh | Resolve mandatory and permitted routing, legal destinations and paths, Interdiction, Low Crawl, surrender, and accompanying leaders. ATTACKER routs first. | DM, LOS, KEU, NMC; Interdiction, Low Crawl, Failure to Rout, surrender | A3.6, A10.5, A20 |
| Advance, APh | Position eligible Infantry for cover, support, objectives, or CC; distinguish a hex advance from a change of Location; assess difficult terrain, portage, CX, and entry against an AFV. | APh, CX, PP, MF, PAATC; Location, ADJACENT, CC | A3.7, A4.7, A11.6 |
| Close Combat, CCPh | Resolve applicable ambush, withdrawal, attacks, special CC procedures, and continuing Melee; resolve eligible concealment gain and end-of-turn marker handling. | CC, CCV, HtH, DR, drm; Ambush, Melee, Infiltration, concealment | A3.8, A11, A12.12, A18.12 |

### Vocabulary relationships matter more than phase buckets

A term can be used in several phases, with different implications. LOS and leadership are cross-cutting concepts; DM connects Rally and Rout; CX connects movement choices to later movement and combat constraints. Opportunity Fire is declared in PFPh and exercised in AFPh. A Bounding Fire marker is not proof that the same action occurred in every context.

Store a many-to-many relationship between options, phases, and acronym identities. Give a link a role such as **declared here**, **resolved here**, **constrains this choice**, or **possible consequence**. This explains why a term appears instead of simply presenting a long alphabetized list.

Reuse exact catalog IDs, such as `main:DR`, `main:dr`, `main:DM`, `main:DFF`, and `main:PAATC`. Preserve case and context. Do not invent an acronym for a plain-language concept, or infer phase membership from the acronym search's historical page hits. Those hits are discovery evidence, not rules classification.

Show a small vocabulary set beside the selected option, with full definitions available through `AcronymEntryDetails`. Offer **All terms for this phase** separately. The complete reviewed definition stays accessible even when a short contextual explanation appears inline.

## 5. Representing every option

Eight phase summaries alone cannot satisfy the all-options requirement. The feature needs an authored **phase option catalog**, built against the rule inventory and detailed sequence of play, independent of engine implementation status.

### Coverage families

The initial content effort must account for all of these families, rather than stopping at Infantry basics:

| Family | Required representation |
|---|---|
| Infantry and leadership | Rally, self-rally, deployment, recombination, leadership, movement modes, fire modes, rout, advance, CC, concealment, capture, prisoners, special morale states, and nationality exceptions. |
| Support weapons and ordnance | Possession, recovery, transfer, repair, dismantling where applicable, portage, firing modes, ammunition choices, ROF, Intensive Fire, Acquisition, covered arcs, malfunction and breakdown consequences. |
| Vehicles and transport | Start/stop and Motion choices, facing, crew exposure, passengers and riders, loading and unloading, abandonment and recrewing, towing, Bog recovery, Overrun, Bounding Fire, Reaction Fire, and vehicle-specific damage or recovery decisions. |
| Artillery and smoke | Observer and radio procedures, access and contact, spotting and correction, mission and ammunition choices, SMOKE and WP, continuation and cancellation, and their timing restrictions. |
| Terrain and engineering | Entrenching, clearance, fortifications, obstacles, mines, wire, demolition, buildings and levels, rubble, fire, bridges, water, and associated movement or labor procedures. |
| Environment and special operations | Wind, weather, night, illumination, concealment and cloaking, snow and ice, special movement, cavalry, skis, air support and AA, airborne and waterborne procedures, and special transport. |
| Scenario and turn administration | Reinforcements, SSR-defined timing, victory and game-end checks, turn ownership, compulsory effects, marker transitions, and any scenario-specific overrides. |
| Deferred rule families | Discoverable Desert, PTO, and DYO topics with their explicit scope labels. Do not imply complete coverage of these chapters from the A to E inventory. |

These are coverage headings, not assertions that every named procedure is legal in every phase. An option can have multiple timing records and can be an automatic effect, a mandatory decision, an optional action, or a modifier to another action.

### Inventory reconciliation

For every numbered rule in the A to E inventory, record one of:

1. Creates an option or obligation, with its catalog IDs.
2. Constrains or modifies existing options, with their IDs.
3. Defines vocabulary, a table, or background with no independent player decision.
4. Is excluded or deferred by an identified ruling, with a visible scope explanation.

Reconcile the detailed ASOP steps against that mapping as a separate check. All eight ordinary phases now have printed-step ledgers in their walkthroughs. A broad “Vehicle actions” card is not coverage of all vehicle choices. Give independently timed decisions their own identities; keep target selection and numeric parameters inside an action unless their timing or consequences make them separate decisions.

**Content completeness is a release condition for the all-options claim.** The tables in this analysis establish the structure and seed content; they do not claim to enumerate every ASL option. The first UI release must include the reconciled option catalog, including pending entries, rather than silently filling missing families in later passes.

### Keep source, support, applicability, consideration and execution separate

| Dimension | Example values | Meaning |
|---|---|---|
| Rules integration | Integrated; Partially integrated; Rules integration pending; Deferred by scope | What the software supports, including the supported subset where partial. |
| Applicability now | Available; Required; Not applicable; Cannot determine from available information | What is established for this scenario, position, timing, and viewer. |
| Source review | Reconciled; Interpretation open; Source missing | Whether the governing evidence supports the proposed procedure. |
| Player consideration | Unreviewed; Considered; Bookmarked for a named window | Private presentation preferences, with no game effect. |
| Execution | Not started; Declared; Committed; Due; Resolved; Cancelled; Interrupted | Authoritative game facts and event evidence, separate from consideration. |

“Not integrated” does not mean “illegal.” “Considered” does not mean “performed.” “No applicable choices” does not mean that an evaluator is absent. If evaluation is unavailable, show **Availability not evaluated** rather than deriving a false answer.

An all-options view always includes pending and partial entries. Scenario filters may organize relevance, but unknown relevance remains visible. Known irrelevant entries can be collapsed with an explanation and restored with **Show all phase options**. Rule-integration labels are supplementary information, not the primary visual hierarchy.

Example pending entry:

```text
Entrench
Prep Fire Phase / applicable labor window
Rules integration pending
Consider whether eligible units should spend this opportunity improving protection.
Timing and eligibility: governing terrain and labor rules.
[Read rule explanation] [Mark considered]
```

The entry does not offer an executable action until its required rule path is supported. A manually considered pending action must not create a simulated event, change a unit, or make the end-phase gate believe an obligation is resolved. Supporting external tabletop adjudication would require a separately designed recording workflow.

## 6. The contextual checklist

Organize live attention into three groups:

- **Must resolve:** verified obligations, compulsory choices, and pending response windows.
- **Can choose now:** applicable optional actions, plus relevant pending options whose availability cannot yet be evaluated.
- **Before leaving:** unresolved obligations, expiring opportunities, and the consequences of the phase transition.

Do not give every item a checkbox. An obligation needs **Resolve**; an optional action may need **Open action**, **Considered**, or **Remind me at…**; an automatic consequence needs a brief explanation and, after resolution, a record link. A failed rally attempt can be resolved even though the unit remains broken. A weapon that retains ROF may still have another opportunity after a completed attack.

Checklist identity must include the option, subject, Player Turn, and specific decision window. Completing one squad's rally attempt does not complete Rally for the side. Passing one defensive-fire window does not mean passing all later expenditures. The existing explicit phase-pass preference is a separate user decision and must remain distinguishable.

### Innovations worth prototyping

| Idea | What the player sees | Benefit and constraint |
|---|---|---|
| Consequence preview | “Prep firing commits this unit against normal movement in the coming MPh.” | Shows what a choice closes off. Use Rules-derived, unit-specific qualifications; do not turn a general reminder into an unconditional rule. |
| Last useful moment | “Opportunity Fire is designated in this PFPh.” | Surfaces a decision before its window closes. Alerts must respect exact timing and avoid repeated prompts for a consciously declined option. |
| Future-phase bookmark | “Review this unit's advance after fire is resolved.” | Lets players plan without committing future actions or reserving eligibility. Reevaluate at the future window. |
| Per-unit option review | A selected unit's outstanding choices across the current and later phases. | Connects the map to the phase guide. Track independent weapon capabilities, attachments, and movement states rather than one “unit done” flag. |
| Defender response focus | “Response to this expenditure,” with qualifying fire choices and pass. | Makes the defending player an active participant during MPh. Preserve movement-plan and phase-pass behavior. |
| Explain absence | “Why is this action unavailable?” | Exposes timing or friendly-unit constraints. Explanations must never reveal hidden enemy facts. |
| Private decision notes | “Save this weapon for the next target.” | Helps deliberate restraint without generating a tactical recommendation or sharing a plan with the opponent. |
| End-phase digest | “No verified obligations remain; two optional topics are unreviewed; one topic is not evaluated.” | Makes omissions visible without requiring every optional choice to be checked. Never display “all legal options exhausted” on incomplete evidence. |

Start by prototyping consequence previews, last-moment reminders, and defender response focus. They address timing mistakes directly. Defer automatic tactical ranking: the best choice depends on objectives, risk appetite, and information the player may not possess.

### A concrete movement example

1. A player selects an eligible Infantry unit. The guide lists its movement choices and relevant terminology, including MF, PP, CX, and Assault Movement.
2. The player proposes an expenditure through the existing movement interaction.
3. The defender's window becomes current. The defender sees the response choices that its own view may disclose, with pending rule options still available for reference.
4. If fire occurs, the resulting checks and compulsory decisions take precedence. Sniper and wound consequences appear only when the rules trigger them; they are not checklist chores at the end of MPh.
5. The defender passes or finishes the applicable response. The checklist returns to movement, preserving the expenditure's identity.
6. Before ending movement, the guide summarizes still-open decisions and future bookmarks. It neither fabricates target knowledge nor delays a valid pass for optional reading.

## 7. Future visual and interaction design, deferred until backend readiness

Use a compact, linear eight-phase strip. Each phase shows its abbreviation and accessible full name; the current phase and the browsed phase have distinct textual states. A wheel communicates repetition, but offers less room for names and makes ordered keyboard navigation harder. It is a secondary learning experiment, not the recommended live control.

On a wide display, put the strip above a selected-phase panel with the checklist and a contextual explanation area. In Play, fit that panel into the established action workspace and keep the map visible. On a narrow display, use a wrapping or vertical phase list and one expanded detail panel. Avoid eight permanently expanded cards.

Example live panel, using illustrative content rather than measured game counts:

```text
Turn 3 / German Player Turn / Movement Phase
Viewing: Russian side / Current decision: response to movement

RPh  PFPh  [MPh]  DFPh  AFPh  RtPh  APh  CCPh

MUST RESOLVE
Respond to the current movement expenditure              [Open response]

CAN CHOOSE NOW
Review Defensive First Fire                              [Open action]
Review other response options                            [Browse options]

BEFORE LEAVING THIS WINDOW
Passing closes this response opportunity                  [Pass]

Selected topic: Defensive First Fire
Timing | Conditions | Consequences | Related terms | Rules
All phase options, including rules integration pending    [Browse all]
```

Provide two uses of the same component:

- **Reference:** all phases and options, full explanations, vocabulary, and rule links without a live game.
- **Live:** the same catalog, contextualized by scenario, unit selection, viewer, and current decision window.

Reference mode does not label fictional units eligible. Live mode can browse future phases without altering current state. A beginner explanation level adds prose; an experienced level compresses it. Neither hides options because they are not integrated.

Reuse `site.css` tokens and the existing theme. Preserve acronym case even in headings. Use native buttons and disclosures, visible focus, text status labels, and accessible names. Avoid hover-only definitions and color-only distinction between pending and available. Keep focus stable as consequences change the checklist, and announce important changes without reading every optional item aloud.

## 8. ASL.Turns backend and Rules integration

The new library is src/ASL/ASL.Turns, with tests in src/ASL/tests/ASL.Turns.Tests. Both are in the ASL solution. The initial immutable reference catalog loads the eight phase inventories and cross-phase dependencies without a UI, Play service or external file path. It preserves qualified topic identity, phase-specific terms, pending notes and source evidence. The B1 coverage gate now validates the complete implemented library with 62 tests and 100% line/branch coverage; it does not establish live rule execution.

The final dependency direction is Play to ASL.Turns to Rules, with the existing Play to Units to Rules path retained. ASL.Turns must not reference Play, Units or MapStudio. Rules receives narrow facts, not a game/page object. Play translates current state and history into those facts and applies the authenticated GameView disclosure policy. Units remains the event-derived state authority.

ASL.Turns will compose immutable guidance snapshots, capability and applicability assessments, dependency instances and phase-close explanations. Rules decides rule-specific prerequisites, due windows, interruptions and cleanup. Play uses the existing proposal/confirmation path for every action; reading guidance cannot mutate state or roll dice. The UI eventually renders the snapshot and submits scoped intentions, with no rule or scheduling logic.

Separate authored dependency definitions from live obligations. Preserve stable owner identity, source event, Game Turn/Player Turn/window, due specification, progress and resolution evidence. Use semantic timing and event dependencies rather than wall clocks or a simple numeric ASOP sort. Derive from existing facts first; add missing durable rule facts through reviewed Units/Play changes. Do not introduce a second mutable game state or an editable obligation store.

Keep source review, implementation, applicability and authoritative lifecycle separate. A private Considered preference never becomes a Resolved fact. Unknown applicability stays visible and does not become a fabricated mandatory blocker. A proven required but unsupported effect belongs in the shared planner assessment, with no guide-only bypass.

The existing PhaseEndBar is not comprehensive PlanAdvance validation. Extract shared read-only assessment incrementally and preserve projector validation. Every cleanup has one mutation owner, either its existing projector transition or an explicitly migrated event. Verify ordered effects, idempotent retries and legacy replay before changing any family.

The [backend design](ASL%20Turn%20Phase%20Guide%20Design.md) specifies contracts, authority, disclosure and timing in detail. The [implementation plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md) requires backend readiness before UI construction. Existing component ideas are consumer requirements, not the current implementation order.

## 9. Hidden information and performance

Evaluate and project for the viewing side before exposing content to the component. Hidden-information protection includes option counts, warning presence, explanations, sort order, disabled-state hints, route overlays, and timing. A prompt such as “hidden enemy prevents this route” is a leak even if it omits the enemy's identity.

Generic rules reminders may be shared; private decisions and evaluated choices may not. Clear the prior side's private panel state on hand-over. Never pass an omniscient option list to the browser and rely on CSS to hide it. Where lawful availability cannot be disclosed, show a neutral explanation or the normal adjudication path without using hidden state to signal the answer.

Follow the standing memory and performance guidance. Retain immutable catalog content once; reuse the planner's existing state-scoped map and rule reads. Do not scan the whole rulebook, replay all history, recompute every LOS, or search every rout destination on each render. Load details for the selected option and render only relevant groups. Cache evaluated answers with all relevant inputs, including state revision, viewer, rules version, scenario context, and decision window; bound their lifetime.

Compare later prototypes against the same replay position and viewer. Measure added latency, allocations, rendered rows, and clicks per decision. A complete catalog need not be a complete recomputation on every movement expenditure.

## 10. Backend-first evaluation and delivery

The first foundation is implemented: ASL.Turns and ASL.Turns.Tests load and validate all 538 topics, 38 dependency definitions and 191 links, without UI services. B1 now supplies typed capability contracts and stable timing bindings while retaining unknown/pending content. The next stages are pure snapshot composition, representative rule-owned obligation lifecycles, and Play/Units adapters with shared closure assessment. Durable missing facts and additional rule profiles follow in bounded slices.

Before any Blazor construction, prove deterministic reads, pending visibility, correct owner-relative clocks, interruption and completion evidence, stale rejection, viewer-safe output, no duplicate cleanup, and agreement between guidance and governed command validation. The test library remains independent of the UI. Existing Rules, Units and Play test projects verify their own boundaries.

Use the 38 cross-phase examples as future positive timing cases and the immediately preceding/following windows as negative cases. Add save/reload, retry, partial progress and hidden-world equivalence checks. Current catalog tests validate reference content only; these lifecycle proofs remain work to do.

The saved design preview in this folder is reference evidence. Production does not consume its JavaScript or host widget state. UI work comes last and retains all reviewed options, including rules not integrated. The full implementation sequence and exit gates are in the [plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md).

## 11. Sources and traceability

- [Chapter A transcription](../../../../docs/ASL/Rulebook_Markdown/02%20-%20Chapter%20A%20-%20Infantry%20and%20Basic%20Game%20Rules.md), especially A3.1 to A3.9, A4.7, A7.25, and A18.11. This is the basis for the phase structure and illustrated timing relationships. Detailed reconciliation of all eight ordinary phases now uses the original PDF and ASOP; scenario/module/equipment source-profile completion remains a separate deliverable.
- [Rout walkthrough and source ledger](ASL%20Rout%20Phase%20Component%20Walkthrough.md), for all four printed steps, detailed destination and surrender decisions, special rout profiles, hazard ordering and explicit source/integration limits.
- [Advancing Fire walkthrough and source ledger](ASL%20Advancing%20Fire%20Phase%20Component%20Walkthrough.md), for all nine AFPh steps, detailed fire exceptions, demolition and terrain obligations, pending integrations and source limits.
- [Defensive Fire walkthrough and source ledger](ASL%20Defensive%20Fire%20Phase%20Component%20Walkthrough.md), for all eight printed DFPh steps, prior-use restrictions, labor, aircraft, day/night cleanup and source limits.
- [Movement walkthrough and source ledger](ASL%20Movement%20Phase%20Component%20Walkthrough.md), for all 20 printed MPh ASOP steps, supplemental movement and response procedures, physical PDF locators and explicit source limits.
- [Rule inventory A to E](../ASL%20Rule%20Inventory%20A%20to%20E.md), [Rule Coverage](../ASL%20Rule%20Coverage.md), and [Redesign Plan](../ASL%20Card%20Play%20and%20Map%20Studio%20Redesign%20Plan.md), especially the scope decisions and complete-coverage pass schedule.
- [Acronym Search Analysis](../ASL%20Acronym%20Search%20Analysis.md) and [Acronym Search Design](../ASL%20Acronym%20Search%20Design.md), plus the committed catalog and components under `LimboDancer.Domains.Asl.MapStudio`.
- [Play Streamlining Analysis](../ASL%20Map%20Studio%20Play%20Streamlining%20Analysis.md), for existing interaction goals and movement-response friction.
- [Pass 71 Design](../Passes/ASL%20Unit%20Backlog%20Pass%2071%20Design.md), for current sniper, wound, and disclosure integration context.
- [Memory and Performance Guidelines](../Memory-And-Performance/ASL%20Memory%20and%20Performance%20Guidelines.md), for projection lifetimes, viewer-sensitive reuse, and incremental work.

The phase matrix is a design-level synthesis, not a replacement for the detailed rules or ASOP. New UI behaviors and contracts in this document are proposals; the cited existing components and commits describe the current integration foundation.

B1 update, 2026-10-27: the backend design records the validated 117-group/176-window identity manifest, ten imported Rally ledger steps, independent capability dimensions and required 100% coverage gate. All 538 topics and 38 dependency definitions remain visible. No live legality or due-window evaluator is claimed.

B2 update, 2026-10-27: pure guidance composition now retains all reference topics and consumes audience-authorized typed verdicts. Unknown capability/subject facts stay NotEvaluated; historical or browsed-phase reads cannot expose action routes. Internal evaluation stamps stay out of snapshots. The full library passes 88 tests with 336/336 lines and 176/176 branches covered. Rule evaluation, obligation lifecycles and governed host adapters remain B3/B4 work.

B3 update, 2026-10-27: Rules-owned Stun, CX, Trip Flare and non-Flame Clearance timing now feeds a read-only obligation coordinator with stable origins, progress/outcome evidence and prerequisite ordering. All dependency definitions remain visible; unsupported families and command integration remain pending. The internal assessment requires B4 disclosure before any UI use. Verification: 116 Turns tests, 425/425 lines and 234/234 branches; 28 focused Rules tests.


B4 update, 2026-10-27: Play supplies disclosed Stun/CX history evidence and shares its full phase-close planner with the guide. Reads never commit or draw dice. Side closure stays conservatively GovernedCheckRequired; adjudicators receive exact existing-engine assessment. Missing ASOP substeps and setup origins are not invented. All pending topics/dependencies remain visible. See the design B4 host contract and implementation plan section 7. ASL.Turns retains 100% coverage: 452/452 lines, 254/254 branches.


B5 update, 2026-10-27: the bounded Placed DC slice persists ownership at placement and derives scheduled, due, interrupted and resolved obligations from the authoritative history. Existing commands and cleanup remain authoritative. The expanded `existing-engine-dc-v1` profile has explicit evidence; all 538 topics and 38 dependencies remain available. Legacy missing ownership stays incomplete. See design B5 and implementation plan section 8 for compatibility, disclosure and remaining families. Coverage: 127 Turns tests, 465/465 lines and 264/264 branches (100%).


B5 fire/Spotter update, 2026-10-27: the new explicit profile derives disclosed firing and Spotter history from existing durable events. Recorded ROF and retained assignments are evidence, not permission to fire. First observed use is distinguished from unrecorded original designation. Replacement timing, Opportunity Fire/breakdown and remaining fire families stay pending. See the design and plan B5 fire/Spotter section. ASL.Turns: 131 tests, 516/516 lines and 288/288 branches covered (100%).


B5 atmosphere update, 2026-10-27: ordinary infantry grenade attempts and MPh cleanup now derive from causal event evidence, with recorded Wind/Gust history retained across phases. Drift, cave WP and the three dust categories remain explicit typed pending entries, including distinct timing and missing facts. Unknown SMOKE metadata is not classified or expired by the guide. See the design and plan atmosphere section for scope and compatibility. Verification: 140 Turns tests, 572/572 lines and 304/304 branches (100%); 40 Play and 15 Rules sequence tests pass.


B5 work-progress update, 2026-10-27: the internal packing/replenishment ledger counts evidenced complete Game Turns, rejects duplicate/overlapping receipts and retains earlier progress across interruptions. Rules owns replenishment/transfer duration; completion remains pending. Packing duration and live declaration/interval/completion events remain unintegrated and visible. See the design and plan work-progress section. Verification: 146 Turns tests, 651/651 lines and 366/366 branches (100%); 28 Rules and 4 host tests pass.


B5 live work update, 2026-10-27: packing/replenishment now have permissioned adjudication events, durable live lifecycle state, replay-checked Game Turn boundaries, work-owned TI and completion windows. Replenishment completion records a system Ammunition DR and its depletion result. The guide discloses only authorized work summaries and still labels packing/ammo rule automation Pending. Packing duration and eligibility are explicitly external adjudications; actual packing containment, ammunition combat state and supply/portage mutations remain unintegrated. See the new live work section in the design and implementation plan for the precise boundary. This preserves the all-Passes design target without claiming missing rules are implemented.


B5 completion-effects update, 2026-10-27: the work declaration can now bind typed Gun assembly/manning or replenishment effects. Successful completion applies them atomically with TI release. Replenishment restores actual C8.9 ammunition choices, clears recorded Low Ammo, resolves the supply's Ammunition DR outcome and updates usable Passenger capacity. Packed Guns cannot bypass their assembly state through normal use commands. Legacy work receives no invented effects, and the guide distinguishes unspecified, waiting, applied and cancelled effects while retaining Pending rule-family support. Packing transport and automatic eligibility, D3.71/B# firing, supply transfers/dumps/wrecks and destruction handling remain all-Passes work. See the new completion-effects section in the design and implementation plan.


B5 supply transfer/offload update (2026-10-27): opt-in four-Game-Turn completion now relocates the existing supply to an adjudicated same-Location vehicle or stationary Ammo Dump, changes the capacity binding, and wrecks an Immobilized source. The declaration freezes the destination; replay validates it again and applies all effects atomically. E10.12's Immobile-source restriction is enforced for available supply. E10.11 qualification, Accessible Locations, dump-backed replenishment, explicit occupant/equipment disposition, transition-turn movement restrictions and remaining E10 integration stay visible as Pending. See the new completion-effects section in the design and implementation plan.


### B5 finite-scope decision, 2026-10-27

B5 now has exactly six remaining integration slices: inventory, admitted-capability audit, durable decision windows, one live AFPh Placed DC topic route, replay/disclosure acceptance, and the backend readiness handoff. All six are Planned. The [implementation plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md#11-finite-b5-completion-backlog-agreed-scope-2026-10-27) owns IDs B5-R1 through B5-R6 and their exit criteria. Eight retained rule-family packages, B5-D1 through B5-D8, belong to later ASL.Rules passes, including Trip Flare/Clearance host execution. This bounds the integration milestone without reducing the all-Passes target or hiding pending options. Existing bounded implementations remain in place; this planning decision adds no runtime support. Safety gaps in an admitted path must be fixed or the path narrowed before B5 closes.


### B5-R1 inventory outcome, 2026-10-27

R1 is complete: the headless inventory explicitly maps every one of the 538 topics and 38 dependencies to source-review status, bounded subset evidence where available, and retained pending-work ownership. All 191 catalog links remain unchanged. The inventory revealed a real ownership omission in the earlier eight deferred packages: ordinary core-phase integration now has owner B5-D9. No additional B5 exit slice was created. Five required slices remain, starting with R2's admitted-capability audit. Evidence inventory does not promote history, timing predicates or adjudicated effects into executable topic routes. The design and implementation plan explain the immutable schema, source gaps and tests.


### B5-R2 audit outcome, 2026-10-27

Profiles now expose explicit integration bounds without promoting pending rules. The audit found and fixed known-CE admission and resume/event revalidation gaps in work effects, plus a Spotter-use disclosure path that could reflect a withheld shot. New work uses a versioned audited event contract; legacy journals replay unchanged, but old open work is read/cancel only through the live planner. The design and plan contain the per-subset fact/authority/refusal matrix. No live topic route or full G10/E10 support is claimed. Four required B5 slices remain, beginning with R3 decision-window evidence.
