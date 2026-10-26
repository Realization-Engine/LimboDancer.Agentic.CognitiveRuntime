# Increment 3b: the morale check outside fire (task 70.1, part I)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.3 to R70.10 of the design (section 6, F3 and F4). Frame A's report (`frame_morale.md`) maps every path, with file:line, in its section 1 table. Increment 3a is committed before you start: it gives `ScenarioA1Sight` and the nearest-seer read (a LLMC or LLTC now costs a "?" under R70.17).

## Pages
- p. 65: A10.2 (LLMC and LLTC: who checks, the reversed DRM, the exceptions for Riders, Passengers, other levels, CC; the LLTC when an unbroken leader breaks).
- p. 66 to 68: A10.5, A10.53 (the Interdiction NMC).
- p. 69: A10.7 and its EXC (a leader's reach), A10.72.
- p. 83: A15.1 (who is exempt from Heat of Battle) and its table.
- p. 85: A17.1, A17.11 (wounds, Wound Severity).
- p. 86: A19.11, A19.12, A19.13 (ELR immunity, Disruption, Replacement).
- p. 200: D3.44 (an Armor Leader's DRM never modifies a MC).
- p. 203, 205, 206: D5.311, D6.24, D6.651.

## Build
1. **The member (R70.3).** `ScenarioA1MoraleCheck` in Rules, built on the Fire calculator: an internal check-only branch of `ScenarioA1FireCalculator` that runs the existing resolution of a check (NMC, nMC, PTC, or none for a lost leader alone) on its subjects, then the leader-loss cascade, with no IFT roll. Reuse MoraleOutcome, Heat of Battle, ELR Replacement, wounds, the LLMC/LLTC cascade, and the prisoner, berserk, hero, and PRC branches as they stand. Its facts are the Fire calculator's (`FireTarget` with new optional facts, `FireRolls`, choices, the side's ELR, no quarter, in Melee); its verdict is the effects Fire gives. Do not refactor Fire's own attack path beyond what the branch needs.
2. **The record (R70.4).** A new event `morale-check-resolved` with the facts and the verdict, read and written like the fire records, its effects written through the Fire planner's own effect events (`GamePlanner.Fire.cs` about :1465). The projector checks it as fire records are checked.
3. **Unarmed (R70.7).** An optional `unarmed` fact on each target of a fire attack and a morale check, from `asl:unarmed`, read by Heat of Battle's subject (A15.1) and ELR immunity (A19.11). Absent means as before.
4. **The callers.**
   - The rout Interdiction NMC: Heat of Battle on an Original 2 (+1 broken), the LLMC after a leader's loss; R70.5: a Heat of Battle that rallies the unit or makes it berserk ends its rout where it was interdicted. Today `RoutInterdicted` carries `Drm 0`; keep the record and append the member's record after it where a follow-up is owed.
   - LLMC after a leader lost outside fire: Mopping Up, a Sniper, Rally Fate (a mortal wound), a PF or ATR firer's Casualty Reduction (and the LLTC when a leader's PF check breaks him), Failure to Rout. Each appends a `morale-check-resolved` with no check and the lost leader. Frame A names the lines.
   - The Bail Out NMC (R70.9): a Rider leader's DRM (A10.7's EXC), the Wound Severity dr for a SMC, and ELR Replacement, never Disruption (A19.12), through the member. **Fix the crash first:** a SMC Rider's Casualty Reduction passes a null severity (GamePlanner.Riders.cs:164 into RoutCalculator.cs:312). Record `drm` and `elr` on `rider-bailed-out`; the projector's check compares them only when present.
   - D5.311 (R70.10): a crew already broken that is attacked again is eliminated by Casualty Reduction; a recorded fact on the vehicle target says it was broken, absent meaning as before.
5. **PRC leadership and LLMC in Fire (R70.8).** `ScenarioA1PassengerActionRules.LeaderReaches` read in the check both ways (a Passenger leader aids fellow Passengers of his vehicle, and is aided only by them); a Passenger leader's loss checks fellow Passengers of the same vehicle only; Riders neither take nor cause LLMC (A10.2). New optional facts on the PRC entries; absent, as before. Heat of Battle for PRC stays none (A15.1).
6. **Verify** A19.12 against Fire's `Replace`: a PRC with no lesser unit must never be Disrupted. Fix it if it is, behind a fact if an older attack could resolve differently, and say which.
7. **What 3a left to this increment (R70.17).** A12.141 names a LLTC or LLMC, a PF or MOL check, and a leadership modifier applied to anything but a PAATC (a MC, a Rally) among the actions that cost a "?". 3a could not reach them: they sit inside the Fire calculator. Where a concealed unit takes a LLTC/LLMC, makes a PF or MOL check, or its leader's modifier is applied to a MC, in the LOS and within 16 hexes of a Good Order enemy ground unit (3a's `ScenarioA1Sight.ActionLosesConcealment` and the planner's `NearestSeer`), it loses its "?". Decide it in the member's facts (a per-target "seen" fact from Play), not inside Fire's IFT path.
8. **The page and the records.** Each new record reads as a plain line in `PlayRecords.cs` and `ReplaySteps.cs` ("[F8]: 4-6-7 squad G1 takes a LLMC against 7 after 8-1 leader G1's loss: DR 6, passed (A10.2)"). The record names only what the reading side may know: a LLMC of a concealed unit out of the other side's sight is not named to it.

## Gating
- New records appended after an older record are new: an older game holds none and replays as it did.
- The Bail Out's `drm`/`elr`, the D5.311 broken-crew fact, `unarmed`, and the PRC leadership facts change results recordable before the pass, so each applies only where present.
- Say how you checked that an older recorded Interdiction, Bail Out, and fire at a loaded halftrack replay unchanged (the p65 games in `src/ASL/boards/units/live/.../p65-*.game.json` are recorded; replay them through the projector in a test, do not edit them).

## Tests (by method)
- **Rules:** the member: an NMC with Heat of Battle on an Original 2; an Unarmed unit takes no Heat of Battle and no Replacement; a LLMC with the reversed DRM and none for Riders or another level; a LLTC when a leader breaks; a Passenger leader aiding only his vehicle's Passengers; a SMC's Casualty Reduction with its wound dr; the Bail Out with a Rider leader's DRM and ELR Replacement; D5.311's broken crew eliminated.
- **Play:** an Interdiction NMC on an Original 2 drawing Heat of Battle; a leader lost to Mopping Up (or Failure to Rout) causing a LLMC; a SMC Rider's Bail Out that would have thrown; an older recorded Bail Out replaying as it did.
- **Blast radius:** frame A's section 9 lists the methods that assert today's paths; run those your change reaches, by method.

## Studio
`games_3b.py`, on board 3: a German 4-6-7 squad broken with an 8-1 leader in [P10] (open ground), a Russian 4-4-7 squad in the woods of [P8] that sees it, in the German RtPh, so the squad must rout and is interdicted (give the dice: an Original 2 for Heat of Battle); and a second game for a LLMC after a leader's loss outside fire (Failure to Rout or Mopping Up, whichever stages more simply). Give the dice and the JSON names to read back.
