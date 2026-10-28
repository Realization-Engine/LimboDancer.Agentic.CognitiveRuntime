# Increment 5c: the SSR tokens, A19.132, and A19.32 (72.2, part I)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for the increments built, and `frame_substitution.md` (sections 1, 3, 4, 5 are your code map). Read `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP log --oneline -4` first.

## Pages
A19.1 to A19.132 and A19.3 to A19.32 (p. 86), A.11 (p. 43), A9.7 (p. 65), A25 on Finnish and on Axis Minor ELR if a catalog unit reads it.

## Build (rulings R72.9, R72.10, R72.13, R72.14)
1. **The tokens (R72.10).** On the pattern of pass 70's `fanatic:` (`ScenarioA1GameStart.FanaticToken`, the card's `RuleDiagnostics`, `ScenarioA1NightAndWeather.RulesBar`): `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>` with n 0 to 4. Rules parses them; a malformed one is refused on the card and for a new game. A Rules member answers, from the special rules, whether a side has Ammunition Shortage and what SSR ELR (if any) a side's (or group's) underscored units have. 5d builds what Ammunition Shortage does; here only the token and its reader.
2. **A19.132 (R72.13).** A fire target, and an FPF firer, gain an optional `underscoredElr` fact (null when no token). `ElrOf` (ScenarioA1FireCalculator near 4695) returns it for an underscored MMC in place of R15.9's 5; when set, `Replace` takes the normal `ReplacementOf` path. Add the table entry: the German 8-3-8 is Replaced by the 4-3-6 Conscript (R72.13); say in the report what the other underscored units (German 3-3-8 and the elite 2-4-8 HS, NKVD 6-2-8 and 3-2-8, Finnish 8-3-8 and 3-3-8) become, and keep the Finnish as R15.13 has it. Any other place an ELR is handed to Rules for a MC outside fire (pass 70's `ScenarioA1MoraleCheck`, Rally) gets the same fact if it reads `ElrOf` or R15.9: grep for it.
3. **A19.32 (R72.9).** A pass-72 stamp on the fire attack (pattern: pass 45's `CoweringMarksAndWeaponResidual`, set where the night-and-weather facts are). With the stamp, any SW used by Inexperienced Personnel has its B# (and X#) one lower, a MG included; Inexperienced is the firer's `Inexperienced` fact (A19.3: a Green MMC stacked with an unbroken leader is not), or Conscript class where the fact is absent. The ATR's existing -1 takes the same read under the stamp. Without the stamp, today's R9.10 stays, so `dl-tractor-works-01` revision 1267 (a Conscript LMG, B11, Original DR 10) still verifies. The ordnance package's class-only read is left (backlog).
4. **A19.31 (R72.14):** verify only. If you touch `Experience.Inexperienced`, read through the index, not `state.At` per unit.

## Not in 5c
Ammunition Shortage's effects (5d).

## Tests by method
Rules: each token parsed, a malformed one refused (card and new game); `ElrOf` with and without `underscoredElr`; the 8-3-8 Replaced by the 4-3-6 at an SSR ELR of 3 and failing by 4; without the token, two HS as before; a Conscript's MG B# one lower with the stamp, unchanged without; a Green squad with a leader keeps the B#. Play: a card with `underscored-elr:` replays a failed MC into a 4-3-6; an Inexperienced MG malfunctions at its lowered B#. Grep and run by method the R15.9, R9.10, and `ScenarioA1Pass9bTests.InexperiencedHandsLowerTheBreakdownAndXNumberByOne` tests and the fanatic token tests.

## Studio games
`games_5c.py`, board 3: a card or game with `underscored-elr:german:3`, a German 8-3-8 under Russian fire (dice for a failed MC beyond the ELR); a Russian Conscript squad firing a LMG (dice for an Original DR equal to the lowered B#).
