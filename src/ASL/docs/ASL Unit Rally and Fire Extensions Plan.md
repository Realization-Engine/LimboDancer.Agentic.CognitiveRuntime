# ASL Unit Rally and Fire Extensions Plan

**Status:** Rulings accepted by the user on 2026-09-26. Steps 19 to 23 are built as the [ASL Unit Rally and Fire Extensions in Live Play Design](<ASL Unit Rally and Fire Extensions in Live Play Design.md>) records; steps 24 and 25 are later passes. It sets out unit steps 19 to 25, the rulings for steps 19 to 23, and how to build them in one implementation pass. Everything the rulings leave out, and each recorded deviation, is in the [ASL Unit Backlog](<ASL Unit Backlog.md>).

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 19 to 25, and acceptance scenarios U21 to U29 (section 14).

**Related documents:** the [Scenario A1 Fire Review](<Scenario A1 Fire Review 2026-09-26.md>) (its scope list of what step 17 left out) and the [ASL Unit Fire in Live Play Design](<ASL Unit Fire in Live Play Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`), checked with `pdftotext 4.00` page by page.

## 1. Outcome

Broken units can rally, and Infantry fire covers the rest of the fire phases, fire groups, targets, and Infantry weapons that step 17 left out. Ordnance and vehicles are designed here so their order and size are known, but they are later passes.

| Step | Subject | Pass |
|---|---|---|
| 19 | Rally, DM, and Fate in the RPh | 1 |
| 20 | Advancing Fire and multi-Location fire groups | 1 |
| 21 | Fire at Locations holding hidden units or Dummies | 1 |
| 22 | Hex-by-hex Infantry movement, Defensive First Fire, Subsequent First Fire, FPF, and Residual FP | 1 |
| 23 | Support weapons and MGs on the IFT, with Repair in the RPh | 1 |
| 24 | Ordnance (Chapter C) | 2 |
| 25 | Vehicles (Chapter D) | 3 |

## 2. Principles carried forward

Each step keeps the method of steps 17 and 18:

- **Review before code.** Register and verify the rule fragments against their physical pages, state what they establish, and have the user rule on what they leave open. A reviewed, digest-pinned package with no execution authority comes first; live play uses it.
- **Every outcome decided before any roll.** A governed action commits only when the package decides every outcome the dice can reach, and draws its rolls one at a time.
- **Printed values from the catalog or the user.** New counters (support weapons, Dummies, Guns, vehicles) take their printed values from the user.
- **Each side sees what it may know.** Records that name units a side cannot see stay with the side that owns them, and a public report carries what the other side would know at the table, as `fire-reported` does.

## 3. Sources

### 3.1 Charts inside the registered range

Every back-matter chart page has a copy inside the registered pages 6 to 253, whose text matches the back-matter page after whitespace normalization. The new steps therefore need no new chart supplement: their transcriptions can come from registered pages.

| Registered page | Back matter | Content | Used by |
|---|---|---|---|
| 106 | 692 | A7 IFT, with the Vehicle line; A3 Sequence of Play; A17 Wounds | 19, 22, 23, 25 |
| 107 | 693 | A12.121 Concealment Loss/Gain | 21 |
| 108 | 694 | Support Weapons Chart; A15; A18.2 | 19, 23 |
| 110 | 696 | A9.22 Fire Lanes | 23 |
| 160 | 698 | B Terrain Chart (underlined Rally terrain; MF costs) | 19, 22 |
| 189 to 191 | 700 to 702 | C3 To Hit, C4 to C6 Cases, C7 To Kill tables, C2.5 ROF, C6.5 | 24 |
| 220 to 221 | 703 to 704 | D charts; D1.8 vehicular MG | 25 |

Ruling R0.1 below asks whether to transcribe from these registered pages. If so, the step 17 supplement for pages 692 and 698 stays as recorded, and later transcriptions cite the registered copies.

### 3.2 A correction

A12.14 begins on page 77, not 76. The Fire in Live Play Design, the `FireReported` summary, and the visibility tests cite page 76; this plan corrects them.

### 3.3 Printed counter values

The user pointed to three places in the PDF where counters are shown (2026-09-26):

- **A1 (p. 44):** the anatomy of a Personnel counter (Firepower, Range, Morale, Class, Identity, Smoke exponent, Assault Fire, Spraying Fire, ELR 5, broken Morale Level, Self-Rally).
- **Appendix 1, Counter Examples (pp. 676 and 677):** one annotated counter of each type, drawn as images: leaders, heroes, squads, HS, crews, a MMG with its Breakdown Number, Rate of Fire, Repair and Removal Numbers, dm weapons, mortars, the ATR, FT, DC, PSK, radio, field phone, and on page 677 the Gun and vehicle counters.
- **Chapter H, Design Your Own (from p. 328):** the purchase rules and each nation's SW Allotment Chart (how many of each SW type a game holds; for example German LMG, MMG, HMG, ATR, light mortar, PSK, FT, DC), then each nation's Vehicle Listing and Ordnance Listing with every vehicle's and Gun's values (BPV, RF, dates, size, AF, TA, OT, CS, MP, MA, ROF, B#, IF, BMG, CMG, AAMG, ammunition, and notes): German vehicles from p. 337, German ordnance from p. 351, Russian vehicles from p. 355, Russian ordnance from p. 363, and the other nations after them.

What this settles:

- **Ordnance and vehicles (steps 24 and 25):** the Chapter H listings give the printed values of every Gun and vehicle by nation, so no counter values are needed from the user for them.
- **Dummies (step 21):** a Dummy has no printed values; the counter examples show the concealment counter, and how many each side has is a setup fact.
- **Support weapons (step 23):** the Support Weapons Chart (registered p. 108; also pp. 687 and 694) gives each SW type's operational capabilities (how many a squad, crew or HS, or SMC may fire, at what cost to inherent FP, and notes A to M), and says that portage costs are "as per counter listing"; it gives no FP, range, B#, ROF, or repair numbers. The Allotment Charts give counts, not values, and the Counter Examples show one example of each type (the MMG shown is 4-10, B11, ROF 2, R2, X6). The values of each nation's own LMG, MMG, and HMG are not listed as text. Ruling R0.3 decides where they come from.

**Manufactured values.** The Index (pp. 11 to 40) was checked for every LMG, MMG, HMG, and MG entry: each points to a usage rule (A4.41, A9, A9.2 to A9.21, A9.8, A21, B20.95, D6.1, E7.51, G1.611, G13.4211), and none to a listing of values. The user ruled (R0.3) that a value no source gives is manufactured. So that a manufactured value is never mistaken for a printed one:

- Each manufactured value is recorded in the catalog with its own provenance, `manufactured`, naming the ruling, instead of a page and transcription digest; the catalog's review lists every such value.
- A manufactured value starts from the nearest shown source: the Counter Examples' MMG (4-10, B11, ROF 2, R2, X6, p. 676) for a MMG, and the rules' constraints (A9.1: FP-Range; A9.2: ROF; A9.7 and A9.72: B#, R#, X#; A21.11: captured penalties). Where nothing is shown, the user chooses the value in the review sitting.
- A manufactured value is replaced, and the catalog republished, if a source for it is later registered.

Two cautions for transcription:

- Pages 328 to 380 and 676 to 677 lie outside the registered pages 6 to 253, so they need registering as a bounded supplement, as step 17 did for pages 692 and 698 (ruling R0.3).
- Several Chapter H pages have an unusable text layer (for example pp. 369, 378, and 379 extract as symbol noise), and the counter examples are images. Their values are read from rendered pages and spot-checked by the user, as step 17 did for chart glyphs.

### 3.4 A gap in step 18: Heat of Battle

A15.1 (p. 83) calls for a Heat of Battle DR after "any Original MC or Rally (not Self-Rally) DR of 2". Step 17 excluded Heat of Battle, but the Fire package still resolves an MC whose Original DR is 2 as an ordinary pass: `ScenarioA1FireCalculator` handles an Original 12 and not a 2. So an attack that calls for an MC can reach an outcome the package does not decide, and step 18's pre-check does not catch it. The same 2 decides a leader-led rally (step 19) and an FPF firer's NMC (step 22).

Ruling R0.2 chose among these (the user accepted option 3):

1. **Review A15 now**, in step 19: A15.1 to A15.5 (pp. 83 to 84) and the Heat of Battle chart (p. 108). Its results open Heroes (A15.2), Battle Hardening (A15.3), Berserk (A15.4), and Surrender (A15.5), each with its own rules. This is the faithful choice and makes step 19 about twice as large.
2. **Refuse**: treat any MC or rally that can roll an Original 2 as undecided. This refuses nearly every fire attack that calls for an MC and every leader-led rally, so it undoes most of step 18.
3. **Record a deviation**: an Original 2 is resolved as its MC or rally result, the record says that Heat of Battle was not taken, and the package's review lists the deviation until A15 is reviewed.

Option 3 applies for this pass, with A15 as the first step of the next pass, since option 1 alone would outgrow the pass and option 2 undoes working play. Whichever is chosen, the Fire package and its reachability walk change in step 19, part 1.

## 4. Step 19: Rally

**Rules.** A3.1 (p. 47): both sides act in the RPh, each unit once. A10.4 (p. 66): a broken unit uses its broken Morale Level. A10.6 to A10.64 (p. 68): a Good Order friendly leader in the Location may rally the broken units there; Self-Rally otherwise only with capability (a boxed broken Morale Level), for broken leaders, and one MMC per Player Turn; success on a DR at most the broken Morale Level; -1 in woods, a building, a pillbox, or a trench (A10.61); +4 under DM (A10.62); +1 for Self-Rally (A10.63); an Original 12 is Fate, Casualty Reduction with no rally (A10.64). A10.7 to A10.72 (pp. 68 to 69): a leader never modifies his own Self-Rally; a lone broken leader rallies himself before the others; a non-zero leadership modifier is mandatory. A19.12 (p. 86): a Disrupted unit may not Self-Rally. A12.141 (p. 78): rallying in LOS of a Good Order enemy within 16 hexes loses "?". A17.11 and A17.3 (p. 85): a leader's Fate is a wound.

DM is removed at the end of every RPh (A10.62), not its start, so a unit rallies under the +4 it carries into the phase.

**What is already there.** The catalog marks Self-Rally capability (`asl:self-rally`) and the broken Morale Levels; the vocabulary has the `asl:dm` state; A17 wounds and Casualty Reduction exist from step 18.

**Design.**

1. *Package `scenario-a1-rally`.* A reviewed resolver like the Fire package's: facts (the unit, its definition and states, the Location's terrain, the leaders present and their states, DM, whether the unit is the side's first MMC Self-Rally of the Player Turn, the ELR), one recorded DR (and a Wound Severity dr after a leader's Fate), and the result: rallied, not rallied, or Fate with its Casualty Reduction or wound, with the arithmetic.
2. *DM in the game.* A unit that breaks gets `asl:dm` (the "broke this Player Turn" source, which the game records). Other sources (attacked while broken, an ADJACENT armed Known enemy, the RtPh) are decided by ruling R19.1. A phase change out of the RPh removes DM (A10.62), as step 18 removes the fire markers.
3. *The action `asl.game.rally`.* It takes one broken unit and an optional rallying leader in the RPh. The planner derives every fact, refuses what the package abstains on, checks that the unit has not attempted a rally this Player Turn and that no other RPh action excludes it (A3.1), and draws the one roll. The record is a `rally-attempted` event with its arithmetic; the effects are `conditions-changed` and `lineage`, as in fire.
4. *Visibility.* Rally DRs are public. A concealed unit that rallies in LOS of a Good Order enemy within 16 hexes loses "?"; otherwise its rally stays with its side, and a public report says only that a rally was attempted in that Location, if ruling R19.8 admits one.
5. *The Play page.* An RPh panel lists each side's broken units with the leader choices and the DRM, and shows each attempt's arithmetic.

**Rulings** (accepted by the user, 2026-09-26; items left out are in the backlog).

| Id | Question | Answer |
|---|---|---|
| R19.1 | Which DM sources does the game track? | Breaking this Player Turn, and being attacked while broken by enough FP to cause a NMC. Leave the ADJACENT and RtPh sources out until routing exists. |
| R19.2 | A leader-led Original 2 calls for Heat of Battle (A15.1, p. 83), which is out of scope. | As R0.2 (section 3.4): the rally succeeds and the record says Heat of Battle was not taken. |
| R19.3 | Is Fate in scope? | Yes: Casualty Reduction and wounds already exist. |
| R19.4 | Is Field Promotion (A18.11, p. 85) in scope? | No, and as with R0.2: the first MMC Self-Rally is an ordinary Self-Rally, an Original 2 on it rallies the unit, and the record says Leader Creation was not taken. Making the 2 Indeterminate would refuse every such rally. |
| R19.5 | Which terrain gives -1? | Woods and the ordinary buildings of the Fire package's terrain list; everything else 0 until reviewed. |
| R19.6 | Order of attempts | Free, except that a lone broken leader must rally first (A10.71). |
| R19.7 | Which side rallies? | Both, in every RPh. |
| R19.8 | Concealed rally visibility | As in the design, with no public report. |
| R19.9 | Wounded leaders | Apply A17.3 to Morale Level and leadership. |
| R19.10 | Exclusions | Commissars, Allied Troops, Encirclement, night, Extreme Winter, Recovery, and Deploy. |

R19.2 is the one that decides the slice: without A15, every leader-led rally can roll an Original 2. It follows R0.2, which also decides the Fire package's MCs.

Step 19 opens with the Fire package change R0.2 calls for (part 1), since it fixes merged behavior and the rally package shares it.

## 5. Step 20: Advancing Fire and multi-Location fire groups

**Rules.** A3.5 (p. 47) and A7.24 (p. 55): the ATTACKER's units that did not Prep Fire may fire in the AFPh at half FP. A7.36 (p. 56): Assault Fire adds 1 FP to an underscored squad in the AFPh. A7.8 (p. 58): a pinned firer's FP is halved, cumulatively. A7.5 to A7.531 (p. 57): a fire group may span Locations that are each ADJACENT (A.8, p. 43) to another participant; every member needs LOS, the worst Hindrance and TEM apply to all, any member's detrimental DRM applies to the whole group, and a leader's DRM applies only when a directing leader is in every Location (the worst of them).

**Design.**

1. *Package revision.* `scenario-a1-fire` gains the AFPh (the halving, Assault Fire from the catalog's `asl:assault-fire` trait, and the pinned-firer halving), and firers in several Locations with per-firer range, level, PBF, and Long Range and the group's worst-case DRM.
2. *The ADJACENT predicate.* The map read (ASL-MAP-080) gains ADJACENT: LOS exists and an Infantry unit could advance between the two Locations.
3. *The fire action.* `firers` may span Locations; the planner reads LOS per firer and refuses a group with a member whose LOS is blocked, before any roll (ruling R20.2). A7.55 applies per Location. The fire record holds several firer Locations.
4. *Markers.* An AFPh attack is recorded by its fire record. The marker question is ruling R20.3.

**Rulings** (accepted by the user, 2026-09-26; items left out are in the backlog).

| Id | Question | Answer |
|---|---|---|
| R20.1 | Opportunity Fire (A7.25) | Out. |
| R20.2 | A member with blocked LOS | Refused before any roll (the planner knows LOS), not A6.11's fired-and-wasted. |
| R20.3 | The AFPh marker | Prep Fire, since A3.5 removes it at the end of the AFPh and A7.1 allows one fire phase per Player Turn. |
| R20.4 | Worst case | The largest attributed Hindrance DRM among the members; the target's TEM once. |
| R20.5 | Levels | Refuse a group unless every member is at the target's level, as step 17 did for one Location. |
| R20.6 | Cowering with leaders in some Locations only | A directing leader prevents Cowering only when he is in every Location. |

## 6. Step 21: fire at Locations holding hidden units or Dummies

**Rules.** A12.11 (p. 76), A12.13 and A12.14 (p. 77), A12.14 continued and A12.141 (p. 78), A12.3 and A12.31 (p. 80), the Concealment Table (p. 107). Fire at concealed units is Area Fire; mixed concealed and unconcealed targets resolve on their own columns with one DR (A12.13). A Dummy stack out of all enemy LOS uses Morale Level 7; one in LOS loses "?" by the Concealment Table and so ceases to exist. A revealed hidden unit is placed without "?" (A12.31).

**Design.**

1. *Dummies.* The vocabulary gains the Dummy (`asl:dummy`, a concealment counter with no unit beneath); setup may place Dummy stacks, which a side's view already seals like any concealed stack.
2. *Package revision.* The Fire package resolves hidden units (as concealed, A12.3) and Dummies, with the Concealment Table transcribed from page 107.
3. *Fire at a Location with no known target.* Ruling R21.1. If admitted, the action commits whatever the Location holds, even nothing, so a refusal no longer tells the firing side that a hidden unit is there. That closes the known limitation of step 18, part 5.
4. *Visibility.* A hidden unit's result is the owner's; the public report carries the arithmetic. A Dummy's removal is public.

**Rulings** (accepted by the user, 2026-09-26; items left out are in the backlog).

| Id | Question | Answer |
|---|---|---|
| R21.1 | May a side fire at a Location where it sees nothing? | Yes; the attack commits with its roll and markers, and resolves against whatever is there. |
| R21.2 | A Dummy in LOS | Resolved by the Concealment Table: removed on PTC or worse. |
| R21.3 | A revealed hidden unit | Placed without "?" (A12.31). |
| R21.4 | Mixed hidden, concealed, and known targets | One DR, each type on its own column (A12.13). |
| R21.5 | Dummy counters | A Dummy has no printed values; how many each side has is a setup fact. |

## 7. Step 22: movement and fire during it

This is the largest Infantry step, because Defensive First Fire needs movement that can be interrupted after every MF, and live play today moves only by entering a building.

**Rules.** A4.1 to A4.62 (pp. 48 to 51) and the B Terrain Chart MF costs (p. 160); A8.1 to A8.15 (p. 59): DFF against the moving unit or stack in the Location where it spends MF, with FFNAM and FFMO (A4.6, p. 51); A8.3 and A8.31 (p. 61): Subsequent First Fire and FPF, with FPF's NMC on the firer; A8.2 to A8.26 (pp. 60 to 61): Residual FP; A8.4 (p. 61): First-Fire-marked units in the DFPh; A7.83 (p. 58): a pinned mover.

**Design.**

1. *Movement.* `asl.game.move` moves a stack one hex (or spends MF within a Location) at the terrain's MF cost, recording MF per Location and ending with an explicit end-of-move. Assault Movement is declared at its start (ruling R22.2). The existing building entry becomes one case of it.
2. *A defender's window.* After each MF expenditure the game holds a pending movement: the DEFENDER may fire (`asl.game.fire` with the MPh admitted) or pass, and the ATTACKER may not spend more MF until he does (ruling R22.1). The Play page shows the window to the DEFENDER.
3. *Package revision.* DFF (target: the moving stack only; FFNAM and FFMO per firer), SFF (Area Fire, range capped by the closest armed Known enemy, attacks limited by MF spent), FPF (with the firer's NMC from the Original DR), the First Fire state, and A8.4 in the DFPh.
4. *Residual FP.* A Location marker (ASL-UNIT-026 entity) placed after DFF, SFF, or FPF at half the highest column used, reduced by A8.26, which attacks a unit that enters or spends MF there, first and automatically, and is removed at the end of the MPh.
5. *Vocabulary asl@1.6.0.* `asl:first-fire` and the Residual FP entity, drawn.

**Rulings** (accepted by the user, 2026-09-26; items left out are in the backlog).

| Id | Question | Answer |
|---|---|---|
| R22.1 | The defender's window | Explicit: a pending movement the DEFENDER closes by firing or passing. |
| R22.2 | Assault Movement; Hazardous Movement | Assault in; Hazardous out. |
| R22.3 | Which terrain may be entered | The Fire package's terrain list (Open Ground, brush, woods, orchard, grain, ordinary buildings) and roads through them; no Bypass, no level changes, no hexside terrain. |
| R22.4 | A8.14 follow-up fire | In: different attackers, or 2 or more MF spent. |
| R22.5 | FPF | In; a firer's NMC Original DR of 2 follows R0.2. |
| R22.6 | TPBF (A8.312) | Out, since entry into an enemy Location stays refused. |
| R22.7 | Residual FP values | The Residual FP counter values of A7.372 (p. 57), and one attack of the whole moving stack with each unit's own MC. |
| R22.8 | Snap Shots, Bypass, CX | Out. |

## 8. Step 23: support weapons and MGs on the IFT

**Rules.** A4.4 to A4.44 (p. 50): possession, transfer, Recovery; A7.35 to A7.353 (p. 56): who may fire a SW and what inherent FP it costs; A7.53 and A7.531 (p. 57): direction of weapons; A7.9 (p. 58): a cowering unit's SW; A9.1 to A9.12, A9.2 (p. 62): MG usage and multiple ROF on the colored dr; A9.3 (p. 63): Sustained Fire; A9.4 to A9.52 (p. 64): long range direction and Spraying Fire; A9.7 to A9.72 (p. 65): malfunction, several SW in a group, Repair in the RPh; the Support Weapons Chart (p. 108).

**What is already there.** The vocabulary has `asl:sw`, `asl:mg`, and their attributes (firepower, range, breakdown, rate of fire, repair); the state model has equipment possessed by a unit, and the synthetic game already draws an LMG held by a squad.

**What is missing.** Equipment has no catalog definition (so no printed FP, ROF, B#, or R#); the dice records do not mark a colored die; there is no transfer, Recovery, or Repair action.

**Design.**

1. *Catalog asl-scenario-a1@1.2.0.* SW definitions (LMG, MMG, and any other the scenario uses) with printed values from the user; equipment instances gain a definition reference.
2. *Colored die.* `dice-rolled` records which die is colored, for ROF and later Target Facing and hit location.
3. *Package revision.* SW firepower in a fire group, inherent FP forfeits (A7.351 to A7.353), leader direction limits, malfunction on the Original DR (A9.7) with Random Selection among several SW (A9.71), multiple ROF (A9.2) as a follow-on attack by the same weapon, and Sustained Fire.
4. *Actions.* `asl.game.fire` names the weapons each firer uses; `asl.game.repair` in the RPh (A9.72), exclusive with a rally attempt by the same unit (A3.1).
5. *Markers.* `asl:malfunctioned` exists; a SW carries its user's Prep or Final Fire state.

**Rulings** (accepted by the user, 2026-09-26; items left out are in the backlog).

| Id | Question | Answer |
|---|---|---|
| R23.1 | Which SW | The weapons the Scenario A1 card gives each side, with values manufactured under R0.3 and approved by the user. |
| R23.2 | Multiple ROF | In, as a new attack by the weapon alone, marked like the first. |
| R23.3 | Sustained Fire, Spraying Fire, Fire Lanes | Sustained Fire in; Spraying Fire and Fire Lanes out (Fire Lanes need step 22's Residual FP and a lane marker). |
| R23.4 | Transfer, Recovery, dismantling, captured SW | Out; weapons stay with the unit that sets up with them. |
| R23.5 | FT, DC, MOL | Out. |

## 9. Step 24: ordnance (a later pass)

Chapter C (pp. 162 to 191) resolves ordnance in two steps: a To Hit DR against a TH# from the target type and range (C3), modified by the gun and ammunition (C4) and by firer and target Cases A to R (C5, C6, 26 with their sub-cases), then its effect on the IFT (HE) or a To Kill table (C7). ROF uses the colored die (C2.24); acquisition (C6.5) is a state across attacks; Critical Hits (C3.7) and hit location (C3.9) follow the dice. Light mortars (C9.2) and LATW (C13) belong here too.

A minimal slice, a Gun firing HE at Infantry, still needs the Covered Arc and facing, the To Hit table with C4, eight to ten Cases, ROF, Critical Hits, acquisition, breakdown on the To Hit DR, and Gun definitions from the user. It is its own review step and its own live step, after step 23's colored die and equipment definitions.

## 10. Step 25: vehicles (a later pass)

Chapter D (pp. 192 to 221) needs a vehicle catalog, vehicular movement with Motion and facing (D2, D3.1), armor factors and target size (D1.6, D1.7), BU and CE with the crew's exposure (D5.2 to D5.32), collateral attacks (D.8), vehicular MGs (D1.8, D3.5), Target Facing on the colored die (D3.2), and ordnance for any attack on an AFV. Vehicle Notes (Chapter H, from p. 328) lie outside the registered pages.

The smallest useful slice, a vehicle's MGs firing on the IFT and Infantry firing at an unarmored vehicle on the IFT Vehicle line (A7.308, p. 55) with collateral attacks on a CE crew, still needs vehicle definitions and a placement state. Fire at or by an AFV's main armament waits for step 24. This is two or three steps.

## 11. Executing steps 19 to 23 in one pass

The Windows LF clone and Docker checks take about ten minutes run in parallel, and each merge needs both. One implementation pass runs them once for five steps.

### 11.1 Stages

1. **This plan** (documents only): the requirements' steps 19 to 25, this document, and the backlog. Its rulings were accepted on 2026-09-26.
2. **One review sitting.** For steps 19 to 23 together: the rule fragments of sections 4 to 8 registered and verified against their pages, transcriptions of the registered chart pages each step uses (with the user's spot-checks), and one review document per package recording the rulings. The user supplies the SW and Dummy counter values at this point. No live code changes in this stage, so it needs no checks of its own beyond the targeted tests.
3. **One implementation branch**, `feature/asl-unit-rally-fire`, built in dependency order so each part stands on the last:
   1. Shared model: vocabulary asl@1.6.0 (First Fire, Dummy, Residual FP entity), catalog 1.2.0 (SW definitions), equipment definitions, the colored die, ADJACENT in the map read.
   2. Step 19, Rally: first the Fire package's Original 2 by ruling R0.2, then the Rally package, DM, the action, and the RPh panel.
   3. Step 20: AFPh and multi-Location groups in the Fire package and the action.
   4. Step 21: hidden and Dummy targets; fire at a Location with no known target.
   5. Step 23: SW and MGs, Repair in the RPh.
   6. Step 22: movement, the defender's window, DFF, SFF, FPF, Residual FP. Last, because it is the largest and changes the MPh that the others do not touch.
   7. Documents: a design for each step, the requirements' done notes.
4. **Checks once.** After the last part, the Windows LF clone and Docker runs start together, as for step 18. Then the user is asked to merge.

### 11.2 Keeping one pass safe

- **A commit per part**, each with the full ASL solution's Debug tests run locally before the next part starts (about four minutes), so a failure is found in the part that caused it, not at the end.
- **Packages first within each step.** A package's reachability walk (as `ScenarioA1FireReachabilityTests` does) runs before any live wiring, so an undecided branch surfaces while the rulings are fresh.
- **A stop point.** If step 22 grows beyond this design (for example, movement needs rules the review did not cover), steps 19 to 21 and 23 can merge without it after one check run, and step 22 follows in its own.
- **Package digests.** Each republished package keeps its earlier digest in the record, as step 18 did for `scenario-a1-fire`.

### 11.3 What the user provides

- Rulings on the tables in sections 4 to 8 and 12: accepted on 2026-09-26.
- Approval of the manufactured SW values for the Scenario A1 weapons (ruling R0.3); ordnance and vehicle values come from the Chapter H listings.
- Spot-checks of the new chart transcriptions against rendered page images.

## 12. Source ruling

| Id | Question | Answer |
|---|---|---|
| R0.1 | Transcribe new charts from the registered copies (pp. 106 to 111, 160, 189 to 191, 220 to 221) rather than registering back-matter pages? | Yes. The step 17 supplement for pp. 692 and 698 stays as recorded. |
| R0.2 | An Original MC or rally DR of 2 and Heat of Battle (section 3.4) | Record a deviation for this pass: resolve the 2 as its MC or rally result, say in the record that Heat of Battle was not taken, and review A15 first in the next pass. |
| R0.3 | Counter sources (section 3.3) | **Ruled by the user, 2026-09-26:** where no source gives a printed value a scenario needs, the value is manufactured. Chapter H's listings and the Counter Examples are registered as a bounded supplement for the values they do give. |
