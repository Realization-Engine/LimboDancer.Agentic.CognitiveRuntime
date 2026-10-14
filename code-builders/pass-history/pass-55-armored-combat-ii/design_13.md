
## 13. Gun Duels: 55.7 (increment 2g)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session. The task was carried in from pass 50 (its design, section 6, question F5).

### 13.1 Departures from the plan's entry and the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The slice, as the frame said (rulings R55.19, R55.20):** ordnance on both sides, the vehicle's Gun Duel DRM strictly lower, the first duel of an expenditure. **When the two totals are equal no duel is offered, though the rule gives one** with the Final DRs deciding the order. Fire that is not ordnance, the second duel with the ROF as a DRM, a duel declared by the DEFENDER (C5.33), and a Gyrostabilizer are left with it.
- **A LATW's declared shot is not dueled** (a PF, a PSK, an ATR): a PF's shot waits on its own check, and the three are kept alike. The frame left this to the build.
- **A Gun stacked with other Infantry is offered no duel:** the tank's shot at such a Location is refused by a check older than the pass, so its potential shot cannot be planned.
- **An Armor Leader who is not yet revealed is not counted in the vehicle's Gun Duel DRM at the declaration** (ruling R55.29). The declaration is the DEFENDER's act, and a proposal read and cancelled would otherwise show the enemy his line. This can deny the ATTACKER a duel that his modifier would earn.
- **A declined duel does not bar another** at the same expenditure; a fought one does.
- **The projector trusts a declaration:** it holds a duel shot to a `duel` answer for that vehicle at that expenditure, and does not ask again whether the choice was rightly offered.
- **Special ammunition is not offered for the duel's shot:** the vehicle fires AP at a vehicle and HE otherwise.

### 13.2 What is built

**The declaration.** When the side that is not phasing proposes a Defensive First Fire shot by a Gun or a vehicle's MA at the moving vehicle, Rules is asked, before any roll, whether that vehicle may duel: the firer is not concealed; the vehicle is not conducting an OVR; its MA could Bounding First Fire at the firer now but for the open window, by the same facts and checks as any such shot; that shot turns no turret and no hull; and its Gun Duel DRM is lower than the firer's. The Gun Duel DRM is read from each shot's own To Hit DRM list: the firer-based cases and the Acquisition, by one classification of every DRM name the three calculators can add (a test scans their sources, so a name added later fails until it is classified), without Case C4's doubled dr. If the vehicle may duel, the commit holds no shot: it is the declaration, a pending choice of the kind `gun-duel` for the ATTACKER with the declared shot's arguments and both totals with their lines, which both sides read. The proposal says so before the commit: "Declaring this shot lets vehicle G1 answer with a Gun Duel (C2.2401): its Gun Duel DRM +7 (Case C2 +6, Case I +1) is lower than gun R1's +8 (Case A +8); no dice are rolled until it answers".

**The answer.** `decline`: the declared shot is planned and resolved. `duel`: one commit holds the vehicle's MA shot at the firer as Bounding First Fire, marked as the duel's, then the declared shot planned again against the state the first left; when the firer can no longer fire, the event `gun-duel-voided` with its cause ("the return shot of gun R1 is void: its crew is broken (C2.2401)"). If the vehicle's shot stops at a choice of its own, the owed shot is carried in that choice's resume and runs after it. The duel shot is a Bounding First Fire shot for the ROF, the counter, and the MP between shots.

### 13.3 Ruling, the new facts, the games, and the tests

- **R55.29.** An Armor Leader not yet revealed is left out of his vehicle's Gun Duel DRM when a duel is offered and in what is shown of it; the shot the vehicle then fires uses him and reveals him.

**New in a record:** the choice kind `gun-duel`; `gunDuel` on an ordnance shot; the event `gun-duel-voided`; `gunDuelOwed` in a later choice's resume; in the state, the duel in progress. A Defensive First Fire shot recorded before the pass replays where a duel would now be offered: the projector asks for no declaration.

In my Studio on port 6671, board 3, scripted dice, the adjudicator's view: a PzKpfw IIIH starts in [D9], enters [D10] with its turret turned east, and is still moving; a Russian AT Gun in the woods of [H10].

| Game | What it shows |
|---|---|
| `p55-duel-decline` | The Gun must turn two hexspines: its proposal names the duel, +7 against +8, and its commit draws no dice: "declares a shot at vehicle G1, which may fire its MA first". The German side declines: the Gun's shot resolves, Case A +8, Case J2 +4. Recorded. |
| `p55-duel-miss` | The German side fires first: "as Bounding First Fire (D3.3), first in a Gun Duel (C2.2401)", Case C2 +6 and the lower dr doubled, a miss; then the Gun's shot. One commit. Recorded. |
| `p55-duel-void` | The tank's shot hits on an Original 2 and breaks the crew: "The return shot of gun R1 is void: its crew is broken (C2.2401)". Recorded. |
| `p55-duel-none` | CONTROL: the Gun already faces the tank, its DRM 0 against +7: no duel named, the shot resolves at once. Recorded. |

Rules tests (the classification, the rule's own example at +6 and +6, each bar, ruling R55.29) and Play tests (declaration, decline, duel and miss, duel and void, a concealed Gun, equal totals, a second declared shot, the vehicle's shot stopping at a choice) in the pass's two classes; a page test of the choice. Run by the agent after the last change: `ScenarioA1Pass55RulesTests` 51, `BacklogPass55Tests` 58, `PlayPageVehicleTests` 9; before it, Rules 149, Play 306, Units 416 whole, MapStudio 62: none failed.

**The replay digest after increment 2f** (93,266 lines, from the copy of 2f's own build): no line differs from the base.
