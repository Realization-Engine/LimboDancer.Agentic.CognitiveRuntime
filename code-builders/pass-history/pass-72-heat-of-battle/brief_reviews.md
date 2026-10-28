# The reviews of pass 72

Four read-only reviews of the branch `feature/asl-backlog-pass-72` against its base b0a36132, run in parallel in the main checkout.

**What a reviewer may do.** Each reviewer reads, and never edits, stages, builds, or runs tests.
- It may run `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff b0a36132..HEAD -- <paths>`.
- It may read files and the rulebook's page texts (`E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`, one physical page a file).
- Never `cd` into the repository. Never `grep -r` over src/ASL in Bash: use the Grep tool, pointed at `src/ASL/LimboDancer.*` folders and `src/ASL/tests`, one at a time. **Never search or open anything under `src/ASL/boards` except the `p72-*.game.json` files the table player reads;** a Grep with a path of `src/ASL`, or a `glob` such as `!boards/**`, reaches the user's live games. Never touch any `dl-tractor-works-*` file.

**The report** is the closing message, under 1,800 words: findings numbered, each with
- its grade (BLOCKER, MAJOR, MINOR, NOTE),
- the file and member, with a line,
- the rule and its page,
- what is wrong,
- and a proposed fix in one or two sentences.

**Read first:**
- the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 72 Design.md`, sections 2 to 7 and the increments' sections (8 onward). The rulings R72.n bind the build; a finding against a ruling says so and argues from the page;
- the session's notes, `code-builders/pass-history/pass-72-heat-of-battle/notes.md`, which list what each increment decided and what is already found for the fixes (do not report a known item again unless it is wrong);
- the frame reports beside them, `frame_charge.md`, `frame_heroes.md`, `frame_substitution.md` (code maps).

**Every reviewer also answers:**
- Is any rule decided outside `LimboDancer.Domains.Asl.Rules` in what you read?
- Does any fact added to a record change what an older record replays to (absent-safety)? The pass's guards: the stamp `unitSubstitution` on a fire attack, and optional facts (`vehicleSides`, `underscoredElr`, `ammunitionShortage`, `shortageSelection`, `shortageEffects`).
- Does any text or event a side reads (a proposal, a refusal, a record line, a choice, a list, a panel, an event's payload with visibility all) name or count a unit it may not know?

**Known, to be fixed and not reported again:** the Inexperienced MG's malfunction line cites A9.7 and not A19.32; no page test asserts the undecided-charge line.

## R1. Referee: Heat of Battle (task 72.1; increments 5a, 5b, 5e)

**Rules to read on their pages, with their examples:** A15.2 to A15.24 (p. 83), A15.3 (p. 83), A15.4 to A15.46 with A15.44 (p. 84), footnote 21 (p. 102), the berserk example (p. 62), E1.533 (p. 224), A7.21 and A7.211 to A7.212 (TPBF; find the page), A22.1 (FT), A11.5 (CC against a vehicle), C13.21, C13.25, C13.45, C13.48 (pp. 183 to 184), A21.11 to A21.12 (p. 88).

**Code:** `ScenarioA1ChargeCalculator`, `ScenarioA1ChargeModels`, `GamePlanner.CloseCombat.cs` charge members, the MPh end in `GamePlanner.cs`, the Battle Hardening choice in `GamePlanner.Choices.cs`, `ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack`, `GamePlanner.Fire.cs` `BerserkReturnsAfterFire`, `GamePlanner.VehicleCloseCombat.cs` `BerserkReturnsAfterVehicleKill`, `ScenarioA1FireEligibility.SelectTargets` (`tpbfSide`), `LiveFire.FromState`, the CC calculator's A15.46 check (`vehicleSides`), and 5e's hero members in the fire and ordnance calculators (if 5e is committed when you start; `git log --oneline -8`).

**Look hardest at:**
- the charge: the range groups, A15.45's comparison as built (only where a rule bar refused a cheaper step), what counts as a rule bar and as a gap, A15.44's Battle Hardening at the MPh's end (timing, refusal, the elite unit's Fanaticism);
- **5b's `tpbfSide`:** every attack it now allows that was refused before. Where can a firer stand in its target Location with enemy units outside Melee (after a berserk charge, after an advance into a vehicle's Location, a Location shared with a vehicle, a CC counter "not yet held in Melee")? Is each such TPBF legal (A7.21, A7.211, A11.16 on fire into a Melee Location)? Is a unit in Melee kept out?
- A15.46: the return's conditions (TPBF halved or FT in the AFPh, CC, a vehicle destroyed in CC), a surviving vehicle, a concealed enemy left;
- the hero (if built): the -1 on the To Hit only, the PSK's +1 and -1, the ATR as Small Arms.

## R2. Referee: Unit Substitution (task 72.2; increments 5c, 5d)

**Rules to read on their pages:** A19 to A19.36 (p. 86), A.9 and A.11 (p. 43), A9.7 (p. 65), A9.22's note (p. 110), A21.11 (p. 88), A22 on a FT's B# or X# (find it), C8.9 or wherever the armor package's Depletion Numbers come from, A25 on Finnish and on Axis Minor ELR.

**Code:** `ScenarioA1UnitSubstitution`, `ScenarioA1FireCalculator` (`ElrOf`, `Replace`, `Inexperience`, `Shortage`, `ShortageReplacements`), `ScenarioA1FireReference` (`UnderscoredReplacementOf`, the 8-3-8 row), `ScenarioA1OrdnanceCalculator.Breakdown`, `ScenarioA1ArmorCalculator.Depletion`, `ScenarioA1FireEligibility` (the Fire Lane refusal), `LiveFire.cs` (the facts and the verifier), `GamePlanner.Fire.cs` (the shortage effects), `ScenarioCards.cs`.

**Look hardest at:**
- A19.131: who is Replaced (MMC using Inherent FP; crews; the underscored HS; FPF firers), Random Selection and its ties (R72.16), "incapable" and "unless berserk", the order "after malfunction or elimination of applicable SW", what is exempt (CC, Sniper, Residual, a To Hit 12, a DC, a captured SW);
- the X#: Ammunition Shortage's X# against the Inexperienced and captured reductions (A.11), and a FT under each (5c left the FT out of A19.32's -1; 5d gave its removal number the -1);
- A19.132: the SSR ELR's reach (underscored only, a group's token), the 8-3-8 to 4-3-6 row (R72.13), the 3-3-8 to 2-3-7 (R72.15), the Finnish;
- A19.32 behind the stamp: Inexperienced by fact or class, a Green with a leader.

## R3. Table player

Play the pass's games by reading their records, as a player at the table would.
- **The games:** `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/p72-*.game.json` (read the events; never touch a `dl-tractor-works-*` file).
- **The record lines** the Studio writes for them (`Services/PlayRecords.cs`, `Services/FireText.cs` or wherever the shortage lines are, `Services/ReplaySteps.cs`, and the texts in Rules they read), the Movement panel's charge lines (`Play.razor` `ChargeUndecidedLine`), the Battle Hardening choice text.

Ask: would a player understand what happened and why, from what each side's view shows? Is a refusal's advice right? Is a result the rule's, on its page? Is anything shown to the side that should not see it? Name the game, the event, and the text.

## R4. UI, the Rules boundary, and memory and performance

**What to read:** `git diff b0a36132..HEAD -- src/ASL/LimboDancer.Domains.Asl.MapStudio src/ASL/LimboDancer.Domains.Asl.Play src/ASL/LimboDancer.Domains.Asl.Units`.

**Check:**
- **The hex UI rule:** no choice names a counter without its hex, hex first (the Battle Hardening choice, the charge lines, the shortage lines, the hero's refusals).
- Every decision of a rule made outside Rules; every fact a verifier recomputes from the state that changes an older record's result (5d's ordnance shot fact is checked against the state with no stamp); every new JSON property not `JsonIgnore` when null or default.
- Against `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`: every `GameState.At`, `Aboard`, `Location`, `Find`, `Unit`, or `Units` scan inside a loop or per render the pass added (5a's MPh-end hardening list; `LiveCloseCombat`'s `vehicleSides`; 5b's Location reads; 5c's `LiveFire.UnderscoredElr` per target and firer; 5d's per-render scans of the special rules); the charge search's single Dijkstra and its memo key.

List each finding with its member and line, and where it belongs.
