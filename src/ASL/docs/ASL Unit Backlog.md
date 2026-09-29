# ASL Unit Backlog

**Status:** Open. Started 2026-09-26, when the user accepted the proposed rulings of the [ASL Unit Rally and Fire Extensions Plan](<ASL Unit Rally and Fire Extensions Plan.md>) and asked that every item left out be recorded here.

**Rule:** anything a ruling leaves out, and any recorded deviation from the rules, gets an entry here. An entry names the ruling that deferred it, the rules it needs (physical pages of `eASLRB_v3_01.pdf`), and what it depends on. An entry leaves this list when a step builds it, with that step named.

## 1. Deviations to remove

These are not left out: the game resolves them in a simplified way and records that it did so. Each should be the first work of a later pass.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A Recalled AFV's fire after its Stun period | R7.10 | D5.341, p. 203 | Recall as a Stun only for its Player Turn | A Recalled AFV never fires its MA; it may still exit. |
| The Shock and Unconfirmed Kill dr at the end of the RPh | R7.8 | C7.42, p. 177 | A phase-end roll | Each Shocked AFV or UK rolls during the RPh, before it ends; the RPh cannot end until each has. |
| One fire marker for a tank's MA and its other weapons | R7.10 | D3.1, A7.1 | Per-weapon fire records | A tank's Prep or Final Fire marker from its MA also bars its AAMG, and the reverse. |
| The verifier takes a SW shot's map reads as recorded: the firer's terrain (a mortar in a building, a PF's Backblast) and the Spotter's reach | R9.2, R9.4 | B23.423, C9.3, C13.8 | The map reads in the verifier, from the boards | They are planner reads recorded with the shot, as range and LOS are; a moved mortar in the AFPh is recomputed from state. |
| The verifier takes a vehicle target's Target Facing as recorded | R7.4 | D3.2, p. 199 | The Target Facing read in the verifier, from the boards | The facing is a map read recorded with the shot, as range and LOS are; the events after a kill or a Shock dr are checked by the gate's readback only. |
| A mortar's Area Target Type shot at a hex holding friendly units | R9.3 | C3.33, p. 169 | The friendly units' MC against their own side's ELR | Refused; C3.33 would hit them too. |
| Residual FP from ordnance Defensive First Fire, a light mortar's included | R9.3 | A8.2, p. 58 | Residual FP from an ordnance hit's IFT column (A8.26, with Air Bursts) | An ordnance hit leaves no Residual FP. |
| A road hexside is always entered at the road rate | R10.1 | A4.132, p. 48; B3.3, p. 113 | A move argument choosing the other terrain's cost | The mover never pays the other terrain's cost instead, so a road-rate entry into woods or a building keeps the hex's TEM against fire and takes no FFMO. |
| Wall Advantage is read from arrival order, not kept as state | R10.6 | B9.322, B9.323, B9.41, pp. 119 to 121 | WA counters in the state | Of two ADJACENT units sharing a wall that could both hold WA, the one that entered its Location first holds it, so a holder that broke and rallied, or left and came back, gets it back; B9.321's all-hexsides rule is not applied. |
| A leader lends his IPC only to the one laden MMC with his leader bonus | R10.8 | A4.42, p. 50 | The player naming the unit | Any SMC may add its IPC to any one Good Order Infantry unit it starts and moves with; the game lends it only when exactly one MMC of the stack with the leader bonus carries more than its IPC. |
| A berserk charge into concealed units removes every Dummy there | R10.11 | A.9, p. 43; A12.15, p. 78 | Random Selection among Dummies and real units | A.9 removes only the Dummies drawn above the first real unit. |
| A berserk charge's shortest route never counts Bypass | R10.7, R10.15 | A15.431, p. 84; A4.3, p. 49 | Bypass lanes in the route graph | The route is the shortest over hex entries only. |
| The CC of a berserk OVR onto a lone SMC is resolved in the CCPh | R10.15 | A4.152, A15.432, pp. 49, 84 | CC in the MPh | The berserk stack enters the SMC's Location; the SMC may fire at it (TPBF), and the CC follows in the CCPh as mandatory berserk CC. |
| A unit pinned or broken in Bypass is treated as in the obstacle once the stack's move ends | R10.7 | A4.32, A4.33, p. 50 | Bypass kept per unit | It takes the obstacle's TEM in the DFPh rather than staying in the open portion for the rest of the MPh. |
| The verifier takes the reveal of a move into concealed units, a Bypass exit, the Road Bonus flag, the leader bonus, and a Minimum Move's allowance as recorded | R10.8 to R10.11 | A12.15, A4.3, B3.4, A4.12, A4.134 | Map reads and the Random Selection record in the projector | They are planner reads recorded with the step, as range and LOS are; the projector checks a Minimum Move is a stack's only step and a forced back stays in its Location. |
| A lone revealed SMC gives the mover no OVR option outside the reviewed building case | R10.11 | A4.15, A12.15, pp. 49, 78 | Infantry OVR (A4.15) | The stack is forced back, as if it declined the OVR. |
| A vehicle that cannot move on may Stop or end its move beside an enemy AFV it could not kill, or after a VCA change at its CAFP | Table player, pass 11 | D2.6, D2.33, p. 199 | Refusing the entry that leaves no way on | After an ALL entry, a Minimum Move, its last MP, or beside a hidden AFV, it Stops or ends its move there. |
| A side's pass in sequential CC covers all its units left in the Location | R11.16 | A11.31, p. 73 | A pass per unit | The side attacks no more there this CCPh. |
| No Ambush dr in a Location holding a vehicle | R11.16 | A11.34, p. 73 | Ambush in sequential CC | The non-vehicular side attacks first, as A11.31 says, even where an Ambush would let the other side attack first. |
| SMOKE grenades placed at another level: down a stairwell, to the ground level of a non-Interior building hex, or up across a Crest Line on a subsequent dr | R9.5 | A24.1, p. 91 | Levels in movement and fire (pass 10.2) | Refused; SMOKE goes in the squad's Location or an ADJACENT Location at its level. |

Berserk and Surrender from Heat of Battle (R0.2, R19.2, R22.5, R28.1) left this list with unit step 30, 2026-09-27. The backlog pass 5 (2026-09-27) built the rest but the berserk route and the wreck: CX on a withdrawal, the captor's choice at a surrender, Acquisition on units, vehicle Motion and MP left, the Unlikely Kill option, Recall as its own status, the hero moving on, Battle Hardening and Leader Creation as options, the second Heat of Battle DR, and the HS keeping its SW. It also built, from the sections below, Double Time and CX, counter names by printed values, Massacre, per-side CC declarations, grain by season for Infantry, the passenger-only survival label, and the Stun +1 and used BU toggle page fixes. The simplified resolutions recorded by passes 1, 3, and 4 moved here from sections 10, 12, and 14 on 2026-09-27. The backlog pass 6 (2026-09-28) built wrecks. The backlog pass 10 (2026-09-28) decided every berserk charge route (ruling R10.15), removing its deviation; a charge into a vehicle's or a Gun crew's Location stays in sections 12 and 14. The backlog pass 11 (2026-09-28) built the A12.41 choice of concealed units a vehicle enters, removing the R6.8 deviation, and added its own three.

## 2. Rally (step 19)

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|
| Rally terrain in pillboxes, trenches, and on rooftops (marsh and rubble were built in pass 13) | R19.5 | A10.61, p. 68; B23.83, p. 140; B30.5, p. 151; B Terrain Chart, p. 160 | Those terrains on the map read |

## 3. Fire phases and fire groups (step 20)

The backlog pass 12 (2026-09-28) built this section; what it leaves out is in section 22.

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|

## 4. Movement and fire during it (step 22)

The backlog pass 10 (2026-09-28) built this section; what it leaves out is in section 20.

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|

## 5. Support weapons (step 23)

The FT, DC, and MOL were built by the backlog pass 15; what they leave out is in section 25.

## 6. Carried from step 17's exclusions

Step 17 left these out; steps 19 to 23 do not build them.

| Item | Rules | Depends on |
|---|---|---|
| Smoke and WP | A24, p. 91 | Smoke placement |
| ELR from the scenario card, once scenario cards are a registered source | A19.1, p. 86 | Scenario card registration |

## 7. Later passes

| Item | Step | Notes |
|---|---|---|
| Light mortars and LATW (PSK, BAZ, PIAT, PF, ATR) | 24 | To Hit weapons, with ordnance. Steps 24 (ordnance) and 25 (vehicles) were built by the deviations passes 3 and 4; what they left out is in sections 12 and 14. |

## 8. Added by the steps 19 to 23 review

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|

## 9. Added by the steps 19 to 23 implementation

The backlog pass 10 (2026-09-28) built this section.

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|

## 10. Added by the deviations pass 1 (steps 26 to 28)

Every item here was built by the backlog pass 15; what they leave out is in section 25.

## 11. Added by the deviations pass 2 (steps 29 and 30)

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|
| CC by or against vehicles, sequential CC with a vehicle, Street Fighting | R29.1 | A11.31, A11.5 to A11.8, pp. 73 to 76 | Vehicles (step 25) |
| Axis Minor and Japanese Heat of Battle exceptions (the Italian one was built in pass 15) | Pass 2 review | Heat of Battle table notes, p. 83 | Counters of those nationalities; until then they are refused |
| Play page: a berserk leader's companions' TCs in the CC record (SW left unpossessed by eliminated units are listed since pass 5) | Table-player review | A20.24, A15.431, A15.41 | | Shown in the fire and rally records and the move's reasons, not in the CC record. |

## 12. Added by the deviations pass 3 (step 24)

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|
| The Area and Vehicle Target Types, mortars, and SMOKE | R24.2 | C3.31, C3.33, C9, pp. 169, 170, 179 | Vehicles (step 25) for the Vehicle Target Type; mortar spotting |
| AP, HEAT, APCR, Canister, and other Special Ammunition, and To Kill | R24.2 | C7, C8, pp. 175 to 179 | Armored targets (step 25) |
| Defensive First Fire by Guns, Cases J1 to J4, Gun Duels, and a kept ROF in Final Fire after First Fire | R24.2 | C2.2401, C2.241, C6.1 to C6.17, pp. 167, 168, 173 | Ordnance in the movement windows |
| Intensive Fire, OVR Prevention, and C2.5's Intensive Fire counter for a Gun without a Multiple ROF | R24.2 | C5.6 to C5.641, C2.5, pp. 168, 172, 173 | A Gun with no Multiple ROF; vehicles |
| Bore Sighting | R24.2 | C6.4, p. 174 | Scenario Defender setup |
| Fire within the Gun's own hex (Case E) | R24.2 | C5.5, p. 172 | Fire into one's own Location |
| Captured and non-qualified use of a Gun (Case H) | R24.4 | C5.8, A21.13, pp. 173, 88 | Captured equipment |
| Concealed crews and Guns, and a firing crew's loss of concealment | R24.4 | A12.14, C6.57 | Concealment of firing ordnance |
| Crews' inherent fire, and A7.352's loss of it after the crew fires its Gun | R24.8 | A7.352, p. 56 | Crews as firers in the Fire package |
| Crews and Guns as targets, and Gun destruction | R24.3 | C11, p. 181; C11.6 chart, p. 701 | Guns as targets |
| Gun movement, manhandling, (un)limbering, towing, and abandoning a Gun; a crew leaving its Gun | R24.4 | C10, pp. 180 and 181; C2.8, A4.41 | Gun movement |
| Gun repair and removal | R24.4 | A9.72, C2.28 | The Guns' malfunctioned sides (no registered source gives them) |
| Overstacked firer or target Locations for ordnance | R24.8 | A5.12, A5.131 | Overstacking penalties |
| A Covered Arc across boards or on a reversed board | Pass 3 design | C3.2, p. 168 | Composed-map geometry for bearings |
| Targets at another level, and C2.6's depression and elevation limits | R24.2 | C2.6, p. 168 | Levels in fire |
| Multiple Hits | R24.2 | C3.8, p. 171 | A reviewed Gun of 40mm or less |
| A Gun's BPV, dates, and Animal-Pack capability (note O) in the catalog | Referee (catalog rows) | Chapter H key, p. 351; G10 | Vocabulary attributes for them |
| Choosing a facing other than the fewest-hexspine turn | Pass 3 design | C3.21, C5.1 | A facing argument on the shot |
| Changing a Gun's Covered Arc without firing, at the end of a friendly fire phase | Table-player review | C3.22, p. 169 | A turn action for Guns |
| Play page: marking which target Locations are in a Gun's Covered Arc, in range, or refused, and drawing the Covered Arc | Table-player review | C3.2 | Map overlays for Guns |
| CC with a Gun's crew, and advances or berserk charges into its Location | R24.3 | A11, C11 | Guns and crews as targets |

## 13. Found in the Studio demo of passes 2 and 3 (2026-09-27)

Found by playing the Play page with scripted dice in the games `pass23-demo`, `pass2-ambush`, and `pass2-hob`. Every refusal was correct; the gaps were in what the page offered and how it explained a refusal. All are fixed on the branch `feature/asl-play-page-fixes` (2026-09-27), each with a test, and were checked on the Studio in the games `fix-hob`, `fix-ambush`, and `fix-charge`. The last two rows were found during that check.

| Item | Rules | Fixed |
|---|---|---|
| The Advance panel offered a crew that mans a Gun; the gate refused it only after it was proposed (`play.advance-crew-mans-gun`) | R24.4; C10, A21.13 | The Advance panel leaves such crews off. |
| The CC panel offered the ambushed side's attacks during the ambusher's round; the refusal showed only the package code `asl.a1.cc.round-outside:0` | A11.3, A11.32 | The planner's `DeclaringSides` gives the sides that may declare: none before a due Ambush dr, then the ambusher, then the ambushed side, or the ambusher again when chosen; the page offers only those, and hides the attack controls until the Ambush drs. |
| Package refusal codes reached the player raw, each under a headline that said the package "does not decide every outcome" | | `RefusalReasons` keeps each Fire, CC, Ordnance, Rally, and Heat of Battle code and adds a sentence with its rule; the headline says "refuses this ... as proposed" unless every code is an undecided one. |
| The Fire panel offered firers that had already fired this phase (`asl.a1.fire.firer-already-fired`) | A7.1, A8.4 | `LiveFire.FireSpent` reads it as the package does in the PFPh, AFPh, and DFPh; such units are left off, unless a MG they possess has not fired. |
| The Fire panel offered units held in Melee, berserk, captured, or a Guard; the refusal named all four causes | A11.15, A15.432, A20.5, A20.52 | `GamePlanner.FireBar` names the one cause; the panel leaves those units, and Locations with none that may fire, off. |
| The Fire panel kept the previous phase's From and Target when the firing side changed, and a proposal was refused with misleading reasons | | Every panel's choices are cleared at each change of turn, phase, or phasing side, and a From or Target the game no longer offers is dropped. |
| The Movement panel did not mark a berserk unit, that it must move first, or its charge target and route | A15.43, A15.431 | The planner's `Charges` gives each berserk unit's target and next steps, or why the model cannot decide its charge (ruling R30.5); the page marks it, checks it, and fills in its only next step. |
| The Units table showed a prisoner as `captured` without its Guard, and the Guard without its prisoners | A20.5 | Conditions show "guarded by" and "guards". |
| Records credited scripted dice to "the system" | DICE-12 | A roll's source stays `system`, since replay requires it (UNIT-STATE-019); while scripted dice are on, the Rolls list says its dice may have come from the queue and the game is test data. |
| The Fire panel offered a crew as a firer; its inherent fire is not reviewed (`firer-outside`) | R24.8 | Crews are left off the firer list. |
| The Movement panel offered a crew that mans a Gun (`play.move-crew-mans-gun`) | R24.4 | Such crews are left off the Movement panel. |

## 14. Added by the deviations pass 4 (step 25)

| Item | Deferred by | Rules | Depends on |
|---|---|---|---|
| BMG and CMG, and Passengers' and Riders' fire (vehicle fire in the MPh and AAMG repair since pass 6) | R25.7 | D3.3, D3.7, D6.64 | Vehicles in the movement windows |
| Closed-topped AFVs, main armament, To Kill, and AP | R25.2 | C7, D3.1 | Step 24's successors |
| A Gun's shot at a Location with a vehicle (the Vehicle Target Type) | R25.10 | C3.31 | Step 24's successors |

Vehicle movement (Reverse, VBM, ESB, Minimum Move, Bog, OVR, stacking, and terrain) and vehicles in CC left this list with the backlog pass 11 (2026-09-28); what that pass leaves out is in section 21.

## 15. Added by the backlog pass 5

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Dropping the SW beyond a withdrawing unit's IPC | R5.5 | A11.21, p. 73; A4.43, p. 50 | SW handling (pass 13) | A unit in Melee carrying more than its IPC may not withdraw; the refusal says so. |
| The Friendly Board Edge from the scenario card | R5.16 | A20.53, p. 87 | Scenario cards (pass 17) | The players name one edge per side at a new game; a side with none named cannot Recall off the map. |
| A Recalled AFV's Stop to unload its Passengers | Table player, item 9 | D5.341, pp. 203 to 204 | Passengers (pass 6) | A leaving Recalled AFV may not Stop. |
| A Recall route to a Friendly Board Edge of more than one edge | R5.16, R5.17 | D5.341, pp. 203 to 204; A20.53 | Scenario cards (pass 17) | Only the one named edge counts. |

## 16. Added by the backlog pass 6

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Armored Assault: Infantry moving with their AFV | R6.1 | D9.31, p. 209 | Combined vehicle and Infantry movement | Infantry and an AFV move apart; the AFV's cover applies once both stand in a Location. |
| An AFV or wreck that is entrenched, Dug-In, or in a Depression, and the Case J exception for units Abandoning, Bailing Out, or unloading | R6.1 | D9.3, p. 209; D9.54 | Entrenchments, Depressions, Passengers | Not reached: none of these exist in the reviewed terrain. |
| A Bypassing AFV's or wreck's Hindrance | R6.2 | D9.4, p. 210 | Bypass movement | Not reached: vehicles do not Bypass. |
| Spreading Fire from a burning wreck, terrain Blazes, and the other B25.14 causes of a burning wreck (FT, MOL, To Kill, CC) | R6.3 | B25.1 to B25.6, B25.14, p. 143 | Fire rules; FT, MOL, To Kill, CC against vehicles | A Blaze stays on its wreck and never spreads. |
| Pushing a wreck, Scrounging its MG, and attacking a wreck as a vehicle | R6.4, R6.5 | D10.1, D10.42, D10.5, pp. 210 to 211 | Tracked AFV weights, Scrounging, CC against vehicles | A wreck stays where it is and cannot be attacked. |
| The A12.2 road clause for a concealed vehicle | R6.7 | A12.2, p. 79 | Road-hex LOS tracing | A vehicle in a grain-road hex counts as in Concealment Terrain. |
| Case H after a Rally, a pin's removal, or another change that gives an enemy unit LOS without a MF or MP expenditure | R6.7 | A12.2, p. 79 | A check after every event | Case H is checked after each MF or MP expenditure only. |
| A vehicle's Final Fire after its First Fire | R6.9 | A8.4 | Vehicles in the Final Fire rules | A vehicle fires once per Player Turn unless it keeps a Multiple ROF. |
| Repair of a vehicle MG by a Hero Rider, and the Shocked crew | R6.10 | D3.7, p. 201 | Riders | Only the CE crew repairs; a Stunned, Shocked, or Recalled crew does not (pass 7 built Shock). |

## 17. Added by the backlog pass 7

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A tank's BMG and CMG | R7.12 | D1.8, D3.4, pp. 196, 200 | Vehicle MG covered arcs; pass 9 (fire extensions) | The tanks fire only their MA. |
| A tank's MA in CC and OVR, and in the MPh (Bounding First Fire and Defensive First Fire by ordnance) | R7.10, R7.12 | D3.3, C2.2401, C6.1 | Ordnance in the movement windows (pass 8) | A tank fires its MA in the PFPh, AFPh, and DFPh only. |
| Case C (a vehicle that moved) and Case C4 (a Motion or Non-Stopped firer) | R7.10 | C5.3, C5.35, D2.42, p. 172 | The To Hit Cases for moving firers | A tank that entered a new hex before its AFPh shot, or is in Motion, may not fire its MA. |
| Setting the TCA with MP or at the end of a fire phase, and pivoting a non-turreted MA | R7.10 | D3.12, C3.22, C5.11 | TCA changes outside fire | The TCA turns only for a shot; it starts along the VCA. |
| HD, Smoke, Intensive Fire, and Deliberate Immobilization | R7.12 | D4, C8.5, C5.6, C5.7 | Terrain and Smoke rules | Not offered. |
| Elite Depletion Numbers | R7.6 | C8.2, p. 179 | Force quality in the scenario setup | Depletion Numbers are the printed ones, also for Guards. |
| A red CS#'s -1 for burning | R7.7 | C7.7 note A, D5.7 | A catalog AFV with a red CS# | No catalog vehicle has one. |
| D5.5's second trigger: an Original 5 against an already immobilized vehicle | R7.9 | D5.5, p. 203 | | Not applied. |
| An NT AFV's upper superstructure hit and its AF | R7.4 | C3.9, p. 171 | A catalog NT AFV | Every non-turreted vehicle is hit on the hull. |
| A Gun's Acquisition of a vehicle following it | R5.13 | C6.51 | Acquisition on vehicles | It stays on the vehicle's hex. |
| Radioless AFVs | R7.1 | D14 | Platoon movement | The T-34's radioless status has no effect. |
| Hazardous Movement for a crew that bails out or survives | R7.9 | D5.5, D5.6, A4.62 | | The crew is placed Good Order beneath its vehicle or wreck. |
| HE at Infantry in a Location that also holds an enemy vehicle | R25.10 | C3.32, C.3 | The hit's IFT attack on vehicles and Infantry together | Refused; the vehicle may be fired at by name. |

## 18. Added by the backlog pass 8

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Gun Duels | R8.1 | C2.2401, p. 167 | Bounding First Fire by ordnance | The DEFENDER's shot is resolved first, alone. |
| OVR Prevention | R8.2 | C5.64, C5.641, p. 173 | Vehicle OVR (pass 11) | Not reached: vehicles do not OVR. |
| A vehicle's Intensive Fire | R8.2 | C5.6, D3 | Vehicle MA fire rules | A tank's MA never Intensive Fires. |
| C5.51's Case E in the MPh, turning with Case A | R8.8 | C5.51, p. 172 | Defensive First Fire into the Gun's own hex | Refused; Case E is fired in the fire phases. Entry into an enemy Location is refused in the MPh, so Case E is rare. |
| The Area Target Type against a Gun, AP and HEAT at Guns (HE Equivalency), an unmanned Gun as a target, and a crew sharing its Location with other units | R8.3 | C11.2, C11.51, C11.52, C8.31 | The Area Target Type (pass 9); HE Equivalency | Refused, or outside the package. |
| Random SW Destruction of Guns by fire | R8.3 | C11.51, A9.74 | Random SW Destruction | Infantry fire never destroys a Gun. |
| A7.353's halved inherent FP of a crew that fired its Gun | R8.4 | A7.353, p. 56 | Subsequent First Fire and Final Fire of crews | The crew has no inherent FP for the rest of the Player Turn. |
| The Labor counter after a failed push, more than one unit pushing, a CA change while pushing, carried PP, and the DEFENDER's fire at a failed push | R8.6 | C10.3, B24.8 | Labor, portage checks | One crew or HS pushes; a failed push ends its move. |
| A vehicle's hook-up opening the DEFENDER's window, Passengers loading with a hook-up, en portee, and limbering | R8.6 | C10.11, C10.13, C10.2, C10.5 | Passengers (pass 11) | The hook-up is resolved at once; both catalog Guns are QSU. |
| Case O for ordnance firing at a pushing crew | R8.6 | C10.3 | Ordnance To Hit Cases for Hazardous Movement | The gunshield is denied; Infantry fire takes Hazardous Movement's -2 since the backlog pass 10 (ruling R10.8), ordnance takes no Case O. |
| Recovery of an unmanned Gun and captured Guns | R8.6, R8.8 | A4.44, A21.11, C5.8 | Gun Recovery (SW Recovery was built in pass 13) | An abandoned Gun can only be hooked up and unhooked for a crew. |
| A Target Facing change restarting the C6.17 count, and Case J1 or J2 after an AFV turns in view | R8.1 | C6.17 | Vehicle Target Facing history | The count runs per Location. |
| A tank's MA Bore Sighting, a Bore Sighted Location kept secret until used, and a SW's Bore Sighting | R8.8 | C6.41, C6.44 | Hidden setup records | The Bore Sighting record is visible to the Scenario Defender only; tanks do not Bore Sight. |
| Vehicles over the stacking limit, and A5.132's accidental hits | R8.10 | A5.12, A5.132 | Vehicle stacking (pass 11) | Only Personnel count. |
| Fire at another level for ordnance | R8.7 | C2.6 | Levels in fire (pass 10.2) | The C2.6 limit refuses; otherwise undecided. |
| Multiple Hits | R8.12 | C3.8 | A Gun of 40mm or less | Not reached. |

## 19. Added by the backlog pass 9

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A registered source for the manufactured Panzerschreck and ATR counters | R9.10, R9.11 | C13.48, C13.2, pp. 183 to 184 | A source that prints the counters | Built in pass 9b with manufactured values (sheet MFG, ruling R0.3); a source would replace them. |
| The ATR's fire at Guns, the Panzerschreck's 12 FP attack on Infantry, a lone hero or two SMC firing a PSK, and a SMC firing an ATR at Personnel | R9.10, R9.11 | C13.23, C13.24, C13.45, C13.48, C8.31 | HE Equivalency; SMC fire with a SW | Refused. |
| A leader firing an ATR from the fire panel, which lists no leader as a firer | R9.10 | C13.21 | SMC fire with a SW | The panel leaves leaders out; the ATR's vehicle shots take a SMC. |
| A MG's B# one lower in Inexperienced hands | R9.10 | A19.32, p. 86 | none | The MG keeps its printed B#; the ATR, mortars, and PSK take the -1. |
| The C13.8 bar on a PF or PSK firing at a target two or more levels higher in an adjacent hex, or directly above | R9.8, R9.11 | C13.8, p. 185 | Levels in fire (pass 10.2) | Not reached while fire at another level is undecided. |
| Light mortar repair, and dismantled 76-82mm mortars | R9.1 | A9.72, C9.2 | A registered source for a SW mortar's malfunctioned side | A malfunctioned light mortar stays malfunctioned. |
| The Area Target Type by Guns, mortars against vehicles and Guns, target hexes with units in several Locations, and units out of the firer's LOS in the target hex | R9.3, R9.9 | C3.33, C3.332, C1.55, p. 170 | IFT attacks on vehicles by HE; levels in fire | Refused. |
| A mortar Spotter's wait until the next MPh after its loss, the Acquisition a spotting squad loses by firing, and spotting in the AFPh | R9.4 | C9.3, C9.31, p. 179 | Opportunity Fire (pass 12) | A new Spotter may be named at once; the Acquisition is kept; spotted fire is in the PFPh and DFPh only. |
| Bore Sighting by a light mortar | R9.9 | C6.41, p. 174 | Hidden setup records | Not offered. |
| SMOKE and WP by ordnance, WP grenades, Dispersed SMOKE, drift, Gusts, and weather | R9.9 | C8.5, A24.3 to A24.62 | A Gun with an s# or WP#; Wind Force and Direction (weather built in pass 16) | Not reached: no catalog Gun lists SMOKE, no nationality in the game has WP grenades. |
| The PF against Infantry, unarmored vehicles, and Guns (HE Equivalency), the PFk, the optional usage of C13.311, Desperation fire, a PF at range 0, and a PF Check in Subsequent First Fire | R9.8 | C8.31, C13.3, C13.311, C13.81 | HE Equivalency; Subsequent First Fire of SW | Refused. Range 0 is not reached, since Infantry and an enemy vehicle never share a Location in the review (R25.3). |
| A squad firing a light mortar and a MG in one phase, forfeiting its inherent FP | R9.2 | A7.351 | One record of each unit's SW use | The Fire package's limit of two MG per squad does not count the mortar. |
| The PF usage limit with reinforcements | R9.7 | C13.31 | Reinforcements (pass 17) | The limit counts the German squad equivalents at the end of setup. |
| A kept Spotter that moved out of reach, and the HS of a Reduced Spotting squad keeping its Spotting ability | R9.4 | C9.3, p. 179 | Spotter records that follow lineage | A kept Spotter blocks a new one while it is Good Order, wherever it is; the HS of a Reduced Spotter is a new unit, so a new Spotter may be named. |
| A SMOKE attempt in a Residual FP Location | R9.5 | A24.1, A8.2 | The Residual FP attack on an MF expenditure without entry | Refused. |
| The SMOKE panel offering only squads with an exponent, and the own and ADJACENT Locations as choices | R9.5 | A24.1 | The page reading catalog attributes | The page lists every checked squad and takes a typed Location; the planner refuses the others. |

## 20. Added by the backlog pass 10

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Terrain the pass does not review: water, streams, gullies, crags, shellholes, graveyards, lumberyards, factories, Rowhouse walls, bridges, sunken and elevated roads, cliffs (Climbing), and Continuous Slopes; rooftops (by SSR) and cellars | R10.1 | B chapter; Terrain Chart, p. 698; B11, B23.41, B23.8 | Their transcription and movement and fire rules | Refused as an entry and as a target's terrain. |
| Fire from a marsh hex, and HE halved into marsh | R10.1 | B16.31, B16.32, p. 130 | Area Fire by firer, and the limited weapons | Fire from marsh is refused; ordnance at marsh keeps its full FP. |
| Attacks between levels of one building hex | R10.2 | B23.26, p. 136 | Fire within one hex between Locations | Refused. |
| A vehicle's MG, ordnance, and mortars firing at another level, and the C13.8 bar on LATW at targets two levels higher | R10.4 | C2.6, C13.8 | Levels in those packages | Refused as before (backlog sections 18 and 19). |
| Hillside walls and hedges, and bocage | R10.5 | B9.5, B9.6, pp. 121 to 124 | Their LOS and TEM rules | Fire at a target with such a hexside is refused; bocage is not crossed. |
| Wall Advantage as a declared, kept state (WA counters) | R10.6 | B9.32 to B9.41, pp. 119 to 121 | WA in the state | Read from arrival order (a deviation in section 1). |
| The vertex LOS to a Bypassing unit, fire at a Bypassing unit in a hex with a wall or hedge, Bypass beyond two hexsides or continued around the same hex, a Bypass across an Abrupt Elevation Change, and an Ablaze obstacle | R10.7 | A4.31, A4.34, pp. 49 to 50; B9.42 | Vertex LOS in the fire planner; terrain Blazes | Refused; LOS to the hex center must cross a Bypassed hexside. |
| Snap Shots at a hexside of a hex with a wall, hedge, SMOKE, or rubble | R10.13 | A8.15, p. 59; B9.42, p. 121 | Their LOS modification | Refused. |
| Hazardous Movement other than a pushing crew (Clearance, Fording, Climbing, a crew bailing out) | R10.8 | A4.62, p. 51; D5.6 | Those activities | Not reached. |
| Hidden units moving | R10.10 | A12.3, p. 79 | HIP loss on movement | Refused. |
| A Minimum Move into concealed enemy units | R10.9 | A4.134, A12.15 | A forced back after a Minimum Move | Refused. |
| Infantry OVR outside the reviewed building case, and a lone revealed SMC's OVR option | R10.11 | A4.15, A4.151, A4.152, p. 49 | OVR NTC and the SMC's options for any terrain | A Known lone SMC's Location is refused; a lone revealed SMC forces the mover back (a deviation in section 1). |
| A berserk charge up or down a stairwell or along an upper level | R10.15 | A15.431, p. 84; B23.4 | Levels in the route graph | A Known enemy unit upstairs has no route: the charge ends in place. |
| Wall Advantage between units adjacent across a wall since setup with no Scenario Defender named | R10.6 | B9.32, p. 119 | WA declared at setup | Fire between them is refused. |
| A stack in Bypass splitting, making a SMOKE attempt, or occupying an obstacle that holds enemy units; fire at it from within the hex; a Snap Shot at a Bypass step; Bypass of a hex with a wall, hedge, or friendly units | R10.7 | A4.3 to A4.34, pp. 49 to 50 | Bypass kept per unit; vertex LOS | Refused. |
| The player choosing which leader lends IPC to which unit | R10.8 | A4.42, p. 50 | A move argument naming them | A deviation in section 1. |

## 21. Added by the backlog pass 11

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A fully-tracked AFV entering a building hex (not by VBM), with its Bog DR and the building's collapse | R11.7 | B23.41, p. 135 | Building entry by vehicles | Refused; a vehicle enters a building hex only by VBM. |
| Reverse Motion (the WEST OF ALAMEIN counters) | R11.1 | D2.24, p. 197 | Reverse Motion counters | A vehicle in Reverse keeps one MP to Stop; an entry or VCA change leaving it less is refused. |
| Motion Attempts, and a vehicle firing in Motion by its own choice | R11.18 | D2.401, D2.42 | Motion Attempts in the DFPh | Not offered; Motion comes only from ending a move without stopping. |
| Trail Breaks, and a Bog Check on leaving a woods-road hex off the road | R11.7; referee, pass 11 | B13.421, B13.422, p. 132 | The road portion of a hex kept per vehicle | No Trail Break is placed; a vehicle leaving by a non-road hexside takes no Bog Check. |
| The CAFP's LOS and fire from a vehicle in Bypass | R11.2 | D2.37, p. 197 | LOS to and from the CAFP | Fire takes the hex center, as before. |
| Armor Leaders, in OVR and CC | R11.11, R11.14 | D7.16, A11.5, D3.44 | Armor Leader counters | Not in the catalog; no OVR leadership. |
| Help freeing a bogged vehicle (towing, Infantry assist) and a Bog Removal's bearing on other vehicles | R11.10 | D8.32 to D8.35 | Towing between vehicles | Only the vehicle's own Bog Removal. |
| Wire for vehicles, and the Bog DR of a vehicle's whole MPh in Mud or Deep Snow (Mud and snow costs and Bog DRM since pass 16) | R11.7; R16.12 | D8.21, D8.23, B26 | Wire; a secret Bog DR per MPh | Not reached for wire; in Mud and Deep Snow only the Bog DRs the terrain calls for are made. |
| FPF Reaction Fire, Street Fighting, and Gun crews' Reaction Fire | R11.13 | D7.211, D7.212, D7.221, D7.23, A11.8 | FPF by leaders; Street Fighting; Gun crews in CC | Not offered. |
| Infantry against Infantry in a Location holding a vehicle | Table player, pass 11 | A11.31, p. 73 | Sequential Infantry CC | Refused; such Infantry stay in Melee and may withdraw. |
| Ambush in a Location holding a vehicle | R11.16 | A11.34, p. 73 | Ambush in sequential CC | A deviation in section 1. |
| A pass by one unit in sequential CC | R11.16; referee, pass 11 | A11.31, p. 73 | A per-unit pass | A deviation in section 1. |
| Captured vehicles used by their captors | R11.16 | A21.2 | Captured vehicle rules | A captured truck is Abandoned and neither moves nor fires. |
| Passengers and Riders: in OVR, in CC, and their A11.611 survival | R11.11, R11.14 | D6, D7.11, A11.611 | Passengers and Riders | No vehicle carries any. |
| Overstacking penalties of vehicles (A5.11 MP), and A5.132 | R11.6 | A5.11, A5.132, p. 52 | Vehicle overstacking | Any number of vehicles share a Location; each adds to the entry cost. |
| A berserk charge at a vehicle | R11.17 | A15.43 | Charges at vehicles | The charge stays undecided and ends in place, as since pass 4. |
| A Bypassing AFV in woods whose Target Facing the TEM rule of the IFT would change, and a turret's facing in Bypass | R11.2 | D2.34, D3.2 | Bypass facing for ordnance | Bypass Target Facing is read from the straddled hexside for both hull and turret. |
| The current Morale Level of a combined PAATC | Referee, pass 11 | A12.41, p. 83 | Morale changes in play (DM, ELR replacement) | The printed ML, with Fanatic and wounds, is used (a reading). |
| The "wished to enter next" field for Motion | Table player, pass 11 | D2.4 | Buttons for the VCA hexes | A free-text Location on the Play page. |

## 22. Added by the backlog pass 12

The backlog pass 12 (2026-09-28) built, and removed from sections 2, 3, 5, 8, 11, and 12: Encirclement, Opportunity Fire, fire at a blocked LOS, Spraying Fire, Fire Lanes, directed and mixed FPF, one attack on pinned and unpinned movers, a squad's MG apart from its inherent FP, a leader firing a MG, the Concealment Table's gains, fire into a Melee and at prisoners, a Guard's fire, and berserk fire. What it leaves out follows.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Opportunity Fire by ordnance, mortars, and LATW, and a mortar Spotter's wait after it | R12.1 | A7.25, C9.3 | Ordnance in the AFPh | Only Infantry small arms and MGs use Opportunity Fire. |
| Random Events on the DR of fire at a blocked LOS | R12.2, R12.12 | A6.11 | Random Events | The DR decides Multiple ROF only. |
| An unmarked unit's First Fire joining FPF | R12.3; referee, pass 12 | A8.31 | Per-firer fire markers | Refused; FPF groups with Subsequent First Fire only. |
| Concealment gain by vehicles, Guns, and Dummy stacks, and Lax units (night since pass 16) | R12.5 | A12.12, the Concealment Table, E1 | Their Cases | Only Infantry not manning a Gun gain "?". |
| Spraying Fire in the MPh, at upper-level Locations of one hex, and counted toward an Encirclement | R12.6; referee, pass 12 | A9.5, A9.52, A7.7 | Spraying in the movement windows | Refused in the MPh; a spray does not count toward an Encirclement. |
| Fire Lanes along an Alternate Hex Grain, their Snap Shots, intersecting lanes, the TPBF and CC Reaction Fire cancellation, Impulse movement, a lane's wall or hedge TEM, attacks on each MF expenditure after the first, and lanes against vehicles | R12.7; referee, pass 12 | A9.22 to A9.223 | Lane geometry and hexside TEM for Residual FP | A lane runs along a straight Hex Grain and attacks Infantry as they enter its Locations; its MG does not fire again until the DFPh. |
| Other Hindrances on a Fire Lane (orchard, wrecks) as DRM | Referee, pass 12 | A9.222 | Hindrance types in the LOS read | No lane Hindrance applies as a DRM; any cancels FFMO. |
| A concealed unit's TPBF into its own Melee Location | R12.8 | A11.15 | A fire phase's read of a concealed unit in a Melee Location (concealment in CC is built, pass 14) | Refused. |
| Encirclement at upper levels, from the Locations above and below, by ordnance, of Vulnerable PRC of an Immobile vehicle, by vehicular armament, and its capture effects | R12.11 | A7.7, A7.72, A20.21 | Those attacks and levels | Only Infantry fire at Normal Range counts; ground-level Personnel are Encircled. |
| The ordnance To Hit +1 for an Encircled Gun crew | R12.11 | A7.7 | The Ordnance package | Not applied. |
| Spraying Fire's second Location, an Encirclement, or a Fire Lane after the first attack stops for an owner's choice (Battle Hardening, an Unlikely Kill dr) | Table player, pass 12 | A9.5, A7.7, A9.22 | Carrying the follow-on work in the choice's resume | They are not made; the choice resumes the first attack only. |
| Fire Lane attacks on vehicles, and on units spending MF in a lane Location other than by entering it (SMOKE placement) | Table player, pass 12 | A9.222, A8.22, A8.222 | Lane attacks in the vehicle and in-Location planners | Only Infantry entering a lane Location are attacked. |
| A Fire Lane's hard Hindrance (orchard) as a DRM, and SMOKE placed on the lane after it is laid | Table player, pass 12 | A9.222 | Hindrance types and SMOKE read at attack time | The Hindrance is read when the lane is placed, and any of it only cancels FFMO. |
| The Fire Lanes and Encircled Locations drawn on the map | Table player, pass 12 | A9.22, A7.7 | Map overlays in the Play page | Listed above the Play page's panels. |

## 23. Added by the backlog pass 13

The backlog pass 13 (2026-09-28) built, and removed from sections 2, 3, 5, 8, 11, and 22: DM from an ADJACENT enemy, at the start of the RtPh, and its retention; the RtPh, Low Crawl, and Interdiction; Failure to Rout and surrender in the RtPh; Deployment and Recombining; SW transfer, drop, and Recovery; dismantling; captured MG and ATR; the rally terrain of marsh and rubble; the Self-Rally of the catalog's MMC; and Encirclement's doubled MF in the RtPh. What it leaves out follows.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Terrain Blazes: routing out of a Blaze, DM in one, Failure to Rout in one | R13.3; referee, pass 13 | B25.4, A10.5, A10.62 | Terrain fire (B25) | No Blaze is read; a Wreck Blaze only costs MF. |
| DM from overstacking, and DM in pillboxes and trenches | R13.1 | A10.62 | Stacking limits, fortifications | Not applied. |
| The A10.532 EXC for a hex of the building a unit starts in | Referee, pass 13 | A10.532 | Building identity in the map read | Such a hex is a destination like any other. |
| A concealed unit revealing itself to Interdict or to repulse a routing unit | R13.3 | A10.533 | Concealment choices in the RtPh | Concealed units are ignored. |
| Interdiction by vehicles, Guns, and ordnance, and their Normal Range for rout and DM | R13.3; referee, pass 13 | A10.532, A10.5 | Vehicle and Gun ranges | Only Infantry Interdict and threaten the open. |
| The leadership DRM of a leader in the Location entered on an Interdiction NMC, and Interdictors whose FP is halved by other causes (Area Fire in marsh) | Referee, pass 13 | A10.53, A10.532, B16.32 | Per-unit FP state | DRM 0; only CX, pinned, Encircled, and Melee units are barred. |
| Voluntary Rout and voluntary breaking at the start of the RtPh | R13.3 | A10.41, A10.711 | Player choices at phase start | Not offered. |
| Rout of Passengers and Riders (night rout since pass 16) | R13.3 | D6.1 | Vehicle cargo | Not built. |
| The DEFENDER's fire window for the MF a Recovery attempt spends in the MPh | Table player, pass 13 | A4.44, A8.1 | A window on non-move MF expenditures | The MF is spent with no window. |
| The German dm MMG firing as a LMG, dismantled mortars, and weapons that start dismantled | R13.6 | A9.8, C9.2 | Their catalog values | Only the German MMG is dismantled and assembled. |
| Captured ordnance, mortars, LATW, Guns, and vehicles | R13.7 | A21.13, A21.2 | Captured Gun and vehicle rules | Only captured MG and ATR take the penalties. |
| The Rout panel's preview of the Failure to Rout eliminations and surrenders, and of the ATTACKER's undecided "may rout" units | Table player, pass 13 | A10.5 | A phase-end preview | Shown only as the phase ends. |
| Records of DM gained, Failure to Rout eliminations, and SW transfers in the records list | Table player, pass 13 | A10.62, A10.5, A4.431 | Event labels for these | They show in the units table only. |
| Short readable ids for HS and squads made by Deployment, Recombining, and Casualty Reduction | Table player, pass 13 | A1.31, A1.32 | An id scheme for produced units | Ids join the attempt and the parent id. |
| The Deploy control splitting several SW between the two HS | Table player, pass 13 | A1.31 | A multi-select on the page | The page names one SW for the second HS; the action takes a list. |
| A route builder with levels on the Play page | Table player, pass 13 | A10.5 | Map clicks on the Play page | Hexes are typed at level 0, or a full Location is typed. |

## 24. Added by the backlog pass 14

The backlog pass 14 (2026-09-28) built, and removed from sections 11 and 20: Hand-to-Hand CC with the red Kill Numbers; concealed and hidden units and Dummies in CC, and advances into concealed enemy units; TI units in CC; capture attempts; prisoners in a CC Location and their escape; Unarmed units, the units freed as Unarmed, and a Guard without capacity; transferring and abandoning prisoners; a Guard advancing into CC; Infiltration and Ambush Withdrawal; overstacked CC and overstacking advances; Field Promotion by MMC of different BPV and the MMC its leader defends with; the odds between 10 and 11 to 1; the berserk Ambush drm; and mandatory CC the package refuses. What it leaves out follows.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| The rest of J2.31, and Gurkha and Japanese Hand-to-Hand (their -1 DRM, automatic declaration) | R14.1 | J2.31 (not in the registered PDF), A25.43, G1.64 | A registered J2.31; those nationalities' counters | Only the red Kill Numbers apply, by SSR. |
| A withdrawal into a concealed enemy unit's Location | R14.2 | A11.21 | Withdrawal destinations with unknown units | Such a Location is not offered. |
| Later attacks of the prisoners' round leaving out a Guard an earlier one eliminated | Referee, pass 14 | A20.55 | Sequential declaration of the prisoners' attacks | Every prisoner attack takes in the Guard (R14.6). |
| Prisoners' Withdrawal from Melee while still guarded, recapture by entering Unarmed units' Location in the MPh, Scrounging, and the exchange of prisoners left alone for Green or Conscript units | R14.6 | A20.54, A20.55, A20.552, A20.221 | CC in the MPh; Scrounging DR | A prisoner that attacked is freed and may withdraw as an Unarmed unit; the rest is not built. |
| An Unarmed counter's own Morale Level | R14.6 | A20.5 | The Unarmed counters' printed values | An Unarmed MMC's NTC uses its unit's Morale Level (R0.3). |
| A Guard squad's automatic Deployment, prisoners sharing a Guard's TI or entrenchment, and escorting prisoners off a Friendly Board Edge | R14.5 | A20.5, A20.51, A20.53 | Deployment outside the RPh; entrenchments; exit | Not built. |
| A Disrupted unit's surrender in phases other than an enemy advance into its Location | R14.11 | A19.12 | A per-phase check for Good Order enemies in its Location | Only the advance is read. |
| The stacked SMC or MMC in the mandatory CC check | R14.14 | A15.43, A11.14 | Stacking in the requirement's read | The unit's attack alone is tried. |
| Tasks that place TI | R14.3 | A4.8 | Entrenching, clearing rubble, and the other labor tasks | TI is read in CC, but nothing places it. |
| Fire at the firer's own Location holding an Unarmed enemy unit is refused with the text for a unit named twice | Table player, pass 14 | A7.212, A20.54 | A same-Location fire text in the Fire package | Refused. |
| A concealed Guard keeping its "?" after abandoning its prisoners in its Location | Table player, pass 14 | A12.14 | Concealment loss when an enemy unit shares the Location | It stays concealed. |

## 25. Added by the backlog pass 15

The backlog pass 15 (2026-09-29) built, and removed from sections 2, 5, 6, 10, 11, and 22: FT, DC, and MOL; Snipers; Commissars; Allied Troops' leadership penalty; NKVD Field Promotion; attacks on units with an underscored Morale Factor and FPF by them; Green MMC; a hero's MG; a hero created by a concealed MMC; the nationalities other than German and Russian in the Heat of Battle and Leader Creation drm; the Italian Heat of Battle exception; British and Finnish Cowering immunity, Finnish Self-Rally, and the Finnish leader ranks; the ELR of 5 of an underscored Morale Factor; one FT or DC per unit per Player Turn; and a berserk unit's choice of the 1PP SW it keeps. What it leaves out follows.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| A FT, DC, or MOL against an AFV, and a DC's Position DR | R15.1 to R15.4 | A22.34, A22.612, A23.5, C7.34, C7.346 | The HE and Flame To Kill Table | Refused: a vehicle in the target Location is outside the attack. |
| Flame and Blaze from a FT, DC, or MOL; a DC's Rubble and Breach | R15.1 to R15.4 | A22.35, A22.6111, A23.41, B24.11, B25.12, B25.13, B23.9221 | Kindling and Flame in the game | No Flame, Rubble, or Breach is placed. |
| A Set DC, and a DC Placed or Thrown to another level (down a stairwell, from a building's upper level, across a cliff) or across a hexside TEM | R15.2, R15.3; referee, pass 15 | A23.3, A23.6, A23.7 to A23.72 | Levels and hexsides in the DC's Location read | Only same-level ADJACENT Locations, with the target Location's in-hex TEM. |
| Placing a DC where any vehicle is (its attack on the vehicle, and the PAATC for an enemy AFV) | R15.2; referee and table player, pass 15 | A23.3, A23.5, A11.6 | The DC against vehicles; the PAATC before a Placement | Refused. |
| A vehicle entering a Location after a DC is operably Placed there | Table player, pass 15 | A23.4, A23.5 | The DC against vehicles | The detonation is refused, the AFPh still ends, and the Placement lapses without effect. |
| A MOL across a road hexside at a unit on the road, a MOL Kindling Attempt, and leadership over more than four MOL FP | R15.4 | A22.611, A22.613, A22.62 | Road hexsides and Kindling | The woods and orchard hexside bar applies without the road exception. |
| Sniper attacks from DRs outside fire records: To Hit, PAATC, Rally-phase and Rout-phase checks in the MPh, Entrenching | R15.5 | A14.1 | A roll hook in every package | Only the IFT, MC, and TC DRs of fire records call a Sniper. |
| Sniper repositioning by forfeiting an attack, Sniper Checks, and attacks on vehicles, PRC, Snipers, and Interior Building Locations | R15.5 | A14.2, A14.22, A14.31, A14.33, A14.4 | The Sniper player's choices; vehicles as Sniper targets | The Sniper never forfeits; only Personnel and Dummies are targets, an enemy Sniper counter is not (table player, pass 15), and Interior Building Locations are not excluded. |
| The Sniper counter's setup placement requirement (six hexes of six enemy-occupied hexes) and changes to a SAN | R15.5 | A14.1, A14.2 | A setup check of the counter's hex | The counter is placed where setup puts it. |
| The Sniper player's choices of Location in a hex and of equidistant hexes | R15.5 | A14.2, A14.21 | An owner's choice before the attack | The most occupied Location, then the lowest TEM, then the first hex by name. |
| The Commissar substitution limits and the 8+1's availability | R15.6 | A25.22, A25.224 | Scenario OB | Setup places Commissars freely. |
| Human Wave | R15.6 | A25.23 to A25.234 | A multi-hex movement declaration | Not built. |
| Allied Troops' leadership in CC, in Deployment and Recombination, and an ally's SW used without captured penalties | R15.8 | A10.7, A21.1 | Nationality in those packages | CC and Deployment keep their nationality rules; an ally's SW counts as captured. |
| An SSR assigning an ELR of 4 or less to underscored units | R15.9 | A19.132 | Scenario cards | The underscored exception always applies. |
| A hero firing a LATW, a light mortar, or a Gun | R15.11 | A15.23 | Those weapons' packages reading a hero | Refused. |
| Japanese, Axis Minor, Allied Minor, Chinese, and Partisan units, and the other national rules of A25 for the new nationalities (American broken Morale Level, Italian Lax, PAATC, capture, and escape, Finnish Ski and Cold rules, French rules) | R15.13 | A25, Chapter G | Their counters and rules | Not built; the new nationalities follow the general rules but for their Heat of Battle, Leader Creation, Replacement and Battle Hardening, and the British and Finnish exceptions above. |
| The leaders' broken Morale Levels of the new nationalities and the Commissars | R15.13 | A1.4 | Printed counters | Their front morale, as every leader of the catalog (R0.3). |
| A DC's attack asks no owner's option | Pass 15 build | A15.3, A7.309 | Resuming a DC's two records after a choice | A DC's attack takes every Battle Hardening and Unlikely Kill option. |
| LLMC after a Sniper eliminates, wounds, or breaks a leader | Referee, pass 15 | A14.3, A10.2 | A LLMC hook after a Sniper attack | No LLMC follows a Sniper attack. |
| A Placed DC halved when only some of its targets were concealed at Placement | Referee, pass 15 | A23.3 | Per-target concealment in the DC's attack | Halved only when every target was concealed at Placement. |
| A broken Commissar's duty to Self-Rally before the other units of his Location | Referee, pass 15 | A25.222 | A rally order in the RPh | The RPh waits for every broken unit of his Location, in any order. |
| The Encircled DRM of a DC's attack by an Encircled user | Referee, pass 15 | A7.7, A23.2 | Encirclement in the DC's attack | No Encircled DRM is added to a DC's attack. |
| Clearer answers once Defensive First Fire has eliminated every mover | Table player, pass 15 | A8.1, A4.1 | The movement window closing with its last mover | pass-fire is accepted with no mover left and end-move answers with the state error UNIT-STATE-007; advancing the phase works. |
| The end-move refusal's wording once the placer of a DC was Replaced and broken | Table player, pass 15 | A23.3 | The DEFENDER's window text reading the placer | end-move says the DEFENDER may still fire; advancing the phase works. |

## 26. Added by the backlog pass 16

The backlog pass 16 (2026-09-29) built, and removed from sections 2, 6, 11, 19, 21, 22, and 23: night (the NVR, Illumination by Starshells and Blazes, Gunflashes, the Low Visibility DRM, rout, DM, concealment, movement, the Ambush, SAN, and Recovery at night), the Wind Change DR, the weather (Overcast, rain, Gusts, Mist, Mud, Falling, Ground, and Deep Snow, and Extreme Winter with its Fate), and their costs for vehicles. The scenario cards are pass 17. What it leaves out follows.

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| Straying, Jitter Fire, Cloaking, and Lax, Normal, and Stealthy units at night | R16.5 | E1.4 to E1.43, E1.53 to E1.55, E1.6 to E1.63 | A Movement DR and the Majority Squad Type | Units move as by day; no Cloaking counters. |
| A moving ATTACKER with an NVR of 0 entering a concealed DEFENDER's Location | R16.2 | E1.13 | The A12.15 return | The unit is returned as by day. |
| The Scenario Defender at night: HIP and "?" allotments, Freedom of Movement, the ELR one lower, and Recon | R16.1 | E1.2 to E1.23 | Scenario cards (pass 17) | Setup places units freely; every unit may move. |
| Fortifications hidden at night, and a Factory's NVR of 1 | R16.2 | E1.16, E1.17 | Fortifications; Factory reads | Not built. |
| A Fire Lane beyond NVR and Bore-Sighted Fire Lanes at night; To Hit from a Blind Hex at night; the captured MG's Sniper dr | R16.3 | E1.71, E1.73, E1.76 | Those windows | Fire Lanes need the NVR as other fire; no Blind Hex TH case; no Sniper dr. |
| Target Acquisition only when Illuminated at night | Referee, pass 16 | E1.74, C6.5 | The Illumination read in the Ordnance facts | An Acquisition applies at night as by day. |
| Ordnance at a Gunflash beyond NVR | R16.2 | E1.81, C6 | Area Fire for ordnance at night | Refused. |
| Gunflashes of Opportunity and No Fire counters, a FT's attacked Location, a detonated DC, mines, and Searching | Referee, pass 16 | E1.8, E1.83 to E1.86 | Their Gunflash markers | Only units and weapons with Prep, First, Final, Bounding, or Intensive Fire counters, and Melee, are Gunflashes. |
| "Known" at night outside fire: routing toward a Known enemy, and a berserk unit's charge | Referee, pass 16 | E1.33, E1.533, A10.51 | An NVR read in those checks | They read the LOS as by day. |
| IR, trip flares, a Blaze's shadows, a Starshell off the map, method 2 along the LOS or at seven or eight hexes, and the motorized vehicle trigger of E1.91 | R16.8 | E1.91, E1.922, E1.93 to E1.953, E1.941 | Offboard placement; Blind Hexes of Illumination | A Starshell stops at the map edge; method 2 only at the target hex within six hexes. |
| A vehicle changing its VCA without moving, as a moving vehicle for NVR | R16.2 | E1.14 | The VCA change in the sight read | Only a vehicle in Motion. |
| The Wind Change DR of the opening RPh, Wind Force and Direction, a DR of 2's wind change, Fog density and Heavy Winds | R16.10 | B25.63 to B25.65, E3.312 | Wind state | Not made; the game starts in that RPh. |
| Fog, Winter Camouflage, Drifts, and Ice | R16.9 | E3.31 to E3.313, E3.712, E3.75, E3.722 | LOS through Fog levels; Ice rules | Refused at setup (Fog, Drifts) or not applied. |
| Marsh and brush as Open Ground in snow, frozen streams, and minefields in Deep Snow | Referee, pass 16 | E3.722, E3.73, E3.732 | A weather-aware terrain key | Marsh and brush keep their day rules. |
| The Clear weather between Locations of one building | R16.11 | E3.8 | Building identity in the LOS read | Only the firer's own hex is Clear. |
| Manhandling, Entrenching, and foxholes in Mud, snow, and Extreme Winter; the Axis vehicles' immobilization in Extreme Winter | R16.12 to R16.14 | E3.61, E3.63, E3.722, E3.7332, E3.743, E3.744 | Manhandling and Entrenching | Not built; E3.744 is the scenario cards'. |
| Extreme Winter's Fate in a pillbox, and the DYO Weather and NVR Tables | R16.14, R16.1 | E3.742, E3, E1.11 | Pillboxes; scenario cards (pass 17) | Only buildings shelter from Fate; the SSRs name the weather and NVR. |
