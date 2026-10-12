# ASL Unit Backlog Pass 50 Design

**Status:** The frame (increment 1), written 2026-10-12. Read-only: nothing is built. The frame's questions are answered by their own proposals under the user's word of 2026-10-12 for this pass ("work independently as much as possible up to merge gate and then pause"); each answer is open to a veto. Increment 2a (50.1, 50.2) is next.

**Date:** 2026-10-12

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 19 (decisions 5 to 7) and section 22.1, "Pass 50: Armored combat I"; the [rule inventory](<../ASL Rule Inventory A to E.md>); the [backlog](<../ASL Unit Backlog.md>), sections 55 to 57; the [pass 45 design](<ASL Unit Backlog Pass 45 Design.md>) and [review](<Scenario A1 Backlog Pass 45 Review 2026-10-12.md>); the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>); the session's prompt, `Prompts/Pass 50 Prompt I.md`.

**The rulebook.** Read for this frame on 2026-10-12, from the page texts of the tools folder, with their examples: page 52 (A5.132), page 64 (A9.4 to A9.61), page 65 (A9.611, A9.7, A9.71), page 167 and the top of page 168 (C2.24, C2.2401), page 169 (C3.22), pages 172 and 173 (C5.11 to C6.17) with the top of page 174, page 195 (D1.8 to D2.12), page 196 (D2.17, D2.18), page 198 (D2.35, D2.41, D2.42), and pages 199 to 201 (D2.6 to D3.7, with the two long examples). The AP To Kill Table's lowest columns were read from its transcription (`c7-to-kill-tables.transcription.json`): a MG's Basic TK# is 4.

**What the frame rests on.** One read-only agent mapped the code on this branch (cut from main at b4ce31ef) for the pass's ground: how a vehicle fires on the IFT, how its weapons malfunction and are repaired, the ordnance shot and its record's check, the vehicle's steps and the DEFENDER's window, the page's panels, the tests, and the catalog's three armed vehicles. Its findings are cited below as found, with file and member; I read `ScenarioA1ArmorReference.cs` myself. Each increment's design reads its code before it builds.

## 1. Outline of this design

1. This outline.
2. What the pass is, and its increments.
3. The 39 rows, by task.
4. What is certain, and what needs a read.
5. What the frame found outside the rows.
6. The frame's questions, each with its analysis and its answer.

Sections for the increments, the reviews, and "As built" are added as the pass goes.

## 2. What the pass is

Pass 50 is the first pass of block 2, armored combat: a tank's weapons one by one, its MA while it moves, and MG fire against armor. It adds no counters. The catalog's armed vehicles are three: the PzKpfw IIIH (`attacker-tank`: T turret, 50 mm, ROF 2, B12, BMG 3, CMG 5, no AAMG), the T-34 M41 (`defender-tank`: RST turret, 76L, no ROF, B12, BMG 2, CMG 4, no AAMG), and the SPW 251/1 (`attacker-halftrack`: MA AAMG 3, ROF 1, OT). No vehicle of the catalog has a tank's AAMG, a hull or turret Rear MG, a Fixed-Mount BMG, a FT, Secondary Armament, or a Gyrostabilizer; the rules for those are named in section 3 and not built.

The plan gives the pass six tasks, 33 rows, 5:00 of building, and 6:15 in all. The inventory names 39 rows: earlier passes moved six here.

**What the frame leaves out of the plan's entry, first** (each decided under the user's word of 2026-10-12, each with a backlog row in section 58 when the documents are written):

- **Task 50.4, Gun Duels (C2.2401), is not built.** A Gun Duel is a choice the ATTACKER makes after the DEFENDER has declared an attack and before it is resolved. The game has no declared attack: a proposal is resolved with its dice in one step. A Gun Duel therefore needs a declaration that stands open between the two sides, an answer from the other side, and two resolutions in an order the Final DRs may decide. That is a design of its own (question F5).
- **A hit on another vehicle of a vehicle-overstacked hex (A5.132)** stays out (question F1).
- **OVR Prevention (C5.64)** stays out and goes to pass 110 (question F1).
- **The declared higher MP expenditure of D2.18** is decided at task 50.3 after the step's check is read; the frame expects to leave it (section 4).
- **A VCA change in woods, a building, or rubble for a shot** (C5.11's Bog Check) and **every rule of a weapon the catalog lacks** (section 3).

**The increments:**

| Increment | Tasks | Why together |
|---|---|---|
| 1 | The frame | This document. |
| 2a | 50.1, 50.2 | A vehicle fires weapon by weapon: which weapons have fired in a phase, each one's malfunction and repair, and the BMG and CMG on the IFT. One request field, one state list, one panel. |
| 2b | 50.3 | The MA while moving: Bounding First Fire with Cases C, C1, C2, and C4, Delay, the TCA changed with an MP expenditure, Bounding Fire in the AFPh, and a turret turned without fire. |
| 2c | 50.6, 50.5 | Two additions to the Vehicle Target Type: the Target Facing from inside the target's hex, and a MG's To Kill attack. |

## 3. The 39 rows, by task

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 50.1 | D1.8 (195) | The b/c/a FP are read; a vehicular FT is not built | 50.1 for the three MGs. A FT: no counter; stays, backlog. |
| 50.1 | D1.81 (195) | BMG fire within the VCA, Normal Range 8; hull RMG; Fixed-Mount +1 | 50.2. RMG and Fixed Mount: no counter; backlog. |
| 50.1 | D1.82 (195) | CMG fire through the TCA, Normal Range 12; loss of Acquisition; turret Rear MG; CMG VCA Only | 50.2, with the loss of the MA's Acquisition when the CMG fires at another target. Rear MG and VCA Only: no counter; backlog. |
| 50.1 | D3.5 (200) | One attack a Player Turn for each MG; all weapons in one fire phase; FP added or fired apart; the Mandatory FG | 50.1 (which weapons fired) and 50.2 (added or apart). |
| 50.1 | D3.7 (201) | Repair of each weapon by its own dr; only the AAMG needs a CE crew; Recall when the MA is disabled | 50.1. SA and a Hero Rider: no counter; backlog. |
| 50.2 | D2.35 (198) | No bow-mounted fire outside the VCA in Bypass | 50.2. The Bypass TCA of D2.321: pass 55. |
| 50.2 | D3.51 (201) | Other turret weapons pay the first one's CA penalty; a further change counts from the current TCA; the TCA turns with the VCA; one hex for all weapons | 50.2. The Known-target condition: backlog (question F4). |
| 50.2 | D3.52 (201) | Case A as an IFT DRM for a BMG or CMG fired outside its CA | 50.2 |
| 50.2 | D3.11 (199) | The VCA for bow-mounted weapons | 50.2 |
| 50.2 | D2.11 (195) | A VCA changed by firing outside the CA or at the end of a fire phase | 50.2 for the BMG's pivot outside woods, buildings, and rubble (question F4); 50.3 for the end of a fire phase. After a Motion Attempt: pass 55 (D2.401 is not built). |
| 50.2 | A7.21 (54) | A vehicle's MG at another level of its hex | 50.2: refused, as now (question F7). IFE: no counter. |
| 50.3 | C5.13 (172) | No Case A in the firer's own MPh; the CA changes by MP only | 50.3 |
| 50.3 | C5.3 (172) | Case C in the AFPh for a vehicle that entered a new hex; Case C for every MPh shot; an MP between shots | 50.3. Passenger ordnance: pass 65. |
| 50.3 | C5.31, C5.32, C5.33 (172) | Cases C1 and C2 by the MP spent in continuous LOS; Case C at the outset | 50.3. The Gun Duel clause of C5.33: with 50.4. |
| 50.3 | C5.35 (172) | Case C4: the lower dr doubled; a Gyrostabilizer | 50.3 for the doubled dr. Gyrostabilizer: no counter; pass 55. |
| 50.3 | C3.22 (169) | A turret or VCA changed without fire at the end of a fire phase | 50.3, for the turret, read as ruling R8.9 reads it for a Gun (question F6). |
| 50.3 | C6.16 (173) | MP left at the MPh's end are spent in the last hex | 50.3: a read of the `remain` step; the frame expects it built. |
| 50.3 | D2.17 (196) | Delay | 50.3 |
| 50.3 | D2.18 (196) | A declared higher MP expenditure on entry | 50.3 decides after its read (section 4). |
| 50.3 | D2.41 (198) | The +2 of DC and MOL against a Motion vehicle | Not this pass: those attacks on AFVs are pass 60's. The row's pass column moves. |
| 50.3 | D2.42 (198) | Case C4 for ordnance in Motion; Passengers' Motion Fire | 50.3 for the MA. Passengers and Riders: pass 65. |
| 50.3 | D3.12 (199) | The TCA changed freely with each MP; at the end of a fire phase; doubled MP in woods and buildings | 50.3. In woods, a building, or rubble the change with an MP is refused (backlog): the doubled expenditure is a cost rule of its own. |
| 50.3 | D3.3 (199) | Bounding First Fire by a tank's MA; the counter; moving again after it | 50.3 |
| 50.3 | D3.31 (200) | A non-MA weapon fired in the MPh bars the AFPh | 50.3. Canister and the Gyrostabilized CMG: no counter; pass 55. |
| 50.3 | D3.32 (200) | The MA that kept its ROF in the MPh fires once in the AFPh with Case C | 50.3 |
| 50.3 | C5.4 (172) | The +2 for each instance of Area Fire on the firer | Not this pass: pass 110 (LATW). The row's pass column moves. |
| 50.3 | C6.1 (173) | The firer's half for a tank that only Started and Stopped | 50.3: Case B, since C5.2 gives Case B to a firer that "did not enter [its] current hex/hexside" and C5.3 gives Case C to one that did. The target's half (backlog 55.2) is unchanged. |
| 50.3 | A8.4 (61) | A vehicle's Final Fire after its First Fire (ruling R6.9) | Closed by the page: D3.5 gives a vehicle's MG one attack a Player Turn, so it has none. The MA's (C2.241, as Intensive Fire by a vehicle): pass 110. |
| 50.3 | A9.3 (63) | Sustained Fire by a vehicle's MA (ruling R6.9) | Closed by the page: D3.5 again. |
| 50.4 | C2.2401 (167) | Gun Duels | Not built (question F5). |
| 50.5 | A9.6 (64) | MG fire at an armored Target Facing on the To Hit and AP To Kill Tables | 50.5 |
| 50.5 | A9.61 (64) | The To Kill attack: alone, in Normal Range, no halved FP, predesignated; the Stun on the Kill Number | 50.5 |
| 50.5 | A9.611 (65) | The Collateral Attack on Vulnerable PRC | Not built: Collateral Attacks (D.8A) are pass 65's. |
| 50.5 | D3.54 (201) | Only a vehicle's MA MG makes a To Kill attack | 50.5: the halftrack's AAMG if the read at the task shows it contained; otherwise the bar alone. |
| 50.6 | D3.2 (199) | The Target Facing by the colored dr from inside the target's hex | 50.6. FT, MOL, and DC from the same hex: pass 60. |
| (none) | A5.132 (52) | A hit on another vehicle of a vehicle-overstacked hex | Left out (question F1). |
| (none) | A8.311 (61) | OVR Prevention | Pass 110 (question F1). |
| (none) | D2.11, D3.11 | (placed above) | |

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **A vehicle is one firer with one weapon.** The fire request names no weapon. `FireVehicleFire` has no weapon field; `VehicleFireOutside` accepts only a definition whose MA is an AAMG, so a tank never fires on the IFT outside an OVR or CC. The vehicle's fire counters, its `asl:malfunctioned`, and its ROF record (`OrdnanceShots`, keyed by the vehicle's id) are the vehicle's own.
- **Per-weapon conditions exist in part.** `asl:malfunctioned` and `asl:disabled` are the MA's; `asl:bmg-malfunctioned` and `asl:cmg-malfunctioned` are set by an OVR's malfunction and cleared by nothing. No condition says a BMG or CMG is disabled.
- **Repair** takes the vehicle as unit and equipment, asks for a CE crew whatever the weapon (the page asks it of the AAMG alone), and repairs only `asl:malfunctioned`. A tank's malfunctioned MA is offered on the page as "AAMG". A disabled MA Recalls no one: the only vehicle whose weapon the code repairs can carry Passengers, which D3.7 exempts.
- **No phasing ordnance fires in the MPh.** `LiveOrdnance.FromState` refuses it by the window test, and the calculator's `phase-outside` has no case for it. A vehicle that moved is refused in the AFPh, and one in Motion everywhere. No Case C, C1, C2, or C4 exists as a To Hit DRM.
- **Case A** is charged whenever the target lies outside the current TCA; the TCA changes only with a MA shot; it is stored absolute (`GameState.TurretFacings`) and does not turn with the hull, which D3.51 says it does (backlog 55.2 has the row).
- **The MP a mover spent in LOS** is read from the phase's `vehicle-step` events (`GamePlanner.MpInLos`) for Cases J1 and J2. LOS being the same both ways, the same read with the target's Location in the Gun's place gives the MP of Cases C1 and C2.
- **A MG's Basic TK# is 4** on the AP To Kill Table, and the table's Case D rows have a column for 25 mm and less. An ATR already fires through the ordnance shot as a possessed weapon; a MG's To Kill attack can take the same road.
- **Ordnance at a vehicle in the firer's own Location is refused,** so D3.2's colored dr is never asked.

**Needs a read at its task.**

- 50.1: what `FireSpent`, the fire markers, and the Mandatory FG do once a vehicle's second weapon may fire in a phase; the CE test of the repair in the planner against the projector's (the differing copies pass 32 kept apart).
- 50.2: how the fire record carries a facing, since a CMG's shot outside the TCA turns the turret and a BMG's pivot turns the hull; the BU rule of D5.2 for MG fire, which the frame did not read.
- 50.3: whether an `enter` step's check accepts a cost above the computed one (D2.18); what a `remain` step leaves in the Location for C6.16; whether `PlanTurnGun` takes a tank.
- 50.5: how the ATR's `GunDefinition` is built, and what a possessed MG needs to be one.
- 50.6: which member refuses the same-Location shot, and whether Case E in the armor run exists.

**New facts in a record, planned now** (the `PROOF_SKIP` and `PROOF_SKIP_JSON` lists; question F8): on a fire record's vehicle firer, the weapons that fire and the Case A each pays; on an ordnance shot, the Bounding First Fire kind, the MP in LOS, the Motion firer, a MG as the weapon, and the same-hex facing; on a vehicle step, the turret's new facing; in the state, the list of a vehicle's weapons that fired this phase, and the conditions of a disabled BMG and CMG.

## 5. What the frame found outside the rows

1. **Two rows close on the page alone.** A8.4's and A9.3's remainders both rest on ruling R6.9, "a vehicle's Final Fire after its First Fire". D3.5 gives a vehicle's MG one fire attack a Player Turn unless it is a MA with a Multiple ROF, so the MG has neither. Nothing is built; the rows are corrected.
2. **The CMG and Mandatory Fire Direction.** A9.4's exception: a CMG needs no leader beyond 16 hexes and does not treat Infantry there as concealed. D1.82 says the same. 50.2 builds the CMG's range as its own (Normal Range 12, Long Range to 24).
3. **The step bar of a TI vehicle** (backlog 55.2) is named by the prompt for this pass. It is a boundary repair with no rule of the pass behind it; 50.3 takes it if its read shows one member moved, and otherwise it keeps its row.
4. **A crew offered as a firer on the page (ruling R24.8)** is not this pass's: no row asks it.
5. **The inventory's count.** The summary row says 39, and the script counts 39.

## 6. The frame's questions

Each is answered by its proposal, decided under the user's word of 2026-10-12. Those that become rulings are numbered R50.n when their task is built.

**F1. The rows the plan does not cite.** A7.21, A8.4, A9.3, C6.1, C6.16, D2.11, D3.11 are placed in section 3. **A5.132:** the rule gives a missed shot at one vehicle a chance to hit another of the hex when the target's side has overstacked it with vehicles; it needs a count of the hex's vehicles in the firer's LOS, a subsequent dr, a Random Selection weighted by size, and a second To Kill. No game of the catalog's five vehicles overstacks a hex by chance, and the rule stands on vehicular overstacking, which pass 40 built for Infantry only. **Answer: left out; its row goes to pass 55.** **OVR Prevention (C5.64):** a non-vehicular Gun's extra shot when it is about to be overrun, with the To Hit DR as a NMC on its crew. It is a Gun's rule with nothing of this pass in it. **Answer: pass 110.** **C5.4:** pass 110. **D2.41:** pass 60.

**F2. The order.** The weapons first (50.1, 50.2), because the MA's Bounding First Fire and the AFPh bar of D3.31 both read which weapons have fired. **Answer: the increments of section 2.**

**F3. How a vehicle's weapon is named and remembered.** The page: each MG fires once a Player Turn; all of a vehicle's weapons fire in one fire phase; their FP may be added for one attack or fired apart; "it makes no difference which weapon fires first". **Answer:** the fire request gains `vehicleWeapons`, a list of `bmg`, `cmg`, `aamg` for a vehicle firer; with none named the vehicle fires as it does today (the halftrack's MA). The state gains a list, cleared at each phase change, of the weapons each vehicle has fired in the phase; a weapon may fire when the vehicle carries no fire counter of an earlier phase and the weapon is not on the list. The MA's own ROF stays where it is. The counters stay the vehicle's: one Prep, First, Final, or Bounding Fire counter for all its weapons, which is what the board shows. A vehicle's MGs fire with no Infantry and no director, as now.

**F4. Fire outside the CA (D3.51, D3.52, D2.35).** A CMG fired outside the TCA turns the turret and adds the MA type's Case A to the IFT DR; a BMG fired outside the VCA pivots the hull, adds the NT Case A (3, then 1 a hexspine), and the turret turns with the hull. The first shot of each other weapon in that phase pays what the first paid. **Answer:** built, with three limits. (a) No pivot in woods, a building, or rubble: C5.11 asks a Bog Check first, and that is refused with its reason. (b) No pivot in Bypass (D2.35), and none in the MPh (C5.13). (c) D3.51 allows a further change of CA in a phase only if the earlier shots were at a Known enemy unit; the attack's record does not say whether its target was Known, so a further change is allowed whatever the earlier target. Backlog rows for (a) and (c).

**F5. Gun Duels (C2.2401).** The page: against a declared Defensive First Fire attack on it, a vehicle may fire its MA first if its Gun Duel DRM is lower; if the two are equal the lower Final DR fires first, and a shot is void when its firer is eliminated, broken, stunned, or shocked first; after it the DEFENDER may declare again and the ATTACKER duel again with the ROF as a DRM. Three things are missing under it. There is no declared attack: every proposal draws its dice. There is no answer by the other side inside a proposal. And one plan cannot hold two resolutions of which the second depends on the first's result. A standing order given beforehand ("this vehicle duels whenever it may") would avoid the answer but not the other two, and takes the choice the page gives after the declaration. **Answer: not built in this pass; the row goes to pass 55 with these three needs written into its backlog row.** The plan's 0:40 for it was for the calculator; a calculator nothing calls is not built either.

**F6. A turret turned without fire (C3.22, D3.12).** The page allows it "only at the end of a friendly fire phase", if the weapon could still fire without Intensive or Sustained Fire; in the PFPh it cancels the vehicle's movement for the Player Turn. Ruling R8.9 reads this for a Gun as: at any moment of the friendly fire phase, and no more fire in that phase. **Answer:** the same reading for a tank's turret, through the action a Gun uses: allowed in the PFPh, AFPh, and DFPh for a tank whose turret weapons may still fire; afterwards no weapon of the vehicle fires in that phase, and after a PFPh turn it does not move. In the MPh the turret turns only with an MP expenditure (D3.12). A VCA change at the end of a fire phase is left: it is a pivot with its Bog Check. Backlog row.

**F7. A vehicle's MG at another level of its hex (A7.21).** PBF there needs a stairwell, and a vehicle stands at ground level; its BMG and CMG are fixed in the hull and turret. No rule read gives a vehicle's MG fire up a stairwell or bars it. **Answer: refused, as now, with the reason the page's Range tab already gives; a ruling, R50.n at task 50.2.**

**F8. The facts a record gains, and older records (ruling R45.4).** Pass 45's lesson: a fact added to a record must be taken from the record at replay for an older record, and a rule that changes a result needs a fact that says the attack was planned under it. **Answer:** every fact this pass adds to a fire record or an ordnance shot is absent from an older record and is then not rebuilt from the state. For each, the task's design says what the record's check does with a record that has none, and the replay digest runs over the store right after the commit that adds it. The rules of this pass that change a result of an attack that could be made before: none found at the frame. A tank's MG fire, the MA in the MPh, the AFPh shot after a move, the shot from Motion, the same-hex shot at a vehicle, and a MG's To Kill attack were all refused before; a recorded game holds none. The repair of a tank's weapon is the one exception: a recorded repair of a halftrack's AAMG must check as it did, which 50.1's design shows.

**F9. The estimate.** The plan's 5:00 of building less Gun Duels (0:40) is 4:20; the BMG's pivot and the turret turned without fire are inside the plan's words for 50.2 and 50.3. With the plan's overhead of 1:15 the pass to the gate is 5:35. **Answer: accepted.**

## 7. A vehicle fires weapon by weapon: 50.1, 50.2 (increment 2a)

Built on 2026-10-12 in one commit, since the two tasks share every file: the weapons a vehicle's attack names are 50.2's and what the state remembers of them is 50.1's.

### 7.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 58.

- **A BMG fires only inside the VCA. The pivot of the hull for a shot is not built,** against the answer to question F4. The reason: D3.51's own example charges a turret weapon the NT penalty of the pivot and then its own turret penalty on top ("+5 Case A DRM (+3 [one hexspine change for NT Gun Type] +2 [two further TCA hexspine changes for T Gun Type])"), with the turret carried round by the hull. That is two remembered penalties for each mount and a turret that turns with the hull, which the state does not do (backlog 55.2: the TCA is kept absolute). The CMG's turret turn, which stands alone, is built. D2.35's bar on a bow weapon outside the VCA in Bypass therefore holds everywhere.
- **A CMG fired at another target does not lose the MA's Acquisition (D1.82).** Section 3 placed it in 50.2. The loss of an Acquisition is not an event the game has (an Acquisition is only ever changed by a shot of its Gun); backlog 55.2 already has the row for an Acquisition's other losses, at pass 110, and the CMG stays in it.
- **A CMG's CA in Bypass (D2.32, D2.321)** is read as its TCA is anywhere else. Bypass's own TCA is pass 55's.
- **The Known-target condition of D3.51** is not read, as question F4 said.

### 7.2 What is built

**The request.** `asl.game.fire` already carries `weapons`, a map from a firer to the SW it uses. A vehicle's entry in that map names `bmg`, `cmg`, or both. A tank with no entry is refused with the reason (`play.fire-vehicle-weapons`: its MA fires as ordnance); a vehicle whose MA is its MG (the halftrack) names none and fires as before.

**Rules.** `FireVehicleFire.Weapons` holds a `FireVehicleWeapon` for each MG named: whether it has malfunctioned, whether it has fired this Player Turn, the hexspines its CA must turn (a map read), and the Case A it maintains. The calculator:

- asks that each named MG is printed on the counter, works, and is named once (D1.8); that a BMG's turn is none (D1.81) and that nothing turns in Bounding First Fire (C5.13); that the CMG of a CE tank with a RST or 1MT turret does not fire (D1.321). Neither MG asks for a CE crew (D1.83);
- gives each MG its own FP line with its own Normal Range, eight hexes for a BMG and twelve for a CMG (D1.81, D1.82), and adds them (D3.5). The attack reaches twice the Normal Range of the nearest-ranged MG that fires. A CMG alone reaches 24 hexes with no leader, which is A9.4's exception;
- adds Case A to the IFT DR for a CMG whose turret turns: the To Hit DRM of the turret's type (D3.52), doubled in woods, a building, or rubble (C5.11);
- malfunctions a MG on an Original 12 (D3.7: "a printed B# < 12 applies only to the MA"), and of two that fired together finds the one by Random Selection (A9.71), on the roll kind the OVR already uses.

**The state.** `GameState.VehicleWeaponShots` lists, until the Player Turn ends, each weapon a vehicle fired: `ma`, `bmg`, or `cmg`, the fire phase, and the hexspines its CA changed. The projector appends to it from a vehicle's fire record and from a tank's ordnance record. From it Rules decides, for a MG (`WithVehicleWeapons`) and for the MA (`OrdnanceVehicleFirer.Weapons`):

- a MG that has fired does not fire again in the Player Turn (D3.5);
- no weapon fires once another fired in a different fire phase ("must fire in the same phase or forfeit");
- the vehicle's fire counter does not bar the MA when only its MGs placed it, in this fire phase ("it makes no difference which weapon fires first");
- the first shot of the MA, or a CMG's, within a TCA that the other turned in this fire phase pays what that one paid (D3.51).

A fire record gains `turretFacing` when a CMG turned the turret, and the projector sets the TCA from it; the record's check holds the turn to the hexspines its facts say.

**Repair.** `asl.game.repair` takes `weapon` for a vehicle: `ma`, `bmg`, or `cmg`. One Rules member, `VehicleWeaponRepairBar`, is read by the planner and the projector: the weapon is malfunctioned and not disabled, its crew is not shocked or stunned, and it has had no attempt this RPh. A 1 repairs; a 6 disables, under the new conditions `asl:bmg-disabled` and `asl:cmg-disabled` for the MGs; a tank whose MA is disabled is Recalled, since no tank of the catalog carries Passengers or tows. A repair record gains `weapon`.

**The page.** A tank is a firer in the Fire panel while a MG of it may fire; its BMG and CMG are listed with their FP beside the SW, each with the reason it may not be ticked. The vehicle panel's Bounding First Fire lists them too. The repair list names each malfunctioned weapon of a tank, and no longer calls a tank's MA an AAMG.

### 7.3 Rulings, decided under the user's word of 2026-10-12

- **R50.1.** A vehicle's weapons are named in the fire request under the vehicle's own id; the state remembers which fired and in which fire phase; the vehicle carries one fire counter for all of them (question F3).
- **R50.2.** "The same fire phase" of D3.5: for the side that is not phasing, Defensive First Fire in the MPh and Final Fire in the DFPh are one, as A.15's example has it ("an AFV firing its CMG as Defensive First Fire may still fire its MA in its DFPh"). For the phasing side the PFPh, the MPh, and the AFPh are three; D3.32's leave for the MA after Bounding First Fire is task 50.3's.
- **R50.3.** D3.51's "same CA Change penalty as the first weapon which fired": the Case A of the first shot of the fire phase that turned the turret. The MA's first shot of the fire phase, or the CMG's, pays it when it fires within the TCA as it now stands; a weapon that turns the turret again pays for its own turn alone.
- **R50.4.** A BMG fires only inside the VCA (section 7.1).
- **R50.5.** A vehicle's MG does not fire at another level of its own hex (question F7). The attack is refused as out of range, as before.
- **R50.6.** The Recall of a tank whose MA is disabled is the Recall of D5.341 as the game has it: "treated the same as a Stun" until the Player Turn ends, then the counter's Recall side. A crew so Recalled repairs nothing more in that RPh.
- **R50.7.** A tank's Gun MA, BMG, and CMG are repaired by a crew that is not shocked or stunned, BU or CE. The halftrack's MA AAMG keeps the CE test, and the two copies of that test (the planner's and the projector's, which pass 32 kept apart) stay as they are: a recorded repair must check as it did.

### 7.4 The new facts, and a record made before the pass

- **A vehicle's fire record** (`vehicleFire.weapons`, `otherPhase`, `inWoodsOrBuilding`; `turretFacing` on the event; `vehicleWeapon` on a weapon effect). A record made before the pass names no weapons; it is read by the old path whole, and none of these is rebuilt for it.
- **A tank's ordnance record** (`vehicle.weapons`). The planner now writes it for every tank shot. A record made before the pass has none, and its check then compares without it, so the three decisions above are not applied to it: it resolves as it was recorded.
- **A repair record** (`weapon`). A record with none is the halftrack's AAMG repair, checked by the old test.
- **The state** (`VehicleWeaponShots`) is rebuilt at every replay from the records themselves, old ones too: an old tank shot appends its `ma` entry. Nothing reads the list for an old record's check.
- **A rule that now refuses what a recorded game did:** a tank's MA fired in the PFPh on a kept ROF and fired again in the AFPh was allowed before the pass and is refused now (D3.5; A7.1). A recorded game that did so still replays, by the point above.

### 7.5 The games, the tests, and the replay

In my Studio on port 6671, board 3, scripted dice; each game's file was read back.

| Game | What it shows |
|---|---|
| `p50-mg-both` | A PzKpfw IIIH in [F10] facing west fires its BMG and CMG together at [D10]: 3 + 5 FP on the 8 column, DR 7, a 1MC; then its MA at the same Location in the same phase, a hit. Recorded. |
| `p50-mg-turn` | The tank faces south-west. Both MGs together are refused for the BMG (D1.81). The CMG alone turns the turret one hexspine and adds 1 to its IFT DR; the turret then stands west and the hull south-west. The MA's shot adds "Case A, maintained, D3.51". Recorded. |
| `p50-mg-ctl` | CONTROL: written, not played; `p50-mg-both` is the control in effect (the same shot with no Case A). |
| `p50-repair` | The BMG repaired on a dr of 1; a second attempt not offered; the MA disabled on a 6, and the tank Recalled, buttoned up; the CMG's attempt then refused (ruling R50.6). Recorded. |
| `p50-repair-ctl` | CONTROL: the halftrack's AAMG repair. Written, not played in this increment; the page's list is checked in the whole-pass Studio check. |
| `p50-mg-first` | A T-34 First Fires its CMG at a squad that moved into [D10] (4 FP, FFNAM and FFMO, a 1MC); in the DFPh its MA fires at the same squad, and the Fire panel still offers its BMG and shows the CMG as fired (A.15). Recorded. |

The Studio games found one fault the build had passed: the gate's check after a commit looked for the vehicle's id among the phase's repairs and reported a repaired weapon as "Failed", though its events were written. Corrected in `GameGate`.

Rules tests: `ScenarioA1Pass50RulesTests`, seven tests. Play tests: `BacklogPass50Tests`, three tests, each replaying its game through the projector.
