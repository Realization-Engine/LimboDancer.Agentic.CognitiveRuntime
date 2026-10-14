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
