# Scenario A1 Backlog Pass 15: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised and republished with their prior manifest digests kept, on catalog 1.10.0. They have no execution authority. Backlog pass 15 (special units and nationalities).

**Date:** 2026-09-29

**Plan:** [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 15), and 5 (rulings R15.1 to R15.14). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 15 Design](<ASL Unit Backlog Pass 15 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

FT (A22.1 to A22.5), DC Placed and Thrown (A23.1 to A23.63), MOL (A22.6 to A22.62), Snipers (A14.1 to A14.3), Commissars (A25.22 to A25.224), NKVD Field Promotion (A25.25), Allied Troops' leadership (A10.7), underscored Morale Factors (A1.23, A19.13), Green MMC (A19.2, A19.3), a hero's SW and a hero created concealed (A15.21, A15.23, A15.24), the American, British, Italian, Finnish, and French units with their national rules (A15.1, A18.2, A25.4 to A25.74), and a berserk unit's kept SW (A15.431).

## Sources

The rule text of each new subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass15-pdf-comparison.json`, SHA-256 `3cccc3cd...97da`, 46 Chapter A fragments: A1.23 (p. 44), A14.1 to A14.3 (p. 82), A19.3 and A19.32 (p. 86), A22.1 to A22.6111 (p. 89), A23.1 to A23.4 (p. 90; A23.3 in two parts), A23.6 to A23.63 (p. 91), A25.22 to A25.224 (p. 94), A25.41, A25.45, A25.51 (p. 96), and A25.61 to A25.74 (p. 97). The referee asked for A1.23 and A25.45 to A25.74, which were added after the review stage. `ThePass15SubjectsAreVerified` checks the 46.

The new counters were read from 150 dpi renders of the A./G. National Capabilities Chart (p. 109, physical page 695): each nationality's MMC classes, FP, range, morale, broken morale, underscores, and BPV. The Finnish leader ranks come from A25.71. No page prints the portage of a FT or DC, or the Commissars' broken morale: those values are manufactured under R0.3 on sheet MFG and say so.

## Rulings

R15.1 to R15.14 are in the plan, section 5. The referee's findings changed R15.1, R15.2, R15.3, R15.5, R15.6, R15.9, and R15.13; the table player's changed R15.1, R15.2, and R15.3.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. An underscored Morale Factor was read as an exemption from the side's ELR; it gives the MMC an ELR of 5 | A1.23, A19.13 | Fixed: the Fire package uses 5 for such a MMC; R15.9 reworded; `AnUnderscoredHalfSquadIsDisruptedRatherThanReplaced` and the Play test fail it by six. |
| 2. The Finns have their own leader ranks, not the general grades | A25.71 | Fixed: catalog 1.10.0 has the Finnish 8+1, 8-0, 9-0, 9-1, 10-0, and 10-1, with their own Battle Hardening chain. |
| 3. The Finnish MMC classes were incomplete (a green 5-3-8 and a squared 1st Line 5-4-8) and followed the general Replacement chain | A25.72 | Fixed: both added; Replacement and Battle Hardening follow A25.72, and a Finnish 1st Line or elite MMC becomes Fanatic. |
| 4. The Italian classes lacked the line 3-4-6, and Italian Replacement followed the general chain | A25.61, A25.62 | Fixed: the line MMC added; the Italian chain is elite, 1st Line, line, conscript. |
| 5. A Placed DC could leave the AFPh unable to end | A23.3, A23.4 | Fixed: no Placement where any vehicle stands or where the target's TEM is not reviewed, and the AFPh waits only for a detonation the package can make. |
| 6. British elite and 1st Line units and Finns but Conscripts never Cower; a Finnish 1st Line user is elite for a FT or DC | A25.45, A25.7, A25.74 | Fixed; `BritishEliteAndFirstLineUnitsAndFinnsButConscriptsNeverCower`, `AFinnishFirstLineUserIsEliteForItsFlamethrowerAndAnInexperiencedUserLosesItOneSooner`. |
| 7. A Sniper that eliminates, wounds, or breaks a leader causes no LLMC | A14.3, A10.2 | Recorded: backlog section 25. |
| 8. An Inexperienced user's FT or DC malfunction number is one lower | A19.32 | Fixed: the fire record carries the user's Inexperience; tested. |
| 9. A DC Thrown or Placed to another level, or across a hexside TEM | A23.3, A23.6 | Recorded: R15.3 now says same level only; backlog section 25. |
| 10. Sniper choices, Interior Building Locations, and the enemy Sniper counter as a target were claimed in R15.5 but not built | A14.2, A14.21, A14.31 | R15.5 aligned with what is built; the rest recorded in the backlog, section 25. |
| 11. The second DR of a Thrown DC, at its thrower's Location, could malfunction it | A23.4 EXC | Fixed: only the first DR can; R15.3 reworded; `AThrownDcsSecondDrAtItsThrowerNeverMalfunctionsAndMayBeDefensiveFirstFire`. |
| 12. A Placed DC is halved only when every target was concealed at Placement; per-target concealment is not read | A23.3 | Recorded: backlog section 25. |
| 13. R15.6 did not say that a TC causes a LLTC, and a broken Commissar's duty to Self-Rally first is not enforced | A25.221, A25.222 | R15.6 reworded; the Self-Rally order recorded in the backlog. |
| 14. An Encircled DC user's DRM is not applied | A7.7, A23.2 | Recorded: backlog section 25. |
| 15. The backlog rows for what the pass leaves out were incomplete | | Fixed: section 25 lists every item above. |
| 16. The comparison lacked the rules the national findings rest on | A1.23, A25.45 to A25.74 | Fixed: ten more fragments compared (46 in all) and cited by the Fire package. |

The referee found R15.4 (MOL), R15.7 (the NKVD table), R15.8 (Allied Troops), R15.10 (Green MMC), R15.11 and R15.12 (heroes), R15.14 (a berserk unit's SW), the Commissar's morale and rally rules, the Heat of Battle and Leader Creation drm of the new nationalities, and the Italian surrender sound.

## Table player findings

The table player played 34 situations in a local clone in the scratchpad (the `.git/worktrees` folder is still held): FT fire in each phase, at Long Range, by a HS and a hero; one FT or DC per Player Turn; Thrown DC in the PFPh, DFPh, and as Defensive First Fire; MOL in First and Final Fire, a colored 6, a failed HS Check; Placed DC with the placer broken, eliminated, or leaving, and in empty, friendly, and vehicle Locations; Sniper attacks among several targets, a concealed stack, a Dummy stack, a side with no counter, and a MC DR calling a Sniper; Commissar rallies, berserk spread; Allied leadership; Italian surrender; a Finnish rally and Self-Rally; and a berserk squad's kept SW. The 33 situations kept as tests are `BacklogPass15TablePlayerTests`.

| Finding | Rules | Disposition |
|---|---|---|
| 1. A DC Thrown as Defensive First Fire crashed: its second attack, at the thrower's Location, had no fire kind the Fire package accepted | A23.6 | Fixed: the package admits a DC's attack on its thrower's Location in the MPh; `AThrownDcAsDefensiveFirstFire`. |
| 2. A Placed DC in a Location holding only a friendly vehicle froze the AFPh | A23.4, A23.5 | Fixed: no DC is Placed where a vehicle stands; `NoDcIsPlacedInALocationWithAFriendlyTank`. A vehicle entering later is in the backlog. |
| 3. A unit could use a FT twice, or a DC and then a FT, in one Player Turn | A22.3 | Fixed: the game records FT and DC users for the Player Turn; `AFlamethrowerIsUsedOncePerPlayerTurn`, `InherentFpThenADcThenAFlamethrower`. |
| 4. A squad that fired only its FT in the PFPh could fire its inherent FP in the AFPh | A3.3, A7.1 | Fixed: a unit whose SW Prep Fired alone is refused in the AFPh; `AFlamethrowerUserInThePfphDoesNotFireInTheAfph`. |
| 5. A squad that Threw a DC could not fire its inherent FP in the same phase | A23.2 | Fixed: a Thrown DC is the squad's one SW use of the phase; `AThrownDcInTheDfphAndTheSquadsInherentFp`, `ADcThenInherentFpInThePfph`. |
| 6. A Sniper never attacks the enemy Sniper counter | A14.21, A14.31 | Recorded: R15.5 no longer claims it; backlog section 25. |
| 7. A Finnish squad's Self-Rally was refused | A25.7 | Fixed with the catalog: Finns but Conscripts have Self-Rally capability; `AFinnishRallyOriginalTwo`. |
| 8. A hero's FT needed `withoutInherent`; pass-fire with no mover left and the end-move texts after a placer broke were unclear | A22.3, A8.1 | The FT fixed: a firer naming a FT fires it apart from its inherent FP; the texts recorded in the backlog. |

## Visual check

In the Studio (`map-studio-scripted`, game `pass15-demo`, driven by DOM events): a German squad fired its FT at an adjacent Russian squad from the fire panel, and the record read 24 FP with no TEM or leadership DRM; a squad Threw a DC from the Demolition Charges panel, and two fire records followed, the target Location's with +2 and the thrower's with +3. The Snipers list and the move panel's DC Placement and berserk "keep" inputs showed.
