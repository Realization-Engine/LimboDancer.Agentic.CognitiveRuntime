# Pass 72 session notes

A running record kept as the pass goes, as pass 71's was. Times are read from the clock. The design holds the decisions; this file holds what was done, in order, and why.

## The user's word

2026-10-27, about 18:52 (after this session prepared the pass and asked how it is to run): "Run Pass 72 without my intervention up to the merge gate."

- Pass 72 runs without the user's intervention, up to the merge gate. The gate (`baseline`, `build`, `proofs`), the push of the pass's branch, its pull request, and the merge are each the user's to start. `base` runs when the branch is cut, `strict` before the documents (the standing rules, section 1).
- Questions go to read-only sub-agents; each decision is a numbered ruling R72.n, open to the user's veto.
- Unit tests (the user, 2026-10-23): only those pertaining to the pass's new code, by method; the full run at the gate.

## The state checked against the repository (18:53)

- **Main is at b0a36132**, as the prompt says; `git fetch` gives origin/main b0a36132. Main's last ASL Authoring CI run (40407647914) passed; the nightly Campaign run (40499073982) passed.
- **The working tree** was clean on main. No Studio listens on 5178, 6670, or 6671. `outside_outputs.sh list`: nothing registered is left.
- **The live folder** held 42 `p70-*` games, 14 `p71-*`, and the three `dl-tractor-works` games; the store 41 `p70-*`, 14 `p71-*`, and `dl-tractor-works-01` and `-02` with `-02`'s cuts.

## Timeline

- 18:53 Prompt I, the standing rules (sections 0 to 3), and `code-builders/README.md` read. Pass 71's notes, frame brief, configuration, and design (sections 1 to 5) read for form.
- 18:53 Branch `feature/asl-backlog-pass-72` cut from b0a36132. **Pass 70's games retired** (the standing rule of 2026-10-25): 41 `p70-*` of the live folder and 41 of the store moved (not deleted) to `E:/Archive/GitHub/dlandi/asl-retired-2026-10-27/live` and `/store` by `retire_p70.sh`; `p70-perf-tw` stays in the live folder as the measurement copy. `pass72.conf` written (OWN empty; BOTH_SKIP `GameEvent.RulePackage`; PRESENT `SniperCheck`, pass 71's; ABSENT `AmmunitionShortage`, which no source holds).
- 18:54 `gate.sh 72 b0a36132 base` started in the background (log `asl-gate-work/base-72.log`).
- 18:54 Read on their pages myself: A15.2 to A15.24 (p. 83), A15.42 to A15.5 (p. 84), A19 to A19.36 (p. 86).
- 18:55 Three read-only frame agents launched on `brief_frame.md`: (A) the berserk charge and its return to normal, with the moved-in vehicle-and-Infantry Location; (B) the hero's weapons (A15.23); (C) Unit Substitution (A19.131 to A19.32).
- 19:00 `base` done (18:54:02 to 19:00:23, 6:21; SDK 10.0.403): baseline 7093ebc71392bd09 of b0a36132, 102 older games in the store (the swept game's cuts, `dl-tractor-works-01` and `-02`, pass 71's 14 games); text list 1,591 distinct texts (2,176 occurrences), replay digest 3,137 lines in 28 s, sweep 51,514 lines in 126 s.
