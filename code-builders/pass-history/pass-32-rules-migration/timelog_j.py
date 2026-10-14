"""Appends the pass 32.j section to the time log (LF, no BOM)."""
import pathlib

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Time Log.md")
text = PATH.read_bytes().decode("utf-8")
assert "## Pass 32.j" not in text
section = """
## Pass 32.j: S10 (setup, entry, exit, the Victory Conditions, and concealment at setup) into Rules

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Preparation (read-only): the design's sections 5 to 9, 12, 15, 23, the Appendix's rows of the seven files and SetupConceal, the seven Play files (2,914 lines) and the projector's member, the count (56 against 57), the eight searches (one for a fact reader, the fold for a second), the delegates, the files, the commit order, the store's coverage; the stop | 22:31 | 22:34 | 0:03 | The prompt had named every reading; the port 6670 question came between the stop and the go |
| The branch from 042d14a; the baseline started from a checked archive (72 uses of the projection, no S10 file; built 22:38 to 22:39; replay 770 s, sweep 1,392 s over four games, done 23:16) | 22:36 | 22:38 | 0:02 | The baseline ran beside the first seven commits |
| Commit 1 (ad44ed4): the card's rules into ScenarioA1SetupCalculator.cs; text list equal | 22:38 | 22:45 | 0:07 | |
| Commit 2 (f61e728): ScenarioSetup.Check and Fill; text list equal | 22:45 | 22:52 | 0:07 | |
| Commit 3 (d4743ea): CardSetup's decisions, PlayableBar, CardSetupIncomplete, OutOfSight, NonObConcealmentBar; text list equal | 22:52 | 22:57 | 0:05 | |
| Commit 4 (79e1837): the entry into ScenarioA1EntryCalculator.cs; a record name clash (EntryMoverFacts) and a missing Edge fact, two build tries | 22:57 | 23:04 | 0:07 | |
| Commit 5 (bb84c6f): PlanExit's ten blocks and ExitScoring; text list equal | 23:04 | 23:10 | 0:06 | |
| Commit 6 (e2a6ee8): VP and the immediate Victory into ScenarioA1VictoryCalculator.cs; IDE0055 fixed by format | 23:10 | 23:14 | 0:04 | |
| Commit 7 (8df09e0): ScenarioVictory over a state reader; MeleeUncontrolled missing from the condition facts, two formatting tries | 23:14 | 23:24 | 0:10 | |
| Commit 8 (061d3f5): concealment with its fact reader, SetupPlanMatch.Look, the projector's SetupConceal; one hole kept by naming the parameter events | 23:24 | 23:30 | 0:06 | Eight commits in 0:54 |
| The proof rebuilt against bin/p31b (copies dated 23:29); the review agent and the slice's tests started; the replay threw at the first setup-concealed event (the projector's map lacked Concealed): commit 9 (65c9726), MapStudio rebuilt, the proof rebuilt (copies dated 23:34), the proofs and the tests restarted | 23:30 | 23:36 | 0:06 | Stopping the test script left its processes and a second script running beside it; both stopped by process id |
| The replay digest (88,728 lines, 1,054 s) equal to the baseline; the review (no behavior change, one cost: PlayableBar's double card read); the slice's tests: Rules 24 of 24 (14 new) after four of my expectations were corrected, Units 409, Play 162 of 162 (passes 17, 19 to 21, 23 to 26, 30, 30b), MapStudio 32 of 32; commit 10 (bd1595a): the tests and the review's fix | 23:36 | 00:03 | 0:27 | The Play tests ran twice: the first run built from the defective tree |
| The gate build into bin/gate (0 warnings, 4:41); Node viewport 5 of 5; the Studio self-test on 6671 from bin/p31b (6670 held by another chat's Studio again): p26-demo's Victory result and standing from Rules, a new game from The Guards Counterattack set up from a plan (every check passed) and refused with three "?" of 0 (ruling R19.5); the chart supplement equal | 00:03 | 00:09 | 0:06 | The test game p32j-check is untracked Studio data; the 6671 launch entry is temporary |
"""
PATH.write_bytes((text.rstrip("\n") + "\n" + section).encode("utf-8"))
print("time log appended")
