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

## 8. The MA while moving: 50.3 (increment 2b)

Built on 2026-10-12 in one commit.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 58.

- **A turret turned without fire at the end of a fire phase (C3.22, D3.12) is not built,** against the answer to question F6. A tank's turret turns with a shot (Case A) and, from this task, with an MP expenditure. The Gun's action does not carry over as it stands: after it no weapon of the vehicle may fire in that phase, a PFPh turn must stop the vehicle moving, and the gate's check after the commit reads a Gun's barrel. That is a small task of its own, and none of the pass's other rules needs it.
- **A declared higher MP expenditure on entry (D2.18) is not built.** The frame left it to this task's read: the entry step's cost is checked as the computed one by the planner and spent as such by the projector, and the rule's use (more MP in LOS before a shot) is what a Delay MP after stopping gives. It keeps its row.
- **The TCA does not turn with a Start MP,** though D3.12 names "start" among the expenditures it may coincide with: the Start has its own plan (Bog Removal, Mechanical Reliability, Reverse). A turret named with a Start is refused with the reason, and it turns with the next expenditure or a Delay MP.
- **The TCA does not turn with an MP in woods, a building, or rubble** (the doubled expenditure of D3.12), as section 3 said. Refused with the reason.
- **A Motion firer of the side that is not phasing** (a DEFENDER's vehicle still under a Motion counter) stays refused. Case C4 is built for a vehicle's own MPh and AFPh, where D3.3 and C5.3 give it a Case C to add to.
- **The step bar of a TI vehicle** (section 5, point 3) is not taken; it keeps its row.

### 8.2 What is built

**Bounding First Fire by the MA (D3.3, C5.3, C5.13).** In its own MPh a tank's MA fires when the vehicle may Bounding First Fire, which the game already decides for a vehicle's MG: at the outset of its move, or once the DEFENDER has passed on its last MP expenditure. The shot is the ordnance shot with the kind `bounding-first-fire`. It fires within the current TCA (a shot that would turn the turret is refused), at a Location the side knows, on either Target Type.

**Cases C, C1, and C2 (C5.3 to C5.33).** Case C is 2 (Case B) and the mount's own 2 for a turret or 3 without one: 4 for both tanks of the catalog. Case C1 adds 1 for a continuous LOS of two and a half to three MP, and Case C2 adds 2 for two MP or less; more than three, or a LOS held since before the MPh, is Case C alone. The MP are read from the phase's own vehicle steps by the member that reads them for Cases J1 and J2, with the target's Location in the firer's place; they are rounded up, so two and a half is three.

**Case C4 (C5.35, D2.42).** A vehicle that moves and has not stopped, or is in Motion, adds the applicable Case C and doubles the lower dr of its TH DR. The doubling is recorded as a DRM line of its own, `case-c4:lower-dr`, whose value is the lower dr; before the roll the proposal says "the lower dr of the roll doubled" and gives no number.

**Between shots (C5.3, C2.24).** A MA that kept its ROF fires again only after another MP expenditure; the state keeps the movement step of each shot. A MA whose ROF is spent leaves a Bounding Fire counter (D3.3), not a First Fire one.

**The AFPh (C5.3, D3.3, D3.32).** A vehicle that entered a new hex in its MPh, or is in Motion, fires its MA in the AFPh with Case C in place of Case B (it was refused before). One under a Bounding Fire counter does not fire. One whose MA kept its ROF in the MPh, with no other weapon fired there, fires that MA once, with Case C.

**A Delay MP (D2.17, D3.3).** The step kind `delay`: one MP in place, by a vehicle that is stopped or has not started, one at a time, each answered by the DEFENDER's window like any expenditure. It leaves the vehicle stopped.

**The TCA with an MP (D3.12).** A Delay, a stop, a VCA change, and an entry take a `turret` argument; the step's record carries the new TCA and the projector sets it. The vehicle panel has "Turret with the next MP".

**The page.** The vehicle panel offers "Propose: Delay", the turret choice, and "Propose: MA Bounding First Fire" beside the MGs', with the vehicle to fire at when the target Location holds one the view sees.

### 8.3 Rulings, decided under the user's word of 2026-10-12

- **R50.8.** The MP of Cases C1 and C2 are counted as those of Cases J1 and J2 are (ruling R8.1's read), rounded up to whole MP: back from the vehicle's latest expenditure to its last one out of LOS of the target's Location.
- **R50.9.** Case C4's doubled lower dr is added to the Final DR alone. The Original DR, which decides the ROF, a malfunction, and an Original 2, is not changed by it; the lowest Final DR that C3.6 asks about is one higher.
- **R50.10.** In the AFPh Case C needs one of three: the vehicle entered a new hex this Player Turn, it is in Motion, or it fired its MA in the MPh. A tank that only started and stopped, or only spent Delay MP, and did not fire, takes Case B (C5.2: "did not enter their current hex/hexside").
- **R50.11.** D3.32's one more shot in the AFPh is read from the counter: no Bounding Fire counter, and the MA the only weapon that fired in the MPh.

### 8.4 The new facts, and a record made before the pass

- **An ordnance shot** gains the kind `bounding-first-fire`, and on its vehicle `mpInLos` and `boundingFirer`. No record made before the pass has the kind: the phasing side's ordnance was refused in the MPh. `mpInLos` is a map read taken from the record at replay. `boundingFirer` is rebuilt from the state's shot list, which holds an `mph` entry only for a Bounding First Fire shot, so it is false for every older record; an older record is compared without it all the same.
- **`inMotion` on a firing vehicle** now also says "moving and not stopped". Before the pass a vehicle could fire its MA only when it was not the moving vehicle, so the fact is unchanged for every older record.
- **A vehicle step** gains the kind `delay` and the field `turret`. An older game has neither.
- **A rule that now allows what was refused:** the AFPh shot of a vehicle that moved, and the shot from Motion. No record holds one.
- **A result that changes for a shot that could be made before:** none. The tank that fired in the AFPh without having entered a hex took Case B and still does.

### 8.5 The games, the tests, and the replay

In my Studio on port 6671, board 3, scripted dice, the adjudicator's view; each game's file was read on the page's "Latest" line and its records.

| Game | What it shows |
|---|---|
| `p50-bff-outsetb` | A PzKpfw IIIH in [F10] fires its MA at [D10] at the outset of its MPh: Case C +4, a miss, the ROF kept. A second shot is refused until an MP is spent (C5.3). A Delay MP, the DEFENDER's pass, and the second shot: a colored 5, "no more fire this phase", and the MA's button gone. The move ends with the tank not in Motion; in the AFPh its MA is refused. Recorded. |
| `p50-bff-movec` | A tank starts in [D9], out of LOS of [H10], and enters [D10] turning its turret east. Moving: Case C2 +6 and the lower dr doubled (+1 on a 1 and a 6). After its Stop MP: Case C2, no doubling. After one Delay MP: Case C1 +5. After another: Case C +4, a hit. Recorded. |
| `p50-afphc` | The tank moves from [D9] to [D10], stops, and in the AFPh fires with Case C +4 and no Case B; a second shot is refused. Recorded. |
| `p50-afph-ctlc` | CONTROL: the tank in [D10] has not moved; in the AFPh it turns its turret (Case A +1) and takes Case B +2. Recorded. |
| `p50-bff-outset`, `p50-bff-move`, `p50-bff-moveb`, `p50-afph`, `p50-afph-ctl`, the other `b` copies | SUPERSEDED: recorded or written before the two repairs below, or with the target in [I10], which [D10] does not see. |

The Studio games found four faults the build had passed: the calculator asked a Bounding First Fire shot for the facts of Defensive First Fire; the IFT refused the hit of the phasing side's ordnance in the MPh; a tank that only spent Delay MP ended its move in Motion; and after the ROF was spent the refusal named the MP between shots. The tests found a fifth: a turret named with a Start MP was dropped without a word.

Rules tests: four more in `ScenarioA1Pass50RulesTests` (eleven in all). Play tests: three more in `BacklogPass50Tests` (six in all).

## 9. The Target Facing from inside the target's hex: 50.6 (increment 2c)

Built on 2026-10-12 in one commit.

### 9.1 Departures from the plan's entry and the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 58.

- **Task 50.5, MG fire against armor (A9.6, A9.61, A9.611, D3.54), is not built.** The frame placed it in this increment and expected it to take the ATR's road through the ordnance shot. The read at the task showed that road is not open to a MG as it stands: a LATW is special-cased at every station of it (the Target Type, the crew that may fire it, a ROF held to none, Case C3 in the AFPh, no Acquisition, the SW's map facts, the projector's test of what a possessed weapon is, the fire counters of a SW and its user), and a MG differs from a LATW at each: it keeps its ROF, it is fired by its squad with a fire counter of the IFT's kind, its result on the Kill Number is a Stun and not a Shock or an Immobilization, and it must be barred wherever its FP would be halved. That is a task of the size of 50.3, not of 0:40, and it stands or falls whole: no part of it is of use alone. A MG's normal IFT attack still has no effect on armor and attacks a Vulnerable crew, as before.
- **Defensive First Fire at a vehicle that enters the firer's own hex (C5.51)** stays refused, as ruling R8.8 left it for Infantry targets: the CA would change to the hexside entered, with Case A.
- **A SW (an ATR, a PSK, a PF) at a vehicle in its own Location** stays refused; the pass opened the Gun's and the tank's shot only.
- **FT, MOL, and DC from the same hex** (the rear Target Facing) are pass 60's, as section 3 said.

### 9.2 What is built

A Gun, or a tank's MA, fires at an enemy vehicle in its own Location in the PFPh, the AFPh, and the DFPh. The shot is at range 0 with no change of CA (C5.51) and with Case E: +2, doubled in woods, a building, or rubble (C5.5); Cases L and M do not apply. A wreck, SMOKE, or another AFV in the hex adds its Hindrance, "other than the target or firer". The Target Facing is not read from the map: the colored dr of the To Hit DR gives it, 1 or 2 the rear, 3 or 4 the side, 5 or 6 the front (D3.2), for a hull hit and a turret hit alike. The proposal says so before the roll, and the record after it.

### 9.3 Ruling, the games, and the tests

- **R50.12.** The colored dr's facing is the hull's and the turret's alike: D3.2 gives one Target Facing for the hit, and from inside the hex a turret's own TCA has no hexside to be read against. The range for the AP To Kill Table's Case D is the table's first column, "0-1".

**The new fact:** none. `sameHex` was an ordnance shot's fact already, for Infantry; a vehicle target's two facings are absent from such a record, and the calculator refuses a same-hex record that names one. A record made before the pass never holds a same-hex shot at a vehicle: it was refused.

| Game | What it shows |
|---|---|
| `p50-same-rear` | A PzKpfw IIIH and a T-34 in [F10]. The German MA fires AP: Case I +1, Case E +2, a DR of 1 and 4, a hit on the turret, "rear Target Facing", TK# 11 + 1 + 1 less AF 6 = 7, a TK DR of 6: a wreck. Recorded. |
| `p50-same-front` | CONTROL: the same shot on a DR of 5 and 2: a hull hit, "front Target Facing", TK# 11 + 1 less AF 11 = 1: no effect. Recorded. |

The Studio games showed the proposal and the record naming an empty facing "toward the firer"; both now say the colored dr gives it. Rules test: `FromInsideTheTargetsHexTheColoredDrGivesTheTargetFacing`, six cases. No Play test: the setup refuses a vehicle placed in an enemy vehicle's Location, so a test game cannot be set up through the gate; the two Studio games, whose records the gate replayed at the commit, stand for it.
