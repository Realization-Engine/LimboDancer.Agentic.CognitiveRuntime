# Scenario A1 Backlog Pass 17: source and card review

**Status:** Reviewed. Two scenario cards adapted from legacy cards to the registered rulebook, registered as a source, and presented in the Studio; catalog 1.11.0 with six counters. No package changes its rules; the Fire, Rally, Close Combat, and Ordnance packages are re-pinned to the new catalog only. Backlog pass 17 (scenario cards, presentation only).

**Date:** 2026-09-29

**Plan:** [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 17), and 5 (rulings R17.1 to R17.11). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage, judging the cards as the Studio presents them.

**Design:** [ASL Unit Backlog Pass 17 Design](<ASL Unit Backlog Pass 17 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

The user's ruling of 2026-09-29: pass 17 builds only the card presentation, a card format and how the Studio shows a card. Setting up a game from a card, reinforcements entering during play, evaluating the Victory Conditions, and the full Chapter H DYO purchase are backlog (section 27).

## Sources

**Rules.** The rule text of each subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass17-pdf-comparison.json`, 22 fragments: A2.1 (p. 45), A3.9 (p. 47), A7.7 (p. 57, in two parts), A14.1 (p. 82), A16.1 (p. 84), A19.1 (p. 86), A20.53 (p. 87), A24.1 (p. 91), A25.22 (p. 94), A25.44 (p. 96), A26.1 (p. 98, in two parts), A26.11 and A26.14 (p. 99), A26.211, A26.23, A26.3, and A26.4 (p. 100), B8.1 and B8.4 (p. 117), B25.5 (p. 143), B25.63 (p. 144), and C8.2 (p. 178). `ThePass17SubjectsAreVerified` checks the 22. A card may cite only these or a ruling. The A16 preamble (p. 84), the Index entry for the Scenario Attacker and Defender (p. 35), and Chapter H (H1.28 and H1.29, p. 329; the leader table, p. 331; H1.8 and H1.81, p. 335) are outside the registered fragments and are cited by physical page in the rulings.

**Legacy cards.** The user supplied 189 volumes of Avalon Hill's The General. The insert cards are in the PDFs (about 60 card pages, Vol. 22 to 32). Two fit the game's boards and catalog: ASL Scenario A, The Guards Counterattack (Vol. 22 No. 6, p. 51) and ASL Scenario T14, Gambit (Vol. 28 No. 6, p. 64). They are models only: each entry was brought to the registered rulebook, the text paraphrased, and no card image committed.

**Counters.** Catalog 1.11.0: the German circled-E 5-4-8 and its 2-3-8 HS (National Capabilities Chart, p. 695, sheet NCC), the British OML 2-in. mortar (British Ordnance Listing, p. 426, sheet OLG), and, manufactured under R0.3 on sheet MFG, the German HMG, British LMG, and British ATR.

## Rulings

R17.1 to R17.11 are in the plan, section 5. The referee judged R17.1 to R17.7 and R17.10 correct as drafted; its findings changed R17.4, R17.7, R17.8, R17.9, R17.10, and R17.11, and the table player's changed R17.9 (the Commissar), R17.10, and R17.11.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. Gambit gave the New Zealanders one 8-0; the legacy card has two | Legacy card | Fixed; `GambitPresentsItsEntryExitAndBoards` counts two. |
| 2. The Guards setup areas named only the anchor hex of multi-hex buildings | A26.14; legacy SSR 3 | Fixed: each area lists its whole building (F5 with F6, G6, H5; K5 with J4, J5, K4; M7 with L6, L7; N4 with M5, N3, N5; F3 with E4, G3, G4); the test checks each area is exactly its building on board 1, and validation that an area includes its anchor hex. |
| 3. SSR 1 turned Moderate EC into weather | B25.5, B25.63, E3 | Fixed: EC and wind kept, not enforced; the weather Clear by default; B25.5 and B25.63 compared. |
| 4. The Exit VP text left out the HS | A26.211 | Fixed: a HS 1 VP; "broken Personnel". |
| 5. R17.8 claimed a terrain check the validation does not make | R17.8 | Fixed: R17.8 says what the validation checks and what the tests check; terrain on reading a card is backlog. |
| 6. The Scenario Defender check was one-sided | Index, p. 35 | Fixed: the Defender must set up wholly or partly on board; tested. |
| 7. The legacy 9-0: A25.22's Commissar | A25.22, p. 94 | Fixed with the table player's finding 2: the 9-0 Commissar. |
| 8. Gambit left out ANZAC Stealth | A25.44, p. 96 | Fixed as an adaptation note; applying it is backlog. |
| 9. SSR 2's paraphrase allowed concealed setup | Legacy card | Fixed: "none of them a Concealment counter". |
| 10. Cited rules missing from the comparison | A16, Index, H1, B8.1, A25.22, A25.44 | Fixed for B8.1, A25.22, A25.44, B25.5, and B25.63 (22 fragments); the A16 preamble, the Index, and Chapter H are not registered fragments and are cited by page. |
| 11. A20.53 allows more than one edge; the bare N | A20.53, p. 87 | Fixed: the notes say "one of" the edges; the North reading is labeled. |
| 12. A3.9 allows a half box on any turn | A3.9, p. 47 | Recorded (backlog section 27). |
| 13. C8.2's "not Elite" is the registered default | C8.2, p. 178 | Fixed in the SSR text. |
| 14. A missing BPV counted as 0 | A16.1 | Fixed: a diagnostic when a starting MMC has no BPV. |
| 15. The manufactured values | R0.3 | Judged reasonable; no change. The legacy German MMG prints ROF 2 against the catalog's 3 (unit step 23), outside this pass. |

## Table player findings

The table player made 16 checks (situations on both cards: the M9 stack, the Guards in F3, the N4 leader, the Integrity totals, turns, SAN, and ELR, the edges, the win at the end of Turn 5, the sewers, the EC, the Gambit boards, the New Zealand OB, entry timing, German setup, the exit win, the New Zealanders' traits, and whether "not played" is clear): 10 passed, 6 failed.

| Finding | Disposition |
|---|---|
| 1. Gambit's second 8-0 | Fixed (referee 1). |
| 2. The legacy 9-0 is the Commissar (its art; A25.22) | Fixed: `defender-commissar-9-0`; R17.9. |
| 3. EC dropped | Fixed (referee 3). |
| 4. Single-hex building areas | Fixed (referee 2); the page lists the hexes. |
| 5. ANZAC Stealth not shown | Fixed (referee 8). |
| 6. "2I1, 2Q1, and 2Y1" read as all three | Fixed: "or". |
| 7. The entry's timing | Fixed: "during the British Player Turn of Turn 1, after the German Player Turn". |
| 8. Sewers need B8.1 | Fixed in the Russian Balance. |
| 9. Control is looser than "completely occupy" | Fixed: an adaptation note says so. |
| 10. The MFG note named only the HMG | Fixed: it names the MGs already manufactured. |
| 11. The page's wording | Fixed: boards by row from the top, rulings marked "ruling", the Integrity total marked optional and not built, edges as one of the side's edges. |

## Pass 17b: The Tractor Works

Added on 2026-09-29 at the user's request (rulings R17.12 and R17.13; catalog 1.12.0; A10.8, A12.11, A26.13, and B23.74 compared, 26 fragments in all). One referee, briefed as a skeptical referee and table player, reviewed the card: it verified every OB count against the legacy card, the plain-E 8-3-8 over the AE row, the Russian HMG, the totals [266] and [226], the setup order, the "?", the building extents, and the SSRs.

| Finding | Disposition |
|---|---|
| 1. Both Friendly Board Edges derive under A20.53 column by column; the manufactured west and east edges had enemy between | Fixed: Russian north, German south, from the setup; tested column by column. |
| 2. The page read "from its manufactured" | Fixed: the basis reads as a phrase; tested. |
| 3. The legacy "295th Infantry Division" on a Russian group | Fixed: kept, with an adaptation note that it is probably a misprint. |
| 4. Start Control of X3 (A26.11) was unstated | Fixed: the Victory Conditions say the Russians Control X3 at the start; A26.11 cited. |
| 5. SSR 2 dropped "no further penalty", and never applies in the Factory | Fixed in both Stalingrad cards; the note says so. |
| 6. Usable sewers let the Germans in by a 4TC | Fixed in both Stalingrad cards' Balance. |
| 7. The Russian HMG's portage note leaned on the MMG's 4 PP; the legacy counter seems to print 5 PP | Fixed: the note cites the legacy counter; the MMG's portage is recorded (backlog section 27). |
| 8. R17.12 cited A2.9 | Fixed: A12.12. |
| 9. The field name `dummies` | Kept: the page calls them "?" counters, which may cover real units (R17.12). |

## Live check

The Studio (`map-studio-scripted`) showed `/units/scenarios` with Gambit and, after a DOM change event on the card choice, The Guards Counterattack: no diagnostics, [130] and [207], the playable area marked not enforced, the manufactured counters marked, and no console errors. The Studio was stopped afterwards.
