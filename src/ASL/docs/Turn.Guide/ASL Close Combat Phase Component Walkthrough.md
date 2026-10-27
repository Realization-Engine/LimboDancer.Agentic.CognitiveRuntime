# ASL Close Combat Phase Component Walkthrough

Design reconciliation, 26 October 2026. Target: the completed ASL.Rules implementation after all passes. This document defines the intended phase guide and reference preview, not a new combat-engine implementation.

## Sources and integration baseline

Primary source: eASLRB v3.01, `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. Locators are one-based physical PDF pages. ASOP section 8 is on page 681; core A11 is on pages 72-76. The reconciliation uses that original sequence and the rules it references, with supplemental cross-phase and equipment families.

Recent commits remain the Pass 71 merge-gate and documentation work at `08c80da8` and `53540236`. Coverage statements use `ASL Rule Coverage.md` including its later amendments, not only the older bold fault lists. Pass 70 added or improved mixed Infantry/vehicle sequences, vehicle-present Ambush, Gun-crew combat and equipment loss, over-IPC Withdrawal choices, concealed-destination consequences, PRC handling and Street Fighting. Pass 65 added abandoned-AFV capture and temporary crew/driver foundations. Passes 70-71 include CC vehicle-target Field Promotion and its AFV modifier. Those are not described here as wholly absent.

Remaining integrated-path qualifications include pillbox CC, vertical Withdrawal, capture manpower/VP details, some prisoner outcomes and nationality rules. ATMM, sN, aerial combat, detailed Clearance, night, Ammo Replenishment and deferred Chapter G/H/J/module profiles remain visible even where execution is pending. A generic partial label means existing foundations, not proof that every option on that card currently works end to end.

The printed-step ledger covers all 17 CCPh steps. It does not certify every campaign SSR, Chapter H note or scenario combination. In particular the named historical campaign concealment references and O1.5 Debris need their own source-profile validation when those sources are not present. Missing source, pending implementation and an illegal action are three different findings.

## Design: two queues and a declaration receipt

The live component should first show a Location queue owned by the ATTACKER. Each Location completes its reveal, Ambush, declaration, attack, interruption, Withdrawal and closing-status sequence before another is selected. A second queue holds phase-wide Aerial Combat, declared Clearance, marker changes, concealment and night cleanup. A turn with no ground CC still has this second queue.

For each attack, a declaration receipt should record the attacking units/weapons, SMC defending groups, targets, capture/HtH mode, strength or CCV, modifiers and commitment point. Simultaneous attacks retain their pre-result strength and targets. Sequential attacks reopen selection only after the preceding result and its immediate consequences are complete. The component should display why prisoners, Ambush or vehicles determine the next decision owner.

Some decisions interrupt normal sequence: immediate Infiltration, creation and recalculation after a Field Promotion, Hara-Kiri before capture resolution, and a defending T-H Hero attempt delayed until attacker Ambush attacks finish. A flat list of checkboxes cannot safely execute these. The reference preview uses consideration only; the eventual live guide needs explicit available, declared, committed, resolved, declined, not-applicable, pending-integration and missing-source states.

A useful comparison view would let the player inspect combined versus split attacks, capture versus elimination, concealed participation versus remaining concealed, and standing versus withdrawing groups before committing. It must use only legally disclosed information. Unknown enemy destinations, hidden fortifications and unauthorized strengths must not leak through option counts, labels or client-side filtering. Numeric odds and favorable outcomes should not imply that unimplemented rules were evaluated.

Closing status belongs to units as well as Locations. A tank can hold Infantry without being held; a concealed survivor can remain free; pillbox and sewer cases can avoid ordinary Melee. Capture is provisional until simultaneous consequences are settled. Clearance uses earlier declarations before Pin/TI removal. Concealment gain uses the illumination still present before the night cleanup step. Trip flares retain their activation-specific expiry.

## Coverage boundaries

The preview now completes the eight ordinary phase catalogs, but remains a design reference. It does not roll dice, adjudicate a position, execute rules or claim all implementation passes are complete. The next work is a cross-phase event and expiry audit, scenario-profile source completion, and a Blazor integration contract with the existing Rules/Play projection and decision mechanisms.

The catalog contains 78 topics and 79 contextual vocabulary labels.

## Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 8.11B | Place hidden items under concealment; disclose concealed Strength Factors; remove Dummies. | `reveal`, `hiddenHero` |
| 8.12B | Ambush in qualifying terrain or concealment; Street Fighting; night threshold. | `ambush`, `ambushModifiers`, `ambushChoice`, `street` |
| 8.13D | Defending Japanese squad/HS Tank-Hunter Hero creation, subject to G1.421 delayed timing. | `tankHunter` |
| 8.14B | Sequential precedence; prisoner escape; escorted abandoned-vehicle capture declaration. | `sequence`, `escape`, `abandonedDeclaration` |
| 8.15B | Optional/required Melee Withdrawal, ATTACKER first; drop equipment preventing departure. | `withdraw`, `brokenWithdrawal`, `withdrawLoad` |
| 8.16B | SMC solo status or pairing, ATTACKER first. | `pairing`, `leadership` |
| 8.21B | Sequential next-attack or simultaneous declarations; HtH/capture mode; reveal attacking/directing units. | `declarations`, `concealedChoice`, `personnelAttack`, `handToHand`, `capture`, `vehicleAttack`, `vehicleFire`, `prc`, `closeDefense`, `terrainCombat`, `mountedWater`, `goliath` |
| 8.22B | German or T-H Hero ATMM checks. | `atmm`, `heroAtmm` |
| 8.23B | Japanese Hara-Kiri response to capture. | `haraKiri` |
| 8.24B | Resolve attacks; Withdrawal/Infiltration; subject-only FFE/mines; Column/flare/Panji effects; berserk recovery; repeat sequential loop. | `resolve`, `vehicleResult`, `unlikelyKill`, `crewSmallArms`, `infiltration`, `promotion`, `equipmentLoss`, `captureValidity`, `guards`, `withdrawArrival`, `streetReturn`, `withdrawHazards`, `withdrawSpecial`, `berserk`, `nextAttack`, `heroRemoval` |
| 8.25B | Interrogation of new prisoners with nationality profiles. | `interrogation` |
| 8.31B | Automatic unescorted abandoned-vehicle capture; CC/Melee state; Dare-Death berserk status. | `automaticCapture`, `melee`, `dareDeath`, `heroRemoval` |
| 8.41B | Declare and resolve Aerial Combat, ATTACKER first, with special ROF/Recall/damage rules. | `dogfight`, `aerialResults`, `aerialClose` |
| 8.42B | All non-Flame Clearance, including flail/PTO/debris profiles; Labor; Recovered tunnel entrances. | `clearance`, `rubble`, `wireSetDc`, `mineClearance`, `flail`, `ptoClearance`, `moduleClearance`, `labor`, `tunnels` |
| 8.43B | Pin/TI cleanup with replenishment/Pack-TI exceptions; Stun and cumulative exceptions; Ammo Replenishment. | `pinTi`, `stun`, `ammo` |
| 8.44A | ATTACKER concealment gain under ordinary and named night/winter/desert/Japanese/campaign profiles. | `concealmentGain`, `concealmentDr`, `concealmentProfiles` |
| 8.45B | Night Starshell/IR removal; Acquisition without persistent qualifying illumination. | `illumination`, `acquisition` |

Resolve 8.11B through 8.31B for one Location before selecting another. Steps 8.41B through 8.45B are phase-wide. The ASOP introduction gives ASOP precedence in ordering conflicts, while preserving the rules' normal restrictions. G1.421 qualifies when its creation attempt is allowed; E1.951 supplies an activation-dependent expiry not fully described by a generic illumination-cleanup heading. Neither establishes a blanket rulebook-over-ASOP hierarchy. Supplemental topics do not invent ASOP numbers. See the [cross-phase audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md) for linked clocks and the remaining prisoner-abandonment interpretation issue.

## Cataloged options

### Location order and commitment

#### Finish one Location before starting the next

ID: `handoff`. **Who:** ATTACKER. **Type:** Phase handoff.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** A11.12; ASOP 8. **PDF pages:** 681, 72. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which Location does the ATTACKER select, and what remains unresolved there?

- The ATTACKER chooses the order of CC Locations. Complete that Location’s opening, declarations, attacks, withdrawals and closing status before starting another.
- Keep phase-wide aerial combat, Clearance and cleanup in a separate queue, even when no ground CC exists.
- A reference Considered mark performs no game action. A live phase cannot close while committed attacks or required effects remain unresolved.

**Checklist behavior:** Propose a Location queue, a declaration receipt and a separate end-phase obligations queue.

**Illustrative position:** Resolving one village fight does not permit TI cleanup while another Location still has attacks.

**Terms:** CCPh, CC, Melee, Declaration receipt.

#### Identify the actual combat participants

ID: `participants`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** A11.1; B23.422; B30.6; G11.87; G13.495. **PDF pages:** 681, 72, 137, 151, 301, 312. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which opposing units can legally engage each other?

- Use Location, level and terrain status rather than just hex membership. Foxhole occupants can fight other units in the same Location; separate building levels normally cannot.
- Apply pillbox’s same-hex exception, cave restrictions and non-frozen Ocean prohibition before showing attacks.
- Carry forward concealment, PIN, TI, CX, broken status, equipment, mounted status, prior Melee and earlier phase history without silently clearing them.

**Checklist behavior:** Show separate combat groups and explain why apparent neighbors cannot attack.

**Illustrative position:** Opposing units on different floors do not automatically fight because their counters share a hex.

**Terms:** Location, CC, PRC.

### 8.11B · Reveal the Location

#### Place hidden items and disclose concealed strengths

ID: `reveal`. **Who:** BOTH. **Type:** Required disclosure.

**Timing:** ASOP 8.11B. **Rules:** A11.19; A12.31. **PDF pages:** 681, 73, 78, 151. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** What must become visible at this Location’s opening?

- Place the Location’s hidden items onboard under concealment and reveal concealed Strength Factors; remove Dummies before attack designation.
- Revealing strength is not blanket loss of concealment. Retain the remaining protection until a specific loss trigger.
- Do not reveal every CC Location simultaneously when only one has been selected; respect special pillbox inspection and other profile rules.

**Checklist behavior:** Separate public strength disclosure from concealment removal.

**Illustrative position:** A concealed squad’s strength becomes known while its concealment still affects the coming attack.

**Terms:** HIP, CC, FP.

#### Resolve a revealed hidden Tank-Hunter Hero

ID: `hiddenHero`. **Who:** Japanese player. **Type:** Required status.

**Timing:** ASOP 8.11B. **Rules:** G1.422; G1.425; A11.19. **PDF pages:** 681, 279, 280. **Coverage:** Rules integration pending; reference available.

**Consider:** Does involuntary revelation meet the hero’s survival exception?

- A hidden T-H Hero involuntarily revealed in CCPh survives this special test only as a DEFENDER in the same Location with an enemy AFV.
- Apply its special target and attack obligations if it remains; do not treat it as an ordinary Hero with a general heroic DRM.

**Checklist behavior:** Resolve the special revealed-counter consequence before offering attacks.

**Illustrative position:** A hidden T-H Hero revealed without the qualifying AFV cannot remain as ordinary Infantry.

**Terms:** T-H Hero, HIP, AFV.

### 8.12B · Ambush and its alternatives

#### Determine whether Ambush applies

ID: `ambush`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.12B. **Rules:** A11.4; G.6; E1.77. **PDF pages:** 681, 73, 225, 276. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is this a new encounter with a qualifying terrain or concealment trigger?

- Check Infantry advance into woods/building or by/against concealed units, and the hidden-unit placement trigger. Jungle, bamboo and kunai qualify under G.6.
- Reinforcing an existing Melee does not create a fresh ordinary Ambush roll. A vehicle in the Location does not by itself prevent Ambush.
- Normally one side must roll at least three lower after drm; the unilluminated night ATTACKER uses the E1.77 two-lower exception.

**Checklist behavior:** Explain the trigger and threshold before either side rolls.

**Illustrative position:** An unilluminated night attacker can gain Ambush with a two-point advantage.

**Terms:** Ambush, dr, Melee.

#### Build each side’s Ambush modifier explanation

ID: `ambushModifiers`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.12B. **Rules:** A11.17-.18; A11.4; G.6. **PDF pages:** 681, 73, 276. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which qualifying units and statuses contribute to the Ambush dr?

- Account for concealment, Stealth/Lax, CX, broken, pinned, berserk, vehicle/Cavalry/pillbox, BU/Stun, bank/Panji and PTO attacker conditions.
- Apply the best eligible unpinned Good Order leader with the alone/berserk restrictions. Do not substitute CC attack DRM for Ambush drm.
- Retain relevant force-wide qualifiers even when only part of the force has the status; keep hidden identities private until lawful disclosure.

**Checklist behavior:** Show one auditable dr calculation per side.

**Illustrative position:** A concealed Stealthy squad with a CX leader has both favorable and unfavorable Ambush modifiers.

**Terms:** drm, CX, BU, Stealthy.

#### Use the Ambush advantage or withdraw

ID: `ambushChoice`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.12B. **Rules:** A11.4-.41. **PDF pages:** 681, 73, 74. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Attack first, leave before combat, or preserve a later withdrawal option?

- The ambusher normally resolves all its attacks first; apply the sequential-priority rules when prisoners are involved. The ambushed side loses concealment.
- Eligible unpinned, non-berserk, non-Disrupted Infantry may withdraw before resolution or after all attacks by and against it, before Melee.
- An attacking ambusher can retain concealment when it eliminates/captures its target. An unsuccessful attack loses that special protection.

**Checklist behavior:** Expose the two distinct withdrawal windows and the consequence of attacking.

**Illustrative position:** An ambusher can leave safely now instead of risking a failed attack and return attack.

**Terms:** Ambush, Withdrawal, CC.

#### Check Street Fighting without inventing an Ambush roll

ID: `street`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.12B. **Rules:** A11.8. **PDF pages:** 681, 76. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does the earlier advance or Bypass position qualify against this vehicle?

- Check the road with buildings on both sides and ground-level advance provenance, or eligible Infantry in the vehicle’s Bypassed obstacle.
- The special -1/+1 vehicle-combat advantage does not stack with actual Ambush; actual Ambush takes precedence.
- Preserve the eligible return-to-origin option after attacks, with broken/pinned/above-Wire restrictions and return hazards. It is not a general free Withdrawal.

**Checklist behavior:** Keep actual Ambush, Street Fighting modifiers and the later return decision separate.

**Illustrative position:** A Street Fighter on top of Wire receives the applicable combat benefit but cannot return to its building.

**Terms:** Street Fighting, Ambush, AFV.

### 8.13D · Defending Japanese hero

#### Consider DEFENDER Tank-Hunter Hero creation

ID: `tankHunter`. **Who:** DEFENDER. **Type:** Combat decision.

**Timing:** ASOP 8.13D. **Rules:** G1.421-.4231. **PDF pages:** 681, 279. **Coverage:** Rules integration pending; reference available.

**Consider:** Can a qualifying Japanese squad or HS create a hero here?

- Require an armed Good Order Japanese Infantry squad/HS with an enemy AFV in the same Location and LOS, no disqualifying fire/Pin/TI marker, and remaining scenario allowance.
- Wait for Ambush determination and completion of all ATTACKER Ambush attacks. The printed opening step does not override that explicit delay.
- Apply the creation dr and HS/Conscript/DC modifiers, Original 6 Pin consequence and Designated Target choice. The hero must attempt to attack that AFV if still in its Location.

**Checklist behavior:** Queue the choice after any required attacker Ambush attacks rather than resolving it prematurely.

**Illustrative position:** An ambushed Japanese squad must survive the attacker’s attacks before attempting creation.

**Terms:** T-H Hero, Designated Target, AFV.

### 8.14B · Sequence, prisoners and abandoned vehicles

#### Choose the correct attack sequence

ID: `sequence`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.14B. **Rules:** A11.3-.34. **PDF pages:** 681, 73. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which rule decides who attacks next?

- Vehicle presence, even an abandoned vehicle not being attacked, creates sequential CC. The non-vehicular side starts; if both sides have vehicles, the ATTACKER starts.
- Ambush normally gives the ambusher all its attacks first. Prisoner attacks take priority when the higher-numbered sequential rule applies.
- Show the precedence explicitly. In ordinary vehicle sequence alternate one attack at a time, recomputing survivors and escorts after each result.

**Checklist behavior:** Display current decision owner, governing rule and the next available attack window.

**Illustrative position:** Prisoners can attack before other combatants even in a Location with both Ambush and a vehicle.

**Terms:** Sequential CC, Simultaneous CC, Ambush.

#### Declare eligible prisoner escape attacks

ID: `escape`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.14B. **Rules:** A20.55; A25.82; G1.621. **PDF pages:** 681, 88, 96, 280. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Can these prisoners attack their Guard now?

- Outside Melee the Guard must be broken and prisoners must pass an NTC. In existing Melee the NTC and broken-Guard conditions no longer apply.
- Only eligible Infantry may escape; apply Italian, Japanese and Axis Minor restrictions and the specified Axis Minor exception.
- The Guard must be eliminated before attacking others unless included in the same attack. Mere Guard elimination does not guarantee freedom if other enemies remain.

**Checklist behavior:** Show eligible escape groups, required checks and the conditions for actual freedom.

**Illustrative position:** Prisoners in established Melee can attack an unbroken Guard without another NTC.

**Terms:** Prisoners, Guard, NTC, Melee.

#### Declare an escorted abandoned-vehicle capture attempt

ID: `abandonedDeclaration`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.14B. **Rules:** A21.2; A11.52. **PDF pages:** 681, 74, 88. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is this vehicle contested or eligible for later automatic capture?

- An abandoned AFV with enemy Personnel present requires a CC capture attempt; both sides may contest it and supply its escort protection until controlled.
- Treat the unmanned AFV as CE and Immobile and apply the no-manned-MG modifier. A manned AFV cannot be captured by this procedure.
- If no enemy Personnel remain, the automatic capture window is at Location close. Do not award ownership at the opening merely because the vehicle is abandoned.

**Checklist behavior:** Distinguish attack commitment from the possible automatic closing result.

**Illustrative position:** Two opposing squads beside an abandoned tank must resolve the contest before assigning its crew.

**Terms:** AFV, Personnel Escort, CCV.

### 8.15B · Melee Withdrawal declarations

#### Declare voluntary Withdrawal from Melee

ID: `withdraw`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.15B. **Rules:** A11.2. **PDF pages:** 681, 73. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which eligible units leave, and which stand to cover them?

- ATTACKER declares first, before either side’s attacks. Ordinary Withdrawal requires existing Melee and excludes pinned, berserk and Disrupted units.
- Withdrawing units cannot attack, and declarations cannot be canceled after seeing combat results. Apply the -2 target DRM plus standing-friendly-unit protection.
- SMC departing without a MMC can be singled out. Apply withdrawal modifiers only to withdrawing targets in a mixed attack.

**Checklist behavior:** Commit separate standing and withdrawing groups before attack selection.

**Illustrative position:** A leader can withdraw while a squad stands, but may then be attacked as a separate target.

**Terms:** Withdrawal, Melee, SMC.

#### Handle compulsory broken-unit Withdrawal

ID: `brokenWithdrawal`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.15B. **Rules:** A11.16; A11.2. **PDF pages:** 681, 72, 73. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which broken Infantry must attempt to leave Melee?

- Non-Disrupted, non-Guard broken Infantry must attempt Withdrawal. Broken units never attack but defend with their full unbroken-side FP and the broken-unit DRM.
- Apply the special Failure-to-Rout consequence for a non-Guard broken unit unable to withdraw at Location close; preserve the Disrupted/Guard distinctions.
- After successful Withdrawal, normal rout rules apply in a subsequent RtPh. This is not an extra rout now.

**Checklist behavior:** Show compulsory declaration and any unresolved closing consequence separately.

**Illustrative position:** A broken squad cannot choose to remain in Melee simply because its retreat looks dangerous.

**Terms:** Withdrawal, Disrupted, Guard.

#### Drop excess equipment and evaluate an Accessible exit

ID: `withdrawLoad`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.15B. **Rules:** A4.43; A11.21; G9.5. **PDF pages:** 681, 50, 73, 151, 290. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** What can this withdrawing unit retain, and where can it legally go?

- Retained equipment cannot exceed IPC; offer the legal opening drop instead of refusing every overburdened unit outright.
- Destination must be Accessible under normal advance conditions, possibly creating CX, and cannot contain a Known enemy. Terrain and pillbox restrictions still apply.
- Entering a real concealed enemy’s Location eliminates the withdrawing unit and forces the prescribed enemy reveal; do not disclose that hidden risk in a public destination list.
- Infantry above a Panji counter cannot Withdraw from CC off that counter. A generic Accessible-hex test must not bypass this explicit prohibition.

**Checklist behavior:** Show load choices and viewer-safe endpoints without claiming unknown destinations are safe.

**Illustrative position:** A squad drops an HMG to meet IPC, but an unseen enemy can still make its chosen exit fatal.

**Terms:** IPC, PP, Accessible, CX.

### 8.16B · SMC pairing

#### Declare SMC solo status and defending groups

ID: `pairing`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.16B. **Rules:** A11.14; A11.12. **PDF pages:** 681, 72. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which unit does each SMC attack and defend with?

- ATTACKER establishes pairing/solo status first, before attack declarations. Each SMC contributes one FP in ordinary Personnel CC.
- A solo attacker also defends alone. A paired SMC attacks with its MMC if combining, and normally cannot be singled out separately.
- A Known SMC cannot defend with a concealed unit without resolving the required concealment compatibility; withdrawal can create separate targetability.

**Checklist behavior:** Make defensible groups visible before targeting, and lock them at the proper point.

**Illustrative position:** Pairing a leader with a squad changes both the attack and the smallest target the opponent can select.

**Terms:** SMC, MMC, FP.

#### Choose eligible leadership and heroic support

ID: `leadership`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.16B. **Rules:** A11.141; A15.24. **PDF pages:** 681, 72, 74, 83, 279. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Who may direct this declared attack?

- One leader can direct the attack of the group it defends with and other units joining that combined attack. No leadership DRM for a solo leader or an attack with berserk units.
- For a vehicle target the leader must combine in that attack; apply the separate CCV combination limit. Heroic effects are distinct, and a T-H Hero supplies no ordinary Hero DRM.
- CC does not generate LLMC/LLTC from leader casualties. Do not queue a generic leader-loss check.

**Checklist behavior:** Show FP/CCV contribution separately from the attack modifier.

**Illustrative position:** A solo 9-2 leader does not get to apply its -2 leadership to its own attack.

**Terms:** SMC, DRM, CCV.

### 8.21B · Declare attacks and special modes

#### Declare targets at the correct commitment boundary

ID: `declarations`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.12; A11.3. **PDF pages:** 681, 72, 73. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which attacks must be fixed now, and which can wait?

- In simultaneous CC the ATTACKER declares all attacks, then DEFENDER declares all, before resolution. Preserve each declared strength even if its units are simultaneously lost.
- Sequential CC declares the next attack after prior effects; no eliminated unit gets a later return attack. Passing and exhausted attackers must be tracked.
- Units need not all attack or all be attacked. Enforce ordinary once-per-unit attack/target limits with vehicle and special-rule exceptions.

**Checklist behavior:** Provide a commitment review rather than letting the player retarget after a favorable die roll.

**Illustrative position:** An eliminated squad still resolves its declared simultaneous attack, but not a future sequential one.

**Terms:** Declaration receipt, Simultaneous CC, Sequential CC.

#### Choose whether concealed units attack

ID: `concealedChoice`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.19; A11.4. **PDF pages:** 681, 72, 73, 74. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Keep concealment or commit it to an attack?

- Resolve the concealed participant’s attack/decline intent before final attack odds and targets depend on it; do not let declaration order provide a concealment trap.
- Making or directing an attack normally loses concealment, with successful Ambush retention as the explicit exception. Attack FP against a concealed target is halved.
- A unit retaining concealment is neither held in Melee nor able to hold opponents in Melee. A casualty can change that status.

**Checklist behavior:** Compare attack now with retaining concealment and later freedom of movement.

**Illustrative position:** A concealed defender can decline its attack, leaving the attacker facing reduced odds.

**Terms:** CC, Ambush, FP.

#### Build ordinary Personnel CC odds

ID: `personnelAttack`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.11-.14; A7.8; A4.51; A4.8. **PDF pages:** 681, 52, 58, 72, 73, 87. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Combine or split the attack, and which defending group is the target?

- Use eligible inherent FP, including SMC groups, and round the odds down to a printed CCT column. Ordinary SW/Guns do not add attack FP.
- Apply applicable Pin, Guard, concealment, CX, TI, broken-target, withdrawal, Ambush and stacking effects at their proper calculation stages.
- No PBF/TPBF, TEM or LOS Hindrance modifies ordinary CC. Keep different target DRM separate rather than assigning one result to an unlike group.

**Checklist behavior:** Show strength arithmetic, odds, Kill Number and target-specific modifiers before commitment.

**Illustrative position:** Two squads may combine against one defender or split attacks, with different odds and risks.

**Terms:** FP, CCT, DRM.

#### Choose or require Hand-to-Hand under the active profile

ID: `handToHand`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** J2.31; A25.43; G1.64; G18.62; SSR RB11. **PDF pages:** 681, 96, 281, 322, 602. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this nationality, Deluxe rule or SSR permit or require HtH?

- HtH is not a universal toggle. Deluxe/SSR ATTACKER permissions, Gurkha choice, Japanese automatic conditions and Dare-Death rules differ; respect Ambush, Pin and Withdrawal exceptions.
- Use red Kill Numbers for eligible Personnel combat and retain HtH for the continuing Melee. Vehicle/PRC/pillbox attacks do not acquire HtH benefits.
- Japanese/Gurkha qualifying HtH attacks receive their extra modifier. Mixed vehicle Locations still require separate eligible Personnel and vehicle procedures.

**Checklist behavior:** Show the enabling profile, whether optional or mandatory, and which attacks it affects.

**Illustrative position:** Japanese eligible attacking Infantry may require HtH while an AFV in the same Location still uses its own procedure.

**Terms:** HtH, CCT, Dare-Death.

#### Declare capture instead of elimination

ID: `capture`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A20.22-.221; A11.52. **PDF pages:** 681, 74, 87. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is capture legal for this attacker and target, and worth the changed modifier?

- A non-vehicular, non-berserk attacker must predesignate capture against a permitted target; manned AFV and Motion/non-stopped vehicle restrictions apply.
- Normal capture adds +1, replaced by -1 against Inexperienced Personnel. Mixed target classes can yield different results from the same roll.
- Below the Kill Number captures all affected defenders; equality captures one defender-selected unit or a HS of a squad. No Quarter does not replace these rules with a generic execute option.

**Checklist behavior:** Keep attack mode and target-specific capture results in the declaration receipt.

**Illustrative position:** A Partial Kill capture lets the defender select the captured unit rather than using ordinary casualty Random Selection.

**Terms:** Prisoners, Partial Kill, No Quarter.

#### Build a Personnel attack against a vehicle

ID: `vehicleAttack`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.5-.51; A11.61. **PDF pages:** 681, 74, 75. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which unit and optional SMC make this vehicle attack?

- Use CCV: squad 5, crew 4, HS 3, SMC 2, with applicable Assault Engineer, Inexperienced and halving penalties.
- At most two units combine, one a SMC adding one to the other unit’s CCV. Do not sum several squads or mix Personnel and vehicle targets in one attack.
- Evaluate current escort, Motion/non-stopped, armor/exposure, immobility and manned functioning MG modifiers at this attack, plus valid leadership/hero/Ambush/Street Fighting effects.

**Checklist behavior:** Display the vehicle attack’s CCV and modifiers separately from Personnel odds.

**Illustrative position:** Eliminating an escort in an earlier sequential attack can improve the next attack on the tank.

**Terms:** CCV, AFV, Personnel Escort.

#### Allocate eligible vehicle CC weapons

ID: `vehicleFire`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.62; D1.82. **PDF pages:** 681, 76. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which functioning armament can attack, separately or together?

- Check eligible AAMG, unrestricted CMG, RMG, turreted MA IFE ≤15mm and allowed halftrack passenger fire. A BMG is not a CC weapon.
- Vehicle FP forms CCT odds against Personnel CCV, using black Kill Numbers. Apply cumulative halving and Shock/Stun limits; no ROF or Intensive Fire sequence is created.
- Weapons may combine or make separate legal attacks. Track each used weapon and target exceptions instead of spending one generic vehicle attack.

**Checklist behavior:** Show available weapon allocations with their own consumed status.

**Illustrative position:** A tank can use its sN separately and still have an eligible CMG attack later in the sequence.

**Terms:** AAMG, CMG, RMG, IFE, CCV.

#### Handle Riders, Passengers and vehicle capture targets

ID: `prc`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.52; A11.611; A11.71. **PDF pages:** 681, 74, 75, 76. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Can these occupants be attacked separately or only with their vehicle?

- Riders may be attacked separately with their own attack/defense modifiers. Other PRC normally share vehicle destruction, with no survival roll in CC.
- For capturing an eligible stationary unarmored vehicle, all other enemy Personnel must be eliminated/captured and its Passengers captured through the special Personnel CC procedure.
- Apply passenger attack/defense and capture modifiers; do not offer capture of a manned AFV or Motion/non-stopped vehicle.

**Checklist behavior:** Separate Rider targets, special Passenger capture and vehicle-wide consequences.

**Illustrative position:** Destroying an AFV in CC eliminates its PRC without a crew-survival choice.

**Terms:** PRC, CCV, Prisoners.

#### Consider the separate sN close-defense attack

ID: `closeDefense`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A11.622; D13.34. **PDF pages:** 681, 76, 212. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this equipped AFV eligible to use its close-defense projector now?

- Require the correct equipment/date and BU status. Normally the AFV or escort must already have been attacked this CCPh; a qualifying ambusher has an exception.
- Use a separate 16-FP IFT attack on every unarmored unit in the Location, including friendly units and vulnerable PRC. Never combine it with MG FP.
- Apply its Original-DR Usage Number test and special CC modifiers, without ordinary TEM/SMOKE. Do not add the usual BU smoke-dispenser usage penalty to this HE attack.

**Checklist behavior:** Preview friendly exposure and the exact eligibility trigger before commitment.

**Illustrative position:** A tank can hurt its own escort with sN, so the choice belongs beside the target consequences.

**Terms:** sN, IFT, BU.

#### Apply pillbox, sewer, cave and obstacle exceptions

ID: `terrainCombat`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** B30.6-.7; B8.44; G11.87; B26.31; G9.5-.56. **PDF pages:** 681, 117, 145, 151, 287, 290, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the terrain change attack eligibility, modifiers or continuing Melee?

- Pillbox CC crosses its separate Location boundary only under B30.6; friendly outside units can block it, vehicle/PRC versus occupants is barred, and HtH/Melee do not result.
- Sewer concealment prevents ordinary Melee lock. Units inside a cave cannot conduct CC with units outside it.
- Apply above-Wire, Panji and bank status consequences to their actual subjects. Do not reintroduce general terrain TEM into CC.

**Checklist behavior:** Explain terrain-specific targeting and post-combat status.

**Illustrative position:** Pillbox occupants and outside Infantry may fight yet remain outside ordinary Melee afterward.

**Terms:** Pillbox, Melee, Wire, Panji.

#### Apply mounted and water-combat profiles

ID: `mountedWater`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.21B. **Rules:** A13.7; A11.71; E4.5; E5.6; E6.2; G13.495. **PDF pages:** 681, 76, 82, 232, 234, 235, 312. **Coverage:** Rules integration pending; reference available.

**Consider:** Are these combatants mounted, swimming or in an Ocean Location?

- Apply the appropriate Cavalry/cyclist/skier/boat-Passenger modifiers and Melee exemptions. Mounted status is not erased at combat start.
- In allowed Water Obstacles boat Passengers fight, but the boat is not a separate CC target; it is destroyed with all its Passengers or captured by capturing them.
- Swimmers have their unarmed Personnel factor and no CCV. CC is not allowed in non-frozen Ocean Locations; do not generalize river combat to surf.

**Checklist behavior:** Select the actual transport/terrain profile before offering attack arithmetic.

**Illustrative position:** Boat passengers in a river can fight, while non-frozen Ocean occupants cannot use the same attack.

**Terms:** CC, PRC, Skis.

#### Keep a Goliath’s special detonation option

ID: `goliath`. **Who:** Controller. **Type:** Equipment exception.

**Timing:** ASOP 8.21B. **Rules:** Chapter H German Vehicle Note 93. **PDF pages:** 681, 348. **Coverage:** Rules integration pending; reference available.

**Consider:** Does an eligible controller have a legal detonation opportunity?

- A Goliath has sequential CC and can attack in its sequential window by controller detonation. Apply its own control, target and explosion rules.
- It cannot be captured in CC. Do not treat the general ban on ordinary SW use as eliminating this explicit equipment exception.

**Checklist behavior:** Expose a pending equipment-specific option when the scenario includes the device.

**Illustrative position:** A surviving Goliath can detonate at its turn in the sequence if its controller remains eligible.

**Terms:** Sequential CC, DC.

### 8.22B · ATMM after attack declaration

#### Attempt a German ATMM check

ID: `atmm`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.22B. **Rules:** C13.7-.74. **PDF pages:** 681, 185. **Coverage:** Rules integration pending; reference available.

**Consider:** Would the ATMM chance justify the risk of pinning the attacker?

- For eligible 1944-45 German Infantry, declare the vehicle attack before the availability dr. Exclude pinned/broken units and wagon/motorcycle targets.
- Success supplies -3 to that attack; apply HS/crew +1, SMC +2, CX +1 and non-armored-target +1. Leadership/heroism does not modify the check.
- Original 6 pins even a berserk unit and changes CCV. Only one combined participant attempts the check; Original attack DR 12 negates the ATMM modifier.

**Checklist behavior:** Show the success benefit and check-failure consequence without allowing post-check retargeting.

**Illustrative position:** The squad fails its ATMM check with Original 6 and must resolve its declared attack with the new Pin penalty.

**Terms:** ATMM, CCV, CX.

#### Use the T-H Hero’s distinct ATMM procedure

ID: `heroAtmm`. **Who:** Japanese player. **Type:** Combat decision.

**Timing:** ASOP 8.22B. **Rules:** G1.4231; C13.73. **PDF pages:** 681, 279. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this unpinned hero attempt its special ATMM check?

- Final dr ≤3 succeeds, with only the pre-1944 +1 drm; Original 6 does not Pin this hero. Other Japanese units cannot use this permission.
- If paired, the hero adds one FP/CCV to its partner and may contribute its ATMM modifier under the combined-attack rule; its standalone CCV is five.

**Checklist behavior:** Use the hero-specific check rather than the German modifier list.

**Illustrative position:** A pre-1944 T-H Hero has a different availability modifier and avoids the German Original-6 Pin result.

**Terms:** T-H Hero, ATMM, CCV.

### 8.23B · Japanese response to capture

#### Offer eligible Hara-Kiri before a capture attempt resolves

ID: `haraKiri`. **Who:** Japanese player. **Type:** Combat decision.

**Timing:** ASOP 8.23B. **Rules:** G1.641. **PDF pages:** 681, 281. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the Japanese defender choose this immediate response?

- Offer immediately before a CC Capture attempt resolves, even after declaring its own attack, but not after it has already made that attack.
- Berserk/heroic units may eliminate themselves automatically; otherwise resolve the NTC with the specified Inexperienced/Unarmed and paired-SMC modifiers. Best SMC acts first.
- Failure forfeits that unit’s CC attack and benefits the capture attempt by the specified extra -1. Apply the appropriate VP and casualty-tally consequences on success.

**Checklist behavior:** Switch decision ownership to the affected Japanese player at the exact response window.

**Illustrative position:** A failed attempt can both cancel the defender’s attack and make its capture easier.

**Terms:** Hara-Kiri, NTC, SMC.

### 8.24B · Resolve attacks and immediate consequences

#### Resolve the committed attack and target-specific results

ID: `resolve`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.11-.12; A20.22. **PDF pages:** 681, 72, 73, 87. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which declared strength and modifier set applies to this roll?

- Ordinary Personnel CC below the Kill Number eliminates; equality causes Casualty Reduction through Random Selection, including ties; above normally has no effect.
- Keep capture equality’s defender choice separate. Preserve Original, Final and colored-die values for special triggers.
- In simultaneous CC keep already-declared attacks despite casualties. In sequential CC apply changes before offering the next declaration.

**Checklist behavior:** Render a result receipt that explains each target outcome and remaining obligations.

**Illustrative position:** One mixed attack can affect withdrawing and standing defenders differently.

**Terms:** CCT, Partial Kill, DR.

#### Resolve vehicle destruction, burning or immobilization

ID: `vehicleResult`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.5; A11.611. **PDF pages:** 681, 74, 75. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** What does the Final vehicle-attack DR do?

- Below CCV eliminates the vehicle; at or below half CCV makes a burning wreck; equality immobilizes it.
- Vehicle destruction eliminates PRC without a crew-survival roll. Apply immobilization and burning consequences before subsequent attacks.
- Do not use the Personnel Partial Kill rule or its Random Selection for this result.

**Checklist behavior:** Show the vehicle result independently of Personnel odds.

**Illustrative position:** An initial immobilization improves the conditions for a later attack against the same vehicle.

**Terms:** CCV, PRC.

#### Offer the Original-2 Unlikely Kill follow-up

ID: `unlikelyKill`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.501. **PDF pages:** 681, 74, 85. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does this Original 2 give a further vehicle-result chance?

- The attacker may roll the subsequent die: 1 burns, 2 eliminates, 3 immobilizes, otherwise no additional effect.
- Retain any elimination/immobilization already earned by the Original attack; the follow-up cannot worsen that result, but can improve it.
- Resolve the associated Infiltration and eligible Field Promotion consequences in their proper order rather than treating the extra die as the whole result.

**Checklist behavior:** Show the existing guaranteed result before offering the optional improving die.

**Illustrative position:** An attack that already immobilized the tank keeps that result even if the extra die has no effect.

**Terms:** CCV, dr.

#### Apply Original-12 crew small arms and ATMM consequences

ID: `crewSmallArms`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.621; C13.74. **PDF pages:** 681, 76, 85, 185. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does this failed vehicle attack also harm the attackers?

- Original 12 against a non-Abandoned crewed vehicle that is neither Shocked nor Stunned causes Casualty Reduction to its attackers.
- Resolve the ATMM malfunction rule and each eligible casualty/wound procedure. Do not treat a high Final DR alone as the Original-12 trigger.

**Checklist behavior:** Keep self-inflicted consequences visible even when the vehicle was not harmed.

**Illustrative position:** A squad’s Original 12 can reduce it despite doing nothing to the tank.

**Terms:** DR, ATMM, Wound.

#### Offer immediate Infiltration withdrawal

ID: `infiltration`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.22. **PDF pages:** 681, 73. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does this Original 2 or 12 open an immediate escape?

- An eligible Infantry/Cavalry attacker rolling Original 2 may withdraw immediately after its attack; eligible targets of Original 12 may likewise withdraw after that attack.
- Respect already eliminated/captured/pinned status and the restrictions of the relevant unit profile. Do not allow the choice to be saved until its own attack or later results.
- Resolve the Accessible destination, portage and movement consequences through the proper Withdrawal rules.

**Checklist behavior:** Pause combat for the entitled player’s use-or-decline decision.

**Illustrative position:** A target of Original 12 cannot make its pending attack and then claim the earlier Infiltration escape.

**Terms:** Infiltration, Withdrawal, DR.

#### Resolve a possible CC Field Promotion

ID: `promotion`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A18.12-.2. **PDF pages:** 681, 85. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Did an attacking MMC roll Original 2?

- Use the Leader Creation table, nationality/status and attack conditions, including applicable CC-versus-AFV modifier. Apply national exceptions rather than creating a leader universally.
- A created leader cannot be refused. Unless Infiltration removed the units, recompute both attacks with their original rolls, new FP/leadership and required defending association.
- Do not reroll attacks or omit the creation opportunity merely because the target was a vehicle.

**Checklist behavior:** Link the creation result to any required recalculation of already displayed combat.

**Illustrative position:** A newly created leader can change an attack result without changing the dice already rolled.

**Terms:** SMC, MMC, DRM.

#### Resolve eliminated possessors’ SW and Gun fate

ID: `equipmentLoss`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.13; A20.24. **PDF pages:** 681, 72, 87. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which equipment survives the loss of its possessor?

- When the Original colored CC die is 1, check each SW/Gun of an eliminated possessor with the subsequent dr against the black Kill Number.
- Other equipment remains unpossessed; captured units first abandon SW. Do not automatically assign a captured weapon to the victor or use red HtH numbers for the destruction check.

**Checklist behavior:** Keep equipment-loss rolls separate from casualty and capture results.

**Illustrative position:** Winning HtH does not automatically destroy the defeated squad’s MG.

**Terms:** SW, dr, CCT.

#### Reconcile simultaneous captures after both sides resolve

ID: `captureValidity`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A20.221. **PDF pages:** 681, 87. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the capturing force survive to keep its prisoners?

- If a side is completely eliminated/captured in simultaneous CC, its captures in that Location during the phase are ineffective.
- Apply the special mutual-elimination/capture rule for remaining unarmed prisoners, rearming and any newly opposing armed units in Melee.
- Do not finalize prisoner ownership, VP or freedom after only the first side’s roll.

**Checklist behavior:** Mark captures provisional until the simultaneous exchange is settled.

**Illustrative position:** A squad apparently captures its opponent, but its own simultaneous elimination can undo that capture.

**Terms:** Simultaneous CC, Prisoners, Melee.

#### Assign Guards and resolve escape or rearming

ID: `guards`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A20.5-.55; A20.551. **PDF pages:** 681, 87, 88. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Who guards surviving prisoners, and which escaped units become armed?

- Assign a legal Guard and capacity, including replacement when a Guard is eliminated. An unbroken Guard squad may use its automatic Deployment permission.
- Guard attack FP versus non-prisoners is halved, not its defense. A CC attack on a Guard does not also attack its prisoners.
- Escaping requires the specified absence of enemies or successful Withdrawal/Infiltration. Apply rearming by eliminated/captured armed enemy and the escaped-SMC rule; no free Scrounging action occurs here.

**Checklist behavior:** Track Guard identity, capacity, escape, rearming and VP as distinct consequences.

**Illustrative position:** Eliminating a broken Guard may leave prisoners in Melee with another enemy unit rather than free to leave.

**Terms:** Guard, Prisoners, Melee.

#### Resolve successful Withdrawal and hidden-enemy entry

ID: `withdrawArrival`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.2-.22; A11.41. **PDF pages:** 681, 73, 74, 148. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Did the withdrawing subject survive and reach a legal endpoint?

- For ordinary Melee Withdrawal, elimination, reduction or capture prevents successful departure. Use the particular exception for Ambush or Infiltration rather than applying all ordinary attack penalties.
- Apply ≤IPC, normal advance accessibility, difficult terrain and Known-enemy exclusion. A real concealed enemy destination causes elimination and the prescribed reveal.
- Record arrival and concealment effects only after origin hazards permit departure; never pre-disclose hidden occupants through route validation.

**Checklist behavior:** Use a movement receipt tied to the actual withdrawal type.

**Illustrative position:** A secretly occupied escape destination can eliminate the withdrawing unit despite a successful combat escape.

**Terms:** Withdrawal, Accessible, HIP.

#### Resolve the Street Fighting return choice

ID: `streetReturn`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A11.8. **PDF pages:** 681, 76. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Return to the origin building or remain in the road?

- Offer the return tied to eligible Street Fighting provenance after the applicable CC attack; do not treat it as ordinary Melee Withdrawal.
- Broken, pinned or above-Wire subjects cannot return. Resolve mines/FFE normally on the return and preserve the resulting encounter state.

**Checklist behavior:** Show the actual original Location and its movement consequences.

**Illustrative position:** A Street Fighter can choose to stay beside the vehicle rather than return, if otherwise eligible.

**Terms:** Street Fighting, Withdrawal, Wire.

#### Resolve FFE and mines against withdrawing subjects

ID: `withdrawHazards`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** C1.51-.53; C1.71-.72; C1.9; E12.5; G14.65-.661; B28.41-.412. **PDF pages:** 681, 148, 165, 166, 167, 247, 316, 317. **Coverage:** Rules integration pending; reference available.

**Consider:** Which attacks occur on this departure or entry?

- Resolve each applicable existing FFE/mine attack against the withdrawing unit/stack only. Stationary occupants are not attacked again because someone moves.
- Exit mines resolve in the origin; a disabling result can prevent arrival and destination discovery. Apply entry mines only after successful exit.
- Keep Harassing Fire, Barrage, Rocket, naval and terrain exceptions available. No new artillery mission action or ordinary defensive-fire window is created.

**Checklist behavior:** Interleave hazards with each withdrawal rather than batch them after combat closes.

**Illustrative position:** A unit survives CC but is stopped by an exit mine attack before reaching its intended cover.

**Terms:** FFE, WP, Withdrawal.

#### Check Panji, trip flares and Column Disbandment

ID: `withdrawSpecial`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** G9.41-.45; E1.95; E11.532-.534. **PDF pages:** 681, 227, 246, 289. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this actual movement or attack trigger a special effect?

- Apply Panji crossing and stack rules with their listed exemptions; do not assume every withdrawal is exempt simply because normal advance can be.
- Resolve trip-flare activation at the triggering event. A flare activated in CCPh survives until the next Player Turn’s AFPh.
- Check Column Disbandment after relevant attacks/movement using E11.532 and the special E11.533 proximity rule; ASOP’s reference to .533 does not replace the general trigger.

**Checklist behavior:** Attach the special event and its future expiry to the movement receipt.

**Illustrative position:** A successful withdrawal sets off a flare that must remain after this turn’s night cleanup.

**Terms:** Panji, Trip Flare, Column.

#### Restore eligible berserk units after eliminating their enemies

ID: `berserk`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.24B. **Rules:** A15.46. **PDF pages:** 681, 84, 322. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Has this berserk unit eliminated all Known enemy units here, including at least one?

- Apply the stated elimination condition and restore Good Order when required; mere absence of an enemy at the start does not establish the trigger.
- Keep Dare-Death voluntary-berserk recovery and T-H Hero removal separate. Recompute subsequent legal actions and closing Melee using the resulting state.

**Checklist behavior:** Explain why berserk status ends or remains before the Location closes.

**Illustrative position:** A berserk unit that eliminates its last Known opponent can return to normal before the next Location is selected.

**Terms:** Good Order, CC.

#### Continue the sequential loop or finish the exchange

ID: `nextAttack`. **Who:** BOTH. **Type:** Sequence continuation.

**Timing:** ASOP 8.24B. **Rules:** A11.3-.34; ASOP 8.24B. **PDF pages:** 681, 73, 74, 76. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does an entitled side still have an unused legal attack?

- Complete casualties, capture responses, Infiltration and other immediate effects before reopening 8.21B for the next attack.
- Recompute targets, escorts, weapon use and ownership of the next decision. Do not reset spent attacks or allow already eliminated units to act.

**Checklist behavior:** Show the next owner and remaining attacks rather than a generic Next button.

**Illustrative position:** After an escort is eliminated, the next attacker sees the tank’s revised protection.

**Terms:** Sequential CC, Declaration receipt.

### 8.25B · New prisoner information

#### Resolve Interrogation when the optional rule is in use

ID: `interrogation`. **Who:** Captor. **Type:** Conditional procedure.

**Timing:** ASOP 8.25B. **Rules:** E2.1-.3; G1.621; G18.71. **PDF pages:** 681, 228, 280, 322. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this a new eligible prisoner under the active rules profile?

- When E2 is in use, apply the immediate one-DR-per-prisoner procedure; a failed or unusable result does not permit another roll for that prisoner.
- Apply prisoner/Guard and nationality/scenario modifiers, including Japanese 1944-45 and Chinese allegiance provisions.
- Apply only the required disclosure, valid fallback result and prisoner-side tie choice. Do not reveal hidden positions to both clients while calculating privately.

**Checklist behavior:** Record the prisoner’s interrogated status and the authorized information change.

**Illustrative position:** A prisoner with no usable first result cannot be interrogated again after the next attack.

**Terms:** Interrogation, Prisoners, HIP.

### 8.31B · Close this Location

#### Capture eligible unescorted abandoned vehicles

ID: `automaticCapture`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.31B. **Rules:** A21.2-.22; A11.52. **PDF pages:** 681, 74, 88. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is automatic capture now allowed after this Location’s combat?

- If the conditions are met, assign the capturing Infantry and vehicle ownership at Location close. No extra Ambush roll is made against an abandoned vehicle alone.
- Apply temporary driver/crew consequences, including a squad’s exchange for a HS and SMC restrictions; do not hand out a fully qualified free crew.
- Preserve captured-use, movement and VP requirements for later phases. Current coverage includes capture foundations, but remaining detailed consequences stay labeled.

**Checklist behavior:** Show who becomes the crew and which Infantry remains outside.

**Illustrative position:** Capturing an armed vehicle consumes the proper capturing manpower rather than duplicating it.

**Terms:** AFV, SMC, MMC.

#### Determine Melee per surviving unit

ID: `melee`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.31B. **Rules:** A11.15; A11.7-.71; B30.6; B8.44. **PDF pages:** 681, 72, 76, 117, 151. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Who is actually held, who holds the enemy, and who remains free?

- Retained concealment neither holds nor is held in Melee. Vehicles are never held, but qualifying non-Abandoned unbroken Stopped vehicles can hold Known enemy Infantry.
- Apply mobile/immobile PRC, Cavalry/cyclist/skier, sewer and pillbox exceptions. Do not mark every surviving counter identically.
- Flip/remove CC or retain/remove Melee as appropriate; carry future movement/fire restrictions to the next turn.

**Checklist behavior:** Record a per-unit hold reason rather than one blanket Location flag.

**Illustrative position:** A stopped tank holds enemy Infantry even though the tank itself is free to move in its next MPh.

**Terms:** Melee, PRC, CC.

#### End voluntary Dare-Death berserk status when permitted

ID: `dareDeath`. **Who:** Chinese player. **Type:** Required status.

**Timing:** ASOP 8.31B. **Rules:** G18.6-.62. **PDF pages:** 681, 322. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this voluntarily berserk unit remain in Melee?

- Voluntarily berserk Dare-Death participants return to normal at turn end unless still in Melee; preserve provenance rather than treating every berserk result alike.
- Retain applicable Dare-Death CC attributes even when no longer berserk. Resolve ordinary A15.46 recovery separately.

**Checklist behavior:** Show the rule and state that preserve or remove this specific berserk marker.

**Illustrative position:** A voluntary Dare-Death unit outside Melee recovers, while one still held remains berserk.

**Terms:** Dare-Death, Melee.

#### Remove spent Tank-Hunter Heroes at their rule window

ID: `heroRemoval`. **Who:** Japanese player. **Type:** Required expiry.

**Timing:** Immediately after its Designated-Target attack; otherwise G1.425 turn-end/Melee continuation. **Rules:** G1.425. **PDF pages:** 681, 280. **Coverage:** Rules integration pending; reference available.

**Consider:** Has the hero completed its permitted CC attack?

- Remove a non-hidden T-H Hero immediately after its CC attack against its Designated Target. If it makes no such attack, remove it at the end of its creation/HIP-loss Player Turn unless held in Melee; then remove it when freed. Earlier loss of the Designated Target eliminates it immediately.
- Preserve related DC/equipment and VP rules. Do not use the general Melee-survivor routine to keep it in play.

**Checklist behavior:** Carry the hero-specific expiry through the attack and Location closing record.

**Illustrative position:** A surviving T-H Hero attack does not imply the hero remains for another turn.

**Terms:** T-H Hero, CC.

### 8.41B · Phase-wide aerial combat

#### Declare Dogfight groups and initial targets

ID: `dogfight`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.41B. **Rules:** E7.22-.221. **PDF pages:** 681, 236. **Coverage:** Rules integration pending; reference available.

**Consider:** Which eligible aircraft enter combat, and what is each first target?

- Only an undamaged FB voluntarily initiates Aerial Combat. Form the allowed aircraft groups and account for existing Aerial Melee.
- Predesignate every aircraft’s initial target, ATTACKER first, before results. Despite sequential resolution, an aircraft cannot wait to pick its initial target.
- Resolve all ATTACKER attacks, including retained ROF, before surviving DEFENDER return attacks.

**Checklist behavior:** Use a distinct aerial declaration receipt rather than the ground CC alternation rule.

**Illustrative position:** A second fighter cannot change its declared first target because the first fighter destroyed another plane.

**Terms:** Dogfight, FB, Aerial Melee.

#### Resolve aerial attacks, damage and retained ROF

ID: `aerialResults`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.41B. **Rules:** E7.221-.223; E7.226. **PDF pages:** 681, 236. **Coverage:** Rules integration pending; reference available.

**Consider:** What does this attack do, and may the FB attack again?

- Apply the aerial modifiers and table: Final DR ≤4 eliminates; 5 Damages. A second Damage eliminates; surviving damaged aircraft face Recall/Aerial Melee rules.
- FB ROF is 1 with bombs or 2 without. A retained-ROF follow-up may choose another target in its Dogfight, after its required initial attack.
- Original 12 disables MG; Original 11 by a FB attacking a Stuka invokes its special rear-gunner result. Do not use ground CC Infiltration or Field Promotion.

**Checklist behavior:** Display aircraft-specific outcomes and remaining ROF before return fire.

**Illustrative position:** A fighter retains ROF and can select another plane after resolving its declared initial target.

**Terms:** Dogfight, ROF, DR.

#### Choose Recall and determine continuing Aerial Melee

ID: `aerialClose`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.41B. **Rules:** E7.22-.226; E7.24. **PDF pages:** 681, 236. **Coverage:** Rules integration pending; reference available.

**Consider:** Which aircraft must leave, may leave, or remain held?

- A Final attack DR ≥10 gives its target an end-CCPh Recall option. Damaged or MG-disabled aircraft have their own Recall and enemy-FB constraints.
- Continuing Aerial Melee requires a qualifying undamaged FB with functioning MG wishing to hold opponents. Ordinary Infantry Withdrawal rules do not apply.
- Bomb jettison is a separate own-MPh option, not a late CCPh way to alter already declared attacks.

**Checklist behavior:** Resolve each aircraft’s exit decision and next-turn hold status.

**Illustrative position:** A damaged bomber may be held in Aerial Melee rather than automatically disappearing at once.

**Terms:** Aerial Melee, Dogfight.

### 8.42B · Complete declared tasks

#### Validate pending non-Flame Clearance tasks

ID: `clearance`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B24.7; B24.8. **PDF pages:** 681, 142, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** Which earlier declarations remain eligible to resolve now?

- Use the carried declaration, target and participants from the proper MPh/DFPh window. Pin or loss of Good Order can end participation.
- Combine attempts against the same item in the same phase as required; apply task-specific leadership, Sapper, Hero, dozer and Labor modifiers.
- Do not offer a fresh uncommitted task or resolve Flame Clearance here. Resolve tasks before ordinary TI/Pin cleanup.

**Checklist behavior:** Make the declaration’s history and current eligibility visible.

**Illustrative position:** A pinned clearing squad cannot regain eligibility by clearing Pin early.

**Terms:** Clearance, TI, Labor.

#### Complete rubble or roadblock Clearance

ID: `rubble`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B24.71; B24.76; B29.5. **PDF pages:** 681, 142, 150. **Coverage:** Rules integration pending; reference available.

**Consider:** Which terrain change does a successful task create?

- Rubble Clearance in a qualifying non-building hex places a TB through chosen hexsides, preserving terrain LOS/TEM until complete removal conditions are met.
- Roadblock Clearance removes the specified obstacle; use the correct earlier declaration and eligible Infantry/dozer path rather than treating both tasks as identical.

**Checklist behavior:** Preview the affected hexsides and exact surviving terrain.

**Illustrative position:** A cleared rubble lane lowers entry cost along its TB without instantly removing all rubble TEM.

**Terms:** Clearance, TB.

#### Complete Wire or Set DC Clearance

ID: `wireSetDc`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B24.73; B24.75. **PDF pages:** 681, 142. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the qualified clearing party retain the required position and status?

- Wire removal requires the prescribed below-Wire position and prior task commitment. Being above Wire is not enough.
- Set DC removal has its own same-Location declaration and eligibility. Resolve the Clearance result; do not detonate or Recover the device as a substitute.

**Checklist behavior:** Keep target identity and above/below status in the task record.

**Illustrative position:** A squad below Wire can finish its declared removal while a squad above it cannot assist as though colocated.

**Terms:** Clearance, Wire, DC.

#### Resolve Infantry minefield lanes and mishaps

ID: `mineClearance`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B24.74; B28.6; B28.8. **PDF pages:** 681, 142, 148, 149. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the task establish a safe lane, or cause casualties?

- Resolve the committed mine Clearance and any Sapper benefit. On success place the legal TB including a previously established partial TB hexside.
- Original 12, or 11-12 for Inexperienced clearers, causes the specified Casualty Reduction. Preserve the partial lane’s user/exit limitations until a full result exists.
- Only when every hexside is crossed by TB does full removal occur; a single success is not blanket minefield deletion.

**Checklist behavior:** Show lane geometry and casualty risk independently of the success total.

**Illustrative position:** A failed task can leave its entering unit a special exit without making that route safe for everyone.

**Terms:** Clearance, TB, DR.

#### Resolve a Flail Tank’s special mine Clearance

ID: `flail`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B28.7-.72. **PDF pages:** 681, 149. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the Flail Tank complete the qualifying movement and remain eligible?

- Require the proper entire-MPh commitment, no disqualifying fire/immobility/elimination and no friendly Infantry/Cavalry/vulnerable PRC in the minefield.
- Use its special Final DR ≤10 and legal entry-hexside-to-current-VCA TB. Original 11 destroys the mechanism; Original 12 has the A-T/A-P-specific destruction or immobilization result.

**Checklist behavior:** Use a separate vehicle procedure rather than the Infantry ≤2 target.

**Illustrative position:** A tank can survive while losing its flail mechanism and future Clearance ability.

**Terms:** Clearance, TB, DR.

#### Complete jungle/bamboo paths or Panji Clearance

ID: `ptoClearance`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** G2.7; G9.71. **PDF pages:** 681, 282, 290. **Coverage:** Rules integration pending; reference available.

**Consider:** Which path or Covered hexside was the declared target?

- Path creation uses its restricted armed MMC eligibility and dedicated modifier list, including light jungle, bamboo and CX, not the generic extra-manpower benefits.
- Panji Clearance changes a predesignated Covered hexside to non-Covered using Wire-like procedures. Units above Panji cannot attempt, direct or assist either task.
- Preserve the chosen path hexsides and remaining obstacle state.

**Checklist behavior:** Show path creation and Panji hexside removal as distinct effects.

**Illustrative position:** A successful Panji task opens one predesignated hexside rather than deleting the entire counter.

**Terms:** Clearance, Panji, Labor.

#### Keep debris and dozer-specific task profiles available

ID: `moduleClearance`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** O1.5; G15.2-.22; SSR. **PDF pages:** 681, 318. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this scenario add a specialized pending task?

- ASOP explicitly includes O1.5 Debris Clearance. Preserve it as a separate selectable profile with its source verification status; do not silently treat Debris as rubble.
- Dozer terrain/fortification/cave tasks use their own prior commitments, mobility, Melee and result requirements. An unsupported profile must remain visible as pending.
- Require the scenario/module source and task-specific result before a live execution is offered.

**Checklist behavior:** Keep named source gaps distinct from absent engine support.

**Illustrative position:** A Red Barricades debris task remains represented even when the current rules package cannot execute it.

**Terms:** Clearance, SSR.

#### Update Labor after an unsuccessful eligible task

ID: `labor`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B24.8. **PDF pages:** 681, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this failure establish or improve the task’s Labor status?

- Apply the -1 then -2 progression to that same task; new participants can join a continuing task under the rule.
- Remove Labor when the task succeeds or all relevant MMC are eliminated/leave. Do not carry the benefit to a different target or treat every phase without an attempt as a failure.

**Checklist behavior:** Attach Labor to the task and its surviving work party.

**Illustrative position:** A failed Wire task does not grant a bonus to clearing nearby mines.

**Terms:** Labor, Clearance.

#### Resolve destruction of Recovered tunnel entrances

ID: `tunnels`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** ASOP 8.42B. **Rules:** B8.63; ASOP 8.42B. **PDF pages:** 681, 118. **Coverage:** Rules integration pending; reference available.

**Consider:** Which entrance has been lawfully discovered and can be destroyed?

- Require the used-in-LOS and subsequent Recovery discovery history, eligible Good Order Infantry and no Known enemy in its Location.
- The ASOP places Recovered-entrance elimination here; preserve B8.63’s eligibility and destruction permission. Do not reveal entrances merely because a Search happened nearby.

**Checklist behavior:** Show the discovered entrance and qualified destruction decision before removing it.

**Illustrative position:** An unrecovered secret tunnel is not deleted by a generic end-turn cleanup.

**Terms:** Tunnel, Good Order.

### 8.43B · Status and replenishment

#### Remove Pin and ordinary TI after tasks resolve

ID: `pinTi`. **Who:** BOTH. **Type:** Required cleanup.

**Timing:** ASOP 8.43B. **Rules:** ASOP 8.43B; A4.8; B24.721; B25.15; E10.3; G10.11; G12.68. **PDF pages:** 681, 52, 142, 143, 244, 291, 308. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which markers expire now, and which belong to continuing tasks?

- Remove the appropriate Pin and ordinary TI markers only after combat and Clearance windows finish.
- Do not remove Ammo Replenishment TI prematurely or Animal-Pack counters. Retain their multi-turn task history and later completion windows. Classify Pin markers by their cause: LC-Blaze Pin follows G12.68, and new/Hampered Flame markers carry terrain history rather than personnel status.

**Checklist behavior:** Use a rule-owned expiry list, not a blanket remove-all-status command.

**Illustrative position:** The clearing squad loses ordinary TI, but a Gun being Animal-Packed remains Pack-TI.

**Terms:** TI, Pack-TI.

#### Convert Stun while preserving cumulative effects

ID: `stun`. **Who:** BOTH. **Type:** Required cleanup.

**Timing:** ASOP 8.43B. **Rules:** D5.34; G12.111; G14.33. **PDF pages:** 681, 203, 302, 314. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which Stun flips to a lasting penalty, and which uses a cumulative profile?

- Flip ordinary Stun to its +1 side at the proper time; do not erase the penalty or equate this with a free CE change.
- Apply LC and seaborne cumulative-Stun exceptions. Preserve Recall and crew consequences under the applicable vehicle profile.

**Checklist behavior:** Explain recovery of immediate capabilities separately from retained penalties.

**Illustrative position:** An AFV’s Stun expires as an immediate restriction while its +1 remains.

**Terms:** BU, PRC.

#### Complete or continue Ammo Replenishment

ID: `ammo`. **Who:** BOTH. **Type:** Required task check.

**Timing:** ASOP 8.43B. **Rules:** E10.3-.4. **PDF pages:** 681, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Has the required number of complete Game Turns been maintained?

- Check qualifying locations, compatible ammunition, uninterrupted TI and CE, and the B#-dependent duration. Moving, firing, losing CE or being attacked in CC can interrupt the process.
- On completion restore the allowed depleted ammunition and make the Ammo Vehicle’s exhaustion DR. Original 12 removes its supply role.
- Retain incomplete-task TI and separate any Ammo Dump/transfer completion. Do not count an interrupted partial Game Turn as complete.

**Checklist behavior:** Show completed turns, interruption cause and the next required outcome.

**Illustrative position:** An attack in CC can interrupt resupply even if it causes no casualties.

**Terms:** TI, CE, DR.

### 8.44A · Concealment gain

#### Evaluate eligible ATTACKER concealment gain

ID: `concealmentGain`. **Who:** ATTACKER. **Type:** Combat decision.

**Timing:** ASOP 8.44A. **Rules:** A12.12-.122; A12.2; A12.32. **PDF pages:** 681, 77, 78, 79. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Which units qualify now under the governing concealment table?

- Use the unit category, Good Order status, Concealment Terrain, enemy LOS and range; the Concealment Table controls over generic prose.
- Evaluate unbroken enemy observers, including the proper Dummy/vehicle distinctions. Apply the lawful disclosure rule if a hidden unit is used to deny concealment.
- Only the appropriate ATTACKER units gain concealment here. Do not grant new HIP or regain Cloaking as an ordinary end-turn benefit.

**Checklist behavior:** Show authorized reasons for automatic gain, a required dr, or refusal without secret-state leakage.

**Illustrative position:** An eligible unit outside enemy LOS can gain concealment while a broken unit cannot.

**Terms:** LOS, Good Order, HIP.

#### Make each required concealment dr

ID: `concealmentDr`. **Who:** ATTACKER. **Type:** Combat decision.

**Timing:** ASOP 8.44A. **Rules:** A12.122. **PDF pages:** 681, 77, 223, 280. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Does this unit need a roll instead of automatic gain?

- Use the two specified terrain/range/LOS cases. Roll separately for each qualifying unit, with success on Final dr ≤5.
- Apply US#, valid leadership, Lax/Stealthy, terrain/Hindrance and Japanese modifiers. A stack does not share one all-or-nothing result.
- Use the night automatic-gain exception when applicable rather than rolling unnecessarily.

**Checklist behavior:** Show per-unit outcomes and the source of each modifier.

**Illustrative position:** A squad can fail while its leader and Hero in the same stack qualify.

**Terms:** dr, drm, SMC.

#### Apply night, winter, desert and campaign concealment rules

ID: `concealmentProfiles`. **Who:** ATTACKER. **Type:** Combat decision.

**Timing:** ASOP 8.44A. **Rules:** E1.32; E3.712; F11.601; G1.63; ASOP 8.44A. **PDF pages:** 681, 223, 230, 266, 280, 298. **Coverage:** Rules integration pending; reference available.

**Consider:** Which special profile modifies ordinary concealment gain?

- Night makes daylight dr cases automatic; winter camouflage, desert visibility and Japanese modifiers require their own applicable conditions.
- Retain named ABtF CG12, BRT CG11, KGP CG11, PB CG10 and RB CG15 profiles from ASOP, with source gaps explicitly identified until those campaign rules are reconciled.
- A cave’s entry-based HIP permission is not an end-CCPh HIP gain. Do not promise every module’s concealment behavior from the generic table.

**Checklist behavior:** Keep profile options available while distinguishing verified rules from missing scenario evidence.

**Illustrative position:** A campaign concealment exception remains selectable for study even before its module source is integrated.

**Terms:** SSR, Cloaking, Source gap.

### 8.45B · Night cleanup after concealment

#### Remove Starshells and IR after concealment is assessed

ID: `illumination`. **Who:** BOTH. **Type:** Required cleanup.

**Timing:** ASOP 8.45B. **Rules:** E1.923; E1.933; ASOP 8.44A-.45B. **PDF pages:** 681, 225, 226, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Which illumination expires at this Player Turn’s end?

- Evaluate concealment gain first, with the illumination still present. Then remove all Starshells and IR at this window.
- Do not clear Blazes, Flames, every trip flare or active searchlights merely because they also illuminate. Their removal rules differ.

**Checklist behavior:** Show ordered expiry so darkness cannot grant premature concealment.

**Illustrative position:** A Starshell still affects the concealment decision before it is removed.

**Terms:** IR, LOS.

#### Remove night Acquisition that loses illumination

ID: `acquisition`. **Who:** BOTH. **Type:** Required cleanup.

**Timing:** ASOP 8.45B. **Rules:** E1.74. **PDF pages:** 681, 225. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the target retain the required Blaze/Flame illumination?

- At turn end retain Acquisition only under the specified Blaze/Flame illumination condition. Starshell/IR expiry can remove its supporting illumination.
- Respect the broader immediate-loss rule whenever illumination disappears earlier. Do not treat all Acquisition as expiring every turn or preserve it because the Gun is still present.

**Checklist behavior:** Link each Acquisition expiry to its target’s illumination state.

**Illustrative position:** A target illuminated only by IR loses Acquisition when that IR expires.

**Terms:** Acquisition, LOS.

### Supplemental events and turn handoff

#### Expire cave smoke and resolve connected WP disclosures

ID: `caveSmoke`. **Who:** BOTH. **Type:** Required expiry.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** G11.85-.851. **PDF pages:** 681, 300, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a half-inch SMOKE counter in a cave expire at this turn’s end?

- Half-inch SMOKE placed in a cave persists until the end of that Player Turn. Apply the specified expiry after concealment placement; do not clear every ordnance SMOKE counter by the same rule.
- Removing cave WP can immediately reveal higher connected hidden caves and above-ground tunnel openings under G11.851, regardless of LOS. Determine the exact Accessible/Complex relationships.
- Revealing a tunnel opening in a hidden pillbox does not reveal the pillbox itself. Keep the required disclosure narrower than a blanket reveal of all underground contents.

**Checklist behavior:** Attach connected revelation to the WP expiry event.

**Illustrative position:** A cave WP counter expires and reveals a higher tunnel opening while the pillbox containing it remains hidden.

**Terms:** WP, HIP, CCPh.

#### Remove a landing-craft Blaze’s temporary Pin marker

ID: `lcBlazePin`. **Who:** BOTH. **Type:** Required expiry.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** G12.68. **PDF pages:** 681, 307, 308. **Coverage:** Rules integration pending; reference available.

**Consider:** Which newly placed Blaze has reached its next-CCPh expiry?

- A Blaze placed on a LC initially receives a Pin marker. Remove that marker at the end of the next CCPh according to its placement record, without extinguishing the Blaze.
- Preserve duplicate Passenger-wreck bookkeeping. The unpinned Blaze then has its specified MP and later AFPh damage effects; those are not a fresh CCPh attack.

**Checklist behavior:** Distinguish a fire-timing marker from a pinned Personnel unit.

**Illustrative position:** Removing the marker activates the Blaze’s later effects rather than clearing the fire.

**Terms:** LC, CCPh.

#### Remove only the trip flares due now

ID: `tripFlareExpiry`. **Who:** BOTH. **Type:** Required cleanup.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** E1.951. **PDF pages:** 681, 227. **Coverage:** Rules integration pending; reference available.

**Consider:** Was this flare activated during MPh or during a later phase?

- Red-on-white MPh trip flares expire at the end of that Player Turn’s CCPh after concealment placement.
- Purple-on-white RtPh/APh/CCPh flares persist until the next Player Turn’s AFPh. Store activation provenance, not just a generic flare flag.

**Checklist behavior:** Use an expiry record tied to the activation phase.

**Illustrative position:** A flare triggered by this CCPh’s Withdrawal survives when the older MPh flare is removed.

**Terms:** Trip Flare, CCPh.

#### Resolve check-triggered effects without inventing Sniper activity

ID: `specialChecks`. **Who:** BOTH. **Type:** Combat decision.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** B28.9; A14.1; A17.11; A18.12. **PDF pages:** 681, 82, 85, 149. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a required check trigger a separate rule effect?

- Apply Booby Trap and casualty/wound consequences to qualifying checks under the scenario profile; preserve Original dice and proper Random Selection.
- Ordinary CCPh rolls are not among A14.1’s normal SAN phases. Do not run a generic Sniper trigger on every CC or task-check roll.
- Preserve Field Promotion and special Original-2/12 branches instead of hiding them behind the ordinary attack result.

**Checklist behavior:** Expose only the triggered branch supported by its rule.

**Illustrative position:** An escape NTC can need a Booby Trap check without creating an ordinary CCPh Sniper attack.

**Terms:** DR, Wound.

#### Finish obligations and hand over the Player Turn

ID: `close`. **Who:** BOTH. **Type:** Turn handoff.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** A3.8-.9; ASOP 8. **PDF pages:** 681, 47, 73, 143, 244, 291. **Coverage:** Partially integrated; full rule path remains to be verified.

**Consider:** Is every Location and phase-wide continuation settled?

- Require completion of all selected Locations, aerial choices, declared tasks, required status changes, concealment and night expiries before the turn changes.
- Carry Melee, HtH, Guard/crew identities, captured equipment, Labor, ongoing resupply/Pack-TI and future flare expiries into the next turn.
- Do not clear DM/CX or every fire/status marker through a generic CCPh cleanup. Use each marker’s rule and apply scenario victory timing separately.

**Checklist behavior:** Keep End Location, End ground combat and End Player Turn distinct in the live design.

**Illustrative position:** No ground fights remain, but incomplete resupply and a next-turn flare still require carried state.

**Terms:** CCPh, Declaration receipt, Melee.

#### Preserve all identified options and source boundaries

ID: `sourceProfile`. **Who:** BOTH. **Type:** Coverage boundary.

**Timing:** Rule-specific CCPh timing; supplemental to ASOP. **Rules:** ASOP 8; SSR; Chapters H/J/O/W. **PDF pages:** 681, 681. **Coverage:** Rules integration pending; reference available.

**Consider:** Which rules are verified, which await integration, and which need more source evidence?

- All 17 printed CCPh steps and identified supplemental families are represented. This is not certification of every Chapter H note, historical module, SSR or optional combination.
- Keep pending integration, source gaps, unknown applicability and known illegality separate. Missing evaluators must not hide reference choices.
- The future Blazor guide should consume viewer-safe Rules projections and durable decision receipts. This preview neither adjudicates combat nor implements engine passes.

**Checklist behavior:** Expose source and integration limitations without shrinking the intended completed system.

**Illustrative position:** A deferred sN attack remains visible with its prerequisites even when the current engine cannot execute it.

**Terms:** SSR, Source gap.

## Contextual vocabulary

| Label | Meaning |
|---|---|
| CCPh | Close Combat Phase. Resolve each selected Location completely, then resolve phase-wide aerial combat, Clearance and cleanup. |
| CC | Close Combat. Usually simultaneous attacks between opposing units in a Location, with explicit sequencing, vehicle and terrain exceptions. |
| Melee | Continuing combat status determined per eligible unit after the Location’s combat, with concealment, vehicle, transport, sewer and pillbox exceptions. |
| Declaration receipt | Proposed live record of attackers, defending groups, targets, attack mode, strength, modifiers and obligations at the commitment boundary. |
| Location | A hex can contain multiple Locations. Same hex does not necessarily mean a leader can help or a transfer is legal. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| FP | Firepower. |
| T-H Hero | Japanese Tank-Hunter Hero, created through G1.421 with a Designated Target and special compulsory advance and combat rules. |
| AFV | Armored Fighting Vehicle. |
| Ambush | An opening advantage that can change attack order, CC DRM, concealment and withdrawal. It is not rerolled merely because new units reinforce a Melee. |
| dr | One-die roll. Ambush and ATMM use dr, not DR. |
| drm | Modifier to a one-die roll, such as an Ambush or ATMM check. |
| CX | Counter Exhausted. Usually bars an already-CX unit from a difficult advance; climbing and deep-stream entry have explicit exceptions. A difficult advance creates CX. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| Stealthy | A11.17 quality affecting Ambush and other named checks, subject to Good Order, nationality and Lax exceptions. |
| Withdrawal | Leaving combat under a specific rule. Ordinary Melee Withdrawal, Ambush Withdrawal, Infiltration and Street Fighting return have different eligibility and timing. |
| Street Fighting | A11.8 vehicle combat advantage and return option based on eligible road/building or Bypass relationships, distinct from an actual Ambush. |
| Designated Target | The qualifying target chosen when a T-H Hero appears. It constrains the hero’s subsequent actions. |
| Sequential CC | Attacks resolved in order, with the next declaration allowed after earlier results. Eliminated units lose their pending attack opportunities. |
| Simultaneous CC | Both sides declare before resolution. A declared attack still resolves at its original strength despite simultaneous elimination, reduction or capture, subject to explicit interrupts. |
| Prisoners | Captured Personnel under a Guard. Transfer, abandonment, accompaniment and escape have distinct rule windows. |
| Guard | Unit assigned to prisoners. Capacity, fire restrictions and accompaniment are separate from the act of accepting surrender. |
| NTC | Normal Task Check, used only when the governing rule calls for it. |
| Personnel Escort | Qualified Personnel in the vehicle’s Location that modify its vulnerability, assessed at each attack rather than frozen for the whole phase. |
| CCV | Close Combat Value. Used for Personnel attacking vehicles and as the defending strength against vehicle CC fire. It is not the unit’s printed Infantry FP. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| Disrupted | A19.12 status with its own opening surrender and restricted rout rules. Ordinary daytime Low Crawl is unavailable; night and No Quarter require explicit treatment. |
| IPC | Inherent Portage Capacity. Evaluate retained load and eligible leader assistance before the difficult-advance test. |
| PP | Portage Points. A retained load leaving no MF bars advance even into otherwise inexpensive terrain. |
| Accessible | A rules-defined connection usable by hypothetical Infantry under normal advance conditions, ignoring enemy presence. Special cave and terrain restrictions still apply. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| DRM | Modifier to a two-dice roll. Keep target-specific CC modifiers separate from Ambush one-die modifiers. |
| CCT | Close Combat Table. Personnel odds use its appropriate black or Hand-to-Hand red Kill Numbers; personnel attacks on vehicles instead use CCV. |
| HtH | Hand-to-Hand CC. Uses red Kill Numbers when the applicable nationality, Deluxe or SSR rule permits or requires it. Not a universal choice. |
| Dare-Death | Chinese designated MMC with special HtH/non-HtH benefits and voluntary-berserk recovery rules. |
| Partial Kill | Final CC DR equal to the applicable Kill Number. Ordinary attacks use Random Selection for Casualty Reduction; capture has its own selection procedure. |
| No Quarter | A surrender policy under A20.3. It does not create a generic right to replace every CC capture with elimination. |
| AAMG | Anti-Aircraft Machine Gun. |
| CMG | Coaxial Machine Gun. |
| RMG | Rear Machine Gun. Use its eligible CC FP under A11.62; weapon allocation remains distinct from ordinary fire. |
| IFE | Infantry Firepower Equivalent, a weapon-specific alternative to ordinary TH fire. |
| sN | Nahverteidigungswaffe. A qualifying BU AFV’s separate close-defense HE attack on the IFT, with usage and timing restrictions and friendly-unit exposure. |
| IFT | Infantry Fire Table. |
| Pillbox | Separate Location equivalent to a building for rout, with special Known-enemy, entry/exit, ordinary forced-rout and Blaze rules. |
| Wire | Obstacle with distinct above/below states. Infantry cannot move beneath it during APh. |
| Panji | Covered-hexside obstacle. Normal advance avoids its entry MC; difficult advance can trigger it. APh does not permit the extra-MF move beneath it. |
| Skis | E4 equipment whose mode changes and downhill bonus have explicit RtPh permission. Low Crawl requires removing skis. |
| DC | Demolition Charge. |
| ATMM | Anti-Tank Magnetic Mine. A pre-resolution availability check after declaring the attack, with nationality/date and failure rules; it is not ordinary fire. |
| Hara-Kiri | G1.641 Japanese Personnel self-elimination attempt immediately before a CC Capture attempt resolves, with special eligibility, order and failure consequences. |
| DR | Two-dice roll. Preserve Original and Final values and the colored die for special results. |
| Wound | A17 injury with severity, reduced morale/leadership, IPC and movement consequences. Track MF already spent if it occurs mid-rout. |
| Infiltration | Immediate withdrawal opportunity from an Original CC DR of 2 or 12, with eligibility restrictions. It cannot be saved until after another attack. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| FFE | Fire For Effect. Existing HE/WP FFE can attack the routing subject on qualifying entry or increased vulnerability; it does not itself Interdict. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| Trip Flare | E1.95 hidden device with entry/expenditure triggers. A flare set off in RtPh persists until the next Player Turn’s AFPh cleanup. |
| Column | Formation using multi-hex Impulse movement/advance at its slowest unit’s rate, with restrictions and immediate Disbandment checks. |
| Good Order | The defined unit status, separate from merely unbroken. Apply the exact status requirement of each action. |
| Interrogation | E2 optional-rule procedure, when in use, immediately after capture or surrender. One roll per prisoner, with controlled disclosure. |
| Dogfight | Aerial-combat group with predesignated initial targets, ATTACKER-first sequential attacks, special ROF, damage and Recall. |
| FB | Fighter-Bomber, an aircraft type with its own sighting and Ground Support procedures. |
| Aerial Melee | Continuing Dogfight held by a qualifying undamaged FB with functioning MG. Ordinary Infantry Withdrawal rules do not apply. |
| ROF | Rate of Fire. A retained opportunity is not an automatic shot. |
| Clearance | Resolution of a previously declared task. Non-Flame tasks finish in CCPh under their own eligibility and result rules; this is not a fresh declaration window. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| Labor | Task-specific Clearance benefit earned by failure, normally progressing from -1 to -2. It does not transfer freely to another task. |
| TB | Trail Break. Creation and use depend on terrain and the applicable vehicle procedure. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| Tunnel | B8.62 special rout connection between entrances, with emergence conditions and immunity to Interdiction while inside. |
| Pack-TI | Animal-Pack task status. It survives ordinary end-CCPh TI removal and completes at its specified later Rally window. |
| CE | Crew Exposed. A vehicle crew posture whose voluntary changes have timing restrictions. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| Cloaking | Night concealment representation whose contents normally advance as one stack until properly separated; hidden contents remain owner-only. |
| Source gap | Missing or unreconciled scenario/module/equipment evidence, distinct from a rule awaiting implementation. |
| IR | Illuminating Round, with its own usage and placement procedure. |
| Acquisition | A weapon-specific targeting benefit. Tracking a moving target is not permission to fire in APh. |
| LC | Landing Craft, governed by its own transport, fire, beaching and grounding rules. |

## Acceptance scenarios for the eventual live component

These are design requirements, not claims that the present engine passes them. Each scenario must preserve the player's information permissions and distinguish consideration from a committed game action.

| Situation | Required behavior |
|---|---|
| Two Locations have unfinished combat | Complete the selected Location before opening the next; keep phase-wide obligations separate. |
| Simultaneous attacks | Commit both sides' declarations before exposing results; retain the declared attacks despite simultaneous casualties or capture. |
| Sequential attacks | Resolve the previous attack and immediate consequences before offering the next eligible attack. |
| Prisoners, Ambush and vehicles coexist | Explain the applicable priority and correct next decision owner. |
| Night Ambush | Apply the correct threshold and illumination exception, without treating every night attack identically. |
| Concealed combatants | Distinguish strength disclosure from concealment loss and participation from declining an attack. |
| SMC defending groups | Record solo or paired status at the proper declaration window. |
| Broken non-Guard unit in Melee | Offer compulsory Withdrawal when required, including the inability-to-withdraw consequence. |
| Equipment exceeds Withdrawal capacity | Require the legal retained load and dropped items before departure. |
| Infantry above Panji | Apply the special Withdrawal restriction rather than the ordinary destination chooser. |
| Apparently empty Withdrawal destination | Do not reveal hidden enemy information in advance; resolve the actual arrival consequence. |
| Original CC DR 2 or 12 | Offer eligible Infiltration immediately; do not bank it for later attacks. |
| German ATMM check fails with Original 6 | Apply the proper Pin and attack consequences, including the berserk exception to ordinary expectations. |
| Tank-Hunter Hero ATMM check | Use its own availability modifiers and failure rules, without copying German failure Pin. |
| Japanese defending Tank-Hunter Hero creation | Defer the attempt until any ATTACKER Ambush attacks have finished. |
| Japanese target of Capture | Offer Hara-Kiri at its exact pre-resolution window with eligibility and order enforced. |
| Equal-to-Kill-Number Capture result | Use the capture selection procedure, not ordinary Partial Kill Random Selection. |
| Captor is eliminated simultaneously | Settle capture validity before assigning final prisoners and guards. |
| Original 2 against a vehicle | Offer the optional Unlikely Kill procedure without worsening an already achieved result. |
| Field Promotion during CC | Create the eligible leader and apply the rule-specific recalculation before proceeding. |
| Eliminated possessor has equipment | Check each affected SW/Gun using the specified die and black Kill Number. |
| Eligible AFV uses sN | Enforce usage and timing and include exposed friendly unarmored units. |
| Vehicle or PRC casualties | Apply each PRC relationship and its CC-specific fate, without inventing a normal survival roll. |
| Street Fighting return | Preserve the origin and apply the return, remaining and hazard rules. |
| Location closes with mixed survivors | Determine Melee for each eligible unit; a vehicle is not held merely because it holds Infantry. |
| Tank-Hunter Hero attacks its Designated Target | Apply immediate removal at that attack's completion; do not delay it to generic cleanup. |
| Dogfight starts | Commit initial targets before the sequential attacks and track ROF, damage and Recall separately. |
| Clearance is unresolved | Resolve the previously declared task before ordinary Pin/TI removal; retain task-specific Labor and partial work. |
| Pack-TI or unfinished ammunition replenishment | Preserve the status that survives ordinary end-phase cleanup. |
| Night concealment gain | Evaluate concealment while current illumination still exists, then remove expiring illumination and affected Acquisition. |
| Trip Flare expires | Use its activation phase to select end-CCPh or next-AFPh expiry. |
| Cave WP expires | Apply the end-turn connected-feature disclosure after concealment gain, with the hidden-pillbox exception. |
| LC Blaze Pin expires | Remove the specified Pin at its correct CCPh without extinguishing the Blaze or losing later damage obligations. |
| Required rule is not integrated | Keep the option and references visible; report pending execution instead of silently treating it as unavailable. |
| Scenario/module source is absent | Report the source gap distinctly from implementation status; do not claim that the profile has been reconciled. |

## Preview verification

The final browser check exercised all 538 topics across the eight phase catalogs, including every contextual definition interaction. It verified the 17 CCPh ledger mappings, timing selection, independent per-phase consideration state, host-state restoration, 320px and 736px layouts, and absence of browser runtime errors. Light and dark screenshots were inspected. These checks validate the reference preview, not combat adjudication or the live-component acceptance scenarios above.
