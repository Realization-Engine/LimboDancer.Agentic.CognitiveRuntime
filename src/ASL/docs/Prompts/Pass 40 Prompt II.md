# Pass 40, the rest: enemy Locations, the small rows, the reviews, the check, the documents, the gate

Written on 2026-10-11 as insurance, in the session that built increments 2a to 2c. It is for a session that has lost that session's context; the session itself carries on from here.

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc (the gate's baseline). Read `Pass Standing Rules and Harness Lessons.md` and `Pass 40 Prompt I.md` first, then the [design](<../Passes/ASL Unit Backlog Pass 40 Design.md>), which holds everything decided and built.

## The user's word of 2026-10-11

"Complete Pass 40 without my intervention." Read as: each task is committed without asking; rulings and review fixes are decided by the proposal that would have been put to the user, and each is recorded in the design as decided under this word, for a veto afterwards; the reviews, the Studio check, the documents, and the gate are run; the merge and the push are made only if the gate is clean (every difference of the three proofs named as an intended change, every suite green, Docker green). Anything not clean: stop and report.

Earlier words that still hold: a Studio game is not skipped for cost; new code is tested in games on the rulebook's own example hexes where a board has them (boards 4 and 5 play; any Verified VASL board does); dice are scripted for the outcome wanted.

## Where the pass stands

Committed: 68c823b8 (the plan's status, prompt I), 472aa365 (the frame), 79c1e663 (40.6 stacking), 3946562e (40.2 the road), 17bac58b (40.3 Dash), fd036fa9 (40.8 Hazardous Movement), 275b64fc (40.7 portage). Rulings R40.1 to R40.11 are approved and written in the design only; the Passes Plan gets them with the documents.

Left, in order:

1. **Increment 2d:** 40.4 Infantry OVR in full and recapture of Unarmed units by entry; 40.5 the forced TPBF and a crew's FP in FPF. The design's sections 3.4, 4 (B and C), 5.3, and questions F5 and F6 say what was decided. Remove `EnemyUnitFacts.Broken` here.
2. **Increment 2e:** 40.9 the small rows (question F10), with the DFPh sentence and the 320 pixel prompt (question F11).
3. **The reviews:** five read-only agents in one message, their briefs in a prompt file, as pass 35 ran them.
4. **The fixes** chosen by judgement; a finding not fixed gets a backlog row.
5. **The Studio check of the whole pass** at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 pixels, in each view, with a full Player Turn at the largest and the smallest.
6. **The documents:** the review; the design's "As built"; rulings R40.1 to R40.11 and those of 2d and 2e in the Passes Plan; the inventory's rows (five moved by question F2, A5.132 to pass 50, A4.43 to passes 125 and 185) and its two count tables; the coverage document; the backlog's section for the pass, a row for every "left out" of sections 7 to 11; the plan's status; the time log; the standing rules' lessons below.
7. **The gate,** then the merge and the push if it is clean.

## Tools

`E:\Archive\GitHub\dlandi\pass32-tools\pass40\`: `make_board_game.py` (a squad a Location on any board), `make_stack.py`, `make_hazard.py`, `make_portage.py`, `make_407_games.py`, `make_boarding.py`, `board_version.py`, `hexfacts.py`, `woods_road.py`, `game_events.py` (what a game file recorded), `trx_failed.py`, and the `apply_*.py` and `docs_*.py` scripts of each task as models. The gate's scripts are pass 35's, to copy.

Build: `DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet build LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj --disable-build-servers -m:1 --warnaserror -p:OutDir=bin/p31b/ -v:q -nologo` from `src/ASL`, with my Studio stopped. Filtered tests: `dotnet test <csproj> --disable-build-servers -m:1 -p:BaseOutputPath=bin/t40/ --filter "FullyQualifiedName~<Class>" --logger "trx;LogFileName=x.trx" --results-directory <folder>`. The Maps tests of the road's target points need `AslMaps__VaslRoot=E:/Archive/GitHub/dlandi/vasl`.

## Lessons of this session, for the standing rules

- **A click on Confirm is not a commit.** Read `#play-status` for "Committed through the Execution Gate", and read the game file (`game_events.py`) for the event. In 2a two shots were taken for committed that the gate had refused.
- **A new fact of a fire attack must be carried by the record's verifier** (`LiveFire.cs`, the merge of recorded map facts), or every attack that holds it is refused at the gate. The same holds for an ordnance shot (`WithRecordedMapFacts`).
- **A failing new test may be at fault in its control.** Three times the control broke a rule of its own (one squad firing twice against A8.3; a lone crew as a target, ruling R24.3). Read the refusal before touching the rule.
- **`window.status` cannot hold a helper** in the page (it is a string of the browser's); name it otherwise. Keep the helpers in `sessionStorage` and `eval` them.
- **The game list of the Play page is read when the page loads:** open a game written afterwards by its URL, `/games/play?game=<id>`.
- **The page offers boarding only while no Infantry move is open,** and the vehicle panel is hidden while one is; spend a unit's MF in place (a failed Recovery, dr 6) to stage what follows a move.
- **Board 3's row 10 from [B10] to [G10] is Open Ground;** [C9] is woods beside [D9]; the woods of [F5] are Bypassed from [E6] along their south hexside.
- **The rulebook's examples stand on real boards:** A4.132 on board 5 ([H3], [I4], [I9], [I10]), A4.63 on board 4 ([CC8], [BB7], [AA7], [Y9], [CC6]), A5.11 on board 1 ([F5]). A game names its board with the Git blob id of the board archive's LOSData entry (`board_version.py`).
- **One-line anonymous objects in a test fail IDE0055;** run `dotnet format whitespace <csproj> --include <file>` on a new test before the run.
- **Read the clock before writing a time,** every time; twice a row was written a minute or two ahead of the clock.
- **The reflexes still slip:** a bare `python -` at the end of a shell line, and a call to a script not yet written. Look at the last token of a Bash call before sending it.
