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
