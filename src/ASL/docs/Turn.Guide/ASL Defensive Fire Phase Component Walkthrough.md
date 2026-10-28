# ASL Defensive Fire Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design content and reference preview. No Blazor component or Rules behavior is implemented here.

Design target: the completed ASL.Rules implementation after all passes. Pending rules and deferred chapter families remain visible. See the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md), [Movement walkthrough](ASL%20Movement%20Phase%20Component%20Walkthrough.md), and [Defensive Fire catalog](ASL%20Defensive%20Fire%20Phase%20Checklist%20Catalog.json).

## Sources and reconciliation scope

Authority: supplied eASLRB v3.01 at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. All eight printed DFPh ASOP steps are on physical PDF page 680; the shared dagger note is on page 678. Page numbers here are one-based physical PDF pages. The printed ASOP layout was visually checked.

The review follows all ASOP action families and checks their linked procedures, especially A.15, A8.4-.41, A9.3, C2.241, C5.6, B24.7, C3.22, E1.8, E7, E8.3 and E12.731. A whole-volume DFPh/Defensive Fire Phase search found 150 hits across 84 pages, including repeated charts and training examples; those are discovery hits, not distinct audited actions. Shared firing and artillery explanations reuse the earlier Prep Fire reconciliation only after role, timing and commitment differences are reviewed.

The catalog has 72 topics and 116 contextual vocabulary labels. It covers the printed ASOP and identified in-volume families, including specific Chapter H configuration options and Chapter W searchlight cancellation. It does not claim an exhaustive inventory of every SSR, module or equipment note. Named ASOP Sniper exceptions RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 remain external-source gaps. Unknown applicability is not false.

## The Defensive Fire interaction model

The central question is “What remains after Movement?” A.15 links defensive firing across MPh and DFPh; the component must not reset prior weapon use, leadership, CA changes or equipment-note commitments. DEFENDER conducts ordinary firing, but both sides resolve results, and ATTACKER can have Light AA decisions against DEFENDER Ground Support. Phase owner, viewer and decision owner remain separate.

### Capability comparison

| Starting state | What the guide offers | What it must not imply |
|---|---|---|
| Eligible unmarked unit/weapon | Normal fire against legal targets; retained ROF when applicable | “Fired once in MPh” necessarily means spent |
| First-Fired inherent FP | Restricted adjacent/same-hex Final Fire as Area Fire, with applicable PBF/TPBF | A fresh normal shot, or MPh SFF/FPF permission |
| First-Fired MG/IFE | Eligible restricted Sustained Fire with breakdown and use consequences | Retained ROF from a low colored die |
| First-Fired Gun using ordnance | Eligible adjacent/same-hex IF under A8.41/C2.241/C5.6 | A normal shot at a distant target |
| Final/Intensive/No Fire | Explanation of exhausted capability and other genuinely eligible choices | Another ordinary fire action or a marker reset on phase entry |
| Missing evaluator | Readable rule and Availability not evaluated | Illegal, exhausted or hidden from the all-options catalog |

This is a guide to Rules projections, not a new rules engine. Unit and weapon states must be assessed separately, with Pin, TI, operator, crew and specific exceptions. In particular, do not impose PFPh’s ordinary Good Order wording on every DFPh attack: berserk firing and pinned firing require their own rules.

### Four design improvements

1. **Carry-forward explanation:** show which MPh actions constrain each weapon now. A squad, its retained-ROF HMG and its leader can have different remaining capabilities.
2. **Changed-versus-retained modifiers:** FFNAM/FFMO disappear; vehicle moving-target history, appropriate CA penalties, terrain and Hazardous Movement do not automatically disappear. Final Fire can affect stationary co-occupants.
3. **Closing-window reminders:** review Dispersed SMOKE/IR/MOL Projectors before ordinary fire, then due artillery; later compare firing with Clearance/equipment commitments; finish CA/AA choices before removing daytime markers.
4. **Consequences beyond this phase:** a Clearance declaration schedules CCPh work, Hampering changes AFPh Flame spread, attacker AA can consume later fire, and night Gunflash remains tied to its original Location. A considered topic is not a resolved action.

### Worked decision chain

The defender reviews a First-Fired squad, a retained-ROF HMG, an unfired mortar and a Final-Fired Gun. It first considers the mortar’s legal opening Dispersed SMOKE or IR opportunity, including the Spotter and weapon commitments. It resolves applicable contact, Access, mission changes and due FFE. The squad can consider a qualifying adjacent Final Fire attack; the HMG’s actual unmarked state can permit normal fire at a farther target; the Final-Fired Gun does not receive another shot.

An unfired unit can instead declare eligible Wire Clearance, creating TI and a later CCPh result. Another eligible unit attempts Flame Clearance immediately; a qualifying result can Hamper rather than extinguish the Flame. If aircraft attack, the attacker’s AA response interrupts the sequence at its prescribed point. At the end, eligible CA/AA-mode choices occur before counter cleanup. Daytime First/Final Fire counters are removed; at night they remain in their original Locations until the specified AFPh step.

### Boundaries that need explicit explanation

- First-Fired Final Fire is not SFF or FPF. No FPF morale test or old movement expenditure is invented. Ground Final Fire does not create a generic new MPh residual/fire-lane procedure; explicit aircraft/special rules retain their own treatment.
- A recorded FFE:1 Creeping Barrage does not correct in DFPh, but its due FFE:2 still resolves if it has not Lifted. Recorded FFE:1-2 requires the applicable correction.
- DFPh Clearance includes Wire, Set DC, roadblock, Path and Panji declarations, plus immediate Flame attempts. Rubble/minefield declarations, entrenchment, Kindling and Mopping Up are not imported from other phase menus.
- Changing a Gun’s CA under C3.22 is decided before marker removal. PFPh’s special movement-cancellation clause is not copied as a DFPh rule.
- A landed glider is a specific target before ordinary contents placement in AFPh. New Placed DC detonation and Flame/Blaze spread also wait for their actual AFPh windows.
- Named equipment options include AVRE fascine discard, M12/listed SP-Gun preparation and Skoda barrel changes. Direct/Indirect Fire and facing restrictions in equipment notes can span MPh plus DFPh.

## Integration evidence

Labels are conservative summaries of [ASL Rule Coverage](../ASL%20Rule%20Coverage.md) and its pass amendments, not new game-engine test results. Existing partial foundations include Final Fire, ROF, ordnance, vehicle fire and attack/Sniper results. C1 Contact, Access and mission handling remain pending; shared mortar/vehicle resolution is not evidence of an implemented OBA mission path. The inventory records a suspected A8.41 range check based on the unit marker rather than the First-Fired weapon marker. It also records that Gunflash currently follows the firer rather than remaining in its original Location. These are recorded coverage findings, not newly reproduced defects; the guide targets the source-backed final behavior. Detailed labor, night, aircraft, special terrain and equipment profiles remain pending unless a verified complete action path is available. The current A-E pass program must not be described as implementing every F/G/H/W rule. The reference preview exposes all cataloged choices without pretending to execute or evaluate them.

## Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 4.11D | Ordnance Dispersed SMOKE; mortar IR; WP NMC; MOL Projector; Flame; Final/Intensive/No Fire/Gunflash markers. | `smokeFire`, `wpEffects`, `mortarIR`, `earlySpotter`, `molProjector` |
| 4.12D | Contact and exceptions; aerial Sighting TC; Access; AR/SR/Pre-Reg FFE/IR; rockets; FFE:C remove/replace. | `radio`, `access`, `newMission`, `obaIR`, `aerialObserver`, `continuation`, `rockets` |
| 4.13D | Correct/convert/cancel SR/FFE; mandatory recorded FFE:1-2 Creeping correction; FFE:1 restriction; rocket Correction exclusion. | `correct`, `barrage` |
| 4.14D | OBA Dispersed SMOKE/IR/WP; HE FFE; shellhole/Flame/rubble; Wire/roadblock/pillbox; mines/Panji; sangar/trip flare; Column; Reverse Slope. | `obaSmokeResolution`, `ffe`, `terrainEffects`, `columnReverseSlope`, `naval` |
| 4.21D | Original mortar Spotter; unmarked fire and restricted First-Fired Final Fire; all listed markers; Heavy AA; attack results; later Starshell; FB/DB sighting/Ground Support and ATTACKER Light AA; Wire/Set DC/roadblock/Path/Panji Clearance declarations; immediate Flame/Labor/Hamper; Column; destroy/malfunction/dm/assemble. | `unmarked`, `firstMarked`, `sustained`, `intensive`, `rof`, `spent`, `smallArms`, `spraying`, `weapons`, `ordnance`, `specialAmmo`, `ftMol`, `encirclement`, `vehicleFire`, `dc`, `mortarSpotter`, `sewerFire`, `landedGlider`, `starshellLater`, `groundSupport`, `lightAA`, `heavyAA`, `airResults`, `wire`, `clearDC`, `roadblock`, `path`, `panji`, `flame`, `columnFire`, `assemble`, `destroy` |
| 4.22D | Limber/unlimber; Gun and crew TI on unlimbering. | `limber`, `unlimber` |
| 4.31D | Eligible end-phase Gun CA change; AA-mode designation/cancellation. | `caEnd`, `aaModeEnd` |
| 4.32D | Daytime First/Final Fire removal; night retention under E1.8. | `cleanup` |

Supplemental entries retain rule-specific timing rather than inventing ASOP step numbers. Triggered results, the dagger’s Sniper procedures and aircraft departure obligations attach to their originating action.

## Cataloged options

### Carry forward the Movement fire record

#### Review what each unit and weapon can still do

ID: `handoff`. **Who:** DEFENDER. **Type:** Phase handoff.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A.15; A8.4-.41; C2.241; C5.6. **PDF pages:** 680, 44, 61, 168, 172, 173. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which capabilities remain after Movement?

- Keep inherent FP, each weapon, leader direction, prior ROF and fire markers separate. Do not clear First/Final Fire at the start of DFPh.
- Unmarked eligible units/weapons may fire normally. First-Fired ones have restricted adjacent/same-hex opportunities. Final/Intensive/No Fire markers bar further normal fire.
- Track the applicable Gun, MG and IFE exceptions rather than applying one unit-wide “shot left” count. TI, broken, Pin, crew and weapon conditions still matter.

**Checklist behavior:** Show a capability record for each subject: normal fire, restricted Final Fire, or no further fire, with the source and remaining choices. Unknown evaluation is not “cannot fire”.

**Illustrative position:** A squad is First-Fired, its HMG retained ROF, and a nearby Gun is Final-Fired. Those are three different DFPh states.

**Terms:** DFPh, First Fire, Final Fire, ROF, IF.

#### Reassess targets and modifiers after movement

ID: `targets`. **Who:** DEFENDER. **Type:** Target and exposure review.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A8.4; C.8; C6.1; A4.62; D9.31. **PDF pages:** 680, 59, 61, 162, 173, 174, 210. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What changes when firing is no longer tied to an expenditure?

- Final Fire affects all applicable units in the target Location, including stationary occupants; ordinary attacks remain subject to target and protection rules.
- Do not apply FFNAM or FFMO. Retain applicable vehicle moving-target history, Motion, Hazardous Movement, concealment and terrain. Under C6.16, DFPh uses applicable Case J, not the MPh-only J1/J2 limited-LOS subcases.
- Reassess any Armored Assault protection and actual LOS. Do not infer that every unit which stopped is now a stationary vehicle target.

**Checklist behavior:** Present changed modifiers alongside retained ones. Ending Movement is not a reset of the attack history.

**Illustrative position:** A stationary squad sharing the target Location is now affected along with the squad that moved there.

**Terms:** FFNAM, FFMO, TH, TEM, Hazardous Movement.

#### Fire, perform another eligible activity, or hold

ID: `hold`. **Who:** DEFENDER. **Type:** Optional phase choice.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A7.1; A8.4; B24.7; C10.21. **PDF pages:** 680, 54, 61, 142, 180. **Coverage:** Rules integration pending; reference available.

**Consider:** Which remaining opportunity is worth using?

- Consider fire against current positions, applicable labor, equipment handling and final CA/AA choices independently.
- Holding fire is a valid decision. Optional unreviewed topics do not block phase completion; required result chains still do.
- Do not offer a new Opportunity Fire declaration here, or promise the DEFENDER an ordinary AFPh attack after holding.

**Checklist behavior:** Distinguish declining a shot from exhausting a weapon or resolving an obligation.

**Illustrative position:** A crew may prefer eligible preparation to an uncertain shot, while another unit simply holds fire.

**Terms:** DFPh, TI, SW.

### 4.11D · Dispersed SMOKE and illumination first

#### Fire ordnance Dispersed Smoke or WP

ID: `smokeFire`. **Who:** DEFENDER. **Type:** Expiring opening window.

**Timing:** ASOP 4.11D. **Rules:** C8.5-.6; A24.5; A8.4-.41. **PDF pages:** 680, 61, 168, 178, 179. **Coverage:** Rules integration pending; reference available.

**Consider:** Should an eligible weapon place Dispersed SMOKE before other ammunition is fired?

- Use the DFPh Dispersed counter and actual weapon ammunition, target, depletion and weather/terrain restrictions.
- First-Fired weapons do not gain unrestricted opening shots: apply A8.4-.41 and C2.241, including adjacent/same-hex and IF conditions where relevant.
- Once friendly non-SMOKE fire closes the window, do not offer another ordinary SMOKE shot. Check IR/MOL-projector ordering before committing.

**Checklist behavior:** Show the side-wide closing window and each weapon’s prior commitments together.

**Illustrative position:** An eligible unmarked Gun can consider Dispersed Smoke; a First-Fired Gun must first satisfy its more restrictive remaining-fire rules.

**Terms:** SMOKE, WP, TH, ROF, IF.

#### Resolve WP checks and possible Flame

ID: `wpEffects`. **Who:** BOTH. **Type:** Triggered consequences.

**Timing:** ASOP 4.11D. **Rules:** A24.31-.32; C3.76. **PDF pages:** 680, 92, 171. **Coverage:** Rules integration pending; reference available

**Consider:** Which units and terrain are affected by this WP placement?

- Resolve WP NMC using its own terrain/CH rules, not the ordinary HE result path.
- Track the source distinction between direct ordnance placement and a WP FFE, which can affect vulnerable occupants throughout its Blast Area.
- Resolve any Flame check before moving on. A successful placement is not the end of the action.

**Checklist behavior:** Create linked resolution rows for each affected disclosed subject and required terrain effect.

**Illustrative position:** A successful WP placement creates required morale and possible Flame effects before the next attack.

**Terms:** WP, NMC, CH, DR.

#### Fire a mortar Illuminating Round

ID: `mortarIR`. **Who:** DEFENDER. **Type:** Optional, commits weapon.

**Timing:** ASOP 4.11D. **Rules:** C8.7; E1.91; E1.93-.932. **PDF pages:** 680, 179, 226, 227. **Coverage:** Rules integration pending; reference available

**Consider:** Is illumination worth giving up this mortar’s other ammunition for the Player Turn?

- Review earlier MPh ammunition and fire commitments before offering IR. The DFPh opening does not reset the mortar’s Player Turn use.
- Confirm IR capability and initial-use permission, then make the Usage dr. Failure is not firing.
- On success make the TH DR for malfunction, Low Ammo and SAN purposes; use the IR placement procedure instead of an ordinary target hit.
- Firing IR uses all the mortar’s ROF for the Player Turn and prevents another ammunition type. Place No Fire; resolve Gunflash/concealment. Its CA does not change because IR was fired.

**Checklist behavior:** Distinguish Usage failed, round fired, and failed due to malfunction. Do not offer HE after an IR has been fired.

**Illustrative position:** An eligible mortar chooses illumination and spends its entire remaining IR fire opportunity; it cannot then switch to HE in this Player Turn.

**Terms:** IR, MTR, dr, TH, SAN, ROF.

#### Designate a Spotter for early mortar fire

ID: `earlySpotter`. **Who:** DEFENDER. **Type:** Optional prerequisite.

**Timing:** ASOP 4.11D. **Rules:** C9.3-.31; ASOP 4.11D/4.21D. **PDF pages:** 680, 179. **Coverage:** Rules integration pending; reference available

**Consider:** Does the early mortar shot need a Spotter?

- Use eligible Good Order Personnel in the same or adjacent hex; several mortars sharing a Spotter must meet the common-hex and common-target conditions.
- Apply spotted-fire TH and ROF penalties. Spotting counts as SW use and restricts movement and inherent fire as specified.
- Keep a hidden Spotter private. Replacing a lost original Spotter has a later MPh timing; this is not a free replacement control.

**Checklist behavior:** Attach the Spotter commitment to the planned early shot and to remaining unit capability.

**Illustrative position:** An early indirect mortar shot needs a legally designated Spotter; waiting for the ordinary-fire list must not silently prevent the opening shot.

**Terms:** MTR, LOS, ROF, SW.

#### Fire a MOL Projector before ordinary weapons

ID: `molProjector`. **Who:** DEFENDER. **Type:** Expiring special-weapon window.

**Timing:** ASOP 4.11D. **Rules:** C13.51-.58; ASOP 4.11D. **PDF pages:** 680, 184. **Coverage:** Rules integration pending; reference available

**Consider:** Is a MOL Projector attack wanted before non-SMOKE weapons fire?

- Predesignate the target and validate range, elevation and CA. A MOL Projector is not a squad MOL attack.
- In DFPh it must precede the first weapon firing other than SMOKE. Apply prior-use restrictions; leadership does not modify its TH DR.
- Resolve hit, smoke, possible Flame and the correct fire/Gunflash markers.

**Checklist behavior:** Display a separate last-chance reminder alongside the SMOKE window.

**Illustrative position:** The defender reviews the MOL Projector before firing ordinary HE from another weapon.

**Terms:** MOL, TH, SMOKE, CA.

### 4.12D · Artillery contact and Access

#### Gain or maintain artillery contact

ID: `radio`. **Who:** DEFENDER’s Observer. **Type:** Optional or mission-dependent.

**Timing:** ASOP 4.12D. **Rules:** C1.2, C1.22-.23, C1.6-.63; G.7; E12.77. **PDF pages:** 680, 163, 166, 276, 248. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** Should this Observer commit the phase to the battery?

- Validate the Observer, possession, transport/Guard limits and radio or Field Phone. Check nationality/date/PTO modifiers and whether maintenance rather than first contact applies.
- Contact/maintenance is the Observer’s sole phase action apart from allowed OBA activities. A failed attempt cannot be retried by this radio or Observer in the same phase.
- Distinguish voluntary and involuntary contact loss and their mission effects. Offboard Observers and Creeping Barrages have explicit exceptions, not an assumed radio roll.

**Checklist behavior:** Compare OBA use with the leader’s competing fire-direction role before commitment.

**Illustrative position:** Gain or maintain artillery contact / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, DR, Radio Contact, FFE.

#### Request Battery Access

ID: `access`. **Who:** DEFENDER’s Observer. **Type:** Conditional choice or required procedure.

**Timing:** ASOP 4.12D. **Rules:** C1.21; C1.34; G14.63. **PDF pages:** 680, 163, 164, 316. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** Is Access needed, and does FFE:C make the request mandatory?

- Keep Contact and Access separate. Use the battery’s actual draw pile and the required additional-draw conditions.
- With Contact and an FFE:C, resolve the mandatory Access attempt and continuation outcome. Do not offer ordinary attacks while an unresolved required branch is pending.
- Honor red-card consequences, permanent loss where applicable, and special battery/NOBA rules. Never reveal private draw-pile order.

**Checklist behavior:** Show the next legal branch after the draw; marking this considered cannot gain Access.

**Illustrative position:** Request Battery Access / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** Battery Access, OBA, FFE:C, AR.

#### Choose a new mission and place its AR

ID: `newMission`. **Who:** DEFENDER’s Observer. **Type:** Optional mission declaration.

**Timing:** ASOP 4.12D. **Rules:** C1.3-.32; C1.7-.732. **PDF pages:** 680, 164, 166. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** Which mission and target are justified by this Observer’s view?

- Choose the legal mission type and declare it at the required time. Ordinary HE Concentration, Harassing Fire, SMOKE, IR and Barrage are distinct choices.
- Place an AR using Observer LOS and the correct additional-draw tests, then resolve Accuracy and Extent of Error. Do not leak hidden units through legality messages.
- A Pre-Registered eligible target may use its direct-FFE procedure. An ordinary SR is not permission to resolve an immediate HE attack.

**Checklist behavior:** Keep proposed AR, resulting SR and FFE as distinct states, with a visible next decision.

**Illustrative position:** Choose a new mission and place its AR / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AR, SR, FFE, HE, LOS, Harassing Fire.

#### Declare an OBA illumination mission

ID: `obaIR`. **Who:** DEFENDER’s Observer. **Type:** Optional early declaration.

**Timing:** ASOP 4.12D. **Rules:** E1.931-.932; C1.7. **PDF pages:** 680, 166, 227. **Coverage:** Rules integration pending; reference available

**Consider:** Should this battery provide illumination instead of a normal fire mission?

- Declare IR before the mission’s first Battery Access draw. OBA needs Contact and Access but no mortar-style Usage dr.
- Track FFE stage offboard for Access purposes; place IR by its own procedure, not as an FFE Blast Area.
- Ordinary IR use consumes the battery’s ROF and other-ammunition opportunity for the Player Turn; apply the separate NOBA exception where authorized.

**Checklist behavior:** Expose this before drawing, so the interface cannot invite a late retroactive mission choice.

**Illustrative position:** Declare an OBA illumination mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IR, OBA, Battery Access, SR.

#### Use an Observation Plane or special Observer

ID: `aerialObserver`. **Who:** DEFENDER. **Type:** Conditional observer procedure.

**Timing:** ASOP 4.12D. **Rules:** C1.63; E7.61-.62; G14.68. **PDF pages:** 680, 166, 239, 240, 317. **Coverage:** Rules integration pending; reference available

**Consider:** Does this Observer use aerial sighting or shipboard procedures?

- For an Observation Plane, resolve applicable Sighting TC and observation restrictions before the battery’s placement and resolution.
- Use its actual aerial LOS and mistaken-attack rules rather than ground-Observer visibility.
- A Shipboard Observer has its own LOS, accuracy and AR restrictions. Keep offboard and aerial Observer identities distinct.

**Checklist behavior:** A new Observer profile changes the decision and consequences; it is not just a different icon.

**Illustrative position:** Use an Observation Plane or special Observer / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, LOS, TC, NOBA.

#### Resolve FFE:C continuation

ID: `continuation`. **Who:** DEFENDER’s Observer. **Type:** Required after applicable Access attempt.

**Timing:** ASOP 4.12D. **Rules:** C1.34-.343; G14.671. **PDF pages:** 680, 164, 316, 317. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** Can the next mission continue here, or must it begin with a new AR?

- Resolve the old mission’s cancellation and the permitted new SR/FFE/AR branches after the Access outcome.
- Do not treat FFE:C as another attacking FFE.
- NOBA forbids the ordinary C1.341/C1.342 continuation shortcuts and requires its new-AR procedure.

**Checklist behavior:** One continuation decision replaces the old mission; it does not silently extend an HE attack.

**Illustrative position:** Resolve FFE:C continuation / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE:C, SR, FFE, NOBA.

#### Place a rocket artillery mission

ID: `rockets`. **Who:** DEFENDER’s Observer. **Type:** Special mission procedure.

**Timing:** ASOP 4.12D. **Rules:** C1.9; ASOP 4.12D-.13D. **PDF pages:** 680, 167. **Coverage:** Rules integration pending; reference available

**Consider:** Is this a rocket battery with its own placement and expenditure rules?

- Use the rocket AR/FFE procedure and applicable restrictions rather than the ordinary SR correction loop.
- Rocket Correction is not allowed. Show the limited mission opportunity and resulting battery state.

**Checklist behavior:** Do not offer a misleading Correct SR button on a rocket mission.

**Illustrative position:** Place a rocket artillery mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** AR, FFE, OBA.

### 4.13D · Existing artillery and scheduled barrages

#### Correct, convert, leave or cancel an existing mission

ID: `correct`. **Who:** DEFENDER’s Observer. **Type:** State-dependent choice.

**Timing:** ASOP 4.13D. **Rules:** C1.33-.337; C1.35-.4. **PDF pages:** 680, 164. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** What does the existing marker and current Observer LOS allow?

- Enumerate the alternatives permitted for this exact SR or FFE:1/2 state, including leaving a qualifying SR in place, correction, predesignated conversion, and cancellation.
- Require the applicable Contact, Access and Blast-Height/target visibility. Preserve declaration-before-dice ordering.
- Do not offer arbitrary backward transitions such as replacing an FFE:1/2 with an SR; apply the special cancellation consequences.

**Checklist behavior:** Display legal branches with reasons for unavailable ones; pending evaluation remains unknown rather than illegal.

**Illustrative position:** Correct, convert, leave or cancel an existing mission / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SR, FFE, AR, LOS.

#### Correct or lift a Creeping Barrage

ID: `barrage`. **Who:** Scenario Attacker, currently DEFENDER. **Type:** Scheduled mandatory procedure with specific choices.

**Timing:** ASOP 4.13D. **Rules:** E12.731; E12.74; E12.76-.771. **PDF pages:** 680, 247, 248. **Coverage:** Rules integration pending; reference available

**Consider:** Was this Creeping Barrage recorded as FFE:1 or FFE:1-2?

- For recorded FFE:1-2, make the required DFPh correction unless the barrage Lifts. For recorded FFE:1, do not offer that correction/conversion/cancellation as an ordinary DFPh choice.
- Under E12.731, both recorded schedules still resolve the due FFE:2 attack if it has not Lifted, then flip to FFE:C. No correction is not the same as no attack.
- Preserve scheduled Lift and the specific early-Lift/conversion provisions. Do not ask for routine Observer contact while the Creeping Barrage procedure governs.

**Checklist behavior:** Show the recorded schedule and due attack as separate facts, with secret planning available only to its owner.

**Illustrative position:** A barrage recorded FFE:1 does not creep during this DFPh, but still resolves its due FFE:2 attack.

**Terms:** FFE, Barrage, AR, OBA.

### 4.14D · Artillery resolution and map effects

#### Place OBA Dispersed SMOKE or IR before HE FFE

ID: `obaSmokeResolution`. **Who:** DEFENDER; both sides resolve effects. **Type:** Required mission resolution.

**Timing:** ASOP 4.14D. **Rules:** C1.71; E12.51; E1.93; A24.32; C3.76; G14.65. **PDF pages:** 680, 166, 171, 227, 247, 316. **Coverage:** Rules integration pending; reference available

**Consider:** Which smoke or illumination missions must resolve before HE artillery?

- Place Dispersed SMOKE in eligible Blast Area hexes, respecting terrain/weather exclusions and Barrage variations.
- Resolve FFE-specific WP NMC and possible Flame. Ordinary OBA IR and NOBA’s IR exception follow their own mission records.
- Complete the required smoke/illumination sequence before HE FFE; do not replace the opening window with free mid-phase placement.

**Checklist behavior:** Resolve all applicable early effects before HE FFE. Separate placement completion from affected-unit resolution.

**Illustrative position:** Place OBA SMOKE or IR before HE FFE / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SMOKE, WP, IR, FFE, NMC.

#### Resolve HE FFE and advance its marker

ID: `ffe`. **Who:** Both sides resolve; DEFENDER’s battery. **Type:** Required when due.

**Timing:** ASOP 4.14D. **Rules:** C1.5; C1.52-.56; G14.65. **PDF pages:** 680, 164, 165, 316. **Coverage:** Rules integration pending; reference available. Shared combat resolution does not establish an implemented OBA mission path.

**Consider:** Which hexes and occupants are affected by this mission now?

- Resolve the actual Blast Area with the prescribed separate hex DR against all eligible friendly and enemy occupants. A unit attacked by this FFE during MPh can be attacked again in DFPh.
- Use level, protection, collateral and special-terrain rules. No FFNAM/FFMO applies to this phase-wide resolution.
- Advance FFE:1 to FFE:2 or FFE:2 to FFE:C as required; resolve existing mission effects even when contact-loss rules prevent new commands.

**Checklist behavior:** The battery stays unresolved until unit and terrain consequences finish.

**Illustrative position:** A squad survived movement through the FFE. It and other eligible occupants still face the due DFPh resolution.

**Terms:** FFE, HE, IFT, DR, PRC.

#### Resolve artillery changes to terrain and fortifications

ID: `terrainEffects`. **Who:** Both sides resolve affected state. **Type:** Triggered mandatory effects.

**Timing:** ASOP 4.14D. **Rules:** B2.1; B6.331; B25.13; B24.11; B26.52; B29.5; B30.92; B28.62; G9.72; G13.7; G14.56; E1.952; F8.41. **PDF pages:** 680, 113, 116, 142, 143, 145, 149, 150, 153, 286, 312, 315, 227, 264, 290. **Coverage:** Rules integration pending; reference available

**Consider:** Did this attack change the map or remove a fortification?

- Check the separate conditions for shellholes, Flame, rubble and bridge effects; a qualifying result is not one universal destruction rule.
- Check wire, roadblock and pillbox removal, minefield and panji removal/reduction, and sangar or trip-flare elimination.
- Include beach-obstacle and pier rules where relevant. Never expose a hidden fortification merely to display a speculative effect.

**Checklist behavior:** Create per-feature follow-ups with the governing rule and originating attack. This covers each terrain/fortification family named in ASOP 4.14D.

**Illustrative position:** Resolve artillery changes to terrain and fortifications / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** HE, FFE, KIA, TEM, Blaze.

#### Resolve Column and Reverse Slope effects

ID: `columnReverseSlope`. **Who:** Affected sides. **Type:** Triggered consequences.

**Timing:** ASOP 4.14D. **Rules:** E11.533; G14.66-.661. **PDF pages:** 680, 245, 316. **Coverage:** Rules integration pending; reference available

**Consider:** Does this artillery resolution break a Column or interact with a Reverse Slope?

- Apply Column disbandment when its trigger is met; do not leave its formation state unchanged after the attack.
- For NOBA, resolve Reverse Slope protection and exceptions against the naval line of fire, not a generic observer-LOS shortcut.

**Checklist behavior:** Surface these as dependencies of the actual mission, with pending integration visible.

**Illustrative position:** Resolve Column and Reverse Slope effects / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FFE, NOBA, Column, LOS.

#### Apply Naval OBA mission exceptions

ID: `naval`. **Who:** Affected battery owner. **Type:** Mission-specific overlay.

**Timing:** ASOP 4.14D. **Rules:** G14.6-.68. **PDF pages:** 680, 315, 316, 317. **Coverage:** Rules integration pending; reference available

**Consider:** Does this naval mission differ from ordinary OBA?

- Apply SFCP/Observer, Access, Blast Area, naval line-of-fire and Reverse Slope rules to the corresponding mission steps.
- Keep the FFE:C new-AR requirement visible.
- A qualifying NOBA battery of at least 150mm can fire IR with its HE/WP mission under G14.672, using its placement restriction. Do not apply the ordinary OBA IR prohibition without this exception.

**Checklist behavior:** Attach this profile to the individual battery; do not provide disconnected generic naval buttons.

**Illustrative position:** Apply Naval OBA mission exceptions / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** NOBA, IR, FFE:C, SFCP.

### 4.21D · Final Fire eligibility and remaining shots

#### Use normal fire with an unmarked eligible unit or weapon

ID: `unmarked`. **Who:** DEFENDER. **Type:** Optional normal attack.

**Timing:** ASOP 4.21D. **Rules:** A8.4-.41; A7; C2.241. **PDF pages:** 680, 61, 84, 168. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What can this unmarked subject fire now?

- An unmarked weapon may retain normal fire even if it fired during MPh and kept ROF. Validate its operator and all other commitments.
- Normal target eligibility is not limited to adjacent hexes. Apply Pin and other modifiers rather than universally demanding Good Order for all DFPh firing.
- Berserk units can fire in DFPh under A15.431. TI, broken and specific crew/weapon restrictions still require individual checks.

**Checklist behavior:** Separate marker history from a simplistic “already fired” boolean.

**Illustrative position:** A Gun retained ROF throughout Movement and can choose a legal distant target now.

**Terms:** Final Fire, ROF, IFT.

#### Use restricted Final Fire after First Fire

ID: `firstMarked`. **Who:** DEFENDER. **Type:** Optional restricted attack.

**Timing:** ASOP 4.21D. **Rules:** A8.4-.41. **PDF pages:** 680, 61. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is an adjacent or same-hex target available for this First-Fired subject?

- Apply the adjacent/same-hex limit and Area Fire for the eligible non-ordnance attack, with possible PBF/TPBF. Preserve other firing restrictions.
- This is DFPh Final Fire, not MPh SFF or FPF. Do not import an expenditure requirement or an FPF morale test.
- Handle weapon-specific IF/Sustained Fire separately, and mark the subject appropriately after use.

**Checklist behavior:** Explain precisely why a distant target is unavailable while an unmarked neighboring weapon may still attack it.

**Illustrative position:** A First-Fired squad has an adjacent enemy target but no ordinary shot at the enemy three hexes away.

**Terms:** First Fire, Final Fire, Area Fire, PBF, TPBF.

#### Consider Sustained Fire with a First-Fired MG or IFE

ID: `sustained`. **Who:** DEFENDER. **Type:** Optional extra fire with risk.

**Timing:** ASOP 4.21D. **Rules:** A8.41; A9.3; C2.29. **PDF pages:** 680, 61, 63, 168. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this weapon qualify for its restricted additional attack?

- Check the adjacent/same-hex target and the actual weapon/operator restrictions. A lone SMC cannot use MG Sustained Fire; vehicular MG and ordnance-use exclusions have stated exceptions.
- Apply reduced firepower and the applicable breakdown penalty; use IFE’s own linked rules.
- The attack cannot retain ROF simply because the colored die is low. Place Final Fire and preserve all linked unit-use commitments.

**Checklist behavior:** Show both reduced effectiveness and loss risk before confirmation.

**Illustrative position:** The MG which laid a Fire Lane in MPh may have a qualifying DFPh shot, but it is not a fresh normal-ROF attack.

**Terms:** Sustained Fire, MG, IFE, B#, ROF.

#### Consider a First-Fired Gun’s Intensive Fire

ID: `intensive`. **Who:** DEFENDER. **Type:** Optional restricted ordnance attack.

**Timing:** ASOP 4.21D. **Rules:** A8.41; C2.241; C5.6-.63. **PDF pages:** 680, 61, 168, 172, 173. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this Gun take its one remaining IF shot?

- A Gun marked First Fire firing as ordnance uses IF for its additional shot, restricted here to adjacent/same-hex targets.
- Check exhausted normal ROF, No IF weapon notes and crew Pin/Shock/Stun/Final/Intensive restrictions. Final-Fired subjects do not receive a general new IF entitlement.
- Apply +2 TH DRM, B# reduction by two under A.11, and the required Intensive Fire marker. OVR Prevention is a separate MPh procedure.

**Checklist behavior:** Evaluate current markers after each shot; do not offer an automatic extra shot after a DFPh attack has made the subject Final-Fired.

**Illustrative position:** A First-Fired AT Gun can consider a qualifying adjacent tank, but not a distant tank under the same permission.

**Terms:** IF, TH, ROF, B#.

#### Use retained Rate of Fire

ID: `rof`. **Who:** DEFENDER. **Type:** Continuing opportunity.

**Timing:** ASOP 4.21D. **Rules:** A8.41; A9.2-.21; C2.241; C3.33; C9.31. **PDF pages:** 680, 63, 168, 170, 179. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Did this weapon retain another normal shot, and is it worth taking?

- An unmarked Multiple-ROF weapon may retain multiple normal attacks and normal target selection even after firing in MPh.
- Reevaluate effective ROF, operator, CA, leadership and concealment after each shot. Apply actual marker changes when ROF is lost.
- First-Fired IF/Sustained Fire is a different opportunity and does not create a new normal-ROF chain. Holding a retained shot remains optional.

**Checklist behavior:** Create a new shot opportunity linked to the weapon and current phase, preserving the prior result.

**Illustrative position:** A retained-ROF HMG fires at a non-adjacent target, while the First-Fired squad possessing it has a different inherent-fire limit.

**Terms:** ROF, DR, MG, MTR, CA.

#### Recognize exhausted fire without resetting it

ID: `spent`. **Who:** DEFENDER. **Type:** Restriction explanation.

**Timing:** ASOP 4.21D. **Rules:** A8.4; C5.6; E1.8. **PDF pages:** 680, 61, 172. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Why is this unit or weapon no longer offered another attack?

- Final, Intensive and No Fire states are not a new pool of DFPh actions. Preserve unit/weapon-specific limits and explicit exceptions.
- Do not offer FPF, SFF, Snap Shots, a new ordinary Fire Lane, Gun Duel or OVR Prevention as DFPh substitutes for spent fire.
- Removing a counter at its cleanup step cannot reopen a phase whose firing window has already closed.

**Checklist behavior:** Make the rule explanation accessible while keeping pending implementation distinct from a verified exhausted capability.

**Illustrative position:** A squad that used SFF in MPh is Final-Fired; it does not gain an FPF shot merely because an enemy ended adjacent.

**Terms:** Final Fire, IF, DFPh.

### 4.21D · Attack choices and commitments

#### Fire inherent FP or a fire group

ID: `smallArms`. **Who:** DEFENDER. **Type:** Optional attack.

**Timing:** ASOP 4.21D. **Rules:** A7.2-.5; A7.53-.55; A9.4. **PDF pages:** 680, 54, 55, 56, 57, 64. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Should these units combine fire or keep separate attacks?

- Choose legal target Locations and validate LOS, range and mandatory FG rules before resolving an attack. Group membership is constrained, not any convenient collection of units.
- Preview PBF/TPBF, Long Range, Area Fire, concealment, terrain and Hindrances. Keep same-hex and multi-level target rules explicit.
- Apply allowed leadership and its direction commitments. Do not suggest splitting an attack where mandatory FG rules require combining it.

**Checklist behavior:** Show adjusted FP and DRM with provenance, plus the members committed by confirmation.

**Illustrative position:** Fire inherent FP or a fire group / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, FG, IFT, DRM, PBF, TPBF, LOS.

#### Choose Spraying Fire

ID: `spraying`. **Who:** DEFENDER. **Type:** Conditional attack mode.

**Timing:** ASOP 4.21D. **Rules:** A7.34; A9.5. **PDF pages:** 680, 56, 64. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Would spreading eligible fire across the permitted targets help?

- Check the weapon or squad’s Spraying Fire capability, target relationship, range and CA.
- Show the halved FP and separate target consequences; do not make this a universal MG or squad option.

**Checklist behavior:** Keep squad Spraying Fire visible even where the current implementation supports only a narrower attack set.

**Illustrative position:** Choose Spraying Fire / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, MG, IFT, CA.

#### Allocate SW and leader direction

ID: `weapons`. **Who:** DEFENDER. **Type:** Attack preparation.

**Timing:** ASOP 4.21D. **Rules:** A7.35; A7.53-.54; A9.1-.4; A21. **PDF pages:** 680, 56, 57, 63, 64, 88. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Which weapons can this unit use while retaining any inherent fire?

- Apply squad/HS/crew/SMC weapon-use limits and the relationship to inherent FP; a SW shot is not free simply because it has a separate counter.
- Check captured and non-qualified use, crew-served requirements, ammunition shortage and relevant nationality adjustments.
- For MGs include mandatory direction and later ROF direction restrictions. Record the directing leader, not just a numeric bonus.
- Carry the preceding MPh direction and SW-use commitments forward under A.15; do not give the same leader a fresh unrelated allocation solely because the phase label changed.

**Checklist behavior:** Maintain per-unit capability and per-weapon shots as related records.

**Illustrative position:** Allocate SW and leader direction / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SW, MG, ROF, SMC, MMC.

#### Choose target type and ammunition

ID: `ordnance`. **Who:** DEFENDER. **Type:** Optional ordnance attack.

**Timing:** ASOP 4.21D. **Rules:** C2-.8; C3.31-.33; C8.9. **PDF pages:** 680, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Which legal target type and available ammunition fit this target?

- Offer only ammunition and target types provided by the actual weapon notes and scenario/date, while keeping unsupported options readable. Include HE, AP, HEAT, APCR/APDS and special types where present.
- Show range, CA, acquisition, size, movement, terrain and qualified-user effects. Resolve hits through IFT or TK as appropriate.
- Keep Critical Hits, Multiple Hits, dud/Depletion and malfunction branches distinct. Do not label every miss as an unchanged game state.

**Checklist behavior:** The attack record captures declared target type/ammo before dice and links resulting effects.

**Illustrative position:** Choose target type and ammunition / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** TH, TK, ATT, ITT, VTT, HE, AP, HEAT, APCR, APDS.

#### Consider Canister, IFE and special attack modes

ID: `specialAmmo`. **Who:** DEFENDER. **Type:** Conditional weapon choices.

**Timing:** ASOP 4.21D. **Rules:** C2.29; C8.4; C5.7; C13; weapon notes. **PDF pages:** 680, 168, 173, 178, 182, 183, 184. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Does this weapon have a useful alternative to its ordinary shot?

- Represent Canister geometry and ammunition, IFE capability and its fire/ROF accounting, and Deliberate Immobilization eligibility as distinct choices.
- For LATW show PF availability, BAZ/PSK/PIAT/ATR restrictions, backblast or Desperation penalties and captured/non-qualified use as applicable.
- Weapon and nationality notes select the actual choices and modifiers. Pending IFE or a specific weapon path must not disappear from the guide.

**Checklist behavior:** Expose modes before commitment and show their separate risk, target restrictions and consequences.

**Illustrative position:** Consider Canister, IFE and special attack modes / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** IFE, C, TH, TK, CA.

#### Use a flamethrower or squad MOL attack

ID: `ftMol`. **Who:** DEFENDER. **Type:** Optional special attack.

**Timing:** ASOP 4.21D. **Rules:** A22.1-.6; B25.12. **PDF pages:** 680, 89, 90, 143. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Does the special weapon justify its range, availability and loss risk?

- Check FT eligibility/range and distinguish Personnel/vehicle resolution, relevant protection exceptions, and depletion.
- MOL requires its applicable SSR or nationality capability and a successful MOL Check; use its actual inherent-fire and target rules.
- Resolve any self-effect, Flame or burning-wreck result. A squad’s MOL is separate from the early MOL Projector timing.

**Checklist behavior:** Preview risks and possible fire consequences rather than showing only nominal FP.

**Illustrative position:** Use a flamethrower or squad MOL attack / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FT, MOL, IFT, TK, X#.

#### Sequence attacks for Encirclement

ID: `encirclement`. **Who:** DEFENDER. **Type:** Conditional attack-order opportunity.

**Timing:** ASOP 4.21D. **Rules:** A7.7-.72. **PDF pages:** 680, 57. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Could consecutive attacks establish Encirclement?

- Check eligible targets, ranges, directions and firepower under A7.7, including the pillbox and other exclusions.
- Qualifying attacks must be consecutive; an intervening attack on a different target can break the sequence.
- Keep ordinary and upper-level Encirclement distinct. Do not automatically apply a result just because two units fired at one hex.
- DFPh attacks can qualify; do not count earlier MPh attacks as the consecutive DFPh sequence.

**Checklist behavior:** Warn when a proposed unrelated shot would abandon an identified sequence, without choosing tactics for the player.

**Illustrative position:** Sequence attacks for Encirclement / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** FP, LOS, Encircled.

#### Choose vehicle weapons and firing arcs

ID: `vehicleFire`. **Who:** DEFENDER. **Type:** Optional attacks with shared constraints.

**Timing:** ASOP 4.21D. **Rules:** D.3; D3.1-.5; D5.3; D6.6; G12.5. **PDF pages:** 680, 192, 200, 201, 202, 203, 207, 304. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Which individual weapons still have a legal shot after their MPh use?

- Track MA, SA and each MG with actual fire markers, retained ROF, crew status and CA. Firing a CMG in MPh does not automatically consume an otherwise eligible MA.
- Preserve hull/turret changes and applicable Case A penalties across MPh/DFPh under A.15/D3.51; apply Motion firer and moving-target rules separately.
- Include passenger/rider, LC, stunned/shocked crew and equipment-note restrictions. A generic tank-fired flag is insufficient.

**Checklist behavior:** Compare individual attacks while keeping vehicle-wide commitments visible.

**Illustrative position:** A vehicle changed TCA to fire its CMG during MPh; its later MA shot must retain the applicable CA-change penalty.

**Terms:** AFV, MA, SA, AAMG, BMG, CMG, CE, BU, TCA, VCA.

#### Throw a DC or detonate an existing Set DC

ID: `dc`. **Who:** DEFENDER. **Type:** Conditional special attack.

**Timing:** ASOP 4.21D. **Rules:** A23.6-.72; G1.6121. **PDF pages:** 680, 90, 91, 280. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Is there a legal thrown or previously Set demolition attack now?

- A legal Thrown DC attack uses its range, elevation, target and thrower-risk procedure. A First-Fired unit cannot Throw a DC; do not turn restricted Final Fire into permission to do so.
- A previously Set DC may be detonated by its authorized Good Order unit/derivative HS in a subsequent Player Turn’s friendly fire phase, with the required distance, LOS and NTC.
- Resolve malfunction and bridge/building effects. Placed DC attacks wait for AFPh; Japanese A-T Set DC has its own trigger.

**Checklist behavior:** Show who is authorized to trigger the charge and the prior-turn requirement before offering execution.

**Illustrative position:** The attacker’s newly Placed DC is still waiting for AFPh; an eligible defender’s previously Set DC has a different possible DFPh decision.

**Terms:** DC, NTC, IFT, US#.

#### Designate a mortar’s original Spotter

ID: `mortarSpotter`. **Who:** DEFENDER. **Type:** Optional prerequisite.

**Timing:** ASOP 4.21D. **Rules:** C9.3; ASOP 4.21D. **PDF pages:** 680, 179. **Coverage:** Rules integration pending; reference available

**Consider:** Does a mortar without an original Spotter need one for this attack?

- This window designates an original Spotter for a mortar that had none; it is not unrestricted replacement of an existing one.
- Apply Personnel/Location, spotted-fire, ROF and SW-use requirements. Keep hidden Spotter records private.
- Coordinate an early mortar shot with its required designation; distinguish this from the MPh replacement window.

**Checklist behavior:** Keep early designation, ordinary original designation and later replacement as separate timing windows.

**Illustrative position:** Designate a mortar’s original Spotter / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MTR, LOS, ROF, SW.

#### Fire between adjacent sewer units

ID: `sewerFire`. **Who:** DEFENDER. **Type:** Conditional special-location attack.

**Timing:** ASOP 4.21D. **Rules:** B8.43. **PDF pages:** 680, 117. **Coverage:** Rules integration pending; reference available.

**Consider:** Are opposing sewer units in the special adjacent firing situation?

- Use B8.43’s sewer adjacency and visibility conditions instead of applying an ordinary surface LOS test.
- Apply concealment and ordinary eligible-fire restrictions; do not expose undisclosed sewer positions or call this automatic Melee.

**Checklist behavior:** Keep sewer combat discoverable even before sewer rules are integrated.

**Illustrative position:** Two opposing sewer groups in the permitted adjacent positions can have DFPh fire without appearing as ordinary surface units.

**Terms:** LOS, IFT, DFPh.

#### Attack a landed glider before its contents deploy

ID: `landedGlider`. **Who:** DEFENDER. **Type:** Conditional target procedure.

**Timing:** ASOP 4.21D. **Rules:** E8.3-.41. **PDF pages:** 680, 241, 242. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the landed glider a legal Final Fire target?

- Use the specified stopped-truck target treatment, size, survival and Damage exceptions. It does not burn; its own cover/Hindrance differs from a wreck.
- Account for possible Case J and glider Damage/contents consequences. Ordinary contents placement occurs in AFPh, not at the DFPh boundary.

**Checklist behavior:** Show the landed glider and its actual content state rather than inventing separately deployed targets.

**Illustrative position:** A glider that just landed can be attacked before its Infantry contents are placed onboard in AFPh.

**Terms:** TH, PRC, TEM.

### 4.21D · Illumination, aircraft and counter-responses

#### Use a later Starshell opportunity

ID: `starshellLater`. **Who:** DEFENDER. **Type:** Conditional later window.

**Timing:** ASOP 4.21D. **Rules:** E1.91-.921; ASOP 4.21D. **PDF pages:** 680, 226, 227. **Coverage:** Rules integration pending; reference available

**Consider:** Does the current illumination history still permit a Starshell attempt now?

- Check Good Order, Pin/TI, Location and the initial-use conditions, then the appropriate leader/MMC/CE AFV Usage dr.
- Leaders have the within-phase timing exception. After the initial illumination Player Turn, non-leaders cannot use this DFPh window as a replacement for their opening PFPh/enemy-MPh window.
- Keep the applicable per-hex attempt and illumination-history record; changing the displayed topic does not grant a fresh attempt.

**Checklist behavior:** Keep the eligibility explanation tied to scenario illumination history.

**Illustrative position:** The leader considers illumination before a later shot; an MMC is not automatically eligible for the same late attempt.

**Terms:** IR, dr, MMC, CE, SMC.

#### Conduct Sighting TC and a Ground Support attack

ID: `groundSupport`. **Who:** DEFENDER. **Type:** Optional aircraft attack.

**Timing:** ASOP 4.21D. **Rules:** E7.3-.4; G17.4-.42; W9.2. **PDF pages:** 680, 237, 238, 239, 667. **Coverage:** Rules integration pending; reference available.

**Consider:** Should an available aircraft attack now?

- Validate aircraft availability, target and Sighting TC, including Recall and Mistaken Attack outcomes. Aircraft do not join a ground FG.
- Use the applicable Strafing Run or Point Attack, declared route and second-attack commitment. Give the ATTACKER each required Light AA response before the corresponding attack.
- Apply bombs, rockets, nationality and Chapter W restrictions, including the prohibition on Ground Support in consecutive Player Turns where W9.2 applies. FAC-directed PFPh CAS remains a different window.

**Checklist behavior:** Keep sighting, flight path, AA windows, attacks and required departure/Recall outcomes as a linked procedure.

**Illustrative position:** An aircraft that spots a target opens an ATTACKER AA decision before its first attack.

**Terms:** FB, DB, TC, AA.

#### Answer the air attack with Light AA

ID: `lightAA`. **Who:** ATTACKER. **Type:** Conditional counter-response.

**Timing:** ASOP 4.21D. **Rules:** E7.5-.512. **PDF pages:** 680, 239, 240. **Coverage:** Rules integration pending; reference available.

**Consider:** Which eligible weapons can oppose this attack run?

- Use AA-capable weapons and the specific target-hex opportunity, rather than the DEFENDER’s ground Final Fire menu.
- Apply IFE/MG ROF rules, no leadership DRM, aircraft protection and damage/evasion outcomes. A vehicle’s actual movement state determines applicable bounding penalties.
- Place AA/Prep/Bounding Fire/Gunflash markers as required, with consequences for the ATTACKER’s coming AFPh. Do not permit repeated fire at the same aerial target in the same hex beyond E7.51.

**Checklist behavior:** The displayed decision owner changes for this response without changing Player Turn roles.

**Illustrative position:** A weapon reserved for the attacker’s later fire may have to weigh that commitment against an immediate AA response.

**Terms:** AA, ROF, BFF, IFT.

#### Fire Heavy AA at an aerial target

ID: `heavyAA`. **Who:** DEFENDER. **Type:** Special attack.

**Timing:** ASOP 4.21D. **Rules:** E7.5; E7.52; W9.13. **PDF pages:** 680, 239, 667. **Coverage:** Rules integration pending; reference available

**Consider:** Does an eligible Heavy AA Gun have a useful aerial target?

- Only the qualifying AA Gun without printed IFE uses Heavy AA; this is not ordinary Light AA fire at any convenient time.
- Validate aerial targets, including Observation Planes and applicable airborne FAC. Resolve the Heavy AA elimination/damage/prevention outcomes, normal malfunction and ROF.
- Update AA mode and its one-attack ROF adjustment as required.

**Checklist behavior:** Offer an aerial-target workflow with its actual resolution, separate from ground fire.

**Illustrative position:** An eligible Heavy AA Gun can attack an aerial target during its DFPh; this is distinct from the attacker’s Light AA response to Ground Support.

**Terms:** AA, TH, ROF, IFE, FAC.

#### Finish aircraft damage, evasion, Mistaken Attack and Recall

ID: `airResults`. **Who:** BOTH. **Type:** Required aerial consequences.

**Timing:** ASOP 4.21D. **Rules:** E7.226; E7.31-.32; E7.511-.512. **PDF pages:** 680, 236, 237, 239. **Coverage:** Rules integration pending; reference available.

**Consider:** What remains to resolve from the sighting or AA result?

- A Damaged or evading aircraft still receives the required AA in its current hex before any permitted attack; apply its attack penalty and later attack limits.
- Resolve Mistaken Attack with the side entitled to make its choices. A Sighting Original 12 can require Recall at phase end after any permitted attack sequence.
- Keep pending departure/Recall obligations separate from marking the Ground Support topic considered.

**Checklist behavior:** Hold the linked attack sequence open until its required results and phase-end obligations finish.

**Illustrative position:** An aircraft forced to evade cannot simply be marked “attack complete” while other current-hex AA remains unresolved.

**Terms:** AA, TC, DR.

### 4.21D · Clearance declarations and Flame control

#### Declare Wire Clearance

ID: `wire`. **Who:** DEFENDER. **Type:** Optional labor commitment.

**Timing:** ASOP 4.21D. **Rules:** B24.73; B26.51-.53. **PDF pages:** 680, 142. **Coverage:** Rules integration pending; reference available.

**Consider:** Should these eligible Infantry work on the Wire instead of firing?

- Require the appropriate unpinned Good Order Infantry beneath the Wire and no prior movement/fire/direction in this Player Turn.
- Declare the attempt and apply TI now; resolve its Clearance DR at the end of CCPh. Do not remove Wire when the player merely declares work.

**Checklist behavior:** Track the workers, target and due resolution window.

**Illustrative position:** An unfired squad under Wire can commit now, while a squad that First-Fired cannot use the same permission.

**Terms:** TI, Labor.

#### Declare Set DC Clearance

ID: `clearDC`. **Who:** DEFENDER. **Type:** Optional labor commitment.

**Timing:** ASOP 4.21D. **Rules:** B24.75. **PDF pages:** 680, 142. **Coverage:** Rules integration pending; reference available.

**Consider:** Can eligible Infantry attempt to remove this Set DC?

- Check the required Location, Good Order/Pin state and no prior movement/fire/direction.
- Apply the declaration/TI commitment and queue end-CCPh Clearance. This is neither detonating a friendly Set DC nor disarming every DC type.

**Checklist behavior:** Keep the charge present until successful resolution.

**Illustrative position:** A unit declares work on a Set DC but cannot mark the explosive removed before the Clearance result.

**Terms:** DC, TI, Labor.

#### Declare Roadblock Clearance

ID: `roadblock`. **Who:** DEFENDER. **Type:** Optional labor commitment.

**Timing:** ASOP 4.21D. **Rules:** B24.76; B29.5. **PDF pages:** 680, 142. **Coverage:** Rules integration pending; reference available.

**Consider:** Should eligible Infantry MMC or a Dozer clear the roadblock?

- Check unpinned Good Order status, the roadblock hexside Location and prior movement/fire/direction restrictions.
- Apply TI and the Hazardous Movement procedure; removal depends on the end-CCPh Clearance result.

**Checklist behavior:** Preview exposure and lost firing capability before the work commitment.

**Illustrative position:** The defender can choose eligible work now rather than waiting for its next movement turn.

**Terms:** TI, Hazardous Movement.

#### Declare Path creation

ID: `path`. **Who:** DEFENDER. **Type:** Optional terrain work.

**Timing:** ASOP 4.21D. **Rules:** G2.7. **PDF pages:** 680, 282. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this unit begin creating a Path?

- Use an eligible Infantry MMC and the Path-specific terrain, load, equipment and position exclusions rather than generic rubble-clearance conditions.
- Become TI in the declared DFPh attempt and resolve the applicable Clearance in the later CCPh, including permitted labor/leadership conditions.

**Checklist behavior:** Keep Path work visible as a pending Chapter G option.

**Illustrative position:** A qualifying jungle position can offer Path work even though its Rules evaluator is not integrated.

**Terms:** TI, IPC, PRC.

#### Declare Panji covered-hexside Clearance

ID: `panji`. **Who:** DEFENDER. **Type:** Optional terrain work.

**Timing:** ASOP 4.21D. **Rules:** G9.71. **PDF pages:** 680, 290. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this unit work on the Panji-covered hexside?

- Predesignate the covered hexside and apply the Wire-like Clearance procedure. Units above a Panji counter cannot attempt, direct or assist Clearance.
- Record the commitment and later result separately; do not reveal hidden Panji through a speculative work prompt.

**Checklist behavior:** Pending integration does not remove the named Clearance option.

**Illustrative position:** Once a Panji feature is lawfully known, its appropriate work option remains readable.

**Terms:** TI, MC.

#### Attempt immediate Flame Clearance

ID: `flame`. **Who:** DEFENDER. **Type:** Optional immediate attempt.

**Timing:** ASOP 4.21D. **Rules:** B24.72-.721. **PDF pages:** 680, 142. **Coverage:** Rules integration pending; reference available.

**Consider:** Should eligible Infantry attempt to extinguish this Flame?

- Require unpinned Good Order Infantry in the same Location with no previous movement/fire/direction and no other phase activity; apply TI and Hazardous Movement.
- Resolve each Flame with its own DR. On failure place applicable Labor; a qualifying final (modified) Flame Clearance DR of 3-6 Hampers the Flame, marking it with PIN to prevent Blaze spread this Player Turn.
- A Blaze cannot be extinguished by this procedure. A failed extinguishing attempt and a successful Hamper are different results.

**Checklist behavior:** Record extinguished, Hampered, or still-unhampered separately, and carry the correct effect into AFPh spread.

**Illustrative position:** A result that does not remove the Flame may still prevent it becoming a Blaze this Player Turn.

**Terms:** TI, Labor, Hazardous Movement, Hampered Flame.

#### Resolve Column Disbandment after fire

ID: `columnFire`. **Who:** BOTH. **Type:** Conditional consequence.

**Timing:** ASOP 4.21D. **Rules:** E11.533. **PDF pages:** 680, 245. **Coverage:** Rules integration pending; reference available.

**Consider:** Has this fire or required result disbanded a Column?

- Apply the specific Column trigger to the affected formation after the attack.
- Update formation state before later fire or phase decisions; a considered reference entry is not the resolution.

**Checklist behavior:** Link the formation consequence to the triggering attack.

**Illustrative position:** A unit’s casualty result can require an update to its Column rather than only its individual counter.

**Terms:** Column, IFT.

### 4.21D · Weapon handling

#### Assemble or dismantle a weapon

ID: `assemble`. **Who:** DEFENDER’s possessor. **Type:** Optional weapon-use commitment.

**Timing:** ASOP 4.21D. **Rules:** A9.8. **PDF pages:** 680, 65. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Should the weapon be ready to fire later or easier to carry later?

- Use eligible weapon types, possession, operator capacity and the requirement that the weapon has not fired. Carry MPh use forward.
- Assembly/disassembly counts as SW use and all its ROF; it is not a way to obtain a fresh shot or reset fire markers.
- Record the changed counter and portage, then determine any remaining inherent capability under the unit-use rules.

**Checklist behavior:** Separate conversion success from later fire or movement eligibility.

**Illustrative position:** A squad may prepare an unfired dismantled weapon for a later turn while its actual SW-use limits still govern other actions.

**Terms:** dm, SW, PP, ROF.

#### Destroy or deliberately malfunction equipment

ID: `destroy`. **Who:** DEFENDER’s possessor or inherent crew. **Type:** Optional irreversible game action.

**Timing:** ASOP 4.21D. **Rules:** A9.73. **PDF pages:** 680, 65. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Would denying this equipment justify losing its use?

- The weapon and possessor must otherwise still be able to fire it; A9.73 explicitly invokes A8.4’s DFPh restrictions.
- Offer destruction and deliberate malfunction separately, within actual weapon-use capacity and instead of firing. Do not let spent fire become unrestricted destruction.
- Preserve resulting Recall, repair and permanent-loss consequences.

**Checklist behavior:** Preview the exact equipment state change before executing, while a reference acknowledgement remains harmless.

**Illustrative position:** A First-Fired subject must satisfy its restricted DFPh eligibility before deliberately destroying the weapon under A9.73.

**Terms:** SW, MA, ROF, Recall.

### 4.22D · Gun preparation

#### Limber a Gun for later movement

ID: `limber`. **Who:** DEFENDER’s eligible crew. **Type:** Optional preparation.

**Timing:** ASOP 4.22D. **Rules:** C10.111; C10.21-.23. **PDF pages:** 680, 180, 181. **Coverage:** Rules integration pending; reference available

**Consider:** Should this Gun prepare for towing instead of firing?

- Check actual Gun type, eligible crew and its ability to fire, disregarding Breakdown as specified. QSU Guns cannot be (un)limbered.
- Neither Gun nor crew may fire in the Player Turn of (un)limbering, so prior MPh firing matters. Limbering does not itself make them TI.
- Allow the specified simultaneous CA change; this does not hook up a towing vehicle or grant a DFPh move.

**Checklist behavior:** Compare fire now with the actual remaining towing sequence; limbering does not itself hook up a vehicle.

**Illustrative position:** A crew that fired in the preceding MPh cannot treat DFPh as a fresh opportunity to limber.

**Terms:** TI, Gun, MPh, QSU.

#### Unlimber a Gun

ID: `unlimber`. **Who:** DEFENDER’s eligible crew. **Type:** Optional TI commitment.

**Timing:** ASOP 4.22D. **Rules:** C10.111; C10.21-.23. **PDF pages:** 680, 180, 181. **Coverage:** Rules integration pending; reference available

**Consider:** Is preparing this Gun for future fire worth committing it and the crew now?

- Check the eligible Gun/crew, prior Player Turn fire and current limbered state. A Gun cannot unlimber while in tow.
- Apply TI to both Gun and crew and allow the specified CA selection. Unlimbering is preparation, not permission for an immediate shot.
- Handle QSU and Limbered Fire equipment through their own rules rather than converting every Gun.

**Checklist behavior:** Track preparation and readiness separately.

**Illustrative position:** An eligible unfired crew unlimbers now to prepare for a future firing opportunity.

**Terms:** TI, Gun, CA, QSU.

### 4.31D · Final arc and AA-mode choices

#### Change Gun CA without firing

ID: `caEnd`. **Who:** DEFENDER. **Type:** Optional end-phase window.

**Timing:** ASOP 4.31D. **Rules:** C3.22; D3.51. **PDF pages:** 680, 169, 201, 55. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Can this Gun legally change its arc without firing before DFPh ends?

- At this point the crew must remain able to fire the Gun without Intensive/Sustained Fire, subject to explicit exceptions. Retained ROF can preserve the option.
- First/Final/Intensive/No Fire commitments matter before cleanup. A First-Fired Gun that could only IF does not qualify under the ordinary rule.
- Do not copy the special PFPh movement-cancellation clause into this DFPh decision. Track the actual CA and acquisition consequences.

**Checklist behavior:** Show a separate movement commitment without falsely marking an attack as fired.

**Illustrative position:** An unfired Gun changes CA at 4.31D. A First-Fired Gun cannot wait for 4.32D marker removal and then claim the same opportunity.

**Terms:** CA, TCA, VCA, IF, AFPh.

#### Designate or cancel AA mode

ID: `aaModeEnd`. **Who:** DEFENDER. **Type:** Optional end-phase window.

**Timing:** ASOP 4.31D. **Rules:** E7.5; C3.22. **PDF pages:** 680, 239, 169. **Coverage:** Rules integration pending; reference available

**Consider:** Should this weapon be prepared for aerial or ground targets?

- Use the end-fire-phase CA eligibility criterion for the relevant weapon, including non-Gun cases covered by E7.5.
- Distinguish this window from an attack that changes AA mode, and retain the actual ROF, weapon-type and AAMG exceptions.
- Resolve the choice before marker cleanup; cleanup does not retroactively create eligibility.

**Checklist behavior:** Do not present mode switching as an always-free toggle.

**Illustrative position:** An eligible unmarked weapon prepares its AA mode for later play without pretending to have fired.

**Terms:** AA, CA, ROF, AAMG.

### 4.32D · Close the phase with the correct markers

#### Remove daytime First/Final Fire, retain night Gunflash evidence

ID: `cleanup`. **Who:** DEFENDER. **Type:** Required phase-end cleanup.

**Timing:** ASOP 4.32D. **Rules:** A3.4; E1.8; ASOP 4.32D/5.33B. **PDF pages:** 680, 47, 226. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which counters actually expire now?

- In a daytime scenario remove First and Final Fire counters at 4.32D, after the 4.31D decisions.
- At night leave First/Final Fire in their original Locations through the specified AFPh cleanup; their Gunflash function is not merely a unit’s spent-fire flag.
- Do not also remove Intensive, No Fire, Prep, Bounding Fire or unrelated counters now. Track their own later expiry, including TI/Labor and Hampered Flame state.

**Checklist behavior:** Maintain separate capability and location-based visibility records. Complete required consequences and aircraft Recall before finishing the phase.

**Illustrative position:** A night defender’s unit can leave later while its original Gunflash Location still matters until the proper removal window.

**Terms:** First Fire, Final Fire, Gunflash, DFPh.

### Supplemental timing and equipment exceptions

#### Cancel a Chapter W searchlight beam

ID: `searchlight`. **Who:** DEFENDER. **Type:** Optional standing choice.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** W10.52; W10.45-.51. **PDF pages:** 680, 670. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the owner cancel its beam during DFPh?

- Voluntary cancellation is allowed in the owner’s DFPh; remove associated Gunflash only as specified.
- Do not copy PFPh maintenance or enemy-MPh tracking/new-target windows into DFPh. Apply the stated initial-use/timing rules to any other beam action.

**Checklist behavior:** Keep cancellation distinct from unrestricted beam establishment or retargeting.

**Illustrative position:** A defender may cancel its existing beam here without receiving a universal DFPh “move beam” control.

**Terms:** IB Lane, NVR, LOS.

#### Drop an AVRE fascine

ID: `fascine`. **Who:** DEFENDER. **Type:** Optional equipment-note action.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** British Vehicle Note 37, fascine provisions. **PDF pages:** 680, 412, 413. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this eligible AVRE discard its carried fascine?

- The equipment note explicitly permits dropping during the AVRE’s DFPh on declaration, at no penalty and in any terrain.
- Dropping removes the fascine and follows the counter/facing provisions; it does not create the terrain benefit of placing it. Any gained MP is not permission to move in DFPh.

**Checklist behavior:** Represent the equipment-specific alternative independently of firing.

**Illustrative position:** The AVRE discards its load during DFPh, but no crossing aid appears in that hex.

**Terms:** fascine, TCA, VCA.

#### Change the mode of a specifically listed self-propelled Gun

ID: `spGunMode`. **Who:** DEFENDER. **Type:** Optional equipment-note preparation.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** U.S. Vehicle Note 45 (M12 GMC); UN Forces Vehicle Note S. **PDF pages:** 680, 386, 575. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this vehicle’s listing allow a limbered/unlimbered mode change?

- For the cited configurations, require the specified crew status and that neither crew nor MA has fired; flip the counter and apply TI to crew and vehicle.
- Apply the listing’s no-fire while limbered, no-MP while unlimbered, crew exposure and TH treatment. Do not give all SP Guns this action.

**Checklist behavior:** Use the exact equipment profile rather than an ordinary Gun counter rule.

**Illustrative position:** An M12 GMC can have this preparation choice; an unrelated tank cannot.

**Terms:** TI, CE, MA, RFNM.

#### Change the Skoda Infantry Gun’s barrel

ID: `barrels`. **Who:** DEFENDER. **Type:** Optional equipment-note conversion.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** Axis Minor Ordnance Note 39; A9.8. **PDF pages:** 680, 531. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the eligible Infantry crew change this Gun’s barrel?

- The note permits an unpinned Good Order crew to flip this counter in a PFPh/DFPh in which it does not fire, treating the change as assembling a SW.
- Carry forward preceding MPh weapon use and the applicable SW-use commitment. Do not generalize interchangeable barrels to other weapons.

**Checklist behavior:** Offer the alternate configuration only for the listed equipment.

**Illustrative position:** The crew prepares the other barrel rather than taking a shot with its current one.

**Terms:** SW, TI, ROF.

#### Preserve equipment-note restrictions across MPh and DFPh

ID: `equipmentHistory`. **Who:** DEFENDER. **Type:** Conditional firing overlay.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** Japanese Ordnance Note 10; Allied Minor Vehicle Notes 14-15; Allied Minor Ordnance Note 8; Communist Forces Ordnance Note 28. **PDF pages:** 680, 458, 503, 510, 594. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the equipment treat MPh and DFPh as one phase for this restriction?

- Specified guns cannot alternate Direct and Indirect Fire within the combined MPh/DFPh period; retain the earlier firing mode.
- Specified T-13 configurations restrict VCA/TCA changes combined with fire or movement. Do not clear those facts when entering DFPh.
- Use the actual listing and exceptions. These identified notes are not proof that every Chapter H listing has been inventoried.

**Checklist behavior:** Store restricted prior actions in the equipment record instead of deriving them only from the current fire marker.

**Illustrative position:** A Gun’s unmarked state alone does not authorize switching firing modes if its own listing forbids it.

**Terms:** CA, VCA, TCA, ROF.

#### Apply nationality, terrain and equipment restrictions

ID: `nationality`. **Who:** Affected side. **Type:** Eligibility and modifier overlay.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A19; A21; A25; G.3; G11.8; G12.5; W7.41; Chapter H. **PDF pages:** 680, 86, 88, 94, 97, 276, 298, 304, 663, 664. **Coverage:** Rules integration pending; reference available

**Consider:** Which specific rule changes this unit’s permitted attack?

- Apply nationality, unit qualification, ammunition shortage, scenario/date and equipment-note conditions to each proposed attack.
- Include PTO fire-group, cave, watercraft/LC and target-protection rules. Keep unsupported profiles visible with unevaluated applicability.
- Do not transfer W7.41’s specific PFPh/Opportunity Fire Area Fire restriction wholesale to ordinary CPVA DFPh shots; use the actual scope and all other applicable rules.

**Checklist behavior:** Modifiers must attach to the proposed shot. Chapter H equipment entries and scenario SSR are source profiles, not a claim that every weapon uses one generic rule.

**Illustrative position:** The same unit can have different firing restrictions in PFPh and DFPh; sharing a glossary must not duplicate permissions.

**Terms:** CPVA, FG, ROF, B#, SSR.

#### Resolve the prisoner-elimination rule

ID: `massacre`. **Who:** Eligible units; compulsory case as specified. **Type:** Optional or compulsory special case.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A20.3-.4; A15.43. **PDF pages:** 680, 87, 84. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Does A20.4 apply in this Location?

- Apply A20.4 nationality/status and no-Melee conditions, SW-use accounting and the relevant fire-phase timing.
- The berserk-unit-with-prisoners case is a compulsory start-of-fire-phase result with its prescribed state change, not an ordinary optional late DFPh attack.
- Apply the No Quarter/Mopping Up and enemy ELR consequences as specified.

**Checklist behavior:** Use the rule’s neutral description and consequences; neither hide the option nor suggest it as a tactic.

**Illustrative position:** A compulsory opening prisoner result is resolved when due, not deferred until after ordinary attacks.

**Terms:** ELR, SW, Melee, Berserk.

#### Consider standing choices and ongoing commitments

ID: `standing`. **Who:** As the specific rule permits. **Type:** Conditional cross-phase choices.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A12.14; A20.5; B9.32-.323; E10.2-.4. **PDF pages:** 680, 78, 87, 119, 244. **Coverage:** Rules integration pending; reference available

**Consider:** Will this action affect an ongoing obligation or standing choice?

- Keep voluntary concealment loss, Guard Deployment and allowed WA claims/relinquishment at their actual timing; no retroactive WA change after target declaration.
- Ongoing ammunition replenishment can be interrupted by firing; carry the work record across phases.
- Do not turn prisoner transfer, ordinary movement or a new Opportunity Fire declaration into DFPh actions.

**Checklist behavior:** Reevaluate relevant commitments when an actual event changes them, without automatically clearing prior consideration.

**Illustrative position:** Firing now may interrupt an existing ammunition task even though the task began in an earlier phase.

**Terms:** HIP, WA, TI, Ammo Vehicle.

### Consequences, source limits and phase boundaries

#### Resolve attack results before the next decision

ID: `attackResults`. **Who:** Both sides as affected. **Type:** Required resolution chain.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A7.3; A9.7; A10; A15; A17; C3.7-.8; C7; D.8. **PDF pages:** 680, 55, 56, 65, 66, 67, 69, 83, 84, 170, 171, 176, 177, 193. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Which required results remain from this shot?

- Resolve random selection, casualties, MC/PTC, wounds, leader-loss effects and applicable HOB or special morale changes in their proper sequence.
- Resolve collateral effects, Shock/Stun/Recall, burning wrecks, equipment destruction, Cowering, malfunction and ammunition depletion where triggered.
- Update concealment, Control/WA and capability only from authorized results. Never mark the attack finished while a dependent choice remains.

**Checklist behavior:** Separate attacker choices from defender resolution, keep private information private, and reopen only newly created opportunities.

**Illustrative position:** Resolve attack results before the next decision / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** MC, PTC, KIA, K, HOB, LLMC, LLTC, DM, CH.

#### Resolve Sniper attacks and checks

ID: `sniper`. **Who:** Both sides according to trigger. **Type:** Triggered response.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A14.1-.4; E1.72; E1.76; G12.603; G14.261; ASOP dagger. **PDF pages:** 680, 82, 83, 225, 305, 314. **Coverage:** Partially integrated; full rule path still needs verification

**Consider:** Did a qualifying Original DR activate a Sniper?

- DFPh is Sniper-capable. Check qualifying Original TH/MC/TC/non-OBA IFT DR and the Prisoner and other exclusions; not every roll triggers a Sniper.
- Finish the triggering roll’s required effects and preserve chained-result ordering; offer a permitted Sniper Check when its conditions occur.
- Retain night, LC/seaborne and named ASOP campaign exceptions. RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 full texts are external source gaps.

**Checklist behavior:** The response can change later eligibility, so rerun affected opportunities after resolution.

**Illustrative position:** Resolve Sniper attacks and checks / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** SAN, TH, MC, TC, DR.

#### Resolve a triggered Field Phone cut

ID: `phoneCut`. **Who:** Affected phone owner. **Type:** Triggered consequence.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** C1.23. **PDF pages:** 680, 163. **Coverage:** Rules integration pending; reference available

**Consider:** Did an opposing unit’s Original 2 trigger a phone-security consequence?

- Check the specified security-area and opposing-unit conditions for an Original 2 DR for any purpose.
- Keep phone information private, and distinguish a cut/non-repairable phone from a malfunctioned repairable radio.

**Checklist behavior:** Link this to the triggering roll, rather than inserting a voluntary phone-cutting action.

**Illustrative position:** Resolve a triggered Field Phone cut / Review subject and scenario / Pending rules remain visible; no game action is performed.

**Terms:** OBA, DR, HIP.

#### Keep adjacent-phase options in their proper windows

ID: `notHere`. **Who:** BOTH. **Type:** Boundary explanation.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** A7.25; A8.1-.4; A23.3-.4; B24.71-.76; E1.8. **PDF pages:** 680, 61, 90, 142, 242. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this an actual DFPh choice or a reminder for another phase?

- Do not offer movement expenditures, Smoke grenades, Search, DFF/SFF/FPF, Snap Shots, Gun Duels or OVR Prevention as ordinary DFPh actions.
- Placed DC resolution, ordinary glider contents deployment and Flame/Blaze spread are AFPh work. New Opportunity Fire belongs in PFPh.
- DFPh Clearance declarations include Wire, Set DC, roadblock, Path and Panji; do not copy MPh rubble/minefield declarations or PFPh entrenchment/Kindling/Mopping Up here. Explicit special exceptions require their own profile.

**Checklist behavior:** Future reminders remain readable but do not become executable current-phase actions.

**Illustrative position:** A Placed DC is shown as an upcoming AFPh consequence, not a missing DFPh checkbox.

**Terms:** DFPh, MPh, AFPh, FPF.

#### Review scenario and external-module exceptions

ID: `sourceProfile`. **Who:** BOTH. **Type:** Source-completeness boundary.

**Timing:** Rule-specific DFPh timing; supplemental to ASOP. **Rules:** ASOP dagger; SSR; Chapters F/G/H/W. **PDF pages:** 680, 678. **Coverage:** Rules integration pending; reference available.

**Consider:** Are all sources for this scenario and equipment available?

- Keep pending and deferred options visible. Integration status is independent from legal availability and source completeness.
- Record the named campaign Sniper gaps and load the exact SSR/equipment note before claiming scenario-complete evaluation.
- The supplied-volume review and eight-step ASOP ledger do not claim exhaustive coverage of every module or individual Chapter H exception.

**Checklist behavior:** Show “source needed” or “availability not evaluated” instead of a false illegal result.

**Illustrative position:** A campaign-specific exception can remain discoverable even while its governing module text is unavailable.

**Terms:** SSR, Source gap.

## Contextual vocabulary

Reuse existing acronym identities where meanings match. Descriptive phrases are term definitions, not invented official acronyms. Preserve the distinction between Final Fire as a phase activity and Final Fire as an exhausted-state marker.

| Term | Meaning in this phase |
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
| Area Fire | A firepower reduction, including eligible First-Fired units firing again in DFPh. It is not the ordnance Area Target Type. |
| ATT | Area Target Type. |
| B# | Breakdown Number, with relevant reductions applied under the actual rule. |
| Barrage | An artillery pattern; a Creeping Barrage additionally follows recorded progression. |
| Battery Access | Permission to use the battery, determined by its access procedure. |
| Berserk | Special morale state; ordinary Prep Fire is prohibited, with specific rule exceptions. |
| BFF | Bounding First Fire, eligible vehicular fire during the vehicle’s MPh. |
| Blaze | A continuing fire state, distinct from a Flame and from its smoke counter. |
| BMG | Bow Machine Gun. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| C | Canister ammunition, in this ordnance context. |
| CA | Covered Arc. |
| CE | Crew Exposed. A vehicle crew posture whose voluntary changes have timing restrictions. |
| CH | Critical Hit. |
| CMG | Coaxial Machine Gun. |
| Column | A formation with specific disbandment rules. |
| CPVA | Chinese People’s Volunteer Army, with Chapter W nationality and movement rules. |
| DB | Dive Bomber, an aircraft type with its own attack and AA response procedures. |
| DC | Demolition Charge. |
| DFPh | Defensive Fire Phase. Its firing is Final Fire under A8.4, distinct from Defensive First Fire in the preceding MPh. |
| dm | Dismantled weapon state, distinct from DM. |
| DM | Desperation Morale, distinct from dismantled dm. |
| dr | One die, distinct from a two-dice DR. |
| DR | Two-dice roll. Keep Original, modified and individual colored/white values. |
| DRM | Modifier to a two-dice roll. A drm modifies a single die. |
| ELR | Experience Level Rating. |
| Encircled | State established by qualifying fire or other specific rules. |
| FAC | Forward Air Controller in the applicable Chapter W rules. |
| fascine | A carried bundle used by specified AVRE equipment. Dropping it in DFPh is not placing a crossing aid. |
| FB | Fighter-Bomber, an aircraft type with its own sighting and Ground Support procedures. |
| FFE | Fire For Effect. FFE:1, FFE:2 and FFE:C have different actions. |
| FFE:C | Continuation marker; it is not an attacking FFE. |
| FFMO | First Fire Movement in Open Ground, assessed for the particular attack and route. |
| FFNAM | First Fire Non-Assault Movement, a modifier tied to movement and activity, not merely a changed hex. |
| FG | Fire Group, subject to formation and mandatory grouping rules. |
| Final Fire | Fire during DFPh. The phrase also names a counter that prevents further fire; the phase name does not grant another shot to a Final-Fired unit. |
| First Fire | A counter and prior-use state carried from MPh. It restricts remaining DFPh fire; it does not reset when MPh ends. |
| FP | Firepower. |
| FPF | Final Protective Fire, restricted further fire that also tests the firer’s morale. |
| FT | Flamethrower. |
| Gun | Rules-defined ordnance, not every weapon colloquially called a gun. |
| Gunflash | Night firing marker or specified equivalent that affects visibility and related night procedures. |
| Hampered Flame | A Flame marked with a PIN counter after the specified Clearance result, preventing its spread to Blaze during that Player Turn. |
| Harassing Fire | OBA mission with different area and firepower from a Concentration. |
| Hazardous Movement | The defined exposure of certain activities. Its modifier can matter in DFPh even though FFNAM and FFMO cannot. |
| HE | High Explosive ammunition or mission. |
| HEAT | High Explosive Anti-Tank ammunition. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| HOB | Heat of Battle, a triggered resolution rather than an optional action. |
| IB Lane | Illuminating Beam Lane created by a Chapter W searchlight. Cancellation has its own timing; it is not ordinary SMOKE. |
| IF | Intensive Fire, an extra Gun shot with accuracy and malfunction penalties. |
| IFE | Infantry Firepower Equivalent, a weapon-specific alternative to ordinary TH fire. |
| IFT | Infantry Fire Table. |
| IPC | Inherent Portage Capacity. Excess portage affects the moving unit’s available MF. |
| IR | Illuminating Round, with its own usage and placement procedure. |
| ITT | Infantry Target Type. |
| K | Casualty Reduction result, distinct from KIA. |
| KIA | Killed In Action attack result. |
| Labor | A counter reflecting qualifying prior failed work. A declaration, a failed attempt and successful Clearance are distinct states. |
| LLMC | Leader-Loss Morale Check. |
| LLTC | Leader-Loss Task Check. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| MA | Main Armament. |
| MC | Morale Check. |
| Melee | Close-combat state restricting many actions. |
| MG | Machine Gun. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| MOL | Molotov Cocktail. A squad MOL and a MOL Projector have different procedures. |
| MPh | Movement Phase. THE MPh contains a separate start, during and end for each moving unit or stack. |
| MTR | Mortar. |
| NMC | Normal Morale Check. |
| NOBA | Naval Offboard Artillery. |
| NTC | Normal Task Check, used only when the governing rule calls for it. |
| NVR | Night Visibility Range, used with illumination and other night visibility exceptions. |
| OBA | Offboard Artillery. |
| PBF | Point Blank Fire. |
| PP | Portage Points, the load represented by carried equipment. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| PTC | Pin Task Check. |
| QSU | Quick Set Up, affecting Gun handling. |
| Radio Contact | Communication state, separate from Battery Access. |
| Recall | Vehicle withdrawal obligation. |
| RFNM | Restricted Fire, No Movement, the Gun classification in C10.25. Apply any equipment-note exceptions to its firing and movement restrictions. |
| ROF | Rate of Fire. A retained opportunity is not an automatic shot. |
| SA | Secondary Armament. |
| SAN | Sniper Activation Number, tested only for qualifying Original rolls. |
| SFCP | Shore Fire Control Party for NOBA. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| SMOKE | The umbrella for Smoke and WP effects; preserve ammunition type, owner and source. |
| Source gap | A named rule or module whose full governing text is not in the supplied volume. Its applicability remains unevaluated. |
| SR | Spotting Round marker. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| Sustained Fire | Eligible First-Fired MG/IFE additional fire. It has reduced FP and increased breakdown risk and does not retain ROF. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| TC | Task Check. |
| TCA | Turret Covered Arc, distinct from the vehicle hull’s covered arc. |
| TEM | Terrain Effects Modifier. |
| TH | To Hit roll or number for an ordnance attack. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| TK | To Kill resolution, distinct from the To Hit roll. |
| TPBF | Triple Point Blank Fire. Some same-Location entry attacks are compulsory under A8.312. |
| US# | Unit Size Number. Used for purposes including prisoner guarding capacity and the dr to Set a DC; it is not a weapon Usage Number. |
| VCA | Vehicular Covered Arc, the vehicle hull’s facing. |
| VTT | Vehicle Target Type. |
| WA | Wall Advantage. Its claim, loss and mandatory conditions depend on B9.32 and the position. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| X# | Permanent weapon-loss/depletion threshold where specified. |

## Acceptance cases for the eventual live component

| Case | Required behavior |
|---|---|
| Squad First-Fired, HMG retained ROF | Evaluate each separately; no blanket normal or exhausted state. |
| First-Fired Gun, distant target | No ordinary DFPh shot through the adjacent/same-hex IF permission. |
| Final-Fired squad next to enemy | No new FPF opportunity in DFPh. |
| First-Fired MG with low colored die | Sustained Fire does not retain normal ROF. |
| Vehicle changed TCA in MPh | Preserve applicable MA Case A consequences into DFPh. |
| Infantry stopped, vehicle moved | Remove FFNAM/FFMO while retaining applicable vehicle target history. |
| Existing FFE already attacked mover | Still resolve due phase-wide FFE against eligible occupants. |
| Creeping schedule FFE:1 | No DFPh correction, but due attack still resolves unless Lifted. |
| Flame Clearance not extinguished | Distinguish Hampered result from unhampered failure. |
| Wire Clearance declaration | TI now, later CCPh result; no instant Wire removal. |
| End-phase First-Fired Gun | No ordinary CA change based on hypothetical later marker removal. |
| Night cleanup | Preserve original-location First/Final Fire through specified AFPh cleanup. |
| Aircraft Ground Support | ATTACKER AA decisions and departure consequences stay linked. |
| Unknown SSR or equipment note | Visible source/evaluation gap, never false permission or illegality. |
| Optional topic marked considered | No game action or required obligation is performed. |

## Preview validation

The combined reference preview was checked across all 289 topics in Rally, Prep Fire, Movement and Defensive Fire, including every contextual definition interaction. Validation covered the eight DFPh step mappings, unique catalog identities, source-page ranges, timing navigation, separate phase consideration, host-state restoration, and 320/736-pixel layouts. Light and dark views were inspected; no browser runtime errors or horizontal overflow were found. These checks validate reference wiring and presentation, not rule execution. The final coverage-label correction leaves OBA mission paths explicitly pending.
