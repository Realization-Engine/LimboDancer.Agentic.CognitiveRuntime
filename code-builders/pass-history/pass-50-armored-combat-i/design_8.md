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
