# Increment 4f: wounds and Field Promotions (71.2, part I)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for 4a to 4e, and `frame_wounds_codemap.md` sections 1 to 3 (agent D's map, your main one). Read the diffs of the increments already committed.

## Pages
A17.1, A17.11, A17.2 (p. 85), A18.2 and its table (p. 85), D5.34 and D5.343 (pp. 203 to 204), A11.5's CC against a vehicle where A18.2's drm meets it.

## Build (rulings R71.14, R71.15, R71.17)
1. **The pin after more than 3 MF (R71.14).** One Rules member: a SMC wounded in the MPh after spending more than 3 MF (3 1/2 counts; `MfSpent`, `HalfMfSpent`) is pinned. In Fire (Defensive First Fire, Final Fire in the MPh, FPF, Residual FP) through a new optional fact on the fire target (JsonIgnore when null; absent, the attack resolves as it did); by the planner with the wound in a Sniper attack, a Search's Casualty, or Mopping Up's. The RtPh's case is backlog.
2. **The SMC crew's Stun wound (R71.15).** Where the planner writes a vehicle's Stun (Fire's `VehicleConditionEvents`, GamePlanner.Fire.cs about :1298; 4a's Sniper Stun), and the vehicle is manned by a SMC Temporary crew (`ObjectIndex.CrewOf`), it rolls a Wound Severity dr (`ScenarioA1Wounds.SeverityPurpose`, id `{attempt}-wound-{crew}`) before the vehicle's change. The projector's `SmcCrewAfter` (GameProjector.cs about :4343) reads it and `ScenarioA1CapturedVehicleRules.SmcCrewOutcome(..., int? severityDr)` decides: wounded, or eliminated on a 5 or 6 (+1 if already wounded, `ScenarioA1Wounds.Mortal`). With no dr in the record, today's result (a second wound eliminating) stands. A Recall eliminates him as today. `PlayRecords` names the man of that dr from its roll id.
3. **A18.2's -1 against an AFV (R71.17).** A new optional fact on vehicle CC (written beside `LeaderCreation = true`, GamePlanner.VehicleCloseCombat.cs about :601) says the vehicle is an AFV (armored); Rules adds -1 (`cc-vs-afv`) to the Leader Creation dr only when it is true. Absent, as before. Check `BacklogPass70Tests.ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack` and change it as intended if it moves.

## Not in 4f
Carrying a wounded man (4g). G.M.D. and an unlisted nationality: unchanged (R27.1). The wound pin in the RtPh: backlog.

## Tests by method
Rules: the pin member (3, 3 1/2, 4 MF; not in the MPh); `SmcCrewOutcome` with a severity dr (1, 5, a second wound with +1) and without; the Leader Creation drm with and without the fact. Play: a SMC moving 4 MF wounded by Defensive First Fire is pinned, at 3 MF not; a Sniper wound in the MPh pins the same way; a Stunned vehicle with a SMC crew rolls the dr and wounds or eliminates him; vehicle CC Leader Creation against a tank takes -1, against a truck none. Grep the wound and Leader Creation tests for the members you change.

## Studio game
`games_4f.py`: board 3, a German 8-1 leader moving four Open Ground hexes in the Russian view of a Russian squad's Defensive First Fire (name the DR for a wound), and a captured vehicle crewed by a SMC Stunned by a MG To Kill (or a Sniper dr 2 on its CE crew).
