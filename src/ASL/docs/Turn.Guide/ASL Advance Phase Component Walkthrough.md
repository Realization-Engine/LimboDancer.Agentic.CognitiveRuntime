# ASL Advance Phase Component Walkthrough

Design reconciliation, 26 October 2026. Target: the completed ASL.Rules implementation after all passes. This is a reference component design, not a new Rules or Play implementation.

## Acceptance cases for the eventual live component

These are design requirements, not claims that the current Rules engine passes them.

| Position or decision | Required behavior |
|---|---|
| Squad fired in an earlier phase but remains eligible | Allow ordinary advance; do not invent a fire-based prohibition. |
| PIN, TI, CC, berserk or actual Melee restriction | Explain the specific bar; do not remove the marker early. |
| Start-of-phase SW transfer followed by advance | Recalculate both loads before committing; late transfer cannot be inserted retroactively. |
| Unattended SW on the floor | Do not treat possession transfer as ordinary Recovery. |
| Load leaves zero MF | Reject advance regardless of inexpensive destination. |
| Same destination, different loads | Explain different difficult-terrain thresholds and CX results. |
| SMOKE is the only added cost | Exclude it from A4.72's comparison. |
| Encircled origin | Apply doubled first-Location entry cost after modifications. |
| Leader joins only at destination | Do not grant accompaniment bonuses retroactively. |
| Unit enters a new hex and wants a separate stair move | Reject the combined ordinary advance; preserve explicit split-level exceptions. |
| First squad passes PAATC, second has not rolled | Immediately advance the first squad; do not retain a provisional success. |
| PAATC fails | Pin the tested unit and preserve the resulting restriction. |
| Fanatic leaves a Beach/pier for an AFV in Hinterland | Evaluate the G14.32 exemption at check time. |
| Hidden fortified building bars an attempted entry | Reveal as required, consume the attempt and leave the unit in place without leaking this beforehand. |
| Exit mines pin the unit | Keep it in the origin; no destination attack or discovery occurs. |
| Existing FFE with stationary occupants | Attack only the triggering advancing/drifting subject under the applicable rules. |
| Normal versus difficult advance across Covered Panji | Apply the MC exemption only to normal advance; no extra-MF move beneath in APh. |
| Already-CX unit enters deep stream or finishes Climbing | Apply the specific exception rather than a blanket CX refusal. |
| Unit wants to enter marsh, ford water, or move beneath Wire | Explain the phase restriction and any named bridge/exit/connection exception. |
| Bicycle treated as one-PP equipment | Still bar moving it in APh. |
| Tunnel occupant would prefer to stay | Require emergence or the prescribed consequence; ordinary optional advance does not apply. |
| Sewer emergence previously disallowed | Do not reroll or permit surface emergence in APh. |
| Touching Cave Complexes | Do not equate Accessible with an allowed APh connection. |
| Night No Move unit | Require Freedom of Movement; no invented APh leader freedom roll. |
| Moderate versus Heavy Current | Owner-only versus both-player APh drift, with proper subject and pier exceptions. |
| Heavy-Surf Ocean drift | Use individual shore-directed displacement, exceptions, Swamping and entry checks. |
| DEFENDER swimmer drifts | Do not add a friendly-APh Drowning check in the wrong player's phase. |
| Swimmer remains in non-fordable water without moving | Retain its required friendly-APh survival check. |
| Newly dropped parachute and ordinary glider squad | Replace the parachute without advance; separately evaluate eligible glider Infantry. |
| Player marks every reference topic considered | Perform no game actions and clear no mandatory live obligations. |
| Missing terrain evaluator | Show pending integration, not a false legal or illegal verdict. |
| Opponent views candidate routes | Do not disclose hidden state through labels, counts, reasons or client-side data. |

## Verification of this reference preview

Validated all 460 topics across seven phases: topic selection, every contextual definition interaction, source-page bounds, unique IDs, group references and populated ASOP ledgers. Verified independent consideration state, host-state restoration, timing navigation and no browser runtime errors. Inspected the 320-pixel and 736-pixel layouts and dark appearance. Compared the nine-step ledger to the rendered original ASOP page 681. Additional final content checks cover Encirclement, bicycle portage, seaborne PAATC and Ocean Drowning refinements. `git diff --check` passes. No .NET behavior or game-rule execution is claimed by these checks.

## Source and coverage

Primary source: eASLRB v3.01, local file `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`. SHA-256: `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. Page numbers below are physical one-based PDF pages. ASOP section 7 is on page 681; A4.7-.72 is on page 52; A11.6 is on page 75.

The printed ASOP ledger accounts for every clause in all nine Advance steps. Detailed options also reconcile the identified rulebook families that refine those steps. This does not certify every historical module, SSR, Chapter H vehicle note or optional scenario combination. Those profiles need their own source ledger. An integration gap must never silently remove an option from the reference UI.

Current coverage is read against `ASL Rule Coverage.md`, including its Pass 70 and Pass 71 amendments. Core advance, PAATC, portage, concealment and CC foundations exist, but terrain and special-profile paths remain incomplete. Pass 71's wounded-man carrying work does not establish APh carrying: that remains backlog 74. The coverage report also records an A4.72 SMOKE-cost fault. The design explicitly excludes SMOKE from that calculation; it does not claim that fault is repaired here. Night, watercraft, parachute, cave, Column and detailed fortification paths retain pending labels.

## Component behavior

The phase opens with a side-owned checklist. Opening transfers precede creation and surf checks. Ordinary advance remains optional, while tunnel emergence, T-H Hero obligations, drift and survival remain required when applicable. DEFENDER Heavy Current drift receives a separate window. Each topic has a question, concrete eligibility checks, consequences, example, contextual glossary and physical source locators.

The proposed live component should show an **advance receipt** before commitment: origin and destination Location/status; carried items; leader support; entry-cost components; available non-Double Time MF; difficult-terrain threshold; PAATC requirement; CX and expected combat consequences. Its comparison is not a leftover-MPh budget. A load change can affect both difficult terrain and Panji exposure. A legal connection is not necessarily ADJACENT, and a named Accessible cave connection can still be prohibited during APh.

PAATC is a commitment, not a planning roll. The player chooses a unit and destination, sees the required check, then commits. Failure pins; success immediately advances that unit before another unit's outcome is known. Hidden fortification entry has a similarly meaningful commitment boundary. Live previews must not reveal secret defenders, mines, fortifications or cave routes through disabled reasons or counts.

The event receipt must retain exit effects before arrival. If exit mines pin or break the unit, it never reaches the destination and does not discover its contents. Existing FFE attacks only the advancing/drifting subject. Required effects interleave with movement rather than being postponed until every unit arrives. Final CC encounters carry forward without prematurely rolling Ambush, revealing all strengths or removing Pin/TI.

The interactive reference preview demonstrates topic navigation, glossary definitions and independent consideration state. It does not evaluate a real position, roll dice, perform an advance, consume a capability, or resolve a required event. The live design needs separate states for available, considered, declined, committed, resolved, not applicable, pending integration and missing source.

The catalog contains 57 topics and 61 contextual vocabulary labels.

## Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 7.11A | Opening SW/Gun/prisoner transfers and immediate ski-use dr. | `transfer`, `prisoners`, `skiUse` |
| 7.12A | Japanese squad/HS T-H Hero creation when ADJACENT to enemy AFV. | `tankHunter` |
| 7.13A | Required opening boat/non-Aground LC Heavy-Surf (un)Beaching DR. | `openingSurf` |
| 7.21A | Eligible Infantry advance; PAATC; Difficult Terrain/CX/Panji; climbing completion; ski mode; CE; WA; sewer emergence or sewer CC; trip flares. | `eligibility`, `destination`, `load`, `drop`, `leader`, `difficult`, `stacking`, `wounded`, `enemy`, `paatc`, `heroAdvance`, `concealment`, `buildings`, `fortified`, `entrenchment`, `pillbox`, `crest`, `climb`, `sewer`, `tunnel`, `wire`, `waterTerrain`, `wall`, `night`, `skis`, `paddy`, `caves`, `columnAdvance`, `entryExit`, `boatLoading`, `swimmer`, `crewExposure`, `tripFlare`, `panji` |
| 7.22A | ATTACKER Moderate/Heavy Current drift and Heavy-Surf Ocean drift; pier exceptions. | `attackerDrift`, `oceanDrift`, `pier` |
| 7.23D | DEFENDER Heavy Current drift; pier exceptions. | `pier`, `defenderDrift` |
| 7.24A | Boat (un)Beaching and required Heavy-Surf (un)Beaching checks. | `beach`, `entrySurf` |
| 7.25B | During 7.21-.24, subject-only FFE and mine attacks with listed profiles; Drowning and Column Disbandment. | `ffe`, `mines`, `drowning`, `disband` |
| 7.26A | Replace onboard just-dropped 5/8-inch parachutes with their contents. | `parachutes` |

The 7.25 effects are interleaved with 7.21-.24, not deferred to a single end-of-phase attack batch. E6.21 retains its end-of-friendly-APh Drowning timing. Supplemental entries do not invent ASOP steps.

## Cataloged options

### Phase ownership and commitments

#### Review advance choices and required commitments

ID: `handoff`. **Who:** BOTH. **Type:** Phase handoff.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** A3.7; A4.7; ASOP 7. **PDF pages:** 681, 47, 52, 75, 118. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Who may advance, who must act, and what carries into Close Combat?

- Ordinary advance is an ATTACKER option. Keep the DEFENDER heavy-current drift window and BOTH-side triggered effects separate.
- Carry forward PIN, TI, CX, CC/Melee, load, night restrictions, tunnel movement, parachutes and water conditions. Optional consideration cannot discharge mandatory emergence or drift.
- Preview destination and consequences before commitment. Passing PAATC immediately commits that unit; do not collect successful checks for later selection.

**Checklist behavior:** Propose a per-unit advance receipt and a distinct required-effects queue.

**Illustrative position:** A squad can stay in cover while a tunnel unit must emerge and an opposing boat must drift.

**Terms:** APh, Advance receipt, CC.

### 7.11A · Opening transfers

#### Transfer possessed SW or Guns before advances

ID: `transfer`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.11A. **Rules:** A4.431; B26.4; G9.52. **PDF pages:** 681, 50, 145, 290. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Would reassignment improve either unit’s advance or later combat?

- Use Good Order unpinned units in the same Location; respect Wire, Panji, Crest and vehicle-possession restrictions.
- Transfer at the start of APh, before advances. Possession transfer does not move a Gun or Recover an unattended item.
- Recompute both units’ retained load and leader assistance before offering destinations.

**Checklist behavior:** Show the before/after load of donor and recipient.

**Illustrative position:** A squad gives its MMG to a stationary eligible unit before advancing uphill.

**Terms:** SW, IPC, PP.

#### Transfer or abandon prisoners

ID: `prisoners`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.11A. **Rules:** A20.5-.53; A4.431. **PDF pages:** 681, 87. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this Guard retain responsibility for the advance?

- Offer friendly opening transfer/abandonment only outside Melee; transfer uses the applicable SW transfer procedure and does not consume either unit’s APh capability.
- Preserve guard identity, capacity and prisoner accompaniment. Abandonment does not erase later Massacre/No Quarter consequences.

**Checklist behavior:** Show the new Guard and resulting movement/combat obligations.

**Illustrative position:** A squad transfers prisoners to another eligible Guard before approaching an enemy Location.

**Terms:** Guard, Prisoners, Melee.

#### Resolve ski-use eligibility after a transfer

ID: `skiUse`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.11A. **Rules:** E4.21. **PDF pages:** 681, 231. **Coverage:** Rules integration pending; reference available.

**Consider:** Can the recipient use these transferred skis?

- A recipient already ski-capable needs no new use dr. Otherwise make the immediate ski-use dr: 1 succeeds; 2-6 eliminates the skis.
- Resolve this before offering ski mode. Off-skis equipment carries its PP cost and cannot exceed the unit’s own usage requirements.

**Checklist behavior:** Keep receiving skis, qualifying to use them, and donning them separate.

**Illustrative position:** An untrained recipient receives skis but fails its use dr; no ski-mode advance is then offered.

**Terms:** Skis, dr.

### 7.12A · Japanese Tank-Hunter Hero

#### Attempt Tank-Hunter Hero creation

ID: `tankHunter`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.12A. **Rules:** G1.421-.424. **PDF pages:** 681, 279. **Coverage:** Rules integration pending; reference available.

**Consider:** Is an eligible Japanese squad or HS ADJACENT to an enemy AFV?

- Require armed Good Order Japanese Infantry, qualifying AFV LOS/ADJACENCY, and no disqualifying Prep/Bounding/First/Final Fire or Pin/TI counter.
- Final dr ≤3 creates the hero; apply HS +1, Conscript +2 and committed DC -2. Original 6 pins the creator as specified. Choose and record the Designated Target.
- If MMC make simultaneous creation attempts, their subsequent friendly-phase advance is only as a stack. Preserve special DC-Hero rules instead of treating the new counter as ordinary Infantry.

**Checklist behavior:** Show attempt risk, DC commitment and the resulting target obligation.

**Illustrative position:** A Japanese HS risks pinning itself to create a hero against the adjacent tank.

**Terms:** T-H Hero, Designated Target, AFV, dr.

### 7.13A · Opening surf obligations

#### Resolve opening Heavy-Surf beaching checks

ID: `openingSurf`. **Who:** ATTACKER. **Type:** Required check.

**Timing:** ASOP 7.13A. **Rules:** G13.442-.4423. **PDF pages:** 681, 311, 312. **Coverage:** Rules integration pending; reference available.

**Consider:** Which boats and non-Aground LC require an opening check?

- Check qualifying Ocean Locations with Beach-able hexsides even when the craft intends no action. Boats use their own G13.4423 procedure.
- Resolve TI, changed beaching state, broaching, ramp and passenger consequences before allowing later actions; do not substitute a voluntary beaching toggle.

**Checklist behavior:** Keep required opening checks ahead of advance and drift choices.

**Illustrative position:** A stationary LC can become TI or Broached before its passengers have any choice.

**Terms:** Heavy Surf, Beached, LC, Broached.

### 7.21A · Eligibility, load and destination

#### Choose eligible Infantry or remain in place

ID: `eligibility`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A.3; A4.7-.8; A11.15; A15.431. **PDF pages:** 681, 43, 52, 72, 84. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does this unit retain an advance capability?

- Exclude broken, pinned, TI and CC-marked Infantry; units held in Melee and berserk units cannot use ordinary advance. Distinguish a concealed unit not actually held in Melee.
- Earlier movement or fire does not itself consume advance. Check specific restrictions rather than interpreting every MPh prohibition as an APh prohibition.
- Staying is normally legal, but special compulsory tunnel or T-H Hero instructions must remain open.

**Checklist behavior:** Explain each restriction and preserve an explicit stay choice.

**Illustrative position:** A squad that fired earlier can still advance, but a pinned squad waits for the later Pin-removal window.

**Terms:** APh, TI, CC, Melee.

#### Select one horizontal or vertical destination

ID: `destination`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A3.7; A4.7; B23.422. **PDF pages:** 681, 47, 52, 137, 138. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which single advance connection is legal?

- Choose one hex horizontally or a legal adjacent building-level Location in the same hex. Do not combine entering a new hex with a separate internal level change.
- A horizontal advance may change elevation across a hill or Depression. Apply explicit split-level and terrain exceptions rather than rejecting every elevation change.
- Resolve source and destination status, not merely hex coordinates; no multi-hex route is created by unused MF.

**Checklist behavior:** Show a Location-to-Location connection with its rule and status changes.

**Illustrative position:** A squad can climb the stairs in its current hex or enter the neighboring hex, but cannot do both.

**Terms:** Location, Accessible, ADJACENT.

#### Evaluate retained weapons and portage

ID: `load`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.42; A4.7-.72; D15.82. **PDF pages:** 681, 50, 52, 215. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does the retained load leave any MF for advance?

- Compute non-Double Time capability after portage. In no case may the unit advance if it retains no MF after deducting portage costs.
- Evaluate legal load changes before the attempt. Do not reuse the MPh’s remaining MF or grant Gun Manhandling as an APh action.
- Bicycles cannot be moved in APh even by portage; their normal one-PP value does not create an exception. Motorcycles also lack ordinary APh movement.

**Checklist behavior:** Compare keep, opening transfer, and legal drop choices before committing.

**Illustrative position:** A loaded squad that retains no MF must shed or transfer weight before it can advance.

**Terms:** IPC, PP, MF.

#### Drop possession of SW or a Gun

ID: `drop`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.43. **PDF pages:** 681, 50. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Would leaving equipment permit the intended advance?

- An unbroken unit may drop possession during its APh at no MF cost. A drop at phase start before advancing is treated as unpossessed at the start.
- An unattended item is not automatically transferred or Recovered by another unit. Apply terrain-related loss of abandoned equipment.

**Checklist behavior:** Show where the item remains and recalculate load without silently assigning a recipient.

**Illustrative position:** A squad leaves an HMG behind; a nearby squad cannot simply claim it as an opening transfer afterward.

**Terms:** SW, PP, IPC.

#### Choose leader accompaniment and portage support

ID: `leader`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.12; A4.42; A4.72. **PDF pages:** 681, 48, 50, 52, 75. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does the planned stack qualify for leadership support?

- Require the qualifying same-nationality leader relationship from APh start to finish, same Location/level/status, and movement as the combined stack for the MF bonus.
- Apply the eligible leader’s portage assistance to the actual load. A leader joining only at the destination does not retroactively supply the bonus.
- PAATC leadership can apply without accompanying the unit; do not confuse that check modifier with movement support.

**Checklist behavior:** Display check leadership and movement/portage support as distinct contributions.

**Illustrative position:** A Russian squad with five PP can avoid a two-MF difficult advance if valid leader support changes its available capability.

**Terms:** SMC, MMC, IPC, MF.

#### Calculate difficult advance and CX

ID: `difficult`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.72; A5.11; A7.7. **PDF pages:** 681, 52, 224, 232, 57, 58. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does entry cost reach the lesser of four MF or this unit’s available MF?

- Use entry cost excluding SMOKE, including applicable elevation, hexside, overstacking, night and ski-change costs. Do not use the terrain name alone.
- An already-CX unit normally cannot make a difficult advance. Otherwise the advance creates CX. Preserve the explicit climbing and deep-stream exceptions.
- Show the actual arithmetic and resulting CC consequences before commitment; a Considered mark never spends MF or creates CX.
- Encirclement doubles the first Location entry cost after modifications, regardless of phase; include that effect before the difficult-advance verdict. Keep the SMOKE exclusion explicit.

**Checklist behavior:** Propose a cost receipt with each modifier and threshold.

**Illustrative position:** A two-MF entry can be difficult for a loaded squad even though another squad enters without CX.

**Terms:** Difficult Terrain, CX, MF, PP.

#### Check arrival order and stacking capacity

ID: `stacking`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A5.1-.13; A5.5-.6. **PDF pages:** 681, 52, 53. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Would this arrival overstack or exceed a hard capacity?

- Compute destination occupancy after each advancing unit/stack. Ordinary overstacking adds costs and combat penalties; it is not universally forbidden.
- Pillbox, entrenchment, sewer and tunnel capacities are hard restrictions. Apply squad-equivalent rules rather than counting counters.
- Recompute after previous arrivals or casualties; the last squad may face a different difficult-terrain test.

**Checklist behavior:** Show occupancy and the reason a later unit’s result differs.

**Illustrative position:** An additional squad makes a building entry four MF and therefore difficult.

**Terms:** MMC, SMC, Difficult Terrain.

#### Advance with a wounded SMC or carry one

ID: `wounded`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A17.2; A4.72. **PDF pages:** 681, 85. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the wounded man advance independently or be carried?

- Use the wounded SMC’s reduced three-MF capability and zero IPC when advancing independently.
- Carrying an eligible Good Order wounded man who is not portaging equipment costs five PP; include that load in the carrier’s advance test.
- Pass 71 built MPh carrying, but APh carrying remains backlog 74. Keep this option visible with a pending APh evaluation.

**Checklist behavior:** Offer separate independent and carried choices with truthful coverage.

**Illustrative position:** A squad may need to leave a SW to carry its wounded leader without losing all advance capability.

**Terms:** SMC, Wound, PP, IPC.

### 7.21A · Enemy entry and combat commitments

#### Advance into an enemy Location or existing Melee

ID: `enemy`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A3.7; A4.14; A11.15; A12.15. **PDF pages:** 681, 47, 49, 72, 78. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is entering this Location worth the coming combat?

- Enemy occupation does not impose the ordinary MPh entry prohibition. Check fortified building, pillbox and other explicit exceptions.
- Keep concealment and hidden identities protected until the rules require disclosure. Do not run the ordinary MPh bounce/detection procedure as a generic advance rule.
- Record the encounter for CCPh. Do not roll Ambush, resolve CC or reveal all hidden contents merely because the user browses a destination.

**Checklist behavior:** Preview known combat commitments without disclosing unknown defenders.

**Illustrative position:** A squad enters an enemy-occupied building and awaits the proper CCPh reveal and Ambush windows.

**Terms:** CC, Melee, HIP.

#### Attempt PAATC and immediately resolve the advance

ID: `paatc`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.71; A11.6; G14.32. **PDF pages:** 681, 52, 75, 93, 95, 314. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Must this unit test before entering the AFV Location?

- A MMC entering a Location containing a manned unconcealed enemy AFV normally tests. Apply SMC/Fanatic exemptions and explicit nationality rules; exemption does not remove other advance restrictions.
- Failure pins the unit. Success requires immediate advance; do not wait for other units’ rolls. Attempts need not all be predesignated.
- A leader in the origin may assist without advancing. Inexperienced Infantry use a 1TC rather than NTC; apply applicable nationality/Commissar rules explicitly.
- Seaborne Assault/Evacuation Infantry Fanatic on a Beach or pier needs no PAATC to advance into the Hinterland against an AFV: assess Fanaticism when the check would be required, not only at the destination.

**Checklist behavior:** Use a commit-and-resolve control in the future live UI, not a provisional roll button.

**Illustrative position:** The first squad passes and enters before the second squad decides to attempt its check.

**Terms:** PAATC, AFV, MMC, SMC.

#### Complete a T-H Hero’s compulsory advance

ID: `heroAdvance`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** G1.423-.424. **PDF pages:** 681, 279. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the hero ADJACENT to its Designated Target?

- A T-H Hero ADJACENT to its Designated Target at APh start must advance into that target’s Location if able.
- Preserve the hero’s special target and DC rules. ATMM and CC attack resolution remain in their own later windows.

**Checklist behavior:** Keep the target obligation visible even if the general advance list is marked considered.

**Illustrative position:** A newly created hero cannot choose a safer unrelated destination instead of its required tank target.

**Terms:** T-H Hero, Designated Target, CC.

#### Preserve or lose concealment at the correct trigger

ID: `concealment`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A12.14-.15; A11.19; E1.31-.43. **PDF pages:** 681, 72, 77, 78, 223, 224, 148. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** What information must this advance reveal now?

- Apply lawful observer and terrain tests, including night and special terrain. Do not expose hidden occupants in candidate counts or disabled reasons.
- Keep normal advance concealment separate from CCPh strength revelation and from mine-attack concealment rules. Concealed units not held in Melee can have legal exit choices.

**Checklist behavior:** Project only viewer-safe candidates and record the precise reveal trigger.

**Illustrative position:** An advance into cover can retain concealment, while CCPh still has its own disclosure step.

**Terms:** HIP, LOS, Cloaking.

### 7.21A · Terrain connections and special profiles

#### Use stairs, rowhouses, split levels and rooftops

ID: `buildings`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B23.4; B23.71-.74; B23.8. **PDF pages:** 681, 137, 138, 139, 140. **Coverage:** Rules integration pending; reference available.

**Consider:** Which actual building connection leads to the intended Location?

- Use a valid stairwell for vertical advance and the proper rowhouse/breach or split-level connection for lateral entry.
- Apply rooftop availability by SSR and its own costs. Do not permit an upper-level jump into a different building or collapse Factory/rooftop distinctions.

**Checklist behavior:** Show connection type and entry cost, with pending interior geometry explicit.

**Illustrative position:** A wall between rowhouses changes the allowed connection; adjacent artwork alone is insufficient.

**Terms:** Location, ADJACENT, Difficult Terrain.

#### Test fortified-building entry before commitment

ID: `fortified`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B23.922-.9221; B8.61. **PDF pages:** 681, 140, 141, 117, 118. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a qualifying enemy squad bar this entry?

- An unpinned Good Order armed enemy squad or equivalent inside bars entry unless an applicable Breach or specific rule permits it.
- An unsuccessful attempt consumes that unit’s APh capability and leaves it in its present hex. Respect revelation at the attempt, not during private preview.
- Tunnel emergence has its own exception; sewer emergence does not inherit that exception.

**Checklist behavior:** Distinguish known legal entry from a potentially revealing committed attempt.

**Illustrative position:** An attempted entry reveals the fortified position and leaves the squad unable to try another destination.

**Terms:** Good Order, CC, Tunnel.

#### Advance into, out of or between entrenchments

ID: `entrenchment`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A4.7; B27.41-.54. **PDF pages:** 681, 52, 146, 147. **Coverage:** Rules integration pending; reference available.

**Consider:** Can the advance end beneath the desired fortification?

- One advance can go from inside an entrenchment to inside another in an Accessible hex, subject to capacity and applicable costs.
- Distinguish ordinary entry/exit from connected trenches. Connected-trench movement has explicit cost and mine protection; do not treat every pair of foxholes as connected.

**Checklist behavior:** Include origin and destination below-counter status in the receipt.

**Illustrative position:** A squad can advance directly between eligible entrenchments, but a full destination still bars entry.

**Terms:** Accessible, Difficult Terrain.

#### Apply pillbox entry, exit and bunker restrictions

ID: `pillbox`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B30.41-.44; B30.6-.8. **PDF pages:** 681, 151, 152. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this the outside hex or the separate pillbox Location?

- Enemy Infantry inside bars entering the pillbox itself. Qualifying enemy outside bars entry too, with the explicit tunnel exception.
- Apply the special exit and bunker/trench connections. Do not treat adjacent outside units as occupants of the same Location or ordinary building CC.

**Checklist behavior:** Keep pillbox and outside-hex candidates separate with their restrictions.

**Illustrative position:** An Infantry unit may approach the pillbox hex without being allowed into its occupied interior.

**Terms:** Pillbox, Tunnel, CC.

#### Choose Crest status or move out of it

ID: `crest`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B20.91-.93. **PDF pages:** 681, 133. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the unit entering, leaving or changing sides of a Depression?

- Eligible Infantry IN a Depression may spend its advance claiming Crest status in the same hex; entry from outside uses the proper entering hexside.
- No direct Crest-to-Crest switch is permitted. Leaving on the opposite side requires first leaving Crest status in the current hex, so do not offer an illegal one-step crossing.

**Checklist behavior:** Show the occupied side and resulting exposed hexsides.

**Illustrative position:** A squad cannot rotate to the far bank and leave the Depression in one ordinary advance.

**Terms:** Crest, Location, MF.

#### Finish climbing or scaling at the reached level

ID: `climb`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B11.432-.434; B23.424; B6.4. **PDF pages:** 681, 127, 137, 115. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the MPh reach the destination’s height?

- No new Climbing attempt occurs in APh. At the proper level, an ascending unit can cross the cliff hexside; a descending unit removes its Climb counter.
- Exchange the completed Climb state for CX and apply B11.432’s additional-penalty Pin consequence. Ordinary difficult-advance restrictions do not prevent this special completion.
- Use the related scaling/bridge rules when applicable. A climber still at ground level may leave the Climb counter there to abandon the attempt; an unfinished climb is not a free advance to the top.

**Checklist behavior:** Carry the reached level from MPh and expose only the legal completion.

**Illustrative position:** A climber at the destination height can finish despite already being CX.

**Terms:** CX, Climbing.

#### Choose permitted sewer emergence or adjacent sewer CC

ID: `sewer`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B8.41-.45. **PDF pages:** 681, 117. **Coverage:** Rules integration pending; reference available.

**Consider:** What did the earlier Sewer Emergence dr permit?

- Use the recorded MPh emergence result, not a fresh APh roll. If permitted, emerge concealed into the Manhole Location subject to its entry restrictions.
- The special alternative is entering CC with an adjacent sewer unit under B8.44. Sewer concealment prevents ordinary Melee lock.
- Apply Pin/CX when emerging after leaving an Encircled Location. Keep next-MPh movement obligations attached.

**Checklist behavior:** Offer only the emergence and sewer-CC alternatives supported by the earlier result.

**Illustrative position:** A stack with a failed emergence result cannot choose the surface simply because APh has arrived.

**Terms:** Sewer, CC, CX.

#### Complete mandatory tunnel emergence

ID: `tunnel`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B8.61; G11.933. **PDF pages:** 681, 118, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Which tunnel occupants must exit now?

- Units that entered the tunnel in MPh must emerge at the opposite entrance in this APh, concealed, even if enemy occupied or Fortified.
- An enemy-occupied pillbox exit instead eliminates them. Apply Pin/CX for an Encircled origin and preserve IPC/capacity limits.
- Cave-complex tunnel endpoints can complete by removing a Sewer counter in the correct hidden box; do not force public disclosure of the underground destination.

**Checklist behavior:** Represent emergence as a required continuation of the MPh record.

**Illustrative position:** A unit cannot remain safely in its tunnel for another turn by declining ordinary advance.

**Terms:** Tunnel, CX, Pillbox.

#### Respect Wire’s above and below states

ID: `wire`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B26.4. **PDF pages:** 681, 145, 147. **Coverage:** Rules integration pending; reference available.

**Consider:** Will the advance end on top of Wire?

- APh cannot be used to move beneath Wire. Apply the legal entry/exit and connected-trench exceptions rather than offering a Wire-clearance roll.
- Being on Wire also restricts equipment access and entry into entrenchments, pillboxes and vehicles. Carry the CC disadvantage forward.

**Checklist behavior:** Keep above-Wire and below-Wire states visible in destination explanations.

**Illustrative position:** A squad advances onto Wire but cannot spend an extra step to slip beneath it.

**Terms:** Wire, CC.

#### Distinguish deep streams, marsh and fording

ID: `waterTerrain`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B16.4; B20.43; B21.41. **PDF pages:** 681, 130, 132, 135. **Coverage:** Rules integration pending; reference available.

**Consider:** Which water-terrain entry is actually allowed?

- Deep-stream entry has the explicit already-CX exception and its four-MF/CX handling. Do not generalize it to every water hex.
- Marsh entry is barred in APh except via a bridge. A Water Obstacle cannot be entered by fording in APh, but can be left by a legal advance.

**Checklist behavior:** Explain the named exception or prohibition instead of one generic impassable-water badge.

**Illustrative position:** A CX squad may have a legal deep-stream entry even though it cannot advance into a marsh.

**Terms:** CX, Difficult Terrain.

#### Claim or forfeit Wall Advantage

ID: `wall`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** B9.32-.323. **PDF pages:** 681, 119, 120. **Coverage:** Rules integration pending; reference available.

**Consider:** Would a legal Wall Advantage change help this unit?

- During its APh the unit may claim WA before, during or after a legal advance when eligible. Apply mandatory WA and competing enemy WA conditions.
- Pinned, TI or Immobile units cannot voluntarily claim or forfeit WA. Losing eligibility requires forfeiture at its proper trigger.

**Checklist behavior:** Offer a status decision even to an otherwise stationary eligible unit.

**Illustrative position:** A unit can claim WA in APh without moving to a different hex.

**Terms:** WA, TI.

#### Check night freedom, Cloaking and entry costs

ID: `night`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** E1.21; E1.422; E1.51. **PDF pages:** 681, 223, 224. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this unit have Freedom of Movement, and what does night change?

- A Scenario Defender without Freedom of Movement cannot attempt advance. Do not add an APh version of the once-per-friendly-MPh leader freedom dr.
- Cloaked contents normally advance as one stack unless properly separated. Apply the extra Concealment Terrain MF and its Cloaking/road/Bypass exceptions to the difficult-advance calculation.
- Do not import MPh Straying rolls or premature night illumination cleanup into APh.

**Checklist behavior:** Show freedom, representation and terrain cost as separate gates.

**Illustrative position:** Night can make a loaded unit’s intended entry difficult even though daytime entry would not.

**Terms:** No Move, Cloaking, NVR, Difficult Terrain.

#### Don or remove skis before or after advancing

ID: `skis`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** E4.2; E4.32; E4.5. **PDF pages:** 681, 231, 232. **Coverage:** Rules integration pending; reference available.

**Consider:** Would foot or ski mode better support the destination and ensuing CC?

- Change mode only where allowed and not while pinned/TI. Removing skis costs no MF; donning costs two MF, or one for Finnish Infantry.
- A one-hex ski advance can include a mode change at the start or end; include its MF in the difficult-advance calculation.
- Show ski-mode CC attack/defense penalties and off-skis portage rather than silently treating skis as cosmetic.

**Checklist behavior:** Compare final mode, load, CX and CC consequences.

**Illustrative position:** A Finnish unit can don skis more cheaply, but the combined cost can still make its advance difficult.

**Terms:** Skis, MF, CX.

#### Advance onto or off a paddy bank

ID: `paddy`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** G8.210-.211; G8.22. **PDF pages:** 681, 286, 287. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the unit end IN the paddy or on its bank?

- Apply normal advance restrictions and the applicable IN-hex cost when leaving Bank status. Preserve the different exposure and status of bank occupants.
- Newly appearing paratroops cannot enter onto a Bank counter in that same APh. Do not turn parachute replacement into a paddy-bank advance.

**Checklist behavior:** Treat bank position as a destination status with consequences.

**Illustrative position:** An advancing squad chooses between the bank and paddy interior; a just-dropped stick has no such extra move.

**Terms:** Bank, MF, Difficult Terrain.

#### Apply cave and Cave Complex advance restrictions

ID: `caves`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** G11.6-.76; G11.933. **PDF pages:** 681, 297, 298, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this particular subterranean connection permit APh entry?

- Only permitted Japanese Infantry and associated items enter caves. Use Entrance Hex or Accessible cave/Complex connections; touching Complexes do not permit APh transfer between them.
- Entering a cave/Complex uses the special four-MF inherent allotment with no leader/Double Time bonus. Entry from an Accessible cave/Complex requires at least two available MF, even for hidden movement.
- Minimum Move and difficult advance cannot override that special entry minimum. Apply the one-cave/Complex/tunnel-per-phase restriction and preserve private movement.

**Checklist behavior:** Explain why an Accessible connection can still be unavailable in this phase.

**Illustrative position:** Two touching Cave Complexes allow an MPh connection but not a direct APh transfer.

**Terms:** Cave Complex, Accessible, IPC.

#### Advance an eligible Column as a formation

ID: `columnAdvance`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** E11.52-.522. **PDF pages:** 681, 246. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the intended impulse legal for every participating Column unit?

- Use the multi-hex stack and slowest-unit rate, with applicable leadership and terrain restrictions. Column formation does not waive ordinary unit-type APh restrictions.
- No woods/building entry except the specified Path/road/Factory cases and no MF expenditure beneath fortification counters. Preserve continuous Hazardous Movement and the following Disbandment checks.

**Checklist behavior:** Evaluate the formation rather than offering unrelated per-counter advances.

**Illustrative position:** A Column’s terrain restriction can prevent a tempting move into nearby woods.

**Terms:** Column, MF.

#### Consider scheduled board entry or permanent exit

ID: `entryExit`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** A2.5-.6. **PDF pages:** 681, 46. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is this an eligible delayed reinforcement entry or intended exit?

- Forces capable of APh movement may delay their scheduled entry until APh. Apply specified entry locations and the blocked-entry rule; do not invent a new reinforcement turn.
- A legal board-edge advance can exit into the mirror-image offboard hex. Exiting normally prevents return; show scenario victory consequences before commitment.

**Checklist behavior:** Offer entry/exit with the actual scenario boundary and irreversible consequence.

**Illustrative position:** An eligible reinforcement squad waits until APh to enter at its scheduled edge.

**Terms:** APh, SSR.

#### Load or unload a Beached boat in APh

ID: `boatLoading`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** E5.22-.23. **PDF pages:** 681, 233. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the boat already Beached on the correct shore hexside?

- Apply E5’s explicit boat APh loading/unloading permission across the Beached hexside, capacity and terrain restrictions. APh loading excludes Guns.
- Do not Manhandle a boat in APh or generalize this permission to ordinary vehicles or LC. Beaching state and un-Beaching affect subsequent drift.

**Checklist behavior:** Keep boat permissions distinct from vehicle transport menus.

**Illustrative position:** Infantry can load an eligible Beached assault boat, while its Gun must wait for an allowed loading phase.

**Terms:** Beached, PP, APh.

#### Leave the water or board eligible transport

ID: `swimmer`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** E6.2. **PDF pages:** 681, 235. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this swimmer or forder reach a legal endpoint?

- A swimmer may advance only onto land or a pontoon bridge through a non-cliff hexside, apart from Drift.
- Swimmers/forders may enter an abandoned or friendly boat/amphibious vehicle in the same Location only during their APh. Enemy-occupied boat capture is a separate CCPh case.

**Checklist behavior:** Show land exit and same-Location boarding without granting free swimming movement.

**Illustrative position:** A swimmer can board a friendly boat nearby in its Location, but cannot choose another water hex as ordinary advance.

**Terms:** Drift, Beached.

#### Change eligible vehicle CE or BU status

ID: `crewExposure`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.21A. **Rules:** D5.33-.34. **PDF pages:** 681, 203. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Would changing crew exposure help before CC or the next turn?

- Owner APh permits placing or removing the CE counter, but not voluntarily changing it both ways in the same phase.
- Apply Stun and broken/pinned passenger restrictions and linked crew/passenger exposure. This is not vehicle movement and does not grant a new VCA or weapon fire action.

**Checklist behavior:** Offer one legal exposure transition with its combat implications.

**Illustrative position:** An eligible vehicle buttons up in APh while its movement capability remains unchanged.

**Terms:** CE, BU, PRC.

#### Resolve trip-flare activation at the triggering entry

ID: `tripFlare`. **Who:** BOTH. **Type:** Triggered effect.

**Timing:** ASOP 7.21A. **Rules:** E1.95. **PDF pages:** 681, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this advance trigger a hidden flare?

- Check the actual entry/expenditure and path, trench or subterranean exceptions at the correct moment, including friendly activation.
- Reveal only when triggered and carry illumination into subsequent phases. APh activation does not receive immediate AFPh cleanup.

**Checklist behavior:** Attach the flare event and its later removal window to the advance.

**Illustrative position:** An advance triggers a flare that remains for the following combat.

**Terms:** Trip Flare, HIP.

### 7.22A · ATTACKER drift

#### Resolve Moderate or Heavy Current drift

ID: `attackerDrift`. **Who:** ATTACKER. **Type:** Required drift.

**Timing:** ASOP 7.22A. **Rules:** B21.121; E5.23; E6.2. **PDF pages:** 681, 134, 233, 235. **Coverage:** Rules integration pending; reference available.

**Consider:** Which ATTACKER water subjects must drift?

- Moderate Current acts in the owner’s APh; Heavy Current acts in every APh. Slow Current does not cause this displacement.
- Apply launching/landing/un-Beaching exceptions and no drift into/out of marsh. When two downstream hexes exist, distinguish eligible powered-craft choice from random paddled/disabled-craft drift.

**Checklist behavior:** Separate a mandatory displacement from a permitted choice of downstream hex.

**Illustrative position:** A paddled boat with two downstream hexes uses the prescribed dr instead of choosing the safer one.

**Terms:** Drift, Beached.

#### Resolve Heavy-Surf Ocean drift

ID: `oceanDrift`. **Who:** ATTACKER. **Type:** Required drift.

**Timing:** ASOP 7.22A. **Rules:** G13.444; G13.441. **PDF pages:** 681, 311, 312. **Coverage:** Rules integration pending; reference available.

**Consider:** Which ATTACKER craft or swimmers drift toward land?

- In Heavy Surf use the owner-APh Ocean procedure: amphibians/swimmers in deep Ocean and boats/LC in Ocean, with Aground, adjacent land/reef and same-turn exit/(un)Beaching exceptions.
- Move each subject individually toward the nearest qualifying non-Island land, randomly choosing tied targets; maintain VCA. Do not substitute river-current direction.
- Resolve Swamping and entry consequences for each craft, not a single platoon roll.

**Checklist behavior:** Display the selected drift direction and each craft’s consequence queue.

**Illustrative position:** A drifting LC can require a new surf check on reaching a Beach-able hexside.

**Terms:** Heavy Surf, Drift, Swamping.

#### Apply the craft-specific pier drift exception

ID: `pier`. **Who:** BOTH. **Type:** Drift exception.

**Timing:** ASOP 7.22A. **Rules:** G13.734. **PDF pages:** 681, 313. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the potential drift enter a protected pier hex?

- LC/amphibians do not drift into a pier hex; boats/swimmers do not drift into a stone-pier hex.
- Apply the same exception to the DEFENDER window. Do not turn all wooden piers into universal drift barriers.

**Checklist behavior:** Explain which subject and pier construction prevent displacement.

**Illustrative position:** A wooden pier can stop an LC’s drift without granting the same exception to a swimmer.

**Terms:** Drift, LC.

### 7.23D · DEFENDER drift

#### Resolve DEFENDER Heavy Current drift

ID: `defenderDrift`. **Who:** DEFENDER. **Type:** Required drift.

**Timing:** ASOP 7.23D. **Rules:** B21.121; E5.23; E6.2; G13.734. **PDF pages:** 681, 134, 233, 235, 313. **Coverage:** Rules integration pending; reference available.

**Consider:** Does Heavy Current require opposing displacement now?

- Resolve eligible DEFENDER boats, amphibians and swimmers in Heavy Current, with the same applicable drift and pier exceptions.
- Do not give DEFENDER ordinary advance, Moderate Current drift or owner-only Ocean Heavy-Surf drift simply because this is a shared effects phase.

**Checklist behavior:** Switch decision ownership for lawful drift choices without changing whose APh it is.

**Illustrative position:** The defending boat drifts in Heavy Current even though its Infantry cannot advance.

**Terms:** Drift, APh.

### 7.24A · Beaching after advance and drift

#### Consider permitted boat beaching or un-Beaching

ID: `beach`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** ASOP 7.24A. **Rules:** E5.23; G12.3. **PDF pages:** 681, 233, 303. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this boat legally change its shore relationship?

- Use a qualifying non-cliff shore or permitted pontoon bridge and the boat’s exact location. Loading/unloading crosses its Beached hexside.
- Keep LC separate: APh LC Beaching requires Heavy Surf and the prescribed DR. Do not offer E5’s free boat option to all landing craft.

**Checklist behavior:** Show boat and LC-specific choices, including downstream drift implications.

**Illustrative position:** A boat may un-Beach, but an LC cannot use the same button as a free APh maneuver.

**Terms:** Beached, LC.

#### Resolve newly required surf and grounding consequences

ID: `entrySurf`. **Who:** ATTACKER. **Type:** Required check.

**Timing:** ASOP 7.24A. **Rules:** G13.442-.4423; G12.21; G13.441. **PDF pages:** 681, 303, 311, 312. **Coverage:** Rules integration pending; reference available.

**Consider:** Did entry or a changed beaching state create another required check?

- A qualifying boat/non-Aground LC in Heavy Surf takes the applicable entry (un)Beaching check, separate from its opening check.
- Apply ramp-down, TI, broaching, survival and obstacle consequences as prescribed. Outside the surf exception, qualifying LC entry into shallow Ocean uses G12.21’s Aground check before Beaching.

**Checklist behavior:** Keep checks tied to distinct events, preventing both missed and duplicated rolls.

**Illustrative position:** A craft survives its opening check but must still resolve the new entry event after drift.

**Terms:** Heavy Surf, Broached, LC.

### 7.25B · Immediate effects and survival

#### Resolve existing FFE against the advancing or drifting subject

ID: `ffe`. **Who:** BOTH. **Type:** Triggered attack.

**Timing:** ASOP 7.25B. **Rules:** C1.51-.53; C1.71-.72; C1.9; E12.5; G14.65-.661. **PDF pages:** 681, 165, 166, 167, 247, 316, 317. **Coverage:** Rules integration pending; reference available.

**Consider:** Does entry or increased vulnerability trigger existing artillery?

- During steps 7.21-.24 resolve applicable HE/WP FFE against the moving unit/stack only, not every occupant of the Location again.
- Apply the relevant mission profile, including Barrage, Harassing Fire, Rocket and naval special cases. No new observer mission action is granted by this trigger.
- Preserve the event sequence and consequences before later movement; do not apply MPh-only FFMO/FFNAM simply because a unit advances.

**Checklist behavior:** Attach each eligible FFE attack to its triggering advance/drift event.

**Illustrative position:** An advancing squad entering an existing Blast Area is checked while a stationary friendly stack is not reattacked by its entry.

**Terms:** FFE, WP.

#### Resolve exit and entry mine attacks separately

ID: `mines`. **Who:** BOTH. **Type:** Triggered attack.

**Timing:** ASOP 7.25B. **Rules:** B28.41-.412; G14.53. **PDF pages:** 681, 147, 148, 315. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the subject leave or enter a minefield?

- An exit attack resolves in the original minefield. If it breaks or pins the unit, the unit does not leave and no destination attack or enemy discovery occurs.
- If it exits successfully, resolve the destination minefield attack separately. Only moving subjects are attacked; apply connected-trench and mine-type exceptions.
- Concealed units are attacked at full strength; A-P mine concealment loss follows its special break/Casualty Reduction rule. Include applicable A-B mines for watercraft.

**Checklist behavior:** Show origin-attack and destination-attack checkpoints before confirming arrival.

**Illustrative position:** A squad pinned by exit mines never reaches the concealed enemy in the intended destination.

**Terms:** CC, HIP.

#### Resolve difficult-advance Panji checks

ID: `panji`. **Who:** BOTH. **Type:** Triggered check.

**Timing:** ASOP 7.21A; resolve at the crossing. **Rules:** G9.31; G9.41-.45. **PDF pages:** 681, 289, 290. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this crossing require a Panji MC?

- A normal advance is exempt from the entry MC; difficult advance across a Covered hexside can require it. Apply trench/subterranean and non-Covered entry exceptions.
- Use the G9.43 stack selection and follow-on PTC procedure when applicable. Panji MC has special doubles/Replacement/Casualty Reduction effects and no Heat of Battle.
- Do not offer an extra-MF move underneath in APh. Record above/below status and any Pin/break result before continuation.

**Checklist behavior:** Explain why the same crossing differs with a heavier load or existing CX.

**Illustrative position:** A load that turns normal advance into difficult advance also exposes the squad to a Panji MC.

**Terms:** Panji, Difficult Terrain, MC.

#### Complete required swimmer survival checks

ID: `drowning`. **Who:** ATTACKER. **Type:** Required survival.

**Timing:** ASOP 7.25B. **Rules:** E6.21; G13.444; G13.444. **PDF pages:** 681, 235, 311, 312. **Coverage:** Rules integration pending; reference available.

**Consider:** Is a swimmer still in non-fordable water at the end of its friendly APh?

- Make the required Drowning DR even if no optional movement was chosen: elimination at ≥12 in Slow, ≥11 in Moderate or ≥10 in Heavy Current.
- When a drift direction dr is needed, use the colored die of this DR as prescribed. Apply Ocean-specific instructions rather than inventing another independent direction roll.
- Keep the rule’s end-of-friendly-APh timing explicit inside the ASOP 7.25 effects family; do not check an opposing swimmer solely because its Heavy Current drift occurred.
- For Drowning only, Heavy-Surf Ocean uses Heavy Current; deep Ocean without Heavy Surf and deep lake use Slow Current. These classifications do not add river-current drift to the Ocean.

**Checklist behavior:** Keep a phase-end survival obligation linked to the swimmer after displacement.

**Illustrative position:** A swimmer that stays in Slow Current still needs the end-of-friendly-APh survival roll.

**Terms:** Drowning, Drift.

#### Check Column Disbandment after each relevant event

ID: `disband`. **Who:** BOTH. **Type:** Triggered status.

**Timing:** ASOP 7.25B. **Rules:** E11.532-.536. **PDF pages:** 681, 246. **Coverage:** Rules integration pending; reference available.

**Consider:** Did an advance or attack require the Column to disband?

- Check as each unit advances and after each attack, using Known-enemy LOS and attack conditions. Do not defer every check until the entire formation finishes.
- Apply special OBA/aerial proximity and mine/Wire exceptions. Update formation and Hazardous Movement state as soon as the rule requires.

**Checklist behavior:** Make formation state a consequence of the event sequence.

**Illustrative position:** An advance reveals a Known enemy and changes the remaining Column’s status immediately.

**Terms:** Column, LOS.

### 7.26A · Parachute replacement

#### Replace just-dropped parachutes with their contents

ID: `parachutes`. **Who:** ATTACKER. **Type:** Required replacement.

**Timing:** ASOP 7.26A. **Rules:** E9.6; E9.42; G8.22. **PDF pages:** 681, 244, 287. **Coverage:** Rules integration pending; reference available.

**Consider:** Which onboard 5/8-inch parachutes must be replaced?

- Remove just-dropped 5/8-inch parachutes and place their component units, except those already Deployed under E9.42. Restore normal capabilities at this prescribed point.
- Paratroops do not advance in this APh. Replacement is not permission to take an additional move or enter paddy Bank status.
- Preserve casualties and equipment from the landing record; do not recreate an intact original stick.

**Checklist behavior:** Show a required landing-record completion rather than an ordinary destination picker.

**Illustrative position:** A damaged stick becomes its surviving component counters where it landed.

**Terms:** APh, SMC.

### Supplemental consequences and phase boundary

#### Distinguish landed glider contents from parachutists

ID: `gliders`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** E8.4; E9.6. **PDF pages:** 681, 242, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Has this unit already become eligible Infantry?

- Ordinary glider contents placed after landing can advance if otherwise eligible; apply surviving state and load.
- Vehicle/Gun-associated contents retained under E8.4 do not gain a generic unloading action. Newly dropped parachutists use 7.26A instead.

**Checklist behavior:** Preserve landing provenance so similar counters receive different legal options.

**Illustrative position:** A surviving glider squad may advance while a just-dropped parachute stick must remain.

**Terms:** APh, PRC.

#### Update lawful Control and objective consequences

ID: `control`. **Who:** ATTACKER. **Type:** Advance choice.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** A26.1-.16; SSR. **PDF pages:** 681, 97, 98. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does arrival change Control or satisfy an objective?

- Apply the appropriate unit, enemy-presence and Location/building/hex Control tests. Entering an enemy Location does not automatically capture every level of its building.
- Apply scenario victory timing and distinguish public objective changes from hidden occupancy. Do not declare an early win from a reference consideration.

**Checklist behavior:** Carry an auditable arrival consequence into the live game.

**Illustrative position:** An eligible squad reaches an objective, but its building Control test still depends on the applicable rules.

**Terms:** Good Order, CC, SSR.

#### Carry Acquisition through the completed advance

ID: `acquisition`. **Who:** Weapon owner. **Type:** Tracking consequence.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** C6.5-.52. **PDF pages:** 681, 174, 175, 226. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a weapon legally retain tracking of this target?

- Follow the acquired target only under the weapon’s LOS and Acquisition rules; preserve the end-of-APh choice when acquired targets separate.
- No shot is granted in APh. Keep tracking decisions private to the authorized player and retain night limitations.

**Checklist behavior:** Queue any required tracking choice after the appropriate movement endpoint.

**Illustrative position:** Two acquired units split during advance; the weapon’s owner must retain a legal target rather than duplicate Acquisition.

**Terms:** Acquisition, LOS.

#### Keep other-phase actions out of Advance

ID: `boundaries`. **Who:** BOTH. **Type:** Boundary explanation.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** A4.7; A13.31; A13.7; A15.431; E5.22; ASOP 8. **PDF pages:** 681, 52, 80, 82, 84, 143, 233. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is this an Advance permission or a later action?

- No ordinary vehicle/Cavalry movement, horse mounting/dismounting, Gun/boat Manhandling, Search, Rally, DFF or ordinary shooting is created here. Keep explicit boat/swimmer and CE exceptions.
- Do not resolve Ambush, ATMM, CC attacks, Melee Withdrawal, end-turn concealment gain or Pin/TI cleanup early.
- Terrain Blaze and other prohibited destinations still matter. A phase-specific exemption must be sourced rather than inferred from the generic advance rule.

**Checklist behavior:** Give an explanation and the correct phase rather than an unexplained disabled control.

**Illustrative position:** A squad reaches CC while pinned units elsewhere remain pinned until the proper CCPh cleanup.

**Terms:** APh, MPh, CCPh.

#### Close Advance only after required continuations

ID: `close`. **Who:** BOTH. **Type:** Phase handoff.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** ASOP 7.11A-7.26A; ASOP 8. **PDF pages:** 681, 681. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Are all mandatory effects and arrivals settled?

- Finish committed advances, required emergence, both drift windows, surf events, attacks, survival and parachute replacement. Optional stay choices do not clear required effects.
- Carry final Locations, CX, equipment, CC encounters, concealment, exposure, objectives and acquisition forward. Do not perform CCPh opening reveals or generic counter cleanup.

**Checklist behavior:** Separate marking reference topics considered from a future guarded End phase action.

**Illustrative position:** All Infantry decline ordinary advance, but an unresolved swimmer check still prevents closure.

**Terms:** APh, CC, Advance receipt.

#### Keep deferred and scenario-specific options visible

ID: `sourceProfile`. **Who:** BOTH. **Type:** Coverage boundary.

**Timing:** Rule-specific APh timing; supplemental to ASOP. **Rules:** ASOP 7; SSR; Chapters F/G/H/W. **PDF pages:** 681, 681. **Coverage:** Rules integration pending; reference available.

**Consider:** Are all scenario overlays and evaluators available?

- All nine printed APh steps and identified supplemental rule families are represented. In-volume historical, nationality and equipment exceptions remain profile obligations, not proof that every permutation is audited.
- Missing source, pending integration, unknown applicability and known illegality must be distinct. Do not hide a reference choice because its engine pass is later.
- Scenario-specific terrain, bridges, cellars, winter/desert/PTO rules and special transport must refine these options using their own source ledger before live legality is claimed.

**Checklist behavior:** Retain the full catalog with truthful source and integration status.

**Illustrative position:** An unintegrated cave connection stays available for study without claiming a computed legal destination.

**Terms:** SSR, Source gap.

## Contextual vocabulary

| Label | Meaning |
|---|---|
| APh | Advance Phase. Ordinary advance belongs to the ATTACKER. Opposing drift and triggered consequences have explicit windows. |
| Advance receipt | Proposed live record of origin, destination, load, support, cost, checks and consequences. The reference preview does not create game events. |
| CC | Close Combat. Advance can create a CC encounter; actual Ambush, attacks and most reveal procedures await their CCPh windows. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| IPC | Inherent Portage Capacity. Evaluate retained load and eligible leader assistance before the difficult-advance test. |
| PP | Portage Points. A retained load leaving no MF bars advance even into otherwise inexpensive terrain. |
| Guard | Unit assigned to prisoners. Capacity, fire restrictions and accompaniment are separate from the act of accepting surrender. |
| Prisoners | Captured Personnel under a Guard. Transfer, abandonment, accompaniment and escape have distinct rule windows. |
| Melee | A11.15 continuing combat restriction. Units held in Melee cannot leave by ordinary advance; a concealed unit not held in Melee is a different case. |
| Skis | E4 equipment whose mode changes and downhill bonus have explicit RtPh permission. Low Crawl requires removing skis. |
| dr | One die, distinct from a two-dice DR. |
| T-H Hero | Japanese Tank-Hunter Hero, created through G1.421 with a Designated Target and special compulsory advance and combat rules. |
| Designated Target | The qualifying target chosen when a T-H Hero appears. It constrains the hero’s subsequent actions. |
| AFV | Armored Fighting Vehicle. |
| Heavy Surf | Scenario water condition with mandatory beaching, drift, swamping and broaching procedures. Not equivalent to river Heavy Current. |
| Beached | Watercraft straddling a qualifying shore hexside. Boat loading/unloading and LC permissions differ. |
| LC | Landing Craft, governed by its own transport, fire, beaching and grounding rules. |
| Broached | G13.4421 adverse surf result, with wreck, passenger and equipment consequences. Not a voluntary beaching choice. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| Location | A hex can contain multiple Locations. Same hex does not necessarily mean a leader can help or a transfer is legal. |
| Accessible | A rules-defined connection usable by hypothetical Infantry under normal advance conditions, ignoring enemy presence. Special cave and terrain restrictions still apply. |
| ADJACENT | Rules-defined Location relationship requiring the specified advance connection and LOS, disregarding SMOKE and NVR for this relationship. Not synonymous with neighboring hexes. |
| MF | Movement Factors. Advance uses a fresh capability/portage comparison, not the MF left over from the MPh, and does not grant a multi-hex movement budget. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| Difficult Terrain | A4.72 advance test using entry cost excluding SMOKE against the lesser of four MF or the available non-Double Time MF allotment. This is a unit-and-load calculation, not just a terrain tag. |
| CX | Counter Exhausted. Usually bars an already-CX unit from a difficult advance; climbing and deep-stream entry have explicit exceptions. A difficult advance creates CX. |
| Wound | A17 injury with severity, reduced morale/leadership, IPC and movement consequences. Track MF already spent if it occurs mid-rout. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| PAATC | Pre-AFV Attack Task Check. An eligible MMC tests before advancing against a manned unconcealed enemy AFV. Failure pins; success commits immediate advance. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| Cloaking | Night concealment representation whose contents normally advance as one stack until properly separated; hidden contents remain owner-only. |
| Good Order | The defined unit status, separate from merely unbroken. Apply the exact status requirement of each action. |
| Tunnel | B8.62 special rout connection between entrances, with emergence conditions and immunity to Interdiction while inside. |
| Pillbox | Separate Location equivalent to a building for rout, with special Known-enemy, entry/exit, ordinary forced-rout and Blaze rules. |
| Crest | Depression-edge status on a specified side, not a free choice of every edge of the hex. |
| Climbing | B11.4 multi-turn movement using a recorded height and vertex. APh completes or leaves the climb at a legally reachable level. |
| Sewer | B8 subterranean Location. APh emergence depends on the earlier Emergence dr; adjacent sewer CC is a special alternative. |
| Wire | Obstacle with distinct above/below states. Infantry cannot move beneath it during APh. |
| WA | Wall Advantage. Its claim, loss and mandatory conditions depend on B9.32 and the position. |
| No Move | Night Scenario Defender restriction. Freedom of Movement must exist before attempting an advance. |
| NVR | Night Visibility Range. Night Rout also has specific E1.54 rules beyond visibility changes. |
| Bank | Rice-paddy bank status, distinct from being IN the paddy. It changes movement, exposure and combat consequences. |
| Cave Complex | Japanese subterranean system. Accessibility does not grant APh movement between touching Complexes. |
| Column | Formation using multi-hex Impulse movement/advance at its slowest unit’s rate, with restrictions and immediate Disbandment checks. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| Drift | Mandatory displacement under current or surf rules. Ownership, timing and exceptions differ from voluntary advance. |
| CE | Crew Exposed. A vehicle crew posture whose voluntary changes have timing restrictions. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| Trip Flare | E1.95 hidden device with entry/expenditure triggers. A flare set off in RtPh persists until the next Player Turn’s AFPh cleanup. |
| Swamping | G13.441 watercraft hazard on qualifying Ocean entry, including drift. Resolve the craft-specific result. |
| FFE | Fire For Effect. Existing HE/WP FFE can attack the routing subject on qualifying entry or increased vulnerability; it does not itself Interdict. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| Panji | Covered-hexside obstacle. Normal advance avoids its entry MC; difficult advance can trigger it. APh does not permit the extra-MF move beneath it. |
| MC | Morale Check. |
| Drowning | E6.21 survival DR at the end of each friendly APh for a swimmer remaining in non-fordable water; thresholds depend on current. |
| Acquisition | A weapon-specific targeting benefit. Tracking a moving target is not permission to fire in APh. |
| MPh | Movement Phase. THE MPh contains a separate start, during and end for each moving unit or stack. |
| CCPh | Close Combat Phase, after Advance. It owns its reveal, Ambush, attack, withdrawal and cleanup windows. |
| Source gap | Scenario/module/equipment evidence still to reconcile. Distinct from integration pending and known illegality. |
