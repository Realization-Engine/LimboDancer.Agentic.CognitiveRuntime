# Scenario A1 Backlog Pass 50: Armored combat I

The review of pass 50, written on 2026-10-12 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 50 Design](<ASL Unit Backlog Pass 50 Design.md>); its section 11 says what was built.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-50`, cut from main at b4ce31ef: the frame, four tasks of six in three commits, then the five reviews, their fixes, the Studio check, and these documents. Everything was done under the user's word of 2026-10-12 for this pass, "work independently as much as possible up to merge gate and then pause", and is open to a veto. **The gate was run on 2026-10-12 at the user's word ("Proceed with the gate") and passed.** Merged into main as 85d628bd and pushed on 2026-10-12 at the user's word ("Proceed with the merge, push, and close the commit"); the CI run of the merge was left for the user to read. The design's section 12 has its table. It found one thing, outside the pass: the race between two page test classes over unit names (backlog 57.2), met locally for the first time and removed on the tests' side in 85685aac. No product code changed in the gate.

## What departs from the plan's entry and the frame

- **Task 50.4, Gun Duels (C2.2401), is not built** (section 6, F5).
- **Task 50.5, MG fire against armor (A9.6, A9.61, A9.611, D3.54), is not built** (section 9.1). The frame placed it in increment 2c.
- **A BMG fires only inside the VCA;** the hull's pivot for a shot is not built, against the answer to F4 (section 7.1).
- **A turret turned without fire at the end of a fire phase (C3.22) is not built,** against the answer to F6 (section 8.1).
- **"One hex for all weapons" (D3.51's last sentence) is not built.** Section 3 placed it in 50.2 and section 7.1 did not list it; the referee found it. A tank may Bounding First Fire one weapon in one hex and another in the next.
- **A CMG fired at another target does not lose the MA's Acquisition (D1.82)** (section 7.1).
- **A declared higher MP expenditure (D2.18); the TCA turned with a Start MP, or in woods, a building, or rubble; a Motion firer of the side that is not phasing; the step bar of a TI vehicle** (section 8.1).
- **A5.132 and OVR Prevention (C5.64)** (section 6, F1).
- **Defensive First Fire at a vehicle entering the firer's hex, a SW at a vehicle in its own Location, and everything of Bypass** in the same-hex shot and the turret's turn (section 9.1; the reviews).
- **No Play test for task 50.6:** the setup refuses a vehicle in an enemy vehicle's Location. Two Studio games and a Rules test of six cases hold it.
- **No real key was pressed in the Studio check.** The pane was not on screen, and the harness sends keys only to a pane that draws; the controls were driven by their click handlers and each was checked for a label, a title, and a reason when disabled.

## What a player now meets

- A tank is a firer in the Fire panel. Its BMG and CMG are listed with their FP; they fire together or apart, each once a Player Turn, and all of a vehicle's weapons fire in one fire phase (D3.5). For the DEFENDER, First Fire in the MPh and Final Fire in the DFPh are one (A.15). The vehicle carries one fire counter for all its weapons.
- A BMG fires at a target inside the VCA, to eight hexes at full FP and sixteen at half; a CMG through the TCA, to twelve and twenty-four, with no leader (D1.81, D1.82, A9.4).
- A CMG fired outside the TCA turns the turret and adds Case A to its IFT DR: 1 a hexspine for the PzKpfw IIIH, 2 and then 1 for the T-34, doubled in woods, a building, or rubble (D3.52, C5.11). The MA's next shot within that TCA pays the same (D3.51).
- A MG malfunctions on an Original 12; of two fired together, Random Selection finds the one (D3.7, A9.71).
- In the RPh a tank's crew that is not shocked or stunned tries to repair each malfunctioned weapon by a dr of its own: a 1 repairs it, a 6 disables it. A tank whose MA is disabled is Recalled (D3.7, D5.341).
- In its own MPh a tank's MA fires as Bounding First Fire, at the outset of its move or once the DEFENDER has passed: within its TCA, with Case C (+4 for both tanks), Case C1 (+5) for a LOS of two and a half to three MP, Case C2 (+6) for two or less, and the lower dr doubled while it has not stopped (C5.3 to C5.35). It takes no Acquisition DRM (C6.55). It spends another MP before it fires again; a spent ROF leaves a Bounding Fire counter (D3.3).
- "Propose: Delay" spends one MP in place while the tank is stopped or has not started (D2.17). "Turret with the next MP" turns the turret with a Delay, a stop, a VCA change, or an entry (D3.12).
- In the AFPh a vehicle that entered a new hex or is in Motion fires its MA with Case C, where it was refused; the MA that kept its ROF in the MPh fires once more (C5.3, D3.32).
- A Gun, or a tank, fires at an enemy vehicle in its own Location with Case E; the colored dr of the To Hit DR gives the Target Facing: 1 or 2 the rear, 3 or 4 the side, 5 or 6 the front (C5.5, D3.2).

## The rulings

R50.1 to R50.14, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All fourteen were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-12.

## The reviews' findings and what was done

One BLOCKER, fixed. The findings fixed:

| Review | What was wrong | What was done |
|---|---|---|
| Table player 1; referee, second half, 2 (BLOCKER) | An Acquisition DRM was taken in Bounding First Fire, which C6.55 (p. 175) gives to a Stabilized Gun alone; the old Case N line was reached by the new shot, and it turned one recorded miss into a hit. | No Case N for a Bounding First Fire shot or a Motion firer's, in both calculators; what such a shot acquires starts from nothing (ruling R50.13). The games recorded again. |
| Referee, second half, 3; table player 3 | Case L was given to a Motion firer at Infantry. | Not when the firer is in Motion or has not stopped (C6.3, p. 174), as the Vehicle Target Type already had it. |
| Referee, first half, 3; second half, 1 | A Bounding Fire counter a MG placed let the MA fire twice with no MP between. | The MP between shots is waived only when the MA's own ROF is spent. |
| Referee, first half, 1 | A tank that had Overrun could fire its MGs and MA apart from it. | An OVR is written into the vehicle's shots of the MPh, and a Bounding Fire counter beside a shot of the MA stops the MA unless it kept its ROF (`BoundingFireSpent`). |
| Referee, first half, 2 | A First Fire counter a MG placed stopped a MA that had kept its ROF. | The counter is not read as the MA's while the MA's last shot of the phase kept its ROF. Across the MPh and the DFPh: backlog. |
| Referee, first half, 5 | An Abandoned tank fired its MGs and repaired its weapons. | Refused in both (D5.41). |
| Referee, first half, 7 | The MA paid nothing for a turret its CMG turned after the MA's own first shot. | The MA pays on its first shot after the CMG's turn, whenever that comes. |
| Referee, first half, 8 | The MGs were barred from the AFPh after a MA-only Bounding First Fire that left no counter. | Allowed, as D3.31 has it (`OtherFirePhase`). |
| Referee, second half, 4, 9, 10 | The same-hex shot and the turret turned with an MP read nothing of Bypass; a maintained Case A was added to Case E. | A same-hex shot by or at a vehicle in Bypass and a turret turned in Bypass are refused with the reason; no maintained Case A within the hex (C5.51). |
| Referee, second half, 7 | A turret turned with an MP kept the MA's Acquisition. | It is a CA change for C6.5. |
| Rules boundary 1, 2, 5, 9 | The calculator did not bar a tank's MA at Infantry in its own hex; the projector took any weapon name for a repair; the page repeated the list of steps a turret turns with; the older-record leave reached records that cannot predate the pass. | Barred in the calculator; an unknown weapon name is refused by the one member; the page calls `TurretTurnsWith`; a Bounding First Fire shot and a same-hex shot at a vehicle keep their facts. |
| UI 1, 2, 3, 6 | The turret choice was never reset, was sent for vehicles that have none, and was cleared before the gate answered; the vehicle named for the MA's shot and the ticked MGs were kept across vehicles and views. | Reset on a change of vehicle, a hand-over, and a phase change; sent only for the vehicle that shows the choice; cleared when its step commits; the vehicle is sent only while the list still offers it. |
| UI 4, 5, 7; table player 12 | Two buttons were disabled with no reason; the Fire panel let a tank be proposed with no MG ticked; the MA's button showed for a MA that cannot fire. | "Propose: MG Bounding First Fire" with its note; the Fire panel waits with "Tick the BMG or the CMG that fires"; the MA's button is hidden when the MA is malfunctioned, disabled, or its crew Recalled, stunned, or shocked. |
| Table player 4, 5, 6, 7, 9, 10, 11; UI 8 | The To Hit cases showed as identifiers; the Recall was told as a condition; a malfunctioned MG was not named; the MA's Bounding First Fire was not called that and its counter not told; one repair refusal covered three causes; the Replay page called a Delay a move and a disabled weapon eliminated; three sentences misread. | Each reworded: the cases in words in the proposal and the record; "the vehicle is Recalled: buttoned up and treated as stunned until the Player Turn ends, then it must leave"; "vehicle's BMG malfunctioned (D3.7)" with its Random Selection dr; "as Bounding First Fire (D3.3)", "it may fire again after another MP", "a Bounding Fire counter: no more fire this Player Turn"; a refusal for each cause; the Replay's two words; the tank's label names each weapon's state. |
| Table player 2, 12 | The three MG games held a firer name the code no longer writes; the AFPh control differed from its game by Case A as well; the superseded copies kept their games' labels. | Every game recorded again under a new id (`p50-mg-both2`, `p50-mg-turn2`, `p50-mg-first2`, `p50-repair2`, `p50-repair-ctl2`, `p50-bff-outset2`, `p50-bff-move2`, `p50-afph2`, `p50-afph-ctl2`); the control now faces east and shows Case B alone; every older copy's label begins SUPERSEDED or NOT PLAYED. |

The findings not fixed, each with a row in the backlog's section 58:

| Review | What |
|---|---|
| Referee, first half, 4 | One hex for all of a vehicle's weapons (D3.51, p. 201) |
| Referee, first half, 6 | A CMG turns the turret in woods, a building, or rubble after the MA fired there (C5.11, p. 172) |
| Referee, first half, 2 | A First Fire counter a MG placed in the MPh, against a MA that kept its ROF there, in the DFPh (D3.5, A.15) |
| Referee, first half, 10 | A Recalled tank's working MGs after its counter is flipped (D5.341; ruling R7.10) |
| Referee, second half, 5 | Case J against a vehicle that has only spent Delay or Start MP (C6.1, D2.12) |
| Referee, second half, 6 | Case C in the AFPh for a vehicle that began in Motion and only stopped (C5.2; ruling R50.10) |
| Referee, second half, 11 | A bogged or immobilized vehicle's MP for a TCA change (D8.5, p. 209) |
| Rules boundary 2, 3, 4, 6, 7, 8, 10, 11 | Decisions still made in Play, Units, or the page: that the counter prints the weapon repaired; what an obstacle and a turret are for the turret's turn, with the woods-road exemption for a stop or a Delay; which facing a weapon reads; the bars of a vehicle's MGs gathered twice; the repair list's own table of conditions; three smaller copies |
| UI 7, 9 | The turret choice shows where the gate will only refuse; the MG read runs three times a vehicle in the render path |
| Table player 8 | The record does not say that a CMG turned the turret, only its Case A |
| (the pass) | No test of the Case L fix; no Play test of task 50.6; the Studio check by real keys |

The referee's second half, finding 8 (Case C from woods), is answered by ruling R50.14 and not by a row.

## The Studio check

In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's, in game `p50-w-mph`: the vehicle panel's Delay button, the turret choice, the Bounding First Fire target, the two MG rows, the MA's button, the MGs' button and its note, each measured for anything past either edge. Nothing at any width, and the page no wider than its window. The Russian view shows none of them.
- **At 320, the Fire panel and the repair list** (`p50-w-pfph`, `p50-w-rph`): the tank as a firer, its two MG rows, the note "Tick the BMG or the CMG that fires", and the three weapons of the repair list, inside the page. The Russian view's repair list holds no German weapon.
- **A full Player Turn at 1920 by 1080** (`p50-bff-move2`, the adjudicator's view): the Start MP, the entry with the turret turned east, the MA's Bounding First Fire while moving, after the Stop MP, and after each of two Delay MP, the end of the move, and every phase to the Russian RPh.
- **A full Player Turn at 320** (`p50-walk320`): the same Start, entry with the turret, and MA shot while moving; the Stop MP; a Delay MP; the CMG's Bounding First Fire at half FP after its row was ticked (the button waits with its note until then); the end of the move; and every phase to the Russian RPh. The page stayed 320 wide at every step but one: while a proposal is open the inspector's tab row is 11 pixels wider, as pass 40 found; it has its backlog row.
- **Each side's view:** `p50-mg-both2` and `p50-repair2` were played in the German view; the others in the adjudicator's, which a vehicle's move needs for the DEFENDER's passes.
- **Not done:** real keys (section 10.1); the Replay page was not opened, though its two changed sentences are in its code; the turret choice and the MA's button were not measured with a vehicle in the target Location, which adds one more list.

## What was not done

- Tasks 50.4 and 50.5.
- The whole Rules and Play test projects, the solution build, the Node viewport test, the chart supplement, the planner sweep, and Docker: the gate's, which the user starts.
- A Play test for task 50.6 and a test of the Case L fix.
- Real keys in the Studio check, and the Replay page.
