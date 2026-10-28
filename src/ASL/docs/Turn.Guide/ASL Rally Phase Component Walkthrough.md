# ASL Rally Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design catalog and interactive preview. This is not an executable Blazor component or a fresh Rules test run.

This expands the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md). The target is the completed ASL.Rules implementation after all passes. Every cataloged option remains visible, including rules whose integration is pending.

## Source reconciliation and limits

The authority for this review is the supplied **eASLRB v3.01**, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. ASOP is physical PDF page 678. Page locators below are one-based PDF pages, not chapter page labels. The original source remains outside the repository; this document contains paraphrases and locators.

Source location: `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`.

The review covers every clause of all ten printed Rally ASOP steps, follows their rule references, and supplements them with the volume’s Rally, standing-choice and cross-phase procedures. A full-volume text search found 124 RPh/Rally Phase mentions; chapter text, charts, training examples and index references were distinguished rather than treated as 124 actions. The printed ASOP page was also visually checked. Chapters F, G, H and W are represented even where they extend beyond the current implementation passes.

The ASOP explicitly names ABtF CG14, BRT CG14, KGP CG18 and RB CG17 first-two Self-Rally exceptions. Their complete module text is not in this PDF. Each is represented now, but must say **Source text needed; rules integration pending**. SASL and other external scenario/module rules require their own source profiles. This is exhaustive against the supplied Rally ASOP, not a claim to have audited every unpublished or external SSR.

### ASOP coverage ledger

| Printed step | Clauses represented | Catalog records |
|---|---|---|
| 1.11A | SSR/provisional reinforcements, offboard setup, Air, DD, Cloaking, Gliders, Para, LC | arrival, entry, dyoArrival |
| 1.12A | Wind, collapse, NVR, civilian information, fog, rain/snow, dust, Heavy Surf, tropical weather, Gusts/SMOKE | wind, night, civilian, precipitation, fog, snowDrifts, smoke, dust, surf |
| 1.13B | Recombination and leaderless exceptions; Animal-Pack start/end and halfway load/unload | recombine, packStart, packHalf, packReverse |
| 1.14B | Recovery, vehicle/PTO restrictions, ski-use dr | recover, skis |
| 1.21A | Deployment and exceptions; Scrounging with TI and Scrounged consequences | deploy, scrounge, nationality |
| 1.22B | SW/Gun and vehicle repair; SW/Gun/prisoner transfers; ski-use dr | repair, vehicleRepair, transfer, prisoners, skis |
| 1.23B | All Rally paths, special leaders, first-MMC privilege, named first-two exceptions and consequences | leaders, first, self, assisted, commissar, results, campaign, nationality, modifiers, winterFate |
| 1.24A | Final Drop Points and aerial Stick placement | drop |
| 1.31B | Shock/UK recuperation; DM removal/retention | shock, dm |
| 1.32B | WA claim, ATTACKER first | wa |

Supplemental procedures are OBA FFE:C end-RPh housekeeping, triggered Field Phone cuts, ammunition replenishment/transfer commitments, incoming Stun boundary verification, concealment/revelation and standing Guard choices. They keep their own timing; they are not invented ASOP substeps.

## What changed after reconciliation

- Replaced the generic deferred-chapter card with specific weather, Animal-Pack, nationality and campaign records.
- Corrected Finnish leaderless Deployment to 1TC; Ranger, USMC and KMC permissions remain distinct.
- Added mandatory non-zero leadership, the prohibition on all Disrupted Self-Rally, and Finnish/Japanese leader-creation restrictions.
- Added Extreme Winter Fate on Original 11 where applicable, special terrain exclusions, and Heavy Surf Rally-to-CX consequences.
- Distinguished repairable radios from non-repairable Field Phones and added triggered phone-security cuts.
- Added Animal-Pack reversal and midpoint scheduling, tropical permanent rain, Desert dust, Heavy Surf wind restrictions, and DYO ground arrivals.
- Reconciled Stun wording: D5.34 says end of Player Turn; quick-reference charts on PDF 221/704 say beginning of next RPh. Represent one boundary transition, with an overdue-state reminder, never duplicate flips or a Shock roll.

## Checklist contract

The printed ASOP controls order where it conflicts with the rulebook. Within the same step, actions can occur in any order unless otherwise specified; conflicts are resolved ATTACKER first. The UI must not force a rigid per-card wizard. A and B step suffixes indicate ATTACKER and both sides; per-rule friendly-RPh restrictions still apply.

Track one normal action/attempt per unit in RPh with the ASOP exceptions: multiple permitted repairs, a leader rallying several units, and Recovery not being an action by the broken former possessor. Keep prisoner transfer’s explicit no-penalty rule and successfully rallied leaders’ ability to rally others. Failed attempts still consume the applicable opportunity.

Each live decision is identified by option, subject, side, Player Turn and decision window. Separate optional attempts from mandatory consequences and cross-turn commitments. Track Not considered, Considered and declined, Attempted and failed, Resolved, and Required follow-up. An acknowledgement cannot resolve a mandatory roll or consume a game action. A pending rule cannot execute.

The preview’s Mark topic considered is a reference-mode acknowledgement only. It does not prove applicability, legal exhaustion or phase completion. Reconciliation version 2 clears old acknowledgements while preserving a valid selected topic. The real UI should invalidate affected acknowledgements when the underlying rule/scenario/window changes.

Shared dice are essential: one Wind Change roll drives several procedures. Snapshot pre-roll weather for snow drifts, use post-roll wind for fog, and remember once-per-Game-Turn versus once-per-Player-Turn effects. Show unknown scenario relevance explicitly and never reveal hidden units or phones through prompts or counts.

An illustrative chain: recombine/recover before committing a leader; consider Deployment before Rally; reserve the first-MMC opportunity before another MMC attempts; rally an eligible leader before dependents; apply mandatory leadership; resolve Fate/HOB/Field Promotion and phone-cut consequences; settle Shock, DM, WA and applicable OBA housekeeping. Each step expands only the subjects the player is entitled to know.

## Cataloged topics

### 1.11A · Reinforcements and entry

#### Check reinforcements and air arrival

Catalog ID: `arrival`. **Who:** ATTACKER. **Type:** Required when due.

**Rules:** ASOP 1.11A; A2.51-.52; E7.2. **PDF pages:** 678, 46, 236. **Coverage:** Air arrival: rules integration pending.

**Consider:** Are forces due now, or does an SSR require an arrival roll?

- Resolve provisional arrivals and any applicable Air Support arrival test. Air Support availability is subject to its weather restrictions.
- Set up forces due this Player Turn offboard, respecting their entry instructions. Do not give offboard units the ordinary on-board action menu.
- An Air Support arrival success schedules entry in the next MPh; it does not make an RPh attack.
- Air Support arrival uses a dr less than the Game Turn number; DYO ground reinforcement arrival is a different DR test. Show separate pending checks.

**Checklist behavior:** Show each scheduled force, its entry edge or restriction, and its next action. Keep failed arrival rolls recorded, with the next eligible check.

**Illustration:** Air Support / Arrival check due / Rules integration pending / Read E7.2. It stays visible even though the arrival roll cannot yet be executed.

**Terms:** SSR, OB, dr, ATTACKER.

#### Prepare special offboard entry

Catalog ID: `entry`. **Who:** ATTACKER. **Type:** Conditional procedure.

**Rules:** A2.51-.52; D16.8; E1.41; E8.1; E9.1-.11; G1.664; G14.23. **PDF pages:** 678, 46, 216, 223, 241, 242, 281, 314. **Coverage:** Rules integration pending; reference available.

**Consider:** Which entry method and initial configuration apply to each arriving force?

- Record entry instructions, offboard setup, passengers and towing, CE/BU, dismantled or limbered equipment, and applicable Motion status.
- Expose DD tank preparation, night Cloaking, glider setup, parachute predesignations including Japanese paratrooper conditions, and seaborne landing-craft manifests as separate entry-method details.
- Keep secret manifests and predesignations private. Offboard forces cannot use the ordinary onboard RPh menu; retain the specific offboard Deployment permission.

**Checklist behavior:** Each force has its own entry record and source conditions. A pending entry method never disappears behind a generic reinforcement check.

**Illustration:** Prepare special offboard entry / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** OB, SSR, CE, BU, dm, DD, LC, HIP.

#### Roll for DYO reinforcements

Catalog ID: `dyoArrival`. **Who:** ATTACKER. **Type:** Conditional procedure.

**Rules:** H1.7. **PDF pages:** 678, 334. **Coverage:** Rules integration pending; reference available.

**Consider:** Are purchased DYO reinforcements awaiting permission to enter?

- Make the prescribed DR at the start of their owner’s RPh until eligible.
- Success requires DR no greater than the current Game Turn number; observe the friendly entry-edge requirement.

**Checklist behavior:** Keep this roll distinct from the Air Support arrival dr.

**Illustration:** Roll for DYO reinforcements / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** DYO, DR, OB.

### 1.12A · Wind and linked effects

#### Resolve Wind Change and building collapse

Catalog ID: `wind`. **Who:** ATTACKER resolves. **Type:** Required procedure.

**Rules:** ASOP 1.12A; B25.65-.66; E3.4. **PDF pages:** 678, 144, 145, 222, 228, 229, 230, 231. **Coverage:** Full wind procedure: partially integrated.

**Consider:** Which opening effects follow from this Wind Change DR and the existing conditions?

- Use one Wind Change DR, retaining both dice for linked procedures. Apply the wind direction/force procedure on the qualifying result.
- Resolve Gusts on 12 normally, or 10 or more under Gusty conditions. Apply B25.66 building-collapse selection and consequences when triggered.
- Fan out to the weather, visibility, information and SMOKE rows below using the same dice. Do not roll independently for every weather topic.
- A Gusts result affects later fire-spread procedures; do not perform ordinary AFPh fire spread in Rally.

**Checklist behavior:** One opening event can produce several required consequences. Expand them as child tasks, not optional checkboxes. Do not roll separately for effects that share the Wind Change dice.

**Illustration:** Wind check → Gusts → eligible SMOKE updates → collapse check, if a burning building qualifies. Civilian information, when triggered by the SSR, cannot simply be declined.

**Terms:** DR, dr, NVR, EC, LV, SSR.

#### Adjust Night Visibility Range

Catalog ID: `night`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** E1.12. **PDF pages:** 678, 222. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the shared Wind Change result alter night visibility?

- Apply the night procedure to the colored and white dice, preserving their identities.
- Respect the opening-turn exclusion and the permitted NVR bounds; update only the affected visibility facts.

**Checklist behavior:** Refresh disclosed LOS-dependent opportunities without revealing hidden enemies.

**Illustration:** Adjust Night Visibility Range / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** NVR, DR, dr.

#### Resolve Civilian Interrogation information

Catalog ID: `civilian`. **Who:** ATTACKER receives. **Type:** Conditional procedure.

**Rules:** E2.4-.43. **PDF pages:** 678, 228. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the SSR enable information from this Wind Change roll?

- Only use this procedure when an SSR enables it. The qualifying total is 3 in hostile/neutral country or 4 in friendly country.
- Information cannot be refused. Determine its random location and eligible receiving friendly units before revealing the permitted information.

**Checklist behavior:** Create a required information-resolution follow-up, not an optional search or attack.

**Illustration:** Resolve Civilian Interrogation information / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** SSR, DR, HIP, SAN.

#### Change rain or falling snow

Catalog ID: `precipitation`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** E3.51-.53; E3.71; G16.2. **PDF pages:** 678, 229, 230, 319. **Coverage:** Rules integration pending; reference available.

**Consider:** Does existing weather make this Wind Change DR start, intensify or stop precipitation?

- Use the proper Overcast/rain or falling-snow procedure; a qualifying high roll can start or intensify, and a low roll can stop precipitation.
- Recompute visibility, eligible SMOKE and environmental effects; rain makes EC Wet for the rest of the scenario.
- Tropical DYO weather footnotes can make Heavy Rain permanent, including rain that begins during play under the specified chart result. Do not let the ordinary stopping roll override that condition.

**Checklist behavior:** Explain which scenario condition prevents an otherwise normal weather change.

**Illustration:** Change rain or falling snow / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** DR, EC, LV, DYO.

#### Reduce fog density

Catalog ID: `fog`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** E3.312. **PDF pages:** 678, 229. **Coverage:** Rules integration pending; reference available.

**Consider:** Is fog due to thin after the Wind Change result?

- Use the post-roll wind conditions. Mild Breeze reduction is once per Game Turn.
- Heavy Winds or Gusts reduction is per Player Turn. Record the last reduction window so a Game Turn effect is not applied twice.

**Checklist behavior:** Update the actual fog level and its visibility consequences.

**Illustration:** Reduce fog density / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** LV, DR.

#### Place new snow drifts

Catalog ID: `snowDrifts`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** E3.75. **PDF pages:** 678, 231. **Coverage:** Rules integration pending; reference available.

**Consider:** Were Heavy Winds or Gusts already in force with Ground or Deep Snow before this roll?

- Check pre-roll conditions, not the new wind state.
- Apply the random drift-placement procedure for each mapboard when due.

**Checklist behavior:** Retain a pre-roll weather snapshot for this branch of the opening procedure.

**Illustration:** Place new snow drifts / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** DR, EC.

#### Apply Gusts and first-RPh SMOKE drift

Catalog ID: `smoke`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** A24.61-.62; B25.651; F11.74; G11.8. **PDF pages:** 678, 92, 93, 145, 268, 298. **Coverage:** Rules integration pending; reference available.

**Consider:** Which existing SMOKE and dust counters must change now?

- Gusts remove dispersed SMOKE and Vehicle Dust and disperse remaining affected SMOKE. Apply cave and other source-specific exceptions.
- Resolve the special first-RPh SMOKE drift where applicable. Ordinary later drift retains its AFPh timing.
- Apply rain restrictions with the precipitation result, rather than leaving impossible counters on the map.

**Checklist behavior:** Show individual affected counters; this is mandatory housekeeping, not an optional smoke-placement action.

**Illustration:** Apply Gusts and first-RPh SMOKE drift / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** SMOKE, DR.

#### Change Desert dust intensity

Catalog ID: `dust`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** F11.73-.77. **PDF pages:** 678, 267, 268. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this Desert scenario allow Heavy Dust, and what do wind and rain do to it?

- Use the density progression under the Heavy-Dust prerequisites, including the Gusts and cessation rules.
- Rain removes the applicable dust for the remainder of the scenario under F11.77. Vehicle Dust removal is also linked to Gusts.

**Checklist behavior:** Keep the Desert option visible as pending integration, with its actual scenario prerequisites.

**Illustration:** Change Desert dust intensity / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** LV, DR.

#### Preserve Heavy Surf wind restrictions

Catalog ID: `surf`. **Who:** ATTACKER resolves. **Type:** Conditional procedure.

**Rules:** G13.448; G16.2. **PDF pages:** 678, 311, 319. **Coverage:** Rules integration pending; reference available.

**Consider:** Does Heavy Surf prevent this wind-force change?

- Heavy Surf keeps Heavy Winds in effect for the entire scenario. A Wind Change dr of 5 or more does not remove that wind.
- Do not invent a Rally roll to start or stop Heavy Surf. Tropical DYO setup tables are setup procedures; only their continuing restrictions are applied now.

**Checklist behavior:** Annotate the shared Wind Change result with the restriction that overrides it.

**Illustration:** Preserve Heavy Surf wind restrictions / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** dr, DYO.

### 1.13B · Recombination and Animal-Pack

#### Recombine half-squads

Catalog ID: `recombine`. **Who:** Both sides. **Type:** Optional early choice.

**Rules:** A1.32; A2.8. **PDF pages:** 678, 45. **Coverage:** Partially integrated.

**Consider:** Would combining these two half-squads serve the coming turn better than keeping them separate?

- Check nationality, matching Strength Factor, Class, Good Order, and the same Location and terrain.
- Check leader presence and the specific leaderless exceptions. Do not infer eligibility from “two HS in one hex.”
- Reserve the participating units’ RPh activity. A leader enabling recombination cannot then take a different RPh activity.
- An eligible leader may enable multiple pairs as its recombination activity. Combining Fanatic and non-Fanatic HS does not produce a Fanatic squad. Include the Unarmed, Guard, Finnish and Carrier leaderless exceptions.

**Checklist behavior:** Show the resulting squad and lost separate capabilities before confirmation. The early timing should be visible before the player spends those units on recovery or repair.

**Illustration:** Two eligible HS + leader / Recombine, or keep separate. Warning: this commits the leader’s RPh activity, which may affect later rally assistance.

**Terms:** HS, MMC, Good Order, Location.

#### Start or finish Animal-Packing

Catalog ID: `packStart`. **Who:** Both sides. **Type:** Conditional procedure.

**Rules:** ASOP 1.13B; G10.11; G10.3. **PDF pages:** 678, 291. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this the initial RPh of the Game Turn, and should a packing period start or finish?

- Check the eligible Gun, Mule and same-nationality Good Order non-PRC crew in the same terrain and Location.
- Place or remove Animal-Pack counters only in the applicable initial-RPh window. Calculate the period from the Gun’s M#, adding the non-mortar adjustment, minimum one Game Turn, and night doubling.
- Track Pack-TI separately from ordinary TI; interruptions can invalidate time accumulated without freeing the Gun or Mule.

**Checklist behavior:** Show the start turn, required full turns, halfway window and completion window before committing.

**Illustration:** Start or finish Animal-Packing / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** TI, M#, MTR, Pack-TI.

#### Load or unload at the packing midpoint

Catalog ID: `packHalf`. **Who:** Both sides when due. **Type:** Conditional procedure.

**Rules:** ASOP 1.13B; G10.31. **PDF pages:** 678, 291. **Coverage:** Rules integration pending; reference available.

**Consider:** Has half of the packing period completed?

- Resolve the load/unload in the following Player-Turn RPh; this can be the second RPh of a Game Turn.
- The crew must retain Pack-TI. On unloading choose the Gun CA, and check terrain that eliminates an unloaded Gun.
- Loading/unloading does not itself finish the packing period.

**Checklist behavior:** Keep midpoint and completion as two obligations, never one checkbox.

**Illustration:** Load or unload at the packing midpoint / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** Pack-TI, CA, MTR.

#### Reverse a packing commitment

Catalog ID: `packReverse`. **Who:** Owning side. **Type:** Conditional procedure.

**Rules:** G10.32-.33. **PDF pages:** 678, 292. **Coverage:** Rules integration pending; reference available.

**Consider:** Would you reverse packing or unpacking at this initial-RPh opportunity?

- Declare reversal at 1.13B in the initial RPh of a Game Turn. The new period uses the net time already spent, with the involuntary-unloading exception.
- Preserve Pack-TI. Explain permitted ancillary handling under G10.32 while the Gun is unloaded, rather than applying an ordinary weapon-transfer shortcut.

**Checklist behavior:** Preview the revised completion date and retained restrictions before the player decides.

**Illustration:** Reverse a packing commitment / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** Pack-TI, CA.

### 1.14B · Recovery

#### Recover a weapon or Gun

Catalog ID: `recover`. **Who:** Both sides. **Type:** Optional early choice.

**Rules:** A4.44; D6.31; E4.21; G.5. **PDF pages:** 678, 50, 207, 276. **Coverage:** Partially integrated.

**Consider:** Is valuable equipment unpossessed, or held by a broken unit from which an SMC can recover it?

- Offer eligible unpinned Good Order non-Bypassing units in the correct Location, with known-enemy, terrain, and transport restrictions.
- Distinguish an unpossessed weapon from a weapon held by a friendly broken unit: the latter recovery is an SMC case.
- Preview the Recovery dr and its modifiers. Recovering a weapon consumes the recovering unit’s RPh action; it is not an action by the broken former possessor.
- A weapon recovered this phase cannot then be transferred in the same phase. Apply a ski-use check if recovering skis requires one.
- Recovery succeeds on a final dr below 6. Include CX and night penalties, and the +2 jungle/kunai/bamboo modifier unless the item is in the listed vehicle, trench, building or pillbox exception. No leadership modifier applies.

**Checklist behavior:** The checklist preserves the attempted result even on failure and shows which later actions are excluded.

**Illustration:** Leader: recover MG, or preserve this leader for rally assistance? Broken former possessor: its own Rally attempt is not consumed by the leader’s recovery.

**Terms:** SW, SMC, Good Order, dr, drm, CX, Location.

#### Resolve ski-use capability after Recovery or Transfer

Catalog ID: `skis`. **Who:** Receiving unit. **Type:** Conditional procedure.

**Rules:** E4.21; ASOP 1.14B and 1.22B. **PDF pages:** 678, 231. **Coverage:** Rules integration pending; reference available.

**Consider:** Does receiving these skis require a ski-use determination?

- Offer the check as a dependent result of either Recovery at 1.14B or Transfer at 1.22B.
- Apply known ski-capable nationality and unit rules before rolling. Preserve the result for the receiving unit.

**Checklist behavior:** One definition is attached to two source windows; it is not an independent extra RPh action.

**Illustration:** Resolve ski-use capability after Recovery or Transfer / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** dr, SW.

### 1.21A · Deployment and Scrounging

#### Deploy a squad

Catalog ID: `deploy`. **Who:** ATTACKER; special exceptions apply. **Type:** Optional choice.

**Rules:** A1.31; A2.52; A20.5; A25.7. **PDF pages:** 678, 45, 46, 87, 97, 319, 659, 660. **Coverage:** Partially integrated.

**Consider:** Would two half-squads provide useful coverage, or do you need the full squad and its leader elsewhere?

- Check nationality and unit exceptions, the leader requirement, and any NTC requirement.
- Show how possessions would be assigned to the resulting HS.
- An ordinary attempt uses both squad and directing leader for this RPh even when it fails. A Guard’s special automatic deployment has its own rule.
- Finnish and Ranger leaderless Deployment uses a 1TC, not the ordinary NTC. USMC 7-6-8 and eligible KMC 5-5-8 use their leaderless NTC exceptions. Unarmed, Guard and Carrier cases keep their own rules.

**Checklist behavior:** Warn about the leader’s competing rally role before committing the attempt. Show special nationality and Guard cases as distinct eligibility explanations.

**Illustration:** Leader can direct Deployment or provide ordinary rally assistance later. Selecting Deployment previews that conflict; it does not silently select a tactic.

**Terms:** MMC, HS, NTC, DRM, Good Order.

#### Scrounge a wreck or abandoned vehicle

Catalog ID: `scrounge`. **Who:** ATTACKER. **Type:** Optional choice.

**Rules:** D10.5–.52. **PDF pages:** 678, 211. **Coverage:** Core procedure integrated.

**Consider:** Is recovering a possible weapon worth tying up this Infantry unit for the Player Turn?

- Check an eligible non-berserk, unbroken Infantry MMC, no Melee, and a same-hex non-burning wreck or abandoned vehicle.
- Exclude already-scrounged or otherwise disqualified sources. Show what is knowable without revealing hidden information.
- Preview the Scrounged and TI consequences regardless of success; any salvaged SW may be dismantled.

**Checklist behavior:** This is a commitment preview, not just a loot button. The unit can lose its later movement and fire opportunities even if the dr finds nothing.

**Illustration:** Scrounge / Considered, declined: this unit is needed to move. The checklist records a deliberate choice without treating it as an executed action.

**Terms:** MMC, SW, TI, dr, dm.

### 1.22B · Repairs and transfers

#### Repair support weapons and Guns

Catalog ID: `repair`. **Who:** Both sides. **Type:** Optional choice.

**Rules:** A9.72; A9.8; C1.22-.23; W2.14; W3.11. **PDF pages:** 678, 65, 163, 660. **Coverage:** Partially integrated.

**Consider:** Which malfunctioned weapons should you risk repairing, and which unit can legally attempt it?

- Show the possessor, repair threshold, and permanent-loss result before any roll.
- Apply the unit’s allowed repair capacity: A9.72 can permit more than one weapon, so do not use one checkbox per unit to mean all repairs are done.
- Gun repair requires the appropriate crew and conditions; captured-equipment restrictions and dismantled-weapon repair remain represented.
- Radio repair is represented under C1.22; Field Phones cannot be repaired under C1.23. Captured weapons cannot be repaired unless recaptured by the original side.
- Include the Early Korean War U.S./ROK SW Repair Number override of 1 under W2.14/W3.11 when their date and force conditions apply.

**Checklist behavior:** Resolve each weapon separately. A completed repair attempt is finished even when nothing is repaired. A malfunction is not the same as permanent disablement.

**Illustration:** MMG / Repair on its R# / dr 6 eliminates / Attempt or leave malfunctioned. A second qualifying weapon remains a separate choice.

**Terms:** SW, R#, dr, Good Order, dm.

#### Repair vehicle armament

Catalog ID: `vehicleRepair`. **Who:** Both sides. **Type:** Optional choice.

**Rules:** D3.7. **PDF pages:** 678, 201. **Coverage:** Partially integrated.

**Consider:** Which individual weapon can this crew attempt to restore?

- List each malfunctioned armament separately. A shocked or stunned crew cannot perform the ordinary repair attempt.
- Use the weapon-specific exposure conditions. AAMG repair is different from internal MA or MG repair; include the qualifying Hero Rider case.
- Preview success, permanent disablement, and any resulting Recall obligation.

**Checklist behavior:** A single “repair tank” checkbox hides meaningful choices. The unit card should show one row per weapon with the applicable crew conditions.

**Illustration:** MA: repair choice. AAMG: exposure or Hero Rider eligibility to check. An unsupported SA case remains visible as rules integration pending.

**Terms:** AFV, MA, SA, AAMG, CE, BU, dr.

#### Transfer weapons and Guns

Catalog ID: `transfer`. **Who:** Both sides. **Type:** Optional choice.

**Rules:** A4.431; D6.31; E4.21. **PDF pages:** 678, 50, 81, 207. **Coverage:** Terrain and transport edge cases require coverage review.

**Consider:** Who should carry or use this weapon after Rally?

- Check giver and receiver eligibility, same Location, and special terrain and vehicle restrictions.
- Exclude equipment recovered this phase. Distinguish Transfer from Recovery and from rearrangement on Deployment.
- Show resulting possession and carrying burden. A transferred ski set can require its own ski-use dr.
- Cavalry cannot Recover SW/Guns but can transfer SW under A13.33. Do not apply Recovery eligibility to every Transfer.

**Checklist behavior:** Link the new possessor to movement planning without inventing a movement expenditure in RPh. Apply the actual action accounting rather than assuming every equipment transaction is free.

**Illustration:** Transfer MG from squad A to squad B / PP consequences shown / Recipient and giver validated independently.

**Terms:** SW, PP, Good Order, Location, dr.

#### Transfer or abandon prisoners

Catalog ID: `prisoners`. **Who:** Owning side in its permitted RPh. **Type:** Optional custody choice.

**Rules:** A20.5–.51. **PDF pages:** 678, 87. **Coverage:** Partially integrated.

**Consider:** Should another eligible unit guard these prisoners, or are you considering abandonment?

- Check custody, same Location, Guard capacity, and Melee restrictions.
- Keep prisoner transfer distinct from SW transfer: its explicit exception does not penalize either unit’s RPh capabilities.
- Explain abandonment consequences and any special Guard deployment option.

**Checklist behavior:** Freeing a Guard for another role is a legitimate consideration. Avoid charging it a generic RPh action that the prisoner rule explicitly exempts.

**Illustration:** Guard transfer / Recipient capacity reviewed / Original Guard’s RPh capability preserved under A20.5.

**Terms:** MMC, SMC, HS, Location.

### 1.23B · Rally attempts and consequences

#### Rally leaders and choose assistance

Catalog ID: `leaders`. **Who:** Both sides. **Type:** Optional; Commissar exceptions.

**Rules:** A10.6, .7, .71; D6.651; A10.72. **PDF pages:** 678, 68, 69, 207. **Coverage:** Core procedure integrated; special cases reviewed separately.

**Consider:** Can a leader rally first and then help the broken units in this Location?

- Where legal, resolve the broken leader before units that depend on that leader. A leader cannot apply its own leadership modifier to itself.
- If an eligible unbroken leader is present, choose the proper assisted procedure rather than assuming Self-Rally is allowed.
- A leader who successfully rallies may then rally other broken units in the Location during this RPh. Respect transport and other restrictions on whom it may help.
- A10.72 makes an applicable non-zero leadership modifier mandatory unless another present leader’s modifier is substituted. The choice to attempt an ordinary Rally is distinct from choosing its modifiers.
- Passenger leadership is restricted under D6.651. Show its vehicle and CE/halftrack conditions; an Armor Leader is not a general passenger Rally leader.

**Checklist behavior:** Show a dependency: leader rally → assisted rally choices become available. Do not mark the leader “used up” merely because its own Rally succeeded.

**Illustration:** Broken leader + two broken squads / Leader rallies successfully / Both squads gain their individual leader-assisted Rally choices.

**Terms:** SMC, ML, DR, DRM, Location, Good Order.

#### Choose the first MMC Rally attempt

Catalog ID: `first`. **Who:** ATTACKER. **Type:** Expiring opportunity.

**Rules:** A10.63; A18.11; A25.71; G1.62. **PDF pages:** 678, 68, 85, 97, 280. **Coverage:** Core procedure integrated; documented edge-case review needed.

**Consider:** Do you want the first MMC attempt to use the special Self-Rally opportunity?

- Identify the eligible MMC before making another MMC Rally attempt. The ordinary opportunity is tied to the first attempt, not a spare action saved until later.
- Check absence of a Good Order leader, Disrupted status, and any scenario-specific exceptions. Distinguish this opportunity from a unit’s inherent Self-Rally capability.
- Preview +1 Self-Rally, DM, terrain, and other applicable effects. On a qualifying Original 2, branch to Field Promotion rather than normal HOB.
- Finnish units are not eligible for Field Promotion under A25.71; Japanese cannot create leaders under G1.62. Do not show an executable leader-creation offer merely because Original 2 was rolled.

**Checklist behavior:** Before another MMC rolls, show a quiet last-chance reminder. Choosing another MMC deliberately closes this opportunity under the ordinary rule.

**Illustration:** Example: broken morale 7, DM, woods, special Self-Rally. +4 DM +1 Self-Rally −1 terrain = +4. Final DR must be ≤7, so Original DR ≤3 succeeds; an Original 2 opens the special leader-creation procedure.

**Terms:** MMC, SMC, DR, DRM, DM, ML, Field Promotion, Disrupted.

#### Use inherent Self-Rally capability

Catalog ID: `self`. **Who:** Both sides where eligible. **Type:** Optional choice.

**Rules:** A10.6, .63, .71; A25.7; A19.12; W2.12. **PDF pages:** 678, 68, 69, 86, 97, 659. **Coverage:** Core procedure integrated; nationality coverage varies.

**Consider:** Which units can rally without assistance even when the special first-MMC opportunity is unavailable?

- Identify the broken-side Self-Rally capability and applicable leader, crew, and nationality cases. Finnish Conscript exceptions must remain distinct.
- Apply the no-Good-Order-leader restriction and the +1 Self-Rally modifier, subject to explicit exceptions.
- Keep each unit’s single Rally attempt for the Player Turn.
- Disrupted units cannot Self-Rally under A19.12, including the inherent-capability path. Finnish Conscript exclusions and Ranger eligibility remain separate.

**Checklist behavior:** The DEFENDER is not given the ATTACKER’s ordinary first-MMC privilege, but still sees eligible inherent Self-Rally choices.

**Illustration:** Broken crew with Self-Rally capability / Its own choice remains visible even after the first MMC opportunity has been spent elsewhere.

**Terms:** MMC, SMC, ML, DRM, Good Order.

#### Rally units with a leader

Catalog ID: `assisted`. **Who:** Both sides. **Type:** Optional; compulsory under some rules.

**Rules:** A10.6–.71. **PDF pages:** 678, 68, 69. **Coverage:** Core procedure integrated; special cases reviewed separately.

**Consider:** Which eligible broken units should this leader attempt to rally?

- List one row per eligible unit, not one row per stack. Check the leader’s existing RPh commitments.
- Preview broken-side morale, applicable leadership, DM, and the terrain bonus. “In cover” is not a substitute for the actual qualifying terrain list.
- A leader can assist multiple broken units in the same Location. Each unit gets only one Rally attempt per Player Turn.
- Apply mandatory leadership under A10.72, including a positive modifier. Do not offer a switch to ignore an inconvenient eligible leader.

**Checklist behavior:** Show Attempted: failed separately from Not attempted. A failed attempt does not leave a misleading unchecked task.

**Illustration:** Squad A: attempted, failed. Squad B: not attempted. Leader: still eligible to assist Squad B under the same rally activity.

**Terms:** MMC, SMC, ML, DM, DR, DRM, Location.

#### Apply Commissar rally duties

Catalog ID: `commissar`. **Who:** Side with the Commissar. **Type:** Required when applicable.

**Rules:** A25.221–.222. **PDF pages:** 678, 94. **Coverage:** Use specific rule coverage, not ordinary-leader assumptions.

**Consider:** Does this Commissar have compulsory Self-Rally or rally duties?

- A broken Commissar must attempt Self-Rally, even with another leader present.
- Apply the Commissar’s morale and DM treatment when rallying other units. Do not stack ordinary leadership benefits onto it.
- Preview replacement, reduction, or elimination consequences for failed attempts under the precise unit rules.

**Checklist behavior:** This is a mandatory procedure with material risks, not an ordinary optional “use leader” item. Marking it considered cannot clear it.

**Illustration:** Commissar present / Required rally attempts listed / Failure consequences visible before resolution.

**Terms:** Commissar, MMC, SMC, HS, DM, ML.

#### Resolve Rally results and follow-ups

Catalog ID: `results`. **Who:** Affected side. **Type:** Triggered requirements and choices.

**Rules:** A10.64; A15.1; A17; A18.11; A10.2; E3.742; G13.447. **PDF pages:** 678, 68, 83, 84, 85, 231, 311. **Coverage:** Coverage follows the triggered rule.

**Consider:** Did the actual roll create another required effect or decision?

- Normal Fate is Original Rally DR 12, causing Casualty Reduction. Extreme Winter E3.742 extends this to 11 or more for its date/nationality conditions outside buildings/pillboxes; use the separate winter row.
- A qualifying non-Self-Rally Original 2 can trigger HOB, with its exclusions. A special first-MMC Self-Rally Original 2 uses the Field Promotion procedure.
- Resolve dependent leader-loss effects only when triggered. Do not trigger a Sniper merely because an ordinary Rally DR equals SAN.
- Apply any resultant state effects, including the G13.447 CX requirement when a unit rallies in shallow Ocean under Heavy Surf.

**Checklist behavior:** The attempted unit is not finished until its actual consequences settle. Do not prepopulate optional “try HOB” or “try Field Promotion” actions.

**Illustration:** Rally roll → qualifying result → required follow-up or offered leader-creation choice → resolved record. Ordinary Self-Rally is not an HOB route.

**Terms:** HOB, Field Promotion, Fate, DR, dr, LLMC, LLTC, SAN.

#### Apply campaign first-two Self-Rally allowance

Catalog ID: `campaign`. **Who:** ATTACKER when the CG rule applies. **Type:** Conditional procedure.

**Rules:** ASOP 1.23B: ABtF CG14; BRT CG14; KGP CG18; RB CG17. **PDF pages:** 678. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the campaign grant the first two MMC attempts the special allowance?

- Keep ABtF CG14, BRT CG14, KGP CG18 and RB CG17 as individually identified scenario-rule references.
- The supplied ASOP names these exceptions, but their full campaign rules are not in this rulebook PDF. Show Source text needed and Rules integration pending.
- Do not infer that every campaign, nationality or module gets two attempts.

**Checklist behavior:** Display the named exception now; require the actual scenario rule before evaluating its complete eligibility and consequences.

**Illustration:** Apply campaign first-two Self-Rally allowance / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** CG, MMC, SSR.

#### Apply nationality and unit-class exceptions

Catalog ID: `nationality`. **Who:** Affected side. **Type:** Conditional procedure.

**Rules:** A25; G1.2, G1.41, G1.62; G17.11; G18.31; W2.12; W3.32; W7.27-.31. **PDF pages:** 678, 94, 97, 278, 280, 319, 322, 659, 660, 663. **Coverage:** Rules integration pending; reference available.

**Consider:** Which force-specific rule changes this Rally or reorganization choice?

- Japanese leaders use their Commissar-like rally treatment, except inherent Armor Leaders; Japanese HS recombine into the specified full-strength squad. Japanese leader creation is excluded.
- Red Chinese Commissar failed-rally penalties differ from Russian Commissars. Fate still applies.
- CPVA Political Officers use +2 DM rather than immunity to DM and do not apply the Russian failed-rally replacement/reduction/elimination penalty. They must be assigned in the OB.
- CPVA HS recombine into full-strength squads; CPVA infantry crews cannot Deploy and vehicle crews cannot Recombine.
- Finns and Korean War Rangers have their own Self-Rally and leaderless reorganization rules. USMC and KMC squad types have specific Deployment exceptions.
- Use A25 nationality, alliance and scenario morale/leadership adjustments on the actual unit; do not treat all Allied or all Axis units as interchangeable.

**Checklist behavior:** Attach these clauses to the affected Deployment, Recombination and Rally rows, with explicit pending coverage instead of a generic deferred-chapter card.

**Illustration:** Apply nationality and unit-class exceptions / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** MMC, HS, ML, DM, PO, CPVA, KMC.

#### Review morale, terrain and Rally modifiers

Catalog ID: `modifiers`. **Who:** Both sides. **Type:** Conditional procedure.

**Rules:** A7.7; A10.61-.62, .8; A17.2; B23.211, B23.83; F12.44; G3.11; G5.5; G11.1-.2. **PDF pages:** 678, 68, 69, 84, 135, 139, 270, 282, 284, 292, 294. **Coverage:** Rules integration pending; reference available.

**Consider:** What number must this particular unit roll, and why?

- Start from the actual broken-side morale, then account for wounds, Fanatic status, encirclement, nationality and scenario conditions.
- Show each applicable DRM separately: leadership, Self-Rally, DM and terrain. A cover graphic alone cannot determine the terrain bonus.
- Woods, buildings, pillboxes and trenches qualify under A10.61. Apply the cave/cave-complex and other terrain rules; rooftops, lumberyards, bamboo without a qualifying pillbox, collapsed huts and mausoleums do not automatically qualify.

**Checklist behavior:** Give an explainable calculation, with unknown or pending modifiers identified rather than silently zeroed.

**Illustration:** Review morale, terrain and Rally modifiers / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** ML, DM, DRM, Fanatic, Encircled, Wounded.

#### Apply Extreme Winter Fate

Catalog ID: `winterFate`. **Who:** Affected units. **Type:** Conditional procedure.

**Rules:** E3.742; A10.64. **PDF pages:** 678, 231, 68. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this attempt have an additional casualty risk on Original 11?

- In Extreme Winter, apply the extended Fate rule to non-Finnish Axis Personnel before April 1942 and Russians before April 1941.
- It applies outside a building or pillbox. Woods or trench Rally bonus does not exempt the unit.
- Preview the risk before the roll and resolve Casualty Reduction on Original Rally DR 11 or 12 when applicable.

**Checklist behavior:** A terrain bonus must not obscure this separate Original-dice consequence.

**Illustration:** Apply Extreme Winter Fate / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** DR, Fate, MMC, SMC.

### 1.24A · Parachute Drop Points

#### Determine parachute Drop Points

Catalog ID: `drop`. **Who:** ATTACKER on entry. **Type:** Required when due.

**Rules:** E9.12. **PDF pages:** 678, 242. **Coverage:** Rules integration pending.

**Consider:** Are parachute Wings entering this Player Turn?

- Determine the final Drop Point for each entering Wing under its procedure.
- Preserve secret predesignations and apply the random displacement process only where required.
- Place the associated parachute counters in their proper aerial positions.

**Checklist behavior:** This remains a distinct entry-timing task. It cannot be hidden just because no executable airborne workflow exists yet.

**Illustration:** Entering Wing / Drop Point determination due / Rules integration pending / Rule explanation available.

**Terms:** dr, SSR, OB.

### 1.31B · Shock and DM

#### Resolve Shock and UK recuperation

Catalog ID: `shock`. **Who:** Both sides. **Type:** Required when due.

**Rules:** C7.42. **PDF pages:** 678, 177. **Coverage:** Core procedure integrated.

**Consider:** Which AFVs are due for a recuperation roll at this RPh end?

- Track when each condition was imposed; only the AFVs due now enter the obligation list.
- For Shock, 1–2 removes it; 3–6 advances to UK. For UK, 1–3 removes it; 4–6 wrecks the AFV without a Crew Survival roll.
- Do not substitute this for Stun handling or offer infantry Rally modifiers.
- The due recuperation procedure still applies to an abandoned AFV. A further Shock result can reset UK to Shock; retain the condition’s timing history.

**Checklist behavior:** A real end-phase block identifies the specific AFV and due roll. Acknowledging the topic is not resolving the obligation.

**Illustration:** AFV 1 / UK recuperation due / Resolve dr. The obligation remains until the game records the result.

**Terms:** AFV, Shock, UK, CS, dr, Stun.

#### Remove or retain DM

Catalog ID: `dm`. **Who:** Both sides. **Type:** Cleanup with a conditional choice.

**Rules:** A10.62. **PDF pages:** 678, 68. **Coverage:** Partially integrated.

**Consider:** Would retaining DM preserve a useful rout option, and is retaining it legal here?

- Apply DM removal and continuing conditions correctly after the Rally attempts. Never remove DM early to improve that same RPh’s roll.
- Offer voluntary retention only under its terrain and overstacking exceptions.
- Explain the tradeoff: retained DM can preserve a later rout option but still affects subsequent rallying while present.

**Checklist behavior:** Use explicit Remove or Retain choices for eligible cases. Do not silently clear every DM marker or force the player to retain all of them.

**Illustration:** Broken unit in eligible open terrain / Retain DM for later rout? The panel explains the reason and leaves the tactical choice to the player.

**Terms:** DM, ML, Location.

### 1.32B · Wall Advantage

#### Claim Wall or Hedge Advantage

Catalog ID: `wa`. **Who:** Both sides; ATTACKER first. **Type:** Optional timing window.

**Rules:** B9.32–.323; B9.55. **PDF pages:** 678, 119, 122. **Coverage:** Rules integration pending.

**Consider:** Do any eligible units need to claim Wall/Hedge Advantage before Prep Fire?

- Check the terrain and enemy relationship and the eligibility restrictions, including Pinned, TI, and Immobile conditions.
- Preserve ATTACKER-first claiming order. Separate voluntary claiming from mandatory gain or loss under other circumstances.
- Claiming or forfeiting WA does not consume the unit’s ordinary RPh action allowance.
- Account for bocage LOS and concealment effects under B9.55 when WA changes. Do not grant normal CCPh concealment gain just because this is the RPh.

**Checklist behavior:** Display this even if every rally attempt is complete. It is an easily missed end-of-phase opportunity, not a rallying modifier.

**Illustration:** Wall/Hedge Advantage / End-RPh window / Rules integration pending / Mark considered. No claim is written into the game.

**Terms:** WA, TI, Location.

### Additional timing and standing choices

#### Settle artillery continuation

Catalog ID: `oba`. **Who:** Affected battery owner. **Type:** Required when applicable.

**Rules:** C1.21–.22; C1.34. **PDF pages:** 163, 682, 685. **Coverage:** Check the battery-specific integration path.

**Consider:** Does an FFE:C state require Battery Access or mission cleanup now?

- For the relevant FFE:C battery, apply end-RPh loss of Battery Access under C1.21; if Radio Contact is not present, apply the FFE:C cancellation under C1.22. Preserve the distinction between Access and Contact.
- Apply the appropriate loss of Access or mission cancellation; do not assume every OBA mission ends in Rally.
- Calling, correcting, and maintaining contact in the ordinary fire-phase procedure belong in those phases, not on a generic Rally action button.

**Checklist behavior:** Show the affected mission and the automatic consequence. The phase cannot be declared clear by a manual checklist tick.

**Illustration:** Battery with FFE:C / End-RPh consequence / Details explain which access or mission state changes and why.

**Terms:** OBA, FFE:C, SSR.

#### Plan ammunition replenishment

Catalog ID: `ammo`. **Who:** Eligible vehicle owner. **Type:** Cross-phase commitment.

**Rules:** E10.3–.31. **PDF pages:** 244. **Coverage:** Rules integration pending.

**Consider:** Should the vehicle and an Ammo Vehicle commit the complete turns required to replenish Low Ammo?

- Check eligible Low Ammo status, the Ammo Vehicle, and the same or Accessible Locations.
- Account for the required complete Game Turns, RPh through CCPh for both Player Turns. Show the commitment before time in the turn is spent elsewhere.
- Track the applicable replenishment period and interruption conditions; do not offer instant repair or a generic Rally roll.

**Checklist behavior:** This is a duration commitment visible at the start of a turn, not an ordinary Rally attempt. A reminder tracks when the required period can complete.

**Illustration:** Low Ammo / Compatible Ammo Vehicle nearby / Required period and TI consequences / Rules integration pending.

**Terms:** AFV, TI, B#, Location.

#### Apply standing concealment and Guard choices

Catalog ID: `other`. **Who:** As the individual rule permits. **Type:** Context-dependent.

**Rules:** A12.14; A12.33; A20.5; B9.55; scenario SSR. **PDF pages:** 77, 80, 87, 122. **Coverage:** Separate content entries as applicable.

**Consider:** Does a standing choice or an SSR create another decision during Rally?

- Voluntary concealment loss remains a standing choice under A12.14; show its consequences without treating it as ordinary RPh concealment gain.
- Check mandatory revelation of a hidden Gun in non-Concealment Terrain under A12.33, including the start of the first RPh. Apply bocage exceptions correctly.
- An unbroken Guard squad can Deploy automatically at any time under A20.5. Do not hide that option because the side is DEFENDER or 1.21A has passed. Prisoner custody transfer or abandonment keeps its friendly-RPh restriction.
- Scenario-specific procedures need individually named rows with their rule text. Unknown SSR relevance remains visible; it is not assumed inapplicable.

**Checklist behavior:** An “all options” view includes these contextual choices. The live checklist expands only the ones whose timing and disclosure can be established.

**Illustration:** Scenario-specific reinforcement condition / Source SSR shown / Required, optional, or not yet evaluated stated separately from integration status.

**Terms:** SSR, HIP, Good Order.

#### Check a triggered Field Phone cut

Catalog ID: `phone`. **Who:** Affected phone owner. **Type:** Conditional procedure.

**Rules:** C1.23. **PDF pages:** 163. **Coverage:** Rules integration pending; reference available.

**Consider:** Did an opposing unit’s Original 2 DR trigger the phone-security rule?

- Check the actual opposing unit and the Field Phone security area when an Original 2 is rolled for any purpose. Rally and Deployment can therefore matter.
- Resolve the cut only if all source conditions hold. A Field Phone cannot be repaired or moved; a radio’s repair option is not applicable.

**Checklist behavior:** Attach this consequence to the triggering roll without exposing a hidden phone or protected security information.

**Illustration:** Check a triggered Field Phone cut / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** DR, OBA, HIP.

#### Commit to an Ammo Dump or ammo transfer

Catalog ID: `ammoTransfer`. **Who:** Owning side. **Type:** Conditional procedure.

**Rules:** E10.4; E10.6. **PDF pages:** 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the Ammo Vehicle offload, or should an immobile Ammo Vehicle transfer its supply?

- Use the eligible offload or accessible-vehicle transfer conditions. The prescribed commitment is four complete Game Turns.
- Track the replenishment-style TI conditions and the end-CCPh completion. Apply vehicle movement restrictions in the turn it gains or loses Ammo Vehicle status.

**Checklist behavior:** This is a multi-turn commitment to consider before spending the turn, not instant RPh SW Transfer.

**Illustration:** Commit to an Ammo Dump or ammo transfer / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** TI, AFV, OB.

#### Verify incoming Stun and Recall state

Catalog ID: `stunBoundary`. **Who:** Affected vehicle owner. **Type:** Conditional procedure.

**Rules:** D5.34-.341; Vehicle reference charts. **PDF pages:** 203, 221, 704. **Coverage:** Rules integration pending; reference available.

**Consider:** Was the previous Player Turn’s Stun transition applied?

- D5.34 places the normal Stun flip at Player-Turn end; the quick charts describe the beginning of the next RPh. Treat this as one boundary transition, never two.
- Show a reminder or reconciliation issue if the imported state still has an overdue Stun marker. Recall follows its own conditions, without a Shock recuperation roll.

**Checklist behavior:** The end-state engine owns the transition. The Rally guide can expose an overdue boundary obligation without relocating the rule.

**Illustration:** Verify incoming Stun and Recall state / Check scenario and subject / Pending integration remains visible; consideration does not perform this procedure.

**Terms:** Stun, Recall, AFV.

### Actions belonging elsewhere

#### Common actions that belong elsewhere

Catalog ID: `notHere`. **Who:** Reference. **Type:** Timing clarification.

**Rules:** A4.43; A9.73; A9.8; A3.2; D5.41; D5.34; C1.22. **PDF pages:** 49, 65, 68, 203. **Coverage:** Reference guidance.

**Consider:** Are you looking for an action that is not normally a Rally Phase choice?

- Assembling or dismantling a weapon normally belongs to PFPh or DFPh, although a qualifying weapon may be repaired while already dismantled.
- Ordinary entrenching belongs to PFPh. Voluntary vehicle abandonment is an MPh action.
- Ordinary dropping of weapon possession and deliberate weapon destruction have their own permitted phases; neither is a generic RPh option.
- Do not reset CX or call an OBA mission merely because this is Rally. Verify an incoming Stun boundary transition separately from Shock recuperation.
- Voluntary breaking belongs to its Rout timing. Pre-game Commissar substitution, initial weather tables and pre-game barrage planning are setup tasks, not repeatable Rally actions.

**Checklist behavior:** Explain where the option belongs and offer a future-phase bookmark in the real component. Avoid filling the Rally checklist with invalid action buttons.

**Illustration:** Assemble MMG / Available in a later fire phase under A9.8 / Future reminder, not an RPh execution control.

**Terms:** dm, DM, Stun, OBA, CX.

## Contextual vocabulary

These labels link to the Acronym Index where an exact catalog identity exists. Expansion text below is contextual guidance, not a replacement acronym catalog. Keep DR/dr and DM/dm distinct. Use Good Order in full; the catalog’s equipment GO means something else. New labels need identity validation before Blazor wiring.

| Term | Meaning in this checklist |
|---|---|
| AAMG | Anti-Aircraft Machine Gun. Its repair can require a CE crew or a qualifying Hero Rider. |
| AFV | Armored Fighting Vehicle. |
| ASOP | Advanced Sequence Of Play. Its ordered windows determine when the listed activities occur. |
| ATTACKER | The side whose Player Turn this is, regardless of its scenario mission. |
| B# | Breakdown Number. In the vehicle ammunition context, check the separate Low Ammo rules rather than treating every failure as a repairable malfunction. |
| BU | Buttoned Up. Check each weapon’s repair requirements; do not demand a CE crew for every repair. |
| CA | Covered Arc. The owner can select it when a Gun is unloaded at the packing midpoint. |
| CE | Crew Exposed, the vehicle crew meaning. Link main:CE rather than the equipment entry with the same form. |
| CG | Campaign Game. Named campaign rules can change the first-MMC Self-Rally allowance. |
| Commissar | A special leader whose compulsory rally procedures and failure consequences differ from ordinary leadership. |
| CPVA | Chinese People’s Volunteer Army. Chapter W adds specific unit and Political Officer rules. |
| CS | Crew Survival, in this context. A wreck caused by failed UK recuperation does not grant a CS roll. |
| CX | Counter Exhaustion. It can affect a Recovery dr. This is not a generic Rally Phase cleanup marker. |
| DD | Duplex Drive tank. Its entry preparation is a special reinforcement case. |
| DEFENDER | The other side during this Player Turn. It still has Rally Phase choices. |
| Disrupted | A condition that prohibits Self-Rally, including both inherent and special first-MMC paths (A19.12). |
| dm | Dismantled weapon. It is unrelated to DM. Eligible weapons can be repaired while dismantled; assembling them is not an ordinary RPh action. |
| DM | Desperation Morale. Normally +4 to a Rally DR; removal or permitted retention is an end-of-RPh matter. |
| dr | Die roll, using one die. Recovery, repair, and Shock recuperation use dr rather than DR. |
| DR | Dice roll. The Rally DR uses two dice; Original means before modifiers. |
| drm | Die Roll Modifier. Preserve lowercase; it is not the same label as DRM. |
| DRM | Dice Roll Modifier. Apply only those allowed for the particular action. |
| DYO | Design Your Own scenario. Its reinforcement and continuing weather rules can add Rally checks. |
| EC | Environmental Conditions. Weather changes can affect this category and other rule procedures. |
| Encircled | A condition with morale and other effects; include its applicable penalty in the actual Rally calculation. |
| Fanatic | Status that increases normal and broken morale by one, with other rule-specific effects. |
| Fate | Normally Original Rally DR 12 causes Casualty Reduction; E3.742 extends the risk to 11 in qualifying Extreme Winter cases. |
| FFE:C | Fire For Effect Continuation. Check applicable end-RPh mission and Battery Access consequences. |
| Field Promotion | The special first-ATTACKER-MMC Self-Rally route can create a leader on an Original 2, under A18.11. |
| Good Order | A rules-defined condition, not merely “unbroken.” Check the complete rule and action-specific restrictions. Display the full term; equipment:GO means something else. |
| HIP | Hidden Initial Placement. The guide must not disclose an opponent’s hidden unit through prompts or counts. |
| HOB | Heat of Battle. A qualifying Original 2 on a non-Self-Rally attempt can trigger it; normal Self-Rally does not. |
| HS | Half-Squad. Two qualifying HS can recombine under A1.32. |
| KMC | Korean Marine Corps. Specific squad types have leaderless Deployment permissions. |
| LC | Landing Craft. Loading and entry conditions require their own manifest and scenario rules. |
| LLMC | Leader-Loss Morale Check. Resolve only when an event meets its trigger and qualifications. |
| LLTC | Leader-Loss Task Check. Keep distinct from LLMC and from the original Rally attempt. |
| Location | A hex can contain multiple Locations. Same hex does not necessarily mean a leader can help or a transfer is legal. |
| LV | Low Visibility. Fog, precipitation, and night have distinct effects; they are not one interchangeable modifier. |
| M# | Manhandling Number. Also used in the Animal-Pack period calculation. |
| MA | Main Armament. |
| ML | Morale Level. Rally normally uses the morale printed on the broken side, with applicable adjustments. |
| MMC | Multi-Man Counter: squad, half-squad, or crew in counter form. |
| MTR | Mortar. Animal-Pack timing distinguishes mortars from other Pack Guns. |
| NTC | Normal Task Check. Used for ordinary Deployment, with the applicable leadership modifier. |
| NVR | Night Visibility Range. The night change procedure uses the Wind Change dice under E1.12. |
| OB | Order of Battle: the forces provided by the scenario. |
| OBA | Offboard Artillery. RPh end housekeeping is not a general invitation to call or correct a mission here. |
| Pack-TI | Animal-Pack task status under G10.11, distinct from ordinary TI and with separate loss and duration rules. |
| PO | Political Officer in the CPVA rules, with Rally behavior distinct from a Russian Commissar. |
| PP | Portage Points. Useful when deciding who should possess a weapon before movement. |
| R# | Repair Number in this context. The same printed form can mean something else on vehicle counters. |
| Recall | Vehicle withdrawal obligation. It can result from armament disablement or crew conditions; it is not repaired by Rally. |
| RPh | Rally Phase. Each Player Turn begins with one; both sides have activities here. |
| SA | Secondary Armament as specifically defined in the vehicle listing, not a synonym for every extra MG. |
| SAN | Sniper Activation Number. Not every DR is a Sniper trigger; ordinary Rally rolls do not activate one merely by matching SAN. |
| Shock | An AFV incapacitation state with a scheduled recuperation dr; it is not infantry rallying or Stun. |
| SMC | Single Man Counter: a leader or hero. Being an SMC does not by itself confer leadership. |
| SMOKE | ASL smoke effects, including WP where applicable. Weather handling and placement timing are separate procedures. |
| SSR | Scenario Special Rule. Read its conditions and timing before applying general procedures. |
| Stun | A vehicle crew condition distinct from Shock. Its normal end-Player-Turn transition is not a Shock recuperation roll. |
| SW | Support Weapon. Possession, recovery, repair, and transfer are different procedures. |
| TI | Temporarily Immobilized. Scrounging imposes this even if unsuccessful, restricting later activities. |
| UK | Unconfirmed Kill. An AFV that fails its first Shock recuperation enters this condition. |
| WA | Wall/Hedge Advantage. Claiming it at the end of RPh has its own eligibility and ordering. |
| Wounded | SMC condition affecting morale and leadership. Do not read only the printed unwounded values. |
| X# | Nonrepairable breakdown number. A permanently lost weapon is not a candidate for an ordinary repair attempt. |

## Implementation handoff and acceptance

The [checklist catalog](ASL%20Rally%20Phase%20Checklist%20Catalog.json) carries stable identities, timing groups, source pages, terms and prompt text. The walkthrough and preview were generated from that same catalog for this reconciliation. It is design content, not a legal-action engine. Existing integration labels are inherited from the repository review; newly detailed cases conservatively say pending until mapped to verified action paths.

Before production, bind every option to the capability inventory and exact acronym identities; implement scenario applicability without suppressing unknowns; use typed rule and ASOP links; and validate with representative positions. Acceptance examples include first-MMC opportunity expiration, failed attempts, successfully rallied leader assistance, mandatory positive leadership, winter Fate in woods, Finnish/Japanese leader-creation exclusions, CPVA PO DM, both Animal-Pack midpoint timings, shared weather dice, external campaign source gaps, and a single Stun boundary transition.

No Rules behavior was changed or claimed tested by this document update.
