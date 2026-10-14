
## 8. Fire at another level: 55.4 (increment 2b)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The halftrack's AAMG is held to C2.6's limit like any vehicle MG.** The rule names "non-AA vehicular MG", so an AAMG may be free to fire at a higher target nearer than the elevation difference; what an AA weapon may do is pass 105's, with the AA Guns.
- **A Critical Hit does not reverse the Height Advantage TEM (C3.71).** The hit's IFT attack does not carry the fact; an ordnance Critical Hit uphill gets no -1.
- **A wall or hedge the LOS crosses gives an ordnance shot no TEM on its To Hit DR.** This is older than the pass: the shot's map read finds the hexside's TEM and drops it, and no ordnance calculator reads it. It is not repaired here, since the repair would change what a shot recorded before the pass resolves to; it has a backlog row. What the pass does with it: such a target claims no Height Advantage, since it has another positive TEM to claim (ruling R55.24).
- **No Play test and no game for a vehicle's MG at another level of its own hex:** the setup refuses a tank in a building. A Rules test holds the bar.
- **A cliff** (B11.31, B11.32) refuses an ordnance shot at an adjacent Location of another level, with the LATW, ATR, PIAT, and mortar exceptions; a Rules test only, since no board in the tests has a cliff.

### 8.2 What is built

**The limit (C2.6).** A Gun, a tank's MA, and a mortar firing down fire at a Location of another level when the range is at least the elevation difference, on the Infantry, Vehicle, and Area Target Types; a mortar fires up at any range and a LATW is free of the limit ("but not to SW [EXC: they do apply to MTR/INF/RCL ordnance SW]"). A vehicle's BMG, CMG, and MA AAMG fire on the IFT under the same limit. The two refusals "at different levels are not reviewed" are gone. Another level of the firer's own hex stays refused, for a Gun and for every vehicle weapon, with the rule in the refusal.

**Height Advantage (B10.31, C6.8, D4.22).** The shot's map read already found it, with the Crest Line exception, and dropped it. It is now a fact of the shot, and Case Q +1 on the To Hit DR of the Infantry and the Vehicle Target Type when no other Case Q of the shot is a positive TEM; a vehicle in Bypass, which takes no terrain TEM, may take it. With it the mover's Case J4 is not applied ("not subject to Interdiction/FFMO by an attack to which that +1 TEM applies"). Not on the Area Target Type: a mortar's fire is Indirect Fire (C.1). A vehicle's MG fire reads Height Advantage on the IFT as Infantry fire does.

**The old lines beside it,** each read against a firer and a target at different levels: the To Hit cases A to P and R, and all of the To Kill run, are right as they stand (Case L has no level clause; no To Kill case reads the firer's elevation but the Aerial, DC, and MOL one); a vehicle's MG no longer doubles at Point Blank Range against a target more than one level above (A7.21; ruling R10.4's bound).

### 8.3 Rulings, the new facts, the games, and the tests

- **R55.23.** C2.6's limit holds for a vehicle's MA AAMG as for its BMG and CMG.
- **R55.24.** A target behind a wall or hedge the LOS crosses claims no Height Advantage on a To Hit DR.

**New in a record:** `heightAdvantage` on an ordnance shot; on a vehicle's fire attack, `heightAdvantage` and `targetLevelAbove`, which Infantry fire had. A record made before the pass is at one level and has none: no Case Q line, Case J4 as it was, the PBF as it was.

In my Studio on port 6671, board 3, scripted dice; each pair's LOS was read on the board viewer first.

| Game | What it shows |
|---|---|
| `p55-ha-inf` | A German INF Gun in [E6] fires up at a squad on the hill [E5]: Case L -2, "Case Q, height-advantage, B10.31" +1, a hit. Recorded. |
| `p55-ha-veh` | A PzKpfw IIIH in [E6] fires AP up at a T-34 in [E5]: Case I, Case L, and Case Q +1 (D4.22). Recorded. |
| `p55-ha-down` | CONTROL: a Russian AT Gun in [E5] fires down at [E6]: Case L alone. Recorded. |
| `p55-cmg-up` | A PzKpfw IIIH in [N4] fires its CMG, then its MA with HE, at a squad on level 1 of the stone building [N2] at range 2: the building's +3 as Case Q, no Height Advantage. Recorded. |
| `p55-own-hex` | A Gun at ground level of [M2] is refused level 1 of its own hex: "does not fire at another level of its own hex ... (C2.6)". A refusal, nothing recorded. |

Rules tests: six more in `ScenarioA1Pass55RulesTests`. Play tests: `BacklogPass55Tests`, new. Run by the agent: Rules 259 in fourteen classes, Play 149 in eleven: none failed. One older test changed: `RefusalReasonsTests` named the refusal that is gone.

**The replay digest after increment 2a** (473 games, 93,266 lines; the base built from main's sources at 846eb5a3; the catalog's identity, which is hashed into every state and went to 1.14.0, left out on both sides): no line differs. The first trial, without that, differed in every state's line and was a void run; a dump of one state named the two properties in a minute.
