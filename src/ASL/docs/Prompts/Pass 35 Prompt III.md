# Pass 35, third session: 35.16, the panel fix, the owed tests, the documents, then the gate

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, branch `feature/asl-backlog-pass-35`, local and not pushed, 27 commits past main (f3b7c47, pushed); the last commit is this prompt with the standing rules' lessons, on top of 9ebde38. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its lessons of 2026-10-09 were learned in this pass, the last eight in its second session. Then read the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 35 Design.md`: sections 1 to 6 are the frame, 7 to 12 the six increments built.

## How this pass runs, at the user's word of 2026-10-09

1. **One task at a time, built as it is designed.** Read the rule on its page of the registered PDF, read the code, write the repair in Rules (Play hands facts over), build MapStudio with warnings as errors.
2. **A small test game for each fix, checked in the Studio, before the report.** The game is not skipped for cost: "no game" is for what cannot be staged, and any other exception is the user's to give. Twice in the second session the user had to ask for this.
3. **xUnit tests with each fix:** a Rules test of the verdict, and a Play test where the planner writes events the Rules test cannot see. Run only the test classes the change touches. Whole projects run at the gate.
4. **Report, then stop for the user's word before each commit.** One commit a task (35.13 ran in six groups, a commit each, at the user's word). Do not push.
5. **Search the whole rulebook before saying what a counter or a rule needs.** The user's ruling is that all 716 pages are used. In the second session an answer was given from one section, and the user's own PDF search showed the rest.
6. **Be sparing:** narrow reads, one targeted value from the page, no test run that proves nothing new.

## Where the second session stopped

| Increment | Tasks | Commits |
|---|---|---|
| 2a to 2c | The Rout Phase, Infantry, terrain | The first session (through 34f2c3e) |
| 2d, ordnance | 35.12, 35.10, 35.11 | 09a9251, 8b6a9b3, 6d68c22 |
| 2e, vehicles | 35.13 a; b and h; c and d; f and g; e, i, and j; the ESB table | caa9fe4, 2b7cc64, 9fe3102, e0dd3f2, 43116fb, f6ecc93 |
| 2f, night and weather | 35.14 | 7e63039 |
| 2g | 35.15 only | 37124f3 |
| The backlog's section 55.1 | The counters with no rules | 9ebde38 |

**Every question put to the user is answered:** section 7.5 question 6 and section 8.4 questions 1 to 4 (yes to each proposal); the hexes-passed-through reading of "touches the Bypassed hexside" (D9.4); the Reverse reading of E3.9 (weather and night costs are added after the multiplier); the refusal of 35.15 kept as a blunt stopgap, with its kinds named.

## What is left

**Increment 2g, the rest:**

- **35.16:** the seven tests not written (three of pass 31c: a tag through Deployment and Recombination, the Melee mark by view, a kept rate of fire; four page tests of pass 31d: the setup map's tooltips, "Since you last looked" after the view acts, a blocked LOS confirmed and its record, the Close Combat due list where the package refuses the Ambush), and a sweep test of Play for what a side may not read. The week review (`src/ASL/docs/ASL Week Review 2026-09-28 to 2026-10-04.md`) has their wording.
- **The vehicle Close Combat panel's stale selection** (found in `p35-rst-cc`): the panel keeps its checked defender, and perhaps its vehicle, from one Location to the next, and the next attack is refused as "Attack outside" until the page is reloaded. The state is `vehicleCcDefenders` and its neighbours in `Play.razor`; the panel is `Components/Play/VehicleCloseCombatPanel.razor`. Likely a reset after a commit. A page test and a Studio game with two Locations.
- **The terrain increment's two owed tests:** out-of-season grain on a LOS, and HE into a marsh (the design's section 9, question 2).
- **The design's section 13** for increment 2g.

**Then the design's sections 14 to 18,** still to be written: recorded games and versions; the proofs and how a difference is read; the reviews; the Studio check; the estimate against the plan's 6:45.

**To carry to the gate, found in the second session:**

- Task 35.11 decides the loss of an Acquisition in the projector, so an older recorded game may replay with a counter dropped earlier, and a later recorded shot that used its DRM may not reproduce. The replay over the store has not been run. Section 14 must say how such a difference is read.
- The refusal of 35.15 stops every action of a game that holds a fortification, rubble, or Flame counter. The 205 live games and the 14 cards were searched and hold none; `units/games/a1-village.synthetic.game.json` holds a foxhole and is only read and replayed.
- A tank freed by its Bog Removal is treated as Non-Stopped from then on. No rule read says so outright (the design's section 11).

**The documents, at the pass's end:**

- Rulings: R13.3 (Open Ground by the enemy's FFMO; a unit bound to surrender owes no rout; either side may end the RtPh; a repulsed unit may surrender), R30.3 (the berserk Fanatic's 11 withdrawn), R10.1 (HE into marsh), R10.10 (who forces a mover's loss of "?"), a ruling that a broken unit not under DM may not rout voluntarily, a ruling for the Reverse reading of E3.9 with D2.21 and E1.52, a ruling for "touches the Bypassed hexside" (D9.4), and one for the refusal of 35.15.
- The inventory's rows of every task, with A.8 moved from pass 40 to this pass and B14.2 from this pass to pass 135; D2.5's row; the Wire clauses seen in D7.211 and D8.21.
- The coverage document's sections, its two notes on counters placed and ignored among them.
- The plan's status.
- Backlog rows, under section 55, for everything each design section lists as left out (sections 10, 11, and 12 each end with such a list), with those of the first session (A25.211; the LLMC after a mortal wound outside fire; the TI rules for the SW and Deployment actions; Play tests of the Mopping Up and PF wound paths; the rest of pass 32's section 12 for passes 40, 45, 50, 65, and 75; the Morale ceiling's Studio game), and the mixed Infantry-and-vehicle hit that blocks an AFV's in-hex Hindrance (rulings R25.10, R7.2).
- The standing rules gained their lessons in the second session; add any of the third.

**The gate,** only when the user says the build is done: the whole Rules and Play projects (not run whole since the Infantry and terrain changes), MapStudio's page tests (three classes were run once, for the unload change; `Play.razor` changed), the solution build with warnings as errors, the Node viewport test, the chart supplement, Docker, the proofs against a new baseline of main with every difference read and named, the reviews, and the Studio check at the five widths.

## The test games and the tools

In `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`, untracked, all on board 3 unless said. Second session: `p35-tow2`; `p35-ordnance`, `p35-own-hex2`, `p35-own-hex3`; `p35-acquire`; `p35-bu-road`; `p35-bypass`; `p35-rst-ovr`, `p35-rst-cc`; `p35-bog`, `p35-bog2`, `p35-recall-stop`, `p35-bog-recall`; `p35-bff-bog`, `p35-cc-counter`, `p35-bog-mired`; `p35-esb-german`, `p35-esb-russian`; `p35-day`, `p35-night`, `p35-night0`, `p35-mud`, `p35-dry`; `p35-wire`. Several are left as played. The design's sections 10 to 12 say what each shows.

In `E:\Archive\GitHub\dlandi\pass32-tools\pass35\`: `counter_scan.py` (the whole-PDF scan against the inventory) and, under `session2\`, two of the game generators and the scan's result. Most generators were written inline and are gone; the standing rules give the recipe.

## Start

Show `git status` and `git log -3 --oneline`, confirm the branch and that the tree is clean but for `src/ASL/boards`, check that no Studio of the last session still runs on 6671, and begin with the vehicle Close Combat panel's stale selection (the smallest), then 35.16, then the two owed tests. Stop for the user after each.
