# Increment 4e: every A14.1 trigger (71.1, part V)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for 4a to 4d, and `frame_triggers.md` (agent A's map, your main one). Read the diffs of the increments already committed.

## Pages
A14.1 (p. 82): "a TH, MC, TC, non-OBA IFT, or Entrenching, Original DR [EXC: those made for Prisoners] equal to the enemy SAN", in the PFPh, MPh, DFPh, AFPh; "The Sniper attack occurs after resolving all effects of the DR that activated it, but not before resolving (14.3) all (if any) Sniper attacks made prior to it." A14.3's last paragraph.

## Build (ruling R71.13; R55.30 superseded)
1. **One reader in Play** finds every qualifying Original DR of a planned step, from the records that carry it with its roll id: `FireResolved.Rolls` (with `collateralCheck` and `taskCheck`, made by the target vehicle's side), `OrdnanceFired.Rolls` (To Hit, the HE hit's IFT and checks, crew checks, Canister), `MoraleCheckResolved.Rolls`, `RiderBailedOut.Roll`, `PaatcTaken.Roll`, `TaskCheck.Roll` (the OVR NTC), `InfantryOverrunNtc.Roll`. Rules names each roll's maker by kind (`ScenarioA1Sniper.RollMaker` extended) and keeps the prisoner exception.
2. **It runs once per planned step** through a central wrapper in the planner (GamePlanner.cs near :410, built as `WithImmediateVictory` is), in the four phases, in DR order, after the step's own effects. Fire's existing calls stay; the wrapper does not run the reader twice for one commit. Each step kind agent A lists gains its triggers without a hook of its own: Residual FP on a move and on a vehicle's entry, a Fire Lane, the vehicle OVR (the plain path and the resumed one agree), ordnance (its Build, its resume, Gun Duels), the morale checks in the four phases (Searching, Mopping Up, a PF check's LLMC), the Bail Out, PAATC in the MPh, both OVR NTCs.
3. **Chaining:** a Sniper attack's own LLMC DRs are triggers too: the reader runs again over the events each attack adds, each new attack resolved after the ones before it; it ends because each round needs a new DR.
4. **Ordnance:** R55.30 recorded that ordnance calls no Sniper; it is superseded (A14.1 names the TH DR, and R15.5 named it). Grep the tests for R55.30 and for any assertion that ordnance or Canister calls no Sniper; change each as intended and name it.
5. **A pending choice in the step** (4b, 4d) still stops the reader, as today, and the resume runs the triggers still to resolve.
6. **Performance:** a step with no active enemy Sniper counter on the map returns at once (read through the memo); the reader walks the step's events once.

## Not in 4e
Entrenching (pass 115). A DR that can yield no result but a SAN: no new guard (R71.13). The early-DR order inside one step: as today (backlog).

## Tests by method
Rules: `RollMaker` for each new kind, the prisoner exception. Play: one test per new trigger family (Residual FP on a move, a Fire Lane, a vehicle OVR, an ordnance To Hit, an ordnance HE hit's MC, a Bail Out NMC, a PAATC in the MPh, an OVR NTC, a Searching LLMC in the MPh), each with a DR equal to the enemy SAN calling the attack and a DR not equal calling none; a PAATC in the APh calls none; a Sniper's LLMC chaining a second attack; two triggers in DR order. Grep the tests for `AddSniperAttacks`, `Sniper`, and `R55.30`.

## Studio game
`games_4e.py`: board 3, a German SAN of 3 and the German Sniper; a Russian vehicle's MA To Hit DR of 3 (ordnance), and a Russian squad's Residual FP attack DR of 3 entering a hex of Residual FP; name the dice.
