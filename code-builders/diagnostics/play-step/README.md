# play-step

The measurements behind `src/ASL/docs/Memory-And-Performance/ASL Map Studio Play Step Analysis.md` (its section 2) and the Studio test of
`ASL Map Studio Play Step Plan.md` (its section 2.1). Every figure comes from the Studio running the Play page's follower of `cd-guards-5`
as the adjudicator, the page visible, a step a second paced on the server.

Since 2026-10-25 `cd-guards-5` is retired with the other games (in `E:/Archive/GitHub/dlandi/asl-retired-2026-10-25/live`); follow
`dl-tractor-works-02` instead, or copy `cd-guards-5` back into the live folder to repeat a recorded figure.

| File | What it does |
|---|---|
| `follower_cut.py` | Brings a follower game to a revision of its record at once: after "Start following", rewrite the follower's file with the record's prefix that ends at a step's end, then press "Next step" once. Saves the 12 minutes of following 700 steps a second at a time |
| `steplog.js` | Evaluated in the Play page: notes `[performance.now(), Date.now(), revision]` at each change of the follower's "revision N of M", and stops the run at `window.__playStepStopAt` |
| `split.py` | A run's step log (and optionally the `dotnet-counters` CSV) split by phase: a step's work is its interval less the follow loop's one-second pause |
| `trace_split.py` | A `dotnet-sampled-thread-time` speedscope trace split into steps by the opens of `Play.FollowNext`; inclusive time of named frames per step, by phase |
| `trace_children.py` | Time under a frame by the frame right below it, split into CPU and blocked leaves, in a time range |
| `trace_nested.py` | Time on stacks that hold an outer frame, and of it, on stacks that also hold inner frames |
| `stalls.py` | The stretches where the runtime's allocation counter reads 0 while collections run, and the jump that ends each (the analysis, 2.5) |
| `window.py` | The counters' seconds around a wall-clock window |
| `steps_by_phase.py` | A record's steps (attempts) counted by turn and phase, with their first revisions: where to cut a follower (the Berserk Charge Plan, 2026-10-23) |
| `audit_spans.py` | The server's spans of a live session from `audit.jsonl`: check to gate verdict, and executor start to end, by turn, with the slowest |
| `counters_series.py` | A `dotnet-counters` CSV as a table of working set, committed and generation sizes, allocation, CPU, and collections, every N seconds and in a window |
| `trace_inclusive.py` | A speedscope trace's frames ranked by inclusive CPU time over all threads, filtered by a name; and the busiest threads |
| `split_log.py` | Two whole-game follower runs' step works (compact `[revision, interval]` logs) split into Rout Phase and other steps: count, median, p90, max, and the slowest |
| `trace_calls.py` | How many times a frame is entered (outermost), each entry's duration, and from which enclosing frame |
| `allocread/allocread.cs` | Reads a `gc-verbose` nettrace: allocation ticks by type, by the nearest LimboDancer frames, by second; and the collections. `dotnet run allocread.cs -- <file.nettrace>` (a file-based program) |
| `clockbench/clock.cs` | The cost of a `TimeProvider.System.GetUtcNow()` call on this machine (the analysis, 2.4) |

## Two rules of reading

- **Time from outer frames only.** The sampled-thread-time trace stops a thread at its next safe point, so a leaf such as a P/Invoke is
  charged the time of the loops before it (the analysis, 2.4). The rout read, the enemy lists, encirclement, and `FollowNext` are sound;
  a leaf's share is an upper bound.
- **Allocation from allocation ticks only.** The runtime's `total_allocated` counter stalls at 0 for seconds and then reports the backlog
  in one second (the analysis, 2.5). `allocread.cs` reads the ticks of a `gc-verbose` trace.

## The Studio test (the plan, section 2.1)

Two Studios, the same game and revisions, side by side:

- **after:** the branch built outside the repository (`-p:OutDir=E:/Archive/GitHub/dlandi/asl-gate-work/studio-6670/`), launch config
  `map-studio-p31b`, port 6670 (it was `bin/p31b` until 2026-10-19);
- **before:** `main` built outside the repository, in a worktree of its own (`git worktree add --detach <path> main`, then build its MapStudio
  there) and started from that build on port 6671 with `--contentRoot` the worktree's MapStudio folder and `--AslMaps:BoardsRoot` this
  repository's `src/ASL/boards`. Register the worktree and both Studios' builds as they are made
  (`bash code-builders/build-and-test/outside_outputs.sh register <path>`), so the gate's `ci` stage removes them once CI passes (the
  standing rules, section 0, point 14). Never build it into another folder under MapStudio's `bin`: the gate's build copies every such folder into
  itself (the gate now refuses to start while one is there). On 2026-10-19 it was built into `bin/before`, which cost the next three builds
  20 to 35 minutes each.

Both read `src/ASL/boards`, so each follower they make is a new game `cd-guards-5-follow-<n>`. Delete the followers after the work.

Measure with each Studio's tab in front: a hidden tab measured lower on 2026-10-19, so a run in a hidden tab is not comparable with one in
front. Runs of 9 Rout Phase steps vary by about ±40 ms from run to run; the whole-game run (run 1) is the one to compare.

1. **Open the follower.** `/games/play`, `#play-perspective` = `adjudicator`, `#play-handover-confirm`; `#follow-record-source` =
   `cd-guards-5`, `#follow-record-start`. Then `follower_cut.py <live folder> cd-guards-5 <follower> <revision>` and `#follow-record-next`.
2. **Behaviour.** Held at the Rout Phase states (revision 621, the Russian Rout Phase of turn 4; 729 to 744, the German one), read the
   Rout panel's obligations and choices, the unit table (`#play-units`) with its cut-off notes, and the audit panel's last lines, in both
   Studios, as the adjudicator and as each side. They must read the same.
3. **The short run.** Cut the follower to 714, evaluate `steplog.js` with `window.__playStepStopAt = 760`, press `#follow-record-run`,
   save `window.__playStep.log` as JSON, and run `split.py <record> <log>`: the Rout Phase steps (729 to 744) and the others, against
   the "before" Studio's run of the same revisions.
4. **The allocation trace** (tasks whose gain is allocation): cut to 620, step to 621, then trace one step by hand:
   `dotnet-trace collect -p <pid> --profile gc-verbose,dotnet-sampled-thread-time -o run.nettrace`, press `#follow-record-next`, stop
   the trace a few seconds later, and read it with `allocread.cs`.

`dotnet-trace` and `dotnet-counters` are not in the repository; install them into a scratch folder (`dotnet tool install dotnet-trace
--tool-path <scratch>/tools`). Measure with nothing else building or testing on the machine: the gate's stages beside a run double its
figures.
