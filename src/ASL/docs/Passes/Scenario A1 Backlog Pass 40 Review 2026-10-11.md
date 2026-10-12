# Scenario A1 Backlog Pass 40: Infantry I

The review of pass 40, written on 2026-10-11 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 40 Design](<ASL Unit Backlog Pass 40 Design.md>); its section 13 says what was built.

## Status

Merged into main as 14dca517 on 2026-10-12, with the gate passed and CI green. Built on branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc: the frame, eight tasks in eight commits, then the five reviews, their fixes, the Studio check, and these documents. The first five tasks were each committed at the user's word. The last three, the reviews' fixes, and rulings R40.12 to R40.26 were made under the user's word of 2026-10-11, "Complete Pass 40 without my intervention", and are approved by the user on 2026-10-12. On 2026-10-12, with the pass's report before them, the user said: "I accept all of your judgments from the latest pass."

Two things the user's answer to question F10 named as built are not: a CE or BU choice at setup, and the half-hex rule for entry and occupation (design 11.4).

## What a player now meets

- A stack is counted one way in every phase. Overstacking costs MF in the MPh as in the APh, and its DRM is on the IFT as on the To Hit and CC DR (A5.1 to A5.5).
- Across a road hexside the mover chooses the road's rate or the other terrain's cost. At the road rate it is fired on at the hex's center or at the point where it crossed, whichever the firer sees better (A4.132).
- Dash (A4.63).
- Hazardous Movement holds in every fire phase until the unit is pinned or broken or the Player Turn ends (A4.62, C10.3).
- A SW dropped mid-move gives no MF back; no item is portaged twice in a phase; a moved MMG or HMG does not fire in the AFPh; the leader's IPC goes to the MMC the mover names; no Recovery in Bypass; four MF for a unit that boards (A4.4 to A4.44, A4.11).
- An Infantry MMC may overrun a lone Known SMC: a NTC, double the MF, the SMC's move away, CC at once. Unarmed enemy units are entered and attacked in CC at once (A4.15 to A4.152, A20.54).
- When an Infantry MMC enters a Location of the DEFENDER's armed, unbroken Infantry, the DEFENDER must attack it before it may pass (A8.312). A crew that fired its Gun adds its FP to FPF (A8.311).
- A Guard of prisoners and an Unarmed unit Deploy and Recombine with no leader; a Guard squad Deploys at will; HS Recombine only at the start of the RPh (A1.31, A1.32, A20.5).

## The rulings

R40.1 to R40.26, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. R40.1 to R40.11 were approved by the user on 2026-10-11. R40.12 to R40.26 were decided without the user, each by the proposal that would have been put.

## The reviews' findings and what was done

One BLOCKER, fixed. The findings fixed:

| Review | What was wrong | What was done |
|---|---|---|
| Referee, enemy Locations, 1 (BLOCKER) | A squad already marked First Fire that holds a MG dropped out of the attack owed: its plainest attack named no MG, which Subsequent First Fire refuses. | Rules says how many MG a unit fires in the attack it owes (`WeaponsFired`): a marked unit fires them with its own FP. Play test `ASquadAlreadyMarkedFirstFireStillOwesTheAttackWithItsMg`. |
| Referee, enemy Locations, 2 and 5 | Two MMC with no leader could never make an OVR together; a leader's passed NTC let his squads enter without him. | `NtcCovers`: each MMC under a passed NTC whose taker enters with it. The state's record keeps the taker. |
| Referee, enemy Locations, 3 | The NTC's DRM added a wall or hedge to the hex's TEM (B9.31, p. 119). | `NtcDrm` takes the larger. Ruling R40.12 reworded. |
| Referee, enemy Locations, 4 | An Unarmed mover and a stack of Dummies could enter Unarmed enemy units (A20.54). | `MayRecapture` over the movers. |
| Referee, enemy Locations, 6 | Where the units form no one group, any one of them satisfied the bar, though others could have fired with it. | `FireBar` refuses while a unit that could fire in one group with those named is left out; Play asks the fire plan. Ruling R40.17 reworded. |
| Referee, enemy Locations, 7 | The bars named the DEFENDER's units to an ATTACKER who proposed fire. | The fire bar is read only for a proposal whose firers are of the non-phasing side. |
| Referee, enemy Locations, 8; UI 5 | A concealed or hidden vehicle beside the SMC turned the NTC away; the DEFENDER's summary listed the Locations open to its SMC, so one left out told of a hidden ATTACKER unit. | The vehicle clause is gone from `TargetBar`; `EvadeText` names no Location. |
| Referee, enemy Locations, 10 | A Guard's Deployment at will in the RPh was written as a RPh action and then barred its side's Recombinations. | It is no RPh action in any phase. The design's words on which unit inherits the prisoners are corrected (11.2). |
| Rules boundary, 1 | A CC record made before the pass, with a crew at its Gun, would fail its check: the rebuilt facts hold `Manning` and the record does not. | The verifier takes `Manning` as recorded. |
| Rules boundary, 2, 3, 4, 7 | Play decided A8.312's preconditions, had its own usable-MG rule (which counted a dismantled MG), filtered the SMC's Locations, and the page decided A20.5. | `Owed` takes the four facts and asks the fire plan last through a reader; the fire package's `UsableMgs` is the one rule; `EvadeDestinations` filters from each neighbour's facts; the page reads `GuardSquadsAtWill`. |
| Rules boundary, 6 | The kinds of fire a Dash halves were tested by the caller alone. | The multiplier in Rules tests the kind and the ordnance hit. |
| Referee, movement, 1 | A Dash declared to a Location the stack cannot enter left it unable to step or to end. | `EndBar` is lifted when the move's own plan refuses the step into that Location. |
| Referee, movement, 2 | One mover's break or pin ended the Dash for the whole stack. | The fire's Dash fact is read over the members still moving. |
| Referee, movement, 4 and 5 | The target's overstacking DRM was read for a vehicle's fire at a stationary stack in the MPh and for vehicle targets, and it counted hidden and concealed units, so a proposal's DRM told of them. | `TargetPenaltyApplies` (the movers in the MPh; Personnel), and the count is over the units the firer knows. Ruling R40.26. |
| Referee, movement, 7 | A failed push and attacks other than direct fire took no Hazardous Movement. | The projector lists the crew on the Manhandling DR; the fact is set for every attack. |
| UI 1 | The leader's IPC name was never cleared and could be sent from a hidden select. | Cleared with the other move drafts and when its unit is unchecked; sent only when a stack moves. |
| UI 2, 3, 4 | The adjudicator had no path to the attack owed and an enabled pass; the attack owed was computed twice a render; a stopped stack's note still offered a pass. | One read a revision (`MandatoryNow`); the block serves the adjudicator; the note gives way to the attack owed. |
| UI 7 | The Guard's block did not say where the SW go. | It says they stay with the first HS. |
| Table player, 1 and 2 | The note for units with different fire counters gave a ruling as the rule, and the pass refusal contradicted it. | Both cite ruling R40.17 and say what the game does. |
| Table player, 3, 7, 8, 9; 6 in part | "Small arms" for the Dash's halving; "beside it" twice; `play.move-occupied` stale; the SMC's block promising fire a leader has not; two refusals that did not say what was wrong. | Reworded as the reviewer proposed. |
| Table player, 4 and 5 | A Guard's Deployment recorded under A1.31; a leader's NTC record without his stack or the cost of failing. | The record cites A20.5, and names the stack and the consequence. |
| The Studio check | The hand-over prompt told the DEFENDER "fire at it or pass" while the attack was owed. | It says the attack is owed (A8.312). |

The findings not fixed, each with a row in the backlog's section 56:

| Review | What | What happens now |
|---|---|---|
| Referee, enemy Locations, 9 | A lone SMC that enters Unarmed units has no CC in the MPh (A20.54, p. 88) | The recapture's CC asks for a MMC of the stack. |
| Referee, enemy Locations, 10 | A crew at its Gun has a US# of 2, where A1.6 gives an attendant crew 4 (A1.6, p. 45) | No rule built reads it for a crew at its Gun. |
| Rules boundary, 5; referee, movement, 12 | The woods-road exception for vehicles, the Dash's SMOKE and DC bar, the bar on the other terrain's cost, and the two exceptions to A5.11 are decided in Play (B13.31, A4.63, A4.132, A5.11) | Each works; each belongs in the Rules member beside it. |
| Rules boundary, 8 and 9 | What a Guard's Deployment at will waives, which acts end the start of the RPh, the two-HS test, and the recapture's exceptions are decided in Play (A20.5, A1.32, A20.54) | Rules gives the yes or no; Play lifts the bars one by one. |
| Referee, movement, 3 | One mover's break ends Assault Movement for the whole stack (A4.61, p. 51) | The unbroken movers take FFNAM in later First Fire. |
| Referee, movement, 6 | The road's second target point is tested only inside the target hex (A4.132, p. 48) | A firer with a LOS to the hexside point alone is refused; one whose line to that point is blocked outside the hex still gets Open Ground. |
| Referee, movement, 8 | A CX leader still lends his IPC (older than the pass) (A4.42, A4.52, pp. 50 to 51) | A HS with 5 PP and a leader who Double Times has six MF, where the page's example has five. |
| Referee, movement, 9 | Ruling R40.9 halves a whole Fire Group against a Dash (A4.63, A7.52) | The page says the penalties apply to some DEFENDERS and not others; halving by firer is the nearer reading. For the user's second look. |
| Referee, movement, 10 | The leader's IPC: only a laden MMC of his nationality is helped, and the unasked default is not kept in the record (A4.42, p. 50) | A second MMC that becomes laden mid-move takes the IPC from the first. |
| Referee, movement, 11 | The overstack MF is added after an Encircled unit's doubling (A7.7, A5.11) | As in the advance (rulings R12.11, R14.10). |
| Referee, movement, 1 (rest) | The overstack MF and the lost Road Bonus of the Location Dashed to are not in the Dash's MF check (A4.63) | A Dash may be declared that the second step cannot pay; the stack may then end in the road. |
| UI 6 | The "Leader's IPC to" list offers units that cannot be helped, and shows hexes without brackets (A4.42) | The planner refuses the wrong choice. |
| Table player, 6 (rest) | The Dash's refusal lists eight causes in one sentence (A4.63) | The player is not told which applies. |
| Table player, 10 | Eight games share three labels, and four that are not fixtures do not say so ((games)) | `p40-ovr-stack`, `p40-board`, `p40-bd04-dash`, and `p40-bd05-road` are not fixtures; the design says so, the labels do not. |
| Table player, 11 | The overstacking clause shows the rounded figure as the overstack (A5.11, p. 52) | "overstacked by 2" where the Location is over by one and a half; the MF charged is right. |
| Table player (not checked) | Whether a squad that takes its own NTC beside a leader should take his leadership DRM (A10.7, p. 68) | A leader in the stack always takes the NTC; the player names one MMC alone to avoid it. |
| Replay | A Guard's Deployment is filed as a "rally" step on the Replay page ((page)) | It is shown with the RPh's actions. |

## The Studio check

Design section 12.4. The pass's controls fit at the five widths; a full Player Turn was walked at 320 pixels and a full OVR step at 1920 by keyboard; the hand-over prompt's words were found wrong for the attack owed and corrected.

## What was not done

- A Studio game for a crew's FP in FPF and for the butted half-hex: neither can be reached through the page (design 10.2, 11.5). Tests hold both.
- Each width in each view for every control, and the Replay page.
- Play tests for the recapture, the stack held in Melee, the leader's IPC choice, the Assault step, the advance through SMOKE, boarding, and Recovery in Bypass: each was seen in the Studio or rests on a Rules test.
