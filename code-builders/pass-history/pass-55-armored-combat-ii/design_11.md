
## 11. Armor Leaders and Inexperienced Crews: 55.5 (increment 2e)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 11.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **A possible leak is not closed:** a pending choice that interrupts a shot at his tank keeps the shot's facts whole in its resume, the crew's morale among them, before he is revealed.
- **A card's setup plan cannot give a vehicle an Armor Leader;** a hand setup on the page and a game file can.
- **The map counter's tooltip and the placement list's row do not name him;** the units table and the firer's label do, for his owner.
- **Whether a vehicle is an armed AFV is checked by the planner's setup bar,** not by the projector, which refuses only a leader on something that is no vehicle, a bad grade, or a leader beside the Inexperienced Crew. A hand-written file with a leader on a truck replays.
- **Not built, as the frame said:** the HD Maneuver dr, the halftrack's FG with its Passengers, the assisting AFV of D8.3, the leave to withhold his modifier (A10.72), his return when the crew re-crews, his VP.
- **Added beyond the brief:** an Inexperienced Crew's +1 lowers Residual FP as a firer's positive DRM does (A8.26).
- **No Studio game of an OVR or of the vehicle's Close Combat attack with him;** their Play tests hold those two uses.

### 11.2 What is built

**The leader (ruling R55.15).** A vehicle instance carries `armorLeader`, a morale and a leadership of the grades 8-1, 9-1, 9-2, and 10-2, and whether he is revealed. The page's placement form offers him for an armed AFV (`#place-armor-leader`). The enemy's view shows neither him on the unit nor his part of the record that created the vehicle, until the first record that carries his modifier or names his morale in a check; from then on he is public. He is lost with a Recall, a wreck or an elimination, and when the vehicle is Abandoned (D5.341, D3.43).

**His modifier (ruling R55.16; D3.44)** is a DRM line of its own, "Armor Leader -1 (D3.44)", on the MA's To Hit DR on the Infantry and Vehicle Target Types, on the IFT DR of a MA that is a MG, on the OVR DR (D7.16), on the vehicle's own Close Combat attack, and on the colored dr of Bog Removal (D8.3). BU does not void it. A tank's BMG and CMG take none.

**His morale (ruling R55.17; D3.42)** replaces the nationality's in the two checks the code has, the Shock and Immobilization checks of a hit and the CE crew's MC under fire, without his modifier. A record names the crew's morale only when that crew checked, so that a miss tells the enemy nothing; the check's record says whose morale it was ("crew morale 10, its Armor Leader's (D3.42)").

**Inexperienced Crews (ruling R55.18; D3.45).** The condition `asl:inexperienced-crew`, given at setup (`#place-inexperienced-crew`), public: +1 on each of those rolls, a crew morale of 6, never lost; not with an Armor Leader.

### 11.3 Ruling, the new facts, the games, and the tests

- **R55.27.** A record carries the morale of a target's crew only when that crew took a check in it.

**New in a record:** `armorLeader` on an instance; `armorLeadership` on a vehicle firer's facts (ordnance, fire, OVR, Close Combat) and on a Bog Removal's check; `crewMorale` on a vehicle target and `moraleSource` on a check. Each is absent without a leader or the condition, which no older game has.

In my Studio on port 6671, board 3, scripted dice: a PzKpfw IIIH in [D10] and a T-34 in [F10].

| Game | What it shows |
|---|---|
| `p55-al-fire` | The German tank with a 9-1 fires AP: "- 1 (Armor Leader, D3.44)". The Russian view's row for the tank does not name him before the shot and reads "with Armor Leader 9-1" after it. Recorded. |
| `p55-al-ctl` | CONTROL: written, not played; `p55-al-shock`'s firer has no leader and shows the same shot without the line. |
| `p55-al-shock` | The T-34 hits the tank with a 10-2 and the TK DR is a dud: Final TK# 9 read; no check, so no morale in the record. Recorded. |
| `p55-al-shock2` | The same hit with a TK DR of 10: "possible Shock; NTC DR 10 against 10 (crew morale 10, its Armor Leader's (D3.42)): passed"; the Russian view then names him. Recorded. |
| `p55-al-inexp` | A T-34 with an Inexperienced Crew fires: "+ 1 (Inexperienced Crew, D3.45)"; its row reads "Inexperienced Crew (D3.45)". Recorded. |
| `p55-al-bog` | A bogged tank with a 9-1 starts: "freed on a colored dr of 1 to 4 (Armor Leader -1 (D3.44))"; a colored 5 frees it. Recorded. |

Rules tests in `ScenarioA1Pass55RulesTests` and Play tests in `BacklogPass55Tests` for each use, the view, and the loss; a Units test of the loss by Recall, Abandonment, and elimination. Run by the agent: Rules 354 in fourteen classes, Units 416 whole, Play 226 in fifteen classes, MapStudio 49 in eight: none failed.

**The replay digest after increment 2d** (93,266 lines, from the copy of 2d's own build, the pass's new properties left out on both sides): no line differs from the base.
