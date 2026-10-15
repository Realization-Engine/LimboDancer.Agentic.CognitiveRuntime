# Pass 60, second session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-15, 15:50

- **Pass 60 (Armored combat III) is built to the merge gate** on `feature/asl-backlog-pass-60`, cut from main at 380b4a7b (the Goodwood merge), the documents its last commit. Not pushed. The design (`Passes/ASL Unit Backlog Pass 60 Design.md`) has the frame, each task as built (sections 7 to 12), the reviews and the Studio check (13), as built (14), and what the gate should show (15); the review is `Passes/Scenario A1 Backlog Pass 60 Review 2026-10-15.md`; the pass's scripts, briefs, and fix lists are in `code-builders/pass-history/pass-60-armored-combat-iii`.
- **The strict build passed for the tree** (`gate.sh 60 380b4a7b strict`, its second run; `strict-check` said so at 7c2be79f, and the documents commit changed no code). The local replay digest of the 504 older games equalled the baseline in all 20,557 lines after each of the six tasks and after the reviews' fixes; the pass's own games replayed to their end each time.
- **The `base` stage ran at the branch's cut** (baseline 2b82cee3, 504 older games). `pass60.conf` now names the pass's 23 recorded games, and the baseline's key holds that list, so the `baseline` stage will make the older games' baseline again once (about eight minutes) before its own part. `p60-random-sw` is left out: it recorded a fact the reviews' fixes no longer record, and is labelled SUPERSEDED by `p60-random-sw2`.
- **main** was 380b4a7b when the branch was cut and was not fetched again after; a Codex session commits to it.
- **No Studio of mine is running.**

## The user's word

On 2026-10-15 the user said: "this is my word to start pass 60, run without my intervention up to the gate." That word is used up at the gate. The gate's stages (`baseline`, `build`, `proofs`), the push of the branch and its pull request, CI's run of it, the merge, and any closing commit are each the user's to start by name. Under the rule of 2026-10-13, once the user has said to push the branch, its pull request is opened without a further word.

Also on 2026-10-15 the user ruled that the Studio check of the whole pass at five widths is scoped case by case (standing rules, section 1, step 4); pass 60 ran the narrower check the user approved.

## Open to a veto

- **Rulings R60.1 to R60.27** (the Passes Plan's section 5), decided under the user's word. Three of the frame's were corrected on the page during the pass: R60.10 by R60.23, R60.11 by R60.24, R60.18 by R60.26.
- **The documents agent's own placements:** the passes it chose for leftovers the design sent to the backlog without naming one (listed in the review and the backlog's section 60), and its status calls on eleven inventory rows now "built".
- **Rulings R45.3 to R45.5,** still open from pass 45.

## Known before the gate

- The proofs' text list will differ in the pass's new texts and changed refusal sentences; the sweep may show refusals turning into plans in older games where a pass 60 refusal stood (design, section 15). Each such line is read as a task's intended change or it is a fault.
- The time log's work of 2026-10-13 after pass 55's closing is recorded as not timed.

## Start

Read the standing rules and the design's sections 13 to 15. Check the state above against the repository (fetch main first), and wait for the user's word.
