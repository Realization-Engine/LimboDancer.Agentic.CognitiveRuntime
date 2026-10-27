# ASL Cross-Phase Dependencies and Cleanup Audit

Date: 2026-10-26. Target: ASL.Rules after all planned passes. This is a source reconciliation and component design, not an engine implementation.

## Evidence and limits

Re-read eASLRB v3.01 ASOP physical PDF pages 678-681 and the core and special-rule passages cited below. The local source is `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. Page numbers below are physical, one-based. Existing phase catalogs supply the linked option inventories and Pass 71 integration baseline. No new engine coverage is claimed.

The audit covers the ordinary eight-phase boundaries, owner-relative and next-friendly-phase clocks, complete-Game-Turn work, immediate consequences and named Chapter E/G/W exceptions represented in the phase guides. Historical module/SSR/equipment combinations still require their source profiles. A source gap, pending implementation and a disallowed action remain separate findings.

## Findings and design corrections

The 538 phase topics lacked a navigable dependency record linking the originating choice to later effects. The new catalog adds 38 linked timing chains without inventing a ninth phase. The preview has a cross-phase timing selector and contextual links on participating topics. Opening another phase preserves each phase’s consideration state. No link resolves a game action.

Specific gaps now made explicit include CX release events and the surviving Double Time prohibition; independent source/drift smoke cleanup; next-RPh Shock versus end-turn Stun; late-phase Trip Flares surviving into the next AFPh; interruption of long-duration work; compulsory tunnel emergence; airborne content release; and subject-specific Blaze, drift and Drowning deadlines. Existing Flame Clearance wording is expanded to show Hamper as an outcome, and generic Pin/TI cleanup explicitly defers special-duration markers.

The earlier Close Combat walkthrough incorrectly said that rule-specific timing overrides ASOP. The ASOP introduction expressly gives ASOP precedence for ordering conflicts, while preserving normal restrictions. The walkthrough is corrected. G1.421 is retained as a restriction on eligibility within the CC sequence. The MPh prisoner-abandonment permission tension remains explicitly unresolved; it is not silently converted into a timing override.

## Boundary coverage

| Boundary | Finish or evaluate here | Carry forward |
|---|---|---|
| RPh to PFPh | Rally before DM removal; Shock/UK recuperation; due FFE:C consequences; WA | Gusts and wind result, legal retained DM, packing/replenishment progress, repair/depletion state |
| PFPh to MPh | Eligible end-fire CA/AA choices before the next phase; owner-specific smoke opening already applied | Prep/Opportunity Fire and Spotter commitments, labor/TI, fire-dependent vehicle/PRC restrictions |
| MPh to DFPh | Complete each expenditure/response and unit move; landing and Blaze/wreck-removal obligations; remove residual and ordinary half-inch smoke | First/Final/IF/No Fire use, qualifying movement history, cave smoke, Placed DC, Clearance and smoke-dispenser attempts |
| DFPh to AFPh | End-fire CA/AA choices before daytime First/Final marker removal | Night Gunflash Location evidence, future Clearance, Hampered Flame, Opportunity Fire, airborne and vehicle histories |
| AFPh to RtPh | Required attacks/DC results; ordered Blaze/Flame spread; specified fire and due-flare cleanup | Blaze escape obligations, Pin/TI, DM, longer-lived illumination and smoke, depletion and weapon history |
| RtPh to APh | Each side's routs, surrender and immediate consequences; Infantry Blaze deadline | Legal surviving unit/load states, DM, later-phase flares, tunnel/airborne obligations with their correct deadlines |
| APh to CCPh | Mandatory emergence, drift, hazards, Drowning and parachute release | CC entry/origin/concealment history, PAATC results, CX, T-H target, illumination and Acquisition |
| CCPh to next RPh | Every Location, then aerial combat, non-Flame work, ordinary status cleanup, concealment, night cleanup and other due expiries | Per-unit Melee, +1/Recall, owner-relative smoke, later-phase flares, next-RPh Shock/UK tests, continuing tasks and lasting damage |
| Second CCPh to next Game Turn | Count completed Game-Turn work exactly once | Packing release scheduled at next 1.13B; interrupted intervals do not count; stable side identities survive role reversal |

The table is an index into the cited chains below, not an additional universal cleanup order. Within each phase, preserve printed ASOP order, normal restrictions and the exact event that owns each consequence. The existing phase catalogs retain action families that do not create cross-phase commitments.

## Required live dependency record

Each obligation needs a stable ID; source event and rule; affected subject and owner side; creation Game Turn, Player Turn and phase; exact due event or ASOP step; prerequisites; completion and interruption predicates; source/provenance dependencies; visibility audience; integration capability; and resolution evidence. A Game Turn counter must not be derived from the number of phase transitions. Preserve history needed by later rules after removing display counters.

Keep reference consideration separate from live available, declared, committed, due, resolved, cancelled, inapplicable, pending-integration and missing-source states. A live phase-close proposal should identify due obligations, retained statuses and queued future effects. Resolve earlier obligations before evaluating the next window, and never let a player acknowledgement manufacture eligibility or discard a required consequence. Deferred rules can show a reference explanation without pretending the engine evaluated their legality.

Persist and replay obligation completion exactly once. Loading a saved game at a boundary must neither skip nor repeat Stun flips, smoke dispersal, work completion or concealment checks. Hidden origins and subjects must stay on the authorized projection; client-side filtering of a full hidden queue is insufficient.

## Reconciled timing chains

### CX survives the turn, but has several release events

**Origin:** Double Time, difficult advance or another rule gives a unit CX.

**Due:** Breaking, or the applicable event in its next friendly Player Turn under A4.51; not ordinary end-CCPh cleanup.

**Sources:** A4.51-.52; A4.72. Physical PDF 51, 52.

- Keep the unit and creation turn. CX still affects intervening attacks, Ambush, CC, Recovery and difficult advance.
- In its next Player Turn, remove at the first applicable event: completion of all Prep Fire, Opportunity Fire designation, movement, becoming TI, or conclusion of its MPh. Apply A4.51’s start-of-MPh removal and retain the prohibition on Double Time that MPh.
- Do not remove CX before its final Prep Fire attack. Preserve prior-CX history after removing the counter, and distinguish new CX caused by Minimum Move or Deep Stream Entry.

**Example:** A squad made CX in APh stays CX through CC and the opposing turn; removing its old CX in its next MPh does not let it Double Time again.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:doubleStart`, `mph:lateDouble`, `pfph:fireOrHold`, `pfph:opportunity`, `aph:difficult`, `ccph:ambushModifiers`, `ccph:pinTi`.

### DM cleanup follows Rally attempts

**Origin:** Breaking and specified subsequent attack, adjacency or Rout-start conditions create DM.

**Due:** End of each RPh, after Rally attempts, with voluntary-retention and continuing conditions.

**Sources:** A10.62; ASOP 1.31B, 6.11B. Physical PDF 68, 678, 680.

- Carry DM into the Rally roll; do not clear it on opening Rally.
- Retain the legal terrain/overstacking choice and re-evaluate the conditions that impose DM. Rout permission and compulsory rout are separate findings.
- A later qualifying attack or Known-enemy condition can impose DM again; a previous cleanup does not immunize the unit.

**Example:** A broken unit cannot improve its current Rally attempt by clearing DM early, but may legally retain DM to preserve a later rout option.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:dm`, `rph:modifiers`, `rtph:dm`, `rtph:optionalRout`.

### Shock and UK use the next Rally, not Stun cleanup

**Origin:** A vehicle receives Shock or has an existing UK counter returned to Shock.

**Due:** End of the next RPh; UK then has its own next-RPh recuperation test.

**Sources:** C7.42; ASOP 1.31B. Physical PDF 177, 678.

- Record the next eligible RPh and the current Shock/UK state. It is not restricted to the owner’s Rally.
- Shock recuperates on 1-2, otherwise becomes UK; UK recuperates on 1-3, otherwise wrecks without Crew Survival. A new Shock result can reset UK to Shock.
- Do not erase the vehicle’s earlier movement history when Shock halts it. No Stun flip or generic end-turn removal substitutes for recuperation.

**Example:** A shocked moving tank stops immediately, retains applicable moving-target history, and waits for the next Rally recuperation window.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:shock`, `dfph:targets`, `ccph:stun`.

### Stun flips once and preserves its lasting penalty

**Origin:** An eligible crew suffers Stun or Recall.

**Due:** Normal Stun flips at the end of that Player Turn, ASOP 8.43B; Recall and cumulative exceptions remain separate.

**Sources:** D5.34-.341; ASOP 8.43B; G12.111; G14.33. Physical PDF 203, 204, 681.

- Keep the persistent +1 and the identity of the affected crew, including when it abandons and later re-crews a vehicle.
- Do not restore CE as a side effect of flipping Stun. Apply D5.33 in a subsequent Player Turn.
- An incoming-RPh reminder must not execute the same boundary transition twice. Shock, Recall and LC/LVT exceptions use their own rules.

**Example:** The next Rally sees a +1 marker, not a fresh Stun recovery roll or automatic restoration of CE.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `ccph:stun`, `rph:stunBoundary`, `mph:recall`, `aph:crewExposure`.

### Pin and ordinary TI survive until after combat and work

**Origin:** A unit is pinned or becomes TI; a terrain or vehicle procedure uses a Pin marker for a different purpose.

**Due:** Ordinary Pin/TI removal at 8.43B, after Location combat and Clearance; earlier break and special-duration rules still apply.

**Sources:** A4.8; ASOP 8.42B-.43B; E10.3; G10.11; G12.68. Physical PDF 52, 244, 291, 308, 681.

- Pinned/TI units do not regain APh or CC capabilities retroactively when the counter is removed.
- Breaking removes ordinary TI under A4.8, but must also invalidate any work whose eligibility has been lost.
- Keep Animal-Pack, replenishment and LC-Blaze Pin durations separate. A counter’s printed label alone is not its expiry rule.

**Example:** A pinned squad cannot advance merely because Pin will disappear later in the same Player Turn.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `aph:eligibility`, `ccph:pinTi`, `ccph:clearance`, `ccph:lcBlazePin`.

### MPh fire commitments survive into DFPh

**Origin:** A defender fires during an expenditure, leaving unit/weapon fire states and possibly residual.

**Due:** Remove residual at MPh end; carry fire eligibility into DFPh; ordinary First/Final counters leave at daytime DFPh end or night AFPh end.

**Sources:** A8.1-.4; ASOP 3.53B, 4.21D, 4.32D; E1.8. Physical PDF 59, 60, 61, 226, 680.

- Keep per-unit and per-weapon use, ROF and CA/direction commitments after each shot.
- Residual expiry does not restore a First-Fired or Final-Fired subject. A pass on one expenditure does not finish all defensive opportunities.
- At night retain the original Gunflash Location even when the firing unit moves away.

**Example:** The Fire Lane disappears at MPh end, while the MG’s earlier fire still governs its DFPh choices.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:firstFire`, `mph:fireLane`, `mph:cleanup`, `dfph:handoff`, `dfph:firstMarked`, `dfph:cleanup`, `afph:cleanup`.

### Opportunity Fire needs a preserved reservation

**Origin:** Eligible Infantry/Cavalry declares Opportunity Fire in PFPh.

**Due:** Use the reservation in AFPh, then remove its Bounding Fire marker at 5.33B.

**Sources:** A7.25; ASOP 2.22A, 3.22A, 5.22A, 5.33B. Physical PDF 55, 679, 680.

- Preserve the reason for the Bounding Fire counter: an Opportunity Firer is an explicit AFPh exception, not an ordinary spent firer.
- Prevent MPh movement and impermissible CA changes. Recheck actual AFPh eligibility without erasing the earlier commitment.
- Keep eligible ROF/IF and weapon/Spotter restrictions. Considered does not reserve or release a shot.

**Example:** A reserved mortar can use its special AFPh opportunity only through the preserved Spotter and firing conditions.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:opportunity`, `mph:eligibility`, `afph:opportunity`, `afph:rof`, `afph:cleanup`.

### A vehicle carryover shot depends on the whole MPh

**Origin:** A vehicle ends its MPh with retained Multiple ROF on a weapon.

**Due:** At most the specific D3.32 shot in its AFPh, if no other weapon including PRC fired in its MPh.

**Sources:** D3.32; C5.3. Physical PDF 200, 172.

- Retain all vehicle and PRC firing history, not just the selected weapon’s ROF.
- This is not ordinary Opportunity Fire or a full new ROF sequence; apply its one-shot limit and appropriate Motion/non-Motion treatment.
- Marker cleanup later cannot retroactively make the carryover legal.

**Example:** An MA with retained ROF loses this opportunity if another weapon or PRC fired during that MPh.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:bounding`, `mph:attackerAA`, `afph:carryover`, `afph:passengers`.

### Stopping and removing markers do not erase movement

**Origin:** A target moves, uses VBM, is in Motion, or a weapon/operator changes position.

**Due:** Keep the history for the rule’s Player-Turn scope; re-evaluate the narrower expenditure modifiers at each phase boundary.

**Sources:** C6.1-.17; C.8; ASOP 4.21D. Physical PDF 173, 174, 680.

- Do not apply MPh FFNAM/FFMO or J1/J2 as though DFPh were another expenditure response.
- Preserve qualifying Case J vehicle history after a Stop or Shock. Hazardous Movement has its own activity duration.
- Keep operator movement and weapon movement distinct for later AFPh eligibility.

**Example:** A stopped tank can still be a moving target in DFPh even though Infantry FFNAM no longer applies.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:endVehicle`, `dfph:targets`, `afph:targets`, `afph:movedWeapons`.

### A Placed DC creates a later attack obligation

**Origin:** A MPh placement becomes operably Placed after its expenditure and responses.

**Due:** Resolve the Placed DC in AFPh 5.22A, with the placement record’s applicable conditions.

**Sources:** A23.3-.4; ASOP 3.32A, 5.22A. Physical PDF 90, 91, 679, 680.

- Record placement success, target and relevant approach/attack facts before moving to another expenditure.
- Do not offer a second ordinary firing eligibility check as a substitute for resolving an operably Placed DC.
- Set DC and Thrown DC have different triggers and persistence; do not schedule every DC for AFPh automatically.

**Example:** The carrier becoming ineligible for ordinary AFPh fire does not erase an already operably Placed DC.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:placeDC`, `mph:setDC`, `afph:placed`, `afph:set`, `afph:thrown`.

### Non-Flame Clearance resolves after CC, not on declaration

**Origin:** An eligible MPh/DFPh declaration identifies participants, task and target; Flail has its own MPh declaration.

**Due:** Resolve the eligible non-Flame task at 8.42B before ordinary Pin/TI cleanup.

**Sources:** B24.7-.8; B28.7; G2.7; G9.71; ASOP 8.42B. Physical PDF 142, 143, 149, 282, 290, 681.

- Retain declaration and participation history, the exact task, its target/hexside, Labor and partial work.
- Loss of eligibility does not become harmless because TI or Pin is about to be removed. Recheck the task’s conditions before rolling.
- Do not open every task in DFPh: its printed list differs from MPh. Named O1.5/module profiles remain source-gated.

**Example:** A Wire attempt declared in DFPh is still pending while the player resolves every CC Location.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:clearDeclare`, `mph:flail`, `dfph:wire`, `dfph:clearDC`, `dfph:roadblock`, `dfph:path`, `dfph:panji`, `ccph:clearance`, `ccph:labor`.

### New and Hampered Flames need distinct origins

**Origin:** A Flame is created, or an immediate MPh/DFPh Flame Clearance attempt Hampers it.

**Due:** Exclude new/Hampered Flames from that Player Turn’s conversion; clear the appropriate Pin at CCPh cleanup.

**Sources:** B24.72-.721; B25.15-.151; ASOP 5.31B-.32B, 8.43B. Physical PDF 142, 143, 680, 681.

- Resolve Flame Clearance immediately. It does not wait with the non-Flame tasks until CCPh.
- A Final Fire Clearance DR of 3-6 Hampers the Flame for that Player Turn; store that cause separately from new-Flame age.
- Build the Blaze-spread queue before the Flame-conversion queue. A newly created Flame cannot cascade into a Blaze in the same AFPh.

**Example:** An unsuccessful extinction attempt can still prevent conversion this turn; showing only success or failure loses that result.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:clearFlame`, `dfph:flame`, `afph:blaze`, `afph:flame`, `ccph:pinTi`.

### Blaze escape deadlines depend on the occupant

**Origin:** Terrain becomes Blaze while occupied, or a glider/parachute lands there.

**Due:** Infantry: end of next RtPh. Vehicular/Cavalry units: next friendly MPh. Landing and entry have their own immediate/end-MPh consequences.

**Sources:** B25.4; ASOP 3.51A, 6.31B. Physical PDF 143, 680.

- Create a subject-specific obligation when Blaze appears, not only when Rout opens.
- Retain the Melee/Pin restrictions and the special escape procedure for qualifying units that cannot voluntarily break.
- Do not apply the Infantry deadline to every vehicle or exempt night units from the Blaze requirement.

**Example:** An AFPh terrain Blaze can require Infantry to escape in the immediately following RtPh, while a vehicle has a different clock.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `afph:blazeExit`, `rtph:blazeEscape`, `rtph:blazeEnd`, `mph:blazeEnd`.

### Ordinary smoke ages on its owner’s Prep Fire

**Origin:** Place a typed SMOKE effect with owner, source, size and full/dispersed state.

**Due:** White Dispersed SMOKE: owner’s next PFPh opening; then disperse remaining white SMOKE. Ordinary half-inch SMOKE: MPh end, with cave exception.

**Sources:** A24.4-.62; ASOP 2.11A, 3.53B, 5.11B-.13A; G11.85. Physical PDF 92, 93, 300, 678, 680.

- Do removal before dispersal, so newly dispersed counters are not removed in the same pass.
- Do not use the active player as a substitute for source ownership when expiring smoke.
- Keep Gusts, weather, drifting smoke, Napalm and cave smoke as distinct transition rules.

**Example:** Smoke laid by the defender is not removed just because the attacker begins Prep Fire.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:smokeCleanup`, `pfph:napalm`, `mph:smoke`, `mph:cleanup`, `afph:drift`, `ccph:caveSmoke`.

### Drift depends on its source, while Gusts act twice

**Origin:** Wind/force changes, a source disperses or disappears, or the Wind Change DR creates Gusts.

**Due:** Immediate drift invalidation when source/wind changes; eligible drift at first game RPh/each AFPh; Gusts smoke effects in RPh and again AFPh.

**Sources:** A24.61-.62; B25.651; ASOP 1.12A, 5.11B-.13A. Physical PDF 92, 93, 144, 678, 680.

- Remove dependent gray drift immediately when its source is removed or wind direction/force changes; source dispersal removes its furthest drift counter.
- Record the RPh Gusts event for later AFPh smoke and fire-spread effects. Do not reroll it or treat the RPh cleanup as the only effect.
- At AFPh follow 5.11 drift, 5.12 WP and 5.13 Gusts order; cave smoke uses its exceptions.

**Example:** A full-strength source becoming dispersed shortens its drift immediately, before the next routine drift placement window.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:wind`, `rph:smoke`, `pfph:smokeCleanup`, `afph:drift`, `afph:gustSmoke`, `afph:windSpread`.

### Night Gunflash is Location evidence, not illumination

**Origin:** A qualifying attack or another night procedure creates Gunflash evidence.

**Due:** Retain ordinary night fire markers through AFPh, then perform 5.33B cleanup; continuing Melee/FFE and switched-on searchlights follow their own sources.

**Sources:** E1.8-.89; W10.31; ASOP 4.32D, 5.33B. Physical PDF 226, 669, 680.

- Keep the original First/Final Fire Location even if the firing unit leaves it.
- Gunflash is not illumination. Removing a generic marker does not cancel a still-existing Melee/FFE or switched-on searchlight source.
- Do not make a hidden unit Known merely because its Location can be fired upon.

**Example:** A player can fire at a vacated Gunflash Location beyond NVR without knowing who, if anyone, now occupies it.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `dfph:cleanup`, `afph:cleanup`, `mph:searchlight`, `ccph:melee`.

### Concealment precedes night illumination cleanup

**Origin:** Starshells or IR illuminate a Location during the Player Turn.

**Due:** Assess ATTACKER concealment at 8.44A before 8.45B Starshell/IR removal and its Acquisition check.

**Sources:** E1.74; E1.923; E1.933; ASOP 8.44A-.45B. Physical PDF 225, 226, 227, 681.

- Keep illumination active while evaluating concealment eligibility; do not darken the map early.
- Apply the specific Acquisition retention test for Blaze/Flame illumination, rather than simply keeping every currently illuminated target.
- Trip Flares have a different expiry rule; Gunflash alone is not a substitute for illumination.

**Example:** A unit cannot claim concealment by using darkness that will exist only after the concealment window has closed.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:starshellOpening`, `dfph:mortarIR`, `ccph:concealmentGain`, `ccph:illumination`, `ccph:acquisition`.

### A Trip Flare’s activation phase determines expiry

**Origin:** A qualifying expenditure, entry or Search sets off a Trip Flare.

**Due:** MPh activation: end of that CCPh after concealment. RtPh/APh/CCPh activation: end of the next Player Turn’s AFPh.

**Sources:** E1.95-.951. Physical PDF 227.

- Store activation Player Turn and phase, not just an illuminated flag.
- The triggering expenditure is illuminated for its own consequences. Resolve activation before the appropriate response.
- A later-phase flare survives the current CCPh and the next Rally/Prep/Movement/Defensive Fire windows.

**Example:** A flare triggered by a rout must remain when the player presses End Player Turn.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:tripFlare`, `rtph:flare`, `aph:tripFlare`, `ccph:withdrawSpecial`, `ccph:tripFlareExpiry`, `afph:cleanup`.

### Cave smoke can expire after concealment and reveal connections

**Origin:** SMOKE is placed in a cave, with its own target and counter size.

**Due:** Half-inch cave SMOKE survives MPh end and expires at that Player Turn’s end; WP dispersal/removal also triggers the connection-disclosure test.

**Sources:** G11.85-.851. Physical PDF 300, 301.

- Do not drift cave smoke or apply ordinary Gusts cleanup to it.
- After concealment placement, expiring cave WP can reveal eligible higher connected caves and tunnel openings. Preserve source and connection identity.
- Reveal only what G11.851 permits; revealing a tunnel opening does not reveal a hidden pillbox.

**Example:** A cave WP grenade remains during Advance and CC, then its removal can create a new disclosure.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:cleanup`, `rph:smoke`, `afph:gustSmoke`, `ccph:caveSmoke`, `ccph:concealmentGain`.

### Artillery has mission, contact and entry clocks

**Origin:** A battery places SR/FFE and maintains separate Contact and Access states.

**Due:** Ordinary FFE:1 becomes FFE:2 after resolution; next friendly PFPh/DFPh resolution changes FFE:2 to FFE:C. FFE:C has fire-phase and end-RPh consequences.

**Sources:** C1.21-.22; C1.34-.35; C1.5-.51; ASOP 2.13A-.15A, 4.12D-.14D. Physical PDF 162, 163, 164, 165, 678, 679, 680.

- Entering/becoming more vulnerable in an existing HE/WP Blast Area during MPh/RtPh/APh/CCPh can cause a subject-only attack; it does not advance the mission counter or attack the whole hex.
- Retain the distinct end-RPh Access/Contact cleanup and next-friendly-fire-phase continuation decision. Involuntary contact loss is not automatic cancellation of every mission.
- Rocket, Barrage, NOBA and illumination schedules must use their own profiles rather than the ordinary HE cycle.

**Example:** A squad entering FFE:C in Advance can be attacked even though no new artillery mission is being called.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:ffe`, `dfph:ffe`, `rph:oba`, `pfph:continuation`, `dfph:continuation`, `mph:ffeMove`, `rtph:ffe`, `aph:ffe`, `ccph:withdrawHazards`.

### Acquisition persists until a defined loss event

**Origin:** A qualifying weapon acquires a unit or area.

**Due:** Track through movement, rout, advance and withdrawal; remove or transfer only on the specified weapon, operator, target or night events.

**Sources:** C6.5-.57; E1.74. Physical PDF 174, 175, 225.

- Record weapon identity, target type, acquired subject/Location and observer-safe LOS history.
- Movement, loss of possession/Good Order, malfunction, particular ammunition/fire choices and CA changes can invalidate Acquisition before any phase-end cleanup.
- Do not erase ordinary Acquisition at every phase boundary, or treat tracking as permission to fire during Rout/Advance/CC Withdrawal.

**Example:** A target that leaves LOS can leave its last visible Location acquired, while a different qualifying weapon event may remove Acquisition entirely.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `afph:acquisition`, `aph:acquisition`, `ccph:acquisition`, `pfph:caEnd`, `dfph:caEnd`.

### Ammunition replenishment counts complete Game Turns

**Origin:** Eligible Low Ammo vehicle and compatible supply commit to TI in the same/Accessible Locations.

**Due:** One, two or three complete Game Turns for B# at most 9, 10 or 11; completion at the applicable CCPh, followed by the supply depletion DR.

**Sources:** E10.2-.4; ASOP 8.43B. Physical PDF 244, 681.

- Count both Player Turns, RPh through CCPh inclusive. Show completed Game Turns and the currently incomplete interval.
- Loss of CE, expenditure of MP, firing or being attacked in CC interrupts as specified; the interrupted Game Turn does not count. Do not erase previously completed Game Turns without a rule requiring it.
- Ammo Dump/offload/transfer uses its separate four-Game-Turn procedure. Ordinary TI cleanup cannot complete either task early.

**Example:** A replenishing vehicle survives one CCPh with TI still present because its required complete Game Turn is unfinished.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:ammo`, `rph:ammoTransfer`, `ccph:ammo`, `ccph:pinTi`.

### Animal Packing finishes at a later Rally window

**Origin:** Place Animal-Pack at 1.13B of the initial RPh of a Game Turn.

**Due:** After the required complete Game Turns, remove it at 1.13B in the next RPh; midpoint loading/unloading and reversal have separate rules.

**Sources:** G10.11; G10.3-.31; ASOP 1.13B. Physical PDF 291, 292, 678.

- Count from first RPh through second CCPh, double the period at night, and preserve the rule’s minimum duration.
- Losing Pack-TI before the interval ends prevents that Game Turn from counting. Keep the Gun, Mule and crew as linked participants.
- Do not remove Animal-Pack at ordinary CCPh TI cleanup or begin it in any arbitrary second-player Rally.

**Example:** Completing the last required CCPh schedules release for the next Rally step; it does not immediately free the crew during CCPh.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:packStart`, `rph:packHalf`, `rph:packReverse`, `ccph:pinTi`.

### Tunnel entry can make Advance compulsory

**Origin:** A Good Order/dummy Infantry unit enters its tunnel at the start of its MPh, using all MF.

**Due:** It must emerge from the opposite entrance in the subsequent APh. Broken-unit tunnel rout instead begins and ends in the same RtPh.

**Sources:** B8.61-.63. Physical PDF 118.

- Keep origin, destination, carried load and entry conditions; do not offer ordinary Stay in place as though emergence were optional.
- Apply enemy-occupied pillbox and Encircled-origin consequences without leaking the hidden destination beforehand.
- Discovered/Recovered entrances create a distinct end-CCPh destruction check; route completion does not itself destroy them.

**Example:** The Movement receipt schedules mandatory emergence, while a rout through the same tunnel cannot remain underground until Advance.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:terrainModes`, `aph:tunnel`, `rtph:tunnel`, `ccph:tunnels`.

### Landing does not release all airborne contents at once

**Origin:** Gliders and parachutes complete their different MPh landing procedures.

**Due:** Glider contents use AFPh 5.21A with vehicle/Gun/PRC exceptions; remaining five-eighth-inch parachutes release contents at APh 7.26A.

**Sources:** E8.4; E9.4-.6; ASOP 3.42A-.43A, 5.21A, 7.26A. Physical PDF 240, 241, 242, 243, 680, 681.

- Preserve landing/crash/damage results and each content category instead of setting a universal landed-and-ready flag.
- Apply the just-landed paratroop fire/rout restrictions and half-inch versus five-eighth-inch counter transitions.
- Airborne Blaze elimination and aerial attacks must resolve at their own earlier windows.

**Example:** A landed glider and a landed parachute cannot be unpacked by the same generic MPh-end cleanup.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `rph:drop`, `mph:gliderLand`, `mph:parachuteLand`, `dfph:landedGlider`, `afph:glider`, `rtph:paratroops`, `aph:parachutes`.

### Drift and Drowning retain side and turn ownership

**Origin:** Boat, amphibian or swimmer occupies water; a survivor enters a Drowning procedure.

**Due:** ATTACKER Moderate/Heavy Current and Ocean drift precede DEFENDER Heavy Current drift; Drowning keeps its end-friendly-APh deadline.

**Sources:** B21.121; E5.23; E6.21; G13.444; ASOP 7.22A-.25B. Physical PDF 136, 233, 235, 311, 681.

- Do not drift both sides under identical conditions or use the currently active side for a swimmer’s friendly-APh clock.
- Resolve mine/FFE/surf/grounding consequences for each actual drifting subject before continuing.
- Apply craft-specific pier exemptions and preserve a Drowning obligation across unrelated phase changes.

**Example:** A swimmer’s survival deadline must not fire one Player Turn early merely because the opponent is advancing.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:climbSwim`, `aph:attackerDrift`, `aph:defenderDrift`, `aph:oceanDrift`, `aph:drowning`.

### Tank-Hunter creation reserves a target and a lifetime

**Origin:** A T-H Hero is created or loses HIP at an allowed window.

**Due:** Immediate window-specific action, compulsory target attack/advance where required, and rule-specific immediate or turn-end removal.

**Sources:** G1.421-.425; ASOP 3.32A, 3.36D, 7.12A, 8.13D. Physical PDF 279, 280, 679, 681.

- Track the creating MMC’s per-Player-Turn windows separately from the scenario-wide remaining allowance.
- Carry the Designated Target through Movement, Reaction Fire, Advance and CC; target loss and completed attacks can terminate the Hero before turn-end.
- Treat G1.421’s post-ATTACKER-Ambush restriction as a condition on the printed CCPh window, not a general permission to reverse ASOP precedence.

**Example:** An APh-created Hero is not an ordinary unit free to decline advancing toward its Designated Target.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:hero`, `mph:heroReaction`, `aph:tankHunter`, `aph:heroAdvance`, `ccph:tankHunter`, `ccph:heroRemoval`.

### Berserk recovery has several distinct windows

**Origin:** A unit becomes berserk, including the separate voluntary Dare-Death procedure.

**Due:** Test the appropriate MPh no-Known-enemy condition, AFPh halved-TPBF elimination, CC elimination, or Dare-Death end-Location condition.

**Sources:** A15.46; G18.6; ASOP 3.52A, 5.23A, 8.24B, 8.31B. Physical PDF 84, 85, 322, 680, 681.

- Preserve the actual Known-enemy and elimination history required by each test; do not replace them with a generic end-turn reset.
- The AFPh and CC elimination cases require the specified enemy elimination, not merely an empty Location after other units leave.
- Voluntary Dare-Death status and Tank-Hunter lifetime are not ordinary berserk recovery.

**Example:** A berserk unit’s condition may end in Movement but another unit’s must persist until its qualifying combat result.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:berserkEnd`, `afph:berserk`, `ccph:berserk`, `ccph:dareDeath`.

### APh entry hands a combat record to CCPh

**Origin:** Advance, reinforcement or another entry creates a CC encounter with origin and concealment history.

**Due:** Resolve one Location’s full CC sequence before the next; retain qualifying Melee into later Player Turns.

**Sources:** A11.12-.19; A11.3-.4; ASOP 7.21A, 8.11B-.31B. Physical PDF 72, 73, 74, 681.

- Carry entry history for Ambush and Street Fighting; do not reroll Ambush for ordinary reinforcement of an existing Melee.
- Freeze simultaneous declarations before results; sequential attacks reopen only after immediate consequences.
- A new Player Turn does not clear Melee. Eligibility to move, fire, rout or withdraw depends on each surviving unit’s status.

**Example:** A vehicle may hold Infantry without itself being held in Melee; a Location-only boolean cannot express that state.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `aph:enemy`, `aph:concealment`, `ccph:ambush`, `ccph:declarations`, `ccph:melee`, `rtph:blazeEscape`.

### LC Blaze Pin is not an ordinary unit Pin

**Origin:** A new Blaze is placed aboard a landing craft and receives its specified temporary Pin.

**Due:** Remove that Pin at the end of the next CCPh; subsequent Blaze MP/damage effects retain their own timing.

**Sources:** G12.68. Physical PDF 307, 308.

- Preserve Blaze creation time, suppression marker and landing craft identity.
- Removing the temporary Pin does not extinguish the Blaze or remove later AFPh damage obligations.
- Do not confuse this marker with personnel Pin, a new terrain Flame or a Hampered Flame.

**Example:** The LC remains ablaze after its temporary Pin is removed, so its next applicable AFPh still needs the damage check.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `ccph:lcBlazePin`, `afph:lcBlaze`, `ccph:pinTi`.

### Turn handover changes roles, not identities or lasting states

**Origin:** All Location and phase-wide CCPh obligations are complete.

**Due:** Hand over the Player Turn; advance the Game Turn only after its second Player Turn.

**Sources:** ASOP 1.11A-8.45B; A11.15; C6.5; D5.34; E10.3; G10.3. Physical PDF 72, 174, 203, 244, 291, 678, 679, 680, 681.

- Preserve side identity independently of ATTACKER/DEFENDER roles. Owner-relative smoke, friendly-phase deadlines and Game-Turn work must use stable side/turn IDs.
- Do not reset Melee, Acquisition, wounds, malfunction/depletion, Recall or other continuing statuses simply because phase counters were removed.
- Carry pending integration and source gaps forward with the affected obligation. A reference consideration mark cannot settle a mandatory effect.

**Example:** A two-Game-Turn task does not complete after two clicks on End Player Turn.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `ccph:close`, `rph:arrival`, `rph:repair`, `rph:ammo`, `rph:packStart`.

### Resolve ordering claims using ASOP precedence

**Origin:** A phase summary, core paragraph or special rule appears to assign a different window.

**Due:** Before exposing an executable action or scheduling a required effect.

**Sources:** ASOP introductory precedence and within-step rules; A24.32; A20.5-.53; G1.421. Physical PDF 87, 88, 92, 279, 678, 679.

- ASOP governs an ordering conflict. Normal eligibility, mutually exclusive actions and explicit restrictions still apply; within a step actions can be ordered freely unless constrained, with ATTACKER first for conflicts.
- WP Flame wording in A24.32 must be read with the printed WP consequence windows in 2.12/2.15, 3.32, 4.11/4.14 and 5.12. Do not silently relocate every listed check to a generic future MPh.
- The existing MPh prisoner-abandonment permission tension remains an interpretation issue: ASOP lists dropping prisoners, but A20.5 supplies friendly RPh/APh permission. Do not call a permission restriction merely an ordering conflict.

**Example:** A rule-specific restriction can qualify an ASOP step without establishing a blanket rulebook-over-ASOP hierarchy.

**Integration:** Source interpretation remains open for MPh prisoner abandonment; other cited ordering rules are reconciled.

**Linked topics:** `pfph:wpEffects`, `dfph:wpEffects`, `afph:wpEffects`, `mph:dropDuring`, `aph:prisoners`, `ccph:tankHunter`.

### Immediate effects cannot be parked until cleanup

**Origin:** An expenditure, attack, entry or qualifying roll triggers a required consequence.

**Due:** Complete the consequence and any nested decision at its trigger before continuing the parent action.

**Sources:** A17; A18.12; C1.23; ASOP 3.35D, 6.21B, 7.25B, 8.24B. Physical PDF 85, 86, 163, 679, 680, 681.

- Maintain parent/child event identity for mines, FFE, wounds, Snipers, Field Phone cuts, Field Promotion and forced movement under their own trigger rules.
- A phase can contain optional actions while still having mandatory unresolved effects. End unit, end side, end Location and end phase are different boundaries.
- Do not generate a Sniper attack from every DR. Preserve phase/roll eligibility and hidden information when explaining the consequence.

**Example:** A wound during movement must update remaining capability before the unit chooses its next expenditure, not at MPh cleanup.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:attackChain`, `dfph:attackResults`, `afph:results`, `rtph:results`, `aph:close`, `ccph:specialChecks`.

### A lost Spotter does not release every mortar obligation

**Origin:** A mortar receives an original Spotter in the permitted fire phase, or loses that Spotter.

**Due:** Retain the relationship; replacement after loss waits until the owner’s MPh opening. Opportunity Fire can leave a mandatory AFPh breakdown roll.

**Sources:** C9.3-.31; ASOP 2.22A, 3.11A, 4.21D. Physical PDF 179, 679, 680.

- Do not replace a Spotter immediately in AFPh just because the reserved shot has become impossible.
- A reduced Spotting squad’s surviving Good Order HS retains the relationship. Pin and loss of Good Order have their own effects.
- An Opportunity-Fire mortar with neither usable Spotter nor a target in its own LOS still makes the C9.3/A6.11 breakdown roll. A failed opportunity is not always an empty obligation.

**Example:** A broken Spotter prevents the promised indirect shot, but the checklist still presents the required breakdown check.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:mortarSpotter`, `pfph:opportunity`, `mph:spotter`, `dfph:mortarSpotter`, `afph:mortar`.

### Labor persists independently of TI cleanup

**Origin:** An unsuccessful qualifying Clearance, Entrenchment or Manhandling attempt earns task-specific Labor.

**Due:** Retain until that task succeeds or all MMC beneath it are eliminated or leave their Location.

**Sources:** B24.8; B27.11; C10.3. Physical PDF 143, 146, 181.

- Store the specific task and Location, not a transferable unit-wide bonus.
- The first eligible failure earns -1 and the next -2; joining units can participate in the same task under the rule.
- Removing TI does not remove earned Labor. Entrenchment resolves in PFPh, Manhandling in MPh, and non-Flame Clearance in CCPh; they share persistence principles but not resolution windows.

**Example:** A squad can lose ordinary TI at turn-end while retaining Labor for its next eligible Entrenching Attempt.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `pfph:entrench`, `mph:manhandle`, `ccph:labor`, `ccph:pinTi`.

### CE and BU changes depend on earlier actions

**Origin:** A vehicle voluntarily changes posture or combat forces it BU.

**Due:** Voluntary changes are in the owner’s MPh/APh, constrained by earlier fire, same-phase changes and unresolved attacks.

**Sources:** D5.33-.34; E10.3. Physical PDF 203, 244.

- Preserve whether a counter was voluntarily placed or removed during this phase. Do not permit both voluntary directions in one phase.
- Do not allow a MPh change after vehicle/PRC Prep or Bounding First Fire, or inside an unresolved attack window tied to the last expenditure.
- A Stun flip does not restore CE; losing CE can interrupt replenishment. Re-evaluate the linked obligation immediately.

**Example:** A crew forced BU during replenishment interrupts that work even though a later phase will offer a legal CE choice.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:crewPosture`, `aph:crewExposure`, `ccph:stun`, `rph:ammo`.

### Vehicle Dust follows its source and motion history

**Origin:** A qualifying vehicle creates or shifts Vehicle Dust as it expends MP.

**Due:** Remove when the vehicle leaves its current hex, or the specified Wind Change result occurs. Otherwise remove at next friendly PFPh if not in Motion or destroyed/stunned/shocked/immobilized; a continuing-Motion vehicle removes it on its next-MPh initial MP expenditure.

**Sources:** F11.74-.741; ASOP 1.12A, 2.11A. Physical PDF 267, 268, 678.

- Keep source vehicle and creation/shift history, with platoon/convoy dust changes at the proper impulse boundary.
- Do not expire a continuing-Motion dust counter at every phase transition or treat all dust as SMOKE.
- FFE Dust and ambient dust use separate conditions. Wind/Gust changes do not affect FFE Dust as though it were Vehicle Dust.

**Example:** A vehicle’s last dust counter can persist into its next friendly Prep Fire if it remains in Motion.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:dust`, `pfph:dustCleanup`, `rph:dust`, `rph:smoke`.

### An unsuccessful smoke-dispenser attempt still spends its window

**Origin:** An eligible AFV attempts to dispense smoke during either side’s MPh.

**Due:** One attempt per Player Turn; keep the attempt spent even when no smoke is fired. CC sN HE uses its separate procedure.

**Sources:** D13.2-.3; A11.622. Physical PDF 212, 76.

- Check the prior AFV/PRC firing history and crew status before offering the MPh attempt.
- A failed attempt does not cost the successful-firing MP, but does not refresh the once-per-Player-Turn allowance.
- Store the attempt and smoke-effect duration separately. Removing smoke or a fire marker cannot restore the spent attempt.

**Example:** An AFV that failed its dispenser roll cannot try again on the opponent’s next expenditure in that same Player Turn.

**Integration:** Execution coverage is mixed; verify the full linked rule path. Pending rules remain visible.

**Linked topics:** `mph:smoke`, `mph:motionAttempt`, `ccph:closeDefense`.

## Acceptance and audit limits

For every chain above, use its example as a positive timing case and test the immediately preceding window as a negative case. Additional required cases: save/reload exactly on a boundary; two effects sharing a source; a source removed before scheduled completion; opposite-player ownership; interruption after one completed Game Turn; an event that produces a new event in the same phase; and a hidden subject whose queue entry must not leak. These are future engine/component acceptance requirements, not passed rules-engine tests.

The cross-phase catalog provides a traceable audit of identified dependencies. It does not replace the full rulebook, resolve the remaining prisoner interpretation, certify external module sources, or implement the scheduled actions.

## Verification performed

Browser checks passed for all 38 chains and 191 bidirectional topic links, with matching origin, due window and example text. Navigation preserved independent phase consideration state. Saved audit selections and phase selections restored correctly, including older state and the supplied Advance/ski-use selection; initial rendering did not save over host state. A maximal consideration snapshot remained below the 16 KiB limit.

The full eight-phase regression exercised all 538 topics and every contextual definition interaction, timing selectors, consideration state, saved-state restoration and the existing CCPh ledger checks. No browser runtime errors or horizontal overflow occurred at 320px and 736px. Light and dark screenshots were inspected. The embedded dependency data matches the repository catalog and audit prose; the preview remains below 1 MB. These are reference-preview checks, not rules-engine execution tests.
