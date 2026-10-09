# Pass 35: Repairs, wrong results in rules already built. The design first, in increments

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, main one commit past 36fbbdd (pass 32.j merged and pushed on 2026-10-09; the migration is complete, and its CI run has not been read yet). That commit, "Renumber the rule passes 35 to 185 in steps of five", is local and not pushed. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it throughout; its three lessons of 2026-10-09 (the renumbering, the views' own unit labels, a played game as the fixture of its repair) apply here.

## Where the last session stopped

The renumbering of 2026-10-09 is committed on main at the user's word, with this prompt and three new lessons in the standing rules file. It is not pushed. The Rules project was built with warnings as errors before the commit; no test was run, since the only code changes are four comments.

- **What it is.** The 31 rule passes are numbered 35 to 185 in steps of five, in the order they run, so that a pass added later takes a free number between two others. The pass this prompt starts, Repairs, was 45 and is 35. Armored combat I is 50 (was 33). The deferred DYO passes are 190 and 195. The plan's section 4 has the record and the table of old and new numbers ("Renumbering of 2026-10-09"); its section 23 is the schedule in force, rewritten the same day from section 22.1; section 22.1's status line records the user's provisional approval.
- **The eight files.** `docs/ASL Card Play and Map Studio Redesign Plan.md`, `docs/ASL Rule Inventory A to E.md`, `docs/ASL Rule Coverage.md`, `docs/ASL Unit Backlog.md`, `docs/ASL Unit Backlog Passes Plan.md`, and comments only in `LimboDancer.Domains.Asl.Rules/ScenarioA1Definitions.cs`, `ScenarioA1FireEligibility.cs`, and `ScenarioA1TerrainCosts.cs` ("for pass 45" became "for pass 35").
- **Documents of finished passes keep their old numbers:** the pass 32 design, the time log, the week review, the reviews.
- **Checked then:** section 23 ascends by five from 35 with no pass needing a later one; the 31 task-list headings of 22.1 (c) match it in order, title, and row count; every task number sits under its own pass; the inventory's cells for each pass equal its summary row.

## The first steps, before the design

1. Read the CI run of 36fbbdd and say what it shows.
2. Show `git status` and `git log -3 --oneline`, confirm the renumbering commit is the head of main and the tree is clean but for `src/ASL/boards`, and ask the user's word to push it.
3. One item is proposed for this pass and not yet in the plan. Put it before the user with the text below, and on their word add it as task 35.17 and carry the 0:20 through the totals: pass 35 (build 6:45, total 8:00, likely 4:00), block 1, and the grand totals in the plan's sections 22.1 (b), (c), (e), (f) and 23, and the whole-plan figure. It is a commit of its own.

## The proposed item: a Rout Phase that cannot end

Found on 2026-10-09 in the user's game `cd-guards-5`, Turn 4 of 5, German Rout Phase, played in the Studio on 6670. Nothing was confirmed in it; the game stands at 729 events. A copy of the file as it was is at `E:\Archive\GitHub\dlandi\pass32-tools\pass35\cd-guards-5.rtph-deadlock.game.json`; the live file is `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/cd-guards-5.game.json`. Do not advance the live game before a fixture is cut from it.

- **What happens.** Two broken German squads in [H5] must rout (German view: 4-4-7 squad G7 and 4-6-7 squad G9; the Russian view calls the second G13). Every route the Rout panel offers them, and Low Crawl, is refused: the unit "can get away ... only by Interdiction or Low Crawl, so it surrenders ... as the RtPh ends instead of routing (A20.21)". The German side may not end the phase: "The Russian side still has a unit that must rout ... hand over to the Russian side first (A10.5; ruling R31.6)". The Russian units in [G5] may not rout: "The ATTACKER's 4-6-7 squad G13 must rout first (A10.5)". No action of either side ends it.
- **The cause as read in the code.** `GamePlanner.Rout.cs` line 298 hands `ScenarioA1RoutCalculator.AttackerMustRoutFirst` a `canRout` that asks only whether a rout step exists (`CanRout`, line 219). The A20.21 check at line 310 (`SurrenderCandidate`, `SurrenderCause`) is made only for the unit that routs. A unit bound to surrender therefore owes a rout for ever.
- **Two lesser faults in the same place.** The panel says such a unit "must rout" and offers routes the gate refuses. The surrender waits for the phase's end, with no action that takes it in the unit's turn of the rout order.
- **The task as proposed.** "35.17 A unit that surrenders instead of routing (A20.21) no longer holds up the rout order: the DEFENDER's units do not wait for it, the phase can end, and the Rout panel says it surrenders and offers it no routes. Rules: A10.5 (p. 67), A20.21. 0:20." The surrender verdict becomes a fact `AttackerMustRoutFirst` takes, in Rules. A20.21 was not read in the PDF for this; when the surrender happens (at once in the rout order, or at the phase's end) is for the design to settle from the page.

## The task

Write the design of pass 35, `src/ASL/docs/Passes/ASL Unit Backlog Pass 35 Design.md`, read-only as to code, and in increments at the user's word: "since it needs a design doc first we will carefully work on that first, incrementally". No branch and no code until the design is answered and the user gives the go for the build.

**What the pass is.** The plan's section 22.1 (c), "Pass 35: Repairs: wrong results in rules already built": sixteen tasks (seventeen with 35.17), 45 rows of the inventory, places where the game gives a result the rule forbids. It comes first because passes 40, 50, 80, 85, 90, 110, 115, 125, and 135 read what it repairs. Every repair is written in the Rules project (the plan's section 19, decision 7): a calculator takes facts and returns a verdict; Play and the projector hand facts over and decide no rule.

**How this pass differs from pass 32.** Pass 32 changed no behavior, and its proofs (the replay digest, the planner sweep, the text list) had to come out equal. Pass 35 changes results on purpose. The same proofs will differ, and each difference must be read and named as a task's intended change or as a fault. The design says, task by task, which games and which refusal texts are expected to change, and how a recorded game that plays differently after a repair is treated (ruling R31d-style revisions, or a catalog and rules version, as the earlier passes settled it; find the precedent and propose).

## The increments

One increment a response, each ending with a stop for the user. Propose this order and let the user change it:

1. **The frame.** The design's outline; the 45 inventory rows listed by task (filter the inventory for "Pass 35"), with each row's status and note; what the audits said is wrong today for each; which tasks are certain from the reading and which need a run to settle. No rule text yet.
2. **One task group at a time**, in this order unless the user says otherwise: the Rout Phase item (35.17) with 35.2 and 35.4, since the played game is waiting on it; then Infantry (35.1, 35.3, 35.8); terrain (35.5, 35.6, 35.7, 35.9); ordnance (35.10, 35.11, 35.12); vehicles (35.13, its nine corrections each on its own); night and weather (35.14); the two that are not rules (35.15, 35.16). For each task: the rule read on its page of the registered PDF (the whole rulebook is in scope; page citations are provenance), what the code does today with file and line, the repair as a Rules verdict and the facts it takes, the refusal texts added or changed, the tests, and the questions, each with a recommendation.
3. **The cross-cutting sections:** recorded games and versions; the proofs and how differences are read; the reviews; the Studio check; the commit order; the estimate against the plan's 6:25 (6:45 with 35.17).
4. **The questions together**, for the user's answers, and the design's status line.

The 22.1 task list may change in the design; the plan says so. A fault found while reading that is not in the 45 rows is proposed for this pass or for the pass that reworks its rule, and goes to the backlog if it is left out.

## The reading, in order

1. The plan: section 19 (decisions 5 to 7), section 22.1 (the status, (c) for pass 35, (d), (f), (g)), section 23, and the record of the renumbering in section 4.
2. `src/ASL/docs/ASL Rule Inventory A to E.md`: sections 1 to 3, then the rows whose pass is 35.
3. `src/ASL/docs/ASL Rule Coverage.md`: the sections those rows belong to, and its list of faults (around line 340).
4. `src/ASL/docs/Passes/ASL Unit Backlog Pass 32 Design.md`: section 12 (the copies kept apart "for pass 45", which is pass 35 now: the three comments in Rules point there) and section 24 (the migration as built).
5. `src/ASL/docs/ASL Unit Backlog.md`: sections 53 and 54, and the rows the tasks name.
6. `src/ASL/docs/ASL Week Review 2026-09-28 to 2026-10-04.md`, for task 35.16's seven unwritten tests and the sweep test.
7. The Rules files of each task as its increment comes, and the registered PDF for every rule cited.

## Tools and builds, for when the build starts

The proof tooling of pass 32 is at `E:\Archive\GitHub\dlandi\pass32-tools` (the pass 32.j prompt, `src/ASL/docs/Prompts/Pass 32j Prompt.md`, describes it; it is bound to the baseline of 042d14a and needs a new baseline of main). Claude's Studio builds with `-p:OutDir=bin/p31b/` and runs on 6670; the gate builds with `-p:BaseOutputPath=bin/gate/`; the user's Studio on 5178 is never touched. The projects share `obj/`: never one build beside another.

## Standing rules that bite here

The rulebook is read first, and at every task. Every ASL rule in Rules alone. Every open question comes with an analysis and a proposal. Whatever is left out goes to the backlog. Record start, end, and duration of each increment in `src/ASL/docs/ASL Unit Time Log.md`. Stage explicit paths, never `src/ASL/boards` or `.claude`. No em-dashes or emoticons in anything written. Ask before the commit, before the branch, and before the merge.
