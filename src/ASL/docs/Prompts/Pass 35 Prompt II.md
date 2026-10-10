# Pass 35, continued: ordnance, vehicles, night and weather, the two that are not rules, then the gate

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, branch `feature/asl-backlog-pass-35`, local and not pushed, fourteen commits past main (f3b7c47, pushed). Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its lessons of 2026-10-09 on test games, the Studio on 6671, and testing only what a change touches were learned in this pass. Then read the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 35 Design.md`: sections 1 to 6 are the frame, 7 to 9 the three increments built.

## How this pass runs, at the user's word of 2026-10-09

The first prompt asked for a design before any code. The user changed that on the first day, and this is the way of working now:

1. **One increment at a time, built as it is designed.** For each task: read the rule on its page of the registered PDF, read the code, write the repair in Rules (Play hands facts over), build MapStudio with warnings as errors.
2. **A small test game for each fix, checked in the Studio.** Written by script into the live games folder (the standing rules say how), opened in Claude's Studio, and the result read from the page. Say plainly when a fix has no game and why.
3. **xUnit tests with each fix:** a Rules test of the verdict, and a Play test where the planner writes events the Rules test cannot see. Run only the test classes the change touches. Whole projects run at the gate.
4. **Report, then stop for the user's word before each commit.** One commit a task. Do not push.
5. **Be sparing.** The user asked for practical token use: narrow reads, one targeted value from the page, no test run that proves nothing new.

## Where the last session stopped

Built, checked, and committed on the branch:

| Increment | Tasks | Commits |
|---|---|---|
| 2a, the Rout Phase | 35.17, 35.2, 35.4 (in three parts), and ADJACENT (A.8, B23.25), taken from pass 40 at the user's word | fcec850, 5fea497, 2f7369b, c08ddf7, 8ed93ef, 47fb402 |
| 2b, Infantry | 35.1, 35.8, 35.3 | eccc22a, c3dab32, d500121 |
| 2c, terrain | 35.5, 35.6, 35.7, 35.9 | c86e4dc, 827238a, b9c839b, 34f2c3e |

The user's own game `cd-guards-5` was played through its Rout Phase on the fixed build and stands in the German Advance Phase of Turn 4 at 743 events; the file as it was at 729 events is kept at `E:\Archive\GitHub\dlandi\pass32-tools\pass35\cd-guards-5.rtph-deadlock.game.json`.

**Answered by the user:** F1 to F6 of the frame; questions 1 to 4 of section 7.5; the orchard's season in the LOS (B14.2) moves to pass 135; the terrain increment's two missing tests are written at the gate. **Not yet answered in so many words** (each has its proposal in the design): section 7.5 question 6 (a repulsed unit surrenders if a captor can take it), and section 8.4 questions 1 to 4 (the TI bar kept on the SW actions; ruling R30.3's clause withdrawn; A25.211 to the backlog; no LLMC after a mortal wound in a rout, to the backlog). Put them to the user with the next report.

## What is left

**The increments, in order:**

- **2d, ordnance: 35.10, 35.11, 35.12.** Nine rows for 35.10 (rubble as a firer's terrain in Cases A, B, E and the CA lock; Case L for the ATR; Case O for a Hazardous mover; the Opportunity Fire exemptions of Case B and Case C3; a PF or PSK fired from rubble), two for 35.11 (Acquisition lost when the rule removes it, and following a vehicle target), one for 35.12 (no Bypass and no wall or hedge crossing while towing). The frame's section 4 marks the Opportunity Fire exemptions as read from the code and not run.
- **2e, vehicles: 35.13,** ten rows and the ESB table, each correction on its own (the frame's section 3.5 lists them a to j). The pass 32 design's section 12 items 5, 6, 14, and 15 were placed here.
- **2f, night and weather: 35.14.** A reversing vehicle and the weather cost is unsettled; read D2 and E3.9 together.
- **2g, the two that are not rules: 35.15** (a refusal for a game that places a fortification counter) **and 35.16** (the seven tests not written, and a sweep test of Play for what a side may not read).

**Then the design's cross-cutting sections 14 to 18,** which are still to be written: recorded games and versions (find the precedent and propose; the catalog version locks nothing, by the standing rules); the proofs and how a difference is read; the reviews; the Studio check; the estimate against the plan's 6:45.

**The documents, at the pass's end:** rulings R13.3 (Open Ground by the enemy's FFMO; a unit bound to surrender owes no rout; either side may end the RtPh), R30.3 (the berserk Fanatic's 11 withdrawn), R10.1 (HE into marsh), R10.10 (who forces a mover's loss of "?"), a ruling that a broken unit not under DM may not rout voluntarily (closing backlog section 52's row), and the repulse's surrender if the user agrees; the inventory rows of every task, with A.8 moved from pass 40 to this pass and B14.2 from this pass to pass 135; the coverage document's sections; the plan's status; backlog rows for what was left out (A25.211; the LLMC after a mortal wound outside fire; the TI rules for the SW and Deployment actions; Play tests of the Mopping Up and PF wound paths; the rest of pass 32's section 12 for passes 40, 45, 50, and 75; the Morale ceiling's Studio game).

**The gate,** only when the user says the build is done: the whole Rules and Play projects (not run since the Infantry and terrain changes; Play was last whole after ADJACENT, 697 of 698 before its one test was rewritten), MapStudio's page tests (not run at all in this pass; the Rout panel and the records changed), the solution build with warnings as errors, the Node viewport test, the chart supplement, Docker, the proofs against a new baseline of main with every difference read and named, the reviews, and the Studio check at the five widths.

## The test games made so far

In `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`, untracked, all on board 3 but the first. Their generator scripts were in the last session's scratchpad and are gone; the standing rules give the recipe.

`cd-guards-5-rtph` (the user's game at 729 events); `p35-rout`, `p35-repulse`, `p35-levels`; `p35-interdict-squad`, `-leader`, `-wreck`, `-fanatic`, `-trap`, `-commissar`, `-between`, `-smoke`; `p35-rally-fanatic`, `-plain`, `-enemy-turn`, `-take`, `-decline`; `p35-wound`, `p35-wound-again`, `p35-harden`, `p35-berserk-deploy`, `p35-seen-viewer`, `p35-hidden-viewer`, `p35-smoke-hill`. Several are left as played.

## Start

Show `git status` and `git log -3 --oneline`, confirm the branch and that the tree is clean but for `src/ASL/boards`, put the five unanswered questions to the user, and begin increment 2d with 35.12, the smallest, then 35.10 and 35.11.
