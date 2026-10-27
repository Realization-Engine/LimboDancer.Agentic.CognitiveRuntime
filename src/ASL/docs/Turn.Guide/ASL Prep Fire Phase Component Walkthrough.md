# ASL Prep Fire Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design content and preview. No Blazor component or Rules behavior is implemented by this document.

Design target: the completed ASL.Rules implementation after all passes. Pending rules and deferred chapters are included now, with their conditions and consequences. See the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md) and [Rally walkthrough](ASL%20Rally%20Phase%20Component%20Walkthrough.md).

## Source and reconciliation scope

Authority: the supplied eASLRB v3.01, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`, at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`. The printed Prep Fire ASOP spans physical PDF pages 678-679. All page locators here are one-based physical PDF pages.

All nine printed PFPh steps are mapped below, including the dagger note for Sniper attacks/checks. The review follows their references and searches the entire extracted volume for PFPh/Prep Fire Phase (197 matches, including index, repeated charts, examples and training material). The ASOP layout was visually checked. Shared firing rules supply the attack-mode and consequence records that the ASOP summarizes as firing eligible units/weapons. Chapters F/G/W and weapon-note-dependent options remain represented.

This is coverage of the supplied ASOP and identified in-volume rule families, not a claim to enumerate every scenario SSR or individual Chapter H equipment-note exception. The ASOP’s RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 Sniper references are present as named source gaps; their full module rules are outside this volume. Unknown scenario or weapon-note applicability stays visible and cannot be evaluated as false. The catalog is explanatory content, not executable proof of legality.

### Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 2.11A | Remove/disperse SMOKE; Napalm consequences; Starshells; Vehicle Dust | `smokeCleanup`, `napalm`, `dustCleanup`, `starshellOpening` |
| 2.12A | Ordnance SMOKE; mortar IR; Spotter; WP NMC/Flame; MOL Projector; fire/Gunflash markers | `smokeFire`, `wpEffects`, `mortarIR`, `earlySpotter`, `molProjector` |
| 2.13A | Contact and exceptions; Sighting TC; Access; AR/SR/Pre-Reg FFE/IR; rockets; FFE:C | `radio`, `access`, `newMission`, `obaIR`, `rockets`, `aerialObserver`, `continuation` |
| 2.14A | Correct/convert/cancel; Creeping Barrage; rocket correction exclusion | `correct`, `continuation`, `barrage`, `rockets` |
| 2.15A | OBA SMOKE/IR/WP; HE FFE; all listed terrain/fortification effects; Column and Reverse Slopes | `obaSmokeResolution`, `ffe`, `terrainEffects`, `columnReverseSlope`, `naval` |
| 2.21A | Mop Up/casualties; entrench/Labor; Kindling/NTC; TI/Prep Fire | `mopUp`, `entrench`, `kindle` |
| 2.22A | Original mortar Spotter; unit and weapon fire; Heavy AA; mutual resolution; Opportunity Fire; later Starshells; destroy/malfunction/dm/assemble | `fireOrHold`, `smallArms`, `spraying`, `weapons`, `ordnance`, `specialAmmo`, `mortarSpotter`, `vehicleFire`, `rof`, `intensive`, `ftMol`, `dc`, `encirclement`, `opportunity`, `starshellLater`, `heavyAA`, `assemble`, `destroy`, `attackResults` |
| 2.23A | Limber/unlimber; unlimber TI | `limber`, `unlimber` |
| 2.31A | Eligible end-phase CA change; AA-mode designation/cancellation | `caEnd`, `aaModeEnd` |

Supplemental records explicitly add Chapter W searchlight establishment/maintenance/cancellation and FAC-directed Close Air Support after all FFE; special nationality/equipment overlays; prisoner-elimination rules; standing choices and ammunition commitments; attack/Sniper/phone-cut consequences. They retain their actual rule timing and do not invent ASOP substep numbers.

## Prep Fire interaction model

Unlike Rally, Prep Fire has no generic one-action-per-unit allowance. Capability depends on inherent fire, weapon-use capacity, direction, retained ROF, Intensive Fire and linked vehicle/PRC commitments. The checklist must track those independently. A successful shot is not a completed weapon if it retains ROF; a failed attempt is not automatically free.

The displayed sequence is authoritative, but activities inside one step can occur in any order unless a rule specifies otherwise. ATTACKER acts first in a conflict. Conditional consequences interrupt the sequence until resolved. Ordinary firing is ATTACKER activity, but both sides resolve results and Sniper responses, and W9.21 creates a specific DEFENDER Light AA window during qualifying PFPh air support.

Each decision includes subject, side, Player Turn, source window, known eligibility, remaining capability and dependency links. Use Not considered, Declined, Attempted, Resolved and Required follow-up as separate states. Implementation coverage is independent: pending does not mean illegal. Reference-mode Mark topic considered never executes, blocks, or satisfies a game obligation.

### Three innovations to carry into the component

1. **Commitment comparison:** before a unit fires, show what that choice gives up and what remains. Prep Fire, Opportunity Fire, holding for movement, spotting, OBA direction and labor are materially different commitments. Do not automatically recommend the highest immediate FP.
2. **Closing-window reminders:** identify the first non-SMOKE attack that closes side-wide smoke opportunities; original Spotter designation; opening non-leader illumination; and the final CA/AA window. The reminder names actual known subjects, never hidden opposing ones.
3. **A chain of consequences:** attach WP NMC, Flame, attack results, Sniper checks, phone cuts and terrain changes to the originating roll. The next shot becomes available only when necessary results settle. Re-evaluate LOS and eligibility after each chain without clearing previous attempts.

### Concrete worked decision sequence

- Before firing, remove due smoke and dust; resolve associated Napalm effects. Offer eligible Starshell and Chapter W searchlight choices.
- A mortar could fire Smoke, IR or ordinary HE. IR commits all of its fire for the Player Turn; a failed IR Usage dr has different accounting. Show the consequence before the choice.
- Resolve contact, Access, existing missions and all due FFE. If the scenario has qualifying Chapter W CAS, expose its after-FFE attack and defender AA window.
- Squad A attacks now, squad B reserves Opportunity Fire, and squad C holds for movement. The checklist records different commitments without treating C as unfinished.
- A Gun retains ROF. Offer its next shot or stopping; only after normal ROF is exhausted consider IF where legal. Keep Encirclement sequencing and concealed information intact.
- At phase end an otherwise eligible unfired Gun can change CA, losing movement but retaining its possible ordinary AFPh shot. An Opportunity-Fire Gun cannot use this to evade its CA restriction.
- End only after verified required effects are settled. Optional unreviewed topics and pending evaluation are reported without claiming every legal option was exhausted.

## Integration evidence

Current coverage labels are conservative summaries of [ASL Rule Coverage](../ASL%20Rule%20Coverage.md), not newly executed tests. It records partial fire-group, ordnance, Opportunity Fire, SMOKE and special-case coverage. Pass 70’s Mopping Up in any building differs from the supplied A12.153 building conditions; the guide targets the source-backed final design and must disclose the current mismatch. Pass 71 added broad Sniper triggers, but terrain and some special cases remain pending. Newly detailed special rules are labeled pending until a verified action path exists.

## Cataloged options

### 2.11A · Opening cleanup and illumination

#### Remove and disperse existing SMOKE

ID: `smokeCleanup`. **Who:** ATTACKER. **Type:** Required opening procedure.

**Rules:** A24.4; A24.61; ASOP 2.11A. **PDF pages:** 678, 92, 93. **Coverage:** Rules integration pending; reference available.

**Consider:** Which of your old SMOKE counters expire or disperse now?

- Remove the owning side’s white Dispersed SMOKE first; then flip its remaining white SMOKE to Dispersed. Do not remove both sides’ SMOKE indiscriminately.
- Update dependent drifting counters when the source is removed or reduced. Separate ordinary source cleanup from the special Napalm rule.
- Re-evaluate visible LOS and Hindrances before proposing new shots. Link gray drift to its originating source: remove it immediately when that source is removed; when the source disperses, remove the furthest drifting counter under A24.61.

**Checklist behavior:** Show the affected counters and resulting Hindrance. A considered topic cannot substitute for required cleanup.

**Illustration:** Remove and disperse existing SMOKE / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SMOKE, WP, PFPh.

#### Resolve expiring Napalm Blaze effects

ID: `napalm`. **Who:** Owner of affected Napalm. **Type:** Required when triggered.

**Rules:** G17.41; ASOP 2.11A. **PDF pages:** 678, 321. **Coverage:** Rules integration pending; reference available.

**Consider:** Does removal of Napalm smoke leave burning terrain or destroy equipment?

- Identify Napalm-origin smoke and its associated Blaze; generic smoke cleanup loses the information needed here.
- On smoke removal, remove the Napalm Blaze in non-Burnable Terrain; in Burnable Terrain make the prescribed Kindling DR to determine conversion to a Terrain Blaze. Resolve the separate 12-FP-column weapon-destruction dr for each eligible SW/Gun, with the pillbox modifier.
- Keep this visible for future air-support scenarios even though ordinary Chapter E ground support does not attack in PFPh.

**Checklist behavior:** Resolve source-linked counters and equipment before continuing.

**Illustration:** Resolve expiring Napalm Blaze effects / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SMOKE, Blaze, SW.

#### Remove due Vehicle Dust

ID: `dustCleanup`. **Who:** ATTACKER. **Type:** Required when due.

**Rules:** F11.74; ASOP 2.11A. **PDF pages:** 678, 267, 268. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this vehicle’s last dust counter expire now?

- Check whether the vehicle begins its next friendly PFPh not in Motion, or has been destroyed, stunned, shocked or immobilized as specified.
- Do not remove an eligible continuing-Motion dust counter merely because the phase changed; its next MP expenditure can be the removal event.

**Checklist behavior:** Retain the source vehicle and movement state on each dust record.

**Illustration:** Remove due Vehicle Dust / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** Motion, AFV, LOS.

#### Attempt opening Starshells

ID: `starshellOpening`. **Who:** ATTACKER. **Type:** Optional, limited timing.

**Rules:** E1.91-.922; ASOP 2.11A. **PDF pages:** 678, 226, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Would illumination help before firing, and which unit should use this hex’s attempt?

- Check the initial-use trigger and the one-attempt-per-hex limit. Eligible Good Order non-pinned, non-TI leaders, MMC and CE AFV have distinct Usage thresholds.
- After the initial Player Turn of illumination, non-leader Starshell attempts must precede ordinary firing. Leaders retain their later timing exception.
- Choose the placement method before its placement dice. Starshell use alone does not consume ordinary fire capability, but hidden/Cloaked firers must be placed concealed.

**Checklist behavior:** Show one shared hex opportunity, not independent unlimited attempts for every unit in the stack.

**Illustration:** Attempt opening Starshells / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** NVR, IR, dr, CE, MMC, SMC.

### 2.12A · Early SMOKE, IR and MOL Projectors

#### Fire ordnance Smoke or WP first

ID: `smokeFire`. **Who:** ATTACKER. **Type:** Expiring ammunition window.

**Rules:** C8.5-.6; A24.2-.5; ASOP 2.12A. **PDF pages:** 678, 91, 92, 178, 179. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Do you want smoke before any friendly non-SMOKE fire closes the window?

- Check ammunition availability, Depletion Number, weather, terrain, range and the appropriate Area Target Type procedure.
- SMOKE must precede other friendly non-SMOKE fire. Keep the closure side-wide, not merely a flag on this Gun. Respect the distinct IR and MOL-Projector procedures and ASOP ordering; their early windows do not grant unrestricted later SMOKE fire.
- Preview full-strength PFPh placement, WP’s distinct height and morale effects, and ROF rules. An unavailable special-ammo attempt has its own C8.9 accounting.

**Checklist behavior:** Before committing the first window-closing shot, show unconsidered eligible SMOKE sources without forcing the player to use them.

**Illustration:** AT Gun: smoke screen or HE attack? The checklist shows the shared SMOKE window before the player commits.

**Terms:** SMOKE, WP, TH, ATT, ROF, s#.

#### Resolve WP checks and possible Flame

ID: `wpEffects`. **Who:** Both sides resolve affected units. **Type:** Triggered consequences.

**Rules:** A24.31-.32; C3.76; ASOP 2.12A and 2.15A. **PDF pages:** 678, 92, 171. **Coverage:** Rules integration pending; reference available.

**Consider:** Which units and terrain are affected by this WP placement?

- Resolve WP NMC using its own terrain/CH rules, not the ordinary HE result path.
- Track the source distinction between direct ordnance placement and a WP FFE, which can affect vulnerable occupants throughout its Blast Area.
- Resolve any Flame check before moving on. A successful placement is not the end of the action.

**Checklist behavior:** Create linked resolution rows for each affected disclosed subject and required terrain effect.

**Illustration:** Resolve WP checks and possible Flame / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** WP, NMC, CH, DR.

#### Fire a mortar Illuminating Round

ID: `mortarIR`. **Who:** ATTACKER. **Type:** Optional, commits weapon.

**Rules:** C8.7; E1.91; E1.93-.932. **PDF pages:** 678, 179, 226, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Is illumination worth giving up this mortar’s other ammunition for the Player Turn?

- Confirm IR capability and initial-use permission, then make the Usage dr. Failure is not firing.
- On success make the TH DR for malfunction, Low Ammo and SAN purposes; use the IR placement procedure instead of an ordinary target hit.
- Firing IR uses all the mortar’s ROF for the Player Turn and prevents another ammunition type. Place No Fire; resolve Gunflash/concealment. Its CA does not change because IR was fired.

**Checklist behavior:** Distinguish Usage failed, round fired, and failed due to malfunction. Do not offer HE after an IR has been fired.

**Illustration:** Fire a mortar Illuminating Round / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IR, MTR, dr, TH, SAN, ROF.

#### Designate a Spotter for early mortar fire

ID: `earlySpotter`. **Who:** ATTACKER. **Type:** Optional prerequisite.

**Rules:** C9.3-.31; ASOP 2.12A. **PDF pages:** 678, 179. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the early mortar shot need a Spotter?

- Use eligible Good Order Personnel in the same or adjacent hex; several mortars sharing a Spotter must meet the common-hex and common-target conditions.
- Apply spotted-fire TH and ROF penalties. Spotting counts as SW use and restricts movement and inherent fire as specified.
- Keep a hidden Spotter private. Replacing a lost original Spotter has a later MPh timing; this is not a free replacement control.

**Checklist behavior:** Attach the Spotter commitment to the planned early shot and to remaining unit capability.

**Illustration:** Designate a Spotter for early mortar fire / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MTR, LOS, ROF, SW.

#### Fire a MOL Projector before ordinary weapons

ID: `molProjector`. **Who:** ATTACKER. **Type:** Expiring special-weapon window.

**Rules:** C13.51-.58; ASOP 2.12A. **PDF pages:** 678, 184. **Coverage:** Rules integration pending; reference available.

**Consider:** Is a MOL Projector attack wanted before non-SMOKE weapons fire?

- Predesignate its target and validate range, elevation and CA using C13.5. It is not a squad’s MOL attack.
- In PFPh it must precede the first weapon firing other than SMOKE. Leadership does not modify its TH DR.
- Resolve its hit, smoke, possible Flame and fire-marker consequences.

**Checklist behavior:** Display a separate last-chance reminder alongside the SMOKE window.

**Illustration:** Fire a MOL Projector before ordinary weapons / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MOL, TH, SMOKE, CA.

### 2.13A · Contact, Access and new artillery missions

#### Gain or maintain artillery contact

ID: `radio`. **Who:** ATTACKER’s Observer. **Type:** Optional or mission-dependent.

**Rules:** C1.2, C1.22-.23, C1.6-.63; G.7; E12.77. **PDF pages:** 678, 163, 166, 276, 248. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Should this Observer commit the phase to the battery?

- Validate the Observer, possession, transport/Guard limits and radio or Field Phone. Check nationality/date/PTO modifiers and whether maintenance rather than first contact applies.
- Contact/maintenance is the Observer’s sole phase action apart from allowed OBA activities. A failed attempt cannot be retried by this radio or Observer in the same phase.
- Distinguish voluntary and involuntary contact loss and their mission effects. Offboard Observers and Creeping Barrages have explicit exceptions, not an assumed radio roll.

**Checklist behavior:** Compare OBA use with the leader’s competing fire-direction role before commitment.

**Illustration:** Gain or maintain artillery contact / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, DR, Radio Contact, FFE.

#### Request Battery Access

ID: `access`. **Who:** ATTACKER’s Observer. **Type:** Conditional choice or required procedure.

**Rules:** C1.21; C1.34; G14.63. **PDF pages:** 678, 163, 164, 316. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Is Access needed, and does FFE:C make the request mandatory?

- Keep Contact and Access separate. Use the battery’s actual draw pile and the required additional-draw conditions.
- With Contact and an FFE:C, resolve the mandatory Access attempt and continuation outcome. Do not offer ordinary attacks while an unresolved required branch is pending.
- Honor red-card consequences, permanent loss where applicable, and special battery/NOBA rules. Never reveal private draw-pile order.

**Checklist behavior:** Show the next legal branch after the draw; marking this considered cannot gain Access.

**Illustration:** Request Battery Access / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** Battery Access, OBA, FFE:C, AR.

#### Choose a new mission and place its AR

ID: `newMission`. **Who:** ATTACKER’s Observer. **Type:** Optional mission declaration.

**Rules:** C1.3-.32; C1.7-.732. **PDF pages:** 678, 164, 166. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which mission and target are justified by this Observer’s view?

- Choose the legal mission type and declare it at the required time. Ordinary HE Concentration, Harassing Fire, SMOKE, IR and Barrage are distinct choices.
- Place an AR using Observer LOS and the correct additional-draw tests, then resolve Accuracy and Extent of Error. Do not leak hidden units through legality messages.
- A Pre-Registered eligible target may use its direct-FFE procedure. An ordinary SR is not permission to resolve an immediate HE attack.

**Checklist behavior:** Keep proposed AR, resulting SR and FFE as distinct states, with a visible next decision.

**Illustration:** Choose a new mission and place its AR / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AR, SR, FFE, HE, LOS, Harassing Fire.

#### Declare an OBA illumination mission

ID: `obaIR`. **Who:** ATTACKER’s Observer. **Type:** Optional early declaration.

**Rules:** E1.931-.932; C1.7. **PDF pages:** 678, 166, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this battery provide illumination instead of a normal fire mission?

- Declare IR before the mission’s first Battery Access draw. OBA needs Contact and Access but no mortar-style Usage dr.
- Track FFE stage offboard for Access purposes; place IR by its own procedure, not as an FFE Blast Area.
- Ordinary IR use consumes the battery’s ROF and other-ammunition opportunity for the Player Turn; apply the separate NOBA exception where authorized.

**Checklist behavior:** Expose this before drawing, so the interface cannot invite a late retroactive mission choice.

**Illustration:** Declare an OBA illumination mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IR, OBA, Battery Access, SR.

#### Place a rocket artillery mission

ID: `rockets`. **Who:** ATTACKER’s Observer. **Type:** Special mission procedure.

**Rules:** C1.9; ASOP 2.13A-.14A. **PDF pages:** 678, 167. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this a rocket battery with its own placement and expenditure rules?

- Use the rocket AR/FFE procedure and applicable restrictions rather than the ordinary SR correction loop.
- Rocket Correction is not allowed. Show the limited mission opportunity and resulting battery state.

**Checklist behavior:** Do not offer a misleading Correct SR button on a rocket mission.

**Illustration:** Place a rocket artillery mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AR, FFE, OBA.

#### Use an Observation Plane or special Observer

ID: `aerialObserver`. **Who:** ATTACKER. **Type:** Conditional observer procedure.

**Rules:** C1.63; E7.61-.62; G14.68. **PDF pages:** 678, 166, 239, 240, 317. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this Observer use aerial sighting or shipboard procedures?

- For an Observation Plane, resolve applicable Sighting TC and observation restrictions before the battery’s placement and resolution.
- Use its actual aerial LOS and mistaken-attack rules rather than ground-Observer visibility.
- A Shipboard Observer has its own LOS, accuracy and AR restrictions. Keep offboard and aerial Observer identities distinct.

**Checklist behavior:** A new Observer profile changes the decision and consequences; it is not just a different icon.

**Illustration:** Use an Observation Plane or special Observer / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, LOS, TC, NOBA.

### 2.14A · Existing artillery missions

#### Correct, convert, leave or cancel an existing mission

ID: `correct`. **Who:** ATTACKER’s Observer. **Type:** State-dependent choice.

**Rules:** C1.33-.337; C1.35-.4. **PDF pages:** 679, 164. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** What does the existing marker and current Observer LOS allow?

- Enumerate the alternatives permitted for this exact SR or FFE:1/2 state, including leaving a qualifying SR in place, correction, predesignated conversion, and cancellation.
- Require the applicable Contact, Access and Blast-Height/target visibility. Preserve declaration-before-dice ordering.
- Do not offer arbitrary backward transitions such as replacing an FFE:1/2 with an SR; apply the special cancellation consequences.

**Checklist behavior:** Display legal branches with reasons for unavailable ones; pending evaluation remains unknown rather than illegal.

**Illustration:** Correct, convert, leave or cancel an existing mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SR, FFE, AR, LOS.

#### Resolve FFE:C continuation

ID: `continuation`. **Who:** ATTACKER’s Observer. **Type:** Required after applicable Access attempt.

**Rules:** C1.34-.343; G14.671. **PDF pages:** 679, 164, 316, 317. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Can the next mission continue here, or must it begin with a new AR?

- Resolve the old mission’s cancellation and the permitted new SR/FFE/AR branches after the Access outcome.
- Do not treat FFE:C as another attacking FFE.
- NOBA forbids the ordinary C1.341/C1.342 continuation shortcuts and requires its new-AR procedure.

**Checklist behavior:** One continuation decision replaces the old mission; it does not silently extend an HE attack.

**Illustration:** Resolve FFE:C continuation / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE:C, SR, FFE, NOBA.

#### Correct or lift a Creeping Barrage

ID: `barrage`. **Who:** Scenario Attacker. **Type:** Scheduled mandatory procedure with specific choices.

**Rules:** E12.1-.7; E12.73-.771. **PDF pages:** 679, 246, 247, 248. **Coverage:** Rules integration pending; reference available.

**Consider:** Which recorded Barrage step is due now?

- Use the pre-recorded direction, timing and Lift schedule. Correct/convert and resolve the Creeping Barrage as required; do not ask for normal Observer radio contact.
- Distinguish an ordinary Barrage’s geometry from a Creeping Barrage’s compulsory progression.
- Represent the allowed early-Lift/conversion decision and later return to ordinary OBA under E12.771, including its delay and Contact/Access requirements.

**Checklist behavior:** Show the scheduled footprint and mandatory progression, with secret planning visible only to its owner.

**Illustration:** Correct or lift a Creeping Barrage / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE, Barrage, AR, OBA.

### 2.15A · Artillery resolution and consequences

#### Place OBA SMOKE or IR before HE FFE

ID: `obaSmokeResolution`. **Who:** ATTACKER; both sides resolve effects. **Type:** Required mission resolution.

**Rules:** C1.71; E12.51; E1.93; A24.32; C3.76; G14.65. **PDF pages:** 679, 166, 171, 227, 247, 316. **Coverage:** Rules integration pending; reference available.

**Consider:** Which smoke or illumination missions must resolve before HE artillery?

- Place eligible SMOKE in the actual Blast Area, respecting terrain and weather exclusions and Barrage variations.
- Resolve WP NMC and Flame consequences under the FFE-specific rules.
- Apply NOBA illumination exceptions through its own mission record; ordinary OBA IR remains distinct from a smoke FFE.

**Checklist behavior:** Resolve all applicable early effects before HE FFE. Separate placement completion from affected-unit resolution.

**Illustration:** Place OBA SMOKE or IR before HE FFE / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SMOKE, WP, IR, FFE, NMC.

#### Resolve HE FFE and advance its marker

ID: `ffe`. **Who:** Both sides resolve; ATTACKER’s battery. **Type:** Required when due.

**Rules:** C1.5; C1.52-.56; G14.65. **PDF pages:** 679, 164, 165, 316. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which hexes and occupants are affected by this mission now?

- Resolve the actual Blast Area with the prescribed separate hex DR and all eligible friendly as well as enemy occupants.
- Use the applicable protection, level, collateral and special-terrain rules. Do not apply movement-only FFMO/FFNAM in this stationary phase resolution.
- Advance FFE:1 to FFE:2 or FFE:2 to FFE:C as required. Involuntary loss of radio contact does not automatically erase all existing FFE effects.

**Checklist behavior:** The battery stays unresolved until unit and terrain consequences finish.

**Illustration:** Resolve HE FFE and advance its marker / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE, HE, IFT, DR, PRC.

#### Resolve artillery changes to terrain and fortifications

ID: `terrainEffects`. **Who:** Both sides resolve affected state. **Type:** Triggered mandatory effects.

**Rules:** B2.1; B6.331; B25.13; B24.11; B26.52; B29.5; B30.92; B28.62; G9.72; G13.7; G14.56; E1.952; F8.41. **PDF pages:** 679, 113, 116, 142, 143, 145, 149, 150, 153, 286, 312, 315, 227, 264. **Coverage:** Rules integration pending; reference available.

**Consider:** Did this attack change the map or remove a fortification?

- Check the separate conditions for shellholes, Flame, rubble and bridge effects; a qualifying result is not one universal destruction rule.
- Check wire, roadblock and pillbox removal, minefield and panji removal/reduction, and sangar or trip-flare elimination.
- Include beach-obstacle and pier rules where relevant. Never expose a hidden fortification merely to display a speculative effect.

**Checklist behavior:** Create per-feature follow-ups with the governing rule and originating attack. This explicitly covers every terrain/fortification family listed in 2.15A.

**Illustration:** Resolve artillery changes to terrain and fortifications / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** HE, FFE, KIA, TEM, Blaze.

#### Resolve Column and Reverse Slope effects

ID: `columnReverseSlope`. **Who:** Affected sides. **Type:** Triggered consequences.

**Rules:** E11.533; G14.66-.661. **PDF pages:** 679, 245, 316. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this artillery resolution break a Column or interact with a Reverse Slope?

- Apply Column disbandment when its trigger is met; do not leave its formation state unchanged after the attack.
- For NOBA, resolve Reverse Slope protection and exceptions against the naval line of fire, not a generic observer-LOS shortcut.

**Checklist behavior:** Surface these as dependencies of the actual mission, with pending integration visible.

**Illustration:** Resolve Column and Reverse Slope effects / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE, NOBA, Column, LOS.

### 2.21A · Mopping Up, entrenching and Kindling

#### Mop Up a building

ID: `mopUp`. **Who:** ATTACKER’s eligible Infantry MMC. **Type:** Optional commitment.

**Rules:** A12.153-.154; A20.3-.4. **PDF pages:** 679, 79, 87. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Is securing this building worth committing these units for the turn?

- Check armed, unpinned Good Order MMC, the source’s building conditions, no unconcealed unbroken enemy, and coverage of uncontrolled ground-level Locations. Predesignate multiple participants where needed.
- One attempt per building per Player Turn. Treat Rowhouses separately and exclude rubble/Blaze Locations as specified. No Quarter/Massacre disables this option.
- Reveal hidden units only through the authorized result, remove Dummies, resolve possible casualties and determine Control/surrender only when its conditions are met.

**Checklist behavior:** Track the building and participants together. In current software, disclose the recorded any-building implementation deviation; final-design eligibility follows the source.

**Illustration:** Two MMC cover a large building together / Both become TI / A hidden defender is revealed concealed, so securing and surrender are not assumed.

**Terms:** MMC, TI, HIP, Control.

#### Attempt entrenchment

ID: `entrench`. **Who:** ATTACKER’s Infantry MMC and participating leader. **Type:** Optional commitment.

**Rules:** B27.11; B24.8; A25.21; F.1B; G3.5; G13.3; G13.82; E3.743. **PDF pages:** 679, 146, 142, 94, 254, 282, 309, 313, 231. **Coverage:** Rules integration pending; reference available.

**Consider:** Would a foxhole improve this position enough to forgo movement and fire?

- Check unused eligible participants, terrain, Wire/existing-entrenchment exclusions and special Desert/PTO/beach restrictions. Extreme Winter forbids digging foxholes.
- Preview the ordinary final DR of 5 or less, single-HS/crew adjustment, leadership, Labor and applicable nationality/terrain modifiers.
- All participants become TI even on failure. Preserve eligible Labor after failure and the actual foxhole capacity/result after success. Entrenching DR can trigger a Sniper.

**Checklist behavior:** A failed attempt is completed, with Labor and TI tracked separately from success.

**Illustration:** Attempt entrenchment / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TI, DR, DRM, MMC, Labor.

#### Attempt Kindling

ID: `kindle`. **Who:** ATTACKER’s Infantry and required leader. **Type:** Optional commitment.

**Rules:** B25.11; A22.613; scenario SSR. **PDF pages:** 679, 143, 90. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the scenario permit deliberate fire here, and is the commitment worthwhile?

- Check Burnable Terrain, unpinned Good Order Infantry and the required SMC NTC. Check scenario restrictions on Kindling.
- Predesignate all attempts a leader directs in this Location. It cannot wait for one result before adding another unit or simultaneously make its own attempt.
- Show terrain Kindling Number, EC, reversed leadership modifier, unit-size and eligible MOL adjustments. All involved use their full Prep Fire capability regardless of outcome.

**Checklist behavior:** Use Prep Fire consequences rather than incorrectly labeling this as ordinary TI entrenchment.

**Illustration:** Attempt Kindling / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** NTC, DR, EC, MOL, Blaze.

### 2.22A · Fire, Opportunity Fire and weapon handling

#### Choose fire now, Opportunity Fire, or preserve movement

ID: `fireOrHold`. **Who:** ATTACKER. **Type:** Commitment comparison.

**Rules:** A3.2-.5; A7.1; A7.25; D.3. **PDF pages:** 679, 47, 54, 55, 192. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which units should commit now, and which should retain later options?

- Prep Fire normally prevents that unit’s subsequent movement and AFPh fire. Identify linked vehicle/PRC restrictions and exceptions rather than treating every counter independently.
- Opportunity Fire defers an eligible Infantry/Cavalry attack to AFPh at full effect but also gives up movement. Simply not firing preserves different choices.
- Do not require every unit to fire or every optional topic to be checked. Track individual weapons, SW capacity and leadership commitments.

**Checklist behavior:** Preview the capabilities given up before execution, then keep retained ROF choices open.

**Illustration:** Squad A fires now. Squad B reserves Opportunity Fire. Squad C holds fire to move. Three deliberate choices, not two incomplete checkboxes.

**Terms:** PFPh, MPh, AFPh, TI, ROF.

#### Fire inherent FP or a fire group

ID: `smallArms`. **Who:** ATTACKER. **Type:** Optional attack.

**Rules:** A7.2-.5; A7.53-.55; A9.4. **PDF pages:** 679, 54, 55, 56, 57, 64. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Should these units combine fire or keep separate attacks?

- Choose legal target Locations and validate LOS, range and mandatory FG rules before resolving an attack. Group membership is constrained, not any convenient collection of units.
- Preview PBF/TPBF, Long Range, Area Fire, concealment, terrain and Hindrances. Keep same-hex and multi-level target rules explicit.
- Apply allowed leadership and its direction commitments. Do not suggest splitting an attack where mandatory FG rules require combining it.

**Checklist behavior:** Show adjusted FP and DRM with provenance, plus the members committed by confirmation.

**Illustration:** Fire inherent FP or a fire group / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, FG, IFT, DRM, PBF, TPBF, LOS.

#### Choose Spraying Fire

ID: `spraying`. **Who:** ATTACKER. **Type:** Conditional attack mode.

**Rules:** A7.34; A9.5. **PDF pages:** 679, 56, 64. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Would spreading eligible fire across the permitted targets help?

- Check the weapon or squad’s Spraying Fire capability, target relationship, range and CA.
- Show the halved FP and separate target consequences; do not make this a universal MG or squad option.

**Checklist behavior:** Keep squad Spraying Fire visible even where the current implementation supports only a narrower attack set.

**Illustration:** Choose Spraying Fire / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, MG, IFT, CA.

#### Allocate SW and leader direction

ID: `weapons`. **Who:** ATTACKER. **Type:** Attack preparation.

**Rules:** A7.35; A7.53-.54; A9.1-.4; A21. **PDF pages:** 679, 56, 57, 63, 64, 88. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which weapons can this unit use while retaining any inherent fire?

- Apply squad/HS/crew/SMC weapon-use limits and the relationship to inherent FP; a SW shot is not free simply because it has a separate counter.
- Check captured and non-qualified use, crew-served requirements, ammunition shortage and relevant nationality adjustments.
- For MGs include mandatory direction and later ROF direction restrictions. Record the directing leader, not just a numeric bonus.

**Checklist behavior:** Maintain per-unit capability and per-weapon shots as related records.

**Illustration:** Allocate SW and leader direction / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SW, MG, ROF, SMC, MMC.

#### Choose target type and ammunition

ID: `ordnance`. **Who:** ATTACKER. **Type:** Optional ordnance attack.

**Rules:** C2-.8; C3.31-.33; C8.9. **PDF pages:** 679, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which legal target type and available ammunition fit this target?

- Offer only ammunition and target types provided by the actual weapon notes and scenario/date, while keeping unsupported options readable. Include HE, AP, HEAT, APCR/APDS and special types where present.
- Show range, CA, acquisition, size, movement, terrain and qualified-user effects. Resolve hits through IFT or TK as appropriate.
- Keep Critical Hits, Multiple Hits, dud/Depletion and malfunction branches distinct. Do not label every miss as an unchanged game state.

**Checklist behavior:** The attack record captures declared target type/ammo before dice and links resulting effects.

**Illustration:** Choose target type and ammunition / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TH, TK, ATT, ITT, VTT, HE, AP, HEAT, APCR, APDS.

#### Consider Canister, IFE and special attack modes

ID: `specialAmmo`. **Who:** ATTACKER. **Type:** Conditional weapon choices.

**Rules:** C2.29; C8.4; C5.7; C13; weapon notes. **PDF pages:** 679, 168, 173, 178, 182, 183, 184. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Does this weapon have a useful alternative to its ordinary shot?

- Represent Canister geometry and ammunition, IFE capability and its fire/ROF accounting, and Deliberate Immobilization eligibility as distinct choices.
- For LATW show PF availability, BAZ/PSK/PIAT/ATR restrictions, backblast or Desperation penalties and captured/non-qualified use as applicable.
- Weapon and nationality notes select the actual choices and modifiers. Pending IFE or a specific weapon path must not disappear from the guide.

**Checklist behavior:** Expose modes before commitment and show their separate risk, target restrictions and consequences.

**Illustration:** Consider Canister, IFE and special attack modes / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IFE, C, TH, TK, CA.

#### Designate a mortar’s original Spotter

ID: `mortarSpotter`. **Who:** ATTACKER. **Type:** Optional prerequisite.

**Rules:** C9.3-.31; ASOP 2.22A. **PDF pages:** 679, 179. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a mortar without an original Spotter need one for this attack?

- Designate an eligible original Spotter now, before ordinary mortar fire, using the same capability accounting as early spotting.
- Apply +2 spotted-fire TH DRM and reduced ROF where applicable. Pinned Spotter effects propagate to its spotted attacks.
- A lost original Spotter cannot be replaced here at will; the source gives replacement timing at the start of the owner’s MPh.

**Checklist behavior:** Keep early designation, ordinary original designation and later replacement as separate timing windows.

**Illustration:** Designate a mortar’s original Spotter / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MTR, LOS, ROF, SW.

#### Choose vehicle weapons and firing arcs

ID: `vehicleFire`. **Who:** ATTACKER. **Type:** Optional attacks with shared constraints.

**Rules:** D.3; D3.1-.5; D5.3; D6.6; G12.5. **PDF pages:** 679, 192, 200, 201, 202, 203, 207, 304. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which weapons can fire, and what will that do to movement and other weapons?

- List MA, SA and individual MGs with crew, exposure, Shock/Stun, Motion and CA eligibility. Do not use one generic fire-tank checkbox.
- Apply weapon-combination and turret/hull sequencing rules. A CA change for one attack can change another weapon’s modifiers.
- Prep Fire by the vehicle or its PRC has linked movement/fire consequences under D.3; preserve the specific unloading exception for unfired Passengers/Riders. Include LC special armament rules as pending.

**Checklist behavior:** Compare individual attacks while keeping vehicle-wide commitments visible.

**Illustration:** Choose vehicle weapons and firing arcs / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AFV, MA, SA, AAMG, BMG, CMG, CE, BU, TCA, VCA.

#### Use retained Rate of Fire

ID: `rof`. **Who:** ATTACKER. **Type:** Continuing opportunity.

**Rules:** A9.2-.21; C2.24; C3.33; C9.31. **PDF pages:** 679, 63, 168, 170, 179. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Did this weapon retain another normal shot, and is it worth taking?

- Use the correct colored die and effective ROF after all applicable reductions, including target type, spotting and AA-mode changes.
- Re-evaluate target, CA restrictions, leadership and concealment after each resolved shot. Retained ROF is permission for another decision, not automatic firing.
- A completed attack must not mark the weapon finished while its legal ROF opportunity remains. Stopping is allowed.

**Checklist behavior:** Create a new shot opportunity linked to the weapon and current phase, preserving the prior result.

**Illustration:** Use retained Rate of Fire / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** ROF, DR, MG, MTR, CA.

#### Consider Intensive Fire after normal ROF

ID: `intensive`. **Who:** ATTACKER. **Type:** Optional extra shot with risk.

**Rules:** C5.6-.63. **PDF pages:** 679, 172, 173. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Is another shot worth the extra malfunction risk?

- Require exhausted normal ROF, an eligible Gun, and remaining Intensive Fire capability. Respect No IF weapon notes.
- Preview +2 TH DRM and the B# reduction by two under A.11. Do not offer ordinary MG Sustained Fire as a generic Prep Fire extra shot.
- Place the correct fire marker and resolve permanent-loss or other weapon consequences as required.

**Checklist behavior:** Keep IF separate from ROF and explain the risk before rolling.

**Illustration:** Consider Intensive Fire after normal ROF / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IF, TH, B#, ROF.

#### Use a flamethrower or squad MOL attack

ID: `ftMol`. **Who:** ATTACKER. **Type:** Optional special attack.

**Rules:** A22.1-.6; B25.12. **PDF pages:** 679, 89, 90, 143. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Does the special weapon justify its range, availability and loss risk?

- Check FT eligibility/range and distinguish Personnel/vehicle resolution, relevant protection exceptions, and depletion.
- MOL requires its applicable SSR or nationality capability and a successful MOL Check; use its actual inherent-fire and target rules.
- Resolve any self-effect, Flame or burning-wreck result. A squad’s MOL is separate from the early MOL Projector timing.

**Checklist behavior:** Preview risks and possible fire consequences rather than showing only nominal FP.

**Illustration:** Use a flamethrower or squad MOL attack / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FT, MOL, IFT, TK, X#.

#### Throw a DC or detonate an existing Set DC

ID: `dc`. **Who:** ATTACKER. **Type:** Conditional special attack.

**Rules:** A23.6-.72; G1.6121. **PDF pages:** 679, 90, 91, 280. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Is there a legal thrown or previously Set demolition attack now?

- A Thrown DC uses its own range, elevation, attack and thrower-risk procedure; it is not MPh Placement.
- A previously Set DC can be detonated in a subsequent Player Turn’s friendly fire phase by its authorized Good Order unit/derivative HS, with the required distance, LOS and NTC.
- Resolve malfunction, attack and bridge/rubble consequences. Japanese A-T Set DC keeps its own trigger rules and is not a universal remote detonation.

**Checklist behavior:** Show who is authorized to trigger the charge and the prior-turn requirement before offering execution.

**Illustration:** Throw a DC or detonate an existing Set DC / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** DC, NTC, IFT, US#.

#### Sequence attacks for Encirclement

ID: `encirclement`. **Who:** ATTACKER. **Type:** Conditional attack-order opportunity.

**Rules:** A7.7-.72. **PDF pages:** 679, 57. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Could consecutive attacks establish Encirclement?

- Check eligible targets, ranges, directions and firepower under A7.7, including the pillbox and other exclusions.
- Qualifying attacks must be consecutive; an intervening attack on a different target can break the sequence.
- Keep ordinary and upper-level Encirclement distinct. Do not automatically apply a result just because two units fired at one hex.

**Checklist behavior:** Warn when a proposed unrelated shot would abandon an identified sequence, without choosing tactics for the player.

**Illustration:** Sequence attacks for Encirclement / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, LOS, Encircled.

#### Declare Opportunity Fire

ID: `opportunity`. **Who:** ATTACKER’s eligible Infantry/Cavalry. **Type:** Expiring commitment choice.

**Rules:** A7.25; C9.3; E7.5. **PDF pages:** 679, 55, 179, 239. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Would firing later at full effect be better than firing or moving now?

- Require Good Order and no prior fire/TI this Player Turn. Vehicles cannot use ordinary Opportunity Fire. Place Bounding Fire and apply concealment loss when required.
- The unit cannot fire in PFPh or move in MPh. In AFPh it can fire without the usual AFPh penalty and may use permitted ROF/IF.
- Infantry-manned ordnance cannot change CA until its AFPh TH attempt. A mortar needs the relevant LOS/Spotter condition; a weapon in AA mode cannot be designated.

**Checklist behavior:** Show the deferred AFPh opportunity and movement cost immediately, with no target-selection shortcut that bypasses the rule.

**Illustration:** A mortar with a Spotter reserves full-effect AFPh fire. Its CA is not freely changed now, and the reservation remains even if later events spoil the shot.

**Terms:** AFPh, MPh, ROF, IF, CA, MTR.

#### Use a later Starshell opportunity

ID: `starshellLater`. **Who:** ATTACKER. **Type:** Conditional later window.

**Rules:** E1.91-.922; ASOP 2.22A. **PDF pages:** 679, 226, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the current illumination history still permit a Starshell attempt now?

- Leaders have the within-phase timing exception. MMC/CE AFV retain later timing only under the initial-illumination conditions.
- Use the same per-hex attempt record as the opening window. A failed opening attempt does not create a fresh later attempt.

**Checklist behavior:** Keep the eligibility explanation tied to scenario illumination history.

**Illustration:** Use a later Starshell opportunity / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IR, dr, MMC, CE, SMC.

#### Fire Heavy AA at an aerial target

ID: `heavyAA`. **Who:** ATTACKER. **Type:** Special attack.

**Rules:** E7.5; E7.52; W9.13. **PDF pages:** 679, 239, 667. **Coverage:** Rules integration pending; reference available.

**Consider:** Does an eligible Heavy AA Gun have a useful aerial target?

- Only the qualifying AA Gun without printed IFE uses Heavy AA; this is not ordinary Light AA fire at any convenient time.
- Validate aerial targets, including Observation Planes and applicable airborne FAC. Resolve the Heavy AA elimination/damage/prevention outcomes, normal malfunction and ROF.
- Update AA mode and its one-attack ROF adjustment as required.

**Checklist behavior:** Offer an aerial-target workflow with its actual resolution, separate from ground fire.

**Illustration:** Fire Heavy AA at an aerial target / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AA, TH, ROF, IFE, FAC.

#### Assemble or dismantle a weapon

ID: `assemble`. **Who:** ATTACKER’s possessor. **Type:** Optional weapon-use commitment.

**Rules:** A9.8. **PDF pages:** 679, 65. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Should the weapon be ready to fire later or easier to carry later?

- Validate eligible weapon types and nationality exceptions, possession, and that the weapon has not fired.
- Conversion counts as using the SW and all of its ROF; enforce the unit’s weapon-use capacity.
- Do not promise that dismantling now allows the possessor to move in this MPh. Show the actual fire/use commitment and resulting PP.

**Checklist behavior:** Separate conversion success from later fire or movement eligibility.

**Illustration:** Assemble or dismantle a weapon / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** dm, SW, PP, ROF.

#### Destroy or deliberately malfunction equipment

ID: `destroy`. **Who:** ATTACKER’s possessor or inherent crew. **Type:** Optional irreversible game action.

**Rules:** A9.73. **PDF pages:** 679, 65. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Would denying this equipment justify losing its use?

- The weapon and possessing unit must still otherwise be able to fire it. Count only as many weapons as the unit could use.
- Offer destruction and deliberate malfunction as distinct outcomes, each instead of firing. Include vehicular weapons.
- Show any resulting vehicle Recall and later repair implications; do not treat permanent destruction as a repairable malfunction.

**Checklist behavior:** Preview the exact equipment state change before executing, while a reference acknowledgement remains harmless.

**Illustration:** Destroy or deliberately malfunction equipment / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SW, MA, ROF, Recall.

### 2.23A · Limber and unlimber

#### Limber a Gun for later movement

ID: `limber`. **Who:** ATTACKER’s eligible crew. **Type:** Optional preparation.

**Rules:** C10.111; C10.21-.23. **PDF pages:** 679, 180, 181. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this Gun prepare for towing instead of firing?

- Check whether the Gun requires/can use limbering, its possession and eligible crew. QSU and NM/RFNM cases are not interchangeable.
- Neither Gun nor crew may fire in the Player Turn of (un)limbering. Limbering does not itself make them TI, unlike unlimbering. Show the planned later hookup separately.
- Any simultaneous CA adjustment follows the (un)limbering exception, not a free general CA change.

**Checklist behavior:** Compare fire now with the actual remaining towing sequence; limbering does not itself hook up a vehicle.

**Illustration:** Limber a Gun for later movement / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TI, Gun, MPh, QSU.

#### Unlimber a Gun

ID: `unlimber`. **Who:** ATTACKER’s eligible crew. **Type:** Optional TI commitment.

**Rules:** C10.111; C10.21-.23. **PDF pages:** 679, 180, 181. **Coverage:** Rules integration pending; reference available.

**Consider:** Is preparing this Gun for future fire worth committing it and the crew now?

- Check Gun type, possession, crew and current limbered state.
- Apply TI to the Gun and crew as required. Do not offer an immediate normal shot just because the counter is now unlimbered.
- Show allowed simultaneous CA selection and the later turn/phase in which the weapon can next act.

**Checklist behavior:** Track preparation and readiness separately.

**Illustration:** Unlimber a Gun / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TI, Gun, CA, QSU.

### 2.31A · Final CA and AA-mode choices

#### Change Gun CA without firing

ID: `caEnd`. **Who:** ATTACKER. **Type:** Optional end-phase window.

**Rules:** C3.22; D3.51; A7.25. **PDF pages:** 679, 169, 201, 55. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Would repositioning the firing arc now improve a later shot?

- Require the crew to remain able to fire the Gun without Intensive/Sustained Fire at phase end, with the explicit exceptions. Retained ROF may preserve this option.
- Changing CA here prevents movement of the Gun and crew for the rest of the Player Turn, but does not by itself prevent AFPh fire.
- Do not allow an Opportunity-Fire ordnance reservation to evade A7.25’s CA restriction.

**Checklist behavior:** Show a separate movement commitment without falsely marking an attack as fired.

**Illustration:** An unfired Gun changes CA now, gives up movement, and may fire in AFPh under its actual AFPh restrictions.

**Terms:** CA, TCA, VCA, IF, AFPh.

#### Designate or cancel AA mode

ID: `aaModeEnd`. **Who:** ATTACKER. **Type:** Optional end-phase window.

**Rules:** E7.5; C3.22. **PDF pages:** 679, 239, 169. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this weapon be prepared for aerial or ground targets?

- Use the end-fire-phase CA eligibility criterion even for a weapon that is not a Gun.
- Keep AA-capable weapon rules, vehicular AAMG exceptions and Opportunity-Fire incompatibility explicit.
- Changing mode by making an attack has different timing and ROF consequences from this end-phase choice.

**Checklist behavior:** Do not present mode switching as an always-free toggle.

**Illustration:** Designate or cancel AA mode / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AA, CA, ROF, AAMG.

### Supplemental timing and special rules

#### Establish or maintain a searchlight beam

ID: `searchlight`. **Who:** ATTACKER under Chapter W. **Type:** Opening illumination procedure.

**Rules:** W10.2-.5; W10.45-.51. **PDF pages:** 668, 669, 670. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a searchlight need a new target or a maintenance decision before firing starts?

- Apply initial-use triggers and the Starshell-equivalent per-hex attempt rule. Later establishment belongs at the start of friendly PFPh under W10.51.
- For an existing beam, choose maintenance with its Reliability DR, or let it cancel and consider a different target. Show malfunction/disablement risk.
- A new target uses Sighting TC and the beam’s LOS, CA and illumination rules. Apply the correct fire marker and remaining capability.

**Checklist behavior:** Place this next to opening illumination in a live checklist, labeled with its Chapter W timing rather than inventing a printed ASOP substep.

**Illustration:** Establish or maintain a searchlight beam / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SL, IB Lane, TC, NVR, DR.

#### Voluntarily cancel a searchlight beam

ID: `cancelBeam`. **Who:** Owning side under Chapter W. **Type:** Standing conditional choice.

**Rules:** W10.52. **PDF pages:** 670. **Coverage:** Rules integration pending; reference available.

**Consider:** Would cancelling the beam change visibility or exposure usefully?

- Cancellation is available during the owner’s PFPh under its rule, and at other times only under the separately specified conditions.
- Remove the beam and update applicable Gunflash/illumination effects. This is not repairing or re-aiming the light.

**Checklist behavior:** Recompute disclosed visibility without counting cancellation as a new Sighting attempt.

**Illustration:** Voluntarily cancel a searchlight beam / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SL, IB Lane, Gunflash.

#### Conduct Korean War Close Air Support

ID: `closeAir`. **Who:** UN ATTACKER; DEFENDER responds. **Type:** After all FFE, when applicable.

**Rules:** W9.1-.22; E7.3-.5. **PDF pages:** 666, 667, 668. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a qualifying FAC-directed aircraft have a legal PFPh attack?

- Use the Chapter W exception immediately after all FFE. Ordinary Chapter E air support is not thereby granted PFPh attacks.
- Require FAC/TACP direction and its Contact, LOS and target limitations. An aircraft cannot conduct Ground Support in two consecutive Player Turns.
- Expose DEFENDER Light AA, mistaken-attack handling and immunity zones. Resolve the source-specific Residual FP and other consequences.

**Checklist behavior:** This creates a real defender response window inside Prep Fire, despite ordinary Prep Fire attacks belonging to ATTACKER.

**Illustration:** Conduct Korean War Close Air Support / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TACP, FAC, TC, AA, Residual FP.

#### Apply Naval OBA mission exceptions

ID: `naval`. **Who:** Affected battery owner. **Type:** Mission-specific overlay.

**Rules:** G14.6-.68. **PDF pages:** 315, 316, 317. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this naval mission differ from ordinary OBA?

- Apply SFCP/Observer, Access, Blast Area, naval line-of-fire and Reverse Slope rules to the corresponding mission steps.
- Keep the FFE:C new-AR requirement visible.
- A qualifying NOBA battery of at least 150mm can fire IR with its HE/WP mission under G14.672, using its placement restriction. Do not apply the ordinary OBA IR prohibition without this exception.

**Checklist behavior:** Attach this profile to the individual battery; do not provide disconnected generic naval buttons.

**Illustration:** Apply Naval OBA mission exceptions / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** NOBA, IR, FFE:C, SFCP.

#### Apply nationality, terrain and equipment restrictions

ID: `nationality`. **Who:** Affected side. **Type:** Eligibility and modifier overlay.

**Rules:** A19; A21; A25; G.3; G11.8; G12.5; W7.41; Chapter H notes. **PDF pages:** 86, 88, 94, 97, 276, 298, 304, 663, 664. **Coverage:** Rules integration pending; reference available.

**Consider:** Which specific rule changes this unit’s permitted attack?

- Check nationality, elite status, captured/non-qualified use, ammunition shortage and the weapon’s listing/date/notes. Make unsupported configurations visible with unknown eligibility.
- Include PTO multi-hex FG restrictions, cave firing/SMOKE and LC armament conditions.
- Qualifying Good Order CPVA squad/HS attacks at non-ADJACENT targets in PFPh or using Opportunity Fire are Area Fire unless directed by a leader under W7.41. Preserve the Recon Unit exception.

**Checklist behavior:** Modifiers must attach to the proposed shot. Chapter H equipment entries and scenario SSR are source profiles, not a claim that every weapon uses one generic rule.

**Illustration:** Apply nationality, terrain and equipment restrictions / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** CPVA, FG, ROF, B#, SSR.

#### Resolve the prisoner-elimination rule

ID: `massacre`. **Who:** Eligible ATTACKER units. **Type:** Optional or compulsory special case.

**Rules:** A20.3-.4; A15.43. **PDF pages:** 87, 84. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Does A20.4 apply in this Location?

- Show the exact nationality/status and no-Melee restrictions. The optional action counts as SW use.
- The berserk-unit-with-prisoners case has a compulsory start-of-fire-phase result and loss of berserk status; it is an exception to the usual no-Prep-Fire guidance for berserk units.
- Apply No Quarter/Mopping Up restrictions and the once-only enemy ELR increase as specified.

**Checklist behavior:** Use the rule’s neutral description and consequences; neither hide the option nor suggest it as a tactic.

**Illustration:** Resolve the prisoner-elimination rule / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** ELR, SW, Melee, Berserk.

#### Consider standing choices and ongoing commitments

ID: `standing`. **Who:** As the specific rule permits. **Type:** Conditional cross-phase choices.

**Rules:** A12.14; A20.5; B9.32-.323; E10.2-.4. **PDF pages:** 78, 87, 119, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Will this action affect an ongoing obligation or standing choice?

- Retain voluntary concealment loss and automatic Guard Deployment where their own rules permit them. Do not turn prisoner transfer into a PFPh choice.
- Apply voluntary/mandatory WA changes at their legal windows, never retroactively after an attack is declared to improve its defense.
- Firing can interrupt replenishment. Using an Ammo Vehicle’s Prep Fire benefit restricts that vehicle’s subsequent movement; show the linked commitment.

**Checklist behavior:** Cross-phase records persist into Prep Fire and update when an actual event changes them.

**Illustration:** Consider standing choices and ongoing commitments / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** HIP, WA, TI, Ammo Vehicle.

### Consequences and response windows

#### Resolve attack results before the next decision

ID: `attackResults`. **Who:** Both sides as affected. **Type:** Required resolution chain.

**Rules:** A7.3; A9.7; A10; A15; A17; C3.7-.8; C7; D.8. **PDF pages:** 55, 56, 65, 66, 67, 69, 83, 84, 170, 171, 176, 177, 193. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Which required results remain from this shot?

- Resolve random selection, casualties, MC/PTC, wounds, leader-loss effects and applicable HOB or special morale changes in their proper sequence.
- Resolve collateral effects, Shock/Stun/Recall, burning wrecks, equipment destruction, Cowering, malfunction and ammunition depletion where triggered.
- Update concealment, Control/WA and capability only from authorized results. Never mark the attack finished while a dependent choice remains.

**Checklist behavior:** Separate attacker choices from defender resolution, keep private information private, and reopen only newly created opportunities.

**Illustration:** Resolve attack results before the next decision / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MC, PTC, KIA, K, HOB, LLMC, LLTC, DM, CH.

#### Resolve Sniper attacks and checks

ID: `sniper`. **Who:** Both sides according to trigger. **Type:** Triggered response.

**Rules:** A14.1-.4; E1.72; E1.76; G12.603; G14.261; ASOP dagger. **PDF pages:** 82, 83, 225, 305, 314, 678. **Coverage:** Partially integrated; full rule path still needs verification.

**Consider:** Did a qualifying Original DR activate a Sniper?

- PFPh is Sniper-capable. Check qualifying TH, MC, TC, non-OBA IFT and Entrenching DR, with the Prisoner and other exclusions. Do not treat every DR or dr as a trigger.
- Finish the triggering roll’s required effects before the Sniper attack, and respect chained-result ordering. Offer the permitted Sniper Check when its conditions arise.
- Include night, LC/seaborne exceptions. ASOP references RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13; their complete module text needs its own source profile.

**Checklist behavior:** The response can change later eligibility, so rerun affected opportunities after resolution.

**Illustration:** Resolve Sniper attacks and checks / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SAN, TH, MC, TC, DR.

#### Resolve a triggered Field Phone cut

ID: `phoneCut`. **Who:** Affected phone owner. **Type:** Triggered consequence.

**Rules:** C1.23. **PDF pages:** 163. **Coverage:** Rules integration pending; reference available.

**Consider:** Did an opposing unit’s Original 2 trigger a phone-security consequence?

- Check the specified security-area and opposing-unit conditions for an Original 2 DR for any purpose.
- Keep phone information private, and distinguish a cut/non-repairable phone from a malfunctioned repairable radio.

**Checklist behavior:** Link this to the triggering roll, rather than inserting a voluntary phone-cutting action.

**Illustration:** Resolve a triggered Field Phone cut / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, DR, HIP.

### Actions belonging elsewhere

#### Keep later-phase actions in their proper windows

ID: `notHere`. **Who:** Reference. **Type:** Timing clarification.

**Rules:** A4.44; A8; A9.72; A23.3; A24.1; B24.7; C10.1; E7.4. **PDF pages:** 50, 58, 65, 90, 91, 142, 180, 237, 238. **Coverage:** Rules integration pending; reference available.

**Consider:** Are you looking for something that belongs to another phase?

- Normal SW Recovery/Transfer/repair are not generic Prep Fire actions. MPh smoke grenades, vehicle smoke dispensers and placed DC keep their own windows.
- Clearance declarations, Gun hookup/unhooking and Pushing normally belong in MPh; entrenching, Mopping Up and Kindling have the PFPh windows described here.
- Defensive First Fire, ordinary Fire Lanes, Subsequent First Fire and FPF are not ordinary Prep Fire attacks. Ordinary air Ground Support has its own timing; the Chapter W CAS exception is separate.
- Do not remove Prep Fire, Bounding Fire or TI simply because PFPh ends; use their actual later removal rules.

**Checklist behavior:** Offer a future-phase reminder, not an invalid action button.

**Illustration:** Keep later-phase actions in their proper windows / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MPh, DFPh, AFPh, SW, ROF.

## Contextual vocabulary

Use exact Acronym Index identities where they exist. These contextual labels must not create false acronym collisions: DR/dr, DM/dm, SL (Searchlight versus Squad Leader), and C (Canister) require context. Plain concepts remain plain concepts. Acronym identity binding is an implementation task.

| Label | Meaning here |
|---|---|
| AA | Anti-Aircraft fire or mode. |
| AAMG | Anti-Aircraft Machine Gun. |
| AFPh | Advancing Fire Phase. Opportunity Fire reserves a full-effect attack here. |
| AFV | Armored Fighting Vehicle. |
| Ammo Vehicle | Ammunition-supply vehicle with linked firing and replenishment commitments. |
| AP | Armor Piercing ammunition. |
| APCR | Armor Piercing Composite Rigid ammunition. |
| APDS | Armor Piercing Discarding Sabot ammunition. |
| AR | Artillery Request marker, placed before Accuracy resolution. |
| ATT | Area Target Type. |
| B# | Breakdown Number, with relevant reductions applied under the actual rule. |
| Barrage | An artillery pattern; a Creeping Barrage additionally follows recorded progression. |
| Battery Access | Permission to use the battery, determined by its access procedure. |
| Berserk | Special morale state; ordinary Prep Fire is prohibited, with specific rule exceptions. |
| Blaze | A continuing fire state, distinct from a Flame and from its smoke counter. |
| BMG | Bow Machine Gun. |
| BU | Buttoned Up vehicle condition. |
| C | Canister ammunition, in this ordnance context. |
| CA | Covered Arc. |
| CE | Crew Exposed vehicle condition. |
| CH | Critical Hit. |
| CMG | Coaxial Machine Gun. |
| Column | A formation with specific disbandment rules. |
| Control | Rules-defined ownership of Locations or buildings, not simply current occupancy. |
| CPVA | Chinese People’s Volunteer Army. |
| DC | Demolition Charge. |
| DFPh | Defensive Fire Phase, distinct from defensive responses during Prep Fire. |
| dm | Dismantled weapon state, distinct from DM. |
| DM | Desperation Morale, distinct from dismantled dm. |
| dr | One die, distinct from a two-dice DR. |
| DR | Two-dice roll. Keep Original, modified and individual colored/white values. |
| DRM | Modifier to a two-dice roll. A drm modifies a single die. |
| EC | Environmental Conditions. |
| ELR | Experience Level Rating. |
| Encircled | State established by qualifying fire or other specific rules. |
| FAC | Forward Air Controller in the applicable Chapter W rules. |
| FFE | Fire For Effect. FFE:1, FFE:2 and FFE:C have different actions. |
| FFE:C | Continuation marker; it is not an attacking FFE. |
| FG | Fire Group, subject to formation and mandatory grouping rules. |
| FP | Firepower. |
| FT | Flamethrower. |
| Gun | Rules-defined ordnance, not every weapon colloquially called a gun. |
| Gunflash | Night firing signature, governed by its own placement/removal rules. |
| Harassing Fire | OBA mission with different area and firepower from a Concentration. |
| HE | High Explosive ammunition or mission. |
| HEAT | High Explosive Anti-Tank ammunition. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| HOB | Heat of Battle, a triggered resolution rather than an optional action. |
| IB Lane | Illuminating Beam Lane created by a searchlight. |
| IF | Intensive Fire, an extra Gun shot with accuracy and malfunction penalties. |
| IFE | Infantry Firepower Equivalent, a weapon-specific alternative to ordinary TH fire. |
| IFT | Infantry Fire Table. |
| IR | Illuminating Round, with its own usage and placement procedure. |
| ITT | Infantry Target Type. |
| K | Casualty Reduction result, distinct from KIA. |
| KIA | Killed In Action attack result. |
| Labor | Progress modifier/status from eligible unsuccessful labor attempts. |
| LLMC | Leader-Loss Morale Check. |
| LLTC | Leader-Loss Task Check. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| MA | Main Armament. |
| MC | Morale Check. |
| Melee | Close-combat state restricting many actions. |
| MG | Machine Gun. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| MOL | Molotov Cocktail. A squad MOL and a MOL Projector have different procedures. |
| Motion | Vehicle state. It changes firing eligibility and Vehicle Dust timing. |
| MPh | Movement Phase. Several Prep Fire commitments prevent subsequent movement. |
| MTR | Mortar. |
| NMC | Normal Morale Check. |
| NOBA | Naval Offboard Artillery. |
| NTC | Normal Task Check. |
| NVR | Night Visibility Range. |
| OBA | Offboard Artillery. |
| PBF | Point Blank Fire. |
| PFPh | Prep Fire Phase, the ATTACKER’s early fire and preparation window. |
| PP | Portage Points. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| PTC | Pin Task Check. |
| QSU | Quick Set Up, affecting Gun handling. |
| Radio Contact | Communication state, separate from Battery Access. |
| Recall | Vehicle withdrawal obligation. |
| Residual FP | Firepower left for qualifying later movement attacks; not an ordinary Prep Fire byproduct. |
| ROF | Rate of Fire. A retained opportunity is not an automatic shot. |
| s# | Smoke Depletion Number on the actual weapon. Distinguish s from WP availability. |
| SA | Secondary Armament. |
| SAN | Sniper Activation Number, tested only for qualifying Original rolls. |
| SFCP | Shore Fire Control Party for NOBA. |
| SL | Searchlight in this Chapter W context; not the Squad Leader game abbreviation. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| SMOKE | The umbrella for Smoke and WP effects; preserve ammunition type, owner and source. |
| SR | Spotting Round marker. |
| SSR | Scenario Special Rule. Unknown content is a source gap, not assumed inapplicable. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| TACP | Tactical Air Control Party. |
| TC | Task Check. |
| TCA | Turret Covered Arc. |
| TEM | Terrain Effects Modifier. |
| TH | To Hit roll or number for an ordnance attack. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| TK | To Kill resolution, distinct from the To Hit roll. |
| TPBF | Triple Point Blank Fire. |
| US# | Unit Size Number, used in the Set DC procedure. |
| VCA | Vehicle Covered Arc. |
| VTT | Vehicle Target Type. |
| WA | Wall/Hedge Advantage. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| X# | Permanent weapon-loss/depletion threshold where specified. |

## Acceptance examples

- First non-SMOKE fire closes the appropriate side-wide window, while exceptions follow their own rules.
- An opening Starshell attempt cannot be repeated as a later attempt by another unit in the same hex.
- Failed IR Usage does not equal fired IR; fired IR suppresses ordinary other-ammunition choices.
- FFE:C follows its Access/continuation branch; NOBA uses its distinct new-AR requirement.
- A lost original Spotter cannot be replaced prematurely in PFPh.
- Retained ROF produces another opportunity; IF is offered only with the correct eligibility and risk.
- A failed entrenchment still records TI/Labor; Kindling uses its own Prep Fire accounting.
- Opportunity Fire preserves full-effect AFPh fire while excluding movement and the premature ordnance CA change.
- An end-phase CA change can prevent movement without being recorded as a fired attack.
- Chapter W CAS exposes a defender response; ordinary Chapter E aircraft do not gain that PFPh option.
- Hidden defenders, phone positions and SSR information are never leaked through counts or explanations.
- Pending rules stay in the all-options view and cannot be executed or cleared as game obligations.

The [machine-readable catalog](ASL%20Prep%20Fire%20Phase%20Checklist%20Catalog.json) is the shared source for the detailed preview and this walkthrough. Production work must bind these records to Rules-derived capabilities and typed citations, validate weapon/nationality/scenario profiles, and resolve the recorded external source gaps.
