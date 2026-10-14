
## 9. Special ammunition: 55.1, the rest (increment 2c)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 9.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **Nothing is built on the Area Target Type.** Only mortars fire on it, and no mortar of the catalog lists an HE number. Ruling R55.7 named all three Target Types.
- **AP against Infantry (C8.31) stays out,** as the frame said: a 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry.
- **An Intensive Fire shot that finds no round leaves no Intensive Fire counter,** read from "not to have fired yet for any purpose" (ruling R55.25).
- **A malfunction on a DR above the number records a miss for the new kinds only** (APDS, HE, AP by number). An APCR or HEAT record keeps the To Hit result it showed before the pass, which may read "hit" beside a malfunction and no To Kill DR; it is left so that no record changes.

### 9.2 What is built

**APDS (C4.3, C7.32, C8.11).** The ammunition `apds` for a Gun that lists a D item, on the Vehicle Target Type: the Basic TH# lessened as APCR's is, the Basic TK# of the APCR/APDS table's D listings (57L 18, 76LL 25; read on a render of p. 190), its own Case D row, and the AP table's number against an unarmored vehicle (C7.321).

**C8.9 for every ammunition with a Depletion Number.** Below the number the round is used; at it, it is the last; above it the Gun had none, has not fired (the ROF kept, no fire counter, the Acquisition as it was), and has none for the rest of the game, unless the same DR is a malfunction. This was APCR's and HEAT's on the Vehicle Target Type; it now holds for APDS there and for HE by its number on the Infantry Target Type, where the step stands after the malfunction check and before the hit, so that no IFT roll is asked for a round that was not there. AP by its number takes the same road (a Rules test with a made-up Gun).

**C2.21.** A Gun whose HE has a Depletion Number does not fire HE unnamed. At Infantry the page names `he` for it; unnamed it is refused (`he-not-named`), and with no HE at the date or none left it is refused with "This Gun's HE is Special Ammunition (C8.8) and it has none now ... its AP against Infantry is not built (C8.31, HE Equivalency)".

**Elite (C8.2; ruling R55.6).** The special rule `elite-ammunition:<side>` adds one to every Depletion Number of that side's ordnance; the shot records `elite`. No card carries it.

**The page.** The ammunition list shows what the Gun may fire at the chosen target, each special kind with its number at the game's date ("APDS (Depletion Number 6)"; "7: 6, and 1 for Elite, C8.2"), and a note for a kind left out ("APDS: none at the scenario's date (C8.9)", "APDS: it has run out (C8.9)"). The proposal says before the roll what a DR above the number means; the record says which of the three it was.

**The Supply Chart (C8.12; ruling R55.5)** is a Rules test's data. Two counters differ from it and are pinned by name, as C8.12 allows a counter to: the PzKpfw IIIH keeps A5 from 1943 on where the chart falls to A4, A3, and none, and the T-34 M41 has A6 in 1945 where the chart has A7. The 6-Pounder and the Russian 45L agree with it.

### 9.3 Ruling, the new facts, the games, and the tests

- **R55.25.** A shot whose special ammunition was not there leaves no fire counter of any kind, the Intensive Fire counter included (C8.9; C8.92 only says the number is not changed by Intensive Fire).

**New in a record:** `elite` on an ordnance shot (absent: not Elite; the check recomputes it from the game's own special rules); `ammunitionUse` on an Infantry Target Type resolution; `apds` and `he` among a Gun's depleted kinds. The step runs only for a shot that names an ammunition whose counter lists a number, which before the pass was APCR and HEAT alone.

In my Studio on port 6671, board 3, scripted dice, a 6-Pounder in [D10], a PzKpfw IIIH in [F10], a squad in [G10]; each game's file was read back.

| Game | What it shows |
|---|---|
| `p55-apds-usedc` | June 1944. APDS on an Original 3: "below the Depletion Number 6: APDS fired", Basic TK# 18. APDS on a 6: its last; the list then offers AP and HE with "APDS: it has run out (C8.9)". Recorded. |
| `p55-apds-nonec` | APDS on an Original 7: no hit, no fire counter, the ROF kept; AP then fires. Recorded. |
| `p55-apds-malfc` | APDS on a 12: none, and the Gun malfunctions and takes its Prep Fire counter. Recorded. |
| `p55-apds-mayc` | CONTROL, May 1944: the list has no APDS, "none at the scenario's date". Nothing proposed. |
| `p55-apds-elitec` | With `elite-ammunition:british`: "Depletion Number 7"; an Original 7 is the last round. Recorded. |
| `p55-he-numberc` | HE (8) at the squad in [G10]: named by the page, a hit, 6 FP, "HE fired (C8.9)". Recorded. |
| `p55-he-1942c` | CONTROL, July 1942: no HE yet; refused with the reason above. |
| the same ids without a letter, and with `b` | SUPERSEDED or spoiled by the session's own script (a tank wrecked by the first shot; a page not yet loaded when the dice were queued). |

Rules tests: 24 in `ScenarioA1Pass55RulesTests` in all. Play tests: 17 in `BacklogPass55Tests` in all. Run by the agent: the Rules classes of the ordnance passes 149, Play 121, MapStudio's ordnance, vehicle, and record classes 14: none failed.

**The replay digest after increment 2b** (93,266 lines, from the copy of 2b's own build): no line differs from the base.
