# ASL Movement Phase Component Walkthrough

Date: 2026-10-26

Status: Source-reconciled design content and interactive reference preview. No Blazor component or Rules behavior is implemented by this document.

Design target: the completed ASL.Rules implementation after all passes. All cataloged options remain visible now, including pending rules and deferred chapter families. See the [Turn Phase Guide Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md), [Prep Fire walkthrough](ASL%20Prep%20Fire%20Phase%20Component%20Walkthrough.md), and [Movement catalog](ASL%20Movement%20Phase%20Checklist%20Catalog.json).

## Sources and scope

Authority: the supplied eASLRB v3.01 at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. The printed MPh ASOP is on physical PDF pages 679-680. Page locators throughout are one-based physical PDF pages. The ASOP layout was visually checked; the ledger below accounts for all 20 printed steps, including every action family named in the dense 3.22A and 3.32A steps.

Reconciliation follows the ASOP references and supplements them with the movement/exposure, defensive-fire, vehicle, night, Search, DC and Chapter W rules. A whole-volume search for MPh/Movement Phase produced 930 text hits across 259 pages, including indexes, charts, examples and repeated material. Those hits are discovery evidence, not 930 distinct options or a claim that every occurrence was individually audited.

The catalog contains 114 topics and 92 contextual vocabulary labels. Some records intentionally group related choices, such as Start/Stop/Delay/Reverse or the Clearance targets. They are not 114 executable actions. Exact live eligibility remains a Rules responsibility. Full coverage of every SSR, individual Chapter H note and external module requires a scenario-profile inventory. Named gaps are retained: O1.5 Debris and the ASOP dagger’s RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13 Sniper references. They are neither silently omitted nor represented as fully verified.

### Source tension retained for review

ASOP 3.32A lists dropping prisoners in the MPh. A20.53 instead refers voluntary abandonment to A20.5, whose listed windows are friendly RPh/APh (PDF 87). The `dropDuring` record preserves the ASOP option and the body-rule restriction together. Do not enable MPh abandonment merely from this checklist until the governing interpretation is settled. The ASOP ordering precedence does not by itself settle a question about permission. This is a source-interpretation issue, distinct from rules integration pending.

## Movement interaction model

THE MPh contains repeated START, DURING and END sequences for ITS MPh. Initial berserk movers precede non-berserk movers; ground movement precedes aerial landing sequences. Within a printed step order is flexible unless a governing rule restricts it; the ASOP precedence and ATTACKER-first conflict rule remain in force.

The component needs four separate scopes: phase, unit/stack movement instance, expenditure or group impulse, and response/consequence window. A consideration identity must include game, Player Turn, viewer, subject, option and window. Passing one response never passes future expenditures. Completing a shot never automatically exhausts retained ROF. Ending one unit’s move never ends THE MPh.

### Proposed live controls

- **Plan route:** show known costs, modes and commitments without spending movement or promising hidden-information-dependent safety.
- **Commit expenditure:** name the actual entry, facing, Stop, Recovery, placement or other action and its MF/MP. Do not bundle separate actions to reduce response windows.
- **Resolve required effects:** process residual, FFE, mines and triggered consequences at their source timing. Keep before-OVR DFF and after-OVR Reaction Fire distinct.
- **Respond or pass:** show the correct decision owner, weapon capability and expenditure allowance. Mandatory TPBF and unresolved consequences cannot be bypassed by a generic pass.
- **End this unit’s move:** review Search, sewer emergence, small-arms Scrounging, Bypass and vehicle ending conditions. Search can create another defensive-fire opportunity.
- **End phase:** resolve due Blaze, Wreck Removal, canister, berserk and cleanup procedures; retain fire markers that expire later.

The reference preview exposes all topics and definitions and records consideration only. It does not execute these controls, simulate eligibility, or resolve obligations. Its topic count is a catalog count, not a live count of legal actions.

### Innovations worth carrying forward

1. **Expenditure receipt:** a compact record of who spent what, where, using which mode and facing, followed by the exact response window. This makes same-Location activities as visible as map movement.
2. **Commitment comparison:** ordinary movement, Assault Movement, Dash, Double Time, Minimum Move, Armored Assault and Bypass show different costs, exposure and later restrictions. Do not rank a route from undisclosed enemy data.
3. **Two finish checks:** End this unit’s move offers last-window options without skipping responses; End phase performs the distinct shared cleanup. Optional reading never becomes a compulsory game task.
4. **Linked participants:** transport, towing, Armored Assault and impulses retain independent unit, weapon and vehicle states within a shared procedure. A casualty does not erase the rest of the group’s obligations.
5. **Changed-state reminders:** invalidate only affected consideration records after Pin, break, wound, Stun, Bog, illumination or a new expenditure. A stale acknowledgment must not suppress a newly mandatory decision.

### Worked movement sequence

A squad plans a move, a SW Recovery and possible Search. It first compares movement modes and portage. It commits the entry MF, then resolves existing effects and the defender’s response. A defender retaining ROF still obeys the shot limit for that expenditure. Only after that window closes may the squad commit Recovery as a new MF expenditure and open its own response. If it remains eligible with one MF available, Search is considered at the end of ITS MPh; Search itself can draw fire and can trigger casualties. The unit’s completed move then returns to mover selection. Phase-end residual cleanup occurs only after all ground and aerial movement sequences and required effects finish.

A vehicle example needs an additional order distinction: declare a vehicular OVR with entry; resolve applicable Bog and pre-OVR DFF; resolve the OVR even when the rules retain a reduced attack after immobilization/destruction; then offer the appropriate Reaction Fire. A Gun Duel is not available to the OVR vehicle. The UI should show these windows as separate decisions rather than a single Attack checkbox.

## Integration evidence and limits

Coverage labels summarize [ASL Rule Coverage](../ASL%20Rule%20Coverage.md), including its later pass amendments, rather than asserting newly tested support. Movement, portage, defensive fire, vehicle actions and some transport/Search paths exist in part. Recorded remaining issues include Dash; special terrain; mandatory A8.312 fire; residual calculation/ROF/ordnance cases; and extensive special movement. Pass 70 adds Search and concealment paths, but does not justify a blanket fully-integrated label for mines, fortifications and every Search exception. New detailed specialty paths are conservatively pending until a verified action path exists.

The current passes target A-E. F/G/H and Chapter W references here describe the intended discoverable catalog, not a promise that the existing pass schedule implements those chapters. No unsupported option is hidden or marked illegal because an evaluator is absent. Exact equipment and SSR applicability remains unknown until its source profile is loaded.

## Printed ASOP reconciliation ledger

| Step | Clauses covered | Catalog IDs |
|---|---|---|
| 3.11A | Replace eliminated/non-Good-Order mortar Spotter. | `spotter` |
| 3.12D | Opening defender leader/MMC/CE AFV Starshell attempts. | `starshellOpening` |
| 3.13A | All gliders blue aerial side up at ILH. | `gliderSetup` |
| 3.21A | Select currently berserk required charge, otherwise ordinary selection. | `berserkPrepare` |
| 3.22A | Ground eligibility; dropping SW/Gun; night FoM/Movement/Straying; Double Time/Assault/Dash/Sewer/Climb; Human Wave/Banzai/Swimming/Dare-Death; Gallop; Motion counter; Column/Convoy/Platoon/radioless NTC; flail TB; surf checks; crew entry/abandonment; transition to ground or aerial/end. | `eligibility`, `startDrop`, `freedom`, `strayingStart`, `doubleStart`, `assault`, `dash`, `sewerStart`, `climbStart`, `wave`, `dareDeath`, `swimStart`, `gallopStart`, `motionStart`, `formations`, `radioless`, `flail`, `surfStart`, `crewVehicle` |
| 3.23A | Aerial selection and routing to glider AA or parachute drift. | `aerialQueue` |
| 3.31A | Required berserk charge with beach exception, then expenditure effects/fire. | `berserkCharge` |
| 3.32A | Ground movement; Sewer/reliability/Stall/Bog Removal/Straying; Infantry OVR; late Double Time; Manhandling/Labor/TI; Place/Set DC/US#; all listed transport/Recovery/drop options; all eight Clearance targets and immediate Flame; hook/unhook; grenades/dispensers/WP/Flame; WA; T-H/DC Hero; Gallop/Charge; Bog/Recall/BFF/Wreck Removal/ESB/HD/CE; Armored Assault; Wreck Checks; impulse-before-fire; concealment/Column; Lost/Falling/Swimming/Climb/Panji; skis/dust/DD screens; beach/Aground/surf/Wading CX. | `route`, `minimum`, `bypass`, `terrainModes`, `vehicleMotion`, `reliability`, `sewerTravel`, `climbSwim`, `lateDouble`, `concealment`, `wallAdvantage`, `recover`, `dropDuring`, `prisonerMovement`, `recapture`, `manhandle`, `placeDC`, `setDC`, `mount`, `hook`, `clearDeclare`, `clearFlame`, `skis`, `smoke`, `infantryOVR`, `hero`, `cavalry`, `bog`, `recall`, `bounding`, `gunDuel`, `vehicleOVR`, `esbHD`, `crewPosture`, `armoredAssault`, `wreckRemoval`, `wreckChecks`, `impulse`, `column`, `panji`, `dust`, `ddScreens`, `beach`, `surf` |
| 3.33A | Glider Evasive Action, return to AA. | `gliderEvade` |
| 3.34A | All aerial parachutes drift, then fire or phase-end if none. | `parachuteDrift` |
| 3.35D | Moving-ground-unit residual/Fire Lane/FFE/mine attacks; later illumination; Column Disbandment; trip flare. | `column`, `residualAttack`, `ffeMove`, `mines`, `starshellLater`, `tripFlare` |
| 3.36D | First/SFF/FPF; Snap Shot/Fire Lane/Thrown DC/Reaction Fire/air Ground Support; residual and all listed fire markers; MOL-Projector Smoke/Flame; reaction T-H Hero; defender Motion/dispenser; attacker Light AA; Column Disbandment. | `column`, `firstFire`, `subsequent`, `fpf`, `mandatoryTPBF`, `snap`, `fireLane`, `defenderWeapons`, `throwDC`, `ovrPrevention`, `reaction`, `heroReaction`, `motionAttempt`, `airSupport`, `attackerAA`, `residualCreate` |
| 3.37D | Light AA First/SFF against glider; markers; evasion or landing branch. | `gliderAA` |
| 3.38D | Small-arms/Light-AA First/SFF against all aerial parachutes; markers; landing. | `parachuteFire` |
| 3.41A | Search/casualties/TI with defensive fire still allowed; broken Bypass obstacle entry; sewer emergence; Unarmed small-arms Scrounging; Motion/Stop; CC counter; return to next mover. | `search`, `brokenBypass`, `sewerEmerge`, `smallArms`, `endVehicle` |
| 3.42A | Glider Landing DR/Crash dr; return to aerial selection. | `gliderLand` |
| 3.43A | Non-German 5/8-inch displacement; all parachutes land; 1/2-inch flip; NMC/NTC. | `parachuteLand` |
| 3.51A | Vehicle and aerial-landing terrain-Blaze elimination; Wreck Removal resolution; pre-1942 German arms canisters. | `blazeEnd`, `wreckEnd`, `canisters` |
| 3.52A | Berserk units with no Known enemy in LOS return to Good Order. | `berserkEnd` |
| 3.53B | Remove all residual and applicable 1/2-inch SMOKE, cave exception. | `cleanup` |

Supplemental records retain actual rule-specific timing. They do not invent ASOP substeps. The shared attack chain includes the dagger’s Sniper procedures. The catalog is a source-backed design inventory, not a replacement for the rulebook.

## Cataloged options

### Movement contract and expenditure windows

#### Commit one expenditure, then resolve responses

ID: `expenditure`. **Who:** BOTH. **Type:** Decision-window contract.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** A8.1-.14; D2.1; ASOP 3.32A-3.36D. **PDF pages:** 679, 59, 195. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What is being spent now, and whose decision follows?

- Keep the route proposal separate from committed expenditures. Entering a hex and Recovering a weapon are separate actions, even in the same Location.
- Record the actual MF/MP cost, route, facing and participating units. Complete an impulse’s expenditures before First Fire.
- Resolve triggered effects and the defender’s current response before the next expenditure. A pass closes only this window, not later windows or the entire MPh.

**Checklist behavior:** A live component needs distinct Continue movement, End this unit’s move and End phase actions, with Rules-owned gates. This reference preview only records consideration.

**Illustrative position:** A squad enters for 1 MF, then wants to Recover a SW for another MF. The defender gets the entry response before Recovery is committed.

**Terms:** MF, MP, DFF, ITS MPh.

#### Choose the next mover without skipping compulsory movement

ID: `order`. **Who:** ATTACKER. **Type:** Sequence obligation.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** ASOP MPh preamble; A15.43; A4.1. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which movement sequence must finish before another begins?

- Complete all units that began THE MPh berserk before any non-berserk unit starts ITS MPh.
- Complete ground movement before aerial glider/parachute sequences. Revisit currently berserk units at 3.21A; newly created berserk states can interrupt other procedures.
- Within a printed step, choose the order unless a rule says otherwise. Do not combine different units into a stack merely to share a completed checklist.

**Checklist behavior:** Track a separate movement instance for each unit/stack and the appropriate group procedure. No moving units is a valid transition only after required work is settled.

**Illustrative position:** An unmoved berserk squad prevents starting an ordinary tank’s move. A later berserk result requires its own source-governed interruption.

**Terms:** MPh, ITS MPh, Human Wave.

### 3.11A-3.13A · Start of the phase

#### Review replacement of a lost mortar Spotter

ID: `spotter`. **Who:** ATTACKER. **Type:** Optional opening choice.

**Timing:** ASOP 3.11A. **Rules:** C9.3. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does a mortar need a replacement Spotter?

- C9.3 requires recorded elimination, break or capture before replacement at a following owner MPh opening. Non-Good-Order status alone, such as Berserk, does not permit replacement.
- Check spotting qualifications and commitments under C9.3; do not treat this as unrestricted mid-move designation.

**Checklist behavior:** Associate the replacement with its mortar and preserve all spotting commitments.

**Illustrative position:** A broken Spotter can trigger this opening review before any unit starts moving.

**Terms:** MTR, LOS.

#### Attempt opening defender Starshells

ID: `starshellOpening`. **Who:** DEFENDER. **Type:** Optional opening choice.

**Timing:** ASOP 3.12D. **Rules:** E1.91-.921. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Would illumination matter before the attacker starts moving?

- Evaluate eligible leaders, MMC and CE AFV in this opening window.
- Keep placement, usage and illumination results separate from later restricted attempts.

**Checklist behavior:** Recompute disclosed illumination and LOS before the first move.

**Illustrative position:** The defender considers an eligible Starshell before the attacker selects its first mover.

**Terms:** NVR, CE, IR.

#### Place aerial gliders at their intended landing hexes

ID: `gliderSetup`. **Who:** ATTACKER. **Type:** Required aerial setup.

**Timing:** ASOP 3.13A. **Rules:** E8.2. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Are incoming gliders represented in their aerial Locations?

- Place all applicable gliders blue side up at their ILH.
- Placement does not bypass ground movement or begin landing; the aerial response sequence comes later.

**Checklist behavior:** Keep gliders queued for 3.23A and 3.37D after ground movement.

**Illustrative position:** A glider appears at its ILH now, but the ground units finish their MPh first.

**Terms:** ILH, MPh.

### 3.21A · Berserk priority

#### Prepare the next required berserk charge

ID: `berserkPrepare`. **Who:** ATTACKER. **Type:** Compulsory movement.

**Timing:** ASOP 3.21A. **Rules:** A15.43; ASOP 3.21A. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is there a currently berserk unit required to charge?

- Determine the legal charge target and route using the berserk rules and Known enemy information.
- Enter 3.31A for that charge; only proceed to ordinary selection if no such unit can charge.

**Checklist behavior:** Do not offer a general decline as an alternative to a compulsory charge.

**Illustrative position:** After a fire result creates a berserk unit, the guide revisits charge obligations rather than keeping a stale ordinary move plan.

**Terms:** LOS, MF, ITS MPh.

### 3.22A · Select a ground mover and declarations

#### Select an eligible unit, stack or group

ID: `eligibility`. **Who:** ATTACKER. **Type:** Movement eligibility.

**Timing:** ASOP 3.22A. **Rules:** A4.1; D2.1. **PDF pages:** 679, 47, 50, 195. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Who can move, and what commitments already limit them?

- Check Good Order/Mobile status, pinned Infantry and Prep/Bounding Fire or TI commitments; preserve explicit special-rule exceptions.
- Calculate MF/MP, portage, leader accompaniment and movement type. Do not restore already-spent MF merely because a SW is dropped.
- Review road bonus, terrain and prior unloading restrictions from their own rules.

**Checklist behavior:** Show available movement and the reason for known restrictions. Unknown terrain evaluation stays visible as not evaluated.

**Illustrative position:** A squad that held fire may move; its neighboring Prep-firing squad cannot be treated as another ordinary mover.

**Terms:** MF, MP, TI, CX.

#### Drop possession before moving

ID: `startDrop`. **Who:** ATTACKER. **Type:** Optional possession choice.

**Timing:** ASOP 3.22A. **Rules:** A4.43. **PDF pages:** 679, 50. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should carried equipment be left before movement begins?

- Name the weapon and its Location; dropping possession is not a Rally transfer to another unit.
- Recalculate starting portage correctly and retain any independent Recovery requirements for the next owner.

**Checklist behavior:** Record ownership and load separately from movement allowance.

**Illustrative position:** A squad leaves an MMG before starting; another unit still needs its own legal Recovery opportunity.

**Terms:** SW, PP, IPC.

#### Check night Freedom of Movement

ID: `freedom`. **Who:** ATTACKER. **Type:** Conditional movement restriction.

**Timing:** ASOP 3.22A. **Rules:** E1.21. **PDF pages:** 679, 223. **Coverage:** Rules integration pending; reference available.

**Consider:** Is a night Scenario Defender unit free to move?

- Check the actual release conditions, including permitted attacks, Known enemy sightings and applicable sharing rules.
- Offer the best eligible leader’s once-per-friendly-MPh dr only when E1.21 permits it; this is not a generic NTC.
- Preserve acquired Freedom of Movement and distinguish Scenario Defender from this turn’s DEFENDER.

**Checklist behavior:** Explain publicly known restrictions without revealing unseen opposing units.

**Illustrative position:** A side now taking its Player Turn can still be the Scenario Defender and subject to night movement limits.

**Terms:** Freedom of Movement, NVR, NTC.

#### Resolve required Movement and Straying checks

ID: `strayingStart`. **Who:** ATTACKER. **Type:** Conditional opening procedure.

**Timing:** ASOP 3.22A. **Rules:** E1.53-.531; G2.22; G3.21; G13.83. **PDF pages:** 679, 224, 225. **Coverage:** Rules integration pending; reference available.

**Consider:** Does night, jungle, bamboo or beach movement require a check?

- Use the applicable rule and unit conditions, rather than applying one universal night roll.
- Resolve movement direction, restrictions and any Jitter trigger before continuing the intended route.

**Checklist behavior:** Keep the intended route and the actual permitted route distinct.

**Illustrative position:** A unit that planned to move north must follow a source-required Straying result instead.

**Terms:** Straying, NVR, DR.

#### Declare Double Time at the start

ID: `doubleStart`. **Who:** ATTACKER. **Type:** Optional movement mode.

**Timing:** ASOP 3.22A. **Rules:** A4.5. **PDF pages:** 679, 50, 51. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is gaining two MF worth the CX consequences?

- Check eligibility and declare before the unit begins its movement.
- Add the permitted two MF and CX; retain portage and other restrictions. Compare with the smaller later declaration.

**Checklist behavior:** Show effects on later fire and CC as well as movement.

**Illustrative position:** A squad considers reaching cover with two extra MF before it spends its first MF.

**Terms:** Double Time, MF, CX.

#### Declare Assault Movement

ID: `assault`. **Who:** ATTACKER. **Type:** Optional movement mode.

**Timing:** ASOP 3.22A. **Rules:** A4.61. **PDF pages:** 679, 51. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can the planned move qualify for Assault Movement?

- Declare before moving; satisfy the one-Location and less-than-all-MF conditions, including qualifying bonuses and exclusions.
- Do not apply it to Cavalry or prohibited PRC/hazardous activity. It cannot be voluntarily cancelled to recover options.
- Avoiding FFNAM does not automatically avoid FFMO; reassess loss of status if the governing rules require it.

**Checklist behavior:** Make the mode’s route constraints visible before commitment.

**Illustrative position:** A one-Location move across Open Ground may avoid FFNAM while still receiving FFMO.

**Terms:** Assault Movement, FFNAM, FFMO.

#### Declare Dash across a road

ID: `dash`. **Who:** ATTACKER. **Type:** Optional movement mode.

**Timing:** ASOP 3.22A. **Rules:** A4.63. **PDF pages:** 679, 51, 52. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this road crossing qualify for Dash?

- Verify the declared start, crossed road Location and destination under A4.63 before accepting the mode.
- Apply Dash-specific expenditure and defensive-fire restrictions rather than treating it as ordinary Assault Movement.

**Checklist behavior:** Keep the crossing and its response opportunities linked as one special procedure.

**Illustrative position:** The player compares an ordinary crossing with a qualifying Dash, without assuming every road hex permits one.

**Terms:** Dash, DFF, MF.

#### Declare Sewer entry

ID: `sewerStart`. **Who:** ATTACKER. **Type:** Optional special movement.

**Timing:** ASOP 3.22A. **Rules:** B8.4. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this unit use the Sewer route?

- Verify entry Location, unit and scenario qualifications.
- Queue travel/Lost checks and a separate end-of-unit emergence attempt; ordinary surface routes do not describe this movement.

**Checklist behavior:** Keep sewer position private as the rules require.

**Illustrative position:** A sewer-moving stack has its emergence review at 3.41A even if its surface move list is empty.

**Terms:** MF, Straying, ITS MPh.

#### Declare Climbing

ID: `climbStart`. **Who:** ATTACKER. **Type:** Optional special movement.

**Timing:** ASOP 3.22A. **Rules:** B11.4. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Is Climbing the intended way across this cliff?

- Check unit eligibility, the cliff route and required declaration before movement.
- Track Climb counters, costs and possible Falling checks through the procedure.

**Checklist behavior:** Replace ordinary hex-entry prompts with climbing progress and exposure.

**Illustrative position:** A climber’s partially completed ascent is not represented as an ordinary move into the upper hex.

**Terms:** MF, Hazardous Movement.

#### Declare Human Wave or Banzai movement

ID: `wave`. **Who:** ATTACKER. **Type:** Optional group movement.

**Timing:** ASOP 3.22A. **Rules:** A13.62; A25.23; G18.61; G1.5. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this group qualify for the intended charge procedure?

- Validate participants, leadership, target/direction and nationality-specific declaration requirements.
- Track each impulse and its constraints; expend the group’s MF/MP before First Fire.
- Keep Japanese Banzai and other Human Wave variants distinct rather than copying one generic rule.

**Checklist behavior:** Create one group procedure with individual casualties and movement states.

**Illustrative position:** Several squads participate in a charge; resolving one squad’s fire result does not finish the other participants.

**Terms:** Human Wave, Banzai, Impulse.

#### Declare Dare-Death berserk status

ID: `dareDeath`. **Who:** ATTACKER. **Type:** Optional declaration, compulsory consequence.

**Timing:** ASOP 3.22A. **Rules:** G18.6. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should eligible Dare-Death Infantry become berserk now?

- Verify the designated unit and declaration conditions.
- Once declared, follow the required charge in 3.32A rather than ordinary elective movement.

**Checklist behavior:** Show the loss of route discretion before confirmation.

**Illustrative position:** An eligible Chinese unit can choose the declaration, but cannot then choose an ordinary cautious route.

**Terms:** MF, LOS.

#### Declare Swimming movement

ID: `swimStart`. **Who:** ATTACKER. **Type:** Optional special movement.

**Timing:** ASOP 3.22A. **Rules:** E6. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Can and should this unit cross by Swimming?

- Check water, unit, equipment and entry qualifications.
- Queue required TC/MC, movement limitations and consequences instead of normal walking costs.

**Checklist behavior:** Retain Swimming as a visible option even when water movement is not integrated.

**Illustrative position:** The route includes water; the guide offers the rule reference without claiming the current engine can adjudicate it.

**Terms:** MF, TC, MC.

#### Declare Cavalry or wagon Gallop

ID: `gallopStart`. **Who:** ATTACKER. **Type:** Optional movement mode.

**Timing:** ASOP 3.22A. **Rules:** A13.36; D12.4. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the horse or wagon Gallop from the start?

- Check CX and type restrictions; opening Cavalry Gallop adds eight MF, wagon Gallop four.
- Do not substitute the later Cavalry four-MF option for the opening declaration.

**Checklist behavior:** Track mount and rider states separately where required.

**Illustrative position:** A Cavalry unit weighs the opening eight-MF option before spending any movement.

**Terms:** MF, CX.

#### Begin movement of a vehicle in Motion

ID: `motionStart`. **Who:** ATTACKER. **Type:** State transition.

**Timing:** ASOP 3.22A. **Rules:** D2.1; D2.4; ASOP 3.22A. **PDF pages:** 679, 195. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Will this vehicle expend MP or MF now?

- Remove the Motion counter when the vehicle will expend movement as prescribed.
- Preserve the correct Non-Stopped and moving-target conditions; removing the counter does not mean it Stopped.

**Checklist behavior:** Keep separate fields for counter state, expenditure state and fire modifiers.

**Illustrative position:** A vehicle begins ITS MPh from Motion without becoming a stationary firing target.

**Terms:** Motion, MP, Non-Stopped.

#### Check Column, Convoy and Platoon movement

ID: `formations`. **Who:** ATTACKER. **Type:** Conditional group procedure.

**Timing:** ASOP 3.22A. **Rules:** E11.2; E11.52; D14.2. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Is movement linked to a formation?

- Validate formation membership, order and movement constraints before individual expenditure.
- Track group expenditure, leaders and disbandment triggers; do not silently turn a group into independent moves.

**Checklist behavior:** Expose each formation’s governing procedure and leave unavailable evaluators explicit.

**Illustrative position:** A convoy member cannot skip ahead just because its individual MP budget allows it.

**Terms:** MP, Impulse.

#### Attempt non-Platoon movement with a radioless AFV

ID: `radioless`. **Who:** ATTACKER. **Type:** Conditional task check.

**Timing:** ASOP 3.22A. **Rules:** D14.23. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this onboard AFV need permission to move independently?

- Check radioless status, Platoon choice and the Recalled exception.
- Resolve the NTC and its consequences before normal movement; do not infer automatic success from an unimplemented check.

**Checklist behavior:** Attach the roll to this vehicle’s movement instance.

**Illustrative position:** An onboard radioless tank considers leaving its platoon; a Recalled vehicle uses the stated exception.

**Terms:** AFV, NTC.

#### Declare a flail Trail Break attempt

ID: `flail`. **Who:** ATTACKER. **Type:** Optional engineering movement.

**Timing:** ASOP 3.22A. **Rules:** B28.7. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the flail tank attempt a Trail Break in the minefield it will enter?

- Declare the attempt at the required opening window and identify the entry hex.
- Apply flail capability, mine and path rules during movement; do not mark a safe route before resolution.

**Checklist behavior:** Record the declared attempt separately from a successfully created TB.

**Illustrative position:** Following units receive a usable route only after the flail procedure actually creates it.

**Terms:** TB, MP.

#### Resolve opening Heavy Surf beaching checks

ID: `surfStart`. **Who:** ATTACKER. **Type:** Conditional opening procedure.

**Timing:** ASOP 3.22A. **Rules:** G13.442; G13.4423. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this boat or non-Aground LC require an opening (un)Beaching DR?

- Check craft state and Heavy Surf before normal movement.
- Carry grounding, immobilization, broaching and swamping outcomes into later choices.

**Checklist behavior:** A failed opening procedure can change which later actions remain available.

**Illustrative position:** An LC cannot plan a normal landing while an unresolved opening surf result controls its state.

**Terms:** LC, DR.

#### Enter or voluntarily abandon a vehicle

ID: `crewVehicle`. **Who:** ATTACKER. **Type:** Optional crew action.

**Timing:** ASOP 3.22A. **Rules:** D5.41-.43; D5.411. **PDF pages:** 679, 204. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should this unit enter the vehicle, or should its crew abandon it?

- Verify start-of-ITS-MPh position, all-MF cost, prior movement/fire and same-Player-Turn re-entry restrictions.
- Offer permitted weapon removal and the abandoning inherent crew’s self-destruction/malfunction choice.
- Resolve the associated fire window; entry and linked hooking succeed only if their survival conditions are met. Abandonment has its own outcome rules.

**Checklist behavior:** Do not complete entry or hooking merely when MF are paid. Show the different commitments of entering and leaving.

**Illustrative position:** A crew abandoning its vehicle considers removing an eligible weapon and wrecking the vehicle as it leaves.

**Terms:** MF, CE, SW, FFNAM.

### 3.23A · Select aerial movement

#### Start the next aerial sequence

ID: `aerialQueue`. **Who:** ATTACKER. **Type:** Sequence transition.

**Timing:** ASOP 3.23A. **Rules:** ASOP 3.23A; E8; E9. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Have all ground movement sequences finished?

- Route an aerial glider to 3.37D before Evasive Action or landing.
- If there is no aerial glider, use the all-parachute drift procedure at 3.34A; do not process parachutes as ordinary individual ground moves.

**Checklist behavior:** Use a dedicated aerial sequence rather than the ground-unit expenditure loop.

**Illustrative position:** The first glider receives its defender AA window before the landing attempt.

**Terms:** ILH, MPh.

### 3.31A · Resolve a berserk charge

#### Conduct the required charge

ID: `berserkCharge`. **Who:** ATTACKER. **Type:** Compulsory movement.

**Timing:** ASOP 3.31A. **Rules:** A15.43-.431; A15.45; G13.491. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What must this berserk unit do at its next expenditure?

- Apply charge target, route and entry restrictions, including relevant beach exceptions.
- After each qualifying expenditure resolve 3.35D and 3.36D; berserk status does not bypass defensive fire.

**Checklist behavior:** Reevaluate the charge after results instead of executing the whole plotted path at once.

**Illustrative position:** A charge pauses for fire; surviving units continue according to their updated state.

**Terms:** MF, DFF, LOS.

### 3.32A · Routes, costs and exposure

#### Choose route, Location and cost

ID: `route`. **Who:** ATTACKER. **Type:** Optional expenditure.

**Timing:** ASOP 3.32A. **Rules:** A4.1-.3; D2; Chapter B; ASOP 3.32A. **PDF pages:** 679, 47, 48, 195. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which legal route and expenditure should be committed next?

- Calculate actual terrain, elevation, hexside, road, portage and movement-type costs. A road-rate entry can change exposure and TEM.
- Distinguish Location changes from hex changes. Include bridges, gullies, streams, shellholes, crags, railroads and building levels in the full design.
- Display pending terrain rules as not evaluated, not impassable; hidden obstacles must be adjudicated without advance disclosure.

**Checklist behavior:** The completed Rules implementation supplies costs and exposure; the component explains the resulting choices.

**Illustrative position:** A squad can compare a road-rate entry with entry through woods, including the different defensive-fire treatment.

**Terms:** MF, MP, PP, FFMO.

#### Consider a Minimum Move

ID: `minimum`. **Who:** ATTACKER. **Type:** Optional special expenditure.

**Timing:** ASOP 3.32A. **Rules:** A4.134. **PDF pages:** 679, 48, 49. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this Infantry make a one-hex Minimum Move?

- Retain at least one MF after excess portage deductions and satisfy the rule’s exclusions, including pushing a Gun or loading/unloading.
- After entry and all First Fire, unbroken survivors become pinned and CX. Do not apply those states before the fire sequence.

**Checklist behavior:** Show both the reachable hex and the resulting Pin/CX commitment.

**Illustrative position:** A unit lacking the full entry cost may still qualify, but the move does not leave it ready for unrestricted follow-on action.

**Terms:** Minimum Move, MF, CX.

#### Choose Infantry Bypass or VBM

ID: `bypass`. **Who:** ATTACKER. **Type:** Optional route mode.

**Timing:** ASOP 3.32A. **Rules:** A4.3-.34; D2.3. **PDF pages:** 679, 49, 50. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is there sufficient clearance, and which side of the obstacle is being bypassed?

- Use unit-specific clearance, terrain and entry restrictions. Infantry and vehicle Bypass are not interchangeable.
- Preserve route vertices/hexsides and facing for LOS and attacks, including applicable mine and SMOKE effects.
- Do not allow an ordinary voluntary Infantry end in Bypass; handle the specific broken-unit and vehicle ending rules.

**Checklist behavior:** Expose route geometry without presenting an unavailable LOS evaluator as a safe route.

**Illustrative position:** Two routes through one woods hex may create different firing opportunities even though the destination hex is the same.

**Terms:** Bypass, VBM, VCA, TCA.

#### Review special terrain and fortification movement

ID: `terrainModes`. **Who:** ATTACKER. **Type:** Conditional route family.

**Timing:** ASOP 3.32A. **Rules:** B6; B8; B9; B11; B20-B23; B26-B30; E3; G2-G5; G9; G11; Chapter F. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this route require a special terrain procedure?

- Include tunnels, cliffs, Crest status, water/ice, rowhouses, Factories, fortified buildings, entrenchments, Wire, mines, pillboxes and roadblocks.
- Include desert terrain, jungle, bamboo, huts, kunai, rice paddies, Panji and caves as explicit pending families; use scenario and equipment overlays.
- Select the exact rule before committing. This family record is an index to detailed route rules, not evidence that every subcase has been adjudicated.

**Checklist behavior:** Keep all named families discoverable; an unsupported cost needs an explanation and reference, not removal from the catalog.

**Illustrative position:** A cave entry and a normal building entry both remain findable, but require different Rules projections.

**Terms:** MF, MP, WA, TB.

#### Choose Start, Stop, Delay, Reverse or facing expenditure

ID: `vehicleMotion`. **Who:** ATTACKER. **Type:** Optional vehicle expenditure.

**Timing:** ASOP 3.32A. **Rules:** D2.1-.3. **PDF pages:** 679, 59, 195. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What vehicle expenditure is intended now?

- Price each Start, Stop, Delay, Reverse, facing and route action under the applicable rule.
- Give the defender each appropriate expenditure window before the next action; do not combine Stop and unload to suppress a response.
- Track remaining MP and the consequences of ending movement, including assumed expenditures where required.

**Checklist behavior:** The movement ledger must describe expenditures, not just a line of visited hexes.

**Illustrative position:** A tank Stops before unloading; the defender’s Stop-MP response occurs before unloading is announced.

**Terms:** MP, Motion, Non-Stopped, VCA, TCA.

#### Resolve Mechanical Reliability, Stall and Bog Removal

ID: `reliability`. **Who:** ATTACKER. **Type:** Conditional prerequisite.

**Timing:** ASOP 3.32A. **Rules:** D2.51; D8.3; G12.211-.212. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this movement need a prerequisite check?

- Check vehicle-specific Mechanical Reliability/Stall triggers and Bog Removal conditions before continuation.
- Keep Bog Removal distinct from a new terrain-entry Bog check; include LC Aground procedures where applicable.

**Checklist behavior:** A failed check changes capability and stops stale route execution.

**Illustrative position:** A Bogged vehicle cannot spend an ordinary movement entry while its removal result is unresolved.

**Terms:** MP, DR.

#### Resolve sewer travel and Lost results

ID: `sewerTravel`. **Who:** ATTACKER. **Type:** Conditional travel procedure.

**Timing:** ASOP 3.32A. **Rules:** B8.41. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Has this sewer move reached a required roll?

- Perform Sewer dr and any required Lost dr at their prescribed timing.
- Update the actual route privately and preserve the separate emergence procedure.

**Checklist behavior:** Do not expose the concealed sewer route through counts or warnings.

**Illustrative position:** A failed route result cannot be replaced by clicking the intended destination again.

**Terms:** MF, Straying.

#### Resolve Falling, Climb progress and Swimming checks

ID: `climbSwim`. **Who:** ATTACKER. **Type:** Required special-movement effects.

**Timing:** ASOP 3.32A. **Rules:** B11.41; B11.43; E6.1; E6.5. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** What consequences does this special movement trigger?

- Resolve any Falling DR and place the correct Climb counter after a successful ascent/descent.
- Resolve required Swimming TC/MC; distinguish movement completion from surviving its checks.

**Checklist behavior:** Keep required effects attached to their originating expenditure.

**Illustrative position:** A successful climb changes the progress counter, while a Falling result requires resolution before another move.

**Terms:** TC, MC, MF.

#### Declare Double Time after starting

ID: `lateDouble`. **Who:** ATTACKER. **Type:** Optional late declaration.

**Timing:** ASOP 3.32A. **Rules:** A4.5. **PDF pages:** 679, 50, 51. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Would one additional MF help now?

- Verify that a late declaration remains legal for this unit and movement mode.
- Gain one MF, not the opening two, and apply CX without undoing previous expenditures.

**Checklist behavior:** Refresh remaining options and later-phase consequences.

**Illustrative position:** A squad one MF short of its revised destination considers late Double Time.

**Terms:** Double Time, CX, MF.

#### Resolve concealment, detection and occupied entry

ID: `concealment`. **Who:** BOTH. **Type:** Conditional disclosure procedure.

**Timing:** ASOP 3.32A. **Rules:** A12.14-.15; A12.2; A12.33; A12.41-.42; A4.15. **PDF pages:** 679, 79, 80. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this expenditure require concealment or entry adjudication?

- Use the viewing side’s lawful information and the proper concealed/HIP reveal procedure.
- Preserve voluntary choices where rules allow them, and handle forced-back or Infantry OVR branches at the correct point.
- Resolve required PAATC and vehicle-ending detection separately; do not expose a hidden defender through route warnings.

**Checklist behavior:** The UI receives only the disclosed outcome, not an omniscient eligibility list.

**Illustrative position:** An attempted entry reveals a concealed SMC; the resulting OVR decision is handled before normal movement resumes.

**Terms:** LOS, OVR, PAATC.

#### Claim or relinquish Wall Advantage when allowed

ID: `wallAdvantage`. **Who:** ATTACKER. **Type:** Conditional position choice.

**Timing:** ASOP 3.32A. **Rules:** B9.32-.322. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this unit claim Wall Advantage at this point?

- Check claim timing, competing enemy claims and mandatory WA conditions.
- Respect firing, movement and loss/reclaim restrictions; do not make WA a freely reversible display toggle.

**Checklist behavior:** Update protection and related fire choices only after the legal state change.

**Illustrative position:** A unit reaching a wall reviews its claim while the expenditure and opposing position are current.

**Terms:** WA, TEM.

### 3.32A · Equipment, transport and labor

#### Recover a SW or Gun

ID: `recover`. **Who:** ATTACKER. **Type:** Optional expenditure.

**Timing:** ASOP 3.32A. **Rules:** A4.44; E4.21. **PDF pages:** 679, 50. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should this unit spend movement to attempt Recovery?

- Check possession, Location, required MF and Recovery conditions; a failed attempt still has its prescribed expenditure.
- Resolve the associated response window and any Ski-use dr. Update portage without refunding spent MF.

**Checklist behavior:** Give each Recovery its own expenditure identity.

**Illustrative position:** The squad enters a hex, survives the entry response, then attempts to Recover the abandoned LMG.

**Terms:** SW, MF, PP.

#### Drop a weapon or release prisoners

ID: `dropDuring`. **Who:** ATTACKER. **Type:** Optional possession change.

**Timing:** ASOP 3.32A. **Rules:** A4.43; A20.53. **PDF pages:** 679, 50, 87. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What should be left at this Location?

- Distinguish dropping a SW/Gun from releasing prisoners and follow the applicable ownership/guard rules.
- Do not refund already lost movement allowance when dropping excess portage during the move.
- Source tension: ASOP 3.32A lists dropping prisoners, while A20.53 sends voluntary abandonment to A20.5, which lists friendly RPh/APh. Keep the listed option visible, but require a resolved rule interpretation before enabling MPh abandonment.

**Checklist behavior:** Leave an explicit position and possession record. Do not infer executable permission from this unresolved source tension.

**Illustrative position:** Dropping a heavy weapon mid-route does not erase the cost already paid for carrying it.

**Terms:** SW, PP, MF.

#### Move Guards and prisoners, or escort prisoners offboard

ID: `prisonerMovement`. **Who:** ATTACKER. **Type:** Conditional combined movement.

**Timing:** ASOP 3.32A. **Rules:** A20.5-.54. **PDF pages:** 679, 87, 88. **Coverage:** Rules integration pending; reference available.

**Consider:** How must the Guard and prisoners move together?

- Move prisoners as one combined stack at their Guard’s foot rate under the captor’s direction. Apply guarding capacity, weapon-use and transport capacity restrictions.
- Consider a legal Friendly Board Edge escort exit and its victory-condition treatment; an ordinary edge is not automatically Friendly.
- Resolve fire against both Guards and prisoners using the special casualty/Pin rules, and handle replacement Guards or freed Unarmed units when a Guard is eliminated.

**Checklist behavior:** Track Guard identity and prisoner capacity through movement and casualties.

**Illustrative position:** Mounted transport must have room for the prisoners as well as the Guard; a spare seat for the Guard alone is insufficient.

**Terms:** MF, US#, PRC.

#### Enter an Unarmed unit’s Location to recapture it

ID: `recapture`. **Who:** ATTACKER. **Type:** Optional immediate combat on entry.

**Timing:** ASOP 3.32A. **Rules:** A20.54. **PDF pages:** 679, 88. **Coverage:** Rules integration pending; reference available.

**Consider:** Should eligible Infantry or Cavalry enter to attempt recapture?

- Unarmed units do not bar movement; entering Infantry/Cavalry can engage them in immediate CC during the MPh.
- If the entry attack fails to eliminate or recapture them, place Melee and allow no further movement. This is neither ordinary Infantry OVR nor a free capture.

**Checklist behavior:** Open the immediate combat consequence before continuing the route.

**Illustrative position:** A squad enters a freed Unarmed unit’s hex; an unsuccessful recapture leaves the units in Melee.

**Terms:** CC, MF, OVR.

#### Manhandle a Gun or boat

ID: `manhandle`. **Who:** ATTACKER. **Type:** Optional movement procedure.

**Timing:** ASOP 3.32A. **Rules:** C10.3; E5.2. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can the crew or Personnel move this equipment by hand?

- Check pushing eligibility, route and modifiers; do not assume ordinary Infantry entry eligibility is sufficient.
- Resolve Manhandling DR, TI and Labor on failure as prescribed, including the applicable defensive-fire exposure.

**Checklist behavior:** Track the equipment and pushing units as a linked action.

**Illustrative position:** A crew considers pushing its Gun into cover; a failed attempt still creates the relevant commitments.

**Terms:** DR, TI, Hazardous Movement.

#### Place a demolition charge

ID: `placeDC`. **Who:** ATTACKER. **Type:** Optional expenditure with later detonation.

**Timing:** ASOP 3.32A. **Rules:** A23.3-.4; C7.346. **PDF pages:** 679, 90. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can the carrier operably Place the DC and survive the response?

- Check ADJACENT target, LOS, movement/fire restrictions and any PAATC. Pay the placement MF in the carrier’s Location.
- The carrier must survive the relevant defensive fire unbroken and unpinned for operable Placement.
- A successfully Placed DC waits for its AFPh resolution; it does not explode merely because placement MF were spent.

**Checklist behavior:** Track declared, operably Placed and detonated as different states.

**Illustrative position:** A squad Places from cover. If pinned during the placement response, the DC is not operably Placed.

**Terms:** DC, MF, PAATC, AFPh.

#### Set a demolition charge

ID: `setDC`. **Who:** ATTACKER. **Type:** Optional special preparation.

**Timing:** ASOP 3.32A. **Rules:** A23.7. **PDF pages:** 679, 91. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the unit Set this DC for later use?

- Spend all MF using Hazardous Movement to Set it in the unit’s own Location; resolve the dr against the unit’s US#, with permitted assisting-leader drm.
- The unit must avoid Pin/break for successful setting. Retain setting-unit identity and the later-Player-Turn, distance, LOS and NTC conditions for detonation.

**Checklist behavior:** Create the later-use record without performing its eventual detonation.

**Illustrative position:** A Set DC is not scheduled to explode automatically with an ordinary Placed DC in AFPh.

**Terms:** DC, US#.

#### Mount, load, dismount or unload

ID: `mount`. **Who:** ATTACKER. **Type:** Optional transport expenditure.

**Timing:** ASOP 3.32A. **Rules:** A13.31; D6.4-.5; D12.2; D15.41; G12.4-.45; G13.443. **PDF pages:** 679, 204, 205. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which transport change is intended, and who pays movement?

- Validate horse, vehicle, wagon, motorcycle or LC capacity, passenger/rider type, Location and movement state.
- Account for both transport and Personnel expenditure and exposure. Resolve the loading/unloading survival rules.
- Calculate remaining movement and fire restrictions from the transport-specific rule, not a generic four-MF reset.

**Checklist behavior:** Show both participants and their remaining capabilities.

**Illustrative position:** An unloading squad and its transport may have different remaining movement and fire options.

**Terms:** PRC, MF, MP, FFNAM.

#### Hook or unhook a Gun

ID: `hook`. **Who:** ATTACKER. **Type:** Optional towing action.

**Timing:** ASOP 3.32A. **Rules:** C10.11-.12; D5.43. **PDF pages:** 679, 204. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should the Gun be attached to or removed from its towing vehicle?

- Check towing capability and legal Personnel/vehicle positions, then the linked expenditure and response.
- Hooking makes Gun, Personnel and vehicle TI; unhooking makes Gun and Infantry TI under the cited procedure.
- Honor survival requirements for simultaneous crew entry/exit and hooking; towing route restrictions continue afterward.

**Checklist behavior:** Use a linked action record rather than changing only the Gun’s displayed carrier.

**Illustrative position:** A crew abandoning its vehicle while unhooking must satisfy the relevant fire-survival conditions.

**Terms:** TI, MF, MP.

#### Declare Clearance work

ID: `clearDeclare`. **Who:** ATTACKER. **Type:** Optional commitment with later resolution.

**Timing:** ASOP 3.32A. **Rules:** B24.71; B24.73-.76; G2.7; G9.71; O1.5. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this unit commit to Clearance?

- Represent rubble, Wire, mines, Set DC, roadblock, Path, Panji covered hexside and non-Factory Debris as separate selectable targets.
- Apply TI and the target-specific declaration and later-resolution procedure. Do not remove the obstacle on declaration.
- O1.5 is a named external-source gap in this volume; its detailed Debris conditions remain unevaluated.

**Checklist behavior:** Create a pending work item linked to its actual resolution window.

**Illustrative position:** A squad declares Wire Clearance now; the wire remains until the clearance procedure succeeds.

**Terms:** TI, DC, TB.

#### Attempt to extinguish Flame

ID: `clearFlame`. **Who:** ATTACKER. **Type:** Optional immediate labor attempt.

**Timing:** ASOP 3.32A. **Rules:** B24.72-.721; B25.15-.151. **PDF pages:** 679, 142, 143. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this unit attempt Flame Clearance now?

- Check unit, Location and Flame conditions, then resolve the attempt.
- Apply TI and task-specific Labor if unsuccessful. A Final Fire Clearance DR of 3-6 Hampers the Flame: mark it PIN so it cannot become Blaze during this Player Turn. A new Flame also uses PIN, but has a different origin. This immediate attempt is distinct from non-Flame work declared for CCPh.

**Checklist behavior:** Show extinguished, Hampered, or still-active Flame, and carry the correct conversion eligibility into AFPh. Consideration does not resolve the task.

**Illustrative position:** A failed attempt leaves a Labor counter and a still-present Flame.

**Terms:** TI, DR.

#### Don or remove skis

ID: `skis`. **Who:** ATTACKER. **Type:** Optional equipment-mode choice.

**Timing:** ASOP 3.32A. **Rules:** E4.2; E4.21. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should this unit change ski use?

- Check the timing, movement cost and unit conditions in E4.2.
- Carry ski status into movement, Recovery and combat effects; perform the Recovery-related Ski-use dr when called for.

**Checklist behavior:** Keep equipment state and movement state linked.

**Illustrative position:** A unit changes ski use for the next terrain segment rather than receiving ordinary walking costs automatically.

**Terms:** MF, SW.

### 3.32A · Smoke, vehicles and attacks

#### Place Smoke grenades or use a dispenser

ID: `smoke`. **Who:** ATTACKER. **Type:** Optional protection expenditure.

**Timing:** ASOP 3.32A. **Rules:** A24.1; D13; D13.35. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Would Smoke help, and can this unit attempt it now?

- Separate Infantry grenades, vehicle grenades and dispenser types, including their usage limits and expenditure.
- Resolve the usage result before assuming a Hindrance; apply WP NMC and possible Flame when triggered.
- A failed attempt still follows its own cost and usage rules. Later smoke does not undo an earlier fire opportunity.

**Checklist behavior:** Place smoke only after successful resolution and refresh subsequent LOS/exposure.

**Illustrative position:** A squad attempts Smoke before crossing; the guide does not promise a protected crossing before the roll.

**Terms:** SMOKE, MF, MP, WP.

#### Attempt Infantry Overrun

ID: `infantryOVR`. **Who:** ATTACKER. **Type:** Optional occupied-entry procedure.

**Timing:** ASOP 3.32A. **Rules:** A4.15-.152. **PDF pages:** 679, 49. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is this an eligible Infantry OVR situation?

- Use the Infantry target, unit, NTC and entry conditions, not the vehicular OVR procedure.
- Handle concealed reveals, additional defenders and resulting forced-back or continuation decisions in source order.

**Checklist behavior:** Keep the initial move, check and resulting entry branch traceable.

**Illustrative position:** A lone enemy SMC may create an Infantry OVR choice that a normal occupied-entry rule would otherwise reject.

**Terms:** OVR, NTC, MF.

#### Create a Tank-Hunter or DC Hero

ID: `hero`. **Who:** ATTACKER. **Type:** Optional nationality-specific action.

**Timing:** ASOP 3.32A. **Rules:** G1.421; G1.424. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this Japanese squad or HS create the special hero now?

- For the movement attempt, verify the enemy AFV in LOS within eight MF and the governing creation restrictions.
- Distinguish DC Hero conditions, equipment and compulsory follow-up from ordinary Tank-Hunter Hero creation.

**Checklist behavior:** Queue the created hero’s procedure instead of granting generic hero movement.

**Illustrative position:** A Japanese squad considers the attempt against a visible nearby tank; the reference remains present even when unimplemented.

**Terms:** T-H Hero, DC Hero, MF, LOS.

#### Declare a late Gallop or Cavalry Charge

ID: `cavalry`. **Who:** ATTACKER. **Type:** Optional mounted action.

**Timing:** ASOP 3.32A. **Rules:** A13.36; A13.6. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the Cavalry accelerate or commit to a Charge?

- A legal later Gallop adds four MF and requires the horse not be CX.
- Declare a Charge while at least three hexes away and in LOS of its target; resolve the charge in the target Location.

**Checklist behavior:** Keep Gallop and Charge as distinct choices with linked restrictions.

**Illustrative position:** A rider cannot wait until adjacent to announce the Charge option listed for three or more hexes away.

**Terms:** MF, CX, LOS.

#### Resolve entry Bog checks

ID: `bog`. **Who:** ATTACKER. **Type:** Required terrain consequence.

**Timing:** ASOP 3.32A. **Rules:** D8.2-.23. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does the terrain entry or maneuver require Bog?

- Apply vehicle, terrain and entry modifiers at the prescribed point.
- Update Bog/immobilization and any already-declared attack procedure before offering more movement.

**Checklist behavior:** Do not discard a declared OVR merely because a Bog result occurred; use the OVR consequences.

**Illustrative position:** A tank declaring an OVR into difficult terrain must settle Bog before resolving that attack.

**Terms:** DR, MP.

#### Attempt the required Recall exit

ID: `recall`. **Who:** ATTACKER. **Type:** Compulsory movement goal.

**Timing:** ASOP 3.32A. **Rules:** D5.341; G14.232; G14.33. **PDF pages:** 679, 203. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does Recall require this vehicle to leave?

- Use the Recall route and movement obligations rather than offering unrestricted tactical movement.
- Include landing-craft exceptions and any inability to move or exit under the governing rules.

**Checklist behavior:** Show the required exit task and its current impediments.

**Illustrative position:** A Recalled vehicle is not treated as an optional unmoved unit at phase end.

**Terms:** MP, LC.

#### Use Bounding First Fire

ID: `bounding`. **Who:** ATTACKER. **Type:** Optional vehicle fire.

**Timing:** ASOP 3.32A. **Rules:** C5.3; C8.6; D3.3; G12.5. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should this vehicle fire now or retain its movement/fire choices?

- Check weapon and PRC eligibility, movement status, CA, ammunition, acquisition and applicable bounding-fire modifiers.
- Track each weapon’s use, ROF and later-fire commitment; include C8.6 Smoke/WP timing exceptions.
- Resolve results and any required consequences before continuing movement.

**Checklist behavior:** Keep an attack tied to the current expenditure state rather than ending the vehicle’s move automatically.

**Illustrative position:** A tank pauses to fire its MA, then checks whether movement and any additional weapon use remain legal.

**Terms:** BFF, ROF, TH, MP.

#### Consider a Gun Duel response

ID: `gunDuel`. **Who:** ATTACKER. **Type:** Conditional attack-order choice.

**Timing:** ASOP 3.32A. **Rules:** C2.2401; D3.3. **PDF pages:** 679, 167, 168. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this vehicle resolve an eligible shot before the declared defender attack?

- The declared opponent must satisfy the rule’s non-concealed, non-aerial conditions; check CA and fire eligibility.
- Compare the required Gun Duel modifiers and honor the OVR exclusion; do not offer a generic interrupt against every shot.

**Checklist behavior:** Resolve attack order through Rules, preserving the initiating defender attack if still applicable.

**Illustrative position:** A tank named as a DFF target considers a legal BFF Gun Duel before the defender rolls.

**Terms:** Gun Duel, BFF, CA.

#### Declare and resolve a vehicular Overrun

ID: `vehicleOVR`. **Who:** ATTACKER. **Type:** Optional combined movement and attack.

**Timing:** ASOP 3.32A. **Rules:** D7.1; D7.2. **PDF pages:** 679, 207. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should the vehicle Overrun this Location?

- Declare at entry as required; combine entry MP with the OVR cost of one quarter printed MP, rounded up, subject to exceptions.
- Resolve applicable Bog and ordinary DFF before the OVR, then Reaction Fire after it. The OVR vehicle cannot invoke a Gun Duel.
- Immobilization or destruction does not automatically cancel the OVR; use the applicable reduced attack and PRC consequences.

**Checklist behavior:** Display the distinct before-OVR and after-OVR windows.

**Illustrative position:** A defender’s shot immobilizes the attacking tank; the guide still resolves the OVR consequences that the rules require.

**Terms:** OVR, MP, Reaction Fire.

#### Attempt ESB or a Hull Down maneuver

ID: `esbHD`. **Who:** ATTACKER. **Type:** Optional vehicle maneuver.

**Timing:** ASOP 3.32A. **Rules:** D2.5; D4.22. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is extra speed or a Hull Down position worth the commitment?

- Present ESB and HD as separate actions, with their eligibility, MP and roll requirements.
- Resolve breakdown or maneuver failure before calculating subsequent capability; nationality and vehicle data matter.

**Checklist behavior:** Preview the cost and possible movement loss without inventing success probabilities.

**Illustrative position:** A tank short of its intended destination can consider ESB, while a different position may permit an HD maneuver.

**Terms:** ESB, HD, MP.

#### Change CE or BU posture

ID: `crewPosture`. **Who:** ATTACKER. **Type:** Optional posture choice.

**Timing:** ASOP 3.32A. **Rules:** D5.33. **PDF pages:** 679, 203. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can the crew change posture at this exact moment?

- Do not both voluntarily place and remove CE in the same phase.
- Do not change after the vehicle/PRC’s Prep or Bounding First Fire, or between being named a target and resolution of all fire allowed by the last MP expenditure.
- Apply linked passenger and crew exceptions; the posture change alone is not movement for moving-target/firer penalties.

**Checklist behavior:** Expose the timing restriction before a shot rather than allowing a reactive BU toggle after target declaration.

**Illustrative position:** A CE tank named as a target cannot become BU while the current fire window remains unresolved.

**Terms:** CE, BU, BFF.

#### Move Infantry with an AFV in Armored Assault

ID: `armoredAssault`. **Who:** ATTACKER. **Type:** Optional linked movement.

**Timing:** ASOP 3.32A. **Rules:** D9.31. **PDF pages:** 679, 209, 210. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should the Infantry and AFV move together?

- Begin with the Infantry beneath the AFV and apply the combined movement requirements.
- Limit how far the AFV may move by the accompanying Infantry’s movement potential even if they later separate.
- Apply protection to the actual combined move; it does not eliminate FFNAM unless Assault Movement applies.

**Checklist behavior:** Track a shared route segment and independent consequences after separation.

**Illustrative position:** The squad leaves the tank to enter a building; the tank’s remaining route still respects the Armored Assault distance constraint.

**Terms:** AFV, MF, MP, FFNAM, FFMO.

#### Declare Wreck Removal

ID: `wreckRemoval`. **Who:** ATTACKER. **Type:** Optional work with phase-end resolution.

**Timing:** ASOP 3.32A. **Rules:** D10.42. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this vehicle undertake Wreck Removal?

- Check the vehicle, wreck, location and commitment required by D10.42.
- Record the declaration now and resolve the result at 3.51A, rather than deleting the wreck immediately.

**Checklist behavior:** Maintain a pending phase-end work item.

**Illustrative position:** The wreck remains on the map while the declared removal procedure awaits its proper resolution.

**Terms:** MP, TI.

#### Resolve wagon or motorcycle Wreck Checks

ID: `wreckChecks`. **Who:** ATTACKER. **Type:** Conditional movement consequence.

**Timing:** ASOP 3.32A. **Rules:** D12.4; D15.46. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Has this movement triggered a Wreck Check?

- Use the specific wagon or motorcycle trigger and modifiers.
- Resolve the check and its passenger/equipment consequences before continuing.

**Checklist behavior:** Do not substitute a tank Bog check for these different procedures.

**Illustrative position:** A motorcycle’s hazardous movement segment may call for its own Wreck Check.

**Terms:** Wreck Check, MF, MP.

### 3.32A · Impulses, terrain and amphibious effects

#### Complete the impulse before First Fire

ID: `impulse`. **Who:** ATTACKER. **Type:** Group expenditure requirement.

**Timing:** ASOP 3.32A. **Rules:** ASOP 3.32A; A25.23; D14.2; E11; G1.5. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Have all participating units made this impulse’s expenditures?

- Commit the prescribed group expenditures before the defensive-fire procedure.
- Preserve each participant’s cost, position and eligibility when resolving attacks and consequences.

**Checklist behavior:** The response window identifies the impulse as well as the affected target.

**Illustrative position:** The defender cannot interrupt halfway through a group’s required simultaneous expenditure by using ordinary single-unit sequencing.

**Terms:** Impulse, MF, MP.

#### Check Column Disbandment

ID: `column`. **Who:** BOTH. **Type:** Conditional formation consequence.

**Timing:** ASOP 3.32A. **Rules:** E11.531-.532. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Has movement or fire triggered Column Disbandment?

- Check the movement triggers at 3.32A and the residual/fire triggers at 3.35D and 3.36D.
- Carry the changed formation and movement state into subsequent expenditure choices.

**Checklist behavior:** One acknowledgment cannot clear later independent disbandment triggers.

**Illustrative position:** The column remains intact after one move, but a later attack creates a new check.

**Terms:** DFF, Impulse.

#### Resolve a Panji movement MC

ID: `panji`. **Who:** ATTACKER. **Type:** Conditional terrain consequence.

**Timing:** ASOP 3.32A. **Rules:** G9.41. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this entry or crossing trigger Panji effects?

- Use the actual Panji terrain and movement conditions.
- Resolve the MC and resulting state before further movement; preserve hidden obstacle disclosure rules.

**Checklist behavior:** Show an encountered effect only when its disclosure is authorized.

**Illustrative position:** The UI must not warn about an undiscovered Panji feature before the normal encounter procedure.

**Terms:** MC, MF.

#### Create Vehicle Dust when required

ID: `dust`. **Who:** ATTACKER. **Type:** Conditional environmental effect.

**Timing:** ASOP 3.32A. **Rules:** F11.74-.741. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this vehicle expenditure create Dust?

- Check the applicable desert/environmental conditions and vehicle movement.
- Place and update dust by its rule, then reevaluate subsequent LOS and Hindrances.

**Checklist behavior:** Dust is an expenditure consequence, not an optional cosmetic trail.

**Illustrative position:** The tank’s movement may change a later shot’s visibility even if no smoke was fired.

**Terms:** MP, LOS.

#### Drop DD tank screens

ID: `ddScreens`. **Who:** ATTACKER. **Type:** Optional amphibious configuration.

**Timing:** ASOP 3.32A. **Rules:** D16.11. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Should the DD tank lower its screens now?

- Check the permitted conditions and resulting movement/fire restrictions.
- Record the configuration change with its rule consequences rather than treating it as CE/BU.

**Checklist behavior:** Keep screen state distinct from crew posture.

**Illustrative position:** An amphibious tank reaching the landing area reviews its DD configuration.

**Terms:** DD, MP.

#### Beach, unbeach or resolve Aground

ID: `beach`. **Who:** ATTACKER. **Type:** Amphibious movement procedure.

**Timing:** ASOP 3.32A. **Rules:** E5.23; G12.3; G12.21; G13.446. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** What landing or departure procedure applies here?

- Separate boats from LC and legal Beach/Unbeach choices from required Aground checks.
- Use the current beach, water, tide and craft state; include corresponding transport restrictions.

**Checklist behavior:** A beached, aground or mobile craft has different next choices.

**Illustrative position:** An LC that runs Aground cannot continue as though it merely paid an ordinary entry cost.

**Terms:** LC, MP.

#### Resolve Swamping, Heavy Surf and Wading CX

ID: `surf`. **Who:** ATTACKER. **Type:** Conditional amphibious effects.

**Timing:** ASOP 3.32A. **Rules:** G13.4222; G13.44-.4423; G13.447. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Which surf effects follow this expenditure?

- Resolve applicable Swamping, Immobilization, (un)Beaching and Broaching effects.
- Infantry/Cavalry Wading in Heavy Surf become CX as prescribed. Keep the craft and its contents’ outcomes linked.

**Checklist behavior:** Carry the new state into fire, transport and remaining movement decisions.

**Illustrative position:** Troops Wading in Heavy Surf acquire CX even without choosing Double Time.

**Terms:** LC, CX, MF.

### 3.33A-3.34A · Aerial movement

#### Take required glider Evasive Action

ID: `gliderEvade`. **Who:** ATTACKER. **Type:** Conditional aerial procedure.

**Timing:** ASOP 3.33A. **Rules:** E8.211. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the last AA result require Evasive Action?

- Resolve the required action and resulting aerial position.
- Return to 3.37D for the next applicable AA opportunity; do not proceed directly to landing after every shot.

**Checklist behavior:** Represent the glider’s repeated AA/evasion sequence explicitly.

**Illustrative position:** An AA result changes the landing approach and creates the next source-defined aerial window.

**Terms:** AA, ILH.

#### Drift all aerial parachutes

ID: `parachuteDrift`. **Who:** ATTACKER. **Type:** Required collective aerial movement.

**Timing:** ASOP 3.34A. **Rules:** E9.2. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Where do all remaining aerial parachutes drift?

- Apply the drift procedure to all aerial parachutes before the 3.38D fire step.
- If none remain aerial, proceed to phase-end work; do not invent a ground MF expenditure.

**Checklist behavior:** Keep aerial position and later landing displacement separate.

**Illustrative position:** All parachutes drift before the defender chooses eligible fire at them.

**Terms:** DR, MPh.

### 3.35D · Effects before ordinary First Fire

#### Resolve existing Residual FP and Fire Lanes

ID: `residualAttack`. **Who:** BOTH. **Type:** Required expenditure consequence.

**Timing:** ASOP 3.35D. **Rules:** A8.22; A9.22. **PDF pages:** 679, 60. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this expenditure trigger an existing residual attack?

- Resolve applicable existing residual before new DFF, against the moving unit/stack only.
- Apply the once-per-Location and increased-vulnerability/strength exceptions; a multi-MF expenditure is not automatically multiple residual attacks.
- Use separate Fire Lane rules and applicable activity/entry timing exceptions.

**Checklist behavior:** Keep a per-target exposure record, not a simple once-per-hex checkbox.

**Illustrative position:** A unit already attacked by residual may face another qualifying attack after its vulnerability increases and it spends more MF.

**Terms:** Residual FP, Fire Lane, MF, MP.

#### Resolve FFE attacks on moving units

ID: `ffeMove`. **Who:** BOTH. **Type:** Required expenditure consequence.

**Timing:** ASOP 3.35D. **Rules:** A24.31; C1.51-.53; C1.55-.56; C1.72; C1.9; G12.5; G14.65-.661. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the current move trigger an existing FFE effect?

- Use the FFE type, expenditure and location rules; include WP, rockets, LC and NOBA/Reverse Slope exceptions.
- Attack the qualifying moving unit/stack, not every stationary unit sharing the Location on each expenditure.

**Checklist behavior:** This resolves an existing mission’s effect; it is not an unrestricted new OBA mission window.

**Illustrative position:** A moving squad enters an FFE area while a stationary squad is already there; their exposure histories are distinct.

**Terms:** FFE, WP, NMC.

#### Resolve minefield attacks

ID: `mines`. **Who:** BOTH. **Type:** Required expenditure consequence.

**Timing:** ASOP 3.35D. **Rules:** B28.41-.412; B28.42-.52; B28.531; G14.53. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Has the expenditure triggered a mine attack?

- Apply AP/AT mine, entry/exit, known route and clearance/TB exceptions as appropriate.
- Disclose mine information only through the rule’s encounter procedure; do not expose hidden fields in route suggestions.

**Checklist behavior:** Attach the attack and discovered information to this movement event.

**Illustrative position:** The player sees a mine attack when it occurs, not an advance warning based on hidden scenario data.

**Terms:** MF, MP, TB.

#### Attempt later defender illumination

ID: `starshellLater`. **Who:** DEFENDER. **Type:** Conditional response choice.

**Timing:** ASOP 3.35D. **Rules:** E1.921. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Is a later Starshell attempt permitted now?

- Check leader eligibility and the special MMC/CE AFV condition when a Starshell/IR has been fired in no previous Player Turn.
- Resolve illumination before subsequent attacks that use the changed visibility.

**Checklist behavior:** Do not carry the broader opening opportunity into every later response without checking its conditions.

**Illustrative position:** A later movement expenditure may offer a leader a Starshell attempt without giving every MMC the same right.

**Terms:** NVR, IR, CE.

#### Resolve trip-flare activation

ID: `tripFlare`. **Who:** BOTH. **Type:** Conditional movement effect.

**Timing:** ASOP 3.35D. **Rules:** G.8; ASOP 3.35D. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the move activate a trip flare?

- Check the triggering condition and resolve the resulting illumination and disclosure.
- Keep this automatic effect distinct from an optional Starshell attempt.

**Checklist behavior:** Refresh the current response’s visibility after activation.

**Illustrative position:** The defender’s possible fire changes after the movement activates illumination.

**Terms:** NVR, LOS.

### 3.36D · Defender fire and response choices

#### Fire at the current expenditure, or pass

ID: `firstFire`. **Who:** DEFENDER. **Type:** Optional response.

**Timing:** ASOP 3.36D. **Rules:** A8.1-.14. **PDF pages:** 679, 59. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Should this defender fire at the current mover now?

- Target only the qualifying moving unit/stack, using its route, expenditure and state. A stationary co-occupant is not automatically a target.
- Limit repeated shots by MF/MP expenditure even when ROF is retained; a one-MF expenditure is not unlimited fire.
- Apply range, LOS, weapon use, fire-group, leadership, cowering and ammunition rules. Passing this window does not pass later ones.

**Checklist behavior:** Keep the shot allowance for this expenditure separate from remaining weapon capability.

**Illustrative position:** A retained-ROF MG may still have to wait for a later expenditure before firing again at this mover.

**Terms:** DFF, MF, MP, ROF.

#### Use Subsequent First Fire

ID: `subsequent`. **Who:** DEFENDER. **Type:** Restricted additional fire.

**Timing:** ASOP 3.36D. **Rules:** A8.3. **PDF pages:** 679, 61. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Can this First-Fired unit use SFF against the mover?

- Check eligible small-arms/MG/IFE use, Normal Range and the nearest Known armed enemy distance restriction.
- Apply Area Fire and relevant Sustained Fire rules; use or forfeit the usable MG/IFE capability as A8.3 requires.
- Place Final Fire markers on the unit and its weapons appropriately.

**Checklist behavior:** Show what this choice gives up for later defensive opportunities.

**Illustrative position:** A First-Fired squad cannot use SFF at a farther target merely because that target is moving.

**Terms:** SFF, MG, IFE, Sustained Fire.

#### Use Final Protective Fire

ID: `fpf`. **Who:** DEFENDER. **Type:** Optional high-risk fire.

**Timing:** ASOP 3.36D. **Rules:** A8.31-.311. **PDF pages:** 679, 61. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Is the adjacent or same-hex mover worth another FPF attack?

- Check Final-Fired Infantry, target and weapon restrictions; apply the required inherent and MG/IFE use.
- Resolve the attack and use the same Original IFT DR as the firer’s NMC under the rule’s leadership conditions.
- The target’s attack result is not cancelled if the firer breaks. Further FPF requires continued eligibility and available expenditure opportunity.

**Checklist behavior:** Link the defender’s morale consequence to the attack record.

**Illustrative position:** A squad’s FPF breaks the moving enemy but also breaks the firing squad; both outcomes remain.

**Terms:** FPF, NMC, Sustained Fire.

#### Resolve mandatory same-Location fire

ID: `mandatoryTPBF`. **Who:** DEFENDER. **Type:** Compulsory response.

**Timing:** ASOP 3.36D. **Rules:** A8.312. **PDF pages:** 679, 61. **Coverage:** Rules integration pending; reference available.

**Consider:** Does enemy Infantry/Cavalry entry compel an attack?

- After residual, mines and OBA, evaluate unbroken armed Infantry not in Melee and the entering MMC conditions.
- Apply compulsory fire-group and weapon-use rules with their exceptions; this is not an optional Pass window.

**Checklist behavior:** A required-fire obligation blocks closing the response until resolved.

**Illustrative position:** A qualifying MMC enters the defender’s Location; the guide replaces a generic optional-fire prompt with the mandatory procedure.

**Terms:** TPBF, FPF, MG.

#### Take a Snap Shot

ID: `snap`. **Who:** DEFENDER. **Type:** Optional crossing attack.

**Timing:** ASOP 3.36D. **Rules:** A8.15. **PDF pages:** 679, 59. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does the crossed hexside present a legal Snap Shot?

- Require LOS to the entire crossed hexside and eligible small-arms/MG use; exclude ordnance, IFE, Canister and prohibited MG CA changes.
- Apply Area Fire without FFNAM/FFMO, limit it to the qualifying crossing opportunity, and leave no Residual FP.

**Checklist behavior:** Store the crossing geometry so the opportunity cannot be recreated after movement has advanced.

**Illustrative position:** A defender sees the crossed hexside but not the destination center, creating a possible Snap Shot.

**Terms:** DFF, FFNAM, FFMO, Residual FP.

#### Establish or maintain a Fire Lane

ID: `fireLane`. **Who:** DEFENDER. **Type:** Optional MG procedure.

**Timing:** ASOP 3.36D. **Rules:** A9.22-.223. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Would a legal Fire Lane cover useful later movement?

- Check the MG, initial attack, lane geometry and LOS conditions before placement.
- Apply Fire Lane attacks and cancellation independently from ordinary Residual FP, including any cancellation from CC Reaction Fire.

**Checklist behavior:** Show the lane’s future coverage without claiming guaranteed targets.

**Illustrative position:** A defender trades the relevant MG capability for a lane that may affect later movers.

**Terms:** MG, Fire Lane, ROF, Residual FP.

#### Review eligible weapons and attack modes

ID: `defenderWeapons`. **Who:** DEFENDER. **Type:** Fire-option family.

**Timing:** ASOP 3.36D. **Rules:** A7-A9; A22; A23.6; C2-C9; C13; D3. **PDF pages:** 679. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which legal weapon and attack mode fits this response?

- Include inherent fire, MG, FT, MOL, LATW, ordnance/IFE, permitted special ammunition, Spraying Fire and fire-group/direction choices.
- Apply actual first/subsequent/final-fire and target restrictions; Intensive Fire is not a blanket right for every weapon or response.
- Recheck ROF, malfunctions, acquisition, ammo depletion and result chains for each shot, including cave and equipment-note exceptions.

**Checklist behavior:** Reuse shared firing definitions from Prep Fire, with MPh-specific eligibility instead of copied permission rules.

**Illustrative position:** A Gun and a squad in one Location may have different legal responses to the same MP expenditure.

**Terms:** IFT, TH, TK, ROF, IF.

#### Throw a DC during defensive fire

ID: `throwDC`. **Who:** DEFENDER. **Type:** Optional special attack.

**Timing:** ASOP 3.36D. **Rules:** A23.6. **PDF pages:** 679, 90, 91. **Coverage:** Rules integration pending; reference available.

**Consider:** Is a Thrown DC attack permitted against this mover?

- Check range, LOS, unit/weapon-use and timing conditions. A unit already marked First Fire cannot Throw a DC, and it cannot do so as SFF or FPF.
- Resolve both target and throwing-Location consequences under the Thrown DC procedure; do not treat it as a Placed DC waiting for AFPh.

**Checklist behavior:** The checklist warns of consequences to the thrower before committing.

**Illustrative position:** The defender considers a DC against a nearby mover while accounting for the attack on the throwing Location.

**Terms:** DC, DFF.

#### Fire an OVR Prevention shot

ID: `ovrPrevention`. **Who:** DEFENDER. **Type:** Conditional pre-OVR response.

**Timing:** ASOP 3.36D. **Rules:** C5.64-.641. **PDF pages:** 679, 173. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this non-vehicular Gun take an OVR Prevention shot?

- Check the Gun’s CA, current fire markers and No IF restriction immediately after the OVR expenditure is announced, before the OVR resolves.
- The Original TH DR also tests its manning Infantry. Pin or break voids the TH attempt except for specified malfunction/Low Ammo effects.
- Apply IF penalties and No Fire afterward where required. An otherwise unfired Gun uses the C5.641 exceptions; do not apply the same penalty and marker to every case.

**Checklist behavior:** Show the risk to the crew and the possibility that its shot is voided before commitment.

**Illustrative position:** A Gun that already Intensive Fired may have this final qualifying defense, but a failed crew check can leave the OVR unaffected.

**Terms:** OVR, IF, TH, NMC.

#### Resolve CC or non-CC Reaction Fire

ID: `reaction`. **Who:** DEFENDER. **Type:** Conditional vehicle response.

**Timing:** ASOP 3.36D. **Rules:** D7.2; C5.64. **PDF pages:** 679, 207, 208. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which response to this vehicle is legal at this point?

- Separate ordinary DFF before an OVR from Reaction Fire after it, and distinguish CC from non-CC procedures.
- Evaluate PAATC, Street Fighting, fire-marker and weapon conditions under the relevant rule.
- Include the separate OVR Prevention procedure of C5.64 where applicable, rather than treating it as unrestricted FPF.

**Checklist behavior:** A vehicle attack can open more than one differently timed response window.

**Illustrative position:** A unit considers a legal CC Reaction Fire attack after the OVR resolves, not as a substitute for pre-OVR DFF.

**Terms:** Reaction Fire, CC, PAATC, OVR.

#### Create a Tank-Hunter Hero in a CC Reaction Fire position

ID: `heroReaction`. **Who:** DEFENDER. **Type:** Optional special response.

**Timing:** ASOP 3.36D. **Rules:** G1.421. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this Japanese squad or HS attempt hero creation here?

- Verify the CC Reaction Fire position and relevant creation conditions.
- Use the defender timing and follow-up procedure rather than copying the attacker’s movement creation window.

**Checklist behavior:** Keep this defender option present even when special nationality rules are pending.

**Illustrative position:** An approaching AFV creates a qualifying reaction position for a Japanese squad.

**Terms:** T-H Hero, Reaction Fire.

#### Attempt defender Motion or a Smoke Dispenser response

ID: `motionAttempt`. **Who:** DEFENDER. **Type:** Optional vehicle response.

**Timing:** ASOP 3.36D. **Rules:** D2.401; D13.2. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this vehicle react without waiting for its own MPh?

- Check the specific Motion attempt conditions, expenditure basis and limits; it is not a general move-away action.
- Evaluate dispenser eligibility and timing independently, then apply usage and resulting smoke effects.

**Checklist behavior:** Motion and dispenser choices remain separate actions within the current response window.

**Illustrative position:** A defending tank may consider Motion or smoke against the mover without gaining an ordinary movement turn.

**Terms:** Motion, MP, SMOKE.

#### Conduct Sighting TC and Ground Support

ID: `airSupport`. **Who:** DEFENDER. **Type:** Optional aerial attack procedure.

**Timing:** ASOP 3.36D. **Rules:** E7.3-.4; G17.4-.42; W9. **PDF pages:** 679, 667. **Coverage:** Rules integration pending; reference available.

**Consider:** Can available aircraft attack this moving target?

- Check sighting, target, attack and nationality/scenario restrictions; do not treat aircraft as an ordinary ground DFF weapon.
- Open the attacker’s appropriate Light AA response and resolve attack results/markers in source order.
- Apply Chapter W restrictions where relevant, including consecutive-Player-Turn Ground Support limits; FAC-directed PFPh CAS is a different window.

**Checklist behavior:** Link aircraft attack and ground AA responses without changing the Player Turn roles.

**Illustrative position:** The DEFENDER’s aircraft attacks, but the ATTACKER may now have an AA decision.

**Terms:** TC, FB, DB, AA.

#### Use Light AA against attacking aircraft

ID: `attackerAA`. **Who:** ATTACKER. **Type:** Conditional counter-response.

**Timing:** ASOP 3.36D. **Rules:** E7.5; E7.51-.512. **PDF pages:** 679. **Coverage:** Rules integration pending; reference available.

**Consider:** Which eligible weapons may answer this air attack?

- Check Light AA weapon, target and current commitments.
- Apply AA/Prep/Bounding Fire or Gunflash markers as required, preserving subsequent movement and fire restrictions.

**Checklist behavior:** Switch the displayed decision owner to ATTACKER for this window.

**Illustrative position:** An attacker’s weapon answers a diving aircraft while the defender’s ground-fire sequence is temporarily interrupted.

**Terms:** AA, FB, DB, BFF.

#### Place residual and fire markers correctly

ID: `residualCreate`. **Who:** BOTH. **Type:** Required attack consequence.

**Timing:** ASOP 3.36D. **Rules:** A8.2-.26; A9.22; G11.82; C13.57-.58. **PDF pages:** 679, 60. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What persists after this attack?

- Use the residual half-FP calculation, reductions and cap in source order. Apply ordnance-hit and ammo/dud exceptions, not a blanket no-ordnance rule.
- A malfunctioned weapon or Snap Shot leaves no ordinary residual; retained ROF and the immediate decision to forfeit it require their own accounting.
- Place First/Final/Intensive/No Fire/Gunflash markers as required. Resolve MOL-Projector Smoke and possible Flame separately.

**Checklist behavior:** Record residual and marker effects before reopening movement.

**Illustrative position:** A MG retaining ROF needs the appropriate residual decision now, not after the next unit enters.

**Terms:** Residual FP, ROF, MG, MOL.

### 3.37D-3.38D · Fire against aerial landings

#### Fire Light AA at an aerial glider

ID: `gliderAA`. **Who:** DEFENDER. **Type:** Optional aerial response.

**Timing:** ASOP 3.37D. **Rules:** E7.51-.512; E8.21-.211. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Is Light AA fire available against this glider?

- Use the special First/Subsequent First Fire rules and required AA/fire/Gunflash markers.
- After fire, go to Evasive Action at 3.33A; if no Light AA fire occurred, go to landing at 3.42A.

**Checklist behavior:** Do not expose a ground-unit FPF menu for an aerial glider.

**Illustrative position:** Declining eligible AA advances the glider toward landing rather than returning to ordinary ground movement.

**Terms:** AA, DFF, SFF.

#### Fire at aerial parachutes

ID: `parachuteFire`. **Who:** DEFENDER. **Type:** Optional aerial response.

**Timing:** ASOP 3.38D. **Rules:** E9.3-.33; E7.5-.512. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Which parachutes and weapons qualify for fire?

- Use small arms or Light AA only, with the special First/Subsequent First Fire conditions.
- Place required markers, then proceed to the collective landing procedure at 3.43A.

**Checklist behavior:** Aerial target and weapon restrictions are visible before attack selection.

**Illustrative position:** An otherwise available heavy weapon does not gain permission to fire at parachutes simply by appearing in the unit’s weapon list.

**Terms:** AA, DFF, SFF.

### 3.41A · End this ground unit’s move

#### Consider Search before ending ITS MPh

ID: `search`. **Who:** ATTACKER. **Type:** Optional last-window expenditure.

**Timing:** ASOP 3.41A. **Rules:** A12.152; A12.154; E1.95; E1.953; G1.63. **PDF pages:** 680, 79. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Does this eligible unit have a useful Search opportunity?

- Check non-Bypassing Good Order Infantry/Cavalry and qualifying MMC/stack conditions, remaining one MF, Pin and Assault Movement exclusions.
- Apply search area, dr modifiers, exclusions, own-hex search and authorized reveal results. Mine discovery does not automatically disclose type or strength.
- Resolve possible Search casualties and night/Japanese exceptions. Search makes the unit TI, but its expenditure still permits DFF/SFF/FPF.

**Checklist behavior:** End this move must offer the remaining Search choice without suggesting that undiscovered enemies exist.

**Illustrative position:** A squad retains one MF and can Search adjacent accessible Locations. The response to that MF remains live.

**Terms:** Search, MF, TI, DFF.

#### Move broken Infantry from Bypass into the obstacle

ID: `brokenBypass`. **Who:** ATTACKER. **Type:** Required end-of-unit consequence.

**Timing:** ASOP 3.41A. **Rules:** A4.32. **PDF pages:** 680, 50. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Did Infantry break while in Bypass?

- At ITS MPh end apply the required obstacle entry procedure.
- Preserve the resulting Location and subsequent rout obligations; do not leave the unit in an ordinary voluntary Bypass ending.

**Checklist behavior:** This required transition is separate from optional end-of-move review.

**Illustrative position:** A squad broken while bypassing woods enters the obstacle according to A4.32.

**Terms:** Bypass, MF.

#### Attempt sewer emergence

ID: `sewerEmerge`. **Who:** ATTACKER. **Type:** Conditional end-of-unit procedure.

**Timing:** ASOP 3.41A. **Rules:** B8.42. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Where can this sewer unit or stack emerge?

- Resolve the required emergence dr and the rule’s legal outcomes.
- Disclose position and units only as the emergence procedure permits.

**Checklist behavior:** Do not mark a sewer move complete while emergence remains unresolved.

**Illustrative position:** The intended exit is a plan; the emergence result determines the actual outcome.

**Terms:** ITS MPh, DR.

#### Scrounge small arms with an Unarmed unit

ID: `smallArms`. **Who:** ATTACKER. **Type:** Optional end-of-unit attempt.

**Timing:** ASOP 3.41A. **Rules:** A20.552; G17.14. **PDF pages:** 680, 88. **Coverage:** Rules integration pending; reference available.

**Consider:** Can this Unarmed unit attempt to rearm here?

- Check all-MF-spent movement and terrain conditions, including non-water/non-marsh, and the once-per-hex-per-MPh limit.
- Use the required DR and nationality-specific rearming outcome. This is not Rally wreck Scrounging.

**Checklist behavior:** Preserve the hex attempt limit across different Unarmed units.

**Illustrative position:** A second Unarmed unit cannot repeat an already-used hex attempt just because its own checklist is new.

**Terms:** MF, DR.

#### End the vehicle’s move Stopped or in Motion

ID: `endVehicle`. **Who:** ATTACKER. **Type:** Required movement closure.

**Timing:** ASOP 3.41A. **Rules:** D2.4; ASOP 3.41A. **PDF pages:** 680. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** What is this vehicle’s legal end-of-move state?

- Place Motion on a qualifying vehicle or expend Stop MP as required.
- Resolve any final expenditure responses and place a CC counter if necessary before selecting another mover.

**Checklist behavior:** End this vehicle’s move does not end THE MPh.

**Illustrative position:** Choosing to Stop creates its applicable MP response; a phase-end button cannot bypass it.

**Terms:** Motion, MP, CC.

### 3.42A-3.43A · Land gliders and parachutes

#### Resolve glider landing and possible crash

ID: `gliderLand`. **Who:** ATTACKER. **Type:** Required aerial conclusion.

**Timing:** ASOP 3.42A. **Rules:** E8.211-.23. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** What landing result does this glider obtain?

- Resolve the Landing DR and any Crash dr using the actual approach and terrain.
- Return to 3.23A after this glider’s sequence; defer contents placement to its specified later window, including AFPh E8.4.

**Checklist behavior:** Do not unload all glider contents as ordinary MPh passengers.

**Illustrative position:** The glider lands now, while the later placement of contents remains a distinct procedure.

**Terms:** ILH, DR.

#### Resolve collective parachute landing

ID: `parachuteLand`. **Who:** ATTACKER. **Type:** Required aerial conclusion.

**Timing:** ASOP 3.43A. **Rules:** E9.4; E9.42. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Which landing displacement and checks apply?

- Move all non-German 5/8-inch parachutes one hex as prescribed, then land all parachutes and flip the 1/2-inch counters.
- Resolve the required NMC/NTC and terrain outcomes before moving to phase-end procedures.

**Checklist behavior:** Keep nationality and counter-size distinctions explicit.

**Illustrative position:** German and non-German parachutes cannot be processed by one unconditional extra-hex rule.

**Terms:** NMC, NTC.

### 3.51A-3.53B · End the entire Movement Phase

#### Resolve required terrain-Blaze eliminations

ID: `blazeEnd`. **Who:** ATTACKER. **Type:** Required phase-end consequence.

**Timing:** ASOP 3.51A. **Rules:** B25.4; E8.232; E9.42. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Are affected vehicles or landed aerial units still in a terrain Blaze?

- Apply the vehicle-unable-to-leave and glider/parachute landing conditions.
- Resolve elimination and associated contents consequences before completing THE MPh.

**Checklist behavior:** This is a phase-end obligation, not an optional warning.

**Illustrative position:** A landed glider in terrain Blaze requires its prescribed outcome even after all movement choices were considered.

**Terms:** LC, MPh.

#### Resolve declared Wreck Removal

ID: `wreckEnd`. **Who:** ATTACKER. **Type:** Required follow-through.

**Timing:** ASOP 3.51A. **Rules:** D10.42. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Which declared Wreck Removal attempts are due?

- Resolve each valid declaration under its governing conditions.
- Update wreck and vehicle state and retain failed outcomes; merely considering the topic did not create a declaration.

**Checklist behavior:** Match each result to its earlier work item.

**Illustrative position:** The guide resolves the vehicle’s previously declared attempt without repeating the declaration.

**Terms:** MP, MPh.

#### Locate pre-1942 German paratroop arms canisters

ID: `canisters`. **Who:** ATTACKER. **Type:** Optional special phase-end attempt.

**Timing:** ASOP 3.51A. **Rules:** E9.7. **PDF pages:** 680. **Coverage:** Rules integration pending; reference available.

**Consider:** Are eligible air-dropped German MMC seeking arms canisters?

- Check the pre-1942, air-drop and unit conditions.
- Use the specific canister-location procedure, not ordinary SW Recovery or Unarmed Scrounging.

**Checklist behavior:** Keep this historical equipment option visible for the relevant scenario profile.

**Illustrative position:** A qualifying paratroop scenario has a phase-end task that an ordinary Infantry scenario does not.

**Terms:** MMC, DR.

#### Return qualifying berserk units to Good Order

ID: `berserkEnd`. **Who:** ATTACKER. **Type:** Required phase-end state change.

**Timing:** ASOP 3.52A. **Rules:** A15.46. **PDF pages:** 680. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Do any berserk units have no Known enemy in LOS?

- Evaluate the actual phase-end condition under A15.46.
- Do not confuse this return with the different later AFPh return condition after eliminating Known enemies.

**Checklist behavior:** Update the unit before DFPh and preserve lawful information boundaries.

**Illustrative position:** A berserk unit with no Known enemy in LOS is reviewed at this step, not at the end of each expenditure.

**Terms:** LOS, MPh.

#### Remove Residual FP and half-inch SMOKE

ID: `cleanup`. **Who:** BOTH. **Type:** Required phase-end cleanup.

**Timing:** ASOP 3.53B. **Rules:** A8.2; A9.223; A24.11; G11.85. **PDF pages:** 680. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which counters expire at the end of THE MPh?

- Remove all residual, including Fire Lane residual, and the applicable 1/2-inch SMOKE counters.
- Honor the cave exception and counter type; do not remove every SMOKE counter or all defensive fire markers.

**Checklist behavior:** Clear only due effects, retaining First/Final Fire commitments for their later removal windows.

**Illustrative position:** A First Fire marker remains for DFPh even after its residual counter is removed.

**Terms:** Residual FP, Fire Lane, SMOKE.

### Additional rule-specific Movement overlays

#### Resolve Jitter when triggered

ID: `jitter`. **Who:** BOTH. **Type:** Conditional night consequence.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** E1.55; E1.53; W7.8. **PDF pages:** 679, 224, 225, 665. **Coverage:** Rules integration pending; reference available.

**Consider:** Has a qualifying night procedure triggered Jitter?

- Use the trigger, Gunflash conditions, side/unit selection and eligible weapon restrictions in E1.55.
- Jitter is not a freely chosen attack; apply the resulting movement/fire effects, including cancellation where required.
- Chapter W bugles introduce their own night trigger and visibility treatment rather than ordinary voluntary Starshell fire.

**Checklist behavior:** Interrupt the originating movement check and resume only after the night procedure settles.

**Illustrative position:** A doubles Straying result can create a Jitter sequence instead of simply choosing a new route.

**Terms:** Jitter, Straying, Gunflash.

#### Apply CPVA IPM and non-IPM movement rules

ID: `cpva`. **Who:** ATTACKER. **Type:** Nationality-specific movement.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** W7.4-.42. **PDF pages:** 679, 663, 664, 665. **Coverage:** Rules integration pending; reference available.

**Consider:** Must these CPVA units move as a group, or do they qualify for an exception?

- Apply squad/HS, Recon, leader-initiation and joining distinctions; preserve group connectivity and impulse rules.
- For a required non-IPM NTC, apply exemptions, night modifier and failure movement/order consequences; do not trigger SAN or concealment loss from this check.
- Handle newly berserk participants and broken connectivity through the Chapter W procedure. Apply W7.9 restrictions, including no Armored Assault and the specified Rider exceptions.

**Checklist behavior:** Keep CPVA movement explicit rather than labelling it ordinary Platoon movement.

**Illustrative position:** A failed non-IPM attempt may restrict a unit to one Location before other movement, rather than simply consuming a generic action.

**Terms:** CPVA, IPM, NTC, Impulse.

#### Use CPVA bugles when an SSR permits

ID: `bugles`. **Who:** ATTACKER. **Type:** Optional scenario-specific opening choice.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** W7.8. **PDF pages:** 679, 665. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this scenario permit the opening bugle choice?

- Check the SSR and eligible Good Order leader onboard or entering, at the start of the CPVA MPh.
- Apply the Jitter/initial-illumination equivalence, CPVA non-IPM and concealment modifiers, and the UN Scenario Defender leader’s Freedom of Movement opportunity.
- For the specified dates and UN nationalities, perform required Jitter-only Straying checks even in daytime or when an initial night Movement DR is unnecessary. Apply the stated unit-quality treatment and avoid a second Jitter trigger from a later Movement DR.

**Checklist behavior:** Place the choice at phase start, not as a freely repeatable mid-move action.

**Illustrative position:** A qualifying SSR adds an opening decision; without that SSR the reference remains visible as scenario-dependent.

**Terms:** CPVA, Jitter, Gunflash.

#### Declare an SSR-enabled UN Bayonet Charge

ID: `bayonet`. **Who:** ATTACKER. **Type:** Optional nationality-specific charge.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** W.6. **PDF pages:** 679, 656. **Coverage:** Rules integration pending; reference available.

**Consider:** Does the scenario permit this designated UN Infantry to charge?

- Use the Banzai-derived procedure with W.6 exceptions, including Known enemy Infantry targeting and participant qualifications.
- Apply leader/NTC nationality exceptions, failure movement and later Hand-to-Hand consequences; do not copy Japanese end-state rules unchanged.

**Checklist behavior:** Show the declaration before movement and preserve its compulsory follow-through.

**Illustrative position:** A designated UN squad’s failed qualifying check has the specified restricted movement outcome.

**Terms:** Banzai, NTC, CC.

#### Maintain or track Chapter W searchlights

ID: `searchlight`. **Who:** DEFENDER. **Type:** Conditional enemy-MPh procedure.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** W10.44-.52. **PDF pages:** 679, 670. **Coverage:** Rules integration pending; reference available.

**Consider:** Must the beam be maintained, tracked or cancelled?

- At the start of the enemy MPh resolve required maintenance/reliability; track a moving target only under the beam and LOS rules.
- Check the limited moving-target exception for a new target within NVR and the conditions for cancellation.
- Do not offer unrestricted beam retargeting or cancellation on every expenditure.

**Checklist behavior:** Attach beam changes to the proper movement window and visibility projection.

**Illustrative position:** A moving unit leaves the beam; the searchlight’s tracking rule determines whether the beam may follow.

**Terms:** NVR, LOS, CA.

#### Resolve attack results, Snipers, wounds and forced movement

ID: `attackChain`. **Who:** BOTH. **Type:** Triggered consequence chain.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** A7-A8; A14; A15; A17; D5; ASOP dagger. **PDF pages:** 679, 678. **Coverage:** Partially integrated; complete rule path remains to be verified.

**Consider:** Which required results remain from the last roll?

- Resolve morale, Pin, casualty, leader loss, HOB/berserk, wound, Stun/Recall, collateral and terrain effects when actually triggered.
- Apply Sniper activation/check eligibility and exclusions to the originating roll; do not create one generic end-of-MPh Sniper task.
- Recalculate ongoing movement after results, including interrupted impulses and charge obligations.

**Checklist behavior:** A required consequence cannot be satisfied by marking this reference topic considered.

**Illustrative position:** A wound changes remaining movement capability; the previously plotted route needs reevaluation.

**Terms:** SAN, MC, HOB, CX.

#### Apply scenario, equipment and external-module exceptions

ID: `profile`. **Who:** BOTH. **Type:** Conditional source overlay.

**Timing:** Rule-specific MPh timing; supplemental to ASOP. **Rules:** Chapter H notes; SSR; ASOP dagger; O1.5. **PDF pages:** 679, 678. **Coverage:** Rules integration pending; reference available.

**Consider:** Does this scenario or equipment introduce an additional MPh condition?

- Keep Chapter F/G/W and equipment-note options visible even though the current implementation passes focus on A-E.
- Record named gaps: O1.5 non-Factory Debris and ASOP Sniper exceptions RB CG8, ABtF CG8, BRT CG8, KGP CG16 and PB CG13. Full module text is outside this volume.
- Load each applicable SSR and equipment note before claiming profile-complete coverage. Unknown relevance is not false; this record does not stand in for an audited inventory of every module.

**Checklist behavior:** Show source unavailable or rules integration pending distinctly from illegal.

**Illustrative position:** A campaign-specific Sniper exception remains named and unresolved until its actual module text is available.

**Terms:** SSR, Source gap.

## Contextual vocabulary

These labels are explanatory links, not newly minted official acronym identities. Reuse the existing acronym catalog where its identity and meaning match; otherwise use a term definition. Preserve case and distinctions such as MF/MP, Motion/Non-Stopped, and DFF/SFF/FPF.

| Term | Meaning in this phase |
|---|---|
| AA | Anti-Aircraft fire or mode. |
| AFPh | Advancing Fire Phase. Opportunity Fire reserves a full-effect attack here. |
| AFV | Armored Fighting Vehicle. |
| Assault Movement | A declared Infantry movement mode under A4.61. It can avoid FFNAM without automatically avoiding FFMO. |
| Banzai | The Japanese charge procedure derived from Human Wave rules with G1.5 exceptions. |
| BFF | Bounding First Fire, eligible vehicular fire during the vehicle’s MPh. |
| BU | Buttoned Up. The alternative protected crew posture, with weapon and observation consequences. |
| Bypass | Movement around a permitted obstacle within its hex. Clearance, costs and attack geometry depend on the unit and route. |
| CA | Covered Arc. |
| CC | Close Combat. A CC counter and MPh CC Reaction Fire have distinct timing from the later Close Combat Phase. |
| CE | Crew Exposed. A vehicle crew posture whose voluntary changes have timing restrictions. |
| CPVA | Chinese People’s Volunteer Army, with Chapter W nationality and movement rules. |
| CX | Counter Exhausted. A state caused by Double Time and other rules, with consequences beyond extra movement. |
| Dash | The special road-crossing movement procedure of A4.63, subject to its own declaration and route conditions. |
| DB | Dive Bomber, an aircraft type with its own attack and AA response procedures. |
| DC | Demolition Charge. |
| DC Hero | A Japanese hero using the special demolition-charge procedure of G1.424. |
| DD | Duplex Drive tank. Its screens and amphibious state have specific consequences. |
| DFF | Defensive First Fire, attacks against eligible moving targets during the opponent’s MPh. |
| Double Time | An Infantry choice to gain MF and become CX. Declaration at the start and later in ITS MPh gives different bonuses. |
| DR | Two-dice roll. Keep Original, modified and individual colored/white values. |
| ESB | Excessive Speed Breakdown. Extra vehicle movement is subject to the governing breakdown risk. |
| FB | Fighter-Bomber, an aircraft type with its own sighting and Ground Support procedures. |
| FFE | Fire For Effect. FFE:1, FFE:2 and FFE:C have different actions. |
| FFMO | First Fire Movement in Open Ground, assessed for the particular attack and route. |
| FFNAM | First Fire Non-Assault Movement, a modifier tied to movement and activity, not merely a changed hex. |
| Fire Lane | A machine-gun residual-fire procedure with its own placement, attack and cancellation rules. |
| FPF | Final Protective Fire, restricted further fire that also tests the firer’s morale. |
| Freedom of Movement | Night Scenario Defender restriction and release procedure under E1.21, not ordinary movement eligibility. |
| Gun Duel | C2.2401 procedure that can alter which eligible declared attack resolves first. |
| Gunflash | Night firing marker or specified equivalent that affects visibility and related night procedures. |
| Hazardous Movement | A defined exposure condition for specified activities, not a generic label for a dangerous route. |
| HD | Hull Down. A vehicle protection state, including a possible maneuver attempt under D4.22. |
| HOB | Heat of Battle, a triggered resolution rather than an optional action. |
| Human Wave | Group movement under A25.23 and applicable nationality rules, using ordered impulses. |
| IF | Intensive Fire, an extra Gun shot with accuracy and malfunction penalties. |
| IFE | Infantry Firepower Equivalent, a weapon-specific alternative to ordinary TH fire. |
| IFT | Infantry Fire Table. |
| ILH | Intended Landing Hex for a glider. |
| Impulse | A coordinated expenditure by a qualifying group, completed for the group before the response procedure. |
| IPC | Inherent Portage Capacity. Excess portage affects the moving unit’s available MF. |
| IPM | Independent Platoon Movement, the CPVA movement procedure in W7.4. |
| IR | Illuminating Round, with its own usage and placement procedure. |
| ITS MPh | The movement sequence of the current unit or stack, distinct from the entire Movement Phase. |
| Jitter | Night firing caused by the E1.55 procedure, not a freely chosen attack. |
| LC | Landing Craft, governed by its own transport, fire, beaching and grounding rules. |
| LOS | Line of Sight. Guidance uses only information the viewing player may know. |
| MC | Morale Check. |
| MF | Movement Factors, used by Infantry and certain other movement types. Do not interchange with vehicle MP. |
| MG | Machine Gun. |
| Minimum Move | A4.134 permits certain one-hex moves despite insufficient MF, with Pin and CX consequences after First Fire. |
| MMC | Multi-Man Counter: squad, half-squad or crew. |
| MOL | Molotov Cocktail. A squad MOL and a MOL Projector have different procedures. |
| Motion | A vehicle state. It is not interchangeable with Non-Stopped or a moving target for fire modifiers. |
| MP | Movement Points. Vehicle expenditure and the resulting defensive-fire opportunity are tracked separately from distance. |
| MPh | Movement Phase. THE MPh contains a separate start, during and end for each moving unit or stack. |
| MTR | Mortar. |
| NMC | Normal Morale Check. |
| Non-Stopped | Vehicle condition used in movement and firing rules. It is distinct from a Motion counter. |
| NTC | Normal Task Check, used only when the governing rule calls for it. |
| NVR | Night Visibility Range, used with illumination and other night visibility exceptions. |
| OVR | Overrun. Infantry OVR and vehicular OVR are different procedures with different prerequisites. |
| PAATC | Pre-AFV Attack Task Check, required in specified Infantry attacks or entries involving an AFV. |
| PP | Portage Points, the load represented by carried equipment. |
| PRC | Passengers, Riders and Crew, with different vulnerability and fire restrictions. |
| Reaction Fire | Defender attacks against vehicles under D7.2, with separate CC and non-CC procedures. |
| Residual FP | Firepower left by a qualifying attack, applied to later qualifying expenditures under A8.2. |
| ROF | Rate of Fire. A retained opportunity is not an automatic shot. |
| SAN | Sniper Activation Number, tested only for qualifying Original rolls. |
| Search | An end-of-ITS-MPh procedure using a qualifying unit or stack. It is not a free inspection of hidden information. |
| SFF | Subsequent First Fire, restricted additional defensive fire after First Fire. |
| SMOKE | The umbrella for Smoke and WP effects; preserve ammunition type, owner and source. |
| Source gap | An identified rule reference whose full text is outside the supplied volume. Its details remain unevaluated. |
| SSR | Scenario Special Rule. It can add or modify applicability and must be included in the scenario profile. |
| Straying | A rules-driven departure from intended movement in specified night or terrain conditions. |
| Sustained Fire | Additional MG/IFE fire with its own firepower and breakdown consequences. |
| SW | Support Weapon. Using one affects its possessor’s capability accounting. |
| T-H Hero | Tank-Hunter Hero, created and used under G1.42 rather than ordinary hero rules. |
| TB | Trail Break. Creation and use depend on terrain and the applicable vehicle procedure. |
| TC | Task Check. |
| TCA | Turret Covered Arc, distinct from the vehicle hull’s covered arc. |
| TEM | Terrain Effects Modifier. |
| TH | To Hit roll or number for an ordnance attack. |
| TI | Task Involved status, not a synonym for Prep Fire. |
| TK | To Kill resolution, distinct from the To Hit roll. |
| TPBF | Triple Point Blank Fire. Some same-Location entry attacks are compulsory under A8.312. |
| US# | Unit Size Number. Used for purposes including prisoner guarding capacity and the dr to Set a DC; it is not a weapon Usage Number. |
| VBM | Vehicular Bypass Movement. Uses vehicle-specific clearance, costs, facing and defensive-fire geometry. |
| VCA | Vehicular Covered Arc, the vehicle hull’s facing. |
| WA | Wall Advantage. Its claim, loss and mandatory conditions depend on B9.32 and the position. |
| WP | White Phosphorus, with its own height, NMC and ignition rules. |
| Wreck Check | A specified check for wagons or motorcycles in the circumstances of D12.4/D15.46. |

## Acceptance cases for the eventual live component

| Case | Required behavior |
|---|---|
| Entry followed by Recovery | Two expenditures and independent response windows. |
| ROF at a one-MF expenditure | Retained capability does not grant unlimited shots at that window. |
| Same-hex additional activity | Reevaluate exposure and residual exceptions without inventing a hex change. |
| Search at end of unit movement | Check eligibility, pay MF, allow defensive fire and resolve casualties before closure. |
| First Fire vs mandatory TPBF | Optional pass cannot close a compulsory A8.312 attack. |
| CE target declaration | No voluntary BU switch while the allowed current fire is unresolved. |
| DC carrier pinned during placement | DC is not operably Placed; no invented AFPh detonation. |
| OVR vehicle immobilized by DFF | Resolve the source-required remaining OVR and Reaction Fire sequence. |
| Impulse with one affected participant | Keep other participants and group constraints intact. |
| Unimplemented Dash, sewer or cave rule | Visible reference and explicit unevaluated availability. |
| Glider vs parachute | Distinct AA/evasion and collective drift/landing sequences. |
| End THE MPh | Due residual/smoke removed; later-expiring First/Final Fire retained. |
| Different hidden state, same public position | Same observable pre-encounter guide, including counts, hints and route decoration. |
| External module or unknown SSR | Source gap remains explicit; no claim of exhaustive profile validation. |

## Preview validation

The updated combined preview was checked across all 114 Movement, 58 Prep Fire and 45 Rally topics and their contextual definitions. Checks covered catalog identities and source-page ranges, Movement/PFPh ledger links, timing navigation, separate phase consideration state, host-state restoration, and 320/736-pixel layouts. Light and dark screenshots were inspected. No browser runtime errors or horizontal overflow were found. These checks validate reference content wiring and presentation, not ASL rule execution; no game-engine behavior changed.
