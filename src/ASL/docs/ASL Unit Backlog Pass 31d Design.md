# ASL Unit Backlog Pass 31d Design

**Status:** Designed 2026-10-04, read-only, and answered the same day: all 12 questions as recommended (section 12). The build is on branch `feature/asl-backlog-pass-31d`. Pass 31d is my name for the short pass the user chose on 2026-10-04 after pass 31c: its leftovers (backlog section 50) before passes 32 and 34. The plan has no row for it yet.

**Date:** 2026-10-04

**Related documents:** the [pass 31c design](<ASL Unit Backlog Pass 31c Design.md>), sections 13 and 14, and its [review](<Scenario A1 Backlog Pass 31c Review 2026-10-04.md>) (the third play test: the refusals table, "What a side may not know", and the 15 findings); the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 50, every row; the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), rulings R13.5, R23.1 to R23.6, R31.6, R31.7, R31b.1, R31b.2, and R31c.1 to R31c.7; the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>), whose standing rules and harness lessons apply unchanged.

Unlike pass 31c, this is not a Studio pass only. Three of its tasks change code below the page: how a game's log is read again at each request (the store, the projector, the planner), the check of what a broken unit leaves behind before it routs, and what the Fire package decides for concealed firers. Section 6 says how a recorded game still replays as it did.

**The rulebook comes first.** Read in the PDF's text for this design, and read again at each task before its change:

| Rule | PDF page | What it gives this pass |
|---|---|---|
| A4.42, A4.43, A4.431 | 50 | IPC of three PP for a MMC and one for a SMC; "A broken unit may not portage anything in excess of its IPC (see 10.4)"; units "sometimes" drop SW "before they can rout (10.4)"; a SW left by a routing unit is unpossessed in its Location |
| A4.51, A4.52 | 51 | A CX counter "is removed if the unit breaks"; "CX Infantry have an IPC one < normal" |
| A6.11 | 53 | "During play, neither player may make potential LOS checks to determine if a LOS exists for an attack until after that attack has been declared"; with a blocked LOS the units "are still considered to have fired for all purposes" |
| A10.4 | 66 | A broken unit "prior to rout must abandon any items in excess of its IPC in its current Location" and must rout with the SW within its IPC; with two or more SW over its IPC "it must rout with a combination of SW of its choice exactly equal to its IPC or, failing that, equal to the highest number of PP it can portage which is also < its IPC. Once it starts its rout, a unit may neither Recover nor Abandon SW" |
| A10.5, A10.51, A10.533 | 66 | Where a rout may not go or end; concealed units "must be ignored by the routing player in determining his legal rout route" |
| A11.1, A11.12 | 72 | CC is between opposing units in one Location in the CCPh; each Location's CC "must be completely resolved before resolving CC in another Location" |
| A11.15 | 72 | Infantry of both sides left in a Location "after all initial CC attacks have been resolved at the end of a CCPh" are locked in Melee; non-Melee units firing at a Melee Location attack "all friendly Melee and enemy units in the Location" |
| A11.19 | 73 | "Dummy units are automatically removed prior to attack designation in CC because they cannot reveal a Strength Factor"; a hidden unit is placed under "?" at the start of the CCPh |
| A12.11 | 76 | A Dummy stack "is removed if it moves without Assault Movement (or into Open Ground) in the LOS of a Good Order enemy as per 12.14" |
| A12.14 | 77 | A concealed unit loses "?" when it fires or directs fire "in LOS of a Good Order enemy ground unit within 16 hexes (such potential LOS checks are free ...)"; "If the only Good Order enemy ground unit in LOS is itself concealed ..., that enemy unit must completely forfeit its "?" momentarily (to prove that it is not a Dummy) if it opts to force the friendly unit to lose his" |
| A12.15 | 78 | A Dummy stack that cannot show a real unit on an attempted entry is removed |
| A20.54 | 87 | "Fire into a Location containing prisoners or unarmed units from outside the Location affects both the Guard and the prisoners/unarmed units as if they were combatants in a Melee"; a prisoner that fails a MC suffers Casualty Reduction; "Prisoners/Unarmed units eliminated by fire from their own side still count double for Victory Conditions" |
| A22.31 | 89 | "A FT may not combine FP with any other unit/weapon--including the unit firing it or even another FT" |

The PDF's text layer mixes an example's hex labels into A10.4; the quotation above is that text with the labels taken out. It is read again from a rendered page at task 31d.2.

## 1. Outcome

A phase's end is checked and confirmed in under a second at any point of The Tractor Works, and so is every other proposal, because a request no longer replays the whole game some twenty times. A laden unit's rout says what it leaves and what it keeps, and does the leaving. The rout panel gives a way to each place a rout may end. Fire by concealed units at a "?" stack is decided, not refused, so a refusal no longer tells a Dummy stack from a real one. A Dummy stack's removal is in the records. A refusal tells a side what is wrong with its own group. A FT and a fired MG are not offered as if they could join a group.

## 2. What the measuring and the reading found

### 2.1 Where a phase's end spends its time

Measured in my Studio on port 6670 with the build of pass 31c's end (`bin/p31b`, 2026-10-04 18:11), on copies of `p31c-tw` cut at a revision (the game's events up to that revision, under a new game id), with nothing else running. The stopwatch is the page's: from the click on "Propose: end the Movement Phase" to Confirm on screen, and from the click on Confirm to the review gone.

| Copy cut at | Revision | Check | Confirm |
|---|---|---|---|
| Turn 1, Russian MPh | 250 | 0.57 s (two runs; 1.3 s the first time after the game is opened) | 1.1 s |
| Turn 5, Russian MPh | 607 | 1.9 to 2.4 s (ten runs; 2.7 s the first time) | 3.75 s (two runs) |
| Turn 7, Russian MPh | 693 | 2.2 to 2.3 s (three runs) | 3.77 s |

These are about half of what the play test saw at Turn 5 (4.4 and 8.0 seconds). The play test ran with the script's own waits and the user's Studio beside it; the proportions are the same.

**The profile.** A sampling profiler (`dotnet-trace`, thread time, installed in the session's scratchpad only) on the Turn 5 copy:

| | Four checks | One confirm |
|---|---|---|
| Time in the page's `Propose` or `Confirm` | 9.4 s | 3.68 s |
| Of it, in `GamePlanner.Replay` (`GameProjector.Project`) | 9.04 s, 96% | 3.38 s, 92% |
| Whole-game replays | 60, or 15 a check | 26 |
| The page's own work (`Refresh`, the render, the map layers), its own replays of the history counted in the row above | under 0.05 s a check | about 0.3 s |

One replay of the 607 events takes 130 to 150 ms. Inside a replay: `FireRecordVerifier.Verify`, which resolves every recorded attack again, 38%; `RallyRecordVerifier.Verify` 14%; `CheckInvariants` 12%. Across all of them, `GameState.Find`, `At`, and `Location`, which scan every object, take about 30%.

**Who asks for the replays.**

- `GamePlanner.PlanAsync` replays the stored log at each of its guards (`GamePlanner.cs:212`, `:218`, `:226`, `:232`, `:285`, `:292`), once more in the action's own planner (`PlanAdvance`, `:524`), once in each of `WithAcquisitions`, `WithAdjacentDm`, and `WithArmedSmc`, and once with the new events (`:343`). Measured: 7 replays for one plan of a phase's end.
- A check plans twice: `GamePlay.RunAsync` plans for the page, and `GameConstraintEvaluator.EvaluateAsync` plans again inside the gate. A confirm plans three times: those two, and `GameActionExecutor.ExecuteAsync`. The store then replays once in `FileGameStore.Commit`, and the executor once to read the effect back.
- The page reads the history again after each (`LivePlay.History`, `LivePlay.cs:129`): once after a check, three times around a confirm.
- `FileGameStore.Read` parses the file at every call, so no two calls see the same event objects.

Nothing is kept between any two of these. The cost of one replay grows with the game's length and with its count of counters and attacks, and the count of replays is fixed, so every proposal slows as the game grows. A phase's end is only where it was noticed: a fire proposal goes through the same guards and adds replays of its own (`GamePlanner.Fire.cs:508`, `:693`, `:706`, `:716`, `:735`).

The page is not the cause. Pass 31c.7 kept one board handle and read the Rout panel once; that met its aim on The Guards Counterattack because the game was short.

### 2.2 What the reading of the code changes in the list

| Item as the prompt has it | What I found |
|---|---|
| The FT of a broken Guard could be dropped only after its DC, "for the referee" | Not a reading of the rule: a fault in the code. `KeepsBestLoad` (`GamePlanner.SupportWeapons.cs:399`) pairs the PP that `Portage` lists in the order the SW were created with the SW that `Held` lists in the order of their ids. For 8-3-8 squad G2 (`g-engineer-2`) with `g-ft-2` (5 PP) and `g-dc-2` (2 PP), the FT was created first and the DC sorts first, so the check took the DC for 5 PP and the FT for 2. By A10.4 the squad keeps the DC (2 PP, the most it can carry within 3) and leaves the FT. The game made it leave the DC, and then the FT too. `ABrokenUnitDropsOnlyWhatItCannotCarry` (`BacklogPass13Tests.cs:576`) uses ids whose two orders agree |
| Fire at a Location holding the firer's own captured units, "with no word before Confirm" | The word is there. `FireWarnings` (`GamePlanner.Consequences.cs:62`) gives `play.fire-own-units` for every target of the firing side, and the review's consequences block and the button "Confirm and fire on your own units" show it. The audit log of the play test has it in both proposals (21:46 and 21:58 UTC): "play.fire-own-units: r-squad-9 of the firing side is in bd01:X4:0 and is attacked too (A11.15, A20.54)", and the same for three units in bd01:Y5:0. My script confirmed without reading it. What is missing is smaller: the line does not say that the units are prisoners or what A20.54 does to them, and the Target list gives no sign |
| Fire by concealed units at a Location holding only Dummies is refused | The Fire package decides a concealed firer's loss of "?" only from the target Location: `Precheck` (`ScenarioA1FireCalculator.cs:104`) and `Resolution.FirerConcealment` (`:3131`) ask that a target be unbroken and not a Dummy, and otherwise give `concealment-unreviewed`. The planner already has the read the rule asks for, `EnemyGoodOrderInLosWithin16` (`GamePlanner.Map.cs:167`), and uses it for movement. A proposal that is refused marks no firer, so a side can try a "?" stack with a concealed unit at no cost: refused means Dummies or broken units |
| Dummies removed in a Location entered for CC, with no record | No event is recorded at all. `GameProjector.StartCloseCombat` (`GameProjector.cs:573`) removes them while it applies the phase change to the CCPh |
| A refusal's reasons withheld | One decision, at `GamePlanner.Fire.cs:267`: when the target Location holds a unit the firing side cannot see, or nothing it can see, the whole pre-check refusal becomes `FireProposal.Undisclosed`. Refusals made before the pre-check (`play.fire-firers`, `play.fire-barred`, and the rest) are told in full already |
| The CCPh ended with a Location entered for CC and no round fought | The phase's check (`GameProjector.cs:389`) looks only at Locations that have an entry in `CloseCombats`, and an entry is made by the Ambush drs or a round. A refused Ambush records nothing, and a round is refused with `play.cc-ambush-first` until the Ambush is rolled, so the Location can have neither. The panel's due list (`GamePlanner.CloseCombatDue`, `GamePlanner.CloseCombat.cs:813`) goes on saying the Ambush is due |
| "Since you last looked" lists the view's own action | `sinceLooked` is set when the view arrives (`Play.razor:3981`) and never again; `PlayRecords.Since` filters on a revision alone, so what the view then does is listed too |
| Tooltips without a hex during setup | `MapUnits` (`Play.razor:1752`) builds the overlay again from the documents while a draft has rows, which drops the lead `GameMaps.Named` wrote. The hand-over screen's map (`GameMaps.Public`) never calls `Named` |

## 3. What the code does now

| Area | Now | Where |
|---|---|---|
| A game read at a request | Parsed from the file and replayed from its first event at every use; nothing kept | `FileGameStore.Read`, `GamePlanner.Replay` (`GamePlanner.cs:171`), `GameProjector.Project` (`GameProjector.cs:16`), `LivePlay.History` |
| A laden unit's rout | Refused with `play.rout-laden`, naming the unit only | `PlanRout`, `GamePlanner.Rout.cs:392`; `Laden`, `GamePlanner.CloseCombat.cs:661` |
| A broken unit's drop | Allowed in the RtPh when the unit is laden and some best load leaves that SW out; the best load is worked out with the PP and the SW in two orders, and without the CX point `Laden` takes off | `PlanDrop`, `GamePlanner.SupportWeapons.cs:358`; `KeepsBestLoad`, `:399`; `Held`, `:100`; `Portage`, `GamePlanner.Movement.cs:63` |
| The Rout panel | A unit select, a route field, Low Crawl, Propose. No drop, no PP | `RoutActionPanel.razor`; `Play.razor:445` |
| The drop on the page | In the Support weapons section, for any SW of the view, with no PP and no tie to the unit chosen for the rout | `SupportWeaponActionPanel.razor:33`; `SupportWeapons`, `Play.razor:5525` |
| The rout status | "Its route must end in [AA4] or [Z6]" | `RoutAdviceText`, `Play.razor:5503`; `GamePlanner.RoutAdvice`, `GamePlanner.Rout.cs:674` |
| The rout search | A least-cost search that returns the cost to each Location and keeps no path | `RoutReach`, `GamePlanner.Rout.cs:183`; `RoutTargets`, `:277` |
| A concealed firer's "?" | Decided only when a target in the target Location is unbroken and not a Dummy and every firer is within 16 hexes; otherwise `concealment-unreviewed` | `ScenarioA1FireCalculator.Precheck`, `:104`; `Resolution.FirerConcealment`, `:3131` |
| A fire refusal's reasons for the firing side | All, or the one sentence `Undisclosed`; the range lines are the one exception (ruling R31c.7) | `FireProposal.ReasonsFor`, `GamePlanner.Fire.cs:40`; `PlanFire`, `:267`; `RangeNamed`, `:1320` |
| A Dummy that moves in enemy sight | A public `instance-eliminated` in the move's events; the proposal's reason is the move's summary alone; no record line | `Unmask`, `GamePlanner.Movement.cs:566`; `PlayRecords.ReadLines`, `PlayRecords.cs:186` |
| A Dummy in a Location entered for CC | Removed by the projector at the phase change, with no event and no line | `GameProjector.StartCloseCombat`, `GameProjector.cs:573` |
| Fire at the firer's own units | A consequence before Confirm, by id, with no word that they are prisoners | `FireWarnings`, `GamePlanner.Consequences.cs:62` |
| A proposed attack with no LOS | Accepted, with the consequence "no firer has a LOS ... the attack has no effect and its firers are still marked as having fired (A6.11)" and the button "Confirm the shot with no LOS"; Cancel drops it and marks nothing. The LOS tab reads any LOS at any time. A confirmed attack with a blocked LOS reads in the records as an attack with no effect | `FireWarnings`; `CancelProposal`, `Play.razor:6481`; `LosPanel` |
| The CCPh's end | Held only by a Location with an entry in `CloseCombats` that is not closed | `GameProjector.ChangePhase`, `GameProjector.cs:389`; `PlanAdvance`, `GamePlanner.cs:678` |
| The firers and weapons offered | Every MG, FT, and ATR a ticked firer possesses, whatever it has done; the "only its MG fires" box is offered for a FT too | `FireWeapons`, `Play.razor:4226`; `FireAloneCandidates`, `:4248`; `SmallArmsFirePanel.razor:47` |
| Setup by hand | Rows as "r-line-squad-1: defender-squad (russian-0)"; "[P7] on board 01" on one board; "[P3] bd01:P3:0" in the non-OB list | `PlacementItems`, `Play.razor:1185`; `WhereText`, `:1915`; `PlaceOf`, `:6732`; `Play.razor:109` |
| "Since you last looked" | Every line after the revision the view last left at | `PlayRecords.Since`, `PlayRecords.cs:184`; `Play.razor:3981`, `:6470` |
| The page tests' board | 3 by 2 hexes painted in code, of which three hexes take units | `SyntheticBoard`; `BuildingBoards`, `PlayPageTests.cs:17` |

## 4. Decisions

### D1. A game is read once, and a request adds only what is new

The aim: a phase's end checked in under one second and confirmed in under one second at revisions 250, 607, and 693 of `p31c-tw`, measured as in section 2.1. The same holds for any other proposal that has no search of its own.

- **The store keeps what it parsed.** `FileGameStore.Read` reads the file's bytes as now and returns the record it parsed last when the bytes are the same (a SHA-256 of the bytes; the bytes are read anyway). So the same event objects come back while the file is unchanged. `Commit` reads through the same path, and after a write it keeps the record it wrote under the hash of the text it wrote. A file changed by anything else has other bytes and is parsed anew.
- **The projector can go on from where it stopped.** `GameProjector.Project` keeps, with the history it returns, what its `Replay` helper carries between events (its six fields: the event ids seen, the rolls, the fires, the catalog, the layout, and the path), with the diagnostics so far. The verifiers are shared and keep nothing of a projection. The location chains are made from the boards the events name (`GamePlanner.Chains`), so a continuation is made only when the tail names no board, and otherwise the whole list is replayed. A new `GameProjector.Continue` takes that and a tail of events and applies the tail alone, on a copy of what is carried, so the kept projection is never changed by a continuation.
- **The planner keeps the projection of the stored log.** `GamePlanner.Replay(events)` looks for a kept projection whose events are the first part of `events`, object for object, and continues from it; a list it has projected whole is returned as it is. It keeps the projection of the longest committed log of each game it has seen, a few games at most, behind a lock. A candidate (the stored log with a plan's new events) is continued and not kept.
- **What this does to the counts:** a check goes from 15 whole replays to none, and a confirm from 26 to none, each with one or two continuations of a few events. A game's first read after the Studio starts, or after its file changed outside the Studio, is one whole replay, as now.
- **Every event is still verified.** A whole replay verifies every recorded attack, Rally, Close Combat, and shot, as now. A continuation verifies the events of its tail by the same code. Nothing is trusted that was not replayed in this process from these bytes.
- **Not in this pass:** a faster verifier, an index for `GameState.Find`, and fewer plans a request (the gate's second plan and the executor's third are the governed path's design). With nothing replayed twice they no longer show; they are measured again after the build and go to the backlog if they do.
- **"Working"** (pass 31c) stays for a request that runs past half a second.

Both kept things are below the page: in `LimboDancer.Domains.Asl.Units` and `LimboDancer.Domains.Asl.Play`. That is question 2.

### D2. A broken unit leaves what A10.4 says, and its rout does the leaving

- **One reading of the load, in one place.** A new `RoutLoad(state, unit)` in the planner gives the unit's IPC, each SW it possesses with its PP, the PP it carries, and its best loads: the sets of its SW with the most PP that is not over its IPC (A10.4: "exactly equal to its IPC or, failing that, ... the highest number of PP it can portage"). The SW and their PP are read together, so no order can part them. `Laden`, `KeepsBestLoad`, and the rout all read it. A broken unit is not CX (A4.51), so the CX point does not enter; where `Laden` serves an unbroken unit it keeps its CX point (A4.52).
- **The drop is right.** A broken unit in the RtPh, before it routs, may leave a SW that some best load leaves out. For 8-3-8 squad G2 that is the FT and not the DC. This corrects the fault of section 2.2; ruling R13.5's words ("keeps SW up to its IPC and drops only the rest first") stand.
- **The rout leaves the rest behind.** A rout proposed for a laden unit carries which SW it keeps (`keep`, a list of SW ids). The planner checks that it is a best load, writes the drop of every other SW as the drop action writes it (the same `equipment-transferred` events, in the unit's Location), and then the rout's steps, in one attempt. With one best load the planner takes it when `keep` is not given. With several and no `keep`, it refuses and names them.
- **The Rout panel says it.** For the unit chosen, when it is laden: "8-3-8 squad G2 carries 7 PP and may rout with 3 (A10.4). It leaves its FT (5 PP) in [Y5] and keeps its DC (2 PP)." With several best loads a select, "It keeps", lists them. The line is closed behind the unit's row when the unit is not laden, so nothing stands open that the moment does not need.
- **Before Confirm** the review says the same as a consequence: "Its FT is left in [Y5], unpossessed (A10.4, A4.431)."
- **The refusal,** for a caller that names a `keep` that is not a best load: "play.rout-laden: 8-3-8 squad G2 carries 7 PP and routs with at most 3; it keeps its DC (2 PP), or ..., and leaves the rest (A10.4)".
- **The Support weapons section** keeps its drop. Its SW rows say their PP.

### D3. The rout status gives a way to each place a rout may end

- `RoutReach` keeps, for each Location it reaches, the Location it came from. `RoutAdvice` returns for each place a rout may end one least-cost route to it.
- The status reads: "Its route must end in [AA4] (by [Z5]) or [Z6] (by [Z5], [Z6])." Each is a button, "Use this route", that fills the route field; the field stays typed or clicked hex by hex as now.
- The route offered is one legal route, not the only one. The planner checks the route proposed as it does now.
- The search reads the units the search reads today. A10.533 has the routing player ignore concealed units; at the task the search is read for any unit the routing side's view does not hold, and a route is offered only from what that view holds.

### D4. Fire by concealed units is decided where no Good Order enemy sees them

A12.14: a concealed unit that fires loses its "?" in the LOS of a Good Order enemy ground unit within 16 hexes. The target Location is only one place such a unit may be.

- The planner reads, for each concealed firer and director, whether a Good Order enemy ground unit within 16 hexes has a LOS to its Location (`EnemyGoodOrderInLosWithin16`, the read movement uses), and gives the Fire package that fact with the attack (a new optional fact on the firer, `seenByGoodOrderEnemy`).
- With the fact, `Precheck` and `FirerConcealment` decide: a firer that is seen loses its "?"; one that is not keeps it. The attack on the Location is resolved as any other: Dummies there are on the concealed column and are removed by a result of PTC or better, as now (A12.14).
- Without the fact (every attack recorded before this pass) the package decides as it does now.
- **What the firing side then learns** is what the table tells: after the attack is made, whether its "?" was lost. It learns it by firing, with its units marked, and not from a refusal before Confirm.
- **The viewing side's option.** A12.14 lets a concealed viewer keep its own "?" by not forcing the loss. The game takes the loss as forced whenever a Good Order enemy unit sees the firer, as movement does today (ruling R10.10). The option as an owner's choice goes to the backlog.

This task changes what the Fire package resolves, for attacks that are refused today. That is question 5.

### D5. A Dummy stack's removal is said

Both players see a Dummy stack leave the map: the counters are taken off the board (A12.11, A11.19). So the record is for both sides.

- **Moved in enemy sight (A12.11).** `PlayRecords` words the move's own `instance-eliminated` for a Dummy: "A Russian Dummy stack is removed in [T4]: it moved without Assault Movement in the LOS of a Good Order enemy unit (A12.11)." Both views read it. Replay's step for the move takes the same words, and `ReplaySteps` stops calling a Dummy "a unit".
- **In a Location entered for CC (A11.19).** There is no event, and none is added. `PlayRecords` reads the states before and after the phase change to the CCPh and says, for each Location: "The Russian Dummies in [X4] are removed before Close Combat (A11.19)." A hidden unit placed there under "?" (A11.19, A12.32) has its line as ruling R31b.2 has it.
- **Before Confirm, only the owner's own facts.** When a stack of Dummies alone is moved without Assault Movement, or into Open Ground, the mover's review says: "This stack holds no real unit. It is removed if a Good Order enemy unit within 16 hexes has a LOS to it here (A12.11)." It says "if" whatever the game knows: whether an enemy "?" that sees the hex is real is not the mover's to learn before the move. When Dummies advance into a Location that holds an enemy unit, the advance's review says they will be removed at the start of the CCPh (A11.19). Nothing is said before Confirm about the other side's Dummies.
- **By fire.** The fire record's "eliminated" for a Dummy becomes "the Dummies are removed" from the effect's own `dummy-removed` tag.
- The hand-over screen's "what happened" goes on leaving Dummies out of the count of units lost (ruling R31.7); it gains the same public line.

### D6. Fire at a Location that holds the firer's own captured units

The warning exists (section 2.2). This pass makes it say what A20.54 says.

- The consequence parts prisoners from units in a Melee: "Your captured 4-4-7 squad R2 and 4-4-7 squad R5 in [Y5] are attacked with their Guard, as if in a Melee: one that fails a MC is Reduced, and one eliminated by your own fire counts double (A20.54)." Units in a Melee keep the line they have (A11.15).
- The Target list marks such a Location: "[Y5]: 1 hex, PBF; holds your captured units".
- At the task the consequences block is seen in my Studio first, before anything is changed: a cut of `p31c-tw` at revision 600 (the Russian Prep Fire Phase of Turn 5), in the Russian view, with the fire at [X4] the play test made. This design read the planner's line in the audit log and the page's code, and did not see the block on screen.

### D7. An LOS before the attack is declared

A6.11 bars it. The page gives it twice: the LOS tab reads any LOS at any time in a game still played, and a fire proposal reads the LOS and can be cancelled. The two stand or fall together; closing one and leaving the other closes nothing.

- **(a) Free LOS, stated.** A ruling says the game is played with free LOS checks, as a departure from A6.11 that the user chose: the LOS tab stays, and a proposal may be cancelled.
- **(b) A6.11 kept for a side's view of a game still played.** The LOS tab is not offered to a side (the adjudicator, an ended game, and the board viewer keep it). A fire proposal shows a side nothing that comes of the LOS before Confirm: no LOS row, no Hindrance in the DRM ("read when the attack is declared"), no word of a blocked LOS. Confirm declares and resolves. Pass 31's warning of a shot with no LOS (play test P-12) goes for a side.

In both, a confirmed attack with a blocked LOS says so in its record, from the resolution's own `LosBlocked`: "... the LOS is blocked: no effect, and the firers have fired (A6.11)". Today it reads as an attack with no effect.

I recommend (a) for this pass and a backlog row for (b). That is question 7.

### D8. A refusal tells a side what is wrong with its own group

Where the target Location holds something the firing side cannot see, a pre-check refusal is told by this rule:

- If any of its reasons rests on the firing side's own group and the map alone, those reasons are told, and nothing else.
- If none does, the one sentence `Undisclosed` is told, as now.

So what a side reads depends only on its own group. It never reads "and something more that you may not know", which would itself tell.

The reasons that rest on the firer's own group and the map alone, each read at the task against what the package tests: `flamethrower-outside` (A22.31), `firer-outside` (a firer that may not fire), `firer-already-fired`, `weapon-outside` (a weapon malfunctioned or already fired), `firers-of-two-sides`, `director-outside`, `phase-outside`, and the range lines of ruling R31c.7. Left out, since each reads a unit that may be unseen: `subsequent-first-fire-outside` (the closest Known enemy), `mol-outside` (a vehicle in the target Location), `unit-listed-twice`, and every `target-`, `crew-target-`, `elr-`, `fact-missing`, `definition-`, `leaders-`, and `concealment-` reason.

The list is one table in the planner, beside `RangeNamed`.

### D9. The Close Combat Phase's end says what was not fought

- **Before Confirm,** ending the CCPh lists each Location that holds unbroken units of both sides, prisoners apart, in which no round was recorded this phase: "No Close Combat was fought in [X4] this phase. The units there are held in Melee (A11.15)." The button reads "Confirm with a Close Combat not fought". It is a consequence, not a refusal: where the Close Combat package cannot decide the Location, a refusal would leave the phase with no way to end, as ruling R31c.5 found.
- At the task the projector is read for what it does with such units at the phase's end (`KeepMelee` only frees units; where the Melee mark is set is read first), and the words follow what the game does.
- **The due list** reads the projector's own test: a Location with nothing left after an Ambush (ruling R31c.5) has no line, and a Location whose Ambush the package refuses reads "the Close Combat package does not decide this Location" in place of "the Ambush drs (A11.4)".
- **The Ambush refusal** is worded through `RefusalReasons`, as a round's refusal is: "a prisoner's Guard is not an enemy unit in its Location (A20.5)" in place of "Prisoner guard outside".
- Why a prisoner stood in a Location without its Guard is not looked into here; it is a backlog row.

### D10. A FT and a fired MG are not offered as if they could join

- A weapon that has fired this phase and kept no rate of fire, or has malfunctioned, is listed and cannot be ticked: "MMG of 4-6-7 squad G10: has fired". The test is the Fire package's own (`LiveFire.Fired` with the First Fire mark), made public.
- A FT reads "FT of 8-3-8 squad G2: fires alone (A22.31)". Ticking it with any other unit or weapon ticked disables Propose with that note; the "only its MG fires" box is not offered for a FT.

### D11. Words

Each is made where its text is made.

| Text now | Becomes | Where |
|---|---|---|
| "9 unit(s) of Russian placed under "?"" | "The Russian side places 9 units under "?"", "1 unit" for one | `GamePlanner.Disclosure.cs:185` |
| "Heat of Battle DR 4, 4 = 8 = Final DR 8" | "Heat of Battle DR 4, 4 = 8" when no DRM applies | `FireText.HeatOfBattleText`, `FireText.cs:105` |
| "x 1 (Assault Fire, A7.36)" | "+ 1 (Assault Fire, A7.36)" | `FireArithmeticPreview.razor:15`, `FireArithmeticBreakdown.razor:8`, by the modifier's name |
| "...: PTC." | "...: PTC; 4-4-7 squad R4 passed", from the effects' checks, for units the view may name | `PlayRecords.FireLine`, `PlayRecords.cs:241` |
| Two sentences for a rout of two hexes, each with the unit's name | One: "4-4-7 squad R4 routs from [Y5] by [Z5] to [AA5] for 3 MF" | `PlayRecords.cs:516` |
| "building N4" in the Mopping Up record and proposal | "building [N4]" | `PlayRecords.cs:225`, `ReplaySteps.cs:356`, through `DisplayText.Hexes` |
| "stone-building", "open-ground", "in the PFPh" in a proposal's summary | "stone building", "Open Ground", "in the Prep Fire Phase" | One step in the page's `Said` for terrain keys and phase short forms; the planner's sentences are not changed |
| A fact's name as a reason | Words for the nine facts of `MoverFacts` in `DisplayText.Reason` | `DisplayText.cs:123` |
| "enter" for one unit | "enters" | `GamePlanner.Movement.cs:502` |

Assault Movement that "may not spend every MF" (the refusal "2 MF left, and the entry costs 2") gets the rule in its sentence: "Assault Movement may not use all of a unit's MF (A4.61)".

### D12. Tooltips, "Since you last looked", and two lines of setup

- **Tooltips.** The overlay built from documents during setup takes a lead as `Named` gives one: "[P7]: 4-4-7 squad" for the view's own counter, from `UnitNames.Counter`, and "[P7]: " before the other side's "?". The hand-over screen's map leads each counter with its hex and no name, since both sides read it. A test reads the title of a "?" in ordinary play, where the code should already give the hex.
- **"Since you last looked"** lists what happened between the revision the view last left at and the revision it arrived at. `PlayRecords.Since` takes both. What the view does after it arrives is in Records and "Latest".
- **Setup,** only these: a Location on a one-board map is written "[P7]" before the game exists (the card's boards give the count), and the non-OB list writes the Location once, in words.

### D13. A page-test board that has room

A board of 10 by 6 hexes painted in code as `SyntheticBoard` is, with woods, two buildings (one of two levels), a wall, and Open Ground between, served by a provider of the same shape as `BuildingBoards`. It is built once for a test class. Page tests that need range or a rout use it; the others keep the 3 by 2 board. Its cost for a class is measured when it is first built, and it is cut to what the tests need if it adds more than a few seconds.

## 5. Disclosure

Rulings R23.1 to R23.6, R31.6, R31.7, R31b.1, R31b.2, and R31c.1 to R31c.7 stand. What this pass adds:

- **The kept projection (D1)** is the full game, as the history the planner holds today is. A view is made from it by `GameLibrary.ViewOf` as now. Nothing a side reads comes from it by another way.
- **The rout's leaving (D2)** names the routing side's own unit and SW. The record of a drop is the one a drop has today.
- **A route offered (D3)** is searched from what the routing side's view holds.
- **A concealed firer's "?" (D4)** is lost or kept after the attack is made, as at the table. No refusal tells the two cases apart before Confirm.
- **A Dummy's removal (D5)** is public once it has happened. Before Confirm the owner reads only of its own Dummies, and only "if".
- **The prisoners' line (D6)** names units of the firing side that the view holds as captured.
- **A refusal (D8)** tells a side reasons that rest on its own group and the map, or the one sentence, never both.
- **The CCPh's end (D9)** names Locations where both sides stand in the CCPh. Each side has its units there, and by A11.19 every unit there has shown its Strength Factor or been placed under "?".
- **A weapon's "has fired" (D10)** is the view's own weapon.

For the referee's review: the advance's summary names the units of the other side in the Location entered ("with ...: CC follows"), by id for each counter. The page turns an id the view may not name into "a concealed unit", once for each, which counts the counters under a "?". It is read in each side's view, and if it counts, it is changed to "with a concealed stack".

## 6. A recorded game replays as it did

- **D1** changes when events are projected, not what a projection is. A continuation runs the code a whole replay runs. The test: for `guards-dl-01` and a cut of `p31c-tw`, the history built one continuation at a time is equal, state for state and diagnostic for diagnostic, to the history built whole; a log with an error is not continued; a file changed on disk is read anew.
- **D2** changes the planner's check of a new proposal. The projector applies a recorded drop as it is recorded (`GameProjector.Transfer`, which checks the holder and the position and has no check of the load), so `p31c-tw` replays with the DC left before the FT, as it was played. A rout recorded before this pass has no drops in its attempt and replays as it is.
- **D4** adds a fact to new attacks, read only when present, as pass 31 did with `pinnedMgAreaFire` and `targetsInMelee`. An attack recorded before has none and is verified as it was resolved. The attacks this lets through were refused before, so no recorded game holds one.
- **D5** adds no event. The line for A11.19 is read from two states the projector already makes.
- **D3, D6 to D12** change words, lists, and a search's return, and no event.

**The proof is the played games:** `guards-dl-01` with 613 revisions and 221 steps, `p31c-play` with 134 steps, and `p31c-tw` with 716 revisions and 231 steps, each opened on Play and on Replay in the adjudicator's view after each task that touches the store, the projector, the planner, or the records.

## 7. What must not change

- A recorded game replays as it did (section 6).
- A side's view: rulings R23.1 to R23.6, R31b.1, R31b.2, and R31c.1 to R31c.7. No revision is shown to a side in a game still played. The Game inspector and the board viewer open no game still played in a view chosen at will.
- Who may propose what (ruling R31.6), and the hand-over screen's terms (rulings R31.7 and R31c.3).
- The governed path: a registered action, the gate, the expected revision, the atomic commit, and the effect read back. D1 makes its reads cheap and removes none of them.
- The Fire package's results for every attack it resolves today.
- A change to a record's words is made in `PlayRecords`, never in a page.
- Hexes in brackets, a unit's hex with its name, and tooltips that begin with the hex (ruling R31c.6).

## 8. What stays out, for the backlog

Left out of this pass, plainly:

| Item | Why |
|---|---|
| Setup by hand in a player's words: counters by name in the rows and the holder select, one form in every list, a plan for a card's later group | It is setup's own form brought to names: draft counters have no tag until they are committed, and the three setup components take no `Say`. A task of its own, not a leftover. D12 takes its two one-line faults |
| A6.11 kept for a side (D7's option (b)) | Recommended as a backlog row; it removes a warning pass 31 added and the LOS tab the user reads play with |
| The viewing side's option to keep its own "?" and not force a firer's loss (A12.14) | An owner's choice in the middle of an attack; D4 takes the loss as forced |
| Why a prisoner stood in a Location without its Guard, and the Close Combat package deciding such a Location | D9 words the refusal and the phase's end; the cause is the Prisoners code's |
| A faster record verifier, an index for `GameState.Find`, one plan a request in place of two and three | Not needed once nothing is replayed twice (D1); measured after the build |
| Tests of pass 31c that need a played fixture or a long set-up: a tag through Deployment and Recombination on a played game, the Melee mark by view, a kept rate of fire said | D13's board does not make them cheap. The row stays, without the range and rout tests this pass writes |
| The rows of section 50 this pass does not name: a Gun's range bands, PBF between levels of one hex, NVR; a leader in each Location of a fire group; a fire group checked as one chain; a word before Confirm when one Location of a group has no LOS; the cue of whether a view has anything left to do in a phase; the CC Location chosen again keeping its attacks; counter names for vehicles and Guns; the IFT column's results in the preview; the search's result kept for a unit at a revision; Propose in a foot; the card as a modal dialog; the narrow Studio; the vehicle's and setup's own picking; the inspector's raw events on Replay; the card's own text in brackets; a saved game for R31c.4's check; the controls no play test has reached | Not in the user's list for this pass |

## 9. Tests

Written at the merge gate, by the handover prompt's rule. My Studio on port 6670 is the only test until the code is complete.

- **The kept projection (Units, Play):** a continuation equal to a whole replay, for `guards-dl-01` one event at a time and for a cut of `p31c-tw` at each phase change; a candidate's continuation leaves the kept projection as it was; a log with an error is not continued; the store returns the same record for the same bytes and a new one after a change on disk; two requests at once for one game.
- **The time,** not as a test of the suite: section 2.1's stopwatch run again on the same cuts, reported with the gate.
- **The load (Play):** the best loads for PP in every order of creation and id, the FT and DC case among them; a broken unit's drop accepted for the SW outside a best load and refused for the one in every best load; a rout that leaves the rest, with one best load and with a choice; the refusal's words; a rout of a unit that is not laden unchanged.
- **The route (Play):** each route offered is accepted by `PlanRout` when proposed, for every broken unit of the played games at the revisions where one must rout.
- **Concealed firers (Rules, Play):** seen, the "?" lost; not seen, kept; a target Location of Dummies alone resolved, the Dummies removed at a PTC or better; an attack recorded without the fact verified as before.
- **Records (Map Studio):** the Dummy's removal by a move in each side's view; the A11.19 line at the phase change in each side's view; "the Dummies are removed" by fire; the blocked-LOS record; the words of D11.
- **Refusals (Play, Map Studio):** a FT with a squad at a Location with a "?" told as the FT's reason alone; a target-side reason alone told as the one sentence; both together told as the FT's reason alone.
- **The CCPh's end (Play):** the consequence for a Location with no round; none for a Location resolved or with nothing left; the due list in both cases.
- **The page (Map Studio), on D13's board:** the Rout panel's load line and its select; a route's button filling the field; a refusal for range and a target disabled for range (pass 31c's row); a fired MG listed and not tickable; a FT's note; the prisoners' consequence and the Target list's mark; the tooltips during setup; "Since you last looked" after the view acts.
- **The played games** open as section 6 has them.
- **The gate also runs** `node --test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs` by hand.

## 10. The order of weight

The user's order is: the phase's end, the rout, the referee's items, the page's smaller faults, the tests. I would keep it, with three changes inside it:

1. **The phase's end first, as given,** and wider than it was put: the cause is one thing, and its fix quickens every proposal, not a phase's end alone.
2. **The rout second, as given, with the drop's fault at its head.** It is the one place in this list where the game gave a wrong result: a squad lost a DC that A10.4 has it keep.
3. **Among the referee's items, the concealed firers first** (D4), since a side can use the refusal today to tell Dummies from units at no cost; then the Dummies' removal (D5), the refusal's reasons (D8), and the CCPh's end (D9). The prisoners (D6) and the LOS (D7) come last: the first is already warned of, and the second is a ruling more than a build.
4. **The tests before the page's smaller faults,** for the part that guards this pass's own engine work (D1, D2, D4). The larger page-test board comes with the page tests that need it.
5. **The page's smaller faults last.** Setup by hand is the one I would leave out altogether.

## 11. Tasks and estimate

| Task | What it changes | Decisions | Estimate |
|---|---|---|---|
| 31d.1 A game read once | The store's kept record; `GameProjector.Continue`; the planner's kept projection; the stopwatch of section 2.1 before and after | D1 | 1:30 |
| 31d.2 The rout | `RoutLoad`; the drop's check; the rout that leaves the rest; the Rout panel's load line; the route to each place a rout may end and its button | D2, D3 | 1:30 |
| 31d.3 Concealed firers | The planner's read for each concealed firer; the fact in the Fire package's `Precheck` and `FirerConcealment`; ruling | D4 | 1:15 |
| 31d.4 For the referee | The Dummy's removal in the records and before Confirm; the prisoners' line and the target's mark; the blocked-LOS record and the LOS ruling's words; a refusal's own reasons; the CCPh's end, the due list, and the Ambush refusal's words | D5 to D9 | 1:30 |
| 31d.5 The page | A FT and a fired MG; the words of D11; tooltips; "Since you last looked"; setup's two lines | D10 to D12 | 1:00 |
| 31d.6 The board and the tests | D13's board; section 9's tests | D13 | 1:30 |
| | Overhead: a referee's review and a table player's review, read-only, and their fixes; the Studio check with the timed runs; the documents; the merge gate | | 1:15 |
| | **Pass 31d total** (build 8:15) | | **9:30** |

Passes 31, 31b, and 31c took about a quarter of their estimates, so this is likely high.

**A shorter cut,** if 9:30 is not short: leave out 31d.3 (1:15) and D13's board with the page tests that need it (0:45), for 7:30. I would not: 31d.3 is the one item that closes a way to read what a side may not know.

The order is 31d.1, then 31d.2, then the rest as numbered. 31d.1 is measured and the three played games are opened before anything else is built on it. The steps are those of passes 30 to 31c: one task at a time with my Studio on port 6670 as the only test, a commit for each task once it passes its Studio check, the reviews, the Studio check of the whole pass, the documents (rulings, backlog section 51 and the rows taken out of section 50, the plan's row), the merge gate, and a stop before the merge.

**Rulings this pass would write,** after the user's answers: R31d.1 (what a broken unit leaves before it routs, and the rout that does it; A10.4), R31d.2 (a concealed firer's "?" where no Good Order enemy sees it; A12.14), R31d.3 (what is said of a Dummy stack's removal, and to whom; A12.11, A11.19), R31d.4 (LOS checks in play; A6.11), R31d.5 (which of a refusal's reasons a side reads), R31d.6 (the CCPh's end with a Location not fought; A11.15).

## 12. The user's answers

Answered 2026-10-04: "Build, all as recommended". The order of weight with section 10's three changes; the speed fix below the page; one proposal for a laden unit's rout; the drop corrected by A10.4; concealed firers decided in the Fire package in this pass; a Dummy stack's removal in the records of both sides and only the owner's own before Confirm; free LOS checks stated as a ruling, with a backlog row for A6.11 kept; a refusal's own reasons or the one sentence, never both; a consequence at the CCPh's end; the 10 by 6 board; setup by hand left out but for two lines; the estimate of 9:30.

| # | Question | Recommendation, taken |
|---|---|---|
| 1 | The order of weight: yours, with the three changes of section 10 (the drop's fault at the head of the rout, the concealed firers first among the referee's items, this pass's engine tests before the page's smaller faults)? | Yes. |
| 2 | The phase's end is slow because each request replays the whole game about twenty times, below the page. The fix is in the store, the projector, and the planner (D1). May a pass that began as the page's leftovers change them? The aim is under one second to check and to confirm at any revision of The Tractor Works. | Yes. No change to the page can remove more than a few percent of the time. Every event is still verified, and section 6's test holds the two ways of reading a game equal. |
| 3 | A laden unit's rout: (a) one proposal, in which the rout leaves what A10.4 says and the panel names what is left and kept, with a choice only among loads of equal PP; or (b) two proposals, a drop offered in the Rout panel and then the rout. | (a). A10.4 makes the leaving a part of the rout and leaves the player a choice only among equal loads; (b) keeps a refusal a player meets at every such rout. |
| 4 | Backlog section 50's row "a broken unit's drop refused for one SW and accepted for another" is a fault in the code and not a reading for the referee (section 2.2). Correct it by A10.4 as D2 has it? `p31c-tw` keeps its recorded drops and replays as it was played. | Yes. |
| 5 | Fire by concealed units where no Good Order enemy unit sees them (D4): decide it in the Fire package in this pass, taking the loss of "?" as forced whenever a Good Order enemy unit has the LOS, with a backlog row for the viewing side's option (A12.14)? Or leave the refusal and a backlog row? | Decide it in this pass. Until then a side can try any "?" stack with a concealed unit and read the answer from the refusal, with no unit marked. |
| 6 | A Dummy stack's removal (D5): a record for both sides once it has happened, and before Confirm only the owner's own Dummies, worded with "if" for a move so that it tells nothing of the enemy's "?"? | Yes. Both players see the counters leave the board; neither may learn before the move whether a "?" that sees the hex is real. |
| 7 | LOS before an attack is declared (D7): (a) free LOS checks, stated as a ruling that departs from A6.11, with the LOS tab and Cancel as they are; or (b) A6.11 kept for a side's view of a game still played: no LOS tab for a side, nothing of the LOS in a fire proposal before Confirm. About 1:00 more for (b). | (a) in this pass, with a backlog row for (b). The LOS tab is how you follow a game on the page, and (b) takes away pass 31's warning of a shot with no LOS. If you want the rule kept, (b) is small enough to take now; say so and I add it to 31d.4. |
| 8 | A refusal's reasons (D8): where the target Location holds something the firing side cannot see, tell the reasons that rest on its own group and the map when there are any, and otherwise the one sentence, never both? | Yes. What a side reads then depends only on its own group. |
| 9 | The CCPh ended with a Location not fought (D9): a consequence before Confirm, or a refusal? | A consequence. Where the Close Combat package cannot decide the Location, a refusal would leave the phase with no way to end. |
| 10 | The tests (D13): a board of 10 by 6 hexes painted in code, with the range and rout page tests on it, and the three tests that need a played fixture or a long set-up left in the backlog? | Yes. |
| 11 | Setup by hand in a player's words stays out, but for the two lines of D12? | Yes. It is a task of setup's own form, and no leftover. |
| 12 | The estimate: 9:30 (build 8:15), or the shorter cut of 7:30 without the concealed firers and the larger board? | 9:30. |

## 13. For the build session

- **The timing copy.** `p31d-t5b` in the Studio's live games is `p31c-tw` cut at revision 607 (the Russian Movement Phase of Turn 5), not advanced. A cut is the game's file with its `game` id changed and its events of a revision up to the cut. It is Studio data under `src/ASL/boards/`, untracked, and never staged.
- **The profiler.** `dotnet tool install dotnet-trace --tool-path <scratchpad>/tools`, then `dotnet-trace collect -p <pid> --profile dotnet-sampled-thread-time --duration 00:00:00:28 --format Speedscope` while the page proposes. The profile named `cpu-sampling` is for Linux only.
- **The Play page's address** is `/games/play?game=<id>`; `/units/play` is not a page.
- **A phase's end leads to a hand-over** when the next phase is the other side's, so a script waits for the review to go, not for the actions pane to come back.
