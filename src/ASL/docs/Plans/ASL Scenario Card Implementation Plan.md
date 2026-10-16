# ASL Scenario Card Implementation Plan

**Status:** Deferred (the user, 2026-10-05). Moved out of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) on 2026-10-16 at the user's word, as it stood there: the outcome, the source, the user's decisions 1 to 4 of 2026-09-30, and the method per card (its section 19); what the cards need (section 20); the display batches (section 21); the card track's schedule and risks (section 23); and the legacy card catalog (Appendix A). Nothing was replanned in the move; only the cross-references changed. The track stays planned as written and is replanned when the user takes it up.

**Date:** 2026-09-30 (the plan); 2026-10-16 (this document).

**Scope:** every portable ASL scenario card of *The General* ported into the game: the display batches D1 to D18, and the making playable of each card once the rule packages it needs are built.

**What stays in the Redesign Plan.** The decisions the rule passes rest on, under their numbers: section 19, decision 5 (rules and cards are separate tracks, and this track is deferred), decision 6 (complete rule coverage of Chapters A to E; Chapters F, G, and H deferred), and decision 7 (every ASL rule in the Rules project). The rule passes, 35 to 185, are its sections 22.1 and 23; "playable after pass N" below names those passes, and its section 4 has the renumberings they follow.

**Related documents:** the [ASL Unit Backlog](<../ASL Unit Backlog.md>) (sections 32 and 33); the [ASL Rule Coverage](<../ASL Rule Coverage.md>); the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 3 and 4 (pass 17, the card format, and passes 18 to 22, card-driven play).

**Contents:** section 1, the outcome and the decisions, with the method per card; section 2, what the cards need; section 3, the display batches and their schedule; section 4, risks; Appendix A, the legacy card catalog.

## 1. Outcome and decisions

**Added 2026-09-30 at the user's request.** Every portable ASL scenario card *The General* published is ported into the game as a scenario card, brought up to the registered rulebook. Pass 17 catalogued the source and ported two cards, and pass 17b a third; the other 83 portable cards are planned here.

**The source.** Avalon Hill's *The General*, Vol. 22 No. 6 (1986) to Vol. 32 No. 3 (1994), at `E:\AvalonHill\The_General\General Avalon Hill`, with the 1988 Special Issue. The insert cards are the last pages of most issues; the magazine's own list, "GENERAL Scenarios for ASL" (Vol. 31 No. 2, p. 52), was the checklist. The catalog of 2026-09-30 found 89 distinct cards: 33 in Vol. 22 to 26 and 56 in Vol. 27 to 32, every listed scenario found, and 14 printed after the list (G35 to G46, V, and W). The Special Issue's scenario H reprints Vol. 24 No. 1's. Appendix A lists all 89.

**The user's decisions (2026-09-30):**

1. **Cards whose printed facts cannot be settled are not portable,** and are not migrated. Three are marked so in Appendix A:

| Card | Title | Why it is not portable |
|---|---|---|
| G28 | Ramsey's Charge | names board 25, which is not in its board layout |
| U | Chance d'Une Affaire | its board number is printed rotated and cannot be read with certainty |
| W | The Defense of Luga | its Turn Record Chart shows both END after Turn 4 and a restart box |

   Two cards first judged so are settled by one-line rulings instead, each recorded as its card's ruling when it is ported:

| Card | Title | Ruling |
|---|---|---|
| C | The Streets of Stalingrad | The date is 6 October 1942, as scenarios A and B that it combines; the card's 1944 is a misprint. |
| H | Escape from Velikiye Luki | The boards are as the original printing lays them out (Vol. 24 No. 1: boards 4, 2, and 3, side by side, east to west); the 1988 reprint's order is not used. |

2. **Display first.** Every portable card is ported first as display only: read, adapted, its counters in the catalog, its provenance complete, and shown on the Scenarios page, but not offered for play. It becomes playable when the rule packages it needs are built (the Redesign Plan's section 22). Amended 2026-10-05 (the Redesign Plan's decision 5): a card whose packages are already built when it is ported is written playable at once.
3. **Order.** The rule passes follow the passes of the Redesign Plan's section 4. The card track follows the rule passes, when the user takes it up (the Redesign Plan's decision 5).
4. **Chapters F and G are not built,** and the cards that need them stay display only, with the chapters in the backlog (section 33 of the ASL Unit Backlog). With one card whose map is not among the VASL boards, 12 portable cards stay display only:

| Card | Title | Why it stays display only |
|---|---|---|
| G9 | Sunday of the Dead | Chapters F and G |
| G13 | A View from the Top | desert boards (Chapter F) |
| G16 | Alligator Creek | Chapters F and G |
| G19 | A Tough Nut to Crack | Chapters F and G |
| G20 | Camp Nibeiwa | Chapters F and G |
| G22 | A Day by the Shore | beaches (Chapter G, G13) |
| G23 | Habbaniya Heights | desert boards (Chapter F) |
| G24 | Mountain Comes to Mohammed | Chapters F and G |
| G27 | Vaagso Venture | a seaborne assault with landing craft (Chapter G, G14) |
| G38 | Castello Fatato | desert boards (Chapter F) |
| G41 | JABO! | the KGP historical map, not among the VASL boards in scope |
| G45 | Halha River Bridge | Chapters F and G |

**The method, per card** (rulings R17.2, R17.9, R17.13, and R0.3 as for the first three):

1. Read the card at 300 dpi; record facts only, paraphrasing the text; never commit a card image or its wording.
2. Bring every entry to the registered rulebook: each term, rule, and reference is checked against the PDF and cited to it; what the rulebook cannot support is rewritten, manufactured under R0.3, or marked not enforced with a backlog row, never carried over as printed.
3. Add the card's counters to the catalog from registered sources (the counter sheets, the National Capabilities Chart, the ordnance and vehicle listings), manufacturing under R0.3 on sheet MFG what no source prints; a catalog version per batch, pinned as batch D1's ruling decides (section 3).
4. Register a PDF comparison for every rule fragment the card newly cites (the pass 17 registry).
5. Write the card (asl-scenario-card/1) marked display only with the packages it waits for, its ruling (one R number per card for its adaptations), and its tests: the card validates and its provenance is complete (the Redesign Plan's section 13.6). Recheck the card's packages against its image: its boards' terrain (a board holding terrain the movement rules refuse waits for pass 125), its nationalities' A25 rules, night and winter conditions, special units, and whether an SSR's "Kindling NA" still leaves a Fire need (DASL-A, T5, and G15 are flagged for this check). A changed need moves the card's playable pass, recorded in the batch's review.
6. When the card's packages are built, the card track makes it playable (until 2026-10-05, the rule pass that completed them did): tests that it sets up from its OB and plays to its end, its not-enforced entries enforced where the new rules allow, and its display-only mark removed.
7. A referee reviews each display batch's adaptations against the rulebook; the table player of a batch that makes cards playable plays one of them (until 2026-10-05, each rule pass's table player did).

## 2. What the cards need

The catalog read each card's units, terrain, and rules, corrected by the reviews of 2026-09-30. Of the 83 portable cards, 12 stay display only (decision 4) and 71 become playable: 4 need nothing the game lacks once the passes of the Redesign Plan's section 4 are done, and the rest wait for rule packages the game has not built. Each display batch rechecks its cards (section 1, step 5).

| Package | Cards needing it (of the 71) | What is missing today |
|---|---:|---|
| Armored combat | 49 | Closed-topped AFVs, main armament, AP and HEAT, special ammunition, To Kill, and Armor Leaders (backlog sections 12 and 14). Vehicles move, fire MGs, and Infantry attacks unarmored vehicles today. |
| Night and winter | 9 | Night rules beyond pass 16's (the Scenario Defender at night, Cloaking, Straying) and Fog, Ice, Drifts, and Winter Camouflage (backlog section 26). |
| Fortifications | 30 | Foxholes, trenches, wire, mines, pillboxes, and roadblocks (backlog sections 2, 16, 21, 23, 24, 26, and 27). |
| Offboard artillery and air support | 24 | Neither is built. |
| Terrain | 22 | Board overlays, streams, rivers, canals, bridges, bocage, cellars, Factories, sewers, and board terrain the movement rules still refuse (gullies, shellholes, graveyards, crags, cliffs, sunken roads). The 235 VASL boards in scope pass both fidelity checks, so the boards themselves are not a limit. |
| Special units | 12 | Cavalry, skis, air drops and gliders, boats, and partisans. |
| Fire | 4 | Kindling, spread, and Blazes (backlog section 27). |

With the card track deferred (the Redesign Plan's section 19, decision 5), no legacy card becomes playable as a package is built. The packages keep the order that would play the most cards soonest, counted by the cards that could then be ported as playable: with the passes of the Redesign Plan's section 4 alone, 4; with armored combat, 15; with night and winter, 17; with fortifications, 25; with offboard artillery and air support, 39; with terrain, 56; with special units, 67; with fire, 71.

## 3. The display batches: D1 to D18

**Deferred 2026-10-05** at the user's word (the Redesign Plan's section 19, decision 5). The batches stay planned as written and are replanned when the track is taken up.

Each display batch ports cards by steps 1 to 5 of section 1. Batch D1 first extends the card format and the pages (1:20): a card's `play` status (`display-only` with the packages it waits for, or `playable`); the Scenarios page and the card editor showing it; the Play page listing a display-only card but refusing to start it, with the packages named; a user card no longer shadowed by a built-in card of the same name, since the batches add 83 built-in cards (backlog section 32); and a ruling on how a catalog version bump affects games started from a built-in card, so that a batch's new catalog does not mark every earlier game's card changed (for example, a card's catalog pin moves only when its own counters change). Batch D4 also builds the American national rules (0:15), since T1 is the first American card made playable. The 4 cards that need no package are made playable in their display batch, with step 6's tests counted in it. Estimates per card: 15 minutes of build for a small card, 20 for a small-medium one, 25 for a medium one, 33 for a medium-large one, and 40 for a large one, with 10 more for a card that needs armored combat (its vehicles enter the catalog), from pass 17b's actual (The Tractor Works, a medium card with new counters, 0:24 of build including its play tests); 1:15 of overhead per batch.

| Batch | Cards | Build | Total |
|---|---|---|---|
| D1 | C The Streets of Stalingrad; D The Hedgehog of Piepsk | 2:35 | 3:50 |
| D2 | E Hill 621; G1 Timoshenko's Attack; F The Paw of the Tiger; G Hube's Pocket | 2:45 | 4:00 |
| D3 | G2 Last Act in Lorraine; G3 The Forgotten Front; DASL-A To the Last Man; H Escape from Velikiye Luki | 2:25 | 3:40 |
| D4 | T1 Gavin Take; T2 The Puma Prowls; T3 Ranger Stronghold; T4 Shklov's Labors Lost; G4 First Action | 2:30 | 3:45 |
| D5 | G5 Six Came Back; I Buchholz Station; J The Bitche Salient; G6 Rocket's Red Glare; K The Cannes Strongpoint | 3:00 | 4:15 |
| D6 | L Hitdorf on the Rhine; G7 Bring Up the Guns; M First Crisis at Army Group North; G8 Recon in Force; G9 Sunday of the Dead; N Soldiers of Destruction | 2:55 | 4:10 |
| D7 | O The St. Goar Assault; P The Road to Wiltz; G10 Grab at Gribovo; Q Land Leviathans; G11 Pegasus Bridge; HASL-A Ghosts in the Rubble | 3:00 | 4:15 |
| D8 | G12 Avalanche!; T6 The Dead of Winter; T5 The Pouppeville Exit; T7 Hill 253.5; T8 Aachen's Pall | 2:45 | 4:00 |
| D9 | G13 A View from the Top; T9 The Niscemi-Biscari Highway; T10 Devil's Hill; T11 The Attempt to Relieve Peiper; T12 Hunters from the Sky; G14 Tiger, Tiger | 3:00 | 4:15 |
| D10 | R Burzevo; G15 Bone of Contention; S The Whirlwind; G16 Alligator Creek; T13 Commando Raid at Dieppe; T15 The Akrotiri Peninsula | 2:45 | 4:00 |
| D11 | T16 Strayer's Strays; G17 Hakkaa Paalle; G18 Goya; G19 A Tough Nut to Crack | 2:13 | 3:28 |
| D12 | G20 Camp Nibeiwa; G21 Cat's Kill; G22 A Day by the Shore | 2:20 | 3:35 |
| D13 | G23 Habbaniya Heights; G24 Mountain Comes to Mohammed; T Pavlov's House; DASL-B The Kiwis Attack | 2:55 | 4:10 |
| D14 | G25 The T-Patchers; G26 Parker's Crossroads; G27 Vaagso Venture | 2:15 | 3:30 |
| D15 | G29 Shoot-N-Scoot; G30 Morgan's Stand; DASL-C Smoke the Kents!; G31 Point of the Sword; G32 A Helping Hand | 2:45 | 4:00 |
| D16 | G33 The Awakening of Spring; G34 The Liberators; G35 Going to Church; G36 Hill of Death | 2:45 | 4:00 |
| D17 | G37 Forth Bridge; V Auld Lang Syne; G38 Castello Fatato; G39 A Desperate Affair; G40 Will to Fight...Eradicated; G41 JABO! | 2:51 | 4:06 |
| D18 | G42 The Youth's First Blood; G43 Kangaroo Hop; G44 Abandon Ship!; G45 Halha River Bridge; G46 Triumph atop Taraldsvikfjell | 2:45 | 4:00 |

**The schedule.** The card track is deferred (2026-10-05; the Redesign Plan's section 19, decision 5), and its batches are listed here as they were planned:

| Pass | Title | Kind | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| D1 | Display: C, D | Display | 2:35 | 3:50 | 2:41 to 4:59 |
| D2 | Display: E, G1, F, G | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D3 | Display: G2, G3, DASL-A, H | Display | 2:25 | 3:40 | 2:34 to 4:46 |
| D4 | Display: T1, T2, T3, T4, G4 | Display | 2:30 | 3:45 | 2:38 to 4:52 |
| D5 | Display: G5, I, J, G6, K | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D6 | Display: L, G7, M, G8, G9, N | Display | 2:55 | 4:10 | 2:55 to 5:25 |
| D7 | Display: O, P, G10, Q, G11, HASL-A | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D8 | Display: G12, T6, T5, T7, T8 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D9 | Display: G13, T9, T10, T11, T12, G14 | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D10 | Display: R, G15, S, G16, T13, T15 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D11 | Display: T16, G17, G18, G19 | Display | 2:13 | 3:28 | 2:26 to 4:30 |
| D12 | Display: G20, G21, G22 | Display | 2:20 | 3:35 | 2:30 to 4:40 |
| D13 | Display: G23, G24, T, DASL-B | Display | 2:55 | 4:10 | 2:55 to 5:25 |
| D14 | Display: G25, G26, G27 | Display | 2:15 | 3:30 | 2:27 to 4:33 |
| D15 | Display: G29, G30, DASL-C, G31, G32 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D16 | Display: G33, G34, G35, G36 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D17 | Display: G37, V, G38, G39, G40, G41 | Display | 2:51 | 4:06 | 2:52 to 5:20 |
| D18 | Display: G42, G43, G44, G45, G46 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| | **The display batches, deferred** | | **48:29** | **70:59** | |
| | **Making the cards playable, deferred** (it left the rule passes on 2026-10-05) | | **14:50** | **14:50** | |
| | **The card track, deferred** | | **63:19** | **85:49** | |

The card track's total, 85:49, was part of the Redesign Plan's whole until 2026-10-16 (its section 23).

## 4. Risks and what is not updated

**What this does not update.** The "playable after pass N" entries of section 3 and Appendix A were written against the eleven passes as first planned. The Redesign Plan's section 22.1 moved rules out of several of them (Passengers and Riders to pass 65, ordnance SMOKE to pass 85, bocage and sewers to pass 145, and others), so a card may now wait for a later pass than its entry names. They are left as they are until the card track is replanned.

**Risks.** The catalog was read from page images and its needs corrected by review, but a card may still need a package the catalog missed; its display batch records the change. That includes T1, T8, T16, and H, playable in their display batch only if their boards hold no terrain the movement rules refuse. Each display batch adds counters, so each carries a catalog version, with its effect on earlier games settled by D1's ruling.

# Appendix A. The legacy card catalog

The 89 distinct ASL scenario cards of *The General*, Vol. 22 to 32 (the Special Issue's reprint of H omitted), catalogued on 2026-09-30 from the magazine's pages and corrected by the reviews the same day. Facts only; titles as printed. "Needs" lists the rule packages of section 2 the card waits for; "Status" is where it is ported and when it becomes playable, why it stays display only, or why it is not portable.

| Id | Title | Issue | Date | Boards | Sides | Turns | Size | Needs | Status |
|---|---|---|---|---|---|---|---|---|---|
| A | The Guards Counterattack | Vol22i6 | 6 Oct 1942 | one board, hexrows A-P only (board number not legible in scan; likely board 1) | German vs Russian (Guards) | 5 | small | none | Ported (pass 17), playable |
| B | The Tractor Works | Vol22i6 | 6 Oct 1942 | one board, hexrows O-GG only (board number not legible in scan; likely board 1) | German (incl. assault engineers) vs Russian | 8 | medium | none | Ported (pass 17b), playable |
| C | The Streets of Stalingrad | Vol22i6 | 6 Oct 1942 (card header misprints 1944) | Combined layout of scenarios A and B | German vs Russian | as A and B (7 turns shown) | large | Armor, OBA-Air, Terrain | Display D1; playable after pass 135 (settled by its ruling, section 1) |
| D | The Hedgehog of Piepsk | Vol23i2 | 14 Nov 1941 | 4, 3, 2 (stacked N-S) | German vs Russian | 10 | medium | OBA-Air | Display D1; playable after pass 160 |
| E | Hill 621 | Vol23i2 | 1 Jul 1944 | 2, 4, 3 (side by side E-W) | German vs Russian (Guards) | 10 | large | Armor, OBA-Air | Display D2; playable after pass 160 |
| G1 | Timoshenko's Attack | Vol23i3 | 12 Jul 1941 | 22, 11, 10 (side by side, separate lanes) | German vs Russian | 9 | large | Armor, OBA-Air, Terrain | Display D2; playable after pass 135 |
| F | The Paw of the Tiger | Vol23i5 | 12 Jan 1943 | 2, 4, 5 (side by side E-W) | German vs Russian | 10 | small-medium | Armor, OBA-Air, Fortifications | Display D2; playable after pass 160 |
| G | Hube's Pocket | Vol23i5 | 6 Apr 1944 | 4, 2, 5 (stacked N-S) | German (SS) vs Russian | 10 (END mark at 4 with restart option to turn 11) | medium | Armor | Display D2; playable after pass 55 |
| G2 | Last Act in Lorraine | Vol23i6 | 6 Dec 1944 | 12, 17 (stacked N-S) | German vs American | 10 | medium | Armor, OBA-Air, Fire | Display D3; playable after pass 185 |
| G3 | The Forgotten Front | Vol23i6 | 9 Feb 1945 | 19, 12 (stacked N-S) | German vs American | 8 | medium | Armor, Fortifications | Display D3; playable after pass 120 |
| DASL-A | To the Last Man | Vol24i1 | 13 Jan 1945 | Deluxe boards a, b, c, d | German (SS cavalry) vs Russian (Guards) | 8 | large | Armor, OBA-Air, Fortifications, Fire | Display D3; playable after pass 185 |
| H | Escape from Velikiye Luki | Vol24i1 | 12 Jan 1943 | 4, 2, 3 (side by side E-W) | German vs Russian | 10 (last turn conditional) | small | none | Display D3; playable in D3 (settled by its ruling, section 1) |
| T1 | Gavin Take | Vol24i2 | 6 Jun 1944 | 3 | German vs American (82nd Airborne) | 6 | small | none | Display D4; playable in D4 |
| T2 | The Puma Prowls | Vol24i2 | 28 Jun 1944 | 22, 4 (stacked N-S) | German vs Russian | 6 | small | Armor | Display D4; playable after pass 55 |
| T3 | Ranger Stronghold | Vol24i2 | 14 Sep 1943 | 2 | German vs American (Rangers) | 6 | small | Armor | Display D4; playable after pass 55 |
| T4 | Shklov's Labors Lost | Vol24i2 | 11 Jul 1941 | 1 | German (Grossdeutschland) vs Russian (officer cadets) | 6 | small | Armor | Display D4; playable after pass 55 |
| G4 | First Action | Vol24i3 | 8 Nov 1944 | 12, 18, 19 (side by side E-W) | German vs American (761st Tank Bn) | 11 (last turn conditional) | medium | Armor, OBA-Air, Fortifications | Display D4; playable after pass 160 |
| G5 | Six Came Back | Vol24i3 | 30 Jan 1944 | 12, 17, 16 (stacked N-S) | German (Hermann Goering, 26th Pz) vs American (Rangers) | 9 | medium | Armor, Terrain, Night and winter | Display D5; playable after pass 135 |
| I | Buchholz Station | Vol24i4 | 16 Dec 1944 | 4, 3 (stacked N-S) | German (Volksgrenadier) vs American | 10 | medium | Armor | Display D5; playable after pass 55 |
| J | The Bitche Salient | Vol24i4 | 14 Jan 1945 | 4, 2 (side by side E-W) | German (Gebirgsjaeger) vs American | 10 | large | Armor, OBA-Air, Fortifications | Display D5; playable after pass 160 |
| G6 | Rocket's Red Glare | Vol24i6 | 22 Dec 1944 | 3 | German (SS) vs American (504th Parachute) | 6 | small | Armor | Display D5; playable after pass 55 |
| K | The Cannes Strongpoint | Vol25i2 | 23 Aug 1944 | 4, 2, 3 (side by side E-W) | German vs American (509th Parachute) | 5 | medium | Armor, OBA-Air, Fortifications | Display D5; playable after pass 160 |
| L | Hitdorf on the Rhine | Vol25i2 | 6 Apr 1945 | 4, 3 (side by side E-W) | German (Volksgrenadier, 11th Pz) vs American (504th Parachute) | 9 | medium | Armor, OBA-Air, Fortifications | Display D6; playable after pass 160 |
| G7 | Bring Up the Guns | Vol25i3 | 10 May 1940 | 4, 33 (stacked N-S) | German (cavalry) vs Dutch | 7 | small | Fortifications, Special units | Display D6; playable after pass 165 |
| M | First Crisis at Army Group North | Vol25i3 | 25 Jun 1941 | 6, 4 (stacked N-S) | German vs Russian | 7 | small | Armor | Display D6; playable after pass 55 |
| G8 | Recon in Force | Vol25i5 | 16 Jul 1943 | 22, 21 (stacked N-S) | Italian and German (Axis) vs American (Rangers) | 7 | medium | Terrain | Display D6; playable after pass 135 |
| G9 | Sunday of the Dead | Vol25i6 | 23 Nov 1941 | 27, 28, 26, 29 (desert boards, side by side) | German vs British (South African) | 9 | large | Armor, OBA-Air, Chapter F or G | Display D6; display only: Chapters F and G |
| N | Soldiers of Destruction | Vol25i6 | 10 Oct 1944 | 6, 4 (side by side E-W) | German (SS Totenkopf) vs Russian (Guards) | 7 | small | Armor | Display D6; playable after pass 55 |
| O | The St. Goar Assault | Vol26i1 | 24 Mar 1945 | 2, 1, 4, 3 (side by side E-W) | German (Wehrkreis XIII) vs American (87th Inf Div) | 8 (last turn conditional) | large | Armor, Terrain, Special units, Night and winter | Display D7; playable after pass 165 |
| P | The Road to Wiltz | Vol26i1 | 18 Dec 1944 | 3, 2 (side by side E-W) | German (Volksgrenadier, Panzer Lehr) vs American (engineers, 707th Tank Bn) | 5, with restart to 10 (restart on turn 11 option) | large | Armor, OBA-Air, Fortifications | Display D7; playable after pass 160 |
| G10 | Grab at Gribovo | Vol26i2 | 3 Jan 1942 | 18, 12, 4 (stacked N-S) | German vs Russian (paratroops) | 8 | small | Special units, Night and winter | Display D7; playable after pass 165 |
| Q | Land Leviathans | Vol26i2 | 3 Jul 1941 | 2, 3, 4 (stacked N-S) | German vs Russian | 7 | small | Armor | Display D7; playable after pass 55 |
| G11 | Pegasus Bridge | Vol26i5 | 6 Jun 1944 | 23 | German vs British (6th Airborne, Ox and Bucks) | 5 | small | Fortifications, Terrain, Special units | Display D7; playable after pass 165 |
| HASL-A | Ghosts in the Rubble | Vol27i1 | 31 October 1942 | RB map (only hexes numbered >= 38 on/east of hexrow U) | German (Pz Div 14) vs Russian (45th Rifle Div) | 7 | medium | OBA-Air, Fortifications, Terrain | Display D7; playable after pass 135 |
| G12 | Avalanche! | Vol27i1 | 6 February 1943 | 9, 15 | German (Gebirgsjaeger Div 1) vs Russian (318th Mountain Rifle Div) | 10 | large | Fortifications, Special units, Night and winter | Display D8; playable after pass 165 |
| T6 | The Dead of Winter | Vol27i2 | 29 December 1941 | 4 (hexrows R-GG only) | German (IR 18) vs Russian (Siberians, 31st Army) | 5 | small | Armor, Fortifications | Display D8; playable after pass 120 |
| T5 | The Pouppeville Exit | Vol27i2 | 6 June 1944 | 3, 5 | German (IR 1058) vs American (501st Parachute Rgt) | 8 | small | Armor, Terrain, Fire | Display D8; playable after pass 185 |
| T7 | Hill 253.5 | Vol27i3 | 9 July 1943 | 2 | German (Pz Div 18, Pzjg Abt 653) vs Russian (307th Rifle Div, mech brigade) | 8 | large | Armor, OBA-Air, Fortifications | Display D8; playable after pass 160 |
| T8 | Aachen's Pall | Vol27i3 | 15 October 1944 | 1 (hexrows A-P only) | German (Aachen HQ) vs American (26th Inf Rgt) | 4 | small | none | Display D8; playable in D8 |
| G13 | A View from the Top | Vol27i5 | 23 February 1945 | 25, 15 (hexrows R-GG on 15) | German (ID 334) vs American (86th Mountain Inf Rgt) | 8 | medium | OBA-Air, Fortifications, Special units, Night and winter | Display D9; display only: desert boards (Chapter F) |
| T9 | The Niscemi-Biscari Highway | Vol28i1 | 10 July 1943 | 4, 5 (hexrows Q-GG) | American (504th Parachute Rgt) vs German (Hermann Goering Div recon) | 8 | small | Armor, Fortifications | Display D9; playable after pass 120 |
| T10 | Devil's Hill | Vol28i1 | 19 September 1944 | 2, 5 | German (Landesschuetzen Div 406) vs American (508th Parachute Rgt) | 7 | small | Armor, Fortifications | Display D9; playable after pass 120 |
| T11 | The Attempt to Relieve Peiper | Vol28i2 | 21 December 1944 | 2, 5 | American (505th Parachute Rgt) vs German (1st SS Pz Div) | 10 | medium | Armor, OBA-Air, Fortifications | Display D9; playable after pass 160 |
| T12 | Hunters from the Sky | Vol28i2 | 24 March 1945 | 5, 4, 2 | German (ID 84) vs American (513th Parachute Rgt) | 10 | medium | Armor, Special units | Display D9; playable after pass 165 |
| G14 | Tiger, Tiger | Vol28i3 | 11 February 1943 | 11, 18, 33, 17 | Russian (46th Tank Bde) vs German (s.Pz.Abt 502) | 7 | medium | Armor | Display D9; playable after pass 55 |
| R | Burzevo | Vol28i3 | 2 December 1941 | 3 | German (IR 478) vs Russian (20th Tank Bde) | 5 | small | Armor, Night and winter | Display D10; playable after pass 90 |
| G15 | Bone of Contention | Vol28i4 | 31 August 1944 | 21, 20 | Free French partisans (Maquis) vs German (SS-Pz Abt 102 tank crews) | 7 | small | Armor, Fire, Special units | Display D10; playable after pass 185 |
| S | The Whirlwind | Vol28i4 | 18 April 1945 | 5, 10 | American (26th Inf Div) vs German (Ersatz Div 471) | 7 | small | Armor | Display D10; playable after pass 55 |
| G16 | Alligator Creek | Vol28i5 | 21 August 1942 | 32, 34 (R-GG) plus overlays Be4, Be6, Ef1, Oc1, Oc2 | American (1st Marine Rgt) vs Japanese (28th Inf Rgt, Ichiki detachment) | 7 | large | OBA-Air, Fortifications, Terrain, Chapter F or G | Display D10; display only: Chapters F and G |
| T13 | Commando Raid at Dieppe | Vol28i6 | 18 August 1942 | 4, 5 | German (302nd ID, Battery Hess) vs British (No. 4 Commando) with US Rangers | 10 | medium | Fortifications | Display D10; playable after pass 120 |
| T14 | Gambit | Vol28i6 | 21 May 1941 | 4, 2 | New Zealand (22nd Bn) vs German (Sturm Rgt 1) | 8 | small | none | Ported (pass 17), playable |
| T15 | The Akrotiri Peninsula | Vol29i1 | 20 May 1941 | 3, 6, 4 | British (102nd AT Rgt, 151st AA Bty) vs German (Sturm Rgt 1, glider) | 9 | medium | Special units | Display D10; playable after pass 165 |
| T16 | Strayer's Strays | Vol29i1 | 6 June 1944 | 6 | German (IR 919) vs American (mixed 502/506/508 PIR) | 4 | small | none | Display D11; playable in D11 |
| G17 | Hakkaa Paalle | Vol29i2 | 12 January 1940 | 16, 19, 32, 34 (R-GG on 32/34 not playable), overlay O5 | Russian (2nd Ski Bde) vs Finnish (9th Sissi Co) | 10 | medium | Terrain, Special units, Night and winter | Display D11; playable after pass 165 |
| G18 | Goya | Vol29i2 | 7 January 1945 | 39, 16, 23 | German (VGR 183) vs American (551st PIB) | 11 | large | Terrain, Night and winter | Display D11; playable after pass 135 |
| G19 | A Tough Nut to Crack | Vol29i3 | 1 January 1943 | 37, 35 plus overlays X13, X14 | Japanese (19th Army remnants) vs Australian (2/9th Bn, 2/6th Armoured Rgt) | 9 | medium-large | Armor, OBA-Air, Fortifications, Terrain, Chapter F or G | Display D11; display only: Chapters F and G |
| G20 | Camp Nibeiwa | Vol29i3 | 9 December 1940 | 30, 31 (Q-GG not playable), 29, 28, overlays D1, D5, D6, H3, H6, S2, S6 | Italian (Gruppo Maletti) vs British (4th Indian Div, 7th RTR) | 10 | large | Armor, Fortifications, Terrain, Chapter F or G | Display D12; display only: Chapters F and G |
| G21 | Cat's Kill | Vol29i4 | 8 June 1944 | 20, 10 plus overlays O1-O5 | Canadian (Regina Rifles) vs German (12. SS Pz Div Hitlerjugend) | 10 | large | Armor, Terrain, Night and winter | Display D12; playable after pass 135 |
| G22 | A Day by the Shore | Vol29i4 | 7 June 1944 | 8, 20, 4 | German (GR 736) vs British (45 Royal Marine Commando) | 10 | large | OBA-Air, Fortifications, Terrain | Display D12; display only: beaches (Chapter G, G13) |
| G23 | Habbaniya Heights | Vol29i5 | 5 May 1941 | 29, 25 plus overlay E1 | Iraqi (2nd Iraqi Legion) vs British (King's Own Royal Rgt) | 7 | large | Armor, Fortifications, Terrain, Night and winter | Display D13; display only: desert boards (Chapter F) |
| G24 | Mountain Comes to Mohammed | Vol29i5 | 25 August 1941 | 28, 26 plus overlays S3, S5, S8, X4, D6, H6 | Iranian (Pahlavi Guards) vs British (2/11th Sikh, 13th Lancers) | 8 | large | Armor, Fortifications, Terrain, Chapter F or G | Display D13; display only: Chapters F and G |
| T | Pavlov's House | Vol29i6 | 20 October 1942 | 1 plus overlays OG1, OG4, OG5 | Russian (42nd Guards Rifle Rgt) vs German (6. Armee Sturmgruppe) | 7 | small | Armor, OBA-Air, Terrain | Display D13; playable after pass 135 |
| DASL-B | The Kiwis Attack | Vol29i6 | 15 March 1944 | Deluxe boards q, p | German (FJR 3) vs New Zealand (25th Bn, 19th Armd Rgt) | 7 | large | Armor, Fortifications, Terrain | Display D13; playable after pass 135 |
| G25 | The T-Patchers | Vol30i1 | 15 December 1943 | 41, 11 (R-GG) plus overlays X12, X13, X14 | German (PzGr Div 29) vs American (143rd Inf Rgt, 753rd Tank Bn) | 7 | medium | Armor, Fortifications, Terrain | Display D14; playable after pass 135 |
| G26 | Parker's Crossroads | Vol30i1 | 23 December 1944 | 42, 19, 33, 43, 35, 16 (parts unplayable) | American (mixed: 589th FA Bn, 3rd Armd Div, 325th GIR) vs German (2nd SS Pz Div Das Reich) | 10 | large | Armor, OBA-Air, Fortifications, Special units | Display D14; playable after pass 165 |
| G27 | Vaagso Venture | Vol30i2 | 27 December 1941 | 41 (R-GG), 8 (A-P) | German (ID 181) vs British (2, 3 and 6 Commandos) | 9 | large | Armor, OBA-Air, Fortifications, Terrain, Fire | Display D14; display only: a seaborne assault with landing craft (Chapter G, G14) |
| G28 | Ramsey's Charge | Vol30i3 | 16 January 1942 | 40, 35 plus overlays 1, X6, OG1, OG2, OG3, OG5 | Japanese (14th Army) vs American (26th Cavalry, Philippine Scouts) | 7 | small-medium | Terrain, Special units, Chapter F or G | Not portable: names board 25, which is not in its board layout |
| G29 | Shoot-N-Scoot | Vol30i4 | 18 December 1944 | 12, 42 | American (38th Inf Rgt, 644th TD Bn, 741st Tank Bn) vs German (12th SS Pz Div) | 9 | large | Armor, OBA-Air, Fortifications | Display D15; playable after pass 160 |
| G30 | Morgan's Stand | Vol30i4 | 11 September 1944 | 10, 7 (R-GG) | American (53rd Armd Inf Bn) vs German (Fusilier Rgt 312) | 7 | small-medium | Armor, Terrain | Display D15; playable after pass 135 |
| DASL-C | Smoke the Kents! | Vol30i5 | 20 May 1940 | Deluxe boards g, d, h, e plus overlays dx2, dx4, dx6, dx7 | British (7th Royal West Kent) vs German (Pz Div 1) | 8 | medium | Armor, Terrain | Display D15; playable after pass 135 |
| U | Chance d'Une Affaire | Vol30i5 | 14 May 1940 | 9 (rotated label; possibly 6) | German (IR 1, Pz Div 1, Sturmpionier Bn 43) vs French (213e RI, 7e BCC) | 9 | medium | Armor | Not portable: its board number is printed rotated and cannot be read with certainty |
| G31 | Point of the Sword | Vol30i6 | 6 June 1944 | 10 | German (Pz Div 21) vs British (4th Brigade Commandos) and Canadian (Regiment de la Chaudiere) | 8 | medium | Fortifications | Display D15; playable after pass 120 |
| G32 | A Helping Hand | Vol30i6 | 13 June 1944 | 11, 2 (A-P) | German (VGR 914, FJR 8) vs American (38th Inf Rgt) | 10 | medium | OBA-Air, Fortifications, Terrain | Display D15; playable after pass 135 |
| G33 | The Awakening of Spring | Vol31i1 | 14 March 1945 | 11, 16, 17, 4 | Russian (35th Guards Rifle Corps, 23rd Tank Corps) vs German (SS Pz Div 6) | 10 | large | Armor, Night and winter | Display D16; playable after pass 90 |
| G34 | The Liberators | Vol31i1 | 2 April 1945 | 22, 3 plus overlays X13, X16, X17, St3 | German (SS-Pz Korps 1) vs Russian (IX Guards Mech Corps) | 9 | large | Armor, OBA-Air, Terrain | Display D16; playable after pass 135 |
| G35 | Going to Church | Vol31i2 | 1 August 1944 | 23 (A-P) | German (SS Pz Div 9) vs Canadian (Les Fusiliers Mont-Royal) | 6 | small | Terrain | Display D16; playable after pass 135 |
| G36 | Hill of Death | Vol31i2 | 11 July 1944 | 33, 9, 16 | German (SS Pz Divs 9 and 10, s.SS-Pz Abt 102) vs British (5th DCLI, 7th RTR) | 8 | large | Armor, OBA-Air, Fortifications, Terrain | Display D16; playable after pass 135 |
| G37 | Forth Bridge | Vol31i3 | 11 April 1945 | 17, 18 plus overlays OG5, Wd4, B2, B3, St1, St3 | German (FJ Div 7) vs British (2nd Gordon Highlanders, 3rd Scots Guards) | 9 | medium-large | Armor, Fortifications, Terrain | Display D17; playable after pass 135 |
| V | Auld Lang Syne | Vol31i3 | 1 January 1945 | 10 (R-GG), 1 (A-P) | American (345th Inf Rgt) vs German (PzGr Div 15) | 6 | small-medium | OBA-Air | Display D17; playable after pass 160 |
| G38 | Castello Fatato | Vol31i4 | 20 December 1942 | 27, 28 (R-GG) plus overlays X7-X15, X18 | Russian (1st Guards Army) vs Italian (3rd Bersaglieri Rgt) | 7 | medium | Terrain, Fire, Night and winter | Display D17; display only: desert boards (Chapter F) |
| G39 | A Desperate Affair | Vol31i4 | 20 May 1941 | 9, 33, 16, 18 | British (2nd Black Watch) vs German (FJR 1) | 8 | medium-large | Fortifications, Special units | Display D17; playable after pass 165 |
| G40 | Will to Fight...Eradicated | Vol31i5 | 7 September 1939 | 12 (hexrows K-Y) plus overlays G1, G2, G4, Wd5, X7, X9 | German (LSSAH) vs Polish (2nd Kaniov Rifle Rgt, dismounted cavalry) | 7 | medium | Terrain, Special units | Display D17; playable after pass 165 |
| G41 | JABO! | Vol31i5 | 18 December 1944 | KGP map (hexes <= 14 in rows AA-TT) | German (Kampfgruppe Peiper) vs American (IX Tactical Air Command aircraft) | 3 | small | Armor, OBA-Air | Display D17; display only: the KGP historical map, not among the VASL boards in scope |
| G42 | The Youth's First Blood | Vol31i6 | 7 June 1944 | 16, 10, 33 | German (ID 716, 12. SS Pz Div) vs Canadian (North Nova Scotia Highlanders, Sherbrooke Fusiliers) | 9 | large | Armor, OBA-Air | Display D18; playable after pass 160 |
| G43 | Kangaroo Hop | Vol31i6 | 17 September 1944 | 2 | German (Bodenstaendige Div 326) vs Canadian (North Nova Scotia Highlanders, Fort Garry Horse, RE) | 6 | medium | Armor, OBA-Air, Fortifications | Display D18; playable after pass 160 |
| G44 | Abandon Ship! | Vol32i2 | 19 December 1944 | 24 | American (501st PIR) vs German (Pz Lehr, PGR 902) | 7 | small-medium | Armor, Fortifications | Display D18; playable after pass 120 |
| G45 | Halha River Bridge | Vol32i2 | 8 July 1939 | 26, 27 (A-P) plus overlays St1, St3, SD2, SD6 | Russian (149th Motorized Rifle Rgt) vs Japanese (4th Tank Rgt, 72nd Inf Rgt) | 6 | small | Armor, Fortifications, Terrain, Chapter F or G | Display D18; display only: Chapters F and G |
| G46 | Triumph atop Taraldsvikfjell | Vol32i3 | 28 May 1940 | 2 | German (Gebirgsjaeger Rgt 139 and naval personnel) vs Allied (French Foreign Legion, Norwegian 2/15th Bn) | 7 | medium | Fortifications | Display D18; playable after pass 120 |
| W | The Defense of Luga | Vol32i3 | 19 July 1941 | 4, 3, 5, 1 | German (IR 469, ID 269) vs Russian (Operational Group Luga) | 10 (turn chart also marks END at 4 and a restart; unclear) | large | Armor | Not portable: its Turn Record Chart shows both END after Turn 4 and a restart box |
