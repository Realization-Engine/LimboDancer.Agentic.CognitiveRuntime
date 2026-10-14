"""Pass 40 task 40.9: the design's section 11 and the time log's row. Usage: python docs_409.py <start HH:MM> <end HH:MM>"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)

SECTION = """
## 11. The small rows: 40.9 (increment 2e)

Built on 2026-10-11 under the user's word of that evening, as section 10 has it. Question F10 named what is built, what is ruled, and what moves on; two of the things it named as built are not (11.4), which is this section's departure from an answer the user gave and is put first in the pass's report.

### 11.1 The rules, as read on their pages

- **A1.31 (p. 45):** a Good Order squad with a Good Order leader in its Location "may attempt, in their RPh, to split into two equal HS" by a leader-modified NTC "[EXC: Finns (25.7), Guards, Carrier HS, unarmed units (20.5), and U.S.M.C. 7-6-8s do not require leader direction to Deploy]".
- **A1.32 (p. 45):** two Good Order HS "may automatically Recombine as their sole action in that RPh ... at the start of any RPh in which they occupy the same Location and terrain ... with a friendly Good Order leader [EXC: Finns, Guards, Carrier HS and unarmed HS do not need leader presence to Recombine]".
- **A20.5 (p. 88):** "An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or leader requirements."
- **A1.6 (p. 45):** the US# is the number of silhouettes (SMC 1, crew or HS 2, squad 3); "Horses and all other 5/8\\" counters have a US# of 4 as do their Riders or attendant crews [EXC: Any vehicle or Gun classified as large or very large ... has a US# of 5]".
- **A2.3 (p. 45):** "Units allowed to set up/enter anywhere on a given board may set up/enter on that board's half-hexes only if the half-hex is either part of another board on which it is also allowed to set up/enter or is not butted against another half-hex. Similarly, occupation of a board's half-hexes will satisfy occupation of that board for any applicable Victory Conditions only if they are not butted against other non-qualifying half-hexes."

### 11.2 What is built

- **No leader for a Guard and an Unarmed unit.** Rules: `ScenarioA1SupportWeaponRules.NeedsNoLeader`. A squad that guards prisoners, or is Unarmed, Deploys in its RPh on a NTC with no modifier when no leader is named; two HS Recombine with none when each is a Guard or Unarmed. A leader named directs all the same. The branch that was dead code (no leader named was always refused) is gone.
- **A Guard squad's Deployment at will.** Rules: `GuardDeploysAtWill`. An unbroken squad that guards prisoners Deploys in any phase, on either side's Player Turn, of any nationality (a Russian Guard too), with no leader and no NTC. One of its HS keeps the prisoners, by the projector's rule for a Guard that leaves play. Outside the RPh the page has a block for it.
- **Recombining at the start of the RPh.** Rules: `RecombineStartBar`. It is refused once a unit of the HS's side has attempted a Rally, a Repair, a Deployment, or a Recovery, or made a Transfer, in that RPh. Other Recombinations do not count.
- **One US# member:** `ScenarioA1Definitions.UnitSize`, with the 4 and the 5. The four copies (prisoners, CC, a Guard's fire, the Concealment dr) and the projector's read it.
- **A butted half-hex at setup.** Rules: `ScenarioA1SetupCalculator.InAreas`; the setup's Location facts carry the hex's name on the other board (`OtherHalf`), which Play reads from the placed map's layout. A counter sets up in a butted half-hex only if its group's areas hold the hex under both boards' names.

### 11.3 Rulings, decided under the user's word of 2026-10-11

- **R40.21 (with R31.4):** the Guards of A1.31 and A1.32 are the Guards of prisoners. Two HS Recombine with no leader only when both are exempt. A Guard squad's Deployment at will is no RPh action outside the RPh.
- **R40.22:** "the start of any RPh" is before any other RPh action by a unit of the HS's own side; the other side's actions do not end it. Recombining keeps no side check: the page says "any RPh", where Deployment says "their RPh".
- **R40.23 (A2.2):** a hex that two boards share keeps the name of the board placed later, as VASL names it, because the boards' fixtures come from VASL; the rule names it by the northeasternmost board. The setup reads both names (A2.3).
- **R40.24 (A3.9):** provisional (gray) turns and a chart that runs past END are built with the first card that prints one; the search of the library's eight cards found none (`cards_survey.py`).
- **R40.25 (A1.6):** the member answers 4 and 5 for a 5/8 inch counter, but no caller hands it a vehicle or a Gun yet: the rules that read a vehicle's US# (its Concealment dr) are other passes'.

### 11.4 Not built, against the answer to F10

| Item | Why | Row |
|---|---|---|
| A CE or BU choice at setup (A2.52) | The read found none: an AFV sets up CE and buttons up only in its MPh or APh. The setup's rows carry a counter's place, its "?", and HIP, and nothing by counter kind; a choice there is a feature of the setup panel, not a small row. | Pass 65 (vehicles) |
| A butted half-hex for entry and for occupation (A2.3) | Entry is by an edge, which reads no area; occupation is the Victory Conditions' own read of an area. Neither is reached by a card of the library. The setup half is built. | Pass 185, with the Victory Conditions |

**Also left out, each for a backlog row:** Finns, U.S.M.C. 7-6-8s, and Carrier HS (passes 80 and 180); "the same terrain" for Recombining, and HD status (pass 100); Crest status (pass 125); a SW dismantled (pass 45) and ordnance limbered (pass 110); the US# of horses (pass 165); a large vehicle's or Gun's 5 from the catalog; the page's setup highlight, which still reads a butted half-hex by its owner's name alone.

**The page's three small things of section 5.2** (the DFPh's button, the "Latest" line, the hand-over prompt at 320 pixels) are read in the pass's Studio check, where the views are walked at that width, and fixed there if they are seen.

### 11.5 The games

On board 3, written by `make_409.py`, with no leader on the map.

| Game | What it holds |
|---|---|
| `p40-noleader` | The German RPh. Two armed HS and an armed squad are refused for want of a leader; two Unarmed HS Recombine; an Unarmed squad Deploys on its NTC (DR 3, DRM 0); the armed HS are then refused again, now for the start of the RPh. |
| `p40-guard-deploy` | The German PFPh. A squad that guards a Russian HS Deploys from the page's block with no dice; the block then goes, since no squad guards. |

**Not staged in the Studio:** the butted half-hex. The one two-board card (Gambit) has no area on its later board, so no setup of it meets the rule either way; the Rules test holds the three cases.

### 11.6 Tests

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: no leader and the Guard's Deployment, with the bar on a late Recombination; the US# with its 4 and 5 and by kind; the butted half-hex under one board's areas, the other's, both, and on the map's edge.
- **Play, new:** `BacklogPass13Tests.AnUnarmedSquadDeploysAndUnarmedHalfSquadsRecombineWithNoLeader`, `BacklogPass13Tests.HalfSquadsRecombineOnlyAtTheStartOfTheirSidesRph`, `BacklogPass14Tests.AGuardSquadDeploysAtWillInAnyPhaseWithNoLeaderAndNoNtc` (the Guard is made by a capture in CC; a prisoner staged by a condition has no Guard).
- **Run:** 121 Rules tests (the pass's class and the setup, prisoner, and CC classes), and 90 Play tests in the five classes that Deploy or Recombine, then the two changed classes again (40), all passed. MapStudio built with warnings as errors.

### 11.7 Recorded games

A record replays as it was played. A record in which HS Recombined after their side's Rally attempt would be refused if proposed today and is not re-judged on replay. The US# reads the same numbers for every Personnel unit.

**Not run:** the replay digest, the sweep, the text list.
"""

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "## 11. The small rows: 40.9" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.5 committed (b840564a), which ends increment 2d. Increment 2e, task 40.9, the small rows: A1.31, A1.32, A1.6, and A2.3 read on p. 45 and A20.5 on p. 88; "
       "Deployment and Recombining with no leader for a Guard and an Unarmed unit, a Guard squad's Deployment at will, Recombining at the RPh's start, one US# member, "
       "a butted half-hex at setup; the cards surveyed for a seam area and a provisional turn (none); MapStudio built with warnings as errors; two games on board 3 played and read back; "
       f"three Rules tests and three Play tests; the design's section 11 with rulings R40.21 to R40.25 decided under the user's word | {start} | {end} | {minutes} min | "
       "Two things question F10 named as built are not, and the design says so first: a CE or BU choice at setup (a setup-panel feature, to pass 65) and the half-hex rule for entry and occupation. "
       "The half-hex rule read on its page is the reverse of the frame's words: a unit is allowed in a butted half-hex only when allowed on both boards. "
       "A Guard staged by a condition has no Guard in the state, so that test moved to the class where a capture makes one. The page's three small things are left to the Studio check. "
       "Not run: the digest, the sweep, the text list. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + ROW)
print("ok", minutes)
