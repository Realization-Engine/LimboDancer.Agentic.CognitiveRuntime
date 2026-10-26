# The reviews of pass 71

Five read-only reviews of the branch `feature/asl-backlog-pass-71` against its base 0b5b9a1d (main as merged into the branch at 00fd8894; the pass's own commits are those after 5379e06f other than the merge), run in parallel in the main checkout.

**What a reviewer may do.** Each reviewer reads, and never edits, stages, builds, or runs tests.
- It may run `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff 0b5b9a1d..HEAD -- <paths>` (the diff against main's head leaves out Codex's acronym files, which are not the pass's).
- It may read files and the rulebook's page texts (`E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`, one physical page a file).
- Never `cd` into the repository. Never `grep -r` over src/ASL in Bash: use the Grep tool, pointed at `src/ASL/LimboDancer.*` folders and `src/ASL/tests`, one at a time. **Never search or open anything under `src/ASL/boards` except the `p71-*.game.json` files the table player reads;** a `glob` such as `!boards/**` does not exclude the folder. Never touch any `dl-tractor-works-*` file.

**The report** is the closing message, under 1,800 words: findings numbered, each with
- its grade (BLOCKER, MAJOR, MINOR, NOTE),
- the file and member, with a line,
- the rule and its page,
- what is wrong,
- and a proposed fix in one or two sentences.

**Read first:**
- the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 71 Design.md`, sections 2 to 7. The rulings R71.n bind the build; a finding against a ruling says so and argues from the page;
- the session's notes, `code-builders/pass-history/pass-71-snipers-wounds/notes.md`, which list what each increment decided and what is already found for the fixes (do not report a known item again unless it is wrong);
- the code map, `frame_wounds_codemap.md` sections 5 to 7 beside it.

**Every reviewer also answers:**
- Is any rule decided outside `LimboDancer.Domains.Asl.Rules` in what you read?
- Does any fact added to a record change what an older record replays to (absent-safety)?
- Does any text or event a side reads (a proposal, a refusal, a record line, a choice, a list, a panel, a map layer, an event's payload with visibility all) name or count a unit it may not know: a concealed or hidden unit, a Dummy's nature, a withheld check?

**Known, to be fixed and not reported again:** the Sniper lines name the counter and the trigger by raw ids ("(gs)", "the SAN roll fire-...-roll-1"); a choice's text ends with "(ruling R5.8)" after its own citation; the Sniper line says the hex twice ("[L4]: 4-4-7 squad in [L4]"); some choice texts name a side in lower case ("the german Sniper"); 4a's pin of a concealed unit that keeps its "?" writes a public `conditions-changed` naming it; the setup list labels a Sniper counter "a Dummy"; the hand placement's refusal says "choose a hex the setup offers"; a trigger written before the same step's own pending choice can be lost; a HS created in the same step finds no maker for its check DR.

## R1. Referee: the Sniper's rules (task 71.1; increments 4a to 4e)

**Rules to read on their pages, with their examples:** A14 to A14.4 with A14.4's EX (pp. 82, 83); A12.14 and its exception (pp. 77, 78); A12.141 (p. 78); D5.311, D5.34, D5.341, D5.342, D5.343 (pp. 203, 204); E1.76 (p. 225); A15.21 (heroes, for the Sniper Check DRM); A17.3; A24 as far as SMOKE in A14.21's comparison needs.

**Code:** `ScenarioA1Sniper`, `GamePlanner.Snipers.cs`, `GamePlanner.SniperPlacement.cs`, the Sniper parts of `GamePlanner.cs`, `GamePlanner.Choices.cs`, `GamePlanner.CardSetup.cs`, `GameGate`, the projector's Sniper, SAN, and Sniper Check members.

**Look hardest at:**
- the targets: who is Vulnerable (A14.22), the crew, the unarmored vehicle, the enemy Sniper; the result of each dr against each;
- the SAN: when it falls, when the counter leaves, night's floor;
- the Sniper Check's eligibility ("has not yet fired/moved during that Player Turn") and DRM against A14.4's EX;
- the placement (A14.2: "within six hexes of >= six enemy-occupied hexes ... if possible", DEFENDER first);
- the five choices: is each offered exactly when the rule gives the Sniper player a choice, and does a forfeit's repositioning follow the placement rule;
- the triggers (A14.1): every DR kind in the four phases, its maker, the prisoner exception, the order of attacks within a step and across a pause.

## R2. Referee: hidden information (every increment)

Read every change **from each side's view**, as the side that must not see. Every event the pass writes: its `visibility`, and what its payload names. Every choice's options and resume. The Sniper attack on a concealed stack (A14.23), a target that keeps its "?" (R71.8), the Sniper Check's participants under "?", the placement set (counted from the placing side's view), the trigger roll id on a public record (4e's note), the result string of an attack on a concealed vehicle (4a's note), and the A12.14 LOS read at night (4a's note).

## R3. Referee: wounds and Field Promotions (task 71.2; increments 4f, 4g if committed when you start)

**Rules to read on their pages:** A17, A17.1, A17.11, A17.2, A17.3 (p. 85); A18.2 and its table (p. 85); A4.4 to A4.43 on portage; A4.11 (p. 48); D5.34, D5.343 (pp. 203, 204); A11.5 (CC against a vehicle).

**Look hardest at:** the pin of a man wounded after more than 3 MF in every path that wounds in the MPh; the SMC crew's Stun wound and its Severity dr in the projector, and an older record without the dr; the -1 against an AFV in Leader Creation; a carried wounded man (if built): his portage, his MF, when he is set down.

## R4. Table player

Play the pass's games by reading their records, as a player at the table would.
- **The games:** `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/p71-*.game.json` (read the events; never touch a `dl-tractor-works-*` file).
- **The record lines** the Studio writes for them (`Services/PlayRecords.cs`, `Services/ReplaySteps.cs`, and the texts in Rules they read), the choice texts (`ChoiceText`, `SniperCheckChoiceText`, `ChoiceOptionText`, `SniperOptionText`), the setup block's texts.

Ask: would a player understand what happened and why, from what each side's view shows? Is a refusal's advice right? Is a result the rule's, on its page? Is anything shown to the side that should not see it? Name the game, the event, and the text.

## R5. UI, the Rules boundary, and memory and performance

**What to read:** `git diff 0b5b9a1d..HEAD -- src/ASL/LimboDancer.Domains.Asl.MapStudio src/ASL/LimboDancer.Domains.Asl.Play src/ASL/LimboDancer.Domains.Asl.Units`.

**Check:**
- **The hex UI rule:** no choice names a counter without its hex, hex first (the Sniper's choice options, the setup block's hexes, the long-list dropdown `#play-choice-select`).
- `@key` on lists that shift; state cleared on a change of game or view; a disabled control's `title` as its reason; the narrow layout under 1024 pixels.
- Every decision of a rule made outside Rules; every fact a verifier recomputes from the state that changes an older record's result; every new JSON property not `JsonIgnore` when null or default.
- Against `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`: every `GameState.At`, `Aboard`, `Location`, `Find`, `Unit`, or `Units` scan inside a loop or per render the pass added; every `Replay` call in a plan where the state is in hand; the new memo stores (`Snipers`, `SniperPlacements`) with their key, lifetime, and fill; 4e's wrapper of every ready plan (its cost when no Sniper is on the map).

List each finding with its member and line, and where it belongs.
