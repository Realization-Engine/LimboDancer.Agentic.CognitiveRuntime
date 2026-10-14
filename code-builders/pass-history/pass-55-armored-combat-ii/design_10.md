
## 10. Canister: 55.2 (increment 2d)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **Canister is not fired as Defensive First Fire.** In the MPh it is refused with its reason. Ruling R55.10's first half (FFMO and FFNAM for Canister) is therefore not built. It would need the mover's own Location among several attacked, FFMO and FFNAM for one Location of several, the shots a Gun has at one expenditure (C6.17), and Residual FP, which A8.2 gives Canister (C8.31 denies it to the AP kinds only).
- **A Location that the IFT refuses refuses the whole shot:** an AFV in positive TEM (ruling R25.6), a Melee, prisoners. For a hidden AFV the refusal could tell the firer something; not tested.
- **One filter stands outside Rules:** a fire record of a Canister shot is marked as ordnance's, and A7.55's bar on a second attack at the same Location passes over such records at three places, two in Play and one in the projector. Without it Infantry beside the Gun could not fire at a Location the Canister attacked. It is a rule's clause decided outside Rules, and has a backlog row for the boundary.
- **A pending choice in one attacked Location** resumes the others through the follow-up the fire planner already has; no test reaches it.
- **Left out as the frame said:** the building option, an aim at another elevation, the SMOKE and LV clause, pillboxes, Infantry that mans a Gun in the MPh, the Bypass TCA, an AFV with Canister (so the Bounding First Fire and Motion halvings are in the calculator and its tests and in no game).

### 10.2 What is built

**The shot (rulings R55.8, R55.9).** `asl.game.fire-ordnance` with the ammunition `canister`, an `aim` of 1 to 5, and an `aimFacing` when the CA turns first; it names no target. Rules holds the aim table in steps of the CA's two adjacent hexes; Play turns an aim into hexes on the map. It rolls no To Hit DR: one IFT DR, recorded on the shot, and a fire record on that same DR for every Location of the cluster and of C8.41's adjacent hex or hexes that is at the firer's elevation and in its LOS, an empty one too, so that the records do not tell the firer which hexes hold hidden units. Each Location takes its own TEM and Hindrance on the IFT DR; a CA change adds Case A to each (D3.52; the Gun's type, doubled in woods, a building, or rubble) and lowers the ROF as for any shot (C2.5).

**The FP** is 12, 20, or 24 by caliber, and each of these halves it: the adjacent hex (a quarter in each of the two when the LOS runs along their hexside), Bounding First Fire, Motion, Intensive Fire, a pinned crew; a concealed target takes half again by the IFT's own Area Fire. No PBF, no leader, no FG, no Cowering, no fire counter from the IFT's side.

**The one DR** is also the Depletion check with all three of C8.9's outcomes (above the number nothing is attacked and the Gun has not fired), the malfunction check, and by its colored dr the ROF check. The Gun's Acquisition is lost (C6.5). An AFV in an attacked Location is unharmed. Friendly units are not attacked (A7.4).

**The page.** A Gun that lists Canister shows its ammunition choice before any target; with Canister chosen, a facing (`#ordnance-canister-facing`, the Gun's own first, the others marked "turn first: Case A on the IFT DR") and the aims of that facing (`#ordnance-canister-aim`), each with its hexes and what the side's view knows there: "aim 4: [D9] [D10] [C10], and [E10] at half FP: 3 units". An aim with no hex on the map is not offered. The proposal lists each Location with its FP and terrain and says what the one DR also checks.

### 10.3 The new facts, the games, and the tests

**"Barring obstacles in the first hex of the cluster"** is read as the LOS to each Location, which the per-Location read gives (ruling R55.26).

- **R55.26.** A Location of the aim is attacked when the firer has a LOS to it; no further test is made of the cluster's first hex.

**New in a record:** on an ordnance shot, `rolls.canister`, `facts.canister` (the aim, the facing, the Locations), `resolution.canister`, and a null `toHit`; on each fire record of the shot, `facts.canister` and no firers; in the state, `ordnance` on a fire of the phase. Every one is absent from a record made before the pass, when the ammunition was refused.

In my Studio on port 6671, board 3, August 1943, scripted dice: a 37mm AT Gun in [F10] facing west; German squads in [D10], [E10], and, concealed, [D9]; a PzKpfw IIIH in [C10].

| Game | What it shows |
|---|---|
| `p55-can-used` | Aim 4, a DR of 2 and 3: "below the Depletion Number 7", four fire records: [D9] at 12 FP and half again against the concealed squad, [D10] and [C10] at 12, [E10] at 6; the ROF kept. Then aim 3 on a DR of 7: the last round, [E10] at 3 FP ("a quarter in each of the two adjacent hexes"), no more fire; the list then reads "it has run out". The tank is unharmed in both. Recorded. |
| `p55-can-none` | A DR of 8: "it had no Canister, attacks nothing, has not fired"; no fire record. Recorded. |
| `p55-can-malf` | A DR of 12: no Canister, and the Gun malfunctions. Recorded. |
| `p55-can-turn` | The CA turned one hexspine to the north-west: "Case A +3 on the IFT DR (D3.52)" on each of three Locations, [C9] with its woods +1; a colored 1 keeps the lowered ROF. An aim to the south-west, off the board, is not offered. Recorded. |
| `p55-can-1942` | CONTROL, July 1942: the list has HE alone, "none at the scenario's date". |

Rules tests: 30 in `ScenarioA1Pass55RulesTests` in all. Play tests: 26 in `BacklogPass55Tests` in all. A page test in `PlayPageOrdnanceTests`. Run by the agent: Rules 196 and 136 in two runs, Units 409 whole, Play 131, MapStudio 35: none failed.

**The replay digest after increment 2c** (93,266 lines, from the copy of 2c's own build): no line differs from the base.
