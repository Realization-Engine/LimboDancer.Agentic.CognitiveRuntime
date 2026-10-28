# Review R3: the table player (head db15561b)

The read-only reviewer's findings, condensed in layout only. Of the 14 `p72-*` games in the live folder, six hold played events (ammo-shortage-replacement, berserk-vehicle-cc-return, hero-atr, hero-mortar, hero-psk, inexperienced-mg); the other eight stop at their setup (rewritten by their scripts after the Studio check), so their results were read from the code and the notes.

1. MAJOR. `PlayRecords.cs:501, 512-513`, `BerserkClearedBy` (~640), `ReplaySteps.cs:525, 530`: once the Battle Hardening choice is answered no line says what happened (berserk off, the Replacement or Fanatic); the record reads "A choice the game waited for was answered"; Replay "A choice waits", "A choice is answered". Fix: word a `ChoiceMade` of key `berserkHardening:` through `HardeningText`, naming the new unit or the Fanatic status; name the `battle-hardening` kind in ReplaySteps.
2. MAJOR. `ScenarioA1FireCalculator.cs:4304, 4249`: the Shortage lowers the X# too (A.11's example: B10 and X12); `p72-ammo-shortage-mmg`'s DR 10 removes the MMG where the page malfunctions it. Fix: the X# at the Original B#; lower a printed X# only (FT, BAZ). (R2's finding 1.)
3. MINOR. `FireText.cs:162`: a weapon removed under the Shortage shows neither its number nor the Shortage ("the DR reached its X# (A.11, A9.7)"). Fix: "X10 (B11, less 1 for Ammunition Shortage, A19.131)".
4. MINOR. `ScenarioA1OrdnanceCalculator.cs:169`: the PSK's +1 cites C13.45; the +1 is A15.23's. Fix: cite A15.23.
5. MINOR. `UnreachableText` (296), `HardeningDueText` (321), `ChargeUndecidedLine`: the texts name only cliffs and Water Obstacles though `RuleBar` counts unconnected building Locations; the panel line gives the hex and "stays in place" twice; "no ... but across" reads ambiguously. Fix: "across terrain a berserk unit will not charge through" or the bar met; drop the repeats; "except across".
6. MINOR. `GamePlanner.Choices.cs:116`, `HardeningChoiceText` (326): the choice names the hex, not the unit. Fix: "[A8]: 4-4-7 squad R1".
7. MINOR (against R72.3). `ScenarioA1ChargeCalculator.cs:263` (`<=`): A15.45's "still closer than the next nearest" excludes a tie. Fix: `<`, amend R72.3.
8. MINOR. `ScenarioA1ChargeCalculator.cs:291`: the "no route" line names only the nearest target though every group was searched, and cites A15.431 for a model gap. Fix: "to no Known enemy unit in its LOS (the nearest [A3])", cite R30.5.
9. NOTE. `ScenarioA1SequenceCalculator.cs:362`: "by its TPBF or FT" where the record knows which.
10. NOTE. `FireText.ShortageLines`: only the chosen unit's dr is shown. Fix: every candidate's dr.
11. NOTE (boundary). `GamePlanner.Fire.cs:1253`: Play decides a firer Replaced under the Shortage does not return to normal. Fix: the Replaced ids as a fact on `BerserkAttackFacts`, decided in Rules.

Common answers: item 11 the one rule outside Rules; no absent-safety fault; nothing names or counts a unit its viewer may not know.
