
## 12. Motion attempts and the DEFENDER's Motion firer: 55.3 (increment 2f)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **An attempt that no dr could pass is refused and costs nothing:** with less than one MF or MP spent in the vehicle's LOS the attempt is not rolled (ruling R55.28).
- **A facing or a turret named for a vehicle in Bypass is refused;** the attempt alone is allowed there. Bypass's CA is not built.
- **The projector admits a Mechanical Reliability or Bog check of a vehicle that is not phasing** while the window is open and the vehicle has made no attempt; it does not ask that the attempt's own record follows.
- **Not built:** a stall; the platoon's and the radioless AFV's drm (D14.211, D14.23); the Convoy's bar.
- **A vehicle's own Bog and Mechanical Reliability checks have no record line of their own** on the page, as before the pass; the attempt's line says what they did to it.

### 12.2 What is built

**The attempt (rulings R55.11 to R55.13).** `asl.game.motion-attempt`: a vehicle of the side that is not phasing, while the window on an enemy ground unit's expenditure is open, with a VCA and a TCA to take on success. Rules bars a vehicle that is immobilized, bogged, mired, stunned, shocked, Abandoned, a wreck, or Recalled in this Player Turn; one under a First, Final, or Intensive Fire counter; and one that has attempted in this MPh. The moving unit must have begun its move out of the vehicle's LOS, read at the Location it began in, or the attempt is refused with the reason and none is spent. The number is the sum of the MF or MP of the mover's steps that ended in the vehicle's LOS, its Start, Stop, and Delay MP among them; a half adds nothing to a dr. Of a moving stack the unit that began out of LOS and has spent the most is answered.

**On a pass:** a red-MP AFV rolls for Mechanical Reliability, and a 12 immobilizes it with neither Motion nor the change (D2.51). A VCA change in woods, a building, or rubble takes a Bog Check for each hexspine, the shorter way round; bogged, the vehicle gains neither. Otherwise it takes the Motion counter, the VCA, and the TCA, and loses its Acquisition if either changed. A failure leaves only the mark of the attempt. A new event, `motion-attempted`, and the state's list of the phase's attempts.

**The DEFENDER's Motion firer (ruling R55.14).** A vehicle in Motion of the side that is not phasing fires its MA in the MPh and the DFPh with Case C alone, the lower dr of its To Hit DR doubled, no Acquisition DRM, and no Case L; a turret turned for the shot pays Case A, which C5.13 bars only in the firer's own MPh. Its MGs fired halved before the pass and still do.

**The page.** The DEFENDER's part of the page gains "Motion attempt (D2.401)": each of the side's vehicles with what the mover has spent in its LOS and the dr that passes, or the reason it may not attempt; a VCA and a TCA to take.

### 12.3 Ruling, the new facts, the games, and the tests

- **R55.28.** A Motion attempt against a mover that has spent less than one MF or MP in the vehicle's LOS is refused and is no attempt.

**New in a record:** the event `motion-attempted` (the vehicle, the roll, the half points, the result, the mover, the facing, the turret); in the state, `motionAttempts`. No fact is added for the Motion firer: its shot was refused before, so no record holds one.

In my Studio on port 6671, board 3, scripted dice, the adjudicator's view: a PzKpfw IIIH starts in [D9], out of LOS of a T-34 in the woods of [H10], and enters [D10].

| Game | What it shows |
|---|---|
| `p55-mot-pass` | After the Start MP: "has spent 0 MP in vehicle R1's LOS", no attempt offered. After the entry: "1 MP in its LOS: succeeds on a dr of 1 or less". A dr of 1 with the VCA to the west: the Mechanical Reliability DR, three Bog DR for three hexspines in woods, then "it gains Motion and turns its VCA to west". The T-34 then fires its MA: Case C +4, Case I, Case J2 +4, "Case C4, the lower dr doubled" +2. Recorded. |
| `p55-mot-fail` | A dr of 4: "it fails, with no penalty but that it gains no Motion this Player Turn"; a second attempt is not offered. Recorded. |
| `p55-mot-immob` | A dr of 1 and a Mechanical Reliability DR of 12: immobilized, "neither Motion nor a change of CA (D2.51)". Recorded. |
| `p55-mot-seen` | CONTROL: the German tank begins in [D10]: "began its move in vehicle R1's LOS ... no attempt is spent". |

Rules and Play tests in the pass's two classes; a page test with an Infantry mover. One older test changed: pass 50's Rules test of a Motion firer asserted the refusal that is gone. Run by the agent: Rules 106 in four classes, Play 136 in six, Units 416 whole, MapStudio 38: none failed.

**The replay digest after increment 2e** (93,266 lines, from the copy of 2e's own build): no line differs from the base.
