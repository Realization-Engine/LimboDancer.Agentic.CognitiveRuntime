# ASL Advancing Fire Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design content and interactive reference preview. No Blazor component or Rules behavior is implemented by this work.

Design target: the completed ASL.Rules implementation after all passes. Pending and deferred rules remain represented. See the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md) and [Advancing Fire catalog](ASL%20Advancing%20Fire%20Phase%20Checklist%20Catalog.json).

## Sources and reconciliation scope

Authority: supplied eASLRB v3.01 at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. All nine printed AFPh steps are on physical PDF page 680; the shared dagger note is on 678. The printed ASOP layout was visually checked. All page references are one-based physical PDF pages, not printed chapter folios.

The reconciliation follows every ASOP clause and its linked procedures, including A4.41, A7.24-.25/.36, A15.46, A23, A24.61-.62, B25.15/.6, C5, C10.21 and E8.4. Whole-volume AFPh/Advancing Fire Phase discovery found 227 occurrences across 99 pages, including repeated tables and training examples. Those counts are search hits, not distinct audited actions. Supplemental checks include sewer/climbing/fording fire, DC Breaches, D3.32 carryover, E7.5 end-phase AA mode, E9.5 paratroops, G12.68 LC damage, German Vehicle Note 93 Goliath and W10.31/.52 searchlights.

This covers the printed ASOP and identified in-volume rule families. It does not claim an exhaustive inventory of every SSR, module or Chapter H permutation. Named campaign Sniper exceptions RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 remain source gaps. Source completeness, legal applicability and integration status are separate facts.

## What the player sees

The phase opens with “Review the fire that remains after Movement.” A timing selector follows the nine printed steps, with supplemental choices clearly labeled. Each topic shows who decides, the window, rule and PDF references, current integration status, a question, concrete checks, consequences, an illustrative position and clickable contextual terms. “Mark topic considered” records review only. It neither fires a weapon nor resolves a required result.

The central question is “What remains, and how does this particular attack work?” AFPh does not give a fresh shot to every counter, and staying stationary does not ordinarily avoid its FP penalty. Distinguish unit, weapon, vehicle, controller and explosive histories.

| Situation | What the checklist explains |
|---|---|
| Ordinary stationary or moved Infantry | Eligible ordinary Small Arms fire is halved for AFPh. Staying still is not Opportunity Fire. |
| Qualifying Assault Fire squad | Apply all inherent-FP factors, add one, then round up per squad. Pin/Spraying do not remove the bonus; Long Range/Opportunity Fire do. |
| Earlier Opportunity Fire | Preserve its PFPh commitment, full AFPh FP treatment and eligible ROF/IF. Apply other restrictions normally. |
| Moved HMG/mortar/Gun | Test the weapon’s movement, not merely its owner’s. Show Recovery and specific equipment exceptions. |
| Ordinary ordnance | Apply the proper TH cases rather than halving effect FP merely for AFPh. |
| Vehicle retained ROF in MPh | D3.32 can permit exactly one carryover shot if no other weapon/PRC fired. No continuing AFPh ROF. |
| Operably Placed DC | Required detonation survives later loss of its placer’s capability. |
| FT/MOL/DC | Preserve their specific exceptions, Motion effects and Thrown DC modifiers. |
| New or Hampered Flame | No same-turn conversion; old eligible Flames have Blaze, extinguished or unchanged outcomes. |
| Missing evaluator | Reference remains available; show “Availability not evaluated,” not “Illegal.” |

### Five design improvements

1. **Commitment explanation:** show the event that spent or preserved each capability. A Bounding Fire marker has different meanings for Opportunity Fire and vehicle fire.
2. **Per-unit arithmetic:** show the FP contribution of each squad and SW before combining a FG. For two 5-4-8 squads at an ADJACENT concealed target, each contributes `5 × 2 ÷ 2 ÷ 2 + 1 = 3.5`, rounded to 4. The FG receives 8 FP from them. Do not round the combined total instead.
3. **Closing-window review:** warn before non-SMOKE fire closes WP and before cleanup closes eligible CA/AA choices. Place required WP effects before Gusts even if Gusts will remove the new counter.
4. **Independent consequence records:** attach a Placed DC to its placement event, a drift to its source and a spread attempt to its eligible target. None depends on a player keeping an optional firing checkbox open.
5. **Rout handoff:** new terrain Blaze creates an escape obligation with a deadline, not immediate generic Infantry elimination. Optional firing can be finished while that future obligation remains visible.

### Worked phase

The attacker reviews an unmoved 4-FP squad, two Assault Fire squads, a moved HMG, an Opportunity-Fire Gun, a vehicle that retained MA ROF in MPh, a Placed DC and several Flames. Missing Mild Breeze drift is placed first. An eligible WP choice comes before non-SMOKE fire; its morale and possible Flame effects resolve before Gusts handling. Surviving ordinary glider contents appear after Damage resolution, while a just-landed paratroop unit receives an explicit no-attack explanation.

The ordinary squad normally contributes 2 FP. The Assault Fire squads receive individual calculations. The moved HMG is checked against A4.41 rather than its owner’s general eligibility. The Opportunity-Fire Gun can use its preserved benefit and eligible ROF/IF. The vehicle gets only its specifically qualified D3.32 carryover shot, not a fresh suite of vehicle attacks. An operably Placed DC still resolves even if its carrier broke during DFPh. Any qualifying berserk local TPBF success triggers the return check.

At the end, old Blazes spread using the appropriate target/source and weather rules; newly created Flames cannot immediately convert. Old unhampered Flames test for conversion or extinguishing. Eligible CA/AA choices are decided using pre-cleanup capability. Specified fire counters are removed, with night and switched-on searchlight exceptions. Newly endangered Infantry remain linked to their next-Rout escape obligations.

### Source distinctions the UI must explain

- The ASOP 5.22A shorthand lists DC, non-Motion FT and Opportunity Fire as FP exceptions. A7.24 explicitly includes MOL. The catalog preserves that additional rulebook exception.
- The ASOP marker shorthand is not a complete capability test. A7.25 uses a Bounding Fire counter for Opportunity Fire; D3.32 separately preserves a qualifying vehicle shot.
- Ordinary AFPh is not a new OBA, Ground Support, Starshell/IR, Smoke-ammunition, SW assembly/destruction or movement window.
- A climbing unit’s special fire window, discovered sewer targets, fording restrictions and a cavalry Charge’s later prohibition need explicit profiles.
- B25 fire spread and G12.68 landing-craft damage use different eligibility rules. Do not suppress LC damage with an ordinary first-turn Blaze-spread test.
- W10.31 preserves switched-on searchlight Gunflash despite the ordinary night cleanup in ASOP 5.33B.

## Integration evidence

Labels summarize [ASL Rule Coverage](../ASL%20Rule%20Coverage.md), including its pass amendments, not new engine validation. Existing foundations include AFPh FP, Assault Fire, Opportunity Fire, ordnance, vehicle attacks and result/Sniper chains. Coverage records incomplete A4.41 moved-weapon restrictions, pending squad Spraying Fire and upper-level Encirclement, no A15.46 AFPh return, mostly missing B25 fire spread, and deferred specialty profiles. Earlier C5 Opportunity-Fire penalty findings were repaired in pass 35; the design does not repeat the old defect as current behavior. Later D3 pass amendments supersede the initial vehicle limitations. No single “partially integrated” badge establishes a complete Rules path.

The reference preview can expose the complete public catalog, but live choices and counts must be projected for the viewer. Hidden enemy state, private controllers and unknown equipment cannot be disclosed through availability explanations. All new options remain represented even while their evaluator is pending.

The catalog contains 58 topics and 91 contextual vocabulary labels.

## Printed ASOP coverage ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 5.11B | Mild Breeze drift from Blaze/white SMOKE lacking it; cave exception. | `drift` |
| 5.12A | Ordnance Dispersed WP; NMC and possible Flame. | `wp`, `wpEffects` |
| 5.13A | Gusts removal then dispersal; cave exceptions. | `gustSmoke` |
| 5.21A | Glider contents; vehicle/Gun and PRC/crew exceptions; Damage before placement. | `glider` |
| 5.22A | Eligible unbroken ground/sewer fire; markers/TI; FP exceptions and Assault Fire; Cases B/C and Opportunity exception; (un)limber/TI; Placed DC resolution. | `smallArms`, `assault`, `opportunity`, `movedWeapons`, `weapons`, `spraying`, `ft`, `mol`, `encirclement`, `ordnance`, `ammo`, `latw`, `mortar`, `rof`, `intensive`, `vehicle`, `carryover`, `passengers`, `sewer`, `placed`, `thrown`, `set`, `limber`, `unlimber`, `mgArmor`, `acquisition`, `targets` |
| 5.23A | Qualifying berserk local halved-TPBF elimination and Good Order return. | `berserk` |
| 5.31B | Blaze spread after initial Player Turn, including weather/Gusts. | `blaze`, `windSpread` |
| 5.32B | Unpinned Flame conversion, extinguishing and new/Hampered exclusions. | `flame`, `blazeExit` |
| 5.33B | Specified fire-counter removal; night First/Final/Gunflash with explicit exceptions. | `cleanup` |

Supplemental entries keep actual rule timing. Opening prisoner results, triggered effects and end-phase CA/AA choices receive no invented ASOP step number.

## Cataloged options

### Carry forward commitments

#### Review the fire that remains after Movement

ID: `handoff`. **Who:** ATTACKER. **Type:** Phase handoff.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A3.5; A4.41; A7.24-.25; D3.32. **PDF pages:** 680, 47, 50, 55, 199, 200. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which unit, weapon and controller still has an opportunity?

- Keep PFPh commitments, weapon movement, MPh fire, DFPh AA use, Pin, TI and operator status separate. Ordinary AFPh fire does not require that the unit moved.
- Distinguish a Bounding Fire counter recording Opportunity Fire from one recording spent vehicle fire. Evaluate D3.32’s retained-ROF carryover separately.
- Do not reset broken, TI or spent subjects on phase entry. An operably Placed DC has a due attack independent of its carrier’s current condition.

**Checklist behavior:** Show prior action, remaining capability and reason together. Missing evaluation is not illegal.

**Illustrative position:** An unmoved squad, moved HMG, Opportunity-Fire Gun and Placed DC need four different evaluations.

**Terms:** AFPh, Opportunity Fire, Bounding Fire, TI.

#### Choose fire, preparation, or no optional action

ID: `hold`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A7.1; A7.32; C10.21. **PDF pages:** 680, 54, 56, 169, 180. **Coverage:** Rules integration pending; reference available.

**Consider:** What is worth doing before Rout and Advance?

- Review newly exposed targets and SW before committing. Compare a shot with eligible equipment or end-phase CA choices.
- Holding is valid. Declining optional fire does not resolve DC, morale, fire spread or other required results. A new Opportunity Fire declaration is unavailable.

**Checklist behavior:** Keep consideration, pass, proposed action and resolved obligation distinct.

**Illustrative position:** A crew holds its shot to prepare its facing while a due DC attack remains open.

**Terms:** AFPh, LOS, CA.

### 5.11B · Drifting SMOKE

#### Create missing drift from eligible sources

ID: `drift`. **Who:** BOTH. **Type:** Required map update.

**Timing:** ASOP 5.11B. **Rules:** A24.61; G11.851. **PDF pages:** 680, 92, 93, 300, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Which original sources lack required Mild Breeze drift?

- Place missing gray Dispersed drift from eligible white SMOKE and Blaze sources. Length equals the original source’s Hindrance DRM, including white Dispersed sources.
- Apply source level and distinct Smoke/WP heights. No drift to/from non-rooftop Interior Building Locations or caves; Infantry grenades do not survive to this window.
- Link gray counters to their source. Removal or wind change removes dependent drift; source dispersal shortens it. Do not duplicate existing valid drift.

**Checklist behavior:** Present a source-linked placement queue for both sides.

**Illustrative position:** A white Dispersed WP source still produces its own shorter drift.

**Terms:** Drifting SMOKE, SMOKE, WP, LOS.

### 5.12A · Opening Dispersed WP

#### Consider ordnance Dispersed WP first

ID: `wp`. **Who:** ATTACKER. **Type:** Expiring opening choice.

**Timing:** ASOP 5.12A. **Rules:** C8.6; C8.9; A24.5. **PDF pages:** 680, 178, 179. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is WP worth this weapon’s remaining shot?

- Check ordnance, ammunition, operator, movement and prior-use eligibility. AFPh placement is Dispersed WP, not ordinary Smoke ammunition.
- Fire before any friendly non-SMOKE fire. Apply AFPh TH treatment; ordinary firers gain no ROF continuation from a low colored die.
- Resolve depletion and failed availability under C8.9; an unavailable special round is not automatically an actual shot.

**Checklist behavior:** Show the side-wide closing window and weapon commitment together.

**Illustrative position:** An eligible mortar considers WP before another unit fires HE.

**Terms:** WP, TH, Case B, Opportunity Fire.

#### Resolve WP morale and possible Flame

ID: `wpEffects`. **Who:** BOTH. **Type:** Required result chain.

**Timing:** ASOP 5.12A. **Rules:** A24.31-.32; C3.76. **PDF pages:** 680, 91, 92, 143, 171. **Coverage:** Rules integration pending; reference available.

**Consider:** What effects follow placement?

- Apply WP-specific morale eligibility and protection, including friendly occupants when applicable, and the WP critical-hit procedure.
- Check the prescribed Flame outcome and mark a new Flame ineligible for same-turn conversion. Finish effects before proceeding to Gusts.

**Checklist behavior:** Attach required results to the placement.

**Illustrative position:** WP causes its NMC even if the following Gusts step removes the counter.

**Terms:** WP, NMC, Flame.

### 5.13A · Gusts and SMOKE

#### Apply Gusts after the WP opening

ID: `gustSmoke`. **Who:** BOTH. **Type:** Required map update.

**Timing:** ASOP 5.13A. **Rules:** A24.62; B25.651; G11.8. **PDF pages:** 680, 93, 145, 300, 301. **Coverage:** Rules integration pending; reference available.

**Consider:** Which counters disappear or disperse now?

- Remove Dispersed SMOKE first, then flip remaining eligible SMOKE to Dispersed. Perform this AFPh treatment even though RPh Gusts effects already occurred.
- Apply cave exceptions and the protected smoke emanating from a Blaze in its own hex. Update source-dependent drift.
- New Dispersed WP may disappear here after its effects. Later Gusts fire spread is a separate obligation.

**Checklist behavior:** Show the ordered map changes rather than a general weather checkbox.

**Illustrative position:** Opening WP resolves, then Gusts remove its Dispersed counter.

**Terms:** Gusts, SMOKE, Drifting SMOKE.

### 5.21A · Glider contents

#### Resolve Damage and place glider contents

ID: `glider`. **Who:** ATTACKER. **Type:** Required placement and results.

**Timing:** ASOP 5.21A. **Rules:** E8.4-.41. **PDF pages:** 680, 241, 242. **Coverage:** Rules integration pending; reference available.

**Consider:** Which contents become onboard units now?

- Resolve accumulated Damage consequences before ordinary contents placement, including prescribed Casualty Reduction, NMC and equipment damage.
- Keep vehicles/Guns and their PRC/manning Infantry aboard until a subsequent MPh under the unloading rules.
- Eligible placed survivors may fire in AFPh, but not Opportunity Fire. Newly landed paratroops do not share this permission.

**Checklist behavior:** Link Damage, placement and eligibility as separate stages.

**Illustrative position:** A damaged glider’s squad may be reduced or broken before its firing is evaluated.

**Terms:** AFPh, SW, NMC, Good Order.

### 5.22A · Infantry and weapon choices

#### Calculate ordinary AFPh firepower

ID: `smallArms`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.21-.24; A7.8-.9. **PDF pages:** 680, 55, 56, 57, 58. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What is this attack’s actual FP?

- Halve ordinary Small Arms/MG/ATR/IFE even if the firer did not move. Apply range, concealment, Pin and PBF/TPBF independently.
- Check same-Location target-selection limits and Melee restrictions. Apply leadership, TEM, Hindrances, CX and Cowering; FFNAM/FFMO do not apply.
- Do not round fractional contributions early. Ordnance, FT, MOL and DC use their distinct treatment.

**Checklist behavior:** Explain each factor before selecting the IFT column.

**Illustrative position:** A stationary 4-FP squad ordinarily has 2 FP before other modifiers.

**Terms:** FP, IFT, PBF, TPBF, Area Fire.

#### Apply Assault Fire per qualifying squad

ID: `assault`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.36. **PDF pages:** 680, 56. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does each squad qualify for the bonus?

- Use inherent FP of a squad with underscored FP. Apply all modifiers, add one, then round fractions up for that squad.
- No bonus for Opportunity Fire or Long Range Fire. Pin and Spraying Fire do not by themselves remove it.
- Calculate each squad separately before summing the FG. The bonus does not apply to its SW.

**Checklist behavior:** Show the per-squad arithmetic and reasons for any exclusion.

**Illustrative position:** Each 5-4-8 at an ADJACENT concealed target contributes 5 × 2 ÷ 2 ÷ 2 + 1 = 3.5, rounded to 4; two contribute 8.

**Terms:** Assault Fire, FP, FRU, Area Fire.

#### Exercise the earlier Opportunity Fire commitment

ID: `opportunity`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.25; C5.2; C5.34. **PDF pages:** 680, 55, 172. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Has this subject preserved its PFPh commitment?

- Require the earlier declaration and valid subsequent history. It must still be unbroken, and unpinned for FT/DC/MOL.
- Avoid AFPh FP halving and the applicable Case B/LATW AFPh penalty, retaining other modifiers. Do not add Assault Fire.
- Only Infantry-fired ordnance can Opportunity Fire. Its CA cannot change until resolving its AFPh TH attempt; apply Case A then. Flip the marker when fire is exhausted.

**Checklist behavior:** Link the earlier commitment to its benefit and any lost eligibility.

**Illustrative position:** A Gun waits for a defender to reveal itself, then changes CA as part of its actual attack.

**Terms:** Opportunity Fire, ROF, IF, CA.

#### Check the weapon’s own movement history

ID: `movedWeapons`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A4.41; A9.8; C12.22. **PDF pages:** 680, 50, 65, 183. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Was the weapon moved, merely Recovered, or excepted?

- Moved MMG, HMG, mortar, INF/RCL SW and non-vehicular 5/8-inch ordnance normally cannot fire in the ensuing AFPh.
- A stationary weapon Recovered by a moving owner may fire with normal AFPh penalties. German dm MMG/HMG firing as LMG is an explicit exception.
- The U.S. 57mm RCL has its own moved-fire permission and Case C3 treatment. Do not exempt every RCL.

**Checklist behavior:** Show weapon movement separately from operator movement.

**Illustrative position:** Moving to a stationary mortar and Recovering it differs from carrying it into the hex.

**Terms:** SW, MMG, HMG, RCL, MPh.

#### Allocate inherent FP, SW and leadership

ID: `weapons`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.35; A7.4-.55; A9.4. **PDF pages:** 680, 56, 57, 63. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which attacks share this unit’s capability?

- A squad using one SW/Gun can retain inherent FP; using two normally loses it. Crew/HS/SMC weapon use has its own inherent-FP cost.
- Check possession, qualification, captured use, Pin and direction. Keep weapon capability distinct from the unit’s marker.
- Validate Mandatory FG, Fire Direction and multi-Location FG legality. Preserve leadership history for later eligible shots.

**Checklist behavior:** Preview capability consumed by each proposed attack.

**Illustrative position:** A squad using two SW cannot also contribute inherent Assault Fire.

**Terms:** SW, FG, MMC, SMC, ROF.

#### Consider squad or MG Spraying Fire

ID: `spraying`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.34; A9.5. **PDF pages:** 680, 56, 64. **Coverage:** Rules integration pending; reference available.

**Consider:** Would a legal two-Location attack help?

- Validate squad/MG capability, geometry, LOS and range, including the squad three-hex limit.
- Apply Spraying and AFPh reductions, then qualifying Assault Fire per squad. Long Range removes the bonus.
- Resolve each target with its own protection and effects.

**Checklist behavior:** Present both target contributions and result chains.

**Illustrative position:** Two targets may receive different results from one Spraying Fire attack.

**Terms:** Spraying Fire, Assault Fire, Area Fire.

#### Fire an eligible FT

ID: `ft`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A22.1-.4; D2.42; D3.6; B25.12. **PDF pages:** 680, 88, 89, 143, 144, 198, 201. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this FT remain usable?

- FT is not halved simply for AFPh. Motion/non-Stopped conveyance can impose a separate reduction; Pin and other FT restrictions remain.
- Apply range, protection, breakdown and Heavy Winds restrictions. Resolve possible Flame after the attack.

**Checklist behavior:** Identify the actual reason for each FP modifier.

**Illustrative position:** A moved Infantry FT and an FT on a Motion vehicle can have different FP.

**Terms:** FT, Motion, Flame.

#### Consider a qualifying MOL attack

ID: `mol`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A22.6-.613; A7.24. **PDF pages:** 680, 55, 89, 90. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is MOL available against this target?

- Check nationality/scenario availability, unit, target and range, then the MOL Check.
- The MOL bonus is not halved for AFPh; accompanying inherent FP follows its own rules.
- Apply SW-use, attack cancellation and firer/target Flame outcomes. A MOL Projector is a different weapon and timing.

**Checklist behavior:** Keep inherent FP and the MOL contribution separate.

**Illustrative position:** Failing the MOL Check does not create the successful MOL bonus.

**Terms:** MOL, SW, IFT, Flame.

#### Check Encirclement from actual attacks

ID: `encirclement`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.7-.72. **PDF pages:** 680, 57. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Have qualifying attacks established Encirclement?

- Check actual attack strength, geometry and Location/upper-level conditions in this phase.
- Considered shots do not count. Apply resulting morale, fire and movement consequences to the appropriate subjects.

**Checklist behavior:** Show the actual attacks supporting the condition.

**Illustrative position:** The second qualifying attack can change the target’s status before further fire.

**Terms:** Encirclement, LOS, FP.

### 5.22A · Ordnance and vehicles

#### Choose target type and resolve ordnance

ID: `ordnance`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C.4; C3.3-.7; C5.2-.4; C6. **PDF pages:** 680, 162, 169, 170, 171, 172, 174. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which target type and modifiers govern?

- Use AFPh TH modifiers, not automatic AFPh halving of ordnance FP. Target-type effects remain separate.
- Case B normally applies without entry into the current hex/hexside: +2, or +3 in woods/building/rubble. Opportunity Fire is excepted.
- Check LOS, CA, facing, acquisition and protection, then critical/improbable hit and collateral procedures.

**Checklist behavior:** Separate the TH calculation from the effect calculation.

**Illustrative position:** A stationary Gun’s HE effect is not halved again merely because it fired in AFPh.

**Terms:** TH, TK, Case B, Case C, TEM.

#### Choose legal special ammunition or Canister

ID: `ammo`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C8.1-.9. **PDF pages:** 680, 178, 179. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which listed round is useful and available?

- Check equipment/date/depletion and declare special ammunition before the roll. Ordinary Smoke and IR are not AFPh ammunition options.
- Canister uses its IFT footprint and specific Area Fire reductions, including applicable Bounding/Motion and concealed-target treatment.
- Keep depletion, malfunction and failed availability distinct; ammunition choice does not grant ROF.

**Checklist behavior:** Preview the ammunition-specific procedure and cost.

**Illustrative position:** A vehicle compares Canister with HE using their different effect paths.

**Terms:** HEAT, APCR, APDS, WP, IFE.

#### Check LATW fire and backblast

ID: `latw`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C5.34; C13.1-.9. **PDF pages:** 680, 172, 183, 184, 185, 186. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this operator fire from this Location?

- Apply ordinary AFPh Case C3 +2 unless Opportunity Fire, plus any separately applicable building/rubble backblast condition.
- Check weapon-specific availability, target, range, use and leadership. Preserve allowed Desperation choices and consequences.
- ATR non-ordnance FP and ordnance TH/TK use different procedures; do not apply both to one attack.

**Checklist behavior:** Show target effect and backblast exposure separately.

**Illustrative position:** An eligible PF attack can target Personnel as well as armor under its weapon rules.

**Terms:** LATW, PF, PSK, ATR, Case C3.

#### Use the existing mortar Spotter relationship

ID: `mortar`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C9.3-.31; A4.41. **PDF pages:** 680, 50, 179. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this mortar have a legal sighting path?

- Carry forward valid designation. No new original Spotter is designated in AFPh; replacement has its own timing after loss.
- Apply Spotter LOS/Hindrances, +2 spotted-fire TH, ROF reduction and Pin effects.
- An Opportunity-Fire mortar lacking both a usable Spotter and a target in its own LOS still makes the C9.3/A6.11 breakdown roll.

**Checklist behavior:** Show lost spotting and any surviving mandatory check.

**Illustrative position:** A broken Spotter can prevent the promised attack without erasing the breakdown obligation.

**Terms:** MTR, ROF, LOS, Opportunity Fire.

#### Continue eligible Opportunity Fire ROF

ID: `rof`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.25; A9.2; C2.24; C5.2. **PDF pages:** 680, 55, 63, 168, 172, 179, 200. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this shot retain another legal attack?

- Ordinary AFPh fire is one shot regardless of the colored die. Eligible Opportunity Fire can retain Multiple ROF.
- Apply Pin, target type, spotted fire, IFE, captured use and other reductions/exclusions.
- D3.32 grants one carryover shot, not an AFPh vehicle ROF sequence.

**Checklist behavior:** Evaluate continuation only after the preceding results.

**Illustrative position:** An ordinary mortar’s low colored die does not authorize a second AFPh shot.

**Terms:** ROF, Opportunity Fire, TH.

#### Consider IF for an eligible Opportunity Firer

ID: `intensive`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.25; C5.6-.62. **PDF pages:** 680, 55, 172, 173. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this Gun Intensive Fire after exhausting ROF?

- AFPh IF requires Opportunity Fire and ordinary Gun/operator eligibility. SW and prohibited/pinned/shocked/stunned/spent subjects do not gain IF.
- Apply Case F +2, reduced breakdown threshold and prescribed malfunction/destruction, then the Intensive Fire marker.
- This is not a generic second ordinary AFPh or vehicle carryover shot.

**Checklist behavior:** Show the accuracy and breakdown tradeoff.

**Illustrative position:** An eligible Infantry-manned Opportunity-Fire Gun can consider IF after normal ROF ends.

**Terms:** IF, ROF, Opportunity Fire, TH.

#### Choose vehicle Bounding Fire

ID: `vehicle`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** D3.1-.33; C5.3-.35. **PDF pages:** 680, 198, 199, 200, 201, 212. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What can this vehicle and its weapons fire?

- After entering a new hex/hexside use Bounding Fire and Case C for ordnance, with applicable Motion Case C4. MPh Cases C1/C2 do not apply here.
- Use AFPh halving for ordinary MG/IFE and additional Motion reduction as applicable, retaining FT/gyrostabilizer exceptions.
- Check prior use per weapon and PRC, VCA/TCA, CE/BU, stun/shock, Armor Leader and equipment restrictions.

**Checklist behavior:** Show separate capability lines with a shared vehicle history.

**Illustrative position:** A vehicle that waited until AFPh has different choices from one that fired during MPh.

**Terms:** Bounding Fire, Case C, Case C4, MA, Motion.

#### Use the D3.32 retained-ROF carryover shot

ID: `carryover`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** D3.31-.32; C5.3. **PDF pages:** 680, 200. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Did MPh preserve exactly this weapon’s shot?

- Require unexhausted Multiple ROF in MPh and no other weapon or PRC firing during MPh.
- Only that weapon may fire once in AFPh: Case C or halved MG/IFE when not in Motion; Case C4 or quartered FP in Motion.
- This is not Bounding Final Fire or continuing AFPh ROF. Apply the explicit exception in marker eligibility.

**Checklist behavior:** Link permission to the retained MPh event.

**Illustrative position:** A vehicle that fired only its MA and retained ROF may have one carryover shot.

**Terms:** Bounding First Fire, ROF, Case C, Case C4.

#### Evaluate mounted and transport-specific fire

ID: `passengers`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** D6.6-.72; D15.6; G12.5. **PDF pages:** 680, 206, 207, 215, 304. **Coverage:** Rules integration pending; reference available.

**Consider:** Which occupants can fire from this conveyance?

- Apply Passenger/Rider commitments, exposure, mounted-fire and Motion rules. Armored halftrack Passengers are not halved solely for Mounted Fire.
- Sidecar Riders have their own FP/SW limits. LC Passengers may not attack; LC armament has separate conditions.
- Keep unsupported conveyance profiles visible with unevaluated applicability.

**Checklist behavior:** Use the actual transport profile, not one Passenger flag.

**Illustrative position:** Halftrack and LC Passengers have different permissions.

**Terms:** PRC, BU, Motion, SW, LC.

#### Consider restricted sewer fire

ID: `sewer`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** B8.43-.45. **PDF pages:** 680, 117. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this sewer unit have a permitted target?

- A sewer unit may attack the Manhole Location above only in AFPh after discovery by Sewer Emergence dr of at least seven.
- B8.44 separately permits normal DFPh/AFPh fire between qualifying ADJACENT sewer units beneath adjacent Manhole Locations. Apply concealment and normal fire restrictions.
- No free emergence or surface LOS is granted. Broken or berserk sewer units are eliminated under B8.45.

**Checklist behavior:** Keep the sewer-specific path discoverable when evaluation is pending.

**Illustrative position:** The unit receives a source-specific target explanation instead of an ordinary map-wide picker.

**Terms:** LOS, FP, AFPh.

### 5.22A · Demolitions and equipment

#### Resolve every operably Placed DC

ID: `placed`. **Who:** BOTH. **Type:** Required attack.

**Timing:** ASOP 5.22A. **Rules:** A23.3-.5; C7.346. **PDF pages:** 680, 90, 91, 177. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which MPh placements require resolution?

- Use the successful operable-placement record. Later Pin, break or elimination of the placer does not cancel it.
- Retain placement Location, facing and applicable CX. Use the armored-target Position DR procedure when required.
- Apply malfunction, including captured DC thresholds, and remove the DC as prescribed. No AFPh FP halving applies.

**Checklist behavior:** Queue the DC independently with its placement provenance.

**Illustrative position:** A squad that broke in DFPh still has its earlier operably Placed DC attack resolved.

**Terms:** Placed DC, DC, CX, TK.

#### Consider a Thrown DC and friendly exposure

ID: `thrown`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A23.6-.63; C7.346. **PDF pages:** 680, 91, 177. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is a legal throw worth its return danger?

- Require unpinned Good Order or berserk Personnel capable of SW use, legal LOS/elevation/adjacency and any same-Location exception.
- Apply target and thrower modifiers plus AFPh +1 to both IFT attacks unless Opportunity Fire. Against an AFV apply the specified Position modifier instead.
- Resolve separate DR for both Locations with elevation/vehicle exceptions and malfunction treatment.

**Checklist behavior:** Preview target and friendly exposure together.

**Illustrative position:** The attack may affect the thrower’s Location as well as its intended target.

**Terms:** Thrown DC, DC, Opportunity Fire, CX.

#### Consider an existing Set DC’s detonation

ID: `set`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A23.7-.72. **PDF pages:** 680, 91. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this controller eligible now?

- Require the setting unit or eligible derivative HS, Good Order, LOS, distance two to six hexes and a subsequent Player Turn; then the NTC.
- Use the Set DC attack, concealment, protection, malfunction and bridge/building destruction rules.
- Do not offer new setting in AFPh. Preserve hidden controller/provenance information.

**Checklist behavior:** Separate voluntary attempt from required demolition consequences.

**Illustrative position:** A surviving derivative HS may control its squad’s earlier Set DC.

**Terms:** Set DC, NTC, LOS, US#.

#### Limber an eligible Gun instead of firing

ID: `limber`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C10.21-.26. **PDF pages:** 680, 180. **Coverage:** Rules integration pending; reference available.

**Consider:** Can Gun and crew make this commitment?

- Check crew, Gun type and prior use; QSU Guns do not receive an ordinary limber action.
- Neither Gun nor crew may fire in that Player Turn. Apply permitted simultaneous CA and acquisition changes.
- Do not automatically impose unlimbering’s TI consequence or grant movement in AFPh.

**Checklist behavior:** Show the full-Player-Turn commitment and resulting configuration.

**Illustrative position:** A crew prepares transport rather than firing.

**Terms:** TI, CA, RFNM.

#### Unlimber an eligible Gun and apply TI

ID: `unlimber`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C10.21-.26. **PDF pages:** 680, 180. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this Gun unlimber now?

- Check crew and prior use; a Gun cannot unlimber while in tow.
- Gun and crew become TI and cannot fire in this Player Turn. Apply permitted facing and acquisition consequences.
- Equipment-specific SP-Gun preparation needs its own note; do not grant this to every vehicle.

**Checklist behavior:** Show future readiness and current TI as separate effects.

**Illustrative position:** The crew prepares for a later fire window without gaining an immediate shot.

**Terms:** TI, CA, RFNM.

### 5.22A · Attack mode and acquisition decisions

#### Compare MG fire at PRC with an eligible armor attack

ID: `mgArmor`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A9.61-.611; D3.53-.54. **PDF pages:** 680, 64, 65, 201. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this an IFT attack or a legal MG To Kill attempt?

- An ordinary IFT MG attack does not penetrate armor, although Vulnerable PRC may be affected. Predesignate any separate AFV To Kill attempt.
- A9.61 requires Normal Range, no FG and no halved-FP penalty. An ordinary SW MG’s AFPh halving therefore matters; Opportunity Fire can remove that particular obstacle.
- Vehicular MG armor attacks require the MG to be MA. Apply D3.53’s ordnance treatment and actual firer/target restrictions rather than granting every vehicle MG a TK shot.

**Checklist behavior:** Show why a different attack mode is available or blocked without hiding it from reference.

**Illustrative position:** An Opportunity-Fire HMG and an ordinary AFPh HMG do not necessarily have the same armor-attack options.

**Terms:** TH, TK, Opportunity Fire, PRC.

#### Review acquisition and alternative ordnance aims

ID: `acquisition`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** C5.7; C6.5-.56; C8.4. **PDF pages:** 680, 172, 173, 174, 175, 178, 212, 220. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should this shot retain acquisition or choose another legal aim?

- Apply the actual weapon/target acquisition rules. Movement, other weapon use, IFE/Canister, configuration and loss of qualifying target/LOS can affect acquisition.
- Preserve Gyrostabilizer exceptions; do not grant ordinary Motion fire Area Target Type or point-blank benefits contrary to its chart.
- Consider Deliberate Immobilization only when its target, range and weapon conditions apply, with its TH modifier and acquisition exclusion.

**Checklist behavior:** Display what a proposed attack loses as well as what it can gain.

**Illustrative position:** An alternate attack mode may sacrifice an acquired-target benefit.

**Terms:** TH, TK, CA, Motion.

#### Reassess targets and cover without erasing movement history

ID: `targets`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** ASOP 5.22A. **Rules:** A7.212; C.8; D9.3-.4. **PDF pages:** 680, 55, 162, 209, 210. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What changed since the defender fired?

- Recheck newly exposed/concealed targets, same-Location targeting restrictions and actual protection. No FFNAM/FFMO applies.
- Vehicle moving-target history does not end simply because MPh ended. AFV TEM/Hindrance eligibility also uses the applicable movement history through AFPh.
- Do not give an AFV its post-AFPh stationary protection early or disclose hidden target data through suggested choices.

**Checklist behavior:** Explain changed visibility and retained history beside the attack.

**Illustrative position:** An AFV that moved this Player Turn does not automatically provide its later-phase protection during AFPh.

**Terms:** LOS, TEM, Motion, PRC.

### 5.23A · Berserk return

#### Return a qualifying berserk unit to Good Order

ID: `berserk`. **Who:** ATTACKER. **Type:** Conditional required result.

**Timing:** ASOP 5.23A. **Rules:** A15.46. **PDF pages:** 680, 84. **Coverage:** Rules integration pending; reference available.

**Consider:** Did its attack meet the return condition?

- Require elimination of all Known enemies, at least one, in its Location by the specified halved TPBF.
- Apply the change after actual results. Eliminating enemies elsewhere or merely losing LOS does not establish this trigger.
- Keep the opening prisoner case and MPh no-Known-enemy return separate.

**Checklist behavior:** Attach the return check to its qualifying attack.

**Illustrative position:** Eliminating the last local Known enemy by qualifying halved TPBF causes the state change.

**Terms:** TPBF, Good Order, AFPh.

### 5.31B · Blaze spread

#### Build the eligible Blaze-spread queue

ID: `blaze`. **Who:** BOTH. **Type:** Required map resolution.

**Timing:** ASOP 5.31B. **Rules:** B25.6-.64. **PDF pages:** 680, 143, 144. **Coverage:** Rules integration pending; reference available.

**Consider:** Which Blazes can spread to which Locations?

- Exclude a Blaze’s initial Player Turn. Include adjacent Burnable Terrain, wreck same-Location spread and Bypass-wreck possibilities.
- Use one DR per eligible target Location with the source offering the greatest chance. Buildings spread horizontally/vertically, not diagonally.
- Apply terrain, attachment, elevation, EC and wind exceptions. Success creates a new Flame, not an immediately propagating Blaze.

**Checklist behavior:** Preview candidate targets and governing sources without duplicate rolls or same-turn cascades.

**Illustrative position:** Two Blazes threaten one Location, but do not grant two independent spread rolls.

**Terms:** Blaze, Flame, EC.

#### Apply Heavy Winds and Gusts spread

ID: `windSpread`. **Who:** BOTH. **Type:** Required weather consequence.

**Timing:** ASOP 5.31B. **Rules:** B25.63-.651. **PDF pages:** 680, 144, 145. **Coverage:** Rules integration pending; reference available.

**Consider:** Which automatic spreads replace rolls?

- Heavy Winds cause the specified downwind automatic spread and prohibit upwind spread; within-building and wreck treatment differ.
- Gusts add automatic spread and Random Selection for eligible two-hex terrain-Blaze spread, placing distant Flames at ground level.
- Apply Wreck Blaze same-Location and possible one-hex exceptions. Do not carry wind-direction modifiers into Flame-to-Blaze conversion.

**Checklist behavior:** Separate automatic outcomes and selection from ordinary spread rolls.

**Illustrative position:** Gusts spread remains due after the earlier SMOKE cleanup.

**Terms:** Gusts, Blaze, Flame, EC.

### 5.32B · Flame conversion

#### Resolve Flame conversion or extinguishing

ID: `flame`. **Who:** BOTH. **Type:** Required terrain result.

**Timing:** ASOP 5.32B. **Rules:** B25.15-.151; B24.721. **PDF pages:** 680, 142, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** Which Flames are eligible to test?

- Skip new and currently Hampered Flames marked PIN. Flames just created by Blaze spread do not immediately convert.
- Use the terrain spread number, EC unless in a building, and Fortified Building modifier. Final DR at least the spread number creates Blaze; at most two extinguishes.
- Intermediate failure leaves the Flame. On Blaze creation remove Flames in that Location and apply smoke/occupancy consequences.

**Checklist behavior:** Offer three outcomes and explicit reasons for skipped counters.

**Illustrative position:** An old unhampered Flame tests; a new WP Flame and a Hampered Flame do not.

**Terms:** New Flame, Hampered Flame, Blaze, EC.

#### Carry Blaze escape obligations into Rout

ID: `blazeExit`. **Who:** BOTH. **Type:** Required follow-up.

**Timing:** ASOP 5.32B. **Rules:** B25.2-.4; A10.41. **PDF pages:** 680, 143, 144. **Coverage:** Rules integration pending; reference available.

**Consider:** Who must leave and by when?

- Apply smoke under wind conditions immediately. Do not immediately eliminate ordinary Infantry simply because a new Blaze appeared.
- Infantry must leave by the end of the next RtPh. Show voluntary break and special exit provisions at their actual window.
- Other occupants require their own rules; avoid a universal Infantry escape procedure.

**Checklist behavior:** Create a linked Rout obligation with its actual deadline.

**Illustrative position:** A new Blaze creates an escape problem for the next phase.

**Terms:** Blaze, RtPh, LOS.

### End choices before 5.33B cleanup

#### Consider an eligible end-phase Gun CA change

ID: `caEnd`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** C3.22; A7.25. **PDF pages:** 680, 55, 169. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can the crew still fire without Intensive/Sustained Fire?

- Assess eligibility before removing fire counters. Do not let cleanup create permission for a spent Gun.
- Apply explicit (un)limbering, pushing and other turret-armament exceptions through their own rules. PFPh movement cancellation is not an AFPh clause.
- Preserve an unexpended Opportunity-Fire Gun’s specific CA restriction rather than bypassing it with this general provision.

**Checklist behavior:** Explain current capability and acquisition effects.

**Illustrative position:** A normally spent Gun cannot gain a free facing change through early marker removal.

**Terms:** CA, IF, AFPh.

#### Consider an eligible end-phase AA-mode change

ID: `aaMode`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** E7.5; C3.22. **PDF pages:** 680, 169, 239. **Coverage:** Rules integration pending; reference available.

**Consider:** May this weapon change mode without an attack?

- E7.5 allows an end-fire-phase change when it can/does change CA under C3.22’s first sentence, even if not a Gun.
- Use actual AA capability; vehicle AAMG do not use ordinary AA counters.
- This is preparation, not an AFPh aircraft attack window.

**Checklist behavior:** Keep mode changes separate from aerial attack permissions.

**Illustrative position:** An eligible weapon prepares AA mode without a current aerial attack.

**Terms:** AA, CA, ROF.

### 5.33B · Fire-counter cleanup

#### Remove specified fire counters at phase close

ID: `cleanup`. **Who:** BOTH. **Type:** Required phase close.

**Timing:** ASOP 5.33B. **Rules:** ASOP 5.33B; A3.5; E1.8; W10.31; E1.951. **PDF pages:** 680, 47, 226, 669, 227. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Are required results and end choices finished?

- Remove Prep Fire, Intensive Fire, No Fire and Bounding Fire. At night also remove First Fire, Final Fire and ordinary Gunflash.
- A switched-on Chapter W searchlight retains Gunflash. Do not remove TI, ordinary PIN, DM or unrelated markers here.
- Keep historical actions needed by later rules. Counter removal does not erase movement, fire or controller history. Also remove Trip Flares activated during the previous Player Turn’s RtPh/APh/CCPh under E1.951; keep flares created during this Player Turn’s MPh until its CCPh.

**Checklist behavior:** Use a typed cleanup list and explicit exceptions before handing off Rout obligations.

**Illustrative position:** Ordinary night Gunflash disappears while a switched-on searchlight’s remains.

**Terms:** Gunflash, AFPh, TI.

### Supplemental equipment and standing choices

#### Consider Goliath detonation

ID: `goliath`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** German Vehicle Note 93; A23.4; C7.346. **PDF pages:** 680, 348. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this controller detonate now?

- Require its purchase-associated controller, SW capacity, unpinned non-BU Good Order status, and LOS to both Goliath and an affected enemy unit or bridge.
- Use the note’s 36-FP own-hex and 16-FP adjacent-hex effects, shared DR, armor treatment and X11, not ordinary DC assumptions.
- Preserve controller, range and equipment history. Explosion on becoming a Burning Wreck is a triggered consequence, not a command.

**Checklist behavior:** Preview blast extent and controller eligibility.

**Illustrative position:** A Goliath in position cannot voluntarily detonate while its controller is pinned.

**Terms:** Goliath, SW, LOS, DC.

#### Apply AFPh Blaze damage aboard landing craft

ID: `lcBlaze`. **Who:** BOTH. **Type:** Required equipment consequence.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** G12.68-.69. **PDF pages:** 680, 308. **Coverage:** Rules integration pending; reference available.

**Consider:** How many unpinned Blazes are aboard?

- Each unpinned Blaze aboard an LC inflicts one DP in each AFPh. Keep this separate from ordinary B25 next-turn spread eligibility.
- Track MP reduction, accumulated damage and elimination. Use LC-specific Blaze/PIN rules.
- Blaze smoke is absent while neither Beached nor immobilized; when applicable, drift appears in the following AFPh.

**Checklist behavior:** Queue equipment damage even when nobody chooses a shot.

**Illustrative position:** An LC with two unpinned Blazes receives two DP.

**Terms:** LC, DP, Blaze.

#### Cancel an existing searchlight beam

ID: `searchlight`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** W10.52; W10.31. **PDF pages:** 680, 669, 670. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the owner cancel its beam?

- Voluntary cancellation is allowed anytime in the owner’s AFPh. Apply prescribed beam and Gunflash consequences.
- Do not infer free retargeting or maintenance. If still switched on, its Gunflash survives ordinary cleanup.

**Checklist behavior:** Connect cancellation to the later cleanup exception.

**Illustrative position:** Keeping the beam active explains why its Gunflash remains.

**Terms:** IB Lane, Gunflash, LOS.

#### Explain the newly landed paratroop restriction

ID: `paratroops`. **Who:** ATTACKER. **Type:** Conditional boundary.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** E9.5-.6. **PDF pages:** 680, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Did these troops land this Player Turn?

- They may not attack during the Player Turn they land. Glider contents’ AFPh permission does not extend to them.
- Keep component placement and Rout/APh restrictions attached to E9. Explain ineligibility rather than hiding the subject.

**Checklist behavior:** Show the rule-backed limitation when landing history is known.

**Illustrative position:** A glider survivor can have a shot when a just-landed parachutist cannot.

**Terms:** Paratroops, AFPh, APh.

#### Resolve A20.4 prisoner cases at their timing

ID: `massacre`. **Who:** Eligible units. **Type:** Rule-specific action or obligation.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A20.4. **PDF pages:** 680, 87. **Coverage:** Rules integration pending; reference available.

**Consider:** Is there an opening obligation or eligible declared action?

- The berserk unit with prisoners case occurs at the start of its fire phase and ends its berserk status as prescribed.
- Optional cases require the listed nationality/status, no-Melee and SW-use conditions.
- Apply No Quarter and the once-only enemy ELR increase to the specified maximum. Do not confuse this with A15.46 TPBF return.

**Checklist behavior:** Place the compulsory case in the opening queue.

**Illustrative position:** An opening prisoner result cannot be postponed until after ordinary attacks.

**Terms:** SW, Good Order, ELR.

#### Apply nationality, terrain and equipment overlays

ID: `nationality`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A19; A21; A25; G.3; G11.8; W7.41; Chapter H. **PDF pages:** 680, 93, 94, 183, 348, 663, 664. **Coverage:** Rules integration pending; reference available.

**Consider:** Which profile changes this choice?

- Apply captured/non-qualified use, shortage, national capabilities, PTO FG, cave and other terrain rules with exact profiles.
- W7.41 includes qualifying CPVA Opportunity Fire restrictions; do not apply that clause to every ordinary AFPh attack.
- Keep identified equipment exceptions visible even while deferred. Unknown scenario/profile applicability stays unevaluated.

**Checklist behavior:** Overlay source-backed conditions without hiding pending reference choices.

**Illustrative position:** One unit’s ordinary and Opportunity Fire can have different profile restrictions.

**Terms:** SSR, FG, Opportunity Fire, Area Fire.

#### Preserve standing choices and ongoing work

ID: `standing`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A12.14; A20.5; B9.32-.323; E10.2-.4. **PDF pages:** 680, 76, 87, 118, 119, 244. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a standing choice affect this shot?

- Keep voluntary concealment loss, Guard Deployment and permitted WA choices at their actual timing; no retroactive WA claim after target declaration.
- E10.2 excludes ammunition-vehicle B# benefit from Bounding (First) Fire but permits otherwise eligible AFPh use. Firing can interrupt replenishment.
- Do not invent prisoner transfer, movement or repair opportunities here.

**Checklist behavior:** Carry ongoing work and capability commitments into the proposed shot.

**Illustrative position:** A stationary eligible Gun can have ammunition support unavailable to a Bounding Firer.

**Terms:** HIP, LOS, SW, WA.

### Consequences and source boundaries

#### Finish attacks and all required consequences

ID: `results`. **Who:** BOTH. **Type:** Required result chain.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A7.3; A9.7; A10; A14-A17; B24.11; B25.12-.14; D.8. **PDF pages:** 680, 55, 56, 65, 82, 84, 85, 140, 143, 192. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What must resolve before dependent actions?

- Complete prescribed leader/unit results, Pin, break, casualty reduction, wounds, HOB and qualifying Sniper triggers.
- Resolve ammunition, malfunction, random weapon destruction, collateral/survival, vehicle Shock/Stun/Recall and wreck effects when triggered.
- Queue qualifying Flame/rubble and feature damage under attack-specific rules. Do not waive a PTC just because defensive firing ended.

**Checklist behavior:** Link a consequence tree to the actual attack; consideration cannot close it.

**Illustrative position:** A late PTC can still matter to Snipers and later combat.

**Terms:** MC, PTC, KIA, HOB, DM.

#### Resolve qualifying Sniper triggers

ID: `sniper`. **Who:** BOTH. **Type:** Triggered obligation.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A14; ASOP dagger. **PDF pages:** 680, 82, 83, 678. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does the original roll qualify?

- Check TH, MC, TC and non-OBA IFT triggers and all exclusions; not every AFPh roll triggers a Sniper.
- Preserve result ordering and permitted Sniper Checks, plus night/seaborne modifications.
- Named RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 exceptions remain external-source gaps.

**Checklist behavior:** Attach the trigger to its roll with viewer-safe disclosure.

**Illustrative position:** A final planned shot can still open a required Sniper chain.

**Terms:** SAN, Sniper, DR, SSR.

#### Keep other-phase options in their proper windows

ID: `notHere`. **Who:** BOTH. **Type:** Boundary explanation.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A7.25; A9.73; A9.8; C8.5; C9.3; E1.92-.93; E7.4. **PDF pages:** 680, 55, 65, 178, 179, 226, 227, 237. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this a current option or a future reminder?

- No new Opportunity Fire, ordinary Smoke, Starshell/IR, artillery Contact/Access or ordinary Ground Support is imported from earlier windows.
- No ordinary SW dismantle/assemble/destroy, movement, DFF/SFF/FPF, grenade, new DC placement or Clearance declaration is invented.
- Explicit exceptions require their own profile. The next phase is Rout, not Advance.

**Checklist behavior:** Offer timing explanations and bookmarks rather than false current actions.

**Illustrative position:** SW assembly remains findable but is explained as an earlier ordinary window.

**Terms:** PFPh, MPh, DFPh, AFPh.

#### Keep pending integrations and missing sources visible

ID: `sourceProfile`. **Who:** BOTH. **Type:** Coverage boundary.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** ASOP dagger; SSR; Chapters F/G/H/W. **PDF pages:** 680, 678. **Coverage:** Rules integration pending; reference available.

**Consider:** Is the source known and the full action path integrated?

- Keep source coverage, legal applicability and integration status independent. Missing evaluation is not illegality.
- The nine-step ledger covers identified in-volume families, not every SSR/module/equipment permutation. Reconcile the exact profile before claiming scenario completeness.
- Public reference entries are different from live viewer-sensitive rows, counts and reasons; never leak HIP or private commitments.

**Checklist behavior:** Show source needed and availability not evaluated explicitly.

**Illustrative position:** A campaign exception remains discoverable without exposing hidden enemy state.

**Terms:** SSR, Source gap.

### Supplemental terrain and movement-status restrictions

#### Consider a climbing unit’s AFPh shot

ID: `climbing`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** B11.431. **PDF pages:** 680, 127. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this climbing unit reach its target?

- Climbing units may fire only during their AFPh and never beyond Normal Range. Trace LOS from the actual climbing vertex.
- Apply AFPh FP treatment and PBF where applicable. Do not silently use hex-center LOS or allow Long Range fire.

**Checklist behavior:** Use climbing position and vertex as explicit attack inputs.

**Illustrative position:** PBF can offset ordinary AFPh halving for an eligible climbing attack.

**Terms:** AFPh, LOS, PBF.

#### Restrict fire while fording a Water Obstacle

ID: `fording`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** B21.42. **PDF pages:** 680, 135. **Coverage:** Rules integration pending; reference available.

**Consider:** Is this unit still fording?

- A fording unit may fire only inherent FP, and only as Area Fire.
- Apply AFPh halving in addition. Do not substitute shallow-stream weapon permissions for this Water Obstacle rule.

**Checklist behavior:** Explain both the weapon exclusion and cumulative FP reductions.

**Illustrative position:** A fording squad cannot add its SW to the AFPh attack.

**Terms:** FP, Area Fire, SW.

#### Carry forward the cavalry Charge prohibition

ID: `charge`. **Who:** ATTACKER. **Type:** Optional choice.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** A13.61. **PDF pages:** 680, 81. **Coverage:** Rules integration pending; reference available.

**Consider:** Did the unit declare a Charge in MPh?

- A unit that declared a Charge may not attack in the ensuing AFPh even if now dismounted.
- Keep the declaration history and its Prep Fire reminder. Dismounting does not restore a shot.

**Checklist behavior:** Show the earlier declaration as the reason for the restriction.

**Illustrative position:** A dismounted former charger has no ordinary AFPh attack.

**Terms:** AFPh, MPh, SW.

#### Resolve a DC placed to Breach a wall

ID: `breach`. **Who:** BOTH. **Type:** Conditional required DC result.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** B23.711; B23.9221. **PDF pages:** 680, 137, 138, 141. **Coverage:** Rules integration pending; reference available.

**Consider:** Was the recorded placement a Breach attempt?

- Use the declared rowhouse or Fortified Building hexside rather than converting it to a generic target-Location attack.
- Resolve the rowhouse wall attack with normal TEM; a detonating NMC-or-better result Breaches. Use the same original DR for the prescribed adjacent-Location Area Fire effect.
- Retain Fortified Building benefits not removed by the Breach and the separate HE KIA Breach provision.

**Checklist behavior:** Link wall result, occupant effects and changed passage/LOS.

**Illustrative position:** The charge can create a route for a later advance without treating all fortification benefits as destroyed.

**Terms:** DC, IFT, Area Fire.

#### Preserve a berserk unit’s blocked-entry attack

ID: `fortifiedBerserk`. **Who:** ATTACKER. **Type:** Conditional obligation.

**Timing:** Rule-specific AFPh timing; supplemental to ASOP. **Rules:** B23.922; A15.43-.46. **PDF pages:** 680, 140. **Coverage:** Rules integration pending; reference available.

**Consider:** Was this charge stopped outside a Fortified Building?

- The berserk unit remains adjacent attacking in AFPh/DFPh until the occupants are sufficiently pinned, broken or Reduced to allow entry, subject to the new-adjacent-enemy exception.
- Do not apply the local halved-TPBF Good Order return merely because enemies in that other Location were eliminated.

**Checklist behavior:** Show the compulsory attack context separately from the local-return trigger.

**Illustrative position:** A berserk unit outside a Fortified Building is not already in its enemy’s Location.

**Terms:** AFPh, TPBF, Good Order.

## Contextual vocabulary

| Label | Meaning |
|---|---|
| AFPh | Advancing Fire Phase. The ATTACKER reviews remaining fire after DFPh, then resolves required fire spread and specified counter cleanup. |
| Opportunity Fire | A7.25 commitment made during PFPh. If still eligible it avoids AFPh halving and the applicable ordnance AFPh penalty and permits ROF/IF. |
| Bounding Fire | Vehicle firing in AFPh after entering a new hex/hexside in MPh. Distinct from Bounding First Fire and from the counter used for Opportunity Fire. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| CA | Covered Arc. |
| Drifting SMOKE | Gray Dispersed SMOKE dependent on an original source, with A24.61 length, height and removal rules. |
| SMOKE | The umbrella for Smoke and WP effects; preserve ammunition type, owner and source. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| TH | To Hit roll or number for an ordnance attack. |
| Case B | C5.2 AFPh ordnance TH modifier: normally +2, +3 in woods/building/rubble. Opportunity Fire is excepted. |
| NMC | Normal Morale Check. |
| Flame | Beginning fire, with no Blaze effects of its own. Eligible old, unhampered Flames test for conversion or extinguishing at AFPh end. |
| Gusts | B25.651 one-Player-Turn wind event with separate AFPh SMOKE and fire-spread consequences. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| Good Order | The defined unit status, separate from merely unbroken. Apply the exact status requirement of each action. |
| FP | Firepower. |
| IFT | Infantry Fire Table. |
| PBF | Point Blank Fire. |
| TPBF | Triple Point Blank Fire. Check same-Location target restrictions, AFPh modifiers and the A15.46 berserk return condition separately. |
| Area Fire | A firepower-halving treatment. Multiple applicable causes can be cumulative; it is different from the ordnance Area Target Type. |
| Assault Fire | A7.36 capability shown by underscored squad FP. Modify each squad’s inherent FP, add one, then round fractions up. Not available for Opportunity Fire or Long Range Fire. |
| FRU | Fractions rounded up, applied separately to each qualifying Assault Fire squad after modification and its +1 bonus. |
| ROF | Rate of Fire. A retained opportunity is not an automatic shot. |
| IF | Intensive Fire, an extra Gun shot with accuracy and malfunction penalties. |
| MMG | Medium Machine Gun. A4.41 restricts firing after the weapon moved in MPh. |
| HMG | Heavy Machine Gun. A4.41 restricts firing after the weapon moved in MPh. |
| RCL | Recoilless Rifle. Movement and backblast rules depend on the actual weapon, including the U.S. 57mm exception. |
| MPh | Movement Phase. THE MPh contains a separate start, during and end for each moving unit or stack. |
| FG | Fire Group, subject to formation and mandatory grouping rules. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| SMC | Single-Man Counter, such as a leader or Hero. |
| Spraying Fire | A capability allowing an attack against two qualifying target Locations with its specific FP and geometry rules. |
| FT | Flamethrower. |
| Motion | A vehicle state. It is not interchangeable with Non-Stopped or a moving target for fire modifiers. |
| MOL | Molotov Cocktail. A squad MOL and a MOL Projector have different procedures. |
| Encirclement | A7.7 condition established by qualifying attacks or upper-level circumstances, with subsequent morale, fire and movement consequences. |
| TK | To Kill resolution, distinct from the To Hit roll. |
| Case C | C5.3 Bounding Firer treatment, including Case B and the applicable Gun-type modifier. |
| TEM | Terrain Effects Modifier. |
| HEAT | High Explosive Anti-Tank ammunition. |
| APCR | Armor Piercing Composite Rigid ammunition. |
| APDS | Armor Piercing Discarding Sabot ammunition. |
| IFE | Infantry Firepower Equivalent, a weapon-specific alternative to ordinary TH fire. |
| LATW | Light Anti-Tank Weapon. Use its own target, range, operator and TH rules, including applicable Case C3. |
| PF | Panzerfaust. Inherent SW availability and use follow C13.3; it is not synonymous with Prep Fire. |
| PSK | Panzerschreck, a light anti-tank weapon with backblast restrictions. |
| ATR | Anti-Tank Rifle. Distinguish its non-ordnance FP attack from its ordnance TH/TK use. |
| Case C3 | C5.34 LATW modifier for AFPh without Opportunity Fire; the building/rubble backblast condition can apply separately. |
| MTR | Mortar. |
| Case C4 | C5.35 Motion/Non-Stopped ordnance treatment, including doubling the lower die for non-stabilized fire. |
| MA | Main Armament. |
| Bounding First Fire | Vehicle fire during its own MPh. D3.32 can preserve one AFPh shot for a retained-ROF weapon under specific conditions. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| LC | Landing Craft, governed by its own transport, fire, beaching and grounding rules. |
| Placed DC | DC operably placed during MPh. Its AFPh attack survives later Pin, break or elimination of its placer. |
| DC | Demolition Charge. |
| CX | Counter Exhausted. A state caused by Double Time and other rules, with consequences beyond extra movement. |
| Thrown DC | Immediate DC attack against the target and normally thrower Locations, with an additional AFPh modifier unless Opportunity Fire. |
| Set DC | A23.7 demolition established by SSR or a prior setting procedure, with controller and later-Player-Turn detonation requirements. |
| NTC | Normal Task Check, used only when the governing rule calls for it. |
| US# | Unit Size Number. Used for purposes including prisoner guarding capacity and the dr to Set a DC; it is not a weapon Usage Number. |
| RFNM | Restricted Fire, No Movement, the Gun classification in C10.25. Apply any equipment-note exceptions to its firing and movement restrictions. |
| Blaze | A continuing fire state, distinct from a Flame and from its smoke counter. |
| EC | Environmental Conditions. |
| New Flame | A Flame marked with PIN in its initial Player Turn so it cannot convert to Blaze in that turn’s AFPh. |
| Hampered Flame | A Flame marked with a PIN counter after the specified Clearance result, preventing its spread to Blaze during that Player Turn. |
| RtPh | Rout Phase, immediately following AFPh. New Blaze exit obligations can carry into this phase. |
| AA | Anti-Aircraft fire or mode. |
| Gunflash | Night firing-location marker, normally removed at AFPh end, with exceptions such as a switched-on Chapter W searchlight. |
| Goliath | German Vehicle Note 93 remotely controlled demolition vehicle with its own AFPh detonation and blast rules. |
| DP | Damage Point, used by landing craft. Each unpinned Blaze aboard an LC inflicts one DP in each AFPh under G12.68. |
| IB Lane | Illuminating Beam Lane created by a Chapter W searchlight. Cancellation has its own timing; it is not ordinary SMOKE. |
| Paratroops | E9 landing troops. Unlike ordinary glider contents they may not attack during the Player Turn they land. |
| APh | Advance Phase, following Rout. AFPh fire and the advance are different windows. |
| ELR | Experience Level Rating. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| HIP | Hidden Initial Placement. No guide prompt may leak hidden opposing information. |
| WA | Wall Advantage. Its claim, loss and mandatory conditions depend on B9.32 and the position. |
| MC | Morale Check. |
| PTC | Pin Task Check. |
| KIA | Killed In Action attack result. |
| HOB | Heat of Battle, a triggered resolution rather than an optional action. |
| DM | Desperation Morale, distinct from dismantled dm. |
| SAN | Sniper Activation Number, tested only for qualifying Original rolls. |
| Sniper | A14 procedure triggered by qualifying original rolls matching the opposing SAN, subject to exclusions and special profiles. |
| DR | Two-dice roll. Keep Original, modified and individual colored/white values. |
| PFPh | Prep Fire Phase, the ATTACKER’s early fire and preparation window. |
| DFPh | Defensive Fire Phase. Its firing is Final Fire under A8.4, distinct from Defensive First Fire in the preceding MPh. |
| Source gap | A named rule or module whose full governing text is not in the supplied volume. Its applicability remains unevaluated. |

## Acceptance scenarios for the eventual live component

| Position | Required behavior |
|---|---|
| Unmoved ordinary squad | Apply AFPh halving; do not infer Opportunity Fire. |
| Pinned Assault Fire squad | Apply Pin and other factors, then the eligible bonus per squad. |
| Two squads in a FG | Round qualifying Assault Fire contributions individually. |
| Moved HMG versus stationary Recovered HMG | Use the weapon’s history and exact exceptions. |
| Opportunity-Fire mortar loses Spotter and has no target in its LOS | Show the required breakdown roll, not silent completion. |
| Vehicle fires only MA in MPh and retains ROF | Evaluate one D3.32 carryover shot; no continued AFPh ROF. |
| Successful Placed DC carrier subsequently breaks | Preserve the due DC attack. |
| Dispersed WP followed by Gusts | Resolve WP effects before removing the counter. |
| Two Blazes threaten one Location | Use the best eligible source, not two independent rolls. |
| Flame created by this phase’s spread | Do not convert it immediately into Blaze. |
| New Blaze under Infantry | Carry the actual escape deadline into Rout. |
| Switched-on searchlight | Preserve its Gunflash during ordinary night cleanup. |
| Optional topic considered | No attack, marker, random roll or obligation is resolved. |
| Missing rule evaluator or module source | Preserve the reference and explain the specific gap. |

## Preview verification

The local browser check exercises all 347 topics across Rally, Prep Fire, Movement, Defensive Fire and Advancing Fire, including every contextual term button. It verifies catalog identities, group membership, physical-page bounds, all nine AFPh ledger mappings, timing navigation, independent consideration state and host-state restoration. Layout checks cover 320px and 736px, with visual inspection of narrow, desktop and dark-mode screenshots. No preview runtime errors were observed. These checks validate design content and reference interactions, not ASL legality or engine execution. No .NET code was changed.
