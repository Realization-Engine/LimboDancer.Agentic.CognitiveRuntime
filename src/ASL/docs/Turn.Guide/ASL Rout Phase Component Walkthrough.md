# ASL Rout Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design content and interactive reference preview. No Blazor component, pathfinder or Rules behavior is implemented by this work.

Design target: the completed ASL.Rules implementation after all passes. Pending and deferred rules remain represented. See the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md) and [Rout catalog](ASL%20Rout%20Phase%20Checklist%20Catalog.json).

## Sources and reconciliation scope

Authority: supplied eASLRB v3.01 at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. All four printed RtPh ASOP steps are on physical PDF page 680. Page numbers here are one-based physical PDF pages, not printed chapter folios. The printed layout was inspected during the preceding AFPh reconciliation and its complete RtPh text was re-read for this review.

The reconciliation follows every ASOP clause and the linked A10.4-.533/.62/.711, A19.12, A20.21/.3, B25.4, E1.54, E2, E4, G9.41 and G14.41 procedures. It checks route-triggered artillery, mines, trip flares and Column Disbandment separately. A whole-volume RtPh/Rout Phase search found 166 occurrences on 75 pages, including repeated charts and training examples. These are discovery hits, not a count of distinct audited actions.

Supplemental entries include triggered SMC Recovery, Guard/prisoner accompaniment, tunnels, fortification protection, Wire, Crest, water and vertical routing, broken Inherent Crew exit, newly landed paratroops and seaborne status changes. All identified options remain visible when integration is pending. This is a complete mapping of the printed ASOP clauses and identified in-volume families, not a claim to have audited every scenario, module or equipment-note permutation.

## The Rout interaction model

Rout is a sequence of constrained decisions and compulsory results. ATTACKER completes its routs before DEFENDER. Within that order, an enemy may own a disclosure, capture or hidden-hazard decision. The live design needs separate identities for phase owner, routing side, current subject, decision owner and viewer. Reference browsing changes none of them.

The timing selector opens with “Separate required rout, permission and surrender.” Each topic displays owner, timing, source and integration status, followed by its question, checks, consequences, example and contextual terms. “Mark topic considered” records understanding only. It cannot move a unit, roll Interdiction, reject surrender, resolve an obligation or end a side’s routs.

### Five tests, not one danger rating

| Test | Question | Important distinction |
|---|---|---|
| Must rout | Can the unit legally remain? | Open Ground exposure, armed unbroken enemy proximity and Blaze are separate triggers with exceptions. |
| May rout | Is an optional rout permitted? | Ordinary DM can permit rout without requiring it. Night and Disruption change the test. |
| Legal destination and path | Where must it go, and how may it get there? | Nearest is measured in MF; the chosen route need not always be the shortest. Known-enemy history persists. |
| Interdiction | Is a particular entered hex exposed to a qualified enemy? | A spent firer may Interdict; a broken enemy may constrain direction but cannot Interdict. |
| Surrender | Must this unit become a prisoner instead? | Low Crawl immunity to Interdiction does not automatically avoid surrender. Qualifying captors and exemptions matter. |

An armed leader without SW illustrates the distinction: it can constrain legal routes and enforce Failure to Rout, yet cannot Interdict alone. Another example is the concealed enemy: it is ignored for ordinary Known-enemy routing until the rules reveal it, but can voluntarily become Known at an allowed window. A route preview must not expose that choice in advance.

### Design improvements

1. **Obligation queue:** identify units that must resolve a route, opening surrender or Blaze escape, without treating every broken counter as mandatory. Separate optional staying from an unresolved compulsory action.
2. **Destination explanation:** show nearest-MF candidates, lawful ties, optional exclusions, and known minefield/FFE avoidance. Record the selected destination and its basis before the first step.
3. **Knowledge history:** retain enemies Known to this router and the moments they became Known. Refigure a blocked destination from the current point without refreshing MF or forgetting earlier enemy constraints. The displayed record is limited to what the viewer may know.
4. **Per-step receipt:** list actual entry/exit and shelter costs, MF spent, hazard results, and continuation state. Weapon load and shelter expenditures are committed at their required timing, not retroactively after a bad result.
5. **Consequence comparison:** ordinary rout, Low Crawl, accompanying leader and evacuation loading have different costs and risks. Explain them without ranking tactical quality or promising safety from unknown hazards.
6. **Decision handoff:** a surrender asks the captor to choose the recipient and accept/reject the whole stack. Interrogation, Guard capacity and dropped equipment remain linked consequences. The routing player cannot silently answer for the opponent.
7. **Profile-aware close:** night changes Rout substantially, but does not cancel the separate Blaze deadline. Finish unit, finish side and finish phase are distinct operations; no generic Rout marker reset is invented.

### Worked daylight decision chain

The ATTACKER first considers eligible voluntary breaks and applies required DM. A Disrupted squad adjacent to a qualifying enemy has its opening surrender resolved before ordinary routes. Another broken DM squad is safe in cover and can choose to stay. A third must leave Open Ground.

For the third squad, the guide presents the nearest eligible cover by MF and any lawful destination exclusions. Before it moves, it must retain the required load within IPC and abandon excess equipment; an eligible SMC staying behind can receive the separate Recovery response. An accompanying leader is optional, but the preview explains that the leader is eliminated if the accompanied unit fails its Interdiction MC.

The player compares an ordinary route with Low Crawl. If a qualifying adjacent captor makes A20.21 surrender compulsory because all escapes require Interdiction or Low Crawl, the route selector yields to that rule. Otherwise, a chosen step can combine entry with foxhole protection if its cost is declared before Interdiction. A mine exit attack is resolved before the next hex’s entry, and mines/Panji are resolved before applicable Interdiction. FFE entry affects the routing subject rather than all occupants of the hex.

If a newly Known enemy blocks the destination, the guide preserves expenditure and earlier enemy knowledge while refiguring from the current point. It resolves any casualty, pin, wound or capture before offering continuation. After the ATTACKER’s obligations, the DEFENDER does the same. At phase end, Infantry still trapped in terrain Blaze are eliminated under B25.4, distinct from Failure to Rout. The guide then hands off to Advance.

### Worked special cases

- **Night:** show the E1.54 Low Crawl procedure, its unusual terrain/exit permissions, no ordinary RtPh surrender and no Failure-to-Rout elimination. Still evaluate enemy direction, crew exit, hazards and the separate Blaze deadline.
- **Skis:** mode change has its own cost and conditions; Low Crawl requires removal. The downhill bonus explicitly can exceed the normal broken/wounded MF limit.
- **Seaborne Evacuation:** eligible LC/amphibians can count as destination buildings. Immobile, burning or insufficient-capacity craft are ignored; a unit that will load uses four MF and pays the applicable loading cost.
- **Tunnel:** validate the passage and emergence independently. Ordinary same-RtPh entrance-to-exit requirements and remembered-enemy conditions are not a normal surface path calculation.

## Reconciled wording and boundaries

- ASOP has four printed RtPh steps, but 6.21B contains many distinct rule families. The catalog subdivides those families without inventing new ASOP numbers.
- A10.51 describes FFE rout attacks by reference to moving-target procedures. C1.51 explicitly limits FFMO/FFNAM to MPh. The Rout guide therefore does not apply those modifiers in RtPh, and it does not treat FFE as Interdiction.
- A10.5’s ordinary six-MF allowance cannot be increased by normal bonuses; E4.31 supplies an explicit ski exception. G14.41 separately gives evacuation loading a four-MF budget.
- A19.12’s ordinary Disrupted Low Crawl prohibition must be read with No Quarter and night exceptions, rather than displayed as a universal ban.
- B30.5’s ordinary pillbox forced-rout protection must be read with B25.4’s explicit Blaze effect on pillbox occupants.
- E1.54’s immunity to Failure to Rout is not immunity to casualty, mines, FFE or terrain-Blaze elimination.
- Ordinary Sniper activation does not occur in RtPh. Required morale, HOB, wounds and other actual result procedures still apply where their rules require them.

## Integration evidence and limits

Labels summarize [ASL Rule Coverage](../ASL%20Rule%20Coverage.md) and its amendments, not new engine execution tests. Pass 70 added Voluntary Break, leader Voluntary Rout, vehicle/Gun Interdiction, own-building exclusion, voluntary concealment loss and relevant morale paths. Earlier initial-snapshot statements that these are absent must not be repeated as current findings. Pass 35 repaired several ordinary rout, Interdiction and surrender paths. The catalog marks those foundations conservatively as partially integrated because full special-profile coverage is not established here.

Known remaining work includes fortification rout choices, detailed night and special terrain profiles, artillery missions and specialty transport. The pass-71 wound work lists RtPh pin handling among remaining backlog items; this analysis does not claim the new MPh wound-pin fix completed the Rout case. Deferred F/G/H/W profiles remain represented without a promise that the current A-E pass program implements them. Future live guidance must consume Rules projections rather than duplicating a second legality engine in the component.

All-options reference content is public. Evaluated routes, hidden hazards, captor availability and counts are not automatically public. The browser must receive only the appropriate viewer’s projection. A missing evaluator means “Availability not evaluated,” while a missing source is separately identified.

The catalog contains 56 topics and 51 contextual vocabulary labels.

## Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 6.11B | Voluntary Break at start; required DM for units that must rout. | `voluntaryBreak`, `dm` |
| 6.12B | Opening Disrupted surrender and conditional Interrogation. | `disruptedOpening`, `interrogationOpening` |
| 6.21B | All routs with Disrupted/night/evacuation profiles; leader accompaniment; ski changes; surrender/No Quarter/Interrogation; Failure to Rout; Interdiction/Panji; trip flares; routing-subject FFE/mine attacks; Column Disbandment. | `mustRout`, `optionalRout`, `disruptedRout`, `budget`, `load`, `recovery`, `leader`, `prisoners`, `destination`, `ignore`, `noCover`, `direction`, `lowCrawl`, `refigure`, `stop`, `openGround`, `interdictor`, `interdiction`, `reveal`, `repulse`, `surrender`, `exempt`, `accept`, `capture`, `interrogation`, `failure`, `mines`, `ffe`, `specialFfe`, `panji`, `flare`, `column`, `night`, `skis`, `evacuation`, `crewExit`, `paratroops` |
| 6.31B | Eliminate Infantry unable to leave terrain Blaze by the end of RtPh. | `blazeEnd` |

A3.6 side order applies across the phase. Supplemental entries preserve actual rule timing without inventing new ASOP numbers.

## Cataloged options

### Phase ownership and obligation review

#### Separate required rout, permission and surrender

ID: `handoff`. **Who:** BOTH. **Type:** Phase handoff.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A3.6; A10.5-.62; A19.12; A20.21. **PDF pages:** 680, 47, 66, 67, 68, 86, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Who must act, who may stay, and whose decision comes next?

- ATTACKER routs first, then DEFENDER. Ordinary routs are one unit at a time; leader accompaniment and prisoner movement use their explicit exceptions.
- Evaluate must-rout, may-rout, destination, Interdiction and surrender separately. Each uses different enemy status, range and knowledge conditions.
- Carry forward Blaze, DM, Disruption, weapons, wounds and scenario profiles. Optional consideration cannot close a required route or result.

**Checklist behavior:** Show a side-owned queue with reasons, distinct from a list of optional topics.

**Illustrative position:** A DM unit in safe cover may stay; another unit must leave; a third must surrender.

**Terms:** RtPh, DM, Surrender, Interdiction.

#### Preserve each router’s own enemy knowledge

ID: `knowledge`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A10.51; A10.533; A12.15. **PDF pages:** 680, 66, 67, 68, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which enemies constrain this unit’s route?

- Do not use hidden/concealed enemy state to reject a reference route or expose a private threat. Use lawful Known status for the specific router.
- Remember armed enemies seen earlier during this rout even after LOS is lost. A newly Known enemy can require a new destination.
- Preserve A20.21’s distinct test involving possible unconcealed Interdictors, even if not Known to the router. Adjudicate without leaking hidden identities.

**Checklist behavior:** Propose a viewer-safe threat record and explicit re-evaluation points rather than a misleading universally safe route.

**Illustrative position:** Turning a corner does not erase the armed enemy the router already saw.

**Terms:** Known enemy, LOS, HIP, ADJACENT.

### 6.11B · Opening break and DM decisions

#### Consider voluntary breaking before anyone routs

ID: `voluntaryBreak`. **Who:** BOTH. **Type:** Optional opening choice.

**Timing:** ASOP 6.11B. **Rules:** A10.41; B25.4. **PDF pages:** 680, 66, 143. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Would an eligible voluntary break enable escape?

- At phase start, require the specified armed unbroken Known enemy ground unit within LOS/Normal Range, or ADJACENCY to an unbroken enemy ground unit.
- Pin does not itself bar this choice, but breaking must not immediately Reduce or eliminate the unit. Apply explicit Blaze provisions separately.
- Record broken/DM state and reevaluate rout and surrender. Voluntary Break is not a guaranteed escape or an option offered midway through an ordinary rout.

**Checklist behavior:** Preview the resulting obligations before the player commits.

**Illustrative position:** A pinned eligible Infantry unit can break now, but its destination still needs a legal route.

**Terms:** Voluntary Break, DM, LOS, Known enemy.

#### Apply required Desperation Morale

ID: `dm`. **Who:** BOTH. **Type:** Required status update.

**Timing:** ASOP 6.11B. **Rules:** A10.62. **PDF pages:** 680, 68, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which broken units must acquire DM now?

- Check Known armed enemy ADJACENCY, the start-of-RtPh Open Ground/LOS/Normal Range condition and terrain Blaze, with applicable special profiles.
- DM grants ordinary rout permission but does not alone require departure. Non-DM safe broken units do not receive free ordinary routs.
- Do not remove DM at this phase’s close. Night retention and later Rally have their own rules.

**Checklist behavior:** Display the triggering condition and separate it from the must-rout verdict.

**Illustrative position:** A broken squad in a building can be DM yet have no duty to leave.

**Terms:** DM, Open Ground, ADJACENT, Blaze.

### 6.12B · Disrupted surrender first

#### Resolve Disrupted units’ opening surrender

ID: `disruptedOpening`. **Who:** BOTH. **Type:** Conditional opening obligation.

**Timing:** ASOP 6.12B. **Rules:** A19.12; A20.3; E1.54. **PDF pages:** 680, 86, 87, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this Disrupted unit have a qualifying captor?

- Check the start-of-RtPh ADJACENT Good Order armed Known enemy Personnel not in Melee condition; same-Location surrender also has its own immediate rule.
- Resolve applicable opening surrenders before ordinary routs. Legal escape routes do not ordinarily exempt a Disrupted unit.
- Apply No Quarter and night exceptions, then reevaluate restricted rout permission rather than treating Disrupted as ordinary broken.

**Checklist behavior:** Queue a compulsory decision with the eligible captor response.

**Illustrative position:** A Disrupted squad can surrender despite having a route to nearby woods.

**Terms:** Disrupted, Surrender, No Quarter, Known enemy.

#### Resolve applicable Interrogation after opening capture

ID: `interrogationOpening`. **Who:** Captor and prisoner owner. **Type:** Conditional result chain.

**Timing:** ASOP 6.12B. **Rules:** E2.1-.3. **PDF pages:** 680, 228. **Coverage:** Rules integration pending; reference available.

**Consider:** Is Interrogation in use, and has this prisoner already been checked?

- When E2 applies, the captor makes the immediate roll for each captured unit; never repeat it for the same prisoner merely because no benefit resulted.
- Apply prisoner/Guard modifiers, substitutions for unavailable results and the LOS/eight-hex disclosure limits.
- Let the interrogated player resolve qualifying ties and disclose only the required information.

**Checklist behavior:** Attach an optional-rule obligation to the actual capture, not a generic reveal button.

**Illustrative position:** A capture can reveal limited information, but not the opponent’s entire hidden setup.

**Terms:** Interrogation, HIP, SSR.

### 6.21B · Decide who routes and with what

#### Identify compulsory departure

ID: `mustRout`. **Who:** Routing side. **Type:** Required eligibility review.

**Timing:** ASOP 6.21B. **Rules:** A10.5; B25.4. **PDF pages:** 680, 66, 68, 143, 151, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** May this broken unit remain where it is?

- Test the same Open Ground hex in LOS/Normal Range of a Known non-Melee enemy, and same-Location/ADJACENT armed unbroken Known enemy conditions separately.
- A leader without SW still counts as armed for legal rout paths and Failure to Rout, even though it cannot Interdict alone.
- Check Melee, night, pillbox and Blaze provisions before deciding the result. Surrender can replace an ordinary rout.

**Checklist behavior:** State the precise reason departure is compulsory rather than marking every broken unit mandatory.

**Illustrative position:** An adjacent unarmed prisoner and an adjacent armed leader have different effects.

**Terms:** Open Ground, Known enemy, ADJACENT, Failure to Rout.

#### Choose whether an otherwise safe DM unit routes

ID: `optionalRout`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.5; A10.62. **PDF pages:** 680, 66, 67, 68. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Would moving toward future rally help this unit?

- An ordinarily eligible broken unit under DM may rout even when not required. A safe unit without DM normally cannot.
- Once begun, its rout follows normal destination, enemy and stopping rules. It is not free movement toward any leader.
- Staying is a valid optional choice; do not treat a marked topic as having moved the unit.

**Checklist behavior:** Separate “remain” from resolving a compulsory route.

**Illustrative position:** A safe DM squad chooses to stay in cover rather than undertake a constrained rout.

**Terms:** DM, RtPh, MF.

#### Apply Disrupted rout limits and exceptions

ID: `disruptedRout`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A19.12; A20.3; E1.54. **PDF pages:** 680, 86, 87, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this Disrupted unit rout at all?

- Use A19.12’s listed Blaze, Open Ground, Water Obstacle and adjacent in-Melee/berserk/vehicle cases; do not offer unrestricted voluntary rout.
- Ordinary daytime Disrupted units cannot Low Crawl. Apply A20.3 No Quarter and E1.54 night exceptions explicitly.
- Preserve Disruption until rally or capture; routing does not cure it.

**Checklist behavior:** Show the specific exception rather than one blanket “Disrupted cannot move” rule.

**Illustrative position:** An adjacent vehicle can require a Disrupted unit to rout even though it cannot receive surrender.

**Terms:** Disrupted, Low Crawl, No Quarter.

#### Establish the actual rout MF allowance

ID: `budget`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.5; A17.2; E4.31; G14.41. **PDF pages:** 680, 66, 85, 232, 315. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What budget applies to this subject?

- Ordinary broken units, including Conscripts, receive six MF; wounded SMC normally receive three. Do not add ordinary leader, road or Double Time bonuses.
- Track MF already spent and apply explicit exceptions: skiing downhill and evacuation loading have their own rules.
- No fresh budget is awarded after a result, destination change or failed route proposal.

**Checklist behavior:** Show starting allowance, expenditures and remaining allowance independently.

**Illustrative position:** A wounded leader cannot assume the squad’s six-MF budget.

**Terms:** MF, Wound, Skis, Seaborne Evacuation.

#### Choose the required legal SW load before routing

ID: `load`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.4; A17.2. **PDF pages:** 680, 66, 69, 85. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which weapons must stay with the unit and which must be abandoned?

- Before routing abandon excess over IPC. If several SW exceed capacity, choose a combination exactly at IPC, or the highest total below IPC when exact is impossible.
- Carry required weapons within capacity; do not discard a legal load merely for convenience. Once routing starts, no Recovery or Abandonment is permitted.
- Wounded SMC IPC and an accompanying leader’s separate load remain distinct. A leader cannot increase the broken unit’s portage capacity.

**Checklist behavior:** Offer legal load combinations and commit them before the first rout step.

**Illustrative position:** A squad leaves an overweight HMG, while a lighter legal SW load must accompany it.

**Terms:** SW, IPC, PP.

#### Offer an eligible SMC the triggered Recovery attempt

ID: `recovery`. **Who:** Eligible SMC owner. **Type:** Triggered optional response.

**Timing:** ASOP 6.21B. **Rules:** A4.44. **PDF pages:** 680, 50, 66, 69. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Has rout, surrender or elimination just left equipment behind?

- An eligible Infantry SMC in the Location can immediately attempt the specified Recovery when the unit surrenders, is eliminated or routes away unable to carry its SW.
- Check unpinned Good Order, non-Bypass, enemy and terrain restrictions, then the proper Recovery dr/modifiers.
- This does not permit the routing unit to Recover mid-route or grant arbitrary RtPh equipment transfers.

**Checklist behavior:** Attach the separate SMC response to the equipment-drop event.

**Illustrative position:** A leader staying behind may attempt to Recover the HMG its broken squad cannot carry.

**Terms:** SW, SMC, Good Order.

#### Choose an eligible leader to accompany the rout

ID: `leader`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.711; A17.2. **PDF pages:** 680, 69, 85. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is accompaniment worth the leader’s risk?

- Require a non-berserk, non-pinned leader already stacked with the broken unit before it routes. It must remain with that unit throughout RtPh.
- It adds applicable leadership to Interdiction NMC but makes no separate Interdiction NMC; if its accompanied unit fails that MC, the leader is eliminated.
- It remains unbroken, carries only its own prior SW, and cannot improve the broken unit’s IPC. Check its own wounded movement limit.

**Checklist behavior:** Preview both the leadership benefit and elimination risk before attaching it.

**Illustrative position:** An 8-1 accompanies a squad but shares the severe consequence of a failed Interdiction check.

**Terms:** Voluntary Rout, Interdiction, SW, Wound.

#### Move prisoners with a routing Guard

ID: `prisoners`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A20.5-.53. **PDF pages:** 680, 87. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this router have prisoners?

- Guarded prisoners accompany a Guard that routes; they do not independently rout or constrain enemy rout as armed units.
- Retain Guard identity/capacity and relevant restrictions. Do not add a free RtPh prisoner abandonment or transfer action.
- If a Guard is lost, resolve the specified reassignment or unguarded status rather than silently deleting the prisoners.

**Checklist behavior:** Treat this as an explicit accompaniment exception, not an ordinary multi-unit rout stack.

**Illustrative position:** A routing Guard brings its prisoners while retaining the required relationship.

**Terms:** Guard, Surrender, US#.

### 6.21B · Select destination and route

#### Designate the nearest eligible destination

ID: `destination`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51. **PDF pages:** 680, 66, 67. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which building or woods destination is required?

- At the start of this unit’s rout calculate nearest in MF, not hex distance, and designate a legal building/woods destination even if overstacked.
- Remove non-traversable paths and apply the option not to use known minefields/FFE. Resolve permitted ignored destinations explicitly.
- Do not force the exact shortest route if the destination can still be reached this RtPh; preserve enemy and terrain restrictions.

**Checklist behavior:** Show candidate costs, ties and reasons a nearer candidate may be ignored.

**Illustrative position:** A three-hex route can be nearer in MF than a two-hex uphill route.

**Terms:** MF, Known enemy, LOS.

#### Choose permitted destinations to ignore

ID: `ignore`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51 EXC. **PDF pages:** 680, 67, 69, 137. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** May this building or woods hex be ignored?

- A unit in a building may ignore its starting building under the rule, including when a destination is refigured; rowhouses use their special building identity.
- A building/woods hex no farther from a Known enemy than the starting hex may also be ignored, even when the route passes through it.
- Record the player’s lawful choice and reason. Do not silently remove all dangerous-looking destinations.

**Checklist behavior:** Make optional exclusions visible before route selection.

**Illustrative position:** The router may leave its own building rather than automatically going upstairs.

**Terms:** Known enemy, MF, RtPh.

#### Choose a legal route when no required cover is reachable

ID: `noCover`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51. **PDF pages:** 680, 67. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is any non-ignorable building/woods reachable this RtPh?

- If none is reachable, the unit may rout to any terrain consistent with the remaining restrictions; it need not head toward the nearest unreachable cover.
- Do not confuse this exception with Low Crawl’s destination-direction rules or with insufficient MF caused by an arbitrary detour.
- Surrender and end-position tests still apply.

**Checklist behavior:** Explain why the destination search ended without a compulsory cover hex.

**Illustrative position:** All qualifying cover is beyond this unit’s legal budget, so other legal destinations become choices.

**Terms:** MF, Low Crawl, Known enemy.

#### Apply enemy-distance and ADJACENCY restrictions per step

ID: `direction`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51; D6.1. **PDF pages:** 680, 66, 67, 205. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does the next step violate a particular enemy restriction?

- Do not decrease range to a Known armed enemy while in its LOS, or after leaving that LOS during this rout, even if that enemy is broken or Disrupted; preserve the Passenger exception.
- Do not move ADJACENT to a Known enemy except when leaving its Location. If already ADJACENT to an armed Known enemy, do not remain ADJACENT to that same enemy by routing.
- Test exact Locations, vertical relationships and source exceptions instead of a generic circle of forbidden hexes.

**Checklist behavior:** Identify the lawful, viewer-visible reason for each restriction.

**Illustrative position:** A broken enemy can constrain direction although it cannot Interdict.

**Terms:** Known enemy, ADJACENT, LOS.

#### Compare Low Crawl with an ordinary rout

ID: `lowCrawl`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.52; A20.21; E1.54. **PDF pages:** 680, 67, 87, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is one Location without Interdiction a legal alternative?

- Ordinarily Low Crawl spends the whole allowance for one Location and still obeys destination direction and enemy restrictions.
- It cannot ordinarily enter marsh, a Water Obstacle or a non-dry stream, or leave an enemy-occupied Location. Apply night exceptions explicitly.
- It prevents Interdiction, not FFE or mines, and may still require surrender under A20.21. Do not present it as a universal escape.

**Checklist behavior:** Show avoided Interdiction separately from hazards and surrender eligibility.

**Illustrative position:** A path offering only Low Crawl can still lead to compulsory daytime surrender to an eligible adjacent enemy.

**Terms:** Low Crawl, Interdiction, MF, Surrender.

#### Refigure after a newly Known enemy blocks the destination

ID: `refigure`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51; A10.533. **PDF pages:** 680, 66, 67, 68. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Has new lawful information prevented the designated route?

- Recalculate the destination from the current point when a newly Known enemy prevents the original one.
- Preserve MF expenditure and remembered enemy constraints; do not restart the rout.
- Resolve the event that changed knowledge before accepting the next route step.

**Checklist behavior:** Invalidate only affected choices and show why a new destination is required.

**Illustrative position:** An enemy revealed around a corner changes the remaining route, not the allowance already spent.

**Terms:** Known enemy, MF, LOS.

#### Check whether the rout must stop or can continue

ID: `stop`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.51. **PDF pages:** 680, 66, 67. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Has the unit reached a valid stopping Location?

- On reaching building/woods not ADJACENT to a Known enemy, normally stop, even if overstacked.
- Use the explicit continuation permission when the next entered hex is another building/woods Location, and preserve ignored-destination rules.
- Complete endpoint, surrender and Failure-to-Rout checks before ending this unit’s rout. Ending one rout is not ending the side’s RtPh.

**Checklist behavior:** Show “end this rout” separately from remaining unit obligations.

**Illustrative position:** A unit reaching woods cannot continue through an open gap merely because it has spare MF.

**Terms:** RtPh, ADJACENT, MF.

### 6.21B · Interdiction and enemy responses

#### Determine Open Ground relative to each potential Interdictor

ID: `openGround`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A10.531; B1.13-.17. **PDF pages:** 680, 67, 113, 146, 147. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Could this enemy apply hypothetical FFMO here?

- Use the actual protection, elevation, hexside, SMOKE and Hindrance relationship, not the base terrain name.
- Shellhole/fortification protection depends on the announced entry expenditure. Make that choice before Interdiction is claimed.
- No actual Defensive First Fire occurs in RtPh. A hypothetical fire test does not open a shot window.

**Checklist behavior:** Show which protection prevents Interdiction for each evaluated enemy without revealing hidden units.

**Illustrative position:** A bare hill hex may still be protected from a lower enemy by Height Advantage.

**Terms:** Open Ground, TEM, LOS, SMOKE.

#### Check who can Interdict

ID: `interdictor`. **Who:** Enemy side. **Type:** Eligibility review.

**Timing:** ASOP 6.21B. **Rules:** A10.532; A20.52. **PDF pages:** 680, 67, 68, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is this enemy capable of the required hypothetical fire?

- Require unbroken status, at least one FP, no applicable Hindrance and range within both Normal Range and 16 hexes.
- Check functioning weapon, CA and positive penalties, Pin, CX, Encirclement, Melee, Spotted Fire and halved FP with the mortar exception. A leader without SW cannot Interdict.
- Vehicle BU/Stun/CA and Guard restrictions matter. Previously exhausting actual fire does not by itself prevent Interdiction.

**Checklist behavior:** Keep fire capability for Interdiction distinct from remaining actual shots.

**Illustrative position:** A spent but otherwise eligible squad can Interdict; a CX squad cannot.

**Terms:** Interdiction, FP, CA, BU.

#### Resolve the one applicable Interdiction NMC

ID: `interdiction`. **Who:** BOTH. **Type:** Required result.

**Timing:** ASOP 6.21B. **Rules:** A10.53; A10.3; A17.11. **PDF pages:** 680, 66, 67, 69, 85. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this entered hex require a check?

- For a qualifying non-Low-Crawl route, resolve one Interdiction NMC per Open Ground hex entered, not one per enemy claiming it.
- Failure causes Casualty Reduction; a surviving HS continues as allowed. An original 12 for a broken unit eliminates it. Apply SMC wound severity and leader accompaniment consequences.
- A pin stops further rout; check remaining enemy ADJACENCY and Failure to Rout. Do not apply the check to stationary co-occupants.

**Checklist behavior:** Resolve the result and revised subject before offering another step.

**Illustrative position:** Several eligible enemies still cause only one Interdiction check in that entered hex.

**Terms:** Interdiction, NMC, Wound, MF.

#### Consider voluntary concealment loss during a rout

ID: `reveal`. **Who:** Enemy side. **Type:** Optional response.

**Timing:** ASOP 6.21B. **Rules:** A10.533; A12.14. **PDF pages:** 680, 68, 76. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should a concealed enemy become Known at this window?

- An eligible concealment-loss choice can establish Interdiction or force a route change. Preserve its exact timing.
- Do not retrospectively eliminate a unit based on information revealed after the completed route’s legal window; apply the next-unit/phase-close timing in A10.533.
- Keep the enemy decision private until its lawful disclosure.

**Checklist behavior:** Provide a distinct opposing-player response rather than revealing enemies automatically to simplify routing.

**Illustrative position:** Revealing before the next rout begins can require a still-exposed unit to continue.

**Terms:** HIP, Known enemy, Interdiction.

#### Resolve entry into a concealed enemy Location

ID: `repulse`. **Who:** BOTH. **Type:** Required encounter.

**Timing:** ASOP 6.21B. **Rules:** A10.533; A12.15. **PDF pages:** 680, 68, 79, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Did the attempted entry meet an unrevealed non-Dummy unit?

- Resolve required reveal by Random Selection and repulse to the last occupied hex. Do not pre-reject the route with hidden knowledge.
- The unit ends its rout there and the prescribed surrender/Failure-to-Rout consequences apply, with relevant exceptions.
- This forced reveal is distinct from voluntary surrender acceptance and does not itself invoke No Quarter.

**Checklist behavior:** Show the actual reveal, repulse and adjudicated outcome as one event chain.

**Illustrative position:** A seemingly valid destination can reveal an enemy only when entry triggers the rule.

**Terms:** Known enemy, HIP, Failure to Rout.

### 6.21B · Surrender and capture consequences

#### Determine compulsory ordinary RtPh surrender

ID: `surrender`. **Who:** BOTH. **Type:** Conditional obligation.

**Timing:** ASOP 6.21B. **Rules:** A20.21; A10.53. **PDF pages:** 680, 86, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is there a qualifying adjacent captor and no qualifying escape?

- Check Known Good Order armed enemy Infantry/Cavalry ADJACENT to the broken Infantry.
- If unable to rout away, or able only by Interdiction exposure or Low Crawl, surrender may be mandatory regardless of the chosen route or whether a possible unconcealed Interdictor is Known.
- Apply Pin and other explicit exceptions. Disrupted, Encircled and HOB-surrender cases have additional rules even with a safe route.

**Checklist behavior:** Evaluate surrender before presenting a false choice of safe Low Crawl.

**Illustrative position:** A broken squad cannot choose Low Crawl merely to evade an otherwise mandatory surrender.

**Terms:** Surrender, Low Crawl, Interdiction, ADJACENT.

#### Apply surrender exemptions and ineligible captors

ID: `exempt`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** A20.21; A10.8; E1.54. **PDF pages:** 680, 69, 87, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this unit or enemy profile change capture?

- Partisans, Gurkhas, Commissars, SS facing Russians, Fanatics, Japanese and units faced with No Quarter do not surrender by this method.
- If only adjacent armed enemies are vehicles, berserk or in Melee, route away as required, even if Disrupted, or apply Failure to Rout; that elimination does not invoke No Quarter.
- At night E1.54 replaces ordinary RtPh surrender. Do not turn a source-profile gap into an assumed exemption.

**Checklist behavior:** Explain exactly which exemption or captor condition applies.

**Illustrative position:** A vehicle can prevent a legal stay without being a recipient of RtPh surrender.

**Terms:** Surrender, No Quarter, Disrupted.

#### Choose the captor and accept or reject the surrender

ID: `accept`. **Who:** Captor. **Type:** Required capture decision.

**Timing:** ASOP 6.21B. **Rules:** A20.21; A20.3. **PDF pages:** 680, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which eligible unit receives this surrender, and is it accepted?

- The captor chooses among qualifying receiving units. A stack surrenders simultaneously and must be accepted or rejected as a stack.
- Rejection at capture eliminates the surrendering subjects and invokes the specified side’s No Quarter consequences; the opposite side’s acceptance ability is unchanged.
- Do not silently choose rejection or process a group as independent accept/reject choices.

**Checklist behavior:** Switch decision ownership to the captor with explicit consequences.

**Illustrative position:** The routing player does not choose the enemy Guard or rejection policy.

**Terms:** Surrender, Guard, No Quarter.

#### Assign Guards, abandon SW and resolve excess prisoners

ID: `capture`. **Who:** Captor and routing side. **Type:** Required capture results.

**Timing:** ASOP 6.21B. **Rules:** A20.21; A20.24; A20.5-.53. **PDF pages:** 680, 50, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What changes when surrender is accepted?

- Abandon SW first, with any eligible separate Recovery response. The captor does not automatically possess the equipment.
- Assign unarmed identity and Guard capacity. Excess prisoners are freed as Unarmed as prescribed; preserve crew/SMC records and victory accounting.
- Surrendering units are not subjected to FFE or minefield attacks during the transfer. Do not run an ordinary rout hazard pipeline on capture movement.

**Checklist behavior:** Apply capture as a distinct state transition with equipment and Guard consequences.

**Illustrative position:** An accepted stack can exceed available guarding capacity without becoming extra eliminated units.

**Terms:** Guard, SW, US#, Surrender.

#### Resolve applicable Interrogation on later capture

ID: `interrogation`. **Who:** Captor and prisoner owner. **Type:** Conditional result chain.

**Timing:** ASOP 6.21B. **Rules:** E2.1-.3. **PDF pages:** 680, 228. **Coverage:** Rules integration pending; reference available.

**Consider:** Did a capture during routing create an E2 obligation?

- If the optional Interrogation rules apply, roll immediately for each captured Personnel unit, once per prisoner.
- Use the same modifier, range/LOS, substitute-result and disclosure rules as for an opening capture.
- Do not omit this because the opening surrender queue has already been completed.

**Checklist behavior:** Attach Interrogation to the capture event wherever it occurs.

**Illustrative position:** A later surrender produces the same conditional interrogation procedure.

**Terms:** Interrogation, HIP, Guard.

#### Resolve Failure to Rout at the proper point

ID: `failure`. **Who:** BOTH. **Type:** Conditional required result.

**Timing:** ASOP 6.21B. **Rules:** A10.5; A10.51-.53; A20.21; E1.54. **PDF pages:** 680, 66, 67, 68, 87, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this unit complete its required legal departure?

- Check the actual route/endpoint and any pin or repulse consequence. Do not eliminate a safe optional non-router merely for having no attractive route.
- Apply surrender before substituting elimination where required, and preserve unit/nationality and night exceptions.
- Distinguish Failure to Rout from casualty elimination, rejected surrender and the separate terrain-Blaze deadline.

**Checklist behavior:** Show the unresolved requirement and exact governing outcome.

**Illustrative position:** A unit pinned adjacent during Interdiction may have a different immediate outcome from a safe unit that simply stops.

**Terms:** Failure to Rout, Surrender, ADJACENT.

### 6.21B · Hazards and route-linked effects

#### Resolve minefield entry and exit attacks

ID: `mines`. **Who:** BOTH. **Type:** Required hazard resolution.

**Timing:** ASOP 6.21B. **Rules:** B28.41-.413. **PDF pages:** 680, 148. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this step enter or leave a minefield?

- Resolve the appropriate attack against only the routing subjects. On exit, resolve in the original hex before entry into the next one.
- A failed exit can prevent the next entry and its effects. A reduced surviving HS may continue as permitted.
- Mines resolve before applicable Interdiction. Low Crawl does not provide general mine immunity; known A-P minefields can be avoided under the route rules.

**Checklist behavior:** Show exit and entry as separate hazard events with their actual Locations.

**Illustrative position:** Crossing between two mined hexes may cause two attacks, but only if the exit succeeds.

**Terms:** MF, MC, Interdiction.

#### Resolve existing FFE against routing subjects

ID: `ffe`. **Who:** BOTH. **Type:** Required hazard resolution.

**Timing:** ASOP 6.21B. **Rules:** C1.51-.54; A10.51-.52. **PDF pages:** 680, 67, 165, 166. **Coverage:** Rules integration pending; reference available.

**Consider:** Has this subject entered a Blast Area or become more vulnerable?

- Existing HE/WP FFE can attack qualifying entry or increased vulnerability, including within a hex. Resolve only the routing unit/stack, not stationary occupants or terrain.
- Use current mission profile, protection, critical-hit and friendly-FFE effects. C1.51 limits FFMO/FFNAM to MPh, so do not apply them to RtPh attacks.
- FFE does not Interdict. Low Crawl is not FFE immunity, and rout does not advance the mission’s phase counter or allow new Contact/Access.

**Checklist behavior:** Keep attack triggers separate from both Interdiction and normal artillery-phase resolution.

**Illustrative position:** A router entering an FFE is attacked while a stationary unit in the same hex is not attacked by that entry event.

**Terms:** FFE, WP, MC, Low Crawl.

#### Use the actual SMOKE, rocket, Barrage or NOBA profile

ID: `specialFfe`. **Who:** BOTH. **Type:** Conditional hazard profile.

**Timing:** ASOP 6.21B. **Rules:** C1.71-.72; C1.9; E12.5; G14.65-.661. **PDF pages:** 680, 166, 167, 247, 317, 318. **Coverage:** Rules integration pending; reference available.

**Consider:** Which existing mission rules affect this route?

- Apply WP/SMOKE, rocket, Barrage and naval profile differences rather than treating every overlay as generic HE.
- Retain the profile’s blast area, immunity, Reverse Slope and other target conditions. Ordinary Smoke affects movement/LOS; it does not become a generic damage attack.
- No new mission adjustment or terrain-wide damage is created by the routing event.

**Checklist behavior:** Keep all ASOP-named mission families visible even without an integrated evaluator.

**Illustrative position:** A route across a naval Blast Area uses its specific protection rules.

**Terms:** FFE, WP, SSR.

#### Resolve Panji entry and movement beneath it

ID: `panji`. **Who:** BOTH. **Type:** Required hazard resolution.

**Timing:** ASOP 6.21B. **Rules:** G9.31; G9.41-.45; G9.5-.6. **PDF pages:** 680, 289, 290. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the route cross a Covered hexside?

- Apply the Panji MC and listed exceptions, including Low Crawl and qualifying trench/subterranean entry. Resolve it before Interdiction.
- Preserve above/below status, extra MF to move beneath and exit restrictions. A broken unit may avoid a Covered hexside placing it above when an alternate legal path exists.
- Do not replace Panji MC with Interdiction or assume a passed MC automatically places the unit beneath the obstacle.

**Checklist behavior:** Show hazard and position as separate consequences.

**Illustrative position:** A survivor above the Panji counter may need another expenditure before it can leave.

**Terms:** Panji, Low Crawl, MC, Interdiction.

#### Check trip flare activation and duration

ID: `flare`. **Who:** Flare owner and routing side. **Type:** Conditional required trigger.

**Timing:** ASOP 6.21B. **Rules:** E1.95-.953. **PDF pages:** 680, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this entry or expenditure trigger a hidden trip flare?

- Check all qualifying ground-unit/stack entries and expenditures, including friendly units, with passage and action exceptions.
- Resolve the activation dr, depletion and illumination at the triggering event. Do not reveal untriggered hidden flare placement in a route preview.
- A RtPh activation uses the purple-on-white side and lasts until the next Player Turn’s AFPh cleanup.

**Checklist behavior:** Schedule illumination and its future removal separately.

**Illustrative position:** A flare triggered now does not disappear at the end of the current CCPh.

**Terms:** Trip Flare, NVR, MF.

#### Check Column Disbandment as routing proceeds

ID: `column`. **Who:** BOTH. **Type:** Conditional formation result.

**Timing:** ASOP 6.21B. **Rules:** E11.53-.536. **PDF pages:** 680, 246. **Coverage:** Rules integration pending; reference available.

**Consider:** Did this expenditure or attack trigger Disbandment?

- Outside MPh, check after attacks and as each routing unit expends MF, not only at phase end.
- Apply actual proximity, OBA, minefield/wire and other Column conditions. Do not infer Column status from a line of units.
- Resolve prescribed concealment loss, DEFENDER Pin and wagon Motion effects where applicable.

**Checklist behavior:** Attach the formation consequence to the triggering route event.

**Illustrative position:** A nearby rout expenditure can force a Column review before the next step.

**Terms:** Column, MF, FFE.

### 6.21B · Special rout procedures

#### Use the night Rout procedure

ID: `night`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** E1.54. **PDF pages:** 680, 224, 143, 203. **Coverage:** Rules integration pending; reference available.

**Consider:** Is E1.54 governing this unit?

- Broken units use Low Crawl at night and are not eliminated for Failure to Rout. Ordinary RtPh capture/surrender is unavailable, even for Disrupted units.
- Night Low Crawl is always available to broken units, including the listed enemy-Location, marsh, fording and tunnel exceptions; no specific terrain destination is required, but Known-enemy direction restrictions remain.
- Broken Inherent Crews still spend their entire RtPh exiting the vehicle. Do not let night’s Failure-to-Rout exemption erase the separate Blaze deadline.

**Checklist behavior:** Switch the explanation to the night profile instead of accumulating misleading daytime warnings.

**Illustrative position:** A Disrupted unit at night can have a Low Crawl choice that is unavailable in ordinary daylight.

**Terms:** NVR, Low Crawl, DM, Failure to Rout.

#### Choose ski mode and use its explicit rout rules

ID: `skis`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** E4.2; E4.31-.33. **PDF pages:** 680, 231, 232. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this unit don or remove skis?

- Eligible routing units may change mode: removing skis has no MF cost; donning normally costs two MF, one for Finnish Infantry. Apply Pin/TI restrictions.
- Skiers must first remove skis to Low Crawl. Use the correct snow/terrain movement profile.
- The downhill two-MF-per-Crest-Line bonus can exceed normal broken/wounded limits under E4.31; do not suppress this explicit exception with a universal six-MF cap.

**Checklist behavior:** Show mode, change cost and downhill allowance separately.

**Illustrative position:** A Finnish router can have different preparation cost and a specific downhill bonus.

**Terms:** Skis, MF, Low Crawl.

#### Consider routing onto an evacuation LC or amphibian

ID: `evacuation`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** G14.41-.42. **PDF pages:** 680, 315. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this evacuation profile permit a loading destination?

- Only the Evacuating side in a Seaborne Evacuation uses this special building-equivalent destination rule.
- Ignore Immobile/burning craft and those lacking required capacity. A unit loading this RtPh has only four MF and pays normal applicable loading cost.
- Apply current craft status, loading and exit-value rules; this is not general permission to board any transport while routing.

**Checklist behavior:** Show qualifying craft as destination candidates with capacity and total loading cost.

**Illustrative position:** The nearest LC is ignored because it lacks room; a farther eligible craft may be considered.

**Terms:** Seaborne Evacuation, LC, PP, MF.

#### Resolve a broken Inherent Crew’s initial rout out

ID: `crewExit`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** D5.311; E1.54. **PDF pages:** 680, 203, 224. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Must this crew leave its vehicle?

- Where D5.311 causes a broken Inherent Crew, it spends all initial-RtPh MF being placed beneath its vehicle.
- Apply the vehicle’s Stop/Abandoned and crew state consequences; do not grant a second ordinary six-MF rout after placing the crew.
- Retain this procedure at night and distinguish it from Stun, Shock, Bail Out or an ordinary optional Abandon action.

**Checklist behavior:** Represent the crew exit as its own complete initial-rout procedure.

**Illustrative position:** A newly broken Inherent Crew is placed beneath the vehicle rather than routed six additional MF.

**Terms:** MF, RtPh, BU.

#### Apply newly landed paratroop Rout immunity

ID: `paratroops`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** ASOP 6.21B. **Rules:** E9.42; E9.5. **PDF pages:** 680, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Did these paratroops land this Player Turn?

- They may not rout and are not subject to Rout rules, including surrender and Failure to Rout, in their landing Player Turn.
- Broken DEFENDER occupants may rout from the landing hex when not yet in Melee. Do not extend the newcomer’s immunity to those defenders.

**Checklist behavior:** Explain the asymmetric landing exception and each unit’s separate status.

**Illustrative position:** The defender can need to leave a hex while the just-landed paratroops remain exempt.

**Terms:** Paratroops, RtPh, Surrender.

### 6.31B · End-of-phase Blaze deadline

#### Resolve Infantry that could not leave terrain Blaze

ID: `blazeEnd`. **Who:** BOTH. **Type:** Required phase-close result.

**Timing:** ASOP 6.31B. **Rules:** B25.4. **PDF pages:** 680, 143, 224. **Coverage:** Rules integration pending; reference available.

**Consider:** Has each affected Infantry unit escaped before the deadline?

- Eliminate Infantry unable to leave terrain Blaze by the end of this RtPh, including applicable pillbox occupants.
- This is separate from Failure to Rout and survives night’s exemption. Melee/pinned units cannot use the special RtPh exit and are eliminated if trapped.
- Vehicles/Cavalry have their next friendly MPh deadline, not a generic Infantry RtPh deadline.

**Checklist behavior:** Close each carried AFPh Blaze obligation only with actual escape or the prescribed outcome.

**Illustrative position:** An Infantry unit cannot survive terrain Blaze merely because night prevents Failure-to-Rout elimination.

**Terms:** Blaze, RtPh, Failure to Rout.

### Supplemental terrain, protection and status

#### Choose the applicable Blaze escape procedure

ID: `blazeEscape`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** B25.4; A10.41. **PDF pages:** 680, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** How can this occupant leave before the deadline?

- Eligible unbroken Infantry may voluntarily break to rout out. A non-pinned unit unable to break voluntarily can use the specified Accessible-Location exit as if withdrawing from Melee, even if berserk.
- Units actually in Melee or pinned cannot use that exit. The special movers remain vulnerable to Interdiction, mines and OBA, and ATTACKER still goes first.
- Do not eliminate the unit early merely because normal rout choices are absent; evaluate the explicit escape provision.

**Checklist behavior:** Keep exceptional unbroken escape separate from ordinary broken rout.

**Illustrative position:** A non-pinned berserk occupant may need the special Blaze exit path.

**Terms:** Blaze, Voluntary Break, Interdiction.

#### Choose shellhole or entrenchment protection on entry

ID: `shelter`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A10.51; A10.531; B27.41. **PDF pages:** 680, 66, 67, 113, 146, 147. **Coverage:** Rules integration pending; reference available.

**Consider:** Will the unit pay for shelter as it enters?

- Extra fortification entry/exit costs do not count in calculating the nearest building/woods destination.
- For the actual route, announce combined entry and shelter costs before Interdiction is claimed. A capacity-eligible foxhole can then prevent Interdiction.
- Use the special shortest-path/shelter option even when spending for protection prevents reaching cover this RtPh. It is not permission for arbitrary detours.

**Checklist behavior:** Compare exposed transit with paid protection, retaining the destination calculation separately.

**Illustrative position:** The router can pay into a foxhole en route instead of being forced to risk Interdiction to reach woods.

**Terms:** MF, Open Ground, Interdiction.

#### Apply pillbox and bunker rout exceptions

ID: `pillbox`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** B30.41-.8; B25.4. **PDF pages:** 680, 151, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this destination or starting Location a pillbox?

- Treat a pillbox as building-equivalent for rout with its special Known-enemy rule. A broken occupant is not ordinarily forced to rout merely by nearby enemies.
- Apply entry/exit restrictions from enemy ground units in the hex, and tunnel/bunker exceptions. Use combined protection costs where permitted.
- Do not treat the pillbox as immunity to terrain Blaze. Its concealed contents also have special disclosure/Knowledge treatment.

**Checklist behavior:** Use the actual interior/exterior Location relation, not one hex-wide permission.

**Illustrative position:** An enemy outside can block ordinary exit while a valid tunnel route requires separate evaluation.

**Terms:** Pillbox, ADJACENT, Known enemy, Tunnel.

#### Consider a tunnel route

ID: `tunnel`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** B8.62; E1.54. **PDF pages:** 680, 118, 224. **Coverage:** Rules integration pending; reference available.

**Consider:** Can the unit complete this passage under its profile?

- Normally start at one entrance and end at the opposite entrance in the same RtPh; no lingering in the tunnel.
- It is immune to Interdiction inside and can pass toward/adjacent to enemies, but emergence must satisfy remembered-enemy distance and ADJACENCY restrictions.
- The unit is not concealed on emergence. Apply the explicit night exception rather than inventing ordinary Low Crawl permission.

**Checklist behavior:** Evaluate passage and emergence separately without displaying private tunnel connections to the opponent.

**Illustrative position:** A passage under an enemy is not necessarily a legal exit beside that enemy.

**Terms:** Tunnel, Interdiction, Known enemy.

#### Pay Wire costs and check escape

ID: `wire`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** B26.4-.41. **PDF pages:** 680, 145. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this router get beneath Wire and reach a legal endpoint?

- Use the Wire exit dr and appropriate rout movement costs; surrender has its stated exception.
- A failed escape can cause Failure to Rout or surrender under the actual enemy/exposure conditions.
- Do not bypass Wire costs or move the unit beneath automatically when the route is chosen.

**Checklist behavior:** Include the unresolved die-dependent cost in the route explanation.

**Illustrative position:** A unit can have a destination yet be unable to reach it after its Wire roll.

**Terms:** MF, Surrender, Failure to Rout.

#### Leave broken Crest status

ID: `crest`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** B20.96; A10.5. **PDF pages:** 680, 133, 165. **Coverage:** Rules integration pending; reference available.

**Consider:** Does broken Crest status require departure from that position?

- Broken Crest Infantry must leave Crest status in the following RtPh.
- Apply actual costs and routing restrictions; leaving Crest is not necessarily the same as changing hex.
- Reevaluate protection and any FFE increased-vulnerability trigger at the actual change.

**Checklist behavior:** Show compulsory position change independently from ordinary destination selection.

**Illustrative position:** A unit can owe a Crest departure even though the map hex has not changed.

**Terms:** MF, RtPh, TEM.

#### Apply vertical, rowhouse, water and special-terrain routes

ID: `terrain`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A10.51-.52; B16.4; B21.43; B23.71; G8; G11. **PDF pages:** 680, 66, 67, 130, 135, 137, 287, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this route use a special Location or movement rule?

- Check stairs, accessible levels, rowhouse passage and entry costs; ordinary broken units cannot use Bypass. Do not confuse rowhouse passage with ordinary woods/building Bypass.
- Broken units in a Water Obstacle must rout even if Disrupted, and ordinary Low Crawl is barred there. Marsh consumes the stated allowance; night can change Low Crawl permission.
- Keep PTO paddy-bank, jungle/bamboo, cave and other profile rules visible with exact evaluation pending when unsupported.

**Checklist behavior:** Avoid ground-level-only or terrain-name-only route assumptions.

**Illustrative position:** An upstairs Location may remove ADJACENCY while an adjacent same-level hex does not.

**Terms:** MF, ADJACENT, Low Crawl, SSR.

#### Apply Seaborne Assault and Evacuation status overlays

ID: `seaborneStatus`. **Who:** Routing side. **Type:** Routing choice.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** G14.23; G14.32; G14.41. **PDF pages:** 680, 314, 315. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this subject have beach or required-debarkation rules?

- Apply Assaulting/Evacuating-side Fanatic and Beach/pier/wading consequences, including their specified break-result exceptions.
- Required ASAP debarkation has a broken-at-start-of-RtPh provision; do not wait automatically for the next MPh.
- Keep these rules distinct from the Evacuation-only permission to rout onto a craft.

**Checklist behavior:** Project profile-specific obligations and morale effects before computing the route.

**Illustrative position:** A beach-unit status rule can change results without granting a new ordinary transport choice.

**Terms:** Seaborne Evacuation, LC, MF.

#### Resolve casualty, wound and morale consequences before continuing

ID: `results`. **Who:** BOTH. **Type:** Required consequence chain.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A10.3; A10.53; A17; A15; A14. **PDF pages:** 680, 66, 67, 69, 82, 84, 85. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What changed in the routing subject?

- Complete casualty reduction, wound severity, relevant leader effects and HOB as required by the actual result, then recalculate remaining capability.
- Track MF already spent if wounded or pinned. Do not refresh the route allowance or transfer a destroyed unit’s remaining movement to a replacement without the rule.
- Ordinary Sniper activation is limited to its named fire phases, not RtPh. Do not import an AFPh SAN trigger simply because a rout MC rolled that number.

**Checklist behavior:** Link every follow-up to its original hazard and current subject.

**Illustrative position:** A surviving HS may continue, while a wounded/pinned SMC needs a different continuation check.

**Terms:** MC, Wound, HOB, MF.

#### Finish each side’s routs and hand off to Advance

ID: `finish`. **Who:** BOTH. **Type:** Phase handoff.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A3.6-.7; A10.5; B25.4. **PDF pages:** 680, 47, 66, 68, 143, 227. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Are both sides’ required routs and effects complete?

- Finish ATTACKER obligations before DEFENDER routs, while preserving opposing capture/reveal decisions at their actual windows.
- Resolve remaining route endpoints and the Blaze deadline. Optional considered topics cannot clear those obligations.
- Do not remove DM, PIN, TI or trip flares through generic RtPh cleanup; their own removal windows remain in force.

**Checklist behavior:** Expose separate end-unit, end-side and end-phase states in the eventual live component.

**Illustrative position:** Completing the last attacker rout does not skip the defender’s units.

**Terms:** RtPh, APh, DM.

#### Keep other-phase actions out of Rout

ID: `boundaries`. **Who:** BOTH. **Type:** Boundary explanation.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** A10.4-.53; A10.711; E1.54; A20.5. **PDF pages:** 680, 50, 66, 67, 69, 224, 232, 315. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this a legal RtPh choice or another phase’s action?

- No Rally attempts, ordinary movement bonuses, shooting, DFF/SFF/FPF, Search or unrestricted equipment handling are added here.
- Interdiction is not a shot, and leader accompaniment is not a generic unbroken-unit movement action.
- Keep explicit exceptions such as triggered SMC Recovery, ski mode, evacuation loading and Blaze escape discoverable.

**Checklist behavior:** Explain the proper window and exception instead of an unexplained disabled action.

**Illustrative position:** A leader may accompany a legal rout but cannot freely move elsewhere during RtPh.

**Terms:** RtPh, MPh, RPh, APh.

#### Preserve pending rules and source completeness

ID: `sourceProfile`. **Who:** BOTH. **Type:** Coverage boundary.

**Timing:** Rule-specific RtPh timing; supplemental to ASOP. **Rules:** ASOP 6; SSR; Chapters F/G/H/W. **PDF pages:** 680, 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Are scenario sources and evaluators available?

- All printed RtPh ASOP clauses and identified in-volume families are represented. This is not proof of every module/SSR/equipment permutation.
- Pending integration, missing source and known illegality are different states. Do not hide reference options because the current pass has not implemented them.
- Live route candidates, counts and reasons must be viewer-safe; the browser must not receive secret enemy data merely to filter it visually.

**Checklist behavior:** Show unknown applicability explicitly and retain the source-backed catalog.

**Illustrative position:** A deferred cave route stays discoverable without claiming it has been evaluated.

**Terms:** SSR, Source gap.

## Contextual vocabulary

| Label | Meaning |
|---|---|
| RtPh | Rout Phase. ATTACKER completes its routs first, then DEFENDER; ordinary routs proceed one unit at a time with explicit accompaniment exceptions. |
| DM | Desperation Morale. It can permit a broken unit to rout without itself requiring that unit to rout. Do not remove DM at RtPh end. |
| Surrender | Rule-required transfer to a qualifying captor. The router does not freely substitute Low Crawl when A20.21 requires surrender. |
| Interdiction | A10.53 NMC when an eligible routing unit enters qualifying Open Ground exposed to a capable enemy. Not an actual fire attack or expenditure of remaining shots. |
| Known enemy | Enemy Known to the particular routing unit under the rules. Preserve that unit’s previously seen armed enemies during its rout; do not substitute omniscient map knowledge. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| ADJACENT | Locations satisfying the rules’ LOS and Infantry-advance relationship. It is more specific than neighboring hexes, including vertical relationships. |
| Voluntary Break | A10.41 start-of-RtPh decision, permitted only with the stated enemy exposure and no immediate Reduction/elimination, plus specific exceptions such as Blaze escape. |
| Open Ground | For rout and Interdiction, the hypothetical FFMO test relative to a particular enemy, not simply the printed terrain label. |
| Blaze | Terrain fire with an end-of-RtPh Infantry escape deadline. Night immunity to Failure to Rout does not cancel this separate elimination rule. |
| Disrupted | A19.12 status with its own opening surrender and restricted rout rules. Ordinary daytime Low Crawl is unavailable; night and No Quarter require explicit treatment. |
| No Quarter | A20.3 side-specific consequence of rejecting surrender or other specified events. It changes future surrender handling, not the legality of rout paths. |
| Interrogation | E2 optional-rule procedure, when in use, immediately after capture or surrender. One roll per prisoner, with controlled disclosure. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| Failure to Rout | Elimination under the applicable routing conditions after checking surrender and exceptions. Distinct from capture and from Blaze elimination. |
| MF | Movement Factors. Ordinary broken routers have six, wounded SMC three; explicit ski and evacuation rules can alter the calculation. |
| Low Crawl | Normally one Location using the entire rout allotment, avoiding Interdiction but not automatically avoiding surrender, mines or FFE. Night has major exceptions. |
| Wound | A17 injury with severity, reduced morale/leadership, IPC and movement consequences. Track MF already spent if it occurs mid-rout. |
| Skis | E4 equipment whose mode changes and downhill bonus have explicit RtPh permission. Low Crawl requires removing skis. |
| Seaborne Evacuation | G14.4 scenario profile allowing eligible Infantry to rout onto qualifying LC/amphibians as buildings, with a four-MF loading budget. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| IPC | Inherent Portage Capacity. A broken router must choose the prescribed load before routing, with no mid-route Recovery or Abandonment. |
| PP | Portage Points, used to evaluate the required legal load and transport capacity. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| Good Order | The defined unit status, separate from merely unbroken. Apply the exact status requirement of each action. |
| Voluntary Rout | A10.711 accompaniment by an eligible unbroken leader already stacked with the broken unit. It is not unrestricted movement by unbroken units. |
| Guard | Unit assigned to prisoners. Capacity, fire restrictions and accompaniment are separate from the act of accepting surrender. |
| US# | Unit Size Number. Used for purposes including prisoner guarding capacity and the dr to Set a DC; it is not a weapon Usage Number. |
| TEM | Terrain Effects Modifier. |
| SMOKE | The umbrella for Smoke and WP effects; preserve ammunition type, owner and source. |
| FP | Firepower. |
| CA | Covered Arc. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| NMC | Normal Morale Check. |
| MC | Morale Check. |
| FFE | Fire For Effect. Existing HE/WP FFE can attack the routing subject on qualifying entry or increased vulnerability; it does not itself Interdict. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| Panji | G9 obstacle with Covered hexsides and above/below states. Its entry MC and movement-under procedure are distinct from Interdiction. |
| Trip Flare | E1.95 hidden device with entry/expenditure triggers. A flare set off in RtPh persists until the next Player Turn’s AFPh cleanup. |
| NVR | Night Visibility Range. Night Rout also has specific E1.54 rules beyond visibility changes. |
| Column | E11 formation whose Disbandment is checked as each routing unit expends MF and after attacks; it is not recreated from ordinary counter alignment. |
| LC | Landing Craft, governed by its own transport, fire, beaching and grounding rules. |
| Paratroops | E9 landing troops. Unlike ordinary glider contents they may not attack during the Player Turn they land. |
| Pillbox | Separate Location equivalent to a building for rout, with special Known-enemy, entry/exit, ordinary forced-rout and Blaze rules. |
| Tunnel | B8.62 special rout connection between entrances, with emergence conditions and immunity to Interdiction while inside. |
| HOB | Heat of Battle, a triggered resolution rather than an optional action. |
| APh | Advance Phase, following Rout. AFPh fire and the advance are different windows. |
| MPh | Movement Phase. THE MPh contains a separate start, during and end for each moving unit or stack. |
| RPh | Rally Phase. Each Player Turn begins with one; both sides have activities here. |
| Source gap | A missing or unreconciled scenario/module/equipment source. Separate this from integration pending and from known illegality. |

## Acceptance scenarios for the eventual live component

| Position | Required behavior |
|---|---|
| Safe broken unit with DM | Offer stay or legal optional rout, not compulsory departure. |
| Safe broken unit without DM | No ordinary voluntary rout; evaluate explicit night/special exceptions. |
| Armed leader without SW | Constrain routing where applicable, but do not Interdict alone. |
| Broken armed enemy | Preserve direction constraints without granting Interdiction. |
| Nearest cover measured differently in hexes and MF | Select by the governing MF calculation. |
| Starting building or equidistant-to-enemy destination | Offer the exact permitted exclusion as a player choice. |
| New enemy becomes Known during rout | Refigure from the current point without refreshing MF or losing earlier knowledge. |
| Only escape is Low Crawl beside an eligible captor | Evaluate compulsory surrender before allowing the escape. |
| Several Interdictors cover one entered Open Ground hex | One Interdiction NMC, with all appropriate eligibility checks. |
| Accompanying leader and failed Interdiction NMC | Resolve the leader’s stated elimination consequence, not an independent NMC. |
| Overweight SW load | Choose the prescribed legal combination before rout; no mid-route discard. |
| Foxhole along shortest route | Announce combined protection cost before Interdiction, without counting it in nearest-cover distance. |
| Exit from one minefield into another | Resolve exit first; failed exit can prevent entry and its attack. |
| FFE entry | Attack routing subjects only, with no RtPh FFMO/FFNAM or new terrain damage. |
| Surrender transfer across mines/FFE | Preserve its specific exemption rather than running normal rout hazard attacks. |
| Night Disrupted unit | Use E1.54’s Low Crawl and surrender/Failure-to-Rout exceptions. |
| Terrain Blaze at night | Preserve the separate end-RtPh elimination deadline. |
| Skiing downhill | Apply the explicit bonus rather than an unconditional six-MF cap. |
| Evacuation loading | Use a qualifying craft and the four-MF loading budget. |
| Trip flare set off in Rout | Schedule removal in the next Player Turn’s AFPh. |
| Routing MC equals enemy SAN | Do not invent an ordinary RtPh Sniper activation. |
| Hidden enemy differs between otherwise identical viewer positions | Do not leak that difference through route suggestions, counts or reasons. |
| Reference topic marked considered | No move, roll, capture or required result occurs. |

## Preview verification

The local browser check passed all 403 topics across six phases, including every contextual term interaction. It checked catalog identities, group membership, physical-page bounds, all four RtPh ledger mappings, timing navigation, independent consideration records and host-state restoration. Screenshots were inspected at 320px and 736px and in dark mode; no horizontal overflow or preview runtime errors were found. The final whitespace check passed. These checks validate the reference design and interactions, not ASL legality, pathfinding or game execution. No .NET code was changed.
